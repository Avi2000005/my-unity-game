using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// One line of instruction, for whichever beat wants one.
    ///
    /// Static because there is only ever one prompt on screen and every beat
    /// needs to be able to set it without holding a reference to whatever
    /// draws it. A reference would work too, and would also mean that a beat
    /// loaded before the HUD — which is every beat, since the HUD sits on Ari
    /// and beats sit in the world — silently drops the one thing that tells the
    /// player what to do.
    ///
    /// Static state is wiped on play mode exit by the component below, so a
    /// prompt cannot survive into the next run and greet the player with
    /// instructions for a beat they have not reached.
    /// </summary>
    public static class BeatPrompt
    {
        static string _text = "";
        static float _until = -1f;
        static bool _sticky;

        /// <summary>What is on screen right now. Read by the HUD, and by probes.</summary>
        public static string Current =>
            _sticky || Time.time < _until ? _text : "";

        /// <summary>
        /// Show a line for a while.
        ///
        /// <paramref name="seconds"/> of zero or less makes it sticky — it stays
        /// until something replaces it or clears it. Sticky is the right default
        /// for anything the player has to act on, because a prompt that times
        /// out while the player is walking up to it is worse than no prompt: it
        /// reads as the game having decided they did not need it.
        /// </summary>
        public static void Show(string text, float seconds = 0f)
        {
            _text = text ?? "";
            _sticky = seconds <= 0f;
            _until = _sticky ? -1f : Time.time + seconds;
        }

        public static void Clear()
        {
            _text = "";
            _until = -1f;
            _sticky = false;
        }

        /// <summary>Wipe everything. Called when play mode stops.</summary>
        public static void ResetAll() => Clear();
    }

    /// <summary>
    /// Draws <see cref="BeatPrompt"/>, and nothing else.
    ///
    /// Separate from the beats so that the level has exactly one place where
    /// text is drawn. Ten beats each with their own OnGUI is ten copies of a
    /// font size, ten of a shadow style, and ten chances for two prompts to be
    /// on screen at once with the last one to Update winning — which is how a
    /// player ends up being told to touch a tree after they already have.
    ///
    /// OnGUI rather than uGUI or TextMeshPro. It needs no scene, no prefab, no
    /// font asset and no canvas, so it works in a scene that has none of those
    /// and cannot be broken by one that does. It is also the ugliest option and
    /// should be replaced when real UI arrives — the replacement is this file
    /// and the static class above it, and nothing else.
    /// </summary>
    [AddComponentMenu("Echoes/Beat HUD")]
    public sealed class BeatHud : MonoBehaviour
    {
        [Tooltip("Where the prompt sits, in normalised screen space. Low and " +
                 "centred: under the middle of the screen, clear of Ari and of " +
                 "the follow camera's horizon.")]
        [SerializeField] Vector2 promptAt = new Vector2(0.5f, 0.22f);

        [Tooltip("Draw the prompt at all. Off once real UI exists.")]
        [SerializeField] bool drawPrompt = true;

        GUIStyle _style;

        // Last text the height was measured for.
        //
        // CalcHeight is not free and OnGUI runs several times a frame, so the
        // measurement is cached against the string it was taken from. A prompt
        // that changes every beat is still a handful of distinct strings, and
        // the cache is wrong for at most one frame after a change — which is
        // indistinguishable, because the new text is being drawn anyway.
        string _measuredFor;
        float _measuredWidth;
        float _measuredHeight;
        int _styleForHeight = -1;

        /// <summary>
        /// Breathing room above and below the text inside its rect.
        ///
        /// CalcHeight returns the tight height of the glyphs. A rect exactly that
        /// tall puts the first and last line flush against the edge, and the
        /// descenders of a line that wrapped onto the extra one get sheared.
        /// </summary>
        const float Padding = 10f;

        void OnEnable()
        {
            // A prompt that outlives the session is a prompt that greets the
            // player with the previous run's instructions. Time.time resets on
            // entering play mode but statics do not.
            BeatPrompt.ResetAll();
        }

        void OnDisable() => BeatPrompt.ResetAll();

        /// <summary>
        /// How tall this text will actually be when wrapped to this width.
        ///
        /// Measured with CalcHeight, which is the same layout pass GUI.Label
        /// runs, so the number is the height the text wants rather than a
        /// guess from line count. Cached against the string and the width
        /// because OnGUI runs several times a frame.
        /// </summary>
        float MeasuredHeight(GUIStyle style, string text, float width)
        {
            if (_measuredFor != text || _measuredWidth != width || _measuredHeight <= 0f)
            {
                _measuredFor = text;
                _measuredWidth = width;
                _measuredHeight = style.CalcHeight(new GUIContent(text), width);
            }

            return _measuredHeight + Padding;
        }

        void OnGUI()
        {
            if (!drawPrompt) return;

            var text = BeatPrompt.Current;
            if (string.IsNullOrEmpty(text)) return;

            if (_style == null || _styleForHeight != Screen.height)
            {
                _styleForHeight = Screen.height;
                _measuredFor = null;      // the font size just changed under it

                _style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(19f * Mathf.Max(0.7f, Screen.height / 720f)),
                    wordWrap = true,
                    richText = false
                };
                _style.normal.textColor = new Color(0.80f, 0.80f, 0.78f);
            }

            float width = Mathf.Min(Screen.width * 0.62f, 760f);
            float height = MeasuredHeight(_style, text, width);

            // The rect is sized to the text, and then kept on screen.
            //
            // It used to be a fixed 60 px, which is one line at 720p and a
            // third of Mono's longest line at 1440p. GUI.Label clips to its
            // rect, so the tail of almost every line Mono says was simply not
            // drawn — measured at 36 px missing at 1080p and 115 px at 1440p.
            //
            // The clamp matters because the height is now a variable: a long
            // line at 1440p is 175 px tall, and promptAt.y of 0.22 on a short
            // window would push the bottom of it off the edge of the screen.
            float y = Mathf.Clamp(Screen.height * promptAt.y, 0f,
                                  Mathf.Max(0f, Screen.height - height - 8f));

            var rect = new Rect(
                (Screen.width - width) * promptAt.x,
                y,
                width, height);

            // Shadow, not a plate. The world is colourless and dim, so a solid
            // panel would be the highest-contrast thing on screen and would pull
            // the eye off Ari at exactly the moment the player should be
            // looking at the tree.
            var shadow = new GUIStyle(_style);
            shadow.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
            GUI.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height),
                      text, shadow);
            GUI.Label(rect, text, _style);
        }
    }
}
