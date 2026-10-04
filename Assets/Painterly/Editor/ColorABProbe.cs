using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Measures the grey-to-colour transition as a number instead of an opinion.
    ///
    /// Everything else in this project asserts that painting restores colour; this is
    /// the only thing that actually proves it. Two frames of the same view are
    /// compared — one with _ColorRestore = 0, one with 1 — and the difference in mean
    /// per-pixel saturation is reported.
    ///
    /// The environment is neutralised for the duration of the test, and that is not
    /// cosmetic. A blue sky or a warm ambient raises saturation on its own account,
    /// so a fully lit scene reads as "partly restored" even when the shader is doing
    /// nothing at all. With a null skybox, flat grey ambient, no fog and no
    /// reflections, the only thing left in frame that can carry colour is the
    /// village's own albedo — which is the thing under test.
    ///
    /// Setup and teardown are separate menu items because the actual screenshots are
    /// taken by the Scene View capture, which has to run between them:
    ///
    ///   1. Neutralise Environment
    ///   2. Frame Village      (twice, with SetRestore between the two captures)
    ///   3. capture grey PNG, capture colour PNG
    ///   4. Report Contrast
    ///   5. Restore Environment
    ///
    /// The original settings are stashed to Temp/color_ab_stash.json first, so a test
    /// that gets interrupted halfway still leaves a way back.
    /// </summary>
    public static class ColorABProbe
    {
        // Stash lives outside Assets so Unity never tries to import it. The probe
        // PNGs come from the Scene View capture tool, which writes relative to the
        // project root's Assets folder, so those are read back from Assets/Temp.
        const string StashPath = "Temp/color_ab_stash.json";
        const string CameraName = "__AB_Camera";
        const string GreyPng = "Assets/Temp/ab_grey.png";
        const string ColourPng = "Assets/Temp/ab_colour.png";

        /// <summary>Below this, a pixel counts as background rather than subject.</summary>
        const byte MinChroma = 6;

        [System.Serializable]
        class Stash
        {
            public int ambientMode;
            public Color ambientSky, ambientEquator, ambientGround;
            public float ambientIntensity, reflectionIntensity;
            public bool fog;
            public string skyboxName;
        }

        static string ProjectPath(params string[] parts)
        {
            var p = Path.GetDirectoryName(Application.dataPath);
            foreach (var s in parts) p = Path.Combine(p, s);
            return p;
        }

        // ---- setup --------------------------------------------------------

        [MenuItem("Tools/Echoes/Probe/1. Neutralise Environment", priority = 40)]
        public static void Neutralise()
        {
            StashAndNeutralise();
            Debug.Log("[Echoes] Environment neutralised for the A/B probe. " +
                      "Screenshot both states, then run '4. Report Contrast'.");
        }

        [MenuItem("Tools/Echoes/Probe/2. Frame Village", priority = 41)]
        public static void FrameVillage()
        {
            var village = GameObject.Find("Village_Grey");
            if (village == null)
            {
                Debug.LogError("[Echoes] No Village_Grey in the scene. Run Generate Grey Village first.");
                return;
            }

            var view = SceneView.lastActiveSceneView;
            if (view == null)
            {
                Debug.LogError("[Echoes] Open a Scene View before framing.");
                return;
            }

            // Frame from the village's own bounds rather than a hard-coded position,
            // so this keeps working when the building count or layout settings change.
            var b = new Bounds(village.transform.position, Vector3.zero);
            foreach (var r in village.GetComponentsInChildren<Renderer>(true))
            {
                if (r.transform.name == "Ground") continue;
                b.Encapsulate(r.bounds);
            }

            view.LookAt(b.center, Quaternion.Euler(32f, 38f, 0f));
            view.pivot = b.center;
            view.size = Mathf.Max(b.extents.x, b.extents.z) * 1.15f;
            view.sceneViewState.alwaysRefresh = true;
            view.Repaint();

            Debug.Log($"[Echoes] Framed village at {b.center} " +
                      $"(extent {b.size.x:F0} x {b.size.z:F0} x {b.size.y:F0}).");
        }

        // ---- the two states -----------------------------------------------

        [MenuItem("Tools/Echoes/Probe/3a. Set Grey (Restore = 0)", priority = 42)]
        public static void SetGrey() => SetRestore(0f);

        [MenuItem("Tools/Echoes/Probe/3b. Set Colour (Restore = 1)", priority = 43)]
        public static void SetColour() => SetRestore(1f);

        static void SetRestore(float value)
        {
            var village = GameObject.Find("Village_Grey");
            if (village == null)
            {
                Debug.LogError("[Echoes] No Village_Grey in the scene.");
                return;
            }

            ColorRestoreTarget.SetAllImmediate(value);
            SceneView.lastActiveSceneView?.Repaint();
            Debug.Log($"[Echoes] _ColorRestore set to {value} on " +
                      $"{village.GetComponentsInChildren<ColorRestoreTarget>(true).Length} target(s).");
        }

        // ---- verdict -------------------------------------------------------

        [MenuItem("Tools/Echoes/Probe/4. Report Contrast", priority = 44)]
        public static void Report()
        {
            var grey = MeanSaturation(GreyPng);
            var colour = MeanSaturation(ColourPng);

            if (grey.pixels == 0 || colour.pixels == 0)
            {
                Debug.LogError("[Echoes] Missing probe PNGs. Capture both states first " +
                               "(grey and colour) before reporting.");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("[Echoes] Grey/Colour contrast");
            sb.AppendLine($"  _ColorRestore = 0   mean saturation {grey.mean:F4}  over {grey.pixels} px");
            sb.AppendLine($"  _ColorRestore = 1   mean saturation {colour.mean:F4}  over {colour.pixels} px");
            sb.AppendLine($"  lift {colour.mean - grey.mean:F4}   ratio {colour.mean / Mathf.Max(0.0001f, grey.mean):F1}x");

            // A grey world that still reads as colourful means desaturation is not
            // reaching the albedo. A painted world that does not lift means
            // _ColorRestore is not reaching the renderer. Both are silent at a
            // glance, so they get an explicit verdict rather than a number to
            // interpret.
            if (grey.mean > 0.12f)
                sb.AppendLine("  VERDICT FAIL: the grey pass is not actually grey.");
            else if (colour.mean < grey.mean * 1.5f)
                sb.AppendLine("  VERDICT FAIL: restoring colour barely moved saturation.");
            else
                sb.AppendLine("  VERDICT PASS: the village is grey, and painting restores colour.");

            Debug.Log(sb.ToString());
        }

        struct Sat
        {
            public float mean;
            public int pixels;
        }

        /// <summary>
        /// Mean HSV saturation over the pixels that differ from a flat background.
        ///
        /// Averaging over every pixel including the background would drag the mean
        /// toward zero and hide a real change; excluding near-grey pixels is the same
        /// idea, and it also strips out sky and any residual clear colour.
        /// </summary>
        static Sat MeanSaturation(string relativePath)
        {
            string path = ProjectPath(relativePath.Split('/'));
            if (!File.Exists(path)) return new Sat();

            // RGBA32, not RGB24: RGB24 silently drops the alpha channel, so a
            // capture with a transparent background becomes indistinguishable from
            // opaque black. Keeping alpha lets the filter below reject background
            // for the right reason instead of by coincidence.
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(path))) return new Sat();

            var px = tex.GetPixels32();
            double sum = 0;
            int n = 0;

            for (int i = 0; i < px.Length; i++)
            {
                var c = px[i];
                if (c.a < 128) continue;             // transparent background

                int max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                int min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));

                // max is at least MinChroma here, so the divide below is safe.
                if (max - min < MinChroma) continue;  // near-grey subject or sky

                sum += (double)(max - min) / max;
                n++;
            }

            Object.DestroyImmediate(tex);
            return new Sat { mean = n > 0 ? (float)(sum / n) : 0f, pixels = n };
        }

        // ---- teardown ------------------------------------------------------

        [MenuItem("Tools/Echoes/Probe/5. Restore Environment", priority = 45)]
        public static void RestoreEnvironment()
        {
            string path = ProjectPath(StashPath);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[Echoes] No stashed environment to restore.");
                return;
            }

            var s = JsonUtility.FromJson<Stash>(File.ReadAllText(path));

            RenderSettings.ambientMode = (AmbientMode)s.ambientMode;
            RenderSettings.ambientSkyColor = s.ambientSky;
            RenderSettings.ambientEquatorColor = s.ambientEquator;
            RenderSettings.ambientGroundColor = s.ambientGround;
            RenderSettings.ambientIntensity = s.ambientIntensity;
            RenderSettings.reflectionIntensity = s.reflectionIntensity;
            RenderSettings.fog = s.fog;

            // The skybox is restored by name, looked up as an asset. Losing it is
            // survivable and worth saying out loud rather than failing silently.
            var sky = AssetDatabase.FindAssets("t:Material " + s.skyboxName);
            if (sky != null && sky.Length > 0)
            {
                var loaded = AssetDatabase.LoadAssetAtPath<Material>(
                    AssetDatabase.GUIDToAssetPath(sky[0]));
                if (loaded != null && loaded.shader != null &&
                    loaded.shader.name == "Skybox/Procedural")
                    RenderSettings.skybox = loaded;
            }

            Debug.Log($"[Echoes] Environment restored: ambientMode={s.ambientMode}, " +
                      $"fog={s.fog}, reflectionIntensity={s.reflectionIntensity}, " +
                      $"skybox='{s.skyboxName}'. If the skybox looks wrong, re-assign it " +
                      $"on the Lighting panel.");
        }

        static void StashAndNeutralise()
        {
            var s = new Stash
            {
                ambientMode = (int)RenderSettings.ambientMode,
                ambientSky = RenderSettings.ambientSkyColor,
                ambientEquator = RenderSettings.ambientEquatorColor,
                ambientGround = RenderSettings.ambientGroundColor,
                ambientIntensity = RenderSettings.ambientIntensity,
                reflectionIntensity = RenderSettings.reflectionIntensity,
                fog = RenderSettings.fog,
                skyboxName = RenderSettings.skybox != null
                    ? AssetDatabase.GetAssetPath(RenderSettings.skybox)
                    : "(none)",
            };
            File.WriteAllText(ProjectPath(StashPath), JsonUtility.ToJson(s));

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.fog = false;
        }
    }
}
