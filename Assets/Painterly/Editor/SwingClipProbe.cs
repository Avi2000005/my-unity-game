using System.Collections.Generic;
using System.IO;
using System.Text;
using Echoes.Painterly;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Measures Ari's brush-swing clip so the animator and the gameplay agree.
///
/// WHY THIS EXISTS
///
/// The swing clip imported at 2.40 seconds. AriMover holds the Swinging flag for
/// swingSeconds, currently 0.42, and the brush has a 0.6s cooldown. Three
/// numbers that all have to describe the same gesture, and nothing was making
/// them agree: a player who clicks twice in a row gets a two-and-a-half second
/// animation for a click that the game thinks took four tenths.
///
/// The measure that decides the trim is where the arm actually comes back down,
/// not where the clip's last keyframe is. An attack clip is authored to be
/// loopable and often ends on a held pose well after the motion is over, so the
/// clip length overstates the stroke by however long the author left it hanging.
/// Trimming to the frame the motion finishes is what makes the animation read as
/// responsive; trimming to the last keyframe is what makes it read as laggy.
///
/// Also measured: the hand bone's arc, so the peak is a real number and not a
/// guess, and the clip's frame count, so a trim lands on a whole frame.
/// </summary>
public static class SwingClipProbe
{
    const string Report = "Temp/swing_clip.txt";
    const string ClipPath = "Assets/Art/L1/Ari_Attack.fbx";
    const string SwingParam = "Swing";
    const string SwingingParam = "Swinging";

    /// <summary>
    /// Frames per second assumed for "where the motion stops". Taken from the
    /// sample rate rather than hard-coded, because a clip imported at a
    /// different rate would otherwise be trimmed at the wrong wall-clock time.
    /// </summary>
    const float AssumeFps = 30f;

    public static void Run()
    {
        var sb = new StringBuilder();
        try { Body(sb); }
        catch (System.Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] swing clip\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            sb.AppendLine("STOPPED: the editor is in play mode. Nothing is changed.");
            return;
        }

        sb.AppendLine("ARI'S BRUSH SWING");
        sb.AppendLine();

        // --- the clips available --------------------------------------------

