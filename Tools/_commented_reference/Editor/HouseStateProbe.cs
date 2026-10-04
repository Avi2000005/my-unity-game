using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Reads where things are right now, and changes nothing.
    ///
    /// This exists because a run that ends by saving the scene can lose its report:
    /// saving may start a domain reload, and the run comes back with nothing to say.
    /// That leaves two possibilities — the move happened, or the tool died before it
    /// did — and they look identical from the outside. So the state is read back
    /// separately, from a tool that has no reason to save anything.
    /// </summary>
    public static class HouseStateProbe
    {
        const string Report = "Temp/house_state.txt";

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] current state (read only, nothing saved)");

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

            float groundY;
            var ground = SquarePlacement.FindGround(SquarePlacement.TopBounds(square),
                                                   out groundY, square.transform);
            var centre = square.transform.position;

            sb.AppendLine();
            sb.AppendLine("--- the square ---");
            sb.AppendLine("centre           : " + centre);
            sb.AppendLine("paving extent    : " + ground.size + " at " + ground.center);
            sb.AppendLine("ground y         : " + groundY.ToString("F3"));

            // --- the house --------------------------------------------------------
            sb.AppendLine();
            sb.AppendLine("--- House_0_0 ---");
            var named = new System.Collections.Generic.List<Transform>();
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name == "House_0_0") named.Add(t);

            if (named.Count == 0)
            {
                sb.AppendLine("NOT FOUND anywhere in the scene.");
            }
            foreach (var t in named)
            {
                var b = SquarePlacement.TopBounds(t.gameObject);
                bool onPaving = b.center.x > ground.min.x && b.center.x < ground.max.x
                             && b.center.z > ground.min.z && b.center.z < ground.max.z;

                sb.AppendLine("path             : " + SquarePlacement.PathOf(t));
                sb.AppendLine("position         : " + t.position);
                sb.AppendLine("yaw              : " + t.eulerAngles.y.ToString("F1"));
                sb.AppendLine("world bounds     : " + b.size + " at " + b.center);
                sb.AppendLine("still on paving? : " + (onPaving ? "YES — still in the square" : "no, it has moved"));
                sb.AppendLine("distance from centre : "
                    + Vector2.Distance(new Vector2(b.center.x - centre.x, b.center.z - centre.z),
                                       Vector2.zero).ToString("F2") + " m");
            }

            // --- the fountain -----------------------------------------------------
            var fountain = GameObject.Find("Fountain");
            sb.AppendLine();
            sb.AppendLine("--- the fountain ---");
            if (fountain == null)
            {
                sb.AppendLine("NOT FOUND.");
            }
            else
            {
                var b = SquarePlacement.TopBounds(fountain);
                sb.AppendLine("position         : " + fountain.transform.position);
                sb.AppendLine("world bounds     : " + b.size + " at " + b.center);
                sb.AppendLine("distance from centre : "
                    + Vector2.Distance(new Vector2(b.center.x - centre.x, b.center.z - centre.z),
                                       Vector2.zero).ToString("F2") + " m");
            }

            // --- is the middle open now? -----------------------------------------
            sb.AppendLine();
            sb.AppendLine("--- the middle of the square ---");
            var scan = SquarePlacement.MeasureSquare(0.4f, 9f);
            if (!scan.Ok)
            {
                sb.AppendLine("scan failed: " + scan.Why);
                Finish(sb);
                return;
            }

            var xs = scan.Spots.Select(s => s.Clearance).OrderBy(v => v).ToList();
            sb.AppendLine("ground samples   : " + scan.Samples);
            sb.AppendLine("median clearance : " + xs[xs.Count / 2].ToString("F2") + " m");
            sb.AppendLine("max clearance    : " + scan.BestClearance.ToString("F2") + " m at " + scan.Best.Point);
            sb.AppendLine("spots fitting r=3.4 m (the fountain needs 3.39 m): "
                          + scan.Fitting(3.4f).Count());

            float bestMiddle = 0f;
            Vector3 atMiddle = Vector3.zero;
            foreach (var s in scan.Spots)
            {
                float d = Vector2.Distance(new Vector2(s.Point.x - centre.x, s.Point.z - centre.z),
                                           Vector2.zero);
                if (d > 2f) continue;
                if (s.Clearance > bestMiddle) { bestMiddle = s.Clearance; atMiddle = s.Point; }
            }
            sb.AppendLine("best clearance within 2 m of the centre : "
                          + bestMiddle.ToString("F2") + " m at " + atMiddle);

            sb.AppendLine();
            if (scan.Fitting(3.4f).Count() > 0
                && SquarePlacement.TryFind(scan, 3.39f, out var p, out var c, out var why, null))
                sb.AppendLine("the fountain would now stand at " + p + " with " + c.ToString("F2")
                              + " m clearance");
            else
                sb.AppendLine("the fountain STILL has nowhere to stand in the square");

            Finish(sb);
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
