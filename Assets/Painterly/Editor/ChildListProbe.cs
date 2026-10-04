using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class ChildListProbe
    {
    	[MenuItem("Tools/Echoes/Probe Children", priority = 98)]
    	public static void Run()
    	{
    		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		string[] array = new string[2] { "Village_Grey", "MarketSquare" };
    		foreach (string text in array)
    		{
    			GameObject val = GameObject.Find(text);
    			if ((Object)(object)val == (Object)null)
    			{
    				GameObject val2 = GameObject.Find("Village_Grey");
    				if ((Object)(object)val2 != (Object)null)
    				{
    					Transform val3 = val2.transform.Find(text);
    					if ((Object)(object)val3 != (Object)null)
    					{
    						val = ((Component)val3).gameObject;
    					}
    				}
    			}
    			if ((Object)(object)val == (Object)null)
    			{
    				stringBuilder.AppendLine(text + ": NOT FOUND");
    				continue;
    			}
    			Transform[] array2 = ((IEnumerable)val.transform).Cast<Transform>().ToArray();
    			stringBuilder.AppendLine($"{text}: {array2.Length} direct children, " + $"{val.GetComponentsInChildren<Renderer>(true).Length} renderers total");
    			Transform[] array3 = array2;
    			foreach (Transform val4 in array3)
    			{
    				int num = ((Component)val4).GetComponentsInChildren<Renderer>(true).Length;
    				stringBuilder.AppendLine($"  {((Object)val4).name,-30} childCount={val4.childCount,-4} " + $"renderers={num,-4} localPos={val4.localPosition} " + $"active={((Component)val4).gameObject.activeSelf}");
    			}
    			stringBuilder.AppendLine();
    		}
    		File.WriteAllText("Temp/children.txt", stringBuilder.ToString());
    		Debug.Log((object)("[Echoes] Child list probe\n" + stringBuilder));
    	}
    }
}