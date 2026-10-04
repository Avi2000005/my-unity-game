using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class WandSetup
    {
    	private const string Dir = "Assets/Art/Props/Wand";

    	private const string ShaderName = "Echoes/PainterlyLit";

    	private const string Material = "Wand";

    	[MenuItem("Tools/Echoes/Import Wand", priority = 64)]
    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		try
    		{
    			RunInner(stringBuilder);
    		}
    		catch (Exception ex)
    		{
    			stringBuilder.AppendLine("FAILED: " + ex);
    		}
    		finally
    		{
    			File.WriteAllText("Temp/wand.txt", stringBuilder.ToString());
    			Debug.Log((object)("[Echoes] Wand import\n" + stringBuilder));
    		}
    	}

    	private static void RunInner(StringBuilder sb)
    	{
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			sb.AppendLine("Stop play mode first — an import made during play is discarded when play mode exits.");
    			return;
    		}
    		AssetDatabase.Refresh((ImportAssetOptions)9);
    		string text = "Assets/Art/Props/Wand/Wand.fbx";
    		AssetImporter atPath = AssetImporter.GetAtPath(text);
    		ModelImporter val = (ModelImporter)(object)((atPath is ModelImporter) ? atPath : null);
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("missing: " + text + " — the file was not copied in.");
    			return;
    		}
    		val.animationType = (ModelImporterAnimationType)0;
    		val.importAnimation = false;
    		val.addCollider = false;
    		val.isReadable = false;
    		val.materialImportMode = (ModelImporterMaterialImportMode)0;
    		((AssetImporter)val).SaveAndReimport();
    		ReportMesh(sb, text);
    		Texture2D albedo = Texture("Assets/Art/Props/Wand/Wand.png", asNormal: false, sb);
    		Texture2D normal = Texture("Assets/Art/Props/Wand/Wand_normal.png", asNormal: true, sb);
    		Texture2D val2 = Texture("Assets/Art/Props/Wand/Wand_roughness.png", asNormal: false, sb);
    		Texture2D val3 = Texture("Assets/Art/Props/Wand/Wand_metallic.png", asNormal: false, sb);
    		sb.AppendLine($"roughness present={(Object)(object)val2 != (Object)null} " + $"metallic present={(Object)(object)val3 != (Object)null} " + "(not packed into _MetallicGlossMap yet)");
    		BuildMaterial(albedo, normal, val3, val2, sb);
    	}

    	private static Texture2D Texture(string path, bool asNormal, StringBuilder sb)
    	{
    		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
    		Texture2D val = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("texture missing: " + path);
    			return null;
    		}
    		AssetImporter atPath = AssetImporter.GetAtPath(path);
    		TextureImporter val2 = (TextureImporter)(object)((atPath is TextureImporter) ? atPath : null);
    		if ((Object)(object)val2 != (Object)null)
    		{
    			TextureImporterType val3 = (TextureImporterType)(asNormal ? 1 : 0);
    			if (val2.textureType != val3)
    			{
    				val2.textureType = val3;
    				val2.sRGBTexture = !asNormal;
    				((AssetImporter)val2).SaveAndReimport();
    				sb.AppendLine($"  {Path.GetFileName(path)}: type set to {val3}");
    			}
    		}
    		sb.AppendLine($"  {Path.GetFileName(path)}: {((Texture)val).width}x{((Texture)val).height}");
    		return val;
    	}

    	private static void BuildMaterial(Texture2D albedo, Texture2D normal, Texture2D metallic, Texture2D roughness, StringBuilder sb)
    	{
    		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0041: Expected Obj, but got Unknown
    		Shader val = Shader.Find("Echoes/PainterlyLit");
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("shader 'Echoes/PainterlyLit' not found — is it imported?");
    			return;
    		}
    		string text = "Assets/Art/Props/Wand/Wand.mat";
    		Material val2 = AssetDatabase.LoadAssetAtPath<Material>(text);
    		bool flag = (Object)(object)val2 == (Object)null;
    		if (flag)
    		{
    			val2 = new Material(val);
    			AssetDatabase.CreateAsset((Object)(object)val2, text);
    		}
    		else
    		{
    			val2.shader = val;
    		}
    		if ((Object)(object)albedo != (Object)null)
    		{
    			val2.SetTexture("_BaseMap", (Texture)(object)albedo);
    		}
    		if ((Object)(object)normal != (Object)null)
    		{
    			val2.SetTexture("_BumpMap", (Texture)(object)normal);
    		}
    		val2.SetFloat("_Metallic", ((Object)(object)metallic != (Object)null) ? 0.05f : 0f);
    		val2.SetFloat("_Smoothness", 0.35f);
    		val2.SetFloat("_ColorRestore", 1f);
    		val2.SetFloat("_RestoreBoost", 1f);
    		EditorUtility.SetDirty((Object)(object)val2);
    		AssetDatabase.SaveAssets();
    		sb.AppendLine(string.Format("material {0}: created={1} ", "Wand", flag) + $"albedo={(Object)(object)albedo != (Object)null} normal={(Object)(object)normal != (Object)null} " + "shader=" + ((Object)val2.shader).name);
    	}

    	private static void ReportMesh(StringBuilder sb, string fbx)
    	{
    		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
    		Mesh val = AssetDatabase.LoadAssetAtPath<Mesh>(fbx);
    		if ((Object)(object)val == (Object)null)
    		{
    			Object[] array = AssetDatabase.LoadAllAssetsAtPath(fbx);
    			foreach (Object obj in array)
    			{
    				Mesh val2 = (Mesh)(object)((obj is Mesh) ? obj : null);
    				if (val2 != null)
    				{
    					val = val2;
    					break;
    				}
    			}
    		}
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("no mesh found in the FBX");
    			return;
    		}
    		string text = $"mesh '{((Object)val).name}' verts={val.vertexCount} ";
    		string text2 = $"subMeshes={val.subMeshCount} ";
    		Bounds bounds = val.bounds;
    		object arg = bounds.min;
    		bounds = val.bounds;
    		string text3 = $"bounds={arg} .. {bounds.max} ";
    		bounds = val.bounds;
    		sb.AppendLine(text + text2 + text3 + $"size={bounds.size}");
    	}
    }
}