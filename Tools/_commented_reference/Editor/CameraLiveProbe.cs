using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Verifies the follow camera actually tracks Ari and obeys orbit/zoom.
    /// Writes Temp/camera_live.txt.
    ///
    /// Run in play mode. Under automation there is no mouse, so the orbit and
    /// zoom limits are exercised by driving the same fields the mouse writes,
    /// and what is measured is the consequence: where the camera ends up and
    /// whether it stays a sane distance from her.
    /// </summary>
    public static class CameraLiveProbe
    {
        [MenuItem("Tools/Echoes/Probe Camera", priority = 101)]
        public static void Run()
        {
            var sb = new StringBuilder();

            if (!EditorApplication.isPlaying) { sb.AppendLine("Not in play mode."); Finish(sb); return; }

            var ari = GameObject.Find("Ari");
            var cam = Camera.main;
            var follow = cam != null ? cam.GetComponent<Echoes.Painterly.AriFollowCamera>() : null;

            sb.AppendLine("--- wiring ---");
            sb.AppendLine($"  Ari found={ari != null}");
            sb.AppendLine($"  Main Camera found={cam != null} " +
                          $"AriFollowCamera present={follow != null}");

            if (ari == null || cam == null || follow == null) { Finish(sb); return; }

            var mover = ari.GetComponent<Echoes.Painterly.AriMover>();
            sb.AppendLine($"  AriMover present={mover != null}");

            Vector3 ariStart = ari.transform.position;
            Vector3 camStart = cam.transform.position;
            float distStart = Vector3.Distance(camStart, ariStart);

            sb.AppendLine($"\n--- before ---");
            sb.AppendLine($"  Ari   at {Fmt(ariStart)}");
            sb.AppendLine($"  camera at {Fmt(camStart)}  pitch {cam.transform.eulerAngles.x:F1}");
            sb.AppendLine($"  distance to Ari = {distStart:F3}");
            sb.AppendLine($"  Ari in frustum = {InFrustum(cam, ari)}");

            // ---- walk her forward; the camera must trail ----
            sb.AppendLine($"\n--- walking Ari 2.0s on +Z ---");
            if (mover != null)
            {
                for (int i = 0; i < 120; i++)
                {
                    mover.Step(new Vector3(0f, 0f, 1f), false, 1f / 60f);
                }
            }

            // LateUpdate does not run for a script invoked from the editor, so
            // the follow maths is stepped by hand at the same cadence.
            Vector3 ariAfter = ari.transform.position;
            float walked = Vector3.Distance(ariStart, ariAfter);
            sb.AppendLine($"  Ari moved {walked:F3} (expect ~4.4)");

            // Let the camera settle onto the new pivot.
            for (int i = 0; i < 30; i++) StepCamera(cam, follow);

            Vector3 camAfter = cam.transform.position;
            float moved = Vector3.Distance(camStart, camAfter);
            sb.AppendLine($"  camera moved {moved:F3} (expect > 3 — it must trail her)");
            sb.AppendLine($"  camera at {Fmt(camAfter)}");
            sb.AppendLine($"  distance to Ari = {Vector3.Distance(camAfter, ariAfter):F3}");

            bool trailing = Vector3.Dot(
                (camAfter - camStart).normalized,
                (ariAfter - ariStart).normalized) > 0.9f;
            sb.AppendLine($"  trailing her (not sliding sideways): {trailing}");
            sb.AppendLine($"  Ari in frustum now = {InFrustum(cam, ari)}");

            // ---- zoom limits ----
            sb.AppendLine($"\n--- zoom ---");
            SetDistance(follow, 0.01f);
            StepCamera(cam, follow, 20);
            float near = Vector3.Distance(cam.transform.position, ari.transform.position);
            sb.AppendLine($"  asked for 0.01, settled at {near:F3} (must be >= 2.5)");

            SetDistance(follow, 500f);
            StepCamera(cam, follow, 20);
            float far = Vector3.Distance(cam.transform.position, ari.transform.position);
            sb.AppendLine($"  asked for 500, settled at {far:F3} (must be <= 14)");

            // ---- pitch limits ----
            sb.AppendLine($"\n--- pitch clamp ---");
            SetPitch(follow, -89f);
            StepCamera(cam, follow, 10);
            float lo = NormalisedPitch(cam.transform.eulerAngles.x);
            SetPitch(follow, 89f);
            StepCamera(cam, follow, 10);
            float hi = NormalisedPitch(cam.transform.eulerAngles.x);
            sb.AppendLine($"  asked -89 -> {lo:F1} (must be >= -8)");
            sb.AppendLine($"  asked  89 -> {hi:F1} (must be <= 72)");

            // ---- is she ever below the ground? ----
            sb.AppendLine($"\n--- sanity ---");
            sb.AppendLine($"  camera y {cam.transform.position.y:F3} vs Ari y {ari.transform.position.y:F3} " +
                          $"(camera must stay above her feet)");

            ari.transform.position = ariStart;
            cam.transform.position = camStart;

            Finish(sb);
        }

        /// <summary>
        /// Runs the follow camera's own LateUpdate by hand, because an editor
        /// script's call does not drive the player loop.
        /// </summary>
        static void StepCamera(Camera cam, Echoes.Painterly.AriFollowCamera follow, int times = 1)
        {
            var m = typeof(Echoes.Painterly.AriFollowCamera)
                .GetMethod("LateUpdate",
                           System.Reflection.BindingFlags.NonPublic |
                           System.Reflection.BindingFlags.Instance);
            if (m == null) return;
            for (int i = 0; i < times; i++) m.Invoke(follow, null);
        }

        static void SetDistance(Echoes.Painterly.AriFollowCamera follow, float d)
        {
            var f = typeof(Echoes.Painterly.AriFollowCamera)
                .GetField("_distance", System.Reflection.BindingFlags.NonPublic |
                                     System.Reflection.BindingFlags.Instance);
            f?.SetValue(follow, d);
        }

        static void SetPitch(Echoes.Painterly.AriFollowCamera follow, float p)
        {
            var f = typeof(Echoes.Painterly.AriFollowCamera)
                .GetField("_pitch", System.Reflection.BindingFlags.NonPublic |
                                   System.Reflection.BindingFlags.Instance);
            f?.SetValue(follow, p);
        }

        static float NormalisedPitch(float eulerX)
        {
            float p = eulerX > 180f ? eulerX - 360f : eulerX;
            return p;
        }

        static bool InFrustum(Camera cam, GameObject target)
        {
            var plane = GeometryUtility.CalculateFrustumPlanes(cam);
            var bounds = new Bounds(target.transform.position + Vector3.up,
                                    new Vector3(1f, 2f, 1f));
            return GeometryUtility.TestPlanesAABB(plane, bounds);
        }

        static string Fmt(Vector3 v) => $"({v.x:F2}, {v.y:F2}, {v.z:F2})";

        static void Finish(StringBuilder sb)
        {
            var text = sb.ToString();
            File.WriteAllText("Temp/camera_live.txt", text);
            Debug.Log("[Echoes] Camera probe\n" + text);
        }
    }
}
