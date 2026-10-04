using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{
    /// <summary>
    /// Ari's brush. Paints colour back into the world: finds every
    /// <see cref="ColorRestoreTarget"/> near a point and animates it to full colour.
    ///
    /// This is the API a real Ari controller will call. <see cref="testMode"/> adds a
    /// mouse-driven fallback so the whole system can be validated in the editor before
    /// a character controller exists.
    /// </summary>
    [AddComponentMenu("Echoes/Brush Painter")]
    public sealed class BrushPainter : MonoBehaviour
    {
        [Header("Brush")]
        [Tooltip("Radius in metres around the stroke that regains colour.")]
        [Min(0.1f)] [SerializeField] float radius = 6f;

        [Tooltip("Seconds for colour to flow back in.")]
        [Min(0f)] [SerializeField] float strokeDuration = 1.2f;

        [Tooltip("Cooldown between strokes, per the combat loop design.")]
        [Min(0f)] [SerializeField] float cooldown = 0.6f;

        [Header("Test mode")]
        [Tooltip("Paint with the left mouse button. Turn this off once Ari can drive " +
                 "the brush himself.")]
        [SerializeField] bool testMode = true;

        [Tooltip("Camera used for test-mode aiming. Falls back to Camera.main.")]
        [SerializeField] Camera aimCamera;

        [SerializeField] float maxRayDistance = 250f;

        [Header("Feedback")]
        [Tooltip("Ari. Found on this object or in her children if left empty. She " +
                 "swings on a stroke, because a click that repaints the world " +
                 "while she stands still reads as the mouse doing something " +
                 "rather than as a brush stroke.")]
        [SerializeField] AriMover ari;

        [Tooltip("Show a mark where the stroke lands. The colour coming back is " +
                 "measured at about 7.6% of full range on a dull surface, which " +
                 "is close to invisible, so without this the player has no way " +
                 "of knowing the click registered.")]
        [SerializeField] bool showStrokeMark = true;

        [Tooltip("Radius of the mark, as a fraction of the stroke radius.")]
        [Range(0.1f, 2f)] [SerializeField] float markScale = 1f;

        [Header("Debug keys")]
        [Tooltip("G greys the whole world again, R restores everything. Useful for A/B.")]
        [SerializeField] bool debugKeys = true;

        float _nextStrokeTime;

        /// <summary>
        /// Raised when a *stroke* lands: position painted, and how many
        /// ColorRestoreTargets it reached.
        ///
        /// Fires from <see cref="TryStroke"/> only, and that is deliberate. A
        /// beat that cares whether Ari swung the brush — Beat 3's tree is the
        /// first thing that does — must not be told about a paint it performed
        /// itself, and <see cref="PaintAt"/> is the door every programmatic paint
        /// in the level comes through. Firing on both would have the tree
        /// awakening itself the moment a beat called PaintAt to make the burst,
        /// with Ari standing well outside touching distance.
        ///
        /// Not fired for a rejected stroke either, so a subscriber can treat
        /// this as "the player swung and it counted".
        /// </summary>
        public event System.Action<Vector3, int> Stroked;

        /// <summary>Radius used by <see cref="PaintAt"/>.</summary>
        public float Radius => radius;

        /// <summary>
        /// Is a stroke allowed right now? Public because a beat that shows a
        /// "swing the brush" prompt needs to know whether the prompt is
        /// currently lying to the player.
        /// </summary>
        public bool CanSwing => CanStroke();

        /// <summary>
        /// Paint colour back in around a world point. Returns how many targets were hit.
        /// This is the entry point a real Ari controller should call on a brush swipe.
        /// </summary>
        public int PaintAt(Vector3 worldPosition, float overrideRadius = -1f, float overrideDuration = -1f)
        {
            float r = overrideRadius > 0f ? overrideRadius : radius;
            return ColorRestoreTarget.RestoreInRadius(worldPosition, r, 1f, overrideDuration);
        }

        /// <summary>Raycast a screen position and paint whatever it lands on.</summary>
        public int PaintFromScreenPoint(Vector3 screenPosition)
        {
            var hit = ResolveScreenPoint(screenPosition);
            if (!hit.HasValue) return 0;
            return PaintAt(hit.Value);
        }

        /// <summary>
        /// Screen point to world point, or null when the ray hits nothing.
        ///
        /// Split out so the click path can go through <see cref="TryStroke"/> and
        /// therefore through the same cooldown a controller swing pays. Testing
        /// CanStroke() at the call site checked the cooldown but never charged it,
        /// so holding the left button repainted every frame instead of once per
        /// stroke.
        /// </summary>
        Vector3? ResolveScreenPoint(Vector3 screenPosition)
        {
            var cam = aimCamera != null ? aimCamera : Camera.main;
            if (cam == null) return null;

            Ray ray = cam.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance)) return null;

            _lastNormal = hit.normal;
            return hit.point;
        }

        /// <summary>
        /// Surface normal of the last resolved point, for laying the mark flat
        /// against whatever was hit. A mark that keeps facing the world axis
        /// stands proud of a wall or vanishes into the paving, and either way
        /// reads as a bug rather than as a brush stroke.
        /// </summary>
        Vector3 _lastNormal = Vector3.up;

        void Update()
        {
            if (testMode)
            {
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    // Resolve first, then stroke: ResolveScreenPoint can report "the
                    // ray hit nothing", and there is no stroke to charge a cooldown
                    // for in that case.
                    var hit = ResolveScreenPoint(mouse.position.ReadValue());
                    if (hit.HasValue && TryStroke(hit.Value))
                        Debug.Log($"[Brush] stroke, radius {radius}", this);
                }
            }

            if (!debugKeys) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.gKey.wasPressedThisFrame)
            {
                ColorRestoreTarget.SetAllImmediate(0f);
                Debug.Log("[Brush] world greyed");
            }

            if (kb.rKey.wasPressedThisFrame)
            {
                int n = 0;
                foreach (var t in ColorRestoreTarget.AllActive)
                {
                    if (t != null) { t.RestoreTo(1f, strokeDuration); n++; }
                }
                Debug.Log($"[Brush] restored {n} target(s)");
            }
        }

        bool CanStroke() => Time.time >= _nextStrokeTime;

        void OnValidate()
        {
            radius = Mathf.Max(0.1f, radius);
            strokeDuration = Mathf.Max(0f, strokeDuration);
            cooldown = Mathf.Max(0f, cooldown);
        }

        /// <summary>Called by Ari's real brush swing.</summary>
        public bool TryStroke(Vector3 worldPosition)
        {
            if (!CanStroke()) return false;
            _nextStrokeTime = Time.time + cooldown;

            int hit = PaintAt(worldPosition);
            FeelStroke(worldPosition);
            Stroked?.Invoke(worldPosition, hit);
            return true;
        }

        /// <summary>
        /// The two things that make a click read as a stroke.
        ///
        /// Both are feedback and neither is the mechanic. The paint itself is
        /// PaintAt, above; this is only what tells the player it happened.
        ///
        /// Order matters and it is swing first. A mark that appears on the same
        /// frame the arm starts moving reads as the mark being thrown, and a
        /// brush that throws its paint rather than laying it looks wrong before
        /// the player has consciously decided anything.
        /// </summary>
        void FeelStroke(Vector3 worldPosition)
        {
            if (ari == null) ari = FindAri();

            // No Ari is not a reason to skip the mark. The mark answers "did my
            // click land", and that question is still worth answering in a scene
            // where the girl has not been placed yet.
            if (ari != null) ari.PlaySwing();

            if (showStrokeMark)
                BrushStrokeFx.Spawn(worldPosition, _lastNormal, radius * markScale);
        }

        /// <summary>
        /// Ari, wherever she is.
        ///
        /// Searched rather than wired, because the brush and the girl are
        /// separate objects in the level and a required reference between them
        /// means every future controller has to remember to set it. Found
        /// including inactive objects: Ari is deactivated while the game is not
        /// running, so the ordinary lookups return nothing at exactly the moment
        /// this is first called from an editor test.
        /// </summary>
        AriMover FindAri()
        {
            // Instance method, not static: GetComponentInParent is a member of
            // Component and a static one has no component to ask. BrushPainter
            // is itself a component, so the brush can be a child of the girl and
            // find her without a wire between the two.
            var own = GetComponentInParent<AriMover>(true);
            if (own != null) return own;

            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name == "Ari")
                {
                    var m = t.GetComponentInChildren<AriMover>(true);
                    if (m != null) return m;
                }

            return null;
        }
    }
}
