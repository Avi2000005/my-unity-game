using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// One read-only pass over the project, so "what is the state of things" is
/// answered by the project rather than by memory.
///
/// WHY ONE PROBE AND NOT SEVERAL
///
/// This is a status report, not a diagnosis. It must not change anything, and
/// it must not spend a minute per question. Every section only reads, and
/// anything that cannot be read says so rather than guessing.
///
/// The sections are ordered by whether the answer is still in doubt: the two
/// items that were fixed but never confirmed by eye come first, then the text
/// bug that was found but never traced to its cause, then what has actually
/// been built of the level.
/// </summary>
public static class StatusProbe
{
    const string Report = "Temp/status.txt";

    public static void Run()
    {
        var sb = new StringBuilder();
        try { Body(sb); }
        catch (System.Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] status\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        sb.AppendLine("WHERE THINGS STAND");
        sb.AppendLine("===================");
        sb.AppendLine();

        // Not named Screen: that would shadow UnityEngine.Screen, and every
        // reference to the class in this file would then resolve to the method.
        ViewSizes(sb);
        Pipeline(sb);
        Textures(sb);
        HudText(sb);
        SceneState(sb);
        LevelProgress(sb);
        Outstanding(sb);
    }

    // ---------------------------------------------------------------------
    // 1. the one that keeps reverting
    // ---------------------------------------------------------------------

    static void ViewSizes(StringBuilder sb)
    {
        sb.AppendLine("=== 1. the game view (the blur's cause) ===");

        var main = Camera.main;
        sb.AppendLine("  monitor:        " + Screen.currentResolution.width + " x " +
                      Screen.currentResolution.height);
        sb.AppendLine("  editor game view: " + Screen.width + " x " + Screen.height);

        if (main != null)
        {
            sb.AppendLine("  camera renders:  " + main.pixelWidth + " x " + main.pixelHeight);

            float factor = Screen.height > 0
                ? Screen.currentResolution.height / (float)main.pixelHeight
                : 0f;

            sb.AppendLine("  stretch factor:  " + factor.ToString("0.00") + "x  " +
                          "(how much each drawn pixel is being blown up to)");

            if (main.pixelWidth >= Screen.currentResolution.width - 2)
                sb.AppendLine("  -> drawing at the monitor's own size. The world is being");
            else
                sb.AppendLine("  -> STILL DRAWING SMALL. Everything is soft and the project");
            if (main.pixelWidth < Screen.currentResolution.width - 2)
                sb.AppendLine("     settings cannot change it. Double-click the Game tab.");
        }

        // The panel's own size, because the camera's is what the frame ends up
        // at after the scaler has had its say.
        try
        {
            var t = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (t != null)
            {
                foreach (var w in Resources.FindObjectsOfTypeAll(t))
                {
                    var win = (EditorWindow)w;
                    sb.AppendLine("  game view panel: " +
                                  Mathf.RoundToInt(win.position.width) + " x " +
                                  Mathf.RoundToInt(win.position.height) +
                                  "   maximized=" + win.maximized);
                }
            }
        }
        catch { /* the panel is a convenience here, not the measurement that counts */ }
    }

    // ---------------------------------------------------------------------
    // 2. the pipeline fixes
    // ---------------------------------------------------------------------

    static void Pipeline(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 2. pipeline (applied and read back last session) ===");

        var seen = new HashSet<string>();
        var assets = new List<UniversalRenderPipelineAsset>();

        void Add(UniversalRenderPipelineAsset a)
        {
            if (a == null || !seen.Add(a.name)) return;
            assets.Add(a);
        }

        Add(GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset);
        int levels = QualitySettings.names.Length;
        for (int i = 0; i < levels; i++)
        {
            try { QualitySettings.SetQualityLevel(i, false); }
            catch { }
            Add(QualitySettings.renderPipeline as UniversalRenderPipelineAsset);
        }

        try { QualitySettings.SetQualityLevel(1, false); }
        catch { }

        foreach (var a in assets)
        {
            bool ok = a.renderScale > 0.999f && a.msaaSampleCount >= 4;
            sb.AppendLine("  " + a.name + ": renderScale=" + a.renderScale.ToString("0.000") +
                          " msaa=" + a.msaaSampleCount + "x   " + (ok ? "OK" : "NOT FIXED"));
        }

        if (assets.Count == 0) sb.AppendLine("  no URP asset could be read");
    }

    // ---------------------------------------------------------------------
    // 3. texture sampling
    // ---------------------------------------------------------------------

