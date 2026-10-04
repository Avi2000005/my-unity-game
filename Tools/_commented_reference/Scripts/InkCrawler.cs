using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// The Ink Crawler of Level 1.
    ///
    /// <para><b>It cannot be killed, and there is deliberately no way to ask it to.</b>
    /// There is no health field, no damage method and no death state on this
    /// component. The only thing the player can do to a crawler is push it back
    /// with the brush, and that is a stagger — it recovers and comes again.
    /// Level 1 is the beat where the game teaches that the brush is not a
    /// weapon, and a component with a health bar that the player empties teaches
    /// the opposite lesson while the text says the right one.
    ///
    /// The animator has a terminal <c>Crawler_Death</c> state and a <c>Die</c>
    /// trigger, and neither is ever used. They are wired because the controller
    /// is a shared asset for all the cast and a later level needs them. Nothing
    /// in <see cref="Beat5Director"/> sends that trigger, and
    /// <c>Beat5Setup.Verify</c> asserts the component has no path that can.
    ///
    /// <para><b>Every speed here is below Ari's.</b> She walks at 2.2 m/s and
    /// runs at 3.6. The crawler walks at <see cref="crawlSpeed"/> and closes at
    /// <see cref="closeSpeed"/>. A creature faster than the girl cannot be a
    /// stealth option, because stealth only means something if leaving is
    /// available. These two numbers are the difference between "it is hunting
    /// me" and "it is unavoidable", and the second one is a bug.
    ///
    /// <para><b>Notice needs line of sight.</b> Distance alone would make the
    /// stealth route depend entirely on where the ruin walls happen to be,
    /// and a wall the crawler can see over is not cover. The sight ray is cast
    /// from its eye, so a low wall genuinely hides her.
    ///
    /// <para><b>Hysteresis.</b> It notices at <see cref="noticeRadius"/> and
    /// only loses her at <see cref="forgetRadius"/>, which is larger. Equal
    /// thresholds make a creature standing on the boundary flicker in and out of
    /// the chase every time its own movement moved it a centimetre, and a
    /// tutorial that flickers teaches the player that nothing it does is
    /// reliable.
    /// </summary>
    [AddComponentMenu("Echoes/Ink Crawler")]
    public sealed class InkCrawler : MonoBehaviour
    {
        /// <summary>What it is doing. Read by the beat and by the probes.</summary>
        public enum State
        {
            /// <summary>At its post. Has not seen her.</summary>
            Dormant,

            /// <summary>Has seen her. The beat before it moves — this is the
            /// moment the player learns they are being hunted.</summary>
            Alerted,

            /// <summary>Coming for her, stopping short to lunge.</summary>
            Closing,

            /// <summary>Committed to a lunge at where she was.</summary>
            Lunging,

            /// <summary>Knocked over by the brush. Recovers.</summary>
            Staggered,

            /// <summary>Gave her up and is walking back. The stealth path.</summary>
            Spent
        }

        [Header("Body")]
        [Tooltip("Standing height in metres. Measured, not assumed — Beat5Setup " +
                 "reads this back off the placed model and fails the build if the " +
                 "figure the designer typed does not match.")]
        [Min(0.1f)] [SerializeField] float bodyHeight = 1.10f;

        [Min(0.05f)] [SerializeField] float bodyRadius = 0.34f;

        [Tooltip("Where its eyes are, as a fraction of standing height. Below 1 " +
                 "so a low wall hides her and a tall one does not.")]
        [Range(0.3f, 1f)] [SerializeField] float eyeFraction = 0.85f;

        [Header("Noticing")]
        [Tooltip("Sees her inside this, with line of sight.")]
        [Min(0.5f)] [SerializeField] float noticeRadius = 7f;

        [Tooltip("Loses her outside this. Larger than the notice radius on " +
                 "purpose, so it does not flicker on its own boundary.")]
        [Min(0.5f)] [SerializeField] float forgetRadius = 11f;

        [Tooltip("Seconds without seeing her before it gives up and goes home. " +
                 "This is what makes the stealth route finish rather than turn " +
                 "into a chase that never ends.")]
        [Min(0.5f)] [SerializeField] float giveUpSeconds = 6f;

        [Tooltip("Seconds it stands up straight before moving. Long enough to " +
                 "be noticed as a change, short enough not to be a cutscene.")]
        [Min(0f)] [SerializeField] float alertSeconds = 0.7f;

        [Header("Moving")]
        [Tooltip("Slower than Ari's 2.2 m/s walk.")]
        [Min(0f)] [SerializeField] float crawlSpeed = 1.5f;

        [Tooltip("Faster than her walk, slower than her 3.6 m/s run.")]
        [Min(0f)] [SerializeField] float closeSpeed = 2.6f;

        [Tooltip("Where it returns to when it gives her up.")]
        [SerializeField] Vector3 home;

        [Tooltip("It walks to home at this speed. Slower than closing, so " +
                 "giving up reads as leaving, not as repositioning.")]
        [Min(0f)] [SerializeField] float returnSpeed = 1.2f;

        [Header("Striking")]
        [Tooltip("Closer than this and it lunges.")]
        [Min(0f)] [SerializeField] float lungeRange = 2.2f;

        [Tooltip("It walks no closer than this. Past here it would be inside " +
                 "the brush's reach with no room to swing.")]
        [Min(0f)] [SerializeField] float standoffRange = 1.9f;

        [Min(0.1f)] [SerializeField] float lungeSeconds = 0.55f;

        [Tooltip("How far it comes in on the lunge.")]
        [Min(0f)] [SerializeField] float lungeReach = 1.1f;

        [Header("The brush")]
        [Tooltip("A stroke landing this close to her staggers it. Measured " +
                 "from Ari, not from where the click landed — the click can " +
                 "resolve a hundred metres down the lane and the reach is hers.")]
        [Min(0.2f)] [SerializeField] float splashRadius = 1.6f;

        [Tooltip("Seconds knocked over.")]
        [Min(0.1f)] [SerializeField] float staggerSeconds = 1.4f;

        [Tooltip("Metres pushed back per splash.")]
        [Min(0f)] [SerializeField] float knockback = 1.3f;

        [Header("Animator")]
        [SerializeField] string speedParameter = "Speed";
        [SerializeField] string crawlingParameter = "Crawling";
        [SerializeField] string attackTrigger = "Attack";

        [Header("Wiring")]
        [Tooltip("Ari. Searched if left empty, and searched inactive — she is " +
                 "never inactive, but Mono is, and this is the pattern that " +
                 "keeps working when someone copies it.")]
        [SerializeField] AriMover ari;

        [Tooltip("Optional. Cleared the moment it gives her up, so a spent " +
                 "crawler follows her instead of walking into the next beat.")]
        [SerializeField] MonoCompanion mono;

        [SerializeField] bool log = true;

        // --- public read-only, for the beat and the probes ------------------

        public State Now => _state;
        public float BodyHeight => bodyHeight;
        public float NoticeRadius => noticeRadius;
        public float ForgetRadius => forgetRadius;
        public float SplashRadius => splashRadius;
        public float StandoffRange => standoffRange;
        public float LungeRange => lungeRange;
        public Vector3 Home => home;

        /// <summary>How many times the brush has staggered it. The beat's score.</summary>
        public int Staggers { get; private set; }

        /// <summary>How many times it has landed a lunge on her.</summary>
        public int Lunges { get; private set; }

        /// <summary>Seconds it has spent actually hunting her, not dormant.</summary>
        public float HuntingSeconds { get; private set; }

        /// <summary>True once it has seen her even once. The stealth score is
        /// the negation of this.</summary>
        public bool EverNoticed { get; private set; }

        /// <summary>
        /// It has given her up and will not pick her up again this beat.
        ///
        /// <para>This is the switch that makes the stealth route a route.</para>
        ///
        /// The geometry will not do it on its own. The crawler stands at
        /// (49, 8.5) and the yard's exit is at (51, 6.4) — under three metres
        /// away, and its notice radius is seven. So even a crawler that has
        /// stopped hunting and walked all the way home will see her walk out,
        /// because she has to walk past where it lives to leave. Cover alone
        /// is not enough; something has to end the pursuit for good.
        ///
        /// Hence one latch. Once it has given up, it goes back to its post,
        /// stands there, and does not notice her again for the rest of the
        /// beat. Beat 5.5 is where the difficulty lives in this level and it
        /// is three crawlers that do not give up at all; this one exists to
        /// teach that the brush works and that there is a way past, and a
        /// teaching creature which can resume hunting at any moment teaches
        /// the player to expect that from every creature in the game, which
        /// is exactly the wrong lesson to teach in the first fight.
        /// </summary>
        public bool Retired { get; private set; }

        /// <summary>Left in <see cref="State.Staggered"/> on a stagger, so the
        /// beat can tell a knockdown from a lunge — they share a state on
        /// purpose, and only this distinguishes them.</summary>
        public bool LastHitWasBrush { get; private set; }

        /// <summary>How far Ari was pushed by the last lunge, in metres.</summary>
        public float LastPushMetres { get; private set; }

        public float DistanceToAri =>
            ari == null ? float.PositiveInfinity
                        : Vector3.Distance(transform.position, ari.transform.position);

        /// <summary>
        /// Can a splash knock it off what it is doing?
        ///
        /// Not while already staggered, and not once it is retired.
        ///
        /// The retired case is the one that was missed, and it was not a
        /// cosmetic one. A retired crawler walks back to its post, walks back
        /// *through* wherever she happens to be standing, and sits down facing
        /// the yard it no longer cares about. If the brush could still stagger
        /// it, a click landing on that scenery knocked it into
        /// <c>Staggered</c>, and 1.4 s later <c>Staggered</c> hands it to
        /// <c>Closing</c>.
        ///
        /// <c>Closing</c> checks whether it can see her before it walks and
        /// before it lunges, but with the latch on, "can it see her" is
        /// permanently false — so it stood still for six seconds, gave up,
        /// and did it again, forever, every time she passed. A crawler
        /// post-Battle that flinches when you walk past it, forever, is the
        /// clearest possible statement that this thing was never really done.
        ///
        /// The refusal is here rather than in the states because that is the
        /// only place from which it is a rule about the world instead of a
        /// special case each state would have to remember.
        /// </summary>
        public bool CanBeStaggered => _state != State.Staggered && !Retired;

        State _state = State.Dormant;
        float _alertLeft;
        float _staggerLeft;
        float _lungeLeft;
        float _unseenSeconds;
        Vector3 _lungeFrom;
        Vector3 _lungeTo;
        Animator _anim;
        float _speedParam;
        int _speedHash, _crawlHash, _attackHash;
        bool _wired;

        void Awake()
        {
            home = home == Vector3.zero ? transform.position : home;
            Bind();
        }

        void Bind()
        {
            if (_wired) return;

            // Its own children or nothing. There is deliberately no
            // `FindAnyObjectByType<Animator>()` fallback here, and that used to
            // be the line below: a scene-wide search for any animator at all.
            //
            // It looks like a harmless convenience and it is the worst line in
            // the file. If this creature's own Animator were ever missing —
            // renamed prefab, controller stripped by a reimport, a body added
            // without one — the search would hand it the *first* animator it
            // found anywhere in the level, which in this village is Ari's. It
            // would then drive her Animator from the crawler's crawl parameter
            // and its attack trigger: the girl would walk in with a spider's
            // gait and lunge on a keypress meant for a creature ten metres
            // away. Nothing would error. It would look like a bug in the
            // animator, and it would be debugged as one for a long time.
            //
            // A missing animator on a creature that cannot animate is a
            // finding, not a condition to paper over, so it is reported as one.
            _anim = GetComponentInChildren<Animator>(true);
            if (_anim == null && log)
                Debug.LogWarning("[Echoes] " + name + " has no Animator in its " +
                                 "children, so it will move without animating",
                                 this);
            if (_anim != null)
            {
                _speedHash = Animator.StringToHash(speedParameter);
                _crawlHash = Animator.StringToHash(crawlingParameter);
                _attackHash = Animator.StringToHash(attackTrigger);
            }

            if (ari == null)
                ari = FindAnyObjectByType<AriMover>(FindObjectsInactive.Include);

            _wired = true;
        }

        void Update()
        {
            Bind();
            float dt = Time.deltaTime;

            if (_state != State.Dormant && _state != State.Spent)
                HuntingSeconds += dt;

            switch (_state)
            {
                case State.Dormant:   Dormant(dt);   break;
                case State.Alerted:   Alerted(dt);   break;
                case State.Closing:   Closing(dt);   break;
                case State.Lunging:   Lunging(dt);   break;
                case State.Staggered: Staggered(dt); break;
                case State.Spent:     Spent(dt);     break;
            }

            Animate();
        }

        // --- states ----------------------------------------------------------

        void Dormant(float dt)
        {
            HoldStill();
            if (SeesHer()) Notice();
        }

        void Alerted(float dt)
        {
            HoldStill();
            _alertLeft -= dt;

            // Facing her for the whole of the alert, so it is clear the pause
            // is it looking and not a dropped frame.
            FaceHer();

            if (_alertLeft <= 0f) Enter(State.Closing);
        }

        void Closing(float dt)
        {
            if (!ChaseOrGiveUp(dt)) return;

            float d = DistanceToAri;

            // Creeping in when she is far, stopping at the standoff when she is
            // near. The standoff is why she gets to swing: inside this it will
            // not close further, so a brush stroke always has a window.
            if (d <= lungeRange)
            {
                BeginLunge();
                return;
            }

            float speed = d < 6f ? crawlSpeed : closeSpeed;
            Vector3 step = Towards(ari.transform.position, d, speed * dt);
            FaceHer();
            Face(step);
            SetSpeedParameter(speed);
            SetCrawling(true);
        }

        /// <summary>
        /// The lunge itself: travel out, then decide whether it connected.
        ///
        /// Split into "moving" and "arrived" rather than one shot at the start,
        /// because a lunge that resolves on its first frame is a hit the player
        /// had no opportunity to move out of. It takes half a second of
        /// telegraphed motion, and half a second is enough to step aside — which
        /// is the difference between a fight and an ambush.
        ///
        /// <b>The brush can interrupt this.</b> A crawler that could not be
        /// staggered mid-lunge would make the beat's only verb fail at the one
        /// moment it matters, and the player would conclude the brush is
        /// unreliable rather than mistimed. <see cref="staggerSeconds"/> is
        /// longer than <see cref="lungeSeconds"/> on purpose, so a clean
        /// interrupt wins decisively rather than by a frame.
        /// </summary>
        void Lunging(float dt)
        {
            _lungeLeft -= dt;

            float total = Mathf.Max(0.01f, lungeSeconds);
            float done = 1f - Mathf.Clamp01(_lungeLeft / total);

            // Two thirds travel, one third wind up. A single linear slide into
            // her reads as a shove; the pause before contact is what reads as a
            // strike.
            float travel = Mathf.Clamp01(done / 0.66f);
            Vector3 want = _lungeFrom + (_lungeTo - _lungeFrom) * travel;
            want.y = transform.position.y;

            Vector3 step = want - transform.position;
            float stepLen = step.magnitude;
            if (stepLen > 0.0001f)
            {
                // Cast, not write: the lunge must not put the crawler inside a
                // ruin wall it happened to aim at.
                float open = Travel(step.normalized, stepLen);
                if (open > 0f)
                {
                    transform.position += step.normalized * open;
                    Face(step);
                }
            }

            SetSpeedParameter(closeSpeed);

            if (_lungeLeft <= 0f) ResolveLunge();
        }

        /// <summary>
        /// Did it reach her? Only if she is still there.
        ///
        /// "Still there" is measured against where she was when it committed,
        /// not against where it ended up. Stepping sideways is meant to work,
        /// and a check on current distance would credit her for standing still
        /// and punish her for a dodge that landed her further away by accident.
        /// </summary>
        void ResolveLunge()
        {
            float reach = lungeReach;

            if (ari != null &&
                Vector3.Distance(ari.transform.position, _lungeTo) <= reach &&
                Vector3.Distance(ari.transform.position, transform.position) <= reach + bodyRadius + ari.BodyRadius)
            {
                // Away from the crawler, and only as far as there is room.
                Vector3 push = ari.transform.position - transform.position;
                push.y = 0f;
                if (push.sqrMagnitude < 0.0001f) push = transform.forward;
                push.Normalize();

                LastPushMetres = ari.TryPush(push * 0.9f);
            }

            Enter(State.Closing);
            _unseenSeconds = 0f;
        }

        void Staggered(float dt)
        {
            HoldStill();
            _staggerLeft -= dt;
            if (_staggerLeft <= 0f)
            {
                // Back to Closing rather than to Dormant. A crawler that
                // forgets her because she hit it hard enough teaches the
                // player that attacking is a way to end a fight, and the whole
                // point of this beat is that it is not.
                Enter(State.Closing);
                _unseenSeconds = 0f;
            }
        }

        void Spent(float dt)
        {
            // Walking home, and staying home.
            //
            // The re-acquire branch that used to sit at the bottom of this
            // method is gone: `Retired` latches before this state is entered
            // and `SeesHer` returns false from then on, so the branch could only
            // ever be dead. It is removed rather than left unreachable, because
            // an unreachable re-acquire reads as a live rule to anyone reading
            // this later and it is the opposite of what the beat does.
            SetCrawling(true);
            float d = Vector3.Distance(transform.position, home);
            if (d > 0.15f)
            {
                float speed = Mathf.Min(returnSpeed, d * 2.5f);
                Vector3 step = Towards(home, d, speed * dt);
                Face(step);
                SetSpeedParameter(speed);
            }
            else
            {
                HoldStill();
                Enter(State.Dormant);
            }
        }

        // --- behaviour --------------------------------------------------------

        // --- sight ------------------------------------------------------------

        /// <summary>Where its eye is, at the height it decides with.</summary>
        public Vector3 Eye =>
            transform.position + Vector3.up * (bodyHeight * eyeFraction);

        /// <summary>
        /// The point on Ari a sighting ends at.
        ///
        /// Her chest rather than her feet, and her *real* body height rather
        /// than a constant: she is the only thing in the yard whose height is
        /// not a designer number, and if this used a constant then crouching
        /// her behind the wall in any future sense would not move the spot the
        /// crawler aims at.
        /// </summary>
        public Vector3 AriChest =>
            ari == null
                ? transform.position
                : ari.transform.position + Vector3.up * (ari.BodyHeight * 0.6f);

        /// <summary>
        /// Is <paramref name="ariPoint"/> hidden from an eye at
        /// <paramref name="eye"/>? Pure geometry — no radius, no state, no
        /// latch. Answering it is the whole of "can it see her".
        ///
        /// <para>Public, and for a reason.</para>
        ///
        /// The build tool needs this exact question — "from its own post, would
        /// it have seen her at that spot?" — and the first version answered it
        /// with a second, hand-written raycast of its own. Two implementations
        /// of one mechanic is how a stealth route gets verified against
        /// geometry the game does not use: the tool reported the wall working
        /// while the component's own mask said the opposite, and the tool was
        /// right about the wall and wrong about the game.
        ///
        /// Now there is one implementation and the tool calls it, so a check
        /// that passes here is a statement about the mechanic rather than about
        /// a model of it.
        /// </summary>
        public bool BlockedFromPost(Vector3 eye, Vector3 ariPoint)
        {
            Vector3 to = ariPoint - eye;
            if (to.sqrMagnitude < 0.0001f) return false;   // close enough to see

            // Mask 0: everything. This was the bug worth writing down.
            //
            // The first version excluded Ari's layer — `~(1 << ari.layer)` —
            // with the reasoning that the ray ends on her chest, so without it
            // she would block herself and never be seen. Ari has no Collider
            // at all in this project; physics knows her as a cast in
            // AriMover, so there was nothing to exclude. And her layer is the
            // default one, which is also the layer the ground, the ruin walls,
            // every village house and every prefab instance are on.
            //
            // So that mask did not exclude Ari. It excluded the entire world,
            // and every sight line came back clear. The crawler would have seen
            // her through a two-metre wall from anywhere in the yard, which
            // means the stealth route did not exist and no editor-mode check
            // in the build tool would have caught it — the tool was measuring
            // a raycast it had written correctly and the game was not using it.
            //
            // Everything is cast against; Ari and this creature's own body are
            // then recognised by *identity* below, which is the only reliable
            // way to exclude an object — by naming it rather than by guessing
            // which layer it happens to share.
            //
            // `~0`, not `0`. A layer mask is a bitfield — bit N is layer N — so
            // `0` selects no layers at all and the raycast returns nothing,
            // every time, without complaint. `~0` is every layer.
            //
            // That single digit was two of this beat's failures at once and
            // neither of them said "layer mask". The build reported the
            // southern lane 0% hidden from the crawler's post with the first
            // sample exposed, and a wall two metres thick was standing right
            // there; and the crawler's own movement cast, in `Travel`, was
            // written the same way, so he too was walking through it. Both
            // numbers were confidently wrong and both said something entirely
            // specific and entirely false: one blamed the wall's position, the
            // other would have printed that the gap was not a gap.
            //
            // Every other raycast in this project says `~0` and none of them
            // has ever been surprised this way. The odd one out was mine.
            if (!Physics.Raycast(eye, to.normalized, out RaycastHit hit,
                                 to.magnitude, ~0, QueryTriggerInteraction.Ignore))
                return false;

            var c = hit.collider;
            if (c == null) return false;

            if (c.transform.IsChildOf(transform)) return false;   // its own body
            if (ari != null && c.transform.IsChildOf(ari.transform)) return false;

            return true;
        }

        /// <summary>
        /// Does it currently have her? Distance, then a ray from its eye.
        ///
        /// The ray matters as much as the distance. Without it, a ruin wall
        /// one metre tall stops a crawler moving but not a crawler looking, and
        /// the cover the stealth route is built on turns out to be a place she
        /// is still perfectly visible.
        /// </summary>
        bool SeesHer()
        {
            if (ari == null) return false;

            // The latch, first thing, before any distance is measured. Once it
            // has given her up this creature is scenery for the rest of the
            // beat — see Retired for why the geometry leaves no other option.
            if (Retired) return false;

            float d = DistanceToAri;
            float gate = _state == State.Dormant || _state == State.Spent
                ? noticeRadius
                : forgetRadius;
            if (d > gate) return false;

            return !BlockedFromPost(Eye, AriChest);
        }

        /// <summary>
        /// Keep hunting her, or give up if the sighting has lapsed.
        ///
        /// Returns false when the crawler has given up this frame, so the caller
        /// can stop before it walks another step in the wrong direction.
        /// </summary>
        bool ChaseOrGiveUp(float dt)
        {
            if (SeesHer())
            {
                _unseenSeconds = 0f;
                return true;
            }

            _unseenSeconds += dt;
            if (_unseenSeconds >= giveUpSeconds)
            {
                if (mono != null) mono.Recall();
                Retired = true;
                Enter(State.Spent);
                if (log) Debug.Log("[Echoes] crawler gave her up, and will not " +
                                   "pick her up again this beat", this);
                return false;
            }

            return true;
        }

        void Notice()
        {
            EverNoticed = true;
            _alertLeft = alertSeconds;
            _unseenSeconds = 0f;
            Enter(State.Alerted);
            if (log) Debug.Log("[Echoes] crawler noticed Ari at " +
                               DistanceToAri.ToString("0.0") + " m", this);
        }

        void BeginLunge()
        {
            _lungeLeft = lungeSeconds;
            _lungeFrom = transform.position;
            _lungeTo = ari.transform.position;

            // It commits to where she *was*. If the lunge tracked her it would
            // be an auto-hit that cannot be dodged by moving, and this beat is
            // about her choosing when to be somewhere else.
            Lunges++;
            Enter(State.Lunging);
            LastHitWasBrush = false;

            if (_anim != null) _anim.SetTrigger(_attackHash);
        }

        /// <summary>
        /// A brush stroke landed. Stagger it, and push it away from her.
        ///
        /// The one verb the player has against this thing.
        /// </summary>
        public bool Splash(Vector3 strokePoint, Vector3 fromAri)
        {
            if (!CanBeStaggered) return false;

            float d = Vector3.Distance(fromAri, strokePoint);
            if (d > splashRadius) return false;

            _staggerLeft = staggerSeconds;
            Staggers++;
            LastHitWasBrush = true;
            Enter(State.Staggered);

            Vector3 away = transform.position - fromAri;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
            Push(away.normalized * knockback);

            if (_anim != null) _anim.SetTrigger(_attackHash);

            if (log) Debug.Log("[Echoes] crawler staggered, stroke " +
                               d.ToString("0.00") + " m from Ari (reach " +
                               splashRadius.ToString("0.00") + " m)", this);
            return true;
        }

        // --- movement ---------------------------------------------------------

        /// <summary>
        /// Move along <paramref name="towards"/> by at most
        /// <paramref name="max"/>, stopping short of anything solid.
        ///
        /// The same cast-shaped movement Ari uses, for the same reason: she has
        /// no collider so hers is a sweep, and if this were a rigidbody or a
        /// transform write the crawler would walk through the ruin walls that
        /// the whole stealth route is built on. A thing that phases through
        /// cover is not blocked by cover.
        /// </summary>
        Vector3 Towards(Vector3 to, float distance, float max)
        {
            Vector3 flat = new Vector3(to.x - transform.position.x, 0f,
                                       to.z - transform.position.z);
            if (flat.sqrMagnitude < 0.000001f || distance <= 0f) return Vector3.zero;

            Vector3 dir = flat.normalized;
            float travel = Travel(dir, max);
            if (travel > 0f) transform.position += dir * travel;
            return dir * travel;
        }

        /// <summary>
        /// How far it can actually go along <paramref name="dir"/> out of the
        /// <paramref name="want"/> asked for. Zero if something is in the way.
        ///
        /// <para>The cast, and the reason it is shaped like Ari's.</para>
        ///
        /// Unity's <c>CapsuleCast</c> has no overload that returns the distance
        /// travelled — the one without a hit report returns bool. The first
        /// version of this called the shape it remembered, which does not
        /// exist, and the compiler said so twice on the same line: argument 2
        /// wanted a Vector3 and got a float, argument 3 wanted a float and got
        /// a Vector3. Three call sites, six errors, one mistake.
        ///
        /// So this uses the <c>out RaycastHit</c> overload and reads
        /// <c>hit.distance</c>. Stopping that far short is deliberately not the
        /// same as projecting the request the way Ari's own slide does: a
        /// crawler that grazes a corner should stop and try again next frame,
        /// not slide sideways, because sliding is a behaviour it has no way of
        /// committing to and would look like it had found a path.
        /// </summary>
        float Travel(Vector3 dir, float want)
        {
            if (want <= 0.0001f) return 0f;

            CapsuleAt(out Vector3 bottom, out Vector3 top);

            // `~0`, not `0`, and the reason is a layer mask being a bitfield:
            // `0` selects nothing, so this cast had never once found a wall.
            // The same digit in `BlockedFromPost` had the crawler's *eyes*
            // deaf as well as its feet, and both bugs looked like level design
            // problems rather than like a cast that could not see.
            //
            // What it would have looked like in play is worse than a walk
            // through a wall: the ruin is the whole of this beat's stealth
            // route, so the player would be told by the walls that she was
            // hidden, watch the creature walk through one to reach her, and be
            // right that the cover was not working while having no way to name
            // why. A thing that phases through the obstacle the puzzle is built
            // on is not an obstacle, and no amount of correct geometry fixes it.
            bool hitSomething = Physics.CapsuleCast(
                bottom, top, bodyRadius, dir, out RaycastHit hit, want,
                ~0, QueryTriggerInteraction.Ignore);

            if (!hitSomething) return want;

            // A capsule whose two spheres overlap casts nothing at all and
            // reports no hit, which is a legal inspector value — bodyHeight
            // below twice the radius — and would read here as "clear road".
            return Mathf.Clamp(want - hit.distance, 0f, want);
        }

        /// <summary>
        /// The two sphere centres of the body capsule.
        ///
        /// From the root as a *standing* body, not from the hips the way Ari's
        /// own capsule is built: this thing has no sole offset and its root sits
        /// on the floor, so its bottom sphere centre is one radius up and the
        /// top one is one radius below the crown.
        /// </summary>
        void CapsuleAt(out Vector3 bottom, out Vector3 top)
        {
            float floorY = transform.position.y;
            bottom = new Vector3(transform.position.x, floorY + bodyRadius,
                                 transform.position.z);
            top = new Vector3(transform.position.x,
                              floorY + bodyHeight - bodyRadius,
                              transform.position.z);
            if (top.y < bottom.y) top = bottom;
        }

        void Push(Vector3 delta) => Towards(transform.position + delta, delta.magnitude, delta.magnitude);

        void FaceHer()
        {
            if (ari == null) return;
            Vector3 to = ari.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        void Face(Vector3 moved)
        {
            if (moved.sqrMagnitude < 0.000001f) return;
            transform.rotation = Quaternion.LookRotation(moved.normalized, Vector3.up);
        }

        void HoldStill()
        {
            SetSpeedParameter(0f);
            SetCrawling(false);
        }

        // --- animation --------------------------------------------------------

        void Animate()
        {
            if (_anim == null) return;

            // The attack clip is standing in for the stagger, because the
            // controller was built with that reuse before this beat existed.
            // Playing it on every stagger is what the graph is for.
            float now = _anim.GetFloat(_speedHash);
            _anim.SetFloat(_speedHash, Mathf.Lerp(now, TargetSpeed(), Time.deltaTime * 6f));
        }

        float TargetSpeed()
        {
            switch (_state)
            {
                case State.Closing: return closeSpeed;
                case State.Lunging: return closeSpeed;
                case State.Spent:   return returnSpeed;
                default:            return 0f;
            }
        }

        void SetSpeedParameter(float v)
        {
            if (_anim == null) return;
            _anim.SetFloat(_speedHash, v);
        }

        void SetCrawling(bool on)
        {
            if (_anim == null) return;
            _anim.SetBool(_crawlHash, on);
        }

        void Enter(State next)
        {
            _state = next;
            if (next != State.Staggered) LastHitWasBrush = false;
        }

        /// <summary>Metres it came in by on the last lunge. Read by the beat
        /// to tell a real lunge from one that hit a wall.</summary>
        public float LastLungeTravel =>
            _lungeFrom.sqrMagnitude == 0f ? 0f
                : Vector3.Distance(_lungeFrom, _lungeTo);

        /// <summary>Back to the start. For a checkpoint reload.</summary>
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
            transform.position = home;
            Physics.SyncTransforms();
        }

        void OnDrawGizmosSelected()
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