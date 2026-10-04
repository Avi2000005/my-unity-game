using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Puts Ari somewhere she can actually be seen and animated, and measures
    /// every part of it. Writes Temp/ari_stand.txt.
    ///
    /// Three separate things were wrong, and they are independent of each other:
    ///
    /// 1. She was behind the camera. The scene camera sits at the near corner
    ///    of the village at 45 degrees looking across it, so the world origin
    ///    — where she was parked — is directly behind it. 52 units back and out
    ///    of the frustum. Nothing about her being correct made her visible.
    ///
    /// 2. Her Animator was switched off. I left it disabled in the saved scene
    ///    on the reasoning that no movement script existed yet. That was the
    ///    wrong call: a saved scene with a dead Animator looks like a broken
    ///    model rather than an unfinished controller, and a disabled component
    ///    is the kind of thing nobody re-checks. She idles as soon as the
    ///    controller is live.
    ///
    /// 3. Her height off the ground is a guess until measured. Mixamo rigs have
    ///    the origin at the hips, not the soles, so a character dropped at y=0 is
    ///    buried up to the waist. The renderer's bounds cannot answer this — they
    ///    are inflated well past the silhouette. Baking the skin and reading the
    ///    lowest real vertex is what actually settles it.
    /// </summary>
    public static class AriStandOnGround
    {
        [MenuItem("Tools/Echoes/Stand Ari On The Square", priority = 63)]
        public static void Run()
        {
            try
            {
                RunInner();
            }
            catch (System.Exception e)
            {
                File.WriteAllText("Temp/ari_stand_error.txt", e.ToString());
                Debug.LogError("[Echoes] Stand Ari failed\n" + e);
            }
        }

        static void RunInner()
        {
            var sb = new StringBuilder();

            // Bailed out early once already, and silently: the editor was in play
            // mode, every edit below applied to a runtime copy, and the tool then
            // died on MarkSceneDirty — leaving the work done and none of it kept.
            // Play mode also has to be stopped by hand, so say so plainly.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Echoes] stop play mode first. Placement made in play " +
                               "mode is thrown away when you exit, so it would look " +
                               "like it worked and then vanish.");
                return;
            }

            var ari = GameObject.Find("Ari");
            if (ari == null)
            {
                Debug.LogError("[Echoes] no Ari in the scene. Run Tools/Echoes/Place Ari.");
                return;
            }

            var skin = ari.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin == null)
            {
                Debug.LogError("[Echoes] Ari has no SkinnedMeshRenderer");
                return;
            }

            // ---- 1. the true silhouette ----
            // BakeMesh puts the skinned result in the renderer's local space, so
            // lowestWorldY is the real sole, not a bound.
            var baked = new Mesh();
            float lowestWorldY, highestWorldY;
            MeasureSilhouette(skin, baked, out lowestWorldY, out highestWorldY);
            Object.DestroyImmediate(baked);

            var height = highestWorldY - lowestWorldY;
            sb.AppendLine($"--- measured silhouette (baked skin, not bounds) ---");
            sb.AppendLine($"  sole  y = {lowestWorldY:F4}   (with root at {ari.transform.position.y:F4})");
            sb.AppendLine($"  crown y = {highestWorldY:F4}");
            sb.AppendLine($"  height   = {height:F4}");

            // ---- 2. where she should stand ----
            float surfaceY;
            Vector3 standAt;
            if (TryFindSquare(out surfaceY, out standAt, sb))
            {
                sb.AppendLine($"--- the market square ---");
                sb.AppendLine($"  paving top y = {surfaceY:F4}  (tiles are placed at " +
                              $"{surfaceY:F2}, so that is the walking surface)");
            }
            else
            {
                surfaceY = 0f;
                standAt = ari.transform.position;
                sb.AppendLine("--- no MarketSquare found; standing at the current position ---");
            }

            // Offset the root by the difference between the sole and the surface.
            // Reading it off the baked sole rather than the bounds is what keeps
            // her from floating or sinking.
            float lift = surfaceY - lowestWorldY;
            // Moving her horizontally does not move the sole — her rotation is
            // unchanged — so the lift measured at the old spot carries over and
            // the position is written exactly once, at the end.
            float targetY = ari.transform.position.y + lift;
            var spot = new Vector3(standAt.x, targetY, standAt.z);

            var cam = Camera.main;
            if (cam != null)
            {
                var probe = new Bounds(spot + Vector3.up * (height * 0.5f),
                                       new Vector3(0.8f, height, 0.8f));

                if (!InFrustum(cam, probe))
                {
                    // The camera cannot be moved by shoving her about. It sits at
                    // the near corner of the village at 45 degrees looking across
                    // it, which puts the whole market square behind it — 52 units
                    // back and outside the frustum. Walking her along the camera's
                    // forward axis would fix the frustum test and put her off the
                    // paving in mid-air, so the camera is reframed instead and she
                    // keeps the spot the paving chose for her.
                    var flat = new Vector3(spot.x - cam.transform.position.x, 0f,
                                           spot.z - cam.transform.position.z);
                    if (flat.sqrMagnitude < 0.0001f) flat = new Vector3(0f, 0f, -1f);
                    flat.Normalize();

                    var eye = spot - flat * 7f + Vector3.up * 3.2f;
                    cam.transform.position = eye;
                    LookAt(cam.transform, spot + Vector3.up * (height * 0.5f));

                    sb.AppendLine($"  she was out of frame, so the camera was reframed: " +
                                  $"pos {cam.transform.position} rot {cam.transform.eulerAngles}");
                    sb.AppendLine($"  in frustum after reframing: {InFrustum(cam, probe)}");
                }
            }

            ari.transform.position = spot;
            ari.transform.rotation = Quaternion.Euler(0f, FaceCamera(ari, cam), 0f);

            sb.AppendLine("--- placed ---");
            sb.AppendLine($"  position {ari.transform.position}  (lift applied {lift:F4})");

            // ---- 3. the mover, then the animator ----
            // The mover is added here rather than left as a to-do, because
            // without something writing to Speed the graph sits on the idle
            // forever — which is the exact symptom of "she will not animate"
            // even though the controller is wired up correctly.
            var mover = ari.GetComponent<Echoes.Painterly.AriMover>();
            bool addedMover = false;
            if (mover == null)
            {
                mover = ari.AddComponent<Echoes.Painterly.AriMover>();
                addedMover = true;
            }

            sb.AppendLine("--- mover ---");
            sb.AppendLine(addedMover
                ? "  added AriMover (WASD or arrows to walk, LeftShift to run)"
                : "  AriMover already present");

            var anim = ari.GetComponent<Animator>() ?? skin.GetComponent<Animator>();
            if (anim == null) anim = ari.AddComponent<Animator>();

            anim.enabled = true;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.Rebind();
            anim.Update(0f);

            sb.AppendLine($"--- animator ---");
            sb.AppendLine($"  enabled={anim.enabled} active={anim.isActiveAndEnabled} " +
                          $"controller={(anim.runtimeAnimatorController == null ? "<NONE>" : anim.runtimeAnimatorController.name)} " +
                          $"avatar={(anim.avatar == null ? "<NONE>" : anim.avatar.name)}");
            sb.AppendLine($"  state after a forced evaluate: " +
                          $"{DescribeState(anim)}  (Speed=0 should be the idle)");

            // ---- 4. can the camera see her now ----
            if (cam != null)
            {
                var b = skin.bounds;
                float d = Vector3.Distance(cam.transform.position, b.center);
                sb.AppendLine("--- visibility ---");
                sb.AppendLine($"  camera {cam.transform.position} -> Ari {b.center}, {d:F2} units");
                sb.AppendLine($"  in frustum: {InFrustum(cam, b)}");
                sb.AppendLine($"  layer {ari.layer} in cullingMask: " +
                              $"{(cam.cullingMask & (1 << ari.layer)) != 0}");
                if (!InFrustum(cam, b))
                {
                    // Last resort: aim the camera. Position is left alone, only
                    // the rotation changes, so the village framing is kept.
                    LookAt(cam.transform, b.center);
                    sb.AppendLine($"  aimed the camera at her: rot now {cam.transform.eulerAngles}");
                    sb.AppendLine($"  in frustum after aiming: {InFrustum(cam, b)}");
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            var text = sb.ToString();
            File.WriteAllText("Temp/ari_stand.txt", text);
            Debug.Log("[Echoes] Ari stood on the square\n" + text);
        }

        /// <summary>
        /// Bake the skin and read the lowest and highest real vertex.
        /// </summary>
        static void MeasureSilhouette(SkinnedMeshRenderer skin, Mesh baked,
                                      out float lowestWorldY, out float highestWorldY)
        {
            skin.BakeMesh(baked);

            var verts = baked.vertices;
            var m = skin.transform.localToWorldMatrix;

            lowestWorldY = float.MaxValue;
            highestWorldY = float.MinValue;

            for (int i = 0; i < verts.Length; i++)
            {
                float y = m.MultiplyPoint3x4(verts[i]).y;
                if (y < lowestWorldY) lowestWorldY = y;
                if (y > highestWorldY) highestWorldY = y;
            }

            if (verts.Length == 0)
            {
                lowestWorldY = skin.transform.position.y;
                highestWorldY = skin.transform.position.y;
            }
        }

        /// <summary>
        /// The square's paving surface and a spot to stand on.
        ///
        /// Only the Floor_* tiles count. The square also holds the landmark
        /// tower — a deliberate 3x3x3 BuildHouse at its centre — and taking the
        /// bounds of everything underneath put the "ground" at y=15.03, the top
        /// of the tower's roof, which parked Ari on the tiles. Filtering to the
        /// paving is what makes this the walking surface and not the highest
        /// thing standing on it.
        /// </summary>
        static bool TryFindSquare(out float surfaceY, out Vector3 standAt, StringBuilder sb)
        {
            surfaceY = 0f;
            standAt = Vector3.zero;

            var square = GameObject.Find("MarketSquare");
            if (square == null)
            {
                var village = GameObject.Find("Village_Grey");
                if (village != null)
                {
                    var t = village.transform.Find("MarketSquare");
                    if (t != null) square = t.gameObject;
                }
            }

            if (square == null) return false;

            var paving = square.GetComponentsInChildren<Renderer>(true)
                                 .Where(r => r.name.StartsWith("Floor_"))
                                 .ToArray();

            if (paving.Length == 0)
            {
                sb.AppendLine("  no Floor_* paving found under MarketSquare");
                return false;
            }

            var bounds = paving[0].bounds;
            foreach (var r in paving) bounds.Encapsulate(r.bounds);

            surfaceY = bounds.max.y;

            // The landmark tower stands at the square's centre, so the middle of
            // the paving is not a place a person can be. The spot is chosen from
            // the tile centres that fall outside the tower's footprint, and the
            // one nearest the camera is taken so she is not hidden behind it.
            var tower = square.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.transform.parent != null &&
                            r.transform.parent.name.StartsWith("House_"))
                .ToArray();

            var footprint = tower.Length > 0 ? tower[0].bounds : new Bounds();
            foreach (var r in tower) footprint.Encapsulate(r.bounds);

            var candidates = paving
                .Select(r => r.bounds.center)
                .Where(c => tower.Length == 0 ||
                            Mathf.Abs(c.x - footprint.center.x) > footprint.extents.x + 0.2f ||
                            Mathf.Abs(c.z - footprint.center.z) > footprint.extents.z + 0.2f)
                .ToArray();

            sb.AppendLine($"  {paving.Length} paving tile(s), top y = {surfaceY:F4}, " +
                          $"spanning {bounds.size.x:F2} x {bounds.size.z:F2}");
            sb.AppendLine($"  landmark tower footprint {footprint.size.x:F2} x " +
                          $"{footprint.size.z:F2} at {footprint.center}");

            if (candidates.Length == 0)
            {
                sb.AppendLine("  every tile is inside the tower's footprint; " +
                              "falling back to the square's edge");
                standAt = new Vector3(bounds.center.x, surfaceY, bounds.max.z - 0.5f);
                return true;
            }

            var cam = Camera.main;
            standAt = cam == null
                ? candidates[0]
                : candidates.OrderBy(c => Vector3.Distance(c, cam.transform.position)).First();

            sb.AppendLine($"  {candidates.Length} clear tile(s); standing at {standAt}");

            return true;
        }

        static bool InFrustum(Camera cam, Bounds b)
        {
            return GeometryUtility.TestPlanesAABB(
                GeometryUtility.CalculateFrustumPlanes(cam), b);
        }

        static float FaceCamera(GameObject ari, Camera cam)
        {
            if (cam == null) return 0f;
            var to = cam.transform.position - ari.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return 0f;
            return Quaternion.LookRotation(to, Vector3.up).eulerAngles.y;
        }

        static void LookAt(Transform t, Vector3 target)
        {
            var dir = target - t.position;
            if (dir.sqrMagnitude < 0.0001f) return;
            t.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        static string DescribeState(Animator anim)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);

            // The state hash is a hash, so it is matched back to a clip by
            // hashing each name — reading shortNameHash as a string never matches.
            var byName = anim.runtimeAnimatorController.animationClips
                .Select(c => new { c.name, c.length })
                .FirstOrDefault(c => Animator.StringToHash(c.name) == info.shortNameHash);

            return $"hash={info.shortNameHash} normalizedTime={info.normalizedTime:F2} " +
                   $"clip={(byName == null ? "?" : byName.name)} " +
                   $"clipLen={(byName == null ? 0f : byName.length):F2}s";
        }
    }
}
