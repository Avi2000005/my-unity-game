using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class SceneSaveProbe
    {
    	private const string Report = "Temp/scene_save.txt";

    	[MenuItem("Tools/Echoes/Save Scene And Verify Ari", priority = 63)]
    	public static void Run()
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		Scene activeScene = SceneManager.GetActiveScene();
    		stringBuilder.AppendLine("active scene: " + activeScene.name + "  path=" + activeScene.path + "  " + $"loaded={activeScene.isLoaded}  dirty={activeScene.isDirty}");
    		if (!activeScene.isLoaded)
    		{
    			stringBuilder.AppendLine("no scene is open. Open SampleScene and run this again.");
    			Finish(stringBuilder);
    			return;
    		}
    		GameObject val = GameObject.Find("Ari");
    		if ((Object)(object)val == (Object)null)
    		{
    			stringBuilder.AppendLine("no Ari in the scene.");
    			Finish(stringBuilder);
    			return;
    		}
    		AriMover component = val.GetComponent<AriMover>();
    		if ((Object)(object)component == (Object)null)
    		{
    			stringBuilder.AppendLine("Ari has no AriMover.");
    			Finish(stringBuilder);
    			return;
    		}
    		stringBuilder.AppendLine("\n--- in memory, before saving ---");
    		stringBuilder.AppendLine("  " + Describe(component));
    		EditorSceneManager.MarkSceneDirty(activeScene);
    		bool flag = EditorSceneManager.SaveScene(activeScene);
    		stringBuilder.AppendLine($"\n--- save ---\n  SaveScene returned {flag}; " + $"dirty now {activeScene.isDirty}");
    		string text = Path.Combine(Directory.GetCurrentDirectory(), activeScene.path.Replace('/', Path.DirectorySeparatorChar));
    		stringBuilder.AppendLine("  file: " + text);
    		stringBuilder.AppendLine($"  exists: {File.Exists(text)}, " + $"written {DateTime.Now:HH:mm:ss}");
    		if (File.Exists(text))
    		{
    			string text2 = File.ReadAllText(text);
    			string[] array = new string[6] { "collideWithWalls", "bodyRadius", "bodyHeight", "bodyBottomLift", "stepHeight", "wallMask" };
    			stringBuilder.AppendLine("\n--- on disk ---");
    			string[] array2 = array;
    			foreach (string key in array2)
    			{
    				string text3 = text2.Split('\n', StringSplitOptions.None).FirstOrDefault((string l) => l.TrimStart().StartsWith(key + ":"));
    				stringBuilder.AppendLine("  " + ((text3 == null) ? (key + ": NOT IN THE FILE — the level is relying on the C# default") : text3.Trim()));
    			}
    		}
    		Finish(stringBuilder);
    	}

    	private static string Describe(AriMover mover)
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0007: Expected Obj, but got Unknown
    		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ab: Invalid comparison between Unknown and I4
    		SerializedObject val = new SerializedObject((Object)(object)mover);
    		string[] array = new string[11]
    		{
    			"walkSpeed", "runSpeed", "jumpHeight", "gravity", "airControl", "collideWithWalls", "bodyRadius", "bodyHeight", "bodyBottomLift", "stepHeight",
    			"soleOffset"
    		};
    		StringBuilder stringBuilder = new StringBuilder();
    		string[] array2 = array;
    		foreach (string text in array2)
    		{
    			SerializedProperty val2 = val.FindProperty(text);
    			if (val2 == null)
    			{
    				stringBuilder.Append(text + "=<absent> ");
    			}
    			else
    			{
    				stringBuilder.Append(text + "=" + (((int)val2.propertyType == 2) ? val2.floatValue.ToString("0.###") : val2.boolValue.ToString()) + " ");
    			}
    		}
    		return stringBuilder.ToString().Trim();
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/scene_save.txt"), sb.ToString());
    		Debug.Log((object)("[Echoes] scene save\n" + sb));
    	}
    }
}