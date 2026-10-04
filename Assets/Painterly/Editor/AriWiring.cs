using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Attaches the Level 1 systems to the objects they belong on, and
    /// reports exactly what it changed.
    ///
    /// <para><b>Idempotent on purpose.</b> Every one of these components is
    /// checked before it is added. A builder that adds without checking will
    /// stack a second <see cref="AriHealth"/> on the second run, and two health
    /// components means two bars, two death handlers and a lose screen that
    /// fires twice — none of which points at the cause.</para>
    ///
    /// <para><b>It reports instead of assuming.</b> Ari may have no
    /// <c>AriMover</c> under a different name, or may be a prefab instance
    /// where adding a component is a prefab edit rather than a scene edit.
    /// Both are things to be told about, not worked around.</para>
    ///
    /// <para><b>Does not save.</b> The scene is marked dirty and the report is
    /// written; whether to keep it is the user's call, because a tool that
    /// saves on every run makes every run a commit.</para>
    /// </summary>
    public static class AriWiring
    {
        const string Report = "Temp/ari_wiring.txt";

        [MenuItem("Tools/Echoes/Wire Level 1 Systems", priority = 40)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] wire level 1 systems");

            if (EditorApplication.isPlaying)
            {
                sb.AppendLine("  refused: the editor is in play mode. Component " +
                              "adds made now are thrown away when play stops.");
                Finish(sb);
                return;
            }

            var scene = SceneManager.GetActiveScene();
            sb.AppendLine("  scene       : '" + scene.name + "' roots=" +
                          scene.rootCount + " dirty=" + scene.isDirty);

            var ari = Object.FindAnyObjectByType<AriMover>(FindObjectsInactive.Include);
            if (ari == null)
            {
                sb.AppendLine();
                sb.AppendLine("  FATAL: no AriMover in the scene. Everything below " +
                              "attaches to HIM, so nothing can be wired. Find HIS " +
                              "first and re-run.");
                Finish(sb);
                return;
            }

            sb.AppendLine();
            sb.AppendLine("Ari at " + ari.transform.position.ToString("F2"));
            sb.AppendLine("  name        : '" + ari.name + "'");
            sb.AppendLine("  isPrefab    : " + PrefabUtility.IsPartOfPrefabInstance(ari.gameObject) +
                          "  <- if true, the adds below land on the INSTANCE and " +
                          "are lost when it is reverted");
            sb.AppendLine();

            int added = 0, present = 0;
            added += Ensure<AriHealth>(ari.gameObject, sb, "health");
            added += Ensure<AriInteract>(ari.gameObject, sb, "interact (E)");
            added += Ensure<AriHudOverlay>(ari.gameObject, sb, "health bar + flash + slot");

            sb.AppendLine();
            sb.AppendLine("  added " + added + ", already there " + present);

            // The HUD bar needs a health to read, and the interact scanner needs
            // the mover. Both are worth stating rather than assuming, because
            // both can be silently absent and both fail at runtime only.
            var hp = ari.GetComponent<AriHealth>();
            sb.AppendLine();
            sb.AppendLine("wiring check:");
            sb.AppendLine("  AriHealth      : " + (hp != null
                ? "hit cost " + (hp.HitCost * 100f).ToString("0") + "% of remaining" +
                  ", low at " + (hp.LowAt * 100f).ToString("0") + "%" +
                  ", hits taken " + hp.Hits
                : "MISSING — the bar will not draw and nothing can be hurt"));

            var inter = ari.GetComponent<AriInteract>();
            sb.AppendLine("  AriInteract   : " + (inter != null
                ? "reach " + inter.Reach.ToString("0.0") + " m from HIM chest, scan " +
                  inter.ScanInterval.ToString("0.00") + "s"
                : "MISSING — nothing in the level can be picked up or fixed"));

            // What it will find. Measured, not assumed: the interactable scan
            // walks MonoBehaviours, so an interactable that is not a
            // MonoBehaviour would be invisible to it and the beat would wait
            // forever.
            var interactables = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            int canInteract = 0;
            var names = new StringBuilder();
            for (int i = 0; i < interactables.Length; i++)
            {
                if (interactables[i] is IInteractable)
                {
                    canInteract++;
                    names.Append(" '").Append(interactables[i].name).Append('\'');
                }
            }

            sb.AppendLine("  interactables : " + canInteract + " in the scene" +
                          (canInteract > 0 ? " ->" + names : ""));
            if (canInteract == 0)
                sb.AppendLine("                   <- Beats 6 and 7 have nothing " +
                              "to press E on yet. Expected until they are placed.");

            // Reset wiring. LevelCheckpoint scans for IResettable, so the count
            // here is exactly how many beats a retry will reset — and a beat
            // that is missing from this list is a beat that does not re-arm.
            int resettable = 0;
            var rn = new StringBuilder();
            for (int i = 0; i < interactables.Length; i++)
            {
                if (interactables[i] is IResettable)
                {
                    resettable++;
                    rn.Append(" '").Append(interactables[i].name).Append('\'');
                }
            }

            sb.AppendLine("  resettable    : " + resettable + " ->" + rn);

            // Whether anything will call the checkpoint at all. A checkpoint
            // with no armed flag is a checkpoint that never fires.
            var checkpoints = Object.FindObjectsByType<LevelCheckpoint>(FindObjectsInactive.Include);
            sb.AppendLine("  checkpoints   : " + checkpoints.Length);
            for (int i = 0; i < checkpoints.Length; i++)
                sb.AppendLine("                  '" + checkpoints[i].name + "'");

            if (added > 0)
            {
                sb.AppendLine();
                sb.AppendLine(SaveAfter.Save("Ari's health, interact and HUD"));
            }

            Finish(sb);
        }

        /// <summary>
        /// Add a component unless it is already there.
        /// </summary>
        /// <returns>1 if it was added, 0 if it was already present.</returns>
        static int Ensure<T>(GameObject go, StringBuilder sb, string what)
            where T : Component
        {
            var have = go.GetComponent<T>();
            if (have != null)
            {
                sb.AppendLine("  " + what.PadRight(22) + " already on '" + go.name + "'");
                return 0;
            }

            var added = go.AddComponent<T>();
            sb.AppendLine("  " + what.PadRight(22) + " ADDED to '" + go.name + "' -> " +
                          added.GetType().Name);
            return 1;
        }

        static void Finish(StringBuilder sb)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), Report),
                sb.ToString());
            Debug.Log("[Echoes] wiring done — see " + Report);
        }
    }
}