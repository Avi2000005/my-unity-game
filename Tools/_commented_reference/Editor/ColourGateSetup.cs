using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes.Painterly.EditorTools
{
    /// <summary>
    /// Wires the end of the level: the colour gate, the fountain, and the
    /// fragment that goes into it.
    ///
    /// <para><b>Beats 6 and 7 are two components and one switch, and all three
    /// have to be wired or the level finishes grey.</b> A gate with no
    /// exemption refuses the fountain's colour; a fountain with no gate cannot
    /// be blue at all; and the gate has to be closed at Awake rather than left
    /// open, because the brush's 6 m radius reaches the fountain from the
    /// middle of the square. Each of those fails without an error, and the
    /// player only finds out at the very end of the level.</para>
    ///
    /// <para><b>It reports the exemption count, not just that it set one.</b>
    /// "The fountain is exempt" and "one target is exempt in the whole village"
    /// are different claims, and only the second one says whether the gate will
    /// hold.</para>
    /// </summary>
    public static class ColourGateSetup
    {
        const string Report = "Temp/colour_gate.txt";

        [MenuItem("Tools/Echoes/Wire Fountain and Colour Gate", priority = 60)]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Echoes] wire the fountain and the colour gate");

            if (EditorApplication.isPlaying)
            {
                sb.AppendLine("  refused: in play mode");
                Finish(sb);
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.rootCount == 0)
            {
                sb.AppendLine("  FATAL: no scene open.");
                Finish(sb);
                return;
            }

            // --- the fountain --------------------------------------------------
            var fountain = Root("Fountain");
            if (fountain == null)
            {
                sb.AppendLine("  FATAL: no root called Fountain. Beat 7 has " +
                              "nothing to fix.");
                Finish(sb);
                return;
            }

            sb.AppendLine("  fountain at " + fountain.position.ToString("F2") +
                          ", " + fountain.childCount + " children");

            var under = fountain.GetComponentsInChildren<ColorRestoreTarget>(true);
            sb.AppendLine("  ColorRestoreTargets under it: " + under.Length);

            for (int i = 0; i < under.Length; i++)
            {
                sb.AppendLine("    '" + under[i].name + "' at " +
                              under[i].transform.position.ToString("F2") +
                              " restore " + under[i].Restore.ToString("0.000") +
                              (under[i].Exempt ? "  EXEMPT" : ""));
            }

            if (under.Length == 0)
            {
                sb.AppendLine("  FATAL: the fountain has no ColorRestoreTarget, so " +
                              "there is nothing for the water to change. Beat 7 " +
                              "would complete with a grey fountain and no warning.");
                Finish(sb);
                return;
            }

            // The water is the first target under the fountain. Said plainly in
            // the report rather than searched for by name: a name search that
            // finds nothing leaves the beat silently unwired, and a first-child
            // choice at least names itself as a choice.
            var water = under[0];
            sb.AppendLine("  using as the water: '" + water.name +
                          "'  <- the FIRST ColorRestoreTarget under the " +
                          "fountain, chosen by position not by name, because a " +
                          "name that does not match leaves the beat unwired");

            // --- the gate ------------------------------------------------------
            var gateGo = new GameObject("L1_ColourGate");
            gateGo.transform.SetParent(scene.GetRootGameObjects().Length > 0
                ? scene.GetRootGameObjects()[0].transform
                : null);

            var gate = gateGo.AddComponent<ColourGate>();
            SetField(gate, "fountainWater", water);

            water.GrantExemption();
            EditorUtility.SetDirty(water);

            sb.AppendLine();
            sb.AppendLine("  + ColourGate on a new root 'L1_ColourGate' -> " +
                          water.name);

            // --- the fountain fix ----------------------------------------------
            var fix = fountain.GetComponent<FountainFix>();
            if (fix == null) fix = fountain.gameObject.AddComponent<FountainFix>();
            else sb.AppendLine("  FountainFix already on the fountain, reusing");

            SetField(fix, "water", water);

            var mono = MonoCompanion.FindInLevel();
            if (mono != null) SetField(fix, "mono", mono);

            sb.AppendLine("  FountainFix -> water '" + water.name + "'" +
                          (mono != null ? ", mono '" + mono.name + "'" : ", mono NOT FOUND"));

            // --- the tally ------------------------------------------------------
            //
            // Read at edit time, so it is a statement about the inspector state
            // rather than about a runtime gate that has not sealed yet.
            int sealedCount = 0, exemptCount = 0;
            var all = Object.FindObjectsByType<ColorRestoreTarget>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].Exempt) exemptCount++;
                else sealedCount++;
            }

            sb.AppendLine();
            sb.AppendLine("  village targets: " + (sealedCount + exemptCount) +
                          " total — " + sealedCount + " will be held grey, " +
                          exemptCount + " exempt");

            if (exemptCount != 1)
                sb.AppendLine("  " + (exemptCount == 0
                    ? "NOTHING is exempt. Beat 7 will ask the water for colour, " +
                      "the gate will refuse, and the level will complete grey " +
                      "while reporting success."
                    : exemptCount + " targets are exempt, so colour could arrive " +
                      "somewhere other than the fountain. The gate will not " +
                      "hold for the beat it exists to hold."));

            // Beat 6's fragment, if it has been placed. Reported rather than
            // created — a fragment's position is a level design decision and
            // this tool does not get to make it.
            var frag = Object.FindAnyObjectByType<ColourFragment>(FindObjectsInactive.Include);
            sb.AppendLine();
            sb.AppendLine("  blue fragment: " + (frag == null
                ? "NOT PLACED — AriInteract will find nothing to press E on, so " +
                  "the fountain will refuse the fix and the level cannot be " +
                  "finished"
                : "'" + frag.name + "' at " + frag.transform.position.ToString("F2") +
                  ", showing " + frag.IsShowing + ", taken " + frag.Taken));

            sb.AppendLine();
            sb.AppendLine(SaveAfter.Save("colour gate + fountain fix"));

            Finish(sb);
        }

        /// <summary>
        /// Set a private serialized field by name.
        ///
        /// <para>Reflection rather than making a setter public for every one of
        /// these: they are wiring fields set once by a build tool, not state a
        /// beat reads. And the name is checked — a typo would otherwise leave
        /// the field null and the beat quietly unwired, which is the exact
        /// failure this tool exists to prevent.</para>
        /// </summary>
        static void SetField(object target, string field, object value)
        {
            var f = target.GetType().GetField(field,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

            if (f == null)
            {
                Debug.LogError("[Echoes] ColourGateSetup: " + target.GetType().Name +
                               " has no field '" + field + "'. The beat is left " +
                               "unwired.");
                return;
            }

            f.SetValue(target, value);
        }

        static Transform Root(string n)
        {
            var all = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == n) return all[i].transform;
            return null;
        }

        static void Finish(StringBuilder sb)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), Report),
                sb.ToString());
            Debug.Log("[Echoes] colour gate wired — see " + Report);
        }
    }
}