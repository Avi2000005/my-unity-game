using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Global namespace, UnityEngine only, so run_script can compile and run it
// independently of the project assembly.
public static class WidthMap
{
    // Suffixed: a const and a method may not share a name, and CS0102 for a
    // collision between a report path and the method that reports reads as
    // though the file were already broken in a more interesting way.
    const string ReportPath = "Temp/width_map.txt";

    // --- sampling ---------------------------------------------------------------

    const float Span = 36f;       // +/- metres
    const float Step = 0.5f;      // grid spacing
    const float MaxReach = 8f;    // how far a width ray looks
    const float Chest = 1.10f;    // height above the floor the width is taken at
    const float From = 30f;       // ground probe start, above the tallest roof
    const float Down = 60f;       // ground probe depth

    /// <summary>
    /// Maps where this village is narrow.
    ///
    /// Three beats in Level 1 all need a constricted space and none of them can
    /// be built until it is known that one exists. Beat 3 wants a gully narrow
    /// enough that the camera has to follow rather than lead. Beat 4 wants a
    /// crawlspace Ari is too big for and Mono is not. Beat 5.5 is an alley
    /// gauntlet and is the level's difficulty spike, so it wants the tightest
    /// and longest run available. Picking those by eye is guesswork; this
    /// measures the whole village and reports corridors with their dimensions.
    ///
    /// Width at each cell is the narrower of the two axis extents. A lane
    /// running north-south is 1 m across the east-west rays and tens of metres
    /// along the north-south ones, so the axis extent that is short is the
    /// width and the other is the length — and taking the minimum of the two
    /// gets both right without first having to know which way the lane runs.
    /// The map is coarse by that measure, so every corridor it finds is then
    /// re-measured properly, perpendicular to the direction it actually runs,
    /// and it is that second number, not the map's, which is reported as the
    /// corridor's width.
    /// </summary>
    public static void Run()
    {
        var sb = new StringBuilder();

        try { Body(sb); }
        catch (Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] width map\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportPath),
                           sb.ToString());
    }

    // --- one cell ----------------------------------------------------------------

    struct Cell
    {
        public int X, Z;
        public float Width;    // narrow axis extent, metres
        public float Ground;
        public bool Void;

        /// <summary>
        /// Visited, so the region grow visits each cell once. On the cell and
        /// not on the region, because the grow is what has to not revisit, and
        /// a flag on the region would let a grow walk straight back into its
        /// own output and run until the queue emptied.
        /// </summary>
        public bool Seen;
    }

    static void Body(StringBuilder sb)
    {
        Physics.SyncTransforms();

        int n = Mathf.RoundToInt(Span * 2f / Step) + 1;

        sb.AppendLine("WHERE THIS VILLAGE IS NARROW");
        sb.AppendLine("grid +/- " + Span + " m at " + Step + " m = " + n + " x " + n +
                      " cells, " + (n * n) + " samples");
        sb.AppendLine("width taken " + Chest.ToString("0.00") + " m above each cell's " +
                      "own floor, rays reaching " + MaxReach.ToString("0") + " m");
        sb.AppendLine();

        var grid = new Cell[n, n];
        int voidCells = 0, cells = 0;

        for (int xi = 0; xi < n; xi++)
        {
            for (int zi = 0; zi < n; zi++)
            {
                var at = new Vector3(-Span + xi * Step, 0f, -Span + zi * Step);

                var c = new Cell { X = xi, Z = zi };

                if (!Floor(at, out c.Ground)) { c.Void = true; voidCells++; }
                else
                {
                    c.Width = WidthAt(at, c.Ground);
                    cells++;
                }

                grid[xi, zi] = c;
            }
        }

        sb.AppendLine("cells with a floor: " + cells + "   with nothing: " + voidCells);
        sb.AppendLine();

        // --- the map -------------------------------------------------------------

        sb.AppendLine("--- the map ---");
        sb.AppendLine("  x runs left to right from -" + Span + " to +" + Span +
                      ", z runs top to bottom from +" + Span + " to -" + Span + " +.");
        sb.AppendLine("  ' ' no floor   # under 0.6   = 0.6-1.2   - 1.2-2.5" +
                      "   + 2.5-4   o 4-6   O 6-8   # beyond 8");
        sb.AppendLine();

        for (int zi = n - 1; zi >= 0; zi--)
        {
            var line = new StringBuilder(n);
            for (int xi = 0; xi < n; xi++) line.Append(Band(grid[xi, zi]));
            sb.AppendLine("  " + line.ToString());
        }

        sb.AppendLine();

        // --- corridors -----------------------------------------------------------

        var corridors = Corridors(grid, n);

        sb.AppendLine("--- connected narrow ground (0.9 m to 6.0 m) ---");
        sb.AppendLine("cells of lane-like ground, in regions of at least 6 cells " +
                      "(3 square metres, which is a doorway's worth of floor): " +
                      corridors.Count);

        if (corridors.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("NOT ONE. There is no narrow ground in this village at all.");
            sb.AppendLine("Beat 3's gully, Beat 4's crawlspace and Beat 5.5's alley all");
            sb.AppendLine("have to be BUILT, and the map above is where to put them:");
            sb.AppendLine("the open 'O' ground is the square, and a wall run along a");
            sb.AppendLine("street edge would turn it into any of the three.");
            return;
        }

        for (int i = 0; i < corridors.Count; i++)
        {
            var c = corridors[i];
            sb.AppendLine();
            sb.AppendLine("region " + (i + 1) + " of " + corridors.Count + ":");
            Report(sb, grid, c);
        }
    }

    // --- measuring ---------------------------------------------------------------

    /// <summary>
    /// The floor under a point: the lowest thing a downward ray finds, so a roof
    /// or a prop overhead is not mistaken for the ground. Measured in
    /// GroundProfile across 7 225 points, where taking the topmost hit instead
    /// returned a statue plinth for the square, a decorative vine for Ari and a
    /// roof 15 m up for the house.
    /// </summary>
    static bool Floor(Vector3 at, out float y)
    {
        y = 0f;

        var hits = Physics.RaycastAll(at + Vector3.up * From, Vector3.down, Down, ~0,
                                      QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return false;

        float lowest = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null) continue;
            if (hits[i].point.y < lowest) lowest = hits[i].point.y;
        }

        if (lowest == float.MaxValue) return false;

        y = lowest;
        return true;
    }

    /// <summary>
    /// How far the ray travels before it meets something, or MaxReach if it
    /// meets nothing, which is the same as a passage at least that wide.
    /// </summary>
    static float WallDistance(Vector3 from, Vector3 dir)
    {
        return Physics.Raycast(from, dir, out RaycastHit hit, MaxReach, ~0,
                               QueryTriggerInteraction.Ignore)
            ? hit.distance
            : MaxReach;
    }

    /// <summary>
    /// The narrower of the two axis extents through this point.
    /// </summary>
    static float WidthAt(Vector3 at, float groundY)
    {
        var from = new Vector3(at.x, groundY + Chest, at.z);

        float xa = WallDistance(from, Vector3.right);
        float xb = WallDistance(from, Vector3.left);
        float za = WallDistance(from, Vector3.forward);
        float zb = WallDistance(from, Vector3.back);

        float eastWest = xa + xb;
        float northSouth = za + zb;

        return Mathf.Min(eastWest, northSouth);
    }

    static char Band(Cell c)
    {
        if (c.Void) return ' ';
        float w = c.Width;
        if (w < 0.6f) return '#';
        if (w < 1.2f) return '=';
        if (w < 2.5f) return '-';
        if (w < 4f) return '+';
        if (w < 6f) return 'o';
        if (w < MaxReach - 0.01f) return 'O';
        return '#';
    }

    // --- regions -----------------------------------------------------------------

    class Region
    {
        public List<Cell> Cells = new List<Cell>();
    }

    static List<Region> Corridors(Cell[,] grid, int n)
    {
        var regions = new List<Region>();

        for (int xi = 0; xi < n; xi++)
        {
            for (int zi = 0; zi < n; zi++)
            {
                var start = grid[xi, zi];
                if (start.Void) continue;
                if (start.Width < 0.9f || start.Width > 6f) continue;
                if (grid[xi, zi].Seen) continue;

                var r = new Region();
                Grow(grid, n, xi, zi, r);

                // Six cells is three square metres, which is a doorway's worth of
                // floor. A single lane-shaped cell is a gap beside a wall, not a
                // passage, and reporting those as corridors fills the list with
                // noise that a designer then has to read past.
                if (r.Cells.Count >= 6) regions.Add(r);
            }
        }

        return regions.OrderByDescending(r => r.Cells.Count).ToList();
    }

    static void Grow(Cell[,] grid, int n, int xi, int zi, Region r)
    {
        var queue = new Queue<int>();
        queue.Enqueue(xi * 1000 + zi);
        grid[xi, zi].Seen = true;

        while (queue.Count > 0)
        {
            int code = queue.Dequeue();
            int cx = code / 1000, cz = code % 1000;

            r.Cells.Add(grid[cx, cz]);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue;

                    int nx = cx + dx, nz = cz + dz;
                    if (nx < 0 || nz < 0 || nx >= n || nz >= n) continue;

                    var c = grid[nx, nz];
                    if (c.Void || c.Seen) continue;
                    if (c.Width < 0.9f || c.Width > 6f) continue;

                    c.Seen = true;
                    grid[nx, nz] = c;
                    queue.Enqueue(nx * 1000 + nz);
                }
            }
        }
    }

    // --- one corridor, measured properly ----------------------------------------

    static void Report(StringBuilder sb, Cell[,] grid, Region r)
    {
        // Where it is, and how big.
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        var widths = new List<float>();

        foreach (var c in r.Cells)
        {
            float x = -Span + c.X * Step;
            float z = -Span + c.Z * Step;

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;

            widths.Add(c.Width);
        }

        widths.Sort();

        sb.AppendLine("  " + r.Cells.Count + " cells, " +
                      (r.Cells.Count * Step * Step).ToString("0.0") + " m2, " +
                      "box x " + minX.ToString("0.0") + " to " + maxX.ToString("0.0") +
                      ", z " + minZ.ToString("0.0") + " to " + maxZ.ToString("0.0"));
        sb.AppendLine("  map width: median " + widths[widths.Count / 2].ToString("0.00") +
                      " m, min " + widths[0].ToString("0.00") +
                      " m, max " + widths[widths.Count - 1].ToString("0.00") + " m");

        // Which way does it run? The dominant direction of the points, which is
        // what a lane's cells line up along and a courtyard's do not.
        float mx = 0f, mz = 0f;
        foreach (var c in r.Cells) { mx += -Span + c.X * Step; mz += -Span + c.Z * Step; }
        mx /= r.Cells.Count; mz /= r.Cells.Count;

        // Second moment: the spread along one axis against the spread along the
        // other says how elongated the region is, and the larger eigenvector
        // gives the direction of the length.
        float sxx = 0f, sxz = 0f, szz = 0f;
        foreach (var c in r.Cells)
        {
            float dx = (-Span + c.X * Step) - mx;
            float dz = (-Span + c.Z * Step) - mz;
            sxx += dx * dx; sxz += dx * dz; szz += dz * dz;
        }
        sxx /= r.Cells.Count; sxz /= r.Cells.Count; szz /= r.Cells.Count;

        float theta = 0.5f * Mathf.Atan2(2f * sxz, sxx - szz);
        var along = new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
        var across = new Vector3(-Mathf.Sin(theta), 0f, Mathf.Cos(theta));

        float spreadAlong = 0f, spreadAcross = 0f;
        foreach (var c in r.Cells)
        {
            float dx = (-Span + c.X * Step) - mx;
            float dz = (-Span + c.Z * Step) - mz;
            spreadAlong = Mathf.Max(spreadAlong, Mathf.Abs(dx * along.x + dz * along.z));
            spreadAcross = Mathf.Max(spreadAcross, Mathf.Abs(dx * across.x + dz * across.z));
        }

        sb.AppendLine("  runs " + (spreadAlong * 2f).ToString("0.0") + " m along " +
                      Direction(along) + " and " + (spreadAcross * 2f).ToString("0.0") +
                      " m across " + Direction(across) + ", about " +
                      (mx.ToString("0.0") + ", " + mz.ToString("0.0")));

        // The authoritative width: perpendicular to the direction it runs,
        // sampled along the corridor. The map's number is the narrow axis of an
        // axis-aligned box, which is only the true width if the lane happens to
        // lie on an axis, and a lane between two buildings rarely does.
        var samples = new List<float>();
        var gaps = new List<float>();

        for (float t = -spreadAlong; t <= spreadAlong; t += Step * 2f)
        {
            var at = new Vector3(mx, 0f, mz) + along * t;

            if (!Floor(at, out float gy)) continue;

            var from = new Vector3(at.x, gy + Chest, at.z);
            float w = WallDistance(from, across) + WallDistance(from, -across);

            samples.Add(w);
            if (w < 0.9f) gaps.Add(t);
        }

        if (samples.Count == 0)
        {
            sb.AppendLine("  no floor along its length — it is not a passage");
            return;
        }

        samples.Sort();

        sb.AppendLine("  MEASURED PERPENDICULAR: " + samples.Count + " samples, " +
                      "median " + samples[samples.Count / 2].ToString("0.00") +
                      " m, min " + samples[0].ToString("0.00") +
                      " m, max " + samples[samples.Count - 1].ToString("0.00") + " m");

        if (gaps.Count > 0)
        {
            sb.AppendLine("  NOT CONTINUOUS: " + gaps.Count + " of " + samples.Count +
                          " samples are under 0.9 m, the first at " +
                          gaps[0].ToString("0.0") + " m along. It narrows below " +
                          "Ari's shoulder somewhere, so this is a lane with a " +
                          "squeeze in it, not a run.");
        }
        else
        {
            sb.AppendLine("  continuous: nothing along it is under 0.9 m, so Ari can " +
                          "walk the whole length");
        }

        if (samples[samples.Count / 2] >= 0.9f && samples[samples.Count / 2] <= 2.0f)
            sb.AppendLine("  SUITABLE FOR A GULLY. Two characters cannot pass without " +
                          "turning, and the camera has to follow.");
        else if (samples[samples.Count / 2] > 2.0f && samples[samples.Count / 2] <= 4.0f)
            sb.AppendLine("  SUITABLE FOR AN ALLEY, not a gully. Wide enough to sidestep " +
                          "in, which Beat 5.5 wants and Beat 3 does not.");
    }

    static string Direction(Vector3 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.z))
            return v.x > 0f ? "east" : "west";
        return v.z > 0f ? "north" : "south";
    }
}
