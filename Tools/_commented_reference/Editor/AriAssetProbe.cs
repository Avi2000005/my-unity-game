using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Reports everything the character FBX actually brought with it.
    /// Writes Temp/ari_assets.txt.
    ///
    /// The FBX is 17 MB, which is far too heavy for a single untextured mesh,
    /// so the media is almost certainly in there somewhere. But the earlier
    /// probe looked only for _BaseMap and _MainTex and found neither, and an FBX
    /// is free to name its texture slot anything at all. So every sub-asset and
    /// every texture-valued shader property is listed instead of assuming.
    ///
    /// This matters before any PainterlyLit material can be made, because that
    /// shader desaturates an albedo. If Ari arrives with no albedo, the
    /// desaturation has nothing to act on and she renders flat white — which
    /// looks identical to a working grey, and would be easy to mistake for one.
    /// </summary>
    public static class AriAssetProbe
    {
        const string Dir = "Assets/Art/Ari/Models";

        [MenuItem("Tools/Echoes/Probe Ari Assets", priority = 93)]
        public static void Run()
        {
            var sb = new StringBuilder();

            foreach (var model in new[] { "Ari_character", "Idle", "Walking" })
                Probe(sb, model);

            sb.AppendLine("=== files on disk ===");
            foreach (var f in Directory.GetFiles(Dir, "*", SearchOption.AllDirectories)
                                       .OrderBy(x => x))
            {
                var info = new FileInfo(f);
                sb.AppendLine($"  {info.Name,-42} {info.Length / 1024} KB");
            }

            File.WriteAllText("Temp/ari_assets.txt", sb.ToString());
            Debug.Log("[Echoes] Ari asset probe -> Temp/ari_assets.txt\n" + sb);
        }

        static void Probe(StringBuilder sb, string model)
        {
            string path = $"{Dir}/{model}.fbx";
            sb.AppendLine($"=== {model} ===");

            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) { sb.AppendLine("  NOT IMPORTED\n"); return; }

            // ---- every sub-asset the importer produced ----
            var sub = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var group in sub.GroupBy(a => a.GetType().Name).OrderBy(g => g.Key))
            {
                sb.AppendLine($"  {group.Count()}x {group.Key}");
                foreach (var a in group.Take(6)) sb.AppendLine($"      {a.name}");
                if (group.Count() > 6) sb.AppendLine($"      ... +{group.Count() - 6} more");
            }

            // ---- every texture-valued property on every material ----
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            var materialMode = importer == null
                ? "n/a"
                : importer.materialImportMode.ToString();
            sb.AppendLine($"  materialImportMode={materialMode}");

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                sb.AppendLine($"  renderer {r.GetType().Name} '{r.name}' " +
                              $"enabled={r.enabled}");

                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) { sb.AppendLine("      material <null>"); continue; }
                    sb.AppendLine($"      material '{m.name}' shader={m.shader?.name}");

                    foreach (var pname in m.GetTexturePropertyNames())
                    {
                        var tex = m.GetTexture(pname);
                        sb.AppendLine($"        {pname} = " +
                                      (tex == null ? "<none>"
                                       : $"{tex.name} {tex.width}x{tex.height} " +
                                         $"fmt={tex.graphicsFormat}"));
                    }

                    if (m.HasProperty("_BaseColor"))
                        sb.AppendLine($"        _BaseColor = {m.GetColor("_BaseColor")}");
                }
            }

            // ---- mesh detail, including vertex colour ----
            var smr = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var mesh = smr != null ? smr.sharedMesh
                                   : root.GetComponentInChildren<MeshFilter>(true)?.sharedMesh;
            if (mesh != null)
            {
                var colors = mesh.colors;
                bool anyColor = false;
                if (colors != null && colors.Length == mesh.vertexCount)
                {
                    foreach (var c in colors)
                        if (c.r > 0.01f || c.g > 0.01f || c.b > 0.01f) { anyColor = true; break; }
                }

                sb.AppendLine($"  mesh '{mesh.name}' verts={mesh.vertexCount} " +
                              $"tris={mesh.triangles.Length / 3} " +
                              $"uv0={mesh.uv.Length} colors={colors?.Length ?? 0} " +
                              $"coloursUsed={anyColor}");
                sb.AppendLine($"  bounds {mesh.bounds.size} " +
                              $"(FBX root scale {root.transform.localScale})");
            }

            sb.AppendLine();
        }
    }
}
