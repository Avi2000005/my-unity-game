using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// The lever Ari has to keep down.
    ///
    /// "Ari holds the lever" is the beat. The *shape* of that instruction is a
    /// design decision and not a description, and the obvious version of it is
    /// wrong here: a lever she grips with her hand needs a hold-to-press input,
    /// and this level has exactly one button — the brush, which is the whole
    /// verb of the game. Spending the brush on a door would teach the player
    /// that painting opens things, which is the precise thing the game is not
    /// about.
    ///
    /// So it is weighted rather than gripped. She stands on it, her weight holds
    /// it down, and stepping off lets it rise. Same instruction, no new keys,
    /// and it reads to a thirteen-year-old without being explained: the thing is
    /// down because she is on it, and the moment she is not on it, it is not.
    ///
    /// Nothing about the lever decides the gate. It only reports whether the
    /// mechanism has power, and the switch in the crawlspace is what latches
    /// while that power is live. That separation is the whole puzzle: she cannot
    /// be in two places, and the beat is about working out that she does not
    /// have to be.
    /// </summary>
    [AddComponentMenu("Echoes/Hold Lever")]
    public sealed class HoldLever : MonoBehaviour
    {
        [Header("Who is holding it")]
        [Tooltip("Ari. Left empty she is found by name at Awake.")]
        [SerializeField] AriMover ari;

        [Tooltip("How close she must be, measured flat on the ground. Loose " +
                 "on purpose: she is stopped by her capsule rather than her " +
                 "centre, so a tight radius would go live while she still " +
                 "looked like she was beside it.")]
        [Min(0.2f)] [SerializeField] float radius = 1.05f;

        [Tooltip("Only counts while she is on the ground. Standing on a wall " +
                 "is not holding a lever, and a capsule sweep lets her stand " +
                 "on things a plate should not respond to.")]
        [SerializeField] bool requireGrounded = true;

        [Header("Reading")]
        [Tooltip("Whether the mechanism has power right now.")]
        public bool Held { get; private set; }

        /// <summary>How long she has been standing on it, in total.</summary>
        public float HeldSeconds { get; private set; }

        /// <summary>
        /// How many times she has let go. Read by the setup tool and by a live
        /// probe, because a lever that silently never registers her would leave
        /// the beat unsolvable and nothing on screen would say so.
        /// </summary>
        public int Releases { get; private set; }

        /// <summary>Fires on every change of Held, with the new value.</summary>
        public event System.Action<bool> Changed;

        /// <summary>Where the lever's working surface is, for the setup tool.</summary>
        public Vector3 PlateCentre => transform.position;

        void Awake()
        {
            if (ari == null) Resolve();
        }

        bool _resolved;

        void Resolve()
        {
            _resolved = true;
            if (ari != null) return;

            var go = GameObject.Find("Ari");
            if (go == null)
            {
                Debug.LogWarning("[Echoes] " + name +
                                 ": no Ari in the scene, so the lever can never be held.");
                return;
            }

            ari = go.GetComponent<AriMover>();
            if (ari == null)
                Debug.LogWarning("[Echoes] " + name + ": Ari has no AriMover.");
        }

        void Update()
        {
            if (!_resolved) Resolve();

            bool now = IsSheOnIt();
            if (now == Held) return;

            Held = now;

            if (!now) Releases++;

            if (log) Debug.Log("[Echoes] lever " + (now ? "held" : "released") +
                               " after " + HeldSeconds.ToString("0.0") + " s", this);

            if (Changed != null) Changed(now);
        }

        bool IsSheOnIt()
        {
            if (ari == null) return false;
            if (requireGrounded && !ari.IsGrounded) return false;

            var a = ari.transform.position;
            var b = transform.position;
            a.y = 0f;
            b.y = 0f;

            return Vector3.Distance(a, b) <= radius;
        }

        [Header("Notes")]
        [SerializeField] bool log = true;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }

        /// <summary>Put it back for a checkpoint reload.</summary>
        public void ResetLever()
        {
            Held = false;
            HeldSeconds = 0f;
            Releases = 0;
        }

        // The first version kept an OnValidate whose whole body was
        // `if (Held) HeldSeconds += 0f;` under a comment about counting time
        // so the total would survive a checkpoint. It did nothing, and the
        // comment described a design that did not exist — HeldSeconds is
        // accumulated in Update.
        //
        // Left in place it is worse than absent: it looks like the place where
        // the lever's timing is decided, so the next person to change how long
        // the beat holds it for will edit this line and see no effect.
    }
}
