using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Moves House_0_0 out of the middle of the market square.
    ///
    /// The square measured as 16 x 16 m of paving with an 8 x 7.2 m house standing
    /// in the middle of it. That is not a market square, it is a yard with a house
    /// in it: the median amount of clear ground anywhere on the paving was 1.43 m,
    /// and the only genuinely open ground was where the two roads leave. A fountain
    /// could only be put in a road mouth, and only by blocking it.
    ///
    /// So the house has to go. The interesting part is not the move, it is deciding
    /// where to, which is why this searches and measures rather than picking a
    /// coordinate. For every candidate the house's own volume is tested against
    /// every obstruction in the village, and the margin is measured as the real gap
    /// left around it.
    ///
    /// Two things are filtered out of the obstruction test, and both matter:
    ///
    ///   - The house's own colliders. Obviously.
    ///   - Ground. A building standing on paving overlaps the paving, and treating
    ///     the floor as an obstruction would make every spot in the village
    ///     unusable. Only things that rise out of the ground, and rise far enough to
    ///     be in the house's body, count.
    ///
    /// The yaw is tried at each position before giving up, because a rectangular
    /// plan turns through ninety degrees: a house that will not fit one way round
    /// usually fits the other way round.
    /// </summary>
    public static class HouseRelocation
    {
        const string Report = "Temp/house_relocation.txt";
        const string HousePath = "Village_Grey/MarketSquare/House_0_0";

        /// <summary>Metres of clear ground wanted around the house, so it is not
        /// merely touching its neighbours.</summary>
        const float MinMargin = 0.75f;

        /// <summary>Distance from the square's centre. The paving reaches 8 m, so
        /// this keeps the house and its overhang clear of the square itself.</summary>
        const float MinRadius = 13f;

        const float MaxRadius = 46f;
        const float Step = 1.0f;

        /// <summary>How far the ground at a candidate may differ from the square's,
        /// so the house is not lifted onto a terrace or dropped into a hollow.</summary>
        const float GroundTolerance = 0.6f;

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] moving the house out of the market square");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sb.AppendLine("STOPPED: exit play mode first (Ctrl+P).");
                Finish(sb);
                return;
            }

            var house = FindHouse();
            if (house == null)
            {
                sb.AppendLine("FATAL: no GameObject named House_0_0 in the scene.");
                Finish(sb);
                return;
            }

            var square = SquarePlacement.FindSquare();
            if (square == null)
            {
                sb.AppendLine("FATAL: no MarketSquare in the scene.");
                Finish(sb);
                return;
            }

            var tr = house.transform;
            var squareCentre = square.transform.position;
            // The house is no longer part of the square, so stop calling it part of
            // the square. Left parented under MarketSquare, it makes the square lie
            // about its own size: the extent is the union of everything beneath it,
            // and a house standing 25 m away stretched the measured square to 35 m
            // deep — which then sent the fountain to the middle of that instead of
            // the middle of the square. Reparent before measuring anything, or the
            // measurement is taken against a shape that does not exist.
            var village = GameObject.Find("Village_Grey");
            bool reparented = false;
            if (village != null && tr.parent != village.transform)
            {
                tr.SetParent(village.transform, true);   // true: keep the world position
                reparented = true;
            }

            float squareGroundY;
            SquarePlacement.FindGround(SquarePlacement.TopBounds(square), out squareGroundY,
                                       square.transform);

            var local = LocalAabb(tr);
            float houseHeight = local.size.y;
            float houseRadius = Mathf.Sqrt(local.extents.x * local.extents.x
                                          + local.extents.z * local.extents.z);
            var baseYaw = tr.eulerAngles.y;

            var worldNow = WorldAabb(tr, local);
            var before = SquarePlacement.MeasureSquare(0.4f, 9f);

            sb.AppendLine();
            sb.AppendLine("--- the house ---");
            sb.AppendLine("path              : " + SquarePlacement.PathOf(tr));
            sb.AppendLine("position          : " + tr.position);
            sb.AppendLine("yaw               : " + baseYaw.ToString("F1") + " deg");
            sb.AppendLine("size (x,y,z)      : " + worldNow.size);
            sb.AppendLine("local half extents: " + local.extents);
            sb.AppendLine("circumradius      : " + houseRadius.ToString("F2") + " m");
            sb.AppendLine("ground under square: y = " + squareGroundY.ToString("F3"));
            if (reparented)
                sb.AppendLine("reparented        : out of MarketSquare, now under Village_Grey");

            sb.AppendLine();
            sb.AppendLine("--- the square, before ---");
            sb.AppendLine("clearance median  : " + Median(before).ToString("F2") + " m");
            sb.AppendLine("clearance max     : " + before.BestClearance.ToString("F2") + " m");
            sb.AppendLine("spots fitting r=3.4 m (the fountain): " + before.Fitting(3.4f).Count());

            // --- search ----------------------------------------------------------
            var ignore = SquarePlacement.PlacementIgnores();
            ignore.Add(tr);                       // the house does not block itself
            var buffer = new Collider[8192];

            // Obstruction boxes gathered once, then tested with plain bounds maths.
            // Six thousand candidates times four rotations times a physics overlap
            // query each is minutes of work; six thousand times four rectangle tests
            // against a list is a fraction of a second. The physics engine is still
            // used afterwards to verify the winner, so the fast path is the one that
            // has to be trusted least — and it is not the one that gets to decide.
            var blockers = CollectBlockers(ignore, squareGroundY, squareGroundY + houseHeight);
            sb.AppendLine("obstruction boxes  : " + blockers.Count);

            int tested = 0, onGround = 0, clear = 0;
            var viable = new List<Candidate>();

            for (float x = -MaxRadius; x <= MaxRadius; x += Step)
            {
                for (float z = -MaxRadius; z <= MaxRadius; z += Step)
                {
                    var fromCentre = new Vector2(x - squareCentre.x, z - squareCentre.z);
                    float dist = fromCentre.magnitude;
                    if (dist < MinRadius || dist > MaxRadius) continue;

                    tested++;
                    if (!TryGround(x, z, squareGroundY, ignore, out var groundY)) continue;
                    onGround++;

                    float bestYaw = float.NaN;
                    Bounds bestAabb = new Bounds();
                    bool found = false;

                    for (int turn = 0; turn < 4; turn++)
                    {
                        float yaw = baseYaw + 90f * turn;
                        var pos = new Vector3(x, groundY - local.min.y, z);
                        var aabb = WorldAabbAt(pos, yaw, local);
                        if (Blocked(aabb, blockers)) continue;
                        bestYaw = yaw;
                        bestAabb = aabb;
                        found = true;
                        break;      // a rectangular plan turns: take the first that fits
                    }

                    if (!found) continue;
                    clear++;

                    float margin = Margin(bestAabb.center, houseRadius, groundY,
                                          groundY + houseHeight, ignore, buffer);
                    viable.Add(new Candidate
                    {
                        X = x,
                        Z = z,
                        GroundY = groundY,
                        Yaw = bestYaw,
                        Margin = margin,
                        DistanceFromSquare = dist,
                    });
                }
            }

            sb.AppendLine();
            sb.AppendLine("--- search ---");
            sb.AppendLine("positions tested  : " + tested);
            sb.AppendLine("on real ground     : " + onGround);
            sb.AppendLine("no obstructions    : " + clear);

            if (viable.Count == 0)
            {
                sb.AppendLine();
                sb.AppendLine("FATAL: nowhere in the village fits this house with any margin.");
                sb.AppendLine("Nothing has been moved. The house is still where it was.");
                Finish(sb);
                return;
            }

            // Enough gap to be a place rather than a collision, and then as close to
            // the square as possible so the village stays one settlement.
            var roomy = viable.Where(c => c.Margin >= MinMargin).ToList();
            bool hadMargin = roomy.Count > 0;
            if (!hadMargin) roomy = viable;

            // Nearest first, and only then the roomiest. Ranking by margin alone
            // picks the emptiest place in the world, which for a village is the
            // middle of a field: a house with four metres of clearance on every
            // side and no neighbours is not a house in a village, it is a house in
            // the countryside. Margin is a floor to clear, not a prize to maximise.
            var chosen = roomy.OrderBy(c => c.DistanceFromSquare)
                              .ThenByDescending(c => c.Margin)
                              .First();

            sb.AppendLine("with " + MinMargin.ToString("F2") + " m margin: "
                          + roomy.Count + (hadMargin ? "" : "   (NONE — best available is only "
                               + viable.Max(c => c.Margin).ToString("F2") + " m)"));

            sb.AppendLine();
            sb.AppendLine("--- best destinations ---");
            foreach (var c in roomy.OrderBy(c => c.DistanceFromSquare).Take(12))
                sb.AppendLine("   at " + new Vector3(c.X, c.GroundY, c.Z).ToString().PadRight(30)
                              + " margin " + c.Margin.ToString("F2").PadLeft(5) + " m"
                              + "   yaw " + c.Yaw.ToString("F0").PadLeft(4)
                              + "   " + c.DistanceFromSquare.ToString("F1").PadLeft(5) + " m from square"
                              + (ReferenceEquals(c, chosen) ? "   <== chosen" : ""));

            // --- move ------------------------------------------------------------
            var oldPos = tr.position;
            var oldYaw = tr.eulerAngles.y;
            tr.position = new Vector3(chosen.X, chosen.GroundY - local.min.y, chosen.Z);
            tr.rotation = Quaternion.Euler(0f, chosen.Yaw, 0f);
            Physics.SyncTransforms();

            int after = CountObstructions(WorldAabb(tr, local), chosen.GroundY,
                                          chosen.GroundY + houseHeight, ignore, buffer);
            float afterMargin = Margin(WorldAabb(tr, local).center, houseRadius,
                                       chosen.GroundY, chosen.GroundY + houseHeight,
                                       ignore, buffer);

            // --- prove the square opened up --------------------------------------
            var scan = SquarePlacement.MeasureSquare(0.4f, 9f);
            float centreClearance = -1f;
            if (scan.Ok)
            {
                float best = 0f;
                foreach (var s in scan.Spots)
                {
                    float d = Vector3.Distance(
                        new Vector2(s.Point.x - squareCentre.x, s.Point.z - squareCentre.z),
                        Vector2.zero);
                    if (d > 2f) continue;                 // the middle, not the mouths
                    if (s.Clearance > best) best = s.Clearance;
                }
                centreClearance = best;
            }

            sb.AppendLine();
            sb.AppendLine("--- moved ---");
            sb.AppendLine("from              : " + oldPos + " yaw " + oldYaw.ToString("F1"));
            sb.AppendLine("to                : " + tr.position + " yaw " + chosen.Yaw.ToString("F1"));
            sb.AppendLine("obstructions now  : " + after + "   (must be 0)");
            sb.AppendLine("margin now        : " + afterMargin.ToString("F2") + " m");

            sb.AppendLine();
            sb.AppendLine("--- the square, after ---");
            sb.AppendLine("clearance median  : " + Median(scan).ToString("F2") + " m   (was "
                          + Median(before).ToString("F2") + ")");
            sb.AppendLine("best clearance within 2 m of the centre: "
                          + centreClearance.ToString("F2") + " m   (was "
                          + CentreClearance(before, squareCentre).ToString("F2") + " m)");
            sb.AppendLine("clearance max     : " + scan.BestClearance.ToString("F2") + " m");
            sb.AppendLine("spots fitting r=3.4 m (the fountain): " + scan.Fitting(3.4f).Count()
                          + "   (was " + before.Fitting(3.4f).Count() + ")");

            if (after == 0)
            {
                EditorSceneManager.MarkSceneDirty(tr.gameObject.scene);
                sb.AppendLine();
                sb.AppendLine("verified clear.");

                // The report is written before the scene is saved, not after. Saving
                // can start a domain reload, and a run that lost its report at the
                // last line cannot tell a move that happened from a move that did
                // not — which is the one thing this tool exists to establish.
                Finish(sb);

                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    EditorSceneManager.SaveScene(tr.gameObject.scene);
                return;
            }

            tr.position = oldPos;
            tr.rotation = Quaternion.Euler(0f, oldYaw, 0f);
            Physics.SyncTransforms();
            sb.AppendLine();
            sb.AppendLine("REVERTED: the move did not verify clean, so the house was put");
            sb.AppendLine("back where it started.");

            Finish(sb);
        }

        sealed class Candidate
        {
            public float X, Z, GroundY, Yaw, Margin, DistanceFromSquare;
        }

        /// <summary>
        /// The house, found by name rather than by its old path.
        ///
        /// The old path only held while the house was a child of the square, and the
        /// first thing this tool does is take it out of there. A tool that cannot be
        /// run twice without editing itself is a tool that gets run once, and then
        /// the second run — the one after something has gone wrong — is the run you
        /// cannot do.
        /// </summary>
        static GameObject FindHouse()
        {
            var direct = GameObject.Find(HousePath);
            if (direct != null) return direct;

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name == "House_0_0") return t.gameObject;

            return null;
        }

        /// <summary>
        /// Every box in the village a building would have to fit around: things that
        /// stand up out of the ground, reach into the building's body, and are not
        /// the building itself or the character or the fountain.
        /// </summary>
        static List<Bounds> CollectBlockers(IList<Transform> ignore, float groundY, float topY)
        {
            var list = new List<Bounds>();
            foreach (var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))
            {
                if (c == null) continue;
                if (IsIgnored(c.transform, ignore)) continue;
                var cb = c.bounds;
                if (cb.max.y <= groundY + 0.35f) continue;   // it is the floor
                if (cb.min.y >= topY) continue;              // it is above the roof
                list.Add(cb);
            }
            return list;
        }

        /// <summary>
        /// Does any obstruction box meet this building box? Plain rectangle tests
        /// against the list gathered once, because this runs a few thousand times.
        /// </summary>
        static bool Blocked(Bounds box, List<Bounds> blockers)
        {
            for (int i = 0; i < blockers.Count; i++)
                if (box.Intersects(blockers[i])) return true;
            return false;
        }

        // ------------------------------------------------------------------ measurement

        /// <summary>
        /// The house's own axis-aligned box, in its own frame.
        ///
        /// Corners are transformed rather than the centre and extents being reused,
        /// because for a rotated object those are not the same box, and the whole
        /// point of this tool is to avoid finding out that the hard way.
        /// </summary>
        static Bounds LocalAabb(Transform root)
        {
            var b = new Bounds();
            bool any = false;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var wb = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? wb.min.x : wb.max.x,
                        (i & 2) == 0 ? wb.min.y : wb.max.y,
                        (i & 4) == 0 ? wb.min.z : wb.max.z);
                    var p = root.InverseTransformPoint(corner);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                    else b.Encapsulate(p);
                }
            }
            return b;
        }

        static Bounds WorldAabb(Transform tr, Bounds local) => WorldAabbAt(tr.position, tr.eulerAngles.y, local);

        static Bounds WorldAabbAt(Vector3 pos, float yaw, Bounds local)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            var b = new Bounds();
            bool any = false;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? local.min.x : local.max.x,
                    (i & 2) == 0 ? local.min.y : local.max.y,
                    (i & 4) == 0 ? local.min.z : local.max.z);
                var p = pos + rot * corner;
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                else b.Encapsulate(p);
            }
            return b;
        }

        /// <summary>
        /// Ground at a candidate, or false. Requires a surface at the square's level
        /// and requires it to be ground — thin and low — so the search cannot put the
        /// house on a rooftop.
        ///
        /// The house's own colliders are skipped. Without that, a second run would
        /// refuse the spot the house is already standing on, because the raycast
        /// comes down onto its fifteen-metre roof and not onto the paving, and the
        /// tool would walk the house further away on every run instead of settling.
        /// </summary>
        static bool TryGround(float x, float z, float squareGroundY,
                              IList<Transform> ignore, out float groundY)
        {
            groundY = 0f;
            var origin = new Vector3(x, squareGroundY + 80f, z);

            // Highest thing first, so the house is encountered before the paving it
            // is standing on rather than instead of it.
            var hits = Physics.RaycastAll(origin, Vector3.down, 400f, ~0,
                                          QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (p, q) => q.distance.CompareTo(p.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                if (IsIgnored(hit.collider.transform, ignore)) continue;

                if (Mathf.Abs(hit.point.y - squareGroundY) > GroundTolerance) return false;

                var hb = hit.collider.bounds;
                if (hb.size.y > 0.7f) return false;
                if (hb.max.y > squareGroundY + 1.2f) return false;

                groundY = hit.point.y;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Things standing in the house's way: rising out of the ground, reaching
        /// into the house's body, and not the ground itself.
        /// </summary>
        static int CountObstructions(Bounds box, float groundY, float topY,
                                     IList<Transform> ignore, Collider[] buffer)
        {
            int n = Physics.OverlapBoxNonAlloc(box.center, box.extents, buffer,
                                               Quaternion.identity, ~0,
                                               QueryTriggerInteraction.Ignore);
            int count = 0;
            for (int i = 0; i < n; i++)
            {
                var col = buffer[i];
                if (col == null) continue;
                if (IsIgnored(col.transform, ignore)) continue;
                var cb = col.bounds;
                if (cb.max.y <= groundY + 0.35f) continue;   // it is the floor
                if (cb.min.y >= topY) continue;              // it is above the roof
                count++;
            }
            return count;
        }

        /// <summary>The real gap left around the house, not a nominal one.</summary>
        static float Margin(Vector3 centre, float radius, float groundY, float topY,
                            IList<Transform> ignore, Collider[] buffer)
        {
            int n = Physics.OverlapSphereNonAlloc(centre, radius + 8f, buffer, ~0,
                                                  QueryTriggerInteraction.Ignore);
            float best = 8f;
            for (int i = 0; i < n; i++)
            {
                var col = buffer[i];
                if (col == null) continue;
                if (IsIgnored(col.transform, ignore)) continue;
                var cb = col.bounds;
                if (cb.max.y <= groundY + 0.35f) continue;
                if (cb.min.y >= topY) continue;
                float d = Vector3.Distance(cb.ClosestPoint(centre), centre) - radius;
                if (d < best) best = d;
            }
            return best;
        }

        static bool IsIgnored(Transform t, IList<Transform> ignore)
        {
            if (ignore == null) return false;
            for (int i = 0; i < ignore.Count; i++)
            {
                var root = ignore[i];
                if (root == null) continue;
                if (t == root || t.IsChildOf(root)) return true;
            }
            return false;
        }

        static float Median(SquarePlacement.Scan scan)
        {
            if (scan == null || !scan.Ok || scan.Spots.Count == 0) return 0f;
            var xs = scan.Spots.Select(s => s.Clearance).OrderBy(v => v).ToList();
            return xs[xs.Count / 2];
        }

        /// <summary>Best clearance in the middle of the square, ignoring the mouths.</summary>
        static float CentreClearance(SquarePlacement.Scan scan, Vector3 centre)
        {
            if (scan == null || !scan.Ok) return 0f;
            float best = 0f;
            foreach (var s in scan.Spots)
            {
                float d = Vector3.Distance(
                    new Vector2(s.Point.x - centre.x, s.Point.z - centre.z), Vector2.zero);
                if (d > 2f) continue;
                if (s.Clearance > best) best = s.Clearance;
            }
            return best;
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
