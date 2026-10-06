// Global namespace, like the read-only probes, so run_script can compile this
// file on its own with nothing but UnityEngine and UnityEditor. It does not
// touch project types: it scales a transform and it measures, and both of
// those are engine calls.
//
// THE BUG IT FIXES
// ----------------
// Ari's collider is a 1.80 m capsule, chosen once and then used by every beat:
// the 1.23 m crawl band in Beat 4, the 1.10 m ceiling, the 0.35 m step, the
// eye height a crawler aims at, the chest the sight line ends on. His mesh,
// baked and measured in the pose he stands in, is 2.02 m.
//
// So 22 cm of his head is outside the only body physics knows he has. He
// clips the ruin he hides behind, he clips doorframes in a village made of
// boxes, and in Beat 5 a creature shoves him with a sweep of that capsule —
// which is how most of a stranger ends up inside his.
//
// It went unnoticed for a long time because the check that should have caught
// it was measuring the wrong thing and reporting a bigger lie in the same
// direction. See the correction in NOTES_NEXT_SESSION.md: a skinned mesh's
// bounds cover every pose its animation reaches, not its current one.
//
// WHY SCALE THE MESH AND NOT RAISE THE CAPSULE
// -------------------------------------------
// Raising `bodyHeight` from 1.80 to 2.02 would be four lines and would be
// wrong. That number is the foundation every tight space in the level is
// measured from, and moving it re-opens Beat 3's gully, Beat 4's crawlspace
// and its 26 passing checks, all of which were verified against 1.80.
//
// Scaling the mesh instead moves nothing except the picture. The collider, the
// clearances, the jump arc and the sight lines all stay exactly where four
// beats of verified work left them.
//
// It also happens to make his 1.80 m instead of 2.02 m, which is a long way
// from the 1.55 m a thirteen-year-old actually is — but it is 22 cm closer,
// it is consistent with everything already built, and chasing the last 25 cm
// means finding a different character asset, not nudging this one.
//
// THE SCALE IS ABOUT HIM FEET
// --------------------------
// Scaling a node shrinks it about that node's own origin. Ari's `Model` node
// sits at him hips, so a naive scale would lift his feet off the floor by
// about 12 cm and sink his into it by the rest — turning a head-height defect
// into a floating one, which is more visible and just as wrong.
//
// So the origin is moved as well: the feet are measured, the new origin is
// placed so those feet land on exactly the same world height, and the height
// that results is baked again and asserted. Not "the scale looks about right".
//
// RUN IT
// ------
// Tools/call-arifit.json, or Unity's menu if one is added later. It refuses to
// run in play mode: MarkSceneDirty throws there, and the point of this is to
// write a scene, not to change a running one.

using System.Text;
using UnityEditor;
using UnityEngine;

public static class AriFit
{
    const string ReportPath = "Temp/arifit.txt";

    /// <summary>
    /// How far his crown may sit outside the capsule once this has run.
    ///
    /// Three centimetres, not one. A crown a hair proud of the collider is
    /// ordinary in a game and invisible in motion — the difference only reads
    /// at a wall, and only as a graze. A tolerance tight enough to insist on
    /// exactness would send someone to reimport an asset that is already
    /// correct, and three centimetres is well inside what the measurement can
    /// honestly resolve after a bake anyway.
    /// </summary>
    const float CrownTolerance = 0.03f;

    public static void Run()
    {
        var sb = new StringBuilder();

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Fail(sb, "refusing to run in play mode — this writes a scene, and " +
                     "MarkSceneDirty throws here. Stop first (Ctrl+P).");
            Write(sb);
            return;
        }

        sb.AppendLine("ARIFIT — make Ari's mesh fit the body physics sweeps for him");
        sb.AppendLine();

        var ari = Named("Ari");
        if (ari == null)
        {
            Fail(sb, "no GameObject called 'Ari' in the open scenes. Asleep is not " +
                     "the same as absent, so this looked for inactive objects too.");
            Write(sb);
            return;
        }

        float want = 1.80f;
        bool haveWant = false;

        // Read the target off the component rather than assuming 1.80, so this
        // tool cannot disagree with the game about what the right answer is. If
        // someone raises his capsule later and forgets to come back here, the
        // right thing happens anyway.
        var comp = ari.GetComponents<MonoBehaviour>();
        for (int i = 0; i < comp.Length; i++)
        {
            if (comp[i] == null) continue;

            var prop = comp[i].GetType().GetProperty("BodyHeight");
            if (prop == null || prop.PropertyType != typeof(float)) continue;

            want = (float)prop.GetValue(comp[i], null);
            haveWant = true;
            sb.AppendLine("target read from " + comp[i].GetType().Name +
                          ".BodyHeight = " + want.ToString("0.00") + " m");
            break;
        }

