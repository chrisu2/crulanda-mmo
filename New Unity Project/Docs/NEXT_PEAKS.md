# Next job: the Shattered Peaks filled out (camps + quests)

Written 2026-10-03 17:00 for a fresh session. Paste the block at the bottom as the first message. Everything a new
session needs is here or in `Docs/CLAUDE_HANDOFF.md` (read RESUME HERE first), `Docs/QUEST_DESIGN.md`, `Docs/WORLD_ZONES.md`.

## Why
Chris: "lets work on quests and camps", zone by zone. Khaven was done 2026-10-03 (commit 12d8ddc: 4 camps + 5 quests, the
pattern to copy). Peaks is next, then Ash Rim. Peaks today: 9 camps, 9 quests, levels 6-8, zone size 430. Its grown ring
(Signal Tower, Goatherd's Shieling, Sealed Adit, Cold Tarn, Listening Shrine, Broken Post, avalanche scar) has landmarks
with no camps at the Sealed Adit, the Cold Tarn, the Listening Shrine, the Broken Post and the Umbra scarp.

## Canon to lean on (D:\code\crulanda)
- CANON (world_bible.md): the Shattered Peaks hold back the Wasting on the east border while the void eats the stone; the
  Pale Things are cosmic auditors, not just monsters; the Silent Pilgrims (south); Sandthrone mercenaries hold the pass and
  take a toll (book1 ch.5); old mining tunnels under the Peaks (book2 ch.19).
- Everything placed in the Peaks is GAME-ONLY / CANON-EXPANDED; label it in `canonStatus` as khaven.json does.

## Design (4 camps, 5 quests; same shape as Khaven)
Existing tags only, so no new SocialRulesTests kind, no new loot table, no new look. Camps must stand 125 m+ from
Pilgrims' Rest (house at -16,6); the Toll-house is the enemy's and is excluded (ZoneGrowthTests.EnemyBuildings).
Camp indices below assume they are appended in this order after camp 8 (Old Scree-Tusk).

| # | name | tag / mob / look | centre | count | level | notes |
|---|------|------------------|--------|-------|-------|-------|
| 9 | Adit hollows | hollow / Hollow Man / hollow | (-128,-62) r8 | 5 | 7-8 | out of the Sealed Adit (-149,-42); respawn 90 |
| 10 | Tarn shadows | paleshadow / Pale shadow / pale | (-52,152) r7 | 3 | 8 | by the Cold Tarn (-24,136); respawn 120 |
| 11 | False pilgrims | cultist / Grey-robed false pilgrim / cultist | (140,-70) r8 | 5 | 7-8 | ambush: true, at the Broken Post (147,-83); add a crates prop "False pilgrims' packs" at (144,-78), interact "Search the false pilgrims' packs" |
| 12 | The Umbra Watcher | pale / The Umbra Watcher / pale | (-122,82) r5 | 1 | 8 | one Pale Thing below Umbra scarp (north) (-90,90); NOT elite (an elite needs a signature loot list; LootDataTests pins 104 named items); respawn 180 |

Quests (append to `EncounterContent/Quests/peaks.json`; kill targets by camp index `mob.<tag>.peaks.<camp>.*`):
1. `npc.maddoc.adit` "What the Mine Gave Back" (npc, Maddoc Vire, L7, requires npc.maddoc.cairns): visit the Sealed Adit
   (-149,-42) r9 with a `say` (the Company bricked the old adit; the mortar is fresh and something inside has been scratching
   at it from the other side), then kill `mob.hollow.peaks.9.*` x5. Rewards xp 190, gold 35.
2. `npc.tarsk.tarn` "Shadows on the Cold Tarn" (npc, Tarsk, L8, requires npc.tarsk.pale): kill `mob.paleshadow.peaks.10.*` x3.
   Tarsk: the shadows stand in the tarn's shallows at dusk, looking down at the water the way the pale ones look at the road.
   Rewards xp 220, gold 40.
3. `faction.sandthrone.2` "Grey Robes on the East Road" (faction, Hadrik Sull, L7, requires faction.sandthrone.1): kill
   `mob.cultist.peaks.11.*` x5, then interact "False pilgrims' packs" (say: pilgrims' grey over Company mail; a list of carts
   the gate let through, in a Sandthrone hand). Copy the reputation reward shape from faction.sandthrone.1 (sandthrone up,
   saltmenders down). Rewards xp 200, gold 45.
4. `side.peaks.shrine` "What the Shrine Hears" (side, Maddoc Vire, L6): visit the Listening Shrine (10,144) r8 at night
   (`"night": true`; say: the shrine has no idol, only an ear cut in the stone, and at night the wind through it says the
   toll-gate's numbers back, cart by cart), then talk to Tarsk. Rewards xp 110, gold 15.
5. `npc.yara.watcher` "The Thing on the Umbra Scarp" (npc, Yara Quell, L8, minLevel 7, requires npc.yara.rockhide): kill
   `mob.pale.peaks.12.*` x1. Yara: her drovers will not take the drove track while it stands up there counting them.
   Rewards xp 230, gold 45.
Voice: short, dry, afraid people (life.mood is "afraid"); Hadrik is all trade; Tarsk is an Ash-Walker scout, few words.
No new quest items (so no icons, no Encounter.asset change). Update `canonNote` in peaks.json with one sentence per addition.

## Procedure (all in CLAUDE_HANDOFF.md; the short form)
1. Edit `EncounterContent/Zones/peaks.json` (camps, the packs prop, canonNote) and `EncounterContent/Quests/peaks.json`.
   Never edit Assets while a run is mirroring them (tests start, build start).
2. Focused tests via the PowerShell tool (not bash's powershell.exe: execution policy): `tools\validation\run_focus.ps1
   -Filter ZoneGrowthTests;SocialRulesTests;QuestDataTests;LootDataTests;ZoneDataTests -Platform EditMode -Tag peaks1`
   (check the exact fixture names with Glob on Tests/EditMode first). Fix until green.
3. Full run WITH a Peaks tour (zone data changed): `New-Item c5a-start.marker` in hel\work, then
   `& 'D:\code\mmo\tools\validation\start_detached.ps1' -Arguments '-Zones zone.peaks'`; wait for `full-run.done` newer than
   the marker (about an hour); EditMode/PlayMode all pass, build Success, no exceptions in the game logs.
4. Publish: robocopy `encounter-validation\Builds\Crulanda` -> `hel\outputs\Crulanda-Playable` /MIR /XF *.log (exit 1 = copied).
5. Docs: CHANGELOG.md entry, CLAUDE_HANDOFF.md round entry + RESUME HERE; commit (author is repo-local; trailer
   `Co-Authored-By: Claude <model> <noreply@anthropic.com>`); copy the handoff to `hel\outputs\Crulanda-Claude-Handoff.md`;
   `tools\Backup.ps1`; update memory `crulanda-where-we-left-off.md`.
6. Chris asked (2026-10-03): after Peaks, save progress, write the handoff, then **shut down the computer**
   (`Stop-Computer -Force` via the PowerShell tool, after the backup has finished and Unity/Crulanda.exe are closed).

## After Peaks (Chris, 2026-10-03 21:20: "lets do your recommendation")
1. Playtest note 9, bloom and sun (open; lifts every screenshot). Then 2. the Ash Rim's camps + quests, same shape as Khaven
and the Peaks. Each is its own round: tests, full run, publish, docs, commit, backup. Shut the computer down only after the
LAST round Chris asks for in that session, or when he says so.

## Paste this to start the new session
```
Read Docs/NEXT_PEAKS.md and do it: fill out the Shattered Peaks with the 4 camps and 5 quests it describes, test (focused,
then a full run with a Peaks tour), publish, write the changelog + handoff, commit, back up, then shut down the computer.
One agent at a time, no workflows. Ask me first and short if anything is unclear.
```
