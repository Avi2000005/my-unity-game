using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly;

[AddComponentMenu("Echoes/Fountain Fix")]
public sealed class FountainFix : MonoBehaviour, IInteractable, IResettable
{
	[Header("Reach")]
	[Tooltip("From Ari's chest. Generous, because the fountain is four metres across and the beat should not fail on whether she walked to its exact middle.")]
	[Min(0.5f)]
	[SerializeField]
	private float range = 4f;

	[Header("Words")]
	[Tooltip("Says the key that actually works. Both E and F do, and the prompt names both — a prompt that names one key when two work still reads as 'press the other one and nothing happens, the game is broken'.")]
	[SerializeField]
	private string promptWithFragment = "Press E or F — set the blue fragment in the water";

	[Tooltip("Shown if she presses E here without it. Says what is missing, because a prompt that does not appear at all reads as the game being broken.")]
	[SerializeField]
	private string promptWithout = "The basin is dry and grey. It wants water, and blue.";

	[Tooltip("Said on the fix. First time the level speaks about colour.")]
	[SerializeField]
	private string fixLineId = "beat7.fix";

	[Header("The fix")]
	[Tooltip("Seconds for the water to fill. Long enough to watch.")]
	[Min(0.2f)]
	[SerializeField]
	private float fillSeconds = 2.4f;

	[Tooltip("Seconds between the fountain going blue and the rest of the village coming back. Long enough for the player to see the one blue thing before the world fills up.")]
	[Min(0f)]
	[SerializeField]
	private float villageDelay = 4.5f;

	[Tooltip("Seconds for the village flood once it starts.")]
	[Min(0.2f)]
	[SerializeField]
	private float villageSeconds = 5f;

	[Tooltip("How much of the village comes back. Not all of it — the level is finished, not the world.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float villageAmount = 0.35f;

	[Header("Parts")]
	[Tooltip("The water. Its own ColorRestoreTarget, marked exempt from the colour gate — without that exemption Beat 7 has no effect at all.")]
	[SerializeField]
	private ColorRestoreTarget water;

	[SerializeField]
	private MonoCompanion mono;

	[SerializeField]
	private bool log = true;

	private bool _done;

	private readonly List<ColorRestoreTarget> _flooded = new List<ColorRestoreTarget>(256);

	public string Prompt
	{
		get
		{
			if (!AriHudOverlay.CarryingFragment)
			{
				return promptWithout;
			}
			return promptWithFragment;
		}
	}

	public float Range => range;

	public Transform At => ((Component)this).transform;

	public bool CanInteract
	{
		get
		{
			if (!_done)
			{
				return !LevelState.Completed;
			}
			return false;
		}
	}

	public bool Done => _done;

	public bool Unsealed { get; private set; }

	public float Seconds { get; private set; }

	public int FloodedTargets { get; private set; }

	private void Awake()
	{
		if ((Object)(object)mono == (Object)null)
		{
			mono = MonoCompanion.FindInLevel();
		}
		if (!((Object)(object)water == (Object)null))
		{
			return;
		}
		ColorRestoreTarget[] componentsInChildren = ((Component)this).GetComponentsInChildren<ColorRestoreTarget>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (((Object)((Component)componentsInChildren[i]).gameObject).name.ToLowerInvariant().Contains("water"))
			{
				water = componentsInChildren[i];
				break;
			}
		}
	}

	private void Update()
	{
		if (_done)
		{
			Seconds += Time.deltaTime;
		}
	}

	public void Interact(AriMover by)
	{
		if (_done || LevelState.Completed)
		{
			return;
		}
		if (!AriHudOverlay.CarryingFragment)
		{
			if (log)
			{
				Debug.Log((object)"[Echoes] the fountain refused the fix — she is not carrying the fragment, and nothing was consumed", (Object)(object)this);
			}
			return;
		}
		_done = true;
		AriHudOverlay.CarryingFragment = false;
		if ((Object)(object)water == (Object)null)
		{
			Debug.LogWarning((object)"[Echoes] the fountain has no water ColorRestoreTarget, so it will complete without turning blue. The beat still counts — a missing target is a bug in the fountain, not a reason to lock the player out.", (Object)(object)this);
		}
		else
		{
			water.RestoreTo(1f, fillSeconds);
		}
		if ((Object)(object)mono != (Object)null && !string.IsNullOrEmpty(fixLineId))
		{
			mono.SayBeat(fixLineId);
		}
		LevelRunner.RunAfter(villageDelay, FloodVillage);
		if (log)
		{
			Debug.Log((object)("[Echoes] beat 7 — the fountain took the fragment. Village in " + villageDelay.ToString("0.0") + " s."), (Object)(object)this);
		}
	}

	public void FloodVillage()
	{
		ColorRestoreTarget.SetSealed(sealedNow: false);
		Unsealed = true;
		IReadOnlyList<ColorRestoreTarget> allActive = ColorRestoreTarget.AllActive;
		_flooded.Clear();
		for (int i = 0; i < allActive.Count; i++)
		{
			ColorRestoreTarget colorRestoreTarget = allActive[i];
			if (!((Object)(object)colorRestoreTarget == (Object)null))
			{
				_flooded.Add(colorRestoreTarget);
			}
		}
		for (int j = 0; j < _flooded.Count; j++)
		{
			_flooded[j].RestoreTo(villageAmount, villageSeconds);
		}
		FloodedTargets = _flooded.Count;
		LevelState.Completed = true;
		if (log)
		{
			Debug.Log((object)("[Echoes] LEVEL 1 COMPLETE — colour gate open, " + FloodedTargets + " target(s) restored to " + (villageAmount * 100f).ToString("0") + "%"), (Object)(object)this);
		}
	}

	public void ResetForCheckpoint()
	{
		_done = false;
		Unsealed = false;
		Seconds = 0f;
		FloodedTargets = 0;
		_flooded.Clear();
		LevelState.Completed = false;
		ColorRestoreTarget.SetSealed(sealedNow: true);
		if ((Object)(object)water != (Object)null)
		{
			water.SetRestoreImmediate(0f);
		}
	}
}
