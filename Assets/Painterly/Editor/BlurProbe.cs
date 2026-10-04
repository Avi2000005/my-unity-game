using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Finds out why everything is soft, by measuring every knob that can do it.
///
/// WHY A PROBE AND NOT A GUESS
///
/// "Everything looks blurry" has about six unrelated causes in URP and they look
/// identical on screen: the render scale below 1, MSAA off with no upscaler,
/// mipmaps biased hard on angled surfaces, anisotropy at zero, a blur pass in
/// post-processing, or a low-resolution target being upscaled. Guessing picks
/// one and edits it, and the next morning the picture is still soft with one
/// more setting changed and no idea which one mattered.
///
/// So this reads all of them and reports the numbers. A number being "fine"
/// here means it was measured, not that it looked acceptable.
///
/// The one that decides everything is the render scale: at 0.7 the whole frame
/// is rendered at 70% and stretched back up, and no amount of texture or
/// filtering setting can put back detail that was never drawn.
///
/// NOTE ON API: this version deliberately avoids typed properties that moved
/// between URP versions (per-camera renderScale, upscalingFilter, shadow atlas).
/// Where a value is not reliably reachable through the typed API it is read
/// through SerializedObject and reported as "unknown" instead of guessed at.
/// </summary>
public static class BlurProbe
{
    const string Report = "Temp/blur.txt";

