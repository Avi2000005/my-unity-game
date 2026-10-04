using UnityEngine;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Sleeping Tree")]
    public sealed class SleepingTree : MonoBehaviour
    {
    	public readonly struct WakeResult
    	{
    	public WakeResult(bool fired, string why, int targets, float radius)
    	{
    	    	this.Fired = fired;
    	    	this.Why = why;
    	    	this.TargetsReached = targets;
    	    	this.Radius = radius;
    	}
    		public readonly bool Fired;

    		public readonly string Why;

    		public readonly int TargetsReached;

    		public readonly float Radius;

    		public static WakeResult Already => new WakeResult(fired: false, "already awoken", 0, 0f);

    		public WakeResult Log(string detail, bool log, Object context)
    		{
    			if (Fired & log)
    			{
    				Debug.Log((object)("[Echoes] Beat 3 tree awoken — " + Why + "; burst reached " + TargetsReached + " target(s) within " + Radius.ToString("0.0") + " m. " + detail), context);
    			}
    			return this;
    		}
    	}

    	[Header("What it is")]
    	[Tooltip("The point on the tree the brush has to reach. Usually the trunk or a child transform, not the root, because the root of a kit piece is often at its base and often has a collider several metres across.")]
    	[SerializeField]
    	private Transform touchPoint;

    	[Tooltip("The tree's own colour target. NOT restored by this beat any more — Level 1 is played entirely grey and the tree only wakes Mono. Kept wired because ResetBeat still has to force it back to grey, and because a target the beat does not raise is a target whose restore value something else may have moved.")]
    	[SerializeField]
    	private ColorRestoreTarget selfColour;

    	[Header("Who is involved")]
    	[Tooltip("Left empty, found by name.")]
    	[SerializeField]
    	private MonoCompanion mono;

    	[Tooltip("The gully run Mono leads her down. Optional — the beat completes without it, and the setup tool reports if it is missing rather than leaving a silent null.")]
    	[SerializeField]
    	private MonoChase chase;

    	[Header("Touching it")]
    	[Tooltip("How close Ari must be for a stroke to count on its own. Slightly more than arm's length: the tree has a trunk and she has a 0.30 m capsule.")]
    	[Min(0.5f)]
    	[SerializeField]
    	private float touchDistance = 2.4f;

    	[Tooltip("How near the stroke has to land to count as hitting the tree, for a swing from further away. Generous, because the camera ray splay at the top of the screen is several degrees and that is metres of ground at this range.")]
    	[Min(0.5f)]
    	[SerializeField]
    	private float strokeHitDistance = 2.5f;

    	[Header("The burst")]
    	[Tooltip("Radius of the ink flash. Wider than the brush's own 6 m, because this is an event and not a stroke — the player should be able to see it from where they are standing. This is a VISUAL SIZE. It is not a radius of colour coming back; nothing in Level 1 comes back here.")]
    	[Min(1f)]
    	[SerializeField]
    	private float burstRadius;

    	[Tooltip("Play the flash at all. Off leaves the wake silent, which is not what the beat wants but is a thing to be able to check.")]
    	[SerializeField]
    	private bool burstFx;

    	[Tooltip("Seconds for the flash to fade.")]
    	[Min(0.1f)]
    	[SerializeField]
    	private float burstSeconds = 1.8f;

    	[Header("Words")]
    	[Tooltip("What Mono says when he wakes. Looked up in his line list, so a missing line is a warning in the console, not silence.")]
    	[SerializeField]
    	private string wakeLineId = "beat3.wake";

    	[Tooltip("Said immediately after the wake line. This is the colour thief, and it is the ONLY thing about the thief that Level 1 delivers: the character is not in this level, is not in it hidden, and is not coming later in this level. If this is empty he says nothing about it.")]
    	[SerializeField]
    	private string thiefLineId = "beat3.thief";

    	[Tooltip("Shown while Ari is close enough to touch it. Sticky, because it is telling her to do the one thing the beat is waiting for.")]
    	[SerializeField]
    	private string promptText = "The old tree is grey. Swing the brush at it.";

    	[Tooltip("Shown for a moment once it has woken.")]
    	[SerializeField]
    	private string doneText = "";

    	[SerializeField]
    	private float doneSeconds = 3.5f;

    	[Header("Debug")]
    	[SerializeField]
    	private bool log;

    	private BrushPainter _brush;

    	private AriMover _ari;

    	private Vector3 _point;

    	private bool _resolved;

    	private bool _awoken;

    	public bool IsAwoken => _awoken;

    	public Vector3 TouchPoint
    	{
    		get
    		{
    			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    			return _point;
    		}
    	}

    	public float BurstRadius => burstRadius;

    	public float TouchDistance => touchDistance;

    	private void OnEnable()
    	{
    		Resolve();
    		if ((Object)(object)_brush != (Object)null)
    		{
    			_brush.Stroked += OnStroked;
    		}
    	}

    	private void OnDisable()
    	{
    		if ((Object)(object)_brush != (Object)null)
    		{
    			_brush.Stroked -= OnStroked;
    		}
    	}

    	private void Reset()
    	{
    		touchPoint = ((Component)this).transform;
    	}

    	public void Resolve()
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
    		if (_point == Vector3.zero)
    		{
    			_point = (((Object)(object)touchPoint != (Object)null) ? touchPoint.position : ((Component)this).transform.position);
    		}
    		if ((Object)(object)_brush == (Object)null)
    		{
    			GameObject val = GameObject.Find("Ari");
    			if ((Object)(object)val != (Object)null)
    			{
    				_ari = val.GetComponent<AriMover>();
    				_brush = val.GetComponentInChildren<BrushPainter>();
    			}
    			if ((Object)(object)_brush == (Object)null && (Object)(object)Camera.main != (Object)null)
    			{
    				_brush = ((Component)Camera.main).GetComponent<BrushPainter>();
    			}
    		}
    		if ((Object)(object)mono == (Object)null)
    		{
    			mono = MonoCompanion.FindInLevel();
    		}
    		if ((Object)(object)chase == (Object)null)
    		{
    			GameObject val2 = GameObject.Find("MonoChase");
    			if ((Object)(object)val2 != (Object)null)
    			{
    				chase = val2.GetComponent<MonoChase>();
    			}
    		}
    		if ((Object)(object)selfColour == (Object)null)
    		{
    			selfColour = ((Component)this).GetComponentInChildren<ColorRestoreTarget>();
    		}
    		_resolved = true;
    	}

    	private void Update()
    	{
    		if (!_awoken)
    		{
    			if (!_resolved)
    			{
    				Resolve();
    			}
    			if (!((Object)(object)_brush == (Object)null) && !((Object)(object)_ari == (Object)null) && AriIsClose())
    			{
    				BeatPrompt.Show(promptText);
    			}
    		}
    	}

    	public bool AriIsClose()
    	{
    		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)_ari == (Object)null)
    		{
    			return false;
    		}
    		Vector3 val = ((Component)_ari).transform.position - _point;
    		val.y = 0f;
    		return val.sqrMagnitude <= touchDistance * touchDistance;
    	}

    	private void OnStroked(Vector3 worldPosition, int targets)
    	{
    		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		if (!_awoken)
    		{
    			if (!_resolved)
    			{
    				Resolve();
    			}
    			bool flag = Vector3.Distance(worldPosition, _point) <= strokeHitDistance;
    			bool flag2 = AriIsClose();
    			if (flag || flag2)
    			{
    				Wake("the stroke landed on the tree" + (flag2 ? " (and she was beside it)" : "")).Log("stroke at " + worldPosition.ToString("F1") + " from " + _point.ToString("F1") + ", reached " + targets + " target(s)", log, (Object)(object)this);
    			}
    		}
    	}

    	public WakeResult Wake(string why = "called directly")
    	{
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
    		if (!_resolved)
    		{
    			Resolve();
    		}
    		if (_awoken)
    		{
    			return WakeResult.Already;
    		}
    		_awoken = true;
    		int targets = 0;
    		if (burstFx)
    		{
    			BrushStrokeFx.Spawn(_point, Vector3.up, burstRadius * 0.5f);
    			targets = 1;
    		}
    		if ((Object)(object)mono != (Object)null)
    		{
    			mono.Wake();
    			mono.SayBeat(wakeLineId);
    			if (!string.IsNullOrEmpty(thiefLineId))
    			{
    				mono.SayBeatIfPresent(thiefLineId);
    			}
    		}
    		if ((Object)(object)chase != (Object)null)
    		{
    			chase.Begin();
    		}
    		BeatPrompt.Show(doneText, doneSeconds);
    		if (string.IsNullOrEmpty(doneText))
    		{
    			BeatPrompt.Clear();
    		}
    		return new WakeResult(fired: true, why, targets, burstRadius);
    	}

    	[ContextMenu("Reset Beat 3")]
    	public void ResetBeat()
    	{
    		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
    		_awoken = false;
    		_resolved = false;
    		if ((Object)(object)selfColour != (Object)null)
    		{
    			selfColour.SetRestoreImmediate(0f);
    		}
    		if ((Object)(object)mono != (Object)null)
    		{
    			mono.Unwake();
    		}
    		if ((Object)(object)chase != (Object)null)
    		{
    			chase.End();
    		}
    		ColorRestoreTarget.RestoreInRadius(_point, burstRadius, 0f, 0.2f);
    		BeatPrompt.Clear();
    		Resolve();
    	}
    }
}