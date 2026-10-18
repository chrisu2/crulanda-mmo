# Project roadmap

Updated 2026-10-05 (rewritten: the 2026-09-28 version had fallen behind). PROJECT_MASTER.md remains the design brief; this file
records where each of its phases stands and the order of the work. Update it every round that moves a phase.

## Chris's order, 2026-10-11 (about 14:26, verbatim)
"keep developing. once completed continue working on classes/perks/talent builds and add new classes/races based on lore canon. and all
phases after." So: finish the Adit pass (CLAUDE_HANDOFF 8A); then the classes (the three missing fourth paths: the Ranger's Skirmisher, the
Mage's Cinderwright, the Rogue's Swashbuckler; talent rows 4-6; perks), then new classes and races from the canon (Phase 9.2 and 9.3), then
every phase after. **No machines** holds over all of it (CRULANDA_CANON): Phase 9.2's Tinker as written (Klyther Forgeheart's drones and
scorpions) is machinery and is to be designed without it, or set aside, and the choice written down.

## Phases (PROJECT_MASTER.md)

| Phase | Goal | Status (2026-10-09; what is verified, tested or only built: CLAUDE_HANDOFF.md, 2026-10-11) |
|---|---|---|
| 0 | Foundation | Done |
| 1 | Combat sandbox | Done: shared abilities, threat, elites, level scaling (mobs above you hit harder, red at +3), crowd control (CC_DESIGN.md) |
| 2 | Class loops | Seven classes (Warrior, Druid, Paladin, Ranger, Mage, Rogue, Archivist), complete for the cap of 15; deeper talents for 16-30 wait on Phase 9 (CLASS_STATUS.md has each class's state); Mira's healer kit |
| 3 | Inventory, items, vendors | Done: bags, gear and looks, named loot and legendaries, vendors, professions and crafting, chests, quest gear (2026-10-09) |
| 4 | Questing | Done: five zones of quests, the chronicle, notice boards and bounties, discoveries, achievements |
| 5 | **SimAdventurers** (the brief's "most important feature") | Done (5.0-5.7, 2026-10-07) |
| 6 | Group gameplay | Done with 5.6-5.7: parties, roles, need/greed, sim-led runs, guilds |
| 7 | First dungeon | Done: Crowsfoot Hollow (Oakhaven) and the Root-Mother's Deep (Verdant Shore) |
| 8 | Vertical slice | **Now**: the Sealed Adit (DUNGEON_DESIGN.md section 9: D1 the caves, D2 boss mechanics, D3 gates and keys, D4 the Weaver's escort and D6 quests and loot done by 2026-10-09; left: Nix's two-part fight, the rest of D5 mob behaviours, D7 sims in the dungeon, D8 the rail ride out), crowd control C1-C5 (CC_DESIGN.md); then **the talents: a fourth path for every class and tiers 4-6 (TALENT_DEPTH.md, decided 2026-10-09: one point a level, Paladin first)**, the Medieval Village Kit, world art, polish and the playtest-note rounds |
| 8.5 | **Gear and figures overhaul** (Chris, 2026-10-10: "we really need a complete overhaul on armor/weapons/helms, all seem to not fit properly"; "its own phase down the line") | Queued (see below) |
| 9 | **The wider world** (Chris, 2026-10-07: "more zones, more classes, more races based on the canon of the novels, after phase 8") | Planned (see below) |

## Phase 5 plan (Chris chose 2026-10-05: Phase 5 now; classes Paladin, Ranger, Mage; faster tests first)

| Step | What gets done | Size |
|---|---|---|
| 5.0 Groundwork (DONE 2026-10-05) | Faster test runs: run only the tests a change touches; one scene load per test fixture; wait-until instead of fixed waits; PlayMode split across two validation copies. Landed as: select_tests.ps1 (only the fixtures a change names; NONE for art and docs) and two lanes (tests in encounter-validation, build, tours and captures in encounter-validation-b at the same time); a docs-only round took 12 min. Shared scene loads and wait-until left for later (most fixture time is simulated village days, not loading). | 1 round |
| 5.1 Class kits (DONE 2026-10-05: Paladin, Ranger, Mage) | Paladin, Ranger, Mage: 8-10 abilities each, an AI rotation, gear rules and looks, playable by the player too (full talent trees later). Names provisional until checked against the books. | 3 rounds |
| 5.2 Sim profiles (DONE 2026-10-06: the roster, the world slot, figures in the zone by the clock; gear on them and a who list; mobs scale to your group) | ~20 persistent SimAdventurers (stable ids; name, race, class, level, gear, personality, home zone), saved; materialised as figures in the player's zone, dematerialised when away. | 2 rounds |
| 5.2b Invite (Chris moved it up, 2026-10-06) | Click a sim, Invite: it joins like Mira (up to three), follows, fights your target in its class's way (melee closes, Ranger and Mage shoot, Druid and Paladin also heal), is in the party frames and can be turned on; Leave party sends it back. Not saved. The full party system stays in 5.6. | 1 round |
| 5.3 In-world life (DONE 2026-10-06: 5.3a choosing, hunting, gathering, the inn, tagging; 5.3b levelling, death and the corpse run, roads between zones; 5.3c trades, the stall, upgrades, the forge) | Utility-AI activities: questing (camp mobs), gathering, travelling the roads, shopping, resting at inns, dying and the corpse run, levelling and gearing up; personality weights the choices. | 3 rounds |
| 5.4 Offscreen world (DONE 2026-10-07: the unseen hunt, gather, trade and level by the clock, and take the roads between zones) | Coarse simulation of the unloaded: levels, zone moves, online and offline hours by the world clock. | 1-2 rounds |
| 5.5 Chat and memory (DONE: zone chat 2026-10-06; whispers, friends and memory 2026-10-07) | Say, Zone, Whisper and System channels from personality and event templates; a who list and friends; compact social memory and relationships (Stranger to Friend or Rival). | 2 rounds |
| 5.6 Groups (DONE 2026-10-07: invite, frames, need/greed, roles, assist, sims asking you, runs to camps) | Invite, accept, leave, party frames, roles, assist, shared kill credit, need and greed, sims inviting the player, dungeon runs with sims. | 3 rounds |
| 5.7 Sims complete (Chris, 2026-10-07: "I want the sims complete first", before the enclave and Khaven) | 1. the party travels with you and is kept (DONE round 27); 2. sims invite you (DONE round 27); 3. dungeon runs led by a sim (DONE round 27); 4. trading with sims (DONE round 27); 5. guilds (chat, tags, joining) (DONE round 27); 6. a bigger population (about 40, more at the high zones) (DONE round 27). | 4-6 rounds |

About 15-17 rounds. No external language model: local state machines and utility AI only (brief section 13).

## Phase 8.5 plan: gear and figures overhaul (queued 2026-10-10; after the talents' fourth paths)
**Why:** armour, weapons and helms are drawn onto our own figures and fitted by measurement (ActorVisual.Gear, GearArmor, GearWeapons;
SeatHead and HatFit for the head). It works for the drawn caps and hoods, not for the models: every Modular Hero helm sits on top of the
skull and some are tilted (wardrobe captures 06-helms-f..j, 2026-10-10; Chris's screenshot of four crowned helms); a level-1 sim's
armour reads as mismatched pieces (note 101); a stowed wand went across the back like a sword (note 101; fixed for wands, daggers and
knives 2026-10-10, ActorVisual.Short).
**What was found (2026-10-10):** CHOSAN's Modular Hero pack (bought; its README in Resources/Props/ModularHero/Documentation) is a
modular character: one base character on one shared skeleton, with headgear, shoulder pads, chest plates, gauntlets, belts, leg guards,
boots and outfits as skinned meshes made to fit it, and light/medium/heavy weapon sets. Only its Armor Parts.fbx and Face Parts.fbx were
imported (as Generic rigs); the base character prefab, the weapons, the poses and the demo are not in the project. The helms are pulled
out of that rig as loose nodes and pinned to our heads, which is why they cannot fit.
**The choice to make when the phase starts:** (a) build the player, sims and villagers on that pack's rig so its pieces fit by
construction (needs the full package re-downloaded from the Asset Store, our animations retargeted through a Humanoid avatar, and a
mapping from our items' looks to its parts, keeping the drawn pieces for what it lacks: cloth, leather, the zones' palettes); or
(b) keep our figures and rewrite the fitting rules piece by piece. (a) is the lasting fix; (b) is the one that keeps failing.
**Chris chose (a)** (2026-10-15 ~22:00, the question prompt, verbatim: "Rebuild on the hero pack's body (Recommended)"). The full
package is not on disk (not in the Asset Store cache): Chris downloads "Modular Hero" (CHOSAN) in the Package Manager's My Assets first.
**Chris changed the pack (2026-10-18 ~18:15).** Asked the CHOSAN pack's name, he answered "GanzSe FREE Modular Character - Fantasy Low
Poly Pack" (downloaded 18:10 into the Asset Store cache: a base body, head/chest/arm/belt/legs/feet armour in 6 styles x 3 colours, hair,
beards, eyes, faces; no weapons), and chose it at the question prompt (verbatim): "GanzSe (Recommended)". Our weapons stay. First step:
import into a test copy, check the rig takes our animations, a wardrobe line-up; CHOSAN's partial import stays until GanzSe replaces it.
Read from the package without Unity (2026-10-18): imported as Humanoid (animationType 3, auto-mapped); one skeleton (spine_01-04,
neck, head, shoulder/upperarm/forearm/hand with five-finger chains, upperleg/shin/foot/toes, _l/_r) with every armour piece skinned to
it in "GanzSe Free Modular Character 1_1.fbx" (update 1.1) and "Free Low Poly Modular Character.fbx"; 215 non-skinned part prefabs;
a demo ModularHeroController (shows one child per category). Chris could not find CHOSAN's pack in My Assets ("i dont see chosan pack
anywhere"); it is not needed now.
Then found in Downloads (2026-10-18 ~18:20): "Modular Hero.unitypackage" (CHOSAN, Oct 15; the old p85-hero branch took its body)
and "Modular Character Outfits - Fantasy[Standard].zip" (Quaternius: the same files as Resources/Characters/Outfits, nothing new). Asked
again with CHOSAN on hand, Chris kept (verbatim) "GanzSe (Recommended)". p85-hero is superseded.
**Step 1 (GanzSe kit): WRITTEN, NOT RUN (2026-10-18, branch p85-ganzse; no Unity run, compile-checked only).** What was built:
- The pack's Built-In files in Resources/Characters/GanzSe with their own GUIDs: "GanzSe Free Modular Character 1_1.fbx" (update 1.1:
  every piece of 1.0 plus armour type 6), "Base Palette Material.mat" (Standard) and "Base Palette Texture.png" (LFS). Not brought: the
  1.0 model (a subset), the 215 non-skinned part prefabs and their part models (the skinned pieces are what fit), the demo scene and
  its script, the URP/HDRP extras. CharacterImport: the folder takes the rule every model there has (Humanoid, avatar from itself,
  readable, no clips); its palette texture is point-filtered, no mipmaps, uncompressed (the pack's own setting; mipmaps bleed swatches).
- GanzSeKit.cs: the switch (`GanzSeKit.On`, default off; `--crulanda-ganzse` on the command line turns it on) and the one table:
  every armour family to a weight (cloth type 4, leather 5, hide 3, fur 6, mail 1, plate 2) and a colourway (1-3) matched to the
  look's cloth, metal, or an epic's trim; families the pack has nothing for stay drawn (hoods, caps, scarves, masks, circlets, crowns,
  the shoulders, the neck; every weapon and shield is ours as before); each look's and trade's own clothes where no gear is worn.
- ModelFigure.GanzSe.cs: the body built from the pack (its skeleton copied bone by bone, its Humanoid avatar, the body, eyes, brows,
  nose, ears, hair, a beard, one piece per slot), the bones in the same fields as the Quaternius kit's, so the frames and weapons hang
  on it unchanged. It faces +Z in its file (not turned like the Quaternius kit). ActorVisual.GanzSe.cs: a worn piece the pack covers
  shows the pack's piece and no drawn armour; a chest piece with armoured shoulders (types 1, 4, 6) hides the drawn shoulder piece.
- Tests: GanzSeKitTests (EditMode): the switch is off; every armour family maps to a piece in the file or is kept drawn; cloth,
  leather, mail and plate read differently; every look and trade wears pieces in the file; the imported file holds exactly the
  table's pieces on a Humanoid avatar with spine_01 as its hips, facing +Z, in metres; the import rule; a man built on it wears and
  takes off its pieces, a woman and an orc stay Quaternius.
**What the pack cannot do (found 2026-10-18):** it has only a man's body (women and orcs keep the Quaternius figure: `GanzSeKit.Women`
stays off); no long hair; no robe, hood, cap or plain shirt (villagers wear its least warlike pieces: the jerkin, the hide top, the
caster's coat); one palette texture, so only the skin is tinted and colours come from the three colourways.
**The lead must run:** in lane A, the EditMode tests GanzSeKitTests and ModelFigureTests (the avatar's auto-mapping and the facing
are the unproven parts); then a build and the wardrobe line-up with the switch on: `Crulanda.exe --crulanda-wardrobe-capture <dir>
--crulanda-ganzse`, front, back and side, and one figure walking (our clips on its avatar). The switch stays off until Chris has
seen it.
**Animations (2026-10-10, brought up to date 2026-10-11):** Kevin Iglesias's Archer pack is in, in part (BowShot01 load, hold and release, BowIdle01; the rest when the bow is right, note 112); his Melee pack waits to be imported. The rest of this paragraph is from before they came:
a Kevin Iglesias archery set (the same maker as the locomotion) would give a real draw, aim and release on the same Humanoid avatar.
**Done when:** the wardrobe line-up (EncounterCapture --crulanda-wardrobe-capture) shows every slot's every family fitting on every
class's figure, and Chris has looked at it.

## Phase 8.6 plan: the living world's second pass (Chris, 2026-10-14 23:30-23:35, verbatim; queued, its place to be decided)
Chris: "so what is the phases look like? the villagers and npcs need better ai" and "more things to do. more incentives for
gathering/professions. make better gear. have gear change the way your character looks when equipping" (PLAYTEST_NOTES 203-204).
After Phase 8 (Chris, 23:25: "work on phase 8"). His answer to what he saw (23:40, verbatim, PLAYTEST_NOTES 205): "running into each
other and sometimes get stuck or delayed at door of inn..prob need double doors and inn a bit bigger so for people. need more / better
loot obtained from crafting armor/weapons/potions/food buffs. find recipes/ book of lore/still havent found many secrets. all this for
ALL Zones".
- **Villager and NPC AI:** they run into each other and get stuck or delayed at the inn's door; the inn's door made double and the
  inn a little bigger for the people in it; the villagers' way-finding and giving way to each other (every zone).
- **More to do; gathering and the professions worth doing:** more and better loot made by crafting (armour, weapons, potions, food
  buffs); recipes to find; a book of lore; the secrets ("still havent found many"): every zone.
- **Better gear, and gear that changes the character's look when equipped** (with Phase 8.5's fit overhaul: the one phase if Chris
  wants them together).

## Phase 9 plan: the wider world (after Phase 8; PROVISIONAL until each item is checked against the books)
Everything here is drawn from the lore folder (D:\code\crulanda: world_bible.md, the three books) and labelled as the game labels lore:
CANON where the books name it, CANON-EXPANDED where the game fills in round a canon place, GAME-ONLY for the rest.

| Step | What gets done | Canon source |
|---|---|---|
| 9.1 Zones | New zones on the world map, each with its band, quests, camps, trades and a dungeon or landmark: the Lowtowns of Argentis (industry and poverty under the Crown), the Crown itself (white stone and gold), Port Caelum and the Glittering Coast (glass ships, the Gilded Bay), the Iron Citadel (the Iron Pact's fortress-monastery in a dormant volcano), the Archive of Silence (the Silent Pilgrims' library-temple), Hallow's Creek (the first town the Wasting took), the Warrens (the goblins' tunnels under the High City), the Tide-Watched Shores (blue water, no ash). Order and bands to be chosen with Chris. | world_bible.md Geography; book1 (the Lowtowns, the Deep Veins, the Warrens); book2 (the Wasting) |
| 9.2 Classes | New class kits with talents, AI rotations, looks and sim support, modelled on the books' people: ~~the Tinker (Klyther Forgeheart: drones and scorpions, an engineer)~~ set aside 2026-10-11 as machinery (his book-3 salt-stone craft goes to the Salt-Mender), the Rogue (Valen: Thief's Grace, shards, vibration-locking), the Knight (Talira Frostveil, Kaelith Dawnstrike: swordmaster; "Stormlight" not used, see CLASS_BUILD_MATRIX.md), the Archivist (the Silent Pilgrims: the Original Song, echo-jars), the Salt-Mender (salt-magic of the West). Names provisional. | world_bible.md Characters and Factions |
| 9.3 Races | Playable and sim folk beyond the human regions: the goblins (the Weavers of the Warrens: vibration-locking, static-charting), the Forgeborn (Tynara Embercoil), the Veridian Keepers (beings of wood and moss; a Keeper playable only if the books allow), and the human peoples by region (Lowtowners, Coast folk, the North's survivors). Each with models, looks, voices in chat and a home zone. | world_bible.md Factions; the character sheets in chars/ |
| 9.4 The sims in the wider world | The roster grows with the zones (more bands, more homes); guilds and trades reach the new places. | ROADMAP 5.7 |

Each step is its own set of rounds; Chris picks the first zone, class and race when Phase 8 closes.
**The first zone (Chris, 2026-10-15 ~22:00, the question prompt, verbatim): "Lowtowns & Rust Market 16-19 (Recommended)"**, the 16-30
road's first band, east through Argentis's gate from the Shore. Hallow's Creek (DUNGEON2_DESIGN.md, 16-18) takes its recommended answers.
Chris's order with it: "keep going finish phase 8" then "them move to 9".
**Chris, 2026-10-16 ~14:50 (verbatim): "after this work on next dungeon and raise top level to 20. make sure there is a zone for 15-20 and a
dungeon for the same range".** The cap goes to 20 (not 19), with a zone and a dungeon for 15-20.
**Before it, the capital (Chris, 2026-10-15 ~22:25, verbatim):** "we need to implement a castle (central point) for auction house, library
(lore), leader of crulanda (canon lore), guild halls, bank, etc more as we come up with it. check maps for location it should be located".
His answers: the Crown of Argentis, open to everyone from level 1, High Councilor Aethelgard, castle first. CAPITAL_HUB.md.

**The 16-30 road (Chris, side chat 2026-10-07; PROVISIONAL, mostly Book 1):** levels 16-30 use places already on the world map:
the Lowtowns and the Rust Market (16-19), the Forge District (19-22), the Warrens with the Deep Veins dungeon (22-25), the Crown and
the Transit Hub (25-28), the Spire (28-30). The Shore's breadcrumb at 15 leads back east to Argentis's gate.
**The dungeon brief (Chris; DUNGEON_DESIGN.md section 0 governs):** four 5-player dungeons (1 tank, 1 healer, 3 damage/support), the
first a mix of the Deadmines, Wailing Caverns and Scarlet Monastery; bands low 10-15, mid 15-25, high 25-35.
**Three more dungeons on that road:** the Sludge-Tunnels (18-20, Forge District: the city's waste tunnels of Book 1, a sewer-and-foundry
run under the smelters, a syndicate boss), the Deep Veins (23-25, the Warrens: the penal-legion mine of book1 ch.9, geode seams, the
Blind Crow's goblins as allies, a Council overseer and his unmade labour as bosses), the Transit Hub (27-29, the Crown: the junction
of every soul-prism and geode, cargo lifts, redaction squads, a Councillor's lieutenant; its last boss drops the letter that opens the Spire).
**Raids (Chris, 2026-10-15, the question prompt: "Wait for the Spire at 30" and "20"):** no cap-15 raid; the Spire of Agony at 30 is
the first, and a raid is **twenty players** (you and nineteen sims). The class sets' higher tiers drop there (PLAYTEST_NOTES 228).
**Endgame at the cap** (30 later; the cap-15 Shore version dropped 2026-10-15): raids (the Spire of Agony first, twenty players: you and sims, a
guild run, several bosses with the Adit's mechanics at scale; the Ashlands Sanctum later); world bosses (one roaming elite per high
zone on a long timer, the Weave-Eater brood's mother on the Rim, a Pale King's shadow on the Shore, announced in Zone chat, a full
group needed, guilds forming up); challenges (timed dungeon runs with a rating the sims' guilds compete on, a weekly notice-board bounty,
rotating harder camps); rewards (set gear above the dungeons', titles, the Chronicle marking each kill). The pieces exist in small
(elite camps and guards, group scaling, loot rolls, guilds, sim-led runs): raids and world bosses scale them up.

**Level cap (Chris, 2026-10-07): 30 in the end, 15 now; "difficult and slow leveling, grind it out".** Round 29 (after round 28):
the cap goes from 13 to 15 and the zones' bands stretch to Oakhaven 1-5, Khaven 5-9, Peaks 9-12, Ash Rim 12-15, Verdant Shore 13-15
(two zones at the cap, "so endgame has more to do"; the Shore "all epic mobs, group required": every mob there elite-strength, a zone flag; the Rim "more solo friendly"); every camp, quest and loot list moves with its zone, Crowsfoot Hollow to about
4-6 and the Sealed Adit to 10-12 (DUNGEON_DESIGN.md); the XP curve steepens (about twice the XP a level, growing faster at the top);
the sim roster's home bands, gear tiers, the wardrobe captures and the tests follow. **Breadcrumbs (Chris, 2026-10-07): each zone's last main quest already turns in to someone in the next zone (QUEST_DESIGN.md: Oakhaven to Khaven to the Peaks to the Rim to the Shore); round 29 sets each of those at the top of its new band, checks the chain reaches it, and makes sure the next zone's first quest offers itself on arrival, so the trail leads through the bands in order.** Levels 16-30 come with the Phase 9 zones, each
new zone carrying its own band, so the cap rises as the world does.

**Next (Chris, 2026-10-08, after his first look at the Sealed Adit): crowd control, with two CC classes: the Rogue, then the Archivist (Docs/CC_DESIGN.md).** "Will be hard to do since we don't have any CC classes yet, so that is something we need to look at next." Give the classes real CC (a Mage's sheep-style hex, a Druid's root/sleep, a Ranger's trap, a Paladin's stun; a Rogue with sap comes with Phase 9), marked targets the party sims respect, and mobs that break out on damage. Then: the Adit deeper (more rooms, longer branches), ghostly Hollow Men (translucent, pale), mobs drawing their weapons as soon as they answer a call.

**After round 29 (Chris, 2026-10-08): Daniel Gruginski's Medieval Village Kit (CC0, hand-painted, ~/Downloads/MedievalVillageKit_FBX.zip).**
Use the FBX zip, not the Unity package (it needs URP). One vertex-colour shader (its painted shading is in the vertex colours),
materials from its Data/materials.json, then a capture of a dozen pieces for Chris before use. Its underground set (mine posts and carts,
crystal veins, lava tiles, dwarf halls, dungeon walls, sewers, cages, altars, cave rock) dresses the Sealed Adit and the later dungeons.
The NatureManufacture castle pack was looked at and passed over (near-realistic, clashes with the stylised look).

## Queued around Phase 5
- The Ash-Walker enclave's interior (playtest note 40).
- Art rounds from Chris's assets (Fantasy Props MegaKit, Medieval props and the Stylized Nature MegaKit's trees in, 2026-10-07): Medieval Village MegaKit, Stylized Megapack
  2in1, Medieval props, HQ Rock Pack, the Ashen Marches sentinel helm.
- Sound: ambience, weapon hits (slash, pierce, blunt each different), spells, mobs; music later.
- Standing: playtest notes as they come (Docs/PLAYTEST_NOTES.md), frame rate, cave lighting (note 10).

## Validation policy
Every round: compile offline, the tests the change touches (all of them once 5.0 lands for core changes), a player build and
the tours the change can be seen in, then publish, commit and back up (CLAUDE.md). Chris plays the published build; his notes
open the next round.

## Ideas for later (Chris)
- **Seasons (Chris, 2026-10-07, playtest note 85):** a year turning through the days. Summer: stamina and water
  drain faster (a water supply to keep up). Winter: snow on the ground in the high and northern places; the cold takes health and
  stops it coming back until you are warm (a fire, an inn, warm clothes). Builds on the crops' round (CropField), WorldWeather and
  WorldClock.

**Asked (the question prompt, 2026-10-16 ~14:55, verbatim): "Lowtowns + Hallow's Creek (Recommended)".** The cap is 20; the Lowtowns & Rust
Market zone is stretched to 15-20 (ZONE_LOWTOWNS.md) and Hallow's Creek to 15-20 (DUNGEON2_DESIGN.md), its door at Oakhaven's Road's End.