        var all = new List<AnimationClip>();
        foreach (var dir in new[] { "Assets/Art/Ari/Models", "Assets/Art/L1" })
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { dir }))
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                {
                    var c = o as AnimationClip;
                    if (c == null || c.legacy || c.name.StartsWith("__")) continue;
                    all.Add(c);
                }
            }
        }

        sb.AppendLine("clips found: " + all.Count);
        foreach (var c in all)
            sb.AppendLine("  " + c.name.PadRight(22) + c.length.ToString("F2") + "s" +
                          (c.frameRate > 0f ? "  @ " + c.frameRate.ToString("F0") + "fps" : ""));
        sb.AppendLine();

        var clip = all.Find(c => c.name == "Ari_Attack");
        if (clip == null)
        {
            sb.AppendLine("Ari_Attack is not present. Nothing measured.");
            return;
        }

        // --- bone arc -------------------------------------------------------

        // The hand is the thing the eye follows. If the arm barely moves, the
        // stroke will read as a shrug no matter how the graph is wired, and the
        // fix is a different clip rather than a different graph.
        string[] watch = { "RightHand", "LeftHand", "RightArm", "LeftArm",
                           "RightShoulder", "LeftShoulder", "Hips" };

        sb.AppendLine("--- bone arcs over the whole clip ---");
        float peakHand = 0f;
        var startPos = new Vector3[watch.Length];
        var minPos = new Vector3[watch.Length];
        var maxPos = new Vector3[watch.Length];

        for (int i = 0; i < watch.Length; i++)
        {
            startPos[i] = minPos[i] = maxPos[i] = Vector3.zero;
        }

        float duration = clip.length;
        const int samples = 120;

        // Resolve the bones once, up front. Resolving them per sample means 121
        // name lookups per bone, and a lookup that fails intermittently reads as
        // a bone that barely moves — the same false "this clip has no arm"
        // verdict as an empty rig.
        var rig = Rig();
        if (rig == null)
        {
            sb.AppendLine("The imported model at " + ClipPath + " did not load, so no " +
                          "bone can be measured. Nothing below is a statement about the clip.");
            return;
        }

        var resolved = new Transform[watch.Length];
        for (int i = 0; i < watch.Length; i++)
        {
            resolved[i] = FindBone(clip, watch[i]);
            if (resolved[i] == null)
                sb.AppendLine("  " + watch[i].PadRight(16) + "NOT IN THIS CLIP  <- will be skipped");
        }

        sb.AppendLine("  rig loaded with " + Bones(clip).Count + " transform(s)");
        sb.AppendLine();

        rig.gameObject.SetActive(true);

        for (int s = 0; s <= samples; s++)
        {
            float t = duration * s / (float)samples;
            clip.SampleAnimation(rig.gameObject, t);

            for (int i = 0; i < watch.Length; i++)
            {
                if (resolved[i] == null) continue;

                var p = resolved[i].position;
                if (s == 0) startPos[i] = minPos[i] = maxPos[i] = p;
                minPos[i] = Vector3.Min(minPos[i], p);
                maxPos[i] = Vector3.Max(maxPos[i], p);
            }
        }

        for (int i = 0; i < watch.Length; i++)
        {
            var tr = resolved[i];
            if (tr == null) continue;

            Vector3 span = maxPos[i] - minPos[i];
            float travel = Vector3.Distance(startPos[i], maxPos[i]);

            sb.AppendLine("  " + watch[i].PadRight(16) +
                          " travel " + travel.ToString("F3") + "m" +
                          "   span " + span.x.ToString("F2") + " x " +
                          span.y.ToString("F2") + " y " + span.z.ToString("F2") + " z");

            if (watch[i].EndsWith("Hand")) peakHand = Mathf.Max(peakHand, travel);
        }

        sb.AppendLine();
        if (peakHand < 0.15f)
            sb.AppendLine("*** the hand barely moves (" + peakHand.ToString("F3") +
                          "m). This clip will not read as a brush swing whatever the graph " +
                          "does, and the fix is a different clip, not a rewire. ***");
        else
            sb.AppendLine("hand travel " + peakHand.ToString("F3") + "m: enough to read as a stroke.");
        sb.AppendLine();

        // --- where the motion actually stops ---------------------------------

        // Per-frame hand speed, sampled per frame rather than per second, so the
        // answer is a frame index and can be trimmed to.
        int frames = Mathf.Max(1, Mathf.RoundToInt(duration * AssumeFps));
        var handSpeed = new float[frames];
        var prev = Vector3.zero;
        bool havePrev = false;

        var hand = resolved[0] ?? resolved[1];   // RightHand, then LeftHand
        if (hand == null)
        {
            sb.AppendLine("No hand bone in this clip, so per-frame speed cannot be measured. " +
                          "The bone arcs above still stand; this section does not.");
            return;
        }

        for (int f = 0; f < frames; f++)
        {
            float t = f / (float)AssumeFps;
            clip.SampleAnimation(rig.gameObject, t);

            var p = hand.position;
            handSpeed[f] = havePrev ? Vector3.Distance(prev, p) : 0f;
            prev = p;
            havePrev = true;
        }

        float peakSpeed = 0f;
        int peakFrame = 0;
        for (int f = 0; f < frames; f++)
            if (handSpeed[f] > peakSpeed) { peakSpeed = handSpeed[f]; peakFrame = f; }

        // "Stopped" is a share of the peak, not zero. A clip that ends on a held
        // pose has real zero speed at the tail, but a clip that eases out has a
        // long approach to zero and cutting at the first near-zero frame would
        // trim the follow-through off the stroke.
        const float StillShare = 0.08f;
        float still = peakSpeed * StillShare;

        int lastMoving = peakFrame;
        for (int f = peakFrame; f < frames; f++)
            if (handSpeed[f] > still) lastMoving = f;

        // Where it was last clearly moving, plus a short tail so the arm is
        // allowed to finish settling rather than being cut mid-motion.
        int trimFrame = Mathf.Min(frames - 1, lastMoving + Mathf.RoundToInt(AssumeFps * 0.10f));
        float trimTime = trimFrame / (float)AssumeFps;

        sb.AppendLine("--- per-frame hand speed ---");
        sb.AppendLine("  clip length          " + duration.ToString("F2") + "s  (" + frames + " frames at " + AssumeFps + "fps)");
        sb.AppendLine("  peak speed           " + peakSpeed.ToString("F4") + "m/frame  at frame " + peakFrame +
                      "  (" + (peakFrame / AssumeFps).ToString("F2") + "s)");
        sb.AppendLine("  still threshold      " + still.ToString("F5") + "m/frame  (" + (StillShare * 100f).ToString("F0") + "% of peak)");
        sb.AppendLine("  last frame moving    frame " + lastMoving + "  (" + (lastMoving / AssumeFps).ToString("F2") + "s)");
        sb.AppendLine("  lastMoving + 0.10s   frame " + trimFrame + "  (" + trimTime.ToString("F2") + "s)");
        sb.AppendLine();

        float trimShare = trimTime / duration;
        sb.AppendLine("  the clip is " + duration.ToString("F2") + "s but the motion is done at " +
                      trimTime.ToString("F2") + "s, so " +
                      ((1f - trimShare) * 100f).ToString("F0") + "% of it is a held pose.");
        sb.AppendLine();

        // --- the speed curve, so a window is chosen and not guessed ---------

        sb.AppendLine("--- hand speed per frame ---");
        for (int f = 0; f < frames; f += 2)
        {
            float bar = handSpeed[f] / (peakSpeed > 0f ? peakSpeed : 1f);
            int bars = Mathf.RoundToInt(bar * 40f);

            sb.AppendLine("  " + (f / AssumeFps).ToString("F2").PadLeft(5) + "s  " +
                          handSpeed[f].ToString("F4") + "  " + new string('#', bars) +
                          (bars == 0 ? "." : ""));
        }
        sb.AppendLine();

        // --- where the stroke actually is ------------------------------------

        // A two-second attack is the wrong shape for a button press, and
        // trimming its tail does not help: the peak is a second in, so a tail
        // trim still leaves the arm rising for most of a second after the click.
        // What a brush wants is the strike itself, so the window is built around
        // the peak rather than around the clip.
        int firstMoving = 0;
        for (int f = 0; f < frames; f++)
            if (handSpeed[f] > still) { firstMoving = f; break; }

        float total = 0f;
        for (int f = 0; f < frames; f++) total += handSpeed[f];

        // -------------------------------------------------------------------
        // The window is searched for, not picked.
        //
        // The first version of this padded the peak by a guessed 0.20s either
        // side and caught 45% of the hand's movement: the clip has a wind-up, a
        // strike, and then a separate settle, and a window centred on the peak
        // keeps only the strike. Padding constants cannot tell the difference
        // between the strike and the wind-up, because both are "near the peak"
        // to a number.
        //
        // So every possible start is measured at the same length and the one
        // that captures the most movement wins. The capture share is reported
        // either way, because the honest answer is sometimes "no window this
        // short holds the whole gesture" and the fix is a longer window or a
        // longer cooldown rather than a different offset.
        // -------------------------------------------------------------------

        float cooldown = ReadBrushCooldown();

        // Aim for a window the cooldown can absorb, so consecutive strokes never
        // overlap. A fraction over is allowed: a stroke that is a hair longer
        // than the gap between strokes reads as rhythm, whereas a stroke far
        // longer than it reads as a stutter.
        float targetLen = cooldown > 0f ? Mathf.Min(cooldown * 1.15f, 0.85f) : 0.7f;
        int targetFrames = Mathf.Max(4, Mathf.RoundToInt(targetLen * AssumeFps));

        int winStart = 0, winEnd = 0;
        float bestCaptured = -1f;

        for (int s = 0; s + targetFrames <= frames; s++)
        {
            float got = 0f;
            for (int f = s; f < s + targetFrames; f++) got += handSpeed[f];

            if (got > bestCaptured)
            {
                bestCaptured = got;
                winStart = s;
                winEnd = s + targetFrames - 1;
            }
        }

        float winLen = (winEnd - winStart + 1) / (float)AssumeFps;
        float capturedShare = total > 0f ? bestCaptured / total : 0f;

        // Where inside the window the peak lands. A peak near the start is a
        // click that lands instantly; a peak in the last third is an arm that
        // hesitates first, which is the thing a brush press must not do.
        int peakOffset = peakFrame - winStart;
        float waitToPeak = peakOffset / AssumeFps;

        sb.AppendLine("--- the stroke window, searched ---");
        sb.AppendLine("  target length         " + targetLen.ToString("F2") + "s  (" +
                      targetFrames + " frames, from the " + cooldown.ToString("F2") +
                      "s cooldown so strokes do not overlap)");
        sb.AppendLine("  best window           frame " + winStart + " .. " + winEnd +
                      "   (" + (winStart / AssumeFps).ToString("F3") + "s .. " +
                      ((winEnd + 1) / AssumeFps).ToString("F3") + "s,  " + winLen.ToString("F2") + "s)");
        sb.AppendLine("  captures              " + (capturedShare * 100f).ToString("F0") +
                      "% of all hand movement in the clip");
        sb.AppendLine("  motion starts at      frame " + firstMoving + "  (" + (firstMoving / AssumeFps).ToString("F2") + "s)");
        sb.AppendLine("  peak inside window    frame " + peakFrame + ", " + peakOffset + " frames in  (" +
                      waitToPeak.ToString("F2") + "s after the click)");
        sb.AppendLine("  movement at window's own start  " +
                      handSpeed[winStart].ToString("F4") + "m/frame, so it does not start frozen");

        if (capturedShare < 0.6f)
            sb.AppendLine("*** even the best " + targetLen.ToString("F2") +
                          "s window holds only " + (capturedShare * 100f).ToString("F0") +
                          "% of the movement. A window this short cannot be a clean trim of " +
                          "this clip: the motion is spread over " +
                          (lastMoving / AssumeFps).ToString("F2") +
                          "s. Either lengthen the window, lengthen the cooldown to match, or " +
                          "accept that this attack clip is the wrong clip for a brush. ***");

        if (waitToPeak > 0.18f)
            sb.AppendLine("*** the peak is " + waitToPeak.ToString("F2") +
                          "s after the click. Under 0.18s the input feels like it landed on " +
                          "frame one; over that the arm visibly hesitates, and a brush press " +
                          "cannot afford to. ***");

        sb.AppendLine();
        sb.AppendLine("  Total hand movement in the clip is spread over " +
                      (lastMoving / AssumeFps).ToString("F2") + "s. An attack clip is a long " +
                      "gesture by design; a button press wants the short middle of one, " +
                      "and the gap between those two is the whole problem here.");
        sb.AppendLine();

        // --- does it agree with the game? ------------------------------------

        float swingSeconds = ReadAriSwingSeconds();

        sb.AppendLine("--- does the clip agree with the gameplay? ---");
        sb.AppendLine("  full clip length      " + duration.ToString("F2") + "s");
        sb.AppendLine("  chosen window         " + winLen.ToString("F2") + "s");
        sb.AppendLine("  AriMover.swingSeconds " + swingSeconds.ToString("F2") + "s  (currently)");
        sb.AppendLine("  brush cooldown        " + cooldown.ToString("F2") + "s");

        float mismatch = Mathf.Abs(winLen - swingSeconds);
        if (mismatch < 0.06f)
            sb.AppendLine("  the flag already matches the window, so the return blend starts " +
                          "as the arm finishes.");
        else
            sb.AppendLine("*** swingSeconds is " + mismatch.ToString("F2") +
                          "s away from the window. If it is shorter, the graph blends out " +
                          "with the arm still travelling; if it is longer, the arm holds " +
                          "after the stroke is over. Set swingSeconds to " +
                          winLen.ToString("F2") + "s. ***");

        // The window also has to fit inside the cooldown, or a second click
        // lands while the first stroke is still playing and the two overwrite.
        if (cooldown > 0f && winLen > cooldown)
            sb.AppendLine("  note: the window (" + winLen.ToString("F2") + "s) is longer than the " +
                          "cooldown (" + cooldown.ToString("F2") + "s), so mashing the button " +
                          "restarts the stroke before it finishes. Not fatal, but the arm " +
                          "will stutter; the cooldown or the window wants to be the shorter one.");
        else if (cooldown > 0f)
            sb.AppendLine("  the window (" + winLen.ToString("F2") + "s) fits inside the cooldown " +
                          "(" + cooldown.ToString("F2") + "s), so consecutive strokes do not overlap.");

        sb.AppendLine();

        // --- what to actually do --------------------------------------------

        sb.AppendLine("--- what to change ---");
        sb.AppendLine("  Trim " + ClipPath + " to frames " + winStart + " .. " + winEnd + ", i.e. " +
                      winStart / AssumeFps + "s .. " + (winEnd + 1) / AssumeFps + "s.  (" +
                      winLen.ToString("F3") + "s, " + (winEnd - winStart + 1) + " frames)");
        sb.AppendLine("  Set AriMover.swingSeconds to " + winLen.ToString("F2") + "s.");
        sb.AppendLine("  The controller needs no change: the Swinging gate already ends the " +
                      "state, and the 0.42 backstop sits at " +
                      (0.42f * winLen).ToString("F2") + "s of the trimmed clip, safely past the end.");
    }

    // ---------------------------------------------------------------------

    /// <summary>
    /// The real skeleton, so a bone's position can be asked for.
    ///
    /// An empty GameObject will not do. SampleAnimation writes into a hierarchy
    /// that already exists, and a humanoid clip additionally needs the Avatar
    /// that the imported model carries — so sampling onto a blank object either
    /// does nothing or creates a partial hierarchy, and both failures look the
    /// same from here: every bone reported as "NOT IN THIS CLIP". The honest
    /// reading would have been that the swing has no arm in it, which is not
    /// true, so the model's own instance is used instead.
    /// </summary>
    static Transform Rig()
    {
        if (_rig != null) return _rig;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ClipPath);
        if (prefab == null) return null;

        var instance = Object.Instantiate(prefab);
        instance.name = "SwingClipProbe_Rig";
        instance.hideFlags = HideFlags.HideAndDontSave;

        var anim = instance.GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            // No controller, or it would overwrite the sample we are asking for.
            anim.runtimeAnimatorController = null;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        instance.SetActive(false);
        _rig = instance.transform;
        return _rig;
    }

    /// <summary>
    /// Every transform in the imported rig, sampled at the clip's start.
    ///
    /// The whole list is taken once and cached. Looking a bone up by name on
    /// every one of 121 samples is 121 traversals per bone, and the bone list
    /// does not change between samples — only the positions do.
    /// </summary>
    static List<Transform> Bones(AnimationClip clip)
    {
        var rig = Rig();
        if (rig == null) return null;

        if (_bones != null && _bonesFor == clip) return _bones;

        rig.gameObject.SetActive(true);
        rig.position = Vector3.zero;
        rig.rotation = Quaternion.identity;

        clip.SampleAnimation(rig.gameObject, 0f);

        _bones = new List<Transform>(rig.GetComponentsInChildren<Transform>(true));
        _bonesFor = clip;
        return _bones;
    }

    static Transform FindBone(AnimationClip clip, string bone)
    {
        var bones = Bones(clip);
        if (bones == null) return null;

        foreach (var t in bones)
            if (t.name == bone) return t;

        // Mixamo names the same bones both ways across exports: RightHand and
        // hand_r are the same joint, and only one of them appears in any given
        // file.
        foreach (var t in bones)
            if (Normalise(t.name) == Normalise(bone)) return t;

        return null;
    }

    /// <summary>Bone name with separators, case and side-suffix differences erased.</summary>
    static string Normalise(string bone)
    {
        var s = bone.Replace("_", "").Replace(" ", "").ToLowerInvariant();

        // Trailing l/r is the side marker, so "rightarm" and "arm" are the same
        // bone as far as a lookup is concerned.
        if (s.EndsWith("l")) s = s.Substring(0, s.Length - 1);
        else if (s.EndsWith("r")) s = s.Substring(0, s.Length - 1);

        if (s.StartsWith("right")) s = s.Substring(5);
        else if (s.StartsWith("left")) s = s.Substring(4);

        return s;
    }

    static Transform _rig;
    static List<Transform> _bones;
    static AnimationClip _bonesFor;

    static float ReadAriSwingSeconds()
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == "Ari")
            {
                var m = t.GetComponentInChildren<AriMover>(true);
                if (m != null)
                {
                    var f = typeof(AriMover).GetField("swingSeconds",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic);
                    if (f != null) return (float)f.GetValue(m);
                }
            }

        return -1f;
    }

    static float ReadBrushCooldown()
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            var b = t.GetComponentInChildren<BrushPainter>(true);
            if (b == null) continue;

            var f = typeof(BrushPainter).GetField("cooldown",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            if (f != null) return (float)f.GetValue(b);
        }

        return -1f;
    }
}
