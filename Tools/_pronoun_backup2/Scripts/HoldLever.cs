using System;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Hold Lever")]
    public sealed class HoldLever : MonoBehaviour
    {
    	[Header("Who is holding it")]
    	[Tooltip("Ari. Left empty he is found by name at Awake.")]
    	[SerializeField]
    	private AriMover ari;

    	[Tooltip("How close he must be, measured flat on the ground. Loose on purpose: he is stopped by him capsule rather than his centre, so a tight radius would go live while he still looked like he was beside it.")]
    	[Min(0.2f)]
    	[SerializeField]
    	private float radius = 1.05f;

    	[Tooltip("Only counts while he is on the ground. Standing on a wall is not holding a lever, and a capsule sweep lets him stand on things a plate should not respond to.")]
    	[SerializeField]
    	private bool requireGrounded = true;

    	private bool _resolved;

    	[Header("Notes")]
    	[SerializeField]
    	private bool log = true;

    	[Header("Reading")]
    	[Tooltip("Whether the mechanism has power right now.")]
    	public bool Held { get; private set; }

    	public float HeldSeconds { get; private set; }

    	public int Releases { get; private set; }

    	public Vector3 PlateCentre
    	{
    		get
    		{
    			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    			return ((Component)this).transform.position;
    		}
    	}

    	public event Action<bool> Changed;

    	private void Awake()
    	{
    		if ((Object)(object)ari == (Object)null)
    		{
    			Resolve();
    		}
    	}

    	private void Resolve()
    	{
    		_resolved = true;
    		if ((Object)(object)ari != (Object)null)
    		{
    			return;
    		}
    		GameObject val = GameObject.Find("Ari");
    		if ((Object)(object)val == (Object)null)
    		{
    			Debug.LogWarning((object)("[Echoes] " + ((Object)this).name + ": no Ari in the scene, so the lever can never be held."));
    			return;
    		}
    		ari = val.GetComponent<AriMover>();
    		if ((Object)(object)ari == (Object)null)
    		{
    			Debug.LogWarning((object)("[Echoes] " + ((Object)this).name + ": Ari has no AriMover."));
    		}
    	}

    	private void Update()
    	{
    		if (!_resolved)
    		{
    			Resolve();
    		}
    		bool flag = IsSheOnIt();
    		if (flag != Held)
    		{
    			Held = flag;
    			if (!flag)
    			{
    				Releases++;
    			}
    			if (log)
    			{
    				Debug.Log((object)("[Echoes] lever " + (flag ? "held" : "released") + " after " + HeldSeconds.ToString("0.0") + " s"), (Object)(object)this);
    			}
    			if (Changed != null)
    			{
    				Changed(flag);
    			}
    		}
    	}

    	private bool IsSheOnIt()
    	{
    		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
    		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
    		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)ari == (Object)null)
    		{
    			return false;
    		}
    		if (requireGrounded && !ari.IsGrounded)
    		{
    			return false;
    		}
    		Vector3 position = ((Component)ari).transform.position;
    		Vector3 position2 = ((Component)this).transform.position;
    		position.y = 0f;
    		position2.y = 0f;
    		return Vector3.Distance(position, position2) <= radius;
    	}

    	private void OnDrawGizmosSelected()
    	{
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
    		Gizmos.color = new Color(0.95f, 0.75f, 0.2f, 0.5f);
    		Gizmos.DrawWireSphere(((Component)this).transform.position, radius);
    	}

    	public void ResetLever()
    	{
    		Held = false;
    		HeldSeconds = 0f;
    		Releases = 0;
    	}
    }
}