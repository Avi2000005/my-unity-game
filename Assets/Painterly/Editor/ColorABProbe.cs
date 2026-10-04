using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;

namespace Echoes.Painterly.EditorTools
{

    public static class ColorABProbe
    {
    	[Serializable]
    	private class Stash
    	{
    		public int ambientMode;

    		public Color ambientSky;

    		public Color ambientEquator;

    		public Color ambientGround;

    		public float ambientIntensity;

    		public float reflectionIntensity;

    		public bool fog;

    		public string skyboxName;
    	}

    	private struct Sat
    	{
    		public float mean;

    		public int pixels;
    	}

    	private const string StashPath = "Temp/color_ab_stash.json";

    	private const string CameraName = "__AB_Camera";

    	private const string GreyPng = "Assets/Temp/ab_grey.png";

    	private const string ColourPng = "Assets/Temp/ab_colour.png";

    	private const byte MinChroma = 6;

    	private static string ProjectPath(params string[] parts)
    	{
    		string text = Path.GetDirectoryName(Application.dataPath);
    		foreach (string path in parts)
    		{
    			text = Path.Combine(text, path);
    		}
    		return text;
    	}

    	[MenuItem("Tools/Echoes/Probe/1. Neutralise Environment", priority = 40)]
    	public static void Neutralise()
    	{
    		StashAndNeutralise();
    		Debug.Log((object)"[Echoes] Environment neutralised for the A/B probe. Screenshot both states, then run '4. Report Contrast'.");
    	}

