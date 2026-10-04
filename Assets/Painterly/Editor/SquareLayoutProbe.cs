using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Maps the market square so the fountain can be put somewhere it can be seen.
    ///
    /// The fountain was built at the MarketSquare transform's position and ended up
    /// inside House_0_0 — the square's pivot sits under a building, not in the open.
    /// The right place is the paving, and it has to be found by measurement: the
    /// open area is whatever the floor pieces cover once the buildings standing on
    /// them have been subtracted, and "once subtracted" is a collision query, not a
    /// reading of the hierarchy.
    ///
    /// So this walks the square's children to see what is actually in it, unions the
    /// floor pieces to get the paved extent, then grid-searches that extent for the
    /// point with the most clear space around it. Clearance is measured to real
    /// collider geometry with the paving and ground filtered out — otherwise every
    /// candidate reports a floor directly beneath it and the ranking is meaningless.
    /// </summary>
    public static class SquareLayoutProbe
    {
        const string Report = "Temp/square_layout.txt";

        // A little more than the fountain's own radius, so a spot that passes has
        // room for the lantern holders and the fallen debris as well.
        const float FountainClearRadius = 3.5f;
        const float FountainHeight = 1.6f;

        static readonly string[] FloorHints =
        {
            "floor", "ground", "paving", "border", "stairs", "step", "holecover", "road", "path",
        };

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] market square layout");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sb.AppendLine("STOPPED: exit play mode first (Ctrl+P).");
                Finish(sb);
                return;
            }

            var square = GameObject.Find("Village_Grey/MarketSquare")
                         ?? GameObject.Find("MarketSquare")
                         ?? GameObject.Find("Square");

            if (square == null)
            {
                sb.AppendLine("FATAL: no MarketSquare found.");
                Finish(sb);
                return;
            }

            sb.AppendLine();
            sb.AppendLine("square        : " + PathOf(square.transform));
            sb.AppendLine("square pivot  : " + square.transform.position);
            sb.AppendLine();

            // --- what is in the square -----------------------------------------
            var kids = square.GetComponentsInChildren<Renderer>(true);
            sb.AppendLine("children of the square (" + kids.Length + " renderers):");
            sb.AppendLine("   " + "name".PadRight(42) + "size (x,y,z)".PadRight(26) + "centre".PadRight(30) + "floor?");
            foreach (var r in kids
                        .OrderByDescending(x => x.bounds.size.x * x.bounds.size.z)
                        .Take(40))
            {
                var s = r.bounds.size;
                sb.AppendLine("   " + PathOf(r.transform).PadRight(42)
                    + string.Format("({0,5:F1},{1,5:F1},{2,5:F1})", s.x, s.y, s.z).PadRight(26)
                    + string.Format("({0,6:F1},{1,6:F1},{2,6:F1})", r.bounds.center.x, r.bounds.center.y, r.bounds.center.z).PadRight(30)
                    + (LooksLikeFloor(r.transform) ? "yes" : ""));
            }

            // --- the paved extent ----------------------------------------------
            bool any = false;
            var floor = new Bounds();
            int floorCount = 0;
            foreach (var r in kids)
            {
                if (!LooksLikeFloor(r.transform)) continue;
                // Only pieces that are actually walk-on sized; trims and kerbs would
                // otherwise drag the union out to the village boundary.
                if (r.bounds.size.x < 3f || r.bounds.size.z < 3f) continue;
                if (any) floor.Encapsulate(r.bounds); else { floor = r.bounds; any = true; }
                floorCount++;
            }

            sb.AppendLine();
            if (!any)
            {
                sb.AppendLine("no floor pieces found under the square; cannot place by paving.");
                Finish(sb);
                return;
            }

            sb.AppendLine("paved area from " + floorCount + " floor piece(s):");
            sb.AppendLine("   centre " + floor.center);
            sb.AppendLine("   size   " + floor.size);
            sb.AppendLine("   min    " + floor.min);
            sb.AppendLine("   max    " + floor.max);
            sb.AppendLine("   usable in X/Z after the fountain's " + FountainClearRadius + " m radius is kept off the edge: "
                + (floor.size.x - 2 * FountainClearRadius).ToString("F2") + " x "
                + (floor.size.z - 2 * FountainClearRadius).ToString("F2") + " m");
            sb.AppendLine();

            // --- grid search for the clearest spot -----------------------------
            float step = 0.5f;
            var candidates = new List<(Vector3 p, float clearance, int blockers, string nearest)>();

            for (float x = floor.min.x + FountainClearRadius; x <= floor.max.x - FountainClearRadius; x += step)
            for (float z = floor.min.z + FountainClearRadius; z <= floor.max.z - FountainClearRadius; z += step)
            {
                var probe = new Vector3(x, floor.center.y + FountainHeight, z);

                // Must stand on paving, not on a roof.
                if (!Physics.Raycast(probe + Vector3.up * 60f, Vector3.down, out var down, 200f,
                                     ~0, QueryTriggerInteraction.Ignore))
                    continue;
                if (!LooksLikeFloor(down.collider.transform)) continue;

                // Nothing solid may intrude into the volume the fountain needs.
                var overlaps = Physics.OverlapSphere(probe, FountainClearRadius, ~0, QueryTriggerInteraction.Ignore);
                float nearest = float.MaxValue;
                string nearestName = "<none>";
                int blockers = 0;
                foreach (var col in overlaps)
                {
                    if (col == null) continue;
                    if (LooksLikeFloor(col.transform)) continue;
                    if (col.transform.IsChildOf(square.transform) == false && col.name == "Ground") continue;

                    blockers++;
                    // Closest point on the intruding collider to the candidate.
                    float d = Vector3.Distance(probe, col.bounds.ClosestPoint(probe));
                    if (d < nearest) { nearest = d; nearestName = PathOf(col.transform); }
                }

                float clearance = blockers == 0 ? FountainClearRadius : nearest;
                candidates.Add((new Vector3(x, down.point.y, z), clearance, blockers, nearestName));
            }

            sb.AppendLine("grid candidates evaluated : " + candidates.Count);
            if (candidates.Count == 0)
            {
                sb.AppendLine("NONE. The paved area is too small for a " + (2 * FountainClearRadius) + " m fountain.");
                Finish(sb);
                return;
            }

            sb.AppendLine();
            sb.AppendLine("best spots by clearance (blockers = non-paving colliders inside the radius):");
            sb.AppendLine("   " + "position".PadRight(30) + "clearance".PadRight(12) + "blockers".PadRight(10) + "nearest blocker");
            foreach (var c in candidates.OrderByDescending(c => c.clearance).Take(12))
            {
                sb.AppendLine("   "
                    + string.Format("({0,6:F2},{1,6:F2},{2,6:F2})", c.p.x, c.p.y, c.p.z).PadRight(30)
                    + c.clearance.ToString("F2").PadRight(12)
                    + c.blockers.ToString().PadRight(10)
                    + c.nearest);
            }

            int clearSpots = candidates.Count(c => c.blockers == 0);
            sb.AppendLine();
            sb.AppendLine("spots with ZERO blockers : " + clearSpots + " of " + candidates.Count);
            var best = candidates.OrderByDescending(c => c.clearance).First();
            sb.AppendLine("best overall             : " + best.p + "  clearance " + best.clearance.ToString("F2")
                + " m, blockers " + best.blockers);

            Finish(sb);
        }

        static bool LooksLikeFloor(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
            {
                string n = p.name.ToLowerInvariant();
                if (FloorHints.Any(h => n.Contains(h))) return true;
            }
            return false;
        }

        static string PathOf(Transform t)
        {
            if (t == null) return "<none>";
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
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
