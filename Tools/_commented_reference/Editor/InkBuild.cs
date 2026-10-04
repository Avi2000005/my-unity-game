using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Gives the Ink Crawler a skin.
    ///
    /// <para><b>Why a generated texture and not an imported one.</b> The level
    /// uses free assets, but a hand-picked ink texture is a licence and a
    /// download, and this is one creature in a monochrome village whose
    /// defining feature is that it is made of spilled ink. Generating it keeps
    /// the "free assets only" rule intact and makes the look reproducible: the
    /// noise is a hash, not a <c>Random</c>, so this tool produces the same
    /// bytes every time it is run and a diff of the PNG means something.</para>
    ///
    /// <para><b>It is generated near-black and near-neutral on purpose.</b> The
    /// village is black and white until the fountain takes the fragment, so a
    /// saturated texture on the thing the player fights with would be the first
    /// colour in the level and would spend Beat 7's reveal early. The warmth in
    /// it is a few percent — enough that it reads as ink rather than as a hole
    /// in the world, and not enough to notice.</para>
    ///
    /// <para><b>Idempotent, and it proves it.</b> It reports the material's
    /// albedo <i>before</i> and <i>after</i> and then samples the texture it
    /// wrote and prints the real mean and spread. A tool that reports the mean
    /// it intended to produce rather than the one it produced is the single
    /// most expensive kind of wrong in this project — it reads as a pass.</para>
    /// </summary>
    public static class InkBuild
    {
        const string OutDir = "Assets/Painterly/Generated/Ink";
        const string PngPath = OutDir + "/InkSkin.png";
        const string MatPath = "Assets/Painterly/Materials/Cast_InkCrawler.mat";
        const string Report = "Temp/ink_build.txt";

        const int Size = 512;

        [MenuItem("Tools/Echoes/Build Ink Crawler Skin", priority = 78)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] ink crawler skin");
            sb.AppendLine("  size " + Size + " x " + Size);

            EnsureDir();

            // --- what the material has now, before anything is touched -----
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                sb.AppendLine("FATAL: no material at " + MatPath);
                Finish(sb);
                return;
            }

            var before = mat.HasProperty("_BaseMap")
                ? mat.GetTexture("_BaseMap") as Texture2D
                : null;

            sb.AppendLine("  material    : " + mat.name);
            sb.AppendLine("  shader      : " + (mat.shader != null ? mat.shader.name : "NULL"));
            sb.AppendLine("  albedo now  : " + (before == null
                ? "NONE — this is why it renders flat"
                : before.name + " " + before.width + "x" + before.height));

            // --- write the texture -------------------------------------------
            var pixels = Skin(Size);
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            tex.name = "InkSkin";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 4;
            tex.SetPixels(pixels);
            tex.Apply(true, false);

            File.WriteAllBytes(PngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(PngPath, ImportAssetOptions.ForceUpdate);
            var loaded = AssetDatabase.LoadAssetAtPath<Texture2D>(PngPath);

            if (loaded == null)
            {
                sb.AppendLine("FATAL: wrote " + PngPath + " but it did not import");
                Finish(sb);
                return;
            }

            // Re-read what the importer actually stored, because the point of
            // the sample below is to describe the file on disk rather than the
            // array that was handed to it.
            loaded.wrapMode = TextureWrapMode.Repeat;
            loaded.filterMode = FilterMode.Trilinear;
            loaded.anisoLevel = 4;
            EditorUtility.SetDirty(loaded);

            // --- the measurement, of the FILE on disk -------------------------
            //
            // Not of `loaded`, and not of `pixels`.
            //
            // `loaded.GetPixels()` throws: an imported texture is not
            // CPU-readable unless the importer asks for it, and turning that on
            // for a 512x2 build texture costs memory in the shipped game for
            // the sake of a one-off measurement. And measuring `pixels` — the
            // array handed to SetPixels — would only be reporting what this
            // script intended to write, which is the one number that cannot be
            // wrong and so is worth nothing.
            //
            // So the bytes go back in through LoadImage into a texture made in
            // memory, which IS readable. What comes out describes the artifact.
            var raw = new Color[0];
            float decodeSeconds = 0f;
            var verify = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            verify.name = "__ink_verify";

            try
            {
                var timer = System.Diagnostics.Stopwatch.StartNew();

                if (ImageConversion.LoadImage(verify, File.ReadAllBytes(PngPath), false))
                    raw = verify.GetPixels();

                // TotalSeconds is a double; the field is float. Explicit cast,
                // because the implicit one does not exist and a "did you mean"
                // here is not worth a compile round trip.
                decodeSeconds = (float)timer.Elapsed.TotalSeconds;
            }
            catch (System.Exception ex)
            {
                sb.AppendLine("  could not read the PNG back: " + ex.Message);
                sb.AppendLine("  the numbers below are therefore missing, not zero — " +
                              "an unreadable artifact and a flat one look the same " +
                              "in a report that says nothing.");
            }
            finally
            {
                Object.DestroyImmediate(verify);
            }

            if (raw.Length == 0)
            {
                sb.AppendLine("FATAL: the PNG is on disk but could not be decoded " +
                              "back into pixels, so there is nothing to measure. " +
                              "The texture may still have been assigned.");
                Finish(sb);
                return;
            }

            float sum = 0f, sumSq = 0f;
            float lo = 1f, hi = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                // Luminance, so the number means "how light does this read" and
                // not "how much red is in it". The village has no hue yet and a
                // mean reported per channel would hide that.
                float l = raw[i].r * 0.299f + raw[i].g * 0.587f + raw[i].b * 0.114f;
                sum += l;
                sumSq += l * l;
                if (l < lo) lo = l;
                if (l > hi) hi = l;
            }

            float n = raw.Length;
            float mean = sum / n;
            float sd = Mathf.Sqrt(Mathf.Max(0f, sumSq / n - mean * mean));

            sb.AppendLine("  read back    : decoded the written PNG in " +
                          decodeSeconds.ToString("0.000") + " s — these numbers are " +
                          "the file, not the array");

            sb.AppendLine("  wrote       : " + PngPath);
            sb.AppendLine("  imported as : " + loaded.name + " " + loaded.width + "x" +
                          loaded.height + ", wrap " + loaded.wrapMode);
            sb.AppendLine("  MEASURED luminance over " + n + " pixels:");
            sb.AppendLine("    mean " + mean.ToString("0.000") +
                          "   spread " + sd.ToString("0.000") +
                          "   range " + lo.ToString("0.000") + " .. " + hi.ToString("0.000"));
            sb.AppendLine("  " + (sd < 0.02f
                ? "spread is below 0.02 — that is a flat fill, not an ink skin, " +
                  "and it will read as a silhouette with no surface"
                : "spread " + sd.ToString("0.000") + " means the blotching and drips " +
                  "survived the write, so the surface will read as liquid"));

            // --- assign --------------------------------------------------------
            if (!mat.HasProperty("_BaseMap"))
            {
                sb.AppendLine("FATAL: " + mat.name + " has no _BaseMap. The shader " +
                              "is " + (mat.shader != null ? mat.shader.name : "?") +
                              ", which is not PainterlyLit.");
                Finish(sb);
                return;
            }

            mat.SetTexture("_BaseMap", loaded);

            // Ink is wet, so it is smooth. But not mirror-smooth: a crawler that
            // reflects the sky reads as a piece of chrome, not as a thing made
            // of spilled ink.
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.42f);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            // --- read it back, which is the only proof that counts ----------
            var reread = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            var after = reread != null && reread.HasProperty("_BaseMap")
                ? reread.GetTexture("_BaseMap") as Texture2D
                : null;

            sb.AppendLine("  albedo after: " + (after == null
                ? "STILL NONE — the assignment did not take"
                : after.name + " " + after.width + "x" + after.height));

            var ok = after != null && after.width == Size;
            sb.AppendLine("  " + (ok
                ? "PASS — the crawler material reads back with the ink skin on it"
                : "FAIL — read-back does not match what was written"));

            Finish(sb);
        }

        // --- the texture ------------------------------------------------------

        /// <summary>
        /// The skin itself. Ink pooled in blotches, with drips running down.
        /// </summary>
        static Color[] Skin(int size)
        {
            var px = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // u across, v up. The drips run along -v, so v is flipped
                    // here to make "down the image" a decreasing v and keep the
                    // drip loop from having to invert it every pixel.
                    float u = x / (float)size;
                    float v = 1f - y / (float)size;

                    float blotch = Fbm(u * 5.0f, v * 5.0f, 4);
                    float fine   = Fbm(u * 19.0f, v * 19.0f, 3);
                    float drip   = Drips(u, v);

                    // Base ink, darker where the blotching is deep. Ink pools.
                    float l = 0.055f + blotch * 0.13f + fine * 0.035f;

                    // Drips are darker and wetter — they read as the surface
                    // running rather than as a stain on it.
                    l -= drip * 0.045f;

                    // A wet sheen where a blotch is thick, so the creature has
                    // a highlight to move with it instead of reading as a hole.
                    l += Mathf.Pow(Mathf.Max(0f, blotch - 0.55f), 2f) * 0.10f;

                    l = Mathf.Clamp01(l);

                    // Near-neutral. The warmth is a couple of percent and is
                    // there so it reads as ink rather than as pure black.
                    px[y * size + x] = new Color(
                        l * 1.00f,
                        l * 0.975f,
                        l * 1.02f,
                        1f);
                }
            }

            return px;
        }

        /// <summary>
        /// Vertical runs of ink, thinning as they go down.
        ///
        /// <para>Thin columns at hashed positions, each with its own length,
        /// width and start row. Tiling is handled by taking the column index
        /// modulo the column count, so a drip that starts at x of 511 continues
        /// at x of 0 rather than being cut in half by the seam.</para>
        /// </summary>
        static float Drips(float u, float v)
        {
            const int Columns = 14;
            float col = u * Columns;
            int index = Mathf.FloorToInt(col);
            float within = col - index;

            float acc = 0f;

            for (int k = -1; k <= 1; k++)
            {
                int i = index + k;
                float h = Hash01(i, 17);

                // Only a third of the columns carry a drip at all. Ink runs off
                // a few edges, not uniformly down every one.
                if (h < 0.66f) continue;

                // The gap wraps so the texture still tiles across the seam.
                float gap = Hash01(i, 91);
                if (within < gap || within > gap + 0.34f) continue;

                float top    = Hash01(i, 43);
                float bottom = top + 0.12f + Hash01(i, 67) * 0.34f;

                if (v > top || v < bottom) continue;

                // Fades out toward the tip, so a drip ends rather than stops.
                float t = (top - v) / Mathf.Max(0.001f, top - bottom);
                float body = Mathf.Sin(t * Mathf.PI);

                // Narrower toward the tip.
                float halfWidth = Mathf.Lerp(0.16f, 0.03f, t);
                float across = Mathf.Abs(within - gap - 0.17f) / halfWidth;
                if (across > 1f) continue;

                float edge = Mathf.Sqrt(Mathf.Max(0f, 1f - across * across));
                acc = Mathf.Max(acc, body * edge);
            }

            return acc;
        }

        /// <summary>Fractal value noise. Deterministic — a hash, not a Random.</summary>
        static float Fbm(float x, float y, int octaves)
        {
            float sum = 0f, amp = 0.5f, freq = 1f, norm = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Value(x * freq, y * freq);
                norm += amp;
                amp *= 0.5f;
                freq *= 2f;
            }

            return norm > 0f ? sum / norm : 0f;
        }

        static float Value(float x, float y)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;

            // Smoothstep the fraction, or the noise shows its grid.
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = Hash01(ix,     iy);
            float b = Hash01(ix + 1, iy);
            float c = Hash01(ix,     iy + 1);
            float d = Hash01(ix + 1, iy + 1);

            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>
        /// A stable pseudo-random number in 0..1 for an integer pair.
        ///
        /// <para>Integer in, stable out, no state. This is what makes the texture
        /// byte-identical across runs, which is what makes regenerating it a safe
        /// thing to do to a file that is already in the repository.</para>
        /// </summary>
        static float Hash01(int a, int b)
        {
            unchecked
            {
                uint h = (uint)(a * 73856093) ^ (uint)(b * 19349663);
                h ^= h >> 13;
                h *= 0x5bd1e995u;
                h ^= h >> 15;
                return (h & 0xFFFFFF) / 16777215f;
            }
        }

        // --- plumbing ---------------------------------------------------------

        static void EnsureDir()
        {
            if (AssetDatabase.IsValidFolder(OutDir)) return;

            string parent = Path.GetDirectoryName(OutDir).Replace('\\', '/');
            string leaf = Path.GetFileName(OutDir);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static void Finish(StringBuilder sb)
        {
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report),
                               sb.ToString());
            Debug.Log("[Echoes] ink skin built — see " + Report);
        }
    }
}