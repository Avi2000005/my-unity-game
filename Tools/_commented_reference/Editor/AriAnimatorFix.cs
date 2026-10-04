using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Gives Ari a working Animator.
    ///
    /// She was moving but never posing, which reads as a statue sliding across
    /// the square. The probe found the cause: both Animators in the hierarchy had
    /// a null controller, so the Speed parameter AriMover publishes went to a
    /// graph with no states and every bone stayed at bind pose. The root
    /// transform still moved because the movement script translates it directly,
    /// which is exactly why the failure looked like an animation problem rather
    /// than a missing reference.
    ///
    /// The controller goes on the Animator that sits on Ari's own root, not the
    /// one the FBX importer added to the model child. AriMover is declared with
    /// <c>RequireComponent(typeof(Animator))</c> and reads its Animator with
    /// <c>GetComponent</c>, so it can only publish Speed to the one on its own
    /// GameObject. Binding the controller to the child instead would leave the
    /// parameter being written to an Animator with no graph while the graph's
    /// Animator sat permanently at Speed 0 — idle forever, however fast she ran.
    /// </summary>
    public static class AriAnimatorFix
    {
        const string ControllerPath = "Assets/Art/Ari/Ari.controller";
        const string Report = "Temp/animator_fix.txt";

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] Ari animator fix");
            sb.AppendLine();

            // Scene writes are refused during play mode, and the change would be
            // thrown away on exit anyway. Say so plainly instead of throwing
            // from MarkSceneDirty several steps in, after the report has already
            // claimed success.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sb.AppendLine("STOPPED: editor is in play mode.");
                sb.AppendLine("  Exiting play mode discards this, so nothing was changed.");
                sb.AppendLine("  Press Ctrl+P to exit play mode, then run Tools/Echoes/Fix Ari Animator.");
                Finish(sb);
                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (controller == null)
            {
                sb.AppendLine($"FATAL: no controller asset at {ControllerPath} - run the builder first");
                Finish(sb);
                return;
            }
            sb.AppendLine($"controller: '{controller.name}' ({ControllerPath})");
            foreach (var c in controller.animationClips)
                sb.AppendLine($"  clip '{c.name}' {c.length:0.00}s human={c.isHumanMotion}");
            sb.AppendLine();

            var ari = FindAri();
            if (ari == null)
            {
                sb.AppendLine("FATAL: could not find Ari (no object with both Animator and AriMover)");
                Finish(sb);
                return;
            }
            sb.AppendLine($"Ari: {Path(ari.transform)}");

            // --- the Animator AriMover actually drives -------------------------
            var mine = ari.GetComponent<Animator>();
            if (mine == null) mine = ari.gameObject.AddComponent<Animator>();

            if (mine.avatar == null)
            {
                sb.AppendLine("  no avatar on Ari's Animator - copying from the model child");
                var child = ari.GetComponentInChildren<Animator>();
                if (child != null && child.avatar != null)
                {
                    mine.avatar = child.avatar;
                    sb.AppendLine($"    avatar <- '{child.avatar.name}' valid={mine.avatar.isValid} human={mine.avatar.isHuman}");
                }
            }
            else
            {
                sb.AppendLine($"  avatar: valid={mine.avatar.isValid} human={mine.avatar.isHuman} '{mine.avatar.name}'");
            }

            if (mine.runtimeAnimatorController != controller)
            {
                mine.runtimeAnimatorController = controller;
                sb.AppendLine("  controller ASSIGNED");
            }
            else
            {
                sb.AppendLine("  controller already correct");
            }

            // Root motion is off because movement is script-driven. Left on, the
            // clip's own displacement fights the mover and she skates or snaps
            // back depending on which one wrote last.
            if (mine.applyRootMotion) { mine.applyRootMotion = false; sb.AppendLine("  applyRootMotion -> false"); }

            // CullUpdateTransforms freezes an Animator that has no visible
            // renderer, which is exactly Ari's situation: the mesh belongs to the
            // child model, so the root Animator can be culled and freeze mid-stride.
            if (mine.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                mine.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                sb.AppendLine("  cullingMode -> AlwaysAnimate");
            }

            if (!mine.enabled) { mine.enabled = true; sb.AppendLine("  enabled -> true"); }
            mine.updateMode = AnimatorUpdateMode.Normal;

            // --- silence the duplicates ---------------------------------------
            // Every enabled Animator sharing this avatar writes the same bones
            // each frame. Whichever runs last wins, so two graphs with different
            // clip times produce a pose that is neither.
            foreach (var other in ari.GetComponentsInChildren<Animator>(true))
            {
                if (other == mine) continue;
                if (other.enabled)
                {
                    other.enabled = false;
                    sb.AppendLine($"  disabled duplicate Animator on '{other.name}' " +
                                  $"(was controller={(other.runtimeAnimatorController == null ? "null" : other.runtimeAnimatorController.name)})");
                }
            }

            EditorUtility.SetDirty(mine.gameObject);
            EditorSceneManager.MarkSceneDirty(ari.scene);
            EditorSceneManager.SaveScene(ari.scene);
            sb.AppendLine();
            sb.AppendLine($"scene saved: {ari.scene.path}");

            Finish(sb);
        }

        static GameObject FindAri()
        {
            // The Animator AriMover drives is on AriMover's own GameObject, so
            // requiring both components together finds the right root without
            // depending on a name that could be renamed later.
            return Object.FindObjectsByType<Animator>(FindObjectsInactive.Include)
                .Where(a => a.GetComponent<Echoes.Painterly.AriMover>() != null)
                .Select(a => a.gameObject)
                .FirstOrDefault();
        }

        static string Path(Transform t)
        {
            if (t == null) return "<none>";
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        static void Finish(StringBuilder sb)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Report));
            File.WriteAllText(Report, sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
