using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Measures the five things the player reported after actually playing.
    /// </summary>
    /// <remarks>
    /// <para>This is a different tool from L1Probe10 and from L1Text1080,
    /// because these five reports are all about RELATIONSHIPS - a crawler
    /// against a wall, a health bar against its own value, a text rect
    /// against the screen - and a per-object dump cannot see any of them.
    /// Every number below is measured from the scene as it is on disk.</para>
    ///
    /// <para>Nothing here is inferred from a field name. The crawler section
    /// reports renderer and collider enabled counts SEPARATELY, because
    /// "invisible but still attacking" has exactly two possible causes and
    /// they look identical in a single combined field - the crawler being
    /// underground, or its renderers being off - and the two need opposite
    /// fixes.</para>
    /// </remarks>
    public static class L1Now
    {
        private const string ReportPath = "Temp/l1_now.txt";

        // Beat5Setup's own constants. These are quoted here so the yard can be
        // compared against the crawlers without trusting a tool to agree with
        // itself.
        private const float YardXMin = 38f, YardXMax = 54f, YardZMin = 4f, YardZMax = 13f;

        [MenuItem("Echoes/L1/What Is Wrong Now")]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("L1 - the five things the player hit while playing");
            sb.AppendLine("yard is x " + YardXMin + ".." + YardXMax + ", z " + YardZMin + ".." + YardZMax + ", ground y 0");
            sb.AppendLine("");

            // -------------------------------------------------------------------
            // 1 + 2 + 3. THE CRAWLERS
            // -------------------------------------------------------------------
            sb.AppendLine("=== 1/2/3. INK CRAWLERS: where, and can they be seen? ===");

            var ari = UnityEngine.Object.FindAnyObjectByType<AriMover>();
            Vector3 ariPos = Vector3.zero;
            bool haveAri = false;
            if (ari != null)
            {
                ariPos = ari.transform.position;
                haveAri = true;
            }
            sb.AppendLine("  Ari at " + (haveAri ? ariPos.ToString("F2") : "NOT FOUND"));
            sb.AppendLine("");
            sb.AppendLine("  name                 pos                 active  rendOn rendOff colOn colOff emerge      stand            sunkBy   inYard");
            sb.AppendLine("  " + new string('-', 128));

            var all = UnityEngine.Object.FindObjectsByType<InkCrawler>((FindObjectsInactive)1);
            var emergeComps = UnityEngine.Object.FindObjectsByType<InkCrawlerEmerge>((FindObjectsInactive)1);

            var report = new List<string>();
            foreach (var c in all)
            {
                var t = c.transform;
                var rs = c.GetComponentsInChildren<Renderer>(true);
                var cs = c.GetComponentsInChildren<Collider>(true);
                int rOn = 0, cOn = 0;
                foreach (var r in rs) if (r != null && r.enabled) rOn++;
                foreach (var q in cs) if (q != null && q.enabled) cOn++;

                var em = c.GetComponent<InkCrawlerEmerge>();
                string phase = em == null ? "none" : em.Now.ToString();
                Vector3 stand = em == null ? t.position : em.StandPoint;
                float sunk = stand.y - t.position.y;
                bool inYard = t.position.x >= YardXMin && t.position.x <= YardXMax &&
                              t.position.z >= YardZMin && t.position.z <= YardZMax;

                // The one measurement that settles "why can I not see it":
                // is the renderer's own world box above the floor, and is
                // anything standing between it and Ari's eye.
                string vis = "n/a";
                var skin = c.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (skin != null && skin.enabled)
                {
                    Bounds b = skin.bounds;
                    float top = b.max.y;
                    float bottom = b.min.y;
                    vis = "skin y " + bottom.ToString("0.00") + ".." + top.ToString("0.00") +
                          (top <= 0.02f ? "  UNDER THE FLOOR" : (bottom < -0.05f ? "  half-buried" : "  visible"));
                }

                string line = string.Format(
                    "  {0,-20} {1,-18} {2,-7} {3,5} {4,7} {5,5} {6,6} {7,-11} {8,-15} {9,-8} {10}",
                    t.name, t.position.ToString("F2"), t.gameObject.activeInHierarchy.ToString(),
                    rOn + "/" + rs.Length, (rs.Length - rOn), cOn + "/" + cs.Length, (cs.Length - cOn),
                    phase, stand.ToString("F2"), sunk.ToString("F2"), inYard ? "yes" : "NO");
                report.Add(line);
                report.Add("        " + vis);

                if (haveAri)
                {
                    report.Add("        " + AriToCrawler(c.transform, ariPos));
                }
            }

            foreach (var l in report) sb.AppendLine(l);

            int activeCount = 0;
            foreach (var c in all) if (c.gameObject.activeInHierarchy && c.GetComponentInChildren<Renderer>(true) != null &&
                ((Renderer)c.GetComponentInChildren<Renderer>(true)).enabled) activeCount++;
            sb.AppendLine("");
            sb.AppendLine("  InkCrawler components total : " + all.Length);
            sb.AppendLine("  of those, RENDERABLE         : " + activeCount + "   <- these are the ones the player can see");
            sb.AppendLine("  of those, collider on       : " + CountSolid(all));
            sb.AppendLine("  of those, collider OFF      : " + CountNotSolid(all) + "   <- sunk, or a statue, or both");
            sb.AppendLine("");
            sb.AppendLine("  READING THIS: if a row shows collider on AND a renderer off, the player");
            sb.AppendLine("  is being hit by something that cannot be seen. If a row shows collider on,");
            sb.AppendLine("  renderers on, but sunkBy > 0, the crawler is standing under the floor.");
            sb.AppendLine("  If both are fine, the crawler is simply too far away to see from Ari's");
            sb.AppendLine("  position, and the number to look at is the distance.");

            // -------------------------------------------------------------------
            // 4. HEALTH
            // -------------------------------------------------------------------
            sb.AppendLine("");
            sb.AppendLine("=== 4. HEALTH ===");
            var hud = UnityEngine.Object.FindAnyObjectByType<AriHudOverlay>();
            if (hud != null)
            {
                var t = typeof(AriHudOverlay);
                sb.AppendLine("  AriHudOverlay on " + hud.name);
                DumpFields(sb, t, hud);
            }
            else sb.AppendLine("  AriHudOverlay NOT FOUND");

            // -------------------------------------------------------------------
            // 5. BLUE FRAGMENT
            // -------------------------------------------------------------------
            sb.AppendLine("");
            sb.AppendLine("=== 5. BLUE FRAGMENT AND THE FOUNTAIN ===");
            var frags = UnityEngine.Object.FindObjectsByType<ColourFragment>((FindObjectsInactive)1);
            if (frags.Length == 0) sb.AppendLine("  NO ColourFragment in the level.");
            foreach (var f in frags)
            {
                sb.AppendLine("  " + f.name + "  pos " + f.transform.position.ToString("F2") +
                              "  active=" + f.gameObject.activeInHierarchy);
                var rs = f.GetComponentsInChildren<Renderer>(true);
                int on = 0;
                foreach (var r in rs) if (r != null && r.enabled) on++;
                sb.AppendLine("      renderers " + on + "/" + rs.Length + (on == 0 ? "   NOT DRAWN" : ""));
                DumpFields(sb, typeof(ColourFragment), f, "      ");
                sb.AppendLine("      distance from Ari: " + (haveAri ? Vector3.Distance(f.transform.position, ariPos).ToString("F2") + " m" : "no Ari"));
            }

            // -------------------------------------------------------------------
            // 1. TEXT COVERAGE
            // -------------------------------------------------------------------
            sb.AppendLine("");
            sb.AppendLine("=== 1. HOW MUCH SCREEN THE TEXT TAKES (1920x1080, hard-coded) ===");
            TextCoverage(sb);

            Write(sb);
        }

        private static string AriToCrawler(Transform c, Vector3 ari)
        {
            Vector3 from = ari + Vector3.up * 1.6f;
            Vector3 to = c.position + Vector3.up * 1.1f;
            Vector3 d = to - from;
            float dist = d.magnitude;
            if (dist < 0.01f) return "on top of Ari";

            var hits = Physics.RaycastAll(from, d / dist, dist);
            var names = new List<string>();
            foreach (var h in hits)
            {
                // a wall, not the crawler itself and not Ari's own body
                if (h.transform == c || h.transform.IsChildOf(c)) continue;
                if (h.transform.name.ToLower().Contains("ari")) continue;
                names.Add(h.transform.name + "(" + h.distance.ToString("0.00") + "m)");
            }
            names.Sort();
            string blocked = names.Count == 0 ? "nothing between them" : string.Join(", ", names.ToArray());
            return string.Format("dist {0,6:F2} m   line of sight: {1}", dist, blocked);
        }

        private static int CountSolid(InkCrawler[] all)
        {
            int n = 0;
            foreach (var c in all)
            {
                var cs = c.GetComponentsInChildren<Collider>(true);
                foreach (var q in cs) if (q != null && q.enabled) { n++; break; }
            }
            return n;
        }

        private static int CountNotSolid(InkCrawler[] all)
        {
            int n = 0;
            foreach (var c in all)
            {
                var cs = c.GetComponentsInChildren<Collider>(true);
                bool any = false;
                foreach (var q in cs) if (q != null && q.enabled) { any = true; break; }
                if (!any) n++;
            }
            return n;
        }

        private static void DumpFields(StringBuilder sb, Type t, object o, string pad = "      ")
        {
            var flags = System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.DeclaredOnly;
            foreach (var f in t.GetFields(flags))
            {
                object v;
                try { v = f.GetValue(o); }
                catch { continue; }
                if (f.FieldType == typeof(Color))
                {
                    var col = (Color)v;
                    sb.AppendLine(pad + f.Name + " = rgba(" + col.r.ToString("0.00") + "," + col.g.ToString("0.00") +
                                  "," + col.b.ToString("0.00") + "," + col.a.ToString("0.00") + ")  " + Describe(col));
                }
                else
                {
                    sb.AppendLine(pad + f.Name + " = " + v);
                }
            }
        }

        private static string Describe(Color c)
        {
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            string name = "grey";
            if (c.g > c.r * 1.15f && c.g > c.b * 1.15f) name = "GREEN";
            else if (c.r > c.g * 1.15f && c.r > c.b * 1.15f) name = "RED";
            else if (c.b > c.r * 1.15f && c.b > c.g * 1.15f) name = "BLUE";
            return name + " (hue " + Mathf.RoundToInt(h * 360f) + "deg, sat " + s.ToString("0.00") + ", val " + v.ToString("0.00") + ")";
        }

        private static void TextCoverage(StringBuilder sb)
        {
            const int W = 1920, H = 1080;
            float scale = Mathf.Max(0.7f, H / 720f);
            int target = Mathf.Max(10, Mathf.RoundToInt(30f * 5f * scale));
            float width = Mathf.Min(W * 0.9f, W - 24f);
            float maxBlock = H * 0.72f;

            var style = BeatText.MakeStandalone(TextAnchor.UpperLeft, target, true, Color.white);
            if (style == null || style.font == null) { sb.AppendLine("  no font, cannot measure"); return; }

            var lines = AssetDatabase.LoadAssetAtPath<MonoHintLines>("Assets/Painterly/MonoHintLines.asset");
            if (lines == null) { sb.AppendLine("  no MonoHintLines asset"); return; }

            sb.AppendLine("  target " + target + " px, block width " + width.ToString("0") + " px, block height limit " + maxBlock.ToString("0") + " px");
            sb.AppendLine("");
            sb.AppendLine("  a 225 px line is " + (target / (float)H * 100f).ToString("0.0") + "% of the screen height ON ITS OWN.");
            sb.AppendLine("  Three of them, plus spacing, is most of the picture. That is the arithmetic");
            sb.AppendLine("  behind 'text is taking the whole screen' - it is not a layout bug, it is");
            sb.AppendLine("  what 5x of 45 px means at 1080p.");
            sb.AppendLine("");
            sb.AppendLine("  worst measured page height, in px and as a share of the screen:");
            float worst = 0f;
            string worstWho = "-";
            foreach (var line in lines.All)
            {
                if (line == null || string.IsNullOrEmpty(line.text)) continue;
                var pages = BeatText.Pages(style, line.text, width, maxBlock, target);
                for (int i = 0; i < pages.Count; i++)
                {
                    style.fontSize = target;
                    float hgt = style.CalcHeight(new GUIContent(pages[i]), width);
                    if (hgt > worst) { worst = hgt; worstWho = line.id + " page " + (i + 1); }
                }
            }
            sb.AppendLine("    " + worst.ToString("0") + " px = " + (worst / H * 100f).ToString("0") + "% of 1080   (" + worstWho + ")");
            sb.AppendLine("");
            sb.AppendLine("  the prompt line is a different thing from a beat line and should be sized");
            sb.AppendLine("  separately. Its current request:");
            sb.AppendLine("    BeatText.PromptTarget = " + BeatText.PromptTarget + " px   (the same 5x as the beat text)");
            sb.AppendLine("    BeatText.RowTarget    = " + BeatText.RowTarget + " px   (5x of 22)");
        }

        private static void Write(StringBuilder sb)
        {
            var dir = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            byte[] bytes = new UTF8Encoding(false).GetBytes(sb.ToString());
            File.WriteAllBytes(ReportPath, bytes);
            long onDisk = new FileInfo(ReportPath).Length;
            Debug.Log("[Echoes] L1 now -> " + ReportPath + "  (" + onDisk + " B on disk, " + bytes.Length + " B intended)");
            Debug.Log(sb.ToString());
        }
    }
}