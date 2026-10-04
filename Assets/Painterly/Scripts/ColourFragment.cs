using UnityEngine;

namespace Echoes.Painterly;

[AddComponentMenu("Echoes/Colour Fragment")]
public sealed class ColourFragment : MonoBehaviour, IInteractable, IResettable
{
	[Header("Reach")]
	[Tooltip("From Ari's chest. Short, because it is set into a wall and she has to stand against it — the point of the beat is the crawlers, not the reach.")]
	[Min(0.5f)]
	[SerializeField]
	private float range = 2.4f;

	[Header("Words")]
	[SerializeField]
	private string promptText = "Take the blue fragment";

	[SerializeField]
	private string carriedText = "";

	[Tooltip("Said the moment it is picked up. Looked up in Mono's line list like every other line in this level.")]
	[SerializeField]
	private string pickupLineId = "beat6.take";

	[Header("When it appears")]
	[Tooltip("OFF by default, and that is a change of mind worth recording. It used to be on, on the reasoning that appearance should be driven by the beat — and the beat that was supposed to drive it did not exist. Measured: there is no runtime caller of Show(true) anywhere in the project, and the fragment object itself has zero children, so it was hidden with nothing behind the hidden state. A pickup that cannot be seen and cannot be taken is not a hidden pickup, it is a missing one — and the fountain, the last beat of the level, was holding nothing.")]
	[SerializeField]
	private bool startsHidden;

	[SerializeField]
	private bool log = true;

	[Header("The glow")]
	[Tooltip("Scale of the glow quad, in metres. Big enough to be found across the yard, small enough that it is clearly an object and not a light source.")]
	[Min(0.05f)]
	[SerializeField]
	private float glowSize = 0.55f;

	[Tooltip("Pulses, so it reads as alive and can be spotted in peripheral vision without being a beacon.")]
	[SerializeField]
	private bool pulse = true;

	[Min(0.1f)]
	[SerializeField]
	private float pulseSpeed = 1.6f;

	[Min(0f)]
	[SerializeField]
	private float pulseAmount = 0.25f;

	private Renderer[] _glow;

	private Transform _visual;

	private MonoCompanion _mono;

	private bool _taken;

	private Vector3 _spot;

	private bool _shown;

	public string Prompt => promptText;

	public float Range => range;

	public Transform At => ((Component)this).transform;

	public bool CanInteract
	{
		get
		{
			if (IsShowing)
			{
				return !_taken;
			}
			return false;
		}
	}

	public bool IsShowing
	{
		get
		{
			if (startsHidden)
			{
				return _shown;
			}
			return true;
		}
	}

	public bool Taken => _taken;

	public Vector3 Spot
	{
		get
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return _spot;
		}
	}

	private void Awake()
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		_mono = MonoCompanion.FindInLevel();
		_visual = ((Component)this).transform.Find("Glow");
		if ((Object)(object)_visual == (Object)null)
		{
			_visual = ((((Component)this).transform.childCount > 0) ? ((Component)this).transform.GetChild(0) : null);
		}
		if ((Object)(object)_visual != (Object)null)
		{
			_glow = ((Component)_visual).GetComponentsInChildren<Renderer>(true);
		}
		_spot = ((Component)this).transform.position;
		if (_glow == null || _glow.Length == 0)
		{
			Debug.LogError((object)("[Echoes] " + ((Object)this).name + " has no Glow child, so there is nothing to see whether or not it is shown. Run Tools/Echoes/Fragment — fix it, which builds the visual."), (Object)(object)this);
		}
		Show(!startsHidden);
	}

	public void Show(bool on)
	{
		if (!_taken)
		{
			_shown = on;
			if ((Object)(object)_visual != (Object)null)
			{
				((Component)_visual).gameObject.SetActive(on);
			}
		}
	}

	public void PlaceAt(Vector3 where)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		_spot = where;
		((Component)this).transform.position = where;
	}

	private void Update()
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		if (pulse && !((Object)(object)_visual == (Object)null) && _glow != null && _glow.Length != 0 && ((Component)_visual).gameObject.activeSelf)
		{
			float num = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
			_visual.localScale = Vector3.one * (glowSize * num);
		}
	}

	public void Interact(AriMover by)
	{
		if (!_taken && _shown)
		{
			_taken = true;
			if ((Object)(object)_visual != (Object)null)
			{
				((Component)_visual).gameObject.SetActive(false);
			}
			AriHudOverlay.CarryingFragment = true;
			AriAnim.PlayCollect();
			if ((Object)(object)_mono != (Object)null && !string.IsNullOrEmpty(pickupLineId))
			{
				_mono.SayBeat(pickupLineId);
			}
			if (!string.IsNullOrEmpty(carriedText))
			{
				BeatPrompt.Show(carriedText, 4f);
			}
			else
			{
				BeatPrompt.Clear();
			}
			if (log)
			{
				Debug.Log((object)"[Echoes] the blue fragment is hers — it is the only coloured thing in the level now", (Object)(object)this);
			}
		}
	}

	public void ResetForCheckpoint()
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		_taken = false;
		AriHudOverlay.CarryingFragment = false;
		_spot = ((Component)this).transform.position;
		Show(!startsHidden);
	}
}
