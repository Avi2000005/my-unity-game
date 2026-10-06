using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
// Global namespace. UnityEditor is imported here because the source figure is
// read off an imported asset, which a runtime-only assembly cannot reach.
public static class CharacterSizeProbe
{
    const string ReportPath = "Temp/character_size.txt";

    /// <summary>
    /// How big Ari and Mono actually are.
    ///
    /// Beat 4 cannot be designed without this number. The beat is that Mono can
    /// go somewhere Ari cannot, and the size of that somewhere is a band between
    /// the two of them — so the band is only as good as the two heights, and a
    /// band built on a guessed height produces a crawlspace that either lets Ari
    /// in or locks Mono out.
    ///
    /// Three measurements per character, because they disagree and the
    /// disagreement is the finding:
    ///
    ///   collider   what the physics engine stops his with. Decides whether he
    ///              physically fits.
    ///   mesh       what the player sees. Decides whether fitting looks right.
    ///   source     the imported model in its rest pose. The only one of the
    ///              three that is not affected by how the level happens to have
    ///              placed or scaled the instance.
    ///
    /// The first attempt at this measured the capsule's height as
    /// height + (centre.y - radius), which adds half the capsule a second time
    /// and reported Mono at 2.30 m when he is 1.80 m. A capsule spans centre.y
    /// plus or minus half its height and nothing else; there is no radius term
    /// in its vertical extent.
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
        Debug.Log("[Echoes] character size\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportPath),
                           sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        sb.AppendLine("HOW BIG IS EVERYBODY");
        sb.AppendLine("Beat 4's crawlspace is the band between these two heights.");
        sb.AppendLine();

        float ari = Measure(sb, "Ari", "Assets/Art/Ari/Models/Ari_character.fbx");
        sb.AppendLine();
        float mono = Measure(sb, "Mono", "Assets/Art/L1/Mono.fbx");

        sb.AppendLine();
        sb.AppendLine("--- what this means for Beat 4 ---");

        if (ari <= 0f || mono <= 0f)
        {
            sb.AppendLine("  Not both measured, so the band cannot be stated. Fix the");
            sb.AppendLine("  missing measurement before building anything.");
            return;
        }

        if (mono >= ari)
        {
            sb.AppendLine("  *** MONO IS NOT SMALLER THAN ARI. He stands " +
                          mono.ToString("0.00") + " m and he stands " +
                          ari.ToString("0.00") + " m. ***");
            sb.AppendLine();
            sb.AppendLine("  There is no height band that excludes his and includes him," +
                          " so a crawlspace cannot do this beat as it stands. Three" +
                          " ways out, and they are not equivalent:");
            sb.AppendLine();
            sb.AppendLine("    1. Shrink Mono's collider. Cheap, and it is the honest fix" +
                          " if he is meant to be small — a spirit the size of a" +
                          " forearm. It changes nothing about how he looks.");
            sb.AppendLine("    2. Build the crawlspace to exclude Ari on WIDTH rather" +
                          " than height. Works with any two sizes, but then it is a" +
                          " squeeze he could force his way through, and the beat" +
                          " stops being about size and starts being about" +
                          " obedience.");
            sb.AppendLine("    3. Give Mono a mechanic that fits a space Ari's body" +
                          " does not, rather than a space his body is too big" +
                          " for. This is the option the brief actually describes" +
                          " — he unlocks tight-space mechanisms — and it does not" +
                          " need the two of them to be different sizes at all.");
            return;
        }

        float band = ari - mono;
        sb.AppendLine("  Ari " + ari.ToString("0.00") + " m, Mono " + mono.ToString("0.00") +
                      " m, so the band is " + band.ToString("0.00") + " m wide.");
        sb.AppendLine("  A passage with its ceiling between " +
                      mono.ToString("0.00") + " and " + ari.ToString("0.00") +
                      " m excludes his and admits him.");
        sb.AppendLine();
        sb.AppendLine("  Two things to check before it is built, both of which decide" +
                      " it rather than decorate it:");
        sb.AppendLine();
        sb.AppendLine("    - a ceiling in the band works because his capsule is" +
                      " " + ari.ToString("0.00") + " m and the hole is shorter than that,");
        sb.AppendLine("      so there is NO height at which he fits and he cannot" +
                      " jump through it either. That is arithmetic, not tuning.");
        sb.AppendLine("    - he walks up a " + AriStep().ToString("0.00") +
                      " m ledge without jumping, so the roof over the entrance has to" +
                      " come down below the crawlspace ceiling as well as over it." +
                      " A lip he can step onto is a lip he will step onto.");
        sb.AppendLine();
        sb.AppendLine("  Note how narrow that band is. At " +
                      ((mono + ari) * 0.5f).ToString("0.00") +
                      " m in the middle, a 2 cm error in either height puts the whole");
        sb.AppendLine("  crawlspace on the wrong side of one of them. Size it to the" +
                      " governing heights above, not to the middle of the band, and" +
                      " re-measure the built crawlspace afterwards.");
    }

