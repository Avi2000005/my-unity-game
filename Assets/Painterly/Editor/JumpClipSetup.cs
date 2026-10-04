using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Imports the jump clip, and proves which candidate is actually a jump.
    ///
    /// Two files are called a jump and neither name says which it is. The probe
    /// found their own takes are 1.000s and 1.900s long. That is the whole
    /// decision: a hop is about a second, and a 1.9s clip would hold Ari in the
    /// air for twice as long as the arc takes, which reads as a hang rather than
    /// a jump. It might also be a run-up, or two hops in one take, and neither
    /// can be told from a duration.
    ///
    /// So the hips are sampled and the shape of the arc is read off the numbers.
    /// One rise and one fall is a hop. Rise, fall, rise, fall is a run. A flat
    /// line is not a jump at all, and would mean the curve is not on the bone
    /// being sampled.
    ///
    /// Both candidates are imported as Humanoid first, because the arcs of a
    /// Generic clip are meaningless: nothing is mapped, so the mesh does not
    /// move, and a take that looks like a hop on the import settings can be a
    /// walk once it is on a real skeleton.
    /// </summary>
    public static class JumpClipSetup
    {
        const string Dir = "Assets/Art/Ari/Models";
        const string Report = "Temp/jump_setup.txt";

        /// <summary>The take holding the file's own animation, as in AriImportSetup.</summary>
        const string OwnTake = "mixamo.com";

        /// <summary>Final name. The controller will look for exactly this.</summary>
        public const string FinalName = "Ari_Jump";

        /// <summary>
        /// A dip smaller than this is noise from the sampling, not a landing.
        /// 3cm: an ankle-height wiggle has to be well under half of a real hop's
        /// apex, which is 20cm or more, and over a 200-sample sweep the sampling
        /// grid alone produces about a centimetre of jitter.
        /// </summary>
        const float ArcProminence = 0.03f;

        const int Samples = 200;

        sealed class Candidate
        {
            public string File;
            public string ProbeName;      // distinct name while both are measured
            public string Path;
            public float Length;
            public bool Human;
            public bool Imported;
            public string Failure;
            public string BoneUsed;
            public string BoneRanges = "";
            public float PoseDelta;
            public float[] Arc = System.Array.Empty<float>();
            public int Arcs;
            public float Min, Max, Range, ApexFraction;
        }

        // readonly, not const: an array of objects is never a compile-time constant.
        static readonly (string File, string Probe)[] Candidates =
        {
            ("ari_running_Jump", "Ari_Jump_RJ"),
            ("ari_jumping",       "Ari_Jump_J"),
        };

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] which of the two jump files is a jump?");
            sb.AppendLine();

            // The files were copied in from outside, so nothing has a .meta yet
            // and every lookup below would otherwise return null.
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate |
                                  ImportAssetOptions.ForceSynchronousImport);

            var measured = new List<Candidate>();

            foreach (var (file, probeName) in Candidates)
            {
                var c = new Candidate
                {
                    File = file,
                    ProbeName = probeName,
                    Path = Dir + "/" + file + ".fbx"
                };
                ImportAsHumanoid(c, sb);
                if (!c.Imported) { measured.Add(c); continue; }
                Measure(c, sb);
                measured.Add(c);
            }

            sb.AppendLine();
            sb.AppendLine("--- arc of each candidate's hips ---");
            foreach (var c in measured)
            {
                if (c.Failure != null)
                {
                    sb.AppendLine(c.File + ": " + c.Failure);
                    continue;
                }

                sb.AppendLine();
                sb.AppendLine(c.File);
                sb.AppendLine("  clip '" + c.ProbeName + "'  " + c.Length.ToString("F3") +
                              " s  humanoid-mapped=" + c.Human);
                sb.AppendLine("  measured on    : " + c.BoneUsed +
                              "   (all bones: " + c.BoneRanges + ")");
                sb.AppendLine("  pose actually moves by " + c.PoseDelta.ToString("F0") +
                              " deg, so the clip reached the rig");
                sb.AppendLine("  vertical range : " + c.Range.ToString("F3") + " m" +
                              "  (low " + c.Min.ToString("F3") +
                              ", high " + c.Max.ToString("F3") + ")");
                sb.AppendLine("  hops           : " + c.Arcs);
                sb.AppendLine("  apex at        : " + (c.ApexFraction * 100f).ToString("F0") +
                              "% of the clip");
                sb.AppendLine("  shape          : " + Sparkline(c.Arc));
            }

            // The decision, stated as a rule so it can be argued with.
            var live = measured.Where(c => c.Failure == null).ToList();
            if (live.Count == 0)
            {
                sb.AppendLine();
                sb.AppendLine("Neither candidate imported. Nothing changed in the controller.");
                Finish(sb);
                return;
            }

            var winner = live
                .OrderBy(c => c.Arcs)                 // a hop is one arc, a run is many
                .ThenBy(c => c.Range < 0.05f ? 1 : 0) // a flat curve is not a jump
                .ThenBy(c => c.Length)                 // then prefer the shorter take
                .First();

            var losers = live.Where(c => c != winner).ToList();

            sb.AppendLine();
            sb.AppendLine("--- decision ---");
            sb.AppendLine("rule: fewest arcs, then any real vertical travel, then shortest.");
            sb.AppendLine("winner: " + winner.File + "  (" + winner.Arcs + " arc, " +
                          winner.Range.ToString("F3") + " m, " +
                          winner.Length.ToString("F3") + " s)");

            // Give the winner its real name.
            Rename(winner.Path, winner.ProbeName, FinalName, sb);
            // And take the losers out of circulation entirely. An unused FBX left
            // in the project still lands a clip in it under the raw Mixamo take
            // name, and two clips sharing a name make Animator.StringToHash
            // ambiguous — a state then resolves to whichever loaded first, with
            // nothing wrong anywhere to find.
            foreach (var l in losers) EmptyClips(l.Path, sb);

            sb.AppendLine();
            sb.AppendLine("--- end state, read back from the project ---");
            foreach (var c in measured)
                sb.AppendLine(c.File + " : " + LandedClips(c.Path));

            var clips = AssetDatabase.LoadAllAssetsAtPath(winner.Path).OfType<AnimationClip>()
                          .Where(x => !x.legacy).Select(x => x.name).ToArray();
            sb.AppendLine();
            sb.AppendLine("clips on the winner: " + string.Join(", ", clips));
            sb.AppendLine(FinalName + " is what the controller needs as a third state.");

            // Checked rather than asserted. A fixed sentence saying "the
            // controller does not have it yet" reads as current fact in every
            // later run, and is wrong from the first run after the controller
            // has been built. It is the kind of stale line that sends someone
            // rebuilding a controller that is already correct.
            var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(
                "Assets/Art/Ari/Ari.controller");
            if (ctrl == null)
            {
                sb.AppendLine("Ari.controller does not exist yet — " +
                              "run Tools/Echoes/Build Ari Controller.");
            }
            else
            {
                var machine = ctrl.layers[0].stateMachine;
                var inGraph = machine.states
                    .Select(c => c.state)
                    .FirstOrDefault(s => s.motion != null && s.motion.name == FinalName);
                sb.AppendLine(inGraph == null
                    ? "Ari.controller exists but has no " + FinalName + " state — " +
                      "run Tools/Echoes/Build Ari Controller."
                    : "Ari.controller already has the " + inGraph.name + " state. Nothing to do.");
            }

            var finalClip = AssetDatabase.LoadAllAssetsAtPath(winner.Path)
                .OfType<AnimationClip>().FirstOrDefault(x => x.name == FinalName);
            sb.AppendLine("on disk: " + (finalClip == null ? "NOT FOUND" :
                FinalName + " is " + finalClip.length.ToString("F3") + " s, isLooping=" +
                finalClip.isLooping +
                (finalClip.isLooping ? "  <- WRONG, a looping hop crouches forever"
                                     : "  <- right, a hop is a one-shot")));

            foreach (var l in losers)
                sb.AppendLine("unused file left in place (clips emptied): " + l.Path +
                              "  — the source is still at C:\\Users\\chate\\Documents\\" +
                              l.File + ".fbx, and AssetDatabase.DeleteAsset will drop it.");

            Finish(sb);
        }

        /// <summary>
        /// Humanoid import, one named clip, no loop. Mirrors AriImportSetup, with
        /// loopTime off because a looping hop replays its crouch forever.
        /// </summary>
        static void ImportAsHumanoid(Candidate c, StringBuilder sb)
        {
            if (AssetImporter.GetAtPath(c.Path) == null)
                AssetDatabase.ImportAsset(c.Path,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            var mi = AssetImporter.GetAtPath(c.Path) as ModelImporter;
            if (mi == null) { c.Failure = "did not import; is the file in the project?"; return; }

            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.sourceAvatar = null;
            // These files carry a duplicate of Ari's mesh that nothing renders.
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = true;
            mi.animationCompression = ModelImporterAnimationCompression.Optimal;
            mi.addCollider = false;
            mi.isReadable = false;
            mi.SaveAndReimport();

            // Re-read: the take list is built during import, so on a file whose
            // animationType just changed it is still the old one.
            mi = AssetImporter.GetAtPath(c.Path) as ModelImporter;
            if (mi == null) { c.Failure = "lost the importer after reimport"; return; }

            var takes = mi.defaultClipAnimations;
            var own = takes.FirstOrDefault(x => x.takeName == OwnTake);
            if (own == null)
            {
                c.Failure = "take '" + OwnTake + "' not found; takes are " +
                            (takes.Length == 0 ? "none" :
                             string.Join(", ", takes.Select(x => x.takeName)));
                return;
            }

            own.name = c.ProbeName;
            own.loopTime = false;
            own.loopPose = false;
            // clipAnimations hands back a fresh copy, so the edit must be assigned
            // back or it is written to nothing and reverts on the next import.
            mi.clipAnimations = new[] { own };
            mi.SaveAndReimport();

            var clip = AssetDatabase.LoadAllAssetsAtPath(c.Path).OfType<AnimationClip>()
                         .FirstOrDefault(x => x.name == c.ProbeName);
            if (clip == null)
            {
                c.Failure = "clip missing after rename; clips are " +
                            string.Join(", ", AssetDatabase.LoadAllAssetsAtPath(c.Path)
                                .OfType<AnimationClip>().Select(x => "'" + x.name + "'"));
                return;
            }

            c.Length = clip.length;
            c.Human = clip.humanMotion;
            c.Imported = true;
            sb.AppendLine(c.File + ": imported as Humanoid, '" + c.ProbeName + "' " +
                          c.Length.ToString("F3") + " s, mapped to the skeleton=" + c.Human);
        }

        /// <summary>
        /// Sample the body rising and falling over the take.
        ///
        /// The feet are the bones worth reading, not the hips. Mixamo exports an
        /// in-place jump with the root planted on the ground, and a humanoid clip
        /// stores muscle rotations rather than a root position — so the hips sit
        /// at the same height all the way through the hop while the feet leave
        /// the paving and come back. Watching the hips would report a flat line
        /// for a perfectly good jump. The hips are sampled too, and whichever
        /// bone actually moves the most is the one reported.
        ///
        /// SampleAnimation rather than Animator.Play because the clip is not in a
        /// controller yet — Play addresses a state, and there is no state to
        /// address. It also avoids stepping a delta 200 times, which accumulates
        /// float error until the tail of the curve no longer matches the start.
        /// </summary>
        static void Measure(Candidate c, StringBuilder sb)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(c.Path);
            if (go == null) { c.Failure = "no GameObject at " + c.Path; return; }

            var clip = AssetDatabase.LoadAllAssetsAtPath(c.Path).OfType<AnimationClip>()
                         .FirstOrDefault(x => x.name == c.ProbeName);
            if (clip == null) { c.Failure = "probe clip vanished"; return; }

            var inst = UnityEngine.Object.Instantiate(go);
            try
            {
                var anim = inst.GetComponentInChildren<Animator>();
                if (anim == null) { c.Failure = "no Animator on the imported model"; return; }

                var watched = new List<(string Name, Transform Bone)>();
                AddBone(watched, anim, "Hips", HumanBodyBones.Hips);
                AddBone(watched, anim, "Spine", HumanBodyBones.Spine);
                AddBone(watched, anim, "LeftFoot", HumanBodyBones.LeftFoot);
                AddBone(watched, anim, "RightFoot", HumanBodyBones.RightFoot);
                AddBone(watched, anim, "Head", HumanBodyBones.Head);
                if (watched.Count == 0)
                {
                    c.Failure = "no bones mapped at all, so the rig cannot be sampled";
                    return;
                }

                var series = new Dictionary<string, float[]>();
                var atStart = new Dictionary<string, Quaternion>();
                foreach (var w in watched)
                {
                    series[w.Name] = new float[Samples];
                    atStart[w.Name] = w.Bone.localRotation;
                }

                for (int i = 0; i < Samples; i++)
                {
                    float t = (float)i / (Samples - 1) * clip.length;
                    clip.SampleAnimation(inst, t);
                    foreach (var w in watched) series[w.Name][i] = w.Bone.position.y;
                }

                // Prove the clip reached the rig at all. A humanoid clip sampled
                // without a usable avatar leaves every bone in its bind pose, and
                // that is indistinguishable from a clip that genuinely does not
                // move — both produce a range of zero. Checking the pose
                // separately is what tells the two apart.
                clip.SampleAnimation(inst, clip.length * 0.5f);
                float poseDelta = 0f;
                foreach (var w in watched)
                    poseDelta = Mathf.Max(poseDelta,
                        Quaternion.Angle(atStart[w.Name], w.Bone.localRotation));

                if (poseDelta < 0.5f)
                {
                    c.Failure = "the clip was sampled but no bone moved " +
                                "(max pose change " + poseDelta.ToString("F2") +
                                " deg) — SampleAnimation cannot reach this humanoid " +
                                "clip, so its arc cannot be read this way";
                    return;
                }
                c.PoseDelta = poseDelta;

                var best = watched
                    .Select(w => (w.Name, Arc: series[w.Name], Range: RangeOf(series[w.Name])))
                    .OrderByDescending(x => x.Range)
                    .First();

                c.Arc = best.Arc;
                c.BoneUsed = best.Name;
                c.BoneRanges = string.Join(", ",
                    watched.Select(w => w.Name + " " +
                        RangeOf(series[w.Name]).ToString("F3") + " m"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inst);
            }

            c.Min = c.Arc.Min();
            c.Max = c.Arc.Max();
            c.Range = c.Max - c.Min;
            c.Arcs = CountArcs(c.Arc, ArcProminence);
            c.ApexFraction = (float)ArrayIndexOfMax(c.Arc) / (Samples - 1);
        }

        static void AddBone(List<(string, Transform)> into, Animator anim,
                            string name, HumanBodyBones bone)
        {
            if (anim == null) return;
            var t = anim.GetBoneTransform(bone);
            if (t != null) into.Add((name, t));
        }

        static float RangeOf(float[] v) => v.Max() - v.Min();

        static int ArrayIndexOfMax(float[] v)
        {
            int best = 0;
            for (int i = 1; i < v.Length; i++) if (v[i] > v[best]) best = i;
            return best;
        }

        /// <summary>
        /// Count rises-and-falls that clear the prominence bar.
        ///
        /// Walks the direction of travel and treats every reversal as a landing
        /// or a launch, but only counts one if it is at least the prominence away
        /// from the last extremum. Without that, a 200-sample sweep of a curve
        /// that is drifting sideways would report a dozen "hops" from numerical
        /// noise, and a single hop would read as two.
        /// </summary>
        static int CountArcs(float[] v, float prominence)
        {
            int arcs = 0;
            int lastExtremum = 0;
            int direction = 0;

            for (int i = 1; i < v.Length; i++)
            {
                float d = v[i] - v[i - 1];
                if (Mathf.Abs(d) < 0.0001f) continue;   // flat step, not a reversal
                int step = d > 0f ? 1 : -1;

                if (direction != 0 && step != direction)
                {
                    if (Mathf.Abs(v[i - 1] - v[lastExtremum]) >= prominence)
                    {
                        arcs++;
                        lastExtremum = i - 1;
                    }
                }
                direction = step;
            }
            return arcs;
        }

        /// <summary>40 characters of shape, so a number can be read as a motion.</summary>
        static string Sparkline(float[] v)
        {
            if (v == null || v.Length == 0) return "(none)";

            const string ramp = " .:-=+*#%@";
            float min = v.Min(), max = v.Max();
            float span = max - min;
            if (span < 0.0001f) return "flat, no vertical movement at all";

            var sb = new StringBuilder();
            int cols = 40;
            for (int i = 0; i < cols; i++)
            {
                // Average the window so a single noisy sample cannot spike the
                // character and make a smooth hop look like it shakes.
                int a = i * v.Length / cols;
                int b = Mathf.Max(a + 1, (i + 1) * v.Length / cols);
                float mean = 0f;
                for (int k = a; k < b && k < v.Length; k++) mean += v[k];
                mean /= (b - a);

                int level = Mathf.Clamp(Mathf.RoundToInt((mean - min) / span * (ramp.Length - 1)),
                                        0, ramp.Length - 1);
                sb.Append(ramp[level]);
            }
            return sb.ToString();
        }

        static void Rename(string path, string from, string to, StringBuilder sb)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { sb.AppendLine("rename failed, no importer: " + path); return; }

            var clips = mi.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                sb.AppendLine("rename failed, no configured clips: " + path);
                return;
            }

            bool changed = false;
            foreach (var c in clips)
                if (c.name == from) { c.name = to; changed = true; }

            if (!changed)
            {
                sb.AppendLine("nothing to rename on " + path + "; clips are " +
                              string.Join(", ", clips.Select(x => "'" + x.name + "'")));
                return;
            }

            mi.clipAnimations = clips;
            mi.SaveAndReimport();

            // Read back rather than trust the write. This has reported success
            // once already while the meta on disk still held the old name.
            var after = (AssetImporter.GetAtPath(path) as ModelImporter)?.clipAnimations;
            if (after != null && after.Any(x => x.name == to))
                sb.AppendLine("renamed '" + from + "' -> '" + to + "' and verified on disk");
            else
                sb.AppendLine("RENAME DID NOT STICK on " + path + "; clips are now " +
                              (after == null || after.Length == 0 ? "none" :
                               string.Join(", ", after.Select(x => "'" + x.name + "'"))));
        }

        /// <summary>
        /// Take a file out of circulation so its raw take names cannot collide.
        ///
        /// Setting clipAnimations to an empty array does NOT do this. An empty
        /// array is read as "no overrides", so the importer falls back to its
        /// default of importing every take in the file — verified, not assumed:
        /// the first run of this tool reported success on the strength of the
        /// configured array being empty, while the file still held two clips
        /// under their raw Mixamo names. Only importAnimation = false actually
        /// results in no clips landing in the project.
        ///
        /// The file itself is left alone. It is 18 MB of unused asset rather
        /// than a broken one, and its source is still on disk outside the
        /// project, so deleting it is a decision rather than a repair.
        /// </summary>
        static void EmptyClips(string path, StringBuilder sb)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { sb.AppendLine("could not empty " + path); return; }

            mi.importAnimation = false;
            mi.SaveAndReimport();

            // Read what actually landed in the project, not what the importer
            // was configured with. Those are different things and only one of
            // them is the truth.
            var after = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                          .Where(x => !x.legacy && !x.name.StartsWith("__")).ToArray();
            sb.AppendLine(after.Length == 0
                ? path + ": animation import switched off, verified — no clips land"
                : path + ": STILL HAS CLIPS -> " +
                  string.Join(", ", after.Select(x => "'" + x.name + "'")));
        }

        /// <summary>
        /// What each candidate file actually contributes to the project.
        ///
        /// Read back through LoadAllAssetsAtPath rather than off the importer,
        /// because the importer reports what it was told and the project holds
        /// what it was given. Where those two disagree — an empty override array
        /// being the obvious case — only the second one matters.
        /// </summary>
        static string LandedClips(string path)
        {
            var landed = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                            .Where(x => !x.legacy && !x.name.StartsWith("__"))
                            .Select(x => x.name).OrderBy(x => x).ToArray();
            return landed.Length == 0 ? "NOTHING (file is inert)" :
                   string.Join(", ", landed.Select(x => "'" + x + "'"));
        }

        static void Finish(StringBuilder sb)
        {
            var full = Path.Combine(Directory.GetCurrentDirectory(), Report);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
