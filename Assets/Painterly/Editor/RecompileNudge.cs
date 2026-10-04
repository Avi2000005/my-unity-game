using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

// Global namespace on purpose: this is a tool about the compilation pipeline
// itself, not part of the game.
public static class RecompileNudge
{
    const string Report = "Temp/recompile_nudge.txt";

    /// <summary>
    /// Dumps what the script compiler has actually been handed, and forces it
    /// to hand it the right thing.
    ///
    /// The symptom this exists for: a brand new, twenty-line, dependency free
    /// .cs file is in the asset database with a GUID, loads as a MonoScript, and
    /// still does not appear in the project compile. Its class cannot be found,
    /// so the runtime scripts that reference it fail, and the editor sits there
    /// reporting the same two errors from an hour ago while the real current
    /// error is somewhere else entirely.
    ///
    /// The one number that settles it is not "does the file exist" and not "does
    /// the class resolve" — both of those have been answering yes all along,
    /// and both are compatible with the compiler never being told the file
    /// exists. It is: is the file in the compiler's own source list.
    /// </summary>
    public static void Run()
    {
        var sb = new StringBuilder();

        try
        {
            Dump(sb);
            Fix(sb);
        }
        catch (System.Exception e)
        {
            sb.AppendLine();
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] recompile nudge\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    // --- the question -------------------------------------------------------------

    static void Dump(StringBuilder sb)
    {
        sb.AppendLine("WHAT THE COMPILER HAS BEEN HANDED");
        sb.AppendLine("at " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        var wanted = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(p => p, System.StringComparer.Ordinal)
            .ToArray();

        sb.AppendLine("scripts in the asset database under Assets: " + wanted.Length);
        sb.AppendLine();

        var compiled = new System.Collections.Generic.HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase);

        foreach (var asm in CompilationPipeline.GetAssemblies(AssembliesType.Editor))
        {
            var sources = SourceFilesOf(asm);
            var name = NameOf(asm);

            foreach (var s in sources) compiled.Add(Normalise(s));

            // Only the user assemblies matter here. The package ones run to
            // hundreds and drown the answer.
            if (name != "Assembly-CSharp" && name != "Assembly-CSharp-Editor") continue;

            sb.AppendLine("--- " + name + ": " + sources.Length + " source file(s)");
            foreach (var s in sources) sb.AppendLine("      " + Normalise(s));
            sb.AppendLine();
        }

        var missing = wanted.Where(p => !compiled.Contains(Normalise(p))).ToArray();

        sb.AppendLine("--- scripts the compiler has NOT been told about: " +
                      missing.Length);
        foreach (var m in missing) sb.AppendLine("      MISSING " + m);

        if (missing.Length == 0)
        {
            sb.AppendLine();
            sb.AppendLine("Nothing is missing. Every script in the project is in the");
            sb.AppendLine("compiler's source list, so if a class still does not resolve,");
            sb.AppendLine("the fault is in the source and not in the pipeline.");
        }
    }

    // --- the repairs --------------------------------------------------------------

    static void Fix(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("REPAIRS ATTEMPTED");

        // Folder import rather than a database refresh. Refresh finds files
        // that have changed; ImportAsset on a folder goes and re-reads the
        // folder's contents and tells the import pipeline about each one, and
        // that is the call that rebuilds the script list.
        //
        // Both folders, because they compile into different assemblies and
        // importing only the runtime one leaves an editor script invisible for
        // no better reason than that it lives in a different directory.
        foreach (var folder in new[] { "Assets/Painterly/Scripts", "Assets/Painterly/Editor" })
        {
            sb.AppendLine("  ImportAsset(" + folder + ", recursive, forced, sync)");
            AssetDatabase.ImportAsset(folder,
                ImportAssetOptions.ImportRecursive |
                ImportAssetOptions.ForceUpdate |
                ImportAssetOptions.ForceSynchronousImport);
        }

        // CleanBuildCache discards the cached response file, which is the cache
        // that is wrong: on its own RequestScriptCompilation re-uses it. This is
        // the only call available from a script that makes the compiler
        // re-derive its file list from the asset database instead.
        sb.AppendLine("  RequestScriptCompilation(CleanBuildCache)");
        CompilationPipeline.RequestScriptCompilation(
            RequestScriptCompilationOptions.CleanBuildCache);

        sb.AppendLine();
        sb.AppendLine("After these, re-run this tool. If the MISSING list is still not");
        sb.AppendLine("empty the editor needs a domain reload, which needs a human:");
        sb.AppendLine("close and reopen Unity, or focus the window and watch the spinner.");
        sb.AppendLine();
        sb.AppendLine("Nothing here deletes anything. Every script still runs correctly");
        sb.AppendLine("through the tool bridge, because that path compiles the file");
        sb.AppendLine("itself and never consults the project assembly at all.");
    }

    // --- reflection ----------------------------------------------------------------

    /// <summary>
    /// The assembly's source paths, read reflectively.
    ///
    /// UnityEditor.Compilation.Assembly does not present the shape it appears
    /// to: outputPath is a method, not a property, and naming a member wrongly
    /// turns a diagnostic into a compile error, which is a much worse place to
    /// discover the shape of an API.
    /// </summary>
    static string[] SourceFilesOf(object asm)
    {
        if (asm == null) return new string[0];

        var prop = asm.GetType().GetProperties()
            .FirstOrDefault(p => p.Name.IndexOf("source",
                                               System.StringComparison.OrdinalIgnoreCase) >= 0);

        if (prop == null || prop.GetValue(asm) is not IEnumerable e) return new string[0];

        return e.Cast<object>().Select(x => x?.ToString() ?? "").ToArray();
    }

    static string NameOf(object asm)
    {
        var prop = asm?.GetType().GetProperty("name");
        return prop?.GetValue(asm) as string ?? "(unreadable)";
    }

    /// <summary>
    /// One spelling for a path, so that a mismatch in separator or in case
    /// cannot be mistaken for a file the compiler has never heard of.
    /// </summary>
    static string Normalise(string path)
    {
        return (path ?? "").Replace('\\', '/').TrimStart('/');
    }
}
