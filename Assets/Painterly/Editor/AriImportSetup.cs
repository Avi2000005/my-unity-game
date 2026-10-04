using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{

    public static class AriImportSetup
    {
    	private const string Dir = "Assets/Art/Ari/Models";

    	private const string Character = "Ari_character";

    	private const string OwnTake = "mixamo.com";

    	private static readonly (string File, string ClipName, bool OwnAvatar, bool Loop)[] Animations = new (string, string, bool, bool)[5]
    	{
    		("Idle3", "Ari_Idle", true, true),
    		("Walking2", "Ari_Walk", true, true),
    		("ari_running_Jump", "Ari_Jump", true, false),
    		("Idle2", "Ari_Idle_Alt", true, true),
    		("Walking", "Ari_Walk_Alt", false, true)
    	};

    	[MenuItem("Tools/Echoes/Import Ari", priority = 60)]
    	public static void Run()
    	{
    		StringBuilder stringBuilder = new StringBuilder();
    		AssetDatabase.Refresh((ImportAssetOptions)9);
    		string text = "Assets/Art/Ari/Models/Ari_character.fbx";
    		ImportIfNew(text);
    		AssetImporter atPath = AssetImporter.GetAtPath(text);
    		ModelImporter val = (ModelImporter)(object)((atPath is ModelImporter) ? atPath : null);
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogError((object)("[Echoes] " + text + " did not import. Is the file in the project?"));
    			return;
    		}
    		Avatar val2 = AssetDatabase.LoadAllAssetsAtPath(text).OfType<Avatar>().FirstOrDefault();
    		if ((Object)(object)val2 != (Object)null && (!val2.isValid || !val2.isHuman))
    		{
    			stringBuilder.AppendLine("existing avatar is unusable " + $"(valid={val2.isValid} human={val2.isHuman}); " + "discarding the cached mapping and re-running the mapper");
    			val.animationType = (ModelImporterAnimationType)2;
    			((AssetImporter)val).SaveAndReimport();
    			AssetImporter atPath2 = AssetImporter.GetAtPath(text);
    			val = (ModelImporter)(object)((atPath2 is ModelImporter) ? atPath2 : null);
    			if ((Object)(object)val == (Object)null)
    			{
    				Debug.LogError((object)("[Echoes] lost the importer while resetting " + text));
    				return;
    			}
    		}
    		val.animationType = (ModelImporterAnimationType)3;
    		val.avatarSetup = (ModelImporterAvatarSetup)1;
    		val.importAnimation = false;
    		val.materialImportMode = (ModelImporterMaterialImportMode)1;
    		val.addCollider = false;
    		val.isReadable = false;
    		((AssetImporter)val).SaveAndReimport();
    		AssetImporter atPath3 = AssetImporter.GetAtPath(text);
    		ReportAvatarBones((ModelImporter)(object)((atPath3 is ModelImporter) ? atPath3 : null), stringBuilder);
    		Avatar val3 = AssetDatabase.LoadAllAssetsAtPath(text).OfType<Avatar>().FirstOrDefault();
    		if ((Object)(object)val3 == (Object)null || !val3.isValid || !val3.isHuman)
    		{
    			Debug.LogError((object)("[Echoes] " + text + " produced no usable humanoid avatar. The animations cannot retarget onto it.\n" + stringBuilder));
    			return;
    		}
    		stringBuilder.AppendLine($"character avatar: valid={val3.isValid} human={val3.isHuman}");
    		(string, string, bool, bool)[] animations = Animations;
    		for (int i = 0; i < animations.Length; i++)
    		{
    			(string, string, bool, bool) tuple = animations[i];
    			string item = tuple.Item1;
    			string item2 = tuple.Item2;
    			bool item3 = tuple.Item3;
    			bool item4 = tuple.Item4;
    			string text2 = "Assets/Art/Ari/Models/" + item + ".fbx";
    			ImportIfNew(text2);
    			AssetImporter atPath4 = AssetImporter.GetAtPath(text2);
    			ModelImporter val4 = (ModelImporter)(object)((atPath4 is ModelImporter) ? atPath4 : null);
    			if ((Object)(object)val4 == (Object)null)
    			{
    				stringBuilder.AppendLine(item + ": MISSING");
    				continue;
    			}
    			val4.animationType = (ModelImporterAnimationType)3;
    			if (item3)
    			{
    				val4.avatarSetup = (ModelImporterAvatarSetup)1;
    				val4.sourceAvatar = null;
    			}
    			else
    			{
    				val4.avatarSetup = (ModelImporterAvatarSetup)2;
    				val4.sourceAvatar = val3;
    			}
    			val4.materialImportMode = (ModelImporterMaterialImportMode)0;
    			val4.importAnimation = true;
    			val4.animationCompression = (ModelImporterAnimationCompression)3;
    			val4.addCollider = false;
    			val4.isReadable = false;
    			((AssetImporter)val4).SaveAndReimport();
    			AssetImporter atPath5 = AssetImporter.GetAtPath(text2);
    			val4 = (ModelImporter)(object)((atPath5 is ModelImporter) ? atPath5 : null);
    			if ((Object)(object)val4 == (Object)null)
    			{
    				stringBuilder.AppendLine(item + ": lost the importer after reimport");
    				continue;
    			}
    			ModelImporterClipAnimation val5 = val4.defaultClipAnimations.FirstOrDefault((ModelImporterClipAnimation c) => c.takeName == "mixamo.com");
    			if (val5 == null)
    			{
    				stringBuilder.AppendLine(item + ": take 'mixamo.com' not found; takes are " + string.Join(", ", val4.defaultClipAnimations.Select((ModelImporterClipAnimation c) => c.takeName)));
    				val4.clipAnimations = new ModelImporterClipAnimation[0];
    			}
    			else
    			{
    				val5.name = item2;
    				val5.loopTime = item4;
    				val5.loopPose = item4;
    				val4.clipAnimations = new ModelImporterClipAnimation[1] { val5 };
    			}
    			((AssetImporter)val4).SaveAndReimport();
    			AnimationClip[] array = (from c in AssetDatabase.LoadAllAssetsAtPath(text2).OfType<AnimationClip>()
    				where !((Object)c).name.StartsWith("__")
    				select c).ToArray();
    			stringBuilder.AppendLine($"{item}: {array.Length} clip(s) " + string.Join(", ", array.Select((AnimationClip c) => $"'{((Object)c).name}' {c.length:0.00}s loop={((Motion)c).isLooping}")));
    		}
    		RenameLegacy("Assets/Art/Ari/Models/Idle.fbx", "Ari_Idle", "Ari_Idle_Legacy", stringBuilder);
    		Debug.Log((object)("[Echoes] Ari import configured\n" + stringBuilder));
    	}

    	private static void RenameLegacy(string path, string from, string to, StringBuilder sb)
    	{
    		AssetImporter atPath = AssetImporter.GetAtPath(path);
    		ModelImporter val = (ModelImporter)(object)((atPath is ModelImporter) ? atPath : null);
    		if ((Object)(object)val == (Object)null)
    		{
    			sb.AppendLine(path + ": no legacy clip to rename");
    			return;
    		}
    		ModelImporterClipAnimation[] clipAnimations = val.clipAnimations;
    		if (clipAnimations == null || clipAnimations.Length == 0)
    		{
    			sb.AppendLine(path + ": no configured clips");
    			return;
    		}
    		bool flag = false;
    		ModelImporterClipAnimation[] array = clipAnimations;
    		foreach (ModelImporterClipAnimation val2 in array)
    		{
    			if (!(val2.name != from))
    			{
    				val2.name = to;
    				flag = true;
    			}
    		}
    		if (!flag)
    		{
    			sb.AppendLine(path + ": nothing to rename (clips now: [" + ClipNames(val) + "])");
    			return;
    		}
    		val.clipAnimations = clipAnimations;
    		((AssetImporter)val).SaveAndReimport();
    		array = val.clipAnimations;
    		for (int i = 0; i < array.Length; i++)
    		{
    			if (!(array[i].name != to))
    			{
    				sb.AppendLine(path + ": '" + from + "' -> '" + to + "' verified");
    				return;
    			}
    		}
    		sb.AppendLine(path + ": RENAME FAILED, clips now: [" + ClipNames(val) + "]");
    	}

    	private static string ClipNames(ModelImporter mi)
    	{
    		ModelImporterClipAnimation[] clipAnimations = mi.clipAnimations;
    		if (clipAnimations == null || clipAnimations.Length == 0)
    		{
    			return "none";
    		}
    		StringBuilder stringBuilder = new StringBuilder();
    		for (int i = 0; i < clipAnimations.Length; i++)
    		{
    			if (i > 0)
    			{
    				stringBuilder.Append(", ");
    			}
    			stringBuilder.Append(clipAnimations[i].name);
    		}
    		return stringBuilder.ToString();
    	}

    	private static void ReportAvatarBones(ModelImporter mi, StringBuilder sb)
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
    		HumanDescription humanDescription = mi.humanDescription;
    		int num = 54;
    		string arg = ((num < humanDescription.human.Length) ? "present" : "ABSENT from the array");
    		sb.AppendLine($"  avatar mapping: {humanDescription.human.Length} slot(s), " + $"UpperChest would be slot {num} -> {arg}");
    		int num2 = 21;
    		sb.AppendLine((num2 >= 0 && num2 < humanDescription.human.Length) ? ("  known defect, left alone: LeftEye -> '" + humanDescription.human[num2].boneName + "' (inert: no eye motion in any clip)") : "  LeftEye: unmapped");
    	}

    	private static void ImportIfNew(string path)
    	{
    		if (!((Object)(object)AssetImporter.GetAtPath(path) != (Object)null))
    		{
    			AssetDatabase.ImportAsset(path, (ImportAssetOptions)9);
    		}
    	}
    }
}