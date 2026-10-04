using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Builds AnimatorControllers for Mono and the Ink Crawler.
    ///
    /// Neither character has one, and a Humanoid rig with no controller does not
    /// stand still — it sits in its bind pose, which for both of these is arms
    /// out and legs together, so a Mono that has "woken up" reads as a scarecrow.
    /// The clips being imported is not the same as the clips being played; only
    /// a graph puts them on.
    ///
    /// Two shapes, for two different reasons.
    ///
    /// Mono is a companion that mostly floats and hovers, so it is Idle and
    /// Walk on one float plus two one-shots — Wake, which is the beat, and Talk,
    /// which is the hint system. Talk is a one-shot rather than a second idle
    /// because the hint lines are dialogue: a line that can be interrupted
    /// halfway through by the next line is a line that arrives in the wrong
    /// order, and the hint ladder in Beat 6 fires three of them in a row.
    ///
    /// The Ink Crawler is a patrolling enemy, so it needs a speed range rather
    /// than a walk/stop pair: a Beat 5.5 patrol that only knows "moving" and
    /// "stopped" looks like it is being dragged along. Three locomotion states
    /// on one float, plus Crawl for the stealth route, plus Attack for the
    /// stagger and Death as a terminal with no way out.
    ///
    /// Death deliberately has no outgoing transition. A crawler that gets up
    /// again after being staggered is a bug the player will find on their own,
    /// and a backstop that returns it to Idle is what puts it there.
    /// </summary>
    public static class CastControllerBuilder
    {
        const string Dir = "Assets/Art/L1";

        const string MonoPath = Dir + "/Mono.controller";

        const string CrawlerPath = Dir + "/InkCrawler.controller";

        const string Report = "Temp/cast_controllers.txt";

        // The parameter names are spelled out at each AddParameter call rather
        // than held here as constants. There are seven of them across two
        // graphs and they are written out in the graph dump immediately
        // afterwards, which is the only place a designer will look — a constant
        // that has to be kept in step with a string literal in three places is
        // a third place to forget.
        //
        // Both graphs name their speed float "Speed", the same as Ari's, so a
        // patrol script and a mover use one name rather than two.

        /// <summary>
        /// Above this a creature is walking. Below it, standing. Shared so Mono
        /// and the crawler agree on what "moving" means.
        /// </summary>
        const float Walking = 0.1f;

        /// <summary>
        /// Above this the crawler is running rather than walking. Set high on
        /// purpose: the two clips are 0.70s and 2.80s, so the run cycle is
        /// about two and a half times as fast per step, and a threshold in the
        /// middle of the two makes it flip between them on ordinary patrol
        /// movement instead of only when it is actually hurrying.
        /// </summary>
        const float Running = 2.5f;

        [MenuItem("Tools/Echoes/Build Cast Controllers", priority = 64)]
        public static void Run()
        {
            var sb = new StringBuilder();
            var full = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(), Report);
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);

            var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { Dir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<AnimationClip>()
                .Where(c => !c.legacy && !c.name.StartsWith("__"))
                .GroupBy(c => c.name)
                .ToDictionary(g => g.Key, g => g.First());

            sb.AppendLine("clips available: " + clips.Count);
            foreach (var c in clips.Values.OrderBy(c => c.name))
                sb.AppendLine($"  {c.name}  {c.length:0.00}s  loop={c.isLooping}");

            BuildMono(clips, sb);
            BuildCrawler(clips, sb);

            // No SetDirty call. CreateAnimatorControllerAtPath marks its own
            // asset dirty, and EditorUtility.SetDirty(null) throws an
            // ArgumentNullException rather than being a harmless no-op — which
            // is what took the report write out below with it, leaving a run
            // that had already built both controllers looking like a total
            // failure.
            AssetDatabase.SaveAssets();

            // Written before anything that could start a domain reload, because
            // everything after that point in this run is lost.
            try
            {
                System.IO.File.WriteAllText(full, sb.ToString());
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Echoes] could not write " + Report + ": " + e.Message);
            }

            Debug.Log("[Echoes] cast controllers built\n" + sb);
        }

        // --- Mono ----------------------------------------------------------------

        static void BuildMono(System.Collections.Generic.Dictionary<string, AnimationClip> clips,
                              StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== Mono ===");

            var wanted = new[] { "Mono_Idle", "Mono_Walk", "Mono_Talk", "Mono_Wake" };
            var missing = wanted.Where(w => !clips.ContainsKey(w)).ToArray();
            if (missing.Length > 0)
            {
                sb.AppendLine("  MISSING: " + string.Join(", ", missing) + " — not built");
                return;
            }

            Replace(MonoPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(MonoPath);

            ctrl.AddParameter(Float("Speed", 0f));
            ctrl.AddParameter(Trigger("Awake"));
            ctrl.AddParameter(Trigger("Talk"));

            var machine = ctrl.layers[0].stateMachine;
            var states = wanted.ToDictionary(
                n => n,
                n => State(machine, n, clips[n]));

            machine.defaultState = states["Mono_Idle"];

            // --- the wake. This is Beat 3, and it is the one transition in this
            // graph that must be unmissable, so it is wired from both idle and
            // walk rather than only from the one the player happens to be in.
            Fire(states["Mono_Idle"], states["Mono_Wake"], "Awake", 0.1f);
            Fire(states["Mono_Walk"], states["Mono_Wake"], "Awake", 0.1f);
            Finish(states["Mono_Wake"], states["Mono_Idle"], 0.4f);

            // --- dialogue ---------------------------------------------------------
            // Fires from either state so a line can be triggered while Mono is
            // moving. Mono hovers rather than walks on the ground, so a line
            // that only played from Idle would be skipped every time the hint
            // ladder fired while he was following Ari.
            Fire(states["Mono_Idle"], states["Mono_Talk"], "Talk", 0.15f);
            Fire(states["Mono_Walk"], states["Mono_Talk"], "Talk", 0.15f);

            // Back to Idle on the line finishing, and a second leg back to Walk
            // for the same reason Ari's landing has two: Unity takes the first
            // transition whose conditions pass, and a Mono that stops dead at
            // the end of every sentence reads as a machine.
            Finish(states["Mono_Talk"], states["Mono_Walk"], 0.2f, "Speed", AnimatorConditionMode.Greater, Walking);
            Finish(states["Mono_Talk"], states["Mono_Idle"], 0.2f, "Speed", AnimatorConditionMode.Less, Walking);

            // --- locomotion --------------------------------------------------------
            Blend(states["Mono_Idle"], states["Mono_Walk"], AnimatorConditionMode.Greater, 0.15f);
            Blend(states["Mono_Walk"], states["Mono_Idle"], AnimatorConditionMode.Less, 0.2f);

            // The wake used to ship at its full 11.17s, which made a tutorial
            // beat look like it was loading, and this note is where that was
            // flagged. It is trimmed now: WakeClipTrim's chosen-window trim cut
            // it to frames 152..241, 2.97s, which is the busiest three seconds
            // in the take — it carries 49.3% of the movement in 27% of the time.
            // The transition above needs no change for that, because Finish
            // leaves on exitTime 1, which is the end of whatever the clip
            // currently is rather than a number of seconds.
            sb.AppendLine("  NOTE Mono_Wake is " + clips["Mono_Wake"].length.ToString("0.00") +
                          "s, trimmed to its busiest window (5.07s-8.03s of the " +
                          "11.17s take). MonoChase's handoverDelay is 3.2s, which " +
                          "leaves the wake on screen before the camera moves.");

            Dump(ctrl, MonoPath, sb);
        }

        // --- Ink Crawler ---------------------------------------------------------

        static void BuildCrawler(System.Collections.Generic.Dictionary<string, AnimationClip> clips,
                                 StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== Ink Crawler ===");

            // Two idles arrived. The 6.10s one is the default because a longer
            // cycle reads less obviously as a loop; the 1.97s one is kept and
            // reachable on a trigger rather than deleted, so a designer can
            // change the pacing of a patrol without a re-import.
            var wanted = new[] { "Crawler_Idle", "Crawler_Idle2", "Crawler_Walk",
                                 "Crawler_Run", "Crawler_Crawl", "Crawler_Attack",
                                 "Crawler_Death" };
            var missing = wanted.Where(w => !clips.ContainsKey(w)).ToArray();
            if (missing.Length > 0)
            {
                sb.AppendLine("  MISSING: " + string.Join(", ", missing) + " — not built");
                return;
            }

            Replace(CrawlerPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CrawlerPath);

            ctrl.AddParameter(Float("Speed", 0f));
            ctrl.AddParameter(Flag("Crawling", false));
            ctrl.AddParameter(Trigger("Attack"));
            ctrl.AddParameter(Trigger("Die"));
            ctrl.AddParameter(Trigger("Idle2"));

            var machine = ctrl.layers[0].stateMachine;
            var states = wanted.ToDictionary(
                n => n,
                n => State(machine, n, clips[n]));

            machine.defaultState = states["Crawler_Idle"];

            // The second idle. It was imported, it is a different 1.97s pacing
            // from the 6.10s default, and leaving it unwired made it a state
            // in the graph that no transition could ever reach — a controller
            // that reads as complete in the inspector and plays the same loop
            // forever. A trigger rather than a bool, because swapping a
            // creature's idle while it is being looked at is a one-off and a
            // bool invites the level script to leave it stuck.
            Fire(states["Crawler_Idle"], states["Crawler_Idle2"], "Idle2", 0.25f);
            Fire(states["Crawler_Idle2"], states["Crawler_Idle"], "Idle2", 0.25f);
            sb.AppendLine("  Crawler_Idle2 is reachable on the 'Idle2' trigger in both " +
                          "directions, so it is a state a designer can use rather than one " +
                          "the graph merely contains.");

            var dying = new[] { "Crawler_Idle", "Crawler_Walk", "Crawler_Run", "Crawler_Crawl" };

            // Death first on every state. Unity evaluates a state's transitions
            // in order and takes the first whose conditions all pass, so a
            // crawler with its Die trigger set and a non-zero Speed would take
            // the locomotion edge and walk off still alive. Every other
            // transition from these states is added after this loop for exactly
            // that reason.
            foreach (var name in dying)
                Fire(states[name], states["Crawler_Death"], "Die", 0.1f);

            // Attack, for the stagger. The crawler cannot be killed in Level 1 —
            // there is no colour yet — so this is the brush-splash from Beat 5
            // and it is the only way the player can affect one.
            foreach (var name in new[] { "Crawler_Idle", "Crawler_Walk", "Crawler_Run" })
                Fire(states[name], states["Crawler_Attack"], "Attack", 0.1f);

            // The stagger plays out and drops back to standing. Into Idle rather
            // than back to the patrol, because a crawler that resumes walking
            // from mid-attack does so on the wrong foot and looks like it was
            // never hit at all.
            Finish(states["Crawler_Attack"], states["Crawler_Idle"], 0.25f);

            // Locomotion, by speed.
            Blend(states["Crawler_Idle"], states["Crawler_Walk"], AnimatorConditionMode.Greater, 0.18f);
            Blend(states["Crawler_Walk"], states["Crawler_Idle"], AnimatorConditionMode.Less, 0.22f);
            Blend(states["Crawler_Walk"], states["Crawler_Run"], AnimatorConditionMode.Greater, 0.2f, Running);
            Blend(states["Crawler_Run"], states["Crawler_Walk"], AnimatorConditionMode.Less, 0.2f, Running);

            // Crawling. A bool rather than a speed range, because a crawl is not
            // slower walking — it is a different thing the crawler does, and the
            // stealth route in Beat 5.5 needs it to be switchable while the
            // crawler is standing still.
            Toggle(states["Crawler_Idle"], states["Crawler_Crawl"], "Crawling", true, 0.3f);
            Toggle(states["Crawler_Crawl"], states["Crawler_Idle"], "Crawling", false, 0.3f);

            // Crawler_Death has no outgoing transitions at all, on purpose.
            sb.AppendLine("  Crawler_Death is terminal: " +
                          states["Crawler_Death"].transitions.Length + " outgoing transition(s). " +
                          "A staggered crawler that gets back up is a bug the player finds for you.");

            Dump(ctrl, CrawlerPath, sb);
        }

        // --- graph helpers -------------------------------------------------------

        /// <summary>
        /// AnimatorController has AddParameter(string) and AddParameter(parameter)
        /// and no three-argument form, so a type and a default have to be packed
        /// into a parameter object. Written once here rather than inline four
        /// times, because the object initializer form is easy to get subtly
        /// wrong — a float whose defaultFloat is left at 0 and a trigger whose
        /// type is left at Float both fail silently, and a graph with a
        /// parameter no transition can ever match behaves exactly like a graph
        /// with the wrong transition conditions.
        /// </summary>
        static AnimatorControllerParameter Float(string name, float value) =>
            new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = value
            };

        static AnimatorControllerParameter Flag(string name, bool value) =>
            new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Bool,
                defaultBool = value
            };

        static AnimatorControllerParameter Trigger(string name) =>
            new AnimatorControllerParameter
            {
                name = name,
                type = AnimatorControllerParameterType.Trigger
            };

        /// <summary>
        /// Delete a controller if one is there, so a rebuild cannot leave orphan
        /// states behind. Safe here and not safe for Ari: this runs before
        /// anything in the scene points at these two, and Ari's builder has a
        /// repoint step for exactly the reason that deleting hers is not safe.
        /// </summary>
        static void Replace(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        static AnimatorState State(AnimatorStateMachine machine, string name, AnimationClip clip)
        {
            var s = machine.AddState(name);
            s.motion = clip;
            // Off, consistently with Ari's graph. With it on, every property the
            // state does not animate is written back at 0 every frame, which
            // fights whatever else is writing those properties and produces a
            // character that slowly deflates towards its bind pose.
            s.writeDefaultValues = false;
            return s;
        }

        /// <summary>
        /// A speed-driven crossfade between two locomotion states.
        ///
        /// Both graphs deliberately name their float Speed, the same as Ari's,
        /// so a patrol script and a mover use one name rather than two and a
        /// level designer is not reading a glossary.
        /// </summary>
        static void Blend(AnimatorState from, AnimatorState to, AnimatorConditionMode mode,
                          float seconds, float threshold = Walking)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.hasFixedDuration = true;   // seconds, not a fraction of the clip
            t.duration = seconds;
            t.exitTime = 0f;
            t.canTransitionToSelf = false;
            t.AddCondition(mode, threshold, "Speed");
        }

        /// <summary>A trigger edge, from one state to another.</summary>
        static void Fire(AnimatorState from, AnimatorState to, string trigger, float seconds)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.hasFixedDuration = true;
            t.duration = seconds;
            t.exitTime = 0f;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        }

        /// <summary>A bool toggle.</summary>
        static void Toggle(AnimatorState from, AnimatorState to, string flag, bool value,
                           float seconds)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.hasFixedDuration = true;
            t.duration = seconds;
            t.exitTime = 0f;
            t.canTransitionToSelf = false;
            t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                           0f, flag);
        }

        /// <summary>
        /// A one-shot leaving on its own end, optionally guarded by a condition.
        ///
        /// exitTime is 1 with a zero-second condition wait, so it leaves on the
        /// frame the clip ends rather than a fixed number of seconds later — the
        /// difference matters when the clip's length is something like Mono's
        /// 5.93s talk.
        /// </summary>
        static void Finish(AnimatorState from, AnimatorState to, float blendSeconds,
                           string parameter = null, AnimatorConditionMode mode = default,
                           float threshold = 0f)
        {            var t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime = 1f;
            t.hasFixedDuration = true;
            t.duration = blendSeconds;
            t.canTransitionToSelf = false;
            if (!string.IsNullOrEmpty(parameter)) t.AddCondition(mode, threshold, parameter);
        }

        static void Dump(AnimatorController ctrl, string path, StringBuilder sb)
        {
            sb.AppendLine("  " + path);
            foreach (var p in ctrl.parameters)
                sb.AppendLine($"    parameter {p.name} ({p.type})" +
                              (p.type == AnimatorControllerParameterType.Float
                                  ? " default=" + p.defaultFloat : "") +
                              (p.type == AnimatorControllerParameterType.Bool
                                  ? " default=" + p.defaultBool : ""));

            var machine = ctrl.layers[0].stateMachine;
            sb.AppendLine("    default state: " + machine.defaultState.name);

            foreach (var child in machine.states)
            {
                var s = child.state;
                var clip = s.motion as AnimationClip;
                sb.AppendLine($"    state '{s.name}' motion='{s.motion?.name}' " +
                              $"len={clip?.length:0.00}s loop={clip?.isLooping} " +
                              $"out={s.transitions.Length}");

                foreach (var t in s.transitions)
                {
                    var conds = string.Join(" AND ", t.conditions.Select(
                        c => $"{c.parameter} {c.mode} {c.threshold}"));
                    sb.AppendLine($"        -> '{t.destinationState?.name}' " +
                                  $"[{conds}] exitTime={t.exitTime:0.00} " +
                                  $"hasExitTime={t.hasExitTime} duration={t.duration:0.00}");
                }
            }
        }
    }
}
