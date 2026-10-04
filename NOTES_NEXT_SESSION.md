# Handover — blur/text investigation

Written at the user's request, so the next session starts from the numbers
instead of from scratch. Every claim below was measured, not guessed.

---

## 0. READ THIS FIRST — the one bug that caused every other bug

Beat 5 took seven builds to go from 32/10 to 44/0. Almost none of that was
Unity, geometry, or design. **Six of the seven rounds were a measurement
reporting a confident answer that its own evidence contradicted.** In order:

| # | What it printed | What was true |
|---|---|---|
| 1 | yard floor **1.10 m**, "not flat enough to fight on" | the probe was hitting **the crawler it had built last run** and calling it the ground |
| 2 | "nothing unexplained in the yard — **4 colliders**", with all four named `path L1_Beat5` above | a missing `continue` double-counted this beat's own furniture as intruders |
| 3 | Ari "**0.46 m** too tall" | she was 0.22 m too tall. A skinned mesh's bounds span the whole animation, not one pose |
| 4 | a guard: "cannot set local position safely at depth" | perfectly safe — and it was **hiding** an arithmetic error underneath it |
| 5 | a guard: "rotated node, scale is not a number" | unnecessary: a **uniform** scale commutes with rotation |
| 6 | feet "**drifted 16 mm**" | unpassable check — it asked a breathing rig to be frozen |
| 7 | "drift **0.000 m**, which is idle sway" | there was no sway. The number was right, the sentence was invented |

### The rule, and why row 7 is the worst

**A check may state what it measured. It may not name a cause it did not
measure.**

Row 7 is the worst because it was committed by the check written to prevent
rows 1–6, on the fourth rewrite of the same assertion. It is the error the
project keeps making, and nothing stops it making itself. A number is not a
finding; a number plus a story is a finding, and **the story is what a reader
acts on**. Pick the sentence from the number:

```csharp
sb.AppendLine(delta > 0.0005f
    ? "differ by " + d.ToString("0.000") + " m — that is movement"
    : "same place, to within " + d.ToString("0.000") + " m");
```

Two smaller forms of the same thing:

- **Seed a measurement from an assumption.** `float top = 0f, bottom = 0f`
  reads as a safe identity and silently assumes the model straddles the world
  origin. Nothing here does. It reported the crawler as 0.96 m on one build and
  1.97 m on the next — same asset, same scale, differing only by the seed.
- **Measure in the wrong frame.** `localPosition.y = worldY − rootY` is the
  local Y only when the parent *is* the root. One level deeper it is out by the
  parent's own height — and a guard fired first, so the number was never
  produced and the bug survived a "successful" refusal.
- **Compare against something you invented.** Deriving a reference instead of
  reading the game's own (`soleOffset` read by reflection, not recomputed)
  means comparing the character against your own invention.

### Two guards that were worse than no guard

Both refused correctly and for the **wrong reason**, which meant the real
arithmetic error beneath them never got fixed and the reader believed the case
was handled.

- *"not a direct child, so local position cannot be set safely"* — it can;
  assign `.position`.
- *"rotated, so lossyScale is not a number"* — true, and irrelevant, because
  **uniform scale commutes with rotation** and uniform was what was wanted:
  she should be shorter, not thinner. `node.position = feet − k·(feet − node.position)`
  needs no matrix inverted and no axis assumed.

### And one check that could never fail

`AriFit`'s early-out printed "already fits" and returned *before any check*.
So the run that applied the correction FAILED on a bad assertion, the next run
exited without checking anything, and the tool had **never once reported PASS**.

**A check that runs only on failure has never confirmed anything.** If the final
state matters, the "no change needed" path runs the assertions too.

---

## 1. The blur — cause found, fixed, but the fix does NOT stick

### Root cause

The Game view was rendering the game at **876 x 453** (later **876 x 373**) while
the monitor is **1920 x 1080**. Unity draws into that panel and scales the image
to fit, so every pixel was being stretched about 2.2x. Nothing in the project
settings can repair that, because the detail was never drawn.

This is why changing pipeline settings alone did not fix it.

### What was changed and verified

| Setting | Before | After | Verification |
|---|---|---|---|
| Game view resolution | 876 x 453 | 1920 x 1080 | `camera after: 1920x1080`; read back from the game's own size list: "index 3 is 1920x1080 ... OK" |
| Game view panel | docked 876 x 474 | maximised 1528 x 709 | `maximized=True` |
| MSAA, PC_RPAsset | 1x | **4x** | read back after write: `msaa=4x` |
| MSAA, Mobile_RPAsset | 1x | **4x** | read back after write: `msaa=4x` |
| Mobile_RPAsset renderScale | 0.800 | **1.000** | read back: `renderScale=1.000` |
| Texture anisotropy (40 textures) | 1 (= none) | **8** | `still below 4 after reimport: 0`; materials read back `aniso=8` |

