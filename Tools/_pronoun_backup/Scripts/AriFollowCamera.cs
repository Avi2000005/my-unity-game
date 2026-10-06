using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Ari Follow Camera")]
    [RequireComponent(typeof(Camera))]
    public sealed class AriFollowCamera : MonoBehaviour
    {
    	[Header("Target")]
    	[Tooltip("Who to follow. Falls back to the object named Ari.")]
    	[SerializeField]
    	private Transform target;

    	[Tooltip("Height above her feet to orbit around. About chest height, so she sits low in the frame and most of the shot is the world.")]
    	[SerializeField]
    	private float pivotHeight = 1.35f;

    	[Header("Distance")]
    	[Tooltip("Closest the camera may come in.")]
    	[Min(0.5f)]
    	[SerializeField]
    	private float minDistance = 2.5f;

    	[Tooltip("Furthest the camera may pull out.")]
    	[Min(1f)]
    	[SerializeField]
    	private float maxDistance = 14f;

    	[SerializeField]
    	private float startDistance = 5.5f;

    	[Tooltip("Metres per zoom notch.")]
    	[Min(0.01f)]
    	[SerializeField]
    	private float zoomStep = 0.6f;

    	[Header("Angles")]
    	[Tooltip("Lowest the camera may drop. Kept just above horizontal so the camera never ends up under the paving looking up.")]
    	[Range(-30f, 10f)]
    	[SerializeField]
    	private float minPitch = -8f;

    	[Tooltip("Highest the camera may rise.")]
    	[Range(10f, 89f)]
    	[SerializeField]
    	private float maxPitch = 72f;

    	[SerializeField]
    	private float startPitch = 14f;

    	[Header("Obstruction")]
    	[Tooltip("What may pull the camera in. Needs to include the village walls and roofs, which have no colliders until Tools/Echoes/Add Village Colliders has been run — without that this does nothing at all.")]
    	[SerializeField]
    	private LayerMask obstructionMask = (-1);

    	[Header("Feel")]
    	[Tooltip("Seconds for the camera to close most of the way to where it wants to be. Lower is stiffer.")]
    	[Min(0.01f)]
    	[SerializeField]
    	private float positionSmooth = 0.14f;

    	[Tooltip("Seconds for the look direction to settle.")]
    	[Min(0.01f)]
    	[SerializeField]
    	private float rotationSmooth = 0.08f;

    	[Tooltip("Metres per second below which Ari counts as standing still.")]
    	[Min(0f)]
    	[SerializeField]
    	private float autoAlignSpeed = 0.4f;

    	[Tooltip("Seconds of mouse stillness before the camera starts following her heading again.")]
    	[Min(0f)]
    	[SerializeField]
    	private float autoAlignDelay = 1.1f;

    	[Tooltip("Degrees per second the yaw swings around behind her.")]
    	[Min(1f)]
    	[SerializeField]
    	private float autoAlignRate = 90f;

    	[Header("Cinematic guidance")]
    	[Tooltip("Seconds for a guided framing to swing round to its new yaw and pitch. Slower than the player camera on purpose, so a beat taking the view over does not feel like a whip pan.")]
    	[Min(0.01f)]
    	[SerializeField]
    	private float guideRotationSmooth = 0.55f;

    	[Tooltip("Seconds for a guided framing to pull back to its new distance.")]
    	[Min(0.01f)]
    	[SerializeField]
    	private float guidePositionSmooth = 0.7f;

    	[Tooltip("How far along the line from Ari to the point of interest the pivot sits. 0 is Ari alone, 0.4 is roughly two thirds of the way to whatever the beat wants in shot — which is what puts Mono and Ari in the same frame instead of one behind the other.")]
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

    	public bool IsGuiding => _guiding;

    	private void Awake()
    	{
    		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
    		_camera = ((Component)this).GetComponent<Camera>();
    		if ((Object)(object)target == (Object)null)
    		{
    			GameObject val = GameObject.Find("Ari");
    			if ((Object)(object)val != (Object)null)
    			{
    				target = val.transform;
    			}
    		}
    		if ((Object)(object)target != (Object)null)
    		{
    			_mover = ((Component)target).GetComponent<AriMover>();
    		}
    		_distance = Mathf.Clamp(startDistance, minDistance, maxDistance);
    		_pitch = Mathf.Clamp(startPitch, minPitch, maxPitch);
    		_yaw = (((Object)(object)target != (Object)null) ? target.eulerAngles.y : ((Component)this).transform.eulerAngles.y);
    	}

    	private void LateUpdate()
    	{
    		if (!((Object)(object)target == (Object)null))
    		{
    			if (_guiding)
    			{
    				GuideStep(Time.deltaTime);
    			}
    			else
    			{
    				ReadMouse();
    				AutoAlign();
    			}
    			Apply();
    		}
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
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
    		Mouse current = Mouse.current;
    		if (current != null)
    		{
    			bool isPressed = current.rightButton.isPressed;
    			if (isPressed)
    			{
    				Vector2 val = current.delta.ReadValue() * (0.06f * Time.deltaTime);
    				_yaw += val.x * 12f;
    				_pitch = Mathf.Clamp(_pitch - val.y * 12f, minPitch, maxPitch);
    			}
    			_idleSinceDrag = (isPressed ? 0f : (_idleSinceDrag + Time.deltaTime));
    			float y = current.scroll.ReadValue().y;
    			if (Mathf.Abs(y) > 0.01f)
    			{
    				_distance = Mathf.Clamp(_distance - y * 0.01f * zoomStep, minDistance, maxDistance);
    			}
    		}
    	}

    	private void AutoAlign()
    	{
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		if (!((Object)(object)_mover == (Object)null) && !(_idleSinceDrag < autoAlignDelay) && !(_mover.CurrentSpeed < autoAlignSpeed))
    		{
    			float num = target.eulerAngles.y + 180f;
    			_yaw = Mathf.MoveTowardsAngle(_yaw, num, autoAlignRate * Time.deltaTime);
    		}
    	}

    	private void Apply()
    	{
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
    		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
    		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 val = target.position + Vector3.up * pivotHeight;
    		Vector3 val2 = (_guiding ? Vector3.Lerp(val, _guideInterest, _guideBlend * guidePivotBlend) : val);
    		if (!_pivotInitialized)
    		{
    			_pivot = val2;
    			_pivotInitialized = true;
    		}
    		else
    		{
    			_pivot = Vector3.Lerp(_pivot, val2, 1f - Mathf.Exp((0f - Time.deltaTime) / Mathf.Max(0.01f, positionSmooth)));
    		}
    		Vector3 pivot = _pivot;
    		Quaternion val3 = Quaternion.Euler(_pitch, _yaw, 0f);
    		Vector3 val4 = val3 * new Vector3(0f, 0f, 0f - _distance);
    		float num = _distance;
    		RaycastHit val5 = default;
    		if (Physics.Raycast(pivot, val4.normalized, out val5, _distance, (obstructionMask), (QueryTriggerInteraction)1))
    		{
    			num = Mathf.Max(minDistance * 0.5f, val5.distance - 0.25f);
    		}
    		Vector3 val6 = pivot + val4.normalized * num;
    		((Component)this).transform.position = Vector3.SmoothDamp(((Component)this).transform.position, val6, ref _velocity, positionSmooth);
    		Quaternion val7 = val3;
    		((Component)this).transform.rotation = Quaternion.Slerp(((Component)this).transform.rotation, val7, 1f - Mathf.Exp((0f - Time.deltaTime) / rotationSmooth));
    	}

    	[ContextMenu("Snap To Default View")]
    	public void SnapToDefault()
    	{
    		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
    		_distance = Mathf.Clamp(startDistance, minDistance, maxDistance);
    		_pitch = Mathf.Clamp(startPitch, minPitch, maxPitch);
    		if ((Object)(object)target != (Object)null)
    		{
    			_yaw = target.eulerAngles.y;
    		}
    	}

    	public void Guide(Vector3 interest, float height = 1.8f, float extraMetres = 2.5f)
    	{
    		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
    		if (!((Object)(object)target == (Object)null))
    		{
    			_guiding = true;
    			_guideInterest = interest;
    			Vector3 val = Flat(interest - target.position);
    			float y;
    			if (!(val.sqrMagnitude < 0.01f))
    			{
    				Quaternion val2 = Quaternion.LookRotation(val.normalized, Vector3.up);
    				y = val2.eulerAngles.y;
    			}
    			else
    			{
    				y = target.eulerAngles.y;
    			}
    			float guideYaw = y;
    			_guideYaw = guideYaw;
    			float num = Vector3.Distance(Flat(interest), Flat(target.position));
    			_guideDistance = Mathf.Clamp(num + extraMetres, minDistance, maxDistance);
    			_guidePitch = Mathf.Clamp(Mathf.Atan2(height, Mathf.Max(0.5f, _guideDistance)) * 57.29578f, minPitch, maxPitch);
    		}
    	}

    	public void ReleaseGuide()
    	{
    		_guiding = false;
    	}

    	private static Vector3 Flat(Vector3 v)
    	{
    		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
    		v.y = 0f;
    		return v;
    	}

    	public AriFollowCamera()
    	{
    		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
    	}
    }
}