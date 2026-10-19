# The first world boss: the Unraveller, mother of the brood (a 10-player raid in the Lowtowns; design, PROVISIONAL)

Written 2026-10-19 from Chris's brief and answers (below), the books (`D:\code\crulanda`) and the code as it stands on `feet-bow-adit-kit`.
Nothing here is built. Citations: `b1 ch.N:L` is `D:\code\crulanda\book1\chapter_N.md` at line L (b2, b3 the same for books 2 and 3); `WB:L` is
`D:\code\crulanda\world_bible.md`; `B1WB:L` is `book1\world_bible.md`; `lowtowns.json` is `Assets/Crulanda/EncounterContent/Zones/lowtowns.json`.
Labels: CANON (the books say it), CANON-EXPANDED (the game fills in round a canon thing), GAME-ONLY, PROVISIONAL (to be settled later).
No machines anywhere in it (CRULANDA_CANON; Chris 2026-10-11).

## Chris's brief (2026-10-19 ~11:00, verbatim; ROADMAP.md "The first world boss")
"i'd like to have one mini world boss in the highest zone. the mob is level 30 but needs 10 people to kill. has to be a raid. so id like to
balance it but definite on the hardest skill level to kill. drop components for legendary weapons for each class, but only one piece will
need other world bosses at end game to complete".

## Chris's answers (2026-10-19, the question prompt)
Four answered in his words (verbatim):
1. The boss: **"Giant Weave-Eater"** (not the recommended Pale King's avatar).
2. How hard: **"1 in 2 with you (Recommended)"**.
3. The weapon: **"Epic first (Recommended)"**.
4. How often: **"Every 3 hours (Recommended)"**.

The other six were not answered separately; the recommended answers stand:
5. Where: one place every time (now the river's Drain Reach, section 1.3; more places later).
6. Level 30 against 20: the game's own +5 rule (it shows 30; any gap over 5 counts as 5), hardness from the mechanics.
7. The raid: 2 tanks, 3 healers, 5 damage, in two parties of five.
8. Loot: components are each player's own roll; the boss's gear by need and greed.
9. The smith: Tynara Embercoil, at a night forge under the Forge District's wall.
10. The Shore's "Pale King's shadow": kept as a later world boss. (The Pale Auditor written in this doc's first draft is set aside; it can be
    the Spire's or the Crown's world boss in 4.4.)

What the answers change from the ROADMAP: its endgame list had "the Weave-Eater brood's mother on the Rim". She is now **the Lowtowns' world
boss**, a 10-player raid at the cap of 20, level 30. The Rim keeps its brood camps (`drop.ash.brood`) and the Rim's own fights (Chris settled
them; the Weave-Eaters' book rules are the Lowtowns' only, `WeaveEaterRules.BookZones`).

**Asked of Chris now:** nothing. Two small things are left to the build, with a default: the name (the Unraveller, section 1.2) and whether the
sims' guilds kill her when you are away (default: yes, one try in three; it changes only the world's chat).

---

## 1. The boss: the Unraveller

### 1.1 What the books say about Weave-Eaters (read for this doc)
- **What they are:** "manifestations of the Wasting itself, born from the frayed, chaotic threads of the broken Weave" (CANON, b1 ch.20:115);
  in the Lowtowns' folklore "vile, parasitic spirits born of the High City's magical pollution" (b1 ch.1:251); Lowtown mothers tell children
  the Weave-Eaters, "shadowy spirits born from the friction of the High City's industry", "will come at night to unravel their dreams"
  (B1WB:121). The First Era's folklore used them "to frighten apprentice mages" (ch.20:115).
- **How they look:** "towering silhouettes made of shimmering, corrupted aether and screaming void. They had no faces, no discernible limbs,
  just jagged, shifting forms of absence. Where they stepped, the petrified ash dissolved into nothingness" (ch.20:119); "They did not make a
  sound as they approached. They did not have physical bodies" (ch.20:113). Round a sparked child, darkness "pooled" at her ankles like black
  smoke (b1 ch.1:249).
- **What draws them:** a strong aura "acted as a lighthouse in a storm, drawing them from miles away" (ch.20:121).
- **How they move:** "a sudden, jerking displacement of space" (ch.20:131).
- **What they do:** a touch erases: spear, armour, flesh, "leaving only a faint puff of grey dust" (ch.20:133); "They feed on structure!"
  (ch.20:137); a barrier of hard light only "paused them", they "actively consumed the magic": "They are eating the spell as fast as I cast it"
  (ch.20:141-143); steel through one "instantly turned black, brittle as dried glass, and shattered" (ch.20:155); if one touched a sparked
  child, "the resulting implosion of Unmaking would level the entire camp" (ch.20:147).
- **The answers:** bone harpoons "carved from petrified leviathan bone, trailing glowing ropes of amber light"; they "did not absorb the bone",
  the harpoons "acted like grounding rods, pinning" them; the ropes are hauled and the Eaters "physically dragged backward", the harpoons
  "preventing them from displacing" (ch.20:177-191). Then salt, "Salt of the First Sea!", "forcing structure upon the chaotic forms": they
  "calcify ... solid, jagged statues of white crystal" (ch.20:195-205), and a mace "shattered the three crystalline statues into a million
  harmless shards" (ch.20:207).
- **The river:** the Chemical River, "a thick, ugly ribbon of bubbling black sludge" (b1 ch.15:335), the High City's runoff, and "The chemical
  runoff interferes with magical tracking" (b1 ch.16:75).