**Both** URP assets were fixed, not just the editor default. The project has two
quality levels: level 0 `Mobile` -> `Mobile_RPAsset`, level 1 `PC` ->
`PC_RPAsset`. Fixing only `GraphicsSettings.currentRenderPipeline` would have let
the blur come back whenever the quality level changed.

### THE PART THAT IS NOT SOLVED

**The game view size reverted.** Measured later in the same session:

```
editor game view: 876 x 373
camera renders:   876 x 373
```

so `win.maximized = true` did not survive. Adding new editor `.cs` files causes
a domain reload, which appears to restore the panel. Do not add more editor
scripts and then expect the panel to stay maximised.

### What the user has to do

**Double-click the "Game" tab** to maximise that panel, then confirm the number
next to the resolution dropdown reads **1920 x 1080**. Anything smaller and
everything is soft again, no matter what the project settings say.

---

## 2. The text — same cause, plus one real separate bug

### Same cause

```
'ScreenControls' ScreenSpaceOverlay, CanvasScaler ScaleWithScreenSize
referenceResolution: 1920 x 1080, matchWidthOrHeight = 0.50
screen being resolved against: 876 x 373
factor the screen gives: 0.401
```

The canvas was scaling every glyph to **40%**. Once the game view is genuinely
1920 x 1080 this factor is **1.000** and text is drawn 1:1. So text blur is not a
separate problem — it is the small game view again.

### A genuine second bug, independent of resolution

```
'ScreenControls/STOP/Label' text="STOP" size=28 parentScale=0.397 effective=11.1px
'ScreenControls/Run/Label'  text="RUN"  size=28 parentScale=0.397 effective=11.1px
```

`lossyScale` of **0.397** is almost exactly the canvas factor of 0.401, which
means the canvas scale is reaching the transform as well and the two are
compounding. A 28px font ends up 11px on screen and bilinearly filtered from a
256x256 `LegacyRuntime` atlas. That is soft at any resolution.

Not yet traced to its cause. Where to look:

- `Assets/Painterly/Scripts/OnScreenControls.cs`
  - line 123-128: canvas created with `ScaleWithScreenSize`, reference 1920x1080
  - line 215: `t.fontSize = 28`
- Search the hierarchy for a non-unit RectTransform scale on `ScreenControls`.

The fix is to make the canvas 1:1 with screen pixels and remove the stray
transform scale, not to keep raising the font size.

### A third text issue, not yet looked at

Font sizes are computed from the game view height:

- `Assets/Painterly/Scripts/BeatHud.cs` line 150:
  `fontSize = Mathf.RoundToInt(19f * Mathf.Max(0.7f, Screen.height / 720f))`
- `Assets/Painterly/Scripts/MonoCompanion.cs` line 362:
  `fontSize = Mathf.RoundToInt(22f * Mathf.Max(0.6f, Screen.height / 720f))`

At a 373px tall game view these clamp to the floor (13px and 13px). So the HUD
text also gets *smaller* when the game view is small — which makes the whole
thing look worse than it is. These need to be driven by the design
resolution, not by whatever the panel happens to be.

---

## 3. Ruled out, so nobody measures these again

- **Bloom is innocent.** Measured by rendering the frame twice and tallying every
  pixel, not by reading the setting:
  ```
  above the bloom threshold (1.0): 0.05%     only a few highlights glow
  spread (95th - 5th):              0.269    contrast is present
  near white (>0.85):               0.00%    no veil
  ```
- **Render scale on the active asset was 1.000.** Not the cause.
- **Texture filtering:** all 40 textures were full size with mipmaps. Aniso was 1,
  which is now fixed to 8.
- **No DepthOfField or MotionBlur** in the post stack.
- **Camera FOV 55** — normal.

Minor, not blur: camera far clip is **1000** while the village needs about 120.
Worth trimming; it costs depth precision for nothing.

---

## 4. Rough edge worth knowing

The `Global Volume` has **no profile asset assigned** (`GetAssetPath` returns
empty), so bloom/vignette/tonemapping run on URP's built-in runtime defaults
rather than on anything saved. Harmless right now, but any post-processing
tuning will not persist unless a profile asset is created and assigned.

---

### `Bounds` has no box-to-box distance

`Bounds.SqrDistance` takes a **Vector3**, not another `Bounds`. For AABB-to-AABB
use `Beat4Setup.Gap()`, which computes it per axis and clamps each to zero, so
touching or overlapping boxes correctly report `0` rather than a negative gap.
That zero is what the "do they touch?" check asserts on.

## 4b. Level build state, and the measurements that drive it

**Beats 1–4 exist. 5, 5.5, 6, 7 do not.**

