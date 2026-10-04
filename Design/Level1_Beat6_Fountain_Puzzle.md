# Level 1 — Beat 6 Design Handoff: "The Waking Order"

**Deliverable chosen:** a new puzzle for the fountain sequence.
**Beat:** 6 — The First Color Fragment.
**Playable in:** under 2 minutes for a first-timer, first attempt.
**Colour used:** none. Every readable signal is shape, flow and wear — not hue.

---

## Why this replaces the two options in the brief

The brief offers *"light the 3 lanterns in the correct order"* or *"push a cracked
statue piece into place."*

**Lanterns in an order fails on the constraint "solvable by a first-timer in under
3 minutes."** Three lanterns is six permutations. A player who cannot deduce the
order brute-forces it, and brute-forcing is not understanding — it teaches the
opposite lesson, and it teaches it in the one beat the level is built around. Worse,
it is invisible by design: with the world colourless, a lantern lit in the wrong
order has *no* visual tell, so the player gets no feedback to learn from either.

**The statue piece is solvable but empty.** One socket, one piece, no wrong
answer. It is a lockpick with no lock. Nothing about it survives contact with
Beat 5.5, which is the level's first real pressure.

The replacement keeps the brief's intent (an environmental logic puzzle at the
fountain, teaching the pattern for later levels) and fixes both failures: **it has
a wrong answer, and the wrong answer is deducible from the world before it is
deducible from trying.**

---

## The concept

The fountain is a water clock that Ari's brush woke up. The basin still has a
channel system, and water still wants to run downhill. The puzzle is not
"which order" — it is **"which path does water take."**

The basin rim carries three carved channel mouths at three different heights.
Water enters at the highest and can only ever reach the lowest by a route that is
carved into the stone and visible from where the player is standing. The fragment
sits in the lowest basin cell, so the player has to bring the water to it.

There are fourteen cells on the rim. Only one chain of four moves delivers water
to the fragment. The other thirteen are dead ends that visibly dry out.

The reveal stroke is the key, and it is the thing the player has been using since
Beat 2. Nothing new is taught. The same verb, three beats later, means something
new — which is the cheapest and best tutorial in the whole level.

---

## Numbered build steps

### Section A — Set dressing (do this first, it is what the player reads)

1. **Carve the chevrons.** On the outer rim of the basin, cut a shallow groove
   that runs the full circumference. In it, cut small chevrons pointing
   *downhill*, in the direction water would actually run. The chevrons must be
   visible from the approach — Ari enters from the north-west. Do not gate this
   behind the reveal stroke. The player must be able to read the water direction
   in plain grey, because a clue they cannot see is not a clue.

2. **Make the heights obvious.** The three channel mouths are not level: the
   high mouth is at +0.42 m, the middle at +0.26 m, the low at +0.08 m, all
   above the waterline. Under grey lighting these must still read as three
   different heights from ten metres away. Test by standing at the approach
   marker and squinting. If the three mouths read as one flat ring, raise the
   height difference to 0.60 / 0.34 / 0.10 and re-test.

3. **Cut the fourteen cells.** Fourteen stone shutters around the rim, each the
   size of a dinner plate, each with a channel stub pointing at its neighbour.
   Thirteen of them are blocked with a stone plug visible from above. The
   fourteenth — the last one before the low mouth — is the fragment cell, and it
   is **not** distinguishable from the other thirteen by appearance. It is
   identifiable only by being the one the water reaches. Do not mark it.

4. **Grey out the "already done" state.** Each shutter is a ring of stone with a
   groove. Shut, the groove is dry. Open, the groove is wet — a specular
   difference, not a colour difference. A wet groove in a colourless world is
   one of the very few honest pieces of feedback available, and it costs
   nothing.

### Section B — The state machine

5. **Every shutter has two states: SHUT and OPEN.** Thirteen start SHUT-and-
   plugged. The fourteenth starts SHUT-and-empty. There is no third state and no
   locked state — a wrong answer is a wrong answer, never a refusal.

6. **The rule is a single rule, applied globally, and it is the whole puzzle:**
   *a shutter may be opened if and only if water can reach it.* Concretely —
   water enters the high mouth and spreads to any open shutter reachable by a
   downhill path of consecutively open shutters. Opening a shutter with no water
   upstream does nothing and says so: the groove stays dry. That is the
   feedback loop. It is immediate, it is honest, and it costs the player nothing
   to read.

7. **The success condition is one specific chain of four shutters** carrying
   water from the high mouth down to the fragment cell. Because the chevrons
   show the downhill geometry and the cell stubs show the adjacency, this chain
   is **deducible without trying**. A player who looks can find it. A player who
   does not look can still stumble into it inside two minutes. Both are
   acceptable; only brute-forcing all thirteen is not, and the wet-groove
   feedback makes brute-forcing pointless anyway.

