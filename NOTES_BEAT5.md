# Beat 5 — Ink Crawler. COMPLETE, 44/44 PASS.

The level's own hardest design constraint: **the brush is not a weapon.** It only
ever *staggers*. Ari cannot kill, and nothing in the beat has a kill path.

## What was built

`L1_Beat5` — yard x 38–54, z 4–13. Enter at 38, exit at 51, 13 m of beat.

- **`InkCrawler.cs`** — `State {Dormant, Alerted, Closing, Lungeing, Staggered, Spent}`
- **`Beat5Director.cs`** — `Phase {Waiting, Engaging, Done}`
- **`Beat5Setup.cs`** — builder + 44 checks + no-kill audit
- **`AriFit.cs`** — separate; fits Ari's mesh to her capsule (see below)

## The two lanes — this is what the beat is

A ruin wall runs east–west at **z 8.2** with a **2 m gap at x 46–48**. The
crawler is posted at **z 10.5**, 2.3 m north of it, so sight lines cross at
different places and there is no single safe spot.

| Route | z | Hidden | Verdict |
|---|---|---|---|
| South (stealth) | 6.4 | **78%** of 13 m | first exposed at 6.5 m |
| Along the wall | 8.2 | 89% | first exposed at 8.5 m |
| North (fight) | 12.2 | **0%** | exposed from 0 m |

North is honestly exposed — that is where it can reach her. South is worth
something. The gap is a real opening, sampled clear at x 47.

## The numbers that make it fair

| | |
|---|---|
| notice / forget | 7 m / 11 m (hysteresis, so it does not flicker on an edge) |
| crawl / close speed | 1.50 / 2.60 — **both below her 2.20 walk and 3.60 run** |
| lunge | 2.20 m throw, 0.55 s |
| standoff | 1.30 m; his 0.34 capsule's near face is 0.96 m from her centre vs a 1.60 m stroke — **0.64 m to spare** |
| stagger | 1.40 s vs a 0.55 s lunge — a clean hit wins outright |
| splash reach | an **arm**: 1.60 m, not the brush's 6.00 m colour radius |
| his eye | 0.74 m, so the 2.20 m wall hides him and the **0.55 m** low wall honestly does not |

A creature faster than the girl makes stealth meaningless, so every one of his
speeds is below hers. Walking east walks *towards* him, not away — the exit is
4.6 m from his post, inside his 7 m notice, so geometry cannot end the pursuit
on its own. `InkCrawler.Retired` is what ends it: once he gives up he goes home
and never re-notices for the rest of the beat.

## No kill path — checked, not assumed

- No member of `InkCrawler` mentions health, damage, hurt, die, kill, death.
- `InkCrawler.cs` mentions the death trigger **only in comments**.
- The controller has 4 transitions into `Crawler_Death`; none are reachable,
  because nothing sets the trigger.
- `Crawler_Death` and the `Die` trigger are never used. `Crawler_Attack` doubles
  as the stagger clip.

## Ari's height — a cross-beat fix, in its own tool

Her collider is **1.80 m** — the number every beat measures from (1.23 m crawl
band, 1.10 m ceiling, 0.35 m step). Her mesh was **2.02 m**, so 22 cm of her
head was outside the only body physics knew she had.

`AriFit` scales the **mesh**, not the capsule: raising `bodyHeight` to 2.02 would
have re-opened Beat 3's gully, Beat 4's crawlspace and its 26 checks. Scale
**0.8930**, origin moved so her feet stay planted. Beats 3 and 4 are untouched.

> A skinned mesh's bounds cover **every pose its animation reaches**. Measured
> against a standing height they always over-report, never under-report — so a
> *pass* on a seeded measurement proves very little. Bake it.

## Test

`N` warps to `beat5_enter` at (39, 0, 6.4) — 10 m from his post, outside his
7 m notice. `N`/`B` to move between stops. `Ctrl+P` to exit.

**Needs the user's eyes:** Ari is 1.80 m now and was 2.02 m. She is 13, so she
still reads tall, but 22 cm closer. Check her at a doorframe and in Beat 4's
crawlspace.