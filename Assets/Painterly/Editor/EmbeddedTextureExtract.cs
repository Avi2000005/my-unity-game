using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class EmbeddedTextureExtract
    {
    	private const string Report = "Temp/extract_texture.txt";

    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] Embedded texture extraction");
    		stringBuilder.AppendLine();
    		Extract("Assets/Art/Ari/Models/Ari_character.fbx", "Assets/Art/Ari/Textures/Ari_basecolor.png", stringBuilder);
    		Directory.CreateDirectory(Path.GetDirectoryName("Temp/extract_texture.txt"));
    		File.WriteAllText("Temp/extract_texture.txt", stringBuilder.ToString());
    		Debug.Log((object)stringBuilder.ToString());
    	}

    	private static void Extract(string fbxPath, string outPath, StringBuilder sb)
    	{
    		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("source: " + fbxPath);
    		if (!File.Exists(fbxPath))
    		{
    			sb.AppendLine("  MISSING from disk - nothing to extract");
    			return;
    		}
    		AssetImporter atPath = AssetImporter.GetAtPath(fbxPath);
    		ModelImporter val = (ModelImporter)(object)((atPath is ModelImporter) ? atPath : null);
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("  no ModelImporter - Unity has not imported this file yet");
    			return;
    		}
    		bool isReadable = val.isReadable;
    		if (!isReadable)
    		{
    			val.isReadable = true;
    			((AssetImporter)val).SaveAndReimport();
    		}
    		Object[] array = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
    		sb.AppendLine($"  sub-assets: {array.Length}");
    		List<Texture2D> list = new List<Texture2D>();
    		List<Material> list2 = new List<Material>();
    		Object[] array2 = array;
    		foreach (Object val2 in array2)
    		{
    			Texture2D val3 = (Texture2D)(object)((val2 is Texture2D) ? val2 : null);
    			if (val3 != null && (Object)(object)val3 != (Object)null)
    			{
    				list.Add(val3);
    				continue;
    			}
    			Material val4 = (Material)(object)((val2 is Material) ? val2 : null);
    			if (val4 != null && (Object)(object)val4 != (Object)null)
    			{
    				list2.Add(val4);
    			}
    		}
    		sb.AppendLine($"  Texture2D: {list.Count}   Material: {list2.Count}");
    		foreach (Material item in list2)
    		{
    			sb.AppendLine("  material '" + ((Object)item).name + "' shader='" + ((Object)item.shader).name + "'");
    			if (item.HasProperty("_BaseMap") && (Object)(object)item.GetTexture("_BaseMap") != (Object)null)
    			{
    				sb.AppendLine("    _BaseMap -> " + ((Object)item.GetTexture("_BaseMap")).name);
    			}
    			if (item.HasProperty("_MainTex") && (Object)(object)item.GetTexture("_MainTex") != (Object)null)
    			{
    				sb.AppendLine("    _MainTex -> " + ((Object)item.GetTexture("_MainTex")).name);
    			}
    		}
    		if (list.Count == 0)
    		{
    			sb.AppendLine("  NO TEXTURE FOUND in this FBX");
    			return;
    		}
    		Texture2D val5 = list[0];
    		foreach (Texture2D item2 in list)
    		{
    			if ((long)((Texture)item2).width * (long)((Texture)item2).height > (long)((Texture)val5).width * (long)((Texture)val5).height)
    			{
    				val5 = item2;
    			}
    		}
    		sb.AppendLine();
    		sb.AppendLine($"chosen: '{((Object)val5).name}' {((Texture)val5).width}x{((Texture)val5).height} fmt={val5.format}");
    		ReportLuma(val5, sb);
    		Directory.CreateDirectory(Path.GetDirectoryName(outPath));
    		byte[] array3 = ImageConversion.EncodeToPNG(val5);
    		File.WriteAllBytes(outPath, array3);
    		AssetDatabase.ImportAsset(outPath, (ImportAssetOptions)1);
    		sb.AppendLine($"written: {outPath} ({array3.Length:N0} bytes)");
    		if (!isReadable)
    		{
    			val.isReadable = false;
    			((AssetImporter)val).SaveAndReimport();
    		}
    	}

    	private static void ReportLuma(Texture2D t, StringBuilder sb)
    	{
    		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
    		try
    		{
    			Color[] pixels = t.GetPixels();
    			if (pixels == null || pixels.Length == 0)
    			{
    				sb.AppendLine("  luma: no pixels readable");
    				return;
    			}
    			double num = 0.0;
    			double num2 = 0.0;
    			double num3 = 0.0;
    			Color[] array = pixels;
    			foreach (Color val in array)
    			{
    				double num4 = 0.2126 * (double)val.r + 0.7152 * (double)val.g + 0.0722 * (double)val.b;
    				num += num4;
    				if (num4 > 0.55)
    				{
    					num2++;
    				}
    				if (num4 < 0.18)
    				{
    					num3++;
    				}
    			}
    			int num5 = pixels.Length;
    			sb.AppendLine($"  pixels   : {num5:N0}");
    			sb.AppendLine($"  avg luma : {num / (double)num5 * 255.0:F1} / 255");
    			sb.AppendLine($"  bright   : {num2 * 100.0 / (double)num5:F1}%  (>140)");
    			sb.AppendLine($"  near-blk : {num3 * 100.0 / (double)num5:F1}%  (<45)");
    		}
    		catch (Exception ex)
    		{
    			sb.AppendLine("  luma FAILED: " + ex.GetType().Name + ": " + ex.Message);
    		}
    	}
    }
}