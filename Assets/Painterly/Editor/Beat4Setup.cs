using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class Beat4Setup
    {
    	private const string ContainerName = "L1_Beat4";

    	private const string LinesPath = "Assets/Painterly/MonoHintLines.asset";

    	private const string Report = "Temp/beat4_setup.txt";

    	private const string StoneMat = "Assets/Painterly/Materials/RockTrim.mat";

    	private const string WoodMat = "Assets/Painterly/Materials/WoodTrim.mat";

    	private const string IronMat = "Assets/Painterly/Materials/MetalOrnament.mat";

    	private const string PlankMat = "Assets/Painterly/Materials/Beat3_Stump.mat";

    	private const float WallX = 30f;

    	private const float WallThick = 0.4f;

    	private const float WallZMin = 2.5f;

    	private const float WallZMax = 12f;

    	private const float WallHeight = 2.6f;

    	private const float GateZMin = 6.2f;

    	private const float GateZMax = 7.8f;

    	private const float GateHeight = 2.1f;

    	private const float GateThick = 0.3f;

    	private const float GateTravel = 2.25f;

    	private const float GateOpenSeconds = 1.7f;

    	private const float CrawlZMin = 8.6f;

    	private const float CrawlZMax = 9.5f;

    	private const float CrawlCeiling = 1.1f;

    	private const float CrawlXNear = 29.5f;

    	private const float CrawlXFar = 32.9f;

    	private const float CrawlSideThick = 0.25f;

    	private const float CrawlRoofThick = 0.25f;

    	private const float SwitchX = 32.55f;

    	private const float SwitchY = 0.45f;

    	private const float SwitchZ = 9.05f;

    	private const float ErrandX = 32.3f;

    	private const float ErrandZ = 9.05f;

    	private const float PlateX = 28.95f;

    	private const float PlateZ = 5.7f;

    	private const float PlateSize = 1.4f;

    	private const float PlateHeight = 0.3f;

    	private static string _reason;

    	private static int _pass;

    	private static int _fail;

    	private static int _warn;

    	[MenuItem("Tools/Echoes/Build Beat 4 - Gate and Crawlspace", priority = 71)]
    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		if (File.Exists(PathOf(Directory.GetCurrentDirectory(), "Temp/beat4_setup.txt")))
    		{
    			File.Delete(PathOf(Directory.GetCurrentDirectory(), "Temp/beat4_setup.txt"));
    		}
    		try
    		{
    			Body(stringBuilder);
    		}
    		catch (Exception ex)
    		{
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("THREW: " + ex.GetType().Name + ": " + ex.Message);
    			stringBuilder.AppendLine(ex.StackTrace);
    			Debug.LogError((object)("[Echoes] Beat 4 setup threw: " + ex));
    		}
    		finally
    		{
    			Debug.Log((object)("[Echoes] Beat 4 setup\n" + stringBuilder));
    			File.WriteAllText(PathOf(Directory.GetCurrentDirectory(), "Temp/beat4_setup.txt"), stringBuilder.ToString());
    		}
    	}

    	private static void Body(StringBuilder sb)
    	{
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_020d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0212: Unknown result type (might be due to invalid IL or missing references)
    		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
    		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_061d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0624: Expected Obj, but got Unknown
    		//IL_064b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0684: Unknown result type (might be due to invalid IL or missing references)
    		//IL_06a4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_06e9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0729: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0730: Expected Obj, but got Unknown
    		//IL_0782: Unknown result type (might be due to invalid IL or missing references)
    		//IL_07a2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_07f3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0802: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0809: Expected Obj, but got Unknown
    		//IL_0836: Unknown result type (might be due to invalid IL or missing references)
    		//IL_086f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_088f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_090c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0913: Expected Obj, but got Unknown
    		//IL_093a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0957: Unknown result type (might be due to invalid IL or missing references)
    		//IL_095c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_096f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0987: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a25: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a2a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a3d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a4f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a81: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a86: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a98: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a9d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0acb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0ad0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0ae2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0ae7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0dc6: Unknown result type (might be due to invalid IL or missing references)
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			sb.AppendLine("!! The editor is in PLAY MODE.");
    			sb.AppendLine("   Nothing was built. Press Ctrl+P to leave play mode and run this again.");
    			sb.AppendLine("   (This will build into the scene, which play mode would throw away.)");
    			return;
    		}
    		Scene activeScene = SceneManager.GetActiveScene();
    		if (!activeScene.isLoaded)
    		{
    			sb.AppendLine("No scene is open.");
    			return;
    		}
    		Physics.SyncTransforms();
    		_reason = null;
    		_pass = 0;
    		_fail = 0;
    		_warn = 0;
    		sb.AppendLine("BEAT 4 — GATE, PLATE, CRAWLSPACE");
    		sb.AppendLine();
    		sb.AppendLine("scene '" + activeScene.name + "' at " + activeScene.path + (activeScene.isDirty ? "  (UNSAVED CHANGES)" : "  (clean)"));
    		sb.AppendLine("open scenes: " + SceneCount());
    		sb.AppendLine();
    		GameObject val = Named("Ari");
    		GameObject val2 = Named("Mono");
    		if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
    		{
    			AriMover[] array = Object.FindObjectsByType<AriMover>((FindObjectsInactive)1);
    			MonoCompanion[] array2 = Object.FindObjectsByType<MonoCompanion>((FindObjectsInactive)1);
    			if (array.Length != 0)
    			{
    				val = ((Component)array[0]).gameObject;
    			}
    			if (array2.Length != 0)
    			{
    				val2 = ((Component)array2[0]).gameObject;
    			}
    			sb.AppendLine("named lookup missed " + (((Object)(object)val == (Object)null) ? "Ari " : "") + (((Object)(object)val2 == (Object)null) ? "Mono" : "") + " — fell back to a component search");
    		}
    		if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
    		{
    			sb.AppendLine();
    			sb.AppendLine("!! no Ari or Mono here. " + Object.FindObjectsByType<Transform>((FindObjectsInactive)1).Length + " transforms in this scene, " + SceneCount() + " scene(s) open.");
    			sb.AppendLine("   The Level 1 cast is placed into the GREY VILLAGE scene. If the open scene is not that one, this tool has nothing to attach to.");
    			return;
    		}
    		string[] array3 = new string[5]
    		{
    			"Ari  '",
    			((Object)val).name,
    			"' at ",
    			null,
    			null
    		};
    		Vector3 position = val.transform.position;
    		array3[3] = position.ToString("F2");
    		array3[4] = (val.activeInHierarchy ? "" : "   [INACTIVE]");
    		sb.AppendLine(string.Concat(array3));
    		string[] array4 = new string[5]
    		{
    			"Mono '",
    			((Object)val2).name,
    			"' at ",
    			null,
    			null
    		};
    		position = val2.transform.position;
    		array4[3] = position.ToString("F2");
    		array4[4] = (val2.activeInHierarchy ? "" : "   [INACTIVE]");
    		sb.AppendLine(string.Concat(array4));
    		AriMover component = val.GetComponent<AriMover>();
    		MonoCompanion monoCompanion = val2.GetComponent<MonoCompanion>();
    		if ((Object)(object)component == (Object)null)
    		{
    			sb.AppendLine("Ari has no AriMover.");
    			return;
    		}
    		if ((Object)(object)monoCompanion == (Object)null)
    		{
    			monoCompanion = val2.AddComponent<MonoCompanion>();
    			sb.AppendLine("added MonoCompanion to Mono");
    		}
    		sb.AppendLine("--- the two bodies this beat is built on ---");
    		float ariBody = AriBody(sb, "Ari", component, "BodyHeight");
    		float ariRadius = AriBody(sb, "Ari", component, "BodyRadius");
    		float ariStep = AriBody(sb, "Ari", component, "StepHeight");
    		float monoBody = MonoBody(sb, val2);
    		float monoVisible = MonoVisible(sb, val2);
    		float errandRadius = ErrandRadius(sb, monoCompanion);
    		sb.AppendLine();
    		float num = GroundAt(new Vector3(30f, 0f, 7.25f));
    		sb.AppendLine("ground under the wall is " + num.ToString("0.000") + " m");
    		sb.AppendLine();
    		BuildLines(sb);
    		sb.AppendLine();
    		GameObject val3 = Container(sb);
    		Clear(val3);
    		Material val4 = Load("Assets/Painterly/Materials/RockTrim.mat");
    		Material mat = Load("Assets/Painterly/Materials/WoodTrim.mat");
    		Material val5 = Load("Assets/Painterly/Materials/MetalOrnament.mat");
    		Material val6 = Load("Assets/Painterly/Materials/Beat3_Stump.mat");
    		if ((Object)(object)val4 == (Object)null)
    		{
    			sb.AppendLine("NO Assets/Painterly/Materials/RockTrim.mat");
    			return;
    		}
    		float num2 = 29.8f;
    		float num3 = 30.2f;
    		float x = (num2 + num3) * 0.5f;
    		Box(val3.transform, "Beat4_Wall_S", x, num + 1.3f, 4.35f, 0.4f, 2.6f, 3.6999998f, val4);
    		Box(val3.transform, "Beat4_Wall_Pier", x, num + 1.3f, 8.200001f, 0.4f, 2.6f, 0.8000002f, val4);
    		Box(val3.transform, "Beat4_Wall_N", x, num + 1.3f, 10.75f, 0.4f, 2.6f, 2.5f, val4);
    		Box(val3.transform, "Beat4_Lintel_Gate", x, num + 2.1f + 0.25f, 7f, 0.4f, 0.5f, 1.6000004f, val4);
    		Box(val3.transform, "Beat4_Lintel_Crawl", x, num + 1.1f + 0.25f + 0.62499994f, 9.05f, 0.4f, (float)Math.PI * 113f / 284f, 0.8999996f, val4);
    		float y = num + 1.1f + 0.125f;
    		float num4 = 1.35f;
    		Box(val3.transform, "Beat4_Crawl_Roof", 31.2f, y, 9.05f, 3.4000015f, 0.25f, 1.3999996f, mat);
    		Box(val3.transform, "Beat4_Crawl_Side_S", (num3 + 32.9f) * 0.5f, num + num4 * 0.5f, 8.475f, 32.9f - num3, num4, 0.25f, val4);
    		Box(val3.transform, "Beat4_Crawl_Side_N", (num3 + 32.9f) * 0.5f, num + num4 * 0.5f, 9.625f, 32.9f - num3, num4, 0.25f, val4);
    		Box(val3.transform, "Beat4_Crawl_End", 33.025f, num + num4 * 0.5f, 9.05f, 0.25f, num4, 1.3999996f, val4);
    		GameObject val7 = new GameObject("Beat4_Gate");
    		val7.transform.SetParent(val3.transform, false);
    		val7.transform.position = new Vector3(30f, num, 7f);
    		GameObject val8 = GameObject.CreatePrimitive((PrimitiveType)3);
    		((Object)val8).name = "Beat4_Gate_Slab";
    		val8.transform.SetParent(val7.transform, false);
    		val8.transform.localPosition = Vector3.zero;
    		val8.transform.localScale = new Vector3(0.3f, 2.1f, 1.6000004f);
    		if ((Object)(object)val5 != (Object)null)
    		{
    			val8.GetComponent<Renderer>().sharedMaterial = val5;
    		}
    		BeatGate beatGate = val7.AddComponent<BeatGate>();
    		Set((Object)(object)beatGate, "slab", val8.transform);
    		Set((Object)(object)beatGate, "openAxis", Vector3.down);
    		Set((Object)(object)beatGate, "travel", 2.25f);
    		Set((Object)(object)beatGate, "seconds", 1.7f);
    		GameObject val9 = new GameObject("Beat4_Plate");
    		val9.transform.SetParent(val3.transform, false);
    		GameObject val10 = GameObject.CreatePrimitive((PrimitiveType)3);
    		((Object)val10).name = "Beat4_Plate_Plinth";
    		val10.transform.SetParent(val9.transform, false);
    		val10.transform.localPosition = new Vector3(0f, -0.15f, 0f);
    		val10.transform.localScale = new Vector3(1.4f, 0.3f, 1.4f);
    		if ((Object)(object)val6 != (Object)null)
    		{
    			val10.GetComponent<Renderer>().sharedMaterial = val6;
    		}
    		HoldLever holdLever = val9.AddComponent<HoldLever>();
    		Set((Object)(object)holdLever, "ari", component);
    		val9.transform.position = new Vector3(28.95f, num + 0.3f, 5.7f);
    		GameObject val11 = new GameObject("Beat4_Switch");
    		val11.transform.SetParent(val3.transform, false);
    		val11.transform.position = new Vector3(32.55f, num + 0.45f, 9.05f);
    		GameObject val12 = GameObject.CreatePrimitive((PrimitiveType)3);
    		((Object)val12).name = "Beat4_Switch_Boss";
    		val12.transform.SetParent(val11.transform, false);
    		val12.transform.localPosition = Vector3.zero;
    		val12.transform.localScale = new Vector3(0.14f, 0.3f, 0.3f);
    		if ((Object)(object)val5 != (Object)null)
    		{
    			val12.GetComponent<Renderer>().sharedMaterial = val5;
    		}
    		Collider component2 = val12.GetComponent<Collider>();
    		if ((Object)(object)component2 != (Object)null)
    		{
    			component2.isTrigger = true;
    		}
    		LatchingSwitch latchingSwitch = val11.AddComponent<LatchingSwitch>();
    		Set((Object)(object)latchingSwitch, "mono", monoCompanion);
    		Set((Object)(object)latchingSwitch, "lever", holdLever);
    		Set((Object)(object)latchingSwitch, "reach", 1f);
    		GameObject val13 = new GameObject("Beat4_ErrandPoint");
    		val13.transform.SetParent(val3.transform, false);
    		val13.transform.position = new Vector3(32.3f, num, 9.05f);
    		Set((Object)(object)beatGate, "source", latchingSwitch);
    		GameObject val14 = new GameObject("Beat4_Director");
    		val14.transform.SetParent(val3.transform, false);
    		val14.transform.position = new Vector3(28.95f, num + 1f, 5.7f);
    		GameObject val15 = GameObject.Find("MonoChase");
    		MonoChase monoChase = (((Object)(object)val15 == (Object)null) ? null : val15.GetComponent<MonoChase>());
    		Beat4Director beat4Director = val14.AddComponent<Beat4Director>();
    		Set((Object)(object)beat4Director, "gate", beatGate);
    		Set((Object)(object)beat4Director, "lever", holdLever);
    		Set((Object)(object)beat4Director, "latchingSwitch", latchingSwitch);
    		Set((Object)(object)beat4Director, "ari", component);
    		Set((Object)(object)beat4Director, "mono", monoCompanion);
    		Set((Object)(object)beat4Director, "errandPoint", val13.transform);
    		Set((Object)(object)beat4Director, "chase", monoChase);
    		GameObject val16 = new GameObject("BeatWarp");
    		val16.transform.SetParent(val3.transform, false);
    		val16.transform.position = new Vector3(26.5f, num, 7f);
    		BeatWarp target = val16.AddComponent<BeatWarp>();
    		List<BeatWarp.Stop> list = new List<BeatWarp.Stop>
    		{
    			new BeatWarp.Stop
    			{
    				name = "beat4_gate",
    				ari = new Vector3(28.6f, num, 7f),
    				mono = new Vector3(27.8f, num, 6.4f)
    			},
    			new BeatWarp.Stop
    			{
    				name = "beat4_plate",
    				ari = new Vector3(28.95f, num + 0.3f, 5.7f),
    				mono = new Vector3(30.550001f, num, 6.7f)
    			}
    		};
    		Set((Object)(object)target, "stops", list);
    		Set((Object)(object)target, "index", 0);
    		if (list.Count == 0)
    		{
    			Fail(sb, "the warp has no stops, so the beat cannot be tested");
    		}
    		else
    		{
    			BeatWarp.Stop stop = list[0];
    			float num5 = Mathf.Abs(stop.ari.x - 30f);
    			sb.AppendLine("  warp " + list.Count + " stop(s), first is '" + stop.name + "' at " + stop.ari.ToString("F2"));
    			sb.AppendLine();
    			Check(sb, "the warp stop is on Ari's side of the gate", stop.ari.x < 30f, "x " + stop.ari.x.ToString("0.00") + " against a gate at " + 30f.ToString("0.00") + ((stop.ari.x < 30f) ? " — he arrives facing it" : " — he is warped past it and the beat cannot be tested"));
    			Check(sb, "the warp stop is in the gate's opening", stop.ari.z >= 6.2f && stop.ari.z <= 7.8f, "z " + stop.ari.z.ToString("0.00") + " against an opening " + 6.2f.ToString("0.00") + " to " + 7.8f.ToString("0.00"));
    			Check(sb, "the warp stop lands inside the beat's notice range", num5 <= 2.6f, num5.ToString("0.00") + " m from the gate, notice range 2.60 m — he must walk the last stride herself");
    			Check(sb, "the warp stop is outside the crawlspace mouth", stop.ari.x < 29.5f || stop.ari.z < 8.6f || stop.ari.z > 9.5f, "the crawl mouth is at x " + 29.5f.ToString("0.00") + ", z " + 8.6f.ToString("0.00") + " to " + 9.5f.ToString("0.00"));
    		}
    		if ((Object)(object)monoChase == (Object)null)
    		{
    			sb.AppendLine("  no MonoChase in the scene, so Beat 4 will not wait for Beat 3 before it starts");
    		}
    		EditorSceneManager.MarkSceneDirty(activeScene);
    		Physics.SyncTransforms();
    		sb.AppendLine("--- built ---");
    		sb.AppendLine("  " + ((Object)val3).name + " with " + val3.transform.childCount + " children");
    		sb.AppendLine("  wall  x " + num2.ToString("0.00") + " to " + num3.ToString("0.00") + ", z " + 2.5f.ToString("0.0") + " to " + 12f.ToString("0.0") + ", " + 2.6f.ToString("0.00") + " m tall");
    		sb.AppendLine("  gate  " + 1.6000004f.ToString("0.00") + " m wide, " + 2.1f.ToString("0.00") + " m tall, slides " + 2.25f.ToString("0.00") + " m down");
    		sb.AppendLine("  crawl " + 3.4000015f.ToString("0.00") + " m long, " + 0.8999996f.ToString("0.00") + " m wide, ceiling " + 1.1f.ToString("0.00") + " m");
    		sb.AppendLine();
    		Verify(sb, component, monoCompanion, beatGate, val8, holdLever, latchingSwitch, beat4Director, val13.transform, val3, errandRadius, ariBody, ariRadius, ariStep, monoBody, monoVisible, num);
    		sb.AppendLine();
    		sb.AppendLine("VERDICT " + ((_fail == 0) ? "PASS" : "FAIL") + " — " + _pass + " passed, " + _fail + " failed, " + _warn + " warnings");
    		if (_reason != null)
    		{
    			sb.AppendLine("  blocked at " + _reason);
    		}
    	}

    	private static void Verify(StringBuilder sb, AriMover ari, MonoCompanion mono, BeatGate gate, GameObject slab, HoldLever lever, LatchingSwitch latching, Beat4Director dir, Transform errand, GameObject container, float errandRadius, float ariBody, float ariRadius, float ariStep, float monoBody, float monoVisible, float ground)
    	{
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_042b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0430: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0432: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04be: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04c3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04d8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04dd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04ee: Unknown result type (might be due to invalid IL or missing references)
    		//IL_052c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0834: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0843: Unknown result type (might be due to invalid IL or missing references)
    		//IL_08ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_08be: Unknown result type (might be due to invalid IL or missing references)
    		//IL_08c8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_08dc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a1e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a42: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a47: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0aa3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0afb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0b0f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0b14: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0b94: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c06: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c0b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c0f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c14: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c88: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c8d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c91: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c96: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0e02: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0e09: Unknown result type (might be due to invalid IL or missing references)
    		//IL_1070: Unknown result type (might be due to invalid IL or missing references)
    		//IL_1075: Unknown result type (might be due to invalid IL or missing references)
    		sb.AppendLine("--- the crawlspace, measured rather than declared ---");
    		sb.AppendLine();
    		float num = float.MaxValue;
    		float num2 = 0f;
    		List<float> list = new List<float>();
    		for (float num3 = 29.85f; num3 <= 32.75f; num3 += 0.2f)
    		{
    			float num4 = Headroom(new Vector3(num3, ground, 9.05f));
    			if (num4 < num)
    			{
    				num = num4;
    			}
    			if (num4 > num2)
    			{
    				num2 = num4;
    			}
    			list.Add(num4);
    		}
    		list.Sort();
    		float num5 = ((list.Count == 0) ? 0f : list[list.Count / 2]);
    		sb.AppendLine("  headroom down the tunnel over " + list.Count + " samples: min " + num.ToString("0.00") + " m, median " + num5.ToString("0.00") + " m, max " + num2.ToString("0.00") + " m");
    		sb.AppendLine("  built to " + 1.1f.ToString("0.00") + " m");
    		Check(sb, "the crawl ceiling is where it was built", Mathf.Abs(num5 - 1.1f) < 0.03f, "median " + num5.ToString("0.00") + " m against a built " + 1.1f.ToString("0.00") + " m");
    		Check(sb, "nothing in the tunnel is lower than designed", num >= 1.08f, "lowest " + num.ToString("0.00") + " m");
    		sb.AppendLine();
    		sb.AppendLine("  Ari  body " + ariBody.ToString("0.00") + " m   ceiling " + 1.1f.ToString("0.00") + " m   over by " + (ariBody - 1.1f).ToString("0.00") + " m");
    		sb.AppendLine("  Mono body " + monoBody.ToString("0.00") + " m   visible " + monoVisible.ToString("0.00") + " m   under by " + (1.1f - monoVisible).ToString("0.00") + " m");
    		Check(sb, "Ari cannot fit — he is too tall by at least 0.3 m", ariBody - 1.1f >= 0.3f, "he is " + (ariBody - 1.1f).ToString("0.00") + " m over the ceiling");
    		Check(sb, "Mono fits — his visible head clears the roof", 1.1f - monoVisible >= 0.2f, "the roof is " + (1.1f - monoVisible).ToString("0.00") + " m above his head");
    		float num6 = 0.8999996f;
    		float num7 = ariRadius * 2f;
    		sb.AppendLine();
    		sb.AppendLine("  the mouth is " + num6.ToString("0.00") + " m wide and Ari is " + num7.ToString("0.00") + " m across, so he would fit through it sideways by " + (num6 - num7).ToString("0.00") + " m");
    		Check(sb, "width is wide enough that height is what stops him", num6 - num7 >= 0.15f, num6.ToString("0.00") + " m mouth against a " + num7.ToString("0.00") + " m body — the puzzle must read as 'too tall', not 'too tight'");
    		sb.AppendLine();
    		sb.AppendLine("--- the gate, tested with Ari's own capsule sweep ---");
    		float num8 = 27.8f;
    		float num9 = 33f;
    		Vector3 a = new Vector3(num8, ground, 7f);
    		Vector3 b = new Vector3(num9, ground, 7f);
    		bool flag = Swept(a, b, ariBody, ariRadius, ground, slab.transform);
    		sb.AppendLine("  gate shut: " + (flag ? "BLOCKED by the slab" : "walks straight through"));
    		Check(sb, "the closed gate stops Ari", flag, "a capsule sweep of " + ariBody.ToString("0.00") + " m by " + num7.ToString("0.00") + " m with his soles on the ground, east along the gate's centre line");
    		Vector3 localPosition = slab.transform.localPosition;
    		slab.transform.localPosition = localPosition + Vector3.down * 2.25f;
    		Physics.SyncTransforms();
    		bool flag2 = Swept(a, b, ariBody, ariRadius, ground, slab.transform);
    		sb.AppendLine("  gate open: " + (flag2 ? "STILL BLOCKED" : "walks through"));
    		slab.transform.localPosition = localPosition;
    		Physics.SyncTransforms();
    		Check(sb, "the open gate lets him through", !flag2, "same sweep with the slab " + 2.25f.ToString("0.00") + " m down, " + (flag2 ? "and it STILL blocked his" : "and he walks through"));
    		float num10 = 2.1f - ariBody;
    		float num11 = 1.6000004f - num7;
    		sb.AppendLine();
    		Check(sb, "the opening is taller than he is", num10 >= 0.2f, "2.10 m opening against a " + ariBody.ToString("0.00") + " m body, " + num10.ToString("0.00") + " m to spare");
    		Check(sb, "the opening is wider than he is", num11 >= 0.4f, 1.6000004f.ToString("0.00") + " m against " + num7.ToString("0.00") + " m, " + num11.ToString("0.00") + " m to spare");
    		float num12 = ground + 2.1f;
    		float num13 = num12 - 2.25f;
    		sb.AppendLine();
    		sb.AppendLine("  gate slab top: " + num12.ToString("0.00") + " m shut, " + num13.ToString("0.00") + " m open, ground " + ground.ToString("0.00") + " m");
    		Check(sb, "the open slab is entirely below the ground", num13 < ground - 0.05f, "top of the slab ends at " + num13.ToString("0.00") + " m, ground is " + ground.ToString("0.00") + " m");
    		sb.AppendLine();
    		sb.AppendLine("--- the plate ---");
    		sb.AppendLine("  plinth top " + (ground + 0.3f).ToString("0.00") + " m, Ari's step height " + ariStep.ToString("0.00") + " m");
    		Check(sb, "he can step onto the plinth without jumping", 0.3f <= ariStep + 0.01f, 0.3f.ToString("0.00") + " m against a " + ariStep.ToString("0.00") + " m step");
    		sb.AppendLine("  plate at (" + 28.95f.ToString("0.00") + ", " + 5.7f.ToString("0.00") + "), " + Vector2.Distance(new Vector2(28f, 7f), new Vector2(28.95f, 5.7f)).ToString("0.00") + " m from where he comes out of the gully");
    		Check(sb, "the plate is clear of the gully's walls", ok: true, "the walls end at x 28.00, the plate is at " + 28.95f.ToString("0.00"));
    		sb.AppendLine();
    		sb.AppendLine("--- Mono's errand ---");
    		float num14 = Vector3.Distance(new Vector3(errand.position.x, 0f, errand.position.z), new Vector3(32.55f, 0f, 9.05f));
    		sb.AppendLine("  errand radius " + errandRadius.ToString("0.00") + " m, switch reach " + latching.Reach.ToString("0.00") + " m, errand point to switch " + num14.ToString("0.00") + " m");
    		Check(sb, "the switch's reach is at least Mono's errand radius", latching.Reach >= errandRadius, "he stops " + errandRadius.ToString("0.00") + " m short, so a " + latching.Reach.ToString("0.00") + " m reach " + ((latching.Reach >= errandRadius) ? "reaches him" : "MISSES him"));
    		Check(sb, "the errand point is inside the switch's reach", num14 <= latching.Reach, num14.ToString("0.00") + " m apart with a " + latching.Reach.ToString("0.00") + " m reach");
    		bool ok = errand.position.x > 31.5f;
    		string[] array = new string[9] { "errand at x ", null, null, null, null, null, null, null, null };
    		Vector3 val = errand.position;
    		array[1] = val.x.ToString("0.00");
    		array[2] = ", mouth at x ";
    		array[3] = 29.5f.ToString("0.00");
    		array[4] = " — he can see ";
    		array[5] = 2f.ToString("0.00");
    		array[6] = " m in, he is ";
    		array[7] = (errand.position.x - 29.5f).ToString("0.00");
    		array[8] = " m in";
    		Check(sb, "the errand point is out of Ari's sight down the tunnel", ok, string.Concat(array));
    		sb.AppendLine();
    		sb.AppendLine("--- everything inside the beat's footprint ---");
    		Bounds val2 = new Bounds(new Vector3(31.4f, ground + 1.2f, 7f), new Vector3(5.6f, 3f, 10.4f));
    		Collider[] array2 = Object.FindObjectsByType<Collider>((FindObjectsInactive)1);
    		HashSet<Collider> hashSet = new HashSet<Collider>();
    		Collect(container.transform, hashSet);
    		HashSet<string> hashSet2 = new HashSet<string> { "Ground" };
    		int num15 = 0;
    		List<string> list2 = new List<string>();
    		List<Collider> list3 = new List<Collider>();
    		Collider[] array3 = array2;
    		foreach (Collider val3 in array3)
    		{
    			if (!((Object)(object)val3 == (Object)null) && !val3.isTrigger && !hashSet.Contains(val3) && val2.Intersects(val3.bounds))
    			{
    				string text = PathOf2(((Component)val3).transform);
    				Bounds bounds;
    				if (text.StartsWith("Village_Grey") || text.Contains("Village_Grey") || hashSet2.Contains(((Object)val3).name))
    				{
    					string[] array4 = new string[6]
    					{
    						"  (village) ",
    						((Object)val3).name,
    						" at ",
    						null,
    						null,
    						null
    					};
    					bounds = val3.bounds;
    					val = bounds.center;
    					array4[3] = val.ToString("F2");
    					array4[4] = "  path ";
    					array4[5] = text;
    					list2.Add(string.Concat(array4));
    					list3.Add(val3);
    				}
    				else
    				{
    					num15++;
    					string[] array5 = new string[8]
    					{
    						"  FOREIGN ",
    						((object)val3).GetType().Name,
    						" '",
    						((Object)val3).name,
    						"' at ",
    						null,
    						null,
    						null
    					};
    					bounds = val3.bounds;
    					val = bounds.center;
    					array5[5] = val.ToString("F2");
    					array5[6] = "  path ";
    					array5[7] = text;
    					list2.Add(string.Concat(array5));
    				}
    			}
    		}
    		list2.Sort(StringComparer.Ordinal);
    		foreach (string item in list2)
    		{
    			sb.AppendLine(item);
    		}
    		sb.AppendLine("  " + hashSet.Count + " collider(s) this beat built, " + list2.Count + " village object(s) already here, " + num15 + " unexplained");
    		Check(sb, "nothing unexplained is standing in the beat", num15 == 0, (num15 == 0) ? "every collider here is either ours or the village's own" : (num15 + " collider(s) from neither"));
    		float num16 = float.MaxValue;
    		string text2 = "";
    		foreach (Collider item2 in hashSet)
    		{
    			if ((Object)(object)item2 == (Object)null)
    			{
    				continue;
    			}
    			foreach (Collider item3 in list3)
    			{
    				if (!((Object)(object)item3 == (Object)null) && !(((Object)item3).name == "Ground"))
    				{
    					float num17 = Gap(item2.bounds, item3.bounds);
    					if (num17 < num16)
    					{
    						num16 = num17;
    						text2 = ((Object)item2).name + " vs " + ((Object)item3).name;
    					}
    				}
    			}
    		}
    		sb.AppendLine("  closest approach between this beat and the village: " + ((num16 == float.MaxValue) ? "nothing to compare against" : (num16.ToString("0.00") + " m  (" + text2 + ")")));
    		Check(sb, "the beat and the village do not overlap", num16 > 0f, (num16 == float.MaxValue) ? "no village geometry in range" : (num16.ToString("0.00") + " m between " + text2 + ((num16 > 0f) ? " — they do not touch" : " — THEY TOUCH")));
    		sb.AppendLine();
    		sb.AppendLine("--- wiring ---");
    		Check(sb, "the gate has a slab with a collider", (Object)(object)gate.SlabCollider != (Object)null, ((Object)(object)gate.SlabCollider == (Object)null) ? "no collider, so it would not block" : ("BoxCollider on " + ((Object)gate.Slab).name));
    		LatchingSwitch latchingSwitch = Src<LatchingSwitch>((Object)(object)gate, "source");
    		StringBuilder sb2 = sb;
    		bool ok2 = (Object)(object)latchingSwitch == (Object)(object)latching;
    		string detail;
    		if ((Object)(object)latchingSwitch == (Object)null)
    		{
    			detail = "source is null";
    		}
    		else
    		{
    			detail = (((Object)(object)latchingSwitch == (Object)(object)latching) ? ("points at " + ((Object)latching).name) : ("points at " + ((Object)latchingSwitch).name + ", not the switch in this tunnel"));
    		}
    		Check(sb2, "the gate knows which switch opens it", ok2, detail);
    		Check(sb, "the switch knows which lever powers it", (Object)(object)Src<HoldLever>((Object)(object)latching, "lever") == (Object)(object)lever, "lever is " + ((Object)lever).name);
    		Check(sb, "the director knows all four parts", (Object)(object)Src<BeatGate>((Object)(object)dir, "gate") == (Object)(object)gate && (Object)(object)Src<HoldLever>((Object)(object)dir, "lever") == (Object)(object)lever && (Object)(object)Src<LatchingSwitch>((Object)(object)dir, "latchingSwitch") == (Object)(object)latching && (Object)(object)Src<Transform>((Object)(object)dir, "errandPoint") != (Object)null, "gate, lever, switch and errand point all assigned");
    		bool ok3 = (Object)(object)Src<Transform>((Object)(object)dir, "errandPoint") == (Object)(object)errand;
    		string name = ((Object)errand).name;
    		val = errand.position;
    		Check(sb, "the errand point is inside the crawlspace", ok3, name + " at x " + val.x.ToString("0.00"));
    	}

    	private static MonoHintLines BuildLines(StringBuilder sb)
    	{
    		MonoHintLines asset = AssetDatabase.LoadAssetAtPath<MonoHintLines>("Assets/Painterly/MonoHintLines.asset");
    		bool flag = false;
    		if ((Object)(object)asset == (Object)null)
    		{
    			asset = ScriptableObject.CreateInstance<MonoHintLines>();
    			flag = true;
    		}
    		asset.lines.RemoveAll((MonoHintLines.Line l) => l?.id.StartsWith("beat4.") ?? false);
    		sb.AppendLine("  rewrote " + Beat4LineCount(asset) + " Beat 4 line(s)");
    		Beat("beat4.hold", "That is a low place. I will fit; you will not. Stay on the plate — I can feel it wanting to be held down.");
    		Beat("beat4.open", "It let go. It is open. Go on through, I will come round by the road. There is a road. There is always a road.");
    		Beat("beat4.after", "Two doors and only one of us could open each one. That is going to be a habit, I think. I do not mind it.");
    		if (flag)
    		{
    			AssetDatabase.CreateAsset((Object)(object)asset, "Assets/Painterly/MonoHintLines.asset");
    			sb.AppendLine("  created Assets/Painterly/MonoHintLines.asset");
    		}
    		else
    		{
    			EditorUtility.SetDirty((Object)(object)asset);
    			AssetDatabase.SaveAssets();
    		}
    		sb.AppendLine("  " + asset.lines.Count + " line(s) in the asset now, " + Beat4LineCount(asset) + " of them Beat 4's");
    		return asset;
    		void Beat(string id, string text)
    		{
    			asset.lines.Add(new MonoHintLines.Line
    			{
    				id = id,
    				text = text,
    				kind = MonoHintLines.LineKind.Beat
    			});
    		}
    	}

    	private static int Beat4LineCount(MonoHintLines asset)
    	{
    		return asset.lines.FindAll((MonoHintLines.Line l) => l?.id.StartsWith("beat4.") ?? false).Count;
    	}

    	private static void Clear(GameObject container)
    	{
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0020: Expected Obj, but got Unknown
    		List<GameObject> list = new List<GameObject>();
    		foreach (Transform item in container.transform)
    		{
    			Transform val = item;
    			list.Add(((Component)val).gameObject);
    		}
    		foreach (GameObject item2 in list)
    		{
    			Object.DestroyImmediate((Object)(object)item2);
    		}
    	}

    	private static GameObject Box(Transform parent, string name, float x, float y, float z, float sx, float sy, float sz, Material mat)
    	{
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
    		GameObject val = GameObject.CreatePrimitive((PrimitiveType)3);
    		((Object)val).name = name;
    		val.transform.SetParent(parent, false);
    		val.transform.position = new Vector3(x, y, z);
    		val.transform.localScale = new Vector3(sx, sy, sz);
    		Renderer component = val.GetComponent<Renderer>();
    		if ((Object)(object)component != (Object)null && (Object)(object)mat != (Object)null)
    		{
    			component.sharedMaterial = mat;
    		}
    		return val;
    	}

    	private static void Collect(Transform t, HashSet<Collider> into)
    	{
    		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0032: Expected Obj, but got Unknown
    		Collider component = ((Component)t).GetComponent<Collider>();
    		if ((Object)(object)component != (Object)null)
    		{
    			into.Add(component);
    		}
    		foreach (Transform item in t)
    		{
    			Collect(item, into);
    		}
    	}

    	private static string PathOf2(Transform t)
    	{
    		List<string> list = new List<string>();
    		while ((Object)(object)t != (Object)null)
    		{
    			list.Insert(0, ((Object)t).name);
    			t = t.parent;
    		}
    		return string.Join(" / ", list.ToArray());
    	}

    	private static Material Load(string path)
    	{
    		return AssetDatabase.LoadAssetAtPath<Material>(path);
    	}

    	private static T Src<T>(Object target, string field) where T : class
    	{
    		if (target == (Object)null)
    		{
    			return null;
    		}
    		FieldInfo field2 = ((object)target).GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    		if (!(field2 == null))
    		{
    			return field2.GetValue(target) as T;
    		}
    		return null;
    	}

    	private static void Set(Object target, string field, object value)
    	{
    		FieldInfo field2 = ((object)target).GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    		if (field2 == null)
    		{
    			Debug.LogError((object)("[Echoes] Beat 4: no field '" + field + "' on " + ((object)target).GetType().Name));
    			_fail++;
    		}
    		else
    		{
    			field2.SetValue(target, value);
    		}
    	}

    	private static float AriBody(StringBuilder sb, string who, AriMover mover, string prop)
    	{
    		Type type = ((object)mover).GetType();
    		PropertyInfo property = type.GetProperty(prop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    		if (property != null)
    		{
    			return Convert.ToSingle(property.GetValue(mover, null));
    		}
    		string text = Behind(prop);
    		FieldInfo fieldInfo = ((text == null) ? null : type.GetField(text, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
    		if (fieldInfo != null)
    		{
    			Warn(sb, who + "." + prop + " came from the field, not the property", "the project assembly looks stale — run call-recompile.json");
    			return Convert.ToSingle(fieldInfo.GetValue(mover));
    		}
    		Fail(sb, who + " has neither " + prop + " nor a field behind it — AriMover.cs must have been renamed");
    		return 0f;
    	}

    	private static string Behind(string prop)
    	{
    		return prop switch
    		{
    			"bodyHeight" => "bodyHeight", 
    			"bodyRadius" => "bodyRadius", 
    			"stepHeight" => "stepHeight", 
    			_ => null, 
    		};
    	}

    	private static float MonoBody(StringBuilder sb, GameObject mono)
    	{
    		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
    		CapsuleCollider component = mono.GetComponent<CapsuleCollider>();
    		if ((Object)(object)component == (Object)null)
    		{
    			Fail(sb, "Mono has no CapsuleCollider");
    			return 0f;
    		}
    		float num = component.height * Mathf.Abs(mono.transform.lossyScale.y);
    		float y = mono.transform.TransformPoint(component.center).y;
    		float y2 = mono.transform.TransformPoint(new Vector3(component.center.x, component.center.y - component.height * 0.5f, component.center.z)).y;
    		sb.AppendLine("  Mono capsule local " + component.height.ToString("0.00") + " m at scale " + mono.transform.lossyScale.y.ToString("0.00") + " -> " + num.ToString("0.00") + " m, centre y " + y.ToString("0.00") + ", bottom y " + y2.ToString("0.00"));
    		if (Mathf.Abs(y - y2 - num * 0.5f) > 0.05f)
    		{
    			Fail(sb, "Mono's capsule centre is not half a body above its own bottom — standing height would be wrong by " + (y - y2 - num * 0.5f).ToString("0.00") + " m");
    		}
    		return num;
    	}

    	private static float MonoVisible(StringBuilder sb, GameObject mono)
    	{
    		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
    		Renderer[] componentsInChildren = mono.GetComponentsInChildren<Renderer>(true);
    		if (componentsInChildren.Length == 0)
    		{
    			Fail(sb, "Mono has no Renderer at all, active or not");
    			return 0f;
    		}
    		float num = float.MaxValue;
    		float num2 = float.MinValue;
    		int num3 = 0;
    		Renderer[] array = componentsInChildren;
    		foreach (Renderer val in array)
    		{
    			if (!((Object)(object)val == (Object)null))
    			{
    				Bounds bounds = val.bounds;
    				if (bounds.min.y < num)
    				{
    					num = bounds.min.y;
    				}
    				if (bounds.max.y > num2)
    				{
    					num2 = bounds.max.y;
    				}
    				num3++;
    			}
    		}
    		if (num3 == 0 || num2 <= num)
    		{
    			Fail(sb, "Mono has " + componentsInChildren.Length + " renderers but none of them reported usable bounds");
    			return 0f;
    		}
    		sb.AppendLine("  Mono " + num3 + " renderer(s), world y " + num.ToString("0.00") + " to " + num2.ToString("0.00") + " m — visible " + num2.ToString("0.00") + " m");
    		return num2;
    	}

    	private static float ErrandRadius(StringBuilder sb, MonoCompanion mono)
    	{
    		PropertyInfo property = ((object)mono).GetType().GetProperty("ErrandRadius", BindingFlags.Instance | BindingFlags.Public);
    		if (property == null)
    		{
    			Fail(sb, "MonoCompanion has no ErrandRadius property — the project assembly has not been rebuilt");
    			return 0.7f;
    		}
    		float result = Convert.ToSingle(property.GetValue(mono, null));
    		sb.AppendLine("  Mono errand radius " + result.ToString("0.00") + " m");
    		return result;
    	}

    	private static float GroundAt(Vector3 at)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
    		RaycastHit[] array = Physics.RaycastAll(at + Vector3.up * 30f, Vector3.down, 60f, -1, (QueryTriggerInteraction)1);
    		if (array == null || array.Length == 0)
    		{
    			return 0f;
    		}
    		float num = float.MaxValue;
    		for (int i = 0; i < array.Length; i++)
    		{
    			if (array[i].point.y < num)
    			{
    				num = array[i].point.y;
    			}
    		}
    		if (num != float.MaxValue)
    		{
    			return num;
    		}
    		return 0f;
    	}

    	private static float Gap(Bounds a, Bounds b)
    	{
    		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
    		float num = Mathf.Max(0f, Mathf.Max(a.min.x - b.max.x, b.min.x - a.max.x));
    		float num2 = Mathf.Max(0f, Mathf.Max(a.min.y - b.max.y, b.min.y - a.max.y));
    		float num3 = Mathf.Max(0f, Mathf.Max(a.min.z - b.max.z, b.min.z - a.max.z));
    		return Mathf.Sqrt(num * num + num2 * num2 + num3 * num3);
    	}

    	private static float Headroom(Vector3 at)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
    		RaycastHit[] array = Physics.RaycastAll(at + Vector3.up * 0.15f, Vector3.up, 6f, -1, (QueryTriggerInteraction)1);
    		if (array == null || array.Length == 0)
    		{
    			return 6f;
    		}
    		float num = float.MaxValue;
    		for (int i = 0; i < array.Length; i++)
    		{
    			if (array[i].point.y < num)
    			{
    				num = array[i].point.y;
    			}
    		}
    		if (num != float.MaxValue)
    		{
    			return num - at.y;
    		}
    		return 6f;
    	}

    	private static bool Swept(Vector3 a, Vector3 b, float height, float radius, float floorY, Transform only)
    	{
    		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
    		float num = Mathf.Max(0f, (height - radius * 2f) * 0.5f);
    		float num2 = floorY + radius + num;
    		Vector3 val = new Vector3(a.x, num2, a.z);
    		Vector3 val2 = new Vector3(b.x, num2, b.z);
    		RaycastHit[] array = new RaycastHit[16];
    		int num3 = Physics.CapsuleCastNonAlloc(val, val2, radius, Vector3.right, array, Vector3.Distance(val, val2), -1, (QueryTriggerInteraction)1);
    		for (int i = 0; i < num3; i++)
    		{
    			if (!((Object)(object)array[i].collider == (Object)null) && (!((Object)(object)only != (Object)null) || !((Object)(object)((Component)array[i].collider).transform != (Object)(object)only)))
    			{
    				return true;
    			}
    		}
    		return false;
    	}

    	private static void Check(StringBuilder sb, string what, bool ok, string detail)
    	{
    		if (ok)
    		{
    			_pass++;
    			sb.AppendLine("  ok    " + what + " — " + detail);
    		}
    		else
    		{
    			_fail++;
    			sb.AppendLine("  FAIL  " + what + " — " + detail);
    		}
    	}

    	private static void Warn(StringBuilder sb, string what, string detail)
    	{
    		_warn++;
    		sb.AppendLine("  warn  " + what + " — " + detail);
    	}

    	private static void Fail(StringBuilder sb, string what)
    	{
    		_fail++;
    		sb.AppendLine("  FAIL  " + what);
    		if (_reason == null)
    		{
    			_reason = what;
    		}
    	}

    	private static GameObject Named(string name)
    	{
    		Transform[] array = Object.FindObjectsByType<Transform>((FindObjectsInactive)1);
    		for (int i = 0; i < array.Length; i++)
    		{
    			if ((Object)(object)array[i] != (Object)null && ((Object)array[i]).name == name)
    			{
    				return ((Component)array[i]).gameObject;
    			}
    		}
    		return null;
    	}

    	private static string SceneCount()
    	{
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
    		List<string> list = new List<string>();
    		for (int i = 0; i < SceneManager.sceneCount; i++)
    		{
    			Scene sceneAt = SceneManager.GetSceneAt(i);
    			list.Add(sceneAt.name + (sceneAt.isLoaded ? "" : " [not loaded]"));
    		}
    		return list.Count + " (" + string.Join(", ", list.ToArray()) + ")";
    	}

    	private static GameObject Container(StringBuilder sb)
    	{
    		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0021: Expected Obj, but got Unknown
    		GameObject val = GameObject.Find("L1_Beat4");
    		if ((Object)(object)val != (Object)null)
    		{
    			return val;
    		}
    		val = new GameObject("L1_Beat4");
    		sb.AppendLine("created container 'L1_Beat4'");
    		return val;
    	}

    	private static Vector3 Flat(Vector3 v)
    	{
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		v.y = 0f;
    		return v;
    	}

    	private static string PathOf(string dir, string file)
    	{
    		return Path.Combine(dir, file);
    	}
    }
}