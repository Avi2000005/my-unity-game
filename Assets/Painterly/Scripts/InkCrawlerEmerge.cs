using UnityEngine;

namespace Echoes.Painterly
{
    [AddComponentMenu("Echoes/Ink Crawler Emerge")]
    [RequireComponent(typeof(InkCrawler))]
    public sealed class InkCrawlerEmerge : MonoBehaviour, IResettable
    {
        public enum Phase
        {
            Submerged,
            Rising,
            Up
        }

        [Header("Timing")]
        [Min(0.05f)]
        [SerializeField]
        private float emergeSeconds = 0.5f;

        [Min(0.05f)]
        [SerializeField]
        private float sinkSeconds = 0.45f;

        [Min(0f)]
        [SerializeField]
        private float extraDepth = 0.35f;

        [Header("Behaviour")]
        [SerializeField]
        private bool startSubmerged = false;

        [Range(0f, 1f)]
        [SerializeField]
        private float colliderAt = 0.3f;

        [SerializeField]
        private bool log = true;

        [SerializeField]
        private Vector3 authoredStand;

        [SerializeField]
        private bool hasAuthoredStand;

        private InkCrawler _crawler;
        private Collider[] _colliders;
        private Renderer[] _renderers;
        private Vector3 _rest;
        private bool _restCaptured;
        private float _t;
        private float _leg;
        private float _depth;
        private int _rises;

        public Phase Now { get; private set; } = Phase.Up;
        public bool IsReady => Now == Phase.Up;
        public float EmergeSeconds => emergeSeconds;

        public Vector3 RestPoint => _rest;

        public Vector3 StandPoint
        {
            get
            {
                if (!_restCaptured) return transform.position;
                return _rest;
            }
        }

        private void Awake()
        {
            _crawler = GetComponent<InkCrawler>();
            _colliders = GetComponentsInChildren<Collider>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
            _rest = transform.position;
            _restCaptured = true;
            _depth = Depth();

            // Default to fully visible and ready:
            Now = Phase.Up;
            SetSolid(true);

            if (startSubmerged)
            {
                Submerge(silent: true);
            }
        }

        private float Depth()
        {
            float num = 0f;
            SkinnedMeshRenderer skin = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin != null)
            {
                float h = SkinHeight.Measure(skin, out var _, out var _);
                if (h > 0.0001f) num = h;
            }
            if (num <= 0.0001f && _colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i] != null)
                    {
                        num = Mathf.Max(num, _colliders[i].bounds.size.y);
                    }
                }
            }
            return num + Mathf.Max(0f, extraDepth);
        }

        public void Submerge(bool silent = false)
        {
            if (!_restCaptured)
            {
                _rest = transform.position;
                _restCaptured = true;
            }
            _depth = Depth();
            Now = Phase.Submerged;
            _t = 0f;
            Vector3 rest = _rest;
            rest.y -= _depth;
            transform.position = rest;
            SetSolid(false);
        }

        public void Emerge()
        {
            Now = Phase.Up;
            if (_restCaptured)
            {
                transform.position = _rest;
            }
            SetSolid(true);
            _rises++;
            if (log)
            {
                Debug.Log("[Echoes] " + name + " emerged and solid at " + transform.position);
            }
        }

        private void Update()
        {
            if (Now == Phase.Rising)
            {
                _t += Time.deltaTime / Mathf.Max(0.01f, emergeSeconds);
                _leg = Mathf.Clamp01(_t);
                float curved = 1f - (1f - _leg) * (1f - _leg);
                Vector3 pos = transform.position;
                pos.y = Mathf.Lerp(_rest.y - _depth, _rest.y, curved);
                transform.position = pos;

                if (_leg >= colliderAt && !Solid())
                {
                    SetSolid(true);
                }

                if (_leg >= 1f)
                {
                    transform.position = _rest;
                    Now = Phase.Up;
                    SetSolid(true);
                }
            }
            else if (Now == Phase.Up)
            {
                // Ensure always solid and visible when Up
                if (!Solid())
                {
                    SetSolid(true);
                }
            }
        }

        private bool Solid()
        {
            if (_renderers != null && _renderers.Length > 0 && _renderers[0] != null)
            {
                return _renderers[0].enabled;
            }
            return true;
        }

        public void SetSolid(bool on)
        {
            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i] != null) _colliders[i].enabled = on;
                }
            }

            if (_renderers != null)
            {
                for (int j = 0; j < _renderers.Length; j++)
                {
                    if (_renderers[j] != null) _renderers[j].enabled = on;
                }
            }
        }

        public void ResetForCheckpoint()
        {
            if (_restCaptured)
            {
                transform.position = _rest;
            }
            Now = Phase.Up;
            SetSolid(true);
            _rises = 0;
        }

        public Vector3 AuthoredStand => authoredStand;
        public bool HasAuthoredStand => hasAuthoredStand;

        public void RememberAuthoredStand(Vector3 where)
        {
            if (hasAuthoredStand) return;
            authoredStand = where;
            hasAuthoredStand = true;
        }

        public bool RestoreAuthoredStand()
        {
            if (!hasAuthoredStand) return false;
            transform.position = authoredStand;
            return true;
        }
    }
}
