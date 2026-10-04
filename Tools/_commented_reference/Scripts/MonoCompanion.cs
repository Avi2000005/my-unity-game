using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// Mono's brain: invisible until he is woken, then he keeps up with Ari,
    /// talks when something asks him to, and otherwise stays quiet.
    ///
    /// The invisible part is the important part. Mono is placed in the village
    /// at build time, several beats before the player is meant to meet him, so
    /// the scene is one static thing that every beat can refer to — but a
    /// character standing in the open square from the first frame is a character
    /// the player walks past, and a beat that can be skipped is a beat that
    /// will be. So he is awake=false from the start and the tree is what turns
    /// that on.
    ///
    /// The following is deliberately unsophisticated: seek, stop inside a radius,
    /// report speed to the Animator. No pathfinding, because Beat 3's chase is
    /// a camera-guided run down one gully with nothing in it, and a navmesh
    /// agent solving a straight corridor is a second way for the beat to fail
    /// after the level designer has already done the work of making it simple.
    ///
    /// The errand is the one thing that changed when Beat 4 arrived, and it is
    /// not pathfinding. Beat 4 sends him somewhere Ari is not: through a
    /// crawlspace she is too tall for, to a switch she cannot reach. A follower
    /// cannot do that by construction, because a follower steers towards Ari and
    /// the whole point of the errand is to go away from her. So there is a
    /// destination that overrides the follow for as long as it lasts, and it
    /// still has no idea what a wall is — which is fine here, because the one
    /// route he is ever sent down is a tunnel he fits inside.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class MonoCompanion : MonoBehaviour
    {
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int TalkId = Animator.StringToHash("Talk");
        static readonly int AwakeId = Animator.StringToHash("Awake");

        [Header("Who he is with")]
        [Tooltip("Ari. Left empty he will find her by name at Awake.")]
        [SerializeField] Transform ari;

        [Header("Following")]
        [Tooltip("How far behind Ari he stops. Close enough to talk over her " +
                 "shoulder, far enough that he is not under her feet.")]
        [Min(0.3f)] [SerializeField] float followDistance = 2.0f;

        [Tooltip("How fast he closes a gap. Deliberately slower than Ari's run: " +
                 "if he can keep up at a sprint he is not being left behind, and " +
                 "the chase stops being a chase.")]
        [Min(0.2f)] [SerializeField] float followSpeed = 3.0f;

        [Tooltip("How fast he turns. Matches Ari's, for the same reason — two " +
                 "characters rotating at different rates is the first thing that " +
                 "makes a pair look like two characters.")]
        [Min(30f)] [SerializeField] float turnRate = 720f;

        [Tooltip("Whether he walks at all. Off for a beat where he should only " +
                 "hover and comment.")]
        [SerializeField] bool follows = true;

        [Header("Errands")]
        [Tooltip("How close he has to get to an errand's destination before he " +
                 "stops and hands control back to following. Small, because he " +
                 "is 0.57 m tall and the switch he is sent to is the size of a " +
                 "fist.")]
        [Min(0.1f)] [SerializeField] float errandRadius = 0.7f;

        [Header("Speech")]
        [Tooltip("The line list. Left empty he still wakes and still follows, " +
                 "he just has nothing to say.")]
        [SerializeField] MonoHintLines lines;

        [Tooltip("How long a line stays up, per character. A typewriter reveal " +
                 "is added on top of this. Four seconds is a long time to read " +
                 "two words of dialogue in a game where nobody else is speaking.")]
        [Min(1f)] [SerializeField] float lineSeconds = 4f;

        [Tooltip("Characters per second for the reveal. Slow enough to be " +
                 "read by a player who is also walking, which is most of the time.")]
        [Min(5f)] [SerializeField] float revealPerSecond = 28f;

        [Tooltip("Where the subtitle sits, in normalised screen space. " +
                 "(0.5, 0.08) is just under centre, which is where a player " +
                 "already is looking.")]
        [SerializeField] Vector2 subtitleAt = new Vector2(0.5f, 0.10f);

        [Tooltip("Draw the subtitle. On by default; off once real UI exists, " +
                 "at which point this whole block goes.")]
        [SerializeField] bool drawSubtitle = true;

        // --- state ---------------------------------------------------------------

        Animator _animator;
        Transform _target;
        Transform _errand;

        bool _awake;
        bool _hasController;

        string _current = "";
        string _showing = "";
        float _reveal;
        float _lineUntil = -1f;

        GUIStyle _style;

        /// <summary>Whether he has been woken. Beat 3's tree is what sets it.</summary>
        public bool IsAwake => _awake;

        /// <summary>Whether he is on his feet and following, as opposed to hidden.</summary>
        public bool IsFollowing => _awake && follows;

        /// <summary>
        /// Whether he has been sent somewhere of his own, away from Ari.
        /// Read by Beat 4's director so it can tell the difference between "he
        /// has not started yet" and "he is there and has not thrown it".
        /// </summary>
        public bool OnErrand => _errand != null;

        /// <summary>
        /// How far he is from an errand's destination, or -1 when he has none.
        ///
        /// -1 rather than infinity, because infinity and "no errand" are the same
        /// number and a beat that compares against it will pass for a man
        /// standing at the far side of the village.
        /// </summary>
        public float ErrandDistance
        {
            get
            {
                if (_errand == null) return -1f;
                var a = transform.position; var b = _errand.position;
                a.y = 0f; b.y = 0f;
                return Vector3.Distance(a, b);
            }
        }

        /// <summary>Whether an errand has been sent and is now finished.</summary>
        public bool ErrandDone => _errand != null && ErrandDistance <= errandRadius;

        /// <summary>
        /// How close he has to get before an errand counts as arrived.
        ///
        /// Exposed because this number and a destination's own reach have to
        /// agree, and there is no way to see that they do not from inside the
        /// two components separately. Send him to a switch whose reach is
        /// shorter than this and he stops just outside it, recalls himself, and
        /// the beat waits forever on a man standing in the right place who
        /// never does the thing.
        /// </summary>
        public float ErrandRadius => errandRadius;

        /// <summary>
        /// Send him somewhere instead of following Ari, until he gets there.
        ///
        /// Beat 4's whole puzzle is that the two of them have to be in different
        /// places: she on the lever, because it is held down by weight, and him
        /// through a gap she is too tall for. A follower cannot be sent away from
        /// the person it follows, so this is what makes the beat possible.
        ///
        /// Idempotent while he is walking to the same place, and replaces the
        /// destination if it is a different one — a beat that re-sends him every
        /// frame while he is still crossing the square should not restart the
        /// errand every frame.
        /// </summary>
        public void SendTo(Transform where)
        {
            if (where == null) { Recall(); return; }
            if (!_awake) return;
            _errand = where;
        }

        /// <summary>
        /// Call him back to Ari. Called on arrival by his own steering, by a beat
        /// that has changed its mind, and by Unwake, so a man sent into a
        /// crawlspace cannot still be walking to it after the level has restarted.
        /// </summary>
        public void Recall()
        {
            _errand = null;
        }

        /// <summary>
        /// Put him somewhere else and bring him back under Ari's lead.
        ///
        /// For the test warp and a checkpoint restore. Clears the errand first,
        /// and that ordering is the whole point: an errand is a *destination*,
        /// so moving him without clearing one leaves him walking back to where
        /// the beat sent him, across the level, past the thing the warp was
        /// supposed to show. He would arrive at the gate eventually and the
        /// beat would work, several minutes later, for reasons that had nothing
        /// to do with the puzzle.
        ///
        /// Recall rather than remembering which destination is which, because a
        /// warp that relocates the world under a standing instruction has to
        /// invalidate the instruction, not try to satisfy it.
        /// </summary>
        public void WarpTo(Vector3 where)
        {
            _errand = null;
            transform.position = where;
            Physics.SyncTransforms();
        }

        /// <summary>How far he is from Ari, in metres. Read by the chase beat.</summary>
        public float DistanceToAri =>
            ari == null ? float.PositiveInfinity : Vector3.Distance(transform.position, ari.position);

        /// <summary>
        /// Find him in the level, asleep or awake.
        ///
        /// <c>GameObject.Find</c> only returns active objects, and Mono is
        /// inactive from build time until the tree wakes him — so an ordinary
        /// lookup fails at exactly the moment he starts to matter, and fails
        /// silently, by returning null. The cast container is searched too,
        /// because <c>Transform.Find</c> does see inactive children.
        ///
        /// One implementation because "which Mono" is not a question a beat
        /// should have to ask, and two of them is a bug in the placement tool
        /// rather than something a level is entitled to handle.
        /// </summary>
        public static MonoCompanion FindInLevel()
        {
            // Touched 2026-09-30: Unity had this file in the asset database and
            // was compiling its two dependents, but had never put it in its own
            // source list, so MonoCompanion was "not found" from files sitting
            // in the same folder. Re-reading the file is what puts it back.
            var live = GameObject.Find("Mono");
            if (live != null)
            {
                var c = live.GetComponent<MonoCompanion>();
                if (c != null) return c;
            }

            var container = GameObject.Find("L1_Cast");
            if (container != null)
            {
                var t = container.transform.Find("Mono");
                if (t != null) return t.GetComponent<MonoCompanion>();
            }

            return null;
        }

        /// <summary>The line currently on screen, or empty. For the setup report.</summary>
        public string CurrentLine => _current;

        void Awake()
        {
            Bind();

            // Awake is what sets this, and it is the reason the component binds
            // lazily as well: a probe that drives Step without a running Awake
            // used to throw UnassignedReferenceException on the Animator, and
            // an exception there is a component that silently does nothing in
            // edit mode and works fine in play — the worst kind of bug to find
            // by running the game.
            if (ari == null)
            {
                var found = GameObject.Find("Ari");
                if (found != null) ari = found.transform;
            }
        }

        /// <summary>
        /// Resolve the Animator and check it can actually do anything. Safe to
        /// call repeatedly, and called from Awake because Awake does not run for
        /// a component added by an editor tool after the scene loaded.
        /// </summary>
        public void Bind()
        {
            if (_animator == null) _animator = GetComponent<Animator>();

            if (_animator == null) return;

            _hasController = _animator.runtimeAnimatorController != null;

            if (!_hasController && Application.isPlaying)
                Debug.LogWarning("[Echoes] Mono has an Animator but no controller. " +
                                 "He will stand in his bind pose, which is arms out " +
                                 "and legs together — a scarecrow, not a character. " +
                                 "Run Tools/Echoes/Build Cast Controllers.",
                                 this);
        }

        /// <summary>
        /// Wake him. Idempotent, so a tree that fires twice — a brush stroke
        /// that overlaps the trigger twice, or a designer pressing the key in
        /// the inspector — cannot restart an eleven second animation and throw
        /// away the line the player was halfway through reading.
        /// </summary>
        public void Wake()
        {
            Bind();
            if (_awake) return;

            _awake = true;
            gameObject.SetActive(true);

            if (_animator != null && _hasController)
                _animator.SetTrigger(AwakeId);
        }

        /// <summary>Put him back to sleep and forget every line he has said.</summary>
        public void Unwake()
        {
            _awake = false;
            gameObject.SetActive(false);
            ClearLine();
            Recall();
            if (lines != null) lines.ResetMemory();
        }

        /// <summary>
        /// Say a line. Ignored if he is already talking, which is the behaviour
        /// the hint ladder needs: three rungs firing in a row should queue into
        /// one interruption, not cut each other off mid-word.
        /// </summary>
        public void Say(MonoHintLines.Line line)
        {
            if (line == null || !_awake) return;
            if (string.IsNullOrEmpty(line.text)) return;

            Begin(line.text);

            if (lines != null) lines.Consume(line);
            if (_animator != null && _hasController) _animator.SetTrigger(TalkId);
        }

        /// <summary>
        /// The next thing worth saying, if anything. Beats call this; it decides
        /// between the hint ladder and the ambient chatter.
        /// </summary>
        public void SaySomething()
        {
            if (lines == null || !_awake) return;

            var line = lines.NextAmbient(Time.time);
            if (line != null) Say(line);
        }

        /// <summary>Say a named beat line, e.g. "beat3.wake".</summary>
        public void SayBeat(string id)
        {
            if (lines == null) return;
            Say(lines.Beat(id));
        }

        /// <summary>Advance the hint ladder one rung. Beat 6 calls this on failure.</summary>
        public bool SayNextHint()
        {
            if (lines == null) return false;

            var line = lines.NextHint();
            if (line == null) return false;

            Say(line);
            return true;
        }

        void Begin(string text)
        {
            _current = text;
            _showing = "";
            _reveal = 0f;
            _lineUntil = Time.time + lineSeconds + text.Length / revealPerSecond;
        }

        void ClearLine()
        {
            _current = "";
            _showing = "";
            _reveal = 0f;
            _lineUntil = -1f;
        }

        void Update()
        {
            if (!_awake) return;

            if (_target == null && ari != null) _target = ari;

            if (_current != "")
            {
                _reveal += revealPerSecond * Time.deltaTime;
                int shown = Mathf.Clamp(Mathf.FloorToInt(_reveal), 0, _current.Length);
                _showing = _current.Substring(0, shown);

                if (Time.time >= _lineUntil) ClearLine();
            }

            if (IsFollowing && (_errand != null || _target != null))
                Follow(dt: Time.deltaTime);
            else
                ReportSpeed(0f);
        }

        void Follow(float dt)
        {
            var t = transform;

            // An errand outranks the follow, and it hands control straight back
            // the moment he is close enough — which is why there is no separate
            // "arrived" branch in the director to keep in step with this one.
            // Two places deciding when he has arrived is how a beat ends up
            // waiting forever for an event that already happened.
            if (_errand != null)
            {
                var there = _errand.position;
                var walk = there - t.position;
                walk.y = 0f;

                if (walk.magnitude <= errandRadius)
                {
                    Recall();
                    return;
                }

                Move(t, walk.normalized, followSpeed, dt);
                return;
            }

            if (_target == null) { ReportSpeed(0f); return; }

            var to = _target.position - t.position;
            to.y = 0f;

            float gap = to.magnitude;

            // Hysteresis rather than a hard radius. Snapping on and off at
            // exactly followDistance makes him jitter on the spot, and a
            // companion who jitters is more distracting than one who is a little
            // too far away.
            if (gap < followDistance * 0.7f)
            {
                // Close enough. He slows rather than stopping dead, so he settles
                // instead of arriving and starting again.
                float want = Mathf.Clamp01((gap - followDistance * 0.35f) /
                                            Mathf.Max(0.001f, followDistance * 0.35f));
                Move(t, to.normalized, followSpeed * want, dt);
                return;
            }

            if (gap < 0.05f)
            {
                ReportSpeed(0f);
                return;
            }

            Move(t, to.normalized, followSpeed, dt);
        }

        void Move(Transform t, Vector3 direction, float speed, float dt)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-6f) { ReportSpeed(0f); return; }

            direction.Normalize();
            t.position += direction * (speed * dt);

            // Turn at a finite rate rather than snapping, so a change of
            // direction reads as him noticing rather than as a cut.
            var want = Quaternion.LookRotation(direction, Vector3.up);
            t.rotation = Quaternion.RotateTowards(t.rotation, want, turnRate * dt);

            ReportSpeed(speed);
        }

        /// <summary>
        /// Tell the graph how fast he is going.
        ///
        /// The same float name Ari's controller uses, on purpose. A level
        /// script that drives two characters should not need to know which is
        /// which, and two names for the same quantity is a bug waiting to happen
        /// the first time one of them is renamed.
        /// </summary>
        void ReportSpeed(float speed)
        {
            if (_animator == null || !_hasController) return;
            _animator.SetFloat(SpeedId, speed);
        }

        // --- subtitle ------------------------------------------------------------

        void OnGUI()
        {
            if (!drawSubtitle || string.IsNullOrEmpty(_current) || !_awake) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(22f * Mathf.Max(0.6f, Screen.height / 720f)),
                    wordWrap = true,
                    richText = false
                };
                _style.normal.textColor = new Color(0.92f, 0.92f, 0.90f);
            }

            // Drawn in a fixed-width box centred on subtitleAt, in normalised
            // screen space, so it sits in the same place at any resolution.
            float width = Mathf.Min(Screen.width * 0.7f, 900f);
            float height = 120f;
            var rect = new Rect(
                (Screen.width - width) * subtitleAt.x,
                Screen.height * subtitleAt.y,
                width, height);

            // A shadow rather than a background box. The world is colourless and
            // lit dimly, so a black plate behind the text would be the darkest
            // thing on screen and would draw the eye away from Ari.
            var shadow = new GUIStyle(_style);
            shadow.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height),
                      _showing, shadow);

            GUI.Label(rect, _showing, _style);

            // A speaker tag, so a player who has not yet seen Mono talk is not
            // left wondering who is speaking.
            if (_showing.Length > 0)
            {
                var tag = new Rect(rect.x, rect.y - 26f, rect.width, 24f);
                var tagStyle = new GUIStyle(_style);
                tagStyle.fontSize = Mathf.Max(11, _style.fontSize - 7);
                tagStyle.normal.textColor = new Color(0.75f, 0.75f, 0.72f);
                GUI.Label(tag, "Mono", tagStyle);
            }
        }
    }
}