| Beat | State | Container |
|---|---|---|
| 1 Awakening | built | — |
| 2 movement, brush, Echo Trail | built | — |
| 3 Mono Awakens | built, handover on a 3.2 s timer | `L1_Beat3` |
| 4 gate + crawlspace | **PASS 26/26, scene saved** | `L1_Beat4` |
| 5 Ink Crawler tutorial | **to do** | |
| 5.5 alley gauntlet | **to do** | |
| 6 first Colour Fragment | **to do** (the `beat6.rung1..3` lines exist, muted) | |
| 7 Color Thief on the hill | **to do**, and there is no hill | |

### The three numbers every tight space in this game is built from

Measured by `CharacterSizeProbe.cs`, report `Temp/character_size.txt`. **Do not
re-derive these by eye — two of them have already been got wrong once.**

- **Ari, swept capsule 1.80 m.** Not her mesh (2.42 m) and not a collider —
  she has no collider at all, physics knows her as a `Physics.CapsuleCast`
  inside `AriMover`. Radius 0.30, step height 0.35, jump peak measured 1.145 m.
- **Mono, capsule 0.57 m.** Local height 1.80 m, local radius 0.40 m, centre
  `(0, 0.90, 0)`, at `localScale 0.31`. So any tool reading his collider
  without applying `lossyScale` gets 1.80 and thinks he is as tall as Ari.
- **Mono, head at 0.63 m**, world, from `Renderer.bounds` max.y.
- **Mono, source mesh 1.70 m** at scale 1 → **0.53 m** at 0.31.

The band for a crawlspace is therefore **0.57 m to 1.80 m** on governing
heights, or **0.63 m to 1.80 m** if you insist on clearing his visible head.
Beat 4's **1.10 m** sits inside both: **0.47 m** over his head, **0.70 m**
under hers.

### ⚠ Mono's mesh is 12 cm inside the floor — UNRESOLVED, ask the user

`CharacterSizeProbe` measures Mono's placed mesh at world y **−0.12 to 0.63 m**.
His `CapsuleCollider` bottom is at exactly **y 0.00**, so his feet are correctly
on the ground but **12 cm of his visible body is below it** — 22% of a 53 cm
character, which would read as standing in a shallow hole.

Either the FBX's origin is not at his feet, or the renderer child is offset.
I cannot judge this by eye — no image input — so it needs the user's eyes.
If confirmed, the fix is to raise the renderer child by `0.12 / 0.31 ≈ 0.387`
local units.

**Do not "fix" this by measuring.** Both 0.53 and 0.75 are defensible numbers
for different questions; the world bounds are what a roof actually hits, so
Beat 4's checks use `Renderer.bounds` and 0.63 m.

### `MonoVisible` — how not to measure him

A third wrong answer appeared while building Beat 4, and it is the subtlest of
the three. Reading `SkinnedMeshRenderer.localBounds` and multiplying by
`lossyScale` gives the mesh's height **in its own file**, not how tall Mono
stands — it knows nothing about where the renderer sits under his root. It
reported **0.42 m** when he is **0.75 m**.

Consequence if missed: the crawlspace gets built 33 cm lower than it should be
and his head goes through the roof.

Correct form — transform the centre by the renderer's transform, scale the
extents:

```csharp
var b = smr.localBounds;
var centre = t.TransformPoint(b.center);
var s = t.lossyScale;
top    = Mathf.Max(top,    centre.y + b.extents.y * s.y);
bottom = Mathf.Min(bottom, centre.y - b.extents.y * s.y);
```

**Use `Renderer.bounds` and skip all of that.** It is already world-space and it
is what `CharacterSizeProbe` does, so both tools agree by construction:

```csharp
foreach (var r in go.GetComponentsInChildren<Renderer>(true))
{
    var b = r.bounds;                        // world, includes the placement
    top = Mathf.Max(top, b.max.y);
}
```

Beat 4's first cut used `localBounds` and got 0.42 m against a true 0.63 m.

### CORRECTION (Beat 5) — `Renderer.bounds` is ALSO wrong, for a skinned mesh

The rule above says "use `Renderer.bounds` and skip all of that", and that is
right about *placement* and wrong about *pose*. Both mistakes are real and they
cancel, which is why the correction only shows up when you compare the result
against something you already trust.

`SkinnedMeshRenderer`'s bounds are `localBounds` transformed by the node's world
matrix, and `localBounds` is baked **once at import** to cover every pose the
animation can reach — the deepest crouch, the top of a jump, a reach. It is not
the character's current shape and it is not their standing height.

Beat 5 measured Ari that way and reported **2.26 m** against a **1.80 m**
capsule — a 0.46 m overshoot on the player character, which is 26% of her
height and more than the whole of the error that was actually there. The
numbers looked like a serious rig fault and pointed at three expensive
repairs: rescale her, reimport her FBX, or raise her collider and destroy every
clearance number in Beats 3 and 4.

