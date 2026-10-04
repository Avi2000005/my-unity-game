using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Reports the range of every meaningful muscle curve in each clip.
    /// Writes Temp/ari_motion.txt.
    ///
    /// The take names cannot be trusted, but not for the reason they first
    /// appear to. Every one of the three files carries a take called
    /// "Armature|Armature|walking_man|baselayer", always exactly 1.033s and
    /// 31 frames, and it is byte-for-byte identical in all three. It is not a
    /// rest pose — it is a walk cycle, widest muscle range 1.209, the same
    /// numbers in every file — it is just Mixamo's default walk baked into
    /// every export of this rig. It says "walking_man" because that is what
    /// the avatar was called when the rig was made.
    ///
    /// So each file really does hold two walk cycles, and picking by name
    /// picks the wrong one. Measured, the file's own take is unmistakable:
    /// Ari_character's 4.467s clip at range 1.363, Idle's 8.333s clip at 0.086
    /// (an idle barely moves anything, which is exactly right), and Walking's
    /// 1.033s clip at 1.039. The import tool keeps only these.
    ///
    /// These curves must be read as muscles, not as bone transforms. A clip
    /// imported as Humanoid is retargeted into Unity's muscle space at import
    /// time, so all 130 bindings are Animator properties ("Left Foot Up-Down",
    /// "RootT.y") and there is not one m_LocalPosition curve in the file.
    /// Searching for bone transforms finds nothing and reports every clip as
    /// static — a false negative, not a finding.
    /// </summary>
    public static class AriMotionProbe
    {
        const string Dir = "Assets/Art/Ari/Models";

        [MenuItem("Tools/Echoes/Probe Ari Motion", priority = 91)]
        public static void Run()
        {
            var sb = new StringBuilder();

            foreach (var model in new[] { "Ari_character", "Idle", "Walking" })
            {
                ProbeMotion(sb, model);
                ProbeSkeleton(sb, model);
            }

            File.WriteAllText("Temp/ari_motion.txt", sb.ToString());
            Debug.Log("[Echoes] Ari motion probe -> Temp/ari_motion.txt\n" + sb);
        }

        /// <summary>
        /// The muscles worth watching, chosen because a walk cycle cannot help
        /// but move them. If these are flat the clip is dead, whatever it is
        /// called.
        ///
        /// The last four are the ones this avatar cannot honour. UpperChest is
        /// unmapped on this rig, and the auto-mapper put LeftEye on the top
        /// spine bone, so motion in those curves has nowhere correct to go and
        /// surfaces as Ari's upper back twitching. Measuring them says whether
        /// that is a live artefact or a dormant one.
        /// </summary>
        static readonly string[] Watch =
        {
            "RootT.x", "RootT.y", "RootT.z",
            "Left Upper Leg Front-Back", "Right Upper Leg Front-Back",
            "Left Lower Leg Stretch", "Right Lower Leg Stretch",
            "Left Foot Up-Down", "Right Foot Up-Down",
            "Left Arm Down-Up", "Right Arm Down-Up",
            "Spine Front-Back", "Head Nod Down-Up",
            "UpperChest Front-Back", "UpperChest Left-Right", "UpperChest Twist Left-Right",
            "Left Eye Down-Up", "Left Eye In-Out",
            "Right Eye Down-Up", "Right Eye In-Out"
        };

        static void ProbeMotion(StringBuilder sb, string model)
        {
            string path = $"{Dir}/{model}.fbx";
            sb.AppendLine($"=== MOTION: {model} ===");

            var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToArray();

            if (clips.Length == 0) { sb.AppendLine("  no clips\n"); return; }

            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                sb.AppendLine($"  '{clip.name}' len={clip.length:0.000}s " +
                              $"curves={bindings.Length} loop={clip.isLooping}");

                double widest = 0;

                foreach (var prop in Watch)
                {
                    float min = float.MaxValue, max = float.MinValue;
                    int keys = 0;

                    foreach (var b in bindings.Where(b => b.propertyName == prop))
                    {
                        var curve = AnimationUtility.GetEditorCurve(clip, b);
                        if (curve == null || curve.length == 0) continue;
                        foreach (var k in curve.keys)
                        {
                            min = Math.Min(min, k.value);
                            max = Math.Max(max, k.value);
                        }
                        keys += curve.length;
                    }

                    if (keys == 0) continue;

                    var range = max - min;
                    widest = Math.Max(widest, range);
                    sb.AppendLine($"      {prop,-28} {min,8:0.000} .. {max,8:0.000}  " +
                                  $"range {range,7:0.000}  keys {keys}");
                }

                sb.AppendLine($"      => widest muscle range {widest:0.000}  " +
                              $"{(widest < 0.01 ? "STATIC (rest pose)" : "ANIMATED")}\n");
            }
        }

        /// <summary>
        /// Print the bone hierarchy with local offsets.
        ///
        /// The avatar maps Spine to Spine02 and Chest to Spine01, which reads
        /// backwards. Whether it actually is inverted depends on which bone sits
        /// below which, and the names here are Blender's, not Unity's — so the
        /// hierarchy gets printed rather than concluded from the names.
        /// </summary>
        static void ProbeSkeleton(StringBuilder sb, string model)
        {
            string path = $"{Dir}/{model}.fbx";
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            sb.AppendLine($"=== SKELETON: {model} ===");

            if (root == null) { sb.AppendLine("  not found\n"); return; }

            var all = root.GetComponentsInChildren<Transform>(true);
            var start = all.FirstOrDefault(t => t.name == "Hips") ?? all.FirstOrDefault();
            if (start == null) { sb.AppendLine("  no transforms\n"); return; }

            int depth = 0;
            for (var p = start.parent; p != null; p = p.parent) depth++;

            Walk(start, depth);
            sb.AppendLine();

            void Walk(Transform t, int d)
            {
                var lp = t.localPosition;
                sb.AppendLine($"  {new string(' ', d * 2)}{t.name} " +
                              $"({lp.x:0.000}, {lp.y:0.000}, {lp.z:0.000})");
                foreach (Transform c in t) Walk(c, d + 1);
            }
        }
    }
}
