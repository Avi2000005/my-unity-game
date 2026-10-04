using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Pulls the texture embedded inside a downloaded FBX out to a standalone
    /// PNG.
    ///
    /// Mixamo ships a character and its animations as separate FBXs, and only
    /// the character FBX carries the artwork. The image is stored as an embedded
    /// <c>Video</c> node with raw <c>Content</c> rather than beside the file, so
    /// there is nothing in the source folder to copy — the bytes only exist once
    /// an importer has decoded them. The same character rig is shared by every
    /// animation FBX, which is why the texture is authored to Ari's UVs and
    /// lands correctly on her rather than needing a UV remap.
    /// </summary>
    public static class EmbeddedTextureExtract
    {
        const string Report = "Temp/extract_texture.txt";

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] Embedded texture extraction");
            sb.AppendLine();

            Extract("Assets/Art/Ari/Models/Ari_character.fbx",
                    "Assets/Art/Ari/Textures/Ari_basecolor.png", sb);

            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report, sb.ToString());
            Debug.Log(sb.ToString());
        }

        static void Extract(string fbxPath, string outPath, StringBuilder sb)
        {
            sb.AppendLine($"source: {fbxPath}");

            if (!File.Exists(fbxPath))
            {
                sb.AppendLine("  MISSING from disk - nothing to extract");
                return;
            }

            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                sb.AppendLine("  no ModelImporter - Unity has not imported this file yet");
                return;
            }

            // GetPixels needs a CPU-side copy. FBX sub-assets default to
            // non-readable, and asking for pixels on one throws rather than
            // returning black, so readability is requested before measuring.
            bool wasReadable = importer.isReadable;
            if (!wasReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            sb.AppendLine($"  sub-assets: {assets.Length}");

            var textures = new System.Collections.Generic.List<Texture2D>();
            var materials = new System.Collections.Generic.List<Material>();
            foreach (var a in assets)
            {
                if (a is Texture2D t && t != null) textures.Add(t);
                else if (a is Material m && m != null) materials.Add(m);
            }

            sb.AppendLine($"  Texture2D: {textures.Count}   Material: {materials.Count}");

            foreach (var m in materials)
            {
                sb.AppendLine($"  material '{m.name}' shader='{m.shader.name}'");
                if (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null)
                    sb.AppendLine($"    _BaseMap -> {m.GetTexture("_BaseMap").name}");
                if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null)
                    sb.AppendLine($"    _MainTex -> {m.GetTexture("_MainTex").name}");
            }

            if (textures.Count == 0)
            {
                sb.AppendLine("  NO TEXTURE FOUND in this FBX");
                return;
            }

            // Largest wins: a character FBX can carry a normal map alongside the
            // albedo, and the albedo is the bigger of the pair.
            Texture2D best = textures[0];
            foreach (var t in textures)
            {
                if ((long)t.width * t.height > (long)best.width * best.height) best = t;
            }

            sb.AppendLine();
            sb.AppendLine($"chosen: '{best.name}' {best.width}x{best.height} fmt={best.format}");

            ReportLuma(best, sb);

            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            var png = best.EncodeToPNG();
            File.WriteAllBytes(outPath, png);

            // Only a committed file becomes a Unity asset; writing the bytes is
            // not enough, or the next AssetDatabase.Refresh has nothing to find.
            AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

            sb.AppendLine($"written: {outPath} ({png.Length:N0} bytes)");

            if (!wasReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }

        static void ReportLuma(Texture2D t, StringBuilder sb)
        {
            try
            {
                var px = t.GetPixels();
                if (px == null || px.Length == 0) { sb.AppendLine("  luma: no pixels readable"); return; }

                double sum = 0, bright = 0, dark = 0;
                foreach (var c in px)
                {
                    double l = 0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b;
                    sum += l;
                    if (l > 0.55) bright++;
                    if (l < 0.18) dark++;
                }

                int n = px.Length;
                sb.AppendLine($"  pixels   : {n:N0}");
                sb.AppendLine($"  avg luma : {sum / n * 255.0:F1} / 255");
                sb.AppendLine($"  bright   : {bright * 100.0 / n:F1}%  (>140)");
                sb.AppendLine($"  near-blk : {dark * 100.0 / n:F1}%  (<45)");
            }
            catch (System.Exception e)
            {
                sb.AppendLine($"  luma FAILED: {e.GetType().Name}: {e.Message}");
            }
        }
    }
}
