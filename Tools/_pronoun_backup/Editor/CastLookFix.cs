using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Echoes.Painterly;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Experimental.Rendering;

/// <summary>
/// Puts the three of the cast on the village's own shader WITH the skins that
/// were embedded in their FBX files.
///
/// Measured first, applied second, re-measured third. Nothing here is guessed:
/// the brightness targets come from measuring the village's own albedo
/// textures, and the tints come from measuring the cast's own textures for
/// clipping. See the block comment on Tint for the arithmetic.
/// </summary>
public static class CastLookFix
{
    const string ReportPath = "Temp/cast_fix.txt";
    const string MaterialFolder = "Assets/Painterly/Materials";
    const string TextureFolder  = "Assets/Art/Cast/Textures";

    // ---------------------------------------------------------------------
    // HOW THE THREE ARE TOLD APART
    //
    // By VALUE, never by hue. This is a colourless village; there is no colour
    // to identify anyone by until Ari paints it, and identifying them by
    // colour would be a colour-based mechanic besides. So the three are three
    // steps on one grey ramp, and the eye reads them the way it reads roofs,
    // walls and ground.
    //
    //   Mono        lightest — the thing the player is meant to find
    //   InkCrawler  middle   — ink, a threat, a shadow in the street
    //   ColorThief  darkest  — a silhouette on a hill 55 m out
    //
    // WHERE THE TINTS COME FROM
    //
    // The cast's own textures, measured by sampling them:
    //     Mono_basecolor.png        mean 19.1% of white
    //     InkCrawler_basecolor.png  mean 17.7%
    //     ColorThief_basecolor.png  mean 15.1%
    // Those three are 19.1 / 17.7 / 15.1 — which is NOT a distinction. Two
    // percent of value is invisible, so untinted they would be three of the
    // same shape in the same shade. The tint is what separates them.
    //
    // Clipping was measured, not assumed. A flat tint multiplies the peaks as
    // well as the mean, and Mono's texture has a bright spike in it:
    //     Mono  tint 1.5 -> mean 28.3%,  2.20% of pixels clip to white
    //     Mono  tint 2.0 -> mean 36.7%,  4.04% clip
    //     Mono  tint 3.5 -> mean 60.2%, 11.15% clip
    // So Mono cannot be lifted to "brightest thing in the level" without
    // blowing out. He is not lifted. 1.5 is the largest tint whose clipping
    // stays mild, and on a painterly texture a 2% highlight rolloff is
    // attractive rather than broken.
    //
    // WHAT THE RESULT SITS AGAINST, measured from the village's own albedo:
    //     T_UnevenBrick_BaseColor  47.9%   (the ground Ari walks on)
    //     T_Plaster_BaseColor      62.4%   (the house walls)
    //     Ari_basecolor            30.5%   (the protagonist)
    //     ab_colour                20.5%   (the darkest thing already there)
    // So all three end up darker than the ground and walls, which is the
    // correct staging — dark characters read cleanly against a light
    // environment, and a character that matches the wall behind it reads as
    // nothing at all.
    // ---------------------------------------------------------------------

    struct Style
    {
        public string who;
        public string baseColor;     // file name inside TextureFolder
        public string normal;        // null when the FBX shipped no normal map
        public float  tint;          // multiplies albedo, value only, no hue
        public float  bumpScale;
        public bool   paintable;     // a ColorRestoreTarget, so the world can grey it
        public string expected;      // predicted final greyscale, for the re-measure
        public string why;
    }

    static readonly Style[] Cast =
    {
        new Style
        {
            who = "Mono",
            baseColor = "Mono_basecolor.png",
            normal = null,
            tint = 1.50f,
            bumpScale = 0f,
            paintable = true,
            expected = "28.3% of white",
            why = "lightest of the three, so the eye finds him first. Paintable " +
                  "because the wake burst is Ari's own brushstroke and it should " +
                  "be the thing that brings his colour back."
        },
        new Style
        {
            who = "InkCrawler",
            baseColor = "InkCrawler_basecolor.png",
            normal = "InkCrawler_normal.png",
            tint = 0.80f,
            bumpScale = 0.60f,
            paintable = true,
            expected = "14.6% of white",
            why = "ink, dark enough to read as a gap in the street but not so " +
                  "dark it becomes a hole. Paintable so it greys with everything " +
                  "else and never sits in the scene as a full-colour object."
        },
        new Style
        {
            who = "ColorThief",
            baseColor = "ColorThief_basecolor.png",
            normal = null,
            tint = 0.45f,
            bumpScale = 0f,
            paintable = false,
            expected = "6.8% of white",
            why = "darkest, and NOT paintable. Beat 7 is her reveal and nothing " +
                  "in Level 1 is allowed to put colour on her — if she could be " +
                  "painted she would stop being a silhouette."
        },
    };

