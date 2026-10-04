using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Procedurally assembles a Grey Village from the Quaternius Medieval Village
    /// MegaKit modular pieces.
    ///
    /// The free kit ships loose FBX with no prefabs and no authored dimensions, so
    /// rather than hard-coding a unit size this measures each piece's real renderer
    /// bounds and lays walls out edge to edge from that. That keeps the layout
    /// correct whatever scale the kit is authored at.
    ///
    /// Rebuild is deterministic: same seed, same village.
    ///
    /// Every dimension below is measured, not assumed. Run Tools > Echoes > Survey
    /// Kit to re-measure all 176 models into Temp/kit_survey.txt. The kit's FBX
    /// roots carry a 100x scale and a 270 degree rotation about X, so local axes are
    /// permuted relative to world and nothing can be read off a mesh without
    /// instantiating it first. Re-run the survey after a kit upgrade.
    ///
    /// Known simplifications, all deliberate for a first pass:
    ///  - Facade variety comes from the wall/door/window variants and the roof
    ///    table. The WindowShutters_*_Closed pieces intersect their window add-on,
    ///    so only the _Open variants are placed.
    ///  - Corner posts are 3.02 tall against a 3.12 storey, so a second post is
    ///    stacked at every internal floor joint to keep the corner closed.
    ///  - The kit's Roughness/ORM maps are not wired up; see PainterlyMaterialSetup.
    /// </summary>
    public static class VillageGenerator
    {
        public const string ModelDir = "Assets/Art/Village/Models";
        public const string MaterialDir = "Assets/Painterly/Materials";
        public const string RootName = "Village_Grey";

        // ---- measured module ------------------------------------------------
        // All Wall_Plaster_* / Wall_UnevenBrick_* are 2.00 x 3.12 x 0.41 and every
        // full Floor_* tile is 2.00 x 2.00, so the whole village snaps to a 2-unit
        // bay and a 3.12-unit storey. Nothing else in the kit is a clean multiple,
        // which is why these two numbers are the only ones worth hard-coding.

        const float BayLen = 2.00f;
        const float StoreyH = 3.12f;
        const float WallT = 0.41f;          // measured wall thickness
        const float CornerH = 3.02f;        // Corner_Exterior_Brick height

        // Floor heights are staggered so overlapping paving never z-fights:
        // ground 0, paths just above it, plaza above that, interiors highest.
        const float YPath = 0.01f;
        const float YSquare = 0.03f;
        const float YFloor = 0.06f;

        // The kit ships hipped roof ASSEMBLIES, not roof tiles. Roof_RoundTiles_6x8
        // is one complete roof. Measured against the surveyed bounds, a roof's
        // world footprint is always larger than the (first name part x second name
        // part) wall footprint it covers, so the walls always sit under the eaves.
        // BuildRoof re-checks that at run time and warns if a kit update breaks it.
        static readonly string[] RoofTable =
        {
            "Roof_RoundTiles_4x4",   // covers 2 x 2 bays
            "Roof_RoundTiles_4x6",   // 2 x 3
            "Roof_RoundTiles_6x4",   // 3 x 2
            "Roof_RoundTiles_4x8",   // 2 x 4
            "Roof_RoundTiles_6x6",   // 3 x 3
            "Roof_RoundTiles_6x8",   // 3 x 4
            "Roof_RoundTiles_6x10",  // 3 x 5
            "Roof_RoundTiles_8x8",   // 4 x 4
            "Roof_RoundTiles_8x10",  // 4 x 5
            "Roof_RoundTiles_8x12",  // 4 x 6
            "Roof_RoundTiles_6x12",  // 3 x 6
            "Roof_RoundTiles_6x14",  // 3 x 7
            "Roof_RoundTiles_8x14",  // 4 x 7
        };

        static readonly string[] PlainWalls =
        {
            "Wall_Plaster_Straight",
            "Wall_Plaster_Straight_L",
            "Wall_Plaster_Straight_R",
            "Wall_Plaster_Straight_Base",
            "Wall_Plaster_WoodGrid",
            "Wall_UnevenBrick_Straight",
        };

        static readonly string[] WindowWalls =
        {
            "Wall_Plaster_Window_Wide_Flat",
            "Wall_Plaster_Window_Wide_Round",
            "Wall_Plaster_Window_Thin_Round",
            "Wall_UnevenBrick_Window_Wide_Flat",
            "Wall_UnevenBrick_Window_Thin_Round",
        };

        static readonly string[] DoorWalls =
        {
            "Wall_Plaster_Door_Flat",
            "Wall_Plaster_Door_Round",
            "Wall_UnevenBrick_Door_Flat",
            "Wall_UnevenBrick_Door_Round",
        };

        // Interior flights. SolidExtended is 6.16 long and only fits a 4-bay depth.
        static readonly string[] StairPieces =
        {
            "Stair_Interior_Simple",
            "Stair_Interior_Solid",
            "Stair_Interior_Rails",
            "Stair_Interior_SolidExtended",
        };

        // Measured list - the kit has no Prop_Vine3, 7 or 8.
        static readonly string[] WallVines = { "Prop_Vine1", "Prop_Vine2", "Prop_Vine4" };
        static readonly string[] BushVines = { "Prop_Vine5", "Prop_Vine6", "Prop_Vine9" };

        struct Piece
        {
            public Vector3 size;        // world size of the mesh bounds
            public Vector3 baseOffset; // world offset from the prefab root to the mesh's min corner
        }

        struct House
        {
            public Vector3 slot;
            public int baysX, baysZ;
        }

        static readonly Dictionary<string, Piece> Cache = new Dictionary<string, Piece>();

        [MenuItem("Tools/Echoes/Generate Grey Village", priority = 20)]
        public static void QuickGenerate()
        {
            var count = Generate(new Settings
            {
                seed = 20260926,
                houseCount = 24,
                minCells = 2,
                maxCells = 3,
                minStoreys = 1,
                maxStoreys = 3,
                spacing = 3f,
                addProps = true,
                addFences = true,
                addGround = true,
                addSquare = true,
                addPaths = true,
            });

            Debug.Log($"[Echoes] Village ready: {count} building(s) under '{RootName}'. " +
                      "All Color Restore = 0 (grey). Open Window > Echoes > Grey Village " +
                      "Generator to tune and rebuild.");
        }

        public struct Settings
        {
            public int seed, houseCount, minCells, maxCells;
            public int minStoreys, maxStoreys;
            public float spacing;
            public bool addProps, addFences, addGround, addSquare, addPaths;
        }

        public static int Generate(Settings s)
        {
            if (Measure("Wall_Plaster_Straight").size == Vector3.zero)
            {
                Debug.LogError("[Echoes] Village kit models not found under " + ModelDir +
                               " - run Tools > Echoes > Import Village Kit first.");
                return 0;
            }

            Delete();

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Generate Grey Village");

            var rng = new System.Random(s.seed);

            if (s.addGround) BuildGround(root.transform);

            // The market square sits at the origin and buildings ring it, so the
            // village has a centre and the paths have somewhere to go. Slots are
            // taken nearest-centre first, so raising the building count grows the
            // village outward as a compact cluster rather than a scattered ring.
            //
            // Only the single centre cell is reserved, not a 3x3 block. A cell is
            // about the size of the paved square, so clearing three of them per side
            // left a 26-unit empty apron between the plaza and the nearest roof.
            float pitchX = 14f + s.spacing;
            float pitchZ = 13f + s.spacing;
            float jitter = s.spacing * 0.33f;
            int halfRing = Mathf.Max(2, Mathf.CeilToInt(Mathf.Sqrt(s.houseCount) * 0.5f));

            var cells = new List<Vector2Int>();
            for (int cz = -halfRing; cz <= halfRing; cz++)
            for (int cx = -halfRing; cx <= halfRing; cx++)
                if (!s.addSquare || cx != 0 || cz != 0)
                    cells.Add(new Vector2Int(cx, cz));

            cells.Sort((a, b) => (a.x * a.x + a.y * a.y).CompareTo(b.x * b.x + b.y * b.y));

            var slots = new List<House>(s.houseCount);
            int built = 0;
            for (int i = 0; i < cells.Count && built < s.houseCount; i++)
            {
                var cell = cells[i];
                float jx = ((float)rng.NextDouble() - 0.5f) * 2f * jitter;
                float jz = ((float)rng.NextDouble() - 0.5f) * 2f * jitter;
                var slot = new Vector3(cell.x * pitchX + jx, 0f, cell.y * pitchZ + jz);

                int baysX = s.minCells + rng.Next(Mathf.Max(1, s.maxCells - s.minCells + 1));
                int baysZ = s.minCells + rng.Next(Mathf.Max(1, s.maxCells - s.minCells + 1));
                int storeys = s.minStoreys + rng.Next(Mathf.Max(1, s.maxStoreys - s.minStoreys + 1));

                // Stair_Interior_Simple needs a 4.62 clear run, so a building only
                // gets extra storeys if it is deep enough to hold the flight. A
                // deep single-storey cottage beats a shallow tower with no way up.
                if (storeys > 1 && baysZ < 3) baysZ = 3;

                if (BuildHouse(root.transform, slot, baysX, baysZ, storeys, rng, s.addProps,
                               out float radius))
                {
                    built++;
                    slots.Add(new House { slot = slot, baysX = baysX, baysZ = baysZ });
                    if (s.addFences && i % 3 == 0) BuildFence(root.transform, slot, radius, rng);
                }
            }

            if (s.addSquare) BuildSquare(root.transform, rng);
            if (s.addPaths) BuildPaths(root.transform, slots, SquareHalf, rng);

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = root;
            return built;
        }

        public static void Delete()
        {
            var existing = GameObject.Find(RootName);
            while (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                existing = GameObject.Find(RootName);
            }
            Cache.Clear();
        }

        // ---- pieces -------------------------------------------------------

        static Material LoadMat(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/{name}.mat");

        /// <summary>
        /// Measure a prefab's world-space mesh bounds once, then cache them.
        ///
        /// Instantiate the prefab at the scene root and read Renderer.bounds. That
        /// frame is the authored one — walls come out 2.00 x 3.12 x 0.41, floors
        /// 2.00 x 0.02 x 2.00 — and Spawn() reproduces exactly this frame at yaw 0,
        /// which is what makes the two agree.
        ///
        /// Do not try to derive this from mesh.bounds instead. The kit's FBX roots
        /// carry a 100x scale and a 270-degree rotation about X, so the local axes are
        /// permuted relative to world: the wall's 3.12 height is the mesh's local Z.
        /// Scaling local size by lossyScale gets that wrong, and so does taking the
        /// length of each transformed local axis (that measures the axis image, not
        /// the world AABB extent).
        ///
        /// The placement offset has to be measured rather than assumed: the prefab
        /// pivot is not at the mesh centre, and getting it wrong buries geometry
        /// below ground without any error being raised.
        /// </summary>
        static Piece Measure(string modelName)
        {
            if (Cache.TryGetValue(modelName, out var cached)) return cached;

            var piece = new Piece();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{modelName}.fbx");
            if (model != null)
            {
                var probe = (GameObject)PrefabUtility.InstantiatePrefab(model);
                probe.hideFlags = HideFlags.HideAndDontSave;

                var rend = probe.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    var b = rend.bounds;
                    piece.size = b.size;
                    piece.baseOffset = b.min - probe.transform.position;
                }

                UnityEngine.Object.DestroyImmediate(probe);
            }

            Cache[modelName] = piece;
            return piece;
        }

        /// <summary>
        /// Instantiate a model under <paramref name="parent"/> at a world position,
        /// yawed about world Y, in the same orientation Measure() reports.
        /// </summary>
        static GameObject Spawn(string modelName, Transform parent, Vector3 worldPos,
                                float yaw, Material mat)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{modelName}.fbx");
            if (model == null) return null;

            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);

            // PrefabUtility.InstantiatePrefab with a parent does not carry the FBX
            // root's authored rotation across, and these roots are rotated 270 deg
            // about X. Left uncorrected, every wall, door and floor tile arrives
            // lying on its side (walls report 0.41 tall instead of 3.12). Re-assert
            // the authored rotation, then yaw in world space on top of it.
            var upright = Quaternion.Euler(0f, yaw, 0f) *
                          Quaternion.Euler(model.transform.localEulerAngles);

            go.transform.SetPositionAndRotation(worldPos, parent.rotation * upright);
            ApplyMaterial(go, mat);
            return go;
        }

        /// <summary>
        /// The world position to hand Spawn() so the piece's XZ bounds are centred on
        /// <paramref name="worldPoint"/> and its lowest point rests at worldPoint.y.
        ///
        /// The XZ half of the offset is the AABB centre, not the AABB minimum. Using
        /// the minimum instead shifts every piece by half its own size: a 2-unit wall
        /// lands a whole bay off, and because the error is the same for every piece
        /// the result is a uniformly displaced building rather than an obviously
        /// broken one. Y stays on the minimum on purpose, so callers can rest
        /// geometry on a floor or a wall plate.
        /// </summary>
        static Vector3 RootAt(string modelName, Vector3 worldPoint, float yaw)
        {
            var piece = Measure(modelName);
            var centre = new Vector3(piece.baseOffset.x + piece.size.x * 0.5f,
                                     piece.baseOffset.y,
                                     piece.baseOffset.z + piece.size.z * 0.5f);
            return worldPoint - Quaternion.Euler(0f, yaw, 0f) * centre;
        }

        /// <summary>
        /// Spawn a piece centred on <paramref name="localTarget"/> (in the parent's
        /// space) in XZ, with its lowest point resting at localTarget.y, yawed about Y.
        ///
        /// This is the only placement that survives the kit's 100x root scale and its
        /// off-centre pivots. <paramref name="parent"/> must not be scaled.
        /// </summary>
        static GameObject SpawnCentered(string modelName, Transform parent, Vector3 localTarget,
                                       float yaw, Material mat)
        {
            if (Measure(modelName).size == Vector3.zero) return null;
            var world = parent.TransformPoint(localTarget);
            return Spawn(modelName, parent, RootAt(modelName, world, yaw), yaw, mat);
        }

        /// <summary>
        /// Slide a piece along its own local +X so its XZ centre sits on the given root.
        ///
        /// Doors are the reason this exists. Door_1_Flat's pivot is on its left edge
        /// and its mesh hangs 0.51 to one side of that pivot, while the wall opening
        /// and the door frame around it are both centred on the wall. Sharing the
        /// wall's root therefore drops the door half a bay off-centre — measured at
        /// 0.51 out on a 2.00 bay — even though the frame lands correctly.
        /// </summary>
        static Vector3 Recentre(string modelName, Vector3 worldRoot, float yaw)
        {
            var piece = Measure(modelName);
            if (piece.size == Vector3.zero) return worldRoot;
            float shift = -(piece.baseOffset.x + piece.size.x * 0.5f);
            return worldRoot + Quaternion.Euler(0f, yaw, 0f) * new Vector3(shift, 0f, 0f);
        }

        /// <summary>
        /// The world position to hand Spawn() so the piece's XZ bounds are centred on
        /// <paramref name="localTarget"/> (in the parent's space).
        ///
        /// Use this for pieces that have to share a frame with something else — a
        /// window add-on and the wall it sits in, a door and the wall it fills. Those
        /// pieces are authored against each other, so they must share a root exactly;
        /// placing each one by its own bounds would drift them apart.
        /// </summary>
        static Vector3 RootFor(string modelName, Transform parent, Vector3 localTarget, float yaw) =>
            RootAt(modelName, parent.TransformPoint(localTarget), yaw);

        /// <summary>
        /// Position in <paramref name="house"/> local space for a wall bay.
        ///
        /// <paramref name="yaw"/> faces the piece's outward normal along the matching
        /// compass direction (0 = +Z, 90 = +X, 180 = -Z, 270 = -X),
        /// <paramref name="along"/> slides sideways around the wall line, and
        /// <paramref name="outward"/> pushes off the footprint edge. This is what
        /// lets all four sides share one placement expression.
        /// </summary>
        static Vector3 WallSlot(float yaw, float along, float outward, float y)
        {
            float c = Mathf.Cos(yaw * Mathf.Deg2Rad);
            float s = Mathf.Sin(yaw * Mathf.Deg2Rad);
            return new Vector3(c * along + s * outward, y, -s * along + c * outward);
        }

        static void ApplyMaterial(GameObject go, Material mat)
        {
            if (mat == null) return;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterial = mat;
        }

        // ---- ground -------------------------------------------------------

        static void BuildGround(Transform parent)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(parent);
            ground.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            ground.transform.localScale = new Vector3(400f, 0.5f, 400f);
            ApplyMaterial(ground, LoadMat("Grey_Ground"));
        }

        // ---- house --------------------------------------------------------

        static bool BuildHouse(Transform parent, Vector3 slot, int baysX, int baysZ, int storeys,
                               System.Random rng, bool addProps, out float footprintRadius,
                               string roofOverride = null, bool stairless = false)
        {
            footprintRadius = 0f;

            if (Measure("Wall_Plaster_Straight").size == Vector3.zero)
            {
                Debug.LogWarning("[Echoes] 'Wall_Plaster_Straight' missing or empty — " +
                                 "check Assets/Art/Village/Models.");
                return false;
            }

            baysX = Mathf.Max(1, baysX);
            baysZ = Mathf.Max(1, baysZ);
            storeys = Mathf.Max(1, storeys);

            float W = baysX * BayLen;
            float D = baysZ * BayLen;

            string roofName = roofOverride ?? PickRoof(baysX, baysZ);
            if (string.IsNullOrEmpty(roofName))
            {
                Debug.LogWarning($"[Echoes] No surveyed roof covers a {baysX} x {baysZ} bay " +
                                 "footprint. Add it to RoofTable or lower the cell range.");
                return false;
            }

            var house = new GameObject($"House_{slot.x:0}_{slot.z:0}");
            house.transform.SetParent(parent, false);
            house.transform.localPosition = slot;
            footprintRadius = Mathf.Sqrt(W * W + D * D) * 0.5f;

            int doorBay = rng.Next(baysX);
            int balconyBay = rng.Next(baysX);
            bool hasStair = storeys > 1 && !stairless && baysZ >= 3;

            for (int s = 0; s < storeys; s++)
                BuildStorey(house.transform, W, D, baysX, baysZ, s,
                            doorBay, balconyBay,
                            stairArrives: hasStair && s > 0,
                            rng);

            // One flight per storey gap, not per building. A three-storey house needs
            // two, and with only one the top floor is unreachable.
            for (int s = 0; s < storeys - 1; s++)
                BuildStair(house.transform, W, D, s * StoreyH, rng);

            // ---- roof ----
            // A real hipped roof from the kit, sized off the surveyed table. This
            // replaces the earlier stretched-slab placeholder, which existed only
            // because scaling a 270-degree-rotated 100x root shears the mesh and
            // throws the child off its pivot.
            var roof = Measure(roofName);
            float wallTop = storeys * StoreyH;

            if (roof.size.x < W - 0.01f || roof.size.z < D - 0.01f)
                Debug.LogWarning($"[Echoes] '{roofName}' is {roof.size.x:0.00} x {roof.size.z:0.00} " +
                                 $"but the walls are {W:0.00} x {D:0.00} — walls will poke through " +
                                 "the eaves. Re-run Tools > Echoes > Survey Kit.");

            SpawnCentered(roofName, house.transform, new Vector3(0f, wallTop, 0f), 0f,
                          LoadMat("RoundTiles"));

            // ---- gable ends ----
            // These roofs are NOT hipped. Measured from the mesh vertices
            // (Tools > Echoes > Probe Roof Shapes), Roof_RoundTiles_NxM narrows in
            // X while Z stays constant, so it is a triangular prism with a ridge
            // running along Z. A raycast through the end (Tools > Echoes > Probe
            // Gable Holes) passes clean through 13 of the 14 round-tile roofs while
            // being blocked across the span, so every one of these houses has a
            // see-through hole at both gable ends until something closes them.
            //
            // The kit ships exactly that: Roof_Front_BrickN, named for the same N as
            // the roof it belongs to. One at each end of the ridge.
            string gable = GableForRoof(roofName);
            if (gable == null)
            {
                Debug.LogWarning($"[Echoes] '{roofName}' has no Roof_Front_Brick piece to " +
                                 "close its gable ends; that building will be see-through above the walls.");
            }
            else
            {
                for (int e = 0; e < 2; e++)
                {
                    float sz = e == 0 ? 1f : -1f;
                    SpawnCentered(gable, house.transform,
                                  new Vector3(0f, wallTop, sz * D * 0.5f), 0f,
                                  LoadMat("Brick"));
                }
            }

            // ---- props ----
            if (addProps)
            {
                if (rng.NextDouble() < 0.75)
                {
                    // Start the chimney partway up the roof slope so it reads as
                    // emerging from it rather than being dropped on top of it.
                    SpawnCentered(rng.Next(2) == 0 ? "Prop_Chimney" : "Prop_Chimney2",
                                  house.transform,
                                  new Vector3((rng.Next(2) == 0 ? -1f : 1f) * W * 0.22f,
                                              wallTop + roof.size.y * 0.42f,
                                              (rng.Next(2) == 0 ? -1f : 1f) * D * 0.18f),
                                  0f, LoadMat("Brick"));
                }

                if (rng.NextDouble() < 0.5)
                    SpawnCentered("Prop_Crate", house.transform,
                                  new Vector3(-W * 0.5f + 0.7f, 0f, D * 0.5f + 0.8f),
                                  rng.Next(0, 4) * 90f, LoadMat("WoodTrim"));

                // Vines read well even fully grey — they hint at the colour to come.
                int vines = rng.Next(1, 4);
                for (int v = 0; v < vines; v++)
                {
                    int storey = rng.Next(storeys);
                    float along = -W * 0.5f + BayLen * (0.5f + rng.Next(baysX));
                    SpawnCentered(WallVines[rng.Next(WallVines.Length)], house.transform,
                                  WallSlot(0f, along, D * 0.5f, storey * StoreyH),
                                  0f, LoadMat("VineLeaf"));
                }

                if (rng.NextDouble() < 0.4)
                {
                    SpawnCentered(BushVines[rng.Next(BushVines.Length)], house.transform,
                                  new Vector3((rng.Next(2) == 0 ? -1f : 1f) * (W * 0.5f + 1.2f),
                                              0f, ((float)rng.NextDouble() - 0.5f) * D),
                                  rng.Next(0, 4) * 90f, LoadMat("VineLeaf"));
                }
            }

            // Every house fades back to colour as one unit.
            house.AddComponent<ColorRestoreTarget>().SetRestoreImmediate(0f);
            return true;
        }

        /// <summary>
        /// One storey of one building: floor, four wall runs, corner posts, and the
        /// dressing (door, window, balcony) that hangs off them.
        ///
        /// The four sides share one placement expression via WallSlot, so "the third
        /// bay of the east wall" is the same code as "the third bay of the front".
        /// </summary>
        static void BuildStorey(Transform house, float W, float D, int baysX, int baysZ,
                                int storeyIndex, int doorBay, int balconyBay,
                                bool stairArrives, System.Random rng)
        {
            float y = storeyIndex * StoreyH;
            bool ground = storeyIndex == 0;

            // ---- floor ----
            // The storey the flight arrives into leaves the tile the stair tops out
            // on open, otherwise the stairs walk into a solid ceiling.
            string[] floorPieces =
            {
                "Floor_Brick", "Floor_RedBrick", "Floor_UnevenBrick",
                "Floor_WoodDark", "Floor_WoodLight",
            };
            Material[] floorMats =
            {
                LoadMat("Brick"), LoadMat("RedBrick"), LoadMat("UnevenBrick"),
                LoadMat("WoodTrim"), LoadMat("WoodTrim"),
            };

            for (int x = 0; x < baysX; x++)
            for (int z = 0; z < baysZ; z++)
            {
                if (stairArrives && x == 0 && z == baysZ - 1) continue;

                int t = rng.Next(floorPieces.Length);
                // Ground floors are stone, upper floors mostly timber.
                if (ground && t > 2) t = rng.Next(3);

                SpawnCentered(floorPieces[t], house,
                              new Vector3(-W * 0.5f + (x + 0.5f) * BayLen, YFloor,
                                          -D * 0.5f + (z + 0.5f) * BayLen),
                              0f, floorMats[t]);
            }

            // ---- walls ----
            for (int side = 0; side < 4; side++)
            {
                float yaw = side * 90f;
                int bays = (side % 2 == 0) ? baysX : baysZ;
                float run = bays * BayLen;
                float outward = (side % 2 == 0) ? D * 0.5f : W * 0.5f;

                for (int i = 0; i < bays; i++)
                {
                    float along = -run * 0.5f + (i + 0.5f) * BayLen;
                    var target = WallSlot(yaw, along, outward, y);

                    bool isDoor = ground && side == 0 && i == doorBay;
                    string wallName;
                    if (isDoor)
                        wallName = DoorWalls[rng.Next(DoorWalls.Length)];
                    else
                    {
                        // Windows get denser as you go up: the ground floor of a
                        // village house is mostly wall, the upper floors are not.
                        float chance = ground ? 0.22f : 0.45f;
                        wallName = rng.NextDouble() < chance
                            ? WindowWalls[rng.Next(WindowWalls.Length)]
                            : PlainWalls[rng.Next(PlainWalls.Length)];
                    }

                    var wallMat = wallName.StartsWith("Wall_UnevenBrick")
                        ? LoadMat("UnevenBrick")
                        : LoadMat("Plaster");

                    var root = RootFor(wallName, house, target, yaw);
                    Spawn(wallName, house, root, yaw, wallMat);

                    if (isDoor)
                        DressDoor(house, root, yaw, rng);
                    else if (wallName.Contains("Window"))
                        DressWindow(house, root, yaw, wallName, rng);

                    if (!ground && side == 0 && i == balconyBay && rng.NextDouble() < 0.45f)
                        DressBalcony(house, root, yaw);
                }
            }

            // ---- corners ----
            FillCorners(house, W, D, y, storeyIndex, rng);
        }

        /// <summary>
        /// Close the four vertical corners of a storey.
        ///
        /// Walls are laid edge to edge around the footprint, so each pair that meets
        /// at a corner leaves a small void on the outside of the turn. One corner
        /// post per corner fills it — but the post is 3.02 tall against a 3.12
        /// storey, which would leave a 0.10 slit at every floor line. Stacking a
        /// second post 0.10 lower makes the pair span the full 3.12 with a harmless
        /// overlap instead of a gap. Only storeys above the ground need the filler,
        /// because the ground storey's joint is under the wall base.
        /// </summary>
        static void FillCorners(Transform house, float W, float D, float y, int storeyIndex,
                                System.Random rng)
        {
            string corner = rng.Next(2) == 0 ? "Corner_Exterior_Brick" : "Corner_ExteriorWide_Brick";
            var mat = LoadMat("Brick");
            float drop = StoreyH - CornerH;

            // Yaw is chosen so the post's body faces out of the building's corner:
            // 0 -> (+X,+Z), 90 -> (+X,-Z), 180 -> (-X,-Z), 270 -> (-X,+Z).
            for (int c = 0; c < 4; c++)
            {
                float yaw = c * 90f;
                var at = new Vector3((c == 0 || c == 3) ? W * 0.5f : -W * 0.5f,
                                     y,
                                     (c < 2) ? D * 0.5f : -D * 0.5f);

                SpawnCentered(corner, house, at, yaw, mat);
                if (storeyIndex > 0)
                    SpawnCentered(corner, house, at - new Vector3(0f, drop, 0f), yaw, mat);
            }
        }

        /// <summary>
        /// Put a door and its frame in a door wall.
        ///
        /// Both are placed against the wall's own root so the frame stays square in
        /// its opening, and both are then re-centred on their own bounds. Door meshes
        /// are authored with the pivot on one edge, so sharing the root alone leaves
        /// the door hanging about half a bay to one side of the frame.
        /// </summary>
        static void DressDoor(Transform house, Vector3 wallRoot, float yaw, System.Random rng)
        {
            bool round = rng.Next(3) == 0;
            string doorName = "Door_" + (rng.Next(2) == 0 ? "1" : "4") + (round ? "_Round" : "_Flat");
            string frameName = round
                ? (rng.Next(2) == 0 ? "DoorFrame_Round_Brick" : "DoorFrame_Round_WoodDark")
                : (rng.Next(2) == 0 ? "DoorFrame_Flat_Brick" : "DoorFrame_Flat_WoodDark");

            Spawn(doorName, house, Recentre(doorName, wallRoot, yaw), yaw, LoadMat("WoodTrim"));
            Spawn(frameName, house, Recentre(frameName, wallRoot, yaw), yaw,
                  LoadMat(frameName.Contains("Brick") ? "Brick" : "WoodTrim"));
        }

        /// <summary>
        /// Put a glazed window in a window wall, and sometimes open shutters on it.
        ///
        /// The window add-on is authored to sit in its wall, so it shares the wall's
        /// root exactly rather than being centred independently.
        ///
        /// Only the _Open shutter variants are used. Their panels fold out past the
        /// window's own width so nothing intersects; the _Closed variants are
        /// narrower than the window and sit inside its depth, so they would cut
        /// straight through the glass.
        /// </summary>
        static void DressWindow(Transform house, Vector3 wallRoot, float yaw, string wallName,
                                System.Random rng)
        {
            bool round = wallName.Contains("Round");
            bool thin = wallName.Contains("Thin");

            string winName = thin ? "Window_Thin_Round1"
                        : round ? "Window_Wide_Round1"
                               : "Window_Wide_Flat1";
            Spawn(winName, house, wallRoot, yaw, LoadMat("WindowPane"));

            if (rng.NextDouble() >= 0.55) return;

            string shutName = thin ? "WindowShutters_Thin_Round_Open"
                           : round ? "WindowShutters_Wide_Round_Open"
                                  : "WindowShutters_Wide_Flat_Open";
            Spawn(shutName, house, wallRoot, yaw, LoadMat("WoodTrim"));
        }

        /// <summary>
        /// Hang a balcony off an upper storey.
        ///
        /// Balcony_Simple_Straight is authored to start 0.90 out from its own root, so
        /// left on the wall's centre point its deck would float 0.80 clear of the wall
        /// face. Push it back by that difference and the deck meets the wall.
        /// </summary>
        static void DressBalcony(Transform house, Vector3 wallRoot, float yaw)
        {
            const string name = "Balcony_Simple_Straight";
            var piece = Measure(name);
            if (piece.size == Vector3.zero) return;

            float push = WallT * 0.5f - piece.baseOffset.z;
            var q = Quaternion.Euler(0f, yaw, 0f);
            Spawn(name, house, wallRoot + q * new Vector3(0f, 0f, push), yaw, LoadMat("WoodTrim"));
        }

        /// <summary>
        /// Run an interior flight from this storey up to the next.
        ///
        /// Stair_Interior_* is authored descending along -Z from a landing at its
        /// root, so centring it against the back wall puts the bottom step in the
        /// corner and the top step in the middle of the floor. It is 1.68 wide, so
        /// it is pushed into the first bay rather than centred, which is what keeps
        /// it clear of the side wall's inner face.
        /// </summary>
        static void BuildStair(Transform house, float W, float D, float y, System.Random rng)
        {
            string name = null;
            var piece = default(Piece);

            foreach (var option in StairPieces)
            {
                var p = Measure(option);
                if (p.size == Vector3.zero) continue;
                // Needs to clear the side walls and finish before the front wall.
                if (p.size.z > D - WallT) continue;
                if (p.size.x > W - WallT) continue;
                name = option;
                piece = p;
                break;
            }

            if (name == null)
            {
                Debug.LogWarning($"[Echoes] No interior stair fits a {W:0.00} x {D:0.00} bay; " +
                                 "storeys above this one are unreachable.");
                return;
            }

            // Sits on the first bay's centre line, nudged inboard on both counts: the
            // flight is 1.68 wide against a 2.00 bay, so a bay-centre placement
            // drives its outer edge into the side wall's inner face, and a back-wall
            // placement drives its foot into that wall instead of stopping at it.
            SpawnCentered(name, house,
                          new Vector3(-W * 0.5f + BayLen * 0.5f + 0.1f,
                                      y + YFloor,
                                      -D * 0.5f + piece.size.z * 0.5f + WallT * 0.5f),
                          0f, LoadMat("WoodTrim"));
        }

        /// <summary>
        /// Smallest surveyed roof whose wall footprint still covers this one.
        ///
        /// The roof names encode the wall footprint they sit on: Roof_RoundTiles_6x8
        /// is the roof for a 6 x 8 unit building, which is 3 x 4 bays.
        /// </summary>
        static string PickRoof(int baysX, int baysZ)
        {
            string best = null;
            int bestArea = int.MaxValue;

            foreach (var name in RoofTable)
            {
                int fitX, fitZ;
                if (!RoofFits(name, out fitX, out fitZ)) continue;
                if (fitX < baysX || fitZ < baysZ) continue;

                int area = fitX * fitZ;
                if (area < bestArea)
                {
                    bestArea = area;
                    best = name;
                }
            }

            return best;
        }

        /// <summary>
        /// The Roof_Front_Brick piece that closes the gable ends of
        /// <paramref name="roofName"/>, or null if the kit has none for it.
        ///
        /// The kit pairs these by number: Roof_RoundTiles_4x* takes
        /// Roof_Front_Brick4, 6x* takes _6, 8x* takes _8. Only the first number is
        /// read, because it is the span the gable triangle has to cover — the ridge
        /// runs along Z, so the gable is closed at the Z ends and spans the X width.
        ///
        /// Returns null rather than guessing for anything that is not a
        /// Roof_RoundTiles_* piece, so a genuinely closed roof (Roof_Tower_RoundTiles,
        /// which tapers in both axes) is not given a gable it does not need.
        /// </summary>
        static string GableForRoof(string roofName)
        {
            if (string.IsNullOrEmpty(roofName)) return null;
            if (!roofName.StartsWith("Roof_RoundTiles_")) return null;

            int i = roofName.LastIndexOf('_');
            if (i < 0 || i + 1 >= roofName.Length) return null;

            var parts = roofName.Substring(i + 1).Split('x');
            int span;
            if (parts.Length != 2 || !int.TryParse(parts[0], out span)) return null;

            string candidate = "Roof_Front_Brick" + span;
            return Measure(candidate).size == Vector3.zero ? null : candidate;
        }

        static bool RoofFits(string name, out int baysX, out int baysZ)
        {
            baysX = baysZ = 0;
            if (Measure(name).size == Vector3.zero) return false;

            int i = name.LastIndexOf('_');
            if (i < 0 || i + 1 >= name.Length) return false;

            var parts = name.Substring(i + 1).Split('x');
            int a, b;
            if (parts.Length != 2 || !int.TryParse(parts[0], out a) || !int.TryParse(parts[1], out b))
                return false;

            baysX = a / 2;
            baysZ = b / 2;
            return true;
        }

        // ---- market square ------------------------------------------------

        public const int SquareBays = 8;                 // 16 x 16 paved
        public const float SquareHalf = SquareBays * BayLen * 0.5f;

        /// <summary>
        /// The village centre: paving, a kerb, a landmark tower and a loading dock.
        ///
        /// The tower is the only reason the square reads as a destination. It is a
        /// three-storey 3 x 3 bay building on the normal code path, so it gets real
        /// walls, a real stair and real corner posts like everything else.
        /// </summary>
        static void BuildSquare(Transform parent, System.Random rng)
        {
            var square = new GameObject("MarketSquare");
            square.transform.SetParent(parent, false);

            string[] pavers = { "Floor_Brick", "Floor_RedBrick", "Floor_UnevenBrick" };
            Material[] paverMats = { LoadMat("Brick"), LoadMat("RedBrick"), LoadMat("UnevenBrick") };

            for (int x = 0; x < SquareBays; x++)
            for (int z = 0; z < SquareBays; z++)
            {
                int t = rng.Next(pavers.Length);
                SpawnCentered(pavers[t], square.transform,
                              new Vector3(-SquareHalf + (x + 0.5f) * BayLen, YSquare,
                                          -SquareHalf + (z + 0.5f) * BayLen),
                              0f, paverMats[t]);
            }

            // ---- kerb ----
            // Prop_ExteriorBorder_Straight1 is a 2.00 strip authored to extend +Z
            // from its root, and Prop_ExteriorBorder_Corner is a 0.70 block authored
            // to extend +X and +Z. Both are placed by their AABB centre here, so the
            // straight runs sit flush outside the paving at SquareHalf and the corner
            // blocks fill the 0.70 gap the runs leave where they meet.
            var kerbMat = LoadMat("RockTrim");
            int n = SquareBays;
            for (int side = 0; side < 4; side++)
            {
                float yaw = side * 90f;
                for (int i = 0; i < n; i++)
                {
                    float along = -SquareHalf + (i + 0.5f) * BayLen;
                    SpawnCentered("Prop_ExteriorBorder_Straight1", square.transform,
                                  WallSlot(yaw, along, SquareHalf + 0.35f, 0f), yaw, kerbMat);
                }
            }

            // The straight runs stop at +/-SquareHalf, so each corner block fills the
            // 0.70 square left over beyond that, on both axes at once.
            for (int c = 0; c < 4; c++)
            {
                float sx = (c == 0 || c == 3) ? 1f : -1f;
                float sz = (c < 2) ? 1f : -1f;
                SpawnCentered("Prop_ExteriorBorder_Corner", square.transform,
                              new Vector3(sx * (SquareHalf + 0.35f), 0f,
                                          sz * (SquareHalf + 0.35f)),
                              0f, kerbMat);
            }

            // ---- landmark tower ----
            BuildHouse(square.transform, Vector3.zero, baysX: 3, baysZ: 3, storeys: 3,
                       rng: rng, addProps: true, out float _);

            // ---- loading dock ----
            // Stairs_Exterior_Platform is a full 1.0-unit step, which is too tall to
            // be a doorstep but exactly right for a loading stage, so the wagon and
            // crates sit on top of it rather than beside a door.
            //
            // It has to clear the tower: a 3 x 3 bay tower has its outer wall face
            // at x = -3.2, so the dock's near edge is pushed out to -4.
            var dock = new GameObject("LoadingDock");
            dock.transform.SetParent(square.transform, false);
            dock.transform.localPosition = new Vector3(-SquareHalf + BayLen, 0f, 0f);

            for (int i = 0; i < 2; i++)
            for (int j = 0; j < 3; j++)
                SpawnCentered("Stairs_Exterior_Platform", dock.transform,
                              new Vector3((i - 0.5f) * BayLen, 0f, (j - 1f) * BayLen),
                              0f, LoadMat("WoodTrim"));

            SpawnCentered("Prop_Wagon", dock.transform, new Vector3(0f, 1f, -BayLen * 0.5f),
                          90f, LoadMat("WoodTrim"));

            for (int i = 0; i < 3; i++)
                SpawnCentered("Prop_Crate", dock.transform,
                              new Vector3((i - 1f) * 1.2f, 1f, BayLen),
                              rng.Next(0, 4) * 90f, LoadMat("WoodTrim"));

            // Planting in the corners the tower and dock leave free. Kept off the
            // dock's footprint because the largest bush is 4.14 across and would
            // grow straight through the platform.
            for (int i = 0; i < 4; i++)
            {
                float sx = (i % 2 == 0) ? 1f : -1f;
                float sz = (i < 2) ? 1f : -1f;
                SpawnCentered(BushVines[rng.Next(BushVines.Length)], square.transform,
                              new Vector3(sx * (SquareHalf - 1.2f), 0f,
                                          sz > 0f ? (SquareHalf - 1.2f) : -(SquareHalf - 3f)),
                              rng.Next(0, 4) * 90f, LoadMat("VineLeaf"));
            }

            // Ari repaints shared ground as well as walls, so the plaza, kerb,
            // loading dock and planting all take a target. Added last, once the
            // children exist, so Awake sees the finished hierarchy — and the
            // landmark tower keeps its own target rather than being absorbed,
            // which is what ColorRestoreTarget's nested-ownership rule is for.
            square.AddComponent<ColorRestoreTarget>().SetRestoreImmediate(0f);
        }

        // ---- paths --------------------------------------------------------

        /// <summary>
        /// Paved runs from each building to the square.
        ///
        /// Streets here are L-shaped rather than radial, because a 2-unit square
        /// tile run diagonally reads as a staircase of disconnected slabs. Each
        /// building walks one axis to the centre line and then the other, with the
        /// axis order chosen per building, which gives a varied but legible street
        /// plan and leaves the square as the natural meeting point.
        /// </summary>
        static void BuildPaths(Transform parent, List<House> houses, float squareHalf,
                               System.Random rng)
        {
            var paths = new GameObject("Paths");
            paths.transform.SetParent(parent, false);

            // Full 2x2 tiles only. The kit's Floor_*_Half pieces are 2.00 x 1.00,
            // and the run steps by a whole bay, so a half tile would leave a 1-unit
            // gap in the paving every other step.
            string[] tiles = { "Floor_WoodDark", "Floor_Brick", "Floor_UnevenBrick" };
            Material[] mats = { LoadMat("WoodTrim"), LoadMat("Brick"), LoadMat("UnevenBrick") };

            foreach (var h in houses)
            {
                int[] order = rng.Next(2) == 0 ? new[] { 0, 2 } : new[] { 2, 0 };
                var cur = h.slot;

                foreach (int axis in order)
                {
                    // Step out past this building's own footprint before paving.
                    float half = (axis == 0 ? h.baysX : h.baysZ) * BayLen * 0.5f;
                    float sign = cur[axis] < 0f ? -1f : 1f;
                    float from = Mathf.Abs(cur[axis]) - half;
                    if (from < 0f) { cur[axis] = 0f; continue; }

                    // Paving is laid on whole-bay steps and stops one bay short of
                    // the square, because the plaza is paved at YSquare and anything
                    // overlapping it would z-fight with the plaza tiles.
                    int steps = Mathf.FloorToInt((from - (squareHalf - BayLen)) / BayLen) + 1;
                    for (int i = 0; i < steps; i++)
                    {
                        cur[axis] = (from - i * BayLen) * sign;
                        if (Blocked(cur, houses, h)) continue;

                        int t = rng.Next(tiles.Length);
                        SpawnCentered(tiles[t], paths.transform,
                                      new Vector3(cur.x, YPath, cur.z), 0f, mats[t]);
                    }

                    cur[axis] = 0f;
                }
            }

            // The streets are paintable for the same reason the square is. One
            // target for the whole run, not one per tile — otherwise a single
            // brush stroke would have to light up several hundred components.
            paths.AddComponent<ColorRestoreTarget>().SetRestoreImmediate(0f);
        }

        static bool Blocked(Vector3 p, List<House> houses, House self)
        {
            foreach (var h in houses)
            {
                if (h.slot == self.slot) continue;
                if (Mathf.Abs(p.x - h.slot.x) < h.baysX * BayLen * 0.5f + 0.4f &&
                    Mathf.Abs(p.z - h.slot.z) < h.baysZ * BayLen * 0.5f + 0.4f)
                    return true;
            }
            return false;
        }

        // ---- fence --------------------------------------------------------

        static void BuildFence(Transform parent, Vector3 slot, float radius, System.Random rng)
        {
            var piece = Measure("Prop_WoodenFence_Single");
            if (piece.size.x <= 0.01f) return;

            float len = piece.size.x;
            int count = Mathf.Clamp(Mathf.RoundToInt(radius * 1.6f / len), 2, 24);

            var fence = new GameObject("Fence");
            fence.transform.SetParent(parent, false);

            float yaw = rng.Next(4) * 90f;
            fence.transform.localPosition = WallSlot(yaw, 0f, radius + 2.2f, 0f) +
                                            new Vector3(slot.x, 0f, slot.z);
            fence.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            for (int i = 0; i < count; i++)
            {
                float along = (-count + 1 + 2 * i) * len * 0.5f;
                SpawnCentered("Prop_WoodenFence_Single", fence.transform,
                              new Vector3(along, 0f, 0f), 0f, LoadMat("WoodTrim"));
            }
        }
    }

    /// <summary>Tuning window for the generator.</summary>
    public sealed class VillageGeneratorWindow : EditorWindow
    {
        VillageGenerator.Settings _s = new VillageGenerator.Settings
        {
            seed = 20260926,
            houseCount = 24,
            minCells = 2,
            maxCells = 3,
            minStoreys = 1,
            maxStoreys = 3,
            spacing = 3f,
            addProps = true,
            addFences = true,
            addGround = true,
            addSquare = true,
            addPaths = true,
        };

        Vector2 _scroll;

        [MenuItem("Window/Echoes/Grey Village Generator", priority = 20)]
        static void Open() => GetWindow<VillageGeneratorWindow>("Grey Village");

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
            _s.seed = EditorGUILayout.IntField("Seed", _s.seed);
            _s.houseCount = Mathf.Clamp(EditorGUILayout.IntField("Building count", _s.houseCount), 1, 60);
            _s.minCells = Mathf.Clamp(EditorGUILayout.IntField("Min cells/side", _s.minCells), 1, 8);
            _s.maxCells = Mathf.Clamp(EditorGUILayout.IntField("Max cells/side", _s.maxCells), _s.minCells, 8);
            _s.spacing = EditorGUILayout.FloatField("Spacing (m)", _s.spacing);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Storeys", EditorStyles.boldLabel);
            _s.minStoreys = Mathf.Clamp(EditorGUILayout.IntField("Min storeys", _s.minStoreys), 1, 5);
            _s.maxStoreys = Mathf.Clamp(EditorGUILayout.IntField("Max storeys", _s.maxStoreys), _s.minStoreys, 5);
            if (_s.maxStoreys > _s.minStoreys)
                EditorGUILayout.HelpBox("Multi-storey buildings place a real stair piece " +
                                        "between floors, so upper storeys are reachable. " +
                                        "This is the main thing that stops the village " +
                                        "reading as a row of sheds.",
                                        MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Contents", EditorStyles.boldLabel);
            _s.addGround = EditorGUILayout.Toggle("Ground plane", _s.addGround);
            _s.addSquare = EditorGUILayout.Toggle("Market square + landmark", _s.addSquare);
            _s.addPaths = EditorGUILayout.Toggle("Paths to square", _s.addPaths);
            _s.addFences = EditorGUILayout.Toggle("Fences", _s.addFences);
            _s.addProps = EditorGUILayout.Toggle("Props (crates, vines, chimneys)", _s.addProps);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Generate Village", GUILayout.Height(30)))
                    VillageGenerator.Generate(_s);

                if (GUILayout.Button("Delete Generated Village"))
                    VillageGenerator.Delete();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Every house gets a ColorRestoreTarget, so the whole village starts grey " +
                "(Color Restore = 0). Add a BrushPainter to any GameObject and enter play " +
                "mode: left click paints colour back, G greys everything, R restores all.",
                MessageType.Info);

            EditorGUILayout.EndScrollView();
        }
    }
}
