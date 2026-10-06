using System;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Beat 4 Director")]
    public sealed class Beat4Director : MonoBehaviour
    {
    	public enum Phase
    	{
    		Idle,
    		Blocked,
    		Holding,
    		Waiting,
    		Opening,
    		Done
    	}

    	[Header("The parts")]
    	[SerializeField]
    	private BeatGate gate;

    	[SerializeField]
    	private HoldLever lever;

    	[SerializeField]
    	private LatchingSwitch latchingSwitch;

    	[Header("Who")]
    	[Tooltip("Ari. Found by name if left empty.")]
    	[SerializeField]
    	private AriMover ari;

    	[Tooltip("Mono. Found by name if left empty.")]
    	[SerializeField]
    	private MonoCompanion mono;

    	[Header("Where Mono is sent")]
    	[Tooltip("Must be inside the crawlspace, out of Ari's sight down the tunnel, and within the switch's reach.")]
    	[SerializeField]
    	private Transform errandPoint;

    	[Header("Waiting on the last beat")]
    	[Tooltip("Beat 3's chase. If set, this beat does not begin until the chase reports Done — otherwise the gate's notice radius catches Ari while she is still running the gully, two metres short of it, and the beat fires mid-chase.")]
    	[SerializeField]
    	private MonoChase chase;

    	[Header("Ranges, flat on the ground")]
    	[Tooltip("How close she must be to the gate before the beat notices her. Kept small because the gully's mouth is only two metres from the gate.")]
    	[Min(1f)]
    	[SerializeField]
    	private float noticeRange = 2.6f;

    	[Tooltip("How far past the gate she must be before the beat counts as finished. Wider than her capsule so she cannot trip the end by brushing the threshold.")]
    	[Min(1f)]
    	[SerializeField]
    	private float throughRange = 2f;

    	private bool _sent;

    	private bool _finished;

    	private Phase _last;

    	[Header("Reading")]
    	public Phase Current { get; private set; }

    	public int LeverSlips { get; private set; }

    	public float PhaseSeconds { get; private set; }

    	public event Action Finished;

    	private void Awake()
    	{
    		if ((Object)(object)ari == (Object)null)
    		{
    			GameObject val = GameObject.Find("Ari");
    			if ((Object)(object)val != (Object)null)
    			{
    				ari = val.GetComponent<AriMover>();
    			}
    		}
    		if ((Object)(object)mono == (Object)null)
    		{
    			GameObject val2 = GameObject.Find("Mono");
    			if ((Object)(object)val2 != (Object)null)
    			{
    				mono = val2.GetComponent<MonoCompanion>();
    			}
    		}
    	}

    	private void OnEnable()
    	{
    		if ((Object)(object)latchingSwitch != (Object)null)
    		{
    			latchingSwitch.Threw += OnSwitchThrew;
    		}
    	}

    	private void OnDisable()
    	{
    		if ((Object)(object)latchingSwitch != (Object)null)
    		{
    			latchingSwitch.Threw -= OnSwitchThrew;
    		}
    	}

    	private void OnSwitchThrew()
    	{
    		if (!((Object)(object)gate == (Object)null) && !gate.IsOpen && !gate.IsOpening)
    		{
    			gate.Open();
    			SetPhase(Phase.Opening);
    			Prompt("The gate is lifting. Go on, then — I am right behind you.");
    			mono.SayBeat("beat4.open");
    		}
    	}

    	private void Update()
    	{
    		PhaseSeconds += Time.deltaTime;
    		if (Current != _last)
    		{
    			_last = Current;
    		}
    		if ((Object)(object)latchingSwitch != (Object)null && latchingSwitch.Thrown && (Object)(object)gate != (Object)null && gate.IsClosed)
    		{
    			OnSwitchThrew();
    		}
    		if (!_finished && PreviousBeatDone())
    		{
    			switch (Current)
    			{
    			case Phase.Idle:
    				DoIdle();
    				break;
    			case Phase.Blocked:
    				DoBlocked();
    				break;
    			case Phase.Holding:
    			case Phase.Waiting:
    				DoLever();
    				break;
    			case Phase.Opening:
    				DoOpening();
    				break;
    			}
    		}
    	}

    	private bool PreviousBeatDone()
    	{
    		if ((Object)(object)chase == (Object)null)
    		{
    			return true;
    		}
    		return chase.PhaseName == "Done";
    	}

    	private void DoIdle()
    	{
    		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
    		if (!((Object)(object)gate == (Object)null) && !((Object)(object)ari == (Object)null) && !(Flat(((Component)ari).transform.position, ((Component)gate).transform.position) > noticeRange))
    		{
    			SetPhase(Phase.Blocked);
    			Prompt("The gate has no handle on this side. But something down here is still connected to it.");
    		}
    	}

    	private void DoBlocked()
    	{
    		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
    		if (!((Object)(object)lever == (Object)null))
    		{
    			if (lever.Held)
    			{
    				SetPhase(Phase.Holding);
    				Prompt("Hold it there. I will find whatever it opens.");
    				mono.SayBeat("beat4.hold");
    			}
    			else if (!((Object)(object)gate == (Object)null) && !((Object)(object)ari == (Object)null) && Flat(((Component)ari).transform.position, ((Component)lever).transform.position) > 6f)
    			{
    				Prompt("There is a plate on the ground by the wall. Step on it and hold it down.");
    			}
    		}
    	}

    	private void DoLever()
    	{
    		if ((Object)(object)lever == (Object)null || (Object)(object)latchingSwitch == (Object)null || (Object)(object)gate == (Object)null)
    		{
    			return;
    		}
    		bool held = lever.Held;
    		if (latchingSwitch.Thrown)
    		{
    			return;
    		}
    		if (!held)
    		{
    			LeverSlips++;
    			SetPhase(Phase.Blocked);
    			Prompt("The lever sprang back up the moment you stepped off.", 2.6f);
    			return;
    		}
    		SetPhase(Phase.Waiting);
    		if (!((Object)(object)mono == (Object)null) && !mono.OnErrand && !latchingSwitch.MonoInReach)
    		{
    			if (!_sent)
    			{
    				_sent = true;
    			}
    			if ((Object)(object)errandPoint != (Object)null)
    			{
    				mono.SendTo(errandPoint);
    			}
    			else
    			{
    				Debug.LogWarning((object)("[Echoes] " + ((Object)this).name + ": no errand point, so Mono will never reach the switch."));
    			}
    		}
    	}

    	private void DoOpening()
    	{
    		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)gate == (Object)null || (Object)(object)ari == (Object)null || !gate.IsOpen)
    		{
    			return;
    		}
    		if (Flat(((Component)ari).transform.position, ((Component)gate).transform.position) < throughRange)
    		{
    			Prompt("This way is dry. Come on.");
    			return;
    		}
    		_finished = true;
    		SetPhase(Phase.Done);
    		if ((Object)(object)mono != (Object)null)
    		{
    			mono.Recall();
    		}
    		mono.SayBeat("beat4.after");
    		if (Finished != null)
    		{
    			Finished();
    		}
    	}

    	private void SetPhase(Phase p)
    	{
    		if (Current != p)
    		{
    			Current = p;
    			PhaseSeconds = 0f;
    		}
    	}

    	private void Prompt(string text, float seconds = 0f)
    	{
    		BeatPrompt.Show(text, seconds);
    	}

    	private static float Flat(Vector3 a, Vector3 b)
    	{
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		a.y = 0f;
    		b.y = 0f;
    		return Vector3.Distance(a, b);
    	}

    	[ContextMenu("Beat 4 / reset")]
    	public void ResetBeat()
    	{
    		_finished = false;
    		_sent = false;
    		LeverSlips = 0;
    		Current = Phase.Idle;
    		PhaseSeconds = 0f;
    		if ((Object)(object)gate != (Object)null)
    		{
    			gate.ResetGate();
    		}
    		else if ((Object)(object)latchingSwitch != (Object)null)
    		{
    			latchingSwitch.ResetSwitch();
    		}
    		if ((Object)(object)lever != (Object)null)
    		{
    			lever.ResetLever();
    		}
    		if ((Object)(object)mono != (Object)null)
    		{
    			mono.Recall();
    		}
    		BeatPrompt.Clear();
    	}
    }
}