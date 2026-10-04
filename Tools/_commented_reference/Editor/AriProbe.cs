using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Read-only survey of the three Ari FBXs. Writes Temp/ari_probe.txt.
    ///
    /// The question this answers is not "did the import work" — the import log
    /// already said it did. It is which of the clips in each file is the real
    /// animation, because Mixamo-via-Blender ships two identically named takes
    /// in every file and one of them is the rest pose. Picking the wrong one
    /// gives a character that refuses to move, and nothing in the inspector
    /// makes the difference obvious.
    /// </summary>
    public static class AriProbe
    {
        const string Dir = "Assets/Art/Ari/Models";

        [MenuItem("Tools/Echoes/Probe Ari", priority = 90)]
        public static void Run()
        {
            var sb = new StringBuilder();

            ProbeAvatar(sb);
            ProbeClips(sb, "Ari_character");
            ProbeClips(sb, "Idle");
            ProbeClips(sb, "Walking");
            ProbeMeshes(sb, "Ari_character");

            File.WriteAllText("Temp/ari_probe.txt", sb.ToString());
            Debug.Log("[Echoes] Ari probe -> Temp/ari_probe.txt\n" + sb);
        }

        /// <summary>
        /// Report the humanoid bone mapping bone by bone.
        ///
        /// Avatar.isHuman only says the mapper produced *a* human rig. It says
        /// nothing about whether the arms and legs actually got bound, and a
        /// half-mapped avatar still reports isHuman=true while the animation
        /// plays on the wrong skeleton. So the mapping is enumerated directly.
        /// </summary>
        static void ProbeAvatar(StringBuilder sb)
        {
            string path = $"{Dir}/Ari_character.fbx";
            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();

            sb.AppendLine("=== AVATAR ===");
            if (avatar == null) { sb.AppendLine("  none"); return; }

            sb.AppendLine($"  valid={avatar.isValid} human={avatar.isHuman}");

            var desc = avatar.humanDescription;
            var slots = desc.human;

            // HumanBodyBones is a dense 0..N enum and human[] is indexed by it,
            // but the array is not guaranteed to cover every slot, so each read is
            // bounds-checked rather than trusting the length.
            var mapped = new System.Collections.Generic.List<string>();
            var have = new System.Collections.Generic.HashSet<HumanBodyBones>();

            foreach (HumanBodyBones bone in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                int i = (int)bone;
                if (i < 0 || i >= slots.Length) continue;
                if (string.IsNullOrEmpty(slots[i].boneName)) continue;

                have.Add(bone);
                mapped.Add($"{bone}={slots[i].boneName}");
            }

            sb.AppendLine($"  {mapped.Count} bone(s) mapped across {slots.Length} slot(s):");
            foreach (var m in mapped.OrderBy(x => x)) sb.AppendLine("    " + m);

            var required = new[]
            {
                HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Head,
                HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
                HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot
            };

            var missing = required.Where(r => !have.Contains(r)).ToArray();
            sb.AppendLine(missing.Length == 0
                ? "  all core limbs mapped"
                : "  MISSING: " + string.Join(", ", missing));
            sb.AppendLine();
        }

        /// <summary>
        /// List every clip in one FBX next to the take names the importer saw.
        ///
        /// A clip Unity has not decided it needs is named after the take; the
        /// pairing is only trustworthy if both sides are printed together, so
        /// a clip can be traced back to the take it came from and renumbered.
        /// </summary>
        static void ProbeClips(StringBuilder sb, string model)
        {
            string path = $"{Dir}/{model}.fbx";
            sb.AppendLine($"=== CLIPS: {model} ===");

            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) { sb.AppendLine("  no importer\n"); return; }

            var takes = importer.defaultClipAnimations;
            sb.AppendLine($"  importer sees {takes.Length} take(s):");
            foreach (var t in takes)
            {
                sb.AppendLine($"    take='{t.takeName}' name='{t.name}' " +
                              $"frames {t.firstFrame}..{t.lastFrame} loop={t.loopTime}");
            }

            var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .OrderBy(c => c.length)
                .ToArray();

            sb.AppendLine($"  {clips.Length} clip asset(s):");
            foreach (var c in clips)
            {
                var paths = AnimationUtility.GetCurveBindings(c).Length;
                sb.AppendLine($"    '{c.name}' len={c.length:0.000}s @ {c.frameRate:0.##}fps " +
                              $"loop={c.isLooping} legacy={c.legacy} curves={paths} " +
                              $"empty={c.length < 0.001f}");
            }

            sb.AppendLine();
        }

        /// <summary>
        /// What does the character actually look like, and what textures does it
        /// bring with it? Needed before any PainterlyLit material can be built,
        /// because the shader needs the source albedo to desaturate.
        /// </summary>
        static void ProbeMeshes(StringBuilder sb, string model)
        {
            string path = $"{Dir}/{model}.fbx";
            sb.AppendLine($"=== MESH: {model} ===");

            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) { sb.AppendLine("  not found\n"); return; }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            sb.AppendLine($"  {renderers.Length} renderer(s)");

            foreach (var r in renderers)
            {
                sb.AppendLine($"    {r.GetType().Name} '{r.name}' " +
                              $"enabled={r.enabled} active={r.gameObject.activeInHierarchy}");

                var smr = r as SkinnedMeshRenderer;
                var mesh = smr != null ? smr.sharedMesh
                                      : r.GetComponent<MeshFilter>()?.sharedMesh;

                if (smr != null)
                {
                    sb.AppendLine($"      bones={smr.bones.Length} " +
                                  $"blendShapeCount={smr.sharedMesh?.blendShapeCount}");
                }

                if (mesh != null)
                    sb.AppendLine($"      mesh verts={mesh.vertexCount} tris={mesh.triangles.Length / 3}");

                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) { sb.AppendLine("      material: <null>"); continue; }
                    sb.AppendLine($"      material '{m.name}' shader={m.shader?.name}");
                    foreach (var tname in new[] { "_BaseMap", "_MainTex" })
                    {
                        var tex = m.HasProperty(tname) ? m.GetTexture(tname) : null;
                        if (tex != null)
                            sb.AppendLine($"        {tname} = {tex.name} ({tex.width}x{tex.height})");
                    }
                }
            }

            sb.AppendLine();
        }
    }
}