    // --- one character -----------------------------------------------------------

    static float Measure(StringBuilder sb, string label, string sourceFbx)
    {
        sb.AppendLine("=== " + label + " ===");

        float colliderHeight = 0f;
        float sweptHeight = 0f;
        float instanceScale = 1f;

        var go = Named(label);
        if (go == null)
        {
            sb.AppendLine("  no GameObject called '" + label +
                          "' in the scene. Cannot measure the placed character.");
        }
        else
        {
            instanceScale = go.transform.lossyScale.y;
            sb.AppendLine("  placed at " + go.transform.position.ToString("F2") +
                          ", active=" + go.activeInHierarchy +
                          ", localScale " + go.transform.localScale.ToString("F2") +
                          ", lossyScale " + go.transform.lossyScale.ToString("F2"));

            colliderHeight = Collider(sb, go);
            sweptHeight = Swept(sb, go);
            Mesh(sb, go);
        }

        float source = Source(sb, label, sourceFbx, instanceScale);

        // What the physics stops his with governs. For Ari that is the swept
        // capsule in AriMover and there is no collider at all; for Mono it is a
        // real CapsuleCollider. Both are asked for, and the mesh is reported
        // because the player will judge the fit by the mesh and the two
        // disagreeing is exactly what a crawlspace gets built wrong on.
        float governing = colliderHeight > 0f ? colliderHeight
                        : sweptHeight > 0f ? sweptHeight
                        : source;
        string which = colliderHeight > 0f ? "the collider"
                     : sweptHeight > 0f ? "AriMover's swept capsule"
                     : "no collider, so the mesh";

        sb.AppendLine("  GOVERNING HEIGHT: " + governing.ToString("0.00") + " m  (" + which + ")");
        sb.AppendLine("  VISIBLE HEIGHT: " + source.ToString("0.00") +
                      " m (the mesh, at the instance's own scale of " +
                      instanceScale.ToString("0.00") + ")");
        sb.AppendLine("    the body and the mesh differ by " +
                      (source - governing).ToString("+0.00;-0.00") +
                      " m. A crawlspace has to clear the VISIBLE one so nothing" +
                      " clips, and clear the BODY one to keep him out, and those" +
                      " are two different conditions and only one of them is about" +
                      " the physics.");

        return governing;
    }

