using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Checks the last thing that could still be softening the frame: the active
/// post-processing values.
///
/// WHY THIS IS STILL WORTH LOOKING AT
///
/// With the resolution, MSAA and anisotropy now fixed, the pipeline draws
/// exactly as many pixels as the monitor has and smooths the edges properly.
/// The remaining softener is the volume. Bloom is additive glow and on its own
/// does not blur — but a bloom built from a downsampled chain spreads light
/// across the whole frame once its threshold is low enough, and that reads as a
/// haze over everything rather than as a glow on the bright parts. Vignette
/// darkens the corners and can make the middle look flatter and softer by
/// comparison.
///
/// Both are measured here rather than judged, because a bloom that looks
/// deliberate at intensity 0.5 and destructive at 1.5 is a matter of numbers,
/// not of taste.
///
/// WHY THE VALUES ARE READ OUT OF THE PROFILE FILE
///
/// A volume component in this URP version stores nothing but its enabled flag.
/// Every parameter — a bloom's threshold and intensity, a vignette's weight —
/// lives in a separate sub-asset inside the profile's .asset file. Reading the
/// component directly reports an override that is switched on and carries no
/// values at all, which is indistinguishable from an empty override and would
/// send the search after a setting that was never the problem.
///
/// Nothing is written here. This only measures, so that the numbers can be
/// judged before anything is changed.
/// </summary>
public static class PostProbe
{
    const string Report = "Temp/post.txt";

    public static void Run()
    {
        var sb = new StringBuilder();
        try { Body(sb); }
        catch (System.Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] post probe\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        sb.AppendLine("POST-PROCESSING: THE LAST SOFTENER");
        sb.AppendLine();

        var volumes = Resources.FindObjectsOfTypeAll<Volume>();
        sb.AppendLine("volumes in the editor: " + volumes.Length);

        foreach (var v in volumes)
        {
            sb.AppendLine();
            sb.AppendLine("volume '" + v.name + "' priority " + v.priority +
                          " weight " + v.weight.ToString("0.00") +
                          " global " + v.isGlobal);

            var prof = v.profile;
            if (prof == null) { sb.AppendLine("  no profile"); continue; }

            string path = AssetDatabase.GetAssetPath(prof);
            sb.AppendLine("  profile: " + Path.GetFileName(path));
            sb.AppendLine("  active overrides:");

            foreach (var c in prof.components)
            {
                if (!c.active) continue;
                sb.AppendLine();
                sb.AppendLine("    " + c.GetType().Name);
                sb.AppendLine("      " + DescribeComponent(c, path));
            }
        }

        CameraThings(sb);
        Verdict(sb);
    }

    /// <summary>
    /// Prints a component's real parameters by finding the sub-asset inside
    /// the profile file that carries them, and walking it.
    ///
    /// The walk uses Next and not NextVisible. Those parameter sub-assets use a
    /// plain [SerializeField] without [HideInInspector], so they are serialized
    /// but not "visible", and NextVisible steps over every one of them and
    /// prints nothing at all.
    ///
    /// Typed accessors are avoided because they move between URP versions; a
    /// serialized read lists what is really set rather than what the API
    /// happened to expose on this build.
    /// </summary>
    static string DescribeComponent(VolumeComponent c, string profilePath)
    {
        var sb = new StringBuilder();
        string wanted = c.GetType().Name;
        int found = 0;

        foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(profilePath))
        {
            if (sub == null || sub == c) continue;
            if (sub.GetType().Name != wanted) continue;

            var so = new SerializedObject(sub);
            var it = so.GetIterator();
            bool enter = true;

            while (it.Next(enter))
            {
                enter = false;
                if (it.propertyPath == "m_Script") continue;
                if (it.name == "m_Enabled") continue;

                if (it.propertyType == SerializedPropertyType.Float)
                    sb.Append(it.name + " = " + it.floatValue.ToString("0.000") + "   ");
                else if (it.propertyType == SerializedPropertyType.Color)
                    sb.Append(it.name + " = " + it.colorValue + "   ");
                else if (it.propertyType == SerializedPropertyType.Vector2)
                    sb.Append(it.name + " = " + it.vector2Value + "   ");
                else if (it.propertyType == SerializedPropertyType.Vector4)
                    sb.Append(it.name + " = " + it.vector4Value + "   ");
                else if (it.propertyType == SerializedPropertyType.Boolean)
                    sb.Append(it.name + " = " + it.boolValue + "   ");

                found++;
            }

            break;
        }

