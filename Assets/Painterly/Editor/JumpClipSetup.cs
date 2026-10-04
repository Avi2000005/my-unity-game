using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class JumpClipSetup
    {
    	private sealed class Candidate
    	{
    		public string File;

    		public string ProbeName;

    		public string Path;

    		public float Length;

    		public bool Human;

    		public bool Imported;

    		public string Failure;

    		public string BoneUsed;

    		public string BoneRanges = "";

    		public float PoseDelta;

    		public float[] Arc = Array.Empty<float>();

    		public int Arcs;

    		public float Min;

    		public float Max;

    		public float Range;

    		public float ApexFraction;
    	}

    	private const string Dir = "Assets/Art/Ari/Models";

    	private const string Report = "Temp/jump_setup.txt";

    	private const string OwnTake = "mixamo.com";

    	public const string FinalName = "Ari_Jump";

    	private const float ArcProminence = 0.03f;

    	private const int Samples = 200;

    	private static readonly (string File, string Probe)[] Candidates = new (string, string)[2]
    	{
    		("ari_running_Jump", "Ari_Jump_RJ"),
    		("ari_jumping", "Ari_Jump_J")
    	};

    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] which of the two jump files is a jump?");
    		stringBuilder.AppendLine();
    		AssetDatabase.Refresh((ImportAssetOptions)9);
    		List<Candidate> list = new List<Candidate>();
    		(string, string)[] candidates = Candidates;
    		for (int i = 0; i < candidates.Length; i++)
    		{
    			(string, string) tuple = candidates[i];
    			string item = tuple.Item1;
    			string item2 = tuple.Item2;
    			Candidate candidate = new Candidate
    			{
    				File = item,
    				ProbeName = item2,
    				Path = "Assets/Art/Ari/Models/" + item + ".fbx"
    			};
    			ImportAsHumanoid(candidate, stringBuilder);
    			if (!candidate.Imported)
    			{
    				list.Add(candidate);
    				continue;
    			}
    			Measure(candidate, stringBuilder);
    			list.Add(candidate);
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- arc of each candidate's hips ---");
    		foreach (Candidate item3 in list)
    		{
    			if (item3.Failure != null)
    			{
    				stringBuilder.AppendLine(item3.File + ": " + item3.Failure);
    				continue;
    			}
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine(item3.File);
    			stringBuilder.AppendLine("  clip '" + item3.ProbeName + "'  " + item3.Length.ToString("F3") + " s  humanoid-mapped=" + item3.Human);
    			stringBuilder.AppendLine("  measured on    : " + item3.BoneUsed + "   (all bones: " + item3.BoneRanges + ")");
    			stringBuilder.AppendLine("  pose actually moves by " + item3.PoseDelta.ToString("F0") + " deg, so the clip reached the rig");
    			stringBuilder.AppendLine("  vertical range : " + item3.Range.ToString("F3") + " m  (low " + item3.Min.ToString("F3") + ", high " + item3.Max.ToString("F3") + ")");
    			stringBuilder.AppendLine("  hops           : " + item3.Arcs);
    			stringBuilder.AppendLine("  apex at        : " + (item3.ApexFraction * 100f).ToString("F0") + "% of the clip");
    			stringBuilder.AppendLine("  shape          : " + Sparkline(item3.Arc));
    		}
    		List<Candidate> list2 = list.Where((Candidate c) => c.Failure == null).ToList();
    		if (list2.Count == 0)
    		{
    			stringBuilder.AppendLine();
    			stringBuilder.AppendLine("Neither candidate imported. Nothing changed in the controller.");
    			Finish(stringBuilder);
    			return;
    		}
    		Candidate winner = (from c in list2
    			orderby c.Arcs, (c.Range < 0.05f) ? 1 : 0, c.Length
    			select c).First();
    		List<Candidate> list3 = list2.Where((Candidate c) => c != winner).ToList();
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- decision ---");
    		stringBuilder.AppendLine("rule: fewest arcs, then any real vertical travel, then shortest.");
    		stringBuilder.AppendLine("winner: " + winner.File + "  (" + winner.Arcs + " arc, " + winner.Range.ToString("F3") + " m, " + winner.Length.ToString("F3") + " s)");
    		Rename(winner.Path, winner.ProbeName, "Ari_Jump", stringBuilder);
    		foreach (Candidate item4 in list3)
    		{
    			EmptyClips(item4.Path, stringBuilder);
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- end state, read back from the project ---");
    		foreach (Candidate item5 in list)
    		{
    			stringBuilder.AppendLine(item5.File + " : " + LandedClips(item5.Path));
    		}
    		string[] value = (from x in AssetDatabase.LoadAllAssetsAtPath(winner.Path).OfType<AnimationClip>()
    			where !x.legacy
    			select ((Object)x).name).ToArray();
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("clips on the winner: " + string.Join(", ", value));
    		stringBuilder.AppendLine("Ari_Jump is what the controller needs as a third state.");
    		AnimatorController val = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Art/Ari/Ari.controller");
    		if ((Object)(object)val == (Object)null)
    		{
    			stringBuilder.AppendLine("Ari.controller does not exist yet — run Tools/Echoes/Build Ari Controller.");
    		}
    		else
    		{
    			AnimatorState val2 = val.layers[0].stateMachine.states.Select((ChildAnimatorState c) => c.state).FirstOrDefault((AnimatorState s) => (Object)(object)s.motion != (Object)null && ((Object)s.motion).name == "Ari_Jump");
    			stringBuilder.AppendLine(((Object)(object)val2 == (Object)null) ? "Ari.controller exists but has no Ari_Jump state — run Tools/Echoes/Build Ari Controller." : ("Ari.controller already has the " + ((Object)val2).name + " state. Nothing to do."));
    		}
    		AnimationClip val3 = AssetDatabase.LoadAllAssetsAtPath(winner.Path).OfType<AnimationClip>().FirstOrDefault((AnimationClip x) => ((Object)x).name == "Ari_Jump");
    		stringBuilder.AppendLine("on disk: " + (((Object)(object)val3 == (Object)null) ? "NOT FOUND" : ("Ari_Jump is " + val3.length.ToString("F3") + " s, isLooping=" + ((Motion)val3).isLooping + (((Motion)val3).isLooping ? "  <- WRONG, a looping hop crouches forever" : "  <- right, a hop is a one-shot"))));
    		foreach (Candidate item6 in list3)
    		{
    			stringBuilder.AppendLine("unused file left in place (clips emptied): " + item6.Path + "  — the source is still at C:\\Users\\chate\\Documents\\" + item6.File + ".fbx, and AssetDatabase.DeleteAsset will drop it.");
    		}
    		Finish(stringBuilder);
    	}

    	private static void ImportAsHumanoid(Candidate c, StringBuilder sb)
    	{
    		if ((Object)(object)AssetImporter.GetAtPath(c.Path) == (Object)null)
    		{
    			AssetDatabase.ImportAsset(c.Path, (ImportAssetOptions)9);
    		}
    		AssetImporter atPath = AssetImporter.GetAtPath(c.Path);
    		ModelImporter val = (ModelImporter)(object)((atPath is ModelImporter) ? atPath : null);
    		if ((Object)(object)val == (Object)null)
    		{
    			c.Failure = "did not import; is the file in the project?";
    			return;
    		}
    		val.animationType = (ModelImporterAnimationType)3;
    		val.avatarSetup = (ModelImporterAvatarSetup)1;
    		val.sourceAvatar = null;
    		val.materialImportMode = (ModelImporterMaterialImportMode)0;
    		val.importAnimation = true;
    		val.animationCompression = (ModelImporterAnimationCompression)3;
    		val.addCollider = false;
    		val.isReadable = false;
    		((AssetImporter)val).SaveAndReimport();
    		AssetImporter atPath2 = AssetImporter.GetAtPath(c.Path);
    		val = (ModelImporter)(object)((atPath2 is ModelImporter) ? atPath2 : null);
    		if ((Object)(object)val == (Object)null)
    		{
    			c.Failure = "lost the importer after reimport";
    			return;
    		}
    		ModelImporterClipAnimation[] defaultClipAnimations = val.defaultClipAnimations;
    		ModelImporterClipAnimation val2 = defaultClipAnimations.FirstOrDefault((ModelImporterClipAnimation x) => x.takeName == "mixamo.com");
    		if (val2 == null)
    		{
    			c.Failure = "take 'mixamo.com' not found; takes are " + ((defaultClipAnimations.Length == 0) ? "none" : string.Join(", ", defaultClipAnimations.Select((ModelImporterClipAnimation x) => x.takeName)));
    			return;
    		}
    		val2.name = c.ProbeName;
    		val2.loopTime = false;
    		val2.loopPose = false;
    		val.clipAnimations = new ModelImporterClipAnimation[1] { val2 };
    		((AssetImporter)val).SaveAndReimport();
    		AnimationClip val3 = AssetDatabase.LoadAllAssetsAtPath(c.Path).OfType<AnimationClip>().FirstOrDefault((AnimationClip x) => ((Object)x).name == c.ProbeName);
    		if ((Object)(object)val3 == (Object)null)
    		{
    			c.Failure = "clip missing after rename; clips are " + string.Join(", ", from x in AssetDatabase.LoadAllAssetsAtPath(c.Path).OfType<AnimationClip>()
    				select "'" + ((Object)x).name + "'");
    			return;
    		}
    		c.Length = val3.length;
    		c.Human = val3.humanMotion;
    		c.Imported = true;
    		sb.AppendLine(c.File + ": imported as Humanoid, '" + c.ProbeName + "' " + c.Length.ToString("F3") + " s, mapped to the skeleton=" + c.Human);
    	}

    	private static void Measure(Candidate c, StringBuilder sb)
    	{
    		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0262: Unknown result type (might be due to invalid IL or missing references)
    		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
    		GameObject val = AssetDatabase.LoadAssetAtPath<GameObject>(c.Path);
    		if ((Object)(object)val == (Object)null)
    		{
    			c.Failure = "no GameObject at " + c.Path;
    			return;
    		}
    		AnimationClip val2 = AssetDatabase.LoadAllAssetsAtPath(c.Path).OfType<AnimationClip>().FirstOrDefault((AnimationClip x) => ((Object)x).name == c.ProbeName);
    		if ((Object)(object)val2 == (Object)null)
    		{
    			c.Failure = "probe clip vanished";
    			return;
    		}
    		GameObject val3 = Object.Instantiate<GameObject>(val);
    		try
    		{
    			Animator componentInChildren = val3.GetComponentInChildren<Animator>();
    			if ((Object)(object)componentInChildren == (Object)null)
    			{
    				c.Failure = "no Animator on the imported model";
    				return;
    			}
    			List<(string, Transform)> list = new List<(string, Transform)>();
    			AddBone(list, componentInChildren, "Hips", (HumanBodyBones)0);
    			AddBone(list, componentInChildren, "Spine", (HumanBodyBones)7);
    			AddBone(list, componentInChildren, "LeftFoot", (HumanBodyBones)5);
    			AddBone(list, componentInChildren, "RightFoot", (HumanBodyBones)6);
    			AddBone(list, componentInChildren, "Head", (HumanBodyBones)10);
    			if (list.Count == 0)
    			{
    				c.Failure = "no bones mapped at all, so the rig cannot be sampled";
    				return;
    			}
    			Dictionary<string, float[]> series = new Dictionary<string, float[]>();
    			Dictionary<string, Quaternion> dictionary = new Dictionary<string, Quaternion>();
    			foreach (var item in list)
    			{
    				series[item.Item1] = new float[200];
    				dictionary[item.Item1] = item.Item2.localRotation;
    			}
    			for (int num = 0; num < 200; num++)
    			{
    				float num2 = (float)num / 199f * val2.length;
    				val2.SampleAnimation(val3, num2);
    				foreach (var item2 in list)
    				{
    					series[item2.Item1][num] = item2.Item2.position.y;
    				}
    			}
    			val2.SampleAnimation(val3, val2.length * 0.5f);
    			float num3 = 0f;
    			foreach (var item3 in list)
    			{
    				num3 = Mathf.Max(num3, Quaternion.Angle(dictionary[item3.Item1], item3.Item2.localRotation));
    			}
    			if (num3 < 0.5f)
    			{
    				c.Failure = "the clip was sampled but no bone moved (max pose change " + num3.ToString("F2") + " deg) — SampleAnimation cannot reach this humanoid clip, so its arc cannot be read this way";
    				return;
    			}
    			c.PoseDelta = num3;
    			(string, float[], float) tuple = (from x in list.Select(((string Name, Transform Bone) w) => (Name: w.Name, Arc: series[w.Name], Range: RangeOf(series[w.Name])))
    				orderby x.Range descending
    				select x).First();
    			c.Arc = tuple.Item2;
    			c.BoneUsed = tuple.Item1;
    			c.BoneRanges = string.Join(", ", list.Select(((string Name, Transform Bone) w) => w.Name + " " + RangeOf(series[w.Name]).ToString("F3") + " m"));
    		}
    		finally
    		{
    			Object.DestroyImmediate((Object)(object)val3);
    		}
    		c.Min = c.Arc.Min();
    		c.Max = c.Arc.Max();
    		c.Range = c.Max - c.Min;
    		c.Arcs = CountArcs(c.Arc, 0.03f);
    		c.ApexFraction = (float)ArrayIndexOfMax(c.Arc) / 199f;
    	}

    	private static void AddBone(List<(string, Transform)> into, Animator anim, string name, HumanBodyBones bone)
    	{
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		if (!((Object)(object)anim == (Object)null))
    		{
    			Transform boneTransform = anim.GetBoneTransform(bone);
    			if ((Object)(object)boneTransform != (Object)null)
    			{
    				into.Add((name, boneTransform));
    			}
    		}
    	}

    	private static float RangeOf(float[] v)
    	{
    		return v.Max() - v.Min();
    	}

    	private static int ArrayIndexOfMax(float[] v)
    	{
    		int num = 0;
    		for (int i = 1; i < v.Length; i++)
    		{
    			if (v[i] > v[num])
    			{
    				num = i;
    			}
    		}
    		return num;
    	}

    	private static int CountArcs(float[] v, float prominence)
    	{
    		int num = 0;
    		int num2 = 0;
    		int num3 = 0;
    		for (int i = 1; i < v.Length; i++)
    		{
    			float num4 = v[i] - v[i - 1];
    			if (!(Mathf.Abs(num4) < 0.0001f))
    			{
    				int num5 = ((num4 > 0f) ? 1 : (-1));
    				if (num3 != 0 && num5 != num3 && Mathf.Abs(v[i - 1] - v[num2]) >= prominence)
    				{
    					num++;
    					num2 = i - 1;
    				}
    				num3 = num5;
    			}
    		}
    		return num;
    	}

    	private static string Sparkline(float[] v)
    	{
    		if (v == null || v.Length == 0)
    		{
    			return "(none)";
    		}
    		float num = v.Min();
    		float num2 = v.Max() - num;
    		if (num2 < 0.0001f)
    		{
    			return "flat, no vertical movement at all";
    		}
    		StringBuilder stringBuilder = new StringBuilder();
    		int num3 = 40;
    		for (int i = 0; i < num3; i++)
    		{
    			int num4 = i * v.Length / num3;
    			int num5 = Mathf.Max(num4 + 1, (i + 1) * v.Length / num3);
    			float num6 = 0f;
    			for (int j = num4; j < num5 && j < v.Length; j++)
    			{
    				num6 += v[j];
    			}
    			num6 /= (float)(num5 - num4);
    			int index = Mathf.Clamp(Mathf.RoundToInt((num6 - num) / num2 * (float)(" .:-=+*#%@".Length - 1)), 0, " .:-=+*#%@".Length - 1);
    			stringBuilder.Append(" .:-=+*#%@"[index]);
    		}
    		return stringBuilder.ToString();
    	}

    	private static void Rename(string path, string from, string to, StringBuilder sb)
    	{
    		AssetImporter atPath = AssetImporter.GetAtPath(path);
    		ModelImporter val = (ModelImporter)(object)((atPath is ModelImporter) ? atPath : null);
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("rename failed, no importer: " + path);
    			return;
    		}
    		ModelImporterClipAnimation[] clipAnimations = val.clipAnimations;
    		if (clipAnimations == null || clipAnimations.Length == 0)
    		{
    			sb.AppendLine("rename failed, no configured clips: " + path);
    			return;
    		}
    		bool flag = false;
    		ModelImporterClipAnimation[] array = clipAnimations;
    		foreach (ModelImporterClipAnimation val2 in array)
    		{
    			if (val2.name == from)
    			{
    				val2.name = to;
    				flag = true;
    			}
    		}
    		if (!flag)
    		{
    			sb.AppendLine("nothing to rename on " + path + "; clips are " + string.Join(", ", clipAnimations.Select((ModelImporterClipAnimation x) => "'" + x.name + "'")));
    			return;
    		}
    		val.clipAnimations = clipAnimations;
    		((AssetImporter)val).SaveAndReimport();
    		AssetImporter atPath2 = AssetImporter.GetAtPath(path);
    		AssetImporter obj = ((atPath2 is ModelImporter) ? atPath2 : null);
    		ModelImporterClipAnimation[] array2 = ((obj != null) ? ((ModelImporter)obj).clipAnimations : null);
    		if (array2 != null && array2.Any((ModelImporterClipAnimation x) => x.name == to))
    		{
    			sb.AppendLine("renamed '" + from + "' -> '" + to + "' and verified on disk");
    		}
    		else
    		{
    			sb.AppendLine("RENAME DID NOT STICK on " + path + "; clips are now " + ((array2 == null || array2.Length == 0) ? "none" : string.Join(", ", array2.Select((ModelImporterClipAnimation x) => "'" + x.name + "'"))));
    		}
    	}

    	private static void EmptyClips(string path, StringBuilder sb)
    	{
    		AssetImporter atPath = AssetImporter.GetAtPath(path);
    		ModelImporter val = (ModelImporter)(object)((atPath is ModelImporter) ? atPath : null);
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine("could not empty " + path);
    			return;
    		}
    		val.importAnimation = false;
    		((AssetImporter)val).SaveAndReimport();
    		AnimationClip[] array = (from x in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
    			where !x.legacy && !((Object)x).name.StartsWith("__")
    			select x).ToArray();
    		sb.AppendLine((array.Length == 0) ? (path + ": animation import switched off, verified — no clips land") : (path + ": STILL HAS CLIPS -> " + string.Join(", ", array.Select((AnimationClip x) => "'" + ((Object)x).name + "'"))));
    	}

    	private static string LandedClips(string path)
    	{
    		string[] array = (from x in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
    			where !x.legacy && !((Object)x).name.StartsWith("__")
    			select ((Object)x).name into x
    			orderby x
    			select x).ToArray();
    		if (array.Length != 0)
    		{
    			return string.Join(", ", array.Select((string x) => "'" + x + "'"));
    		}
    		return "NOTHING (file is inert)";
    	}

    	private static void Finish(StringBuilder sb)
    	{
    		string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp/jump_setup.txt");
    		Directory.CreateDirectory(Path.GetDirectoryName(path));
    		File.WriteAllText(path, sb.ToString());
    		Debug.Log((object)sb.ToString());
    	}
    }
}