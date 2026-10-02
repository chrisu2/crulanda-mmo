# Chris's playtest notes

Chris jots these down as he plays. Each note keeps his words, what was measured, and what happened to it.
Status: OPEN (not started), PLANNED (design decided), BUILT (on a branch, not yet run or published), FIXED (published; says
which publish).

## 1. The bandit camp is way too close to the village (2026-10-01) — BUILT (branch `fix/n1-oakhaven-grows`; compile-checked, not run, not toured, not published)
> "one thing i notice is the bandit camp is way too close to the village. i asked before to expand the zone."

Measured in `oakhaven.json` (the village green is at about (-8, -8)):
- The first Sandthrone deserters stand at (-16, 84.5): 93 m north of the green and only about 25 m past the Woodyard
  (-24, 60). Crowsfoot Hollow's mouth is at (-16, 93), 100 m out. That is a 15-20 second walk from the Great Oak.
- The Old Orchard (-55, 88) and the Carder farm (-61, 45) are as close to the bandits as to the village.
- The cave runs north from there to Caddock at (-0.8, 175.6), against the zone's north edge (the zone is 380 m, so the edge
  is at 190). There is no room to push the hollow further out without growing the zone.
- The 2026-09-30 growth (260 to 380 m) added places in the new ground but left the hollow's mouth where it was.

