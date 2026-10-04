using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Builds Beat 3: the sleeping tree, Mono's line list, the gully route and
    /// the wiring between them.
    ///
    /// The route is measured, not drawn. "A narrow gully" is a claim about the
    /// village's geometry, and the village was assembled from a kit by someone
    /// who was not thinking about Beat 3, so the only way to know whether a
    /// passage is a gully is to stand a body in it and see how much room is
    /// left. The tool casts left and right along each candidate route, reports
    /// the free width at every sample, and picks the narrowest route that a
    /// capsule the size of Ari's can actually walk down.
    ///
    /// A route that had to be accepted with a warning is reported as such. A
    /// gully that measures 9 m wide is a lane between two houses, the camera
    /// will pull in instead of out, and the beat will not read as intended —
    /// and none of that is visible anywhere except here.
    /// </summary>
    public static class Beat3Setup
    {
        const string ContainerName = "L1_Beat3";
        const string LinesPath = "Assets/Painterly/MonoHintLines.asset";
        const string Report = "Temp/beat3_setup.txt";

        /// <summary>Wider than this anywhere along the route and it is a lane, not a gully.</summary>
        const float GullyMaxWidth = 6.0f;

        /// <summary>Narrower than this and the follow camera cannot frame both.</summary>
        const float GullyMinWidth = 1.2f;

        /// <summary>
        /// Narrowest passage Ari can actually get down. Her capsule is 0.60 m
        /// across and the wall sweep stops her on contact, so a measured 0.7 m
        /// is a gap she jams against and the beat is unplayable at its first
        /// corner. 0.9 m is a margin over the body, not a round number.
        /// </summary>
        const float MinWalkableWidth = 0.9f;

        /// <summary>
        /// Why the last <see cref="Walkable"/> returned false. Empty when it
        /// returned true.
        ///
        /// Static, and at class scope because a static field cannot be declared
        /// inside a method body — the first attempt put it there and every read
        /// of it failed to resolve. It is static for the same reason the
        /// signature could not grow: two out parameters already carry the
        /// numbers, and adding "why it failed" and "how far it got" would make
        /// every caller responsible for reading four things to find out whether a
        /// route works. The one caller that forgot to check the return value is
        /// exactly how "free width median 0.0 m" got printed for a blocked
        /// route.
        ///
        /// Not thread-safe, and never will be. This runs in the editor, once,
        /// from a menu item.
        /// </summary>
        static string _reason;

        /// <summary>Where on the route the last <see cref="Walkable"/> ran out.</summary>
        static Vector3 _blockedAt;

        [MenuItem("Tools/Echoes/Build Beat 3 - Mono Awakens", priority = 70)]
        public static void Run()
        {
            var sb = new StringBuilder();
            if (File.Exists(PathOf(Directory.GetCurrentDirectory(), Report)))
                File.Delete(PathOf(Directory.GetCurrentDirectory(), Report));

            try
            {
                Body(sb);
            }
            catch (System.Exception e)
            {
                sb.AppendLine();
                sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
                sb.AppendLine(e.StackTrace);
                Debug.LogError("[Echoes] Beat 3 setup threw: " + e);
            }
            finally
            {
                Debug.Log("[Echoes] Beat 3 setup\n" + sb);
                File.WriteAllText(PathOf(Directory.GetCurrentDirectory(), Report), sb.ToString());
            }
        }

        static void Body(StringBuilder sb)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.isLoaded) { sb.AppendLine("No scene is open."); return; }

            Physics.SyncTransforms();

            // --- 1. the cast must be placed before the beat can reference it ---
            var monoGo = GameObject.Find("Mono");
            if (monoGo == null)
            {
                sb.AppendLine("NO Mono in the scene. Run Tools/Echoes/Place Level 1 " +
                              "Cast first — Beat 3 has nothing to attach to.");
                return;
            }

            // Added if absent, not reported as a reason to stop.
            //
            // The cast tool builds Mono out of a model, an animator and a
            // capsule. It does not know what behaviour the character has, so
            // the brain arrives with the beat that needs it — which is the
            // right way round, since MonoCompanion is meaningless before the
            // tree that wakes him exists. Bailing out here made the beat
            // unbuildable from a correctly placed scene.
            var mono = monoGo.GetComponent<MonoCompanion>();
            if (mono == null)
            {
                mono = monoGo.AddComponent<MonoCompanion>();
                sb.AppendLine("added MonoCompanion to Mono — the cast tool places " +
                              "a body, the beat gives it a mind");
            }

            var ariGo = GameObject.Find("Ari");
            if (ariGo == null) { sb.AppendLine("No Ari in the scene."); return; }

            var ariPos = ariGo.transform.position;
            var monoPos = monoGo.transform.position;

            sb.AppendLine("Ari  at " + ariPos.ToString("F2"));
            sb.AppendLine("Mono at " + monoPos.ToString("F2") + ", " +
                          Vector3.Distance(Flat(monoPos), Flat(ariPos)).ToString("0.0") +
                          " m from her");
            sb.AppendLine();

            // --- 2. the line list ---
            var lines = BuildLines(sb);

            // --- 3. the sleeping tree, beside Mono ---
            var tree = BuildTree(sb, container: Container(sb), monoPos: monoPos, mono: mono);

            // --- 4. the gully ---
            var chaseGo = GetOrAdd("MonoChase", Container(sb));
            var chase = chaseGo.GetComponent<MonoChase>();
            if (chase == null) chase = chaseGo.AddComponent<MonoChase>();

            BuildRoute(sb, chaseGo.transform, start: monoPos);

            // --- 5. wiring ---
            Wire(sb, mono, monoGo, tree, chase, ariGo);

            // --- 6. what Beat 3 will actually do, verified ---
            Verify(sb, tree, chase, mono, ariGo);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // --- the lines ------------------------------------------------------------

        /// <summary>
        /// Write Mono's dialogue. Rewritten every run rather than edited in
        /// place, because this is the script of record for the beat and a
        /// half-edited asset left over from an earlier run is worse than one
        /// that is definitely current.
        ///
        /// The ladder at the bottom is Beat 6's, not Beat 3's, and it is here
        /// because ids have to exist before a beat can fire them. Beat 6 will
        /// un-mute them when it is built.
        /// </summary>
        static MonoHintLines BuildLines(StringBuilder sb)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MonoHintLines>(LinesPath);
            bool isNew = false;

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MonoHintLines>();
                isNew = true;
            }

            var l = new List<MonoHintLines.Line>();

            void Beat(string id, string text, bool muted = false) =>
                l.Add(new MonoHintLines.Line
                {
                    id = id, text = text, kind = MonoHintLines.LineKind.Beat, muted = muted
                });

            void Rung(int n, string text) =>
                l.Add(new MonoHintLines.Line
                {
                    id = "beat6.rung" + n, text = text, rung = n,
                    kind = MonoHintLines.LineKind.HintRung
                });

            void Ambient(string id, string text, float gap = 25f) =>
                l.Add(new MonoHintLines.Line
                {
                    id = id, text = text, kind = MonoHintLines.LineKind.Ambient,
                    minGapSeconds = gap
                });

            // --- Beat 3 ---
            Beat("beat3.wake",
                 "Light. I felt it on the bark, and I thought I was dreaming again. " +
                 "Hello. I'm Mono. I'm not going to leave, if that's all right.");

            Beat("beat3.follow",
                 "Keep going. The gully is longer than it looks, and it goes " +
                 "somewhere. It always goes somewhere. That is the good news.");

            Beat("beat3.leg1",
                 "You paint. I have never seen anyone paint before. I only " +
                 "remember the colour, not the doing of it.");

            Beat("beat3.leg2",
                 "Careful here. The stones are loose. I have been asleep a long " +
                 "time, but I remember this path by my knees.");

            Beat("beat3.leg3",
                 "There. Through. Was that hard? This part is not supposed to be " +
                 "hard. This part is always not hard.");

            Beat("beat3.arrive",
                 "This is as far as I go on my own for now. The rest is yours. " +
                 "I'll be right behind you. I always am.");

            // --- Beat 6, the fountain's three rungs. Muted until Beat 6 exists,
            //     so the ids are real and Beat 6 does not ship with silence. ---
            Rung(1, "The rim is marked. Those chevrons are the water's own way of " +
                    "running — every one of them points downhill.");
            Rung(2, "Start at the top. The highest notch on the rim. From there the " +
                    "water could only ever go one way.");
            Rung(3, "Highest notch, then follow the chevrons down, one at a time. " +
                    "Four notches. The fifth will disagree with the water — and " +
                    "that disagreement is your answer. It is the decoy.");

            // --- ambient, for the rest of the level ---
            Ambient("amb.grey", "Everything here is grey. It wasn't always. I could " +
                                "tell you what colour it was, but I'd only be " +
                                "guessing, and I'd rather you looked.", 30f);
            Ambient("amb.sky", "You could paint the sky, you know. I don't think " +
                               "anyone has tried the sky.", 55f);
            Ambient("amb.quiet", "If you're tired, say so. I'll keep quiet. I'm very " +
                                 "good at keeping quiet.", 70f);
            Ambient("amb.fountain", "There's a fountain a little further on. It used " +
                                    "to make a noise. I'd forgotten that noises " +
                                    "could do that.", 45f);

            asset.lines = l;

            if (isNew)
            {
                if (!Directory.Exists("Assets/Painterly"))
                    Directory.CreateDirectory("Assets/Painterly");
                AssetDatabase.CreateAsset(asset, LinesPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }

            asset.Tally(out int b, out int r, out int a, out int m);
            sb.AppendLine("Mono's lines: " + l.Count + " total — " + b + " beat, " +
                          r + " hint rungs, " + a + " ambient" +
                          (m > 0 ? ", " + m + " muted" : "") + (isNew ? " (new asset)" : ""));
            sb.AppendLine("  at " + LinesPath);
            sb.AppendLine("  beat ids: " + string.Join(", ", Ids(l, MonoHintLines.LineKind.Beat)));
            return asset;
        }

        static IEnumerable<string> Ids(List<MonoHintLines.Line> l, MonoHintLines.LineKind k)
        {
            foreach (var line in l) if (line.kind == k) yield return line.id;
        }

        // --- the tree -------------------------------------------------------------

        /// <summary>
        /// The sleeping tree Mono is inside, placed beside him.
        ///
        /// Reuses a tree or stump already in the village if one can be found by
        /// name, because a kit prop is a better tree than a box, and a box
        /// standing in a village called the Grey Village is the sort of thing
        /// that is noticed immediately and never forgiven. If nothing matches,
        /// it says so and leaves the trigger point on an empty marker, so the
        /// beat is buildable and the missing art is one line in the report
        /// rather than an absence.
        /// </summary>
        static SleepingTree BuildTree(StringBuilder sb, GameObject container,
                                     Vector3 monoPos, MonoCompanion mono)
        {
            var go = new GameObject("SleepingTree");
            go.transform.SetParent(container.transform, false);

            // Behind and to one side of Mono, in world terms, and never closer
            // than his own body: they are the same creature's two halves.
            var at = monoPos + new Vector3(-0.9f, 0f, -1.1f);
            go.transform.position = at;

            var tree = go.AddComponent<SleepingTree>();

            var source = FindVillageTree(monoPos, out string sourceName);
            if (source != null)
            {
                var clone = (GameObject)UnityEngine.Object.Instantiate(source);
                clone.name = "SleepingTree_Art";
                clone.transform.SetParent(go.transform, false);
                clone.transform.localPosition = Vector3.zero;

                foreach (var c in clone.GetComponentsInChildren<Collider>())
                    DestroyImmediateCompat(c);

                // Reuse the source's colliders as well as its meshes — the
                // tree has to be something the ray can hit, or the player can
                // never paint it and the beat is unclearable.
                foreach (var c in source.GetComponentsInChildren<Collider>())
                {
                    var mine = clone.AddComponent(c.GetType());
                    UnityEditor.EditorUtility.CopySerialized(c, mine);

                    // CopySerialized brings the values across but not the
                    // placement, and a collider left at the origin would be
                    // floating in the middle of the square. The transform on a
                    // Component is read only, so the local pose is copied a
                    // piece at a time.
                    mine.transform.localPosition = c.transform.localPosition;
                    mine.transform.localRotation = c.transform.localRotation;
                    mine.transform.localScale = c.transform.localScale;
                }

                sb.AppendLine("SleepingTree: reusing village prop '" + sourceName + "'");
            }
            else
            {
                // No tree in the village, which was measured rather than
                // assumed — the scene contains one name ending in "stump" and it
                // is the fountain's statue plinth, which is Beat 6's puzzle.
                //
                // A placeholder is better than leaving the beat unbuilt, for a
                // reason specific to this level: the world is grey and
                // untextured, so a grey blocky stump is not out of place in it,
                // and it is about to be painted anyway. What it cannot do is
                // look like a tree. That is art debt with a name on it, rather
                // than an absence nobody can see.
                BuildStumpArt(sb, go.transform, at);
                sb.AppendLine("SleepingTree: PLACEHOLDER art from primitives, not a " +
                              "tree. Wants replacing with a real dead tree before it " +
                              "ships — but it is grey, it is paintable, and it makes " +
                              "the beat testable today.");
            }

            tree.Resolve();
            sb.AppendLine("SleepingTree: at " + tree.TouchPoint.ToString("F2") +
                          ", " + Vector3.Distance(Flat(tree.TouchPoint),
                                                  Flat(monoPos)).ToString("0.00") +
                          " m from Mono, burst radius " + tree.BurstRadius.ToString("0.0") +
                          " m, touch distance " + tree.TouchDistance.ToString("0.0") + " m");
            return tree;
        }

        static readonly string[] TreeWords =
            { "tree", "trunk", "stump", "log", "deadwood", "root" };

        /// <summary>
        /// Words that disqualify a prop even when it matches a tree word.
        ///
        /// The village contains exactly one thing whose name ends in "stump" and
        /// it is the fountain's statue plinth — which is Beat 6's puzzle piece
        /// and is meant to be moved by the player. A name search that picks it
        /// up would clone Beat 6's answer into the middle of the square, and the
        /// level would end up with two puzzles depending on one another.
        /// </summary>
        static readonly string[] NotATree =
            { "statue", "fountain", "debris", "chevron", "fragment", "rubble", "marker" };

        /// <summary>
        /// The closest village prop that looks like a tree. The search radius is
        /// generous because a tree is a piece of the village's dressing and the
        /// one that matters is the one that happens to be near Mono, not the one
        /// that happens to be named best.
        /// </summary>
        static GameObject FindVillageTree(Vector3 near, out string name)
        {
            name = "";
            GameObject best = null;
            float bestD = float.MaxValue;

            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>())
            {
                // Anything under the cast or beat containers is already part of
                // this beat, or will be, and re-cloning Mono into a tree is not
                // a thing to discover at run time.
                if (go.transform.root.name == "L1_Cast" ||
                    go.transform.root.name == ContainerName) continue;

                var n = go.name.ToLowerInvariant();

                bool match = false;
                foreach (var w in TreeWords)
                    if (n.Contains(w)) { match = true; break; }
                if (!match) continue;

                bool disqualified = false;
                foreach (var w in NotATree)
                    if (n.Contains(w)) { disqualified = true; break; }
                if (disqualified) continue;

                var r = go.GetComponentInChildren<Renderer>();
                if (r == null) continue;

                float d = Vector3.Distance(Flat(go.transform.position), Flat(near));
                if (d < bestD) { bestD = d; best = go; }
            }

            if (best != null) name = best.name;
            return best;
        }

        // --- the gully ------------------------------------------------------------

        /// <summary>
        /// Find a genuinely narrow route out of Mono's spot and measure it.
        ///
        /// Sixteen directions, each tried at four lengths. Candidates that a
        /// body the size of Ari's cannot walk are discarded — this is not a
        /// shortcut through a wall, it is somewhere the player goes — and of
        /// the rest the narrowest is taken, because narrow is the requirement
        /// and short is only a preference.
        /// </summary>
        static void BuildRoute(StringBuilder sb, Transform parent, Vector3 start)
        {
            var route = new List<MonoChase.Leg>();
            string[] legLines = { "beat3.leg1", "beat3.leg2", "beat3.leg3" };

            Vector3 bestEnd = Vector3.zero;
            float bestScore = float.MaxValue;
            float bestMedian = 0f;
            bool found = false;

            for (int d = 0; d < 16 && !found; d++)
            {
                float a = d / 16f * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));

                for (int lenIndex = 0; lenIndex < 4; lenIndex++)
                {
                    float len = 16f + lenIndex * 5f;
                    if (!Walkable(start, start + dir * len, out float median, out float max))
                        continue;

                    // Prefer routes that stay in a gully, and among those the
                    // short ones. A long route that opens out into the square
                    // halfway along is not a gully for the whole of it.
                    float score = median + len * 0.05f;
                    if (score >= bestScore) continue;

                    bestScore = score;
                    bestMedian = median;
                    bestEnd = start + dir * len;
                    found = true;
                }
            }

            var waypoints = new GameObject("Gully");
            waypoints.transform.SetParent(parent, false);

            if (!found)
            {
                // Honest fallback: a straight line in the direction that was
                // least obstructed, flagged, with its measurements printed. A
                // route that is wrong but visible beats a missing beat.
                var dir = LeastObstructed(start);
                bestEnd = start + dir * 18f;
                sb.AppendLine("Gully: no route scored as narrow. Falling back to the " +
                              "least obstructed direction, MEASURED BELOW — treat " +
                              "the chase as unbuilt until a real gully exists.");
            }

            // The return value was being dropped here, which turned a failure
            // into a measurement. Walkable leaves median and max at zero when it
            // gives up, and zero was then printed as "free width median 0.0 m" —
            // a number that reads like a measurement and means "this route is
            // blocked somewhere along the way, and here is how far it got before
            // it found out". The fallback path deliberately builds a route that
            // is known to be unverified, so this is precisely the line where the
            // report has to admit that.
            bool endWalkable = Walkable(start, bestEnd, out float med, out float mx);

            sb.AppendLine("Gully: " + start.ToString("F1") + " -> " + bestEnd.ToString("F1") +
                          ", " + Vector3.Distance(Flat(start), Flat(bestEnd)).ToString("0.0") +
                          " m");

            if (endWalkable)
            {
                sb.AppendLine("  free width median " + med.ToString("0.0") +
                              " m, widest " + mx.ToString("0.0") + " m");
            }
            else
            {
                sb.AppendLine("  NOT WALKABLE end to end. " + _reason +
                              " at " + _blockedAt.ToString("F1") +
                              ", after " +
                              Vector3.Distance(Flat(start), Flat(_blockedAt))
                                  .ToString("0.0") + " m of the " +
                              Vector3.Distance(Flat(start), Flat(bestEnd)).ToString("0.0") +
                              " m planned.");
                sb.AppendLine("  The width below is where the search gave up, not the " +
                              "width of the gully.");
            }

            if (med > GullyMaxWidth)
                sb.AppendLine("  WARNING: median free width is over " +
                              GullyMaxWidth.ToString("0.0") +
                              " m. This is a lane between two buildings, not a " +
                              "gully. The follow camera will pull IN and the beat " +
                              "will not read. Needs geometry, not a setting.");
            if (med < GullyMinWidth)
                sb.AppendLine("  WARNING: median free width is under " +
                              GullyMinWidth.ToString("0.0") +
                              " m. Too tight to frame two characters, and possibly " +
                              "too tight for Ari's 0.30 m capsule to pass without " +
                              "catching on both walls at once.");

            // Three corners, at a third, two thirds and the end. Not a corner
            // every 2 m: the camera pulls back as the gap grows, and a route
            // with a marker every 2 m is a route the camera never gets to use.
            for (int i = 1; i <= 3; i++)
            {
                float t = i / 3f;
                var at = Vector3.Lerp(start, bestEnd, t);

                var leg = new GameObject("GullyLeg" + i);
                leg.transform.SetParent(waypoints.transform, false);

                // Sit the marker on the ground, not in the air at the pivot
                // height — the camera frames this point and a point floating a
                // metre up pitches the whole shot at the sky.
                leg.transform.position = GroundAt(at);

                route.Add(new MonoChase.Leg
                {
                    point = leg.transform,
                    triggerRadius = 3.0f,
                    lineId = i <= legLines.Length ? legLines[i - 1] : ""
                });
            }

            var chase = GameObject.Find("MonoChase");
            if (chase != null)
            {
                var c = chase.GetComponent<MonoChase>();
                if (c != null) c.SetLegs(route);
            }
            else
            {
                sb.AppendLine("  NOTE: no MonoChase in the scene yet, so the route " +
                              "was measured but not assigned.");
            }
        }

        /// <summary>
        /// Is there a walkable line, and how wide is it?
        ///
        /// Width by two horizontal rays, one each way, at chest height. Chest
        /// and not knee, because a kerb is 15 cm and a doorway is 90 and the
        /// difference matters: measuring at the knee reports every alley in the
        /// village as a metre wide.
        /// </summary>
        static bool Walkable(Vector3 a, Vector3 b, out float median, out float max)
        {
            const int samples = 21;
            var widths = new List<float>(samples);

            // Seeded, because the early exits below are legitimate answers —
            // a passage that is not walkable has no width worth reporting, and
            // leaving an out parameter unassigned on that path is a compile
            // error rather than a default.
            median = 0f;
            max = 0f;

            _reason = "";
            _blockedAt = a;

            for (int i = 0; i < samples; i++)
            {
                var at = Vector3.Lerp(a, b, i / (float)(samples - 1));
                float gy = GroundAt(at).y;

                float left, right;
                if (!FreeWidth(at, gy, out left, out right))
                {
                    // Saying where, not just that it failed. "Not walkable" with
                    // no position leaves the reader with nothing to act on, and
                    // the caller has no way to tell a wall across the path from a
                    // hole in the ground at the first sample.
                    _reason = "no floor or no wall to measure against (a ray left " +
                             "the village, or the ground is missing)";
                    _blockedAt = at;
                    return false;
                }

                float width = left + right;

                // Ari's capsule is 0.60 m across and her sweep stops her on
                // contact with both walls at once, so anything under 0.9 m is
                // not a gully she can run down — it is a gap she wedges into.
                if (width < MinWalkableWidth)
                {
                    // The width that failed is kept, and put in the out
                    // parameters. Reporting 0.0 here is the bug this replaces:
                    // zero is not what was measured, and a reader comparing
                    // "median 0.0 m" against the 0.9 m minimum learns nothing
                    // about how near the miss was. The value belongs to the
                    // narrowest sample seen, which is the useful number — it
                    // says 0.7 against a 0.9 requirement, and that is a
                    // different problem from 0.2.
                    _reason = "free width " + width.ToString("0.00") +
                             " m is under the " + MinWalkableWidth.ToString("0.00") +
                             " m minimum at sample " + i + " of " + samples +
                             " (clear " + left.ToString("0.00") + " m left, " +
                             right.ToString("0.00") + " m right)";
                    _blockedAt = at;
                    median = width;
                    max = width;
                    return false;
                }

                widths.Add(width);
            }

            widths.Sort();
            median = widths[widths.Count / 2];
            max = widths[widths.Count - 1];
            return true;
        }

        static bool FreeWidth(Vector3 at, float groundY, out float left, out float right)
        {
            // 6 m each way is as far as this needs to look: a passage wider
            // than 12 m is the square, not a gully, and a ray that finds
            // nothing is a ray that has left the village.
            const float reach = 6f;
            var from = new Vector3(at.x, groundY + 1.1f, at.z);

            var dir = new Vector3(-at.z, 0f, at.x).normalized;   // any perpendicular

            left = RayReach(from, dir, reach);
            right = RayReach(from, -dir, reach);
            return left < reach || right < reach;
        }

        static float RayReach(Vector3 from, Vector3 dir, float reach)
        {
            return Physics.Raycast(from, dir, out RaycastHit hit, reach, ~0,
                                   QueryTriggerInteraction.Ignore)
                ? hit.distance
                : reach;
        }

        static Vector3 LeastObstructed(Vector3 from)
        {
            Vector3 best = Vector3.forward;
            float bestBlocked = float.MaxValue;

            for (int d = 0; d < 16; d++)
            {
                float a = d / 16f * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float blocked = 0f;

                for (int i = 1; i <= 6; i++)
                {
                    var at = from + dir * (i * 3f);
                    if (GroundAt(at).y < -900f) { blocked++; continue; }

                    // OverlapCapsuleNonAlloc: results in fourth, mask fifth,
                    // trigger policy sixth. OverlapCapsule itself allocates and
                    // cannot be given a buffer at all — see PhysicsApiProbe.
                    if (Physics.OverlapCapsuleNonAlloc(
                            at + Vector3.up * 0.5f, at + Vector3.up * 1.3f, 0.30f,
                            OverlapBuffer, ~0, QueryTriggerInteraction.Ignore) > 0)
                        blocked++;
                }

                if (blocked < bestBlocked) { bestBlocked = blocked; best = dir; }
            }

            return best;
        }

        static readonly Collider[] OverlapBuffer = new Collider[8];

        static Vector3 GroundAt(Vector3 at)
        {
            if (Physics.Raycast(at + Vector3.up * 6f, Vector3.down, out var hit, 12f, ~0,
                                QueryTriggerInteraction.Ignore))
                return hit.point;
            return new Vector3(at.x, -1000f, at.z);
        }

        // --- wiring ---------------------------------------------------------------

        static void Wire(StringBuilder sb, MonoCompanion mono, GameObject monoGo,
                         SleepingTree tree, MonoChase chase, GameObject ariGo)
        {
            var lines = AssetDatabase.LoadAssetAtPath<MonoHintLines>(LinesPath);

            // SerializedObject, not the C# fields: `lines` and `ari` are
            // [SerializeField] private, so there is no public way to set them
            // and a tool that assigns them anyway would compile against
            // internals that a later refactor is free to change.
            var so = new SerializedObject(mono);
            so.FindProperty("lines").objectReferenceValue = lines;
            so.FindProperty("ari").objectReferenceValue = ariGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var tso = new SerializedObject(tree);
            tso.FindProperty("mono").objectReferenceValue = mono;
            tso.FindProperty("chase").objectReferenceValue = chase;
            tso.ApplyModifiedPropertiesWithoutUndo();

            var cso = new SerializedObject(chase);
            cso.FindProperty("mono").objectReferenceValue = mono;

            // The camera, by whichever of the two routes has it.
            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.GetComponent<AriFollowCamera>();
                if (follow == null) follow = Object.FindAnyObjectByType<AriFollowCamera>();
                if (follow != null) cso.FindProperty("followCamera").objectReferenceValue = follow;
                else sb.AppendLine("Wiring: no AriFollowCamera found. The chase will " +
                                   "run but the camera will not be guided.");
            }
            cso.ApplyModifiedPropertiesWithoutUndo();

            // The HUD. On Ari, so it travels with the player and needs no
            // lookup at runtime.
            if (ariGo.GetComponent<BeatHud>() == null) ariGo.AddComponent<BeatHud>();
            else sb.AppendLine("Ari already has a BeatHud — left alone.");

            // Asleep, and therefore hidden.
            //
            // MonoCompanion's _awake flag stops him moving; it does not stop him
            // being seen. Left active, he stands in the middle of the village
            // from the first frame, and a companion the player can walk past
            // before the beat that introduces them is the exact failure Beat 3
            // exists to prevent — the player meets him, says nothing to him, and
            // the tree is then a puzzle with no reason attached.
            //
            // Deactivated here, not in Awake, so the editor viewport shows the
            // level as the player will meet it rather than as the level designer
            // will.
            if (monoGo.activeSelf)
            {
                monoGo.SetActive(false);
                sb.AppendLine("Mono: deactivated — asleep and invisible until the " +
                              "tree's brush stroke wakes him");
            }
            else
            {
                sb.AppendLine("Mono: already deactivated, left alone");
            }

            EditorUtility.SetDirty(mono);
            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(chase);

            sb.AppendLine("Wiring: Mono.lines = " + (lines == null ? "NOTHING" : lines.name) +
                          ", Mono.ari = " + ariGo.name);
            sb.AppendLine("        SleepingTree.mono = Mono, .chase = MonoChase, " +
                          "BeatHud on Ari");
        }

        // --- verification ---------------------------------------------------------

        /// <summary>
        /// The beat, checked the way the rest of this project is checked.
        ///
        /// Not "did the tool run" — every line above can be true and the beat
        /// still unplayable, because the things that actually matter are all
        /// about reachability: can Ari walk to the tree, is the tree somewhere
        /// the camera can see, does a stroke from where she stands reach it at
        /// all. Those are measurable and all three have been wrong before.
        /// </summary>
        static void Verify(StringBuilder sb, SleepingTree tree, MonoChase chase,
                           MonoCompanion mono, GameObject ariGo)
        {
            sb.AppendLine();
            sb.AppendLine("--- can the beat actually be played? ---");

            var ari = ariGo.GetComponent<AriMover>();
            var point = tree.TouchPoint;

            // Is the tree somewhere she can stand and swing?
            float walkable = 0f;
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                var at = new Vector3(point.x + Mathf.Cos(a) * 1.6f, 0f,
                                     point.z + Mathf.Sin(a) * 1.6f);
                var g = GroundAt(at);
                if (g.y > -900f &&
                    Physics.OverlapCapsuleNonAlloc(
                        g + Vector3.up * 0.40f, g + Vector3.up * 1.50f, 0.30f,
                        OverlapBuffer, ~0, QueryTriggerInteraction.Ignore) == 0)
                    walkable += 1f / 8f;
            }

            sb.AppendLine("ground she can stand on within 1.6 m of the tree: " +
                          (walkable * 100f).ToString("0") + "% (a body of " +
                          "Ari's size, 8 directions)");
            if (walkable < 0.5f)
                sb.AppendLine("  BAD: she cannot get next to it. The stroke test " +
                              "needs her inside " + tree.TouchDistance.ToString("0.0") +
                              " m, and she cannot stand there.");

            // Can she see it from the square, i.e. is it not inside a house?
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 eye = cam.transform.position + cam.transform.forward;
                bool clear = !Physics.Linecast(eye, point, ~0, QueryTriggerInteraction.Ignore);
                if (!clear)
                {
                    // Raycast hands the hit back through the out parameter and
                    // returns only whether it connected, so the name `hit` on
                    // the bool would be a lie the compiler is right to reject.
                    Physics.Raycast(eye, (point - eye).normalized,
                                    out RaycastHit h, Vector3.Distance(eye, point),
                                    ~0, QueryTriggerInteraction.Ignore);
                    sb.AppendLine("from Camera.main the tree is behind '" +
                                  (h.collider == null ? "?" : h.collider.name) + "'");
                }
                else
                {
                    sb.AppendLine("from Camera.main the tree is in clear view " +
                                  "(she will have to walk to it)");
                }
            }

            // Does the beat have all its pieces?
            var missing = new List<string>();
            if (mono == null) missing.Add("MonoCompanion");

            // Awake and visible are different failures and produce the same
            // symptom — a player walks past a character who should not be there
            // yet — so both are named.
            if (mono != null && mono.gameObject.activeSelf)
                missing.Add("Mono is still ACTIVE in the scene — he will be standing " +
                            "in the village before the tree wakes him");

            if (mono != null && mono.GetComponent<Animator>() != null &&
                mono.GetComponent<Animator>().runtimeAnimatorController == null)
                missing.Add("Mono's Animator has no controller");
            if (tree == null) missing.Add("SleepingTree");
            if (chase == null) missing.Add("MonoChase");
            if (ariGo.GetComponent<BeatHud>() == null) missing.Add("BeatHud on Ari");
            if (ariGo.GetComponent<AriMover>() == null) missing.Add("AriMover on Ari");

            var brush = ariGo.GetComponentInChildren<BrushPainter>();
            if (brush == null && (Camera.main == null ||
                                  Camera.main.GetComponent<BrushPainter>() == null))
                missing.Add("BrushPainter — the stroke that wakes the tree has no brush");

            if (missing.Count == 0)
            {
                sb.AppendLine("every piece is wired. Keys: WASD walk, Shift run, " +
                              "Space jump, LMB swing the brush at the tree.");
            }
            else
            {
                sb.AppendLine("MISSING:");
                foreach (var m in missing) sb.AppendLine("  - " + m);
            }

            // The route, and whether the chase can end.
            sb.AppendLine("chase: " + chase.PhaseName + ", " + chase.LegsTotal +
                          " leg(s), next corner " + chase.DistanceToNextLeg.ToString("0.0") + " m");

            if (ari == null)
                sb.AppendLine("NOTE: no AriMover, so the tree's reach test cannot run");
        }

        // --- small helpers --------------------------------------------------------

        /// <summary>
        /// A dead stump out of Unity primitives, in the project's own shader.
        ///
        /// The shader is the part that is not optional. Colour comes back
        /// through a per-renderer MaterialPropertyBlock carrying `_ColorRestore`,
        /// and a material property that the shader does not declare is dropped
        /// silently — so a stump left on Unity's default Lit material would
        /// accept the brush stroke, wake Mono, restore every target in range,
        /// and then stay grey. The beat would appear to work and the one thing
        /// the player is meant to see would not happen.
        ///
        /// Colliders are kept. The stroke test needs a ray from the camera to
        /// land on the trunk, and a renderer with no collider is a stump the
        /// player cannot paint by aiming at it — though the touch-distance
        /// fallback in SleepingTree would still catch them standing beside it,
        /// which is a worse way to find out.
        /// </summary>
        static void BuildStumpArt(StringBuilder sb, Transform parent, Vector3 at)
        {
            var mat = StumpMaterial(sb);

            var root = new GameObject("SleepingTree_Art");
            root.transform.SetParent(parent, false);
            root.transform.position = at;

            // A Unity cylinder is 2 m tall and 1 m across at scale 1, so a
            // wanted diameter goes straight into x and z and a wanted height is
            // halved into y. Getting that wrong gives a stump the size of a
            // house, which is what the last scale factor that was guessed at
            // instead of measured would have produced.
            StumpPart(sb, root.transform, "Stump_Flare", PrimitiveType.Cylinder,
                      new Vector3(0f, 0.13f, 0f), new Vector3(0.68f, 0.13f, 0.68f),
                      Quaternion.identity, mat);

            StumpPart(sb, root.transform, "Stump_Trunk", PrimitiveType.Cylinder,
                      new Vector3(0f, 0.72f, 0f), new Vector3(0.46f, 0.74f, 0.46f),
                      Quaternion.identity, mat);

            // Two snapped-off limbs, so the silhouette is not a post.
            StumpPart(sb, root.transform, "Stump_LimbA", PrimitiveType.Cylinder,
                      new Vector3(0.26f, 1.08f, 0.04f), new Vector3(0.15f, 0.26f, 0.15f),
                      Quaternion.Euler(0f, 0f, -64f), mat);

            StumpPart(sb, root.transform, "Stump_LimbB", PrimitiveType.Cylinder,
                      new Vector3(-0.20f, 0.90f, -0.16f), new Vector3(0.12f, 0.20f, 0.12f),
                      Quaternion.Euler(26f, 40f, 52f), mat);

            // One target on the root, which collects every child renderer. A
            // target per part would work too and would be four times the
            // property blocks for the same visual result.
            var crt = root.AddComponent<ColorRestoreTarget>();
            crt.SetRestoreImmediate(0f);

            int parts = root.GetComponentsInChildren<Renderer>().Length;
            float top = 0f;
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                top = Mathf.Max(top, r.bounds.max.y - at.y);

            sb.AppendLine("  built from " + parts + " primitives, " +
                          top.ToString("0.00") + " m tall, material " +
                          (mat == null ? "NONE — the stump will NOT change colour " +
                                        "when painted" : "'" + mat.name + "' (Echoes/PainterlyLit)"));
            sb.AppendLine("  one ColorRestoreTarget on the root, covering all " + parts +
                          " renderers; it will be coloured by the burst, and the " +
                          "colours it already has are grey because the burst is " +
                          "what brings them back");
        }

        static void StumpPart(StringBuilder sb, Transform parent, string name,
                              PrimitiveType type, Vector3 localPos, Vector3 localScale,
                              Quaternion localRot, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;

            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;

            // CreatePrimitive also leaves a MeshCollider. Kept on purpose — see
            // the note above — but the default one is a convex capsule around a
            // cylinder, which is a slightly wrong shape to aim at and cheap to
            // make right by leaving it alone. Nothing here needs changing.
        }

        const string StumpMatPath = "Assets/Painterly/Materials/Beat3_Stump.mat";

        static Material StumpMaterial(StringBuilder sb)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(StumpMatPath);
            if (mat != null) return mat;

            var shader = Shader.Find("Echoes/PainterlyLit");
            if (shader == null)
            {
                sb.AppendLine("  shader 'Echoes/PainterlyLit' NOT FOUND. Using Unity's " +
                              "default material, which has no _ColorRestore, so the " +
                              "stump will stay grey through the whole of Beat 3.");
                return null;
            }

            if (!Directory.Exists("Assets/Painterly/Materials"))
                Directory.CreateDirectory("Assets/Painterly/Materials");

            mat = new Material(shader) { name = "Beat3_Stump" };

            // Dead wood, and grey. The albedo is what lerps towards full colour
            // when _ColorRestore reaches 1, so it is a desaturated brown rather
            // than a neutral: fully grey would give a painted stump with no
            // colour in it, which is a subtle and thoroughly confusing bug.
            var wood = new Color(0.40f, 0.37f, 0.33f);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", wood);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", wood);

            AssetDatabase.CreateAsset(mat, StumpMatPath);
            sb.AppendLine("  created " + StumpMatPath);
            return mat;
        }

        static GameObject Container(StringBuilder sb)
        {
            var go = GameObject.Find(ContainerName);
            if (go != null) return go;

            go = new GameObject(ContainerName);
            sb.AppendLine("created container '" + ContainerName + "'");
            return go;
        }

        static GameObject GetOrAdd(string componentName, GameObject parent)
        {
            var go = new GameObject(componentName);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static void DestroyImmediateCompat(Object o) => Object.DestroyImmediate(o);

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        static string PathOf(string dir, string file) => System.IO.Path.Combine(dir, file);
    }
}
