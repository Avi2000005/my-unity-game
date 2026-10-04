using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Reads the Level 1 models back off disk and says what is actually there.
    ///
    /// Written because two things came back impossible at once. Every
    /// animation importer reported itself as lost immediately after its own
    /// SaveAndReimport, and all three character models reported an unusable
    /// avatar. Those do not go together: if the imports had genuinely failed,
    /// the .meta files would be missing, and if the .meta files are present
    /// then GetAtPath returning null is something else. So this looks at the
    /// meta, the importer and the sub-assets separately, and changes nothing.
    /// </summary>
    public static class L1StateProbe
    {
        const string Dir = "Assets/Art/L1";

        const string Report = "Temp/l1_state.txt";

        [MenuItem("Tools/Echoes/Probe Level 1 Cast", priority = 62)]
        public static void Run()
        {
            var sb = new StringBuilder();
            var reportPath = Path.Combine(Directory.GetCurrentDirectory(), Report);
            if (File.Exists(reportPath)) File.Delete(reportPath);

            var paths = AssetDatabase.FindAssets("t:Model", new[] { Dir })
                            .Select(AssetDatabase.GUIDToAssetPath)
                            .Where(p => p.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                            .OrderBy(p => p, System.StringComparer.Ordinal)
                            .ToArray();

            sb.AppendLine($"{paths.Length} model(s) under {Dir}");

            foreach (var path in paths)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                string meta = path + ".meta";
                bool metaExists = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), meta));

                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                var avatar = all.OfType<Avatar>().FirstOrDefault();
                var clips = all.OfType<AnimationClip>()
                                .Where(c => !c.name.StartsWith("__")).ToArray();

                sb.AppendLine();
                sb.AppendLine($"{name}");
                sb.AppendLine($"  meta on disk : {(metaExists ? "yes" : "NO")}");

                if (mi == null)
                {
                    // Reported rather than skipped, because this is the exact
                    // line the import tool printed and the two must be
                    // reconciled: an importer that is missing at rest and
                    // missing only mid-reimport are different bugs.
                    sb.AppendLine("  importer     : NULL at rest  <-- not a reimport race");
                    sb.AppendLine($"  sub-assets   : {all.Length} ({Describe(all)})");
                    sb.AppendLine($"  clips on disk: {clips.Length} [{string.Join(", ", clips.Select(c => c.name))}]");
                    continue;
                }

                sb.AppendLine($"  importer     : {mi.animationType}, avatarSetup={mi.avatarSetup}, " +
                              $"importAnimation={mi.importAnimation}");
                sb.AppendLine($"  avatar       : {(avatar == null ? "NONE" : $"valid={avatar.isValid} human={avatar.isHuman}")}");

                if (mi is ModelImporter model)
                {
                    sb.AppendLine($"  configured   : {model.clipAnimations.Length} clip(s) " +
                                  $"[{string.Join(", ", model.clipAnimations.Select(c => c.name))}]");
                }

                sb.AppendLine($"  takes        : {CountTakes(mi)}");
                sb.AppendLine($"  clips on disk: {clips.Length} [{string.Join(", ", clips.Select(c => c.name + " " + c.length.ToString("0.00") + "s"))}]");
            }

            Debug.Log("[Echoes] L1 state\n" + sb);
            File.WriteAllText(reportPath, sb.ToString());
        }

        static string Describe(Object[] all)
        {
            if (all.Length == 0) return "none";
            return string.Join(", ", all.Select(a => a == null ? "null" : a.GetType().Name));
        }

        static int CountTakes(ModelImporter mi)
        {
            int n = 0;
            try { n = mi.defaultClipAnimations.Length; }
            catch (System.Exception e) { return -1; }
            return n;
        }
    }
}
