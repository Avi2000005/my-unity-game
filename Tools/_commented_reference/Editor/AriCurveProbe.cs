using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Dumps what curve data a clip actually holds. Writes Temp/ari_curves.txt.
    ///
    /// The motion probe reported every clip as static, but it also matched no
    /// transform curves at all — which is not the same as "no motion". It means
    /// the lookup was looking in the wrong place. A clip imported as Humanoid is
    /// not stored as m_LocalPosition on the bones; the pose is retargeted into
    /// Unity's muscle space at import, so the position data that remains lives
    /// on the Animator as muscle curves, and the bone transforms are outputs of
    /// that rather than inputs to it.
    ///
    /// So the shape of the stored data has to be looked at before concluding
    /// anything about whether Ari moves.
    /// </summary>
    public static class AriCurveProbe
    {
        const string Dir = "Assets/Art/Ari/Models";

        [MenuItem("Tools/Echoes/Probe Ari Curves", priority = 92)]
        public static void Run()
        {
            var sb = new StringBuilder();

            foreach (var model in new[] { "Ari_character", "Idle", "Walking" })
            {
                string path = $"{Dir}/{model}.fbx";
                sb.AppendLine($"=== {model} ===");

                var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__"))
                    .ToArray();

                foreach (var clip in clips)
                {
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    sb.AppendLine($"  '{clip.name}' len={clip.length:0.000}s " +
                                  $"bindings={bindings.Length} humanMotion=" +
                                  $"{clip.humanMotion}");

                    foreach (var group in bindings
                             .GroupBy(b => b.type.Name + " . " + b.propertyName)
                             .OrderByDescending(g => g.Count()))
                    {
                        var sample = string.Join(", ", group.Take(3)
                            .Select(b => b.path ?? "<root>"));
                        sb.AppendLine($"      {group.Count(),5}x  {group.Key}   e.g. {sample}");
                    }

                    sb.AppendLine();
                }
            }

            File.WriteAllText("Temp/ari_curves.txt", sb.ToString());
            Debug.Log("[Echoes] Ari curve probe -> Temp/ari_curves.txt\n" + sb);
        }
    }
}
