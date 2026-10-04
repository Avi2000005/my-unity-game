using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

// Global namespace, UnityEngine only, so run_script can compile and run it
// while the project assembly is still settling.
public static class GroundProfile
{
    const string Report = "Temp/ground_profile.txt";

    /// <summary>Half-width of the sampled area, in metres.</summary>
    const float Span = 42f;

    /// <summary>Spacing between samples. 1 m over an 84 m square is 7 000
    /// raycasts, which is nothing, and it resolves kerbs and steps that a
    /// coarser grid would step straight over.</summary>
    const float Step = 1f;

    const float From = 60f;
    const float Down = 120f;

    /// <summary>
    /// Measures where the ground of this village actually is.
    ///
    /// Written because two tools have now disagreed about it in a way that
    /// stops work: a downward raycast at the square's origin reports y = 1.22
    /// and one under Ari reports y = 2.27, while Ari herself stands at 0.25 and
    /// every one of the 2 151 colliders in the scene is floor, wall or roof.
    /// Both numbers are a downward ray finding the top of something, and the
    /// origin happens to be the fountain's rim while Ari has an awning over her.
    ///
    /// So rather than pick a better reference point, this measures the whole
    /// village and reports the distribution of surface heights. Where there is
    /// one dominant height, that is the ground and everything else is a roof, a
    /// ledge or a fountain. Where there is not, the village is terraced and a
    /// single reference height is the wrong idea entirely and the placement
    /// tool needs to compare against the local ground instead.
    ///
    /// Each sample also counts what is above it, because a point whose only
    /// surface is 2 m overhead is a courtyard and not a roof, and the two
    /// demand opposite treatment.
    /// </summary>
    public static void Run()
    {
        var sb = new StringBuilder();

        try
        {
            Body(sb);
        }
        catch (Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] ground profile\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        Physics.SyncTransforms();

        sb.AppendLine("WHERE THE GROUND OF THIS VILLAGE IS");
        sb.AppendLine("at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("grid +/- " + Span + " m at " + Step + " m, ray from y = " +
                      From + " down " + Down + " m");
        sb.AppendLine();

        var tops = new List<float>();
        var open = new List<float>();      // samples with nothing overhead
        var covered = 0;
        var bare = 0;
        var noHit = 0;

        var rays = new RaycastHit[16];
        var counts = new Dictionary<int, int>();

        for (float x = -Span; x <= Span; x += Step)
        {
            for (float z = -Span; z <= Span; z += Step)
            {
                var origin = new Vector3(x, From, z);
                int n = Physics.RaycastNonAlloc(origin, Vector3.down, rays, Down,
                                               ~0, QueryTriggerInteraction.Ignore);

                if (n == 0) { noHit++; continue; }

                // NonAlloc does not promise order, so sort. The topmost hit is
                // the surface a character would stand on; anything deeper is
                // only interesting as evidence that the point is elevated.
                var hits = rays.Take(n)
                    .Where(h => h.collider != null)
                    .OrderByDescending(h => h.point.y)
                    .ToArray();

                if (hits.Length == 0) { noHit++; continue; }

                float top = hits[0].point.y;
                tops.Add(top);

                // Is there floor under this point at all, or is the topmost
                // surface hanging over nothing?
                bool hasFloor = hits.Any(h => h.point.y < top - 0.5f);

                if (!hasFloor) bare++;

                // Open sky: nothing within 3 m above the surface. The awning
                // over Ari and the fountain rim both fail this.
                bool sky = !hits.Any(h => h.point.y > top + 0.05f && h.point.y < top + 3f);

                if (sky) { open.Add(top); covered++; }
                else covered++;

                int bucket = Mathf.RoundToInt(top);
                counts.TryGetValue(bucket, out int c);
                counts[bucket] = c + 1;
            }
        }

        sb.AppendLine("samples with a surface: " + tops.Count +
                      "   none at all: " + noHit +
                      "   open sky: " + open.Count +
                      "   with something overhead: " + (covered - open.Count));
        sb.AppendLine("surfaces with nothing underneath (hanging): " + bare);
        sb.AppendLine();

        sb.AppendLine("--- every surface height, by the metre ---");
        foreach (var kv in counts.OrderBy(k => k.Key))
            sb.AppendLine("  y = " + kv.Key.ToString().PadLeft(4) +
                          "  " + Bar(kv.Value) + " " + kv.Value);
        sb.AppendLine();

        sb.AppendLine("--- open-sky surface heights only (the ones a character " +
                      "could stand on) ---");
        var openCounts = new Dictionary<int, int>();
        foreach (var t in open)
        {
            int b = Mathf.RoundToInt(t);
            openCounts.TryGetValue(b, out int c);
            openCounts[b] = c + 1;
        }
        foreach (var kv in openCounts.OrderBy(k => k.Key))
            sb.AppendLine("  y = " + kv.Key.ToString().PadLeft(4) +
                          "  " + Bar(kv.Value) + " " + kv.Value);
        sb.AppendLine();

        if (openCounts.Count == 0)
        {
            sb.AppendLine("NOTHING IS OPEN SKY. Either the ray start is below the");
            sb.AppendLine("roofs, or the village is enclosed. Raise From and re-run.");
            return;
        }

        int modal = openCounts.OrderByDescending(k => k.Value).First().Key;
        int total = open.Count;

        sb.AppendLine("MOST COMMON OPEN-SKY HEIGHT: y = " + modal +
                      " (" + (100f * openCounts[modal] / total).ToString("0") +
                      "% of " + total + " open samples)");
        sb.AppendLine("That is the village floor. Anything far from it is a roof.");
        sb.AppendLine();

        float spread = 0f;
        int near = 0;
        foreach (var t in open)
        {
            if (Mathf.Abs(t - modal) <= 0.5f) near++;
        }
        spread = 100f * near / total;
        sb.AppendLine("within +/- 0.5 m of it: " + spread.ToString("0") + "%");
        if (spread < 70f)
        {
            sb.AppendLine("Below 70% the village is terraced or stepped, and a single");
            sb.AppendLine("reference height will reject valid ground. Placement must");
            sb.AppendLine("then compare a candidate against the ground beside the path");
            sb.AppendLine("to it, not against one number for the whole level.");
        }

        // Named spots, because "the floor is at y" and "the player spawns at y"
        // are different questions and only the second one matters for a beat.
        sb.AppendLine();
        sb.AppendLine("--- named spots ---");
        foreach (var spot in new[]
        {
            new { Name = "square origin (fountain)", At = new Vector3(0f, 0f, 0f) },
            new { Name = "Ari", At = new Vector3(7f, 0.25f, 7f) },
            new { Name = "house", At = new Vector3(25f, 0f, -8f) },
            new { Name = "alley mouth", At = new Vector3(0f, 0f, 12f) },
            new { Name = "back lane", At = new Vector3(-8f, 0f, -14f) },
        })
        {
            var hits = Physics.RaycastAll(new Vector3(spot.At.x, From, spot.At.z),
                                          Vector3.down, Down, ~0,
                                          QueryTriggerInteraction.Ignore)
                .Where(h => h.collider != null)
                .OrderByDescending(h => h.point.y)
                .ToArray();

            if (hits.Length == 0) { sb.AppendLine("  " + spot.Name + ": nothing"); continue; }

            string above = hits.Length > 1
                ? ", then " + hits[1].point.y.ToString("F2") + " ('" + hits[1].collider.name + "')"
                : "";

            sb.AppendLine("  " + spot.Name.PadRight(24) + " top y = " +
                          hits[0].point.y.ToString("F2").PadLeft(6) +
                          "  '" + hits[0].collider.name + "'" + above);
        }
    }

    static string Bar(int n)
    {
        int len = (int)Math.Round(n / 40.0);
        return new string('#', Math.Max(1, Math.Min(60, len)));
    }
}
