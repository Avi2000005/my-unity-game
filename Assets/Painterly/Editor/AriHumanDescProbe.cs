using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Settles how HumanDescription.human is indexed, before anything tries to
    /// edit it. Writes Temp/ari_humandesc.txt.
    ///
    /// An earlier report claimed human.Length was 22, yet the same array was read
    /// at LeftLowerLeg — an enum value far past 22 — and came back with a bone
    /// name. Both cannot be true, so one of the two readings was wrong and it
    /// matters: an avatar edit that indexes the array by enum value will corrupt
    /// a different bone entirely if the array is not laid out that way.
    /// </summary>
    public static class AriHumanDescProbe
    {
        [MenuItem("Tools/Echoes/Probe Ari HumanDesc", priority = 94)]
        public static void Run()
        {
            var sb = new StringBuilder();

            var avatar = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Ari/Models/Ari_character.fbx")
                                  .OfType<Avatar>().FirstOrDefault();

            if (avatar == null) { Debug.LogError("[Echoes] no avatar"); return; }

            var hd = avatar.humanDescription;
            var human = hd.human;
            var skel = hd.skeleton;

            sb.AppendLine($"human.Length   = {human.Length}");
            sb.AppendLine($"skeleton.Length= {skel.Length}");
            sb.AppendLine($"HumanBodyBones values = {System.Enum.GetValues(typeof(HumanBodyBones)).Length}");
            sb.AppendLine();

            sb.AppendLine("idx  enum                     -> boneName (via human[idx])");
            for (int i = 0; i < human.Length; i++)
            {
                var name = human[i].boneName;
                var asEnum = System.Enum.IsDefined(typeof(HumanBodyBones), i)
                    ? ((HumanBodyBones)i).ToString()
                    : "(out of enum range)";

                sb.AppendLine($"  {i,3}  {asEnum,-24} -> '{name}'");
            }

            File.WriteAllText("Temp/ari_humandesc.txt", sb.ToString());
            Debug.Log("[Echoes] Ari humanDesc probe -> Temp/ari_humandesc.txt\n" + sb);
        }
    }
}
