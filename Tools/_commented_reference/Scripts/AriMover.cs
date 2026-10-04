using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{
    /// <summary>
    /// Walks Ari around, jumps her, and drives her Animator's Speed parameter.
    ///
    /// This exists because the controller and the clips were already correct
    /// and she still did not move: Speed sat at its default of 0, which pins the
    /// graph to the idle state. Assets being wired up is not the same as the
    /// thing moving, and only something writing to the parameter closes that
    /// gap.
    ///
    /// Movement is kinematic with a raycast for ground height rather than a
    /// CharacterController. Ari is 1.80 units tall and the village is made of
    /// boxes, so the thing worth getting right is that her soles stay on the
    /// paving as she crosses from a tile at 0.08 to a path at 0.02 — a rigidbody
    /// would solve that by shoving her through the kerb, and a capsule would
    /// need tuning against colliders the generator never created.
    ///
    /// Her sides are handled separately, by a swept capsule that answers only
    /// "is there a wall between here and there". It was added late and for one
    /// reason: she used to walk straight through houses, and the hop made that
    /// obvious rather than new. It is a sweep and not a CharacterController
    /// because a controller would also replace the ground handling, which is
    /// the part of this that is already correct and already measured.
    ///
    /// The jump is the one place gravity enters, and it is entered only by
    /// jumping. Walking off an edge still leaves her where she was, as it always
    /// has, because gravity here is not physics — a failed ground check means
    /// she is standing inside a wall with the ray origin buried in it about as
    /// often as it means she is over a drop, and treating that as a fall would
    /// put her under the paving.
    ///
    /// She faces the direction she is travelling, and turns at a measured rate
    /// rather than snapping, because a character that pivots instantly reads as
    /// a turret.
    /// </summary>
    [AddComponentMenu("Echoes/Ari Mover")]
    [RequireComponent(typeof(Animator))]
    public sealed class AriMover : MonoBehaviour
    {
        [Header("Movement")]
        [Tooltip("Metres per second on the ground.")]
        [Min(0f)] [SerializeField] float walkSpeed = 2.2f;

        [Tooltip("Metres per second while a sprint input is held. The walk clip " +
                 "plays at walkSpeed; anything faster reuses it, which is fine at " +
                 "this scale but is the first thing to replace with a run clip.")]
        [Min(0f)] [SerializeField] float runSpeed = 3.6f;

        [Tooltip("Degrees per second when turning to face the direction of travel.")]
        [Min(1f)] [SerializeField] float turnRate = 720f;

        [Header("Ground")]
        [Tooltip("How far below her feet to look for ground.")]
        [Min(0.1f)] [SerializeField] float groundProbe = 1.5f;

        [Tooltip("Layer mask the ground raycast hits. Empty means everything.")]
        [SerializeField] LayerMask groundMask = ~0;

        [Tooltip("How level a surface must be before she will stand on it, as " +
                 "the up-component of its normal. 1 is flat, 0.7 is about 45 " +
                 "degrees. This is what keeps her off roofs and out of wall " +
                 "caps once the whole village has colliders.")]
        [Range(0f, 1f)] [SerializeField] float minGroundSlope = 0.7f;

        [Header("Sole offset")]
        [Tooltip("Distance from this transform down to the soles. Measured from the " +
                 "skinned mesh at startup; the inspector value is only a fallback for " +
                 "when there is no renderer to measure.")]
        [SerializeField] float soleOffset = 0.1717f;

        [Tooltip("Metres per second she closes a height difference. The paving, the " +
                 "paths and the kerb line sit at three different heights so they do " +
                 "not z-fight, which means snapping straight to the surface pops " +
                 "her up a few centimetres at every tile seam. Easing it keeps her " +
                 "on the ground without the stair-stepping.")]
        [Min(0.1f)] [SerializeField] float groundFollow = 6f;

        [Header("Input")]
        [Tooltip("Run modifier. Held, it raises the target speed.")]
        [SerializeField] Key runKey = Key.LeftShift;

        [Tooltip("Also honour the on-screen pad. Turned on automatically when " +
                 "screen controls are added to the scene. The keyboard keeps " +
                 "working either way — this only adds a second way in.")]
        [SerializeField] bool useScreenControls;

        [Header("Jump")]
        [Tooltip("Jump. Only read while she is already on something solid, so " +
                 "holding it down does not make her bounce the moment she lands.")]
        [SerializeField] Key jumpKey = Key.Space;
        [Tooltip("Peak height of the hop above the surface, in metres. The launch " +
                 "speed is derived from this rather than set directly, so changing " +
                 "the height cannot desync it from the gravity below the way a " +
                 "hand-picked speed does.")]
        [Min(0.1f)] [SerializeField] float jumpHeight = 1.2f;

        [Tooltip("Downward acceleration, as a positive number. 18 is not Earth's " +
                 "9.8: this is a stylised arc, and a real-gravity hop at this " +
                 "height hangs at the top long enough to look like floating. " +
                 "Faster gravity keeps the hop reading as one motion.")]
        [Min(0.1f)] [SerializeField] float gravity = 18f;

        [Tooltip("How much of her ground speed she keeps while off the ground, " +
                 "0 to 1. Some, so a jump can be steered in the air; not all, so " +
                 "she cannot change direction instantly at the apex.")]
        [Range(0f, 1f)] [SerializeField] float airControl = 0.6f;

        [Tooltip("How far above the surface still counts as having landed, in " +
                 "metres. Without it, a frame at a low frame rate steps her past " +
                 "the paving and she falls through it.")]
        [Min(0f)] [SerializeField] float landTolerance = 0.25f;

        [Header("Walls")]
        [Tooltip("Whether she is stopped by the sides of things. On by default " +
                 "because the narrow-alley beat and every building interior are " +
                 "unbuildable without it: she used to walk through houses, and " +
                 "once the hop landed she jumped through them too.")]
        [SerializeField] bool collideWithWalls = true;

        [Tooltip("Radius of the body she pushes around with, in metres. 0.30 is " +
                 "deliberately wider than she looks. A tight capsule reads as " +
                 "more accurate and is not: it catches on the corner of every " +
                 "doorframe in a village made of boxes, and the result is a " +
                 "player who appears to be clipping into geometry rather than " +
                 "one who is brushing past it.")]
        [Min(0.05f)] [SerializeField] float bodyRadius = 0.30f;

        [Tooltip("Total height of the body, soles to crown. Matches the 1.80 " +
                 "measured on her mesh; a separate number from soleOffset " +
                 "because that one is read off the skin at runtime.")]
        [Min(0.5f)] [SerializeField] float bodyHeight = 1.80f;

        [Tooltip("How far above her soles the sides of the body start. The " +
                 "paving she stands on is level with the soles, and a capsule " +
                 "whose bottom sphere is tangent to a surface is a coin toss: " +
                 "it either reports a hit on the floor or misses a wall, " +
                 "depending on which way the float rounds. Lifting it clear of " +
                 "the ground costs a collision with anything shorter than this " +
                 "and makes the floor case impossible.")]
        [Min(0f)] [SerializeField] float bodyBottomLift = 0.10f;

        [Tooltip("How high a kerb or a single step she walks up without " +
                 "jumping. The village's paths and paving sit at three " +
                 "different heights, so without this she is stopped by every " +
                 "seam between them.")]
        [Min(0f)] [SerializeField] float stepHeight = 0.35f;

        [Tooltip("Layer mask the sideways sweep hits. Triggers are always " +
                 "ignored, so paint targets, pickups and checkpoints do not " +
                 "need to be kept off this list to be walkable.")]
        [SerializeField] LayerMask wallMask = ~0;

        [Header("Animator")]
        [Tooltip("Float the controller crossfades on. Must match the parameter " +
                 "name in Ari.controller.")]
        [SerializeField] string speedParameter = "Speed";

        [Tooltip("Trigger the controller jumps on. Must match Ari.controller.")]
        [SerializeField] string jumpParameter = "Jump";

        [Tooltip("Bool the controller ends the jump on. Must match Ari.controller.")]
        [SerializeField] string groundedParameter = "Grounded";

        [Tooltip("Trigger the controller swings the brush on. Must match " +
                 "Ari.controller.")]
        [SerializeField] string swingParameter = "Swing";

        [Tooltip("Bool the controller holds for the length of the swing. The " +
                 "graph cannot end the stroke on Speed, because Speed already " +
                 "says 'standing' before the stroke begins and the swing would " +
                 "last one frame.")]
        [SerializeField] string swingingParameter = "Swinging";

        /// <summary>How far she is from the surface under her, for reporting.</summary>
        public float GroundGap { get; private set; }

        /// <summary>Her current speed in metres per second.</summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>
        /// Whether she is standing on something. True on the first frame, because
        /// a character that starts a level falling spends it visibly sinking
        /// through the paving before the first raycast catches.
        /// </summary>
        public bool IsGrounded { get; private set; } = true;

        /// <summary>
        /// The height she actually reached on her last jump, in metres, measured
        /// from the surface she left. This is the number that says whether the
        /// arc clears the kerbs, and it is only knowable after the fact.
        /// </summary>
        public float LastJumpHeight { get; private set; }

        /// <summary>Whether she is off the ground right now.</summary>
        public bool IsAirborne => !IsGrounded;

        /// <summary>
        /// Whether a wall is within reach of her sides right now, and the name
        /// of what it is. Set every frame by the sweep, and public because the
        /// only honest way to know collision works is to measure it: a
        /// character that walks through a house and a character that is stopped
        /// by one look identical from the outside.
        /// </summary>
        public bool TouchingWall { get; private set; }

        public string WallName { get; private set; } = "none";

        /// <summary>
        /// The size of the body the physics actually knows about, in world
        /// metres: total height soles to crown, and the radius of its sweep.
        ///
        /// Read-only and exposed because these two numbers, and not her visible
        /// mesh, are what decide whether she can get into a space. A level tool
        /// measuring the model finds a figure around 2.2 m and builds a doorway
        /// to it; she is stopped by the 1.80 m capsule in here, and the doorway
        /// she can never use is the one the player watched her walk past.
        ///
        /// So: a space sized to this is a space she can use, whatever the mesh
        /// says, and a space sized to the mesh is a space that looks like it
        /// should work and does not.
        /// </summary>
        public float BodyHeight => bodyHeight;

        public float BodyRadius => bodyRadius;

        /// <summary>
        /// The highest ledge she walks up without jumping.
        ///
        /// Exposed for the same reason as the body: anything built to stop her
        /// has to clear it. A barrier she can step onto is a barrier she will
        /// step onto, quietly, in front of the player.
        /// </summary>
        public float StepHeight => stepHeight;

        /// <summary>
        /// How far she actually moved sideways last frame, against how far she
        /// asked to move. Equal means nothing was in the way; less means
        /// something was.
        /// </summary>
        public float LastSweepRatio { get; private set; } = 1f;

        /// <summary>
        /// Whether the on-screen buttons, rather than the keyboard, are driving.
        /// Set by <see cref="OnScreenControls"/> when it is added to the scene.
        /// </summary>
        public bool UseScreenControls
        {
            get => useScreenControls;
            set => useScreenControls = value;
        }

        /// <summary>
        /// Where the on-screen controls want to go: x right, y up, magnitude 0-1.
        ///
        /// A static value rather than a per-instance one because a UI has no
        /// useful reference to the character it is steering — the canvas is a
        /// sibling of the player at best, and re-finding it every frame to write
        /// a direction is the kind of coupling that breaks the moment a second
        /// character exists. The controls write here; Ari reads here.
        /// </summary>
        public static Vector2 ScreenMove { get; set; }

        /// <summary>Whether the on-screen run button is held.</summary>
        public static bool ScreenRun { get; set; }

        Animator _animator;
        int _speedHash;
        int _jumpHash;
        int _groundedHash;
        int _swingHash;
        int _swingingHash;
        Vector3 _groundNormal = Vector3.up;

        /// <summary>
        /// Whether she is mid brush-swing, for a beat that has to wait her out.
        ///
        /// A timer rather than a flag cleared on the way back to locomotion,
        /// because nothing in the movement code watches the Animator's state and
        /// a flag would only ever be cleared by the swing interrupting the jump
        /// it is competing with.
        /// </summary>
        public bool IsSwinging => _swingTimer > 0f;

        /// <summary>
        /// Seconds of swing the trigger buys, for tuning the cooldown.
        ///
        /// 0.31 is the clip's own arithmetic, not a feel-good number: the swing
        /// state's motion is a 0.70s window — frames 16 to 36 of a 2.40s clip —
        /// played at 4.0x, and 0.70 / 4.0 is 0.18s of window on top of the
        /// 0.13s of wind-up before it, which is 0.31s from the click.
        ///
        /// It has to agree with both the clip and the speed, because the flag
        /// this timer drives is what the controller's return transitions wait
        /// on. Too short and the graph blends out while the arm is still
        /// travelling and the stroke looks cut; too long and the arm holds a
        /// pose the game has already moved past.
        ///
        /// It is also safely under the 0.60s brush cooldown, so mashing the
        /// button cannot restart the stroke before the last one has finished.
        ///
        /// Changing either the clip or SwingSpeed in AriControllerBuilder means
        /// this number has to be re-derived, not nudged. SwingClipProbe prints
        /// both halves.
        /// </summary>
        [Min(0.05f)] [SerializeField] float swingSeconds = 0.31f;

        float _swingTimer;

        /// <summary>Upward speed, positive while rising, negative while falling.</summary>
        float _verticalVelocity;

        /// <summary>
        /// The highest root height reached on the current or last hop. Tracked
        /// rather than derived, because the height of a jump is not the distance
        /// between where she took off and where she came down — those are the
        /// same point by definition, and measuring between them reports every
        /// jump as exactly zero. That is not a hypothetical: the first version
        /// of this did exactly that and reported 0.000 m for a hop that reached
        /// 1.145 m.
        /// </summary>
        float _jumpPeakY;

        /// <summary>
        /// Put her somewhere else, standing still.
        ///
        /// For the test warp and for a checkpoint restore. Not a movement verb
        /// — `Step` stays the only way she moves under her own power, because
        /// a second path into her position is a second set of things that have
        /// to stay in sync with the first.
        ///
        /// The vertical velocity is cleared because it is the one piece of
        /// momentum she carries. Leave it and a warp that happens to land during
        /// a jump drops her from wherever the arc was going rather than from
        /// where she was put: she is placed at the right height and then keeps
        /// rising, or lands with the speed she had and sticks. Either way the
        /// player is somewhere they did not ask to be, and the fault is in the
        /// warp rather than in the level — which is the worst place for it.
        ///
        /// `SyncTransforms` afterwards because her next `Step` casts from
        /// `transform.position`, and a transform written this frame is not in
        /// the physics world until something asks for it.
        /// </summary>
        public void Teleport(Vector3 where)
        {
            _verticalVelocity = 0f;
            _swingTimer = 0f;
            _jumpPeakY = where.y;

            transform.position = where;
            Physics.SyncTransforms();
        }

        /// <summary>
        /// Shove her, but only as far as there is room.
        ///
        /// For Beat 5's crawler lunge. Not a teleport with extra steps: the
        /// distance is walked out against her own capsule cast and stops at
        /// the first thing in the way, so being hit against a wall slides her
        /// along it rather than into it.
        ///
        /// Returns how far she actually went, because a lunge that reports a
        /// full push when it moved her two centimetres is a beat that cannot
        /// tell whether its own mechanic fired.
        ///
        /// Vertical velocity is cleared, as in Teleport. A shove while she is
        /// rising would otherwise be a shove *and* a jump, and the player
        /// learns to hold Space against an attack, which is a mechanic this
        /// beat has no business teaching.
        /// </summary>
        public float TryPush(Vector3 delta)
        {
            if (delta.sqrMagnitude < 0.000001f) return 0f;

            Vector3 flat = new Vector3(delta.x, 0f, delta.z);
            float wanted = flat.magnitude;
            if (wanted < 0.0001f) return 0f;

            Vector3 dir = flat / wanted;
            Vector3 foot = transform.position;

            // Her own Sweep, not a cast written out again here. This is the
            // point of the method: the space a shove is allowed to move her
            // into is then *by construction* the space she is allowed to walk
            // into, because it is the same function. A second copy of the cast
            // would agree today and drift the first time either of them changed,
            // and the day it drifted she would be shoved through walls while the
            // collision test beside it reported her as solid.
            //
            // The first version of this hand-rolled a CapsuleCast and got the
            // argument order wrong: Unity's cast is (bottom, top, radius, ...),
            // and passing (centre, radius, offset, ...) produced two CS1503s on
            // the same line, in this file and twice more in InkCrawler.
            Vector3 made = Sweep(foot, dir * wanted);

            float travel = made.magnitude;
            if (travel > 0f) transform.position = foot + made;

            _verticalVelocity = 0f;
            Physics.SyncTransforms();

            return travel;
        }

        void Awake()
        {
            Bind();

            // The Animator is authored switched off in the scene, because a saved
            // scene with a live controller and no movement script animates a
            // character nobody is steering. This component is that script, so it
            // takes over here — in Awake rather than Update so the very first
            // frame is already being animated.
            if (!_animator.enabled) _animator.enabled = true;

            MeasureSoleOffset();
        }

        /// <summary>
        /// Resolve the Animator and the parameter hashes.
        ///
        /// Done on demand rather than only in Awake because Step is public and
        /// documented as safe to call directly, and Awake does not run for a
        /// component that is not playing. An editor probe stepping real frames
        /// therefore hit a null Animator on the very first line of the movement
        /// and threw. The same call arrives at runtime from anything that
        /// touches a character before it has been enabled, and a null-reference
        /// on the first frame of a level is not a recoverable bug.
        ///
        /// Cheap on the path that matters: in play mode Awake has already bound
        /// it, so this is one null check per frame.
        /// </summary>
        void Bind()
        {
            if (_animator == null) _animator = GetComponent<Animator>();
            if (_animator == null) return;

            _speedHash = Animator.StringToHash(speedParameter);
            _jumpHash = Animator.StringToHash(jumpParameter);
            _groundedHash = Animator.StringToHash(groundedParameter);
            _swingHash = Animator.StringToHash(swingParameter);
            _swingingHash = Animator.StringToHash(swingingParameter);
        }

        void OnDisable()
        {
            if (_animator == null) return;

            // An unconsumed trigger is not a pending action, it is a stray one.
            // If she is disabled with the trigger set and then re-enabled, the
            // graph fires the jump or the swing the instant it is switched on —
            // she hops, or paints, without anything having asked her to.
            _animator.ResetTrigger(_jumpHash);
            _animator.ResetTrigger(_swingHash);
            _animator.SetBool(_swingingHash, false);
        }

        /// <summary>
        /// Swing the brush.
        ///
        /// The arm has to move, because a click that repaints the world while the
        /// character stands perfectly still does not read as a brush stroke — it
        /// reads as the mouse doing something. This is the whole of the swing on
        /// the movement side: the animation is the controller's job, and this
        /// only fires the trigger.
        ///
        /// Public and separate from the click handler because the swing is
        /// Ari's, not the brush's: Beat 3's tree has to be able to make her
        /// swing on a beat's timing, and a real controller calls this rather
        /// than the brush.
        /// </summary>
        public bool PlaySwing()
        {
            Bind();
            if (_animator == null) return false;

            _animator.SetTrigger(_swingHash);

            // The flag goes up in the same call as the trigger, not a frame
            // later. A frame of false here is enough for the graph to read the
            // return condition as satisfied and leave the swing before the arm
            // has moved at all.
            _animator.SetBool(_swingingHash, true);

            _swingTimer = swingSeconds;
            return true;
        }

        /// <summary>
        /// Find how far the soles sit below this transform, and snap her onto the
        /// surface using it.
        ///
        /// Mixamo rigs put the origin at the hips, not the ground, so the lowest
        /// real vertex is 0.17 below the root. Setting the root to the ground
        /// height therefore buries her feet, which is exactly what happened on
        /// the first run: she stood at y=0 with her soles at -0.17, under the
        /// paving. The renderer's own bounds cannot give this number — they are
        /// inflated well past the silhouette — so the skin is baked and the
        /// lowest vertex is read.
        /// </summary>
        void MeasureSoleOffset()
        {
            var skin = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null || skin.sharedMesh == null) return;

            // Pose first: the bind pose has the arms out and stands a little
            // differently from the idle she will actually be measured in.
            _animator.Update(0f);

            var baked = new Mesh();
            skin.BakeMesh(baked);

            var verts = baked.vertices;
            if (verts.Length == 0) { Destroy(baked); return; }

            var m = skin.transform.localToWorldMatrix;
            float lowest = float.MaxValue;
            for (int i = 0; i < verts.Length; i++)
            {
                float y = m.MultiplyPoint3x4(verts[i]).y;
                if (y < lowest) lowest = y;
            }

            Destroy(baked);

            soleOffset = transform.position.y - lowest;
        }

        void Update()
        {
            // dt rather than a fixed step, so she keeps pace with the frame rate
            // instead of walking in slow motion when it drops.
            //
            // Both sources are live at once and the keyboard wins when it is
            // being used. An either/or switch was worse than useless: turning
            // the pad on silently killed WASD, so the controls appeared to
            // "take over" the character rather than join it.
            Vector3 keys = KeyboardInput();
            Vector3 pad = new Vector3(ScreenMove.x, 0f, ScreenMove.y);

            bool padActive = useScreenControls && pad.sqrMagnitude > 0.0001f;
            Vector3 wish = keys.sqrMagnitude > 0.0001f ? keys : (padActive ? pad : Vector3.zero);

            // Run is the union rather than a choice, so holding Shift still
            // works while a pad button is down and vice versa.
            bool run = KeyHeld(runKey) || (useScreenControls && ScreenRun);

            // Read as a press, not as a level. isPressed would keep the request
            // alive for as long as the key is down, and Step accepts a jump
            // whenever it is on the ground — so holding Space would bounce her
            // the instant each hop finished. wasPressedThisFrame is one frame
            // wide, which is what "the player wants to jump" actually means.
            bool jump = JumpPressed();

            Step(wish, run, Time.deltaTime, jump);
        }

        /// <summary>
        /// Whether the jump key went down on this frame.
        ///
        /// Split out so it can be tested, and because a probe driving Step
        /// directly has no keyboard at all: Keyboard.current is null under
        /// automation, and a jump gated on a key press is a jump that provably
        /// never fires in a headless run.
        /// </summary>
        bool JumpPressed()
        {
            var kb = Keyboard.current;
            return kb != null && kb[jumpKey].wasPressedThisFrame;
        }

        /// <summary>
        /// WASD and the arrow cluster both work, because one of them is always
        /// the wrong choice for the person at the keyboard.
        /// </summary>
        static Vector3 KeyboardInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector3.zero;

            float x = 0f, z = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) z += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) z -= 1f;

            var v = new Vector3(x, 0f, z);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        static bool KeyHeld(Key key)
        {
            var kb = Keyboard.current;
            return kb != null && kb[key].isPressed;
        }

        /// <summary>
        /// Move her for one frame and publish the result to the Animator.
        ///
        /// Split out of Update and public so the movement can be exercised
        /// without a keyboard attached. That matters: under automation
        /// Keyboard.current is null, ReadInput returns zero, and a component
        /// that only moves on real key presses reports identically to a broken
        /// one — both leave her standing still. Calling Step directly measures
        /// the same code path the keyboard feeds, so "she walks" is a result
        /// rather than an assumption.
        ///
        /// <paramref name="wish"/> is a raw stick position: x for left/right,
        /// z for forward/back, in camera space.
        /// </summary>
        public void Step(Vector3 wish, bool running, float dt, bool jump = false)
        {
            // See Bind: Step is callable without Awake having run, so the
            // Animator cannot be assumed to be there.
            Bind();
            if (_animator == null) return;

            // The flag falls when the timer runs out, which is what the controller
            // is waiting for. Set from here rather than inferred from the
            // Animator's current state, because nothing else in this component
            // watches the graph and a flag cleared anywhere else would be a flag
            // that silently never clears.
            if (_swingTimer > 0f)
            {
                _swingTimer -= dt;

                if (_swingTimer <= 0f)
                {
                    _swingTimer = 0f;
                    _animator.SetBool(_swingingHash, false);
                }
            }

            Vector3 planar = new Vector3(wish.x, 0f, wish.z);

            // Movement is relative to the camera, so "forward" is away from the
            // viewer rather than whatever way the world happens to point.
            var cam = Camera.main;
            if (cam != null && planar.sqrMagnitude > 0.0001f)
            {
                var f = cam.transform.forward;
                var r = cam.transform.right;
                f.y = 0f; r.y = 0f;
                if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
                if (r.sqrMagnitude < 1e-6f) r = Vector3.right;
                f.Normalize(); r.Normalize();
                planar = f * planar.z + r * planar.x;
                if (planar.sqrMagnitude > 0.0001f) planar.Normalize();
            }

            float target = planar.sqrMagnitude > 0.0001f
                ? (running ? runSpeed : walkSpeed)
                : 0f;

            // The speed the animation should show, which is the full ground speed
            // even in the air. Deliberately not the reduced figure used for the
            // move below: the walk clip is authored for walkSpeed, so reporting
            // a fraction of it would make her stride out of a landing as though
            // she had walked into a wall.
            CurrentSpeed = target;

            Vector3 velocity = planar * target;
            var t = transform;

            // The horizontal move is resolved against walls before anything
            // else looks at it, so the ground probe and the landing check both
            // run at the position she will actually be at rather than the one
            // she was aiming for. Getting that order wrong drops her onto the
            // ground beside a wall she is standing against.
            Vector3 move = velocity * dt;
            Vector3 accepted = Sweep(t.position, move);
            LastSweepRatio = move.sqrMagnitude < 1e-8f
                ? 1f
                : Mathf.Clamp01(accepted.magnitude / move.magnitude);

            Vector3 next = t.position + accepted;

            // --- the hop ----------------------------------------------------------
            // Accepted only from the ground, so a second press mid-air is ignored
            // rather than turning into an unintended double jump. The launch speed
            // is solved from the requested height and the gravity below instead of
            // being a field of its own: with the two independent, a change to one
            // silently breaks the peak height of the other and the arc stops
            // matching the animation.
            if (IsGrounded && jump)
            {
                _verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                _jumpPeakY = t.position.y;
                IsGrounded = false;
                _animator.SetTrigger(_jumpHash);
            }

            if (IsGrounded)
            {
                // Stay on the surface: find the ground under the new position and
                // put the soles on it. The raycast reports the surface height and
                // the soles hang soleOffset below the root, so the root sits that
                // much above it — getting this wrong buries her feet, which is
                // what happened on the first run.
                //
                // The height is eased towards rather than assigned. Paving, paths
                // and kerbs are staggered across three heights to avoid
                // z-fighting, so a hard assignment pops her a few centimetres at
                // every seam, every couple of metres. Climb speed is bounded
                // instead.
                if (TryGround(next, out float groundY, out _groundNormal))
                {
                    float want = groundY + soleOffset;
                    next.y = Mathf.MoveTowards(t.position.y, want, groundFollow * dt);
                }

                // No fall is started when the ground check fails. Gravity here
                // is a field, not physics, so a missed check as often means she
                // is standing inside something with the ray origin buried in it
                // as it means she is over a drop, and dropping her from there
                // puts her under the paving. Falling is entered by jumping, where
                // the intent is unambiguous.
            }
            else
            {
                // In the air. Air control scales the move, not the reported speed.
                // The sweep is not skipped while airborne: the hop in Beat 2
                // crosses a gap, and a hop that carries her through the side of
                // a building is worse than one that does not clear it.
                Vector3 airMove = (velocity * airControl) * dt;
                next = t.position + Sweep(t.position, airMove);

                _verticalVelocity -= gravity * dt;
                next.y += _verticalVelocity * dt;
                if (next.y > _jumpPeakY) _jumpPeakY = next.y;

                // Landing, and only while descending. While rising, the ground
                // probe still reaches the paving she just left — it looks a metre
                // and a half below her — and honouring that would clamp her back
                // down mid-hop and cancel the jump on its second frame.
                //
                // The tolerance covers one frame stepping past the surface. At
                // the speed she lands, 0.25m is about two frames of travel, so a
                // dropped frame cannot put her through the paving.
                if (_verticalVelocity <= 0f &&
                    TryGround(next, out float landY, out _groundNormal) &&
                    next.y <= landY + soleOffset + landTolerance)
                {
                    next.y = landY + soleOffset;
                    _verticalVelocity = 0f;
                    IsGrounded = true;
                    // Measured from the surface she came down on, not from the
                    // surface she left: jumping up a kerb and coming down on it
                    // is a hop, and measuring from the lower paving would call it
                    // a two-metre jump.
                    LastJumpHeight = _jumpPeakY - (landY + soleOffset);
                }
            }

            t.position = next;

            if (planar.sqrMagnitude > 0.0001f)
            {
                var face = Quaternion.LookRotation(planar, Vector3.up);
                // RotateTowards takes its limit in radians; turnRate is in
                // degrees per second, so it has to be converted, not passed
                // through as-is.
                float maxStep = turnRate * dt * Mathf.Deg2Rad;
                t.rotation = Quaternion.RotateTowards(t.rotation, face, maxStep);
            }

            // The one line the whole component exists for, plus the two the
            // controller needs in order to leave the jump. Grounded is published
            // every frame rather than only when it changes, because the graph is
            // what decides whether she is standing or airborne, and a value
            // written only on the frames it changes leaves the transition to be
            // taken by luck.
            _animator.SetFloat(_speedHash, CurrentSpeed);
            _animator.SetBool(_groundedHash, IsGrounded);
        }

        // --- walls ------------------------------------------------------------
        //
        // A capsule sweep, not a CharacterController and not a rigidbody. Both
        // of those were the wrong shape of answer: a rigidbody solves the
        // three paving heights by shoving her through the kerb, and a
        // CharacterController brings its own gravity and its own ground
        // detection, which is the part of this component that is already
        // correct and already measured. A sweep is the smallest thing that
        // answers the only question being asked, which is whether the sides of
        // her body are against something solid.
        //
        // It is a separate code path from the ground ray on purpose. The two
        // answer different questions -- "is there floor" and "is there a wall"
        // -- and a single combined shape test for both is how a character ends
        // up unable to stand on a slope she is also sliding down.

        /// <summary>
        /// The two sphere centres of the body capsule, for a given root position.
        ///
        /// Expressed from soleOffset rather than from the root, because the root
        /// is at the hips: a capsule placed at the root and sized to 1.80 buries
        /// its lower half in her legs and lets her walk out through anything
        /// below knee height.
        /// </summary>
        void CapsuleAt(Vector3 at, out Vector3 bottom, out Vector3 top)
        {
            float soles = at.y - soleOffset;
            bottom = new Vector3(at.x, soles + bodyBottomLift + bodyRadius, at.z);
            top = new Vector3(at.x, soles + bodyHeight - bodyRadius, at.z);

            // A capsule whose spheres overlap is invalid and Unity's cast
            // returns nothing at all for it, silently disabling collision. That
            // happens if bodyHeight is ever set below twice the radius, which is
            // a legal value in the inspector.
            if (top.y < bottom.y) top = bottom;
        }

        /// <summary>
        /// Resolve a horizontal move into the part of it she can actually make.
        /// Returns a horizontal vector, never a full one — the caller owns the
        /// vertical, and mixing the two here would let a wall cancel gravity.
        /// </summary>
        Vector3 Sweep(Vector3 from, Vector3 delta)
        {
            TouchingWall = false;
            WallName = "none";

            delta.y = 0f;
            if (!collideWithWalls || delta.sqrMagnitude < 1e-8f) return delta;

            // Fast path. Most frames nothing is in the way, and this is the
            // only way to find that out without a cast that reports a miss.
            if (!Blocked(from, delta, out _)) return delta;

            // A kerb, a single step, a threshold. Tried before sliding, because
            // sliding along the face of a step is how a character ends up
            // pressed against a ten-centimetre rise forever, able to move
            // sideways but not forward.
            if (TryStepUp(from, delta, out Vector3 stepped))
            {
                TouchingWall = true;
                WallName = "stepped";
                return stepped;
            }

            Vector3 remaining = Slide(from, delta);
            return remaining;
        }

        /// <summary>
        /// Slide along whatever is in the way, rather than stopping dead.
        ///
        /// Three passes, because one is not enough at a corner and stopping at
        /// one is how a player gets pinned. Each pass removes the component of
        /// the remaining motion that goes into the surface and retries with
        /// what is left, which is what turns "walk into a wall at 45 degrees"
        /// into "walk around it".
        /// </summary>
        Vector3 Slide(Vector3 from, Vector3 delta)
        {
            Vector3 remaining = delta;

            for (int i = 0; i < 3; i++)
            {
                if (remaining.sqrMagnitude < 1e-8f) break;

                if (!Blocked(from, remaining, out RaycastHit hit)) break;

                TouchingWall = true;
                if (WallName == "none") WallName = hit.collider.name;

                // ProjectOnPlane rather than subtracting the normal, because the
                // normal is not normalised to the motion: subtracting it takes
                // the whole vector away and she stops dead at every corner.
                remaining = Vector3.ProjectOnPlane(remaining, hit.normal);
            }

            return remaining;
        }

        /// <summary>
        /// Try the same move from a step's height up, and take it if there is
        /// somewhere to land.
        ///
        /// The landing check is the part that matters. Without it she steps up
        /// onto thin air every time a wall happens to be stepHeight tall, and
        /// then walks off the top of it — which looks like a bug in the level
        /// rather than in the movement.
        /// </summary>
        bool TryStepUp(Vector3 from, Vector3 delta, out Vector3 accepted)
        {
            accepted = Vector3.zero;
            if (stepHeight <= 0f) return false;

            Vector3 raised = from + Vector3.up * stepHeight;
            if (Blocked(raised, delta, out _)) return false;

            // Something to stand on at the top, and not so far down that this
            // has become a jump.
            if (!TryGround(raised + delta, out float topY, out _)) return false;
            float lift = topY - (from.y - soleOffset);
            if (lift <= 0.001f || lift > stepHeight) return false;

            accepted = delta;
            return true;
        }

        /// <summary>
        /// Whether the body capsule would hit something travelling a horizontal
        /// delta. The hit is reported through an out-parameter rather than
        /// returned so the call sites read as questions — is it blocked, and by
        /// what — instead of unpacking a struct at every use.
        ///
        /// Eight arguments, not nine: Unity's CapsuleCast has no overload that
        /// also reports the distance travelled, so the hit point is what the
        /// slide works from, and the accepted move is reconstructed by
        /// projecting the request rather than by stopping short of the surface.
        /// That reconstruction is why the sweep can never leave her embedded:
        /// a move that grazed a corner comes back as a diagonal one, not as a
        /// shorter version of the same line.
        /// </summary>
        bool Blocked(Vector3 from, Vector3 delta, out RaycastHit hit)
        {
            CapsuleAt(from, out Vector3 bottom, out Vector3 top);

            return Physics.CapsuleCast(bottom, top, bodyRadius, delta.normalized,
                                       out hit, delta.magnitude, wallMask,
                                       QueryTriggerInteraction.Ignore);
        }

        bool TryGround(Vector3 at, out float y, out Vector3 normal)
        {            var origin = at + Vector3.up * groundProbe;

            // Loop rather than a single raycast, because the first thing hit
            // looking down is often a roof eave or a wall cap — surfaces she must
            // not stand on. A single ray would take that as the floor and drop
            // her on top of a house.
            const int attempts = 4;

            for (int i = 0; i < attempts; i++)
            {
                if (!Physics.Raycast(origin, Vector3.down, out var hit,
                                     groundProbe * 2f, groundMask,
                                     QueryTriggerInteraction.Ignore))
                    break;

                // Too steep to walk on. Rejected by slope rather than by layer,
                // because a layer would mean also hand-maintaining a mask that
                // has to exclude walls, roofs, chimneys and props separately —
                // and the moment a new piece of kit geometry arrives it is on the
                // wrong side of that mask by default.
                if (hit.normal.y >= minGroundSlope)
                {
                    y = hit.point.y;
                    normal = hit.normal;
                    GroundGap = 0f;
                    return true;
                }
            }

            y = at.y;
            normal = Vector3.up;
            GroundGap = float.NaN;
            return false;
        }
    }
}