8. **The forced first error.** On first contact, the nearest shutter to the
   player is the *wrong* one — with no water upstream. The player opens it, the
   groove stays dry, and Mono's line fires. This is intentional and must not be
   removed. It is the "oh, I should have *thought*" beat, and it is the single
   moment that makes the puzzle a puzzle rather than a formality. It costs
   eight seconds.

### Section C — Ari's part and Mono's part

9. **Ari is the water.** Ari's reveal stroke on a shutter is what opens it. This
   is the Echo Trail verb from Beat 2, unchanged, no new input, no new tutorial
   text. The reveal stroke is grey; on stone it reads as a bright outline; on a
   shutter it reads as a line of wet stone.

10. **Mono is the reader, not the solver.** Mono does not open anything. Mono
    looks at the basin and narrates the *rule* the first time it matters. He is
    the hint system, and Beat 3 is where that system was introduced — so this
    pays off a tutorial the player already has.

11. **Hint ladder — three rungs, all optional, none timed.** The player is never
    blocked:
    - **Rung 1 (on first dry groove):** Mono states the rule.
    - **Rung 2 (on a second dry groove, or 45 seconds of no progress):** Mono
      names the direction — downhill, always downhill.
    - **Rung 3 (on a third dry groove):** Mono says the high mouth is the only
      place water can start. The player is now being told the answer in words
      and can still fail to execute it, which is the right place to be.

### Section D — The payoff

12. **Water sound before water is visible.** When the first shutter opens with
    water upstream, a low water-noise cue plays under the ambience — quiet, and
    only in the fountain's radius. This is the first non-wind sound in the level
    since Beat 1, and it does more for the "the world is waking up" feeling than
    any visual could at this stage of the game.

13. **The chain completing is the level's colour moment.** On the fourth shutter,
    water reaches the fragment cell and the fragment lifts out of the water on
    its own. Ari does not have to walk in and collect it. Let it come to her.

14. **The colour burst is small, and it stays small.** Per the brief: a small
    radius regains colour. A grass patch, one wall, the water surface. Roughly a
    6 m radius from the fountain. The point is that the player has never seen
    colour before in this game, so 6 m of it is enormous. Do not make it larger —
    a big burst teaches the player the ceiling and spends it early.

15. **Nothing else in the village changes.** The rest of the level stays grey
    until later levels. The first fragment must feel like a leak, not a switch.

### Section E — Failure, and why there is none

16. **There is no fail state and no reset.** Nothing here can be lost, so nothing
    needs restoring. This is deliberate: Beat 5.5 immediately before this one
    already introduced a checkpoint restart on hit, and putting a second reset
    loop eight seconds later teaches the player to expect punishment and stops
    them looking at the fountain. Let them look at it.

17. **Undo is free and silent.** Opening a shutter can never be a mistake that
    costs anything. If the player wants to close everything and start over, they
    can, and the fountain will not comment.

18. **Escape hatch for the truly stuck.** If the player paints every shutter
    that has water and the fragment still has not lifted, the low mouth's own
    groove fills on its own after 20 seconds. The level cannot be failed. The
    designer does not need to build a skip because there is no state to skip
    out of.

### Section F — What this teaches, for later levels

19. **The lesson to carry forward is: read the world, not the UI.** Beat 2 was
    "paint three things fast." Beat 6 is "the world has been telling you the
    answer and you did not read it." That is the arc of the level, and it should
    be the template for every environmental puzzle from here on.

20. **Reuse the verb, change the meaning.** The brush is grey in Beat 2 and grey
    in Beat 6. Same button, same animation, same sound. What changed is that
    the player now knows what the brush is *for*. This is free depth, and it is
    only available if the designer is disciplined about not adding a new input.

21. **Do not put a colour-based variant of this in Level 1.** The obvious
    temptation is to make the fragment's cell light up when the chain is right.
    Don't. A correct answer that is announced by a light the player has never
    seen before teaches nothing and breaks the "no colour unlocked" rule twice —
    once in the mechanic and once in the promise Beat 2 made.

---

## In-game dialogue

Mono, at the fountain — the moment the player opens the first shutter that has no
water behind it:

> **Mono:** "…Hm. Nothing came. The stone stayed dry."
> **Mono:** "Water doesn't jump. It only ever goes *down*."

Mono, on the first shutter that does take water:

> **Mono:** "There. Do you hear that? It's *running*."
> **Mono:** "Follow it. Don't guess — just follow where it wants to go."

Mono, as the fragment lifts out of the water:

> **Mono:** "It's giving it back. It doesn't hate this place, Ari."
> **Mono:** "It just forgot how to be anything else."

*(Three lines each, in the beat where they fire. Deliberately no line in Beat 7 —
the silence after the fragment is what makes the Color Thief's silhouette land.)*
