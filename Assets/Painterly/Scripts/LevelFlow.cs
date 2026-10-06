using System;
using System.Collections.Generic;
using UnityEngine;


using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    public static class LevelState
    {
    	public static bool Lost;

    	public static bool Completed;

    	public static int Retries;

    	public static void ResetAll()
    	{
    		Lost = false;
    		Completed = false;
    		Retries = 0;
    	}
    }


    [AddComponentMenu("Echoes/Level Runner")]
    public sealed class LevelRunner : MonoBehaviour
    {
    	private sealed class Pending
    	{
    		public float At;

    		public Action Act;

    		public bool Dead;
    	}

    	private static LevelRunner _shared;

    	private readonly List<Pending> _queue = new List<Pending>(8);

    	private readonly List<Pending> _scratch = new List<Pending>(8);

    	public static LevelRunner Shared
    	{
    		get
    		{
    			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
    			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
    			//IL_001d: Expected Obj, but got Unknown
    			if ((Object)(object)_shared == (Object)null)
    			{
    				GameObject val = new GameObject("__LevelRunner");
    				Object.DontDestroyOnLoad((Object)val);
    				_shared = val.AddComponent<LevelRunner>();
    			}
    			return _shared;
    		}
    	}

    	public static void RunAfter(float seconds, Action act)
    	{
    		if (act != null)
    		{
    			Shared.Schedule(seconds, act);
    		}
    	}

    	public void Schedule(float seconds, Action act)
    	{
    		_queue.Add(new Pending
    		{
    			At = Time.time + Mathf.Max(0f, seconds),
    			Act = act
    		});
    	}

    	public static void CancelAll()
    	{
    		Shared._queue.Clear();
    	}

    	private void Update()
    	{
    		if (_queue.Count == 0)
    		{
    			return;
    		}
    		_scratch.Clear();
    		_scratch.AddRange(_queue);
    		for (int i = 0; i < _scratch.Count; i++)
    		{
    			Pending pending = _scratch[i];
    			if (!pending.Dead && !(Time.time < pending.At))
    			{
    				pending.Dead = true;
    				_queue.Remove(pending);
    				try
    				{
    					pending.Act();
    				}
    				catch (Exception ex)
    				{
    					Debug.LogError((object)("[Echoes] a scheduled level action threw: " + ex.Message));
    				}
    			}
    		}
    	}
    }


    [AddComponentMenu("Echoes/Level Checkpoint")]
    public sealed class LevelCheckpoint : MonoBehaviour
    {
    	[Header("Trigger")]
    	[Tooltip("Radius Ari walks into to arm this checkpoint.")]
    	[Min(0.5f)]
    	[SerializeField]
    	private float armRadius = 4f;

    	[Tooltip("Name, for the report. The brief calls this one the start of the three-crawler fight.")]
    	[SerializeField]
    	private string label = "beat5_start";

    	[Header("Wiring")]
    	[SerializeField]
    	private AriMover ari;

    	[SerializeField]
    	private AriHealth health;

    	[SerializeField]
    	private bool log = true;

    	private Vector3 _point;

    	public bool Armed { get; private set; }

    	public Vector3 Point
    	{
    		get
    		{
    			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    			return _point;
    		}
    	}

    	public string Label => label;

    	public int TimesUsed { get; private set; }

    	private void Awake()
    	{
    		if ((Object)(object)ari == (Object)null)
    		{
    			ari = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    		}
    		if ((Object)(object)health == (Object)null)
    		{
    			health = Object.FindAnyObjectByType<AriHealth>((FindObjectsInactive)1);
    		}
    	}

    	private void Update()
    	{
    		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
    		if (!Armed && !((Object)(object)ari == (Object)null) && !(Vector3.Distance(((Component)ari).transform.position, ((Component)this).transform.position) > armRadius))
    		{
    			Arm();
    		}
    	}

    	public void Arm()
    	{
    		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 val = (((Object)(object)ari != (Object)null) ? ((Component)ari).transform.position : ((Component)this).transform.position);
    		val.y = GroundUnder(val);
    		_point = val;
    		Armed = true;
    		if (log)
    		{
    			Debug.Log((object)("[Echoes] checkpoint '" + label + "' armed at " + _point.ToString("F2")), (Object)(object)this);
    		}
    	}

    	public int Retry()
    	{
    		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
    		LevelRunner.CancelAll();
    		int result = ResetEveryBeat();
    		if ((Object)(object)health != (Object)null)
    		{
    			health.ResetHealth();
    		}
    		if ((Object)(object)ari != (Object)null)
    		{
    			ari.Teleport(_point);
    		}
    		LevelState.Lost = false;
    		LevelState.Retries++;
    		if (log)
    		{
    			Debug.Log((object)("[Echoes] retry " + LevelState.Retries + " at '" + label + "' — " + result + " beat(s) reset"), (Object)(object)this);
    		}
    		return result;
    	}

    	public static int ResetEveryBeat()
    	{
    		MonoBehaviour[] array = Object.FindObjectsByType<MonoBehaviour>((FindObjectsInactive)1);
    		int num = 0;
    		for (int i = 0; i < array.Length; i++)
    		{
    			if (array[i] is IResettable resettable)
    			{
    				try
    				{
    					resettable.ResetForCheckpoint();
    					num++;
    				}
    				catch (Exception ex)
    				{
    					Debug.LogError((object)("[Echoes] '" + ((Object)array[i]).name + "' threw while resetting: " + ex.Message));
    				}
    			}
    		}
    		return num;
    	}

    	private static float GroundUnder(Vector3 p)
    	{
    		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
    		RaycastHit val = default;
    		if (Physics.Raycast(p + Vector3.up * 4f, Vector3.down, out val, 40f, -1, (QueryTriggerInteraction)1))
    		{
    			return val.point.y;
    		}
    		return p.y;
    	}
    }
}