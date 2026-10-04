using UnityEngine;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Ink Crawler Emerge")]
    [RequireComponent(typeof(InkCrawler))]
    public sealed class InkCrawlerEmerge : MonoBehaviour, IResettable
    {
    	public enum Phase
    	{
    		Submerged,
    		Rising,
    		Up
    	}

    	[Header("Timing")]
    	[Tooltip("Seconds from fully under to standing. Long enough to see, short enough that the beat does not stop and wait.")]
    	[Min(0.05f)]
    	[SerializeField]
    	private float emergeSeconds = 1.1f;

    	[Tooltip("Seconds to sink again on a reset. Shorter than the rise, because a retry should not make the player watch three crawlers disappear slowly.")]
    	[Min(0.05f)]
    	[SerializeField]
    	private float sinkSeconds = 0.45f;

    	[Tooltip("How deep under the floor to put it. Zero means measure it from the collider, which is what stops a crawler being only half-sunk by a number that was typed for a different model.")]
    	[Min(0f)]
    	[SerializeField]
    	private float extraDepth = 0.35f;

    	[Header("Behaviour")]
    	[Tooltip("Start under the floor. Off is for a crawler that is meant to be standing there when the level opens.")]
    	[SerializeField]
    	private bool startSubmerged = true;

    	[Tooltip("Put the collider back at this fraction of the rise rather than at the top. Slightly early on purpose: a crawler that is already standing but cannot be hit for a quarter of a second reads as a bug in the stagger rule, which is the one rule this beat is teaching.")]
    	[Range(0f, 1f)]
    	[SerializeField]
    	private float colliderAt = 0.6f;

    	[SerializeField]
    	private bool log = true;

    	private InkCrawler _crawler;

    	private Collider[] _colliders;

    	private Renderer[] _renderers;

    	private Vector3 _rest;

    	private bool _restCaptured;

    	private float _t;

    	private float _leg;

    	private float _depth;

    	private int _rises;

    	public Phase Now { get; private set; } = Phase.Up;

    	public bool IsReady => Now == Phase.Up;

    	public float EmergeSeconds => emergeSeconds;

    	public Vector3 RestPoint
    	{
    		get
    		{
    			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    			return _rest;
    		}
    	}

    	public Vector3 StandPoint
    	{
    		get
    		{
    			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
    			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    			if (!_restCaptured)
    			{
    				return ((Component)this).transform.position;
    			}
    			return _rest;
    		}
    	}

    	private void Awake()
    	{
    		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
    		_crawler = ((Component)this).GetComponent<InkCrawler>();
    		_colliders = ((Component)this).GetComponentsInChildren<Collider>(true);
    		_renderers = ((Component)this).GetComponentsInChildren<Renderer>(true);
    		_rest = ((Component)this).transform.position;
    		_depth = Depth();
    		if (startSubmerged)
    		{
    			Submerge(silent: true);
    		}
    	}

    	private float Depth()
    	{
    		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
    		float num = 0f;
    		SkinnedMeshRenderer componentInChildren = ((Component)this).GetComponentInChildren<SkinnedMeshRenderer>(true);
    		if ((Object)(object)componentInChildren != (Object)null)
    		{
    			float num2 = SkinHeight.Measure((Renderer)(object)componentInChildren, out var _, out var _);
    			if (num2 > 0.0001f)
    			{
    				num = num2;
    			}
    		}
    		if (num <= 0.0001f)
    		{
    			for (int i = 0; i < _colliders.Length; i++)
    			{
    				if (!((Object)(object)_colliders[i] == (Object)null))
    				{
    					float num3 = num;
    					Bounds bounds = _colliders[i].bounds;
    					num = Mathf.Max(num3, bounds.size.y);
    				}
    			}
    			if (num > 0.0001f && log)
    			{
    				Debug.LogWarning((object)("[Echoes] " + ((Object)this).name + " has no measurable skin, so its sink depth is taken from the collider (" + num.ToString("0.00") + " m). It is sinking a box, not the thing the player sees."), (Object)(object)this);
    			}
    		}
    		return num + Mathf.Max(0f, extraDepth);
    	}

    	public void Submerge(bool silent = false)
    	{
    		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		if (!_restCaptured)
    		{
    			_rest = ((Component)this).transform.position;
    			_restCaptured = true;
    		}
    		_depth = Depth();
    		Now = Phase.Submerged;
    		_t = 0f;
    		Vector3 rest = _rest;
    		rest.y -= _depth;
    		((Component)this).transform.position = rest;
    		SetSolid(on: false);
    		if (!silent && log)
    		{
    			Debug.Log((object)("[Echoes] " + ((Object)this).name + " sunk " + _depth.ToString("0.00") + " m"), (Object)(object)this);
    		}
    	}

    	public void Emerge()
    	{
    		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
    		if (Now != Phase.Up)
    		{
    			_rest = new Vector3(((Component)this).transform.position.x, _rest.y, ((Component)this).transform.position.z);
    			Now = Phase.Rising;
    			_t = 0f;
    			_rises++;
    			if (log)
    			{
    				Debug.Log((object)("[Echoes] " + ((Object)this).name + " rising, rise " + _rises), (Object)(object)this);
    			}
    		}
    	}

    	private void Update()
    	{
    		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
    		if (Now != Phase.Rising)
    		{
    			return;
    		}
    		_t += Time.deltaTime / Mathf.Max(0.01f, emergeSeconds);
    		_leg = Mathf.Clamp01(_t);
    		float num = 1f - (1f - _leg) * (1f - _leg);
    		Vector3 position = ((Component)this).transform.position;
    		position.y = Mathf.Lerp(_rest.y - _depth, _rest.y, num);
    		((Component)this).transform.position = position;
    		if (_leg >= colliderAt && !Solid())
    		{
    			SetSolid(on: true);
    		}
    		if (!(_leg < 1f))
    		{
    			((Component)this).transform.position = _rest;
    			Now = Phase.Up;
    			SetSolid(on: true);
    			if (log)
    			{
    				Debug.Log((object)("[Echoes] " + ((Object)this).name + " up at " + _rest.ToString("F2")), (Object)(object)this);
    			}
    		}
    	}

    	private bool Solid()
    	{
    		if (_colliders != null && _colliders.Length != 0)
    		{
    			return _colliders[0].enabled;
    		}
    		return false;
    	}

    	private void SetSolid(bool on)
    	{
    		for (int i = 0; i < _colliders.Length; i++)
    		{
    			if ((Object)(object)_colliders[i] != (Object)null)
    			{
    				_colliders[i].enabled = on;
    			}
    		}
    		for (int j = 0; j < _renderers.Length; j++)
    		{
    			if ((Object)(object)_renderers[j] != (Object)null)
    			{
    				_renderers[j].enabled = on;
    			}
    		}
    	}

    	public void ResetForCheckpoint()
    	{
    		Submerge(silent: true);
    		_rises = 0;
    	}
    }
}