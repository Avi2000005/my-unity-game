using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class RoofShapeProbe
    {
    	private const string OutPath = "Temp/roof_shapes.txt";

    	private const string ModelDir = "Assets/Art/Village/Models";

    	[MenuItem("Tools/Echoes/Probe Roof Shapes", priority = 50)]
    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("Roof cross-sections, measured from mesh vertices");
    		stringBuilder.AppendLine("(footprint width at 5 height bands, as a fraction of the base)");
    		stringBuilder.AppendLine();
    		List<string> list = new List<string>();
    		string[] array = AssetDatabase.FindAssets("t:Model", new string[1] { "Assets/Art/Village/Models" });
    		for (int i = 0; i < array.Length; i++)
    		{
    			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(array[i]));
    			if (fileNameWithoutExtension.StartsWith("Roof_"))
    			{
    				list.Add(fileNameWithoutExtension);
    			}
    		}
    		list.Sort();
    		int num = 0;
    		int num2 = 0;
    		int num3 = 0;
    		foreach (string item in list)
    		{
    			string text = Describe(item);
    			if (text != null)
    			{
    				stringBuilder.AppendLine(text);
    				if (text.Contains("GABLED"))
    				{
    					num++;
    				}
    				else if (text.Contains("HIPPED"))
    				{
    					num2++;
    				}
    				else if (text.Contains("FLAT"))
    				{
    					num3++;
    				}
    			}
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine($"summary: {num2} hipped, {num} gabled, {num3} flat " + $"(of {list.Count} roof models)");
    		File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/roof_shapes.txt"), stringBuilder.ToString());
    		AssetDatabase.Refresh();
    		Debug.Log((object)("[Echoes] Roof shapes written to Temp/roof_shapes.txt\n" + $"hipped={num2} gabled={num} flat={num3}"));
    	}

    	private static int CountBoundaryEdges(Mesh mesh)
    	{
    		int[] array;
    		try
    		{
    			int num = 0;
    			for (int i = 0; i < mesh.subMeshCount; i++)
    			{
    				num += mesh.GetIndices(i).Length;
    			}
    			array = new int[num];
    			int num2 = 0;
    			for (int j = 0; j < mesh.subMeshCount; j++)
    			{
    				int[] indices = mesh.GetIndices(j);
    				Array.Copy(indices, 0, array, num2, indices.Length);
    				num2 += indices.Length;
    			}
    		}
    		catch
    		{
    			return -1;
    		}
    		if (array.Length < 3)
    		{
    			return -1;
    		}
    		Dictionary<long, int> dictionary = new Dictionary<long, int>(array.Length / 3);
    		for (int k = 0; k < array.Length; k += 3)
    		{
    			for (int l = 0; l < 3; l++)
    			{
    				int a = array[k + l];
    				int b = array[k + (l + 1) % 3];
    				long key = EdgeKey(mesh, a, b, 0.0005f);
    				if (!dictionary.TryGetValue(key, out var value))
    				{
    					dictionary[key] = 1;
    				}
    				else
    				{
    					dictionary[key] = value + 1;
    				}
    			}
    		}
    		int num3 = 0;
    		foreach (KeyValuePair<long, int> item in dictionary)
    		{
    			if (item.Value == 1)
    			{
    				num3++;
    			}
    		}
    		return num3;
    	}

    	private static long EdgeKey(Mesh mesh, int a, int b, float weld)
    	{
    		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 val = mesh.vertices[a];
    		Vector3 val2 = mesh.vertices[b];
    		long num = (Quant(val.x, weld) << 42) ^ (Quant(val.y, weld) << 21) ^ Quant(val.z, weld);
    		long num2 = (Quant(val2.x, weld) << 42) ^ (Quant(val2.y, weld) << 21) ^ Quant(val2.z, weld);
    		long num3 = ((num < num2) ? num : num2);
    		long num4 = ((num < num2) ? num2 : num);
    		return (num3 * 1000003) ^ (num4 * 31);
    	}

    	private static long Quant(float v, float weld)
    	{
    		return (long)Mathf.Round(v / weld);
    	}

    	private static string Describe(string name)
    	{
    		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002d: Expected Obj, but got Unknown
    		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
    		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_035d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
    		GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Village/Models/" + name + ".fbx");
    		if ((Object)(object)val == (Object)null)
    		{
    			return null;
    		}
    		GameObject val2 = (GameObject)PrefabUtility.InstantiatePrefab((Object)(object)val);
    		((Object)val2).hideFlags = (HideFlags)61;
    		Renderer componentInChildren = val2.GetComponentInChildren<Renderer>();
    		if ((Object)(object)componentInChildren == (Object)null)
    		{
    			Object.DestroyImmediate((Object)(object)val2);
    			return null;
    		}
    		MeshFilter component = ((Component)componentInChildren).GetComponent<MeshFilter>();
    		Mesh val3 = (((Object)(object)component != (Object)null) ? component.sharedMesh : null);
    		Bounds bounds = componentInChildren.bounds;
    		string result;
    		if ((Object)(object)val3 == (Object)null || val3.vertexCount == 0)
    		{
    			result = "no readable mesh";
    		}
    		else
    		{
    			Vector3[] vertices = val3.vertices;
    			Matrix4x4 localToWorldMatrix = ((Component)component).transform.localToWorldMatrix;
    			float[] array = new float[5];
    			float[] array2 = new float[5];
    			float[] array3 = new float[5];
    			float[] array4 = new float[5];
    			int[] array5 = new int[5];
    			for (int i = 0; i < 5; i++)
    			{
    				array[i] = float.MaxValue;
    				array2[i] = float.MinValue;
    				array3[i] = float.MaxValue;
    				array4[i] = float.MinValue;
    			}
    			float y = bounds.min.y;
    			float num = Mathf.Max(bounds.max.y, bounds.min.y + 0.0001f);
    			for (int j = 0; j < vertices.Length; j++)
    			{
    				Vector3 val4 = localToWorldMatrix.MultiplyPoint3x4(vertices[j]);
    				int num2 = Mathf.Clamp((int)(Mathf.Clamp01((val4.y - y) / (num - y)) * 5f), 0, 4);
    				array5[num2]++;
    				if (val4.x < array[num2])
    				{
    					array[num2] = val4.x;
    				}
    				if (val4.x > array2[num2])
    				{
    					array2[num2] = val4.x;
    				}
    				if (val4.z < array3[num2])
    				{
    					array3[num2] = val4.z;
    				}
    				if (val4.z > array4[num2])
    				{
    					array4[num2] = val4.z;
    				}
    			}
    			int num3 = 0;
    			int num4 = 3;
    			float num5 = array2[num3] - array[num3];
    			float num6 = array4[num3] - array3[num3];
    			float num7 = ((array5[num4] > 0) ? (array2[num4] - array[num4]) : num5);
    			float num8 = ((array5[num4] > 0) ? (array4[num4] - array3[num4]) : num6);
    			float num9 = ((num5 > 0.001f) ? (num7 / num5) : 1f);
    			float num10 = ((num6 > 0.001f) ? (num8 / num6) : 1f);
    			bool flag = num9 < 0.85f;
    			bool flag2 = num10 < 0.85f;
    			string text;
    			if (flag & flag2)
    			{
    				text = "HIPPED";
    			}
    			else
    			{
    				text = ((flag | flag2) ? "GABLED" : "FLAT");
    			}
    			StringBuilder stringBuilder = new StringBuilder();
    			for (int k = 0; k < 5; k++)
    			{
    				if (array5[k] == 0)
    				{
    					stringBuilder.Append("  --  ");
    					continue;
    				}
    				float num11 = array2[k] - array[k];
    				float num12 = array4[k] - array3[k];
    				stringBuilder.Append($"{num11:0.00}x{num12:0.00} ");
    			}
    			int num13 = CountBoundaryEdges(val3);
    			string text2 = ((num13 == 0) ? "sealed" : $"{num13} open edges");
    			result = $"{text,-7} {name,-32} {bounds.size.x:0.00}x{bounds.size.y:0.00}x{bounds.size.z:0.00}  " + $"top/base X {num9:0.00} Z {num10:0.00}  {text2,-14}  bands: {stringBuilder}";
    		}
    		Object.DestroyImmediate((Object)(object)val2);
    		return result;
    	}
    }
}