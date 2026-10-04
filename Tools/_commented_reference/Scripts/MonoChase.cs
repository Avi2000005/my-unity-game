using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// Beat 3's second half: a short run down a narrow gully with Mono.
    ///
    /// Nothing in here can hurt the player. There is no damage, no spawn, no
    /// fail state and no timer, and that is not an omission to fill in later —
    /// it is the beat. The brief calls this a chase, and a chase in a tutorial
    /// level is a chase with nothing in it: the running is the point, the
    /// partner is the point, and the gully being too tight to be comfortable is
    /// the point. Adding a threat to it would turn a walk that teaches the
    /// player they have a companion into a fight that teaches them the
    /// companion cannot help.
    ///
    /// The camera does the actual guiding. It frames Ari and the next corner
    /// together and pulls back as the gap between them grows, so the player is
    /// shown where the gully goes rather than told. A waypoint marker or an
    /// arrow would be the other option and would break the fiction: this is a
    /// greyed village in which a girl with a brush has just woken something
    /// that will not leave her, and there is nothing in the fiction that draws
    /// waypoint arrows.
    ///
    /// The legs are measured at build time — the setup tool reports the free
    /// width at every one — because "narrow gully" is a claim about geometry
    /// and geometry does not care what the level designer intended.
    /// </summary>
    [AddComponentMenu("Echoes/Mono Chase")]
    public sealed class MonoChase : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Leg
        {
            [Tooltip("A point along the gully. Empty legs are skipped and " +
                     "reported, not silently ignored.")]
            public Transform point;

            [Tooltip("How close Ari must come for this leg to count as passed.")]
            [Min(0.5f)] public float triggerRadius = 3.0f;

            [Tooltip("Mono's line as she reaches this leg. Looked up in his " +
                     "line list; a missing id warns instead of going silent.")]
            public string lineId = "";
        }

        [Header("The route")]
        [Tooltip("Corners of the gully, in order. Ari reaching the last one " +
                 "ends the beat. Built by Tools/Echoes/Build Beat 3.")]
        [SerializeField] List<Leg> legs = new List<Leg>();

        [Header("Who is running it")]
        [Tooltip("Left empty, found by name.")]
        [SerializeField] MonoCompanion mono;

        [Tooltip("Left empty, taken from Camera.main.")]
        [SerializeField] AriFollowCamera followCamera;

        [Header("The handover")]
        [Tooltip("Seconds between the tree waking and the run starting.\n\n" +
                 "The tree calls Begin in the same frame it fires the colour " +
                 "burst and wakes Mono. Without a gap the camera re-frames to " +
                 "the gully on that exact frame, so the beat's payoff — the " +
                 "colour arriving — happens off-screen or half off-screen, and " +
                 "the first thing the player sees of Beat 3 is a running start " +
                 "with no idea what just happened.\n\n" +
                 "This is a timer, not a wait on Mono_Wake finishing. A beat " +
                 "gated on an animation is a beat that stalls if the animation " +
                 "is re-imported, plays at a different speed, or is interrupted " +
                 "by the player leaving the trigger. 3.2s sits just past the " +
                 "trimmed 3.0s wake, but the two numbers were chosen " +
                 "separately and neither reads the other.")]
        [Min(0f)] [SerializeField] float handoverDelay = 3.2f;

        [Header("Camera")]
        [Tooltip("Metres the camera looks down from while guiding. A little " +
                 "higher than play, so the floor of the gully — the thing she " +
                 "has to run along — is in shot.")]
        [Min(0.5f)] [SerializeField] float guideHeight = 2.1f;

        [Tooltip("Extra metres of framing beyond the measured gap to the next " +
                 "corner. The gap exactly is as tight as it can be and clips " +
                 "the near character on the way round.")]
        [Min(0f)] [SerializeField] float guideMargin = 3.0f;

        [Header("Words")]
        [Tooltip("Said when the run starts, before the first corner.")]
        [SerializeField] string startLineId = "beat3.follow";

        [Tooltip("Shown while the run is going, sticky so it cannot time out " +
                 "under the player.")]
        [SerializeField] string promptText = "Stay with Mono.";

        [Tooltip("Shown briefly when the run ends.")]
        [SerializeField] string doneText = "";

        [SerializeField] float doneSeconds = 3.0f;

        [Header("Debug")]
        [SerializeField] bool log = true;

        // --- state ---------------------------------------------------------------

        enum Phase { Idle, Waiting, Running, Done }

        Phase _phase = Phase.Idle;
        int _nextLeg;
        AriMover _ari;
        bool _resolved;
        float _handoverLeft;

        /// <summary>Where the beat is. Read by the setup tool and by probes.</summary>
        public string PhaseName => _phase.ToString();

        /// <summary>Seconds until the run starts, or 0 when it has.</summary>
        public float HandoverLeft => _phase == Phase.Waiting ? Mathf.Max(0f, _handoverLeft) : 0f;

        /// <summary>The configured handover gap. Read by probes.</summary>
        public float HandoverSeconds => handoverDelay;

        /// <summary>Legs remaining. Read by probes.</summary>
        public int LegsLeft => Mathf.Max(0, legs.Count - _nextLeg);

        /// <summary>Legs configured, including any with no point set.</summary>
        public int LegsTotal => legs.Count;

        /// <summary>How far Ari is from the next corner, or -1 when there is none.</summary>
        public float DistanceToNextLeg
        {
            get
            {
                var leg = NextLeg();
                if (leg == null || leg.point == null || _ari == null) return -1f;
                return Vector3.Distance(_ari.transform.position, leg.point.position);
            }
        }

        Leg NextLeg()
        {
            for (int i = _nextLeg; i < legs.Count; i++)
            {
                if (legs[i] != null && legs[i].point != null) return legs[i];
            }
            return null;
        }

        void OnEnable() => Resolve();
        void Reset() => legs = new List<Leg>();

        /// <summary>
        /// Find Ari, Mono and the camera. Called from OnEnable because a
        /// component added by an editor tool after the scene was loaded never
        /// gets an Awake, and a chase with no Ari is a chase that never ends.
        /// </summary>
        public void Resolve()
        {
            if (_ari == null)
            {
                var go = GameObject.Find("Ari");
                if (go != null) _ari = go.GetComponent<AriMover>();
            }

            if (mono == null)
            {
                // FindInLevel, because Mono is asleep — and therefore inactive —
                // for the whole of Beats 1 and 2, which is exactly when this
                // component's Awake would otherwise run and find nobody.
                mono = MonoCompanion.FindInLevel();
            }

            if (followCamera == null && Camera.main != null)
                followCamera = Camera.main.GetComponent<AriFollowCamera>();

            _resolved = true;
        }

        /// <summary>
        /// Start the run. Idempotent, because the tree calls it and a level
        /// script may also call it on a checkpoint reload.
        ///
        /// Note what this does NOT do: it does not start the run. It starts the
        /// clock on the handover and returns, leaving the camera where the
        /// level designer left it and the beat's payoff on screen. The run
        /// itself happens in Update, when the timer runs out. See
        /// handoverDelay for why.
        /// </summary>
        public void Begin()
        {
            if (!_resolved) Resolve();

            if (_phase == Phase.Running || _phase == Phase.Waiting) return;

            _phase = Phase.Waiting;
            _nextLeg = 0;
            _handoverLeft = handoverDelay;

            if (log) Debug.Log("[Echoes] Beat 3 handover begun, " +
                               handoverDelay.ToString("0.00") + "s, " +
                               LegsTotal + " leg(s)", this);
        }

        /// <summary>
        /// The actual hand-off: take the camera, say the line, show the prompt.
        ///
        /// Split out of Begin so the timing lives in one place and the thing the
        /// beat is about stays readable on its own.
        /// </summary>
        void StartRun()
        {
            _phase = Phase.Running;
            _handoverLeft = 0f;

            if (mono != null && !string.IsNullOrEmpty(startLineId))
                mono.SayBeat(startLineId);

            if (!string.IsNullOrEmpty(promptText)) BeatPrompt.Show(promptText);

            if (log) Debug.Log("[Echoes] Beat 3 chase begun with " + LegsTotal + " leg(s)", this);
        }

        /// <summary>
        /// End the run and hand the camera back. Not the same as a reset: the
        /// chase is over, the gully has been run, and Mono stays with her as an
        /// ordinary follower. What is given back is the camera and the prompt.
        /// </summary>
        public void End()
        {
            if (_phase == Phase.Idle || _phase == Phase.Done) return;

            _phase = Phase.Done;
            _handoverLeft = 0f;

            if (followCamera != null) followCamera.ReleaseGuide();

            if (!string.IsNullOrEmpty(doneText)) BeatPrompt.Show(doneText, doneSeconds);
            else BeatPrompt.Clear();

            if (log) Debug.Log("[Echoes] Beat 3 chase ended after " + _nextLeg + " leg(s)", this);
        }

        /// <summary>
        /// Back to the start: no legs passed, no camera held, no prompt. Mono's
        /// own state is the tree's business, not this one's.
        /// </summary>
        [ContextMenu("Reset Chase")]
        public void ResetChase()
        {
            if (followCamera != null) followCamera.ReleaseGuide();
            _phase = Phase.Idle;
            _nextLeg = 0;
            _handoverLeft = 0f;
            BeatPrompt.Clear();
        }

        void Update()
        {
            if (_phase == Phase.Waiting)
            {
                _handoverLeft -= Time.deltaTime;
                if (_handoverLeft > 0f) return;

                StartRun();
                return;
            }

            if (_phase != Phase.Running) return;
            if (!_resolved) Resolve();
            if (_ari == null) return;

            // Pass any leg she is inside, and say its line on the way past. The
            // loop rather than a single check, because a player who runs the
            // length of a gully in one go can clear two corners in a frame and
            // would otherwise skip a line per corner skipped.
            while (true)
            {
                var leg = NextLeg();
                if (leg == null) break;

                float d = Vector3.Distance(_ari.transform.position, leg.point.position);
                if (d > leg.triggerRadius) break;

                if (mono != null && !string.IsNullOrEmpty(leg.lineId))
                    mono.SayBeat(leg.lineId);

                // Counted by index, not by "next unpassed", so that a leg with
                // no point set in the middle of the list cannot wedge the run.
                _nextLeg = legs.IndexOf(leg) + 1;
            }

            var ahead = NextLeg();

            if (ahead == null)
            {
                End();
                return;
            }

            if (followCamera != null)
                followCamera.Guide(ahead.point.position, guideHeight, guideMargin);
        }

        /// <summary>
        /// Replace the route. Used by the setup tool, which measures the gully
        /// and then hands the corners over. Public because a tool that cannot
        /// set the thing it is building is a tool that has to be re-run by hand.
        /// </summary>
        public void SetLegs(List<Leg> route)
        {
            legs = route ?? new List<Leg>();
        }
    }
}
