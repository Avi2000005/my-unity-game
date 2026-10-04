using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools;

public static class OpenLevelScene
{
	private const string ScenePath = "Assets/Scenes/SampleScene.unity";

	private const string Report = "Temp/open_scene.txt";

	[MenuItem("Tools/Echoes/Open Level 1 Scene", priority = 99)]
	public static void Run()
	{
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[Echoes] open level 1 scene");
		stringBuilder.AppendLine("  path        : Assets/Scenes/SampleScene.unity");
		if (!File.Exists("Assets/Scenes/SampleScene.unity"))
		{
			stringBuilder.AppendLine("  FATAL: that file is not on disk. Every build tool writes to it, so this is a wrong path, not a lost scene.");
			Finish(stringBuilder);
			return;
		}
		stringBuilder.AppendLine("  size        : " + new FileInfo("Assets/Scenes/SampleScene.unity").Length.ToString("N0") + " bytes");
		stringBuilder.AppendLine("  modified    : " + File.GetLastWriteTime("Assets/Scenes/SampleScene.unity").ToString("yyyy-MM-dd HH:mm:ss"));
		Scene activeScene = SceneManager.GetActiveScene();
		stringBuilder.AppendLine("  was open    : '" + activeScene.name + "' valid=" + activeScene.IsValid() + " dirty=" + activeScene.isDirty + (activeScene.IsValid() ? (" roots=" + activeScene.rootCount) : ""));
		stringBuilder.AppendLine("  was         : " + ((activeScene.IsValid() && activeScene.rootCount > 0) ? "a real scene with content" : "EMPTY — this is why nothing was visible. Opening the project lands on a blank untitled scene; the level is on disk."));
		if (EditorApplication.isPlaying)
		{
			stringBuilder.AppendLine("  refused: cannot change scenes in play mode");
			Finish(stringBuilder);
			return;
		}
		Scene val = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", (OpenSceneMode)0);
		stringBuilder.AppendLine("  opened      : '" + val.name + "' valid=" + val.IsValid() + " loaded=" + val.isLoaded + " roots=" + val.rootCount + " dirty=" + val.isDirty);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("roots in the scene:");
		GameObject[] rootGameObjects = val.GetRootGameObjects();
		foreach (GameObject val2 in rootGameObjects)
		{
			stringBuilder.AppendLine("  " + ((Object)val2).name.PadRight(26) + " children=" + val2.transform.childCount.ToString().PadLeft(5) + " renderers=" + val2.GetComponentsInChildren<Renderer>(true).Length.ToString().PadLeft(6) + " active=" + val2.activeSelf);
		}
		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
		stringBuilder.AppendLine();
		if ((Object)(object)ariMover == (Object)null)
		{
			stringBuilder.AppendLine("  Ari: NOT IN THE SCENE — if she is not there, nothing will respond to movement.");
		}
		else
		{
			string[] array = new string[8] { "  Ari at ", null, null, null, null, null, null, null };
			Vector3 position = ((Component)ariMover).transform.position;
			array[1] = position.ToString("F2");
			array[2] = ", bodyHeight ";
			array[3] = ariMover.BodyHeight.ToString("F2");
			array[4] = ", AriHealth ";
			array[5] = ((Object)(object)((Component)ariMover).GetComponent<AriHealth>() != (Object)null).ToString();
			array[6] = ", AriInteract ";
			array[7] = ((Object)(object)((Component)ariMover).GetComponent<AriInteract>() != (Object)null).ToString();
			stringBuilder.AppendLine(string.Concat(array));
		}
		Camera[] array2 = Object.FindObjectsByType<Camera>((FindObjectsInactive)1);
		stringBuilder.AppendLine("  cameras: " + array2.Length);
		Camera[] array3 = array2;
		foreach (Camera val3 in array3)
		{
			if (((Behaviour)val3).enabled)
			{
				stringBuilder.AppendLine("    '" + ((Object)val3).name + "' active, far clip " + val3.farClipPlane.ToString("F0"));
			}
		}
		Finish(stringBuilder);
	}

	private static void Finish(StringBuilder sb)
	{
		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/open_scene.txt"), sb.ToString());
		Debug.Log((object)"[Echoes] scene opened — see Temp/open_scene.txt");
	}
}
