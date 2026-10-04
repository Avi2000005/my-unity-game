using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Echoes.Painterly;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TreeSightline
{
    const string ReportPath = "Temp/tree_sightline.txt";

    /// <summary>How far the eye sits above the floor. Ari's capsule is 1.80 m
    /// and she is thirteen, so this is her eye, not an adult's.</summary>
    const float Eye = 1.60f;

    /// <summary>Height the sight line aims at — the middle of the stump, which
    /// is what the player is being asked to look at.</summary>
    const float AimAt = 0.80f;

    /// <summary>How far out the pretend camera stands, and how many of them.</summary>
    const float RingRadius = 3.0f;
    const int RingCount = 8;

    /// <summary>How many of the ring must see the tree before it counts as
    /// visible. Six of eight, so the tree can hide behind the one thing the
    /// camera happens to be looking past without failing outright.</summary>
    const int NeedVisible = 6;

    /// <summary>Where the tree is allowed to sit, as a distance from Mono. He
    /// is asleep beside it, and the burst has to reach him.</summary>
    const float MinFromMono = 1.2f;
    const float MaxFromMono = 2.2f;

    public static void Run()
    {
        var sb = new StringBuilder();

        try { Body(sb); }
        catch (Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] tree sight line\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportPath),
                           sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded) { sb.AppendLine("No scene is open."); return; }

        Physics.SyncTransforms();

        var tree = UnityEngine.Object.FindAnyObjectByType<SleepingTree>();
        if (tree == null)
        {
            sb.AppendLine("No SleepingTree in the level. Run Beat 3 setup first.");
            return;
        }

        var mono = MonoCompanion.FindInLevel();
        if (mono == null) { sb.AppendLine("No Mono in the level."); return; }

        var ari = GameObject.Find("Ari");
        var treeRoot = tree.transform.root;

        sb.AppendLine("CAN ARI SEE THE TREE SHE HAS TO TOUCH");
        sb.AppendLine();
        sb.AppendLine("Beat 3 asks the player to swing a brush at a grey stump. If " +
                      "the stump is behind a fence from every angle the camera can " +
                      "hold, the beat cannot be started and nothing about it is " +
                      "wrong except that it is invisible. The ring below is eight " +
                      "places a follow camera can stand, " + RingRadius.ToString("0.0") +
                      " m out and " + Eye.ToString("0.00") + " m up, which is where " +
                      "it actually ends up while she walks up to it.");
        sb.AppendLine();

        var treeAt = tree.transform.position;
        int before = Visible(sb, "as built", treeAt, treeRoot, ari);

        if (before >= NeedVisible)
        {
            sb.AppendLine("PASS — " + before + " of " + RingCount +
                          " camera positions can see it.");
            return;
        }

        sb.AppendLine("FAIL — only " + before + " of " + RingCount +
                      " can see it. Looking for somewhere better.");
        sb.AppendLine();

        // --- search -----------------------------------------------------------

        var monoAt = mono.transform.position;
        var ground = GroundAt(treeAt);

        var best = (score: int.MinValue, pos: Vector3.zero, visible: 0, note: "");

        for (int a = 0; a < 24; a++)
        {
            float ang = a / 24f * Mathf.PI * 2f;
            for (int r = 0; r <= 2; r++)
            {
                float dist = Mathf.Lerp(MinFromMono, MaxFromMono, r / 2f);
                var at = new Vector3(monoAt.x + Mathf.Cos(ang) * dist, 0f,
                                     monoAt.z + Mathf.Sin(ang) * dist);
                at.y = GroundAt(at);
                if (at.y < -900f) continue;

                // Must be standable in its own right, not a roof or a kerb.
                if (Blocked(at)) continue;

                int vis = Visible(null, "", at, treeRoot, ari);

                // Prefer what a player would call a better spot: more of the ring
                // can see it, then being closer to where she starts, then being
                // squarely in front of her rather than behind her.
                float score = vis * 10f;
                if (ari != null)
                    score -= Vector3.Distance(at, ari.transform.position) * 0.3f;
                score += ground < -900f ? 0f : 2f;

                if (score <= best.score) continue;

                best = ((int)score, at, vis,
                        Mathf.RoundToInt(dist * 100f) / 100f + " m from Mono, " +
                        Mathf.RoundToInt(Vector3.Distance(at, monoAt) * 100f) / 100f + " m");
            }
        }

        if (best.score == int.MinValue)
        {
            sb.AppendLine("NOWHERE BETTER. Every candidate spot around Mono is " +
                          "blocked, or is further from the camera than the stump " +
                          "already is. The tree is hidden by something structural " +
                          "and moving it will not help — the obstruction has to go.");
            return;
        }

        sb.AppendLine("best alternative: " + best.pos.ToString("F2") + "  (" +
                      best.note + ")");
        sb.AppendLine("  visible from " + best.visible + " of " + RingCount +
                      " camera positions, against " + before + " where it is now");

        if (best.visible <= before)
        {
            sb.AppendLine("  NOT BETTER. Leaving the tree where it is; moving it " +
                          "for a worse view is churn.");
            return;
        }

        // --- move it ----------------------------------------------------------

        var offset = best.pos - treeAt;

        // The whole hierarchy moves, touch point included, because SleepingTree
        // reads its point off touchPoint and leaving that behind would put the
        // brush target where the stump used to be.
        foreach (Transform t in treeRoot.GetComponentsInChildren<Transform>(true))
            t.position += offset;

        Physics.SyncTransforms();

        int after = Visible(sb, "after moving", best.pos, treeRoot, ari);

        sb.AppendLine();
        sb.AppendLine("moved the tree and its touch point " +
                      offset.magnitude.ToString("0.00") + " m to " +
                      best.pos.ToString("F2"));
        sb.AppendLine("VERDICT " + (after >= NeedVisible ? "PASS" : "STILL FAIL") +
                      " — " + after + " of " + RingCount + " can see it" +
                      (after < NeedVisible
                          ? ". Something structural is in the way and it has to be " +
                            "moved or the camera has to be reframed."
                          : "."));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("scene saved.");
    }

    // --- the test ----------------------------------------------------------------

    /// <summary>
    /// How many of the ring can see the tree, and what is in the way of the rest.
    /// </summary>
    static int Visible(StringBuilder sb, string label, Vector3 treeAt,
                       Transform treeRoot, GameObject ari)
    {
        int seen = 0;
        var blockers = new Dictionary<string, int>();
        var lines = new List<string>();

        for (int i = 0; i < RingCount; i++)
        {
            float ang = i / (float)RingCount * Mathf.PI * 2f;
            var eye = new Vector3(treeAt.x + Mathf.Cos(ang) * RingRadius, 0f,
                                  treeAt.z + Mathf.Sin(ang) * RingRadius);
            float gy = GroundAt(eye);
            if (gy < -900f) { lines.Add("    " + i + ": no floor to stand on"); continue; }

            eye.y = gy + Eye;
            var aim = new Vector3(treeAt.x, GroundAt(treeAt) + AimAt, treeAt.z);

            var dir = aim - eye;
            float dist = dir.magnitude;
            if (dist < 0.05f) { seen++; continue; }

            bool clear = !Physics.Raycast(eye, dir / dist, out RaycastHit hit, dist - 0.1f,
                                          ~0, QueryTriggerInteraction.Ignore)
                         || IsTreeOrPlayer(hit.collider, treeRoot, ari);

            if (clear)
            {
                seen++;
                lines.Add("    " + i + " at " + RingRadius.ToString("0.0") +
                          " m: sees it");
            }
            else
            {
                blockers.TryGetValue(hit.collider.name, out int n);
                blockers[hit.collider.name] = n + 1;
                lines.Add("    " + i + " at " + RingRadius.ToString("0.0") +
                          " m: blocked by '" + hit.collider.name + "' at " +
                          hit.distance.ToString("0.00") + " m");
            }
        }

        if (sb != null)
        {
            sb.AppendLine("--- sight line " + label + " ---");
            foreach (var l in lines) sb.AppendLine(l);
            sb.AppendLine("  visible from " + seen + " of " + RingCount +
                          " camera positions" +
                          (seen >= NeedVisible ? "  PASS" : "  FAIL, needs " + NeedVisible));
        }

        return seen;
    }

    /// <summary>
    /// A hit on the tree itself, or on Ari, is not an obstruction. The tree is
    /// what is being looked at and Ari is standing in her own shot.
    /// </summary>
    static bool IsTreeOrPlayer(Collider col, Transform treeRoot, GameObject ari)
    {
        if (col == null) return true;

        var t = col.transform;
        while (t != null)
        {
            if (t == treeRoot) return true;
            if (ari != null && t == ari.transform) return true;
            t = t.parent;
        }

        return false;
    }

    static bool Blocked(Vector3 feet)
    {
        return Physics.OverlapCapsuleNonAlloc(
            feet + Vector3.up * 0.40f, feet + Vector3.up * 1.50f, 0.30f,
            Buffer, ~0, QueryTriggerInteraction.Ignore) > 0;
    }

    static readonly Collider[] Buffer = new Collider[8];

    static float GroundAt(Vector3 at)
    {
        var hits = Physics.RaycastAll(at + Vector3.up * 30f, Vector3.down, 60f, ~0,
                                      QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return -999f;

        float lowest = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
            if (hits[i].collider != null && hits[i].point.y < lowest)
                lowest = hits[i].point.y;

        return lowest == float.MaxValue ? -999f : lowest;
    }
}
