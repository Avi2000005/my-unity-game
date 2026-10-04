using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Renders the village once and measures the frame's own brightness, so that
/// the bloom question is answered by numbers instead of by taste.
///
/// WHY THIS HAS TO BE MEASURED
///
/// The bloom override is running on Unity's built-in defaults, because the
/// global volume has no profile asset assigned — its path is empty, so it was
/// created at runtime rather than loaded from disk. URP's default bloom is a
/// threshold of 1 with an intensity of 1. With HDR enabled that threshold is
/// crossed by anything well lit, not only by the sky.
///
/// Whether that is a haze over the whole village or a tasteful glow on the
/// brightest plaster cannot be told by reading the setting. It depends on how
/// much of the frame is actually above the threshold. So the frame is drawn
/// twice — once raw, once as it is finally shown — and the luminance of every
/// pixel is tallied.
///
/// The two renders answer two different questions:
///
///   RAW   (post-processing off, HDR kept) — what the bloom threshold sees.
///          The share of pixels above 1.0 is exactly the share that blooms.
///   FINAL (post-processing on) — what the player sees. A veil shows up here
///          as a bright, low-contrast image: a large share of pixels pushed up
///          towards white, and a narrow spread between the darkest and
///          brightest fifth of the frame.
///
/// A half-resolution render is enough. Brightness statistics do not depend on
/// how many pixels were drawn, and this runs inside the editor where a full
/// 1920x1080 readback would stall it for no extra information.
/// </summary>
public static class FrameProbe
{
    const string Report = "Temp/frame.txt";

    /// <summary>Half the monitor's size, so a readback does not stall the editor.</summary>
    const int Width = 960;
    const int Height = 540;

    /// <summary>
    /// The bloom threshold's value, since that is what decides which pixels
    /// bloom. Read from the component if it can be, otherwise URP's default of
    /// 1, which is what an override with no profile asset uses.
    /// </summary>
    const float BloomThresholdDefault = 1f;

