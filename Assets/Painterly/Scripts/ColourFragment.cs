using UnityEngine;

namespace Echoes.Painterly
{
    [AddComponentMenu("Echoes/Colour Fragment")]
    public sealed class ColourFragment : MonoBehaviour, IInteractable, IResettable
    {
        public static ColourFragment Instance { get; private set; }
        private GameObject _beacon;
        [Header("Reach")]
        [Min(0.5f)]
        [SerializeField]
        private float range = 2.8f;

        [Header("Words")]
        [SerializeField]
        private string promptText = "[E] Take the Blue Fragment";

        [SerializeField]
        private string carriedText = "Mono: You got the Blue Fragment! Bring it to the fountain!";

        [SerializeField]
        private string pickupLineId = "beat6.take";

        [Header("When it appears")]
        [SerializeField]
        private bool startsHidden;

        [SerializeField]
        private bool log = true;

        [Header("The glow")]
        [Min(0.05f)]
        [SerializeField]
        private float glowSize = 0.65f;

        [SerializeField]
        private bool pulse = true;

        [Min(0.1f)]
        [SerializeField]
        private float pulseSpeed = 2.0f;

        [Min(0f)]
        [SerializeField]
        private float pulseAmount = 0.35f;

        private Renderer[] _glow;
        private Transform _visual;
        private MonoCompanion _mono;
        private bool _taken;
        private Vector3 _spot;
        private bool _shown = true;

        public string Prompt => promptText;
        public float Range => range;
        public Transform At => transform;

        public bool CanInteract => !_taken && IsShowing;

        public bool IsShowing
        {
            get
            {
                if (startsHidden) return _shown;
                return true;
            }
        }

        public bool Taken => _taken;
        public Vector3 Spot => _spot;

        private void Awake()
        {
            Instance = this;
            CreateBeacon();
            _mono = MonoCompanion.FindInLevel();
            _visual = transform.Find("Glow");
            if (_visual == null)
            {
                _visual = (transform.childCount > 0) ? transform.GetChild(0) : null;
            }
            if (_visual != null)
            {
                _glow = _visual.GetComponentsInChildren<Renderer>(true);
            }
            _spot = transform.position;

            Show(!startsHidden);
            if (_beacon != null) _beacon.SetActive(!startsHidden);
        }

        public void Show(bool on)
        {
            if (!_taken)
            {
                _shown = on;
                if (_visual != null)
                {
                    _visual.gameObject.SetActive(on);
                }
                if (_beacon != null)
                {
                    _beacon.SetActive(on);
                }
            }
        }

        public void PlaceAt(Vector3 where)
        {
            _spot = where;
            transform.position = where;
        }

        private void Update()
        {
            if (pulse && _visual != null && !_taken && _visual.gameObject.activeSelf)
            {
                float num = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                _visual.localScale = Vector3.one * (glowSize * num);
            }
            if (_beacon != null && _beacon.activeSelf)
            {
                float bPulse = 0.45f + Mathf.Sin(Time.time * 2.5f) * 0.08f;
                _beacon.transform.localScale = new Vector3(bPulse, 9f, bPulse);
            }
        }

        private void CreateBeacon()
        {
            if (_beacon != null) return;
            _beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _beacon.name = "WaterFragmentSkyBeacon";
            _beacon.transform.SetParent(transform, false);
            _beacon.transform.localPosition = new Vector3(0f, 9f, 0f);
            _beacon.transform.localScale = new Vector3(0.45f, 9f, 0.45f);

            Collider c = _beacon.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Renderer r = _beacon.GetComponent<Renderer>();
            if (r != null)
            {
                Shader s = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
                if (s != null)
                {
                    Material m = new Material(s);
                    m.color = new Color(0.18f, 0.72f, 1f, 0.65f);
                    r.material = m;
                }
            }
        }

        public void Interact(AriMover by)
        {
            if (!_taken && IsShowing)
            {
                _taken = true;
                StartCoroutine(PickupRoutine(by));
            }
        }

        private System.Collections.IEnumerator PickupRoutine(AriMover by)
        {
            // Briefly pause Ari during the reach down so he does not slide
            if (by != null)
            {
                by.Frozen = true;
                Vector3 toFrag = transform.position - by.transform.position;
                toFrag.y = 0f;
                if (toFrag.sqrMagnitude > 0.001f)
                {
                    by.transform.forward = toFrag.normalized;
                }
            }

            // Snappy collect animation plays immediately
            AriAnim.PlayCollect();

            // Wait until Ari's hand reaches down to the fragment (~0.22s)
            yield return new WaitForSeconds(0.22f);

            // Hide world visual and give fragment to Ari
            if (_visual != null)
            {
                _visual.gameObject.SetActive(false);
            }
            if (_beacon != null)
            {
                _beacon.SetActive(false);
            }
            AriHudOverlay.CarryingFragment = true;
            BrushStrokeFx.Spawn(transform.position, Vector3.up, 1.2f);

            // Short follow-through as Ari stands back up (~0.18s)
            yield return new WaitForSeconds(0.18f);

            if (by != null)
            {
                by.Frozen = false;
            }

            if (_mono != null)
            {
                _mono.SayDirect("You got the water fragment! Quick, bring it to the fountain and fix it there!");
            }

            BeatPrompt.Show("Objective: Bring the Blue Water Fragment to the fountain!", 5f);

            if (log)
            {
                Debug.Log("[Echoes] Blue fragment picked up! Carry it to the fountain.");
            }
        }

        public void ResetForCheckpoint()
        {
            StopAllCoroutines();
            _taken = false;
            AriHudOverlay.CarryingFragment = false;
            _spot = transform.position;
            Show(!startsHidden);
        }
    }
}
