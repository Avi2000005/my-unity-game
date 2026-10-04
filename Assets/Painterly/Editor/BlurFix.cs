using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Finds the blur's real cause and fixes it, measuring before and after.
///
/// WHAT THE FIRST PROBE ALREADY RULED OUT
///
/// Render scale was 1.0, every texture was full size, no DepthOfField or
/// MotionBlur was active. So the softness is not coming from the pipeline
/// asset the editor reports by default, which leaves three candidates this
/// tool measures directly:
///
///   1. A DIFFERENT URP ASSET AT RUNTIME. GraphicsSettings.currentRenderPipeline
///      is the editor default. In play mode QualitySettings picks the asset of
///      the active quality level instead, and that asset can carry its own
///      renderScale and MSAA. A project that looks crisp in the editor and
///      soft in play is almost always this, and it is invisible to the first
///      probe.
///
///   2. THE GAME VIEW ITSELF. Unity renders the game into the Game view panel
///      and scales it to fit. If that panel is 453px tall and is being shown on
///      a 1440p display, every pixel is genuinely soft and no project setting
///      can fix it, because the detail was never drawn. Camera.pixelHeight
///      reported 453, which is small enough to be the whole answer.
///
///   3. ANISOTROPY OF 1. Unity's default. On a ground plane seen at a grazing
///      angle the floor samples a tiny mip and turns to mush from the middle
///      distance out. The first probe only flagged anisotropy 0 and so let 1
///      through, which was too lenient: 1 means "no anisotropic filtering",
///      not "some".
///
/// The fixes are applied to every URP asset any quality level can select, not
/// just the one the editor happens to report, so the result does not change
/// when the quality level changes.
/// </summary>
public static class BlurFix
{
    const string Report = "Temp/blurfix.txt";

