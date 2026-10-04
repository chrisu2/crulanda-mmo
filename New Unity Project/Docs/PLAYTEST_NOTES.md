# Chris's playtest notes

Chris jots these down as he plays. Each note keeps his words, what was measured, and what happened to it.
Status: OPEN (not started), PLANNED (design decided), BUILT (code and tests written; not yet run in Unity, seen or published),
FIXED (published; says which publish).

## 1. The bandit camp is way too close to the village (2026-10-01) — FIXED (published 2026-10-02 morning)
> "one thing i notice is the bandit camp is way too close to the village. i asked before to expand the zone."

**DECIDED (Chris, 2026-10-02: "yes. grow about 20%"):** the other four zones grew about 20% and their camps moved out (WORLD_ZONES.md,
"The other four zones grown"). Before that, the 120 m rule held in Oakhaven only. Either **(a)** grow Khaven, the Peaks, the Ash Rim and
the Verdant Shore the same way, one zone a step, or **(b)** hold the rule for open villages only (Khaven is walled and
Sandthrone-held; the Rim's enclave fights at its door; Pilgrims' Rest and Rootfast are not villages). Until that is
answered the every-zone part of this note is open and the note is not FIXED on publish. `ZoneGrowthTests.NearHomes` names
every camp that breaks the rule today (20 in the four other zones), so nothing new can creep in meanwhile.

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
  stands 125 m and more from every house of the village (a house, the inn, the mill); all ten outdoor camps moved, and three
  were added.
- After review: the third new wolf camp stood 36 m from Moss's lodge (a home: Garet sleeps there). It is the **Hanger
  wolves** now, at (-222, 196) in the north-west wood, 140 m from the lodge. Two low rises west of the mouth hide it from
  the Mastwood, the Old Fold and the West road as well. Oak creek keeps the line it had through the village (its swing is
  counted from its old west end). The copse by the village is the **Moonbell copse**, and Garet's quest sends you to it on
  the way home. The villagers' meadow places are looked for within their reach, so all fourteen are found.