The truth was that **neither number was a standing height.** Her mesh bounds run
−1.175 to 0.953 *in the mesh's own file* — 2.128 m — which is how far her body
travels across the animation set, not how tall she is.

**The authority is a bake, which is what `AriMover` already does for
`soleOffset`:**

```csharp
var skin = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
skin.GetComponentInParent<Animator>()?.Update(0f);
var baked = new Mesh();
skin.BakeMesh(baked);
var m = skin.transform.localToWorldMatrix;
float lo = float.MaxValue, hi = float.MinValue;
foreach (var v in baked.vertices)
{
    float y = m.MultiplyPoint3x4(v).y;
    if (y < lo) lo = y;
    if (y > hi) hi = y;
}
Object.DestroyImmediate(baked);      // editor; Destroy at runtime
float standing = hi - lo;
```

Rules that came out of it:

1. **A skinned mesh has three heights and they are all different** — bind-pose
   `localBounds`, current-pose `bounds`, and the baked pose. Only the third is
   a person. Name which one a number is when you print it; "visible height"
   named all three at different times.
2. **Print both numbers when they disagree.** Seeded and baked, side by side.
   The whole diagnosis took one build because both were on the page.
3. **Comparing a seeded bound to a standing height always over-reports**, by
   the animation's vertical range. It cannot under-report, so a *pass* on a
   seeded measurement means very little — it should never be trusted to prove
   a fit.

### And the probe itself had the inactive bug

`CharacterSizeProbe` was **also** calling `GameObject.Find("Mono")`, so it
reported "no GameObject called 'Mono' in the scene" and fell back to measuring
the **FBX asset** — 1.70 m at scale 1. It concluded:

> Ari 1.80 m, Mono 1.70 m, so the band is **0.10 m** wide.

The real band is **1.23 m**. Mono was asleep in the scene the whole time, and
*asleep is not the same as absent*. All three `GameObject.Find` call sites in
that file now go through `Named()`. **If a probe reports a missing character,
check `activeInHierarchy` before believing it.**

### Where the level's landmarks are

From `WhereIs`'s landmark dump. `WhereIs` reads its box from
`Temp/whereis_box.txt` (six numbers: minX minY minZ maxX maxY maxZ), so it can
be pointed anywhere **without recompiling** — write the file, run it again.
That is the cheapest tool in the project; use it before adding geometry.

```
Fountain / Statue        (0, 0)        Beat 6, and it is the origin
SleepingTree             (8.1, 5.9)    Beat 3
Ari                      (7.1, 12.8)
Mono                     (7.1, 12.1)
Gully walls              x 14 to 28, z 6.0 and 8.0
GullyLeg_1..4            (14,7) → (28,7)
House_35_0               x 31.9 to 37.9, z 1.5 to 3.5
Beat 4                   x 28.5 to 33, z 4.4 to 10
```

Beat 4's site was chosen by measurement, not taste: **x 28–34, z 4.4–10 is
empty of every collider except `Ground`**, flat at 0.00 m. It is the strip
between the gully's east end and House_35_0. Nothing had to be moved.

Note `House_35_0` only reaches z ≈ 3.5, so the whole line east of the gully at
z > 4 is open. Beats 5 and 5.5 have room.

### Beat 4 as built — `L1_Beat4`, report `Temp/beat4_setup.txt`

Everything is measured off `WhereIs` first and re-measured after. The gate wall
runs north–south at x = 30.00 and is **9.5 m long (z 2.5 → 12.0)** on purpose:
in open field a 0.40 m thick wall with a gate in it can simply be walked round,
and a gate you can walk round is not a gate.

```
  z 2.50 ┌──────────────────────────────┐
         │  Beat4_Wall_S                │      2.60 m tall, 0.40 thick
  z 6.20 ├──────────┐   ┌───────────────┤
         │          │GATE│               │      gate 1.60 w x 2.10 h,
  z 7.80 │          └───┘               │      slides 2.25 m DOWN
  z 8.60 │        ┌──┴───────────────────┤      pier 0.80 m
         │        │CRAWL  0.90 wide      │      ceiling 1.10 m
  z 9.50 │        └──────────────────────┤      tunnel x 29.50 → 32.90
         │  Beat4_Wall_N                │
  z 12.0 └──────────────────────────────┘
```

- **Gate** slides **down**, not up. The wall is 2.60 m and the opening 2.10 m,
  so lifting would put the slab 1.85 m above the parapet, hanging in the sky
  where the player watches it. Down puts it 0.15 m **under the ground plane**,
  which the existing `Ground` collider hides for free.
- **Plate** at (28.95, 5.70), plinth 0.30 m — under her 0.35 m step height, so
  she walks on without jumping. 1.61 m from the gully mouth, and clear of the
  gully walls because those end at x 28.00.
- **Switch** at (32.55, 9.05) behind the tunnel's far wall. Reach 1.00 m against
  Mono's 0.70 m errand radius, so him stopping short still puts him in reach.
