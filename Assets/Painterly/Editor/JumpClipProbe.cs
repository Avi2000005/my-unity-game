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

    public static class JumpClipProbe
    {
    	private const string Report = "Temp/jump_clips.txt";

    	private static readonly string[] Candidates = new string[2] { "Assets/Art/Ari/Models/ari_jumping.fbx", "Assets/Art/Ari/Models/ari_running_Jump.fbx" };

    	private const string ExistingDir = "Assets/Art/Ari/Models";

    	public static void Run()
    	{
    		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] what is in the jump candidates?");
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("The two files have similar sizes, which is the whole problem:");
    		stringBuilder.AppendLine("a single hop and a run-up hop are the same rig and the same");
    		stringBuilder.AppendLine("textures, so size says nothing about which one is which.");
    		string[] candidates = Candidates;
    		foreach (string text in candidates)
    		{
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("--- " + text + " ---");
    			if (!File.Exists(text))
    			{
    				stringBuilder.AppendLine("NOT ON DISK.");
    				continue;
    			}
    			AssetImporter atPath = AssetImporter.GetAtPath(text);
    			ModelImporter val = (ModelImporter)(object)((atPath is ModelImporter) ? atPath : null);
    			if ((Object)(object)val == (Object)null)
    			{
    				stringBuilder.AppendLine("NOT IMPORTED YET (no ModelImporter). Refresh the asset database, then run again.");
    				continue;
    			}
    			FileInfo fileInfo = new FileInfo(text);
    			stringBuilder.AppendLine("size            : " + fileInfo.Length.ToString("N0") + " bytes");
    			stringBuilder.AppendLine("animationType   : " + ((object)val.animationType/*cast due to constrained. prefix*/).ToString() + "   (0 = None, 1 = Legacy, 2 = Generic, 3 = Humanoid)");
    			stringBuilder.AppendLine("importAnimation : " + val.importAnimation);
    			stringBuilder.AppendLine("avatarSetup     : " + ((object)val.avatarSetup/*cast due to constrained. prefix*/).ToString());
    			ModelImporterClipAnimation[] clipAnimations = val.clipAnimations;
    			if (clipAnimations != null && clipAnimations.Length != 0)
    			{
    				stringBuilder.AppendLine("clipAnimations  : " + clipAnimations.Length + " configured");
    				ModelImporterClipAnimation[] array = clipAnimations;
    				foreach (ModelImporterClipAnimation val2 in array)
    				{
    					stringBuilder.AppendLine("   '" + val2.name + "'  loop=" + val2.loopTime + "  in=" + val2.firstFrame.ToString("F3") + " out=" + val2.lastFrame.ToString("F3") + (val2.name.StartsWith("__") ? "   <- importer preview" : ""));
    				}
    			}
    			else
    			{
    				stringBuilder.AppendLine("clipAnimations  : none configured (importing whole file as one take)");
    			}
    			AnimationClip[] array2 = (from c in (from c in AssetDatabase.LoadAllAssetsAtPath(text).OfType<AnimationClip>()
    					where !c.legacy
    					select c).ToArray()
    				where !((Object)c).name.StartsWith("__")
    				select c).ToArray();
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("takes in the file: " + array2.Length);
    			foreach (AnimationClip item in array2.OrderBy((AnimationClip c) => c.length))
    			{
    				stringBuilder.AppendLine("   '" + ((Object)item).name + "'");
    				stringBuilder.AppendLine("        length      : " + item.length.ToString("F3") + " s");
    				stringBuilder.AppendLine("        frameRate   : " + item.frameRate.ToString("F2"));
    				stringBuilder.AppendLine("        looping     : " + ((Motion)item).isLooping);
    				stringBuilder.AppendLine("        humanMotion : " + item.humanMotion + "   <- " + HumanoidNote(item.humanMotion));
    				stringBuilder.AppendLine("        root drift  : " + RootDrift(item));
    			}
    			if (array2.Length == 0)
    			{
    				stringBuilder.AppendLine("   (no takes — importAnimation may be off, or the file is a model only)");
    			}
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- what Ari already has, for comparison ---");
    		AnimationClip[] array3 = (from c in ((IEnumerable<string>)AssetDatabase.FindAssets("t:AnimationClip", new string[1] { "Assets/Art/Ari/Models" })).Select((Func<string, string>)AssetDatabase.GUIDToAssetPath).Distinct().SelectMany((Func<string, IEnumerable<Object>>)AssetDatabase.LoadAllAssetsAtPath)
    				.OfType<AnimationClip>()
    			where !c.legacy && !((Object)c).name.StartsWith("__")
    			group c by ((Object)c).name into g
    			select g.First() into c
    			orderby ((Object)c).name
    			select c).ToArray();
    		stringBuilder.AppendLine("distinct clips in Assets/Art/Ari/Models: " + array3.Length);
    		AnimationClip[] array4 = array3;
    		foreach (AnimationClip val3 in array4)
    		{
    			stringBuilder.AppendLine("   '" + ((Object)val3).name + "'  " + val3.length.ToString("F2") + " s  loop=" + ((Motion)val3).isLooping + "  human=" + val3.humanMotion);
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("Ari_Idle and Ari_Walk are the two the controller needs today.");
    		stringBuilder.AppendLine("A jump should be a single take, under about 1.5 s, not looping.");
    		Finish(stringBuilder);
    	}

    	private static string HumanoidNote(bool human)
    	{
    		if (!human)
    		{
    			return "NOT mapped — plays but the mesh will not move";
    		}
    		return "mapped to the skeleton, so the mesh moves";
    	}

    	private static string RootDrift(AnimationClip clip)
    	{
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
    		try
    		{
    			AnimationClipSettings animationClipSettings = AnimationUtility.GetAnimationClipSettings(clip);
    			if (animationClipSettings != null && animationClipSettings.loopTime)
    			{
    				return "clip is looped, so drift is not meaningful";
    			}
    			EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);
    			if (curveBindings == null || curveBindings.Length == 0)
    			{
    				return "no curves";
    			}
    			float num = 0f;
    			EditorCurveBinding[] array = curveBindings;
    			for (int i = 0; i < array.Length; i++)
    			{
    				EditorCurveBinding val = array[i];
    				if (!(val.type != typeof(Animator)) && !(val.propertyName != "m_LocalPosition.x"))
    				{
    					AnimationCurve editorCurve = AnimationUtility.GetEditorCurve(clip, val);
    					if (editorCurve != null && editorCurve.length >= 2)
    					{
    						num += Mathf.Abs(editorCurve.keys[editorCurve.length - 1].value - editorCurve.keys[0].value);
    					}
    				}
    			}
    			return num.ToString("F3") + " m of root travel on X";
    		}
    		catch (Exception ex)
    		{
    			return "could not be read: " + ex.GetType().Name;
    		}
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), "Temp/jump_clips.txt")));
    		File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/jump_clips.txt"), sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}