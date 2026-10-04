using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// One read-only sweep over everything the ten-item complaint needs a
    /// number for. Nothing here changes the scene.
    ///
    /// <para>Written because each of those items is a claim about the running
    /// game and none of them can be settled by reading the scripts. "There is
    /// only 2 crawlers" could mean two components, two visible skins, two risen,
    /// or three with one underground — four different bugs with four different
    /// fixes. "One crawler is in air" could be a bad placement height or a
    /// renderer that is still submerged. Guessing which is what produced four
    /// false verdicts in one night.</para>
    ///
    /// <para>Every section carries the number that argues against its own
    /// reading, which is the only reason the last four got caught.</para>
    /// </summary>
    public static class L1Probe10
    {
        const string Report = "Temp/l1_probe10.txt";

        [MenuItem("Tools/Echoes/L1 Probe — Ten Items", priority = 61)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] L1 probe — the ten complaints, measured");
            sb.AppendLine("scene: " + SceneManager.GetActiveScene().path);

            ScreenSection(sb);
            Crawlers(sb);
            CrawlerLookingThings(sb);
            Fragment(sb);
            MonoLines(sb);
            Animators(sb);
            Finish(sb);
        }

        // --- 1. the screen the text is measured against ------------------------

        static void ScreenSection(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== 1. THE SCREEN THE TEXT IS DRAWN ON ===");

            sb.AppendLine("  Screen.width/height right now (edit mode) : " +
                          Screen.width + " x " + Screen.height);
            sb.AppendLine("  PlayerSettings default                    : " +
                          PlayerSettings.defaultScreenWidth + " x " +
                          PlayerSettings.defaultScreenHeight +
                          "  fullscreen=" + PlayerSettings.fullScreenMode);

            var res = Screen.currentResolution;
            sb.AppendLine("  display                                    : " +
                          res.width + " x " + res.height);

            // The sizes the HUD will actually ask for, at three resolutions, so
            // the report says what the player sees rather than what a constant
            // says.
            sb.AppendLine();
            sb.AppendLine("  font size the HUD asks for, by resolution");
            sb.AppendLine("    resolution   scale   prompt(30)  row(22)  ari label(15)");
            int[] heights = { 480, 720, 900, 1080, 1440, 2160 };
            for (int i = 0; i < heights.Length; i++)
            {
                float k = Mathf.Max(0.7f, heights[i] / 720f);
                sb.AppendLine("    " + heights[i].ToString().PadLeft(4) + "p      " +
                              k.ToString("F2") + "     " +
                              Mathf.RoundToInt(30f * k).ToString().PadLeft(3) + " px     " +
                              Mathf.RoundToInt(22f * k).ToString().PadLeft(3) + " px    " +
                              Mathf.RoundToInt(15f * k).ToString().PadLeft(3) + " px");
            }
            sb.AppendLine();
            sb.AppendLine("  the number that argues against 'too small': at 1080p the " +
                          "prompt asks for 45 px, which is 4.2% of screen height. " +
                          "If the player is calling that unreadable, scaling it " +
                          "further is the only lever there is — there is no second " +
                          "font and no resolution to blame.");
        }

        // --- 2. every InkCrawler, and whether the player can see it ------------

        static void Crawlers(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== 2. INK CRAWLERS ===");

            var all = Object.FindObjectsByType<InkCrawler>(FindObjectsInactive.Include);
            sb.AppendLine("  InkCrawler components in the level: " + all.Length +
                          "   (the beat is written for 3)");

            int visible = 0, enabledCount = 0;

            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                var t = c.transform;

                var skin = c.GetComponentInChildren<SkinnedMeshRenderer>(true);
                var rends = c.GetComponentsInChildren<Renderer>(true);
                int rendsOn = 0;
                for (int r = 0; r < rends.Length; r++)
                    if (rends[r] != null && rends[r].enabled &&
                        rends[r].gameObject.activeInHierarchy) rendsOn++;

                var em = c.GetComponent<InkCrawlerEmerge>();
                var caps = c.GetComponentsInChildren<Collider>(true);
                int capsOn = 0;
                for (int r = 0; r < caps.Length; r++)
                    if (caps[r] != null && caps[r].enabled) capsOn++;

                if (c.enabled) enabledCount++;
                if (rendsOn > 0) visible++;

                sb.AppendLine();
                sb.AppendLine("  [" + i + "] " + t.root.name + " / " + PathName(t));
                sb.AppendLine("      position   " + t.position.ToString("F2"));
                sb.AppendLine("      active     self=" + t.gameObject.activeSelf +
                              " inHierarchy=" + t.gameObject.activeInHierarchy +
                              "  componentEnabled=" + c.enabled);
                sb.AppendLine("      layer      " + LayerName(c.gameObject.layer));
                sb.AppendLine("      skin       " + (skin == null ? "NONE"
                              : skin.name + "  mesh=" + (skin.sharedMesh == null ? "NULL" : "ok") +
                                "  bounds " + skin.bounds.ToString("F2")));
                sb.AppendLine("      renderers  " + rendsOn + " on of " + rends.Length);
                sb.AppendLine("      colliders  " + capsOn + " on of " + caps.Length);
                sb.AppendLine("      emerge     " + (em == null ? "NO COMPONENT — it can " +
                                  "never come up out of the ground" : "present"));
                sb.AppendLine("      under beat " + Under(t, "L1_Beat5"));

                // Why is it where it is? Every downward hit under the spot, so
                // "one crawler is in the air" can be told apart from "one
                // crawler is under the paving".
                sb.AppendLine("      what is under it, top first:");
                var from = t.position + Vector3.up * 8f;
                var hits = Physics.RaycastAll(from, Vector3.down, 30f, ~0,
                                              QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));
                int shown = 0;
                for (int h = 0; h < hits.Length && shown < 6; h++)
                {
                    if (hits[h].collider.GetComponentInParent<InkCrawler>() != null) continue;
                    sb.AppendLine("        y " + hits[h].point.y.ToString("F2") +
                                  "  " + hits[h].collider.name + " (" +
                                  hits[h].collider.GetType().Name + ", " +
                                  hits[h].collider.bounds.size.ToString("F2") + ")");
                    shown++;
                }
                if (shown == 0) sb.AppendLine("        NOTHING — it is over a hole");
            }

            sb.AppendLine();
            sb.AppendLine("  summary: " + all.Length + " component(s), " +
                          enabledCount + " enabled, " + visible +
                          " with a renderer actually switched on.");
            sb.AppendLine("  if components=3 and visible<3, the missing one is not " +
                          "missing: it is there and switched off.");
        }

        // --- 3. things that LOOK like crawlers but are not components ---------

        static void CrawlerLookingThings(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== 3. ANYTHING ELSE WITH A CRAWLER IN ITS NAME ===");
            sb.AppendLine("(the report said two more ink crawlers stand there the " +
                          "whole level. Those are not InkCrawler components, or " +
                          "they would be in section 2.)");

            var roots = SceneManager.GetActiveScene().GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
                Walk(roots[i].transform, sb);
        }

        static void Walk(Transform t, StringBuilder sb)
        {
            string n = t.name.ToLowerInvariant();

            if (n.Contains("crawler") || n.Contains("ink"))
            {
                var comps = t.GetComponents<Component>();
                var sbC = new StringBuilder();
                for (int i = 0; i < comps.Length; i++)
                    if (comps[i] != null && !(comps[i] is Transform))
                        sbC.Append(comps[i].GetType().Name + " ");

                var rends = t.GetComponentsInChildren<Renderer>(true);
                int on = 0;
                for (int r = 0; r < rends.Length; r++)
                    if (rends[r] != null && rends[r].enabled) on++;

                var sk = t.GetComponentInChildren<SkinnedMeshRenderer>(true);

                sb.AppendLine("  " + PathName(t));
                sb.AppendLine("      at " + t.position.ToString("F2") +
                              "  active=" + t.gameObject.activeInHierarchy +
                              "  renderers " + on + "/" + rends.Length);
                if (sk != null)
                    sb.AppendLine("      skin " + sk.name + " bounds " +
                                  sk.bounds.ToString("F2") +
                                  (on > 0 ? "  <-- THIS IS WHY THE PLAYER SEES A STATUE"
                                          : "  (renderer off)"));
                sb.AppendLine("      has InkCrawler: " +
                              (t.GetComponent<InkCrawler>() != null) +
                              "   has InkCrawlerEmerge: " +
                              (t.GetComponent<InkCrawlerEmerge>() != null));
                sb.AppendLine("      components: " + sbC.ToString().Trim());
            }

            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), sb);
        }

        // --- 4. the blue fragment ---------------------------------------------

        static void Fragment(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== 4. THE BLUE FRAGMENT ===");

            var frags = Object.FindObjectsByType<ColourFragment>(FindObjectsInactive.Include);
            sb.AppendLine("  ColourFragment components: " + frags.Length);

            for (int i = 0; i < frags.Length; i++)
            {
                var f = frags[i];
                sb.AppendLine("  [" + i + "] " + PathName(f.transform));
                sb.AppendLine("      at " + f.transform.position.ToString("F2") +
                              "  active=" + f.gameObject.activeInHierarchy +
                              "  taken=" + f.Taken + "  showing=" + f.IsShowing);
                sb.AppendLine("      has a Glow child: " +
                              (f.transform.Find("Glow") != null) +
                              "   children: " + f.transform.childCount);
                sb.AppendLine("      prompt: '" + f.Prompt + "'  reach " +
                              f.Range.ToString("F2") + " m");
            }

            sb.AppendLine();
            sb.AppendLine("  who calls Show(true)?  (a fragment that is never shown " +
                          "is a wall with nothing in it)");
            sb.AppendLine("  --- searching source ---");
            sb.AppendLine(SourceGrep("ColourFragment") + "--- end ---");

            var fountain = Object.FindObjectsByType<FountainFix>(FindObjectsInactive.Include);
            sb.AppendLine();
            sb.AppendLine("  FountainFix in scene: " + fountain.Length);
            for (int i = 0; i < fountain.Length; i++)
                sb.AppendLine("      " + PathName(fountain[i].transform) + " at " +
                              fountain[i].transform.position.ToString("F2"));

            sb.AppendLine();
            sb.AppendLine("  Ari's own carry flag right now: " +
                          AriHudOverlay.CarryingFragment);
        }

        // --- 5. Mono's lines ---------------------------------------------------

        static void MonoLines(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== 5. MONO'S LINES ===");

            var guids = AssetDatabase.FindAssets("t:MonoHintLines");
            sb.AppendLine("  MonoHintLines assets: " + guids.Length);

            for (int g = 0; g < guids.Length; g++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[g]);
                var asset = AssetDatabase.LoadAssetAtPath<MonoHintLines>(path);
                sb.AppendLine("  " + path);
                if (asset == null) { sb.AppendLine("      could not load"); continue; }

                asset.Tally(out int beat, out int rung, out int amb, out int muted);
                sb.AppendLine("      " + beat + " beat, " + rung + " rung, " +
                              amb + " ambient, " + muted + " muted");

                for (int i = 0; i < asset.lines.Count; i++)
                {
                    var l = asset.lines[i];
                    sb.AppendLine("      [" + i + "] " + (l.muted ? "MUTED " : "") +
                                  l.kind + " '" + l.id + "'" +
                                  (l.kind == MonoHintLines.LineKind.HintRung
                                      ? " rung " + l.rung : "") + "\n            " +
                                  l.text);
                }
            }

            var mono = MonoCompanion.FindInLevel();
            sb.AppendLine();
            sb.AppendLine("  Mono in level: " + (mono == null ? "NOT FOUND"
                              : PathName(mono.transform) + "  awake=" + mono.IsAwake));
        }

        // --- 6. the animators, because "the crawlers lag" is an animator claim

        static void Animators(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== 6. ANIMATOR CONTROLLERS ===");

            var anims = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include);

            for (int i = 0; i < anims.Length; i++)
            {
                var a = anims[i];
                var ac = a.runtimeAnimatorController;

                sb.AppendLine();
                sb.AppendLine("  " + PathName(a.transform));
                sb.AppendLine("      controller: " +
                              (ac == null ? "NONE — it stands in its bind pose"
                                          : ac.name));

                if (ac == null) continue;

                // Which parameters does anything actually drive? An un-driven
                // parameter is the whole of "the crawler lags": the clip is
                // playing, the transform is sliding, and nothing is connected.
                sb.AppendLine("      states:");

                // parameters/layers are on the editor-side AnimatorController,
                // not on RuntimeAnimatorController. A controller asset that is
                // an override or a blend tree has neither, and that is reported
                // rather than skipped silently.
                var ctrl = ac as UnityEditor.Animations.AnimatorController;

                if (ctrl == null)
                {
                    sb.AppendLine("        NOT AN AnimatorController asset (" +
                                  ac.GetType().Name + ") — no parameters or states " +
                                  "to read. This is a finding: a controller of this " +
                                  "kind cannot have a Speed blend tree.");
                    continue;
                }

                var names = new List<string>();
                foreach (var p in ctrl.parameters) names.Add(p.name + ":" + p.type);
                sb.AppendLine("      parameters: " +
                              (names.Count == 0 ? "none" : string.Join(", ", names)));

                var layers = ctrl.layers;
                for (int L = 0; L < layers.Length; L++)
                {
                    var sm = layers[L].stateMachine;
                    foreach (var cs in sm.states)
                    {
                        int outs = 0;
                        foreach (var t in cs.state.transitions) if (!t.hasExitTime == false) outs++;
                        sb.AppendLine("        L" + L + " '" + cs.state.name +
                                      "'  motion=" +
                                      (cs.state.motion == null ? "NULL"
                                          : cs.state.motion.name) +
                                      "  speed=" + cs.state.speed.ToString("F2") +
                                      "  -> " + outs + " transition(s)");
                    }
                }
            }
        }

        // --- helpers -------------------------------------------------------------

        static string SourceGrep(string token)
        {
            var outp = new StringBuilder();
            string[] files = Directory.GetFiles(Application.dataPath, "*.cs",
                                                SearchOption.AllDirectories);
            System.Array.Sort(files);

            for (int i = 0; i < files.Length; i++)
            {
                var lines = File.ReadAllLines(files[i]);
                for (int l = 0; l < lines.Length; l++)
                {
                    string s = lines[l];
                    if (s.TrimStart().StartsWith("//")) continue;      // prose, not code
                    if (!s.Contains(token)) continue;
                    outp.AppendLine("      " + Path.GetFileName(files[i]) + ":" +
                                    (l + 1) + ": " + s.Trim());
                }
            }
            return outp.ToString();
        }

        static string LayerName(int i) =>
            LayerMask.LayerToName(i) == "" ? i.ToString() : LayerMask.LayerToName(i);

        static string PathName(Transform t)
        {
            var parts = new List<string>();
            var cur = t;
            while (cur != null) { parts.Insert(0, cur.name); cur = cur.parent; }
            return string.Join("/", parts.ToArray());
        }

        static string Under(Transform t, string root)
        {
            var cur = t;
            while (cur != null)
            {
                if (cur.name == root) return "yes (" + root + ")";
                cur = cur.parent;
            }
            return "NO — not under " + root;
        }

        static void Finish(StringBuilder sb)
        {
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report),
                              sb.ToString());
            Debug.Log("[Echoes] L1 probe — see " + Report);
        }
    }
}