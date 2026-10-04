using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class InkApply
    {
    	private const string MatPath = "Assets/Painterly/Materials/Cast_InkCrawler.mat";

    	private const string Report = "Temp/ink_apply.txt";

    	[MenuItem("Tools/Echoes/Apply Ink Skin To Crawlers", priority = 41)]
    	public static void Run()
    	{
    		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_041f: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] apply ink skin to the crawlers that are real");
    		if (EditorApplication.isPlaying)
    		{
    			stringBuilder.AppendLine("  refused: in play mode");
    			Finish(stringBuilder);
    			return;
    		}
    		Material val = AssetDatabase.LoadAssetAtPath<Material>("Assets/Painterly/Materials/Cast_InkCrawler.mat");
    		if ((Object)(object)val == (Object)null)
    		{
    			stringBuilder.AppendLine("  FATAL: no material at Assets/Painterly/Materials/Cast_InkCrawler.mat");
    			Finish(stringBuilder);
    			return;
    		}
    		object obj;
    		if (!val.HasProperty("_BaseMap"))
    		{
    			obj = null;
    		}
    		else
    		{
    			Texture texture = val.GetTexture("_BaseMap");
    			obj = ((texture is Texture2D) ? texture : null);
    		}
    		Texture2D val2 = (Texture2D)obj;
    		stringBuilder.AppendLine("  target material : " + ((Object)val).name + " (Assets/Painterly/Materials/Cast_InkCrawler.mat)");
    		stringBuilder.AppendLine("  shader          : " + (((Object)(object)val.shader != (Object)null) ? ((Object)val.shader).name : "NULL"));
    		stringBuilder.AppendLine("  _BaseMap        : " + (((Object)(object)val2 == (Object)null) ? "NONE — run 'Build Ink Crawler Skin' first, or this will grey the crawler instead of giving it a skin" : (((Object)val2).name + " " + ((Texture)val2).width + "x" + ((Texture)val2).height)));
    		stringBuilder.AppendLine();
    		InkCrawler[] array = Object.FindObjectsByType<InkCrawler>((FindObjectsInactive)1);
    		stringBuilder.AppendLine("crawlers in the level: " + array.Length);
    		if (array.Length == 0)
    		{
    			stringBuilder.AppendLine("  FATAL: no InkCrawler in the scene, so there is nothing to put a skin on.");
    			Finish(stringBuilder);
    			return;
    		}
    		int num = 0;
    		int num2 = 0;
    		int num3 = 0;
    		foreach (InkCrawler inkCrawler in array)
    		{
    			SkinnedMeshRenderer[] componentsInChildren = ((Component)inkCrawler).GetComponentsInChildren<SkinnedMeshRenderer>(true);
    			stringBuilder.AppendLine();
    			string[] array2 = new string[7]
    			{
    				"-- ",
    				((Object)inkCrawler).name,
    				" at ",
    				null,
    				null,
    				null,
    				null
    			};
    			Vector3 position = ((Component)inkCrawler).transform.position;
    			array2[3] = position.ToString("F2");
    			array2[4] = "  (";
    			array2[5] = (((Component)inkCrawler).gameObject.activeInHierarchy ? "active" : "INACTIVE");
    			array2[6] = ")";
    			stringBuilder.AppendLine(string.Concat(array2));
    			if (componentsInChildren.Length == 0)
    			{
    				stringBuilder.AppendLine("   NO SkinnedMeshRenderer — a material cannot fix this. The creature is invisible.");
    				num3++;
    				continue;
    			}
    			foreach (SkinnedMeshRenderer val3 in componentsInChildren)
    			{
    				Material[] sharedMaterials = ((Renderer)val3).sharedMaterials;
    				stringBuilder.AppendLine("   skin '" + ((Object)val3).name + "' " + sharedMaterials.Length + " slot(s)");
    				for (int k = 0; k < sharedMaterials.Length; k++)
    				{
    					Material val4 = sharedMaterials[k];
    					string text = (((Object)(object)val4 == (Object)null) ? "NONE" : ((Object)val4).name);
    					string text2;
    					if ((Object)(object)val4 == (Object)null)
    					{
    						text2 = "?";
    					}
    					else
    					{
    						text2 = ((AssetDatabase.GetAssetPath((Object)(object)val4).Length > 0) ? AssetDatabase.GetAssetPath((Object)(object)val4) : "EMBEDDED IN THE MODEL");
    					}
    					string text3;
    					if ((Object)(object)val4 == (Object)null || !val4.HasProperty("_BaseMap"))
    					{
    						text3 = "n/a";
    					}
    					else
    					{
    						text3 = (((Object)(object)val4.GetTexture("_BaseMap") == (Object)null) ? "NONE" : "set");
    					}
    					stringBuilder.AppendLine("      slot " + k + ": " + text + "  _BaseMap " + text3 + "  <- " + text2);
    				}
    				bool flag = false;
    				for (int l = 0; l < sharedMaterials.Length; l++)
    				{
    					if ((Object)(object)sharedMaterials[l] != (Object)(object)val)
    					{
    						flag = true;
    						break;
    					}
    				}
    				if (!flag)
    				{
    					stringBuilder.AppendLine("      already on the project material — left alone");
    					num2++;
    					continue;
    				}
    				for (int m = 0; m < sharedMaterials.Length; m++)
    				{
    					sharedMaterials[m] = val;
    				}
    				((Renderer)val3).sharedMaterials = sharedMaterials;
    				EditorUtility.SetDirty((Object)(object)val3);
    				num++;
    				stringBuilder.AppendLine("      -> ALL SLOTS now use " + ((Object)val).name);
    			}
    		}
    		EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("=== READ BACK OFF THE RENDERERS ===");
    		int num4 = 0;
    		int num5 = 0;
    		InkCrawler[] array3 = Object.FindObjectsByType<InkCrawler>((FindObjectsInactive)1);
    		for (int n = 0; n < array3.Length; n++)
    		{
    			SkinnedMeshRenderer componentInChildren = ((Component)array3[n]).GetComponentInChildren<SkinnedMeshRenderer>(true);
    			if ((Object)(object)componentInChildren == (Object)null)
    			{
    				num5++;
    				continue;
    			}
    			Material[] sharedMaterials2 = ((Renderer)componentInChildren).sharedMaterials;
    			int num6 = 0;
    			for (int num7 = 0; num7 < sharedMaterials2.Length; num7++)
    			{
    				if ((Object)(object)sharedMaterials2[num7] == (Object)(object)val)
    				{
    					num6++;
    				}
    			}
    			bool flag2 = num6 == sharedMaterials2.Length && sharedMaterials2.Length != 0;
    			if (flag2)
    			{
    				num4++;
    			}
    			else
    			{
    				num5++;
    			}
    			stringBuilder.AppendLine("  " + ((Object)array3[n]).name.PadRight(22) + (flag2 ? "OK   " : "BAD  ") + num6 + "/" + sharedMaterials2.Length + " slot(s) on " + ((Object)val).name);
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("RESULT: " + num4 + " crawler(s) wearing the ink skin, " + num5 + " not");
    		stringBuilder.AppendLine("  repaired " + num + ", already correct " + num2 + ", no skin at all " + num3);
    		stringBuilder.AppendLine("  " + ((num4 == array3.Length) ? ("PASS — every crawler in the level renders from " + ((Object)val).name + ", which carries " + (((Object)(object)val2 == (Object)null) ? "NOTHING" : ((Object)val2).name)) : ("FAIL — " + num5 + " crawler(s) are not on it. Their skins are embedded in the FBX and the swap did not stick; reimport or assign by hand.")));
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine(SaveAfter.Save("crawler materials -> Cast_InkCrawler"));
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("  Still to confirm by eye: the skin is near-black with a luminance spread of 0.02, which will read as a subtle surface. If it looks like a flat silhouette on screen, the spread is too low and not the assignment.");
    		Finish(stringBuilder);
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/ink_apply.txt"), sb.ToString());
    		Debug.Log((object)"[Echoes] ink skin applied — see Temp/ink_apply.txt");
    	}
    }
}