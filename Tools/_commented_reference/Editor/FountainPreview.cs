using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Renders the fountain from a few fixed viewpoints so it can be judged
    /// without a human standing at the editor.
    ///
    /// A Scene view capture would depend on wherever the user last left the
    /// camera, which makes "show me the result" a coin flip. This builds its own
    /// camera, aims it explicitly, and writes PNGs — and it renders the basin
    /// twice, once in the Grey Realm and once fully restored, because the whole
    /// point of the prop is that it changes when Ari paints it. A single grey
    /// frame would prove the geometry and say nothing about the colour path.
    /// </summary>
    public static class FountainPreview
    {
        const string Report = "Temp/fountain_preview.txt";
        const int W = 960, H = 640;

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] fountain preview");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sb.AppendLine("STOPPED: exit play mode first (Ctrl+P).");
                Finish(sb);
                return;
            }

            var root = GameObject.Find("Fountain");
            if (root == null)
            {
                sb.AppendLine("FATAL: no 'Fountain' in the scene. Run Tools/Echoes/Build Fountain.");
                Finish(sb);
                return;
            }

            var target = root.GetComponent<ColorRestoreTarget>();
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                bounds.Encapsulate(r.bounds);

            var c = bounds.center;
            sb.AppendLine($"fountain centre : {c}");
            sb.AppendLine($"fountain size   : {bounds.size}");

            var camGo = new GameObject("__FountainPreviewCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 500f;
            // URP refuses to render through a camera with no additional data, so
            // the component has to be asked for by name rather than by type — the
            // editor assembly does not reference the URP runtime by default.
            var urpType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpType != null) camGo.AddComponent(urpType);

            // Turn off every other camera so nothing renders on top of the shot.
            var others = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            var wasEnabled = new bool[others.Length];
            for (int i = 0; i < others.Length; i++)
            {
                wasEnabled[i] = others[i].enabled;
                others[i].enabled = false;
            }

            var shots = new[]
            {
                new { name = "01_front",       eye = c + new Vector3(0.0f, 2.6f, -7.4f), look = c + new Vector3(0, 0.5f, 0), restore = 0f },
                new { name = "02_three_quarter", eye = c + new Vector3(5.4f, 3.0f, -5.6f), look = c + new Vector3(0, 0.5f, 0), restore = 0f },
                new { name = "03_statue_close", eye = c + new Vector3(1.5f, 1.55f, -1.85f), look = c + new Vector3(0.25f, 0.95f, 0.25f), restore = 0f },
                new { name = "04_side",        eye = c + new Vector3(-6.8f, 2.8f, 2.2f), look = c + new Vector3(0, 0.5f, 0), restore = 0f },
                new { name = "05_restored",    eye = c + new Vector3(5.4f, 3.0f, -5.6f), look = c + new Vector3(0, 0.5f, 0), restore = 1f },
                new { name = "06_restored_top",eye = c + new Vector3(0.4f, 6.2f, -3.4f), look = c + new Vector3(0, 0.3f, 0), restore = 1f },
            };

            Directory.CreateDirectory(Path.GetDirectoryName(
                Path.Combine(Directory.GetCurrentDirectory(), Report)));

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;

            foreach (var s in shots)
            {
                if (target != null) target.SetRestoreImmediate(s.restore);

                camGo.transform.position = s.eye;
                camGo.transform.LookAt(s.look, Vector3.up);

                cam.Render();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;

                var bytes = tex.EncodeToPNG();
                var path = Path.Combine(Directory.GetCurrentDirectory(), $"Temp/fountain_{s.name}.png");
                File.WriteAllBytes(path, bytes);
                Object.DestroyImmediate(tex);

                sb.AppendLine($"  {s.name,-18} restore={s.restore:F0}  {bytes.Length:N0} bytes -> Temp/fountain_{s.name}.png");
            }

            if (target != null) target.SetRestoreImmediate(0f);   // leave it grey

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            for (int i = 0; i < others.Length; i++)
                if (others[i] != null) others[i].enabled = wasEnabled[i];
            Object.DestroyImmediate(camGo);

            sb.AppendLine("done. Fountain left in the Grey Realm (restore=0).");
            Finish(sb);
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
