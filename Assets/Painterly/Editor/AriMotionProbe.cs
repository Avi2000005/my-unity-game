using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class AriMotionProbe
    {
    	private const string Dir = "Assets/Art/Ari/Models";

    	private static readonly string[] Watch = new string[20]
    	{
    		"RootT.x", "RootT.y", "RootT.z", "Left Upper Leg Front-Back", "Right Upper Leg Front-Back", "Left Lower Leg Stretch", "Right Lower Leg Stretch", "Left Foot Up-Down", "Right Foot Up-Down", "Left Arm Down-Up",
    		"Right Arm Down-Up", "Spine Front-Back", "Head Nod Down-Up", "UpperChest Front-Back", "UpperChest Left-Right", "UpperChest Twist Left-Right", "Left Eye Down-Up", "Left Eye In-Out", "Right Eye Down-Up", "Right Eye In-Out"
    	};

    	[MenuItem("Tools/Echoes/Probe Ari Motion", priority = 91)]
    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		string[] array = new string[3] { "Ari_character", "Idle", "Walking" };
    		foreach (string model in array)
    		{
    			ProbeMotion(stringBuilder, model);
    			ProbeSkeleton(stringBuilder, model);
    		}
    		File.WriteAllText("Temp/ari_motion.txt", stringBuilder.ToString());
    		Debug.Log((object)("[Echoes] Ari motion probe -> Temp/ari_motion.txt\n" + stringBuilder));
    	}

    	private static void ProbeMotion(StringBuilder sb, string model)
    	{
    		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
    		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
    		string text = "Assets/Art/Ari/Models/" + model + ".fbx";
    		sb.AppendLine("=== MOTION: " + model + " ===");
    		AnimationClip[] array = (from c in AssetDatabase.LoadAllAssetsAtPath(text).OfType<AnimationClip>()
    			where !((Object)c).name.StartsWith("__preview__")
    			select c).ToArray();
    		if (array.Length == 0)
    		{
    			sb.AppendLine("  no clips\n");
    			return;
    		}
    		AnimationClip[] array2 = array;
    		foreach (AnimationClip val in array2)
    		{
    			EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(val);
    			sb.AppendLine($"  '{((Object)val).name}' len={val.length:0.000}s " + $"curves={curveBindings.Length} loop={((Motion)val).isLooping}");
    			double num2 = 0.0;
    			string[] watch = Watch;
    			foreach (string prop in watch)
    			{
    				float num4 = float.MaxValue;
    				float num5 = float.MinValue;
    				int num6 = 0;
    				foreach (EditorCurveBinding item in curveBindings.Where((EditorCurveBinding b) =>
    				{
    					//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    					return b.propertyName == prop;
    				}))
    				{
    					AnimationCurve editorCurve = AnimationUtility.GetEditorCurve(val, item);
    					if (editorCurve != null && editorCurve.length != 0)
    					{
    						Keyframe[] keys = editorCurve.keys;
    						for (int num7 = 0; num7 < keys.Length; num7++)
    						{
    							Keyframe val2 = keys[num7];
    							num4 = Math.Min(num4, val2.value);
    							num5 = Math.Max(num5, val2.value);
    						}
    						num6 += editorCurve.length;
    					}
    				}
    				if (num6 != 0)
    				{
    					float num8 = num5 - num4;
    					num2 = Math.Max(num2, num8);
    					sb.AppendLine($"      {prop,-28} {num4,8:0.000} .. {num5,8:0.000}  " + $"range {num8,7:0.000}  keys {num6}");
    				}
    			}
    			sb.AppendLine($"      => widest muscle range {num2:0.000}  " + ((num2 < 0.01) ? "STATIC (rest pose)" : "ANIMATED") + "\n");
    		}
    	}

    	private static void ProbeSkeleton(StringBuilder sb, string model)
    	{
    		GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Ari/Models/" + model + ".fbx");
    		sb.AppendLine("=== SKELETON: " + model + " ===");
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("  not found\n");
    			return;
    		}
    		Transform[] componentsInChildren = val.GetComponentsInChildren<Transform>(true);
    		Transform val2 = componentsInChildren.FirstOrDefault((Transform t) => ((Object)t).name == "Hips") ?? componentsInChildren.FirstOrDefault();
    		if ((Object)(object)val2 == (Object)null)
    		{
    			sb.AppendLine("  no transforms\n");
    			return;
    		}
    		int num = 0;
    		Transform parent = val2.parent;
    		while ((Object)(object)parent != (Object)null)
    		{
    			num++;
    			parent = parent.parent;
    		}
    		Walk(val2, num);
    		sb.AppendLine();
    		void Walk(Transform t, int d)
    		{
    			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
    			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
    			//IL_008f: Expected Obj, but got Unknown
    			Vector3 localPosition = t.localPosition;
    			sb.AppendLine("  " + new string(' ', d * 2) + ((Object)t).name + " " + $"({localPosition.x:0.000}, {localPosition.y:0.000}, {localPosition.z:0.000})");
    			foreach (Transform item in t)
    			{
    				Walk(item, d + 1);
    			}
    		}
    	}
    }
}