# DLSS 5 — Cyberpunk 2077 follow-up: 0x887A0006 crashes, update 2.5 and the Perfect City modpack

**Version française :** [DLSS5_Cyberpunk2077_suivi.md](DLSS5_Cyberpunk2077_suivi.md)

**Date: October 23, 2026.** Follow-up to `DLSS5_310.9.16_RTX4070_EN.md` (the
compat-fp16 DLL build of October 19). This document covers what was not yet
documented: the investigation of the Cyberpunk 2077 crashes, the decisive test
protocol, and the plan for the modpack.

## 1. The problem

- Crash **0x887A0006** (DEVICE_HUNG) in **Cyberpunk 2077 only** (observed on
  October 19), while the same DLSS 5 chain (ReShade/OptiScaler + compat-fp16 NR DLL)
  is stable in Dead as Disco and Crime Scene Cleaner.
- Possible suspects: the modpack, the NR chain itself, the GPU overclock, or the
  pressure on the **12 GB of VRAM** of the RTX 4070 (the modpack's 4K texture packs
  are hungry).
- Method adopted: **one variable at a time**. No verdict without an isolated test.

## 2. Context — update 2.5 "Edgerunners 2" (October 22, 2026)

- **Native DLSS 4.5** (Multi Frame Generation up to 6X) — the game no longer needs
  the external chain for recent DLSS, which makes a clean vanilla test possible.
- **Official mod support via mod.io** (achievements are disabled on modded saves),
  prequel quest "Clampdown", dual-wielding ("Double Trouble" perk).
- The modding frameworks (CET, RED4ext, redscript, ArchiveXL/TweakXL/Codeware) need
  their own 2.5 versions after the patch. As of October 23, no confirmation that
  everything is ready — that is what blocks resuming the modded playthrough.

## 3. The decisive test protocol

Preparation:

- **Save backup**, kept intact throughout all tests.
- **Vortex purge**: removes from the game folder everything Vortex deployed
  (reversible, Deploy restores everything). ⚠️ The purge does NOT cover manual
  installs: **ReShade (`dxgi.dll`), OptiScaler and the NR DLL must be removed by
  hand** in `bin/x64`, otherwise the "vanilla" test is not one.
- **Stock clocks**: in MSI Afterburner, **Reset + Apply** and uncheck applying the
  settings at startup, verified in HWiNFO. Closing Afterburner/RTSS resets
  NOTHING — the offsets live in the NVIDIA driver. The card (Gigabyte RTX 4070
  Windforce OC) is factory-OC: high default clocks are normal.

Steps:

1. **Total vanilla** — new game (the only advanced save is modded), native DLSS 4.5,
   no mods, no external chain, stock clocks.
2. **DLSS 5 chain alone** — ReShade/OptiScaler + NR DLL reintroduced, still no mods.
3. **Up-to-date modpack** — only when the pack's 2.5 revision exists (see section 5).

Reading the verdicts:

- Stable at step 1, crash at step 2 → the NR chain (or the OC) is suspect #1.
- Stable at steps 1-2, crash at step 3 → the modpack is guilty.
- Nuance: 2.5 is day-1 code, it can crash on its own; an isolated crash concludes
  nothing without reproduction.

## 4. Results

- **Step 1 (evening of October 22): SUCCESS** — vanilla session on a new game,
  **zero crashes, no problems**. The base (game 2.5 + driver + hardware at stock
  clocks) is healthy.
- **Step 2: planned for the evening of October 23.**
- Step 3: waiting for the modpack's 2.5 revision.

## 5. The "Perfect City" modpack and resuming the advanced playthrough

- Pack used: **Perfect City, by Lilevitchi** — Nexus Mods collection managed by
  Vortex, **versioned by game patch** (the pre-2.5 version was called
  "Perfect City 2.31").
- **Resuming the advanced playthrough**: possible in principle when the same mod
  set returns in 2.5 versions, under the following conditions:
  - wait for the pack's 2.5 revision published by Lilevitchi (who himself depends
    on the 2.5 frameworks) — do not improvise a partial update;
  - **never load then re-save** the old playthrough in a "missing mods" state: the
    losses of items/systems would be frozen into the save;
  - first load after the complete update → check inventory, appearance and
    systems, then save in a **new slot**. The backup remains the parachute;
  - residual risk: if the 2.5 revision removes or replaces mods, their linked
    content may disappear from the playthrough.
- **Lightening plan** (to apply after the tests, when reinstalling the pack):
  remove the **4K** texture packs, keep the **2K**, compensate with DLSS 5 —
  objective: relieve the 12 GB of VRAM. The 4K packs would be optional at the
  collection's installation (to be confirmed on the 2.5 revision's installation
  screen; otherwise, disable in Vortex afterwards, to be redone after each
  revision). Technical nuance: DLSS upscales the final image, it does not
  regenerate textures — at 1440p, the 2K/4K difference is mostly visible on
  close-up elements (weapon in hand, faces).
- **Long-term mod strategy** (decided on October 21): as authors republish, prefer
  the official mod.io version when it exists, Vortex for the rest; never both
  versions of the same mod; a single master for the load order; freeze automatic
  updates during an ongoing playthrough.

## 6. Current state (October 23, 2026)

- openNR 310.9.16 build: documented and validated (main document).
- Decisive test: step 1 validated, step 2 to do, step 3 waiting for the pack.
- Advanced modded playthrough: paused, backup intact, resumption conditioned on
  the 2.5 revision of Perfect City.

## 7. Lessons

- **One variable at a time** — without the clean vanilla test, the crash could
  have been attributed to the wrong suspect indefinitely.
- **A manager's purge does not cover manual installs** — a "vanilla" test is
  verified file by file.
- **Closing overclock software does not restore stock clocks** — the offsets are
  in the driver; only an explicit Reset removes them.
