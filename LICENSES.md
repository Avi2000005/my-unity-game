# Third-Party Licenses

Every asset in this project that did not originate here, and what allows us to use it.
Keep this file updated the day you download anything — tracking licences later is
far more painful than tracking them now.

---

## Quaternius — Medieval Village MegaKit (FREE version)

- **Source:** https://quaternius.com/packs/medievalvillagemegakit.html
- **Author:** Quaternius (https://www.patreon.com/quaternius)
- **Licence:** CC0 1.0 Universal (public domain dedication)
  https://creativecommons.org/publicdomain/zero/1.0/
- **Version used:** FREE / "Standard" — 176 FBX models + 29 PBR textures
- **Location in project:** `Assets/Art/Village/`
- **Original licence file:** `Assets/Art/Village/LICENSE_Quaternius_CC0.txt`

### What the free version does *not* include

Worth knowing, because it dictates how much work this project carries:

- Only a portion of the full model set.
- **No prefabs.** Every building must be assembled by hand from loose pieces.
- **No Unity materials and no `.unitypackage`.** The models are raw DCC source
  (FBX/OBJ/glTF), so materials are authored by us in `Assets/Painterly/Materials/`.
- No ready-made Unity/URP project. The paid SOURCE tier is the one that ships
  Unity(URP) scenes and custom shaders pre-wired.

Because it is CC0, none of the above restrict us — we just have to do the assembly.

---

## Other planned sources (not yet imported)

| Source | Licence | Intended use |
|---|---|---|
| [Kenney](https://kenney.nl/) — Input Prompts, UI Pack, audio | CC0 1.0 | "Press E to Paint" prompt, hotbar frames, SFX |
| [Poly Haven](https://polyhaven.com/) | CC0 1.0 | HDRIs (overcast / "Abandoned & Ruins") for the grey sky |
| [ambientCG](https://ambientcg.com/) | CC0 1.0 | PBR textures, if we move off untextured |
| [Poly Pizza](https://poly.pizza/) | CC0 1.0 | One-off fill-in props (fences, wells, buckets) |
| [Freesound](https://freesound.org/) | CC0 / CC-BY — **check per file** | Grey-world ambience. Attribution required for CC-BY |
| [Mixamo](https://www.mixamo.com/) | Free with an Adobe account, Adobe's terms | Locomotion animation for Ari (no weapons needed) |
| [Sonniss GDC bundles](https://sonniss.com/gameaudiogdc) | Free, own licence terms | Higher-fidelity SFX |

> If any Freesound file is CC-BY, credit the author in a third section below and in
> the game's own credits screen.

---

## Our own code

Everything under `Assets/Painterly/` is original to this project — the desaturation
shader (`Echoes/PainterlyLit`), the colour-restore runtime, the material builder and
the village generator.

`Echoes/PainterlyLit` is derived from Unity's Universal Render Pipeline `Lit` shader
and includes URP's `LitInput.hlsl` / `LitForwardPass.hlsl` at runtime. URP ships under
the Unity Companion License; see `Packages/manifest.json` for the pinned version
(URP 17.6.0).
