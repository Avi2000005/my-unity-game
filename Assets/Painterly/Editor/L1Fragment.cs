using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools;

public static class L1Fragment
{
	private const string Report = "Temp/l1_fragment.txt";

	[MenuItem("Tools/Echoes/Place the Blue Fragment", priority = 61)]
	public static void Run()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0344: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_041f: Unknown result type (might be due to invalid IL or missing references)
		//IL_045d: Unknown result type (might be due to invalid IL or missing references)
		//IL_047c: Unknown result type (might be due to invalid IL or missing references)
		//IL_048e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0493: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0535: Unknown result type (might be due to invalid IL or missing references)
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("[Echoes] place the blue water fragment (beat 6)");
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
		Transform val = Root("L1_Beat5");
		if ((Object)(object)val == (Object)null)
		{
			stringBuilder.AppendLine("  FATAL: no L1_Beat5 root. Run Beat5Setup first.");
			Finish(stringBuilder);
			return;
		}
		Collider[] componentsInChildren = ((Component)val).GetComponentsInChildren<Collider>(true);
		int num = 0;
		List<Bounds> list = new List<Bounds>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (!((Object)(object)((Component)componentsInChildren[i]).GetComponentInParent<InkCrawler>() != (Object)null))
			{
				num++;
				list.Add(componentsInChildren[i].bounds);
			}
		}
		stringBuilder.AppendLine("  colliders under the yard that are not crawlers: " + num);
		if (num == 0)
		{
			stringBuilder.AppendLine("  FATAL: no walls found, so 'near a wall' cannot be honoured. Placing it in the open would be a guess, and Beat 6 is written against a wall.");
			Finish(stringBuilder);
			return;
		}
		Beat5Director beat5Director = Object.FindAnyObjectByType<Beat5Director>((FindObjectsInactive)1);
		float num2 = (((Object)(object)beat5Director != (Object)null) ? beat5Director.ExitX : 54f);
		List<Bounds> list2 = new List<Bounds>();
		Bounds val2;
		for (int j = 0; j < list.Count; j++)
		{
			val2 = list[j];
			if (val2.max.x > num2 - 14f)
			{
				list2.Add(list[j]);
			}
		}
		List<Bounds> list3 = ((list2.Count > 0) ? list2 : list);
		stringBuilder.AppendLine("  walls within 14 m of the yard exit (x=" + num2.ToString("F0") + "): " + list3.Count + ((list2.Count > 0) ? "" : ("  <- NONE, using all " + list.Count)));
		stringBuilder.AppendLine("    chosen face, from the bounds of the colliders themselves:");
		Bounds val3 = list3[0];
		for (int k = 1; k < list3.Count; k++)
		{
			val2 = list3[k];
			if (val2.max.x > val3.max.x)
			{
				val3 = list3[k];
			}
		}
		stringBuilder.AppendLine("    x " + val3.min.x.ToString("F2") + " .. " + val3.max.x.ToString("F2") + "   y " + val3.min.y.ToString("F2") + " .. " + val3.max.y.ToString("F2") + "   z " + val3.min.z.ToString("F2") + " .. " + val3.max.z.ToString("F2"));
		stringBuilder.AppendLine("    height " + val3.size.y.ToString("F2") + " m — " + ((val3.size.y < 0.5f) ? "this is a kerb or a threshold, not a wall. Placing the fragment against it is legal but it will not read as 'against a wall' on screen." : "a real wall face."));
		float num3 = 0.9f;
		Vector3 val4 = new Vector3(val3.min.x - num3, 0f, val3.center.z);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("  fragment spot: " + val4.ToString("F2") + "  (" + num3.ToString("F1") + " m off the wall face, on the yard side of it)");
		ColourFragment colourFragment = Object.FindAnyObjectByType<ColourFragment>((FindObjectsInactive)1);
		if ((Object)(object)colourFragment == (Object)null)
		{
			colourFragment = new GameObject("L1_BlueFragment").AddComponent<ColourFragment>();
			stringBuilder.AppendLine("  + ColourFragment on a new root 'L1_BlueFragment'");
		}
		else
		{
			stringBuilder.AppendLine("  ColourFragment already present on '" + ((Object)colourFragment).name + "', moving it");
		}
		((Component)colourFragment).transform.position = val4;
		((Object)((Component)colourFragment).gameObject).name = "L1_BlueFragment";
		EditorUtility.SetDirty((Object)(object)colourFragment);
		if (GroundHeight(val4, out var y))
		{
			Vector3 position = ((Component)colourFragment).transform.position;
			position.y = y;
			((Component)colourFragment).transform.position = position;
			stringBuilder.AppendLine("    dropped to the floor at y = " + y.ToString("F2") + " (raycast, not assumed)");
		}
		else
		{
			stringBuilder.AppendLine("    NO GROUND under it — left at y=0 and it may be buried or floating. Nothing below the ray hit.");
		}
		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
		AriInteract ariInteract = (((Object)(object)ariMover != (Object)null) ? ((Component)ariMover).GetComponent<AriInteract>() : Object.FindAnyObjectByType<AriInteract>((FindObjectsInactive)1));
		stringBuilder.AppendLine();
		if ((Object)(object)ariInteract == (Object)null)
		{
			stringBuilder.AppendLine("  reach NOT CHECKED — no AriInteract in the scene. AriWiring must run first.");
		}
		else
		{
			float reach = ariInteract.Reach;
			_ = (Object)(object)ariMover != (Object)null;
			float num4 = Mathf.Abs(num2 - val4.x);
			stringBuilder.AppendLine("  reach check, against AriInteract.Reach = " + reach.ToString("F2") + " m:");
			stringBuilder.AppendLine("    from the yard exit (x=" + num2.ToString("F0") + "): " + num4.ToString("F2") + " m  " + ((num4 <= reach) ? "OK — she can pick it up on the way" : "TOO FAR — she cannot pick it up there and must cross the yard"));
			ColourFragment[] array = Object.FindObjectsByType<ColourFragment>((FindObjectsInactive)1);
			stringBuilder.AppendLine("    ColourFragments in the level: " + array.Length + " (wanted 1" + ((array.Length != 1) ? "  <-- MISMATCH" : "") + ")");
			FountainFix[] array2 = Object.FindObjectsByType<FountainFix>((FindObjectsInactive)1);
			stringBuilder.AppendLine("    FountainFix components: " + array2.Length + ((array2.Length == 0) ? "  <- none, so Beat 7 cannot run even if she has the fragment" : ""));
		}
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("  NOTE the order this beat needs: she picks the fragment UP while the crawlers are still active (per the brief), so this placement is only correct if the crawlers have not been retired by the time she arrives. That ordering lives in Beat5Director, not here.");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine(SaveAfter.Save("the blue fragment"));
		Finish(stringBuilder);
	}

	private static bool GroundHeight(Vector3 at, out float y)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		y = 0f;
		RaycastHit[] array = Physics.RaycastAll(at + Vector3.up * 6f, Vector3.down, 20f, -1, (QueryTriggerInteraction)1);
		float num = float.MaxValue;
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			if (!((Object)(object)((Component)array[i].collider).GetComponentInParent<InkCrawler>() != (Object)null) && !((Object)(object)((Component)array[i].collider).GetComponentInParent<AriMover>() != (Object)null) && array[i].point.y < num)
			{
				num = array[i].point.y;
				flag = true;
			}
		}
		if (!flag)
		{
			return false;
		}
		y = num;
		return true;
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
		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/l1_fragment.txt"), sb.ToString());
		Debug.Log((object)"[Echoes] fragment placed — see Temp/l1_fragment.txt");
	}
}
