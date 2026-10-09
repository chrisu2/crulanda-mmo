# Deeper talents and a fourth path for every class (decided 2026-10-09)

**Chris decided (2026-10-09, evening):** one point a level as now (the deep rows open with Phase 9's levels 16-30); the fourth paths as
proposed below; the Adit first (D5-D8), then the talents class by class, the Paladin first.

Chris, 2026-10-09: "need deeper talents and more customization (more rows, i.e. paladin can be a tank, healer, dps, or support class
with 4 rows). We need to explore something similar for all classes, albeit some classes can't tank." This is the plan to decide on.
CLASS_STATUS.md says where each class stands today. All names are GAME-ONLY and provisional.

## How talents work today (what "deeper" means)

- A tree has branches (paths); each branch has tiers 0 to 6, two or three talents a tier, most with 3 or 5 ranks.
- Tier t opens once 5 x t points are spent in that branch (TalentTree.PointsPerTier): tier 3 at 15 points, tier 6 at 30.
- You get one point a level, plus one (16 at the cap of 15). So today a level-15 character reaches tier 3 of one branch and has a
  point left over; tiers 4-6 are out of reach until the cap rises (Phase 9: levels 16-30, 31 points at 30, tier 6 of one branch).
- The Warrior's and the Druid's tiers 4-6 are designed in their files (not built); the other five classes stop at tier 2 or 3.

## Decision 1: how a level-15 character reaches the deep rows

| Option | What changes | Effect at 15 | Effect at 30 (later) |
|---|---|---|---|
| **A. Two points a level** (my pick for now) | 2 points a level (+1): 31 at 15 | reaches tier 6 of one branch: the whole depth, now | 61 points: two full branches; re-tune then (perhaps back to 1 a level past 15) |
| B. One point a level (as now) | nothing | as today: tier 3; the deep rows are built but unreachable until Phase 9 | 31 points: one full branch |
| C. One a level, one more every third level | 21 at 15 | tier 4 | 41 points |

A gives the customisation Chris asked for in the game he plays now; the deep rows are not dead content for months.

## Decision 2: the fourth path, class by class

Every class gets four paths; a class that cannot tank gets something else as its fourth. Roles after: tanks Warrior, Paladin, Druid;
healers Paladin, Druid, Archivist (new), Mira; damage all seven; support Warrior, Paladin (new), Mage, Rogue (new), Archivist;
control Ranger, Mage, Rogue, Archivist, Warrior (new).

| Class | Today | Fourth path (proposed) | Can tank? |
|---|---|---|---|
| Paladin | Oathguard tank, Judicator damage, Sanctuary healer | **Herald** (support): auras and rallies, blessings on the party, a banner that holds | yes |
| Warrior | Tank, DPS, Support | **Jailer** (control): nets, chains, hamstrings, a throw that stuns | yes |
| Druid | Barkhide tank, Thornclaw melee, Rootmend healer, Thornsong ranged | has four: depth only | yes |
| Ranger | Marksman ranged, Beastbond pet, Pathfinder control | **Skirmisher** (melee): spear and short blade, hit-and-run | no (the wolf holds a little) |
| Mage | Combustion ranged, Heatweaver control, Spellbinder support | **Cinderwright** (pets): ember familiars and a forge-golem | no |
| Rogue | Thief's Grace stealth, Shardwork poisons, Locksmith control | **Swashbuckler** (support): flanking, exposing, party haste | no |
| Archivist | the Silent Vow control, the Lull control, the Archive support | **the Cantor** (healer): songs that mend, a chorus that shields | no |

A new path means 3-4 new abilities and 16-21 talents; a deepened old one about 8 new talents.

## Decision 3: order, and where it sits against the Adit

The Adit still wants D5 (the rest), D7 and D8 (ROADMAP Phase 8). The talents are 10-20 rounds: one class a round for the design and
the new abilities, one or two more for the talents' code and tests. Proposed order: Paladin (Chris's example) first, then Warrior and
Druid (their deep rows are designed), Archivist, Ranger, Rogue, Mage. Each round ends published, so Chris can play each class as it lands.

## Size and risk

- About 60 new abilities and 200 talents in all; every implemented talent is code in its class's kit, with a test.
- Option A changes balance now: a full branch at 15 makes the sims and the mobs read softer; the elite paper fights
  (EliteBalanceTests) and the group scaling need a look once the first class lands.
- The sims use the talent trees too (SimAdventurers): they get the new paths as roles.
