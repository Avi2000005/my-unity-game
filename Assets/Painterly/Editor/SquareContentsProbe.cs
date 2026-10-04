using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class SquareContentsProbe
    {
    	[MenuItem("Tools/Echoes/Probe Square Contents", priority = 97)]
    	public static void Run()
    	{
    		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
    		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
    		//IL_036b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
    		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0393: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		GameObject val = GameObject.Find("MarketSquare");
    		if ((Object)(object)val == (Object)null)
    		{
    			GameObject val2 = GameObject.Find("Village_Grey");
    			if ((Object)(object)val2 != (Object)null)
    			{
    				Transform val3 = val2.transform.Find("MarketSquare");
    				if ((Object)(object)val3 != (Object)null)
    				{
    					val = ((Component)val3).gameObject;
    				}
    			}
    		}
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] no MarketSquare found");
    			return;
    		}
    		stringBuilder.AppendLine($"MarketSquare at {val.transform.position}, " + $"{val.transform.childCount} children");
    		var array = val.GetComponentsInChildren<Renderer>(true).Select((Renderer r) =>
    		{
    			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
    			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
    			return new
    			{
    				r = r,
    				b = r.bounds,
    				scale = ((Component)r).transform.lossyScale
    			};
    		}).OrderByDescending(x =>
    		{
    			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    			Bounds b2 = x.b;
    			return b2.max.y;
    		})
    			.ToArray();
    		stringBuilder.AppendLine($"  {array.Length} renderer(s), tallest first\n");
    		stringBuilder.AppendLine("  model name                    world bounds size            maxY      lossyScale            parent");
    		var array2 = array;
    		Bounds b;
    		foreach (var anon in array2)
    		{
    			Transform parent = ((Component)anon.r).transform.parent;
    			string[] array3 = new string[5]
    			{
    				$"  {((Object)anon.r).name,-28} ",
    				null,
    				null,
    				null,
    				null
    			};
    			b = anon.b;
    			object arg = b.size.x;
    			b = anon.b;
    			object arg2 = b.size.y;
    			b = anon.b;
    			array3[1] = $"({arg,6:0.00},{arg2,6:0.00},{b.size.z,6:0.00})  ";
    			b = anon.b;
    			array3[2] = $"{b.max.y,8:0.00}  ";
    			array3[3] = $"({anon.scale.x:0.###},{anon.scale.y:0.###},{anon.scale.z:0.###})  ";
    			array3[4] = ((parent != null) ? ((Object)parent).name : null) ?? "<none>";
    			stringBuilder.AppendLine(string.Concat(array3));
    		}
    		stringBuilder.AppendLine("\n--- by model name, aggregated ---");
    		foreach (var item in from x in array
    			group x by ((Object)x.r).name into g
    			orderby g.Max(x =>
    			{
    				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    				//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    				Bounds b2 = x.b;
    				return b2.size.y;
    			}) descending
    			select g)
    		{
    			var anon2 = item.First();
    			string text = $"  {item.Key,-28} x{item.Count(),-3} ";
    			b = anon2.b;
    			stringBuilder.AppendLine(text + $"height {b.size.y,8:0.00}  " + $"lossyScale y {anon2.scale.y:0.###}");
    		}
    		var anon3 = array.First();
    		string name = ((Object)anon3.r).name;
    		b = anon3.b;
    		string text2 = $"\ntallest piece: '{name}' reaching y={b.max.y:0.00} ";
    		b = anon3.b;
    		stringBuilder.AppendLine(text2 + $"at size {b.size}");
    		stringBuilder.AppendLine("if that is a kerb or paver, anything above ~1 unit tall is the kit's 100x root scale leaking through — the houses are corrected for it by RootAt/SpawnCentered and these are not.");
    		File.WriteAllText("Temp/square_contents.txt", stringBuilder.ToString());
    		Debug.Log((object)("[Echoes] Square contents probe\n" + stringBuilder));
    	}
    }
}