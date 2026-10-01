# Crulanda — game brief (what this is, and the bar it is held to)

Updated 2026-09-30. This is the standing direction for the project, gathered from Chris's instructions across sessions.
The handoff (`CLAUDE_HANDOFF.md`) says where the work stands; this file says what the game is meant to be. When they
disagree, this file wins on direction and the handoff wins on current state.

## The goal, in Chris's words

A **high-fidelity, beautiful MMO-style game**, inspired by the feel of classic MMORPGs. Big emphasis on **details,
graphics, lighting, mood and visual feel**. Think AAA game quality in that classic style. The world should show how
good the landscape looks with **weather, lighting, particles**, and living detail. It is a single-player game that
simulates an MMO world; the MMO systems matter, but the *look and feel* is the bar every piece is judged against.

The starting zone is **Oakhaven** and the world is **Crulanda**, from the novels in `D:\code\crulanda`. Reference
games are style references only: no copied names, assets, zones or creatures from any other game.

## What exists (Unity 6000.6.3f1, built-in pipeline)

- **Four zones on a level ladder, cap 10:** Oakhaven (1-2, farmland village, the Great Oak, the Golden Cask inn),
  Khaven Village (3-5, a walled village in permanent grim dusk beside the dead Whispering Wood), the Shattered Peaks
  (6-8, a mountain toll pass held by Sandthrone mercenaries), the Ashland Rim (9-10, grey petrified ash beside the
  Wasting, with the Ash-Walker caves and Weave-Eaters). All built from JSON by `ZoneBuilder` at load, 340-380 m across
  (grown 2026-09-30 with 37 new places, 18 camps and 14 secrets in the new ground).
- **Living world:** villagers with trades and daily routines, hen-wives and coops, critters with legs, a day/night
  cycle with lamps, falling leaves and ash, wind-swayed grass, respawning camps, ambushes from tall grass, breadcrumb
  quests that lead from zone to zone, a quest book, factions and standing.
- **A first dungeon, walked into with no loading:** Crowsfoot Hollow at the end of Oakhaven's North road runs 114 m and
  16 m down under the hills: the deserters' camp, the Drop, the Store Caves (Quartermaster Hesk), the Deep Stair and the
  Echoing Hall of Caddock the Bandit King in his tin crown; levels 3-5, the quest "The Tin Crown", a locked strongbox
  and its key, and Caddock's crown as his drop (WORLD_ZONES.md "Caves you walk into").
- **Secrets:** five hidden finds in every zone on no map (lookouts, caches, Chronicle pages, herbs, a key and its chest),
  a "Discovered" toast and a Discoveries tab in the quest book (WORLD_ZONES.md "Secrets").
- **Weather:** per-zone schedules of rain, storms, mist, flurries and ash squalls; a painted cloud layer with cloud
  shadows; wind gusts through grass and leaf crowns; wet ground and rain rings (WORLD_ZONES.md "Weather").
- **Water:** stylized turquoise (Chris's chosen look): a sandy bed showing through pale shallows, foam at shores and
  around legs, a creek current, ripple rings, wading and swimming. Khaven's Gloom Creek stays murky.
- **Camera and targeting:** trees fade when they block the view; a coloured ring under the selected target.
- **Classes:** Warrior and Druid playable, with data-driven talent trees; saves per character (format 7).
- **Docs to read next:** `WORLD_ZONES.md` (how zones are built), `QUEST_DESIGN.md`, `SAVE_FORMAT.md`,
  `CHANGELOG.md`, `CRULANDA_CANON.md` and `CRULANDA_LOCATIONS.md` (what is canon).

## The art direction (decided 2026-09-29)

Chris shared a reference of a lush, stylized, hand-painted MMO cove and chose it as the target look, as a style
reference only:
- **Painted, saturated, hand-crafted:** ragged leaf silhouettes with sky showing through, visible fronds and boughs,
  sculpted rock with painted gradient shading, chunky stylized props, a warm clear sky.
- **Every zone keeps its own mood:** Khaven stays grim dusk, the Ash Rim stays grey, the Peaks stay mountains. The
  style pass raises the craft, not the palette.
- **Trees are judged hardest.** Chris has called them out twice. Trunks are fixed, and crowns are now painted leaf cards
  (published 2026-09-30). Keep them that way: no blobs or cones.
- **Weather and atmosphere are part of the bar:** fog and haze that give depth, sun shafts, bloom, a moonlit night
  that stays readable, particles (leaves, ash, splashes, motes). **Weather** is built (2026-09-30): each zone has its own
  (Oakhaven rain and storms, Khaven mist, Peaks flurries, Rim ash squalls), with a painted cloud layer, cloud shadows and
  wind through grass and crowns.

## The world direction (decided 2026-09-29)

- **Zones need to be bigger, with more places and secrets to explore.** Today nothing is hidden: every interactable is
  a map-marked quest pickup. Decided: exploration is rewarded with **discoveries + loot** (a "Discovered" toast, XP, a
  cache, a Discoveries tab in the quest book). The overall order: **tree crowns, weather, the cave, secrets in every
  zone, the cave made a deep dungeon, then grow the zones** to about 340-380 m once there is content to fill them, then
  the painted style pass. (All up to the zone growth are built, 2026-09-30; the painted style pass is next.)
- **Seamless caves:** a rock-shell cave built into a hillside with no loading screen, darkening as you walk in. The
  first, **Crowsfoot Hollow**, is built (2026-09-30), the **Sandthrone deserters' cave** in Oakhaven's north hills: a quest
  from level 3 to clear the bandits and their game-only Bandit King so they stop harassing the villagers. Chris: "the cave
  should be deep and the first foray into dungeon crawling", so it is now a starter dungeon, 16 m deep. (Sandthrone is a canon mercenary company; the Bandit King
  is ours, and no canon Sandthrone name is used for him.)
- **Camps for grinding** stay in every zone; breadcrumb quests carry the player from zone to zone.
- **The next zone is a truly lush one** (Chris, 2026-09-30, with style references: a giant-tree forest of a thousand greens,
  cozy wooden houses built into the trees, waterfalls, flowers and glowing plants). Decided: **the Verdant Shore** (CANON,
  Book 3: "a forest that didn't know when to stop", trunks as wide as houses, the Veridian Keepers and their temple), set in
  the game's time, before the books' exodus. It is the fifth zone, **levels 11-13, after the Ash Rim**, so the level cap rises
  to 13; the way there crosses the ash-mountains west, as the survivors do in Book 3. The references are style only: nothing
  is copied from any other game.

## Standing rules

- The game is set **before** Oakhaven's erasure, so the village and the Great Oak stand, alive and in full leaf.
- Label lore honestly: CANON, CANON-EXPANDED, GAME-ONLY or PROVISIONAL.
- Evolve the world first; per-skill balancing comes later.
- Chris skims: ask him decisions first and short. Keep going without waiting between ordered steps, and tell him at
  each publish. Finish each step (fix, test, re-shoot, verify, commit, publish, back up) before starting the next.
- Validation runs against the copy in `work\encounter-validation`, never against the open project. Check every build
  log for `Shader error` (a broken shader silently falls back). Back up the save before a format change.

## Where things live

`D:\code\mmo` is the git repo (backed up to `J:\claude\unity projects\mmo` by `tools\Backup.ps1`). The playable build
is `outputs\Crulanda-Playable\Crulanda.exe`; run it with `--crulanda-temp-save` for a throwaway character. Every path
is in `D:\code\mmo\CLAUDE.md`.
