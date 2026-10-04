using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools;

public static class L1Beat45
{
	private const string Report = "Temp/l1_beat45.txt";

	[MenuItem("Tools/Echoes/Place Beat 4.5 and Audit the Level", priority = 62)]
	public static void Run()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[Echoes] place beat 4.5, then audit the whole level");
		if (EditorApplication.isPlaying)
		{
			stringBuilder.AppendLine("  refused: in play mode");
			Finish(stringBuilder);
			return;
		}
		Scene activeScene = SceneManager.GetActiveScene();
		if (!activeScene.IsValid() || activeScene.rootCount == 0)
		{
			stringBuilder.AppendLine("  FATAL: no scene open.");
			Finish(stringBuilder);
			return;
		}
		Beat4HealthIntro beat4HealthIntro = Object.FindAnyObjectByType<Beat4HealthIntro>((FindObjectsInactive)1);
		if ((Object)(object)beat4HealthIntro == (Object)null)
		{
			beat4HealthIntro = new GameObject("L1_Beat4_5").AddComponent<Beat4HealthIntro>();
			stringBuilder.AppendLine("  + Beat4HealthIntro on a new root 'L1_Beat4_5'");
		}
		else
		{
			stringBuilder.AppendLine("  Beat4HealthIntro already present on '" + ((Object)beat4HealthIntro).name + "'");
		}
		Beat4Director beat4Director = Object.FindAnyObjectByType<Beat4Director>((FindObjectsInactive)1);
		SetField(beat4HealthIntro, "beat4", beat4Director);
		stringBuilder.AppendLine("    -> Beat4Director " + (((Object)(object)beat4Director != (Object)null) ? ("'" + ((Object)beat4Director).name + "' (the card waits for its Done phase)") : "NOT FOUND — the card would never appear and nothing would say so"));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("=== BEAT COVERAGE ===");
		stringBuilder.AppendLine("(counted in the open scene, not on disk)");
		int num = 0;
		num += Want(stringBuilder, "1  intro + button tutorial", typeof(Beat1Intro));
		num += Want(stringBuilder, "2  broken tree, brush swing", typeof(SleepingTree));
		num += Want(stringBuilder, "3  Mono awakens", typeof(MonoCompanion));
		num += Want(stringBuilder, "4  gate chase", typeof(Beat4Director));
		num += Want(stringBuilder, "4.5 health bar introduced", typeof(Beat4HealthIntro));
		num += Want(stringBuilder, "5  three ink crawlers", typeof(Beat5Director));
		num += Want(stringBuilder, "6  blue fragment to carry", typeof(ColourFragment));
		num += Want(stringBuilder, "7  fountain fix + blue burst", typeof(FountainFix));
		num += Want(stringBuilder, "-- colour stays shut until 7", typeof(ColourGate));
		num += Want(stringBuilder, "-- lose and retry from beat 5", typeof(LevelCheckpoint));
		num += Want(stringBuilder, "-- E to interact", typeof(AriInteract));
		num += Want(stringBuilder, "-- health + HUD", typeof(AriHealth));
		num += Want(stringBuilder, "-- crawlers rise out of the ground", typeof(InkCrawlerEmerge));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("  missing: " + num + ((num == 0) ? "  <- every beat in the brief has something behind it" : "  <- each missing row is a beat that will not happen"));
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("=== THE TWO THAT FAIL SILENTLY ===");
		int num2 = 0;
		int num3 = 0;
		ColorRestoreTarget[] array = Object.FindObjectsByType<ColorRestoreTarget>((FindObjectsInactive)1);
		for (int i = 0; i < array.Length; i++)
		{
			if (!((Object)(object)array[i] == (Object)null))
			{
				num3++;
				if (array[i].Exempt)
				{
					num2++;
				}
			}
		}
		stringBuilder.AppendLine("  colour targets in the level: " + num3 + ", exempt: " + num2);
		stringBuilder.AppendLine("    " + num2 switch
		{
			0 => "NONE. Beat 7 will ask the water for blue, the gate will refuse, and the level will complete grey while reporting success.", 
			1 => "exactly one — the fountain, which is the only thing allowed to take colour before Beat 7 ends", 
			_ => num2 + " of them. Colour could arrive somewhere it should not, which is the failure the gate exists to prevent.", 
		});
		FountainFix fountainFix = Object.FindAnyObjectByType<FountainFix>((FindObjectsInactive)1);
		stringBuilder.AppendLine("  FountainFix: " + (((Object)(object)fountainFix != (Object)null) ? ("present on '" + ((Object)fountainFix).name + "'") : "ABSENT — nothing will consume the fragment she is carrying"));
		if ((Object)(object)fountainFix != (Object)null && GetField(fountainFix, "water") == null)
		{
			stringBuilder.AppendLine("    its `water` is NULL — the fix has no target to turn blue and would complete without any effect");
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(SaveAfter.Save("beat 4.5"));
		Finish(stringBuilder);
	}

	private static int Want(StringBuilder sb, string label, Type t)
	{
		int num = Object.FindObjectsByType(t, (FindObjectsInactive)1).Length;
		List<string> list = new List<string>();
		MonoBehaviour[] array = Object.FindObjectsByType<MonoBehaviour>((FindObjectsInactive)1);
		for (int i = 0; i < array.Length; i++)
		{
			if (t.IsInstanceOfType(array[i]))
			{
				list.Add("'" + ((Object)array[i]).name + "'");
			}
		}
		sb.AppendLine("  " + ((num > 0) ? "ok  " : "MISS") + "  " + label.PadRight(34) + num + "  " + string.Join(" ", list.ToArray()));
		return (num <= 0) ? 1 : 0;
	}

	private static void SetField(object target, string field, object value)
	{
		FieldInfo field2 = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (field2 == null)
		{
			Debug.LogError((object)("[Echoes] L1Beat45: " + target.GetType().Name + " has no field '" + field + "'."));
		}
		else
		{
			field2.SetValue(target, value);
		}
	}

	private static object GetField(object target, string field)
	{
		FieldInfo field2 = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (!(field2 != null))
		{
			return null;
		}
		return field2.GetValue(target);
	}

	private static void Finish(StringBuilder sb)
	{
		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/l1_beat45.txt"), sb.ToString());
		Debug.Log((object)"[Echoes] beat 4.5 placed + audit — see Temp/l1_beat45.txt");
	}
}
