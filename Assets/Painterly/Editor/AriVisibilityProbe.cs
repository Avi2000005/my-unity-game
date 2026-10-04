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
    /// Answers "why can I not see her / why does she not animate" by measuring,
    /// not by looking. Writes Temp/ari_visible.txt.
    ///
    /// A character can be present, enabled, correctly wired and still be
    /// invisible for reasons that have nothing to do with each other: a camera
    /// pointed elsewhere, a renderer on a layer the camera does not render, a
    /// shader that failed to compile and fell back, or a bounds cull because
    /// she is behind something. Each of those is a separate line here so a
    /// single check does not hide the others.
    /// </summary>
    public static class AriVisibilityProbe
    {
        [MenuItem("Tools/Echoes/Probe Ari Visible", priority = 96)]
        public static void Run()
        {
            var sb = new StringBuilder();
            var scene = SceneManager.GetActiveScene();
            sb.AppendLine($"scene '{scene.name}' path={scene.path} loaded={scene.isLoaded} " +
                          $"dirty={scene.isDirty}");

            // ---- the character ----
            var ari = GameObject.Find("Ari");
            if (ari == null)
            {
                sb.AppendLine("\nNO GameObject named 'Ari' in the open scene.");
                sb.AppendLine("Roots in this scene:");
                foreach (var r in scene.GetRootGameObjects())
                    sb.AppendLine($"  {r.name} ({(r.activeSelf ? "active" : "INACTIVE")})");
                Finish(sb);
                return;
            }

            var rt = ari.transform;
            sb.AppendLine($"\nAri: activeInHierarchy={ari.activeInHierarchy} layer={ari.layer} " +
                          $"tag={ari.tag}");
            sb.AppendLine($"  world pos {rt.position}  localScale {rt.localScale}  " +
                          $"children={ari.transform.childCount}");

            var renderers = ari.GetComponentsInChildren<Renderer>(true);
            sb.AppendLine($"  {renderers.Length} renderer(s)");

            foreach (var r in renderers)
            {
                sb.AppendLine($"    {r.GetType().Name} '{r.name}' enabled={r.enabled} " +
                              $"activeInHierarchy={r.gameObject.activeInHierarchy} layer={r.gameObject.layer}");
                sb.AppendLine($"      activeInHierarchy={r.gameObject.activeInHierarchy} " +
                              $"isVisible={r.isVisible}");

                if (r is SkinnedMeshRenderer sk)
                {
                    sb.AppendLine($"      skin={sk.rootBone?.name} quality={sk.quality} " +
                                  $"updateWhenOffscreen={sk.updateWhenOffscreen} " +
                                  $"bounds(local) centre={sk.localBounds.center} size={sk.localBounds.size}");
                    sb.AppendLine($"      mesh={(sk.sharedMesh == null ? "<none>" : sk.sharedMesh.name + " " + sk.sharedMesh.vertexCount + "v")}");
                }
                else if (r is MeshRenderer)
                {
                    sb.AppendLine($"      bounds(world) centre={r.bounds.center} size={r.bounds.size}");
                }

                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) { sb.AppendLine("      material: <NULL>"); continue; }
                    var sh = m.shader;
                    sb.AppendLine($"      material '{m.name}' shader='{sh?.name}' " +
                                  $"supported={sh != null && sh.isSupported} " +
                                  $"renderQueue={m.renderQueue} passCount={sh?.passCount}");
                    if (sh != null && !sh.isSupported)
                        sb.AppendLine("      ^ SHADER NOT SUPPORTED: this is what makes her invisible");
                    var baseMap = m.GetTexture("_BaseMap");
                    sb.AppendLine($"      _BaseMap={(baseMap == null ? "<none>" : baseMap.name)} " +
                                  $"renderQueue={m.renderQueue} " +
                                  $"keywords={string.Join(",", m.shaderKeywords)}");
                }
            }

            // ---- bounds in world space, the thing a renderer is culled by ----
            var worldBounds = new Bounds(rt.position, Vector3.zero);
            bool any = false;
            foreach (var r in renderers)
            {
                if (!any) { worldBounds = r.bounds; any = true; }
                else worldBounds.Encapsulate(r.bounds);
            }
            if (any)
                sb.AppendLine($"\nAri world bounds: centre={worldBounds.center} " +
                              $"size={worldBounds.size} minY={worldBounds.min.y:F3} " +
                              $"maxY={worldBounds.max.y:F3}");

            // ---- the camera ----
            var cam = Camera.main;
            if (cam == null)
            {
                sb.AppendLine("\nNO Camera.main in the scene.");
                Finish(sb);
                return;
            }

            var ct = cam.transform;
            sb.AppendLine($"\nCamera.main '{cam.name}' pos={ct.position} " +
                          $"rot={ct.eulerAngles} fov={cam.fieldOfView} " +
                          $"near={cam.nearClipPlane} far={cam.farClipPlane} " +
                          $"clearFlags={cam.clearFlags} cullingMask={cam.cullingMask} " +
                          $"enabled={cam.enabled}");
            sb.AppendLine($"  forward={ct.forward}");

            if (any)
            {
                sb.AppendLine($"  Ari layer {ari.layer} visible to this camera: " +
                              $"{(cam.cullingMask & (1 << ari.layer)) != 0}");
                sb.AppendLine($"  Ari bounds centre in front of camera: " +
                              $"{Vector3.Dot(worldBounds.center - ct.position, ct.forward) >= 0}");
                sb.AppendLine($"  distance camera->Ari = " +
                              $"{Vector3.Distance(ct.position, worldBounds.center):F3} " +
                              $"(near={cam.nearClipPlane}, far={cam.farClipPlane})");
                sb.AppendLine($"  in camera frustum: {TestInFrustum(cam, worldBounds)}");
            }

            // ---- anything sitting inside her ----
            if (any)
            {
                // OverlapBoxNonAlloc fills a Collider buffer, not a RaycastHit
                // one, and takes it as the third argument.
                var blockers = new Collider[16];
                int n = Physics.OverlapBoxNonAlloc(
                    worldBounds.center,
                    worldBounds.extents,
                    blockers,
                    ct.rotation,
                    ~0,
                    QueryTriggerInteraction.Ignore);

                var names = blockers.Where(c => c != null)
                    .Select(c => $"{c.name}({c.GetType().Name})")
                    .Distinct().ToArray();

                sb.AppendLine($"\n  colliders overlapping Ari's bounds: {n}");
                foreach (var s in names) sb.AppendLine("    " + s);
                if (n == 0) sb.AppendLine("    none — nothing is in her space");
            }

            // ---- the animator ----
            var anim = ari.GetComponent<Animator>();
            if (anim == null) anim = ari.GetComponentInChildren<Animator>();
            if (anim == null)
            {
                sb.AppendLine("\nNO Animator on Ari or her children.");
            }
            else
            {
                sb.AppendLine($"\nAnimator on '{anim.name}':");
                sb.AppendLine($"  enabled={anim.enabled} " +
                              $"activeInHierarchy={anim.isActiveAndEnabled} " +
                              $"controller={(anim.runtimeAnimatorController == null ? "<NONE>" : anim.runtimeAnimatorController.name)} " +
                              $"avatar={(anim.avatar == null ? "<NONE>" : anim.avatar.name)} " +
                              $"applyRootMotion={anim.applyRootMotion} " +
                              $"culling={anim.cullingMode} speed={anim.speed} " +
                              $"updateMode={anim.updateMode}");
                if (anim.avatar != null)
                    sb.AppendLine($"  avatar valid={anim.avatar.isValid} human={anim.avatar.isHuman}");
                if (anim.runtimeAnimatorController != null)
                {
                    // The parameters live on the Animator, not the controller
                    // asset; the base class does not expose them.
                    foreach (var p in anim.parameters)
                        sb.AppendLine($"  param {p.name} {p.type} default={p.defaultFloat}");
                }
            }

            Finish(sb);
        }

        static bool TestInFrustum(Camera cam, Bounds bounds)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }

        static void Finish(StringBuilder sb)
        {
            var text = sb.ToString();
            File.WriteAllText("Temp/ari_visible.txt", text);
            Debug.Log("[Echoes] Ari visibility probe\n" + text);
        }
    }
}
