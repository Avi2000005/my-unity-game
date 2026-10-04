using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class TextureSaturationProbe
    {
    	private sealed class Stats
    	{
    		public Texture2D Texture;

    		public float MeanChroma;

    		public float P95Chroma;

    		public float MaxChroma;

    		public float MeanLuma;
    	}

    	private const string Report = "Temp/texture_saturation.txt";

    	private const int Sample = 256;

    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] how much colour is actually in the textures?");
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			stringBuilder.AppendLine("STOPPED: exit play mode first (Ctrl+P).");
    			Finish(stringBuilder);
    			return;
    		}
    		List<Material> list = new List<Material>();
    		HashSet<string> hashSet = new HashSet<string>();
    		Renderer[] array = Object.FindObjectsByType<Renderer>((FindObjectsInactive)1);
    		for (int i = 0; i < array.Length; i++)
    		{
    			Material[] sharedMaterials = array[i].sharedMaterials;
    			foreach (Material val in sharedMaterials)
    			{
    				if (!((Object)(object)val == (Object)null) && hashSet.Add(AssetDatabase.GetAssetPath((Object)(object)val)))
    				{
    					list.Add(val);
    				}
    			}
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("materials in use by the scene: " + list.Count);
    		Dictionary<string, Stats> dictionary = new Dictionary<string, Stats>();
    		List<(Material, Stats)> list2 = new List<(Material, Stats)>();
    		foreach (Material item in list)
    		{
    			Texture texture = item.GetTexture("_BaseMap");
    			Texture2D val2 = (Texture2D)(object)((texture is Texture2D) ? texture : null);
    			if ((Object)(object)val2 == (Object)null)
    			{
    				list2.Add((item, null));
    				continue;
    			}
    			string assetPath = AssetDatabase.GetAssetPath((Object)(object)val2);
    			if (!dictionary.TryGetValue(assetPath, out var value))
    			{
    				value = (dictionary[assetPath] = Measure(val2));
    			}
    			list2.Add((item, value));
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- materials in use ---");
    		stringBuilder.AppendLine("material".PadRight(26) + "chroma  p95    max    luma   verdict");
    		foreach (var (val3, stats2) in list2.OrderByDescending(((Material mat, Stats s) x) => (x.s == null) ? (-1f) : x.s.MeanChroma))
    		{
    			if (stats2 == null)
    			{
    				stringBuilder.AppendLine(((Object)val3).name.PadRight(26) + "  (no _BaseMap texture)");
    				continue;
    			}
    			stringBuilder.AppendLine(((Object)val3).name.PadRight(26) + stats2.MeanChroma.ToString("F4") + "  " + stats2.P95Chroma.ToString("F3").PadRight(5) + "  " + stats2.MaxChroma.ToString("F3").PadRight(5) + "  " + stats2.MeanLuma.ToString("F3").PadRight(5) + "  " + Verdict(stats2.MeanChroma));
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- distinct textures behind those materials ---");
    		foreach (KeyValuePair<string, Stats> item2 in dictionary.OrderByDescending((KeyValuePair<string, Stats> k) => k.Value.MeanChroma))
    		{
    			Texture2D texture2 = item2.Value.Texture;
    			stringBuilder.AppendLine(Path.GetFileName(((Object)(object)texture2 != (Object)null) ? AssetDatabase.GetAssetPath((Object)(object)texture2) : "?").PadRight(30) + (((Object)(object)texture2 != (Object)null) ? (((Texture)texture2).width + "x" + ((Texture)texture2).height) : "?").PadRight(11) + "chroma " + item2.Value.MeanChroma.ToString("F4") + "   p95 " + item2.Value.P95Chroma.ToString("F3") + "   " + Verdict(item2.Value.MeanChroma));
    		}
    		List<Stats> list3 = dictionary.Values.Where((Stats s) => s.MeanChroma >= 0.05f).ToList();
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- verdict ---");
    		if (list3.Count == 0)
    		{
    			stringBuilder.AppendLine("EVERY texture sampled is effectively greyscale.");
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("That means _ColorRestore cannot bring colour back by itself, on");
    			stringBuilder.AppendLine("anything, because lerp(grey, grey, 1) is still grey. The mechanism");
    			stringBuilder.AppendLine("needs a colour to restore *to*: a per-material restored tint, or a");
    			stringBuilder.AppendLine("saturation target, rather than only an amount.");
    		}
    		else
    		{
    			stringBuilder.AppendLine(list3.Count + " of " + dictionary.Count + " textures carry real colour.");
    			stringBuilder.AppendLine("The lerp(grey, albedo, restore) approach will work on those.");
    			List<Stats> list4 = dictionary.Values.OrderBy((Stats s) => s.MeanChroma).Take(8).ToList();
    			stringBuilder.AppendLine("Textures with little or none (restore will be nearly invisible here):");
    			foreach (Stats item3 in list4)
    			{
    				stringBuilder.AppendLine("   " + Path.GetFileName(((Object)(object)item3.Texture != (Object)null) ? AssetDatabase.GetAssetPath((Object)(object)item3.Texture) : "?") + "  chroma " + item3.MeanChroma.ToString("F4") + ((item3.MeanChroma < 0.05f) ? "   <- greyscale" : ""));
    			}
    		}
    		Finish(stringBuilder);
    	}

    	private static string Verdict(float chroma)
    	{
    		if (!(chroma < 0.01f))
    		{
    			if (!(chroma < 0.05f))
    			{
    				if (!(chroma < 0.15f))
    				{
    					return "coloured";
    				}
    				return "muted";
    			}
    			return "nearly grey";
    		}
    		return "GREYSCALE";
    	}

    	private static Stats Measure(Texture2D tex)
    	{
    		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Expected Obj, but got Unknown
    		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
    		Stats stats = new Stats
    		{
    			Texture = tex
    		};
    		int num = Mathf.Max(1, Mathf.Min(256, ((Texture)tex).width));
    		int num2 = Mathf.Max(1, Mathf.Min(256, ((Texture)tex).height));
    		RenderTexture temporary = RenderTexture.GetTemporary(num, num2, 0, (RenderTextureFormat)0);
    		Texture2D val = new Texture2D(num, num2, (TextureFormat)4, false);
    		try
    		{
    			RenderTexture active = RenderTexture.active;
    			Graphics.Blit((Texture)(object)tex, temporary);
    			RenderTexture.active = temporary;
    			val.ReadPixels(new Rect(0f, 0f, (float)num, (float)num2), 0, 0);
    			val.Apply();
    			RenderTexture.active = active;
    			Color32[] pixels = val.GetPixels32();
    			List<float> list = new List<float>(pixels.Length);
    			double num3 = 0.0;
    			double num4 = 0.0;
    			float num5 = 0f;
    			Color32[] array = pixels;
    			foreach (Color32 val2 in array)
    			{
    				if (val2.a >= 8)
    				{
    					float num6 = (float)(int)val2.r / 255f;
    					float num7 = (float)(int)val2.g / 255f;
    					float num8 = (float)(int)val2.b / 255f;
    					float num9 = Mathf.Max(num6, Mathf.Max(num7, num8));
    					float num10 = Mathf.Min(num6, Mathf.Min(num7, num8));
    					float num11 = num9 - num10;
    					num3 += (double)num11;
    					num4 += (double)(0.2126f * num6 + 0.7152f * num7 + 0.0722f * num8);
    					if (num11 > num5)
    					{
    						num5 = num11;
    					}
    					list.Add(num11);
    				}
    			}
    			if (list.Count > 0)
    			{
    				stats.MeanChroma = (float)(num3 / (double)list.Count);
    				stats.MeanLuma = (float)(num4 / (double)list.Count);
    				stats.MaxChroma = num5;
    				list.Sort();
    				stats.P95Chroma = list[Mathf.Clamp((int)((float)list.Count * 0.95f), 0, list.Count - 1)];
    			}
    		}
    		finally
    		{
    			RenderTexture.ReleaseTemporary(temporary);
    			Object.DestroyImmediate((Object)(object)val);
    		}
    		return stats;
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), "Temp/texture_saturation.txt")));
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/texture_saturation.txt"), sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}