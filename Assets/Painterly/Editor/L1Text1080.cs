using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

// The namespace house style of the other tools in this folder is
// Echoes.Painterly.EditorTools, and this file has to compile in the EDITOR
// assembly, so it cannot be MonoBehaviour-holding game code.
namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Measures whether every line of text in Level 1 actually renders at the
    /// 5x size the player asked for.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this exists as a separate tool instead of a line in
    /// BeatTextAudit.</b> BeatTextAudit measures inside OnGUI, which means play
    /// mode, which means it can only report the resolution the game view
    /// happens to be at - and the one time it was run from a menu item it
    /// reported the editor's 640x480 default and a 105 px target as though
    /// those were the game's numbers. A measurement whose resolution depends
    /// on which window is focused is not a measurement of the game.</para>
    ///
    /// <para><b>So the resolution is a constant here.</b> 1920x1080, written
    /// into the source, and the target is derived from it with BeatText's own
    /// formula rather than by asking Screen. Run this in edit mode and the
    /// answer is the same every time, on any machine, with the game view
    /// hidden.</para>
    ///
    /// <para><b>It reports before and after.</b> "As shipped" is the size the
    /// whole line was fitted to when the only option was shrinking it - that is
    /// the 50-78% of target that complaint 2 was about. "Paged" is the size
    /// each page renders at now. If paging did nothing, the two columns would
    /// be identical, and this table would say so.</para>
    /// </remarks>
    public static class L1Text1080
    {
        // Hard-coded on purpose. See the remarks above.
        private const int ScreenW = 1920;
        private const int ScreenH = 1080;

        private const string LinesPath = "Assets/Painterly/MonoHintLines.asset";
        private const string ReportPath = "Temp/l1_text_1080.txt";

        [MenuItem("Echoes/L1/Text Audit at 1080p")]
        public static void Run()
        {
            var sb = new StringBuilder();

            // BeatText's own formulas, applied to the constant resolution
            // instead of to Screen. If these formulas change in BeatText, this
            // tool goes quietly wrong - so the arithmetic is spelled out here
            // and the achieved target is printed, and the report says what the
            // target was rather than assuming it is 225.
            float scale = Mathf.Max(0.7f, ScreenH / 720f);
            int target = Mathf.Max(10, Mathf.RoundToInt(30f * 5f * scale));
            float width = Mathf.Min(ScreenW * 0.9f, ScreenW - 24f);
            // Read the fraction from BeatText rather than repeating the number here.
            //
            // This line used to say 0.72f. When MaxBlockFraction was changed to
            // 0.42 this tool kept measuring against 0.72 and kept printing
            // "every page renders at the full 5x target" - a perfect-looking
            // report about a limit the game had stopped using. Nothing in the
            // output was wrong, which is exactly what made it dangerous: the
            // only way to catch it is to read the header and notice the number
            // did not move. A measuring tool that keeps its own copy of the
            // thing it measures is not measuring the game.
            float maxHeight = ScreenH * BeatText.MaxBlockFraction;

            sb.AppendLine("L1 text at " + ScreenW + "x" + ScreenH + "  (edit mode, resolution is a constant)");
            sb.AppendLine("");
            sb.AppendLine("  scale      = max(0.7, " + ScreenH + "/720) = " + scale.ToString("0.000"));
            sb.AppendLine("  target px  = round(30 * 5 * scale) = " + target + "   <- the 5x size");
            sb.AppendLine("  width px   = min(" + ScreenW + " * 0.90, " + ScreenW + " - 24) = " + width.ToString("0"));
            sb.AppendLine("  height px  = " + ScreenH + " * " + BeatText.MaxBlockFraction.ToString("0.00") + " (BeatText.MaxBlockFraction) = " + maxHeight.ToString("0.0"));

            var style = BeatText.MakeStandalone(TextAnchor.UpperLeft, target, wordWrap: true, Color.white);
            if (style == null || style.font == null)
            {
                sb.AppendLine("");
                sb.AppendLine("NO FONT. MakeStandalone could not get a builtin font, so nothing can be measured.");
                Write(sb);
                return;
            }
            sb.AppendLine("  font       = " + style.font.name + " (via MakeStandalone, the same call the runtime uses)");
            sb.AppendLine("  per page   = " + BeatText.LinesThatFit(style, width, maxHeight, target) + " line(s) fit " + maxHeight.ToString("0") + " px at " + target + " px");

            // -------------------------------------------------------------------
            // Gather every piece of text that can reach the screen.
            // -------------------------------------------------------------------
            var items = new List<KeyValuePair<string, string>>();

            var lines = AssetDatabase.LoadAssetAtPath<MonoHintLines>(LinesPath);
            if (lines == null)
            {
                sb.AppendLine("");
                sb.AppendLine("MISSING " + LinesPath + " - cannot audit Mono's lines.");
            }
            else
            {
                foreach (var line in lines.All)
                {
                    if (line == null || string.IsNullOrEmpty(line.text)) continue;
                    items.Add(new KeyValuePair<string, string>(
                        "mono." + line.kind + (line.muted ? " (muted)" : "") + " " + line.id, line.text));
                }
            }

            // Beat1Intro.introText is private and CardText() returns "" unless
            // the intro is in its Card phase, which edit mode never is. Reading
            // the field directly is the only way to measure the opening card
            // without entering play mode, which this tool deliberately does not.
            var intro = UnityEngine.Object.FindAnyObjectByType<Beat1Intro>();
            if (intro != null)
            {
                var f = typeof(Beat1Intro).GetField("introText", BindingFlags.Instance | BindingFlags.NonPublic);
                if (f != null)
                {
                    var v = f.GetValue(intro) as string;
                    if (!string.IsNullOrEmpty(v)) items.Add(new KeyValuePair<string, string>("intro card", v));
                }
                else
                {
                    sb.AppendLine("  (no private field 'introText' found on Beat1Intro - intro card not measured)");
                }
            }
            else
            {
                sb.AppendLine("  (no Beat1Intro in the open scene - intro card not measured)");
            }

            // -------------------------------------------------------------------
            // Measure.
            // -------------------------------------------------------------------
            int totalLines = 0, totalPages = 0, pagesAtTarget = 0, pagesBelow = 0;
            int linesNeedingPaging = 0;
            int worst = target;
            string worstWho = "-";

            sb.AppendLine("");
            sb.AppendLine(string.Format(
                "{0,-42} {1,6} {2,6} {3,6} {4,7} {5,7} {6}",
                "what", "chars", "lines", "pages", "shipped", "paged", "verdict"));
            sb.AppendLine(new string('-', 96));

            foreach (var item in items)
            {
                string who = item.Key, text = item.Value;

                int shipped = BeatText.Fit(style, text, width, maxHeight, target);
                int wrapped = MeasureLines(style, text, width, target);

                var pages = BeatText.Pages(style, text, width, maxHeight, target);
                totalLines++;
                totalPages += pages.Count;
                if (pages.Count > 1) linesNeedingPaging++;

                int minOnPage = int.MaxValue;
                for (int i = 0; i < pages.Count; i++)
                {
                    int size = BeatText.Fit(style, pages[i], width, maxHeight, target);
                    if (size >= target) pagesAtTarget++;
                    else
                    {
                        pagesBelow++;
                        if (size < minOnPage) minOnPage = size;
                    }
                    if (size < worst) { worst = size; worstWho = who + " page " + (i + 1); }
                }
                if (minOnPage == int.MaxValue) minOnPage = target;

                sb.AppendLine(string.Format(
                    "{0,-42} {1,6} {2,6} {3,6} {4,7} {5,7} {6}",
                    Shorten(who, 42), text.Length, wrapped, pages.Count,
                    shipped + " px", minOnPage + " px",
                    minOnPage >= target ? "AT TARGET" : "** " + (minOnPage * 100 / target) + "% **"));
            }

            sb.AppendLine(new string('-', 96));
            sb.AppendLine("");
            sb.AppendLine("  pieces of text measured : " + totalLines);
            sb.AppendLine("  total pages             : " + totalPages);
            sb.AppendLine("  lines that need paging  : " + linesNeedingPaging);
            sb.AppendLine("  pages AT target (" + target + " px) : " + pagesAtTarget);
            sb.AppendLine("  pages below target       : " + pagesBelow);
            sb.AppendLine("  smallest page rendered   : " + worst + " px" + (worst < target ? "   <-- " + worstWho : ""));
            sb.AppendLine("");
            sb.AppendLine(pagesBelow == 0
                ? "VERDICT: every page renders at the full 5x target."
                : "VERDICT: " + pagesBelow + " page(s) still below target. A single line that overflows on its own");
            if (pagesBelow != 0)
            {
                sb.AppendLine("        cannot be paged and is the only case where shrinking is correct.");
            }
            sb.AppendLine("");
            sb.AppendLine("How to read 'shipped': that is what the whole line was fitted to when shrinking");
            sb.AppendLine("was the only option. It is the number complaint 2 was filed about.");

            Write(sb);
        }

        /// <summary>How many wrapped lines the text occupies at the target size.</summary>
        private static int MeasureLines(GUIStyle style, string text, float width, int target)
        {
            style.fontSize = target;
            string[] paras = text.Split('\n');
            int n = 0;
            foreach (var para in paras)
            {
                if (para.Trim().Length == 0) { n++; continue; }
                style.fontSize = target;
                n += Mathf.Max(1, Mathf.CeilToInt(style.CalcHeight(new GUIContent(para), width) /
                                                  Mathf.Max(1f, style.CalcHeight(new GUIContent("Wjq"), width))));
            }
            return n;
        }

        private static string Shorten(string s, int n)
        {
            s = s.Replace("\n", " ");
            return s.Length <= n ? s : s.Substring(0, n - 1) + "~";
        }

        private static void Write(StringBuilder sb)
        {
            string path = ReportPath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // Report both bytes and the length it meant to write. A byte count
            // alone is how a script can report success while achieving nothing.
            byte[] bytes = new UTF8Encoding(false).GetBytes(sb.ToString());
            File.WriteAllBytes(path, bytes);

            long onDisk = new FileInfo(path).Length;
            Debug.Log("[Echoes] L1 text audit at " + ScreenW + "x" + ScreenH +
                      " -> " + path + "  (" + onDisk + " B on disk, " + bytes.Length + " B intended)");
            Debug.Log(sb.ToString());
        }
    }
}