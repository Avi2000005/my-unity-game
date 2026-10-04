using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools;

public static class L1Opening
{
	private const string Report = "Temp/l1_opening.txt";

	[MenuItem("Tools/Echoes/Place Beat 1 and the Checkpoint", priority = 42)]
	public static void Run()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[Echoes] place beat 1 and the checkpoint");
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
		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
		if ((Object)(object)ariMover == (Object)null)
		{
			stringBuilder.AppendLine("  FATAL: no AriMover.");
			Finish(stringBuilder);
			return;
		}
		AriHealth component = ((Component)ariMover).GetComponent<AriHealth>();
		Vector3 position = ((Component)ariMover).transform.position;
		stringBuilder.AppendLine("  Ari at " + position.ToString("F2") + ", AriHealth " + (((Object)(object)component != (Object)null) ? "on her" : "MISSING"));
		Beat1Intro beat1Intro = Object.FindAnyObjectByType<Beat1Intro>((FindObjectsInactive)1);
		if ((Object)(object)beat1Intro == (Object)null)
		{
			beat1Intro = new GameObject("L1_Beat1").AddComponent<Beat1Intro>();
			stringBuilder.AppendLine("  + Beat1Intro on a new root 'L1_Beat1'");
		}
		else
		{
			stringBuilder.AppendLine("  Beat1Intro already present on '" + ((Object)beat1Intro).name + "'");
		}
		stringBuilder.AppendLine("    card is up until E is pressed; the four button prompts tick off as she uses them");
		stringBuilder.AppendLine("    it does NOT gate the level — she can walk off with prompts unused, which is what makes them a tutorial and not a cutscene");
		Beat5Director beat5Director = Object.FindAnyObjectByType<Beat5Director>((FindObjectsInactive)1);
		if ((Object)(object)beat5Director == (Object)null)
		{
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("  NO Beat5Director in the scene, so the checkpoint's position cannot be derived from the beat's own enter point. Not placing it — a checkpoint at a guessed x would silently stop matching the yard.");
			Finish(stringBuilder);
			return;
		}
		float enterX = beat5Director.EnterX;
		Vector3 position2 = new Vector3(enterX, 0f, ((Component)ariMover).transform.position.z);
		LevelCheckpoint levelCheckpoint = Object.FindAnyObjectByType<LevelCheckpoint>((FindObjectsInactive)1);
		if ((Object)(object)levelCheckpoint == (Object)null)
		{
			GameObject val = new GameObject("L1_Checkpoint_Beat5");
			val.transform.position = position2;
			levelCheckpoint = val.AddComponent<LevelCheckpoint>();
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("  + LevelCheckpoint on a new root at " + position2.ToString("F2"));
		}
		else
		{
			((Component)levelCheckpoint).transform.position = position2;
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("  LevelCheckpoint already present, moved to " + position2.ToString("F2"));
		}
		SetField(levelCheckpoint, "ari", ariMover);
		if ((Object)(object)component != (Object)null)
		{
			SetField(levelCheckpoint, "health", component);
		}
		stringBuilder.AppendLine("    position derived from Beat5Director.EnterX = " + enterX.ToString("F1") + " m, not typed — the beat and the checkpoint cannot drift apart");
		position = ((Component)ariMover).transform.position;
		stringBuilder.AppendLine("    z taken from Ari's current " + position.z.ToString("F2") + ", so she arrives on the lane she walked in on");
		int num = 0;
		StringBuilder stringBuilder2 = new StringBuilder();
		MonoBehaviour[] array = Object.FindObjectsByType<MonoBehaviour>((FindObjectsInactive)1);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is IResettable)
			{
				num++;
				stringBuilder2.Append(" '").Append(((Object)array[i]).name).Append('\'');
			}
		}
		stringBuilder.AppendLine("    a retry will reset " + num + " beat(s) ->" + stringBuilder2);
		if (num == 0)
		{
			stringBuilder.AppendLine("    ^ NONE. Ari would be sent back with every beat already done and the level unplayable from there.");
		}
		if ((Object)(object)component == (Object)null)
		{
			stringBuilder.AppendLine("    AriHealth is NOT on her, so a retry will NOT restore her health — she would come back at whatever she died with, which is dead.");
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(SaveAfter.Save("beat 1 intro + beat 5 checkpoint"));
		Finish(stringBuilder);
	}

	private static void SetField(object target, string field, object value)
	{
		FieldInfo field2 = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (field2 == null)
		{
			Debug.LogError((object)("[Echoes] L1Opening: " + target.GetType().Name + " has no field '" + field + "'."));
		}
		else
		{
			field2.SetValue(target, value);
		}
	}

	private static void Finish(StringBuilder sb)
	{
		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/l1_opening.txt"), sb.ToString());
		Debug.Log((object)"[Echoes] beat 1 + checkpoint placed — see Temp/l1_opening.txt");
	}
}
