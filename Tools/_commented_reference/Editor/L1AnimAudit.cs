using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Reads the three animator controllers and the scene's Animator
    /// assignments, and says which clips exist but are never referenced.
    ///
    /// <para><b>Why this exists.</b> Mono walks like a statue, and the cause is
    /// not a missing model — <c>Mono_Walk.fbx</c>, <c>Mono_Wake.fbx</c>,
    /// <c>Mono_Talk.fbx</c> and <c>Ari_Collect.fbx</c> are all in the project.
    /// The assets are there and the character is not moving, which points at the
    /// controller: a clip that is not in a state machine is a file on disk that
    /// nothing can play.</para>
    ///
    /// <para><b>It also counts the clips nothing references.</b> That is the
    /// number that turns "Mono looks like a statue" from a description into a
    /// diagnosis, because it names the specific clips that are being wasted
    /// rather than asserting that something is misconfigured.</para>
    ///
    /// <para><b>Nothing here changes anything.</b> It reports; a separate pass
    /// fixes.</para>
    /// </summary>
    public static class L1AnimAudit
    {
        const string Report = "Temp/l1_anim.txt";

        [MenuItem("Tools/Echoes/Audit the Animations", priority = 70)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] audit the animation controllers");

            if (SceneManager.GetActiveScene().rootCount == 0)
            {
                sb.AppendLine("  FATAL: no scene open.");
                Finish(sb);
                return;
            }

            // --- the controllers ------------------------------------------------
            var wanted = new List<string> { "Mono", "Ari", "InkCrawler" };

            for (int i = 0; i < wanted.Count; i++)
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    "Assets/Art/L1/" + wanted[i] + ".controller")
                    ?? AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    "Assets/Art/Ari/" + wanted[i] + ".controller");

                if (ctrl == null)
                {
                    sb.AppendLine();
                    sb.AppendLine("=== " + wanted[i] + ".controller NOT FOUND ===");
                    continue;
                }

                sb.AppendLine();
                sb.AppendLine("=== " + wanted[i] + " (" + ctrl.name + ") ===");

                // parameters — what the code is allowed to drive
                sb.AppendLine("  parameters (" + ctrl.parameters.Length + "):");
                for (int p = 0; p < ctrl.parameters.Length; p++)
                    sb.AppendLine("    " + ctrl.parameters[p].name + " : " +
                                  ctrl.parameters[p].type +
                                  (ctrl.parameters[p].type == AnimatorControllerParameterType.Trigger ? "" :
                                   ctrl.parameters[p].defaultFloat.ToString("0.00")));

                // states — what can actually play
                for (int L = 0; L < ctrl.layers.Length; L++)
                {
                    var sm = ctrl.layers[L].stateMachine;
                    sb.AppendLine("  layer '" + ctrl.layers[L].name + "', " +
                                  sm.states.Length + " state(s), " +
                                  sm.anyStateTransitions.Length + " from Any State:");

                    for (int s = 0; s < sm.states.Length; s++)
                    {
                        var st = sm.states[s].state;
                        var clip = st.motion as AnimationClip;
                        sb.AppendLine("    " + st.name.PadRight(20) +
                                      (clip != null
                                          ? "clip '" + clip.name + "' " +
                                            clip.length.ToString("0.00") + "s"
                                          : st.motion == null
                                            ? "NO MOTION — it plays nothing"
                                            : "motion is a " + st.motion.GetType().Name));

                        // The outgoing transitions, with their conditions.
                        //
                        // This is the part that was missing and it is the part
                        // that decides whether assigning a controller fixes
                        // anything at all. A state machine with states and no
                        // transitions plays the first state forever: assign it
                        // to Mono and Mono stands in Idle — a statue with
                        // better posture. "There are states" is not "there is
                        // a way to change state".
                        var outTr = st.transitions;
                        if (outTr == null || outTr.Length == 0)
                        {
                            sb.AppendLine("        -> NOWHERE. This state never " +
                                          "exits, so the character is stuck in it " +
                                          "forever no matter what the code sets.");
                            continue;
                        }

                        for (int t = 0; t < outTr.Length; t++)
                        {
                            var tr = outTr[t];
                            string cond = tr.hasExitTime
                                ? "exitTime " + tr.exitTime.ToString("0.00")
                                : "instant";

                            if (tr.conditions != null && tr.conditions.Length > 0)
                            {
                                var cs = new List<string>();
                                for (int q = 0; q < tr.conditions.Length; q++)
                                    cs.Add(tr.conditions[q].parameter + " " +
                                           tr.conditions[q].mode + " " +
                                           tr.conditions[q].threshold.ToString("0.00"));
                                cond += " if " + string.Join(" and ", cs.ToArray());
                            }
                            else if (!tr.hasExitTime)
                            {
                                cond += ", NO CONDITION — it fires the moment " +
                                        "this state is entered";
                            }

                            sb.AppendLine("        -> " +
                                          (tr.destinationState != null
                                              ? tr.destinationState.name
                                              : "EXIT (leaves the machine)") +
                                          "   " + cond);
                        }
                    }
                }

                // which clips on disk does this controller never mention?
                var unused = UnreferencedClips(wanted[i]);
                sb.AppendLine("  clips in the project this controller NEVER uses:");
                if (unused.Count == 0) sb.AppendLine("    none — every clip is wired");
                else
                    for (int u = 0; u < unused.Count; u++)
                        sb.AppendLine("    " + unused[u] + "   <-- on disk, unreachable");
            }

            // --- who has what assigned -------------------------------------------
            sb.AppendLine();
            sb.AppendLine("=== IN THE SCENE ===");

            Who(sb, "Mono",
                Object.FindAnyObjectByType<MonoCompanion>(FindObjectsInactive.Include) != null
                ? Object.FindAnyObjectByType<MonoCompanion>(FindObjectsInactive.Include).gameObject
                : null);

            var ari = Object.FindAnyObjectByType<AriMover>(FindObjectsInactive.Include);
            Who(sb, "Ari", ari != null ? ari.gameObject : null);

            var crawlers = Object.FindObjectsByType<InkCrawler>(FindObjectsInactive.Include);
            sb.AppendLine();
            sb.AppendLine("  InkCrawler x" + crawlers.Length);
            for (int i = 0; i < crawlers.Length; i++)
            {
                var an = crawlers[i].GetComponentInChildren<Animator>(true);
                sb.AppendLine("    " + crawlers[i].name.PadRight(20) +
                              " controller: " + (an == null
                                  ? "NO ANIMATOR"
                                  : an.runtimeAnimatorController == null
                                    ? "NONE — it holds its bind pose, which is " +
                                      "exactly what a statue looks like"
                                    : an.runtimeAnimatorController.name));
            }

            Finish(sb);
        }

        static void Who(StringBuilder sb, string who, GameObject go)
        {
            var an = go != null ? go.GetComponentInChildren<Animator>(true) : null;

            sb.AppendLine("  " + who.PadRight(8) + " '" + (go != null ? go.name : "NOT FOUND") +
                          "'  animator: " + (an == null
                              ? "NONE"
                              : an.runtimeAnimatorController == null
                                ? "NO CONTROLLER — this is the statue"
                                : an.runtimeAnimatorController.name));
        }

        /// <summary>
        /// Every fbx clip whose name starts with this character, minus the ones
        /// the controller actually contains.
        /// </summary>
        static List<string> UnreferencedClips(string who)
        {
            var used = new HashSet<string>();
            string path = "Assets/Art/L1/" + who + ".controller";
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path)
                       ?? AssetDatabase.LoadAssetAtPath<AnimatorController>(
                           "Assets/Art/Ari/" + who + ".controller");

            if (ctrl != null)
                for (int L = 0; L < ctrl.layers.Length; L++)
                {
                    var sm = ctrl.layers[L].stateMachine;
                    for (int s = 0; s < sm.states.Length; s++)
                    {
                        var c = sm.states[s].state.motion as AnimationClip;
                        if (c != null) used.Add(c.name);
                    }

                    foreach (var t in sm.anyStateTransitions)
                    {
                        var c = t.destinationState != null
                            ? t.destinationState.motion as AnimationClip : null;
                        if (c != null) used.Add(c.name);
                    }
                }

            var found = new List<string>();
            string[] dirs = { "Assets/Art/L1", "Assets/Art/Ari" };
            for (int d = 0; d < dirs.Length; d++)
            {
                if (!Directory.Exists(dirs[d])) continue;

                foreach (var f in Directory.GetFiles(dirs[d], who + "_*.fbx"))
                {
                    string n = Path.GetFileNameWithoutExtension(f);
                    if (!used.Contains(n)) found.Add(n);
                }
            }

            found.Sort();
            return found;
        }

        static void Finish(StringBuilder sb)
        {
            File.WriteAllText(
                Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
            Debug.Log("[Echoes] animation audit — see " + Report);
        }
    }
}
