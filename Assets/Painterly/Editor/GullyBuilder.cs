using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Echoes.Painterly;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

using Object = UnityEngine.Object;
public static class GullyBuilder
{
    const string ReportPath = "Temp/gully.txt";
    const string ContainerName = "L1_Gully";
    const string WallMaterialPath = "Assets/Painterly/Materials/Beat3_Gully.mat";
    const string SourceMaterialPath = "Assets/Painterly/Materials/Beat3_Stump.mat";

    // --- the shape a gully has to be -------------------------------------------

    /// <summary>
    /// Clear width between the two wall faces. 1.60 m, not narrower and not
    /// wider.
    ///
    /// Measured against two numbers. Ari's capsule is 0.60 m across, so
    /// anything under 0.90 m is somewhere he wedges rather than walks, and the
    /// wall sweep stops him on contact with both walls at once, which is what
    /// makes a gully read as a gully. Anything over 2.00 m and two characters
    /// can pass each other without turning, and the camera stops having to
    /// follow and can go back to leading. 1.60 m leaves a hand's width either
    /// side of his, which is enough to walk and not enough to overtake.
    /// </summary>
    const float Width = 1.60f;

    /// <summary>How far the gully runs. Long enough for a follow with legs in
    /// it, short enough that the beat is not a commute.</summary>
    const float Length = 14f;

    /// <summary>Wall height. Ari is 1.80 m, so anything shorter is a fence he
    /// can see over and a chase he can watch from outside.</summary>
    const float WallHeight = 2.00f;

    const float WallThickness = 0.40f;

    /// <summary>How far past Mono the gully starts, so the tree and the burst
    /// stay in the open and the walls do not crowd the waking.</summary>
    const float SetBack = 5f;

    const float Chest = 1.10f;    // where width is measured
    const float AriRadius = 0.30f;

    static readonly Collider[] Buffer = new Collider[24];

    // --- entry ------------------------------------------------------------------

