using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Beat Text Audit")]
    public sealed class BeatTextAudit : MonoBehaviour
    {
    	private const string ReportPath = "Temp/beattext_audit.txt";

    	private const float FreshSeconds = 600f;

    	[Tooltip("Extra strings to measure, one per entry. Blank entries are skipped. These are usually the serialized fields on beats, which are the longest text in the level and the ones least likely to be in a dialogue asset.")]
    	[TextArea(3, 12)]
    	[SerializeField]
    	private List<string> extra = new List<string>();

    	private bool _done;

    	public void Add(string text)
    	{
    		if (!string.IsNullOrEmpty(text))
    		{
    			if (extra == null)
    			{
    				extra = new List<string>();
    			}
    			if (!extra.Contains(text))
    			{
    				extra.Add(text);
    			}
    		}
    	}

    	private void Update()
    	{
    		if (!File.Exists("Temp/beattext_audit.txt") || !(Time.realtimeSinceStartup < 600f))
    		{
    			Object.Destroy((Object)(object)((Component)this).gameObject);
    		}
    	}

    	private void OnGUI()
    	{
    		if (_done)
    		{
    			return;
    		}
    		_done = true;
    		StringBuilder stringBuilder = new StringBuilder();
    		stringBuilder.AppendLine("[Echoes] beat text audit — measured inside OnGUI, so");
    		stringBuilder.AppendLine("        these numbers are the real skin at the real");
    		stringBuilder.AppendLine("        resolution. Compare with the out-of-play");
    		stringBuilder.AppendLine("        estimate in Temp/l1_fixups.txt.");
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("screen        " + Screen.width + " x " + Screen.height);
    		stringBuilder.AppendLine("scale         " + BeatText.ScreenScale.ToString("0.00") + "  (Screen.height / 720)");
    		stringBuilder.AppendLine("asked for     " + 5f + "x the old sizes");
    		stringBuilder.AppendLine("  old prompt  " + 30f + " px  -> target " + BeatText.PromptTarget + " px");
    		stringBuilder.AppendLine("  old row     " + 22f + " px  -> target " + BeatText.RowTarget + " px");
    		stringBuilder.AppendLine("  old subtitle " + 22f + " px -> target " + BeatText.SubtitleTarget + " px");
    		stringBuilder.AppendLine("  old label   " + 15f + " px -> target " + BeatText.HudTarget + " px");
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("block budget  " + 0.72f + " of height = " + BeatText.MaxBlock.ToString("0") + " px");
    		float width = BeatText.PromptWidth(0.9f);
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- Mono's lines, at the subtitle width " + width.ToString("0") + " px, target " + BeatText.SubtitleTarget + " px ---");
    		int num = 0;
    		int shortCount = 0;
    		float worstRatio = 99f;
    		string worst = "";
    		MonoHintLines monoHintLines = Object.FindAnyObjectByType<MonoHintLines>();
    		if ((Object)(object)monoHintLines != (Object)null && monoHintLines.lines != null)
    		{
    			for (int i = 0; i < monoHintLines.lines.Count; i++)
    			{
    				num += One(stringBuilder, monoHintLines.lines[i].text, width, BeatText.SubtitleTarget, 22f, ref shortCount, ref worstRatio, ref worst);
    			}
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("--- serialized strings on beats (card width " + BeatText.PromptWidth(0.82f).ToString("0") + " px, target " + BeatText.PromptTarget + " px) ---");
    		float width2 = BeatText.PromptWidth(0.82f);
    		for (int j = 0; j < extra.Count; j++)
    		{
    			num += One(stringBuilder, extra[j], width2, BeatText.PromptTarget, 30f, ref shortCount, ref worstRatio, ref worst);
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("=== " + num + " strings, " + shortCount + " of them could not reach the target ===");
    		if (shortCount == 0)
    		{
    			stringBuilder.AppendLine("every line reached 5x. That is a true statement only for the strings measured here.");
    		}
    		else
    		{
    			stringBuilder.AppendLine("worst shortfall: \"" + Snip(worst) + "\" reached " + (worstRatio * 100f).ToString("0") + "% of target.");
    		}
    		stringBuilder.AppendLine();
    		stringBuilder.AppendLine("this is the number that argues against the change:");
    		try
    		{
    			File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Temp/beattext_audit.txt"), stringBuilder.ToString());
    			Debug.Log((object)("[Echoes] beat text audit -> Temp/beattext_audit.txt\n" + stringBuilder));
    		}
    		catch (Exception ex)
    		{
    			Debug.LogWarning((object)("[Echoes] beat text audit could not write Temp/beattext_audit.txt (" + ex.GetType().Name + "), but the numbers are here:\n" + stringBuilder));
    		}
    	}

    	private int One(StringBuilder sb, string text, float width, int target, float basePx, ref int shortCount, ref float worstRatio, ref string worst)
    	{
    		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    		if (string.IsNullOrEmpty(text))
    		{
    			return 0;
    		}
    		GUIStyle val = BeatText.Make((TextAnchor)4, target, wordWrap: true, BeatText.Ink);
    		int num = BeatText.Fit(val, text, width, BeatText.MaxBlock, target);
    		float num2 = BeatText.Height(val, text, width);
    		float num3 = Mathf.Max(1f, (float)Mathf.CeilToInt(num2 / Mathf.Max(1f, val.lineHeight)));
    		float num4 = (float)num * 100f / (float)target;
    		float num5 = ((basePx > 0f) ? ((float)num / basePx) : 0f);
    		sb.AppendLine("[" + num + "/" + target + " px, " + num5.ToString("0.00") + "x the old " + basePx + " px, " + num3.ToString("0") + " lines] " + Snip(text));
    		if (num < target)
    		{
    			shortCount++;
    			if (num4 < worstRatio)
    			{
    				worstRatio = num4;
    				worst = text;
    			}
    		}
    		return 1;
    	}

    	private static string Snip(string s)
    	{
    		if (string.IsNullOrEmpty(s))
    		{
    			return "";
    		}
    		s = s.Replace("\n", " ");
    		if (s.Length > 54)
    		{
    			return s.Substring(0, 51) + "...";
    		}
    		return s;
    	}
    }
}