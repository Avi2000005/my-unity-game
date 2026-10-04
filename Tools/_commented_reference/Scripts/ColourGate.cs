using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// Holds the colour shut for the whole of Level 1, and is the one thing
    /// that opens it.
    ///
    /// <para><b>This is instruction 3 as an object rather than as a promise.</b>
    /// "The village stays grey until Ari finishes Level 1" is easy to state in a
    /// comment and easy to break: someone adds a <c>RestoreTo</c> to a beat, or
    /// the brush's 6 m radius happens to reach the fountain, and the world gains
    /// a colour four beats early with nothing in the log saying so. Every one of
    /// those is invisible from the script that caused it.</para>
    ///
    /// <para>So the rule is enforced at the bottom of
    /// <see cref="ColorRestoreTarget.RestoreTo"/> instead. A target that is
    /// sealed refuses colour arriving, and everything in the village is sealed
    /// by default. Opening the gate is then one call at the end of the level, and
    /// a beat that tries to restore mid-level gets refused and logged.</para>
    ///
    /// <para><b>Sealed by default, exempt by name.</b> The fountain's water is
    /// the one thing in Level 1 that must take colour, and it takes it while the
    /// gate is still shut — Beat 7's first half is the water going blue and
    /// nothing else moving. Without the exemption that beat is a no-op that
    /// reports success: the water is asked for colour, the gate refuses, the
    /// level completes, and the player finishes a grey level.</para>
    ///
    /// <para><b>It re-seals on a retry.</b> A checkpoint retry that left the
    /// gate open would hand the player a level that looks finished.</para>
    /// </summary>
    [AddComponentMenu("Echoes/Colour Gate")]
    public sealed class ColourGate : MonoBehaviour, IResettable
    {
        [Header("The one exemption")]
        [Tooltip("The fountain's water. Set exempt in Awake so it can take " +
                 "colour while the gate is shut. Left empty, the tool that " +
                 "places this reports it rather than Beat 7 failing quietly.")]
        [SerializeField] ColorRestoreTarget fountainWater;

        [Tooltip("Also exempt anything that names itself as water inside the " +
                 "fountain. Off, because a name match is a guess and a guess in " +
                 "the gate that decides the ending is the wrong place for one.")]
        [SerializeField] bool searchChildren = false;

        [SerializeField] bool log = true;

        // --- public read-only -------------------------------------------------

        public static bool IsSealed => ColorRestoreTarget.Sealed;

        /// <summary>How many targets are shut, and how many are allowed through.</summary>
        public static void Tally(out int sealedCount, out int exemptCount)
        {
            ColorRestoreTarget.Tally(out sealedCount, out exemptCount);
        }

        public ColorRestoreTarget FountainWater => fountainWater;

        void Awake()
        {
            Apply();
        }

        /// <summary>
        /// Seal everything, and let the water through.
        ///
        /// <para>Called from Awake and again on a retry. Awake alone is not
        /// enough: the statics do not survive a domain reload, but they DO
        /// survive a checkpoint retry inside one play session, and Beat 7 opens
        /// the gate legitimately — so a retry has to put it back.</para>
        /// </summary>
        public void Apply()
        {
            if (fountainWater == null && searchChildren)
            {
                var all = GetComponentsInChildren<ColorRestoreTarget>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (!all[i].gameObject.name.ToLowerInvariant().Contains("water")) continue;
                    fountainWater = all[i];
                    break;
                }
            }

            // Order matters and is not obvious: exempt first. Sealing is a
            // static global, so if the gate shut before the water was exempt and
            // something restored in between, the water would refuse — and the
            // refusal is silent at the call site.
            if (fountainWater != null) fountainWater.GrantExemption();
            else if (log)
                Debug.LogWarning("[Echoes] colour gate has no fountain water. " +
                                 "Beat 7 will complete without turning blue, and " +
                                 "nothing will say why until the player sees it.", this);

            ColorRestoreTarget.SetSealed(true);

            if (!log) return;

            Tally(out int shut, out int free);
            Debug.Log("[Echoes] colour gate shut: " + shut + " target(s) sealed, " +
                      free + " exempt" +
                      (fountainWater != null
                          ? " (the fountain, '" + fountainWater.name + "')"
                          : " (NONE — Beat 7 cannot work)"), this);
        }

        /// <summary>
        /// Put the level back to grey.
        /// </summary>
        public void ResetForCheckpoint()
        {
            // The water goes back to grey too. A retry that left it blue hands
            // the player a fixed fountain in an unfixed level.
            if (fountainWater != null) fountainWater.SetRestoreImmediate(0f);

            Apply();
        }
    }
}