    public static void Run()
    {
        var sb = new StringBuilder();

        try { Body(sb); }
        catch (Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] gully\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportPath),
                           sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded) { sb.AppendLine("No scene is open."); return; }

        Physics.SyncTransforms();

        var mono = MonoCompanion.FindInLevel();
        if (mono == null)
        {
            sb.AppendLine("No Mono in the level. Run the cast tool first.");
            return;
        }

        var monoGo = mono.gameObject;
        var from = monoGo.transform.position;
        var origin = Vector3.zero;                 // the market square

        sb.AppendLine("BUILDING A GULLY FOR BEAT 3");
        sb.AppendLine("Mono at " + from.ToString("F2") +
                      ", " + Vector3.Distance(new Vector3(from.x, 0f, from.z),
                                             new Vector3(origin.x, 0f, origin.z))
                            .ToString("0.0") + " m from the square");
        sb.AppendLine("target: " + Width.ToString("0.00") + " m clear, " +
                      Length.ToString("0.0") + " m long, walls " +
                      WallHeight.ToString("0.00") + " m and " +
                      WallThickness.ToString("0.00") + " m thick");
        sb.AppendLine();
        sb.AppendLine("There is no gully in the village to find. WidthMap measured " +
                      "21 025 cells at 0.5 m and every one of the 27 narrow " +
                      "regions came back 3.83 m or wider, which is a gap between " +
                      "two houses and not a passage. So it is built, and the " +
                      "build measures itself afterwards rather than trusting the " +
                      "numbers it was given.");
        sb.AppendLine();

        // --- 1. where ------------------------------------------------------------

        var dir = ChooseDirection(sb, from, origin);

        if (dir == Vector3.zero)
        {
            sb.AppendLine("NO DIRECTION IS CLEAR. Every one of 16 headings out of " +
                          "Mono's position has something standing in it within " +
                          (SetBack + Length).ToString("0") +
                          " m. The village is denser than the gully wants. " +
                          "The gully was NOT built, and the chase has no route.");
            return;
        }

        var start = Flat(from) + dir * SetBack;
        start = new Vector3(start.x, GroundAt(start), start.z);
        var end = start + dir * Length;

        sb.AppendLine("chosen heading: " + dir.x.ToString("0.00") + ", " +
                      dir.z.ToString("0.00") + " (" + Compass(dir) + ")");
        sb.AppendLine("gully from " + start.ToString("F2") + " to " + end.ToString("F2") +
                      ", " + Length.ToString("0.0") + " m");
        sb.AppendLine();

        // --- 2. build ------------------------------------------------------------

        var mat = Material(sb);
        var container = Rebuild(sb);

        var across = new Vector3(-dir.z, 0f, dir.x);
        float offset = (Width + WallThickness) * 0.5f;

        // --- 3. is the ground the walls will stand on actually free? ------------
        //
        // Checked here, before either wall exists, because a check that runs
        // after they are built finds the walls themselves and reports the gully
        // as having landed inside a building when the only thing in the volume
        // is the thing that was just placed in it.

        foreach (var side in new[] { 1f, -1f })
        {
            int occupied = 0;
            string what = "";

            for (int i = 0; i <= 8; i++)
            {
                var p = start + dir * (Length * i / 8f) + across * (offset * side);
                float gy = GroundAt(p);
                if (gy < -900f) continue;

                if (WallSiteBlocked(p, dir, gy))
                {
                    occupied++;
                    if (what == "") what = Nearest(p, gy);
                }
            }

            sb.AppendLine("wall site " + (side > 0f ? "A (+" : "B (-") +
                          offset.ToString("0.00") + " m): " +
                          (occupied == 0
                              ? "clear for the whole 14 m"
                              : occupied + " of 9 samples blocked, nearest is " +
                                what));
        }

        sb.AppendLine();

        // Placed at the MIDPOINT of each side, which is where a Length-long box
        // belongs. Passing the start point instead puts the wall's first metre
        // on the corridor and its remaining thirteen behind the start, which
        // measures correctly for the first ten samples and then reports twelve
        // metres of open ground where the wall should be. The per-sample dump
        // is the only reason that was caught: a median of 1.60 m and a pass on
        // every summary line, with half the gully not built.
        var mid = start + dir * (Length * 0.5f);

        var wallA = Wall(container.transform, "GullyWall_A",
                         mid + across * offset, dir, mat);
        var wallB = Wall(container.transform, "GullyWall_B",
                         mid - across * offset, dir, mat);

        Physics.SyncTransforms();

        // The walls exist now, so their actual extent is worth stating rather
        // than assuming. A 14 m box centred on the midpoint reaches the first
        // and last sample exactly, and anything else is a placement error.
        foreach (var w in new[] { wallA, wallB })
        {
            var b = new Bounds(w.transform.position, Vector3.zero);
            foreach (var c in w.GetComponentsInChildren<Collider>()) b.Encapsulate(c.bounds);

            sb.AppendLine(w.name + " spans " + b.min.ToString("F2") + " to " +
                          b.max.ToString("F2") + " — " +
                          b.size.ToString("F2") + " m, centred on " +
                          w.transform.position.ToString("F2"));
        }

        sb.AppendLine();

        // --- 4. measure it -------------------------------------------------------

        sb.AppendLine("--- MEASURED, NOT ASSUMED ---");
        var widths = new List<float>();
        int blocked = 0, noFloor = 0;
        float lowestCeiling = 999f;

        for (int i = 0; i <= 20; i++)
        {
            var p = start + dir * (Length * i / 20f);
            float gy = GroundAt(p);
            if (gy < -900f) { noFloor++; continue; }

            var from2 = new Vector3(p.x, gy + Chest, p.z);
            float w = WallReach(from2, across) + WallReach(from2, -across);
            widths.Add(w);

            if (CapsuleBlocked(new Vector3(p.x, gy, p.z))) blocked++;

            // Ceiling: from knee height upward, the first thing overhead. Ari's
            // head is at 1.80 m, so anything under that is a crawl, not a gully.
            if (Physics.Raycast(new Vector3(p.x, gy + 1.0f, p.z), Vector3.up,
                                out RaycastHit up, 2.0f, ~0, QueryTriggerInteraction.Ignore))
                lowestCeiling = Mathf.Min(lowestCeiling, up.point.y - gy);
        }

        if (widths.Count == 0)
        {
            sb.AppendLine("NOTHING TO MEASURE — no floor along the corridor.");
            return;
        }

        var sorted = widths.OrderBy(w => w).ToList();

        sb.AppendLine("samples with a floor: " + widths.Count + " of 21" +
                      (noFloor > 0 ? "  (" + noFloor + " have no floor at all)" : ""));
        sb.AppendLine("clear width: median " + sorted[sorted.Count / 2].ToString("0.00") +
                      " m, min " + sorted[0].ToString("0.00") +
                      " m, max " + sorted[sorted.Count - 1].ToString("0.00") +
                      " m  (asked for " + Width.ToString("0.00") + " m)");

        // Every sample, because a median hides the problem. A max of 12.00 m in
        // a corridor whose walls are 1.60 m apart means at least one sample is
        // not between the walls at all, and a summary line alone would let that
        // pass as a gully.
        sb.AppendLine();
        sb.AppendLine("--- every sample ---");
        sb.AppendLine("     at        ground   width   note");

        for (int i = 0; i <= 20; i++)
        {
            var p = start + dir * (Length * i / 20f);
            float gy = GroundAt(p);

            if (gy < -900f)
            {
                sb.AppendLine("  " + i.ToString().PadLeft(2) + "  " +
                              p.ToString("F2").PadRight(20) + "   no floor");
                continue;
            }

            var f = new Vector3(p.x, gy + Chest, p.z);
            float w = WallReach(f, across) + WallReach(f, -across);
            bool capped = w >= 11.9f;

            sb.AppendLine("  " + i.ToString().PadLeft(2) + "  " +
                          p.ToString("F2").PadRight(20) + "  " +
                          gy.ToString("F2").PadLeft(5) + "  " +
                          w.ToString("0.00").PadLeft(5) + "  " +
                          (capped
                              ? "NOT A GULLY POINT — nothing within 6 m either side, " +
                                "so this is not between the walls. " + Nearest(p, gy)
                              : w > 3f ? "wider than the walls are wide apart" : ""));
        }

        sb.AppendLine();
        sb.AppendLine("narrowest point is " + (sorted[0] - AriRadius * 2f)
                          .ToString("0.00") + " m wider than Ari's " +
                      (AriRadius * 2f).ToString("0.00") + " m capsule");
        sb.AppendLine("Ari's capsule blocked at " + blocked + " of " + widths.Count +
                      " samples");
        sb.AppendLine("lowest ceiling: " +
                      (lowestCeiling > 900f ? "open sky" : lowestCeiling.ToString("0.00") + " m") +
                      "  (he is " + (AriRadius * 2f + 1.2f).ToString("0.0") + " m tall, " +
                      "needs 1.80 m)");
        sb.AppendLine();

        bool widthOk = sorted[0] >= 0.90f && sorted[sorted.Count / 2] <= 2.00f;
        bool passOk = blocked == 0;
        bool headOk = lowestCeiling > 1.80f;

        // No sample may sit outside the corridor.
        //
        // Checked against the widest sample and not the median, because the
        // median was 1.60 m and the verdict passed on a run where nine of
        // twenty-one samples were open ground with no wall within six metres.
        // A gully is a claim about the whole of its length; half of one is a
        // corridor with a wall at each end.
        int notInGully = widths.Count(w => w > 3f);

        sb.AppendLine("VERDICT");
        sb.AppendLine("  width " + (widthOk ? "PASS" : "FAIL") + " — " +
                      (widthOk
                          ? "between 0.90 and 2.00 m along its whole length, so it "
                            + "reads as a gully and Ari can walk it"
                          : sorted[0] < 0.90f
                              ? "it pinches to " + sorted[0].ToString("0.00") +
                                " m, which is narrower than his shoulders"
                              : "the median is " + sorted[sorted.Count / 2].ToString("0.00") +
                                " m, which is wide enough to walk two abreast and " +
                                "is not a gully"));
        sb.AppendLine("  continuous " + (notInGully == 0 ? "PASS" : "FAIL") + " — " +
                      (notInGully == 0
                          ? "every one of the " + widths.Count +
                            " samples is between the walls"
                          : notInGully + " of " + widths.Count +
                            " samples are WIDER THAN THE WALLS ARE APART, so the " +
                            "gully stops part way along. A median and a minimum " +
                            "both passed on the previous run while this was " +
                            "failing, which is why it is a separate line."));
        sb.AppendLine("  passable " + (passOk ? "PASS" : "FAIL") + " — " +
                      (passOk ? "his capsule clears every sample"
                              : blocked + " samples have something in him volume"));
        sb.AppendLine("  headroom " + (headOk ? "PASS" : "FAIL") + " — " +
                      (headOk ? "nothing overhead below 1.80 m"
                              : "something is " + lowestCeiling.ToString("0.00") +
                                " m above the floor"));
        sb.AppendLine();

        if (!(widthOk && passOk && headOk && notInGully == 0))
        {
            sb.AppendLine("THE GULLY AS BUILT DOES NOT WORK. The walls are in the " +
                          "scene and the chase is NOT wired to them, because a " +
                          "chase through a passage he cannot walk is worse than " +
                          "no chase. Fix the heading or the width and run again.");
            return;
        }

        // --- 5. wire the chase --------------------------------------------------

        var legs = new List<MonoChase.Leg>();
        const int legCount = 4;
        string[] lineIds = { "beat3.leg1", "beat3.leg2", "beat3.leg3", "beat3.arrive" };

        for (int i = 0; i < legCount; i++)
        {
            var p = start + dir * (Length * i / (legCount - 1f));
            var go = new GameObject("GullyLeg_" + (i + 1));
            go.transform.SetParent(container.transform, false);
            go.transform.position = new Vector3(p.x, GroundAt(p), p.z);
            go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            var leg = new MonoChase.Leg
            {
                point = go.transform,
                triggerRadius = 2.2f,
                lineId = i < lineIds.Length ? lineIds[i] : "beat3.arrive",
            };
            legs.Add(leg);

            sb.AppendLine("leg " + (i + 1) + " of " + legCount + " at " +
                          go.transform.position.ToString("F2") + "  line '" +
                          leg.lineId + "'");
        }

        var chase = UnityEngine.Object.FindAnyObjectByType<MonoChase>();
        if (chase == null)
        {
            sb.AppendLine("No MonoChase in the scene — run Beat 3 setup first. " +
                          "The gully is built; the route is not attached.");
        }
        else
        {
            chase.SetLegs(legs);
            sb.AppendLine();
            sb.AppendLine("MonoChase has " + chase.LegsTotal + " legs, " +
                          chase.LegsLeft + " still to walk.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("scene saved.");
    }

    // --- choosing a heading ------------------------------------------------------

    /// <summary>
    /// Which way the gully runs.
    ///
    /// Away from the square, because a gully that leads back into the market
    /// undoes the beat — Ari has just been shown something there. And clear:
    /// every heading is swept with Ari's own capsule for the whole length the
    /// gully needs, and a heading with anything in it is out. The headings that
    /// were rejected are listed, because "it picked a strange direction" is a
    /// question this should be able to answer.
    /// </summary>
    static Vector3 ChooseDirection(StringBuilder sb, Vector3 from, Vector3 origin)
    {
        var out1 = new Vector3(from.x - origin.x, 0f, from.z - origin.z).normalized;
        float need = SetBack + Length;
        float offset = (Width + WallThickness) * 0.5f;

        var ranked = new List<(Vector3 dir, int clear, int wallHits, float score)>();

        for (int d = 0; d < 16; d++)
        {
            float a = d / 16f * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            var across = new Vector3(-dir.z, 0f, dir.x);

            int clear = 0, total = 0, wallHits = 0;
            for (int i = 1; i <= 14; i++)
            {
                var p = Flat(from) + dir * (need * i / 14f);
                float gy = GroundAt(p);
                if (gy < -900f) continue;

                total++;
                if (!CapsuleBlocked(new Vector3(p.x, gy, p.z))) clear++;
            }

            // The wall lines as well as the centre line, and this is the test
            // that matters.
            //
            // Sweeping only the centre asks whether Ari can walk there, and a
            // gully whose walls have to go somewhere is not a question about
            // Ari. A heading can be perfectly clear down the middle and have a
            // house sitting a metre to either side, and the walls then land
            // inside it: the corridor measures exactly the width that was asked
            // for, because the measurement is between two walls that are buried
            // in a house, and the beat looks built and is not. So each side is
            // swept as a box, the same box the wall will occupy.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i <= 8; i++)
                {
                    var p = Flat(from) + dir * (SetBack + Length * i / 8f)
                          + across * (offset * side);
                    float gy = GroundAt(p);
                    if (gy < -900f) continue;

                    if (WallSiteBlocked(p, dir, gy)) { wallHits++; break; }
                }
            }

            // A heading pointing back at the square is a bad gully even if it is
            // clear, so it is ranked below anything that leads away.
            float away = Vector3.Dot(dir, out1);
            float score = clear + (away > 0.3f ? 100f : away < -0.3f ? -100f : 0f);

            ranked.Add((dir, clear, wallHits, score));
        }

        // A heading with a wall site blocked is not a heading at all, whatever
        // its centre line looks like, so it is filtered out before ranking
        // rather than merely down-weighted.
        var usable = ranked.Where(r => r.wallHits == 0).ToList();

        if (usable.Count == 0)
        {
            sb.AppendLine("EVERY HEADING HAS A WALL SITE BLOCKED.");
            foreach (var r in ranked.OrderBy(r => r.wallHits))
                sb.AppendLine("  " + Compass(r.dir) + ": centre " + r.clear +
                              " of 14 clear, but " + r.wallHits +
                              " of 2 wall lines are inside something");
            return Vector3.zero;
        }

        var best = usable.OrderByDescending(r => r.clear == 14 ? r.score : r.clear)
                         .First();

        sb.AppendLine("headings with nowhere to put a wall, and so not candidates:");
        foreach (var r in ranked.Where(r => r.wallHits > 0))
            sb.AppendLine("  " + Compass(r.dir) + ": " + r.wallHits +
                          " of 2 wall lines blocked");

        return best.clear >= 10f ? best.dir : Vector3.zero;
    }

    /// <summary>
    /// Is the volume a wall would occupy already full?
    ///
    /// Lifted 10 cm off the floor, and that lift is the whole answer to a bug
    /// this function spent one run reporting. Set the box's bottom face exactly
    /// on the measured ground and PhysX counts the floor as being inside it —
    /// touching is overlapping, for an overlap query — so all sixteen headings
    /// came back "both wall lines blocked" in a village that has open ground
    /// between its houses, and the tool concluded it was too dense to build in.
    /// Ari's own capsule sweep is lifted by bodyBottomLift for the same reason.
    /// </summary>
    static bool WallSiteBlocked(Vector3 groundPoint, Vector3 dir, float groundY)
    {
        const float lift = 0.10f;

        return Physics.CheckBox(
            new Vector3(groundPoint.x, groundY + lift + WallHeight * 0.5f, groundPoint.z),
            new Vector3(WallThickness * 0.5f, WallHeight * 0.5f, Length / 16f),
            Quaternion.LookRotation(dir, Vector3.up),
            ~0, QueryTriggerInteraction.Ignore);
    }

    // --- building ----------------------------------------------------------------

    static GameObject Rebuild(StringBuilder sb)
    {
        var old = GameObject.Find(ContainerName);
        if (old != null)
        {
            // Destroyed, not deactivated, and said so: a stale gully left in the
            // scene silently blocks the corridor the new one is trying to measure
            // as clear, and the report then blames the walls for a wall that is
            // not there any more.
            int kids = old.transform.childCount;
            sb.AppendLine("removed a previous '" + ContainerName + "' with " +
                          kids + " child object(s)");
            UnityEngine.Object.DestroyImmediate(old);
        }

        var go = new GameObject(ContainerName);
        return go;
    }

    static Material Material(StringBuilder sb)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath);
        if (mat != null)
        {
            sb.AppendLine("material: " + WallMaterialPath + " (reused)");
            return mat;
        }

        var src = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        if (src == null)
        {
            sb.AppendLine("NO SOURCE MATERIAL at " + SourceMaterialPath +
                          ". The stump's material is what makes placeholder art " +
                          "behave — it is on Echoes/PainterlyLit, and a default " +
                          "URP Lit material drops the _ColorRestore override " +
                          "silently and the walls will never take colour. Not " +
                          "building without it.");
            return null;
        }

        mat = new Material(src) { name = "Beat3_Gully" };
        AssetDatabase.CreateAsset(mat, WallMaterialPath);
        AssetDatabase.SaveAssets();

        sb.AppendLine("created " + WallMaterialPath + " from " + SourceMaterialPath +
                      " — the stump's. Both are on " + src.shader.name +
                      ", which is the one that reads _ColorRestore.");
        return mat;
    }

    static GameObject Wall(Transform parent, string name, Vector3 centre,
                           Vector3 dir, Material mat)
    {
        float gy = GroundAt(centre);

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = new Vector3(centre.x, gy + WallHeight * 0.5f, centre.z);
        cube.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        cube.transform.localScale = new Vector3(WallThickness, WallHeight, Length);

        // The primitive brings a BoxCollider, sized by the transform, which is
        // what Ari's sweep will hit. Kept on Default: every collider in the
        // village is, and wallMask is ~0, so a different layer would be
        // invisible to him.

        var rend = cube.GetComponent<Renderer>();
        if (rend != null && mat != null) rend.sharedMaterial = mat;

        return cube;
    }

    // --- measuring ---------------------------------------------------------------

    static float GroundAt(Vector3 at)
    {
        var hits = Physics.RaycastAll(at + Vector3.up * 30f, Vector3.down, 60f, ~0,
                                      QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return -999f;

        float lowest = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
            if (hits[i].collider != null && hits[i].point.y < lowest)
                lowest = hits[i].point.y;

        return lowest == float.MaxValue ? -999f : lowest;
    }

    static float WallReach(Vector3 from, Vector3 dir)
    {
        return Physics.Raycast(from, dir, out RaycastHit hit, 6f, ~0,
                               QueryTriggerInteraction.Ignore)
            ? hit.distance
            : 6f;
    }

    static bool CapsuleBlocked(Vector3 feet)
    {
        // The same capsule AriMover.CapsuleAt builds: bottom centre lifted clear
        // of the floor by bodyBottomLift rather than sunk into it. A capsule
        // whose bottom sphere is centred on the ground always overlaps the floor
        // and reports every spot on the level as blocked, which is how an
        // earlier version of the cast tool placed nothing at all.
        return Physics.OverlapCapsuleNonAlloc(
            feet + Vector3.up * (AriRadius + 0.10f),
            feet + Vector3.up * (1.50f),
            AriRadius, Buffer, ~0, QueryTriggerInteraction.Ignore) > 0;
    }

    // --- small helpers -----------------------------------------------------------

    /// <summary>
    /// What is actually standing at this spot, by name.
    ///
    /// "Blocked" on its own is not a finding. Whether a wall site is refused
    /// because of a house, a fence or the village boundary changes what to do
    /// about it, and a run that only counts hits makes the designer go and look.
    /// </summary>
    static string Nearest(Vector3 at, float groundY)
    {
        // RaycastAll, not RaycastAllNonAlloc: there is no such name, and the
        // results-taking form of the allocating call does not exist either. Only
        // the NonAlloc family takes a buffer, and a one-line helper for a debug
        // string is not worth the buffer.
        var down = Physics.RaycastAll(
            new Vector3(at.x, groundY + Chest, at.z), Vector3.down, 3.0f, ~0,
            QueryTriggerInteraction.Ignore);

        var names = new List<string>();
        foreach (var h in down)
            if (h.collider != null && !names.Contains(h.collider.name))
                names.Add(h.collider.name);

        if (names.Count == 0) return "nothing identifiable, just a hit";
        return string.Join(", ", names.Take(3).ToArray());
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    static string Compass(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
            return dir.x > 0f ? "east" : "west";
        return dir.z > 0f ? "north" : "south";
    }
}
