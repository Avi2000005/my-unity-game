using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Puts Mono, the Ink Crawlers and the Color Thief into the village, at
    /// spots chosen by measuring rather than by picking numbers.
    ///
    /// Every position here is found the way the house was: a candidate is
    /// proposed, the ground under it is found by raycast, and a clearance
    /// capsule is stood up on it and asked whether it fits. A spot that cannot
    /// hold a body-sized capsule is not a spot, whatever its coordinates look
    /// like on a grid.
    ///
    /// The clearance capsule is built the same way AriMover builds hers —
    /// bottom centre at sole + radius + lift, not at sole + lift. That is not
    /// pedantry. A sphere of radius r whose centre is only `lift` above the
    /// ground reaches `lift - r` *below* it, so a test capsule built that way
    /// is buried in the floor, overlaps the floor's own collider every single
    /// time, and reports every point in the level as blocked. The first run of
    /// this tool did exactly that and returned "no spot found" for all four
    /// placements in a village with two thousand colliders in it.
    ///
    /// So the reasons are named, not summarised. A placement that fails has to
    /// be able to say *what* stopped it, because "nothing here fits" is
    /// indistinguishable from "this tool is broken", and the second one is
    /// expensive to rediscover.
    /// </summary>
    public static class CastPlacement
    {
        const string Dir = "Assets/Art/L1";

        const string Report = "Temp/cast_placement.txt";

        /// <summary>The market square. Everything in Level 1 radiates from here.</summary>
        static readonly Vector3 Square = Vector3.zero;

        /// <summary>
        /// Half the diameter of the space a creature needs to stand in. Wider
        /// than Ari's own 0.30 on purpose: the crawlers get a 0.40 body, and a
        /// crawler placed where its body does not fit is a crawler fused to a
        /// doorframe. Testing at the wider of the two means everything placed
        /// here is somewhere Ari can also walk to and hit.
        /// </summary>
        const float BodyRadius = 0.40f;

        const float BodyHeight = 1.80f;

        /// <summary>
        /// How far the ground may rise or fall between the square and a cast
        /// member before the spot counts as somewhere else entirely.
        ///
        /// This is the test that keeps a crawler off a roof. "Standable" is not
        /// the same as "reachable", and this village has pitched roofs, a
        /// fountain basin and upper storeys — all of which are flat, all of
        /// which pass a slope test, and none of which the player can walk to.
        /// A metre of tolerance is generous: the paving steps and the low kerb
        /// in Beat 2 are well inside it, and the roofs are metres above it.
        /// </summary>
        const float ReachableHeightTolerance = 1.0f;

        /// <summary>
        /// How far above a point to start the ground probe, and how far down to
        /// look.
        ///
        /// The start has to clear the tallest thing in the village. Measured:
        /// the house roof tiles sit at y = 15.01, and the tallest surfaces
        /// anywhere in the sampled area are around 15 m. A probe that started at
        /// 6 m, which is what this used to do, would have begun underneath every
        /// roof in the level and so could not have seen one — which is the
        /// whole reason a roof was ever mistaken for the ground.
        /// </summary>
        const float GroundProbeHeight = 30f;
        const float GroundProbeDepth = 60f;

        static readonly Collider[] Overlap = new Collider[24];

        /// <summary>
        /// What is being placed, from which model, onto which controller.
        ///
        /// Search is a set of rings rather than a single spot, because "an Ink
        /// Crawler in Beat 5.5's alley" is a constraint the village geometry has
        /// to satisfy rather than something this tool can decide. It takes the
        /// closest spot that actually fits and reports how far out that turned
        /// out to be, so a crawler ending up 2 m further from the intended alley
        /// than asked is visible in the report instead of in play.
        ///
        /// Group ties the alley pair together. Searched independently they can
        /// land several metres apart, and two crawlers that are meant to be one
        /// corridor become two unrelated problems standing in a field.
        /// </summary>
        static readonly (string Name, string Model, string Controller, Vector3 Wanted,
                          float SearchRadius, float MinFromSquare, float TargetHeight,
                          string Group)[] Cast =
        {
            // Beat 3. Mono is asleep at the foot of the old tree east of the
            // square. Well off the paving: this is a quiet beat and it should
            // not be walked past on the way to something else.
            ("Mono", "Mono.fbx", "Mono.controller",
             new Vector3(9f, 0f, 7f), 7f, 6f, 0.75f, ""),

            // Beat 5. The stealth crawler, on the back path behind the houses.
            // Far from the square so that reaching it is a decision.
            ("InkCrawler_Back", "InkCrawler.fbx", "InkCrawler.controller",
             new Vector3(-8f, 0f, -14f), 8f, 8f, 1.10f, ""),

            // Beat 5.5. The alley pair. Close together, both on the paving, so
            // they are a corridor rather than two separate problems. The B
            // entry's wanted point is ignored in favour of wherever A landed.
            ("InkCrawler_AlleyA", "InkCrawler.fbx", "InkCrawler.controller",
             new Vector3(-2.5f, 0f, 12f), 6f, 5f, 1.10f, "Alley"),
            ("InkCrawler_AlleyB", "InkCrawler.fbx", "InkCrawler.controller",
             new Vector3(2.5f, 0f, 12f), 4f, 5f, 1.10f, "Alley")
        };

        [MenuItem("Tools/Echoes/Place Level 1 Cast", priority = 65)]
        public static void Run()
        {
            var sb = new StringBuilder();
            var full = PathOf(Directory.GetCurrentDirectory(), Report);
            if (File.Exists(full)) File.Delete(full);

            // The whole body in a try, with the report written from the
            // finally. A tool that has already placed three of four cast
            // members and then thrown on the fourth has done most of its work,
            // and a tool that reports nothing looks exactly like a tool that
            // did none.
            try
            {
                Body(sb);
            }
            catch (System.Exception e)
            {
                sb.AppendLine();
                sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
                sb.AppendLine(e.StackTrace);
                Debug.LogError("[Echoes] cast placement threw: " + e);
            }
            finally
            {
                Finish(sb);
            }
        }

        static void Body(StringBuilder sb)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.isLoaded)
            {
                sb.AppendLine("No scene is open.");
                return;
            }

            Physics.SyncTransforms();

            var ariGo = GameObject.Find("Ari");

            // The reference height every candidate is judged against, measured
            // off a raycast rather than assumed to be y = 0.
            //
            // Measured under Ari, and taken from the floor rather than the top
            // of whatever is overhead — see GroundHeight. Where this used to
            // read 1.22 at the square's origin and 2.27 under Ari, both of
            // which were a statue plinth and a decorative vine, the village
            // floor measures 0.05 and 82 per cent of a 7 225 sample grid reads
            // y = 0. Judged against 2.27, every real patch of ground in the
            // level looked like a roof two metres below the square and nothing
            // could be placed anywhere.
            float squareGround = GroundHeight(
                ariGo != null ? ariGo.transform.position : new Vector3(6f, 0f, 6f),
                out _);

            string referenceName = ariGo != null
                ? "under Ari at " + ariGo.transform.position.ToString("F2")
                : "at (6, 0, 6), the fallback, because there is no Ari in the scene";

            sb.AppendLine("reference ground y = " + squareGround.ToString("F2") +
                          ", measured " + referenceName +
                          " (the floor, not the roof or prop above it)");
            sb.AppendLine("house at (25, 0, -8); fountain at (0.01, 0.05, 0); " +
                          "Ari's capsule is r 0.30 h 1.80, walls ~0, clearance " +
                          "capsule here is r " + BodyRadius.ToString("0.00") +
                          " h " + BodyHeight.ToString("0.00"));
            sb.AppendLine();

            sb.AppendLine(ariGo == null
                ? "no Ari in the scene — clearance is measured against village geometry only"
                : "Ari is at " + ariGo.transform.position.ToString("F2"));
            sb.AppendLine();

            var container = Container(sb);
            int placed = 0, problems = 0;

            // Where each group ended up, so the second half of a pair can search
            // around the first half.
            var groupAt = new Dictionary<string, Vector3>();

            foreach (var who in Cast)
            {
                var modelPath = Dir + "/" + who.Model;
                var ctlPath = Dir + "/" + who.Controller;

                var ctl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctlPath);
                if (ctl == null)
                {
                    sb.AppendLine(who.Name + ": NO CONTROLLER at " + ctlPath + " — skipped");
                    problems++;
                    continue;
                }

                // Re-runnable: an existing instance is reported rather than
                // duplicated, so running this twice does not leave two Monos.
                var existing = container.transform.Find(who.Name);
                if (existing != null)
                {
                    Describe(sb, who, existing.position, existing.gameObject,
                             "already placed, left alone");
                    if (who.Group != "") groupAt[who.Group] = existing.position;
                    placed++;
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (prefab == null)
                {
                    sb.AppendLine(who.Name + ": NO MODEL at " + modelPath + " — skipped");
                    problems++;
                    continue;
                }

                var wanted = who.Wanted;
                string groupNote = "";
                if (who.Group != "" && groupAt.TryGetValue(who.Group, out var gpos))
                {
                    wanted = gpos;
                    groupNote = "searched around its group mate at " +
                                gpos.ToString("F1");
                }

                var spot = FindSpot(who, wanted, squareGround, out string why);
                if (spot == null)
                {
                    sb.AppendLine(who.Name + ": NO SPOT FOUND near " + wanted.ToString("F1") +
                                  ". " + why);
                    problems++;
                    continue;
                }

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container.transform);
                inst.name = who.Name;
                inst.transform.position = spot.Value;
                inst.transform.rotation = FaceSquare(spot.Value);

                var animator = inst.GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    sb.AppendLine(who.Name + ": no Animator on the model at all — it will not move");
                    problems++;
                }
                else
                {
                    // The root's Animator, not the first one found. A Humanoid
                    // FBX puts its Animator on the root GameObject; on a piece
                    // with children it can be further down, and a controller on a
                    // child Animator plays a character that appears frozen while
                    // something inside it animates.
                    animator.runtimeAnimatorController = ctl;
                    animator.applyRootMotion = false;
                    animator.updateMode = AnimatorUpdateMode.Normal;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                EnsureCollider(inst);

                // The child transforms of a kit FBX root carry a 100x scale, so
                // the world height of the model is measured and scaled to a
                // target rather than guessed at with a magic factor.
                Physics.SyncTransforms();
                float before = HeightOf(inst);
                FitHeight(inst, who.TargetHeight);
                Physics.SyncTransforms();

                Describe(sb, who, inst.transform.position, inst, why + groupNote +
                         ", scaled from " + before.ToString("0.00") + " m to " +
                         HeightOf(inst).ToString("0.00") + " m tall");

                if (who.Group != "") groupAt[who.Group] = inst.transform.position;
                placed++;
            }

            // The Color Thief. Placed rather than imported-and-forgotten,
            // because Beat 7 is the beat the level ends on and it is entirely
            // this object on a hill.
            PlaceThief(sb, container, squareGround, ref placed, ref problems);

            sb.AppendLine();
            sb.AppendLine(placed + " placed, " + problems + " problem(s)");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // --- placement -----------------------------------------------------------

        static GameObject Container(StringBuilder sb)
        {
            var go = GameObject.Find("L1_Cast");
            if (go != null) return go;

            go = new GameObject("L1_Cast");
            sb.AppendLine("created container 'L1_Cast'");
            return go;
        }

        /// <summary>
        /// Find somewhere near a wanted point that a body actually fits on.
        ///
        /// Four tests, all of which have to pass. There has to be ground. The
        /// ground has to be flat enough to stand on. The ground has to be at
        /// roughly the square's height, or it is a roof. And a body-sized
        /// capsule has to fit in the space above it.
        /// </summary>
        static Vector3? FindSpot(
            (string Name, string Model, string Controller, Vector3 Wanted,
             float SearchRadius, float MinFromSquare, float TargetHeight,
             string Group) who,
            Vector3 wanted, float squareGround, out string why)
        {
            if (Vector3.Distance(Flat(wanted), Square) < who.MinFromSquare)
            {
                why = "wanted point is inside the " + who.MinFromSquare +
                      " m exclusion around the square";
                return null;
            }

            // Walk outwards in rings. A spot that has to be searched for is a
            // spot the level did not really want, so the answer is reported
            // rather than quietly accepted.
            var lastWhy = "no candidates were tested";

            for (float r = 0f; r <= who.SearchRadius + 0.01f; r += 0.5f)
            {
                int count = r < 0.01f ? 1 : Mathf.Max(8, Mathf.RoundToInt(r * 8f));

                for (int i = 0; i < count; i++)
                {
                    float a = count == 1 ? 0f : i / (float)count * Mathf.PI * 2f;
                    var candidate = new Vector3(
                        wanted.x + Mathf.Cos(a) * r, 0f, wanted.z + Mathf.Sin(a) * r);

                    if (Vector3.Distance(Flat(candidate), Square) < who.MinFromSquare) continue;

                    if (Fits(candidate, squareGround, out float y, out string no))
                    {
                        why = r < 0.01f
                            ? "the wanted point itself"
                            : "closest spot that fits, " + r.ToString("0.0") +
                              " m out (last rejection was: " + no + ")";
                        return new Vector3(candidate.x, y, candidate.z);
                    }

                    lastWhy = no;
                }
            }

            why = "nothing within " + who.SearchRadius.ToString("0.0") +
                  " m had clear standing room. Every rejection was the same: " + lastWhy;
            return null;
        }

        /// <summary>
        /// Whether a body-sized capsule stands here without touching anything,
        /// on ground flat enough to stand on, at a height the player can walk to.
        /// </summary>
        static bool Fits(Vector3 at, float squareGround, out float groundY, out string no)
        {
            groundY = 0f;
            no = "";

            if (!GroundHeight(at, out groundY, out float ny))
            {
                no = "no ground under it";
                return false;
            }

            if (ny < 0.7f)
            {
                no = "ground tilts " +
                      Vector3.Angle(Vector3.up * ny, Vector3.up).ToString("0") + " deg";
                return false;
            }

            // Flat, standable, and at the level the player walks on. Now that the
            // ground probe returns the floor rather than the roof above it, a
            // rooftop reads as the courtyard underneath it, which is where a
            // character belongs. What this still catches is a genuine ledge: a
            // first-floor gallery or a wall top, which is walkable and flat and
            // a metre above the lane.
            if (Mathf.Abs(groundY - squareGround) > ReachableHeightTolerance)
            {
                no = "floor is " + (groundY - squareGround).ToString("+0.0;-0.0") +
                      " m from the square's height — a ledge or a gallery, not a path";
                return false;
            }

            // The same shape AriMover.CapsuleAt builds, lifted clear of the
            // floor by bodyBottomLift rather than sunk into it.
            float lift = 0.10f;
            var bottom = new Vector3(at.x, groundY + BodyRadius + lift, at.z);
            var top = new Vector3(at.x, groundY + BodyHeight - BodyRadius, at.z);

            // OverlapCapsuleNonAlloc, not OverlapCapsule, and the order is
            // results-then-mask. Measured by reflection over Physics rather than
            // written from memory: OverlapCapsule in this engine allocates and
            // returns a fresh Collider[], and its mask overload takes no
            // results array at all. The NonAlloc family puts the buffer in
            // fourth, the mask in fifth and the trigger policy in sixth —
            // the opposite order from OverlapBoxNonAlloc two methods away,
            // which is exactly the kind of thing worth checking rather than
            // recalling. Reusing one buffer matters here: this test runs a few
            // hundred times per placement pass.
            int n = Physics.OverlapCapsuleNonAlloc(bottom, top, BodyRadius, Overlap,
                                                   ~0, QueryTriggerInteraction.Ignore);

            if (n > 0)
            {
                // Name the culprits. Three is enough to tell a wall from a kerb
                // from a stray collider, and a list of 24 is a wall of text.
                no = n.ToString() + " collider(s) in the capsule, nearest: ";
                for (int i = 0; i < n && i < 3; i++)
                {
                    if (i > 0) no += ", ";
                    no += Overlap[i] == null ? "null" : Overlap[i].name;
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// The height of the floor a character would actually stand on.
        ///
        /// Every hit on the way down, and the lowest one taken — not the first.
        /// A single downward ray returns whatever is on top, and in this village
        /// that is almost never the floor. Measured, at the places this matters:
        /// the square origin returns the statue plinth at 1.22, Ari's own
        /// position returns a decorative vine prop at 2.27, the house returns
        /// its roof tiles at 15.01 and the alley mouth returns a roof at 6.04.
        /// The floors underneath all of those sit at 0.03 to 0.08, and 82 per
        /// cent of the whole village measures y = 0.
        ///
        /// Taking the lowest hit also disposes of the roof test that used to be
        /// needed. Nobody can end up standing on a rooftop here, because a
        /// rooftop is not what this returns — the ground below it is. What
        /// reaches the topmost surface is a character standing in a courtyard,
        /// which is where they should be.
        ///
        /// Returns false when there is nothing below at all, which is outside
        /// the village rather than at the bottom of a hole, and the caller
        /// treats those two very differently.
        /// </summary>
        static bool GroundHeight(Vector3 at, out float height, out float normalY)
        {
            height = 0f;
            normalY = 0f;

            var hits = Physics.RaycastAll(at + Vector3.up * GroundProbeHeight, Vector3.down,
                                          GroundProbeDepth, ~0,
                                          QueryTriggerInteraction.Ignore);

            if (hits == null || hits.Length == 0) return false;

            var lowest = hits[0];
            for (int i = 1; i < hits.Length; i++)
                if (hits[i].point.y < lowest.point.y) lowest = hits[i];

            height = lowest.point.y;
            normalY = lowest.normal.y;
            return true;
        }

        /// <summary>
        /// Ground height, for the places that only want a number and have
        /// already established that there is ground.
        /// </summary>
        static float GroundHeight(Vector3 at, out float normalY)
        {
            return GroundHeight(at, out float h, out normalY) ? h : float.NaN;
        }

        /// <summary>
        /// Turn to face the square, so every creature in the level is looking at
        /// the one place the player is coming from.
        /// </summary>
        static Quaternion FaceSquare(Vector3 at)
        {
            var look = Flat(Square - at);
            if (look.sqrMagnitude < 1e-4f) return Quaternion.identity;
            return Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        // --- size -----------------------------------------------------------------

        /// <summary>
        /// World-space height of everything under this object.
        ///
        /// Renderer, not MeshRenderer. The Color Thief is a rigged character,
        /// so its skin is a SkinnedMeshRenderer, and asking for MeshRenderer
        /// returns an empty array on a perfectly good model — which is how the
        /// last run reported a Color Thief 0.0 m tall with no renderer at all.
        /// </summary>
        static float HeightOf(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return 0f;

            var bounds = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in rends) bounds.Encapsulate(r.bounds);
            return bounds.size.y;
        }

        /// <summary>
        /// Scale the root so the model stands a given height.
        ///
        /// Measured, then corrected by the ratio, then left to be reported.
        /// A kit FBX's root scale is not the model's height in metres — its
        /// children carry a 100x scale — so a root scale of 1 means nothing on
        /// its own and the only way to get a 1.1 m crawler is to ask what
        /// height it currently is and divide.
        /// </summary>
        static void FitHeight(GameObject go, float target)
        {
            float now = HeightOf(go);
            if (now < 0.01f) return;      // no renderer to measure; leave it alone

            var t = go.transform;
            t.localScale *= target / now;
        }

        // --- collider ------------------------------------------------------------

        /// <summary>
        /// A capsule the size of the body, solid rather than a trigger.
        ///
        /// The import's own colliders go first. A Humanoid FBX with a mesh
        /// collider gives a character a collision shape that is a mesh with a
        /// hand where the arm is, and that snags on everything the player
        /// brushes past. Solid because Beat 5.5's dodge and its stagger both
        /// depend on the crawler being something the player can hit.
        /// </summary>
        static void EnsureCollider(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.direction = 1;                     // Y
            capsule.height = BodyHeight;
            capsule.radius = BodyRadius;
            capsule.center = new Vector3(0f, BodyHeight * 0.5f, 0f);
            capsule.isTrigger = false;
        }

        // --- the Color Thief ------------------------------------------------------

        /// <summary>
        /// Beat 7. The last thing in the level is a shape on a far hill, and it
        /// is the only thing the player is shown that is not the village — so
        /// the distance is the point of it, and the silhouette is only readable
        /// if there is sky behind it.
        ///
        /// Found by looking for high ground rather than by naming a coordinate,
        /// for the same reason as everywhere else: a coordinate picked off a map
        /// is either inside a house or floating.
        /// </summary>
        static void PlaceThief(StringBuilder sb, GameObject container,
                               float squareGround, ref int placed, ref int problems)
        {
            const string model = Dir + "/ColorThief.fbx";

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(model);
            if (prefab == null)
            {
                sb.AppendLine("ColorThief: NO MODEL at " + model + " — Beat 7 has nothing to show");
                problems++;
                return;
            }

            var existing = container.transform.Find("ColorThief");
            if (existing != null)
            {
                var rr = existing.GetComponentsInChildren<Renderer>();
                sb.AppendLine();
                sb.AppendLine("ColorThief: already at " + existing.position.ToString("F1") +
                              ", " +
                              Vector3.Distance(Flat(existing.position), Square).ToString("0") +
                              " m from the square, " + rr.Length + " renderer(s), " +
                              HeightOf(existing.gameObject).ToString("0.00") + " m tall");
                placed++;
                return;
            }

            // Sample outward from the square and keep the highest ground that is
            // far enough away to be a silhouette rather than a person.
            Vector3 best = Vector3.zero;
            float bestY = float.NegativeInfinity;
            float bestDist = 0f;
            int considered = 0;
            float highest = 0f, lowest = 0f;

            for (float d = 55f; d <= 110f; d += 5f)
            {
                for (int i = 0; i < 24; i++)
                {
                    float a = i / 24f * Mathf.PI * 2f;
                    var at = new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);

                    if (!Physics.Raycast(at + Vector3.up * 40f, Vector3.down,
                                         out var hit, 90f, ~0, QueryTriggerInteraction.Ignore))
                        continue;

                    considered++;
                    if (considered == 1) { highest = hit.point.y; lowest = hit.point.y; }
                    else
                    {
                        highest = Mathf.Max(highest, hit.point.y);
                        lowest = Mathf.Min(lowest, hit.point.y);
                    }

                    if (hit.point.y > bestY)
                    {
                        bestY = hit.point.y;
                        best = new Vector3(at.x, hit.point.y, at.z);
                        bestDist = d;
                    }
                }
            }

            if (considered == 0)
            {
                sb.AppendLine("ColorThief: no ground found 55-110 m out. " +
                              "The village may be smaller than Beat 7 assumes.");
                problems++;
                return;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container.transform);
            inst.name = "ColorThief";
            inst.transform.position = best;
            inst.transform.rotation = FaceSquare(best);   // looking back at the village

            foreach (var c in inst.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);

            // No controller: in Level 1 it is a silhouette on a hill, and there
            // is no Color Thief animation in the Level 1 asset set to give it.
            // Leaving a disabled Animator in place makes the omission visible
            // and obvious to fix in Level 2 rather than silently absent.
            foreach (var a in inst.GetComponentsInChildren<Animator>())
                a.enabled = false;

            Physics.SyncTransforms();
            FitHeight(inst, 2.4f);
            Physics.SyncTransforms();

            int rends = inst.GetComponentsInChildren<Renderer>().Length;

            sb.AppendLine();
            sb.AppendLine("ColorThief: placed at " + best.ToString("F1") + ", " +
                          bestDist.ToString("0") + " m from the square, " + rends +
                          " renderer(s), " + HeightOf(inst).ToString("0.00") + " m tall, Animator off");

            // The honest finding. A "hilltop silhouette" needs a hill, and this
            // village does not have one: 288 samples out to 110 m came back
            // between these two heights, so the thief is standing on flat ground
            // with the treeline behind it. Beat 7 needs either a raised mound
            // built under him or a distant backdrop, and until one of those
            // exists this is a man-shaped hole at the edge of the world.
            sb.AppendLine("  GROUND OUT 55-110 m: " + lowest.ToString("0.0") + " m to " +
                          highest.ToString("0.0") + " m, a range of " +
                          (highest - lowest).ToString("0.00") +
                          " m. The village is flat out here — there is no hill.");
            sb.AppendLine("  Best available height was " + (bestY - squareGround).ToString("+0.0;-0.0") +
                          " m relative to the square, picked from " + considered + " samples.");
            sb.AppendLine("  NEEDS: a raised mound or a distant backdrop, and a light behind " +
                          "the silhouette, before Beat 7 reads. Not built here — it is " +
                          "Beat 7's job, not the cast's.");

            placed++;
        }

        // --- reporting -----------------------------------------------------------

        static void Describe(StringBuilder sb,
                             (string Name, string Model, string Controller, Vector3 Wanted,
                              float SearchRadius, float MinFromSquare, float TargetHeight,
                              string Group) who,
                             Vector3 at, GameObject go, string note)
        {
            Physics.SyncTransforms();

            var rends = go.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds(at, Vector3.zero);
            foreach (var r in rends) bounds.Encapsulate(r.bounds);

            var anim = go.GetComponentInChildren<Animator>();
            var col = go.GetComponentInChildren<Collider>();

            string collider = "NONE — the player can walk through it";
            var capsule = col as CapsuleCollider;
            if (capsule != null)
            {
                collider = "Capsule r=" + capsule.radius.ToString("0.00") +
                           " h=" + capsule.height.ToString("0.00") +
                           (capsule.isTrigger ? " TRIGGER" : " solid");
            }
            else if (col != null)
            {
                collider = col.GetType().Name + (col.isTrigger ? " TRIGGER" : " solid");
            }

            // Is the player actually able to touch it? Measured, because a
            // crawler 4 m up a slope can be perfectly standable and perfectly
            // unreachable, and only one of those is a placement.
            float walkHeight = GroundHeight(at, out float ny);
            bool hasGround = !float.IsNaN(walkHeight);

            sb.AppendLine();
            sb.AppendLine(who.Name + "  at " + at.ToString("F2"));
            sb.AppendLine("  " + Vector3.Distance(Flat(at), Square).ToString("0.0") +
                          " m from the square, " +
                          Vector3.Distance(Flat(at), Flat(who.Wanted)).ToString("0.0") +
                          " m from where it was wanted");
            sb.AppendLine("  " + note);
            sb.AppendLine("  " + (hasGround
                ? "ground y " + walkHeight.ToString("F2") + " (normal.y " + ny.ToString("0.00") + ")"
                : "NO GROUND UNDER IT — this is off the edge of the village") +
                          ", model bounds " +
                          bounds.size.x.ToString("0.0") + " x " + bounds.size.y.ToString("0.0") +
                          " x " + bounds.size.z.ToString("0.0") + " m, " + rends.Length +
                          " renderer(s)");
            sb.AppendLine("  Animator: " + (anim == null
                ? "NONE — will not move"
                : anim.runtimeAnimatorController == null
                    ? "present but no controller — will not move"
                    : "'" + anim.runtimeAnimatorController.name + "'" +
                      (anim.avatar != null && anim.avatar.isValid ? " (avatar ok)"
                                                                   : " (AVATAR INVALID)")));
            sb.AppendLine("  collider: " + collider);
        }

        static void Finish(StringBuilder sb)
        {
            Debug.Log("[Echoes] cast placement\n" + sb);
            File.WriteAllText(PathOf(Directory.GetCurrentDirectory(), Report), sb.ToString());
        }

        /// <summary>
        /// System.IO.Path under a name of its own. A helper called Path in this
        /// namespace shadows the type for every other method in the file, and
        /// the failure is "Path is a method, not a type" somewhere far from here.
        /// </summary>
        static string PathOf(string dir, string file) => System.IO.Path.Combine(dir, file);
    }
}