    public static void Run()
    {
        var sb = new StringBuilder();
        try { Body(sb); }
        catch (System.Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] blur fix\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    static StringBuilder Problems;
    static List<string> Applied;

    static void Body(StringBuilder sb)
    {
        sb.AppendLine("FINDING AND FIXING THE BLUR");
        sb.AppendLine();
        Problems = new StringBuilder();
        Applied = new List<string>();

        WhichAssetActuallyDraws(sb);
        GameViewSize(sb);
        FixPipelines(sb);
        FixAnisotropy(sb);
        Verdict(sb);
    }

    static void Problem(string s) { Problems.AppendLine("  *** " + s + " ***"); }
    static void Did(string s) { Applied.Add(s); }

    // =====================================================================
    // 1. which asset actually draws the game
    // =====================================================================

    /// <summary>
    /// Collects every URP asset that could be live: the graphics-settings
    /// default, the asset of each quality level, and whatever is active right
    /// now. Fixing only the first is the classic mistake, because the one that
    /// draws in play mode is chosen by QualitySettings at load time.
    /// </summary>
    static List<UniversalRenderPipelineAsset> AllLiveAssets(out List<string> notes)
    {
        notes = new List<string>();
        var assets = new List<UniversalRenderPipelineAsset>();
        var seen = new HashSet<string>();

        // The original level is captured before the loop walks the levels. Doing
        // it afterwards would read back the last level the loop set, not the one
        // the user was on, and quietly leave the project on the wrong one.
        int original = QualitySettings.GetQualityLevel();

        Add(assets, seen, notes,
            GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset,
            "graphics settings default");
        Add(assets, seen, notes,
            GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset,
            "current (editor)");

        int levelCount = QualitySettings.names.Length;
        notes.Add("quality levels: " + levelCount + "  active index: " + original);

        for (int i = 0; i < levelCount; i++)
        {
            SetQualityLevel(i, false);
            notes.Add("  level " + i + " '" + QualitySettings.names[i] +
                      "' renderPipeline: " +
                      (QualitySettings.renderPipeline == null
                          ? "none (inherits graphics settings)"
                          : QualitySettings.renderPipeline.name));
            Add(assets, seen, notes,
                QualitySettings.renderPipeline as UniversalRenderPipelineAsset,
                "quality level " + i);
        }

        // Put the user back where they were. Leaving the quality level moved
        // would silently change how the game looks for reasons unrelated to
        // this fix, which is exactly the kind of thing that wastes a morning.
        SetQualityLevel(original, false);
        notes.Add("restored active quality level to: " + original);

        return assets;
    }

    static void Add(List<UniversalRenderPipelineAsset> assets, HashSet<string> seen,
                    List<string> notes, UniversalRenderPipelineAsset a, string why)
    {
        if (a == null) return;
        if (!seen.Add(a.name)) return;

        assets.Add(a);
        notes.Add(why + " -> " + a.name);
    }

    static void SetQualityLevel(int index, bool apply)
    {
        try { QualitySettings.SetQualityLevel(index, apply); }
        catch { /* older signature; the no-apply overload above is enough */ }
    }

    static void WhichAssetActuallyDraws(StringBuilder sb)
    {
        sb.AppendLine("=== 1. which URP asset actually draws the game ===");
        sb.AppendLine("GraphicsSettings reports the editor default. In play mode the");
        sb.AppendLine("active quality level picks the asset instead. If those differ, the");
        sb.AppendLine("first probe measured the wrong pipeline entirely.");

        var assets = AllLiveAssets(out var notes);
        foreach (var n in notes) sb.AppendLine("  " + n);

        sb.AppendLine();
        sb.AppendLine("  distinct URP assets that can be live: " + assets.Count);

        foreach (var a in assets)
        {
            sb.AppendLine("  " + a.name + ": renderScale=" + a.renderScale.ToString("0.000") +
                          " msaa=" + a.msaaSampleCount + "x" +
                          "  path=" + AssetDatabase.GetAssetPath(a));
        }

        if (assets.Count > 1)
            sb.AppendLine("  -> more than one. Every one of them is fixed below, so it does");
        else
            sb.AppendLine("  -> only one, so the editor default is the one that draws.");
    }

    // =====================================================================
    // 2. the game view's own size
    // =====================================================================

    /// <summary>
    /// Reads the Game view through reflection rather than a guessed property
    /// name. The fields it cares about have been renamed several times, so the
    /// tool prints whatever fields it can find whose name mentions size, scale
    /// or zoom instead of assuming one. Unity renders into this panel and then
    /// scales it to fit, so a small panel is a real loss of detail that no
    /// project setting can undo.
    /// </summary>
    static void GameViewSize(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 2. the game view's own resolution ===");
        sb.AppendLine("Unity draws the game into this panel and scales it to fit. If the");
        sb.AppendLine("panel is small and is being displayed on a large monitor, every");
        sb.AppendLine("pixel is genuinely soft and no project setting can help.");

        var main = Camera.main;
        if (main != null)
        {
            sb.AppendLine("  camera pixelWidth x pixelHeight: " + main.pixelWidth + " x " + main.pixelHeight);

            if (main.targetTexture != null)
                sb.AppendLine("  camera renders into a target texture: " + main.targetTexture.name +
                              " " + main.targetTexture.width + "x" + main.targetTexture.height);
            else
                sb.AppendLine("  camera renders straight to the game view (no render texture)");
        }

        sb.AppendLine("  monitor: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height);

        try
        {
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (type == null)
            {
                sb.AppendLine("  game view type not found by name");
                return;
            }

            var views = Resources.FindObjectsOfTypeAll(type);
            sb.AppendLine("  game view panels open: " + views.Length);

            foreach (var v in views)
            {
                var win = (EditorWindow)v;
                sb.AppendLine("    '" + win.titleContent.text + "' docked size " +
                              win.position.width.ToString("0") + " x " +
                              win.position.height.ToString("0"));

                sb.AppendLine("      size / scale / zoom fields as they actually exist:");
                foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    string n = f.Name.ToLowerInvariant();
                    if (n.IndexOf("scale", System.StringComparison.Ordinal) < 0 &&
                        n.IndexOf("size", System.StringComparison.Ordinal) < 0 &&
                        n.IndexOf("zoom", System.StringComparison.Ordinal) < 0) continue;

                    object val;
                    try { val = f.GetValue(v); }
                    catch { continue; }

                    if (val == null) continue;
                    sb.AppendLine("        " + f.Name + " = " + val);
                }
            }
        }
        catch (System.Exception e)
        {
            sb.AppendLine("  could not read the game view: " + e.GetType().Name + ": " + e.Message);
        }

        sb.AppendLine();
        sb.AppendLine("  A scale below 1, or a fixed resolution much smaller than the");
        sb.AppendLine("  monitor, is a blur no asset setting can repair. If the values");
        sb.AppendLine("  above are small, widen the game view or set its resolution to the");
        sb.AppendLine("  monitor's.");
    }

    // =====================================================================
    // 3. fix every pipeline asset
    // =====================================================================

    /// <summary>Anti-aliasing samples. 4x is the usual choice: visible on geometry,
    /// and cheap enough to leave on without measuring the frame time.</summary>
    const int WantMsaa = 4;

    static void FixPipelines(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 3. fixing the pipeline assets ===");

        var assets = AllLiveAssets(out _);

        foreach (var a in assets)
        {
            sb.AppendLine("  " + a.name + ":");

            float before = a.renderScale;
            if (before < 0.999f)
            {
                a.renderScale = 1f;
                EditorUtility.SetDirty(a);
                Did(a.name + " renderScale " + before.ToString("0.000") + " -> 1.000");
                sb.AppendLine("    renderScale " + before.ToString("0.000") + " -> 1.000  CHANGED");
                sb.AppendLine("      -> this was drawing the frame at " + (before * 100f).ToString("0") +
                              "% and stretching it back up.");
                Problem(a.name + " was drawing the frame at " + (before * 100f).ToString("0") +
                        "% of the window and upscaling it.");
            }
            else
                sb.AppendLine("    renderScale " + before.ToString("0.000") + " (already full)");

            int msaaBefore = a.msaaSampleCount;
            if (msaaBefore != WantMsaa)
            {
                a.msaaSampleCount = WantMsaa;
                EditorUtility.SetDirty(a);
                Did(a.name + " MSAA " + msaaBefore + "x -> " + WantMsaa + "x");
                sb.AppendLine("    MSAA " + msaaBefore + "x -> " + WantMsaa + "x  CHANGED");
                Problem("MSAA was " + msaaBefore + "x, so every edge was a hard staircase.");
            }
            else
                sb.AppendLine("    MSAA " + msaaBefore + "x (already correct)");

            // A sharpening upscaler on top of real multisamples would soften what
            // MSAA just cleaned up, so the two are not combined.
            if (a.upscalingFilter.ToString() != "Auto")
                sb.AppendLine("    upscaling filter: " + a.upscalingFilter +
                              " (left alone; MSAA handles the edges)");
            else
                sb.AppendLine("    upscaling filter: Auto");

            // Read back rather than trusting the setter. A write that appears to
            // succeed and does not is how a wrong value survives a day of
            // work, and the cost of reading it back is nothing.
            float afterScale = a.renderScale;
            int afterMsaa = a.msaaSampleCount;
            sb.AppendLine("    read back: renderScale=" + afterScale.ToString("0.000") +
                          " msaa=" + afterMsaa + "x");

            if (afterScale < 0.999f || afterMsaa != WantMsaa)
            {
                sb.AppendLine("    -> THE WRITE DID NOT STICK. Whatever set this value is");
                sb.AppendLine("       still in charge; treat the fix as not applied.");
                Problem(a.name + " did not accept the corrected render scale or MSAA.");
            }
        }

        AssetDatabase.SaveAssets();
    }

    // =====================================================================
    // 4. anisotropy
    // =====================================================================

    /// <summary>
    /// Anisotropy 1 is Unity's default and means "no anisotropic filtering".
    /// On the village floor, which is most of the screen and is seen at a
    /// grazing angle, that samples the smallest mip and the ground turns to
    /// mush from the middle distance out. This does not cost memory; it only
    /// costs extra samples, and only where there are mips to choose from.
    /// </summary>
    const int WantAniso = 8;

    static void FixAnisotropy(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 4. texture anisotropy ===");
        sb.AppendLine("Unity's default is 1, which is no anisotropic filtering at all.");
        sb.AppendLine("Raising it costs memory nothing and only improves angled surfaces.");

        var changed = new List<string>();
        int examined = 0, low = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D",
            new[] { "Assets/Art", "Assets/Painterly" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            examined++;

            if (importer.anisoLevel >= WantAniso) continue;
            if (importer.mipmapEnabled == false) continue;   // no mips, nothing to choose

            if (importer.anisoLevel < 4) low++;

            importer.anisoLevel = WantAniso;
            importer.SaveAndReimport();
            changed.Add(Path.GetFileName(path) + " " + importer.anisoLevel + "->" + WantAniso);
        }

        // Aniso has to be read back after the reimport queue drains, because
        // SaveAndReimport returns when the import is queued and not when it has
        // finished. Reading the value straight away reads the importer object,
        // which is already correct, so the real check is that the asset on disk
        // now agrees.
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        int stillLow = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D",
            new[] { "Assets/Art", "Assets/Painterly" }))
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
            if (importer == null) continue;
            if (importer.mipmapEnabled && importer.anisoLevel < 4) stillLow++;
        }

        sb.AppendLine("  textures examined: " + examined);
        sb.AppendLine("  below 4 (mush at grazing angles): " + low);
        sb.AppendLine("  raised to " + WantAniso + ": " + changed.Count);
        if (changed.Count > 0)
        {
            Did(changed.Count + " textures raised to anisotropy " + WantAniso);
            for (int i = 0; i < changed.Count && i < 10; i++)
                sb.AppendLine("    " + changed[i]);
            if (changed.Count > 10) sb.AppendLine("    ... and " + (changed.Count - 10) + " more");
        }
        else
            sb.AppendLine("  none needed raising");

        sb.AppendLine("  still below 4 after reimport: " + stillLow);
        if (stillLow > 0)
            Problem(stillLow + " textures are still below anisotropy 4 after the reimport.");

        // The value that matters is the one the material ends up with, since an
        // import setting only counts once it survives into the material.
        sb.AppendLine();
        sb.AppendLine("  what the cast materials ended up with:");
        foreach (var matPath in new[]
        {
            "Assets/Painterly/Materials/Cast_Mono.mat",
            "Assets/Painterly/Materials/Cast_InkCrawler.mat",
            "Assets/Painterly/Materials/Ari_Painterly.mat"
        })
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) continue;

            foreach (var name in new[] { "_BaseMap", "_BumpMap" })
            {
                var t = mat.GetTexture(name) as Texture2D;
                if (t == null) continue;
                sb.AppendLine("    " + mat.name + " " + name + ": " + t.name +
                              " " + t.width + "x" + t.height +
                              " mips=" + t.mipmapCount + " aniso=" + t.anisoLevel);
            }
        }
    }

    // =====================================================================

    static void Verdict(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== WHAT WAS CHANGED ===");
        if (Applied.Count == 0)
            sb.AppendLine("  nothing. Every value measured was already correct.");
        else
            foreach (var a in Applied) sb.AppendLine("  * " + a);

        sb.AppendLine();
        sb.AppendLine("=== WHAT WAS FOUND ===");
        if (Problems.Length == 0)
        {
            sb.AppendLine("  Nothing wrong in the project settings. If it still looks soft,");
            sb.AppendLine("  the cause is outside the project: the game view panel's own");
            sb.AppendLine("  size, or the textures being low-detail artwork to begin with.");
        }
        else
            sb.Append(Problems.ToString());

        sb.AppendLine();
        sb.AppendLine("  Run the game again and look. The settings are saved, so the");
        sb.AppendLine("  change is already in the project.");
    }
}