- **Errand point** (32.30, 9.05) — 2.80 m in, she can see 2.00 m.
- **Mouth is 0.90 m wide against her 0.60 m body**, so width can never be why
  she is stopped. It is her height, and the level should read that way.

### The four checks that make this beat or break it

A build without these would have shipped. The gate once stood 5 m from its own
opening and nothing else complained.

1. **Ari's own capsule sweep against the slab, shut and open.** Her sweep is a
   `Physics.CapsuleCast` because she has no collider, so this is the only test
   that uses the shape physics actually gives her.
2. **Headroom down the tunnel by ray, min/median/max.** Built 1.10, measured
   1.10 flat over 15 samples.
3. **The two bodies against that ceiling** — the band check.
4. **Every private `[SerializeField]` read back by reflection.** All the wiring
   is private on purpose, which means a builder that failed to assign one
   produces a beat that looks built and is not. `Src<T>()` is that check.

---

## 5. Probes written this session (all under `Assets/Painterly/Editor/`)

| File | What it answers |
|---|---|
| `BlurProbe.cs` | every knob that can soften an image: render scale, MSAA, post stack, texture sampling, per-camera overrides |
| `BlurFix.cs` | which URP asset actually draws at runtime (all quality levels), and applies the fixes with read-back |
| `GameViewFix.cs` | sets the game view to the monitor's resolution through the internal API |
| `PostProbe.cs` | the real parameter values of the active volume overrides |
| `FrameProbe.cs` | renders the village and tallies per-pixel luminance, to decide bloom by measurement |
| `TextProbe.cs` | canvas scaling factors and per-label font size, parent scale and atlas |

Delete them once the blur work is closed; they are diagnostics, not game code.

### Keep these — they are how the level was built

| File | What it answers |
|---|---|
| `CharacterSizeProbe.cs` | how big Ari and Mono actually are. **The authority.** |
| `WhereIs.cs` | what is in any box in the village, plus landmarks, ground, headroom and lane width. Box comes from `Temp/whereis_box.txt`. |
| `CrawlspaceMap.cs` | headroom over the whole village, as a map of low regions |
| `WidthMap.cs` | free lane width over the whole village |
| `GullyBuilder.cs` | the build-then-re-measure pattern. Copy this for any new geometry. |
| `Beat3Setup.cs` / `Beat4Setup.cs` | the two built beats, and their Verify sections — **copy the Verify, it is the point** |
| `SwingClipProbe.cs` | per-clip hand speed, for the brush |
| `WakeClipTrim.cs` | owns the applied 5.07–8.03 s trim of `Mono_Wake` |

### Three bugs these probes had, all of which produced confident false answers

Worth knowing before trusting any of them again:

1. **`Floor()` took the *lowest* downward hit**, which lands on the base ground
   collider *below* the thin path slabs. Every upward ray then started ~2 cm
   under the real surface and hit the slab's own underside. Fixed by starting
   0.15 m up and taking the lowest of all upward hits. **`WidthMap` still has the
   old rule** — its widths are 3–5 cm low, harmless, but do not trust it for
   anything finer.
2. **Capsule standing height is `centre.y + height/2`** — no radius term. The
   wrong formula reported Mono at 2.30 m.
3. **Capsule dimensions are local.** Mono is scaled 0.31.

Bug 2 and 3 both reported Mono as taller than Ari, which very nearly designed
Beat 4 out of existence.

### And a fifth: a test whose verdict was the reverse of its own evidence

Beat 4's gate check swept a capsule of Ari's size eastward along the gate line,
once shut and once open. `Swept` returns **true for a collision**, and the open
case passed that raw boolean straight into `Check(..., ok: open)`. The report
therefore printed `gate open: walks through` — correct — and
`FAIL the open gate lets her through` — backwards, in consecutive lines.

Both variables are now named for what they mean (`blockedShut`, `blockedOpen`)
before anything asserts on them. **When a check and the line it prints
disagree, the printed line is usually the one that is right**, because it was
written from the measurement and the assertion from memory.

### And a report that contradicted itself again

`call-recompile.json` returned:

```json
{ "status": "completed", "failed": false, "errors": [], "compilationFailed": true }
```

All three fields at once, and two of them say the compile was fine. Unity's
`Editor.log` had no `error CS` lines either, and the next `call-beat4.json`
compiled and ran clean — so the compile had succeeded and `compilationFailed`
was stale.

`compilationFailed` is not in this project's code or in `Tools`; it comes from
the Pipeline package and is not ours to reason about from here.

**Treat `failed` + `errors` as the truth and `compilationFailed` as a hint.**
Two independent signals that disagree means the compile is worth re-testing
directly, not a reason to go hunting through the source. The cheapest way to
settle it is to run the next builder: it either compiles or it does not.

