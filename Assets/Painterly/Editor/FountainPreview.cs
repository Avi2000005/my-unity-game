using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class FountainPreview
    {
    	private const string Report = "Temp/fountain_preview.txt";

    	private const int W = 960;

    	private const int H = 640;

    	public static void Run()
    	{
    		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ec: Expected Obj, but got Unknown
    		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
    		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0216: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
    		//IL_022c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
    		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
    		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_028f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
    		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
    		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0364: Expected Obj, but got Unknown
    		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03fa: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0419: Expected Obj, but got Unknown
    		//IL_0447: Expected Obj, but got Unknown
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] fountain preview");
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			stringBuilder.AppendLine("STOPPED: exit play mode first (Ctrl+P).");
    			Finish(stringBuilder);
    			return;
    		}
    		GameObject val = GameObject.Find("Fountain");
    		if ((Object)(object)val == (Object)null)
    		{
    			stringBuilder.AppendLine("FATAL: no 'Fountain' in the scene. Run Tools/Echoes/Build Fountain.");
    			Finish(stringBuilder);
    			return;
    		}
    		ColorRestoreTarget component = val.GetComponent<ColorRestoreTarget>();
    		Bounds val2 = new Bounds(val.transform.position, Vector3.zero);
    		Renderer[] componentsInChildren = val.GetComponentsInChildren<Renderer>(true);
    		foreach (Renderer val3 in componentsInChildren)
    		{
    			val2.Encapsulate(val3.bounds);
    		}
    		Vector3 center = val2.center;
    		stringBuilder.AppendLine($"fountain centre : {center}");
    		stringBuilder.AppendLine($"fountain size   : {val2.size}");
    		GameObject val4 = new GameObject("__FountainPreviewCam");
    		Camera val5 = val4.AddComponent<Camera>();
    		val5.fieldOfView = 42f;
    		val5.nearClipPlane = 0.05f;
    		val5.farClipPlane = 500f;
    		Type type = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
    		if (type != null)
    		{
    			val4.AddComponent(type);
    		}
    		Camera[] array = Object.FindObjectsByType<Camera>((FindObjectsInactive)1);
    		bool[] array2 = new bool[array.Length];
    		for (int j = 0; j < array.Length; j++)
    		{
    			array2[j] = ((Behaviour)array[j]).enabled;
    			((Behaviour)array[j]).enabled = false;
    		}
    		var array3 = new[]
    		{
    			new
    			{
    				name = "01_front",
    				eye = center + new Vector3(0f, 2.6f, -7.4f),
    				look = center + new Vector3(0f, 0.5f, 0f),
    				restore = 0f
    			},
    			new
    			{
    				name = "02_three_quarter",
    				eye = center + new Vector3(5.4f, 3f, -5.6f),
    				look = center + new Vector3(0f, 0.5f, 0f),
    				restore = 0f
    			},
    			new
    			{
    				name = "03_statue_close",
    				eye = center + new Vector3(1.5f, 1.55f, -1.85f),
    				look = center + new Vector3(0.25f, 0.95f, 0.25f),
    				restore = 0f
    			},
    			new
    			{
    				name = "04_side",
    				eye = center + new Vector3(-6.8f, 2.8f, 2.2f),
    				look = center + new Vector3(0f, 0.5f, 0f),
    				restore = 0f
    			},
    			new
    			{
    				name = "05_restored",
    				eye = center + new Vector3(5.4f, 3f, -5.6f),
    				look = center + new Vector3(0f, 0.5f, 0f),
    				restore = 1f
    			},
    			new
    			{
    				name = "06_restored_top",
    				eye = center + new Vector3(0.4f, 6.2f, -3.4f),
    				look = center + new Vector3(0f, 0.3f, 0f),
    				restore = 1f
    			}
    		};
    		Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), "Temp/fountain_preview.txt")));
    		RenderTexture val6 = (val5.targetTexture = new RenderTexture(960, 640, 24, (RenderTextureFormat)0)
    		{
    			antiAliasing = 4
    		});
    		var array4 = array3;
    		foreach (var anon in array4)
    		{
    			if ((Object)(object)component != (Object)null)
    			{
    				component.SetRestoreImmediate(anon.restore);
    			}
    			val4.transform.position = anon.eye;
    			val4.transform.LookAt(anon.look, Vector3.up);
    			val5.Render();
    			RenderTexture active = RenderTexture.active;
    			RenderTexture.active = val6;
    			Texture2D val8 = new Texture2D(960, 640, (TextureFormat)3, false);
    			val8.ReadPixels(new Rect(0f, 0f, 960f, 640f), 0, 0);
    			val8.Apply();
    			RenderTexture.active = active;
    			byte[] array5 = ImageConversion.EncodeToPNG(val8);
    			File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Temp/fountain_" + anon.name + ".png"), array5);
    			Object.DestroyImmediate((Object)val8);
    			stringBuilder.AppendLine($"  {anon.name,-18} restore={anon.restore:F0}  {array5.Length:N0} bytes -> Temp/fountain_{anon.name}.png");
    		}
    		if ((Object)(object)component != (Object)null)
    		{
    			component.SetRestoreImmediate(0f);
    		}
    		val5.targetTexture = null;
    		Object.DestroyImmediate((Object)(object)val6);
    		for (int k = 0; k < array.Length; k++)
    		{
    			if ((Object)(object)array[k] != (Object)null)
    			{
    				((Behaviour)array[k]).enabled = array2[k];
    			}
    		}
    		Object.DestroyImmediate((Object)(object)val4);
    		stringBuilder.AppendLine("done. Fountain left in the Grey Realm (restore=0).");
    		Finish(stringBuilder);
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), "Temp/fountain_preview.txt")));
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/fountain_preview.txt"), sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}