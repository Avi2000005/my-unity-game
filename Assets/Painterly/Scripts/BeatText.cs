using System;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Beat Text")]
    public static class BeatText
    {
    	private struct Key
    	{
    		public string Text;

    		public float Width;

    		public float MaxHeight;

    		public int Target;

    		public bool Same(Key o)
    		{
    			if (o.Target == Target && Mathf.Abs(o.Width - Width) < 0.5f && Mathf.Abs(o.MaxHeight - MaxHeight) < 0.5f)
    			{
    				return string.Equals(o.Text, Text, StringComparison.Ordinal);
    			}
    			return false;
    		}
    	}

    	public const float Scale = 5f;

    	public const float BasePrompt = 30f;

    	public const float BaseRow = 22f;

    	public const float BaseSubtitle = 22f;

    	public const float BaseHud = 15f;

    	public const int MinFont = 10;

    	public const float MaxBlockFraction = 0.72f;

    	private static readonly Key[] _keys = new Key[8];

    	private static readonly int[] _sizes = new int[8];

    	private static int _next;

    	public static float ScreenScale => Mathf.Max(0.7f, (float)Screen.height / 720f);

    	public static int PromptTarget => Target(30f);

    	public static int RowTarget => Target(22f);

    	public static int SubtitleTarget => Target(22f);

    	public static int HudTarget => Target(15f);

    	public static Color Ink
    	{
    		get
    		{
    			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    			return new Color(0.95f, 0.95f, 0.93f);
    		}
    	}

    	public static Color InkBright
    	{
    		get
    		{
    			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    			return new Color(0.98f, 0.98f, 0.96f);
    		}
    	}

    	public static Color InkDim
    	{
    		get
    		{
    			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    			return new Color(0.84f, 0.84f, 0.82f);
    		}
    	}

    	public static Color InkFaint
    	{
    		get
    		{
    			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    			return new Color(0.72f, 0.72f, 0.7f);
    		}
    	}

    	public static Color Shadow
    	{
    		get
    		{
    			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    			return new Color(0f, 0f, 0f, 0.92f);
    		}
    	}

    	public static float MaxBlock => (float)Screen.height * 0.72f;

    	private static int Target(float basePx)
    	{
    		return Mathf.Max(10, Mathf.RoundToInt(basePx * 5f * ScreenScale));
    	}

    	public static GUIStyle Make(TextAnchor anchor, int fontSize, bool wordWrap, Color colour)
    	{
    		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003f: Expected Obj, but got Unknown
    		GUIStyle val = new GUIStyle(GUI.skin.label)
    		{
    			alignment = anchor,
    			fontSize = Mathf.Max(10, fontSize),
    			wordWrap = wordWrap,
    			richText = false
    		};
    		val.normal.textColor = colour;
    		return val;
    	}

    	public static GUIStyle MakeStandalone(TextAnchor anchor, int fontSize, bool wordWrap, Color colour)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Expected Obj, but got Unknown
    		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
    		GUIStyle val = new GUIStyle
    		{
    			alignment = anchor,
    			fontSize = Mathf.Max(10, fontSize),
    			wordWrap = wordWrap,
    			richText = false
    		};
    		if ((Object)(object)val.font == (Object)null)
    		{
    			Font builtinResource = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    			if ((Object)(object)builtinResource == (Object)null)
    			{
    				builtinResource = Resources.GetBuiltinResource<Font>("Arial.ttf");
    			}
    			if ((Object)(object)builtinResource != (Object)null)
    			{
    				val.font = builtinResource;
    			}
    		}
    		val.normal.textColor = colour;
    		return val;
    	}

    	public static int FitStandalone(string text, float width, float maxHeight, int target, TextAnchor anchor)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
    		return Fit(MakeStandalone(anchor, target, wordWrap: true, Ink), text, width, maxHeight, target);
    	}

    	public static List<string> ShortfallsStandalone(IList<string> lines, TextAnchor anchor, float width, float maxHeight, int target)
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
    		List<string> list = new List<string>();
    		GUIStyle probe = MakeStandalone(anchor, target, wordWrap: true, Ink);
    		for (int i = 0; i < lines.Count; i++)
    		{
    			string text = lines[i];
    			if (!string.IsNullOrEmpty(text))
    			{
    				int num = Fit(probe, text, width, maxHeight, target);
    				if (num < target)
    				{
    					list.Add(Report(i + 1, text, probe, num, target, maxHeight, width));
    				}
    			}
    		}
    		return list;
    	}

    	internal static string Report(int n, string s, GUIStyle probe, int fit, int target, float maxHeight, float width)
    	{
    		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0013: Expected Obj, but got Unknown
    		float num = Mathf.Max(1f, (float)Mathf.CeilToInt(probe.CalcHeight(new GUIContent(s), width) / Mathf.Max(1f, probe.lineHeight)));
    		return "  " + n + ". " + ((float)s.Length).ToString("0") + " chars, " + num.ToString("0") + " wrapped lines: asked " + target + " px, largest that fits " + maxHeight.ToString("0") + " px of height is " + fit + " px  (" + ((float)fit * 100f / (float)target).ToString("0") + "% of target)";
    	}

    	public static int Fit(GUIStyle probe, string text, float width, float maxHeight, int target)
    	{
    		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0098: Expected Obj, but got Unknown
    		if (string.IsNullOrEmpty(text) || width <= 1f)
    		{
    			return target;
    		}
    		for (int i = 0; i < _keys.Length; i++)
    		{
    			if (_keys[i].Same(new Key
    			{
    				Text = text,
    				Width = width,
    				MaxHeight = maxHeight,
    				Target = target
    			}))
    			{
    				return _sizes[i];
    			}
    		}
    		int num = 10;
    		int num2 = Mathf.Max(10, target);
    		int num3 = 10;
    		while (num <= num2)
    		{
    			int num4 = (probe.fontSize = (num + num2) / 2);
    			if (probe.CalcHeight(new GUIContent(text), width) <= maxHeight)
    			{
    				num3 = num4;
    				num = num4 + 1;
    			}
    			else
    			{
    				num2 = num4 - 1;
    			}
    		}
    		probe.fontSize = target;
    		_keys[_next] = new Key
    		{
    			Text = text,
    			Width = width,
    			MaxHeight = maxHeight,
    			Target = target
    		};
    		_sizes[_next] = num3;
    		_next = (_next + 1) % _keys.Length;
    		return num3;
    	}

    	public static float Height(GUIStyle style, string text, float width)
    	{
    		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000d: Expected Obj, but got Unknown
    		return style.CalcHeight(new GUIContent(text), width);
    	}

    	public static GUIStyle Fitted(GUIStyle template, string text, float width, float maxHeight, int target)
    	{
    		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001a: Expected Obj, but got Unknown
    		int fontSize = Fit(template, text, width, maxHeight, target);
    		return new GUIStyle(template)
    		{
    			fontSize = fontSize
    		};
    	}

    	public static Rect Block(GUIStyle template, string text, float centreX, float bottomY, float width, float maxHeight, int target, out int usedSize)
    	{
    		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0020: Expected Obj, but got Unknown
    		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Expected Obj, but got Unknown
    		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004d: Expected Obj, but got Unknown
    		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
    		usedSize = Fit(template, text, width, maxHeight, target);
    		GUIStyle val = new GUIStyle(template)
    		{
    			fontSize = usedSize
    		};
    		float num = val.CalcHeight(new GUIContent(text), width);
    		Rect val2 = new Rect(centreX - width * 0.5f, bottomY - num, width, num);
    		GUIStyle val3 = new GUIStyle(val);
    		val3.normal.textColor = Shadow;
    		GUI.Label(new Rect(val2.x + 2f, val2.y + 2f, val2.width, val2.height), text, val3);
    		GUI.Label(val2, text, val);
    		return val2;
    	}

    	public static float PromptWidth(float fraction)
    	{
    		return Mathf.Min((float)Screen.width * fraction, (float)Screen.width - 24f);
    	}

    	public static List<string> Shortfalls(IList<string> lines, TextAnchor anchor, float width, float maxHeight, int target)
    	{
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0066: Expected Obj, but got Unknown
    		List<string> list = new List<string>(lines.Count);
    		GUIStyle val = Make(anchor, target, wordWrap: true, Ink);
    		for (int i = 0; i < lines.Count; i++)
    		{
    			string text = lines[i];
    			if (!string.IsNullOrEmpty(text))
    			{
    				int num = Fit(val, text, width, maxHeight, target);
    				if (num < target)
    				{
    					float num2 = text.Length;
    					float num3 = Mathf.Max(1f, (float)Mathf.CeilToInt(val.CalcHeight(new GUIContent(text), width) / Mathf.Max(1f, val.lineHeight)));
    					list.Add("  " + (i + 1) + ". " + num2.ToString("0") + " chars, " + num3.ToString("0") + " wrapped lines: asked " + target + " px, largest that fits " + maxHeight.ToString("0") + " px of height is " + num + " px  (" + ((float)num * 100f / (float)target).ToString("0") + "% of target)");
    				}
    			}
    		}
    		return list;
    	}
    }
}