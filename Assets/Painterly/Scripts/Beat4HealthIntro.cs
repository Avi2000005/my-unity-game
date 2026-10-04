using UnityEngine;

namespace Echoes.Painterly;

[AddComponentMenu("Echoes/L1 Beat 4.5 Health Intro")]
public sealed class Beat4HealthIntro : MonoBehaviour, IResettable
{
	[Tooltip("How long the card holds before it hands the screen back. Long enough to read two lines, short enough that standing still does not cost anything.")]
	[Min(2f)]
	[SerializeField]
	private float holdSeconds = 7f;

	[Tooltip("Give the card up early once she starts walking east, so it never holds her in place longer than it has to.")]
	[SerializeField]
	private bool releaseOnWalking = true;

	[Tooltip("The beat that must finish before the bar is introduced.")]
	[SerializeField]
	private Beat4Director beat4;

	[SerializeField]
	private bool log = true;

	private bool _shown;

	private float _since;

	private Vector3 _shownAt;

	private AriMover _ari;

	[Min(0.1f)]
	[SerializeField]
	private float walkOffMetres = 0.6f;

	public bool Shown => _shown;

	public bool Holding
	{
		get
		{
			if (_shown)
			{
				return AriHudOverlay.ForceBar;
			}
			return false;
		}
	}

	private void OnEnable()
	{
		if ((Object)(object)beat4 == (Object)null)
		{
			beat4 = Object.FindAnyObjectByType<Beat4Director>((FindObjectsInactive)1);
		}
		if ((Object)(object)_ari == (Object)null)
		{
			_ari = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
		}
	}

	private void Update()
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		if (!_shown && (Object)(object)beat4 != (Object)null && beat4.Current == Beat4Director.Phase.Done)
		{
			Show();
		}
		if (!_shown)
		{
			return;
		}
		_since += Time.unscaledDeltaTime;
		bool flag = _since >= holdSeconds;
		bool flag2 = false;
		if (releaseOnWalking)
		{
			if ((Object)(object)_ari == (Object)null)
			{
				_ari = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
			}
			if ((Object)(object)_ari != (Object)null)
			{
				Vector3 val = ((Component)_ari).transform.position - _shownAt;
				val.y = 0f;
				flag2 = val.magnitude >= walkOffMetres;
			}
		}
		if (flag || flag2)
		{
			Release(flag2 ? "she started walking" : "the card timed out");
		}
	}

	private void Show()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		_shown = true;
		_since = 0f;
		if ((Object)(object)_ari == (Object)null)
		{
			_ari = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
		}
		if ((Object)(object)_ari != (Object)null)
		{
			_shownAt = ((Component)_ari).transform.position;
		}
		AriHudOverlay.ForceBar = true;
		AriHealth i = AriHealth.I;
		float num = (((Object)(object)i != (Object)null) ? (i.HitCost * 100f) : 12f);
		float num2 = (((Object)(object)i != (Object)null) ? (i.LowAt * 100f) : 30f);
		BeatPrompt.Show("That is your health, top left. It shortens when one of them reaches you — about " + Mathf.RoundToInt(num) + "% a hit. The mark at " + Mathf.RoundToInt(num2) + "% is the line past which you are in trouble.", holdSeconds);
		if (log)
		{
			Debug.Log((object)("[Echoes] beat 4.5 — health bar introduced (hit " + Mathf.RoundToInt(num) + "%, low at " + Mathf.RoundToInt(num2) + "%). Bar forced visible for " + holdSeconds.ToString("0") + " s, then it goes back to appearing only once it can be wrong."), (Object)(object)this);
		}
	}

	private void Release(string why)
	{
		AriHudOverlay.ForceBar = false;
		_shown = false;
		BeatPrompt.Clear();
		if (log)
		{
			Debug.Log((object)("[Echoes] beat 4.5 — released (" + why + "). The bar will now appear the first time she is hit."), (Object)(object)this);
		}
	}

	public void ResetForCheckpoint()
	{
		AriHudOverlay.ForceBar = false;
		_shown = false;
		_since = 0f;
		BeatPrompt.Clear();
		if (log)
		{
			Debug.Log((object)"[Echoes] beat 4.5 — reset; the bar is off again and will be re-introduced after Beat 4.", (Object)(object)this);
		}
	}
}
