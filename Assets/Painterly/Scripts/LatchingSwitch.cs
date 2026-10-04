using System;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Latching Switch")]
    public sealed class LatchingSwitch : MonoBehaviour
    {
    	[Header("Who can reach it")]
    	[Tooltip("Mono. Left empty he is found by name at Awake.")]
    	[SerializeField]
    	private MonoCompanion mono;

    	[Tooltip("How close he must be. The setup tool checks this against Mono's errand radius — if he arrives and stops short of this, the beat never fires and nothing on screen says why.")]
    	[Min(0.3f)]
    	[SerializeField]
    	private float reach = 1f;

    	[Header("What lets it throw")]
    	[Tooltip("The lever that powers the mechanism. Left empty the switch always has power, which turns the beat off but leaves it working.")]
    	[SerializeField]
    	private HoldLever lever;

    	[Tooltip("Require the lever to be held at the moment he throws.")]
    	[SerializeField]
    	private bool requirePower = true;

    	[SerializeField]
    	private bool log = true;

    	[Header("Reading")]
    	public bool Thrown { get; private set; }

    	public int WaitedForPower { get; private set; }

    	public int Arrivals { get; private set; }

    	public bool Powered
    	{
    		get
    		{
    			if (requirePower && !((Object)(object)lever == (Object)null))
    			{
    				return lever.Held;
    			}
    			return true;
    		}
    	}

    	public bool MonoInReach
    	{
    		get
    		{
    			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
    			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
    			if ((Object)(object)mono == (Object)null)
    			{
    				return false;
    			}
    			if (!((Component)mono).gameObject.activeInHierarchy)
    			{
    				return false;
    			}
    			return Flat(((Component)mono).transform.position, ((Component)this).transform.position) <= reach;
    		}
    	}

    	public float DistanceToMono
    	{
    		get
    		{
    			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    			if ((Object)(object)mono == (Object)null)
    			{
    				return -1f;
    			}
    			return Flat(((Component)mono).transform.position, ((Component)this).transform.position);
    		}
    	}

    	public float Reach => reach;

    	public event Action Threw;

    	private void Awake()
    	{
    		if ((Object)(object)mono == (Object)null)
    		{
    			GameObject val = GameObject.Find("Mono");
    			if ((Object)(object)val != (Object)null)
    			{
    				mono = val.GetComponent<MonoCompanion>();
    			}
    		}
    	}

    	private static float Flat(Vector3 a, Vector3 b)
    	{
    		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
    		a.y = 0f;
    		b.y = 0f;
    		return Vector3.Distance(a, b);
    	}

    	private void Update()
    	{
    		if (!Thrown && !((Object)(object)mono == (Object)null) && ((Component)mono).gameObject.activeInHierarchy && MonoInReach)
    		{
    			Arrivals++;
    			if (!Powered)
    			{
    				WaitedForPower++;
    			}
    			else
    			{
    				Throw();
    			}
    		}
    	}

    	private void Throw()
    	{
    		Thrown = true;
    		if (log)
    		{
    			Debug.Log((object)("[Echoes] switch thrown after " + Arrivals + " arrival(s), " + WaitedForPower + " of them without power"), (Object)(object)this);
    		}
    		if (Threw != null)
    		{
    			Threw();
    		}
    	}

    	private void OnDrawGizmosSelected()
    	{
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.5f);
    		Gizmos.DrawWireSphere(((Component)this).transform.position, reach);
    	}

    	public void ResetSwitch()
    	{
    		Thrown = false;
    		WaitedForPower = 0;
    		Arrivals = 0;
    	}
    }
}