using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Saves the open scene and reports what is in it.
    ///
    /// Separate from the tools that build things, because every one of those
    /// leaves the scene dirty. Until it is written to disk none of it exists:
    /// a domain reload throws the whole hierarchy away and the work is gone,
    /// which looks exactly like the tool having done nothing.
    /// </summary>
    public static class SceneSaver
    {
        [MenuItem("Tools/Echoes/Save Scene", priority = 95)]
        public static void Run()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[Echoes] no loaded scene to save");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);

            var sb = new StringBuilder();
            sb.AppendLine($"scene '{scene.name}' saved={saved} path={scene.path}");
            sb.AppendLine($"  dirty={scene.isDirty} rootCount={scene.rootCount}");

            foreach (var root in scene.GetRootGameObjects())
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true).Length;
                sb.AppendLine($"  {root.name,-20} children={root.transform.childCount} " +
                              $"renderers={renderers} active={root.activeSelf}");
            }

            Debug.Log("[Echoes] Scene saved\n" + sb);
        }
    }
}
