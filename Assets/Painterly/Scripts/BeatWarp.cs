using System;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;
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

    		[Tooltip("Where Ari lands. He is dropped here, not moved through anything.")]
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
    	private Key forward = Key.N;

    	[SerializeField]
    	private Key back = Key.B;

    	[Header("Behaviour")]
    	[Tooltip("How far above the stop Ari is dropped. Enough to clear the ground he lands on without letting his fall a visible distance.")]
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
    		if (WarpPressed(forward))
    		{
    			Go(1);
    		}
    		if (WarpPressed(back))
    		{
    			Go(-1);
    		}
    	}

    	/// <summary>
        /// One warp key, read the way this project reads keys.
        /// </summary>
        /// <remarks>
        /// <para>This used to be <c>Input.GetKeyDown(forward)</c>, and it threw
        /// every single frame: <c>InvalidOperationException: You are trying to
        /// read Input using the UnityEngine.Input class, but you have switched
        /// active Input handling to Input System package in Player Settings.</c>
        /// Project Settings is set to Input System, so every legacy
        /// <c>Input.</c> call is a hard exception, not a silent no-op - which
        /// meant BeatWarp threw from Update, and because an exception out of
        /// Update leaves the rest of that frame unrun, the warp keys stopped
        /// working entirely rather than degrading.</para>
        ///
        /// <para>The fields were also <c>KeyCode</c>, while the rest of the
        /// project holds its keys as <c>UnityEngine.InputSystem.Key</c> - see
        /// AriMover.jumpKey. Both enums are int-backed and both put the letters
        /// at their ASCII codes, so the scene's <c>forward: 110</c> and
        /// <c>back: 98</c> still mean N and B after the type change. That was
        /// checked against the scene file rather than assumed, because a silent
        /// reset of a serialized key to KeyCode.None is exactly the kind of
        /// thing that looks like the tool working.</para>
        /// </remarks>
        private static bool WarpPressed(Key key)
        {
            return Keyboard.current?[key].wasPressedThisFrame ?? false;
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