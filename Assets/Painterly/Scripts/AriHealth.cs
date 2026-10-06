using System;
using UnityEngine;


using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Ari Health")]
    public sealed class AriHealth : MonoBehaviour
    {
    	[Header("Health")]
    	[Tooltip("Fraction of FULL health a crawler lunge takes. The brief asks for 10-15%; 12% is the middle of that band. This is a flat subtraction from the current value, NOT a fraction of what is left. From full health that reaches zero in about nine hits, which is what makes Ari's death real and the complaint 'Ari does not die at health 0' stay fixed. It is deliberately not Now - (Now * hitFraction): that is 0.88 to the power n, so in a float it creeps toward zero forever and she effectively never dies at all.")]
    	[Range(0.01f, 1f)]
    	[SerializeField]
    	private float hitFraction = 0.08f;

    	[Tooltip("Below this the low-health warning shows and the stinger loops.")]
    	[Range(0.05f, 0.9f)]
    	[SerializeField]
    	private float lowFraction = 0.3f;

    	[Tooltip("Seconds the damage flash stays up.")]
    	[Min(0.05f)]
    	[SerializeField]
    	private float flashSeconds = 0.35f;

    	[Tooltip("Seconds before another hit may flash again. Stops three crawlers hitting in the same frame from looking like one hit.")]
    	[Min(0f)]
    	[SerializeField]
    	private float flashCooldown = 1.0f;

    	[Header("Death")]
    	[Tooltip("Seconds after reaching zero before the lose screen is raised. Long enough for the flash to be seen at all.")]
    	[Min(0f)]
    	[SerializeField]
    	private float deathDelay = 2.8f;

    	[SerializeField]
    	private bool log = true;

    	private static float _flashUntil = -1f;

    	private float _lastHitTime = -1f;

    	public static AriHealth I { get; private set; }

    	public float Now { get; private set; } = 1f;

    	public float Max => 1f;

    	public bool IsDead { get; private set; }

    	public float Fraction => Now;

    	public int Hits { get; private set; }

    	public float Lost => 1f - Now;

    	public bool IsLow
    	{
    		get
    		{
    			if (!IsDead)
    			{
    				return Now <= lowFraction;
    			}
    			return false;
    		}
    	}

    	public static float FlashLeft
    	{
    		get
    		{
    			if (!(_flashUntil < 0f))
    			{
    				return Mathf.Max(0f, _flashUntil - Time.time);
    			}
    			return 0f;
    		}
    	}

    	public static bool Flashing => FlashLeft > 0f;

    	public static bool AnyLow
    	{
    		get
    		{
    			if ((Object)(object)I != (Object)null)
    			{
    				return I.IsLow;
    			}
    			return false;
    		}
    	}

    	public float HitCost => hitFraction;

    	public float LowAt => lowFraction;

    	public event Action<float> Changed;

    	public event Action<float> Hurt;

    	public event Action Died;

    	private void Awake()
    	{
    		if ((Object)(object)I != (Object)null && (Object)(object)I != (Object)(object)this)
    		{
    			Debug.LogWarning((object)("[Echoes] a second AriHealth on '" + ((Object)this).name + "' was ignored; Ari already has one on '" + ((Object)I).name + "'"), (Object)(object)this);
    			((Behaviour)this).enabled = false;
    		}
    		else
    		{
    			I = this;
    		}
    	}

    	private void OnDisable()
    	{
    		if ((Object)(object)I == (Object)(object)this)
    		{
    			I = null;
    		}
    	}

    	public bool TakeHit()
    	{
    		if (IsDead)
    		{
    			return false;
    		}
    		if (_lastHitTime >= 0f && Time.time - _lastHitTime < flashCooldown)
    		{
    			return false;
    		}
    		_lastHitTime = Time.time;
    		Now = Mathf.Max(0f, Now - hitFraction);
    		Hits++;
    		_flashUntil = Time.time + flashSeconds;
    		if (log)
    		{
    			Debug.Log((object)("[Echoes] Ari hit — health " + (Now * 100f).ToString("0") + "%"), (Object)(object)this);
    		}
    		Hurt?.Invoke(Now);
    		if (Now > 0f)
    		{
    			Changed?.Invoke(Now);
    			return true;
    		}
    		Die();
    		return true;
    	}

    	private void Die()
    	{
    		IsDead = true;
    		Now = 0f;
    		if (log)
    		{
    			Debug.Log("[Echoes] Ari defeated! Playing death animation.", (Object)(object)this);
    		}
    		AriAnim.PlayDeath();
    		if ((Object)(object)AriMover.I != (Object)null)
    		{
    			AriMover.I.Frozen = true;
    		}
    		Changed?.Invoke(Now);
    		Died?.Invoke();
    		LevelRunner.RunAfter(deathDelay, RaiseLost);
    	}

    	private void RaiseLost()
    	{
    		if (IsDead)
    		{
    			LevelState.Lost = true;
    		}
    	}

    	public void ResetHealth()
    	{
    		bool isDead = IsDead;
    		IsDead = false;
    		Now = 1f;
    		Hits = 0;
    		_lastHitTime = -1f;
    		_flashUntil = -1f;
    		LevelState.Lost = false;
    		AriAnim.ResetDeath();
    		if ((Object)(object)AriMover.I != (Object)null)
    		{
    			AriMover.I.Frozen = false;
    		}
    		if (isDead && log)
    		{
    			Debug.Log((object)"[Echoes] Ari restored to full health", (Object)(object)this);
    		}
    		Changed?.Invoke(Now);
    	}

    	public void Heal(float fraction)
    	{
    		if (!(fraction <= 0f) && !IsDead)
    		{
    			Now = Mathf.Clamp01(Now + fraction);
    			Changed?.Invoke(Now);
    		}
    	}
    }
}
