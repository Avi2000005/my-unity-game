using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Measures the jump: does the graph reach the Jump state, does she leave
    /// the ground, how high, does she come back down onto the paving, and does
    /// the clip actually pose her. Writes Temp/jump_live.txt.
    ///
    /// Runs in edit mode rather than play mode, because the bridge has no way to
    /// enter play mode and a probe that can only run inside a running game is a
    /// probe that mostly does not run. Stepping AriMover.Step and Animator.Update
    /// by hand is the same code path the keyboard feeds, so the measurement is
    /// of the real thing rather than of a stand-in.
    ///
    /// Keyboard.current is null under automation, which is why the jump is
    /// requested through Step's flag and not through the key: a jump gated on a
    /// key press is a jump that provably never happens in a headless run, and
    /// would report as broken while working perfectly at the keyboard.
    /// </summary>
    public static class JumpLiveProbe
    {
        const string Report = "Temp/jump_live.txt";
        const string ControllerPath = "Assets/Art/Ari/Ari.controller";

        [MenuItem("Tools/Echoes/Probe Jump", priority = 98)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] does the jump work?");
            sb.AppendLine("mode: " + (EditorApplication.isPlaying ? "play" : "edit") +
                          "  (play is better; edit still drives the real code path)");

            AuditController(sb);

            var ari = GameObject.Find("Ari");
            if (ari == null)
            {
                sb.AppendLine("\nNo Ari in the scene. Nothing to jump.");
                Finish(sb);
                return;
            }

            var anim = ari.GetComponent<Animator>();
            var mover = ari.GetComponent<Echoes.Painterly.AriMover>();

            sb.AppendLine("\n--- on Ari ---");
            sb.AppendLine("  Animator: " + (anim == null ? "MISSING" :
                "enabled=" + anim.enabled +
                " controller=" + (anim.runtimeAnimatorController == null
                    ? "<NONE>" : anim.runtimeAnimatorController.name) +
                " avatar=" + (anim.avatar == null ? "<NONE>" : anim.avatar.name)));
            sb.AppendLine("  AriMover: " + (mover == null ? "MISSING" : "present"));

            if (anim == null || mover == null) { Finish(sb); return; }

            // In edit mode Awake has not run, so the Animator is still in the
            // state it was authored in — which is switched off, deliberately, so
            // that a scene with a live controller and no mover is not animating
            // a character nobody is steering. Awake turns it on; here we have to
            // do it by hand or every frame below evaluates nothing.
            bool wasAnimEnabled = anim.enabled;
            if (!anim.enabled) anim.enabled = true;

            // In play mode Update would overwrite the transform every real frame
            // and the scripted steps would be fighting it. In edit mode there is
            // no Update at all.
            bool wasMoverEnabled = mover.enabled;
            if (EditorApplication.isPlaying) mover.enabled = false;

            Vector3 startPos = ari.transform.position;
            var foot = ari.GetComponentsInChildren<Transform>(true)
                           .FirstOrDefault(t => t.name == "LeftFoot");
            Vector3 footAtRest = foot != null ? foot.position : Vector3.zero;

            const float dt = 1f / 60f;

            try
            {
                // Each test reports its own failure instead of aborting the run.
                // A probe that throws on the first problem reports nothing about
                // the three that would have followed, and the one thing it did
                // find is lost the moment the bridge drops the connection.
                Guard(sb, "standing jump", () => StandJump(ari, anim, mover, foot, footAtRest, dt, sb));
                Guard(sb, "walking jump", () => WalkingJump(ari, anim, mover, dt, sb));
                Guard(sb, "held jump", () => NoDoubleJump(ari, anim, mover, dt, sb));
            }
            finally
            {
                ari.transform.position = startPos;
                mover.enabled = wasMoverEnabled;
                anim.enabled = wasAnimEnabled;
            }

            Finish(sb);
        }

        /// <summary>
        /// Run one measurement, and write down what went wrong if it does not
        /// survive to the end.
        /// </summary>
        static void Guard(StringBuilder sb, string what, System.Action test)
        {
            try
            {
                test();
            }
            catch (System.Exception e)
            {
                sb.AppendLine("  " + what + " THREW: " + e.GetType().Name +
                              " — " + (e.Message ?? "").Split('\n')[0]);
            }
        }

        /// <summary>
        /// Read the graph itself. A jump that plays and a jump that is merely
        /// animated are different things, and only the graph says which is
        /// wired up.
        /// </summary>
        static void AuditController(StringBuilder sb)
        {
            sb.AppendLine("\n--- controller ---");
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null)
            {
                sb.AppendLine("  " + ControllerPath + " NOT FOUND. Run Tools/Echoes/Build Ari Controller.");
                return;
            }

            var wanted = new[] { "Speed", "Jump", "Grounded" };
            foreach (var name in wanted)
            {
                var p = ctrl.parameters.FirstOrDefault(x => x.name == name);
                sb.AppendLine("  parameter " + name + ": " +
                    (p == null ? "MISSING" : p.type + " default=" + p.defaultBool + "/" + p.defaultFloat));
            }

            var machine = ctrl.layers[0].stateMachine;
            sb.AppendLine("  default state: " + (machine.defaultState == null
                ? "<none>" : machine.defaultState.name));

            foreach (var child in machine.states)
            {
                var s = child.state;
                var clip = s.motion as AnimationClip;
                sb.AppendLine("  state '" + s.name + "' motion=" +
                    (s.motion == null ? "<none>" : "'" + s.motion.name + "'") +
                    (clip == null ? "" :
                     " len=" + clip.length.ToString("F2") + "s loop=" + clip.isLooping) +
                    (s.writeDefaultValues ? " [writeDefaults ON]" : ""));

                foreach (var t in s.transitions)
                {
                    var conds = string.Join(" AND ", t.conditions.Select(
                        c => c.parameter + " " + c.mode));
                    sb.AppendLine("      -> '" + (t.destinationState == null
                            ? "<exit>" : t.destinationState.name) +
                        "'  [" + conds + "]  exitTime=" +
                        t.exitTime.ToString("F2") + " duration=" + t.duration.ToString("F2"));
                }
            }
        }

        /// <summary>
        /// The plain case: standing still, one jump, watch the whole arc.
        /// </summary>
        static void StandJump(GameObject ari, Animator anim,
                              Echoes.Painterly.AriMover mover, Transform foot,
                              Vector3 footAtRest, float dt, StringBuilder sb)
        {
            sb.AppendLine("\n--- one jump from standing ---");

            // Settle first. She eases onto the surface over time, and measuring
            // a jump from the first frame measures the easing too.
            for (int i = 0; i < 30; i++) { mover.Step(Vector3.zero, false, dt); anim.Update(dt); }

            float y0 = ari.transform.position.y;
            sb.AppendLine("  standing at y=" + y0.ToString("F4") +
                          "  grounded=" + mover.IsGrounded + "  state=" + ClipNow(anim));

            // The jump request goes in on one frame only.
            mover.Step(Vector3.zero, false, dt, true);
            anim.Update(dt);

            float peak = y0;
            float peakAt = 0f;
            int airborneFrames = 0;
            bool sawJumpState = false;
            string stateInFlight = "-";
            int landings = 0;
            bool wasGrounded = mover.IsGrounded;

            // Two seconds is more than the longest airtime the tuning allows,
            // so a hop that never comes back is visible as a hop that never
            // comes back rather than as a truncated trace.
            int frames = Mathf.RoundToInt(2f / dt);
            for (int i = 0; i < frames; i++)
            {
                mover.Step(Vector3.zero, false, dt);
                anim.Update(dt);

                float y = ari.transform.position.y;
                if (y > peak) { peak = y; peakAt = i * dt; }
                if (mover.IsAirborne) airborneFrames++;
                if (mover.IsAirborne && stateInFlight == "-") stateInFlight = ClipNow(anim);

                if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash ==
                    Animator.StringToHash("Ari_Jump")) sawJumpState = true;

                if (mover.IsGrounded && !wasGrounded) landings++;
                wasGrounded = mover.IsGrounded;

                if (landings > 0 && i > 10) break;   // measured what we came for
            }

            float y1 = ari.transform.position.y;

            sb.AppendLine("  left the ground: " + (airborneFrames > 0
                ? "yes, for " + (airborneFrames * dt).ToString("F2") + "s"
                : "NO — SHE NEVER LEFT THE PAVING"));
            sb.AppendLine("  peak y=" + peak.ToString("F4") + " at " + peakAt.ToString("F2") +
                          "s  =>  " + (peak - y0).ToString("F3") + " m up" +
                          "  (jumpHeight is set to 1.2)");
            sb.AppendLine("  landed back at y=" + y1.ToString("F4") +
                          ", " + (y1 - y0).ToString("F4") + " m from where she took off" +
                          (landings == 1 ? "  (one landing, no bounce)" : "  LANDINGS=" + landings));
            sb.AppendLine("  mover reported LastJumpHeight = " +
                          mover.LastJumpHeight.ToString("F3") + " m");
            sb.AppendLine("  grounded now=" + mover.IsGrounded);
            sb.AppendLine("  graph entered Ari_Jump: " + sawJumpState);
            sb.AppendLine("  state on the frame after the jump key went down: " + stateInFlight);

            // Read the state only once the blend has finished. Read on the landing
            // frame it reports Ari_Jump with transitioning=true, which is the
            // source of a transition that is still in progress and says nothing
            // about where she ended up — the exact false negative this project
            // hit before with the 0.15s idle/walk crossfade.
            for (int i = 0; i < 20; i++) { mover.Step(Vector3.zero, false, dt); anim.Update(dt); }
            string settled = ClipNow(anim);
            sb.AppendLine("  state once settled: " + settled +
                          (settled.Contains("Ari_Idle") && !settled.Contains("transitioning=True")
                              ? "  — she is back to standing"
                              : "  — CHECK: did not come back to rest"));

            // The rig. A clip can drive the graph and leave the mesh in its bind
            // pose — that is what an unmapped retarget looks like from the
            // Animator window, and it is invisible in every number above.
            if (foot != null)
            {
                float moved = Vector3.Distance(foot.position, footAtRest);
                sb.AppendLine("  LeftFoot ended " + moved.ToString("F3") +
                              " m from where it started — " +
                              (moved > 0.02f ? "the clip is posing her"
                                             : "THE BONES DID NOT MOVE"));
            }
            else sb.AppendLine("  no LeftFoot bone found on the rig");
        }

        /// <summary>
        /// Jumping out of a walk. A separate transition, and a trigger set while
        /// the graph is on the Walk state is the case most likely to be missed.
        /// </summary>
        static void WalkingJump(GameObject ari, Animator anim,
                                Echoes.Painterly.AriMover mover, float dt, StringBuilder sb)
        {
            sb.AppendLine("\n--- jump while walking ---");

            Vector3 from = ari.transform.position;
            for (int i = 0; i < 40; i++) { mover.Step(Vector3.forward, false, dt); anim.Update(dt); }

            string walkingState = ClipNow(anim);
            sb.AppendLine("  after 40 frames of forward input: " + walkingState);
            sb.AppendLine("  CurrentSpeed=" + mover.CurrentSpeed.ToString("F2"));

            mover.Step(Vector3.forward, false, dt, true);
            anim.Update(dt);

            bool jumped = false;
            for (int i = 0; i < 8; i++)
            {
                mover.Step(Vector3.forward, false, dt);
                anim.Update(dt);
                if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash ==
                    Animator.StringToHash("Ari_Jump")) jumped = true;
            }

            sb.AppendLine("  jumped out of the walk: " + jumped +
                          (jumped ? "" : "  — THE WALK-TO-JUMP EDGE IS MISSING"));

            // Put her back where the walk started so the next test is not
            // measuring from a position she was carried to.
            ari.transform.position = from;
            for (int i = 0; i < 30; i++) { mover.Step(Vector3.zero, false, dt); anim.Update(dt); }
        }

        /// <summary>
        /// Can a second press in mid-air give her a second hop?
        ///
        /// Answered by comparison rather than by counting. A single press is
        /// measured first, then the request is held true for the whole of a
        /// second flight. If the two peaks match, the extra presses added
        /// nothing and there is no double jump; if the held one flies higher,
        /// the request is being honoured in mid-air.
        ///
        /// Comparing against a value remembered from an earlier jump would have
        /// been the obvious shortcut and would have reported a fault, because
        /// the first version of this compared the peak against LastJumpHeight
        /// read before that jump had landed — a value belonging to a different
        /// hop entirely. Both peaks here come from this test and nothing else.
        ///
        /// The window is half a second, which is shorter than the shortest
        /// airtime the tuning can produce, so nothing in it is a second jump off
        /// a fresh press.
        /// </summary>
        static void NoDoubleJump(GameObject ari, Animator anim,
                                 Echoes.Painterly.AriMover mover, float dt, StringBuilder sb)
        {
            sb.AppendLine("\n--- one press, then a press held down mid-air ---");

            int window = Mathf.RoundToInt(0.5f / dt);

            // --- baseline: a single press, never repeated ---
            Settle(mover, anim, dt);
            float baseY = ari.transform.position.y;
            mover.Step(Vector3.zero, false, dt, true);
            anim.Update(dt);
            float single = baseY;
            for (int i = 0; i < window; i++)
            {
                mover.Step(Vector3.zero, false, dt);
                anim.Update(dt);
                single = Mathf.Max(single, ari.transform.position.y);
            }
            float singlePeak = single - baseY;
            bool touchedDown = mover.IsGrounded;

            // --- the same hop with the key held the entire way ---
            Settle(mover, anim, dt);
            float heldY = ari.transform.position.y;
            float held = heldY;
            bool everGroundedMidFlight = false;
            for (int i = 0; i < window; i++)
            {
                // Every frame asks again, as a held key would.
                mover.Step(Vector3.zero, false, dt, true);
                anim.Update(dt);
                held = Mathf.Max(held, ari.transform.position.y);
                if (mover.IsGrounded) everGroundedMidFlight = true;
            }
            float heldPeak = held - heldY;

            sb.AppendLine("  single press        : peak " + singlePeak.ToString("F3") + " m" +
                          (touchedDown ? "" : "  (still in the air at the end of the window)"));
            sb.AppendLine("  key held all through: peak " + heldPeak.ToString("F3") + " m");
            sb.AppendLine("  difference          : " +
                          (heldPeak - singlePeak).ToString("F3") + " m — " +
                          (heldPeak - singlePeak < 0.05f
                              ? "the mid-air presses were ignored, there is no double jump"
                              : "SHE GETS A SECOND HOP, which the design does not give her"));
            sb.AppendLine("  touched down inside half a second: " + everGroundedMidFlight +
                          (everGroundedMidFlight ? "  WRONG" : "  (no, as expected)"));

            // Now let her finish, and check she ends up on the paving rather than
            // through it. Stopping the loop while she is still rising is what
            // made the first version report 0.46 m of unexplained drift.
            for (int i = 0; i < Mathf.RoundToInt(2f / dt); i++)
            {
                mover.Step(Vector3.zero, false, dt);
                anim.Update(dt);
                if (mover.IsGrounded && i > 5) break;
            }

            float y1 = ari.transform.position.y;
            sb.AppendLine("  after the flight: y=" + y1.ToString("F4") + " from " +
                          heldY.ToString("F4") + ", " +
                          (y1 - heldY).ToString("F4") + " m from take-off" +
                          (Mathf.Abs(y1 - heldY) < 0.3f
                              ? "  — back on the paving, not through it"
                              : "  — SHE HAS SANK THROUGH IT OR DRIFTED"));
            sb.AppendLine("  grounded=" + mover.IsGrounded +
                          ", mover measured LastJumpHeight=" +
                          mover.LastJumpHeight.ToString("F3") + " m");
        }

        /// <summary>Stand her still, long enough for the ground easing to settle.</summary>
        static void Settle(Echoes.Painterly.AriMover mover, Animator anim, float dt)
        {
            for (int i = 0; i < 30; i++) { mover.Step(Vector3.zero, false, dt); anim.Update(dt); }
        }

        /// <summary>
        /// Which clip is playing, by name.
        ///
        /// Null-safe on the controller, because an Animator with no controller is
        /// exactly the failure this probe exists to catch — and reading the clip
        /// name by dereferencing a null controller turns that finding into a
        /// NullReferenceException, which reports as "the probe crashed" and says
        /// nothing about the thing that was actually wrong.
        /// </summary>
        static string ClipNow(Animator anim)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            var controller = anim.runtimeAnimatorController;
            if (controller == null) return "clip=<NO CONTROLLER ASSIGNED>";

            var clip = controller.animationClips
                .FirstOrDefault(c => Animator.StringToHash(c.name) == info.shortNameHash);

            return "clip=" + (clip == null ? "?" : clip.name) +
                   " t=" + info.normalizedTime.ToString("F2") +
                   " transitioning=" + anim.IsInTransition(0);
        }

        static void Finish(StringBuilder sb)
        {
            var full = Path.Combine(Directory.GetCurrentDirectory(), Report);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
