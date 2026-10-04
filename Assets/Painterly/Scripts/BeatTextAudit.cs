using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// Measures what the text ACTUALLY renders at, once, during a play session,
    /// and then removes itself.
    ///
    /// <para><b>Why this exists when <c>BeatText.Shortfalls</c> already
    /// measures.</b> Because that one has to answer the question outside play
    /// mode, and outside play mode there is no <c>GUI.skin</c> — Unity throws
    /// rather than guess — so it falls back to the built-in runtime font and
    /// every line count it prints is an estimate. This one is inside
    /// <c>OnGUI</c>, so it measures against the real skin, the real font and
    /// the real resolution, and those are the numbers to believe.</para>
    ///
    /// <para><b>It deletes itself after one pass.</b> That is not tidiness, it
    /// is the reason it is allowed to be in the scene at all: a component whose
    /// whole job is to time itself carefully and write a file must not be
    /// running during a fight, and a self-removing component cannot be left
    /// behind by accident. If the file is already there and recent, it writes
    /// nothing and leaves itself alone — so running it twice is harmless.</para>
    ///
    /// <para>What it reports per string: the characters, the size asked for,
    /// the size actually used, the percentage of the target achieved, and the
    /// achieved multiplier over the OLD size. The last of those is the number
    /// that matters: "5x" is the request, "3.2x" is the truth.</para>
    /// </summary>
    [AddComponentMenu("Echoes/Beat Text Audit")]
    public sealed class BeatTextAudit : MonoBehaviour
    {
        const string ReportPath = "Temp/beattext_audit.txt";

        /// <summary>
        /// How long a written report is considered current. Ten minutes is long
        /// enough that a player who does not play for a while gets a fresh
        /// number, and short enough that it is not a permanent claim about a
        /// level that has since changed.
        /// </summary>
        const float FreshSeconds = 600f;

        [Tooltip("Extra strings to measure, one per entry. Blank entries are " +
                 "skipped. These are usually the serialized fields on beats, " +
                 "which are the longest text in the level and the ones least " +
                 "likely to be in a dialogue asset.")]
        [TextArea(3, 12)]
        [SerializeField] List<string> extra = new List<string>();

        bool _done;

        /// <summary>
        /// Add a string to be measured. Called by the fixups tool so the
        /// opening card — the longest single string in the level and the one
        /// the player reads first — is in the report without anybody having to
        /// type it in.
        /// </summary>
        public void Add(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (extra == null) extra = new List<string>();
            if (!extra.Contains(text)) extra.Add(text);
        }

        void Update()
        {
            // Fresh enough: do nothing, and stay in the scene so the next
            // resolution change can be measured against the real thing.
            if (File.Exists(ReportPath) &&
                Time.realtimeSinceStartup < FreshSeconds)
            {
                return;
            }

            Destroy(gameObject);
        }

        void OnGUI()
        {
            if (_done) return;
            _done = true;

            var sb = new StringBuilder();

            sb.AppendLine("[Echoes] beat text audit — measured inside OnGUI, so");
            sb.AppendLine("        these numbers are the real skin at the real");
            sb.AppendLine("        resolution. Compare with the out-of-play");
            sb.AppendLine("        estimate in Temp/l1_fixups.txt.");
            sb.AppendLine();
            sb.AppendLine("screen        " + Screen.width + " x " + Screen.height);
            sb.AppendLine("scale         " + BeatText.ScreenScale.ToString("0.00") +
                          "  (Screen.height / 720)");
            sb.AppendLine("asked for     " + BeatText.Scale + "x the old sizes");
            sb.AppendLine("  old prompt  " + BeatText.BasePrompt + " px  -> target " +
                          BeatText.PromptTarget + " px");
            sb.AppendLine("  old row     " + BeatText.BaseRow + " px  -> target " +
                          BeatText.RowTarget + " px");
            sb.AppendLine("  old subtitle " + BeatText.BaseSubtitle + " px -> target " +
                          BeatText.SubtitleTarget + " px");
            sb.AppendLine("  old label   " + BeatText.BaseHud + " px -> target " +
                          BeatText.HudTarget + " px");
            sb.AppendLine();
            sb.AppendLine("block budget  " + BeatText.MaxBlockFraction + " of height = " +
                          BeatText.MaxBlock.ToString("0") + " px");

            float w = BeatText.PromptWidth(0.90f);

            sb.AppendLine();
            sb.AppendLine("--- Mono's lines, at the subtitle width " +
                          w.ToString("0") + " px, target " +
                          BeatText.SubtitleTarget + " px ---");

            int lines = 0, short1 = 0;
            float worstRatio = 99f;
            string worst = "";

            var hints = FindAnyObjectByType<MonoHintLines>();
            if (hints != null && hints.lines != null)
            {
                for (int i = 0; i < hints.lines.Count; i++)
                    lines += One(sb, hints.lines[i].text, w,
                                 BeatText.SubtitleTarget,
                                 BeatText.BaseSubtitle,
                                 ref short1, ref worstRatio, ref worst);
            }

            sb.AppendLine();
            sb.AppendLine("--- serialized strings on beats (card width " +
                          BeatText.PromptWidth(0.82f).ToString("0") + " px, target " +
                          BeatText.PromptTarget + " px) ---");

            float cw = BeatText.PromptWidth(0.82f);

            for (int i = 0; i < extra.Count; i++)
                lines += One(sb, extra[i], cw, BeatText.PromptTarget,
                             BeatText.BasePrompt,
                             ref short1, ref worstRatio, ref worst);

            sb.AppendLine();
            sb.AppendLine("=== " + lines + " strings, " + short1 +
                          " of them could not reach the target ===");

            if (short1 == 0)
            {
                sb.AppendLine("every line reached 5x. That is a true statement " +
                              "only for the strings measured here.");
            }
            else
            {
                sb.AppendLine("worst shortfall: \"" + Snip(worst) + "\" reached " +
                              (worstRatio * 100f).ToString("0") + "% of target.");
            }

            sb.AppendLine();
            sb.AppendLine("this is the number that argues against the change:");

            try
            {
                var abs = Path.Combine(Directory.GetCurrentDirectory(), ReportPath);
                File.WriteAllText(abs, sb.ToString());
                Debug.Log("[Echoes] beat text audit -> " + ReportPath + "\n" + sb);
            }
            catch (System.Exception e)
            {
                // A failed write is still a measurement worth having.
                Debug.LogWarning("[Echoes] beat text audit could not write " +
                                 ReportPath + " (" + e.GetType().Name + "), but the " +
                                 "numbers are here:\n" + sb);
            }
        }

        int One(StringBuilder sb, string text, float width, int target,
                float basePx, ref int shortCount, ref float worstRatio,
                ref string worst)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var style = BeatText.Make(TextAnchor.MiddleCenter, target, true,
                                      BeatText.Ink);

            int fit = BeatText.Fit(style, text, width, BeatText.MaxBlock, target);
            float h = BeatText.Height(style, text, width);

            float lines2 = Mathf.Max(1f, Mathf.CeilToInt(h / Mathf.Max(1f,
                                     style.lineHeight)));

            float ratio = fit * 100f / target;
            float mult = basePx > 0f ? fit / basePx : 0f;

            sb.AppendLine("[" + fit + "/" + target + " px, " +
                          mult.ToString("0.00") + "x the old " + basePx +
                          " px, " + lines2.ToString("0") + " lines] " + Snip(text));

            if (fit < target)
            {
                shortCount++;
                if (ratio < worstRatio) { worstRatio = ratio; worst = text; }
            }

            return 1;
        }

        static string Snip(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\n", " ");
            return s.Length <= 54 ? s : s.Substring(0, 51) + "...";
        }
    }
}