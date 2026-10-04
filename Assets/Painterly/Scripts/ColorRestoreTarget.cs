using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// Drives the _ColorRestore value on every Renderer beneath this object, so a
    /// whole building (or a whole cluster of them) can fade from the Grey Realm
    /// back to full colour as one unit.
    ///
    /// Values are pushed through a shared MaterialPropertyBlock rather than by
    /// cloning materials, so a thousand buildings still share ten materials.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu("Echoes/Color Restore Target")]
    public sealed class ColorRestoreTarget : MonoBehaviour
    {
        public static readonly int ColorRestoreId = Shader.PropertyToID("_ColorRestore");
        public static readonly int RestoreBoostId = Shader.PropertyToID("_RestoreBoost");

        // Created lazily rather than in static field initialisers. A static
        // initialiser runs the first time the type is touched, which here is inside
        // AddComponent<ColorRestoreTarget>() — and Unity rejects native object
        // construction from a MonoBehaviour constructor context
        // ("CreateImpl is not allowed to be called from a MonoBehaviour constructor").
        static List<ColorRestoreTarget> _active;
        static List<ColorRestoreTarget> Active =>
            _active ?? (_active = new List<ColorRestoreTarget>());

        static MaterialPropertyBlock _block;
        static MaterialPropertyBlock Block =>
            _block ?? (_block = new MaterialPropertyBlock());

        [Header("State")]
        [Tooltip("0 = fully grey, 1 = full colour. The Grey Realm starts at 0.")]
        [Range(0f, 1f)] [SerializeField] float startRestore;

        [Tooltip("Seconds for a full grey-to-colour transition.")]
        [Min(0f)] [SerializeField] float duration = 1.5f;

        [Header("Flare")]
        [Tooltip("How much the restored colour overshoots the moment it lands, so a " +
                 "fresh stroke reads as a flare before settling. 1 = no flare.")]
        [Range(1f, 3f)] [SerializeField] float boostAmount = 1.7f;

        [Tooltip("Seconds for the flare to settle back to normal.")]
        [Min(0f)] [SerializeField] float boostDuration = 0.7f;

        Renderer[] _renderers;

        // Animation state. Timers only ever advance in Update; Push() is pure output.
        float _from;
        float _to;
        float _restoreTimer;
        float _restoreLength;
        bool _restoring;

        float _boostTimer;
        bool _boosting;
        bool _dirty = true;

        Bounds _worldBounds;
        bool _boundsValid;

        /// <summary>
        /// World-space AABB of every renderer this target owns.
        ///
        /// Radius queries must use this rather than <see cref="Center"/>. The
        /// centre is the transform pivot, which for a kit building sits at the
        /// footprint origin while the walls it drives rise six metres above it —
        /// so aiming at the top of a wall measured ~7.5m away and reported
        /// "0 target(s)" even though the player was pointing directly at the
        /// building they were trying to paint. Measuring to the volume instead
        /// means a stroke that lands anywhere on a surface gives distance zero,
        /// which is what "paint what you are aiming at" has to mean.
        /// </summary>
        public Bounds WorldBounds
        {
            get
            {
                if (!_boundsValid) RebuildBounds();
                return _worldBounds;
            }
        }

        /// <summary>
        /// Cached for the lifetime of the target. The village is static, and
        /// the alternative is walking every owned renderer's bounds on every
        /// candidate test of every stroke — 27 targets against dozens of rays.
        /// </summary>
        void RebuildBounds()
        {
            if (_renderers == null || _renderers.Length == 0)
            {
                // A target that owns no renderers still has to return something
                // sane. An uninitialised Bounds sits at the origin, which would
                // make a target in the far corner of the village appear to be at
                // world zero and silently paint whatever happened to be there.
                _worldBounds = new Bounds(transform.position, Vector3.zero);
                _boundsValid = true;
                return;
            }

            bool first = true;
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;

                if (first) { _worldBounds = r.bounds; first = false; }
                else _worldBounds.Encapsulate(r.bounds);
            }

            if (first) _worldBounds = new Bounds(transform.position, Vector3.zero);
            _boundsValid = true;
        }

        /// <summary>
        /// Transform pivot. Retained for callers that want a single point, but
        /// <b>not</b> for radius queries — use <see cref="WorldBounds"/>, which
        /// measures the volume actually painted.
        /// </summary>
        public Vector3 Center => transform.position;

        /// <summary>Current 0..1 colour restoration for this target.</summary>
        public float Restore => Mathf.Lerp(_from, _to, RestoreT);

        float RestoreT => _restoreLength <= 0f ? 1f : Mathf.Clamp01(_restoreTimer / _restoreLength);

        public static IReadOnlyList<ColorRestoreTarget> AllActive => Active;

        void Awake()
        {
            _renderers = CollectOwnedRenderers();
            _from = _to = Mathf.Clamp01(startRestore);
            _dirty = true;
            RebuildBounds();
        }

        /// <summary>
        /// Every renderer under this target that no *nested* target has claimed.
        ///
        /// GetComponentsInChildren alone is wrong once targets can nest. The market
        /// square needs its own target so its paving, kerb and loading dock can be
        /// painted, but the landmark tower is a child of the square and already has
        /// a target of its own. Collected naively, the square would also drive the
        /// tower's renderers and the two would fight over the same property block
        /// every frame — which shows up as a building that flickers between two
        /// restore values rather than as an obvious error.
        ///
        /// So ownership goes to the *nearest* ancestor target, and each renderer is
        /// driven by exactly one component.
        /// </summary>
        Renderer[] CollectOwnedRenderers()
        {
            var all = GetComponentsInChildren<Renderer>(true);
            var kept = new List<Renderer>(all.Length);

            for (int i = 0; i < all.Length; i++)
            {
                var r = all[i];
                if (r == null) continue;

                // Walk up to the nearest ColorRestoreTarget. If that is anybody but
                // this component, the renderer belongs to them instead.
                var owner = r.GetComponentInParent<ColorRestoreTarget>();
                if (owner != null && owner != this) continue;

                kept.Add(r);
            }

            return kept.ToArray();
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        void Update()
        {
            if (_restoring)
            {
                _restoreTimer += Time.deltaTime;
                if (_restoreLength <= 0f || _restoreTimer >= _restoreLength)
                {
                    _restoreTimer = _restoreLength;
                    _from = _to;
                    _restoring = false;
                }
                _dirty = true;
            }

            if (_boosting)
            {
                _boostTimer += Time.deltaTime;
                if (boostDuration <= 0f || _boostTimer >= boostDuration)
                {
                    _boosting = false;
                }
                _dirty = true;
            }

            if (_dirty)
            {
                _dirty = false;
                Push();
            }
        }

        /// <summary>Jump to a restoration value with no animation.</summary>
        public void SetRestoreImmediate(float value)
        {
            _from = _to = Mathf.Clamp01(value);
            _restoring = false;
            _boosting = false;
            _dirty = true;

            // Push now rather than waiting for Update. Editor tooling calls this to
            // stage the Grey Realm, and Update does not run outside Play Mode — so
            // waiting would leave the property blocks unwritten and the change
            // invisible in the Scene view.
            Push();
        }

        /// <summary>
        /// Animate toward a restoration value. Flares on the way up so colour
        /// arriving reads as an event rather than a fade.
        /// </summary>
        public void RestoreTo(float value, float overrideDuration = -1f)
        {
            value = Mathf.Clamp01(value);

            // Only flare when colour is actually arriving, never when it drains away.
            if (value > _to + 0.001f && boostAmount > 1f)
            {
                _boosting = true;
                _boostTimer = 0f;
            }

            _from = Restore;
            _to = value;
            _restoreLength = overrideDuration >= 0f ? overrideDuration : duration;
            _restoreTimer = 0f;
            _restoring = true;
            _dirty = true;

            // Apply the first frame immediately so the animation starts from the
            // right value instead of lingering on the previous one for a frame.
            Push();
        }

        /// <summary>Send the current value to every child renderer.</summary>
        void Push()
        {
            // Build lazily as well as in Awake: Push() is reachable from the public
            // API, and a target created via AddComponent in the editor may not have
            // been through Awake yet.
            if (_renderers == null) _renderers = CollectOwnedRenderers();
            if (_renderers == null) return;

            float restore = Restore;

            float boost = 1f;
            if (_boosting)
            {
                float t = boostDuration <= 0f ? 1f : Mathf.Clamp01(_boostTimer / boostDuration);
                // Ease out so the flare snaps on and relaxes gently.
                boost = Mathf.Lerp(boostAmount, 1f, 1f - (1f - t) * (1f - t));
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;

                r.GetPropertyBlock(Block);
                Block.SetFloat(ColorRestoreId, restore);
                Block.SetFloat(RestoreBoostId, boost);
                r.SetPropertyBlock(Block);
            }
        }

        /// <summary>
        /// Every active target whose volume falls within <paramref name="radius"/>
        /// of <paramref name="point"/>. Reuses a shared buffer, so this allocates
        /// nothing per call.
        ///
        /// Distance is measured to the target's <see cref="WorldBounds"/>, not its
        /// pivot: a stroke landing on a wall sits inside that building's volume and
        /// so reads as distance zero, which is the only interpretation where
        /// "aim at a building and it recolours" actually holds.
        /// </summary>
        public static int RestoreInRadius(Vector3 point, float radius, float target = 1f,
                                          float overrideDuration = -1f)
        {
            float sqr = radius * radius;
            int count = 0;

            for (int i = 0; i < Active.Count; i++)
            {
                var t = Active[i];
                if (t == null) continue;

                if (t.WorldBounds.SqrDistance(point) <= sqr)
                {
                    t.RestoreTo(target, overrideDuration);
                    count++;
                }
            }

            return count;
        }

        /// <summary>Set every active target in the scene, e.g. to grey the world on load.</summary>
        public static void SetAllImmediate(float value)
        {
            for (int i = 0; i < Active.Count; i++)
            {
                if (Active[i] != null) Active[i].SetRestoreImmediate(value);
            }
        }
    }
}
