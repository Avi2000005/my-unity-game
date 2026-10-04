using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class ScreenControlsSetup
    {
    	private const string ObjectName = "ScreenControls";

    	[MenuItem("Tools/Echoes/Add Screen Controls", priority = 65)]
    	public static void Run()
    	{
    		try
    		{
    			RunInner();
    		}
    		catch (Exception ex)
    		{
    			File.WriteAllText("Temp/screen_controls_error.txt", ex.ToString());
    			Debug.LogError((object)("[Echoes] screen controls failed\n" + ex));
    		}
    	}

    	private static void RunInner()
    	{
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Expected Obj, but got Unknown
    		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0087: Expected Obj, but got Unknown
    		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
    		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			Debug.LogError((object)"[Echoes] stop play mode first; the canvas would be discarded on exit.");
    			return;
    		}
    		StringBuilder stringBuilder = new StringBuilder();
    		Scene activeScene = SceneManager.GetActiveScene();
    		GameObject val = GameObject.Find("ScreenControls");
    		if ((Object)(object)val != (Object)null)
    		{
    			Object.DestroyImmediate((Object)(object)val);
    			stringBuilder.AppendLine("removed the existing ScreenControls");
    		}
    		GameObject val2 = new GameObject("ScreenControls");
    		SceneManager.MoveGameObjectToScene(val2, activeScene);
    		val2.AddComponent<OnScreenControls>();
    		stringBuilder.AppendLine("created ScreenControls with OnScreenControls");
    		EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
    		if ((Object)(object)eventSystem == (Object)null)
    		{
    			GameObject val3 = new GameObject("EventSystem");
    			SceneManager.MoveGameObjectToScene(val3, activeScene);
    			val3.AddComponent<EventSystem>();
    			val3.AddComponent<InputSystemUIInputModule>();
    			stringBuilder.AppendLine("created EventSystem with InputSystemUIInputModule");
    		}
    		else
    		{
    			stringBuilder.AppendLine("EventSystem already present ('" + ((Object)eventSystem).name + "')");
    			StandaloneInputModule component = ((Component)eventSystem).GetComponent<StandaloneInputModule>();
    			if ((Object)(object)component != (Object)null)
    			{
    				Object.DestroyImmediate((Object)(object)component);
    				if ((Object)(object)((Component)eventSystem).GetComponent<InputSystemUIInputModule>() == (Object)null)
    				{
    					((Component)eventSystem).gameObject.AddComponent<InputSystemUIInputModule>();
    				}
    				stringBuilder.AppendLine("replaced StandaloneInputModule with InputSystemUIInputModule");
    			}
    		}
    		GameObject val4 = GameObject.Find("Ari");
    		if ((Object)(object)val4 == (Object)null)
    		{
    			stringBuilder.AppendLine("WARNING: no Ari in the scene, so nothing was pointed at the controls. Run Tools/Echoes/Place Ari.");
    		}
    		else
    		{
    			AriMover ariMover = val4.GetComponent<AriMover>();
    			if ((Object)(object)ariMover == (Object)null)
    			{
    				ariMover = val4.AddComponent<AriMover>();
    				stringBuilder.AppendLine("added AriMover to Ari");
    			}
    			ariMover.UseScreenControls = true;
    			stringBuilder.AppendLine("Ari's AriMover.UseScreenControls = true");
    		}
    		EditorSceneManager.MarkSceneDirty(activeScene);
    		AssetDatabase.SaveAssets();
    		string text = stringBuilder.ToString();
    		File.WriteAllText("Temp/screen_controls.txt", text);
    		Debug.Log((object)("[Echoes] Screen controls added\n" + text));
    	}
    }
}