using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Puts Ari in the scene with her material, animator and controller, then
    /// proves the retarget works by measuring it. Writes Temp/ari_scene.txt.
    ///
    /// The check at the end is the point of this tool. Every asset can be
    /// present, correctly named, and wired up, and Ari can still stand in a
    /// T-pose with the animation playing perfectly on a skeleton that is not
    /// hers — which is exactly what a broken avatar produces, and it looks
    /// fine in a hierarchy and wrong on screen. So the walk clip is sampled
    /// across its length and the feet are measured. A retargeted walk cycle
    /// lifts a foot tens of centimetres; a failed one moves nothing.
    /// </summary>
    public static class AriSceneSetup
    {
        const string ModelsDir = "Assets/Art/Ari/Models";
        const string CharacterPath = ModelsDir + "/Ari_character.fbx";
        const string ControllerPath = "Assets/Art/Ari/Ari.controller";
        const string MaterialPath = "Assets/Painterly/Materials/Ari_Painterly.mat";
        const string ShaderName = "Echoes/PainterlyLit";
        const string RootName = "Ari";

        [MenuItem("Tools/Echoes/Place Ari", priority = 62)]
        public static void Run()
        {
            try
            {
                RunInner();
            }
            catch (System.Exception e)
            {
                // The MCP bridge reports a bare "Runtime Error" with no message,
                // so the exception is written out rather than lost.
                File.WriteAllText("Temp/ari_scene_error.txt", e.ToString());
                Debug.LogError("[Echoes] Place Ari failed\n" + e);
            }
        }

        static void RunInner()
        {
            var sb = new StringBuilder();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            if (model == null)
            {
                Debug.LogError("[Echoes] " + CharacterPath + " not found. Run Tools/Echoes/Import Ari.");
                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("[Echoes] " + ControllerPath +
                               " not found. Run Tools/Echoes/Build Ari Controller.");
                return;
            }

            var material = GetOrCreateMaterial(sb);
            var scene = GetScene();

            // The avatar is taken from the FBX rather than trusted from whatever
            // the instantiated model happens to carry, so the Animator is bound
            // to a known-good avatar instead of an inherited guess.
            var avatar = AssetDatabase.LoadAllAssetsAtPath(CharacterPath)
                                   .OfType<Avatar>()
                                   .FirstOrDefault(a => a.isValid && a.isHuman);

            if (avatar == null)
            {
                Debug.LogError("[Echoes] no valid humanoid avatar in " + CharacterPath +
                               ". Run Tools/Echoes/Import Ari.");
                return;
            }

            // ---- the object ----
            // An Ari left over from a failed run is rebuilt rather than reused:
            // such an object can carry an Animator with no avatar bound, which
            // silently plays nothing while looking correct in the hierarchy.
            var existing = GameObject.Find(RootName);
            if (existing != null)
            {
                var stale = existing.GetComponent<Animator>();
                bool broken = stale == null ||
                              stale.avatar == null ||
                              !stale.avatar.isValid ||
                              !stale.avatar.isHuman;

                if (broken)
                {
                    Object.DestroyImmediate(existing);
                    existing = null;
                    sb.AppendLine("discarded a broken Ari left by an earlier run");
                }
            }

            GameObject root;
            if (existing != null)
            {
                root = existing;
                sb.AppendLine("reusing the existing Ari object");
            }
            else
            {
                root = new GameObject(RootName);
                sb.AppendLine("created Ari");
            }

            if (root.scene != scene) SceneManager.MoveGameObjectToScene(root, scene);

            // ---- the model under it ----
            var child = root.transform.Find("Model");
            GameObject modelGO;
            if (child != null)
            {
                modelGO = child.gameObject;
            }
            else
            {
                modelGO = Object.Instantiate(model, root.transform);
                modelGO.name = "Model";
            }

            // The FBX root carries a corrective rotation and a 100x scale on the
            // village kit; Mixamo's is clean, but the local values are restated
            // so a re-import cannot silently leave the model lying on its side.
            modelGO.transform.localPosition = Vector3.zero;
            modelGO.transform.localRotation = model.transform.localRotation;
            modelGO.transform.localScale = model.transform.localScale;

            // ---- renderers ----
            var renderers = modelGO.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                r.sharedMaterials = mats;
            }
            sb.AppendLine($"{renderers.Length} renderer(s) -> {material.name}");

            // ---- animator ----
            // The imported model already carries an Animator with the avatar
            // bound, so that one is preferred over adding a second. Written as
            // an explicit test rather than `??`: Unity's Object overloads == to
            // catch destroyed components, and ?? bypasses that entirely, which
            // leaves a wrapper with no native component behind it.
            var animator = root.GetComponent<Animator>() ?? modelGO.GetComponent<Animator>();
            if (animator == null) animator = root.AddComponent<Animator>();

            if (animator == null)
            {
                Debug.LogError("[Echoes] could not obtain an Animator for Ari");
                return;
            }

            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            // Ari is the player and the movement script does not exist yet, so
            // the controller is left switched off rather than animating a
            // character nobody is steering.
            animator.enabled = false;

            sb.AppendLine($"animator on '{animator.name}': controller={controller.name} " +
                          $"avatar={(animator.avatar == null ? "<none>" : animator.avatar.name)} " +
                          $"valid={animator.avatar != null && animator.avatar.isValid} " +
                          $"human={animator.avatar != null && animator.avatar.isHuman} " +
                          "enabled=false (the movement script will turn this on)");

            // ---- where she stands ----
            if (root.transform.position == Vector3.zero)
            {
                root.transform.position = new Vector3(0f, 0.05f, 0f);
                root.transform.rotation = Quaternion.identity;
            }
            sb.AppendLine($"position {root.transform.position}");

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();

            VerifyRetarget(sb, modelGO, animator.avatar);
            VerifyMaterial(sb, material);

            File.WriteAllText("Temp/ari_scene.txt", sb.ToString());
            Debug.Log("[Echoes] Ari placed\n" + sb);
        }

        /// <summary>
        /// Sample the walk cycle and measure how far the feet actually travel.
        ///
        /// Sampled on the scene instance rather than the asset, so what is
        /// measured is the actual hierarchy the Animator will drive, avatar
        /// included. A retarget that failed leaves the bones welded to the bind
        /// pose and the travel collapses to zero, which is a result, not a
        /// silent pass.
        /// </summary>
        static void VerifyRetarget(StringBuilder sb, GameObject modelGO, Avatar avatar)
        {
            sb.AppendLine("--- retarget check (sampling the walk cycle) ---");

            // A folder path has to be searched, not handed to LoadAllAssetsAtPath,
            // which only accepts a single asset.
            var clip = AssetDatabase.FindAssets("t:AnimationClip", new[] { ModelsDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => c.name == "Ari_Walk");

            if (clip == null)
            {
                var present = AssetDatabase.FindAssets("t:AnimationClip", new[] { ModelsDir })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Distinct()
                    .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                    .OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__"))
                    .Select(c => $"'{c.name}'");

                sb.AppendLine("  Ari_Walk missing; clips present: " +
                              string.Join(", ", present));
                return;
            }

            var feet = new[] { "LeftFoot", "RightFoot" }
                .Select(n => FindBone(modelGO, n))
                .ToArray();

            if (feet.Any(f => f == null))
            {
                sb.AppendLine("  foot bones not found: " +
                    string.Join(", ", new[] { "LeftFoot", "RightFoot" }
                        .Where(n => FindBone(modelGO, n) == null)));
                return;
            }

            const int steps = 24;

            // Sample the whole cycle and track how high each foot gets above
            // its own lowest point. Bone world position is the honest measure —
            // the skin is a consequence of it.
            var lowest = new float[feet.Length];
            float lift = 0f;

            for (int i = 0; i <= steps; i++)
            {
                float t = clip.length * i / steps;
                clip.SampleAnimation(modelGO, t);

                for (int f = 0; f < feet.Length; f++)
                {
                    var y = feet[f].position.y;
                    if (i == 0 || y < lowest[f]) lowest[f] = y;
                    lift = Mathf.Max(lift, y);
                }
            }

            var travel = feet.Select((f, i) => lift - lowest[i]).ToArray();
            sb.AppendLine($"  clip '{clip.name}' len={clip.length:0.000}s over {steps} samples");
            for (int i = 0; i < feet.Length; i++)
                sb.AppendLine($"  {feet[i].name,-10} rises {travel[i]:0.0000} " +
                              $"(lowest y {lowest[i]:0.0000}, highest {lift:0.0000})");

            float best = travel.Max();
            string verdict = best > 0.05f
                ? $"RETARGET OK — a walk cycle lifts a foot {best:0.000}"
                : best > 0.0005f
                    ? $"WEAK — foot only moves {best:0.0005}, expect a glide rather than a walk"
                    : $"FAILED — feet do not move at all; avatar={avatar?.name} " +
                      $"valid={avatar?.isValid} human={avatar?.isHuman}";

            sb.AppendLine("  => " + verdict);

            clip.SampleAnimation(modelGO, 0f);
        }

        static Transform FindBone(GameObject go, string name)
        {
            var all = go.GetComponentsInChildren<Transform>(true);
            return all.FirstOrDefault(t => t.name == name);
        }

        /// <summary>
        /// Report what the material will actually look like.
        ///
        /// Ari arrived with no texture and no vertex colours — measured, not
        /// assumed: every texture slot is empty and the mesh has no colour
        /// channel. That matters more than it sounds, because PainterlyLit
        /// works by desaturating the albedo, and a grey albedo stays grey at
        /// _ColorRestore = 1. So the restore value cannot make her coloured on
        /// its own; only a texture can. Said plainly here so a flat grey Ari is
        /// not later mistaken for the shader failing.
        /// </summary>
        static void VerifyMaterial(StringBuilder sb, Material material)
        {
            sb.AppendLine("--- material check ---");
            sb.AppendLine($"  {material.name} shader={material.shader?.name}");

            var baseMap = material.GetTexture("_BaseMap");
            sb.AppendLine($"  _BaseMap={(baseMap == null ? "<none>" : baseMap.name)} " +
                          $"_BaseColor={material.GetColor("_BaseColor")} " +
                          $"_ColorRestore={material.GetFloat("_ColorRestore")}");

            if (baseMap == null)
                sb.AppendLine("  WARNING: no albedo texture and the mesh has no vertex " +
                              "colours, so Ari renders flat. _ColorRestore cannot help — " +
                              "the shader desaturates an albedo that is not there. " +
                              "Re-download the character from Mixamo with textures " +
                              "included and re-run Import Ari.");
        }

        static Material GetOrCreateMaterial(StringBuilder sb)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("[Echoes] shader " + ShaderName +
                               " not found. Run Tools/Echoes/Build Village Materials.");
                return null;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Ari_Painterly" };
                AssetDatabase.CreateAsset(material, MaterialPath);
                sb.AppendLine("created " + MaterialPath);
            }
            else
            {
                material.shader = shader;
                sb.AppendLine("reusing " + MaterialPath);
            }

            material.SetColor("_BaseColor", new Color(0.72f, 0.68f, 0.64f, 1f));
            material.SetFloat("_Smoothness", 0.25f);
            material.SetFloat("_Metallic", 0f);

            // Ari is in colour, not greyed. She is the source of the colour in
            // this game, so her material is the one that stays restored. On an
            // untextured albedo this makes no visible difference, which is the
            // point made in VerifyMaterial.
            material.SetFloat("_ColorRestore", 1f);
            material.SetFloat("_RestoreBoost", 1f);

            EditorUtility.SetDirty(material);
            return material;
        }

        static Scene GetScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded) return scene;

            scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            Debug.Log("[Echoes] opened SampleScene");
            return scene;
        }
    }
}
