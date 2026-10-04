using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{
    /// <summary>
    /// Third-person camera that trails Ari and can be orbited by hand.
    ///
    /// Right mouse drags, wheel zooms. The right button specifically, because the
    /// left button is Ari's paint brush — a camera that orbits on left-drag makes
    /// it impossible to paint and look at the same time.
    ///
    /// The camera eases toward its target position rather than snapping. Snapping
    /// is not a stylistic choice here: the ground under Ari sits at three
    /// different heights (paving, paths, kerb line), so a rigid follow copies
    /// every tile seam into the camera as a visible stutter.
    ///
    /// When Ari is walking and the mouse has been still for a moment, the yaw
    /// eases around behind her. Without that, "follow" means a camera that only
    /// ever slides sideways, and you spend the whole game looking at her back as
    /// she walks away from you. It stops the instant the mouse moves, so it can
    /// never fight a deliberate orbit.
    /// </summary>
    [AddComponentMenu("Echoes/Ari Follow Camera")]
    [RequireComponent(typeof(Camera))]
    public sealed class AriFollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Who to follow. Falls back to the object named Ari.")]
        [SerializeField] Transform target;

        [Tooltip("Height above her feet to orbit around. About chest height, so " +
                 "she sits low in the frame and most of the shot is the world.")]
        [SerializeField] float pivotHeight = 1.35f;

        [Header("Distance")]
        [Tooltip("Closest the camera may come in.")]
        [Min(0.5f)] [SerializeField] float minDistance = 2.5f;

        [Tooltip("Furthest the camera may pull out.")]
        [Min(1f)] [SerializeField] float maxDistance = 14f;

        [SerializeField] float startDistance = 5.5f;

        [Tooltip("Metres per zoom notch.")]
        [Min(0.01f)] [SerializeField] float zoomStep = 0.6f;

        [Header("Angles")]
        [Tooltip("Lowest the camera may drop. Kept just above horizontal so the " +
                 "camera never ends up under the paving looking up.")]
        [Range(-30f, 10f)] [SerializeField] float minPitch = -8f;

        [Tooltip("Highest the camera may rise.")]
        [Range(10f, 89f)] [SerializeField] float maxPitch = 72f;

        [SerializeField] float startPitch = 14f;

        [Header("Obstruction")]
        [Tooltip("What may pull the camera in. Needs to include the village " +
                 "walls and roofs, which have no colliders until Tools/Echoes/" +
                 "Add Village Colliders has been run — without that this does " +
                 "nothing at all.")]
        [SerializeField] LayerMask obstructionMask = ~0;

        [Header("Feel")]
        [Tooltip("Seconds for the camera to close most of the way to where it " +
                 "wants to be. Lower is stiffer.")]
        [Min(0.01f)] [SerializeField] float positionSmooth = 0.14f;

        [Tooltip("Seconds for the look direction to settle.")]
        [Min(0.01f)] [SerializeField] float rotationSmooth = 0.08f;

        [Tooltip("Metres per second below which Ari counts as standing still.")]
        [Min(0f)] [SerializeField] float autoAlignSpeed = 0.4f;

        [Tooltip("Seconds of mouse stillness before the camera starts following " +
                 "her heading again.")]
        [Min(0f)] [SerializeField] float autoAlignDelay = 1.1f;

        [Tooltip("Degrees per second the yaw swings around behind her.")]
        [Min(1f)] [SerializeField] float autoAlignRate = 90f;

        [Header("Cinematic guidance")]
        [Tooltip("Seconds for a guided framing to swing round to its new yaw " +
                 "and pitch. Slower than the player camera on purpose, so a " +
                 "beat taking the view over does not feel like a whip pan.")]
        [Min(0.01f)] [SerializeField] float guideRotationSmooth = 0.55f;

        [Tooltip("Seconds for a guided framing to pull back to its new distance.")]
        [Min(0.01f)] [SerializeField] float guidePositionSmooth = 0.70f;

        [Tooltip("How far along the line from Ari to the point of interest the " +
                 "pivot sits. 0 is Ari alone, 0.4 is roughly two thirds of the " +
                 "way to whatever the beat wants in shot — which is what puts " +
                 "Mono and Ari in the same frame instead of one behind the other.")]
        [Range(0f, 1f)] [SerializeField] float guidePivotBlend = 0.4f;

        Camera _camera;
        AriMover _mover;
        float _yaw;
        float _pitch;
        float _distance;
        float _idleSinceDrag;

        // Guided framing. Written by a beat, read by Apply, and left alone
        // entirely when nothing is guiding — the player camera is the default
        // state, not the fallback.
        bool _guiding;
        float _guideBlend;                 // 0 = Ari's own pivot, 1 = the beat's
        Vector3 _guideInterest;            // the world point being framed
        float _guideDistance;
        float _guidePitch;
        float _guideYaw;
        Vector3 _pivot;
        bool _pivotInitialized;

        void Awake()
        {
            _camera = GetComponent<Camera>();

            if (target == null)
            {
                var ari = GameObject.Find("Ari");
                if (ari != null) target = ari.transform;
            }

            if (target != null) _mover = target.GetComponent<AriMover>();

            _distance = Mathf.Clamp(startDistance, minDistance, maxDistance);
            _pitch = Mathf.Clamp(startPitch, minPitch, maxPitch);

            // Start behind her rather than at whatever yaw the editor left, so the
            // first frame of play is already a sensible over-the-shoulder view.
            _yaw = target != null ? target.eulerAngles.y : transform.eulerAngles.y;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                // No target means no basis for a follow camera. Holding the last
                // position is better than snapping to the origin, which would
                // throw the view across the village.
                return;
            }

            // A beat that has taken the camera over gets it outright — no mouse,
            // no auto-align. Mixing the two means the player's scroll fights the
            // beat's framing and neither of them gets what it asked for.
            if (_guiding) GuideStep(Time.deltaTime);
            else
            {
                ReadMouse();
                AutoAlign();
            }

            Apply();
        }

        /// <summary>
        /// Move the existing framing variables towards the beat's, rather than
        /// assigning them.
        ///
        /// The beat's values are a target; Apply() is still the thing that
        /// places the camera, so it keeps the wall pull-in and the smoothing
        /// that make a guide look like the same camera and not a cut. Assigning
        /// _yaw directly would also break auto-align's memory of where the
        /// player last put the camera, and the view would snap on release.
        /// </summary>
        void GuideStep(float dt)
        {
            float rotRate = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, guideRotationSmooth));
            float posRate = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, guidePositionSmooth));

            _yaw = Mathf.LerpAngle(_yaw, _guideYaw, rotRate);
            _pitch = Mathf.Lerp(_pitch, _guidePitch, rotRate);
            _distance = Mathf.Clamp(_distance + (_guideDistance - _distance) * posRate,
                                    minDistance, maxDistance);

            // The pivot's share of the way to the point of interest, eased in
            // over the same period so the two characters do not separate at the
            // moment the camera starts turning.
            _guideBlend = Mathf.MoveTowards(_guideBlend, 1f,
                                            dt / Mathf.Max(0.01f, guideRotationSmooth));
        }

        void ReadMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            bool dragging = mouse.rightButton.isPressed;

            if (dragging)
            {
                // delta is a raw device delta and is not framerate-normalised, so
                // it is scaled by dt. Without that the camera orbits faster on a
                // fast machine purely because it draws more frames, and a 240Hz
                // mouse would orbit at four times the speed of a 60Hz one.
                Vector2 delta = mouse.delta.ReadValue() * (0.06f * Time.deltaTime);

                _yaw += delta.x * 12f;
                _pitch = Mathf.Clamp(_pitch - delta.y * 12f, minPitch, maxPitch);
            }

            // Reset on any frame the button is up, so the timer measures time
            // since the last drag rather than time since the drag started.
            _idleSinceDrag = dragging ? 0f : _idleSinceDrag + Time.deltaTime;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _distance = Mathf.Clamp(
                    _distance - scroll * 0.01f * zoomStep, minDistance, maxDistance);
            }
        }

        /// <summary>
        /// Ease the yaw around behind Ari while she walks and the mouse is idle.
        /// </summary>
        void AutoAlign()
        {
            if (_mover == null || _idleSinceDrag < autoAlignDelay) return;
            if (_mover.CurrentSpeed < autoAlignSpeed) return;

            // 180 degrees puts the camera behind her: her forward is her local +Z,
            // and the camera sits at -Z of the pivot in its own rotated frame.
            float wanted = target.eulerAngles.y + 180f;
            _yaw = Mathf.MoveTowardsAngle(_yaw, wanted, autoAlignRate * Time.deltaTime);
        }

        void Apply()
        {
            // Two pivots: Ari's own, and the beat's. The blend between them is
            // the whole mechanism — at 0 the camera is exactly the player
            // camera, so releasing a guide is not a change of state at all, it
            // is the same camera with a lever back at zero.
            Vector3 ariPivot = target.position + Vector3.up * pivotHeight;
            Vector3 wantPivot = _guiding
                ? Vector3.Lerp(ariPivot, _guideInterest, _guideBlend * guidePivotBlend)
                : ariPivot;

            if (!_pivotInitialized) { _pivot = wantPivot; _pivotInitialized = true; }
            else
            {
                _pivot = Vector3.Lerp(_pivot, wantPivot,
                                      1f - Mathf.Exp(-Time.deltaTime /
                                                     Mathf.Max(0.01f, positionSmooth)));
            }

            var pivot = _pivot;
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var offset = rotation * new Vector3(0f, 0f, -_distance);

            // Pull in if something solid is between her and where the camera
            // wants to be, or it ends up on the far side of a wall. Done before
            // the smoothing so the pull-in is a target, not a correction applied
            // after the fact — the camera then slides in rather than punching
            // through and snapping back.
            float allowed = _distance;
            if (Physics.Raycast(pivot, offset.normalized, out RaycastHit hit,
                                _distance, obstructionMask,
                                QueryTriggerInteraction.Ignore))
            {
                // Back off by a margin, otherwise the near plane ends up flush
                // with the surface and the wall fills the whole screen.
                allowed = Mathf.Max(minDistance * 0.5f, hit.distance - 0.25f);
            }

            // SmoothDamp on the position rather than a lerp factor, because it is
            // the only one of the two that stays frame-rate independent when the
            // smoothing time is expressed in seconds.
            Vector3 want = pivot + offset.normalized * allowed;
            transform.position = Vector3.SmoothDamp(
                transform.position, want, ref _velocity, positionSmooth);

            Quaternion wantRot = rotation;
            transform.rotation = Quaternion.Slerp(
                transform.rotation, wantRot, 1f - Mathf.Exp(-Time.deltaTime / rotationSmooth));
        }

        Vector3 _velocity;

        /// <summary>
        /// Puts the camera behind Ari at the default framing. Bound so it can be
        /// reset from a key or a button when the view has drifted somewhere
        /// unhelpful.
        /// </summary>
        [ContextMenu("Snap To Default View")]
        public void SnapToDefault()
        {
            _distance = Mathf.Clamp(startDistance, minDistance, maxDistance);
            _pitch = Mathf.Clamp(startPitch, minPitch, maxPitch);

            // Her forward is her local +Z, and the camera is placed at -Z of the
            // pivot in its own frame, so matching her yaw puts it behind her.
            if (target != null) _yaw = target.eulerAngles.y;
        }

        // --- cinematic guidance --------------------------------------------------

        /// <summary>Is a beat currently holding the camera? Read by the beat itself.</summary>
        public bool IsGuiding => _guiding;

        /// <summary>
        /// Frame a point in the world along with Ari, and keep doing it.
        ///
        /// The one-call form a beat wants, because the arithmetic it would
        /// otherwise repeat is not the interesting part: where the camera has
        /// to end up for two characters to be in shot at once.
        ///
        /// The camera goes on the far side of Ari from the point of interest and
        /// looks along the line between them. That is the only arrangement that
        /// reliably holds both — a camera locked to Ari's back has Mono directly
        /// behind the near plane, and a camera that merely widens puts him at
        /// the very edge of frame, which is where the player will not look.
        ///
        /// Distance is not a constant. It is however far apart the two are, plus
        /// a margin, so a beat that sends them down a long gully gets a wider
        /// shot automatically instead of having to compute one and hard-code it.
        /// </summary>
        /// <param name="interest">The world point to keep in shot — Mono, a door,
        /// the end of the alley.</param>
        /// <param name="height">Metres the camera looks down from. 1.8 puts both
        /// characters' heads and the floor between them in frame.</param>
        /// <param name="extraMetres">Added on top of the measured gap. The gap
        /// alone is exactly as tight as it can be and clips.</param>
        public void Guide(Vector3 interest, float height = 1.8f, float extraMetres = 2.5f)
        {
            if (target == null) return;

            _guiding = true;
            _guideInterest = interest;

            // Yaw from interest towards Ari, flattened: the camera sits behind
            // Ari on that line. A camera that looks the other way puts the
            // interest point behind the player.
            var along = Flat(interest - target.position);
            float yaw = along.sqrMagnitude < 0.01f
                ? target.eulerAngles.y
                : Quaternion.LookRotation(along.normalized, Vector3.up).eulerAngles.y;

            _guideYaw = yaw;

            float gap = Vector3.Distance(Flat(interest), Flat(target.position));
            _guideDistance = Mathf.Clamp(gap + extraMetres, minDistance, maxDistance);

            // Pitch from the height, so the framing is a consequence of where the
            // beat asked to look from rather than a number that has to match the
            // distance by hand.
            _guidePitch = Mathf.Clamp(Mathf.Atan2(height, Mathf.Max(0.5f, _guideDistance))
                                          * Mathf.Rad2Deg,
                                      minPitch, maxPitch);
        }

        /// <summary>
        /// Hand the camera back to the player.
        ///
        /// _yaw, _pitch and _distance are left where the guide put them, so
        /// control resumes from the framing the player is currently looking at
        /// and drifts back behind Ari on auto-align if they let go. Snapping to
        /// Ari's back instead would be a cut, and a cut at the moment a beat
        /// hands control over is exactly where a player expects to be looking
        /// somewhere new.
        /// </summary>
        public void ReleaseGuide()
        {
            _guiding = false;
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }
    }
}
