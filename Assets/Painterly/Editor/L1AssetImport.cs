using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Brings the Level 1 cast in from the loose folder on disk.
    ///
    /// Three things about these files are known facts now, all of them measured
    /// rather than assumed, and the tool is built around them.
    ///
    /// First: the names cannot be trusted. The Ink Crawler is spelled four ways
    /// across seven animation files ("Ink crawller", "Ink crwaller", "Ink
    /// Crawler", "Ink_crwaller") and they are all the same enemy, so the same
    /// animation imported under two of those names lands in the project as two
    /// clips called "mixamo.com" -- and Animator.StringToHash resolves a state
    /// to whichever loaded first, which makes a controller look like it is
    /// ignoring an animation with nothing wrong in it. So nothing is imported
    /// under the name it arrives with.
    ///
    /// Second: every file carries two takes. One is the real animation, called
    /// "mixamo.com", and the other is a 1.03s walk cycle called
    /// "Armature|Armature|walking_man|baselayer" that Mixamo bakes into every
    /// export it makes. Mono's walk is measured at 1.033s, which is the same
    /// walk, so dropping the baselayer is not a judgement call -- leaving it in
    /// would put a second identical walk in the project with nothing to tell
    /// the two apart in the inspector.
    ///
    /// Third: the asset database is not ready when SaveAndReimport returns.
    /// Reading the importer or the sub-assets straight afterwards reports a
    /// null importer and an invalid avatar for files that are, in fact, fine
    /// -- which is what an earlier pass of this tool did, and it cost a full
    /// diagnostic to tell the difference between a broken import and a
    /// premature read. Every read here goes through Settle first.
    /// </summary>
    public static class L1AssetImport
    {
        /// <summary>
        /// Where the loose files are. This has been given three different
        /// spellings so far and the one with an "11" on the end does not
        /// exist, so the folder is found by listing the parent directory rather
        /// than by trusting a typed path.
        /// </summary>
        const string SourceFolderHint = "painter_game_assests";

        const string DestDir = "Assets/Art/L1";

        const string Report = "Temp/l1_import.txt";

        const string Log = "Temp/l1_import_log.txt";

        /// <summary>
        /// How long one call may work before it writes its report and returns.
        ///
        /// The bridge caps a single call at sixty seconds, and killing one
        /// mid-reimport leaves the next file with a .meta describing settings
        /// its asset does not have. So the tool stops on its own, between
        /// files, while there is still time to write the report -- and because
        /// every file is driven to the same settings on every run, running it
        /// again simply picks up where it stopped.
        /// </summary>
        const double BudgetSeconds = 34.0;

        /// <summary>
        /// One line per file.
        ///
        /// Source and Dest are the rename: Dest is what the file is called
        /// inside the project and the clip inside it takes its name from Dest.
        /// Group is the character it belongs to, or null when the file carries
        /// only a model. Loop is measured per clip rather than guessed from the
        /// name, because a cycle that is set not to loop plays once and holds
        /// the last pose, while a one-shot that is set to loop snaps back to
        /// its crouch forever -- and those two failures look nothing alike.
        /// </summary>
        static readonly (string Source, string Dest, string Group, bool Loop)[] Files =
        {
            // --- Mono. Four files, one creature, spelled three ways on disk.
            ("mono character.fbx",        "Mono.fbx",      null, false),
            ("Mono Idle.fbx",             "Mono_Idle.fbx", "Mono", true),
            ("Mono Walking.fbx",          "Mono_Walk.fbx", "Mono", true),
            ("mono_talking_to_ari.fbx",   "Mono_Talk.fbx", "Mono", false),
            ("Mono_waking_up.fbx",        "Mono_Wake.fbx", "Mono", false),

            // --- The Ink Crawler: four spellings, one enemy. The two idles
            // keep the A/B naming out of it on purpose -- renaming the files
            // would leave the originals in the project as orphans still
            // carrying unconfigured clips called "mixamo.com", and an orphan
            // clip is exactly the thing that makes Animator.StringToHash
            // ambiguous later.
            ("ink_crwaller_character.fbx",               "InkCrawler.fbx",         null, false),
            ("Ink Crwaller Zombie Idle.fbx",             "InkCrawler_Idle.fbx",    "Crawler", true),
            ("Ink_crwaller_Idle.fbx",                    "InkCrawler_Idle2.fbx",   "Crawler", true),
            ("Ink crawller Running Crawl.fbx",           "InkCrawler_Crawl.fbx",   "Crawler", true),
            ("Ink crawller Running.fbx",                 "InkCrawler_Run.fbx",     "Crawler", true),
            ("Ink crawller Swagger Walk.fbx",            "InkCrawler_Walk.fbx",    "Crawler", true),
            ("Ink crawller Sword And Shield Attack.fbx", "InkCrawler_Attack.fbx",  "Crawler", false),
            ("Ink Crawller Standing Death Forward 01.fbx","InkCrawler_Death.fbx",  "Crawler", false),

            // --- The Color Thief. Model only. In Level 1 it is a silhouette on
            // a hill and never animates inside the village, so importing takes
            // for it would add seven orphan clips to the project.
            ("villian_character.fbx",                    "ColorThief.fbx",         null, false),

            // --- Ari's remaining clips. Her Idle, Walk and Jump are already
            // configured by AriImportSetup, which is not touched from here, so
            // these are purely additive.
            ("Ari_Running.fbx",              "Ari_Run.fbx",         "Ari", true),
            ("ari_Talking.fbx",              "Ari_Talk.fbx",        "Ari", true),
            ("ari_attack.fbx",               "Ari_Attack.fbx",      "Ari", false),
            ("ari on hit reaction.fbx",      "Ari_HitReact.fbx",    "Ari", false),
            ("Ari Magic Attack on enemy.fbx","Ari_MagicAttack.fbx", "Ari", false),
            ("Ari collecting fragments.fbx", "Ari_Collect.fbx",     "Ari", false),
            ("Ari Death.fbx",                "Ari_Death.fbx",       "Ari", false)
        };

        [MenuItem("Tools/Echoes/Import Level 1 Cast", priority = 61)]
        public static void Run()
        {
            var sb = new StringBuilder();
            var reportPath = Path.Combine(Directory.GetCurrentDirectory(), Report);
            if (File.Exists(reportPath)) File.Delete(reportPath);

            string source = ResolveSourceFolder(sb);
            if (source == null)
            {
                Finish(sb, "could not find the asset folder");
                return;
            }

            sb.AppendLine($"source: {source}");
            sb.AppendLine($"dest:   {DestDir}");

            Directory.CreateDirectory(DestDir);

            var started = System.Diagnostics.Stopwatch.StartNew();
            int changed = 0, untouched = 0, problems = 0;

            foreach (var (src, dest, group, loop) in Files)
            {
                // A fifty-megabyte humanoid reimport takes seconds on its own,
                // so the budget is checked here rather than at the end of a
                // batch. Running out of time halfway through a reimport is
                // exactly what leaves a .meta describing an asset that is not
                // there.
                if (changed > 0 && started.Elapsed.TotalSeconds > BudgetSeconds)
                {
                    sb.AppendLine($"-- stopped on the clock after {changed} change(s); " +
                                  "run again to continue");
                    break;
                }

                string from = Path.Combine(source, src);
                string to = $"{DestDir}/{dest}";
                // The label is the name without the extension, so the report
                // reads "Mono_Idle" rather than "Mono_Idle.fbx". The file on
                // disk is still the .fbx; only the reporting is shortened.
                var label = Path.GetFileNameWithoutExtension(dest);

                if (!File.Exists(from)) { sb.AppendLine($"MISSING  {src}"); problems++; continue; }

                if (CopyIfDifferent(from, to))
                {
                    sb.AppendLine($"{label}: copied, importing for the first time");
                    AssetDatabase.ImportAsset(to,
                        ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }

                if (group == null) { DoModel(to, label, ref changed, ref untouched, ref problems, sb); }
                else { DoAnim(to, label, group, loop, ref changed, ref untouched, ref problems, sb); }
            }

            sb.AppendLine($"-- {changed} changed, {untouched} already correct, " +
                          $"{problems} problem(s), {started.Elapsed.TotalSeconds:0.0}s");

            AssetDatabase.SaveAssets();
            Finish(sb, "done");
        }

        /// <summary>
        /// Model-only files. The animation is off because these carry the same
        /// baked baselayer walk and nothing will ever play it, and leaving it
        /// on drops an orphan clip per model into the project.
        /// </summary>
        static void DoModel(string path, string name, ref int changed, ref int untouched,
                            ref int problems, StringBuilder sb)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { sb.AppendLine($"{name}: NO IMPORTER"); problems++; return; }

            bool dirty = mi.animationType != ModelImporterAnimationType.Human ||
                         mi.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel ||
                         mi.importAnimation ||
                         mi.addCollider;

            if (dirty)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = false;
                mi.addCollider = false;
                mi.isReadable = false;
                // Materials are imported here rather than set to None, because
                // this is a mesh that will actually be rendered in the village
                // and the animator files' duplicates are the ones that are not.
                mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                mi.SaveAndReimport();
                changed++;
            }
            else untouched++;

            // The whole point of Settle. Read without it, a valid avatar
            // measures as invalid, which is how an earlier pass of this tool
            // reported three perfectly good character models as broken.
            Settle(path);
            mi = AssetImporter.GetAtPath(path) as ModelImporter;

            var avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            var mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
            int verts = mesh == null ? 0 : mesh.vertexCount;

            bool ok = avatar != null && avatar.isValid && avatar.isHuman;
            if (!ok) problems++;

            sb.AppendLine($"MODEL {name}: " +
                          $"avatar={(avatar == null ? "NONE" : $"valid={avatar.isValid} human={avatar.isHuman}")} " +
                          $"mesh={verts}v {(dirty ? "(reconfigured)" : "(already correct)")}" +
                          (ok ? "" : "   <-- NOT USABLE"));
        }

        /// <summary>
        /// Animation files. Each gets an avatar of its own rather than sharing
        /// the character's: a file exported from Blender keeps the "mixamorig:"
        /// bone prefix while Ari's have it stripped, and CopyFromOther compares
        /// transform names rather than HumanBodyBones, so it refuses the whole
        /// import with "Transform 'Hips' for human bone 'Hips' not found" and
        /// zero clips come in. Giving each file its own avatar sidesteps that
        /// and costs nothing in playback, because Humanoid clips store muscle
        /// curves and retarget by design.
        /// </summary>
        static void DoAnim(string path, string name, string group, bool loop,
                           ref int changed, ref int untouched, ref int problems, StringBuilder sb)
        {
            var clipName = $"{group}_{name.Substring(name.IndexOf('_') + 1)}";

            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { sb.AppendLine($"{name}: NO IMPORTER"); problems++; return; }

            // Already configured and correct? Then do not reimport. These are
            // fifty-megabyte files and a redundant reimport is the single most
            // expensive thing this tool can do.
            if (mi.animationType == ModelImporterAnimationType.Human &&
                mi.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel &&
                mi.importAnimation &&
                mi.clipAnimations.Length == 1 &&
                mi.clipAnimations[0].name == clipName &&
                mi.clipAnimations[0].loopTime == loop)
            {
                untouched++;
                var existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                                       .FirstOrDefault(c => c.name == clipName);
                sb.AppendLine($"ANIM {name}: '{clipName}' already correct" +
                              (existing == null ? "  <-- BUT NOT ON DISK" : $" {existing.length:0.00}s"));
                if (existing == null) problems++;
                return;
            }

            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.sourceAvatar = null;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.importAnimation = true;
            mi.animationCompression = ModelImporterAnimationCompression.Optimal;
            mi.addCollider = false;
            mi.isReadable = false;
            mi.SaveAndReimport();
            Settle(path);
            mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { sb.AppendLine($"{name}: importer lost after reimport"); problems++; return; }

            // The take list is built during import, so on a file whose
            // animationType has just changed it is still empty and every take
            // reads as missing. Hence the Settle above, before this read.
            var takes = mi.defaultClipAnimations;
            if (takes == null || takes.Length == 0)
            {
                sb.AppendLine($"ANIM {name}: no takes at all  <-- CHECK");
                problems++;
                return;
            }

            var real = takes.Where(t => !IsBaselayer(t.takeName)).ToArray();

            sb.Append($"ANIM {name}: {takes.Length} take(s)");
            foreach (var t in takes) sb.Append($" [{t.takeName} f{t.firstFrame:0}-{t.lastFrame:0}]");

            if (real.Length != 1)
            {
                sb.AppendLine($"  <-- {real.Length} non-baselayer take(s); left unconfigured");
                problems++;
                return;
            }

            var chosen = real[0];
            chosen.name = clipName;
            chosen.loopTime = loop;
            // loopPose blends the tail of the clip into its first frame, which
            // only means anything for a cycle. On a one-shot it smears the
            // landing into the crouch.
            chosen.loopPose = loop;
            mi.clipAnimations = new[] { chosen };

            // clipAnimations hands back a fresh copy on get, so without the
            // write back the rename edits a temporary, the importer re-reads
            // the original on the next save, and the clip lands in the project
            // under the raw take name with every later lookup by name quietly
            // finding nothing.
            mi.SaveAndReimport();
            Settle(path);
            changed++;

            mi = AssetImporter.GetAtPath(path) as ModelImporter;
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                                .FirstOrDefault(c => c.name == clipName);
            bool configured = mi != null && mi.clipAnimations.Length == 1 &&
                              mi.clipAnimations[0].name == clipName &&
                              mi.clipAnimations[0].loopTime == loop;
            bool onDisk = clip != null;

            if (!configured || !onDisk) problems++;

            sb.AppendLine($"  -> '{clipName}' " +
                          (configured ? "verified" : "RENAME NOT CONFIRMED") + ", " +
                          (onDisk ? $"{clip.length:0.00}s on disk" : "NOT ON DISK") +
                          $"{(loop ? ", looping" : ", one-shot")}");
        }

        /// <summary>
        /// Blocks until the asset database has actually finished with a path.
        ///
        /// SaveAndReimport returns as soon as the import is queued. Reading the
        /// importer or its sub-assets immediately afterwards gives a null
        /// importer, an invalid avatar, or an empty take list for an asset that
        /// is entirely healthy -- and all three look identical to a failed
        /// import, which is how a correct file gets "fixed" into a broken one.
        ///
        /// ImportAsset on an unchanged asset is close to free, so this is
        /// called unconditionally rather than only after a reimport.
        /// </summary>
        static void Settle(string path)
        {
            AssetDatabase.ImportAsset(path,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>
        /// The take Mixamo bakes into every export it makes: the rig's own
        /// default walk, 1.03s, present in Ari's files and Mono's alike. It is
        /// identified by name because it is the only take whose name mentions
        /// the armature rather than the clip.
        /// </summary>
        static bool IsBaselayer(string takeName)
        {
            if (string.IsNullOrEmpty(takeName)) return true;
            return takeName.IndexOf("baselayer", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static string ResolveSourceFolder(StringBuilder sb)
        {
            string docs = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.MyDocuments);
            if (!Directory.Exists(docs)) return null;

            var match = Directory.GetDirectories(docs, SourceFolderHint + "*")
                              .OrderByDescending(p => p.Length)
                              .FirstOrDefault();
            if (match != null) return match;

            // No prefix match at all. Rather than fail on a spelling difference,
            // look for any folder holding the one file with no plausible
            // alternative name.
            var byContent = Directory.GetDirectories(docs)
                .Where(p => File.Exists(Path.Combine(p, "ink_crwaller_character.fbx")))
                .FirstOrDefault();
            if (byContent != null)
            {
                sb.AppendLine($"NOTE: no folder named '{SourceFolderHint}*'; used {byContent} " +
                              "because it holds the Ink Crawler");
                return byContent;
            }

            sb.AppendLine("SCANNED: " + docs);
            return null;
        }

        /// <summary>
        /// Copies unless the destination is already byte- and time-identical.
        /// Returns true only when a copy actually happened, so a re-run does
        /// not invalidate twenty importers by rewriting twenty identical files
        /// with fresh timestamps.
        /// </summary>
        static bool CopyIfDifferent(string from, string to)
        {
            var a = new FileInfo(from);
            if (File.Exists(to))
            {
                var b = new FileInfo(to);
                if (a.Length == b.Length && a.LastWriteTimeUtc == b.LastWriteTimeUtc) return false;
            }
            File.Copy(from, to, true);
            return true;
        }

        static void Finish(StringBuilder sb, string tail)
        {
            Debug.Log("[Echoes] L1 import " + tail + "\n" + sb);
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), Report), sb.ToString());
            // Appended as well as overwritten: the report is truncated every
            // pass because a stale one left by a killed call reads exactly
            // like a successful one, but the passes that did not fit inside
            // the bridge's time limit still need to be readable afterwards.
            File.AppendAllText(Path.Combine(Directory.GetCurrentDirectory(), Log), sb.ToString());
        }
    }
}
