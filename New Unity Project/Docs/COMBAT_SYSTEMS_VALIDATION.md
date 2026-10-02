# Combat systems / Phase 1 completion

Unity 6000.6.3f1. This report covers the shared systems needed to leave the combat-sandbox phase.

## Changes
- Added Crulanda.Combat: CombatMath, Combatant, DerivedStatRules/Calculator/Controller,
  StatusEffectDefinition and StatusEffectRuntime.
- Guard uses a data-defined timed status. Damage uses shared armor/multiplier resolution.
- Primary attributes, level and equipment feed derived health, attack power, spell power and armor.
- Modifier batches invalidate all related caches before events fire. Multi-stat effects apply together.
- Reapplication refreshes a status ID without stacking magnitude; expiry/death/disable/rebuild remove its modifiers.
- Actor lifecycle notifications let derived-stat bindings reconnect after reinitialization.
- Enemy proximity threat is elapsed-time based; damage threat uses actual applied damage.

## Validation coverage
EditMode covers stat formulas, mitigation, status refresh/expiry/source ownership, invalid definitions,
consistent batched reads and frame-rate-independent proximity threat.
PlayMode covers timed protection, death cleanup, derived-stat updates/reinitialization, lethal stat changes,
atomic multi-stat application and the full encounter/save/load/movement regression.
Final results: 130/130 EditMode and 15/15 PlayMode tests passed; Windows player build succeeded. Reports are in ValidationResults/combat-*. Validation ran in Unity 6000.6.3f1 on a synchronized project copy, leaving the open working editor untouched.

## Phase boundary
This completes the reusable Phase 1 MVP. It does not claim a finished RPG framework.
Phase 2 focuses on distinct class loops, ability ownership/unlocks, resources, trainers and basic talents.
Generic vendors/inventory expansion, full parties and offscreen simulation remain in later phases.

## Compatibility and limitations
No save schema change. Existing save IDs, progression and equipment remain valid.
Temporary effects are cleared on load; long-lived persistent buffs require a future schema/policy.
Status effects currently support stat modifiers and incoming-damage multipliers. Periodic effects,
CC/dispels and advanced stacking are future class-driven extensions.
Combat remains deliberately paced: regular fights ~21/24 seconds; veteran ~35 seconds after corrected threat.
Visual art/HUD remain provisional. No user replay is needed for this technical milestone.


## Social aggro and elites (playtest notes 2 and 3, 2026-10-01)

**Status: written and compiled offline. Not yet run in Unity (EditMode or PlayMode) and not yet seen on screen.** The numbers
in the tables below come from running the game's own code (`EliteBalance.Fight`, `EncounterEnemy.MobHealth` / `MobHit`,
`EliteMoves`, `ItemDatabase.Get`, `CombatMath`) in an offline harness with Encounter.asset's ability numbers; `EliteBalanceTests`
makes the same calls in the editor and logs the same table (`ELITE_BALANCE`).

### Social aggro (note 3)
- Every camp has a kind (`SocialAggro.KindFor`): what its data says (`social` on the camp: `pack`, `call`, `solitary`) or, when
  that is empty, by its look. Wolves and hounds, Weave-Eaters, spiders and briars **pack**. Boar and stags are **solitary**.
  Everyone with a voice **calls**: deserters, Sandthrone riders, the Concord, cultists, Hollow Men, the pale, withered Keepers.
  No zone file needed a `social` entry: all 61 camps come out right by look (`SocialRulesTests` holds the table by tag).
