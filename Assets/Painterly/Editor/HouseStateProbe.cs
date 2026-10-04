using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class HouseStateProbe
    {
    	private const string Report = "Temp/house_state.txt";

    	public static void Run()
    	{
    		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
    		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0245: Unknown result type (might be due to invalid IL or missing references)
    		//IL_024a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
    		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0290: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_037e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0383: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03be: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
    		//IL_040f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0419: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05b5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05ba: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05e1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05f5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_05fd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0602: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0629: Unknown result type (might be due to invalid IL or missing references)
    		//IL_062e: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] current state (read only, nothing saved)");
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
    		Vector3 position = val.transform.position;
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- the square ---");
    		stringBuilder.AppendLine("centre           : " + ((object)position/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine("paving extent    : " + ((object)val2.size/*cast due to constrained. prefix*/).ToString() + " at " + ((object)val2.center/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine("ground y         : " + groundY.ToString("F3"));
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- House_0_0 ---");
    		List<Transform> list = new List<Transform>();
    		Transform[] array = Object.FindObjectsByType<Transform>((FindObjectsInactive)1);
    		foreach (Transform val3 in array)
    		{
    			if (((Object)val3).name == "House_0_0")
    			{
    				list.Add(val3);
    			}
    		}
    		if (list.Count == 0)
    		{
    			stringBuilder.AppendLine("NOT FOUND anywhere in the scene.");
    		}
    		foreach (Transform item in list)
    		{
    			Bounds val4 = SquarePlacement.TopBounds(((Component)item).gameObject);
    			bool flag = val4.center.x > val2.min.x && val4.center.x < val2.max.x && val4.center.z > val2.min.z && val4.center.z < val2.max.z;
    			stringBuilder.AppendLine("path             : " + SquarePlacement.PathOf(item));
    			stringBuilder.AppendLine("position         : " + ((object)item.position/*cast due to constrained. prefix*/).ToString());
    			stringBuilder.AppendLine("yaw              : " + item.eulerAngles.y.ToString("F1"));
    			stringBuilder.AppendLine("world bounds     : " + ((object)val4.size/*cast due to constrained. prefix*/).ToString() + " at " + ((object)val4.center/*cast due to constrained. prefix*/).ToString());
    			stringBuilder.AppendLine("still on paving? : " + (flag ? "YES — still in the square" : "no, it has moved"));
    			stringBuilder.AppendLine("distance from centre : " + Vector2.Distance(new Vector2(val4.center.x - position.x, val4.center.z - position.z), Vector2.zero).ToString("F2") + " m");
    		}
    		GameObject val5 = GameObject.Find("Fountain");
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- the fountain ---");
    		if ((Object)(object)val5 == (Object)null)
    		{
    			stringBuilder.AppendLine("NOT FOUND.");
    		}
    		else
    		{
    			Bounds val6 = SquarePlacement.TopBounds(val5);
    			stringBuilder.AppendLine("position         : " + ((object)val5.transform.position/*cast due to constrained. prefix*/).ToString());
    			stringBuilder.AppendLine("world bounds     : " + ((object)val6.size/*cast due to constrained. prefix*/).ToString() + " at " + ((object)val6.center/*cast due to constrained. prefix*/).ToString());
    			stringBuilder.AppendLine("distance from centre : " + Vector2.Distance(new Vector2(val6.center.x - position.x, val6.center.z - position.z), Vector2.zero).ToString("F2") + " m");
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- the middle of the square ---");
    		SquarePlacement.Scan scan = SquarePlacement.MeasureSquare();
    		if (!scan.Ok)
    		{
    			stringBuilder.AppendLine("scan failed: " + scan.Why);
    			Finish(stringBuilder);
    			return;
    		}
    		List<float> list2 = (from s in scan.Spots
    			select s.Clearance into v
    			orderby v
    			select v).ToList();
    		stringBuilder.AppendLine("ground samples   : " + scan.Samples);
    		stringBuilder.AppendLine("median clearance : " + list2[list2.Count / 2].ToString("F2") + " m");
    		stringBuilder.AppendLine("max clearance    : " + scan.BestClearance.ToString("F2") + " m at " + ((object)scan.Best.Point/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine("spots fitting r=3.4 m (the fountain needs 3.39 m): " + scan.Fitting(3.4f).Count());
    		float num = 0f;
    		Vector3 val7 = Vector3.zero;
    		foreach (SquarePlacement.Spot spot in scan.Spots)
    		{
    			if (!(Vector2.Distance(new Vector2(spot.Point.x - position.x, spot.Point.z - position.z), Vector2.zero) > 2f) && spot.Clearance > num)
    			{
    				num = spot.Clearance;
    				val7 = spot.Point;
    			}
    		}
    		stringBuilder.AppendLine("best clearance within 2 m of the centre : " + num.ToString("F2") + " m at " + ((object)val7/*cast due to constrained. prefix*/).ToString());
    		stringBuilder.AppendLine();
    		if (scan.Fitting(3.4f).Count() > 0 && SquarePlacement.TryFind(scan, 3.39f, out var point, out var clearance, out var _))
    		{
    			stringBuilder.AppendLine("the fountain would now stand at " + ((object)point/*cast due to constrained. prefix*/).ToString() + " with " + clearance.ToString("F2") + " m clearance");
    		}
    		else
    		{
    			stringBuilder.AppendLine("the fountain STILL has nowhere to stand in the square");
    		}
    		Finish(stringBuilder);
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), "Temp/house_state.txt")));
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/house_state.txt"), sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}