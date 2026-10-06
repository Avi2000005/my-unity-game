using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class AriWiring
    {
    	private const string Report = "Temp/ari_wiring.txt";

    	[MenuItem("Tools/Echoes/Wire Level 1 Systems", priority = 40)]
    	public static void Run()
    	{
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] wire level 1 systems");
    		if (EditorApplication.isPlaying)
    		{
    			stringBuilder.AppendLine("  refused: the editor is in play mode. Component adds made now are thrown away when play stops.");
    			Finish(stringBuilder);
    			return;
    		}
    		Scene activeScene = SceneManager.GetActiveScene();
    		stringBuilder.AppendLine("  scene       : '" + activeScene.name + "' roots=" + activeScene.rootCount + " dirty=" + activeScene.isDirty);
    		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    		if ((Object)(object)ariMover == (Object)null)
    		{
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("  FATAL: no AriMover in the scene. Everything below attaches to him, so nothing can be wired. Find his first and re-run.");
    			Finish(stringBuilder);
    			return;
    		}
    		stringBuilder.AppendLine();
    		Vector3 position = ((Component)ariMover).transform.position;
    		stringBuilder.AppendLine("Ari at " + position.ToString("F2"));
    		stringBuilder.AppendLine("  name        : '" + ((Object)ariMover).name + "'");
    		stringBuilder.AppendLine("  isPrefab    : " + PrefabUtility.IsPartOfPrefabInstance((Object)(object)((Component)ariMover).gameObject) + "  <- if true, the adds below land on the INSTANCE and are lost when it is reverted");
    		stringBuilder.AppendLine();
    		int num = 0;
    		int num2 = 0;
    		num += Ensure<AriHealth>(((Component)ariMover).gameObject, stringBuilder, "health");
    		num += Ensure<AriInteract>(((Component)ariMover).gameObject, stringBuilder, "interact (E)");
    		num += Ensure<AriHudOverlay>(((Component)ariMover).gameObject, stringBuilder, "health bar + flash + slot");
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("  added " + num + ", already there " + num2);
    		AriHealth component = ((Component)ariMover).GetComponent<AriHealth>();
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("wiring check:");
    		stringBuilder.AppendLine("  AriHealth      : " + (((Object)(object)component != (Object)null) ? ("hit cost " + (component.HitCost * 100f).ToString("0") + "% of remaining, low at " + (component.LowAt * 100f).ToString("0") + "%, hits taken " + component.Hits) : "MISSING — the bar will not draw and nothing can be hurt"));
    		AriInteract component2 = ((Component)ariMover).GetComponent<AriInteract>();
    		stringBuilder.AppendLine("  AriInteract   : " + (((Object)(object)component2 != (Object)null) ? ("reach " + component2.Reach.ToString("0.0") + " m from him chest, scan " + component2.ScanInterval.ToString("0.00") + "s") : "MISSING — nothing in the level can be picked up or fixed"));
    		MonoBehaviour[] array = Object.FindObjectsByType<MonoBehaviour>((FindObjectsInactive)1);
    		int num3 = 0;
    		StringBuilder stringBuilder2 = new StringBuilder();
    		for (int i = 0; i < array.Length; i++)
    		{
    			if (array[i] is IInteractable)
    			{
    				num3++;
    				stringBuilder2.Append(" '").Append(((Object)array[i]).name).Append('\'');
    			}
    		}
    		stringBuilder.AppendLine("  interactables : " + num3 + " in the scene" + ((num3 > 0) ? (" ->" + stringBuilder2) : ""));
    		if (num3 == 0)
    		{
    			stringBuilder.AppendLine("                   <- Beats 6 and 7 have nothing to press E on yet. Expected until they are placed.");
    		}
    		int num4 = 0;
    		StringBuilder stringBuilder3 = new StringBuilder();
    		for (int j = 0; j < array.Length; j++)
    		{
    			if (array[j] is IResettable)
    			{
    				num4++;
    				stringBuilder3.Append(" '").Append(((Object)array[j]).name).Append('\'');
    			}
    		}
    		stringBuilder.AppendLine("  resettable    : " + num4 + " ->" + stringBuilder3);
    		LevelCheckpoint[] array2 = Object.FindObjectsByType<LevelCheckpoint>((FindObjectsInactive)1);
    		stringBuilder.AppendLine("  checkpoints   : " + array2.Length);
    		for (int k = 0; k < array2.Length; k++)
    		{
    			stringBuilder.AppendLine("                  '" + ((Object)array2[k]).name + "'");
    		}
    		if (num > 0)
    		{
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine(SaveAfter.Save("Ari's health, interact and HUD"));
    		}
    		Finish(stringBuilder);
    	}

    	private static int Ensure<T>(GameObject go, StringBuilder sb, string what) where T : Component
    	{
    		if ((Object)(object)go.GetComponent<T>() != (Object)null)
    		{
    			sb.AppendLine("  " + what.PadRight(22) + " already on '" + ((Object)go).name + "'");
    			return 0;
    		}
    		T val = go.AddComponent<T>();
    		sb.AppendLine("  " + what.PadRight(22) + " ADDED to '" + ((Object)go).name + "' -> " + ((object)val).GetType().Name);
    		return 1;
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/ari_wiring.txt"), sb.ToString());
    		Debug.Log((object)"[Echoes] wiring done — see Temp/ari_wiring.txt");
    	}
    }
}