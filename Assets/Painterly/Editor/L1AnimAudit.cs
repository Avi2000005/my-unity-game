using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class L1AnimAudit
    {
    	private const string Report = "Temp/l1_anim.txt";

    	[MenuItem("Tools/Echoes/Audit the Animations", priority = 70)]
    	public static void Run()
    	{
    		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a9: Invalid comparison between Unknown and I4
    		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
    		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] audit the animation controllers");
    		Scene activeScene = SceneManager.GetActiveScene();
    		if (activeScene.rootCount == 0)
    		{
    			stringBuilder.AppendLine("  FATAL: no scene open.");
    			Finish(stringBuilder);
    			return;
    		}
    		List<string> list = new List<string> { "Mono", "Ari", "InkCrawler" };
    		for (int i = 0; i < list.Count; i++)
    		{
    			AnimatorController val = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/L1/" + list[i] + ".controller") ?? AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/Ari/" + list[i] + ".controller");
    			if ((Object)(object)val == (Object)null)
    			{
    				stringBuilder.AppendLine();
    				stringBuilder.AppendLine("=== " + list[i] + ".controller NOT FOUND ===");
    				continue;
    			}
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("=== " + list[i] + " (" + ((Object)val).name + ") ===");
    			stringBuilder.AppendLine("  parameters (" + val.parameters.Length + "):");
    			for (int j = 0; j < val.parameters.Length; j++)
    			{
    				stringBuilder.AppendLine("    " + val.parameters[j].name + " : " + ((object)val.parameters[j].type/*cast due to constrained. prefix*/).ToString() + (((int)val.parameters[j].type == 9) ? "" : val.parameters[j].defaultFloat.ToString("0.00")));
    			}
    			for (int k = 0; k < val.layers.Length; k++)
    			{
    				AnimatorStateMachine stateMachine = val.layers[k].stateMachine;
    				stringBuilder.AppendLine("  layer '" + val.layers[k].name + "', " + stateMachine.states.Length + " state(s), " + stateMachine.anyStateTransitions.Length + " from Any State:");
    				for (int l = 0; l < stateMachine.states.Length; l++)
    				{
    					AnimatorState state = stateMachine.states[l].state;
    					Motion motion = state.motion;
    					AnimationClip val2 = (AnimationClip)(object)((motion is AnimationClip) ? motion : null);
    					StringBuilder stringBuilder2 = stringBuilder;
    					string text = ((Object)state).name.PadRight(20);
    					string text2;
    					if ((Object)(object)val2 != (Object)null)
    					{
    						text2 = "clip '" + ((Object)val2).name + "' " + val2.length.ToString("0.00") + "s";
    					}
    					else
    					{
    						text2 = (((Object)(object)state.motion == (Object)null) ? "NO MOTION — it plays nothing" : ("motion is a " + ((object)state.motion).GetType().Name));
    					}
    					stringBuilder2.AppendLine("    " + text + text2);
    					AnimatorStateTransition[] transitions = state.transitions;
    					if (transitions == null || transitions.Length == 0)
    					{
    						stringBuilder.AppendLine("        -> NOWHERE. This state never exits, so the character is stuck in it forever no matter what the code sets.");
    						continue;
    					}
    					foreach (AnimatorStateTransition val3 in transitions)
    					{
    						string text3 = (val3.hasExitTime ? ("exitTime " + val3.exitTime.ToString("0.00")) : "instant");
    						if (((AnimatorTransitionBase)val3).conditions != null && ((AnimatorTransitionBase)val3).conditions.Length != 0)
    						{
    							List<string> list2 = new List<string>();
    							for (int n = 0; n < ((AnimatorTransitionBase)val3).conditions.Length; n++)
    							{
    								list2.Add(((AnimatorTransitionBase)val3).conditions[n].parameter + " " + ((object)((AnimatorTransitionBase)val3).conditions[n].mode/*cast due to constrained. prefix*/).ToString() + " " + ((AnimatorTransitionBase)val3).conditions[n].threshold.ToString("0.00"));
    							}
    							text3 = text3 + " if " + string.Join(" and ", list2.ToArray());
    						}
    						else if (!val3.hasExitTime)
    						{
    							text3 += ", NO CONDITION — it fires the moment this state is entered";
    						}
    						stringBuilder.AppendLine("        -> " + (((Object)(object)((AnimatorTransitionBase)val3).destinationState != (Object)null) ? ((Object)((AnimatorTransitionBase)val3).destinationState).name : "EXIT (leaves the machine)") + "   " + text3);
    					}
    				}
    			}
    			List<string> list3 = UnreferencedClips(list[i]);
    			stringBuilder.AppendLine("  clips in the project this controller NEVER uses:");
    			if (list3.Count == 0)
    			{
    				stringBuilder.AppendLine("    none — every clip is wired");
    				continue;
    			}
    			for (int num = 0; num < list3.Count; num++)
    			{
    				stringBuilder.AppendLine("    " + list3[num] + "   <-- on disk, unreachable");
    			}
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("=== IN THE SCENE ===");
    		Who(stringBuilder, "Mono", ((Object)(object)Object.FindAnyObjectByType<MonoCompanion>((FindObjectsInactive)1) != (Object)null) ? ((Component)Object.FindAnyObjectByType<MonoCompanion>((FindObjectsInactive)1)).gameObject : null);
    		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    		Who(stringBuilder, "Ari", ((Object)(object)ariMover != (Object)null) ? ((Component)ariMover).gameObject : null);
    		InkCrawler[] array = Object.FindObjectsByType<InkCrawler>((FindObjectsInactive)1);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("  InkCrawler x" + array.Length);
    		for (int num2 = 0; num2 < array.Length; num2++)
    		{
    			Animator componentInChildren = ((Component)array[num2]).GetComponentInChildren<Animator>(true);
    			StringBuilder stringBuilder3 = stringBuilder;
    			string text4 = ((Object)array[num2]).name.PadRight(20);
    			string text5;
    			if ((Object)(object)componentInChildren == (Object)null)
    			{
    				text5 = "NO ANIMATOR";
    			}
    			else
    			{
    				text5 = (((Object)(object)componentInChildren.runtimeAnimatorController == (Object)null) ? "NONE — it holds its bind pose, which is exactly what a statue looks like" : ((Object)componentInChildren.runtimeAnimatorController).name);
    			}
    			stringBuilder3.AppendLine("    " + text4 + " controller: " + text5);
    		}
    		Finish(stringBuilder);
    	}

    	private static void Who(StringBuilder sb, string who, GameObject go)
    	{
    		Animator val = (((Object)(object)go != (Object)null) ? go.GetComponentInChildren<Animator>(true) : null);
    		sb.AppendLine("  " + who.PadRight(8) + " '" + (((Object)(object)go != (Object)null) ? ((Object)go).name : "NOT FOUND") + "'  animator: " + (((Object)(object)val == (Object)null) ? "NONE" : (((Object)(object)val.runtimeAnimatorController == (Object)null) ? "NO CONTROLLER — this is the statue" : ((Object)val.runtimeAnimatorController).name)));
    	}

    	private static List<string> UnreferencedClips(string who)
    	{
    		HashSet<string> hashSet = new HashSet<string>();
    		AnimatorController val = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/L1/" + who + ".controller") ?? AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/Ari/" + who + ".controller");
    		if ((Object)(object)val != (Object)null)
    		{
    			for (int i = 0; i < val.layers.Length; i++)
    			{
    				AnimatorStateMachine stateMachine = val.layers[i].stateMachine;
    				for (int j = 0; j < stateMachine.states.Length; j++)
    				{
    					Motion motion = stateMachine.states[j].state.motion;
    					AnimationClip val2 = (AnimationClip)(object)((motion is AnimationClip) ? motion : null);
    					if ((Object)(object)val2 != (Object)null)
    					{
    						hashSet.Add(((Object)val2).name);
    					}
    				}
    				AnimatorStateTransition[] anyStateTransitions = stateMachine.anyStateTransitions;
    				foreach (AnimatorStateTransition val3 in anyStateTransitions)
    				{
    					object obj;
    					if (!((Object)(object)((AnimatorTransitionBase)val3).destinationState != (Object)null))
    					{
    						obj = null;
    					}
    					else
    					{
    						Motion motion2 = ((AnimatorTransitionBase)val3).destinationState.motion;
    						obj = ((motion2 is AnimationClip) ? motion2 : null);
    					}
    					AnimationClip val4 = (AnimationClip)obj;
    					if ((Object)(object)val4 != (Object)null)
    					{
    						hashSet.Add(((Object)val4).name);
    					}
    				}
    			}
    		}
    		List<string> list = new List<string>();
    		string[] array = new string[2] { "Assets/Art/L1", "Assets/Art/Ari" };
    		for (int l = 0; l < array.Length; l++)
    		{
    			if (!Directory.Exists(array[l]))
    			{
    				continue;
    			}
    			string[] files = Directory.GetFiles(array[l], who + "_*.fbx");
    			for (int k = 0; k < files.Length; k++)
    			{
    				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[k]);
    				if (!hashSet.Contains(fileNameWithoutExtension))
    				{
    					list.Add(fileNameWithoutExtension);
    				}
    			}
    		}
    		list.Sort();
    		return list;
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/l1_anim.txt"), sb.ToString());
    		Debug.Log((object)"[Echoes] animation audit — see Temp/l1_anim.txt");
    	}
    }
}