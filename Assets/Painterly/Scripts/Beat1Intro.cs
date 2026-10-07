// Level 1 Intro Sequence & Controls Tutorial
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{
    [AddComponentMenu("Echoes/Beat 1 Intro")]
    public sealed class Beat1Intro : MonoBehaviour, IResettable
    {
        public enum Phase
        {
            WelcomeAndControls,
            ChallengesBriefing,
            StoryCard,
            Done
        }

        public static bool IntroActive { get; private set; } = true;

        public Phase Now { get; private set; } = Phase.WelcomeAndControls;
        public bool CardUp => IntroActive;
        public float Seconds { get; private set; }

        private float _phaseTimer = 0f;
        private float _allTestedTimer = 0f;

        // Button testing states
        private bool _testedMove = false;
        private bool _testedRun = false;
        private bool _testedJump = false;
        private bool _testedBrush = false;
        private bool _testedInteract = false;
        private bool _testedLook = false;

        public bool AllTested => _testedMove && _testedRun && _testedJump && _testedBrush && _testedInteract && _testedLook;

        // Legacy serialized fields preserved for scene backwards compatibility
        [Header("The introduction")]
        [TextArea(2, 6)]
        [SerializeField]
        private string introText = "Ari has never seen a colour.\n\nEverything here is black or white, and he has a brush anyway — because everything he likes, he tries to paint.";

        [SerializeField]
        private string introPrompt = "Press E to begin.";

        [Header("The buttons")]
        [SerializeField]
        private string[] keyGlyphs = new string[6] { "W A S D", "SHIFT", "SPACE", "LEFT CLICK", "E / F", "MOUSE LOOK" };

        [SerializeField]
        private string[] keyLabels = new string[6] { "Move in any direction", "Sprint / Run fast", "Jump", "Swing the brush", "Interact", "Look around 360" };

        [Header("Behaviour")]
        [Min(0f)]
        [SerializeField]
        private float introLockSeconds = 0.5f;

        [SerializeField]
        private bool log = true;

        private Vector3 _entry = Vector3.zero;
        public Vector3 EntryPoint => _entry;

        // GUI assets
        private Texture2D _whiteTex;
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _challengeTitleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _tagStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _skipButtonStyle;
        private int _cachedHeight = -1;

        private void Awake()
        {
            IntroActive = true;
            Now = Phase.WelcomeAndControls;
            _phaseTimer = 0f;
            _allTestedTimer = 0f;
            _testedMove = false;
            _testedRun = false;
            _testedJump = false;
            _testedBrush = false;
            _testedInteract = false;
            _testedLook = false;
        }

        private void OnEnable()
        {
            IntroActive = true;
            Now = Phase.WelcomeAndControls;
            _phaseTimer = 0f;
            _allTestedTimer = 0f;
        }

        private void Update()
        {
            if (!IntroActive || Now == Phase.Done)
            {
                return;
            }

            Seconds += Time.unscaledDeltaTime;
            _phaseTimer += Time.unscaledDeltaTime;

            // Keep Ari completely frozen during intro
            if (AriMover.I != null)
            {
                AriMover.I.Frozen = true;
            }

            // Ensure cursor is free to click buttons
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            switch (Now)
            {
                case Phase.WelcomeAndControls:
                    PollControlsInput();

                    if (AllTested)
                    {
                        _allTestedTimer += Time.unscaledDeltaTime;
                        if (_allTestedTimer >= 0.75f || IsActionAdvancePressed())
                        {
                            AdvanceTo(Phase.ChallengesBriefing);
                        }
                    }

                    if (IsSkipPressed())
                    {
                        AdvanceTo(Phase.ChallengesBriefing);
                    }
                    break;

                case Phase.ChallengesBriefing:
                    if (_phaseTimer >= introLockSeconds && (IsActionAdvancePressed() || IsInteractPressed()))
                    {
                        AdvanceTo(Phase.StoryCard);
                    }
                    else if (IsSkipPressed())
                    {
                        FinishIntro();
                    }
                    break;

                case Phase.StoryCard:
                    if (_phaseTimer >= introLockSeconds && (IsActionAdvancePressed() || IsInteractPressed()))
                    {
                        FinishIntro();
                    }
                    break;
            }
        }

        private void AdvanceTo(Phase next)
        {
            Now = next;
            _phaseTimer = 0f;
            if (log)
            {
                Debug.Log("[Echoes] Beat 1 Intro transitioned to: " + next, this);
            }
        }

        public void FinishIntro()
        {
            IntroActive = false;
            Now = Phase.Done;

            if (AriMover.I != null)
            {
                AriMover.I.Frozen = false;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (log)
            {
                Debug.Log("[Echoes] Beat 1 Intro completed. Player is free to explore!", this);
            }
        }

        private void PollControlsInput()
        {
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad gp = Gamepad.current;

            if (kb != null)
            {
                if (kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ||
                    kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame ||
                    kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed)
                {
                    _testedMove = true;
                }

                if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame || kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
                {
                    _testedRun = true;
                }

                if (kb.spaceKey.wasPressedThisFrame || kb.spaceKey.isPressed)
                {
                    _testedJump = true;
                }

                if (kb.eKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)
                {
                    _testedInteract = true;
                }
            }

            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame || mouse.leftButton.isPressed)
                {
                    _testedBrush = true;
                }

                Vector2 delta = mouse.delta.ReadValue();
                if (delta.sqrMagnitude > 4.0f)
                {
                    _testedLook = true;
                }
            }

            if (gp != null)
            {
                if (gp.leftStick.ReadValue().sqrMagnitude > 0.15f) _testedMove = true;
                if (gp.leftStickButton.wasPressedThisFrame || gp.rightTrigger.isPressed) _testedRun = true;
                if (gp.buttonSouth.wasPressedThisFrame) _testedJump = true;
                if (gp.buttonWest.wasPressedThisFrame) _testedBrush = true;
                if (gp.buttonNorth.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame) _testedInteract = true;
                if (gp.rightStick.ReadValue().sqrMagnitude > 0.15f) _testedLook = true;
            }
        }

        private static bool IsInteractPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.eKey.wasPressedThisFrame || kb.fKey.wasPressedThisFrame)) return true;
            return Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false;
        }

        private static bool IsActionAdvancePressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) return true;
            return Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false;
        }

        private static bool IsSkipPressed()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.tabKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)) return true;
            return Gamepad.current?.buttonEast.wasPressedThisFrame ?? false;
        }

        public string CardText() => "";
        public string CardPrompt() => "";

        private void EnsureStyles()
        {
            if (_whiteTex == null)
            {
                _whiteTex = new Texture2D(1, 1);
                _whiteTex.SetPixel(0, 0, Color.white);
                _whiteTex.Apply();
            }

            if (_headerStyle == null || _cachedHeight != Screen.height)
            {
                _cachedHeight = Screen.height;
                float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 1.4f);

                _headerStyle = BeatText.Make(TextAnchor.MiddleCenter, Mathf.RoundToInt(30f * scale), true, new Color(0.35f, 0.85f, 1f));
                _subHeaderStyle = BeatText.Make(TextAnchor.MiddleCenter, Mathf.RoundToInt(17f * scale), false, new Color(0.85f, 0.88f, 0.95f));
                _challengeTitleStyle = BeatText.Make(TextAnchor.MiddleLeft, Mathf.RoundToInt(18f * scale), true, new Color(1f, 0.88f, 0.4f));
                _bodyStyle = BeatText.Make(TextAnchor.MiddleLeft, Mathf.RoundToInt(15f * scale), false, new Color(0.88f, 0.90f, 0.95f));
                _bodyStyle.wordWrap = true;

                _tagStyle = BeatText.Make(TextAnchor.MiddleCenter, Mathf.RoundToInt(14f * scale), true, Color.white);

                _buttonStyle = new GUIStyle(GUI.skin.button);
                _buttonStyle.fontSize = Mathf.RoundToInt(16f * scale);
                _buttonStyle.fontStyle = FontStyle.Bold;
                _buttonStyle.alignment = TextAnchor.MiddleCenter;
                _buttonStyle.normal.textColor = Color.white;

                _skipButtonStyle = new GUIStyle(GUI.skin.button);
                _skipButtonStyle.fontSize = Mathf.RoundToInt(14f * scale);
                _skipButtonStyle.fontStyle = FontStyle.Normal;
                _skipButtonStyle.alignment = TextAnchor.MiddleCenter;
                _skipButtonStyle.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
            }
        }

        private void OnGUI()
        {
            if (!IntroActive || Now == Phase.Done) return;

            EnsureStyles();

            // Background cinematic tint
            Color prevColor = GUI.color;
            GUI.color = new Color(0.02f, 0.03f, 0.05f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _whiteTex);
            GUI.color = prevColor;

            // Come-in animation: smooth ease-out vertical slide and alpha
            float animProgress = Mathf.Clamp01(_phaseTimer / 0.4f);
            float ease = 1f - Mathf.Pow(1f - animProgress, 3f);
            float animOffsetY = (1f - ease) * 28f;

            switch (Now)
            {
                case Phase.WelcomeAndControls:
                    DrawWelcomeAndControls(animOffsetY, ease);
                    break;
                case Phase.ChallengesBriefing:
                    DrawChallengesBriefing(animOffsetY, ease);
                    break;
                case Phase.StoryCard:
                    DrawStoryCard(animOffsetY, ease);
                    break;
            }
        }

        private void DrawWelcomeAndControls(float offsetY, float alpha)
        {
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 1.4f);
            float boxW = Mathf.Min(Screen.width * 0.85f, 880f * scale);
            float boxH = 580f * scale;

            float boxX = (Screen.width - boxW) * 0.5f;
            float boxY = (Screen.height - boxH) * 0.5f + offsetY;

            Rect cardRect = new Rect(boxX, boxY, boxW, boxH);
            DrawGlassPanel(cardRect, new Color(0.06f, 0.08f, 0.12f, 0.94f * alpha), new Color(0.2f, 0.6f, 1f, 0.6f * alpha));

            // Header Banner
            Rect titleRect = new Rect(boxX + 20f, boxY + 20f * scale, boxW - 40f, 40f * scale);
            GUI.Label(titleRect, "✦  WELCOME TO LEVEL 1  ✦", _headerStyle);

            Rect subRect = new Rect(boxX + 20f, boxY + 62f * scale, boxW - 40f, 26f * scale);
            GUI.Label(subRect, "Test your controls below to prepare Ari for the adventure, or skip anytime:", _subHeaderStyle);

            // Controls checklist
            float startY = boxY + 104f * scale;
            float rowH = 46f * scale;
            float gap = 8f * scale;

            DrawControlRow(boxX + 35f * scale, startY + (rowH + gap) * 0, boxW - 70f * scale, rowH, "W  A  S  D", "Move Ari across the screen (any direction)", _testedMove, scale);
            DrawControlRow(boxX + 35f * scale, startY + (rowH + gap) * 1, boxW - 70f * scale, rowH, "SHIFT", "Sprint / Run fast (essential to evade crawlers)", _testedRun, scale);
            DrawControlRow(boxX + 35f * scale, startY + (rowH + gap) * 2, boxW - 70f * scale, rowH, "SPACE", "Jump over obstacles & escape traps", _testedJump, scale);
            DrawControlRow(boxX + 35f * scale, startY + (rowH + gap) * 3, boxW - 70f * scale, rowH, "LEFT CLICK", "Swing painter's brush (paint objects & awaken magic)", _testedBrush, scale);
            DrawControlRow(boxX + 35f * scale, startY + (rowH + gap) * 4, boxW - 70f * scale, rowH, "E  /  F", "Interact with objects, characters, and fragments", _testedInteract, scale);
            DrawControlRow(boxX + 35f * scale, startY + (rowH + gap) * 5, boxW - 70f * scale, rowH, "MOUSE", "Free 360° look around the village", _testedLook, scale);

            // Status message & Actions
            float btnY = boxY + boxH - 64f * scale;
            float btnH = 44f * scale;

            int testedCount = (_testedMove ? 1 : 0) + (_testedRun ? 1 : 0) + (_testedJump ? 1 : 0) + (_testedBrush ? 1 : 0) + (_testedInteract ? 1 : 0) + (_testedLook ? 1 : 0);

            if (AllTested)
            {
                Rect proceedRect = new Rect(boxX + boxW * 0.5f - 180f * scale, btnY, 360f * scale, btnH);
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f, 1f);
                if (GUI.Button(proceedRect, "✓ All Tested! Proceed (Press SPACE)", _buttonStyle))
                {
                    AdvanceTo(Phase.ChallengesBriefing);
                }
                GUI.backgroundColor = old;
            }
            else
            {
                Rect statusRect = new Rect(boxX + 40f * scale, btnY + 8f * scale, 300f * scale, 30f * scale);
                GUI.Label(statusRect, $"Progress: {testedCount} / 6 controls tested", _subHeaderStyle);

                Rect skipRect = new Rect(boxX + boxW - 270f * scale, btnY, 230f * scale, btnH);
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.25f, 0.28f, 0.35f, 0.9f);
                if (GUI.Button(skipRect, "Skip Tutorial (TAB)", _skipButtonStyle))
                {
                    AdvanceTo(Phase.ChallengesBriefing);
                }
                GUI.backgroundColor = old;
            }
        }

        private void DrawControlRow(float x, float y, float w, float h, string keyBadge, string description, bool tested, float scale)
        {
            Rect rowRect = new Rect(x, y, w, h);
            Color bg = tested ? new Color(0.08f, 0.24f, 0.16f, 0.75f) : new Color(0.11f, 0.13f, 0.18f, 0.65f);
            Color border = tested ? new Color(0.2f, 0.9f, 0.45f, 0.8f) : new Color(0.25f, 0.30f, 0.40f, 0.45f);
            DrawGlassPanel(rowRect, bg, border);

            // Key badge
            float badgeW = 140f * scale;
            Rect badgeRect = new Rect(x + 10f * scale, y + 6f * scale, badgeW, h - 12f * scale);
            DrawGlassPanel(badgeRect, tested ? new Color(0.15f, 0.45f, 0.25f, 0.9f) : new Color(0.20f, 0.24f, 0.32f, 0.9f), border);

            Color prev = GUI.color;
            _tagStyle.normal.textColor = tested ? new Color(0.4f, 1f, 0.6f) : Color.white;
            GUI.Label(badgeRect, keyBadge, _tagStyle);

            // Description
            Rect descRect = new Rect(x + badgeW + 22f * scale, y, w - badgeW - 130f * scale, h);
            GUIStyle descStyle = new GUIStyle(_bodyStyle);
            descStyle.alignment = TextAnchor.MiddleLeft;
            descStyle.normal.textColor = tested ? new Color(0.95f, 0.98f, 0.95f) : new Color(0.75f, 0.78f, 0.85f);
            GUI.Label(descRect, description, descStyle);

            // Checkmark badge
            Rect checkRect = new Rect(x + w - 95f * scale, y + 8f * scale, 85f * scale, h - 16f * scale);
            if (tested)
            {
                DrawGlassPanel(checkRect, new Color(0.12f, 0.55f, 0.25f, 0.9f), new Color(0.3f, 1f, 0.5f, 0.8f));
                _tagStyle.normal.textColor = Color.white;
                GUI.Label(checkRect, "✓ READY", _tagStyle);
            }
            else
            {
                DrawGlassPanel(checkRect, new Color(0.18f, 0.20f, 0.26f, 0.5f), new Color(0.4f, 0.45f, 0.55f, 0.35f));
                _tagStyle.normal.textColor = new Color(0.55f, 0.6f, 0.7f);
                GUI.Label(checkRect, "PRESS KEY", _tagStyle);
            }
            GUI.color = prev;
        }

        private void DrawChallengesBriefing(float offsetY, float alpha)
        {
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 1.4f);
            float boxW = Mathf.Min(Screen.width * 0.85f, 880f * scale);
            float boxH = 560f * scale;

            float boxX = (Screen.width - boxW) * 0.5f;
            float boxY = (Screen.height - boxH) * 0.5f + offsetY;

            Rect cardRect = new Rect(boxX, boxY, boxW, boxH);
            DrawGlassPanel(cardRect, new Color(0.06f, 0.08f, 0.14f, 0.95f * alpha), new Color(1f, 0.75f, 0.25f, 0.65f * alpha));

            // Header Banner
            Rect titleRect = new Rect(boxX + 20f, boxY + 22f * scale, boxW - 40f, 40f * scale);
            _headerStyle.normal.textColor = new Color(1f, 0.82f, 0.35f);
            GUI.Label(titleRect, "✦  LEVEL 1 : CHALLENGES & OBJECTIVES  ✦", _headerStyle);

            Rect subRect = new Rect(boxX + 20f, boxY + 64f * scale, boxW - 40f, 26f * scale);
            GUI.Label(subRect, "Master Painter's Briefing — Complete these goals to restore the village:", _subHeaderStyle);

            // Challenge Cards
            float startY = boxY + 105f * scale;

            // 1. Broken Tree
            DrawChallengeItem(boxX + 35f * scale, startY, boxW - 70f * scale, 95f * scale,
                "1. The Broken Sleeping Tree",
                "Find the withered, monochrome tree nearby and paint it with your brush [Left Click]. One surprise is waiting there — a magical floating companion named Mono!",
                new Color(0.4f, 0.95f, 0.55f), scale);

            // 2. Glowing Water Fragment
            DrawChallengeItem(boxX + 35f * scale, startY + 105f * scale, boxW - 70f * scale, 95f * scale,
                "2. The Glowing Water Fragment & Fountain",
                "Awaken Mono, then search the outer ruins near the eastern wall for the glowing Blue Water Fragment. Pick it up with [E] and fix it into the dry village fountain!",
                new Color(0.35f, 0.8f, 1f), scale);

            // 3. Crawler Warning
            DrawChallengeItem(boxX + 35f * scale, startY + 210f * scale, boxW - 70f * scale, 95f * scale,
                "3. Danger: Corrupted Ink Crawlers",
                "Be careful! Corrupted ink crawlers roam the village. You cannot attack them with color yet — sprint [Shift] to avoid their strikes and stay alive! Best of luck!",
                new Color(1f, 0.45f, 0.45f), scale);

            // Action Buttons
            float btnY = boxY + boxH - 68f * scale;
            float btnH = 46f * scale;

            Rect contRect = new Rect(boxX + boxW * 0.5f - 170f * scale, btnY, 340f * scale, btnH);
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.65f, 1f, 1f);
            if (GUI.Button(contRect, "Continue (Press E or ENTER)", _buttonStyle))
            {
                AdvanceTo(Phase.StoryCard);
            }
            GUI.backgroundColor = old;

            Rect skipRect = new Rect(boxX + boxW - 200f * scale, btnY, 160f * scale, btnH);
            GUI.backgroundColor = new Color(0.25f, 0.28f, 0.35f, 0.85f);
            if (GUI.Button(skipRect, "Skip to Game (TAB)", _skipButtonStyle))
            {
                FinishIntro();
            }
            GUI.backgroundColor = old;
        }

        private void DrawChallengeItem(float x, float y, float w, float h, string title, string description, Color accentColor, float scale)
        {
            Rect rect = new Rect(x, y, w, h);
            DrawGlassPanel(rect, new Color(0.08f, 0.10f, 0.15f, 0.85f), new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f));

            // Accent bar on left edge
            Color prev = GUI.color;
            GUI.color = accentColor;
            GUI.DrawTexture(new Rect(x, y, 5f * scale, h), _whiteTex);
            GUI.color = prev;

            // Title
            _challengeTitleStyle.normal.textColor = accentColor;
            GUI.Label(new Rect(x + 18f * scale, y + 10f * scale, w - 30f * scale, 24f * scale), title, _challengeTitleStyle);

            // Description
            GUI.Label(new Rect(x + 18f * scale, y + 36f * scale, w - 30f * scale, h - 42f * scale), description, _bodyStyle);
        }

        private void DrawStoryCard(float offsetY, float alpha)
        {
            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 1.4f);
            float boxW = Mathf.Min(Screen.width * 0.75f, 760f * scale);
            float boxH = 340f * scale;

            float boxX = (Screen.width - boxW) * 0.5f;
            float boxY = (Screen.height - boxH) * 0.55f + offsetY;

            Rect cardRect = new Rect(boxX, boxY, boxW, boxH);
            DrawGlassPanel(cardRect, new Color(0.05f, 0.05f, 0.08f, 0.94f * alpha), new Color(0.5f, 0.55f, 0.65f, 0.55f * alpha));

            // Story Text
            float textW = boxW - 60f * scale;
            GUIStyle storyStyle = new GUIStyle(_bodyStyle);
            storyStyle.fontSize = Mathf.RoundToInt(20f * scale);
            storyStyle.alignment = TextAnchor.MiddleCenter;
            storyStyle.normal.textColor = new Color(0.95f, 0.96f, 0.98f);

            string story = "Ari has never seen a colour.\n\nEverything here is black or white, and he has a brush anyway — because everything he likes, he tries to paint.";
            GUI.Label(new Rect(boxX + 30f * scale, boxY + 36f * scale, textW, 160f * scale), story, storyStyle);

            // Action Button
            float btnW = 280f * scale;
            float btnH = 50f * scale;
            Rect beginRect = new Rect(boxX + (boxW - btnW) * 0.5f, boxY + boxH - 85f * scale, btnW, btnH);

            Color old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.75f, 1f, 1f);
            if (GUI.Button(beginRect, "Press E to Begin", _buttonStyle))
            {
                FinishIntro();
            }
            GUI.backgroundColor = old;
        }

        private void DrawGlassPanel(Rect rect, Color fill, Color border)
        {
            Color prev = GUI.color;

            // Fill
            GUI.color = fill;
            GUI.DrawTexture(rect, _whiteTex);

            // Borders
            GUI.color = border;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1.5f), _whiteTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1.5f, rect.width, 1.5f), _whiteTex);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1.5f, rect.height), _whiteTex);
            GUI.DrawTexture(new Rect(rect.x + rect.width - 1.5f, rect.y, 1.5f, rect.height), _whiteTex);

            GUI.color = prev;
        }

        public void ResetForCheckpoint()
        {
            IntroActive = true;
            Now = Phase.WelcomeAndControls;
            Seconds = 0f;
            _phaseTimer = 0f;
            _allTestedTimer = 0f;
            _testedMove = false;
            _testedRun = false;
            _testedJump = false;
            _testedBrush = false;
            _testedInteract = false;
            _testedLook = false;
            _entry = Vector3.zero;
        }
    }
}
