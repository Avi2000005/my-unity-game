using UnityEngine;

namespace Echoes.Painterly
{
    [AddComponentMenu("Echoes/Colour Fragment")]
    public sealed class ColourFragment : MonoBehaviour, IInteractable, IResettable
    {
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
        }

        public void Interact(AriMover by)
        {
            if (!_taken && IsShowing)
            {
                _taken = true;
                if (_visual != null)
                {
                    _visual.gameObject.SetActive(false);
                }

                AriHudOverlay.CarryingFragment = true;
                AriAnim.PlayCollect();

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
        }

        public void ResetForCheckpoint()
        {
            _taken = false;
            AriHudOverlay.CarryingFragment = false;
            _spot = transform.position;
            Show(!startsHidden);
        }
    }
}