### And a cascade that dressed up as five unrelated errors

One missed type argument in `Beat4Setup` produced this, in order:

1. `Src(warp, "stops")` → **CS0411**, type arguments cannot be inferred.
2. Project recompile **fails**, so `BeatWarp` never enters the project
   assembly.
3. `run_script` compiles `Beat4Setup.cs` alone against that assembly, so it
   cannot see `BeatWarp` — and reports **five CS0246 "type BeatWarp could not
   be found"** errors, all pointing at the warp, none of which were about it.

Five errors, one cause, and the compiler's message named the innocent type
rather than the line that broke the chain. **When several of the same error
appear at once in one file, they are one error.** Fix the first, re-read, and
expect the rest to go with it — do not start editing the type it names.

Related: `run_script` only ever compiles the file it is given, so a **new type
in a new file** is invisible to it until a project recompile has succeeded at
least once. `call-recompile.json` first, sleep ~45 s, then the builder.

### And a warning that lied, twice

`Beat4Setup` passed `"bodyHeight"` — the **field** name — where it wanted the
**property** `BodyHeight`. Reflection found nothing, the fallback read the
private field, got the right number, and printed:

> `warn Ari.bodyHeight came from the field, not the property — the project
> assembly looks stale — run call-recompile.json`

That was a lie. Two recompiles did not clear it, and *that* is what proved it
was a lie: **no amount of rebuilding fixes a misspelt name.**

Generalise it — when a check says it needs step A, run step A, and the
complaint survives, the complaint is wrong. Do not keep rebuilding; re-read the
message and check the name it names.

### And a sixth, in the footprint check itself

`Collect()` walked only the children of the container, never testing the node it
was handed. Every wall and roof the tool had just built was therefore
"foreign", and Beat 4 failed its own footprint check 22 times over. Worth
naming because a check that fails on its own output is a check nobody reads
twice — and the next genuinely foreign collider would have been filed as noise.

The fix also taught the check to separate **village** objects from
**unexplained** ones. `Ground` and `House_35_0` are printed and counted
separately, because a check that flags the floor as a defect trains its reader
to ignore it.

---

## 5b. Bridge commands that exist

`cd Tools`, then `.\mcp.ps1 -Call <file>.json`. **Always `Remove-Item Temp\*.txt`
first**, and every call takes 25–45 s because a changed `.cs` triggers a domain
reload.

| File | Runs |
|---|---|
| `call-whereis.json` | `WhereIs.Run` — repoint by writing `Temp/whereis_box.txt` |
| `call-beat4.json` | `Beat4Setup.Run` |
| `call-recompile.json` | forces a project-assembly rebuild — **needed after adding a property to a runtime script, or reflection against it returns null** |
| `call-recompile-status.json` | checks that rebuild |
| `call-charsize.json` | `CharacterSizeProbe.Run` |
| `call-crawl.json` | `CrawlspaceMap.Run` |
| `call-savescene.json` | saves the open scene |
| `call-gvfix.json` | re-pins the Game view to 1920×1080 |
| `call-controller.json`, `call-swingprobe.json`, `call-status.json` | |

## 5c. Test keys — the user needs these in every message

```
WASD / arrows   walk (camera-relative)
Left Shift      run
Space           jump
LMB             brush swing
G               grey everything          R   restore everything
N               warp to next test stop   B   warp to previous
Ctrl+P          exit play mode
```

### `BeatWarp` — why N and B exist

Level 1 runs ~120 m west to east. Beat 4's gate is at **x 30**, Ari starts at
**x 7**, so testing whether the gate opens meant walking the whole village
every time. A check that expensive doesn't get skipped deliberately — it gets
skipped quietly, and the beat ships untested.

`BeatWarp` is a **separate component on its own object**, deliberately not a
key inside `AriMover`: movement is the one thing in this project that should be
boring to read, and a debug key in there is a debug key forever after. It
disables itself outside the editor unless `enabledInBuild` is ticked, and
`CaptureHere()` (right-click in the Inspector) drops a stop wherever Ari is
standing — better than reading coordinates off the Inspector and typing them
into a script, which is how a beat gets tested in one place and then forgets to
be moved.

Beat 4's two stops are placed by `Beat4Setup` and **checked**, because a stop in
the wrong place fails silently: Ari is a swept-capsule mover with no collider,
so warping her inside the crawlspace raises nothing at all. She just stands
there under a ceiling that was never meant to have her under it, and the beat
looks solved. The checks are "on Ari's side of the gate", "in the opening",
"inside the 2.6 m notice range", "outside the crawl mouth".

---

## 5d. `localPosition` is not `world − root` (Beat 5, `AriFit`)

Ari's skin renderer sits on `Model/char1` — a node named `char1` under a node
named `Model`, under her root. The first cut of `AriFit` wrote:

```csharp
lp.y = nodeWorldY1 - ari.transform.position.y;   // WRONG
node.localPosition = lp;
```

