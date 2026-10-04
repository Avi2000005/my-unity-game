using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class GableProbe
    {
    	private const string OutPath = "Temp/gable_holes.txt";

    	private const string ModelDir = "Assets/Art/Village/Models";

    	[MenuItem("Tools/Echoes/Probe Gable Holes", priority = 51)]
    	public static void Run()
    	{
    		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0079: Expected Obj, but got Unknown
    		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f1: Expected Obj, but got Unknown
    		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
    		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
    		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
    		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
    		List<string> list = new List<string>();
    		string[] array = AssetDatabase.FindAssets("t:Model", new string[1] { "Assets/Art/Village/Models" });
    		for (int i = 0; i < array.Length; i++)
    		{
    			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(array[i]));
    			if (fileNameWithoutExtension.StartsWith("Roof_RoundTiles_") || fileNameWithoutExtension == "Roof_Tower_RoundTiles")
    			{
    				list.Add(fileNameWithoutExtension);
    			}
    		}
    		list.Sort();
    		GameObject val = new GameObject("__GableProbe");
    		((Object)val).hideFlags = (HideFlags)61;
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("Gable end closure, by raycast (Z = along the ridge, X = across)");
    		stringBuilder.AppendLine("'open' at a height means you can see straight through the building there.");
    		stringBuilder.AppendLine();
    		int num = 0;
    		foreach (string item in list)
    		{
    			GameObject val2 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Village/Models/" + item + ".fbx");
    			if ((Object)(object)val2 == (Object)null)
    			{
    				continue;
    			}
    			GameObject val3 = (GameObject)PrefabUtility.InstantiatePrefab((Object)(object)val2);
    			((Object)val3).hideFlags = (HideFlags)61;
    			val3.transform.SetParent(val.transform, true);
    			Renderer[] componentsInChildren = val3.GetComponentsInChildren<Renderer>();
    			if (componentsInChildren.Length == 0)
    			{
    				Object.DestroyImmediate((Object)(object)val3);
    				continue;
    			}
    			Renderer[] array2 = componentsInChildren;
    			foreach (Renderer val4 in array2)
    			{
    				if (!((Object)(object)((Component)val4).GetComponent<MeshCollider>() != (Object)null))
    				{
    					((Component)val4).gameObject.AddComponent<MeshCollider>().sharedMesh = (((Object)(object)((Component)val4).GetComponent<MeshFilter>() != (Object)null) ? ((Component)val4).GetComponent<MeshFilter>().sharedMesh : null);
    				}
    			}
    			Physics.SyncTransforms();
    			Bounds val5 = new Bounds(Vector3.zero, Vector3.zero);
    			array2 = componentsInChildren;
    			foreach (Renderer val6 in array2)
    			{
    				val5.Encapsulate(val6.bounds);
    			}
    			float num2 = val5.min.y + (val5.max.y - val5.min.y) * 0.15f;
    			float num3 = val5.min.y + (val5.max.y - val5.min.y) * 0.8f;
    			int num4 = 0;
    			int num5 = 0;
    			for (int j = 0; j < 9; j++)
    			{
    				float num6 = (float)j / 8f;
    				float num7 = Mathf.Lerp(num2, num3, num6);
    				Vector3 origin = new Vector3(val5.center.x, num7, val5.center.z);
    				if (!Blocked(origin, Vector3.forward, val5.extents.z * 2f + 4f))
    				{
    					num4++;
    				}
    				if (!Blocked(origin, Vector3.right, val5.extents.x * 2f + 4f))
    				{
    					num5++;
    				}
    			}
    			string arg = ((num4 > 0) ? $"SEE-THROUGH at {num4}/{9} heights" : "closed");
    			if (num4 > 0)
    			{
    				num++;
    			}
    			if (num5 > 0)
    			{
    				stringBuilder.AppendLine($"  (note: also open across X at {num5}/{9} — collider may be unreliable)");
    			}
    			stringBuilder.AppendLine($"{item,-26} {val5.size.x:0.0}x{val5.size.y:0.0}x{val5.size.z:0.0}  " + string.Format("gable Z: {0,-26} across X: {1}", arg, (num5 == 0) ? "blocked (control ok)" : "OPEN"));
    			Object.DestroyImmediate((Object)(object)val3);
    		}
    		Object.DestroyImmediate((Object)(object)val);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine($"{num} of {list.Count} round-tile roofs have an open gable end.");
    		File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/gable_holes.txt"), stringBuilder.ToString());
    		Debug.Log((object)string.Format("[Echoes] Gable probe written to {0}\n{1} of {2} roofs open at the gable.", "Temp/gable_holes.txt", num, list.Count));
    	}

    	private static bool Blocked(Vector3 origin, Vector3 dir, float span)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		return Physics.Raycast(origin + dir * (span * 0.5f + 0.05f), -dir, span, -1, (QueryTriggerInteraction)1);
    	}
    }
}