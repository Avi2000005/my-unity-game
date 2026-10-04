using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// What is actually in the scene for the four things that were asked about:
    /// the crawler's skin and body, the tree's colour, and Mono's size.
    ///
    /// <para>This is a measurement, not a builder. Each of those four is a
    /// question about the current state of the hierarchy — what child holds the
    /// crawler's renderer, what Mono's real height in metres is, whether the
    /// tree's restore targets are at 0 right now — and every one of them has
    /// been guessed wrong at least once already. A tool that answers them by
    /// reading the scene costs one round trip; guessing costs a rewrite.</para>
    ///
    /// <para><b>Mono's height is measured by baking his skin</b>, the same way
    /// <c>AriFit</c> and <c>Beat5Setup.AriPosedHeight</c> do it, rather than
    /// from <c>SkinnedMeshRenderer.bounds</c>. Those bounds span every pose the
    /// animation reaches, so they always over-report. Reading them here and
    /// then scaling Mono to hit a chest height would overshoot.</para>
    /// </summary>
    public static class L1CastProbe
    {
        const string Report = "Temp/l1_cast_probe.txt";

        [MenuItem("Tools/Echoes/Probe L1 Cast", priority = 96)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] cast probe");
            sb.AppendLine("ground check — every number below is read off the " +
                          "hierarchy, none is a design constant");

            Crawlers(sb);
            Mono(sb);
            Tree(sb);
            Ari(sb);

            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report),
                               sb.ToString());
            Debug.Log("[Echoes] cast probe written to " + Report);
        }

        // --- 1. the crawler: skin and body -----------------------------------

        static void Crawlers(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== INK CRAWLERS ===");

            var all = UnityEngine.Object.FindObjectsByType<InkCrawler>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            sb.AppendLine("found " + all.Length + " InkCrawler component(s)");

            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i];
                var go = c.gameObject;

                sb.AppendLine();
                sb.AppendLine("-- " + go.name + "  at " + go.transform.position.ToString("F2") +
                              "  active=" + go.activeSelf);
                sb.AppendLine("   parent      : " + (go.transform.parent == null
                    ? "(none — a root)" : go.transform.parent.name));
                sb.AppendLine("   localScale  : " + go.transform.localScale.ToString("F4") +
                              "   localPos    : " + go.transform.localPosition.ToString("F3"));
                sb.AppendLine("   declared    : bodyHeight " + c.BodyHeight.ToString("F2") +
                              " m, splashRadius " + c.SplashRadius.ToString("F2") +
                              ", notice " + c.NoticeRadius.ToString("F1"));

                var col = go.GetComponent<Collider>();
                sb.AppendLine("   collider    : " + Describe(col));

                // The renderer hunt. This is the question that matters: is the
                // skin on a child, on a grandchild, or not present at all.
                var rends = go.GetComponentsInChildren<Renderer>(true);
                sb.AppendLine("   renderers   : " + rends.Length);

                if (rends.Length == 0)
                {
                    sb.AppendLine("      ^ NO RENDERER ANYWHERE UNDER THIS OBJECT. Nothing " +
                                  "is drawn for this crawler — which is a different " +
                                  "problem from an untextured one, and the fix is not " +
                                  "a texture.");
                }

                for (int r = 0; r < rends.Length; r++)
                {
                    var rd = rends[r];
                    sb.AppendLine("      [" + r + "] " + rd.GetType().Name + " '" + rd.name +
                                  "' enabled=" + rd.enabled +
                                  " path=" + PathOf(go.transform, rd.transform));

                    var smr = rd as SkinnedMeshRenderer;
                    if (smr != null)
                    {
                        sb.AppendLine("          skinned, blendShapeCount " + smr.sharedMesh.blendShapeCount);
                    }

                    var mf = rd.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        sb.AppendLine("          mesh       : '" + mf.sharedMesh.name +
                                      "' " + mf.sharedMesh.vertexCount + " verts, " +
                                      "bounds " + mf.sharedMesh.bounds.size.ToString("F3"));
                        sb.AppendLine("          uv0        : " +
                                      (mf.sharedMesh.uv.Length > 0
                                        ? mf.sharedMesh.uv.Length + " coords"
                                        : "NONE — a texture assigned here would sample " +
                                          "nothing and the mesh would render as flat colour"));
                    }

                    var mats = rd.sharedMaterials;
                    var slots = mats == null ? 0 : mats.Length;
                    sb.AppendLine("          materials  : " + slots);

                    for (int m = 0; m < slots; m++)
                    {
                        var mat = mats[m];
                        if (mat == null)
                        {
                            sb.AppendLine("             [" + m + "] NULL — this slot draws pink");
                            continue;
                        }

                        sb.AppendLine("             [" + m + "] '" + mat.name + "' shader=" +
                                      (mat.shader != null ? mat.shader.name : "NULL SHADER") +
                                      " path=" + AssetDatabase.GetAssetPath(mat));

                        if (!mat.HasProperty("_BaseMap")) continue;

                        var tex = mat.GetTexture("_BaseMap") as Texture2D;
                        sb.AppendLine("                   _BaseMap = " +
                                      (tex == null
                                        ? "NONE  <-- the flat look"
                                        : tex.name + " " + tex.width + "x" + tex.height +
                                          " @" + AssetDatabase.GetAssetPath(tex)));
                    }

                    sb.AppendLine("          worldBounds: " + rd.bounds.size.ToString("F3") +
                                  " at " + rd.bounds.center.ToString("F2"));
                }

                // Kids, so the emerge animation knows what to sink.
                var kids = go.transform.childCount;
                sb.AppendLine("   children    : " + kids);
                for (int k = 0; k < kids; k++)
                {
                    var kid = go.transform.GetChild(k);
                    int kr = kid.GetComponentsInChildren<Renderer>(true).Length;
                    sb.AppendLine("      " + kid.name + " (" + kid.GetType().Name +
                                  ") localPos " + kid.localPosition.ToString("F3") +
                                  " localScale " + kid.localScale.ToString("F3") +
                                  " renderers=" + kr + " active=" + kid.gameObject.activeSelf);
                }
            }
        }

        // --- 5. Mono: how big, and is he off the floor -----------------------

        static void Mono(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== MONO ===");

            var all = UnityEngine.Object.FindObjectsByType<MonoCompanion>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            sb.AppendLine("found " + all.Length);

            for (int i = 0; i < all.Length; i++)
            {
                var m = all[i];
                var go = m.gameObject;
                var t = go.transform;

                sb.AppendLine();
                sb.AppendLine("-- " + go.name + " at " + t.position.ToString("F3") +
                              " active=" + go.activeSelf);
                sb.AppendLine("   localScale  : " + t.localScale.ToString("F4") +
                              "   localScale3 : " + t.lossyScale.ToString("F4"));
                sb.AppendLine("   localPos    : " + t.localPosition.ToString("F3"));
                sb.AppendLine("   rotation    : " + t.eulerAngles.ToString("F1"));

                var rends = go.GetComponentsInChildren<Renderer>(true);
                sb.AppendLine("   renderers   : " + rends.Length);

                Bounds allBounds = new Bounds();
                bool first = true;
                for (int r = 0; r < rends.Length; r++)
                {
                    var rd = rends[r];
                    var smr = rd as SkinnedMeshRenderer;

                    sb.AppendLine("      " + rd.GetType().Name + " '" + rd.name +
                                  " enabled=" + rd.enabled);

                    if (smr != null)
                    {
                        sb.AppendLine("         animationBounds " +
                                      smr.localBounds.size.ToString("F3") +
                                      "  <- every pose it reaches, NOT its current height");

                        // Through SkinHeight, not a local bake. SkinHeight is the
                        // one implementation of "how tall is this"; a second copy
                        // in a probe is how two tools end up reporting two
                        // different heights for the same character.
                        float h = SkinHeight.Measure(rd, out var feet, out var how);

                        sb.AppendLine("         measured    : " + (h > 0.0001f
                            ? h.ToString("F3") + " m tall, feet at y " +
                              feet.y.ToString("F3") + "  (" + how + ")"
                            : "COULD NOT MEASURE — " + how));
                    }
                    else
                    {
                        var mf = rd.GetComponent<MeshFilter>();
                        sb.AppendLine("         meshRenderer bounds " + rd.bounds.size.ToString("F3") +
                                      (mf != null && mf.sharedMesh != null
                                        ? " (mesh " + mf.sharedMesh.name + ")"
                                        : ""));
                    }

                    if (first) { allBounds = rd.bounds; first = false; }
                    else allBounds.Encapsulate(rd.bounds);
                }

                if (!first)
                {
                    sb.AppendLine("   MEASURED    : Mono stands " +
                                  allBounds.size.y.ToString("F3") + " m tall, bottom at y " +
                                  allBounds.min.y.ToString("F3"));
                    sb.AppendLine("   his root y  : " + t.position.y.ToString("F3") +
                                  "  -> bottom is " +
                                  (allBounds.min.y - t.position.y).ToString("F3") +
                                  " m " + (allBounds.min.y > t.position.y + 0.02f
                                     ? "ABOVE his root, so he is hovering"
                                     : "at or below his root, so he is on the floor"));
                }

                var kids = t.childCount;
                sb.AppendLine("   children    : " + kids);
                for (int k = 0; k < kids; k++)
                {
                    var kid = t.GetChild(k);
                    sb.AppendLine("      " + kid.name + " (" + kid.GetType().Name +
                                  ") localPos " + kid.localPosition.ToString("F3") +
                                  " renderers=" + kid.GetComponentsInChildren<Renderer>(true).Length);
                }
            }

            // Ari, for the target height.
            sb.AppendLine();
            var ari = UnityEngine.Object.FindAnyObjectByType<AriMover>(
                FindObjectsInactive.Include);
            if (ari != null)
            {
                sb.AppendLine("Ari's bodyHeight  : " + ari.BodyHeight.ToString("F3") +
                              " m  (collider authority, unchanged)");
                sb.AppendLine("Ari's chest, as InkCrawler measures it : " +
                              (ari.BodyHeight * 0.6f).ToString("F3") + " m");
                sb.AppendLine("  ^ that is the number to size Mono to. It is the project's " +
                              "existing definition of HIS chest, so using it means " +
                              "two scripts cannot disagree about where HIS chest is.");
            }
        }

        // --- 3. the tree: is anything coloured yet ---------------------------

        static void Tree(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== SLEEPING TREE / COLOUR STATE ===");

            var tree = UnityEngine.Object.FindAnyObjectByType<SleepingTree>(
                FindObjectsInactive.Include);

            if (tree == null)
            {
                sb.AppendLine("no SleepingTree in the scene");
            }
            else
            {
                sb.AppendLine("tree " + tree.gameObject.name + " at " +
                              tree.transform.position.ToString("F2"));
                sb.AppendLine("  burstRadius : " + tree.BurstRadius.ToString("F2") + " m");
                sb.AppendLine("  awoken      : " + tree.IsAwoken);
                sb.AppendLine("  _point      : " + tree.TouchPoint.ToString("F2"));
            }

            var targets = UnityEngine.Object.FindObjectsByType<ColorRestoreTarget>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            sb.AppendLine();
            sb.AppendLine("ColorRestoreTarget components: " + targets.Length);

            float sum = 0f, mx = 0f;
            int coloured = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                sum += targets[i].Restore;
                if (targets[i].Restore > 0.001f) coloured++;
                if (targets[i].Restore > mx) mx = targets[i].Restore;
            }

            if (targets.Length > 0)
            {
                sb.AppendLine("  mean restore " + (sum / targets.Length).ToString("0.000") +
                              ", highest " + mx.ToString("0.000") +
                              ", coloured now " + coloured + " of " + targets.Length);
                sb.AppendLine("  " + (coloured == 0
                    ? "the whole village is grey, which is what Beat 7 needs to change"
                    : coloured + " target(s) are ALREADY partly coloured. The brief says " +
                      "no colour until the fountain, so something is restoring early " +
                      "— most likely BrushPainter.PaintAt, which calls " +
                      "RestoreInRadius(1f) on every stroke."));
            }

            // Anything already restored near the tree is the specific thing to
            // look at.
            sb.AppendLine();
            sb.AppendLine("targets within 15 m of the tree:");
            for (int i = 0; i < targets.Length; i++)
            {
                if (tree == null) break;
                float d = Vector3.Distance(targets[i].transform.position,
                                           tree.TouchPoint);
                if (d > 15f) continue;
                sb.AppendLine("   " + targets[i].name.PadRight(28) + " " +
                              d.ToString("F1") + " m  restore " +
                              targets[i].Restore.ToString("0.000"));
            }
        }

        // --- Ari, for reference ---------------------------------------------

        static void Ari(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=== ARI ===");
            var ari = UnityEngine.Object.FindAnyObjectByType<AriMover>(
                FindObjectsInactive.Include);
            if (ari == null) { sb.AppendLine("none in the scene"); return; }

            sb.AppendLine("at " + ari.transform.position.ToString("F3"));
            sb.AppendLine("  bodyHeight " + ari.BodyHeight.ToString("F3") +
                          "  bodyRadius " + ari.BodyRadius.ToString("F3"));
            sb.AppendLine("  localScale " + ari.transform.localScale.ToString("F4"));

            var hp = ari.GetComponent<AriHealth>();
            sb.AppendLine("  AriHealth   : " + (hp == null ? "NOT ON HIM — beats 5-7 have no health"
                                                         : "present, at " + hp.Fraction.ToString("0.00")));

            var inter = ari.GetComponent<AriInteract>();
            sb.AppendLine("  AriInteract : " + (inter == null
                ? "NOT ON HIM — there is no E button in the level yet"
                : "present"));
        }

        // --- helpers ----------------------------------------------------------

        /// <summary>
        /// Describe a collider.
        ///
        /// <para><c>Collider</c> itself has no <c>center</c> — that lives on the
        /// three shapes that have one. Asking the base type for it is a compile
        /// error, and reading only <c>bounds</c> instead would lose the thing
        /// this report is for, which is whether the capsule is centred on the
        /// root as Beat5Setup claims.</para>
        /// </summary>
        static string Describe(Collider c)
        {
            if (c == null) return "NONE";

            string s = c.GetType().Name +
                       " size=" + c.bounds.size.ToString("F2") +
                       " enabled=" + c.enabled +
                       " trigger=" + c.isTrigger;

            // Reported for each shape rather than through the base type, and the
            // "no centre" case is stated rather than left blank — a blank reads
            // as a measurement that was not taken.
            if (c is BoxCollider b)
                return s + " centre=" + b.center.ToString("F2");
            if (c is SphereCollider sp)
                return s + " centre=" + sp.center.ToString("F2") +
                       " radius=" + sp.radius.ToString("F2");
            if (c is CapsuleCollider cap)
                return s + " centre=" + cap.center.ToString("F2") +
                       " r=" + cap.radius.ToString("F2") +
                       " h=" + cap.height.ToString("F2") +
                       " dir=" + cap.direction;

            return s + " centre=not on this collider type";
        }

        static string PathOf(Transform from, Transform to)
        {
            if (from == to) return ".";
            var parts = new System.Collections.Generic.List<string>();
            var cur = to;
            while (cur != null && cur != from)
            {
                parts.Add(cur.name);
                cur = cur.parent;
            }
            if (cur == null) return "(not under " + from.name + ")";
            parts.Reverse();
            return from.name + "/" + string.Join("/", parts.ToArray());
        }
    }
}