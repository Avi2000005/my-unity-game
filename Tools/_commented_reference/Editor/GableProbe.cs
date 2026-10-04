using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Answers the only question that actually matters about a gable roof: is there
    /// a hole you can see through?
    ///
    /// RoofShapeProbe already established that Roof_RoundTiles_* are gabled — the
    /// cross-section narrows in X while Z stays constant, so they are triangular
    /// prisms with a ridge along Z. It also showed they are thin shells with many
    /// boundary edges. Those boundary edges are ambiguous on their own: a shell of
    /// overlapping tile strips has them everywhere, whether or not the gable end is
    /// closed, and the count even grows with the roof's length, which says nothing
    /// about the ends.
    ///
    /// So this asks the direct question instead of inferring it. A ray is fired
    /// along Z, through the middle of the gable end, at a range of heights between
    /// the eave and the ridge. If the gable is closed, something stops the ray. If
    /// it is open, the ray passes clean through the building and out the far side.
    ///
    /// The same ray is then fired along X, through the middle of the building. That
    /// one is expected to be blocked, because the roof is solid across its span
    /// there — it doubles as a control, so a "blocked" result cannot be an artefact
    /// of the collider setup.
    /// </summary>
    public static class GableProbe
    {
        const string OutPath = "Temp/gable_holes.txt";
        const string ModelDir = VillageGenerator.ModelDir;

        [MenuItem("Tools/Echoes/Probe Gable Holes", priority = 51)]
        public static void Run()
        {
            var names = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelDir }))
            {
                var n = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
                if (n.StartsWith("Roof_RoundTiles_") || n == "Roof_Tower_RoundTiles")
                    names.Add(n);
            }
            names.Sort();

            var host = new GameObject("__GableProbe");
            host.hideFlags = HideFlags.HideAndDontSave;

            var sb = new StringBuilder();
            sb.AppendLine("Gable end closure, by raycast (Z = along the ridge, X = across)");
            sb.AppendLine("'open' at a height means you can see straight through the building there.");
            sb.AppendLine();

            int leaky = 0;

            foreach (var name in names)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{name}.fbx");
                if (model == null) continue;

                // Single-arg InstantiatePrefab on purpose: the two-arg overload is
                // ambiguous against the Scene version here, and it is also known to
                // silently drop the prefab root's corrective rotation. Parenting
                // afterwards with worldPositionStays keeps the transform intact.
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
                inst.hideFlags = HideFlags.HideAndDontSave;
                inst.transform.SetParent(host.transform, true);

                var rends = inst.GetComponentsInChildren<Renderer>();
                if (rends.Length == 0) { Object.DestroyImmediate(inst); continue; }

                // Temporary colliders: the kit ships no colliders, and Physics
                // needs a collider rather than a renderer to be raycast against.
                foreach (var r in rends)
                {
                    if (r.GetComponent<MeshCollider>() != null) continue;
                    var mc = r.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = r.GetComponent<MeshFilter>() != null
                                  ? r.GetComponent<MeshFilter>().sharedMesh : null;
                }
                Physics.SyncTransforms();

                var b = new Bounds(Vector3.zero, Vector3.zero);
                foreach (var r in rends) b.Encapsulate(r.bounds);

                // Fire between the eave and 80% of the way to the ridge. Above that
                // the triangle is so thin that missing it proves nothing; the wide
                // part just above the eave is where a hole actually shows.
                float y0 = b.min.y + (b.max.y - b.min.y) * 0.15f;
                float y1 = b.min.y + (b.max.y - b.min.y) * 0.80f;

                const int Steps = 9;
                int openZ = 0, openX = 0;

                for (int i = 0; i < Steps; i++)
                {
                    float t = Steps == 1 ? 0f : i / (float)(Steps - 1);
                    float y = Mathf.Lerp(y0, y1, t);
                    var probe = new Vector3(b.center.x, y, b.center.z);

                    if (!Blocked(probe, Vector3.forward, b.extents.z * 2f + 4f)) openZ++;
                    if (!Blocked(probe, Vector3.right, b.extents.x * 2f + 4f)) openX++;
                }

                string verdict = openZ > 0
                    ? $"SEE-THROUGH at {openZ}/{Steps} heights"
                    : "closed";

                if (openZ > 0) leaky++;
                if (openX > 0)
                    sb.AppendLine($"  (note: also open across X at {openX}/{Steps} — collider may be unreliable)");

                sb.AppendLine($"{name,-26} {b.size.x:0.0}x{b.size.y:0.0}x{b.size.z:0.0}  " +
                              $"gable Z: {verdict,-26} across X: {(openX == 0 ? "blocked (control ok)" : "OPEN")}");

                Object.DestroyImmediate(inst);
            }

            Object.DestroyImmediate(host);

            sb.AppendLine();
            sb.AppendLine($"{leaky} of {names.Count} round-tile roofs have an open gable end.");

            var path = Path.Combine(Path.GetDirectoryName(Application.dataPath), OutPath);
            File.WriteAllText(path, sb.ToString());

            Debug.Log($"[Echoes] Gable probe written to {OutPath}\n{leaky} of {names.Count} roofs open at the gable.");
        }

        /// <summary>
        /// True if anything blocks a ray leaving <paramref name="origin"/> in
        /// <paramref name="dir"/>. The ray is started slightly outside the bounds so
        /// it cannot begin already embedded in the roof.
        /// </summary>
        static bool Blocked(Vector3 origin, Vector3 dir, float span)
        {
            var start = origin + dir * (span * 0.5f + 0.05f);
            return Physics.Raycast(start, -dir, span, ~0, QueryTriggerInteraction.Ignore);
        }
    }
}
