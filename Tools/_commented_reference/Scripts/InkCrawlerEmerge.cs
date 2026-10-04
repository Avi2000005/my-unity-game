using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// The crawlers come up out of the ground.
    ///
    /// <para><b>They sink first and surface second, on purpose.</b> A crawler
    /// that is simply standing there when the beat starts is a target that was
    /// always in the level, and the player reads it as scenery until it moves.
    /// One that is visibly under the cobbles and then comes up is something
    /// arriving, which is the whole point of three of them arriving at once.</para>
    ///
    /// <para><b>It moves the whole root, not a child, and says so in the report.</b>
    /// The alternative — sliding a visual child down and leaving the root up —
    /// keeps the logic's idea of where the crawler is correct while it is
    /// underground, which sounds tidier and is in fact worse: the collider stays
    /// at head height on an empty patch of floor and Ari walks into an invisible
    /// obstacle. Moving the root moves the collider with it, and the collider is
    /// switched off while it is down, so nothing can be hit by a thing that is
    /// not there yet.</para>
    ///
    /// <para><b>The sink is a rest position, not a one-shot.</b> A checkpoint
    /// retry puts them back underground so the entrance plays again rather than
    /// dumping the player into a fight with three crawlers already awake and no
    /// memory of where they came from.</para>
    /// </summary>
    [AddComponentMenu("Echoes/Ink Crawler Emerge")]
    [RequireComponent(typeof(InkCrawler))]
    public sealed class InkCrawlerEmerge : MonoBehaviour, IResettable
    {
        public enum Phase { Submerged, Rising, Up }

        [Header("Timing")]
        [Tooltip("Seconds from fully under to standing. Long enough to see, " +
                 "short enough that the beat does not stop and wait.")]
        [Min(0.05f)] [SerializeField] float emergeSeconds = 1.1f;

        [Tooltip("Seconds to sink again on a reset. Shorter than the rise, " +
                 "because a retry should not make the player watch three " +
                 "crawlers disappear slowly.")]
        [Min(0.05f)] [SerializeField] float sinkSeconds = 0.45f;

        [Tooltip("How deep under the floor to put it. Zero means measure it " +
                 "from the collider, which is what stops a crawler being only " +
                 "half-sunk by a number that was typed for a different model.")]
        [Min(0f)] [SerializeField] float extraDepth = 0.35f;

        [Header("Behaviour")]
        [Tooltip("Start under the floor. Off is for a crawler that is meant to " +
                 "be standing there when the level opens.")]
        [SerializeField] bool startSubmerged = true;

        [Tooltip("Put the collider back at this fraction of the rise rather " +
                 "than at the top. Slightly early on purpose: a crawler that " +
                 "is already standing but cannot be hit for a quarter of a " +
                 "second reads as a bug in the stagger rule, which is the one " +
                 "rule this beat is teaching.")]
        [Range(0f, 1f)] [SerializeField] float colliderAt = 0.6f;

        [SerializeField] bool log = true;

        // --- public read-only -------------------------------------------------

        public Phase Now { get; private set; } = Phase.Up;
        public bool IsReady => Now == Phase.Up;
        public float EmergeSeconds => emergeSeconds;

        /// <summary>Where it stands once it is up. Measured, not authored.</summary>
        public Vector3 RestPoint => _rest;

        /// <summary>
        /// Where it stands, before Awake has run.
        ///
        /// <para>Exists because <c>InkCrawler.Awake</c> needs this point and
        /// cannot be trusted to read it off its own transform: Awake order
        /// between two components on one GameObject is undefined, so by the time
        /// the crawler asks, this one may already have sunk the root a metre.
        /// </para>
        /// </summary>
        public Vector3 StandPoint =>
            _restCaptured ? _rest : transform.position;

        InkCrawler _crawler;
        Collider[] _colliders;
        Renderer[] _renderers;
        Vector3 _rest;
        bool _restCaptured;
        float _t;
        float _leg;
        float _depth;
        int _rises;

        void Awake()
        {
            _crawler = GetComponent<InkCrawler>();
            _colliders = GetComponentsInChildren<Collider>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);

            _rest = transform.position;
            _depth = Depth();

            if (startSubmerged) Submerge(true);
        }

        float Depth()
        {
            // Off the SKIN, not off the collider.
            //
            // Measured on this crawler: skin 0.698 m, capsule 0.96 m. Sinking by
            // the capsule puts it 0.26 m deeper than the creature is tall, which
            // is 0.6 m of extra travel on the rise and means the thing surfaces
            // late and near the top of its emergence, where the ease-out has
            // already slowed to a crawl. The collider is a beat-sized box for
            // gameplay; the skin is what the player watches go under the paving.
            float h = 0f;

            var smr = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr != null)
            {
                float baked = SkinHeight.Measure(smr, out _, out _);
                if (baked > 0.0001f) h = baked;
            }

            // Fall back to the collider only when there is no skin at all, and
            // say so — a crawler with no skin is a different defect and this
            // should not quietly paper over it by sinking a logic box.
            if (h <= 0.0001f)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i] == null) continue;
                    h = Mathf.Max(h, _colliders[i].bounds.size.y);
                }

                if (h > 0.0001f && log)
                    Debug.LogWarning("[Echoes] " + name + " has no measurable skin, " +
                                     "so its sink depth is taken from the collider " +
                                     "(" + h.ToString("0.00") + " m). It is sinking a " +
                                     "box, not the thing the player sees.", this);
            }

            return h + Mathf.Max(0f, extraDepth);
        }

        /// <summary>Put it under the floor at once. No animation.</summary>
        public void Submerge(bool silent = false)
        {
            // The rest point is captured ONCE, in Awake, and not re-read here.
            //
            // <para>Re-reading it was a bug with a long fuse. The first call
            // happens in Awake with the crawler standing, so `_rest` is right.
            // The second call — a checkpoint retry — happened with the crawler
            // already a metre under the paving, so it captured <i>that</i> as
            // the place to stand, and sank it a second time. Two retries put a
            // crawler two metres under the yard: no renderer, no collider, an
            // eye below the floor where the paving blocks every line of sight,
            // and an Emerge that raises it to a spot it never belongs. It fails
            // silently and only after the player has already lost.</para>
            //
            // <para>`Emerge` is the place that legitimately moves `_rest`, and
            // only in x and z — because by then it knows where the crawler is
            // standing.</para>
            if (!_restCaptured)
            {
                _rest = transform.position;
                _restCaptured = true;
            }

            // Re-measure every time. The crawler's own height can change when the
            // setup tool re-fits it, and a sink depth measured once and cached
            // is a crawler that is two-thirds visible after a re-scale.
            _depth = Depth();

            Now = Phase.Submerged;
            _t = 0f;

            var p = _rest;
            p.y -= _depth;
            transform.position = p;

            SetSolid(false);

            if (!silent && log)
                Debug.Log("[Echoes] " + name + " sunk " + _depth.ToString("0.00") +
                          " m", this);
        }

        /// <summary>Bring it up. Idempotent while it is already up.</summary>
        public void Emerge()
        {
            if (Now == Phase.Up) return;

            _rest = new Vector3(transform.position.x,
                                _rest.y,
                                transform.position.z);

            Now = Phase.Rising;
            _t = 0f;
            _rises++;

            if (log)
                Debug.Log("[Echoes] " + name + " rising, rise " + _rises, this);
        }

        void Update()
        {
            if (Now != Phase.Rising) return;

            _t += Time.deltaTime / Mathf.Max(0.01f, emergeSeconds);
            _leg = Mathf.Clamp01(_t);

            // Ease out. Ink coming up through a crack is fast at first and then
            // slows as it clears the floor — linear motion reads as a lift.
            float e = 1f - (1f - _leg) * (1f - _leg);

            var p = transform.position;
            p.y = Mathf.Lerp(_rest.y - _depth, _rest.y, e);
            transform.position = p;

            if (_leg >= colliderAt && !Solid()) SetSolid(true);

            if (_leg < 1f) return;

            // Land exactly on the rest point rather than wherever the last eased
            // frame left it, so a beat that measures this crawler's position
            // reads the same number every time.
            transform.position = _rest;
            Now = Phase.Up;
            SetSolid(true);

            if (log)
                Debug.Log("[Echoes] " + name + " up at " + _rest.ToString("F2"), this);
        }

        bool Solid()
        {
            return _colliders != null && _colliders.Length > 0 && _colliders[0].enabled;
        }

        /// <summary>
        /// Colliders and renderers together.
        ///
        /// <para>Renderers are switched off as well as colliders because the
        /// ground in this village is paving with a thickness, not a plane —
        /// a crawler 0.35 m under it is visible from a low camera, and a
        /// half-submerged crawler is worse than one that is either there or is
        /// not.</para>
        /// </summary>
        void SetSolid(bool on)
        {
            for (int i = 0; i < _colliders.Length; i++)
                if (_colliders[i] != null) _colliders[i].enabled = on;

            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].enabled = on;
        }

        /// <summary>
        /// Back underground, so the entrance plays again.
        ///
        /// <para>Implements <see cref="IResettable"/>, which is what
        /// <c>LevelCheckpoint.Retry</c> scans for. Without this a retry would
        /// drop the player into a yard with three crawlers already standing in
        /// it and no memory of the entrance.</para>
        /// </summary>
        public void ResetForCheckpoint()
        {
            // Instant, not animated. A retry is already a jump; animating three
            // crawlers into the floor first means the player watches 1.5 s of
            // nothing before the level is playable again.
            Submerge(true);
            _rises = 0;
        }
    }
}