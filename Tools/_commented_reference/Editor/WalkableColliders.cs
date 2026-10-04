using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Gives the walkable surfaces colliders. Writes Temp/colliders.txt.
    ///
    /// The generator spawns the market square and the paths as renderers only,
    /// so the whole village had exactly one collider in it: a single large
    /// Ground box at y=0. Anything standing on the plaza therefore probes
    /// straight through 89 paving tiles, hits the ground plane 0.08 below them,
    /// and sinks into the square up to the ankles. That is not a movement bug —
    /// it is the surfaces not existing to the physics ray.
    ///
    /// Colliders are added to the paving and the paths only. The kerbs, walls,
    /// roofs and props are left alone: they need to stop her eventually, but a
    /// walkable surface is what is missing right now, and adding 355 static
    /// mesh colliders is already a real cost that should be measured rather than
    /// spent on decoration.
    /// </summary>
    public static class WalkableColliders
    {
        [MenuItem("Tools/Echoes/Add Walkable Colliders", priority = 64)]
        public static void Run()
        {
            try
            {
                RunInner();
            }
            catch (System.Exception e)
            {
                File.WriteAllText("Temp/colliders_error.txt", e.ToString());
                Debug.LogError("[Echoes] colliders failed\n" + e);
            }
        }

        static void RunInner()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Echoes] stop play mode before adding colliders; " +
                               "they would be discarded on exit.");
                return;
            }

            var sb = new StringBuilder();
            var village = GameObject.Find("Village_Grey");
            if (village == null)
            {
                Debug.LogError("[Echoes] no Village_Grey in the scene");
                return;
            }

            // The walkable set: the square's paving and the street tiles. Both
            // are flat, single-renderer, and named by the kit's own convention,
            // which is what keeps this from sweeping up walls and roofs.
            var walkable = new System.Collections.Generic.List<Renderer>();

            var square = village.transform.Find("MarketSquare");
            if (square != null)
                walkable.AddRange(square.GetComponentsInChildren<Renderer>(true)
                                        .Where(r => r.name.StartsWith("Floor_")));

            var paths = village.transform.Find("Paths");
            if (paths != null)
                walkable.AddRange(paths.GetComponentsInChildren<Renderer>(true));

            sb.AppendLine($"{walkable.Count} walkable renderer(s) found " +
                          $"({square?.GetComponentsInChildren<Renderer>(true).Count(r => r.name.StartsWith("Floor_")) ?? 0} paving, " +
                          $"{paths?.GetComponentsInChildren<Renderer>(true).Length ?? 0} path tiles)");

            int added = 0, skipped = 0, failed = 0;
            var heights = new System.Collections.Generic.SortedSet<float>();

            foreach (var r in walkable)
            {
                var existing = r.GetComponent<Collider>();
                if (existing != null) { skipped++; continue; }

                var mc = r.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = r is SkinnedMeshRenderer
                    ? null
                    : r.GetComponent<MeshFilter>()?.sharedMesh;

                if (mc.sharedMesh == null)
                {
                    // Nothing to collide against. Left in place would be worse
                    // than absent — an empty MeshCollider is a null reference the
                    // physics scene has to skip every query.
                    Object.DestroyImmediate(mc);
                    failed++;
                    continue;
                }

                mc.convex = false;
                mc.isTrigger = false;
                added++;
                heights.Add(Mathf.Round(r.bounds.max.y * 1000f) / 1000f);
            }

            sb.AppendLine($"added={added} already had one={skipped} no mesh={failed}");
            sb.AppendLine($"surface heights present: {string.Join(", ", heights)}");

            // Reported because a single height means the ground probe will find
            // one level and the character will step oddly at the seams.
            sb.AppendLine(heights.Count > 1
                ? $"note: {heights.Count} distinct heights, so she will step between them"
                : "note: one height only");

            EditorUtility.SetDirty(village);
            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            var text = sb.ToString();
            File.WriteAllText("Temp/colliders.txt", text);
            Debug.Log("[Echoes] Walkable colliders\n" + text);
        }
    }
}