That is the local Y **only when the node's parent is the root**. One level
deeper it is out by exactly the `Model` node's own height — and `Model` is her
hips, so the error would have been about half a metre, moving her whole body.

It did not get that far, because the same block carried a guard:

```csharp
if (node.parent != ari.transform) { Fail("...cannot be set safely..."); }
```

which refused first. **The guard fired on the wrong condition, and in doing so
hid the arithmetic error underneath it.** The reason it gave for refusing —
"a local position cannot be set safely at depth" — is simply not true: assign
`node.position` and Unity converts world→local through the whole chain. So the
tool reported a safety it did not have, and the reader would have believed the
nesting was handled.

Correct form:

```csharp
var wp = node.position;       // world, then let Unity do the conversion
wp.y = nodeWorldY1;
node.position = wp;
```

**Rule: a guard that refuses on the wrong condition is worse than no guard**,
because it converts an arithmetic bug into an apparent safety. Same family as
the gap check that printed "BLOCKED — the two runs are touching" when the cause
was the zero layer mask.

Related, same file: `lossyScale` on a **rotated** transform is a decomposition of
a matrix, not a fact about the node, so dividing by it to recover a local scale
gives a number that does not produce the intended result. `AriFit` walks the
parent chain and refuses if anything is not `Quaternion.identity`, rather than
assuming.

Also fixed in the same pass: the guard originally sat *after* `localPosition`
had already been written, so a failure returned with a half-applied change to
the player character. **All refusing checks go before all writes.**

### The rotated-node refusal was unnecessary, not cautious

`Model` carries a 90°-about-X rotation (the standard import fix for a Z-up
FBX). `AriFit` refused, correctly, on the grounds that `lossyScale` on a
rotated transform "is a decomposition of a matrix rather than a fact about the
node". True, and beside the point: **a uniform scale commutes with a
rotation**, and uniform is what was wanted anyway — she should be shorter, not
thinner.

Under `S' = k·S`, a node-local point goes from `node + R·p` to `node + k·R·p`
whatever `R` is. Height scales by exactly `k`, and the origin that keeps the
feet in place is:

```csharp
Vector3 d = feetAt - node.position;      // feet, in world, already measured
node.localScale  = node.localScale * k;  // relative multiply, not assignment
node.position    = feetAt - d * k;       // world assign, correct at any depth
```

No matrix inverted, no axis assumed, rotation not needed. This also removed a
second error in the original: `feetLocalY = (worldY − nodeY) / lossyScale.y`
assumes world Y maps to local **Y**, which on a 90°-rotated node it does not.

### "The feet did not move" was an unpassable check

After a correct, provably-right change it failed by 16 mm. Two separate
mistakes in one assertion:

**The pose moves.** Two bakes with string-building between them, each calling
`anim.Update(0f)`, so the second is a slightly different idle pose. 16 mm of
foot sway in a breathing idle is nothing. The check could never pass on any
character with an idle animation — it was reporting on the animation, not on
the tool.

**The frame of reference was arbitrary.** A foot is not a fixed point in the
world; it is wherever the hips put it this frame. Asking whether it stayed
still asks the rig to be frozen.

What actually must hold is that **she is standing on the floor** — that her
mesh's lowest point and the soles her collider sweeps agree. That is a claim
about the character, it is stable across a breathing pose, and it is the thing a
player would see if it broke. Two assertions replace the one: gap *changed by*
< 2 cm, and gap is < 10 cm absolute.

`soleOffset` is read by reflection rather than recomputed. It is the game's own
definition of her floor, and re-deriving it here from a collider or the ground
plane would give a slightly different number — which puts you back in the habit
this whole file argues against: inventing the reference and then comparing the
character against your own invention.

### The early-out that made the tool unable to fail

`AriFit` was run four times. The run that **applied** the correction FAILED, on
the bad foot-drift assertion above. The next run found her already 1.800 m and
printed:

```
VERDICT already fits — 1.800 m against 1.80, inside 0.03 m. Nothing was changed.
```

and returned immediately, before a single check.

So: the change was in the scene, the tool had **never once reported PASS**, and
the honest summary was "no run of this tool has ever confirmed the correction."
The early-out turned the tool into something that could only ever agree with
itself — and it did so *exactly when confirmation was needed*, because a
verification step that is skipped on success is not a verification step.

The early-out now suppresses only the **write**. Every check runs either way,
which makes the same code both the tool and the verification of its own result:
"did the write do what it said" and "is the result still right" are one question
asked at different times.

**Corollary: a check that runs only on failure has never confirmed anything.**
If the final state of a build tool matters — and it always does — the path that
reports "no change needed" must run the assertions too.

Related: `var node` had to be hoisted out of the write branch, because the
verification prints the node's current scale and position. Scoping a variable to
the branch that happened to write is how a report ends up unable to state what
state it is in.

