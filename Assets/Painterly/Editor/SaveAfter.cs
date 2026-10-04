using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class SaveAfter
    {
    	public static string Save(string what)
    	{
    		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
    		if (EditorApplication.isPlaying)
    		{
    			return "  NOT SAVED — in play mode. Scene changes in play mode are discarded by Unity on stop, and calling MarkSceneDirty here throws.";
    		}
    		Scene activeScene = SceneManager.GetActiveScene();
    		if (!activeScene.IsValid() || activeScene.rootCount == 0)
    		{
    			return "  NOT SAVED — no scene is open. Open the level first; otherwise the changes have nowhere to go.";
    		}
    		EditorSceneManager.MarkSceneDirty(activeScene);
    		string path = activeScene.path;
    		if (string.IsNullOrEmpty(path))
    		{
    			return "  NOT SAVED — the open scene has never been saved, so it has no path. This is what 'Untitled' in the window title means.";
    		}
    		if (!EditorSceneManager.SaveScene(activeScene))
    		{
    			return "  SAVE FAILED — the editor refused to write " + path;
    		}
    		FileInfo fileInfo = new FileInfo(path);
    		return "  saved: " + what + " -> " + Path.GetFileName(path) + " (" + fileInfo.Length.ToString("N0") + " bytes, " + fileInfo.LastWriteTime.ToString("HH:mm:ss") + ")";
    	}
    }
}