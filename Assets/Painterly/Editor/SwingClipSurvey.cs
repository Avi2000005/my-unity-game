using System.Collections.Generic;
using System.IO;
using System.Text;
using Echoes.Painterly;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Measures every Ari clip for one job: can it be a brush stroke?
///
/// WHY A SURVEY AND NOT A TRIM
///
/// The swing clip was measured properly. Ari_Attack's hand travels 0.866m, which
/// is plenty — but its movement is spread over 2.00 seconds and its peak arrives
/// 0.97 seconds in. The best possible 0.69s window captures 54% of the movement
/// and still puts the strike 0.43s after the click.
///
/// That gap is not a trimming problem. An attack clip is a long gesture by
/// design, and a button press wants the short middle of one. No offset fixes a
/// clip whose whole shape is wrong, so the only remaining question is whether
/// another imported clip is the right shape.
///
/// CRITERIA, IN ORDER OF IMPORTANCE
///
///   1. Time from the start of motion to the peak. This is the wait the player
///      feels after clicking. Under 0.18s reads as frame-one response; over
///      0.35s reads as the arm hesitating, and no amount of good colour on the
///      mark will fix that.
///   2. How much of a short window the clip's movement fits into. A clip whose
///      movement is concentrated is trimmable; one spread thin is not.
///   3. Hand travel. Needs to be enough to read as a stroke at all.
///   4. Clip length. Short is better, but only once the above are satisfied.
///
/// The window is searched per clip, not fixed, because a clip's best window
/// starts wherever its movement is.
/// </summary>
public static class SwingClipSurvey
{
    const string Report = "Temp/swing_survey.txt";

    /// <summary>Frames per second, matched to the import rate seen in the probe.</summary>
    const float Fps = 30f;

    /// <summary>Longest wait after a click before the strike, in seconds.</summary>
    const float MaxWaitToPeak = 0.18f;

    /// <summary>Wait at which the hesitation stops being acceptable.</summary>
    const float HesitationWait = 0.35f;

    /// <summary>Movement a good brush window should be able to hold.</summary>
    const float GoodCapture = 0.60f;

    /// <summary>Hand travel below which nothing reads as a stroke.</summary>
    const float MinTravel = 0.30f;

    public static void Run()
    {
        var sb = new StringBuilder();
        try { Body(sb); }
        catch (System.Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] swing survey\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    struct Result
    {
        public string name;
        public float length;
        public float travel;
        public float legTravel;      // how far a foot travels: a walk cycle moves
        public float peakTime;      // from the start of motion
        public float windowLen;
        public float capture;       // share of movement inside the window
        public int   windowStart;
        public int   windowEnd;
        public float waitToPeak;    // from the start of the window
        public bool  usable;
        public string why;
        public string warn;       // passes, but something to know before using it
        public float  neededSpeed;  // playback multiple needed to meet the timing
    }

    static void Body(StringBuilder sb)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            sb.AppendLine("STOPPED: the editor is in play mode. Nothing is changed.");
            return;
        }

