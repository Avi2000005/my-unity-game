using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Puts the on-screen movement controls into the scene. Writes
    /// Temp/screen_controls.txt.
    ///
    /// The canvas is one empty GameObject with a single component on it — the
    /// component builds the rest in Awake. Authoring the pad as a prefab would
    /// mean a binary asset holding five buttons, four labels and a font
    /// reference, none of which can be reviewed in a diff. Four rectangles do not
    /// justify that.
    ///
    /// Ari's AriMover is switched to read from the buttons at the same time, so
    /// the two cannot be set up in one order and work and in the other order sit
    /// there inert — which is the failure that looks like "the buttons don't do
    /// anything".
    /// </summary>
    public static class ScreenControlsSetup
    {
        const string ObjectName = "ScreenControls";

        [MenuItem("Tools/Echoes/Add Screen Controls", priority = 65)]
        public static void Run()
        {
            try
            {
                RunInner();
            }
            catch (System.Exception e)
            {
                File.WriteAllText("Temp/screen_controls_error.txt", e.ToString());
                Debug.LogError("[Echoes] screen controls failed\n" + e);
            }
        }

        static void RunInner()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Echoes] stop play mode first; the canvas would be " +
                               "discarded on exit.");
                return;
            }

            var sb = new StringBuilder();
            var scene = SceneManager.GetActiveScene();

            // ---- the canvas ----
            var existing = GameObject.Find(ObjectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
                sb.AppendLine("removed the existing ScreenControls");
            }

            var go = new GameObject(ObjectName);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<Echoes.Painterly.OnScreenControls>();
            sb.AppendLine("created ScreenControls with OnScreenControls");

            // ---- an EventSystem, if the scene has none ----
            // Unity's built-in "create EventSystem" adds the legacy
            // StandaloneInputModule, which is inert while the Input System
            // package owns input, so the type is specified rather than left to
            // whatever the menu item picks.
            var existingES = Object.FindAnyObjectByType<EventSystem>();
            if (existingES == null)
            {
                var es = new GameObject("EventSystem");
                SceneManager.MoveGameObjectToScene(es, scene);
                es.AddComponent<EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                sb.AppendLine("created EventSystem with InputSystemUIInputModule");
            }
            else
            {
                sb.AppendLine($"EventSystem already present ('{existingES.name}')");

                var legacy = existingES.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (legacy != null)
                {
                    // The legacy module and the Input System module cannot both
                    // drive the same EventSystem; the legacy one wins and the
                    // buttons never receive a press.
                    Object.DestroyImmediate(legacy);

                    // Added via the GameObject, not the component: EventSystem is
                    // not a GameObject, so it has no AddComponent of its own.
                    if (existingES.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                        existingES.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

                    sb.AppendLine("replaced StandaloneInputModule with InputSystemUIInputModule");
                }
            }

            // ---- point Ari at the buttons ----
            var ari = GameObject.Find("Ari");
            if (ari == null)
            {
                sb.AppendLine("WARNING: no Ari in the scene, so nothing was pointed " +
                              "at the controls. Run Tools/Echoes/Place Ari.");
            }
            else
            {
                var mover = ari.GetComponent<Echoes.Painterly.AriMover>();
                if (mover == null)
                {
                    mover = ari.AddComponent<Echoes.Painterly.AriMover>();
                    sb.AppendLine("added AriMover to Ari");
                }

                mover.UseScreenControls = true;
                sb.AppendLine("Ari's AriMover.UseScreenControls = true");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();

            var text = sb.ToString();
            File.WriteAllText("Temp/screen_controls.txt", text);
            Debug.Log("[Echoes] Screen controls added\n" + text);
        }
    }
}
