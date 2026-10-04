using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Painterly
{
    /// <summary>
    /// Mono's lines, in order, as data a designer can edit.
    ///
    /// A ScriptableObject rather than a string array in a component, because
    /// the beat director, the tree and Mono itself all need to ask questions
    /// about the same lines — "has the player heard this one", "what comes
    /// next after the fountain's second dry groove" — and three components
    /// holding three copies of the same text drift apart the first time one of
    /// them is edited.
    ///
    /// The order in the list *is* the order they play, and the ids are how
    /// beats refer to them. A line is never removed from the middle, only
    /// muted, so an id always means the same thing across saves and across
    /// level rebuilds. That matters more than it sounds: a beat that fires
    /// "fountain.rung2" and finds it missing has to fall back to something
    /// sensible rather than to nothing.
    /// </summary>
    [CreateAssetMenu(fileName = "MonoHintLines",
                     menuName = "Echoes/Mono Hint Lines",
                     order = 40)]
    public sealed class MonoHintLines : ScriptableObject
    {
        /// <summary>What a line is for. Decides when it is allowed to play.</summary>
        public enum LineKind
        {
            /// <summary>Fires once, from whatever beat triggered it.</summary>
            Beat,

            /// <summary>
            /// Part of a ladder: rung 1, 2, 3. Plays in order, and only the
            /// next unplayed rung of a ladder can ever fire, so Mono cannot
            /// spoil rung 3 by saying it early.
            /// </summary>
            HintRung,

            /// <summary>
            /// Ambient lines while following, fired at random after a gap. These
            /// are what make Mono feel like he is with her rather than queued
            /// up, and the gap is the only thing stopping him from talking over
            /// himself.
            /// </summary>
            Ambient
        }

        [Serializable]
        public sealed class Line
        {
            [Tooltip("Stable id beats refer to, e.g. 'beat3.wake'. Never reused.")]
            public string id = "line";

            [TextArea(2, 4)]
            [Tooltip("What Mono says. Mono is the only speaker in Level 1.")]
            public string text = "...";

            public LineKind kind = LineKind.Beat;

            [Tooltip("For HintRung lines: 1 plays first, 2 second, and so on. " +
                     "Anything already played is skipped.")]
            public int rung = 1;

            [Tooltip("For Ambient lines: minimum seconds of quiet before this " +
                     "one may play at all.")]
            public float minGapSeconds = 25f;

            [Tooltip("Muted lines stay in the list so ids keep meaning the same " +
                     "thing, but never play. How the rest of Level 1 is switched off.")]
            public bool muted;
        }

        [Tooltip("Lines in play order. The order here is the order they fire.")]
        public List<Line> lines = new List<Line>();

        // --- runtime state --------------------------------------------------------
        // Not serialized: this is per-playthrough bookkeeping and has no business
        // surviving into the asset file, where a stale "already played" flag would
        // silently mute the first hint of every session.

        readonly HashSet<string> _played = new HashSet<string>();

        int _nextRung = 1;
        float _lastAmbientAt = -999f;

        /// <summary>Every line, in order, whether or not it can currently play.</summary>
        public IEnumerable<Line> All => lines;

        /// <summary>
        /// Take the next line Mono is allowed to say, or null.
        ///
        /// The ladder is walked strictly forwards. Asking for rung 2 first is
        /// how a player would get rung 3 without ever being stuck, which is the
        /// entire point of the ladder — Mono should get more explicit the longer
        /// the player is stuck, never less.
        /// </summary>
        public Line NextHint()
        {
            foreach (var line in lines)
            {
                if (line.muted || line.kind != LineKind.HintRung) continue;
                if (line.rung != _nextRung) continue;
                if (_played.Contains(line.id)) continue;

                _nextRung++;
                return line;
            }

            return null;
        }

        /// <summary>
        /// A beat line by id, or null if the id is not in the list.
        ///
        /// Reported rather than substituted. A beat asking for a line that is
        /// not there should be visible in the console, because the alternative
        /// — silently playing something else — turns a typo into a mystery.
        /// </summary>
        public Line Beat(string id)
        {
            foreach (var line in lines)
            {
                if (line.kind == LineKind.Beat && line.id == id) return line.muted ? null : line;
            }

            Debug.LogWarning("[Echoes] Mono has no beat line called '" + id + "'.");
            return null;
        }

        /// <summary>
        /// Something worth saying, or null.
        ///
        /// Only after a gap, and only if there is no beat or hint line waiting —
        /// Mono saying an aside in the middle of the fountain's second hint is
        /// the one way the hint system can be made useless.
        /// </summary>
        public Line NextAmbient(float now)
        {
            if (now - _lastAmbientAt < 20f) return null;
            if (NextHint() != null) return null;          // do not talk over a rung

            foreach (var line in lines)
            {
                if (line.muted || line.kind != LineKind.Ambient) continue;
                if (_played.Contains(line.id)) continue;
                if (now - _lastAmbientAt < line.minGapSeconds) continue;

                _lastAmbientAt = now;
                return line;
            }

            return null;
        }

        /// <summary>Play a line, marking it and its ladder position as used.</summary>
        public void Consume(Line line)
        {
            if (line == null) return;
            _played.Add(line.id);
        }

        /// <summary>Has this specific line already been used?</summary>
        public bool HasPlayed(string id) => _played.Contains(id);

        /// <summary>How far along the hint ladder Mono is, 0 before the first rung.</summary>
        public int RungsPlayed => Mathf.Max(0, _nextRung - 1);

        /// <summary>
        /// Forget everything. Called on a new playthrough and on a checkpoint
        /// reload, because a checkpoint that restores the player but not Mono's
        /// state gives them a companion who will not repeat himself and a
        /// puzzle whose hints are all spent.
        /// </summary>
        public void ResetMemory()
        {
            _played.Clear();
            _nextRung = 1;
            _lastAmbientAt = -999f;
        }

        /// <summary>
        /// A count of what is muted, for the setup tool to report. Muted lines
        /// are how the other 12 lines of Level 1 get switched off, and a line
        /// that was muted by accident is invisible in play.
        /// </summary>
        public void Tally(out int beat, out int rung, out int ambient, out int muted)
        {
            beat = rung = ambient = muted = 0;
            foreach (var line in lines)
            {
                if (line.muted) { muted++; continue; }
                switch (line.kind)
                {
                    case LineKind.Beat: beat++; break;
                    case LineKind.HintRung: rung++; break;
                    default: ambient++; break;
                }
            }
        }
    }
}
