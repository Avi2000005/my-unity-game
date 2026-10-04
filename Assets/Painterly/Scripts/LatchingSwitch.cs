using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// The switch at the far end of the crawlspace. Only Mono can reach it.
    ///
    /// It latches rather than holding: it stays thrown once it is thrown, but it
    /// only *can* be thrown while the lever has power. That single conditional
    /// is the whole puzzle. The obvious version of this beat — Ari stands on
    /// the plate, so the gate opens, full stop — is not a puzzle, it is a
    /// waiting sim. The version where the plate only has to be held *while the
    /// switch is thrown* is a sequence the player has to work out on their own,
    /// and it still has exactly one solution, which matters for a level's
    /// first cooperative puzzle.
    ///
    /// Mono is not scripted to throw it. He walks to it because the director
    /// sends him, and he throws it because he is standing next to it, which
    /// means a player who parks him somewhere unhelpful gets a switch that
    /// does not throw rather than a beat that silently skips.
    /// </summary>
    [AddComponentMenu("Echoes/Latching Switch")]
    public sealed class LatchingSwitch : MonoBehaviour
    {
        [Header("Who can reach it")]
        [Tooltip("Mono. Left empty he is found by name at Awake.")]
        [SerializeField] MonoCompanion mono;

        [Tooltip("How close he must be. The setup tool checks this against " +
                 "Mono's errand radius — if he arrives and stops short of " +
                 "this, the beat never fires and nothing on screen says why.")]
        [Min(0.3f)] [SerializeField] float reach = 1.0f;

        [Header("What lets it throw")]
        [Tooltip("The lever that powers the mechanism. Left empty the switch " +
                 "always has power, which turns the beat off but leaves it " +
                 "working.")]
        [SerializeField] HoldLever lever;

        [Tooltip("Require the lever to be held at the moment he throws.")]
        [SerializeField] bool requirePower = true;

        [Header("Reading")]
        /// <summary>Thrown and latched. The gate may now open.</summary>
        public bool Thrown { get; private set; }

        /// <summary>
        /// How many times he has stood here with no power. Zero in a correct
        /// build only if the player never does it wrong, so a live probe
        /// reads this; a build that reports several has a reach problem.
        /// </summary>
        public int WaitedForPower { get; private set; }

        /// <summary>How many times he has come within reach at all.</summary>
        public int Arrivals { get; private set; }

        /// <summary>Whether the mechanism has power, lever or not.</summary>
        public bool Powered => !requirePower || lever == null || lever.Held;

        /// <summary>
        /// Whether Mono is standing close enough right now. The director asks
        /// this before re-sending him, so that a man already standing at the
        /// switch does not pace back and forth because he arrived and the
        /// arrival recalled him.
        /// </summary>
        public bool MonoInReach
        {
            get
            {
                if (mono == null) return false;
                if (!mono.gameObject.activeInHierarchy) return false;
                return Flat(mono.transform.position, transform.position) <= reach;
            }
        }

        /// <summary>
        /// How far Mono is from the switch, or -1 when there is no Mono. Read
        /// by the setup tool to prove the errand radius reaches the switch.
        /// </summary>
        public float DistanceToMono
        {
            get
            {
                if (mono == null) return -1f;
                return Flat(mono.transform.position, transform.position);
            }
        }

        /// <summary>How far Mono has to get. The setup tool prints this.</summary>
        public float Reach => reach;

        /// <summary>Fires on the one frame it latches.</summary>
        public event System.Action Threw;

        void Awake()
        {
            if (mono == null)
            {
                var go = GameObject.Find("Mono");
                if (go != null) mono = go.GetComponent<MonoCompanion>();
            }
        }

        /// <summary>
        /// Flat distance, because reach is about who can get their hands on it.
        /// A switch on a shelf a metre below Mono's chest is not out of his
        /// reach, and measuring in three dimensions would say it was.
        /// </summary>
        static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void Update()
        {
            if (Thrown) return;
            if (mono == null) return;
            if (!mono.gameObject.activeInHierarchy) return;
            if (!MonoInReach) return;

            Arrivals++;

            if (!Powered)
            {
                // Counted once per arrival, not once per frame. A man standing
                // there waiting for power would otherwise push this into the
                // hundreds within a second and it would mean nothing.
                WaitedForPower++;
                return;
            }

            Throw();
        }

        void Throw()
        {
            Thrown = true;

            if (log) Debug.Log("[Echoes] switch thrown after " +
                               Arrivals + " arrival(s), " +
                               WaitedForPower + " of them without power", this);

            if (Threw != null) Threw();
        }

        [SerializeField] bool log = true;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, reach);
        }

        /// <summary>Back to unthrown, for a checkpoint reload.</summary>
        public void ResetSwitch()
        {
            Thrown = false;
            WaitedForPower = 0;
            Arrivals = 0;
        }
    }
}
