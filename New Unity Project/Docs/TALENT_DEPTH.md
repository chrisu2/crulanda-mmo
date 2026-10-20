# Deeper talents and a fourth path for every class (decided 2026-10-09)

**Chris decided (2026-10-09, evening):** one point a level as now (the deep rows open with Phase 9's levels 16-30); the fourth paths as
proposed below (the Paladin's named Vanguard, not Herald: the books have no church, and their rallying figure is a general); the Adit
first (D5-D8, done), then the talents class by class, the Paladin first (its Vanguard built 2026-10-09).

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
healers Paladin, Druid, Archivist (new), Mira; damage all seven; support Warrior, Paladin (the Vanguard, built), Mage, Rogue (new), Archivist;
control Ranger, Mage, Rogue, Archivist, Warrior (new).

| Class | Today | Fourth path (proposed) | Can tank? |
|---|---|---|---|
| Paladin | Oathguard tank, Judicator damage, Sanctuary healer | **Vanguard** (support; built 2026-10-09): Plant the Colours (the party under the standard takes less), Close Ranks (barriers, Conviction for each ally rallied), Press On (the party's blows harder, Mira faster). Chris chose it over Herald (no church in the books) and Marshal (a Warrior's word). | yes |
| Warrior | Tank, DPS, Support | **Jailer** (control; built 2026-10-10): Hock Cut (a slow), Weighted Net (a root, a talent), Shackle Cast (a stun; Drag Chain hauls the foe to you); a held foe takes harder blows (Turnkey), a netted or shackled one deals less (Warden). Tiers 0-3 built, 4-6 written. | yes |
| Druid | Barkhide tank, Thornclaw melee, Rootmend healer, Thornsong ranged | has four: depth only | yes |
| Ranger | Marksman ranged, Beastbond pet, Pathfinder control | **Skirmisher** (melee; built 2026-10-11, GAME-ONLY): Spear Lunge (a bound in and a drive of the spear), Whirling Spear (every foe within 3.5 m), Feint (a talent: the next blow misses). Tiers 0-3 built, 4-6 written. | no (the wolf holds a little) |
| Mage | Combustion ranged, Heatweaver control, Spellbinder support | **Cinderwright** (pets; built 2026-10-11, GAME-ONLY): an ember hound at heel (Kindle Hound), Stoke (the Heat poured into its bites), Cinder Burst (a talent: it flares). The first plan's "forge-golem" is dropped: no machines (Chris, 2026-10-11). Tiers 0-3 built, 4-6 written. | no |
| Rogue | Thief's Grace stealth, Shardwork poisons, Locksmith control | **Swashbuckler** (support; built 2026-10-11, GAME-ONLY): Opening Cut (Exposes the target: the party's blows on it 8% harder), Rallying Flourish (the combo spent on haste: your swings and Mira's casts), Tumble (a talent: to its back, the next blow harder). Tiers 0-3 built, 4-6 written. | no |
| Archivist | the Silent Vow control, the Lull control, the Archive support | **the Cantor** (healer; built 2026-10-10, CANON-EXPANDED from the books' Song of Mending): Mending Phrase (a sung heal on the most hurt), the Song of Mending (a third song: the party near you mends 1% a second), Chorus of the Weave (a talent: barriers on everyone near). Tiers 0-3 built, 4-6 written. | no |

A new path means 3-4 new abilities and 16-21 talents; a deepened old one about 8 new talents.

## The Ranger's Skirmisher (designed 2026-10-11, after Chris's "continue working on classes/perks/talent builds"; GAME-ONLY archetype)
Close in with a short spear, strike and get out again; the bow for the opener, the wolf for the pull, the spear for the kill. No new
weapon is held: the thrusts are the figure's own jab and cross (ActorVisual.Strike), the Ranger's bow slung while it works close.
**Abilities** (on the bar after the Snare Trap: keys -, = and [):
- **Spear Lunge** (level 4): a bound of up to 8 m to the target and a drive of the spear: 1.6 times the weapon's hit. Focus 20, 10 s.
- **Whirling Spear** (level 8): the haft swung round: every enemy within 3.5 m takes 0.9 times the weapon's hit. Focus 25, 14 s.
- **Feint** (a talent, tier 2): a false thrust; the target's next blow within 3 s misses you. Focus 10, 12 s.
**Built 2026-10-11** (RangerKit, Talents/ranger.json "skirmisher", Encounter.asset ranger.spear_lunge / whirling_spear / feint, icons; tests
RangerRulesTests and RangerLoopTests). The thrusts show the figure's own jab and cross (ActorVisual.Thrust; ClassKit.OwnBlow keeps the
bow from loosing an arrow on a spear blow). Status: TESTED, not yet played by Chris.
**Talents** (tiers 0-3 built with their tests; 4-6 written, for the cap of 30):
| Tier | Talent | Ranks | Does |
|---|---|---|---|
| 0 | Long Haft | 3 | Spear Lunge reaches 1 m further per rank |
| 0 | Light Step | 3 | 1% faster per rank while an enemy is within 5 m |
| 1 | Bleeding Point | 3 | Spear Lunge opens a wound: 2% of its hit a second for 6 s, per rank |
| 1 | Quick Recovery | 2 | Disengage comes back 1.5 s sooner per rank |
| 2 | Feint | 1 | the Feint ability |
| 2 | Pack Tactics | 3 | you and the wolf on the same target within 3 s: 3% harder per rank |
| 3 | Whirl Edge | 3 | Whirling Spear hits 10% harder per rank |
| 3 | Hit and Run | 1 | after Disengage, the next Spear Lunge costs no Focus |
| 4 | Thrown Spear | 1 | (written) Spear Lunge from 15 m, thrown, when out of reach |
| 4 | Second Wind | 3 | (written) a kill in melee gives back 5 Focus per rank |
| 5 | Dancer | 3 | (written) Feint also makes the next two blows miss, per rank one more |
| 6 | Untouchable | 1 | (written) for 4 s after Whirling Spear, half of every blow is turned |

## The Rogue's Swashbuckler (designed and built 2026-10-11, after Chris's "continue working on classes/perks/talent builds"; GAME-ONLY archetype)
A duellist who opens a foe up for the party and sets its pace: the support path the Rogue lacked. No new weapon; the Rogue's blades.
**Abilities** (on the bar after Vanish: keys 8, 9 and 0):
- **Opening Cut** (level 3): a quick cut that Exposes the target for 8 s: the party's blows on it land 8% harder. A combo point. Focus 25, 6 s.
- **Rallying Flourish** (level 6): the combo points on the target spent on pace: for 10 s you swing and Mira casts 4% faster per point. Focus 20, 30 s.
- **Tumble** (a talent, tier 2): a roll past the target to its back; the next blow within 4 s lands 30% harder, as from behind (a foe
  turns to face whoever it fights, so the roll's opening counts as behind). Focus 15, 15 s.
**Talents** (tiers 0-3 built with their tests, RogueRulesTests and RogueLoopTests; 4-6 written, for the cap of 30):
| Tier | Talent | Ranks | Does |
|---|---|---|---|
| 0 | Showmanship | 3 | Opening Cut's Exposed lasts 2 s longer per rank |
| 0 | Light Blade | 3 | Opening Cut costs 5 less Focus per rank |
| 1 | From Behind | 3 | your strikes from behind a foe land 4% harder per rank |
| 1 | Long Flourish | 2 | Rallying Flourish lasts 3 s longer per rank |
| 2 | Tumble | 1 | the Tumble ability |
| 2 | Wide Open | 3 | an Exposed foe takes 2% more from the party per rank |
| 3 | Encore | 1 | a five-point Rallying Flourish also mends the party 5% |
| 3 | Dashing | 3 | Tumble's cooldown 3 s shorter per rank |
| 4 | Panache | 1 | (written) Opening Cut gives two combo points |
| 4 | Crowd Pleaser | 3 | (written) a foe dropped while Exposed gives back 10 Focus per rank |
| 5 | Riposte | 3 | (written) for 2 s after Tumble a blow at you is answered by a cut, per rank one more |
| 6 | Grand Finale | 1 | (written) Eviscerate on an Exposed foe Exposes every foe within 8 m |
Status: TESTED, not yet played by Chris.

## The Mage's Cinderwright (designed and built 2026-10-11; GAME-ONLY archetype)
The pet path, without machines: the first plan's "ember familiars and a forge-golem" became one ember hound, a fire spirit in a hound's
shape (the wolf's body, charred black, its light parts glowing like coals: ActorVisual.Beasts, the name "Ember hound"). It is the
Ranger's pet code (RangerPet, now kept by any IPetMaster): at heel, joining your fight, a party member the enemies can turn on, the
companion bar's orders. Heat is its fuel: Heat poured into the hound is Heat that cannot Overload you.
**Abilities** (on the bar after Ash Hex: keys -, = and [):
- **Kindle Hound** (level 2): the hound at your side; again, it sinks back into the embers. Mana 25.
- **Stoke** (level 6): all your Heat (20 at least) into the hound: its bites 1% harder per point for 10 s. Mana 5, 12 s.
- **Cinder Burst** (a talent, tier 2): the hound flares; every foe within 4 m of it burns (14 + 3 per level); it costs the hound 15% of its health. Mana 20, 15 s.
**Talents** (tiers 0-3 built with their tests, MageRulesTests and MageLoopTests; 4-6 written):
| Tier | Talent | Ranks | Does |
|---|---|---|---|
| 0 | Banked Coals | 3 | the hound has 8% more health per rank |
| 0 | Hot Jaws | 3 | its bites burn 10% harder per rank |
| 1 | Kinship | 3 | each Ember Bolt that lands mends the hound 2% per rank |
| 1 | Long Stoke | 2 | Stoke lasts 3 s longer per rank |
| 2 | Cinder Burst | 1 | the Cinder Burst ability |
| 2 | Shared Heat | 3 | each of its bites builds 1 Heat per rank |
| 3 | White Coals | 1 | stoked from 60 Heat, it takes 30% less while it lasts |
| 3 | Wide Burst | 3 | Cinder Burst reaches 1 m further per rank |
| 4 | Twin Embers | 1 | (written) a second, smaller hound at half strength |
| 4 | Ash Coat | 3 | (written) the hound takes 5% less per rank |
| 5 | Rekindle | 1 | (written) once a fight, a fallen hound kindles again at a third of its health |
| 6 | The Great Hound | 1 | (written) Stoke from 100 Heat: the hound twice its size for 12 s, its bites and bursts half again as hard |
Status: TESTED, not yet played by Chris.

## Decision 3: order, and where it sits against the Adit

The Adit still wants D5 (the rest), D7 and D8 (ROADMAP Phase 8). The talents are 10-20 rounds: one class a round for the design and
the new abilities, one or two more for the talents' code and tests. Proposed order: Paladin (Chris's example) first, then Warrior and
Druid (their deep rows are designed), Archivist, Ranger, Rogue, Mage. Each round ends published, so Chris can play each class as it lands.

## Size and risk

- About 60 new abilities and 200 talents in all; every implemented talent is code in its class's kit, with a test.
- Option A changes balance now: a full branch at 15 makes the sims and the mobs read softer; the elite paper fights
  (EliteBalanceTests) and the group scaling need a look once the first class lands.
- The sims use the talent trees too (SimAdventurers): they get the new paths as roles.
## The tiers at 20 (the cap of 20, Chris 2026-10-16: "raise top level to 20")
At 20 a character has 21 points (level + 1). A tier-t node needs 5t points already in its branch, so a single-branch build fills tiers
0-3 (20 points) and its 21st point goes either in tier 3's spare ranks or in a tier-4 node. Every branch of every class now holds 21 or
more ranks in tiers 0-3, so a level-20 character can spend all 21 in one branch (TalentDataTests.At_twenty_every_branch_can_spend_21_in_itself).
The plan was tools/wip/talents19.py (written for 19, its extra ranks in tier 3's free columns); 47 nodes built 2026-10-16 (GAME-ONLY names,
each about 3% a rank at most, 5% on a conditional one-rank node), every one with code in its kit:
| Class | New nodes (tier 3 unless said) |
|---|---|
| Archivist | Long Stillness (the Vow's ward +1 s a rank), Long Echo (Echo Bind +2 s a rank), Half-Waking (a mob woken early hits 3% softer a rank, 6 s) |
| Cleric | Trimmed Wick, Lamp Held Low, Even Breath, Answering Hills, Burning Word, Ashes Returned, Patient Ward, The Shrine Holds; Renewing Ward (tier 1, was written) |
| Mage | Even Flame, Kept Coals, Thick Smoke, Open Hands; Ossuary Coat (was tier 4, written: 3% less a rank while the thrall stands) |
| Paladin | Iron Oath (2% less under the Ward), Sentence Passed (Judgement 3%), Shared Mercy (Lay On mends the party 3% a rank) |
| Ranger | Practised Draw, Scarred Hide, Shared Hunt, Lasting Mark; Catch Your Breath (was the written tier-4 Second Wind: 5 Focus a rank on a kill within 5 m) |
| Rogue | Gone Clean, Unremarked, Spare Motion, Close Work, Practised Hands, Pocket of Dust; Crowd Pleaser (was tier 4, written: 10 Focus a rank when an Exposed foe falls) |
| Salt-Mender | Ready Salt (Calcify 2 s sooner a rank) |
| Scoundrel | Old Grudge, Collected, Honeyed Words, Rude Awakening, Second Chorus, Greased Palms |
| Shaman | Soft Rain, Spring Tide, Low Clouds, Heavy Sky, Bedrock, Stone-Call, Far Wind, Clinging Mist |
| Warrior, Druid | nothing new: 23-26 ranks a branch already |
The written tiers 4-6 stay written for the cap of 30 (a tier-4 capstone of one rank would also take the 21st point; none is needed now).
Status: written and compile-checked (Roslyn), not yet run in Unity; OPEN until Chris plays a level-20 build.
