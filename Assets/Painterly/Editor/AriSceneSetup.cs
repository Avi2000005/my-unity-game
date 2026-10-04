using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class AriSceneSetup
    {
    	private const string ModelsDir = "Assets/Art/Ari/Models";

    	private const string CharacterPath = "Assets/Art/Ari/Models/Ari_character.fbx";

    	private const string ControllerPath = "Assets/Art/Ari/Ari.controller";

    	private const string MaterialPath = "Assets/Painterly/Materials/Ari_Painterly.mat";

    	private const string ShaderName = "Echoes/PainterlyLit";

    	private const string RootName = "Ari";

    	[MenuItem("Tools/Echoes/Place Ari", priority = 62)]
    	public static void Run()
    	{
    		try
    		{
    			RunInner();
    		}
    		catch (Exception ex)
    		{
    			File.WriteAllText("Temp/ari_scene_error.txt", ex.ToString());
    			Debug.LogError((object)("[Echoes] Place Ari failed\n" + ex));
    		}
    	}

    	private static void RunInner()
    	{
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0134: Expected Obj, but got Unknown
    		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
    		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0401: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Ari/Models/Ari_character.fbx");
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] Assets/Art/Ari/Models/Ari_character.fbx not found. Run Tools/Echoes/Import Ari.");
    			return;
    		}
    		RuntimeAnimatorController val2 = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Ari/Ari.controller");
    		if ((Object)(object)val2 == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] Assets/Art/Ari/Ari.controller not found. Run Tools/Echoes/Build Ari Controller.");
    			return;
    		}
    		Material orCreateMaterial = GetOrCreateMaterial(stringBuilder);
    		Scene scene = GetScene();
    		Avatar val3 = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Ari/Models/Ari_character.fbx").OfType<Avatar>().FirstOrDefault((Avatar a) => a.isValid && a.isHuman);
    		if ((Object)(object)val3 == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] no valid humanoid avatar in Assets/Art/Ari/Models/Ari_character.fbx. Run Tools/Echoes/Import Ari.");
    			return;
    		}
    		GameObject val4 = GameObject.Find("Ari");
    		if ((Object)(object)val4 != (Object)null)
    		{
    			Animator component = val4.GetComponent<Animator>();
    			if ((Object)(object)component == (Object)null || (Object)(object)component.avatar == (Object)null || !component.avatar.isValid || !component.avatar.isHuman)
    			{
    				Object.DestroyImmediate((Object)(object)val4);
    				val4 = null;
    				stringBuilder.AppendLine("discarded a broken Ari left by an earlier run");
    			}
    		}
    		GameObject val5;
    		if ((Object)(object)val4 != (Object)null)
    		{
    			val5 = val4;
    			stringBuilder.AppendLine("reusing the existing Ari object");
    		}
    		else
    		{
    			val5 = new GameObject("Ari");
    			stringBuilder.AppendLine("created Ari");
    		}
    		if (val5.scene != scene)
    		{
    			SceneManager.MoveGameObjectToScene(val5, scene);
    		}
    		Transform val6 = val5.transform.Find("Model");
    		GameObject val7;
    		if ((Object)(object)val6 != (Object)null)
    		{
    			val7 = ((Component)val6).gameObject;
    		}
    		else
    		{
    			val7 = Object.Instantiate<GameObject>(val, val5.transform);
    			((Object)val7).name = "Model";
    		}
    		val7.transform.localPosition = Vector3.zero;
    		val7.transform.localRotation = val.transform.localRotation;
    		val7.transform.localScale = val.transform.localScale;
    		Renderer[] componentsInChildren = val7.GetComponentsInChildren<Renderer>(true);
    		Renderer[] array = componentsInChildren;
    		foreach (Renderer val8 in array)
    		{
    			Material[] sharedMaterials = val8.sharedMaterials;
    			for (int num2 = 0; num2 < sharedMaterials.Length; num2++)
    			{
    				sharedMaterials[num2] = orCreateMaterial;
    			}
    			val8.sharedMaterials = sharedMaterials;
    		}
    		stringBuilder.AppendLine($"{componentsInChildren.Length} renderer(s) -> {((Object)orCreateMaterial).name}");
    		Animator val9 = val5.GetComponent<Animator>() ?? val7.GetComponent<Animator>();
    		if ((Object)(object)val9 == (Object)null)
    		{
    			val9 = val5.AddComponent<Animator>();
    		}
    		if ((Object)(object)val9 == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] could not obtain an Animator for Ari");
    			return;
    		}
    		val9.avatar = val3;
    		val9.runtimeAnimatorController = val2;
    		val9.applyRootMotion = false;
    		val9.cullingMode = (AnimatorCullingMode)0;
    		val9.updateMode = (AnimatorUpdateMode)0;
    		((Behaviour)val9).enabled = false;
    		stringBuilder.AppendLine("animator on '" + ((Object)val9).name + "': controller=" + ((Object)val2).name + " avatar=" + (((Object)(object)val9.avatar == (Object)null) ? "<none>" : ((Object)val9.avatar).name) + " " + $"valid={(Object)(object)val9.avatar != (Object)null && val9.avatar.isValid} " + $"human={(Object)(object)val9.avatar != (Object)null && val9.avatar.isHuman} " + "enabled=false (the movement script will turn this on)");
    		if (val5.transform.position == Vector3.zero)
    		{
    			val5.transform.position = new Vector3(0f, 0.05f, 0f);
    			val5.transform.rotation = Quaternion.identity;
    		}
    		stringBuilder.AppendLine($"position {val5.transform.position}");
    		EditorUtility.SetDirty((Object)(object)val5);
    		EditorSceneManager.MarkSceneDirty(scene);
    		AssetDatabase.SaveAssets();
    		VerifyRetarget(stringBuilder, val7, val9.avatar);
    		VerifyMaterial(stringBuilder, orCreateMaterial);
    		File.WriteAllText("Temp/ari_scene.txt", stringBuilder.ToString());
    		Debug.Log((object)("[Echoes] Ari placed\n" + stringBuilder));
    	}

    	private static void VerifyRetarget(StringBuilder sb, GameObject modelGO, Avatar avatar)
    	{
    		//IL_0218: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("--- retarget check (sampling the walk cycle) ---");
    		AnimationClip val = ((IEnumerable<string>)AssetDatabase.FindAssets("t:AnimationClip", new string[1] { "Assets/Art/Ari/Models" })).Select((Func<string, string>)AssetDatabase.GUIDToAssetPath).Distinct().SelectMany((Func<string, IEnumerable<Object>>)AssetDatabase.LoadAllAssetsAtPath)
    			.OfType<AnimationClip>()
    			.FirstOrDefault((AnimationClip c) => ((Object)c).name == "Ari_Walk");
    		if ((Object)(object)val == (Object)null)
    		{
    			IEnumerable<string> values = from c in ((IEnumerable<string>)AssetDatabase.FindAssets("t:AnimationClip", new string[1] { "Assets/Art/Ari/Models" })).Select((Func<string, string>)AssetDatabase.GUIDToAssetPath).Distinct().SelectMany((Func<string, IEnumerable<Object>>)AssetDatabase.LoadAllAssetsAtPath)
    					.OfType<AnimationClip>()
    				where !((Object)c).name.StartsWith("__")
    				select "'" + ((Object)c).name + "'";
    			sb.AppendLine("  Ari_Walk missing; clips present: " + string.Join(", ", values));
    			return;
    		}
    		Transform[] array = new string[2] { "LeftFoot", "RightFoot" }.Select((string n) => FindBone(modelGO, n)).ToArray();
    		if (array.Any((Transform f) => (Object)(object)f == (Object)null))
    		{
    			sb.AppendLine("  foot bones not found: " + string.Join(", ", new string[2] { "LeftFoot", "RightFoot" }.Where((string n) => (Object)(object)FindBone(modelGO, n) == (Object)null)));
    			return;
    		}
    		float[] lowest = new float[array.Length];
    		float lift = 0f;
    		for (int num = 0; num <= 24; num++)
    		{
    			float num2 = val.length * (float)num / 24f;
    			val.SampleAnimation(modelGO, num2);
    			for (int num3 = 0; num3 < array.Length; num3++)
    			{
    				float y = array[num3].position.y;
    				if (num == 0 || y < lowest[num3])
    				{
    					lowest[num3] = y;
    				}
    				lift = Mathf.Max(lift, y);
    			}
    		}
    		float[] array2 = array.Select((Transform f, int i) => lift - lowest[i]).ToArray();
    		sb.AppendLine($"  clip '{((Object)val).name}' len={val.length:0.000}s over {24} samples");
    		for (int num4 = 0; num4 < array.Length; num4++)
    		{
    			sb.AppendLine($"  {((Object)array[num4]).name,-10} rises {array2[num4]:0.0000} " + $"(lowest y {lowest[num4]:0.0000}, highest {lift:0.0000})");
    		}
    		float num5 = array2.Max();
    		string text;
    		if (num5 > 0.05f)
    		{
    			text = $"RETARGET OK — a walk cycle lifts a foot {num5:0.000}";
    		}
    		else
    		{
    			text = ((num5 > 0.0005f) ? $"WEAK — foot only moves {num5:0.0005}, expect a glide rather than a walk" : ("FAILED — feet do not move at all; avatar=" + ((avatar != null) ? ((Object)avatar).name : null) + " " + $"valid={((avatar != null) ? new bool?(avatar.isValid) : ((bool?)null))} human={((avatar != null) ? new bool?(avatar.isHuman) : ((bool?)null))}"));
    		}
    		sb.AppendLine("  => " + text);
    		val.SampleAnimation(modelGO, 0f);
    	}

    	private static Transform FindBone(GameObject go, string name)
    	{
    		return go.GetComponentsInChildren<Transform>(true).FirstOrDefault((Transform t) => ((Object)t).name == name);
    	}

    	private static void VerifyMaterial(StringBuilder sb, Material material)
    	{
    		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("--- material check ---");
    		string name = ((Object)material).name;
    		Shader shader = material.shader;
    		sb.AppendLine("  " + name + " shader=" + ((shader != null) ? ((Object)shader).name : null));
    		Texture texture = material.GetTexture("_BaseMap");
    		sb.AppendLine("  _BaseMap=" + (((Object)(object)texture == (Object)null) ? "<none>" : ((Object)texture).name) + " " + string.Format("_BaseColor={0} ", material.GetColor("_BaseColor")) + string.Format("_ColorRestore={0}", material.GetFloat("_ColorRestore")));
    		if ((Object)(object)texture == (Object)null)
    		{
    			sb.AppendLine("  WARNING: no albedo texture and the mesh has no vertex colours, so Ari renders flat. _ColorRestore cannot help — the shader desaturates an albedo that is not there. Re-download the character from Mixamo with textures included and re-run Import Ari.");
    		}
    	}

    	private static Material GetOrCreateMaterial(StringBuilder sb)
    	{
    		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0056: Expected Obj, but got Unknown
    		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
    		Shader val = Shader.Find("Echoes/PainterlyLit");
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] shader Echoes/PainterlyLit not found. Run Tools/Echoes/Build Village Materials.");
    			return null;
    		}
    		Directory.CreateDirectory(Path.GetDirectoryName("Assets/Painterly/Materials/Ari_Painterly.mat"));
    		Material val2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Painterly/Materials/Ari_Painterly.mat");
    		if ((Object)(object)val2 == (Object)null)
    		{
    			val2 = new Material(val)
    			{
    				name = "Ari_Painterly"
    			};
    			AssetDatabase.CreateAsset((Object)(object)val2, "Assets/Painterly/Materials/Ari_Painterly.mat");
    			sb.AppendLine("created Assets/Painterly/Materials/Ari_Painterly.mat");
    		}
    		else
    		{
    			val2.shader = val;
    			sb.AppendLine("reusing Assets/Painterly/Materials/Ari_Painterly.mat");
    		}
    		val2.SetColor("_BaseColor", new Color(0.72f, 0.68f, 0.64f, 1f));
    		val2.SetFloat("_Smoothness", 0.25f);
    		val2.SetFloat("_Metallic", 0f);
    		val2.SetFloat("_ColorRestore", 1f);
    		val2.SetFloat("_RestoreBoost", 1f);
    		EditorUtility.SetDirty((Object)(object)val2);
    		return val2;
    	}

    	private static Scene GetScene()
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		Scene result = SceneManager.GetActiveScene();
    		if (result.IsValid() && result.isLoaded)
    		{
    			return result;
    		}
    		result = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
    		Debug.Log((object)"[Echoes] opened SampleScene");
    		return result;
    	}
    }
}