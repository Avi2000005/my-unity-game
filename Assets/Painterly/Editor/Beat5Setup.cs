using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// The builders in this folder live in Echoes.Painterly.EditorTools and the
// read-only probes are in the global namespace, and the split is not
// cosmetic. A probe is written so that `run_script` can compile it on its own
// with nothing but UnityEngine in scope — which is only true if it touches no
// project type. A builder cannot be, because it has to reach InkCrawler,
// Beat5Director, BrushPainter, MonoHintLines, BeatWarp and AriMover.
//
// Leaving this file global and solving it with `using Echoes.Painterly;`
// would have compiled too. It would also have been the one builder in the
// folder that does not look like the other two, and the compile that follows
// every rename in this project would be the first place to notice.
namespace Echoes.Painterly.EditorTools
{
public static class Beat5Setup
{
    const string LinesPath = "Assets/Painterly/MonoHintLines.asset";
    const string ModelPath = "Assets/Art/L1/InkCrawler.fbx";
    const string ControllerPath = "Assets/Art/L1/InkCrawler.controller";

    const string Container = "L1_Beat5";
    const string CrawlerName = "Beat5_Crawler";
    const string DirectorName = "Beat5_Director";

    // --- the yard, in metres -----------------------------------------------
    //
    // Measured, not chosen. `WhereIs` over x 38-64 / z 2-14 reported the ground
    // dead flat at 0.00 m, a lane median of 18.00 m and exactly seven village
    // renderers in the whole box — the last of House_35_0's corner at x 37.93,
    // which is why the yard starts at 38 and not at 37.
    //
    // The yard runs x 38-54, z 4-13, with the crawler posted at (49, 10.5).

    // The ruin wall is the whole level design of this beat, so it is worth
    // saying why it is where it is.
    //
    // The first version put the walls to the north and south of the yard, as
    // symmetric cover, which is what a ruin looks like and is useless as a
    // route: a crawler looking at a girl standing in the south of the yard
    // draws a straight line that ends on her *before* it ever reaches a wall
    // off to one side. Cover has to be between the two of them, not beside
    // them, and a wall you walk alongside is a wall that hides nothing.
    //
    // So the wall runs east-west at z 8.2, between the crawler to the north and
    // the southern lane. That single wall is the choice: south of it she is
    // unseen and the crawler is a thing she walks past; north of it she is in
    // the open and has to use the brush. It is deliberately not a full-width
    // slab — the gap at x 46-48 is the honest way through for a player who
    // wants to take the fight, and the gap is a place where the southern lane
    // briefly stops being safe.
    const float YardXMin = 38f, YardXMax = 54f;
    const float YardZMin = 4f, YardZMax = 13f;

    const float WallZ = 8.2f;       // the wall itself
    const float WallGapX = 46f, WallGapX2 = 48f;   // the gap in it
    const float LaneSouthZ = 6.4f;  // where the stealth route runs
    const float LaneFightZ = 12.2f; // north of the crawler, open

    // --- the crawler, placed off the yard's own numbers -------------------
    const float EnterX = 38f;      // she is in the beat past this
    const float ExitX = 51f;      // she is out of it past this
    const float CrawlerOffset = 11f;

    // Ruin walls. Height is the load-bearing number here and it is measured
    // against the crawler's eye, not against Ari: cover that only hides her
    // from something taller than the crawler is not cover.
    const float WallThick = 0.45f;
    const float CoverHeight = 2.20f;   // above the crawler's 0.94 m eye
    // A ruined footing, not cover.
    //
    // The check that matters here is the build's: "the low wall is below his
    // eye, and says so". It failed at 0.95 m, because his eye is at 0.82 —
    // so the low wall was a metre of *real* cover standing on the fight lane,
    // named and commented as though it hid nothing. A thing called a kerb that
    // is taller than the creature it is meant to fail to hide is worse than
    // either a wall or no wall.
    //
    // 0.55 m is a foot-high broken footing: you step over it, a crawler looks
    // straight over it, and it reads as what it is — the reason the open lane
    // is open.
    const float LowWallHeight = 0.55f;

    const float CrawlerX = EnterX + CrawlerOffset;   // 49

    // 2.3 m north of the wall, and that distance is not a preference.
    //
    // With the crawler at z 8.5 — a third of a metre north of the wall's own
    // centre line — its sight line to anything in the southern lane crosses
    // the wall's z band almost immediately, at x within half a metre of the
    // post itself. Every one of those crossings lands inside the east run, so
    // the gap at x 46-48 could never be used: the run east of the gap was
    // permanently in the way of the only opening in it. A gap that no
    // sight line can ever reach is not a gap, it is a mistake in a drawing,
    // and only the ray test found it — it looked correct.
    //
    // Pushed to z 10.5 the crossings spread out across the wall instead of
    // piling up at the post, and a walker at x 45 has a sight line that goes
    // cleanly through the opening. The gap is now worth two metres of
    // exposure, which is what it was drawn to be worth.
    const float CrawlerZ = 10.5f;

    // Measured off the FBX's own renderers, not guessed: `Renderer.bounds`
    // over the instantiated prefab reports y -0.09 to 0.87, so he is 0.96 m
    // tall.
    //
    // The first value was 1.10, on the assumption that a collider wants some
    // headroom above the mesh. That is defensible for collision and it is
    // wrong for the eye, because the eye is `bodyHeight * 0.85` — at 1.10 that
    // is 0.94 m, which is seven centimetres *above the top of his own head*.
    // A creature whose eye sits above its skull does not peer over walls, it
    // has no sight line at all, and every number about cover in this beat
    // would have been measured against an eye that is not on him.
    //
    // So the capsule is his real height and the eye lands at 0.82 m, inside
    // him, which is where an eye on a creature this size belongs.
    const float CrawlerHeight = 0.96f;
    const float CrawlerRadius = 0.34f;

    // How close he comes before he stops, and it is set by the brush, not by
    // taste.
    //
    // The stroke is judged from Ari's own centre to the point the click
    // landed on, which is the surface of his capsule — so what decides whether
    // she can touch him is his standoff *minus his own radius*. At the 1.90 m
    // that was here first, that is 1.56 m against a 1.60 m reach: four
    // centimetres of margin, on a swing aimed with a mouse. It would have
    // read as the brush being broken whenever she was slightly off.
    //
    // 1.30 m puts his near surface at 0.96 m, which is inside her reach with
    // room to spare, and keeps his capsule clear of hers: 1.30 against her
    // 0.30 plus his 0.34 is 0.64 m, so they never end up inside each other.
    const float CloseStandoff = 1.30f;

    // Ari, for the reach numbers.
    const float AriBodyHeight = 1.80f;
    const float AriBodyRadius = 0.30f;
    const float AriStepHeight = 0.35f;
    const float AriWalkSpeed = 2.20f;
    const float AriRunSpeed = 3.60f;

    // The floor the whole yard was designed over: 0.00 m, dead flat across
    // sixteen metres, measured by `WhereIs` over x 38-64 / z 2-14 before
    // anything was built here.
    //
    // Held as a constant on purpose, and checked against every time rather
    // than assumed. The previous build of this beat measured the ground under
    // the crawler at 0.00 m and this one at 1.10 m, with the level's total
    // collider count identical in both — so nothing was added and nothing was
    // removed, and the floor the beat was standing on changed underneath it.
    // That is the kind of drift a tool that re-measures its own baseline can
    // never catch: if the yard's floor is whatever the yard's floor currently
    // is, then a house dropped into the yard becomes the new design.
    const float YardGround = 0f;

    // The brush's own colour radius, so the report can say how wrong it would
    // be to have used it as a combat reach.
    const float BrushColourRadius = 6f;

    const float SplashRadius = 1.6f;
    const float NoticeRadius = 7f;
    const float ForgetRadius = 11f;

    // What each sampled lane is called in the report, in the same order as the
    // lane array below. `static readonly`, not `const`: an array cannot be a
    // compile-time constant in C#, and the first attempt put it inside the
    // method, where `static` is not a valid modifier at all — two CS0106s that
    // both said the same thing about two different words.
    static readonly string[] LaneNames =
    {
        "south lane (the stealth route)",
        "along the wall",
        "north lane (the fight route)"
    };

    public static void Run()
    {
        var sb = new StringBuilder();
        int pass = 0, fail = 0, warn = 0;

        try
        {
            if (Application.isPlaying)
            {
                sb.AppendLine("!! refusing to run in play mode — MarkSceneDirty throws " +
                              "here and the build would stop halfway with nothing to " +
                              "show for it. Stop, then run again.");
                File.WriteAllText(Report, sb.ToString());
                Debug.LogError("[Echoes] beat5: in play mode");
                return;
            }

            Body(sb, ref pass, ref fail, ref warn);
        }
        catch (Exception e)
        {
            sb.AppendLine();
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
            fail++;
        }

        sb.AppendLine();
        sb.AppendLine("VERDICT " + (fail == 0 ? "PASS" : "FAIL") +
                      " — " + pass + " passed, " + fail + " failed, " + warn + " warnings");

        var full = Path.Combine(Directory.GetCurrentDirectory(), Report);
        File.WriteAllText(full, sb.ToString());
        Debug.Log("[Echoes] beat5\n" + sb);
    }

    const string Report = "Temp/beat5_setup.txt";

    static void Body(StringBuilder sb, ref int pass, ref int fail, ref int warn)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            sb.AppendLine("!! no scene open");
            fail++;
            return;
        }

        sb.AppendLine("BEAT 5 — INK CRAWLER, THE BRUSH IS NOT A WEAPON");
        sb.AppendLine();
        sb.AppendLine("scene '" + scene.name + "' at " + scene.path);
        sb.AppendLine("open scenes: " + scene.rootCount);

        var ariGo = Named("Ari");
        var monoGo = Named("Mono");

        sb.AppendLine();
        sb.AppendLine("--- the two bodies, and the third that matters more ---");

        float ariBody = AriBody(sb, "BodyHeight", AriBodyHeight);
        float ariRadius = AriBody(sb, "BodyRadius", AriBodyRadius);
        float ariStep = AriBody(sb, "StepHeight", AriStepHeight);
        float ariVisible = AriVisible(ariGo);

        Check(sb, ref pass, ref fail, "Ari's body is the number the level was designed on",
              Mathf.Abs(ariBody - AriBodyHeight) < 0.01f,
              ariBody.ToString("0.00") + " m swept capsule against " +
              AriBodyHeight.ToString("0.00") + " m assumed");

