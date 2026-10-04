using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Puts the ink skin on the crawler that is actually in the scene.
    ///
    /// <para><b>This exists because the first attempt did not work, and the way
    /// it did not work is the reason this tool reports before it writes.</b>
    /// The texture was generated and assigned to
    /// <c>Assets/Painterly/Materials/Cast_InkCrawler.mat</c>, which is a real
    /// material asset, and the tool reported PASS because it read the texture
    /// back off that material. It was true. The material was simply not on
    /// anything.</para>
    ///
    /// <para>The crawler's skin is a <c>SkinnedMeshRenderer</c> inside
    /// <c>InkCrawler.fbx</c>, and its material is <c>material_0</c> — embedded in
    /// the model, with no <c>_BaseMap</c>. So the generated skin was sitting on a
    /// material asset in the project while the creature in the level rendered
    /// from an embedded one. A read-back off the wrong material confirms
    /// nothing about what is on screen.</para>
    ///
    /// <para><b>So this walks the renderers rather than the materials.</b> It
    /// reports what each crawler is actually rendering with, assigns the project
    /// material to every slot, and then reads back off the <i>renderer</i> —
    /// because the renderer is the thing that decides what appears.</para>
    ///
    /// <para><b>Why the project material is the right one to move to.</b> A
    /// material embedded in an FBX cannot be edited without re-importing the
    /// model and losing the edit on every import. Moving to the project's
    /// <c>Cast_InkCrawler.mat</c> means the ink skin is a change to a file the
    /// build keeps.</para>
    /// </summary>
    public static class InkApply
    {
        const string MatPath = "Assets/Painterly/Materials/Cast_InkCrawler.mat";
        const string Report = "Temp/ink_apply.txt";

        [MenuItem("Tools/Echoes/Apply Ink Skin To Crawlers", priority = 41)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] apply ink skin to the crawlers that are real");

            if (EditorApplication.isPlaying)
            {
                sb.AppendLine("  refused: in play mode");
                Finish(sb);
                return;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null)
            {
                sb.AppendLine("  FATAL: no material at " + MatPath);
                Finish(sb);
                return;
            }

            // The material is only useful if it carries the skin. Stated here,
            // once, so a run where InkBuild has not been run yet says so instead
            // of quietly assigning a blank material and looking like it worked.
            var baseMap = mat.HasProperty("_BaseMap")
                ? mat.GetTexture("_BaseMap") as Texture2D
                : null;

            sb.AppendLine("  target material : " + mat.name + " (" + MatPath + ")");
            sb.AppendLine("  shader          : " + (mat.shader != null ? mat.shader.name : "NULL"));
            sb.AppendLine("  _BaseMap        : " + (baseMap == null
                ? "NONE — run 'Build Ink Crawler Skin' first, or this will grey " +
                  "the crawler instead of giving it a skin"
                : baseMap.name + " " + baseMap.width + "x" + baseMap.height));
            sb.AppendLine();

            var crawlers = Object.FindObjectsByType<InkCrawler>(FindObjectsInactive.Include);
            sb.AppendLine("crawlers in the level: " + crawlers.Length);
            if (crawlers.Length == 0)
            {
                sb.AppendLine("  FATAL: no InkCrawler in the scene, so there is " +
                              "nothing to put a skin on.");
                Finish(sb);
                return;
            }

            int fixedCount = 0, alreadyOk = 0, noSkin = 0;

            for (int i = 0; i < crawlers.Length; i++)
            {
                var c = crawlers[i];
                var skins = c.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                sb.AppendLine();
                sb.AppendLine("-- " + c.name + " at " + c.transform.position.ToString("F2") +
                              "  (" + (c.gameObject.activeInHierarchy ? "active" : "INACTIVE") + ")");

                if (skins.Length == 0)
                {
                    // Not a failure to be repaired here: this crawler has no
                    // skin at all, which is a different problem and belongs to
                    // Beat5Setup, not to a material swap.
                    sb.AppendLine("   NO SkinnedMeshRenderer — a material cannot " +
                                  "fix this. The creature is invisible.");
                    noSkin++;
                    continue;
                }

                for (int s = 0; s < skins.Length; s++)
                {
                    var smr = skins[s];
                    var slots = smr.sharedMaterials;

                    sb.AppendLine("   skin '" + smr.name + "' " + slots.Length + " slot(s)");

                    for (int m = 0; m < slots.Length; m++)
                    {
                        var cur = slots[m];
                        string was = cur == null ? "NONE" : cur.name;
                        string where = cur == null ? "?"
                            : (AssetDatabase.GetAssetPath(cur).Length > 0
                                ? AssetDatabase.GetAssetPath(cur)
                                : "EMBEDDED IN THE MODEL");
                        string map = cur == null || !cur.HasProperty("_BaseMap")
                            ? "n/a"
                            : (cur.GetTexture("_BaseMap") == null ? "NONE" : "set");

                        sb.AppendLine("      slot " + m + ": " + was +
                                      "  _BaseMap " + map + "  <- " + where);
                    }

                    // Only slots that are actually wrong are touched. A slot
                    // already pointing at the project material is left alone,
                    // so running this twice cannot create a duplicate
                    // material instance in the renderer.
                    bool needs = false;
                    for (int m = 0; m < slots.Length; m++)
                    {
                        if (slots[m] != mat) { needs = true; break; }
                    }

                    if (!needs)
                    {
                        sb.AppendLine("      already on the project material — left alone");
                        alreadyOk++;
                        continue;
                    }

                    for (int m = 0; m < slots.Length; m++) slots[m] = mat;
                    smr.sharedMaterials = slots;
                    EditorUtility.SetDirty(smr);

                    fixedCount++;
                    sb.AppendLine("      -> ALL SLOTS now use " + mat.name);
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            // --- read back off the RENDERER, not the material --------------
            //
            // This is the check the first attempt got wrong in principle: it
            // proved the material had a texture, which says nothing about what
            // the creature is wearing. What decides is the renderer's slot.
            sb.AppendLine();
            sb.AppendLine("=== READ BACK OFF THE RENDERERS ===");

            int confirmed = 0, mismatched = 0;
            var crawlers2 = Object.FindObjectsByType<InkCrawler>(FindObjectsInactive.Include);

            for (int i = 0; i < crawlers2.Length; i++)
            {
                var smr = crawlers2[i].GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (smr == null) { mismatched++; continue; }

                var slots = smr.sharedMaterials;
                int right = 0;
                for (int m = 0; m < slots.Length; m++)
                    if (slots[m] == mat) right++;

                bool all = right == slots.Length && slots.Length > 0;
                if (all) confirmed++; else mismatched++;

                sb.AppendLine("  " + crawlers2[i].name.PadRight(22) +
                              (all ? "OK   " : "BAD  ") +
                              right + "/" + slots.Length + " slot(s) on " + mat.name);
            }

            sb.AppendLine();
            sb.AppendLine("RESULT: " + confirmed + " crawler(s) wearing the ink skin, " +
                          mismatched + " not");
            sb.AppendLine("  repaired " + fixedCount + ", already correct " + alreadyOk +
                          ", no skin at all " + noSkin);
            sb.AppendLine("  " + (confirmed == crawlers2.Length
                ? "PASS — every crawler in the level renders from " + mat.name +
                  ", which carries " + (baseMap == null ? "NOTHING" : baseMap.name)
                : "FAIL — " + mismatched + " crawler(s) are not on it. Their skins " +
                  "are embedded in the FBX and the swap did not stick; reimport " +
                  "or assign by hand."));

            sb.AppendLine();
            sb.AppendLine(SaveAfter.Save("crawler materials -> Cast_InkCrawler"));
            sb.AppendLine();
            sb.AppendLine("  Still to confirm by eye: the skin is near-black with a " +
                          "luminance spread of 0.02, which will read as a subtle " +
                          "surface. If it looks like a flat silhouette on screen, " +
                          "the spread is too low and not the assignment.");

            Finish(sb);
        }

        static void Finish(StringBuilder sb)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), Report),
                sb.ToString());
            Debug.Log("[Echoes] ink skin applied — see " + Report);
        }
    }
}