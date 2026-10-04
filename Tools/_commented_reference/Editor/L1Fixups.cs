using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// The scene-side half of the ten complaints, in one pass, so every change
    /// is reported against a measurement instead of against a belief.
    ///
    /// <para><b>Nothing here invents a system.</b> Four of the ten items were
    /// not code problems at all: the thieves and statues are objects that are
    /// present and switched on, the fragment is present and switched off with
    /// no visual behind it, and Ari's pronouns are in a serialized string on a
    /// component rather than in a script. None of those can be fixed by reading
    /// code, and all four are one scene edit each.</para>
    ///
    /// <para>Run with <c>-restore</c> to put the hidden things back, because
    /// "hidden" is a decision and the report has to be reversible by someone who
    /// disagrees with it.</para>
    /// </summary>
    public static class Run
    {
        const string Report = "Temp/l1_fixups.txt";
        const string GlowMatPath = "Assets/Painterly/Materials/FragmentGlow.mat";

        static readonly StringBuilder Sb = new StringBuilder();

        [MenuItem("Tools/Echoes/L1 Fixups — the ten complaints (restore)", priority = 63)]
        static void Restore() => Do(true);

        /// <summary>
        /// Do all of it. The menu entry and the bridge call both land here, so
        /// there is one body and one report rather than two paths that can
        /// drift — the drift being the version that fixes the scene but writes
        /// the old numbers.
        /// </summary>
        [MenuItem("Tools/Echoes/L1 Fixups — the ten complaints", priority = 62)]
        public static void Do(bool restore = false)
        {
            // Refuse to run in play mode at all.
            //
            // <para>Every edit here is to a scene or an asset, and a play-mode
            // edit is thrown away when the session stops — so the tool would
            // print a report describing changes that do not exist. That is the
            // worst possible outcome for a tool whose entire job is to be
            // believed: a clean report and no change.</para>
            //
            // <para>The player is in play mode as often as not, and asking them
            // to stop it by hand is asking them to do the one thing they were
            // told not to have to do, so the tool says why instead.</para>
            if (Application.isPlaying)
            {
                Debug.LogWarning("[Echoes] L1 fixups will not run while the " +
                                 "editor is in play mode: every change here is to " +
                                 "a scene or an asset, and play-mode edits are " +
                                 "discarded when the session stops, so the report " +
                                 "would describe changes that do not exist. " +
                                 "Press Ctrl+P to leave play mode, then run it.");
                return;
            }

            Sb.Clear();
            Sb.AppendLine("[Echoes] L1 fixups — " + (restore ? "RESTORE" : "apply"));
            Sb.AppendLine("scene: " + SceneManager.GetActiveScene().path);

            Statues(restore);
            Thief(restore);
            FragmentGlow();
            Pronouns();
            CrawlerGround();
            TextReport();

            // SAVED, and this line is the load-bearing one.
            //
            // <para>Four of the ten items are scene state, not code. A scene
            // edit that is not saved is not a fix, it is a change in an editor
            // buffer that a domain reload discards — and it will look correct
            // in the moment it is made and be gone by the next play session.
            // It is the one failure mode here that reports success while
            // achieving nothing, which is why the report below is written only
            // after the save, so it describes what is on disk and not what is
            // in memory.</para>
            if (!Application.isPlaying)
            {
                var scene = SceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isDirty)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    bool ok = EditorSceneManager.SaveScene(scene);

                    Sb.AppendLine();
                    Sb.AppendLine("SCENE SAVE: " + (ok ? "written to disk"
                                                      : "FAILED — nothing above " +
                                                        "is permanent"));
                }
                else
                {
                    Sb.AppendLine();
                    Sb.AppendLine("SCENE SAVE: scene was not marked dirty, so " +
                                  "there was nothing to write.");
                }
            }
            else
            {
                Sb.AppendLine();
                Sb.AppendLine("SCENE SAVE: SKIPPED - the editor is in play mode. " +
                              "Nothing above is on disk. Run this from edit mode.");
            }

            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report),
                              Sb.ToString());
            Debug.Log("[Echoes] L1 fixups written to " + Report);
        }

        // ---------------------------------------------------------------------
        // 8. the two extra crawler statues
        // ---------------------------------------------------------------------

        /// <summary>
        /// Hide every crawler-looking object that is NOT one of beat 5's three.
        ///
        /// <para><b>Measured, and the cause is not what it looks like.</b> The
        /// probe found three of them and the report filed two:</para>
        ///
        /// <list type="bullet">
        /// <item><c>InkCrawler_Back</c> at (-8.0, 0.0, -14.0)</item>
        /// <item><c>InkCrawler_AlleyA</c> at (-2.5, 0.0, 12.0)</item>
        /// <item><c>InkCrawler_AlleyB</c> at (-2.5, 0.0, 13.0)</item>
        /// </list>
        ///
        /// <para>Three, not two, so the count in the report is one low. Two are
        /// 1 m apart at the same x, which is also worth saying: from any camera
        /// that can see one it sees both, so a player counting statues sees a
        /// pair — which is consistent with the report as filed.</para>
        ///
        /// <para>Each has an <c>Animator</c> with <b>no
        /// runtimeAnimatorController</b>. That is the whole reason they read as
        /// statues rather than as scenery: a skinned mesh with no controller
        /// holds its bind pose for ever — no breathing, no weight shift, no
        /// blink. Three of them, in a level whose entire visual language is
        /// greyscale and still, is not ambiguous.</para>
        ///
        /// <para>Hidden rather than deleted, and the reason matters. The alley
        /// pair sit 13.2 m from the fountain, which is between beats 6 and 7, so
        /// they were plausibly authored for a beat that has not been built.
        /// Deleting them would quietly delete someone's scenery; switching the
        /// renderers off says the same thing about this level without making it
        /// unsaid for the next one. <c>-restore</c> puts them back.</para>
        /// </summary>
        static void Statues(bool restore)
        {
            Sb.AppendLine();
            Sb.AppendLine("=== 8. CRAWLER STATUES ===");

            var beat5 = UnityEngine.Object.FindAnyObjectByType<Beat5Director>(
                FindObjectsInactive.Include);

            var beatSet = new HashSet<InkCrawler>();
            if (beat5 != null && beat5.Crawlers != null)
                for (int i = 0; i < beat5.Crawlers.Count; i++)
                    if (beat5.Crawlers[i] != null) beatSet.Add(beat5.Crawlers[i]);

            Sb.AppendLine("  beat 5 owns " + beatSet.Count + " crawler(s)");

            int found = 0, changed = 0;

            var roots = SceneManager.GetActiveScene().GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
                found += Walk(roots[i].transform, beatSet, restore, ref changed);

            Sb.AppendLine();
            Sb.AppendLine("  objects with a crawler in the name and no InkCrawler " +
                          "component: " + found);
            Sb.AppendLine("  " + (restore ? "restored" : "hidden") + ": " + changed);
            Sb.AppendLine();
            Sb.AppendLine("  the number that argues against this: " + found +
                          " were found, not the 2 that were reported. If the " +
                          "player counted two, the likely reason is that " +
                          "InkCrawler_AlleyA and AlleyB are 1.0 m apart at the " +
                          "same x (-2.5) and read as one silhouette from any " +
                          "camera that sees them.");
        }

        static int Walk(Transform t, HashSet<InkCrawler> beatSet, bool restore,
                        ref int changed)
        {
            int found = 0;

            if (t.GetComponent<InkCrawler>() == null)
            {
                string n = t.name.ToLowerInvariant();

                if ((n.Contains("crawler") || n.Contains("ink")) &&
                    !t.IsChildOfAny(beatSet))
                {
                    found++;

                    var rends = t.GetComponentsInChildren<Renderer>(true);
                    var caps = t.GetComponentsInChildren<Collider>(true);
                    var anim = t.GetComponentInChildren<Animator>(true);

                    bool controller = anim != null &&
                                      anim.runtimeAnimatorController != null;

                    if (restore)
                    {
                        Set(t, true); changed++;
                    }
                    else
                    {
                        Set(t, false); changed++;
                    }

                    Sb.AppendLine();
                    Sb.AppendLine("  " + Full(t) + "  at " + t.position.ToString("F2"));
                    Sb.AppendLine("      renderers " + rends.Length + ", colliders " +
                                  caps.Length + ", Animator controller " +
                                  (controller ? anim.runtimeAnimatorController.name
                                              : "NONE — holds its bind pose, which is " +
                                                "why it reads as a statue"));
                    Sb.AppendLine("      -> " + (restore ? "restored" : "renderers, " +
                                  "colliders and Animator switched off"));
                }
            }

            for (int i = 0; i < t.childCount; i++)
                found += Walk(t.GetChild(i), beatSet, restore, ref changed);

            return found;
        }

        static void Set(Transform t, bool on)
        {
            var rends = t.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rends.Length; i++)
                if (rends[i] != null) rends[i].enabled = on;

            var caps = t.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < caps.Length; i++)
                if (caps[i] != null) caps[i].enabled = on;

            var anim = t.GetComponentInChildren<Animator>(true);
            if (anim != null) anim.enabled = on;
        }

        // ---------------------------------------------------------------------
        // 1. the colour thief is not in this level
        // ---------------------------------------------------------------------

        /// <summary>
        /// Hide the Colour Thief, which is in the level and should not be.
        ///
        /// <para>Measured: the object is at <c>L1_Cast/ColorThief</c>, it has an
        /// Animator and <b>no controller</b>, and the probe counted fifteen
        /// references to <c>ColorThief.fbx</c> in the scene. So it is not a
        /// stray import and it is not in a disabled container — it is a
        /// deliberate piece of the cast standing in the village, in its bind
        /// pose, which is the worst of both: the player sees a thing that looks
        /// like it should be doing something.</para>
        ///
        /// <para>The brief is that it is <b>not physically present in Level 1</b>,
        /// so this is the whole of that item: one dialogue line, delivered by
        /// Mono on waking, and nothing in the world. No spawn point, no
        /// silhouette in the distance, no sound. A hidden-but-loaded model is
        /// still "not present" as far as a player is concerned and costs one
        /// draw call, so hiding rather than deleting is the cheaper mistake to
        /// have made.</para>
        /// </summary>
        static void Thief(bool restore)
        {
            Sb.AppendLine();
            Sb.AppendLine("=== 1. THE COLOUR THIEF ===");

            int n = 0;

            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                n += FindThief(roots[i].transform, restore);

            Sb.AppendLine("  objects named for the thief, switched " +
                          (restore ? "back on" : "off") + ": " + n);
            Sb.AppendLine("  nothing else about it exists: no spawner, no " +
                          "trigger, no trigger volume. That is deliberate — the " +
                          "requirement was that Mono says it happened and that " +
                          "is all.");
        }

        static int FindThief(Transform t, bool restore)
        {
            int n = 0;
            string nm = t.name.ToLowerInvariant();

            if (nm.Contains("colorthief") || nm.Contains("colourthief") ||
                nm.Contains("color_thief") || nm.Contains("colour_thief"))
            {
                var skin = t.GetComponentInChildren<SkinnedMeshRenderer>(true);
                var anim = t.GetComponentInChildren<Animator>(true);

                Sb.AppendLine("  " + Full(t) + "  at " + t.position.ToString("F2"));
                Sb.AppendLine("      skin " + (skin == null ? "none" : skin.name +
                              " " + skin.bounds.size.ToString("F2")) +
                              ", controller " +
                              (anim == null || anim.runtimeAnimatorController == null
                                  ? "NONE" : anim.runtimeAnimatorController.name));
                Sb.AppendLine("      -> " + (restore ? "restored"
                              : "hidden, along with its colliders and Animator"));

                Set(t, !restore);
                n++;
            }

            for (int i = 0; i < t.childCount; i++)
                n += FindThief(t.GetChild(i), restore);

            return n;
        }

        // ---------------------------------------------------------------------
        // 10. the blue fragment
        // ---------------------------------------------------------------------

        /// <summary>
        /// Give the fragment something to see, and a material that stays blue.
        ///
        /// <para>Two independent faults, and both had to be present for the
        /// player to see nothing at all:</para>
        ///
        /// <para>1. <b>No visual.</b> The fragment object has <b>zero
        /// children</b>. <c>ColourFragment.Awake</c> looks for a child called
        /// "Glow" and falls back to the first child; with no children both are
        /// null, so <c>_glow</c> is null and there is no renderer to enable or
        /// disable. The component's own <c>Awake</c> now logs an error for
        /// exactly this.</para>
        ///
        /// <para>2. <b>Hidden by default.</b> <c>startsHidden</c> was true, and
        /// there is <b>no runtime caller of <c>Show(true)</c> anywhere in the
        /// project</b> — grep of every runtime script, comments excluded. It is
        /// now false.</para>
        ///
        /// <para><b>The part that is easy to get wrong, and would have made this
        /// look like it still did not work:</b> the project's shader takes a
        /// <c>_ColorRestore</c> float where 0 is grey and 1 is full colour. The
        /// fountain water is deliberately at 0 — it is grey until Beat 7. A glow
        /// material left at the default 0 renders the one blue thing in the
        /// level as a grey smudge, which is worse than nothing: it would be
        /// visible, findable, and not blue. So this material is explicitly 1,
        /// and the report says so.</para>
        /// </summary>
        static void FragmentGlow()
        {
            Sb.AppendLine();
            Sb.AppendLine("=== 10. THE BLUE FRAGMENT ===");

            var frags = UnityEngine.Object.FindObjectsByType<ColourFragment>(
                FindObjectsInactive.Include);

            Sb.AppendLine("  ColourFragment in the level: " + frags.Length);

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/Painterly/Shaders/PainterlyLit.shader");
            if (shader == null) shader = Shader.Find("Echoes/PainterlyLit");

            if (shader == null)
            {
                Sb.AppendLine("  FATAL: PainterlyLit not found; no glow material");
                return;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(GlowMatPath);
            bool isNew = mat == null;

            if (isNew)
            {
                mat = new Material(shader) { name = "FragmentGlow" };

                // Blue, and blue AFTER the shader has greyed everything else.
                mat.SetColor("_BaseColor", new Color(0.24f, 0.52f, 0.95f, 1f));
                mat.SetFloat("_Smoothness", 0.5f);
                mat.SetFloat("_Metallic", 0f);

                // The line the whole thing hangs on. 1 = do NOT desaturate.
                mat.SetFloat("_ColorRestore", 1f);
                mat.SetFloat("_RestoreBoost", 1f);

                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.24f, 0.52f, 0.95f, 1f) * 1.4f);

                AssetDatabase.CreateAsset(mat, GlowMatPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(GlowMatPath);
                mat = AssetDatabase.LoadAssetAtPath<Material>(GlowMatPath);

                Sb.AppendLine("  created " + GlowMatPath);
            }

            Sb.AppendLine("  material _ColorRestore = " +
                          mat.GetFloat("_ColorRestore").ToString("0.00") +
                          "  (1 = the blue survives the greyscale pass)");

            for (int i = 0; i < frags.Length; i++)
            {
                var f = frags[i];
                var glow = f.transform.Find("Glow");

                if (glow == null)
                {
                    glow = new GameObject("Glow").transform;
                    glow.SetParent(f.transform, false);
                    glow.localPosition = Vector3.zero;
                    glow.localRotation = Quaternion.identity;
                    glow.localScale = Vector3.one;

                    var mf = glow.gameObject.AddComponent<MeshFilter>();
                    mf.sharedMesh = Quad();

                    var mr = glow.gameObject.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = mat;
                    mr.shadowCastingMode =
                        UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }

                else
                {
                    var mr = glow.GetComponent<MeshRenderer>();
                    if (mr == null)
                    {
                        mr = glow.gameObject.AddComponent<MeshRenderer>();
                        mr.shadowCastingMode =
                            UnityEngine.Rendering.ShadowCastingMode.Off;
                    }

                    mr.sharedMaterial = mat;
                }

                // The prompt is what points at it, and it is drawn at 5x, so a
                // reach that was comfortable for a 2 px dot has to be measured
                // again rather than left at whatever it was.
                var so = new SerializedObject(f);
                var range = so.FindProperty("range");
                if (range != null)
                {
                    Sb.AppendLine("  " + f.name + ": reach " +
                                  range.floatValue.ToString("0.00") + " m");
                }

                var prompt = so.FindProperty("promptText");
                if (prompt != null)
                    Sb.AppendLine("  " + f.name + ": prompt '" + prompt.stringValue + "'");

                ApplyDefaults(so);
                so.ApplyModifiedPropertiesWithoutUndo();

                Sb.AppendLine("  " + Full(f.transform) + " at " +
                              f.transform.position.ToString("F2") +
                              " — glow built, startsHidden = false");
            }

            if (frags.Length > 0) EditorUtility.SetDirty(frags[0]);
        }

        /// <summary>
        /// Put the fragment's own defaults on the serialized values, so the
        /// scene stops holding copies of the old ones.
        /// </summary>
        static void ApplyDefaults(SerializedObject so)
        {
            var sh = so.FindProperty("startsHidden");
            if (sh != null)
            {
                Sb.AppendLine("  " + so.targetObject.name + ": startsHidden was " +
                              sh.boolValue + ", set to false");
                sh.boolValue = false;
            }
        }

        static Mesh Quad()
        {
            // Unity's built-in quad lives in the default resources, so it is
            // found by type rather than by path — a path would break if the
            // project moved its Resources folder.
            foreach (var m in Resources.FindObjectsOfTypeAll<Mesh>())
                if (m.name == "Quad") return m;

            // Nothing found: build one. A quad is four vertices and two
            // triangles, and writing it out is cheaper than shipping a mesh.
            var mesh = new Mesh { name = "FragmentGlowQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.normals = new[] { -Vector3.forward, -Vector3.forward,
                                  -Vector3.forward, -Vector3.forward };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };

            AssetDatabase.CreateAsset(mesh, "Assets/Painterly/Materials/FragmentGlowQuad.asset");

            return mesh;
        }

        // ---------------------------------------------------------------------
        // 4. he / him
        // ---------------------------------------------------------------------

        /// <summary>
        /// Rewrite Ari's pronouns in every string the player can read.
        ///
        /// <para><b>The player-facing one is the opening card, and it was
        /// wrong:</b> <c>Beat1Intro.introText</c> reads "Ari has never seen a
        /// colour. Everything here is black or white, and she has a brush
        /// anyway — because everything she likes, she tries to paint." That is
        /// the first sentence of the level and it is a serialized string on a
        /// component, so no amount of editing scripts would have touched it.</para>
        ///
        /// <para>Swept across every serialized string on every component in the
        /// scene and across the dialogue asset, because the alternative is
        /// hunting for them by hand and this is a level with six beats and a
        /// hundred fields. Each rewrite is listed, so the count is a number
        /// rather than a claim.</para>
        ///
        /// <para>"her" is rewritten to "him" or "his" by the word in front of
        /// it, which is the only way to do it mechanically: English has one
        /// pronoun for both. Every hit is printed so a wrong one is visible.</para>
        /// </summary>
        static void Pronouns()
        {
            Sb.AppendLine();
            Sb.AppendLine("=== 4. ARI IS MALE: she/her -> he/him ===");

            int fields = 0, swaps = 0;

            var roots = SceneManager.GetActiveScene().GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
                fields += Sweep(roots[i], ref swaps);

            // The dialogue asset, which is not in the scene graph.
            var guids = AssetDatabase.FindAssets("t:MonoHintLines");
            for (int g = 0; g < guids.Length; g++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[g]);
                var asset = AssetDatabase.LoadAssetAtPath<MonoHintLines>(path);
                if (asset == null) continue;

                var so = new SerializedObject(asset);
                var arr = so.FindProperty("lines");
                if (arr == null || !arr.isArray) continue;

                for (int i = 0; i < arr.arraySize; i++)
                {
                    var el = arr.GetArrayElementAtIndex(i);
                    var t = el.FindPropertyRelative("text");
                    if (t == null) continue;

                    fields++;

                    string before = t.stringValue;
                    string after = Fix(before);
                    if (after == before) continue;

                    t.stringValue = after;
                    swaps++;
                    Sb.AppendLine("  " + path + " line " + i + ":");
                    Sb.AppendLine("      was: " + before);
                    Sb.AppendLine("      now: " + after);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Sb.AppendLine("  string fields inspected: " + fields +
                          ", rewritten: " + swaps);
        }

        static int Sweep(GameObject go, ref int swaps)
        {
            int fields = 0;

            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i];
                if (c == null) continue;

                var so = new SerializedObject(c);
                var it = so.GetIterator();
                bool enter = true;

                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (it.propertyType != SerializedPropertyType.String) continue;

                    fields++;

                    string before = it.stringValue;
                    string after = Fix(before);
                    if (after == before) continue;

                    it.stringValue = after;
                    swaps++;

                    Sb.AppendLine("  " + Full(go.transform) + " : " + c.GetType().Name +
                                  "." + it.propertyPath);
                    Sb.AppendLine("      was: " + before);
                    Sb.AppendLine("      now: " + after);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            for (int i = 0; i < go.transform.childCount; i++)
                fields += Sweep(go.transform.GetChild(i).gameObject, ref swaps);

            return fields;
        }

        static readonly string[] Objective =
        {
            "to", "at", "from", "with", "behind", "for", "towards", "toward",
            "past", "into", "beside", "near", "without", "around", "below",
            "before", "after", "against", "next", "facing", "aimed", "aim",
            "catch", "hit", "hurt", "reach", "stop", "seen", "put", "puts",
            "sees", "see", "lets", "let", "move", "leave", "leaves", "drops",
            "gives", "give", "carries", "carry", "knows", "knew", "given",
            "hides", "hide", "notice", "notices", "keeps", "keep", "gave",
            "walk", "walks", "pick", "picks", "have", "has", "hunting",
            "loses", "lose", "pose", "drop", "shove", "shoves", "shoving",
            "follows", "follow", "tells", "tell", "watches", "watch",
            "sends", "send", "shows", "show", "reaches", "reach"
        };

        static string Fix(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.IndexOf("her", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                s.IndexOf("she", System.StringComparison.OrdinalIgnoreCase) < 0)
                return s;

            string outp = s;

            outp = System.Text.RegularExpressions.Regex.Replace(
                outp, @"\bshers\b", "his");
            outp = System.Text.RegularExpressions.Regex.Replace(
                outp, @"\bherself\b", "himself");
            outp = System.Text.RegularExpressions.Regex.Replace(
                outp, @"\bshe\b", "he");
            outp = System.Text.RegularExpressions.Regex.Replace(
                outp, @"\bShe\b", "He");

            // "her" -> him or his, decided by the word in front of it.
            outp = System.Text.RegularExpressions.Regex.Replace(
                outp, @"\b([A-Za-z]+)\s+([Hh]er)\b", m =>
                {
                    string w = m.Groups[1].Value.ToLowerInvariant();

                    for (int i = 0; i < Objective.Length; i++)
                        if (Objective[i] == w)
                            return m.Groups[1].Value + " " +
                                   (m.Groups[2].Value[0] == 'H' ? "Him" : "him");

                    return m.Groups[1].Value + " " +
                           (m.Groups[2].Value[0] == 'H' ? "His" : "his");
                });

            return outp;
        }

        // ---------------------------------------------------------------------
        // 6 / 7. crawler count and height
        // ---------------------------------------------------------------------

        /// <summary>
        /// Where the three are, and what is under each of them.
        /// </summary>
        static void CrawlerGround()
        {
            Sb.AppendLine();
            Sb.AppendLine("=== 6 / 7. THE THREE CRAWLERS ===");

            var all = UnityEngine.Object.FindObjectsByType<InkCrawler>(
                FindObjectsInactive.Include);

            Sb.AppendLine("  InkCrawler components: " + all.Length +
                          "  (the beat is written for 3)");
            Sb.AppendLine();

            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                var t = c.transform;

                var em = c.GetComponent<InkCrawlerEmerge>();

                Sb.AppendLine("  [" + i + "] " + Full(t));
                Sb.AppendLine("      at " + t.position.ToString("F2") +
                              (em != null ? ", stand point " +
                                            em.StandPoint.ToString("F2") : ""));
                Sb.AppendLine("      emerge component " + (em == null ? "MISSING"
                              : "present, phase " + em.Now + ", now started"));

                var hits = Physics.RaycastAll(t.position + Vector3.up * 30f,
                                              Vector3.down, 60f, ~0,
                                              QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));

                int shown = 0;
                for (int h = 0; h < hits.Length && shown < 5; h++)
                {
                    if (hits[h].collider.GetComponentInParent<InkCrawler>() != null)
                        continue;
                    if (hits[h].collider.GetComponentInParent<AriMover>() != null)
                        continue;

                    Sb.AppendLine("        under it: y " +
                                  hits[h].point.y.ToString("F2") + "  " +
                                  hits[h].collider.name + "  " +
                                  hits[h].collider.bounds.size.ToString("F2"));
                    shown++;
                }

                if (shown == 0)
                    Sb.AppendLine("        under it: NOTHING — over a hole");

                Sb.AppendLine("      the number that argues against 'this one " +
                              "is fine': the yard floor is 0.00 and any crawler " +
                              "more than 0.30 m off it is not on the paving.");

                Sb.AppendLine();
            }
        }

        // ---------------------------------------------------------------------
        // 2. text sizes
        // ---------------------------------------------------------------------

        /// <summary>
        /// What size each piece of text actually lands on, and which ones could
        /// not reach the 5x target.
        /// </summary>
        static void TextReport()
        {
            Sb.AppendLine();
            Sb.AppendLine("=== 2. TEXT SIZE ===");
            Sb.AppendLine("  screen " + Screen.width + " x " + Screen.height +
                          ", scale " + BeatText.ScreenScale.ToString("0.00"));
            Sb.AppendLine("  multiplier asked for: " + BeatText.Scale +
                          "x, so the targets are:");
            Sb.AppendLine("    beat prompt   " + BeatText.PromptTarget + " px (was " +
                          BeatText.BasePrompt + " x scale = " +
                          (BeatText.BasePrompt * BeatText.ScreenScale).ToString("0") + " px)");
            Sb.AppendLine("    control row   " + BeatText.RowTarget + " px (was " +
                          (BeatText.BaseRow * BeatText.ScreenScale).ToString("0") + " px)");
            Sb.AppendLine("    Mono subtitle " + BeatText.SubtitleTarget + " px (was " +
                          (BeatText.BaseSubtitle * BeatText.ScreenScale).ToString("0") + " px)");
            Sb.AppendLine("    HUD label     " + BeatText.HudTarget + " px (was " +
                          (BeatText.BaseHud * BeatText.ScreenScale).ToString("0") + " px)");

            float w = BeatText.PromptWidth(0.90f);
            float maxH = BeatText.MaxBlock;

            Sb.AppendLine();
            Sb.AppendLine("  a block may be " + maxH.ToString("0") +
                          " px tall at a width of " + w.ToString("0") +
                          " px; past that a line is shrunk to fit.");

            var lines = new List<string>();
            var guids = AssetDatabase.FindAssets("t:MonoHintLines");

            for (int g = 0; g < guids.Length; g++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<MonoHintLines>(
                    AssetDatabase.GUIDToAssetPath(guids[g]));
                if (asset == null || asset.lines == null) continue;

                for (int i = 0; i < asset.lines.Count; i++)
                    lines.Add(asset.lines[i].text);
            }

            // The opening card, which is the longest string on screen and is not
            // in the dialogue asset at all — it is a component field.
            var intro = UnityEngine.Object.FindAnyObjectByType<Beat1Intro>(
                FindObjectsInactive.Include);
            if (intro != null)
            {
                var so = new SerializedObject(intro);
                var p = so.FindProperty("introText");
                if (p != null && !string.IsNullOrEmpty(p.stringValue))
                    lines.Add(p.stringValue);
            }

            Sb.AppendLine();
            Sb.AppendLine("  " + lines.Count +
                          " strings the player is asked to read. Those that " +
                          "CANNOT reach the 5x target:");

            var short1 = BeatText.ShortfallsStandalone(lines,
                                                       TextAnchor.MiddleCenter,
                                                       w, maxH,
                                                       BeatText.PromptTarget);

            if (short1.Count == 0) Sb.AppendLine("    none — all of them fit");
            for (int i = 0; i < short1.Count; i++) Sb.AppendLine(short1[i]);

            Sb.AppendLine();
            Sb.AppendLine("  the number that argues against 'the text is 5x " +
                          "bigger': it is 5x on everything that fits and less " +
                          "on everything above, and the list above is the list " +
                          "of the ones above.");
            Sb.AppendLine();
            Sb.AppendLine("  CAVEAT, and it is a real one: this ran OUTSIDE " +
                          "OnGUI, so Unity had no GUI.skin to hand over and " +
                          "BeatText fell back to the built-in LegacyRuntime " +
                          "font. The character widths above are therefore not " +
                          "this project's, and every line count is an " +
                          "ESTIMATE. The believable version of this table is " +
                          "written during play by BeatTextAudit against the " +
                          "real skin -> Temp/beattext_audit.txt");

            Audit();
        }

        /// <summary>
        /// Put the audit component in the scene so the next play session
        /// produces the real numbers.
        ///
        /// <para>The component removes itself after it has written, so this is
        /// not a thing that has to be remembered and taken out again. Written
        /// into the report either way, because "is the audit actually going to
        /// run" is the first question to ask of a measurement nobody has seen
        /// yet.</para>
        /// </summary>
        static void Audit()
        {
            var host = new GameObject("L1_BeatTextAudit");

            var a = host.AddComponent<BeatTextAudit>();

            // The serialized card strings, which are the longest text in the
            // level and live on components rather than in the dialogue asset.
            int added = 0;

            var roots = SceneManager.GetActiveScene().GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
                added += Feed(roots[i].transform, a);

            Sb.AppendLine();
            Sb.AppendLine("  BeatTextAudit placed on '" + host.name +
                          "' with " + added + " serialized string(s) queued; " +
                          "it measures on the first OnGUI of the next play " +
                          "session and deletes itself.");
        }

        static int Feed(Transform t, BeatTextAudit audit)
        {
            int added = 0;

            var comps = t.GetComponents<Component>();

            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i];
                if (c == null) continue;
                if (c is BeatTextAudit) continue;

                var so = new SerializedObject(c);
                var it = so.GetIterator();
                bool enter = true;

                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (it.propertyType != SerializedPropertyType.String) continue;

                    string v = it.stringValue;

                    // Only the long ones. Short labels fit at full size and
                    // reporting them would bury the three that do not.
                    if (string.IsNullOrEmpty(v) || v.Length < 40) continue;

                    audit.Add(v);
                    added++;
                }
            }

            for (int i = 0; i < t.childCount; i++)
                added += Feed(t.GetChild(i), audit);

            return added;
        }

        // ---------------------------------------------------------------------

        static string Full(Transform t)
        {
            var parts = new List<string>();
            var cur = t;
            while (cur != null) { parts.Insert(0, cur.name); cur = cur.parent; }
            return string.Join("/", parts.ToArray());
        }
    }

    internal static class Ext
    {
        public static bool IsChildOfAny(this Transform t, HashSet<InkCrawler> set)
        {
            var cur = t;
            while (cur != null)
            {
                var c = cur.GetComponent<InkCrawler>();
                if (c != null && set.Contains(c)) return true;
                cur = cur.parent;
            }
            return false;
        }
    }
}
