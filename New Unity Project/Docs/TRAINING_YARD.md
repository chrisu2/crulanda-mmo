# Oakhaven's training yard (GAME-ONLY)

## Chris's briefs (2026-10-13, verbatim)

"put a few test dummies in town so the player can test their builds maybe just one for dps and one for heals. maybe near the blacksmith or
training grounds/area" / "put out in a field near town" / "these dummies should be targetable and hitable / healable of course" / "just make
the heal dummy party aligned?" / "or target aligned if solo" / "why is one in the river? sparring area needs to resemble something like
this? try the small props pack in downloads to help." (with two reference pictures: a fenced sparring yard with tents, banners, dummies,
weapon racks and crates; a training court with archery targets in a row; references for the layout only).

## What it is

- **Where:** east-north-east of the Great Oak, centred at (58, 26), 22 m across and 16 m deep, its gate facing the town (rotation 245.9).
  Chosen by a search that kept 8 m or more from the creek and the ponds (the first field stood on the creek's bank), clear of roads, props,
  fields and groves; 9 m from a road. A map landmark, "Training yard".
- **The yard** (`ZoneBuilder.TrainingYard`, zone prop `training_yard`, and a clearing of the same name for its trampled, level floor): a
  closed ring of plank fence panels (the Megapack's, fitted end to end, 1.3 m high, solid) with a gate toward the town, two tents at the
  back, banners at the gate and corners and pennants along the sides, dummies and mannequins down the left, archery butts down the right,
  three weapon stands, crates and a barrel, a healer's corner (stool, bucket, basin).
- **The training dummy** (zone prop `training_dummy`, `EncounterSession.Training`): a foe you target and strike, at your level, no armour,
  5,000,000 health, never moves or strikes, whole again after 5 quiet seconds.
- **The mending dummy** (zone prop `mending_dummy`): a friend you click and heal; it loses 4% a second while you are within 25 m (never below
  a fifth) and every healer's party counts it within 40 m (party aligned, so solo it is the one healed).
- Striking or mending them counts as a fight, so the meter (/meter, /meter reset) times it.

## Assets

- The Small Props Pack (RG Poly, CC0; `Resources/Props/SmallProps`, licence beside it): the archery butts, the mannequins, the healer's
  things. One atlas material (`SmallPropsAtlas.mat`) applied to its models when they are placed (`ZoneBuilder.ModelProp`).
- The Megapack (tents, the outpost dummy, an arrow target) and the Fantasy kit (banners, the second dummy, weapon stands, peg rack).

## What Chris's looks caught (2026-10-13)

- One dummy on the creek's bank (the spot search never checked water): the yard moved to dry ground; a test checks both dummies.
- Two archery targets and two dummies upside down: the Small Props Pack's "Unity FBX" models have their vertices turned half round X
  (compared against its glTF copies, which are always Y up); all but PracticeTarget_1_B now get a half turn (`ZoneBuilder.PropTurns`).
- Not closed in like the pictures: the rail fence became a ring of plank panels.
- Found in the frames: the Fantasy kit's Banner_2 and peg rack are made Z up and came in lying flat and many times too big; Banner_2 is
  turned like Banner_1, the peg rack (a 35 cm wall board) is gone.

## Tests and frames

- `TrainingFieldTests` (PlayMode): the dummy struck and whole again; the mending dummy hurt and healed as one of the party; selecting it;
  both on dry ground inside the yard.
- `--crulanda-training-capture <folder> --crulanda-class class.cleric --crulanda-zone zone.oakhaven --crulanda-temp-save`: tf-01..tf-05.
  Last frames: `D:\crulanda-work\ui-captures\training-20261013-1417` (TESTED: TrainingFieldTests 4/4, EditMode 462/462; not yet seen in
  play by Chris).

## Chris, 2026-10-13 ~15:02 (brief, verbatim; not built yet)

"also have sims wonder into the fighting arena (dummies) to practice and improve their dps"
