using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Builds the cracked stone fountain for the market square.
    ///
    /// Nothing is downloaded. Every surface is a surface of revolution or a box,
    /// generated here, and shaded with the village's own T_RockTrim texture through
    /// the existing RockTrim material. That matters more than it sounds: a
    /// downloaded prop would arrive with its own albedo, its own texel density and
    /// its own idea of how big a stone is, and it would read as a stranger standing
    /// in a village made of someone else's walls. Generating the geometry from the
    /// same profiles the kit's rock trim uses keeps the texel scale identical, and
    /// because the material is already <c>Echoes/PainterlyLit</c> with
    /// <c>_ColorRestore = 0</c>, the fountain arrives in the Grey Realm and comes back
    /// to colour when Ari paints it, with no extra wiring.
    ///
    /// The damage is geometry, not texture. Cracks and missing fragments are made by
    /// leaving arc segments out of the basin ring and dropping others, so the broken
    /// silhouette survives at any distance and in silhouette-only rendering — a
    /// normal-mapped crack would vanish the moment the fountain was backlit.
    /// </summary>
    public static class FountainBuild
    {
        const string OutDir = "Assets/Painterly/Generated/Fountain";
        const string StoneMatPath = "Assets/Painterly/Materials/RockTrim.mat";
        const string MetalMatPath = "Assets/Painterly/Materials/MetalOrnament.mat";
        const string WaterMatPath = "Assets/Painterly/Materials/FountainWater.mat";
        const string Report = "Temp/fountain.txt";

        // Texture repeats per metre. Matches the density the kit's own rock trim
        // pieces land at, so a 2 m block of basin and a kit wall read as the same
        // quarried stone rather than one being a photograph of the other.
        const float UvPerMetre = 0.5f;

        const float BasinFloorY = 0.10f;
        const float WaterY = 0.46f;
        const float PlinthTopY = 0.60f;

        // ------------------------------------------------------------------
        //  Profiles. Each is a polyline in the (radius, y) plane, swept about Y.
        //  A profile that returns to its own first point is a closed section and
        //  needs no cap at its ends; anything else gets a flat cap on the open side.
        // ------------------------------------------------------------------

        /// <summary>Basin wall: up the inside, over the ornate lip, back down outside.</summary>
        static Vector2[] BasinWallProfile() => new[]
        {
            new Vector2(1.72f, 0.00f),
            new Vector2(1.76f, 0.62f),
            new Vector2(1.84f, 0.70f),
            new Vector2(1.98f, 0.72f),
            new Vector2(2.08f, 0.86f),   // the lip: the only ornate part
            new Vector2(2.14f, 0.79f),
            new Vector2(2.12f, 0.70f),
            new Vector2(2.14f, 0.10f),
            new Vector2(2.24f, 0.00f),
            new Vector2(1.72f, 0.00f),   // closes the section along the ground
        };

        /// <summary>Stepped plinth the statue stands on.</summary>
        static Vector2[] PlinthProfile() => new[]
        {
            new Vector2(0.00f, 0.00f),
            new Vector2(0.66f, 0.00f),
            new Vector2(0.66f, 0.15f),
            new Vector2(0.55f, 0.18f),
            new Vector2(0.55f, 0.32f),
            new Vector2(0.45f, 0.35f),
            new Vector2(0.45f, 0.46f),
            new Vector2(0.36f, 0.46f),
            new Vector2(0.00f, 0.46f),
        };

        /// <summary>
        /// The statue's robe, still standing. The top of the profile is deliberately
        /// uneven — three radii alternating over a few centimetres — so the break
        /// reads as snapped stone rather than a clean lathe cut.
        /// </summary>
        static Vector2[] StatueStumpProfile() => new[]
        {
            new Vector2(0.00f, 0.00f),
            new Vector2(0.44f, 0.00f),
            new Vector2(0.40f, 0.10f),
            new Vector2(0.31f, 0.28f),
            new Vector2(0.25f, 0.44f),
            new Vector2(0.23f, 0.53f),
            new Vector2(0.19f, 0.49f),   // -- the break --
            new Vector2(0.24f, 0.58f),
            new Vector2(0.20f, 0.55f),
            new Vector2(0.00f, 0.57f),
        };

        /// <summary>Torso and shoulder of the same figure, as a separate fallen piece.</summary>
        static Vector2[] StatueTorsoProfile() => new[]
        {
            new Vector2(0.00f, 0.00f),
            new Vector2(0.21f, 0.02f),
            new Vector2(0.19f, 0.07f),   // -- matching break --
            new Vector2(0.24f, 0.10f),
            new Vector2(0.20f, 0.14f),
            new Vector2(0.23f, 0.20f),
            new Vector2(0.26f, 0.30f),
            new Vector2(0.25f, 0.42f),
            new Vector2(0.16f, 0.50f),
            new Vector2(0.10f, 0.52f),
            new Vector2(0.00f, 0.52f),
        };

        /// <summary>A turned baluster — the "ornate" in ornate stone.</summary>
        static Vector2[] LanternPostProfile(float height) => new[]
        {
            new Vector2(0.00f, 0.00f),
            new Vector2(0.17f, 0.00f),
            new Vector2(0.17f, 0.06f),
            new Vector2(0.11f, 0.12f),
            new Vector2(0.09f, 0.18f),
            new Vector2(0.14f, 0.26f),   // collar
            new Vector2(0.08f, 0.33f),
            new Vector2(0.08f, height - 0.26f),
            new Vector2(0.13f, height - 0.18f),  // capital
            new Vector2(0.13f, height - 0.08f),
            new Vector2(0.19f, height - 0.04f),
            new Vector2(0.19f, height),
            new Vector2(0.00f, height),
        };

        /// <summary>Open bowl the lantern would sit in.</summary>
        static Vector2[] LanternCupProfile(float radius, float depth) => new[]
        {
            new Vector2(0.00f, 0.00f),
            new Vector2(radius * 0.55f, 0.01f),
            new Vector2(radius, depth),
            new Vector2(radius * 0.88f, depth),
            new Vector2(radius * 0.45f, depth * 0.28f),
            new Vector2(0.00f, depth * 0.22f),
        };

        // ------------------------------------------------------------------

        [MenuItem("Tools/Echoes/Build Fountain", priority = 75)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] fountain build");
            sb.AppendLine();

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sb.AppendLine("STOPPED: exit play mode first (Ctrl+P). Nothing was changed.");
                Finish(sb);
                return;
            }

            var stone = AssetDatabase.LoadAssetAtPath<Material>(StoneMatPath);
            var metal = AssetDatabase.LoadAssetAtPath<Material>(MetalMatPath);
            if (stone == null || metal == null)
            {
                sb.AppendLine($"FATAL: missing material. stone={stone != null} metal={metal != null}");
                Finish(sb);
                return;
            }

            var water = EnsureWaterMaterial(sb);

            // --- where can it stand, and therefore how big may it be? ----------
            // Measured before anything is built, because the room available is what
            // decides the size, not the other way round. The first attempt placed
            // the fountain at the square's transform position and buried it inside
            // House_0_0; the square's pivot is not its open ground, and its open
            // ground is a ring around a building rather than a plain middle.
            var scan = SquarePlacement.MeasureSquare(0.4f, 9f);
            if (!scan.Ok)
            {
                sb.AppendLine("FATAL: cannot measure the market square: " + scan.Why);
                Finish(sb);
                return;
            }

            var square = SquarePlacement.FindSquare();
            sb.AppendLine("square      : " + SquarePlacement.PathOf(square.transform));
            sb.AppendLine("ground      : y=" + scan.GroundY.ToString("F3")
                          + ", extent " + scan.Area.size + " at " + scan.Area.center);
            sb.AppendLine("open ground : " + scan.Samples + " samples, clearance max "
                          + scan.BestClearance.ToString("F2") + " m, median "
                          + MedianClearance(scan).ToString("F2") + " m");

            // Tear down any previous build so this is idempotent.
            var old = GameObject.Find("Fountain");
            if (old != null) Object.DestroyImmediate(old);

            // Built at full size at the origin first, so its real footprint can be
            // measured rather than assumed from the profile numbers.
            var root = new GameObject("Fountain");
            root.transform.position = Vector3.zero;

            int tris = 0, meshes = 0, colliders = 0;

            // --- basin ring, built segment by segment so it can be broken -----
            var wall = BasinWallProfile();
            const int Segments = 12;
            const float Step = 360f / Segments;

            for (int i = 0; i < Segments; i++)
            {
                float deg = i * Step;

                // Two whole fragments are simply gone. This is the readable damage:
                // the ring is visibly incomplete from any angle.
                if (i == 3 || i == 9) continue;

                var seg = new GameObject($"Basin_{i:00}");
                seg.transform.SetParent(root.transform, false);
                seg.transform.localRotation = Quaternion.Euler(0, deg + Step * 0.5f, 0);

                var profile = wall;
                float yScale = 1f, rOffset = 0f, lean = 0f;

                if (i == 2)
                {
                    // A fragment shoved outward and rotated off true — the crack
                    // either side of it is the tell.
                    rOffset = 0.05f;
                    lean = 1.4f;
                }
                else if (i == 7)
                {
                    // Collapsed inward: the wall has fallen into the basin and no
                    // longer reaches the lip.
                    yScale = 0.52f;
                    rOffset = -0.11f;
                    lean = -3.5f;
                }

                var m = Revolve(profile, -Step * 0.5f, Step * 0.5f, 7, true,
                                yScale, rOffset, Mathf.Deg2Rad * lean);
                tris += AddMesh(seg, m, stone, sb, ref meshes, ref colliders);
            }

            // --- plinth -------------------------------------------------------
            var plinthGo = new GameObject("Plinth");
            plinthGo.transform.SetParent(root.transform, false);
            plinthGo.transform.localPosition = new Vector3(0, BasinFloorY, 0);
            tris += AddMesh(plinthGo, Revolve(PlinthProfile(), 0, 360, 20, false, 1, 0, 0),
                            stone, sb, ref meshes, ref colliders);

            // --- the broken statue -------------------------------------------
            var statueGo = new GameObject("Statue");
            statueGo.transform.SetParent(root.transform, false);

            // Still standing: robe on the plinth, ending in a ragged break.
            var stumpGo = new GameObject("Statue_Stump");
            stumpGo.transform.SetParent(statueGo.transform, false);
            stumpGo.transform.localPosition = new Vector3(0.02f, PlinthTopY, -0.03f);
            stumpGo.transform.localRotation = Quaternion.Euler(0.6f, 24f, -1.1f);
            tris += AddMesh(stumpGo, Revolve(StatueStumpProfile(), 0, 360, 16, false, 1, 0, 0),
                            stone, sb, ref meshes, ref colliders);

            // Fallen: the torso, tipped off the plinth onto the basin floor.
            var torsoGo = new GameObject("Statue_Torso");
            torsoGo.transform.SetParent(statueGo.transform, false);
            torsoGo.transform.localPosition = new Vector3(0.74f, BasinFloorY + 0.23f, 0.46f);
            torsoGo.transform.localRotation = Quaternion.Euler(-74f, 38f, 21f);
            tris += AddMesh(torsoGo, Revolve(StatueTorsoProfile(), 0, 360, 16, false, 1, 0, 0),
                            stone, sb, ref meshes, ref colliders);

            // The head, a little further out, face down.
            var headGo = new GameObject("Statue_Head");
            headGo.transform.SetParent(statueGo.transform, false);
            headGo.transform.localPosition = new Vector3(1.16f, BasinFloorY + 0.15f, 0.86f);
            headGo.transform.localRotation = Quaternion.Euler(28f, 150f, 44f);
            var head = Sphere(0.165f, 16, 10);
            tris += AddMesh(headGo, head, stone, sb, ref meshes, ref colliders);

            // A forearm, snapped off, on the paving outside the basin.
            var armGo = new GameObject("Statue_Arm");
            armGo.transform.SetParent(statueGo.transform, false);
            armGo.transform.localPosition = new Vector3(2.05f, 0.07f, 1.42f);
            armGo.transform.localRotation = Quaternion.Euler(0, 66f, 90);
            tris += AddMesh(armGo, Box(0.44f, 0.12f, 0.12f, 1), stone, sb, ref meshes, ref colliders);

            // --- three lantern holders ---------------------------------------
            var lanternGo = new GameObject("Lanterns");
            lanternGo.transform.SetParent(root.transform, false);
            var angles = new[] { 40f, 160f, 280f };

            for (int i = 0; i < angles.Length; i++)
            {
                float rad = angles[i] * Mathf.Deg2Rad;
                float h = i == 1 ? 0.58f : 1.16f;   // the middle one snapped short

                var l = new GameObject($"Lantern_{i:00}");
                l.transform.SetParent(lanternGo.transform, false);
                l.transform.localPosition = new Vector3(Mathf.Cos(rad) * 2.78f, 0f, Mathf.Sin(rad) * 2.78f);
                l.transform.localRotation = Quaternion.Euler(
                    i == 1 ? 9f : 0f, -angles[i], i == 1 ? -13f : 0f);

                tris += AddMesh(l, Revolve(LanternPostProfile(h), 0, 360, 12, false, 1, 0, 0),
                                metal, sb, ref meshes, ref colliders);
                tris += AddMesh(l, Revolve(LanternCupProfile(0.20f, 0.17f), 0, 360, 12, false, 1, 0, 0),
                                metal, sb, ref meshes, ref colliders, childName: "Cup");

                // The broken holder's cup is on the ground, not on the post.
                if (i == 1)
                {
                    var fallen = new GameObject("Lantern_01_CupFallen");
                    fallen.transform.SetParent(lanternGo.transform, false);
                    fallen.transform.localPosition = new Vector3(
                        Mathf.Cos(rad) * 3.24f, 0.02f, Mathf.Sin(rad) * 3.24f);
                    fallen.transform.localRotation = Quaternion.Euler(84f, 12f, 0f);
                    tris += AddMesh(fallen, Revolve(LanternCupProfile(0.20f, 0.17f), 0, 360, 12, false, 1, 0, 0),
                                    metal, sb, ref meshes, ref colliders);
                }
            }

            // --- fallen rim fragments, on the paving -------------------------
            var debrisGo = new GameObject("Debris");
            debrisGo.transform.SetParent(root.transform, false);
            var debris = new[]
            {
                new { p = new Vector3(2.62f, 0.11f, 0.55f), r = new Vector3(0, 34f, 22f), s = new Vector3(0.62f, 0.34f, 0.30f) },
                new { p = new Vector3(-2.48f, 0.09f, -0.92f), r = new Vector3(0, -58f, -15f), s = new Vector3(0.48f, 0.26f, 0.34f) },
                new { p = new Vector3(0.35f, 0.08f, 2.71f), r = new Vector3(14f, 12f, 0f), s = new Vector3(0.70f, 0.30f, 0.28f) },
            };
            for (int i = 0; i < debris.Length; i++)
            {
                var d = debris[i];
                var g = new GameObject($"Debris_{i:00}");
                g.transform.SetParent(debrisGo.transform, false);
                g.transform.localPosition = d.p;
                g.transform.localRotation = Quaternion.Euler(d.r);
                var mesh = Box(d.s.x, d.s.y, d.s.z, 1);
                tris += AddMesh(g, mesh, stone, sb, ref meshes, ref colliders);
            }

            // --- water --------------------------------------------------------
            var waterGo = new GameObject("Water");
            waterGo.transform.SetParent(root.transform, false);
            waterGo.transform.localPosition = new Vector3(0, WaterY, 0);
            if (water != null)
            {
                var disc = Disc(1.70f, 24);
                tris += AddMesh(waterGo, disc, water, sb, ref meshes, ref colliders, withCollider: false);
            }

            // --- the paintable target ------------------------------------------
            var target = root.AddComponent<ColorRestoreTarget>();
            var so = new SerializedObject(target);
            so.FindProperty("startRestore").floatValue = 0f;   // Grey Realm
            so.FindProperty("duration").floatValue = 1.6f;
            so.ApplyModifiedPropertiesWithoutUndo();
            target.SetRestoreImmediate(0f);

            root.isStatic = true;

            // --- fit it to the room it was given ---------------------------------
            //
            // The room is fixed and the prop is not, so the prop gives. The footprint
            // is measured off the built meshes rather than read from the profile
            // numbers, because the widest part of this fountain is the fallen lantern
            // cup lying outside the basin, which no single profile knows about — and
            // because a measurement does not go stale when the profiles are edited.
            float builtRadius = FootprintRadius(root);

            const float Margin = 0.15f;    // a little air, so it is not touching a wall
            const float MinScale = 0.40f;  // below this the statue stops reading as one

            float fitScale = 1f;
            Vector3 spot = Vector3.zero;
            float clearance = 0f;
            string why = "";

            for (; fitScale >= MinScale - 1e-4f; fitScale -= 0.05f)
            {
                if (SquarePlacement.TryFind(scan, builtRadius * fitScale + Margin,
                                            out spot, out clearance, out why, null))
                    break;
            }

            if (fitScale < MinScale - 1e-4f)
            {
                // Even the smallest size the design still reads at does not fit. Put
                // it at the roomiest spot and say so plainly, rather than hiding a
                // fountain inside a building and reporting success.
                fitScale = MinScale;
                spot = scan.Best != null ? scan.Best.Point : scan.Area.center;
                clearance = scan.BestClearance;
                why = "NOTHING FITS: placed at the roomiest spot anyway";
            }

            if (fitScale < 0.999f)
            {
                ScaleBuilt(root, fitScale);
                sb.AppendLine("resized     : x" + fitScale.ToString("F2")
                              + " (built radius " + builtRadius.ToString("F2")
                              + " m -> " + (builtRadius * fitScale).ToString("F2") + " m)");
            }
            else
            {
                sb.AppendLine("resized     : x1.00 (fits at the size it was authored)");
            }

            root.transform.position = spot;

            sb.AppendLine("placement   : " + spot);
            sb.AppendLine("clearance   : " + clearance.ToString("F2") + " m available");
            sb.AppendLine("why         : " + why);

            // Save meshes as assets so they are inspectable and survive a scene
            // reload, rather than living only inside the .unity file.
            EnsureFolder(OutDir);
            int savedMeshes = SaveMeshAssets(root, sb);

            EditorUtility.SetDirty(root);
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                EditorUtility.SetDirty(t.gameObject);

            var scene = root.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // --- measure, do not eyeball ---------------------------------------
            var all = root.GetComponentsInChildren<Renderer>(true);
            var b = new Bounds(root.transform.position, Vector3.zero);
            foreach (var r in all) b.Encapsulate(r.bounds);

            // Counted from the MeshFilters rather than from the running total the
            // builder accumulates, so this is an independent measurement of what
            // actually ended up in the scene. Two numbers that disagree mean a
            // mesh was built and then not parented.
            int total = 0;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh != null) total += mf.sharedMesh.triangles.Length / 3;

            sb.AppendLine($"root        : {PathOf(root.transform)}");
            sb.AppendLine($"meshes      : {meshes}   colliders: {colliders}");
            sb.AppendLine($"mesh assets : {savedMeshes} written to {OutDir}");
            sb.AppendLine($"triangles   : {total:N0}  (builder counted {tris:N0})");
            sb.AppendLine($"bounds size : {b.size.x:F2} x {b.size.y:F2} x {b.size.z:F2} m");
            sb.AppendLine($"bounds min  : {b.min}");
            sb.AppendLine($"bounds max  : {b.max}");
            sb.AppendLine($"renderers   : {all.Length}");
            sb.AppendLine($"target      : {target.GetType().Name} restore={target.Restore:F2}");
            sb.AppendLine($"scene saved : {scene.path}");

            Finish(sb);
        }

        // ==================================================================
        //  Geometry
        // ==================================================================

        /// <summary>
        /// Sweep a (radius, y) profile about the Y axis.
        ///
        /// <paramref name="closedSection"/> means the profile's last point equals
        /// its first, so the swept surface is a closed tube and needs no cap at the
        /// profile's ends — only at the two angular ends, and only when the sweep
        /// does not go all the way round.
        ///
        /// <paramref name="yScale"/> and <paramref name="rOffset"/> exist so the
        /// damaged segments can be derived from the intact profile instead of being
        /// authored separately. A collapsed fragment is the same wall, squashed and
        /// pushed in, which is what keeps its stone grain continuous with its
        /// neighbours.
        /// </summary>
        static Mesh Revolve(Vector2[] profile, float a0Deg, float a1Deg, int seg,
                            bool closedSection, float yScale, float rOffset, float tiltRad)
        {
            int n = profile.Length;
            int cols = seg + 1;                 // +1 so the UV seam has its own verts
            var verts = new List<Vector3>(cols * n);
            var uvs = new List<Vector2>(cols * n);
            var tris = new List<int>(cols * n * 6);

            // Profile arc length, so u is metres along the section rather than an
            // index — otherwise a profile with unevenly spaced points stretches
            // the texture unevenly along the wall.
            var arc = new float[n];
            for (int j = 1; j < n; j++)
                arc[j] = arc[j - 1] + Vector2.Distance(profile[j - 1], profile[j]);

            float midR = 0f;
            for (int j = 0; j < n; j++) midR += profile[j].x;
            midR /= Mathf.Max(1, n);

            for (int i = 0; i < cols; i++)
            {
                float a = Mathf.Lerp(a0Deg, a1Deg, seg <= 0 ? 0f : (float)i / seg) * Mathf.Deg2Rad;
                float ca = Mathf.Cos(a), sa = Mathf.Sin(a);

                for (int j = 0; j < n; j++)
                {
                    float r = profile[j].x + rOffset;
                    float y = profile[j].y * yScale;

                    // Tilt about the segment's own X axis, pivoting on the base, so
                    // a leaning fragment still meets the ground.
                    float ty = y * Mathf.Cos(tiltRad);
                    float tr = r + y * Mathf.Sin(tiltRad);

                    verts.Add(new Vector3(ca * tr, ty, sa * tr));
                    uvs.Add(new Vector2(arc[j] * UvPerMetre, a * midR * UvPerMetre));
                }
            }

            int jMax = closedSection ? n : n - 1;
            for (int i = 0; i < seg; i++)
            {
                for (int j = 0; j < jMax; j++)
                {
                    int j2 = (j + 1) % n;
                    int a0 = i * n + j;   // (angle i,   profile j)
                    int a1 = i * n + j2;  // (angle i,   profile j+1)
                    int b0 = (i + 1) * n + j;
                    int b1 = (i + 1) * n + j2;

                    // A profile that touches the axis collapses the quad onto a
                    // pole. Emit the single triangle that survives instead of two
                    // with coincident corners, which would otherwise average their
                    // normals into noise at the tip of every spire.
                    bool poleA = profile[j].x <= 1e-5f && rOffset <= 1e-5f;
                    bool poleB = profile[j2].x <= 1e-5f && rOffset <= 1e-5f;

                    if (poleA && poleB) continue;

                    if (poleA)       { tris.Add(a0); tris.Add(b1); tris.Add(a1); }
                    else if (poleB)  { tris.Add(a0); tris.Add(b1); tris.Add(b0); }
                    else
                    {
                        tris.Add(a0); tris.Add(b1); tris.Add(b0);
                        tris.Add(a0); tris.Add(a1); tris.Add(b1);
                    }
                }
            }

            // Angular end caps. Only needed on a partial sweep.
            bool full = Mathf.Abs(a1Deg - a0Deg) >= 359.9f;
            if (!full)
            {
                AddCap(verts, uvs, tris, profile, 0f, yScale, rOffset, tiltRad, true);
                AddCap(verts, uvs, tris, profile, a1Deg, yScale, rOffset, tiltRad, false);
            }

            var m = new Mesh { name = "FountainPart" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            OrientOutward(m);
            m.RecalculateNormals();
            m.RecalculateBounds();
            Weld(m);
            return m;
        }

        /// <summary>
        /// Orient the triangles so the solid faces outward, using its signed volume.
        ///
        /// The profiles here are not all wound the same way round. The basin wall
        /// section runs up the inside, over the lip and back down the outside —
        /// clockwise in (radius, y). The plinth runs outward along the ground and
        /// back up to the axis — counter-clockwise. Reading that off each profile by
        /// eye is exactly the kind of thing that silently produces a fountain with
        /// inverted faces, so the winding is decided from the geometry instead:
        /// a closed mesh wound outward has positive signed volume, and if it comes
        /// out negative every triangle is reversed in one pass.
        /// </summary>
        static void OrientOutward(Mesh m)
        {
            var v = m.vertices;
            var t = m.triangles;
            if (t.Length < 3) return;

            double vol = 0.0;
            for (int i = 0; i < t.Length; i += 3)
            {
                var a = v[t[i]];
                var b = v[t[i + 1]];
                var c = v[t[i + 2]];
                vol += Vector3.Dot(a, Vector3.Cross(b, c));
            }
            vol /= 6.0;

            // Guard the degenerate case: a perfectly flat mesh has zero volume and
            // no correct answer, so leave it alone rather than flip at random.
            if (Mathf.Abs((float)vol) < 1e-7f) return;
            if (vol > 0.0) return;

            for (int i = 0; i < t.Length; i += 3)
            {
                int tmp = t[i + 1];
                t[i + 1] = t[i + 2];
                t[i + 2] = tmp;
            }
            m.triangles = t;
        }

        /// <summary>
        /// Flat cap on an open angular end, fanned from the section's centroid.
        ///
        /// Fanning works because every profile here is a staircase or a wall
        /// section — star-shaped about its own centroid — so no triangle ever has
        /// to wrap around the outside.
        /// </summary>
        static void AddCap(List<Vector3> verts, List<Vector2> uvs, List<int> tris,
                           Vector2[] profile, float angleDeg, float yScale, float rOffset,
                           float tiltRad, bool atStart)
        {
            int n = profile.Length;
            float cr = 0f, cy = 0f;
            for (int j = 0; j < n - 1; j++) { cr += profile[j].x; cy += profile[j].y; }
            cr /= (n - 1); cy /= (n - 1);

            float a = angleDeg * Mathf.Deg2Rad;
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a);

            float ty = cy * yScale;
            float tr = cr + rOffset + cy * Mathf.Sin(tiltRad);
            int centre = verts.Count;
            verts.Add(new Vector3(ca * tr, ty, sa * tr));
            uvs.Add(new Vector2(0.5f, 0.5f));

            int ringStart = verts.Count;
            for (int j = 0; j < n - 1; j++)
            {
                float r = profile[j].x + rOffset;
                float y = profile[j].y * yScale;
                float y2 = y * Mathf.Cos(tiltRad);
                float r2 = r + y * Mathf.Sin(tiltRad);
                verts.Add(new Vector3(ca * r2, y2, sa * r2));
                uvs.Add(new Vector2(0.5f + profile[j].x * 0.4f, 0.5f + profile[j].y * 0.4f));
            }

            for (int j = 0; j < n - 2; j++)
            {
                int p0 = ringStart + j, p1 = ringStart + j + 1;
                if (atStart) { tris.Add(centre); tris.Add(p1); tris.Add(p0); }
                else { tris.Add(centre); tris.Add(p0); tris.Add(p1); }
            }
        }

        /// <summary>Axis-aligned box with per-face UVs scaled to real size.</summary>
        static Mesh Box(float w, float h, float d, int _ = 0)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            Vector3 e = new Vector3(w, h, d) * 0.5f;
            // face normal, then the two in-plane axes
            var faces = new[]
            {
                new { n = Vector3.forward,  u = Vector3.right,  v = Vector3.up,     w = w, h = h },
                new { n = Vector3.back,     u = Vector3.left,   v = Vector3.up,     w = w, h = h },
                new { n = Vector3.right,    u = Vector3.back,   v = Vector3.up,     w = d, h = h },
                new { n = Vector3.left,     u = Vector3.forward,v = Vector3.up,     w = d, h = h },
                new { n = Vector3.up,       u = Vector3.right,  v = Vector3.forward,w = w, h = d },
                new { n = Vector3.down,     u = Vector3.right,  v = Vector3.back,   w = w, h = d },
            };

            foreach (var f in faces)
            {
                var nrm = f.n;
                var u = f.u * (f.w * 0.5f);
                var v = f.v * (f.h * 0.5f);
                var c = Vector3.Scale(nrm, e);

                int i0 = verts.Count;
                verts.Add(c - u - v); uvs.Add(new Vector2(0f, 0f));
                verts.Add(c + u - v); uvs.Add(new Vector2(f.w * UvPerMetre, 0f));
                verts.Add(c + u + v); uvs.Add(new Vector2(f.w * UvPerMetre, f.h * UvPerMetre));
                verts.Add(c - u + v); uvs.Add(new Vector2(0f, f.h * UvPerMetre));

                tris.Add(i0); tris.Add(i0 + 2); tris.Add(i0 + 1);
                tris.Add(i0); tris.Add(i0 + 3); tris.Add(i0 + 2);
            }

            var m = new Mesh { name = "FountainBox" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            OrientOutward(m);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static Mesh Sphere(float r, int seg, int rings)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int y = 0; y <= rings; y++)
            {
                float v = (float)y / rings;
                float phi = v * Mathf.PI;
                for (int x = 0; x <= seg; x++)
                {
                    float u = (float)x / seg;
                    float theta = u * Mathf.PI * 2f;
                    verts.Add(new Vector3(
                        r * Mathf.Sin(phi) * Mathf.Cos(theta),
                        r * Mathf.Cos(phi),
                        r * Mathf.Sin(phi) * Mathf.Sin(theta)));
                    uvs.Add(new Vector2(u * Mathf.PI * r * UvPerMetre * 2f, v * Mathf.PI * r * UvPerMetre));
                }
            }
            for (int y = 0; y < rings; y++)
                for (int x = 0; x < seg; x++)
                {
                    int a = y * (seg + 1) + x, b = a + seg + 1;
                    tris.Add(a); tris.Add(b); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(b); tris.Add(b + 1);
                }

            var m = new Mesh { name = "FountainHead" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            OrientOutward(m);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static Mesh Disc(float r, int seg)
        {
            var verts = new List<Vector3> { Vector3.zero };
            var uvs = new List<Vector2> { new Vector2(0.5f, 0.5f) };
            var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
                uvs.Add(new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            // Wound (centre, i+1, i) rather than (centre, i, i+1). The angles run
            // anticlockwise in XZ, so the ascending order faces -Y and the water
            // would be lit from underneath and backface-culled from above — a
            // perfectly invisible fountain basin.
            for (int i = 1; i <= seg; i++) { tris.Add(0); tris.Add(i + 1); tris.Add(i); }

            var m = new Mesh { name = "FountainWater" };
            m.SetVertices(verts);
            m.SetUVs(0, uvs);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// Average normals of coincident vertices.
        ///
        /// Revolve() emits a duplicated column of verts at the UV seam so the
        /// texture can wrap. Left alone, RecalculateNormals treats those two
        /// columns as unrelated, and the seam shows as a hard crease running the
        /// whole height of every basin segment. Welding after the fact fixes the
        /// shading without collapsing the UVs.
        /// </summary>
        static void Weld(Mesh m)
        {
            var v = m.vertices;
            var n = m.normals;
            var map = new Dictionary<Vector3Int, List<int>>(v.Length);
            var keyOf = new Vector3Int[ v.Length ];

            for (int i = 0; i < v.Length; i++)
            {
                var p = v[i];
                // Quantise to 0.1 mm — below any geometry this generates, above
                // float noise on a value that round-tripped through a .asset.
                var k = new Vector3Int(
                    Mathf.RoundToInt(p.x * 10000f),
                    Mathf.RoundToInt(p.y * 10000f),
                    Mathf.RoundToInt(p.z * 10000f));
                keyOf[i] = k;
                if (!map.TryGetValue(k, out var list)) map[k] = list = new List<int>(2);
                list.Add(i);
            }

            var welded = (Vector3[])n.Clone();
            foreach (var kv in map)
            {
                if (kv.Value.Count < 2) continue;
                var sum = Vector3.zero;
                foreach (int i in kv.Value) sum += n[i];
                var avg = sum.normalized;
                foreach (int i in kv.Value) welded[i] = avg;
            }
            m.normals = welded;
        }

        // ==================================================================
        //  Scene plumbing
        // ==================================================================

        static int AddMesh(GameObject go, Mesh mesh, Material mat, StringBuilder sb,
                           ref int meshCount, ref int colliderCount,
                           string childName = null, bool withCollider = true)
        {
            if (childName != null)
            {
                var child = new GameObject(childName);
                child.transform.SetParent(go.transform, false);
                go = child;
            }

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            // Static everything. A fountain is not going anywhere, and this is what
            // let VillageColliders batch it with the rest of the village.
            go.isStatic = true;

            if (withCollider)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
                // Non-convex: the mesh is one closed solid, so convexity buys
                // nothing and costs a decomposition at import time.
                mc.convex = false;
                colliderCount++;
            }

            meshCount++;
            return mesh != null ? mesh.triangles.Length / 3 : 0;
        }

        /// <summary>
        /// Create a nested asset folder, one level at a time.
        ///
        /// AssetDatabase.CreateFolder refuses to create a child of a folder Unity
        /// does not already know about, and it does so by returning an empty string
        /// rather than throwing — so the failure only surfaces later, as a
        /// CreateAsset error several steps away from the cause. A directory that
        /// exists on disk but has not been imported counts as unknown, which is
        /// exactly how a plain Directory.CreateDirectory call elsewhere leaves
        /// things.
        /// </summary>
        static void EnsureFolder(string assetFolder)
        {
            assetFolder = assetFolder.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            int slash = assetFolder.LastIndexOf('/');
            if (slash <= 0) return;
            var parent = assetFolder.Substring(0, slash);
            EnsureFolder(parent);

            // Pick up anything that exists physically but not yet in the database.
            if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.Refresh();
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            AssetDatabase.CreateFolder(parent, assetFolder.Substring(slash + 1));
        }

        /// <summary>Write each generated mesh out as its own asset. Returns how many.</summary>
        static int SaveMeshAssets(GameObject root, StringBuilder sb)
        {
            int saved = 0;
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;

                var name = $"{mf.gameObject.transform.parent.name}_{mf.gameObject.name}";
                mf.sharedMesh.name = name;
                var path = $"{OutDir}/{name}.asset";

                // Replace rather than add: a second CreateAsset at a live path
                // fails, and this script is meant to be re-runnable after the
                // fountain is edited.
                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
                    AssetDatabase.DeleteAsset(path);

                AssetDatabase.CreateAsset(mf.sharedMesh, path);
                if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) saved++;
                else sb.AppendLine($"  mesh asset NOT written: {path}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return saved;
        }

        /// <summary>
        /// A pale, slightly blue water. It ships grey with everything else, and
        /// turns blue when the basin is painted — the first thing in the square
        /// that comes back looking alive.
        /// </summary>
        static Material EnsureWaterMaterial(StringBuilder sb)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(WaterMatPath);
            if (existing != null) return existing;

            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Painterly/Shaders/PainterlyLit.shader");
            if (shader == null) shader = Shader.Find("Echoes/PainterlyLit");
            if (shader == null)
            {
                sb.AppendLine("FATAL: PainterlyLit shader not found; water will be skipped");
                return null;
            }

            var m = new Material(shader) { name = "FountainWater" };
            m.SetColor("_BaseColor", new Color(0.62f, 0.70f, 0.74f, 1f));
            m.SetFloat("_Smoothness", 0.92f);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_ColorRestore", 0f);
            m.SetFloat("_RestoreBoost", 1f);

            AssetDatabase.CreateAsset(m, WaterMatPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(WaterMatPath);
            sb.AppendLine($"created water material -> {WaterMatPath}");
            return AssetDatabase.LoadAssetAtPath<Material>(WaterMatPath);
        }

        /// <summary>
        /// The largest horizontal distance from the root's origin to anything it
        /// contains, including that piece's own width.
        ///
        /// Measured off the renderers rather than taken from the profile numbers.
        /// The widest part of this fountain is the cup of the broken lantern holder,
        /// lying on the paving well outside the basin, which no single profile knows
        /// about — and the profile numbers would go stale the moment the composition
        /// was edited. This is the number the placement search is given, so a wrong
        /// footprint means a fountain intersecting a wall.
        /// </summary>
        static float FootprintRadius(GameObject root)
        {
            var origin = root.transform.position;
            float r = 0f;
            foreach (var rend in root.GetComponentsInChildren<Renderer>(true))
            {
                var b = rend.bounds;
                var offset = new Vector2(b.center.x - origin.x, b.center.z - origin.z);
                r = Mathf.Max(r, offset.magnitude + Mathf.Max(b.extents.x, b.extents.z));
            }
            return r;
        }

        /// <summary>
        /// Shrink the built fountain about its root, meshes and all.
        ///
        /// A uniform transform scale would have been one line, and would have been
        /// wrong: it scales geometry without scaling UVs, so the kit's stone grain
        /// would come out smaller on a smaller fountain and the rock would visibly
        /// change size next to the walls it stands between. UVs here are metres —
        /// u is profile arc length — so scaling position and UV by the same factor
        /// keeps the grain the same size in the world, which is what makes a small
        /// fountain read as the same quarry as the village around it.
        /// </summary>
        static void ScaleBuilt(GameObject root, float s)
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.sharedMesh;
                if (m == null) continue;

                var v = m.vertices;
                for (int i = 0; i < v.Length; i++) v[i] *= s;
                var uv = m.uv;
                for (int i = 0; i < uv.Length; i++) uv[i] *= s;

                m.vertices = v;
                m.uv = uv;
                m.RecalculateBounds();

                // A MeshCollider keeps its own baked copy of the mesh, so it has to
                // be handed the mesh again or it carries on colliding along the old
                // outline and the paint radius disagrees with what you can see.
                var mc = mf.GetComponent<MeshCollider>();
                if (mc != null) { mc.sharedMesh = null; mc.sharedMesh = m; }
            }

            // Child offsets are as much a part of the size as the meshes are: the
            // lanterns ring the basin and the debris lies around it, and scaling only
            // the meshes would leave them arranged for a fountain that is not there.
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t != root.transform) t.localPosition *= s;
        }

        static float MedianClearance(SquarePlacement.Scan scan)
        {
            if (scan == null || scan.Spots.Count == 0) return 0f;
            var xs = scan.Spots.Select(s => s.Clearance).OrderBy(v => v).ToList();
            return xs[xs.Count / 2];
        }

        static string PathOf(Transform t)
        {
            if (t == null) return "<none>";
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        static void Finish(StringBuilder sb)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(
                Path.Combine(Directory.GetCurrentDirectory(), Report)));
            File.WriteAllText(Report, sb.ToString());
            Debug.Log(sb.ToString());
        }
    }
}
