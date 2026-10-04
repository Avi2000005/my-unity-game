using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{

    public static class AriAnimatorFix
    {
    	private const string ControllerPath = "Assets/Art/Ari/Ari.controller";

    	private const string Report = "Temp/animator_fix.txt";

    	public static void Run()
    	{
    		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
    		//IL_033a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0346: Unknown result type (might be due to invalid IL or missing references)
    		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] Ari animator fix");
    		stringBuilder.AppendLine();
    		if (EditorApplication.isPlayingOrWillChangePlaymode)
    		{
    			stringBuilder.AppendLine("STOPPED: editor is in play mode.");
    			stringBuilder.AppendLine("  Exiting play mode discards this, so nothing was changed.");
    			stringBuilder.AppendLine("  Press Ctrl+P to exit play mode, then run Tools/Echoes/Fix Ari Animator.");
    			Finish(stringBuilder);
    			return;
    		}
    		RuntimeAnimatorController val = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Ari/Ari.controller");
    		if ((Object)(object)val == (Object)null)
    		{
    			stringBuilder.AppendLine("FATAL: no controller asset at Assets/Art/Ari/Ari.controller - run the builder first");
    			Finish(stringBuilder);
    			return;
    		}
    		stringBuilder.AppendLine("controller: '" + ((Object)val).name + "' (Assets/Art/Ari/Ari.controller)");
    		AnimationClip[] animationClips = val.animationClips;
    		foreach (AnimationClip val2 in animationClips)
    		{
    			stringBuilder.AppendLine($"  clip '{((Object)val2).name}' {val2.length:0.00}s human={((Motion)val2).isHumanMotion}");
    		}
    		stringBuilder.AppendLine();
    		GameObject val3 = FindAri();
    		if ((Object)(object)val3 == (Object)null)
    		{
    			stringBuilder.AppendLine("FATAL: could not find Ari (no object with both Animator and AriMover)");
    			Finish(stringBuilder);
    			return;
    		}
    		stringBuilder.AppendLine("Ari: " + Path(val3.transform));
    		Animator val4 = val3.GetComponent<Animator>();
    		if ((Object)(object)val4 == (Object)null)
    		{
    			val4 = val3.gameObject.AddComponent<Animator>();
    		}
    		if ((Object)(object)val4.avatar == (Object)null)
    		{
    			stringBuilder.AppendLine("  no avatar on Ari's Animator - copying from the model child");
    			Animator componentInChildren = val3.GetComponentInChildren<Animator>();
    			if ((Object)(object)componentInChildren != (Object)null && (Object)(object)componentInChildren.avatar != (Object)null)
    			{
    				val4.avatar = componentInChildren.avatar;
    				stringBuilder.AppendLine($"    avatar <- '{((Object)componentInChildren.avatar).name}' valid={val4.avatar.isValid} human={val4.avatar.isHuman}");
    			}
    		}
    		else
    		{
    			stringBuilder.AppendLine($"  avatar: valid={val4.avatar.isValid} human={val4.avatar.isHuman} '{((Object)val4.avatar).name}'");
    		}
    		if ((Object)(object)val4.runtimeAnimatorController != (Object)(object)val)
    		{
    			val4.runtimeAnimatorController = val;
    			stringBuilder.AppendLine("  controller ASSIGNED");
    		}
    		else
    		{
    			stringBuilder.AppendLine("  controller already correct");
    		}
    		if (val4.applyRootMotion)
    		{
    			val4.applyRootMotion = false;
    			stringBuilder.AppendLine("  applyRootMotion -> false");
    		}
    		if ((int)val4.cullingMode != 0)
    		{
    			val4.cullingMode = (AnimatorCullingMode)0;
    			stringBuilder.AppendLine("  cullingMode -> AlwaysAnimate");
    		}
    		if (!((Behaviour)val4).enabled)
    		{
    			((Behaviour)val4).enabled = true;
    			stringBuilder.AppendLine("  enabled -> true");
    		}
    		val4.updateMode = (AnimatorUpdateMode)0;
    		Animator[] componentsInChildren = val3.GetComponentsInChildren<Animator>(true);
    		foreach (Animator val5 in componentsInChildren)
    		{
    			if (!((Object)(object)val5 == (Object)(object)val4) && ((Behaviour)val5).enabled)
    			{
    				((Behaviour)val5).enabled = false;
    				stringBuilder.AppendLine("  disabled duplicate Animator on '" + ((Object)val5).name + "' (was controller=" + (((Object)(object)val5.runtimeAnimatorController == (Object)null) ? "null" : ((Object)val5.runtimeAnimatorController).name) + ")");
    			}
    		}
    		EditorUtility.SetDirty((Object)(object)((Component)val4).gameObject);
    		EditorSceneManager.MarkSceneDirty(val3.scene);
    		EditorSceneManager.SaveScene(val3.scene);
    		stringBuilder.AppendLine();
    		Scene scene = val3.scene;
    		stringBuilder.AppendLine("scene saved: " + scene.path);
    		Finish(stringBuilder);
    	}

    	private static GameObject FindAri()
    	{
    		return (from a in Object.FindObjectsByType<Animator>((FindObjectsInactive)1)
    			where (Object)(object)((Component)a).GetComponent<AriMover>() != (Object)null
    			select ((Component)a).gameObject).FirstOrDefault();
    	}

    	private static string Path(Transform t)
    	{
    		if ((Object)(object)t == (Object)null)
    		{
    			return "<none>";
    		}
    		StringBuilder stringBuilder = new StringBuilder(((Object)t).name);
    		Transform parent = t.parent;
    		while ((Object)(object)parent != (Object)null)
    		{
    			stringBuilder.Insert(0, ((Object)parent).name + "/");
    			parent = parent.parent;
    		}
    		return stringBuilder.ToString();
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		Directory.CreateDirectory(System.IO.Path.GetDirectoryName("Temp/animator_fix.txt"));
    		File.WriteAllText("Temp/animator_fix.txt", sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}