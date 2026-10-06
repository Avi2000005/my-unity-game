using UnityEngine;

namespace Echoes.Painterly
{
    [AddComponentMenu("Echoes/Ink Crawler")]
    public sealed class InkCrawler : MonoBehaviour
    {
        public enum State
        {
            Dormant,
            Alerted,
            Closing,
            Lunging,
            Staggered,
            Spent
        }

        [Header("Body")]
        [Min(0.1f)]
        [SerializeField]
        private float bodyHeight = 1.1f;

        [Min(0.05f)]
        [SerializeField]
        private float bodyRadius = 0.34f;

        [Range(0.3f, 1f)]
        [SerializeField]
        private float eyeFraction = 0.85f;

        [Header("Noticing")]
        [Min(0.5f)]
        [SerializeField]
        private float noticeRadius = 15f;

        [Min(0.5f)]
        [SerializeField]
        private float forgetRadius = 25f;

        [Min(0.5f)]
        [SerializeField]
        private float giveUpSeconds = 12f;

        [Min(0f)]
        [SerializeField]
        private float alertSeconds = 0.35f;

        [Header("Moving")]
        [Min(0f)]
        [SerializeField]
        private float crawlSpeed = 1.8f;

        [Min(0f)]
        [SerializeField]
        private float closeSpeed = 2.8f;

        [SerializeField]
        private Vector3 home;

        [Min(0f)]
        [SerializeField]
        private float returnSpeed = 1.5f;

        [Header("Striking")]
        [Min(0f)]
        [SerializeField]
        private float lungeRange = 2.3f;

        [Min(0f)]
        [SerializeField]
        private float standoffRange = 1.5f;

        [Min(0.1f)]
        [SerializeField]
        private float lungeSeconds = 0.45f;

        [Min(0f)]
        [SerializeField]
        private float lungeReach = 1.3f;

        [Header("The brush")]
        [Min(0.2f)]
        [SerializeField]
        private float splashRadius = 1.8f;

        [Min(0.1f)]
        [SerializeField]
        private float staggerSeconds = 1.4f;

        [Min(0f)]
        [SerializeField]
        private float knockback = 1.6f;

        [Header("Animator")]
        [SerializeField]
        private string speedParameter = "Speed";

        [SerializeField]
        private string crawlingParameter = "Crawling";

        [SerializeField]
        private string attackTrigger = "Attack";

        [Header("Wiring")]
        [SerializeField]
        private AriMover ari;

        [SerializeField]
        private MonoCompanion mono;

        [SerializeField]
        private bool log = true;

        private State _state;
        private float _alertLeft;
        private float _staggerLeft;
        private float _lungeLeft;
        private float _unseenSeconds;
        private float _lungeCooldown;
        private Vector3 _lungeFrom;
        private Vector3 _lungeTo;
        private Animator _anim;
        private float _speedParam;
        private int _speedHash;
        private int _crawlHash;
        private int _attackHash;
        private bool _wired;
        private bool _hunting;
        private float _moved;
        private AriHealth _health;

        public State Now => _state;
        public float BodyHeight => bodyHeight;
        public float BodyRadius => bodyRadius;
        public float NoticeRadius => noticeRadius;
        public float ForgetRadius => forgetRadius;
        public float SplashRadius => splashRadius;
        public float StandoffRange => standoffRange;
        public float LungeRange => lungeRange;
        public Vector3 Home => home;
        public int Staggers { get; private set; }
        public int Lunges { get; private set; }
        public float HuntingSeconds { get; private set; }
        public bool EverNoticed { get; private set; }
        public bool Retired { get; private set; }
        public bool LastHitWasBrush { get; private set; }
        public float LastPushMetres { get; private set; }

        public float DistanceToAri
        {
            get
            {
                if (ari != null)
                {
                    return Vector3.Distance(transform.position, ari.transform.position);
                }
                return float.PositiveInfinity;
            }
        }

        public bool CanBeStaggered => _state != State.Staggered && !Retired;
        public bool IsHunting => _hunting;

        public Vector3 Eye => transform.position + Vector3.up * (bodyHeight * eyeFraction);

