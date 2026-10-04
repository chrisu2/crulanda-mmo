# Crulanda Unity 6 — Claude continuation handoff

Updated 2026-09-28, America/New_York (latest logs cross into 2026-09-29 UTC).
This is the authoritative current progress snapshot. Some older project documents retain historical status statements.

## Start here

Chris wants you to continue developing the existing Unity project, not restart it.
Phase 0 and the Phase 1 combat MVP are complete. Phase 2 classes/build trees are underway.
The user requested this handoff because they ran out of tokens. Do not assume any agent is still working in the background.

## RESUME HERE (updated 2026-10-03, evening)
**NEXT:** Peaks done (round 12). Note 9 done (23:35). Next: notes 20-22 (Thornbolt lost at range, floating boulders + the coop's sack, deer flee too short), then POIs + achievements (Chris chose: every landmark a POI with XP and map reveal; an Achievements tab with points and titles), then the Ash Rim's
camps and quests (`Docs/NEXT_PEAKS.md` "After Peaks"). Chris said not to shut the computer down until he says.
**The playable build:** published 2026-10-03 23:35: the sun (note 9), hats, wheel zoom and staff slinging (notes 17-19); before it 22:25: the Peaks filled out (4 camps, 5 quests); before it 21:05: the dead trees rebuilt (playtest note 16: snags with heavy crooked limbs, blunt broken tops) and the board capture, on top of 17:25: notice boards with bounties and the rare courier posting, silver crowns, Khaven's
four camps and five quests (12d8ddc), on top of the boars (14:42), the farm animals (13:06) and the real animals (01:59).
**Last full run (23:33):** EditMode 396/396, PlayMode 175/175, Oakhaven toured, no exceptions. 22:21: EditMode 396/396, PlayMode 175/175, build OK, the Peaks toured, no exceptions. 21:01: EditMode 396/396, PlayMode 175/175, build OK, Khaven toured, no exceptions in any game log. Before it, 17:19: EditMode 396/396, PlayMode 175/175 (two zone tests failed on the board's postings in the full run and were
taught that a bounty belongs to the notice board; re-run green), build OK, Oakhaven toured, all shots, no exceptions in any game
log, no shader errors.
**Nothing in flight:** everything is committed, published and backed up.
**What the game has now (newest first):** notice boards (one a zone by the inn or hall: three postings a game day, village bounties
and Sandthrone contracts, a 1.5%-a-slot rare posting whose Bureau courier carries an Aether-Geode shard, the component for five
resonance-tempered weapons at the forge); the coin is the silver crown; Khaven's Scarp hollows, Thicket deserters, Heights scree
spiders and the Hush-Mother with five quests; boars (CraftPix, moved in code); farm animals in Oakhaven; real animals (Quaternius
models through `ModelBeast`); foot armour; armour and hats fitted to the form; every person a Quaternius model (`ModelFigure`);
before that the professions, loot, hunting, the five grown zones and the painted style (see the numbered history below and
CHANGELOG.md).

**Open (ask Chris before starting; he skims: put the question first or use the question prompt):**
1. **Farm animals are in Oakhaven** (published 2026-10-03 13:06): more could go to the other zones' farms and towns (Khaven,
   the Ash Rim's steadings), using the same kinds ("horse", "donkey", "cow") in their life.critters, at the END of each list.
   The pack also has Bull, Alpaca, Fox, Husky and Shiba Inu (zip at `tools/wip/animals/source/AnimatedAnimalPack.zip`).
2. **Still primitives**: spiders, the bramble-things, and the village's hens, sheep, cats, crows and rabbits. The CraftPix set
   (zip kept) also has a hare, fox, bear, owl, squirrel, hedgehog: the procedural mode can move them (the hare could be the
   game rabbit). A paid pack (polyperfect Low Poly Animated Animals, $50 on sale, has spider/hen/sheep/cat/rabbit with real
   animations) was offered on 2026-10-03; Chris did not choose it then.
3. Playtest notes 9 (bloom and sun) and 10 (baked cave lighting) are OPEN; note 12's armour designs could go further (plate
   shapes, pauldrons); note 14's PDF was last brought up to date 2026-10-02 21:45 (`tools/docs/make_history_pdf.py`).

