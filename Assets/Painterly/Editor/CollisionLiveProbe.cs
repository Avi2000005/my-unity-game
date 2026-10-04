using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Measures whether Ari is actually stopped by things, and whether adding
    /// that broke the jump. Writes Temp/collision_live.txt.
    ///
    /// Every test here is against a box the probe puts down itself rather than
    /// against the village. That is deliberate. A wall in the village is at some
    /// unknown angle at some unknown distance, and a character who slides
    /// beautifully along a test wall and walks through a real one is a real
    /// possibility — but so is the reverse, where a probe fails because it
    /// started her inside a kerb. A box at a known place gives a number that
    /// means something: she must stop one body radius short of it, and anything
    /// else is a fault rather than a surprise.
    ///
    /// The village colliders are counted separately at the end, because the
    /// boxes answer "does the sweep work" and only the count answers "will
    /// there be anything for it to hit".
    /// </summary>
    public static class CollisionLiveProbe
    {
        const string Report = "Temp/collision_live.txt";

        const float Dt = 1f / 60f;

        [MenuItem("Tools/Echoes/Probe Collision", priority = 99)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] does the wall sweep work, and did it break the jump?");
            sb.AppendLine("mode: " + (EditorApplication.isPlaying ? "play" : "edit"));

            var ari = GameObject.Find("Ari");
            if (ari == null) { sb.AppendLine("\nNo Ari in the scene."); Finish(sb); return; }

            var anim = ari.GetComponent<Animator>();
            var mover = ari.GetComponent<Echoes.Painterly.AriMover>();
            if (anim == null || mover == null)
            {
                sb.AppendLine("\nAri is missing her Animator or her AriMover. Nothing to measure.");
                Finish(sb);
                return;
            }

            bool wasAnim = anim.enabled;
            if (!anim.enabled) anim.enabled = true;
            bool wasMover = mover.enabled;
            if (EditorApplication.isPlaying) mover.enabled = false;

            Vector3 start = ari.transform.position;
            var probes = new System.Collections.Generic.List<GameObject>();

            try
            {
                Physics.SyncTransforms();

                Guard(sb, "head-on wall", () => HeadOn(ari, anim, mover, probes, sb));
                Guard(sb, "45 degree wall", () => Angled(ari, anim, mover, probes, sb));
                Guard(sb, "low step", () => StepUp(ari, anim, mover, probes, sb));
                Guard(sb, "no collision off", () => SwitchedOff(ari, anim, mover, probes, sb));
                Guard(sb, "jump regression", () => JumpStillWorks(ari, anim, mover, probes, sb));
            }
            finally
            {
                foreach (var p in probes) if (p != null) Object.DestroyImmediate(p);
                ari.transform.position = start;
                mover.enabled = wasMover;
                anim.enabled = wasAnim;
            }

            CountVillageColliders(sb);
            Finish(sb);
        }

        static void Guard(StringBuilder sb, string what, System.Action test)
        {
            try { test(); }
            catch (System.Exception e)
            {
                sb.AppendLine("  " + what + " THREW: " + e.GetType().Name +
                              " — " + (e.Message ?? "").Split('\n')[0]);
            }
        }

        /// <summary>
        /// Walk her straight at a wall for two seconds and see how close she
        /// gets to it.
        ///
        /// The pass mark is not zero. She is a capsule, so stopping flush against
        /// the surface would mean the sweep let her interpenetrate by its own
        /// radius. She is expected to stop about one radius short and no more.
        ///
        /// Everything here is laid out in the camera's frame rather than the
        /// world's, because that is the frame she moves in. See Stick.
        /// </summary>
        static void HeadOn(GameObject ari, Animator anim, Echoes.Painterly.AriMover mover,
                           System.Collections.Generic.List<GameObject> probes, StringBuilder sb)
        {
            sb.AppendLine("\n--- head-on into a wall ---");
            Clear(probes);
            Settle(mover, anim);

            Vector3 fwd = CamForward();
            Vector3 here = Flat(ari.transform.position);
            var wall = Box("probe_wall", here + fwd * 2f + Vector3.up,
                           new Vector3(4f, 3f, 0.4f), fwd, probes);
            Physics.SyncTransforms();

            float face = NearFaceOf(wall, fwd, here, sb);
            float walked = Vector3.Dot(Walk(ari, anim, mover, fwd, 2f), fwd);
            float gap = face - walked;

            sb.AppendLine("  she started " + face.ToString("F3") +
                          " m from a 4x3x0.4 m wall and was asked to walk straight at it for 2 s" +
                          "  (unobstructed that would be 4.40 m)");
            sb.AppendLine("  she stopped " + gap.ToString("F3") + " m short of the face" +
                          "  (mover: TouchingWall=" + mover.TouchingWall +
                          ", swept " + mover.LastSweepRatio.ToString("F2") +
                          ", hit '" + mover.WallName + "')");
            sb.AppendLine("  VERDICT: " + (gap < -0.05f
                ? "SHE WENT THROUGH IT (" + (-gap).ToString("F3") + " m inside). " +
                  "The sweep is not working."
                : gap > 0.75f
                    ? "stopped, but " + gap.ToString("F2") + " m out — well clear of the " +
                      "wall. bodyRadius is probably too small."
                    : "stopped at the wall, about one body radius clear. Correct."));
        }

        /// <summary>
        /// Into the same wall at forty-five degrees.
        ///
        /// This is the test that a "did she stop" check cannot pass on its own.
        /// A sweep that just refuses the move entirely still stops her at the
        /// wall — it looks correct head-on and pins her uselessly at every
        /// corner in the village, which is where a player will actually notice.
        /// She has to end up meaningfully along the wall, not stopped on it.
        /// </summary>
        static void Angled(GameObject ari, Animator anim, Echoes.Painterly.AriMover mover,
                           System.Collections.Generic.List<GameObject> probes, StringBuilder sb)
        {
            sb.AppendLine("\n--- 45 degrees into a wall ---");
            Clear(probes);
            Settle(mover, anim);

            Vector3 fwd = CamForward();
            Vector3 right = CamRight();
            Vector3 here = Flat(ari.transform.position);

            // Start her off to one side so the wall is ahead and to her left,
            // and the walk has both a blocked and a free component to it.
            Vector3 start = here + right * 1.5f;
            ari.transform.position = start;
            Physics.SyncTransforms();

            // 12 m wide, not 4. She slides at 45 degrees for two seconds and
            // covers 3.1 m along the wall, so a 4 m box — half-width 2 m — let
            // her simply walk off its end and continue into open space. The
            // sweep was working the whole time; the test walked her around the
            // obstacle and then reported that she had got past it.
            var wall = Box("probe_wall2", here + fwd * 2f + Vector3.up,
                           new Vector3(12f, 3f, 0.4f), fwd, probes);
            Physics.SyncTransforms();

            float face = NearFaceOf(wall, fwd, start, sb);
            Vector3 moved = Walk(ari, anim, mover, (fwd + right).normalized, 2f);

            float along = Vector3.Dot(moved, right);
            float into = Vector3.Dot(moved, fwd);

            sb.AppendLine("  wall face " + face.ToString("F3") + " m ahead of where she started; " +
                          "asked to go forward-right for 2 s");
            sb.AppendLine("  she travelled " + into.ToString("F3") + " m into the wall and " +
                          along.ToString("F3") + " m along it" +
                          "  (into - face = " + (into - face).ToString("F3") + " m)");
            sb.AppendLine("  VERDICT: " + (along > 0.8f && into < face + 0.4f
                ? "she slid along it and stopped at it. Corners are survivable."
                : along <= 0.2f
                    ? "SHE IS PINNED — she stops dead instead of sliding, which makes " +
                      "every corner in the village unusable."
                    : "she got " + (into - face).ToString("F2") +
                      " m through the wall. The slide is not holding."));
        }

        /// <summary>
        /// A kerb-height box in front of her. She should go over it without
        /// jumping, because the village's paving, paths and kerb line sit at
        /// three different heights and a sweep that ignores steps is a sweep
        /// that stops her dead at every seam between them.
        /// </summary>
        static void StepUp(GameObject ari, Animator anim, Echoes.Painterly.AriMover mover,
                           System.Collections.Generic.List<GameObject> probes, StringBuilder sb)
        {
            sb.AppendLine("\n--- a 0.25 m step in front of her ---");
            Clear(probes);
            Settle(mover, anim);

            Vector3 fwd = CamForward();
            Vector3 here = Flat(ari.transform.position);

            // Placed so its near face is 2.2 m away and it is 6 m deep, so a
            // 1.5 s walk at 2.2 m/s (3.3 m) leaves her standing on top of it
            // when the peak height is read rather than already back on the
            // ground beyond it.
            var step = Box("probe_step", here + fwd * 5.2f + Vector3.up * 0.125f,
                           new Vector3(6f, 0.25f, 6f), fwd, probes);
            Physics.SyncTransforms();

            float stepTop = step.transform.position.y + 0.125f;
            float before = ari.transform.position.y;

            Vector3 from = ari.transform.position;
            var stick = Stick(fwd);
            float highest = before;
            for (int i = 0; i < Mathf.RoundToInt(1.5f / Dt); i++)
            {
                mover.Step(stick, false, Dt);
                anim.Update(Dt);
                highest = Mathf.Max(highest, ari.transform.position.y);
            }

            float reached = Vector3.Dot(ari.transform.position - from, fwd);
            float rose = highest - before;

            sb.AppendLine("  a 6x6x0.25 m step whose near face is " +
                          NearFaceOf(step, fwd, here, sb).ToString("F3") +
                          " m away, top at y=" + stepTop.ToString("F3") +
                          ", her root started at y=" + before.ToString("F3"));
            sb.AppendLine("  she covered " + reached.ToString("F3") +
                          " m and her root peaked at y=" + highest.ToString("F3") +
                          "  =>  " + rose.ToString("F3") + " m up");
            sb.AppendLine("  VERDICT: " + (rose > 0.18f
                ? "she walked up it. Paving seams and kerbs will not stop her."
                : "SHE DID NOT GO UP IT. Every height change in the village stops her dead."));
        }

        /// <summary>
        /// The same wall with the sweep switched off, which is the control.
        ///
        /// Without it, "she stopped at the wall" is only meaningful next to
        /// "she did not stop when nothing was in the way". If the switch is not
        /// doing anything, the first test passing proves nothing.
        /// </summary>
        static void SwitchedOff(GameObject ari, Animator anim, Echoes.Painterly.AriMover mover,
                                System.Collections.Generic.List<GameObject> probes, StringBuilder sb)
        {
            sb.AppendLine("\n--- control: the same wall, sweep switched off ---");
            Clear(probes);
            Settle(mover, anim);

            Vector3 fwd = CamForward();
            Vector3 here = Flat(ari.transform.position);
            var wall = Box("probe_wall3", here + fwd * 2f + Vector3.up,
                           new Vector3(4f, 3f, 0.4f), fwd, probes);
            Physics.SyncTransforms();

            var so = new SerializedObject(mover);
            var prop = so.FindProperty("collideWithWalls");
            bool had = prop.boolValue;
            prop.boolValue = false;

            // Applied here, not left to the finally block. Setting boolValue on
            // a SerializedObject only stages the change; until it is applied the
            // component still reads true, and this test reported that switching
            // collision off made no difference to how far she walked — which is
            // what an ignored flag looks like, and which is also what a
            // genuinely broken flag looks like. The two are indistinguishable
            // from the output, so the write has to be unambiguous.
            so.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                float face = NearFaceOf(wall, fwd, here, sb);
                float reached = Vector3.Dot(Walk(ari, anim, mover, fwd, 2f), fwd);
                sb.AppendLine("  collideWithWalls is now " + prop.boolValue +
                              "; wall face " + face.ToString("F3") +
                              " m away; she reached " + reached.ToString("F3") + " m");
                sb.AppendLine("  VERDICT: " + (reached > face + 0.05f
                    ? "she walked straight through it. The switch works, so the " +
                      "first test's stop was the sweep's doing and not a coincidence."
                    : "she is STILL being stopped at " + reached.ToString("F2") +
                      " m, so something other than this flag is stopping her and " +
                      "the first test proved nothing."));
            }
            finally
            {
                prop.boolValue = had;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// The jump, again, on the same code.
        ///
        /// The sweep was added into the middle of Step and it changed what
        /// "the position she asked for" means. If the hop now lands somewhere
        /// else, or stops rising, that is this change's fault and has to be
        /// caught here rather than by the player noticing it in the village.
        /// </summary>
        static void JumpStillWorks(GameObject ari, Animator anim,
                                   Echoes.Painterly.AriMover mover,
                                   System.Collections.Generic.List<GameObject> probes,
                                   StringBuilder sb)
        {
            sb.AppendLine("\n--- jump, after the sweep was added ---");
            Clear(probes);
            Settle(mover, anim);

            Vector3 from = ari.transform.position;
            float y0 = from.y;

            mover.Step(Vector3.zero, false, Dt, true);
            anim.Update(Dt);

            float peak = y0;
            int airborne = 0;
            bool landed = false;
            for (int i = 0; i < Mathf.RoundToInt(2f / Dt); i++)
            {
                mover.Step(Vector3.zero, false, Dt);
                anim.Update(Dt);
                peak = Mathf.Max(peak, ari.transform.position.y);
                if (mover.IsAirborne) airborne++;
                else if (i > 5) { landed = true; break; }
            }

            Vector3 end = ari.transform.position;
            float height = peak - y0;
            float drift = new Vector2(end.x - from.x, end.z - from.z).magnitude;

            sb.AppendLine("  peak " + height.ToString("F3") + " m" +
                          "  (was 1.145 before the sweep)");
            sb.AppendLine("  airborne for " + (airborne * Dt).ToString("F2") + " s, landed: " + landed);
            sb.AppendLine("  drifted " + drift.ToString("F4") +
                          " m sideways while standing still and jumping");
            sb.AppendLine("  VERDICT: " + (height > 1.0f && height < 1.3f && landed && drift < 0.05f
                ? "unchanged. The hop still works."
                : "THE JUMP CHANGED. It was 1.145 m, landed once, no drift."));
        }

        // --- helpers ------------------------------------------------------------

        /// <summary>
        /// The camera's flattened forward, which is the direction "forward"
        /// means to Ari. There is no world-north in this game.
        /// </summary>
        static Vector3 CamForward()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.forward;

            var f = cam.transform.forward;
            f.y = 0f;
            return f.sqrMagnitude < 1e-4f ? Vector3.forward : f.normalized;
        }

        static Vector3 CamRight()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.right;

            var r = cam.transform.right;
            r.y = 0f;
            return r.sqrMagnitude < 1e-4f ? Vector3.right : r.normalized;
        }

        /// <summary>
        /// Convert a world direction into the camera-relative stick position
        /// that Step expects.
        ///
        /// This is the mistake the first version of this probe made, and it
        /// made every wall test report a pass for the wrong reason: the walls
        /// were laid out along world +Z, Step was given Vector3.forward, and
        /// Step resolves that against the camera — which faces -Z in the
        /// village. Ari walked away from all three walls, "travelled 4 m", and
        /// the head-on test congratulated the sweep for stopping her 5.9 m
        /// clear of a wall she had never approached. Passing Vector3.forward is
        /// not a way to walk north; it is a way to walk whichever way the
        /// camera happens to be pointing.
        /// </summary>
        static Vector3 Stick(Vector3 worldDir)
        {
            worldDir.y = 0f;
            if (worldDir.sqrMagnitude < 1e-6f) return Vector3.zero;

            worldDir.Normalize();
            return new Vector3(Vector3.Dot(worldDir, CamRight()),
                               0f,
                               Vector3.Dot(worldDir, CamForward()));
        }

        /// <summary>Walk for a fixed time, returning how far she got.</summary>
        static Vector3 Walk(GameObject ari, Animator anim, Echoes.Painterly.AriMover mover,
                            Vector3 worldDir, float seconds)
        {
            Vector3 from = ari.transform.position;
            var stick = Stick(worldDir);

            int frames = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < frames; i++)
            {
                mover.Step(stick, false, Dt);
                anim.Update(Dt);
            }
            return ari.transform.position - from;
        }

        static void Settle(Echoes.Painterly.AriMover mover, Animator anim)
        {
            for (int i = 0; i < 30; i++) { mover.Step(Vector3.zero, false, Dt); anim.Update(Dt); }
            Physics.SyncTransforms();
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        /// <summary>
        /// A box, turned to face a direction.
        ///
        /// The facing matters and was the second fault in the first version of
        /// this probe. The boxes were built with an identity rotation, so a
        /// 0.4 m thick wall's thin axis lay along world Z whatever way the
        /// camera was pointing — Ari walked in from the side, through the long
        /// face, and the head-on test reported she had got 3 m inside a wall
        /// she was demonstrably being stopped by. Size is in the box's own
        /// frame: x across the face, y up, z into it.
        /// </summary>
        static GameObject Box(string name, Vector3 centre, Vector3 size, Vector3 faceDir,
                              System.Collections.Generic.List<GameObject> into)
        {
            var go = new GameObject(name);
            go.transform.position = centre;
            go.transform.localScale = size;

            faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 1e-6f)
            {
                faceDir.Normalize();
                go.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up);
            }

            go.AddComponent<BoxCollider>();
            go.layer = 0;
            into.Add(go);
            return go;
        }

        /// <summary>
        /// How far along a direction the near face of a probe box is, measured
        /// from the point the walk started.
        ///
        /// Uses the box's own local thickness rather than its world bounds, and
        /// checks the box is actually facing the direction first.
        ///
        /// The bounds were the real bug. The world AABB of a rotated box is
        /// larger than the box, by an amount that depends on the angle it
        /// happens to be turned by — up to 4.2 m of phantom thickness for a
        /// 0.4 m wall rotated forty-five degrees — so projecting that box along
        /// the direction it was rotated to face reported a face nowhere near
        /// the wall. Taking the half-thickness from the box's own local scale,
        /// which is only valid because Box turns it to face, is exact at every
        /// angle.
        ///
        /// The subtraction is the low side, because the box is placed ahead of
        /// her and the face she meets first is the near one. (An intermediate
        /// version of this had it the other way round, on the reasoning that
        /// the near face was the high side along her direction of travel. It is
        /// not: she travels away from the origin, and the wall is in front of
        /// her, so the near face is the lower one. That version put every
        /// reported face 0.4 m further away than it was.)
        /// </summary>
        static float NearFaceOf(GameObject box, Vector3 fwd, Vector3 origin, StringBuilder sb)
        {
            fwd.y = 0f;
            fwd.Normalize();

            // The self-check. If the box is not facing the way the walk is
            // going, every distance below is meaningless, and the failure looks
            // exactly like a broken sweep — which is how this got wrong twice.
            float aligned = Vector3.Dot(box.transform.forward, fwd);
            if (aligned < 0.99f)
                sb.AppendLine("    !! the probe box is facing " + aligned.ToString("F2") +
                              " of the way the walk is going — its distances below are nonsense");

            float half = box.transform.localScale.z * 0.5f;
            return Vector3.Dot(box.transform.position, fwd) - half - Vector3.Dot(origin, fwd);
        }

        /// <summary>
        /// Take down everything a previous test put down.
        ///
        /// Without this the second test measures the first test's leftovers:
        /// the angled walk ends with Ari pressed against a twelve-metre wall,
        /// that wall is still standing when the step test begins, and the step
        /// test reports "she covered 0.000 m" and concludes she cannot climb
        /// steps. She was not blocked by the step. She was blocked by a wall
        /// she had already been measured against, and the same wall made the
        /// control test report that the sweep was having no effect either.
        /// </summary>
        static void Clear(System.Collections.Generic.List<GameObject> probes)
        {
            foreach (var p in probes) if (p != null) Object.DestroyImmediate(p);
            probes.Clear();
            Physics.SyncTransforms();
        }

        /// <summary>
        /// What is actually in the village for the sweep to hit. A sweep that
        /// works against a test box and has nothing to hit in the level is
        /// still a level where she walks through houses.
        /// </summary>
        static void CountVillageColliders(StringBuilder sb)
        {
            sb.AppendLine("\n--- village colliders ---");

            // No FindObjectsSortMode: it is deprecated in this Unity, and the
            // ordering it used to buy is not used by anything below.
            var cols = Object.FindObjectsByType<Collider>();
            sb.AppendLine("  " + cols.Length + " collider(s) in the scene");

            if (cols.Length == 0)
            {
                sb.AppendLine("  NONE. The sweep has nothing to collide with — she will " +
                              "still walk through every wall until the kit has colliders.");
                return;
            }

            foreach (var g in cols.GroupBy(c => LayerName(c.gameObject.layer))
                                   .OrderByDescending(g => g.Count()))
            {
                sb.AppendLine($"  layer {g.Key}: {g.Count()}");
            }

            int triggers = cols.Count(c => c.isTrigger);
            int meshes = cols.OfType<MeshCollider>().Count();
            int boxes = cols.OfType<BoxCollider>().Count();
            int capsule = cols.OfType<CapsuleCollider>().Count();
            sb.AppendLine($"  {boxes} box, {capsule} capsule, {meshes} mesh; " +
                          $"{triggers} are triggers and are ignored by the sweep");

            // The top few by volume: the things that will actually be a wall to
            // her rather than a decoration. Bounds are read once here rather
            // than inside the sort, because c.bounds on a mesh collider is a
            // world-space box Unity recomputes per access.
            var big = cols.Where(c => !c.isTrigger)
                          .Select(c => new { Col = c, Box = c.bounds })
                          .Where(x => x.Box.size.sqrMagnitude > 4f)
                          .OrderByDescending(x => x.Box.size.sqrMagnitude)
                          .Take(6);

            foreach (var x in big)
            {
                sb.AppendLine($"  big: {x.Col.name}  {x.Box.size.x:0.0}x{x.Box.size.y:0.0}x{x.Box.size.z:0.0} m" +
                              $"  at {x.Box.center.x:0.0},{x.Box.center.y:0.0},{x.Box.center.z:0.0}");
            }
        }

        static string LayerName(int layer)
        {
            string n = LayerMask.LayerToName(layer);
            return string.IsNullOrEmpty(n) ? layer.ToString() : n;
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
