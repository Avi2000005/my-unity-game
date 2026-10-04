using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class PainterlyMaterialSetup
    {
    	private struct Spec
    	{
    	public Spec(string n, string bc, string nm, float s, float m = 0f, bool clip = false, string note = null)
    	{
    	    	this.name = n;
    	    	this.baseColor = bc;
    	    	this.normal = nm;
    	    	this.smoothness = s;
    	    	this.metallic = m;
    	    	this.alphaClip = clip;
    	    	this.note = note;
    	}
    		public string name;

    		public string baseColor;

    		public string normal;

    		public float smoothness;

    		public float metallic;

    		public bool alphaClip;

    		public string note;
    	}

    	private const string ShaderName = "Echoes/PainterlyLit";

    	private const string TextureDir = "Assets/Art/Village/Textures";

    	private const string OutputDir = "Assets/Painterly/Materials";

    	private static readonly Spec[] Specs = new Spec[14]
    	{
    		new Spec("Brick", "T_Brick_BaseColor", "T_Brick_Normal", 0.15f),
    		new Spec("RedBrick", "T_RedBrick_BaseColor", null, 0.15f),
    		new Spec("UnevenBrick", "T_UnevenBrick_BaseColor", "T_UnevenBrick_Normal", 0.15f),
    		new Spec("Plaster", "T_Plaster_BaseColor", "T_Plaster_Normal", 0.1f),
    		new Spec("RockTrim", "T_RockTrim_BaseColor", "T_RockTrim_Normal", 0.12f),
    		new Spec("RoundTiles", "T_RoundTiles_BaseColor", "T_RoundTiles_Normal", 0.25f),
    		new Spec("WoodTrim", "T_WoodTrim_BaseColor", "T_WoodTrim_Normal", 0.18f),
    		new Spec("MetalOrnament", "T_MetalOrnaments_BaseColor", null, 0.45f, 0.9f),
    		new Spec("VineLeaf", "T_VineLeaf", null, 0.1f, 0f, clip: true, "alpha clipped"),
    		new Spec("WindowPane", "T_WindowGradient", null, 0.6f, 0f, clip: false, "no normal map in kit"),
    		new Spec("Grey_Plaster", null, null, 0.08f, 0f, clip: false, "untextured"),
    		new Spec("Grey_Wood", null, null, 0.15f, 0f, clip: false, "untextured"),
    		new Spec("Grey_Stone", null, null, 0.12f, 0f, clip: false, "untextured"),
    		new Spec("Grey_Ground", null, null, 0.05f, 0f, clip: false, "untextured")
    	};

    	[MenuItem("Tools/Echoes/Build Village Materials")]
    	public static void Build()
    	{
    		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008c: Expected Obj, but got Unknown
    		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
    		Shader val = Shader.Find("Echoes/PainterlyLit");
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] Could not find shader 'Echoes/PainterlyLit'. Has Unity finished importing it?");
    			return;
    		}
    		if (!Directory.Exists("Assets/Painterly/Materials"))
    		{
    			Directory.CreateDirectory("Assets/Painterly/Materials");
    		}
    		List<string> list = new List<string>();
    		List<string> list2 = new List<string>();
    		Spec[] specs = Specs;
    		for (int i = 0; i < specs.Length; i++)
    		{
    			Spec spec = specs[i];
    			Material val2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Painterly/Materials/" + spec.name + ".mat");
    			bool flag = (Object)(object)val2 == (Object)null;
    			if (flag)
    			{
    				val2 = new Material(val);
    			}
    			val2.shader = val;
    			Texture2D val3 = LoadTexture(spec.baseColor, list2);
    			if ((Object)(object)val3 != (Object)null)
    			{
    				val2.SetTexture("_BaseMap", (Texture)(object)val3);
    				val2.EnableKeyword("_NORMALMAP");
    			}
    			else
    			{
    				val2.SetTexture("_BaseMap", (Texture)null);
    				val2.DisableKeyword("_NORMALMAP");
    			}
    			Texture2D val4 = LoadTexture(spec.normal, list2);
    			if ((Object)(object)val4 != (Object)null)
    			{
    				val2.SetTexture("_BumpMap", (Texture)(object)val4);
    				val2.SetFloat("_BumpScale", 1f);
    				if ((Object)(object)val3 != (Object)null)
    				{
    					val2.EnableKeyword("_NORMALMAP");
    				}
    			}
    			else
    			{
    				val2.SetTexture("_BumpMap", (Texture)null);
    				if ((Object)(object)val3 == (Object)null)
    				{
    					val2.DisableKeyword("_NORMALMAP");
    				}
    			}
    			val2.SetColor("_BaseColor", Color.white);
    			val2.SetFloat("_Smoothness", spec.smoothness);
    			val2.SetFloat("_Metallic", spec.metallic);
    			val2.SetFloat("_BumpScale", 1f);
    			val2.enableInstancing = true;
    			val2.SetFloat("_ColorRestore", 0f);
    			val2.SetFloat("_RestoreBoost", 1f);
    			if (spec.alphaClip)
    			{
    				val2.SetFloat("_Cutoff", 0.4f);
    				val2.SetFloat("_Surface", 0f);
    				val2.SetFloat("_Blend", 0f);
    				val2.SetFloat("_ZWrite", 1f);
    				val2.EnableKeyword("_ALPHATEST_ON");
    				val2.renderQueue = 2450;
    				val2.SetOverrideTag("RenderType", "TransparentCutout");
    			}
    			else
    			{
    				val2.DisableKeyword("_ALPHATEST_ON");
    				val2.renderQueue = -1;
    				val2.SetOverrideTag("RenderType", "Opaque");
    			}
    			string text = "Assets/Painterly/Materials/" + spec.name + ".mat";
    			if (flag)
    			{
    				AssetDatabase.CreateAsset((Object)(object)val2, text);
    			}
    			else
    			{
    				EditorUtility.SetDirty((Object)(object)val2);
    			}
    			list.Add(spec.name + ((spec.note != null) ? ("  (" + spec.note + ")") : ""));
    		}
    		AssetDatabase.SaveAssets();
    		AssetDatabase.Refresh();
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine(string.Format("Created/updated {0} materials in {1}\n", list.Count, "Assets/Painterly/Materials"));
    		foreach (string item in list)
    		{
    			stringBuilder.AppendLine("  • " + item);
    		}
    		if (list2.Count > 0)
    		{
    			stringBuilder.AppendLine("\n" + Environment.NewLine + "Textures not found (material left untextured):");
    			foreach (string item2 in list2.Distinct())
    			{
    				stringBuilder.AppendLine("  - " + item2);
    			}
    		}
    		Debug.Log((object)$"[Echoes] Village materials\n\n{stringBuilder}");
    	}

    	private static Texture2D LoadTexture(string fileName, List<string> missing)
    	{
    		if (string.IsNullOrEmpty(fileName))
    		{
    			return null;
    		}
    		Texture2D val = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Village/Textures/" + fileName + ".png");
    		if ((Object)(object)val == (Object)null)
    		{
    			val = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Village/Textures/" + fileName + ".jpg");
    		}
    		if ((Object)(object)val == (Object)null)
    		{
    			missing.Add(fileName);
    		}
    		return val;
    	}
    }
}