using System;
using System.Collections.Generic;
using UnityEngine;

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

        public const float Scale = 1f;
        public const float BasePrompt = 22f;
        public const float BaseRow = 15f;
        public const float BaseSubtitle = 18f;
        public const float BaseHud = 14f;
        public const int MinFont = 10;

        // Keep it restrained so text never covers the screen (at most 22%):
        public static readonly float MaxBlockFraction = 0.22f;

        public static float ScreenScale => Mathf.Clamp((float)Screen.height / 720f, 0.8f, 1.4f);

        public static int PromptTarget => Target(BasePrompt);
        public static int RowTarget => Target(BaseRow);
        public static int SubtitleTarget => Target(BaseSubtitle);
        public static int HintTarget => Target(16f);
        public static int HudTarget => Target(BaseHud);

        private static int Target(float basePx)
        {
            return Mathf.Max(12, Mathf.RoundToInt(basePx * ScreenScale));
        }

        public static Color Ink => new Color(0.95f, 0.95f, 0.93f);
        public static Color InkBright => new Color(0.98f, 0.98f, 0.96f);
        public static Color InkDim => new Color(0.84f, 0.84f, 0.82f);
        public static Color InkFaint => new Color(0.72f, 0.72f, 0.7f);
        public static Color Shadow => new Color(0f, 0f, 0f, 0.92f);

        public static float MaxBlock => (float)Screen.height * MaxBlockFraction;

        public static float PromptWidth(float fraction = 0.82f)
        {
            return Mathf.Min((float)Screen.width * fraction, 860f);
        }

        public static GUIStyle Make(TextAnchor anchor, int fontSize, bool wordWrap, Color colour)
        {
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
            GUIStyle val = new GUIStyle
            {
                alignment = anchor,
                fontSize = Mathf.Max(10, fontSize),
                wordWrap = wordWrap,
                richText = false
            };
            if (val.font == null)
            {
                Font builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (builtin == null) builtin = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (builtin != null) val.font = builtin;
            }
            val.normal.textColor = colour;
            return val;
        }

        public static int Fit(GUIStyle style, string text, float width, float maxHeight, int target)
        {
            if (string.IsNullOrEmpty(text)) return target;
            return target;
        }

        public static int FitStandalone(string text, float width, float maxHeight, int target, TextAnchor anchor)
        {
            return Fit(MakeStandalone(anchor, target, true, Ink), text, width, maxHeight, target);
        }

        /// <summary>
        /// Returns how many lines of text at <paramref name="target"/> font size fit inside
        /// <paramref name="maxHeight"/> pixels, using the given <paramref name="style"/>.
        /// Used by editor probes to report line capacity at a given resolution.
        /// </summary>
        public static int LinesThatFit(GUIStyle style, float width, float maxHeight, int target)
        {
            if (style == null || maxHeight <= 0f) return 0;
            // Use a single-line sample to get the real line height at the target font size.
            GUIStyle probe = new GUIStyle(style) { fontSize = Mathf.Max(10, target), wordWrap = false };
            float lineH = probe.CalcHeight(new GUIContent("Mg"), width);
            if (lineH <= 0f) return 0;
            return Mathf.Max(1, Mathf.FloorToInt(maxHeight / lineH));
        }

        public static GUIStyle Fitted(GUIStyle baseStyle, string text, float width, float maxHeight, int target)
        {
            GUIStyle val = new GUIStyle(baseStyle);
            val.fontSize = target;
            return val;
        }

        public static float Height(GUIStyle style, string text, float width)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            return style.CalcHeight(new GUIContent(text), width);
        }

        public static void Block(GUIStyle style, string text, float centerX, float centerY, float width, float maxHeight, int target, out Rect rect)
        {
            float h = Mathf.Min(Height(style, text, width) + 16f, maxHeight);
            rect = new Rect(centerX - width * 0.5f, centerY - h * 0.5f, width, h);
            
            Color prev = GUI.color;
            GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.85f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            
            GUIStyle shadow = new GUIStyle(style);
            shadow.normal.textColor = Shadow;
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, shadow);
            GUI.Label(rect, text, style);
            GUI.color = prev;
        }

        public static List<string> Pages(GUIStyle style, string text, float width, float maxHeight, int target)
        {
            List<string> pages = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                pages.Add(string.Empty);
                return pages;
            }
            pages.Add(text);
            return pages;
        }

        public static float PageSeconds = 4.0f;
        private static string _pagedKey;
        private static int _pagedIndex;
        private static float _pagedAt;

        public static int PageOf(string text, List<string> pages)
        {
            if (pages == null || pages.Count == 0) return 0;
            if (pages.Count == 1) return 0;
            if (!string.Equals(_pagedKey, text))
            {
                _pagedKey = text;
                _pagedIndex = 0;
                _pagedAt = Time.time;
            }
            else if (Application.isPlaying && Time.time - _pagedAt >= PageSeconds)
            {
                if (_pagedIndex < pages.Count - 1) _pagedIndex++;
                _pagedAt = Time.time;
            }
            return Mathf.Clamp(_pagedIndex, 0, pages.Count - 1);
        }

        public static void ForgetPages()
        {
            _pagedKey = null;
            _pagedIndex = 0;
            _pagedAt = 0f;
        }

        public static List<string> Shortfalls(IList<string> lines, TextAnchor anchor, float width, float maxHeight, int target)
        {
            return new List<string>();
        }

        public static List<string> ShortfallsStandalone(IList<string> lines, TextAnchor anchor, float width, float maxHeight, int target)
        {
            return new List<string>();
        }
    }
}
