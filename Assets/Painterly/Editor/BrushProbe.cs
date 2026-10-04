using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Measures whether Ari's brush can actually reach anything.
    ///
    /// The failure being diagnosed reported "[Brush] stroke at 0 target(s)" on
    /// every click. Two things can produce that: the raycast missing entirely
    /// (no collider under the cursor), or landing somewhere more than radius
    /// metres from every ColorRestoreTarget. Those need opposite fixes, so the
    /// probe reports them separately rather than collapsing them into one
    /// number.
    ///
    /// A grid of screen points is sampled rather than a single centre pixel,
    /// because "works when aimed at the middle of a wall but nowhere else" is a
    /// real and common outcome that one sample would hide.
    ///
    /// Runs in edit mode — Physics.Raycast sees scene colliders without play
    /// mode, so this costs no domain reload into play and cannot disturb a
    /// session the user is testing in. Writes Temp/brush_probe.txt.
    /// </summary>
    public static class BrushProbe
    {
        const int Grid = 7;                 // 7x7 = 49 sample points
        const float Radius = 6f;            // must match BrushPainter.radius
        const float MaxDistance = 250f;     // must match BrushPainter.maxRayDistance

        [MenuItem("Tools/Echoes/Probe Brush", priority = 70)]
        public static void Run()
        {
            var sb = new StringBuilder();
            try { RunInner(sb); }
            catch (System.Exception e) { sb.AppendLine("FAILED: " + e); }
            finally
            {
                System.IO.File.WriteAllText("Temp/brush_probe.txt", sb.ToString());
                Debug.Log("[Echoes] Brush probe\n" + sb);
            }
        }

        static void RunInner(StringBuilder sb)
        {
            var cam = Camera.main;
            if (cam == null) { sb.AppendLine("no Camera.main"); return; }

            var targets = Object.FindObjectsByType<ColorRestoreTarget>(
                FindObjectsInactive.Exclude);

            sb.AppendLine($"camera: {cam.name} pos={cam.transform.position}");
            sb.AppendLine($"ColorRestoreTarget count: {targets.Length}");
            sb.AppendLine($"brush radius: {Radius}   max ray: {MaxDistance}");
            sb.AppendLine($"scene colliders: {Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude).Length}");
            sb.AppendLine();

            if (targets.Length == 0)
            {
                sb.AppendLine("NO TARGETS IN SCENE — every stroke returns 0 regardless of aim.");
                return;
            }

            int misses = 0, hitNoTarget = 0, hitWithTarget = 0, closestMiss = 0;
            float closestSeen = float.MaxValue;

            sb.AppendLine("screen(u,v) | hit collider | hit point | nearest target | within r | n");

            for (int y = 0; y < Grid; y++)
            {
                for (int x = 0; x < Grid; x++)
                {
                    // Inclusive 0..1 so the edges of the frame are sampled too;
                    // sampling only the interior would report a healthier result
                    // than the player actually sees.
                    float u = Grid == 1 ? 0.5f : x / (float)(Grid - 1);
                    float v = Grid == 1 ? 0.5f : y / (float)(Grid - 1);
                    Vector3 screen = new Vector3(u * cam.pixelWidth, v * cam.pixelHeight, 0f);

                    Ray ray = cam.ScreenPointToRay(screen);
                    if (!Physics.Raycast(ray, out RaycastHit hit, MaxDistance))
                    {
                        misses++;
                        sb.AppendLine($"({u:0.00},{v:0.00}) | MISS | - | - | - | 0");
                        continue;
                    }

                    float nearest = NearestDistance(targets, hit.point);
                    int within = CountWithin(targets, hit.point, Radius);

                    if (nearest < closestSeen) closestSeen = nearest;

                    if (within == 0)
                    {
                        hitNoTarget++;
                        if (nearest > Radius) closestMiss++;
                    }
                    else
                    {
                        hitWithTarget++;
                    }

                    sb.AppendLine($"({u:0.00},{v:0.00}) | {hit.collider.name} | " +
                                  $"{hit.point} | {nearest:0.0}m | " +
                                  $"{(nearest <= Radius ? "YES" : "NO")} | {within}");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"raycast MISSED entirely : {misses}/{Grid * Grid}");
            sb.AppendLine($"hit but 0 targets in r  : {hitNoTarget}/{Grid * Grid}");
            sb.AppendLine($"hit WITH targets in r   : {hitWithTarget}/{Grid * Grid}");
            sb.AppendLine($"nearest was beyond r    : {closestMiss}/{Grid * Grid}");
            sb.AppendLine($"closest target seen     : " +
                          (closestSeen == float.MaxValue ? "n/a" : closestSeen + "m") +
                          $"   (brush reaches {Radius}m)");

            sb.AppendLine();
            if (hitWithTarget > 0)
                sb.AppendLine("DIAGNIS: painting WORKS on the sampled points above.");
            else if (misses == Grid * Grid)
                sb.AppendLine("DIAGNIS: raycast hits nothing — colliders still missing.");
            else
                sb.AppendLine("DIAGNIS: raycasts land, but every hit is outside the brush radius.");
        }

        static float NearestDistance(ColorRestoreTarget[] targets, Vector3 point)
        {
            float best = float.MaxValue;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;

                // Must mirror ColorRestoreTarget.RestoreInRadius exactly: distance
                // to the owned volume, not to the transform pivot. Measuring
                // anything else here would report a different number than the one
                // the brush actually acts on.
                float d = Mathf.Sqrt(targets[i].WorldBounds.SqrDistance(point));
                if (d < best) best = d;
            }
            return best;
        }

        static int CountWithin(ColorRestoreTarget[] targets, Vector3 point, float radius)
        {
            int n = 0;
            float sqr = radius * radius;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                if (targets[i].WorldBounds.SqrDistance(point) <= sqr) n++;
            }
            return n;
        }
    }
}
