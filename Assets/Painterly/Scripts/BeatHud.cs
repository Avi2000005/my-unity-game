using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{
    [AddComponentMenu("Echoes/Beat HUD")]
    public sealed class BeatHud : MonoBehaviour
    {
        [Tooltip("Where the prompt sits, in normalised screen space.")]
        [SerializeField]
        private Vector2 promptAt = new Vector2(0.5f, 0.82f);

        [Tooltip("Draw the prompt at all.")]
        [SerializeField]
        private bool drawPrompt = true;

        private GUIStyle _style;
        private GUIStyle _cardStyle;
        private GUIStyle _rowStyle;
        private GUIStyle _tagStyle;
        private GUIStyle _pillStyle;
        private Texture2D _pillTex;

        private int _styleForHeight = -1;
        private const float Padding = 14f;

        [Range(0.3f, 0.98f)]
        [SerializeField]
        private float promptWidth = 0.7f;

        [Range(0.3f, 0.98f)]
        [SerializeField]
        private float cardWidth = 0.75f;

        private float MaxBlockHeight => (float)Screen.height * BeatText.MaxBlockFraction;

        private void Update()
        {
            BeatPrompt.PollSkip();
        }

        private void OnEnable()
        {
            BeatPrompt.ResetAll();
        }

        private void OnDisable()
        {
            BeatPrompt.ResetAll();
        }

        private void EnsureStyle()
        {
            if (_style == null || _styleForHeight != Screen.height)
            {
                _styleForHeight = Screen.height;
                _style = BeatText.Make(TextAnchor.MiddleCenter, BeatText.HintTarget, true, BeatText.Ink);
                _cardStyle = BeatText.Make(TextAnchor.MiddleCenter, BeatText.PromptTarget, true, BeatText.InkBright);
                _rowStyle = BeatText.Make(TextAnchor.MiddleCenter, BeatText.RowTarget, false, BeatText.InkDim);
                _tagStyle = BeatText.Make(TextAnchor.MiddleCenter, Mathf.Max(10, BeatText.RowTarget - 2), false, BeatText.InkFaint);
                _pillStyle = BeatText.Make(TextAnchor.MiddleCenter, BeatText.HintTarget, false, Color.white);
            }

            if (_pillTex == null)
            {
                _pillTex = new Texture2D(1, 1);
                _pillTex.SetPixel(0, 0, Color.white);
                _pillTex.Apply();
            }
        }

        private void DrawCard(string body, string prompt)
        {
            EnsureStyle();
            float maxWidth = Mathf.Min((float)Screen.width * cardWidth, 800f);

            var cardPages = BeatText.Pages(_cardStyle, body, maxWidth, MaxBlockHeight, BeatText.PromptTarget);
            string shown = (cardPages != null && cardPages.Count > 0) ? cardPages[BeatText.PageOf(body, cardPages)] : body;

            GUIStyle cardFont = new GUIStyle(_cardStyle)
            {
                fontSize = BeatText.PromptTarget
            };

            float textHeight = BeatText.Height(cardFont, shown, maxWidth);
            float promptHeight = string.IsNullOrEmpty(prompt) ? 0f : (cardFont.lineHeight + 8f);
            float totalBoxHeight = textHeight + promptHeight + 28f;
            float totalBoxWidth = maxWidth + 40f;

            float x = ((float)Screen.width - totalBoxWidth) * 0.5f;
            float y = (float)Screen.height * 0.75f - totalBoxHeight * 0.5f;
            Rect boxRect = new Rect(x, y, totalBoxWidth, totalBoxHeight);

            // Draw clean dark glass container
            Color prevColor = GUI.color;
            GUI.color = new Color(0.05f, 0.05f, 0.07f, 0.88f);
            GUI.DrawTexture(boxRect, _pillTex);

            // Draw subtle border
            GUI.color = new Color(0.35f, 0.35f, 0.4f, 0.5f);
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.y, boxRect.width, 1.5f), _pillTex);
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.y + boxRect.height - 1.5f, boxRect.width, 1.5f), _pillTex);
            GUI.color = prevColor;

            // Draw body text with soft shadow
            Rect textRect = new Rect(boxRect.x + 20f, boxRect.y + 12f, maxWidth, textHeight);
            GUIStyle shadowStyle = new GUIStyle(cardFont);
            shadowStyle.normal.textColor = BeatText.Shadow;
            GUI.Label(new Rect(textRect.x + 1f, textRect.y + 1f, textRect.width, textRect.height), shown, shadowStyle);
            GUI.Label(textRect, shown, cardFont);

            // Draw button prompt at bottom of card
            if (promptHeight > 0f)
            {
                Rect promptRect = new Rect(boxRect.x + 20f, boxRect.y + 16f + textHeight, maxWidth, promptHeight);
                GUI.Label(promptRect, prompt, _rowStyle);
            }
        }

        private void OnGUI()
        {
            if (Beat1Intro.IntroActive) return;
            if (!drawPrompt) return;

            Beat1Intro beat1Intro = Object.FindAnyObjectByType<Beat1Intro>(FindObjectsInactive.Include);
            string text = (beat1Intro != null) ? beat1Intro.CardText() : "";
            if (!string.IsNullOrEmpty(text))
            {
                DrawCard(text, beat1Intro.CardPrompt());
                return;
            }

            DrawPrompt();
            // Tutorial key rows ("> E / F interact") are intentionally suppressed here 
            // so they do not obstruct gameplay or characters.
        }

        private void DrawPrompt()
        {
            string current = BeatPrompt.Current;
            if (string.IsNullOrEmpty(current)) return;

            EnsureStyle();

            // Calculate compact pill badge for interaction hint
            GUIContent content = new GUIContent(current);
            Vector2 textSize = _pillStyle.CalcSize(content);

            float pillWidth = Mathf.Min(textSize.x + 36f, (float)Screen.width * 0.85f);
            float pillHeight = textSize.y + 16f;

            float x = ((float)Screen.width - pillWidth) * 0.5f;
            float y = (float)Screen.height * promptAt.y;
            Rect pillRect = new Rect(x, y, pillWidth, pillHeight);

            // Draw dark backdrop pill
            Color prev = GUI.color;
            GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.82f);
            GUI.DrawTexture(pillRect, _pillTex);

            // Subtle accent top line (cyan-blue tint)
            GUI.color = new Color(0.2f, 0.6f, 0.95f, 0.7f);
            GUI.DrawTexture(new Rect(pillRect.x + 10f, pillRect.y, pillRect.width - 20f, 2f), _pillTex);
            GUI.color = prev;

            // Text
            GUIStyle shadow = new GUIStyle(_pillStyle);
            shadow.normal.textColor = BeatText.Shadow;
            GUI.Label(new Rect(pillRect.x + 1f, pillRect.y + 1f, pillRect.width, pillRect.height), current, shadow);
            GUI.Label(pillRect, current, _pillStyle);
        }
    }
}
