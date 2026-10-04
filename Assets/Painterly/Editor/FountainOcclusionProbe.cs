using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class FountainOcclusionProbe
    {
    	private const string Report = "Temp/fountain_occlusion.txt";

    	private const int W = 800;

    	private const int H = 500;

    	public static void Run()
    	{
    		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f3: Expected Obj, but got Unknown
    		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_038b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_039c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03a5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03b6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03cd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
    		//IL_044d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
    		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_045f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0461: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0463: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04cf: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05da: Expected Obj, but got Unknown
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] fountain occlusion probe");
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
    		Renderer[] componentsInChildren = val.GetComponentsInChildren<Renderer>(true);
    		Bounds val2 = new Bounds(val.transform.position, Vector3.zero);
    		Renderer[] array = componentsInChildren;
    		foreach (Renderer val3 in array)
    		{
    			val2.Encapsulate(val3.bounds);
    		}
    		stringBuilder.AppendLine();
    		Vector3 val4 = val2.center;
    		stringBuilder.AppendLine("fountain bounds centre : " + ((object)val4/*cast due to constrained. prefix*/).ToString());
    		val4 = val2.size;
    		stringBuilder.AppendLine("fountain bounds size   : " + ((object)val4/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine("fountain renderers     : " + componentsInChildren.Length);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("per-renderer material / enabled / layer:");
    		Dictionary<string, int> dictionary = new Dictionary<string, int>();
    		array = componentsInChildren;
    		for (int i = 0; i < array.Length; i++)
    		{
    			Material sharedMaterial = array[i].sharedMaterial;
    			string key = (((Object)(object)sharedMaterial == (Object)null) ? "<none>" : (((Object)sharedMaterial).name + " [" + ((Object)sharedMaterial.shader).name + "]"));
    			if (!dictionary.ContainsKey(key))
    			{
    				dictionary[key] = 0;
    			}
    			dictionary[key]++;
    		}
    		foreach (KeyValuePair<string, int> item in dictionary)
    		{
    			stringBuilder.AppendLine("   " + item.Value + " x  " + item.Key);
    		}
    		array = componentsInChildren;
    		foreach (Renderer val5 in array)
    		{
    			stringBuilder.AppendLine("   " + ((Object)val5).name.PadRight(22) + " enabled=" + val5.enabled + " layer=" + LayerMask.LayerToName(((Component)val5).gameObject.layer) + " renderLayerMask=" + val5.renderingLayerMask + " visible=" + (val5.isVisible ? "yes" : "no"));
    		}
    		Vector3 center = val2.center;
    		val4 = val2.extents;
    		float magnitude = val4.magnitude;
    		GameObject val6 = new GameObject("__FountainOcclusionCam");
    		Camera val7 = val6.AddComponent<Camera>();
    		val7.fieldOfView = 50f;
    		val7.nearClipPlane = 0.05f;
    		val7.farClipPlane = 1000f;
    		val7.clearFlags = (CameraClearFlags)2;
    		val7.backgroundColor = new Color(0f, 0f, 0f, 1f);
    		Type type = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
    		if (type != null)
    		{
    			val6.AddComponent(type);
    		}
    		float num = magnitude / Mathf.Sin(val7.fieldOfView * 0.5f * ((float)Math.PI / 180f)) * 1.25f;
    		val4 = new Vector3(0.62f, 0.45f, -0.65f);
    		Vector3 val8 = center + val4.normalized * num;
    		val6.transform.position = val8;
    		val6.transform.LookAt(center, Vector3.up);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("bounding radius        : " + magnitude.ToString("F2") + " m");
    		stringBuilder.AppendLine("camera eye             : " + ((object)val8/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine("camera->centre dist    : " + Vector3.Distance(val8, center).ToString("F2") + " m (fit by FOV, not guessed)");
    		val4 = center - val8;
    		Vector3 normalized = val4.normalized;
    		RaycastHit[] array2 = Physics.RaycastAll(val8, normalized, num * 1.5f, -1, (QueryTriggerInteraction)1);
    		Array.Sort(array2, (RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance));
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("raycast from camera to fountain centre, in hit order:");
    		if (array2.Length == 0)
    		{
    			stringBuilder.AppendLine("   NOTHING HIT — no collider anywhere along the view line");
    		}
    		RaycastHit[] array3 = array2;
    		for (int i = 0; i < array3.Length; i++)
    		{
    			RaycastHit val9 = array3[i];
    			bool flag = ((Component)val9.collider).transform.IsChildOf(val.transform);
    			stringBuilder.AppendLine("   " + val9.distance.ToString("F2").PadLeft(7) + " m  " + (flag ? "FOUNTAIN  " : "other     ") + PathOf(((Component)val9.collider).transform) + "  [" + ((object)val9.collider).GetType().Name + "]");
    		}
    		Camera[] array4 = Object.FindObjectsByType<Camera>((FindObjectsInactive)1);
    		bool[] array5 = new bool[array4.Length];
    		for (int num2 = 0; num2 < array4.Length; num2++)
    		{
    			array5[num2] = ((Behaviour)array4[num2]).enabled;
    			((Behaviour)array4[num2]).enabled = false;
    		}
    		RenderTexture val10 = (val7.targetTexture = new RenderTexture(800, 500, 24, (RenderTextureFormat)0));
    		Renderer[] array6 = Object.FindObjectsByType<Renderer>((FindObjectsInactive)1);
    		bool[] array7 = new bool[array6.Length];
    		for (int num3 = 0; num3 < array6.Length; num3++)
    		{
    			array7[num3] = array6[num3].enabled;
    			if (!((Component)array6[num3]).transform.IsChildOf(val.transform))
    			{
    				array6[num3].enabled = false;
    			}
    		}
    		int num4 = Render(stringBuilder, val7, val10, "fountain_only");
    		stringBuilder.AppendLine("   ^ non-black pixels with everything else hidden: " + num4 + " of " + 400000 + " (" + (100f * (float)num4 / 400000f).ToString("F1") + "%)");
    		for (int num5 = 0; num5 < array6.Length; num5++)
    		{
    			if ((Object)(object)array6[num5] != (Object)null)
    			{
    				array6[num5].enabled = array7[num5];
    			}
    		}
    		int num6 = Render(stringBuilder, val7, val10, "everything");
    		stringBuilder.AppendLine("   ^ non-black pixels with the whole village on  : " + num6 + " of " + 400000 + " (" + (100f * (float)num6 / 400000f).ToString("F1") + "%)");
    		val7.targetTexture = null;
    		Object.DestroyImmediate((Object)(object)val10);
    		for (int num7 = 0; num7 < array4.Length; num7++)
    		{
    			if ((Object)(object)array4[num7] != (Object)null)
    			{
    				((Behaviour)array4[num7]).enabled = array5[num7];
    			}
    		}
    		Object.DestroyImmediate((Object)(object)val6);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("If fountain_only is near zero, the fountain's own renderers are not");
    		stringBuilder.AppendLine("drawing and the restore question is moot until that is fixed.");
    		Finish(stringBuilder);
    	}

    	private static int Render(StringBuilder sb, Camera cam, RenderTexture rt, string name)
    	{
    		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Expected Obj, but got Unknown
    		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
    		cam.Render();
    		RenderTexture active = RenderTexture.active;
    		RenderTexture.active = rt;
    		Texture2D val = new Texture2D(800, 500, (TextureFormat)3, false);
    		val.ReadPixels(new Rect(0f, 0f, 800f, 500f), 0, 0);
    		val.Apply();
    		RenderTexture.active = active;
    		Color[] pixels = val.GetPixels();
    		int num = 0;
    		for (int i = 0; i < pixels.Length; i++)
    		{
    			if (pixels[i].r + pixels[i].g + pixels[i].b > 0.06f)
    			{
    				num++;
    			}
    		}
    		byte[] array = ImageConversion.EncodeToPNG(val);
    		File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Temp/oc_" + name + ".png"), array);
    		Object.DestroyImmediate((Object)(object)val);
    		sb.AppendLine();
    		sb.AppendLine(name + "  ->  Temp/oc_" + name + ".png  (" + array.Length.ToString("N0") + " bytes)");
    		return num;
    	}

    	private static string PathOf(Transform t)
    	{
    		if ((Object)(object)t == (Object)null)
    		{
    			return "<none>";
    		}
    		StringBuilder stringBuilder = new StringBuilder(((Object)t).name);
    		Transform parent = t.parent;
    		while ((Object)(object)parent != (Object)null)
    		{
    			stringBuilder.Insert(0, ((Object)parent).name + "/");
    			parent = parent.parent;
    		}
    		return stringBuilder.ToString();
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), "Temp/fountain_occlusion.txt")));
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/fountain_occlusion.txt"), sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}