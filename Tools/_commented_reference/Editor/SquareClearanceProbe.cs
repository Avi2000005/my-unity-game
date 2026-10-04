using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Reports how much open ground the market square has and where, using the same
    /// measurement the builder uses to choose a spot.
    ///
    /// The first clearance reading was wrong in a way that mattered. It decided what
    /// counted as ground from transform names, and the kit names its paving a dozen
    /// ways, so real floor pieces read as solid obstacles and the non-paving region
    /// came out half again as wide as the building standing on it. A prop sized to
    /// that reading would have been built smaller than the square could take.
    ///
    /// Ground is now decided by measurement instead: flat, low, and at the height a
    /// plurality of the flat low pieces sit at. This report exists to check that
    /// decision rather than to trust it — it prints the ground level it settled on,
    /// the height histogram it settled it from, and the resulting field, so a wrong
    /// choice is visible as a wrong number rather than as a prop that quietly does
    /// not fit.
    /// </summary>
    public static class SquareClearanceProbe
    {
        const string Report = "Temp/square_clearance.txt";

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] open ground, measured");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sb.AppendLine("STOPPED: exit play mode first (Ctrl+P).");
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

            // --- the height the ground search settled on ------------------------
            float groundY;
            var ground = SquarePlacement.FindGround(
                SquarePlacement.TopBounds(square), out groundY, square.transform);

            sb.AppendLine();
            sb.AppendLine("--- ground detection ---");
            sb.AppendLine("MarketSquare      : " + SquarePlacement.PathOf(square.transform));
            sb.AppendLine("chosen ground Y   : " + groundY.ToString("F3"));
            sb.AppendLine("ground extent     : " + ground.size + " at " + ground.center);
            sb.AppendLine("height histogram of flat low pieces (0.1 m buckets):");

            var hist = new System.Collections.Generic.Dictionary<int, int>();
            foreach (var r in square.GetComponentsInChildren<Renderer>(true))
            {
                var b = r.bounds;
                if (b.size.x < 0.8f || b.size.z < 0.8f) continue;
                if (b.size.y > 0.7f || b.max.y > 2.0f) continue;
                int k = Mathf.RoundToInt(b.max.y / 0.1f);
                if (!hist.ContainsKey(k)) hist[k] = 0;
                hist[k]++;
            }
            foreach (var kv in hist.OrderBy(k => k.Key))
                sb.AppendLine("   y~" + (kv.Key * 0.1f).ToString("F1").PadLeft(5)
                    + "  " + kv.Value + " piece(s)" + (Mathf.Abs(kv.Key * 0.1f - groundY) < 0.05f ? "   <== chosen" : ""));

            // --- the square -----------------------------------------------------
            var scan = SquarePlacement.MeasureSquare(0.4f, 9f);
            if (!scan.Ok)
            {
                sb.AppendLine();
                sb.AppendLine("FATAL: square scan failed: " + scan.Why);
                Finish(sb);
                return;
            }

            sb.AppendLine();
            sb.AppendLine("--- market square ---");
            sb.AppendLine("scanned area      : " + scan.Area.size + " at " + scan.Area.center);
            sb.AppendLine("ground samples    : " + scan.Samples);
            sb.AppendLine("clearance median  : " + Median(scan).ToString("F2") + " m");
            sb.AppendLine("clearance max     : " + scan.BestClearance.ToString("F2") + " m at " + scan.Best.Point);

            Map(sb, scan, 0.4f);

            // --- what fits, and where -------------------------------------------
            sb.AppendLine();
            sb.AppendLine("--- what fits in the square ---");
            foreach (var need in new[] { 3.24f, 2.60f, 2.20f, 2.00f, 1.80f, 1.60f, 1.40f, 1.20f })
            {
                if (SquarePlacement.TryFind(scan, need, out var p, out var c, out _, null))
                    sb.AppendLine("   radius " + need.ToString("F2").PadLeft(5)
                        + " m (dia " + (2 * need).ToString("F2").PadLeft(5) + " m)  ->  "
                        + p.ToString().PadRight(28) + " clearance " + c.ToString("F2"));
                else
                    sb.AppendLine("   radius " + need.ToString("F2").PadLeft(5)
                        + " m (dia " + (2 * need).ToString("F2").PadLeft(5) + " m)  ->  DOES NOT FIT");
            }

            // --- is the square even the best place in the village? ---------------
            sb.AppendLine();
            sb.AppendLine("--- whole village, coarse ---");
            var village = SquarePlacement.MeasureVillage(1.2f, 8f);
            if (!village.Ok)
            {
                sb.AppendLine("village scan failed: " + village.Why);
            }
            else
            {
                sb.AppendLine("scanned area      : " + village.Area.size + " at " + village.Area.center);
                sb.AppendLine("ground samples    : " + village.Samples);
                sb.AppendLine("clearance max     : " + village.BestClearance.ToString("F2")
                    + " m at " + village.Best.Point);
                sb.AppendLine("spots fitting r = 6.0 m (a house footprint): "
                    + village.Fitting(6.0f).Count());
                sb.AppendLine("spots fitting r = 3.4 m (the fountain)    : "
                    + village.Fitting(3.4f).Count());
                sb.AppendLine();
                sb.AppendLine("widest open regions in the village (greedy, spots further than");
                sb.AppendLine("their clearance from each other so they are different places):");

                var picked = new System.Collections.Generic.List<SquarePlacement.Spot>();
                foreach (var s in village.Spots.OrderByDescending(s => s.Clearance))
                {
                    if (picked.Count >= 10) break;
                    if (picked.Any(p => Vector3.Distance(p.Point, s.Point) < s.Clearance + 1f)) continue;
                    picked.Add(s);
                }
                foreach (var s in picked)
                    sb.AppendLine("   clearance " + s.Clearance.ToString("F2").PadLeft(5)
                        + " m  at " + s.Point.ToString().PadRight(30)
                        + (s.Clearance >= 6.0f ? "fits a house  " : "             ")
                        + " nearest " + s.Nearest);
            }

            Finish(sb);
        }

        static float Median(SquarePlacement.Scan scan)
        {
            var xs = scan.Spots.Select(s => s.Clearance).OrderBy(v => v).ToList();
            return xs.Count == 0 ? 0f : xs[xs.Count / 2];
        }

        static void Map(StringBuilder sb, SquarePlacement.Scan scan, float step)
        {
            var area = scan.Area;
            int cols = Mathf.CeilToInt(area.size.x / step) + 1;
            int rows = Mathf.CeilToInt(area.size.z / step) + 1;

            var grid = new float[cols, rows];
            for (int i = 0; i < cols; i++)
                for (int j = 0; j < rows; j++)
                    grid[i, j] = -1f;

            int bestI = -1, bestJ = -1;
            foreach (var s in scan.Spots)
            {
                int i = Mathf.RoundToInt((s.Point.x - area.min.x) / step);
                int j = Mathf.RoundToInt((s.Point.z - area.min.z) / step);
                if (i < 0 || j < 0 || i >= cols || j >= rows) continue;
                grid[i, j] = s.Clearance;
                if (scan.Best != null && s.Point == scan.Best.Point) { bestI = i; bestJ = j; }
            }

            sb.AppendLine();
            sb.AppendLine("clearance map (x left->right, z bottom->top, cell " + step + " m)");
            sb.AppendLine("  '#'<0.5 '+'<1.0 '-'<1.5 ':'<2.0 '.'<2.5 ','<3.0 ' '>=3.0  'O'=best  'X'=no ground");
            sb.AppendLine();

            var head = new StringBuilder("       ");
            for (int i = 0; i < cols; i += 2) head.Append((area.min.x + i * step).ToString("F0").PadRight(2));
            sb.AppendLine(head.ToString());

            for (int j = rows - 1; j >= 0; j--)
            {
                var line = new StringBuilder();
                line.Append((area.min.z + j * step).ToString("F0").PadLeft(5)).Append("  ");
                for (int i = 0; i < cols; i++)
                {
                    float c = grid[i, j];
                    if (c < 0f) { line.Append('X'); continue; }
                    if (i == bestI && j == bestJ) { line.Append('O'); continue; }
                    line.Append(c < 0.5f ? '#' : c < 1.0f ? '+' : c < 1.5f ? '-' :
                                c < 2.0f ? ':' : c < 2.5f ? '.' : c < 3.0f ? ',' : ' ');
                }
                sb.AppendLine(line.ToString());
            }
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
