using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class CastControllerBuilder
    {
    	private const string Dir = "Assets/Art/L1";

    	private const string MonoPath = "Assets/Art/L1/Mono.controller";

    	private const string CrawlerPath = "Assets/Art/L1/InkCrawler.controller";

    	private const string Report = "Temp/cast_controllers.txt";

    	private const float Walking = 0.1f;

    	private const float Running = 2.5f;

    	[MenuItem("Tools/Echoes/Build Cast Controllers", priority = 64)]
    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp/cast_controllers.txt");
    		if (File.Exists(path))
    		{
    			File.Delete(path);
    		}
    		Dictionary<string, AnimationClip> dictionary = (from c in ((IEnumerable<string>)AssetDatabase.FindAssets("t:AnimationClip", new string[1] { "Assets/Art/L1" })).Select((Func<string, string>)AssetDatabase.GUIDToAssetPath).Distinct().SelectMany((Func<string, IEnumerable<Object>>)AssetDatabase.LoadAllAssetsAtPath)
    				.OfType<AnimationClip>()
    			where !c.legacy && !((Object)c).name.StartsWith("__")
    			group c by ((Object)c).name).ToDictionary((IGrouping<string, AnimationClip> g) => g.Key, (IGrouping<string, AnimationClip> g) => g.First());
    		stringBuilder.AppendLine("clips available: " + dictionary.Count);
    		foreach (AnimationClip item in dictionary.Values.OrderBy((AnimationClip c) => ((Object)c).name))
    		{
    			stringBuilder.AppendLine($"  {((Object)item).name}  {item.length:0.00}s  loop={((Motion)item).isLooping}");
    		}
    		BuildMono(dictionary, stringBuilder);
    		BuildCrawler(dictionary, stringBuilder);
    		AssetDatabase.SaveAssets();
    		try
    		{
    			File.WriteAllText(path, stringBuilder.ToString());
    		}
    		catch (Exception ex)
    		{
    			Debug.LogWarning((object)("[Echoes] could not write Temp/cast_controllers.txt: " + ex.Message));
    		}
    		Debug.Log((object)("[Echoes] cast controllers built\n" + stringBuilder));
    	}

    	private static void BuildMono(Dictionary<string, AnimationClip> clips, StringBuilder sb)
    	{
    		sb.AppendLine();
    		sb.AppendLine("=== Mono ===");
    		string[] source = new string[4] { "Mono_Idle", "Mono_Walk", "Mono_Talk", "Mono_Wake" };
    		string[] array = source.Where((string w) => !clips.ContainsKey(w)).ToArray();
    		if (array.Length != 0)
    		{
    			sb.AppendLine("  MISSING: " + string.Join(", ", array) + " — not built");
    			return;
    		}
    		Replace("Assets/Art/L1/Mono.controller");
    		AnimatorController val = AnimatorController.CreateAnimatorControllerAtPath("Assets/Art/L1/Mono.controller");
    		val.AddParameter(Float("Speed", 0f));
    		val.AddParameter(Trigger("Awake"));
    		val.AddParameter(Trigger("Talk"));
    		AnimatorStateMachine machine = val.layers[0].stateMachine;
    		Dictionary<string, AnimatorState> dictionary = source.ToDictionary((string n) => n, (string n) => State(machine, n, clips[n]));
    		machine.defaultState = dictionary["Mono_Idle"];
    		Fire(dictionary["Mono_Idle"], dictionary["Mono_Wake"], "Awake", 0.1f);
    		Fire(dictionary["Mono_Walk"], dictionary["Mono_Wake"], "Awake", 0.1f);
    		Finish(dictionary["Mono_Wake"], dictionary["Mono_Idle"], 0.4f, null, (AnimatorConditionMode)0);
    		Fire(dictionary["Mono_Idle"], dictionary["Mono_Talk"], "Talk", 0.15f);
    		Fire(dictionary["Mono_Walk"], dictionary["Mono_Talk"], "Talk", 0.15f);
    		Finish(dictionary["Mono_Talk"], dictionary["Mono_Walk"], 0.2f, "Speed", (AnimatorConditionMode)3, 0.1f);
    		Finish(dictionary["Mono_Talk"], dictionary["Mono_Idle"], 0.2f, "Speed", (AnimatorConditionMode)4, 0.1f);
    		Blend(dictionary["Mono_Idle"], dictionary["Mono_Walk"], (AnimatorConditionMode)3, 0.15f);
    		Blend(dictionary["Mono_Walk"], dictionary["Mono_Idle"], (AnimatorConditionMode)4, 0.2f);
    		sb.AppendLine("  NOTE Mono_Wake is " + clips["Mono_Wake"].length.ToString("0.00") + "s, trimmed to its busiest window (5.07s-8.03s of the 11.17s take). MonoChase's handoverDelay is 3.2s, which leaves the wake on screen before the camera moves.");
    		Dump(val, "Assets/Art/L1/Mono.controller", sb);
    	}

    	private static void BuildCrawler(Dictionary<string, AnimationClip> clips, StringBuilder sb)
    	{
    		sb.AppendLine();
    		sb.AppendLine("=== Ink Crawler ===");
    		string[] source = new string[7] { "Crawler_Idle", "Crawler_Idle2", "Crawler_Walk", "Crawler_Run", "Crawler_Crawl", "Crawler_Attack", "Crawler_Death" };
    		string[] array = source.Where((string w) => !clips.ContainsKey(w)).ToArray();
    		if (array.Length != 0)
    		{
    			sb.AppendLine("  MISSING: " + string.Join(", ", array) + " — not built");
    			return;
    		}
    		Replace("Assets/Art/L1/InkCrawler.controller");
    		AnimatorController val = AnimatorController.CreateAnimatorControllerAtPath("Assets/Art/L1/InkCrawler.controller");
    		val.AddParameter(Float("Speed", 0f));
    		val.AddParameter(Flag("Crawling", value: false));
    		val.AddParameter(Trigger("Attack"));
    		val.AddParameter(Trigger("Die"));
    		val.AddParameter(Trigger("Idle2"));
    		AnimatorStateMachine machine = val.layers[0].stateMachine;
    		Dictionary<string, AnimatorState> dictionary = source.ToDictionary((string n) => n, (string n) => State(machine, n, clips[n]));
    		machine.defaultState = dictionary["Crawler_Idle"];
    		Fire(dictionary["Crawler_Idle"], dictionary["Crawler_Idle2"], "Idle2", 0.25f);
    		Fire(dictionary["Crawler_Idle2"], dictionary["Crawler_Idle"], "Idle2", 0.25f);
    		sb.AppendLine("  Crawler_Idle2 is reachable on the 'Idle2' trigger in both directions, so it is a state a designer can use rather than one the graph merely contains.");
    		string[] array2 = new string[4] { "Crawler_Idle", "Crawler_Walk", "Crawler_Run", "Crawler_Crawl" };
    		foreach (string key in array2)
    		{
    			Fire(dictionary[key], dictionary["Crawler_Death"], "Die", 0.1f);
    		}
    		array2 = new string[3] { "Crawler_Idle", "Crawler_Walk", "Crawler_Run" };
    		foreach (string key2 in array2)
    		{
    			Fire(dictionary[key2], dictionary["Crawler_Attack"], "Attack", 0.1f);
    		}
    		Finish(dictionary["Crawler_Attack"], dictionary["Crawler_Idle"], 0.25f, null, (AnimatorConditionMode)0);
    		Blend(dictionary["Crawler_Idle"], dictionary["Crawler_Walk"], (AnimatorConditionMode)3, 0.18f);
    		Blend(dictionary["Crawler_Walk"], dictionary["Crawler_Idle"], (AnimatorConditionMode)4, 0.22f);
    		Blend(dictionary["Crawler_Walk"], dictionary["Crawler_Run"], (AnimatorConditionMode)3, 0.2f, 2.5f);
    		Blend(dictionary["Crawler_Run"], dictionary["Crawler_Walk"], (AnimatorConditionMode)4, 0.2f, 2.5f);
    		Toggle(dictionary["Crawler_Idle"], dictionary["Crawler_Crawl"], "Crawling", value: true, 0.3f);
    		Toggle(dictionary["Crawler_Crawl"], dictionary["Crawler_Idle"], "Crawling", value: false, 0.3f);
    		sb.AppendLine("  Crawler_Death is terminal: " + dictionary["Crawler_Death"].transitions.Length + " outgoing transition(s). A staggered crawler that gets back up is a bug the player finds for you.");
    		Dump(val, "Assets/Art/L1/InkCrawler.controller", sb);
    	}

    	private static AnimatorControllerParameter Float(string name, float value)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001b: Expected Obj, but got Unknown
    		return new AnimatorControllerParameter
    		{
    			name = name,
    			type = (AnimatorControllerParameterType)1,
    			defaultFloat = value
    		};
    	}

    	private static AnimatorControllerParameter Flag(string name, bool value)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001b: Expected Obj, but got Unknown
    		return new AnimatorControllerParameter
    		{
    			name = name,
    			type = (AnimatorControllerParameterType)4,
    			defaultBool = value
    		};
    	}

    	private static AnimatorControllerParameter Trigger(string name)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0015: Expected Obj, but got Unknown
    		return new AnimatorControllerParameter
    		{
    			name = name,
    			type = (AnimatorControllerParameterType)9
    		};
    	}

    	private static void Replace(string path)
    	{
    		if ((Object)(object)AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != (Object)null)
    		{
    			AssetDatabase.DeleteAsset(path);
    		}
    	}

    	private static AnimatorState State(AnimatorStateMachine machine, string name, AnimationClip clip)
    	{
    		AnimatorState val = machine.AddState(name);
    		val.motion = (Motion)(object)clip;
    		val.writeDefaultValues = false;
    		return val;
    	}

    	private static void Blend(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float seconds, float threshold = 0.1f)
    	{
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		AnimatorStateTransition val = from.AddTransition(to);
    		val.hasExitTime = false;
    		val.hasFixedDuration = true;
    		val.duration = seconds;
    		val.exitTime = 0f;
    		val.canTransitionToSelf = false;
    		((AnimatorTransitionBase)val).AddCondition(mode, threshold, "Speed");
    	}

    	private static void Fire(AnimatorState from, AnimatorState to, string trigger, float seconds)
    	{
    		AnimatorStateTransition val = from.AddTransition(to);
    		val.hasExitTime = false;
    		val.hasFixedDuration = true;
    		val.duration = seconds;
    		val.exitTime = 0f;
    		val.canTransitionToSelf = false;
    		((AnimatorTransitionBase)val).AddCondition((AnimatorConditionMode)1, 0f, trigger);
    	}

    	private static void Toggle(AnimatorState from, AnimatorState to, string flag, bool value, float seconds)
    	{
    		AnimatorStateTransition val = from.AddTransition(to);
    		val.hasExitTime = false;
    		val.hasFixedDuration = true;
    		val.duration = seconds;
    		val.exitTime = 0f;
    		val.canTransitionToSelf = false;
    		((AnimatorTransitionBase)val).AddCondition((AnimatorConditionMode)(value ? 1 : 2), 0f, flag);
    	}

    	private static void Finish(AnimatorState from, AnimatorState to, float blendSeconds, string parameter = null, AnimatorConditionMode mode = (AnimatorConditionMode)0, float threshold = 0f)
    	{
    		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
    		AnimatorStateTransition val = from.AddTransition(to);
    		val.hasExitTime = true;
    		val.exitTime = 1f;
    		val.hasFixedDuration = true;
    		val.duration = blendSeconds;
    		val.canTransitionToSelf = false;
    		if (!string.IsNullOrEmpty(parameter))
    		{
    			((AnimatorTransitionBase)val).AddCondition(mode, threshold, parameter);
    		}
    	}

    	private static void Dump(AnimatorController ctrl, string path, StringBuilder sb)
    	{
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0047: Invalid comparison between Unknown and I4
    		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0070: Invalid comparison between Unknown and I4
    		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("  " + path);
    		AnimatorControllerParameter[] parameters = ctrl.parameters;
    		foreach (AnimatorControllerParameter val in parameters)
    		{
    			sb.AppendLine($"    parameter {val.name} ({val.type})" + (((int)val.type == 1) ? (" default=" + val.defaultFloat) : "") + (((int)val.type == 4) ? (" default=" + val.defaultBool) : ""));
    		}
    		AnimatorStateMachine stateMachine = ctrl.layers[0].stateMachine;
    		sb.AppendLine("    default state: " + ((Object)stateMachine.defaultState).name);
    		ChildAnimatorState[] states = stateMachine.states;
    		for (int i = 0; i < states.Length; i++)
    		{
    			ChildAnimatorState val2 = states[i];
    			AnimatorState state = val2.state;
    			Motion motion = state.motion;
    			AnimationClip val3 = (AnimationClip)(object)((motion is AnimationClip) ? motion : null);
    			string[] array = new string[7]
    			{
    				"    state '",
    				((Object)state).name,
    				"' motion='",
    				null,
    				null,
    				null,
    				null
    			};
    			Motion motion2 = state.motion;
    			array[3] = ((motion2 != null) ? ((Object)motion2).name : null);
    			array[4] = "' ";
    			array[5] = $"len={((val3 != null) ? new float?(val3.length) : ((float?)null)):0.00}s loop={((val3 != null) ? new bool?(((Motion)val3).isLooping) : ((bool?)null))} ";
    			array[6] = $"out={state.transitions.Length}";
    			sb.AppendLine(string.Concat(array));
    			AnimatorStateTransition[] transitions = state.transitions;
    			foreach (AnimatorStateTransition val4 in transitions)
    			{
    				string arg = string.Join(" AND ", ((AnimatorTransitionBase)val4).conditions.Select((AnimatorCondition c) =>
    				{
    					//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    					return $"{c.parameter} {c.mode} {c.threshold}";
    				}));
    				string[] array2 = new string[5] { "        -> '", null, null, null, null };
    				AnimatorState destinationState = ((AnimatorTransitionBase)val4).destinationState;
    				array2[1] = ((destinationState != null) ? ((Object)destinationState).name : null);
    				array2[2] = "' ";
    				array2[3] = $"[{arg}] exitTime={val4.exitTime:0.00} ";
    				array2[4] = $"hasExitTime={val4.hasExitTime} duration={val4.duration:0.00}";
    				sb.AppendLine(string.Concat(array2));
    			}
    		}
    	}
    }
}