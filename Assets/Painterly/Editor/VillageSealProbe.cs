using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class VillageSealProbe
    {
    	private const string OutPath = "Temp/village_seal.txt";

    	[MenuItem("Tools/Echoes/Probe Village Seal", priority = 52)]
    	public static void Run()
    	{
    		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0088: Expected Obj, but got Unknown
    		//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
    		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
    		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0361: Unknown result type (might be due to invalid IL or missing references)
    		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0375: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0323: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0414: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03ac: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03cc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03db: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0483: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04af: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04bb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04e8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
    		//IL_044b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_053f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0550: Unknown result type (might be due to invalid IL or missing references)
    		GameObject val = GameObject.Find("Village_Grey");
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] No Village_Grey in the scene.");
    			return;
    		}
    		GameObject val2 = Object.Instantiate<GameObject>(val);
    		((Object)val2).name = "__SealProbeVillage";
    		((Object)val2).hideFlags = (HideFlags)61;
    		List<Transform> list = new List<Transform>();
    		Transform[] componentsInChildren = val2.GetComponentsInChildren<Transform>(true);
    		foreach (Transform val3 in componentsInChildren)
    		{
    			if (((Object)val3).name.StartsWith("House_"))
    			{
    				list.Add(val3);
    			}
    		}
    		GameObject val4 = new GameObject("__SealProbe");
    		((Object)val4).hideFlags = (HideFlags)61;
    		bool queriesHitBackfaces = Physics.queriesHitBackfaces;
    		Physics.queriesHitBackfaces = true;
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("Assembled-house gable seal test (rays along Z, through the gable ends)");
    		stringBuilder.AppendLine();
    		int num = 0;
    		int num2 = 0;
    		int num3 = 0;
    		int num4 = 0;
    		int num5 = 0;
    		int num6 = -1;
    		Collider[] array = new Collider[64];
    		foreach (Transform item in list)
    		{
    			Renderer[] componentsInChildren2 = ((Component)item).GetComponentsInChildren<Renderer>();
    			List<MeshCollider> list2 = new List<MeshCollider>();
    			Renderer[] array2 = componentsInChildren2;
    			foreach (Renderer val5 in array2)
    			{
    				if (!((Object)(object)((Component)val5).GetComponent<MeshCollider>() != (Object)null))
    				{
    					MeshFilter component = ((Component)val5).GetComponent<MeshFilter>();
    					if (!((Object)(object)component == (Object)null) && !((Object)(object)component.sharedMesh == (Object)null))
    					{
    						MeshCollider val6 = ((Component)val5).gameObject.AddComponent<MeshCollider>();
    						val6.sharedMesh = component.sharedMesh;
    						list2.Add(val6);
    					}
    				}
    			}
    			Physics.SyncTransforms();
    			num4 += list2.Count;
    			if (num6 < 0)
    			{
    				num6 = list2.Count;
    			}
    			if (!((Component)item).gameObject.activeInHierarchy)
    			{
    				num5++;
    			}
    			Bounds val7 = default;
    			bool flag = false;
    			array2 = componentsInChildren2;
    			foreach (Renderer val8 in array2)
    			{
    				if (((Object)val8).name.StartsWith("Roof_RoundTiles"))
    				{
    					if (!flag)
    					{
    						val7 = val8.bounds;
    						flag = true;
    					}
    					else
    					{
    						val7.Encapsulate(val8.bounds);
    					}
    				}
    			}
    			if (!flag)
    			{
    				num2++;
    				foreach (MeshCollider item2 in list2)
    				{
    					Object.DestroyImmediate((Object)(object)item2);
    				}
    				continue;
    			}
    			Bounds val9 = new Bounds(Vector3.zero, Vector3.zero);
    			array2 = componentsInChildren2;
    			foreach (Renderer val10 in array2)
    			{
    				val9.Encapsulate(val10.bounds);
    			}
    			int num7 = 0;
    			StringBuilder stringBuilder2 = new StringBuilder();
    			for (int j = 0; j < 11; j++)
    			{
    				float num8 = (float)j / 10f;
    				float num9 = Mathf.Lerp(val7.min.y, val7.max.y, num8 * num8);
    				if (!Blocked(new Vector3(val7.center.x, num9, val7.center.z), Vector3.forward, val7.extents.z * 2f + 4f))
    				{
    					num7++;
    					if (stringBuilder2.Length > 0)
    					{
    						stringBuilder2.Append(", ");
    					}
    					stringBuilder2.Append($"{(num9 - val7.min.y) / val7.size.y:0.00}");
    				}
    			}
    			if (!Physics.Raycast(val7.center + Vector3.up * 50f, Vector3.down, 120f, -1, (QueryTriggerInteraction)1))
    			{
    				num3++;
    			}
    			string text = "";
    			if (num4 == num6)
    			{
    				Vector3 val11 = new Vector3(1f, 0.5f, 1f);
    				int num10 = Physics.OverlapBoxNonAlloc(val7.center, val11, array, Quaternion.identity, -1, (QueryTriggerInteraction)1);
    				bool flag2 = Physics.Raycast(val7.center + Vector3.up * 20f, Vector3.down, 60f, -1, (QueryTriggerInteraction)1);
    				text = $"   [diag: overlap {num10}, raycast-down {flag2}]";
    			}
    			Bounds val12 = default;
    			bool flag3 = false;
    			array2 = componentsInChildren2;
    			foreach (Renderer val13 in array2)
    			{
    				if (((Object)val13).name.StartsWith("Roof_Front_Brick"))
    				{
    					if (!flag3)
    					{
    						val12 = val13.bounds;
    						flag3 = true;
    					}
    					else
    					{
    						val12.Encapsulate(val13.bounds);
    					}
    				}
    			}
    			string text2 = (flag3 ? ($"gable top {val12.max.y:0.00} vs roof top {val7.max.y:0.00} " + $"(short {val7.max.y - val12.max.y:0.00}), " + $"gable half-width {val12.extents.x:0.00} vs roof half-width {val7.extents.x:0.00}") : "NO GABLE WALL");
    			if (num7 > 0)
    			{
    				num++;
    			}
    			stringBuilder.AppendLine($"{((Object)item).name,-22} colliders {list2.Count,3}  roof " + $"{val7.size.x:0.0}x{val7.size.y:0.0}" + $"  gable open at {num7}/{11} heights" + ((num7 > 0) ? $"  [at rise {stringBuilder2}]" : "  [sealed]") + "  " + text2 + text);
    			foreach (MeshCollider item3 in list2)
    			{
    				Object.DestroyImmediate((Object)(object)item3);
    			}
    			Physics.SyncTransforms();
    		}
    		Object.DestroyImmediate((Object)(object)val4);
    		Object.DestroyImmediate((Object)(object)val2);
    		Physics.queriesHitBackfaces = queriesHitBackfaces;
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine($"{list.Count} houses: {num} still see-through, " + $"{num2} without a round-tile roof, {num3} control failures.");
    		stringBuilder.AppendLine($"colliders added: {num4}; inactive houses: {num5}");
    		if (num3 > 0)
    		{
    			stringBuilder.AppendLine($"RESULT VOID: the downward control missed on {num3} house(s), " + "so the colliders are not being hit and 'see-through' here means 'no collider', not 'hole'. Fix the probe before trusting any number above.");
    		}
    		else
    		{
    			stringBuilder.AppendLine("Control passed on every house (a downward ray hit each roof), so the gable figures above mean what they say.");
    		}
    		File.WriteAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/village_seal.txt"), stringBuilder.ToString());
    		Debug.Log((object)("[Echoes] Village seal test -> Temp/village_seal.txt\n" + $"{num} of {list.Count} houses still see-through at the gable."));
    	}

    	private static bool Blocked(Vector3 origin, Vector3 dir, float span)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		return Physics.Raycast(origin + dir * (span * 0.5f + 0.05f), -dir, span, -1, (QueryTriggerInteraction)1);
    	}
    }
}