        // Reports *which* renderer is tall — but the assertion is about the
        // baked pose, not about the animation-wide bounds.
        //
        // Both numbers are printed. The seeded one is what every renderer
        // report in this project prints, and it is the one that made a
        // correctly-authored character look half a metre wrong; the baked one
        // is what the game actually uses. Printing both is the only way a
        // reader can tell them apart later, because they look like the same
        // measurement of the same object and they are not.
        float ariFeetY = float.PositiveInfinity;
        if (ariGo != null)
        {
            foreach (var r in ariGo.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                ariFeetY = Mathf.Min(ariFeetY, r.bounds.min.y);
            }
        }
        if (float.IsPositiveInfinity(ariFeetY)) ariFeetY = 0f;

        bool posed = AriPosedHeight(ariGo, out float poseFeet, out float poseCrown);
        float posedHeight = posed ? poseCrown - poseFeet : 0f;

        sb.AppendLine("  seeded bounds: " + ariFeetY.ToString("F2") + " to " +
                      (ariFeetY + ariVisible).ToString("F2") + ", " +
                      ariVisible.ToString("0.00") +
                      " m — this spans EVERY pose her animation reaches, " +
                      "not her height");
        sb.AppendLine("  baked pose:    " +
                      (posed ? poseFeet.ToString("F2") + " to " +
                               poseCrown.ToString("F2") + ", " +
                               posedHeight.ToString("0.00") + " m standing"
                             : "could not be baked") +
                      " against " + ariBody.ToString("0.00") + " m of capsule");

        // The same full breakdown the crawler gets, because this is now a real
        // measurement rather than an artefact of a seed value and the three
        // possible causes need three different fixes.
        //
        // It is 0.46 m. That is not a rounding error and not a headroom
        // decision: it is 26% of her height, and she is the one body in the
        // level every other beat measured against — the 1.23 m crawl band,
        // the 1.10 m ceiling, the 0.35 m step, all of it computed from a 1.80 m
        // capsule that her mesh does not fit inside.
        if (ariGo != null)
        {
            sb.AppendLine("  Ari root at " + ariGo.transform.position.ToString("F2") +
                          ", local scale " + ariGo.transform.localScale.ToString("F3") +
                          ", lossy scale " + ariGo.transform.lossyScale.ToString("F3"));

            foreach (var r in ariGo.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;

                var t = r.transform;
                var lb = r.localBounds;
                var wb = r.bounds;

                string rel = t.name;
                var walk = t.parent;
                int depth = 0;
                while (walk != null && walk != ariGo.transform && depth < 8)
                {
                    rel = walk.name + "/" + rel;
                    walk = walk.parent;
                    depth++;
                }

                sb.AppendLine("    " + r.GetType().Name + " '" + rel + "'");
                sb.AppendLine("      seeded world y " + wb.min.y.ToString("F3") + " to " +
                              wb.max.y.ToString("F3"));
                sb.AppendLine("      localBounds y " + lb.min.y.ToString("F3") + " to " +
                              lb.max.y.ToString("F3") + ", " +
                              (lb.max.y - lb.min.y).ToString("F3") +
                              " m across the whole animation");
                sb.AppendLine("      localScale " + t.localScale.ToString("F3") +
                              "   lossyScale " + t.lossyScale.ToString("F3") +
                              "   localPos " + t.localPosition.ToString("F3"));
            }
        }

        // The claim: standing up, she fits the body physics sweeps for her.
        //
        // Ten centimetres of tolerance, and the number is printed so it reads
        // as a decision rather than a fudge. A crown a couple of centimetres
        // proud of the capsule is ordinary in a game and invisible in motion;
        // it only shows at a wall, as the difference between a head clipping
        // and a head grazing. Half a metre is not that.
        const float crownTolerance = 0.10f;

        Check(sb, ref pass, ref fail, "Ari fits the body physics sweeps for her",
              posed && posedHeight <= ariBody + crownTolerance,
              !posed
                  ? "her skin could not be baked, so her standing height is unknown " +
                    "and this is untested rather than passed"
                  : "she stands " + posedHeight.ToString("0.00") + " m against " +
                    ariBody.ToString("0.00") + " m of capsule, so her crown is " +
                    (posedHeight - ariBody).ToString("0.00") + " m " +
                    (posedHeight <= ariBody
                        ? "inside it — nothing of her is outside what physics " +
                          "knows about"
                        : "outside it, and this is the beat that shoves her, so " +
                          "that is how much of her can end up inside the creature"));

        sb.AppendLine("  Ari: body " + ariBody.ToString("0.00") + " m, radius " +
                      ariRadius.ToString("0.00") + " m, step " + ariStep.ToString("0.00") +
                      " m, visible " + ariVisible.ToString("0.00") + " m");

        // The crawler is instantiated later, so his numbers are checked there.
        // What matters at this point is only that the design assumption about
        // him is stated and will be measured.

        // --- the ground, measured on a yard with nothing of ours on it -------

        // This block used to sit *above* `GetOrAdd`/`Clear`, on the reasonable
        // assumption that "the ground before anything is built on it" meant
        // "before this run builds anything". It did not. The previous run's
        // build was still standing in the scene, and it was standing exactly
        // on the point being probed.
        //
        // So `GroundUnder(CrawlerX, CrawlerZ)` cast down and hit the top of the
        // crawler the last build had put there — whose capsule was 1.10 m tall
        // — and reported **1.10 m** as the floor. The yard was not 1.10 m of
        // fall; the tool was standing on its own previous answer. It then put
        // the new crawler 1.10 m up, on top of that number, and the build
        // after that would have measured 2.20.
        //
        // It is the same failure as reading a body's height off the origin
        // instead of its own feet: a measurement that includes the thing it
        // made last time, in a spot chosen because it was the one point known
        // to be occupied. The report did not drift, it climbed.
        //
        // Two independent guards, because either alone leaves the other in
        // place: the container is cleared first, *and* the probe refuses to
        // count anything this beat owns. If `Clear` ever misses a child again
        // the answer is still the ground rather than the remains.
        var container = GetOrAdd(Container);
        Clear(container);
        Physics.SyncTransforms();

        float ground = GroundUnder((YardXMin + YardXMax) * 0.5f, (YardZMin + YardZMax) * 0.5f);
        float groundEast = GroundUnder(CrawlerX, CrawlerZ);
        float groundWest = GroundUnder(EnterX + 1f, CrawlerZ);

        sb.AppendLine();
        sb.AppendLine("--- the ground the yard is built on ---");
        sb.AppendLine("  measured after this beat's own geometry was cleared, and " +
                      "with anything under " + Container + " refused by the probe");
        sb.AppendLine("  west " + groundWest.ToString("0.00") + " m, centre " +
                      ground.ToString("0.00") + " m, crawler " +
                      groundEast.ToString("0.00") + " m");

        // NaN propagates through Max/Min in a way that does not stay NaN, and
        // a hole reported as a flat yard is the last thing this check should
        // be able to do. So the absence of ground is its own finding, stated
        // first, and only then is a spread computed — over real numbers.
        Check(sb, ref pass, ref fail, "there is ground under the yard at all",
              !float.IsNaN(ground) && !float.IsNaN(groundEast) && !float.IsNaN(groundWest),
              (float.IsNaN(ground) || float.IsNaN(groundEast) || float.IsNaN(groundWest)
                  ? "a probe at (" + (float.IsNaN(ground) ? "yard centre"
                          : float.IsNaN(groundWest) ? "yard west" : "crawler post") +
                    ") hit nothing but this beat's own cleared geometry, so there is " +
                    "no floor there to fight on. Everything below is reported against " +
                    "the designed " + YardGround.ToString("0.00") + " m and is not measured"
                  : "all three probes hit real geometry, not this beat's and not a hole"));

        // The floor is a *design* number, held as a constant, not something
        // re-derived from whatever happens to be under the cursor this run.
        // A baseline that moves with the thing it measures cannot report that
        // the thing moved.
        Check(sb, ref pass, ref fail, "the yard sits on the floor it was designed over",
              Mathf.Abs(FloorOf(ground) - YardGround) < 0.05f &&
              Mathf.Abs(FloorOf(groundEast) - YardGround) < 0.05f &&
              Mathf.Abs(FloorOf(groundWest) - YardGround) < 0.05f,
              "all three probes at " + FloorOf(ground).ToString("0.00") + ", " +
              FloorOf(groundEast).ToString("0.00") + " and " +
              FloorOf(groundWest).ToString("0.00") + " m against a designed floor of " +
              YardGround.ToString("0.00") + " m" +
              (Mathf.Abs(FloorOf(ground) - YardGround) >= 0.05f ||
               Mathf.Abs(FloorOf(groundEast) - YardGround) >= 0.05f ||
               Mathf.Abs(FloorOf(groundWest) - YardGround) >= 0.05f
                  ? ". Anything standing here is not the yard: every sight line, " +
                    "every lane percentage and every creature position below was " +
                    "cast at this height, so they are all wrong together and no " +
                    "single one of them will show it"
                  : ""));

        float g0 = FloorOf(ground), g1 = FloorOf(groundEast), g2 = FloorOf(groundWest);
        float spread = Mathf.Max(g0, Mathf.Max(g1, g2)) - Mathf.Min(g0, Mathf.Min(g1, g2));
        Check(sb, ref pass, ref fail, "the yard is flat enough to fight on",
              spread <= 0.05f,
              spread.ToString("0.00") + " m of fall across " +
              (YardXMax - YardXMin).ToString("0") + " m" +
              (spread > 0.05f ? " — the crawler will slide downhill on top of Ari" : ""));

        // Everything placed from here uses the measured floor where there is
        // one and the designed floor where there is not, so a missing probe
        // degrades the build to "designed, unchecked" instead of poisoning a
        // Vector3 with NaN and taking the whole hierarchy with it.
        ground = g0;
        groundEast = g1;
        groundWest = g2;

        // --- build ------------------------------------------------------------


        sb.AppendLine();
        sb.AppendLine("--- built ---");

        var stone = LoadMat("RockTrim");
        var brick = LoadMat("Brick");

