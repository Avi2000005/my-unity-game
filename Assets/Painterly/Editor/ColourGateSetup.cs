using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class ColourGateSetup
    {
    	private const string Report = "Temp/colour_gate.txt";

    	[MenuItem("Tools/Echoes/Wire Fountain and Colour Gate", priority = 60)]
    	public static void Run()
    	{
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0401: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] wire the fountain and the colour gate");
    		if (EditorApplication.isPlaying)
    		{
    			stringBuilder.AppendLine("  refused: in play mode");
    			Finish(stringBuilder);
    			return;
    		}
    		Scene activeScene = SceneManager.GetActiveScene();
    		if (!activeScene.IsValid() || activeScene.rootCount == 0)
    		{
    			stringBuilder.AppendLine("  FATAL: no scene open.");
    			Finish(stringBuilder);
    			return;
    		}
    		Transform val = Root("Fountain");
    		if ((Object)(object)val == (Object)null)
    		{
    			stringBuilder.AppendLine("  FATAL: no root called Fountain. Beat 7 has nothing to fix.");
    			Finish(stringBuilder);
    			return;
    		}
    		string[] array = new string[5] { "  fountain at ", null, null, null, null };
    		Vector3 position = val.position;
    		array[1] = position.ToString("F2");
    		array[2] = ", ";
    		array[3] = val.childCount.ToString();
    		array[4] = " children";
    		stringBuilder.AppendLine(string.Concat(array));
    		ColorRestoreTarget[] componentsInChildren = ((Component)val).GetComponentsInChildren<ColorRestoreTarget>(true);
    		stringBuilder.AppendLine("  ColorRestoreTargets under it: " + componentsInChildren.Length);
    		for (int i = 0; i < componentsInChildren.Length; i++)
    		{
    			string[] array2 = new string[7]
    			{
    				"    '",
    				((Object)componentsInChildren[i]).name,
    				"' at ",
    				null,
    				null,
    				null,
    				null
    			};
    			position = ((Component)componentsInChildren[i]).transform.position;
    			array2[3] = position.ToString("F2");
    			array2[4] = " restore ";
    			array2[5] = componentsInChildren[i].Restore.ToString("0.000");
    			array2[6] = (componentsInChildren[i].Exempt ? "  EXEMPT" : "");
    			stringBuilder.AppendLine(string.Concat(array2));
    		}
    		if (componentsInChildren.Length == 0)
    		{
    			stringBuilder.AppendLine("  FATAL: the fountain has no ColorRestoreTarget, so there is nothing for the water to change. Beat 7 would complete with a grey fountain and no warning.");
    			Finish(stringBuilder);
    			return;
    		}
    		ColorRestoreTarget colorRestoreTarget = componentsInChildren[0];
    		stringBuilder.AppendLine("  using as the water: '" + ((Object)colorRestoreTarget).name + "'  <- the FIRST ColorRestoreTarget under the fountain, chosen by position not by name, because a name that does not match leaves the beat unwired");
    		GameObject val2 = new GameObject("L1_ColourGate");
    		val2.transform.SetParent((activeScene.GetRootGameObjects().Length != 0) ? activeScene.GetRootGameObjects()[0].transform : null);
    		SetField(val2.AddComponent<ColourGate>(), "fountainWater", colorRestoreTarget);
    		colorRestoreTarget.GrantExemption();
    		EditorUtility.SetDirty((Object)(object)colorRestoreTarget);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("  + ColourGate on a new root 'L1_ColourGate' -> " + ((Object)colorRestoreTarget).name);
    		FountainFix fountainFix = ((Component)val).GetComponent<FountainFix>();
    		if ((Object)(object)fountainFix == (Object)null)
    		{
    			fountainFix = ((Component)val).gameObject.AddComponent<FountainFix>();
    		}
    		else
    		{
    			stringBuilder.AppendLine("  FountainFix already on the fountain, reusing");
    		}
    		SetField(fountainFix, "water", colorRestoreTarget);
    		MonoCompanion monoCompanion = MonoCompanion.FindInLevel();
    		if ((Object)(object)monoCompanion != (Object)null)
    		{
    			SetField(fountainFix, "mono", monoCompanion);
    		}
    		stringBuilder.AppendLine("  FountainFix -> water '" + ((Object)colorRestoreTarget).name + "'" + (((Object)(object)monoCompanion != (Object)null) ? (", mono '" + ((Object)monoCompanion).name + "'") : ", mono NOT FOUND"));
    		int num = 0;
    		int num2 = 0;
    		ColorRestoreTarget[] array3 = Object.FindObjectsByType<ColorRestoreTarget>((FindObjectsInactive)1);
    		for (int j = 0; j < array3.Length; j++)
    		{
    			if (!((Object)(object)array3[j] == (Object)null))
    			{
    				if (array3[j].Exempt)
    				{
    					num2++;
    				}
    				else
    				{
    					num++;
    				}
    			}
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("  village targets: " + (num + num2) + " total — " + num + " will be held grey, " + num2 + " exempt");
    		if (num2 != 1)
    		{
    			stringBuilder.AppendLine("  " + ((num2 == 0) ? "NOTHING is exempt. Beat 7 will ask the water for colour, the gate will refuse, and the level will complete grey while reporting success." : (num2 + " targets are exempt, so colour could arrive somewhere other than the fountain. The gate will not hold for the beat it exists to hold.")));
    		}
    		ColourFragment colourFragment = Object.FindAnyObjectByType<ColourFragment>((FindObjectsInactive)1);
    		stringBuilder.AppendLine();
    		string text;
    		if (!((Object)(object)colourFragment == (Object)null))
    		{
    			string[] array4 = new string[8]
    			{
    				"'",
    				((Object)colourFragment).name,
    				"' at ",
    				null,
    				null,
    				null,
    				null,
    				null
    			};
    			position = ((Component)colourFragment).transform.position;
    			array4[3] = position.ToString("F2");
    			array4[4] = ", showing ";
    			array4[5] = colourFragment.IsShowing.ToString();
    			array4[6] = ", taken ";
    			array4[7] = colourFragment.Taken.ToString();
    			text = string.Concat(array4);
    		}
    		else
    		{
    			text = "NOT PLACED — AriInteract will find nothing to press E on, so the fountain will refuse the fix and the level cannot be finished";
    		}
    		stringBuilder.AppendLine("  blue fragment: " + text);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine(SaveAfter.Save("colour gate + fountain fix"));
    		Finish(stringBuilder);
    	}

    	private static void SetField(object target, string field, object value)
    	{
    		FieldInfo field2 = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    		if (field2 == null)
    		{
    			Debug.LogError((object)("[Echoes] ColourGateSetup: " + target.GetType().Name + " has no field '" + field + "'. The beat is left unwired."));
    		}
    		else
    		{
    			field2.SetValue(target, value);
    		}
    	}

    	private static Transform Root(string n)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		Scene activeScene = SceneManager.GetActiveScene();
    		GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
    		for (int i = 0; i < rootGameObjects.Length; i++)
    		{
    			if (((Object)rootGameObjects[i]).name == n)
    			{
    				return rootGameObjects[i].transform;
    			}
    		}
    		return null;
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/colour_gate.txt"), sb.ToString());
    		Debug.Log((object)"[Echoes] colour gate wired — see Temp/colour_gate.txt");
    	}
    }
}