        var clips = new List<AnimationClip>();
        foreach (var dir in new[] { "Assets/Art/Ari/Models", "Assets/Art/L1" })
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { dir }))
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    var c = o as AnimationClip;
                    if (c == null || c.legacy || c.name.StartsWith("__")) continue;
                    if (c.name.StartsWith("Crawler") || c.name.StartsWith("Mono")) continue;
                    clips.Add(c);
                }
            }
        }

        sb.AppendLine("ARI CLIPS, JUDGED AS A BRUSH STROKE");
        sb.AppendLine();
        sb.AppendLine("Three criteria on the arm, then two warnings:");
        sb.AppendLine("  wait to peak    under " + MaxWaitToPeak.ToString("F2") + "s   (how long the arm hesitates after the click)");
        sb.AppendLine("  capture         over " + (GoodCapture * 100f).ToString("F0") + "%     (how much of the movement a short window can hold)");
        sb.AppendLine("  hand travel     over " + MinTravel.ToString("F2") + "m     (enough to read as a stroke)");
        sb.AppendLine();
        sb.AppendLine("  The arm passing is necessary, not sufficient. A clip can swing a hand");
        sb.AppendLine("  perfectly well and still be the wrong thing to play as a brush stroke:");
        sb.AppendLine("  a walk cycle moves the legs too, so Ari would appear to walk in place");
        sb.AppendLine("  mid-stroke. So leg travel is measured and reported alongside.");
        sb.AppendLine();
        sb.AppendLine();

        var results = new List<Result>();

        // Per clip, not per survey. The rig is resolved inside Measure, because
        // each clip is a separate FBX and a shared rig measures a clip against a
        // skeleton that is not its own. A rig that will not load now costs one
        // row rather than the whole survey.
        foreach (var clip in clips)
            results.Add(Measure(clip));

        results.Sort((a, b) =>
        {
            if (a.usable != b.usable) return a.usable ? -1 : 1;
            float aw = a.usable ? a.waitToPeak : a.peakTime;
            float bw = b.usable ? b.waitToPeak : b.peakTime;
            if (!Mathf.Approximately(aw, bw)) return aw.CompareTo(bw);
            return (-a.capture).CompareTo(-b.capture);
        });

        foreach (var r in results)
        {
            sb.AppendLine((r.usable ? "  PASS  " : "        ") + r.name.PadRight(20) +
                          r.length.ToString("F2") + "s  travel " + r.travel.ToString("F2") +
                          "m  feet " + r.legTravel.ToString("F2") + "m" +
                          "  peak at " + r.peakTime.ToString("F2") + "s" +
                          "  win " + r.windowStart + ".." + r.windowEnd +
                          " (" + r.windowLen.ToString("F2") + "s)" +
                          "  capture " + (r.capture * 100f).ToString("F0") + "%" +
                          "  wait " + (float.IsInfinity(r.waitToPeak) ? "n/a" :
                                      r.waitToPeak.ToString("F2") + "s") +
                          (r.neededSpeed > 1.01f
                              ? "  needs " + r.neededSpeed.ToString("F2") + "x" : ""));

            // `why` is only assigned when a clip fails, so a passing row leaves
            // it null — and reaching .Length on that threw, killing the report on
            // the very row that mattered most. The one clip that passed was the
            // clip we had spent the whole survey looking for.
            if (!string.IsNullOrEmpty(r.why)) sb.AppendLine("            " + r.why);
            if (!string.IsNullOrEmpty(r.warn)) sb.AppendLine("            WARNING: " + r.warn);
        }

        sb.AppendLine();

        var winners = new List<Result>();
        foreach (var r in results) if (r.usable) winners.Add(r);

        sb.AppendLine("--- verdict ---");

        if (winners.Count == 0)
        {
            sb.AppendLine("No imported Ari clip is the right shape for a brush stroke.");

            // Name the nearest miss by how close it came, not by whatever sorted
            // first. Sorting puts unusable rows together and the first of them is
            // whichever idle clip happened to sort ahead — so the previous
            // version cheerfully reported "the closest was Ari_Idle_Legacy at
            // wait 0.00s and capture 0%", a clip whose hand never moved at all.
            // An idle loop is not a near miss; it is the opposite of a stroke.
            // Nullable, because Result is a struct and there is no null for it.
            // A plain Result seeded with default values would be a plausible-
            // looking row — all zeros — and the report would name it as the
            // closest clip.
            // Ranked by how few criteria the clip fails, not by capture.
            //
            // Ranking by capture alone picked Ari_Run: 100% of its movement fits
            // one window, which is the best score in the list. But 95% of that
            // movement is leg stride — Ari_Run was chosen as the closest thing to
            // a brush stroke on the strength of the one measurement that a run
            // cycle wins by construction, and it would have put Ari sprinting on
            // the spot while painting. Counting failures makes a clip with feet
            // that stay still outrank a clip whose every number is high because
            // it is running.
            // nearHas, not near == null: Result is a struct, so there is no null for it.
            // Got wrong twice in this file — once as a plain Result, once as a
            // nullable that later got unwrapped back to a plain struct. Both
            // times a compile error rather than a wrong answer, which is the
            // better of the two failure modes.
            bool nearHas = false;
            Result near = default(Result);
            int nearFails = int.MaxValue;

            foreach (var r in results)
            {
                if (r.travel < MinTravel) continue;           // never a stroke at all

                // Moving feet disqualify a clip outright, rather than counting as
                // one failure among four.
                //
                // Counting made Ari_Walk the winner with "1 of 4 failed", beating
                // Ari_HitReact which also failed exactly one — and the one it
                // failed was 0.02s of timing against a 183% leg swing. Those are
                // not the same size of problem. One is a shave of polish on a
                // clip that already works; the other is Ari marching on the spot
                // while he paints, which no amount of correct timing hides.
                // A tie on count should therefore never be broken in favour of a
                // locomotion clip.
                if (r.legTravel > r.travel * 0.10f) continue;

                int fails = 0;
                if (float.IsInfinity(r.waitToPeak)) fails++;  // peak outside window
                else if (r.waitToPeak > HesitationWait) fails++;
                else if (r.waitToPeak > MaxWaitToPeak) fails++;
                if (r.capture < GoodCapture) fails++;

                if (fails >= nearFails) continue;
                nearFails = fails;
                near = r;
                nearHas = true;
            }

            sb.AppendLine();
            if (!nearHas)
            {
                sb.AppendLine("Nothing in this kit even moves a hand far enough to call a " +
                              "stroke (" + MinTravel.ToString("F2") + "m), so there is no " +
                              "near miss to name either.");
            }
            else
            {
                sb.AppendLine("Closest (" + nearFails + " of 4 criteria failed): " +
                              near.name + " — " + near.length.ToString("F2") +
                              "s clip, hand " + near.travel.ToString("F2") + "m, " +
                              "feet " + near.legTravel.ToString("F2") + "m, " +
                              "strike " +
                              (float.IsInfinity(near.waitToPeak) ? "outside its window" :
                               near.waitToPeak.ToString("F2") + "s after the click") +
                              ", a " + near.windowLen.ToString("F2") + "s window holds " +
                              (near.capture * 100f).ToString("F0") + "% of it.");
                if (!string.IsNullOrEmpty(near.why)) sb.AppendLine("  " + near.why);

                // Worth saying explicitly, because it is the one lever left
                // before sourcing new art: speeding a clip up is free and loses
                // nothing, where trimming throws motion away.
                if (near.neededSpeed > 1.01f && !float.IsInfinity(near.neededSpeed))
                    sb.AppendLine("  Playing it at " + near.neededSpeed.ToString("F2") +
                                  "x would bring the strike inside " +
                                  MaxWaitToPeak.ToString("F2") +
                                  "s, and the Animator does that without discarding any of " +
                                  "the gesture. Worth trying before sourcing a clip.");
            }

            sb.AppendLine();
            sb.AppendLine("A free brush-stroke clip is needed — one authored as a single " +
                          "quick sweep rather than a full attack. This kit ships attacks, " +
                          "idles, walks, a run, a jump, a talk, a death and a hit-react; " +
                          "none of them is a stroke.");
            sb.AppendLine();
            sb.AppendLine("WHAT WORKS WITHOUT IT, so the game is playable now: the brush mark. " +
                          "It appears on the frame of the click, at the point that was clicked, " +
                          "and the world around it colours in. A player clicking a wall gets an " +
                          "immediate, unambiguous answer even with Ari's arm standing still.");
        }
        else
        {
            var best = winners[0];
            sb.AppendLine("Use " + best.name + ".");
            sb.AppendLine("  trim it to frames " + best.windowStart + " .. " + best.windowEnd +
                          "  (" + (best.windowStart / Fps).ToString("F3") + "s .. " +
                          ((best.windowEnd + 1) / Fps).ToString("F3") + "s, " + best.windowLen.ToString("F2") + "s)");
            sb.AppendLine("  set AriMover.swingSeconds to " + best.windowLen.ToString("F2"));
            sb.AppendLine("  it waits " + best.waitToPeak.ToString("F2") +
                          "s for the strike and holds " + (best.capture * 100f).ToString("F0") +
                          "% of the movement");
            sb.AppendLine("  hand travel " + best.travel.ToString("F2") + "m, foot travel " +
                          best.legTravel.ToString("F2") + "m");

            if (best.neededSpeed > 1.01f)
                sb.AppendLine("  to make the timing pass it would have to play at " +
                              best.neededSpeed.ToString("F2") + "x — the Animator can do that " +
                              "without losing any of the gesture, which trimming cannot");

            // Printed next to the verdict rather than buried in the table,
            // because a caller who acts on "use X" needs to know this before
            // pressing it, not after watching Ari march on the spot.
            if (!string.IsNullOrEmpty(best.warn))
            {
                sb.AppendLine();
                sb.AppendLine("  WARNING: " + best.warn);
                sb.AppendLine("  It still is the best of what is imported, and the arm does " +
                              "swing on time — but the legs will move through the stroke. " +
                              "Either accept it, or mask the legs with an Animator layer.");
            }

            if (best.name != "Ari_Attack")
                sb.AppendLine();
            if (best.name != "Ari_Attack")
                sb.AppendLine("  This means swapping the clip in the controller from Ari_Attack, " +
                              "which was the wrong shape: its movement is spread over 2.00s " +
                              "with the peak nearly a second in.");
        }
    }

    // ---------------------------------------------------------------------

    static Result Measure(AnimationClip clip)
    {
        // why starts empty rather than null so the print loop never has to
        // special-case a row that passed.
        var r = new Result { name = clip.name, length = clip.length, why = "", warn = "" };

        var rig = Rig(clip);
        if (rig == null)
        {
            // Spelled out rather than folded into "no hand bone", because the two
            // mean different things: this is a measuring failure, that one would
            // be a claim about the clip. Reporting a broken rig as a property of
            // the clip is how fourteen innocent animations got condemned at once.
            r.why = "no FBX to measure against (a .anim asset, or the file is gone), " +
                    "so this row says nothing about the clip";
            return r;
        }

        var bones = Bones(clip, rig);

        var hand = FindHand(bones);
        if (hand == null)
        {
            r.why = "no hand bone among this rig's " + bones.Count +
                    " transforms [" + boneNames(bones) + "] — a naming problem, not proof " +
                    "that the arm does not move";
            return r;
        }

        // The foot, watched for the same reason as the hand. A clip that passes every
        // arm test but walks the legs is Ari taking a step in mid-stroke, which
        // reads worse than no animation at all — the eye tracks the legs, not
        // the brush. Measured in the same sampling pass, because sampling twice
        // doubles the cost for no extra information.
        var foot = bones.Find(t => Norm(t.name) == "foot");

        int frames = Mathf.Max(4, Mathf.RoundToInt(clip.length * Fps));
        var speed = new float[frames];
        var prev = Vector3.zero;
        var prevFoot = Vector3.zero;
        float footTravel = 0f;
        bool havePrev = false;

        rig.gameObject.SetActive(true);

        for (int f = 0; f < frames; f++)
        {
            clip.SampleAnimation(rig.gameObject, f / Fps);
            var p = hand.position;
            speed[f] = havePrev ? Vector3.Distance(prev, p) : 0f;
            prev = p;

            if (foot != null)
            {
                var fp = foot.position;
                if (havePrev) footTravel += Vector3.Distance(prevFoot, fp);
                prevFoot = fp;
            }

            havePrev = true;
        }

        r.legTravel = footTravel;

        // Total travel, which is what the eye reads as "did the arm do anything",
        // as opposed to the end-to-end distance which a loop can fake.
        float travel = 0f;
        float total = 0f, peakSpeed = 0f;
        int peakFrame = 0;

        for (int f = 0; f < frames; f++)
        {
            travel += speed[f];
            total += speed[f];
            if (speed[f] > peakSpeed) { peakSpeed = speed[f]; peakFrame = f; }
        }

        r.travel = travel;

        if (total <= 0f)
        {
            r.why = "the hand does not move at all in this clip";
            return r;
        }

        const float StillShare = 0.08f;
        float still = peakSpeed * StillShare;

        int firstMoving = 0;
        for (int f = 0; f < frames; f++)
            if (speed[f] > still) { firstMoving = f; break; }

        r.peakTime = (peakFrame - firstMoving) / Fps;

        // The window is the clip's own length, capped, so a long clip is judged
        // on whether its movement concentrates into a press-length window at
        // all. A window the same length as a long clip would trivially capture
        // everything and every long clip would score identically, which tells
        // us nothing.
        int winFrames = Mathf.Min(frames, Mathf.Max(4, Mathf.RoundToInt(0.70f * Fps)));

        int bestStart = 0;
        float bestGot = -1f;

        for (int s = 0; s + winFrames <= frames; s++)
        {
            float got = 0f;
            for (int f = s; f < s + winFrames; f++) got += speed[f];
            if (got > bestGot) { bestGot = got; bestStart = s; }
        }

        int bestEnd = Mathf.Min(frames - 1, bestStart + winFrames - 1);
        r.windowStart = bestStart;
        r.windowEnd = bestEnd;
        r.windowLen = (bestEnd - bestStart + 1) / Fps;
        r.capture = bestGot / total;

        // Where the peak sits inside the window decides whether the click feels
        // answered. A peak before the window is not counted as a win: it means
        // the search put the window after the action, which is the one outcome
        // that would make the wait look good while the player saw nothing.
        int peakOffset = peakFrame - bestStart;
        r.waitToPeak = peakOffset < 0 ? float.PositiveInfinity : peakOffset / Fps;

        // --- judge ----------------------------------------------------------

        var reasons = new List<string>();

        if (r.travel < MinTravel)
            reasons.Add("hand travel " + r.travel.ToString("F2") + "m is under " +
                        MinTravel.ToString("F2") + "m, so it will not read as a stroke");

        if (float.IsInfinity(r.waitToPeak))
            reasons.Add("the peak falls before its own window, so the search found " +
                        "nothing to hold on to");
        else if (r.waitToPeak > HesitationWait)
            reasons.Add("wait of " + r.waitToPeak.ToString("F2") + "s before the strike reads " +
                        "as the arm hesitating");
        else if (r.waitToPeak > MaxWaitToPeak)
            reasons.Add("wait of " + r.waitToPeak.ToString("F2") + "s is over the " +
                        MaxWaitToPeak.ToString("F2") + "s that still feels like frame-one response");

        if (r.capture < GoodCapture)
            reasons.Add("a " + r.windowLen.ToString("F2") + "s window holds only " +
                        (r.capture * 100f).ToString("F0") + "% of the movement, so it is " +
                        "cutting the gesture rather than trimming padding");

        // The feet are a hard criterion, not a note in the margin.
        //
        // This started as a warning and that was wrong. Ari_Walk was the only
        // clip the survey passed, and its feet travelled 0.85m against the hand's
        // 0.47m — a walk cycle, whose hand motion is incidental arm swing. A
        // warning is something you read and route around; a PASS is something you
        // act on, and acting on this one would have Ari marching on the spot while
        // supposedly painting. Feet must be still, or the clip is a locomotion
        // cycle that happens to move an arm.
        //
        // The threshold is a tenth of the hand's travel, not a fraction of it.
        // Scaling by the hand would have excused a walk cycle whose stride is
        // merely proportional to its swing; the question is whether the feet move
        // at all in something that is meant to be a brush.
        const float MaxLegShare = 0.10f;
        if (r.legTravel > r.travel * MaxLegShare)
        {
            reasons.Add("the feet travel " + r.legTravel.ToString("F2") + "m against the " +
                        "hand's " + r.travel.ToString("F2") + "m (" +
                        (r.legTravel / Mathf.Max(0.0001f, r.travel) * 100f).ToString("F0") +
                        "%), so this is a locomotion cycle with incidental arm swing, " +
                        "not a stroke");
        }

        // What playback speed would make the timing pass, if the clip is worth
        // speeding up at all. Reported for every arm-clean clip because it is the
        // lever that actually exists: the Animator can play a clip faster without
        // discarding any of it, which trimming does. The question the number
        // answers is "how fast would this have to move to feel immediate", and
        // the useful part is that anything past about 2.5x stops reading as a
        // brush and starts reading as a glitch.
        r.neededSpeed = r.waitToPeak > MaxWaitToPeak
            ? r.waitToPeak / MaxWaitToPeak
            : 1f;

        // A warning, not a failure, and deliberately not gated on r.usable.
        // Gating it on usable would have been dead code: usable is assigned two
        // lines below and is still the struct's default here, so the check could
        // never fire. Speed stays informational — it is a real lever, and the
        // point is to show the caller what it would cost, not to fail the clip
        // for a fix that might work.
        if (r.neededSpeed > 3f)
            r.warn = "it would need to play " + r.neededSpeed.ToString("F1") +
                     "x to answer the click on time; past about 2.5x that reads as a " +
                     "glitch rather than a brush";

        r.usable = reasons.Count == 0;
        if (reasons.Count > 0) r.why = string.Join("; ", reasons.ToArray());

        return r;
    }

    // ---------------------------------------------------------------------

    /// <summary>
    /// The skeleton a particular clip was authored against.
    ///
    /// Keyed by the clip's own asset, not shared. Every clip in this kit is a
    /// separate FBX and they do not necessarily carry the same bone names, so a
    /// rig shared across the survey measures a clip against a skeleton that is
    /// not its own. When they do differ, the hand lookup fails and the clip is
    /// reported as having no hands — a false verdict produced entirely by the
    /// measuring rig, and indistinguishable from a real one.
    ///
    /// The file the clip came from is asked for rather than guessed, because a
    /// hard-coded path is a rig that silently does not match on the next import.
    /// </summary>
    static Transform Rig(AnimationClip clip)
    {
        if (_rigs.TryGetValue(clip, out var cached) && cached != null) return cached;

        string path = AssetDatabase.GetAssetPath(clip);
        if (string.IsNullOrEmpty(path)) return null;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return null;   // a .anim asset, not an FBX: no rig of its own

        var instance = Object.Instantiate(prefab);
        instance.name = "SwingSurvey_Rig_" + clip.name;
        instance.hideFlags = HideFlags.HideAndDontSave;

        var anim = instance.GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            anim.runtimeAnimatorController = null;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        instance.SetActive(false);
        _rigs[clip] = instance.transform;
        return instance.transform;
    }

    static readonly Dictionary<AnimationClip, Transform> _rigs = new Dictionary<AnimationClip, Transform>();

    /// <summary>
    /// The rig's transforms, re-read whenever the clip changes.
    ///
    /// The cache was the bug that made this whole survey lie. It kept one bone
    /// list and re-served it to the next clip, so every clip after the first was
    /// measured against a lookup that did not belong to it — and a hand that
    /// could not be found in a stale list produced "no hand bone in this clip",
    /// which is indistinguishable from a clip that genuinely has no hands.
    /// Fourteen clips, fourteen identical verdicts, and the tool's own reason
    /// field was the tell: the first clip had a hand, measured moments earlier
    /// by a probe that resolved bones once and correctly.
    ///
    /// So: no cross-clip caching. The list is rebuilt per clip, from that
    /// clip's own sample, and a clip that genuinely has no hand now says so
    /// alone.
    /// </summary>
    static List<Transform> Bones(AnimationClip clip, Transform rig)
    {
        rig.gameObject.SetActive(true);
        rig.position = Vector3.zero;
        rig.rotation = Quaternion.identity;
        clip.SampleAnimation(rig.gameObject, 0f);

        return new List<Transform>(rig.GetComponentsInChildren<Transform>(true));
    }

    /// <summary>
    /// The hand to watch, right preferred.
    ///
    /// This is where the survey first lied, and it is worth being precise about
    /// why. The lookup normalised the bone name it had and compared it to the
    /// raw literal "righthand". But Norm deliberately erases the side: it strips
    /// a trailing l or r and then a leading "right" or "left", so Norm("RightHand")
    /// is "hand" and can never equal "righthand". Fourteen clips were measured and
    /// every one of them reported no hand, and the reason field blamed the
    /// clips. The comparison has to normalise both sides, which is what the
    /// earlier single-clip probe did and why it was right.
    /// </summary>
    static Transform FindHand(List<Transform> bones)
    {
        Transform right = null, any = null;

        foreach (var t in bones)
        {
            if (Norm(t.name) != HandStem) continue;

            if (any == null) any = t;

            var lower = t.name.ToLowerInvariant();
            if (right == null && (lower.Contains("right") || lower.Contains("_r") ||
                                  lower.EndsWith("r")))
                right = t;
        }

        return right ?? any;
    }

    /// <summary>
    /// What a hand bone looks like once the side is taken off: "RightHand",
    /// "hand_r" and "Hand_R" all reduce to this.
    /// </summary>
    const string HandStem = "hand";

    /// <summary>The rig's bone names, so a failed lookup names the bone it wanted.</summary>
    static string boneNames(List<Transform> bones)
    {
        var names = new List<string>();
        foreach (var t in bones) names.Add(t.name);
        names.Sort();
        return string.Join(" ", names.ToArray());
    }

    /// <summary>
    /// Bone name reduced to a comparable stem: no namespace, no separators, no
    /// side marker. "mixamorig:RightHand", "RightHand" and "hand_r" all become
    /// "hand".
    ///
    /// The namespace prefix is the part that is easy to miss. This rig ships two
    /// flavours — some FBX name bones bare, some prefix every one with
    /// "mixamorig:" — and a stem rule that only strips the side finds hands in
    /// the bare rigs and reports "no hand bone" in the prefixed ones. That is
    /// exactly what happened: four clips listed "mixamorig:RightHand" right in
    /// the report's own bone list and were still condemned for having no hands.
    /// A lookup that rejects a name it has just printed is not measuring the
    /// clip, it is measuring its own string handling.
    /// </summary>
    static string Norm(string bone)
    {
        var s = bone;

        // Namespaces are "mixamorig:" or "mixamorig|Hips" depending on importer.
        int cut = s.LastIndexOfAny(new[] { ':', '|', '/' });
        if (cut >= 0 && cut < s.Length - 1) s = s.Substring(cut + 1);

        s = s.Replace("_", "").Replace(" ", "").ToLowerInvariant();

        if (s.EndsWith("l")) s = s.Substring(0, s.Length - 1);
        else if (s.EndsWith("r")) s = s.Substring(0, s.Length - 1);
        if (s.StartsWith("right")) s = s.Substring(5);
        else if (s.StartsWith("left")) s = s.Substring(4);
        return s;
    }
}
