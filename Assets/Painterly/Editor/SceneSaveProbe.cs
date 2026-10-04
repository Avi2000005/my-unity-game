using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Marks the open scene dirty and saves it, then reads the file back off
    /// disk to confirm the new AriMover collision settings are actually in it.
    ///
    /// Written because save_all reported {"saved": true, "scenes": []}: it saved
    /// nothing. A scene that is not dirty is not written, and adding a field to
    /// a component does not by itself dirty the scene — the component's
    /// serialized data is only rewritten when something asks Unity to
    /// re-serialize it. So the six new collision fields existed in the compiled
    /// class and not in the level, which means the level is relying on the C#
    /// field initialisers to turn collision on at all. Those initialisers are
    /// correct, so nothing is broken today; it is just that nothing on disk
    /// records that the wall sweep is supposed to be there, and the first
    /// person to reorder a field or rename one would silently turn it off.
    ///
    /// The read-back matters for the usual reason: a save that reports success
    /// and writes nothing looks identical to a save that worked.
    /// </summary>
    public static class SceneSaveProbe
    {
        const string Report = "Temp/scene_save.txt";

        [MenuItem("Tools/Echoes/Save Scene And Verify Ari", priority = 63)]
        public static void Run()
        {
            var sb = new StringBuilder();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            sb.AppendLine($"active scene: {scene.name}  path={scene.path}  " +
                          $"loaded={scene.isLoaded}  dirty={scene.isDirty}");

            if (!scene.isLoaded)
            {
                sb.AppendLine("no scene is open. Open SampleScene and run this again.");
                Finish(sb);
                return;
            }

            var ari = GameObject.Find("Ari");
            if (ari == null)
            {
                sb.AppendLine("no Ari in the scene.");
                Finish(sb);
                return;
            }

            var mover = ari.GetComponent<Echoes.Painterly.AriMover>();
            if (mover == null)
            {
                sb.AppendLine("Ari has no AriMover.");
                Finish(sb);
                return;
            }

            // What the component holds right now. Written to the report before
            // the save, because after a save the value on disk and the value in
            // memory are the same thing and can no longer tell you whether the
            // write happened.
            sb.AppendLine("\n--- in memory, before saving ---");
            sb.AppendLine("  " + Describe(mover));

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);

            sb.AppendLine($"\n--- save ---\n  SaveScene returned {saved}; " +
                          $"dirty now {scene.isDirty}");

            // And now what is actually in the file.
            string full = Path.Combine(Directory.GetCurrentDirectory(),
                                        scene.path.Replace('/', Path.DirectorySeparatorChar));
            sb.AppendLine($"  file: {full}");
            sb.AppendLine($"  exists: {File.Exists(full)}, " +
                          $"written {System.DateTime.Now:HH:mm:ss}");

            if (File.Exists(full))
            {
                var text = File.ReadAllText(full);

                var wanted = new[]
                {
                    "collideWithWalls", "bodyRadius", "bodyHeight",
                    "bodyBottomLift", "stepHeight", "wallMask"
                };

                sb.AppendLine("\n--- on disk ---");
                foreach (var key in wanted)
                {
                    var line = text.Split('\n')
                                   .FirstOrDefault(l => l.TrimStart().StartsWith(key + ":"));
                    sb.AppendLine("  " + (line == null
                        ? key + ": NOT IN THE FILE — the level is relying on the C# default"
                        : line.Trim()));
                }
            }

            Finish(sb);
        }

        /// <summary>
        /// The component's current settings, read through a SerializedObject so
        /// that what is reported is what Unity would write rather than what the
        /// fields happen to hold in this process.
        /// </summary>
        static string Describe(Echoes.Painterly.AriMover mover)
        {
            var so = new SerializedObject(mover);
            var keys = new[]
            {
                "walkSpeed", "runSpeed", "jumpHeight", "gravity", "airControl",
                "collideWithWalls", "bodyRadius", "bodyHeight",
                "bodyBottomLift", "stepHeight", "soleOffset"
            };

            var sb = new StringBuilder();
            foreach (var k in keys)
            {
                var p = so.FindProperty(k);
                if (p == null) { sb.Append(k + "=<absent> "); continue; }
                sb.Append(k + "=" + (p.propertyType == SerializedPropertyType.Float
                                     ? p.floatValue.ToString("0.###")
                                     : p.boolValue.ToString()) + " ");
            }
            return sb.ToString().Trim();
        }

        static void Finish(StringBuilder sb)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), Report);
            File.WriteAllText(path, sb.ToString());
            Debug.Log("[Echoes] scene save\n" + sb);
        }
    }
}
