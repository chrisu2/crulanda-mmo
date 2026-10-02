# Chris's playtest notes

Chris jots these down as he plays. Each note keeps his words, what was measured, and what happened to it.
Status: OPEN (not started), PLANNED (design decided), FIXED (published; says which publish).

## 1. The bandit camp is way too close to the village (2026-10-01) — OPEN
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

## 2. Elites are too easy for the loot they give (2026-10-01) — OPEN
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

## 3. Mobs are not social: they pull one at a time (2026-10-01) — OPEN
> "some mobs need to be more social. i can pull them easy 1 at a time even when they stand next to each other"

Measured (`EncounterEnemy.Update`): every mob notices the player by itself, within 5 m and with a clear line; nothing links
it to its neighbours. Hitting or walking up to one of four wolves, or one of Caddock's guards, brings that one alone.

What the fix needs: social aggro by kind. When a mob joins a fight, others of its camp within reach join too: pack beasts
(wolves, hounds) come together; people (deserters, cultists, Concord) call out and bring those in earshot, with a short
delay and a shout so it reads; solitary beasts (boar, stags) stay single. Sneaking (Ctrl) should still let a careful player
peel the edge of a camp. A boss always fights with its guards.

## 4. The sheep look like bugs on sticks (2026-10-01) — OPEN
> "fix the sheep. looks like bugs on sticks"

Measured (`CritterBody.Make`, case "sheep"): a sheep is one pale ellipsoid (0.75 x 0.6 x 1.0 m), a black ball for a head and
four thin black cylinders 8 cm thick and 47 cm long. From any distance that is a white tick on black legs.

What the fix needs: a sheep that reads as a sheep in the painted style from 5 m and from 30 m: a deep woolly fleece built of
lumps, short sturdy legs with wool to the knee and dark hooves, a dark wedge of a face with a muzzle and ears out sideways,
a wool cap, a tail; a head that goes down to the grass when it grazes. Then a line-up shot of every animal in the capture
tour, so the others can be judged the same way.

## 5. Less green in the ore (2026-10-01) — OPEN
> "less green in the ore"

The copper seams' verdigris: after the first fix (beads pressed flat) seven lumps in ten still carry a bright green patch
(`OreSeam`, fleck (.30, .55, .46)), so a seam reads as orange and green. Fix: a hint of patina only: fewer patches, smaller,
duller and nearer the rock's own colour.

## 6. Lumber trees: just four or five sticks standing up (2026-10-01) — OPEN
> "lumber trees . trunk just has 4-5 stick stickup up. make it more broken/chopped down looking"

Measured (`ZoneBuilder.Nodes.cs` `Windfall`): the fallen trunk carries four thin stub limbs, two of them pointing up 0.9 to
1.4 m, on a plain tapered log beside a small stump. Fix: a tree that reads as broken or felled: a torn, splintered stump,
a heavy trunk with snapped boughs and torn bark, and the litter of a fall (or of an axe) round it.

## 7. Cats' tails need to be more flexible (2026-10-01) — OPEN
> "cats tails need to be more flexible"

Measured (`CritterBody.Make`, the cat): the tail is one rigid cylinder set at a fixed angle. Fix: a tail of several short
segments that curves, sways as the cat walks, lifts when it trots and curls and flicks when it sits or looks about.

## 8. Lights look better (2026-10-01) — KEEP
> "lights look better"

The night pass (firelit windows and lamps, the moonlit blue base). Nothing to fix: do not regress it.

## 9. Bloom and sun effects? (2026-10-01) — OPEN
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

## 11. Real icons for the hot bar and the items in the bags (2026-10-01) — OPEN
> "need set icons for the hot bar. items in bags. not just letters and colors. can't tell what anything is"

Today every ability on the bar is three letters ("STR", "CHA", "GUA") and every item square two letters on a colour ("Mt",
"Lt", "Fd", "Wp", "Bg"). Fix: a painted icon for every ability and talent, and for every item: each ore, bar, log, herb,
hide, meat, food, potion, tool, trade bag, junk and quest item its own picture, and gear by what it is (blade, axe, mace,
staff, shield, helm, chest, gloves, legs, boots, neck, shoulders) in its own colours, with the quality still shown by the
border.

## 12. Armour too blocky; retire the block characters (2026-10-02) — OPEN (needs Chris's choice of approach)
> "armor and weapons look good but armor is way too blocky. needs to feel flowing. time to retire the block characters"

Every figure (the player, Mira, villagers, people among the mobs) is built in code from boxes, capsules and spheres, and the
armour is fitted to those blocks. He likes the gear's designs; the bodies and the stiffness are what must go: smooth bodies,
cloth that hangs and moves (capes, robes, tabards, skirts, hair), armour that follows the form.

## 13. More colour: high fantasy, not pale (2026-10-02) — OPEN
> "more colors as well seems like all is muted pallets..need high fantasy not pale"

The gear palettes (`Resources/Gear/looks.json`), villagers' clothes and much of the world sit in muted earth tones. Fix:
saturated, confident colour: jewel-toned cloth, heraldic contrasts, gold and blued steel that gleam, rarer gear richer
still; then the same eye over the villagers and the world's accents, without losing each zone's mood.

## 14. A PDF of everything done since the start (2026-10-02) — OPEN
> "give me a pdf of all items/process/builds/additions/subtractions to this whole game since we started"
