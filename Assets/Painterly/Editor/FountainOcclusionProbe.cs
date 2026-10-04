using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Finds out why the fountain never appears in a render.
    ///
    /// The restore probe rendered the same view five times — grey, magenta albedo,
    /// material restore, property-block restore on and off — and all five files came
    /// out byte-for-byte identical. That is not "the colour channel is broken". It
    /// means changing the fountain's own material does not alter the image at all,
    /// so the fountain is not in that view: either something is in front of it, it
    /// is outside the frame, or its renderers are not being drawn.
    ///
    /// Three independent checks, because any one of them alone can be fooled:
    ///
    ///   1. the camera is placed by solving for the distance that exactly fits the
    ///      fountain's bounding sphere, instead of by a hand-picked offset that
    ///      assumed the framing worked out;
    ///   2. a raycast from the camera to the fountain's centre reports whatever it
    ///      hits first, which is what actually occludes the shot;
    ///   3. every renderer in the scene is switched off except the fountain's, and
    ///      the resulting frame is counted for non-background pixels. If the
    ///      fountain is on screen it must dominate that frame.
    /// </summary>
    public static class FountainOcclusionProbe
    {
        const string Report = "Temp/fountain_occlusion.txt";
        const int W = 800, H = 500;

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] fountain occlusion probe");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sb.AppendLine("STOPPED: exit play mode first (Ctrl+P).");
                Finish(sb);
                return;
            }

            var root = GameObject.Find("Fountain");
            if (root == null)
            {
                sb.AppendLine("FATAL: no 'Fountain' in the scene.");
                Finish(sb);
                return;
            }

            var mine = root.GetComponentsInChildren<Renderer>(true);

            // --- 1. where is it, and what is it made of ------------------------
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (var r in mine) bounds.Encapsulate(r.bounds);

            sb.AppendLine();
            sb.AppendLine("fountain bounds centre : " + bounds.center);
            sb.AppendLine("fountain bounds size   : " + bounds.size);
            sb.AppendLine("fountain renderers     : " + mine.Length);
            sb.AppendLine();
            sb.AppendLine("per-renderer material / enabled / layer:");

            var matNames = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var r in mine)
            {
                var m = r.sharedMaterial;
                string mn = m == null ? "<none>" : m.name + " [" + m.shader.name + "]";
                if (!matNames.ContainsKey(mn)) matNames[mn] = 0;
                matNames[mn]++;
            }
            foreach (var kv in matNames) sb.AppendLine("   " + kv.Value + " x  " + kv.Key);

            foreach (var r in mine)
            {
                sb.AppendLine("   " + r.name.PadRight(22)
                    + " enabled=" + r.enabled
                    + " layer=" + LayerMask.LayerToName(r.gameObject.layer)
                    + " renderLayerMask=" + r.renderingLayerMask
                    + " visible=" + (r.isVisible ? "yes" : "no"));
            }

            // --- 2. the camera, framed by solving for distance -----------------
            var c = bounds.center;
            float radius = bounds.extents.magnitude;
            var camGo = new GameObject("__FountainOcclusionCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 1000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            // A colour no stone can be, so "did the fountain draw?" is a colour test
            // rather than a brightness guess that the skybox could satisfy.
            cam.backgroundColor = new Color(0f, 0f, 0f, 1f);
            var urpType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpType != null) camGo.AddComponent(urpType);

            // Fit the bounding sphere with a margin, so nothing can be clipped by a
            // framing error.
            float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.25f;
            var eye = c + new Vector3(0.62f, 0.45f, -0.65f).normalized * dist;
            camGo.transform.position = eye;
            camGo.transform.LookAt(c, Vector3.up);

            sb.AppendLine();
            sb.AppendLine("bounding radius        : " + radius.ToString("F2") + " m");
            sb.AppendLine("camera eye             : " + eye);
            sb.AppendLine("camera->centre dist    : " + Vector3.Distance(eye, c).ToString("F2") + " m (fit by FOV, not guessed)");

            // --- 2b. what does the camera actually hit first? ------------------
            var dir = (c - eye).normalized;
            var hits = Physics.RaycastAll(eye, dir, dist * 1.5f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            sb.AppendLine();
            sb.AppendLine("raycast from camera to fountain centre, in hit order:");
            if (hits.Length == 0) sb.AppendLine("   NOTHING HIT — no collider anywhere along the view line");
            foreach (var h in hits)
            {
                bool isMine = h.collider.transform.IsChildOf(root.transform);
                sb.AppendLine("   " + h.distance.ToString("F2").PadLeft(7) + " m  "
                    + (isMine ? "FOUNTAIN  " : "other     ")
                    + PathOf(h.collider.transform) + "  [" + h.collider.GetType().Name + "]");
            }

            // --- 3. renders ----------------------------------------------------
            var others = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            var wasEnabled = new bool[others.Length];
            for (int i = 0; i < others.Length; i++)
            {
                wasEnabled[i] = others[i].enabled;
                others[i].enabled = false;
            }

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            // Every renderer in the scene that is not part of the fountain.
            var allRenderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            var othersOn = new bool[allRenderers.Length];
            for (int i = 0; i < allRenderers.Length; i++)
            {
                othersOn[i] = allRenderers[i].enabled;
                if (!allRenderers[i].transform.IsChildOf(root.transform))
                    allRenderers[i].enabled = false;
            }

            int fountainOnly = Render(sb, cam, rt, "fountain_only");
            sb.AppendLine("   ^ non-black pixels with everything else hidden: " + fountainOnly
                + " of " + (W * H) + " (" + (100f * fountainOnly / (W * H)).ToString("F1") + "%)");

            for (int i = 0; i < allRenderers.Length; i++)
                if (allRenderers[i] != null) allRenderers[i].enabled = othersOn[i];

            int allOn = Render(sb, cam, rt, "everything");
            sb.AppendLine("   ^ non-black pixels with the whole village on  : " + allOn
                + " of " + (W * H) + " (" + (100f * allOn / (W * H)).ToString("F1") + "%)");

            // --- teardown ------------------------------------------------------
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            for (int i = 0; i < others.Length; i++)
                if (others[i] != null) others[i].enabled = wasEnabled[i];
            Object.DestroyImmediate(camGo);

            sb.AppendLine();
            sb.AppendLine("If fountain_only is near zero, the fountain's own renderers are not");
            sb.AppendLine("drawing and the restore question is moot until that is fixed.");
            Finish(sb);
        }

        static int Render(StringBuilder sb, Camera cam, RenderTexture rt, string name)
        {
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            var px = tex.GetPixels();
            int nonBlack = 0;
            for (int i = 0; i < px.Length; i++)
                if (px[i].r + px[i].g + px[i].b > 0.06f) nonBlack++;

            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Temp/oc_" + name + ".png"), bytes);
            Object.DestroyImmediate(tex);

            sb.AppendLine();
            sb.AppendLine(name + "  ->  Temp/oc_" + name + ".png  (" + bytes.Length.ToString("N0") + " bytes)");
            return nonBlack;
        }

        static string PathOf(Transform t)
        {
            if (t == null) return "<none>";
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        static void Finish(StringBuilder sb)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(
                Path.Combine(Directory.GetCurrentDirectory(), Report)));
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
