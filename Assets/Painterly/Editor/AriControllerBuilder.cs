using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Builds Ari's AnimatorController from the imported clips.
    ///
    /// Three states. Idle and Walk crossfade on a single Speed float rather than
    /// on a boolean. A bool forces a hard cut, and a hard cut between a
    /// standing pose and a mid-stride pose reads as a glitch every time Ari
    /// changes speed. One float covers the whole range from standing to running
    /// and leaves room for a jog later without touching the graph.
    ///
    /// Jump is not on Speed. It is a one-shot on a trigger, and it leaves on
    /// whether the movement code says she is on the ground — not on where the
    /// clip happens to be, because the arc the physics produces and the arc the
    /// animation was drawn for are the same shape but not the same duration.
    ///
    /// The locomotion transitions deliberately ignore exit time. Exit time makes
    /// a state wait for a fixed point in the source clip before leaving, which
    /// is right for a one-shot attack and wrong for locomotion: it would make
    /// Ari start walking up to a third of a stride late, and keep walking a
    /// stride after she stopped.
    ///
    /// Root motion is off. The walk clip's root barely moves (0.040 units of
    /// drift over a 1.033s cycle) and the jump's does not move at all, so there
    /// is nothing to extract, and leaving it off keeps the Animator from
    /// fighting whatever moves Ari.
    /// </summary>
    public static class AriControllerBuilder
    {
        const string ControllerPath = "Assets/Art/Ari/Ari.controller";
        const string ModelsDir = "Assets/Art/Ari/Models";

        /// <summary>
        /// Where the rest of Ari's clips live.
        ///
        /// The attack clip arrived with the rest of the Level 1 import and was
        /// put in the shared L1 folder next to the cast's FBX, so searching only
        /// ModelsDir found three clips and reported the fourth as missing. A
        /// search scope that is narrower than where the assets are is a missing
        /// clip that looks exactly like an unwired one.
        /// </summary>
        static readonly string[] ClipDirs = { ModelsDir, "Assets/Art/L1" };

        const string Report = "Temp/ari_controller.txt";

        /// <summary>Name of the float the movement code writes to.</summary>
        public const string SpeedParam = "Speed";

        /// <summary>Above this, Ari is walking. Below, she is standing.</summary>
        public const float Threshold = 0.1f;

        const float BlendSeconds = 0.15f;

        const string IdleClip = "Ari_Idle";
        const string WalkClip = "Ari_Walk";
        const string JumpClip = "Ari_Jump";
        /// <summary>
        /// The brush swing's motion. Chosen by the player, not by the survey.
        ///
        /// SwingClipSurvey found no imported Ari clip that is authored as a
        /// brush sweep: every candidate fails on how long the hand takes to
        /// reach the canvas, how much of the gesture a short window can hold,
        /// or whether the feet move while it happens. Ari_HitReact was being
        /// used as the least bad stand-in, and it still read as a hit-react.
        ///
        /// Ari_Attack was requested instead. What it measures (SwingClipProbe on
        /// this clip):
        ///
        ///   length            2.40s, 72 frames at 30fps
        ///   hand travel       0.866m across the clip
        ///   hand starts       frame 2   (0.07s) — the arm moves almost at once
        ///   hand peak         frame 29  (0.97s from the click, 0.34 m/frame)
        ///   motion ends       frame 60  (2.00s); the last 13% is a held pose
        ///
        /// The honest problem with it: 0.97s from the click to the strike is a
        /// long wind-up for a brush press, and the movement is spread over
        /// 2.00s, so even the best 0.70s window can only hold 54% of it. There
        /// is no speed that fixes both — speeding up to reach the peak on time
        /// also shortens the sweep until it reads as a flick. SwingSpeed is set
        /// below to put the peak at 0.24s, which answers the click promptly and
        /// keeps the stroke long enough to read as an arm movement rather than a
        /// pop.
        ///
        /// When a real brush clip arrives, change this one line. SwingSpeed,
        /// SwingBackExitTime and AriMover.swingSeconds all follow from it.
        /// </summary>
        const string SwingClip = "Ari_Attack";

        /// <summary>
        /// Playback speed for the swing, chosen so the strike lands on time.
        ///
        /// This is a different lever from trimming. A trim throws motion away to
        /// buy time; a speed change keeps every frame of the gesture and only
        /// spends the clock, which is the right tool when no window holds the
        /// whole movement.
        ///
        /// Derived from the measured peak, not typed in. The strike is at 0.97s
        /// of clip time and the limit is 0.18s of real time, and 0.97/0.18 is
        /// 5.4 — but at 5.4x the main sweep collapses from 0.17s to 0.03s and
        /// stops reading as a gesture at all. 4.0x puts the peak at 0.242s and
        /// keeps the sweep at 0.04s, which is the compromise between answering
        /// the click and keeping the arm readable.
        ///
        /// A correction worth keeping: the earlier survey reported this clip's
        /// peak as "0.43s after the click" and derived 2.41x from it. That 0.43s
        /// is the gap between the window's START and the peak, not between the
        /// click and the peak. The real wait is 0.97s. Both the 0.43 figure and
        /// the speed derived from it were wrong, and the probe's own per-frame
        /// table showed 0.97s all along.
        /// </summary>
        const float SwingSpeed = 4.0f;

        /// <summary>
        /// Where the swing state gives up waiting for the player and blends out
        /// on its own.
        ///
        /// Normalised to the clip, so it does not move when SwingSpeed changes.
        /// It sits at the END of the frame window that actually contains the
        /// stroke — frames 16 to 36 of 72 — so the arm is never cut off with the
        /// brush still travelling, and never held after it has arrived.
        ///
        /// 36/72 is 0.514. SwingBackExitTime therefore agrees with
        /// AriMover.swingSeconds by construction rather than by two numbers
        /// happening to match.
        /// </summary>
        const float SwingBackExitTime = 0.514f;

        /// <summary>
        /// The brush swing. This is the one clip the player causes directly.
        ///
        /// A trigger, like the jump, and for a sharper reason: it fires from a
        /// click that can be repeated every cooldown, and a bool would keep the
        /// graph pinned in the swing for as long as it stayed set. She would
        /// brush-paint with her arm permanently up.
        /// </summary>
        public const string SwingParam = "Swing";

        /// <summary>
        /// Bool the controller ends the swing on. Must match AriMover.
        ///
        /// This exists because Speed cannot do the job. A return leg guarded on
        /// "Speed Less 0.1" is already true the instant Ari is standing still,
        /// so the graph would leave the swing on the very next frame and the
        /// stroke would flash for a twelfth of a second. The jump avoids this by
        /// returning on Grounded, which is false for the whole jump and only
        /// becomes true when she lands — a fresh event. The swing needs the same
        /// thing, so AriMover publishes how long the stroke is for and the graph
        /// waits for that flag to fall.
        /// </summary>
        public const string SwingingParam = "Swinging";

        /// <summary>
        /// How long the swing blends out. Long enough to read as a follow-through
        /// rather than a cut, short enough that a second stroke can land inside
        /// the first one's recovery.
        /// </summary>
        const float SwingBlendSeconds = 0.12f;

        /// <summary>
        /// How fast the swing blends in. Almost nothing. A brush stroke that
        /// eases into its own arc reads as a slow wind-up, and the click has
        /// already happened by the time the arm moves.
        /// </summary>
        const float SwingInSeconds = 0.02f;

        /// <summary>
        /// Trigger AriMover sets on the frame a jump is accepted.
        ///
        /// A trigger rather than a bool, because a bool is a level and not an
        /// edge: with a bool the Idle-to-Jump condition stays true for as long as
        /// the key is held, so the moment she lands it fires again and she jumps
        /// without the key being pressed a second time. A trigger is consumed by
        /// the transition that uses it and resets itself.
        /// </summary>
        public const string JumpParam = "Jump";

        /// <summary>
        /// Bool AriMover keeps true while she is on something solid.
        ///
        /// This is what ends the jump. A transition driven by exit time alone has
        /// to guess when she lands, and the guess is only right if the jump
        /// height and the clip length happen to agree. They do not, and when they
        /// disagree she either hangs in the air for the rest of the clip or drops
        /// out of the pose mid-hop. The movement code already knows the exact
        /// frame she touches down, so it says so.
        /// </summary>
        public const string GroundedParam = "Grounded";

        /// <summary>
        /// How long the jump blends out on landing. Short, because the blend is
        /// covering the difference between where the physics put her down and
        /// where the last frame of the clip is; a long one reads as a slow
        /// motion recovery from the fall.
        /// </summary>
        const float LandBlendSeconds = 0.12f;

        /// <summary>
        /// How long the jump blends in. Nearly nothing, because a jump has to
        /// read as instant. At 0.05s she is already off the ground before the
        /// blend out of idle has finished.
        /// </summary>
        const float LaunchBlendSeconds = 0.05f;

        [MenuItem("Tools/Echoes/Build Ari Controller", priority = 61)]
        public static void Run()
        {
            // LoadAllAssetsAtPath takes a single asset, not a folder, so the
            // folder has to be searched rather than handed to it directly.
            // The __preview__ clips the importer also generates are filtered out;
            // they are the same takes again and would double every name.
            var found = ClipDirs
                .SelectMany(dir => AssetDatabase.FindAssets("t:AnimationClip", new[] { dir }))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<AnimationClip>()
                .Where(c => !c.legacy && !c.name.StartsWith("__"))
                .ToArray();

            // All four are required. AriMover publishes the Jump and Swing
            // triggers on every frame it accepts one, so a controller built
            // without those states does not fail here — it fails at play time,
            // as a trigger sent to a graph that has no such parameter, which is
            // a warning in a console nobody is reading.
            var wanted = new[] { IdleClip, WalkClip, JumpClip, SwingClip };

            var clips = wanted
                .Select(name => found.FirstOrDefault(c => c.name == name))
                .ToArray();

            var missing = wanted
                .Where((_, i) => clips[i] == null).ToArray();

            if (missing.Length > 0)
            {
                Debug.LogError("[Echoes] Ari controller: missing clip(s) " +
                    string.Join(", ", missing) +
                    ". Clips actually present: " +
                    (found.Length == 0 ? "<none>" : string.Join(", ", found.Select(c => $"'{c.name}'"))) +
                    ". Run Tools/Echoes/Import Ari first.");
                return;
            }

            // Recreated rather than edited. Patching an existing graph in place
            // leaves orphan states and duplicate transitions behind, and this
            // graph is small enough to rebuild outright. Nothing references the
            // controller yet, so deleting it is safe; once something does, this
            // has to become an in-place update.
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
                AssetDatabase.DeleteAsset(ControllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = SpeedParam,
                type = AnimatorControllerParameterType.Float,
                defaultFloat = 0f
            });
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = JumpParam,
                type = AnimatorControllerParameterType.Trigger
            });
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = SwingParam,
                type = AnimatorControllerParameterType.Trigger
            });
            controller.AddParameter(new AnimatorControllerParameter
            {
                name = SwingingParam,
                type = AnimatorControllerParameterType.Bool,
                defaultBool = false
            });
            controller.AddParameter(new AnimatorControllerParameter
            {
                // True by default, so an Animator driven by anything other than
                // AriMover — a cutscene, an editor preview — reads as standing
                // rather than as permanently falling.
                name = GroundedParam,
                type = AnimatorControllerParameterType.Bool,
                defaultBool = true
            });

            var machine = controller.layers[0].stateMachine;

            var states = clips.ToDictionary(
                c => c.name,
                c =>
                {
                    var s = machine.AddState(c.name);
                    s.motion = c;
                    s.writeDefaultValues = false;
                    return s;
                });

            // The swing plays faster than realtime. Set on the state rather than
            // on the clip, because a speed change is a property of how this
            // gesture is used and not of the asset — the same clip still plays
            // at 1x anywhere else, and trimming the clip instead would discard
            // a third of the arm movement to achieve what this one number does
            // without losing any of it.
            states[SwingClip].speed = SwingSpeed;

            machine.defaultState = states[IdleClip];

            // Transitions hang off the source state, not off the machine.
            CrossFade(states[IdleClip], states[WalkClip], AnimatorConditionMode.Greater);
            CrossFade(states[WalkClip], states[IdleClip], AnimatorConditionMode.Less);

            // --- the jump, out and back -----------------------------------------
            // Out of locomotion, on the trigger, from either state, and with no
            // exit time: a hop starts on the frame the key goes down, not
            // whenever the current walk cycle happens to reach a chosen point.
            Launch(states[IdleClip], states[JumpClip]);
            Launch(states[WalkClip], states[JumpClip]);

            // Back down. The walking leg is listed first because Unity takes the
            // first transition whose conditions all pass: landing while still
            // holding a direction has to reach Walk, and it never would if the
            // walking transition were considered second.
            Land(states[JumpClip], states[WalkClip], AnimatorConditionMode.Greater);
            Land(states[JumpClip], states[IdleClip], AnimatorConditionMode.Less);

            // Backstop. If Grounded is never set true — AriMover removed, the
            // component erroring, the parameter renamed on the component while
            // the controller kept its own name — the graph has no way out of
            // Jump at all and she is frozen in mid-hop for the rest of the
            // level. Letting the clip end on its own returns control.
            var backstop = states[JumpClip].AddTransition(states[IdleClip]);
            backstop.hasExitTime = true;
            backstop.exitTime = 1f;
            backstop.hasFixedDuration = true;
            backstop.duration = LandBlendSeconds;
            backstop.canTransitionToSelf = false;

            // --- the brush swing, out and back -----------------------------------
            // Out of locomotion, on the trigger, from either state, and with no
            // exit time — the arm has to move on the frame the player clicks,
            // not whenever the walk cycle reaches a chosen point.
            Swing(states[IdleClip], states[SwingClip]);
            Swing(states[WalkClip], states[SwingClip]);

            // Straight out of a swing into a hop. Listed before the return legs
            // because Unity takes the first transition whose conditions all
            // pass, and a jump requested mid-stroke has to win.
            var swingToJump = states[SwingClip].AddTransition(states[JumpClip]);
            swingToJump.hasExitTime = false;
            swingToJump.hasFixedDuration = true;
            swingToJump.duration = SwingBlendSeconds;
            swingToJump.canTransitionToSelf = false;
            swingToJump.AddCondition(AnimatorConditionMode.If, 0f, JumpParam);

            // Back to locomotion on Speed, the same pair the jump uses, so a
            // stroke taken while running returns her to a stride rather than to
            // a standstill.
            SwingBack(states[SwingClip], states[WalkClip], AnimatorConditionMode.Greater);
            SwingBack(states[SwingClip], states[IdleClip], AnimatorConditionMode.Less);

            // Backstop, for the same reason the jump has one: if the Swinging
            // flag is never dropped — the component was disabled mid-stroke, or
            // the parameter was renamed — there is otherwise no way out and her
            // arm stays up for the rest of the level.
            //
            // Late enough that a real stroke always leaves by the Swinging flag
            // first and never sees this, early enough that the failure it exists
            // for is a blink rather than a freeze.
            //
            // It sits at the end of the frame window that holds the stroke
            // rather than at the end of the clip, because Ari_Attack is 2.40s
            // long but its motion is over by 2.00s and the useful part is frames
            // 16 to 36. Leaving at the end of the clip would hold a finished pose
            // for another 0.10s of wall clock at this speed; leaving inside the
            // window would cut the brush off while it was still travelling.
            var swingOut = states[SwingClip].AddTransition(states[IdleClip]);
            swingOut.hasExitTime = true;
            swingOut.exitTime = SwingBackExitTime;
            swingOut.hasFixedDuration = true;
            swingOut.duration = SwingBlendSeconds;
            swingOut.canTransitionToSelf = false;

            var sb = new StringBuilder();

            // Deleting the controller above unhooks it from every Animator that
            // was using it, so put it back before reporting anything.
            RepointAnimators(controller, sb);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            sb.AppendLine($"controller: {ControllerPath}");
            sb.AppendLine($"  parameter {SpeedParam} (float, default 0)");
            sb.AppendLine($"  parameter {JumpParam} (trigger)");
            sb.AppendLine($"  parameter {SwingParam} (trigger)");
            sb.AppendLine($"  parameter {GroundedParam} (bool, default true)");
            sb.AppendLine($"  default state: {machine.defaultState.name}");
            foreach (var child in machine.states)
            {
                var s = child.state;
                var clip = s.motion as AnimationClip;
                // speed is printed even when it is 1, because "the swing is supposed to
                // play faster" is exactly the kind of thing that silently
                // stops being true while the report still looks fine.
                sb.AppendLine($"  state '{s.name}' motion='{s.motion?.name}' " +
                              $"len={clip?.length:0.00}s loop={clip?.isLooping} " +
                              $"speed={s.speed:0.00}");

                foreach (var t in s.transitions)
                {
                    var conds = string.Join(" AND ", t.conditions.Select(
                        c => $"{c.parameter} {c.mode} {c.threshold}"));
                    sb.AppendLine($"      -> '{t.destinationState?.name}' " +
                                  $"cond=[{conds}] " +
                                  $"exitTime={t.exitTime:0.00} hasExitTime={t.hasExitTime} " +
                                  $"duration={t.duration:0.00}");
                }
            }

            // Written to a file as well as the console. Saving the scene can
            // start a domain reload, and everything after that point in this run
            // is lost — including the log line that would have said the
            // Animators were re-pointed.
            try
            {
                var full = System.IO.Path.Combine(
                    System.IO.Directory.GetCurrentDirectory(), Report);
                System.IO.Directory.CreateDirectory(
                    System.IO.Path.GetDirectoryName(full));
                System.IO.File.WriteAllText(full, sb.ToString());
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Echoes] could not write " + Report + ": " + e.Message);
            }

            Debug.Log("[Echoes] Ari controller built\n" + sb);
        }

        /// <summary>
        /// Put the rebuilt controller back on every Ari in the open scenes.
        ///
        /// Rebuilding means DeleteAsset followed by CreateAnimatorControllerAtPath,
        /// and that is not transparent to anything already pointing at the old
        /// asset. A scene serialises the controller by GUID, deleting the asset
        /// leaves that reference dangling, and the replacement gets a fresh GUID
        /// — so every Animator that was animating Ari comes back with
        /// runtimeAnimatorController null. The character stands in a T-pose and
        /// nothing says why, because nothing was misconfigured: the asset is
        /// fine, the pointer is not. This was measured, not reasoned about — the
        /// first jump probe came back with controller=&lt;NONE&gt; moments after
        /// a successful build.
        ///
        /// Keyed off AriMover rather than off "was something pointing at the old
        /// controller", because after the delete there is nothing left to detect.
        /// The component that drives the Animator is the one place that knows
        /// which Animator is meant to have it. Closed scenes are left alone:
        /// they are not part of the running game, and touching their objects
        /// dirties assets nobody asked to change.
        /// </summary>
        static void RepointAnimators(AnimatorController controller, StringBuilder sb)
        {
            int repointed = 0;
            int alreadyCorrect = 0;
            int other = 0;

            for (int s = 0; s < UnityEngine.SceneManagement.SceneManager.sceneCount; s++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var anim in root.GetComponentsInChildren<Animator>(true))
                    {
                        if (anim.GetComponent<Echoes.Painterly.AriMover>() == null)
                        {
                            other++;
                            continue;
                        }

                        if (anim.runtimeAnimatorController == controller) { alreadyCorrect++; continue; }

                        var was = anim.runtimeAnimatorController;
                        anim.runtimeAnimatorController = controller;
                        repointed++;
                        sb.AppendLine("  re-pointed Animator on '" + anim.name + "'" +
                                      (was == null
                                          ? "  (it had no controller at all — a dangling reference)"
                                          : "  (from '" + was.name + "')"));
                    }
                }
            }

            sb.AppendLine("  animators: " + repointed + " re-pointed, " +
                          alreadyCorrect + " already correct, " + other +
                          " on objects that are not Ari");

            if (repointed > 0)
                EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        /// <summary>Locomotion to Jump, on the trigger.</summary>
        static void Launch(AnimatorState from, AnimatorState jump)
        {
            var t = from.AddTransition(jump);
            t.hasExitTime = false;
            t.exitTime = 0f;
            t.hasFixedDuration = true;
            t.duration = LaunchBlendSeconds;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, JumpParam);
        }

        /// <summary>
        /// Jump to locomotion, on having touched down, and into whichever of the
        /// two states suits the speed she is carrying.
        /// </summary>
        static void Land(AnimatorState from, AnimatorState to, AnimatorConditionMode speed)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.exitTime = 0f;
            t.hasFixedDuration = true;
            t.duration = LandBlendSeconds;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, GroundedParam);
            t.AddCondition(speed, Threshold, SpeedParam);
        }

        /// <summary>Locomotion to the brush swing, on the trigger.</summary>
        static void Swing(AnimatorState from, AnimatorState swing)
        {
            var t = from.AddTransition(swing);
            t.hasExitTime = false;
            t.exitTime = 0f;
            t.hasFixedDuration = true;
            t.duration = SwingInSeconds;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.If, 0f, SwingParam);
        }

        /// <summary>
        /// Back to locomotion, into whichever state suits her speed — but only
        /// once the swing is genuinely over.
        ///
        /// Two conditions, and both are needed. Swinging-if-false is the real
        /// gate: AriMover holds it true for the length of the stroke and drops it
        /// at the follow-through, which is a fresh event the graph can wait on.
        /// The Speed test then picks which of Idle and Walk to return to. The
        /// Speed test alone, as measured, cut the stroke to one frame.
        /// </summary>
        static void SwingBack(AnimatorState from, AnimatorState to, AnimatorConditionMode speed)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.exitTime = 0f;
            t.hasFixedDuration = true;
            t.duration = SwingBlendSeconds;
            t.canTransitionToSelf = false;
            t.AddCondition(AnimatorConditionMode.IfNot, 0f, SwingingParam);
            t.AddCondition(speed, Threshold, SpeedParam);
        }

        /// <summary>
        /// One crossfade between two locomotion states, guarded by Speed.
        ///
        /// Walking is "faster than the threshold" and standing is "not faster
        /// than the threshold", so the return leg uses a plain Less rather than
        /// a second cut-off. That way a stick nudged a hair below zero still
        /// settles into the idle instead of getting stuck mid-stride.
        /// </summary>
        static void CrossFade(AnimatorState from, AnimatorState to,
                              AnimatorConditionMode mode)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.hasFixedDuration = true;   // duration in real seconds, not a fraction of the clip
            t.duration = BlendSeconds;
            t.exitTime = 0f;
            t.canTransitionToSelf = false;
            t.AddCondition(mode, Threshold, SpeedParam);
        }
    }
}