- When a camp mob joins a fight (it noticed the player, was hit, or sprang from the grass) `EncounterSession.RaiseAlarm` runs once:
  - **pack:** packmates of its camp within 9 m join at once, silently. Ambushers in the grass spring out with it.
  - **call:** it shouts (a line in the chat with its name, a word over its head; one chat line at most every 1.5 s) and those
    within 12 m come after a beat of 1.2 s, each 0.15 s after the last. Its own camp hears, and so do kin in a neighbouring
    camp (`SocialAggro.Kin`: deserters with their king, toll-guards with pickets and outriders).
  - **solitary:** nobody comes.
  - **sneaking** (Ctrl): when the mob only noticed the player (it was not hit) the reach is 3.5 m for both kinds, less than
    the 5 m a mob notices from, so the edge of a camp can be peeled. A blow from stealth is a plain pull.
  - **an elite's guards** always come, from 16 m, however it was pulled: its own camp and the camps paired with it. Pairing is
    by the data (`guards` on the elite's camp: guard camp names, comma-separated, or `none`) or, when that is empty, every
    non-elite camp whose edge is within 8 m of the elite's centre. Today that pairs Caddock with the King's guard, Hesk with
    the Store Caves, the Ash-Deacon with the Ash-pit cultists, Greyheart with the Greying and the Root-Warden with the Heart's
    withered. The Brood Weave-Eater's one broodmate (its own camp) comes the same way.
  - **a guard does not bring its elite** unless it stands right beside it (`SocialAggro.LordReach`, 3.5 m). A guard's or a
    kinsman's alarm brings fellow guards and kin as usual; the elite stays where it is. So the careful way is the classic one:
    clear the guards as a pull of their own, then fight the elite. Pull the elite first and every guard within 16 m comes. An
    elite still notices the player by itself within 5 m, and an elite that is drawn in brings the rest of its guards.
  - Nobody answers through rock: the two must be within the reach in a straight line, on much the same level, and the walk
    between them (navmesh path) no longer than twice the reach and 4 m.
- A mob that answers holds threat on **whoever pulled** (`EncounterSession.JoinThreat`: at least 30, and three times what one
  of Mira's heals draws at the player's level). A heal's threat (half of what it healed) is now **shared out among the mobs in
  the fight** (`EncounterSession.HealThreat`): given to each in full, as it was, her second heal turned every untouched mob of
  a pull on her, because the puller's blows land on one mob at a time. One mob alone takes all of it, as before. With four
  mobs at level 1 a joiner holds 63 on the puller and each heal adds 5 for Mira; at level 13 it holds 177 and each heal adds 15
  of four or 30 of two. A mob nobody touches for most of a minute while she heals does turn in the end. A mob that answers
  raises no alarm of its own, so a pull does not run through a whole camp. An elite that is drawn in still brings its guards.
- **Leash and reset** (`FightGroup`): the mobs of one pull are linked. They leash from where the pull began (the first mob's
  home), by their target's distance only, so those who came from across the camp are not sent back for having come. When the
  leash breaks they all heal and go home together ("... and the rest break off and go back to their places."), and for 3 s on
  the way they notice nobody and answer no call (a blow still turns them). A mob that cannot reach its target for 4 s gives up
  alone. A mob fighting alone keeps the old leash (its own home).
- Story enemies (the Concord collectors, the Khaven outriders) are not in camps and are not linked.

### Elites (note 2)
- **Numbers.** A camp elite was a normal mob with 2.2 times the health and 1.4 times the hit. It now has 5.5 times the health
  (`EncounterEnemy.EliteHealth`) and 3 times the hit, plus 9% of that hit for each level past the first (`EliteHit`,
  `EliteHitPerLevel`: a player's armour grows with level, a mob's swing barely does). A dungeon's end boss (Caddock, the Hollow
  Root-Warden) has a quarter more health again and a tenth more hit (`EliteMoves.BossHealth`, `BossHit`). Normal mobs, story
  enemies and their veterans keep their numbers.
