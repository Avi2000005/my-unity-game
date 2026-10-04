using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class KitSurvey
    {
    	private const string ModelDir = "Assets/Art/Village/Models";

    	private const string ReportPath = "Temp/kit_survey.txt";

    	[MenuItem("Tools/Echoes/Survey Kit", priority = 40)]
    	public static void Run()
    	{
    		string[] array = AssetDatabase.FindAssets("t:Model", new string[1] { "Assets/Art/Village/Models" });
    		List<string> list = new List<string>();
    		string[] array2 = array;
    		foreach (string text in array2)
    		{
    			list.Add(Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(text)));
    		}
    		list.Sort();
    		Dictionary<string, List<string[]>> dictionary = new Dictionary<string, List<string[]>>();
    		List<string[]> list2 = new List<string[]>();
    		int num = 0;
    		foreach (string item in list)
    		{
    			string[] array3 = Measure(item);
    			if (array3 == null)
    			{
    				num++;
    				continue;
    			}
    			list2.Add(array3);
    			Group(dictionary, item).Add(array3);
    		}
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("VILLAGE KIT SURVEY");
    		stringBuilder.AppendLine("models found: " + list.Count + "   measured: " + list2.Count + "   unmeasurable: " + num);
    		stringBuilder.AppendLine("columns: size(X,Y,Z) world units | offset from prefab root to mesh min corner");
    		stringBuilder.AppendLine();
    		foreach (string item2 in Sorted(dictionary.Keys))
    		{
    			stringBuilder.AppendLine("== " + item2 + " (" + dictionary[item2].Count + ") ==");
    			List<string[]> list3 = dictionary[item2];
    			list3.Sort((string[] a, string[] b) => string.CompareOrdinal(a[0], b[0]));
    			foreach (string[] item3 in list3)
    			{
    				stringBuilder.AppendLine("  " + item3[0].PadRight(34) + item3[1]);
    			}
    			stringBuilder.AppendLine();
    		}
    		stringBuilder.AppendLine("== module hints ==");
    		float[] array4 = new float[6] { 1f, 0.5f, 0.25f, 0.1f, 0.05f, 0.01f };
    		for (int i = 0; i < array4.Length; i++)
    		{
    			float num2 = array4[i];
    			int num3 = 0;
    			int num4 = 0;
    			foreach (string[] item4 in list2)
    			{
    				array2 = item4[1].Trim().Split('|', StringSplitOptions.None)[0].Replace("size(", "").Replace(")", "").Split(',', StringSplitOptions.None);
    				for (int num5 = 0; num5 < array2.Length; num5++)
    				{
    					if (float.TryParse(array2[num5], out var result) && result > 0.05f)
    					{
    						num4++;
    						if (Mathf.Abs(result / num2 - Mathf.Round(result / num2)) < 0.01f)
    						{
    							num3++;
    						}
    					}
    				}
    			}
    			stringBuilder.AppendLine("  step " + num2.ToString("0.###") + ": " + num3 + "/" + num4 + " (" + (100f * (float)num3 / (float)Mathf.Max(1, num4)).ToString("F0") + "%) of extents are whole multiples");
    		}
    		Directory.CreateDirectory(Path.GetDirectoryName("Temp/kit_survey.txt"));
    		File.WriteAllText("Temp/kit_survey.txt", stringBuilder.ToString());
    		Debug.Log((object)("[Echoes] Kit survey written to Temp/kit_survey.txt (" + list2.Count + " models, " + dictionary.Count + " groups)."));
    	}

    	private static List<string> Sorted(ICollection<string> keys)
    	{
    		List<string> list = new List<string>(keys);
    		list.Sort();
    		return list;
    	}

    	private static List<string[]> Group(Dictionary<string, List<string[]>> groups, string name)
    	{
    		string key = (name.Contains("_") ? name.Substring(0, name.IndexOf('_')) : "(misc)");
    		if (!groups.TryGetValue(key, out var value))
    		{
    			value = (groups[key] = new List<string[]>());
    		}
    		return value;
    	}

    	private static string[] Measure(string modelName)
    	{
    		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002d: Expected Obj, but got Unknown
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
    		GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Village/Models/" + modelName + ".fbx");
    		if ((Object)(object)val == (Object)null)
    		{
    			return null;
    		}
    		GameObject val2 = (GameObject)PrefabUtility.InstantiatePrefab((Object)(object)val);
    		((Object)val2).hideFlags = (HideFlags)61;
    		string[] result = null;
    		Renderer componentInChildren = val2.GetComponentInChildren<Renderer>();
    		if ((Object)(object)componentInChildren != (Object)null)
    		{
    			Bounds bounds = componentInChildren.bounds;
    			Vector3 val3 = bounds.min - val2.transform.position;
    			result = new string[2]
    			{
    				modelName,
    				$"size({bounds.size.x:F2},{bounds.size.y:F2},{bounds.size.z:F2}) | offset({val3.x:F2},{val3.y:F2},{val3.z:F2})"
    			};
    		}
    		Object.DestroyImmediate((Object)(object)val2);
    		return result;
    	}
    }
}