    public static void Run()
    {
        var sb = new StringBuilder();

        try { Body(sb); }
        catch (Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] cast fix\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportPath),
                           sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            sb.AppendLine("STOPPED: the editor is in play mode.");
            sb.AppendLine("Nothing is changed. A material written during play is " +
                          "reverted when play stops, and the scene save would " +
                          "capture the play-time state. Press Ctrl+P, then re-run.");
            return;
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.isLoaded) { sb.AppendLine("No scene is open."); return; }

        var painterly = Shader.Find("Echoes/PainterlyLit");
        if (painterly == null)
        {
            sb.AppendLine("Echoes/PainterlyLit does not resolve. Without it the " +
                          "cast cannot be greyscaled or painted, and nothing can " +
                          "be assigned to them. Not changing anything.");
            return;
        }

        sb.AppendLine("GIVING THE CAST THEIR OWN SKINS");
        sb.AppendLine();

        // --- the textures must exist and be imported ------------------------

        var tex = new Dictionary<string, Texture2D>();
        int missing = 0;

        foreach (var style in Cast)
        {
            if (!Import(style.baseColor, false, sb, tex)) missing++;
            if (style.normal != null && !Import(style.normal, true, sb, tex)) missing++;
        }

        if (missing > 0)
        {
            sb.AppendLine();
            sb.AppendLine("STOPPED: " + missing + " texture(s) did not import. " +
                          "Assigning a material a texture that is not there is " +
                          "how a character ends up invisible. Not changing anything.");
            return;
        }

        sb.AppendLine();

        // --- the template ----------------------------------------------------

        var template = FindVillageMaterial(sb);
        if (template == null)
        {
            sb.AppendLine("No existing material on Echoes/PainterlyLit to copy the " +
                          "property set from. A template invented from nothing is " +
                          "how a material ends up with a missing roughness value " +
                          "and a mirror finish. Not changing anything.");
            return;
        }

        sb.AppendLine("template: '" + template.name + "' (" + template.shader.name + ")");
        sb.AppendLine();

        // --- apply -----------------------------------------------------------

        // Counts problems found BOTH while applying and while re-measuring, so
        // that a unit which could not be found at all is still a failure. A
        // missing character is not a warning to skim past; it is a character in
        // the level wearing the kit's default.
        int wrong = 0;

        foreach (var style in Cast)
        {
            var units = FindUnits(style.who);

            if (units.Count == 0)
            {
                sb.AppendLine(style.who + ": NOT FOUND ANYWHERE, and nothing was assigned " +
                              "to it. This is not a warning to skim past; it means a " +
                              "character is in the level wearing the kit's default.");
                wrong += 1;
                continue;
            }

            var mat = Material(sb, style, template, tex);

            foreach (var unit in units)
            {
                var rends = unit.GetComponentsInChildren<Renderer>(true);
                int assigned = 0;

                foreach (var r in rends)
                {
                    if (mat == null) break;
                    if (r is ParticleSystemRenderer) continue;
                    r.sharedMaterial = mat;
                    assigned++;
                }

                // A ColorRestoreTarget, on the ones allowed to take colour.
                var existing = unit.GetComponent<ColorRestoreTarget>();
                string target = "none";

                if (style.paintable)
                {
                    if (existing == null) existing = unit.AddComponent<ColorRestoreTarget>();
                    target = existing == null ? "COULD NOT ADD" : "added or reused";
                }
                else if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing);
                    target = "removed, nothing in Level 1 may paint her";
                }

                sb.AppendLine(style.who + "  '" + unit.name + "'  at " +
                              unit.transform.position.ToString("F2") +
                              ", " + rends.Length + " renderer(s), " + assigned + " reassigned");
                sb.AppendLine("    material    " + MaterialFolder + "/Cast_" + style.who + ".mat");
                sb.AppendLine("    base map    " + style.baseColor);
                sb.AppendLine("    normal map  " + (style.normal ?? "none, the FBX shipped none"));
                sb.AppendLine("    tint        " + style.tint.ToString("F2") +
                              "  ->  expected final greyscale " + style.expected);
                sb.AppendLine("    paintable   " + target);
                sb.AppendLine("    because     " + style.why);
                sb.AppendLine();
            }

            sb.AppendLine("    " + style.who + ": " + units.Count +
                          " unit(s) found and " + (units.Count > 1 ? "all" : "the one") + " assigned");
            sb.AppendLine();
        }

        // --- is Ari in the same boat? ---------------------------------------

        sb.AppendLine("--- and Ari, for comparison, not changed ---");
        var ari = GameObject.Find("Ari");
        if (ari == null) sb.AppendLine("no 'Ari' GameObject");
        else
            foreach (var r in ari.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) { sb.AppendLine("  " + r.name + ": null material"); continue; }

                    var t = m.mainTexture;
                    sb.AppendLine("  " + r.name + " -> " + m.name + "  '" +
                                  (m.shader != null ? m.shader.name : "none") + "'  " +
                                  (t == null ? "NO TEXTURE" : t.name + " " + t.width + "x" + t.height));
                }

        // --- re-measure, do not trust the assignment ------------------------

        sb.AppendLine();
        sb.AppendLine("--- MEASURED AFTER ---");
        sb.AppendLine("Each check below is an IDENTITY check, not a plausibility check.");
        sb.AppendLine("The previous version of this tool reported PASS while Mono and the");
        sb.AppendLine("Color Thief were still wearing UnevenBrick, because brick happens to");
        sb.AppendLine("be on the village shader, has a base map, sits at _ColorRestore 0 and");
        sb.AppendLine("has no hue. Satisfying every loose check is not the same as being");
        sb.AppendLine("dressed, so this now insists on the cast's own material path and the");
        sb.AppendLine("cast's own texture by name.");
        var finals = new List<string>();

        foreach (var style in Cast)
        {
            string wantMat = MaterialFolder + "/Cast_" + style.who + ".mat";
            var units = FindUnits(style.who);

            if (units.Count == 0) { wrong++; continue; }

            foreach (var unit in units)
            {
                var rends = unit.GetComponentsInChildren<Renderer>(true);
                if (rends.Length == 0)
                {
                    wrong++;
                    sb.AppendLine("  " + unit.name + ": NO RENDERER AT ALL");
                    continue;
                }

                foreach (var r in rends)
                {
                    if (r is ParticleSystemRenderer) continue;
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null)
                        {
                            wrong++;
                            sb.AppendLine("  " + unit.name + ": NULL MATERIAL");
                            continue;
                        }

                        // --- identity: is this the cast's material? -----------
                        string gotPath = AssetDatabase.GetAssetPath(m);
                        bool matOk = gotPath == wantMat;
                        if (!matOk) wrong++;

                        // --- identity: is this the cast's texture? -----------
                        var bm = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                        bool mapOk = bm != null && bm.name == Path.GetFileNameWithoutExtension(style.baseColor);
                        if (!mapOk) wrong++;

                        var nm = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
                        string wantNormal = style.normal == null
                            ? null
                            : Path.GetFileNameWithoutExtension(style.normal);
                        bool normOk = wantNormal == null
                            ? nm == null
                            : nm != null && nm.name == wantNormal;
                        if (!normOk) wrong++;

                        bool shaderOk = m.shader != null && m.shader.name == "Echoes/PainterlyLit";
                        if (!shaderOk) wrong++;

                        // Stray maps are as bad as missing ones: a character
                        // wearing the village's brick roughness looks wet.
                        foreach (var prop in new[] { "_MetallicGlossMap", "_OcclusionMap", "_EmissionMap" })
                        {
                            var t = m.HasProperty(prop) ? m.GetTexture(prop) : null;
                            if (t != null) { wrong++; sb.AppendLine("  " + unit.name + ": STRAY MAP on " + prop); }
                        }

                        float restore = m.HasProperty("_ColorRestore") ? m.GetFloat("_ColorRestore") : -1f;
                        if (Mathf.Abs(restore) > 0.001f) wrong++;

                        var tint = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                        float val = tint.grayscale;
                        bool tintOk = Mathf.Abs(val - style.tint) < 0.005f;
                        if (!tintOk) wrong++;

                        if (Saturation(tint) > 0.02f) wrong++;

                        sb.AppendLine("  " + unit.name.PadRight(16) +
                                      (matOk ? "wearing Cast_" + style.who
                                             : "WEARING THE WRONG MATERIAL: " + gotPath) +
                                      ", _BaseMap " + (mapOk ? Describe(bm) : "WRONG, " + Describe(bm)) +
                                      ", _BumpMap " + (normOk ? Describe(nm) : "WRONG, " + Describe(nm)) +
                                      ", _ColorRestore " + restore.ToString("F2") +
                                      ", value " + val.ToString("F2") +
                                      (tintOk ? "" : "  EXPECTED " + style.tint.ToString("F2")) +
                                      ", hue " + Saturation(tint).ToString("P0"));

                        if (!finals.Contains(style.who)) finals.Add(style.who + " " + val.ToString("F2") + " (" + style.expected + ")");
                    }
                }

                // --- identity: the paintable ones, and only those ----------
                bool hasTarget = unit.GetComponent<ColorRestoreTarget>() != null;
                if (style.paintable && !hasTarget)
                {
                    wrong++;
                    sb.AppendLine("  " + unit.name + ": paintable but carries NO ColorRestoreTarget, " +
                                  "so the world cannot grey it back");
                }
                if (!style.paintable && hasTarget)
                {
                    wrong++;
                    sb.AppendLine("  " + unit.name + ": NOT paintable but still carries a ColorRestoreTarget, " +
                                  "so she could be painted and stop being a silhouette");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("the three, as one grey ramp: " + string.Join("  |  ", finals));
        sb.AppendLine();

        sb.AppendLine("VERDICT " + (wrong == 0 ? "PASS" : "FAIL, " + wrong + " problem(s)") +
                      (wrong == 0
                          ? ": all three on Echoes/PainterlyLit wearing their own skin by " +
                            "name, no hue in any tint, _ColorRestore 0, and only the " +
                            "paintable ones carry a ColorRestoreTarget."
                          : ""));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("scene saved.");
    }

    // ---------------------------------------------------------------------

    /// <summary>
    /// Loads a texture and, for a normal map, makes the importer treat it as
    /// one. A normal map imported as a normal-coloured image comes back with
    /// its channels gamma-decoded and its lighting information destroyed, and
    /// nothing in the render says why.
    /// </summary>
    static bool Import(string file, bool asNormal, StringBuilder sb,
                       Dictionary<string, Texture2D> into)
    {
        string path = TextureFolder + "/" + file;
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (t == null)
        {
            sb.AppendLine("MISSING: " + path + " did not import. Nothing is assigned for it.");
            return false;
        }

        if (asNormal)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { sb.AppendLine(path + ": no TextureImporter"); return false; }

            if (imp.textureType != TextureImporterType.NormalMap ||
                !imp.sRGBTexture)
            {
                imp.textureType = TextureImporterType.NormalMap;
                imp.sRGBTexture = false;
                imp.SaveAndReimport();
                t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                sb.AppendLine(path + ": reimported as a normal map (sRGB off)");
                if (t == null) { sb.AppendLine("  it did not come back. Stopping."); return false; }
            }
        }

        into[file] = t;
        bool alpha = GraphicsFormatUtility.HasAlphaChannel(t.graphicsFormat);
        sb.AppendLine("  " + file.PadRight(30) + t.width + " x " + t.height +
                      (asNormal ? "  normal, sRGB off" : "  alpha channel: " + alpha));
        return true;
    }

    static Material Material(StringBuilder sb, Style style, Material template,
                             Dictionary<string, Texture2D> tex)
    {
        string path = MaterialFolder + "/Cast_" + style.who + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            mat = new Material(template) { name = "Cast_" + style.who };
            AssetDatabase.CreateAsset(mat, path);

            // CreateAsset REPLACES the object you handed it. The instance that
            // was just written is not the instance that now lives at the path,
            // and assigning the stale one to a renderer is a silent no-op: the
            // renderer keeps whatever material it already had and nothing in the
            // render says why. This is the bug this tool had, and it is why the
            // cast appeared to be wearing brick.
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                sb.AppendLine("    CREATED BUT DID NOT LOAD BACK, refusing to assign it");
                return null;
            }
            sb.AppendLine("    created " + path + "  (reloaded from disk, name now '" + mat.name + "')");
        }
        else sb.AppendLine("    reused " + path + "  (loaded '" + mat.name + "')");

        if (mat.shader != Shader.Find("Echoes/PainterlyLit"))
            mat.shader = Shader.Find("Echoes/PainterlyLit");

        // --- the diagnostic that explains the silent skip -------------------
        //
        // This tool spent a run reporting PASS while the cast wore brick,
        // because every property write went through SerializedObject and every
        // one of them was guarded by "if (prop != null)". FindProperty was
        // returning null, the guard swallowed it, and ApplyModifiedProperties
        // then honestly reported that it had changed nothing. The guard meant
        // to be safe and it was the opposite: it converted a failure into a
        // quiet success.
        //
        // So the properties are now written through the Material API directly,
        // which does not depend on FindProperty finding anything, and which one
        // is reached is reported before the write rather than after.
        sb.AppendLine("    shader '" + (mat.shader == null ? "NULL" : mat.shader.name) + "'");

        var probe = new SerializedObject(mat);
        sb.AppendLine("    property probe (HasProperty / FindProperty):");
        foreach (var name in new[] { "_BaseMap", "_BumpMap", "_BaseColor", "_Color",
                                     "_ColorRestore", "_Smoothness", "_Metallic",
                                     "_BumpScale", "_MainTex" })
        {
            bool has = mat.HasProperty(name);
            bool found = probe.FindProperty(name) != null;
            sb.AppendLine("      " + name.PadRight(16) +
                          " HasProperty " + (has ? "yes" : "NO ") +
                          ", FindProperty " + (found ? "yes" : "NO ") +
                          (has && !found ? "   <-- HasProperty says yes, FindProperty says no" : ""));
        }

        // --- write, through the API that works ------------------------------

        foreach (var name in new[] { "_MainTex", "_NormalMap",
                                     "_MetallicGlossMap", "_OcclusionMap", "_EmissionMap" })
            if (mat.HasProperty(name)) mat.SetTexture(name, null);

        mat.SetTexture("_BaseMap", tex[style.baseColor]);

        if (style.normal != null) mat.SetTexture("_BumpMap", tex[style.normal]);
        else if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", null);

        // Value only. A tinted cast would be a colour-based tell in a game that
        // has not earned colour yet.
        var grey = new Color(style.tint, style.tint, style.tint, 1f);
        mat.SetColor("_BaseColor", grey);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", grey);

        // Greyscale to begin with. The world is colourless until Ari paints it,
        // and this is the value the editor shows; ColorRestoreTarget drives it
        // from here in play.
        if (mat.HasProperty("_ColorRestore")) mat.SetFloat("_ColorRestore", 0f);

        // Ink and a tree spirit are both matte. A village material cloned from
        // roofing tiles arrives far too smooth and makes a character look wet.
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.12f);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
        if (mat.HasProperty("_BumpScale"))  mat.SetFloat("_BumpScale",  style.bumpScale);

        EditorUtility.SetDirty(mat);

        // Without this the writes live only in memory and the NEXT run, which
        // loads the material from disk, gets the old brick values back. A tool
        // that reports success and then reverts on its own second run is worse
        // than a tool that fails.
        AssetDatabase.SaveAssetIfDirty(mat);

        // Read back immediately. An assignment that silently does not take is
        // the single most common way a character ends up wearing the wrong
        // thing, and the render never says why.
        var readMap = mat.GetTexture("_BaseMap");
        var readNrm = mat.GetTexture("_BumpMap");
        var readCol = mat.GetColor("_BaseColor");

        sb.AppendLine("    read back: _BaseMap " + (readMap == null ? "NULL" : readMap.name) +
                      ", _BumpMap " + (readNrm == null ? "NULL" : readNrm.name) +
                      ", _BaseColor value " + readCol.grayscale.ToString("F2") +
                      ", _Smoothness " + mat.GetFloat("_Smoothness") +
                      ", _BumpScale " + mat.GetFloat("_BumpScale") +
                      ", _ColorRestore " + mat.GetFloat("_ColorRestore"));

        if (readMap == null || readMap.name != Path.GetFileNameWithoutExtension(style.baseColor))
            sb.AppendLine("    *** the base map did NOT take. Everything below will be wrong. ***");
        if (Mathf.Abs(readCol.grayscale - style.tint) > 0.005f)
            sb.AppendLine("    *** the tint did NOT take. " + readCol.grayscale.ToString("F2") +
                          " is on the material, not the " + style.tint.ToString("F2") + " asked for. ***");

        return mat;
    }

    static string Describe(Texture t)
    {
        if (t == null) return "NULL  <-- ";
        var t2 = t as Texture2D;
        return t2 == null ? t.name + "  " : t2.name + " " + t2.width + "x" + t2.height + "  ";
    }

    static float Saturation(Color c)
    {
        float mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        float mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
        return mx <= 0f ? 0f : (mx - mn) / mx;
    }

    /// <summary>
    /// A real material already on the village shader, to copy.
    ///
    /// Found by looking at the scene's own renderers, NOT by asking the Asset
    /// Database. Measured: the village's materials — UnevenBrick, WoodTrim,
    /// WindowPane — are embedded inside the kit FBX rather than standing as
    /// .mat files, so FindAssets("t:Material") returns none of them and a
    /// search built on it finds an empty project and stops. A renderer's
    /// sharedMaterial hands the same object over whether it is embedded or not.
    /// </summary>
    static Material FindVillageMaterial(StringBuilder sb)
    {
        Material best = null;
        int bestScore = -1;
        string bestFrom = "";

        int Consider(Material m, string from)
        {
            if (m == null || m.shader == null) return 0;
            if (m.shader.name != "Echoes/PainterlyLit") return 0;

            int score = 0;
            foreach (var p in new[] { "_BaseMap", "_BumpMap", "_Smoothness",
                                      "_Metallic", "_BaseColor", "_ColorRestore" })
                if (m.HasProperty(p)) score++;

            if (m.GetTexture("_BaseMap") != null) score += 3;   // a skin, not a blank
            if (m.GetTexture("_BumpMap") != null)  score += 1;

            if (score > bestScore) { bestScore = score; best = m; bestFrom = from; }
            return score;
        }

        int seen = 0;
        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
        {
            if (r is ParticleSystemRenderer) continue;
            foreach (var m in r.sharedMaterials) { seen++; Consider(m, "renderer '" + r.name + "'"); }
        }

        // Fallback, in case the scene is empty of village props: any material
        // asset on the shader. Only the cast's own clones are excluded, or the
        // cast would end up cloning itself.
        int assets = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).StartsWith("Cast_")) continue;
            Consider(AssetDatabase.LoadAssetAtPath<Material>(path), "asset " + path);
            assets++;
        }

        if (best != null)
            sb.AppendLine("the village shader carries " + bestScore +
                          " points of the property set this needs, on '" + best.name +
                          "', taken from " + bestFrom);

        sb.AppendLine("  (scanned " + seen + " material slot(s) on renderers and " +
                      assets + " material asset(s))");

        return best;
    }

    /// <summary>
    /// Every unit belonging to a character, including ones that are switched off.
    ///
    /// Two things this has to get right, and both were wrong at first:
    ///
    ///   Mono is deactivated until his beat fires, and both GameObject.Find and
    ///   FindObjectsByType skip inactive objects, so the ordinary lookups return
    ///   nothing at all for the one character who most needs looking at.
    ///
    ///   The Ink Crawler is placed as THREE separate GameObjects named
    ///   InkCrawler_Back, InkCrawler_AlleyA and InkCrawler_AlleyB. An exact
    ///   name lookup finds none of them, which is how all three got skipped and
    ///   the crawler shipped unskinned. So this matches on a prefix.
    /// </summary>
    static List<GameObject> FindUnits(string who)
    {
        var found = new List<GameObject>();

        // HashSet<GameObject> rather than a set of ids: UnityEngine.Object
        // overrides Equals to compare instance identity, so this de-duplicates
        // correctly without touching GetInstanceID, which is obsolete in 6000.6.
        var seen = new HashSet<GameObject>();

        void Take(Transform t)
        {
            if (t == null) return;
            string n = t.name;
            if (n != who && !n.StartsWith(who + "_")) return;
            if (seen.Add(t.gameObject)) found.Add(t.gameObject);
        }

        // L1_Cast first: that is where the level puts them.
        var cast = GameObject.Find("L1_Cast");
        if (cast != null)
            foreach (Transform c in cast.transform) Take(c);

        // Then everywhere, so a unit dragged out of the container is still found.
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            Take(t);

        // MonoCompanion is the authority on who Mono is, and it finds him even
        // if somebody has renamed the GameObject.
        if (who == "Mono")
        {
            var m = MonoCompanion.FindInLevel();
            if (m != null && seen.Add(m.gameObject)) found.Add(m.gameObject);
        }

        return found;
    }
}