- **The heavy blow.** Every 9 to 12 s, at a swing, the elite draws back instead: it stops, leans back and turns with its target
  for the wind-up, a mark lies on the ground out to the blow's reach and fills (`BlowMark`), the chat says "... draws back:
  <move>! Step out of the mark, or raise Guard.", the move's name floats over it, and the target frame shows a cast bar in the
  swing bar's place with the seconds left (`EncounterHud.DrawBlowBar`). When the wind-up ends the blow lands on whoever it is
  fighting if they are still inside the mark, for the multiple below of its swing. It goes through the class kit like any hit:
  armour, Guard (0.4), Heartwood Brace, barriers and Intercept all work on it. Outside the mark it misses ("You step clear of
  ..."). The reach is always past the player's own 3.2 m, so the answer is to step away, not to stand at arm's length.
- **The enrage.** At 30% health (35% for the two bosses), once a fight: its swings come 1.54 times as often (1.67 for the
  bosses), a line of its own in the chat, "Enraged!" over it, "ENRAGED" in the target frame, and it looms a little larger.
- **The call.** At 60% health, once a fight: its guards and its kin within 22 m who are not yet in the fight come after the
  beat, as part of its group. Three calls carry further, to the camp of kin that is there to hear them: Old Whitefoot's howl
  32 m (his pack of three), the Grey Sexton's knell 40 m (the Gloom Creek hollows) and the Sandthrone captain's shout 50 m
  (the Toll-gate guards). Every guard who hears comes; of other kin the Sexton and the captain bring the nearest two and no
  more (`EliteMove.callMost`), because a whole camp of four or five on top of an elite is not a fight a Warrior with Mira
  wins. Who answers today: Hesk's call brings the Deep Stair watch (two), Whitefoot's his pack (three), the Sexton's two
  hollows, the captain's two toll-guards; for Caddock, the Brood Weave-Eater, the Ash-Deacon, Greyheart and the Root-Warden it
  brings any guard not yet in the fight. **The Pale Reckoner has nobody to call:** no pale camp stands in Khaven today, so its
  tally is never heard until one does (`SocialRulesTests.An_elites_call_has_someone_to_answer_it` names it as the one
  exception). Nothing is said when nobody can answer. Old Scree-Tusk and Old Ninebranch are solitary beasts and call nobody.
  Unseen: whether the walk from the Toll-gate up to the eyrie is short enough for the guards to answer (the walk may be no
  longer than twice the reach and 4 m, the climb no more than half the reach).
- **Overmatched.** For each level a camp elite stands above the player (five at most) it hits 12% harder and takes 6% less
  from the party (`EncounterEnemy.OverHit`, `OverTough`). Normal mobs never do. This is what keeps an elite two levels up from
  being a solo kill at every level, not only the low ones.
- **Mira keeps pace.** Her heal was 42 at every level and her health 130. Each player level past the first now adds 15% of her
  heal and 22 health (`HealerCompanion.HealPerLevel`, `HealthPerLevel`; her mana and its cost are unchanged). Without this she
  was a third of a level-13 player's health in a whole fight and could not be what an elite "wants".
- A reset (leash, evade, death of the player, respawn) clears the wind-up, the enrage and the call.

| Elite | Level | Move | Wind-up | Blow | Reach | Every | Enrage | Call |
|---|---|---|---|---|---|---|---|---|
| Caddock, the Bandit King (dungeon end boss) | 5 | The King's Due | 2 s | x4.3 | 3.8 m | 11 s | at 35%, swings x1.67 as often | at 60%, 22 m |
| Quartermaster Hesk | 4 | Short Weight | 1.8 s | x3.8 | 3.4 m | 10 s | at 30%, swings x1.54 as often | at 60%, 22 m |
| Old Whitefoot | 3 | Throat-Lunge | 1.5 s | x3.5 | 4.2 m | 9 s | at 30%, swings x1.54 as often | at 60%, 32 m |
| The Grey Sexton | 5 | Gravedigger's Swing | 2.2 s | x4.3 | 3.8 m | 11 s | at 30%, swings x1.54 as often | at 60%, 40 m, two at most |
| The Pale Reckoner | 7 | The Reckoning | 2.4 s | x4.6 | 3.6 m | 12 s | at 30%, swings x1.54 as often | at 60%, 22 m (no pale camp in Khaven today: nobody answers) |
| Sandthrone captain | 8 | Eyrie Cleave | 1.7 s | x3.8 | 3.6 m | 10 s | at 30%, swings x1.54 as often | at 60%, 50 m, two at most |
| Old Scree-Tusk | 8 | Tusk-Heave | 1.8 s | x4 | 4.4 m | 10 s | at 30%, swings x1.54 as often | none (a solitary beast) |
| Brood Weave-Eater | 10 | Fraying Lash | 2 s | x3.8 | 4 m | 10 s | at 30%, swings x1.54 as often | at 60%, 22 m |
| The Ash-Deacon | 10 | Cinder Benediction | 2.2 s | x4 | 3.8 m | 11 s | at 30%, swings x1.54 as often | at 60%, 22 m |
| Greyheart | 13 | Felling Stroke | 2.3 s | x4.3 | 4 m | 11 s | at 30%, swings x1.54 as often | at 60%, 22 m |
| Old Ninebranch | 13 | Nine-Tine Toss | 1.6 s | x3.8 | 4.4 m | 9 s | at 30%, swings x1.54 as often | none (a solitary beast) |
| The Hollow Root-Warden (dungeon end boss) | 13 | The Root's Weight | 2.2 s | x4.6 | 4.2 m | 11 s | at 35%, swings x1.67 as often | at 60%, 22 m |

All names GAME-ONLY. Wind-up: seconds between the tell and the blow. Blow: multiple of its swing. Every: seconds between blows.

### The paper fight
`EliteBalance.Fight` steps a fight a tenth of a second at a time: the player's auto-attacks and base abilities (no talents),
the mobs' swings, the elite's heavy blow and enrage, Mira's heal (under 78% health) and bolt (above 80%), and the game's damage
rule. **Careful** means every heavy blow is answered: a Warrior raises Guard when it is ready and steps out when it is not, a
Druid steps out; stepping out costs the time out of reach. **Careless** means standing in all of it and never guarding. The
player wears on-curve gear: one generated uncommon piece in every slot at the player's level, suffix stats averaged over sixty
seeds a slot. Left out: threat (every mob stays on the player; with Mira a Warrior pays for Challenge), movement, talents (ten
points by level 10 make the real player stronger), and a Druid changing form. The elite's call is in it where a table says so:
those who answer stand out of the fight until the elite is at 60%, then come after the beat and their walk.

| Player level (Warrior, geared) | Health | Armour | Attack power | Mira's heal |
|---|---|---|---|---|
| 1 | 251 | 24 | 23 | 42 |
| 3 | 301 | 62 | 30 | 55 |
| 5 | 422 | 92 | 46 | 67 |
| 8 | 579 | 149 | 64 | 86 |
| 10 | 648 | 189 | 78 | 99 |
| 13 | 812 | 245 | 100 | 118 |

**Warrior**

| Elite | Level | Health | Swing | Alone, careless | Alone, careful | Mira, careless | Mira, careful | Two levels below: alone, careful | Two below: Mira, careful |
|---|---|---|---|---|---|---|---|---|---|
| Caddock, the Bandit King | 5 | 1650 | 80 | dies 18 s, mob 69% left, took 431 | dies 28 s, mob 51% left, took 430 | dies 39 s, mob 32% left, took 999 | wins 56 s, 19% left, took 944 | dies 13 s, mob 87% left, took 312 | dies 33 s, mob 65% left, took 746 |
| Quartermaster Hesk | 4 | 1155 | 59 | dies 20 s, mob 60% left, took 421 | dies 32 s, mob 30% left, took 377 | dies 45 s, mob 7% left, took 941 | wins 45 s, 75% left, took 573 | dies 15 s, mob 78% left, took 313 | dies 37 s, mob 47% left, took 674 |
| Old Whitefoot | 3 | 842 | 47 | dies 19 s, mob 55% left, took 349 | dies 34 s, mob 22% left, took 309 | dies 43 s, mob 1% left, took 785 | wins 40 s, 68% left, took 425 | dies 15 s, mob 77% left, took 277 | dies 39 s, mob 36% left, took 602 |
| The Grey Sexton | 5 | 1320 | 73 | dies 23 s, mob 50% left, took 554 | dies 31 s, mob 31% left, took 426 | dies 42 s, mob 8% left, took 983 | wins 43 s, 80% left, took 620 | dies 16 s, mob 79% left, took 342 | dies 38 s, mob 49% left, took 780 |
| The Pale Reckoner | 7 | 1650 | 103 | dies 23 s, mob 52% left, took 670 | dies 29 s, mob 41% left, took 514 | dies 41 s, mob 15% left, took 1137 | wins 48 s, 52% left, took 877 | dies 16 s, mob 75% left, took 452 | dies 39 s, mob 40% left, took 1026 |
| Sandthrone captain | 8 | 1815 | 119 | dies 19 s, mob 59% left, took 604 | dies 36 s, mob 21% left, took 612 | dies 44 s, mob 5% left, took 1352 | wins 45 s, 76% left, took 829 | dies 19 s, mob 72% left, took 505 | dies 49 s, mob 27% left, took 1132 |
| Old Scree-Tusk | 8 | 1543 | 119 | dies 20 s, mob 51% left, took 624 | dies 33 s, mob 16% left, took 605 | wins 40 s, 12% left, took 1200 | wins 37 s, 73% left, took 672 | dies 20 s, mob 67% left, took 515 | dies 45 s, mob 21% left, took 1125 |
| Brood Weave-Eater | 10 | 1823 | 156 | dies 20 s, mob 50% left, took 682 | dies 33 s, mob 14% left, took 668 | wins 37 s, 35% left, took 1212 | wins 37 s, 77% left, took 744 | dies 20 s, mob 64% left, took 579 | dies 45 s, mob 16% left, took 1267 |
| The Ash-Deacon | 10 | 2145 | 156 | dies 23 s, mob 50% left, took 758 | dies 33 s, mob 27% left, took 650 | wins 45 s, 5% left, took 1407 | wins 45 s, 80% left, took 921 | dies 23 s, mob 63% left, took 671 | dies 49 s, mob 20% left, took 1404 |
| Greyheart | 13 | 2640 | 221 | dies 23 s, mob 48% left, took 934 | dies 33 s, mob 25% left, took 822 | wins 45 s, 4% left, took 1721 | wins 43 s, 83% left, took 1086 | dies 23 s, mob 63% left, took 840 | dies 46 s, mob 26% left, took 1563 |
| Old Ninebranch | 13 | 2244 | 221 | dies 22 s, mob 39% left, took 870 | dies 35 s, mob 7% left, took 822 | wins 37 s, 32% left, took 1497 | wins 37 s, 64% left, took 886 | dies 24 s, mob 54% left, took 789 | dies 51 s, mob 6% left, took 1673 |
| The Hollow Root-Warden | 13 | 3300 | 243 | dies 23 s, mob 59% left, took 1068 | dies 33 s, mob 40% left, took 876 | dies 42 s, mob 27% left, took 1812 | wins 56 s, 32% left, took 1612 | dies 23 s, mob 71% left, took 891 | dies 49 s, mob 37% left, took 1738 |

**Druid, Barkhide** (the tank form: 30 armour, a fifth more health, Bough Strike, Barkmend from level 4)

| Elite | Level | Health | Swing | Alone, careless | Alone, careful | Mira, careless | Mira, careful | Two levels below: alone, careful | Two below: Mira, careful |
|---|---|---|---|---|---|---|---|---|---|
| Caddock, the Bandit King | 5 | 1650 | 80 | dies 33 s, mob 52% left, took 670 | wins 76 s, 9% left, took 900 | dies 65 s, mob 2% left, took 1531 | wins 71 s, 92% left, took 828 | dies 25 s, mob 80% left, took 364 | dies 61 s, mob 51% left, took 884 |
| Quartermaster Hesk | 4 | 1155 | 59 | dies 32 s, mob 43% left, took 559 | wins 64 s, 43% left, took 551 | wins 55 s, 75% left, took 929 | wins 60 s, 91% left, took 493 | dies 27 s, mob 73% left, took 352 | dies 73 s, mob 23% left, took 836 |
| Old Whitefoot | 3 | 842 | 47 | dies 27 s, mob 50% left, took 364 | dies 50 s, mob 12% left, took 360 | wins 50 s, 16% left, took 728 | wins 51 s, 84% left, took 384 | dies 27 s, mob 68% left, took 304 | dies 62 s, mob 19% left, took 684 |
| The Grey Sexton | 5 | 1320 | 73 | dies 38 s, mob 32% left, took 753 | wins 62 s, 47% left, took 660 | wins 53 s, 68% left, took 1092 | wins 58 s, 91% left, took 594 | dies 28 s, mob 72% left, took 376 | dies 73 s, mob 29% left, took 893 |
| The Pale Reckoner | 7 | 1650 | 103 | dies 39 s, mob 34% left, took 927 | wins 65 s, 44% left, took 780 | wins 56 s, 54% left, took 1379 | wins 60 s, 86% left, took 702 | dies 50 s, mob 44% left, took 798 | wins 86 s, 52% left, took 1482 |
| Sandthrone captain | 8 | 1815 | 119 | dies 44 s, mob 23% left, took 1121 | wins 63 s, 60% left, took 817 | wins 53 s, 87% left, took 1336 | wins 59 s, 87% left, took 731 | dies 62 s, mob 35% left, took 944 | wins 88 s, 65% left, took 1534 |
| Old Scree-Tusk | 8 | 1543 | 119 | dies 38 s, mob 20% left, took 986 | wins 52 s, 62% left, took 645 | wins 45 s, 63% left, took 1243 | wins 50 s, 88% left, took 645 | dies 61 s, mob 23% left, took 944 | wins 76 s, 71% left, took 1298 |
| Brood Weave-Eater | 10 | 1823 | 156 | dies 37 s, mob 18% left, took 1097 | wins 52 s, 53% left, took 784 | wins 45 s, 66% left, took 1381 | wins 49 s, 93% left, took 686 | dies 64 s, mob 11% left, took 1260 | wins 71 s, 89% left, took 1400 |
| The Ash-Deacon | 10 | 2145 | 156 | dies 41 s, mob 24% left, took 1127 | wins 62 s, 50% left, took 980 | wins 53 s, 66% left, took 1568 | wins 59 s, 79% left, took 882 | dies 64 s, mob 25% left, took 1260 | wins 80 s, 71% left, took 1750 |
| Greyheart | 13 | 2640 | 221 | dies 40 s, mob 24% left, took 1408 | wins 59 s, 56% left, took 1062 | wins 50 s, 77% left, took 1703 | wins 56 s, 83% left, took 944 | dies 72 s, mob 13% left, took 1620 | wins 81 s, 73% left, took 2025 |
| Old Ninebranch | 13 | 2244 | 221 | dies 39 s, mob 15% left, took 1380 | wins 49 s, 64% left, took 885 | wins 45 s, 82% left, took 1663 | wins 49 s, 89% left, took 885 | dies 64 s, mob 11% left, took 1539 | wins 68 s, 89% left, took 1620 |
| The Hollow Root-Warden | 13 | 3300 | 243 | dies 38 s, mob 42% left, took 1544 | wins 73 s, 33% left, took 1495 | wins 64 s, 21% left, took 2557 | wins 70 s, 80% left, took 1430 | dies 62 s, mob 41% left, took 1513 | wins 100 s, 25% left, took 2848 |

**Druid, Thornclaw** (the melee form: Rake, Tear, faster swings)

| Elite | Level | Health | Swing | Alone, careless | Alone, careful | Mira, careless | Mira, careful | Two levels below: alone, careful | Two below: Mira, careful |
|---|---|---|---|---|---|---|---|---|---|
| Caddock, the Bandit King | 5 | 1650 | 80 | dies 18 s, mob 44% left, took 431 | dies 30 s, mob 12% left, took 420 | wins 32 s, 12% left, took 820 | wins 34 s, 58% left, took 504 | dies 15 s, mob 74% left, took 305 | dies 41 s, mob 22% left, took 732 |
| Quartermaster Hesk | 4 | 1155 | 59 | dies 20 s, mob 25% left, took 421 | dies 29 s, mob 2% left, took 330 | wins 26 s, 34% left, took 520 | wins 29 s, 84% left, took 297 | dies 22 s, mob 49% left, took 318 | dies 40 s, mob 6% left, took 689 |
| Old Whitefoot | 3 | 842 | 47 | dies 19 s, mob 13% left, took 349 | wins 26 s, 19% left, took 232 | wins 23 s, 54% left, took 407 | wins 24 s, 87% left, took 203 | dies 22 s, mob 37% left, took 288 | wins 33 s, 40% left, took 480 |
| The Grey Sexton | 5 | 1320 | 73 | dies 23 s, mob 12% left, took 554 | wins 30 s, 15% left, took 342 | wins 26 s, 26% left, took 630 | wins 28 s, 74% left, took 304 | dies 18 s, mob 62% left, took 336 | dies 41 s, mob 4% left, took 728 |
| The Pale Reckoner | 7 | 1650 | 103 | dies 18 s, mob 35% left, took 467 | wins 30 s, 5% left, took 440 | wins 27 s, 40% left, took 758 | wins 30 s, 84% left, took 396 | dies 26 s, mob 39% left, took 462 | wins 42 s, 20% left, took 858 |
| Sandthrone captain | 8 | 1815 | 119 | dies 19 s, mob 28% left, took 604 | wins 32 s, 21% left, took 432 | wins 27 s, 49% left, took 796 | wins 29 s, 75% left, took 480 | dies 25 s, mob 43% left, took 476 | wins 42 s, 31% left, took 884 |
| Old Scree-Tusk | 8 | 1543 | 119 | dies 20 s, mob 15% left, took 624 | wins 27 s, 30% left, took 384 | wins 23 s, 47% left, took 720 | wins 26 s, 77% left, took 384 | dies 25 s, mob 33% left, took 476 | wins 35 s, 79% left, took 680 |
| Brood Weave-Eater | 10 | 1823 | 156 | dies 20 s, mob 12% left, took 682 | wins 27 s, 29% left, took 432 | wins 23 s, 61% left, took 736 | wins 27 s, 78% left, took 432 | dies 27 s, mob 23% left, took 624 | wins 35 s, 68% left, took 780 |
| The Ash-Deacon | 10 | 2145 | 156 | dies 23 s, mob 17% left, took 758 | wins 30 s, 12% left, took 540 | wins 27 s, 55% left, took 866 | wins 30 s, 69% left, took 486 | dies 28 s, mob 30% left, took 624 | wins 41 s, 41% left, took 1014 |
| Greyheart | 13 | 2640 | 221 | dies 23 s, mob 12% left, took 934 | wins 30 s, 16% left, took 640 | wins 27 s, 54% left, took 1062 | wins 30 s, 71% left, took 576 | dies 28 s, mob 31% left, took 712 | wins 41 s, 54% left, took 1157 |
| Old Ninebranch | 13 | 2244 | 221 | dies 19 s, mob 15% left, took 806 | wins 26 s, 33% left, took 512 | wins 23 s, 55% left, took 934 | wins 26 s, 79% left, took 512 | dies 27 s, mob 25% left, took 712 | wins 36 s, 64% left, took 979 |
| The Hollow Root-Warden | 13 | 3300 | 243 | dies 23 s, mob 33% left, took 1068 | dies 32 s, mob 12% left, took 770 | wins 34 s, 14% left, took 1488 | wins 38 s, 83% left, took 840 | dies 26 s, mob 52% left, took 686 | dies 52 s, mob 1% left, took 1666 |

**Normal mobs of the player's level: one, and a pack of four at once** (Warrior, then Barkhide, then Thornclaw)

| Level | One | Four, alone | Four, with Mira |
|---|---|---|---|
| 1 | wins 8 s, 92% left, took 21 | wins 34 s, 11% left, took 224 | wins 31 s, 89% left, took 196 |
| 2 | wins 8 s, 92% left, took 24 | wins 35 s, 9% left, took 264 | wins 34 s, 92% left, took 264 |
| 3 | wins 10 s, 89% left, took 32 | dies 34 s, mob 17% left, took 304 | wins 34 s, 85% left, took 264 |
| 5 | wins 8 s, 94% left, took 27 | wins 34 s, 32% left, took 288 | wins 34 s, 93% left, took 297 |
| 8 | wins 8 s, 95% left, took 30 | wins 34 s, 45% left, took 320 | wins 34 s, 88% left, took 330 |
| 10 | wins 8 s, 95% left, took 30 | wins 34 s, 51% left, took 320 | wins 34 s, 80% left, took 330 |
| 13 | wins 8 s, 96% left, took 30 | wins 34 s, 61% left, took 320 | wins 34 s, 88% left, took 330 |

| Level | One | Four, alone | Four, with Mira |
|---|---|---|---|
| 1 | wins 9 s, 92% left, took 24 | wins 40 s, 23% left, took 222 | wins 36 s, 83% left, took 216 |
| 2 | wins 11 s, 93% left, took 24 | wins 45 s, 20% left, took 264 | wins 40 s, 91% left, took 222 |
| 3 | wins 11 s, 92% left, took 28 | wins 45 s, 10% left, took 308 | wins 45 s, 78% left, took 294 |
| 5 | wins 11 s, 93% left, took 32 | wins 42 s, 82% left, took 320 | wins 40 s, 89% left, took 296 |
| 8 | wins 11 s, 95% left, took 36 | wins 42 s, 93% left, took 360 | wins 40 s, 85% left, took 333 |
| 10 | wins 11 s, 95% left, took 36 | wins 42 s, 87% left, took 360 | wins 40 s, 89% left, took 342 |
| 13 | wins 11 s, 96% left, took 36 | wins 42 s, 97% left, took 360 | wins 40 s, 86% left, took 351 |

| Level | One | Four, alone | Four, with Mira |
|---|---|---|---|
| 1 | wins 4 s, 94% left, took 14 | wins 17 s, 51% left, took 119 | wins 17 s, 85% left, took 119 |
| 2 | wins 5 s, 94% left, took 16 | wins 17 s, 45% left, took 152 | wins 17 s, 80% left, took 152 |
| 3 | wins 6 s, 92% left, took 24 | wins 21 s, 35% left, took 184 | wins 19 s, 82% left, took 160 |
| 5 | wins 5 s, 96% left, took 18 | wins 19 s, 55% left, took 180 | wins 19 s, 89% left, took 180 |
| 8 | wins 6 s, 95% left, took 30 | wins 23 s, 56% left, took 240 | wins 21 s, 79% left, took 200 |
| 10 | wins 6 s, 95% left, took 30 | wins 23 s, 61% left, took 240 | wins 21 s, 83% left, took 200 |
| 13 | wins 5 s, 97% left, took 20 | wins 21 s, 74% left, took 200 | wins 21 s, 89% left, took 200 |

**The fight as the zone gives it** (player of the elite's level, with Mira, careful). The tables above fight each elite
alone. Six of the twelve stand with guards who come when the elite is pulled, and four have kin who answer the call at 60%.
"The guards alone" is the guards pulled without the elite (a guard's alarm does not bring it). "It, its call answered" is the
elite after that, with its kin coming at 60%. "All at once" is the elite pulled with every guard up (each guard counted at its
camp's top level and all of them in reach: the worst case), killing the guards first or the elite first.

**Warrior**

| Elite | Guards | Answer its call | The guards alone | It, its call answered | All at once, guards first | All at once, it first |
|---|---|---|---|---|---|---|
| Caddock, the Bandit King | 3 (level 5) | nobody | wins 26 s, 89% left, took 180 | wins 56 s, 19% left, took 944 | dies 61 s, mob 27% left, took 1062 | dies 41 s, mob 50% left, took 987 |
| Quartermaster Hesk | 2 (level 4) | 2 (level 5, from 15 m) | wins 16 s, 92% left, took 90 | wins 69 s, 34% left, took 838 | dies 62 s, mob 26% left, took 913 | dies 40 s, mob 51% left, took 833 |
| Old Whitefoot | none | 3 (level 2, from 23 m) | - | wins 61 s, 37% left, took 684 | - | - |
| The Grey Sexton | none | 2 (level 5, from 32 m) | - | wins 61 s, 52% left, took 807 | - | - |
| Sandthrone captain | none | 2 (level 7, from 47 m) | - | wins 64 s, 66% left, took 971 | - | - |
| Brood Weave-Eater | 1 (level 10) | nobody | wins 5 s, 97% left, took 20 | wins 37 s, 77% left, took 744 | wins 45 s, 88% left, took 872 | wins 45 s, 87% left, took 878 |
| The Ash-Deacon | 5 (level 10) | nobody | wins 43 s, 84% left, took 500 | wins 45 s, 80% left, took 921 | dies 62 s, mob 32% left, took 1566 | dies 42 s, mob 51% left, took 1483 |
| Greyheart | 4 (level 13) | nobody | wins 34 s, 88% left, took 330 | wins 43 s, 83% left, took 1086 | dies 74 s, mob 10% left, took 2042 | wins 80 s, 6% left, took 1942 |
| The Hollow Root-Warden | 2 (level 13) | nobody | wins 16 s, 88% left, took 100 | wins 56 s, 32% left, took 1612 | dies 69 s, mob 6% left, took 2004 | dies 55 s, mob 23% left, took 1936 |

**Druid, Barkhide**

| Elite | Guards | Answer its call | The guards alone | It, its call answered | All at once, guards first | All at once, it first |
|---|---|---|---|---|---|---|
| Caddock, the Bandit King | 3 (level 5) | nobody | wins 29 s, 86% left, took 184 | wins 71 s, 92% left, took 828 | wins 107 s, 85% left, took 1364 | wins 104 s, 61% left, took 1620 |
| Quartermaster Hesk | 2 (level 4) | 2 (level 5, from 15 m) | wins 19 s, 87% left, took 96 | wins 84 s, 94% left, took 799 | wins 108 s, 92% left, took 1078 | wins 111 s, 71% left, took 1409 |
| Old Whitefoot | none | 3 (level 2, from 23 m) | - | wins 78 s, 64% left, took 672 | - | - |
| The Grey Sexton | none | 2 (level 5, from 32 m) | - | wins 79 s, 96% left, took 818 | - | - |
| Sandthrone captain | none | 2 (level 7, from 47 m) | - | wins 81 s, 95% left, took 923 | - | - |
| Brood Weave-Eater | 1 (level 10) | nobody | wins 8 s, 96% left, took 27 | wins 49 s, 93% left, took 686 | wins 59 s, 88% left, took 811 | wins 57 s, 94% left, took 866 |
| The Ash-Deacon | 5 (level 10) | nobody | wins 50 s, 93% left, took 504 | wins 59 s, 79% left, took 882 | wins 116 s, 93% left, took 2063 | wins 113 s, 88% left, took 2322 |
| Greyheart | 4 (level 13) | nobody | wins 40 s, 86% left, took 351 | wins 56 s, 83% left, took 944 | wins 103 s, 90% left, took 1985 | wins 96 s, 92% left, took 1979 |
| The Hollow Root-Warden | 2 (level 13) | nobody | wins 19 s, 89% left, took 99 | wins 70 s, 80% left, took 1430 | wins 94 s, 73% left, took 2049 | wins 91 s, 92% left, took 1970 |

**Druid, Thornclaw**

| Elite | Guards | Answer its call | The guards alone | It, its call answered | All at once, guards first | All at once, it first |
|---|---|---|---|---|---|---|
| Caddock, the Bandit King | 3 (level 5) | nobody | wins 15 s, 90% left, took 108 | wins 34 s, 58% left, took 504 | wins 50 s, 51% left, took 798 | wins 48 s, 17% left, took 936 |
| Quartermaster Hesk | 2 (level 4) | 2 (level 5, from 15 m) | wins 10 s, 83% left, took 54 | wins 39 s, 78% left, took 437 | wins 52 s, 68% left, took 652 | wins 50 s, 35% left, took 761 |
| Old Whitefoot | none | 3 (level 2, from 23 m) | - | wins 36 s, 79% left, took 336 | - | - |
| The Grey Sexton | none | 2 (level 5, from 32 m) | - | wins 38 s, 90% left, took 376 | - | - |
| Sandthrone captain | none | 2 (level 7, from 47 m) | - | wins 39 s, 81% left, took 534 | - | - |
| Brood Weave-Eater | 1 (level 10) | nobody | wins 4 s, 97% left, took 20 | wins 27 s, 78% left, took 432 | wins 30 s, 82% left, took 506 | wins 31 s, 91% left, took 552 |
| The Ash-Deacon | 5 (level 10) | nobody | wins 26 s, 83% left, took 300 | wins 30 s, 69% left, took 486 | wins 61 s, 29% left, took 1322 | wins 57 s, 18% left, took 1390 |
| Greyheart | 4 (level 13) | nobody | wins 21 s, 89% left, took 200 | wins 30 s, 71% left, took 576 | wins 51 s, 69% left, took 1180 | wins 51 s, 80% left, took 1216 |
| The Hollow Root-Warden | 2 (level 13) | nobody | wins 11 s, 92% left, took 60 | wins 38 s, 83% left, took 840 | wins 49 s, 68% left, took 1190 | wins 49 s, 85% left, took 1180 |

What these say, plainly:
- **Clear the guards first.** Every kit wins the guards' pull with four fifths of its health left and then the elite with its
  call answered (the least is the Warrior against Caddock, 19% left, and against the Root-Warden, 32%).
- **Pulling a guarded elite with every guard up is a death for a Warrior, even with Mira**: Caddock with his three, Hesk with
  his two and the stair watch, the Ash-Deacon with his five, the Root-Warden with his two; Greyheart with all four is won
  with 6% left only by killing him first. Both Druid forms win these (Barkhide easily). This is meant: an end boss's hall is
  not walked into. What made it unfair before this fix was that a guard's shout brought the elite, so there was no other way.
- **Hesk's two stand close to his desk** (the Store Caves' centre is 3.3 m from it): a guard within 3.5 m of him brings him.
  Pull the further guard (the nearer one answers the shout without bringing Hesk). If both stand beside him the fight is all
  at once, which a Warrior loses by a quarter of their health. Unseen; if it is so in the game, pair them further apart in
  the data or shorten `LordReach`.
- **The Ash-Deacon stands in the middle of his five**: any of them within 3.5 m of him brings him and, through him, all five.
  Pull one from the edge of the pit.

What the tables say, and what `EliteBalanceTests` asserts:
- An elite of the player's level kills a careless player who is alone: every kit, every elite.
- With Mira and careful play it falls with health to spare: every kit, every elite (the least is the Warrior against Caddock,
  19% left; a dungeon's end boss is a step harder than every outdoor elite of its level).
- As the zone gives it: the guards alone fall with at least half the player's health left, and the elite with its call
  answered falls with at least a tenth left: every kit, every guarded or answered elite. All at once is logged, not asserted.
- A careful Warrior alone does not get any of the twelve down. It wants Mira.
- An elite two levels above the player is not a solo kill: every kit, every elite, however careful.
- A normal mob alone is as it was (8 to 11 s, nine tenths of the health left). Four at once cost a Warrior alone between two
  fifths and all of his health; a new character with no armour dies to four.

What it also says, plainly:
- **The Druid is a better soloist than the Warrior** in these numbers. A careful Barkhide Druid of level 4 or more beats an
  outdoor elite of its level alone with about half its health left (Barkmend heals 12% every 8 s: it is its own healer), and a
  careful Thornclaw Druid races the outdoor ones down with a twentieth to a third left (Hesk and the two bosses beat it). Both
  still die when careless and both lose two levels down. Tuning the elites to make a lone Barkhide Druid sweat would put them out of a Warrior's reach even with Mira.
  The gap is in the kits, not in the elites.
- **A Barkhide Druid shrugs off four normal mobs** from level 5 on (armour and Barkmend). Packs are a real pull for a Warrior
  and for a Thornclaw Druid, less so for Barkhide.
- **Kiting is not modelled.** A Thornsong Druid with instant Seedshot can outrun any mob (the player runs at 5.2 m/s, mobs at
  2.8 to 4.2) inside the leash and take no blows at all. That was true before this change and still is; an elite's moves all
  need it in reach.
- A player in less than on-curve gear finds elites much harder (armour halves or thirds the swing by level 13).

### Tests (all unrun)
- EditMode: `SocialRulesTests` (kinds for every camp, the data's override, reaches, calls and kin, guard pairs, the twelve
  moves, every call has someone to answer it but the Pale Reckoner's, overmatch, the mob curve), `EliteBalanceTests` (the kit
  numbers against Encounter.asset, the table, the targets above, the fight as the zone gives it, the called mob's arrival).
- PlayMode, Oakhaven, own save folder: `SocialPullTests` (a wolf pack comes together and a far wolf stays; a boar stays single;
  a deserter's shout is in the chat and over his head and brings camp and kin after the beat; sneaking peels one wolf; an
  elite's guards come from further than a call carries; a guard's shout brings his fellows and not the king across the hall;
  a guard beside the king brings him and he brings the rest; two of Mira's heals do not turn a wolf that joined; a linked
  group resets together and stays home), `EliteFightTests` (the wind-up taken standing and avoided by stepping out; Guard
  blunts it; the enrage and its timed swings; the call brings kin; overmatch; Mira's health follows the level, an older save
  loads her whole and a wounded Mira stays wounded).
- To look at (no test sees them): `Crulanda.exe --crulanda-elite-capture <folder>` (windowed) takes 01-pack-pull, 02-shout,
  03-camp-comes, 04-heavy-blow (the mark and the cast bar) and 05-enraged.
