using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

// Deliberately in the global namespace. It has to compile against whatever
// Assembly-CSharp currently IS, and the question it is asking is whether that
// assembly contains the Beat 3 types at all. Putting it in a namespace of its
// own would hide nothing but would read as if it were part of the game.
public static class AssemblyTypeProbe
{
    const string Report = "Temp/type_probe.txt";

    public static void Run()
    {
        var sb = new StringBuilder();
        try
        {
            // typeof(MonoBehaviour).Assembly is UnityEngine.CoreModule, not
            // Assembly-CSharp. Asking the question that way returns 1792 Unity
            // types and reports every one of the game's own classes as absent,
            // which is a confident and completely wrong answer.
            Assembly asm = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");

            if (asm == null)
            {
                sb.AppendLine("Assembly-CSharp is NOT LOADED. Assemblies that do:");
                foreach (var a in System.AppDomain.CurrentDomain.GetAssemblies())
                    if (a.GetName().Name.IndexOf("Assembly",
                                                 System.StringComparison.Ordinal) >= 0)
                        sb.AppendLine("  " + a.GetName().Name);
                return;
            }

            sb.AppendLine("Assembly-CSharp is " + asm.FullName);
            sb.AppendLine("  at " + asm.Location);

            var all = asm.GetTypes();
            sb.AppendLine("  holds " + all.Length + " types");

            string[] want = { "MonoCompanion", "MonoHintLines", "MonoChase", "SleepingTree",
                              "BeatHud", "CastPlacement", "Beat3Setup", "AriMover",
                              "BrushPainter", "ColorRestoreTarget", "AriFollowCamera",
                              "ZzCompileCanary" };

            foreach (var w in want)
            {
                var t = all.FirstOrDefault(x => x.Name == w);
                sb.AppendLine("  " + w + ": " + (t == null ? "ABSENT" : "present in " + t.Namespace));
            }

            // Anything at all matching Mono, in case the name is not what I
            // think it is.
            foreach (var t in all.Where(x => x.Name.IndexOf("Mono",
                                                              System.StringComparison.OrdinalIgnoreCase) >= 0))
                sb.AppendLine("  (name match) " + t.FullName);

            // The beat 3 files as the asset database sees them, which is a
            // different question from whether they compiled.
            foreach (var p in new[] { "Assets/Painterly/Scripts/MonoCompanion.cs",
                                      "Assets/Painterly/Scripts/MonoChase.cs",
                                      "Assets/Painterly/Scripts/SleepingTree.cs",
                                      "Assets/Painterly/Scripts/MonoHintLines.cs",
                                      "Assets/Painterly/Editor/Beat3Setup.cs",
                                      "Assets/Painterly/Editor/CastPlacement.cs" })
            {
                var guid = UnityEditor.AssetDatabase.AssetPathToGUID(p);
                var obj = UnityEditor.AssetDatabase.LoadMainAssetAtPath(p);
                sb.AppendLine("  asset " + p + " guid=" +
                              (guid.Length == 0 ? "NONE" : guid) +
                              " loaded=" + (obj != null));
            }
        }
        catch (System.Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
        }

        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
        Debug.Log("[Probe] " + sb);
    }
}
