using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Works out whether <c>_ColorRestore</c> actually brings colour back, and by
    /// which route.
    ///
    /// An earlier version of this test rendered the fountain grey, then rendered it
    /// restored, and found the two images eight bytes apart — and concluded that the
    /// colour path was broken. That conclusion was wrong, and the mistake is worth
    /// keeping written down: the fountain was standing inside House_0_0 at the time,
    /// so a camera outside the house was looking at a wall. Every frame in the test,
    /// including the deliberately impossible magenta one, was a picture of the same
    /// wall. Five identical images proved the fountain was not on screen, and said
    /// nothing at all about the shader.
    ///
    /// So the camera is chosen rather than assumed here. A ring of candidate eye
    /// positions is tested and the first one with a clear line to the fountain's
    /// centre is used, and the shots are compared by non-background pixel count
    /// inside Unity as well as written out as PNGs. The magenta albedo shot is kept
    /// as the control it should always have been: if swapping the basin's albedo to a
    /// colour no stone could be does not change the image, the frame is not showing
    /// the fountain and no later measurement in the run means anything.
    ///
    /// What the shots separate:
    ///
    ///   A  baseline, grey
    ///   B  magenta albedo       — is the fountain on screen at all?
    ///   C  material _ColorRestore = 1 — does the shader honour the value?
    ///   D  property block _ColorRestore = 1 — does the path the brush uses work?
    ///
    /// C working while D does not is the signature of a batcher that carries
    /// UnityPerMaterial values from the batch and ignores per-renderer overrides —
    /// which is exactly the trade the shader made when it moved _ColorRestore into
    /// UnityPerMaterial to stay GPU Resident Drawer compatible.
    /// </summary>
    public static class FountainRestoreProbe
    {
        const string Report = "Temp/fountain_restore.txt";
        const int W = 960, H = 640;

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] fountain restore probe");

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

            var target = root.GetComponent<ColorRestoreTarget>();
            var stone = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Painterly/Materials/RockTrim.mat");
            if (stone == null)
            {
                sb.AppendLine("FATAL: RockTrim.mat not found.");
                Finish(sb);
                return;
            }

            float origRestore = stone.GetFloat("_ColorRestore");
            Color origColor = stone.GetColor("_BaseColor");

            sb.AppendLine();
            sb.AppendLine("--- material ---");
            sb.AppendLine("RockTrim _ColorRestore = " + origRestore);
            sb.AppendLine("RockTrim _BaseColor    = " + origColor);
            sb.AppendLine("RockTrim shader        = " + stone.shader.name);
            sb.AppendLine("keywords               = " + string.Join(", ", stone.shaderKeywords));

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            sb.AppendLine();
            sb.AppendLine("--- the fountain ---");
            sb.AppendLine("renderers              = " + renderers.Length);
            sb.AppendLine("root position          = " + root.transform.position);
            sb.AppendLine("root scale             = " + root.transform.localScale);

            var bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            sb.AppendLine("world bounds           = " + bounds.size + " at " + bounds.center);

            int withBlock = 0;
            foreach (var r in renderers) if (r.HasPropertyBlock()) withBlock++;
            if (renderers.Length > 0)
            {
                var b0 = new MaterialPropertyBlock();
                renderers[0].GetPropertyBlock(b0);
                sb.AppendLine("with a property block  = " + withBlock + " of " + renderers.Length
                              + ", renderer[0] block _ColorRestore = " + b0.GetFloat("_ColorRestore"));
            }

            var urp = GraphicsSettings.currentRenderPipeline;
            sb.AppendLine();
            sb.AppendLine("--- pipeline ---");
            sb.AppendLine("pipeline               = " + (urp == null ? "<built-in>" : urp.name));
            if (urp != null)
            {
                var so = new SerializedObject(urp);
                foreach (var prop in new[]
                {
                    "m_GPUResidentDrawerMode",
                    "m_GPUResidentDrawerEnableOcclusionCullingInCameras",
                    "m_UseSRPBatcher",
                })
                {
                    var p = so.FindProperty(prop);
                    sb.AppendLine("  " + prop.PadRight(52) + " = "
                                  + (p == null ? "<absent>" : p.intValue.ToString()));
                }
            }

            // --- camera: chosen for a clear line, not assumed --------------------
            var eye = PickEye(root, bounds, out var occluders, out var eyeDetail);
            sb.AppendLine();
            sb.AppendLine("--- camera ---");
            sb.AppendLine("eye                    = " + eye);
            sb.AppendLine(eyeDetail);

            var camGo = new GameObject("__FountainRestoreCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 45f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 800f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 1f);
            var urpCamType = System.Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpCamType != null) camGo.AddComponent(urpCamType);

            camGo.transform.position = eye;
            camGo.transform.LookAt(bounds.center, Vector3.up);

            var others = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            var wasEnabled = new bool[others.Length];
            for (int i = 0; i < others.Length; i++)
            {
                wasEnabled[i] = others[i].enabled;
                others[i].enabled = false;
            }

            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;

            // --- shots ----------------------------------------------------------
            sb.AppendLine();
            sb.AppendLine("--- shots ---");
            Shot(sb, cam, rt, "A_grey_baseline", "nothing touched", null);

            // Control: an albedo no stone could be. If this does not change the
            // frame, the fountain is not on screen and nothing below means anything.
            stone.SetColor("_BaseColor", new Color(1f, 0f, 0.6f, 1f));
            stone.SetFloat("_ColorRestore", origRestore);
            Shot(sb, cam, rt, "B_magenta_control", "material albedo magenta", null);

            stone.SetColor("_BaseColor", origColor);
            stone.SetFloat("_ColorRestore", 1f);
            Shot(sb, cam, rt, "C_material_restore", "material _ColorRestore = 1", null);

            stone.SetFloat("_ColorRestore", origRestore);
            Shot(sb, cam, rt, "D_block_restore", "block _ColorRestore = 1", () =>
            {
                if (target != null) target.SetRestoreImmediate(1f);
            });

            Shot(sb, cam, rt, "E_block_grey", "block _ColorRestore = 0", () =>
            {
                if (target != null) target.SetRestoreImmediate(0f);
            });

            // --- verdict --------------------------------------------------------
            sb.AppendLine();
            sb.AppendLine("--- verdict: pixels that CHANGED between shots ---");
            sb.AppendLine("A vs B  magenta control       : " + Verdict("A_grey_baseline", "B_magenta_control")
                          + "   <- must be large, or the fountain is not on screen");
            sb.AppendLine("A vs C  material restore      : " + Verdict("A_grey_baseline", "C_material_restore")
                          + "   <- does the shader honour the value?");
            sb.AppendLine("A vs D  property-block restore: " + Verdict("A_grey_baseline", "D_block_restore")
                          + "   <- does the brush's route work?");
            sb.AppendLine("D vs E  block on vs off       : " + Verdict("D_block_restore", "E_block_grey"));

            if (occluders.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("NOTE: something was still in front of the fountain at the chosen");
                sb.AppendLine("eye, so read the numbers above with that in mind:");
                foreach (var o in occluders) sb.AppendLine("   " + o);
            }

            // --- teardown: leave everything exactly as found ---------------------
            stone.SetColor("_BaseColor", origColor);
            stone.SetFloat("_ColorRestore", origRestore);
            EditorUtility.SetDirty(stone);
            if (target != null) target.SetRestoreImmediate(0f);
            AssetDatabase.SaveAssets();

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            for (int i = 0; i < others.Length; i++)
                if (others[i] != null) others[i].enabled = wasEnabled[i];
            Object.DestroyImmediate(camGo);

            sb.AppendLine();
            sb.AppendLine("materials restored to their original values.");
            Finish(sb);
        }

        /// <summary>
        /// Walk candidate directions and distances until one has a clear line to the
        /// fountain's centre, so an occluded frame cannot masquerade as a result.
        /// </summary>
        static Vector3 PickEye(GameObject root, Bounds bounds,
                               out string[] occluders, out string detail)
        {
            float radius = bounds.extents.magnitude;
            var centre = bounds.center;
            float fit = radius / Mathf.Sin(45f * 0.5f * Mathf.Deg2Rad);

            var list = new System.Collections.Generic.List<string>();
            var dirs = new[]
            {
                new Vector3(0.62f, 0.42f, -0.66f),
                new Vector3(-0.62f, 0.42f, -0.66f),
                new Vector3(0.00f, 0.40f, -0.92f),
                new Vector3(0.90f, 0.40f, 0.00f),
                new Vector3(-0.90f, 0.40f, 0.00f),
                new Vector3(0.00f, 0.45f, 0.92f),
                new Vector3(0.50f, 0.55f, 0.66f),
                new Vector3(-0.50f, 0.55f, 0.66f),
            };
            var mults = new[] { 1.0f, 1.35f, 1.8f, 2.4f, 3.2f };

            foreach (var m in mults)
            {
                foreach (var d in dirs)
                {
                    var eye = centre + d.normalized * (fit * m);
                    var dir = (centre - eye).normalized;
                    var hits = Physics.RaycastAll(eye, dir, Vector3.Distance(eye, centre) * 1.2f,
                                                 ~0, QueryTriggerInteraction.Ignore);
                    System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

                    bool clear = hits.Length > 0 && hits[0].collider.transform.IsChildOf(root.transform);
                    if (clear)
                    {
                        occluders = new string[0];
                        detail = "framing                : fit distance x" + m.ToString("F2")
                                 + ", clear line of sight to the centre";
                        return eye;
                    }

                    list.Clear();
                    foreach (var h in hits)
                    {
                        bool mine = h.collider.transform.IsChildOf(root.transform);
                        list.Add(h.distance.ToString("F2") + " m "
                                 + (mine ? "FOUNTAIN " : "other    ")
                                 + SquarePlacement.PathOf(h.collider.transform));
                        if (mine) break;
                    }
                }
            }

            // Nothing was clear; take the widest shot and report what is in the way.
            var fallback = centre + new Vector3(0.62f, 0.42f, -0.66f).normalized * (fit * 3.2f);
            var fdir = (centre - fallback).normalized;
            var fhits = Physics.RaycastAll(fallback, fdir, Vector3.Distance(fallback, centre) * 1.2f,
                                          ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(fhits, (x, y) => x.distance.CompareTo(y.distance));
            list.Clear();
            foreach (var h in fhits)
                list.Add(h.distance.ToString("F2") + " m  " + SquarePlacement.PathOf(h.collider.transform));

            occluders = list.ToArray();
            detail = "framing                : NO clear line from any candidate; "
                     + "fell back to the widest shot";
            return fallback;
        }

        static int Shot(StringBuilder sb, Camera cam, RenderTexture rt, string name,
                        string what, System.Action before)
        {
            before?.Invoke();
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            var px = tex.GetPixels32();
            int lit = 0;
            for (int i = 0; i < px.Length; i++)
                if ((int)px[i].r + px[i].g + px[i].b > 15) lit++;

            _shots[name] = px;

            var bytes = tex.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Temp/fr_" + name + ".png"), bytes);
            Object.DestroyImmediate(tex);

            sb.AppendLine(name.PadRight(24) + what.PadRight(32)
                          + "lit " + lit.ToString("N0").PadLeft(9) + " px   "
                          + bytes.Length.ToString("N0") + " bytes");
            return lit;
        }

        static readonly System.Collections.Generic.Dictionary<string, Color32[]> _shots =
            new System.Collections.Generic.Dictionary<string, Color32[]>();

        /// <summary>
        /// How many pixels actually differ between two shots, and by how much.
        ///
        /// Counting lit pixels would have been nearly useless here. Restoring colour
        /// changes what the pixels are, not how many of them the fountain covers, so
        /// two shots could be visibly different and still score the same. Counting
        /// changed pixels answers the question that was actually asked: did setting
        /// this value change the picture?
        ///
        /// The threshold is a channel-sum of 6 out of 765, which is above render
        /// noise and well below anything a real colour change produces.
        /// </summary>
        static string Verdict(string a, string b)
        {
            if (!_shots.TryGetValue(a, out var pa) || !_shots.TryGetValue(b, out var pb))
                return "MISSING SHOT";
            if (pa.Length != pb.Length) return "SIZE MISMATCH";

            int changed = 0;
            int maxDelta = 0;
            for (int i = 0; i < pa.Length; i++)
            {
                int d = Mathf.Abs(pa[i].r - pb[i].r) + Mathf.Abs(pa[i].g - pb[i].g)
                        + Mathf.Abs(pa[i].b - pb[i].b);
                if (d > 6) changed++;
                if (d > maxDelta) maxDelta = d;
            }

            float pct = 100f * changed / pa.Length;
            return changed.ToString("N0") + " px changed (" + pct.ToString("F2")
                   + "% of frame), max delta " + maxDelta + "/765";
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