**How to work (the rules that matter):** read `D:\code\mmo\CLAUDE.md` first. Sync into the validation copy and test there, never
in Chris's open project. Never edit the repo's Assets while a run is in flight (`run_tests.ps1` and `build_and_tour.ps1` both
mirror Assets when they start); stage elsewhere and apply after the build's mirror. Fast checks: `tools\validation\run_focus.ps1
-Filter <fixtures> -Platform EditMode|PlayMode`; renders in a minute: `run_method.ps1 -Method Crulanda.EditorTools.FigureCapture.Run
-Graphics` (people) or `...CreatureCapture.Run -Graphics` (animals), to `hel\work\ui-captures\figures`. The full check:
`start_detached.ps1 -Arguments '-Zones zone.oakhaven'` (or `-NoTour` when nothing visible changed), then wait for
`hel\work\full-run.done` (summary lines; fixed 2026-10-03 to read the mixed-encoding log). After the first Unity run of new
files, copy their new `.meta` files back from the validation copy (its robocopy /MIR deletes them otherwise). Publish:
robocopy `encounter-validation\Builds\Crulanda` to `hel\outputs\Crulanda-Playable` /MIR /XF *.log while Crulanda.exe is not
running; copy this file to `hel\outputs\Crulanda-Claude-Handoff.md`; commit (repo-local author, Opus 5.5 trailer); then
`tools\Backup.ps1`. Tests never touch Chris's save (SaveDirectoryOverride); back it up before any save-format change.

## Resume notes as of 2026-10-01 night (history)
**PUBLISHED 2026-10-01/02 (eleven publishes; the playable build is main at f046de3, published 2026-10-02 00:20):**
1. The Root-Mother's Deep, Crowsfoot's hidden mouth, every trade has a day (4c579e6).
2. The painted style pass parts 1-4 (03fd06d) and 3. part 5, painted masonry (17168d8).
4. The out of work drink at the inn; the visual review's first batch (ecc233b).
5. The visual review's second batch: Ash Rim, world edge, ruins, Peaks, caves (078d276).
6. Trades steps 1-2 (format 8, materials, tools, the Trades window, new buildings, Maud's leather shop) and loot A1 (weapons in
   hand) (4979457).
7. **Trades steps 3-4 (households and homes; gathering in Oakhaven), loot A2 (armour on the body), visual items 10-11 (buildings
   on slopes; the inns and the smithy as hero buildings)**, plus Chris's ore fix (no more green "peas"), knee-high herbs and
   mourner's cap as a mushroom. Tests on this round: EditMode 240/240 (one test was wrong: its figure's own name started "Gear"),
   PlayMode 108/108; five zones toured and the wardrobe 01-12 viewed. The tour/captures were rerun after the ore and herb fixes.
8. **Trades steps 5, 6 and 8 and loot A3 (round3):** Maud's four trade bags (bought, or "A Wallet for Simples"), Hob Linden the
   innkeeper and every trade at its own workshop, ten ore / eight timber / ten herbs in each of the other four zones, the loot
   ledger drafted (104 named items, unregistered, on mannequins in wardrobe 13-18); herbs 1.3x and the hauberk's surcoat belted.
   Tests: EditMode 285/286 (1 skipped), PlayMode 115/116, the one failure (Lisbet's drying errand, a timing flake in the test)
   fixed in 78d0528 and its fixture 6/6.
9. **Trades steps 7 and 9 and loot L1 (round 4):** purses (what you pay Maud feeds and warms the Tanner house; a cold chimney
   when they go without), stations and charcoal (forge, oven, bench, the Cask's range and hearth, field stations in four zones,
   the recipe pane), loot feel (beams on bodies by quality, the loot window, compare lines and upgrade arrows, call-outs); the
   trades' herbs 1.3x, dewfern rebuilt (its fronds ran into the ground), vendor names in ink, Khaven's inn door fixed for the
   navmesh (its taproom was cut off). Tests: EditMode 310/312 (1 skipped; one wrong test fixed), PlayMode 133/135 (both
   failures fixed: a test bug and the Khaven door; their fixtures 6/6); five zones toured, then Khaven and Verdant again.
10. **Trades steps 10 and 11 and loot L2 (round 5):** cooking (ten recipes at any fire, meats from the beasts), hunting (deer and
   rabbits are game, never hens, sheep or cats; "Skin the body"; three new hides; Maud's other three bag quests; the skinner
   sells salt), named loot live (the six loot files in Items, uniques by inventory, sets, vendor pieces; new characters start
   with the Trailblade). Chris's save backed up first (`save-backups/20261001-2235-pre-loot`; it was still format 6 on disk).
   Tests: EditMode 319/320 (1 skipped), PlayMode 150/151: `HuntTests.Game_bolts_and_sneaking_gets_closer` is INTERMITTENT (a
   rabbit that should bolt stood and watched, at (25.9, -68.4) by the Brook pond; alert reset twice, so GameAnimal.Bolt gave up
   or its path ended at once; passed alone and on a second fixture run). Published with that known. Oakhaven toured only.
11. **Round 6: trades steps 12 and 13, loot L3, polish:** blacksmithing (five smelts, 21 pieces) and the two-craft rule with
   take up and forget, alchemy (five potions), the Armoury (SAVE FORMAT 9; Chris's save backed up to
   `save-backups/20261001-2315-before-format9`), rare and epic gear that glows on the body, no talking through walls, herbs
   that follow the slope, the rabbit's bolt hardened. Tests: EditMode 341/341, PlayMode 154/158 with all four failures fixed
   (three test-text bugs, and mourner's cap buried on two Khaven banks) and their fixtures 11/11.
**Long runs go detached:** `tools\validation\start_detached.ps1 [-Arguments '-NoTests' | '-NoTour' | '-NoBuild' | '-Zones zone.khaven,zone.verdant']`
runs `full_run.ps1` outside the tool's process tree; wait for `hel\work\full-run.done` (the log is mixed-encoding: read the result
XMLs and logs directly). `-NoTour` builds and takes the HUD, wardrobe and loot shots but tours no zone; `-NoBuild` is tests only.
**Chris's rule (2026-10-01): skip the zone tours on rounds that don't change how the world looks** (World code, zone data, art,
visible creature or villager looks keep them; tour just the zones a round touches when that is enough). Never edit the repo's
Assets while a run is in flight (it mirrors Assets).

**CHRIS'S PLAYTEST NOTES COME FIRST: `Docs/PLAYTEST_NOTES.md`** (he jots issues as he plays; log each there, fix them in the
next rounds, tell him which build has the fix). Fourteen notes by 2026-10-02 00:30; note 8 is praise (the lights), the rest are work.
**IN FLIGHT (2026-10-02 00:30), in order of what to do when each comes back:**
1. **Round 7 = notes 1, 2, 3** on branch `round7` (worktree `scratchpad\wt\int7`; merged from `fix/n23-social-elites` and
   `fix/n1-oakhaven-grows`; compile ALL OK; NOT run): Oakhaven grown to 560 m with Crowsfoot Hollow moved to (-120, 240) and
   turned; social aggro by kind; elites at 5.5x health with a named heavy blow, enrage and a call; guards. Fast-forward main to
   it, run the full check with tests and tours of Oakhaven, Khaven and the Ash Rim (arrival points moved), look at everything
   note 1's write-up lists (and the fight shots in `ui-captures\elite`), check the build log's zone build time (estimate
   13 s editor, 11 s player) and frame cost (the ground is one 401k-triangle mesh), publish. Open decision for Chris in note 1:
   the 120 m rule elsewhere (default taken: Oakhaven now; Khaven next if he wants it; the outposts keep their camps).
2. **Art notes** (workflow `art-notes`, run wf_1ca487e1-45f): `fix/n4-sheep` (`wt\n4`: sheep, cats' tails, a critter line-up
   shot), `fix/n56-ore-windfall` (`wt\n6`: less green ore, done by hand; the lumber trees), `fix/n9-sun` (`wt\n9`: sun and
   bloom, then baked cave lighting), `fix/n11-icons` (`wt\n11`: generated icons for abilities and every item). And run
   wf_99f8008d-998: `fix/n13-colour` (`wt\n13`: high-fantasy palettes). Each: design panel with previews, judges, build,
   review, verify, fix. Previews are in `scratchpad\artnotes\<item>`. Merge after round 7; all change the look: tour.
3. **Characters** (workflow `characters-design`, run wf_755e3e44-eb0; Chris chose "build them in code"): maps, three designs
   with rendered previews, judges, the plan in `tools/wip/characters` on branch `plan/characters` (`wt\c0`), critics, then
   step C1 (his own character smooth and moving, behind a switch) built and reviewed on that branch.
4. **The PDF** (workflow `history-pdf`, run wf_e1fd6182-250): `Docs/Crulanda-Everything-Built-So-Far.pdf`, generator in
   `tools/docs`. When it reports: open it, commit it, send it to Chris.
**Then:** step 14 (depth and tuning), loot L4-L6, and whatever he notes next.

**THE PLANS (read the owner notes first; they are his words and decisions):**
- Professions, gathering, households, purses, bags, hunting: `tools/wip/professions/OWNER_NOTES.md`, `DESIGN.md`, `ADDENDUM.md`,
  `BUILD_PLAN.md` (14 publishable steps on two tracks: A village 1, 3, 6, 7; B professions 2, 4, 5, 8-14).
- Loot and worn appearances: `tools/wip/loot/OWNER_NOTES.md`, `DESIGN.md` (steps A1-A3 run beside professions; L1-L6 land at
  professions step boundaries because they share the item code), `ITEMS_V1.md` (104 named items).
- Defaults told to Chris and being built unless he objects: the leatherworker is Maud (her family: Fen the skinner, Nettie);
  invented kin so every villager has a household, two new houses; a hide comes from searching the body (no skinning skill);
  only a Blacksmith smelts; low skill never blocks a node; empty gear slots show empty; cloaks as a tenth slot last; boss trophies
  become "one you do not own yet". To raise when L-steps start: a new character should not be left empty-handed (start with the
  training blade equipped).
- Chris's save is backed up before format 8: `hel/work/save-backups/20261001-1545-before-format8`.

**Visual worklist** (`tools/wip/painted/visual_review.md`): items 10 (buildings on slopes) and 11 (the inns and the smithy as
hero buildings) are done and published (2026-10-01 night), so all 14 ranked items are done. The reviewers' raw lists
below them still hold smaller things: Mossveil Falls as a flat rectangle, mud/puddle/fire-bed discs, the giant trunks' straight
moss line, the salt flats' squiggles, neon ferns on the Peaks' scree, hard-edged tall-grass discs.

**How today's work was done, worth repeating:** design by a panel (readers map the code, three lenses design, one synthesis),
critique, then build in worktrees with review and refutation before merging; for visual work, agents render and LOOK at generated
textures before anything is built, patches are verified on scratch copies, and every batch is toured and looked at before it is
published. Never edit Assets while a run is in flight (the tour mirrors Assets again). `run_focus.ps1 -Filter '<fixtures>'` runs
a few fixtures fast; `build_art.ps1` builds generated art in the validation copy and brings it into the repo.

**Chris asked whether to move to Unreal** (2026-10-01): answered no (the art, not the engine, is the limit; a port rewrites
everything; a Unity lighting-pipeline trial is the cheap experiment). He did not ask for the trial.

**The standing direction is in `GAME_BRIEF.md` (read it first): a high-fidelity, beautiful MMO-style world, AAA quality in a classic style, judged on details, graphics, lighting, mood and visual feel.**
Chris's rules: work in order of importance; finish each step; keep going without waiting (memory: crulanda-autonomy); tell him at each publish.
Chris was away from 2026-09-30 evening and gave authority to carry on through the phases unattended, publishing each.

**State: everything below is PUBLISHED** to `outputs/Crulanda-Playable` and committed on `main` (backed up to J:). Tests: EditMode 183/183, PlayMode 76/76, 0 shader errors.
- Published so far:
  - steps 1-4 of the visual review;
  - the turquoise water;
  - the step-3 fix round;
  - the cliff rework;
  - the temp-save travel fix;
  - the tree crowns (painted leaf cards);
  - **WEATHER** (commit 58d06b1, published 2026-09-30 afternoon);
  - **CROWSFOOT HOLLOW** (commit 87dd05b, published 2026-09-30 evening);
  - **SECRETS** (commit f3f6b9a, published 2026-09-30 late evening): twenty hidden finds on no map, the Discoveries tab, save
    format 7. Design: `WORLD_ZONES.md` "Secrets", `SAVE_FORMAT.md` format 7, the CHANGELOG.
  - and now **CROWSFOOT HOLLOW GOES DEEP** (published 2026-09-30 night): the first dungeon, 114 m and 16 m down: the camp,
    the Drop, the Store Caves (Quartermaster Hesk, elite 4; the strongbox and its key), the Deep Stair, the Echoing Hall
    (Caddock, elite 5, drops Caddock's Tin Crown). The Tin Crown is level 4. A cave-name banner, dungeon respawns, no use
    through rock, a kill plane under the deepest floor. Design: `WORLD_ZONES.md` "Caves you walk into", `QUEST_DESIGN.md`
    section 5, the CHANGELOG.
  - and now **THE ZONES GROWN** (published 2026-09-30 night): Oakhaven 380 m, Khaven 340, the Peaks and the Rim 360, with 37
    new places, 18 camps (4 named elites) and 14 secrets in the new ground; Crowsfoot Ridge over the dungeon; loads faster
    than before (parallel ground paint). Design: `WORLD_ZONES.md` "Zone size" and each zone's section, the CHANGELOG.
  - and now **THE VERDANT SHORE** (published 2026-10-01): the fifth zone, levels 11-13 (cap 13), Book 3's giant-tree forest:
    the Ridge of Long Shadows and the first view, Rootfast's treehouses, the Veridian Temple, Mossveil Falls, the Mistmere, the
    Whispering Glade, the Salt-Flats, the Greying and Palemist Hollow; Keepers with their own talk; twelve quests from Grohl's
    breadcrumb; and the lush art every zone now shares (painted ferns, broad leaves, reeds, flower drifts, giant trees,
    waterfalls, mushrooms, real dead trees). Design: `WORLD_ZONES.md` "The Verdant Shore" and "Painted plants and lush props",
    `QUEST_DESIGN.md`, the CHANGELOG.
  - and now **THE ROOT-MOTHER'S DEEP and the hidden Crowsfoot mouth** (published 2026-10-01 morning): the Verdant Shore's
    dungeon under the Temple (a second cavern plan, `variant: 1`, grown through earth and root: the Root Gallery, the Sap Well,
    the Cold Stair, the Heart with the Root-Mother and the cold in her root; six camps and the Hollow Root-Warden; the finale
    quest `main.verdant.5`); and Crowsfoot's mouth as a slot in a cliff face at the end of a bent track, not a rock on the
    grass. Design: `WORLD_ZONES.md` "Caves you walk into", `QUEST_DESIGN.md`, the CHANGELOG.
  - and now **EVERY TRADE HAS A DAY** (published 2026-10-01 morning): `VillageWork.cs`, daily shifts and errands for every
    trade, goods carried in their hands between the trades (eggs, water, grain, flour, bread, logs, hides, herbs, hares,
    wares), the village's stock, the trades talking about each other's goods, the hen-wife's full day (feed, eggs to the inn,
    water to the hens' pan, dinner, feed, eggs to the stall, eggs home) and Oakhaven eggs on the merchant's stall. Design:
    `WORLD_ZONES.md` "Life, day and night", the CHANGELOG.
- **Weather** covers:
  - per-zone seeded schedules (7-minute spells, one severity step at a time);
  - a painted cloud layer and cloud shadows;
  - rain with streaks, splashes, rings on water and wet ground;
  - storms with lightning; Khaven ground mist, Peaks flurries and Rim ash squalls;
  - wind gusts through grass and crowns;
  - a chat line when the weather turns, and the weather under the minimap clock.
  - Design and data: `WORLD_ZONES.md` "Weather"; the CHANGELOG has what the three tours and the independent review fixed.
- Dev: F8 cycles the zone's weather in development builds; `--crulanda-weather rain` starts in it.
- Marketing: `marketing/steam-blurb.md` is the Steam copy draft (commit 06e8795). Chris to confirm:
  - the store title, "The Quiet Trail" (the productName) or "The Land of Crulanda" as the novels and bullet-hell use;
  - that *The First Spoke* is out.

~~THE VERDANT SHORE~~, ~~THE ROOT-MOTHER'S DEEP~~ and ~~EVERY TRADE HAS A DAY~~ DONE and published 2026-10-01. **NEXT JOB: THE
PAINTED STYLE PASS on the four older zones**, reusing
the lush art (the undergrowth, dead trees and glowing plants are already in them): richer saturated colour per biome, more
plant kinds, sculpted rock with painted gradient shading, chunky stylized props (rope-and-post bridges, the `bridge`
variant 1), a warm clear sky; each zone keeps its mood. Then: the Verdant Shore's optional extras from the zone agent's notes
(low mist banks, glass-frog critters, a root-stair prop for the Temple, the west backdrop falling to the sea); a `hollow`
mist-walker variant with violet-lit eyes; weather polish; audio.
~~ZONE SIZE~~ DONE and published (item 0c below). Then: **THE PAINTED STYLE PASS** (item 5 under "Next" below, and
GAME_BRIEF's art direction): richer saturated colour, more plant types (ferns, broad leaves, flowers, tall grasses),
sculpted rock with painted gradient shading, chunky stylized props (rope-and-post bridges), a warm clear sky; every zone
keeps its mood (Khaven grim dusk, the Rim grey). Plan it as a few publishes, biggest visual win first.
More walk-in caves (a crypt under Khaven, a mine in the Peaks, a sunken ruin on the Rim) can reuse the `cavern` prop: a new
plan in `ZoneBuilder.CavernPlan(variant)` (rows: x, z, half-width, height, floor drop).

Weather polish for later (not blocking):
- rain and thunder audio (there is no audio yet);
- puddles;
- a wet sheen on roofs and props;
- villagers heading indoors in rain;
- snow settling on the Peaks.

**Next:**
0. ~~Finish step 4 + water~~ DONE and published.
0. ~~TREES~~ DONE and published 2026-09-30 (two passes). Original notes kept:
   - The trunks are fine now. The problem is the CROWNS: every broadleaf/orchard tree is one or two smooth faceted balls on a stick (no leaf edge, no gaps, no branches inside), and the pines are perfect stacked cones. See `work\world-captures\oakhaven-99-target-ring.png` and `oakhaven-08-the-old-orchard.png`.
   - Target look (Chris's reference, memory crulanda-direction): ragged leaf silhouettes, sky showing through, visible fronds and branch structure.
   - Plan: leaf CARDS. Painted alpha-cutout leaf-cluster textures generated in ZoneSceneBuilder (green, yellow-green, autumn, a pine-bough variant); crowns from 8-14 crossed, tilted quads on short boughs plus a small dark inner core; pine tiers as tilted bough cards, not solid cones; a Crulanda/Leaf cutout shader (two-sided, wind sway, ShadowCaster with the cutout); TreeFade taught to fade cutout leaves. Keep crown volumes so bounds, colliders and navmesh don't change; keep the zone rng stream.
   - **The patch is READY:** `tools\pending-patches\step3b-tree-crowns.json` (verdict fixable: apply `review.correctedEdits` and write `review.correctedNewFiles`, i.e. `World/Shaders/Leaf.shader` + `.meta`). The critic verified all anchors against the tree at commit f959dfd and the zone rng stream unchanged. Re-check the Great Oak anchor (`cap.center = new Vector3(0, 3.2f, 0); cap.height = 6.4f; cap.radius = 1.1f;`) before applying. Then: tests, build, four-zone tour, check `oakhaven-99-target-ring` and `oakhaven-08-the-old-orchard` and a pine shot, publish, tell Chris.
0a. ~~WEATHER~~ DONE and published 2026-09-30 (commit 58d06b1; see WORLD_ZONES.md "Weather"). The original plan is kept below; cloud shadows ended up in the post composite, not a light cookie, because a cookie on the sun would push every lit surface into an extra forward pass.
   - The goal in the brief: the landscape should show off weather, lighting and particles. Nothing weather-like exists yet beyond the fixed fog, the day/night cycle and the falling leaves/ash.
   - Build a `WorldWeather` system (Scripts/World): a per-zone weather state that changes over game time (clear, overcast, rain, wind, and per-biome extras: mountain snow flurries in the Peaks, ash squalls on the Rim, drifting mist in Khaven's wood), blending fog density/colour, ambient and sun intensity, the sky's exposure/haze and the post grade over 20-40 s transitions.
   - Rain: a particle system that follows the camera (world-space streaks, a ground splash layer, darker wet ground tint, water surface pocked with rings); rain never falls indoors or under the Wasting curtain.
   - Wind: gusts that drive the existing grass and leaf sway (a global `_WindGust` shader value the Grass and Leaf shaders read), bend the falling leaves/ash, and lean the tall grass.
   - Cloud shadow: a slow-scrolling soft cookie on the directional light (a generated cloud texture), strongest when overcast.
   - Keep it cheap and data-driven: weather chances per zone in the JSON; a dev key to force a state; the capture tour shoots one rain and one overcast shot per zone. Night must stay readable; the HUD clock could show a small weather glyph.
   - Tests: state machine transitions are deterministic under a seed; forcing rain sets the wet tint and spawns the system; no state leaves fog outside its band.
0b. **TEMP-SAVE TRAVEL BUG** (Chris: "lose talent points going to new zones", confirmed twice): FIXED and PUBLISHED on 2026-09-30 (commit d9f6617; static `tempSaveRoot`, test `TempSaveTravelTests` passes: level, gold and a spent talent arrive in the next zone). Chris has been told. Cause: `--crulanda-temp-save` minted a new folder on every scene reload, so each zone started a fresh character. Real saves were never affected. Done.
0c. **WORLD DEPTH (Chris, 2026-09-29: "zones do need to be bigger with more places and secrets to explore").** Decisions: rewards = DISCOVERIES + LOOT; order = CAVE, then SECRETS, then SIZE.
   - Today every zone is a ~260 m square with 5 camps, and nearly every interactable is a map-marked quest pickup (Peaks: 6 interactables over 60 props). Nothing is hidden; wandering pays nothing.
   - **Secrets:** hidden finds NOT marked on any map, a few per zone: a cache behind a waterfall / under a bridge, a lookout with a view, an abandoned camp with loot and a note, a Chronicle page in a hollow tree, a rare herb patch, a locked chest whose key is elsewhere. Each grants a 'Discovered: <name>' toast + XP, most hold a cache (gold, a rare item, a page). Add a Discoveries tab to the quest book (per zone, found/total), a `discoveries` list in the save (format 7), and a `secret` flag on props so maps skip them. Tests: every secret reachable on the navmesh; discovery toast and save round-trip.
   - **Sub-areas:** the bandit cave first (item 2 in this list), then one per zone in the same vein: a crypt interior under Khaven, a mine or ledge path in the Peaks, a sunken ruin on the Rim.
   - **Size:** grow zones to ~340-380 m LAST, once there is content to fill them; keep the village cores where they are (roads, exits and arrival points move with the edge: check ZoneExitTests). 
   a. Apply the turquoise water: `tools\pending-patches\step5-stylized-water.json`. Use the review's `correctedNewFiles` (a full new `World/Shaders/Water.shader`; write it) and `correctedEdits` (2 in `ZoneBuilder.WaterMaterial()`, 1 in WORLD_ZONES.md). The night-foam glow is already fixed in the corrected version.
   b. Lighten the bed so the shallows show sand, as in Chris's reference: `ZoneBuilder.PaintGround` shore paint (about line 529-531) darkens the waterline to .22/.2/.15 and the bed to .16/.15/.12. For non-gloom biomes use a pale sand (about .62/.56/.42) at the waterline and a slightly darker sand under water; keep Khaven dark.
   c. `run_tests.ps1`, then `build_and_tour.ps1` for all four zones; check `Shader error` in the build log (a broken shader silently falls back).
   d. Look at the water shots (oakhaven-99-creek/wading/pond/pond-night, 06-oak-creek-mill, 17-brook-pond, khaven-99-*) and the HUD shots (*-97-*, *-98-*, *-99-*): road signs at exits, no label overlaps, tracker readable.
   e. Publish, back up, and **tell Chris step 4 is published and show him the water**.
1. ~~Step 4, HUD~~ done (see above). Remaining detail: apply `tools\pending-patches\step4-hud.json` (overlaps, readability).
   - Its `overlaps` patch rewrites `EncounterHud` label drawing and was already rebased once. Expect skipped edits; merge them.
   - Also add world labels at exits ("Road to Khaven Village (3-5)") within about 30 m, so exits are easy to find. Chris was lost at a map edge with no exit.
2. **NEW STEP, right after step 4: a seamless cave and the Sandthrone bandit quest.** Chris decided on 2026-09-29 (order: after the HUD, before the water).
   - A quest appears at level 3: clear out the bandits and their Bandit King in a cave, so they stop harassing the villagers.
   - Tie-in (chosen): SANDTHRONE DESERTERS. The Sandthrone is a canon mercenary company, antagonists in world_bible.md, holding the Peaks toll in book1 ch.5; in the game they already hold Khaven.
     - A deserter band dug into a cave near Oakhaven and raids its farms.
     - The 'Bandit King' is a GAME-ONLY character. Don't use canon Sandthrone names such as Zarytha Vex for him.
     - The lore has no bandit king. Copper-Tithe (book1 ch.24) was rejected because they are sympathetic in canon.
   - Seamless means no loading: a rock-shell cave built into a hillside (tunnels and chambers, blob-mesh walls and ceiling, a flat walkable floor on the terrain).
     - Inside: navmesh, torches, and lighting and fog that darken as you walk in (a trigger volume lerping ambient, fog and post exposure).
     - A camp of deserters, with the Bandit King (elite) in the deepest chamber and loot.
   - Good spot: Oakhaven's north hills. Chris wandered to the north edge looking for a way out.
   - Add tests: ZoneContentTests reachability inside the cave, and the quest data.
3. **Step 5, stylized turquoise water:**
   - Ready: `tools\pending-patches\step5-stylized-water.json` has a full new Water.shader plus WaterMaterial() edits, compile-checked with fxc. Use the review's `correctedNewFiles`/`correctedEdits` when the verdict is 'fixable'. `apply_patches.ps1` doesn't write new files.
   - Otherwise re-run the design, which asks for: a depth colour ramp (pale teal to turquoise to deep teal), soft wobbly white foam at shores and objects, stylized sparkle bands, and little reflection.
   - Khaven stays murky.
4. **Step 6:** the painted style pass on all zones, keeping each zone's mood (see memory crulanda-direction).

**Next, in order:**
0. ~~Zone-travel bug~~: fixed or verified on 2026-09-29 (see above). The original report: Chris, 2026-09-29, playing the published build `76ed4eb`.
   - Reproduce it first. Try each exit in Oakhaven (west road to Khaven, south road to the Ash Rim) and the others.
   - Suspects, in order:
     - Step 1's backdrop skirt, forest edge or boundary colliders keeping the player (a CharacterController, not a navmesh agent) out of the exit radius.
     - E now picking a friend or target before the exit (`EncounterSession.Interact` / `TalkTarget` priority).
     - The "not in combat" rule, or an ambush or camp near an exit keeping you in combat.
     - Arrival points landing in blocked or unwalkable spots after the zone changes (the phantom-Wasting wall in Khaven and the Peaks was fixed on `main` in `191d9b8`).
     - ZoneContentTests only path-checks exits on the navmesh.
   - Add a PlayMode test that walks the player (the motor, not a teleport) into each exit and presses E, and checks that the zone changes.
1. **Finish step 2:**
   1. `git merge wip/step2-fix-round`.
   2. Run `tools\validation\run_tests.ps1`, then `build_and_tour.ps1 -Zones zone.oakhaven, zone.khaven, zone.peaks, zone.ashrim`.
   3. Check the build log for `Shader error`.
   4. Verify the shots against the 7 problems:
      - Khaven roads visible, boars visible in the grass;
      - Peaks pines seated, rock not pale;
      - Ash Rim cracks thin;
      - the curtain has no seam and no night smears.
   5. Commit, then publish: robocopy `work\encounter-validation\Builds\Crulanda` to `outputs\Crulanda-Playable` if Crulanda.exe isn't running.
   6. Back up and **tell Chris step 2 is published**.
   - Minor step-2 notes not yet addressed: Khaven reads one hue (rose) with no orange-versus-violet split; its leafy trees are grey puffs; the HUD clock shows day in permanent dusk; the Ash Rim's far trees are paler than the backdrop; the Peaks' far distance is a bit milky.
2. **Step 3, props, landmarks and creatures:** patches are ready in `tools\pending-patches\step3-props-landmarks-creatures.json` (see its README).
   - Floating props; the Pale's floating head; the Weave-Eater remodelled to canon (a drifting thread-mass, not a dog).
   - Oakhaven, Khaven, Peaks and Ash landmarks that match their names.
   - Tour framing.
   - Expect some skipped edits, because step 2 changed `ZoneBuilder` after these were written; merge those by hand.
   - **Also in step 3** (Chris, playtest screenshot, 2026-09-29): the broadleaf and orchard tree trunks "look bad". They are plain grey pipes.
     - `ZoneBuilder.Broadleaf` adds a wider base cylinder (`h * .08`) that reads as a pipe collar.
     - Fix: tapered trunks; a root flare like DeadTree's (a swell plus half-sunk buttress ridges) instead of the collar; bark colour variation; a slight lean; limbs that join the trunk.
   - **PRIORITY in step 3: rebuild the Great Oak as the town's centrepiece.** Chris: "this tree looks terrible. this should be the center piece of town".
     - CANON (book1/chapter_4.md, line 39): a massive, ancient oak dominating the centre of the communal square, the historical heart of the settlement. After the erasure it is found petrified mid-bloom, in spring leaf.
     - So before the erasure, when the game is set, it must be ALIVE: in full leaf, with sprawling massive branches.
     - Today it is `dead_oak` (DeadTree cylinders): a banded trunk where the cylinders overlap, stick limbs with ball knuckles, no leaves.
     - Rebuild it as a new `great_oak` kind in oakhaven.json:
       - a gnarled, tapered trunk mesh with smoothly merging limbs;
       - a huge layered canopy of blob clumps with fresh green leaf (it may carry some autumn colour to fit Oakhaven's season);
       - a big root flare, perhaps a low stone ring or bench round it;
       - the green framing it.
     - Keep its TreeFade and collider, and keep it readable from the whole village.
   - **Also in step 3: critters have no legs.** Chris: "cats have no legs".
     - In `WorldLife.cs` (the critter body switch, around line 715), the cat, chicken, rabbit and crow are built without legs. Only the deer and sheep have them.
     - Add legs (cat: four slim legs and paws; chicken and crow: two thin legs and feet; rabbit: haunches and forepaws), sized so the bodies stand on the ground.
     - Add a simple leg swing while they move, like the villagers' walk.
3. **Step 4, HUD overlaps and readability:** `tools\pending-patches\step4-hud.json`.
4. **Step 5: STYLIZED TURQUOISE WATER.** Chris's decision on 2026-09-29 replaces the realism plan. His reference was a screenshot of a lush tropical cove from a stylized MMO; use it for STYLE only, copying no assets or names.
   - Bright clear pale-teal shallows over a visible sandy bed, deepening to rich turquoise and blue.
   - Soft white foam lines at the shores and around rocks and legs, painted ripple highlights, little mirror reflection.
   - Oakhaven, the Peaks and the Ash Rim get it. Khaven's Gloom Creek stays murky.
   - The see-through water, murk and touch-foam we already have are the base. From `tools\pending-patches\step5-water-realism.json`, only parts of 'surface-detail' may still help (layered ripples); the planar-reflection camera is not needed for this style.
5. **Step 6: PAINTED STYLE PASS on all zones, keeping each zone's mood** (Chris's decision the same day).
   - Richer, saturated colour; many more plant types (broad leaves, ferns, flowers, tall grasses).
   - Sculpted rock formations with painted gradient shading; chunky stylized props (rope-and-post wooden bridges); a warm, clear sky.
   - Khaven stays grim dusk and the Ash Rim stays grey, painted in their own palettes.
   - A planar reflection camera (`WaterReflection.cs` is in `newFiles`), plus finer ripples and sparkle.

**Helper scripts** (`D:\code\mmo\tools\validation`):
- `run_tests.ps1`.
- `build_and_tour.ps1`: now skips the tour if the build fails, and waits 420 s per zone because the machine is slow under load.
- Archive `work\world-captures` before each tour.

## UPDATE 2026-09-29 (Claude session) — zones 1-10, camps, items, water, graphics
- **Decision by Chris:** the level cap stays at 10 for now.
  - Ladder: Oakhaven 1-2 → Khaven 3-5 → Shattered Peaks 6-8 → Ashland Rim 9-10.
  - Build order: Peaks then Ashlands.
  - Graphics priorities: lighting and atmosphere, terrain and nature, characters and creatures. Buildings come later.
- **Chris's standing requests:**
  - Every zone keeps respawning camps for grinding.
  - Breadcrumb quests carry you to the next zone.
  - Water must look and behave correctly.
- **Where to read:**
  - CHANGELOG, the "Zones 1-10" entry.
  - WORLD_ZONES.md: the new zones, the level ladder and camps table, Water, and Nature and post-processing.
  - QUEST_DESIGN.md §8: the chain across zones.
  - SAVE_FORMAT.md: formats 5 (XP curve) and 6 (bag and equipment).
- **Key new code:**
  - `World/ZoneWater.cs`: the single water model.
  - `World/NatureFx.cs` and `World/ZonePost.cs`.
  - `World/Shaders/Water`, `Grass` and `Post`.
  - `Encounter/Items.cs`: `ItemDatabase` and `Inventory`.
  - `Encounter/EncounterHud.Items.cs`: bags, the character sheet and vendors.
  - Camps and ambush in `EncounterEnemy`, and swimming and sneaking in `AdventurerMotor`.
- **Validation (2026-09-29):**
  - EditMode 166/166, PlayMode 47/47. These include `ZoneContentTests` and `WaterTests` for all four zones.
  - The player build succeeded, and world tours of all four zones were captured.
- **Chris's save:** backed up to `work\save-backups\20260929-020630-pre-v6` before the format 6 build was published.
- **Helper scripts** are in the session scratchpad (`run_tests.ps1`, `build_and_tour.ps1`, `build_ui_capture.ps1`).
  - They mirror Assets into the validation copy and run `ZoneSceneBuilder.BuildOakhaven`, which registers zones, quests and items.
  - `EncounterBuildPlayer.Build` now does the same before building.
- **PowerShell gotcha:** never pass a single replacement pair as `@( @(a,b) )`. PowerShell flattens it, which corrupted two files once.
- **Visual review:** 81 confirmed defects across the four zones, listed in `Docs/VISUAL_REVIEW_2026-09-29.md`. None are fixed yet. The main themes:
  - The world's edge is visible past the exits.
  - Night-facing sides render pure black.
  - A grey slab sits under the bridge arches.
  - Zone moods don't come through: Khaven isn't dusk or grey, Gloom Creek is chrome, the Peaks are grassy hills, and the Ash Rim is tan sand.
  - Floating or clipping props.
  - Landmarks that don't match their names.
  - Speech bubbles and labels over the HUD.
  - Capture-tour cameras stuck inside trees and walls.
- **Open items:**
  - Quest rewards don't grant items yet.
  - Water doesn't receive shadows, and there is no underwater camera.
  - No audio.
  - Buildings pass (graphics) still to do.
  - Hollow Men / the well at night (Chronicle II).

## UPDATE 2026-09-29 (Claude session) — quests, village trades, direction decisions
- **Decisions by Chris (2026-09-29):**
  - The game is set BEFORE Oakhaven is erased, so the village stays standing and the erasure is not shown.
  - The inn is the **Golden Cask**, the book's name (it was Whispering Barrel).
  - When you need his input, ask prominently: use the question prompt, or put the question first and short. He skims long reports.
- **Quests:** the framework and 10 Oakhaven quests are done. Read `Docs/QUEST_DESIGN.md` section 7. The book is on L. Save format 4 (`SAVE_FORMAT.md`).
- **Village trades:**
  - Trade outfits, `<Trade>` nameplates and workplaces: forge, stall, oven, tannery and woodpile.
  - Residents (`life.residents`): Quill the Salt-Mender and Warden Ivel.
  - See WORLD_ZONES.md, "Life, day and night".
- **Next:** the well at night and the Hollow Men (Chronicle II onward), then Khaven quests and the Sandthrone contracts.
- **Validation:** helper scripts are in the session scratchpad (they can be recreated from "Practical validation workflow" below).
  They sync with robocopy /MIR, run EditMode and PlayMode, build, and capture with `--crulanda-ui-capture` and `--crulanda-world-capture`.

## UPDATE 2026-09-28 (Claude session) — day/night cycle, hen-wives and chicken coops
Chris asked for a chicken farmer who gathers eggs, feeds the chickens, and puts them in the coops at night.
- `Scripts/World/WorldClock.cs`: the day/night cycle (40 min/day, static hour, lamps as `NightLights`) and `ZoneCoop`.
- `ZoneBuilder.Coop` builds the `coop` prop. Oakhaven has three coops.
- `WorldLife.cs`:
  - Villager role `henwife` with a scheduled routine: open, feed, eggs, yard, herd, close.
  - Every villager has a bedtime.
  - Chickens link to a coop and gain the states Roost, Return and Feed.
- Full description: WORLD_ZONES.md, "Life, day and night". Tests: VillageDayTests (PlayMode).
- Earlier in this session (not yet in the CHANGELOG):
  - WoW-classic-style HUD (`EncounterHud`, `HudMaps`): minimap, M zone map, scroll out to the world map.
  - Oakhaven enlarged to 260 m with farms, mill, orchard, groves and instanced grass.
  - Walk-in inn with a door; villagers and critters (`WorldLife.cs`).

## UPDATE 2026-09-28 (Claude session, latest) — direction: evolve the world; Druid playable; Oakhaven zone
Chris's direction (latest): stop per-skill testing/balancing; "the game needs to evolve first" - build graphics, zones
and maps from the lore in `D:\code\crulanda` (world_bible.md, maps\*.png, interactive_map.html, chars\*.html, books).
- Multi-class: `ClassKit` base (Warrior/Druid kits), per-class character saves (`encounter`, `encounter-druid`),
  last-played remembered via a `profile` slot, switch character from the Esc pause menu. `--crulanda-class <id>` flag.
- Druid playable: 4 forms (Barkhide/Thornclaw/Rootmend/Thornsong), Shift Breath + per-form pools, 22 base actions,
  28 talents (rows 0-2 of druid.json). Shared frameworks added: PeriodicEffects (DoT/HoT/channels), enemy Slow/Root,
  player cast bars with movement interrupt, keys 9/0.
- World: data-driven zones (`Docs/WORLD_ZONES.md`), Oakhaven built from the canon village map and set as the opening
  scene; procedural humanoid characters (`ActorVisual`); camera collision. Canon notes: `Docs/CRULANDA_LOCATIONS.md`
  (Forgeborn is canon; Driftkin/Windcaller are not found in the lore folder).
- Next world steps: zone-to-zone travel, a second zone (Khaven Village has a strong canon map), interiors for the inn,
  ambient life (villagers), audio, then better character art.

## UPDATE 2026-09-28 (Claude session, later) — Warrior tree is data-driven and 27 talents are playable
Read PHASE2_CLASS_LOOPS.md "Update 2026-09-28 (later)" and SAVE_FORMAT.md for details. Key facts:
- Source of truth for talents: workspace `work\calculator\trees\warrior.json` -> copied to
  `Assets\Crulanda\EncounterContent\Talents\warrior.json` with `build.py --sync` (build.py refuses to run on drift).
  Game loads it via EncounterContent.talentTree. Only nodes with `"impl": true` are playable, and each must be listed in
  WarriorKit.ImplementedTalents with code behind it (TalentTree.Parse throws otherwise).
- Budget = level + 1; tier gate 5 points per row in a branch; same rule in calculator (both modes).
- Code: TalentTree.cs, WarriorKit.cs (new), EncounterSession/Enemy/Hud/Save/Input/Capture, HealerCompanion,
  Combatant (barrier), AbilityDefinition (Intercept/Breach/Rally effects), ClassDefinition (talentId on unlocks).
- Verified: 145/145 EditMode, 26/26 PlayMode, player build OK, HUD screenshots checked (work\ui-captures).
- Build published to outputs\Crulanda-Playable. Chris's save was backed up to work\save-backups\*-pre-v3 (it was
  still format 1, level 2) before the new build could migrate it. Zip checkpoint of Assets/Docs/Packages/ProjectSettings
  from before this slice: work\checkpoints\pre-warrior-slice-*.zip (no Git on this machine).
- Chris's Unity editor was open on Phase0_TestMap during this work; it will reimport the changed scripts when focused.

## UPDATE 2026-09-28 (Claude session) — calculator delivered
The all-class calculator from "IMMEDIATE UNFINISHED WORK" below now exists and was browser-tested:
- Page: workspace `outputs\Crulanda-Talent-Calculator.html` (self-contained, ~290 KB; opens from disk).
- Source: workspace `work\calculator\`: `trees\<class>.json` (20 files, hand-authored per path, PROVISIONAL / GAME-ONLY),
  `template.html` (renderer), `build.py` (bundles; embeds the Warrior prototype mirroring WarriorTalents.cs),
  `validate.py` (schema + tier-gate reachability under worst exclusive choice), `tweak.py` (lead-review pass; idempotent),
  `lint.py` (cross-class duplicate names / borrowed MMO names / cheat-death effects), `summarize.py` (compact review view).
  Rebuild: run `build.py` with the bundled Python. Old `data.json`/`generate.py` placeholders are superseded.
- Proposed trees: 3 branches (Druid 4), 7 tiers x 4 columns, 5 points per tier gate, 30-42 ranks per branch,
  one signature (tier 3-4) and one capstone (tier 6) per path; 1 point per level from level 1 up to level 50 (assumption, not approved).
- Warrior has two modes: "Implemented prototype" (exact Unity rules, level 1-10) and the proposed 16-node-per-branch full tree
  that grows out of the prototype nodes.
- Features: search, per-tree/all reset, tooltips + inspector, dependency-safe refunds, build codes (URL hash, copy/load),
  saved builds in browser storage, two-build compare. "Design emphasis" bars sum authored per-rank weights; they are
  explicitly NOT DPS/healing measurements, and observations are allocation facts, not viability verdicts.
- Verified: 25 in-browser rule/UI checks passed (prototype gates/levels/exclusive signature, tier gates, refund blocking,
  code round-trip and rejection, save/compare/load, all 20 classes render, every branch fully purchasable under each exclusive choice),
  no console errors, phone width has no horizontal scroll.
- Not done: the in-game Unity talent panel still has not been visually inspected (needs a window capture incl. IMGUI);
  HUD stretches non-uniformly on non-16:10 screens (see KNOWN_ISSUES). Proposed talents are unreviewed by the author,
  unbalanced and not checked against the novels.
- Next: Chris reviews trees in the calculator -> lock Warrior's next nodes -> implement them in Unity
  (data-driven talent assets rather than static WarriorTalents) -> Druid four-path slice.

## Exact locations

- **Git repo (2026-09-29):**
  - `D:\code\mmo` on branch `main`, first commit `72b4af7`. The repo root also holds `tools\` and `CLAUDE.md`.
  - Never commit the game into the unrelated `D:\code` repo.
  - Git is at `C:\Program Files\Git\cmd\git.exe` (not on PATH).
  - The author is repo-local (`Chris Underwood <chrisu2@gmail.com>`), and there is no global identity.
  - LFS is local to this repo.
- **Backup:** `J:\claude\unity projects\mmo`, a full copy including `.git`.
  - Run `D:\code\mmo\tools\Backup.ps1` after commits. It is additive; `-Full` includes Library, and `-Mirror` makes an exact copy.
  - J: is slow (about 2 MB/s).
- Lore: `D:\code\crulanda`
- Chris's save: `C:\Users\chris\AppData\LocalLow\Crulanda\Crulanda - The Quiet Trail\CrulandaEncounter`. Save backups: workspace `work\save-backups`.
- Screenshots: workspace `work\world-captures` and `work\ui-captures`.
- Actual Unity project: `D:\code\mmo\New Unity Project`
- Assets: `D:\code\mmo\New Unity Project\Assets`
- Project docs: `D:\code\mmo\New Unity Project\Docs`
- Unity: `D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe`
- Validation project copy: `C:\Users\chris\Documents\Codex\2026-09-28\hel\work\encounter-validation`
- Workspace: `C:\Users\chris\Documents\Codex\2026-09-28\hel`
- Outputs: `C:\Users\chris\Documents\Codex\2026-09-28\hel\outputs`
- Latest packaged player: `outputs\Crulanda-Playable\Crulanda.exe` under the workspace above.
- Original brief: `C:\Users\chris\Downloads\CRULANDA_UNITY6_CLAUDE_MASTER.md`
- Supplemental archive: `C:\Users\chris\Downloads\files.zip` (contains Phase 0 archive plus docs).
- Original brief copied into project: `Docs\PROJECT_MASTER.md`.
- Calculator reference screenshot: `C:\Users\chris\AppData\Local\Temp\codex-clipboard-27f154d6-8cf3-418c-a2d4-da297c823e45.png`.
- Bundled Python: `C:\Users\chris\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe`
- Bundled Node: `C:\Users\chris\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe`

This session had full filesystem access and used PowerShell. Since 2026-09-29 the project is under git (see above);
earlier checkpoints are zip files in `work\checkpoints`.
Do not overwrite or recreate the project or scene to update it. Preserve .meta GUIDs and user saves.

## User direction and feedback

- User repeatedly confirmed the project path above and granted full access.
- Integrate the supplied foundation and validate in actual Unity before expanding the sandbox.
- Handle technical testing autonomously; do not ask the user to play every tiny feature.
- User confirmed the playable encounter worked, then said combat was too fast. It was slowed.
- User later saw dead mobs from saved progress. We backed up their save and restarted fresh.
- User noted walking through scenery. Solid scenery collision/navigation were added and tested.
- User asked to see auto-attack timing. Do NOT add a separate toggle: keep auto-attacks starting from Strike/Challenge. Show countdown/progress between swings.
- User wants 3–4 base builds per class, combining classic class trees, branching upgrades and hybrid investment.
- Explicit examples: Warrior Tank/DPS/Support; Druid Tank/Melee/Healer/Ranged.
- User then explicitly asked for the matrix for ALL classes. It has been expanded.
- Latest feature request: a calculator visually like the supplied classic talent-tree screenshot, for every build, to explore choices and judge viability.
- Be candid about proposed estimates versus measured game balance. No false claims that every class is playable.

## Completed foundation / Phase 1

Imported and repaired the provided Phase 0 foundation, then built the playable encounter.
Assemblies include Core, Data, Gameplay, Persistence, DevTools, Game, Editor, Encounter, Abilities and Combat.

Implemented:
- Stable actor identity, registry, health/resources, stats/modifiers, content registry and persistence infrastructure.
- Third-person movement, jump, orbit/zoom camera, tab/click enemy targeting.
- Automatic melee attacks plus Strike, Challenge (taunt), Guard.
- Three sentries, threat, leash, death/recovery and recruitable healer Mira.
- Healer casting, mana, healing threat and opportunistic damage.
- XP/levels, one loot/equipment upgrade, reward deduplication, save/load and Repeat Trail.
- Shared per-actor ability runtime: cooldowns, global cooldowns, casts, interruption and resource commitment.
- Shared Combatant/CombatMath, armor mitigation, timed statuses and derived stats.
- Status refresh without magnitude stacking; expiry/death/disable/rebuild cleanup.
- Batched stat mutations invalidate all related caches before notifications; regression coverage for reentrancy and lethal changes.
- Time-based proximity threat rather than frame-dependent accumulation.
- Solid scenery collision and blocked NavMesh footprints for tent, crate, rocks, trunks, ruins and boundaries.
- Visible automatic swing countdown/progress; no extra activation control.

Current scene: `Assets/Crulanda/Scenes/PlayableEncounter.unity`.
Original foundation scene: `Assets/Crulanda/Scenes/Phase0_TestMap.unity`.
Do not confuse the two; the foundation map has no playable character loop.

Pacing defaults:
- Regular sentries 240 HP, veteran 340 HP.
- Player/enemy swing interval 2.6 seconds.
- Strike +12 damage, 5-second cooldown.
- Guard baseline 5 seconds, incoming damage multiplier 0.4.
- Challenge taunt 3 seconds.
- Healer heal 42, mana cost 18, cast 1.5s, cooldown 3.5s; bolt 7 every 4s.
- Historical fresh-character fight timings about 21/24/35 seconds after corrected threat.

## Phase 2 foundation already implemented

Files under `Assets/Crulanda/Scripts/Encounter`:
- `ClassDefinition.cs`: ClassDefinition, ClassAbilityUnlock, ClassLoadout. Profiles hold identity, role, resource capacity/regen, derived stats and ordered ability unlocks. IDs resolve against the catalog; invalid/duplicate/unknown unlocks are rejected.
- `BuildTree.cs`: validates complete proposed rank allocations with rank/level gates, prerequisites, branch investment, budget, exclusive choices and reachable purchase order. Rejects cyclic or missing graph prerequisites.
- EncounterContent now has `playerClass`; current serialized content is `class.warrior` with all original three actions available at level 1.
- Session gates abilities before spending costs/effects and derives action ordering from the class profile.
- HUD reads class/resource names and action slots from data. Number keys 1–8 are supported.
- Actor.ConfigureResource configures class resource at spawn. Derived stats use playerClass.stats.
- Legacy EncounterContent.playerStats remains for asset compatibility but is no longer authoritative; existing values were copied to playerClass.stats.

No class selection, Druid forms, extra playable class kits or full equipment restrictions/trainers exist yet.

## NEW: implemented Warrior talent prototype (latest code/build)

This is newer than several older docs stating no tree exists.

New file: `Assets/Crulanda/Scripts/Encounter/WarriorTalents.cs`.
Nine nodes across three columns, 12 possible rank points in total:

| Path | Early node (2 ranks, level 1) | Middle (1 rank, level 3, requires early 2/2) | Signature (1 rank, level 5, requires middle) |
|---|---|---|---|
| Tank | Tempered armor: +8 armor/rank | Steady challenge: landed Challenge refunds 10 vigor | Bulwark: Guard lasts 7s and reduces damage by 75% |
| DPS | Weapon pressure: +2 weapon damage/rank, including autos | Finishing strike: Strike +8 against targets below 40% HP | Sweeping strike: Strike hits other living sentries within 4m of target for half raw damage; can pull extras |
| Support | Rally reserve: +10 max vigor/rank, no extra regen | Rallying challenge: landed Challenge restores 12 mana to living/recruited Mira within 13m | Shared shelter: Guard heals nearby Mira for 20 and gives 30% damage reduction for 5s |

Rules:
- One talent point per level including level 1; capped at 10 for this prototype.
- All three signature nodes share an exclusive group: only one signature can be chosen.
- Cross-path investment in earlier talents is allowed within the budget.
- Changes/refunds/full reset require alive, unpaused and out of combat.
- Individual refunds validate the entire allocation; cannot orphan a dependent talent.
- Changes save immediately; full respec is free during the prototype.
- Talent stat modifiers have a dedicated source and are removed/reapplied, preventing stacking.
- Respec/allocation changes remove active Guard/shared-shelter effects so talents do not leave temporary benefits behind.
- Support healing creates threat attributed to the player.
- Build UI opens with B or the Talents button. It has three connected columns, descriptions, rank counts, requirements, invest/refund and full reset.
- Movement, target cycling and ability number keys are suppressed while the tree is open. Combat itself is not paused; allocation becomes read-only during combat.

Modified files: EncounterSession.cs, EncounterHud.cs, EncounterProgress.cs, EncounterSave.cs, EncounterInput.cs, AdventurerMotor.cs.

Important: the in-game talent UI has NOT been visually inspected yet, and the user has NOT played this talent build.
The automated tests and player build passed. Do not equate that with completed visual QA or balanced builds.

## Save migration and compatibility

Current save folder:
`C:\Users\chris\AppData\LocalLow\Crulanda\Crulanda - The Quiet Trail\CrulandaEncounter`
Files: `encounter.save.json` plus `.bak`.

Latest EncounterSave writes envelope formatVersion 2, gameVersion 0.2.0.
- EncounterProgress now includes classId (`class.warrior`) and List<TalentRank> talents.
- Version 1 reads explicitly migrate to Warrior and an empty talent list, preserving IDs, progression, equipment, party and enemies.
- Version 2 validates known Warrior class identity and complete talent allocations.
- Unknown/invalid allocations reject loading; existing save-protection behavior blocks overwrite of unreadable startup saves.
- Legacy migration is persisted on the next normal save, not by overwriting during read.
- Transient statuses/cooldowns remain unpersisted.
- Only Warrior is currently supported by this save validator; expand it deliberately when other classes become playable.

Previous user save backup is under workspace `work\save-backups\<timestamp>`.
Do not reset or delete the user's progress just to test. Use isolated test save directories.

## Latest validation — VERIFIED

Unity 6000.6.3f1 ran these on the synchronized validation project copy, leaving the user's open project/editor untouched:
- **142/142 EditMode tests passed.**
- **22/22 PlayMode tests passed.**
- **Windows player build succeeded.**

New coverage:
- Hybrid budgets, exclusive signature choice, level gates and prerequisite refunds.
- Legacy save migration, v2 talent round trip, rejection of foreign talent IDs.
- Talent stats survive reload and do not stack after respec/reallocation.
- Tank Guard multiplier/duration and cleanup on respec.
- DPS cleave and refusal to respec in combat.
- Support mana restoration, healing and protection.
- Existing class gating, swing timing, scenery/NavMesh, combat/healer/loot/equip/save/load regressions still pass.

Reports copied to actual project `Docs\ValidationResults`:
`talents-edit-results.xml`, `talents-play-results.xml`, `talents-edit.log`, `talents-play.log`, `talents-build.log`.
Earlier reports remain for history.

Latest build was copied into `outputs\Crulanda-Playable` during handoff (no player process was running).
No user replay requested. No visual screenshot of the new tree was captured.

## Full roster/build matrix — complete as a design document

Project: `Docs\CLASS_BUILD_MATRIX.md`.
User-facing copy: `outputs\Crulanda-Class-Build-Matrix.md`.

Covers 20 entries / 61 proposed paths:
1. Warrior: Tank / DPS / Support.
2. Paladin: Oathguard / Judicator / Sanctuary.
3. Ranger: Marksman / Beastbond / Pathfinder.
4. Rogue / Assassin / Shadow Operative: Duelist / Assassin / Infiltrator (Spy grouped here).
5. Cleric: Restoration / Aegis / Censure.
6. Druid: Tank / Melee / Healer / Ranged.
7. Shaman: Spirit / Weapon-channeler / Totemkeeper.
8. Mage: Combustion / Heatweaver / Spellbinder.
9. Necromancer: Bone host / Withering / Soul ward.
10. Warlock: Pactbound / Affliction / Sacrifice.
11. Bard: Anthem / Elegist / Skirmisher.
12. Monk: Sentinel / Striker / Harmonist.
13. Knight: Bastion / Challenger / Oathkeeper.
14. Seer: Oracle / Fateweaver / Watcher.
15. Tinker: Construct keeper / Gadgeteer / Field technician.
16. Engineer: Artillerist / Demolitionist / Fortifier.
17. Healer / medicinal practitioner: Herbalist / Woundkeeper / Apothecary.
18. Mystic: Resonant / Prism guardian / Crystal binder.
19. Windcaller: Storm conductor / Gale keeper / Tempest.
20. Venom / Alchemical Specialist: Toxicologist / Catalyst / Chemist.

The original brief had 12 generic classes and later additional Crulanda seeds. Covering all produces 20 candidates,
not a decision to ship 20 classes. The up-to-15 final roster target and overlaps remain unresolved.
Tinker/Engineer, Knight/Paladin/Warrior, Cleric/Healer, Rogue/Assassin, etc. have explicit differentiation notes.
The user asked for ALL-class matrix coverage; do not limit the calculator to Warrior/Druid.
The newer 3–4-build instruction takes precedence over the brief's earlier up-to-3 specializations.

Novels were not supplied/cross-checked. Canon names/claims from the brief remain unverified.
Avoid treating proposed spell names and mechanics as established Crulanda lore.

## IMMEDIATE UNFINISHED WORK: visual all-class calculator

The user's reference shows tall parallel trees, icon nodes, rank badges, connecting arrows,
branch totals, tooltips/search, reset controls and a shared point budget. They want to see choices
for every class/build and use the calculator to explore viability.

What EXISTS:
1. `outputs\Crulanda-Build-Workshop.html` (~20 KB): an earlier, self-contained preview.
   - Actual 9-node Warrior prototype calculator with levels 1–10, ranked investment/refund and exclusive signatures.
   - Other classes have build-path cards copied from the matrix and hybrid comparison selections.
   - Does NOT yet match the full reference or provide detailed trees for all classes.
   - No saved-build system, robust viability model or visual/browser QA completed.
2. `work\calculator\generate.py`: draft data generator for the larger calculator.
3. `work\calculator\data.json` (~251 KB): generated 20 classes / 61 paths / 13 draft nodes per path = 793 draft nodes.
   - Seven tiers, branch-spend gates 0/5/10/15/20/25/30, proposed level gates, rank caps, prerequisite IDs.
   - Per-path proposed signature names derive from the matrix's mechanics.
   - Most passive nodes and numerical modifiers are generated templates, NOT individually designed/approved talents.
   - Provisional model modifiers: damage, healing, mitigation, support, control, capacity, efficiency, regen, coverage, drain.
   - There is NO completed renderer or viability evaluator consuming this new dataset yet.
   - No assertions of balanced builds are warranted. Review/refine placeholders, especially duplicated percentage nodes.
   - The intended full-tree point budget was still a design assumption (classic-style 51-point exploration was being considered), NOT a user-approved progression rule. Actual Unity prototype remains 1 point/level to level 10.

The reference screenshot path is listed above. Do not copy branded art/assets; use the tree organization as a visual reference.

What to do next:
1. Inspect the existing Unity talent UI visually and address layout/input issues before calling it presentation-ready.
2. Build the full standalone calculator with original icon/branch presentation, connected nodes, tooltips and locked/unlocked states.
3. Cover every class in the matrix; clearly distinguish implemented Warrior prototype talents from proposed full-game design talents.
4. Support add/refund, dependency-safe reset, branch/class totals, point/level budget, search and saved/imported/exported builds.
5. Add explicit assumptions and comparison results for modeled damage/healing/durability/resource pressure/utility.
   Allocation legality can be definitive; gameplay viability is speculative until classes/rotations/encounters are implemented.
   Do not fake simulation by presenting generic percentage sums as measured DPS or a definitive viability score.
6. Test prerequisite/refund/point-budget rules, imports, class changes, save slots and comparison behavior.
7. Visually verify the calculator and open it for Chris. User asked to SEE options, not only receive another prose matrix.
8. Continue Warrior build iteration and then Druid's four paths, using the calculator as a reviewed design surface.

## Practical validation workflow

Do not run two Unity processes against the same project directory concurrently.
Copy changed source/assets from actual project to validation copy, preserving relative paths.
Use hidden background processes for technical runs; do not close the user's open Unity editor.

PowerShell test pattern (substitute EditMode/PlayMode and report names):

```powershell
$v = 'C:\Users\chris\Documents\Codex\2026-09-28\hel\work\encounter-validation'
$unity = 'D:\unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @(
  '-batchmode','-nographics','-projectPath',('"'+$v+'"'),
  '-runTests','-testPlatform','PlayMode',
  '-testResults',('"'+$v+'\talents-play-results.xml"'),
  '-logFile',('"'+$v+'\talents-play.log"')
)
```

Do NOT add `-quit` to test runs. Wait for completion and inspect XML test-run totals/failures.
PlayMode full encounter runs take roughly 90–120 seconds.

Build pattern:

```powershell
Start-Process $unity -WindowStyle Hidden -PassThru -ArgumentList @(
  '-batchmode','-nographics','-quit','-projectPath',('"'+$v+'"'),
  '-executeMethod','Crulanda.EditorTools.EncounterBuildPlayer.Build',
  '-logFile',('"'+$v+'\talents-build.log"')
)
```

Output: validation project `Builds\Crulanda`.
Confirm `Build Finished, Result: Success` in the log, then copy to outputs only when the player is not running.
New Unity-generated .meta files should be copied back to actual project only if missing; preserve existing GUIDs.

There is an opt-in `--crulanda-capture <directory>` rendered smoke harness with isolated saves.
It captures camera-rendered world frames, NOT IMGUI overlays. It cannot visually validate the talent HUD.
Use actual window capture for HUD inspection, or a deliberate appropriate UI capture method.

## Controls and practical limitations

WASD move, Space jump, RMB orbit, wheel zoom, Tab/click target.
1 Strike starts automatic attacks; 2 Challenge also starts autos; 3 Guard.
E talk/recruit/revive/loot/travel; left-click selects NPCs; I bags; C character; L quest book; M map; B talents;
Ctrl (hold) sneak; F5 save; F9 load; R recover after death; Esc pause. Development builds: F10 level 10, F11 skip an hour.
Repeat Trail respawns enemies while keeping progression/equipment.

Art is placeholder primitives; HUD is IMGUI. No final animation/audio/world art.
Scenery collisions are added at runtime by EncounterNavigation based on existing prototype object names;
agent navigation excludes renderer-bound footprints. This should eventually become authored collision/navigation data.
Talent content is currently a static WarriorTalents definition in Encounter, not a general content-asset pipeline.
Support targets Mira specifically; general friendly targeting/party support is future work.
Cleave can aggro additional enemies intentionally. Talents are provisional and may change combat pacing.
No DOT/HoT/CC/dispels framework, full inventory/vendors, quests, full parties, offscreen population simulation or dungeons yet.
Do not claim Phase 2 complete just because the foundation and one small talent tree compile.

## Handoff housekeeping

Copies of this handoff are in project `Docs\CLAUDE_HANDOFF.md` and workspace `outputs\Crulanda-Claude-Handoff.md`.
Roadmap, Phase 2 plan, build matrix and known-issues docs received dated handoff notes correcting their older no-tree/schema-1 status.
The completed matrix and earlier browser preview remain available in outputs.
There were no active background Unity test/build processes at the last completion check.
No new calculator browser server was launched, and no new UI screenshot was captured.

## STOPPED 2026-10-02 ~00:40 (Chris's usage limit)
All agent work was stopped. Nothing is lost: each worktree keeps what its agents had committed, and each workflow can be
resumed with `Workflow({scriptPath, resumeFromRunId})` (finished agents replay from cache).
- Round 7 (notes 1-3) is on main (61a2e94, compile ALL OK, EditMode 369/369). Its detached full check was left running
  locally (no cost); read `hel\work\full-run.done`, `q-PlayMode.xml` and the tour shots, fix, publish.
- Stopped workflows (resume or rerun): art notes wf_1ca487e1-45f (sheep/cats, lumber trees, sun, caves, icons), colour
  wf_99f8008d-998, characters wf_755e3e44-eb0, history PDF wf_e1fd6182-250 (its research notes are in
  `scratchpad\history`; `tools/docs` holds a partial generator, uncommitted).
- Ask Chris before starting multi-agent runs again: tonight's used his Fable allowance and his 5-hour limit.
- The history PDF's writer finished a DRAFT before the stop: `Docs/Crulanda-Everything-Built-So-Far.pdf` (96 pages), built by
  `tools/docs/make_history_pdf.py` from `tools/docs/history_content.md`. It was NOT fact-checked or page-checked (the
  checkers never ran). Committed as a draft; check it before calling it final.

### NEXT SESSION, IN ORDER (cheap first; ask Chris before any multi-agent run, with a cost estimate)
1. Read `hel\work\full-run.done` and `encounter-validation\q-PlayMode.xml` for round 7 (notes 1-3, main at 61a2e94). Fix
   failures by hand, look at the tour shots note 1's write-up lists and `ui-captures\elite`, publish, back up.
2. Send Chris the PDF draft and ask if he wants it checked.
3. His notes 4-13 (PLAYTEST_NOTES.md), one at a time, by hand: 5 (ore green) is done on `fix/n56-ore-windfall` and only needs
   merging; then whichever he picks. Worktrees under `scratchpad\wt` hold any partial work from the stopped runs.

## UPDATE 2026-10-02 morning (worked alone, no agents, at Chris's request: "dont overload the system")
- **Round 7 PUBLISHED** (main e6cd39b): Chris's notes 1-3 (Oakhaven 560 m with Crowsfoot Hollow moved out; social pulls;
  harder elites and Bandit King). The full run's six PlayMode failures were four test bugs (one long path across the big
  zone, which Unity's pathfinder gives up on: tests now use `Tests/PlayMode/NavReach.Walkable`), Lark Hill's map mark on its
  secret (moved), and a test timeout; fixtures 34/34 after. Oakhaven's player build of the zone is now 11.6-13.5 s (was 6.4).
- Note for later: a mob that turns on a party member standing past its leash resets its whole group at once. Mira follows
  the player so it does not arise in play, but a far-off heal could be used to reset a fight.
- **Next, one at a time, asking Chris before anything with several agents:** the art notes (4-13) by hand, starting with
  what is already built on its branch: note 5 (ore green) on `fix/n56-ore-windfall`. Then send him the PDF draft.

## UPDATE 2026-10-02 midday: the other four zones grown, PUBLISHED (main 4c5dcc7, build of 12:27)
- Chris said "yes. grow about 20%": Khaven 410 m, the Peaks, the Ash Rim and the Verdant Shore 430 m; every camp within
  about 120 m of a home moved out whole to 125 m and more (`tools/wip/zonegrowth/grow_zone.py`, one zone a run; WORLD_ZONES.md
  "The other four zones grown"; ZoneGrowthTests.NearHomes names the ones left by design).
- The check found four things, all fixed: place_nodes.py's writer dropped the stations after the nodes (fixed; stations
  restored from d9afc92); exits and arrivals left off their roads where a road's last bend changed (snapped back along the
  road); the Fraying placed past the Ash Rim's Wasting curtain (grow_zone.py now keeps 15 m inside a curtain); a rabbit could
  respawn in the Brook pond (GameAnimal.NearestDry). New EditMode tests: exits on roads in every zone, nothing in the unmade,
  the creeks' line. EditMode 372/372; PlayMode fixtures 10/10 after; the zones toured.
- Chris's new notes while playing: **15** (a few trees' limbs do not meet their trunks) and **6 again** (the lumber trees still
  have sticks poking up; "need to look chopped down").
- **NEXT (in hand):** note 6, the felled tree. Design previewed with the software renderer (`scratchpad\artnotes\windfall\d3`,
  gen.py v2): an axe-cut stump (pale face, heartwood, a short hinge ridge, chips), the trunk limbed (flush stubs with pale
  ends) and bucked in two with pale cut ends, the lopped crown as a low heap of bare branches that stays when the logs are
  taken. Port it into `ZoneBuilder.Nodes.cs` Windfall (within -2.9..+2.5 m along X for WindfallYaw), then note 15 (find the
  tree builder whose limbs float), then merge note 5 (ore, `fix/n56-ore-windfall`), one build for all three.

## UPDATE 2026-10-02 afternoon: notes 4, 5, 6, 7 and 15 PUBLISHED (main dd653fa, build of 13:49)
- 13:41 build: note 6 felled lumber trees (Windfall rebuilt; preview tools in `tools/wip/render`, design `tools/wip/windfall`),
  note 15 limbs joined (ZoneBuilder.LimbAt; TreeLimbTests with ZoneBuilder.RecordWood), note 5 ore (fix/n56-ore-windfall
  merged). Full check: EditMode 372/372, PlayMode 174/174, five zones toured.
- 13:50 build: notes 4 and 7 (CritterBody: designer 0's sheep and jointed cat tail from the stopped art-notes run, previews in
  `scratchpad\artnotes\critters\d0`); the Oakhaven tour now shoots `oakhaven-98-critters-lineup` and `-cats`. HuntTests and
  VillageHomeTests 14/14.
- **Note 11 icons PUBLISHED 14:12** (main: IconDb, Resources/Icons, tools/art/make_icons.py; rerun it after adding items).
- **Open notes:** 9 (bloom and sun), 10 (baked cave lighting), 12 (smooth characters, "build them in code"),
  13 (high-fantasy colour), 14 (the PDF: draft committed, not checked). The stopped run's previews for 9, 10, 11 and 13 are in
  `scratchpad\artnotes\{sun,caves,icons,colour}` (no code in their worktrees). Ask Chris which next; work solo.

## UPDATE 2026-10-02 late afternoon: notes 11, 13, 16 PUBLISHED (15:33); note 12 (smooth characters) IN HAND, solo
- Published 14:12: note 11 icons. 15:33: note 13 colour (gear palettes, people, village) and note 16 (Khaven sign lantern).
- Ultracode was switched on for the session; Chris chose "Solo, as today" (memory crulanda-usage-budget).
- **Note 12, the smooth figure** (Chris: "still have the bubble forms/armor/characters"). Plan and state:
  - C1a DONE on main (c933194): SmoothBody.cs lofts one skinned body per region on a skeleton (Body, Spine, Chest, Neck, Head;
    Arm L/R + Forearm + Hand; Leg L/R + Shin + Foot), shoulder/hip pivots unchanged; head, face and hair on the Head bone;
    knees/elbows/feet in ActorVisual.WalkJoints/Bend; a limb carrying something rigid stays near straight (Carries).
    ActorVisual.Smooth=false gives the old block figure. Reference + preview: tools/wip/characters/body.py with
    tools/wip/render/mesh_view.py (preview-v2.png sent to Chris). EditMode 378/378 after the gear tests were moved to regions.
    Full check (tests, build, Oakhaven tour, wardrobe/fight shots) started 15:39.
  - C1b NEXT: held weapons/shields on the forearm bones (hand point (-.005,-.335,.01) in Forearm R space; shield
    (-.095,-.095,.06) in Forearm L); limb armour skinned to (pivot, lower bone[, foot]) in ActorVisual.GearArmor Fitted for
    roots off the body (weights by y: elbow at -.285, knee at -.43, ankle at -.84 in pivot space), cached; GearPartCount to
    count Renderers; tests: main hand parent "Forearm R", enemy "Body/Arm R/..." paths.
  - C2: rounder armour shells (square 3-4 on the smooth torso), cloth that moves (verlet bones: tabards, skirts, capes, robes,
    the long hair fall); C3: outfits (Dress) and class kits refitted; Pale and Keeper smooth.

## UPDATE 2026-10-02 evening: note 12 C1a+C1b PUBLISHED (16:58); then REAL MODELS (C3), Chris AFK till ~22:30
- C1b (limb armour and held gear skinned to the elbows and knees) published with C1a at 16:58; full check EditMode 378/378,
  PlayMode 174/174. Chris: "that still looks REALLY blocky" -> chose real models: "Find real models", "Quaternius free",
  "need females also"; armour to be REFIT, not redone. He granted download authority ("you have authority to download and
  do what is need to make this a success"). Purchases stay his: Quaternius' $20 tier adds knight/noble/wizard outfits.
- **The kits** (CC0): Universal Base Characters, Modular Character Outfits - Fantasy (Peasant, Ranger; m/f), Universal
  Animation Library (UAL1_Standard, 43 clips). Zips in the session scratchpad; `tools/wip/characters/quaternius_import.py
  <zip folder>` extracts into `Resources/Characters/{Bodies,Hair,Outfits,Animations,Textures}` (ORM -> MetalSmooth + Occlusion).
  `Editor/CharacterImport.cs`: humanoid import, texture types, clip settings (loop the _Loop clips, root baked into the pose),
  the kit materials (`Resources/Characters/Materials/*.mat`, so their shader variants ship), `Report()` (bones/meshes/clips to
  CharacterReport.txt beside the validation project) and `Prepare()`.
- **ModelFigure.cs**: the outfit's skeleton (male outfits use the slighter "Regular" skeleton; heads and necks match the
  Superhero body exactly), the Superhero head cut at the collar (HeadOnly: triangles >= half weighted to Head/neck), its eyes
  and brows, hair (male: SimpleParted/Buzzed/bald; female: Long/Buns/BuzzedFemale), beard, a hood (Ranger's, on any outfit),
  tints (skin, hair, shirt, breeches, hood), turned 180 (the files face -Z). Motion: a PlayableGraph mixer (idle/walk/jog/
  sprint by speed, sit, swim, sneak/crouch, talk, gather, death). Edit mode cannot run a graph on an Animator: `Sample` uses
  AnimationClip.SampleAnimation.
- **ActorVisual.Model.cs**: `ActorVisual.Models` (default on; off = the smooth figure). Frames rebuilt on the model's bones in
  its bind pose and copied from them every LateUpdate: Arm/Forearm/Hand and Leg/Shin/Foot (-Y down the bone, +Z front,
  scaled by limb length against .57 and .84), Head/Chest/Hips frames (old body space mapped onto the model's head, chest,
  hips). Gear slot roots on the head/chest/hips frames (SlotParent), limb armour skinned as before but slimmed (ArmGirth .8,
  LegGirth .72; SkinnedLimb cache keyed by joints and girth). Cover/Bare dye the model's cloth (or hands for gloves) and hide
  its belts/bracers. Old kit on the body is remapped to the frames by height; flat boards (aprons, tabards, capes, shawls)
  and skirt drums are left off, round head balls become the outfit's hood. Poses: PoseOf -> clip slot or old arm angles
  (PoseArm onto the model's arms), back bends for stoop/slump, swim lift from the clip's head height, ambushers crouch,
  the dead play Death01; the Body is only written for poses that lean/drop (elites' wind-up lean survives).
  `Preview(pose, walk, t)` / `PreviewDead()` pose it in edit mode. Women by name list/trade (Female()).
- **Editor/FigureCapture.cs** (`run_method.ps1 -Method Crulanda.EditorTools.FigureCapture.Run -Tag figures -Graphics`):
  people, trades, poses, armour and faces to `hel\work\ui-captures\figures` in about a minute, no build.
- `tools/validation/run_method.ps1` (new; `-Graphics` keeps the GPU). GPU skinning turned on (ProjectSettings, both copies).
- Tests: GearVisualTests run on the smooth figure (Models off); new ModelFigureTests (5); focus run 21/21. GearBinderTests
  accepts the chest frame. WorldLife's tankard hangs from `RightHandle` (rides the forearm).
- **Metas**: robocopy /MIR deletes what Unity made in the validation copy; copy new .meta/.mat files back after each Unity run
  (done for the kit, the materials, the scripts).
- Committed 4078ab7 (models). The first full run (18:20) was STOPPED in PlayMode: `Villager.Emerge` hit a destroyed
  MeshRenderer (Remap destroyed old board parts after WorldLife cached the villager's renderers) -> parts are now hidden and
  unparented first (Kill). Follow-ups in the same round: rounder shells on models (GearArmor `Shell`, RoundSquare 3.2, keys
  "+.round"), carry poses (`ActorVisual.Carry`, set by WorldLife Carry/DropLoad; FigureCapture "carry" row), the off-screen
  guard (`ModelFigure.Visible`: PoseArm and back bends only when the Animator posed the bones). Markers: hel\work\c3-start.marker
  (round3-start.marker is stale, 16:55).
- `Ranger_White` kit material: T_Ranger_White_BaseColor.png (tools/wip/characters/concord: the green cloth bleached to linen
  white by hue, leather and metal kept; other maps the Ranger's). `Spec.bleach` picks it for the Warrior, Collector, Warden,
  Outrider, Cultist, Deserter, Bandit King and the stranger. Clip start offsets now hash the figure's name (no global Random).
- NEXT for the models (seen in FigureCapture, not yet done): face pieces sit ~4 cm high (the head frame is lifted .12 for
  helmets; masks and mouth scarves want a face frame at the old .8 -> HeadBone+.07); big balls on the chest still read as
  bubbles (the skinner's pelt, the warden's bark pauldrons, the deserters' leather pauldron: drop chest spheres >= .2 on
  models); the stranger's side panels; children are scaled adults; the armour's own designs (C2 proper: plate shapes, moving
  cloth); a Peasant "bleached" copy for truer villager dyes; the $20 Quaternius tier (Chris's to buy) for knights/nobles.
- **PUBLISHED 2026-10-02 19:37 (d618963, build of 19:27)**: models + round 2. Full run 2: EditMode 383/383, PlayMode 173/174
  (HollowQuestTests' crown path, fixed in d618963). Backup OK.
- Round 3 (uncommitted at 19:38, in full run 3): `Face frame` (old face .835 eyes/.745 mouth -> HeadBone+.10/.04; items with
  y<.87 and z>=.09 on the head), chest Spheres >= .2 left off, Peasant figures `bleach` (their Ranger hood dyes true), the
  merchant's collar box off models. `round3_toggle.py` (scratchpad) can back it in or out.
- **PUBLISHED 2026-10-02 20:38 (round 3, build of 20:28)**: full run 3 EditMode 383/383, PlayMode 174/174, Oakhaven toured,
  HUD/wardrobe/loot/fight shots; no exceptions in any player log. Chris was sent tools/wip/characters/models-preview.jpg.
- Round 4 (full run 4 started 20:39): villager height .96-1.04 (Spec.scale by variant), `ChildHead` 1.22 (Head bone scaled
  before the frames; head and face frames scale with it), stone figures use KitMat("Stone") (no asset: plain matte grey).
- **PUBLISHED 2026-10-02 21:38 (round 4, build of 21:29)**: full run 4 EditMode 383/383, PlayMode 174/174, Oakhaven toured,
  all shots taken, no exceptions in any player log.
- **Round 5 (night, in full run 4 started ~23:05):** `ModelArmour.cs` (Cloud of the model's clothes in the bind pose by piece:
  shirt = torso, sleeves = arms, trousers/boots = legs; cages per frame from convex outlines per 1.5 cm band; Warp keeps
  BodyGap .62 / LimbGap .55 of the old clearance; hands/feet as boxes; weights from the 4 nearest cloth points, skirts lean to
  the pelvis). GearArmor.Fitted sends every non-head group on a model to `ModelPart` (SkinnedMeshRenderer on the model's 65
  bones). Hats: `HeadOutline` (skull, and skull+hair) in head-frame space; `HatFit` (rim to eyes+.065, crown to clear the skull
  +.03), `Cap` (head shells for ball and pillbox caps), peaks to the front rim; armour head.cap/kettle/wrap/circlet/crown use it.
  Swim still = "swim" slowed to .35. Mira's hood = her cloth. ModelFigureTests 6 (new: armour follows the form, hats).
- **PUBLISHED 2026-10-02 23:44 (round 5, efba82b)**: armour that follows the form and fitted hats (see CHANGELOG).
- **Round 6, feet (PUBLISHED 2026-10-03 00:44)**: `ModelArmour` cages keep a leg's foot as sections along z (`fz0`, `nf`, `fc`
  middles, `fr` radii per angle; convex hull per 1.5 cm section with its neighbours); the old boot is `FootZ/FootW/FootH/FootY`
  (heel to toe: z, half-width, half-height, middle) as a squarish oval, mapped section by section (`AlongFoot`), clearance
  kept at LimbGap. The hand is still box to box. Full run (c3f-start.marker): EditMode 384/384, PlayMode 174/174, Oakhaven
  toured.
- **Round 7, real animals (PUBLISHED 2026-10-03 01:59)**: `Resources/Creatures` (Wolf, Stag, Deer FBX from the poly.pizza
  bundle "Animated Animal Pack" = Quaternius Ultimate Animated Animals, CC0; Quaternius's Google Drive was over its download
  quota; `tools/wip/animals/animals_import.py` copies them from the zip). `Editor/CreatureImport.cs`: Generic rig, materials
  by description, readable, normals smoothed 70, the "AnimalArmature|X" takes kept and named X, loops for idles/walk/gallop/
  eating; `CreatureImport.Report` writes CreatureReport.txt. `ModelBeast.cs`: Build(parent, kind, ground, height, Coat, shade)
  measures the rest pose from the baked skin (renderer bounds carry a margin) and scales so the head (never the antlers) stands
  at `height`; colours are the file's (linear, x.8 diffuse factor: undone, then to gamma) or a Coat by material name, with
  `Native` coats for Stag/Deer; `Rounded` (PN-triangle midpoints, normals per colour patch; NB Vector3.normalized is zero under
  1e-5 and these meshes are at 1/100 scale: `Unit`); a PlayableGraph with slots idle/idle2/headlow/eat/walk/run/attack/kick/
  hitL/hitR/death/jump, ticked by its own Driver component (so a dead game animal's death plays on); Drive(speed, standing);
  Play(one-shot); Die/Revive; Sample for edit mode. `Hips` = the "Back" bone (Body is the low root). `ActorVisual.Beasts.cs`:
  BuildModelBeast for Wolf (ash = variant odd; Old Whitefoot by name; gloom), Stag (doe = Deer kind); BeastLate (dead: body set
  upright, death held; LyingLow: headlow; standing: idle/idle2 or eat/idle; fighting: idle); Strike (EncounterEnemy swing and
  elite ReleaseBlow), Flinch (Receive). CritterBody "deer": MakeModel (r < .5 Stag 1.75 m, else Deer 1.55 m); Stride/Rest/
  Alert/LieDown/StandUp drive the model; Part uses DestroyImmediate in edit mode. Heights: wolf 1.0 (elite 1.12), stag 1.9,
  doe 1.6 (elite x1.1). `Editor/CreatureCapture.cs` rows beasts-1/before/round/wolf/stag; sheet tools/wip/characters/show/
  6-animals.jpg. Batch PlayMode draws nothing: the Animator (CullUpdateTransforms) leaves bones be, so PlayMode tests check
  state (Model.Dead), EditMode tests check poses (Sample).
- **Round 8, farm animals (PUBLISHED 2026-10-03 13:06, commit see git log: Farm animals)**: Horse, Donkey and Cow FBX added to Resources/Creatures
  (`animals_import.py <zip> Horse Donkey Cow`); CritterBody kinds "horse", "donkey", "cow" (MakeModel with `FarmCoat(kind, r)`:
  four horse coats, four cow coats, two donkey coats; heights 2.15 / 1.6 / 1.6 m; Speed 1.3/.9/.7, FleeSpeed 2.2/1.8,
  FleeRadius 2.2/1.8; Rest grazes two thirds of the time; models off: `PlainBeast`, the old deer's body scaled, which the deer
  now uses too). GameAnimals.NeverHunted gains horse, donkey, cow. Oakhaven's life.critters gains five groups at the END of the
  list (the earlier critters' spawn draws unchanged): horse (-8.5,-9) r1.5 x1 (the Cask), horse (22,128) r6 x2 (Carder's field
  barn), donkey (-63,-49) r2 x1 (the mill), cow (92,-119) r8 x4 (Brook), cow (-114,50) r6 x3 (Harrow); canonNote labels them
  GAME-ONLY. CreatureCapture rows beasts-farm and beasts-cows; sheet tools/wip/characters/show/7-farm.jpg. Zone map plots:
  scratchpad zonemap.py (roads, water, props, critters, camps on a 10 m grid).
- **Round 9, boars (PUBLISHED 2026-10-03 14:42)**: CraftPix "Free Wild Animal 3D Low Poly Models" (free-game-assets.itch.io, no
  login there; craftpix.net itself wants an account; licence: CraftPix freebie, commercial use OK, no redistribution of the
  art; zip kept at tools/wip/animals/source/CraftPix-Free-Wild-Animal-3D-Models.zip, git-ignored): Resources/Creatures/Boar.fbx
  + wild_animals_map.png (a 64x64 palette; CreatureImport: no mipmaps, uncompressed) + CraftPix-License.txt. The set's models
  are RIGGED, NO CLIPS: `ModelBeast.Proc.cs` (partial) moves them: MakeRig finds the chest/rear spine (Neck's and Tail_1's
  parents), legs = chains of 3+ bones hanging from them (side and front by position in the animal's frame), Pose() resets to
  rest and swings legs about the animal's right axis (diagonal pairs, middle joint folds on the forward swing), neck/head/tail,
  and moves/rolls the whole model (lunge, drop, death roll 88 deg about its middle onto the ground: halfWidth). Slots as the
  clip animals (idle, idle2, eat, headlow, walk, run, attack, kick, hitL/R, death, jump). ModelBeast: textured materials keep
  their atlas (Atlas()), facing from hips to head in any direction (Shape.yaw), Hips falls back to Spine_2, Neck to Neck.
  ActorVisual.Beasts: Boar coats by name (wild .56/.49/.43, Carrion, Mire, Rockhide/Scree, gloom x.8), height .95 (elite 1.1).
  CreatureCapture rows beasts-boar, beasts-boar-moves; sheet tools/wip/characters/show/8-boar.jpg.
- **Round 10, Khaven camps and quests (PUBLISHED 2026-10-03 17:25 with the boards round, below; commit 12d8ddc)**: khaven.json camps 10-13 appended (Scarp hollows
  (-100,115) hollow x5 L4-5; Thicket deserters (-150,-108) deserter x5 ambush; Heights scree spiders (160,25) spider x5 L5;
  The Hush-Mother (-165,112) tag wolf x1 L6, not elite: an elite needs a signature loot list and the 104-item count is pinned by
  LootDataTests), a "Deserters' strongbox" crates prop (interact only) and "The Thicket" landmark; Quests/khaven.json +5 quests
  and item.charnel_silk (collect from kill:mob.spider.khaven*). Quest kill targets by camp index: mob.hollow.khaven.10.* and
  mob.wolf.khaven.13.*. Rules met on the way: a new tag needs a SocialRulesTests kind and (for beasts) an ItemTests hide table;
  camps 125 m+ from houses (ZoneGrowthTests). Zone maps: scratchpad zonemap.py.
- **Round 11, notice boards + crowns (PUBLISHED 2026-10-03 17:25, the boards commit, see git log)**: `Bounties.cs` (BoardState in Progress.boards; Progress.days
  ticks when the clock crosses 06:00 in EncounterSession.Update; Board(zone) draws Slots=3 from the zone's kind "bounty" quests,
  giver/turnIn "board", seeded by zone+day; RareChance .015 a slot; Entries(); Finished(); ActiveRare(); CourierId()). No save
  format bump: the two new Progress fields are declared BEFORE the lists the save tests pin (payload tail unchanged; older saves
  load with days 0). Quests: kind "bounty" (never in questsDone; QuestDef.rare/poster). Session: UseInteractable on a "board"
  prop -> OpenBoard (TalkTo("board") first so bring postings hand over; conversation npc = Bounties.BoardName; HUD intro "Pinned to
  the board:"); AcceptQuest of a rare posting -> SpawnCourier (ActorLook.Collector, level zone max+2, on roads[0] two thirds
  along, Camp=false, Enemies.Add); CompleteQuest -> Boards.Finished + DespawnCourier; Update respawns a courier for an active
  rare posting on load. ZoneBuilder prop kind "board" (NoticeBoard: two stakes, a board, a pent roof, four pinned notices).
  Content: Quests/bounties.json (22 postings: Oakhaven 4+1, Khaven 4+1, Peaks 3+1, Ash Rim 3+1, Verdant 3+1; kill targets by camp
  tag, bring items are gathered/hunted materials only, visits at night); items.json mat.geode_shard (value 30, CANON-EXPANDED)
  + temper.{copper,bogiron,ridgesteel,ashsteel,veridian} (quality 1 shard, stack 20; the craft.* curve tests pin their 21 pieces) (quality 3, mainhand); professions.json recipe.tempered_* (3
  bars + wood + shard; value <= 1.5x cost); looks.json looks on the concord palette; a "board" prop in each zone by the inn/hall.
  "gold" -> "crowns" in every user-facing string (Discoveries, HUD Items/Quests, Loot, Session, Items.cs, Quests.cs; the data
  fields stay "gold"). Encounter.asset must list bounties.json (the register step adds it; copy the asset back from the validation
  copy).
- **Round 12, the Peaks' camps and quests (PUBLISHED 2026-10-03 22:25, see git log)**: peaks.json camps 9-12 appended (Adit
  hollows (-128,-62) hollow x5 L7-8; Tarn shadows (-52,152) paleshadow x3 L8; False pilgrims (140,-70) cultist x5 ambush;
  The Umbra Watcher (-140,40) pale x1 L8, not elite), a "False pilgrims' packs" crates prop (inserted before the board), the
  canonNote; Quests/peaks.json +5 (npc.maddoc.adit, npc.tarsk.tarn, faction.sandthrone.2 (reputation copied from
  faction.sandthrone.1), side.peaks.shrine (Maddoc -> Tarsk, night visit), npc.yara.watcher). No new items, tags or looks.
- **Round 13, the sun + notes 17-19 (PUBLISHED 2026-10-03 23:35, see git log)**: ZonePost.Sun (pass 5 of Post.shader: disc,
  halo, glow on sky pixels from the depth texture; corner rays now always set by Rays()); shafts x .6 x (1 + 1.3 low); veil in
  the composite; Water.shader sunGlint (pow 120 lobe, uncapped). EncounterCapture world tour: 82-sun-dawn/noon/dusk (6.3, 12,
  18.7 h, weather forced Clear, camera set directly). Hats: brim <= 1.65 crown before HatFit. Zoom: EncounterInput.Zoom takes
  1-per-notch or 120-per-notch; AdventurerMotor 1.2 m a notch, 2.5-22 m. Staff: ActorVisual.ModelLate slings held gear
  taller than 1.3 m (HeldTall, measured in hand, cached per held array) when speed > 3.2 (back below 2.2), unless
  ActorVisual.Fighting(gameObject).

