using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class AriAssetProbe
    {
    	private const string Dir = "Assets/Art/Ari/Models";

    	[MenuItem("Tools/Echoes/Probe Ari Assets", priority = 93)]
    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		string[] array = new string[3] { "Ari_character", "Idle", "Walking" };
    		foreach (string model in array)
    		{
    			Probe(stringBuilder, model);
    		}
    		stringBuilder.AppendLine("=== files on disk ===");
    		foreach (string item in from x in Directory.GetFiles("Assets/Art/Ari/Models", "*", SearchOption.AllDirectories)
    			orderby x
    			select x)
    		{
    			FileInfo fileInfo = new FileInfo(item);
    			stringBuilder.AppendLine($"  {fileInfo.Name,-42} {fileInfo.Length / 1024} KB");
    		}
    		File.WriteAllText("Temp/ari_assets.txt", stringBuilder.ToString());
    		Debug.Log((object)("[Echoes] Ari asset probe -> Temp/ari_assets.txt\n" + stringBuilder));
    	}

    	private static void Probe(StringBuilder sb, string model)
    	{
    		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0496: Unknown result type (might be due to invalid IL or missing references)
    		//IL_049b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_049f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
    		string text = "Assets/Art/Ari/Models/" + model + ".fbx";
    		sb.AppendLine("=== " + model + " ===");
    		GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(text);
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("  NOT IMPORTED\n");
    			return;
    		}
    		foreach (IGrouping<string, Object> item in from a in AssetDatabase.LoadAllAssetsAtPath(text)
    			group a by ((object)a).GetType().Name into g
    			orderby g.Key
    			select g)
    		{
    			sb.AppendLine($"  {item.Count()}x {item.Key}");
    			foreach (Object item2 in item.Take(6))
    			{
    				sb.AppendLine("      " + item2.name);
    			}
    			if (item.Count() > 6)
    			{
    				sb.AppendLine($"      ... +{item.Count() - 6} more");
    			}
    		}
    		AssetImporter atPath = AssetImporter.GetAtPath(text);
    		ModelImporter val2 = (ModelImporter)(object)((atPath is ModelImporter) ? atPath : null);
    		string text2 = (((Object)(object)val2 == (Object)null) ? "n/a" : ((object)val2.materialImportMode/*cast due to constrained. prefix*/).ToString());
    		sb.AppendLine("  materialImportMode=" + text2);
    		Renderer[] componentsInChildren = val.GetComponentsInChildren<Renderer>(true);
    		foreach (Renderer val3 in componentsInChildren)
    		{
    			sb.AppendLine("  renderer " + ((object)val3).GetType().Name + " '" + ((Object)val3).name + "' " + $"enabled={val3.enabled}");
    			Material[] sharedMaterials = val3.sharedMaterials;
    			foreach (Material val4 in sharedMaterials)
    			{
    				if ((Object)(object)val4 == (Object)null)
    				{
    					sb.AppendLine("      material <null>");
    					continue;
    				}
    				string name = ((Object)val4).name;
    				Shader shader = val4.shader;
    				sb.AppendLine("      material '" + name + "' shader=" + ((shader != null) ? ((Object)shader).name : null));
    				string[] texturePropertyNames = val4.GetTexturePropertyNames();
    				foreach (string text3 in texturePropertyNames)
    				{
    					Texture texture = val4.GetTexture(text3);
    					sb.AppendLine("        " + text3 + " = " + (((Object)(object)texture == (Object)null) ? "<none>" : ($"{((Object)texture).name} {texture.width}x{texture.height} " + $"fmt={texture.graphicsFormat}")));
    				}
    				if (val4.HasProperty("_BaseColor"))
    				{
    					sb.AppendLine(string.Format("        _BaseColor = {0}", val4.GetColor("_BaseColor")));
    				}
    			}
    		}
    		SkinnedMeshRenderer componentInChildren = val.GetComponentInChildren<SkinnedMeshRenderer>(true);
    		object obj;
    		if (!((Object)(object)componentInChildren != (Object)null))
    		{
    			MeshFilter componentInChildren2 = val.GetComponentInChildren<MeshFilter>(true);
    			obj = ((componentInChildren2 != null) ? componentInChildren2.sharedMesh : null);
    		}
    		else
    		{
    			obj = componentInChildren.sharedMesh;
    		}
    		Mesh val5 = (Mesh)obj;
    		if ((Object)(object)val5 != (Object)null)
    		{
    			Color[] colors = val5.colors;
    			bool flag = false;
    			if (colors != null && colors.Length == val5.vertexCount)
    			{
    				Color[] array = colors;
    				foreach (Color val6 in array)
    				{
    					if (val6.r > 0.01f || val6.g > 0.01f || val6.b > 0.01f)
    					{
    						flag = true;
    						break;
    					}
    				}
    			}
    			sb.AppendLine($"  mesh '{((Object)val5).name}' verts={val5.vertexCount} " + $"tris={val5.triangles.Length / 3} " + $"uv0={val5.uv.Length} colors={colors?.Length ?? 0} " + $"coloursUsed={flag}");
    			Bounds bounds = val5.bounds;
    			sb.AppendLine($"  bounds {bounds.size} " + $"(FBX root scale {val.transform.localScale})");
    		}
    		sb.AppendLine();
    	}
    }
}