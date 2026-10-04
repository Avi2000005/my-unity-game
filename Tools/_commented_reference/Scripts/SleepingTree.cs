using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// Beat 3's spine: the sleeping tree at the edge of the square, the brush
    /// stroke that wakes it, the colour that comes back, and Mono.
    ///
    /// The trigger is the brush, not a proximity box. A trigger volume is the
    /// right tool for "walk here" and this beat is not that — the whole point
    /// is that Ari has to *do* the thing Beat 2 taught her. So the tree waits
    /// for a stroke, and accepts it on either of two grounds, because they fail
    /// in different ways and it is cheaper to accept both than to tune one
    /// exactly right on the first try:
    ///
    ///   - the stroke landed on the tree. Measured against the stroke point, not
    ///     against Ari, because the brush is a ray from the camera and she can
    ///     quite reasonably paint the trunk from four metres away.
    ///   - Ari is within arm's reach. The forgiveness case: a player who is
    ///     standing on top of the thing swinging the brush deserves it to work
    ///     even if the ray clipped a leaf.
    ///
    /// Which one fired is written to the console, so if the beat turns out to
    /// be too eager or too shy in play, the log says whether Ari was near the
    /// tree or the ray was on it.
    /// </summary>
    [AddComponentMenu("Echoes/Sleeping Tree")]
    public sealed class SleepingTree : MonoBehaviour
    {
        [Header("What it is")]
        [Tooltip("The point on the tree the brush has to reach. Usually the " +
                 "trunk or a child transform, not the root, because the root of " +
                 "a kit piece is often at its base and often has a collider " +
                 "several metres across.")]
        [SerializeField] Transform touchPoint;

        [Tooltip("The tree's own colour. Restored separately from the burst so " +
                 "the trunk is guaranteed to end up coloured even if the burst " +
                 "radius is later dialled in smaller than the tree is tall.")]
        [SerializeField] ColorRestoreTarget selfColour;

        [Header("Who is involved")]
        [Tooltip("Left empty, found by name.")]
        [SerializeField] MonoCompanion mono;

        [Tooltip("The gully run Mono leads her down. Optional — the beat " +
                 "completes without it, and the setup tool reports if it is " +
                 "missing rather than leaving a silent null.")]
        [SerializeField] MonoChase chase;

        [Header("Touching it")]
        [Tooltip("How close Ari must be for a stroke to count on its own. " +
                 "Slightly more than arm's length: the tree has a trunk and she " +
                 "has a 0.30 m capsule.")]
        [Min(0.5f)] [SerializeField] float touchDistance = 2.4f;

        [Tooltip("How near the stroke has to land to count as hitting the tree, " +
                 "for a swing from further away. Generous, because the camera " +
                 "ray splay at the top of the screen is several degrees and that " +
                 "is metres of ground at this range.")]
        [Min(0.5f)] [SerializeField] float strokeHitDistance = 2.5f;

        [Header("The burst")]
        [Tooltip("Radius of colour that comes back. Wider than the brush's own " +
                 "6 m, because this is an event and not a stroke — the player " +
                 "should be able to see it from where they are standing.")]
        [Min(1f)] [SerializeField] float burstRadius = 9f;

        [Tooltip("Seconds for it to flow in. Slow enough to read as colour " +
                 "moving rather than a switch being flipped.")]
        [Min(0.1f)] [SerializeField] float burstSeconds = 1.8f;

        [Header("Words")]
        [Tooltip("What Mono says when he wakes. Looked up in his line list, so " +
                 "a missing line is a warning in the console, not silence.")]
        [SerializeField] string wakeLineId = "beat3.wake";

        [Tooltip("Shown while Ari is close enough to touch it. Sticky, because " +
                 "it is telling her to do the one thing the beat is waiting for.")]
        [SerializeField] string promptText = "The old tree is grey. Swing the brush at it.";

        [Tooltip("Shown for a moment once it has woken.")]
        [SerializeField] string doneText = "";

        [SerializeField] float doneSeconds = 3.5f;

        [Header("Debug")]
        [SerializeField] bool log = true;

        // --- state ---------------------------------------------------------------

        BrushPainter _brush;
        AriMover _ari;
        Vector3 _point;
        bool _resolved;
        bool _awoken;

        /// <summary>Has the beat fired? Read by the setup tool and by probes.</summary>
        public bool IsAwoken => _awoken;

        /// <summary>Where the brush has to reach. Read by the setup tool.</summary>
        public Vector3 TouchPoint => _point;

        /// <summary>The stroke radius the beat will use. Read by the setup tool.</summary>
        public float BurstRadius => burstRadius;

        /// <summary>How far away Ari counts as touching it. Read by the setup tool.</summary>
        public float TouchDistance => touchDistance;

        void OnEnable()
        {
            Resolve();
            if (_brush != null) _brush.Stroked += OnStroked;
        }

        void OnDisable()
        {
            if (_brush != null) _brush.Stroked -= OnStroked;
        }

        void Reset()
        {
            touchPoint = transform;
        }

        /// <summary>
        /// Find everything this needs. Safe to call again, and called from
        /// OnEnable because a component added by an editor tool after the scene
        /// was loaded does not get an Awake.
        /// </summary>
        public void Resolve()
        {
            if (_point == Vector3.zero) _point = touchPoint != null ? touchPoint.position
                                                                    : transform.position;

            if (_brush == null)
            {
                // The brush is a screen ray, so it may live on Ari or on the
                // camera. Both are legitimate and only one of them is right for
                // a given scene, so try the player first and fall back to main.
                var ariGo = GameObject.Find("Ari");
                if (ariGo != null)
                {
                    _ari = ariGo.GetComponent<AriMover>();
                    _brush = ariGo.GetComponentInChildren<BrushPainter>();
                }

                if (_brush == null && Camera.main != null)
                    _brush = Camera.main.GetComponent<BrushPainter>();
            }

            if (mono == null)
            {
                // FindInLevel, not GameObject.Find: Mono is inactive until this
                // very beat fires, so the ordinary lookup returns null and the
                // tree wakes nobody.
                mono = MonoCompanion.FindInLevel();
            }

            if (chase == null)
            {
                var found = GameObject.Find("MonoChase");
                if (found != null) chase = found.GetComponent<MonoChase>();
            }

            if (selfColour == null) selfColour = GetComponentInChildren<ColorRestoreTarget>();

            _resolved = true;
        }

        void Update()
        {
            if (_awoken) return;
            if (!_resolved) Resolve();

            if (_brush == null || _ari == null) return;

            bool near = AriIsClose();
            if (near) BeatPrompt.Show(promptText);
        }

        /// <summary>Is Ari within touching distance of the tree?</summary>
        public bool AriIsClose()
        {
            if (_ari == null) return false;
            Vector3 d = _ari.transform.position - _point;
            d.y = 0f;
            return d.sqrMagnitude <= touchDistance * touchDistance;
        }

        void OnStroked(Vector3 worldPosition, int targets)
        {
            if (_awoken) return;
            if (!_resolved) Resolve();

            bool onTree = Vector3.Distance(worldPosition, _point) <= strokeHitDistance;
            bool close = AriIsClose();

            if (!onTree && !close) return;

            Wake("the stroke landed on the tree" + (close ? " (and she was beside it)" : ""))
                .Log("stroke at " + worldPosition.ToString("F1") + " from " +
                      _point.ToString("F1") + ", reached " + targets + " target(s)",
                      log, this);
        }

        /// <summary>
        /// The beat's one irreversible step. Idempotent.
        ///
        /// Idempotent because there are two realistic ways to reach it twice —
        /// a designer pressing the context menu after already playing through,
        /// and a level script that fires the beat on a checkpoint reload as well
        /// as on the trigger. Either would restart an eleven second wake
        /// animation and blow away the line the player was reading.
        /// </summary>
        /// <param name="why">Written to the console, so a beat that fires when
        /// it should not can be diagnosed from the log without a repro.</param>
        public WakeResult Wake(string why = "called directly")
        {
            if (!_resolved) Resolve();

            if (_awoken) return WakeResult.Already;
            _awoken = true;

            // The burst first, so the colour is already moving by the time Mono
            // stands up. Doing it the other way round means the beat opens on
            // a character animation in a dead grey world and the payoff arrives
            // a beat late.
            int burst = ColorRestoreTarget.RestoreInRadius(_point, burstRadius, 1f, burstSeconds);
            if (selfColour != null) selfColour.RestoreTo(1f, burstSeconds);

            // Then Mono. Say before Wake would be dropped — Say() refuses to
            // speak for a character who is still asleep.
            if (mono != null)
            {
                mono.Wake();
                mono.SayBeat(wakeLineId);
            }

            if (chase != null) chase.Begin();

            BeatPrompt.Show(doneText, doneSeconds);
            if (string.IsNullOrEmpty(doneText)) BeatPrompt.Clear();

            return new WakeResult(true, why, burst, burstRadius);
        }

        /// <summary>
        /// Put the beat back to the start. Everything the beat changed, undone:
        /// the tree greyed, Mono asleep and hidden, his lines unspent, the chase
        /// released, the camera handed back.
        ///
        /// The line memory matters more than it looks. Mono's hint ladder is
        /// spent state, and a checkpoint reload that restores the world but not
        /// his memory hands the player a companion who has nothing left to say
        /// and a puzzle whose hints are all used up.
        /// </summary>
        [ContextMenu("Reset Beat 3")]
        public void ResetBeat()
        {
            _awoken = false;
            _resolved = false;

            if (selfColour != null) selfColour.SetRestoreImmediate(0f);
            if (mono != null) mono.Unwake();
            if (chase != null) chase.End();

            ColorRestoreTarget.RestoreInRadius(_point, burstRadius, 0f, 0.2f);

            BeatPrompt.Clear();
            Resolve();
        }

        /// <summary>What happened, for the console and for probes.</summary>
        public readonly struct WakeResult
        {
            public readonly bool Fired;
            public readonly string Why;
            public readonly int TargetsReached;
            public readonly float Radius;

            public WakeResult(bool fired, string why, int targets, float radius)
            {
                Fired = fired;
                Why = why;
                TargetsReached = targets;
                Radius = radius;
            }

            public static WakeResult Already => new WakeResult(false, "already awoken", 0, 0f);

            /// <summary>
            /// Chainable so the trigger can log and still return the result.
            ///
            /// The verbosity flag and the log context are passed in rather than
            /// read off the component: this is a struct, and a struct has no
            /// reach back to the MonoBehaviour that owns it, so anything it
            /// tried to read off the enclosing instance would not compile.
            /// </summary>
            public WakeResult Log(string detail, bool log, Object context)
            {
                if (Fired && log)
                    Debug.Log("[Echoes] Beat 3 tree awoken — " + Why + "; burst reached " +
                              TargetsReached + " target(s) within " + Radius.ToString("0.0") +
                              " m. " + detail, context);
                return this;
            }
        }
    }
}
