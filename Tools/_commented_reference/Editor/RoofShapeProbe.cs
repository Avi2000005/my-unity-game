using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Answers the question the bounding-box survey cannot: is a kit roof *hipped*
    /// (slopes on all four sides) or *gabled* (slopes on two, with flat triangular
    /// ends)?
    ///
    /// It matters because the difference is not cosmetic. A gabled roof leaves a
    /// triangular void above the end walls that has to be closed with a separate
    /// piece, and the kit ships those — Roof_Front_Brick{2,4,6,8} plus _Half_L/R
    /// variants, which are exactly the shape of the gap. If the roofs turn out to be
    /// gabled and I place only the roof and the walls, every house has a hole in
    /// each gable end and you can see the sky through the building.
    ///
    /// Renderer.bounds cannot answer this — it is a single box, and a hipped and a
    /// gabled roof of the same overall size have identical boxes. The method here is
    /// to read the mesh's vertices, bin them by height, and measure how the XZ
    /// footprint narrows as you go up:
    ///
    ///   narrows in both X and Z  -> hipped
    ///   narrows in one axis only -> gabled, and the other axis needs a Roof_Front piece
    ///   no narrowing            -> flat slab
    ///
    /// Reading vertices rather than trusting a name is the whole point. The kit's
    /// "RoundTiles" naming says nothing about the roof's actual form.
    /// </summary>
    public static class RoofShapeProbe
    {
        const string OutPath = "Temp/roof_shapes.txt";
        const string ModelDir = VillageGenerator.ModelDir;

        [MenuItem("Tools/Echoes/Probe Roof Shapes", priority = 50)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Roof cross-sections, measured from mesh vertices");
            sb.AppendLine("(footprint width at 5 height bands, as a fraction of the base)");
            sb.AppendLine();

            var names = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelDir }))
            {
                var n = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                if (n.StartsWith("Roof_")) names.Add(n);
            }
            names.Sort();

            int gabled = 0, hipped = 0, flat = 0;

            foreach (var name in names)
            {
                string line = Describe(name);
                if (line == null) continue;
                sb.AppendLine(line);
                if (line.Contains("GABLED")) gabled++;
                else if (line.Contains("HIPPED")) hipped++;
                else if (line.Contains("FLAT")) flat++;
            }

            sb.AppendLine();
            sb.AppendLine($"summary: {hipped} hipped, {gabled} gabled, {flat} flat " +
                          $"(of {names.Count} roof models)");

            var path = Path.Combine(
                Path.GetDirectoryName(Application.dataPath), OutPath);
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"[Echoes] Roof shapes written to {OutPath}\n" +
                      $"hipped={hipped} gabled={gabled} flat={flat}");
        }

        /// <summary>
        /// Edges that belong to exactly one triangle — that is, holes in the mesh.
        ///
        /// A roof modelled as a solid has none. A roof modelled as sloped panels
        /// with nothing closing the gable ends has a boundary running round each
        /// open end, and a building wearing one of those is see-through where it
        /// matters least to the silhouette and most to the player.
        /// </summary>
        static int CountBoundaryEdges(Mesh mesh)
        {
            int[] tris;
            try
            {
                // GetTriangles needs an explicit submesh index, and these FBX
                // pieces are not guaranteed to be single-material, so gather them
                // all. The index buffer is shared, so concatenating the submeshes
                // is still one consistent set of triangles.
                int total = 0;
                for (int s = 0; s < mesh.subMeshCount; s++) total += mesh.GetIndices(s).Length;

                tris = new int[total];
                int at = 0;
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    var idx = mesh.GetIndices(s);
                    System.Array.Copy(idx, 0, tris, at, idx.Length);
                    at += idx.Length;
                }
            }
            catch
            {
                return -1;   // mesh not readable from script
            }

            if (tris.Length < 3) return -1;

            // Quantised endpoints, so vertices that coincide within a hair are
            // treated as the same point. Weld tolerance has to be far below the
            // smallest real feature or genuine seams get counted as holes, and far
            // above float noise or real holes get missed.
            const float Weld = 0.0005f;

            var seen = new Dictionary<long, int>(tris.Length / 3);

            for (int t = 0; t < tris.Length; t += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    int a = tris[t + e], b = tris[t + (e + 1) % 3];
                    var key = EdgeKey(mesh, a, b, Weld);
                    if (!seen.TryGetValue(key, out int n)) seen[key] = 1;
                    else seen[key] = n + 1;
                }
            }

            int open = 0;
            foreach (var kv in seen) if (kv.Value == 1) open++;
            return open;
        }

        /// <summary>
        /// Order-independent key for an undirected edge, with endpoints snapped to
        /// a grid. Sorting the two indices makes (a,b) and (b,a) the same key, which
        /// is what lets a shared edge between two triangles be counted twice.
        /// </summary>
        static long EdgeKey(Mesh mesh, int a, int b, float weld)
        {
            var pa = mesh.vertices[a];
            var pb = mesh.vertices[b];

            long qa = (Quant(pa.x, weld) << 42) ^ (Quant(pa.y, weld) << 21) ^ Quant(pa.z, weld);
            long qb = (Quant(pb.x, weld) << 42) ^ (Quant(pb.y, weld) << 21) ^ Quant(pb.z, weld);

            long lo = qa < qb ? qa : qb;
            long hi = qa < qb ? qb : qa;
            return unchecked((lo * 1000003L) ^ (hi * 31L));
        }

        static long Quant(float v, float weld) => (long)Mathf.Round(v / weld);

        static string Describe(string name)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{name}.fbx");
            if (model == null) return null;

            var probe = (GameObject)PrefabUtility.InstantiatePrefab(model);
            probe.hideFlags = HideFlags.HideAndDontSave;

            var rend = probe.GetComponentInChildren<Renderer>();
            if (rend == null)
            {
                Object.DestroyImmediate(probe);
                return null;
            }

            var mf = rend.GetComponent<MeshFilter>();
            var mesh = mf != null ? mf.sharedMesh : null;
            var b = rend.bounds;

            string result;
            if (mesh == null || mesh.vertexCount == 0)
            {
                result = "no readable mesh";
            }
            else
            {
                // World-space vertices, matching the frame rend.bounds reports.
                //
                // The transform must come from the MeshFilter, not the prefab root.
                // These kit FBXs put the mesh on a child and carry a corrective
                // rotation on the root, so using the root matrix would stand the
                // cross-section on its side and silently measure the wrong axis.
                var verts = mesh.vertices;
                var m = mf.transform.localToWorldMatrix;

                const int Bands = 5;
                var minX = new float[Bands];
                var maxX = new float[Bands];
                var minZ = new float[Bands];
                var maxZ = new float[Bands];
                var count = new int[Bands];

                for (int i = 0; i < Bands; i++)
                { minX[i] = float.MaxValue; maxX[i] = float.MinValue;
                  minZ[i] = float.MaxValue; maxZ[i] = float.MinValue; }

                float y0 = b.min.y, y1 = Mathf.Max(b.max.y, b.min.y + 0.0001f);

                for (int i = 0; i < verts.Length; i++)
                {
                    var v = m.MultiplyPoint3x4(verts[i]);
                    float t = Mathf.Clamp01((v.y - y0) / (y1 - y0));
                    int band = Mathf.Clamp((int)(t * Bands), 0, Bands - 1);
                    count[band]++;
                    if (v.x < minX[band]) minX[band] = v.x;
                    if (v.x > maxX[band]) maxX[band] = v.x;
                    if (v.z < minZ[band]) minZ[band] = v.z;
                    if (v.z > maxZ[band]) maxZ[band] = v.z;
                }

                // Width of the footprint in each axis, normalised to the base band.
                // The top band is skipped: near the apex a hipped roof's true
                // footprint is a ridge line or a point, and one sparse triangle can
                // make the ratio noisy. The second-from-top band is the honest one.
                int baseBand = 0, topBand = Bands - 2;
                float baseW = maxX[baseBand] - minX[baseBand];
                float baseD = maxZ[baseBand] - minZ[baseBand];
                float topW = count[topBand] > 0 ? maxX[topBand] - minX[topBand] : baseW;
                float topD = count[topBand] > 0 ? maxZ[topBand] - minZ[topBand] : baseD;

                float rx = baseW > 0.001f ? topW / baseW : 1f;
                float rz = baseD > 0.001f ? topD / baseD : 1f;

                // "Tapers" means meaningfully narrower than the base, not just
                // slightly. A hip recedes a lot in both axes; a gable recedes in one.
                const float Taper = 0.85f;
                bool tapersX = rx < Taper;
                bool tapersZ = rz < Taper;

                string kind = (tapersX && tapersZ) ? "HIPPED"
                            : (tapersX || tapersZ) ? "GABLED"
                            : "FLAT";

                var bands = new StringBuilder();
                for (int i = 0; i < Bands; i++)
                {
                    if (count[i] == 0) { bands.Append("  --  "); continue; }
                    float w = maxX[i] - minX[i], d = maxZ[i] - minZ[i];
                    bands.Append($"{w:0.00}x{d:0.00} ");
                }

                // Is the mesh a closed solid, or just a shell of sloped panels?
                //
                // This is the difference between a house that is sealed and one you
                // can see daylight through at the gable ends. An edge belonging to
                // exactly one triangle is a boundary edge: a hole in the surface.
                // A watertight mesh has none.
                int boundary = CountBoundaryEdges(mesh);
                string seal = boundary == 0 ? "sealed" : $"{boundary} open edges";

                result = $"{kind,-7} {name,-32} {b.size.x:0.00}x{b.size.y:0.00}x{b.size.z:0.00}  " +
                         $"top/base X {rx:0.00} Z {rz:0.00}  {seal,-14}  bands: {bands}";
            }

            Object.DestroyImmediate(probe);
            return result;
        }
    }
}
