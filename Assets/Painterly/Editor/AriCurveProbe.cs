using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class AriCurveProbe
    {
    	private const string Dir = "Assets/Art/Ari/Models";

    	[MenuItem("Tools/Echoes/Probe Ari Curves", priority = 92)]
    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		string[] array = new string[3] { "Ari_character", "Idle", "Walking" };
    		foreach (string text in array)
    		{
    			string text2 = "Assets/Art/Ari/Models/" + text + ".fbx";
    			stringBuilder.AppendLine("=== " + text + " ===");
    			AnimationClip[] array2 = (from c in AssetDatabase.LoadAllAssetsAtPath(text2).OfType<AnimationClip>()
    				where !((Object)c).name.StartsWith("__preview__")
    				select c).ToArray();
    			foreach (AnimationClip val in array2)
    			{
    				EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(val);
    				stringBuilder.AppendLine($"  '{((Object)val).name}' len={val.length:0.000}s " + $"bindings={curveBindings.Length} humanMotion=" + $"{val.humanMotion}");
    				foreach (IGrouping<string, EditorCurveBinding> item in from g in curveBindings.GroupBy((EditorCurveBinding b) =>
    					{
    						//IL_0011: Unknown result type (might be due to invalid IL or missing references)
    						return b.type.Name + " . " + b.propertyName;
    					})
    					orderby g.Count() descending
    					select g)
    				{
    					string arg = string.Join(", ", item.Take(3).Select((EditorCurveBinding b) =>
    					{
    						//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    						return b.path ?? "<root>";
    					}));
    					stringBuilder.AppendLine($"      {item.Count(),5}x  {item.Key}   e.g. {arg}");
    				}
    				stringBuilder.AppendLine();
    			}
    		}
    		File.WriteAllText("Temp/ari_curves.txt", stringBuilder.ToString());
    		Debug.Log((object)("[Echoes] Ari curve probe -> Temp/ari_curves.txt\n" + stringBuilder));
    	}
    }
}