What the fix needs: grow Oakhaven again and move the whole of Crowsfoot Hollow (mouth, cave, the camps in it, Caddock and
Hesk, the quest's objective points, its secrets and the rich copper seams) well out, so there is real country between the
village and the bandits: farms and fields first, then wolves and boar, then the hills and the hidden mouth. Target: the
mouth about 280 m from the green (three times today's distance). Also check the other camps against the same rule (nothing
hostile within about 120 m of a village's houses) in every zone.

**What was built** (WORLD_ZONES.md, "Oakhaven grown to 560 m" and "Caves you walk into", has the whole of it):
- Oakhaven is 560 m. The village core did not move; the exits, arrivals, roads, creek and the Wasting's curtain went out with
  the edge.
- Crowsfoot Hollow moved whole, turned a quarter: its mouth is at (-120, 240), 272 m from the green, facing west, away from
  the village, in the west heel of Crowsfoot Ridge, which now stands along the north edge. You reach it by the North road to
  its end at (-32, 184) and the Crowsfoot track west along the ridge's foot and round its heel. The ridge hides the mouth
  from the green, from the road and from the track until its last bend.
- Between: Carder's field barn and the north fields, then the wolf and boar woods, then the hills. Every camp in Oakhaven
  stands 125 m and more from every house (a house, the inn, the mill); all eleven outdoor camps moved, and three were added.
- Six new places, ten woods, three secrets; the nodes re-placed by the placement script (the hollow's nine seams went with it).

**What the lead must run and look at** (nothing below has been run):
- EditMode: `ZoneGrowthTests` (new), and the data tests that read Oakhaven (`WorkshopDataTests`, `HouseholdDataTests`,
  `ProfessionDataTests`, `VillageEconomyTests`, `LootRollTests`, `QuestDataTests`).
- PlayMode: `CaveTests` (one new test), `HollowQuestTests`, `ZoneContentTests`, `SecretPlacementTests`, `NodePlacementTests`,
  `NodeStreamTests`, `VillageStreamTests`, `WaterTests`, `ZoneExitTests`, `ZoneTravelTests`, `TempSaveTravelTests`, `HuntTests`,
  `GatherTests`, `DiscoveryTests`, and the village's (`VillageHomeTests`, `VillageWorkshopTests`, `VillagePurseTests`,
  `CraftStationTests`, `BuildingGroundTests`, `VillageDayTests`, `VillageErrandTests`, `VillageDrinkTests`, `VillageSupplyTests`,
  `OakhavenQuestTests`): no coordinate in them changed, but the village's own random stream shifts with the zone's size (where
  people potter), and the PlayMode suite loads a zone that takes about 1.7 times as long to build.
- The build log's "Zone zone.oakhaven (560 m) built in N ms (...)": the estimate is about 13 s in the editor and 11 s in the
  player (7.6 and 6.4 at 380 m). If it is much over, the ground paint's cap (4096, `BuildGround`) and `wildDensity` are the
  two knobs.
- The world tour (`--crulanda-world-capture`), Oakhaven (file names start `oakhaven-`): `19-crowsfoot-hollow` (the mouth from the track's last bend: a slot
  in a cliff, scarps either side, the brow over it, nothing floating or cut), `20-crowsfoot-ridge` (the ridge from the North
  road's end: a line of hills, no mouth to be seen, the Ridge pines on its face), `85-hollow-camp`, `88-hollow-drop`,
  `89-hollow-stores`, `86-hollow-hall` (the cave inside as it was: the furnishings re-roll, so look for anything standing in
  a wall or in the way), `34-carders-field-barn`, `35-thornshaw`, `36-the-carters-rest`, `37-lark-hill`,
  `38-sallow-bottom`, `39-the-bound-wood`, `40-exit` (the West road's waystone at the new edge), `05-the-wasting` and
  `87-secret-...`; `18-the-tall-grass`, `99-ambush-before` and `99-ambush-sprung` (the stalkers' new patch by the South road);
  every `80-node-NN` of the hollow's seams; and from the green, north: how the hills read on the skyline at 270 m through
  the fog (fog ends at 330 m), and east: how the Wasting's curtain reads from the village now it stands at 254 m, not 164.
- Walk it: green to the mouth by the road and the track (about 330 m on foot); the level-1 start, where the first wolves are
  now about 180 m from the green (Upper pines) and the tall grass is 110 m south of the start, not beside it.

**Deviations and open points:**
- The Wasting's curtain moved out with the edge, as the plan said and the last growth did. It is 90 m further from the
  village. If it should stay the presence it was, it can stand anywhere west of the edge (`wasting.x`): the ground behind
  it is flat unmade, and what stands by the grey would move back with it.
- "No hostile camp within about 120 m of a village's houses" holds in Oakhaven only. Measured in the other four zones
  (`tools/wip/zonegrowth/rule120.py`; nearest house, inn or shelter, in metres): Khaven: Whispering Wood wolves 80, Carrion
  boars 81, the outriders' camp 73, Gloom Creek hollows 73, the Grey Sexton 83, the picket 116. The Peaks (no village; from
  Pilgrims' Rest): Wolf pines 74, Rockhide wallow 94; the toll-gate guards and the eyrie stand at the Sandthrone's own
  buildings. The Ash Rim (from the Ash-Walkers' shelters): Fraying eaters 50, the brood 77, Unwoven Flats 85, Cinderfold 91.
  The Verdant Shore (from Rootfast's treehouses): Antler Meadow stags 43, the Ghost-Oak spiders 72, the Briar Way 95, Old
  Ninebranch 98, reed-boars 102, the Greying 106. **None was moved.** Those zones are 340 to 360 m with a camp ring 50 to
  100 m from the hub, and nearly every camp belongs to a built or canon place (the Whispering Wood, the Carrion Cliffs, the
  drowned graveyard, the Antler Meadow): to keep the rule they need what Oakhaven got, a growth of their own, one zone a
  step. The choice is Chris's: grow them, or keep the rule for open villages only (Khaven is walled and held by the
  Sandthrone; the Rim's enclave fights the Weave-Eaters at its door, book1 ch.20).
- Every wolf and boar camp of Oakhaven moved, not only the bandits: the rule puts level-1 beasts 125 m from the nearest
  house, so the first kill is a longer walk than it was. If that is too far for the first minutes, the rule could spare
  beasts and hold for the deserters alone.

## 2. Elites are too easy for the loot they give (2026-10-01) — OPEN
> "elite was too easy for the loot obtain."

Measured (`EncounterEnemy.MobHealth` / `MobHit`): a camp elite is a normal mob of its level with 2.2 times the health and 1.4
times the hit, drawn 1.18 times the size. It has no ability of its own, its camp does not come to its aid (see note 3), and
with Mira healing nothing threatens the player. From loot step L1 an elite is the only source of a blue beam, and from L2 each
drops its own named rare or epic, so the reward has outgrown the fight.
The twelve camp elites: Caddock, Quartermaster Hesk, Old Whitefoot (Oakhaven); the Grey Sexton, the Pale Reckoner (Khaven);
the Sandthrone captain, Old Scree-Tusk (Peaks); the Brood Weave-Eater, the Ash-Deacon (Ash Rim); Greyheart, Old Ninebranch,
the Hollow Root-Warden (Verdant Shore).

What the fix needs: elites that are a fight. More health and a harder hit; one or two moves of their own that the player must
answer (a wound-up heavy blow to step out of or guard, an enrage when low, a call that brings the camp); their guards joined
to them; and bosses at a dungeon's end harder than the outdoor named beasts. Tune so an elite of the player's level is
dangerous alone and wants Mira, and one two levels up is not a solo kill.

## 3. Mobs are not social: they pull one at a time (2026-10-01) — OPEN
> "some mobs need to be more social. i can pull them easy 1 at a time even when they stand next to each other"

Measured (`EncounterEnemy.Update`): every mob notices the player by itself, within 5 m and with a clear line; nothing links
it to its neighbours. Hitting or walking up to one of four wolves, or one of Caddock's guards, brings that one alone.

What the fix needs: social aggro by kind. When a mob joins a fight, others of its camp within reach join too: pack beasts
(wolves, hounds) come together; people (deserters, cultists, Concord) call out and bring those in earshot, with a short
delay and a shout so it reads; solitary beasts (boar, stags) stay single. Sneaking (Ctrl) should still let a careful player
peel the edge of a camp. A boss always fights with its guards.
