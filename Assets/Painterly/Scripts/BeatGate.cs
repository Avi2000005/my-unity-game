using System;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Beat Gate")]
    public sealed class BeatGate : MonoBehaviour
    {
    	private enum GateState
    	{
    		Closed,
    		Opening,
    		Open
    	}

    	[Header("What moves")]
    	[Tooltip("The slab. Left empty, the first child of this object.")]
    	[SerializeField]
    	private Transform slab;

    	[Tooltip("Which way the slab travels when it opens. Down, for the reason in the note above this class.")]
    	[SerializeField]
    	private Vector3 openAxis = Vector3.down;

    	[Min(0.1f)]
    	[SerializeField]
    	private float travel = 2.25f;

    	[Min(0.05f)]
    	[SerializeField]
    	private float seconds = 1.7f;

    	[Header("What opens it")]
    	[Tooltip("The switch that latches it. The director owns the decision in practice; this is only used by Reset.")]
    	[SerializeField]
    	private LatchingSwitch source;

    	private GateState _state;

    	private Vector3 _closedLocal;

    	private float _moving;

    	[SerializeField]
    	private bool log = true;

    	[Header("Reading")]
    	public bool IsClosed => _state == GateState.Closed;

    	public bool IsOpening => _state == GateState.Opening;

    	public bool IsOpen => _state == GateState.Open;

    	public float Progress { get; private set; }

    	public Collider SlabCollider
    	{
    		get
    		{
    			if ((Object)(object)slab == (Object)null)
    			{
    				return null;
    			}
    			return ((Component)slab).GetComponent<Collider>();
    		}
    	}

    	public Transform Slab => slab;

    	public event Action Opened;

    	private void Awake()
    	{
    		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)slab == (Object)null && ((Component)this).transform.childCount > 0)
    		{
    			slab = ((Component)this).transform.GetChild(0);
    		}
    		if ((Object)(object)slab == (Object)null)
    		{
    			Debug.LogWarning((object)("[Echoes] " + ((Object)this).name + ": no slab, so this gate cannot open. The setup tool checks for this."));
    		}
    		else
    		{
    			_closedLocal = slab.localPosition;
    		}
    	}

    	public void Shut()
    	{
    		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
    		if (!((Object)(object)slab == (Object)null))
    		{
    			_state = GateState.Closed;
    			Progress = 0f;
    			_moving = 0f;
    			slab.localPosition = _closedLocal;
    		}
    	}

    	public void Open()
    	{
    		if (_state == GateState.Closed)
    		{
    			_state = GateState.Opening;
    			_moving = 0f;
    		}
    	}

    	private void Update()
    	{
    		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
    		if (_state != GateState.Opening || (Object)(object)slab == (Object)null)
    		{
    			return;
    		}
    		_moving += Time.deltaTime;
    		Progress = Mathf.Clamp01((seconds <= 0f) ? 1f : (_moving / seconds));
    		float num = Progress * Progress * (3f - 2f * Progress);
    		slab.localPosition = _closedLocal + openAxis.normalized * travel * num;
    		if (Progress >= 1f)
    		{
    			_state = GateState.Open;
    			if (log)
    			{
    				Debug.Log((object)"[Echoes] gate open", (Object)(object)this);
    			}
    			if (Opened != null)
    			{
    				Opened();
    			}
    		}
    	}

    	public void ResetGate()
    	{
    		Shut();
    		if ((Object)(object)source != (Object)null)
    		{
    			source.ResetSwitch();
    		}
    	}

    	public BeatGate()
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    	}
    }
}