using UnityEngine;

namespace Echoes.Painterly
{
    [AddComponentMenu("Echoes/Sleeping Tree")]
    public sealed class SleepingTree : MonoBehaviour
    {
        public static SleepingTree Instance { get; private set; }
        public readonly struct WakeResult
        {
            public WakeResult(bool fired, string why, int targets, float radius)
            {
                this.Fired = fired;
                this.Why = why;
                this.TargetsReached = targets;
                this.Radius = radius;
            }
            public readonly bool Fired;
            public readonly string Why;
            public readonly int TargetsReached;
            public readonly float Radius;

            public static WakeResult Already => new WakeResult(false, "already awoken", 0, 0f);

            public WakeResult Log(string detail, bool log, Object context)
            {
                if (Fired && log)
                {
                    Debug.Log("[Echoes] Beat 3 tree awoken: " + Why + "; burst reached " + TargetsReached + " target(s). " + detail, context);
                }
                return this;
            }
        }

        [Header("What it is")]
        [SerializeField]
        private Transform touchPoint;

        [SerializeField]
        private ColorRestoreTarget selfColour;

        [Header("Who is involved")]
        [SerializeField]
        private MonoCompanion mono;

        [SerializeField]
        private MonoChase chase;

        [Header("Touching it")]
        [Min(0.5f)]
        [SerializeField]
        private float touchDistance = 2.8f;

        [Min(0.5f)]
        [SerializeField]
        private float strokeHitDistance = 3.0f;

        [Header("The burst")]
        [Min(1f)]
        [SerializeField]
        private float burstRadius = 4f;

        [SerializeField]
        private bool burstFx = true;

        [Min(0.1f)]
        [SerializeField]
        private float burstSeconds = 1.8f;

        [Header("Words")]
        [SerializeField]
        private string wakeLineId = "beat3.wake";

        [SerializeField]
        private string thiefLineId = "beat3.thief";

        [SerializeField]
        private string promptText = "[Left Click] Swing brush at the glowing tree";

        [SerializeField]
        private string doneText = "";

        [SerializeField]
        private float doneSeconds = 3.5f;

        [Header("Debug")]
        [SerializeField]
        private bool log;

        private BrushPainter _brush;
        private AriMover _ari;
        private Vector3 _point;
        private bool _resolved;
        private bool _awoken;

        public bool IsAwoken => _awoken;
        public Vector3 TouchPoint => _point;
        public float BurstRadius => burstRadius;
        public float TouchDistance => touchDistance;

        private void OnEnable()
        {
            Instance = this;
            _awoken = false;
            Resolve();
            if (_brush != null)
            {
                _brush.Stroked += OnStroked;
            }
        }

        private void OnDisable()
        {
            if (_brush != null)
            {
                _brush.Stroked -= OnStroked;
            }
        }

        private void Reset()
        {
            touchPoint = transform;
        }

        public void Resolve()
        {
            if (_point == Vector3.zero)
            {
                _point = (touchPoint != null) ? touchPoint.position : transform.position;
            }
            if (_brush == null)
            {
                GameObject val = GameObject.Find("Ari");
                if (val != null)
                {
                    _ari = val.GetComponent<AriMover>();
                    _brush = val.GetComponentInChildren<BrushPainter>();
                }
                if (_brush == null && Camera.main != null)
                {
                    _brush = Camera.main.GetComponent<BrushPainter>();
                }
            }
            if (mono == null)
            {
                mono = MonoCompanion.FindInLevel();
            }
            if (chase == null)
            {
                GameObject val2 = GameObject.Find("MonoChase");
                if (val2 != null)
                {
                    chase = val2.GetComponent<MonoChase>();
                }
            }
            if (selfColour == null)
            {
                selfColour = GetComponentInChildren<ColorRestoreTarget>();
            }
            _resolved = true;
        }

        private void Update()
        {
            if (Beat1Intro.IntroActive) return;
            if (!_awoken)
            {
                if (!_resolved)
                {
                    Resolve();
                }
                if (_brush != null && _ari != null)
                {
                    if (AriIsClose())
                    {
                        BeatPrompt.Show(promptText);
                    }
                    else if (BeatPrompt.Current == promptText)
                    {
                        BeatPrompt.Clear();
                    }
                }
            }
        }

        public bool AriIsClose()
        {
            if (_ari == null) return false;
            Vector3 val = _ari.transform.position - _point;
            val.y = 0f;
            return val.sqrMagnitude <= touchDistance * touchDistance;
        }

        private void OnStroked(Vector3 worldPosition, int targets)
        {
            if (Beat1Intro.IntroActive) return;
            if (!_awoken)
            {
                if (!_resolved) Resolve();
                bool flag = Vector3.Distance(worldPosition, _point) <= strokeHitDistance;
                bool flag2 = AriIsClose();
                if (flag || flag2)
                {
                    Wake("the stroke landed on the tree" + (flag2 ? " (and he was beside it)" : "")).Log("stroke at " + worldPosition.ToString("F1"), log, this);
                }
            }
        }

        public WakeResult Wake(string why = "called directly")
        {
            if (Beat1Intro.IntroActive) return WakeResult.Already;
            if (!_resolved) Resolve();
            if (_awoken) return WakeResult.Already;

            _awoken = true;
            int targets = 0;
            if (burstFx)
            {
                BrushStrokeFx.Spawn(_point, Vector3.up, burstRadius * 0.5f);
                targets = 1;
            }
            if (mono != null)
            {
                mono.Wake();
            }

            BeatPrompt.Clear();
            if (!string.IsNullOrEmpty(doneText))
            {
                BeatPrompt.Show(doneText, doneSeconds);
            }

            return new WakeResult(true, why, targets, burstRadius);
        }

        [ContextMenu("Reset Beat 3")]
        public void ResetBeat()
        {
            _awoken = false;
            _resolved = false;
            if (selfColour != null)
            {
                selfColour.SetRestoreImmediate(0f);
            }
            if (mono != null)
            {
                mono.Unwake();
            }
            if (chase != null)
            {
                chase.End();
            }
            ColorRestoreTarget.RestoreInRadius(_point, burstRadius, 0f, 0.2f);
            BeatPrompt.Clear();
            Resolve();
        }
    }
}
