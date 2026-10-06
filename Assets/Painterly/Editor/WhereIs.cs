using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

using Object = UnityEngine.Object;
// Global namespace, UnityEngine only, so run_script can compile and run it
// independently of the project assembly.
public static class WhereIs
{
    const string ReportPath = "Temp/where_is.txt";

    /// <summary>
    /// What is standing in a box of the village.
    ///
    /// The crawlspace map found exactly one substantial low space — a run with
    /// a flat 0.78 m ceiling and 1.60 m of width, three metres by six — and
    /// every other candidate turned out to be a roof overhang beside a wall,
    /// which is what thirty-one of them were. That one space is either a real
    /// covered passage worth building the beat in, or it is the inside of
    /// something that was already there, and the two are not interchangeable:
    /// building a crawlspace through the back wall of a house produces a beat
    /// whose ceiling is somebody's floor.
    ///
    /// So this asks what is actually in the box before anything is designed
    /// around it: every collider, every renderer, and every named object whose
    /// bounds reach into it.
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
        Debug.Log("[Echoes] where is\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportPath),
                           sb.ToString());
    }

    // The box the crawlspace map's region 1 occupies, with a metre of slack
    // around it so a wall just outside the box still shows up. Used when
    // Temp/whereis_box.txt is absent — see ReadBox.
    const float DefaultMinX = -8.5f, DefaultMaxX = -3.5f;
    const float DefaultMinZ = -4.0f, DefaultMaxZ = 4.0f;
    const float DefaultMinY = -0.5f, DefaultMaxY = 3.0f;

    const string BoxPath = "Temp/whereis_box.txt";

    static float MinX, MaxX, MinY, MaxY, MinZ, MaxZ;

    /// <summary>
    /// The box to survey, read from a file so it can be moved without
    /// recompiling.
    ///
    /// Surveying the village by recompiling the tool for every candidate spot
    /// costs forty seconds a time, and there are five beats still to place.
    /// The box is six numbers; putting them in a text file makes asking the
    /// next question free.
    ///
    /// A file that does not exist, is short, or holds something that is not a
    /// number falls back to the default and says so. A survey tool that quietly
    /// used the wrong box would be worse than no tool, because the report would
    /// still look right.
    /// </summary>
    static void ReadBox(StringBuilder sb)
    {
        MinX = DefaultMinX; MaxX = DefaultMaxX;
        MinY = DefaultMinY; MaxY = DefaultMaxY;
        MinZ = DefaultMinZ; MaxZ = DefaultMaxZ;

        string text = null;
        try { if (File.Exists(BoxPath)) text = File.ReadAllText(BoxPath); }
        catch (Exception e)
        {
            sb.AppendLine("!! could not read " + BoxPath + ": " + e.Message +
                          " — using the default box");
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            sb.AppendLine("no " + BoxPath + ", so the default box is used");
            return;
        }

        var parts = text.Split(new[] { ',', ' ', '\n', '\r', '\t' },
                               StringSplitOptions.RemoveEmptyEntries);
        var got = new List<float>();

        foreach (var raw in parts)
        {
            if (!float.TryParse(raw, out float v)) continue;
            got.Add(v);
        }

        if (got.Count < 6)
        {
            sb.AppendLine("!! " + BoxPath + " has " + got.Count +
                          " numbers, needs 6 (minX minY minZ maxX maxY maxZ)" +
                          " — using the default box");
            return;
        }

        MinX = got[0]; MinY = got[1]; MinZ = got[2];
        MaxX = got[3]; MaxY = got[4]; MaxZ = got[5];

        sb.AppendLine("box read from " + BoxPath);
    }

    static void Body(StringBuilder sb)
    {
        ReadBox(sb);
        Physics.SyncTransforms();

        var box = new Bounds(Centre(), new Vector3(MaxX - MinX, MaxY - MinY, MaxZ - MinZ));

        sb.AppendLine();
        sb.AppendLine("WHAT IS IN x " + MinX.ToString("0.0") + " to " + MaxX.ToString("0.0") +
                      ", y " + MinY.ToString("0.0") + " to " + MaxY.ToString("0.0") +
                      ", z " + MinZ.ToString("0.0") + " to " + MaxZ.ToString("0.0"));
        sb.AppendLine("centre " + box.center.ToString("F2") + ", size " +
                      box.size.ToString("F2"));
        sb.AppendLine();

        Landmarks(sb, box.center);
        FreeSpace(sb, box);

        // --- colliders ---------------------------------------------------------

        // Physics overlap, not a scene walk. It answers what a body would
        // actually collide with in that box, which is the question; walking the
        // hierarchy answers what exists, which is a different and weaker one.
        var hits = Physics.OverlapBox(box.center, box.size * 0.5f, Quaternion.identity,
                                      ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => string.CompareOrdinal(a.name, b.name));

        sb.AppendLine("--- colliders a body would meet here (" + hits.Length + ") ---");
        sb.AppendLine("  sorted by name, because the same wall is generated once" +
                      " per house and an unsorted list hides that.");

        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            var b = c.bounds;
            sb.AppendLine("  " + c.GetType().Name.PadRight(20) + " '" + c.name + "'");
            sb.AppendLine("      at " + b.center.ToString("F2") +
                          ", size " + b.size.ToString("F2") +
                          "  top " + b.max.y.ToString("F2") +
                          "  layer " + c.gameObject.layer +
                          "  tag '" + c.gameObject.tag + "'");
            sb.AppendLine("      path " + Route(c.transform));
        }

        sb.AppendLine();

        // --- renderers ---------------------------------------------------------

        var seen = new HashSet<GameObject>();
        var rows = new List<string>();

        foreach (var r in FindAll<Renderer>())
        {
            if (r == null || !Intersects(r.bounds, box)) continue;
            if (!seen.Add(r.gameObject)) continue;

            var b = r.bounds;
            var mats = r.sharedMaterials;
            string mat = (mats != null && mats.Length > 0 && mats[0] != null)
                ? mats[0].name : "none";

            rows.Add("  '" + r.gameObject.name + "'  " + r.GetType().Name +
                     "\n      at " + b.center.ToString("F2") +
                     ", size " + b.size.ToString("F2") +
                     "  y " + b.min.y.ToString("F2") + " to " + b.max.y.ToString("F2") +
                     "  material '" + mat + "'" +
                     (r.gameObject.activeInHierarchy ? "" : "  [INACTIVE]") +
                     "\n      path " + Route(r.transform));
        }

        rows.Sort(StringComparer.Ordinal);
        sb.AppendLine("--- renderers reaching into the box (" + rows.Count + ") ---");
        foreach (var r in rows) sb.AppendLine(r);

        sb.AppendLine();

        // --- named objects -----------------------------------------------------

        // The transforms with no renderer and no collider: markers, empties,
        // triggers, beat containers. Invisible in both lists above and the
        // reason a build tool sometimes reports a space as free when a beat is
        // already standing in it.
        var empties = new List<string>();
        foreach (var t in FindAll<Transform>())
        {
            if (t == null || !seen.Contains(t.gameObject)) continue;
            if (t.GetComponent<Renderer>() != null) continue;
            if (t.GetComponent<Collider>() != null) continue;
            if (!Intersects(BoundsOf(t), box)) continue;
            empties.Add("  '" + t.name + "'  " + t.GetComponentTypeNames() +
                        "\n      at " + t.position.ToString("F2") +
                        "\n      path " + Route(t));
        }

        empties.Sort(StringComparer.Ordinal);
        sb.AppendLine("--- non-visual objects with children here (" + empties.Count + ") ---");
        foreach (var e in empties) sb.AppendLine(e);

        sb.AppendLine();

        // --- what it means -----------------------------------------------------

        sb.AppendLine("--- reading ---");

        var roofed = rows.Count > 0;
        sb.AppendLine("  a roof over this box: " + (roofed
            ? "yes, " + rows.Count + " renderer(s) reach into it"
            : "NO. There is nothing above this ground but sky, which means the" +
              " flat 0.78 m ceiling the map reported is not a ceiling at all"));

        if (!roofed)
        {
            sb.AppendLine();
            sb.AppendLine("  So region 1 is a measurement artefact and the crawlspace" +
                          " has to be BUILT from nothing — a roof put over open" +
                          " ground, a wall put up with a 1.10 m gap in it. That is" +
                          " a legitimate thing to do and it is what Beat 3's gully" +
                          " already does, so this is not a blocker; but it does" +
                          " mean nothing in this box can be reused and the whole" +
                          " beat is new geometry.");
        }
    }

    // --- how much room is actually free ----------------------------------------

    /// <summary>
    /// Ground, headroom and lane width inside the box.
    ///
    /// The collider and renderer lists above answer "what is here". They cannot
    /// answer "is there enough clear space here to build a beat", which is the
    /// question that matters, and answering it by eye from the lists is exactly
    /// the guessing this project keeps refusing to do.
    ///
    /// So: a grid of what the ground is doing, and a profile of how wide the
    /// open lane is and how high the ceiling over it, sampled the same way
    /// CrawlspaceMap samples so the two reports can be read together.
    /// </summary>
    static void FreeSpace(StringBuilder sb, Bounds box)
    {
        const float Step = 0.5f;

        sb.AppendLine("--- ground, headroom and lane width ---");
        sb.AppendLine();

        bool alongX = (MaxX - MinX) >= (MaxZ - MinZ);

        // --- ground grid -------------------------------------------------------

        sb.AppendLine("ground surface, sampled every " + Step.ToString("0.0") +
                      " m. '.' flat, digits are the height in decimetres, 'x' no floor.");

        int across = alongX
            ? Mathf.Max(2, Mathf.RoundToInt((MaxZ - MinZ) / Step) + 1)
            : Mathf.Max(2, Mathf.RoundToInt((MaxX - MinX) / Step) + 1);
        int along = alongX
            ? Mathf.Max(2, Mathf.RoundToInt((MaxX - MinX) / Step) + 1)
            : Mathf.Max(2, Mathf.RoundToInt((MaxZ - MinZ) / Step) + 1);

        var surface = new float[across, along];

        for (int i = 0; i < across; i++)
        {
            var row = new StringBuilder();
            for (int j = 0; j < along; j++)
            {
                var at = alongX
                    ? new Vector3(MinX + j * Step, 0f, MinZ + i * Step)
                    : new Vector3(MinX + i * Step, 0f, MinZ + j * Step);

                float y = GroundAt(at);
                surface[i, j] = y;

                if (y <= float.MinValue / 2f) { row.Append('x'); continue; }
                if (y < 0.12f) { row.Append('.'); continue; }

                int dm = Mathf.RoundToInt(y * 10f);
                row.Append(dm > 9 ? '+' : dm.ToString());
            }
            sb.AppendLine("  " + row);
        }

        // --- profile -----------------------------------------------------------

        sb.AppendLine();
        sb.AppendLine("profile down the middle of the box. 'head' is the ceiling" +
                      " over the floor; 'lane' is the free width at 1.0 m up,");
        sb.AppendLine("which is where Ari's body is, so a lane narrower than" +
                      " 0.70 m is one he cannot walk down.");
        sb.AppendLine("lane is capped at " + (LaneCap).ToString("0.00") +
                      " m — that reading means 'nothing within " +
                      (LaneCap * 0.5f).ToString("0.0") + " m either way', not that the" +
                      " lane is exactly that wide.");
        sb.AppendLine();

        var heads = new List<float>();
        var lanes = new List<float>();

        for (int j = 0; j < along; j++)
        {
            var at = alongX
                ? new Vector3(MinX + j * Step, 0f, (MinZ + MaxZ) * 0.5f)
                : new Vector3((MinX + MaxX) * 0.5f, 0f, MinZ + j * Step);

            float g = GroundAt(at);
            if (g <= float.MinValue / 2f)
            {
                sb.AppendLine("  " + (alongX ? MinX + j * Step : MinZ + j * Step)
                                 .ToString("0.0") + "  no floor");
                continue;
            }

            float head = HeadroomAt(at, g);
            float lane = LaneAt(at, g);

            heads.Add(head);
            lanes.Add(lane);

            sb.AppendLine("  " + (alongX ? at.x : at.z).ToString("0.0").PadLeft(6) +
                          "  ground " + g.ToString("0.00") +
                          "  head " + head.ToString("0.00") +
                          "  lane " + lane.ToString("0.00") +
                          "  " + Verdict(head, lane));
        }

        if (heads.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("  no floor down the middle of this box at all.");
            return;
        }

        heads.Sort(); lanes.Sort();

        sb.AppendLine();
        sb.AppendLine("  headroom: median " + heads[heads.Count / 2].ToString("0.00") +
                      " m, min " + heads[0].ToString("0.00") +
                      " m, max " + heads[heads.Count - 1].ToString("0.00") + " m");
        sb.AppendLine("  lane:     median " + lanes[lanes.Count / 2].ToString("0.00") +
                      " m, min " + lanes[0].ToString("0.00") +
                      " m, max " + lanes[lanes.Count - 1].ToString("0.00") + " m");

        float openLane = 0f;
        for (int i = 0; i < lanes.Count; i++) openLane = Mathf.Max(openLane, lanes[i]);
        float openHead = heads[heads.Count - 1];

        sb.AppendLine();
        sb.AppendLine("  widest open lane " + openLane.ToString("0.00") +
                      " m, highest clear ceiling " + openHead.ToString("0.00") +
                      " m, over a run of " + along + " samples (" +
                      ((along - 1) * Step).ToString("0.0") + " m)");
        sb.AppendLine("  a beat gate needs a lane it can close and a run long" +
                      " enough to walk up to; a crawlspace needs a lane wide" +
                      " enough for a 0.25 m-wide man.");
    }

    static string Verdict(float head, float lane)
    {
        string s = "";
        if (lane < 0.7f) s += "[TOO NARROW FOR ARI] ";
        if (head > 0f && head < 2.0f) s += "[COVERED] ";
        if (s.Length == 0) s = "[open]";
        return s;
    }

    static float GroundAt(Vector3 at)
    {
        var hits = Physics.RaycastAll(at + Vector3.up * 30f, Vector3.down, 60f, ~0,
                                      QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return float.MinValue;

        float lowest = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null) continue;
            if (hits[i].point.y < lowest) lowest = hits[i].point.y;
        }
        return lowest == float.MaxValue ? float.MinValue : lowest;
    }

    static float HeadroomAt(Vector3 at, float groundY)
    {
        const float lift = 0.15f;
        var hits = Physics.RaycastAll(new Vector3(at.x, groundY + lift, at.z),
                                      Vector3.up, 6f, ~0,
                                      QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return 6f;

        float lowest = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null) continue;
            if (hits[i].point.y < lowest) lowest = hits[i].point.y;
        }
        return lowest == float.MaxValue ? 6f : lowest - groundY;
    }

    /// <summary>
    /// Half the lane reading that means "nothing was in the way".
    ///
    /// A width measured by two rays is only ever known to be at least as large
    /// as the rays were long, and a report that printed the reach as a width
    /// would claim a sixteen-metre lane down the middle of a village square.
    /// </summary>
    const float LaneReach = 9f;

    const float LaneCap = LaneReach * 2f;

    static float LaneAt(Vector3 at, float groundY)
    {
        float reach = LaneReach;
        var from = new Vector3(at.x, groundY + 1.0f, at.z);

        float East() => Physics.Raycast(from, Vector3.right, out RaycastHit h, reach, ~0,
                                        QueryTriggerInteraction.Ignore) ? h.distance : reach;
        float West() => Physics.Raycast(from, Vector3.left, out RaycastHit h, reach, ~0,
                                        QueryTriggerInteraction.Ignore) ? h.distance : reach;
        float North() => Physics.Raycast(from, Vector3.forward, out RaycastHit h, reach, ~0,
                                         QueryTriggerInteraction.Ignore) ? h.distance : reach;
        float South() => Physics.Raycast(from, Vector3.back, out RaycastHit h, reach, ~0,
                                         QueryTriggerInteraction.Ignore) ? h.distance : reach;

        return Mathf.Min(East() + West(), North() + South());
    }

    // --- where the rest of the level is ----------------------------------------

    /// <summary>
    /// The fixed points of the level, and how far the box is from each.
    ///
    /// Beat 4 has to go after Beat 3, and nothing in the scene says where
    /// Beat 3 ends except the gully's own route object. Guessing at the layout
    /// from a screenshot is how a gate ends up forty metres from the run that
    /// was supposed to lead to it, and the report would show a perfectly good
    /// patch of ground with nothing to connect it to.
    ///
    /// Every beat marker and every named point in the level, sorted by distance
    /// from the box, so the nearest thing the player has already seen is at the
    /// top and the geometry can be chosen against it.
    /// </summary>
    static void Landmarks(StringBuilder sb, Vector3 from)
    {
        var rows = new List<string>();

        foreach (var t in FindAll<Transform>())
        {
            if (t == null) continue;
            string n = t.name;

            // Beat markers, level anchors and anything the beat tools name. Not
            // every transform in a scene of 167 objects and 2129 prefab
            // instances — only the ones that place a beat.
            bool interesting =
                n.StartsWith("L1_", StringComparison.Ordinal) ||
                n.StartsWith("Gully", StringComparison.Ordinal) ||
                n.StartsWith("Beat", StringComparison.Ordinal) ||
                n.StartsWith("Fountain", StringComparison.Ordinal) ||
                n.StartsWith("Statue", StringComparison.Ordinal) ||
                n.StartsWith("Gate", StringComparison.Ordinal) ||
                n.StartsWith("Lever", StringComparison.Ordinal) ||
                n.StartsWith("Crawl", StringComparison.Ordinal) ||
                n.StartsWith("Switch", StringComparison.Ordinal) ||
                n.StartsWith("Tree", StringComparison.Ordinal) ||
                n.StartsWith("SleepingTree", StringComparison.Ordinal) ||
                n == "Ari" || n == "Mono" || n == "Spawn" || n == "SpawnPoint" ||
                n.StartsWith("Checkpoint", StringComparison.Ordinal);

            if (!interesting) continue;

            var p = t.position;
            p.y = 0f;
            rows.Add(Vector3.Distance(from, p).ToString("0.0").PadLeft(7) +
                     " m   " + p.ToString("F2") + "   '" + n + "'" +
                     "  [" + string.Join(", ", Components(t.gameObject)) + "]");
        }

        rows.Sort(StringComparer.Ordinal);

        sb.AppendLine("--- landmarks (" + rows.Count + "), nearest first ---");
        foreach (var r in rows) sb.AppendLine(r);
        sb.AppendLine();
    }

    static IEnumerable<string> Components(GameObject go)
    {
        var names = new List<string>();
        foreach (var c in go.GetComponents<Component>())
        {
            if (c != null) names.Add(c.GetType().Name);
        }
        return names;
    }

    // --- helpers ----------------------------------------------------------------

    static Vector3 Centre() => new Vector3((MinX + MaxX) * 0.5f,
                                           (MinY + MaxY) * 0.5f,
                                           (MinZ + MaxZ) * 0.5f);

    static bool Intersects(Bounds a, Bounds b) => a.Intersects(b);

    static Bounds BoundsOf(Transform t)
    {
        // No renderers, so bounds of the box a child's own renderers would fill
        // are not available. The transform's own sphere is the honest stand-in
        // and it is enough to decide whether an empty is anywhere near.
        // Transform is a non-generic IEnumerable, so `foreach (var ch in t)`
        // hands back object and the recursion will not compile without the cast.
        // Stated here because the error it produces points at BoundsOf.
        var c = new Bounds(t.position, Vector3.zero);
        c.Encapsulate(new Vector3(t.position.x, t.position.y, t.position.z));
        foreach (Transform ch in t) c.Encapsulate(BoundsOf(ch));
        return c;
    }

    static IEnumerable<T> FindAll<T>() where T : Component
    {
        // No FindObjectsSortMode. It is deprecated in this version and the
        // overload that takes it warns on every compile; the surviving one
        // takes the inactive flag and nothing else, which is all this wanted.
        return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
    }

    static string Route(Transform t)
    {
        var parts = new List<string>();
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join(" / ", parts);
    }
}

/// <summary>
/// The component type names on a GameObject, for the empties list.
///
/// An extension because it is needed by exactly one report and putting a
/// fifteen-line helper in the middle of a probe file buries it. It is not on
/// GameObject in Unity, and ReflectionTypeOf is not in this version's BCL
/// surface, so it is written out.
/// </summary>
static class TransformExtensions
{
    public static string GetComponentTypeNames(this Transform t)
    {
        var names = new List<string>();
        foreach (var c in t.GetComponents<Component>())
        {
            if (c == null) { names.Add("MISSING SCRIPT"); continue; }
            names.Add(c.GetType().Name);
        }
        return names.Count > 0 ? "[" + string.Join(", ", names) + "]" : "[]";
    }
}
