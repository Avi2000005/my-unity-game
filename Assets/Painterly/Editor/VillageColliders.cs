using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Gives every village renderer a MeshCollider, so the paint brush and the
    /// camera can see the buildings. Writes Temp/colliders_all.txt.
    ///
    /// The village shipped with colliders only on paving and paths, which is
    /// correct for one job — keeping Ari's feet on the ground — and silently
    /// breaks two others. A brush raycast aimed at a wall passes through it and
    /// paints the ground behind, and a follow camera has nothing to collide
    /// against so it slides straight through houses.
    ///
    /// Ari is kept off the new geometry by slope rather than by layer. A layer
    /// mask would be a second, hand-maintained list of what she may stand on,
    /// and every new piece of kit geometry would default to the wrong side of
    /// it. Rejecting anything steeper than minGroundSlope needs no list.
    ///
    /// Everything already collidable is left alone, so re-running is safe and the
    /// 355 hand-placed ground colliders are not duplicated.
    /// </summary>
    public static class VillageColliders
    {
        [MenuItem("Tools/Echoes/Add Village Colliders", priority = 66)]
        public static void Run()
        {
            try { RunInner(); }
            catch (System.Exception e)
            {
                File.WriteAllText("Temp/colliders_all_error.txt", e.ToString());
                Debug.LogError("[Echoes] village colliders failed\n" + e);
            }
        }

        static void RunInner()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Echoes] stop play mode first; colliders added in " +
                               "play mode are discarded on exit.");
                return;
            }

            var sb = new StringBuilder();
            var scene = SceneManager.GetActiveScene();

            var village = GameObject.Find("Village_Grey");
            if (village == null)
            {
                sb.AppendLine("No Village_Grey in the scene.");
                File.WriteAllText("Temp/colliders_all.txt", sb.ToString());
                return;
            }

            // Renderer, not MeshRenderer, so the skinned case below is reachable.
            // Fetched as MeshRenderer it would be impossible to be a
            // SkinnedMeshRenderer and the branch would never run — dead code
            // that reads like a working guard.
            var renderers = village.GetComponentsInChildren<Renderer>(true);
            sb.AppendLine($"village renderers: {renderers.Length}");

            int added = 0, already = 0, shared = 0, failed = 0;
            var failedNames = new System.Collections.Generic.List<string>();

            foreach (var mr in renderers)
            {
                if (mr == null) continue;

                // Non-convex is right for static geometry and far cheaper, but it
                // cannot be added to a GameObject that already has a non-convex
                // collider on the same object, and it is illegal on a
                // SkinnedMeshRenderer. Neither is a fault here, so both are
                // counted and skipped rather than thrown on.
                if (mr.GetComponent<MeshCollider>() != null) { already++; continue; }
                if (mr is SkinnedMeshRenderer) { shared++; continue; }

                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) { failed++; continue; }

                // Static flags cleared, not set. Nothing here needs a Rigidbody —
                // a MeshCollider on a GameObject without one is already a static
                // collider, so there is no physics reason to touch these. They
                // are cleared because the village is spawned at runtime, and
                // leaving generator output flagged contributes nothing to a
                // lightmap or a bake it will never appear in.
                GameObjectUtility.SetStaticEditorFlags(
                    mr.gameObject, (StaticEditorFlags)0);

                try
                {
                    var mc = mr.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    added++;
                }
                catch
                {
                    failed++;
                    if (failedNames.Count < 8) failedNames.Add(mr.name);
                }
            }

            sb.AppendLine($"  added     {added}");
            sb.AppendLine($"  already   {already}");
            sb.AppendLine($"  skinned   {shared}  (a MeshCollider is illegal on these)");
            sb.AppendLine($"  no mesh   {failed}" +
                          (failedNames.Count > 0 ? "  e.g. " + string.Join(", ", failedNames) : ""));

            // ---- how Ari is meant to keep walking on the ground only ----
            var ari = GameObject.Find("Ari");
            if (ari == null)
            {
                sb.AppendLine("WARNING: no Ari, so the ground layer mask was not touched.");
            }
            else
            {
                var mover = ari.GetComponent<Echoes.Painterly.AriMover>();
                if (mover == null) sb.AppendLine("WARNING: Ari has no AriMover.");
                else sb.AppendLine("Ari's ground test rejects slopes under minGroundSlope, " +
                                   "so the new wall/roof colliders cannot be stood on.");
            }

            sb.AppendLine($"\ntotal colliders in scene: " +
                          $"{Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude).Length}");

            EditorSceneManager.MarkSceneDirty(scene);

            var text = sb.ToString();
            File.WriteAllText("Temp/colliders_all.txt", text);
            Debug.Log("[Echoes] Village colliders\n" + text);
        }
    }
}
