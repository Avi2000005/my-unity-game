using UnityEngine;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Colour Gate")]
    public sealed class ColourGate : MonoBehaviour, IResettable
    {
    	[Header("The one exemption")]
    	[Tooltip("The fountain's water. Set exempt in Awake so it can take colour while the gate is shut. Left empty, the tool that places this reports it rather than Beat 7 failing quietly.")]
    	[SerializeField]
    	private ColorRestoreTarget fountainWater;

    	[Tooltip("Also exempt anything that names itself as water inside the fountain. Off, because a name match is a guess and a guess in the gate that decides the ending is the wrong place for one.")]
    	[SerializeField]
    	private bool searchChildren;

    	[SerializeField]
    	private bool log = true;

    	public static bool IsSealed => ColorRestoreTarget.Sealed;

    	public ColorRestoreTarget FountainWater => fountainWater;

    	public static void Tally(out int sealedCount, out int exemptCount)
    	{
    		ColorRestoreTarget.Tally(out sealedCount, out exemptCount);
    	}

    	private void Awake()
    	{
    		Apply();
    	}

    	public void Apply()
    	{
    		if ((Object)(object)fountainWater == (Object)null && searchChildren)
    		{
    			ColorRestoreTarget[] componentsInChildren = ((Component)this).GetComponentsInChildren<ColorRestoreTarget>(true);
    			for (int i = 0; i < componentsInChildren.Length; i++)
    			{
    				if (((Object)((Component)componentsInChildren[i]).gameObject).name.ToLowerInvariant().Contains("water"))
    				{
    					fountainWater = componentsInChildren[i];
    					break;
    				}
    			}
    		}
    		if ((Object)(object)fountainWater != (Object)null)
    		{
    			fountainWater.GrantExemption();
    		}
    		else if (log)
    		{
    			Debug.LogWarning((object)"[Echoes] colour gate has no fountain water. Beat 7 will complete without turning blue, and nothing will say why until the player sees it.", (Object)(object)this);
    		}
    		ColorRestoreTarget.SetSealed(sealedNow: true);
    		if (log)
    		{
    			Tally(out var sealedCount, out var exemptCount);
    			Debug.Log((object)("[Echoes] colour gate shut: " + sealedCount + " target(s) sealed, " + exemptCount + " exempt" + (((Object)(object)fountainWater != (Object)null) ? (" (the fountain, '" + ((Object)fountainWater).name + "')") : " (NONE — Beat 7 cannot work)")), (Object)(object)this);
    		}
    	}

    	public void ResetForCheckpoint()
    	{
    		if ((Object)(object)fountainWater != (Object)null)
    		{
    			fountainWater.SetRestoreImmediate(0f);
    		}
    		Apply();
    	}
    }
}