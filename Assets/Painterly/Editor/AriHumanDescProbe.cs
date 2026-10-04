using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly.EditorTools
{

    public static class AriHumanDescProbe
    {
    	[MenuItem("Tools/Echoes/Probe Ari HumanDesc", priority = 94)]
    	public static void Run()
    	{
    		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
    		StringBuilder stringBuilder = new StringBuilder();
    		Avatar val = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Ari/Models/Ari_character.fbx").OfType<Avatar>().FirstOrDefault();
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogError((object)"[Echoes] no avatar");
    			return;
    		}
    		HumanDescription humanDescription = val.humanDescription;
    		HumanBone[] human = humanDescription.human;
    		SkeletonBone[] skeleton = humanDescription.skeleton;
    		stringBuilder.AppendLine($"human.Length   = {human.Length}");
    		stringBuilder.AppendLine($"skeleton.Length= {skeleton.Length}");
    		stringBuilder.AppendLine($"HumanBodyBones values = {Enum.GetValues(typeof(HumanBodyBones)).Length}");
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("idx  enum                     -> boneName (via human[idx])");
    		for (int i = 0; i < human.Length; i++)
    		{
    			string boneName = human[i].boneName;
    			string arg = (Enum.IsDefined(typeof(HumanBodyBones), i) ? ((object)(HumanBodyBones)i/*cast due to constrained. prefix*/).ToString() : "(out of enum range)");
    			stringBuilder.AppendLine($"  {i,3}  {arg,-24} -> '{boneName}'");
    		}
    		File.WriteAllText("Temp/ari_humandesc.txt", stringBuilder.ToString());
    		Debug.Log((object)("[Echoes] Ari humanDesc probe -> Temp/ari_humandesc.txt\n" + stringBuilder));
    	}
    }
}