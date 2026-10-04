using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Global namespace, UnityEngine only, so run_script can compile and run it
// independently of the project assembly.
public static class WallSurvey
{
    const string ReportPath = "Temp/wall_survey.txt";

    /// <summary>
    /// What in this kit can be used as a wall.
    ///
    /// Beat 3's gully has to be built, because WidthMap measured 27 narrow
    /// regions across 21 025 cells and not one of them is narrower than 3.83 m.
    /// A gully is 1.2 to 2.0 m and nothing in the village is. So it gets made,
    /// and the only question standing between here and a walkable chase route
    /// is whether the kit holds anything long and thin enough to be a gully's
    /// two sides.
    ///
    /// The trap this exists to avoid is picking a prop by its name. A thing
    /// called a wall can be a 0.2 m thick slat, and a thing called a house can
    /// be a 6 m cube, and either can be silently built into a corridor that
    /// measures correctly and looks like nothing. So every candidate is
    /// measured: its world bounds, its thickness as the shortest horizontal
    /// extent, its length as the longest, and how many colliders came with it.
    /// A wall wants to be long and thin and cheap. A house wants to be neither.
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

        Debug.Log("[Echoes] wall survey\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportPath),
                           sb.ToString());
    }

    // Words that mean "this could plausibly stand as a side of a lane".
    static readonly string[] WallWords =
    {
        "wall", "fence", "border", "house", "building", "gate", "door",
        "hedge", "rail", "barrier", "wooden", "stone",
    };

    static void Body(StringBuilder sb)
    {
        Physics.SyncTransforms();

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        sb.AppendLine("WHAT IN THIS KIT CAN BE A WALL");
        sb.AppendLine("scene '" + scene.name + "', " + scene.rootCount + " roots");
        sb.AppendLine();

        // --- what is in the village at all -------------------------------------

        var roots = scene.GetRootGameObjects();
        var prefixes = new Dictionary<string, int>();
        int totalRenderers = 0, totalColliders = 0, totalTris = 0;

        // The kit is one level down, not at the top.
        //
        // Scanning the roots finds ten objects — Village, Ari, the camera, a
        // light — and every one of the 2 160 renderers is a child of Village, so
        // a root-level survey reports that the kit contains no walls, which is
        // true of the roots and false of the village. The village is the thing
        // being surveyed, so the survey goes into it.
        var units = new List<GameObject>();

        foreach (var root in roots)
        {
            totalRenderers += root.GetComponentsInChildren<Renderer>().Length;
            totalColliders += root.GetComponentsInChildren<Collider>().Length;
            totalTris += Triangles(root);

            // Expanded by shape, not by name.
            //
            // An exact match on "Village" did nothing, because the root's real
            // name is not that — the prefix histogram only said the name starts
            // with "Village", and a root called Village_0_0 or VillageKit sails
            // straight past an equality test while sitting in the middle of a
            // ten object scene. Anything with children is a container and gets
            // opened; anything without is a piece in its own right.
            if (root.transform.childCount > 0)
            {
                foreach (Transform child in root.transform) units.Add(child.gameObject);
                sb.AppendLine("'" + root.name + "' has " + root.transform.childCount +
                              " direct children — these are the kit's pieces.");
            }
            else
            {
                units.Add(root);
            }
        }

        foreach (var u in units)
        {
            var key = Prefix(u.name);
            prefixes.TryGetValue(key, out int c);
            prefixes[key] = c + 1;
        }

        // Length, not Count: GetRootGameObjects hands back an array, and on an
        // array Count is the LINQ extension method rather than a property.
        sb.AppendLine("roots: " + roots.Length + ", kit pieces: " + units.Count +
                      ", renderers: " + totalRenderers +
                      ", colliders: " + totalColliders + ", triangles: " + totalTris);
        sb.AppendLine();
        sb.AppendLine("--- the kit, by name prefix ---");

        foreach (var kv in prefixes.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
            sb.AppendLine("  " + kv.Value.ToString().PadLeft(5) + "  " + kv.Key);

        sb.AppendLine();

        // --- the candidates ---------------------------------------------------

        var candidates = new List<string>();

        foreach (var unit in units)
        {
            var lower = unit.name.ToLowerInvariant();
            if (!WallWords.Any(w => lower.Contains(w))) continue;

            candidates.Add(Describe(unit, sb, isRoot: false));
        }

        sb.AppendLine("--- candidates by name, measured ---");
        sb.AppendLine("  length is the longest horizontal extent, thickness the " +
                      "shortest, and a wall is the thing with a big gap between them.");

        foreach (var line in candidates) sb.AppendLine(line);

        if (candidates.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("NOTHING IN THE SCENE MATCHED. The kit has no wall, fence, " +
                          "border, house, gate or door by name, so a gully has to " +
                          "be built from primitives the way the placeholder stump " +
                          "was, and the level designer will have to accept grey " +
                          "boxes until real art arrives.");
        }

        // --- what building one would actually cost ---------------------------

        sb.AppendLine();
        sb.AppendLine("--- cost of a gully, measured ---");
        sb.AppendLine("  Ari's capsule is 0.60 m across, so the walls must clear " +
                      "1.20 m between their faces and stand at least 1.80 m tall.");
        sb.AppendLine("  At a target width of 1.60 m that is two runs of wall, each " +
                      "as long as the gully, plus 1.80 m of height.");
    }

    static string Prefix(string name)
    {
        int i = name.IndexOf('_');
        return i > 0 ? name.Substring(0, i) : name;
    }

    static int Triangles(GameObject go)
    {
        int n = 0;
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            var m = mf.sharedMesh;
            if (m == null) continue;
            n += m.triangles.Length / 3;
        }
        foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var m = smr.sharedMesh;
            if (m == null) continue;
            n += m.triangles.Length / 3;
        }
        return n;
    }

    static string Describe(GameObject go, StringBuilder sb, bool isRoot)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        var cols = go.GetComponentsInChildren<Collider>();

        if (rends.Length == 0)
            return "  " + go.name.PadRight(40) + " NO RENDERERS — " + cols.Length + " collider(s)";

        // World bounds, so a parent transform that carries a scale is included.
        var b = new Bounds(go.transform.position, Vector3.zero);
        bool first = true;
        foreach (var r in rends)
        {
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }

        var size = b.size;
        float length = Mathf.Max(size.x, size.z);
        float thickness = Mathf.Min(size.x, size.z);
        float slenderness = thickness > 0.01f ? length / thickness : 999f;

        return "  " + go.name.PadRight(40) +
               " " + size.x.ToString("0.0").PadLeft(5) + " x " +
               size.y.ToString("0.0").PadLeft(5) + " x " +
               size.z.ToString("0.0").PadLeft(5) + " m" +
               "   long " + length.ToString("0.0").PadLeft(5) +
               "  thick " + thickness.ToString("0.00").PadLeft(5) +
               "  x" + slenderness.ToString("0").PadLeft(5) +
               "  " + rends.Length + " renderer(s), " + cols.Length + " collider(s)" +
               // isTrigger lives on the Collider, not on the Renderer. Asking a
               // Renderer turns the silent question into a compile error, which
               // is the better of the two outcomes.
               TriggerNote(cols) +
               (isRoot ? "" : "  (not a root)");
    }

    static string TriggerNote(Collider[] cols)
    {
        for (int i = 0; i < cols.Length; i++)
            if (cols[i] != null && cols[i].isTrigger) return "  HAS A TRIGGER";
        return "";
    }
}