    /// <summary>
    /// Ari's body, asked of the component that owns it.
    ///
    /// By reflection, since this tool is compiled on its own. Ari has no
    /// collider — his sides are a Physics.CapsuleCast inside AriMover — so a
    /// tool that only looks for colliders reports his as having no body at all
    /// and concludes that nothing built can keep him out.
    /// </summary>
    static float Swept(StringBuilder sb, GameObject go)
    {
        var type = go.GetComponents<MonoBehaviour>()
                      .FirstOrDefault(m => m != null && m.GetType().Name == "AriMover")
                      ?.GetType();
        if (type == null)
        {
            if (go.name == "Ari")
                sb.AppendLine("  no AriMover on him. If his body is supposed to be a" +
                              " swept capsule, it is not, and nothing will stop him" +
                              " going through a wall.");
            return 0f;
        }

        var h = type.GetProperty("BodyHeight");
        var r = type.GetProperty("BodyRadius");
        var s = type.GetProperty("StepHeight");

        if (h == null)
        {
            sb.AppendLine("  AriMover has no BodyHeight property, so the swept body" +
                          " cannot be read from here.");
            return 0f;
        }

        float height = Convert.ToSingle(h.GetValue(go.GetComponent(type)));
        sb.AppendLine("  swept body capsule: height " + height.ToString("0.00") +
                      " m, radius " + (r != null ? Convert.ToSingle(r.GetValue(go.GetComponent(type)))
                                                 .ToString("0.00") : "?") +
                      " m, step height " + (s != null ? Convert.ToSingle(s.GetValue(go.GetComponent(type)))
                                                       .ToString("0.00") : "?") + " m");
        sb.AppendLine("    a cast, not a collider, so nothing on him will be found by" +
                      " a search for colliders — which is why he looked bodiless" +
                      " before this was asked the right question");

        return height;
    }

    /// <summary>Reports the colliders and returns the tallest one.</summary>
    static float Collider(StringBuilder sb, GameObject go)
    {
        // The whole hierarchy, not just the root. The first version of this asked
        // the root only and reported "NO CapsuleCollider" for Ari, which reads as
        // "he walks through walls" — a conclusion about the game drawn from a
        // question that was never asked of the object holding the collider.
        var capsules = go.GetComponentsInChildren<CapsuleCollider>(true);
        var controllers = go.GetComponentsInChildren<CharacterController>(true);
        var others = go.GetComponentsInChildren<Collider>(true)
                       .Where(c => !(c is CapsuleCollider)).ToArray();

        if (controllers.Length > 0)
        {
            foreach (var c in controllers)
                sb.AppendLine("  CharacterController on '" + Below(go.transform, c.transform) +
                              "': local height " + c.height.ToString("0.00") +
                              " m, local radius " + c.radius.ToString("0.00") +
                              " m, centre " + c.center.ToString("F2") +
                              ", scale " + c.transform.lossyScale.y.ToString("0.00") +
                              "  ->  stands " + Standing(c.height, c.center.y, c.transform).ToString("0.00") +
                              " m" + (c.enabled ? "" : "  [DISABLED]"));
        }

        if (capsules.Length > 0)
        {
            foreach (var c in capsules)
                sb.AppendLine("  CapsuleCollider on '" + Below(go.transform, c.transform) +
                              "': local height " + c.height.ToString("0.00") +
                              " m, local radius " + c.radius.ToString("0.00") +
                              " m, centre " + c.center.ToString("F2") +
                              ", scale " + c.transform.lossyScale.y.ToString("0.00") +
                              "  ->  stands " + Standing(c.height, c.center.y, c.transform).ToString("0.00") +
                              " m, " + (Radius(c.radius, c.transform) * 2f).ToString("0.00") +
                              " m wide" + (c.enabled ? "" : "  [DISABLED]"));
        }
        else
        {
            sb.AppendLine("  no CapsuleCollider anywhere in the hierarchy");
        }

        if (others.Length > 0)
        {
            foreach (var c in others)
                sb.AppendLine("  other collider: " + c.GetType().Name + " on '" +
                              Below(go.transform, c.transform) + "'" +
                              (c.enabled ? "" : " [DISABLED]"));
        }

        // Tallest, not first: a character with a small capsule on him root and a
        // taller one on a child is not 1.80 m, he is the taller of the two, and
        // a crawlspace sized to the first one is a crawlspace he walks out of.
        float tallest = 0f;
        foreach (var c in controllers) tallest = Mathf.Max(tallest, Standing(c.height, c.center.y, c.transform));
        foreach (var c in capsules) tallest = Mathf.Max(tallest, Standing(c.height, c.center.y, c.transform));

        if (tallest <= 0f)
        {
            // Ari lands here and always will: his body is a swept capsule cast
            // inside AriMover and there is no collider anywhere on him. Saying
            // "nothing will stop him going through a wall" about Ari is wrong
            // and expensive — the sweep does stop him, it just is not a
            // collider, so the tool has to go and ask the component instead.
            sb.AppendLine("  no usable capsule or character controller.");
            sb.AppendLine("    (if this is Ari, that is expected: his body is a swept" +
                          " capsule cast in AriMover, not a collider. CrawlspaceMap" +
                          " asks that component for the real numbers.)");
        }

        return tallest;
    }

