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

        // Navigation & Hint system
        private float _hintTimer = 0f;
        private string _hintMessage = "";

        public static bool CarryingFragment { get; set; }
        public static bool ForceBar { get; set; }

        public int Retries => _retried;

        private void OnEnable()
        {
            _isRestarting = false;
            _victoryAt = -1f;
            _lostAt = -1f;
            _hintTimer = 0f;
        }

        private void Update()
        {
            if (_isRestarting) return;

            // Poll Hint Hotkey (H / M / Gamepad Dpad Up)
            if (!Beat1Intro.IntroActive && !LevelState.Completed && !LevelState.Lost)
            {
                bool hintPressed = false;
                Keyboard kb = Keyboard.current;
                if (kb != null && (kb.hKey.wasPressedThisFrame || kb.mKey.wasPressedThisFrame))
                {
                    hintPressed = true;
                }
                Gamepad gp = Gamepad.current;
                if (gp != null && gp.dpad.up.wasPressedThisFrame)
                {
                    hintPressed = true;
                }

                if (hintPressed)
                {
                    TriggerHint();
                }

                if (_hintTimer > 0f)
                {
                    _hintTimer -= Time.deltaTime;
                }
            }

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

        private void TriggerHint()
        {
            _hintTimer = 6.0f;

            SleepingTree tree = SleepingTree.Instance ?? Object.FindAnyObjectByType<SleepingTree>();
            MonoCompanion mono = MonoCompanion.FindInLevel();

            if (tree != null && !tree.IsAwoken)
            {
                _hintMessage = "Look around Ari! Find the withered grey tree right beside you and strike it with [Left Click] to uncover a surprise!";
            }
            else if (!CarryingFragment)
            {
                _hintMessage = "Mono: Look toward the eastern outer wall! Look for the bright blue light beacon rising into the sky near the stone ruins!";
                if (mono != null)
                {
                    mono.SayDirect("Look toward the eastern wall! The water fragment is glowing bright blue over there!");
                }
            }
            else
            {
                _hintMessage = "Mono: Fantastic job! Now sprint back to the village square and restore the dried fountain with the fragment!";
                if (mono != null)
                {
                    mono.SayDirect("Bring the water fragment to the stone fountain in the center of the village!");
                }
            }
        }

        private void OnGUI()
        {
            // Do not draw in-game gameplay HUD while intro sequences are active
            if (Beat1Intro.IntroActive)
            {
                return;
            }

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

            DrawObjectiveAndWaypoint();
            DrawMiniRadar();
            DrawEyeWidget();
            DrawHintBanner();

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

        private void DrawObjectiveAndWaypoint()
        {
            if (LevelState.Completed || LevelState.Lost) return;

            Vector3 targetPos = Vector3.zero;
            string objText = "";
            string markerLabel = "";
            Color accentColor = Color.white;

            SleepingTree tree = SleepingTree.Instance ?? Object.FindAnyObjectByType<SleepingTree>();
            ColourFragment frag = ColourFragment.Instance ?? Object.FindAnyObjectByType<ColourFragment>();
            FountainFix fount = FountainFix.Instance ?? Object.FindAnyObjectByType<FountainFix>();

            AriMover ari = AriMover.I ?? Object.FindAnyObjectByType<AriMover>();
            Vector3 ariPos = ari != null ? ari.transform.position : Vector3.zero;

            if (tree != null && !tree.IsAwoken)
            {
                targetPos = tree.TouchPoint != Vector3.zero ? tree.TouchPoint : tree.transform.position;
                float d = Vector3.Distance(ariPos, targetPos);
                objText = $"OBJECTIVE: Paint the withered tree nearby [Left Click]  •  {Mathf.RoundToInt(d)}m  •  Press [H] for Hint";
                markerLabel = "🌲 Broken Tree";
                accentColor = new Color(0.4f, 0.95f, 0.55f);
            }
            else if (!CarryingFragment)
            {
                targetPos = frag != null ? frag.Spot : new Vector3(47.1f, 0f, 8.2f);
                float d = Vector3.Distance(ariPos, targetPos);
                objText = $"OBJECTIVE: Find the Blue Water Fragment (Near East Wall)  •  {Mathf.RoundToInt(d)}m  •  Press [H] for Hint";
                markerLabel = "💧 Water Fragment";
                accentColor = FragmentBlue;
            }
            else
            {
                targetPos = fount != null ? fount.transform.position : new Vector3(0f, 0.05f, 0f);
                float d = Vector3.Distance(ariPos, targetPos);
                objText = $"OBJECTIVE: Deliver Fragment to the Fountain in Center Square!  •  {Mathf.RoundToInt(d)}m  •  Press [H] for Hint";
                markerLabel = "⛲ Village Fountain";
                accentColor = new Color(0.4f, 0.85f, 1f);
            }

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 1.4f);

            // 1. Top Banner
            float bannerW = Mathf.Min(Screen.width * 0.72f, 760f * scale);
            float bannerH = 34f * scale;
            float bannerX = (Screen.width - bannerW) * 0.5f;
            float bannerY = 16f;

            Rect bannerRect = new Rect(bannerX, bannerY, bannerW, bannerH);
            DrawGlass(bannerRect, new Color(0.05f, 0.07f, 0.11f, 0.88f), accentColor * 0.7f);

            GUIStyle objStyle = new GUIStyle(_small)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(13.5f * scale),
                fontStyle = FontStyle.Bold
            };
            objStyle.normal.textColor = Color.white;
            GUI.Label(bannerRect, objText, objStyle);

            // 2. 3D World Waypoint
            Camera cam = Camera.main;
            if (cam == null || targetPos == Vector3.zero) return;

            float dist = Vector3.Distance(cam.transform.position, targetPos);
            if (dist < 2.0f) return;

            Vector3 screenPt = cam.WorldToScreenPoint(targetPos + Vector3.up * 1.4f);
            bool inFront = screenPt.z > 0f;

            float screenX = screenPt.x;
            float screenY = Screen.height - screenPt.y;

            float padX = 110f * scale;
            float padY = 75f * scale;

            if (!inFront)
            {
                screenX = Screen.width - screenX;
                screenY = Screen.height - screenY;
            }

            bool clamped = !inFront || screenX < padX || screenX > Screen.width - padX || screenY < padY || screenY > Screen.height - padY;

            screenX = Mathf.Clamp(screenX, padX, Screen.width - padX);
            screenY = Mathf.Clamp(screenY, padY, Screen.height - padY);

            string distStr = $"{Mathf.RoundToInt(dist)}m";
            string pinText;

            if (clamped)
            {
                if (screenX <= padX + 5f) pinText = $"◀  {markerLabel} [{distStr}]";
                else if (screenX >= Screen.width - padX - 5f) pinText = $"{markerLabel} [{distStr}]  ▶";
                else if (screenY <= padY + 5f) pinText = $"▲  {markerLabel} [{distStr}]";
                else pinText = $"▼  {markerLabel} [{distStr}]";
            }
            else
            {
                pinText = $"{markerLabel} • {distStr}";
            }

            GUIContent pinContent = new GUIContent(pinText);
            Vector2 pinSize = _small.CalcSize(pinContent);
            float pinW = pinSize.x + 24f * scale;
            float pinH = 28f * scale;
            Rect pinRect = new Rect(screenX - pinW * 0.5f, screenY - pinH * 0.5f, pinW, pinH);

            DrawGlass(pinRect, new Color(0.06f, 0.09f, 0.15f, 0.92f), accentColor);

            GUIStyle pinStyle = new GUIStyle(_small)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(12f * scale),
                fontStyle = FontStyle.Bold
            };
            pinStyle.normal.textColor = Color.white;
            GUI.Label(pinRect, pinText, pinStyle);
        }

        private void DrawEyeWidget()
        {
            if (LevelState.Completed || LevelState.Lost) return;

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 1.4f);
            float radius = 46f * scale;
            float centerX = Screen.width - radius - 24f * scale;
            float centerY = (CarryingFragment ? 68f : 24f) * scale + radius;

            float eyeW = 150f * scale;
            float eyeH = 28f * scale;
            float eyeX = centerX - eyeW * 0.5f;
            float eyeY = centerY + radius + 18f * scale;

            Rect eyeRect = new Rect(eyeX, eyeY, eyeW, eyeH);

            bool isFreeLook = AriFollowCamera.IsFreeLookActive;
            Color bg = isFreeLook ? new Color(0.18f, 0.14f, 0.05f, 0.92f) : new Color(0.05f, 0.07f, 0.12f, 0.75f);
            Color border = isFreeLook ? new Color(1f, 0.85f, 0.35f, 0.95f) : new Color(0.35f, 0.45f, 0.6f, 0.5f);

            DrawGlass(eyeRect, bg, border);

            GUIStyle eyeStyle = new GUIStyle(_small)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(11.5f * scale),
                fontStyle = isFreeLook ? FontStyle.Bold : FontStyle.Normal
            };
            eyeStyle.normal.textColor = isFreeLook ? new Color(1f, 0.9f, 0.45f) : new Color(0.85f, 0.9f, 0.95f);

            string eyeText = isFreeLook ? "👁️ 360° FREE LOOK" : "👁️ [ALT] 360° Look";
            if (GUI.Button(eyeRect, eyeText, eyeStyle))
            {
                if (AriFollowCamera.Instance != null)
                {
                    AriFollowCamera.Instance.RecenterBehindAri();
                }
            }
        }

        private void DrawMiniRadar()
        {
            if (LevelState.Completed || LevelState.Lost) return;
            AriMover ari = AriMover.I ?? Object.FindAnyObjectByType<AriMover>();
            if (ari == null) return;

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 1.4f);
            float radius = 46f * scale;
            float centerX = Screen.width - radius - 24f * scale;
            float centerY = (CarryingFragment ? 68f : 24f) * scale + radius;

            Rect radarRect = new Rect(centerX - radius, centerY - radius, radius * 2f, radius * 2f);
            DrawGlass(radarRect, new Color(0.04f, 0.06f, 0.10f, 0.85f), new Color(0.3f, 0.6f, 0.9f, 0.6f));

            Color prev = GUI.color;
            GUI.color = new Color(0.25f, 0.35f, 0.5f, 0.4f);
            GUI.DrawTexture(new Rect(centerX - radius + 4f, centerY, (radius - 4f) * 2f, 1f), _white);
            GUI.DrawTexture(new Rect(centerX, centerY - radius + 4f, 1f, (radius - 4f) * 2f), _white);

            // Ari marker
            GUI.color = Color.white;
            GUIStyle centerStyle = new GUIStyle(_small)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(13f * scale)
            };
            GUI.Label(new Rect(centerX - 10f, centerY - 10f, 20f, 20f), "▲", centerStyle);

            Vector3 ariPos = ari.transform.position;
            SleepingTree tree = SleepingTree.Instance;
            ColourFragment frag = ColourFragment.Instance;
            FountainFix fount = FountainFix.Instance;

            Vector3 target = Vector3.zero;
            Color blipColor = Color.cyan;

            if (tree != null && !tree.IsAwoken)
            {
                target = tree.TouchPoint != Vector3.zero ? tree.TouchPoint : tree.transform.position;
                blipColor = new Color(0.3f, 1f, 0.5f);
            }
            else if (!CarryingFragment)
            {
                target = frag != null ? frag.Spot : new Vector3(47.1f, 0f, 8.2f);
                blipColor = FragmentBlue;
            }
            else
            {
                target = fount != null ? fount.transform.position : new Vector3(0f, 0.05f, 0f);
                blipColor = new Color(0.4f, 0.9f, 1f);
            }

            if (target != Vector3.zero)
            {
                Vector3 toTarget = target - ariPos;
                toTarget.y = 0f;
                float d = toTarget.magnitude;
                Vector3 localDir = ari.transform.InverseTransformDirection(toTarget.normalized);

                float radarRange = 65f;
                float blipDist = Mathf.Clamp01(d / radarRange) * (radius - 8f);

                float blipX = centerX + localDir.x * blipDist;
                float blipY = centerY - localDir.z * blipDist;

                GUI.color = blipColor;
                GUIStyle blipStyle = new GUIStyle(_small)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(15f * scale),
                    fontStyle = FontStyle.Bold
                };
                blipStyle.normal.textColor = blipColor;
                GUI.Label(new Rect(blipX - 8f, blipY - 8f, 16f, 16f), "●", blipStyle);
            }

            GUI.color = new Color(0.7f, 0.8f, 0.95f, 0.8f);
            GUIStyle rLabelStyle = new GUIStyle(_small)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(10f * scale)
            };
            GUI.Label(new Rect(centerX - radius, centerY + radius + 2f, radius * 2f, 14f), "COMPASS", rLabelStyle);

            GUI.color = prev;
        }

        private void DrawHintBanner()
        {
            if (_hintTimer <= 0f || string.IsNullOrEmpty(_hintMessage)) return;

            float scale = Mathf.Clamp(Screen.height / 1080f, 0.7f, 1.4f);
            float bannerW = Mathf.Min(Screen.width * 0.82f, 840f * scale);
            float bannerH = 64f * scale;
            float bannerX = (Screen.width - bannerW) * 0.5f;
            float bannerY = 56f * scale;

            Rect rect = new Rect(bannerX, bannerY, bannerW, bannerH);
            DrawGlass(rect, new Color(0.07f, 0.09f, 0.15f, 0.96f), new Color(1f, 0.82f, 0.35f, 0.9f));

            GUIStyle hintTitleStyle = new GUIStyle(_small)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(15f * scale),
                fontStyle = FontStyle.Bold
            };
            hintTitleStyle.normal.textColor = new Color(1f, 0.88f, 0.45f);

            GUIStyle hintBodyStyle = new GUIStyle(_small)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(13f * scale)
            };
            hintBodyStyle.normal.textColor = Color.white;

            Rect titleRect = new Rect(bannerX + 10f, bannerY + 6f * scale, bannerW - 20f, 22f * scale);
            GUI.Label(titleRect, "✦  MISSION NAVIGATION HINT  ✦", hintTitleStyle);

            Rect bodyRect = new Rect(bannerX + 15f, bannerY + 28f * scale, bannerW - 30f, 30f * scale);
            GUI.Label(bodyRect, _hintMessage, hintBodyStyle);
        }

        private void DrawGlass(Rect rect, Color fill, Color border)
        {
            Color prev = GUI.color;
            GUI.color = fill;
            GUI.DrawTexture(rect, _white);

            GUI.color = border;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1.5f), _white);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - 1.5f, rect.width, 1.5f), _white);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1.5f, rect.height), _white);
            GUI.DrawTexture(new Rect(rect.x + rect.width - 1.5f, rect.y, 1.5f, rect.height), _white);
            GUI.color = prev;
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

            GUI.color = FragmentBlue;
            GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.y, badgeRect.width, 2f), _white);
            GUI.DrawTexture(new Rect(badgeRect.x, badgeRect.y + badgeRect.height - 2f, badgeRect.width, 2f), _white);

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

            Color prev = GUI.color;
            GUI.color = new Color(0.04f, 0.01f, 0.01f, 0.88f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _white);

            float boxWidth = Mathf.Min(640f, Screen.width * 0.85f);
            float boxHeight = 220f;
            float boxX = (Screen.width - boxWidth) * 0.5f;
            float boxY = (Screen.height - boxHeight) * 0.5f;
            Rect boxRect = new Rect(boxX, boxY, boxWidth, boxHeight);

            GUI.color = new Color(0.08f, 0.04f, 0.04f, 0.95f);
            GUI.DrawTexture(boxRect, _white);

            GUI.color = new Color(0.95f, 0.22f, 0.22f, 0.9f);
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.y, boxRect.width, 3f), _white);
            GUI.DrawTexture(new Rect(boxRect.x, boxRect.y + boxRect.height - 3f, boxRect.width, 3f), _white);
            GUI.color = prev;

            Rect titleRect = new Rect(boxRect.x, boxRect.y + 24f, boxRect.width, 50f);
            GUI.Label(titleRect, "DEFEAT", _defeatTitleStyle);

            Rect subRect = new Rect(boxRect.x + 20f, boxRect.y + 80f, boxRect.width - 40f, 40f);
            GUI.Label(subRect, lostText, _titleStyle);

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

            Color prev = GUI.color;

            float topBarH = 72f;
            GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.82f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, topBarH), _white);
            GUI.color = FragmentBlue;
            GUI.DrawTexture(new Rect(0f, topBarH - 3f, Screen.width, 3f), _white);
            GUI.color = prev;

            Rect titleRect = new Rect(0f, 6f, Screen.width, 36f);
            GUI.Label(titleRect, "LEVEL 1 COMPLETE  -  VILLAGE RESTORED!", _victoryTitleStyle);

            Rect subRect = new Rect(0f, 42f, Screen.width, 24f);
            GUI.Label(subRect, "The fountain flows with pure water and vibrant colors return to the village!", _titleStyle);

            float btmBarH = 60f;
            float btmBarY = Screen.height - btmBarH;
            GUI.color = new Color(0.02f, 0.04f, 0.08f, 0.82f);
            GUI.DrawTexture(new Rect(0f, btmBarY, Screen.width, btmBarH), _white);
            GUI.color = FragmentBlue;
            GUI.DrawTexture(new Rect(0f, btmBarY, Screen.width, 3f), _white);
            GUI.color = prev;

            Rect promptRect = new Rect(20f, btmBarY + 12f, Screen.width - 240f, 36f);
            GUIStyle restartStyle = new GUIStyle(_titleStyle)
            {
                fontSize = 17,
                alignment = TextAnchor.MiddleLeft
            };
            restartStyle.normal.textColor = new Color(0.95f, 0.88f, 0.45f);
            GUI.Label(promptRect, "Press [ENTER] or [SPACE] to Play Again   |   Watching 360 Village Panoramic View", restartStyle);

            float btnWidth = 180f;
            float btnHeight = 36f;
            Rect btnRect = new Rect(Screen.width - btnWidth - 25f, btmBarY + 12f, btnWidth, btnHeight);
            if (GUI.Button(btnRect, "PLAY AGAIN"))
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
            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.buttonSouth.wasPressedThisFrame ||
                    gamepad.startButton.wasPressedThisFrame)
                {
                    return true;
                }
            }
            return false;
        }

        private void Retry()
        {
            _retried++;
            LevelCheckpoint levelCheckpoint = Object.FindAnyObjectByType<LevelCheckpoint>(FindObjectsInactive.Include);
            if (levelCheckpoint != null)
            {
                levelCheckpoint.Retry();
            }
            else
            {
                LevelCheckpoint.ResetEveryBeat();
                AriHealth i = AriHealth.I;
                if (i != null)
                {
                    i.ResetHealth();
                }
                LevelState.Lost = false;
            }
        }

        private void RestartFullGame()
        {
            if (_isRestarting) return;
            _isRestarting = true;
            LevelState.Completed = false;
            LevelState.Lost = false;
            CarryingFragment = false;
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
        }
    }
}
