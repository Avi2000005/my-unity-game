using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;

namespace Echoes.Painterly.EditorTools
{

    public static class FountainRestoreProbe
    {
    	private const string Report = "Temp/fountain_restore.txt";

    	private const int W = 960;

    	private const int H = 640;

    	private static readonly Dictionary<string, Color32[]> _shots = new Dictionary<string, Color32[]>();

    	public static void Run()
    	{
    		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
    		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
    		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0279: Expected Obj, but got Unknown
    		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
    		//IL_040c: Expected Obj, but got Unknown
    		//IL_0457: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0339: Expected Obj, but got Unknown
    		//IL_0488: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0498: Unknown result type (might be due to invalid IL or missing references)
    		//IL_049d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0502: Expected Obj, but got Unknown
    		//IL_054e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0581: Unknown result type (might be due to invalid IL or missing references)
    		//IL_06fa: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] fountain restore probe");
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			stringBuilder.AppendLine("STOPPED: exit play mode first (Ctrl+P).");
    			Finish(stringBuilder);
    			return;
    		}
    		GameObject val = GameObject.Find("Fountain");
    		if ((Object)(object)val == (Object)null)
    		{
    			stringBuilder.AppendLine("FATAL: no 'Fountain' in the scene.");
    			Finish(stringBuilder);
    			return;
    		}
    		ColorRestoreTarget target = val.GetComponent<ColorRestoreTarget>();
    		Material val2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Painterly/Materials/RockTrim.mat");
    		if ((Object)(object)val2 == (Object)null)
    		{
    			stringBuilder.AppendLine("FATAL: RockTrim.mat not found.");
    			Finish(stringBuilder);
    			return;
    		}
    		float num = val2.GetFloat("_ColorRestore");
    		Color color = val2.GetColor("_BaseColor");
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- material ---");
    		stringBuilder.AppendLine("RockTrim _ColorRestore = " + num);
    		stringBuilder.AppendLine("RockTrim _BaseColor    = " + ((object)color/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine("RockTrim shader        = " + ((Object)val2.shader).name);
    		stringBuilder.AppendLine("keywords               = " + string.Join(", ", val2.shaderKeywords));
    		Renderer[] componentsInChildren = val.GetComponentsInChildren<Renderer>(true);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- the fountain ---");
    		stringBuilder.AppendLine("renderers              = " + componentsInChildren.Length);
    		stringBuilder.AppendLine("root position          = " + ((object)val.transform.position/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine("root scale             = " + ((object)val.transform.localScale/*cast due to constrained. prefix*/).ToString());
    		Bounds bounds = new Bounds(val.transform.position, Vector3.zero);
    		Renderer[] array = componentsInChildren;
    		foreach (Renderer val3 in array)
    		{
    			bounds.Encapsulate(val3.bounds);
    		}
    		stringBuilder.AppendLine("world bounds           = " + ((object)bounds.size/*cast due to constrained. prefix*/).ToString() + " at " + ((object)bounds.center/*cast due to constrained. prefix*/).ToString());
    		int num2 = 0;
    		array = componentsInChildren;
    		for (int i = 0; i < array.Length; i++)
    		{
    			if (array[i].HasPropertyBlock())
    			{
    				num2++;
    			}
    		}
    		if (componentsInChildren.Length != 0)
    		{
    			MaterialPropertyBlock val4 = new MaterialPropertyBlock();
    			componentsInChildren[0].GetPropertyBlock(val4);
    			stringBuilder.AppendLine("with a property block  = " + num2 + " of " + componentsInChildren.Length + ", renderer[0] block _ColorRestore = " + val4.GetFloat("_ColorRestore"));
    		}
    		RenderPipelineAsset currentRenderPipeline = GraphicsSettings.currentRenderPipeline;
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- pipeline ---");
    		stringBuilder.AppendLine("pipeline               = " + (((Object)(object)currentRenderPipeline == (Object)null) ? "<built-in>" : ((Object)currentRenderPipeline).name));
    		if ((Object)(object)currentRenderPipeline != (Object)null)
    		{
    			SerializedObject val5 = new SerializedObject((Object)(object)currentRenderPipeline);
    			string[] array2 = new string[3] { "m_GPUResidentDrawerMode", "m_GPUResidentDrawerEnableOcclusionCullingInCameras", "m_UseSRPBatcher" };
    			foreach (string text in array2)
    			{
    				SerializedProperty val6 = val5.FindProperty(text);
    				stringBuilder.AppendLine("  " + text.PadRight(52) + " = " + ((val6 == null) ? "<absent>" : val6.intValue.ToString()));
    			}
    		}
    		Vector3 position = PickEye(val, bounds, out var occluders, out var detail);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- camera ---");
    		stringBuilder.AppendLine("eye                    = " + ((object)position/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine(detail);
    		GameObject val7 = new GameObject("__FountainRestoreCam");
    		Camera val8 = val7.AddComponent<Camera>();
    		val8.fieldOfView = 45f;
    		val8.nearClipPlane = 0.05f;
    		val8.farClipPlane = 800f;
    		val8.clearFlags = (CameraClearFlags)2;
    		val8.backgroundColor = new Color(0f, 0f, 0f, 1f);
    		Type type = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
    		if (type != null)
    		{
    			val7.AddComponent(type);
    		}
    		val7.transform.position = position;
    		val7.transform.LookAt(bounds.center, Vector3.up);
    		Camera[] array3 = Object.FindObjectsByType<Camera>((FindObjectsInactive)1);
    		bool[] array4 = new bool[array3.Length];
    		for (int j = 0; j < array3.Length; j++)
    		{
    			array4[j] = ((Behaviour)array3[j]).enabled;
    			((Behaviour)array3[j]).enabled = false;
    		}
    		RenderTexture val9 = (val8.targetTexture = new RenderTexture(960, 640, 24, (RenderTextureFormat)0)
    		{
    			antiAliasing = 4
    		});
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- shots ---");
    		Shot(stringBuilder, val8, val9, "A_grey_baseline", "nothing touched", null);
    		val2.SetColor("_BaseColor", new Color(1f, 0f, 0.6f, 1f));
    		val2.SetFloat("_ColorRestore", num);
    		Shot(stringBuilder, val8, val9, "B_magenta_control", "material albedo magenta", null);
    		val2.SetColor("_BaseColor", color);
    		val2.SetFloat("_ColorRestore", 1f);
    		Shot(stringBuilder, val8, val9, "C_material_restore", "material _ColorRestore = 1", null);
    		val2.SetFloat("_ColorRestore", num);
    		Shot(stringBuilder, val8, val9, "D_block_restore", "block _ColorRestore = 1", () =>
    		{
    			if ((Object)(object)target != (Object)null)
    			{
    				target.SetRestoreImmediate(1f);
    			}
    		});
    		Shot(stringBuilder, val8, val9, "E_block_grey", "block _ColorRestore = 0", () =>
    		{
    			if ((Object)(object)target != (Object)null)
    			{
    				target.SetRestoreImmediate(0f);
    			}
    		});
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- verdict: pixels that CHANGED between shots ---");
    		stringBuilder.AppendLine("A vs B  magenta control       : " + Verdict("A_grey_baseline", "B_magenta_control") + "   <- must be large, or the fountain is not on screen");
    		stringBuilder.AppendLine("A vs C  material restore      : " + Verdict("A_grey_baseline", "C_material_restore") + "   <- does the shader honour the value?");
    		stringBuilder.AppendLine("A vs D  property-block restore: " + Verdict("A_grey_baseline", "D_block_restore") + "   <- does the brush's route work?");
    		stringBuilder.AppendLine("D vs E  block on vs off       : " + Verdict("D_block_restore", "E_block_grey"));
    		if (occluders.Length != 0)
    		{
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("NOTE: something was still in front of the fountain at the chosen");
    			stringBuilder.AppendLine("eye, so read the numbers above with that in mind:");
    			string[] array2 = occluders;
    			foreach (string text2 in array2)
    			{
    				stringBuilder.AppendLine("   " + text2);
    			}
    		}
    		val2.SetColor("_BaseColor", color);
    		val2.SetFloat("_ColorRestore", num);
    		EditorUtility.SetDirty((Object)(object)val2);
    		if ((Object)(object)target != (Object)null)
    		{
    			target.SetRestoreImmediate(0f);
    		}
    		AssetDatabase.SaveAssets();
    		val8.targetTexture = null;
    		Object.DestroyImmediate((Object)(object)val9);
    		for (int num3 = 0; num3 < array3.Length; num3++)
    		{
    			if ((Object)(object)array3[num3] != (Object)null)
    			{
    				((Behaviour)array3[num3]).enabled = array4[num3];
    			}
    		}
    		Object.DestroyImmediate((Object)(object)val7);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("materials restored to their original values.");
    		Finish(stringBuilder);
    	}

    	private static Vector3 PickEye(GameObject root, Bounds bounds, out string[] occluders, out string detail)
    	{
    		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
    		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
    		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
    		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0341: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 val = bounds.extents;
    		float magnitude = val.magnitude;
    		Vector3 center = bounds.center;
    		float num = magnitude / Mathf.Sin((float)Math.PI / 8f);
    		List<string> list = new List<string>();
    		Vector3[] array = new Vector3[8]
    		{
    			new Vector3(0.62f, 0.42f, -0.66f),
    			new Vector3(-0.62f, 0.42f, -0.66f),
    			new Vector3(0f, 0.4f, -0.92f),
    			new Vector3(0.9f, 0.4f, 0f),
    			new Vector3(-0.9f, 0.4f, 0f),
    			new Vector3(0f, 0.45f, 0.92f),
    			new Vector3(0.5f, 0.55f, 0.66f),
    			new Vector3(-0.5f, 0.55f, 0.66f)
    		};
    		float[] array2 = new float[5] { 1f, 1.35f, 1.8f, 2.4f, 3.2f };
    		RaycastHit[] array5;
    		for (int i = 0; i < array2.Length; i++)
    		{
    			float num2 = array2[i];
    			Vector3[] array3 = array;
    			for (int j = 0; j < array3.Length; j++)
    			{
    				Vector3 val2 = array3[j];
    				Vector3 val3 = center + val2.normalized * (num * num2);
    				val = center - val3;
    				Vector3 normalized = val.normalized;
    				RaycastHit[] array4 = Physics.RaycastAll(val3, normalized, Vector3.Distance(val3, center) * 1.2f, -1, (QueryTriggerInteraction)1);
    				Array.Sort(array4, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
    				if (array4.Length != 0 && ((Component)array4[0].collider).transform.IsChildOf(root.transform))
    				{
    					occluders = new string[0];
    					detail = "framing                : fit distance x" + num2.ToString("F2") + ", clear line of sight to the centre";
    					return val3;
    				}
    				list.Clear();
    				array5 = array4;
    				for (int num3 = 0; num3 < array5.Length; num3++)
    				{
    					RaycastHit val4 = array5[num3];
    					bool flag = ((Component)val4.collider).transform.IsChildOf(root.transform);
    					list.Add(val4.distance.ToString("F2") + " m " + (flag ? "FOUNTAIN " : "other    ") + SquarePlacement.PathOf(((Component)val4.collider).transform));
    					if (flag)
    					{
    						break;
    					}
    				}
    			}
    		}
    		val = new Vector3(0.62f, 0.42f, -0.66f);
    		Vector3 val5 = center + val.normalized * (num * 3.2f);
    		val = center - val5;
    		Vector3 normalized2 = val.normalized;
    		RaycastHit[] array6 = Physics.RaycastAll(val5, normalized2, Vector3.Distance(val5, center) * 1.2f, -1, (QueryTriggerInteraction)1);
    		Array.Sort(array6, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
    		list.Clear();
    		array5 = array6;
    		for (int i = 0; i < array5.Length; i++)
    		{
    			RaycastHit val6 = array5[i];
    			list.Add(val6.distance.ToString("F2") + " m  " + SquarePlacement.PathOf(((Component)val6.collider).transform));
    		}
    		occluders = list.ToArray();
    		detail = "framing                : NO clear line from any candidate; fell back to the widest shot";
    		return val5;
    	}

    	private static int Shot(StringBuilder sb, Camera cam, RenderTexture rt, string name, string what, Action before)
    	{
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002f: Expected Obj, but got Unknown
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		before?.Invoke();
    		cam.Render();
    		RenderTexture active = RenderTexture.active;
    		RenderTexture.active = rt;
    		Texture2D val = new Texture2D(960, 640, (TextureFormat)3, false);
    		val.ReadPixels(new Rect(0f, 0f, 960f, 640f), 0, 0);
    		val.Apply();
    		RenderTexture.active = active;
    		Color32[] pixels = val.GetPixels32();
    		int num = 0;
    		for (int i = 0; i < pixels.Length; i++)
    		{
    			if (pixels[i].r + pixels[i].g + pixels[i].b > 15)
    			{
    				num++;
    			}
    		}
    		_shots[name] = pixels;
    		byte[] array = ImageConversion.EncodeToPNG(val);
    		File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Temp/fr_" + name + ".png"), array);
    		Object.DestroyImmediate((Object)(object)val);
    		sb.AppendLine(name.PadRight(24) + what.PadRight(32) + "lit " + num.ToString("N0").PadLeft(9) + " px   " + array.Length.ToString("N0") + " bytes");
    		return num;
    	}

    	private static string Verdict(string a, string b)
    	{
    		if (!_shots.TryGetValue(a, out var value) || !_shots.TryGetValue(b, out var value2))
    		{
    			return "MISSING SHOT";
    		}
    		if (value.Length != value2.Length)
    		{
    			return "SIZE MISMATCH";
    		}
    		int num = 0;
    		int num2 = 0;
    		for (int i = 0; i < value.Length; i++)
    		{
    			int num3 = Mathf.Abs(value[i].r - value2[i].r) + Mathf.Abs(value[i].g - value2[i].g) + Mathf.Abs(value[i].b - value2[i].b);
    			if (num3 > 6)
    			{
    				num++;
    			}
    			if (num3 > num2)
    			{
    				num2 = num3;
    			}
    		}
    		float num4 = 100f * (float)num / (float)value.Length;
    		return num.ToString("N0") + " px changed (" + num4.ToString("F2") + "% of frame), max delta " + num2 + "/765";
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), "Temp/fountain_restore.txt")));
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/fountain_restore.txt"), sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}