using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Echoes.Painterly;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;
public static class CastLookProbe
{
    const string ReportPath = "Temp/cast_look.txt";

    public static void Run()
    {
        var sb = new StringBuilder();

        try { Body(sb); }
        catch (Exception e)
        {
            sb.AppendLine("THREW: " + e.GetType().Name + ": " + e.Message);
            sb.AppendLine(e.StackTrace);
        }

        Debug.Log("[Echoes] cast look\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportPath),
                           sb.ToString());
    }

    static void Body(StringBuilder sb)
    {
        sb.AppendLine("WHY THE CAST LOOKS UNTEXTURED");
        sb.AppendLine();

        // --- 1. who is actually in the cast container, by their real names ---

        var cast = GameObject.Find("L1_Cast");
        sb.AppendLine("L1_Cast: " + (cast == null ? "NOT FOUND" : cast.transform.childCount + " child(ren)"));

        var units = new List<GameObject>();

        if (cast != null)
            foreach (Transform c in cast.transform) units.Add(c.gameObject);

        foreach (var u in units)
        {
            sb.AppendLine("  '" + u.name + "'  at " + u.transform.position.ToString("F2") +
                          "  components: " + u.GetComponents<Component>().Length);

            foreach (var comp in u.GetComponents<Component>())
                sb.AppendLine("      " + comp.GetType().Name);
        }

        sb.AppendLine();

        // Mono is found by its mind, not its name, because the cast tool placed
        // an FBX instance whose object name is not the one the designer typed.
        var monoComp = MonoCompanion.FindInLevel();
        sb.AppendLine("MonoCompanion.FindInLevel() -> " +
                      (monoComp == null ? "NULL" : "'" + monoComp.gameObject.name + "' at " +
                                         monoComp.transform.position.ToString("F2")));
        sb.AppendLine();

        // --- 2. the material on every renderer, and the shader it is on -----

        var painterly = Shader.Find("Echoes/PainterlyLit");

        sb.AppendLine("Echoes/PainterlyLit resolves: " + (painterly == null ? "NO" : "yes, '" + painterly.name + "'"));
        sb.AppendLine();

        var targets = new List<string>();
        if (cast != null) foreach (Transform c in cast.transform) targets.Add(c.gameObject.name);
        if (monoComp != null) targets.Add(monoComp.gameObject.name);

        foreach (var name in targets.Distinct())
        {
            var go = FindDeep(name);
            if (go == null) continue;

            sb.AppendLine("--- " + name + " ---");

            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                sb.AppendLine("  " + r.GetType().Name + " '" + r.name + "', " +
                              (mats == null ? 0 : mats.Length) + " material slot(s)");

                if (mats == null) continue;

                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null) { sb.AppendLine("    [" + i + "] null"); continue; }

                    string path = AssetDatabase.GetAssetPath(m);
                    bool onPainterly = m.shader != null && m.shader.name == "Echoes/PainterlyLit";

                    sb.AppendLine("    [" + i + "] " + m.name +
                                  "  shader '" + (m.shader != null ? m.shader.name : "none") + "'" +
                                  (onPainterly ? "" : "   <-- NOT the village shader, so it " +
                                                    "is neither greyed nor paintable"));

                    if (m.HasProperty("_BaseMap"))
                    {
                        var t = m.GetTexture("_BaseMap") as Texture2D;
                        sb.AppendLine("        _BaseMap " + (t == null
                            ? "NULL  <-- this is why there is no texture" : t.name + " " + t.width + "x" + t.height));
                    }
                    else sb.AppendLine("        no _BaseMap property on this shader");

                    // What the villager shaders carry, for comparison.
                    sb.AppendLine("        asset path: " + (string.IsNullOrEmpty(path)
                        ? "(scene object, not an asset)" : path));
                }
            }

            sb.AppendLine();
        }

        // --- 3. do character textures exist anywhere under Assets? -----------

        sb.AppendLine("--- every texture under Assets/, by folder ---");

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        sb.AppendLine("found " + guids.Length + " texture(s) under Assets");

        var folders = new Dictionary<string, List<string>>();

        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            string folder = Path.GetDirectoryName(p).Replace('\\', '/');
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (t == null) continue;

            if (!folders.TryGetValue(folder, out var list)) folders[folder] = list = new List<string>();
            list.Add(t.width + "x" + t.height + " " + Path.GetFileName(p));
        }

        foreach (var kv in folders.OrderBy(k => k.Key))
        {
            sb.AppendLine("  " + kv.Key + "   (" + kv.Value.Count + ")");
            foreach (var s in kv.Value.OrderBy(s => s)) sb.AppendLine("      " + s);
        }

        sb.AppendLine();

        // --- 4. is there a texture for each character, by name? --------------

        sb.AppendLine("--- name search for character art ---");
        string[] want = { "mono", "crawler", "thief", "ink", "char", "ari" };

        foreach (var w in want)
        {
            var hits = guids.Where(g =>
                AssetDatabase.GUIDToAssetPath(g).ToLowerInvariant().Contains(w)).ToList();

            sb.AppendLine("  '" + w + "': " + hits.Count + " hit(s)");
            foreach (var g in hits.Take(12))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                sb.AppendLine("      " + p + (t == null ? "" : "  " + t.width + "x" + t.height));
            }
        }

        sb.AppendLine();
        sb.AppendLine("The FBX folders are where character textures would live if they");
        sb.AppendLine("existed at all. An empty result there is the answer: the kit ships");
        sb.AppendLine("the models without their skins, and no texture can be assigned");
        sb.AppendLine("that was not drawn for that character.");
    }

    static GameObject FindDeep(string name)
    {
        var direct = GameObject.Find(name);
        if (direct != null) return direct;

        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>())
            if (t.name == name) return t.gameObject;

        return null;
    }
}
