using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Echoes.Painterly
{
    /// <summary>
    /// On-screen movement buttons: a D-pad, a run toggle, and a stop.
    ///
    /// Built from code in Awake rather than authored as a prefab, because a
    /// prefab is a binary blob that cannot be reviewed or diffed, and this is
    /// four rectangles. The trade is that the layout is in code, which is the
    /// right place for something that is going to be adjusted.
    ///
    /// It exists because the keyboard is not always available. Under automation
    /// Keyboard.current is null, and a remote or locked-focus editor session
    /// drops key presses the same way — in both cases AriMover reads nothing and
    /// the game looks frozen with no indication why.
    ///
    /// The controls write to AriMover's static ScreenMove and ScreenRun, and the
    /// STOP button clears both. That means stopping works even if the controller
    /// is mid-blend: Speed goes to zero and the graph crossfades back to idle
    /// rather than waiting out the walk clip.
    /// </summary>
    [AddComponentMenu("Echoes/On Screen Controls")]
    public sealed class OnScreenControls : MonoBehaviour
    {
        [Header("Layout")]
        [Tooltip("Side of the D-pad buttons, in units of the short screen edge.")]
        [Min(0.05f)] [SerializeField] float buttonSize = 0.13f;

        [Tooltip("How far the D-pad sits from the corner, as a fraction of height.")]
        [Range(0f, 0.4f)] [SerializeField] float edgeMargin = 0.06f;

        [Tooltip("Gap between the D-pad buttons, as a fraction of the button size.")]
        [Range(0f, 0.5f)] [SerializeField] float gap = 0.06f;

        [Header("Behaviour")]
        [Tooltip("Also drive Ari from these buttons, ignoring the keyboard.")]
        [SerializeField] bool driveAri = true;

        [Tooltip("Start visible. Turn off if the buttons are in the way of the " +
                 "scene; the whole canvas is disabled, not just the graphics.")]
        [SerializeField] bool visibleOnStart = true;

        /// <summary>Colours chosen to read against a grey world.</summary>
        static readonly Color PadColour = new Color(0.92f, 0.92f, 0.92f, 0.55f);
        static readonly Color StopColour = new Color(0.85f, 0.30f, 0.28f, 0.70f);
        static readonly Color RunColour = new Color(0.95f, 0.80f, 0.35f, 0.65f);
        static readonly Color HeldColour = new Color(1f, 1f, 1f, 0.85f);

        Canvas _canvas;
        Button _up, _down, _left, _right, _run, _stop;
        HoldButton _upHold, _downHold, _leftHold, _rightHold;

        void Awake()
        {
            Build();
        }

        void Update()
        {
            Tick();
        }

        /// <summary>
        /// Reads the pad and publishes a direction for AriMover.
        ///
        /// Public and separate from Update so it can be called at a chosen
        /// moment rather than whenever the frame happens to land — a probe
        /// pressing a button and immediately reading the result cannot wait for
        /// the next Update without also stepping whatever else runs in it.
        /// </summary>
        public void Tick()
        {
            if (_canvas == null || !_canvas.gameObject.activeInHierarchy)
            {
                AriMover.ScreenMove = Vector2.zero;
                return;
            }

            // Summed rather than exclusive, so a thumb on two buttons at once
            // gives a diagonal instead of one direction winning. Normalised
            // afterwards, or the diagonal would be 41% faster than the axes.
            Vector2 dir = Vector2.zero;
            if (Pressing(_upHold)) dir += Vector2.up;
            if (Pressing(_downHold)) dir += Vector2.down;
            if (Pressing(_rightHold)) dir += Vector2.right;
            if (Pressing(_leftHold)) dir += Vector2.left;

            AriMover.ScreenMove = dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        static bool Pressing(HoldButton b) => b != null && b.Held;

        void OnDisable()
        {
            // Losing focus mid-press must not leave Ari walking into a wall.
            AriMover.ScreenMove = Vector2.zero;
        }

        void OnDestroy()
        {
            // Ari would otherwise keep reading a direction nobody is holding.
            AriMover.ScreenMove = Vector2.zero;
            AriMover.ScreenRun = false;
        }

        void Build()
        {
            // An EventSystem is what turns a button into something clickable.
            // Unity's own "create EventSystem" menu item adds the legacy
            // StandaloneInputModule, which does nothing under the Input System
            // package, so the module type is chosen explicitly.
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem",
                                        typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(transform, false);
            }

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            _canvas.gameObject.SetActive(visibleOnStart);

            // RectTransform anchors, so the pad holds its corner at any aspect.
            float m = edgeMargin;
            float b = buttonSize;
            float g = b * gap;

            _leftHold = PadHold("Left",  new Vector2(m, m), new Vector2(0f, 0f), Vector2.left, out _left);
            _downHold = PadHold("Down",  new Vector2(m + b + g, m), new Vector2(0f, 0f), Vector2.down, out _down);
            _rightHold = PadHold("Right", new Vector2(m + (b + g) * 2f, m), new Vector2(0f, 0f), Vector2.right, out _right);
            _upHold = PadHold("Up", new Vector2(m + b + g, m + b + g), new Vector2(0f, 0f), Vector2.up, out _up);

            PadHold("Run", new Vector2(0f, m), new Vector2(1f, 0f), Vector2.zero, out _run, RunColour);
            PadHold("STOP", new Vector2(1f, m), new Vector2(1f, 0f), Vector2.zero, out _stop, StopColour);

            Label(_run, "RUN");
            Label(_stop, "STOP");

            _run.onClick.AddListener(ToggleRun);
            _stop.onClick.AddListener(Stop);

            if (driveAri)
            {
                var ari = GameObject.Find("Ari");
                if (ari != null)
                {
                    var mover = ari.GetComponent<AriMover>();
                    if (mover != null) mover.UseScreenControls = true;
                }
            }
        }

        /// <summary>
        /// One pad button, returning both the Button and its HoldButton.
        ///
        /// The Button is what the EventSystem drives; the HoldButton is what
        /// reports the held state back. Both come out of here so the caller
        /// cannot wire up one and forget the other.
        /// </summary>
        HoldButton PadHold(string name, Vector2 anchor, Vector2 pivot, Vector2 direction,
                           out Button button, Color? colour = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_canvas.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            // Square in reference-resolution units, which the scaler maps onto the
            // real screen. Sizing by the short edge keeps the pad usable in
            // portrait as well as landscape.
            rt.sizeDelta = new Vector2(buttonSize, buttonSize);

            var img = go.GetComponent<Image>();
            img.color = colour ?? PadColour;

            button = go.GetComponent<Button>();
            button.targetGraphic = img;
            button.transition = Selectable.Transition.None;   // the hold tint is ours

            var hold = go.AddComponent<HoldButton>();
            hold.Configure(direction, img.color);

            return hold;
        }

        void Label(Button button, string text)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(button.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = BuiltinFont();
            t.fontSize = 28;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            t.raycastTarget = false;
        }

        /// <summary>
        /// Arial ships inside Unity, so the buttons have text without the project
        /// needing a font asset of its own.
        /// </summary>
        static Font BuiltinFont()
        {
            // Resources.GetBuiltinResource is the supported way to reach it; the
            // legacy name "Arial.ttf" still resolves in Unity 6 but the
            // try/catch keeps a future rename from being a hard failure.
            try
            {
                return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                return Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
        }

        void Stop()
        {
            AriMover.ScreenMove = Vector2.zero;
            AriMover.ScreenRun = false;
        }

        void ToggleRun()
        {
            AriMover.ScreenRun = !AriMover.ScreenRun;
        }
    }

    /// <summary>
    /// A D-pad button that reports being held, and tints itself while it is.
    ///
    /// Deliberately not driven by IPointerDownHandler. Pointer callbacks stop
    /// arriving in several ordinary situations — the pointer leaves the button
    /// while held, the editor loses focus, a second finger lands — and each of
    /// those leaves the last direction stuck on. Polling the Button's own pressed
    /// state cannot get stuck, because the Button clears it whenever the pointer
    /// is no longer over it.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class HoldButton : MonoBehaviour
    {
        /// <summary>Direction to push, in the same space as AriMover.ScreenMove.</summary>
        public Vector2 Direction { get; private set; }

        Button _button;
        Image _image;
        Color _resting;

        /// <summary>Whether this direction is being held right now.</summary>
        public bool Held => _button != null && _button.IsPressed();

        public void Configure(Vector2 direction, Color resting)
        {
            Direction = direction;
            _resting = resting;
        }

        void Awake()
        {
            _button = GetComponent<Button>();
            _image = GetComponent<Image>();
        }

        void LateUpdate()
        {
            if (_image == null) return;

            // White and near-opaque while held, so it is obvious which way she is
            // going without having to read the character.
            _image.color = Held
                ? new Color(1f, 1f, 1f, 0.9f)
                : _resting;
        }
    }
}
