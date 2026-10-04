using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Ari Interact")]
    public sealed class AriInteract : MonoBehaviour
    {
    	[Header("Finding things")]
    	[Tooltip("How far to look for an interactable at all. The widest of the individual Ranges, plus slack. Costs one scan per frame.")]
    	[Min(0.5f)]
    	[SerializeField]
    	private float searchRadius = 4.5f;

    	[Tooltip("How often the scan runs, in seconds. The scan is cheap but it is a GetComponentsInChildren over the level and does not need to happen at frame rate.")]
    	[Min(0.02f)]
    	[SerializeField]
    	private float scanInterval = 0.12f;

    	[Tooltip("Search inactive too. Every interactable in this level starts inactive and is switched on by the beat that owns it.")]
    	[SerializeField]
    	private bool includeInactive = true;

    	[Header("Wiring")]
    	[Tooltip("Ari. Found if left empty.")]
    	[SerializeField]
    	private AriMover ari;

    	[SerializeField]
    	private bool log = true;

    	private static readonly List<IInteractable> _found = new List<IInteractable>(16);

    	private float _nextScan;

    	public static AriInteract I { get; private set; }

    	public static IInteractable Near { get; private set; }

    	public static string NearPrompt
    	{
    		get
    		{
    			if (Near != null && Near.CanInteract)
    			{
    				return Near.Prompt;
    			}
    			return "";
    		}
    	}

    	public static float NearDistance { get; private set; } = -1f;

    	public int Uses { get; private set; }

    	public float Reach => searchRadius;

    	public float ScanInterval => scanInterval;

    	private void Awake()
    	{
    		if ((Object)(object)I != (Object)null && (Object)(object)I != (Object)(object)this)
    		{
    			Debug.LogWarning((object)("[Echoes] a second AriInteract on '" + ((Object)this).name + "' ignored; already have one on '" + ((Object)I).name + "'"), (Object)(object)this);
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
    		Near = null;
    		NearDistance = -1f;
    		BeatPrompt.Clear();
    	}

    	private void Update()
    	{
    		if ((Object)(object)ari == (Object)null)
    		{
    			ari = Object.FindAnyObjectByType<AriMover>((FindObjectsInactive)1);
    			if ((Object)(object)ari == (Object)null)
    			{
    				return;
    			}
    		}
    		if (Time.time >= _nextScan)
    		{
    			_nextScan = Time.time + scanInterval;
    			Rescan();
    		}
    		if (Pressed())
    		{
    			TryUse();
    		}
    	}

    	private static bool Pressed()
    	{
    		Keyboard current = Keyboard.current;
    		if (current != null && (current.eKey.wasPressedThisFrame || current.fKey.wasPressedThisFrame))
    		{
    			return true;
    		}
    		return Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false;
    	}

    	private void Rescan()
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
    		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
    		Vector3 chest = Chest();
    		_found.Clear();
    		MonoBehaviour[] array = Object.FindObjectsByType<MonoBehaviour>((FindObjectsInactive)1, (FindObjectsSortMode)0);
    		IInteractable best = null;
    		float bestRange = -1f;
    		float bestDist = float.PositiveInfinity;
    		for (int i = 0; i < array.Length; i++)
    		{
    			if (array[i] is IInteractable interactable)
    			{
    				_found.Add(interactable);
    				Pick(interactable, chest, ref best, ref bestRange, ref bestDist);
    			}
    		}
    		Near = best;
    		NearDistance = ((best == null) ? (-1f) : bestDist);
    		if (best != null && best.CanInteract)
    		{
    			BeatPrompt.Show(best.Prompt);
    		}
    		else
    		{
    			BeatPrompt.Clear();
    		}
    	}

    	private void Pick(IInteractable mb, Vector3 chest, ref IInteractable best, ref float bestRange, ref float bestDist)
    	{
    		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
    		if (!mb.CanInteract)
    		{
    			return;
    		}
    		Transform at = mb.At;
    		if (!((Object)(object)at == (Object)null))
    		{
    			float range = mb.Range;
    			float num = Vector3.Distance(chest, at.position);
    			if ((!(num > range) || !(num > searchRadius)) && (best == null || (!(num > bestDist) && (!Mathf.Approximately(num, bestDist) || !(range >= bestRange)))))
    			{
    				best = mb;
    				bestRange = range;
    				bestDist = num;
    			}
    		}
    	}

    	private Vector3 Chest()
    	{
    		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
    		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)ari == (Object)null)
    		{
    			return ((Component)this).transform.position;
    		}
    		return ((Component)ari).transform.position + Vector3.up * (ari.BodyHeight * 0.6f);
    	}

    	private void TryUse()
    	{
    		if (Near != null && Near.CanInteract)
    		{
    			IInteractable near = Near;
    			near.Interact(ari);
    			Uses++;
    			if (log)
    			{
    				Debug.Log((object)("[Echoes] interact: " + near.Prompt + " at " + NearDistance.ToString("0.00") + " m"), (Object)(object)this);
    			}
    			Rescan();
    		}
    	}
    }
}