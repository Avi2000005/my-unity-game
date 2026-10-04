using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Imports the Meshy paint-splattered wand and builds its material.
    /// Writes Temp/wand.txt.
    ///
    /// This is a static prop, not a character: the FBX carries no skeleton
    /// (zero occurrences of Hips, Spine, Armature, Skin or Deformer), so it is
    /// configured with no rig rather than being pointed at Ari's avatar. Treating
    /// it as a Humanoid would make the importer try to map bones that are not
    /// there.
    ///
    /// The four textures land where PainterlyLit has somewhere to put them:
    /// albedo to _BaseMap, the normal to _BumpMap. Metallic and roughness are
    /// not packed into one map here — Unity's _MetallicGlossMap expects metallic
    /// in red and smoothness in alpha, and these arrive as two separate images
    /// that would each need a channel operation first. Until that is done the
    /// material uses scalar values, which is honest: a wand made of painted wood
    /// is not meaningfully metallic.
    ///
    /// _ColorRestore starts at 1. Ari is the painter; her own tool should not be
    /// grey.
    /// </summary>
    public static class WandSetup
    {
        const string Dir = "Assets/Art/Props/Wand";

        // Named ShaderName, not Shader: a const called Shader shadows the
        // UnityEngine.Shader type, and Shader.Find(Shader) then resolves against
        // the string with "'string' does not contain a definition for 'Find'".
        const string ShaderName = "Echoes/PainterlyLit";
        const string Material = "Wand";

        [MenuItem("Tools/Echoes/Import Wand", priority = 64)]
        public static void Run()
        {
            var sb = new StringBuilder();
            try { RunInner(sb); }
            catch (System.Exception e)
            {
                sb.AppendLine("FAILED: " + e);
            }
            finally
            {
                File.WriteAllText("Temp/wand.txt", sb.ToString());
                Debug.Log("[Echoes] Wand import\n" + sb);
            }
        }

        static void RunInner(StringBuilder sb)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sb.AppendLine("Stop play mode first — an import made during play " +
                              "is discarded when play mode exits.");
                return;
            }

            // The files are copied in by hand, so without a forced refresh the
            // AssetDatabase has never seen them and every lookup below returns
            // null while reporting success.
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate |
                                  ImportAssetOptions.ForceSynchronousImport);

            string fbx = Dir + "/Wand.fbx";
            var mi = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (mi == null)
            {
                sb.AppendLine($"missing: {fbx} — the file was not copied in.");
                return;
            }

            // A prop, not a rig. None rather than Human or Generic: there are no
            // bones to import and generating an avatar for them is wasted work.
            mi.animationType = ModelImporterAnimationType.None;
            mi.importAnimation = false;
            mi.addCollider = false;
            // Not readable: the mesh only ever renders, and keeping the CPU copy
            // of a 14 MB mesh alive is memory spent on nothing. Bounds are still
            // readable without it.
            mi.isReadable = false;
            // Materials are ours to author so the shader is PainterlyLit and the
            // colour-restoration property exists on it.
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.SaveAndReimport();

            ReportMesh(sb, fbx);

            Texture2D albedo = Texture(Dir + "/Wand.png", false, sb);
            Texture2D normal = Texture(Dir + "/Wand_normal.png", true, sb);
            Texture2D roughness = Texture(Dir + "/Wand_roughness.png", false, sb);
            Texture2D metallic = Texture(Dir + "/Wand_metallic.png", false, sb);

            sb.AppendLine($"roughness present={roughness != null} " +
                          $"metallic present={metallic != null} " +
                          $"(not packed into _MetallicGlossMap yet)");

            BuildMaterial(albedo, normal, metallic, roughness, sb);
        }

        static Texture2D Texture(string path, bool asNormal, StringBuilder sb)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (t == null) { sb.AppendLine($"texture missing: {path}"); return null; }

            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                var want = asNormal
                    ? TextureImporterType.NormalMap
                    : TextureImporterType.Default;

                // Only reimport when something actually differs, or the tool
                // churns the importer's .meta and re-imports on every run.
                if (ti.textureType != want)
                {
                    ti.textureType = want;
                    ti.sRGBTexture = !asNormal;
                    ti.SaveAndReimport();
                    sb.AppendLine($"  {Path.GetFileName(path)}: type set to {want}");
                }
            }

            sb.AppendLine($"  {Path.GetFileName(path)}: {t.width}x{t.height}");
            return t;
        }

        static void BuildMaterial(Texture2D albedo, Texture2D normal,
                                  Texture2D metallic, Texture2D roughness,
                                  StringBuilder sb)
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                sb.AppendLine($"shader '{ShaderName}' not found — is it imported?");
                return;
            }

            string path = $"{Dir}/{Material}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = mat == null;

            if (created)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                // Reassign the shader rather than keeping whatever is there:
                // if PainterlyLit is ever renamed, a material that still points
                // at the old one falls back to magenta with no log entry.
                mat.shader = shader;
            }

            if (albedo != null) mat.SetTexture("_BaseMap", albedo);
            if (normal != null) mat.SetTexture("_BumpMap", normal);

            // Scalar stand-ins for the maps that are not packed yet.
            mat.SetFloat("_Metallic", metallic != null ? 0.05f : 0f);
            mat.SetFloat("_Smoothness", 0.35f);

            // Ari's own tool starts in full colour.
            mat.SetFloat("_ColorRestore", 1f);
            mat.SetFloat("_RestoreBoost", 1f);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            sb.AppendLine($"material {Material}: created={created} " +
                          $"albedo={albedo != null} normal={normal != null} " +
                          $"shader={mat.shader.name}");
        }

        /// <summary>
        /// Measures the mesh rather than assuming its scale. Meshy exports come
        /// out of Blender at whatever unit the scene happened to use, so the
        /// wand is anywhere from 0.03 to 30 units until it has been measured.
        /// </summary>
        static void ReportMesh(StringBuilder sb, string fbx)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(fbx);
            if (mesh == null)
            {
                // A model with multiple sub-objects has no single mesh; look for
                // the first one instead of reporting a false "missing".
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbx))
                {
                    if (o is Mesh m) { mesh = m; break; }
                }
            }

            if (mesh == null) { sb.AppendLine("no mesh found in the FBX"); return; }

            sb.AppendLine($"mesh '{mesh.name}' verts={mesh.vertexCount} " +
                          $"subMeshes={mesh.subMeshCount} " +
                          $"bounds={mesh.bounds.min} .. {mesh.bounds.max} " +
                          $"size={mesh.bounds.size}");
        }
    }
}
