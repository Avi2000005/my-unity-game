using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{

    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu("Echoes/Color Restore Target")]
    public sealed class ColorRestoreTarget : MonoBehaviour
    {
    	public static readonly int ColorRestoreId = Shader.PropertyToID("_ColorRestore");

    	public static readonly int RestoreBoostId = Shader.PropertyToID("_RestoreBoost");

    	private static List<ColorRestoreTarget> _active;

    	private static MaterialPropertyBlock _block;

    	[Header("State")]
    	[Tooltip("0 = fully grey, 1 = full colour. The Grey Realm starts at 0.")]
    	[Range(0f, 1f)]
    	[SerializeField]
    	private float startRestore;

    	[Tooltip("Seconds for a full grey-to-colour transition.")]
    	[Min(0f)]
    	[SerializeField]
    	private float duration = 1.5f;

    	[Header("Flare")]
    	[Tooltip("How much the restored colour overshoots the moment it lands, so a fresh stroke reads as a flare before settling. 1 = no flare.")]
    	[Range(1f, 3f)]
    	[SerializeField]
    	private float boostAmount = 1.7f;

    	[Tooltip("Seconds for the flare to settle back to normal.")]
    	[Min(0f)]
    	[SerializeField]
    	private float boostDuration = 0.7f;

    	private Renderer[] _renderers;

    	private float _from;

    	private float _to;

    	private float _restoreTimer;

    	private float _restoreLength;

    	private bool _restoring;

    	private float _boostTimer;

    	private bool _boosting;

    	private bool _dirty = true;

    	private Bounds _worldBounds;

    	private bool _boundsValid;

    	[Tooltip("Colour returns to this even while the level is sealed. The fountain is the only thing in Level 1 that should have one.")]
    	[SerializeField]
    	private bool exemptFromSeal;

    	private static List<ColorRestoreTarget> Active => _active ?? (_active = new List<ColorRestoreTarget>());

    	private static MaterialPropertyBlock Block
    	{
    		get
    		{
    			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
    			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0014: Expected Obj, but got Unknown
    			MaterialPropertyBlock val = _block;
    			if (val == null)
    			{
    				MaterialPropertyBlock val2 = new MaterialPropertyBlock();
    				_block = val2;
    				val = val2;
    			}
    			return val;
    		}
    	}

    	public Bounds WorldBounds
    	{
    		get
    		{
    			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    			if (!_boundsValid)
    			{
    				RebuildBounds();
    			}
    			return _worldBounds;
    		}
    	}

    	public Vector3 Center
    	{
    		get
    		{
    			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    			return ((Component)this).transform.position;
    		}
    	}

    	public float Restore => Mathf.Lerp(_from, _to, RestoreT);

    	private float RestoreT
    	{
    		get
    		{
    			if (!(_restoreLength <= 0f))
    			{
    				return Mathf.Clamp01(_restoreTimer / _restoreLength);
    			}
    			return 1f;
    		}
    	}

    	public static IReadOnlyList<ColorRestoreTarget> AllActive => Active;

    	public static bool Sealed { get; private set; } = true;

    	public bool Exempt => exemptFromSeal;

    	private void RebuildBounds()
    	{
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
    		if (_renderers == null || _renderers.Length == 0)
    		{
    			_worldBounds = new Bounds(((Component)this).transform.position, Vector3.zero);
    			_boundsValid = true;
    			return;
    		}
    		bool flag = true;
    		for (int i = 0; i < _renderers.Length; i++)
    		{
    			Renderer val = _renderers[i];
    			if (!((Object)(object)val == (Object)null))
    			{
    				if (flag)
    				{
    					_worldBounds = val.bounds;
    					flag = false;
    				}
    				else
    				{
    					_worldBounds.Encapsulate(val.bounds);
    				}
    			}
    		}
    		if (flag)
    		{
    			_worldBounds = new Bounds(((Component)this).transform.position, Vector3.zero);
    		}
    		_boundsValid = true;
    	}

    	public void GrantExemption()
    	{
    		if (exemptFromSeal)
    		{
    			Debug.Log((object)("[Echoes] '" + ((Object)this).name + "' is already exempt from the colour gate. A second caller just granted the same exemption, which means two scripts think this target is theirs."), (Object)(object)this);
    		}
    		else
    		{
    			exemptFromSeal = true;
    		}
    	}

    	public static void Tally(out int sealedCount, out int exemptCount)
    	{
    		sealedCount = 0;
    		exemptCount = 0;
    		for (int i = 0; i < Active.Count; i++)
    		{
    			ColorRestoreTarget colorRestoreTarget = Active[i];
    			if (!((Object)(object)colorRestoreTarget == (Object)null))
    			{
    				if (colorRestoreTarget.exemptFromSeal)
    				{
    					exemptCount++;
    				}
    				else
    				{
    					sealedCount++;
    				}
    			}
    		}
    	}

    	public static void SetSealed(bool sealedNow, float wipeSeconds = 0.4f)
    	{
    		Sealed = sealedNow;
    		if (!sealedNow)
    		{
    			return;
    		}
    		SetAllImmediate(0f);
    		for (int i = 0; i < Active.Count; i++)
    		{
    			ColorRestoreTarget colorRestoreTarget = Active[i];
    			if (!((Object)(object)colorRestoreTarget == (Object)null) && !colorRestoreTarget.Exempt)
    			{
    				colorRestoreTarget.RestoreTo(0f, wipeSeconds);
    			}
    		}
    	}

    	private void Awake()
    	{
    		_renderers = CollectOwnedRenderers();
    		_from = (_to = Mathf.Clamp01(startRestore));
    		_dirty = true;
    		RebuildBounds();
    	}

    	private Renderer[] CollectOwnedRenderers()
    	{
    		Renderer[] componentsInChildren = ((Component)this).GetComponentsInChildren<Renderer>(true);
    		List<Renderer> list = new List<Renderer>(componentsInChildren.Length);
    		foreach (Renderer val in componentsInChildren)
    		{
    			if (!((Object)(object)val == (Object)null))
    			{
    				ColorRestoreTarget componentInParent = ((Component)val).GetComponentInParent<ColorRestoreTarget>();
    				if (!((Object)(object)componentInParent != (Object)null) || !((Object)(object)componentInParent != (Object)(object)this))
    				{
    					list.Add(val);
    				}
    			}
    		}
    		return list.ToArray();
    	}

    	private void OnEnable()
    	{
    		Active.Add(this);
    	}

    	private void OnDisable()
    	{
    		Active.Remove(this);
    	}

    	private void Update()
    	{
    		if (_restoring)
    		{
    			_restoreTimer += Time.deltaTime;
    			if (_restoreLength <= 0f || _restoreTimer >= _restoreLength)
    			{
    				_restoreTimer = _restoreLength;
    				_from = _to;
    				_restoring = false;
    			}
    			_dirty = true;
    		}
    		if (_boosting)
    		{
    			_boostTimer += Time.deltaTime;
    			if (boostDuration <= 0f || _boostTimer >= boostDuration)
    			{
    				_boosting = false;
    			}
    			_dirty = true;
    		}
    		if (_dirty)
    		{
    			_dirty = false;
    			Push();
    		}
    	}

    	public void SetRestoreImmediate(float value)
    	{
    		_from = (_to = Mathf.Clamp01(value));
    		_restoring = false;
    		_boosting = false;
    		_dirty = true;
    		Push();
    	}

    	public void RestoreTo(float value, float overrideDuration = -1f)
    	{
    		value = Mathf.Clamp01(value);
    		if (!Sealed || exemptFromSeal || !(value > Restore + 0.0005f))
    		{
    			if (value > _to + 0.001f && boostAmount > 1f)
    			{
    				_boosting = true;
    				_boostTimer = 0f;
    			}
    			_from = Restore;
    			_to = value;
    			_restoreLength = ((overrideDuration >= 0f) ? overrideDuration : duration);
    			_restoreTimer = 0f;
    			_restoring = true;
    			_dirty = true;
    			Push();
    		}
    	}

    	private void Push()
    	{
    		if (_renderers == null)
    		{
    			_renderers = CollectOwnedRenderers();
    		}
    		if (_renderers == null)
    		{
    			return;
    		}
    		float restore = Restore;
    		float num = 1f;
    		if (_boosting)
    		{
    			float num2 = ((boostDuration <= 0f) ? 1f : Mathf.Clamp01(_boostTimer / boostDuration));
    			num = Mathf.Lerp(boostAmount, 1f, 1f - (1f - num2) * (1f - num2));
    		}
    		for (int i = 0; i < _renderers.Length; i++)
    		{
    			Renderer val = _renderers[i];
    			if (!((Object)(object)val == (Object)null))
    			{
    				val.GetPropertyBlock(Block);
    				Block.SetFloat(ColorRestoreId, restore);
    				Block.SetFloat(RestoreBoostId, num);
    				val.SetPropertyBlock(Block);
    			}
    		}
    	}

    	public static int RestoreInRadius(Vector3 point, float radius, float target = 1f, float overrideDuration = -1f)
    	{
    		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
    		float num = radius * radius;
    		int num2 = 0;
    		for (int i = 0; i < Active.Count; i++)
    		{
    			ColorRestoreTarget colorRestoreTarget = Active[i];
    			if (!((Object)(object)colorRestoreTarget == (Object)null))
    			{
    				Bounds worldBounds = colorRestoreTarget.WorldBounds;
    				if (worldBounds.SqrDistance(point) <= num && (!Sealed || colorRestoreTarget.Exempt || !(target > colorRestoreTarget.Restore + 0.0005f)))
    				{
    					colorRestoreTarget.RestoreTo(target, overrideDuration);
    					num2++;
    				}
    			}
    		}
    		return num2;
    	}

    	public static void SetAllImmediate(float value)
    	{
    		for (int i = 0; i < Active.Count; i++)
    		{
    			ColorRestoreTarget colorRestoreTarget = Active[i];
    			if (!((Object)(object)colorRestoreTarget == (Object)null) && (!Sealed || colorRestoreTarget.exemptFromSeal || !(value > colorRestoreTarget.Restore + 0.0005f)))
    			{
    				colorRestoreTarget.SetRestoreImmediate(value);
    			}
    		}
    	}
    }
}