    	[MenuItem("Tools/Echoes/Probe/2. Frame Village", priority = 41)]
    	public static void FrameVillage()
    	{
    		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
    		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
    		GameObject val = GameObject.Find("Village_Grey");
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] No Village_Grey in the scene. Run Generate Grey Village first.");
    			return;
    		}
    		SceneView lastActiveSceneView = SceneView.lastActiveSceneView;
    		if ((Object)(object)lastActiveSceneView == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] Open a Scene View before framing.");
    			return;
    		}
    		Bounds val2 = new Bounds(val.transform.position, Vector3.zero);
    		Renderer[] componentsInChildren = val.GetComponentsInChildren<Renderer>(true);
    		foreach (Renderer val3 in componentsInChildren)
    		{
    			if (!(((Object)((Component)val3).transform).name == "Ground"))
    			{
    				val2.Encapsulate(val3.bounds);
    			}
    		}
    		lastActiveSceneView.LookAt(val2.center, Quaternion.Euler(32f, 38f, 0f));
    		lastActiveSceneView.pivot = val2.center;
    		lastActiveSceneView.size = Mathf.Max(val2.extents.x, val2.extents.z) * 1.15f;
    		lastActiveSceneView.sceneViewState.alwaysRefresh = true;
    		((EditorWindow)lastActiveSceneView).Repaint();
    		Debug.Log((object)($"[Echoes] Framed village at {val2.center} " + $"(extent {val2.size.x:F0} x {val2.size.z:F0} x {val2.size.y:F0})."));
    	}

    	[MenuItem("Tools/Echoes/Probe/3a. Set Grey (Restore = 0)", priority = 42)]
    	public static void SetGrey()
    	{
    		SetRestore(0f);
    	}

    	[MenuItem("Tools/Echoes/Probe/3b. Set Colour (Restore = 1)", priority = 43)]
    	public static void SetColour()
    	{
    		SetRestore(1f);
    	}

    	private static void SetRestore(float value)
    	{
    		GameObject val = GameObject.Find("Village_Grey");
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] No Village_Grey in the scene.");
    			return;
    		}
    		ColorRestoreTarget.SetAllImmediate(value);
    		SceneView lastActiveSceneView = SceneView.lastActiveSceneView;
    		if (lastActiveSceneView != null)
    		{
    			((EditorWindow)lastActiveSceneView).Repaint();
    		}
    		Debug.Log((object)($"[Echoes] _ColorRestore set to {value} on " + $"{val.GetComponentsInChildren<ColorRestoreTarget>(true).Length} target(s)."));
    	}

    	[MenuItem("Tools/Echoes/Probe/4. Report Contrast", priority = 44)]
    	public static void Report()
    	{
    		Sat sat = MeanSaturation("Assets/Temp/ab_grey.png");
    		Sat sat2 = MeanSaturation("Assets/Temp/ab_colour.png");
    		if (sat.pixels == 0 || sat2.pixels == 0)
    		{
    			Debug.LogError((object)"[Echoes] Missing probe PNGs. Capture both states first (grey and colour) before reporting.");
    			return;
    		}
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] Grey/Colour contrast");
    		stringBuilder.AppendLine($"  _ColorRestore = 0   mean saturation {sat.mean:F4}  over {sat.pixels} px");
    		stringBuilder.AppendLine($"  _ColorRestore = 1   mean saturation {sat2.mean:F4}  over {sat2.pixels} px");
    		stringBuilder.AppendLine($"  lift {sat2.mean - sat.mean:F4}   ratio {sat2.mean / Mathf.Max(0.0001f, sat.mean):F1}x");
    		if (sat.mean > 0.12f)
    		{
    			stringBuilder.AppendLine("  VERDICT FAIL: the grey pass is not actually grey.");
    		}
    		else if (sat2.mean < sat.mean * 1.5f)
    		{
    			stringBuilder.AppendLine("  VERDICT FAIL: restoring colour barely moved saturation.");
    		}
    		else
    		{
    			stringBuilder.AppendLine("  VERDICT PASS: the village is grey, and painting restores colour.");
    		}
    		Debug.Log((object)stringBuilder.ToString());
    	}

    	private static Sat MeanSaturation(string relativePath)
    	{
    		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002c: Expected Obj, but got Unknown
    		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
    		string path = ProjectPath(relativePath.Split('/', StringSplitOptions.None));
    		if (!File.Exists(path))
    		{
    			return default;
    		}
    		Texture2D val = new Texture2D(2, 2, (TextureFormat)4, false);
    		if (!ImageConversion.LoadImage(val, File.ReadAllBytes(path)))
    		{
    			return default;
    		}
    		Color32[] pixels = val.GetPixels32();
    		double num = 0.0;
    		int num2 = 0;
    		foreach (Color32 val2 in pixels)
    		{
    			if (val2.a >= 128)
    			{
    				int num3 = Mathf.Max((int)val2.r, Mathf.Max((int)val2.g, (int)val2.b));
    				int num4 = Mathf.Min((int)val2.r, Mathf.Min((int)val2.g, (int)val2.b));
    				if (num3 - num4 >= 6)
    				{
    					num += (double)(num3 - num4) / (double)num3;
    					num2++;
    				}
    			}
    		}
    		Object.DestroyImmediate((Object)(object)val);
    		return new Sat
    		{
    			mean = ((num2 > 0) ? ((float)(num / (double)num2)) : 0f),
    			pixels = num2
    		};
    	}

    	[MenuItem("Tools/Echoes/Probe/5. Restore Environment", priority = 45)]
    	public static void RestoreEnvironment()
    	{
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		string path = ProjectPath("Temp/color_ab_stash.json");
    		if (!File.Exists(path))
    		{
    			Debug.LogWarning((object)"[Echoes] No stashed environment to restore.");
    			return;
    		}
    		Stash stash = JsonUtility.FromJson<Stash>(File.ReadAllText(path));
    		RenderSettings.ambientMode = (AmbientMode)stash.ambientMode;
    		RenderSettings.ambientSkyColor = stash.ambientSky;
    		RenderSettings.ambientEquatorColor = stash.ambientEquator;
    		RenderSettings.ambientGroundColor = stash.ambientGround;
    		RenderSettings.ambientIntensity = stash.ambientIntensity;
    		RenderSettings.reflectionIntensity = stash.reflectionIntensity;
    		RenderSettings.fog = stash.fog;
    		string[] array = AssetDatabase.FindAssets("t:Material " + stash.skyboxName);
    		if (array != null && array.Length != 0)
    		{
    			Material val = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(array[0]));
    			if ((Object)(object)val != (Object)null && (Object)(object)val.shader != (Object)null && ((Object)val.shader).name == "Skybox/Procedural")
    			{
    				RenderSettings.skybox = val;
    			}
    		}
    		Debug.Log((object)($"[Echoes] Environment restored: ambientMode={stash.ambientMode}, " + $"fog={stash.fog}, reflectionIntensity={stash.reflectionIntensity}, " + "skybox='" + stash.skyboxName + "'. If the skybox looks wrong, re-assign it on the Lighting panel."));
    	}

    	private static void StashAndNeutralise()
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0010: Expected I4, but got Unknown
    		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
    		Stash stash = new Stash
    		{
    			ambientMode = (int)RenderSettings.ambientMode,
    			ambientSky = RenderSettings.ambientSkyColor,
    			ambientEquator = RenderSettings.ambientEquatorColor,
    			ambientGround = RenderSettings.ambientGroundColor,
    			ambientIntensity = RenderSettings.ambientIntensity,
    			reflectionIntensity = RenderSettings.reflectionIntensity,
    			fog = RenderSettings.fog,
    			skyboxName = (((Object)(object)RenderSettings.skybox != (Object)null) ? AssetDatabase.GetAssetPath((Object)(object)RenderSettings.skybox) : "(none)")
    		};
    		File.WriteAllText(ProjectPath("Temp/color_ab_stash.json"), JsonUtility.ToJson((object)stash));
    		RenderSettings.skybox = null;
    		RenderSettings.ambientMode = (AmbientMode)3;
    		RenderSettings.ambientSkyColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    		RenderSettings.ambientIntensity = 1f;
    		RenderSettings.reflectionIntensity = 0f;
    		RenderSettings.fog = false;
    	}
    }
}