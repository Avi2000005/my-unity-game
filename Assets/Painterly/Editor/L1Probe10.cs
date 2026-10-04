using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class L1Probe10
    {
    	private const string Report = "Temp/l1_probe10.txt";

    	[MenuItem("Tools/Echoes/L1 Probe — Ten Items", priority = 61)]
    	public static void Run()
    	{
    		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] L1 probe — the ten complaints, measured");
    		Scene activeScene = SceneManager.GetActiveScene();
    		stringBuilder.AppendLine("scene: " + activeScene.path);
    		ScreenSection(stringBuilder);
    		Crawlers(stringBuilder);
    		CrawlerLookingThings(stringBuilder);
    		Fragment(stringBuilder);
    		MonoLines(stringBuilder);
    		Animators(stringBuilder);
    		Finish(stringBuilder);
    	}

    	private static void ScreenSection(StringBuilder sb)
    	{
    		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine();
    		sb.AppendLine("=== 1. THE SCREEN THE TEXT IS DRAWN ON ===");
    		sb.AppendLine("  Screen.width/height right now (edit mode) : " + Screen.width + " x " + Screen.height);
    		sb.AppendLine("  PlayerSettings default                    : " + PlayerSettings.defaultScreenWidth + " x " + PlayerSettings.defaultScreenHeight + "  fullscreen=" + ((object)PlayerSettings.fullScreenMode/*cast due to constrained. prefix*/).ToString());
    		Resolution currentResolution = Screen.currentResolution;
    		sb.AppendLine("  display                                    : " + currentResolution.width + " x " + currentResolution.height);
    		sb.AppendLine();
    		sb.AppendLine("  font size the HUD asks for, by resolution");
    		sb.AppendLine("    resolution   scale   prompt(30)  row(22)  ari label(15)");
    		int[] array = new int[6] { 480, 720, 900, 1080, 1440, 2160 };
    		for (int i = 0; i < array.Length; i++)
    		{
    			float num = Mathf.Max(0.7f, (float)array[i] / 720f);
    			sb.AppendLine("    " + array[i].ToString().PadLeft(4) + "p      " + num.ToString("F2") + "     " + Mathf.RoundToInt(30f * num).ToString().PadLeft(3) + " px     " + Mathf.RoundToInt(22f * num).ToString().PadLeft(3) + " px    " + Mathf.RoundToInt(15f * num).ToString().PadLeft(3) + " px");
    		}
    		sb.AppendLine();
    		sb.AppendLine("  the number that argues against 'too small': at 1080p the prompt asks for 45 px, which is 4.2% of screen height. If the player is calling that unreadable, scaling it further is the only lever there is — there is no second font and no resolution to blame.");
    	}

    	private static void Crawlers(StringBuilder sb)
    	{
    		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
    		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0338: Unknown result type (might be due to invalid IL or missing references)
    		//IL_033d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
    		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0351: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0445: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0449: Unknown result type (might be due to invalid IL or missing references)
    		//IL_044e: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine();
    		sb.AppendLine("=== 2. INK CRAWLERS ===");
    		InkCrawler[] array = Object.FindObjectsByType<InkCrawler>((FindObjectsInactive)1);
    		sb.AppendLine("  InkCrawler components in the level: " + array.Length + "   (the beat is written for 3)");
    		int num = 0;
    		int num2 = 0;
    		for (int i = 0; i < array.Length; i++)
    		{
    			InkCrawler inkCrawler = array[i];
    			Transform transform = ((Component)inkCrawler).transform;
    			SkinnedMeshRenderer componentInChildren = ((Component)inkCrawler).GetComponentInChildren<SkinnedMeshRenderer>(true);
    			Renderer[] componentsInChildren = ((Component)inkCrawler).GetComponentsInChildren<Renderer>(true);
    			int num3 = 0;
    			for (int j = 0; j < componentsInChildren.Length; j++)
    			{
    				if ((Object)(object)componentsInChildren[j] != (Object)null && componentsInChildren[j].enabled && ((Component)componentsInChildren[j]).gameObject.activeInHierarchy)
    				{
    					num3++;
    				}
    			}
    			InkCrawlerEmerge component = ((Component)inkCrawler).GetComponent<InkCrawlerEmerge>();
    			Collider[] componentsInChildren2 = ((Component)inkCrawler).GetComponentsInChildren<Collider>(true);
    			int num4 = 0;
    			for (int k = 0; k < componentsInChildren2.Length; k++)
    			{
    				if ((Object)(object)componentsInChildren2[k] != (Object)null && componentsInChildren2[k].enabled)
    				{
    					num4++;
    				}
    			}
    			if (((Behaviour)inkCrawler).enabled)
    			{
    				num2++;
    			}
    			if (num3 > 0)
    			{
    				num++;
    			}
    			sb.AppendLine();
    			sb.AppendLine("  [" + i + "] " + ((Object)transform.root).name + " / " + PathName(transform));
    			Vector3 val = transform.position;
    			sb.AppendLine("      position   " + val.ToString("F2"));
    			sb.AppendLine("      active     self=" + ((Component)transform).gameObject.activeSelf + " inHierarchy=" + ((Component)transform).gameObject.activeInHierarchy + "  componentEnabled=" + ((Behaviour)inkCrawler).enabled);
    			sb.AppendLine("      layer      " + LayerName(((Component)inkCrawler).gameObject.layer));
    			Bounds bounds;
    			string text;
    			if (!((Object)(object)componentInChildren == (Object)null))
    			{
    				string[] array2 = new string[5]
    				{
    					((Object)componentInChildren).name,
    					"  mesh=",
    					((Object)(object)componentInChildren.sharedMesh == (Object)null) ? "NULL" : "ok",
    					"  bounds ",
    					null
    				};
    				bounds = ((Renderer)componentInChildren).bounds;
    				array2[4] = bounds.ToString("F2");
    				text = string.Concat(array2);
    			}
    			else
    			{
    				text = "NONE";
    			}
    			sb.AppendLine("      skin       " + text);
    			sb.AppendLine("      renderers  " + num3 + " on of " + componentsInChildren.Length);
    			sb.AppendLine("      colliders  " + num4 + " on of " + componentsInChildren2.Length);
    			sb.AppendLine("      emerge     " + (((Object)(object)component == (Object)null) ? "NO COMPONENT — it can never come up out of the ground" : "present"));
    			sb.AppendLine("      under beat " + Under(transform, "L1_Beat5"));
    			sb.AppendLine("      what is under it, top first:");
    			RaycastHit[] array3 = Physics.RaycastAll(transform.position + Vector3.up * 8f, Vector3.down, 30f, -1, (QueryTriggerInteraction)1);
    			Array.Sort(array3, (RaycastHit a, RaycastHit b) =>
    			{
    				//IL_0002: Unknown result type (might be due to invalid IL or missing references)
    				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
    				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
    				return b.point.y.CompareTo(a.point.y);
    			});
    			int num5 = 0;
    			for (int num6 = 0; num6 < array3.Length; num6++)
    			{
    				if (num5 >= 6)
    				{
    					break;
    				}
    				if (!((Object)(object)((Component)array3[num6].collider).GetComponentInParent<InkCrawler>() != (Object)null))
    				{
    					string[] array4 = new string[9] { "        y ", null, null, null, null, null, null, null, null };
    					val = array3[num6].point;
    					array4[1] = val.y.ToString("F2");
    					array4[2] = "  ";
    					array4[3] = ((Object)array3[num6].collider).name;
    					array4[4] = " (";
    					array4[5] = ((object)array3[num6].collider).GetType().Name;
    					array4[6] = ", ";
    					bounds = array3[num6].collider.bounds;
    					val = bounds.size;
    					array4[7] = val.ToString("F2");
    					array4[8] = ")";
    					sb.AppendLine(string.Concat(array4));
    					num5++;
    				}
    			}
    			if (num5 == 0)
    			{
    				sb.AppendLine("        NOTHING — it is over a hole");
    			}
    		}
    		sb.AppendLine();
    		sb.AppendLine("  summary: " + array.Length + " component(s), " + num2 + " enabled, " + num + " with a renderer actually switched on.");
    		sb.AppendLine("  if components=3 and visible<3, the missing one is not missing: it is there and switched off.");
    	}

    	private static void CrawlerLookingThings(StringBuilder sb)
    	{
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine();
    		sb.AppendLine("=== 3. ANYTHING ELSE WITH A CRAWLER IN ITS NAME ===");
    		sb.AppendLine("(the report said two more ink crawlers stand there the whole level. Those are not InkCrawler components, or they would be in section 2.)");
    		Scene activeScene = SceneManager.GetActiveScene();
    		GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
    		for (int i = 0; i < rootGameObjects.Length; i++)
    		{
    			Walk(rootGameObjects[i].transform, sb);
    		}
    	}

    	private static void Walk(Transform t, StringBuilder sb)
    	{
    		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
    		string text = ((Object)t).name.ToLowerInvariant();
    		if (text.Contains("crawler") || text.Contains("ink"))
    		{
    			Component[] components = ((Component)t).GetComponents<Component>();
    			StringBuilder stringBuilder = new StringBuilder();
    			for (int i = 0; i < components.Length; i++)
    			{
    				if ((Object)(object)components[i] != (Object)null && !(components[i] is Transform))
    				{
    					stringBuilder.Append(((object)components[i]).GetType().Name + " ");
    				}
    			}
    			Renderer[] componentsInChildren = ((Component)t).GetComponentsInChildren<Renderer>(true);
    			int num = 0;
    			for (int j = 0; j < componentsInChildren.Length; j++)
    			{
    				if ((Object)(object)componentsInChildren[j] != (Object)null && componentsInChildren[j].enabled)
    				{
    					num++;
    				}
    			}
    			SkinnedMeshRenderer componentInChildren = ((Component)t).GetComponentInChildren<SkinnedMeshRenderer>(true);
    			sb.AppendLine("  " + PathName(t));
    			string[] array = new string[8] { "      at ", null, null, null, null, null, null, null };
    			Vector3 position = t.position;
    			array[1] = position.ToString("F2");
    			array[2] = "  active=";
    			array[3] = ((Component)t).gameObject.activeInHierarchy.ToString();
    			array[4] = "  renderers ";
    			array[5] = num.ToString();
    			array[6] = "/";
    			array[7] = componentsInChildren.Length.ToString();
    			sb.AppendLine(string.Concat(array));
    			if ((Object)(object)componentInChildren != (Object)null)
    			{
    				string[] array2 = new string[5]
    				{
    					"      skin ",
    					((Object)componentInChildren).name,
    					" bounds ",
    					null,
    					null
    				};
    				Bounds bounds = ((Renderer)componentInChildren).bounds;
    				array2[3] = bounds.ToString("F2");
    				array2[4] = ((num > 0) ? "  <-- THIS IS WHY THE PLAYER SEES A STATUE" : "  (renderer off)");
    				sb.AppendLine(string.Concat(array2));
    			}
    			sb.AppendLine("      has InkCrawler: " + ((Object)(object)((Component)t).GetComponent<InkCrawler>() != (Object)null) + "   has InkCrawlerEmerge: " + ((Object)(object)((Component)t).GetComponent<InkCrawlerEmerge>() != (Object)null));
    			sb.AppendLine("      components: " + stringBuilder.ToString().Trim());
    		}
    		for (int k = 0; k < t.childCount; k++)
    		{
    			Walk(t.GetChild(k), sb);
    		}
    	}

    	private static void Fragment(StringBuilder sb)
    	{
    		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
    		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine();
    		sb.AppendLine("=== 4. THE BLUE FRAGMENT ===");
    		ColourFragment[] array = Object.FindObjectsByType<ColourFragment>((FindObjectsInactive)1);
    		sb.AppendLine("  ColourFragment components: " + array.Length);
    		Vector3 position;
    		for (int i = 0; i < array.Length; i++)
    		{
    			ColourFragment colourFragment = array[i];
    			sb.AppendLine("  [" + i + "] " + PathName(((Component)colourFragment).transform));
    			string[] array2 = new string[8] { "      at ", null, null, null, null, null, null, null };
    			position = ((Component)colourFragment).transform.position;
    			array2[1] = position.ToString("F2");
    			array2[2] = "  active=";
    			array2[3] = ((Component)colourFragment).gameObject.activeInHierarchy.ToString();
    			array2[4] = "  taken=";
    			array2[5] = colourFragment.Taken.ToString();
    			array2[6] = "  showing=";
    			array2[7] = colourFragment.IsShowing.ToString();
    			sb.AppendLine(string.Concat(array2));
    			sb.AppendLine("      has a Glow child: " + ((Object)(object)((Component)colourFragment).transform.Find("Glow") != (Object)null) + "   children: " + ((Component)colourFragment).transform.childCount);
    			sb.AppendLine("      prompt: '" + colourFragment.Prompt + "'  reach " + colourFragment.Range.ToString("F2") + " m");
    		}
    		sb.AppendLine();
    		sb.AppendLine("  who calls Show(true)?  (a fragment that is never shown is a wall with nothing in it)");
    		sb.AppendLine("  --- searching source ---");
    		sb.AppendLine(SourceGrep("ColourFragment") + "--- end ---");
    		FountainFix[] array3 = Object.FindObjectsByType<FountainFix>((FindObjectsInactive)1);
    		sb.AppendLine();
    		sb.AppendLine("  FountainFix in scene: " + array3.Length);
    		for (int j = 0; j < array3.Length; j++)
    		{
    			string text = PathName(((Component)array3[j]).transform);
    			position = ((Component)array3[j]).transform.position;
    			sb.AppendLine("      " + text + " at " + position.ToString("F2"));
    		}
    		sb.AppendLine();
    		sb.AppendLine("  Ari's own carry flag right now: " + AriHudOverlay.CarryingFragment);
    	}

    	private static void MonoLines(StringBuilder sb)
    	{
    		sb.AppendLine();
    		sb.AppendLine("=== 5. MONO'S LINES ===");
    		string[] array = AssetDatabase.FindAssets("t:MonoHintLines");
    		sb.AppendLine("  MonoHintLines assets: " + array.Length);
    		for (int i = 0; i < array.Length; i++)
    		{
    			string text = AssetDatabase.GUIDToAssetPath(array[i]);
    			MonoHintLines monoHintLines = AssetDatabase.LoadAssetAtPath<MonoHintLines>(text);
    			sb.AppendLine("  " + text);
    			if ((Object)(object)monoHintLines == (Object)null)
    			{
    				sb.AppendLine("      could not load");
    				continue;
    			}
    			monoHintLines.Tally(out var beat, out var rung, out var ambient, out var muted);
    			sb.AppendLine("      " + beat + " beat, " + rung + " rung, " + ambient + " ambient, " + muted + " muted");
    			for (int j = 0; j < monoHintLines.lines.Count; j++)
    			{
    				MonoHintLines.Line line = monoHintLines.lines[j];
    				sb.AppendLine("      [" + j + "] " + (line.muted ? "MUTED " : "") + line.kind.ToString() + " '" + line.id + "'" + ((line.kind == MonoHintLines.LineKind.HintRung) ? (" rung " + line.rung) : "") + "\n            " + line.text);
    			}
    		}
    		MonoCompanion monoCompanion = MonoCompanion.FindInLevel();
    		sb.AppendLine();
    		sb.AppendLine("  Mono in level: " + (((Object)(object)monoCompanion == (Object)null) ? "NOT FOUND" : (PathName(((Component)monoCompanion).transform) + "  awake=" + monoCompanion.IsAwake)));
    	}

    	private static void Animators(StringBuilder sb)
    	{
    		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
    		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine();
    		sb.AppendLine("=== 6. ANIMATOR CONTROLLERS ===");
    		Animator[] array = Object.FindObjectsByType<Animator>((FindObjectsInactive)1);
    		foreach (Animator val in array)
    		{
    			RuntimeAnimatorController runtimeAnimatorController = val.runtimeAnimatorController;
    			sb.AppendLine();
    			sb.AppendLine("  " + PathName(((Component)val).transform));
    			sb.AppendLine("      controller: " + (((Object)(object)runtimeAnimatorController == (Object)null) ? "NONE — it stands in its bind pose" : ((Object)runtimeAnimatorController).name));
    			if ((Object)(object)runtimeAnimatorController == (Object)null)
    			{
    				continue;
    			}
    			sb.AppendLine("      states:");
    			AnimatorController val2 = (AnimatorController)(object)((runtimeAnimatorController is AnimatorController) ? runtimeAnimatorController : null);
    			if ((Object)(object)val2 == (Object)null)
    			{
    				sb.AppendLine("        NOT AN AnimatorController asset (" + ((object)runtimeAnimatorController).GetType().Name + ") — no parameters or states to read. This is a finding: a controller of this kind cannot have a Speed blend tree.");
    				continue;
    			}
    			List<string> list = new List<string>();
    			AnimatorControllerParameter[] parameters = val2.parameters;
    			foreach (AnimatorControllerParameter val3 in parameters)
    			{
    				list.Add(val3.name + ":" + ((object)val3.type/*cast due to constrained. prefix*/).ToString());
    			}
    			sb.AppendLine("      parameters: " + ((list.Count == 0) ? "none" : string.Join(", ", list)));
    			AnimatorControllerLayer[] layers = val2.layers;
    			for (int k = 0; k < layers.Length; k++)
    			{
    				ChildAnimatorState[] states = layers[k].stateMachine.states;
    				for (int j = 0; j < states.Length; j++)
    				{
    					ChildAnimatorState val4 = states[j];
    					int num = 0;
    					AnimatorStateTransition[] transitions = val4.state.transitions;
    					for (int l = 0; l < transitions.Length; l++)
    					{
    						if (transitions[l].hasExitTime)
    						{
    							num++;
    						}
    					}
    					sb.AppendLine("        L" + k + " '" + ((Object)val4.state).name + "'  motion=" + (((Object)(object)val4.state.motion == (Object)null) ? "NULL" : ((Object)val4.state.motion).name) + "  speed=" + val4.state.speed.ToString("F2") + "  -> " + num + " transition(s)");
    				}
    			}
    		}
    	}

    	private static string SourceGrep(string token)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		string[] files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
    		Array.Sort(files);
    		for (int i = 0; i < files.Length; i++)
    		{
    			string[] array = File.ReadAllLines(files[i]);
    			for (int j = 0; j < array.Length; j++)
    			{
    				string text = array[j];
    				if (!text.TrimStart().StartsWith("//") && text.Contains(token))
    				{
    					stringBuilder.AppendLine("      " + Path.GetFileName(files[i]) + ":" + (j + 1) + ": " + text.Trim());
    				}
    			}
    		}
    		return stringBuilder.ToString();
    	}

    	private static string LayerName(int i)
    	{
    		if (!(LayerMask.LayerToName(i) == ""))
    		{
    			return LayerMask.LayerToName(i);
    		}
    		return i.ToString();
    	}

    	private static string PathName(Transform t)
    	{
    		List<string> list = new List<string>();
    		Transform val = t;
    		while ((Object)(object)val != (Object)null)
    		{
    			list.Insert(0, ((Object)val).name);
    			val = val.parent;
    		}
    		return string.Join("/", list.ToArray());
    	}

    	private static string Under(Transform t, string root)
    	{
    		Transform val = t;
    		while ((Object)(object)val != (Object)null)
    		{
    			if (((Object)val).name == root)
    			{
    				return "yes (" + root + ")";
    			}
    			val = val.parent;
    		}
    		return "NO — not under " + root;
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/l1_probe10.txt"), sb.ToString());
    		Debug.Log((object)"[Echoes] L1 probe — see Temp/l1_probe10.txt");
    	}
    }
}