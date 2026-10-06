using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    [InitializeOnLoad]
    public static class AriDeathAnimSetup
    {
        static AriDeathAnimSetup()
        {
            EditorApplication.delayCall += Setup;
        }

        [MenuItem("Tools/Echoes/Setup Ari Death Animation", priority = 72)]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            string controllerPath = "Assets/Art/Ari/Ari.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) return;

            var sm = controller.layers[0].stateMachine;
            bool hasDieParam = false;
            foreach (var p in controller.parameters)
            {
                if (p.name == "Die") { hasDieParam = true; break; }
            }
            if (!hasDieParam)
            {
                controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
            }

            AnimatorState deathState = null;
            foreach (var s in sm.states)
            {
                if (s.state.name == "Ari_Death") { deathState = s.state; break; }
            }

            if (deathState == null)
            {
                AnimationClip deathClip = null;
                var assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/L1/Ari_Death.fbx");
                foreach (var a in assets)
                {
                    if (a is AnimationClip clip && clip.name == "Ari_Death")
                    {
                        deathClip = clip;
                        break;
                    }
                }

                if (deathClip != null)
                {
                    deathState = sm.AddState("Ari_Death", new Vector3(300f, 550f, 0f));
                    deathState.motion = deathClip;

                    var anyTrans = sm.AddAnyStateTransition(deathState);
                    anyTrans.hasExitTime = false;
                    anyTrans.duration = 0.15f;
                    anyTrans.canTransitionToSelf = false;
                    anyTrans.AddCondition(AnimatorConditionMode.If, 0f, "Die");

                    EditorUtility.SetDirty(controller);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[Echoes] Ari_Death state and Die trigger successfully wired into Ari.controller!");
                }
                else
                {
                    Debug.LogWarning("[Echoes] Could not find Ari_Death clip in Assets/Art/L1/Ari_Death.fbx");
                }
            }
        }
    }
}
