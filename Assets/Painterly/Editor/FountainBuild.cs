using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class FountainBuild
    {
    	private const string OutDir = "Assets/Painterly/Generated/Fountain";

    	private const string StoneMatPath = "Assets/Painterly/Materials/RockTrim.mat";

    	private const string MetalMatPath = "Assets/Painterly/Materials/MetalOrnament.mat";

    	private const string WaterMatPath = "Assets/Painterly/Materials/FountainWater.mat";

    	private const string Report = "Temp/fountain.txt";

    	private const float UvPerMetre = 0.5f;

    	private const float BasinFloorY = 0.1f;

    	private const float WaterY = 0.46f;

    	private const float PlinthTopY = 0.6f;

    	private static Vector2[] BasinWallProfile()
    	{
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
    		return new Vector2[10]
    		{
    			new Vector2(1.72f, 0f),
    			new Vector2(1.76f, 0.62f),
    			new Vector2(1.84f, 0.7f),
    			new Vector2(1.98f, 0.72f),
    			new Vector2(2.08f, 0.86f),
    			new Vector2(2.14f, 0.79f),
    			new Vector2(2.12f, 0.7f),
    			new Vector2(2.14f, 0.1f),
    			new Vector2(2.24f, 0f),
    			new Vector2(1.72f, 0f)
    		};
    	}

    	private static Vector2[] PlinthProfile()
    	{
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
    		return new Vector2[9]
    		{
    			new Vector2(0f, 0f),
    			new Vector2(0.66f, 0f),
    			new Vector2(0.66f, 0.15f),
    			new Vector2(0.55f, 0.18f),
    			new Vector2(0.55f, 0.32f),
    			new Vector2(0.45f, 0.35f),
    			new Vector2(0.45f, 0.46f),
    			new Vector2(0.36f, 0.46f),
    			new Vector2(0f, 0.46f)
    		};
    	}

    	private static Vector2[] StatueStumpProfile()
    	{
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
    		return new Vector2[10]
    		{
    			new Vector2(0f, 0f),
    			new Vector2(0.44f, 0f),
    			new Vector2(0.4f, 0.1f),
    			new Vector2(0.31f, 0.28f),
    			new Vector2(0.25f, 0.44f),
    			new Vector2(0.23f, 0.53f),
    			new Vector2(0.19f, 0.49f),
    			new Vector2(0.24f, 0.58f),
    			new Vector2(0.2f, 0.55f),
    			new Vector2(0f, 0.57f)
    		};
    	}

    	private static Vector2[] StatueTorsoProfile()
    	{
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
    		return new Vector2[11]
    		{
    			new Vector2(0f, 0f),
    			new Vector2(0.21f, 0.02f),
    			new Vector2(0.19f, 0.07f),
    			new Vector2(0.24f, 0.1f),
    			new Vector2(0.2f, 0.14f),
    			new Vector2(0.23f, 0.2f),
    			new Vector2(0.26f, 0.3f),
    			new Vector2(0.25f, 0.42f),
    			new Vector2(0.16f, 0.5f),
    			new Vector2(0.1f, 0.52f),
    			new Vector2(0f, 0.52f)
    		};
    	}

    	private static Vector2[] LanternPostProfile(float height)
    	{
    		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
    		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
    		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
    		return new Vector2[13]
    		{
    			new Vector2(0f, 0f),
    			new Vector2(0.17f, 0f),
    			new Vector2(0.17f, 0.06f),
    			new Vector2(0.11f, 0.12f),
    			new Vector2(0.09f, 0.18f),
    			new Vector2(0.14f, 0.26f),
    			new Vector2(0.08f, 0.33f),
    			new Vector2(0.08f, height - 0.26f),
    			new Vector2(0.13f, height - 0.18f),
    			new Vector2(0.13f, height - 0.08f),
    			new Vector2(0.19f, height - 0.04f),
    			new Vector2(0.19f, height),
    			new Vector2(0f, height)
    		};
    	}

    	private static Vector2[] LanternCupProfile(float radius, float depth)
    	{
    		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
    		return new Vector2[6]
    		{
    			new Vector2(0f, 0f),
    			new Vector2(radius * 0.55f, 0.01f),
    			new Vector2(radius, depth),
    			new Vector2(radius * 0.88f, depth),
    			new Vector2(radius * 0.45f, depth * 0.28f),
    			new Vector2(0f, depth * 0.22f)
    		};
    	}

    	[MenuItem("Tools/Echoes/Build Fountain", priority = 75)]
    	public static void Run()
    	{
    		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0203: Expected Obj, but got Unknown
    		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
    		//IL_032d: Expected Obj, but got Unknown
    		//IL_0357: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03a7: Expected Obj, but got Unknown
    		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03c7: Expected Obj, but got Unknown
    		//IL_03f1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0411: Unknown result type (might be due to invalid IL or missing references)
    		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0461: Expected Obj, but got Unknown
    		//IL_048b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04ab: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04fb: Expected Obj, but got Unknown
    		//IL_0525: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0545: Unknown result type (might be due to invalid IL or missing references)
    		//IL_057a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0581: Expected Obj, but got Unknown
    		//IL_05ab: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05cb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0603: Unknown result type (might be due to invalid IL or missing references)
    		//IL_060a: Expected Obj, but got Unknown
    		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0260: Expected Obj, but got Unknown
    		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0844: Unknown result type (might be due to invalid IL or missing references)
    		//IL_084b: Expected Obj, but got Unknown
    		//IL_0876: Unknown result type (might be due to invalid IL or missing references)
    		//IL_088a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_089e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_08ba: Unknown result type (might be due to invalid IL or missing references)
    		//IL_08ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_08e2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_08fe: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0912: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0926: Unknown result type (might be due to invalid IL or missing references)
    		//IL_066a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0671: Expected Obj, but got Unknown
    		//IL_06ab: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0953: Unknown result type (might be due to invalid IL or missing references)
    		//IL_095a: Expected Obj, but got Unknown
    		//IL_0977: Unknown result type (might be due to invalid IL or missing references)
    		//IL_098a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_098f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_099b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_09a7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_09b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_09f1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_09f8: Expected Obj, but got Unknown
    		//IL_0a22: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a64: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a69: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0a7e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0abd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0ac2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_06e4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_077f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0786: Expected Obj, but got Unknown
    		//IL_07c0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_07e0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0b39: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0b2b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0bdd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0b3e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c80: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c85: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c87: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0c8f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0caa: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0caf: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0cb4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0ccd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0db0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0dc1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0dd2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0df4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0e11: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] fountain build");
    		stringBuilder.AppendLine();
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			stringBuilder.AppendLine("STOPPED: exit play mode first (Ctrl+P). Nothing was changed.");
    			Finish(stringBuilder);
    			return;
    		}
    		Material val = AssetDatabase.LoadAssetAtPath<Material>("Assets/Painterly/Materials/RockTrim.mat");
    		Material val2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Painterly/Materials/MetalOrnament.mat");
    		if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
    		{
    			stringBuilder.AppendLine($"FATAL: missing material. stone={(Object)(object)val != (Object)null} metal={(Object)(object)val2 != (Object)null}");
    			Finish(stringBuilder);
    			return;
    		}
    		Material val3 = EnsureWaterMaterial(stringBuilder);
    		SquarePlacement.Scan scan = SquarePlacement.MeasureSquare();
    		if (!scan.Ok)
    		{
    			stringBuilder.AppendLine("FATAL: cannot measure the market square: " + scan.Why);
    			Finish(stringBuilder);
    			return;
    		}
    		GameObject val4 = SquarePlacement.FindSquare();
    		stringBuilder.AppendLine("square      : " + SquarePlacement.PathOf(val4.transform));
    		stringBuilder.AppendLine("ground      : y=" + scan.GroundY.ToString("F3") + ", extent " + ((object)scan.Area.size/*cast due to constrained. prefix*/).ToString() + " at " + ((object)scan.Area.center/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine("open ground : " + scan.Samples + " samples, clearance max " + scan.BestClearance.ToString("F2") + " m, median " + MedianClearance(scan).ToString("F2") + " m");
    		GameObject val5 = GameObject.Find("Fountain");
    		if ((Object)(object)val5 != (Object)null)
    		{
    			Object.DestroyImmediate((Object)(object)val5);
    		}
    		GameObject val6 = new GameObject("Fountain");
    		val6.transform.position = Vector3.zero;
    		int num = 0;
    		int meshCount = 0;
    		int colliderCount = 0;
    		Vector2[] profile = BasinWallProfile();
    		for (int i = 0; i < 12; i++)
    		{
    			float num2 = (float)i * 30f;
    			if (i != 3 && i != 9)
    			{
    				GameObject val7 = new GameObject($"Basin_{i:00}");
    				val7.transform.SetParent(val6.transform, false);
    				val7.transform.localRotation = Quaternion.Euler(0f, num2 + 15f, 0f);
    				float yScale = 1f;
    				float rOffset = 0f;
    				float num3 = 0f;
    				switch (i)
    				{
    				case 2:
    					rOffset = 0.05f;
    					num3 = 1.4f;
    					break;
    				case 7:
    					yScale = 0.52f;
    					rOffset = -0.11f;
    					num3 = -3.5f;
    					break;
    				}
    				Mesh mesh = Revolve(profile, -15f, 15f, 7, closedSection: true, yScale, rOffset, (float)Math.PI / 180f * num3);
    				num += AddMesh(val7, mesh, val, stringBuilder, ref meshCount, ref colliderCount);
    			}
    		}
    		GameObject val8 = new GameObject("Plinth");
    		val8.transform.SetParent(val6.transform, false);
    		val8.transform.localPosition = new Vector3(0f, 0.1f, 0f);
    		num += AddMesh(val8, Revolve(PlinthProfile(), 0f, 360f, 20, closedSection: false, 1f, 0f, 0f), val, stringBuilder, ref meshCount, ref colliderCount);
    		GameObject val9 = new GameObject("Statue");
    		val9.transform.SetParent(val6.transform, false);
    		GameObject val10 = new GameObject("Statue_Stump");
    		val10.transform.SetParent(val9.transform, false);
    		val10.transform.localPosition = new Vector3(0.02f, 0.6f, -0.03f);
    		val10.transform.localRotation = Quaternion.Euler(0.6f, 24f, -1.1f);
    		num += AddMesh(val10, Revolve(StatueStumpProfile(), 0f, 360f, 16, closedSection: false, 1f, 0f, 0f), val, stringBuilder, ref meshCount, ref colliderCount);
    		GameObject val11 = new GameObject("Statue_Torso");
    		val11.transform.SetParent(val9.transform, false);
    		val11.transform.localPosition = new Vector3(0.74f, 0.33f, 0.46f);
    		val11.transform.localRotation = Quaternion.Euler(-74f, 38f, 21f);
    		num += AddMesh(val11, Revolve(StatueTorsoProfile(), 0f, 360f, 16, closedSection: false, 1f, 0f, 0f), val, stringBuilder, ref meshCount, ref colliderCount);
    		GameObject val12 = new GameObject("Statue_Head");
    		val12.transform.SetParent(val9.transform, false);
    		val12.transform.localPosition = new Vector3(1.16f, 0.25f, 0.86f);
    		val12.transform.localRotation = Quaternion.Euler(28f, 150f, 44f);
    		Mesh mesh2 = Sphere(0.165f, 16, 10);
    		num += AddMesh(val12, mesh2, val, stringBuilder, ref meshCount, ref colliderCount);
    		GameObject val13 = new GameObject("Statue_Arm");
    		val13.transform.SetParent(val9.transform, false);
    		val13.transform.localPosition = new Vector3(2.05f, 0.07f, 1.42f);
    		val13.transform.localRotation = Quaternion.Euler(0f, 66f, 90f);
    		num += AddMesh(val13, Box(0.44f, 0.12f, 0.12f, 1), val, stringBuilder, ref meshCount, ref colliderCount);
    		GameObject val14 = new GameObject("Lanterns");
    		val14.transform.SetParent(val6.transform, false);
    		float[] array = new float[3] { 40f, 160f, 280f };
    		for (int j = 0; j < array.Length; j++)
    		{
    			float num4 = array[j] * ((float)Math.PI / 180f);
    			float height = ((j == 1) ? 0.58f : 1.16f);
    			GameObject val15 = new GameObject($"Lantern_{j:00}");
    			val15.transform.SetParent(val14.transform, false);
    			val15.transform.localPosition = new Vector3(Mathf.Cos(num4) * 2.78f, 0f, Mathf.Sin(num4) * 2.78f);
    			val15.transform.localRotation = Quaternion.Euler((j == 1) ? 9f : 0f, 0f - array[j], (j == 1) ? (-13f) : 0f);
    			num += AddMesh(val15, Revolve(LanternPostProfile(height), 0f, 360f, 12, closedSection: false, 1f, 0f, 0f), val2, stringBuilder, ref meshCount, ref colliderCount);
    			num += AddMesh(val15, Revolve(LanternCupProfile(0.2f, 0.17f), 0f, 360f, 12, closedSection: false, 1f, 0f, 0f), val2, stringBuilder, ref meshCount, ref colliderCount, "Cup");
    			if (j == 1)
    			{
    				GameObject val16 = new GameObject("Lantern_01_CupFallen");
    				val16.transform.SetParent(val14.transform, false);
    				val16.transform.localPosition = new Vector3(Mathf.Cos(num4) * 3.24f, 0.02f, Mathf.Sin(num4) * 3.24f);
    				val16.transform.localRotation = Quaternion.Euler(84f, 12f, 0f);
    				num += AddMesh(val16, Revolve(LanternCupProfile(0.2f, 0.17f), 0f, 360f, 12, closedSection: false, 1f, 0f, 0f), val2, stringBuilder, ref meshCount, ref colliderCount);
    			}
    		}
    		GameObject val17 = new GameObject("Debris");
    		val17.transform.SetParent(val6.transform, false);
    		var array2 = new[]
    		{
    			new
    			{
    				p = new Vector3(2.62f, 0.11f, 0.55f),
    				r = new Vector3(0f, 34f, 22f),
    				s = new Vector3(0.62f, 0.34f, 0.3f)
    			},
    			new
    			{
    				p = new Vector3(-2.48f, 0.09f, -0.92f),
    				r = new Vector3(0f, -58f, -15f),
    				s = new Vector3(0.48f, 0.26f, 0.34f)
    			},
    			new
    			{
    				p = new Vector3(0.35f, 0.08f, 2.71f),
    				r = new Vector3(14f, 12f, 0f),
    				s = new Vector3(0.7f, 0.3f, 0.28f)
    			}
    		};
    		for (int k = 0; k < array2.Length; k++)
    		{
    			var anon = array2[k];
    			GameObject val18 = new GameObject($"Debris_{k:00}");
    			val18.transform.SetParent(val17.transform, false);
    			val18.transform.localPosition = anon.p;
    			val18.transform.localRotation = Quaternion.Euler(anon.r);
    			Mesh mesh3 = Box(anon.s.x, anon.s.y, anon.s.z, 1);
    			num += AddMesh(val18, mesh3, val, stringBuilder, ref meshCount, ref colliderCount);
    		}
    		GameObject val19 = new GameObject("Water");
    		val19.transform.SetParent(val6.transform, false);
    		val19.transform.localPosition = new Vector3(0f, 0.46f, 0f);
    		if ((Object)(object)val3 != (Object)null)
    		{
    			Mesh mesh4 = Disc(1.7f, 24);
    			num += AddMesh(val19, mesh4, val3, stringBuilder, ref meshCount, ref colliderCount, null, withCollider: false);
    		}
    		ColorRestoreTarget colorRestoreTarget = val6.AddComponent<ColorRestoreTarget>();
    		SerializedObject val20 = new SerializedObject((Object)(object)colorRestoreTarget);
    		val20.FindProperty("startRestore").floatValue = 0f;
    		val20.FindProperty("duration").floatValue = 1.6f;
    		val20.ApplyModifiedPropertiesWithoutUndo();
    		colorRestoreTarget.SetRestoreImmediate(0f);
    		val6.isStatic = true;
    		float num5 = FootprintRadius(val6);
    		float num6 = 1f;
    		Vector3 point = Vector3.zero;
    		float clearance = 0f;
    		string detail = "";
    		while (num6 >= 0.39990002f && !SquarePlacement.TryFind(scan, num5 * num6 + 0.15f, out point, out clearance, out detail))
    		{
    			num6 -= 0.05f;
    		}
    		if (num6 < 0.39990002f)
    		{
    			num6 = 0.4f;
    			point = ((scan.Best != null) ? scan.Best.Point : scan.Area.center);
    			clearance = scan.BestClearance;
    			detail = "NOTHING FITS: placed at the roomiest spot anyway";
    		}
    		if (num6 < 0.999f)
    		{
    			ScaleBuilt(val6, num6);
    			stringBuilder.AppendLine("resized     : x" + num6.ToString("F2") + " (built radius " + num5.ToString("F2") + " m -> " + (num5 * num6).ToString("F2") + " m)");
    		}
    		else
    		{
    			stringBuilder.AppendLine("resized     : x1.00 (fits at the size it was authored)");
    		}
    		val6.transform.position = point;
    		stringBuilder.AppendLine("placement   : " + ((object)point/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine("clearance   : " + clearance.ToString("F2") + " m available");
    		stringBuilder.AppendLine("why         : " + detail);
    		EnsureFolder("Assets/Painterly/Generated/Fountain");
    		int num7 = SaveMeshAssets(val6, stringBuilder);
    		EditorUtility.SetDirty((Object)(object)val6);
    		Transform[] componentsInChildren = val6.GetComponentsInChildren<Transform>(true);
    		for (int l = 0; l < componentsInChildren.Length; l++)
    		{
    			EditorUtility.SetDirty((Object)(object)((Component)componentsInChildren[l]).gameObject);
    		}
    		Scene scene = val6.scene;
    		EditorSceneManager.MarkSceneDirty(scene);
    		EditorSceneManager.SaveScene(scene);
    		Renderer[] componentsInChildren2 = val6.GetComponentsInChildren<Renderer>(true);
    		Bounds val21 = new Bounds(val6.transform.position, Vector3.zero);
    		Renderer[] array3 = componentsInChildren2;
    		foreach (Renderer val22 in array3)
    		{
    			val21.Encapsulate(val22.bounds);
    		}
    		int num8 = 0;
    		MeshFilter[] componentsInChildren3 = val6.GetComponentsInChildren<MeshFilter>(true);
    		foreach (MeshFilter val23 in componentsInChildren3)
    		{
    			if ((Object)(object)val23.sharedMesh != (Object)null)
    			{
    				num8 += val23.sharedMesh.triangles.Length / 3;
    			}
    		}
    		stringBuilder.AppendLine("root        : " + PathOf(val6.transform));
    		stringBuilder.AppendLine($"meshes      : {meshCount}   colliders: {colliderCount}");
    		stringBuilder.AppendLine(string.Format("mesh assets : {0} written to {1}", num7, "Assets/Painterly/Generated/Fountain"));
    		stringBuilder.AppendLine($"triangles   : {num8:N0}  (builder counted {num:N0})");
    		stringBuilder.AppendLine($"bounds size : {val21.size.x:F2} x {val21.size.y:F2} x {val21.size.z:F2} m");
    		stringBuilder.AppendLine($"bounds min  : {val21.min}");
    		stringBuilder.AppendLine($"bounds max  : {val21.max}");
    		stringBuilder.AppendLine($"renderers   : {componentsInChildren2.Length}");
    		stringBuilder.AppendLine($"target      : {((object)colorRestoreTarget).GetType().Name} restore={colorRestoreTarget.Restore:F2}");
    		stringBuilder.AppendLine("scene saved : " + scene.path);
    		Finish(stringBuilder);
    	}

    	private static Mesh Revolve(Vector2[] profile, float a0Deg, float a1Deg, int seg, bool closedSection, float yScale, float rOffset, float tiltRad)
    	{
    		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ff: Expected Obj, but got Unknown
    		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
    		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0311: Expected Obj, but got Unknown
    		//IL_0312: Expected Obj, but got Unknown
    		int num = profile.Length;
    		int num2 = seg + 1;
    		List<Vector3> list = new List<Vector3>(num2 * num);
    		List<Vector2> list2 = new List<Vector2>(num2 * num);
    		List<int> list3 = new List<int>(num2 * num * 6);
    		float[] array = new float[num];
    		for (int i = 1; i < num; i++)
    		{
    			array[i] = array[i - 1] + Vector2.Distance(profile[i - 1], profile[i]);
    		}
    		float num3 = 0f;
    		for (int j = 0; j < num; j++)
    		{
    			num3 += profile[j].x;
    		}
    		num3 /= (float)Mathf.Max(1, num);
    		for (int k = 0; k < num2; k++)
    		{
    			float num4 = Mathf.Lerp(a0Deg, a1Deg, (seg <= 0) ? 0f : ((float)k / (float)seg)) * ((float)Math.PI / 180f);
    			float num5 = Mathf.Cos(num4);
    			float num6 = Mathf.Sin(num4);
    			for (int l = 0; l < num; l++)
    			{
    				float num7 = profile[l].x + rOffset;
    				float num8 = profile[l].y * yScale;
    				float num9 = num8 * Mathf.Cos(tiltRad);
    				float num10 = num7 + num8 * Mathf.Sin(tiltRad);
    				list.Add(new Vector3(num5 * num10, num9, num6 * num10));
    				list2.Add(new Vector2(array[l] * 0.5f, num4 * num3 * 0.5f));
    			}
    		}
    		int num11 = (closedSection ? num : (num - 1));
    		for (int m = 0; m < seg; m++)
    		{
    			for (int n = 0; n < num11; n++)
    			{
    				int num12 = (n + 1) % num;
    				int item = m * num + n;
    				int item2 = m * num + num12;
    				int item3 = (m + 1) * num + n;
    				int item4 = (m + 1) * num + num12;
    				bool flag = profile[n].x <= 1E-05f && rOffset <= 1E-05f;
    				bool flag2 = profile[num12].x <= 1E-05f && rOffset <= 1E-05f;
    				if (!(flag & flag2))
    				{
    					if (flag)
    					{
    						list3.Add(item);
    						list3.Add(item4);
    						list3.Add(item2);
    						continue;
    					}
    					if (flag2)
    					{
    						list3.Add(item);
    						list3.Add(item4);
    						list3.Add(item3);
    						continue;
    					}
    					list3.Add(item);
    					list3.Add(item4);
    					list3.Add(item3);
    					list3.Add(item);
    					list3.Add(item2);
    					list3.Add(item4);
    				}
    			}
    		}
    		if (!(Mathf.Abs(a1Deg - a0Deg) >= 359.9f))
    		{
    			AddCap(list, list2, list3, profile, 0f, yScale, rOffset, tiltRad, atStart: true);
    			AddCap(list, list2, list3, profile, a1Deg, yScale, rOffset, tiltRad, atStart: false);
    		}
    		Mesh val = new Mesh
    		{
    			name = "FountainPart"
    		};
    		val.SetVertices(list);
    		val.SetUVs(0, list2);
    		val.SetTriangles(list3, 0);
    		OrientOutward(val);
    		val.RecalculateNormals();
    		val.RecalculateBounds();
    		Weld(val);
    		return val;
    	}

    	private static void OrientOutward(Mesh m)
    	{
    		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
    		Vector3[] vertices = m.vertices;
    		int[] triangles = m.triangles;
    		if (triangles.Length < 3)
    		{
    			return;
    		}
    		double num = 0.0;
    		for (int i = 0; i < triangles.Length; i += 3)
    		{
    			Vector3 val = vertices[triangles[i]];
    			Vector3 val2 = vertices[triangles[i + 1]];
    			Vector3 val3 = vertices[triangles[i + 2]];
    			num += (double)Vector3.Dot(val, Vector3.Cross(val2, val3));
    		}
    		num /= 6.0;
    		if (!(Mathf.Abs((float)num) < 1E-07f) && !(num > 0.0))
    		{
    			for (int j = 0; j < triangles.Length; j += 3)
    			{
    				int num2 = triangles[j + 1];
    				triangles[j + 1] = triangles[j + 2];
    				triangles[j + 2] = num2;
    			}
    			m.triangles = triangles;
    		}
    	}

    	private static void AddCap(List<Vector3> verts, List<Vector2> uvs, List<int> tris, Vector2[] profile, float angleDeg, float yScale, float rOffset, float tiltRad, bool atStart)
    	{
    		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
    		int num = profile.Length;
    		float num2 = 0f;
    		float num3 = 0f;
    		for (int i = 0; i < num - 1; i++)
    		{
    			num2 += profile[i].x;
    			num3 += profile[i].y;
    		}
    		num2 /= (float)(num - 1);
    		num3 /= (float)(num - 1);
    		float num4 = angleDeg * ((float)Math.PI / 180f);
    		float num5 = Mathf.Cos(num4);
    		float num6 = Mathf.Sin(num4);
    		float num7 = num3 * yScale;
    		float num8 = num2 + rOffset + num3 * Mathf.Sin(tiltRad);
    		int count = verts.Count;
    		verts.Add(new Vector3(num5 * num8, num7, num6 * num8));
    		uvs.Add(new Vector2(0.5f, 0.5f));
    		int count2 = verts.Count;
    		for (int j = 0; j < num - 1; j++)
    		{
    			float num9 = profile[j].x + rOffset;
    			float num10 = profile[j].y * yScale;
    			float num11 = num10 * Mathf.Cos(tiltRad);
    			float num12 = num9 + num10 * Mathf.Sin(tiltRad);
    			verts.Add(new Vector3(num5 * num12, num11, num6 * num12));
    			uvs.Add(new Vector2(0.5f + profile[j].x * 0.4f, 0.5f + profile[j].y * 0.4f));
    		}
    		for (int k = 0; k < num - 2; k++)
    		{
    			int item = count2 + k;
    			int item2 = count2 + k + 1;
    			if (atStart)
    			{
    				tris.Add(count);
    				tris.Add(item2);
    				tris.Add(item);
    			}
    			else
    			{
    				tris.Add(count);
    				tris.Add(item);
    				tris.Add(item2);
    			}
    		}
    	}

    	private static Mesh Box(float w, float h, float d, int _ = 0)
    	{
    		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
    		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
    		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
    		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0253: Unknown result type (might be due to invalid IL or missing references)
    		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0261: Expected Obj, but got Unknown
    		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
    		//IL_026e: Expected Obj, but got Unknown
    		List<Vector3> list = new List<Vector3>();
    		List<Vector2> list2 = new List<Vector2>();
    		List<int> list3 = new List<int>();
    		Vector3 val = new Vector3(w, h, d) * 0.5f;
    		var array = new[]
    		{
    			new
    			{
    				n = Vector3.forward,
    				u = Vector3.right,
    				v = Vector3.up,
    				w = w,
    				h = h
    			},
    			new
    			{
    				n = Vector3.back,
    				u = Vector3.left,
    				v = Vector3.up,
    				w = w,
    				h = h
    			},
    			new
    			{
    				n = Vector3.right,
    				u = Vector3.back,
    				v = Vector3.up,
    				w = d,
    				h = h
    			},
    			new
    			{
    				n = Vector3.left,
    				u = Vector3.forward,
    				v = Vector3.up,
    				w = d,
    				h = h
    			},
    			new
    			{
    				n = Vector3.up,
    				u = Vector3.right,
    				v = Vector3.forward,
    				w = w,
    				h = d
    			},
    			new
    			{
    				n = Vector3.down,
    				u = Vector3.right,
    				v = Vector3.back,
    				w = w,
    				h = d
    			}
    		};
    		foreach (var anon in array)
    		{
    			Vector3 n = anon.n;
    			Vector3 val2 = anon.u * (anon.w * 0.5f);
    			Vector3 val3 = anon.v * (anon.h * 0.5f);
    			Vector3 val4 = Vector3.Scale(n, val);
    			int count = list.Count;
    			list.Add(val4 - val2 - val3);
    			list2.Add(new Vector2(0f, 0f));
    			list.Add(val4 + val2 - val3);
    			list2.Add(new Vector2(anon.w * 0.5f, 0f));
    			list.Add(val4 + val2 + val3);
    			list2.Add(new Vector2(anon.w * 0.5f, anon.h * 0.5f));
    			list.Add(val4 - val2 + val3);
    			list2.Add(new Vector2(0f, anon.h * 0.5f));
    			list3.Add(count);
    			list3.Add(count + 2);
    			list3.Add(count + 1);
    			list3.Add(count);
    			list3.Add(count + 3);
    			list3.Add(count + 2);
    		}
    		Mesh val5 = new Mesh
    		{
    			name = "FountainBox"
    		};
    		val5.SetVertices(list);
    		val5.SetUVs(0, list2);
    		val5.SetTriangles(list3, 0);
    		OrientOutward(val5);
    		val5.RecalculateNormals();
    		val5.RecalculateBounds();
    		return val5;
    	}

    	private static Mesh Sphere(float r, int seg, int rings)
    	{
    		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
    		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0160: Expected Obj, but got Unknown
    		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
    		//IL_016d: Expected Obj, but got Unknown
    		List<Vector3> list = new List<Vector3>();
    		List<Vector2> list2 = new List<Vector2>();
    		List<int> list3 = new List<int>();
    		for (int i = 0; i <= rings; i++)
    		{
    			float num = (float)i / (float)rings;
    			float num2 = num * (float)Math.PI;
    			for (int j = 0; j <= seg; j++)
    			{
    				float num3 = (float)j / (float)seg;
    				float num4 = num3 * (float)Math.PI * 2f;
    				list.Add(new Vector3(r * Mathf.Sin(num2) * Mathf.Cos(num4), r * Mathf.Cos(num2), r * Mathf.Sin(num2) * Mathf.Sin(num4)));
    				list2.Add(new Vector2(num3 * (float)Math.PI * r * 0.5f * 2f, num * (float)Math.PI * r * 0.5f));
    			}
    		}
    		for (int k = 0; k < rings; k++)
    		{
    			for (int l = 0; l < seg; l++)
    			{
    				int num5 = k * (seg + 1) + l;
    				int num6 = num5 + seg + 1;
    				list3.Add(num5);
    				list3.Add(num6);
    				list3.Add(num5 + 1);
    				list3.Add(num5 + 1);
    				list3.Add(num6);
    				list3.Add(num6 + 1);
    			}
    		}
    		Mesh val = new Mesh
    		{
    			name = "FountainHead"
    		};
    		val.SetVertices(list);
    		val.SetUVs(0, list2);
    		val.SetTriangles(list3, 0);
    		OrientOutward(val);
    		val.RecalculateNormals();
    		val.RecalculateBounds();
    		return val;
    	}

    	private static Mesh Disc(float r, int seg)
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0101: Expected Obj, but got Unknown
    		List<Vector3> list = new List<Vector3> { Vector3.zero };
    		List<Vector2> list2 = new List<Vector2>
    		{
    			new Vector2(0.5f, 0.5f)
    		};
    		List<int> list3 = new List<int>();
    		for (int i = 0; i <= seg; i++)
    		{
    			float num = (float)i / (float)seg * (float)Math.PI * 2f;
    			list.Add(new Vector3(Mathf.Cos(num) * r, 0f, Mathf.Sin(num) * r));
    			list2.Add(new Vector2(0.5f + Mathf.Cos(num) * 0.5f, 0.5f + Mathf.Sin(num) * 0.5f));
    		}
    		for (int j = 1; j <= seg; j++)
    		{
    			list3.Add(0);
    			list3.Add(j + 1);
    			list3.Add(j);
    		}
    		Mesh val = new Mesh
    		{
    			name = "FountainWater"
    		};
    		val.SetVertices(list);
    		val.SetUVs(0, list2);
    		val.SetTriangles(list3, 0);
    		val.RecalculateNormals();
    		val.RecalculateBounds();
    		return val;
    	}

    	private static void Weld(Mesh m)
    	{
    		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
    		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
    		Vector3[] vertices = m.vertices;
    		Vector3[] normals = m.normals;
    		Dictionary<Vector3Int, List<int>> dictionary = new Dictionary<Vector3Int, List<int>>(vertices.Length);
    		Vector3Int[] array = new Vector3Int[vertices.Length];
    		for (int i = 0; i < vertices.Length; i++)
    		{
    			Vector3 val = vertices[i];
    			Vector3Int key = (array[i] = new Vector3Int(Mathf.RoundToInt(val.x * 10000f), Mathf.RoundToInt(val.y * 10000f), Mathf.RoundToInt(val.z * 10000f)));
    			if (!dictionary.TryGetValue(key, out var value))
    			{
    				value = (dictionary[key] = new List<int>(2));
    			}
    			value.Add(i);
    		}
    		Vector3[] array2 = (Vector3[])normals.Clone();
    		foreach (KeyValuePair<Vector3Int, List<int>> item in dictionary)
    		{
    			if (item.Value.Count < 2)
    			{
    				continue;
    			}
    			Vector3 val2 = Vector3.zero;
    			foreach (int item2 in item.Value)
    			{
    				val2 += normals[item2];
    			}
    			Vector3 normalized = val2.normalized;
    			foreach (int item3 in item.Value)
    			{
    				array2[item3] = normalized;
    			}
    		}
    		m.normals = array2;
    	}

    	private static int AddMesh(GameObject go, Mesh mesh, Material mat, StringBuilder sb, ref int meshCount, ref int colliderCount, string childName = null, bool withCollider = true)
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001f: Expected Obj, but got Unknown
    		if (childName != null)
    		{
    			GameObject val = new GameObject(childName);
    			val.transform.SetParent(go.transform, false);
    			go = val;
    		}
    		go.AddComponent<MeshFilter>().sharedMesh = mesh;
    		((Renderer)go.AddComponent<MeshRenderer>()).sharedMaterial = mat;
    		go.isStatic = true;
    		if (withCollider)
    		{
    			MeshCollider val2 = go.AddComponent<MeshCollider>();
    			val2.sharedMesh = mesh;
    			val2.convex = false;
    			colliderCount++;
    		}
    		meshCount++;
    		if (!((Object)(object)mesh != (Object)null))
    		{
    			return 0;
    		}
    		return mesh.triangles.Length / 3;
    	}

    	private static void EnsureFolder(string assetFolder)
    	{
    		assetFolder = assetFolder.Replace('\\', '/').TrimEnd('/');
    		if (AssetDatabase.IsValidFolder(assetFolder))
    		{
    			return;
    		}
    		int num = assetFolder.LastIndexOf('/');
    		if (num > 0)
    		{
    			string text = assetFolder.Substring(0, num);
    			EnsureFolder(text);
    			if (!AssetDatabase.IsValidFolder(text))
    			{
    				AssetDatabase.Refresh();
    			}
    			if (!AssetDatabase.IsValidFolder(assetFolder))
    			{
    				AssetDatabase.CreateFolder(text, assetFolder.Substring(num + 1));
    			}
    		}
    	}

    	private static int SaveMeshAssets(GameObject root, StringBuilder sb)
    	{
    		int num = 0;
    		MeshFilter[] componentsInChildren = root.GetComponentsInChildren<MeshFilter>(true);
    		foreach (MeshFilter val in componentsInChildren)
    		{
    			if (!((Object)(object)val.sharedMesh == (Object)null))
    			{
    				string text = ((Object)((Component)val).gameObject.transform.parent).name + "_" + ((Object)((Component)val).gameObject).name;
    				((Object)val.sharedMesh).name = text;
    				string text2 = "Assets/Painterly/Generated/Fountain/" + text + ".asset";
    				if ((Object)(object)AssetDatabase.LoadAssetAtPath<Mesh>(text2) != (Object)null)
    				{
    					AssetDatabase.DeleteAsset(text2);
    				}
    				AssetDatabase.CreateAsset((Object)(object)val.sharedMesh, text2);
    				if ((Object)(object)AssetDatabase.LoadAssetAtPath<Mesh>(text2) != (Object)null)
    				{
    					num++;
    				}
    				else
    				{
    					sb.AppendLine("  mesh asset NOT written: " + text2);
    				}
    			}
    		}
    		AssetDatabase.SaveAssets();
    		AssetDatabase.Refresh();
    		return num;
    	}

    	private static Material EnsureWaterMaterial(StringBuilder sb)
    	{
    		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cb: Expected Obj, but got Unknown
    		Material val = AssetDatabase.LoadAssetAtPath<Material>("Assets/Painterly/Materials/FountainWater.mat");
    		if ((Object)(object)val != (Object)null)
    		{
    			return val;
    		}
    		Shader val2 = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Painterly/Shaders/PainterlyLit.shader");
    		if ((Object)(object)val2 == (Object)null)
    		{
    			val2 = Shader.Find("Echoes/PainterlyLit");
    		}
    		if ((Object)(object)val2 == (Object)null)
    		{
    			sb.AppendLine("FATAL: PainterlyLit shader not found; water will be skipped");
    			return null;
    		}
    		Material val3 = new Material(val2)
    		{
    			name = "FountainWater"
    		};
    		val3.SetColor("_BaseColor", new Color(0.62f, 0.7f, 0.74f, 1f));
    		val3.SetFloat("_Smoothness", 0.92f);
    		val3.SetFloat("_Metallic", 0f);
    		val3.SetFloat("_ColorRestore", 0f);
    		val3.SetFloat("_RestoreBoost", 1f);
    		AssetDatabase.CreateAsset((Object)val3, "Assets/Painterly/Materials/FountainWater.mat");
    		AssetDatabase.SaveAssets();
    		AssetDatabase.ImportAsset("Assets/Painterly/Materials/FountainWater.mat");
    		sb.AppendLine("created water material -> Assets/Painterly/Materials/FountainWater.mat");
    		return AssetDatabase.LoadAssetAtPath<Material>("Assets/Painterly/Materials/FountainWater.mat");
    	}

    	private static float FootprintRadius(GameObject root)
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 position = root.transform.position;
    		float num = 0f;
    		Renderer[] componentsInChildren = root.GetComponentsInChildren<Renderer>(true);
    		for (int i = 0; i < componentsInChildren.Length; i++)
    		{
    			Bounds bounds = componentsInChildren[i].bounds;
    			Vector2 val = new Vector2(bounds.center.x - position.x, bounds.center.z - position.z);
    			num = Mathf.Max(num, val.magnitude + Mathf.Max(bounds.extents.x, bounds.extents.z));
    		}
    		return num;
    	}

    	private static void ScaleBuilt(GameObject root, float s)
    	{
    		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
    		MeshFilter[] componentsInChildren = root.GetComponentsInChildren<MeshFilter>(true);
    		foreach (MeshFilter val in componentsInChildren)
    		{
    			Mesh sharedMesh = val.sharedMesh;
    			if (!((Object)(object)sharedMesh == (Object)null))
    			{
    				Vector3[] vertices = sharedMesh.vertices;
    				for (int j = 0; j < vertices.Length; j++)
    				{
    					ref Vector3 reference = ref vertices[j];
    					reference *= s;
    				}
    				Vector2[] uv = sharedMesh.uv;
    				for (int k = 0; k < uv.Length; k++)
    				{
    					ref Vector2 reference2 = ref uv[k];
    					reference2 *= s;
    				}
    				sharedMesh.vertices = vertices;
    				sharedMesh.uv = uv;
    				sharedMesh.RecalculateBounds();
    				MeshCollider component = ((Component)val).GetComponent<MeshCollider>();
    				if ((Object)(object)component != (Object)null)
    				{
    					component.sharedMesh = null;
    					component.sharedMesh = sharedMesh;
    				}
    			}
    		}
    		Transform[] componentsInChildren2 = root.GetComponentsInChildren<Transform>(true);
    		foreach (Transform val2 in componentsInChildren2)
    		{
    			if ((Object)(object)val2 != (Object)(object)root.transform)
    			{
    				val2.localPosition *= s;
    			}
    		}
    	}

    	private static float MedianClearance(SquarePlacement.Scan scan)
    	{
    		if (scan == null || scan.Spots.Count == 0)
    		{
    			return 0f;
    		}
    		List<float> list = (from s in scan.Spots
    			select s.Clearance into v
    			orderby v
    			select v).ToList();
    		return list[list.Count / 2];
    	}

    	private static string PathOf(Transform t)
    	{
    		if ((Object)(object)t == (Object)null)
    		{
    			return "<none>";
    		}
    		StringBuilder stringBuilder = new StringBuilder(((Object)t).name);
    		Transform parent = t.parent;
    		while ((Object)(object)parent != (Object)null)
    		{
    			stringBuilder.Insert(0, ((Object)parent).name + "/");
    			parent = parent.parent;
    		}
    		return stringBuilder.ToString();
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), "Temp/fountain.txt")));
    		File.WriteAllText("Temp/fountain.txt", sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}