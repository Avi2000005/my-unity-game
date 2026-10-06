using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Drives Ari in play mode and measures whether she moves, whether the
    /// Animator changes state, and whether the camera can see it.
    /// Writes Temp/ari_live.txt.
    ///
    /// The earlier version of this probe set Speed and read the state back
    /// immediately, which proved nothing: the crossfade is 0.15s long, so the
    /// state was still Ari_Idle and only inTransition had flipped. A transition
    /// that has started is not a transition that has finished, and reading the
    /// clip name in the same frame is the false negative. Everything here steps
    /// real frames instead.
    ///
    /// Movement is driven through AriMover.Step rather than by faking key
    /// presses, because Keyboard.current is null under automation — a component
    /// that only responds to real input then looks exactly like a broken one.
    /// </summary>
    public static class AriLiveProbe
    {
        [MenuItem("Tools/Echoes/Probe Ari Live", priority = 99)]
        public static void Run()
        {
            var sb = new StringBuilder();

            if (!EditorApplication.isPlaying)
            {
                sb.AppendLine("Not in play mode. Enter play mode first — this observes " +
                              "the running game, not the saved scene.");
                Finish(sb);
                return;
            }

            var ari = GameObject.Find("Ari");
            if (ari == null) { sb.AppendLine("No Ari in the scene."); Finish(sb); return; }

            var anim = ari.GetComponent<Animator>();
            var mover = ari.GetComponent<Echoes.Painterly.AriMover>();

            sb.AppendLine($"Ari at {ari.transform.position}  rot {ari.transform.eulerAngles.y:F1}");
            sb.AppendLine($"  Animator enabled={anim?.enabled} " +
                          $"controller={(anim?.runtimeAnimatorController == null ? "<NONE>" : anim.runtimeAnimatorController.name)} " +
                          $"avatar={(anim?.avatar == null ? "<NONE>" : anim.avatar.name)}");
            sb.AppendLine($"  AriMover present={mover != null}");

            if (anim == null || mover == null) { Finish(sb); return; }

            // Update() will overwrite the transform every real frame, so the
            // scripted steps have to fight it. Turning the component off for the
            // duration keeps the measurement about Step and nothing else.
            bool wasEnabled = mover.enabled;
            mover.enabled = false;

            var foot = ari.GetComponentsInChildren<Transform>(true)
                           .FirstOrDefault(t => t.name == "LeftFoot");

            // ---- standing ----
            sb.AppendLine("\n--- standing still ---");
            mover.Step(Vector3.zero, false, 0f);
            anim.Update(0f);
            var idle = ClipNow(anim);
            Vector3 startPos = ari.transform.position;
            Vector3 startFoot = foot != null ? foot.position : Vector3.zero;
            sb.AppendLine($"  Speed={mover.CurrentSpeed:F2} -> {idle}");
            sb.AppendLine($"  sole offset measured at runtime = " +
                          $"{ReadSoleOffset(mover):F4} below the root");

            // ---- walk ----
            sb.AppendLine("\n--- walking forward, 2.0 seconds of frames ---");
            const float dt = 1f / 60f;
            string walkClip = idle;
            int frames = Mathf.RoundToInt(2f / dt);

            // Captured before the walk. Reading it afterwards and calling that the
            // "before" figure reports the turn as no turn at all.
            float yawBefore = ari.transform.eulerAngles.y;

            for (int i = 0; i < frames; i++)
            {
                mover.Step(Vector3.forward, false, dt);
                anim.Update(dt);
                if (i == frames / 2) walkClip = ClipNow(anim);
            }

            var walkedTo = ari.transform.position;
            var walkedFoot = foot != null ? foot.position : Vector3.zero;

            sb.AppendLine($"  travelled {Vector3.Distance(startPos, walkedTo):F3} in 2.00s " +
                          $"(walkSpeed 2.2 => about 4.4 expected)");
            sb.AppendLine($"  CurrentSpeed reported = {mover.CurrentSpeed:F2}");
            sb.AppendLine($"  facing {yawBefore:F1} -> {ari.transform.eulerAngles.y:F1} " +
                          $"(she turns towards travel at {ari.transform.eulerAngles.y - yawBefore:F1} deg)");
            sb.AppendLine($"  Speed>0.1 so the graph should be on the walk clip: " +
                          $"{mover.CurrentSpeed > 0.1f}");
            sb.AppendLine($"  {walkClip}");

            float footTravel = Vector3.Distance(startFoot, walkedFoot);
            sb.AppendLine($"  LeftFoot bone moved {footTravel:F3} — " +
                          (footTravel > 0.05f
                              ? "the walk clip is actually posing her"
                              : "THE BONES DID NOT MOVE, the retarget is not driving the rig"));

            // ---- stop ----
            sb.AppendLine("\n--- stopping ---");
            for (int i = 0; i < 60; i++) { mover.Step(Vector3.zero, false, dt); anim.Update(dt); }
            var stopped = ClipNow(anim);
            sb.AppendLine($"  Speed={mover.CurrentSpeed:F2} -> {stopped}");
            sb.AppendLine($"  back on the idle: {stopped.Contains("Ari_Idle")}");

            // ---- ground ----
            sb.AppendLine("\n--- ground under her ---");
            foreach (var probe in new[] { ari.transform.position, walkedTo })
            {
                bool hit = Physics.Raycast(probe + Vector3.up * 1.5f, Vector3.down,
                                           out var h, 4f, ~0, QueryTriggerInteraction.Ignore);
                if (!hit) { sb.AppendLine($"  {probe}: nothing below"); continue; }
                float gap = ari.transform.position.y - ReadSoleOffset(mover) - h.point.y;
                sb.AppendLine($"  {probe} -> '{h.collider.name}' y={h.point.y:F4} " +
                              $"sole gap {gap:F4}");
            }

            // ---- camera ----
            var cam = Camera.main;
            var rend = ari.GetComponentInChildren<SkinnedMeshRenderer>();
            if (cam != null && rend != null)
            {
                var b = rend.bounds;
                var planes = GeometryUtility.CalculateFrustumPlanes(cam);
                bool inFrustum = GeometryUtility.TestPlanesAABB(planes, b);

                sb.AppendLine("\n--- camera ---");
                sb.AppendLine($"  cam {cam.transform.position} -> Ari {b.center}, " +
                              $"{Vector3.Distance(cam.transform.position, b.center):F2} units");
                sb.AppendLine($"  in frustum: {inFrustum}");
                sb.AppendLine($"  renderer isVisible: {rend.isVisible}");
                sb.AppendLine($"  her height on screen: {b.size.y:F2} units, " +
                              $"from {cam.transform.position.y:F2} away");
            }

            // Put her back and hand control to Update again.
            ari.transform.position = startPos;
            mover.enabled = wasEnabled;

            Finish(sb);
        }

        /// <summary>
        /// How far the soles sit below the root, measured the same way the mover
        /// does, so the ground gap can be reported against a real number.
        /// </summary>
        static float ReadSoleOffset(Echoes.Painterly.AriMover mover)
        {
            var skin = mover.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skin == null || skin.sharedMesh == null) return 0f;

            var baked = new Mesh();
            skin.BakeMesh(baked);
            var verts = baked.vertices;
            float lowest = float.MaxValue;
            var m = skin.transform.localToWorldMatrix;
            for (int i = 0; i < verts.Length; i++)
            {
                float y = m.MultiplyPoint3x4(verts[i]).y;
                if (y < lowest) lowest = y;
            }
            Object.DestroyImmediate(baked);

            return mover.transform.position.y - lowest;
        }

        static string ClipNow(Animator anim)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            var clip = anim.runtimeAnimatorController.animationClips
                .Select(c => new { c.name, c.length })
                .FirstOrDefault(c => Animator.StringToHash(c.name) == info.shortNameHash);

            return $"clip={(clip == null ? "?" : clip.name)} " +
                   $"len={(clip == null ? 0f : clip.length):F2}s " +
                   $"t={info.normalizedTime:F2} " +
                   $"transitioning={anim.IsInTransition(0)}";
        }

        static void Finish(StringBuilder sb)
        {
            var text = sb.ToString();
            File.WriteAllText("Temp/ari_live.txt", text);
            Debug.Log("[Echoes] Ari live probe\n" + text);
        }
    }
}
