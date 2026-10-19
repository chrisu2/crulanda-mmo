# Crulanda: Claude handoff (Start here rewritten 2026-10-18 ~14:40: the stopping point after the Lowtowns/Hallow's Creek rework, waves 1-2)

## Start here

**Update 2026-10-18 ~21:50: new art and fixes, tested headless, pictures waiting (Chris is playing another game: "please test headless").**
Branch preview-1018c (worktree D:\code\mmo-preview-1018c) = feet-bow-adit-kit + bestiary + megakit-dress + fix-lowtown-trades +
inn-seats + angular-gear. Headless round chainR228: EditMode 104/104, PlayMode 23/23. NOT yet built and looked at as a whole:
the picture round D:\crulanda-work\chainR229.ps1 is written (wardrobe, oakhaven/khaven/lowtowns/crown/adit frames, both bestiary
tours) and waits for Chris to say he's done. Then check the frames, merge the branches into feet-bow-adit-kit, publish.
- bestiary (play notes 233): Quaternius's Imp = the Ember-bound (the Adit's Ember Vent, 2 chained pairs, Chain-Lash), the Puglin =
  Bank-snouts (4 packs on the Lowtowns' river banks, kept 4 m off the water by the camp flag "dry"). Docs/BESTIARY_MONSTERS.md.
  Adding camps shifted the rares' indices: a save that killed the Quiet Miner or the Lantern-Eater sees them alive again.
- megakit-dress (234-235): 64 of the Fantasy kit's 94 props placed (dungeons, inns, the Crown, markets); 37 food models kept from the
  food pack (only what the books support), FoodModels.Dishes for every food and buff item. Chris liked the Adit scriptorium drift (238).
- fix-lowtown-trades (236): the capture's line-ups out of the river; villagers' wander spots never in water; mould not on the water.
- inn-seats (237): sitters pinned on the seat's middle (the crowd pushed them 0.3-0.6 m off); seat heights per seat; one per seat;
  window frames and a hearth at the Golden Cask and the Cracked Hearth. Open: the two share one floor plan (every zone unique).
- angular-gear (239, 241): helms built from the head's own shape (no domes, CHOSAN model helms retired), gloves the hand's own skin
  with a cuff, angular pauldrons. Chris turned down GanzSe's bodies ("i dont like the new characters"/"the old ones were better");
  branch p85-ganzse kept, not merged.
- LootRollTests fixed on trunk (legendaries outside the cap, as Loot.Roll counts).

**Update 2026-10-18 ~18:05: two more publishes (17:49 and 18:00; save backups 20261018-1749- and -1800-before-update-publish).**
- 17:49: the group run holds the party's level (no experience during a run), the dummy is inert, pets keep clear, the vaults' capture
  camera inside the hall. 18:00: the drowned vaults read as built vaults (unbroken rubble-stone rib arches on footings, mould pads on
  the stones, no stalagmites, ledges or dripstone in a cave of style "vaults"; every other cave unchanged). Frame
  world-captures/lowtowns-19/lowtowns-19-the-drowned-vaults.png; the mould glows a little bright (tune if Chris says so).
- Tests: the confirming round 192/193 EditMode, 79/80 PlayMode (only the level-15 Scoundrel on the dummy, 3.0 s not striking:
  not shoves, holds, Nerve or the pet; still open). The vaults round 11/11 + 6/6. Lowtowns ~195-201 fps at noon.
- Run hallows-12 (level held at 17): 26/26 camps, 32 min, 1 death (bot) + 1 (Warrior); Warrior 47/s, Rogue 62, Ranger 53 + wolf 16,
  Druid heal 45; the bot Paladin 84.7/s. Chris asked "is the mason's trowel op?": no (52 weapon damage at level 17, on the curve
  44 at 14 to 57 at 19). The bot starts with its level-1 blade and jumps when it loots a weapon (8:17 here, 11:09 in hallows-11).
  Open: a dummy test of a level-17 Paladin with the Trowel against a level-17 Rogue with the Picker-Master's Hook, to settle whether
  the Paladin with a good weapon is too strong (a hybrid should be ~90% of pure) or the sims too soft (Rogue 62 vs a 66-72 band).

**Update 2026-10-18 ~16:50: the rework finished (Chris: "finish up" -> "Finish the rework").** Published 16:44 (save backup
20261018-1643-before-rework-finish). Every pick of REWORK_LOWTOWNS_HALLOWS.md is now BUILT:
- Hallow's Creek 4 (a valley town under the gatehouse: Main Street's descent to a -8 floor, the Granary's shelf -3.5, "The town below"
  look), 5 (two roads: the high road over the slabs; the low road up the chasm wall by a railed ledge stair from the Mason's slab, and a
  chasm stair under the gardens into the Cellars), 9 (four Glitches that open ways for the visit: the stone handcart's lane, the hung
  slates' ramp to the Archive's north gallery, the ford's dark cellar door, the well-boy's key to the trapdoor), 11 (the Granary, Cellars
  and Archive open together; the Picker-Master and First-Cut each thin the tower's guard; chat counts the wings left).
- The Lowtowns 9 (the Sweep: the horns, hired warders on Pell Row, Lissa led over the roofs to Brother Aldo; quest side.aldo.sweep with
  "Lissa's chalk"; a Seeker glimpsed, never fought) and 11 (the drowned vaults: a stair behind the Blind Seer's, the vaulted hall with a
  black canal, the fence Ottie Brack's skiff, a crawl under the river to the Drain Courts and a culvert from the bank; Iron Teeth guards;
  the carvings' secret and lore page).
- Tests: the last rounds green apart from SimDummyTests' Scoundrel at level 15 (3 s not striking on the dummy; the Rogue passes since
  the dummy was made inert). Run hallows-11 (valley, Glitches, wings): 26/26, no deaths; Warrior 49/s, Rogue 66, Ranger 55 + wolf 17,
  Druid heal 46, but the bot Paladin 74.7/s, above the Rogue (a hybrid should be under): look at that next.
- Open: the vaults' landmark capture view is on the street above (move its camera spot); the Scoundrel dummy gap; the Paladin bot's
  damage; GitHub push (Chris's permission); ambient sound (ask).


**Stopping point: 2026-10-18 ~14:40.** Chris: "you have 4 hours to finish and then save backup and write a handoff." Everything is committed on
`feet-bow-adit-kit` in D:\code\mmo and backed up to E:. **Not pushed to GitHub** (the auto-mode check refuses `git push`; ask Chris to allow
it or push himself: `git -C D:\code\mmo push origin feet-bow-adit-kit`). No worktrees left (every helper branch merged and deleted). Nothing VERIFIED until Chris
sees it.

### Chris's verdict and the rework (Docs/REWORK_LOWTOWNS_HALLOWS.md, all of it)
- "im not sure i like the lowlands or the harrow.seems boring just my opinion"; why: all four of "Looks flat and dull", "Nothing to do",
  "Fights feel samey", "Layout is a straight line".
- The first plan was written without the books; Chris: "please consult the lore canon and redo / edit these choices based on my
  descriptions in the novels" and "it should always be referenced on all design documents" (now a GAME_BRIEF standing rule and memory).
  The redone plan cites Book 1 throughout (Hallow's Creek is canon: b1 ch.17-23; white houses, quartz roses, a valley, a glass-crater
  town hall, no birds or wind; the Lowtowns are vertical, neon moss, yellow haze, the black Chemical River).
- Decisions: "keep all" (Lowtowns 1-12, Hallow's Creek 1-12); the Copper-Tithe out; fifty years; the stone girl by the willow is **Talla**;
  **Snivels** in; "i never said not use the name characters. it would be nice to see some of them come alive" ... "like the leader in
  crowns" (canon characters welcome: GAME_BRIEF standing rule). Unanswered conflicts take the recommended answers (doc, top).

### Published
- **2026-10-18 01:36:** the re-buff after a wipe, the pets' clear spot and closer stop, the Lowtowns' soot smoke (save backup
  20261018-0136-before-oct18-update).
- **2026-10-18 03:23, wave 1** (save backup 20261018-0323-before-wave1), tests 110/110 + 37/37:
  - Hallow's Creek 1, 2, 3, 12: white and perfect (lime-washed houses, white picket fences, red-quartz roses; only the people grey,
    blue eyes), the silence (no wind, ash stopped, no music in town, "Help us" near the paused), the Square as ch.21 (the obsidian bowl
    16 m deep, a still mirror floor, the red door on its floating slab with the sewn-lipped knocker, drifting debris, chalk equations, the
    willow, the town well, the chapel's hanging glass), the people (Gable's Glitch and pie: "Take it. Break the loop.", Martha's voice,
    Mrs Miller, the children's ball, the dog mid-leap, **Talla** by the hollow under the willow).
  - The Lowtowns 1, 2, 4, 5, 6: built downhill (the Rust Market +8 at the Forge wall, the Lower Market and Burners' Yard +4, the High
    Rows +6 over the sunk Drain Courts -3, the river and docks at 0; 13 stairs/ramps incl. the twisting causeway; dressed retaining
    walls), the books' colours (yellow haze, orange/violet windows, sickly yellow lamps, neon moss, green drain mould, the Iron Teeth's
    ochre mark), the Chemical River (black, poisons waders, hides you from magical tracking; sludge-eels; the Old Croc rare), the
    Weave-Eaters as ch.20 (blink, drawn to casters, eat wards; harpoons pin; salt calcifies; Lowtowns only), the Spoon as ch.1 (Snivels
    Gear-Grinder, Triad, the Ballad of the One-Legged Goat, the brawl out through the front window, Gorm's line about the boy he dug out).
  - Frames: world-captures/hallows-10, lowtowns-11/-12 (river fixed from beige to black). Noon: Lowtowns ~215 fps, Hallows ~107.
- **2026-10-18 13:15, wave 2** (save backup 20261018-1315-before-update-publish): Hallow's Creek 6, 7, 8, 10 (the Hollow as ch.4: cellar
  trapdoors, the glide, choking dust, crawling halves, the anchor-well; the Ash Road's time-squall, cutting stone leaves, green fissures,
  soft wrong-way shadows; the bridge's tilting/spinning slabs and the fifth slab that dips and tips behind you; new fight tricks: the
  cutters' clock, Time-Lost echoes, crossbows on the walls, the Pact shield-wall, "Struck from the record"); the Lowtowns 3, 7, 8, 10, 12
  (the roofs over Pell Row: rain-spout and fire escape up, lead walks, rope bridges, drops through the washing, the water tower; the
  warders' shield-line, slate-throwers, circling hounds, the dock-rats' hook, the Grey-touched's dazzle; Dead Man's Drop with the fungal
  net; the Night Market: the Merchant of Nothing, the candlemaker's light-butterflies, a card-reader's chalk ring; the Stack, the bridge of
  houses, the Iron Teeth's corner by the day, the punt ferry, the weir). Also: the Bell Tower stall fixed (sims and mobs re-path at most
  once a second; MobChase), the squall bites through out-of-fight regen. Run hallows-9: 26/26, no deaths.
- **2026-10-18 14:34** (save backup 20261018-1434-before-update-publish), from Chris's notes while watching:
  - "warrior doing way too low damage ... maybe 3/4 of dps": the tank sim at 75% of a damage-dealer (run: Warrior 48.7/s, Rogue 66.2,
    Paladin 63.1, Ranger 54 + wolf 17.3, Druid 13 + heal 57.8); mobs gain a tank health share (1.8).
  - "group is not rolling on loot..if upgrade they need to get it" then "dont roll on common gear. i just didnt see people win rolls":
    rolls for uncommon and better only; sims Need by SimGear.IsUpgrade and wear what they win (run: the Druid, Rogue and Warrior won pieces).
  - "falling off ramps and bad pathing": rails on every open deck/ramp/stair edge in Hallow's Creek (timber or stone, just outside the
    edge, gaps where ways join) and the Lowtowns' raised walks (the weir also fixed: its piers blocked the plank); agents' radius capped.
  - "for buffs need icons or something bc this is messy": buff icons with countdowns, borders and tooltips replace the text tags; your
    own row moved next to the target frame.
  - Run hallows-10r: 27/27, no deaths, 5 stuck warps. Noon: Lowtowns 190 fps, Hallow's Creek 111.

### Next, in order
1. Chris plays wave 2 and the 14:34 fixes; reports stay open until he confirms.
2. The rest of the rework: Hallow's Creek 4 (a valley seen from the lip), 5 (two roads in), 9 (Glitches that change the route), 11 (choose the
   wing first); the Lowtowns 9 (the Sweep) and 11 (the drowned vaults). See REWORK_LOWTOWNS_HALLOWS.md (picks marked BUILT).
3. Sim Rogue and Scoundrel land ~61/s against 64-68 at level 15 only (SimDummyTests; the diagnostic line added to the report did not print:
   check its output path). Small; other levels pass.
4. GitHub push (Chris's permission).
5. Ambient sound/footsteps don't exist (the books' silence is the music stopping): ask Chris whether to add.

### Known limits
- No ambient sound or footsteps exist in the game (only music): the books' silence is the music stopping. Needs sound files and a
  footstep system (asked Chris whether to add it; no answer yet).
- The sim Scoundrel lands ~61/s against 64-68 at level 15 (SimDummyTests); the pet wasn't the cause.
- The pale "spikes" at the very top of the Rust Market's high capture view are gaps between mountain peaks cut by the frame's top edge
  (not a bug; the skyline is right side up from where a player stands).

### Lessons (memory holds them)
- Hit Chris's stop time; before any stop end every background task (memory stop-on-time-no-orphans). Missed on Oct 17 (shut down at
  01:00 instead of 22:00; a cancelled chain's watcher ran 7 h 20 m).
- Read the books before any design doc (memory reference-the-books).
- Helpers in their own worktrees, merged and removed the same hour (memory clean-up-worktrees).

---

## Oct 17 (superseded)

**Stopping point: 2026-10-17 ~21:00.** Chris: "no run for another 2 hours and then create a handoff and shutdown the computer". Everything
is committed on `feet-bow-adit-kit` in D:\code\mmo, backed up to E: (tools\Backup.ps1), and the PC shut down at his word. **Not pushed
to GitHub**: the auto-mode check refused `git push` both times (Oct 16 night and today); ask Chris to allow it or push himself
(`git -C D:\code\mmo push origin feet-bow-adit-kit`). All the D:\code\mmo-* worktrees were removed today (Chris: "you delete what is no
longer needed"); every branch is still in git (p85-hero unmerged, nothing built on it). Nothing is VERIFIED until Chris sees it.

### The Oct 17 update (published 2026-10-17 20:47)
Built from feet-bow-adit-kit; Chris's saves backed up first (save-backups/20261017-2047-before-oct17-update). On top of the Oct 16 publish (castle, level 20, Lowtowns,
Hallow's Creek, checkpoints, balance answers):
- **Lowtowns sky:** the "upside-down roofs" at the top of the Rust Market's view were the Forge District's backdrop stacks and blocks
  (to ~70 m) with their feet in the smog; lowered (SkyGhostDiagnostics found it by toggling: the ghost went with the fog and the
  backdrop). A few pale downward spikes remain at the very top of that one view (not identified: they show with fog off too).
- **Sims' damage:** the Rogue/Ranger/healer stood idle at the end of most packs (a moon-held mob was "leave it alone" all fight); now the
  party fights the last held mob; damage sims' hits set from the band (SimCompanion.Damage.cs); rooted sims keep attacking. Measured in
  the run: Rogue 61/s, Ranger 52 + wolf 12, Paladin bot 54 (hybrid 90%), Druid heals 51/s.
- **Run bot:** dead mobs' capsules no longer block the player (EncounterEnemy: real for Chris too); the bot hops kerbs, side-steps, paths
  from the navmesh; **the chasm floor was "out of the world"** (Hollow.Lowest 5 m under the land: dropping to the Fallen Mason sent you
  back to the yard; now ZoneBuilder.LowestFloor) — real for Chris too. Stuck warps 20 → 5.
- **Checkpoints** reached within 12 m (was 4): all three Hallow's Creek lamps reached on the way; after a death the party woke at the
  furthest lamp.
- **/oakhaven** chat command (home from anywhere out of a fight).
- **Auto attack:** E (G on ESDF) with a foe targeted and nothing else to use starts your swing, every class; the Shaman swings its staff
  (Rooting Stamp starts it), with its own swing scale (Stonebound spells 2.45 → 1.81 so spells + swings stay in the tank band).
  Cleric and Archivist still have no swings (no melee path).
- **World map:** labels on dark plates with state pips, auto-placed without overlaps, faint roads, the Lowtowns at Argentis' foot,
  Hallow's Creek east of Oakhaven, designers' map notes hidden, the zone list scrolls.
- **Oakhaven:** the tannery yard, woodyard and bake oven off the fields (WorkshopDataTests: no workshop on a field).
- **The Crown:** trainers and fixed posts face the open room (PostFacingPlayTests); the Hall of Arms dressed (class banners, runner,
  benches, candle-stands); the Gilded Lantern's porch columns out 2.2 m; every inn's door queues fitted to open floor (Oakhaven's and
  Khaven's were against the porch posts); **lag:** townsfolk past 30 m cast no shadows and past 95 m are not drawn; ivy/leaves/garden
  detail in 24 m blocks hidden past 100 m (DetailCull). Noon on the capture rig: 64 → 130 fps (at a 70 m reach; 100 m set after,
  121 fps measured with the 100 m reach).
- **The Bureau Examiner** (Hallow's Creek's last boss) wiped the party 3 times: his 3 s "hour" stun every 18 s reached 8 m on a 7.8 m
  floor, so nobody could get clear; now 5 m and the healer/archer sims step clear at the warning (SimCompanion.StepClearOfHold).
  Confirmed: the level-17 run (run-captures/hallows-7, 20:44) beat him on the first pull, the Druid "stepping clear of the hour".

### Tested
- The last full round (chainR200): EditMode 87/87, PlayMode 77/78; then SimDummyTests + SimPartyTests 18/19 after the Mage pet fix.
  The one red: **a sim Scoundrel at level 15 lands 60.7/s against 64.3-68.2** (the others and other levels pass). Suspect (unconfirmed):
  the stuck-pet fix (RangerPet, e8fec5a5) sets the Mage's thrall 1.4 m from the dummy beside the Scoundrel and they shove; fix = pick
  the pet's spot away from party members (about 1.1 m clear). Not done.
- The level-17 Hallow's Creek run (run-captures/hallows-7): 27/27 camps in 35 min, one death (Sergeant Tredway, woke at the lip lamp),
  8 stuck warps; meter Rogue 69.6/s, Paladin bot 61.1, Ranger 57.7 + wolf 17.1, Warrior 18.7, Druid heal 50.8. The Examiner first pull.
- Frames: world-captures/crown-8 (Hall of Arms, the inn porch), lowtowns-7, ui-captures/r197 (the new world map); skyghost/ (the
  diagnostic's toggles). Noon: Crown 121 fps, Lowtowns 164-230.

### Next, in order
1. Chris plays the update; reports stay open until he confirms (the Crown's lag and posts, the inn doors, the map, the Shaman's swing
   with E, /oakhaven, the Lowtowns sky, Hallow's Creek's final boss).
2. The sim Scoundrel's 5.6% shortfall at level 15 (above).
3. After a wipe the party does not buff up again before the next pull (BossBuffs: once per boss); make it re-buff after a wipe.
4. The pale downward spikes at the top of the Rust Market's high view (SkyGhostDiagnostics: run it again, toggle at finer grain).
5. GitHub push (Chris's permission).
6. From before: retune bosses for buffed parties (Nix with one hold); Phase 8.5 on p85-hero (the Modular Hero body); the Lowtowns'
   generator (tools/wip/lowtowns/make_lowtowns.py) is out of date with lowtowns.json: never run it over the file.

### Lessons from today (memory holds them)
- Don't run Unity jobs while Chris plays: they made his game lag. Chains now wait until the game and Unity are closed (Busy()).
- Lanes get only committed work while helpers edit: D:\crulanda-work\syncCommitted.ps1 (mirror, then the committed copy of every open
  file, and files not yet added removed). A half-done helper file broke a run before this.
- Clean up worktrees as soon as they are merged (memory: clean-up-worktrees).
- Measure, don't guess: SkyGhostDiagnostics (render the view, toggle groups) found the sky ghost after three wrong guesses.
- A wipe: read the log from the pull (the Examiner's stun reach vs his floor), don't just turn numbers.

### Chris's words today (verbatim; the docs hold them)
- "keep working and get it right"; "when i play the new zone. iasked this last night and you said 2-3 hours. did it complete?";
  "how do i load the game"; "you have left lots of folders in my d:\code\ locations with lots off mmo* variants" / "you delete what is
  no longer needed."; "is there a command to get to oakhaven?"; "map is not clear"; "shaman still had no auto attack."; "tools in the
  farmers field"; "still people standing backwards at /crown also still laggy"; "people stuck getting into inn"; "whats been running an
  hour"; "long run back ..are you not using checkpoints"; "how much longer?"; "no run for another 2 hours and then create a handoff and
  shutdown the computer".

---

## Oct 16 night (superseded)

**Stopping point: 2026-10-17 ~00:10.** Chris: "once all these jobs are complete. backup, save and then complete the handoff for next
time". Everything is committed and pushed (every branch), backed up to E: (tools\Backup.ps1). Nothing is VERIFIED until Chris sees it
in his build.

### What is in the normal game now (published 2026-10-16 23:54)
`D:\crulanda-work\outputs\Crulanda-Playable`, from `p9-lowtowns` (fast-forwarded into `feet-bow-adit-kit`); Chris's saves backed up
first (save-backups/20261016-2354-before-castle-lowtowns-publish). It carries everything since the 01:23 publish:
- **Phase 8.6** (inns, crafting, buffs, lockouts, lore), already published at 15:27 on Oct 16 (character save format 11).
- **The castle, the Crown of Argentis** (CAPITAL_HUB.md): the halls, the library, Aethelgard on his throne, the guild halls, the two-part
  bank (3 shared slots, more bought; the rest per character), the Exchange with sims listing and buying, the Crafters' Court with every
  trade, the Gilded Lantern inn (its own design), ivy, gardens, iron; profile and world saves at format 2. `/crown` jumps there.
- **Level 20**: 47 talent nodes, gear band 16-20 (slag-iron, tier 6).
- **The Lowtowns** (zone.lowtowns, 15-20; ZONE_LOWTOWNS.md): Oakhaven's south-east road; the Rust Market (14 stalls, market folk),
  street clutter, the city roofline on the horizon, the Crown lower on the NE skyline (it hung "upside down" at the top of the view
  before), the new **Rusty Spoon** (inn variant 4: soot-brick warehouse, sagging timber upper floor, sunken bar, dice pit, the curtained
  back room; Chris: "each zone has to be unique not reused assets"). `/lowtowns` (sets you to 17).
- **Hallow's Creek** (zone.hallows, dungeon 15-20; DUNGEON2_DESIGN.md): 27 camps on the standard run, the frozen town (townsfolk held
  mid-step, ash grey), the glass-slab bridge with parapets on every edge, the stair down to the Fallen Mason, the Old Archive (broken
  roof, ledger shelves, papers in the air), the Bell Tower with its bell, the crater square, the Warden's walk escort, three still-lamp
  checkpoints. `/hallows` (sets you to 16).
- **Checkpoints in every dungeon** (Chris: "maybe 3 checkpoints in dungeons"): the Adit's three miners' cairns, Hallow's Creek's three
  still lamps; Recover wakes you and the fallen party at the furthest reached.
- **Balance** (BALANCE_REPORT.md, Chris's answers): hybrids' damage builds 10% under the pure damage classes ("A bit lower"; the
  Warrior counts as a hybrid: "Hybrid"); sims count their gear's Stamina ("Fix it now"), and mobs hit a sim party harder to match;
  the Warrior sim is the toughest in a party; the group-run bot spends its talents.

### Tested
- Final round (chainR195): EditMode 120/120 and PlayMode 53/53, all green, including the Adit camp-reach check (now by hops, ZoneContentTests) that had failed for days; the release build 0 shader errors.
- The level-17 Hallow's Creek group run (run-captures/hallows-4, 23:10): all 27 camps, no wipes, 43 min; the Druid healed 41/s (it was
  16/s at 19: the run at 19 was too easy, Chris: "still not balanced i dont think").
- Frames: world-captures/lowtowns-5 (the Spoon, the skyline), hallows-3 and -4 (bridge, square, archive, tower), lowtowns-3/-4.
  Frame rate at noon: Lowtowns ~154 fps, Hallow's Creek ~116 fps.

### Next, in order
1. **The Rust Market's high view: upside-down roofs along the top of the frame** (world-captures/lowtowns-5/lowtowns-12-the-rust-market.png,
   also faintly the soup hall's). NOT the Crown on the skyline (lowered to summit 95 and coloured whole, 950ad7bc: no change), NOT the
   DistantRanges city ring (built upright) and not the backdrop rows (the perf file's HIGH list shows nothing else 25 m up). It looks like a
   mirror of the city about a plane over the camera: suspect a reflection (the Water shader's sky/probe sample, the realtime "Sky
   reflection" probe in ZoneBuilder.cs:145, the puddles) or the RoofFade dither. Reproduce at that view and toggle renderers.
2. **The sims under-hit their damage band.** In the level-17 run the Paladin bot (talented, x0.64) did 64/s, inside its band (59-65),
   but the sim Rogue did 42/s and the Ranger 37/s (+14 the wolf); pure damage should be 66-72. Raise the sims' damage to their band
   (SimCompanion's hit formula, their rotation, the Rogue standing in boss stuns, holding the marked mob), then a group run at 17 for
   Chris to watch. Chris's eye is on the meter ("still not balanced i dont think").
3. **The run bot got stuck 20 times** in the level-17 run (warped to the leader/fight; the bridge was the worst before the parapets, but
   it still happens on Main Street, the Granary, the Archive). It is the test player's steering (RunCapture), not the sims; read the
   log's positions (run-captures/hallows-4/run-player.log).
4. Chris looks at the Lowtowns, the Rusty Spoon, Hallow's Creek (queue it with J or /queue at 15+, or /hallows), the checkpoints, the
   castle. Reports stay open until he confirms.
5. From before: retune bosses for buffed parties (Nix with one hold); Phase 8.5 on p85-hero (the Modular Hero body); the p86-inns and
   p86-lore leftovers if any; the Lowtowns' generator (tools/wip/lowtowns/make_lowtowns.py) is out of date with lowtowns.json: never
   run it over the file.
6. Later dungeons need not be caves (Chris: "all dungeons don't have to be cave systems"); every zone's buildings its own.

### Lessons from today (memory holds them)
- A chain that waits with `Start-Process -Wait` hangs when Unity starts Unity Hub (2 h lost); chains now wait on Unity's own pid, and
  every chain has a stall watcher (scratchpad stallwatch.sh: 20 min with no new line and no Unity or game running, or 90 min on a step).
  Never say a job is running without looking at its output and the process.
- Cloned chains: fix the wait line (chainRNNN.out) every time.
- Check the frames yourself before telling Chris a zone is done: the first Hallow's Creek frames showed T-posed townsfolk, a roofless
  archive and cameras in walls.

### Chris's words today (verbatim; the docs hold them)
- "after this work on next dungeon and raise top level to 20. make sure there is a zone for 15-20 and a dungeon for the same range";
  "no thanks i dont want a half assed version. complete it first".
- "keeps falling off the edge over and over"; "people still falling off the crazy stone stair area"; "still not balanced i dont think".
- "just fyi all dungeons don't have to be cave systems. just a note" (DUNGEON_DESIGN.md section 0).
- "one thing to help respawns is to have checkpoints in the dungeon so they don't have to run all the back from the original spawn
  area. maybe 3 checkpoints in dungeons" (DUNGEON_DESIGN.md section 0).
- "same damn inn in the lowlands as well. each zone has to be unique not reused assets" (WORLD_ZONES.md).
- Balance answers (BALANCE_REPORT.md): hybrids "A bit lower"; sims' gear Stamina "Fix it now"; the Warrior "Hybrid".
- "ugh always making mistakes and wasting time"; "you have wasted most of the day with the run on jobs" (the stalled chains).

---

## Earlier (Oct 16 and before)

### The night of Oct 15-16

**Stopping point: 2026-10-16 ~01:45.** Chris: "once complete find a stopping point. backup, publish all to git, and create a handoff for
next time documenting everything done and what needs to be done." then "ok shutdown the computer when done". Everything below is pushed
to GitHub (every branch), backed up to E:, and the PC shut down at his word. Nothing is VERIFIED until Chris sees it in his build.

### Published
- **The normal game** (`D:\crulanda-work\outputs\Crulanda-Playable`, 01:23, from `feet-bow-adit-kit` before the three merges below;
  save backed up to `save-backups/20261016-0123-before-ccpack-publish`): the Adit run fixes (the bot counts height to the leader, out of
  water at any distance, a sim with no path to you catches up, Lusk's Moon and his camp, a boss camp open while he lives), Chris's CC rule
  (holds on elites half as long, each kind once a fight; Nix held once), every mob's signature move and the sims' holds (cc-pack), birds
  that walk and hop now and then, the talent window's role tags. Tests 41/41 and a Nix win at 01:16. No full group run since adit-final11
  (cut for the stopping point).
- **The castle preview** (`D:\crulanda-work\outputs\Crulanda-Castle-Preview\Play Castle Preview.bat`, 01:27, from `p9-capital`): plays a
  copy of Chris's saves in `D:\crulanda-work\castle-preview-save` (`--crulanda-save-dir`), because p9-capital moves the profile and world
  saves to format 2. `/crown` jumps there. Frames: `D:\crulanda-work\world-captures\crown-2` (50).

### Branches (all pushed; worktrees under D:\code\mmo-*)
| Branch | What | State |
|---|---|---|
| `feet-bow-adit-kit` (D:\code\mmo) | the working branch; tonight merged: cc-pack, p86-craft, p86-buffs, p86-lockout | the three p86 merges are compile-checked only, not yet run together in Unity; they bring **character save format 11** (recipes, lockouts): back up Chris's saves before the next publish |
| `p86-craft` | buffs, 37 found leaves, Leatherworking, Weaving, Simpling, every tier's nine slots | merged; 131/132 + 11/11 (the one red, set-piece icons `loot.set.t1_*`, is older than tonight: run tools/art/make_icons.py) |
| `p86-buffs` | buff up before bosses (stop, eat, elixir, a class buff for every class) | merged; 26/27 (the same icons) + 30/30 |
| `p86-lockout` | lockouts (dungeons 2 days, open-world elites a day, open-world bosses and raids a week, at 06:00) and the dungeon queue (J, /queue) | merged; 27/27 + 4/4 |
| `p86-inns` | double doors, bigger inns, doorway queues, taprooms everywhere, step aside, rain shelter; table dressing on the table's top | NOT merged: 23/24 (a moved prop by a grove), 24/26 (two villagers couldn't path into the Cask; the Adit camp reach is the old search-budget red) |
| `p86-lore` | the Lore tab's seven chapters, 91 secrets with clues, five new kinds, regrowing herbs | NOT merged: 31/31, 12/13 (a map's "?" mark stands on secret.oakhaven.elins-cairn) |
| `p9-capital` | the castle: zone (p9-crown), halls (p9-halls), bank and Exchange (p9-exchange), inn and townsfolk (p9-cityinn), throne and floors (p9-throne), ivy/iron/gardens (p9-crowndetail), the two-part bank, bookcases, facing, doorways | NOT merged: 75/79 and 12/16 (below) |
| `p9-lowtowns` | the cap 15 to 19 (one commit); tools/wip/talents19.py (the talent plan, not run) | waits on the castle |
| `p85-hero` | the Modular Hero pack's base body, poses and presets (route a) | nothing built on it yet |

### The castle's open reds (p9-capital, 2026-10-16 00:45-00:57)
1. `Hall_roles_are_read_from_the_residents_role` expects "The Lantern Watch" (a guildmaster role parse after the merges).
2. `The_Council_wardens_stand_in_pairs`: Warden Maeve Corbin has no partner (an odd warden after the townsfolk).
3. The Gilded Lantern's city layout: seats 0.25 m from their tables (the test wants more room).
4. `secret.crown.clerks-purse`'s clue names "The Spire" (a landmark): reword.
5. Maddy Brook (water-carrier) can't be walked to from the Gilt Approach.
6. The detail's cost check ("The detail stays cheap"): measure the ivy and garden triangles, trim.
7. The Adit camp-reach red (old, the search budget; trust the hop map).
Also look in the frames: Aethelgard did not read as seated and robed in crown-2/crown-07-the-audience-hall; the Crafters' Court is still bare.

### Chris's words tonight (verbatim; the docs hold them in full)
- "keep going finish phase 8" / "them move to 9"; the question prompts: Phase 8.6 on my defaults, the gear fit on the hero pack's body
  (PHASE8_6_LIVING_WORLD.md, ROADMAP), the Lowtowns first in Phase 9 (ZONE_LOWTOWNS.md: Oakhaven's south road, Gorm himself, the
  Council's warders, the dead-house cellar), Hallow's Creek on the recommended answers (DUNGEON2_DESIGN.md).
- The castle (CAPITAL_HUB.md, all of it): "we need to implement a castle (central point) ..."; the Crown of Argentis, open from level 1,
  Aethelgard, castle first; "use the map for reference design"; "make sure all professions are located in the castle"; the bank shared,
  then "need a limited shared space. maybe 3 slots ... the remainder is character only"; the Exchange with sims; "i really want the castle
  done asap so double down on it"; the inn redesigned; "lots more npcs in the castle with real jobs"; a castle on the northern skyline;
  more detail (ivy, iron); "we need a real garden"; the fixes (the floor, the trainers, the mugs, the throne, the bookshelves); "need more
  profession tools/small huts/vendored items" (NOT done: first job next time); "otherwise looks like a good start".
- CC (CC_DESIGN.md): "you can't CC elites , bosses, world bosses , etc." then "normal CCs are cut in half the duration and can't imply the
  same CC after the first" and, of Nix, "CC duration cut in half and can only have one cc type one time".
- Lockouts (ADIT_REWORK.md, top): bosses/elites don't respawn; "Until a lockout resets", "Everywhere"; "the dungeons should include an
  instance que that can be done every 2 days for loot"; "open world boss is once per week"; "raids once per week".
- "we should use food buffs, alchemy buffs, class buffs,etc to beat the bosses" (built: p86-buffs; the bosses not yet retuned for it).
- "birds still hoppy/jumpy ..." (fixed); "in the talent tree you need to designate their distinction like tank/healer/dps/support" (fixed).

### Next, in order
1. The Crafters' Court: a workshop for every trade (huts or stalls, tools, stock) and vendors of each trade's tools and materials.
2. The castle's reds above; frames of the throne (front, side, back), the inn, the gardens, the skyline from Oakhaven; then merge p9-capital.
3. The p86-inns and p86-lore reds; merge them.
4. Run the merged working branch in Unity (the p86 merges together), back up Chris's saves (format 11), publish, a full group run.
5. Retune the bosses for buffed parties (Nix with one hold); the set-piece icons.
6. Phase 8.5 on p85-hero (retarget, map the looks to parts); Phase 9: the Lowtowns (talents for 19 in tier 3 per talents19.py, then the zone).

## Earlier start here (the evening of Oct 15)

**Night of 2026-10-15 (~22:15): Chris: "keep going finish phase 8" then "them move to 9".** His answers tonight (question prompts,
verbatim in the docs): Phase 8.6 on my defaults (PHASE8_6_LIVING_WORLD.md, end); Phase 8.5 by route (a), the hero pack's body (ROADMAP;
**blocked until Chris downloads "Modular Hero" (CHOSAN) in the Package Manager's My Assets**: it is not on disk); Phase 9's first zone
the Lowtowns & Rust Market 16-19 (ZONE_LOWTOWNS.md: arrival by Oakhaven's south road, Gorm himself, the Council's warders, the
dead-house cellar); Hallow's Creek on the recommended answers (DUNGEON2_DESIGN.md, end).
- **Branches and worktrees:** `feet-bow-adit-kit` (D:\code\mmo, the working branch; the Adit fixes); `cc-pack` (D:\code\mmo-ccpack:
  every mob's signature move and the sims' holds; feet-bow merged in at 74aeaf9d; its tests (chainR157's set) still to run, merge if green);
  three Phase 8.6 helpers' worktrees made at 7691e7db: `p86-inns` (D:\code\mmo-inns: steps 1-3, the inns, doorways, taprooms),
  `p86-craft` (D:\code\mmo-craft: steps 4-6, buffs, found recipes and save format 11, crafting tiers and the new crafts), `p86-lore`
  (D:\code\mmo-lore: steps 7-8, the Lore tab's chapters, secrets to about 90, the regrowing herbs); `p9-lowtowns` (D:\code\mmo-lowtowns:
  the cap 15 to 19, 4d9c20fa; the rest of ZONE_LOWTOWNS.md section 10 to come). Compile scripts: scratchpad `b-<name>/build.py`.
- **Phase 9 step 2 found:** at 19 a character has 20 points and most branches have 16-19 ranks built (only the Druid, the Warrior's
  first three and the Archivist's Still Vow, Unwriting and Cantor reach 20): a tier more for every short branch before the cap ships.
- **The full group run** (chainR161, adit-final9, 22:03) with the bot's height fix and the leader keeping a camp while it waits.

**Current state: 2026-10-15 afternoon. Chris: "i told you to finish adit before shutting down" and a deadline at 18:00.** The Adit
rework is finished in code and merged into `feet-bow-adit-kit` (the working branch, PR #2); `adit-rework` (worktree `D:\code\mmo-adit`)
is the same. Nothing is VERIFIED until Chris sees it; talk to him in game terms (memory: plain-words-no-run-names, finish-the-named-task).
- **Chris's answers (ADIT_REWORK section 1):** respawn 20 minutes; keep the ~75 minute run; build the Cove; add the Foundry's buckets.
  Raids: the Spire at 30 is the first, twenty players. Class sets: "6 pieces. 2/6 give 1 special, 4/6 another ... 6/6 ... 3 special
  abilities and these are class specific"; the Adit's are tier 1 (dungeon), higher tiers from raids (loot DESIGN.md 3.6, 3.8).
- **What landed today (CHANGELOG "2026-10-15, 12:40-18:00"):** the Cove (S6), the polish (S7), eleven class sets, the deck-placement bug
  and the rails it stood up, the far-prop culling kept off cave walls, the dungeon map (`tools/wip/dungeon/adit_map.py`, `/loc`), the
  sim leader's give-up timer and long walk back, the run bot's unsearchable bodies.
- **Speed:** the Adit with the Cove 100 fps at the yard and 125 in the Tally Hall (from 13 and 17 before PropCull and sleeping mobs).
- **Tests:** the Adit play set 27/27 at 16:15 (AditPolish incl. the leader sent from the yard to Nix, AditCove, AditTests, AditCaveBuild, AditNav); the party tests 22/22 at 16:39 (SimPartyTests, GroupRunTests) with damage by role. **Group run:** The standard group run (a Paladin bot, a Warrior tank, a Druid healer, a Rogue, a Ranger) was played three times on Chris's screen: the first stood an hour (the leader gave Nix up after a wipe, the bot at a body it could not search), the second stood in water and fell off the rope bridge (rails opened by R129's CarriesOn rule beside the bridge tower), the third (16:22-16:31, every fix in) cleared 15 of 51 camps in 21 minutes, walked back from the yard after a wipe without stalling, and was stopped at its third death at Nix (Nix and the Rock-Eater stun the tank every 14 s; the party cannot beat him): Nix is a balance item, not a broken dungeon. The bot now takes each path corner (it cut the decks' turns and stepped off their open junctions) and counts swimming as stuck..
- **Evening of Oct 15 (CHANGELOG "18:00-20:00"):** the sim parties wiped at Nix because of three solo-era rules (a mob reset when you fell; getting up pulled the party out and reset every mob; a rider's home stayed at its camp), not the duo's numbers; fixed, plus the revived leave the mobs' threat and the bot assists the tank. The party now beats the duo 3 of 3 (AditNixFightTests). Two helpers' work waits on branch `cc-pack` (worktree `D:\code\mmo-ccpack`): a signature move for every ordinary mob (54 new) and the sims' holds for every class with an area blow; merge only if its tests (chainR157) are green.
- **Open, in order:** (1) **Nix and the Rock-Eater: half fixed (17:21 build).** Four group runs wiped at the duo. Found (logs and a helper's sums): the healer mended whoever had the lowest share of health, never the tank first (now: the tank first under 60%); the beast's every blow sat at the 60% cap (235 on a 392 tank, every 2.6 s, against ~30 healing a second), so easing Rock-Crush did nothing (now its hit is halved, health .9, its Shaken on its own foe only). `AditNixFightTests` (new, PlayMode: the run's party placed at the duo, ~2 min) then killed the beast in 61 s with the tank up, but lost to Nix on foot (.8 of an elite, 7,392 health, the party ~35 damage a second: 140 s, his enrage killed the tank at 204 s). Nix at .5 broke the duo's design range (SocialRulesTests: Nix .7-1, both 1.5-2.2) and the solo-elite rules, so he is back at .8. Tried at 17:25 and reverted: Nix's hit .6 with a later, milder enrage let a lone Warrior and a lone Barkhide Druid beat him (the solo rules: an elite of your level wants Mira), so Nix's own numbers cannot come down; the fix is the sim party (its damage on him, ~35 a second, and the tank's survival: the healer's 31 a second). Next: raise the party's damage on him (the sims' role damage, the bot's kit use) rather than Nix, and make AditNixFightTests reliable (its second run the sims never engaged and you died alone at 15 s: check the pull and the placement); then the group run to the Cove; (2) monsters' and sims' crowd control and area moves (Chris asked: most mobs have none; sims only the Rogue/Archivist hold and interrupts): one signature move a mob type, a control move for the classes without one, an area move for sims at three or more mobs; (3) the Cove's water still shows bright streaks in the tour frame near the camera (probably the tour lamp; check in play); (4) sims wear class sets for looks only; the action bar shows the old cooldowns; (5) the fixture and tour captures for Chris (the Cove, the buckets) and his play-through.
- **Helpers' worktrees** (`D:\code\mmo-cove` adit-cove, `D:\code\mmo-polish` adit-polish, `D:\code\mmo-sets` class-sets) are merged and
  can be removed (`git worktree remove`), their branches deleted, once Chris is happy.
- **Phase 8.6 and Phase 9 wait:** the 14 decisions at the end of PHASE8_6_LIVING_WORLD.md (defaults proposed in chat 2026-10-15 ~13:00:
  8.6 now, looks wait on 8.5, three craft slots with Leatherworking and Weaving, coin-only recipes, 30-minute buffs, bigger inns with
  double doors and taprooms everywhere, step aside and shelter in rain); Phase 9 first zone to be picked by Chris; the orc race playable
  next (Races.cs has the rule, no figure or pick yet).

## Oct 15 small hours (history; Start here as it stood at 04:10)

**Current state: 2026-10-15, about 04:30, the PC shut down at Chris's word** ("once all is done and clear. shutdown the computer").
Chris played the Adit build through the small hours; his words verbatim in PLAYTEST_NOTES 199-225, what was done in CHANGELOG's
2026-10-15 entries. Nothing is VERIFIED until Chris has seen it in his build. Talk to him in game terms, not run names
(memory: plain-words-no-run-names).
- **Branches:** `feet-bow-adit-kit` (PR #2) and `adit-rework` (worktree `D:\code\mmo-adit`, main merged in to the last commit), both
  pushed. Do not merge either to `main` without Chris. **The published build comes from `adit-rework`.**
- **Published:** 04:28 from `adit-rework` with everything below (the save backed up to `save-backups/20261015-0428-before-closing-publish`). Save backups in `D:/crulanda-work/save-backups/20261015-*`.
- **The Adit's lag (notes 218, 225) is the night's big fix:** with the frame cap off, the yard went from 13 to 124 frames a second and the
  Tally Hall from 17 to 166 (the probe in the Adit camera tour: `D:/crulanda-work/world-captures/adit-tour/adit-perf.txt`). The cost
  was the zone's props (the whole dungeon drawn from anywhere: 15,857 renderers, 56 million triangles) and its 186 mobs. Now in a zone
  with caves a prop more than 70 m from the camera is not drawn (`PropCull`), and a camp's idle mob more than 85 m from you sleeps
  (`EncounterEnemy.Dormant`; wakes at 75 m, or the moment a call, threat or foe reaches it; road patrols never sleep). Watch for pop-in in
  the biggest halls (the Foundry) and for any event whose mobs should move while you are far: they stand until you come or call them.
- **New tonight, for Chris to look at (BUILT, OPEN):** the walk-in barns' walls no longer flicker; Nerve stays at 0 out of a fight; Mira
  runs out of mana in long fights, warns, drinks between fights, and casts Briar Lash and Kindled Seed when nobody needs healing; cave
  water without sunlit streaks, glow or foam lines; roamers outside half the open-air camps; worked nodes vanish whole and return sooner
  with more people about; the Sound panel in the pause menu; the chapter-house doors open; the chapel's tour frame fixed.
- **Asked of Chris, not answered:** whether "slow the progress down" (note 219) also means slower levelling (less XP); the 14
  decisions at the end of `PHASE8_6_LIVING_WORLD.md` and the 13 at the end of `DUNGEON2_DESIGN.md`; the bandit camp, the Bandit King and
  loot.
- **Open work, in order:** Chris's reactions (the last tests: EditMode 22/22, PlayMode 35/36 at 04:10 with the one red fixed and BuildingGroundTests 2/2 at 04:27; every footing now 3 cm further out, so check a few buildings' looks); the fixture cave's two reds in `AditCaveBuildTests` (the test cave's rail and
  loop, not the Adit's); sound effects and ambient sounds for the Sound panel's two empty channels (`AudioMix.Effective`); the Adit
  played through; Phase 8.6 once Chris decides.
- **How work runs:** up to three helpers (Chris: "you can run 3 bots at most"), none running Unity; compile with the scratch Roslyn
  check (`scratchpad/ccf/build.py` for main, `scratchpad/cc/build.py` for the worktree; a new session must rebuild these, they live in
  the old session's temp folder), commit by path; one Unity job at a time through hidden chains `D:/crulanda-work/chainRNN.ps1`
  (R124 the last). Chains wait while the game is open. Never write a chain's paths through Python without raw strings: a backslash-a in
  "\adit-perf" became a bell character tonight.

## Oct 14 night (history; Start here as it stood at 23:00)

**Current state: the night of 2026-10-14, 23:00** (Chris away from 20:30: "keep working till 11pm then backup, publish, and create a
handoff. i wont be here to approve anything so you have full reign. shutdown at 11pm"). His words of the day verbatim in
PLAYTEST_NOTES 179-199; what was done, in order, in `Docs/CHANGELOG.md` (2026-10-14, 11:40 on, and "The evening's reports").
Nothing is VERIFIED until Chris has seen it in his build.
- **Branch:** `feet-bow-adit-kit`, PR #2, pushed. Do not merge to `main` without Chris. The Adit rework is on `adit-rework` (worktree
  `D:\code\mmo-adit`, pushed, not merged).
- **Published (the playable build):** 19:54, the build of 19:51; the same build again at 20:12 (nothing newer). Saves backed up to
  `D:/crulanda-work/save-backups/20261014-1954-before-shepherd-publish` and `-2012-before-night-publish`. It holds everything below.
- **New today, for Chris to look at first (all BUILT, OPEN until he sees them):**
  - **The wild woods** in every zone (the "Elwynn" brief): the zone's own trees as one canopy, old oaks (the broadleaf grown large,
    not the half giant) only in the dense old-forest patches, fallen trunks the kit's dead trees, fuller glades. Frames:
    `D:/crulanda-work/world-captures/oakhaven-21-the-mastwood.png`, `-37-lark-hill.png`, `-38-sallow-bottom.png`. Tour 111 a second.
  - **Kevin's melee, spell and work motions** and **every spell's effect**: `D:/crulanda-work/ui-captures/combat3/<class>/sheet-b.png`;
    the Scoundrel inside the yard: `ui-captures/combat5/scoundrel`.
  - **The buff row** by the frames and **the Nerve panel** (movable): `ui-captures/combat4/scoundrel/10-scoundrel_rallying_verse-b.png`.
  - **The inn's tables and benches standing** (the Fantasy pack's root turn kept in ModelProp; every table, bench, stool and mug of
    that pack had lain on its side): `world-captures/oakhaven-99-inn-drinkers-night.png`.
  - **The shepherds** Tam and Nell Sedge at Oakhaven's two flocks (the count at the gate, the shearing, the fleeces to the stall, the
    afternoon feed; the merchant offers fleeces), the Old Fold's flock inside its walls: `world-captures/oakhaven-23-the-old-fold-night.png`.
  - **Quit**: the pause menu's "Quit game", `/exit` and `/quit` (saved first, out of combat).
  - Also: the orc's legs run in the air, the giants' roots, the birds' hops, the Morro villagers on their own avatars (the farmer's
    sack and the skinner's bundle in their hands), the forge's sparks off, the barn's sill and turn, a poor drop's glint fading.
- **Tests (TESTED):** the full PlayMode suite 345/349 at 16:33 (chainR72; see CHANGELOG); after that only the classes each change
  touched (test-budget: Chris, "you just tested for 7 hours"): chainR76 14/14, R78 17/17, R79 EditMode 16/16 and PlayMode 15/16
  (the home test's count, fixed). Balance 49 of 49 builds in range (`D:/crulanda-work/outputs/balance/balance.html`, the page Chris sorts).
- **The Adit rework (`adit-rework`; ADIT_REWORK.md section 18):** the evening's runs took it from 18/29 Adit PlayMode classes to
  20/29 at 20:06 and 19/29 after (one test, the fixture trestle's rail raycast, went red when the deck blocks were
  named and not yet looked at); EditMode 44/44. The decks are whole and joined; the big find at 20:55 was that every branch cave's floor was cut off a few metres past
  its mouth by its parent's knoll and shell (faces metres wide crossing the passage with their middles in the rock): `ZoneBuilder.Cut`
  now drops a face with a corner inside another passage. That lifted the Sunless River (9 to 59 of 341 samples reached) and the Dry Ledge (0 to 54) but not the
  Pen, the Cutting Drift, the Foundry, the Muster Yard, the Chapel or the Breach: the R87 map (21:12) then showed most "unreached" floor was PathComplete from a
  reached sample metres away: one `NavMesh.CalculatePath` from the yard gives up part way through the drawn-out dungeon, so the
  camps test, the breadcrumbs and the run order were measuring the search budget, not the caves. `NavReach.Length` now searches
  leg by leg without giving up on a leg that ends farther off as the crow flies, and the camps test and the map use it (R88: Adit classes 16/23, AditGateTests green); still a leg-by-leg search ends in pockets,
  so the map now also floods by hops (R90, the measure of the mesh itself): the River, the Dry Ledge, the Breach, the Quiet Miner's
  Drift, the Pitch Pit and the Scriptorium are wholly reachable; **really cut off were four places;** the Pen was opened at
  21:48 (the Spoil heaps camp's dressing blocks had walled the strip beside Lusk's stage: the camp is on the west side now, R92).
  The Cutting Drift and the whole Geode Foundry opened at 21:56 (a knoll face wider than the drift's tube: `Cut` now tries each
  edge's middle). The chapel opened at 23:41 (the altar stood in the doorway to the cloister stair) and the island under Lusk's stage
  went at 00:13 (shelves inside a solid deck's body): **every floor of the Adit is reachable from the yard** (R99), the camps,
  gates and breadcrumbs tests green, the Adit nav set 19/23 (R100, 00:24). The "solid" with mesh on its top was a buried cave's knoll poking into the hall (fixed 00:49).
  Three helpers worked the small hours (Chris: "you can run 3 bots at most"): `AditDataTests` (every reference in the Adit's data
  resolves), the Adit camera tour (`Crulanda.exe --crulanda-adit-tour <dir> --crulanda-zone zone.adit --crulanda-temp-save`,
  28 frames: `D:/crulanda-work/world-captures/adit-tour` once it has run), and the release of `after` camps (bosses' relief shifts,
  the Wake's waves), which the data named and nothing spawned. **The Phase 8.6 plan** is written:
  `Docs/PHASE8_6_LIVING_WORLD.md` (PROVISIONAL; fourteen one-line decisions for Chris at its end; the inn-door jam's cause is in it:
  a 1.6 m door and a navmesh baked at agent radius .5 leave a 0.6 m strip, one body at a time). **The Adit's nav, camps, gate and release tests are 26/26 (01:41):** every floor reachable, the way home
  down the cloister stair (its foot was 1.5 m below the Gallery's floor), the bosses' relief shifts, the Wake's waves and the keg's
  release written and tested. Left: the fixture test's knoll ray and rail check; the built rooms on the dungeon packs (a helper);
  then play the run. The Adit camera tour ran once (28 frames, too dark: a lamp on its camera now, re-run pending); the release of `after`
  camps is being written (ADIT_REWORK.md section 18).
  The real short blocks the map names: the Pen's floor from the hall beside Lusk's stage (a "Solid" at (7, -24, -183)), and the
  Cutting Drift's own floor past its Company door. Start the next Adit session from `adit-nav-diagnostics-R88.txt`. The diagnostics (`AditNavDiagnostics`, lane A) write
  `D:/crulanda-work/encounter-validation/adit-nav-diagnostics.txt`: the deck tables, a floor map of every hall (reached from the yard
  or not, what stands by the first unreached sample), each gate's crossing. Run the Adit set with a copy of `D:\crulanda-work\chainR86.ps1`.
- **The small hours (00:40-01:45; CHANGELOG's top entry):** three helpers at a time (Chris: "you can run 3 bots at most"): Oakhaven's open
  ground filled (eleven places, ten secrets), secrets' clues and villagers' stories in every zone with a Lore tab, the Phase 8.6 and
  Hallow's Creek plans (decisions for Chris at their ends), the Adit's releases, camera tour and data check. In flight when this was
  written: the dungeon packs into the builder (worktree), the water/grass/candle shaders, zone music and the Fantasy props (main).
  Their files land uncommitted in each repo: compile with the scratch Roslyn check, commit by path, run the touched tests.
- **Next, in order:** Chris's reactions to the above; the Adit (section 18's "still red"); the Stylized Newbie Weapons Pack (skinned
  meshes: a bake before they can be held); the fixture test's knoll ray; the older list below.

## Oct 12 night (history; these runs were stopped and replaced on Oct 13)

**Read the "2026-10-13, 00:03 on" section below first: it is the current state.** (The bullets about suiteQ, chainR, editR and balC
further down this section are from the night of Oct 12 and are history: those runs were stopped, and Oct 13's runs replaced them.)
- **Branch:** `feet-bow-adit-kit`, PR #2. Do not merge it, and do not publish, without Chris.
- **Published build:** commit 8dbb69d, published at 1:42 PM on Oct 12. Everything after that is committed and pushed but not published.
- **Save backups:**
  - `D:\crulanda-work\save-backups\20261012-1325-before-publish`
  - `D:\crulanda-work\save-backups\20261012-1040-before-path-rework`
  - `D:\crulanda-work\save-backups\grok-2155`
- **Not compiled yet:** the Cleric, Shaman, race and race-UI code (from ae04cd6 onward). It is all committed but has not compiled once. The first compile comes from `editR` (below).
- **Running now** in lane A, `D:\crulanda-work\encounter-validation`:
  - `suiteQ`: the full PlayMode suite, started at 23:13. Results go to `suiteQ.xml` and `suiteQ.log` in lane A. It tests the code from before ae04cd6; the lane was copied when it started.
  - Then `chainR` (`D:\crulanda-work\chainR.ps1`, PID 26280) runs three jobs one at a time. Each copies the current repo into the lane first.
    - `editR`: the EditMode suite, which is the first compile of the new code. Output: `editR.xml`, `editR.log` and `editR.out`.
    - `socialR`: SocialPullTests. Output: `socialR.xml`.
    - `balC`: the balance harness with `CRULANDA_BALANCE_CLASSES=cleric,shaman`. Output: `balC.xml` and `D:\crulanda-work\outputs\balance\balance-cleric-shaman.csv` and `party_heal.csv`.
- **First 3 steps:**
  1. Check whether `chainR` (PID 26280) is still alive. Read `suiteQ.xml`, then `editR.xml`. If there's no xml, look for `error CS` in the log.
  2. Fix the compile and test failures. The new code is in `ClericKit.cs`, `ShamanKit.cs`, `Races.cs`, `RaceTests.cs` and `BalanceHarness.cs`, plus the Cleric and Shaman blocks in `Encounter.asset`. Then rerun the failing test groups alone.
  3. Read `balC` and report Cleric and Shaman DPS and HPS against the role medians and the caps. Check that the party heal rows now say "3 sims joined".

## 2026-10-13, 00:03 on (Claude): pickup, then balance on fixed dummies
- EditMode 456/460 at first (the Cleric and Shaman compile); the four failures fixed (icons for their 24 abilities and 115 talents, the
  class count of ten), commit 05c78ff. After the refresh fix: **EditMode 461/461** (00:51).
- **Balance (Chris: "dps and healing per sec on test runs ... on all classes", fixed dummies, "62-72 is good range", "a number for each
  build ... tank/dps/support/healer/CC", "why is the druid healing so low").** All in `Docs/BALANCE_REPORT.md`, round 5. The table is
  `D:\crulanda-work\outputs\balance\BALANCE_ALL.md` (`python tools\validation\balance_merge.py`); `balance_retune.py [--apply]
  [--skip=a,b]` moves builds outside their target. Older runs in `outputs\balance\archive-*`.
  - Fixed: refreshed heals/damage over time keep their tick schedule (the Druid's Seedling never healed); the Druid and Paladin heal the
    whole party; the Shaman's charge; the heal scale in the Archivist, Paladin and Salt-Mender kits; the harness (no heals in damage
    fights, Brand/Unwrite/Marrow Drain kept up, songs started once, heals taking turns in the heal tests).
  - 35 of 40 builds in range after the first retune (01:15 run); the second retune and the harness's healing fixes are in the confirm run.
  - Targets settled by Chris for level 15 (02:07: "the #s you proposed are good til lvl 15 and will change as we progress to 30"):
    DPS 62-72, CC and Support 45-52, Tank 42-55, Healer at most 35 DPS and 40-50 HPS. Set them again for levels 16-30 (Phase 9).
- **After 02:00 (Chris's briefs, all in BALANCE_REPORT.md):** spirits (SpiritCompanion.cs: aether butterfly for the Stormcaller, glade-fire
  for the Rootmend); Oakhaven's training field (EncounterSession.Training.cs, ZoneBuilder.Training.cs, oakhaven.json; TrainingFieldTests);
  area by specialization (EncounterSession.Spread.cs; AoE targets PROPOSED: x1.7 or more against three for the five area builds, x1.25 or
  less for the rest); the tank test in the harness. None of these have been run yet: chainR12 (balance) and chainR13 (training tests)
  run them. Then: tune the spread shares and the spirits so pet and player land on 62-72 and the area builds on x1.7; read the tank
  ranking; keep one class (the Paladin) from leading all three roles; show Chris the field in a build.
- **Next class, designed not built:** the Scoundrel (`Docs/CLASS_SCOUNDREL.md`; Chris: an assassin-type rogue with charisma for songs,
  merchants and dealing; a single-target DPS path, a group-CC path, a support path; canon root Valen, "yes good example"). Chris settled
  the questions (Scoundrel; three paths for now; races once all races are defined; song, special weapons later). Build when he says go.
- **The grey (TODO 8) done:** Hollow Men's blows grey (EncounterEnemy.Grey.cs), EditMode test. GAME-ONLY mechanic, CANON creature.
- **Adit NavMesh tests 3/3 (01:12):** the column and plinth tops are off the navmesh (TODO 14's code is done); the group run to the end
  is still to do (`tools\validation\group_run.ps1` on Chris's screen, with the lane B build the chain makes).
- SocialPull 7/8 at 01:14 ("The first is home"): fixed in 31cad5d (a mob finishes the walk home before strolling; far-off mobs ask
  for the way home at most every 2 s). The full suite from 01:34 (`suiteS.xml`) includes it.
- **Stopped at 02:29 (Chris: "once ... full playmode suite's result is finish we need to wrap it up"):** only the full suite from 01:34
  ran to the end (`D:\crulanda-work\chainR9.ps1`, `encounter-validation\suiteS.xml`). **Result (01:34-03:00, code as of 01:34):
  PlayMode 315/318.** SocialPull passed (the walk-home fix). The three failures are the known animation items: the hoe hand spread (hands
  37-43 cm apart, 45-60 wanted; TODO 5), the foot flake (running after a jump, toes pulled up; TODO 4), and one new: a long tool's shaft
  through a villager's leg while walking (Grete Lowe, 0.07 m; `A_long_tool_is_held_in_the_fist_like_a_staff_clear_of_the_arm`). The
  work after 01:34 (spirits, training field, spread, tank test, harness fixes, the grey) is not in this run; chainR12/R13 test it. **Next session, first:** run
  `D:\crulanda-work\chainR12.ps1` (all ten classes on the dummies with every Oct 13 change: the spirits, area spread, tank test, toggles
  and healing fixes; the merge; EditMode `editT`; the lane B release build with -NoPublish) and then `chainR13.ps1` (TrainingFieldTests).
  Both wait for `CHAIN9_DONE` in chainR9.out, which is there. Then tune: `balance_retune.py --apply`, the spread shares
  (`EncounterSession.Spread.cs`) to x1.7 against three, and the spirits so pet and player land on 62-72; report the table and the tank
  ranking to Chris; show him the training field in a build (republish only with his OK).

- **2026-10-13, 12:42 on (Claude, after the restart):** chainR12 failed to compile (a comment had swallowed the end of a harness line);
  fixed. Then the harness itself was found unfair, four ways (BALANCE_REPORT.md round 6): the test scene's other sentries took the area
  spells (single-target numbers padded), the dummies were re-picked each fight (once off the navmesh: the Druid cast nothing), the
  spread cache never switched on, the tank test hit for a flat 40 with threat piling up. All fixed; the honest table at 13:23 (27 of 40
  in range), the tank ranking (Warrior, Shaman Stonebound, Druid Barkhide, Mason, Paladin Oathguard), the retune applied (d2a4880).
  EditMode 461/462 (WorkshopDataTests insisted on "the last six" props; now the append rule, 37106f1). TrainingFieldTests 3/3. The
  13:27 training frames (`D:\crulanda-work\ui-captures\training-20261013-1327`) showed the training dummy's nameplate floating off its
  model (its agent snapped it away) and the HUD cut off under the saved layout: each dummy's look now on its own actor, the capture
  frames the HUD (37106f1, 740f779).
  **Confirm run 13:55: 40 of 40 builds in range** (BALANCE_REPORT.md; the Stormcaller's splash eased once more after it, untested).
  Party-of-five healing reported (the Paladin, Salt-Mender and the Cantor leave a party low). **Open for Chris:** a party target for
  healers; the single-target builds whose own spells hit several foes (the Unwriting, Thornsong, Bonecaller, Sifter, the Shaman's kit).
- **The training field became a training yard** (Chris, ~13:40: "why is one in the river? sparring area needs to resemble something like
  this? try the small props pack"; then "two archer targets upside down", "two dummies upside down", "area is not closed in"):
  `Docs/TRAINING_YARD.md`. On dry ground at (58, 26), a closed ring of plank panels, tents, banners, dummies, butts, a healer's corner; the
  Small Props Pack (CC0) imported to `Resources/Props/SmallProps` with an atlas material and half turns for its upside-down models.
  TrainingFieldTests 4/4, EditMode 462/462; frames `D:\crulanda-work\ui-captures\training-20261013-1417` sent to Chris. Lane B holds a
  release build of all this (not published: republish only with Chris's OK, saves backed up first).

## Rules (from CLAUDE.md and Chris)
- Run Unity only in the validation lanes: lane A (`D:\crulanda-work\encounter-validation`, tests) and lane B (`encounter-validation-b`, builds and captures).
- Never run Unity on `D:\code\mmo`.
- Run one Unity job at a time. Tests go through `C:\Users\ChrisWin11\crulanda-cap\rt.ps1 <PlayMode|EditMode> <filter> <name>`, which copies the repo Assets into lane A first.
- After every chunk: commit, push, then `pwsh tools\Backup.ps1`, which copies to `E:\claude\unity projects\mmo`.
- Keep `.meta` GUIDs. Write new metas yourself and never let assets re-import with new GUIDs.
- Back up saves to `D:\crulanda-work\save-backups\<stamp>` before changing any save format.
- Label all lore CANON, CANON-EXPANDED or GAME-ONLY. The canon source is `D:\code\crulanda`, which is read-only.
- No machines, in lore or in kits.
- Keep at most 3 `candidate-*` folders in `D:\crulanda-work`. Delete the oldest first, and never the published build.
- Movement keys are ESDF.
- Nothing is VERIFIED until Chris has seen it. Write TESTED or BUILT otherwise.
- Hard stops: 35-minute chunks, each ending with commit, push, backup and a report.

## Done on Oct 11-12, with commits
- **Farmers and the hoe:**
  - f4400b6: the farmer's grip and stance (note 119).
  - 6feb10e and 5b5d931: the hoe stroke and the plough team (notes 150 and 155).
  - 394ad33: the hoe hand spread (47-58 cm).
  - The spread regressed later. dcd45d8 tried a margin change, which dfe50ff reverted.
- **Class path reworks:**
  - a0d3ca0: the Salt-Mender's fourth path, the Mender (healer, CANON-EXPANDED), with its sims and its own figure.
  - cd9ca8d: the Mage's Cinderwright reworked into the Bonecaller (necromancer, GAME-ONLY), with a save migration.
  - 4f5d5ca and f600363: the Archivist's Unwriting (damage over time) and Still Vow (shields).
- **Auras:** efdef7f, 4756505 and 89488b0. The rings follow the drawn body, and the pet frame says "Thrall".
- **Houses and trees fade:**
  - 5819e26: houses are never see-through outdoors (note 159).
  - 8f67fe5 and 8dbb69d: only the part that's in the way fades, and trees never vanish.
- **Mira invites, the leash, the meter:** 8f67fe5 and 8dbb69d cover the invite target and Mira, and the leash. A group breaks off only past the far leash and after 5 s with no blows. 8dbb69d is the published build.
- **Movable meter and pet bar:** d0b9aef (MovablePanelTests). Frames in c3ba8b3.
- **Thrall:**
  - ec8a817: built by hand in Blender (7.8k tris, same 18-bone rig and GUID).
  - 7bd9f7e: narrower skull, angular jaw and a grave-cloth hood (note 175), plus an in-game video.
  - Hunyuan3D wasn't used; its venv is broken (see PC notes).
- **Balance harness** (`Tests/PlayMode/BalanceHarness.cs`, `Docs/BALANCE_REPORT.md`):
  - da65872 (v2), 173c5d7 (v3) and 53e9161 (v4) fixed HPS, the seeded openings, the cooldown-first rotation, `foes_hit`, gear and the class filter.
  - 7089d8b: round 1 retune with build damage scales (`EncounterSession.RoleScale.cs`) and role DPS caps (note 177). Healers must stay at or under 50% of the DPS median, control and support at 70-75%, enforced by `CapViolations`.
  - c17f429: pet damage scale (Beastbond .75, Bonecaller 1.6).
  - ae61e0b: v6.
  - c6715af: v7, the Druid retune (Thornsong x2.1, Rootmend heals x2.2).
  - dfe50ff: v8 confirm run. The casters are back to their numbers and the Druid retune holds.
- **Party heal test (note 178):**
  - ae61e0b and dcd45d8 added it, and 065561c made it wait for the sims and log why none joined.
  - bb082f3: the test now makes its own SimPopulation and sim figures. Until then 0 sims joined, because PlayableEncounter has no zone. This hasn't been tested yet; `balC` is the first run.
  - Healers in it: Paladin sanctuary, Salt-Mender mender, Archivist cantor, Druid rootmend, Cleric lantern and choir, Shaman rainspeaker.
- **Zone-loading fix:** 512e056. The Oakhaven scene listed only 2 of its 6 zone files. This caused about 35 of the 42 PlayMode failures, and the reruns are now green (46/46 and 12/13).
- **SocialPull test fix:** bb082f3. The test waits out QuietSeconds; it had been out of date since 8f67fe5. Not rerun yet; `socialR` is the rerun.
- **Feet:** 0f81dbe. Standing, the body sits to the higher sole, which took FootTests from 12 problems to 1. GroundingTests passes. The rest is parked (see the TODO).
- **Cleric and Shaman** (design in `Docs/CLASS_CLERIC_SHAMAN.md`):
  - f264e07, ae04cd6 and 2c896da: ClericKit, with 12 abilities in `Encounter.asset` and `Talents/cleric.json` (Lantern, Choir, Censure and Warden, 17 ranks each). It's a healer that uses mana, labelled GAME-ONLY.
  - 8b0d6bc and 323fa03: ShamanKit, with 12 abilities and `Talents/shaman.json` (Rainspeaker, Stormcaller, Stonebound and Gravewind, 17 ranks each). It's orc-only and CANON-EXPANDED, signed off by Chris.
  - Both kits are switched on in `ClassKit.Create`. 96cb8be and 323fa03 add them to the harness.
  - f526f6e: both count as healers in the sim hooks.
- **Races** (`Docs/RACES.md`, `Scripts/Encounter/Races.cs`):
  - 323fa03: Human and Orc.
  - 391032e: orcs play Warrior, Ranger, Rogue, Mage and Shaman (no Druid). The Shaman is orc-only and the Cleric is human-only.
  - Saves get a `raceId`; an old save reads as human.
  - `EncounterSession.FreshProgress` takes a race and corrects a class/race clash.
  - EditMode `RaceTests` has 5 tests.
- **Orc sims and race in the UI:** ecf1238. About 1 in 4 sims whose class orcs can play is an orc (by roster position, so the rolls are unchanged). Race shows on a sim's card and on the character sheet.

## PC notes
- **Ethernet power saving:** with Chris's approval, Energy-Efficient Ethernet, Green Ethernet and Power Saving Mode on the Realtek 5GbE adapter were disabled (ffde5f1). The original values are in `D:\code\mmo\tools\nic_power_original.txt` for reverting.
- **Disconnects:** the ~8:12 PM drops were on the agent connection, not the PC. The event logs showed no sleep, shutdown or NIC errors.
- **Keep-awakes:**
  - PID 19320 is the sleep keep-awake (`keepawake.ps1`, SetThreadExecutionState).
  - PID 26952 is the disk keep-awake (`D:\code\mmo\tools\keepdisk.ps1`), which writes a tiny file to D: and E: every 15 min.
  - Leave both running.
- **Hunyuan3D-2 mini:** its venv under `D:\crulanda-work\tools` is broken and was never used.

## Staged, not imported
- Orc packs are in `D:\crulanda-work\imports\orcs`: "3D Stylized Orc - animations+PBR (13.6k faces)" and "Orc Army Pack" (includes an Orc Shaman).
- Their rigs and scale haven't been checked against the humanoid/ModelFigure rig yet.

## TODO, in order
1. **Chris's reactions** to the woods, the motions, the effects, the butts and the trap (the frames named under Start here).
2. **The Adit rework** (Chris: "adit dungeon need to be more complex like wow's wailing caves or the deadmine"; "you can start expanding
   adit so not so empty next on the agenda"): branch `adit-rework`, worktree `D:\code\mmo-adit`, `Docs/ADIT_REWORK.md`. Build steps 1-3
   written, the twelve new elites' moves and loot written (sections 14-17). Its first Unity run: EditMode 488/495, Adit PlayMode 19/30.
   Red, in the order to fix (section 16): the cave web's joins (the Cutting Drift's lip into the Foundry), rock tops the navmesh bakes
   on, the Sandthrone pickets 12 m up with no way there, two overseers 1.2 m apart, the run order from the bridge's tower, Lusk's door
   after his death, the planner's modes test. Then S4-S7 (the releases, the Wake, the NEW moves, the cave map), then Chris's decisions
   (section 1), then merge. Test with `rt.ps1` against the worktree's Assets (chainR68.ps1 is the pattern).
3. **The Stylized Newbie Weapons Pack** (downloaded 12:01): its weapons are skinned meshes driven by the pack's own character script;
   bake each to a plain mesh (an editor method) before `model.weapon` looks can hold them. Low-level gear.
4. The hoe stroke's measures under the ploughing clip (FieldWorkTests: a stroke every 1-3 s), if the final run shows it red.
5. Spell effects, second pass: each class's own colours and shapes where the packs' stock looks repeat (the healing glow is one green
   for every healer; a cast's hand glow is the same sparks for all).
6. Bonecaller tiers 4-6.
7. An enemy that applies the grey (Adit/Wasting).
8. The sim-follow flake.
9. GroundingTests' sim 2.8 cm up; the parked tool shaft (VillageDayTests); EliteFightTests under -nographics.
10. The three later dungeons (DUNGEON_DESIGN.md) when the level cap rises past 15.
11. Republish only with Chris's OK; back up the save first (`chainR61.ps1` is the pattern).

## Key docs (in `Docs\`)
- `BALANCE_REPORT.md`: harness rounds v1-v8, caps, retunes.
- `RACES.md`: canon check, race storage, orc plan.
- `CLASS_CLERIC_SHAMAN.md`: design and status.
- `PLAYTEST_NOTES.md`: Chris's words, numbered notes, TODOs.
- `ZONE_FILL_PLAN.md`: note 172.
- `HANDOFF_ARCHIVE.md`: older handoffs.
- `D:\code\mmo\CLAUDE.md`: the rules.