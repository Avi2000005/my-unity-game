using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Lists every renderer under the market square with its real world size,
    /// so an oversized piece can be told apart from a correctly placed one.
    /// Writes Temp/square_contents.txt.
    ///
    /// Written because the square's overall height came back at 15.03 units
    /// when its paving is authored at 0.03. Something in there is 100x too big,
    /// and the only way to tell which piece is to measure them one at a time —
    /// the union bounds hide it completely, because one tall outlier defines the
    /// whole.
    /// </summary>
    public static class SquareContentsProbe
    {
        [MenuItem("Tools/Echoes/Probe Square Contents", priority = 97)]
        public static void Run()
        {
            var sb = new StringBuilder();

            var square = GameObject.Find("MarketSquare");
            if (square == null)
            {
                var village = GameObject.Find("Village_Grey");
                if (village != null)
                {
                    var t = village.transform.Find("MarketSquare");
                    if (t != null) square = t.gameObject;
                }
            }

            if (square == null)
            {
                Debug.LogError("[Echoes] no MarketSquare found");
                return;
            }

            sb.AppendLine($"MarketSquare at {square.transform.position}, " +
                          $"{square.transform.childCount} children");

            var rows = square.GetComponentsInChildren<Renderer>(true)
                .Select(r => new
                {
                    r,
                    b = r.bounds,
                    // lossyScale is what actually reaches the mesh; localScale
                    // alone would hide an extra factor coming from a parent.
                    scale = r.transform.lossyScale
                })
                .OrderByDescending(x => x.b.max.y)
                .ToArray();

            sb.AppendLine($"  {rows.Length} renderer(s), tallest first\n");
            sb.AppendLine("  model name                    world bounds size            " +
                          "maxY      lossyScale            parent");

            foreach (var x in rows)
            {
                var p = x.r.transform.parent;
                sb.AppendLine($"  {x.r.name,-28} " +
                              $"({x.b.size.x,6:0.00},{x.b.size.y,6:0.00},{x.b.size.z,6:0.00})  " +
                              $"{x.b.max.y,8:0.00}  " +
                              $"({x.scale.x:0.###},{x.scale.y:0.###},{x.scale.z:0.###})  " +
                              $"{p?.name ?? "<none>"}");
            }

            sb.AppendLine("\n--- by model name, aggregated ---");
            foreach (var g in rows.GroupBy(x => x.r.name)
                                  .OrderByDescending(g => g.Max(x => x.b.size.y)))
            {
                var first = g.First();
                sb.AppendLine($"  {g.Key,-28} x{g.Count(),-3} " +
                              $"height {first.b.size.y,8:0.00}  " +
                              $"lossyScale y {first.scale.y:0.###}");
            }

            var tallest = rows.First();
            sb.AppendLine($"\ntallest piece: '{tallest.r.name}' reaching y={tallest.b.max.y:0.00} " +
                          $"at size {tallest.b.size}");
            sb.AppendLine($"if that is a kerb or paver, anything above ~1 unit tall is the " +
                          "kit's 100x root scale leaking through — the houses are " +
                          "corrected for it by RootAt/SpawnCentered and these are not.");

            File.WriteAllText("Temp/square_contents.txt", sb.ToString());
            Debug.Log("[Echoes] Square contents probe\n" + sb);
        }
    }
}
