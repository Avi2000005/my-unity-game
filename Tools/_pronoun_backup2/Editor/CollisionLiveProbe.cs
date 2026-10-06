using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class CollisionLiveProbe
    {
    	private const string Report = "Temp/collision_live.txt";

    	private const float Dt = 1f / 60f;

    	[MenuItem("Tools/Echoes/Probe Collision", priority = 99)]
    	public static void Run()
    	{
    		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder sb = new StringBuilder();
    		sb.AppendLine("[Echoes] does the wall sweep work, and did it break the jump?");
    		sb.AppendLine("mode: " + (EditorApplication.isPlaying ? "play" : "edit"));
    		GameObject ari = GameObject.Find("Ari");
    		if ((Object)(object)ari == (Object)null)
    		{
    			sb.AppendLine("\nNo Ari in the scene.");
    			Finish(sb);
    			return;
    		}
    		Animator anim = ari.GetComponent<Animator>();
    		AriMover mover = ari.GetComponent<AriMover>();
    		if ((Object)(object)anim == (Object)null || (Object)(object)mover == (Object)null)
    		{
    			sb.AppendLine("\nAri is missing his Animator or his AriMover. Nothing to measure.");
    			Finish(sb);
    			return;
    		}
    		bool enabled = ((Behaviour)anim).enabled;
    		if (!((Behaviour)anim).enabled)
    		{
    			((Behaviour)anim).enabled = true;
    		}
    		bool enabled2 = ((Behaviour)mover).enabled;
    		if (EditorApplication.isPlaying)
    		{
    			((Behaviour)mover).enabled = false;
    		}
    		Vector3 position = ari.transform.position;
    		List<GameObject> probes = new List<GameObject>();
    		try
    		{
    			Physics.SyncTransforms();
    			Guard(sb, "head-on wall", () =>
    			{
    				HeadOn(ari, anim, mover, probes, sb);
    			});
    			Guard(sb, "45 degree wall", () =>
    			{
    				Angled(ari, anim, mover, probes, sb);
    			});
    			Guard(sb, "low step", () =>
    			{
    				StepUp(ari, anim, mover, probes, sb);
    			});
    			Guard(sb, "no collision off", () =>
    			{
    				SwitchedOff(ari, anim, mover, probes, sb);
    			});
    			Guard(sb, "jump regression", () =>
    			{
    				JumpStillWorks(ari, anim, mover, probes, sb);
    			});
    		}
    		finally
    		{
    			foreach (GameObject item in probes)
    			{
    				if ((Object)(object)item != (Object)null)
    				{
    					Object.DestroyImmediate((Object)(object)item);
    				}
    			}
    			ari.transform.position = position;
    			((Behaviour)mover).enabled = enabled2;
    			((Behaviour)anim).enabled = enabled;
    		}
    		CountVillageColliders(sb);
    		Finish(sb);
    	}

    	private static void Guard(StringBuilder sb, string what, Action test)
    	{
    		try
    		{
    			test();
    		}
    		catch (Exception ex)
    		{
    			sb.AppendLine("  " + what + " THREW: " + ex.GetType().Name + " — " + (ex.Message ?? "").Split('\n', StringSplitOptions.None)[0]);
    		}
    	}

    	private static void HeadOn(GameObject ari, Animator anim, AriMover mover, List<GameObject> probes, StringBuilder sb)
    	{
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("\n--- head-on into a wall ---");
    		Clear(probes);
    		Settle(mover, anim);
    		Vector3 val = CamForward();
    		Vector3 val2 = Flat(ari.transform.position);
    		GameObject box = Box("probe_wall", val2 + val * 2f + Vector3.up, new Vector3(4f, 3f, 0.4f), val, probes);
    		Physics.SyncTransforms();
    		float num = NearFaceOf(box, val, val2, sb);
    		float num2 = Vector3.Dot(Walk(ari, anim, mover, val, 2f), val);
    		float num3 = num - num2;
    		sb.AppendLine("  he started " + num.ToString("F3") + " m from a 4x3x0.4 m wall and was asked to walk straight at it for 2 s  (unobstructed that would be 4.40 m)");
    		sb.AppendLine("  he stopped " + num3.ToString("F3") + " m short of the face  (mover: TouchingWall=" + mover.TouchingWall + ", swept " + mover.LastSweepRatio.ToString("F2") + ", hit '" + mover.WallName + "')");
    		StringBuilder stringBuilder = sb;
    		string text;
    		if (num3 < -0.05f)
    		{
    			text = "HE WENT THROUGH IT (" + (0f - num3).ToString("F3") + " m inside). The sweep is not working.";
    		}
    		else
    		{
    			text = ((num3 > 0.75f) ? ("stopped, but " + num3.ToString("F2") + " m out — well clear of the wall. bodyRadius is probably too small.") : "stopped at the wall, about one body radius clear. Correct.");
    		}
    		stringBuilder.AppendLine("  VERDICT: " + text);
    	}

    	private static void Angled(GameObject ari, Animator anim, AriMover mover, List<GameObject> probes, StringBuilder sb)
    	{
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("\n--- 45 degrees into a wall ---");
    		Clear(probes);
    		Settle(mover, anim);
    		Vector3 val = CamForward();
    		Vector3 val2 = CamRight();
    		Vector3 val3 = Flat(ari.transform.position);
    		Vector3 val4 = val3 + val2 * 1.5f;
    		ari.transform.position = val4;
    		Physics.SyncTransforms();
    		GameObject box = Box("probe_wall2", val3 + val * 2f + Vector3.up, new Vector3(12f, 3f, 0.4f), val, probes);
    		Physics.SyncTransforms();
    		float num = NearFaceOf(box, val, val4, sb);
    		Vector3 val5 = val + val2;
    		Vector3 val6 = Walk(ari, anim, mover, val5.normalized, 2f);
    		float num2 = Vector3.Dot(val6, val2);
    		float num3 = Vector3.Dot(val6, val);
    		sb.AppendLine("  wall face " + num.ToString("F3") + " m ahead of where he started; asked to go forward-right for 2 s");
    		sb.AppendLine("  he travelled " + num3.ToString("F3") + " m into the wall and " + num2.ToString("F3") + " m along it  (into - face = " + (num3 - num).ToString("F3") + " m)");
    		StringBuilder stringBuilder = sb;
    		string text;
    		if (num2 > 0.8f && num3 < num + 0.4f)
    		{
    			text = "he slid along it and stopped at it. Corners are survivable.";
    		}
    		else
    		{
    			text = ((num2 <= 0.2f) ? "HE IS PINNED — he stops dead instead of sliding, which makes every corner in the village unusable." : ("he got " + (num3 - num).ToString("F2") + " m through the wall. The slide is not holding."));
    		}
    		stringBuilder.AppendLine("  VERDICT: " + text);
    	}

    	private static void StepUp(GameObject ari, Animator anim, AriMover mover, List<GameObject> probes, StringBuilder sb)
    	{
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
    		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("\n--- a 0.25 m step in front of his ---");
    		Clear(probes);
    		Settle(mover, anim);
    		Vector3 val = CamForward();
    		Vector3 val2 = Flat(ari.transform.position);
    		GameObject val3 = Box("probe_step", val2 + val * 5.2f + Vector3.up * 0.125f, new Vector3(6f, 0.25f, 6f), val, probes);
    		Physics.SyncTransforms();
    		float num = val3.transform.position.y + 0.125f;
    		float y = ari.transform.position.y;
    		Vector3 position = ari.transform.position;
    		Vector3 wish = Stick(val);
    		float num2 = y;
    		for (int i = 0; i < Mathf.RoundToInt(89.99999f); i++)
    		{
    			mover.Step(wish, running: false, 1f / 60f);
    			anim.Update(1f / 60f);
    			num2 = Mathf.Max(num2, ari.transform.position.y);
    		}
    		float num3 = Vector3.Dot(ari.transform.position - position, val);
    		float num4 = num2 - y;
    		sb.AppendLine("  a 6x6x0.25 m step whose near face is " + NearFaceOf(val3, val, val2, sb).ToString("F3") + " m away, top at y=" + num.ToString("F3") + ", his root started at y=" + y.ToString("F3"));
    		sb.AppendLine("  he covered " + num3.ToString("F3") + " m and his root peaked at y=" + num2.ToString("F3") + "  =>  " + num4.ToString("F3") + " m up");
    		sb.AppendLine("  VERDICT: " + ((num4 > 0.18f) ? "he walked up it. Paving seams and kerbs will not stop him." : "HE DID NOT GO UP IT. Every height change in the village stops his dead."));
    	}

    	private static void SwitchedOff(GameObject ari, Animator anim, AriMover mover, List<GameObject> probes, StringBuilder sb)
    	{
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0079: Expected Obj, but got Unknown
    		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("\n--- control: the same wall, sweep switched off ---");
    		Clear(probes);
    		Settle(mover, anim);
    		Vector3 val = CamForward();
    		Vector3 val2 = Flat(ari.transform.position);
    		GameObject box = Box("probe_wall3", val2 + val * 2f + Vector3.up, new Vector3(4f, 3f, 0.4f), val, probes);
    		Physics.SyncTransforms();
    		SerializedObject val3 = new SerializedObject((Object)(object)mover);
    		SerializedProperty val4 = val3.FindProperty("collideWithWalls");
    		bool boolValue = val4.boolValue;
    		val4.boolValue = false;
    		val3.ApplyModifiedPropertiesWithoutUndo();
    		try
    		{
    			float num = NearFaceOf(box, val, val2, sb);
    			float num2 = Vector3.Dot(Walk(ari, anim, mover, val, 2f), val);
    			sb.AppendLine("  collideWithWalls is now " + val4.boolValue + "; wall face " + num.ToString("F3") + " m away; he reached " + num2.ToString("F3") + " m");
    			sb.AppendLine("  VERDICT: " + ((num2 > num + 0.05f) ? "he walked straight through it. The switch works, so the first test's stop was the sweep's doing and not a coincidence." : ("he is STILL being stopped at " + num2.ToString("F2") + " m, so something other than this flag is stopping his and the first test proved nothing.")));
    		}
    		finally
    		{
    			val4.boolValue = boolValue;
    			val3.ApplyModifiedPropertiesWithoutUndo();
    		}
    	}

    	private static void JumpStillWorks(GameObject ari, Animator anim, AriMover mover, List<GameObject> probes, StringBuilder sb)
    	{
    		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("\n--- jump, after the sweep was added ---");
    		Clear(probes);
    		Settle(mover, anim);
    		Vector3 position = ari.transform.position;
    		float y = position.y;
    		mover.Step(Vector3.zero, running: false, 1f / 60f, jump: true);
    		anim.Update(1f / 60f);
    		float num = y;
    		int num2 = 0;
    		bool flag = false;
    		for (int i = 0; i < Mathf.RoundToInt(119.99999f); i++)
    		{
    			mover.Step(Vector3.zero, running: false, 1f / 60f);
    			anim.Update(1f / 60f);
    			num = Mathf.Max(num, ari.transform.position.y);
    			if (mover.IsAirborne)
    			{
    				num2++;
    			}
    			else if (i > 5)
    			{
    				flag = true;
    				break;
    			}
    		}
    		Vector3 position2 = ari.transform.position;
    		float num3 = num - y;
    		Vector2 val = new Vector2(position2.x - position.x, position2.z - position.z);
    		float magnitude = val.magnitude;
    		sb.AppendLine("  peak " + num3.ToString("F3") + " m  (was 1.145 before the sweep)");
    		sb.AppendLine("  airborne for " + ((float)num2 * (1f / 60f)).ToString("F2") + " s, landed: " + flag);
    		sb.AppendLine("  drifted " + magnitude.ToString("F4") + " m sideways while standing still and jumping");
    		sb.AppendLine("  VERDICT: " + ((((num3 > 1f && num3 < 1.3f) & flag) && magnitude < 0.05f) ? "unchanged. The hop still works." : "THE JUMP CHANGED. It was 1.145 m, landed once, no drift."));
    	}

    	private static Vector3 CamForward()
    	{
    		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
    		Camera main = Camera.main;
    		if ((Object)(object)main == (Object)null)
    		{
    			return Vector3.forward;
    		}
    		Vector3 forward = ((Component)main).transform.forward;
    		forward.y = 0f;
    		if (!(forward.sqrMagnitude < 0.0001f))
    		{
    			return forward.normalized;
    		}
    		return Vector3.forward;
    	}

    	private static Vector3 CamRight()
    	{
    		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
    		Camera main = Camera.main;
    		if ((Object)(object)main == (Object)null)
    		{
    			return Vector3.right;
    		}
    		Vector3 right = ((Component)main).transform.right;
    		right.y = 0f;
    		if (!(right.sqrMagnitude < 0.0001f))
    		{
    			return right.normalized;
    		}
    		return Vector3.right;
    	}

    	private static Vector3 Stick(Vector3 worldDir)
    	{
    		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		worldDir.y = 0f;
    		if (worldDir.sqrMagnitude < 1E-06f)
    		{
    			return Vector3.zero;
    		}
    		worldDir.Normalize();
    		return new Vector3(Vector3.Dot(worldDir, CamRight()), 0f, Vector3.Dot(worldDir, CamForward()));
    	}

    	private static Vector3 Walk(GameObject ari, Animator anim, AriMover mover, Vector3 worldDir, float seconds)
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 position = ari.transform.position;
    		Vector3 wish = Stick(worldDir);
    		int num = Mathf.RoundToInt(seconds / (1f / 60f));
    		for (int i = 0; i < num; i++)
    		{
    			mover.Step(wish, running: false, 1f / 60f);
    			anim.Update(1f / 60f);
    		}
    		return ari.transform.position - position;
    	}

    	private static void Settle(AriMover mover, Animator anim)
    	{
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		for (int i = 0; i < 30; i++)
    		{
    			mover.Step(Vector3.zero, running: false, 1f / 60f);
    			anim.Update(1f / 60f);
    		}
    		Physics.SyncTransforms();
    	}

    	private static Vector3 Flat(Vector3 v)
    	{
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		v.y = 0f;
    		return v;
    	}

    	private static GameObject Box(string name, Vector3 centre, Vector3 size, Vector3 faceDir, List<GameObject> into)
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0007: Expected Obj, but got Unknown
    		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
    		GameObject val = new GameObject(name);
    		val.transform.position = centre;
    		val.transform.localScale = size;
    		faceDir.y = 0f;
    		if (faceDir.sqrMagnitude > 1E-06f)
    		{
    			faceDir.Normalize();
    			val.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up);
    		}
    		val.AddComponent<BoxCollider>();
    		val.layer = 0;
    		into.Add(val);
    		return val;
    	}

    	private static float NearFaceOf(GameObject box, Vector3 fwd, Vector3 origin, StringBuilder sb)
    	{
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
    		fwd.y = 0f;
    		fwd.Normalize();
    		float num = Vector3.Dot(box.transform.forward, fwd);
    		if (num < 0.99f)
    		{
    			sb.AppendLine("    !! the probe box is facing " + num.ToString("F2") + " of the way the walk is going — its distances below are nonsense");
    		}
    		float num2 = box.transform.localScale.z * 0.5f;
    		return Vector3.Dot(box.transform.position, fwd) - num2 - Vector3.Dot(origin, fwd);
    	}

    	private static void Clear(List<GameObject> probes)
    	{
    		foreach (GameObject probe in probes)
    		{
    			if ((Object)(object)probe != (Object)null)
    			{
    				Object.DestroyImmediate((Object)(object)probe);
    			}
    		}
    		probes.Clear();
    		Physics.SyncTransforms();
    	}

    	private static void CountVillageColliders(StringBuilder sb)
    	{
    		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
    		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
    		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0289: Unknown result type (might be due to invalid IL or missing references)
    		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("\n--- village colliders ---");
    		Collider[] array = Object.FindObjectsByType<Collider>();
    		sb.AppendLine("  " + array.Length + " collider(s) in the scene");
    		if (array.Length == 0)
    		{
    			sb.AppendLine("  NONE. The sweep has nothing to collide with — he will still walk through every wall until the kit has colliders.");
    			return;
    		}
    		foreach (IGrouping<string, Collider> item in from c in array
    			group c by LayerName(((Component)c).gameObject.layer) into g
    			orderby g.Count() descending
    			select g)
    		{
    			sb.AppendLine($"  layer {item.Key}: {item.Count()}");
    		}
    		int num = array.Count((Collider c) => c.isTrigger);
    		int num2 = array.OfType<MeshCollider>().Count();
    		int num3 = array.OfType<BoxCollider>().Count();
    		int num4 = array.OfType<CapsuleCollider>().Count();
    		sb.AppendLine($"  {num3} box, {num4} capsule, {num2} mesh; " + $"{num} are triggers and are ignored by the sweep");
    		foreach (var item2 in array.Where((Collider c) => !c.isTrigger).Select((Collider c) =>
    		{
    			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
    			return new
    			{
    				Col = c,
    				Box = c.bounds
    			};
    		}).Where(x =>
    		{
    			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    			Bounds box2 = x.Box;
    			Vector3 size = box2.size;
    			return size.sqrMagnitude > 4f;
    		})
    			.OrderByDescending(x =>
    			{
    				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    				//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    				//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    				Bounds box2 = x.Box;
    				Vector3 size = box2.size;
    				return size.sqrMagnitude;
    			})
    			.Take(6))
    		{
    			object[] array2 = new object[4]
    			{
    				((Object)item2.Col).name,
    				null,
    				null,
    				null
    			};
    			Bounds box = item2.Box;
    			array2[1] = box.size.x;
    			box = item2.Box;
    			array2[2] = box.size.y;
    			box = item2.Box;
    			array2[3] = box.size.z;
    			string text = string.Format("  big: {0}  {1:0.0}x{2:0.0}x{3:0.0} m", array2);
    			box = item2.Box;
    			object arg = box.center.x;
    			box = item2.Box;
    			object arg2 = box.center.y;
    			box = item2.Box;
    			sb.AppendLine(text + $"  at {arg:0.0},{arg2:0.0},{box.center.z:0.0}");
    		}
    	}

    	private static string LayerName(int layer)
    	{
    		string text = LayerMask.LayerToName(layer);
    		if (!string.IsNullOrEmpty(text))
    		{
    			return text;
    		}
    		return layer.ToString();
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp/collision_live.txt");
    		Directory.CreateDirectory(Path.GetDirectoryName(path));
    		File.WriteAllText(path, sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}