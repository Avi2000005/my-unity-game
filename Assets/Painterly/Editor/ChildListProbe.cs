using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Prints the direct children of the named object. Writes Temp/children.txt.
    ///
    /// The square probe reported 106 children and 231 renderers under
    /// MarketSquare, which is far more paving than a 5x5 plaza needs, and named
    /// House_0_0 as the parent of its tallest pieces. GetChildrenNames returns
    /// the immediate level only, so this separates "the square legitimately holds
    /// its own paving, kerbs and loading dock" from "the houses ended up inside
    /// it" without either being assumed.
    /// </summary>
    public static class ChildListProbe
    {
        [MenuItem("Tools/Echoes/Probe Children", priority = 98)]
        public static void Run()
        {
            var sb = new StringBuilder();
            var targets = new[] { "Village_Grey", "MarketSquare" };

            foreach (var name in targets)
            {
                var go = GameObject.Find(name);
                if (go == null)
                {
                    var v = GameObject.Find("Village_Grey");
                    if (v != null)
                    {
                        var t = v.transform.Find(name);
                        if (t != null) go = t.gameObject;
                    }
                }

                if (go == null) { sb.AppendLine($"{name}: NOT FOUND"); continue; }

                var kids = go.transform.Cast<Transform>().ToArray();
                sb.AppendLine($"{name}: {kids.Length} direct children, " +
                              $"{go.GetComponentsInChildren<Renderer>(true).Length} renderers total");

                foreach (Transform k in kids)
                {
                    int rends = k.GetComponentsInChildren<Renderer>(true).Length;
                    sb.AppendLine($"  {k.name,-30} childCount={k.childCount,-4} " +
                                  $"renderers={rends,-4} localPos={k.localPosition} " +
                                  $"active={k.gameObject.activeSelf}");
                }
                sb.AppendLine();
            }

            File.WriteAllText("Temp/children.txt", sb.ToString());
            Debug.Log("[Echoes] Child list probe\n" + sb);
        }
    }
}
