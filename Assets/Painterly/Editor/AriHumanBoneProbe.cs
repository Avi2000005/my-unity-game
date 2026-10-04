using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Dumps the real shape of HumanBone and the importer's current mapping.
    /// Writes Temp/ari_humanbone.txt.
    ///
    /// Written because guessing at this struct's field names has already failed
    /// twice — it has no member called humanBodyBone, and none called
    /// humanBodyBoneToBoneScale either. Reflecting over it is the way to find
    /// out what it actually exposes instead of spending another compile cycle
    /// on a guess.
    /// </summary>
    public static class AriHumanBoneProbe
    {
        [MenuItem("Tools/Echoes/Probe Ari HumanBone", priority = 95)]
        public static void Run()
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== HumanBone members ===");
            foreach (var f in typeof(HumanBone).GetFields(
                         BindingFlags.Public | BindingFlags.Instance))
            {
                sb.AppendLine($"  field {f.FieldType.Name,-20} {f.Name}  " +
                              $"(get={f.IsPublic}, readonly={f.IsInitOnly})");
            }
            foreach (var p in typeof(HumanBone).GetProperties(
                         BindingFlags.Public | BindingFlags.Instance))
            {
                sb.AppendLine($"  prop  {p.PropertyType.Name,-20} {p.Name}  " +
                              $"(canWrite={p.CanWrite})");
            }

            var mi = AssetImporter.GetAtPath("Assets/Art/Ari/Models/Ari_character.fbx")
                        as ModelImporter;
            if (mi == null) { sb.AppendLine("\nno importer"); goto done; }

            var hd = mi.humanDescription;
            sb.AppendLine($"\n=== importer humanDescription.human.Length = {hd.human.Length} ===");

            foreach (HumanBodyBones slot in new[]
            {
                HumanBodyBones.UpperChest, HumanBodyBones.LeftEye,
                HumanBodyBones.RightEye, HumanBodyBones.Chest, HumanBodyBones.Spine
            })
            {
                int i = (int)slot;
                if (i < 0 || i >= hd.human.Length) { sb.AppendLine($"  {slot}: no slot"); continue; }

                var hb = hd.human[i];
                var parts = typeof(HumanBone).GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Select(f => $"{f.Name}={f.GetValue(hb)}");
                sb.AppendLine($"  [{i,2}] {slot,-12} {string.Join("  ", parts)}");
            }

            // Any bone already claimed by a slot, so a reassignment cannot collide.
            sb.AppendLine("\n=== all currently mapped slots ===");
            for (int i = 0; i < hd.human.Length; i++)
            {
                var name = hd.human[i].boneName;
                if (string.IsNullOrEmpty(name)) continue;
                sb.AppendLine($"  [{i,2}] {(HumanBodyBones)i,-14} -> '{name}'");
            }

        done:
            File.WriteAllText("Temp/ari_humanbone.txt", sb.ToString());
            Debug.Log("[Echoes] Ari HumanBone probe -> Temp/ari_humanbone.txt\n" + sb);
        }
    }
}
