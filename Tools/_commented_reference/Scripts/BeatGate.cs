using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// The gate, and the only thing in the level that this beat moves.
    ///
    /// It slides *down* rather than up. That is not a style choice, it is an
    /// arithmetic one: the wall it sits in is 2.60 m tall and the opening is
    /// 2.10 m, so a gate lifting into the wall would have to travel 2.25 m and
    /// finish 1.85 m above the wall's parapet, hanging in the sky where the
    /// player would watch it. Sliding it down puts the whole slab under the
    /// ground plane, where the existing Ground collider hides it for free and
    /// where no camera angle can catch it.
    ///
    /// The collider moves with the slab rather than being switched off, so a
    /// gate that is open is open because it is up, and if anything ever closes
    /// it again it re-blocks. A gate that works by toggling a collider can be
    /// walked through by anything that arrives on the frame it was disabled.
    /// </summary>
    [AddComponentMenu("Echoes/Beat Gate")]
    public sealed class BeatGate : MonoBehaviour
    {
        [Header("What moves")]
        [Tooltip("The slab. Left empty, the first child of this object.")]
        [SerializeField] Transform slab;

        [Tooltip("Which way the slab travels when it opens. Down, for the " +
                 "reason in the note above this class.")]
        [SerializeField] Vector3 openAxis = Vector3.down;

        [Min(0.10f)] [SerializeField] float travel = 2.25f;

        [Min(0.05f)] [SerializeField] float seconds = 1.7f;

        [Header("What opens it")]
        [Tooltip("The switch that latches it. The director owns the decision " +
                 "in practice; this is only used by Reset.")]
        [SerializeField] LatchingSwitch source;

        [Header("Reading")]
        /// <summary>Fully shut. Ari is blocked.</summary>
        public bool IsClosed => _state == GateState.Closed;

        /// <summary>On its way up — down — to open.</summary>
        public bool IsOpening => _state == GateState.Opening;

        /// <summary>Fully open and latched.</summary>
        public bool IsOpen => _state == GateState.Open;

        /// <summary>How far along the travel, 0 shut to 1 open.</summary>
        public float Progress { get; private set; }

        /// <summary>Fires when the gate reaches fully open.</summary>
        public event System.Action Opened;

        enum GateState { Closed, Opening, Open }

        GateState _state = GateState.Closed;
        Vector3 _closedLocal;
        float _moving;

        /// <summary>
        /// The slab's collider, for the setup tool. Null is a build error the
        /// tool reports rather than a gate that quietly does nothing.
        /// </summary>
        public Collider SlabCollider
        {
            get
            {
                if (slab == null) return null;
                return slab.GetComponent<Collider>();
            }
        }

        /// <summary>The slab, for the setup tool's clearance checks.</summary>
        public Transform Slab => slab;

        void Awake()
        {
            if (slab == null && transform.childCount > 0) slab = transform.GetChild(0);
            if (slab == null)
            {
                Debug.LogWarning("[Echoes] " + name + ": no slab, so this gate " +
                                 "cannot open. The setup tool checks for this.");
                return;
            }

            _closedLocal = slab.localPosition;
        }

        /// <summary>Shut it and put it back where it started.</summary>
        public void Shut()
        {
            if (slab == null) return;
            _state = GateState.Closed;
            Progress = 0f;
            _moving = 0f;
            slab.localPosition = _closedLocal;
        }

        /// <summary>
        /// Start opening. Safe to call every frame — the state guard means the
        /// director can simply ask "is it open?" and act on the answer without
        /// keeping its own memory of whether it already asked.
        /// </summary>
        public void Open()
        {
            if (_state != GateState.Closed) return;
            _state = GateState.Opening;
            _moving = 0f;
        }

        void Update()
        {
            if (_state != GateState.Opening || slab == null) return;

            _moving += Time.deltaTime;
            Progress = Mathf.Clamp01(seconds <= 0f ? 1f : _moving / seconds);

            // Smoothstep, not linear. A gate that starts and stops at constant
            // speed reads as a sliding box; one that eases in and out reads as
            // something heavy being winched. The beat is a one-shot so there is
            // no second chance to notice.
            float e = Progress * Progress * (3f - 2f * Progress);

            slab.localPosition = _closedLocal + openAxis.normalized * travel * e;

            if (Progress >= 1f)
            {
                _state = GateState.Open;
                if (log) Debug.Log("[Echoes] gate open", this);
                if (Opened != null) Opened();
            }
        }

        [SerializeField] bool log = true;

        /// <summary>Back to shut, for a checkpoint reload.</summary>
        public void ResetGate()
        {
            Shut();
            if (source != null) source.ResetSwitch();
        }
    }
}
