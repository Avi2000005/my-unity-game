using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class L1CrawlerProbe
    {
    	private const string Report = "Temp/l1_crawler.txt";

    	[MenuItem("Tools/Echoes/Probe the Crawlers", priority = 72)]
    	public static void Run()
    	{
    		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_048a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_048f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0493: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_04c3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_056d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_058e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0593: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05a4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05a9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05ae: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05b7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05bc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05c9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0740: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0745: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0635: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0659: Unknown result type (might be due to invalid IL or missing references)
    		//IL_065e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_066f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0674: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0679: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0682: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0687: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0694: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0699: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] can a crawler ever see Ari in the yard?");
    		Scene activeScene = SceneManager.GetActiveScene();
    		if (activeScene.rootCount == 0)
    		{
    			stringBuilder.AppendLine("  FATAL: no scene open.");
    			Finish(stringBuilder);
    			return;
    		}
    		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    		Beat5Director beat5Director = Object.FindAnyObjectByType<Beat5Director>((FindObjectsInactive)1);
    		if ((Object)(object)ariMover == (Object)null)
    		{
    			stringBuilder.AppendLine("  FATAL: no AriMover, so there is nothing to be seen.");
    			Finish(stringBuilder);
    			return;
    		}
    		InkCrawler[] array = Object.FindObjectsByType<InkCrawler>((FindObjectsInactive)1);
    		stringBuilder.AppendLine("  AriMover '" + ((Object)ariMover).name + "', body height " + ariMover.BodyHeight.ToString("0.00") + " m");
    		float num = (((Object)(object)beat5Director != (Object)null) ? beat5Director.EnterX : 38f);
    		float num2 = (((Object)(object)beat5Director != (Object)null) ? beat5Director.ExitX : 51f);
    		float num3 = 0f;
    		string text = "NOTHING — defaulted to 0";
    		BeatGate beatGate = Object.FindAnyObjectByType<BeatGate>((FindObjectsInactive)1);
    		LevelCheckpoint levelCheckpoint = Object.FindAnyObjectByType<LevelCheckpoint>((FindObjectsInactive)1);
    		if ((Object)(object)beatGate != (Object)null)
    		{
    			num3 = ((Component)beatGate).transform.position.z;
    			text = "the gate";
    		}
    		else if ((Object)(object)levelCheckpoint != (Object)null)
    		{
    			num3 = ((Component)levelCheckpoint).transform.position.z;
    			text = "the checkpoint";
    		}
    		stringBuilder.AppendLine("  Beat5Director: " + (((Object)(object)beat5Director != (Object)null) ? ("EnterX " + num.ToString("0.0") + ", ExitX " + num2.ToString("0.0")) : "NOT IN THE SCENE — using typed values, which is itself a fault"));
    		stringBuilder.AppendLine("  lane z " + num3.ToString("0.00") + " read off " + text);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("=== THE THREE ===");
    		InkCrawler[] array2 = array;
    		if ((Object)(object)beat5Director != (Object)null && beat5Director.CrawlerCount > 0)
    		{
    			array2 = new InkCrawler[beat5Director.CrawlerCount];
    			for (int i = 0; i < array2.Length; i++)
    			{
    				array2[i] = beat5Director.Crawlers[i];
    			}
    		}
    		stringBuilder.AppendLine("  in the beat: " + array2.Length + "   (found in the scene at all: " + array.Length + ")");
    		Vector3 val;
    		for (int j = 0; j < array2.Length; j++)
    		{
    			InkCrawler inkCrawler = array2[j];
    			if ((Object)(object)inkCrawler == (Object)null)
    			{
    				stringBuilder.AppendLine("  [" + j + "] NULL in the list");
    				continue;
    			}
    			InkCrawlerEmerge component = ((Component)inkCrawler).GetComponent<InkCrawlerEmerge>();
    			stringBuilder.AppendLine("  [" + j + "] " + ((Object)inkCrawler).name);
    			string[] array3 = new string[11]
    			{
    				"        stands at ", null, null, null, null, null, null, null, null, null,
    				null
    			};
    			val = ((Component)inkCrawler).transform.position;
    			array3[1] = val.ToString("F2");
    			array3[2] = "   notice ";
    			array3[3] = inkCrawler.NoticeRadius.ToString("0.0");
    			array3[4] = " m, forget ";
    			array3[5] = inkCrawler.ForgetRadius.ToString("0.0");
    			array3[6] = " m, lunge ";
    			array3[7] = inkCrawler.LungeRange.ToString("0.0");
    			array3[8] = " m, standoff ";
    			array3[9] = inkCrawler.StandoffRange.ToString("0.0");
    			array3[10] = " m";
    			stringBuilder.AppendLine(string.Concat(array3));
    			stringBuilder.AppendLine("        emerge " + (((Object)(object)component != (Object)null) ? "present" : "MISSING — it stands at load, the arrival never plays") + ", collider enabled " + ColliderState(inkCrawler));
    			val = inkCrawler.Eye;
    			stringBuilder.AppendLine("        eye at " + val.ToString("F2"));
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("=== SWEEP: Ari walking the yard, x " + num.ToString("0.0") + " to " + num2.ToString("0.0") + " ===");
    		stringBuilder.AppendLine("  For every point he can stand, the distance to each crawler");
    		stringBuilder.AppendLine("  and whether that crawler's OWN line-of-sight test passes.");
    		float num4 = float.MaxValue;
    		float num5 = float.MinValue;
    		float num6 = float.MaxValue;
    		float num7 = float.MinValue;
    		for (int k = 0; k < array2.Length; k++)
    		{
    			if (!((Object)(object)array2[k] == (Object)null))
    			{
    				Vector3 position = ((Component)array2[k]).transform.position;
    				num4 = Mathf.Min(num4, position.x);
    				num5 = Mathf.Max(num5, position.x);
    				num6 = Mathf.Min(num6, position.z);
    				num7 = Mathf.Max(num7, position.z);
    			}
    		}
    		float num8 = num4 - 12f;
    		float num9 = num5 + 12f;
    		float num10 = num6 - 12f;
    		float num11 = num7 + 12f;
    		float num12 = num3;
    		foreach (InkCrawler inkCrawler2 in array2)
    		{
    			if ((Object)(object)inkCrawler2 == (Object)null)
    			{
    				continue;
    			}
    			int num13 = 0;
    			int num14 = 0;
    			float num15 = float.MaxValue;
    			bool flag = false;
    			int num16 = 0;
    			int num17 = 0;
    			float num18 = float.MaxValue;
    			bool flag2 = false;
    			float num19 = 0f;
    			float num20 = 0f;
    			for (float num21 = num - 2f; num21 <= num2 + 2f; num21 += 0.5f)
    			{
    				float num22 = FloorAt(new Vector3(num21, 40f, num12));
    				if (!float.IsNaN(num22))
    				{
    					num13++;
    					Vector3 val2 = new Vector3(num21, num22, num12) + Vector3.up * (ariMover.BodyHeight * 0.6f);
    					float num23 = Vector3.Distance(((Component)inkCrawler2).transform.position, val2);
    					bool flag3 = !inkCrawler2.BlockedFromPost(inkCrawler2.Eye, val2);
    					if ((num23 <= inkCrawler2.NoticeRadius) & flag3)
    					{
    						num14++;
    					}
    					if (num23 < num15)
    					{
    						num15 = num23;
    						flag = flag3;
    					}
    				}
    			}
    			for (float num24 = num8; num24 <= num9; num24 += 0.5f)
    			{
    				for (float num25 = num10; num25 <= num11; num25++)
    				{
    					float num26 = FloorAt(new Vector3(num24, 40f, num25));
    					if (!float.IsNaN(num26))
    					{
    						num16++;
    						Vector3 val3 = new Vector3(num24, num26, num25) + Vector3.up * (ariMover.BodyHeight * 0.6f);
    						float num27 = Vector3.Distance(((Component)inkCrawler2).transform.position, val3);
    						bool flag4 = !inkCrawler2.BlockedFromPost(inkCrawler2.Eye, val3);
    						if ((num27 <= inkCrawler2.NoticeRadius) & flag4)
    						{
    							num17++;
    						}
    						if (num27 < num18)
    						{
    							num18 = num27;
    							flag2 = flag4;
    							num19 = num24;
    							num20 = num25;
    						}
    					}
    				}
    			}
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("  " + ((Object)inkCrawler2).name);
    			string text2 = inkCrawler2.NoticeRadius.ToString("0.00");
    			val = ((Component)inkCrawler2).transform.position;
    			stringBuilder.AppendLine("      notice radius " + text2 + " m, at " + val.ToString("F2"));
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("      ON THE LANE (straight run, z " + num12.ToString("0.0") + ", " + num13 + " walkable cells):");
    			stringBuilder.AppendLine("          closest " + num15.ToString("0.00") + " m" + (flag ? ", clear line" : ", BLOCKED") + "   -> noticed at " + num14 + " cell(s)");
    			stringBuilder.AppendLine("      ON THE GROUND (x " + num8.ToString("0.0") + " .. " + num9.ToString("0.0") + ", z " + num10.ToString("0.0") + " .. " + num11.ToString("0.0") + ", " + num16 + " walkable cells):");
    			stringBuilder.AppendLine("          closest " + num18.ToString("0.00") + " m at (" + num19.ToString("0.0") + ", " + num20.ToString("0.0") + ")" + (flag2 ? ", clear line" : ", BLOCKED") + "   -> noticed at " + num17 + " cell(s)");
    			if (num14 == 0 && num17 == 0)
    			{
    				stringBuilder.AppendLine();
    				stringBuilder.AppendLine("      VERDICT: INERT. It cannot see him anywhere he can stand.");
    				if (num18 > inkCrawler2.NoticeRadius)
    				{
    					stringBuilder.AppendLine("      The closest approach (" + num18.ToString("0.00") + " m) is further than its own " + inkCrawler2.NoticeRadius.ToString("0.00") + " m notice radius.");
    				}
    				else
    				{
    					stringBuilder.AppendLine("      He comes inside the radius but every line from its eye to him chest is blocked by something, so the sight test never succeeds.");
    				}
    				stringBuilder.AppendLine("      It stands in the yard doing nothing and attacks never. This is the reported fault.");
    			}
    			else if (num14 == 0)
    			{
    				stringBuilder.AppendLine();
    				stringBuilder.AppendLine("      VERDICT: he walks straight past it. Not inert — it will hunt if he comes its way (" + num17 + " cells) — but holding forward engages nothing.");
    			}
    			else
    			{
    				stringBuilder.AppendLine();
    				stringBuilder.AppendLine("      VERDICT: it engages his on a straight walk. " + num14 + " cells on the lane, " + num17 + " on the ground.");
    			}
    		}
    		Finish(stringBuilder);
    	}

    	private static string ColliderState(InkCrawler c)
    	{
    		Collider[] componentsInChildren = ((Component)c).GetComponentsInChildren<Collider>(true);
    		if (componentsInChildren == null || componentsInChildren.Length == 0)
    		{
    			return "NO COLLIDER";
    		}
    		for (int i = 0; i < componentsInChildren.Length; i++)
    		{
    			if ((Object)(object)componentsInChildren[i] != (Object)null && componentsInChildren[i].enabled)
    			{
    				return "yes";
    			}
    		}
    		return "ALL OFF — it cannot be staggered while sunk";
    	}

    	private static float FloorAt(Vector3 from)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
    		RaycastHit[] array = Physics.RaycastAll(from, Vector3.down, 60f, -1, (QueryTriggerInteraction)1);
    		if (array == null || array.Length == 0)
    		{
    			return float.NaN;
    		}
    		float num = float.MinValue;
    		for (int i = 0; i < array.Length; i++)
    		{
    			num = Mathf.Max(num, array[i].point.y);
    		}
    		return num;
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/l1_crawler.txt"), sb.ToString());
    		Debug.Log((object)"[Echoes] crawler probe — see Temp/l1_crawler.txt");
    	}
    }
}