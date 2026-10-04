using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class CameraLiveProbe
    {
    	[MenuItem("Tools/Echoes/Probe Camera", priority = 101)]
    	public static void Run()
    	{
    		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0230: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
    		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0295: Unknown result type (might be due to invalid IL or missing references)
    		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0329: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0367: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0455: Unknown result type (might be due to invalid IL or missing references)
    		//IL_047f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_048c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		if (!EditorApplication.isPlaying)
    		{
    			stringBuilder.AppendLine("Not in play mode.");
    			Finish(stringBuilder);
    			return;
    		}
    		GameObject val = GameObject.Find("Ari");
    		Camera main = Camera.main;
    		AriFollowCamera ariFollowCamera = (((Object)(object)main != (Object)null) ? ((Component)main).GetComponent<AriFollowCamera>() : null);
    		stringBuilder.AppendLine("--- wiring ---");
    		stringBuilder.AppendLine($"  Ari found={(Object)(object)val != (Object)null}");
    		stringBuilder.AppendLine($"  Main Camera found={(Object)(object)main != (Object)null} " + $"AriFollowCamera present={(Object)(object)ariFollowCamera != (Object)null}");
    		if ((Object)(object)val == (Object)null || (Object)(object)main == (Object)null || (Object)(object)ariFollowCamera == (Object)null)
    		{
    			Finish(stringBuilder);
    			return;
    		}
    		AriMover component = val.GetComponent<AriMover>();
    		stringBuilder.AppendLine($"  AriMover present={(Object)(object)component != (Object)null}");
    		Vector3 position = val.transform.position;
    		Vector3 position2 = ((Component)main).transform.position;
    		float num = Vector3.Distance(position2, position);
    		stringBuilder.AppendLine("\n--- before ---");
    		stringBuilder.AppendLine("  Ari   at " + Fmt(position));
    		stringBuilder.AppendLine($"  camera at {Fmt(position2)}  pitch {((Component)main).transform.eulerAngles.x:F1}");
    		stringBuilder.AppendLine($"  distance to Ari = {num:F3}");
    		stringBuilder.AppendLine($"  Ari in frustum = {InFrustum(main, val)}");
    		stringBuilder.AppendLine("\n--- walking Ari 2.0s on +Z ---");
    		if ((Object)(object)component != (Object)null)
    		{
    			for (int i = 0; i < 120; i++)
    			{
    				component.Step(new Vector3(0f, 0f, 1f), running: false, 1f / 60f);
    			}
    		}
    		Vector3 position3 = val.transform.position;
    		float num2 = Vector3.Distance(position, position3);
    		stringBuilder.AppendLine($"  Ari moved {num2:F3} (expect ~4.4)");
    		for (int j = 0; j < 30; j++)
    		{
    			StepCamera(main, ariFollowCamera);
    		}
    		Vector3 position4 = ((Component)main).transform.position;
    		float num3 = Vector3.Distance(position2, position4);
    		stringBuilder.AppendLine($"  camera moved {num3:F3} (expect > 3 — it must trail her)");
    		stringBuilder.AppendLine("  camera at " + Fmt(position4));
    		stringBuilder.AppendLine($"  distance to Ari = {Vector3.Distance(position4, position3):F3}");
    		Vector3 val2 = position4 - position2;
    		Vector3 normalized = val2.normalized;
    		val2 = position3 - position;
    		bool flag = Vector3.Dot(normalized, val2.normalized) > 0.9f;
    		stringBuilder.AppendLine($"  trailing her (not sliding sideways): {flag}");
    		stringBuilder.AppendLine($"  Ari in frustum now = {InFrustum(main, val)}");
    		stringBuilder.AppendLine("\n--- zoom ---");
    		SetDistance(ariFollowCamera, 0.01f);
    		StepCamera(main, ariFollowCamera, 20);
    		float num4 = Vector3.Distance(((Component)main).transform.position, val.transform.position);
    		stringBuilder.AppendLine($"  asked for 0.01, settled at {num4:F3} (must be >= 2.5)");
    		SetDistance(ariFollowCamera, 500f);
    		StepCamera(main, ariFollowCamera, 20);
    		float num5 = Vector3.Distance(((Component)main).transform.position, val.transform.position);
    		stringBuilder.AppendLine($"  asked for 500, settled at {num5:F3} (must be <= 14)");
    		stringBuilder.AppendLine("\n--- pitch clamp ---");
    		SetPitch(ariFollowCamera, -89f);
    		StepCamera(main, ariFollowCamera, 10);
    		float num6 = NormalisedPitch(((Component)main).transform.eulerAngles.x);
    		SetPitch(ariFollowCamera, 89f);
    		StepCamera(main, ariFollowCamera, 10);
    		float num7 = NormalisedPitch(((Component)main).transform.eulerAngles.x);
    		stringBuilder.AppendLine($"  asked -89 -> {num6:F1} (must be >= -8)");
    		stringBuilder.AppendLine($"  asked  89 -> {num7:F1} (must be <= 72)");
    		stringBuilder.AppendLine("\n--- sanity ---");
    		stringBuilder.AppendLine($"  camera y {((Component)main).transform.position.y:F3} vs Ari y {val.transform.position.y:F3} " + "(camera must stay above her feet)");
    		val.transform.position = position;
    		((Component)main).transform.position = position2;
    		Finish(stringBuilder);
    	}

    	private static void StepCamera(Camera cam, AriFollowCamera follow, int times = 1)
    	{
    		MethodInfo method = typeof(AriFollowCamera).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
    		if (!(method == null))
    		{
    			for (int i = 0; i < times; i++)
    			{
    				method.Invoke(follow, null);
    			}
    		}
    	}

    	private static void SetDistance(AriFollowCamera follow, float d)
    	{
    		typeof(AriFollowCamera).GetField("_distance", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(follow, d);
    	}

    	private static void SetPitch(AriFollowCamera follow, float p)
    	{
    		typeof(AriFollowCamera).GetField("_pitch", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(follow, p);
    	}

    	private static float NormalisedPitch(float eulerX)
    	{
    		if (!(eulerX > 180f))
    		{
    			return eulerX;
    		}
    		return eulerX - 360f;
    	}

    	private static bool InFrustum(Camera cam, GameObject target)
    	{
    		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
    		Plane[] array = GeometryUtility.CalculateFrustumPlanes(cam);
    		Bounds val = new Bounds(target.transform.position + Vector3.up, new Vector3(1f, 2f, 1f));
    		return GeometryUtility.TestPlanesAABB(array, val);
    	}

    	private static string Fmt(Vector3 v)
    	{
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
    		return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		string text = sb.ToString();
    		File.WriteAllText("Temp/camera_live.txt", text);
    		Debug.Log((object)("[Echoes] Camera probe\n" + text));
    	}
    }
}