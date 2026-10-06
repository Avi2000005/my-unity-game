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

    public static class WalkableColliders
    {
    	[MenuItem("Tools/Echoes/Add Walkable Colliders", priority = 64)]
    	public static void Run()
    	{
    		try
    		{
    			RunInner();
    		}
    		catch (Exception ex)
    		{
    			File.WriteAllText("Temp/colliders_error.txt", ex.ToString());
    			Debug.LogError((object)("[Echoes] colliders failed\n" + ex));
    		}
    	}

    	private static void RunInner()
    	{
    		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			Debug.LogError((object)"[Echoes] stop play mode before adding colliders; they would be discarded on exit.");
    			return;
    		}
    		StringBuilder stringBuilder = new StringBuilder();
    		GameObject val = GameObject.Find("Village_Grey");
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] no Village_Grey in the scene");
    			return;
    		}
    		List<Renderer> list = new List<Renderer>();
    		Transform val2 = val.transform.Find("MarketSquare");
    		if ((Object)(object)val2 != (Object)null)
    		{
    			list.AddRange(from r in ((Component)val2).GetComponentsInChildren<Renderer>(true)
    				where ((Object)r).name.StartsWith("Floor_")
    				select r);
    		}
    		Transform val3 = val.transform.Find("Paths");
    		if ((Object)(object)val3 != (Object)null)
    		{
    			list.AddRange(((Component)val3).GetComponentsInChildren<Renderer>(true));
    		}
    		stringBuilder.AppendLine($"{list.Count} walkable renderer(s) found " + string.Format("({0} paving, ", (val2 != null) ? ((Component)val2).GetComponentsInChildren<Renderer>(true).Count((Renderer r) => ((Object)r).name.StartsWith("Floor_")) : 0) + $"{((val3 != null) ? ((Component)val3).GetComponentsInChildren<Renderer>(true).Length : 0)} path tiles)");
    		int num = 0;
    		int num2 = 0;
    		int num3 = 0;
    		SortedSet<float> sortedSet = new SortedSet<float>();
    		foreach (Renderer item in list)
    		{
    			if ((Object)(object)((Component)item).GetComponent<Collider>() != (Object)null)
    			{
    				num2++;
    				continue;
    			}
    			MeshCollider val4 = ((Component)item).gameObject.AddComponent<MeshCollider>();
    			object sharedMesh;
    			if (!(item is SkinnedMeshRenderer))
    			{
    				MeshFilter component = ((Component)item).GetComponent<MeshFilter>();
    				sharedMesh = ((component != null) ? component.sharedMesh : null);
    			}
    			else
    			{
    				sharedMesh = null;
    			}
    			val4.sharedMesh = (Mesh)sharedMesh;
    			if ((Object)(object)val4.sharedMesh == (Object)null)
    			{
    				Object.DestroyImmediate((Object)(object)val4);
    				num3++;
    				continue;
    			}
    			val4.convex = false;
    			((Collider)val4).isTrigger = false;
    			num++;
    			Bounds bounds = item.bounds;
    			sortedSet.Add(Mathf.Round(bounds.max.y * 1000f) / 1000f);
    		}
    		stringBuilder.AppendLine($"added={num} already had one={num2} no mesh={num3}");
    		stringBuilder.AppendLine("surface heights present: " + string.Join(", ", sortedSet));
    		stringBuilder.AppendLine((sortedSet.Count > 1) ? $"note: {sortedSet.Count} distinct heights, so he will step between them" : "note: one height only");
    		EditorUtility.SetDirty((Object)(object)val);
    		EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    		AssetDatabase.SaveAssets();
    		string text = stringBuilder.ToString();
    		File.WriteAllText("Temp/colliders.txt", text);
    		Debug.Log((object)("[Echoes] Walkable colliders\n" + text));
    	}
    }
}