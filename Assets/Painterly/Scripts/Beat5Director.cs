using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// Beat 5 — the Ink Crawler. How she learns the brush is not a weapon.
    ///
    /// <para><b>The beat has two endings and the game does not choose between
    /// them.</b> She can fight the crawler with the brush and push it back, or
    /// she can go round it and never be seen. Both reach the far wall. What
    /// differs is recorded on this component rather than scored, because the
    /// brief asks for an optional stealth route and a route that quietly
    /// corrects itself into the only ending is not optional.
    ///
    /// <para><b>There is no way to lose this beat.</b> Nothing here can kill
    /// Ari, damage her, or fail. A crawler lunge pushes her along the ground
    /// and that is the whole of the consequence. This is the teaching beat, and
    /// a tutorial that can be failed is a tutorial the player has to reload.
    ///
    /// <para><b>The stroke is connected through <see cref="BrushPainter.Stroked"/>,
    /// not through a second input path.</b> The brush already raises an event
    /// whenever a stroke genuinely lands — not when a paint was performed
    /// programmatically, which is the distinction Beat 3's tree needed. Opening
    /// a parallel "combat input" here would have meant a second thing that has
    /// to agree with the first about when a swing happened, and the two would
    /// have disagreed the first time something painted without swinging.
    ///
    /// <para><b>Reach is measured from Ari, not from the click.</b> The brush
    /// resolves a click to a world point up to 250 m away, and its colour
    /// radius is 6 m because paint should be generous. Neither number is about
    /// how far her arm reaches, so neither is used. The crawler's own
    /// <c>SplashRadius</c> is a third thing, sized to her arm.
    /// </summary>
    [AddComponentMenu("Echoes/Beat 5 Director")]
    public sealed class Beat5Director : MonoBehaviour
    {
        [Header("Parts")]
        [Tooltip("The crawler. Found in the level if left empty.")]
        [SerializeField] InkCrawler crawler;

        [Tooltip("Ari's brush. Found on Ari if left empty.")]
        [SerializeField] BrushPainter brush;

        [Tooltip("Mono, for the commentary. He is awake by now.")]
        [SerializeField] MonoCompanion mono;

        [Header("The beat")]
        [Tooltip("She has entered the yard past this x. The beat waits for " +
                 "this before it starts reacting, so walking past the beat's " +
                 "west edge is enough to trigger it.")]
        [SerializeField] float enterX = 38f;

        [Tooltip("She has finished the beat past this x.")]
        [SerializeField] float exitX = 48f;

        [Tooltip("How far east of enterX the crawler starts. Far enough that " +
                 "she gets a clear look at it before it moves, which is what " +
                 "makes the stealth route a decision rather than a coin toss.")]
        [SerializeField] float crawlerOffset = 8f;

        [Tooltip("Staggers needed before Mono concedes that the brush works. " +
                 "One teaches that it happened; two teaches that it is the " +
                 "answer. He says nothing after that.")]
        [Min(1)] [SerializeField] int staggersToExplain = 2;

        [SerializeField] bool log = true;

        // --- public read-only -------------------------------------------------

        /// <summary>Where the beat is. Read by the setup tool and the probes.</summary>
        public enum Phase { Waiting, Engaging, Done }

        public Phase Now { get; private set; } = Phase.Waiting;
        public InkCrawler Crawler => crawler;
        public float EnterX => enterX;
        public float ExitX => exitX;
        public int Staggers => crawler == null ? 0 : crawler.Staggers;
        public int Lunges => crawler == null ? 0 : crawler.Lunges;
        public bool EverNoticed => crawler != null && crawler.EverNoticed;
        public bool BeatStarted { get; private set; }
        public float Seconds { get; private set; }

        /// <summary>
        /// How she got through. Only meaningful once <see cref="Now"/> is Done.
        ///
        /// <para>Measured on what happened to her, not on whether the crawler
        /// ever turned its head.</para>
        ///
        /// The first version keyed this on <c>EverNoticed</c> alone, which is
        /// the wrong question and quietly made the stealth route impossible to
        /// report: the yard's exit is under three metres from the crawler's post
        /// and its notice radius is seven, so she is going to be seen on her way
        /// out no matter which side of the wall she walked. A classification
        /// that can only ever say "seen" is a classification measuring nothing.
        ///
        /// So it counts what she had to do instead — how many times the brush
        /// connected, how many lunges she took — and the stealth route is the
        /// one where she had to do neither. A crawler that spots her and then
        /// loses her behind a wall has not failed the route; it has made it
        /// interesting.
        /// </summary>
        public string Route
        {
            get
            {
                if (Now != Phase.Done)
                    return crawler != null && crawler.Retired
                        ? "in progress, it has given her up"
                        : EverNoticed
                            ? "in progress, it has seen her"
                            : "in progress, unseen";

                int staggers = Staggers, lunges = Lunges;

                if (staggers == 0 && lunges == 0)
                    return "stealth — she was never in a fight";

                if (lunges == 0)
                    return "brush — " + staggers + " stagger(s), never touched";

                return "brush — " + staggers + " stagger(s), " + lunges +
                       " lunge(s) taken";
            }
        }

        /// <summary>
        /// Did she get through without ever having to fight.
        ///
        /// The one bit of this the beat's design actually turns on, and
        /// separate from <see cref="Route"/> so a reader is not left inferring
        /// it out of a sentence.
        /// </summary>
        public bool WentUnfought => Now == Phase.Done && Staggers == 0 && Lunges == 0;

        Vector3 _entry;
        bool _saidStagger, _saidNoKill, _saidLunge, _saidUnseen;
        BrushPainter _subscribed;

        void Awake()
        {
            if (crawler == null)
                crawler = FindAnyObjectByType<InkCrawler>(FindObjectsInactive.Include);
            if (mono == null)
                mono = MonoCompanion.FindInLevel();
        }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (brush == null)
            {
                var ari = FindAnyObjectByType<AriMover>(FindObjectsInactive.Include);
                if (ari != null) brush = ari.GetComponent<BrushPainter>();
            }

            if (brush == null || _subscribed == brush) return;
            brush.Stroked += OnStroke;
            _subscribed = brush;
        }

        void Unsubscribe()
        {
            if (_subscribed == null) return;
            _subscribed.Stroked -= OnStroke;
            _subscribed = null;
        }

        void Update()
        {
            if (Now == Phase.Done) return;
            Seconds += Time.deltaTime;

            var ari = AriNow();
            if (ari == null) return;

            float x = ari.transform.position.x;

            if (!BeatStarted)
            {
                if (x < enterX) return;
                BeatStarted = true;
                _entry = ari.transform.position;
                Now = Phase.Engaging;
                if (log) Debug.Log("[Echoes] beat 5 begun at " + _entry.ToString("F2"), this);
            }

            if (x >= exitX)
            {
                Now = Phase.Done;
                Say(mono, "beat5.done");
                if (log) Debug.Log("[Echoes] beat 5 done after " +
                                   Seconds.ToString("0.0") + " s — " + Route, this);
                return;
            }

            Commentary();
        }

        /// <summary>
        /// The two lines that are about what is *not* happening.
        ///
        /// Both are deliberate silences made audible. Nothing in the beat tells
        /// the player that being hit is survivable or that going unseen is
        /// allowed, because neither is true until something says it — and a
        /// player who does not know the crawler cannot kill her will either
        /// refuse to fight it or expect to die, and both make the beat worse.
        /// </summary>
        void Commentary()
        {
            if (crawler == null) return;

            // Fires the moment a lunge actually reaches her, not when one starts — a
            // line that fires on the wind-up tells her she has been hit for
            // something that has not happened yet.
            SayOnce(crawler.Lunges > 0 && crawler.LastPushMetres > 0f,
                    ref _saidLunge, "beat5.hits_back");

            // The moment it gives her up. This used to key on "still unseen at twelve
            // seconds", which fired while she was walking through the first
            // stretch of the yard before the crawler had any chance to see her,
            // and congratulated her for a route she had not chosen yet. It also
            // stopped firing entirely once she took the fight, because by then
            // she had been seen — so the only beat in this level with an
            // optional route had no line for taking it.
            SayOnce(crawler.Retired, ref _saidUnseen, "beat5.unseen");
        }

        AriMover AriNow()
        {
            var a = FindAnyObjectByType<AriMover>(FindObjectsInactive.Include);
            return a;
        }

        /// <summary>
        /// Ari swung the brush and it landed.
        ///
        /// The only place the crawler can be hurt, by the only verb the player
        /// has. Nothing is subtracted from anything — this returns a bool and
        /// calls <c>Splash</c>, and there is deliberately no damage, no health
        /// and no kill behind it.
        /// </summary>
        void OnStroke(Vector3 point, int painted)
        {
            if (crawler == null || Now == Phase.Done) return;

            var ari = AriNow();
            if (ari == null) return;

            bool hit = crawler.Splash(point, ari.transform.position);
            if (!hit) return;

            // He comments on the second one, not the first. The first stagger
            // is the player finding out; the second is the game confirming, and
            // a line that fires on every hit turns the beat into a narration
            // machine.
            if (!_saidStagger && crawler.Staggers >= Mathf.Max(1, staggersToExplain))
            {
                _saidStagger = true;
                Say(mono, "beat5.push");
            }

            if (!_saidNoKill && crawler.Staggers >= 1)
            {
                _saidNoKill = true;
                Say(mono, "beat5.nokill");
            }
        }

        /// <summary>
        /// Say a line at most once, and only while <paramref name="when"/>
        /// holds.
        ///
        /// The parameter is the *condition*, not a flag meaning "only once" —
        /// the latching is the point and it is what <paramref name="said"/>
        /// carries. The first version took a bool called `once` and returned
        /// when it was true, which meant the one line that had a real trigger
        /// condition could never fire and the two that did not were the only
        /// ones that worked.
        /// </summary>
        void SayOnce(bool when, ref bool said, string line)
        {
            if (!when || said) return;
            said = true;
            Say(mono, line);
        }

        static void Say(MonoCompanion speaker, string id)
        {
            if (speaker != null && !string.IsNullOrEmpty(id)) speaker.SayBeat(id);
        }

        /// <summary>Put the beat back to the start. For a checkpoint reload.</summary>
        public void ResetBeat()
        {
            Now = Phase.Waiting;
            BeatStarted = false;
            Seconds = 0f;
            _saidStagger = _saidNoKill = _saidLunge = _saidUnseen = false;
            if (crawler != null) crawler.ResetCrawler();
        }

        /// <summary>Where the crawler stands. Placed by the setup tool so it is
        /// measured off the yard rather than typed in twice.</summary>
        public Vector3 WantedCrawlerPoint => new Vector3(enterX + crawlerOffset, 0f, transform.position.z);
    }
}