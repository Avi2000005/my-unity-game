using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Marks the scene dirty and saves it, and says what it did.
    ///
    /// <para><b>Exists because work was lost.</b> These tools originally marked
    /// the scene dirty and deliberately did not save, on the grounds that
    /// saving on every run makes every run a commit. That was the wrong call in
    /// this project: the MCP relay shuts the Editor down after 180 seconds of
    /// inactivity, and everything unsaved went with it — three placed crawlers
    /// and a material swap, gone, with the scene's last save still two days
    /// old.</para>
    ///
    /// <para>So every tool that changes the scene now saves it, and reports the
    /// path it wrote and the time. A tool that changes a scene and leaves it in
    /// memory only is a tool whose output is a suggestion.</para>
    ///
    /// <para>Still refuses in play mode, because saving there throws
    /// <c>MarkSceneDirty</c> and would corrupt the file.</para>
    /// </summary>
    public static class SaveAfter
    {
        /// <summary>
        /// Mark dirty, save, and return a line for the report.
        /// </summary>
        /// <param name="what">What was changed, for the log line.</param>
        public static string Save(string what)
        {
            if (EditorApplication.isPlaying)
                return "  NOT SAVED — in play mode. Scene changes in play mode " +
                       "are discarded by Unity on stop, and calling " +
                       "MarkSceneDirty here throws.";

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.rootCount == 0)
                return "  NOT SAVED — no scene is open. Open the level first; " +
                       "otherwise the changes have nowhere to go.";

            EditorSceneManager.MarkSceneDirty(scene);

            var path = scene.path;
            if (string.IsNullOrEmpty(path))
                return "  NOT SAVED — the open scene has never been saved, so it " +
                       "has no path. This is what 'Untitled' in the window title " +
                       "means.";

            bool ok = EditorSceneManager.SaveScene(scene);

            if (!ok)
                return "  SAVE FAILED — the editor refused to write " + path;

            var info = new FileInfo(path);
            return "  saved: " + what + " -> " + Path.GetFileName(path) +
                   " (" + info.Length.ToString("N0") + " bytes, " +
                   info.LastWriteTime.ToString("HH:mm:ss") + ")";
        }
    }
}