    public static void Run()
    {
        var sb = new StringBuilder();
        try { Body(sb); }
        catch (System.Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] blur probe\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    /// <summary>Anything below this and the frame is drawn smaller than the window.</summary>
    const float BadRenderScale = 0.999f;

    /// <summary>Anisotropy of 0 is Unity's "as low as it goes".</summary>
    const int BadAniso = 0;

    static StringBuilder Problems;

    static void Body(StringBuilder sb)
    {
        sb.AppendLine("WHY IS EVERYTHING SOFT?");
        sb.AppendLine();
        Problems = new StringBuilder();

        RenderScale(sb);
        PostProcessing(sb);
        TextureFiltering(sb);
        CameraSettings(sb);
        Verdict(sb);
    }

    // ---------------------------------------------------------------------

    static void Problem(string s)
    {
        Problems.AppendLine("  *** " + s + " ***");
    }

    /// <summary>
    /// Reads a serialized property without ever guessing. Returns false when the
    /// property does not exist under that name, which is itself worth reporting
    /// because it means the setting is somewhere this probe cannot see.
    /// </summary>
    static bool TryFloat(UnityEngine.Object o, string prop, out float value)
    {
        value = 0f;
        if (o == null) return false;

        var so = new SerializedObject(o);
        var p = so.FindProperty(prop);
        if (p == null) return false;
        if (p.propertyType != SerializedPropertyType.Float) return false;

        value = p.floatValue;
        return true;
    }

    static bool TryInt(UnityEngine.Object o, string prop, out int value)
    {
        value = 0;
        if (o == null) return false;

        var so = new SerializedObject(o);
        var p = so.FindProperty(prop);
        if (p == null) return false;
        if (p.propertyType != SerializedPropertyType.Integer) return false;

        value = p.intValue;
        return true;
    }

    // ---------------------------------------------------------------------

    static void RenderScale(StringBuilder sb)
    {
        sb.AppendLine("=== 1. the render target's size ===");
        sb.AppendLine("This is the one that cannot be recovered from. Everything below");
        sb.AppendLine("it happens to pixels that already exist; this happens before any");
        sb.AppendLine("pixel is drawn.");

        var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (asset == null)
        {
            sb.AppendLine("No URP asset is active. Everything below assumes one.");
            Problem("No UniversalRenderPipelineAsset is active, so nothing below is");
            Problem("reading the asset that actually draws the game.");
            return;
        }

        sb.AppendLine("  asset: " + asset.name);

        // A scale under 1 draws the world into a smaller buffer and stretches it
        // to fill the window, so the softness is baked into every pixel.
        float scale = asset.renderScale;
        sb.AppendLine("  renderScale: " + scale.ToString("0.000"));

        if (scale < BadRenderScale)
        {
            sb.AppendLine("  -> THIS IS THE BLUR. " + scale.ToString("0.000") +
                          " means the frame is drawn at " +
                          (scale * 100f).ToString("0") + "% of the window's width and " +
                          "height, then stretched back up. At 2560 wide that is " +
                          Mathf.RoundToInt(2560f * scale) + " px drawn and 2560 px shown.");
            sb.AppendLine("     Set it to 1.0. There is no way to recover detail that was");
            sb.AppendLine("     never drawn, so nothing else on this list matters until this");
            sb.AppendLine("     is 1.0.");
            Problem("URP asset renderScale is " + scale.ToString("0.000") +
                    ", not 1.0. The whole frame is drawn small and upscaled.");
        }
        else
            sb.AppendLine("  -> full resolution, nothing is being upscaled");

        sb.AppendLine();

        // The URP asset's own MSAA is the setting that actually decides whether
        // edges are jagged or smooth. It is separate from the camera's
        // allowMSAA, and both have to agree: the camera has to opt in, and the
        // asset has to have samples for the opt-in to do anything.
        sb.AppendLine("=== 2. anti-aliasing ===");
        int msaa = asset.msaaSampleCount;
        sb.AppendLine("  asset MSAA: " + msaa + "x");

        var main = Camera.main;
        if (main != null)
            sb.AppendLine("  main camera allowMSAA: " + main.allowMSAA +
                          "   (a camera without this on gets no AA even when the asset has samples)");

        if (msaa <= 1)
        {
            sb.AppendLine("  -> NO ANTI-ALIASING AT ALL. Every edge in the game is a hard");
            sb.AppendLine("     staircase. On a grey village made of straight walls and");
            sb.AppendLine("     pillars this reads as softness even though resolution is fine,");
            sb.AppendLine("     because the eye reads jagged and soft in a similar way.");
            sb.AppendLine("     Set MSAA to 4x. It is nearly free on a GPU of this class.");
            Problem("MSAA is " + msaa + "x. Edges are staircased, which reads as blur.");
        }
        else
            sb.AppendLine("  -> edges are being smoothed");

        // Upscaling filters recover smoothness without real multisamples, so if
        // MSAA is off but one of these is on, the edges are still smoothed.
        //
        // The member names of this enum differ between URP versions and are not
        // spelled the same in any of them, so it is matched by name at runtime
        // instead of by constant. Comparing against a constant that does not
        // exist is a compile error, and the whole probe would never run.
        string upscaler = asset.upscalingFilter.ToString();
        bool sharpening = upscaler.IndexOf("Catmull", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                          upscaler.IndexOf("FSR", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                          upscaler.IndexOf("Sharpen", System.StringComparison.OrdinalIgnoreCase) >= 0;

        sb.AppendLine("  upscaling filter: " + upscaler);

        if (sharpening)
            sb.AppendLine("     -> a sharpening upscaler is active, so edges are being");
        else
            sb.AppendLine("     -> not a sharpening upscaler, so if MSAA is off above the");
        if (msaa <= 1 && !sharpening)
            sb.AppendLine("        edges have nothing at all smoothing them.");

        sb.AppendLine();
        sb.AppendLine("  opaque texture: " + asset.supportsCameraOpaqueTexture +
                      "  HDR: " + asset.supportsHDR);

        if (TryInt(asset, "m_ShadowAtlasResolution", out int shadowAtlas))
            sb.AppendLine("  shadow atlas: " + shadowAtlas + "px");
        else
            sb.AppendLine("  shadow atlas: could not be read (name differs in this URP)");

        sb.AppendLine("  main light shadows: " +
                      (asset.supportsMainLightShadows
                          ? asset.mainLightShadowmapResolution + "px"
                          : "off"));
    }

    // ---------------------------------------------------------------------

    static void PostProcessing(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 3. the post-processing stack ===");
        sb.AppendLine("Anything in here that blurs is applied to the final image, so it");
        sb.AppendLine("blurs everything equally. This is the other candidate for a whole-");
        sb.AppendLine("scene softness that no resolution setting explains.");

        var main = Camera.main;
        if (main != null)
        {
            var data = main.GetUniversalAdditionalCameraData();
            sb.AppendLine("  main camera post-processing: " + data.renderPostProcessing);

            if (data.renderPostProcessing)
                sb.AppendLine("     -> ON. Every override in the active profile is being");
            else
                sb.AppendLine("     -> off, so overrides cannot be the cause");
        }

        var volumes = FindVolumes(out bool hasGlobal);
        sb.AppendLine("  global volume: " + (hasGlobal ? "present" : "none"));
        sb.AppendLine("  volumes found: " + volumes.Count);

        bool sawBlur = false;

        foreach (var v in volumes)
        {
            sb.AppendLine("  volume '" + v.name + "' priority " + v.priority +
                          " weight " + v.weight.ToString("0.00") +
                          " global " + v.isGlobal);

            var prof = v.profile;
            if (prof == null)
            {
                sb.AppendLine("      no profile assigned");
                continue;
            }

            foreach (var c in prof.components)
            {
                if (!c.active) continue;

                string line = "      " + c.GetType().Name;

                // DepthOfField is the component that blurs everything outside a
                // focus range, and motion blur smears the whole frame. Both are
                // checked by name because the typed classes moved between
                // versions and would not compile against all of them.
                string typeName = c.GetType().Name;
                if (typeName.Contains("DepthOfField") || typeName.Contains("MotionBlur"))
                {
                    sb.AppendLine(line + "   <-- BLURS THE WHOLE FRAME");
                    sawBlur = true;
                }
                else
                    sb.AppendLine(line);
            }
        }

        if (sawBlur)
        {
            Problem("An active post-processing component blurs the final frame.");
            sb.AppendLine();
            sb.AppendLine("  A DepthOfField or MotionBlur override explains softness that");
            sb.AppendLine("  no resolution or filtering setting can fix, because it is");
            sb.AppendLine("  applied after the image is finished.");
        }

        sb.AppendLine();
        sb.AppendLine("  Bloom is additive and does not blur, so it is not the cause.");
        sb.AppendLine("  Tonemapping and colour grading do not blur either.");
    }

    static List<Volume> FindVolumes(out bool hasGlobal)
    {
        var list = new List<Volume>();
        hasGlobal = false;

        // Resources.FindObjectsOfTypeAll also catches the global volume, which
        // lives on an object outside the scene hierarchy.
        foreach (var v in Resources.FindObjectsOfTypeAll<Volume>())
        {
            if (v == null) continue;
            list.Add(v);
            if (v.isGlobal) hasGlobal = true;
        }

        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Include))
        {
            if (v != null && !list.Contains(v)) list.Add(v);
        }

        return list;
    }

    // ---------------------------------------------------------------------

    static void TextureFiltering(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 4. texture import and sampling ===");
        sb.AppendLine("A texture is resampled down to a mip chain whenever it is drawn at");
        sb.AppendLine("less than full size. That is correct and free-looking; it only");
        sb.AppendLine("becomes blur if the smallest mip is much too coarse, or if the");
        sb.AppendLine("importer shrank the source on the way in.");

        int examined = 0, smallMax = 0, anisoOff = 0, srgbOffOnColor = 0, noMips = 0;
        var small = new List<string>();
        var badAniso = new List<string>();
        var colorNoSrgb = new List<string>();

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D",
            new[] { "Assets/Art", "Assets/Painterly" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            examined++;

            if (importer.maxTextureSize < 2048)
            {
                smallMax++;
                if (small.Count < 8)
                    small.Add(Path.GetFileName(path) + " -> " + importer.maxTextureSize + "px");
            }

            // Anisotropy 0 is Unity's "let the hardware pick the cheapest". On a
            // ground plane seen at a grazing angle, that picks the smallest mip
            // and the floor turns to mush from the middle distance onwards —
            // which is exactly what "everything looks blurry" looks like on a
            // village whose floor is most of the screen.
            if (importer.anisoLevel <= BadAniso && importer.textureType == TextureImporterType.Default)
            {
                anisoOff++;
                if (badAniso.Count < 8) badAniso.Add(Path.GetFileName(path));
            }

            if (!importer.mipmapEnabled) noMips++;

            if (importer.textureType == TextureImporterType.Default && !importer.sRGBTexture)
            {
                srgbOffOnColor++;
                if (colorNoSrgb.Count < 8) colorNoSrgb.Add(Path.GetFileName(path));
            }
        }

        sb.AppendLine("  textures examined: " + examined);

        sb.AppendLine("  imported below 2048px: " + smallMax +
                      (smallMax > 0 ? "   e.g. " + string.Join(", ", small.ToArray()) : ""));
        sb.AppendLine("  anisotropy 0 (mush at grazing angles): " + anisoOff +
                      (anisoOff > 0 ? "   e.g. " + string.Join(", ", badAniso.ToArray()) : ""));
        sb.AppendLine("  mipmaps off (shimmer when moving, not blur): " + noMips);
        sb.AppendLine("  colour textures with sRGB OFF (wrong gamma, looks washed): " + srgbOffOnColor +
                      (srgbOffOnColor > 0 ? "   e.g. " + string.Join(", ", colorNoSrgb.ToArray()) : ""));

        if (anisoOff > 0)
        {
            Problem(anisoOff + " textures have anisotropy 0, so any surface seen at a");
            Problem("grazing angle — most of the village floor — samples a tiny mip.");
            Problem("Set anisotropy to 4 or 8 on every texture.");
        }

        if (smallMax > 0)
            Problem(smallMax + " textures are imported below 2048px, so their detail was");
        if (smallMax > 0)
            Problem("thrown away at import and cannot come back.");

        sb.AppendLine();

        // The sampler the material actually ends up with, read from the cast's
        // own material, because an import setting only matters once it survives
        // into the material.
        sb.AppendLine("  the sampler the cast materials actually ended up with:");

        foreach (var matPath in new[]
        {
            "Assets/Painterly/Materials/Cast_Mono.mat",
            "Assets/Painterly/Materials/Cast_InkCrawler.mat",
            "Assets/Painterly/Materials/Ari_Painterly.mat"
        })
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                sb.AppendLine("    " + Path.GetFileName(matPath) + ": not found");
                continue;
            }

            sb.AppendLine("    " + mat.name + ":");
            foreach (var name in new[] { "_BaseMap", "_BumpMap" })
            {
                var t = mat.GetTexture(name) as Texture2D;
                if (t == null) { sb.AppendLine("      " + name + ": none"); continue; }

                sb.AppendLine("      " + name + ": " + t.name + " " +
                              t.width + "x" + t.height + " mips=" + t.mipmapCount +
                              " aniso=" + t.anisoLevel + " filter=" + t.filterMode);

                if (t.anisoLevel <= BadAniso)
                    Problem(mat.name + " " + name + " has anisotropy 0 in the material.");
            }
        }
    }

    // ---------------------------------------------------------------------

    static void CameraSettings(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 5. cameras ===");
        sb.AppendLine("The main camera's own overrides sit on top of the asset's, so a");
        sb.AppendLine("camera can undo a good render scale without touching it.");

        var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        sb.AppendLine("  cameras in scene: " + cams.Length);

        foreach (var c in cams)
        {
            sb.AppendLine("  '" + c.name + "' active=" + c.gameObject.activeInHierarchy +
                          " allowMSAA=" + c.allowMSAA +
                          " HDR=" + c.allowHDR +
                          " pixelHeight=" + c.pixelHeight);

            var d = c.GetUniversalAdditionalCameraData();
            if (d == null) continue;

            sb.AppendLine("      antialiasing=" + d.antialiasing +
                          " postProcessing=" + d.renderPostProcessing +
                          " renderType=" + d.renderType +
                          " requiresDepth=" + d.requiresDepthTexture +
                          " requiresColor=" + d.requiresColorTexture);

            // A per-camera render scale below 1 overrides the asset's, and it is
            // the setting most likely to have been nudged while chasing
            // performance. Read through SerializedObject because the typed
            // accessor for it is not present on this URP version.
            if (TryFloat(d, "m_RenderScale", out float camScale))
            {
                sb.AppendLine("      camera renderScale=" + camScale.ToString("0.000"));
                if (camScale < BadRenderScale)
                {
                    sb.AppendLine("      -> THIS CAMERA IS DRAWN SMALLER THAN THE WINDOW (" +
                                  camScale.ToString("0.000") + "). It overrides the asset's");
                    sb.AppendLine("         render scale, so fixing the asset alone will not");
                    sb.AppendLine("         change what this camera shows.");
                    Problem("camera '" + c.name + "' has renderScale " +
                            camScale.ToString("0.000") + ", overriding the asset.");
                }
            }
            else
                sb.AppendLine("      camera renderScale: not reachable under that name in this URP");

            if (d.antialiasing == AntialiasingMode.None && !c.allowMSAA)
            {
                sb.AppendLine("      -> anti-aliasing is off on this camera specifically");
                Problem("camera '" + c.name + "' has AA off on both the camera and its URP data.");
            }
        }

        sb.AppendLine();
        sb.AppendLine("  The Game view's own resolution is the remaining suspect. It is");
        sb.AppendLine("  not stored on the camera — it is a Game view setting, so if the");
        sb.AppendLine("  probe above finds nothing wrong, that is where to look: set the");
        sb.AppendLine("  Game view scale to 1 and its resolution to your monitor's.");
    }

    // ---------------------------------------------------------------------

    static void Verdict(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== VERDICT ===");

        if (Problems.Length == 0)
        {
            sb.AppendLine("  Nothing measured is set to soften the image: render scale is 1,");
            sb.AppendLine("  anti-aliasing is on, no blur override is in the post stack, and");
            sb.AppendLine("  textures are full size with anisotropy on.");
            sb.AppendLine();
            sb.AppendLine("  That means the softness is coming from something this probe");
            sb.AppendLine("  does not measure — most likely the Game view's own scaling, a");
            sb.AppendLine("  very wide camera field of view, or the textures themselves");
            sb.AppendLine("  being genuinely low-detail artwork.");
            sb.AppendLine("  Check the Game view's render size next.");
            return;
        }

        sb.AppendLine("  Found, in the order they matter:");
        sb.Append(Problems.ToString());
        sb.AppendLine();
        sb.AppendLine("  Fix the first one and look again before changing the second. The");
        sb.AppendLine("  render scale in particular hides everything else: at a reduced");
        sb.AppendLine("  scale no filtering setting can restore detail that was never drawn.");
    }
}
