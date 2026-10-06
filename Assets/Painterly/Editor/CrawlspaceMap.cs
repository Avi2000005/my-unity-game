using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Global namespace, UnityEngine only, so run_script can compile and run it
// independently of the project assembly.
public static class CrawlspaceMap
{
    const string ReportPath = "Temp/crawlspace_map.txt";

    // --- sampling ---------------------------------------------------------------

    // Same grid as WidthMap, deliberately. That tool found the narrow lanes and
    // this one finds the low ones, and a crawlspace that cannot be compared
    // against the map it was meant to sit next to is not a measurement.
    const float Span = 36f;
    const float Step = 0.5f;
    const float MaxReach = 8f;
    const float From = 30f;
    const float Down = 60f;

    /// <summary>How high a ceiling can be before a cell stops being "low".</summary>
    const float Roofline = 2.2f;

    /// <summary>
    /// How far above a cell's floor the upward ray starts.
    ///
    /// Not zero. Cast from the floor itself and the ray reports a hit a
    /// millimetre later — the ground collider the downward probe just found, or
    /// a thin decal sitting on it — so every cell in the village came back with
    /// a headroom of about one centimetre and the whole map was a grid of
    /// "nothing here is low". Starting above the floor's own surface and adding
    /// the lift back afterwards measures the ceiling without measuring the floor.
    /// </summary>
    const float OffFloor = 0.15f;

    /// <summary>Highest ceiling the sweep will report before calling it open sky.</summary>
    const float MaxHead = 4.0f;

    /// <summary>
    /// Height the width is measured at. Low, because the question is whether a
    /// small companion fits, and a passage that is 1.1 m wide at knee height
    /// and 1.1 m wide at chest height is two different rooms as far as a
    /// crawler is concerned.
    /// </summary>
    const float CrawlChest = 0.35f;

