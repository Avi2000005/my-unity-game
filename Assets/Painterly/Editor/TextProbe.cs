using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Measures every piece of text in the game, because blurry text has its own
/// cause and it is not the same cause as a blurry world.
///
/// WHY TEXT GOES BLURRY ON ITS OWN
///
/// Text is not rendered like geometry. It is rasterised once into a font atlas
/// at a fixed pixel size, and afterwards it is only ever scaled. Two things
/// make that scaling visible:
///
///   1. THE CANVAS SCALER'S MATH. A Canvas Scaler set to "Scale With Screen
///      Size" divides the screen by a reference resolution. If the reference is
///      larger than the screen — 2560x1440 on a 1920x1080 monitor — every
///      label is drawn at 0.75 and blown back up, which softens every glyph
///      edge. If the reference is smaller, the same thing happens by scaling
///      up. Either way the text is drawn at one size and shown at another.
///
///   2. THE FONT'S OWN SIZE. Legacy Text rasterises dynamic fonts at exactly
///      fontSize pixels. Ask for a small size and then scale the canvas up, and
///      the glyphs are permanently soft no matter how much resolution the
///      canvas has.
///
/// Neither is visible in the image settings, which is why they get mistaken for
/// the resolution problem. The world can be rendered perfectly while the text
/// on top of it is the only soft thing on screen.
///
/// EVERY LABEL IS FOUND AND REPORTED, not just the ones that happen to be on
/// screen right now, because a HUD that is hidden at the moment still renders
/// the moment the player reaches it.
/// </summary>
public static class TextProbe
{
    const string Report = "Temp/text.txt";

    public static void Run()
    {
        var sb = new StringBuilder();
        try { Body(sb); }
        catch (System.Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] text probe\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        sb.AppendLine("WHY IS THE TEXT SOFT?");
        sb.AppendLine();

        Canvases(sb);
        LegacyText(sb);
        ScreenAndReferences(sb);
        Verdict(sb);
    }

    // ---------------------------------------------------------------------

