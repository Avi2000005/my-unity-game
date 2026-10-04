using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Measures every model in the village kit and writes a report to Temp/.
    ///
    /// This exists because nothing in the kit can be trusted by eye. The FBX roots
    /// carry a 100x scale and a 270-degree rotation about X, so a wall's 3.12 height
    /// is stored along the mesh's local Z axis, and a "2 unit" floor tile is not
    /// necessarily 2 units. Hard-coding dimensions produced a village with buried
    /// floors and 0.41-unit-tall walls; measuring is the only reliable option.
    ///
    /// The report is grouped by name prefix (Wall_, Roof_, Stairs_ ...) and sorted by
    /// size within each group, so the pieces that fit together on the same module
    /// end up adjacent.
    /// </summary>
    public static class KitSurvey
    {
        const string ModelDir = "Assets/Art/Village/Models";
        const string ReportPath = "Temp/kit_survey.txt";

        [MenuItem("Tools/Echoes/Survey Kit", priority = 40)]
        public static void Run()
        {
            var guids = AssetDatabase.FindAssets("t:Model", new[] { ModelDir });
            var names = new List<string>();
            foreach (var g in guids)
                names.Add(Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)));
            names.Sort();

            var groups = new Dictionary<string, List<string[]>>();
            var all = new List<string[]>();
            int missing = 0;

            foreach (var n in names)
            {
                var row = Measure(n);
                if (row == null) { missing++; continue; }
                all.Add(row);
                Group(groups, n).Add(row);
            }

            var sb = new StringBuilder();
            sb.AppendLine("VILLAGE KIT SURVEY");
            sb.AppendLine("models found: " + names.Count + "   measured: " + all.Count
                          + "   unmeasurable: " + missing);
            sb.AppendLine("columns: size(X,Y,Z) world units | offset from prefab root to mesh min corner");
            sb.AppendLine();

            foreach (var key in Sorted(groups.Keys))
            {
                sb.AppendLine("== " + key + " (" + groups[key].Count + ") ==");
                var rows = groups[key];
                rows.Sort((a, b) => string.CompareOrdinal(a[0], b[0]));
                foreach (var r in rows)
                    sb.AppendLine("  " + r[0].PadRight(34) + r[1]);
                sb.AppendLine();
            }

            // The single most useful fact: what module does the kit snap to?
            sb.AppendLine("== module hints ==");
            foreach (var step in new[] { 1f, 0.5f, 0.25f, 0.1f, 0.05f, 0.01f })
            {
                int hits = 0, total = 0;
                foreach (var r in all)
                {
                    var parts = r[1].Trim().Split('|');
                    var size = parts[0].Replace("size(", "").Replace(")", "").Split(',');
                    foreach (var s in size)
                    {
                        if (float.TryParse(s, out float v) && v > 0.05f)
                        {
                            total++;
                            if (Mathf.Abs(v / step - Mathf.Round(v / step)) < 0.01f) hits++;
                        }
                    }
                }
                sb.AppendLine("  step " + step.ToString("0.###") + ": " + hits + "/" + total
                              + " (" + (100f * hits / Mathf.Max(1, total)).ToString("F0") + "%) of extents are whole multiples");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, sb.ToString());

            Debug.Log("[Echoes] Kit survey written to " + ReportPath
                      + " (" + all.Count + " models, " + groups.Count + " groups).");
        }

        static List<string> Sorted(ICollection<string> keys)
        {
            var l = new List<string>(keys);
            l.Sort();
            return l;
        }

        static List<string[]> Group(Dictionary<string, List<string[]>> groups, string name)
        {
            string key = name.Contains("_") ? name.Substring(0, name.IndexOf('_')) : "(misc)";
            if (!groups.TryGetValue(key, out var list)) groups[key] = list = new List<string[]>();
            return list;
        }

        /// <summary>
        /// Returns { name, "size(x,y,z) | offset(x,y,z)" } or null if unmeasurable.
        /// Mirrors VillageGenerator.Measure exactly — scene-root instantiation, then
        /// Renderer.bounds — so the two can never disagree about a piece's size.
        /// </summary>
        static string[] Measure(string modelName)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{modelName}.fbx");
            if (model == null) return null;

            var probe = (GameObject)PrefabUtility.InstantiatePrefab(model);
            probe.hideFlags = HideFlags.HideAndDontSave;

            string[] row = null;
            var rend = probe.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                var b = rend.bounds;
                var off = b.min - probe.transform.position;
                row = new[]
                {
                    modelName,
                    string.Format("size({0:F2},{1:F2},{2:F2}) | offset({3:F2},{4:F2},{5:F2})",
                                  b.size.x, b.size.y, b.size.z, off.x, off.y, off.z)
                };
            }

            Object.DestroyImmediate(probe);
            return row;
        }
    }
}
