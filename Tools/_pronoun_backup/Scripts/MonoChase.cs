using System;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Mono Chase")]
    public sealed class MonoChase : MonoBehaviour
    {
    	[Serializable]
    	public sealed class Leg
    	{
    		[Tooltip("A point along the gully. Empty legs are skipped and reported, not silently ignored.")]
    		public Transform point;

    		[Tooltip("How close Ari must come for this leg to count as passed.")]
    		[Min(0.5f)]
    		public float triggerRadius = 3f;

    		[Tooltip("Mono's line as she reaches this leg. Looked up in his line list; a missing id warns instead of going silent.")]
    		public string lineId = "";
    	}

    	private enum Phase
    	{
    		Idle,
    		Waiting,
    		Running,
    		Done
    	}

    	[Header("The route")]
    	[Tooltip("Corners of the gully, in order. Ari reaching the last one ends the beat. Built by Tools/Echoes/Build Beat 3.")]
    	[SerializeField]
    	private List<Leg> legs = new List<Leg>();

    	[Header("Who is running it")]
    	[Tooltip("Left empty, found by name.")]
    	[SerializeField]
    	private MonoCompanion mono;

    	[Tooltip("Left empty, taken from Camera.main.")]
    	[SerializeField]
    	private AriFollowCamera followCamera;

    	[Header("The handover")]
    	[Tooltip("Seconds between the tree waking and the run starting.\n\nThe tree calls Begin in the same frame it fires the colour burst and wakes Mono. Without a gap the camera re-frames to the gully on that exact frame, so the beat's payoff — the colour arriving — happens off-screen or half off-screen, and the first thing the player sees of Beat 3 is a running start with no idea what just happened.\n\nThis is a timer, not a wait on Mono_Wake finishing. A beat gated on an animation is a beat that stalls if the animation is re-imported, plays at a different speed, or is interrupted by the player leaving the trigger. 3.2s sits just past the trimmed 3.0s wake, but the two numbers were chosen separately and neither reads the other.")]
    	[Min(0f)]
    	[SerializeField]
    	private float handoverDelay = 3.2f;

    	[Header("Camera")]
    	[Tooltip("Metres the camera looks down from while guiding. A little higher than play, so the floor of the gully — the thing she has to run along — is in shot.")]
    	[Min(0.5f)]
    	[SerializeField]
    	private float guideHeight = 2.1f;

    	[Tooltip("Extra metres of framing beyond the measured gap to the next corner. The gap exactly is as tight as it can be and clips the near character on the way round.")]
    	[Min(0f)]
    	[SerializeField]
    	private float guideMargin = 3f;

    	[Header("Words")]
    	[Tooltip("Said when the run starts, before the first corner.")]
    	[SerializeField]
    	private string startLineId = "beat3.follow";

    	[Tooltip("Shown while the run is going, sticky so it cannot time out under the player.")]
    	[SerializeField]
    	private string promptText = "Stay with Mono.";

    	[Tooltip("Shown briefly when the run ends.")]
    	[SerializeField]
    	private string doneText = "";

    	[SerializeField]
    	private float doneSeconds = 3f;

    	[Header("Debug")]
    	[SerializeField]
    	private bool log = true;

    	private Phase _phase;

    	private int _nextLeg;

    	private AriMover _ari;

    	private bool _resolved;

    	private float _handoverLeft;

    	public string PhaseName => _phase.ToString();

    	public float HandoverLeft
    	{
    		get
    		{
    			if (_phase != Phase.Waiting)
    			{
    				return 0f;
    			}
    			return Mathf.Max(0f, _handoverLeft);
    		}
    	}

    	public float HandoverSeconds => handoverDelay;

    	public int LegsLeft => Mathf.Max(0, legs.Count - _nextLeg);

    	public int LegsTotal => legs.Count;

    	public float DistanceToNextLeg
    	{
    		get
    		{
    			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
    			Leg leg = NextLeg();
    			if (leg == null || (Object)(object)leg.point == (Object)null || (Object)(object)_ari == (Object)null)
    			{
    				return -1f;
    			}
    			return Vector3.Distance(((Component)_ari).transform.position, leg.point.position);
    		}
    	}

    	private Leg NextLeg()
    	{
    		for (int i = _nextLeg; i < legs.Count; i++)
    		{
    			if (legs[i] != null && (Object)(object)legs[i].point != (Object)null)
    			{
    				return legs[i];
    			}
    		}
    		return null;
    	}

    	private void OnEnable()
    	{
    		Resolve();
    	}

    	private void Reset()
    	{
    		legs = new List<Leg>();
    	}

    	public void Resolve()
    	{
    		if ((Object)(object)_ari == (Object)null)
    		{
    			GameObject val = GameObject.Find("Ari");
    			if ((Object)(object)val != (Object)null)
    			{
    				_ari = val.GetComponent<AriMover>();
    			}
    		}
    		if ((Object)(object)mono == (Object)null)
    		{
    			mono = MonoCompanion.FindInLevel();
    		}
    		if ((Object)(object)followCamera == (Object)null && (Object)(object)Camera.main != (Object)null)
    		{
    			followCamera = ((Component)Camera.main).GetComponent<AriFollowCamera>();
    		}
    		_resolved = true;
    	}

    	public void Begin()
    	{
    		if (!_resolved)
    		{
    			Resolve();
    		}
    		if (_phase != Phase.Running && _phase != Phase.Waiting)
    		{
    			_phase = Phase.Waiting;
    			_nextLeg = 0;
    			_handoverLeft = handoverDelay;
    			if (log)
    			{
    				Debug.Log((object)("[Echoes] Beat 3 handover begun, " + handoverDelay.ToString("0.00") + "s, " + LegsTotal + " leg(s)"), (Object)(object)this);
    			}
    		}
    	}

    	private void StartRun()
    	{
    		_phase = Phase.Running;
    		_handoverLeft = 0f;
    		if ((Object)(object)mono != (Object)null && !string.IsNullOrEmpty(startLineId))
    		{
    			mono.SayBeat(startLineId);
    		}
    		if (!string.IsNullOrEmpty(promptText))
    		{
    			BeatPrompt.Show(promptText);
    		}
    		if (log)
    		{
    			Debug.Log((object)("[Echoes] Beat 3 chase begun with " + LegsTotal + " leg(s)"), (Object)(object)this);
    		}
    	}

    	public void End()
    	{
    		if (_phase != Phase.Idle && _phase != Phase.Done)
    		{
    			_phase = Phase.Done;
    			_handoverLeft = 0f;
    			if ((Object)(object)followCamera != (Object)null)
    			{
    				followCamera.ReleaseGuide();
    			}
    			if (!string.IsNullOrEmpty(doneText))
    			{
    				BeatPrompt.Show(doneText, doneSeconds);
    			}
    			else
    			{
    				BeatPrompt.Clear();
    			}
    			if (log)
    			{
    				Debug.Log((object)("[Echoes] Beat 3 chase ended after " + _nextLeg + " leg(s)"), (Object)(object)this);
    			}
    		}
    	}

    	[ContextMenu("Reset Chase")]
    	public void ResetChase()
    	{
    		if ((Object)(object)followCamera != (Object)null)
    		{
    			followCamera.ReleaseGuide();
    		}
    		_phase = Phase.Idle;
    		_nextLeg = 0;
    		_handoverLeft = 0f;
    		BeatPrompt.Clear();
    	}

    	private void Update()
    	{
    		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
    		if (_phase == Phase.Waiting)
    		{
    			_handoverLeft -= Time.deltaTime;
    			if (!(_handoverLeft > 0f))
    			{
    				StartRun();
    			}
    		}
    		else
    		{
    			if (_phase != Phase.Running)
    			{
    				return;
    			}
    			if (!_resolved)
    			{
    				Resolve();
    			}
    			if ((Object)(object)_ari == (Object)null)
    			{
    				return;
    			}
    			while (true)
    			{
    				Leg leg = NextLeg();
    				if (leg == null || Vector3.Distance(((Component)_ari).transform.position, leg.point.position) > leg.triggerRadius)
    				{
    					break;
    				}
    				if ((Object)(object)mono != (Object)null && !string.IsNullOrEmpty(leg.lineId))
    				{
    					mono.SayBeat(leg.lineId);
    				}
    				_nextLeg = legs.IndexOf(leg) + 1;
    			}
    			Leg leg2 = NextLeg();
    			if (leg2 == null)
    			{
    				End();
    			}
    			else if ((Object)(object)followCamera != (Object)null)
    			{
    				followCamera.Guide(leg2.point.position, guideHeight, guideMargin);
    			}
    		}
    	}

    	public void SetLegs(List<Leg> route)
    	{
    		legs = route ?? new List<Leg>();
    	}
    }
}