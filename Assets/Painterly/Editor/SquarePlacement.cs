using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Finds somewhere in the village that a prop of a given radius can stand, and
    /// measures how much room each candidate actually has.
    ///
    /// The first fountain was placed at the MarketSquare transform's position and
    /// ended up inside House_0_0. Measuring showed why that was easy to get wrong:
    /// the square's pivot is not its open ground, the paving is a field of tiles, and
    /// the open ground is a ring around a building rather than a plain rectangle.
    /// "The middle of the square" and "the middle of the square's open space" are
    /// different points, and only the second one can hold a fountain.
    ///
    /// The first attempt to measure that used the transform names to decide what was
    /// ground — anything called Floor, Ground, Paving and so on. That was wrong in a
    /// way worth recording: the non-paving region came out twelve metres across when
    /// the building on it is eight, because the floor pieces that were not named the
    /// way the filter expected read as solid obstacles. A name is a guess about what
    /// an artist called something; a surface being flat and low is a fact about it.
    ///
    /// So ground is decided geometrically here. The ground level is taken from the
    /// low flat renderers under a root, and a sample counts as standing on ground if
    /// the raycast down to it lands at that height. An obstruction is then anything
    /// whose top rises more than <see cref="BlockerHeight"/> above that level while
    /// still reaching into the prop's own height band. Paving tiles pass both tests
    /// because they are five centimetres thick, and walls fail both because they are
    /// not, without either one ever having been named.
    ///
    /// Clearance is measured inside the prop's height band rather than in three
    /// dimensions. A roof twelve metres above House_0_0 occupies the same column of
    /// space as the ground beneath it, and counting it would make every spot in the
    /// square look equally bad.
    /// </summary>
    public static class SquarePlacement
    {
        /// <summary>Height of the volume a placed prop needs kept clear.</summary>
        public const float BandHeight = 1.6f;

        /// <summary>Height within that band at which clearance is measured.</summary>
        public const float BandEyeHeight = 0.7f;

        /// <summary>How far a surface may rise above ground and still be walked on
        /// rather than walked into. Paving is centimetres; a kerb is tens of them.</summary>
        public const float BlockerHeight = 0.35f;

        /// <summary>A renderer counts as ground if it is flatter than this.</summary>
        const float GroundMaxThickness = 0.7f;

        /// <summary>...and no wider than this on both axes, so trim strips are not
        /// mistaken for paving.</summary>
        const float GroundMinExtent = 0.8f;

        /// <summary>Tops above this are roofs or walls, never ground.</summary>
        const float GroundMaxTop = 2.0f;

        public sealed class Spot
        {
            public Vector3 Point;     // world position, resting on the ground surface
            public float Clearance;   // metres of clear radius available here
            public int Blockers;      // obstructions inside the radius
            public string Nearest;    // path of the closest obstruction
        }

        public sealed class Scan
        {
            public bool Ok;
            public string Why;
            public Bounds Area;
            public Bounds Ground;
            public float GroundY;
            public int Tiles;
            public int Samples;
            public List<Spot> Spots = new List<Spot>();
            public Spot Best;

            public float BestClearance => Best == null ? 0f : Best.Clearance;

            public IEnumerable<Spot> Fitting(float radius) =>
                Spots.Where(s => s.Clearance >= radius);
        }

        // ---------------------------------------------------------------- scanning

        /// <summary>Score every ground sample in a patch of the world for clearance.</summary>
        public static Scan Measure(Bounds area, float step, float queryRadius,
                                   IList<Transform> ignore = null)
        {
            var scan = new Scan { Area = area };

            float groundY;
            var ground = FindGround(area, out groundY);
            if (groundY < float.NegativeInfinity + 1f || groundY > float.PositiveInfinity - 1f)
            {
                scan.Why = "no ground surface found in the area";
                return scan;
            }

            scan.Ground = ground;
            scan.GroundY = groundY;
            scan.Ok = true;

            int cols = Mathf.CeilToInt(area.size.x / step) + 1;
            int rows = Mathf.CeilToInt(area.size.z / step) + 1;
            float x0 = area.min.x;
            float z0 = area.min.z;

            // Big enough that a sphere query in the middle of the village cannot
            // silently overflow and lose blockers, which would read as extra room.
            var buffer = new Collider[8192];

            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    float x = x0 + i * step;
                    float z = z0 + j * step;

                    if (!Physics.Raycast(new Vector3(x, area.max.y + 120f, z), Vector3.down,
                                         out var down, 500f, ~0, QueryTriggerInteraction.Ignore))
                        continue;

                    // Must be standing on ground: at ground height, and on something
                    // thin rather than on a roof.
                    if (Mathf.Abs(down.point.y - groundY) > 0.8f) continue;
                    var hb = down.collider.bounds;
                    if (hb.size.y > GroundMaxThickness) continue;
                    if (IsIgnored(down.collider.transform, ignore)) continue;

                    scan.Samples++;
                    var eye = new Vector3(x, down.point.y + BandEyeHeight, z);

                    int n = Physics.OverlapSphereNonAlloc(eye, queryRadius, buffer,
                                                          ~0, QueryTriggerInteraction.Ignore);
                    float nearest = queryRadius;
                    string nearestName = "<none>";
                    int blockers = 0;

                    for (int k = 0; k < n; k++)
                    {
                        var col = buffer[k];
                        if (col == null) continue;
                        if (IsIgnored(col.transform, ignore)) continue;

                        var cb = col.bounds;
                        // Must rise out of the floor to be something you cannot build
                        // through, and must reach into the prop's height band to
                        // matter. Roofs fail the second test; paving fails the first.
                        if (cb.max.y <= down.point.y + BlockerHeight) continue;
                        if (cb.min.y > down.point.y + BandHeight) continue;

                        blockers++;
                        float d = Vector3.Distance(eye, cb.ClosestPoint(eye));
                        if (d < nearest) { nearest = d; nearestName = PathOf(col.transform); }
                    }

                    var spot = new Spot
                    {
                        Point = new Vector3(x, down.point.y, z),
                        Clearance = blockers == 0 ? queryRadius : nearest,
                        Blockers = blockers,
                        Nearest = nearestName,
                    };
                    scan.Spots.Add(spot);

                    if (scan.Best == null || spot.Clearance > scan.Best.Clearance)
                        scan.Best = spot;
                }
            }

            if (scan.Spots.Count == 0) scan.Why = "no sample landed on ground";
            scan.Ok = scan.Spots.Count > 0;
            return scan;
        }

        /// <summary>The market square, framed by the paving tiles it is made of.</summary>
        public static Scan MeasureSquare(float step = 0.4f, float queryRadius = 9f)
        {
            var square = FindSquare();
            if (square == null)
                return new Scan { Why = "no MarketSquare in the scene" };

            float groundY;
            var ground = FindGround(TopBounds(square), out groundY, square.transform);
            if (ground.size == Vector3.zero)
                return new Scan { Why = "MarketSquare has no low flat ground pieces" };

            var area = ground;
            area.Expand(-0.01f);   // shrink a hair so the boundary is inside the paving

            // Only the character and any fountain already in the scene are ignored;
            // the square's own buildings are exactly what has to be measured against.
            return Measure(area, step, queryRadius, PlacementIgnores());
        }

        /// <summary>
        /// The whole village, at a coarser step, to find out whether the market
        /// square is even the best open ground in it.
        /// </summary>
        public static Scan MeasureVillage(float step = 1.6f, float queryRadius = 6f)
        {
            var root = GameObject.Find("Village_Grey") ?? GameObject.Find("Village");
            if (root == null)
                return new Scan { Why = "no Village_Grey in the scene" };

            // Clamped to the built-up area around the square. An unclamped scan
            // measured the whole kit's bounds and reported its best spot as
            // (-200, 0, -200) with six metres of clearance and nothing anywhere near
            // — which is not a place with room, it is a place with no village in it.
            // A clearance figure only means something where there was something that
            // could have obstructed, so the search has to stay inside the world it is
            // trying to describe.
            var square = FindSquare();
            var anchor = square != null ? square.transform.position : Vector3.zero;

            var area = TopBounds(root);
            area.size = new Vector3(Mathf.Min(area.size.x, 96f),
                                    area.size.y,
                                    Mathf.Min(area.size.z, 96f));
            area.center = new Vector3(anchor.x, area.center.y, anchor.z);

            return Measure(area, step, queryRadius, PlacementIgnores());
        }

        // ---------------------------------------------------------------- choosing

        /// <summary>
        /// Find a home for a prop of the given radius within a scan.
        ///
        /// Among the spots that fit, the one nearest the middle of the scanned area
        /// wins, not the one with the most clearance. A market fountain belongs in the
        /// middle of the market; a corner with an extra half metre is not a better
        /// answer to "where does the fountain go".
        /// </summary>
        public static bool TryFind(Scan scan, float requiredRadius,
                                   out Vector3 point, out float clearance, out string detail,
                                   Vector3? preferNear = null)
        {
            point = Vector3.zero;
            clearance = 0f;

            if (scan == null || !scan.Ok || scan.Spots.Count == 0)
            {
                detail = "scan failed: " + (scan == null ? "<null>" : scan.Why);
                return false;
            }

            var anchor = preferNear ?? new Vector3(scan.Area.center.x, scan.GroundY, scan.Area.center.z);

            var fitting = scan.Fitting(requiredRadius)
                              .OrderBy(s => Vector3.Distance(s.Point, anchor))
                              .ToList();

            if (fitting.Count == 0)
            {
                detail = "nothing fits radius " + requiredRadius.ToString("F2")
                         + " m; largest available is " + scan.BestClearance.ToString("F2")
                         + " m at " + (scan.Best != null ? scan.Best.Point.ToString() : "<none>");
                return false;
            }

            var chosen = fitting[0];
            point = chosen.Point;
            clearance = chosen.Clearance;
            detail = "chosen " + point.ToString() + " with " + clearance.ToString("F2")
                     + " m clearance, " + chosen.Blockers + " blocker(s); nearest "
                     + chosen.Nearest + "; " + fitting.Count + " of " + scan.Spots.Count
                     + " sampled spots fit";
            return true;
        }

        /// <summary>The largest radius that fits anywhere in a scan.</summary>
        public static float LargestFittingRadius(Scan scan) => scan.BestClearance;

        // ---------------------------------------------------------------- ground

        /// <summary>
        /// Ground level for a patch, taken from the renderers under it that are low
        /// and flat. Returns the union of them and, via <paramref name="groundY"/>,
        /// the height to compare raycast hits against.
        ///
        /// Renderers are gathered from <paramref name="within"/> when given, so the
        /// square is measured against its own paving rather than the whole village's,
        /// and from an area query otherwise.
        /// </summary>
        public static Bounds FindGround(Bounds area, out float groundY, Transform within = null)
        {
            var candidates = new List<Bounds>();

            if (within != null)
            {
                foreach (var r in within.GetComponentsInChildren<Renderer>(true))
                    if (IsGroundish(r.bounds, area)) candidates.Add(r.bounds);
            }
            else
            {
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
                {
                    if (!area.Intersects(r.bounds)) continue;
                    if (IsGroundish(r.bounds, area)) candidates.Add(r.bounds);
                }
            }

            if (candidates.Count == 0)
            {
                groundY = 0f;
                return new Bounds();
            }

            // The ground is the lowest surface that a plausible share of the pieces
            // sit on. Not the lowest outright, because a cellar floor or a pit would
            // drag every later comparison underground; and not the most common
            // outright, because a kit can ship more kerb strips than paving tiles and
            // the kerbs sit on top of it.
            var buckets = new Dictionary<int, float>();
            foreach (var b in candidates)
            {
                int key = Mathf.RoundToInt(b.max.y / 0.1f);
                if (!buckets.ContainsKey(key)) buckets[key] = 0f;
                buckets[key] += 1f;
            }

            float threshold = candidates.Count * 0.2f;
            int bestKey = buckets.Keys.OrderBy(k => k).First(k => buckets[k] >= threshold);
            groundY = bestKey * 0.1f;

            // Keep the pieces sitting at that level, and union them for the extent.
            var union = new Bounds();
            bool any = false;
            foreach (var b in candidates)
            {
                if (Mathf.Abs(b.max.y - groundY) > 0.35f) continue;
                if (any) union.Encapsulate(b); else { union = b; any = true; }
            }

            if (!any)
            {
                var pick = candidates[0];
                groundY = pick.max.y;
                union = pick;
            }

            return union;
        }

        static bool IsGroundish(Bounds b, Bounds area)
        {
            if (b.size.x < GroundMinExtent || b.size.z < GroundMinExtent) return false;
            if (b.size.y > GroundMaxThickness) return false;
            if (b.max.y > GroundMaxTop) return false;
            return area.Intersects(b);
        }

        // ---------------------------------------------------------------- helpers

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

        public static GameObject FindSquare() =>
            GameObject.Find("Village_Grey/MarketSquare") ?? GameObject.Find("MarketSquare");

        public static GameObject FindVillage() =>
            GameObject.Find("Village_Grey") ?? GameObject.Find("Village");

        /// <summary>World AABB of everything under a transform.</summary>
        public static Bounds TopBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        public static string PathOf(Transform t)
        {
            if (t == null) return "<none>";
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        /// <summary>
        /// Roots whose geometry is not part of the village and must not be counted as
        /// an obstruction: the character stands somewhere, and the fountain being
        /// placed is itself in the scene while it is being searched for a home.
        /// </summary>
        public static List<Transform> PlacementIgnores()
        {
            var list = new List<Transform>();
            foreach (var name in new[] { "Fountain", "Ari", "Painter", "Player" })
            {
                var go = GameObject.Find(name);
                if (go != null) list.Add(go.transform);
            }
            return list;
        }
    }
}
