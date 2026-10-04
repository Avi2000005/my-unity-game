using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// Test-only warp between beats, on one key.
    ///
    /// Level 1 runs about 120 m west to east and the beats sit at the far end of
    /// it. Without this, checking whether a gate opens correctly means walking
    /// the whole village first, every time, which means the check happens
    /// rarely — and a check that is expensive does not get skipped, it gets
    /// skipped *quietly*, and the beat ships untested.
    ///
    /// It is deliberately not in `AriMover`. Movement is the one thing in this
    /// project that should be boring to read, and a debug key in there is a
    /// debug key forever after. It is a separate component on its own object,
    /// it strips itself in a build, and `Beat4Setup` places it.
    ///
    /// `N` steps forward through the beat list, `B` steps back. Both report the
    /// stop they landed on, so a wrong jump is visible in the log rather than
    /// being a five-minute walk in the wrong direction.
    /// </summary>
    [AddComponentMenu("Echoes/Beat Warp (test only)")]
    public sealed class BeatWarp : MonoBehaviour
    {
        /// <summary>One named place worth being able to jump to.</summary>
        [System.Serializable]
        public sealed class Stop
        {
            public string name = "";
            [Tooltip("Where Ari lands. She is dropped here, not moved through " +
                     "anything.")]
            public Vector3 ari;
            [Tooltip("Where Mono lands, if he is awake. Leave at zero and he is " +
                     "put at Ari's feet.")]
            public Vector3 mono;
            [Tooltip("Optional. Looked up in the level's MonoHintLines and " +
                     "fired as if he had said it on his own, so a line can be " +
                     "heard without walking to the moment that triggers it.")]
            public string lineId = "";
        }

        [Header("The list")]
        [SerializeField] List<Stop> stops = new List<Stop>();

        [Header("Keys")]
        [SerializeField] KeyCode forward = KeyCode.N;
        [SerializeField] KeyCode back = KeyCode.B;

        [Header("Behaviour")]
        [Tooltip("How far above the stop Ari is dropped. Enough to clear the " +
                 "ground she lands on without letting her fall a visible " +
                 "distance.")]
        [Min(0f)] [SerializeField] float drop = 0.6f;

        [Tooltip("A beat that has not been built yet is skipped rather than " +
                 "dropping you into empty ground.")]
        [SerializeField] bool skipUnbuilt = true;

        [Header("State")]
        [SerializeField] int index;

        [Tooltip("Read by probes. Off by default in a build.")]
        [SerializeField] bool enabledInBuild = false;

        /// <summary>Where the warp currently is. Read by the setup tools.</summary>
        public int Index => index;

        /// <summary>How many stops are on the list.</summary>
        public int Count => stops.Count;

        /// <summary>The name of the current stop, for the log.</summary>
        public string CurrentName =>
            index >= 0 && index < stops.Count ? stops[index].name : "(none)";

        AriMover ari;
        MonoCompanion mono;

        void Awake()
        {
            // A build should not have a key that puts the player anywhere in
            // the level, and it should not have the stop list either.
            if (!Application.isEditor && !enabledInBuild)
            {
                enabled = false;
                return;
            }

            ari = Find<AriMover>();
            mono = Find<MonoCompanion>();
        }

        /// <summary>
        /// First component of a kind, asleep or awake.
        ///
        /// `FindObjectsByType` rather than `GameObject.Find`, for the reason
        /// that has bitten four tools in this project already: Mono is inactive
        /// from load until Beat 3 wakes him, and asleep is not the same as
        /// absent. It returns the first match rather than a specific one,
        /// because "which Mono" is not a question a test key should have to ask.
        /// </summary>
        static T Find<T>() where T : Component
        {
            var all = FindObjectsByType<T>(FindObjectsInactive.Include);
            return all.Length > 0 ? all[0] : null;
        }

        void Update()
        {
            if (Input.GetKeyDown(forward)) Go(1);
            if (Input.GetKeyDown(back)) Go(-1);
        }

        /// <summary>
        /// Move the index by a step and warp there.
        ///
        /// Steps round, because the list is short and the edges are where a
        /// player presses the key twice looking for a reaction.
        /// </summary>
        public void Go(int step)
        {
            if (stops == null || stops.Count == 0)
            {
                Debug.LogWarning("[Echoes] warp: no stops on the list");
                return;
            }

            index = ((index + step) % stops.Count + stops.Count) % stops.Count;
            Land(stops[index]);
        }

        /// <summary>
        /// Put the cast at a stop.
        ///
        /// Ari is placed and her velocity cleared. Zeroing velocity is not
        /// optional: she is a swept-capsule mover with her own momentum, and a
        /// teleport that leaves her running for a tenth of a second carries her
        /// straight through whatever the stop was meant to be showing.
        /// </summary>
        public void Land(Stop stop)
        {
            if (stop == null) return;

            Vector3 where = stop.ari + Vector3.up * drop;

            if (ari != null)
            {
                ari.Teleport(where);
            }
            else
            {
                Debug.LogWarning("[Echoes] warp: no AriMover in the scene");
            }

            if (mono != null)
            {
                // A stop that does not say where Mono goes means "at her feet".
                // He is awake by Beat 4 and there is nothing in the level for
                // him to be doing alone, and a follower left behind in the
                // last village is a man who never arrives at the next beat.
                Vector3 his = stop.mono.sqrMagnitude > 0.0001f
                    ? stop.mono
                    : where + new Vector3(0.8f, 0f, 0f);
                mono.WarpTo(his);
            }

            if (!string.IsNullOrEmpty(stop.lineId)) mono.SayBeat(stop.lineId);

            if (log)
                Debug.Log("[Echoes] warp -> " + index + " '" + stop.name +
                          "' at " + stop.ari.ToString("F2"), this);
        }

        [SerializeField] bool log = true;

#if UNITY_EDITOR
        /// <summary>
        /// Drop a stop where Ari is standing, named for the current hour.
        ///
        /// The alternative — reading coordinates off the Inspector and typing
        /// them into a script — is how a beat gets tested in one place and then
        /// forgets to be moved. A stop captured where you are looking is a stop
        /// you have already seen.
        /// </summary>
        [ContextMenu("Capture a stop here")]
        public void CaptureHere()
        {
            if (ari == null) ari = Find<AriMover>();
            if (ari == null) { Debug.LogWarning("[Echoes] warp: no AriMover"); return; }

            Vector3 p = ari.transform.position;

            var s = new Stop
            {
                name = "stop" + (stops.Count + 1) + "_" +
                       System.DateTime.Now.ToString("HHmmss"),
                ari = p,
                mono = mono != null ? mono.transform.position : p
            };

            stops.Add(s);
            Debug.Log("[Echoes] warp: captured '" + s.name + "' at " +
                      p.ToString("F2") + ". Set its name in the Inspector.");
        }
#endif
    }
}