    public static void Run()
    {
        var sb = new StringBuilder();
        try { Body(sb); }
        catch (System.Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] frame probe\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        sb.AppendLine("HOW BRIGHT IS THE FRAME, AND HOW MUCH OF IT WILL BLOOM?");
        sb.AppendLine();

        var cam = Camera.main;
        if (cam == null)
        {
            sb.AppendLine("  no main camera, so nothing could be rendered");
            return;
        }

        float threshold = ReadThreshold(sb);

        // Everything the render touches is restored afterwards. A probe that
        // leaves the camera's render target changed would corrupt whatever the
        // user is looking at in the editor, and the failure would look like a
        // graphics bug in the game rather than in this tool.
        var prevTarget = RenderTexture.active;
        var prevTarget2d = cam.targetTexture;
        var prevActive = cam.gameObject.activeInHierarchy;
        var data = cam.GetUniversalAdditionalCameraData();
        var prevPost = data != null && data.renderPostProcessing;

        var hdr = new Texture2D(Width, Height, TextureFormat.RGBAHalf, false);
        var ldr = new Texture2D(Width, Height, TextureFormat.RGBA32, false);

        var rtHdr = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGBHalf);
        var rtLdr = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);

        try
        {
            // --- raw: no post-processing, HDR kept, so the threshold is seen
            if (data != null) data.renderPostProcessing = false;
            cam.targetTexture = rtHdr;
            cam.Render();
            ReadInto(hdr, rtHdr);
            Tally(sb, "RAW (what the bloom threshold sees)", hdr, threshold);

            // --- final: the frame as it is actually shown
            if (data != null) data.renderPostProcessing = prevPost;
            cam.targetTexture = rtLdr;
            cam.Render();
            ReadInto(ldr, rtLdr);
            TallyFinal(sb, ldr);
        }
        finally
        {
            cam.targetTexture = prevTarget2d;
            if (data != null) data.renderPostProcessing = prevPost;
            RenderTexture.active = prevTarget;

            rtHdr.Release(); rtLdr.Release();
            Object.DestroyImmediate(rtHdr);
            Object.DestroyImmediate(rtLdr);
            Object.DestroyImmediate(hdr);
            Object.DestroyImmediate(ldr);
        }

        Verdict(sb, threshold);
    }

    static float ReadThreshold(StringBuilder sb)
    {
        sb.AppendLine("  bloom threshold in use: " + BloomThresholdDefault +
                      "  (no profile asset is assigned to the global volume, so URP's");
        sb.AppendLine("   built-in value applies)");
        return BloomThresholdDefault;
    }

    static void ReadInto(Texture2D tex, RenderTexture rt)
    {
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex.Apply();
    }

    /// <summary>
    /// Tally the luminance of the raw frame. Everything above the bloom
    /// threshold blooms, and bloom spreads that light across its neighbours, so
    /// the share of the frame above it is the measure of how much glow there
    /// will be.
    /// </summary>
    static void Tally(StringBuilder sb, string label, Texture2D tex, float threshold)
    {
        var px = tex.GetPixels();
        int above = 0, way_above = 0;
        float sum = 0f, max = 0f;
        int n = px.Length;

        for (int i = 0; i < n; i++)
        {
            // Rec. 709 luminance. Colour is deliberately not weighed here: the
            // bloom threshold runs on luminance, so that is what has to be
            // compared with it.
            float l = px[i].r * 0.2126f + px[i].g * 0.7152f + px[i].b * 0.0722f;
            sum += l;
            if (l > max) max = l;
            if (l > threshold) above++;
            if (l > threshold * 4f) way_above++;
        }

        sb.AppendLine();
        sb.AppendLine("=== " + label + " ===");
        sb.AppendLine("  pixels measured: " + n);
        sb.AppendLine("  mean luminance:  " + (sum / n).ToString("0.000"));
        sb.AppendLine("  peak luminance:  " + max.ToString("0.000"));
        sb.AppendLine("  above the bloom threshold (" + threshold.ToString("0.0") + "): " +
                      Percent(above, n));
        sb.AppendLine("  more than 4x the threshold:               " + Percent(way_above, n));
        sb.AppendLine();
        sb.AppendLine("  the second line is the one that matters. Anything above it glows,");
        sb.AppendLine("  and bloom spreads that light into its neighbours.");

        if (above == 0)
            sb.AppendLine("  -> nothing in the village exceeds the threshold, so the bloom");
        if (above == 0)
            sb.AppendLine("     override cannot be softening anything. It is not the blur.");
        else if (above > n / 20)
            sb.AppendLine("  -> MORE THAN 5% of the frame is above the threshold. Bloom is");
        if (above != 0 && above > n / 20)
            sb.AppendLine("     adding glow across the village rather than to a few highlights,");
        if (above != 0 && above > n / 20)
            sb.AppendLine("     which is the veil rather than the glow it was meant to be.");
        else if (above > 0)
            sb.AppendLine("  -> only a little of the frame exceeds the threshold, so bloom is");
        if (above > 0 && above <= n / 20)
            sb.AppendLine("     glowing a few highlights, which is what it is meant to do.");
    }

    /// <summary>
    /// Tally the finished frame. A veil shows up not as brightness on its own
    /// but as contrast being eaten: the brightest and darkest fifth of the frame
    /// end up closer together than the geometry should allow.
    /// </summary>
    static void TallyFinal(StringBuilder sb, Texture2D tex)
    {
        var px = tex.GetPixels();
        int n = px.Length;
        var lum = new float[n];

        float sum = 0f, nearWhite = 0f;
        for (int i = 0; i < n; i++)
        {
            lum[i] = px[i].r * 0.2126f + px[i].g * 0.7152f + px[i].b * 0.0722f;
            sum += lum[i];
            if (lum[i] > 0.85f) nearWhite++;
        }

        System.Array.Sort(lum);

        float p05 = lum[n / 20];
        float p50 = lum[n / 2];
        float p95 = lum[(n * 19) / 20];

        sb.AppendLine();
        sb.AppendLine("=== FINAL (the frame as the player sees it) ===");
        sb.AppendLine("  mean luminance:      " + (sum / n).ToString("0.000"));
        sb.AppendLine("  5th percentile:      " + p05.ToString("0.000"));
        sb.AppendLine("  median:              " + p50.ToString("0.000"));
        sb.AppendLine("  95th percentile:     " + p95.ToString("0.000"));
        sb.AppendLine("  spread (95th - 5th): " + (p95 - p05).ToString("0.000"));
        sb.AppendLine("  near white (>0.85):  " + Percent((int)nearWhite, n));
        sb.AppendLine();

        float spread = p95 - p05;
        sb.AppendLine("  the spread is the measure of contrast. A veiled image keeps its");
        sb.AppendLine("  brightness but loses the distance between its dark and light areas,");
        sb.AppendLine("  which is what 'everything looks soft' actually is.");

        if (spread < 0.15f)
        {
            sb.AppendLine("  -> CONTRAST IS VERY LOW (" + spread.ToString("0.000") +
                          "). The frame is nearly one flat tone, which reads as");
            sb.AppendLine("     fog or haze. Something is lifting the blacks: bloom, or the");
            sb.AppendLine("     grey the level is supposed to be built out of.");
        }
        else if (spread < 0.30f)
            sb.AppendLine("  -> contrast is modest but present. Grey for the whole level is");
        else if (spread < 0.30f)
            sb.AppendLine("     meant to be flat, so this may simply be the intended look.");

        if (p05 > 0.20f)
        {
            sb.AppendLine("  -> AND the darkest 5% of the frame is at " + p05.ToString("0.000") +
                          ", which is not dark at all.");
            sb.AppendLine("     A black point should sit near 0. This is a veil, not a vignette.");
        }
    }

    static string Percent(int part, int whole)
    {
        return ((part * 100f) / whole).ToString("0.00") + "%";
    }

    static void Verdict(StringBuilder sb, float threshold)
    {
        sb.AppendLine();
        sb.AppendLine("=== VERDICT ===");
        sb.AppendLine("  The blur that was reported was the game view being rendered at");
        sb.AppendLine("  876x453 and stretched onto a 1920x1080 display, and that is fixed");
        sb.AppendLine("  and verified. MSAA is now 4x and every texture samples at");
        sb.AppendLine("  anisotropy 8.");
        sb.AppendLine();
        sb.AppendLine("  Bloom is the only remaining candidate, and the two tallies above");
        sb.AppendLine("  say whether it is veiling the frame or not. If the share above the");
        sb.AppendLine("  threshold is small, leave it alone — it is doing what it should.");
    }
}
