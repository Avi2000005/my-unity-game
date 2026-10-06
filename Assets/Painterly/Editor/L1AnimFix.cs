using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class L1AnimFix
    {
    	private const string Report = "Temp/l1_animfix.txt";

    	[MenuItem("Tools/Echoes/Assign and Wire the Animations", priority = 71)]
    	public static void Run()
    	{
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] assign and wire the animations");
    		if (EditorApplication.isPlaying)
    		{
    			stringBuilder.AppendLine("  refused: in play mode");
    			Finish(stringBuilder);
    			return;
    		}
    		Scene activeScene = SceneManager.GetActiveScene();
    		if (activeScene.rootCount == 0)
    		{
    			stringBuilder.AppendLine("  FATAL: no scene open.");
    			Finish(stringBuilder);
    			return;
    		}
    		AssignMono(stringBuilder);
    		FixAri(stringBuilder);
    		ReportCrawlers(stringBuilder);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine(SaveAfter.Save("animation controllers"));
    		Finish(stringBuilder);
    	}

    	private static void AssignMono(StringBuilder sb)
    	{
    		sb.AppendLine();
    		sb.AppendLine("=== MONO ===");
    		MonoCompanion monoCompanion = Object.FindAnyObjectByType<MonoCompanion>((FindObjectsInactive)1);
    		if ((Object)(object)monoCompanion == (Object)null)
    		{
    			sb.AppendLine("  no MonoCompanion in the scene. Beat 3 cannot run.");
    			return;
    		}
    		Animator componentInChildren = ((Component)monoCompanion).GetComponentInChildren<Animator>(true);
    		if ((Object)(object)componentInChildren == (Object)null)
    		{
    			sb.AppendLine("  Mono has no Animator at all. MonoCompanion carries [RequireComponent(typeof(Animator))], so this means the requirement was bypassed.");
    			return;
    		}
    		sb.AppendLine("  '" + ((Object)componentInChildren).name + "' animator, on '" + ((Object)((Component)componentInChildren).gameObject).name + "'");
    		sb.AppendLine("    was: " + (((Object)(object)componentInChildren.runtimeAnimatorController == (Object)null) ? "NOTHING — the statue" : ((Object)componentInChildren.runtimeAnimatorController).name));
    		AnimatorController val = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/L1/Mono.controller");
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("  FATAL: Assets/Art/L1/Mono.controller is not loadable. Assigning nothing is what is already happening.");
    			return;
    		}
    		componentInChildren.runtimeAnimatorController = (RuntimeAnimatorController)(object)val;
    		EditorUtility.SetDirty((Object)(object)componentInChildren);
    		sb.AppendLine("    now: " + (((Object)(object)componentInChildren.runtimeAnimatorController == (Object)null) ? "STILL NOTHING — the assignment did not take" : (((Object)componentInChildren.runtimeAnimatorController).name + "  (read back off the Animator, not off the variable)")));
    	}

    	private static void FixAri(StringBuilder sb)
    	{
    		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
    		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine();
    		sb.AppendLine("=== ARI ===");
    		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    		if ((Object)(object)ariMover == (Object)null)
    		{
    			sb.AppendLine("  no AriMover.");
    			return;
    		}
    		Animator componentInChildren = ((Component)ariMover).GetComponentInChildren<Animator>(true);
    		if ((Object)(object)componentInChildren == (Object)null)
    		{
    			sb.AppendLine("  Ari has no Animator.");
    			return;
    		}
    		RuntimeAnimatorController runtimeAnimatorController = componentInChildren.runtimeAnimatorController;
    		AnimatorController val = (AnimatorController)(object)((runtimeAnimatorController is AnimatorController) ? runtimeAnimatorController : null);
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("  FATAL: Ari's controller is " + (((Object)(object)componentInChildren.runtimeAnimatorController == (Object)null) ? "null" : ((object)componentInChildren.runtimeAnimatorController).GetType().Name) + ", so its states cannot be added to.");
    			return;
    		}
    		AnimatorStateMachine stateMachine = val.layers[0].stateMachine;
    		AddBool(val, "Run");
    		AddTrigger(val, "Talk");
    		AddTrigger(val, "Collect");
    		AddTrigger(val, "Hurt");
    		sb.AppendLine("  parameters now: " + val.parameters.Length);
    		for (int i = 0; i < val.parameters.Length; i++)
    		{
    			sb.AppendLine("    " + val.parameters[i].name + " : " + ((object)val.parameters[i].type/*cast due to constrained. prefix*/).ToString());
    		}
    		AnimatorState val2 = Find(stateMachine, "Ari_Idle");
    		AnimatorState val3 = Find(stateMachine, "Ari_Walk");
    		ChildAnimatorState[] states;
    		if ((Object)(object)val2 == (Object)null || (Object)(object)val3 == (Object)null)
    		{
    			sb.AppendLine("  FATAL: the existing machine no longer has Ari_Idle/Ari_Walk. Its states are:");
    			states = stateMachine.states;
    			for (int j = 0; j < states.Length; j++)
    			{
    				ChildAnimatorState val4 = states[j];
    				sb.AppendLine("    " + ((Object)val4.state).name);
    			}
    			return;
    		}
    		AddState(sb, val, stateMachine, "Ari_Run", "Ari_Run");
    		AddState(sb, val, stateMachine, "Ari_Talk", "Ari_Talk");
    		AddState(sb, val, stateMachine, "Ari_Collect", "Ari_Collect");
    		AnimatorState val5 = Find(stateMachine, "Ari_Run");
    		AnimatorState val6 = Find(stateMachine, "Ari_Talk");
    		AnimatorState val7 = Find(stateMachine, "Ari_Collect");
    		Link(sb, val2, val5, "Run", want: true);
    		Link(sb, val3, val5, "Run", want: true);
    		Link(sb, val5, val3, "Run", want: false);
    		LinkSpeed(sb, val5, val2, faster: false);
    		LinkTrigger(sb, val2, val6, "Talk");
    		LinkTrigger(sb, val3, val6, "Talk");
    		if ((Object)(object)val5 != (Object)null)
    		{
    			LinkTrigger(sb, val5, val6, "Talk");
    		}
    		if ((Object)(object)val6 != (Object)null && !HasExit(val6))
    		{
    			AnimatorStateTransition val8 = val6.AddExitTransition();
    			val8.hasExitTime = true;
    			val8.exitTime = 0.92f;
    			sb.AppendLine("    + Ari_Talk -> Ari_Idle at 92% (a talk pose held to the end of the clip looks like he is glitching)");
    		}
    		LinkTrigger(sb, val2, val7, "Collect");
    		LinkTrigger(sb, val3, val7, "Collect");
    		if ((Object)(object)val5 != (Object)null)
    		{
    			LinkTrigger(sb, val5, val7, "Collect");
    		}
    		if ((Object)(object)val7 != (Object)null && !HasExit(val7))
    		{
    			AnimatorStateTransition val9 = val7.AddExitTransition();
    			val9.hasExitTime = true;
    			val9.exitTime = 0.9f;
    			sb.AppendLine("    + Ari_Collect -> Ari_Idle at 90%");
    		}
    		EditorUtility.SetDirty((Object)(object)val);
    		AssetDatabase.SaveAssets();
    		sb.AppendLine();
    		sb.AppendLine("  after: " + stateMachine.states.Length + " state(s)");
    		states = stateMachine.states;
    		for (int j = 0; j < states.Length; j++)
    		{
    			ChildAnimatorState val10 = states[j];
    			Motion motion = val10.state.motion;
    			AnimationClip val11 = (AnimationClip)(object)((motion is AnimationClip) ? motion : null);
    			sb.AppendLine("    " + ((Object)val10.state).name.PadRight(18) + (((Object)(object)val11 != (Object)null) ? ((Object)val11).name : "NO MOTION"));
    		}
    		sb.AppendLine();
    		sb.AppendLine("  the code must set these, or the states never play:");
    		sb.AppendLine("    Run     (bool)   AriMover, from the run key");
    		sb.AppendLine("    Talk    (trigger) when Mono starts a line to him");
    		sb.AppendLine("    Collect (trigger) when he picks the fragment up");
    	}

    	private static void ReportCrawlers(StringBuilder sb)
    	{
    		sb.AppendLine();
    		sb.AppendLine("=== CRAWLERS ===");
    		sb.AppendLine("  their controller is complete and every transition is wired, so a crawler that will not move is not an animation fault. Listed here so it is not chased twice.");
    		InkCrawler[] array = Object.FindObjectsByType<InkCrawler>((FindObjectsInactive)1);
    		for (int i = 0; i < array.Length; i++)
    		{
    			GameObject gameObject = ((Component)array[i]).gameObject;
    			Animator componentInChildren = gameObject.GetComponentInChildren<Animator>(true);
    			InkCrawlerEmerge component = gameObject.GetComponent<InkCrawlerEmerge>();
    			bool flag = ((Behaviour)array[i]).enabled && gameObject.activeInHierarchy;
    			sb.AppendLine("    " + ((Object)gameObject).name.PadRight(18) + " controller " + (((Object)(object)componentInChildren != (Object)null && (Object)(object)componentInChildren.runtimeAnimatorController != (Object)null) ? ((Object)componentInChildren.runtimeAnimatorController).name : "NONE") + ", emerge " + (((Object)(object)component != (Object)null) ? ("on (enabled " + ((Behaviour)component).enabled + ")") : "MISSING") + ", active " + (flag ? "yes" : "NO — it cannot hunt"));
    		}
    	}

    	private static AnimatorState Find(AnimatorStateMachine sm, string name)
    	{
    		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
    		ChildAnimatorState[] states = sm.states;
    		for (int i = 0; i < states.Length; i++)
    		{
    			ChildAnimatorState val = states[i];
    			if (((Object)val.state).name == name)
    			{
    				return val.state;
    			}
    		}
    		return null;
    	}

    	private static bool HasExit(AnimatorState s)
    	{
    		if (s.transitions != null)
    		{
    			return s.transitions.Length != 0;
    		}
    		return false;
    	}

    	private static void AddBool(AnimatorController c, string name)
    	{
    		AnimatorControllerParameter[] parameters = c.parameters;
    		for (int i = 0; i < parameters.Length; i++)
    		{
    			if (parameters[i].name == name)
    			{
    				return;
    			}
    		}
    		c.AddParameter(name, (AnimatorControllerParameterType)4);
    	}

    	private static void AddTrigger(AnimatorController c, string name)
    	{
    		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0025: Invalid comparison between Unknown and I4
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
    		AnimatorControllerParameter[] parameters = c.parameters;
    		foreach (AnimatorControllerParameter val in parameters)
    		{
    			if (val.name == name)
    			{
    				if ((int)val.type != 9)
    				{
    					Debug.LogError((object)("[Echoes] '" + name + "' exists on " + ((Object)c).name + " as a " + ((object)val.type/*cast due to constrained. prefix*/).ToString() + ", not a Trigger. Code that sets it as a trigger will not work."));
    				}
    				return;
    			}
    		}
    		c.AddParameter(name, (AnimatorControllerParameterType)9);
    	}

    	private static void AddState(StringBuilder sb, AnimatorController ctrl, AnimatorStateMachine sm, string stateName, string clipName)
    	{
    		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)Find(sm, stateName) != (Object)null)
    		{
    			sb.AppendLine("  " + stateName + " already in the machine, left alone");
    			return;
    		}
    		AnimationClip val = LoadClip(clipName);
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("  COULD NOT ADD " + stateName + " — no clip named '" + clipName + "' in Assets/Art/L1 or Assets/Art/Ari. Not added as an empty state, because a state with no motion is a state that plays nothing and looks exactly like the bug being fixed.");
    		}
    		else
    		{
    			sm.AddState(stateName, new Vector3(420f, 60f + (float)sm.states.Length * 70f)).motion = (Motion)(object)val;
    			sb.AppendLine("  + state " + stateName.PadRight(16) + "clip '" + ((Object)val).name + "' " + val.length.ToString("0.00") + "s");
    		}
    	}

    	private static AnimationClip LoadClip(string clipName)
    	{
    		string[] array = new string[2] { "Assets/Art/L1", "Assets/Art/Ari" };
    		for (int i = 0; i < array.Length; i++)
    		{
    			if (!Directory.Exists(array[i]))
    			{
    				continue;
    			}
    			string[] files = Directory.GetFiles(array[i], "*.fbx");
    			for (int j = 0; j < files.Length; j++)
    			{
    				Object[] array2 = AssetDatabase.LoadAllAssetsAtPath(files[j]);
    				foreach (Object obj in array2)
    				{
    					AnimationClip val = (AnimationClip)(object)((obj is AnimationClip) ? obj : null);
    					if ((Object)(object)val != (Object)null && ((Object)val).name == clipName)
    					{
    						return val;
    					}
    				}
    			}
    		}
    		return null;
    	}

    	private static bool Link(StringBuilder sb, AnimatorState from, AnimatorState to, string param, bool want)
    	{
    		if ((Object)(object)from == (Object)null || (Object)(object)to == (Object)null)
    		{
    			return false;
    		}
    		AnimatorStateTransition[] transitions = from.transitions;
    		for (int i = 0; i < transitions.Length; i++)
    		{
    			if ((Object)(object)((AnimatorTransitionBase)transitions[i]).destinationState == (Object)(object)to)
    			{
    				sb.AppendLine("    = " + ((Object)from).name + " -> " + ((Object)to).name + " (exists)");
    				return false;
    			}
    		}
    		AnimatorStateTransition val = from.AddTransition(to);
    		val.hasExitTime = false;
    		val.duration = 0.08f;
    		((AnimatorTransitionBase)val).AddCondition((AnimatorConditionMode)(want ? 1 : 2), 0f, param);
    		sb.AppendLine("    + " + ((Object)from).name + " -> " + ((Object)to).name + " when " + param + " is " + (want ? "true" : "false"));
    		return true;
    	}

    	private static void LinkSpeed(StringBuilder sb, AnimatorState from, AnimatorState to, bool faster)
    	{
    		if ((Object)(object)from == (Object)null || (Object)(object)to == (Object)null)
    		{
    			return;
    		}
    		AnimatorStateTransition[] transitions = from.transitions;
    		for (int i = 0; i < transitions.Length; i++)
    		{
    			if ((Object)(object)((AnimatorTransitionBase)transitions[i]).destinationState == (Object)(object)to)
    			{
    				return;
    			}
    		}
    		AnimatorStateTransition val = from.AddTransition(to);
    		val.hasExitTime = false;
    		val.duration = 0.08f;
    		((AnimatorTransitionBase)val).AddCondition((AnimatorConditionMode)(faster ? 3 : 4), 0.1f, "Speed");
    		sb.AppendLine("    + " + ((Object)from).name + " -> " + ((Object)to).name + " when Speed is " + (faster ? "above" : "below") + " 0.10");
    	}

    	private static void LinkTrigger(StringBuilder sb, AnimatorState from, AnimatorState to, string trigger)
    	{
    		if ((Object)(object)from == (Object)null || (Object)(object)to == (Object)null)
    		{
    			return;
    		}
    		AnimatorStateTransition[] transitions = from.transitions;
    		for (int i = 0; i < transitions.Length; i++)
    		{
    			if ((Object)(object)((AnimatorTransitionBase)transitions[i]).destinationState == (Object)(object)to)
    			{
    				return;
    			}
    		}
    		AnimatorStateTransition val = from.AddTransition(to);
    		val.hasExitTime = false;
    		val.duration = 0.1f;
    		((AnimatorTransitionBase)val).AddCondition((AnimatorConditionMode)1, 0f, trigger);
    		sb.AppendLine("    + " + ((Object)from).name + " -> " + ((Object)to).name + " on the " + trigger + " trigger");
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/l1_animfix.txt"), sb.ToString());
    		Debug.Log((object)"[Echoes] animations wired — see Temp/l1_animfix.txt");
    	}
    }
}