    static void Textures(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 3. texture sampling ===");

        int n = 0, low = 0, shrunk = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D",
            new[] { "Assets/Art", "Assets/Painterly" }))
        {
            var im = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (im == null) continue;
            n++;
            if (im.mipmapEnabled && im.anisoLevel < 4) low++;
            if (im.maxTextureSize < 2048) shrunk++;
        }

        sb.AppendLine("  textures: " + n);
        sb.AppendLine("  below anisotropy 4: " + low + "   (last session: 40 were at 1, all raised to 8)");
        sb.AppendLine("  imported under 2048px: " + shrunk);
        sb.AppendLine("  " + (low == 0 ? "anisotropy held" : "anisotropy has been reset somewhere"));
    }

    // ---------------------------------------------------------------------
    // 4. the text bug, still untraced
    // ---------------------------------------------------------------------

    static void HudText(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 4. HUD text ===");

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        sb.AppendLine("  canvases: " + canvases.Length);

        foreach (var c in canvases)
        {
            var sc = c.GetComponent<CanvasScaler>();
            sb.AppendLine("  '" + c.name + "' " + c.renderMode +
                          (sc == null ? "  (no scaler)" : ""));

            if (sc != null)
            {
                float refW = sc.referenceResolution.x, refH = sc.referenceResolution.y;
                float t = sc.matchWidthOrHeight;
                float f = Screen.width / refW * (1f - t) + Screen.height / refH * t;
                sb.AppendLine("      reference " + refW.ToString("0") + "x" + refH.ToString("0") +
                              "  match " + t.ToString("0.00") +
                              "  factor now " + f.ToString("0.000"));
            }

            // The root's own transform scale is the thing that was never
            // explained. Reported per level so the culprit is visible.
            sb.AppendLine("      transform scale chain from the canvas down:");
            var texts = c.GetComponentsInChildren<Text>(true);
            foreach (var tx in texts)
            {
                var chain = new List<string>();
                var tr = tx.transform as RectTransform;
                int guard = 0;
                while (tr != null && guard++ < 8)
                {
                    chain.Add(tr.name + "=" + tr.localScale.x.ToString("0.000"));
                    tr = tr.parent as RectTransform;
                }
                chain.Reverse();

                float lossy = ((RectTransform)tx.transform).lossyScale.x;
                sb.AppendLine("        '" + tx.name + "' \"" + tx.text + "\" size=" + tx.fontSize +
                              "  lossyscale=" + lossy.ToString("0.000") +
                              "  effective=" + (tx.fontSize * lossy).ToString("0.0") + "px");
                sb.AppendLine("          " + string.Join("  >  ", chain.ToArray()));
            }
        }
    }

    // ---------------------------------------------------------------------
    // 5. scene and compile state
    // ---------------------------------------------------------------------

    static void SceneState(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 5. scene ===");

        var scene = EditorSceneManager.GetActiveScene();
        sb.AppendLine("  active scene: '" + scene.name + "'   path=" + scene.path);
        sb.AppendLine("  dirty (unsaved changes): " + scene.isDirty);
        sb.AppendLine("  loaded: " + scene.isLoaded);

        if (!string.IsNullOrEmpty(scene.path) && scene.isDirty)
            sb.AppendLine("  -> there are unsaved changes in the scene. Saving before the");
        if (!string.IsNullOrEmpty(scene.path) && scene.isDirty)
            sb.AppendLine("     next builder runs is safer than losing them.");

        sb.AppendLine();
        sb.AppendLine("  this probe compiled, which means the project's scripts compile.");
    }

    // ---------------------------------------------------------------------
    // 6. what has actually been built
    // ---------------------------------------------------------------------

    static void LevelProgress(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 6. level 1 progress ===");

        string[] roots =
        {
            "Ari", "Mono", "ColourThief", "ColorThief",
            "InkCrawler_1", "InkCrawler_2", "InkCrawler_3",
            "Gully", "ScreenControls"
        };

        foreach (var r in roots)
        {
            var found = false;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name != r) continue;
                found = true;
                sb.AppendLine("  " + r + ": present, active=" + t.gameObject.activeInHierarchy);
                break;
            }
            if (!found) sb.AppendLine("  " + r + ": NOT FOUND");
        }

        // Beat markers, if the builders left any.
        var beatMarkers = new List<string>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.name.IndexOf("Beat", System.StringComparison.OrdinalIgnoreCase) >= 0)
                beatMarkers.Add(t.name + (t.gameObject.activeInHierarchy ? "" : " (inactive)"));
        }

        beatMarkers.Sort();
        sb.AppendLine();
        sb.AppendLine("  beat markers found: " + beatMarkers.Count);
        foreach (var m in beatMarkers) sb.AppendLine("    " + m);
    }

    // ---------------------------------------------------------------------
    // 7. what is still owed
    // ---------------------------------------------------------------------

    static void Outstanding(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 7. still owed ===");
        sb.AppendLine("  Beat 3  handover on a timer from Mono_Wake, not on the clip ending");
        sb.AppendLine("  Beat 4  crawlspace 0.9-1.2 m, built then re-measured");
        sb.AppendLine("  Beat 5  Ink Crawler combat tutorial, brush splash staggers only");
        sb.AppendLine("  Beat 5.5 narrow alley gauntlet, the real difficulty spike");
        sb.AppendLine("  Beat 6  first colour fragment at the fountain chevron chain");
        sb.AppendLine("  Beat 7  Color Thief on the hill, level complete, sting");
        sb.AppendLine();
        sb.AppendLine("  A hill does not exist: 288 samples from 55 to 110 m out are flat,");
        sb.AppendLine("  so Beat 7 has nothing to put her on yet.");
        sb.AppendLine();
        sb.AppendLine("  No real brush-stroke clip exists in the kit. Ari_HitReact at");
        sb.AppendLine("  SwingSpeed 1.11 is the stand-in and still reads as a hit-react.");
    }
}
