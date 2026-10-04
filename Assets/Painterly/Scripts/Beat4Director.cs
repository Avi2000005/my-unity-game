using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// Beat 4 — the gate, the plate, and the crawlspace switch.
    ///
    /// The beat in one sentence: Ari cannot fit through the hole and Mono can,
    /// so she has to stay out here holding the lever down while he goes and
    /// throws the switch, and the only thing that makes the lever worth holding
    /// is that it has to be held *while* he throws it.
    ///
    /// Two decisions worth stating, because both were the other way round at
    /// some point.
    ///
    /// First, arrival is decided here and nowhere else. The director sends Mono
    /// once and then only intervenes if he has wandered out of the switch's
    /// reach, so he cannot pace back and forth on the spot because an errand
    /// that completed recalled him and the recall looked like a failure. The
    /// switch decides whether he is close enough, because "close enough" is the
    /// switch's own question — it has a reach because a switch with no reach is
    /// a trigger volume and would throw itself from across the village.
    ///
    /// Second, the gate opens on the switch being thrown, *not* on the lever
    /// still being held. A latched thing stays latched. Checking the lever at
    /// the moment the gate finished travelling would mean the player had to hold
    /// a plate for one and a half more seconds than the puzzle asked for, and
    /// would fail them for stepping away at the exact instant they were told
    /// they were free.
    /// </summary>
    [AddComponentMenu("Echoes/Beat 4 Director")]
    public sealed class Beat4Director : MonoBehaviour
    {
        public enum Phase
        {
            /// <summary>Nothing has happened. Ari has not reached the gate.</summary>
            Idle,
            /// <summary>She is at the gate and it will not open for her.</summary>
            Blocked,
            /// <summary>She is on the plate; Mono is on his way.</summary>
            Holding,
            /// <summary>He is at the switch and it has not thrown.</summary>
            Waiting,
            /// <summary>The gate is moving.</summary>
            Opening,
            /// <summary>She is through.</summary>
            Done
        }

        [Header("The parts")]
        [SerializeField] BeatGate gate;
        [SerializeField] HoldLever lever;
        [SerializeField] LatchingSwitch latchingSwitch;

        [Header("Who")]
        [Tooltip("Ari. Found by name if left empty.")]
        [SerializeField] AriMover ari;

        [Tooltip("Mono. Found by name if left empty.")]
        [SerializeField] MonoCompanion mono;

        [Header("Where Mono is sent")]
        [Tooltip("Must be inside the crawlspace, out of Ari's sight down the " +
                 "tunnel, and within the switch's reach.")]
        [SerializeField] Transform errandPoint;

        [Header("Waiting on the last beat")]
        [Tooltip("Beat 3's chase. If set, this beat does not begin until the " +
                 "chase reports Done — otherwise the gate's notice radius " +
                 "catches Ari while she is still running the gully, two " +
                 "metres short of it, and the beat fires mid-chase.")]
        [SerializeField] MonoChase chase;

        [Header("Ranges, flat on the ground")]
        [Tooltip("How close she must be to the gate before the beat notices " +
                 "her. Kept small because the gully's mouth is only two metres " +
                 "from the gate.")]
        [Min(1f)] [SerializeField] float noticeRange = 2.6f;

        [Tooltip("How far past the gate she must be before the beat counts as " +
                 "finished. Wider than her capsule so she cannot trip the end " +
                 "by brushing the threshold.")]
        [Min(1f)] [SerializeField] float throughRange = 2.0f;

        [Header("Reading")]
        /// <summary>Where the beat has got to. Read by the setup tool.</summary>
        public Phase Current { get; private set; } = Phase.Idle;

        /// <summary>
        /// How many times she has let go of the lever while the beat was still
        /// unsolved. Zero in a clean playthrough; several means the puzzle is
        /// fighting the player rather than teaching them.
        /// </summary>
        public int LeverSlips { get; private set; }

        /// <summary>Seconds spent in the current phase.</summary>
        public float PhaseSeconds { get; private set; }

        /// <summary>Fires when she is through. Beat 5 can listen to this.</summary>
        public event System.Action Finished;

        bool _sent;
        bool _finished;
        Phase _last;

        void Awake()
        {
            if (ari == null)
            {
                var go = GameObject.Find("Ari");
                if (go != null) ari = go.GetComponent<AriMover>();
            }

            if (mono == null)
            {
                var go = GameObject.Find("Mono");
                if (go != null) mono = go.GetComponent<MonoCompanion>();
            }
        }

        void OnEnable()
        {
            // Subscribed here rather than in Awake so that a beat which is
            // switched off in the inspector stops talking to the gate, and a
            // replay after a Reset picks the subscription back up.
            if (latchingSwitch != null) latchingSwitch.Threw += OnSwitchThrew;
        }

        void OnDisable()
        {
            if (latchingSwitch != null) latchingSwitch.Threw -= OnSwitchThrew;
        }

        void OnSwitchThrew()
        {
            // The switch is the authority on this. The director reacts to it
            // rather than polling it every frame, so there is exactly one code
            // path into "the gate opens" whether the switch throws because the
            // lever was held or because a test called ThrowForTest.
            if (gate == null) return;
            if (gate.IsOpen || gate.IsOpening) return;

            gate.Open();
            SetPhase(Phase.Opening);
            Prompt("The gate is lifting. Go on, then — I am right behind you.");
            mono.SayBeat("beat4.open");
        }

        void Update()
        {
            PhaseSeconds += Time.deltaTime;

            if (Current != _last) _last = Current;

            // The gate opens on the latch, whatever the lever is doing now.
            // Checked first and unconditionally so that stepping off the plate
            // in the same frame the switch throws cannot be the thing that stops
            // it.
            if (latchingSwitch != null && latchingSwitch.Thrown &&
                gate != null && gate.IsClosed)
            {
                OnSwitchThrew();
            }

            if (_finished) return;

            if (!PreviousBeatDone()) return;

            switch (Current)
            {
                case Phase.Idle: DoIdle(); break;
                case Phase.Blocked: DoBlocked(); break;
                case Phase.Holding:
                case Phase.Waiting: DoLever(); break;
                case Phase.Opening: DoOpening(); break;
            }
        }

        bool PreviousBeatDone()
        {
            // No chase means this beat stands alone, which is how it gets
            // tested without the whole of Beat 3 in front of it.
            if (chase == null) return true;
            return chase.PhaseName == "Done";
        }

        void DoIdle()
        {
            if (gate == null || ari == null) return;
            if (Flat(ari.transform.position, gate.transform.position) > noticeRange) return;

            SetPhase(Phase.Blocked);
            Prompt("The gate has no handle on this side. But something down " +
                   "here is still connected to it.");
        }

        void DoBlocked()
        {
            if (lever == null) return;

            if (lever.Held)
            {
                SetPhase(Phase.Holding);
                Prompt("Hold it there. I will find whatever it opens.");
                mono.SayBeat("beat4.hold");
                return;
            }

            // She walked away from the gate without finding the plate. Point
            // her at it rather than repeating what she has already read.
            if (gate == null || ari == null) return;

            var toLever = Flat(ari.transform.position, lever.transform.position);
            if (toLever > 6f)
                Prompt("There is a plate on the ground by the wall. " +
                       "Step on it and hold it down.");
        }

        void DoLever()
        {
            if (lever == null || latchingSwitch == null || gate == null) return;

            bool powered = lever.Held;
            bool thrown = latchingSwitch.Thrown;

            if (thrown)
            {
                // Handled at the top of Update; this is here so the phase can
                // never sit in Holding with the switch already thrown.
                return;
            }

            if (!powered)
            {
                LeverSlips++;
                SetPhase(Phase.Blocked);
                Prompt("The lever sprang back up the moment you stepped off.", 2.6f);
                return;
            }

            SetPhase(Phase.Waiting);

            // Send once, then only rescue him. Re-sending a man who is already
            // standing at the switch is how a companion ends up jogging on the
            // spot for as long as the player takes to read the prompt.
            if (mono == null) return;

            if (!mono.OnErrand && !latchingSwitch.MonoInReach)
            {
                if (!_sent) _sent = true;
                if (errandPoint != null) mono.SendTo(errandPoint);
                else Debug.LogWarning("[Echoes] " + name + ": no errand point, " +
                                      "so Mono will never reach the switch.");
            }
        }

        void DoOpening()
        {
            if (gate == null || ari == null) return;
            if (!gate.IsOpen) return;

            if (Flat(ari.transform.position, gate.transform.position) < throughRange)
            {
                Prompt("This way is dry. Come on.");
                return;
            }

            _finished = true;
            SetPhase(Phase.Done);

            // He has done his errand and the gate is open; bring him back to
            // her so Beat 5 starts with a follower rather than a man standing
            // in a tunnel on the wrong side of a wall.
            if (mono != null) mono.Recall();

            mono.SayBeat("beat4.after");

            if (Finished != null) Finished();
        }

        void SetPhase(Phase p)
        {
            if (Current == p) return;
            Current = p;
            PhaseSeconds = 0f;
        }

        void Prompt(string text, float seconds = 0f) => BeatPrompt.Show(text, seconds);

        static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        [ContextMenu("Beat 4 / reset")]
        public void ResetBeat()
        {
            _finished = false;
            _sent = false;
            LeverSlips = 0;
            Current = Phase.Idle;
            PhaseSeconds = 0f;
            if (gate != null) gate.ResetGate();
            else if (latchingSwitch != null) latchingSwitch.ResetSwitch();
            if (lever != null) lever.ResetLever();
            if (mono != null) mono.Recall();
            BeatPrompt.Clear();
        }
    }
}