**What the game has already built from this (ZONE_LOWTOWNS.md, "The rework, picks 4 and 5"; Lowtowns only):** the blink (every 4.5 s up to
7 m toward a foe over 5 m off), the lighthouse (every 2 s whoever works magic or wears a ward within 25 m gains 12% of the top threat), wards
eaten (35% of what is left each second), harpoons stop the blink for 12 s (root 5 s; Ketta Rune sells them), salt calcifies for 4 s (Tibb
Marrow sells it), steel turned black and brittle (-10% damage for 20 s on a melee hitter), and the river (1.2% health a second; it hides you
from a Weave-Eater unless it is within 3 m). The Lowtowns' Weave-Eaters are night camps in the Drain Courts, and the elite **Mother of Drains**
(20) lives in the deepest court (`lowtowns.json`, camp at -140, 202). The zone's lore already has Tibb's rhyme ("Steel goes through and steel
goes black, / Bone goes in and holds it back, / Salt goes on and turns it white") and Ketta's grandmother's knots: "Bone holds. Steel lies."

### 1.2 Who she is (GAME-ONLY creature, from CANON kind)
**The Unraveller** (GAME-ONLY name, from the Lowtown mothers' tale that the Weave-Eaters "unravel their dreams", B1WB:121), **mother of the
brood**: the Weave-Eater the tale is about. Level 30, a raid boss (a new rank above elite: a skull on her plate and "Raid (10)" under her name).
- **Why she is so much bigger than the zone (GAME-ONLY, from CANON):** Weave-Eaters are born of the High City's magical pollution (ch.1:251),
  and the Chemical River is that pollution, running through the Lowtowns. She has lain in it for a hundred years and fed. The Drain Courts'
  Weave-Eaters, the Mother of Drains among them, are her brood: they come up the drains from the river she lies in.
- **How she looks:** a towering shape of shimmering, corrupted aether and black void three times a man's height, no face and no limbs
  (ch.20:119), a shifting edge like torn cloth that never stops moving; under her the river's black water goes still and grey. Where she
  drifts onto the bank, the setts dissolve to grey nothing (ch.20:119). She makes no sound (ch.20:113): her lines are written in the chat in a
  pale grey, and the brood's shriek (ch.20:181, "ripping metal and screaming wind") is the only noise of the fight.
- **Her lines** (the chat; no voice): on rising, "...hungry..."; on a phase, "...more...", "...the little ones are hungry too..."; on a wipe,
  "...fed..." and she sinks; on death, she calcifies white from the edge in (salt's work) and breaks into shards on the water.

### 1.3 Where she rises: the Drain Reach
- **The river's north reach** between the Rag Bridge (5.6, 96) and the zone's north edge, where the river runs past the Drain Courts (the
  river's course in `lowtowns.json` runs through -6, 180 and 4, 120): **the Drain Reach** (GAME-ONLY place). It is away from the docks (the
  quays are on the south reach, y -55 to -158) and their paths, and it is the Weave-Eaters' own side of town: the courts' drain mouths empty
  into it.
- **What has to be built there:** a **silt bar**, a broad shoal of black mud and brick rubble in the river's bend, about 30 m long and 16 m
  wide, level and standable, joined to the west bank's brick apron (a further 10 m of setts under the courts' back walls). The raid stands on
  the bar and the apron; the river runs round the bar on both sides (wadeable, poisoning, hiding: the river's rules, unchanged). Altogether
  about 40 m by 26 m of footing, with the river always within a few steps. Measured and placed in build step 3 (no house, stair or camp over
  it; the eels' camp above the Rag Bridge is emptied while she is up).
- **Salt and harpoons at hand:** when the warning sounds, **Tibb Marrow and Ketta Rune bring their stock to the apron** (GAME-ONLY: the
  Lowtowners know what is coming): a salt barrel and a harpoon rack at the apron's north and south ends. Anyone in the raid takes from them
  (the price as in the market; the sims buy too). The rack is set out because the fight needs more harpoons than a player carries.
- The villagers within 60 m go indoors when the warning sounds; the Drain Courts' night camps and the Mother of Drains stay in the courts
  (not pulled: the courts are walled off from the apron).

### 1.4 Spawn timer and the warnings
- **Every 3 hours of play** (Chris: "Every 3 hours"), counted in any zone and saved; she rises at a random moment in the 30 minutes after the
  3 hours are up, by day or night. She stays **45 minutes**, or until she is killed or has wiped the raid twice. A kill or her sinking starts
  the next 3 hours.
- **The lockout is the game's own (already built):** an open-world boss is looted once a week by each character (`Lockouts.WorldBoss`, seven
  days to the 6 am reset; Chris, 2026-10-15: "open world boss is once per week"). While you are locked out she still rises: you can fight her
  for coin, standing and the Chronicle's line, and roll no components (the raid window says so before the pull).
- **The warning (5 minutes before), in Zone chat and on the river:**
  - System, the Lowtowns only: "The Chemical River has gone still above the Rag Bridge. The drains are quiet." (GAME-ONLY)
  - On the river: the Drain Reach's water stops moving and greys over; every lamp in the Drain Courts gutters out (the Lantern-Eater's trick,
    zone-wide for the reach); the brood's shriek far off.
  - The Lowtowners in the chat in their own voices (GAME-ONLY lines): Tibb Marrow, "Salt to the river, salt to the river! Two crowns a twist and
    worth it tonight."; Ketta Rune, "Bone to the Drain Reach! Bring your arms, I'll bring the rope."
  - The sims (`SimChatter`, as today): "Unraveller's coming up, who's in?", "LF raid Unraveller, need heals", "Bring salt."
- **On rising:** System, every zone: "The Unraveller has risen in the Drain Reach of the Lowtowns." A skull on the map at the bar.
- **On a kill:** System, every zone: "<leader> and <guild or 'their raid'> have salted the Unraveller. The Lowtowns' drains are quiet."
  The Chronicle marks it for every member. On her sinking unbeaten: "The Unraveller sinks back into the river, fed."
- **When you are away** (default, PROVISIONAL): the sims' guilds try her (`SimPopulation.LiveAway`), one try in three wins; the line comes over
  General. It changes nothing of yours.

---

## 2. How a 10-player raid forms

### 2.1 What exists (code, 2026-10-19)
- A party is five: you and up to four sims (`EncounterSession.MaxPartySims = 4`, `InviteLevelGap = 5`). Roles: `SimCompanion.PartyRole`
  Tank, Healer, Damage (`SimCompanion.Role`: a Warrior tanks, a Druid, Cleric or Shaman heals, a Salt-Mender heals unless a Druid is along,
  a Paladin tanks or heals by the party, everyone else is damage). The dungeon queue (`EncounterSession.Queue.cs`) gathers sims by role.
- Guilds (`SimAdventurer.guild`, `EncounterSession.Guild.cs`); chat channels `ChatChannel { System, Say, Zone, Trade, LFG, Party, Whisper,
  Guild }` (`ZoneChat.cs`); System lines through `EncounterSession.Message`, toasts through `ShowToast`.
- Group scaling (`EncounterEnemy.GroupScale`); raid marks (`RaidMark` Skull, Moon, Cross); `ZoneDefinition.raid`; the 7-day lockouts.
- No raid: no group over five, no raid frames, no ten-way loot rule.

### 2.2 The raid group
- **Two parties of five** (Party 1 and Party 2), ten in all: you and nine sims. "Form raid" turns your party into Party 1; new members fill
  Party 2. Party-wide heals and buffs reach your own party; raid-wide ones reach all ten (tagged so in the ability data). You lead, or a sim
  leads when you join someone else's raid.
- **2 tanks, 3 healers, 5 damage** (Chris's answer stands): a tank and one or two healers in each party. You fill any role.
- **Level gate:** sims of 18-20 only. The raid window shows each member's gear level (the mean of their pieces).

### 2.3 Calling the sims
- **The call to arms** (a button in the raid window, or `/raid call`): you post in Zone chat, "LFM Unraveller, need 1 tank 2 heals 4 dps"
  (built from what the raid lacks). Answers come over 20-60 s by role: (1) **your guild** first (online, 18+, they travel to you: a minute
  or two); (2) **the Lowtowns' sims**, by boldness, level and role (a sim with `bold` under .3 declines with a line); (3) **friends anywhere**
  (`SimMemory` Friend, 18+). The queue's summoning is not used: the zone should be seen to rally, and an empty zone at 3 am is part of it
  ("Not enough of the Lowtowns' adventurers are about. Try your guild, or wait for the evening.").
- **Sims' own raids:** a sim guild leader can call one in Zone chat, and you can answer it.
- **Salt and harpoons:** a sim answering the call buys 3 salt and 3 harpoons on the way (or at the apron's barrel and rack).

### 2.4 What the raid UI needs
- **Raid frames:** two columns of five (one per party): name in class colour, health, a role mark, a mana sliver for healers, and pips for the
  fight's marks on that member (Unravel stacks on a tank, "Hunted", "Swallowed", blackened steel). Click to target. Party frames hide.
- **A boss frame:** her health, the phase, her cast bar (the Feeding, to interrupt), **her pin state** ("Pinned 8 s" or "Free": the harpoons'
  timer, the whole fight turns on it) and the clock against the enrage.
- **Raid warnings:** a large centred line for what kills ("She is hunting YOU: get in the river!", "Salt her! <name> is swallowed!"), 2 s.
- **A ready check**, **the marks** (Skull on her, Moon and Cross on brood), and **the raid window** (roster by party, roles, gear levels, your
  lockout, the call button, leave).

### 2.5 Loot for ten
- **Components: your own roll** (Chris's answer stands): every member not locked out rolls their own component drop off the body (section 4);
  nothing to win or lose against a sim. Sims roll theirs (their own counters).
- **Her gear:** two epics a kill by **need and greed** among the ten (`PartyLoot`: need beats greed, the sims choose by class and upgrade).
- **Coin and standing** for everyone (the Lowtowners').

---

## 3. The fight

### 3.1 Level 30 against a raid capped at 20
The game's level math as built (`EncounterEnemy.cs`, `.Elite.cs`): no hit, miss, crit or resist rolls by level; a gap is two multipliers (an elite
with a move hits 12% harder and takes 6% less a level), and **both stop at 5 levels**, so at +10 she hits x1.6 and takes x0.7. No blow takes over
60% of a member's health (`MaxBlowShare`). Kept as it is (Chris's answer stands): she shows 30 (a skull, Deadly), the clamp counts +5, and the
hardness comes from the fight. The few blows meant to kill (her touch on the hunted, the swallowing's end, the feast) carry a flag that passes
the 60% cap; every other blow keeps it.

### 3.2 The numbers (first targets, set by the harness, section 3.6)
From BALANCE_REPORT.md at 19-20: a pure damage-dealer about 60-70 a second on a boss, a hybrid 90% of that, a tank 75% (`TankDpsShare`),
a healer sim about 20%.
- **The raid's damage:** 5 damage (about 330 a second) + 2 tanks (about 100) + 3 healers (about 40) = about 470 a second with all up. Melee
  loses some to blackened steel; everyone loses time to moving, salting, harpooning and the brood: **about 350 a second** on her.
- **Health: 160,000** after the +5 clamp (no group scaling: fixed for ten). At 350 a second, 7 minutes 40 s.
- **Enrage: 9 minutes.** A clean raid has about 80 s to spare; a damage-dealer dead from the 4-minute mark costs about 60 s; two dead early is a
  loss. That is "the hardest skill level": no slack for a missed pin or salt.
- **Her blow on the tank:** every 2.2 s, about 22% of a level-20 tank's health after armour (about 280-320 on today's tank of 1,300-1,400 with
  gear), climbing with Unravel (below) to about 40% at 5 stacks.
- **Damage to the raid:** about 85% of the three healers' measured sustained healing over the fight, with spikes needing two healers at once;
  the healers' mana just lasts 9 minutes.
- All numbers GAME-ONLY, in one data file (`EncounterContent/Raids/unraveller.json`) for the harness to tune.

### 3.3 The rules that hold all fight (from the built Weave-Eater rules, scaled)
- **The lighthouse** (built, ch.20:121): whoever works magic or wears a ward gains threat on her; the tanks must taunt to keep her. On her it
  is stronger (18% of the top threat every 2 s; GAME-ONLY number): the healers and casters are always her next meal.
- **The blink** (built, ch.20:131): every 9 s, unless pinned, she displaces up to 12 m onto whoever is top of her threat (a tank who has lost
  her, or the brightest caster). Her touch erases (ch.20:133): a blink that lands on a member who is not a tank deals 60% of their health and
  silences them 3 s.
- **The harpoons pin her** (built, ch.20:181-191): a harpoon that hits her roots her and stops her blink for 12 s (the rule built for the
  Eaters). She shakes a harpoon loose sooner each time (12 s, then 11, 10 ... never under 7: GAME-ONLY), so **the raid needs a harpoon
  rotation**: two or three members (any class: a harpoon is thrown from the bags) throwing in turn, so she is pinned all fight. A free blink is
  the commonest wipe.
- **The hauling** (CANON-EXPANDED, ch.20:189-191, "physically dragged backward"): while two or more harpoons are in her at once, she is dragged
  1 m a second toward the river's middle and takes 10% more damage. Doubled pins are a reward, not a need.
- **Steel goes black** (built, ch.20:155): melee hitting her with steel gets the brittle debuff, here stacking to three (-10% each, 20 s).
  Bone and glass-steel do not blacken (the books: she "did not absorb the bone"; GAME-ONLY for glass-steel), so the awake legendaries
  (section 4) ignore it.
- **Wards and buffs eaten** (built for wards, ch.20:143): any ward on a member within 10 m of her loses 35% a second; and see the Feeding.
- **The river** (built, b1 ch.16:75): standing or wading in it hides you from her unless she is within 3 m, at 1.2% of your health a second.

### 3.4 Phases and mechanics
**Phase 1, "The Hunger" (100% to 70%)**
- **Unravel** (the tank's stacks; "They feed on structure!", ch.20:137): each blow on her target unravels its armour, a stack for 20 s: 6% less
  armour and 5% more damage taken a stack. At 4-5 stacks the other tank taunts (`EncounterThreat.Taunt`) and the first lets them fall off.
  No swap: by 7 stacks the tank is erased.
- **The Feeding** (ch.20:141-143: "eating the spell as fast as I cast it"): a 3 s channel every 15 s. Not stopped, it strips **every good
  effect** (buffs, wards, heals over time) off every member within 20 m, and each one eaten heals her 0.3% and makes her blows 2% heavier for
  the fight. **Two answers:** an interrupt (each interrupt makes her immune to the next for 10 s, so two or three interrupters take turns), or
  a fresh harpoon landing during the channel (it breaks it: "Bone holds"). A raid that loses the Feeding loses its buffs and then the fight.

**Phase 2, "The Brood" (70% to 40%)** (the Feeding every 20 s; the blink and the pins carry on)
- **The brood** (her children, the Drain Courts' kind): every 30 s three Weave-Eaters (level 22, a fifth of a camp elite's health, with the
  built rules: blink, lighthouse, ward-eating) rise out of the river on both sides of the bar and go for the healers. **Salt calcifies one for
  4 s** (built, ch.20:205), and **a calcified one shatters at three times the damage** ("shattered ... into a million harmless shards",
  ch.20:207; GAME-ONLY number): salt then a heavy blow is the quick kill. A brood that reaches her is eaten: she heals 3% and her blows grow
  10% (stacking). The off-tank and Party 2's damage take the brood (Moon and Cross marks); crowd control holds them; harpoons pin them too.
- **Unmade ground** ("Where they stepped, the petrified ash dissolved into nothingness", ch.20:119): every 20 s she leaves a patch of grey
  nothing 5 m across where she floats (shown 3 s before), 8% of health a second inside it, for the rest of the fight. The hauling drags her,
  so a raid that pins well puts the patches out over the water, not on the bar. The bar shrinks if they don't: the fight's slow squeeze.
- **Swallowed** (CANON-EXPANDED: her touch erases, ch.20:133; salt forces structure on her, ch.20:203): every 35 s she drifts over a member
  who is not her target and takes them inside her: they cannot act and take 4% a second. **Three salt throws from three different members
  within 12 s** calcify her for 4 s: she spits them out (whole), and takes 50% more damage for those 4 s. Not freed in 12 s: they are erased
  (dead, no raising until the fight ends). Salt thrown at her outside a swallowing does nothing (she is too big to calcify on a handful).

**Phase 3, "The Unravelling" (40% to 0%)**
- **Unravelling** (the soft enrage; she "will come at night to unravel their dreams", B1WB:121): a pulse to all ten every 3 s, 1.5% at first and
  0.1% more each pulse (about 7.5% a pulse three minutes in).
- **Hunted** (the lighthouse at its worst; the river's cover, b1 ch.16:75): every 20 s she marks the member working the most magic ("She is
  hunting YOU"); 3 s later she displaces onto them, pinned or not, and everything within 5 m is erased (80%). **The answer is the river:** the
  hunted steps into it (it hides magic from her) and she blinks onto empty water and is stunned for 2 s ("missed her meal"; GAME-ONLY). The
  river costs them health and the healers a heal, and anyone who stood with the hunted and did not move is in the 5 m.
- The brood every 40 s; the Feeding every 20 s; Swallowed every 40 s.
- **Hard enrage, 9:00 from the pull: "The Feast."** She takes the bar whole: everyone on it is erased. She sinks. The raid can try again while
  she stays (45 minutes; two wipes and she sinks for good). The run back is short (the Lowtowns' graveyard).

**Why it is the hardest and still fair:** every mechanic shows before it kills (a cast bar, a pin timer, a mark, a warning, grey ground drawn
in advance), every one has an answer the zone already teaches (harpoons, salt, the river, interrupts, taunts, target swaps), and phase 3
leaves no room for a missed one. Nothing random-unfair: the hunted and the swallowed are told; the brood come from the water you can see.

### 3.5 How the sims handle it
- **Exists** (`SimCompanion`, `SimCompanion.Fight.cs`): healers and archers step out of a warned hold ring (`EliteMove.ccWarn`); sims who can
  interrupt stop a cast in reach (one every 12 s each); sims strike the Skull mark, then your target, and leave crowd-controlled mobs alone;
  Rogue and Archivist sims hold the Moon mark; tanks taunt and peel; healers heal the most hurt. Sims do not throw salt or harpoons today
  (those are the player's consumables) and **no sim steps out of a ground mark** (`BlowMark`) or a trick's area.
- **New, each small:**
  - **Out of the ground:** a sim in warned ground (unmade patches, the hunted's 5 m) walks to the nearest safe spot first (shared with the
    dungeons: their heavy blows' marks gain it too).
  - **Throwing salt and harpoons:** sims carry both and throw them (the same item use as yours, from their bags).
  - **The harpoon rotation:** the leader sets two or three harpooners; each throws when the pin has 2 s left and it is their turn (skipping one
    who is down or swallowed). You can be in it; your turn shows on the boss frame.
  - **The interrupt rotation** on the Feeding, the same way (a harpoon in the channel counts as the interrupt).
  - **The tank swap** at 4 Unravel stacks.
  - **Salt on the swallowed:** the three nearest sims with salt throw it at her when someone is swallowed (if you are one of the three nearest,
    two sims throw and the raid warning tells you to).
  - **The brood:** the off-tank picks them up; Party 2's damage-dealers salt and shatter them; nobody lets one reach her.
  - **The hunted:** a hunted sim steps into the nearest water and out again 3 s after the blink; the others move out of 5 m.
  - **Mistakes on purpose:** each sim misses a mechanic now and then by personality and level (a careless 18 more than a careful 20), set from the
    harness so that a sims-only raid wins about 1 in 3 and **you with them about 1 in 2** (Chris's answer): your play tips it.

### 3.6 How we test it
1. **EditMode rules tests** (no scene): each mechanic's numbers and timers (Unravel, the Feeding and its two answers, the shortening pins, the
   hauling, three-stack blackened steel, the brood shattering, unmade ground, Swallowed and its three salts, Hunted and the river, Unravelling's
   growth, the 9-minute feast), the +5 clamp and the cap-passing flag, loot and bad-luck counters, the save round-trip.
2. **PlayMode fight tests:** ten at 20 in the Lowtowns' gear on a fixed seed at the Drain Reach: she stays pinned through the rotation, the tanks
   swap, the swallowed are salted out, the hunted take to the river, the brood die before reaching her.
3. **The group run tool at 10** (`tools\validation\group_run.ps1` with a raid option and `--crulanda-zone zone.lowtowns`): you (the bot) and
   nine sims at 20 in the best Lowtowns gear (tier-6 crafted and the zone's rares), HUD, meter and scorecard on, ten pulls on ten seeds, on a
   quiet machine. **Targets:** sims alone 30-40%; with the bot playing well about 50% (Chris: "1 in 2 with you"); a raid in plain uncommons
   under 10%; a raid without salt and harpoons under 5% (the zone's lesson must matter). Each loss read from the log round the first death
   before any number moves.
4. **Chris's run:** a preview build with its own save folder (`--crulanda-save-dir`, a .bat), a level-20 character in good gear, the call to arms
   and a kill. Nothing is done until he has played it.

---

## 4. The legendary weapons

### 4.1 What the books give
- **Glass-steel**, "a lost art of the First Era" (CANON, b1 ch.7:149): Veyra's *Truth-Taker* hums "a vibration Valen could feel in his teeth"
  (ch.17:25) and leaves "searing, burning afterimages of gold" (ch.10:67).
- **What a Weave-Eater cannot eat** (CANON): bone ("The Weave-Eaters did not absorb the bone", ch.20:181) and the amber ropes that "actively
  resisted the Unmaking" (ch.20:181); salt forces structure on them (ch.20:203). Steel goes black (ch.20:155).
- **What is left of a salted one** (CANON): "solid, jagged statues of white crystal" (ch.20:205), shattered into shards (ch.20:207).
- **Named weapons and their kind:** Baston Iron-Hand's war hammer with "its own minor gravitational pull" (b2 ch.13:23), Baston as the Iron
  Anchor (WB:19); the Ash-Walkers' harpoons and Grohl, their chieftain (b1 ch.20:181; ch.25:25); the Glass-Archers' "salt-tipped shafts" and
  their "splash of emerald light that neutralized the violet void" (b3 ch.8:36); the Praetorian's "double-bladed Sun-staff" (b1 ch.26:69); the
  Root-Mother "encased in amber-wood" (WB:70); the Original Song and Sister Lyra's echo-jars (WB:74-78); the Lowtowners' two Rules (b1
  ch.1:257-259) and Valen's "Song of Sifting" (b3 ch.4:46); Klyther and Tynara's tools "forged from salvaged scrap and sea-salt" (b3 ch.1:70).
- **The smith:** Tynara Embercoil, Forgeborn, "can telekinetically manipulate metal" (CANON, B1WB:31; WB:36); in the game's time she serves the
  Spoke's works (b1 ch.2:7); in book 3, "I'm tired of iron anyway" (b3 ch.1:74).

### 4.2 The reading (GAME-ONLY): what she could not eat
A hundred years in the river, the Unraveller has swallowed everything the Lowtowns threw in or lost: blades, tools, the dead's grave-goods.
Steel she ate. **What she could not eat is still inside her:** First Era glass-steel, bone, amber-wood, salt-stone. When she is salted and
breaks, it falls out with the white crystal. Tynara remakes them with that crystal (which still holds salt's "structure"), so the weapon cannot
be unmade: the reason the awake legendaries do not blacken.

### 4.3 The weapons (one per class; each four pieces)
The same recipe for every class: **three pieces from the Unraveller + one from the end-game world bosses.**
1. **Calcified Brood-Crystal** x3 (shared by every class; a stack): the white crystal of a salted Weave-Eater (ch.20:205).
2. **The class's uneaten blank** x1 (what she could not eat, by class; table below).
3. **The Unraveller's Heart, salted** x1 (shared): her core, calcified white, still moving inside like smoke under glass.
4. **The last piece: A Note of the Original Song** (PROVISIONAL; WB:74-75: "only the 'Original Song' can close the Fracture"), made from three
   **Verses**, one from each later world boss (4.5). It wakes the weapon.

| Class | Weapon | Kind | Its uneaten blank (from the Unraveller) | Book root | Label |
|---|---|---|---|---|---|
| Warrior | **Second Truth** | glass-steel longsword | Uneaten Glass-Steel Blade | *Truth-Taker*'s glass-steel, hum and gold afterimages (b1 ch.7:149, ch.10:67, ch.17:25); never *Truth-Taker* itself, which is Veyra's | CANON-EXPANDED |
| Paladin | **The Iron Anchor** | war hammer | Uneaten Glass-Steel Hammer Head | Baston's hammer with its own pull (b2 ch.13:23); the Iron Anchor (WB:19) | CANON-EXPANDED |
| Rogue | **The Two Rules** ("Exist Elsewhere" and "Never Interfere") | a pair of glass-steel dirks | Uneaten Glass-Steel Dirks | Rule One and Rule Two (b1 ch.1:257-259); the Rogue's pair of Lowtown dirks | CANON-EXPANDED |
| Scoundrel | **The Song of Sifting** | a singing stiletto | Uneaten Glass-Steel Stiletto | Valen's "Song of Sifting" (b3 ch.4:46) | CANON-EXPANDED |
| Ranger | **The Salt-Wall Bow** | longbow of bone and glass-steel | Uneaten Leviathan-Bone Limbs | the leviathan-bone harpoons (b1 ch.20:177); the Glass-Archers' salt-tipped shafts (b3 ch.4:46, ch.8:36) | CANON-EXPANDED |
| Mage | **The Sun-Staff** | double-ended staff | Uneaten Void-Iron Haft | the Praetorian's Sun-staff and its void-iron haft (b1 ch.26:69, 137) | CANON-EXPANDED |
| Druid | **Amber-Wood Crook** | staff | Uneaten Amber-Wood Stave | the Root-Mother in amber-wood (WB:70); amber resists the Unmaking (b1 ch.20:181) | CANON-EXPANDED |
| Archivist | **The First Verse** | bone staff with an echo-jar | Uneaten Bone Tuning Staff | the Original Song and echo-jars (WB:74-78); the pilgrim's bone staff (b1 ch.13:73) | CANON-EXPANDED |
| Salt-Mender | **Mender's First Tool** | salt-stone maul | Uneaten Salt-Stone Maul Head | tools of scrap and sea-salt (b3 ch.1:70-74); "Salt of the First Sea" (b1 ch.20:199) | CANON-EXPANDED |
| Shaman | **Grohl's Harpoon** | bone harpoon on amber rope (a spear) | Uneaten Harpoon Head | the Ash-Walkers' harpoons (b1 ch.20:177-191); Grohl (b1 ch.25:25) | CANON-EXPANDED |
| Cleric | **The Hill-Shrine Lantern** | mace with a hung lantern | Uneaten Glass-Steel Mace Head | no clerics in the books (CLASS_CLERIC_SHAMAN.md); the Song of Mending (WB:20) | GAME-ONLY |

No machines: glass-steel, bone, amber-wood, salt-stone, crystal; no workings, springs, gears or engines.

### 4.4 What she drops, and the bad-luck protection
Per kill, for each member not locked out (personal, 2.5):
- **1 Calcified Brood-Crystal, always** (three kills give the three).
- **Your class's uneaten blank: 25%**, plus 15% for each kill without it (25, 40, 55, 70, 85, 100: never more than six kills). Always your
  character's class.
- **The Unraveller's Heart: 20%**, plus 15% a kill without it (never more than six kills).
- The loot window shows the counters ("Heart: 35% next time").
- With the weekly lockout that is **3 to 6 weeks** for her half, about 4 on average; every kill gives a crystal, and the epic comes the day the
  three pieces are in. Counters are per character and saved (section 5); sims keep their own in the world slot.

### 4.5 The one piece from the end-game world bosses (PROVISIONAL)
**A Note of the Original Song** (one per weapon): Elder Orym, the blind High Archivist "who hears the world's song" (WB:77), or Brother Aris
(WB:76) tells you an uneaten weapon wakes only with a note of the Original Song, whose verses scattered when the Weave broke. Three **Verses**
make one Note; each later world boss drops one Verse a kill (personal, guaranteed):
- **Verse of the Forge:** the Forge District's world boss (19-22 zone; PROVISIONAL, born of the foundries' slag-memory, never a machine).
- **Verse of the Deep:** the Warrens' world boss (22-25; PROVISIONAL, from the Deep Veins' geode seams, b1 ch.9).
- **Verse of the Crown:** the Spire's or the Crown's world boss (25-30; PROVISIONAL: the Pale Auditor of this doc's first draft, a Pale King's
  avatar with a gold mask, b1 ch.12:375-403, fits here).
- The Shore's "Pale King's shadow" stays a later world boss (Chris's answer); it can drop a spare Verse (any of the three) for bad luck.
None is designed; this doc only names the slot. The Verses wait in the component bag.

### 4.6 Where the combining happens, and what the weapon does
**Tynara Embercoil's night forge** under the Forge District's wall, at the Rust Market's east edge (CANON-EXPANDED: B1WB:31; the Rust Market
against the Forge District's walls, b1 ch.3:7). By day she works for the Council (b1 ch.2:7); at night, unasked and unpaid in anything but the
crystal, she remakes what the river could not eat, because she has seen what the Spoke eats (her turn by book 3, b3 ch.1:74). At her forge
21:00 to 06:00; few words. When the Forge District opens, her forge moves inside its gate.

**Stage 1, the epic** (Chris: "Epic first"): three Brood-Crystals, the class's blank and the Heart, plus coin (a week's worth at 20) and a
tier-6 slag-iron bar (Blacksmithing), make **the weapon, asleep**: an **epic** named weapon of **level 20** (quality 4: about 70 weapon damage
on today's curve, `ItemDatabase` power level x 2.1), with its look but dim, its effect line greyed: "Sleeps. A note of the Original Song would
wake it."
**Stage 2, the legendary:** the Note added at the same forge wakes it to **legendary, level 30** (quality 5, power level x 2.5: about 123 weapon
damage at 30, as the Cinderheart's 55 at 12), with its **one effect**, the legendary glow, and immunity to blackened steel. Same item id; the
save marks it awake.

**The effects** (one each; worth about 3-5% of the build's output, measured by the harness at the cap; they work for every path of the class;
GAME-ONLY numbers):

| Weapon | Effect |
|---|---|
| Second Truth (Warrior) | **Afterimage:** every 5th blow leaves a gold afterimage that strikes again for 40% (with the tank's threat when tanking). (b1 ch.10:67) |
| The Iron Anchor (Paladin) | **The Anchor:** dropping under 30% health roots you in place and takes 25% off every blow for 6 s; once a minute. (WB:19) |
| The Two Rules (Rogue) | **Exist Elsewhere:** a blow of over 20% of your health moves you 5 m behind your foe (a blink, as the Weave-Eaters move, b1 ch.20:131); once every 30 s. |
| The Song of Sifting (Scoundrel) | **Sifting:** your holds last 20% longer, and the first blow on a held foe as it breaks does 30% more. |
| The Salt-Wall Bow (Ranger) | **Salt-Tipped:** every 6th shot is salt-tipped: 50% more damage and it strips one good effect from the target (double on things of the Void and the Wasting). (b3 ch.8:36) |
| The Sun-Staff (Mage) | **Sunfire:** a spell cast at full Heat leaves a 3 m patch of sunfire for 4 s (burns foes, never friends). (b1 ch.26:69) |
| Amber-Wood Crook (Druid) | **Amber:** every 20 s your next heal also wraps the target in amber (a small ward that a Weave-Eater cannot eat), or in a damage form your next blow roots for 1.5 s. (WB:70; b1 ch.20:181) |
| The First Verse (Archivist) | **Echo-Jar:** every 8th spell is kept in the jar, and your next spell is echoed at 50%. (WB:78) |
| Mender's First Tool (Salt-Mender) | **Salt Line:** a salt working also pours a short salt line that calcifies the first foe across it for 2 s; once every 20 s. (b1 ch.20:205) |
| Grohl's Harpoon (Shaman) | **Grounding:** every 20 s your next spell pins its target: no blink, no move for 3 s, and things of the Void are silenced too. (b1 ch.20:181, 191) |
| The Hill-Shrine Lantern (Cleric) | **Kept Light:** healing past full becomes a ward of up to 10% of the target's health (gone after 8 s). |

Looks (with Phase 8.5's figures): glass-steel pale, almost white, with a seam of salt-white crystal; bone yellowed and smooth; amber-wood warm
and clear; asleep dim, awake the seam glows. Each its own design (no reused models).

### 4.7 Her other loot (signature list)
Two epics a kill by need and greed, level 20, from a list of eight (GAME-ONLY names, written with the item data): a **harpooner's coat** for
each armour weight (leather, mail, cloth: amber cord at the cuffs), **Salt-Crusted Mantle**, **Brood-Bone Gauntlets**, **the Drain Reach
Waders** (boots: the river's poison 50% weaker on you), **Knotted Amber Cord** (neck: Ketta's grandmother's way, a knot a pin), and a **bone
buckler**. No legendary on her table (the weapons are the legendaries).

---

## 5. Save and data, and the build plan

### 5.1 What has to be saved
Back up Chris's save before the format change (CLAUDE.md rule). Today: per character `EncounterSave` format 11 (one slot per class,
`EncounterProgress` with the lockouts; migrations through `SaveMigrator`); the world slot `WorldSave` format 2 (`CrulandaWorld`: sims, party,
guilds, the exchange). No world timer is saved today. Item kinds are strings ("gear", "bag", "junk", "material", "recipe"); bags are 24-slot
`Inventory` lists of `ItemStack { item, count }`. The raid-boss rank is a new flag on `EncounterEnemy` (it uses `Elite` bools today;
`ActorClassification.Boss` exists in the enums unused), with `EliteMove.phases` for the three phases.
- **Per character (`EncounterProgress`, format 12):**
  - **The component bag:** a list of `{ id, count }` (Brood-Crystals, the blank, the Heart, Verses, Notes), outside the bags (no slot), shown
    in a "Legendary" tab of the bags or the Armoury. A new item kind, `"component"`: stackable, bound, never sold or traded.
  - **Bad-luck counters:** `{ component id, kills without it }`.
  - **The weapon's stage:** the forged weapon is an ordinary item; awake is a progress key (`legend.awake:<item id>`; no item-state change).
  - **The lockout:** already saved (`EncounterProgress.lockouts`, `Lockouts.WorldBoss`).
- **World slot (`WorldSave`, format 3):** her timer (play-seconds to the next rising, or her state: warned, up, sunk), the unmade patches
  (none survive the fight: nothing to save), the sims' component counters and forged weapons (when sims carry legendaries).
- **Data (no save):** `EncounterContent/Raids/unraveller.json` (numbers, phases, lines), the components and the eleven weapons in
  `loot.legendary.json` with looks, the forge's recipes, and the Drain Reach's bar and apron in `lowtowns.json`.

### 5.2 The build plan (each step its own branch, tests and build; Chris looks where marked)
1. **The raid group** (M): two parties of five, raid-wide and party-wide effects, raid frames (with the boss frame's pin timer), the ready check,
   the raid window. Tests: EditMode raid rules (join, leave, roles, party of each member, raid-wide reach); PlayMode ten in the Lowtowns.
   *Chris looks: the frames.*
2. **The call to arms** (S): Zone chat call; guild, the Lowtowns, friends; boldness and level gates; sims' own raids; sims buying salt and
   harpoons. Tests: EditMode on a fixed roster (who answers, by role; the 3 am empty zone).
3. **The Drain Reach and her rising** (M): measure the north reach; build the silt bar and the apron (`LowtownsLevelsTests` keeps the river
   chemical and the ground level; nothing over the bar); the raid-boss rank (skull, "Raid (10)"), the +5 clamp kept with the cap-passing flag,
   no group scaling; the play-time timer in the world slot (format 3, save backed up first); the warning (the still water, the courts' lamps,
   Tibb's and Ketta's lines and their barrel and rack, the sims' chat), the System lines, her sinking. Tests: EditMode timer and world-save
   round-trip; PlayMode her rising at the bar. *Chris looks: her and the reach, by day and night.*
4. **Phase 1 and the all-fight rules** (M): the stronger lighthouse, the 9 s blink and its erasing touch, the shortening pins and the hauling,
   three-stack blackened steel, Unravel and the swap, the Feeding with its two answers; for the sims: leaving ground marks (new, shared with the
   dungeons), throwing salt and harpoons, the harpoon and interrupt rotations, the swap. Tests: EditMode each rule; PlayMode a phase-1 fight.
5. **Phases 2 and 3** (M): the brood (salt and shatter), unmade ground, Swallowed and its three salts, Unravelling, Hunted and the river, the
   feast; the sims' parts for each. Tests as step 4.
6. **The balance** (M): the group run tool at 10 in the Lowtowns, ten seeds, the targets of 3.6 (sims alone 30-40%, with you about 50%); read
   each wipe first; tune only the data file. *Chris watches a run (HUD, meter, scorecard on).*
7. **The loot** (M; the character save change): the component kind and bag, the drops and counters, her signature epics, format 12 with a
   migration test, Chris's save backed up first. Tests: EditMode drops over 10,000 kills (counters cap at six), save round-trip, migration
   from 11.
8. **The forge and the weapons** (L): Tynara's night forge, the eleven weapons' data, looks and effects, the epic stage, the awake key (a test
   fixture gives a Note until the Verses' bosses exist), awake weapons ignoring blackened steel. Tests: EditMode each effect within its band;
   the harness measures effects with a test Note at 30 later.
9. **Chris plays it** (a preview build with its own save folder and a .bat): call a raid, kill her, forge the epic. Not done until he has.

---

## Lore audit (every name used)
| Name | Label | Source |
|---|---|---|
| Weave-Eaters: aether and void, no bodies, no sound, the blink, the lighthouse, eating spells, erasing touch, feeding on structure, steel gone black | CANON | b1 ch.20:111-155; ch.1:249-251 |
| Bone harpoons on amber ropes, hauled, pinning; salt calcifying them to white crystal; the statues shattered | CANON | b1 ch.20:177-207 |
| The Lowtown mothers' tale (unravelling dreams) | CANON | B1WB:121 |
| The Unraveller, mother of the brood; her size from the river; the Drain Courts' Eaters as her brood | GAME-ONLY (from CANON kind) | this doc |
| The Chemical River; it hides from magical tracking | CANON | b1 ch.15:335; ch.16:75 |
| The Drain Reach, its silt bar and apron | GAME-ONLY | this doc; `lowtowns.json` |
| Tibb Marrow, Ketta Rune (and their stock at the apron) | GAME-ONLY people, CANON crafts | ZONE_LOWTOWNS.md; b1 ch.20 |
| What she could not eat; the uneaten blanks | CANON-EXPANDED (b1 ch.20:181) / GAME-ONLY reading | section 4.2 |
| Glass-steel, a lost art; Truth-Taker's hum and afterimages | CANON | b1 ch.7:149; ch.10:67; ch.17:25 |
| Tynara Embercoil, Forgeborn metal-shaper, as the night smith | CANON-EXPANDED | B1WB:31; b1 ch.2:7; b3 ch.1:70-74 |
| The Original Song; Elder Orym; Brother Aris; echo-jars | CANON | WB:74-78 |
| A Note of the Original Song and its three Verses; the later world bosses | PROVISIONAL | this doc; ROADMAP "The 16-30 road" |
| The eleven weapons | CANON-EXPANDED (the Cleric's GAME-ONLY) | section 4.3 |

Kept out on purpose: *Truth-Taker* itself (Veyra's own); the Ash-Walkers in person at the fight (the Rim's people; their craft comes through
Ketta); a sparked child's "implosion of Unmaking" (ch.20:147: not a mechanic on the player's side); Kael's "kinetic-axe" and the Wardens'
"arc-throwers" (b2 ch.9:77, ch.13:9: machines); the Spoke's works and levers (b1 ch.2:7: machinery); any other game's boss or weapon names.