- Six new places, ten woods, three secrets; the nodes re-placed by the placement script (the hollow's nine seams went with it).

**What the lead must run and look at** (nothing below has been run):
- EditMode: `ZoneGrowthTests` (new; it also holds the every-zone list of camps near homes and the creek's line), and the data tests that read Oakhaven (`WorkshopDataTests`, `HouseholdDataTests`,
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
- Frame time, on the green and at the hollow's mouth, against the 380 m build. The ground is one shadow-casting mesh that
  is never culled: 401,000 triangles (was 185,000), drawn about six times a frame (four shadow cascades, the depth texture,
  the main pass), so about 2.4 M triangles a frame against 1.1 M. Unmeasured; if it costs, WORLD_ZONES.md ("Load and frame
  cost") says how to cut the ground into renderers that cull. No code was changed for it.
- `WorldLife` in Oakhaven: `Places["meadow"]` should hold 14 (log it, or watch Lisbet's dawn round).
- The world tour (`--crulanda-world-capture`), Oakhaven (file names start `oakhaven-`): `19-crowsfoot-hollow` (the mouth from
  the track's last bend: a slot in a cliff, scarps either side, the brow over it, nothing floating or cut), `20-crowsfoot-ridge` (the ridge from the North
  road's end: a line of hills, no mouth to be seen, the Ridge pines on its face), `85-hollow-camp`, `88-hollow-drop`,
  `89-hollow-stores`, `86-hollow-hall` (the cave inside as it was: the furnishings re-roll, so look for anything standing in
  a wall or in the way), `34-carders-field-barn`, `35-thornshaw`, `36-the-carters-rest`, `37-lark-hill`,
  `38-sallow-bottom`, `39-the-bound-wood`, `40-exit` (the West road's waystone at the new edge), `05-the-wasting` and
  `87-secret-...`; `18-the-tall-grass`, `99-ambush-before` and `99-ambush-sprung` (the stalkers' new patch by the South road);
  every `80-node-NN` of the hollow's seams; and from the green, north: how the hills read on the skyline at 270 m through
  the fog (fog ends at 330 m), and east: how the Wasting's curtain reads from the village now it stands at 254 m, not 164.
- Stand in the Mastwood (-130, 170), at the Crow oak (-152, 200) and at the Old Fold (-156, 64) and look to the mouth
  (north-east): two low rises (the west spur, the heel rise; 5 and 7 m, sides of about 37 degrees at their steepest) should
  hide the scarps and the hole and read as part of the ridge, not as two lumps. Then Oak creek through the village: the
  mill, both bridges, the rock at (12, -52) and the crates at (-64, -55) should sit by the water as in the last build.
- Walk it: green to the mouth by the road and the track (about 330 m on foot); the level-1 start, where the first wolves are
  now about 180 m from the green (Upper pines) and the tall grass is 110 m south of the start, not beside it.

**Deviations and open points:**
- The Wasting's curtain moved out with the edge, as the plan said and the last growth did. It is 90 m further from the
  village. If it should stay the presence it was, it can stand anywhere west of the edge (`wasting.x`): the ground behind
  it is flat unmade, and what stands by the grey would move back with it.
- (The decision at the top of this note.) "No hostile camp within about 120 m of a village's houses" holds in Oakhaven only. Measured in the other four zones
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
- **Moss's lodge is a home outside the village, and the rule does not hold for it.** It stands at (-93, 143) in the
  Mastwood and could not move in this step (a household's door; the village tests name it). The Mastwood boars are 46 m
  from it by intent (the hunter lives among his game; 41 m before the growth). Crowsfoot Hollow's mouth is 102 m from it
  as the crow flies, with the ridge's heel between and the mouth out of its sight (the old mouth was 92 m from it), and
  the cave's camps lie under the ridge 94 to 110 m off. The tests name these and allow no others. If the lodge should be
  as safe as a village house, the lodge has to move south (about 30 m), which moves Garet's home, yard and game rack.
- Left where they were: the copse south of the east road and its Moonbells (62, -18), the second point of Garet's
  deer-track quest there, and the Old wayshrine (86, 20). The copse was 100 m from the curtain after the last growth and is
  190 m from it now, so its name and the quest's words were changed, not its place: it is the Moonbell copse, and Garet
  says "on your way back look into the Moonbell copse". The quest still walks out 240 m to the grey and back. The other
  way (move the copse, its secret, its windfall and the quest's point out to about (212, 18)) takes a level-1 quest spot
  150 m further from the village; that is Chris's choice.
- The two rises added west of the mouth change heights there, so the scatter and grove trees round them re-roll once more
  (the same stream, the same unpublished growth).
- Saves: no format change. A character saved in Oakhaven stands where it stood (one saved down the old cave stands on the
  meadow by the North road now). Mob ids, secret ids and quest progress are unchanged; resting nodes that moved are full again.
- `tools/wip/professions/place_nodes.py` was edited (the plan named it) and `tools/wip/zonegrowth` added, in a tree the step
  was otherwise told to leave alone.

## 2. Elites are too easy for the loot they give (2026-10-01) — FIXED (published 2026-10-02 morning)
> "elite was too easy for the loot obtain."
> "bandit king way too easy for the loot obtained" (the same night, after killing Caddock)

Caddock, the Bandit King, the end of the first dungeon, is a level 5 elite: 528 health and about 25 a swing, alone in the
fight (his deserters two metres away do not join), no move of his own, and he gives the Tin Crown, a blue beam and from L2
his crown, cleaver or coat. A dungeon's end boss must be the hardest fight in its zone.

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

What was built (2026-10-01; `COMBAT_SYSTEMS_VALIDATION.md`, "Social aggro and elites", has the rules, the moves and the table):
- Health 5.5 times a normal mob's (was 2.2) and hit 3 times, growing 9% a level (was 1.4); Caddock and the Hollow Root-Warden,
  the two dungeon end bosses, a quarter more health and a tenth more hit again.
- Each of the twelve has a named heavy blow it winds up for 1.5 to 2.4 s: a mark on the ground, a cast bar under the target
  frame, a line in the chat. Step out of the mark and it misses; Guard blunts it; stand in it and it lands for 3.5 to 4.6
  swings. Each enrages at 30-35% health (faster swings) and, unless it is a solitary beast, calls once at 60% for its guards
  and kin (Old Whitefoot's howl brings his pack, the Grey Sexton's knell two of the Gloom Creek hollows, the Sandthrone
  captain's shout two toll-guards). The Pale Reckoner has nobody to call yet: no pale camp stands in Khaven.
- An elite above the player's level hits 12% harder and takes 6% less for each level of the gap.
- Mira's heal and health now grow with the player's level (they were fixed at level-1 numbers), so she still matters at 13.
- On paper (the kits' real numbers, on-curve gear): alone and careless, every class dies to an elite of its level; with Mira
  and careful play it falls; a careful Warrior alone does not get one down; nobody solos one two levels up.
- Six elites stand with guards (Caddock, Hesk, the Brood Weave-Eater, the Ash-Deacon, Greyheart, the Root-Warden). On paper
  the way through is to clear the guards first and then fight the elite: every class wins that with Mira. Pulling the elite
  with every guard up is a death for a Warrior even with Mira (Caddock, Hesk, the Deacon, the Root-Warden).
- An older save loads Mira whole: her saved health of 130 (her old full health) is read as full, not as wounded.
- Not done: nothing has been run or seen. The Druid's Barkhide form can still solo an outdoor elite of its level with care
  (it heals itself); that is the class kit, and is written up beside the table.

## 3. Mobs are not social: they pull one at a time (2026-10-01) — FIXED (published 2026-10-02 morning)
> "some mobs need to be more social. i can pull them easy 1 at a time even when they stand next to each other"

Measured (`EncounterEnemy.Update`): every mob notices the player by itself, within 5 m and with a clear line; nothing links
it to its neighbours. Hitting or walking up to one of four wolves, or one of Caddock's guards, brings that one alone.

What the fix needs: social aggro by kind. When a mob joins a fight, others of its camp within reach join too: pack beasts
(wolves, hounds) come together; people (deserters, cultists, Concord) call out and bring those in earshot, with a short
delay and a shout so it reads; solitary beasts (boar, stags) stay single. Sneaking (Ctrl) should still let a careful player
peel the edge of a camp. A boss always fights with its guards.

What was built (2026-10-01; same section of `COMBAT_SYSTEMS_VALIDATION.md`):
- Every camp has a kind, by its look unless the zone data says otherwise (`social` on the camp). Wolves, hounds, Weave-Eaters,
  spiders and briars pack: those within 9 m of the one that joined the fight come at once. People call: a shout in the chat and
  over the caller's head, and those within 12 m come 1.2 s later, kin from the next camp among them. Boar and stags stay single.
- Sneaking, a mob that only notices you brings nobody further than 3.5 m, so the edge of a camp can be peeled one at a time.
- An elite's guards (its camp, and the camp standing beside it) always come, from 16 m, however it was pulled. A guard's
  shout brings his fellow guards but not the elite, unless the guard stands right beside it (3.5 m): guards can be cleared
  first.
- Those who join come for whoever pulled, not for Mira: the threat of her heal is shared out among the mobs in the fight, so
  her second heal no longer turns the ones the puller has not hit yet. The group leashes from where the pull began; when it
  breaks they all heal and go home together and stay there.
- Not done: nothing has been run or seen.

## 4. The sheep look like bugs on sticks (2026-10-01) — FIXED (published 2026-10-02 13:50)
> "fix the sheep. looks like bugs on sticks"

Measured (`CritterBody.Make`, case "sheep"): a sheep is one pale ellipsoid (0.75 x 0.6 x 1.0 m), a black ball for a head and
four thin black cylinders 8 cm thick and 47 cm long. From any distance that is a white tick on black legs.

What the fix needs: a sheep that reads as a sheep in the painted style from 5 m and from 30 m: a deep woolly fleece built of
lumps, short sturdy legs with wool to the knee and dark hooves, a dark wedge of a face with a muzzle and ears out sideways,
a wool cap, a tail; a head that goes down to the grass when it grazes. Then a line-up shot of every animal in the capture
tour, so the others can be judged the same way.

## 5. Less green in the ore (2026-10-01) — FIXED (published 2026-10-02 13:41)
> "less green in the ore"

The copper seams' verdigris: after the first fix (beads pressed flat) seven lumps in ten still carry a bright green patch
(`OreSeam`, fleck (.30, .55, .46)), so a seam reads as orange and green. Fix: a hint of patina only: fewer patches, smaller,
duller and nearer the rock's own colour.

## 6. Lumber trees: just four or five sticks standing up (2026-10-01, again 2026-10-02) — FIXED (published 2026-10-02 13:41)
> "lumber trees . trunk just has 4-5 stick stickup up. make it more broken/chopped down looking"

> (2026-10-02, playing the build with the zones grown) "the lumber trees the trunk still has 4-5 stick poking up. need to looked
> chopped down"

Built: a felled tree, worked (ZoneBuilder.Nodes.cs Windfall): an axe-cut stump with a pale face and chips, the trunk limbed
and bucked in two with pale cut ends, the lopped crown in a low heap of bare branches; chopping takes the logs. Previewed
on the software renderer first (tools/wip/windfall).

Measured (`ZoneBuilder.Nodes.cs` `Windfall`): the fallen trunk carries four thin stub limbs, two of them pointing up 0.9 to
1.4 m, on a plain tapered log beside a small stump. Fix: a tree that reads as broken or felled: a torn, splintered stump,
a heavy trunk with snapped boughs and torn bark, and the litter of a fall (or of an axe) round it.

## 7. Cats' tails need to be more flexible (2026-10-01) — FIXED (published 2026-10-02 13:50)
> "cats tails need to be more flexible"

Measured (`CritterBody.Make`, the cat): the tail is one rigid cylinder set at a fixed angle. Fix: a tail of several short
segments that curves, sways as the cat walks, lifts when it trots and curls and flicks when it sits or looks about.

## 8. Lights look better (2026-10-01) — KEEP
> "lights look better"

The night pass (firelit windows and lamps, the moonlit blue base). Nothing to fix: do not regress it.

## 9. Bloom and sun effects? (2026-10-01) — FIXED (published 2026-10-03 23:35)
> "bloom sun effects?"

What exists (`ZonePost`, `Post.shader`): bloom on lamps, windows, embers and the sun, and sun shafts when the sun is in view,
both tuned low; they evidently do not read in play. Fix: a sun that reads: a visible disc with a warm halo, glare and light
shafts when you look toward it (strongest low in the sky, through trees and at dawn and dusk), glints on water, and bloom
that shows on bright sky and firelight without washing the painted colours out. Add tour shots that face the sun at dawn,
noon and dusk so it can be judged.

## 10. Baked lighting in the caves, more atmospheric (2026-10-01) — OPEN
> "need baked lighting in the caves. more atmospheric"

The caves (Crowsfoot Hollow, the Root-Mother's Deep) are lit evenly by the zone's ambient light with a few live lights, so
they read flat. Zones are generated when they load, so Unity's lightmapper cannot bake them; the bake has to be done by the
zone builder: light from each torch, fire and glowing thing and from the mouth, blocked by the cave's own walls, with
darkness in the depths and in the creases, stored on the cave's mesh (or in a light map over its plan) and used by the cave
shader, the props and the actors alike; then haze, shafts at the mouth and a colour script per cave.

## 11. Real icons for the hot bar and the items in the bags (2026-10-01) — FIXED (published 2026-10-02 14:12)
> "need set icons for the hot bar. items in bags. not just letters and colors. can't tell what anything is"

Today every ability on the bar is three letters ("STR", "CHA", "GUA") and every item square two letters on a colour ("Mt",
"Lt", "Fd", "Wp", "Bg"). Fix: a painted icon for every ability and talent, and for every item: each ore, bar, log, herb,
hide, meat, food, potion, tool, trade bag, junk and quest item its own picture, and gear by what it is (blade, axe, mace,
staff, shield, helm, chest, gloves, legs, boots, neck, shoulders) in its own colours, with the quality still shown by the
border.

## 12. Armour too blocky; retire the block characters (2026-10-02, again "still have the bubble forms/armor/characters") — CHARACTERS DONE (real models published 2026-10-02 19:37 and 20:38); armour designs still to reshape
> "armor and weapons look good but armor is way too blocky. needs to feel flowing. time to retire the block characters"

Every figure (the player, Mira, villagers, people among the mobs) is built in code from boxes, capsules and spheres, and the
armour is fitted to those blocks. He likes the gear's designs; the bodies and the stiffness are what must go: smooth bodies,
cloth that hangs and moves (capes, robes, tabards, skirts, hair), armour that follows the form.

Chris chose to build them in code. Plan (tools/wip/characters): C1a one smooth skinned body for every person (a shaped torso,
neck, tapered limbs with elbows and knees, hands, boots; hair and face on the head bone), the regions armour covers kept as
separate renderers; C1b limb armour and held weapons skinned to the new joints, full bends for all; C2 rounder armour and
cloth that moves (tabards, skirts, capes, robes, hair). Previewed first with mesh_view.py (tools/wip/characters/preview-v2.png).

C1a and C1b were published 2026-10-02 16:58; Chris: "that still looks REALLY blocky". He then chose real models over code
("Find real models", "Quaternius free", "need females also"), and asked that the armour be refit, not redone.
**C3, real models (2026-10-02 evening):** the Quaternius kits (CC0, quaternius.com; tools/wip/characters/quaternius_import.py
brings them into Resources/Characters):
- **The kits:** Universal Base Characters for the heads, eyes, brows and hair, Modular Character Outfits - Fantasy (Peasant
  and Ranger, men's and women's) for the clothes, and the Universal Animation Library's clips for the motion.
- **ModelFigure:** builds each person from the outfit's skeleton, with the head cut at the collar, a hairstyle and a beard,
  dyed shirt, breeches and hood, skin tones as tints, and men and women by name or trade.
- **The frames (ActorVisual.Model.cs):** the old pivots ("Arm R" > "Forearm R" > "Hand R" and so on, plus head, chest and
  hips frames) ride the model's bones, scaled to its proportions. Tools, weapons, slung kit and all the worn armour keep
  their numbers and sit on the model; limb armour is slimmed to the model's limbs.
- **Poses:** sitting, swimming, sneaking, talking, gathering and dying are the kit's clips; hammering, chopping, hoeing,
  kneading, drinking, cowering and slumping put the old pose's arm angles on the model's arms.
- **Gone on models:** the old flat-board aprons, tabards, capes and the ball hoods. A ball hood becomes the outfit's hood,
  and Collectors and Wardens wear Concord white.
- **Editor capture:** FigureCapture draws every look, trade, pose and an armour kit without a build
  (hel/work/ui-captures/figures).

The kits' paid tier ($20, Quaternius) adds knight, noble and wizard outfits; that is Chris's to buy. Still to come: the armour's
own shapes (C2, rounder plate, cloth that moves), a carrying pose for loads.

## 13. More colour: high fantasy, not pale (2026-10-02) — FIXED (published 2026-10-02 15:33)
> "more colors as well seems like all is muted pallets..need high fantasy not pale"

The gear palettes (`Resources/Gear/looks.json`), villagers' clothes and much of the world sit in muted earth tones. Fix:
saturated, confident colour: jewel-toned cloth, heraldic contrasts, gold and blued steel that gleam, rarer gear richer
still; then the same eye over the villagers and the world's accents, without losing each zone's mood.

## 14. A PDF of everything done since the start (2026-10-02) — OPEN
> "give me a pdf of all items/process/builds/additions/subtractions to this whole game since we started"

## 15. A few trees are disjointed in the limb area (2026-10-02) — FIXED (published 2026-10-02 13:41)
> "a few trees are disjointed in the limb area"

Some trees' boughs do not meet their trunks: limbs float beside or above the trunk, or start part-way out of it. To find
which tree builders do it (the broadleaf and giant trees' boughs and leaf cards, the dead and pine trees' limbs), at what
sizes and on what slopes, then join every bough to its trunk (rooted inside the bark, never floating), with a test that
measures each limb's root against the trunk it belongs to.

Found (note 15): branches started on the straight line between a bowed limb's ends while the limb sags below it: the Verdant
giants' branches up to a metre over their great limbs, the dead trees' twigs and forks off theirs. They now start on the
bowed line (ZoneBuilder.LimbAt); TreeLimbTests checks every limb's root in all five zones.

## 16. The light under the sign looks like neon (2026-10-02) — FIXED (published 2026-10-02 15:33)
> "the light under the sign looks like neon. it should probably be laterns or candles"

Found: Khaven's inn sign (the second inn's, an ember pot) had four thin bars glowing at 2.2x zig-zagging across the bottom
of the board, its "cracks". Now: three short cracks on the pot itself, embers just showing, and a candle lantern hung under
the sign on a short chain (an open iron cage under a little roof, a wax candle and its flame, a small warm light that grows
after dark).

## 16. The Hush's dead tree is still broken (2026-10-03) — FIXED (published 2026-10-03 21:05)
> "this tree is still broke. im glad you screenshot it i brought it up quite some time ago." (with the Hush capture)

The dead tree's limbs were long thin spears that all climbed from the top of the bole (an antler; the grove's husks, brooms
and sharpened posts). Note 15 had put the twigs back on the limbs but not changed the limbs. Built: DeadTree's limbs start
lower, heavy, crooked in three turning lengths with forks that droop and fork again, ending in snapped stubs; the snag's top
is blunt and broken. Before/after: hel\work\world-captures\khaven-21-the-hush.png (Chris's shot was the same stop).

## 17. Hats too big (2026-10-03) — FIXED (published 2026-10-03 23:35)
> "hats too big" (with a shot of Tamsin Reed and a Salt-Mender in the Golden Cask, wide-brimmed hats)

The hat fitter (ActorVisual.Model HatFit/WearHat) grows a whole hat round the model's head, brim and crown alike, up to 1.9x,
so the crown clears the hair; the brims were sized for the old figure's big round head, so they came out as cartwheels.
Built: a brim is sized from its crown before the fit (no wider than 1.65 crowns); the crown still fits as before.

## 18. The wheel should zoom the camera in and out (2026-10-03) — FIXED (published 2026-10-03 23:35)
> "need to zoom in and out with scroll wheel"

The zoom was there, but Input System 1.20 reports a wheel notch as 1 and the code divided it by 120 (the old Windows
units), so a notch moved the camera about a centimetre. Built: either kind of value is one step; a notch is 1.2 m; the
range is 2.5 to 22 m (was 4 to 17).

## 19. Running with a staff looks awkward (2026-10-03) — FIXED (published 2026-10-03 23:35)
> "she runs awkwardly with the stave" (with a clip of the run)

A staff was held mid-shaft in the right fist and swung with the run cycle, so it whipped across the body. Built: running
out of a fight, a staff or a polearm goes on the back (its slung copy); stopping or fighting brings it to hand. Enemies
keep theirs drawn while engaged.
