using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Puts the three ink crawlers where they can actually be seen and reached.
    /// </summary>
    /// <remarks>
    /// <para><b>The defect this fixes.</b> The player reported that the crawlers
    /// attack Ari but cannot be seen, and that Ari has to walk to them instead
    /// of them coming to her. L1Now measured both halves of that and they have
    /// one cause: placement.</para>
    ///
    /// <para><c>Beat5_Crawler_A</c> stood at z = -0.52 and <c>Beat5_Crawler_B</c>
    /// at z = 1.48, while the yard is z 4..13. Both are outside it, and the ray
    /// from Ari to each one hits <c>Beat4_Wall_S</c>, <c>DoorFrame_Flat_WoodDark</c>,
    /// <c>GullyWall_B</c> and <c>Window_Thin_Round1</c> on the way. So they are
    /// behind the scenery, in a place the player never sees.</para>
    ///
    /// <para>The chase logic is not at fault and was deliberately not touched.
    /// <c>ChaseOrGiveUp</c> returns true unconditionally while hunting, with no
    /// distance limit, and <c>Closing</c> calls <c>Towards(Ari)</c> every frame.
    /// They do not reach her because <c>Travel</c> returns 0 when the step is
    /// blocked, and a wall is exactly that. A crawler cannot path through a
    /// doorframe, so the fix belongs in the position, not in the brain.</para>
    ///
    /// <para><b>Why this searches the whole yard.</b> L1Fixups.CrawlerGround
    /// looks for a clear spot within 3 m of wherever the crawler already is.
    /// That is the right rule for complaint 6, which was one crawler standing
    /// 2.20 m up on a cover wall, and the wrong rule here: A and B are 5 m and
    /// 12 m outside the yard, so a 3 m search can never reach any legal spot and
    /// the tool correctly refuses to push a crawler into a wall. It was not
    /// failing. It was solving a different question.</para>
    ///
    /// <para><b>Every accepted spot is measured four ways</b> and printed, so
    /// the choice can be checked rather than trusted:</para>
    /// <list type="number">
    /// <item><description>there is a floor under it, taken from a downward ray
    /// that ignores the crawlers' own colliders;</description></item>
    /// <item><description>nothing is inside its body capsule;</description></item>
    /// <item><description>nothing solid stands between it and the yard entry,
    /// which is what "the player can see it" means;</description></item>
    /// <item><description>it is within range of the entry, so it closes on Ari
    /// instead of making her cross the yard.</description></item>
    /// </list>
    ///
    /// <para><b>Reversible, in the scene.</b> The old position is written to
    /// <c>InkCrawlerEmerge.authoredStand</c> before the move, which is the same
    /// mechanism complaint 6 used, because Temp is not part of the project and
    /// a restore that depends on Temp silently becomes a no-op the first time
    /// Temp is cleaned.</para>
    /// </remarks>
    public static class L1YardFix
    {
        // Beat5Setup's constants, quoted rather than imported so this tool and
        // the beat cannot quietly disagree about where the yard is.
        private const float YardXMin = 38f, YardXMax = 54f;
        private const float YardZMin = 4f, YardZMax = 13f;
        private const float GroundY = 0f;

        // Where Ari comes in. Crawlers are required to be able to see THIS, and
        // to be within reach of it, so that they converge on her instead of
        // waiting in a corner for her to come over.
        private const float EntryX = 38.5f;
        private const float EntryZ = 8.5f;

        private const float MaxFromEntry = 13f;

        // Ari walks in at the entry. A crawler standing closer than this is
        // inside her arrival, and the beat opens with her already being hit -
        // which reads as the game being broken rather than as three crawlers
        // closing in. The first run of this tool had no lower bound and put one
        // crawler 0.7 m from the entry as a direct result, so the bound is here
        // because that number was measured and not imagined.
        private const float MinFromEntry = 4f;

        private const float MinApart = 5f;

        private const string ReportPath = "Temp/l1_yard_fix.txt";

        [MenuItem("Echoes/L1/Yard Fix - put the crawlers where they can be seen", priority = 71)]
        public static void Run()
        {
            Apply();
        }

        [MenuItem("Echoes/L1/Yard Fix - restore", priority = 72)]
        public static void Restore()
        {
            var sb = new StringBuilder();
            sb.AppendLine("L1 Yard Fix - restore");

            var all = UnityEngine.Object.FindObjectsByType<InkCrawler>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                var em = all[i].GetComponent<InkCrawlerEmerge>();
                if (em == null)
                {
                    sb.AppendLine("  " + all[i].name + ": no InkCrawlerEmerge, nothing recorded, nothing moved.");
                    continue;
                }
                if (em.RestoreAuthoredStand())
                    sb.AppendLine("  " + all[i].name + ": restored to " + em.AuthoredStand.ToString("F2"));
                else
                    sb.AppendLine("  " + all[i].name + ": no stand point was ever recorded, so nothing was moved.");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Write(sb);
        }

        private static void Apply()
        {
            var sb = new StringBuilder();
            sb.AppendLine("L1 Yard Fix - put the crawlers where they can be seen");
            sb.AppendLine("yard x " + YardXMin + ".." + YardXMax + ", z " + YardZMin + ".." + YardZMax + ", ground y " + GroundY);
            sb.AppendLine("entry (where Ari comes in) " + EntryX.ToString("F1") + ", " + EntryZ.ToString("F1"));
            sb.AppendLine("required: a floor, a clear body capsule, nothing solid between it and the entry, within " +
                          MaxFromEntry.ToString("0") + " m of the entry, and " + MinApart.ToString("0") + " m from the others");
            sb.AppendLine("");

            var crawlers = UnityEngine.Object.FindObjectsByType<InkCrawler>(FindObjectsInactive.Include);
            var owner = new HashSet<Collider>(CrawlerColliders(crawlers));

            // -------------------------------------------------------------------
            // Where they are now, and how wrong that is.
            // -------------------------------------------------------------------
            sb.AppendLine("=== BEFORE ===");
            sb.AppendLine("  name                 pos                 inYard  distance to the yard  LOS to entry");
            for (int i = 0; i < crawlers.Length; i++)
            {
                var t = crawlers[i].transform;
                var p = t.position;
                bool inYard = p.x >= YardXMin && p.x <= YardXMax && p.z >= YardZMin && p.z <= YardZMax;
                sb.AppendLine(string.Format("  {0,-20} {1,-18} {2,-6} {3,8} m   {4}",
                    t.name, p.ToString("F2"), inYard ? "yes" : "NO",
                    OutsideBy(p), LineOfSight(p, owner)));
            }

            // -------------------------------------------------------------------
            // Search the whole yard.
            // -------------------------------------------------------------------
            var legal = new List<Vector3>();
            var rejected = new Dictionary<string, int>();
            for (float x = YardXMin + 1f; x <= YardXMax - 1f; x += 1f)
            {
                for (float z = YardZMin + 1f; z <= YardZMax - 1f; z += 1f)
                {
                    var p = new Vector3(x, GroundY, z);
                    string why;
                    if (!FloorAt(p.x, p.z, owner, out float floor, out string fname)) { Bump(rejected, "no floor"); continue; }
                    if (Mathf.Abs(floor - GroundY) > 0.35f) { Bump(rejected, "floor at " + floor.ToString("0.00") + " not paving"); continue; }
                    if (!ClearBody(p, floor, owner, out why)) { Bump(rejected, why); continue; }
                    if (BlockedToEntry(p, owner)) { Bump(rejected, "something solid between it and the entry"); continue; }
                    float fromEntry = Vector3.Distance(p, EntryPoint);
                    if (fromEntry > MaxFromEntry) { Bump(rejected, "too far from the entry"); continue; }
                    if (fromEntry < MinFromEntry) { Bump(rejected, "inside Ari's arrival, under " + MinFromEntry.ToString("0") + " m"); continue; }
                    legal.Add(p);
                }
            }

            sb.AppendLine("");
            sb.AppendLine("=== SEARCH: every cell of the yard, 1 m steps ===");
            sb.AppendLine("  legal spots : " + legal.Count);
            sb.AppendLine("  rejected:");
            foreach (var kv in rejected) sb.AppendLine("    " + kv.Key.PadRight(46) + kv.Value + " cell(s)");

            if (legal.Count < 3)
            {
                sb.AppendLine("");
                sb.AppendLine("STOPPED. Fewer than 3 legal spots exist, so placing 3 crawlers would mean");
                sb.AppendLine("putting at least one inside geometry. Nothing was moved. A yard this blocked");
                sb.AppendLine("is a level design problem, not something a tool should paper over.");
                Write(sb);
                return;
            }

            // Closest to the entry first, so the crawlers engage quickly, and skip
            // anything too close to one already chosen so they do not overlap.
            legal.Sort((a, b) => Vector3.Distance(a, EntryPoint).CompareTo(Vector3.Distance(b, EntryPoint)));
            var chosen = new List<Vector3>();
            foreach (var c in legal)
            {
                bool ok = true;
                for (int i = 0; i < chosen.Count; i++)
                    if (Vector3.Distance(c, chosen[i]) < MinApart) { ok = false; break; }
                if (!ok) continue;
                chosen.Add(c);
                if (chosen.Count == 3) break;
            }

            sb.AppendLine("");
            sb.AppendLine("=== CHOSEN ===");
            for (int i = 0; i < chosen.Count; i++)
            {
                var p = chosen[i];
                sb.AppendLine("  [" + i + "] " + p.ToString("F2") +
                              "   " + Vector3.Distance(p, EntryPoint).ToString("0.0") + " m from the entry" +
                              "   LOS: " + LineOfSight(p, owner));
            }

            // -------------------------------------------------------------------
            // Move, remembering where each one was.
            // -------------------------------------------------------------------
            sb.AppendLine("");
            sb.AppendLine("=== AFTER ===");
            var scene = EditorSceneManager.GetActiveScene();
            for (int i = 0; i < crawlers.Length && i < chosen.Count; i++)
            {
                var c = crawlers[i];
                var t = c.transform;
                var em = c.GetComponent<InkCrawlerEmerge>();
                Vector3 from = t.position;

                if (em == null)
                {
                    sb.AppendLine("  " + t.name + ": NO InkCrawlerEmerge, so this move cannot be undone. Left alone.");
                    continue;
                }
                em.RememberAuthoredStand(from);
                if (!em.HasAuthoredStand)
                {
                    sb.AppendLine("  " + t.name + ": could not record the stand point. Left alone rather than moved irreversibly.");
                    continue;
                }

                t.position = chosen[i];
                sb.AppendLine(string.Format("  {0,-20} {1,-18} -> {2,-18}  ({3} m)  authored stand kept: {4}",
                    t.name, from.ToString("F2"), chosen[i].ToString("F2"),
                    Vector3.Distance(from, chosen[i]).ToString("0.0"), em.AuthoredStand.ToString("F2")));
            }

            // -------------------------------------------------------------------
            // Prove it, by measuring the scene again rather than trusting the move.
            // -------------------------------------------------------------------
            sb.AppendLine("");
            sb.AppendLine("=== RE-MEASURED, AFTER ===");
            var now = UnityEngine.Object.FindObjectsByType<InkCrawler>(FindObjectsInactive.Include);
            var nowOwner = new HashSet<Collider>(CrawlerColliders(now));
            for (int i = 0; i < now.Length; i++)
            {
                var t = now[i].transform;
                var p = t.position;
                bool inYard = p.x >= YardXMin && p.x <= YardXMax && p.z >= YardZMin && p.z <= YardZMax;
                var skin = now[i].GetComponentInChildren<SkinnedMeshRenderer>(true);
                string skinLine = "no skin";
                if (skin != null)
                {
                    Bounds b = skin.bounds;
                    skinLine = "skin y " + b.min.y.ToString("0.00") + ".." + b.max.y.ToString("0.00") +
                               (skin.enabled ? "  drawn" : "  RENDERER OFF");
                }
                sb.AppendLine(string.Format("  {0,-20} {1,-18} inYard {2,-4} {3,6} m to entry  LOS {4}  {5}",
                    t.name, p.ToString("F2"), inYard ? "yes" : "NO",
                    Vector3.Distance(p, EntryPoint).ToString("0.0"), LineOfSight(p, nowOwner), skinLine));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            sb.AppendLine("");
            sb.AppendLine("scene saved: " + scene.path);
            Write(sb);
        }

        // -----------------------------------------------------------------------
        // Measurement helpers.
        // -----------------------------------------------------------------------

        private static Vector3 EntryPoint => new Vector3(EntryX, GroundY, EntryZ);

        private static Collider[] CrawlerColliders(InkCrawler[] crawlers)
        {
            var list = new List<Collider>();
            for (int i = 0; i < crawlers.Length; i++)
                list.AddRange(crawlers[i].GetComponentsInChildren<Collider>(true));
            return list.ToArray();
        }

        private static void Bump(Dictionary<string, int> d, string k)
        {
            int n;
            d[k] = d.TryGetValue(k, out n) ? n + 1 : 1;
        }

        private static string OutsideBy(Vector3 p)
        {
            float dx = p.x < YardXMin ? YardXMin - p.x : (p.x > YardXMax ? p.x - YardXMax : 0f);
            float dz = p.z < YardZMin ? YardZMin - p.z : (p.z > YardZMax ? p.z - YardZMax : 0f);
            float d = Mathf.Max(dx, dz);
            return d <= 0.001f ? "inside" : d.ToString("0.0");
        }

        /// <summary>Floor under a spot, ignoring the crawlers themselves.</summary>
        private static bool FloorAt(float x, float z, HashSet<Collider> owner, out float y, out string name)
        {
            y = 0f; name = "-";
            var origin = new Vector3(x, 8f, z);
            var hits = Physics.RaycastAll(origin, Vector3.down, 20f);
            float best = float.MaxValue;
            bool found = false;
            foreach (var h in hits)
            {
                if (owner.Contains(h.collider)) continue;
                if (h.collider.isTrigger) continue;
                if (h.point.y < best) { best = h.point.y; name = h.collider.name; found = true; }
            }
            if (found) y = best;
            return found;
        }

        /// <summary>
        /// Nothing solid inside the crawler's body.
        /// </summary>
        /// <remarks>
        /// The lower sphere is lifted by 2 cm and shrunk by 5%, because a sphere
        /// centred exactly one radius above the floor TOUCHES the floor, and
        /// whether a touching overlap counts is up to PhysX. That is not a
        /// hypothetical: an earlier version of this test reported all three
        /// crawlers wedged into the ground for exactly that reason, which
        /// looks identical to a real placement failure in the output.
        /// </remarks>
        private static bool ClearBody(Vector3 p, float floorY, HashSet<Collider> owner, out string why)
        {
            const float radius = 0.34f;
            const float height = 1.10f;
            float r = radius * 0.95f;
            var a = new Vector3(p.x, floorY + radius + 0.02f, p.z);
            var b = new Vector3(p.x, floorY + height - radius, p.z);
            var hits = Physics.OverlapCapsule(a, b, r);
            for (int i = 0; i < hits.Length; i++)
            {
                if (owner.Contains(hits[i])) continue;
                if (hits[i].isTrigger) continue;
                why = hits[i].name + " is inside the body";
                return false;
            }
            why = "clear";
            return true;
        }

        /// <summary>True when something solid stands between a spot and the entry.</summary>
        private static bool BlockedToEntry(Vector3 p, HashSet<Collider> owner)
        {
            return !LineOfSight(p, owner).StartsWith("clear");
        }

        /// <summary>Reports what stands between a spot and the entry, at chest height.</summary>
        private static string LineOfSight(Vector3 p, HashSet<Collider> owner)
        {
            var from = new Vector3(EntryX, 1.0f, EntryZ);
            var to = new Vector3(p.x, 1.0f, p.z);
            var d = to - from;
            float dist = d.magnitude;
            if (dist < 0.05f) return "clear (on the entry)";
            var hits = Physics.RaycastAll(from, d / dist, dist);
            Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
            var names = new List<string>();
            foreach (var h in hits)
            {
                if (owner.Contains(h.collider)) continue;
                if (h.collider.isTrigger) continue;
                if (names.Contains(h.collider.name)) continue;
                names.Add(h.collider.name + "@" + h.distance.ToString("0.0"));
            }
            return names.Count == 0 ? "clear" : string.Join(", ", names.ToArray());
        }

        private static void Write(StringBuilder sb)
        {
            var dir = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            byte[] bytes = new UTF8Encoding(false).GetBytes(sb.ToString());
            File.WriteAllBytes(ReportPath, bytes);
            long onDisk = new FileInfo(ReportPath).Length;
            Debug.Log("[Echoes] L1 yard fix -> " + ReportPath + "  (" + onDisk + " B on disk, " + bytes.Length + " B intended)");
            Debug.Log(sb.ToString());
        }
    }
}