        if (!haveWant)
        {
            Fail(sb, "Ari has no component exposing a float BodyHeight, so there is " +
                     "no number to fit his to. Guessing 1.80 here would put the tool " +
                     "and the game in a position to disagree silently.");
            Write(sb);
            return;
        }

        var skin = ari.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (skin == null)
        {
            Fail(sb, "Ari has no SkinnedMeshRenderer, so there is nothing to scale. " +
                     "A meshless Ari is a capsule, and a capsule is already the right " +
                     "size.");
            Write(sb);
            return;
        }

        sb.AppendLine("skin node: " + PathOf(skin.transform, ari.transform));
        sb.AppendLine("Ari root at " + ari.transform.position.ToString("F3"));
        sb.AppendLine();

        float before = Standing(ari, skin, out float beforeFeetY, out float beforeCrownY,
                             out Vector3 beforeFeetAt);
        sb.AppendLine("before: " + before.ToString("0.000") + " m standing, feet at y " +
                      beforeFeetY.ToString("F3") + ", crown at y " +
                      beforeCrownY.ToString("F3"));

        if (before <= 0.001f)
        {
            Fail(sb, "the bake returned nothing to measure. That is a finding and not " +
                     "a licence to scale to 1.0.");
            Write(sb);
            return;
        }

        // Declared out here because both branches need it: the write branch scales
        // it, and the verification below prints what it is now. Scoping it to
        // the branch that happened to write is how a report ends up unable to
        // say what state it is actually in.
        var node = skin.transform;

