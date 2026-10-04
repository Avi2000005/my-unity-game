using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class SquareClearanceProbe
    {
    	private const string Report = "Temp/square_clearance.txt";

    	public static void Run()
    	{
    		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
    		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
    		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0310: Unknown result type (might be due to invalid IL or missing references)
    		//IL_032b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0588: Unknown result type (might be due to invalid IL or missing references)
    		//IL_058d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05a8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05ad: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] open ground, measured");
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			stringBuilder.AppendLine("STOPPED: exit play mode first (Ctrl+P).");
    			Finish(stringBuilder);
    			return;
    		}
    		GameObject val = SquarePlacement.FindSquare();
    		if ((Object)(object)val == (Object)null)
    		{
    			stringBuilder.AppendLine("FATAL: no MarketSquare in the scene.");
    			Finish(stringBuilder);
    			return;
    		}
    		Bounds val2 = SquarePlacement.FindGround(SquarePlacement.TopBounds(val), out var groundY, val.transform);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- ground detection ---");
    		stringBuilder.AppendLine("MarketSquare      : " + SquarePlacement.PathOf(val.transform));
    		stringBuilder.AppendLine("chosen ground Y   : " + groundY.ToString("F3"));
    		stringBuilder.AppendLine("ground extent     : " + ((object)val2.size/*cast due to constrained. prefix*/).ToString() + " at " + ((object)val2.center/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine("height histogram of flat low pieces (0.1 m buckets):");
    		Dictionary<int, int> dictionary = new Dictionary<int, int>();
    		Renderer[] componentsInChildren = val.GetComponentsInChildren<Renderer>(true);
    		for (int i = 0; i < componentsInChildren.Length; i++)
    		{
    			Bounds bounds = componentsInChildren[i].bounds;
    			if (!(bounds.size.x < 0.8f) && !(bounds.size.z < 0.8f) && !(bounds.size.y > 0.7f) && !(bounds.max.y > 2f))
    			{
    				int key = Mathf.RoundToInt(bounds.max.y / 0.1f);
    				if (!dictionary.ContainsKey(key))
    				{
    					dictionary[key] = 0;
    				}
    				dictionary[key]++;
    			}
    		}
    		foreach (KeyValuePair<int, int> item in dictionary.OrderBy((KeyValuePair<int, int> k) => k.Key))
    		{
    			stringBuilder.AppendLine("   y~" + ((float)item.Key * 0.1f).ToString("F1").PadLeft(5) + "  " + item.Value + " piece(s)" + ((Mathf.Abs((float)item.Key * 0.1f - groundY) < 0.05f) ? "   <== chosen" : ""));
    		}
    		SquarePlacement.Scan scan = SquarePlacement.MeasureSquare();
    		if (!scan.Ok)
    		{
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("FATAL: square scan failed: " + scan.Why);
    			Finish(stringBuilder);
    			return;
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- market square ---");
    		stringBuilder.AppendLine("scanned area      : " + ((object)scan.Area.size/*cast due to constrained. prefix*/).ToString() + " at " + ((object)scan.Area.center/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine("ground samples    : " + scan.Samples);
    		stringBuilder.AppendLine("clearance median  : " + Median(scan).ToString("F2") + " m");
    		stringBuilder.AppendLine("clearance max     : " + scan.BestClearance.ToString("F2") + " m at " + ((object)scan.Best.Point/*cast due to constrained. prefix*/).ToString());
    		Map(stringBuilder, scan, 0.4f);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- what fits in the square ---");
    		float[] array = new float[8] { 3.24f, 2.6f, 2.2f, 2f, 1.8f, 1.6f, 1.4f, 1.2f };
    		for (int i = 0; i < array.Length; i++)
    		{
    			float num = array[i];
    			if (SquarePlacement.TryFind(scan, num, out var point, out var clearance, out var _))
    			{
    				stringBuilder.AppendLine("   radius " + num.ToString("F2").PadLeft(5) + " m (dia " + (2f * num).ToString("F2").PadLeft(5) + " m)  ->  " + ((object)point/*cast due to constrained. prefix*/).ToString().PadRight(28) + " clearance " + clearance.ToString("F2"));
    			}
    			else
    			{
    				stringBuilder.AppendLine("   radius " + num.ToString("F2").PadLeft(5) + " m (dia " + (2f * num).ToString("F2").PadLeft(5) + " m)  ->  DOES NOT FIT");
    			}
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- whole village, coarse ---");
    		SquarePlacement.Scan scan2 = SquarePlacement.MeasureVillage(1.2f, 8f);
    		if (!scan2.Ok)
    		{
    			stringBuilder.AppendLine("village scan failed: " + scan2.Why);
    		}
    		else
    		{
    			stringBuilder.AppendLine("scanned area      : " + ((object)scan2.Area.size/*cast due to constrained. prefix*/).ToString() + " at " + ((object)scan2.Area.center/*cast due to constrained. prefix*/).ToString());
    			stringBuilder.AppendLine("ground samples    : " + scan2.Samples);
    			stringBuilder.AppendLine("clearance max     : " + scan2.BestClearance.ToString("F2") + " m at " + ((object)scan2.Best.Point/*cast due to constrained. prefix*/).ToString());
    			stringBuilder.AppendLine("spots fitting r = 6.0 m (a house footprint): " + scan2.Fitting(6f).Count());
    			stringBuilder.AppendLine("spots fitting r = 3.4 m (the fountain)    : " + scan2.Fitting(3.4f).Count());
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("widest open regions in the village (greedy, spots further than");
    			stringBuilder.AppendLine("their clearance from each other so they are different places):");
    			List<SquarePlacement.Spot> list = new List<SquarePlacement.Spot>();
    			foreach (SquarePlacement.Spot s in scan2.Spots.OrderByDescending((SquarePlacement.Spot spot) => spot.Clearance))
    			{
    				if (list.Count >= 10)
    				{
    					break;
    				}
    				if (!list.Any((SquarePlacement.Spot p) =>
    				{
    					//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    					//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    					return Vector3.Distance(p.Point, s.Point) < s.Clearance + 1f;
    				}))
    				{
    					list.Add(s);
    				}
    			}
    			foreach (SquarePlacement.Spot item2 in list)
    			{
    				stringBuilder.AppendLine("   clearance " + item2.Clearance.ToString("F2").PadLeft(5) + " m  at " + ((object)item2.Point/*cast due to constrained. prefix*/).ToString().PadRight(30) + ((item2.Clearance >= 6f) ? "fits a house  " : "             ") + " nearest " + item2.Nearest);
    			}
    		}
    		Finish(stringBuilder);
    	}

    	private static float Median(SquarePlacement.Scan scan)
    	{
    		List<float> list = (from s in scan.Spots
    			select s.Clearance into v
    			orderby v
    			select v).ToList();
    		if (list.Count != 0)
    		{
    			return list[list.Count / 2];
    		}
    		return 0f;
    	}

    	private static void Map(StringBuilder sb, SquarePlacement.Scan scan, float step)
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
    		Bounds area = scan.Area;
    		int num = Mathf.CeilToInt(area.size.x / step) + 1;
    		int num2 = Mathf.CeilToInt(area.size.z / step) + 1;
    		float[,] array = new float[num, num2];
    		for (int i = 0; i < num; i++)
    		{
    			for (int j = 0; j < num2; j++)
    			{
    				array[i, j] = -1f;
    			}
    		}
    		int num3 = -1;
    		int num4 = -1;
    		foreach (SquarePlacement.Spot spot in scan.Spots)
    		{
    			int num5 = Mathf.RoundToInt((spot.Point.x - area.min.x) / step);
    			int num6 = Mathf.RoundToInt((spot.Point.z - area.min.z) / step);
    			if (num5 >= 0 && num6 >= 0 && num5 < num && num6 < num2)
    			{
    				array[num5, num6] = spot.Clearance;
    				if (scan.Best != null && spot.Point == scan.Best.Point)
    				{
    					num3 = num5;
    					num4 = num6;
    				}
    			}
    		}
    		sb.AppendLine();
    		sb.AppendLine("clearance map (x left->right, z bottom->top, cell " + step + " m)");
    		sb.AppendLine("  '#'<0.5 '+'<1.0 '-'<1.5 ':'<2.0 '.'<2.5 ','<3.0 ' '>=3.0  'O'=best  'X'=no ground");
    		sb.AppendLine();
    		StringBuilder stringBuilder = new StringBuilder("       ");
    		for (int k = 0; k < num; k += 2)
    		{
    			stringBuilder.Append((area.min.x + (float)k * step).ToString("F0").PadRight(2));
    		}
    		sb.AppendLine(stringBuilder.ToString());
    		for (int num7 = num2 - 1; num7 >= 0; num7--)
    		{
    			StringBuilder stringBuilder2 = new StringBuilder();
    			stringBuilder2.Append((area.min.z + (float)num7 * step).ToString("F0").PadLeft(5)).Append("  ");
    			for (int l = 0; l < num; l++)
    			{
    				float num8 = array[l, num7];
    				if (num8 < 0f)
    				{
    					stringBuilder2.Append('X');
    				}
    				else if (l == num3 && num7 == num4)
    				{
    					stringBuilder2.Append('O');
    				}
    				else
    				{
    					StringBuilder stringBuilder3 = stringBuilder2;
    					char value;
    					if (num8 < 0.5f)
    					{
    						value = '#';
    					}
    					else if (num8 < 1f)
    					{
    						value = '+';
    					}
    					else if (num8 < 1.5f)
    					{
    						value = '-';
    					}
    					else if (num8 < 2f)
    					{
    						value = ':';
    					}
    					else if (num8 < 2.5f)
    					{
    						value = '.';
    					}
    					else
    					{
    						value = ((num8 < 3f) ? ',' : ' ');
    					}
    					stringBuilder3.Append(value);
    				}
    			}
    			sb.AppendLine(stringBuilder2.ToString());
    		}
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), "Temp/square_clearance.txt")));
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/square_clearance.txt"), sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}