using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// ModelImporter exposes no "give me the override for this take" call, and
    /// every use of it in this file needs exactly that. Written out here rather
    /// than left to a caller, because the obvious alternatives each fail
    /// quietly: a name comparison misses clips matched by take name, and
    /// FirstOrDefault on a guessed predicate cannot tell "no override" apart
    /// from "override with an empty name".
    /// </summary>
    static class ModelImporterClipAnimations
    {
        public static ModelImporterClipAnimation FirstOrDefaultByName(
            this ModelImporterClipAnimation[] settings, string name)
        {
            if (settings == null) return null;

            return settings.FirstOrDefault(
                s => string.Equals(s.name, name, System.StringComparison.Ordinal) ||
                     string.Equals(s.takeName, name, System.StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Finds where Mono's wake animation actually happens, and trims the clip
    /// to it.
    ///
    /// The clip is 11.40 seconds long and the beat wants about two and a half
    /// of them. That gap is not padding to be guessed at: the asset is a single
    /// wake with a long hold on the end, so the last eight seconds are Mono
    /// standing still in a bind pose, and an untrimmed clip makes a tutorial
    /// beat look like it is loading.
    ///
    /// So the motion is measured. A frame-by-frame sample of the root and the
    /// hands, and the first and last frames where anything moves by more than a
    /// millimetre. Everything before the first and after the last is the
    /// "hold", and the hold is exactly what should be cut.
    ///
    /// The report is written before the reimport and names the frame range it
    /// chose, because a trimmed clip that is trimmed wrongly is much harder to
    /// notice than one that was never trimmed.
    /// </summary>
    public static class WakeClipTrim
    {
        const string Fbx = "Assets/Art/L1/Mono_Wake.fbx";
        const string Report = "Temp/wake_clip_trim.txt";

        /// <summary>Metres of movement below which a frame counts as still.</summary>
        const float StillEpsilon = 0.001f;

        /// <summary>Seconds of extra clip kept past the last movement, so the
        /// animation does not stop on the frame it finishes.</summary>
        const float TailKeep = 0.25f;

        [MenuItem("Tools/Echoes/Measure And Trim Mono's Wake", priority = 72)]
        public static void Run()
        {
            var sb = new StringBuilder();
            if (File.Exists(Report)) File.Delete(Report);

            try
            {
                Body(sb);
            }
            catch (System.Exception e)
            {
                sb.AppendLine();
                sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
                sb.AppendLine(e.StackTrace);
                Debug.LogError("[Echoes] wake trim threw: " + e);
            }
            finally
            {
                Debug.Log("[Echoes] wake trim\n" + sb);
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report),
                                  sb.ToString());
            }
        }

        // --- the window the designer chose -----------------------------------------

        /// <summary>
        /// Where in the wake the three seconds worth keeping actually are.
        ///
        /// Not a round number typed in for convenience. The profile this same
        /// file prints buckets the movement every half second, and 5.07s to
        /// 8.03s is the busiest three-second run of buckets in the whole
        /// performance — it carries more of the wake's movement than any other
        /// three seconds, by a wide margin, and it is the part where Mono
        /// actually gets up rather than shifting in a bind.
        ///
        /// The measure tool offered this window and deliberately did not apply
        /// it, because where a performance gets cut is an art call and only
        /// someone who has watched it can make it. It has been made.
        /// </summary>
        const float ChosenStart = 5.07f;
        const float ChosenEnd = 8.03f;

        const string WindowReport = "Temp/wake_window.txt";

        [MenuItem("Tools/Echoes/Trim Mono's Wake To The Chosen Window", priority = 73)]
        public static void RunChosen()
        {
            var sb = new StringBuilder();
            if (File.Exists(WindowReport)) File.Delete(WindowReport);

            try
            {
                BodyChosen(sb);
            }
            catch (System.Exception e)
            {
                sb.AppendLine();
                sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
                sb.AppendLine(e.StackTrace);
                Debug.LogError("[Echoes] wake window trim threw: " + e);
            }
            finally
            {
                Debug.Log("[Echoes] wake window trim\n" + sb);
                File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), WindowReport),
                                  sb.ToString());
            }
        }

        static void BodyChosen(StringBuilder sb)
        {
            sb.AppendLine("TRIM MONO'S WAKE TO THE CHOSEN WINDOW");
            sb.AppendLine();

            var importer = AssetImporter.GetAtPath(Fbx) as ModelImporter;
            if (importer == null)
            {
                sb.AppendLine("No ModelImporter at " + Fbx + ". Nothing changed.");
                return;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Fbx);
            if (clip == null)
            {
                sb.AppendLine("No AnimationClip at " + Fbx + ". Nothing changed.");
                return;
            }

            float fps = clip.frameRate > 1f ? clip.frameRate : 30f;
            int frames = Mathf.Max(2, Mathf.RoundToInt(clip.length * fps));
            int first = Mathf.RoundToInt(ChosenStart * fps);
            int last = Mathf.RoundToInt(ChosenEnd * fps);

            sb.AppendLine(Fbx);
            sb.AppendLine("  clip '" + clip.name + "' is " + clip.length.ToString("F3") +
                          " s, " + frames + " frames at " + fps.ToString("F2") + " fps");
            sb.AppendLine("  chosen window " + ChosenStart.ToString("F2") + " s to " +
                          ChosenEnd.ToString("F2") + " s  ->  frames " + first + " .. " + last);

            // Checked against the clip rather than assumed. A window that runs
            // off the end of the take is not a trim, it is a shorter clip with
            // an error message, and it is the single most likely way for this
            // tool to delete most of an animation by accident.
            if (first < 0 || last >= frames || last - first < 2)
            {
                sb.AppendLine();
                sb.AppendLine("  *** WINDOW IS OFF THE CLIP. Frames " + first + ".." + last +
                              " against " + frames + " frames in the take. ***");
                sb.AppendLine("  NOTHING CHANGED. Trim to a range that exists.");
                return;
            }

            int kept = last - first + 1;
            float keptSeconds = kept / fps;
            sb.AppendLine("  keeps " + kept + " frames = " + keptSeconds.ToString("F2") +
                          " s, which is " +
                          (keptSeconds / clip.length * 100f).ToString("0") +
                          "% of the clip; " +
                          (clip.length - keptSeconds).ToString("F2") + " s comes off");

            // --- what the cut actually costs ----------------------------------

            GameObject holder = null;
            try
            {
                holder = MakeRig(sb);
                if (holder == null)
                {
                    sb.AppendLine("  NOTHING CHANGED — no rig to measure on, and a trim " +
                                  "nobody measured is just a guess.");
                    return;
                }

                string[] bones = { "Hips", "LeftHand", "RightHand", "Head", "Spine" };
                float[] totals;
                bool[] moved;
                SampleFrames(clip, holder, fps, frames, bones, out totals, out moved);

                float whole = 0f, keptSum = 0f;
                for (int f = 0; f < frames; f++)
                {
                    whole += totals[f];
                    if (f >= first && f <= last) keptSum += totals[f];
                }

                sb.AppendLine();
                sb.AppendLine("--- what the three seconds carry ---");
                sb.AppendLine("  movement in the whole take   " +
                              (whole * 100f).ToString("0.0") + " cm over " + frames + " frames");
                sb.AppendLine("  movement in the kept window " +
                              (keptSum * 100f).ToString("0.0") + " cm over " + kept + " frames");
                sb.AppendLine("  the window keeps " +
                              (whole > 0f ? keptSum / whole * 100f : 0f).ToString("0.0") +
                              "% of the movement in " +
                              (keptSeconds / clip.length * 100f).ToString("0") +
                              "% of the time");
                sb.AppendLine("  density: " +
                              (keptSeconds > 0f ? keptSum / keptSeconds / (whole / clip.length)
                                                : 0f).ToString("0.00") +
                              "x the take's average. Above 1.0 means the window is the " +
                              "action; near 1.0 means the cut has bought nothing but a " +
                              "shorter file");

                // The two cuts, measured. A frame boundary is where a new pose
                // appears out of nowhere, and the only way to know whether that
                // reads as a cut or as a start is to know how much the pose was
                // moving either side of it.
                sb.AppendLine();
                sb.AppendLine("--- the two cut points ---");
                sb.AppendLine("  frame " + first + " (" + (first / fps).ToString("F2") +
                              " s): this frame moves " +
                              (totals[first] * 100f).ToString("0.00") + " cm");
                sb.AppendLine("  frame " + last + " (" + (last / fps).ToString("F2") +
                              " s): this frame moves " +
                              (totals[last] * 100f).ToString("0.00") + " cm");
                sb.AppendLine("  window average is " +
                              (keptSum / kept * 100f).ToString("0.00") +
                              " cm/frame, so a cut point well under that lands on a " +
                              "settled pose and pops into motion from stillness — " +
                              "visible. Well over means it lands mid-twitch and the " +
                              "gesture is already running.");

                int stillInside = 0;
                for (int f = first; f <= last; f++) if (!moved[f]) stillInside++;
                sb.AppendLine("  still frames inside the window: " + stillInside + " of " +
                              kept + (stillInside == 0
                                  ? "  (continuous motion — the clip will play through)"
                                  : "  (gaps in the gesture — the clip will visibly stall " +
                                    "at each one)"));
            }
            finally
            {
                if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
            }

            // --- apply ---------------------------------------------------------

            Apply(sb, importer, clip, first, last, fps);
        }

        /// <summary>
        /// Movement per frame, over the watched bones.
        ///
        /// Two arrays rather than one, and they are not the same thing: totals
        /// is the sum across bones and moved says whether any single bone passed
        /// the threshold on its own. Deriving one from the other would call a
        /// frame "moving" when five bones each moved a fraction of a millimetre,
        /// which is a different question from the one being asked.
        /// </summary>
        static void SampleFrames(AnimationClip clip, GameObject holder, float fps, int frames,
                                 string[] bones, out float[] totals, out bool[] moved)
        {
            totals = new float[frames];
            moved = new bool[frames];

            // One animation-mode session for the whole sweep rather than one
            // per sample. The mode is global editor state, and entering and
            // leaving it a hundred times in a run is a hundred chances to
            // leave the editor sampling after the tool has finished.
            AnimationMode.StartAnimationMode();
            try
            {
                for (int f = 0; f < frames; f++)
                {
                    float t0 = f / fps;
                    float t1 = Mathf.Min((f + 1) / fps, clip.length);

                    for (int b = 0; b < bones.Length; b++)
                    {
                        var bone = FindBone(holder.transform, bones[b]);
                        if (bone == null) continue;

                        Vector3 p0, p1;
                        Quaternion r0, r1;
                        if (!TrySample(clip, holder, t0, bone.name, out p0, out r0)) continue;
                        if (!TrySample(clip, holder, t1, bone.name, out p1, out r1)) continue;

                        float movedMetres = Vector3.Distance(p0, p1);
                        float turned = Quaternion.Angle(r0, r1) * Mathf.Deg2Rad * 0.2f;

                        float step = movedMetres + turned;
                        totals[f] += step;
                        if (step > StillEpsilon) moved[f] = true;
                    }
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        static void Body(StringBuilder sb)
        {
            var importer = AssetImporter.GetAtPath(Fbx) as ModelImporter;
            if (importer == null)
            {
                sb.AppendLine("No ModelImporter at " + Fbx +
                              ". Nothing to measure — is the asset still there?");
                return;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Fbx);
            if (clip == null)
            {
                sb.AppendLine("No AnimationClip at " + Fbx + ". The FBX imported with " +
                              "no take named Mono_Wake.");
                return;
            }

            sb.AppendLine(Fbx);
            sb.AppendLine("  clip '" + clip.name + "', " + clip.length.ToString("F3") +
                          " s, " + clip.frameRate.ToString("F2") + " fps, " +
                          (clip.isLooping ? "LOOPING" : "one-shot"));

            // Importer state first, because a clip that is already trimmed still
            // reports its full sampled length and the two must not be confused.
            var settings = importer.clipAnimations;
            var mine = settings.FirstOrDefaultByName(clip.name);
            if (mine == null)
            {
                sb.AppendLine("  no clipAnimations override for '" + clip.name + "'. " +
                              "Running with the take as imported.");
            }
            else
            {
                sb.AppendLine("  importer range: firstFrame " + mine.firstFrame.ToString("F2") +
                              ", lastFrame " + mine.lastFrame.ToString("F2") +
                              ", loop " + mine.loopTime + ", name '" + mine.name + "'");
            }

            // --- measure ---

            // A throwaway rig, because sampling a Humanoid clip needs a
            // correctly proportioned skeleton or the retargeting silently
            // produces no motion at all and every frame reads as still.
            //
            // Built from the FBX's own hierarchy rather than from an empty
            // GameObject. AnimationClip.avatar is not readable from a script in
            // this version of Unity, and an Animator with no avatar and no
            // bones would report all 11.4 seconds as a still pose — a result
            // that reads like a finding and is actually a fault.
            GameObject holder = null;
            try
            {
                holder = MakeRig(sb);
                if (holder == null) return;

                float fps = clip.frameRate > 1f ? clip.frameRate : 30f;
                int frames = Mathf.Max(2, Mathf.RoundToInt(clip.length * fps));

                // Roots that matter. The hips and the hands: a wake that is only
                // a face would move neither of the first two and the head is
                // the least reliably sampled bone on a Humanoid clip.
                string[] bones = { "Hips", "LeftHand", "RightHand", "Head", "Spine" };

                float[] totals;
                bool[] moved;
                SampleFrames(clip, holder, fps, frames, bones, out totals, out moved);

                int firstMoved = -1, lastMoved = -1, movingFrames = 0;
                for (int f = 0; f < frames; f++)
                {
                    if (!moved[f]) continue;
                    movingFrames++;
                    if (firstMoved < 0) firstMoved = f;
                    lastMoved = f;
                }

                sb.AppendLine("  sampled " + frames + " frames at " + fps.ToString("F1") +
                              " fps across " + bones.Length + " bones");
                sb.AppendLine("  frames with any movement: " + movingFrames + " of " + frames);

                if (firstMoved < 0)
                {
                    sb.AppendLine("  NO FRAME MOVES. The clip is either empty, a still " +
                                  "pose, or the bones named above do not exist in it — " +
                                  "in which case trimming to zero would delete the " +
                                  "animation, so nothing was changed. Bone names " +
                                  "actually present: " + string.Join(", ", BoneNames(holder)));
                    return;
                }

                float firstTime = firstMoved / fps;
                float lastTime = lastMoved / fps;
                float trimmedLength = lastTime - firstTime + TailKeep;

                sb.AppendLine("  first movement at frame " + firstMoved + " (" +
                              firstTime.ToString("F2") + " s)");
                sb.AppendLine("  last  movement at frame " + lastMoved + " (" +
                              lastTime.ToString("F2") + " s)");
                sb.AppendLine("  hold at the front: " + firstTime.ToString("F2") +
                              " s. hold at the back: " +
                              (clip.length - lastTime).ToString("F2") + " s");

                // The busiest frames, so the report says what the animation is
                // doing rather than only when it stops.
                sb.AppendLine("  loudest frames: " + Loudest(totals, fps));

                float budget = 3.0f;

                // Measured, and it contradicts the assumption this tool was
                // written on. The wake was expected to be a short burst with a
                // long hold on the end. It is not: 336 of 342 frames move, from
                // 0.20 s to 11.37 s. There is no tail to cut, and trimming the
                // lead-in out would save two tenths of a second while making the
                // asset differ from its source for nothing.
                //
                // So instead of trimming blind, this lays the motion out so the
                // choice can be made on evidence: where the movement actually
                // is, and which short window carries the most of it.
                Profile(sb, totals, fps);
                var window = BestWindow(totals, fps, budget);

                sb.AppendLine();

                if (trimmedLength > clip.length * 0.9f)
                {
                    sb.AppendLine("  NOT TRIMMING. The whole clip is in motion, so a " +
                                  "trim would save less than a tenth of it and would " +
                                  "just be a different copy of the source. Restoring " +
                                  "the full range.");
                    Apply(sb, importer, clip, 0, Mathf.RoundToInt(clip.length * fps), fps);
                    Offer(sb, window, budget, fps);
                    return;
                }

                sb.AppendLine("  trimmed to " + trimmedLength.ToString("F2") +
                              " s (from " + clip.length.ToString("F2") + " s)" +
                              (trimmedLength < budget
                                  ? ", inside the " + budget.ToString("F0") +
                                    " s Beat 3 can afford"
                                  : ", STILL LONGER than the " + budget.ToString("F0") +
                                    " s Beat 3 can afford — the wake itself is long, " +
                                    "and only the level designer can decide to cut into it"));

                Apply(sb, importer, clip, firstMoved, lastMoved, fps);
                Offer(sb, window, budget, fps);
            }
            finally
            {
                // Qualified, not bare: `using UnityEngine;` imports types, not
                // their static members, so an unqualified call to a member of
                // UnityEngine.Object does not resolve.
                if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        /// <summary>
        /// The motion laid out over time, in half-second buckets.
        ///
        /// This is the part that makes the decision possible. A report that
        /// says only "it moves for 11.2 seconds" leaves nothing to act on; a
        /// report that shows where the movement is lets a designer pick a
        /// window in the part that carries the action, which here is around
        /// seven seconds in rather than at the start.
        /// </summary>
        static void Profile(StringBuilder sb, float[] totals, float fps)
        {
            const float bucketSeconds = 0.5f;
            int perBucket = Mathf.Max(1, Mathf.RoundToInt(bucketSeconds * fps));

            sb.AppendLine();
            sb.AppendLine("--- motion over time, " + bucketSeconds.ToString("0.0") +
                          " s buckets (bar is relative to the busiest bucket) ---");

            var counts = new List<float>();
            for (int start = 0; start < totals.Length; start += perBucket)
            {
                float sum = 0f;
                for (int i = start; i < System.Math.Min(start + perBucket, totals.Length); i++)
                    sum += totals[i];
                counts.Add(sum);
            }

            float busiest = counts.Count > 0 ? counts.Max() : 0f;

            for (int b = 0; b < counts.Count; b++)
            {
                int len = busiest > 0f
                    ? Mathf.Clamp(Mathf.RoundToInt(counts[b] / busiest * 40f), 0, 40)
                    : 0;

                sb.AppendLine("  " + (b * perBucket / fps).ToString("F1").PadLeft(5) +
                              "s |" + new string('#', len) +
                              " " + (counts[b] * 100f).ToString("0.0") + " cm");
            }
        }

        /// <summary>
        /// The short window carrying the most movement, and where it is.
        ///
        /// Best by total movement rather than by peak, so the answer is not a
        /// single twitch. Reported and not applied: a wake cut out of the
        /// middle of a performance can end mid-gesture, and which gesture is the
        /// one worth ending on is an art call, not a measurement.
        /// </summary>
        static (int first, int last, float amount) BestWindow(float[] totals, float fps,
                                                               float seconds)
        {
            int span = Mathf.Max(2, Mathf.RoundToInt(seconds * fps));
            if (totals.Length < span) return (0, System.Math.Max(0, totals.Length - 1), 0f);

            int bestAt = 0;
            float best = -1f;

            for (int start = 0; start + span <= totals.Length; start++)
            {
                float sum = 0f;
                for (int i = start; i < start + span; i++) sum += totals[i];
                if (sum > best) { best = sum; bestAt = start; }
            }

            return (bestAt, bestAt + span - 1, best);
        }

        static void Offer(StringBuilder sb, (int first, int last, float amount) window,
                          float budget, float fps)
        {
            sb.AppendLine();
            sb.AppendLine("  BUSIEST " + budget.ToString("0.0") + " s WINDOW: frames " +
                          window.first + " to " + window.last + "  (" +
                          (window.first / fps).ToString("F2") + " s to " +
                          (window.last / fps).ToString("F2") + " s), " +
                          (window.amount * 100f).ToString("0.0") + " cm of movement.");
            sb.AppendLine("  NOT applied. Cutting here is a judgement about which " +
                          "gesture the wake should end on, and a designer who has " +
                          "watched it is the only one who can make that call. Say " +
                          "the frame range and it will be set.");
            sb.AppendLine("  The other option is not to cut at all: leave the clip " +
                          "whole and let the Animator play it at speed, or have " +
                          "Beat 3 hand over to Idle after " + budget.ToString("0.0") +
                          " s regardless of where the clip has got to. The beat " +
                          "should not be gated on an animation finishing.");
        }

        /// <summary>
        /// The rig the clip is sampled on: a private instance of the FBX itself,
        /// with the FBX's own avatar if the import produced a valid one.
        ///
        /// Returns null, having said why, rather than an empty GameObject. An
        /// empty rig samples to a constant pose and the measurement that comes
        /// back is "this animation never moves", which is indistinguishable from
        /// a correct answer and is how an animation gets silently deleted.
        /// </summary>
        static GameObject MakeRig(StringBuilder sb)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            if (prefab == null)
            {
                sb.AppendLine("  no GameObject at " + Fbx + ", so there is no rig to " +
                              "sample the clip on. NOT being trimmed.");
                return null;
            }

            var rig = (GameObject)UnityEngine.Object.Instantiate(prefab);
            rig.name = "WakeTrimRig";
            rig.hideFlags = HideFlags.HideAndDontSave;
            rig.transform.position = Vector3.zero;

            var anim = rig.GetComponent<Animator>();
            if (anim == null) anim = rig.AddComponent<Animator>();

            // The avatar lives as a sub-asset of the model, not on the clip.
            var avatar = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<Avatar>()
                             .FirstOrDefault(a => a.isValid);

            if (avatar == null)
            {
                sb.AppendLine("  " + Fbx + " has no valid Avatar, so the clip's " +
                              "Humanoid motion cannot be retargeted onto a rig and " +
                              "every frame would sample as still. Import it as " +
                              "Humanoid (Create From This Model) first. NOT being " +
                              "trimmed — an unmeasured trim is a guess.");
                UnityEngine.Object.DestroyImmediate(rig);
                return null;
            }

            anim.avatar = avatar;
            anim.applyRootMotion = true;
            anim.enabled = false;        // sampled by hand, never played

            var found = rig.GetComponentsInChildren<Transform>(true).Length;
            sb.AppendLine("  sampling on an instance of the FBX: " + found +
                          " transform(s), avatar '" + avatar.name + "'" +
                          (avatar.isHuman ? ", human" : ", NOT human"));

            // Say which of the bones it is about to watch actually exist, before
            // the sweep, so a clip that lacks them reports the cause instead of
            // quietly reporting "no movement".
            var bones = new[] { "Hips", "LeftHand", "RightHand", "Head", "Spine" };
            var present = bones.Where(b => FindBone(rig.transform, b) != null).ToArray();
            if (present.Length == 0)
                sb.AppendLine("  WARNING: none of " + string.Join(", ", bones) +
                              " exist in this rig. Transforms present: " +
                              string.Join(", ", BoneNames(rig).Take(12)));
            else if (present.Length < bones.Length)
                sb.AppendLine("  note: only " + string.Join(", ", present) +
                              " of the watched bones exist in this rig");

            return rig;
        }

        // --- applying --------------------------------------------------------------

        static void Apply(StringBuilder sb, ModelImporter importer, AnimationClip clip,
                          int firstFrame, int lastFrame, float fps)
        {
            // ModelImporter.clipAnimations hands back a fresh copy on get, so an
            // edit that is not assigned back changes nothing at all and the tool
            // reports success.
            var settings = importer.clipAnimations;
            var mine = settings.FirstOrDefaultByName(clip.name);

            if (mine == null)
            {
                // No override exists, so this run created one. Only worth doing
                // if there is nothing already there to clobber — which is
                // exactly why the name is looked up rather than assumed.
                mine = new ModelImporterClipAnimation
                {
                    name = clip.name,
                    takeName = clip.name
                };
                settings = new ModelImporterClipAnimation[] { mine };
            }

            int beforeFirst = mine.firstFrame > 0f ? Mathf.RoundToInt(mine.firstFrame) : 0;
            int beforeLast = mine.lastFrame > 0f ? Mathf.RoundToInt(mine.lastFrame) : 0;

            mine.firstFrame = firstFrame;
            mine.lastFrame = lastFrame;
            mine.loopTime = false;
            mine.name = clip.name;

            importer.clipAnimations = settings;
            importer.SaveAndReimport();

            // SaveAndReimport returns as soon as the import is QUEUED. Reading
            // the importer back straight away gives a null importer or an
            // empty take list for a completely healthy asset, and all three
            // look identical to a failed import — which is how a correct file
            // gets "fixed" into a broken one.
            Settle(Fbx);

            var check = AssetImporter.GetAtPath(Fbx) as ModelImporter;
            if (check == null)
            {
                sb.AppendLine("  the importer is still null after settling. The reimport " +
                              "has not landed yet; re-run this tool rather than " +
                              "trusting this report.");
                return;
            }

            var now = check.clipAnimations.FirstOrDefaultByName(clip.name);
            if (now == null)
            {
                sb.AppendLine("  after reimport there is no clip called '" + clip.name +
                              "'. The name may have been changed by the import. NOT OK.");
                return;
            }

            sb.AppendLine();
            sb.AppendLine("  APPLIED: firstFrame " + beforeFirst + " -> " +
                          Mathf.RoundToInt(now.firstFrame) + ", lastFrame " +
                          beforeLast + " -> " + Mathf.RoundToInt(now.lastFrame) +
                          ", loop " + now.loopTime);
            sb.AppendLine("  read back off disk, so this is what the asset now is " +
                          "and not what was asked for");

            float newLength = (now.lastFrame - now.firstFrame) / fps;
            sb.AppendLine("  new sampled length " + newLength.ToString("F2") + " s");

            if (newLength < 0.05f)
                sb.AppendLine("  BAD: the trimmed clip is effectively zero length. Undo " +
                              "this in source control and report it.");
        }

        /// <summary>
        /// Wait for the import to actually land before reading anything back.
        /// </summary>
        static void Settle(string path)
        {
            for (int i = 0; i < 40; i++)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp != null && imp.clipAnimations != null &&
                    imp.clipAnimations.Length > 0) return;
                System.Threading.Thread.Sleep(50);
            }
        }

        // --- sampling --------------------------------------------------------------

        static bool TrySample(AnimationClip clip, GameObject holder, float time,
                              string boneName, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;

            var bone = FindBone(holder.transform, boneName);
            if (bone == null) return false;

            // AnimationMode, not AnimationUtility: this is the editor's own
            // sampling path, it applies a Humanoid clip to a rig without
            // entering play mode, and it records what it changed so the mode can
            // be stopped cleanly afterwards. The Animator is left disabled
            // throughout, so nothing is playing and nothing is being stepped.
            AnimationMode.SampleAnimationClip(holder, clip, time);
            pos = bone.position;
            rot = bone.rotation;

            return true;
        }

        static Transform FindBone(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var hit = FindBone(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        static IEnumerable<string> BoneNames(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (t != go.transform) yield return t.name;
        }

        /// <summary>
        /// The three frames where the most is moving, as "1.23s (moved 4.1 cm)".
        ///
        /// Without this the report says only *when* the wake stops, which is
        /// enough to trim it and not enough to tell whether what is being kept
        /// is the wake or a settling loop that happens to sit in the middle of
        /// it.
        /// </summary>
        static string Loudest(float[] totals, float fps)
        {
            var order = new List<int>();
            for (int i = 0; i < totals.Length; i++) order.Add(i);

            // Descending by amount moved. A comparison rather than a negated
            // key, so a tie keeps frame order and the report is identical on
            // every run.
            order.Sort((a, b) =>
            {
                int byAmount = totals[b].CompareTo(totals[a]);
                return byAmount != 0 ? byAmount : a.CompareTo(b);
            });

            var sb = new StringBuilder();
            for (int i = 0; i < 3 && i < order.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append((order[i] / fps).ToString("F2") + "s (moved " +
                          (totals[order[i]] * 100f).ToString("0.0") + " cm)");
            }

            return sb.Length > 0 ? sb.ToString() : "nothing moved at all";
        }
    }
}
