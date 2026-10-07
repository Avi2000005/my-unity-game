using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{
    [AddComponentMenu("Echoes/Fountain Fix")]
    public sealed class FountainFix : MonoBehaviour, IInteractable, IResettable
    {
        public static FountainFix Instance { get; private set; }
        [Header("Reach")]
        [Min(0.5f)]
        [SerializeField]
        private float range = 4.5f;

        [Header("Words")]
        [SerializeField]
        private string promptWithFragment = "[E] Restore Fountain with Blue Fragment";

        [SerializeField]
        private string promptWithout = "[E] The basin is dry and grey. Find the Blue Fragment near the wall!";

        [SerializeField]
        private string fixLineId = "beat7.fix";

        [Header("The fix")]
        [Min(0.2f)]
        [SerializeField]
        private float fillSeconds = 2.0f;

        [Min(0f)]
        [SerializeField]
        private float villageDelay = 1.5f;

        [Min(0.2f)]
        [SerializeField]
        private float villageSeconds = 3.5f;

        [Range(0f, 1f)]
        [SerializeField]
        private float villageAmount = 0.5f;

        [Header("Parts")]
        [SerializeField]
        private ColorRestoreTarget water;

        [SerializeField]
        private MonoCompanion mono;

        [SerializeField]
        private bool log = true;

        private bool _done;
        private readonly List<ColorRestoreTarget> _flooded = new List<ColorRestoreTarget>(256);

        public string Prompt
        {
            get
            {
                if (!AriHudOverlay.CarryingFragment)
                {
                    return promptWithout;
                }
                return promptWithFragment;
            }
        }

        public float Range => range;
        public Transform At => transform;

        public bool CanInteract => !_done && !LevelState.Completed;

        public bool Done => _done;
        public bool Unsealed { get; private set; }
        public float Seconds { get; private set; }
        public int FloodedTargets { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (mono == null)
            {
                mono = MonoCompanion.FindInLevel();
            }
            if (water == null)
            {
                ColorRestoreTarget[] comps = GetComponentsInChildren<ColorRestoreTarget>(true);
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i].gameObject.name.ToLowerInvariant().Contains("water"))
                    {
                        water = comps[i];
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (_done)
            {
                Seconds += Time.deltaTime;
            }
        }

        public void Interact(AriMover by)
        {
            if (_done || LevelState.Completed) return;

            if (!AriHudOverlay.CarryingFragment)
            {
                BeatPrompt.Show("The fountain needs the Blue Fragment from the wall!", 3f);
                return;
            }

            _done = true;
            AriHudOverlay.CarryingFragment = false;
            VanquishCrawlers();

            if (water != null)
            {
                water.RestoreTo(1f, fillSeconds);
            }

            if (mono != null)
            {
                mono.SayDirect("The water has returned to the fountain! The vibrant blue color is restored! You did it, Ari!");
            }
            BeatPrompt.Show("LEVEL 1 COMPLETE! The village fountain has regained its color!", 6f);

            LevelRunner.RunAfter(villageDelay, FloodVillage);

            if (log)
            {
                Debug.Log("[Echoes] Fountain restored with Blue Fragment! Restoring village colors...");
            }
        }

        public void FloodVillage()
        {
            ColorRestoreTarget.SetSealed(false);
            Unsealed = true;

            var all = ColorRestoreTarget.AllActive;
            _flooded.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null) _flooded.Add(all[i]);
            }

            for (int j = 0; j < _flooded.Count; j++)
            {
                _flooded[j].RestoreTo(villageAmount, villageSeconds);
            }
            FloodedTargets = _flooded.Count;

            LevelState.Completed = true;
            VanquishCrawlers();

            AriFollowCamera cam = Object.FindAnyObjectByType<AriFollowCamera>(FindObjectsInactive.Include);
            if (cam != null)
            {
                cam.StartVictoryCinematic(transform.position);
            }

            if (log)
            {
                Debug.Log("[Echoes] LEVEL 1 COMPLETE! The village has begun to regain its color.");
            }
        }

        private void VanquishCrawlers()
        {
            InkCrawler[] crawlers = Object.FindObjectsByType<InkCrawler>(FindObjectsInactive.Include);
            for (int i = 0; i < crawlers.Length; i++)
            {
                if (crawlers[i] != null)
                {
                    crawlers[i].Die();
                }
            }
        }

        public void ResetForCheckpoint()
        {
            _done = false;
            Unsealed = false;
            Seconds = 0f;
            FloodedTargets = 0;
            _flooded.Clear();
            LevelState.Completed = false;
            ColorRestoreTarget.SetSealed(true);
            if (water != null)
            {
                water.SetRestoreImmediate(0f);
            }
        }
    }
}
