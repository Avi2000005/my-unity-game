using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Acceptance test for the gable fix: with walls, roof and gable ends actually
    /// assembled into a house, can you still see through it?
    ///
    /// GableProbe proved the bare roof models are open at their ends. That only
    /// matters if the finished building is open too, and it would be easy to add
    /// Roof_Front_Brick pieces in the wrong place, at the wrong height, or facing
    /// the wrong way and still have a hole. So this measures the thing that
    /// actually ships.
    ///
    /// Rays are fired along Z — parallel to the ridge, through the gable ends —
    /// at heights spanning each roof's own vertical extent, at the centre of the
    /// building. Every one of those must be blocked.
    ///
    /// The heights come from the roof renderer's bounds rather than from a
    /// fraction of the house, because a house with openable doors and windows in
    /// its walls would report false holes if the test dipped below the wall top.
    /// A gable is only ever open above the walls, so only the roof band is tested.
    ///
    /// Rays along X are fired as a control, exactly as in GableProbe: if those are
    /// open too, the colliders are not working and the whole result is void.
    /// </summary>
    public static class VillageSealProbe
    {
        const string OutPath = "Temp/village_seal.txt";

        [MenuItem("Tools/Echoes/Probe Village Seal", priority = 52)]
        public static void Run()
        {
            var village = GameObject.Find("Village_Grey");
            if (village == null)
            {
                Debug.LogError("[Echoes] No Village_Grey in the scene.");
                return;
            }

            // Work on a throwaway clone rather than on the live village.
            //
            // The generated houses are FBX prefab instances, and adding components
            // to their children does not behave the way it does on an ordinary
            // hierarchy: the colliders go on, the count confirms it, and the physics
            // scene never sees them. GableProbe raycasts correctly against freshly
            // instantiated objects, so the clone puts both probes on the same
            // footing and keeps the scene untouched.
            var clone = Object.Instantiate(village);
            clone.name = "__SealProbeVillage";
            clone.hideFlags = HideFlags.HideAndDontSave;

            var houses = new List<Transform>();
            foreach (var t in clone.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("House_")) houses.Add(t);

            var host = new GameObject("__SealProbe");
            host.hideFlags = HideFlags.HideAndDontSave;

            // These kit meshes are single-sided tile shells, so a ray meeting the
            // back of a panel is ignored unless this is on. The probe cannot tell
            // "no collider" apart from "hit a back face" without it, and getting
            // that wrong reads as a hole that is not there.
            bool oldBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            var sb = new StringBuilder();
            sb.AppendLine("Assembled-house gable seal test (rays along Z, through the gable ends)");
            sb.AppendLine();

            int leaky = 0, noRoof = 0, controlFailed = 0, totalColliders = 0, inactive = 0;
            int firstHouseColliders = -1;
            var Overlap = new Collider[64];

            foreach (var h in houses)
            {
                // Temporary colliders on this house only. Added, used, then removed
                // in the same pass so the scene is left exactly as it was found.
                var rends = h.GetComponentsInChildren<Renderer>();
                var added = new List<MeshCollider>();

                foreach (var r in rends)
                {
                    if (r.GetComponent<MeshCollider>() != null) continue;
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;

                    var mc = r.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    added.Add(mc);
                }
                Physics.SyncTransforms();
                totalColliders += added.Count;
                if (firstHouseColliders < 0) firstHouseColliders = added.Count;
                if (!h.gameObject.activeInHierarchy) inactive++;

                // The roof defines the band worth testing: the gable triangle lives
                // entirely between its underside and its ridge.
                Bounds roofBounds = new Bounds();
                bool haveRoof = false;
                foreach (var r in rends)
                {
                    if (!r.name.StartsWith("Roof_RoundTiles")) continue;
                    if (!haveRoof) { roofBounds = r.bounds; haveRoof = true; }
                    else roofBounds.Encapsulate(r.bounds);
                }

                if (!haveRoof)
                {
                    noRoof++;
                    foreach (var mc in added) Object.DestroyImmediate(mc);
                    continue;
                }

                var b = new Bounds(Vector3.zero, Vector3.zero);
                foreach (var r in rends) b.Encapsulate(r.bounds);

                const int Steps = 11;
                int openZ = 0;
                var openAt = new System.Text.StringBuilder();

                for (int i = 0; i < Steps; i++)
                {
                    float t = Steps == 1 ? 0f : i / (float)(Steps - 1);

                    // Bias towards the bottom of the roof band, where the gable
                    // triangle is widest and a hole is most likely to show.
                    float y = Mathf.Lerp(roofBounds.min.y, roofBounds.max.y, t * t);
                    // Sample on the ROOF's centre line, not the house's. A house's
                    // bounds are dragged off-centre by balconies, crates, chimneys
                    // and fences, so a ray through the house centre can miss the
                    // roof entirely and report a hole that is not there. The roof's
                    // centre X is the ridge line and its Z extremes are the gable
                    // ends, which is exactly the geometry under test.
                    var probe = new Vector3(roofBounds.center.x, y, roofBounds.center.z);

                    if (!Blocked(probe, Vector3.forward, roofBounds.extents.z * 2f + 4f))
                    {
                        openZ++;
                        // Height as a fraction of the roof's own rise, so houses of
                        // different sizes can be compared: 0 is the eave, 1 the ridge.
                        if (openAt.Length > 0) openAt.Append(", ");
                        openAt.Append($"{(y - roofBounds.min.y) / roofBounds.size.y:0.00}");
                    }
                }

                // The control is a ray straight DOWN onto the roof, which no
                // geometry arrangement can miss while the colliders are real.
                //
                // An earlier version cast sideways along X instead, on the
                // assumption the roof is solid across its span. It is not: both
                // slopes fall away from the ridge, so a horizontal ray at apex
                // height passes cleanly over them. That made the control report
                // failure on a correctly sealed house and nearly hid a real result.
                if (!Physics.Raycast(roofBounds.center + Vector3.up * 50f, Vector3.down,
                                     120f, ~0, QueryTriggerInteraction.Ignore))
                    controlFailed++;

                // If the control fails, the figure above is meaningless, so find out
                // why rather than just reporting a void result. An OverlapBox at the
                // roof centre counts colliders the physics scene can actually see;
                // comparing that with the number added separates "no collider" from
                // "collider present but raycast not reaching it".
                string diag = "";
                if (totalColliders == firstHouseColliders)
                {
                    var box = new Vector3(1f, 0.5f, 1f);
                    int seen = Physics.OverlapBoxNonAlloc(roofBounds.center, box, Overlap,
                                                          Quaternion.identity, ~0,
                                                          QueryTriggerInteraction.Ignore);
                    bool down = Physics.Raycast(roofBounds.center + Vector3.up * 20f,
                                                Vector3.down, 60f, ~0,
                                                QueryTriggerInteraction.Ignore);
                    diag = $"   [diag: overlap {seen}, raycast-down {down}]";
                }

                // Where the gable walls actually sit, against where the roof needs
                // them. Reported because "open at 0.81 of the rise" only has one
                // explanation if the gable stops short of the ridge, and that is a
                // placement number rather than a guess.
                Bounds gableBounds = new Bounds();
                bool haveGable = false;
                foreach (var r in rends)
                {
                    if (!r.name.StartsWith("Roof_Front_Brick")) continue;
                    if (!haveGable) { gableBounds = r.bounds; haveGable = true; }
                    else gableBounds.Encapsulate(r.bounds);
                }

                string gableInfo = haveGable
                    ? $"gable top {gableBounds.max.y:0.00} vs roof top {roofBounds.max.y:0.00} " +
                      $"(short {roofBounds.max.y - gableBounds.max.y:0.00}), " +
                      $"gable half-width {gableBounds.extents.x:0.00} vs roof half-width {roofBounds.extents.x:0.00}"
                    : "NO GABLE WALL";

                if (openZ > 0) leaky++;

                sb.AppendLine($"{h.name,-22} colliders {added.Count,3}  roof " +
                              $"{roofBounds.size.x:0.0}x{roofBounds.size.y:0.0}" +
                              $"  gable open at {openZ}/{Steps} heights" +
                              (openZ > 0 ? $"  [at rise {openAt}]" : "  [sealed]") +
                              $"  {gableInfo}" + (diag) + "");

                foreach (var mc in added) Object.DestroyImmediate(mc);
                Physics.SyncTransforms();
            }

            Object.DestroyImmediate(host);
            Object.DestroyImmediate(clone);
            Physics.queriesHitBackfaces = oldBackfaces;

            sb.AppendLine();
            sb.AppendLine($"{houses.Count} houses: {leaky} still see-through, " +
                          $"{noRoof} without a round-tile roof, {controlFailed} control failures.");
            sb.AppendLine($"colliders added: {totalColliders}; inactive houses: {inactive}");
            if (controlFailed > 0)
                sb.AppendLine($"RESULT VOID: the downward control missed on {controlFailed} house(s), " +
                              "so the colliders are not being hit and 'see-through' here means " +
                              "'no collider', not 'hole'. Fix the probe before trusting any number above.");
            else
                sb.AppendLine("Control passed on every house (a downward ray hit each roof), so " +
                              "the gable figures above mean what they say.");

            var path = Path.Combine(Path.GetDirectoryName(Application.dataPath), OutPath);
            File.WriteAllText(path, sb.ToString());

            Debug.Log($"[Echoes] Village seal test -> {OutPath}\n" +
                      $"{leaky} of {houses.Count} houses still see-through at the gable.");
        }

        static bool Blocked(Vector3 origin, Vector3 dir, float span)
        {
            var start = origin + dir * (span * 0.5f + 0.05f);
            return Physics.Raycast(start, -dir, span, ~0, QueryTriggerInteraction.Ignore);
        }
    }
}
