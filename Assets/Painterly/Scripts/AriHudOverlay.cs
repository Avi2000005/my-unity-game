using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly
{
    [AddComponentMenu("Echoes/L1 HUD Overlay")]
    public sealed class AriHudOverlay : MonoBehaviour
    {
        [Header("Health bar")]
        [SerializeField]
        private Vector2 barAt = new Vector2(0.03f, 0.035f);

        [Range(0.1f, 0.45f)]
        [SerializeField]
        private float barWidth = 0.20f;

        [Min(4f)]
        [SerializeField]
        private float barHeight = 22f;

        [SerializeField]
        private bool drawBar = true;

        [SerializeField]
        private bool showNumber = true;

        [Header("Damage")]
        [SerializeField]
        private bool drawFlash = true;

        [Min(0f)]
        [SerializeField]
        private float flashPeak = 0.55f;

        [SerializeField]
        private bool pulseWhenLow = true;

        [Header("Fragment")]
        [SerializeField]
        private bool drawSlot = true;

        [SerializeField]
        private Vector2 slotAt = new Vector2(0.94f, 0.05f);

        [Min(16f)]
        [SerializeField]
        private float slotSize = 48f;

        public static readonly Color FragmentBlue = new Color(0.25f, 0.65f, 1f);

        private GUIStyle _label;
        private GUIStyle _small;
        private GUIStyle _titleStyle;
        private GUIStyle _defeatTitleStyle;
        private GUIStyle _victoryTitleStyle;

        private int _styleForHeight = -1;
        private Texture2D _white;

        [Header("Death")]
        [SerializeField]
        private bool drawLost = true;

        [Min(0f)]
        [SerializeField]
        private float lostDelay = 0.5f;

        [TextArea(2, 4)]
        [SerializeField]
        private string lostText = "Ari was overcome by the ink crawlers.";

        [TextArea(1, 3)]
        [SerializeField]
        private string retryText = "Press  ENTER  or  R  to retry from checkpoint";

        private float _lostAt = -1f;
        private float _victoryAt = -1f;
        private static bool _isRestarting = false;
        private int _retried;

        public static bool CarryingFragment { get; set; }
        public static bool ForceBar { get; set; }

        public int Retries => _retried;

        private void OnEnable()
        {
            _isRestarting = false;
            _victoryAt = -1f;
            _lostAt = -1f;
        }

        private void Update()
        {
            if (_isRestarting) return;

            if (LevelState.Completed)
            {
                if (_victoryAt < 0f)
                {
                    _victoryAt = Time.time + 0.4f;
                }
                if (Time.time >= _victoryAt && RetryPressed())
                {
                    RestartFullGame();
                }
            }
            else
            {
                AriHealth i = AriHealth.I;
                bool isDead = (i != null && i.IsDead) || LevelState.Lost;
                if (isDead)
                {
                    if (_lostAt < 0f)
                    {
                        _lostAt = Time.time + lostDelay;
                    }
                    if (Time.time >= _lostAt && RetryPressed())
                    {
                        _lostAt = -1f;
                        Retry();
                    }
                }
                else
                {
                    _lostAt = -1f;
                    _victoryAt = -1f;
                }
            }
        }

        private void OnGUI()
        {
            if (_white == null)
            {
                _white = new Texture2D(1, 1);
                _white.SetPixel(0, 0, Color.white);
                _white.Apply();
            }

            EnsureStyles();

            if (drawFlash)
            {
                Flash();
            }

            if (drawBar)
            {
                Bar();
            }

            if (drawSlot)
            {
                Slot();
            }

            if (LevelState.Completed)
            {
                Victory();
            }
            else if (drawLost)
            {
                Lost();
            }
        }

        private void EnsureStyles()
        {
            if (_label == null || _styleForHeight != Screen.height)
            {
                _styleForHeight = Screen.height;
                _label = BeatText.Make(TextAnchor.MiddleLeft, 14, false, new Color(0.95f, 0.95f, 0.95f));
                _small = BeatText.Make(TextAnchor.MiddleCenter, 13, false, new Color(0.9f, 0.9f, 0.9f));

                _defeatTitleStyle = BeatText.Make(TextAnchor.MiddleCenter, Mathf.RoundToInt(38f * BeatText.ScreenScale), false, new Color(0.95f, 0.22f, 0.22f));
                _victoryTitleStyle = BeatText.Make(TextAnchor.MiddleCenter, Mathf.RoundToInt(38f * BeatText.ScreenScale), false, new Color(0.3f, 0.85f, 1f));
                _titleStyle = BeatText.Make(TextAnchor.MiddleCenter, Mathf.RoundToInt(20f * BeatText.ScreenScale), false, new Color(0.95f, 0.95f, 0.95f));
            }
        }

        private void Bar()
        {
            AriHealth i = AriHealth.I;
            if (i == null) return;

            float fraction = i.Fraction;
            bool isLow = i.IsLow;

            float screenW = Screen.width;
            float screenH = Screen.height;
            float totalW = Mathf.Max(180f, screenW * barWidth);
            float totalH = barHeight;

            float x = screenW * barAt.x;
            float y = screenH * barAt.y;

            Rect trackRect = new Rect(x, y, totalW, totalH);

            Color prev = GUI.color;

            // Outer drop shadow
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(trackRect.x - 2f, trackRect.y - 2f, trackRect.width + 4f, trackRect.height + 4f), _white);

            // Background track
            GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
            GUI.DrawTexture(trackRect, _white);

            // Fill color
            Color fillColor;
            if (isLow)
            {
                float t = Mathf.PingPong(Time.time * 3.5f, 1f);
                fillColor = Color.Lerp(new Color(0.85f, 0.15f, 0.15f), new Color(1f, 0.45f, 0.2f), t);
            }
            else
            {
                fillColor = new Color(0.22f, 0.85f, 0.55f);
            }

            // Health fill bar
            float fillWidth = Mathf.Max(0f, (trackRect.width - 4f) * fraction);
            if (fillWidth > 0f)
            {
                GUI.color = fillColor;
                GUI.DrawTexture(new Rect(trackRect.x + 2f, trackRect.y + 2f, fillWidth, trackRect.height - 4f), _white);
            }

            // Outline border
            GUI.color = isLow ? new Color(1f, 0.3f, 0.3f, 0.8f) : new Color(0.35f, 0.4f, 0.48f, 0.7f);
            GUI.DrawTexture(new Rect(trackRect.x, trackRect.y, trackRect.width, 1.5f), _white);
            GUI.DrawTexture(new Rect(trackRect.x, trackRect.y + trackRect.height - 1.5f, trackRect.width, 1.5f), _white);
            GUI.DrawTexture(new Rect(trackRect.x, trackRect.y, 1.5f, trackRect.height), _white);
            GUI.DrawTexture(new Rect(trackRect.x + trackRect.width - 1.5f, trackRect.y, 1.5f, trackRect.height), _white);

            GUI.color = prev;

            // Health percentage text
            if (showNumber)
            {
                string hpText = $"HP  {Mathf.CeilToInt(fraction * 100f)}%";
                GUIStyle textStyle = new GUIStyle(_small)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12
                };
                textStyle.normal.textColor = Color.white;

                GUIStyle shadowStyle = new GUIStyle(textStyle);
                shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);

                GUI.Label(new Rect(trackRect.x + 1f, trackRect.y + 1f, trackRect.width, trackRect.height), hpText, shadowStyle);
                GUI.Label(trackRect, hpText, textStyle);
            }
        }

        private void Flash()
        {
            float flashLeft = AriHealth.FlashLeft;
            if (flashLeft <= 0f) return;

            float num = Mathf.Clamp01(flashLeft / 0.35f) * flashPeak;
            Color color = GUI.color;
            GUI.color = new Color(0.95f, 0.15f, 0.15f, num * 0.75f);
            float borderH = Mathf.Max(32f, (float)Screen.height * 0.12f);
            float borderW = Mathf.Max(32f, (float)Screen.width * 0.08f);

            GUI.DrawTexture(new Rect(0f, 0f, (float)Screen.width, borderH), _white);
            GUI.DrawTexture(new Rect(0f, (float)Screen.height - borderH, (float)Screen.width, borderH), _white);
            GUI.DrawTexture(new Rect(0f, 0f, borderW, (float)Screen.height), _white);
            GUI.DrawTexture(new Rect((float)Screen.width - borderW, 0f, borderW, (float)Screen.height), _white);
            GUI.color = color;
        }

        private void Slot()
        {
            if (!CarryingFragment) return;

            float badgeWidth = 210f;
            float badgeHeight = 32f;
            float x = Screen.width - badgeWidth - 24f;
            float y = 24f;
            Rect badgeRect = new Rect(x, y, badgeWidth, badgeHeight);

            Color color = GUI.color;
            GUI.color = new Color(0.05f, 0.07f, 0.12f, 0.88f);
            GUI.DrawTexture(badgeRect, _white);

            // Blue border
            GUI.color = FragmentBlue;
            GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.y, badgeRect.width, 2f), _white);
            GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.y + badgeRect.height - 2f, badgeRect.width, 2f), _white);

            // Icon & Text
            GUI.color = color;
            GUIStyle itemStyle = new GUIStyle(_small)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            };
            itemStyle.normal.textColor = new Color(0.7f, 0.9f, 1f);
            GUI.Label(badgeRect, "[ BLUE FRAGMENT CARRIED ]", itemStyle);
        }

        private void Lost()
        {
            AriHealth i = AriHealth.I;
            bool isDead = (i != null && i.IsDead) || LevelState.Lost;

            if (!isDead)
            {
                _lostAt = -1f;
                return;
            }

            if (_lostAt < 0f)
            {
                _lostAt = Time.time + lostDelay;
            }

            if (Time.time < _lostAt) return;

            if (RetryPressed())
            {
                _lostAt = -1f;
                Retry();
                return;
            }

            // Full screen cinematic Defeat Overlay
            Color prev = GUI.color;
            GUI.color = new Color(0.04f, 0.01f, 0.01f, 0.88f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _white);

            // Center card
            float boxWidth = Mathf.Min(640f, Screen.width * 0.85f);
            float boxHeight = 220f;
            float boxX = (Screen.width - boxWidth) * 0.5f;
            float boxY = (Screen.height - boxHeight) * 0.5f;
            Rect boxRect = new Rect(boxX, boxY, boxWidth, boxHeight);

            // Dark inner panel
            GUI.color = new Color(0.08f, 0.04f, 0.04f, 0.95f);
            GUI.DrawTexture(boxRect, _white);

            // Red accent lines
            GUI.color = new Color(0.95f, 0.22f, 0.22f, 0.9f);
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.y, boxRect.width, 3f), _white);
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.y + boxRect.height - 3f, boxRect.width, 3f), _white);
            GUI.color = prev;

            // Header: DEFEAT
            Rect titleRect = new Rect(boxRect.x, boxRect.y + 24f, boxRect.width, 50f);
            GUI.Label(titleRect, "DEFEAT", _defeatTitleStyle);

            // Subtitle
            Rect subRect = new Rect(boxRect.x + 20f, boxRect.y + 80f, boxRect.width - 40f, 40f);
            GUI.Label(subRect, lostText, _titleStyle);

            // Prompt
            Rect promptRect = new Rect(boxRect.x + 20f, boxRect.y + 140f, boxRect.width - 40f, 40f);
            GUIStyle retryStyle = new GUIStyle(_titleStyle)
            {
                fontSize = 16
            };
            retryStyle.normal.textColor = new Color(0.95f, 0.85f, 0.4f);
            GUI.Label(promptRect, retryText, retryStyle);
        }

        private void Victory()
        {
            if (_victoryAt < 0f)
            {
                _victoryAt = Time.time + 0.4f;
            }

            if (Time.time >= _victoryAt && RetryPressed())
            {
                RestartFullGame();
                return;
            }

            // Full screen celebration banner
            Color prev = GUI.color;
            GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.85f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _white);

            float boxWidth = Mathf.Min(700f, Screen.width * 0.85f);
            float boxHeight = 260f;
            float boxX = (Screen.width - boxWidth) * 0.5f;
            float boxY = (Screen.height - boxHeight) * 0.5f;
            Rect boxRect = new Rect(boxX, boxY, boxWidth, boxHeight);

            GUI.color = new Color(0.04f, 0.08f, 0.14f, 0.95f);
            GUI.DrawTexture(boxRect, _white);

            // Blue accent lines
            GUI.color = FragmentBlue;
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.y, boxRect.width, 3f), _white);
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.y + boxRect.height - 3f, boxRect.width, 3f), _white);
            GUI.color = prev;

            // Title
            Rect titleRect = new Rect(boxRect.x, boxRect.y + 20f, boxRect.width, 50f);
            GUI.Label(titleRect, "LEVEL 1 COMPLETE", _victoryTitleStyle);

            // Subtitle
            Rect subRect = new Rect(boxRect.x + 20f, boxRect.y + 75f, boxRect.width - 40f, 40f);
            GUI.Label(subRect, "The Grey Village begins to awaken! The blue flow has been restored.", _titleStyle);

            // Note
            Rect noteRect = new Rect(boxRect.x + 20f, boxRect.y + 125f, boxRect.width - 40f, 35f);
            GUIStyle noteStyle = new GUIStyle(_small)
            {
                fontSize = 15
            };
            noteStyle.normal.textColor = new Color(0.85f, 0.95f, 1f);
            GUI.Label(noteRect, "Well done! You have completed Level 1.", noteStyle);

            // Restart prompt
            Rect promptRect = new Rect(boxRect.x + 20f, boxRect.y + 165f, boxRect.width - 40f, 35f);
            GUIStyle restartStyle = new GUIStyle(_titleStyle)
            {
                fontSize = 16
            };
            restartStyle.normal.textColor = new Color(0.95f, 0.85f, 0.4f);
            GUI.Label(promptRect, "Press  ENTER  or  SPACE  to play again", restartStyle);

            // Interactive restart button
            float btnWidth = 220f;
            float btnHeight = 36f;
            Rect btnRect = new Rect(boxRect.x + (boxRect.width - btnWidth) * 0.5f, boxRect.y + boxHeight - 48f, btnWidth, btnHeight);
            if (GUI.Button(btnRect, "RESTART LEVEL"))
            {
                RestartFullGame();
            }
        }

        public static bool RetryPressed()
        {
            Keyboard current = Keyboard.current;
            if (current != null)
            {
                if (current.enterKey.wasPressedThisFrame ||
                    current.numpadEnterKey.wasPressedThisFrame ||
                    current.spaceKey.wasPressedThisFrame ||
                    current.rKey.wasPressedThisFrame ||
                    current.eKey.wasPressedThisFrame)
                {
                    return true;
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }

            if (Gamepad.current != null)
            {
                if (Gamepad.current.startButton.wasPressedThisFrame ||
                    Gamepad.current.buttonSouth.wasPressedThisFrame)
                {
                    return true;
                }
            }

            Event e = Event.current;
            if (e != null && e.isKey && e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Space || e.keyCode == KeyCode.R))
            {
                return true;
            }

            return false;
        }

        public static void RestartFullGame()
        {
            if (_isRestarting) return;
            _isRestarting = true;

            Debug.Log("[Echoes] Restarting Level 1 cleanly from beginning...");

            LevelRunner.CancelAll();
            LevelState.ResetAll();

            MonoCompanion.ResetDialogueState();

            CarryingFragment = false;
            ForceBar = false;

            ColorRestoreTarget.SetSealed(true);

            BeatPrompt.ResetAll();
            ControlPrompts.ResetAll();

            AriAnim.Forget();

            if (AriHealth.I != null)
            {
                AriHealth.I.ResetHealth();
            }
            if (AriMover.I != null)
            {
                AriMover.I.Frozen = false;
                AriMover.ScreenMove = Vector2.zero;
                AriMover.ScreenRun = false;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded && !string.IsNullOrEmpty(activeScene.name))
            {
                SceneManager.LoadScene(activeScene.name);
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }

        private void Retry()
        {
            LevelCheckpoint levelCheckpoint = Object.FindAnyObjectByType<LevelCheckpoint>(FindObjectsInactive.Include);
            if (levelCheckpoint != null && levelCheckpoint.Armed)
            {
                levelCheckpoint.Retry();
            }
            else
            {
                RestartFullGame();
            }
            _retried++;
        }
    }
}
