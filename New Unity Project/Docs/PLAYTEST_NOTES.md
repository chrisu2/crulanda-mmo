# Chris's playtest notes

Chris jots these down as he plays. Each note keeps his words, what was measured, and what happened to it.
Status: OPEN (not started), PLANNED (design decided), BUILT (code and tests written; not yet run in Unity, seen or published),
FIXED (published; says which publish).

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

## 2. Elites are too easy for the loot they give (2026-10-01) — BUILT (branch fix/n23-social-elites)
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

## 3. Mobs are not social: they pull one at a time (2026-10-01) — BUILT (branch fix/n23-social-elites)
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
