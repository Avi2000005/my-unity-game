using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Configures the three Mixamo FBXs as one working character.
    ///
    /// The three files arrive unrelated. Each carries its own copy of the
    /// skeleton and its own copy of the mesh, none knows about the others, and
    /// the animations have no avatar to play on. Dropping Walking.fbx onto Ari
    /// does nothing at all. What makes them work is a shared Humanoid avatar:
    /// all three files were verified to carry the same Mixamo bone set, which
    /// Unity's mapper reads directly (LeftArm to LeftUpperArm, LeftForeArm to
    /// LeftLowerArm), so the character gets an avatar from its own rig and both
    /// animations are pointed at it.
    ///
    /// The subtle part is picking the right take out of each file. Every one of
    /// them contains two: the file's own animation, and a 1.033s walk cycle
    /// called "Armature|Armature|walking_man|baselayer" that is byte-for-byte
    /// identical in all three because Mixamo bakes the rig's default walk into
    /// every export. Importing both means two identically named walk cycles to
    /// choose between and no way to tell them apart in the inspector. So the
    /// shared take is dropped and only the file's own animation is imported,
    /// under a name that says what it is.
    /// </summary>
    public static class AriImportSetup
    {
        const string Dir = "Assets/Art/Ari/Models";

        const string Character = "Ari_character";

        /// <summary>
        /// The take holding each file's own animation. Identified by measuring
        /// the muscle curves: this take moves (widest muscle range 0.086 for
        /// Idle, 1.039 for Walking) while the baselayer walk is the same
        /// 1.209-range clip in every file regardless of which file it is in.
        /// Verified rather than assumed — if this take is ever missing the
        /// import fails loudly instead of silently falling back to the walk.
        /// </summary>
        const string OwnTake = "mixamo.com";

        /// <summary>
        /// Animation files, the clip name the controller looks for, whether the
        /// file needs an avatar of its own, and whether the clip loops.
        ///
        /// Idle2 is the replacement idle. Its source file was "Idle (2).fbx";
        /// spaces inside an asset path are a permanent, avoidable nuisance, so it
        /// was copied in as Idle2. The original is left in Downloads untouched
        /// and the previous idle stays in the project as Ari_Idle_Legacy.
        ///
        /// The third flag is what makes Idle2 work. Its bones are exported with
        /// Blender's Mixamo prefix (mixamorig:Hips, mixamorig:Spine1) while
        /// Ari's files have it stripped (Hips, Spine01), so CopyFromOther fails
        /// outright: "Transform 'Hips' for human bone 'Hips' not found", and
        /// zero clips import. Give it its own avatar instead. That is not a
        /// workaround — Humanoid clips store muscle curves rather than bone
        /// rotations, so a clip authored on one avatar retargets onto another
        /// by design. The prefix stops mattering the moment Unity maps it to
        /// a HumanBodyBone.
        ///
        /// The fourth flag exists for one clip. A hop must not loop: with
        /// loopTime on, the crouch at the start of the jump is the frame the
        /// animator returns to, so Ari sinks into a crouch and springs out of it
        /// forever, and on landing she is still in the crouch half of a second
        /// later. loopPose is off for the same reason — it exists to smooth the
        /// seam of a cycle that does not have one.
        ///
        /// The jump is ari_running_Jump, chosen over the 1.9s ari_jumping by
        /// measurement: the first has one rise-and-fall, the second has two plus
        /// a run-up, so the longer one would hold her off the ground for 1.9s.
        /// </summary>
        static readonly (string File, string ClipName, bool OwnAvatar, bool Loop)[] Animations =
        {
            // The pair the user supplied last: source files "Idle (1).fbx" and
            // "Walking (1).fbx". Both carry the mixamorig: prefix, so both need
            // an avatar of their own exactly as Idle2 did.
            ("Idle3", "Ari_Idle", true, true),
            ("Walking2", "Ari_Walk", true, true),

            // The hop, triggered on demand. Never looped.
            ("ari_running_Jump", "Ari_Jump", true, false),

            // Kept selectable rather than deleted. Same names would collide on
            // Animator.StringToHash and make state resolution order-dependent, so
            // the superseded pair is demoted to *_Alt below.
            ("Idle2", "Ari_Idle_Alt", true, true),
            ("Walking", "Ari_Walk_Alt", false, true)
        };

        [MenuItem("Tools/Echoes/Import Ari", priority = 60)]
        public static void Run()
        {
            var sb = new StringBuilder();

            // The files are copied in by hand, so on a fresh checkout the
            // AssetDatabase has never seen them and has written no .meta.
            // Without this every LoadAssetAtPath below returns null and the
            // whole setup quietly does nothing.
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate |
                                  ImportAssetOptions.ForceSynchronousImport);

            string charPath = $"{Dir}/{Character}.fbx";
            ImportIfNew(charPath);

            var ci = AssetImporter.GetAtPath(charPath) as ModelImporter;
            if (ci == null)
            {
                Debug.LogError($"[Echoes] {charPath} did not import. Is the file in the project?");
                return;
            }

            // If the cached bone mapping has gone bad, drop it and let the
            // auto-mapper run again. Once an avatar import breaks, the importer
            // will keep serving the broken mapping indefinitely otherwise, so
            // without this the tool has no way back from a bad run. Switching
            // the rig to Generic and saving is what invalidates the cache;
            // setting Human again below re-runs the mapper from the mesh.
            var existing = AssetDatabase.LoadAllAssetsAtPath(charPath)
                                 .OfType<Avatar>().FirstOrDefault();
            if (existing != null && !(existing.isValid && existing.isHuman))
            {
                sb.AppendLine($"existing avatar is unusable " +
                              $"(valid={existing.isValid} human={existing.isHuman}); " +
                              "discarding the cached mapping and re-running the mapper");

                ci.animationType = ModelImporterAnimationType.Generic;
                ci.SaveAndReimport();

                ci = AssetImporter.GetAtPath(charPath) as ModelImporter;
                if (ci == null)
                {
                    Debug.LogError("[Echoes] lost the importer while resetting " + charPath);
                    return;
                }
            }

            ci.animationType = ModelImporterAnimationType.Human;
            ci.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            // The character model only needs to be a model. Its two takes are
            // the same shared walk plus one unidentified 4.5s clip, and neither
            // is referenced by anything.
            ci.importAnimation = false;
            ci.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            ci.addCollider = false;
            ci.isReadable = false;
            ci.SaveAndReimport();

            // Reported after the reimport, not before: humanDescription is only
            // populated once the mapper has actually run, so reading it earlier
            // just reports an empty array and looks like the rig was empty.
            ReportAvatarBones(AssetImporter.GetAtPath(charPath) as ModelImporter, sb);

            var avatar = AssetDatabase.LoadAllAssetsAtPath(charPath)
                            .OfType<Avatar>().FirstOrDefault();

            if (avatar == null || !avatar.isValid || !avatar.isHuman)
            {
                Debug.LogError("[Echoes] " + charPath + " produced no usable humanoid avatar. " +
                               "The animations cannot retarget onto it.\n" + sb);
                return;
            }

            sb.AppendLine($"character avatar: valid={avatar.isValid} human={avatar.isHuman}");

            foreach (var (file, clipName, ownAvatar, shouldLoop) in Animations)
            {
                string path = $"{Dir}/{file}.fbx";
                ImportIfNew(path);

                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null) { sb.AppendLine($"{file}: MISSING"); continue; }

                // See Animations: an avatar of its own is required for any file
                // whose bone names do not match Ari's, because CopyFromOther
                // compares transform names rather than HumanBodyBones and
                // refuses the import outright.
                mi.animationType = ModelImporterAnimationType.Human;
                if (ownAvatar)
                {
                    mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    mi.sourceAvatar = null;
                }
                else
                {
                    mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    mi.sourceAvatar = avatar;
                }

                // Each animation file carries a duplicate of Ari's mesh that
                // nothing will ever render, so materials are not imported and
                // three sets of duplicate assets are avoided.
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importAnimation = true;
                mi.animationCompression = ModelImporterAnimationCompression.Optimal;
                mi.addCollider = false;
                mi.isReadable = false;

                // Reimport BEFORE reading defaultClipAnimations. The take list is
                // built during import, so on a file whose animationType or
                // avatarSetup has just changed it is still empty and
                // FirstOrDefault reports the take as missing while a clip with
                // the raw name ('mixamo.com') silently lands in the project —
                // unrenamed and not looping, so the controller finds no Ari_Idle
                // and the idle appears to have imported wrong.
                mi.SaveAndReimport();
                mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null)
                {
                    sb.AppendLine($"{file}: lost the importer after reimport");
                    continue;
                }

                var own = mi.defaultClipAnimations.FirstOrDefault(c => c.takeName == OwnTake);
                if (own == null)
                {
                    sb.AppendLine($"{file}: take '{OwnTake}' not found; takes are " +
                        string.Join(", ", mi.defaultClipAnimations.Select(c => c.takeName)));
                    mi.clipAnimations = new ModelImporterClipAnimation[0];
                }
                else
                {
                    own.name = clipName;
                    // A crouch that loops is Ari dipping into a squat and springing
                    // out of it forever, so the hop is the one clip here that must
                    // not loop. See Animations.
                    own.loopTime = shouldLoop;
                    // loopPose blends the tail of the clip into its first frame,
                    // which is only meaningful for a cycle. On a one-shot it
                    // smears the landing into the crouch.
                    own.loopPose = shouldLoop;
                    mi.clipAnimations = new[] { own };
                }

                mi.SaveAndReimport();

                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                              .Where(c => !c.name.StartsWith("__")).ToArray();

                sb.AppendLine($"{file}: {clips.Length} clip(s) " +
                    string.Join(", ", clips.Select(c => $"'{c.name}' {c.length:0.00}s loop={c.isLooping}")));
            }

            // The superseded Idle.fbx still ships a clip named Ari_Idle, and two
            // clips with that name make Animator.StringToHash ambiguous — a
            // state resolves to whichever loaded first, so the controller can
            // appear to ignore the new animation with nothing wrong in it.
            // Renamed rather than deleted: the file stays as a one-line fallback.
            RenameLegacy(Dir + "/Idle.fbx", "Ari_Idle", "Ari_Idle_Legacy", sb);

            Debug.Log("[Echoes] Ari import configured\n" + sb);
        }

        /// <summary>
        /// Report the two known defects in this avatar, and change nothing.
        ///
        /// Both were found by measuring, and neither is worth the risk of a
        /// fix. Editing ModelImporter.humanDescription to correct them was
        /// tried and it broke the avatar outright — the round-trip is not
        /// faithful, and clearing a single bone was enough to take the import
        /// from valid-and-human to unusable. A working avatar beats a tidy one.
        ///
        /// LeftEye is mapped onto "Spine", the topmost of the three spine
        /// bones. An eye is not a spine. It is inert today because Mixamo writes
        /// no eye motion — measured dead flat at 0.000 across every clip in all
        /// three files — but it would surface the day a facial clip is added.
        ///
        /// UpperChest is unmapped, which does cost something real: the walk
        /// cycle swings it through 0.694 rad and that motion is discarded,
        /// leaving Ari's upper torso stiff. It cannot be fixed from script
        /// because humanDescription.human is truncated to 22 entries, ending
        /// at the last slot the mapper filled, and UpperChest would be slot 54.
        /// The slot is not in the array. Mapping it by hand in the inspector
        /// would mean editing the FBX's importer asset directly.
        ///
        /// Spine and Chest are both mapped, so the torso still bends and twists
        /// below the collarbone.
        /// </summary>
        /// <summary>
        /// Renames a clip inside an FBX importer so it cannot collide with a
        /// newer one.
        ///
        /// An AnimationClip inside a model is addressed by the name given in the
        /// importer's clip settings, so renaming it is a one-line change to the
        /// importer rather than a rewrite of the asset. Left alone, two clips
        /// called Ari_Idle mean any name-based lookup — an Animator state
        /// matching by StringToHash, or a diagnostic that lists clips — reports
        /// whichever happened to load first, and the new animation appears to
        /// have silently failed to take effect.
        /// </summary>
        static void RenameLegacy(string path, string from, string to, StringBuilder sb)
        {
            var mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) { sb.AppendLine($"{path}: no legacy clip to rename"); return; }

            var clips = mi.clipAnimations;
            if (clips == null || clips.Length == 0) { sb.AppendLine($"{path}: no configured clips"); return; }

            bool changed = false;
            foreach (var c in clips)
            {
                if (c.name != from) continue;
                c.name = to;
                changed = true;
            }

            if (!changed)
            {
                sb.AppendLine($"{path}: nothing to rename (clips now: [{ClipNames(mi)}])");
                return;
            }

            // clipAnimations is a property whose getter hands back a fresh copy,
            // so renaming the objects it yielded only edited that copy. The
            // importer's own state is untouched, SaveAndReimport re-imports the
            // original name, and the rename silently reverts on disk.
            mi.clipAnimations = clips;
            mi.SaveAndReimport();

            // Read back instead of trusting the write. This step has already
            // reported success once while the meta on disk still said the old
            // name and the controller carried on referencing the old clip.
            var after = mi.clipAnimations;
            foreach (var c in after)
            {
                if (c.name != to) continue;
                sb.AppendLine($"{path}: '{from}' -> '{to}' verified");
                return;
            }

            sb.AppendLine($"{path}: RENAME FAILED, clips now: [{ClipNames(mi)}]");
        }

        /// <summary>Comma-joined clip names currently configured on a model.</summary>
        static string ClipNames(ModelImporter mi)
        {
            var names = mi.clipAnimations;
            if (names == null || names.Length == 0) return "none";

            var joined = new StringBuilder();
            for (int i = 0; i < names.Length; i++)
            {
                if (i > 0) joined.Append(", ");
                joined.Append(names[i].name);
            }
            return joined.ToString();
        }

        static void ReportAvatarBones(ModelImporter mi, StringBuilder sb)
        {
            var hd = mi.humanDescription;

            int upperChest = (int)HumanBodyBones.UpperChest;
            string upperChestState =
                upperChest < hd.human.Length ? "present" : "ABSENT from the array";

            sb.AppendLine($"  avatar mapping: {hd.human.Length} slot(s), " +
                          $"UpperChest would be slot {upperChest} -> {upperChestState}");

            int eye = (int)HumanBodyBones.LeftEye;
            sb.AppendLine(eye >= 0 && eye < hd.human.Length
                ? $"  known defect, left alone: LeftEye -> '{hd.human[eye].boneName}' " +
                  "(inert: no eye motion in any clip)"
                : "  LeftEye: unmapped");
        }

        /// <summary>
        /// Force a synchronous import if the model has no importer yet.
        ///
        /// A model copied into Assets/ outside the editor has no .meta, so
        /// GetAtPath returns null and nothing downstream can be configured.
        /// These are 17 MB exports and there are three of them, so this is
        /// deliberately synchronous rather than left to the background refresh.
        /// </summary>
        static void ImportIfNew(string path)
        {
            if (AssetImporter.GetAtPath(path) != null) return;
            AssetDatabase.ImportAsset(path,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