        // The ruin, in two runs with a gap between them, plus a low stub.
        //
        // The main run is east-west at z 8.2, and it is the only thing in this
        // beat that is load-bearing. See the comment on WallZ for why it has to
        // be between her and the crawler rather than beside them.
        //
        // Deliberately not one slab. A continuous wall from x 39 to x 52 is a
        // corridor with an invisible lid: there would be exactly one answer, and
        // a player who did not spot it would have to fight. The gap at x 46-48
        // is the honest alternative — and it is also where the southern lane
        // stops being safe for two metres, so taking it is a decision with a
        // cost rather than a free route.
        var walls = new List<Wall>
        {
            new Wall("west run", (YardXMin + 1f + WallGapX) * 0.5f, WallZ,
                     WallGapX - (YardXMin + 1f), WallThick, CoverHeight),
            new Wall("east run", (WallGapX2 + CrawlerX + 3f) * 0.5f, WallZ,
                     CrawlerX + 3f - WallGapX2, WallThick, CoverHeight),

            // Low, and north-west of the crawler in the open. It breaks the
            // ground into two so the fight lane has somewhere to be lost, and
            // it is honest that it hides nothing: a crawler can see straight
            // over 0.95 m and will, which is why it is not called cover.
            //
            // Placed on the far side of the crawler from the gap on purpose —
            // a player who took the gap walks into the open, and this is what
            // they walk into.
            new Wall("north stub", CrawlerX - 5f, 11f, 0.45f, 3.0f, LowWallHeight)
        };

        int made = 0;
        foreach (var w in walls)
        {
            MakeWall(container.transform, w, stone);
            made++;
            sb.AppendLine("  " + w.name + ": " + w.sizeX.ToString("0.0") + " x " +
                          w.sizeZ.ToString("0.0") + " x " + w.height.ToString("0.0") +
                          " m at (" + w.x.ToString("0.0") + ", " + w.z.ToString("0.0") + ")");
        }
        sb.AppendLine("  " + made + " ruin wall(s) in the yard");

        // --- the crawler ------------------------------------------------------

        var crawlerGo = Prefab(ModelPath, CrawlerName, container.transform,
                               new Vector3(CrawlerX, groundEast, CrawlerZ));

        sb.AppendLine();
        sb.AppendLine("--- the crawler, placed and measured ---");
        if (crawlerGo == null)
        {
            Fail(sb, ref pass, ref fail,
                 "could not instantiate " + ModelPath + " — the beat has no antagonist");
        }
        else
        {
            SetController(crawlerGo, ControllerPath);

            // Seeded from the first renderer, not from zero.
            //
            // `float top = 0f, bottom = 0f` reads as a safe identity — zero is
            // neither high nor low, is it not — and it silently assumes the
            // model straddles the world origin. Nothing in this level does.
            // Ari's root sits at y 0.25, the crawler's is placed on the ground
            // it was measured under, and every prefab instance in the village
            // is somewhere above the plane.
            //
            // So `bottom` never moved off its seed: it reported 0.00 for any
            // model that sat entirely above the ground, and only picked up a
            // real number when some part of the mesh happened to hang below
            // zero. It reported the crawler as 0.96 m tall on one build and
            // 1.97 m on the next, from the same asset at the same scale, with
            // the difference being entirely the seed — and the only thing that
            // changed between the builds was the floor it was placed on, which
            // lifted the whole model above the origin and made the seed
            // visible for the first time.
            //
            // A measurement with an implicit assumption in its initial value
            // is not a measurement. It agrees with you until the day the
            // assumption stops holding, and then it does not fail — it
            // reports a number.
            float top = float.NegativeInfinity;
            float bottom = float.PositiveInfinity;
            int renderers = 0;
            foreach (var r in crawlerGo.GetComponentsInChildren<Renderer>(true))
            {
                var b = r.bounds;
                top = Mathf.Max(top, b.max.y);
                bottom = Mathf.Min(bottom, b.min.y);
                renderers++;
            }

            if (renderers == 0) { top = 0f; bottom = 0f; }

            float visible = top - bottom;
            sb.AppendLine("  " + renderers + " renderer(s), world y " +
                          bottom.ToString("0.00") + " to " + top.ToString("0.00") +
                          " m, so " + visible.ToString("0.00") + " m tall");

            // Every renderer, in full, because "1.97 m tall" against a model
            // that measured 0.96 m on the previous build of the same scene is
            // not a size that can be corrected by picking a number.
            //
            // The top stayed at +0.87 m in both builds and the *bottom* moved
            // from -0.09 to -1.10. Something is hanging a metre below him, and
            // "one renderer" does not say what. The three candidates are
            // completely different problems with completely different fixes: a
            // child node at a large negative local offset (move it), a scaled
            // node inflating its own bounds (unscale it), or a skinned mesh
            // whose bind-pose bounds are being read in the wrong space (read
            // `localBounds` and multiply by `lossyScale` instead of `bounds`).
            //
            // So: world bounds, local bounds, local scale, world scale, and
            // the chain of parents with their scales. The chain is the part
            // that usually explains it, because the offending node is almost
            // never the renderer itself.
            sb.AppendLine("  crawler root at " + crawlerGo.transform.position.ToString("F2") +
                          ", local scale " + crawlerGo.transform.localScale.ToString("F3") +
                          ", lossy scale " + crawlerGo.transform.lossyScale.ToString("F3"));

            foreach (var r in crawlerGo.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;

                var t = r.transform;
                var lb = r.localBounds;
                var wb = r.bounds;

                string pathToRoot = t.name;
                var walk = t.parent;
                int depth = 0;
                while (walk != null && walk != crawlerGo.transform && depth < 8)
                {
                    pathToRoot = walk.name + "/" + pathToRoot;
                    walk = walk.parent;
                    depth++;
                }

                sb.AppendLine("    " + r.GetType().Name + " '" + pathToRoot + "'");
                sb.AppendLine("      world  y " + wb.min.y.ToString("F3") + " to " +
                              wb.max.y.ToString("F3") + "   (" +
                              (wb.max.y - wb.min.y).ToString("F3") + " m tall)");
                sb.AppendLine("      local  y " + lb.min.y.ToString("F3") + " to " +
                              lb.max.y.ToString("F3") + "   (" +
                              (lb.max.y - lb.min.y).ToString("F3") + " m tall, " +
                              "in the mesh's own file)");
                sb.AppendLine("      localScale " + t.localScale.ToString("F3") +
                              "   lossyScale " + t.lossyScale.ToString("F3") +
                              "   localPos " + t.localPosition.ToString("F3"));
                sb.AppendLine("      localBounds x lossyScale would be y " +
                              (lb.min.y * t.lossyScale.y).ToString("F3") + " to " +
                              (lb.max.y * t.lossyScale.y).ToString("F3"));
            }

            Check(sb, ref pass, ref fail, "the crawler is standing on the ground",
                  Mathf.Abs(bottom) <= 0.10f,
                  "lowest point " + bottom.ToString("0.00") + " m against ground " +
                  groundEast.ToString("0.00") + " m" +
                  (bottom < -0.10f
                      ? " — " + (-bottom).ToString("0.00") + " m of him is buried. " +
                        "Mono had the same 0.12 m of sunk mesh and it needs the " +
                        "renderer child raised by 0.387 local units, not the " +
                        "root moved, or his collider lifts off the floor too."
                      : ""));

            float head = top;

            Check(sb, ref pass, ref fail, "the crawler is the height the cover was sized against",
                  Mathf.Abs(visible - CrawlerHeight) <= 0.25f,
                  visible.ToString("0.00") + " m against " + CrawlerHeight.ToString("0.00") +
                  " m assumed, tolerance 0.25 m");

            // The eye, and therefore the cover.
            float eye = head * 0.85f;
            sb.AppendLine("  his eye at 85% is " + eye.ToString("0.00") +
                          " m; the tall cover is " + CoverHeight.ToString("0.00") +
                          " m, the low wall " + LowWallHeight.ToString("0.00") + " m");

            Check(sb, ref pass, ref fail, "the tall cover is actually above his eye",
                  CoverHeight > eye,
                  CoverHeight.ToString("0.00") + " m wall against a " +
                  eye.ToString("0.00") + " m eye — " +
                  (CoverHeight > eye
                      ? "he cannot see through it"
                      : "he can see straight over the only thing she was meant to hide behind"));

            Check(sb, ref pass, ref fail, "the low wall is below his eye, and says so",
                  LowWallHeight < eye,
                  LowWallHeight.ToString("0.00") + " m against a " +
                  eye.ToString("0.00") + " m eye — it hides nothing and must not " +
                  "look as though it does");

            // --- the no-kill check -------------------------------------------

            EnsureCollider(sb, ref pass, ref fail, crawlerGo);

            var crawler = crawlerGo.AddComponent<InkCrawler>();
            Set(crawler, "bodyHeight", CrawlerHeight);
            Set(crawler, "bodyRadius", CrawlerRadius);
            Set(crawler, "home", new Vector3(CrawlerX, groundEast, CrawlerZ));
            Set(crawler, "splashRadius", SplashRadius);
            Set(crawler, "noticeRadius", NoticeRadius);
            Set(crawler, "forgetRadius", ForgetRadius);

            // The standoff is a brush-reach number, and the brush is not
            // reachable at the component's own default — see CloseStandoff.
            // It was left unset for two builds, so the fight was tuned around
            // a value the build tool was checking and the game was not using.
            Set(crawler, "standoffRange", CloseStandoff);

            if (ariGo != null) Set(crawler, "ari", ariGo.GetComponent<AriMover>());

            var monoComp = monoGo != null ? monoGo.GetComponent<MonoCompanion>() : null;
            if (monoComp != null) Set(crawler, "mono", monoComp);

            NoKillCheck(sb, ref pass, ref fail, ref warn, crawler, crawlerGo);

            // --- the director --------------------------------------------------

            var dirGo = new GameObject(DirectorName);
            dirGo.transform.SetParent(container.transform, false);
            dirGo.transform.position = new Vector3(EnterX + 1.5f, groundWest, WallZ);

            var dir = dirGo.AddComponent<Beat5Director>();
            Set(dir, "crawler", crawler);
            Set(dir, "enterX", EnterX);
            Set(dir, "exitX", ExitX);
            Set(dir, "crawlerOffset", CrawlerOffset);
            if (ariGo != null) Set(dir, "brush", ariGo.GetComponent<BrushPainter>());
            if (monoComp != null) Set(dir, "mono", monoComp);

            Verify(sb, ref pass, ref fail, crawlerGo, crawler, dir, dirGo,
                   groundEast, ariBody, ariRadius, ariStep);
        }

        // --- the lines --------------------------------------------------------

        BuildLines(sb, ref pass, ref fail);

        // --- the warp ---------------------------------------------------------

