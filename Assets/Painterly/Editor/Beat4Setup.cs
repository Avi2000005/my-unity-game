using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Beat 4 — build the gate, the plate, and the crawlspace, then prove the
    /// crawlspace is a crawlspace.
    ///
    /// Every number in <see cref="Where"/> came out of a measurement, not a
    /// sketch, and the two that decide whether the beat works are worth
    /// repeating here because they are the whole reason this tool verifies
    /// anything at all.
    ///
    /// Ari's body is 1.80 m — a swept capsule inside AriMover, not a mesh and
    /// not a collider, because she has no collider at all. Mono's body is
    /// 0.57 m. The gap between them is 1.23 m, and the crawlspace's ceiling is
    /// 1.10 m: 0.70 m below Ari and 0.35 m above Mono. That ordering is not
    /// negotiable and neither is the sign of it — a crawlspace at 1.10 m that
    /// is *wider* than Mono is 0.25 m wide is deliberately too wide for width
    /// to be the reason she cannot use it, so that when she stands at the mouth
    /// and tries, the thing that stops her is obviously her own height and not
    /// a gap she could squeeze through.
    ///
    /// The site is the strip of ground between the east end of the gully walls
    /// and the west wall of House_35_0: measured flat at 0.00 m, empty of every
    /// collider from x 28 to x 34 and z 4.4 to z 10. Nothing had to be moved.
    /// </summary>
    public static class Beat4Setup
    {
        const string ContainerName = "L1_Beat4";
        const string LinesPath = "Assets/Painterly/MonoHintLines.asset";
        const string Report = "Temp/beat4_setup.txt";

        const string StoneMat = "Assets/Painterly/Materials/RockTrim.mat";
        const string WoodMat = "Assets/Painterly/Materials/WoodTrim.mat";
        const string IronMat = "Assets/Painterly/Materials/MetalOrnament.mat";
        const string PlankMat = "Assets/Painterly/Materials/Beat3_Stump.mat";

        // --- the wall ------------------------------------------------------------
        const float WallX = 30.00f;          // metres, world
        const float WallThick = 0.40f;
        const float WallZMin = 2.50f;
        const float WallZMax = 12.00f;
        const float WallHeight = 2.60f;

        // --- the gate ------------------------------------------------------------
        const float GateZMin = 6.20f;        // 1.60 m: the gully's own width, so
        const float GateZMax = 7.80f;        // she walks straight out into it
        const float GateHeight = 2.10f;
        const float GateThick = 0.30f;
        const float GateTravel = 2.25f;      // > GateHeight, so the open slab's
                                             // top is below the ground plane
        const float GateOpenSeconds = 1.7f;

        // --- the crawlspace ------------------------------------------------------
        const float CrawlZMin = 8.60f;       // 0.90 m wide interior
        const float CrawlZMax = 9.50f;
        const float CrawlCeiling = 1.10f;    // the governing number
        const float CrawlXNear = 29.50f;     // the lip that overhangs the mouth
        const float CrawlXFar = 32.90f;
        const float CrawlSideThick = 0.25f;
        const float CrawlRoofThick = 0.25f;

        const float SwitchX = 32.55f;
        const float SwitchY = 0.45f;
        const float SwitchZ = 9.05f;

        const float ErrandX = 32.30f;        // out of Ari's sight down the tunnel
        const float ErrandZ = 9.05f;

        // --- the plate -----------------------------------------------------------
        const float PlateX = 28.95f;
        const float PlateZ = 5.70f;
        const float PlateSize = 1.40f;
        const float PlateHeight = 0.30f;     // below Ari's 0.35 m step height

        static string _reason;
        static int _pass, _fail, _warn;

        [MenuItem("Tools/Echoes/Build Beat 4 - Gate and Crawlspace", priority = 71)]
        public static void Run()
        {
            var sb = new StringBuilder();
            if (File.Exists(PathOf(Directory.GetCurrentDirectory(), Report)))
                File.Delete(PathOf(Directory.GetCurrentDirectory(), Report));

            try { Body(sb); }
            catch (Exception e)
            {
                sb.AppendLine();
                sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
                sb.AppendLine(e.StackTrace);
                Debug.LogError("[Echoes] Beat 4 setup threw: " + e);
            }
            finally
            {
                Debug.Log("[Echoes] Beat 4 setup\n" + sb);
                File.WriteAllText(PathOf(Directory.GetCurrentDirectory(), Report),
                                  sb.ToString());
            }
        }

        static void Body(StringBuilder sb)
        {
            // Every check below, and every write, assumes edit mode.
            //
            // In play mode `MarkSceneDirty` throws outright, so the tool dies
            // halfway through and leaves the level half-built — the container
            // created, the walls not, no report. Worse, a tool run from a
            // play-mode script that skipped the dirty call would build objects
            // that vanish the moment the user stops, and the report would read
            // PASS. Refusing up front is the only version that cannot lie.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sb.AppendLine("!! The editor is in PLAY MODE.");
                sb.AppendLine("   Nothing was built. Press Ctrl+P to leave play mode " +
                              "and run this again.");
                sb.AppendLine("   (This will build into the scene, which play mode " +
                              "would throw away.)");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) { sb.AppendLine("No scene is open."); return; }

            Physics.SyncTransforms();
            _reason = null; _pass = 0; _fail = 0; _warn = 0;

            sb.AppendLine("BEAT 4 — GATE, PLATE, CRAWLSPACE");
            sb.AppendLine();

            sb.AppendLine("scene '" + scene.name + "' at " + scene.path +
                          (scene.isDirty ? "  (UNSAVED CHANGES)" : "  (clean)"));
            sb.AppendLine("open scenes: " + SceneCount());
            sb.AppendLine();

            // --- 1. the cast ------------------------------------------------------

            // Named lookup, then a type-based one.
            //
            // `GameObject.Find` returns null for an inactive object, and a
            // character hidden by a beat's own script is exactly what a tool
            // like this gets pointed at — so a null here reads as "the cast was
            // never placed" when the truth is "the cast is asleep". The
            // fallback finds them either way and says which path it took, so
            // the difference is visible rather than silent.
            var ariGo = Named("Ari");
            var monoGo = Named("Mono");

            if (ariGo == null || monoGo == null)
            {
                var movers = UnityEngine.Object.FindObjectsByType<AriMover>(
                    FindObjectsInactive.Include);
                var brains = UnityEngine.Object.FindObjectsByType<MonoCompanion>(
                    FindObjectsInactive.Include);

                if (movers.Length > 0) ariGo = movers[0].gameObject;
                if (brains.Length > 0) monoGo = brains[0].gameObject;

                sb.AppendLine("named lookup missed " +
                              (ariGo == null ? "Ari " : "") +
                              (monoGo == null ? "Mono" : "") +
                              " — fell back to a component search");
            }

            if (ariGo == null || monoGo == null)
            {
                sb.AppendLine();
                sb.AppendLine("!! no Ari or Mono here. " +
                              UnityEngine.Object.FindObjectsByType<Transform>(
                                  FindObjectsInactive.Include).Length +
                              " transforms in this scene, " +
                              SceneCount() + " scene(s) open.");
                sb.AppendLine("   The Level 1 cast is placed into the GREY " +
                              "VILLAGE scene. If the open scene is not that " +
                              "one, this tool has nothing to attach to.");
                return;
            }

            sb.AppendLine("Ari  '" + ariGo.name + "' at " +
                          ariGo.transform.position.ToString("F2") +
                          (ariGo.activeInHierarchy ? "" : "   [INACTIVE]"));
            sb.AppendLine("Mono '" + monoGo.name + "' at " +
                          monoGo.transform.position.ToString("F2") +
                          (monoGo.activeInHierarchy ? "" : "   [INACTIVE]"));

            var ari = ariGo.GetComponent<AriMover>();
            var mono = monoGo.GetComponent<MonoCompanion>();
            if (ari == null) { sb.AppendLine("Ari has no AriMover."); return; }
            if (mono == null)
            {
                mono = monoGo.AddComponent<MonoCompanion>();
                sb.AppendLine("added MonoCompanion to Mono");
            }

            sb.AppendLine("--- the two bodies this beat is built on ---");

            // Property names, capitalised, exactly as AriMover declares them.
            //
            // This passed "bodyHeight" — the *field* — where a property name was
            // wanted, so reflection found nothing, fell through to the field
            // fallback, read the right number anyway, and printed a warning
            // blaming a stale assembly. Two recompiles later the warning was
            // still there, which is what finally showed the message was wrong
            // and the lookup was: no amount of rebuilding fixes a misspelt name.
            // A check that survives a rebuild but complains about needing one is
            // telling you it is wrong.
            float ariBody = AriBody(sb, "Ari", ari, "BodyHeight");
            float ariRadius = AriBody(sb, "Ari", ari, "BodyRadius");
            float ariStep = AriBody(sb, "Ari", ari, "StepHeight");
            float monoBody = MonoBody(sb, monoGo);
            float monoVisible = MonoVisible(sb, monoGo);
            float errandRadius = ErrandRadius(sb, mono);
            sb.AppendLine();

            // --- 2. ground --------------------------------------------------------

            float ground = GroundAt(new Vector3(WallX, 0f, (WallZMin + WallZMax) * 0.5f));
            sb.AppendLine("ground under the wall is " + ground.ToString("0.000") + " m");
            sb.AppendLine();

            // --- 3. the lines -----------------------------------------------------

            // Mono fetches his own lines from the asset, so the director never
            // needs a reference to it. The call is here because Beat 4's ids
            // have to exist before a beat can fire them — a beat that ships
            // without its script is silent, and silence looks identical to a
            // beat that has not triggered yet.
            BuildLines(sb);
            sb.AppendLine();

            // --- 4. build ---------------------------------------------------------

            var container = Container(sb);
            Clear(container);

            var stone = Load(StoneMat);
            var wood = Load(WoodMat);
            var iron = Load(IronMat);
            var plank = Load(PlankMat);

            if (stone == null) { sb.AppendLine("NO " + StoneMat); return; }

            float hx0 = WallX - WallThick * 0.5f;
            float hx1 = WallX + WallThick * 0.5f;
            float cx = (hx0 + hx1) * 0.5f;

            // Wall in three pieces, because the gate and the crawl mouth are
            // holes in it and a single box cannot have holes.
            Box(container.transform, "Beat4_Wall_S", cx, ground + WallHeight * 0.5f,
                (WallZMin + GateZMin) * 0.5f,
                WallThick, WallHeight, GateZMin - WallZMin, stone);

            Box(container.transform, "Beat4_Wall_Pier", cx, ground + WallHeight * 0.5f,
                (GateZMax + CrawlZMin) * 0.5f,
                WallThick, WallHeight, CrawlZMin - GateZMax, stone);

            Box(container.transform, "Beat4_Wall_N", cx, ground + WallHeight * 0.5f,
                (CrawlZMax + WallZMax) * 0.5f,
                WallThick, WallHeight, WallZMax - CrawlZMax, stone);

            // Lintels. Without these the gate opening runs to the sky and the
            // crawl mouth is a hole with the sky above it, which reads as an
            // unfinished wall rather than as two ways through.
            Box(container.transform, "Beat4_Lintel_Gate", cx,
                ground + GateHeight + (WallHeight - GateHeight) * 0.5f,
                (GateZMin + GateZMax) * 0.5f,
                WallThick, WallHeight - GateHeight, GateZMax - GateZMin, stone);

            Box(container.transform, "Beat4_Lintel_Crawl", cx,
                ground + CrawlCeiling + CrawlRoofThick +
                      (WallHeight - CrawlCeiling - CrawlRoofThick) * 0.5f,
                (CrawlZMin + CrawlZMax) * 0.5f,
                WallThick, WallHeight - CrawlCeiling - CrawlRoofThick,
                CrawlZMax - CrawlZMin, stone);

            // --- the crawlspace ----------------------------------------------------

            float roofMid = ground + CrawlCeiling + CrawlRoofThick * 0.5f;
            float sideH = CrawlCeiling + CrawlRoofThick;

            Box(container.transform, "Beat4_Crawl_Roof",
                (CrawlXNear + CrawlXFar) * 0.5f, roofMid,
                (CrawlZMin + CrawlZMax) * 0.5f,
                CrawlXFar - CrawlXNear, CrawlRoofThick,
                (CrawlZMax - CrawlZMin) + CrawlSideThick * 2f, wood);

            Box(container.transform, "Beat4_Crawl_Side_S",
                (hx1 + CrawlXFar) * 0.5f, ground + sideH * 0.5f,
                CrawlZMin - CrawlSideThick * 0.5f,
                CrawlXFar - hx1, sideH, CrawlSideThick, stone);

            Box(container.transform, "Beat4_Crawl_Side_N",
                (hx1 + CrawlXFar) * 0.5f, ground + sideH * 0.5f,
                CrawlZMax + CrawlSideThick * 0.5f,
                CrawlXFar - hx1, sideH, CrawlSideThick, stone);

            Box(container.transform, "Beat4_Crawl_End",
                CrawlXFar + CrawlSideThick * 0.5f, ground + sideH * 0.5f,
                (CrawlZMin + CrawlZMax) * 0.5f,
                CrawlSideThick, sideH,
                (CrawlZMax - CrawlZMin) + CrawlSideThick * 2f, stone);

            // --- the gate ----------------------------------------------------------

            var gateGo = new GameObject("Beat4_Gate");
            gateGo.transform.SetParent(container.transform, false);

            // Z is the gate's centre along the wall — the average of the two
            // opening edges, which is 7.00. The first version wrote
            // `GateHeight * 0.5f` here, which put the whole gate at z 1.05,
            // five metres south of the opening it was supposed to close, and
            // the wall it stood in. The check that should have caught it —
            // does the closed gate stop Ari — did catch it, and said
            // "walks straight through", which was the only honest thing in the
            // report. Everything else passed, so a build without the sweep
            // test would have shipped a gate floating beside a wall.
            gateGo.transform.position =
                new Vector3(WallX, ground, (GateZMin + GateZMax) * 0.5f);

            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Beat4_Gate_Slab";
            slab.transform.SetParent(gateGo.transform, false);
            slab.transform.localPosition = Vector3.zero;
            slab.transform.localScale = new Vector3(GateThick, GateHeight,
                                                    GateZMax - GateZMin);
            if (iron != null) slab.GetComponent<Renderer>().sharedMaterial = iron;

            var gate = gateGo.AddComponent<BeatGate>();
            Set(gate, "slab", slab.transform);
            Set(gate, "openAxis", Vector3.down);
            Set(gate, "travel", GateTravel);
            Set(gate, "seconds", GateOpenSeconds);

            // --- the plate ---------------------------------------------------------

            var plateGo = new GameObject("Beat4_Plate");
            plateGo.transform.SetParent(container.transform, false);

            var plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plinth.name = "Beat4_Plate_Plinth";
            plinth.transform.SetParent(plateGo.transform, false);
            plinth.transform.localPosition = new Vector3(0f, -PlateHeight * 0.5f, 0f);
            plinth.transform.localScale = new Vector3(PlateSize, PlateHeight, PlateSize);
            if (plank != null) plinth.GetComponent<Renderer>().sharedMaterial = plank;

            var lever = plateGo.AddComponent<HoldLever>();
            Set(lever, "ari", ari);

            // On the plinth's top face, because that is where Ari's own feet go
            // when she steps onto it and the lever's reach is measured from
            // here. Sitting the transform at ground level instead would put the
            // trigger 30 cm below where she actually stands, which is inside
            // her step height and so within her own sweep — the lever would
            // then read as held while she was still standing beside it.
            plateGo.transform.position = new Vector3(PlateX, ground + PlateHeight, PlateZ);

            // --- the switch and the errand point -----------------------------------

            var switchGo = new GameObject("Beat4_Switch");
            switchGo.transform.SetParent(container.transform, false);
            switchGo.transform.position = new Vector3(SwitchX, ground + SwitchY, SwitchZ);

            var boss = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boss.name = "Beat4_Switch_Boss";
            boss.transform.SetParent(switchGo.transform, false);
            boss.transform.localPosition = Vector3.zero;
            boss.transform.localScale = new Vector3(0.14f, 0.30f, 0.30f);
            if (iron != null) boss.GetComponent<Renderer>().sharedMaterial = iron;
            // A trigger would be tidier, but a trigger is invisible and a boss
            // you can aim at is not. The reach test in the director uses
            // distance, so the collider only has to be out of the way.
            var bossCol = boss.GetComponent<Collider>();
            if (bossCol != null) bossCol.isTrigger = true;

            var latching = switchGo.AddComponent<LatchingSwitch>();
            Set(latching, "mono", mono);
            Set(latching, "lever", lever);
            Set(latching, "reach", 1.0f);

            var errandGo = new GameObject("Beat4_ErrandPoint");
            errandGo.transform.SetParent(container.transform, false);
            errandGo.transform.position = new Vector3(ErrandX, ground, ErrandZ);

            Set(gate, "source", latching);

            // --- the director -------------------------------------------------------

            var dirGo = new GameObject("Beat4_Director");
            dirGo.transform.SetParent(container.transform, false);
            dirGo.transform.position = new Vector3(PlateX, ground + 1.0f, PlateZ);

            var chaseGo = GameObject.Find("MonoChase");
            var chase = chaseGo == null ? null : chaseGo.GetComponent<MonoChase>();

            var dir = dirGo.AddComponent<Beat4Director>();
            Set(dir, "gate", gate);
            Set(dir, "lever", lever);
            Set(dir, "latchingSwitch", latching);
            Set(dir, "ari", ari);
            Set(dir, "mono", mono);
            Set(dir, "errandPoint", errandGo.transform);
            Set(dir, "chase", chase);

            // --- the test warp -----------------------------------------------------
            //
            // Placed here because this is the beat that made it necessary: the
            // gate is at x 30 and she starts at x 7, so checking whether it
            // opens meant crossing the whole village first, every time.
            //
            // The stop sits *west of the gate, on Ari's side*, facing it — not
            // past it. Warping her to the far side would drop her inside the
            // crawlspace's footprint with a shut gate behind her and a beat
            // that cannot be tested at all. The point of the stop is to watch
            // the gate refuse her.
            var warpGo = new GameObject("BeatWarp");
            warpGo.transform.SetParent(container.transform, false);
            warpGo.transform.position = new Vector3(WallX - 3.5f, ground, 7.0f);

            var warp = warpGo.AddComponent<BeatWarp>();

            // Built into a local first, then handed over. Reading it back out
            // with reflection to check it would be a round trip to ask the
            // component the question it was just handed the answer to, and it
            // cost a compile cycle to learn that `Src` needs its type argument
            // spelled out — which broke the project build, which is why
            // `BeatWarp` never reached the assembly `run_script` compiles
            // against, which failed with five "type not found" errors that all
            // pointed at the warp and none of which were about the warp. One
            // missed type argument, five unrelated-looking errors.
            var stops = new List<BeatWarp.Stop>
            {
                new BeatWarp.Stop
                {
                    name = "beat4_gate",
                    // 1.4 m west of the gate face, which is inside the
                    // director's 2.6 m notice range, so the beat starts on
                    // arrival rather than having to be walked into.
                    ari = new Vector3(WallX - 1.4f, ground, 7.0f),
                    mono = new Vector3(WallX - 2.2f, ground, 6.4f)
                },
                new BeatWarp.Stop
                {
                    name = "beat4_plate",
                    // Standing on the plinth, for testing the lever and Mono's
                    // errand without having to arrive at the gate first.
                    ari = new Vector3(PlateX, ground + PlateHeight, PlateZ),
                    mono = new Vector3(PlateX + 1.6f, ground, PlateZ + 1.0f)
                }
            };
            Set(warp, "stops", stops);
            Set(warp, "index", 0);

            // Checked here rather than in Verify, because the stop is built
            // here and Verify runs after everything else — a check that needs
            // a local from the middle of the build section belongs next to the
            // build section.
            //
            // Ari is a swept-capsule mover with no collider, so a stop that
            // lands her inside the crawlspace is not something the physics
            // complains about. She would simply stand there, under a ceiling
            // that is not meant to have her under it, and the beat would look
            // like it had been solved.
            if (stops.Count == 0)
            {
                Fail(sb, "the warp has no stops, so the beat cannot be tested");
            }
            else
            {
                var gate0 = stops[0];
                float stopToGate = Mathf.Abs(gate0.ari.x - WallX);

                sb.AppendLine("  warp " + stops.Count + " stop(s), first is '" +
                              gate0.name + "' at " + gate0.ari.ToString("F2"));
                sb.AppendLine();

                Check(sb, "the warp stop is on Ari's side of the gate",
                      gate0.ari.x < WallX,
                      "x " + gate0.ari.x.ToString("0.00") + " against a gate at " +
                      WallX.ToString("0.00") +
                      (gate0.ari.x < WallX ? " — she arrives facing it"
                                          : " — she is warped past it and the " +
                                            "beat cannot be tested"));

                Check(sb, "the warp stop is in the gate's opening",
                      gate0.ari.z >= GateZMin && gate0.ari.z <= GateZMax,
                      "z " + gate0.ari.z.ToString("0.00") + " against an opening " +
                      GateZMin.ToString("0.00") + " to " + GateZMax.ToString("0.00"));

                Check(sb, "the warp stop lands inside the beat's notice range",
                      stopToGate <= 2.6f,
                      stopToGate.ToString("0.00") + " m from the gate, notice range " +
                      "2.60 m — she must walk the last stride herself");

                Check(sb, "the warp stop is outside the crawlspace mouth",
                      gate0.ari.x < CrawlXNear || gate0.ari.z < CrawlZMin ||
                      gate0.ari.z > CrawlZMax,
                      "the crawl mouth is at x " + CrawlXNear.ToString("0.00") +
                      ", z " + CrawlZMin.ToString("0.00") + " to " +
                      CrawlZMax.ToString("0.00"));
            }

            if (chase == null)
                sb.AppendLine("  no MonoChase in the scene, so Beat 4 will not " +
                              "wait for Beat 3 before it starts");

            EditorSceneManager.MarkSceneDirty(scene);
            Physics.SyncTransforms();

            sb.AppendLine("--- built ---");
            sb.AppendLine("  " + container.name + " with " +
                          container.transform.childCount + " children");
            sb.AppendLine("  wall  x " + hx0.ToString("0.00") + " to " +
                          hx1.ToString("0.00") +
                          ", z " + WallZMin.ToString("0.0") + " to " +
                          WallZMax.ToString("0.0") +
                          ", " + WallHeight.ToString("0.00") + " m tall");
            sb.AppendLine("  gate  " + (GateZMax - GateZMin).ToString("0.00") +
                          " m wide, " + GateHeight.ToString("0.00") + " m tall, " +
                          "slides " + GateTravel.ToString("0.00") + " m down");
            sb.AppendLine("  crawl " + (CrawlXFar - CrawlXNear).ToString("0.00") +
                          " m long, " + (CrawlZMax - CrawlZMin).ToString("0.00") +
                          " m wide, ceiling " + CrawlCeiling.ToString("0.00") + " m");
            sb.AppendLine();

            // --- 5. verify ----------------------------------------------------------

            Verify(sb, ari, mono, gate, slab, lever, latching, dir, errandGo.transform,
                   container, errandRadius, ariBody, ariRadius, ariStep,
                   monoBody, monoVisible, ground);

            sb.AppendLine();
            sb.AppendLine("VERDICT " + (_fail == 0 ? "PASS" : "FAIL") +
                          " — " + _pass + " passed, " + _fail + " failed, " +
                          _warn + " warnings");
            if (_reason != null)
                sb.AppendLine("  blocked at " + _reason);
        }

        // --- verification ---------------------------------------------------------

        static void Verify(StringBuilder sb, AriMover ari, MonoCompanion mono,
                           BeatGate gate, GameObject slab, HoldLever lever,
                           LatchingSwitch latching, Beat4Director dir,
                           Transform errand, GameObject container,
                           float errandRadius, float ariBody, float ariRadius,
                           float ariStep, float monoBody, float monoVisible,
                           float ground)
        {
            sb.AppendLine("--- the crawlspace, measured rather than declared ---");
            sb.AppendLine();

            // 1. headroom along the tunnel, by ray, at Mono's own width.
            float minHead = float.MaxValue, maxHead = 0f;
            var heads = new List<float>();
            for (float x = CrawlXNear + 0.35f; x <= CrawlXFar - 0.15f; x += 0.20f)
            {
                float head = Headroom(new Vector3(x, ground, (CrawlZMin + CrawlZMax) * 0.5f));
                if (head < minHead) minHead = head;
                if (head > maxHead) maxHead = head;
                heads.Add(head);
            }

            heads.Sort();
            float medHead = heads.Count == 0 ? 0f : heads[heads.Count / 2];

            sb.AppendLine("  headroom down the tunnel over " + heads.Count +
                          " samples: min " + minHead.ToString("0.00") +
                          " m, median " + medHead.ToString("0.00") +
                          " m, max " + maxHead.ToString("0.00") + " m");
            sb.AppendLine("  built to " + CrawlCeiling.ToString("0.00") + " m");

            Check(sb, "the crawl ceiling is where it was built",
                  Mathf.Abs(medHead - CrawlCeiling) < 0.03f,
                  "median " + medHead.ToString("0.00") + " m against a built " +
                  CrawlCeiling.ToString("0.00") + " m");

            Check(sb, "nothing in the tunnel is lower than designed",
                  minHead >= CrawlCeiling - 0.02f,
                  "lowest " + minHead.ToString("0.00") + " m");

            // 2. the two bodies against that ceiling. This is the beat.
            sb.AppendLine();
            sb.AppendLine("  Ari  body " + ariBody.ToString("0.00") + " m" +
                          "   ceiling " + CrawlCeiling.ToString("0.00") + " m" +
                          "   over by " + (ariBody - CrawlCeiling).ToString("0.00") + " m");
            sb.AppendLine("  Mono body " + monoBody.ToString("0.00") + " m" +
                          "   visible " + monoVisible.ToString("0.00") + " m" +
                          "   under by " + (CrawlCeiling - monoVisible).ToString("0.00") + " m");

            Check(sb, "Ari cannot fit — she is too tall by at least 0.3 m",
                  ariBody - CrawlCeiling >= 0.30f,
                  "she is " + (ariBody - CrawlCeiling).ToString("0.00") +
                  " m over the ceiling");

            Check(sb, "Mono fits — his visible head clears the roof",
                  CrawlCeiling - monoVisible >= 0.20f,
                  "the roof is " + (CrawlCeiling - monoVisible).ToString("0.00") +
                  " m above his head");

            // 3. and width is NOT the reason, on purpose.
            float mouthWidth = CrawlZMax - CrawlZMin;
            float ariDiameter = ariRadius * 2f;
            sb.AppendLine();
            sb.AppendLine("  the mouth is " + mouthWidth.ToString("0.00") +
                          " m wide and Ari is " + ariDiameter.ToString("0.00") +
                          " m across, so she would fit through it sideways by " +
                          (mouthWidth - ariDiameter).ToString("0.00") + " m");
            Check(sb, "width is wide enough that height is what stops her",
                  mouthWidth - ariDiameter >= 0.15f,
                  mouthWidth.ToString("0.00") + " m mouth against a " +
                  ariDiameter.ToString("0.00") + " m body — the puzzle must read " +
                  "as 'too tall', not 'too tight'");

            // 4. the gate actually stops her, by Ari's own sweep.
            sb.AppendLine();
            sb.AppendLine("--- the gate, tested with Ari's own capsule sweep ---");

            float from = WallX - 2.2f;
            float to = WallX + 3.0f;
            Vector3 a = new Vector3(from, ground, (GateZMin + GateZMax) * 0.5f);
            Vector3 b = new Vector3(to, ground, (GateZMin + GateZMax) * 0.5f);

            bool blockedShut = Swept(a, b, ariBody, ariRadius, ground, slab.transform);
            sb.AppendLine("  gate shut: " + (blockedShut ? "BLOCKED by the slab"
                                                         : "walks straight through"));
            Check(sb, "the closed gate stops Ari", blockedShut,
                  "a capsule sweep of " + ariBody.ToString("0.00") + " m by " +
                  ariDiameter.ToString("0.00") + " m with her soles on the ground, " +
                  "east along the gate's centre line");

            // Move the slab where it will end up and prove it is the slab and
            // not the wall, the lintel or the ground that was in the way.
            var lp = slab.transform.localPosition;
            slab.transform.localPosition = lp + Vector3.down * GateTravel;
            Physics.SyncTransforms();

            bool blockedOpen = Swept(a, b, ariBody, ariRadius, ground, slab.transform);
            sb.AppendLine("  gate open: " + (blockedOpen ? "STILL BLOCKED"
                                                         : "walks through"));

            slab.transform.localPosition = lp;
            Physics.SyncTransforms();

            // Negated, and named `blockedOpen` for a reason. The first version
            // named it `open` and passed it straight into Check, so the
            // assertion read "the gate lets her through" whenever the gate
            // was in fact still blocking her. The physics was right and the
            // verdict was backwards — and because the printed line said
            // "walks through" while the FAIL said otherwise, the report
            // contradicted itself on the one thing the beat exists to prove.
            //
            // `Swept` returns true for a *collision*. Both call sites now
            // name what it means before asserting on it.
            Check(sb, "the open gate lets her through", !blockedOpen,
                  "same sweep with the slab " + GateTravel.ToString("0.00") +
                  " m down, " +
                  (blockedOpen ? "and it STILL blocked her" : "and she walks through"));

            // 5. the opening is big enough for her in the first place.
            float openH = GateHeight - ariBody;
            float openW = (GateZMax - GateZMin) - ariDiameter;
            sb.AppendLine();
            Check(sb, "the opening is taller than she is", openH >= 0.20f,
                  "2.10 m opening against a " + ariBody.ToString("0.00") +
                  " m body, " + openH.ToString("0.00") + " m to spare");
            Check(sb, "the opening is wider than she is", openW >= 0.40f,
                  (GateZMax - GateZMin).ToString("0.00") + " m against " +
                  ariDiameter.ToString("0.00") + " m, " + openW.ToString("0.00") +
                  " m to spare");

            // 6. the open slab really is underground.
            float slabTopClosed = ground + GateHeight;
            float slabTopOpen = slabTopClosed - GateTravel;
            sb.AppendLine();
            sb.AppendLine("  gate slab top: " + slabTopClosed.ToString("0.00") +
                          " m shut, " + slabTopOpen.ToString("0.00") + " m open, " +
                          "ground " + ground.ToString("0.00") + " m");
            Check(sb, "the open slab is entirely below the ground",
                  slabTopOpen < ground - 0.05f,
                  "top of the slab ends at " + slabTopOpen.ToString("0.00") +
                  " m, ground is " + ground.ToString("0.00") + " m");

            // 7. the plate is somewhere a person can stand.
            sb.AppendLine();
            sb.AppendLine("--- the plate ---");
            sb.AppendLine("  plinth top " + (ground + PlateHeight).ToString("0.00") +
                          " m, Ari's step height " + ariStep.ToString("0.00") + " m");
            Check(sb, "she can step onto the plinth without jumping",
                  PlateHeight <= ariStep + 0.01f,
                  PlateHeight.ToString("0.00") + " m against a " +
                  ariStep.ToString("0.00") + " m step");

            sb.AppendLine("  plate at (" + PlateX.ToString("0.00") + ", " +
                          PlateZ.ToString("0.00") + "), " +
                          Vector2.Distance(new Vector2(28f, 7f),
                                           new Vector2(PlateX, PlateZ)).ToString("0.00") +
                          " m from where she comes out of the gully");
            Check(sb, "the plate is clear of the gully's walls",
                  PlateX > 28f + 0.6f, "the walls end at x 28.00, the plate is at " +
                  PlateX.ToString("0.00"));

            // 8. Mono can actually get to the switch.
            sb.AppendLine();
            sb.AppendLine("--- Mono's errand ---");
            float errandToSwitch = Vector3.Distance(
                new Vector3(errand.position.x, 0f, errand.position.z),
                new Vector3(SwitchX, 0f, SwitchZ));
            sb.AppendLine("  errand radius " + errandRadius.ToString("0.00") +
                          " m, switch reach " + latching.Reach.ToString("0.00") +
                          " m, errand point to switch " +
                          errandToSwitch.ToString("0.00") + " m");
            Check(sb, "the switch's reach is at least Mono's errand radius",
                  latching.Reach >= errandRadius,
                  "he stops " + errandRadius.ToString("0.00") + " m short, so a " +
                  latching.Reach.ToString("0.00") + " m reach " +
                  (latching.Reach >= errandRadius ? "reaches him" : "MISSES him"));

            Check(sb, "the errand point is inside the switch's reach",
                  errandToSwitch <= latching.Reach,
                  errandToSwitch.ToString("0.00") + " m apart with a " +
                  latching.Reach.ToString("0.00") + " m reach");

            Check(sb, "the errand point is out of Ari's sight down the tunnel",
                  errand.position.x > WallX + 1.5f,
                  "errand at x " + errand.position.x.ToString("0.00") +
                  ", mouth at x " + CrawlXNear.ToString("0.00") +
                  " — she can see " + (WallX + 1.5f - CrawlXNear).ToString("0.00") +
                  " m in, he is " + (errand.position.x - CrawlXNear).ToString("0.00") +
                  " m in");

            // 9. every collider that landed inside the beat, so an accidental
            //    extra wall cannot hide in here.
            sb.AppendLine();
            sb.AppendLine("--- everything inside the beat's footprint ---");

            var box = new Bounds(new Vector3(WallX + 1.4f, ground + 1.2f, 7.0f),
                                 new Vector3(5.6f, 3.0f, 10.4f));
            var cols = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include);
            var mine = new HashSet<Collider>();
            Collect(container.transform, mine);

            // `Ground` and House_35_0 are the village's own floor and a building
            // that was already standing here. They are listed so the report
            // shows what is in the space, but they are not defects and
            // counting them as such would train the reader to ignore the check.
            var exempt = new HashSet<string> { "Ground" };

            int foreign = 0;
            var rows = new List<string>();
            var villageColliders = new List<Collider>();
            foreach (var c in cols)
            {
                if (c == null || c.isTrigger) continue;
                if (mine.Contains(c)) continue;
                if (!box.Intersects(c.bounds)) continue;

                string path = PathOf2(c.transform);

                // `PathOf2` joins with " / ", spaces and all, so the test is a
                // plain substring on the name and not on "/Village_Grey/" as
                // the first version wrote it — that never matched, and every
                // one of the house's eleven colliders was reported as a
                // defect. A check that reports the village's own front door as
                // an intruder is worse than no check, because the reader
                // learns to skip the section.
                bool isVillage = path.StartsWith("Village_Grey") ||
                                 path.Contains("Village_Grey");

                if (isVillage || exempt.Contains(c.name))
                {
                    rows.Add("  (village) " + c.name + " at " +
                             c.bounds.center.ToString("F2") + "  path " + path);
                    villageColliders.Add(c);
                    continue;
                }

                foreign++;
                rows.Add("  FOREIGN " + c.GetType().Name + " '" + c.name + "' at " +
                         c.bounds.center.ToString("F2") + "  path " + path);
            }

            rows.Sort(StringComparer.Ordinal);
            foreach (var r in rows) sb.AppendLine(r);
            sb.AppendLine("  " + mine.Count + " collider(s) this beat built, " +
                          rows.Count + " village object(s) already here, " +
                          foreign + " unexplained");

            Check(sb, "nothing unexplained is standing in the beat", foreign == 0,
                  foreign == 0 ? "every collider here is either ours or the " +
                                   "village's own"
                               : foreign + " collider(s) from neither");

            // --- and the thing the list above cannot answer -------------------
            //
            // House_35_0 is genuinely inside the surveyed box — its front wall
            // is at z 3.5 and the beat's wall starts at z 2.5 — so it will be
            // listed, and "listed" is not "in the way". The question that
            // matters is whether any of it *touches* anything we built, and
            // that is a distance, not a membership test. Ground is excluded
            // because we are standing on it.
            //
            // A negative gap here means two colliders overlap, which for
            // mesh colliders over boxes means a wall with a house growing
            // through it: it looks fine in the hierarchy and is a hole in the
            // level at knee height.
            float worstGap = float.MaxValue;
            string worstWho = "";
            foreach (var mineC in mine)
            {
                if (mineC == null) continue;
                foreach (var v in villageColliders)
                {
                    if (v == null || v.name == "Ground") continue;
                    float gap = Gap(mineC.bounds, v.bounds);
                    if (gap < worstGap)
                    {
                        worstGap = gap;
                        worstWho = mineC.name + " vs " + v.name;
                    }
                }
            }

            sb.AppendLine("  closest approach between this beat and the " +
                          "village: " +
                          (worstGap == float.MaxValue ? "nothing to compare against"
                                                     : worstGap.ToString("0.00") +
                                                       " m  (" + worstWho + ")"));

            Check(sb, "the beat and the village do not overlap", worstGap > 0f,
                  worstGap == float.MaxValue
                      ? "no village geometry in range"
                      : worstGap.ToString("0.00") + " m between " + worstWho +
                        (worstGap > 0f ? " — they do not touch" : " — THEY TOUCH"));

            // 10. the pieces are wired to each other, not just placed.
            sb.AppendLine();
            sb.AppendLine("--- wiring ---");
            Check(sb, "the gate has a slab with a collider",
                  gate.SlabCollider != null,
                  gate.SlabCollider == null ? "no collider, so it would not block" :
                                              "BoxCollider on " + gate.Slab.name);

            // The gate's source field is private and nothing in the level ever
            // reads it back, so a builder that failed to set it would produce
            // a gate that opens on one switch and a switch that latches with
            // nothing listening. Reading it here is the only place that failure
            // is visible.
            var src = Src<LatchingSwitch>(gate, "source");
            Check(sb, "the gate knows which switch opens it",
                  src == latching,
                  src == null ? "source is null" :
                                (src == latching ? "points at " + latching.name
                                                 : "points at " + src.name +
                                                   ", not the switch in this tunnel"));

            Check(sb, "the switch knows which lever powers it",
                  Src<HoldLever>(latching, "lever") == lever,
                  "lever is " + lever.name);

            Check(sb, "the director knows all four parts",
                  Src<BeatGate>(dir, "gate") == gate &&
                  Src<HoldLever>(dir, "lever") == lever &&
                  Src<LatchingSwitch>(dir, "latchingSwitch") == latching &&
                  Src<Transform>(dir, "errandPoint") != null,
                  "gate, lever, switch and errand point all assigned");

            // The errand point must be the one inside this tunnel and not some
            // other beat's, or Mono walks off to another part of the level.
            Check(sb, "the errand point is inside the crawlspace",
                  Src<Transform>(dir, "errandPoint") == errand,
                  errand.name + " at x " + errand.position.x.ToString("0.00"));
        }

        // --- the lines -------------------------------------------------------------

        /// <summary>
        /// Add Beat 4's three lines to Mono's script, leaving every other line
        /// alone.
        ///
        /// Beat 3's tool rewrites the whole asset, ids included, because it is
        /// the script of record for its own beat. Two tools that both rewrote
        /// the whole asset would each delete the other's lines every time
        /// either was run, which is a bug that only shows up when you go
        /// looking for why Beat 4 went quiet after a Beat 3 rebuild. So this one
        /// appends: it strips its own ids and adds them back, and touches
        /// nothing it does not own.
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

            asset.lines.RemoveAll(l => l != null && l.id.StartsWith("beat4."));
            sb.AppendLine("  rewrote " + Beat4LineCount(asset) + " Beat 4 line(s)");

            void Beat(string id, string text) =>
                asset.lines.Add(new MonoHintLines.Line
                {
                    id = id, text = text, kind = MonoHintLines.LineKind.Beat
                });

            Beat("beat4.hold",
                 "That is a low place. I will fit; you will not. Stay on the " +
                 "plate — I can feel it wanting to be held down.");

            Beat("beat4.open",
                 "It let go. It is open. Go on through, I will come round by " +
                 "the road. There is a road. There is always a road.");

            Beat("beat4.after",
                 "Two doors and only one of us could open each one. That is " +
                 "going to be a habit, I think. I do not mind it.");

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, LinesPath);
                sb.AppendLine("  created " + LinesPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }

            sb.AppendLine("  " + asset.lines.Count + " line(s) in the asset now, " +
                          Beat4LineCount(asset) + " of them Beat 4's");

            return asset;
        }

        static int Beat4LineCount(MonoHintLines asset) =>
            asset.lines.FindAll(l => l != null && l.id.StartsWith("beat4.")).Count;

        // --- building --------------------------------------------------------------

        static void Clear(GameObject container)
        {
            var kids = new List<GameObject>();
            foreach (Transform t in container.transform) kids.Add(t.gameObject);
            foreach (var k in kids) UnityEngine.Object.DestroyImmediate(k);
        }

        static GameObject Box(Transform parent, string name, float x, float y, float z,
                              float sx, float sy, float sz, Material mat)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = new Vector3(x, y, z);
            cube.transform.localScale = new Vector3(sx, sy, sz);
            var r = cube.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            return cube;
        }

        static void Collect(Transform t, HashSet<Collider> into)
        {
            // Tests `t` itself, then its children.
            //
            // The first version walked only the children, so every wall and
            // roof this tool had just built was reported as a FOREIGN collider
            // and the beat failed its own footprint check 22 times over. The
            // first cut also skipped the slab, which is a child of the gate
            // rather than a child of the container, so the one collider the
            // whole beat exists to place was the one it was least sure about.
            var own = t.GetComponent<Collider>();
            if (own != null) into.Add(own);

            foreach (Transform ch in t) Collect(ch, into);
        }

        static string PathOf2(Transform t)
        {
            var parts = new List<string>();
            while (t != null) { parts.Insert(0, t.name); t = t.parent; }
            return string.Join(" / ", parts.ToArray());
        }

        static Material Load(string path) =>
            AssetDatabase.LoadAssetAtPath<Material>(path);

        /// <summary>
        /// Read a private serialised field back.
        ///
        /// The level's wiring is almost entirely private [SerializeField], which
        /// is right — nobody should be poking it at runtime — and which means a
        /// builder that silently failed to assign one produces a beat that
        /// looks built and is not. This is the only way to see that, and it is
        /// why Verify ends with a wiring section instead of stopping at
        /// geometry.
        /// </summary>
        static T Src<T>(UnityEngine.Object target, string field) where T : class
        {
            if (target == null) return null;
            var f = target.GetType().GetField(field,
                        BindingFlags.Instance | BindingFlags.NonPublic |
                        BindingFlags.Public);
            return f == null ? null : f.GetValue(target) as T;
        }

        static void Set(UnityEngine.Object target, string field, object value)
        {
            var f = target.GetType().GetField(field,
                        BindingFlags.Instance | BindingFlags.NonPublic |
                        BindingFlags.Public);
            if (f == null)
            {
                Debug.LogError("[Echoes] Beat 4: no field '" + field + "' on " +
                               target.GetType().Name);
                _fail++;
                return;
            }

            f.SetValue(target, value);
        }

        // --- measurement -----------------------------------------------------------

        /// <summary>
        /// Ari's body, read off the mover rather than assumed.
        ///
        /// She has no collider, so a scene-wide collider sweep finds nothing
        /// standing under her name and reports the level's single beat marker as
        /// the tallest thing in the village. Reflection is the only honest
        /// source, which is why a failed recompile here shows up as "Ari has no
        /// BodyHeight property" rather than as a plausible wrong number.
        /// </summary>
        static float AriBody(StringBuilder sb, string who, AriMover mover, string prop)
        {
            // Try the property, then fall back to the private field behind it.
            //
            // AriMover does expose BodyHeight/BodyRadius/StepHeight as public
            // read-only properties, and grep finds them there. Reflection still
            // came back null once, which is the known symptom of the project
            // assembly not having been rebuilt since the property was added.
            //
            // Rather than report FAIL and stop, read the serialized field, which
            // exists whether or not the assembly was rebuilt, and say which of
            // the two paths produced the number. A beat built from a stale
            // reading of zero metres is worse than one built from the same
            // value by a different route.
            var type = mover.GetType();

            var p = type.GetProperty(prop, BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null) return Convert.ToSingle(p.GetValue(mover, null));

            var field = Behind(prop);
            var f = field == null ? null
                    : type.GetField(field, BindingFlags.Instance |
                                    BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null)
            {
                Warn(sb, who + "." + prop + " came from the field, not the property",
                     "the project assembly looks stale — run call-recompile.json");
                return Convert.ToSingle(f.GetValue(mover));
            }

            Fail(sb, who + " has neither " + prop + " nor a field behind it — " +
                      "AriMover.cs must have been renamed");
            return 0f;
        }

        /// <summary>
        /// The field name behind one of the three properties this tool reads.
        ///
        /// A switch, not a lower-casing rule. The generic version —
        /// lower-case the first letter and hope — returns null for anything not
        /// anticipated, and a null here is indistinguishable from "this class
        /// changed", which is a much more expensive thing to go and check.
        /// </summary>
        static string Behind(string prop)
        {
            switch (prop)
            {
                case "bodyHeight": return "bodyHeight";
                case "bodyRadius": return "bodyRadius";
                case "stepHeight": return "stepHeight";
                default: return null;
            }
        }

        static float MonoBody(StringBuilder sb, GameObject mono)
        {
            var c = mono.GetComponent<CapsuleCollider>();
            if (c == null) { Fail(sb, "Mono has no CapsuleCollider"); return 0f; }

            // centre.y + height/2, in *world* units. Two things have gone wrong
            // here before: adding a radius term that a capsule does not have
            // (which reported Mono at 2.30 m, taller than Ari, and nearly
            // designed Beat 4 out of existence), and reading height as local
            // when his scale is 0.31, which reported the same wrong 2.30 m for
            // the opposite reason. Both are one line each and both are the
            // kind of error that looks like a finding.
            float h = c.height * Mathf.Abs(mono.transform.lossyScale.y);
            float cy = mono.transform.TransformPoint(c.center).y;
            float bottom = mono.transform.TransformPoint(
                new Vector3(c.center.x, c.center.y - c.height * 0.5f, c.center.z)).y;

            sb.AppendLine("  Mono capsule local " + c.height.ToString("0.00") +
                          " m at scale " + mono.transform.lossyScale.y.ToString("0.00") +
                          " -> " + h.ToString("0.00") + " m, " +
                          "centre y " + cy.ToString("0.00") + ", bottom y " +
                          bottom.ToString("0.00"));

            if (Mathf.Abs((cy - bottom) - h * 0.5f) > 0.05f)
                Fail(sb, "Mono's capsule centre is not half a body above its own " +
                         "bottom — standing height would be wrong by " +
                         ((cy - bottom) - h * 0.5f).ToString("0.00") + " m");

            return h;
        }

        static float MonoVisible(StringBuilder sb, GameObject mono)
        {
            // The top of his world bounds, across every renderer under him.
            //
            // Uses `Renderer.bounds`, which is the local bounds pushed through
            // the renderer's own transform — the same thing
            // CharacterSizeProbe does and the reason it can be trusted. The
            // first version of this read `SkinnedMeshRenderer.localBounds`
            // directly and multiplied by lossyScale, which measures the mesh as
            // it sits in its own file and knows nothing about where the
            // renderer is placed under his root. It reported 0.42 m against a
            // true 0.75 m, and a crawlspace sized off 0.42 m has its roof
            // 33 cm inside his head.
            //
            // Every renderer, not just skinned ones: Mono's hierarchy carries
            // more than one kind, and filtering by type is how the other kind
            // silently stops being measured.
            var renderers = mono.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Fail(sb, "Mono has no Renderer at all, active or not");
                return 0f;
            }

            float lowest = float.MaxValue, highest = float.MinValue;
            int counted = 0;

            foreach (var r in renderers)
            {
                if (r == null) continue;
                var b = r.bounds;
                if (b.min.y < lowest) lowest = b.min.y;
                if (b.max.y > highest) highest = b.max.y;
                counted++;
            }

            if (counted == 0 || highest <= lowest)
            {
                Fail(sb, "Mono has " + renderers.Length +
                         " renderers but none of them reported usable bounds");
                return 0f;
            }

            sb.AppendLine("  Mono " + counted + " renderer(s), world y " +
                          lowest.ToString("0.00") + " to " +
                          highest.ToString("0.00") + " m — visible " +
                          highest.ToString("0.00") + " m");

            return highest;
        }

        static float ErrandRadius(StringBuilder sb, MonoCompanion mono)
        {
            var p = mono.GetType().GetProperty("ErrandRadius",
                        BindingFlags.Instance | BindingFlags.Public);
            if (p == null)
            {
                Fail(sb, "MonoCompanion has no ErrandRadius property — the " +
                         "project assembly has not been rebuilt");
                return 0.7f;
            }

            var v = Convert.ToSingle(p.GetValue(mono, null));
            sb.AppendLine("  Mono errand radius " + v.ToString("0.00") + " m");
            return v;
        }

        static float GroundAt(Vector3 at)
        {
            var hits = Physics.RaycastAll(at + Vector3.up * 30f, Vector3.down, 60f, ~0,
                                          QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return 0f;

            float lowest = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
                if (hits[i].point.y < lowest) lowest = hits[i].point.y;

            return lowest == float.MaxValue ? 0f : lowest;
        }

        /// <summary>
        /// Shortest distance between two axis-aligned boxes. Zero if they
        /// touch or overlap.
        ///
        /// Written out rather than taken from `Bounds`, because the API has
        /// `SqrDistance(Vector3)` — point to box — and nothing for box to box.
        /// The first cut reached for the box overload and it does not compile,
        /// which is the cheapest possible way to find out and the only reason
        /// this is spelled out instead of called.
        ///
        /// Per-axis the gap is how far the intervals miss each other, and if
        /// they overlap on an axis that axis contributes zero — which is
        /// exactly what makes two boxes that intersect report a distance of
        /// zero rather than a negative one.
        /// </summary>
        static float Gap(Bounds a, Bounds b)
        {
            float dx = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
            float dy = Mathf.Max(0f, Mathf.Max(a.min.y - b.max.y, b.min.y - a.max.y));
            float dz = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        static float Headroom(Vector3 at)
        {
            // Starts 0.15 m up, not on the floor. Starting on the floor puts the
            // ray's origin inside the paving slab and it reports the slab's own
            // underside as the ceiling — which is how the first crawlspace map
            // came back with a median of minus one centimetre everywhere.
            const float lift = 0.15f;
            var hits = Physics.RaycastAll(at + Vector3.up * lift, Vector3.up, 6f, ~0,
                                          QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return 6f;

            float lowest = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
                if (hits[i].point.y < lowest) lowest = hits[i].point.y;

            return lowest == float.MaxValue ? 6f : lowest - at.y;
        }

        static bool Swept(Vector3 a, Vector3 b, float height, float radius,
                          float floorY, Transform only)
        {
            // Ari's own sweep, at her own size, with her soles on the floor.
            //
            // The arithmetic that matters: a capsule of total height H and
            // radius r has a cylindrical section of H - 2r, so its sphere
            // centres sit at floorY + r and floorY + H - r. Getting this wrong
            // in the obvious direction — putting the centres half a body above
            // the point the caller happened to pass in — sweeps a capsule from
            // 1.50 m to 2.10 m, which flies straight over a 2.10 m gate and
            // reports the level's most important collision as a pass.
            float half = Mathf.Max(0f, (height - radius * 2f) * 0.5f);
            float centreY = floorY + radius + half;

            Vector3 p0 = new Vector3(a.x, centreY, a.z);
            Vector3 p1 = new Vector3(b.x, centreY, b.z);

            var hits = new RaycastHit[16];
            int n = Physics.CapsuleCastNonAlloc(p0, p1, radius, Vector3.right, hits,
                                                Vector3.Distance(p0, p1), ~0,
                                                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (hits[i].collider == null) continue;
                if (only != null && hits[i].collider.transform != only) continue;
                return true;
            }
            return false;
        }

        // --- reporting ------------------------------------------------------------

        static void Check(StringBuilder sb, string what, bool ok, string detail)
        {
            if (ok) { _pass++; sb.AppendLine("  ok    " + what + " — " + detail); }
            else { _fail++; sb.AppendLine("  FAIL  " + what + " — " + detail); }
        }

        static void Warn(StringBuilder sb, string what, string detail)
        {
            _warn++;
            sb.AppendLine("  warn  " + what + " — " + detail);
        }

        static void Fail(StringBuilder sb, string what)
        {
            _fail++;
            sb.AppendLine("  FAIL  " + what);
            if (_reason == null) _reason = what;
        }

        // --- housekeeping ---------------------------------------------------------

        /// <summary>
        /// Find a game object by name, including inactive ones.
        ///
        /// `GameObject.Find` skips inactive objects, which is a trap specific to
        /// this project: Beat 3 hides Mono and he stays hidden until the tree is
        /// struck, so a tool that runs before the player gets that far reports
        /// a perfectly good scene as having no cast in it.
        /// </summary>
        static GameObject Named(string name)
        {
            var all = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].name == name) return all[i].gameObject;
            return null;
        }

        /// <summary>
        /// Every open scene by name, and how many there are.
        ///
        /// Written as a plain loop into a List because the one-line LINQ
        /// version of this needs `System.Linq` explicitly imported, and every
        /// editor file in this project has been written without it.
        /// </summary>
        static string SceneCount()
        {
            var names = new List<string>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                names.Add(s.name + (s.isLoaded ? "" : " [not loaded]"));
            }
            return names.Count + " (" + string.Join(", ", names.ToArray()) + ")";
        }

        static GameObject Container(StringBuilder sb)
        {
            var go = GameObject.Find(ContainerName);
            if (go != null) return go;

            go = new GameObject(ContainerName);
            sb.AppendLine("created container '" + ContainerName + "'");
            return go;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        static string PathOf(string dir, string file) => System.IO.Path.Combine(dir, file);
    }
}
