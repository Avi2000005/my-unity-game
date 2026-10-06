using System;
using System.Runtime.CompilerServices;
using UnityEngine;

using Object = UnityEngine.Object;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Ari Mover")]
    [RequireComponent(typeof(Animator))]
    public sealed class AriMover : MonoBehaviour
    {
    	[Header("Movement")]
    	[Tooltip("Metres per second on the ground.")]
    	[Min(0f)]
    	[SerializeField]
    	private float walkSpeed = 2.2f;

    	[Tooltip("Metres per second while a sprint input is held. The walk clip plays at walkSpeed; anything faster reuses it, which is fine at this scale but is the first thing to replace with a run clip.")]
    	[Min(0f)]
    	[SerializeField]
    	private float runSpeed = 3.6f;

    	[Tooltip("Degrees per second when turning to face the direction of travel.")]
    	[Min(1f)]
    	[SerializeField]
    	private float turnRate = 720f;

    	[Header("Ground")]
    	[Tooltip("How far below her feet to look for ground.")]
    	[Min(0.1f)]
    	[SerializeField]
    	private float groundProbe = 1.5f;

    	[Tooltip("Layer mask the ground raycast hits. Empty means everything.")]
    	[SerializeField]
    	private LayerMask groundMask = (-1);

    	[Tooltip("How level a surface must be before she will stand on it, as the up-component of its normal. 1 is flat, 0.7 is about 45 degrees. This is what keeps her off roofs and out of wall caps once the whole village has colliders.")]
    	[Range(0f, 1f)]
    	[SerializeField]
    	private float minGroundSlope = 0.7f;

    	[Header("Sole offset")]
    	[Tooltip("Distance from this transform down to the soles. Measured from the skinned mesh at startup; the inspector value is only a fallback for when there is no renderer to measure.")]
    	[SerializeField]
    	private float soleOffset = 0.1717f;

    	[Tooltip("Metres per second she closes a height difference. The paving, the paths and the kerb line sit at three different heights so they do not z-fight, which means snapping straight to the surface pops her up a few centimetres at every tile seam. Easing it keeps her on the ground without the stair-stepping.")]
    	[Min(0.1f)]
    	[SerializeField]
    	private float groundFollow = 6f;

    	[Header("Input")]
    	[Tooltip("Run modifier. Held, it raises the target speed.")]
    	[SerializeField]
    	private Key runKey = Key.LeftShift;

    	[Tooltip("Also honour the on-screen pad. Turned on automatically when screen controls are added to the scene. The keyboard keeps working either way — this only adds a second way in.")]
    	[SerializeField]
    	private bool useScreenControls;

    	[Header("Jump")]
    	[Tooltip("Jump. Only read while she is already on something solid, so holding it down does not make her bounce the moment she lands.")]
    	[SerializeField]
    	private Key jumpKey = Key.Space;

    	[Tooltip("Peak height of the hop above the surface, in metres. The launch speed is derived from this rather than set directly, so changing the height cannot desync it from the gravity below the way a hand-picked speed does.")]
    	[Min(0.1f)]
    	[SerializeField]
    	private float jumpHeight = 1.2f;

    	[Tooltip("Downward acceleration, as a positive number. 18 is not Earth's 9.8: this is a stylised arc, and a real-gravity hop at this height hangs at the top long enough to look like floating. Faster gravity keeps the hop reading as one motion.")]
    	[Min(0.1f)]
    	[SerializeField]
    	private float gravity = 18f;

    	[Tooltip("How much of her ground speed she keeps while off the ground, 0 to 1. Some, so a jump can be steered in the air; not all, so she cannot change direction instantly at the apex.")]
    	[Range(0f, 1f)]
    	[SerializeField]
    	private float airControl = 0.6f;

    	[Tooltip("How far above the surface still counts as having landed, in metres. Without it, a frame at a low frame rate steps her past the paving and she falls through it.")]
    	[Min(0f)]
    	[SerializeField]
    	private float landTolerance = 0.25f;

    	[Header("Walls")]
    	[Tooltip("Whether she is stopped by the sides of things. On by default because the narrow-alley beat and every building interior are unbuildable without it: she used to walk through houses, and once the hop landed she jumped through them too.")]
    	[SerializeField]
    	private bool collideWithWalls = true;

    	[Tooltip("Radius of the body she pushes around with, in metres. 0.30 is deliberately wider than she looks. A tight capsule reads as more accurate and is not: it catches on the corner of every doorframe in a village made of boxes, and the result is a player who appears to be clipping into geometry rather than one who is brushing past it.")]
    	[Min(0.05f)]
    	[SerializeField]
    	private float bodyRadius = 0.3f;

    	[Tooltip("Total height of the body, soles to crown. Matches the 1.80 measured on her mesh; a separate number from soleOffset because that one is read off the skin at runtime.")]
    	[Min(0.5f)]
    	[SerializeField]
    	private float bodyHeight = 1.8f;

    	[Tooltip("How far above her soles the sides of the body start. The paving she stands on is level with the soles, and a capsule whose bottom sphere is tangent to a surface is a coin toss: it either reports a hit on the floor or misses a wall, depending on which way the float rounds. Lifting it clear of the ground costs a collision with anything shorter than this and makes the floor case impossible.")]
    	[Min(0f)]
    	[SerializeField]
    	private float bodyBottomLift = 0.1f;

    	[Tooltip("How high a kerb or a single step she walks up without jumping. The village's paths and paving sit at three different heights, so without this she is stopped by every seam between them.")]
    	[Min(0f)]
    	[SerializeField]
    	private float stepHeight = 0.35f;

    	[Tooltip("Layer mask the sideways sweep hits. Triggers are always ignored, so paint targets, pickups and checkpoints do not need to be kept off this list to be walkable.")]
    	[SerializeField]
    	private LayerMask wallMask = (-1);

    	[Header("Animator")]
    	[Tooltip("Float the controller crossfades on. Must match the parameter name in Ari.controller.")]
    	[SerializeField]
    	private string speedParameter = "Speed";

    	[Tooltip("Trigger the controller jumps on. Must match Ari.controller.")]
    	[SerializeField]
    	private string jumpParameter = "Jump";

    	[Tooltip("Bool the controller ends the jump on. Must match Ari.controller.")]
    	[SerializeField]
    	private string groundedParameter = "Grounded";

    	[Tooltip("Trigger the controller swings the brush on. Must match Ari.controller.")]
    	[SerializeField]
    	private string swingParameter = "Swing";

    	[Tooltip("Bool the controller holds for the length of the swing. The graph cannot end the stroke on Speed, because Speed already says 'standing' before the stroke begins and the swing would last one frame.")]
    	[SerializeField]
    	private string swingingParameter = "Swinging";

    	private Animator _animator;

    	private int _speedHash;

    	private int _jumpHash;

    	private int _groundedHash;

    	private int _swingHash;

    	private int _swingingHash;

    	private Vector3 _groundNormal = Vector3.up;

    	[Min(0.05f)]
    	[SerializeField]
    	private float swingSeconds = 0.31f;

    	private float _swingTimer;

    	private float _verticalVelocity;

    	private float _jumpPeakY;

    	public float GroundGap { get; private set; }

    	public float CurrentSpeed { get; private set; }

    	public bool IsGrounded { get; private set; } = true;

    	public float LastJumpHeight { get; private set; }

    	public bool IsAirborne => !IsGrounded;

    	public bool Frozen { get; set; }

    	public bool TouchingWall { get; private set; }

    	public string WallName { get; private set; } = "none";

    	public float BodyHeight => bodyHeight;

    	public float BodyRadius => bodyRadius;

    	public float StepHeight => stepHeight;

    	public float LastSweepRatio { get; private set; } = 1f;

    	public bool UseScreenControls
    	{
    		get
    		{
    			return useScreenControls;
    		}
    		set
    		{
    			useScreenControls = value;
    		}
    	}
	// ILSpy could not name this property's backing store and emitted the bare word 'field' instead, which is not a member of anything. Both accessors are public, and the property below is written the same longhand way, so it is an auto-property.
	public static Vector2 ScreenMove { get; set; }

    	public static bool ScreenRun { get; set; }

    	public bool IsSwinging => _swingTimer > 0f;

    	public void Teleport(Vector3 where)
    	{
    		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
    		_verticalVelocity = 0f;
    		_swingTimer = 0f;
    		_jumpPeakY = where.y;
    		((Component)this).transform.position = where;
    		Physics.SyncTransforms();
    	}

    	public float TryPush(Vector3 delta)
    	{
    		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
    		if (delta.sqrMagnitude < 1E-06f)
    		{
    			return 0f;
    		}
    		Vector3 val = new Vector3(delta.x, 0f, delta.z);
    		float magnitude = val.magnitude;
    		if (magnitude < 0.0001f)
    		{
    			return 0f;
    		}
    		Vector3 val2 = val / magnitude;
    		Vector3 position = ((Component)this).transform.position;
    		Vector3 val3 = Sweep(position, val2 * magnitude);
    		float magnitude2 = val3.magnitude;
    		if (magnitude2 > 0f)
    		{
    			((Component)this).transform.position = position + val3;
    		}
    		_verticalVelocity = 0f;
    		Physics.SyncTransforms();
    		return magnitude2;
    	}

    	private void Awake()
    	{
    		Bind();
    		if (!((Behaviour)_animator).enabled)
    		{
    			((Behaviour)_animator).enabled = true;
    		}
    		MeasureSoleOffset();
    	}

    	private void Bind()
    	{
    		if ((Object)(object)_animator == (Object)null)
    		{
    			_animator = ((Component)this).GetComponent<Animator>();
    		}
    		if (!((Object)(object)_animator == (Object)null))
    		{
    			_speedHash = Animator.StringToHash(speedParameter);
    			_jumpHash = Animator.StringToHash(jumpParameter);
    			_groundedHash = Animator.StringToHash(groundedParameter);
    			_swingHash = Animator.StringToHash(swingParameter);
    			_swingingHash = Animator.StringToHash(swingingParameter);
    		}
    	}

    	private void OnDisable()
    	{
    		if (!((Object)(object)_animator == (Object)null))
    		{
    			_animator.ResetTrigger(_jumpHash);
    			_animator.ResetTrigger(_swingHash);
    			_animator.SetBool(_swingingHash, false);
    		}
    	}

    	public bool PlaySwing()
    	{
    		Bind();
    		if ((Object)(object)_animator == (Object)null)
    		{
    			return false;
    		}
    		_animator.SetTrigger(_swingHash);
    		_animator.SetBool(_swingingHash, true);
    		_swingTimer = swingSeconds;
    		return true;
    	}

    	private void MeasureSoleOffset()
    	{
    		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0035: Expected Obj, but got Unknown
    		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
    		SkinnedMeshRenderer componentInChildren = ((Component)this).GetComponentInChildren<SkinnedMeshRenderer>();
    		if ((Object)(object)componentInChildren == (Object)null || (Object)(object)componentInChildren.sharedMesh == (Object)null)
    		{
    			return;
    		}
    		_animator.Update(0f);
    		Mesh val = new Mesh();
    		componentInChildren.BakeMesh(val);
    		Vector3[] vertices = val.vertices;
    		if (vertices.Length == 0)
    		{
    			Object.Destroy((Object)(object)val);
    			return;
    		}
    		Matrix4x4 localToWorldMatrix = ((Component)componentInChildren).transform.localToWorldMatrix;
    		float num = float.MaxValue;
    		for (int i = 0; i < vertices.Length; i++)
    		{
    			float y = localToWorldMatrix.MultiplyPoint3x4(vertices[i]).y;
    			if (y < num)
    			{
    				num = y;
    			}
    		}
    		Object.Destroy((Object)(object)val);
    		soleOffset = ((Component)this).transform.position.y - num;
    	}

    	private void Update()
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 val = KeyboardInput();
    		Vector3 val2 = new Vector3(ScreenMove.x, 0f, ScreenMove.y);
    		bool flag = useScreenControls && val2.sqrMagnitude > 0.0001f;
    		Vector3 wish;
    		if (val.sqrMagnitude > 0.0001f)
    		{
    			wish = val;
    		}
    		else
    		{
    			wish = (flag ? val2 : Vector3.zero);
    		}
    		bool running = KeyHeld(runKey) || (useScreenControls && ScreenRun);
    		bool jump = JumpPressed();
    		Step(wish, running, Time.deltaTime, jump);
    	}

    	private bool JumpPressed()
    	{
    		return Keyboard.current?[jumpKey].wasPressedThisFrame ?? false;
    	}

    	private static Vector3 KeyboardInput()
    	{
    		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
    		Keyboard current = Keyboard.current;
    		if (current == null)
    		{
    			return Vector3.zero;
    		}
    		float num = 0f;
    		float num2 = 0f;
    		if (current.aKey.isPressed || current.leftArrowKey.isPressed)
    		{
    			num--;
    		}
    		if (current.dKey.isPressed || current.rightArrowKey.isPressed)
    		{
    			num++;
    		}
    		if (current.wKey.isPressed || current.upArrowKey.isPressed)
    		{
    			num2++;
    		}
    		if (current.sKey.isPressed || current.downArrowKey.isPressed)
    		{
    			num2--;
    		}
    		Vector3 result = new Vector3(num, 0f, num2);
    		if (!(result.sqrMagnitude > 1f))
    		{
    			return result;
    		}
    		return result.normalized;
    	}

    	private static bool KeyHeld(Key key)
    	{
    		return Keyboard.current?[key].isPressed ?? false;
    	}

    	public void Step(Vector3 wish, bool running, float dt, bool jump = false)
    	{
    		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
    		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
    		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
    		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0382: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0278: Unknown result type (might be due to invalid IL or missing references)
    		//IL_031e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0398: Unknown result type (might be due to invalid IL or missing references)
    		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03a2: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03db: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03e0: Unknown result type (might be due to invalid IL or missing references)
    		//IL_03e5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_041b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0422: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0427: Unknown result type (might be due to invalid IL or missing references)
    		//IL_042c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0433: Unknown result type (might be due to invalid IL or missing references)
    		Bind();
    		if ((Object)(object)_animator == (Object)null)
    		{
    			return;
    		}
    		if (Frozen || ((Object)(object)AriHealth.I != (Object)null && AriHealth.I.IsDead))
    		{
    			wish = Vector3.zero;
    			running = false;
    			jump = false;
    		}
    		if (_swingTimer > 0f)
    		{
    			_swingTimer -= dt;
    			if (_swingTimer <= 0f)
    			{
    				_swingTimer = 0f;
    				_animator.SetBool(_swingingHash, false);
    			}
    		}
    		Vector3 val = new Vector3(wish.x, 0f, wish.z);
    		Camera main = Camera.main;
    		if ((Object)(object)main != (Object)null && val.sqrMagnitude > 0.0001f)
    		{
    			Vector3 forward = ((Component)main).transform.forward;
    			Vector3 right = ((Component)main).transform.right;
    			forward.y = 0f;
    			right.y = 0f;
    			if (forward.sqrMagnitude < 1E-06f)
    			{
    				forward = Vector3.forward;
    			}
    			if (right.sqrMagnitude < 1E-06f)
    			{
    				right = Vector3.right;
    			}
    			forward.Normalize();
    			right.Normalize();
    			val = forward * val.z + right * val.x;
    			if (val.sqrMagnitude > 0.0001f)
    			{
    				val.Normalize();
    			}
    		}
    		float num = (CurrentSpeed = ((!(val.sqrMagnitude > 0.0001f)) ? 0f : (running ? runSpeed : walkSpeed)));
    		Vector3 val2 = val * num;
    		Transform transform = ((Component)this).transform;
    		Vector3 delta = val2 * dt;
    		Vector3 val3 = Sweep(transform.position, delta);
    		LastSweepRatio = ((delta.sqrMagnitude < 1E-08f) ? 1f : Mathf.Clamp01(val3.magnitude / delta.magnitude));
    		Vector3 val4 = transform.position + val3;
    		if (IsGrounded & jump)
    		{
    			_verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
    			_jumpPeakY = transform.position.y;
    			IsGrounded = false;
    			_animator.SetTrigger(_jumpHash);
    		}
    		if (IsGrounded)
    		{
    			if (TryGround(val4, out var y, out _groundNormal))
    			{
    				float num3 = y + soleOffset;
    				val4.y = Mathf.MoveTowards(transform.position.y, num3, groundFollow * dt);
    			}
    		}
    		else
    		{
    			Vector3 delta2 = val2 * airControl * dt;
    			val4 = transform.position + Sweep(transform.position, delta2);
    			_verticalVelocity -= gravity * dt;
    			val4.y += _verticalVelocity * dt;
    			if (val4.y > _jumpPeakY)
    			{
    				_jumpPeakY = val4.y;
    			}
    			if (_verticalVelocity <= 0f && TryGround(val4, out var y2, out _groundNormal) && val4.y <= y2 + soleOffset + landTolerance)
    			{
    				val4.y = y2 + soleOffset;
    				_verticalVelocity = 0f;
    				IsGrounded = true;
    				LastJumpHeight = _jumpPeakY - (y2 + soleOffset);
    			}
    		}
    		transform.position = val4;
    		if (val.sqrMagnitude > 0.0001f)
    		{
    			Quaternion val5 = Quaternion.LookRotation(val, Vector3.up);
    			float num4 = turnRate * dt * ((float)Math.PI / 180f);
    			transform.rotation = Quaternion.RotateTowards(transform.rotation, val5, num4);
    		}
    		else if (AriAnim.Talking)
    		{
    			Vector3 val6 = AriAnim.TalkFacing - transform.position;
    			val6.y = 0f;
    			if (val6.sqrMagnitude > 0.0004f)
    			{
    				float num5 = turnRate * 0.6f * dt * ((float)Math.PI / 180f);
    				transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(val6.normalized, Vector3.up), num5);
    			}
    		}
    		_animator.SetFloat(_speedHash, CurrentSpeed);
    		_animator.SetBool(_groundedHash, IsGrounded);
    		AriAnim.SetRun(running && num > 0f);
    	}

    	private void CapsuleAt(Vector3 at, out Vector3 bottom, out Vector3 top)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
    		float num = at.y - soleOffset;
    		bottom = new Vector3(at.x, num + bodyBottomLift + bodyRadius, at.z);
    		top = new Vector3(at.x, num + bodyHeight - bodyRadius, at.z);
    		if (top.y < bottom.y)
    		{
    			top = bottom;
    		}
    	}

    	private Vector3 Sweep(Vector3 from, Vector3 delta)
    	{
    		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
    		TouchingWall = false;
    		WallName = "none";
    		delta.y = 0f;
    		if (!collideWithWalls || delta.sqrMagnitude < 1E-08f)
    		{
    			return delta;
    		}
    		if (!Blocked(from, delta, out var _))
    		{
    			return delta;
    		}
    		if (TryStepUp(from, delta, out var accepted))
    		{
    			TouchingWall = true;
    			WallName = "stepped";
    			return accepted;
    		}
    		return Slide(from, delta);
    	}

    	private Vector3 Slide(Vector3 from, Vector3 delta)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 val = delta;
    		for (int i = 0; i < 3; i++)
    		{
    			if (val.sqrMagnitude < 1E-08f)
    			{
    				break;
    			}
    			if (!Blocked(from, val, out var hit))
    			{
    				break;
    			}
    			TouchingWall = true;
    			if (WallName == "none")
    			{
    				WallName = ((Object)hit.collider).name;
    			}
    			val = Vector3.ProjectOnPlane(val, hit.normal);
    		}
    		return val;
    	}

    	private bool TryStepUp(Vector3 from, Vector3 delta, out Vector3 accepted)
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
    		accepted = Vector3.zero;
    		if (stepHeight <= 0f)
    		{
    			return false;
    		}
    		Vector3 val = from + Vector3.up * stepHeight;
    		if (Blocked(val, delta, out var _))
    		{
    			return false;
    		}
    		if (!TryGround(val + delta, out var y, out var _))
    		{
    			return false;
    		}
    		float num = y - (from.y - soleOffset);
    		if (num <= 0.001f || num > stepHeight)
    		{
    			return false;
    		}
    		accepted = delta;
    		return true;
    	}

    	private bool Blocked(Vector3 from, Vector3 delta, out RaycastHit hit)
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
    		CapsuleAt(from, out var bottom, out var top);
    		return Physics.CapsuleCast(bottom, top, bodyRadius, delta.normalized, out hit, delta.magnitude, (wallMask), (QueryTriggerInteraction)1);
    	}

    	private bool TryGround(Vector3 at, out float y, out Vector3 normal)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
    		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 val = at + Vector3.up * groundProbe;
    		RaycastHit val2 = default;
    		for (int i = 0; i < 4; i++)
    		{
    			if (!Physics.Raycast(val, Vector3.down, out val2, groundProbe * 2f, (groundMask), (QueryTriggerInteraction)1))
    			{
    				break;
    			}
    			if (val2.normal.y >= minGroundSlope)
    			{
    				y = val2.point.y;
    				normal = val2.normal;
    				GroundGap = 0f;
    				return true;
    			}
    		}
    		y = at.y;
    		normal = Vector3.up;
    		GroundGap = float.NaN;
    		return false;
    	}

    	public AriMover()
    	{
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
    	}
    }
}