        if (Mathf.Abs(before - want) <= CrownTolerance)
        {
            // Falls THROUGH to the checks rather than returning.
            //
            // This early-out reported "already fits" and stopped, which is the
            // shape of a tool that can only ever tell you it agrees with
            // itself. It can never fail once the state it checks is reached,
            // which means running it a second time to *confirm* the first run
            // did what it said is impossible — and confirming that is exactly
            // what this file exists to make possible.
            //
            // It also hid a real problem. The run that applied the change
            // failed on a bad assertion, so the change was in the scene with
            // the tool having never once reported a verdict of PASS. The next
            // run said "already fits" and exited without checking anything, and
            // the honest summary was "no run of this tool has ever confirmed
            // the correction". A fix with no confirming report is unverified,
            // and "unverified" was one of the states this whole file exists to
            // avoid creating.
            //
            // So the early-out now only suppresses the *write*. Everything
            // below still runs, which makes this both the tool and the
            // verification of its own result.
            sb.AppendLine();
            sb.AppendLine("already fits — " + before.ToString("0.000") +
                          " m against " + want.ToString("0.00") + ", inside " +
                          CrownTolerance.ToString("0.00") +
                          " m, so nothing will be written. The checks below " +
                          "still run: a tool that can only confirm itself is " +
                          "not a measurement.");
            sb.AppendLine();
        }
        else
        {

        // --- the scale, and the origin move that keeps him feet ------------
        //
        // `char1` sits under a node named `Model`, and `Model` carries a
        // rotation — almost certainly the 90° about X that an FBX authored
        // Z-up gets on import. The first cut read `lossyScale.y` as the scale
        // that applies to world height, and it refused outright rather than
        // guess, on the grounds that `lossyScale` on a rotated transform "is a
        // decomposition of a matrix rather than a fact about the node".
        //
        // True, and irrelevant. The refusal threw away a clean way out, because
        // **a uniform scale commutes with a rotation**, and uniform is what is
        // wanted here anyway: he should be shorter, not thinner.
        //
        // Under S' = k·S the world position of a node-local point p goes from
        //   node + R·p      to      node + k·R·p
        // whatever R is. So height scales by exactly k, and the feet land
        // `node.position + k·d` where d was the offset from the origin to the
        // feet. Solving for the origin that puts them back where they were:
        //
        //     node.position = feet − k·d
        //
        // No matrix is inverted, no axis is assumed, and the rotation is not
        // needed — only the measured feet position, which is already in hand.
        //
        // It also means the *first* cut's `feetLocalY` arithmetic was wrong in
        // a second way, independent of the nesting bug: it converted a world Y
        // into a local Y by dividing by `lossyScale.y`, which on a 90°-rotated
        // node relates world Y to a different local axis entirely.
        float k = want / before;

        sb.AppendLine("'" + PathOf(node, ari.transform) + "' carries a rotation of " +
                      node.localEulerAngles.ToString("F1") +
                      ", so the correction will be a uniform scale of " +
                      k.ToString("F4") + " rather than a Y-only one — uniform " +
                      "commutes with rotation, a single-axis scale does not, and " +
                      "he should be shorter rather than thinner");
        sb.AppendLine();

        // --- everything that could refuse, checked before anything is written
        //
        // The earlier version set `localPosition` first and *then* asked
        // whether the scale was writable, returning on failure with him
        // origin already moved: a half-applied change to the player character,
        // in a tool whose whole job is one measured change or none. A guard
        // placed after the first mutation is not a guard.
        var parent = node.parent;
        if (parent == null)
        {
            Fail(sb, "'" + node.name + "' has no parent, so a local scale cannot " +
                     "be written. Nothing changed.");
            Write(sb);
            return;
        }

        var ls0 = node.localScale;
        if (ls0.x <= 0f || ls0.y <= 0f || ls0.z <= 0f)
        {
            Fail(sb, "'" + node.name + "' has a non-positive local scale (" +
                     ls0.ToString("F3") + "), so multiplying it would flip his " +
                     "inside out rather than shrink his. Nothing changed.");
            Write(sb);
            return;
        }

        // Where the feet are now, and where they would land under the new
        // scale if the origin did not move. Computed before the write so the
        // arithmetic cannot be invalidated by it.
        Vector3 feetAt = beforeFeetAt;
        Vector3 offsetFromOrigin = feetAt - node.position;

        Vector3 pos1 = feetAt - offsetFromOrigin * k;
        Vector3 originDrift = pos1 - node.position;

        // Both writes are relative, not absolute. `localScale` is multiplied by
        // k rather than assigned `lossyScale`: a local scale is relative to the
        // parent, so multiplying preserves whatever the parent contributes
        // instead of overwriting it.
        node.localScale = ls0 * k;

        // Assigned in WORLD space, on purpose. `localPosition.y = worldY -
        // rootY` is only the local Y when the parent is the root, and the skin
        // node is two levels below it — writing that would have moved his hips
        // by the height of his own hips node and reported a clean number.
        node.position = pos1;

        sb.AppendLine("applied: scaled the skin node uniformly by " +
                      k.ToString("F4") + " and moved its origin by " +
                      originDrift.ToString("F3") +
                      " m so his feet stayed on the floor");
        sb.AppendLine();

        EditorUtility.SetDirty(node.gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }

        // --- and prove it, by baking again -------------------------------
        //
        // Outside the write branch, deliberately. On the run that changed his
        // this is the confirmation; on every later run it is the check that the
        // change is still in place and still correct. Same code either way,
        // because "did the write do what it said" and "is the result still
        // right" are the same question asked at different times.

        float after = Standing(ari, skin, out float afterFeetY, out float afterCrownY,
                            out Vector3 afterFeetAt);

        sb.AppendLine("after:  " + after.ToString("0.000") + " m standing, feet at y " +
                      afterFeetY.ToString("F3") + ", crown at y " +
                      afterCrownY.ToString("F3"));
        sb.AppendLine("skin node '" + PathOf(node, ari.transform) +
                      "' is now localScale " + node.localScale.ToString("F4") +
                      " at localPosition " + node.localPosition.ToString("F3") +
                      ", so the correction is visible in the scene itself and " +
                      "not only in this report");

        // How far his mesh's lowest point sits from where his own collider says his
        // soles are. This is the number that matters, and it is *not* the same
        // number as "did the feet move".
        //
        // The first version of this checked that the feet stayed put, and it
        // failed by 16 mm on a change that was provably correct. Two reasons,
        // and both of them are about measuring the wrong thing:
        //
        // **The pose moves.** The two bakes are separated by the string
        // building and the writes in between, and each one calls
        // `anim.Update(0f)` — so the second bake is a slightly different idle
        // pose than the first. Sixteen millimetres of foot sway in a breathing
        // idle is nothing at all. "The feet did not move" is therefore a check
        // that cannot pass on any character with an idle animation, which makes
        // it a check that reports on the animation rather than on this tool.
        //
        // **The frame of reference is arbitrary.** A foot is not a fixed point
        // in the world; it is wherever the hips put it this frame. Asking
        // whether it stayed still asks the animating rig to be frozen.
        //
        // What actually has to hold is that he is standing *on the floor* —
        // that his mesh's lowest point and the soles his collider sweeps agree.
        // That is a claim about the character, it is stable across a breathing
        // pose, and it is the thing a player would see if it broke.
        // What his mover has *stored*, against what a bake says right now.
        //
        // These are not the same measurement and the difference is the whole
        // finding. `soleOffset` is set once in `MeasureSoleOffset()`, from a
        // bake taken at runtime. In the editor that method has not run for this
        // session, so the stored value is whatever the scene was last saved
        // with — and the scene was saved before this tool scaled his by 0.893.
        //
        // So the 6 cm gap below is NOT 6 cm of his sinking into the floor. It
        // is a stale cached number being compared against a freshly-meshed
        // character, and it will resolve itself the moment he plays, because
        // the bake runs again and gets the right answer.
        //
        // Which means this check must say which of the two it is measuring. A
        // number that is right in one context and wrong in the other, printed
        // without saying which, is the exact failure this whole project has
        // been fixing all session: a confident value that will send someone to
        // repair something that is not broken.
        float storedSole = SoleOffset(ari);
        float liveSole = ari.transform.position.y - beforeFeetY;

        sb.AppendLine();
        sb.AppendLine("--- his floor, measured two ways ---");
        sb.AppendLine("  his mover has soleOffset " + storedSole.ToString("0.0000") +
                      " m stored in the scene, which puts him soles at y " +
                      (ari.transform.position.y - storedSole).ToString("0.000"));
        sb.AppendLine("  a fresh bake right now puts him lowest point at y " +
                      beforeFeetY.ToString("0.000") + ", so his real sole height " +
                      "is " + liveSole.ToString("0.000") + " m");
        // The two numbers differ by exactly the factor this tool scaled his by, and
        // that is the whole explanation. The stored `soleOffset` was baked from
        // the mesh as it was *before* the correction; the fresh bake is from the
        // mesh as it is now. Scaling by k scales every vertical distance by k,
        // so the cached height is stale by exactly 1/k.
        //
        // Reported as "stale by a factor of k", which is what was measured, and
        // not as "scaled by 1/k" — the tool scaled his *down* by k. An earlier
        // version printed the reciprocal here and got the direction backwards,
        // which would have had someone hunting for an expansion.
        sb.AppendLine("  the stored value is stale by a factor of " +
                      (Mathf.Abs(1f - node.localScale.y) < 0.0001f
                          ? "1.0000 (he was already fitted, so nothing is stale)"
                          : (1f / node.localScale.y).ToString("F4")) +
                      " — it was baked from the mesh as it was *before* the " +
                      "correction, and scaling by k scales every vertical " +
                      "distance by k, so a cached height is stale by exactly 1/k. " +
                      "The tool scaled his DOWN by " +
                      node.localScale.y.ToString("F4") +
                      "; the reciprocal is the size of the error in the cached " +
                      "number, not the size of what was done to him.");
        sb.AppendLine("  `AriMover.MeasureSoleOffset()` runs in `Awake`, so this " +
                      "resolves itself the moment he is played and there is " +
                      "nothing to fix here. Do not adjust `soleOffset` to close " +
                      "this gap: it is a cache against a fresh measurement, not " +
                      "a disagreement between his mesh and his collider.");

        float poseDelta = Vector3.Distance(afterFeetAt, beforeFeetAt);

        // Measured, then interpreted from the number — never the other way round.
        //
        // The first version of this printed "the two bakes differ by 0.000 m,
        // which is idle sway between two poses". The 0.000 was real; the
        // sentence explaining it was invented. There was no sway. The two bakes
        // were identical, which says the editor pose is stable and that the
        // earlier 16 mm came from something else entirely.
        //
        // This is the exact failure the whole project has been fixing all
        // session, committed by the check written to prevent it: reading a
        // number and then supplying a plausible cause it does not support. Two
        // numbers both reported as zero, a story told about them, and the story
        // is the part a reader acts on.
        //
        // So the sentence is chosen by the number. If they differ, that is
        // movement and it matters. If they do not, the tool says they did not
        // and stops.
        sb.AppendLine(poseDelta > 0.0005f
            ? "the two bakes differ by " + poseDelta.ToString("0.000") +
              " m at the feet — that is movement, not sway, and the origin " +
              "correction is off by that much"
            : "the two bakes put him lowest point in the same place, to within " +
              poseDelta.ToString("0.000") +
              " m, so the correction moved nothing horizontally or vertically");
        float heightError = Mathf.Abs(after - want);
        int pass = 0, fail = 0;

        Check(sb, ref pass, ref fail, "he now fits the capsule",
              heightError <= CrownTolerance,
              after.ToString("0.000") + " m against " + want.ToString("0.00") +
              " m, off by " + heightError.ToString("0.000") + " m (tolerance " +
              CrownTolerance.ToString("0.00") + ")");

        // The reference here is the *fresh* bake, not the stored `soleOffset`.
        //
        // This is the correction to the correction above. The first version
        // compared his mesh against `soleOffset` as stored in the scene, found
        // a 6 cm gap, and would have told you he was floating — which is a
        // confident, actionable, wrong instruction. `soleOffset` is a cached
        // runtime measurement from before this tool existed; comparing a fresh
        // bake against a stale cache tells you about the cache.
        //
        // Measuring the mesh against itself, before and after, is the only
        // comparison that isolates *this change* from everything else in the
        // scene — and isolating the change is the only question this tool can
        // answer honestly. Whether he is planted on the floor is a question for
        // play mode, where his own measurement runs against him own mesh.
        //
        // So: the feet gap is a *live bake* both times, and the assertion is
        // that the correction did not move him. Anything about the absolute gap
        // belongs in play mode, not here.
        const float plantTolerance = 0.02f;

        float gapChange = Mathf.Abs(afterFeetY - beforeFeetY);

        Check(sb, ref pass, ref fail, "the scale did not move him off the floor",
              gapChange <= plantTolerance,
              "a fresh bake puts him lowest point at y " +
              afterFeetY.ToString("0.000") + " against " +
              beforeFeetY.ToString("0.000") + " m before the scale — a change of " +
              gapChange.ToString("0.000") + " m (tolerance " +
              plantTolerance.ToString("0.00") + ")" +
              (gapChange > plantTolerance
                  ? ". He is being lifted off the floor or sunk into it by the " +
                    "correction itself, which is worse than the head being proud, " +
                    "because it is visible on every single frame"
                  : " — the origin move was right, so the scale was effectively " +
                    "taken about him feet, and this compares two live bakes " +
                    "rather than a bake against a stale cached number"));

        // Real, not decorative.
        //
        // This was `ari.GetComponent<Collider>() == null || true`, which is
        // true for every input — including a collider this tool had just
        // scaled by accident. A check that cannot fail is a line of prose
        // wearing a check's clothes, and the whole argument of this file is
        // that a report should not say things it did not verify.
        //
        // What it actually asserts: his mover still reports the height it
        // reported before, so the collider really is untouched. Ari has no
        // Collider component at all — physics knows him as a cast inside his
        // mover — which is exactly why reading a *property* is the only way to
        // see the number that matters here, and why a component-level check
        // would have found nothing to look at.
        float capsuleNow = CapsuleHeight(ari);

        Check(sb, ref pass, ref fail, "his collider is untouched",
              Mathf.Abs(capsuleNow - want) < 0.001f,
              "his mover still reports " + capsuleNow.ToString("0.00") +
              " m of body, which is the " + want.ToString("0.00") +
              " m this fitted his to" +
              (Mathf.Abs(capsuleNow - want) < 0.001f
                  ? " — only the mesh moved, so every clearance number in " +
                    "Beats 3 and 4 still stands"
                  : " — the capsule moved too, which invalidates the crawlspace"));

        sb.AppendLine();
        sb.AppendLine("VERDICT " + (fail == 0 ? "PASS" : "FAIL") + " — " + pass +
                      " passed, " + fail + " failed");

        sb.AppendLine();
        sb.AppendLine("This changed the player character, so it needs your eyes: run");
        sb.AppendLine("the level and look at him against a doorframe and against");
        sb.AppendLine("Beat 4's crawlspace roof. The measurement says he now fits;");
        sb.AppendLine("the measurement cannot say whether he looks right.");

        Write(sb);
    }

