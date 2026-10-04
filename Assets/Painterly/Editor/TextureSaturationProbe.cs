using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Measures how much colour the village's own textures actually contain.
    ///
    /// The shader brings colour back by un-desaturating the albedo — lerp(grey,
    /// albedo, _ColorRestore). That is only a restoration if the colour was still
    /// in the texture underneath. If the textures arrived greyscale, which "the
    /// village imports colourless" might easily have been taken to mean, then
    /// _ColorRestore = 1 changes nothing anywhere and the game's central mechanic
    /// cannot fire at all.
    ///
    /// That is a real possibility rather than a worry, because a restored fountain
    /// cannot be checked by eye: the thing it is made of is rock trim, and rock trim
    /// is very nearly grey to begin with. So the restored and unrestored versions of
    /// a stone fountain differ by a few percent of saturation even when the whole
    /// mechanism is working perfectly.
    ///
    /// This measures rather than argues. A mean chroma of 0.004 is a decision to be
    /// made somewhere else — it is only a decision that can be made if it is printed.
    ///
    /// Sampling goes through a blit rather than GetPixels, because the village's
    /// textures were imported without Read/Write enabled and GetPixels would simply
    /// fail on every one of them, which reads as "no colour found" and is not the
    /// same thing as no colour existing.
    /// </summary>
    public static class TextureSaturationProbe
    {
        const string Report = "Temp/texture_saturation.txt";
        const int Sample = 256;   // enough to judge chroma, cheap enough to do 40 of them

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] how much colour is actually in the textures?");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sb.AppendLine("STOPPED: exit play mode first (Ctrl+P).");
                Finish(sb);
                return;
            }

            // Every material the scene actually draws with, not every material in
            // the project: an unused texture's colour changes nothing on screen.
            // Keyed by asset path rather than by instance id: GetInstanceID is an
            // error in Unity 6000.6, and a path is a better identity anyway, because
            // it survives a domain reload and reads out in the report.
            var used = new List<Material>();
            var seen = new HashSet<string>();
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    if (!seen.Add(AssetDatabase.GetAssetPath(m))) continue;
                    used.Add(m);
                }
            }

            sb.AppendLine();
            sb.AppendLine("materials in use by the scene: " + used.Count);

            // One pass per texture, however many materials point at it.
            var perTexture = new Dictionary<string, Stats>();
            var byMaterial = new List<(Material mat, Stats s)>();

            foreach (var m in used)
            {
                var tex = m.GetTexture("_BaseMap") as Texture2D;
                if (tex == null) { byMaterial.Add((m, null)); continue; }

                var texPath = AssetDatabase.GetAssetPath(tex);
                if (!perTexture.TryGetValue(texPath, out var s))
                {
                    s = Measure(tex);
                    perTexture[texPath] = s;
                }
                byMaterial.Add((m, s));
            }

            // --- per material ----------------------------------------------------
            sb.AppendLine();
            sb.AppendLine("--- materials in use ---");
            sb.AppendLine("material".PadRight(26) + "chroma  p95    max    luma   verdict");
            foreach (var (mat, s) in byMaterial.OrderByDescending(x => x.s != null ? x.s.MeanChroma : -1f))
            {
                if (s == null)
                {
                    sb.AppendLine(mat.name.PadRight(26) + "  (no _BaseMap texture)");
                    continue;
                }
                sb.AppendLine(
                    mat.name.PadRight(26)
                    + s.MeanChroma.ToString("F4") + "  "
                    + s.P95Chroma.ToString("F3").PadRight(5) + "  "
                    + s.MaxChroma.ToString("F3").PadRight(5) + "  "
                    + s.MeanLuma.ToString("F3").PadRight(5) + "  "
                    + Verdict(s.MeanChroma));
            }

            // --- per texture -----------------------------------------------------
            sb.AppendLine();
            sb.AppendLine("--- distinct textures behind those materials ---");
            foreach (var kv in perTexture.OrderByDescending(k => k.Value.MeanChroma))
            {
                var tex = kv.Value.Texture;
                sb.AppendLine(Path.GetFileName(tex != null ? AssetDatabase.GetAssetPath(tex) : "?")
                              .PadRight(30)
                              + (tex != null ? tex.width + "x" + tex.height : "?").PadRight(11)
                              + "chroma " + kv.Value.MeanChroma.ToString("F4")
                              + "   p95 " + kv.Value.P95Chroma.ToString("F3")
                              + "   " + Verdict(kv.Value.MeanChroma));
            }

            // --- the question, answered ------------------------------------------
            var anyColour = perTexture.Values.Where(s => s.MeanChroma >= 0.05f).ToList();
            sb.AppendLine();
            sb.AppendLine("--- verdict ---");
            if (anyColour.Count == 0)
            {
                sb.AppendLine("EVERY texture sampled is effectively greyscale.");
                sb.AppendLine();
                sb.AppendLine("That means _ColorRestore cannot bring colour back by itself, on");
                sb.AppendLine("anything, because lerp(grey, grey, 1) is still grey. The mechanism");
                sb.AppendLine("needs a colour to restore *to*: a per-material restored tint, or a");
                sb.AppendLine("saturation target, rather than only an amount.");
            }
            else
            {
                sb.AppendLine(anyColour.Count + " of " + perTexture.Count + " textures carry real colour.");
                sb.AppendLine("The lerp(grey, albedo, restore) approach will work on those.");
                var worst = perTexture.Values.OrderBy(s => s.MeanChroma).Take(8).ToList();
                sb.AppendLine("Textures with little or none (restore will be nearly invisible here):");
                foreach (var s in worst)
                    sb.AppendLine("   " + Path.GetFileName(s.Texture != null ? AssetDatabase.GetAssetPath(s.Texture) : "?")
                                  + "  chroma " + s.MeanChroma.ToString("F4")
                                  + (s.MeanChroma < 0.05f ? "   <- greyscale" : ""));
            }

            Finish(sb);
        }

        sealed class Stats
        {
            public Texture2D Texture;
            public float MeanChroma;
            public float P95Chroma;
            public float MaxChroma;
            public float MeanLuma;
        }

        static string Verdict(float chroma) =>
            chroma < 0.01f ? "GREYSCALE"
            : chroma < 0.05f ? "nearly grey"
            : chroma < 0.15f ? "muted" : "coloured";

        static Stats Measure(Texture2D tex)
        {
            var st = new Stats { Texture = tex };

            int w = Mathf.Max(1, Mathf.Min(Sample, tex.width));
            int h = Mathf.Max(1, Mathf.Min(Sample, tex.height));

            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            var read = new Texture2D(w, h, TextureFormat.RGBA32, false);
            try
            {
                var prev = RenderTexture.active;
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                read.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                read.Apply();
                RenderTexture.active = prev;

                var px = read.GetPixels32();
                var chromas = new List<float>(px.Length);
                double sumChroma = 0, sumLuma = 0;
                float maxChroma = 0f;

                foreach (var c in px)
                {
                    if (c.a < 8) continue;   // transparent texels are not a colour
                    float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f;
                    float hi = Mathf.Max(r, Mathf.Max(g, b));
                    float lo = Mathf.Min(r, Mathf.Min(g, b));
                    float chroma = hi - lo;

                    sumChroma += chroma;
                    sumLuma += 0.2126f * r + 0.7152f * g + 0.0722f * b;
                    if (chroma > maxChroma) maxChroma = chroma;
                    chromas.Add(chroma);
                }

                if (chromas.Count > 0)
                {
                    st.MeanChroma = (float)(sumChroma / chromas.Count);
                    st.MeanLuma = (float)(sumLuma / chromas.Count);
                    st.MaxChroma = maxChroma;
                    chromas.Sort();
                    st.P95Chroma = chromas[Mathf.Clamp((int)(chromas.Count * 0.95f), 0, chromas.Count - 1)];
                }
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(read);
            }

            return st;
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
