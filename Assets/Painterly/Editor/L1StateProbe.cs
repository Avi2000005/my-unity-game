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

    public static class L1StateProbe
    {
    	private const string Dir = "Assets/Art/L1";

    	private const string Report = "Temp/l1_state.txt";

    	[MenuItem("Tools/Echoes/Probe Level 1 Cast", priority = 62)]
    	public static void Run()
    	{
    		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
    		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp/l1_state.txt");
    		if (File.Exists(path))
    		{
    			File.Delete(path);
    		}
    		string[] array = (from p in ((IEnumerable<string>)AssetDatabase.FindAssets("t:Model", new string[1] { "Assets/Art/L1" })).Select((Func<string, string>)AssetDatabase.GUIDToAssetPath)
    			where p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)
    			select p).OrderBy((string p) => p, StringComparer.Ordinal).ToArray();
    		stringBuilder.AppendLine(string.Format("{0} model(s) under {1}", array.Length, "Assets/Art/L1"));
    		string[] array2 = array;
    		foreach (string obj in array2)
    		{
    			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(obj);
    			bool flag = File.Exists(Path.Combine(path2: obj + ".meta", path1: Directory.GetCurrentDirectory()));
    			AssetImporter atPath = AssetImporter.GetAtPath(obj);
    			ModelImporter val = (ModelImporter)(object)((atPath is ModelImporter) ? atPath : null);
    			Object[] array3 = AssetDatabase.LoadAllAssetsAtPath(obj);
    			Avatar val2 = array3.OfType<Avatar>().FirstOrDefault();
    			AnimationClip[] array4 = (from c in array3.OfType<AnimationClip>()
    				where !((Object)c).name.StartsWith("__")
    				select c).ToArray();
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine(fileNameWithoutExtension ?? "");
    			stringBuilder.AppendLine("  meta on disk : " + (flag ? "yes" : "NO"));
    			if ((Object)(object)val == (Object)null)
    			{
    				stringBuilder.AppendLine("  importer     : NULL at rest  <-- not a reimport race");
    				stringBuilder.AppendLine($"  sub-assets   : {array3.Length} ({Describe(array3)})");
    				stringBuilder.AppendLine(string.Format("  clips on disk: {0} [{1}]", array4.Length, string.Join(", ", array4.Select((AnimationClip c) => ((Object)c).name))));
    				continue;
    			}
    			stringBuilder.AppendLine($"  importer     : {val.animationType}, avatarSetup={val.avatarSetup}, " + $"importAnimation={val.importAnimation}");
    			stringBuilder.AppendLine("  avatar       : " + (((Object)(object)val2 == (Object)null) ? "NONE" : $"valid={val2.isValid} human={val2.isHuman}"));
    			if (val != null)
    			{
    				ModelImporter val3 = val;
    				stringBuilder.AppendLine($"  configured   : {val3.clipAnimations.Length} clip(s) " + "[" + string.Join(", ", val3.clipAnimations.Select((ModelImporterClipAnimation c) => c.name)) + "]");
    			}
    			stringBuilder.AppendLine($"  takes        : {CountTakes(val)}");
    			stringBuilder.AppendLine(string.Format("  clips on disk: {0} [{1}]", array4.Length, string.Join(", ", array4.Select((AnimationClip c) => ((Object)c).name + " " + c.length.ToString("0.00") + "s"))));
    		}
    		Debug.Log((object)("[Echoes] L1 state\n" + stringBuilder));
    		File.WriteAllText(path, stringBuilder.ToString());
    	}

    	private static string Describe(Object[] all)
    	{
    		if (all.Length == 0)
    		{
    			return "none";
    		}
    		return string.Join(", ", all.Select((Object a) => (!(a == (Object)null)) ? ((object)a).GetType().Name : "null"));
    	}

    	private static int CountTakes(ModelImporter mi)
    	{
    		int num = 0;
    		try
    		{
    			return mi.defaultClipAnimations.Length;
    		}
    		catch (Exception)
    		{
    			return -1;
    		}
    	}
    }
}