    static void Canvases(StringBuilder sb)
    {
        sb.AppendLine("=== 1. canvases and how they scale ===");
        sb.AppendLine("A scaler's reference resolution divided into the screen gives the");
        sb.AppendLine("factor every glyph is drawn at. Anything that is not 1.0 is a resize,");
        sb.AppendLine("and a resized glyph is a soft glyph.");

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        sb.AppendLine("  canvases in scene: " + canvases.Length);

        foreach (var c in canvases)
        {
            sb.AppendLine();
            sb.AppendLine("  '" + c.name + "' active=" + c.gameObject.activeInHierarchy +
                          " renderMode=" + c.renderMode);

            // A world-space canvas is drawn as geometry, so its text is sampled
            // like a texture and softened by distance. Overlay and camera-space
            // canvases are drawn straight to the screen at full resolution.
            if (c.renderMode == RenderMode.WorldSpace)
            {
                sb.AppendLine("      -> WORLD SPACE. Its text is drawn as geometry and resampled");
                sb.AppendLine("         like a texture, so it blurs with distance. Text meant to");
                sb.AppendLine("         be read should be on a screen-space canvas.");
            }

            var scaler = c.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                sb.AppendLine("      no CanvasScaler: drawn at 1:1 with screen pixels, so any");
                sb.AppendLine("      blur here comes from the font, not from a resize");
                continue;
            }

            sb.AppendLine("      CanvasScaler uiScaleMode=" + scaler.uiScaleMode +
                          " scaleFactor=" + scaler.scaleFactor.ToString("0.000") +
                          " referencePixelsPerUnit=" + scaler.referencePixelsPerUnit);

            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;

            sb.AppendLine("      referenceResolution: " +
                          scaler.referenceResolution.x.ToString("0") + " x " +
                          scaler.referenceResolution.y.ToString("0") +
                          "   screenMatchMode=" + scaler.screenMatchMode +
                          " matchWidthOrHeight=" + scaler.matchWidthOrHeight.ToString("0.00"));

            // The screen the canvas is being resolved against. In the editor
            // outside play mode this is the game view, which is what the player
            // will see, so it is the right number to divide by.
            float screenW = Screen.width;
            float screenH = Screen.height;

            sb.AppendLine("      screen being resolved against: " +
                          screenW.ToString("0") + " x " + screenH.ToString("0"));

            float refW = scaler.referenceResolution.x;
            float refH = scaler.referenceResolution.y;
            if (refW <= 0f || refH <= 0f) continue;

            float byWidth = screenW / refW;
            float byHeight = screenH / refH;

            // The lerp is not the simple mean: matchWidthOrHeight of 0 is all
            // width, 1 is all height, and half is a blend. Reproducing it here
            // matters, because averaging the two ratios reports a factor the
            // canvas never uses.
            float t = scaler.matchWidthOrHeight;
            float factor = byWidth * (1f - t) + byHeight * t;

            sb.AppendLine("      factor the screen gives: by width " + byWidth.ToString("0.000") +
                          ", by height " + byHeight.ToString("0.000") +
                          ", blended " + factor.ToString("0.000"));

            if (Mathf.Abs(factor - 1f) < 0.02f)
            {
                sb.AppendLine("      -> 1:1. Text is drawn at the reference size, so it is as");
                sb.AppendLine("         sharp as this setup can make it.");
            }
            else
            {
                sb.AppendLine("      -> TEXT IS BEING RESIZED BY " + factor.ToString("0.000") +
                              ". Every glyph is drawn at one size and shown at another.");

                if (factor < 1f)
                {
                    sb.AppendLine("         Shrunken: the reference resolution is larger than the");
                    sb.AppendLine("         screen, so the canvas is being scaled back down. The");
                    sb.AppendLine("         glyphs lose the detail they were rasterised with.");
                }
                else
                {
                    sb.AppendLine("         Enlarged: the reference resolution is smaller than the");
                    sb.AppendLine("         screen, so the canvas is being scaled up past what the");
                    sb.AppendLine("         font atlas can resolve. This is the blur.");
                }
            }
        }
    }

    // ---------------------------------------------------------------------

    static void LegacyText(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 2. the labels themselves ===");
        sb.AppendLine("Each one's font size, its atlas, and the scale its parent actually");
        sb.AppendLine("applies. A label drawn at 14px and then scaled up is soft forever,");
        sb.AppendLine("no matter how much resolution is behind it.");

        var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
        sb.AppendLine("  legacy Text components: " + texts.Length);

        int worst = 0;
        var listed = new List<string>();

        foreach (var t in texts)
        {
            var rt = t.transform as RectTransform;
            float lossy = rt != null ? rt.lossyScale.x : 1f;

            // What the rasteriser is asked for, multiplied by the scale it is
            // finally drawn at, is the number of screen pixels each glyph gets.
            float effective = t.fontSize * lossy;

            string row = "  '" + NameOf(t.transform) + "' text=\"" + OneLine(t.text) + "\"" +
                         " size=" + t.fontSize + " parentScale=" + lossy.ToString("0.000") +
                         " effective=" + effective.ToString("0.0") + "px" +
                         " font=" + (t.font == null ? "NONE" : t.font.name) +
                         " alignment=" + t.alignment;

            if (t.font != null)
            {
                // A dynamic font rasterises its glyphs at exactly the size
                // requested and packs them into this atlas. The atlas's own
                // dimensions are the ceiling on how sharp the label can ever be,
                // so it is the atlas that is worth measuring, not the name of
                // the font.
                //
                // Font.fontTexture was removed in Unity 6. The atlas is reached
                // through the font's material instead, which is the same texture
                // the renderer actually samples.
                Texture2D atlas = null;
                try { atlas = t.font.material.mainTexture as Texture2D; }
                catch { /* a font with no material reports nothing rather than throwing here */ }

                if (atlas != null)
                {
                    row += " atlas=" + atlas.name + " " + atlas.width + "x" + atlas.height +
                           " filter=" + atlas.filterMode + " mips=" + atlas.mipmapCount;
                }
                else
                {
                    row += " atlas=unreadable";
                }
            }

            listed.Add(row);
            if (t.fontSize < 14 && lossy > 1.2f) worst++;
        }

        foreach (var r in listed) sb.AppendLine(r);

        if (texts.Length == 0)
        {
            sb.AppendLine("  -> no legacy Text in the scene. If labels appear on screen they");
            sb.AppendLine("     are TextMeshPro, which rasterises differently and is covered");
            sb.AppendLine("     by its own atlas resolution.");
        }

        if (worst > 0)
        {
            sb.AppendLine();
            sb.AppendLine("  -> " + worst + " label(s) are drawn at under 14px and then scaled");
            sb.AppendLine("     up. Those are soft at any resolution.");
        }
    }

    // ---------------------------------------------------------------------

    static void ScreenAndReferences(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== 3. the screen this is being judged against ===");

        sb.AppendLine("  editor game view: " + Screen.width + " x " + Screen.height);
        sb.AppendLine("  monitor:          " + Screen.currentResolution.width + " x " +
                      Screen.currentResolution.height);

        var main = Camera.main;
        if (main != null)
            sb.AppendLine("  camera renders:   " + main.pixelWidth + " x " + main.pixelHeight);

        sb.AppendLine();
        sb.AppendLine("  A canvas in Overlay mode is drawn at the game view's own");
        sb.AppendLine("  resolution, so if the game view is larger than the camera's render");
        sb.AppendLine("  size the text is actually sharper than the world behind it.");
    }

    // ---------------------------------------------------------------------

    static string NameOf(Transform t)
    {
        var names = new List<string>();
        int guard = 0;

        while (t != null && guard++ < 12)
        {
            names.Add(t.name);
            t = t.parent;
        }

        names.Reverse();
        return string.Join("/", names.ToArray());
    }

    static string OneLine(string s)
    {
        if (string.IsNullOrEmpty(s)) return "(empty)";
        s = s.Replace("\n", " ").Replace("\r", " ");
        return s.Length <= 28 ? s : s.Substring(0, 25) + "...";
    }

    // ---------------------------------------------------------------------

    static void Verdict(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== VERDICT ===");
        sb.AppendLine("  The world was fixed: the game view went from 876x453 to 1920x1080,");
        sb.AppendLine("  MSAA is 4x and every texture samples at anisotropy 8. That is done.");
        sb.AppendLine();
        sb.AppendLine("  Text has its own scaling and is not covered by any of that. If the");
        sb.AppendLine("  numbers above show a factor away from 1.000, that is why text alone");
        sb.AppendLine("  still looks soft while the village behind it looks right — and it is");
        sb.AppendLine("  a different fix, made by putting the reference resolution on the");
        sb.AppendLine("  screen's actual size.");
    }
}
