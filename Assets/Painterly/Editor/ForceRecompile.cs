using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Asks Unity to notice the scripts on disk, then reports what the RUNNING
    /// assembly actually contains.
    /// </summary>
    /// <remarks>
    /// <para><b>Everything here goes through reflection, on purpose.</b> The
    /// first version of this file wrote <c>BeatText.HintTarget</c> directly,
    /// and could not be compiled at all: the bridge reported
    /// <c>CS0117: 'BeatText' does not contain a definition for 'HintTarget'</c>.
    /// That is not a mistake in the name. It is the whole point of this tool.
    /// Unity was linking against an Assembly-CSharp that predated the edit, so
    /// a tool that names a NEW member cannot even build, and a tool that names
    /// an OLD member builds happily and reports the OLD value as though it
    /// were current.</para>
    ///
    /// <para>So this file may only use reflection, and the report always shows
    /// the source file's number next to the loaded assembly's number. When they
    /// disagree, the loaded one is a lie and every measurement taken through
    /// this bridge is a measurement of the previous build - which is exactly
    /// what happened to the 0.72 versus 0.42 block budget.</para>
    /// </remarks>
    public static class ForceRecompile
    {
        private const string ReportPath = "Temp/l1_recompile.txt";
        private const string BeatTextPath = "Assets/Painterly/Scripts/BeatText.cs";

        [MenuItem("Echoes/L1/Force Recompile and Report Live Values", priority = 81)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ForceRecompile");
            sb.AppendLine("");
            AppendLive(sb);
            sb.AppendLine("--- asking Unity to rebuild ---");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CompilationPipeline.RequestScriptCompilation(
                RequestScriptCompilationOptions.CleanBuildCache);

            sb.AppendLine("  AssetDatabase.Refresh + CleanBuildCache requested.");
            sb.AppendLine("  The domain reload may interrupt this call, so the report above may be");
            sb.AppendLine("  from before the rebuild. Run this again in a few seconds and compare:");
            sb.AppendLine("  if MaxBlockFraction is now 0.42 and matches the source, Unity has");
            sb.AppendLine("  caught up and previous measurements are worth trusting again.");

            Write(sb);
        }

        /// <summary>Report only. Requests nothing, so it is safe at any time.</summary>
        [MenuItem("Echoes/L1/Report Live Values (no rebuild)", priority = 82)]
        public static void ReportOnly()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Live values, read through reflection so this file compiles against any build");
            AppendLive(sb);
            Write(sb);
        }

        private static void AppendLive(StringBuilder sb)
        {
            Type bt = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType("Echoes.Painterly.BeatText");
                if (t != null) { bt = t; break; }
            }

            if (bt == null)
            {
                sb.AppendLine("  BeatText type not found in any loaded assembly.");
                return;
            }
            sb.AppendLine("  loaded from: " + bt.Assembly.Location + "   (" + bt.Assembly.GetName().Name + ")");

            string fracLoaded = ReadConstOrField(bt, "MaxBlockFraction");
            string hintLoaded = ReadConstOrField(bt, "HintTarget");
            sb.AppendLine("  RUNNING  MaxBlockFraction = " + (fracLoaded.Length == 0 ? "ABSENT" : fracLoaded));
            sb.AppendLine("  RUNNING  HintTarget       = " + (hintLoaded.Length == 0 ? "ABSENT" : hintLoaded));

            string fracSrc = ReadSourceFraction();
            sb.AppendLine("  SOURCE   MaxBlockFraction = " + (fracSrc.Length == 0 ? "not found" : fracSrc));
            sb.AppendLine("  SOURCE   HintTarget       = " + (ReadSourceHas("HintTarget") ? "present" : "ABSENT"));

            bool match = fracLoaded.Length > 0 && fracSrc.Length > 0 &&
                         fracLoaded.TrimEnd('f', 'F') == fracSrc.TrimEnd('f', 'F');
            sb.AppendLine("");
            if (match)
            {
                sb.AppendLine("  MATCH. The loaded assembly and the source agree, so anything measured");
                sb.AppendLine("  through this bridge reflects the code on disk.");
            }
            else
            {
                sb.AppendLine("  MISMATCH. Unity is running an Assembly-CSharp that predates the edit.");
                sb.AppendLine("  Every number measured through this bridge right now describes the");
                sb.AppendLine("  PREVIOUS build, not the code you are looking at. Rebuild first.");
            }
        }

        /// <summary>Reads a static field or property by name, whatever it is.</summary>
        private static string ReadConstOrField(Type t, string name)
        {
            var f = t.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return Convert.ToString(f.GetValue(null));

            var p = t.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null) return Convert.ToString(p.GetValue(null, null));

            return "";
        }

        private static string ReadSourceFraction()
        {
            try
            {
                string src = File.ReadAllText(BeatTextPath);
                int i = src.IndexOf("MaxBlockFraction = ", StringComparison.Ordinal);
                if (i < 0) return "";
                int j = src.IndexOf(';', i);
                if (j < 0) return "";
                return src.Substring(i + "MaxBlockFraction = ".Length, j - i - "MaxBlockFraction = ".Length).Trim();
            }
            catch { return ""; }
        }

        private static bool ReadSourceHas(string name)
        {
            try { return File.ReadAllText(BeatTextPath).Contains(name); }
            catch { return false; }
        }

        private static void Write(StringBuilder sb)
        {
            var dir = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            byte[] bytes = new UTF8Encoding(false).GetBytes(sb.ToString());
            File.WriteAllBytes(ReportPath, bytes);
            long onDisk = new FileInfo(ReportPath).Length;
            Debug.Log("[Echoes] live values -> " + ReportPath + "  (" + onDisk + " B on disk, " + bytes.Length + " B intended)");
            Debug.Log(sb.ToString());
        }
    }
}