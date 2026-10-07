using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{
    [AddComponentMenu("Echoes/Ari Follow Camera")]
    [RequireComponent(typeof(Camera))]
    public sealed class AriFollowCamera : MonoBehaviour
    {
        public static AriFollowCamera Instance { get; private set; }
        public static bool IsFreeLookActive { get; private set; }
        private bool _wasFreeLookHeld = false;

        [Header("Target")]
        private bool IsMovingForward
        {
            get
            {
                Keyboard kb = Keyboard.current;
                if (kb != null)
                {
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) return true;
                }
                return false;
            }
        }

        private bool IsBackOrStrafeOnly
        {
            get
            {
                Keyboard kb = Keyboard.current;
                if (kb != null)
                {
                    bool movingForward = kb.wKey.isPressed || kb.upArrowKey.isPressed;
                    bool movingOther = kb.sKey.isPressed || kb.downArrowKey.isPressed ||
                                       kb.aKey.isPressed || kb.leftArrowKey.isPressed ||
                                       kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                    return movingOther && !movingForward;
                }
                return false;
            }
        }

        [Tooltip("Who to follow. Falls back to the object named Ari.")]
        [SerializeField]
        private Transform target;

        [Tooltip("Height above feet to orbit around.")]
        [SerializeField]
        private float pivotHeight = 1.35f;

        [Header("Distance")]
        [Min(0.5f)]
        [SerializeField]
        private float minDistance = 2.2f;

        [Min(1f)]
        [SerializeField]
        private float maxDistance = 14f;

        [SerializeField]
        private float startDistance = 5.5f;

        [Min(0.01f)]
        [SerializeField]
        private float zoomStep = 0.8f;

        [Header("Angles (BGMI Style TPP)")]
        [Range(-40f, 10f)]
        [SerializeField]
        private float minPitch = -22f;

        [Range(10f, 89f)]
        [SerializeField]
        private float maxPitch = 76f;

        [SerializeField]
        private float startPitch = 15f;

        [Header("Controls & Sensitivity")]
        [Tooltip("Mouse look sensitivity for 360 camera.")]
        [Range(0.1f, 10f)]
        [SerializeField]
        private float mouseSensitivity = 1.6f;

        [Tooltip("Whether to lock cursor on click for continuous 360 mouse look.")]
        [SerializeField]
        private bool lockCursorOnClick = true;

        [Header("BGMI Free Look / Spring-Back")]
        [Tooltip("Seconds of no mouse look before camera smoothly springs back behind Ari.")]
        [SerializeField]
        private float returnDelay = 1.0f;

        [Tooltip("Speed in degrees/sec at which the camera springs back behind Ari.")]
        [SerializeField]
        private float returnSpeed = 140f;

        [Header("Obstruction")]
        [SerializeField]
        private LayerMask obstructionMask = (-1);

        [Header("Feel")]
        [Min(0.01f)]
        [SerializeField]
        private float positionSmooth = 0.12f;

        [Min(0.01f)]
        [SerializeField]
        private float rotationSmooth = 0.05f;

        [Header("Cinematic guidance")]
        [Min(0.01f)]
        [SerializeField]
        private float guideRotationSmooth = 0.55f;

        [Min(0.01f)]
        [SerializeField]
        private float guidePositionSmooth = 0.7f;

        [Range(0f, 1f)]
        [SerializeField]
        private float guidePivotBlend = 0.4f;

        private Camera _camera;
        private AriMover _mover;
        private float _yaw;
        private float _pitch;
        private float _distance;
        private float _idleSinceDrag;
        private bool _guiding;
        private float _guideBlend;
        private Vector3 _guideInterest;
        private float _guideDistance;
        private float _guidePitch;
        private float _guideYaw;
        private Vector3 _pivot;
        private bool _pivotInitialized;
        private Vector3 _velocity;

        // Victory Cinematic state
        private bool _victoryMode;
        private Vector3 _victoryCenter;
        private float _victoryTimer;

        public bool IsGuiding => _guiding;
        public bool IsVictoryCinematic => _victoryMode;

        private void Awake()
        {
            Instance = this;
            _camera = GetComponent<Camera>();
            if (target == null)
            {
                GameObject val = GameObject.Find("Ari");
                if (val != null)
                {
                    target = val.transform;
                }
            }
            if (target != null)
            {
                _mover = target.GetComponent<AriMover>();
            }
            _distance = Mathf.Clamp(startDistance, minDistance, maxDistance);
            _pitch = Mathf.Clamp(startPitch, minPitch, maxPitch);
            // Default angle behind Ari looking forward
            _yaw = (target != null) ? target.eulerAngles.y : transform.eulerAngles.y;
        }

        private void Update()
        {
            HandleCursorLock();
            CheckQuickRecenter();
        }

        private void CheckQuickRecenter()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.vKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame))
            {
                RecenterBehindAri();
            }
        }

        public void RecenterBehindAri()
        {
            if (target != null)
            {
                _yaw = target.eulerAngles.y;
                _idleSinceDrag = returnDelay + 1f;
            }
        }

        private void HandleCursorLock()
        {
            if (_victoryMode || MonoCompanion.WakeDialogueActive || (AriHealth.I != null && AriHealth.I.IsDead) || LevelState.Completed)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                return;
            }

            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            Mouse m = Mouse.current;
            if (lockCursorOnClick && m != null)
            {
                if (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (_victoryMode)
            {
                VictoryStep(Time.deltaTime);
            }
            else if (_guiding)
            {
                GuideStep(Time.deltaTime);
            }
            else
            {
                ReadMouse();
                AutoSpringBack(Time.deltaTime);
            }

            Apply();
        }

        private void VictoryStep(float dt)
        {
            _victoryTimer += dt;
            // Majestic high panoramic view of the colorful village
            _pitch = Mathf.MoveTowards(_pitch, 22f, 16f * dt);
            // Pull back for wide view of village
            _distance = Mathf.MoveTowards(_distance, 11f, 2.2f * dt);
            // Continuous 360 degree flyaround showcase
            _yaw = (_yaw + 24f * dt) % 360f;

            // Center pivot around the restored fountain
            Vector3 center = _victoryCenter + Vector3.up * 1.5f;
            _pivot = Vector3.Lerp(_pivot, center, 2f * dt);
        }

        private void GuideStep(float dt)
        {
            float num = 1f - Mathf.Exp((0f - dt) / Mathf.Max(0.01f, guideRotationSmooth));
            float num2 = 1f - Mathf.Exp((0f - dt) / Mathf.Max(0.01f, guidePositionSmooth));
            _yaw = Mathf.LerpAngle(_yaw, _guideYaw, num);
            _pitch = Mathf.Lerp(_pitch, _guidePitch, num);
            _distance = Mathf.Clamp(_distance + (_guideDistance - _distance) * num2, minDistance, maxDistance);
            _guideBlend = Mathf.MoveTowards(_guideBlend, 1f, dt / Mathf.Max(0.01f, guideRotationSmooth));
        }

        private void ReadMouse()
        {
            Mouse current = Mouse.current;
            if (current == null) return;

            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            bool isDragging = current.rightButton.isPressed || current.leftButton.isPressed || current.middleButton.isPressed;

            // Check if Alt or Right Mouse Button (classic BGMI/Free Fire Free Look eye key) is held
            Keyboard kb = Keyboard.current;
            bool altHeld = kb != null && (kb.leftAltKey.isPressed || kb.rightAltKey.isPressed);
            bool freeLookButton = altHeld || current.rightButton.isPressed;

            IsFreeLookActive = freeLookButton;

            if (isLocked || isDragging || freeLookButton)
            {
                Vector2 delta = current.delta.ReadValue();
                if (delta.sqrMagnitude > 0.0001f)
                {
                    // 360 Free Look horizontally (Eye Symbol)
                    _yaw = (_yaw + delta.x * (mouseSensitivity * 0.12f)) % 360f;
                    if (_yaw < 0f) _yaw += 360f;

                    // Clamped Vertical Pitch
                    _pitch = Mathf.Clamp(_pitch - delta.y * (mouseSensitivity * 0.12f), minPitch, maxPitch);
                    _idleSinceDrag = 0f;
                }
                else
                {
                    _idleSinceDrag += Time.deltaTime;
                }
            }
            else
            {
                _idleSinceDrag += Time.deltaTime;
            }

            // Detect the moment Free Look is released
            if (_wasFreeLookHeld && !freeLookButton)
            {
                // Player released the eye button - immediately trigger smooth spring back behind Ari
                _idleSinceDrag = returnDelay + 1f;
            }
            _wasFreeLookHeld = freeLookButton;

            float scrollY = current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollY) > 0.01f)
            {
                _distance = Mathf.Clamp(_distance - Mathf.Sign(scrollY) * zoomStep, minDistance, maxDistance);
            }
        }

        private void AutoSpringBack(float dt)
        {
            // BGMI Eye Symbol Spring-Back:
            // 1. While holding the Eye / Free-Look button (Alt / Right-click), maintain free 360 view
            if (IsFreeLookActive) return;

            // 2. If the player is only moving backward (S) or strafing (A/D) without W:
            // Do NOT spring back! This prevents the camera from auto-spinning into player's face
            // or fighting the player when backpedaling/strafing!
            if (IsBackOrStrafeOnly) return;

            // 3. Return behind Ari if:
            // - User is moving forward (W pressed)
            // - OR if user has been idle (no mouse rotation) for returnDelay seconds (temporary 360 look finished)
            bool shouldReturn = IsMovingForward || (_idleSinceDrag >= returnDelay);

            if (shouldReturn && target != null)
            {
                float targetYaw = target.eulerAngles.y;
                float currentSpeed = returnSpeed;

                if (IsMovingForward)
                {
                    currentSpeed *= 1.4f; // Faster, responsive alignment when actively running forward
                }

                float angleDiff = Mathf.DeltaAngle(_yaw, targetYaw);
                if (Mathf.Abs(angleDiff) > 0.2f)
                {
                    _yaw = Mathf.MoveTowardsAngle(_yaw, targetYaw, currentSpeed * dt);
                }
                else
                {
                    _yaw = targetYaw;
                }
            }
        }

        private void Apply()
        {
            Vector3 val = target.position + Vector3.up * pivotHeight;
            Vector3 targetPivot = _victoryMode ? _pivot : (_guiding ? Vector3.Lerp(val, _guideInterest, _guideBlend * guidePivotBlend) : val);

            if (!_pivotInitialized)
            {
                _pivot = targetPivot;
                _pivotInitialized = true;
            }
            else if (!_victoryMode)
            {
                _pivot = Vector3.Lerp(_pivot, targetPivot, 1f - Mathf.Exp((0f - Time.deltaTime) / Mathf.Max(0.01f, positionSmooth)));
            }

            Vector3 pivot = _pivot;
            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 offset = rot * new Vector3(0f, 0f, -_distance);

            float currentDist = _distance;
            if (Physics.Raycast(pivot, offset.normalized, out RaycastHit hit, _distance, obstructionMask, QueryTriggerInteraction.Ignore))
            {
                currentDist = Mathf.Max(minDistance * 0.5f, hit.distance - 0.25f);
            }

            Vector3 wantedPos = pivot + offset.normalized * currentDist;
            transform.position = Vector3.SmoothDamp(transform.position, wantedPos, ref _velocity, positionSmooth);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp((0f - Time.deltaTime) / Mathf.Max(0.01f, rotationSmooth)));
        }

        [ContextMenu("Snap To Default View")]
        public void SnapToDefault()
        {
            _distance = Mathf.Clamp(startDistance, minDistance, maxDistance);
            _pitch = Mathf.Clamp(startPitch, minPitch, maxPitch);
            if (target != null)
            {
                _yaw = target.eulerAngles.y;
            }
        }

        public void StartVictoryCinematic(Vector3 focalPoint)
        {
            _victoryMode = true;
            _victoryCenter = focalPoint;
            _victoryTimer = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (target != null)
            {
                AriMover mover = target.GetComponent<AriMover>();
                if (mover != null) mover.Frozen = true;
            }
            Debug.Log("[Echoes] Victory cinematic camera started: rotating 360 around the village!");
        }

        public void ResetVictoryCinematic()
        {
            _victoryMode = false;
            _victoryTimer = 0f;
        }

        public void Guide(Vector3 interest, float height = 1.8f, float extraMetres = 2.5f)
        {
            if (target == null) return;
            _guiding = true;
            _guideInterest = interest;
            Vector3 val = Flat(interest - target.position);
            float y = (val.sqrMagnitude >= 0.01f) ? Quaternion.LookRotation(val.normalized, Vector3.up).eulerAngles.y : target.eulerAngles.y;
            _guideYaw = y;
            float num = Vector3.Distance(Flat(interest), Flat(target.position));
            _guideDistance = Mathf.Clamp(num + extraMetres, minDistance, maxDistance);
            _guidePitch = Mathf.Clamp(Mathf.Atan2(height, Mathf.Max(0.5f, _guideDistance)) * Mathf.Rad2Deg, minPitch, maxPitch);
        }

        public void ReleaseGuide()
        {
            _guiding = false;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
