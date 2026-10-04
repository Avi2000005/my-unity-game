using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class VillageColliders
    {
    	[MenuItem("Tools/Echoes/Add Village Colliders", priority = 66)]
    	public static void Run()
    	{
    		try
    		{
    			RunInner();
    		}
    		catch (Exception ex)
    		{
    			File.WriteAllText("Temp/colliders_all_error.txt", ex.ToString());
    			Debug.LogError((object)("[Echoes] village colliders failed\n" + ex));
    		}
    	}

    	private static void RunInner()
    	{
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			Debug.LogError((object)"[Echoes] stop play mode first; colliders added in play mode are discarded on exit.");
    			return;
    		}
    		StringBuilder stringBuilder = new StringBuilder();
    		Scene activeScene = SceneManager.GetActiveScene();
    		GameObject val = GameObject.Find("Village_Grey");
    		if ((Object)(object)val == (Object)null)
    		{
    			stringBuilder.AppendLine("No Village_Grey in the scene.");
    			File.WriteAllText("Temp/colliders_all.txt", stringBuilder.ToString());
    			return;
    		}
    		Renderer[] componentsInChildren = val.GetComponentsInChildren<Renderer>(true);
    		stringBuilder.AppendLine($"village renderers: {componentsInChildren.Length}");
    		int num = 0;
    		int num2 = 0;
    		int num3 = 0;
    		int num4 = 0;
    		List<string> list = new List<string>();
    		Renderer[] array = componentsInChildren;
    		foreach (Renderer val2 in array)
    		{
    			if ((Object)(object)val2 == (Object)null)
    			{
    				continue;
    			}
    			if ((Object)(object)((Component)val2).GetComponent<MeshCollider>() != (Object)null)
    			{
    				num2++;
    				continue;
    			}
    			if (val2 is SkinnedMeshRenderer)
    			{
    				num3++;
    				continue;
    			}
    			MeshFilter component = ((Component)val2).GetComponent<MeshFilter>();
    			if ((Object)(object)component == (Object)null || (Object)(object)component.sharedMesh == (Object)null)
    			{
    				num4++;
    				continue;
    			}
    			GameObjectUtility.SetStaticEditorFlags(((Component)val2).gameObject, (StaticEditorFlags)0);
    			try
    			{
    				((Component)val2).gameObject.AddComponent<MeshCollider>().sharedMesh = component.sharedMesh;
    				num++;
    			}
    			catch
    			{
    				num4++;
    				if (list.Count < 8)
    				{
    					list.Add(((Object)val2).name);
    				}
    			}
    		}
    		stringBuilder.AppendLine($"  added     {num}");
    		stringBuilder.AppendLine($"  already   {num2}");
    		stringBuilder.AppendLine($"  skinned   {num3}  (a MeshCollider is illegal on these)");
    		stringBuilder.AppendLine($"  no mesh   {num4}" + ((list.Count > 0) ? ("  e.g. " + string.Join(", ", list)) : ""));
    		GameObject val3 = GameObject.Find("Ari");
    		if ((Object)(object)val3 == (Object)null)
    		{
    			stringBuilder.AppendLine("WARNING: no Ari, so the ground layer mask was not touched.");
    		}
    		else if ((Object)(object)val3.GetComponent<AriMover>() == (Object)null)
    		{
    			stringBuilder.AppendLine("WARNING: Ari has no AriMover.");
    		}
    		else
    		{
    			stringBuilder.AppendLine("Ari's ground test rejects slopes under minGroundSlope, so the new wall/roof colliders cannot be stood on.");
    		}
    		stringBuilder.AppendLine("\ntotal colliders in scene: " + $"{Object.FindObjectsByType<MeshCollider>((FindObjectsInactive)0).Length}");
    		EditorSceneManager.MarkSceneDirty(activeScene);
    		string text = stringBuilder.ToString();
    		File.WriteAllText("Temp/colliders_all.txt", text);
    		Debug.Log((object)("[Echoes] Village colliders\n" + text));
    	}
    }
}