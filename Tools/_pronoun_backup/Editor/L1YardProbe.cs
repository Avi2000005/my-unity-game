using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class L1YardProbe
    {
    	private const string Report = "Temp/l1_yard.txt";

    	[MenuItem("Tools/Echoes/Probe the Yard", priority = 44)]
    	public static void Run()
    	{
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
    		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0258: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02cd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0302: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] probe the yard");
    		if (EditorApplication.isPlaying)
    		{
    			stringBuilder.AppendLine("  refused: in play mode");
    			Finish(stringBuilder);
    			return;
    		}
    		Scene activeScene = SceneManager.GetActiveScene();
    		if (activeScene.rootCount == 0)
    		{
    			stringBuilder.AppendLine("  FATAL: no scene open.");
    			Finish(stringBuilder);
    			return;
    		}
    		Beat5Director beat5Director = Object.FindAnyObjectByType<Beat5Director>((FindObjectsInactive)1);
    		float num = 32f;
    		float num2 = 62f;
    		float num3 = 0f;
    		float num4 = 22f;
    		stringBuilder.AppendLine("  grid: x " + num.ToString("0") + " .. " + num2.ToString("0") + ", z " + num3.ToString("0") + " .. " + num4.ToString("0") + ", cell " + 0.5f.ToString("0.0") + " m");
    		stringBuilder.AppendLine("  '#' floor   '.' no floor   'W' wall   'C' crawler   'F' Ari's spawn");
    		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    		int num5 = Mathf.RoundToInt((num2 - num) / 0.5f);
    		int num6 = Mathf.RoundToInt((num4 - num3) / 0.5f);
    		char[,] array = new char[num6, num5];
    		float num7 = float.MaxValue;
    		float num8 = float.MinValue;
    		float num9 = float.MaxValue;
    		float num10 = float.MinValue;
    		int num11 = 0;
    		int num12 = 0;
    		for (int i = 0; i < num6; i++)
    		{
    			float num13 = num3 + (float)i * 0.5f + 0.25f;
    			for (int j = 0; j < num5; j++)
    			{
    				float num14 = num + (float)j * 0.5f + 0.25f;
    				Vector3 val = new Vector3(num14, 0f, num13);
    				bool flag = false;
    				InkCrawler[] array2 = Object.FindObjectsByType<InkCrawler>((FindObjectsInactive)1);
    				for (int k = 0; k < array2.Length; k++)
    				{
    					if (!((Object)(object)array2[k] == (Object)null) && Mathf.Abs(((Component)array2[k]).transform.position.x - num14) < 0.8f && Mathf.Abs(((Component)array2[k]).transform.position.z - num13) < 0.8f)
    					{
    						flag = true;
    						break;
    					}
    				}
    				bool flag2 = (Object)(object)ariMover != (Object)null && Mathf.Abs(((Component)ariMover).transform.position.x - num14) < 0.8f && Mathf.Abs(((Component)ariMover).transform.position.z - num13) < 0.8f;
    				RaycastHit[] array3 = Physics.RaycastAll(val + Vector3.up * 12f, Vector3.down, 4f, -1, (QueryTriggerInteraction)1);
    				bool flag3 = false;
    				for (int l = 0; l < array3.Length; l++)
    				{
    					_ = array3[l].transform;
    					Bounds bounds = array3[l].collider.bounds;
    					if (!(bounds.size.y <= 0.6f))
    					{
    						flag3 = true;
    						break;
    					}
    				}
    				RaycastHit[] array4 = Physics.RaycastAll(val + Vector3.up * 4f, Vector3.down, 20f, -1, (QueryTriggerInteraction)1);
    				bool flag4 = false;
    				for (int m = 0; m < array4.Length; m++)
    				{
    					if (!((Object)(object)((Component)array4[m].collider).GetComponentInParent<InkCrawler>() != (Object)null) && !((Object)(object)((Component)array4[m].collider).GetComponentInParent<AriMover>() != (Object)null))
    					{
    						flag4 = true;
    						break;
    					}
    				}
    				char c;
    				if (!flag3)
    				{
    					if (flag)
    					{
    						c = 'C';
    					}
    					else if (flag2)
    					{
    						c = 'F';
    					}
    					else
    					{
    						c = ((!flag4) ? '.' : '#');
    					}
    				}
    				else
    				{
    					c = 'W';
    					num12++;
    				}
    				array[i, j] = c;
    				if (flag4 && !flag3 && !flag)
    				{
    					num11++;
    					num7 = Mathf.Min(num7, num14);
    					num8 = Mathf.Max(num8, num14);
    					num9 = Mathf.Min(num9, num13);
    					num10 = Mathf.Max(num10, num13);
    				}
    			}
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("=== THE MAP ===");
    		stringBuilder.AppendLine("     " + Axis(num, num2, 0.5f));
    		for (int num15 = num6 - 1; num15 >= 0; num15--)
    		{
    			StringBuilder stringBuilder2 = new StringBuilder();
    			stringBuilder2.Append("  ").Append((num3 + (float)num15 * 0.5f + 0.25f).ToString("00.0")).Append(' ');
    			for (int n = 0; n < num5; n++)
    			{
    				stringBuilder2.Append(array[num15, n]);
    			}
    			stringBuilder.AppendLine(stringBuilder2.ToString());
    		}
    		stringBuilder.AppendLine("     " + Axis(num, num2, 0.5f));
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("=== WHAT IT SAYS ===");
    		stringBuilder.AppendLine("  walkable floor cells: " + num11 + "  (each " + 0.25f.ToString("0.00") + " m2)");
    		stringBuilder.AppendLine("  wall cells: " + num12);
    		if (num11 == 0)
    		{
    			stringBuilder.AppendLine("  NO FLOOR FOUND ANYWHERE. The probe is looking in the wrong place or the yard has no colliders, and every other number here would be about nothing.");
    			Finish(stringBuilder);
    			return;
    		}
    		stringBuilder.AppendLine("  walkable extent: x " + num7.ToString("0.0") + " .. " + num8.ToString("0.0") + "   (" + (num8 - num7 + 0.5f).ToString("0.0") + " m)");
    		stringBuilder.AppendLine("                   z " + num9.ToString("0.0") + " .. " + num10.ToString("0.0") + "   (" + (num10 - num9 + 0.5f).ToString("0.0") + " m)");
    		stringBuilder.AppendLine("  area: " + ((num8 - num7 + 0.5f) * (num10 - num9 + 0.5f)).ToString("0.0") + " m2");
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("  cross-check against the two earlier numbers:");
    		stringBuilder.AppendLine("    renderer bounds said: 13.0 x 4.5 m  (x 39..52, z 8..12.5)");
    		stringBuilder.AppendLine("    walkable floor says: " + (num8 - num7 + 0.5f).ToString("0.0") + " x " + (num10 - num9 + 0.5f).ToString("0.0") + " m");
    		stringBuilder.AppendLine("    if these differ, the renderer bounds were measuring the paint and not the floor, and Beat 5 was being sized against a number about geometry.");
    		if ((Object)(object)beat5Director != (Object)null)
    		{
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("  Beat5Director.EnterX = " + beat5Director.EnterX.ToString("0.0") + ", ExitX = " + beat5Director.ExitX.ToString("0.0"));
    			stringBuilder.AppendLine("    enter line is " + ((beat5Director.EnterX < num7) ? ((num7 - beat5Director.EnterX).ToString("0.0") + " m WEST of the first floor cell — she starts on nothing, which may be correct (a lane outside the yard) or may be the whole reason the crawlers read as noticed too early") : ("inside the yard by " + (beat5Director.EnterX - num7).ToString("0.0") + " m")));
    		}
    		Finish(stringBuilder);
    	}

    	private static string Axis(float from, float to, float cell)
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		int num = Mathf.RoundToInt((to - from) / cell);
    		for (int i = 0; i < num; i++)
    		{
    			stringBuilder.Append((i % 10 == 0) ? ((from + (float)i * cell) / 10f % 10f).ToString("0") : ((object)' '));
    		}
    		return stringBuilder.ToString();
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/l1_yard.txt"), sb.ToString());
    		Debug.Log((object)"[Echoes] yard probed — see Temp/l1_yard.txt");
    	}
    }
}