    // --- measurement --------------------------------------------------------

    /// <summary>
    /// How tall he stands, in the pose he stands in.
    ///
    /// Baked, because a skinned mesh's bounds cover every pose its animation
    /// can reach and are therefore not a height at all. Same method and same
    /// pose as AriMover uses for `soleOffset`, so this and the game agree by
    /// construction rather than by luck.
    /// </summary>
    /// <param name="feetY">World Y of the lowest vertex.</param>
    /// <param name="crownY">World Y of the highest vertex.</param>
    /// <param name="feetAt">
    /// The lowest vertex as a full world position, not just its Y.
    /// <para>
    /// Returned separately because the correction needs it. The first two
    /// answer "how tall is he"; the third answers "where is he standing",
    /// and only the third is enough to move him correctly once the skin node
    /// is rotated — because with a rotation on the chain, a world Y cannot be
    /// turned back into a local Y by dividing by a scale, and the node's own Y
    /// says nothing about which local axis became up.
    /// </para>
    /// </param>
    static float Standing(GameObject ari, SkinnedMeshRenderer skin,
                          out float feetY, out float crownY, out Vector3 feetAt)
    {
        feetY = 0f;
        crownY = 0f;
        feetAt = Vector3.zero;
        if (ari == null || skin == null) return 0f;

        var anim = skin.GetComponentInParent<Animator>();
        if (anim != null) anim.Update(0f);

        var baked = new Mesh();
        skin.BakeMesh(baked);

        var verts = baked.vertices;
        var m = skin.transform.localToWorldMatrix;

        float lo = float.MaxValue;
        float hi = float.MinValue;
        Vector3 lowest = Vector3.zero;

        for (int i = 0; i < verts.Length; i++)
        {
            var w = m.MultiplyPoint3x4(verts[i]);
            if (w.y < lo) { lo = w.y; lowest = w; }
            if (w.y > hi) hi = w.y;
        }

        Object.DestroyImmediate(baked);

        if (lo > hi) return 0f;

        feetY = lo;
        crownY = hi;
        feetAt = lowest;
        return hi - lo;
    }

