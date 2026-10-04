using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Reports what animation takes the newly copied FBX files actually contain.
    ///
    /// Read only. Two candidate jump files were copied in and the choice between
    /// them should not be made on the basis of their file names: "ari_jumping"
    /// and "ari_running_Jump" are both called a jump and there is no way to tell
    /// from outside whether either is a single hop, a run-up, or a run-cycle with
    /// a hop in the middle of it. Those play back very differently — a run-up
    /// clip makes Ari back up before she leaves the ground, and a cycle with the
    /// hop baked in the middle loops her back into a run she is not in.
    ///
    /// So the clips are counted and timed first, and the name is read afterwards.
    /// </summary>
    public static class JumpClipProbe
    {
        const string Report = "Temp/jump_clips.txt";

        // readonly, not const: an array of constant strings is still not itself a
        // compile-time constant, and C# will not pretend otherwise.
        static readonly string[] Candidates =
        {
            "Assets/Art/Ari/Models/ari_jumping.fbx",
            "Assets/Art/Ari/Models/ari_running_Jump.fbx",
        };

        /// <summary>Everything the project already has, for comparison.</summary>
        const string ExistingDir = "Assets/Art/Ari/Models";

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] what is in the jump candidates?");
            sb.AppendLine();
            sb.AppendLine("The two files have similar sizes, which is the whole problem:");
            sb.AppendLine("a single hop and a run-up hop are the same rig and the same");
            sb.AppendLine("textures, so size says nothing about which one is which.");

            foreach (var path in Candidates)
            {
                sb.AppendLine();
                sb.AppendLine("--- " + path + " ---");

                if (!File.Exists(path))
                {
                    sb.AppendLine("NOT ON DISK.");
                    continue;
                }

                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    sb.AppendLine("NOT IMPORTED YET (no ModelImporter). " +
                                  "Refresh the asset database, then run again.");
                    continue;
                }

                var fi = new FileInfo(path);
                sb.AppendLine("size            : " + fi.Length.ToString("N0") + " bytes");
                sb.AppendLine("animationType   : " + importer.animationType +
                                  "   (0 = None, 1 = Legacy, 2 = Generic, 3 = Humanoid)");
                sb.AppendLine("importAnimation : " + importer.importAnimation);

                // A Humanoid clip without a valid avatar plays, but nothing maps
                // to the skeleton, so the mesh does not move. Worth stating rather
                // than assuming, because it looks like "the animation is broken".
                var avatar = importer.avatarSetup;
                sb.AppendLine("avatarSetup     : " + avatar);

                // Named takes, as the importer currently has them.
                var named = importer.clipAnimations;
                if (named != null && named.Length > 0)
                {
                    sb.AppendLine("clipAnimations  : " + named.Length + " configured");
                    foreach (var c in named)
                        sb.AppendLine("   '" + c.name + "'  loop=" + c.loopTime +
                                      "  in=" + c.firstFrame.ToString("F3") +
                                      " out=" + c.lastFrame.ToString("F3") +
                                      (c.name.StartsWith("__") ? "   <- importer preview" : ""));
                }
                else
                {
                    sb.AppendLine("clipAnimations  : none configured (importing whole file as one take)");
                }

                // What is genuinely in the file.
                var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .Where(c => !c.legacy)
                    .ToArray();

                var real = clips.Where(c => !c.name.StartsWith("__")).ToArray();
                sb.AppendLine();
                sb.AppendLine("takes in the file: " + real.Length);
                foreach (var c in real.OrderBy(c => c.length))
                {
                    sb.AppendLine("   '" + c.name + "'");
                    sb.AppendLine("        length      : " + c.length.ToString("F3") + " s");
                    sb.AppendLine("        frameRate   : " + c.frameRate.ToString("F2"));
                    sb.AppendLine("        looping     : " + c.isLooping);
                    sb.AppendLine("        humanMotion : " + c.humanMotion +
                                      "   <- " + HumanoidNote(c.humanMotion));

                    // Root drift decides whether this can drive a jump at all, or
                    // whether the Animator will fight the movement code for the
                    // transform's position.
                    sb.AppendLine("        root drift  : " + RootDrift(c));
                }

                if (real.Length == 0)
                    sb.AppendLine("   (no takes — importAnimation may be off, or the file is a model only)");
            }

            // --- what Ari already uses, so the new clip can be judged against it
            sb.AppendLine();
            sb.AppendLine("--- what Ari already has, for comparison ---");
            var existing = AssetDatabase.FindAssets("t:AnimationClip", new[] { ExistingDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<AnimationClip>()
                .Where(c => !c.legacy && !c.name.StartsWith("__"))
                .GroupBy(c => c.name)
                .Select(g => g.First())
                .OrderBy(c => c.name)
                .ToArray();

            sb.AppendLine("distinct clips in " + ExistingDir + ": " + existing.Length);
            foreach (var c in existing)
                sb.AppendLine("   '" + c.name + "'  " + c.length.ToString("F2") + " s  loop=" + c.isLooping
                              + "  human=" + c.humanMotion);

            sb.AppendLine();
            sb.AppendLine("Ari_Idle and Ari_Walk are the two the controller needs today.");
            sb.AppendLine("A jump should be a single take, under about 1.5 s, not looping.");

            Finish(sb);
        }

        static string HumanoidNote(bool human)
            => human ? "mapped to the skeleton, so the mesh moves"
                     : "NOT mapped — plays but the mesh will not move";

        /// <summary>
        /// How far the root travels over the take, sampled from the first and last
        /// frames. A hop should carry the body up and back down; a run cycle
        /// carries it forward. The number decides whether root motion has to be
        /// extracted or can stay off.
        /// </summary>
        static string RootDrift(AnimationClip clip)
        {
            try
            {
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                if (settings != null && settings.loopTime)
                    return "clip is looped, so drift is not meaningful";

                // Sample the hips. An arbitrary bone index would be luck; the hips
                // are the one bone every humanoid rig agrees on.
                var bindings = AnimationUtility.GetCurveBindings(clip);
                if (bindings == null || bindings.Length == 0) return "no curves";

                float total = 0f;
                foreach (var b in bindings)
                {
                    if (b.type != typeof(Animator) || b.propertyName != "m_LocalPosition.x")
                        continue;
                    var curve = AnimationUtility.GetEditorCurve(clip, b);
                    if (curve == null || curve.length < 2) continue;
                    total += Mathf.Abs(curve.keys[curve.length - 1].value - curve.keys[0].value);
                }
                return total.ToString("F3") + " m of root travel on X";
            }
            catch (System.Exception e)
            {
                return "could not be read: " + e.GetType().Name;
            }
        }

        static void Finish(StringBuilder sb)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(
                Path.Combine(Directory.GetCurrentDirectory(), Report)));
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
