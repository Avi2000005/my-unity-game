using System;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;
namespace Echoes.Painterly
{

    [AddComponentMenu("Echoes/Beat Warp (test only)")]
    public sealed class BeatWarp : MonoBehaviour
    {
    	[Serializable]
    	public sealed class Stop
    	{
    		public string name = "";

    		[Tooltip("Where Ari lands. She is dropped here, not moved through anything.")]
    		public Vector3 ari;

    		[Tooltip("Where Mono lands, if he is awake. Leave at zero and he is put at Ari's feet.")]
    		public Vector3 mono;

    		[Tooltip("Optional. Looked up in the level's MonoHintLines and fired as if he had said it on his own, so a line can be heard without walking to the moment that triggers it.")]
    		public string lineId = "";
    	}

    	[Header("The list")]
    	[SerializeField]
    	private List<Stop> stops = new List<Stop>();

    	[Header("Keys")]
    	[SerializeField]
    	private KeyCode forward = (KeyCode)110;

    	[SerializeField]
    	private KeyCode back = (KeyCode)98;

    	[Header("Behaviour")]
    	[Tooltip("How far above the stop Ari is dropped. Enough to clear the ground she lands on without letting her fall a visible distance.")]
    	[Min(0f)]
    	[SerializeField]
    	private float drop = 0.6f;

    	[Tooltip("A beat that has not been built yet is skipped rather than dropping you into empty ground.")]
    	[SerializeField]
    	private bool skipUnbuilt = true;

    	[Header("State")]
    	[SerializeField]
    	private int index;

    	[Tooltip("Read by probes. Off by default in a build.")]
    	[SerializeField]
    	private bool enabledInBuild;

    	private AriMover ari;

    	private MonoCompanion mono;

    	[SerializeField]
    	private bool log = true;

    	public int Index => index;

    	public int Count => stops.Count;

    	public string CurrentName
    	{
    		get
    		{
    			if (index < 0 || index >= stops.Count)
    			{
    				return "(none)";
    			}
    			return stops[index].name;
    		}
    	}

    	private void Awake()
    	{
    		if (!Application.isEditor && !enabledInBuild)
    		{
    			((Behaviour)this).enabled = false;
    			return;
    		}
    		ari = Find<AriMover>();
    		mono = Find<MonoCompanion>();
    	}

    	private static T Find<T>() where T : Component
    	{
    		T[] array = Object.FindObjectsByType<T>((FindObjectsInactive)1);
    		if (array.Length == 0)
    		{
    			return default;
    		}
    		return array[0];
    	}

    	private void Update()
    	{
    		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
    		if (Input.GetKeyDown(forward))
    		{
    			Go(1);
    		}
    		if (Input.GetKeyDown(back))
    		{
    			Go(-1);
    		}
    	}

    	public void Go(int step)
    	{
    		if (stops == null || stops.Count == 0)
    		{
    			Debug.LogWarning((object)"[Echoes] warp: no stops on the list");
    			return;
    		}
    		index = ((index + step) % stops.Count + stops.Count) % stops.Count;
    		Land(stops[index]);
    	}

    	public void Land(Stop stop)
    	{
    		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
    		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
    		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
    		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
    		if (stop != null)
    		{
    			Vector3 val = stop.ari + Vector3.up * drop;
    			if ((Object)(object)ari != (Object)null)
    			{
    				ari.Teleport(val);
    			}
    			else
    			{
    				Debug.LogWarning((object)"[Echoes] warp: no AriMover in the scene");
    			}
    			if ((Object)(object)mono != (Object)null)
    			{
    				Vector3 val2 = ((stop.mono.sqrMagnitude > 0.0001f) ? stop.mono : (val + new Vector3(0.8f, 0f, 0f)));
    				mono.WarpTo(val2);
    			}
    			if (!string.IsNullOrEmpty(stop.lineId))
    			{
    				mono.SayBeat(stop.lineId);
    			}
    			if (log)
    			{
    				Debug.Log((object)("[Echoes] warp -> " + index + " '" + stop.name + "' at " + stop.ari.ToString("F2")), (Object)(object)this);
    			}
    		}
    	}

    	[ContextMenu("Capture a stop here")]
    	public void CaptureHere()
    	{
    		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
    		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
    		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
    		if ((Object)(object)ari == (Object)null)
    		{
    			ari = Find<AriMover>();
    		}
    		if ((Object)(object)ari == (Object)null)
    		{
    			Debug.LogWarning((object)"[Echoes] warp: no AriMover");
    			return;
    		}
    		Vector3 position = ((Component)ari).transform.position;
    		Stop stop = new Stop
    		{
    			name = "stop" + (stops.Count + 1) + "_" + DateTime.Now.ToString("HHmmss"),
    			ari = position,
    			mono = (((Object)(object)mono != (Object)null) ? ((Component)mono).transform.position : position)
    		};
    		stops.Add(stop);
    		Debug.Log((object)("[Echoes] warp: captured '" + stop.name + "' at " + position.ToString("F2") + ". Set its name in the Inspector."));
    	}

    	public BeatWarp()
    	{
    		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
    		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
    	}
    }
}