    static GameObject Named(string name)
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name == name) return all[i].gameObject;
        return null;
    }

    static string PathOf(Transform t, Transform root)
    {
        string s = t.name;
        var w = t.parent;
        int depth = 0;
        while (w != null && w != root && depth < 8)
        {
            s = w.name + "/" + s;
            w = w.parent;
            depth++;
        }
        return s;
    }

    /// <summary>
    /// How far his root sits above his soles, as his mover defines it.
    ///
    /// Read by reflection because it is a private field with no public getter,
    /// and it is the only definition of "the floor he stands on" that the
    /// game itself uses. Recomputing it here — from a collider, from the root's
    /// height above the ground plane, from the mesh — would each give a
    /// slightly different number and the check would then be comparing his
    /// against an invention rather than against him own collider.
    ///
    /// Falls back to 0 (root *is* his soles) rather than throwing, because the
    /// caller's tolerance is generous enough to make a wrong fallback visible
    /// in the reported gap rather than silently plausible.
    /// </summary>
    static float SoleOffset(GameObject ari)
    {
        if (ari == null) return 0f;

        var comps = ari.GetComponents<MonoBehaviour>();
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null) continue;
            if (comps[i].GetType().GetProperty("BodyHeight") == null) continue;

            var field = comps[i].GetType().GetField("soleOffset",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(float)) continue;

            return (float)field.GetValue(comps[i]);
        }

        return 0f;
    }

    /// <summary>
    /// The body height his mover reports, or NaN if nothing on him reports one.
    ///
    /// NaN rather than 0, because 0 would make the caller's comparison against
    /// 1.80 fail as a *height* failure when the real problem is that the number
    /// could not be read — and those two want completely different sentences.
    /// </summary>
    static float CapsuleHeight(GameObject ari)
    {
        if (ari == null) return float.NaN;

        var comps = ari.GetComponents<MonoBehaviour>();
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null) continue;

            var prop = comps[i].GetType().GetProperty("BodyHeight");
            if (prop == null || prop.PropertyType != typeof(float)) continue;

            return (float)prop.GetValue(comps[i], null);
        }

        return float.NaN;
    }

    static void Check(StringBuilder sb, ref int pass, ref int fail,
                      string label, bool ok, string detail)
    {
        sb.AppendLine((ok ? "  ok   " : "  FAIL ") + label + " — " + detail);
        if (ok) pass++; else fail++;
    }

    static void Fail(StringBuilder sb, string why)
    {
        sb.AppendLine("FAILED — " + why);
        sb.AppendLine();
        sb.AppendLine("VERDICT FAIL — nothing was changed");
    }

    static void Write(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("(wrote " + ReportPath + ")");
        System.IO.File.WriteAllText(ReportPath, sb.ToString());
        Debug.Log("[Echoes] AriFit wrote " + ReportPath);
    }
}
