using UnityEngine;

namespace Echoes.Painterly
{

    public static class AriAnim
    {
    	private static Animator _anim;

    	private static bool _warned;

    	private static readonly int RunId = Animator.StringToHash("Run");

    	private static readonly int TalkId = Animator.StringToHash("Talk");

    	private static readonly int CollectId = Animator.StringToHash("Collect");

    	private static readonly int SwingId = Animator.StringToHash("Swing");

    	private static readonly int SpeedId = Animator.StringToHash("Speed");
    	private static readonly int DieId = Animator.StringToHash("Die");

    	private static Vector3? _talkFace;

    	private static float _talkUntil;

    	public static bool Ready
    	{
    		get
    		{
    			Animator val = Resolve();
    			if ((Object)(object)val == (Object)null || (Object)(object)val.runtimeAnimatorController == (Object)null)
    			{
    				return false;
    			}
    			if (Has(val, RunId, "Run") && Has(val, TalkId, "Talk"))
    			{
    				return Has(val, CollectId, "Collect");
    			}
    			return false;
    		}
    	}

    	public static bool Talking
    	{
    		get
    		{
    			if (Time.time < _talkUntil)
    			{
    				return _talkFace.HasValue;
    			}
    			return false;
    		}
    	}

    	public static Vector3? TalkTarget
    	{
    		get
    		{
    			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
    			MonoCompanion monoCompanion = Object.FindAnyObjectByType<MonoCompanion>((FindObjectsInactive)1);
    			if (!((Object)(object)monoCompanion != (Object)null))
    			{
    				return null;
    			}
    			return ((Component)monoCompanion).transform.position;
    		}
    	}

    	public static Vector3 TalkFacing
    	{
    		get
    		{
    			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
    			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
    			Vector3? talkFace = _talkFace;
    			if (!talkFace.HasValue)
    			{
    				if (!((Object)(object)_anim != (Object)null))
    				{
    					return Vector3.zero;
    				}
    				return ((Component)_anim).transform.position;
    			}
    			return talkFace.GetValueOrDefault();
    		}
    	}

    	public static void SetRun(bool running)
    	{
    		Animator val = Resolve();
    		if (!((Object)(object)val == (Object)null) && Has(val, RunId, "Run"))
    		{
    			val.SetBool(RunId, running);
    		}
    	}

    	public static void SetSpeed(float normalised)
    	{
    		if (normalised > 0.05f)
    		{
    			_talkUntil = 0f;
    			_talkFace = null;
    		}
    		Animator val = Resolve();
    		if (!((Object)(object)val == (Object)null) && Has(val, SpeedId, "Speed"))
    		{
    			val.SetFloat(SpeedId, normalised);
    		}
    	}

    	public static void PlaySwing()
    	{
    		Animator val = Resolve();
    		if (!((Object)(object)val == (Object)null) && Has(val, SwingId, "Swing"))
    		{
    			val.SetTrigger(SwingId);
    		}
    	}

    	public static void PlayTalk()
    	{
    		Animator val = Resolve();
    		if (!((Object)(object)val == (Object)null) && Has(val, TalkId, "Talk"))
    		{
    			val.ResetTrigger(TalkId);
    			val.SetTrigger(TalkId);
    			_talkUntil = Time.time + 4f;
    			_talkFace = TalkTarget;
    		}
    	}

    	public static void PlayCollect()
    	{
    		Animator val = Resolve();
    		if (!((Object)(object)val == (Object)null) && Has(val, CollectId, "Collect"))
    		{
    			val.ResetTrigger(CollectId);
    			val.SetTrigger(CollectId);
    		}
    	}

    	public static void PlayDeath()
    	{
    		Animator val = Resolve();
    		if ((Object)(object)val != (Object)null)
    		{
    			val.ResetTrigger(TalkId);
    			val.ResetTrigger(CollectId);
    			val.ResetTrigger(SwingId);
    			if (Has(val, RunId, "Run")) val.SetBool(RunId, false);
    			if (Has(val, SpeedId, "Speed")) val.SetFloat(SpeedId, 0f);
    			if (Has(val, DieId, "Die"))
    			{
    				val.SetTrigger(DieId);
    			}
    			val.Play("Ari_Death", 0, 0f);
    		}
    	}

    	public static void ResetDeath()
    	{
    		Animator val = Resolve();
    		if ((Object)(object)val != (Object)null)
    		{
    			if (Has(val, DieId, "Die"))
    			{
    				val.ResetTrigger(DieId);
    			}
    			val.Play("Ari_Idle", 0, 0f);
    		}
    	}

    	private static Animator Resolve()
    	{
    		if ((Object)(object)_anim != (Object)null)
    		{
    			return _anim;
    		}
    		AriMover ariMover = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    		if ((Object)(object)ariMover == (Object)null)
    		{
    			if (!_warned)
    			{
    				Debug.LogWarning((object)"[Echoes] AriAnim: no AriMover in the scene. Ari will walk without any animation.");
    				_warned = true;
    			}
    			return null;
    		}
    		_anim = ((Component)ariMover).GetComponentInChildren<Animator>(true);
    		if ((Object)(object)_anim == (Object)null || (Object)(object)_anim.runtimeAnimatorController == (Object)null)
    		{
    			if (!_warned)
    			{
    				Debug.LogWarning((object)("[Echoes] AriAnim: Ari has " + (((Object)(object)_anim == (Object)null) ? "no Animator" : "no controller") + ", so his run, talk and collect poses cannot play. Assign Ari.controller to his Animator."));
    				_warned = true;
    			}
    			return null;
    		}
    		return _anim;
    	}

    	private static bool Has(Animator a, int id, string name)
    	{
    		AnimatorControllerParameter[] parameters = a.parameters;
    		for (int i = 0; i < parameters.Length; i++)
    		{
    			if (parameters[i].nameHash == id)
    			{
    				return true;
    			}
    		}
    		Debug.LogWarning((object)("[Echoes] AriAnim: " + ((Object)a.runtimeAnimatorController).name + " has no parameter '" + name + "'. Whatever drives that pose will not play. Run Tools/Echoes/Assign and Wire the Animations."));
    		return false;
    	}

    	public static void Forget()
    	{
    		_anim = null;
    		_warned = false;
    		_talkFace = null;
    		_talkUntil = 0f;
    	}
    }
}