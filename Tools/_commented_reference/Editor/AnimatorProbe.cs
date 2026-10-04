using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Reports why Ari animates as a sliding statue instead of a walking one.
    ///
    /// A character that translates without any limb motion almost always has an
    /// Animator whose avatar does not describe the mesh actually in the scene.
    /// Unity will happily move the root transform via the controller's Speed
    /// parameter while every bone lookup silently fails, so the only way to tell
    /// the difference is to compare the avatar Unity built against the transforms
    /// the skinned mesh is really bound to.
    /// </summary>
    public static class AnimatorProbe
    {
        const string Report = "Temp/animator_probe.txt";

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] Animator probe");
            sb.AppendLine();

            var animators = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include);
            sb.AppendLine($"Animator components in scene: {animators.Length}");

            if (animators.Length == 0)
            {
                sb.AppendLine("  NONE - nothing is animating at all");
                Write(sb);
                return;
            }

            foreach (var a in animators) ReportAnimator(a, sb);

            sb.AppendLine();
            sb.AppendLine("=== SkinnedMeshRenderers ===");
            var skins = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include);
            sb.AppendLine($"count: {skins.Length}");

            foreach (var s in skins)
            {
                if (s == null) continue;
                var bones = s.bones;
                sb.AppendLine($"  '{s.name}'  bones={bones.Length}  rootBone={(s.rootBone ? s.rootBone.name : "NULL")}");

                if (bones.Length == 0) { sb.AppendLine("    NO BONES BOUND - mesh cannot be animated"); continue; }

                // The first animator is the one the controller drives; check the
                // mesh is actually under it, because an avatar rooted elsewhere
                // resolves every bone to null and the mesh just slides.
                var a0 = animators.Length > 0 ? animators[0] : null;
                if (a0 != null)
                {
                    bool under = bones[0].IsChildOf(a0.transform) || bones[0] == a0.transform;
                    sb.AppendLine($"    bone[0]='{bones[0].name}' under Animator '{a0.name}': {under}");
                }

                // PrefabInstance-style rigs from Mixamo keep the mesh under
                // mixamorig:Hips, so naming the first few is often enough to see
                // a prefix mismatch between the avatar and the scene.
                var names = new StringBuilder();
                for (int i = 0; i < Mathf.Min(6, bones.Length); i++)
                {
                    if (i > 0) names.Append(", ");
                    names.Append(bones[i].name);
                }
                sb.AppendLine($"    first bones: {names}");
            }

            sb.AppendLine();
            sb.AppendLine("=== AnimationClips in project ===");
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip"))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!p.Contains("/Ari/")) continue;

                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(p))
                {
                    if (!(o is AnimationClip c) || c == null) continue;
                    // Preview clips are generated per model and would double the list.
                    if (!AssetDatabase.Contains(c)) continue;
                    sb.AppendLine($"  {System.IO.Path.GetFileName(p),-20} '{c.name}' {c.length:F2}s " +
                                  $"legacy={c.legacy} human={c.isHumanMotion} loop={c.isLooping}");
                }
            }

            Write(sb);
        }

        static void ReportAnimator(Animator a, StringBuilder sb)
        {
            sb.AppendLine($"--- Animator on '{a.name}'  path={Path(a.transform)} ---");
            sb.AppendLine($"  enabled          : {a.enabled}");
            sb.AppendLine($"  activeInHierarchy: {a.gameObject.activeInHierarchy}");

            var av = a.avatar;
            if (av == null)
            {
                sb.AppendLine("  avatar           : NULL  <-- nothing to retarget onto, mesh will not animate");
            }
            else
            {
                sb.AppendLine($"  avatar           : valid={av.isValid} human={av.isHuman} " +
                              $"name='{av.name}' src='{AssetDatabase.GetAssetPath(av)}'");
            }

            var ctrl = a.runtimeAnimatorController;
            if (ctrl == null)
            {
                sb.AppendLine("  controller       : NULL  <-- no states, nothing will play");
            }
            else
            {
                sb.AppendLine($"  controller       : '{ctrl.name}' ({AssetDatabase.GetAssetPath(ctrl)})");
                foreach (var layer in ctrl.animationClips)
                    sb.AppendLine($"    clip: '{layer.name}' {layer.length:F2}s human={layer.isHumanMotion}");
            }

            sb.AppendLine($"  applyRootMotion   : {a.applyRootMotion}");
            sb.AppendLine($"  updateMode        : {a.updateMode}");
            sb.AppendLine($"  cullingMode       : {a.cullingMode}  " +
                          $"({(a.cullingMode == AnimatorCullingMode.CullUpdateTransforms ? "OFFSCREEN = invisible = frozen" : "always simulated")})");
            sb.AppendLine($"  speed             : {a.speed:F2}");

            // An Animator on a parent of the mesh, or a second Animator on the
            // mesh itself, both produce a sliding result: one wins the update
            // and the other never gets a chance to pose anything. A *disabled*
            // duplicate is inert, so counting it as a conflict would report a
            // problem that was already solved.
            var liveChildren = a.GetComponentsInChildren<Animator>(true).Count(x => x.enabled);
            if (liveChildren > 1)
                sb.AppendLine($"  WARNING: {liveChildren} enabled Animators under this one - they will fight");

            var parentAnimator = a.GetComponentInParent<Animator>();
            if (parentAnimator != null && parentAnimator != a && parentAnimator.enabled)
                sb.AppendLine($"  WARNING: enabled parent '{parentAnimator.name}' also has an Animator");
        }

        static string Path(Transform t)
        {
            if (t == null) return "<none>";
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        static void Write(StringBuilder sb)
        {
            Directory.CreateDirectory(Path2(Report));
            File.WriteAllText(Report, sb.ToString());
            Debug.Log(sb.ToString());
        }

        static string Path2(string p) => System.IO.Path.GetDirectoryName(p);
    }
}
