using System;
using UnityEngine;

using Object = UnityEngine.Object;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Brush Painter")]
    public sealed class BrushPainter : MonoBehaviour
    {
    	[Header("Brush")]
    	[Tooltip("Radius in metres around the stroke that regains colour.")]
    	[Min(0.1f)]
    	[SerializeField]
    	private float radius = 6f;

    	[Tooltip("Seconds for colour to flow back in.")]
    	[Min(0f)]
    	[SerializeField]
    	private float strokeDuration = 1.2f;

    	[Tooltip("Cooldown between strokes, per the combat loop design.")]
    	[Min(0f)]
    	[SerializeField]
    	private float cooldown = 0.6f;

    	[Header("Test mode")]
    	[Tooltip("Paint with the left mouse button. Turn this off once Ari can drive the brush himself.")]
    	[SerializeField]
    	private bool testMode = true;

    	[Tooltip("Camera used for test-mode aiming. Falls back to Camera.main.")]
    	[SerializeField]
    	private Camera aimCamera;

    	[SerializeField]
    	private float maxRayDistance = 250f;

    	[Header("Feedback")]
    	[Tooltip("Ari. Found on this object or in his children if left empty. He swings on a stroke, because a click that repaints the world while he stands still reads as the mouse doing something rather than as a brush stroke.")]
    	[SerializeField]
    	private AriMover ari;

    	[Tooltip("Show a mark where the stroke lands. The colour coming back is measured at about 7.6% of full range on a dull surface, which is close to invisible, so without this the player has no way of knowing the click registered.")]
    	[SerializeField]
    	private bool showStrokeMark = true;

    	[Tooltip("Radius of the mark, as a fraction of the stroke radius.")]
    	[Range(0.1f, 2f)]
    	[SerializeField]
    	private float markScale = 1f;

    	[Header("Debug keys")]
    	[Tooltip("G greys the whole world again, R restores everything. Useful for A/B.")]
    	[SerializeField]
    	private bool debugKeys = true;

    	private float _nextStrokeTime;

    	private Vector3 _lastNormal = Vector3.up;

    	public float Radius => radius;

    	public bool CanSwing => CanStroke();

    	public event Action<Vector3, int> Stroked;

    	public int PaintAt(Vector3 worldPosition, float overrideRadius = -1f, float overrideDuration = -1f)
    	{
    		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
    		float num = ((overrideRadius > 0f) ? overrideRadius : radius);
    		return ColorRestoreTarget.RestoreInRadius(worldPosition, num, 1f, overrideDuration);
    	}

    	public int PaintFromScreenPoint(Vector3 screenPosition)
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
    		Vector3? val = ResolveScreenPoint(screenPosition);
    		if (!val.HasValue)
    		{
    			return 0;
    		}
    		return PaintAt(val.Value);
    	}

    	private Vector3? ResolveScreenPoint(Vector3 screenPosition)
    	{
    		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
    		Camera val = (((Object)(object)aimCamera != (Object)null) ? aimCamera : Camera.main);
    		if ((Object)(object)val == (Object)null)
    		{
    			return null;
    		}
    		RaycastHit val2 = default;
    		if (!Physics.Raycast(val.ScreenPointToRay(screenPosition), out val2, maxRayDistance))
    		{
    			return null;
    		}
    		_lastNormal = val2.normal;
    		return val2.point;
    	}

    	private void Update()
    	{
    		if (ari != null && ari.Frozen) return;
    		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
    		if (testMode)
    		{
    			Mouse current = Mouse.current;
    			if (current != null && current.leftButton.wasPressedThisFrame)
    			{
    				Vector3? val = ResolveScreenPoint((current.position.ReadValue()));
    				if (val.HasValue && TryStroke(val.Value))
    				{
    					Debug.Log((object)$"[Brush] stroke, radius {radius}", (Object)(object)this);
    				}
    			}
    		}
    		if (!debugKeys)
    		{
    			return;
    		}
    		Keyboard current2 = Keyboard.current;
    		if (current2 == null)
    		{
    			return;
    		}
    		if (current2.gKey.wasPressedThisFrame)
    		{
    			ColorRestoreTarget.SetAllImmediate(0f);
    			Debug.Log((object)"[Brush] world greyed");
    		}
    		if (!current2.rKey.wasPressedThisFrame)
    		{
    			return;
    		}
    		int num = 0;
    		foreach (ColorRestoreTarget item in ColorRestoreTarget.AllActive)
    		{
    			if ((Object)(object)item != (Object)null)
    			{
    				item.RestoreTo(1f, strokeDuration);
    				num++;
    			}
    		}
    		Debug.Log((object)$"[Brush] restored {num} target(s)");
    	}

    	private bool CanStroke()
    	{
    		return Time.time >= _nextStrokeTime;
    	}

    	private void OnValidate()
    	{
    		radius = Mathf.Max(0.1f, radius);
    		strokeDuration = Mathf.Max(0f, strokeDuration);
    		cooldown = Mathf.Max(0f, cooldown);
    	}

    	public bool TryStroke(Vector3 worldPosition)
    	{
    		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
    		if (!CanStroke())
    		{
    			return false;
    		}
    		_nextStrokeTime = Time.time + cooldown;
    		int arg = PaintAt(worldPosition);
    		FeelStroke(worldPosition);
    		Stroked?.Invoke(worldPosition, arg);
    		return true;
    	}

    	private void FeelStroke(Vector3 worldPosition)
    	{
    		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)ari == (Object)null)
    		{
    			ari = FindAri();
    		}
    		if ((Object)(object)ari != (Object)null)
    		{
    			ari.PlaySwing();
    		}
    		if (showStrokeMark)
    		{
    			BrushStrokeFx.Spawn(worldPosition, _lastNormal, radius * markScale);
    		}
    	}

    	private AriMover FindAri()
    	{
    		AriMover componentInParent = ((Component)this).GetComponentInParent<AriMover>(true);
    		if ((Object)(object)componentInParent != (Object)null)
    		{
    			return componentInParent;
    		}
    		Transform[] array = Object.FindObjectsByType<Transform>((FindObjectsInactive)1);
    		foreach (Transform val in array)
    		{
    			if (((Object)val).name == "Ari")
    			{
    				AriMover componentInChildren = ((Component)val).GetComponentInChildren<AriMover>(true);
    				if ((Object)(object)componentInChildren != (Object)null)
    				{
    					return componentInChildren;
    				}
    			}
    		}
    		return null;
    	}

    	public BrushPainter()
    	{
    		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
    	}
    }
}