    /// <summary>
    /// Where the ceiling is too low for Ari and low enough for Mono.
    ///
    /// WidthMap answered the wrong question for Beat 4. It mapped how narrow the
    /// village is, and Beat 4's crawlspace is not a narrow space — it is a short
    /// one. Ari's problem is his height, not his shoulders, and a lane 1.0 m wide
    /// under open sky is a lane he walks down without ducking. So this measures
    /// the one thing that decides the beat: how far it is from the floor to the
    /// first thing overhead, at every point in the village.
    ///
    /// The two character heights are measured too and printed first, because the
    /// crawl band is meaningless without them. Everything after that is
    /// reported against them rather than against a number typed in here.
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
        Debug.Log("[Echoes] crawlspace map\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportPath),
                           sb.ToString());
    }

    // --- one cell ----------------------------------------------------------------

    struct Cell
    {
        public int X, Z;
        public float Head;      // floor to first thing overhead
        public float Width;     // narrow axis extent, at crawl height
        public float Ground;
        public bool Void;
        public bool Seen;
    }

    static void Body(StringBuilder sb)
    {
        Physics.SyncTransforms();

        sb.AppendLine("WHERE THIS VILLAGE IS LOW");
        sb.AppendLine();

        float ari = Character(sb, "Ari", "Ari");
        sb.AppendLine();
        float mono = Character(sb, "Mono", "Mono");

        sb.AppendLine();
        if (ari > 0f && mono > 0f)
        {
            sb.AppendLine("  so a passage is Mono-only if its headroom is under " +
                          ari.ToString("0.00") + " m (Ari) and at least " +
                          mono.ToString("0.00") + " m (Mono), a window of " +
                          (ari - mono).ToString("0.00") + " m.");
        }
        else
        {
            sb.AppendLine("  one or both heights are unknown, so the band below is a " +
                          "guess and the cells have to be measured by hand.");
        }

        sb.AppendLine();
        sb.AppendLine("grid +/- " + Span + " m at " + Step + " m, ceiling read as the " +
                      "first thing above " + OffFloor.ToString("0.00") + " m of each " +
                      "cell's own floor, width read at " + CrawlChest.ToString("0.00") +
                      " m up");

        int n = Mathf.RoundToInt(Span * 2f / Step) + 1;
        var grid = new Cell[n, n];
        int voidCells = 0, cells = 0, lowCells = 0;

        for (int xi = 0; xi < n; xi++)
        {
            for (int zi = 0; zi < n; zi++)
            {
                var at = new Vector3(-Span + xi * Step, 0f, -Span + zi * Step);
                var c = new Cell { X = xi, Z = zi };

                if (!Floor(at, out c.Ground)) { c.Void = true; voidCells++; }
                else
                {
                    c.Head = Headroom(at, c.Ground);
                    c.Width = WidthAt(at, c.Ground);
                    cells++;
                    if (c.Head < Roofline) lowCells++;
                }

                grid[xi, zi] = c;
            }
        }

        sb.AppendLine("cells with a floor: " + cells + "   with nothing: " + voidCells +
                      "   low enough to matter: " + lowCells);
        sb.AppendLine();

        // --- the map -------------------------------------------------------------

        sb.AppendLine("--- headroom map ---");
        sb.AppendLine("  ' ' no floor    _ under Ari   = Ari-Mono   - 1.2-1.6   + 1.6-2.2");
        sb.AppendLine("  O open sky (over " + Roofline.ToString("0.0") + " m)");
        sb.AppendLine();

        for (int zi = n - 1; zi >= 0; zi--)
        {
            var line = new StringBuilder(n);
            for (int xi = 0; xi < n; xi++) line.Append(Band(grid[xi, zi], ari, mono));
            sb.AppendLine("  " + line.ToString());
        }

        sb.AppendLine();
        sb.AppendLine("--- low regions (headroom under " + Roofline.ToString("0.0") +
                      " m, in runs of at least 6 cells = 1.5 m2) ---");

        var regions = LowRegions(grid, n);
        sb.AppendLine("found: " + regions.Count);

        if (regions.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("NOT ONE. There is no covered space in this village at all —");
            sb.AppendLine("no cell anywhere has a ceiling under " +
                          Roofline.ToString("0.0") + " m.");
            sb.AppendLine();
            sb.AppendLine("That is the finding that shapes Beat 4: the crawlspace has to");
            sb.AppendLine("be BUILT, and the map above is where to build it. Anything with");
            sb.AppendLine("a wall on two sides and something overhead already — a lean-to");
            sb.AppendLine("against a house, the underside of a loading dock, the gap under");
            sb.AppendLine("a collapsed floor — can be roofed over to make one.");
            return;
        }

        for (int i = 0; i < regions.Count; i++)
        {
            sb.AppendLine();
            Report(sb, grid, regions[i], i + 1, regions.Count, ari, mono);
        }
    }

    // --- the two characters ------------------------------------------------------

    /// <summary>
    /// How tall a character stands, and how he stands.
    ///
    /// The collider first and the visual bounds second, because they disagree
    /// and which one is right depends on what is being asked. The collider is
    /// what Ari actually collides with, so it is what decides whether he fits;
    /// the mesh is what the player sees, so it is what decides whether fitting
    /// looks like the character squeezed in. A crawlspace sized to the collider
    /// and not the mesh reads as his head going through the ceiling.
    /// </summary>
    static float Character(StringBuilder sb, string label, string goName)
    {
        sb.AppendLine("--- " + label + " ---");

        var go = GameObject.Find(goName);
        if (go == null)
        {
            sb.AppendLine("  no GameObject called '" + goName +
                          "'. Cannot measure. Is the level still built?");
            return 0f;
        }

        float best = 0f;

        // Ari is special-cased: his body is not a collider at all.
        if (label == "Ari")
        {
            float step;
            string note;
            float swept = AriBody(go.transform, out step, out note);
            sb.AppendLine("  swept body: " + swept.ToString("0.00") + " m tall (" + note + ")");
            if (step > 0f)
                sb.AppendLine("  he can walk up a " + step.ToString("0.00") +
                              " m ledge without jumping, so anything meant to stop" +
                              " his has to be taller than that");
            best = swept;
        }
        else
        {
            var caps = go.GetComponentsInChildren<CapsuleCollider>(true);
            var ccs = go.GetComponentsInChildren<CharacterController>(true);

            if (caps.Length > 0 || ccs.Length > 0)
            {
                foreach (var c in ccs)
                {
                    float st = Standing(c.height, c.center.y, c.transform);
                    sb.AppendLine("  CharacterController on '" + c.gameObject.name +
                                  "': local height " + c.height.ToString("0.00") +
                                  " m, world " + st.ToString("0.00") + " m");
                    best = Mathf.Max(best, st);
                }

                foreach (var c in caps)
                {
                    float st = Standing(c.height, c.center.y, c.transform);
                    sb.AppendLine("  CapsuleCollider on '" + c.gameObject.name +
                                  "': local height " + c.height.ToString("0.00") +
                                  " m, local radius " + c.radius.ToString("0.00") +
                                  " m, scale " + Mathf.Abs(c.transform.lossyScale.y).ToString("0.00") +
                                  "  ->  stands " + st.ToString("0.00") +
                                  " m, " +
                                  (2f * c.radius * Mathf.Abs(c.transform.lossyScale.x)
                                   ).ToString("0.00") + " m wide" +
                                  (c.enabled ? "" : "  [DISABLED]"));
                    best = Mathf.Max(best, st);
                }
            }
            else
            {
                sb.AppendLine("  no CapsuleCollider or CharacterController anywhere in" +
                              " the hierarchy.");
            }
        }

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                var b = r.bounds;
                if (b.min.y < minY) minY = b.min.y;
                if (b.max.y > maxY) maxY = b.max.y;
            }
            if (minY < maxY)
            {
                sb.AppendLine("  visible mesh: " + maxY.ToString("0.00") + " m to " +
                              minY.ToString("0.00") + " m, so " +
                              (maxY - minY).ToString("0.00") + " m tall, " +
                              renderers.Length + " renderer(s)");
                if (best > 0f && maxY - minY > best + 0.02f)
                    sb.AppendLine("  NOTE the mesh is " +
                                  ((maxY - minY) - best).ToString("0.00") +
                                  " m taller than the body. A crawlspace sized to " +
                                  "the body alone would be tall enough to admit his " +
                                  "and short enough to clip his head, which is the " +
                                  "one combination that looks broken.");
            }
        }
        else
        {
            sb.AppendLine("  no active Renderer in the hierarchy — asleep, hidden, or " +
                          "not yet placed.");
        }

        if (best <= 0f) sb.AppendLine("  HEIGHT UNKNOWN (0 m). Do not trust the band map.");
        return best;
    }

    /// <summary>
    /// How tall a capsule stands, in world metres.
    ///
    /// Two things are easy to get wrong here and both were.
    ///
    /// The capsule spans centre.y plus or minus half its height. The radius is
    /// how wide it is and has no place in its vertical extent — folding it in
    /// adds the lower cap a second time and makes everyone half a metre taller
    /// than they are.
    ///
    /// And every one of those numbers is LOCAL. Mono is scaled to 0.31, so his
    /// 1.80 m capsule is 0.56 m of actual world, and a probe that reported 1.80
    /// concluded he was taller than Ari and that Beat 4 was impossible — when
    /// the real answer is that the band is over a metre wide and the beat is
    /// easy.
    /// </summary>
    static float Standing(float height, float centreY, Transform owner)
    {
        float scale = owner != null ? Mathf.Abs(owner.lossyScale.y) : 1f;
        return Mathf.Max(0f, (centreY + height * 0.5f) * scale);
    }

    /// <summary>
    /// Ari's real body, asked of the component that owns it.
    ///
    /// By reflection, because this tool is compiled on its own with no reference
    /// to the project's own assembly. Ari has no collider at all — his body is a
    /// swept capsule cast inside AriMover — so a tool that only looks for
    /// colliders finds nothing and concludes he walks through walls.
    ///
    /// Returns 0 when there is no such component or no such member, and says
    /// which, rather than returning a null and leaving the caller to print it.
    /// </summary>
    static float AriBody(Transform t, out float stepHeight, out string note)
    {
        stepHeight = 0f;
        note = "";

        if (t == null) { note = "no Ari in the scene"; return 0f; }

        var comp = t.GetComponent("AriMover");
        if (comp == null)
        {
            var all = t.GetComponents<MonoBehaviour>();
            foreach (var m in all) if (m != null && m.GetType().Name == "AriMover") comp = m;
        }

        if (comp == null)
        {
            note = "no AriMover on him, so his swept body is unknown and this map " +
                   "cannot say what he fits through";
            return 0f;
        }

        var type = comp.GetType();
        var height = type.GetProperty("BodyHeight");
        if (height == null)
        {
            note = "AriMover has no BodyHeight property. It is the swept capsule's " +
                   "total height and without it this tool can only guess.";
            return 0f;
        }

        var step = type.GetProperty("StepHeight");
        if (step != null) stepHeight = Convert.ToSingle(step.GetValue(comp));

        note = "from AriMover's swept capsule, not a collider: he has no collider, " +
               "the physics knows him as a cast";
        return Convert.ToSingle(height.GetValue(comp));
    }

    // --- measuring ---------------------------------------------------------------

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
    /// Floor to the first thing overhead.
    ///
    /// The first hit and not the nearest collider's bounds: a wall beside the
    /// cell casts no shadow in an upward ray, but a sloping roof does, and a
    /// crawlspace under a pitched roof has to be measured at its tightest point
    /// rather than its average or it will not fit at the far end.
    ///
    /// MaxHead when nothing is hit, which is open sky. Reported as a number and
    /// not as a sentinel, so the arithmetic downstream stays ordinary.
    /// </summary>
    static float Headroom(Vector3 at, float groundY)
    {
        var from = new Vector3(at.x, groundY + OffFloor, at.z);

        // The lowest of every hit, not the first. A cell under a lean-to that
        // has a beam over a roof over a shutter has three ceilings and the
        // crawlspace is governed by the lowest of them; taking the first hit
        // along the ray would usually be right but not always, and "usually"
        // is how a passage gets built one beam too high.
        var hits = Physics.RaycastAll(from, Vector3.up, MaxHead, ~0,
                                      QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return MaxHead;

        float lowest = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null) continue;
            if (hits[i].point.y < lowest) lowest = hits[i].point.y;
        }

        return lowest == float.MaxValue ? MaxHead : Mathf.Min(MaxHead, lowest - groundY);
    }

    static float WallDistance(Vector3 from, Vector3 dir)
    {
        return Physics.Raycast(from, dir, out RaycastHit hit, MaxReach, ~0,
                               QueryTriggerInteraction.Ignore)
            ? hit.distance
            : MaxReach;
    }

    static float WidthAt(Vector3 at, float groundY)
    {
        var from = new Vector3(at.x, groundY + CrawlChest, at.z);
        return Mathf.Min(WallDistance(from, Vector3.right) + WallDistance(from, Vector3.left),
                         WallDistance(from, Vector3.forward) + WallDistance(from, Vector3.back));
    }

    static char Band(Cell c, float ari, float mono)
    {
        if (c.Void) return ' ';
        float h = c.Head;

        if (h < mono * 0.5f) return '!';      // tighter than Mono too: not a passage
        if (ari > 0f && h < ari) return '_';  // Ari cannot pass
        if (ari > 0f && mono > 0f && h < ari + 0.4f) return '=';
        if (h < 1.6f) return '-';
        if (h < Roofline) return '+';
        if (h < MaxHead - 0.01f) return 'O';
        return ' ';
    }

    // --- regions -----------------------------------------------------------------

    class Region { public List<Cell> Cells = new List<Cell>(); }

    static List<Region> LowRegions(Cell[,] grid, int n)
    {
        var regions = new List<Region>();

        for (int xi = 0; xi < n; xi++)
        {
            for (int zi = 0; zi < n; zi++)
            {
                var start = grid[xi, zi];
                if (start.Void || start.Seen) continue;
                if (start.Head >= Roofline) continue;

                var r = new Region();
                Grow(grid, n, xi, zi, r);

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
                    if (c.Void || c.Seen || c.Head >= Roofline) continue;

                    c.Seen = true;
                    grid[nx, nz] = c;
                    queue.Enqueue(nx * 1000 + nz);
                }
            }
        }
    }

    // --- one region, in full ----------------------------------------------------

    static void Report(StringBuilder sb, Cell[,] grid, Region r, int index, int total,
                       float ari, float mono)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        float mx = 0f, mz = 0f;
        var heads = new List<float>();
        var widths = new List<float>();

        foreach (var c in r.Cells)
        {
            float x = -Span + c.X * Step;
            float z = -Span + c.Z * Step;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (z < minZ) minZ = z;
            if (z > maxZ) maxZ = z;
            mx += x; mz += z;
            heads.Add(c.Head);
            widths.Add(c.Width);
        }
        mx /= r.Cells.Count; mz /= r.Cells.Count;

        heads.Sort(); widths.Sort();

        sb.AppendLine("region " + index + " of " + total + ": " + r.Cells.Count +
                      " cells, " + (r.Cells.Count * Step * Step).ToString("0.0") +
                      " m2, box x " + minX.ToString("0.0") + " to " + maxX.ToString("0.0") +
                      ", z " + minZ.ToString("0.0") + " to " + maxZ.ToString("0.0") +
                      ", about (" + mx.ToString("0.0") + ", " + mz.ToString("0.0") + ")");

        sb.AppendLine("  headroom: median " + heads[heads.Count / 2].ToString("0.00") +
                      " m, min " + heads[0].ToString("0.00") +
                      " m, max " + heads[heads.Count - 1].ToString("0.00") + " m");
        sb.AppendLine("  width at " + CrawlChest.ToString("0.00") + " m up: median " +
                      widths[widths.Count / 2].ToString("0.00") + " m, min " +
                      widths[0].ToString("0.00") + " m");

        if (ari <= 0f || mono <= 0f) return;

        float lo = heads[0], mid = heads[heads.Count / 2];
        bool loBlocksAri = lo < ari, midBlocksAri = mid < ari;
        bool fitsMono = lo >= mono;

        if (loBlocksAri && fitsMono)
            sb.AppendLine("  USABLE AS A CRAWLSPACE as it stands: even its lowest point " +
                          "(" + lo.ToString("0.00") + " m) is under " +
                          ari.ToString("0.00") + " m so Ari cannot get through, and " +
                          "over " + mono.ToString("0.00") + " m so Mono can.");
        else if (midBlocksAri)
            sb.AppendLine("  HALF RIGHT. Ari fits in the middle (" +
                          mid.ToString("0.00") + " m) but not the low end (" +
                          lo.ToString("0.00") + " m). Roof the tight end over to make " +
                          "the whole run Mono-only.");
        else
            sb.AppendLine("  NOT A CRAWLSPACE. Ari stands " + ari.ToString("0.00") +
                          " m and the lowest ceiling here is " + lo.ToString("0.00") +
                          " m. He would walk straight through.");

        if (widths[0] < mono + 0.3f)
            sb.AppendLine("  and the narrowest point is " + widths[0].ToString("0.00") +
                          " m wide, which may be too tight for Mono to turn in. " +
                          "Widen it before trusting the height.");
    }
}
