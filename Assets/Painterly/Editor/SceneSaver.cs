using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class SceneSaver
    {
    	[MenuItem("Tools/Echoes/Save Scene", priority = 95)]
    	public static void Run()
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    		Scene activeScene = SceneManager.GetActiveScene();
    		if (!activeScene.IsValid() || !activeScene.isLoaded)
    		{
    			Debug.LogError((object)"[Echoes] no loaded scene to save");
    			return;
    		}
    		EditorSceneManager.MarkSceneDirty(activeScene);
    		bool flag = EditorSceneManager.SaveScene(activeScene);
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine($"scene '{activeScene.name}' saved={flag} path={activeScene.path}");
    		stringBuilder.AppendLine($"  dirty={activeScene.isDirty} rootCount={activeScene.rootCount}");
    		GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
    		foreach (GameObject val in rootGameObjects)
    		{
    			int num = val.GetComponentsInChildren<Renderer>(true).Length;
    			stringBuilder.AppendLine($"  {((Object)val).name,-20} children={val.transform.childCount} " + $"renderers={num} active={val.activeSelf}");
    		}
    		Debug.Log((object)("[Echoes] Scene saved\n" + stringBuilder));
    	}
    }
}