using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Echoes.Painterly;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.InputSystem;

public static class L1DefectProbe
{
    const string ReportPath = "Temp/defects.txt";

    public static void Run()
    {
        var sb = new StringBuilder();

        try { Input(sb); }
        catch (Exception e) { sb.AppendLine("INPUT THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }

        sb.AppendLine();
        try { Hud(sb); }
        catch (Exception e) { sb.AppendLine("HUD THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }

        sb.AppendLine();
        try { Textures(sb); }
        catch (Exception e) { sb.AppendLine("TEXTURE THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace); }

        Debug.Log("[Echoes] defect probe\n" + sb);
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), ReportPath), sb.ToString());
    }

    // ======================================================================
    // 1. ARI CANNOT SWING THE BRUSH
    // ======================================================================

    static void Input(StringBuilder sb)
    {
        sb.AppendLine("=============== BUG 1: ARI CANNOT SWING THE BRUSH ===============");
        sb.AppendLine();

        // --- is there a mouse at all? ---------------------------------------

        // activeInputHandler is internal in Unity 6, so it is read reflectively
        // and its absence is reported rather than treated as a value. The
        // observable that actually decides whether the brush works is
        // Mouse.current, two lines below, and that one is a plain field read.
        var handlerProp = typeof(PlayerSettings).GetProperty("activeInputHandler",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (handlerProp == null)
        {
            sb.AppendLine("input handler: NOT READABLE (the property is internal in Unity 6). " +
                          "Falling back to the observable:");
        }
        else
        {
            int handler = (int)handlerProp.GetValue(null);
            sb.AppendLine("input handler (0 = old only, 1 = new only, 2 = both): " + handler +
                          (handler == 0
                              ? "   <-- the Input System package is NOT active, so Mouse.current is null"
                              : handler == 2 ? "   (both, new system included)" : "   (Input System active)"));
        }

        sb.AppendLine("Keyboard.current: " + (Keyboard.current == null ? "NULL" : "present"));
        sb.AppendLine("Mouse.current:    " + (Mouse.current == null ? "NULL" : "present"));

        if (Mouse.current != null)
        {
            var p = Mouse.current.position.ReadValue();
            sb.AppendLine("  mouse position: " + p.ToString("F0") +
                          "   (0,0 is the bottom-left of the game view)");
        }

        sb.AppendLine();

        // --- is the brush on Ari, and is it allowed to swing? ---------------

        var ari = GameObject.Find("Ari");
        if (ari == null) { sb.AppendLine("NO ARI IN THE SCENE."); return; }

        var brushes = ari.GetComponentsInChildren<BrushPainter>(true);
        sb.AppendLine("BrushPainter components under 'Ari': " + brushes.Length);

        for (int i = 0; i < brushes.Length; i++)
        {
            var b = brushes[i];
            var so = new SerializedObject(b);

            sb.AppendLine("  [" + i + "] on '" + Path2(b.transform) + "'");
            sb.AppendLine("      enabled " + (b.enabled ? "yes" : "NO") +
                          ", activeInHierarchy " + b.gameObject.activeInHierarchy);
            sb.AppendLine("      testMode  = " + Bool(so, "testMode") +
                          (Bool(so, "testMode") ? "" : "   <-- FALSE, so the mouse path never runs"));
            sb.AppendLine("      debugKeys = " + Bool(so, "debugKeys"));
            sb.AppendLine("      radius    = " + Float(so, "radius") +
                          " m, cooldown = " + Float(so, "cooldown") + " s");
            sb.AppendLine("      aimCamera = " +
                          (so.FindProperty("aimCamera").objectReferenceValue == null
                              ? "null (falls back to Camera.main)"
                              : so.FindProperty("aimCamera").objectReferenceValue.name));

            // The state only exists in play mode; this is what the editor can say.
            if (!Application.isPlaying)
                sb.AppendLine("      nextStrokeTime = n/a outside play mode");
        }

        sb.AppendLine();

        // --- what would a click actually hit? -------------------------------

        var cam = Camera.main;
        if (cam == null) { sb.AppendLine("NO Camera.main."); }
        else
        {
            sb.AppendLine("Camera.main '" + cam.name + "' at " + cam.transform.position.ToString("F2") +
                          ", pixel size " + cam.pixelWidth + " x " + cam.pixelHeight);

            var centre = new Vector3(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f, 0f);
            var ray = cam.ScreenPointToRay(centre);

            sb.AppendLine("centre-screen ray: " + (Physics.Raycast(ray, out RaycastHit hit, 250f)
                ? "hits '" + hit.collider.name + "' at " + hit.distance.ToString("F2") + " m, point " + hit.point.ToString("F2")
                : "HITS NOTHING — a click here cannot paint, because a stroke needs a world point"));

            if (Physics.Raycast(ray, out RaycastHit near, 250f))
            {
                // A stroke on bare floor is legal but invisible; the interesting
                // question is whether there is anything to paint there at all.
                int inRadius = ColorRestoreTarget.RestoreInRadius(near.point, 6f, 1f, -1f);
                sb.AppendLine("  a stroke there reaches " + inRadius + " ColorRestoreTarget(s) in 6 m");
                if (inRadius == 0)
                    sb.AppendLine("  <-- ZERO targets: the swing runs but paints nothing, " +
                                  "which looks exactly like the brush being broken");
            }
        }

        sb.AppendLine();
        sb.AppendLine("ColorRestoreTarget.AllActive: " + ColorRestoreTarget.AllActive.Count() + " target(s) in the level");

        // --- and does the tree stand where the beat thinks it does? ----------

        var tree = UnityEngine.Object.FindAnyObjectByType<SleepingTree>();
        if (tree == null) { sb.AppendLine("no SleepingTree in the scene."); return; }

        var tso = new SerializedObject(tree);
        var tp = tso.FindProperty("touchPoint").objectReferenceValue as Transform;

        sb.AppendLine();
        sb.AppendLine("SleepingTree:");
        sb.AppendLine("  touchPoint  = " + (tp == null ? "NULL" : tp.name + " at " + tp.position.ToString("F2")));
        sb.AppendLine("  touchDistance = " + Float(tso, "touchDistance") + " m, strokeHitDistance = " +
                      Float(tso, "strokeHitDistance") + " m");
        sb.AppendLine("  selfColour  = " +
                      (tso.FindProperty("selfColour").objectReferenceValue == null
                          ? "NULL  <-- nothing to take colour" : "set"));
        sb.AppendLine("  mono        = " + (tso.FindProperty("mono").objectReferenceValue == null ? "NULL" : "set"));
        sb.AppendLine("  chase       = " + (tso.FindProperty("chase").objectReferenceValue == null ? "NULL" : "set"));
        sb.AppendLine("  burstRadius = " + Float(tso, "burstRadius") + " m");

        var mover = ari.GetComponent<AriMover>();
        if (mover != null && tp != null)
        {
            var d = mover.transform.position - tp.position;
            d.y = 0f;
            float dist = d.magnitude;

            sb.AppendLine("  Ari is " + dist.ToString("F2") + " m from the touch point");
            sb.AppendLine("  AriIsClose() = " + tree.AriIsClose() +
                          (dist > Float(tso, "touchDistance")
                              ? "   <-- she is FURTHER than touchDistance, so the prompt never shows"
                              : ""));

            int reached = ColorRestoreTarget.RestoreInRadius(tp.position, Float(tso, "burstRadius"), 1f, -1f);
            sb.AppendLine("  a burst at the tree reaches " + reached + " ColorRestoreTarget(s) in " +
                          Float(tso, "burstRadius") + " m");
        }
    }

    // ======================================================================
    // 2. THE TEXT IS CUTTING OFF
    // ======================================================================

    static void Hud(StringBuilder sb)
    {
        sb.AppendLine("=============== BUG 2: THE TEXT IS CUTTING OFF ===============");
        sb.AppendLine();

        var font = BuiltinFont();
        sb.AppendLine("measuring with font: " + (font == null ? "NONE FOUND" : font.name));
        if (font == null)
        {
            sb.AppendLine("no built-in font, so the HUD's own font size cannot be reproduced. " +
                          "GUIStyle.CalcHeight with no font returns a guess, not a measurement.");
            return;
        }

        // A tuple array, not a rectangular int[,].
        //
        // foreach over a two dimensional array hands back each element as an
        // int, not as a row, so s[0] indexes an integer. The compiler is right
        // and the array is the wrong shape for the loop.
        var screens = new (int w, int h)[]
        {
            (1920, 1080), (2560, 1440), (1280, 720), (1600, 900)
        };

        var lines = new List<string>();
        lines.Add("The old tree is grey. Swing the brush at it.");

        var asset = AssetDatabase.LoadAssetAtPath<MonoHintLines>("Assets/Painterly/MonoHintLines.asset");
        if (asset == null) sb.AppendLine("MonoHintLines.asset NOT FOUND at Assets/Painterly/");
        else
        {
            int beat, rung, ambient, muted;
            asset.Tally(out beat, out rung, out ambient, out muted);
            sb.AppendLine("MonoHintLines: " + beat + " beat, " + rung + " rung, " +
                          ambient + " ambient, " + muted + " muted");
            foreach (var l in asset.All) lines.Add(l.text);
        }

        sb.AppendLine();
        sb.AppendLine("BeatHud draws into a FIXED 60 px tall rect with wordWrap on, so " +
                      "anything taller than 60 px is clipped by GUI.Label. Needed height:");
        sb.AppendLine();

        foreach (var l in lines)
        {
            if (string.IsNullOrWhiteSpace(l)) continue;

            sb.AppendLine("  \"" + l.Replace("\n", " / ") + "\"");

            foreach (var (w, h) in screens)
            {
                int fontSize = Mathf.RoundToInt(19f * Mathf.Max(0.7f, h / 720f));
                float width = Mathf.Min(w * 0.62f, 760f);

                var style = new GUIStyle
                {
                    font = font,
                    fontSize = fontSize,
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter
                };

                // CalcHeight takes GUIContent and a width. There is no
                // (Rect, string) overload — that shape is CalcSize's, and it
                // refuses to wrap, which is the one thing being measured.
                float need = style.CalcHeight(new GUIContent(l), width);

                sb.AppendLine("      " + w + "x" + h + "  font " + fontSize +
                              "  width " + width.ToString("F0") +
                              "  needs " + need.ToString("F0") + " px  " +
                              (need > 60f
                                  ? "CLIPPED, " + (need - 60f).ToString("F0") + " px missing"
                                  : "fits"));
            }
        }
    }

    // ======================================================================
    // 3. NO TEXTURE ON MONO, INK CRAWLER, COLOR THIEF
    // ======================================================================

    static void Textures(StringBuilder sb)
    {
        sb.AppendLine("=============== BUG 3: NO TEXTURE ON THE CAST ===============");
        sb.AppendLine();

        string[] cast = { "Mono", "ColorThief", "InkCrawler_Back", "InkCrawler_AlleyA", "InkCrawler_AlleyB" };

        foreach (var name in cast)
        {
            var go = FindDeep(name);
            if (go == null) { sb.AppendLine(name + ": NOT FOUND"); continue; }

            var rends = go.GetComponentsInChildren<Renderer>(true);
            int withTex = 0;

            sb.AppendLine(name + " — " + rends.Length + " renderer(s)");

            for (int i = 0; i < rends.Length; i++)
            {
                var r = rends[i];
                var mats = r.sharedMaterials;

                if (mats == null || mats.Length == 0)
                {
                    sb.AppendLine("   [" + i + "] " + r.GetType().Name + ": NO MATERIAL AT ALL — " +
                                  "Unity draws it with the built-in default, which is untextured");
                    continue;
                }

                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null)
                    {
                        sb.AppendLine("   [" + i + "] slot " + m + ": NULL material");
                        continue;
                    }

                    string path = AssetDatabase.GetAssetPath(mat);
                    string shader = mat.shader != null ? mat.shader.name : "NO SHADER";
                    var tex = mat.mainTexture;

                    string verdict;
                    if (tex == null) verdict = "NO TEXTURE  <-- this is the bug";
                    else if (tex.width < 64 || tex.height < 64) verdict = "texture is " + tex.width + "x" + tex.height + ", too small to be art";
                    else { verdict = "texture " + tex.width + "x" + tex.height; withTex++; }

                    sb.AppendLine("   [" + i + "] slot " + m + "  " + verdict);
                    sb.AppendLine("        material " + mat.name + "  at " + Shorten(path));
                    sb.AppendLine("        shader   " + shader +
                                  (shader == "Standard" ? "   <-- built-in Standard renders wrong in URP" : ""));
                    sb.AppendLine("        _BaseMap  " + (mat.HasProperty("_BaseMap") ? "yes" : "NO, shader has no _BaseMap") +
                                  "   _MainTex " + (mat.HasProperty("_MainTex") ? "yes" : "no") +
                                  "   _Color " + (mat.HasProperty("_Color") ? mat.GetColor("_Color").ToString() : "n/a"));
                }
            }

            sb.AppendLine("   -> " + withTex + " of " + rends.Length + " renderer(s) have a real texture");
            sb.AppendLine();
        }

        // --- what textures exist that might belong to them? -----------------

        sb.AppendLine("--- every texture asset in the project, by name ---");
        var all = AssetDatabase.FindAssets("t:Texture2D");
        sb.AppendLine("found " + all.Length);

        foreach (var g in all.OrderBy(g => g))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (t == null) continue;

            sb.AppendLine("  " + (t.width + "x" + t.height).PadRight(11) +
                          t.format.ToString().PadRight(22) + Shorten(p));
        }

        // --- and a prop that does have a texture, for contrast ---------------

        sb.AppendLine();
        sb.AppendLine("--- a village prop that DOES have a texture, for contrast ---");
        int shown = 0;

        foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>())
        {
            if (shown >= 4) break;
            var m = r.sharedMaterial;
            if (m == null || m.mainTexture == null) continue;
            if (r.GetType().Name != "MeshRenderer") continue;

            sb.AppendLine("  " + r.name + "  ->  " + m.name + "  " +
                          m.shader.name + "  " + m.mainTexture.width + "x" + m.mainTexture.height);
            shown++;
        }

        if (shown == 0) sb.AppendLine("  NOT ONE MeshRenderer in the scene has a texture. " +
                                      "That would mean the problem is the renderer, not the cast.");
    }

    // ======================================================================
    // helpers
    // ======================================================================

    static Font BuiltinFont()
    {
        // Renamed in Unity 6, and the old name throws rather than returning null.
        try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch (Exception) { }

        try { return Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        catch (Exception) { }

        return null;
    }

    static string Path2(Transform t)
    {
        var sb = new StringBuilder(t.name);
        for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
        return sb.ToString();
    }

    static string Shorten(string p)
    {
        if (string.IsNullOrEmpty(p)) return "(not an asset — scene object)";
        int i = p.LastIndexOf('/');
        return i < 0 ? p : p.Substring(i + 1);
    }

    static GameObject FindDeep(string name)
    {
        var direct = GameObject.Find(name);
        if (direct != null) return direct;

        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>())
            if (t.name == name) return t.gameObject;

        return null;
    }

    static bool Bool(SerializedObject so, string field)
    {
        var p = so.FindProperty(field);
        return p != null && p.propertyType == SerializedPropertyType.Boolean && p.boolValue;
    }

    static float Float(SerializedObject so, string field)
    {
        var p = so.FindProperty(field);
        return p != null && p.propertyType == SerializedPropertyType.Float ? p.floatValue : -1f;
    }
}
