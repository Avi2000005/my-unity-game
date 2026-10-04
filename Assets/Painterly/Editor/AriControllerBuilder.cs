using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class AriControllerBuilder
    {
    	private const string ControllerPath = "Assets/Art/Ari/Ari.controller";

    	private const string ModelsDir = "Assets/Art/Ari/Models";

    	private static readonly string[] ClipDirs = new string[2] { "Assets/Art/Ari/Models", "Assets/Art/L1" };

    	private const string Report = "Temp/ari_controller.txt";

    	public const string SpeedParam = "Speed";

    	public const float Threshold = 0.1f;

    	private const float BlendSeconds = 0.15f;

    	private const string IdleClip = "Ari_Idle";

    	private const string WalkClip = "Ari_Walk";

    	private const string JumpClip = "Ari_Jump";

    	private const string SwingClip = "Ari_Attack";

    	private const float SwingSpeed = 4f;

    	private const float SwingBackExitTime = 0.514f;

    	public const string SwingParam = "Swing";

    	public const string SwingingParam = "Swinging";

    	private const float SwingBlendSeconds = 0.12f;

    	private const float SwingInSeconds = 0.02f;

    	public const string JumpParam = "Jump";

    	public const string GroundedParam = "Grounded";

    	private const float LandBlendSeconds = 0.12f;

    	private const float LaunchBlendSeconds = 0.05f;

    	[MenuItem("Tools/Echoes/Build Ari Controller", priority = 61)]
    	public static void Run()
    	{
    		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b8: Expected Obj, but got Unknown
    		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d6: Expected Obj, but got Unknown
    		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f4: Expected Obj, but got Unknown
    		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
    		//IL_020c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0218: Expected Obj, but got Unknown
    		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
    		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0229: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
    		//IL_023c: Expected Obj, but got Unknown
    		//IL_054d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0552: Unknown result type (might be due to invalid IL or missing references)
    		AnimationClip[] found = (from c in ClipDirs.SelectMany((string dir) => AssetDatabase.FindAssets("t:AnimationClip", new string[1] { dir })).Select((Func<string, string>)AssetDatabase.GUIDToAssetPath).Distinct()
    				.SelectMany((Func<string, IEnumerable<Object>>)AssetDatabase.LoadAllAssetsAtPath)
    				.OfType<AnimationClip>()
    			where !c.legacy && !((Object)c).name.StartsWith("__")
    			select c).ToArray();
    		string[] source = new string[4] { "Ari_Idle", "Ari_Walk", "Ari_Jump", "Ari_Attack" };
    		AnimationClip[] clips = source.Select((string name) => found.FirstOrDefault((AnimationClip c) => ((Object)c).name == name)).ToArray();
    		string[] array = source.Where((string _, int i) => (Object)(object)clips[i] == (Object)null).ToArray();
    		if (array.Length != 0)
    		{
    			Debug.LogError((object)("[Echoes] Ari controller: missing clip(s) " + string.Join(", ", array) + ". Clips actually present: " + ((found.Length == 0) ? "<none>" : string.Join(", ", found.Select((AnimationClip c) => "'" + ((Object)c).name + "'"))) + ". Run Tools/Echoes/Import Ari first."));
    			return;
    		}
    		if ((Object)(object)AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/Ari/Ari.controller") != (Object)null)
    		{
    			AssetDatabase.DeleteAsset("Assets/Art/Ari/Ari.controller");
    		}
    		AnimatorController val = AnimatorController.CreateAnimatorControllerAtPath("Assets/Art/Ari/Ari.controller");
    		val.AddParameter(new AnimatorControllerParameter
    		{
    			name = "Speed",
    			type = (AnimatorControllerParameterType)1,
    			defaultFloat = 0f
    		});
    		val.AddParameter(new AnimatorControllerParameter
    		{
    			name = "Jump",
    			type = (AnimatorControllerParameterType)9
    		});
    		val.AddParameter(new AnimatorControllerParameter
    		{
    			name = "Swing",
    			type = (AnimatorControllerParameterType)9
    		});
    		val.AddParameter(new AnimatorControllerParameter
    		{
    			name = "Swinging",
    			type = (AnimatorControllerParameterType)4,
    			defaultBool = false
    		});
    		val.AddParameter(new AnimatorControllerParameter
    		{
    			name = "Grounded",
    			type = (AnimatorControllerParameterType)4,
    			defaultBool = true
    		});
    		AnimatorStateMachine machine = val.layers[0].stateMachine;
    		Dictionary<string, AnimatorState> dictionary = clips.ToDictionary((AnimationClip c) => ((Object)c).name, (AnimationClip c) =>
    		{
    			AnimatorState val8 = machine.AddState(((Object)c).name);
    			val8.motion = (Motion)(object)c;
    			val8.writeDefaultValues = false;
    			return val8;
    		});
    		dictionary["Ari_Attack"].speed = 4f;
    		machine.defaultState = dictionary["Ari_Idle"];
    		CrossFade(dictionary["Ari_Idle"], dictionary["Ari_Walk"], (AnimatorConditionMode)3);
    		CrossFade(dictionary["Ari_Walk"], dictionary["Ari_Idle"], (AnimatorConditionMode)4);
    		Launch(dictionary["Ari_Idle"], dictionary["Ari_Jump"]);
    		Launch(dictionary["Ari_Walk"], dictionary["Ari_Jump"]);
    		Land(dictionary["Ari_Jump"], dictionary["Ari_Walk"], (AnimatorConditionMode)3);
    		Land(dictionary["Ari_Jump"], dictionary["Ari_Idle"], (AnimatorConditionMode)4);
    		AnimatorStateTransition val2 = dictionary["Ari_Jump"].AddTransition(dictionary["Ari_Idle"]);
    		val2.hasExitTime = true;
    		val2.exitTime = 1f;
    		val2.hasFixedDuration = true;
    		val2.duration = 0.12f;
    		val2.canTransitionToSelf = false;
    		Swing(dictionary["Ari_Idle"], dictionary["Ari_Attack"]);
    		Swing(dictionary["Ari_Walk"], dictionary["Ari_Attack"]);
    		AnimatorStateTransition val3 = dictionary["Ari_Attack"].AddTransition(dictionary["Ari_Jump"]);
    		val3.hasExitTime = false;
    		val3.hasFixedDuration = true;
    		val3.duration = 0.12f;
    		val3.canTransitionToSelf = false;
    		((AnimatorTransitionBase)val3).AddCondition((AnimatorConditionMode)1, 0f, "Jump");
    		SwingBack(dictionary["Ari_Attack"], dictionary["Ari_Walk"], (AnimatorConditionMode)3);
    		SwingBack(dictionary["Ari_Attack"], dictionary["Ari_Idle"], (AnimatorConditionMode)4);
    		AnimatorStateTransition val4 = dictionary["Ari_Attack"].AddTransition(dictionary["Ari_Idle"]);
    		val4.hasExitTime = true;
    		val4.exitTime = 0.514f;
    		val4.hasFixedDuration = true;
    		val4.duration = 0.12f;
    		val4.canTransitionToSelf = false;
    		StringBuilder stringBuilder = new StringBuilder();
    		RepointAnimators(val, stringBuilder);
    		EditorUtility.SetDirty((Object)(object)val);
    		AssetDatabase.SaveAssets();
    		stringBuilder.AppendLine("controller: Assets/Art/Ari/Ari.controller");
    		stringBuilder.AppendLine("  parameter Speed (float, default 0)");
    		stringBuilder.AppendLine("  parameter Jump (trigger)");
    		stringBuilder.AppendLine("  parameter Swing (trigger)");
    		stringBuilder.AppendLine("  parameter Grounded (bool, default true)");
    		stringBuilder.AppendLine("  default state: " + ((Object)machine.defaultState).name);
    		ChildAnimatorState[] states = machine.states;
    		for (int num = 0; num < states.Length; num++)
    		{
    			ChildAnimatorState val5 = states[num];
    			AnimatorState state = val5.state;
    			Motion motion = state.motion;
    			AnimationClip val6 = (AnimationClip)(object)((motion is AnimationClip) ? motion : null);
    			string[] array2 = new string[7]
    			{
    				"  state '",
    				((Object)state).name,
    				"' motion='",
    				null,
    				null,
    				null,
    				null
    			};
    			Motion motion2 = state.motion;
    			array2[3] = ((motion2 != null) ? ((Object)motion2).name : null);
    			array2[4] = "' ";
    			array2[5] = $"len={((val6 != null) ? new float?(val6.length) : ((float?)null)):0.00}s loop={((val6 != null) ? new bool?(((Motion)val6).isLooping) : ((bool?)null))} ";
    			array2[6] = $"speed={state.speed:0.00}";
    			stringBuilder.AppendLine(string.Concat(array2));
    			AnimatorStateTransition[] transitions = state.transitions;
    			foreach (AnimatorStateTransition val7 in transitions)
    			{
    				string text = string.Join(" AND ", ((AnimatorTransitionBase)val7).conditions.Select((AnimatorCondition c) =>
    				{
    					//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    					return $"{c.parameter} {c.mode} {c.threshold}";
    				}));
    				string[] array3 = new string[7] { "      -> '", null, null, null, null, null, null };
    				AnimatorState destinationState = ((AnimatorTransitionBase)val7).destinationState;
    				array3[1] = ((destinationState != null) ? ((Object)destinationState).name : null);
    				array3[2] = "' cond=[";
    				array3[3] = text;
    				array3[4] = "] ";
    				array3[5] = $"exitTime={val7.exitTime:0.00} hasExitTime={val7.hasExitTime} ";
    				array3[6] = $"duration={val7.duration:0.00}";
    				stringBuilder.AppendLine(string.Concat(array3));
    			}
    		}
    		try
    		{
    			string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp/ari_controller.txt");
    			Directory.CreateDirectory(Path.GetDirectoryName(path));
    			File.WriteAllText(path, stringBuilder.ToString());
    		}
    		catch (Exception ex)
    		{
    			Debug.LogWarning((object)("[Echoes] could not write Temp/ari_controller.txt: " + ex.Message));
    		}
    		Debug.Log((object)("[Echoes] Ari controller built\n" + stringBuilder));
    	}

    	private static void RepointAnimators(AnimatorController controller, StringBuilder sb)
    	{
    		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
    		int num = 0;
    		int num2 = 0;
    		int num3 = 0;
    		for (int i = 0; i < SceneManager.sceneCount; i++)
    		{
    			Scene sceneAt = SceneManager.GetSceneAt(i);
    			if (!sceneAt.isLoaded)
    			{
    				continue;
    			}
    			GameObject[] rootGameObjects = sceneAt.GetRootGameObjects();
    			for (int j = 0; j < rootGameObjects.Length; j++)
    			{
    				Animator[] componentsInChildren = rootGameObjects[j].GetComponentsInChildren<Animator>(true);
    				foreach (Animator val in componentsInChildren)
    				{
    					if ((Object)(object)((Component)val).GetComponent<AriMover>() == (Object)null)
    					{
    						num3++;
    						continue;
    					}
    					if ((Object)(object)val.runtimeAnimatorController == (Object)(object)controller)
    					{
    						num2++;
    						continue;
    					}
    					RuntimeAnimatorController runtimeAnimatorController = val.runtimeAnimatorController;
    					val.runtimeAnimatorController = (RuntimeAnimatorController)(object)controller;
    					num++;
    					sb.AppendLine("  re-pointed Animator on '" + ((Object)val).name + "'" + (((Object)(object)runtimeAnimatorController == (Object)null) ? "  (it had no controller at all — a dangling reference)" : ("  (from '" + ((Object)runtimeAnimatorController).name + "')")));
    				}
    			}
    		}
    		sb.AppendLine("  animators: " + num + " re-pointed, " + num2 + " already correct, " + num3 + " on objects that are not Ari");
    		if (num > 0)
    		{
    			EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    		}
    	}

    	private static void Launch(AnimatorState from, AnimatorState jump)
    	{
    		AnimatorStateTransition val = from.AddTransition(jump);
    		val.hasExitTime = false;
    		val.exitTime = 0f;
    		val.hasFixedDuration = true;
    		val.duration = 0.05f;
    		val.canTransitionToSelf = false;
    		((AnimatorTransitionBase)val).AddCondition((AnimatorConditionMode)1, 0f, "Jump");
    	}

    	private static void Land(AnimatorState from, AnimatorState to, AnimatorConditionMode speed)
    	{
    		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
    		AnimatorStateTransition val = from.AddTransition(to);
    		val.hasExitTime = false;
    		val.exitTime = 0f;
    		val.hasFixedDuration = true;
    		val.duration = 0.12f;
    		val.canTransitionToSelf = false;
    		((AnimatorTransitionBase)val).AddCondition((AnimatorConditionMode)1, 0f, "Grounded");
    		((AnimatorTransitionBase)val).AddCondition(speed, 0.1f, "Speed");
    	}

    	private static void Swing(AnimatorState from, AnimatorState swing)
    	{
    		AnimatorStateTransition val = from.AddTransition(swing);
    		val.hasExitTime = false;
    		val.exitTime = 0f;
    		val.hasFixedDuration = true;
    		val.duration = 0.02f;
    		val.canTransitionToSelf = false;
    		((AnimatorTransitionBase)val).AddCondition((AnimatorConditionMode)1, 0f, "Swing");
    	}

    	private static void SwingBack(AnimatorState from, AnimatorState to, AnimatorConditionMode speed)
    	{
    		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
    		AnimatorStateTransition val = from.AddTransition(to);
    		val.hasExitTime = false;
    		val.exitTime = 0f;
    		val.hasFixedDuration = true;
    		val.duration = 0.12f;
    		val.canTransitionToSelf = false;
    		((AnimatorTransitionBase)val).AddCondition((AnimatorConditionMode)2, 0f, "Swinging");
    		((AnimatorTransitionBase)val).AddCondition(speed, 0.1f, "Speed");
    	}

    	private static void CrossFade(AnimatorState from, AnimatorState to, AnimatorConditionMode mode)
    	{
    		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
    		AnimatorStateTransition val = from.AddTransition(to);
    		val.hasExitTime = false;
    		val.hasFixedDuration = true;
    		val.duration = 0.15f;
    		val.exitTime = 0f;
    		val.canTransitionToSelf = false;
    		((AnimatorTransitionBase)val).AddCondition(mode, 0.1f, "Speed");
    	}
    }
}