### The check committed the exact error it was written to prevent

This is the one to remember.

`AriFit` printed:

```
the two bakes differ by 0.000 m at the feet, which is idle sway
between two poses, not movement caused by this change
```

`0.000 m` was **correct**. **Idle sway did not happen.** The two bakes put her
lowest vertex in the same place to the millimetre — which says the editor pose
is *stable*, and says nothing at all about sway.

The sentence was written first and the number was dropped in underneath it. Same
failure as the gap check that printed "BLOCKED — the two runs are touching" when
the cause was the zero layer mask, and same as `MonoVisible` reporting 0.42 m
for a 0.75 m character — **a confident cause attached to a measurement that did
not support it**. The number was fine. The story was invented, and the story is
the part a reader acts on.

This one matters more than the others, because it was committed **by the check
written to prevent it**, on the fourth rewrite of the same assertion, in the
middle of carefully avoiding exactly this. Four rewrites is not a defence.

Correct form — pick the sentence from the number:

```csharp
sb.AppendLine(poseDelta > 0.0005f
    ? "differ by " + poseDelta.ToString("0.000") + " m — that is movement, " +
      "not sway, and the origin correction is off by that much"
    : "same place, to within " + poseDelta.ToString("0.000") + " m, so the " +
      "correction moved nothing");
```

### A reciprocal printed as if it were the action

The same report said the tool "scaled her by 1.1199". It scaled her by
**0.8930**. 1.1199 is the reciprocal — the size of the *staleness in the cached
number*, not the size of what was done to her. Direction reversed; a reader
would have gone looking for an expansion.

Two different quantities, one printed in place of the other. Report the factor
with its subject attached: "stale by a factor of 1.1199" and "scaled her DOWN by
0.8930" are separate claims and must be separate strings.

### The 6 cm was not a bug, and the report now says so

`soleOffset` is stored as `0.1717`; a fresh bake measures `0.114`. The
difference is `0.058 m`, and `1/0.8930 = 1.1199` — the stored value is stale by
**exactly** the factor the mesh was scaled. `MeasureSoleOffset()` runs in
`Awake`, so it re-derives on every play.

I read that gap as "6 cm of her is buried" and recommended correcting it. It is
**a cache compared against a fresh measurement**, and the check now prints both
numbers plus: *"Do not adjust `soleOffset` to close this gap."*

The general trap: **a measurement that is right in one context and wrong in the
other, printed without saying which, sends someone to repair something that is
not broken.** Editor context vs play context is exactly such a distinction, and
almost every number in this project has both forms.

---

## 6. Still open

---

## 6. Still open from before this session

- ~~Beat 3's handover must run on a timer from `Mono_Wake`~~ — **done**,
  `handoverDelay = 3.2f`, and `Mono_Wake` trimmed to the 5.07–8.03 s window.
- ~~Beat 4 crawlspace~~ — **done**, built and measured.
- Beats 5, 5.5, 6, 7. Beat 7 needs a hill; 288 samples from 55–110 m out are all
  flat. The alley for 5.5 does **not** have to be width-map region 8 at
  (25.1, −8.1) — that was the tightest lane found, not a committed site, and
  with Beat 4 now at x 30 it is 5 m behind the player. Pick one near Beat 4's
  gate with `WhereIs`.
- No real brush-stroke animation exists in the kit. `Ari_Attack` at
  `SwingSpeed = 4.0f` is the stand-in (`swingSeconds = 0.31f`). If a real clip
  arrives, only `SwingClip` in `AriControllerBuilder.cs` changes — but the speed
  and `swingSeconds` must both be re-derived from `SwingClipProbe`.
- Never answered by the user: (a) delete `ari_jumping.fbx` (18 MB, inert), (b) is
  1.2 m jump height right after walking the village, (c) is the placeholder stump
  acceptable.

## 7. Two Unity traps that cost real time this session

**Play mode silently swallows editor tools.** `EditorSceneManager.MarkSceneDirty`
throws `InvalidOperationException: This cannot be used during play mode`, which
aborts a builder *halfway through* — container created, walls not, no report.
Worse than the throw: a tool that skipped the dirty call would report PASS while
writing objects that vanish on stop. `Beat4Setup` now refuses up front and tells
you to press **Ctrl+P**. Never exit the user's play mode for them; ask.

**`GameObject.Find` skips inactive objects.** Beat 3 hides Mono until the tree is
struck, so every tool that looked him up by name reported "no cast in the scene"
for a scene with a cast in it. Use `FindObjectsByType<T>(FindObjectsInactive.Include)`.
`Beat4Setup.Named()` is the version to copy.

## 8. Minor, not done

- Camera far clip 1000 → ~120.
- `Global Volume` has no profile asset, so post-processing runs URP defaults and
  any tuning will not persist.
