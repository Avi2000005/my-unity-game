using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Builds one PainterlyLit material per texture set in the Quaternius village
    /// textures, and a few untextured greys for anything the kit doesn't cover.
    ///
    /// Re-runnable: it updates existing materials in place rather than duplicating.
    ///
    /// Note on roughness: the kit ships Roughness and ORM maps, but URP/Lit expects
    /// metallic in R and smoothness in A of a single map, so neither maps across
    /// cleanly. Rather than wire them subtly wrong, this uses a flat smoothness per
    /// material — which suits a matte storybook village anyway. Revisit with a
    /// custom UnpackORM() in the shader if you later want the roughness variation.
    /// </summary>
    public static class PainterlyMaterialSetup
    {
        const string ShaderName = "Echoes/PainterlyLit";
        const string TextureDir = "Assets/Art/Village/Textures";
        const string OutputDir = "Assets/Painterly/Materials";

        struct Spec
        {
            public string name;
            public string baseColor;   // texture filename, or null for untextured
            public string normal;
            public float smoothness;
            public float metallic;
            public bool alphaClip;
            public string note;

            public Spec(string n, string bc, string nm, float s, float m = 0f, bool clip = false, string note = null)
            {
                name = n; baseColor = bc; normal = nm;
                smoothness = s; metallic = m; alphaClip = clip; this.note = note;
            }
        }

        static readonly Spec[] Specs =
        {
            new Spec("Brick",        "T_Brick_BaseColor",        "T_Brick_Normal",        0.15f),
            new Spec("RedBrick",     "T_RedBrick_BaseColor",     null,                    0.15f),
            new Spec("UnevenBrick",  "T_UnevenBrick_BaseColor",  "T_UnevenBrick_Normal",  0.15f),
            new Spec("Plaster",      "T_Plaster_BaseColor",      "T_Plaster_Normal",      0.10f),
            new Spec("RockTrim",     "T_RockTrim_BaseColor",     "T_RockTrim_Normal",     0.12f),
            new Spec("RoundTiles",   "T_RoundTiles_BaseColor",   "T_RoundTiles_Normal",   0.25f),
            new Spec("WoodTrim",     "T_WoodTrim_BaseColor",     "T_WoodTrim_Normal",     0.18f),
            new Spec("MetalOrnament","T_MetalOrnaments_BaseColor", null,                  0.45f, 0.9f),
            new Spec("VineLeaf",     "T_VineLeaf",               null,                    0.10f, 0f, true,
                     "alpha clipped"),
            new Spec("WindowPane",   "T_WindowGradient",         null,                    0.60f, 0f, false,
                     "no normal map in kit"),

            // Untextured greys — anything the texture set doesn't cover.
            new Spec("Grey_Plaster", null, null, 0.08f, 0f, false, "untextured"),
            new Spec("Grey_Wood",    null, null, 0.15f, 0f, false, "untextured"),
            new Spec("Grey_Stone",   null, null, 0.12f, 0f, false, "untextured"),
            new Spec("Grey_Ground",  null, null, 0.05f, 0f, false, "untextured"),
        };

        [MenuItem("Tools/Echoes/Build Village Materials")]
        public static void Build()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                // Logged, not dialogged: this tool also runs headless via the
                // Pipeline/MCP bridge, where a modal dialog would block the Editor.
                Debug.LogError($"[Echoes] Could not find shader '{ShaderName}'. " +
                               "Has Unity finished importing it?");
                return;
            }

            if (!Directory.Exists(OutputDir)) Directory.CreateDirectory(OutputDir);

            var made = new List<string>();
            var missing = new List<string>();

            foreach (var s in Specs)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{OutputDir}/{s.name}.mat");
                bool isNew = mat == null;

                if (isNew) mat = new Material(shader);

                mat.shader = shader;

                // Base colour
                var bc = LoadTexture(s.baseColor, missing);
                if (bc != null)
                {
                    mat.SetTexture("_BaseMap", bc);
                    mat.EnableKeyword("_NORMALMAP");
                }
                else
                {
                    mat.SetTexture("_BaseMap", null);
                    mat.DisableKeyword("_NORMALMAP");
                }

                // Normal map — kit normals are authored as "bump" type
                var nm = LoadTexture(s.normal, missing);
                if (nm != null)
                {
                    mat.SetTexture("_BumpMap", nm);
                    mat.SetFloat("_BumpScale", 1f);
                    if (bc != null) mat.EnableKeyword("_NORMALMAP");
                }
                else
                {
                    mat.SetTexture("_BumpMap", null);
                    if (bc == null) mat.DisableKeyword("_NORMALMAP");
                }

                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Smoothness", s.smoothness);
                mat.SetFloat("_Metallic", s.metallic);
                mat.SetFloat("_BumpScale", 1f);
                mat.enableInstancing = true;

                // The whole point: every village material starts colourless.
                mat.SetFloat("_ColorRestore", 0f);
                mat.SetFloat("_RestoreBoost", 1f);

                // Alpha clipping for the vines
                if (s.alphaClip)
                {
                    mat.SetFloat("_Cutoff", 0.4f);
                    mat.SetFloat("_Surface", 0f);
                    mat.SetFloat("_Blend", 0f);
                    mat.SetFloat("_ZWrite", 1f);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    mat.SetOverrideTag("RenderType", "TransparentCutout");
                }
                else
                {
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.renderQueue = -1;
                    mat.SetOverrideTag("RenderType", "Opaque");
                }

                string path = $"{OutputDir}/{s.name}.mat";
                if (isNew) AssetDatabase.CreateAsset(mat, path);
                else EditorUtility.SetDirty(mat);

                made.Add(s.name + (s.note != null ? $"  ({s.note})" : ""));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Created/updated {made.Count} materials in {OutputDir}\n");
            foreach (var m in made) sb.AppendLine("  • " + m);

            if (missing.Count > 0)
            {
                sb.AppendLine($"\n{System.Environment.NewLine}Textures not found (material left untextured):");
                foreach (var m in System.Linq.Enumerable.Distinct(missing))
                    sb.AppendLine("  - " + m);
            }

            Debug.Log($"[Echoes] Village materials\n\n{sb}");
        }

        static Texture2D LoadTexture(string fileName, List<string> missing)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            string path = $"{TextureDir}/{fileName}.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (tex == null)
            {
                // Fall back to a jpg if the pack shipped one.
                path = $"{TextureDir}/{fileName}.jpg";
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            if (tex == null) missing.Add(fileName);

            return tex;
        }
    }
}