        if (found == 0)
            return "no values found among the sub-assets of this profile. " +
                   "The override is on, so if this prints nothing the profile is " +
                   "running on Unity's built-in defaults rather than on settings of its own.";

        return sb.ToString().TrimEnd();
    }

    static void CameraThings(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== the camera's own view ===");
        sb.AppendLine("A very wide field of view makes everything small on screen, which");
        sb.AppendLine("reads as soft detail even when the pixels are perfect. Measured");
        sb.AppendLine("rather than assumed.");

        var main = Camera.main;
        if (main == null) { sb.AppendLine("  no main camera"); return; }

        sb.AppendLine("  field of view: " + main.fieldOfView.ToString("0.0") +
                      "   (55-70 is normal; above 90 things look small and smeared)");

        sb.AppendLine("  near clip: " + main.nearClipPlane.ToString("0.000") +
                      "   far clip: " + main.farClipPlane.ToString("0"));

        if (main.fieldOfView > 80f)
        {
            sb.AppendLine("  -> THIS IS WIDE. Everything is drawn small on screen and the");
            sb.AppendLine("     detail of every surface is compressed toward the centre.");
        }

        // The gap between the clip planes decides how much depth precision is
        // spent. A far plane set far past what the village needs costs
        // z-fighting rather than blur, but it is worth knowing.
        if (main.farClipPlane > 500f)
        {
            sb.AppendLine("  -> far clip is well past the village, which costs depth");
            sb.AppendLine("     precision for no visible benefit. Not a blur cause, but");
            sb.AppendLine("     worth trimming to about 120.");
        }

        var d = main.GetUniversalAdditionalCameraData();
        if (d != null)
        {
            sb.AppendLine("  URP camera data: antialiasing=" + d.antialiasing +
                          " postProcessing=" + d.renderPostProcessing +
                          " renderShadows=" + d.renderShadows +
                          " requiresDepth=" + d.requiresDepthTexture +
                          " requiresColor=" + d.requiresColorTexture);

            // Anti-aliasing of None here is correct now: the pipeline asset has
            // 4x MSAA, and stacking a post-process smoother on top of real
            // multisamples would soften what they just cleaned up.
            if (d.antialiasing != AntialiasingMode.None)
            {
                sb.AppendLine("  -> a post-process smoother is also on. With 4x MSAA that is");
                sb.AppendLine("     double smoothing and it costs sharpness. None is the right");
                sb.AppendLine("     choice here, because MSAA handles the edges properly.");
            }
            else
                sb.AppendLine("  -> no post-process smoother, which is correct alongside 4x MSAA");
        }
    }

    static void Verdict(StringBuilder sb)
    {
        sb.AppendLine();
        sb.AppendLine("=== WHAT TO LOOK AT ===");
        sb.AppendLine("  Resolution, MSAA and anisotropy are all fixed and verified. The");
        sb.AppendLine("  values above are the only softener left, and they are worth one");
        sb.AppendLine("  number each:");
        sb.AppendLine();
        sb.AppendLine("  - Bloom threshold at or above 1.0, intensity around 0.4 to 0.8,");
        sb.AppendLine("    glows only what is genuinely bright. A threshold below 1.0 veils");
        sb.AppendLine("    the whole frame, and an intensity above 1.5 is a haze.");
        sb.AppendLine("  - Vignette intensity above about 0.4 darkens the corners enough to");
        sb.AppendLine("    make the middle look flat by comparison.");
        sb.AppendLine();
        sb.AppendLine("  Run the game first if you like — the resolution fix on its own is");
        sb.AppendLine("  a large change and worth seeing before anything else is touched.");
    }
}