        sb.AppendLine();
        sb.AppendLine("--- the test warp ---");
        AddWarpStop(sb, ref pass, ref fail, ref warn, ground);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.EditorUtility.SetDirty(container);
    }

    /// <summary>
    /// Append this beat's stop to the existing warp.
    ///
    /// It appends and re-keys by name rather than re-running Beat 4's tool,
    /// which rebuilds Beat 4's whole wall and would move a beat that has just
    /// been signed off in order to save someone a walk. Same reasoning as the
    /// hint lines: a shared asset is edited in place, by this beat's own id.
    /// </summary>
    static void AddWarpStop(StringBuilder sb, ref int pass, ref int fail,
                            ref int warn, float ground)
    {
        var warp = UnityEngine.Object.FindAnyObjectByType<BeatWarp>(FindObjectsInactive.Include);
        if (warp == null)
        {
            Warn(sb, ref warn,
                 "no BeatWarp in the level, so Beat 5 has no test stop. Re-run " +
                 "Beat 4's tool if the N and B keys are not working.");
            return;
        }

        var stops = Src<List<BeatWarp.Stop>>(warp, "stops");
        if (stops == null)
        {
            Fail(sb, ref pass, ref fail, "BeatWarp has no stops list to add to");
            return;
        }

        const string mine = "beat5_enter";
        stops.RemoveAll(s => s != null && s.name == mine);

        var ari = UnityEngine.Object.FindAnyObjectByType<AriMover>(FindObjectsInactive.Include);
        var monoGo = Named("Mono");

        stops.Add(new BeatWarp.Stop
        {
            name = mine,
            // In the southern lane, one metre past the entrance line, so the
            // beat starts on arrival.
            //
            // Standing her *in* the lane rather than beside it is the whole
            // reason to use a warp for this beat: from here the wall is on her
            // right with the crawler visible past the end of it, and the yard is
            // open to her left. Both routes are in one frame, which is the
            // decision the beat exists to pose. Land her on the open side and
            // she has already been seen and the choice was made for her.
            ari = new Vector3(EnterX + 1.0f, ground, LaneSouthZ),
            mono = monoGo != null
                ? new Vector3(EnterX - 0.6f, ground, LaneSouthZ)
                : Vector3.zero
        });

        Set(warp, "index", stops.Count - 1);

        Check(sb, ref pass, ref fail, "there is a warp stop for this beat",
              stops.Count > 0,
              warp.Count + " stop(s) now, this one is '" + mine + "' at " +
              stops[stops.Count - 1].ari.ToString("F2"));

        Check(sb, ref pass, ref fail, "the stop is past the beat's entrance",
              stops[stops.Count - 1].ari.x > EnterX,
              "x " + stops[stops.Count - 1].ari.x.ToString("0.00") +
              " against an entrance at " + EnterX.ToString("0") + " — " +
              (stops[stops.Count - 1].ari.x > EnterX
                  ? "the beat starts on arrival"
                  : "she would have to walk into it"));

        Check(sb, ref pass, ref fail, "the stop is short of the crawler",
              CrawlerX - stops[stops.Count - 1].ari.x > NoticeRadius,
              (CrawlerX - stops[stops.Count - 1].ari.x).ToString("0.0") +
              " m from the post against " + NoticeRadius.ToString("0") +
              " m of notice — she lands " +
              (CrawlerX - stops[stops.Count - 1].ari.x - NoticeRadius).ToString("0.0") +
              " m outside it and walks in under her own steam" +
              (CrawlerX - stops[stops.Count - 1].ari.x <= NoticeRadius
                  ? " — she is spotted the frame she lands, which is not a first " +
                    "look at a crawler, it is the start of a chase"
                  : ""));
    }

    // --- the checks that make this beat --------------------------------------

    /// <summary>
    /// It has no health and no way to be killed.
    ///
    /// Checked three ways, because "I did not write a damage method" is a
    /// statement about the file rather than about the component: every member
    /// by reflection, every animator trigger it can send, and every transition
    /// reachable from the death state. The brief for Level 1 says the crawlers
    /// are teaching crawlers and that nothing is lethal yet, and the cheapest
    /// way to break that promise later is to add one field without thinking.
    ///
    /// The controller *does* have a terminal Crawler_Death state and a Die
    /// trigger, because the controller is shared with the cast and a later
    /// level needs them. So the check is not "the asset has no death" — it is
    /// "nothing in this beat can reach it".
    /// </summary>
    /// <summary>
    /// Give the crawler a collider that is the same shape as the one it
    /// collides with, and say what it found.
    ///
    /// <para>These have to be one shape, not two similar ones.</para>
    ///
    /// <c>InkCrawler</c> has no rigidbody and no collider of its own — it
    /// moves by <c>Travel</c>, a capsule cast of exactly
    /// <c>bodyHeight</c> and <c>bodyRadius</c>. So the collider on this object
    /// serves two jobs at once and both must be the same shape: it is what
    /// Ari's own capsule is stopped by, and it is what her brush click
    /// raycasts into, because <c>BrushPainter</c> casts with no layer mask and
    /// can only be aimed at a creature that has something to hit.
    ///
    /// A mesh collider off the FBX would satisfy the second job and quietly
    /// spoil the first — it is the shape of an artist's spider, not of the
    /// capsule the component actually sweeps with, and the two would disagree
    /// about where his edge is by most of a body's width. She would be stopped
    /// by a wall he walks through, or she would be able to stand inside him.
    ///
    /// Anything the FBX brought is disabled rather than deleted, so the shape
    /// it arrived with is still inspectable in the scene and the report can
    /// print it.
    /// </summary>
    static void EnsureCollider(StringBuilder sb, ref int pass, ref int fail,
                               GameObject go)
    {
        var found = go.GetComponentsInChildren<Collider>(true);
        var kept = new List<string>();

        for (int i = 0; i < found.Length; i++)
        {
            var c = found[i];
            if (c == null) continue;
            kept.Add(c.GetType().Name + " '" + c.name + "' " +
                     c.bounds.size.ToString("F2"));
            c.enabled = false;
        }

        var capsule = go.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = go.AddComponent<CapsuleCollider>();

        capsule.enabled = true;
        capsule.isTrigger = false;
        capsule.direction = 1;                        // Y
        capsule.height = Mathf.Max(CrawlerHeight, CrawlerRadius * 2f + 0.01f);
        capsule.radius = CrawlerRadius;
        // Centred on the root, which sits on the floor, so the capsule runs
        // 0 to CrawlerHeight and the bottom sphere is a radius up — the same
        // shape CapsuleAt builds for the cast, so the two cannot drift.
        capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);

        sb.AppendLine("  collider: a capsule " + capsule.radius.ToString("0.00") +
                      " r x " + capsule.height.ToString("0.00") + " h at y " +
                      capsule.center.y.ToString("0.00") +
                      (kept.Count == 0
                          ? ", nothing on the FBX to replace"
                          : "; disabled from the FBX: " + string.Join(", ",
                                                                kept.ToArray())));

        Check(sb, ref pass, ref fail, "the crawler is a solid capsule he can be aimed at",
              capsule.enabled && capsule.radius > 0f && capsule.height >= CrawlerRadius * 2f,
              "radius " + capsule.radius.ToString("0.00") + " m, height " +
              capsule.height.ToString("0.00") + " m, trigger " +
              (capsule.isTrigger ? "YES — clicks ignore triggers only if they ask " +
                                  "to, and Ari's cast does not"
                                : "no"));
    }

    static void NoKillCheck(StringBuilder sb, ref int pass, ref int fail,
                            ref int warn, InkCrawler crawler, GameObject go)
    {
        var t = crawler.GetType();

        var bad = new List<string>();
        foreach (var m in t.GetMembers())
        {
            string n = m.Name.ToLowerInvariant();
            if (n.Contains("health") || n.Contains("damage") || n.Contains("hurt") ||
                n.Contains("die") || n.Contains("kill") || n.Contains("death") ||
                n.Contains("hitpoint") || n == "hp")
                bad.Add(m.Name);
        }

        Check(sb, ref pass, ref fail, "the crawler component has no way to be hurt or killed",
              bad.Count == 0,
              bad.Count == 0
                  ? "no member of " + t.Name + " mentions health, damage, hurt, die, kill or death"
                  : "found: " + string.Join(", ", bad.ToArray()));

        // What can it actually send the animator?
        var anim = go.GetComponentInChildren<Animator>(true);
        if (anim == null)
        {
            Warn(sb, ref warn, "no Animator on the crawler, so no trigger can be checked");
            return;
        }

        Check(sb, ref pass, ref fail, "the crawler is running the built controller",
              anim.runtimeAnimatorController != null,
              anim.runtimeAnimatorController != null
                  ? anim.runtimeAnimatorController.name
                  : "NONE — it would stand in its bind pose for the whole beat");

        // Grep the source for the one trigger that must never be set. A
        // reflection check cannot see a string literal, and this is a string.
        string src = "";
        string[] files = Directory.GetFiles(
            Path.Combine(Directory.GetCurrentDirectory(), "Assets/Painterly/Scripts"),
            "InkCrawler.cs");
        if (files.Length > 0) src = File.ReadAllText(files[0]);

        Check(sb, ref pass, ref fail, "nothing in the crawler ever fires the death trigger",
              !src.Contains("\"Die\"") && !src.Contains("dieTrigger") &&
              !src.Contains("deathTrigger"),
              "InkCrawler.cs mentions the death trigger " +
              (src.Contains("\"Die\"") ? "and uses it" : "only in comments"));

        // And the graph must leave Death alone.
        //
        // `runtimeAnimatorController` is the *base* type, which has no `layers`
        // — the layers live on AnimatorController, the subclass Unity creates
        // for a .controller asset. Reading it off the base was CS1061.
        // Cast, and say so if the cast fails rather than silently skipping the
        // check: a tool that quietly checks nothing is worse than one that
        // refuses to run.
        var ctrl = anim.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
        if (ctrl == null)
        {
            Warn(sb, ref warn,
                 "the crawler's controller is a " +
                 (anim.runtimeAnimatorController == null
                     ? "null"
                     : anim.runtimeAnimatorController.GetType().Name) +
                 ", not an AnimatorController, so the death-state transitions " +
                 "could not be counted. The no-kill guarantee still rests on " +
                 "the two checks above it.");
        }
        else
        {
            int intoDeath = 0;
            foreach (var layer in ctrl.layers)
            {
                foreach (var cs in layer.stateMachine.states)
                    foreach (var tr in cs.state.transitions)
                        if (tr.destinationState != null &&
                            tr.destinationState.name == "Crawler_Death") intoDeath++;
            }

            // Transitions INTO death exist — that is correct, the controller was
            // built for a level that needs them. What must not exist is a way to
            // set the trigger, which is the line above. Both are reported so a
            // future change to the asset cannot quietly remove the guard.
            sb.AppendLine("  the controller has " + intoDeath + " transition(s) into " +
                          "Crawler_Death; none of them are reachable from this beat, " +
                          "because nothing sets the trigger");
        }
    }

    static void Verify(StringBuilder sb, ref int pass, ref int fail,
                       GameObject crawlerGo, InkCrawler crawler,
                       Beat5Director dir, GameObject dirGo,
                       float ground, float ariBody, float ariRadius, float ariStep)
    {
        Physics.SyncTransforms();
        var ari = UnityEngine.Object.FindAnyObjectByType<AriMover>(FindObjectsInactive.Include);

        sb.AppendLine();
        sb.AppendLine("--- the fight, in numbers ---");

        float notice = Src(crawler, "noticeRadius", NoticeRadius);
        float forget = Src(crawler, "forgetRadius", ForgetRadius);
        float standoff = Src(crawler, "standoffRange", 1.9f);
        float lungeRange = Src(crawler, "lungeRange", 2.2f);
        float lungeReach = Src(crawler, "lungeReach", 1.1f);
        float stagger = Src(crawler, "staggerSeconds", 1.4f);
        float giveUp = Src(crawler, "giveUpSeconds", 6f);
        float crawlSpd = Src(crawler, "crawlSpeed", 1.5f);
        float closeSpd = Src(crawler, "closeSpeed", 2.6f);

        Check(sb, ref pass, ref fail, "it notices before it is on top of her",
              notice > lungeRange + ariRadius,
              notice.ToString("0.00") + " m notice against a " +
              lungeRange.ToString("0.00") + " m lunge and a " +
              ariRadius.ToString("0.00") + " m body radius");

        // Measured to the surface, because that is where the click lands.
        //
        // `Splash` is handed the point the ray hit and asks how far *that* is
        // from Ari, so the number that decides whether she can touch it is the
        // distance to the near face of his capsule: standoff minus his own
        // radius. The first version compared centre-to-centre instead
        // (standoff + her radius against the reach), added her radius in
        // where it did not belong, and then printed the sentence "inside a
        // 1.60 m stroke" *underneath a FAIL for being 2.20 m away*.
        //
        // A check whose own explanation contradicts its verdict is worse than
        // no check: it was read as passing, or as the design being broken,
        // while the number it was actually computing had nothing to do with
        // the swing. The printed line has to be the same measurement as the
        // assertion, or it is a rumour.
        float gapToSkin = standoff - Src(crawler, "bodyRadius", CrawlerRadius);

        Check(sb, ref pass, ref fail, "it stops with the brush in range",
              gapToSkin <= SplashRadius - 0.30f,
              "it holds " + standoff.ToString("0.00") + " m off and his capsule is " +
              Src(crawler, "bodyRadius", CrawlerRadius).ToString("0.00") +
              " m, so the near face of it is " + gapToSkin.ToString("0.00") +
              " m from her centre against a " + SplashRadius.ToString("0.00") +
              " m stroke" +
              (gapToSkin > SplashRadius - 0.30f
                  ? " — she cannot reach him where he stops, so the brush would " +
                    "connect only by accident and the fight could not be taught"
                  : ", with " + (SplashRadius - gapToSkin).ToString("0.00") +
                    " m to spare for a swing aimed with a mouse"));

        Check(sb, ref pass, ref fail, "its standoff is not so tight she cannot swing",
              standoff >= ariRadius + 0.4f,
              standoff.ToString("0.00") + " m — closer than " +
              (ariRadius + 0.4f).ToString("0.00") +
              " m and the two bodies are inside each other");

        Check(sb, ref pass, ref fail, "the splash reach is an arm, not the brush",
              SplashRadius < BrushColourRadius,
              "stroke reach " + SplashRadius.ToString("0.00") +
              " m against the brush's own colour radius " +
              BrushColourRadius.ToString("0.00") + " m" +
              (SplashRadius >= BrushColourRadius
                  ? " — she could stagger it from across the yard by clicking the ground"
                  : ", so clicking the ground beside her cannot reach it"));

        Check(sb, ref pass, ref fail, "a brush sweep past it would connect",
              SplashRadius >= lungeReach,
              SplashRadius.ToString("0.00") + " m reach against a " +
              lungeReach.ToString("0.00") + " m lunge it can throw at her");

        // Walking does not resolve it, in *either* direction, and both halves
        // matter.
        //
        // This check used to assert `crawlSpeed > 2.2` — that it is faster than
        // her walk — and print "she could walk away and never be taught
        // anything" as the failure. That was the design upside down. A creature
        // faster than the girl cannot make stealth optional: every encounter
        // becomes a chase, cover stops being worth anything because running is
        // always the answer, and the first fight in a level that has to teach
        // the brush teaches running instead.
        //
        // So the property to assert is the one the beat is built on. It is
        // slower than her walk, so she is never forced to fight; and the yard's
        // exit is on the far side of its post, so walking is not leaving either
        // — she has to get past it somehow, and cover is how.
        Check(sb, ref pass, ref fail, "walking is never an answer, and she is never forced to swing",
              crawlSpd < AriWalkSpeed && (ExitX - CrawlerX) > 0f,
              "it crawls at " + crawlSpd.ToString("0.00") + " m/s against her " +
              AriWalkSpeed.ToString("0.00") + " m/s walk, so she is never caught on " +
              "foot — and its post is " + (ExitX - CrawlerX).ToString("0.00") +
              " m short of the exit, so walking east is walking towards it, not " +
              "away" +
              (crawlSpd >= AriWalkSpeed
                  ? " — it is FASTER than she is, so every encounter becomes a " +
                    "chase and cover is worth nothing"
                  : ""));

        Check(sb, ref pass, ref fail, "it can always be outrun",
              closeSpd < 3.6f,
              closeSpd.ToString("0.00") + " m/s against her 3.60 m/s run" +
              (closeSpd >= 3.6f
                  ? " — and it is FASTER than she is, so the stealth route is " +
                    "not actually a route and she can be caught no matter what she does"
                  : ", so leaving is always available"));

        Check(sb, ref pass, ref fail, "it forgets more slowly than it notices",
              forget > notice,
              forget.ToString("0.00") + " m forget against " +
              notice.ToString("0.00") + " m notice");

        Check(sb, ref pass, ref fail, "giving up is quick enough to finish a stealth run",
              giveUp <= 10f,
              giveUp.ToString("0.0") + " s of not seeing her, and the yard is " +
              (ExitX - EnterX).ToString("0") + " m long");

        Check(sb, ref pass, ref fail, "a stagger outlasts the lunge it interrupts",
              stagger > Src(crawler, "lungeSeconds", 0.55f),
              stagger.ToString("0.00") + " s stagger against a " +
              Src(crawler, "lungeSeconds", 0.55f).ToString("0.00") +
              " s lunge" + (stagger <= Src(crawler, "lungeSeconds", 0.55f)
                  ? " — she can hit it and it still lands on her"
                  : ", so a clean hit wins outright"));

        // --- the stealth route, measured ----------------------------------

        sb.AppendLine();
        sb.AppendLine("--- does the ruin wall do anything? ---");

        // For every half metre of each lane, from the entrance to the exit,
        // ask whether the crawler sitting on its post could see her. Report the
        // blocked fraction, and where the first exposed sample is.
        //
        // This is deliberately *not* "is there a lane that is never seen". That
        // question has no answer in this yard and asking it anyway is what made
        // the first version of this check report FAIL against a design that
        // worked: the exit is three metres from the crawler's post, so a lane
        // that was never seen could not exist. The question that matters is
        // whether the wall is actually between them for most of the run, which
        // is the thing the level design rests on and the thing a broken wall
        // would silently not deliver.
        var post = new Vector3(CrawlerX, ground, CrawlerZ);
        var lanes = new[] { LaneSouthZ, WallZ, LaneFightZ };

        var blockedFrac = new float[lanes.Length];
        var firstExposed = new float[lanes.Length];
        var steps = new int[lanes.Length];

        for (int li = 0; li < lanes.Length; li++)
        {
            float z = lanes[li];
            int samples = 0, blocked = 0;
            firstExposed[li] = -1f;

            for (float x = EnterX; x <= ExitX; x += 0.5f)
            {
                samples++;
                if (Blocked(new Vector3(x, ground, z), post, crawler)) blocked++;
                else if (firstExposed[li] < 0f) firstExposed[li] = x - EnterX;
            }

            steps[li] = samples;
            blockedFrac[li] = samples > 0 ? (float)blocked / samples : 0f;
        }

        for (int li = 0; li < lanes.Length; li++)
            sb.AppendLine("  z " + lanes[li].ToString("0.0") + " " +
                          LaneNames[li].PadRight(30) + " hidden " +
                          (blockedFrac[li] * 100f).ToString("0") + "% of " +
                          (ExitX - EnterX).ToString("0") + " m, in " +
                          steps[li] + " samples" +
                          (firstExposed[li] < 0f
                              ? ", never exposed"
                              : ", first exposed at " + firstExposed[li].ToString("0.0") + " m"));

        Check(sb, ref pass, ref fail, "the wall hides the southern lane from its post",
              blockedFrac[0] >= 0.6f,
              (blockedFrac[0] * 100f).ToString("0") + "% of the southern lane is out " +
              "of its sight" +
              (blockedFrac[0] < 0.6f
                  ? " — below 60%. The wall is not between them for most of the " +
                    "run, so there is no cover in this yard and 'optional " +
                    "stealth' is only optional in the sense that it does not exist"
                  : ""));

        // The two lanes must disagree, or there is no decision to make.
        Check(sb, ref pass, ref fail, "the two lanes are not equally good",
              blockedFrac[0] - blockedFrac[2] >= 0.25f,
              "south " + (blockedFrac[0] * 100f).ToString("0") + "% hidden against " +
              "north " + (blockedFrac[2] * 100f).ToString("0") + "%" +
              (blockedFrac[0] - blockedFrac[2] < 0.25f
                  ? " — if both routes were equally hidden there would be no " +
                    "reason to walk one rather than the other, and the wall " +
                    "would be decoration"
                  : ", so going south is worth something"));

        Check(sb, ref pass, ref fail, "the fight lane is honestly exposed",
              blockedFrac[2] <= 0.4f,
              (blockedFrac[2] * 100f).ToString("0") + "% hidden on the northern lane" +
              (blockedFrac[2] > 0.4f
                  ? " — if the north lane is also safe then the player who went " +
                    "round to the north took the same route and the choice was " +
                    "an illusion"
                  : ", so north is the open ground where it can reach her"));

        // The gap has to be a real gap, not a gap in the drawing.
        // Sample the gap from a spot chosen so the answer means what the check
        // says it means, and — more to the point — do not *assert a cause* the
        // measurement does not establish.
        //
        // The message this replaces had two possible causes baked into it. It
        // reported "BLOCKED — the two runs are touching and there is no way
        // through" whenever the ray came back blocked, and on the build that
        // produced it the ray was blocked by a layer mask of zero, which sees
        // nothing at all. So the tool diagnosed a geometry fault that did not
        // exist, named two walls that were correctly placed, and pointed at the
        // drawing. It was wrong with a very specific story attached, which is
        // the most expensive kind of wrong to debug.
        //
        // A check may state what it measured. It may not name a cause it did
        // not measure, because the reader cannot tell the difference and will
        // go and fix the thing that was named.
        float gapMid = (WallGapX + WallGapX2) * 0.5f;
        var gapEye = new Vector3(gapMid, ground, LaneSouthZ);
        bool gapBlocked = Blocked(gapEye, post, crawler);

        sb.AppendLine();
        sb.AppendLine("  sampled through the middle of the gap, at x " +
                      gapMid.ToString("0.0") + ", z " + LaneSouthZ.ToString("0.0") +
                      ", looking at its post: " +
                      (gapBlocked ? "something is in the way" : "nothing in the way"));

        Check(sb, ref pass, ref fail, "the gap in the wall is an opening, not a joint",
              WallGapX2 > WallGapX && !gapBlocked,
              "the gap runs " + (WallGapX2 - WallGapX).ToString("0.0") + " m at x " +
              WallGapX.ToString("0.0") + " to " + WallGapX2.ToString("0.0") + ", and a " +
              "sight line through its centre from the southern lane is " +
              (gapBlocked
                  ? "blocked. That is measured, not diagnosed: run the two walls " +
                    "apart and re-run this before assuming they are touching"
                  : "clear, so a walker at that spot is seen and the opening is " +
                    "worth the two metres of exposure it was drawn for"));

        // And the geometry alone cannot finish the route, so say so out loud
        // rather than letting the reader assume the cover did all of it.
        float exitToPost = Vector3.Distance(new Vector3(ExitX, ground, LaneSouthZ), post);
        sb.AppendLine();
        sb.AppendLine("  the exit is " + exitToPost.ToString("0.0") +
                      " m from its post, inside a " + NoticeRadius.ToString("0") +
                      " m notice radius");

        Check(sb, ref pass, ref fail, "no crawler could let her out on geometry alone",
              exitToPost <= NoticeRadius,
              exitToPost.ToString("0.0") + " m against " + NoticeRadius.ToString("0") +
              " m of notice — she has to walk past where it lives, so the wall " +
              "cannot finish the route on its own and something has to end the " +
              "pursuit. InkCrawler.Retired is that thing; see its comment");

        Check(sb, ref pass, ref fail, "the crawler does have a way to give up for good",
              HasRetireLatch(crawler),
              "SeesHer() returns false once Retired is set, and Retired is set " +
              "when it gives up" +
              (HasRetireLatch(crawler)
                  ? ""
                  : " — NOT SET, so it will spot her on the way out and the " +
                    "stealth route cannot complete"));

        // --- where everything stands ---------------------------------------

        sb.AppendLine();
        sb.AppendLine("--- what is standing in the yard ---");

        // Scoped to the yard, with the tolerance stated, and printing the rest
        // of the level as a count rather than as a list of forty names.
        //
        // This walked every collider in the scene and called anything not built
        // by this beat and not under Village_Grey "unexplained in the yard". It
        // found 47 — every object in Beat 3, Beat 4, the cast lineup and the
        // fountain, all of them tens of metres away — and reported them as a
        // fault in the yard, listing them one per line under a heading that said
        // they were in the yard. They were not in the yard.
        //
        // A census is not a finding. The useful question is whether anything is
        // standing where the fight happens that this beat did not put there, so
        // that is the question, and the yard's own bounds are the filter. Half
        // a metre of slack lets a collider that legitimately overhangs the edge
        // of the yard be reported rather than hidden.
        const float yardTolerance = 0.5f;
        var yardMin = new Vector3(YardXMin - yardTolerance, ground - yardTolerance,
                                  YardZMin - yardTolerance);
        var yardMax = new Vector3(YardXMax + yardTolerance, ground + 6f,
                                  YardZMax + yardTolerance);

        int mine = 0, village = 0, foreign = 0, elsewhere = 0;
        var mineSet = new HashSet<Collider>();
        var inTheYard = new List<string>();
        var all = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include);

        foreach (var col in all)
        {
            if (col == null) continue;
            string path = PathOf(col.transform);

            // `continue`. Without it this beat's own four colliders were counted
            // as `mine` and then fell straight through to be counted a second
            // time as UNEXPLAINED — which is how a yard containing nothing but
            // this beat's wall and this beat's creature reported "4 colliders
            // in the yard from neither this beat nor the village", with the
            // four names listed directly above it, each one ending
            // `path L1_Beat5`.
            //
            // The names were the entire proof and the verdict read them anyway.
            // A check that prints its evidence and then contradicts it is worse
            // than no check, and this contradiction was the *count* against the
            // *list*: anyone reading it went looking for an intruder and found
            // the yard's own furniture.
            if (path.Contains(Container)) { mine++; mineSet.Add(col); continue; }

            bool inYard = col.bounds.max.x >= yardMin.x && col.bounds.min.x <= yardMax.x &&
                          col.bounds.max.z >= yardMin.z && col.bounds.min.z <= yardMax.z;

            if (!inYard) { elsewhere++; continue; }

            bool isVillage = path.Contains("Village_Grey");
            if (isVillage) village++; else foreign++;

            inTheYard.Add("    " + (isVillage ? "village " : "UNEXPLAINED ") +
                          col.name + "  " + col.GetType().Name +
                          "  y " + col.bounds.min.y.ToString("F2") + " to " +
                          col.bounds.max.y.ToString("F2") +
                          "  x " + col.bounds.min.x.ToString("F2") + "-" +
                          col.bounds.max.x.ToString("F2") +
                          "  z " + col.bounds.min.z.ToString("F2") + "-" +
                          col.bounds.max.z.ToString("F2") +
                          "  path " + path);
        }

        sb.AppendLine("  " + mine + " collider(s) this beat built, " +
                      village + " village collider(s) and " + foreign +
                      " unexplained inside the yard's " +
                      YardXMin.ToString("0") + "-" + YardXMax.ToString("0") + " x " +
                      YardZMin.ToString("0") + "-" + YardZMax.ToString("0") +
                      " bounds, " + elsewhere + " elsewhere in the level");
        sb.AppendLine("  (the village's Ground is one collider spanning the whole " +
                      "level, so it is always 'inside the yard'; the houses are " +
                      "House_35_0's east corner, whose roof reaches x 39.05)");
        sb.AppendLine();
        foreach (var line in inTheYard) sb.AppendLine(line);

        // A collider standing in the yard that this beat did not build is a
        // thing to stand in the yard. If the ground under the crawler is not
        // the ground this beat measured, every number above it is measured on
        // the wrong floor — which is exactly what a 1.10 m drop under a post
        // that was measured flat at 0.00 on the previous build means.
        // The floor itself is checked once, in the section that measures it —
        // "the yard sits on the floor it was designed over" — and that check
        // carries the consequence: every lane percentage, every sight line and
        // every place this beat put a creature was cast at the measured
        // height, so a wrong floor makes all of them wrong together and the
        // failure does not show up in any one of them.

        Check(sb, ref pass, ref fail, "nothing unexplained is standing in the yard",
              foreign == 0,
              foreign == 0
                  ? "the yard holds only what this beat built and the village's own " +
                    "ground; the other " + elsewhere + " colliders in the level are " +
                    "other beats' and cast's, and none of them is in here"
                  : foreign + " collider(s) in the yard from neither this beat nor " +
                    "the village, listed above");

        // Village clearance — the thing the list above cannot answer.
        float worst = float.MaxValue;
        string worstWho = "";
        foreach (var col in all)
        {
            if (col == null || !mineSet.Contains(col)) continue;
            string path = PathOf(col.transform);
            if (!path.Contains("Village_Grey")) continue;
            foreach (var other in all)
            {
                if (other == null || other.name == "Ground") continue;
                if (!PathOf(other.transform).Contains("Village_Grey")) continue;
                if (mineSet.Contains(other)) continue;
                float g = Gap(col.bounds, other.bounds);
                if (g < worst) { worst = g; worstWho = col.name + " vs " + other.name; }
            }
        }

        sb.AppendLine("  closest approach to the village: " +
                      (worst == float.MaxValue ? "nothing in range"
                                               : worst.ToString("0.00") + " m  (" + worstWho + ")"));
        Check(sb, ref pass, ref fail, "the yard does not grow through a house",
              worst > 0f,
              worst == float.MaxValue ? "no village geometry in range"
                                      : worst.ToString("0.00") + " m between " + worstWho +
                                        (worst > 0f ? " — they do not touch" : " — THEY TOUCH"));

        // --- the wiring -----------------------------------------------------

        sb.AppendLine();
        sb.AppendLine("--- wiring ---");

        Check(sb, ref pass, ref fail, "the crawler has a collider, or clicks pass through it",
              crawlerGo.GetComponentInChildren<Collider>(true) != null,
              "BrushPainter casts its click with no layer mask, so a crawler with " +
              "no collider simply cannot be aimed at");

        // Read as the type the value *has*, not the type it is wished it had.
        //
        // The crawler was read as `Src<Transform>` — the field holds an
        // AriMover, so `as Transform` yields null and the beat reported "the
        // crawler knows Ari — NOT WIRED" with Ari wired into it correctly. And
        // the director was read as `Src<Beat5Director>` for a field holding an
        // InkCrawler, same failure, same confident lie.
        //
        // `as` is not a cast that complains. Given a reference of the wrong
        // type it yields null, and null compared against the thing that was
        // set looks precisely like a field nobody set. The wire was there and
        // the check could not read it.
        //
        // The rule, which this project has now paid for twice: when a check
        // says a reference is null, suspect the field name and the type
        // argument before you suspect the setter.
        Check(sb, ref pass, ref fail, "the crawler knows Ari",
              Src<AriMover>(crawler, "ari") != null,
              Src<AriMover>(crawler, "ari") != null
                  ? "wired"
                  : "NOT WIRED — it can never notice her");

        Check(sb, ref pass, ref fail, "the director knows the crawler",
              Src<InkCrawler>(dir, "crawler") == crawler,
              Src<InkCrawler>(dir, "crawler") == crawler
                  ? "wired"
                  : "NOT WIRED — no stroke reaches it");

        var brush = ari != null ? ari.GetComponent<BrushPainter>() : null;
        Check(sb, ref pass, ref fail, "the director knows Ari's brush",
              Src<BrushPainter>(dir, "brush") == brush,
              brush != null ? (Src<BrushPainter>(dir, "brush") == brush
                                  ? "wired"
                                  : "NOT WIRED — the brush would fall back to a search and " +
                                    "might find a different one")
                            : "Ari has no BrushPainter at all");

        Check(sb, ref pass, ref fail, "the brush will actually tell someone",
              brush != null, brush != null ? "present" : "MISSING");

        float dirEnter = Src(dir, "enterX", EnterX);
        float dirExit = Src(dir, "exitX", ExitX);

        Check(sb, ref pass, ref fail, "the yard is entered before it is left",
              dirExit > dirEnter + 5f,
              dirEnter.ToString("0") + " m in, " + dirExit.ToString("0") +
              " m out — " + (dirExit - dirEnter).ToString("0") + " m of beat");

        Check(sb, ref pass, ref fail, "the crawler starts inside the yard, past the entrance",
              CrawlerX > dirEnter + 2f && CrawlerX < dirExit,
              "crawler at x " + CrawlerX.ToString("0") + ", yard " +
              dirEnter.ToString("0") + " to " + dirExit.ToString("0"));

        Check(sb, ref pass, ref fail, "she sees it before it sees her",
              notice < CrawlerX - dirEnter,
              "notice range " + notice.ToString("0.0") + " m against " +
              (CrawlerX - dirEnter).ToString("0") +
              " m of approach — she gets " +
              (CrawlerX - dirEnter - notice).ToString("0.0") + " m of looking at it first" +
              (notice >= CrawlerX - dirEnter
                  ? " — it is already hunting before she is through the gate"
                  : ""));

        Check(sb, ref pass, ref fail, "the yard is not wider than the walk is long",
              YardXMax - YardXMin >= dirExit - dirEnter,
              "yard " + (YardXMax - YardXMin).ToString("0") + " m for " +
              (dirExit - dirEnter).ToString("0") + " m of beat");
    }

    /// <summary>
    /// Would a crawler at its home post have seen her?
    ///
    /// It asks the component, rather than casting a ray of its own.
    ///
    /// <para>That is not tidiness, it is the whole point of this function.</para>
    ///
    /// The first version read <c>bodyHeight</c> by reflection, built an eye
    /// and a chest, and cast a ray with mask 0 — all of it correct, and all of
    /// it a *reimplementation*. The component meanwhile excluded Ari's layer
    /// from its own ray, which on Ari's default layer also excluded the
    /// ground and the ruin walls, so the game's crawler could see her through
    /// the cover. The two disagreed, and this tool's number was the one the
    /// player would never see: it would have reported a working stealth lane
    /// while every sight line in the real game came back clear.
    ///
    /// A verification tool that keeps its own copy of the rule under test is
    /// not verifying the rule. It is verifying that the geometry is nice, and
    /// reporting the result as though it were about the game.
    /// </summary>
    static bool Blocked(Vector3 ari, Vector3 crawler, InkCrawler c)
    {
        if (c == null) return false;

        float height = Src(c, "bodyHeight", CrawlerHeight);
        float eyeY = height * 0.85f;

        Vector3 eye = crawler + Vector3.up * eyeY;
        Vector3 chest = ari + Vector3.up * (AriBodyHeight * 0.6f);

        return c.BlockedFromPost(eye, chest);
    }

    /// <summary>
    /// Does the crawler really have the latch that lets her out of the yard?
    ///
    /// Three ordered facts, all read from its own source, because a member
    /// being *present* proves none of them:
    ///
    ///   1. <c>Retired</c> exists and can be read.
    ///   2. <c>SeesHer</c> consults it <i>before</i> it measures the distance.
    ///      A crawler that checks line of sight first and only ignores the
    ///      answer afterwards still spends the raycast, and still works — but
    ///      the point of the latch is that it stops asking.
    ///   3. <c>SeesHer</c> ends in the shared <c>BlockedFromPost</c>, so the
    ///      sight test this build tool called is the same one the game calls.
    ///
    /// <para>A source read rather than reflection, and the reason is the
    /// arrows in 2 and 3.</para>
    ///
    /// Reflection can tell you a method exists. It cannot tell you which of
    /// two statements comes first, and *the order is the entire claim* — the
    /// difference between a latch that stops the behaviour and one that
    /// overrides its result. Reflection also cannot tell whether
    /// <c>BlockedFromPost</c> is the thing this tool is calling or a namesake
    /// it invented, which is exactly the bug a hand-rolled duplicate raycast
    /// produced once already.
    ///
    /// Facts 1 and 3 are also checked by reflection and by the tool's own use
    /// of the method respectively, so this is corroboration of two
    /// independent kinds, not a single source reading trusted alone.
    /// </summary>
    static bool HasRetireLatch(InkCrawler c)
    {
        if (c == null) return false;

        // 1. reflection: it exists and is readable.
        var prop = c.GetType().GetProperty("Retired");
        if (prop == null || !prop.CanRead) return false;

        // 3. reflection: the sight test is a member, so calling it from here
        //    really did enter the component rather than a local copy.
        if (c.GetType().GetMethod("BlockedFromPost") == null) return false;

        string[] files = Directory.GetFiles(
            Path.Combine(Directory.GetCurrentDirectory(), "Assets/Painterly/Scripts"),
            "InkCrawler.cs");
        if (files.Length == 0) return false;

        string src = File.ReadAllText(files[0]);

        int sees = src.IndexOf("bool SeesHer(", StringComparison.Ordinal);
        if (sees < 0) return false;

        int end = src.IndexOf("bool ChaseOrGiveUp", sees, StringComparison.Ordinal);
        if (end < 0) end = src.Length;

        string body = src.Substring(sees, end - sees);

        // 2. ordering: the latch, then the gate.
        int latch = body.IndexOf("Retired", StringComparison.Ordinal);
        int gate = body.IndexOf("DistanceToAri", StringComparison.Ordinal);

        // 3. ordering: the shared sight test is the last thing SeesHer does.
        int shared = body.IndexOf("BlockedFromPost", StringComparison.Ordinal);

        return latch >= 0
            && (gate < 0 || latch < gate)
            && shared > 0;
    }

    // --- helpers ---------------------------------------------------------------

    static float AriBody(StringBuilder sb, string property, float fallback)
    {
        var go = Named("Ari");
        if (go == null) { sb.AppendLine("  no Ari in the scene"); return fallback; }

        var mover = go.GetComponent<AriMover>();
        if (mover == null) return fallback;

        var p = mover.GetType().GetProperty(property);
        if (p != null) return Convert.ToSingle(p.GetValue(mover, null));

        // The field fallback is silent on purpose. The first version of this
        // warned, and the warning blamed a stale assembly — which was wrong,
        // and survived two recompiles, and cost two compile cycles to disprove.
        var f = mover.GetType().GetField(CharLower(property),
                       System.Reflection.BindingFlags.NonPublic |
                       System.Reflection.BindingFlags.Instance);
        return f != null ? Convert.ToSingle(f.GetValue(mover)) : fallback;
    }

    static string CharLower(string s) =>
        char.ToLowerInvariant(s[0]) + s.Substring(1);

    /// <summary>
    /// How tall she looks, in metres.
    ///
    /// Seeded the same way the crawler is: from the first renderer's own
    /// bounds, never from zero. See the long note in the crawler block — the
    /// zero seed reports a model that sits above the ground as though its
    /// feet were on the floor plane, which for Ari, whose root is at y 0.25,
    /// meant every number about her height in every beat's report was inflated
    /// by her root's elevation.
    ///
    /// It read as "2.26 m visible against 1.80 m of body", a 46 cm overshoot
    /// bad enough to be worth investigating, to change her collider, or to
    /// reimport her FBX. The actual excess is her root height, and possibly
    /// nothing at all. An inflation added to a real measurement is worse than
    /// either alone: it makes a correct rig look broken and sends you to fix
    /// the thing that was already right.
    /// </summary>
    static float AriVisible(GameObject go)
    {
        if (go == null) return 0f;
        float top = float.NegativeInfinity;
        float bottom = float.PositiveInfinity;
        int n = 0;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            top = Mathf.Max(top, r.bounds.max.y);
            bottom = Mathf.Min(bottom, r.bounds.min.y);
            n++;
        }
        return n == 0 ? 0f : top - bottom;
    }

    /// <summary>
    /// The top of whatever is under a point, as a floor height.
    ///
    /// Casts from far above and takes the highest hit, which is the correct
    /// answer for "what would you stand on here" — except for the one thing
    /// standing here, which is the beat's own geometry.
    ///
    /// <para>So it refuses to answer with anything this beat owns.</para>
    ///
    /// Not as convenience but because the ray cannot tell the difference and
    /// the difference is the whole bug. A downward probe at the crawler's own
    /// post finds the crawler's own capsule and calls it the floor; the crawler
    /// is then placed on the answer, and the next run finds a taller answer
    /// again. The report's ground rose 0.00 → 1.10 → and would have gone on
    /// rising, each step caused by the step before it, every one of them a
    /// plausible number with a confident explanation attached — "the yard is
    /// not flat enough to fight on", pointing at a fall that was the creature
    /// the same report had just measured three lines above.
    ///
    /// Taking the highest hit rather than the last is not a fix either: the
    /// crawler was on top. And refusing our own colliders is not a fix on its
    /// own either, because it leans on the container name staying right and on
    /// `Clear` having removed everything — and this failure was two of those
    /// assumptions being quietly wrong at the same time, which is why both are
    /// now guarded rather than one.
    ///
    /// Returns NaN when there is nothing at all under the point. That is a
    /// finding, not a zero: returning 0.00 for it would report the yard as
    /// flat ground over a hole.
    /// </summary>
    /// <summary>
    /// A measured floor height, or the designed one when nothing was there.
    ///
    /// Only ever called on a probe result already reported, so the fallback is
    /// visible in the log rather than silent — but the caller still gets a
    /// usable number, because a NaN handed to a Vector3 constructor puts NaN
    /// in a position, then in a parent, then in a whole hierarchy, and the
    /// result looks like a broken scene rather than like a missing probe.
    /// </summary>
    /// <summary>
    /// How tall she stands, in metres, measured the way <c>AriMover</c>
    /// measures it: bake the skin in the pose she actually stands in and take
    /// the lowest and highest vertex.
    ///
    /// <para>And the reason this exists at all.</para>
    ///
    /// The check above it compared <c>Renderer.bounds</c> against
    /// <c>bodyHeight</c> and reported her 46 cm too tall — very nearly the size
    /// of the whole error — and pointed the fix at rescaling the player
    /// character, reimporting her FBX, or raising her collider and so
    /// destroying every clearance number in Beats 3 and 4.
    ///
    /// All three would have been wrong, because the two numbers are not
    /// measurements of the same thing.
    ///
    /// A <c>SkinnedMeshRenderer</c>'s bounds are not its current shape. They
    /// are <c>localBounds</c>, baked once at import to cover **every pose the
    /// animation can reach** — her deepest crouch, the top of a jump, the
    /// reach of an idle — transformed by the node's world matrix. Ari's read
    /// −1.175 to 0.953 in the mesh's own file: 2.128 m, and the distance
    /// between those two numbers is not her height, it is how far her body
    /// travels across the animation set.
    ///
    /// Set that beside a standing height and it reports an extra half metre on
    /// a character who is standing perfectly still — every time, with nothing
    /// in the number to say it was the wrong number.
    ///
    /// So this bakes instead: same method, same pose and same authority the
    /// component uses for <c>soleOffset</c>. It agrees with the component by
    /// construction rather than by luck.
    /// </summary>
    static bool AriPosedHeight(GameObject go, out float feetY, out float crownY)
    {
        feetY = 0f;
        crownY = 0f;
        if (go == null) return false;

        var skin = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (skin == null) return false;

        var anim = skin.GetComponentInParent<Animator>();
        if (anim != null) anim.Update(0f);

        var baked = new Mesh();
        skin.BakeMesh(baked);

        var verts = baked.vertices;
        var m = skin.transform.localToWorldMatrix;

        float lo = float.MaxValue;
        float hi = float.MinValue;
        for (int i = 0; i < verts.Length; i++)
        {
            float y = m.MultiplyPoint3x4(verts[i]).y;
            if (y < lo) lo = y;
            if (y > hi) hi = y;
        }

        UnityEngine.Object.DestroyImmediate(baked);

        if (lo > hi) return false;

        feetY = lo;
        crownY = hi;
        return true;
    }

    static float FloorOf(float measured) =>
        float.IsNaN(measured) ? YardGround : measured;

    static float GroundUnder(float x, float z)
    {
        var ray = new Ray(new Vector3(x, 40f, z), Vector3.down);
        var hits = Physics.RaycastAll(ray, 400f, ~0, QueryTriggerInteraction.Ignore);

        float best = float.NaN;
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i].collider;
            if (col == null) continue;

            // Our own geometry is not the ground, at whatever height it
            // happens to be sitting.
            if (PathOf(col.transform).Contains(Container)) continue;

            if (float.IsNaN(best) || hits[i].point.y > best) best = hits[i].point.y;
        }

        return best;
    }

    /// <summary>
    /// A slab of ruin. Its own type, because the first version smuggled six
    /// numbers through a Vector3 — `new Vector3(x, y, z, sx, h, sz)` — which
    /// does not compile, and the two errors it produced pointed at the
    /// constructor and at a `.w` rather than at the idea that should never have
    /// been tried. Five numbers, five fields.
    /// </summary>
    struct Wall
    {
        public readonly string name;
        public readonly float x, z, sizeX, sizeZ, height;

        public Wall(string name, float x, float z, float sizeX, float sizeZ, float height)
        {
            this.name = name;
            this.x = x; this.z = z;
            this.sizeX = sizeX; this.sizeZ = sizeZ;
            this.height = height;
        }

        /// <summary>Half-extents, for the clearance report.</summary>
        public Vector3 Extent =>
            new Vector3(sizeX * 0.5f, height * 0.5f, sizeZ * 0.5f);

        public Vector3 Centre => new Vector3(x, height * 0.5f, z);
    }

    static void MakeWall(Transform parent, Wall w, Material mat)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Beat5_Cover_" + w.name.Replace(' ', '_');
        cube.transform.SetParent(parent, false);
        cube.transform.position = w.Centre;
        cube.transform.localScale = new Vector3(w.sizeX, w.height, w.sizeZ);

        // A cube primitive's collider is a BoxCollider with the mesh shape left
        // on, which reports bounds off the *scaled mesh* — a 1 m cube scaled to
        // 12 x 2.2 x 0.45 is fine, but the shell is a collider that does not
        // need to exist and shows up in every collider census as an
        // unexplained object. Setting the shape to the box makes the bounds
        // agree with the transform, which is what the clearance number in the
        // report is measured from.
        var box = cube.GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = false;

        var r = cube.GetComponent<Renderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;
    }

    static GameObject Prefab(string assetPath, string name, Transform parent, Vector3 at)
    {
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null) return null;

        var go = UnityEngine.Object.Instantiate(asset);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = at;
        return go;
    }

    static void SetController(GameObject go, string path)
    {
        var ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        if (ctrl == null) return;
        var anim = go.GetComponentInChildren<Animator>(true);
        if (anim == null) anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
    }

    static Material LoadMat(string name)
    {
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets(name + " t:Material"))
        {
            var m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                        UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (m != null) return m;
        }
        return null;
    }

    static GameObject Named(string name)
    {
        var all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name == name) return all[i].gameObject;
        return null;
    }

    static GameObject GetOrAdd(string name)
    {
        var go = Named(name);
        if (go != null) return go;
        go = new GameObject(name);
        return go;
    }

    static void Clear(GameObject container)
    {
        var kids = new List<GameObject>();
        foreach (Transform t in container.transform) kids.Add(t.gameObject);
        foreach (var k in kids) UnityEngine.Object.DestroyImmediate(k);
    }

    static string PathOf(Transform t)
    {
        var parts = new List<string>();
        var cur = t;
        while (cur != null)
        {
            parts.Insert(0, cur.name);
            cur = cur.parent;
        }
        return string.Join(" / ", parts.ToArray());
    }

    /// <summary>Shortest distance between two boxes; zero if they touch.</summary>
    static float Gap(Bounds a, Bounds b)
    {
        float dx = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
        float dy = Mathf.Max(0f, Mathf.Max(a.min.y - b.max.y, b.min.y - a.max.y));
        float dz = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    static void Set(object target, string field, object value)
    {
        if (target == null) return;
        var f = target.GetType().GetField(field,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (f == null)
        {
            Debug.LogError("[Echoes] beat5: " + target.GetType().Name + " has no field '" +
                           field + "' — the beat is not fully wired");
            return;
        }
        f.SetValue(target, value);
    }

    static T Src<T>(object target, string field) where T : class
    {
        if (target == null) return null;
        var f = target.GetType().GetField(field,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        return f == null ? null : f.GetValue(target) as T;
    }

    static float Src(object target, string field, float fallback)
    {
        if (target == null) return fallback;
        var f = target.GetType().GetField(field,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (f == null)
        {
            Debug.LogError("[Echoes] beat5: no field '" + field + "' on " +
                           target.GetType().Name + " — using " + fallback);
            return fallback;
        }
        return Convert.ToSingle(f.GetValue(target));
    }

    static void Check(StringBuilder sb, ref int pass, ref int fail,
                      string what, bool ok, string why)
    {
        if (ok) pass++;
        else fail++;
        sb.AppendLine("  " + (ok ? "ok   " : "FAIL ") + what + " — " + why);
    }

    static void Fail(StringBuilder sb, ref int pass, ref int fail, string why)
    {
        fail++;
        sb.AppendLine("  FAIL " + why);
    }

    static void Warn(StringBuilder sb, ref int warn, string why)
    {
        warn++;
        sb.AppendLine("  warn " + why);
    }

    // --- the lines -------------------------------------------------------------

    static void BuildLines(StringBuilder sb, ref int pass, ref int fail)
    {
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<MonoHintLines>(LinesPath);
        bool isNew = false;
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<MonoHintLines>();
            isNew = true;
        }

        // Only this beat's own ids. Beat 3 and Beat 4 both rewrite the asset, and
        // a rewriter that cleared the whole list would delete every other beat's
        // lines — which is exactly what two rewriters racing would do to each
        // other.
        asset.lines.RemoveAll(l => l != null && l.id.StartsWith("beat5."));

        void Beat(string id, string text) =>
            asset.lines.Add(new MonoHintLines.Line
            { id = id, text = text, kind = MonoHintLines.LineKind.Beat });

        Beat("beat5.push",
             "You moved it. Good. But look — it is getting up. Whatever that " +
             "was, it was not enough. It will not be enough.");

        Beat("beat5.nokill",
             "You cannot kill it. Not with that. There is nothing here yet that " +
             "would answer to that, and I have looked.");

        Beat("beat5.hits_back",
             "It reached you. Are you all right? — Good. Then neither of us " +
             "can end this, and that changes what we do next.");

        Beat("beat5.unseen",
             "It has stopped. It is walking back to where it was sitting, and " +
             "it has not even turned its head to watch you go. — It lost you. " +
             "That is a thing you did, not a thing you were spared.");

        Beat("beat5.done",
             "There are more of them. Further in, and closer together. We go " +
             "slowly, and we go together, or not at all.");

        if (isNew)
        {
            UnityEditor.AssetDatabase.CreateAsset(asset, LinesPath);
            sb.AppendLine("  created " + LinesPath);
        }
        else
        {
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
        }

        int mine = asset.lines.FindAll(l => l != null && l.id.StartsWith("beat5.")).Count;
        sb.AppendLine("  " + mine + " Beat 5 line(s) authored, " +
                      asset.lines.Count + " in the asset now");

        Check(sb, ref pass, ref fail, "every line the beat can fire exists",
              mine >= 5, mine + " beat5.* lines for 5 call sites in Beat5Director");
    }
}
}