    /// <summary>
    /// How tall a capsule stands in WORLD metres, from its own numbers.
    ///
    /// Two mistakes are built into the obvious version of this and both were
    /// made. A capsule is centre.y plus or minus half its height, with no radius
    /// term in its vertical extent. And every one of those numbers is LOCAL —
    /// Mono is scaled to 0.31, so his 1.80 m capsule is 0.56 m of actual world.
    /// Read as local numbers it looks like he is taller than Ari and that a
    /// crawlspace between them is impossible, which is the opposite of the truth
    /// and would have had a working beat designed out of existence.
    /// </summary>
    static float Standing(float height, float centreY, Transform owner)
    {
        float scale = owner != null ? Mathf.Abs(owner.lossyScale.y) : 1f;
        return Mathf.Max(0f, (centreY + height * 0.5f) * scale);
    }

    static float Radius(float radius, Transform owner)
    {
        float scale = owner != null ? Mathf.Abs(owner.lossyScale.x) : 1f;
        return radius * scale;
    }

    static void Mesh(StringBuilder sb, GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            sb.AppendLine("  no Renderer in the hierarchy at all, active or not");
            return;
        }

        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var b = r.bounds;
            if (b.min.y < minY) minY = b.min.y;
            if (b.max.y > maxY) maxY = b.max.y;
        }

        if (minY >= maxY)
        {
            sb.AppendLine("  " + renderers.Length + " renderer(s) but no usable bounds");
            return;
        }

        sb.AppendLine("  placed mesh: " + renderers.Length + " renderer(s), " +
                      minY.ToString("0.00") + " m to " + maxY.ToString("0.00") +
                      " m, so " + (maxY - minY).ToString("0.00") + " m tall");
        sb.AppendLine("    world-space bounds, so this includes the level's placement. " +
                      "The source figure below is the one to trust.");
    }

    /// <summary>
    /// The model as imported, in whatever pose it came in, at the prefab's own
    /// scale. Unaffected by the level, which is the whole reason it is here.
    /// </summary>
    static float Source(StringBuilder sb, string label, string path, float instanceScale = 1f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            sb.AppendLine("  source: nothing at " + path + ". The governing height" +
                          " above is therefore the placed collider, not the model.");
            return PlacedColliderHeight(label);
        }

        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            sb.AppendLine("  source " + path + " has no Renderer");
            return PlacedColliderHeight(label);
        }

        // Bounds on a prefab asset are its local ones; reading .bounds on an
        // asset gives a degenerate box, so the meshes are measured directly.
        //
        // Both kinds of renderer, because looking only for MeshFilter found no
        // meshes on Mono at all and reported "no usable meshes" for a model that
        // is sitting right there in the project. Mono is skinned, Ari is not,
        // and a probe that only understands one of them answers about half of
        // the cast.
        float minY = float.MaxValue, maxY = float.MinValue;
        var scale = prefab.transform.localScale.y;
        int measured = 0;

        foreach (var r in renderers)
        {
            if (r == null) continue;

            Mesh mesh = null;
            if (r is SkinnedMeshRenderer sk && sk.sharedMesh != null) mesh = sk.sharedMesh;
            else if (r.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null) mesh = mf.sharedMesh;
            if (mesh == null) continue;

            var b = mesh.bounds;
            var lossy = r.transform.lossyScale.y;
            minY = Mathf.Min(minY, (b.min.y - r.transform.position.y) * lossy);
            maxY = Mathf.Max(maxY, (b.max.y - r.transform.position.y) * lossy);
            measured++;
        }

        if (measured == 0)
        {
            sb.AppendLine("  source " + path + " has " + renderers.Length +
                          " renderer(s) but no readable mesh on any of them");
            return PlacedColliderHeight(label);
        }

        if (minY >= maxY)
        {
            sb.AppendLine("  source " + path + " has renderers but no usable meshes");
            return PlacedColliderHeight(label);
        }

        float atOne = (maxY - minY);
        float height = atOne * scale * instanceScale;
        sb.AppendLine("  source " + path + ": " + measured + " of " +
                      renderers.Length + " renderer(s) measured, mesh spans " +
                      atOne.ToString("0.00") + " m at scale 1, prefab scale " +
                      scale.ToString("0.00") + ", instance scale " +
                      instanceScale.ToString("0.00") + "  ->  " + height.ToString("0.00") +
                      " m tall as placed");
        return height;
    }

    /// <summary>
    /// Ari's step height, for the two checks the crawlspace actually turns on.
    /// Returns 0 if he is not in the scene or has no AriMover.
    /// </summary>
    static float AriStep()
    {
        var go = Named("Ari");
        if (go == null) return 0f;

        var comp = go.GetComponents<MonoBehaviour>()
                     .FirstOrDefault(m => m != null && m.GetType().Name == "AriMover");
        if (comp == null) return 0f;

        var p = comp.GetType().GetProperty("StepHeight");
        return p == null ? 0f : Convert.ToSingle(p.GetValue(comp));
    }

    /// <summary>
    /// Find a character by name, inactive or not.
    ///
    /// `GameObject.Find` skips inactive objects, and Beat 3 hides Mono from the
    /// moment the level loads until the tree is struck. So this probe, asked
    /// the most basic question in the project, reported "no GameObject called
    /// 'Mono' in the scene" about a scene that has him sitting right there —
    /// switched off — and then fell back to measuring the FBX *asset*, which is
    /// 1.70 m at scale 1.00 while the placed character is a third of that.
    ///
    /// That produced a report claiming Mono was 1.70 m tall and therefore
    /// 0.10 m shorter than Ari, and a conclusion that the usable crawlspace
    /// band was 10 cm wide. The band is actually 1.23 m wide. Nothing here
    /// needed tuning — the character was simply asleep, and asleep is not the
    /// same as absent.
    /// </summary>
    static GameObject Named(string name)
    {
        var all = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].name == name) return all[i].gameObject;
        return null;
    }

    static float PlacedColliderHeight(string label)
    {
        var go = Named(label);
        if (go == null) return 0f;

        float best = 0f;
        foreach (var c in go.GetComponentsInChildren<CapsuleCollider>(true))
            best = Mathf.Max(best, Standing(c.height, c.center.y, c.transform));
        foreach (var c in go.GetComponentsInChildren<CharacterController>(true))
            best = Mathf.Max(best, Standing(c.height, c.center.y, c.transform));
        return best;
    }

    /// <summary>
    /// A child's path below a root, for the report.
    ///
    /// Not called Path. A static method named Path in a class that also uses
    /// System.IO.Path shadows the type, and the file stops compiling with CS0119
    /// at the File.WriteAllText call at the top — a failure that points at the
    /// report-writing line and has nothing to do with it.
    /// </summary>
    static string Below(Transform root, Transform t)
    {
        if (t == root) return ".";
        var parts = new List<string>();
        while (t != null && t != root) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts);
    }
}