        public Vector3 AriChest
        {
            get
            {
                if (ari != null)
                {
                    return ari.transform.position + Vector3.up * (ari.BodyHeight * 0.6f);
                }
                return transform.position;
            }
        }

        public float AnimatorSpeed => (_anim != null) ? _anim.GetFloat(_speedHash) : 0f;
        public float MeasuredSpeed => _moved;

        private void Awake()
        {
            if (home == Vector3.zero)
            {
                InkCrawlerEmerge component = GetComponent<InkCrawlerEmerge>();
                home = (component != null) ? component.StandPoint : transform.position;
            }
            Bind();
            EnsureVisible();
        }

        private void Bind()
        {
            if (!_wired)
            {
                _anim = GetComponentInChildren<Animator>(true);
                if (_anim != null)
                {
                    _speedHash = Animator.StringToHash(speedParameter);
                    _crawlHash = Animator.StringToHash(crawlingParameter);
                    _attackHash = Animator.StringToHash(attackTrigger);
                }
                if (ari == null)
                {
                    ari = Object.FindAnyObjectByType<AriMover>(FindObjectsInactive.Include);
                }
                _wired = true;
            }
        }

        private void EnsureVisible()
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] != null && !rends[i].enabled)
                {
                    rends[i].enabled = true;
                }
            }

            // Ensure not sunk below the floor
            if (transform.position.y < -0.05f)
            {
                Vector3 p = transform.position;
                p.y = Mathf.Max(0f, home.y);
                transform.position = p;
            }
        }

        private void Update()
        {
            Bind();
            EnsureVisible();

            float dt = Time.deltaTime;
            if (_state != State.Dormant && _state != State.Spent)
            {
                HuntingSeconds += dt;
            }
            if (_lungeCooldown > 0f) _lungeCooldown -= dt;
            _moved = 0f;

            switch (_state)
            {
                case State.Dormant:
                    Dormant(dt);
                    break;
                case State.Alerted:
                    Alerted(dt);
                    break;
                case State.Closing:
                    Closing(dt);
                    break;
                case State.Lunging:
                    Lunging(dt);
                    break;
                case State.Staggered:
                    Staggered(dt);
                    break;
                case State.Spent:
                    Spent(dt);
                    break;
            }
            Animate();
        }

        public void Aggro()
        {
            _hunting = true;
            Retired = false;
            _unseenSeconds = 0f;
            EverNoticed = true;
            EnsureVisible();

            if (_state == State.Dormant || _state == State.Spent || _state == State.Alerted)
            {
                Enter(State.Closing);
            }
            if (log)
            {
                Debug.Log("[Echoes] " + name + " aggro triggered! Chasing Ari at " + DistanceToAri.ToString("0.0") + "m");
            }
        }

        private void Dormant(float dt)
        {
            HoldStill();
            if (_hunting || SeesHer())
            {
                Notice();
            }
        }

        private void Alerted(float dt)
        {
            HoldStill();
            _alertLeft -= dt;
            FaceHer();
            if (_alertLeft <= 0f || _hunting)
            {
                Enter(State.Closing);
            }
        }

        private void Closing(float dt)
        {
            if (ari == null) return;

            float dist = DistanceToAri;
            if (dist <= lungeRange && _lungeCooldown <= 0f)
            {
                BeginLunge();
                return;
            }

            // Actively advance towards Ari
            float speed = (dist < 4f) ? crawlSpeed : closeSpeed;
            if (_lungeCooldown > 0f)
            {
                speed = 1.3f; // Slower during attack cooldown so player can move
            }
            else if (_hunting)
            {
                speed = Mathf.Max(speed, 2.3f);
            }

            Vector3 moved = Towards(ari.transform.position, dist, speed * dt);
            _moved = (dt > 0f) ? (moved.magnitude / dt) : 0f;

            FaceHer();
            if (moved.sqrMagnitude > 0.0001f)
            {
                Face(moved);
            }
            SetCrawling(true);
        }

        private void Lunging(float dt)
        {
            _lungeLeft -= dt;
            float total = Mathf.Max(0.01f, lungeSeconds);
            float progress = Mathf.Clamp01(1f - (_lungeLeft / total));

            Vector3 targetPos = Vector3.Lerp(_lungeFrom, _lungeTo, progress);
            targetPos.y = transform.position.y;
            Vector3 delta = targetPos - transform.position;

            if (delta.magnitude > 0.001f)
            {
                float travel = Travel(delta.normalized, delta.magnitude);
                transform.position += delta.normalized * travel;
                Face(delta);
                _moved = (dt > 0f) ? (travel / dt) : 0f;
            }

            if (_lungeLeft <= 0f || progress >= 0.9f)
            {
                ResolveLunge();
            }
        }

        private void ResolveLunge()
        {
            if (ari != null && DistanceToAri <= lungeReach + bodyRadius + ari.BodyRadius)
            {
                Vector3 pushDir = ari.transform.position - transform.position;
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude < 0.001f) pushDir = transform.forward;
                pushDir.Normalize();

                LastPushMetres = ari.TryPush(pushDir * 1.0f);
                AriHealth hp = Health();
                if (hp != null)
                {
                    hp.TakeHit();
                }
            }

            _lungeCooldown = 2.2f;
            Enter(State.Closing);
            _unseenSeconds = 0f;
        }

        private AriHealth Health()
        {
            if (_health == null)
            {
                _health = Object.FindAnyObjectByType<AriHealth>(FindObjectsInactive.Include);
            }
            return _health;
        }

        private void Staggered(float dt)
        {
            HoldStill();
            _staggerLeft -= dt;
            if (_staggerLeft <= 0f)
            {
                Enter(State.Closing);
                _unseenSeconds = 0f;
            }
        }

        private void Spent(float dt)
        {
            if (_hunting)
            {
                Enter(State.Closing);
                return;
            }
            SetCrawling(true);
            float d = Vector3.Distance(transform.position, home);
            if (d > 0.3f)
            {
                Vector3 moved = Towards(home, d, returnSpeed * dt);
                Face(moved);
                _moved = (dt > 0f) ? (moved.magnitude / dt) : 0f;
            }
            else
            {
                HoldStill();
                Enter(State.Dormant);
            }
        }

        private bool SeesHer()
        {
            if (ari == null || Retired) return false;
            if (_hunting) return true;
            return DistanceToAri <= noticeRadius;
        }

        private void Notice()
        {
            EverNoticed = true;
            _alertLeft = alertSeconds;
            _unseenSeconds = 0f;
            Enter(State.Alerted);
        }

        private void BeginLunge()
        {
            _lungeLeft = lungeSeconds;
            _lungeFrom = transform.position;
            _lungeTo = (ari != null) ? ari.transform.position : transform.position + transform.forward * lungeReach;
            Lunges++;
            Enter(State.Lunging);
            LastHitWasBrush = false;
            if (_anim != null)
            {
                _anim.SetTrigger(_attackHash);
            }
        }

        public bool Splash(Vector3 strokePoint, Vector3 fromAri)
        {
            if (!CanBeStaggered) return false;

            float d = Vector3.Distance(fromAri, transform.position);
            if (d > splashRadius + bodyRadius) return false;

            _staggerLeft = 2.5f;
            _lungeCooldown = 2.8f;
            Staggers++;
            LastHitWasBrush = true;
            Enter(State.Staggered);

            Vector3 pushDir = transform.position - fromAri;
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude < 0.001f) pushDir = -transform.forward;
            pushDir.Normalize();

            Push(pushDir * 2.4f);
            return true;
        }

        private Vector3 Towards(Vector3 to, float distance, float max)
        {
            Vector3 diff = new Vector3(to.x - transform.position.x, 0f, to.z - transform.position.z);
            if (diff.sqrMagnitude < 0.0001f || distance <= 0f) return Vector3.zero;

            Vector3 dir = diff.normalized;
            float num = Travel(dir, max);

            if (num > 0.001f)
            {
                transform.position += dir * num;
                return dir * num;
            }

            // If directly obstructed, attempt to slide sideways towards Ari:
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            Vector3 tryA = (dir + right * 0.8f).normalized;
            float travelA = Travel(tryA, max * 0.75f);
            if (travelA > 0.001f)
            {
                transform.position += tryA * travelA;
                return tryA * travelA;
            }

            Vector3 tryB = (dir - right * 0.8f).normalized;
            float travelB = Travel(tryB, max * 0.75f);
            if (travelB > 0.001f)
            {
                transform.position += tryB * travelB;
                return tryB * travelB;
            }

            return Vector3.zero;
        }

        private float Travel(Vector3 dir, float want)
        {
            if (want <= 0.0001f) return 0f;
            CapsuleAt(out var bottom, out var top);

            RaycastHit hit;
            if (!Physics.CapsuleCast(bottom, top, bodyRadius * 0.85f, dir, out hit, want, -1, QueryTriggerInteraction.Ignore))
            {
                return want;
            }

            if (hit.collider != null)
            {
                if (hit.collider.transform.IsChildOf(transform) || (ari != null && hit.collider.transform.IsChildOf(ari.transform)))
                {
                    return want;
                }
            }

            return Mathf.Clamp(hit.distance - 0.03f, 0f, want);
        }

        private void CapsuleAt(out Vector3 bottom, out Vector3 top)
        {
            float y = transform.position.y;
            bottom = new Vector3(transform.position.x, y + bodyRadius, transform.position.z);
            top = new Vector3(transform.position.x, y + bodyHeight - bodyRadius, transform.position.z);
            if (top.y < bottom.y) top = bottom;
        }

        private void Push(Vector3 delta)
        {
            float mag = delta.magnitude;
            if (mag > 0.001f)
            {
                Travel(delta.normalized, mag);
            }
        }

        private void FaceHer()
        {
            if (ari != null)
            {
                Vector3 toAri = ari.transform.position - transform.position;
                toAri.y = 0f;
                if (toAri.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(toAri.normalized, Vector3.up);
                }
            }
        }

        private void Face(Vector3 moved)
        {
            if (moved.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(moved.normalized, Vector3.up);
            }
        }

        private void HoldStill()
        {
            SetCrawling(false);
        }

        private void Animate()
        {
            if (_anim != null)
            {
                float smooth = 1f - Mathf.Exp(-Time.deltaTime / 0.05f);
                float cur = _anim.GetFloat(_speedHash);
                _anim.SetFloat(_speedHash, cur + (_moved - cur) * smooth);
            }
        }

        private void SetCrawling(bool on)
        {
            if (_anim != null)
            {
                _anim.SetBool(_crawlHash, on);
            }
        }

        private void Enter(State next)
        {
            _state = next;
            if (next != State.Staggered)
            {
                LastHitWasBrush = false;
            }
        }

        public void ResetCrawler()
        {
            Staggers = 0;
            Lunges = 0;
            HuntingSeconds = 0f;
            EverNoticed = false;
            Retired = false;
            LastHitWasBrush = false;
            LastPushMetres = 0f;
            _state = State.Dormant;
            _alertLeft = _staggerLeft = _lungeLeft = _unseenSeconds = 0f;
            _hunting = false;
            _moved = 0f;
            transform.position = home;
            Physics.SyncTransforms();
            EnsureVisible();
        }


        /// <summary>
        /// Returns true if a Physics raycast from <paramref name="from"/> to <paramref name="to"/>
        /// hits something other than this crawler or Ari, meaning line-of-sight is blocked.
        /// Used by the L1CrawlerProbe editor tool to map visibility coverage.
        /// </summary>
        public bool BlockedFromPost(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.001f) return false;
            RaycastHit hit;
            if (!Physics.Raycast(from, dir / dist, out hit, dist, -1, QueryTriggerInteraction.Ignore))
                return false;
            // Ignore self and Ari
            if (hit.collider != null)
            {
                if (hit.collider.transform.IsChildOf(transform)) return false;
                if (ari != null && hit.collider.transform.IsChildOf(ari.transform)) return false;
            }
            return true;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.1f, 0.7f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, noticeRadius);
            Gizmos.color = new Color(0.6f, 0.1f, 0.7f, 0.18f);
            Gizmos.DrawWireSphere(transform.position, forgetRadius);
            Gizmos.color = new Color(0.2f, 0.9f, 0.9f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, lungeRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(home, 0.3f);
        }
    }
}
