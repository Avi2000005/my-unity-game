using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools;

public static class L1Spine
{
	private struct Mark
	{
		public string label;

		public Vector3 at;
	}

	private const string Report = "Temp/l1_spine.txt";

	[MenuItem("Tools/Echoes/Measure the Level's Route", priority = 45)]
	public static void Run()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_034d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0429: Unknown result type (might be due to invalid IL or missing references)
		//IL_043c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04de: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[Echoes] measure level 1's route");
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
		List<Mark> list = new List<Mark>();
		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
		if ((Object)(object)ariMover != (Object)null)
		{
			Add(list, "spawn   (Ari now)", ((Component)ariMover).transform.position);
		}
		SleepingTree sleepingTree = Object.FindAnyObjectByType<SleepingTree>((FindObjectsInactive)1);
		if ((Object)(object)sleepingTree != (Object)null)
		{
			Add(list, "beat 2  tree", ((Component)sleepingTree).transform.position);
		}
		BeatGate beatGate = Object.FindAnyObjectByType<BeatGate>((FindObjectsInactive)1);
		if ((Object)(object)beatGate != (Object)null)
		{
			Add(list, "beat 4  gate", ((Component)beatGate).transform.position);
		}
		Beat5Director beat5Director = Object.FindAnyObjectByType<Beat5Director>((FindObjectsInactive)1);
		if ((Object)(object)beat5Director != (Object)null)
		{
			Add(list, "beat 5  yard ENTRANCE", new Vector3(beat5Director.EnterX, 0f, 0f));
			Add(list, "beat 5  yard EXIT", new Vector3(beat5Director.ExitX, 0f, 0f));
		}
		ColourFragment colourFragment = Object.FindAnyObjectByType<ColourFragment>((FindObjectsInactive)1);
		if ((Object)(object)colourFragment != (Object)null)
		{
			Add(list, "beat 6  fragment", ((Component)colourFragment).transform.position);
		}
		LevelCheckpoint levelCheckpoint = Object.FindAnyObjectByType<LevelCheckpoint>((FindObjectsInactive)1);
		if ((Object)(object)levelCheckpoint != (Object)null)
		{
			Add(list, "      checkpoint", ((Component)levelCheckpoint).transform.position);
		}
		List<Transform> list2 = new List<Transform>();
		activeScene = SceneManager.GetActiveScene();
		GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
		for (int i = 0; i < rootGameObjects.Length; i++)
		{
			Transform[] componentsInChildren = rootGameObjects[i].GetComponentsInChildren<Transform>(true);
			foreach (Transform val in componentsInChildren)
			{
				string name = ((Object)val).name;
				if (name == "InkCrawler_AlleyA" || name == "InkCrawler_AlleyB")
				{
					list2.Add(val);
				}
			}
		}
		for (int k = 0; k < list2.Count; k++)
		{
			Add(list, "beat 5.5 alley " + (((Object)list2[k]).name.EndsWith("A") ? "A" : "B"), list2[k].position);
		}
		Transform val2 = Root("Fountain");
		if ((Object)(object)val2 != (Object)null)
		{
			Add(list, "beat 7  fountain", val2.position);
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("=== WHERE THINGS ARE ===");
		Mark mark;
		for (int l = 0; l < list.Count; l++)
		{
			string text = list[l].label.PadRight(26);
			mark = list[l];
			stringBuilder.AppendLine("  " + text + mark.at.ToString("F1"));
		}
		if (list.Count == 0)
		{
			stringBuilder.AppendLine("  NOTHING FOUND. Every beat marker is missing, so there is no route to report.");
			Finish(stringBuilder);
			return;
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("=== IN SCRIPT ORDER ===");
		for (int m = 0; m < list.Count; m++)
		{
			if (m == 0)
			{
				stringBuilder.AppendLine("  " + list[m].label);
				continue;
			}
			float num = Vector3.Distance(list[m].at, list[m - 1].at);
			stringBuilder.AppendLine("  " + list[m].label.PadRight(26) + num.ToString("F1").PadLeft(6) + " m from the last  " + ((num > 45f) ? "  <-- a long walk" : ""));
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("=== DOES THE ROUTE ADD UP? ===");
		if ((Object)(object)beat5Director == (Object)null)
		{
			stringBuilder.AppendLine("  no Beat5Director, so the yard's place in the route cannot be compared with anything.");
		}
		else
		{
			float enterX = beat5Director.EnterX;
			float exitX = beat5Director.ExitX;
			Vector3 val3 = new Vector3((enterX + exitX) * 0.5f, 0f, 0f);
			for (int n = 0; n < list2.Count; n++)
			{
				float num2 = Vector3.Distance(list2[n].position, val3);
				stringBuilder.AppendLine("  alley " + (((Object)list2[n]).name.EndsWith("A") ? "A" : "B") + " is " + num2.ToString("F1") + " m from the middle of the yard.");
			}
			stringBuilder.AppendLine();
			if (list2.Count > 0)
			{
				float num3 = Vector3.Distance(list2[0].position, val3);
				stringBuilder.AppendLine("  " + ((num3 < 20f) ? "the alley is INSIDE the yard's neighbourhood, so a beat between 5 and 6 can be placed there and the numbering is consistent." : ("the alley is " + num3.ToString("F0") + " m from the yard. A beat numbered 5.5 cannot be walked between them without a " + num3.ToString("F0") + " m detour, so either the alley moves, the numbering is wrong, or the yard's own coordinates are not where the village is. This is a decision, not a bug to fix quietly.")));
			}
			else
			{
				stringBuilder.AppendLine("  no InkCrawler_AlleyA/B objects in the scene at all. Beat 5.5 has no geometry of its own, so there is nothing to place crawlers into.");
			}
			List<Mark> list3 = new List<Mark>(list);
			list3.Sort((Mark a, Mark b) =>
			{
				//IL_000d: Unknown result type (might be due to invalid IL or missing references)
				return a.at.x.CompareTo(b.at.x);
			});
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("  by x, west to east:");
			for (int num4 = 0; num4 < list3.Count; num4++)
			{
				mark = list3[num4];
				stringBuilder.AppendLine("    " + mark.at.x.ToString("F1").PadLeft(6) + "  " + list3[num4].label);
			}
		}
		Finish(stringBuilder);
	}

	private static void Add(List<Mark> into, string label, Vector3 at)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		into.Add(new Mark
		{
			label = label,
			at = at
		});
	}

	private static Transform Root(string n)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		Scene activeScene = SceneManager.GetActiveScene();
		GameObject[] rootGameObjects = activeScene.GetRootGameObjects();
		for (int i = 0; i < rootGameObjects.Length; i++)
		{
			if (((Object)rootGameObjects[i]).name == n)
			{
				return rootGameObjects[i].transform;
			}
		}
		return null;
	}

	private static void Finish(StringBuilder sb)
	{
		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/l1_spine.txt"), sb.ToString());
		Debug.Log((object)"[Echoes] route measured — see Temp/l1_spine.txt");
	}
}
