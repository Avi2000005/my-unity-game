using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Echoes.Painterly
{
    [AddComponentMenu("Echoes/Beat 5 Director")]
    public sealed class Beat5Director : MonoBehaviour, IResettable
    {
        public enum Phase
        {
            Waiting,
            Rising,
            Engaging,
            Done
        }

        [Header("Parts")]
        [SerializeField]
        private List<InkCrawler> crawlers = new List<InkCrawler>(3);

        [SerializeField]
        private BrushPainter brush;

        [SerializeField]
        private MonoCompanion mono;

        [Header("The beat")]
        [SerializeField]
        private bool huntOnMonoWake = false;

        [SerializeField]
        private float enterX = 38f;

        [SerializeField]
        private float exitX = 51f;

        [Min(1f)]
        [SerializeField]
        private int staggersToExplain = 2;

        [Header("The entrance")]
        [Min(0f)]
        [SerializeField]
        private float emergeStagger = 0.2f;

        [Min(0f)]
        [SerializeField]
        private float emergeDelay = 0.5f;

        [SerializeField]
        private bool log = true;

        private Vector3 _entry;
        private bool _saidStagger;
        private bool _saidNoKill;
        private bool _saidLunge;
        private bool _saidUnseen;
        private bool _saidFragmentHint;
        private bool _beganOnWake;
        private BrushPainter _subscribed;

        public Phase Now { get; private set; }

        public InkCrawler Crawler => (crawlers != null && crawlers.Count > 0) ? crawlers[0] : null;

        public int CrawlerCount => (crawlers != null) ? crawlers.Count : 0;

        public IReadOnlyList<InkCrawler> Crawlers => crawlers;

        public float EnterX => enterX;
        public float ExitX => exitX;

        public int Staggers => Sum(c => c.Staggers);
        public int Lunges => Sum(c => c.Lunges);
        public bool EverNoticed => Any(c => c.EverNoticed);

        public bool BeatStarted { get; private set; }
        public float Seconds { get; private set; }

        public int Emerged
        {
            get
            {
                int num = 0;
                if (crawlers == null) return num;
                for (int i = 0; i < crawlers.Count; i++)
                {
                    if (crawlers[i] != null)
                    {
                        InkCrawlerEmerge e = crawlers[i].GetComponent<InkCrawlerEmerge>();
                        if (e != null && e.IsReady) num++;
                    }
                }
                return num;
            }
        }

        public string Route
        {
            get
            {
                if (Now != Phase.Done)
                {
                    return "in progress, combat active";
                }
                return "completed";
            }
        }

        public bool WentUnfought => Now == Phase.Done && Staggers == 0 && Lunges == 0;
        public bool BeganOnWake => _beganOnWake;

        public void SetCrawlers(IEnumerable<InkCrawler> set)
        {
            crawlers = new List<InkCrawler>(3);
            if (set == null) return;
            foreach (var item in set)
            {
                if (item != null && !crawlers.Contains(item))
                {
                    crawlers.Add(item);
                }
            }
        }

        private int Sum(Func<InkCrawler, int> read)
        {
            int num = 0;
            if (crawlers == null) return num;
            for (int i = 0; i < crawlers.Count; i++)
            {
                if (crawlers[i] != null) num += read(crawlers[i]);
            }
            return num;
        }

        private bool Any(Func<InkCrawler, bool> read)
        {
            if (crawlers == null) return false;
            for (int i = 0; i < crawlers.Count; i++)
            {
                if (crawlers[i] != null && read(crawlers[i])) return true;
            }
            return false;
        }

        private void Awake()
        {
            if (crawlers == null) crawlers = new List<InkCrawler>(3);

            if (crawlers.Count == 0)
            {
                InkCrawler[] found = Object.FindObjectsByType<InkCrawler>(FindObjectsInactive.Include);
                for (int j = 0; j < found.Length; j++)
                {
                    crawlers.Add(found[j]);
                }
            }

            if (mono == null)
            {
                mono = MonoCompanion.FindInLevel();
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (brush == null)
            {
                AriMover mover = Object.FindAnyObjectByType<AriMover>(FindObjectsInactive.Include);
                if (mover != null) brush = mover.GetComponent<BrushPainter>();
            }
            if (brush != null && _subscribed != brush)
            {
                brush.Stroked += OnStroke;
                _subscribed = brush;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribed != null)
            {
                _subscribed.Stroked -= OnStroke;
                _subscribed = null;
            }
        }

        private void Update()
        {
            if (Now == Phase.Done) return;

            Seconds += Time.deltaTime;
            AriMover ari = AriNow();
            if (ari == null) return;

            float x = ari.transform.position.x;

            if (!BeatStarted)
            {
                if (!MonoCompanion.WakeDialogueCompleted) return;

                ColourFragment frag = Object.FindAnyObjectByType<ColourFragment>();
                bool distTrigger = (x >= 20f) || (frag != null && Vector3.Distance(ari.transform.position, frag.transform.position) <= 22f);

                if (!distTrigger) return;

                BeatStarted = true;
                _entry = ari.transform.position;
                _beganOnWake = false;
                Now = Phase.Rising;
                BeginEntrance();

                BeatPrompt.Show("⚠️ INK CRAWLERS! Hold [SHIFT] to sprint fast and avoid their attacks! Reach the water fragment!", 7f);

                if (mono != null)
                {
                    mono.SayDirect("Watch out Ari! Ink Crawlers sent by the Color Thief! You cannot hurt them with color yet — hold SHIFT to sprint fast and dodge their attacks! Get the water fragment!");
                }

                if (log)
                {
                    Debug.Log("[Echoes] Beat 5 activated! Ink Crawlers introduced and attacking Ari.");
                }
            }

            if (Now == Phase.Rising)
            {
                Now = Phase.Engaging;
            }

            // Periodic fragment hint from Mono if combat is ongoing:
            if (BeatStarted && !_saidFragmentHint && Seconds >= 3.5f)
            {
                _saidFragmentHint = true;
                BeatPrompt.Show("Mono: Ari! Look near the wall — I see a blue fragment glowing!", 4.5f);
            }

            Commentary();
        }

        private void BeginEntrance()
        {
            if (crawlers == null || crawlers.Count == 0)
            {
                crawlers = new List<InkCrawler>(Object.FindObjectsByType<InkCrawler>(FindObjectsInactive.Include));
            }

            for (int i = 0; i < crawlers.Count; i++)
            {
                if (crawlers[i] == null) continue;
                InkCrawler c = crawlers[i];
                InkCrawlerEmerge e = c.GetComponent<InkCrawlerEmerge>();
                if (e != null)
                {
                    e.Emerge();
                }
                c.Aggro();
            }

            BeatPrompt.Show("Mono: Look out, Ari! Ink Crawlers are attacking! Push them back with your brush!", 4.5f);
        }

        private void Commentary()
        {
            SayOnce(Lunges > 0, ref _saidLunge, "beat5.hits_back");
        }

        private AriMover AriNow()
        {
            return Object.FindAnyObjectByType<AriMover>(FindObjectsInactive.Include);
        }

        private void OnStroke(Vector3 point, int painted)
        {
            if (Now == Phase.Done || crawlers == null) return;

            AriMover ari = AriNow();
            if (ari == null) return;

            int hitCount = 0;
            for (int i = 0; i < crawlers.Count; i++)
            {
                if (crawlers[i] != null && crawlers[i].Splash(point, ari.transform.position))
                {
                    hitCount++;
                }
            }

            if (hitCount > 0)
            {
                if (!_saidStagger && Staggers >= Mathf.Max(1, staggersToExplain))
                {
                    _saidStagger = true;
                    Say(mono, "beat5.push");
                }
            }
        }

        private void SayOnce(bool when, ref bool said, string line)
        {
            if (when && !said)
            {
                said = true;
                Say(mono, line);
            }
        }

        private static void Say(MonoCompanion speaker, string id)
        {
            if (speaker != null && !string.IsNullOrEmpty(id))
            {
                speaker.SayBeat(id);
            }
        }

        public void ResetForCheckpoint()
        {
            ResetBeat();
        }

        public void ResetBeat()
        {
            Now = Phase.Waiting;
            BeatStarted = false;
            Seconds = 0f;
            _entry = Vector3.zero;
            _saidStagger = false;
            _saidNoKill = false;
            _saidLunge = false;
            _saidUnseen = false;
            _saidFragmentHint = false;

            if (crawlers != null)
            {
                for (int i = 0; i < crawlers.Count; i++)
                {
                    if (crawlers[i] != null)
                    {
                        crawlers[i].ResetCrawler();
                        InkCrawlerEmerge e = crawlers[i].GetComponent<InkCrawlerEmerge>();
                        if (e != null)
                        {
                            e.ResetForCheckpoint();
                        }
                    }
                }
            }
            LevelRunner.CancelAll();
        }
    }
}
