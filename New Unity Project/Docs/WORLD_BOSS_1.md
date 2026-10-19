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

   **Step 1-2: WRITTEN, NOT RUN** (2026-10-19, branch `wb-raid`; compiled outside Unity, no test run yet, nothing seen in game):
   - **What is built.** `RaidGroup.cs` (the ten: Form, Join with Party 2 first, Leave, Move, Arrange, the part and level gates, Reaches,
     the LFM line, the ready check; `RaidReach`, the tags; `RaidFrames`, the frames' words and cells). `RaidCall.cs` (who answers, in
     order: guild, the zone by nerve, friends; the 3 am line; `BuyKit`, 3 `throw.salt_twist` and 3 `throw.bone_harpoon`, Tibb's and Ketta's
     items that already exist). `EncounterSession.Raid.cs` (the raid on the session: `PartySims` holds all nine sims, each with its part and
     party; `/raid`, `/raid form|call|ready|arrange|leave`; the call's coroutine). `EncounterHud.Raid.cs` (the raid frames in place of the
     party frames, the boss frame, the raid window). A party of five is unchanged: `MaxPartySims` stays 4, the queue refuses a raid.
   - **The tags (what reaches whom), as built:** a class's standing buff is raid-wide (all ten); a feast feeds the eater's party; the heals
     that go out round the healer are party-wide (a Cleric's mending round, Chorus and Answering Hills, a Paladin's and Archivist's party
     heals, a Salt-Mender's chant and party heal, an Archivist's kept vow, a Rogue's Encore, a sim's Salt Cairn); single heals and circles
     laid on the ground reach anyone in the raid. Songs are tagged party-wide but the kits' song code is unchanged (still the whole group):
     settle in step 4 if a fight shows it matters.
   - **Loot for ten:** need and greed already rolls among everyone in `PartySims`, so ten roll (the PlayMode test checks it); coin splits ten
     ways. The components are step 7. The boss frame's pin state reads `RaidBossPinned` (0 shows "Free") and its clock `RaidBossEnrage`:
     step 4 sets them. Her lockout key is `RaidGroup.BossLockKey` (`mob.worldboss.lowtowns.unraveller`): step 3 gives her that key.
   - **Not built here:** the sims' own raids (a sim leader calling in Zone chat), left for step 3 with her warning. Open-world mobs scale for
     a group of ten as for any group (`GroupScale`); step 3 turns scaling off for her only.
   - **Tests (each fails on today's code: the raid types and rules do not exist there):** EditMode `RaidGroupTests` (10) and
     `RaidCallTests` (5); PlayMode `RaidFormTests` (2: ten formed in the Lowtowns, and the call to arms at 1/50 speed).
   - **To run and look at (the lead):** the three test files; then in a build in the Lowtowns at level 20: `/raid`, Form raid, invite or call
     ten, **capture the raid frames** (two columns of five, role marks, health, the healers' mana slivers, ready-check bars during a
     check), the raid window (roster by party, parts, gear levels, the lockout line, the four buttons) and the boss frame on an elite
     target, front at 1920x1080. *Chris looks: the frames.*
3. **The Drain Reach and her rising** (M): measure the north reach; build the silt bar and the apron (`LowtownsLevelsTests` keeps the river
   chemical and the ground level; nothing over the bar); the raid-boss rank (skull, "Raid (10)"), the +5 clamp kept with the cap-passing flag,
   no group scaling; the play-time timer in the world slot (format 3, save backed up first); the warning (the still water, the courts' lamps,
   Tibb's and Ketta's lines and their barrel and rack, the sims' chat), the System lines, her sinking. Tests: EditMode timer and world-save
   round-trip; PlayMode her rising at the bar. *Chris looks: her and the reach, by day and night.*
   **WRITTEN, NOT RUN (2026-10-19, branch `wb-reach`; compiles, no Unity run yet).** What is in it:
   - **The reach (measured):** the river's north reach is flat street level (0) on both banks from the Rag Bridge to the zone's edge, the
     river's line about x -7.4 from y 180 to 205, 16 m half-width, 2.4 m deep. The silt bar (`lowtowns.json` "shoals", `ZoneShoal`;
     GAME-ONLY) is 16 m by 30 m centred at (-7.4, 192), its top 0.3 m over the water; the river is widened to 1.3 there (eased over 10 m
     past the bar's ends) so a wadeable channel about 5-7 m wide runs on each side. A neck of mud (8 by 6 m at y 183) joins it to the
     apron, 9 by 30 m of wet setts on the west bank (x -31.5 to -22.5) at street level. The sootwort node at (-30, 200) moved to (-36, 202)
     to keep it off the apron. Dressed in `ZoneBuilder.DrainReach.cs` (black mud and brick rubble, setts).
   - **The rank:** `ActorClassification.Boss` now used (`Actor.SetClassification`); `EncounterEnemy.Raid.cs`: a skull before her name on the
     plate and on her portrait, "Raid (10)" in her frame's status line; the +5 clamp as an elite's (x1.6, x0.7); `CapBlow`'s cap-passing
     flag (only a raid boss's blows may carry it; none does yet: steps 4-5); no group scaling. A locked character still sees her rise
     (no loot); her lockout is a world boss's week.
   - **Her (placeholder):** the Weave-Eater's look at 2.6 times, level 30, pool 112,000 (160,000 after the clamp), swing base 190, the
     Weave-Eaters' book rules (blink, lighthouse, wards eaten, harpoons, salt, blackened steel); no phases. She rises out of the river at
     the bar over 4 s ("...hungry..." in the chat) and sinks over 5 s.
   - **The timer:** `WorldBossClock` (3 h of play, any zone, not paused; a moment of the next 30 min; the warning 5 min before; 45 min up;
     two wipes and she sinks, "...fed..."; a kill or sinking starts the next 3 h), saved in the world slot, **format 3**
     (`AddWorldBossMigration`; SAVE_FORMAT.md). **Back up Chris's save before any build with it.**
   - **The warning (Lowtowns only):** the System line; the reach's water stilled grey; every lamp in the Drain Courts and within 70 m of the
     bar out; Tibb Marrow and Ketta Rune call in Zone chat and stand by their salt barrel and harpoon rack at the apron's ends (talk to them:
     their own shops); villagers within 60 m go indoors; up to three sims call the raid in Zone chat. On rising, a kill or her sinking: the
     System lines in every zone.
   - **Not in it yet:** the eels' camp emptied while she is up (it is 55 m south of the bar, by the weir); the map's skull; the
     Chronicle's line; the sims' guilds trying her while you are away; her brood, phases and loot (steps 4-7).
   - **Tests:** EditMode `WorldBossTests` (the clock: 3 h, the 30-minute window, the 5-minute warning, 45 minutes, kill, two wipes,
     paused; the world slot's round trip at format 3; migration from formats 2 and 1; the rank's numbers and the cap flag) and
     `DrainReachTests` (the bar level, dry and in the river with wadeable chemical water both sides, the neck and apron dry and level,
     nothing over them, the river elsewhere unchanged); PlayMode `UnravellerRiseTests` (the bar walkable from the apron, the warning's
     lines, still water, lamps, Tibb and Ketta at the stock, her rising at the bar with the rank, then her sinking after 45 minutes and
     the reach put back). `LowtownsLevelsTests` must still pass (the node moved; no terrace touched). `ExchangeTests` updated for format 3.
   - **To capture (Chris looks):** her risen on the bar from the apron and from the Rag Bridge, by day (11:00) and by night (21:00); the
     reach stilled during the warning by night (the Drain Courts' lamps out); Tibb's barrel and Ketta's rack with the two of them by
     them; her plate and target frame (skull, "Raid (10)"). Front, back and side of her at the bar.
4. **Phase 1 and the all-fight rules** (M): the stronger lighthouse, the 9 s blink and its erasing touch, the shortening pins and the hauling,
   three-stack blackened steel, Unravel and the swap, the Feeding with its two answers; for the sims: leaving ground marks (new, shared with the
   dungeons), throwing salt and harpoons, the harpoon and interrupt rotations, the swap. Tests: EditMode each rule; PlayMode a phase-1 fight.
5. **Phases 2 and 3** (M): the brood (salt and shatter), unmade ground, Swallowed and its three salts, Unravelling, Hunted and the river, the
   feast; the sims' parts for each. Tests as step 4.

   **Steps 4-5: WRITTEN, NOT RUN** (2026-10-19, branch `wb-fight` = `wb-raid` + `wb-reach`; compiled outside Unity, no test run yet, nothing
   seen in game). Every rule is the books' (CANON: b1 ch.20:111-207 the blink, the lighthouse, the eaten spell, the erasing touch, steel gone
   black, the harpoons that pin and are hauled, the salt that calcifies, the shattering; b1 ch.16:75 the river that hides from magic; B1WB:121
   the mothers' unravelling) and every number GAME-ONLY, PROVISIONAL, in one data file.
   - **The numbers:** `EncounterContent/Raids/unraveller.json` (`UnravellerTuning`; registered on the content as `raidFiles`, by Build
     Oakhaven too). Balancing (step 6) edits only that file. Her health (160,000 after the clamp) and swing (190) now come from it.
   - **The all-fight rules** (`EncounterEnemy.WeaveEater` reads her numbers, not a second copy; `EncounterEnemy.Unraveller`;
     `UnravellerRules`): the lighthouse at 18% of the top threat every 2 s within 30 m (her lighthouse knows a sim's craft by its class: the
     sims all carry one pool); the blink every 9 s, up to 12 m onto the top of her threat, and where it lands on one who is not a tank, 60% of
     their health and 3 s of silence (you: no spells; a sim: no mends, no kicks); wards eaten off everyone within 10 m; each harpoon pins her
     (no blink, no move) 12 s, then 11 ... never under 7, and two in at once haul her a metre a second toward the river and she takes 10%
     more; steel black three stacks deep (-10% each); Unravel a stack a blow on whom she strikes (-6% armour, +5% taken, 20 s each), seven and
     the tank is erased; the Feeding, her cast (the cast bar, interruptible): 3 s, landed it eats every good effect and every ward within 20 m,
     0.3% of her health and 2% heavier blows each; an interrupt that stops it makes her immune to the next for 10 s (no diminishing returns
     on her interrupts); a harpoon landing in it breaks it ("Bone holds"). She takes no long hold (incap, fear) and no sim's hold.
   - **Phase 2** (`EncounterSession.UnravellerFight`): three brood every 30 s out of the river on both sides of the bar, level 22, a fifth of a
     camp elite's health, the Weave-Eaters' book rules, going for the healers; salt calcifies one 4 s and the next blow shatters it (x3); one
     that reaches her (3.5 m) is eaten: she heals 3% and her blows grow 10%. Unmade ground every 20 s where she floats (5 m across, a ground
     mark filling for 3 s, then 8% of health a second, for the fight). Swallowed every 35 s: the nearest who is not her target, held inside her
     at 4% a second; three salts from three hands within 12 s calcify her 4 s (she takes 50% more) and she spits them out; 12 s and they are
     erased. Salt on her with nobody inside: "too big to calcify on a handful", nothing.
   - **Phase 3:** the Unravelling's pulse every 3 s on everyone within 60 m, 1.5% and 0.1% more each; Hunted every 20 s, the one working the
     most magic (never her target), a ground mark following them 3 s, then she falls on where they stand, pinned or not: 80% (passing the 60%
     cap) to all within 5 m; the hunted in the river is not struck and she misses her meal (stunned 2 s). The brood and the swallowing every
     40 s. **The feast** at 9:00 from the pull: everyone of the party out of the river erased, a wipe counted, she is whole again (the second
     wipe: she sinks, fed).
   - **The frames:** the boss frame's pin timer ("Pinned 8 s" / "Free"), the enrage clock, the phase by name, whose harpoon is next ("Your
     harpoon!" in gold) and "Hauled"; the raid frames' row: Unravel n, Steel n, Hunted, Swallowed. **The raid warning**, a large red line for
     2 s: hunted, swallowed, salt her, unmade ground under you, four Unravels on you, the brood (to a healer).
   - **Her action track:** a hook, as the music is on another branch: `ZoneMusic.FightOverride` takes the fight track while she fights the
     raid, from `Unraveller.FightTrack` ("Music/WorldBoss/WorldBossFight"), played only if that clip is in Resources. On merging the music
     branch, point `Unraveller.FightTrack` at `ZoneMusic.WorldBossFight`.
   - **The sims** (`SimCompanion.Raid`, choices in `RaidJobs`): **out of marked ground** (new, shared with the dungeons: `GroundMarks`, every
     mark the game draws: a heavy blow's, a warned hold's, a trick's ring, a Hollow's dust, the loop's snap, her ground and her hunt; every sim,
     melee and tanks too, a mend in hand landing first). **This changes the dungeons' fights** (the sims now step out of a boss's heavy-blow mark):
     the dungeon group runs should be seen again. **The harpoon rotation:** set at the pull, three damage-dealers (archers and casters first),
     each throwing on its turn when her pin has 2 s left, passed over when down, swallowed or held; you join with `/raid harpoon` (your turn on
     the boss frame; passed on after 1.5 s free). **The interrupt rotation** on the Feeding (three with a kick); when the kick on turn will not
     do, the harpooner on turn throws into it. **The tank swap** (`RaidJobs.TauntHer`): the other tank takes her at four stacks when it has
     fewer, or when the holder is one blow from erased; the tank with fewer stacks takes her back from a caster. **Salt on the swallowed:** the
     three nearest with salt (two and the warning to you when you are one of them), asked again each second until freed. **The brood:** the
     tank not holding her and Party 2's damage-dealers; salt, then the shattering blow. **The hunted** runs for the nearest black water and
     stays 3 s after she falls; the others step out of the hunt's mark. **Kit:** three salt and three harpoons bought on the way (step 2);
     when out, three more from Tibb's barrel or Ketta's rack on the apron (on credit if the purse is short). **Mistakes on purpose:** a late
     harpoon, a salt not thrown, a hunt not run from (12% at 18, 4% at 20, 6% more when rash), from the data file.
   - **Not built, or built differently (say if it matters):** the brood are not marked Moon and Cross by the game (a Cross or a held Moon is
     "leave alone" to the sims today; you can still mark them); the erased can be raised as anyone dead (no "not until the fight ends"); the
     Feeding eats statuses and wards, not heals over time kept outside the status list; bone and glass-steel weapons still blacken (step 8);
     Unravel's 20 s outlasts a swap at four, so the swap is "four and fewer than the holder" (step 6 may shorten it to about 12 s).
   - **Tests (each fails on today's code: the types and the sims' parts do not exist there):** EditMode `UnravellerRulesTests` (12: the data
     file, phases, the shortening pins and the hauling, whom she blinks to and the touch, Unravel and the erasing, steel three deep, the
     Feeding and the 10 s immunity, the shattering and the eating, the three salts and the erasing, the hunt and the river, the pulse and the
     feast, the rotation) and `RaidJobsTests` (7: when a harpooner throws, the answer to the Feeding, the swap, the three salts, who takes the
     brood, the careless 18 against the careful 20, the step out of marked ground); PlayMode `UnravellerFightTests` (2, ten in a raid at the
     Drain Reach at four times speed, her health made deep and her waits shortened: phase 1, and phases 2 and 3 to the feast).
   - **To run and look at (the lead):** the three test files, the old `LowtownsReworkTests`, `RaidFormTests`, `UnravellerRiseTests`,
     `WorldBossTests`, and a dungeon PlayMode set (the sims' new step out of marks). Then in a build, `group_run.ps1 -Raid -Pulls 1` (below)
     and **capture each mechanic visible**, front at 1920x1080 and from the side: the pin (a harpoon in her, "Pinned 11 s" on the boss frame,
     a second harpoon and "Hauled"); blackened steel (Steel 3 on a melee member's raid frame and its buff row); the Feeding's cast bar and
     "Interrupted!" / "Bone holds"; Unravel 4 on a tank's frame and the swap; the brood rising out of the river at the bar's sides, a salted
     one white and its shattering; the unmade ground's grey patches on the bar and the sims stepping out; the swallowed player or sim inside
     her (the raid warning "Salt her!") and the spitting out; the Unravelling's pulse numbers; the hunted caster's mark and the caster in the
     river with her landing on empty water ("Missed her meal"); the feast. *Chris looks: the mechanics.*
   - **Step 6 is ready to run:** `tools\validation\group_run.ps1 -Raid [-Pulls 10] [-Class class.paladin]` takes the raid of ten to her
     (`--crulanda-raid`: zone.lowtowns, you and nine sims at 20, salt and harpoons, the raid formed on the apron; `--crulanda-unraveller-now`
     brings her up at once, and again after she sinks), a seed a pull; `raid-log.txt` has each pull's phases, every death with what was on
     at that moment (her health and phase, the pin, Unravel on them, the Feeding, the brood, the ground, the swallowed, the hunted) and the
     end; `run-summary.txt` the kill rate against 3.6's targets and the mechanics' counts. Out to `D:\crulanda-work\run-captures\unraveller-raid`.
   **Run 1 (2026-10-19, `group_run.ps1 -Raid -Pulls 3` on Chris's screen; run-captures\unraveller-raid-1) and what was changed (WRITTEN,
   NOT RUN again).** Chris (play note 256, verbatim): "seems a bit unbalanced and need a better mob skin/monster skin for the boss".
   - **What happened:** 0 kills of 3. The Warrior tank was erased at 0:21 with seven Unravel stacks: the swap never came, because the sim
     tank looked for the other tank only among the sims and you (the bot) were the other tank, and the bot never taunted. The brood rose beside
     the bar's middle, passed her on the way to the healers and she ate 23 of 36, each healing 3%: she went back over 70% and the fight went
     back to phase 1, so phase 3 (the pulse, the hunt) never came. The brood piled up (eleven at once) and the healers died under them. The
     PlayMode hunt failed because the hunted found no water: the river was asked for at the bank's height.
   - **Fixed in code:** a phase once reached holds (`UnravellerRules.PhaseNow`); the swap counts you as the other tank, and the run's bot
     taunts as a sim tank does (`EncounterSession.TauntsUnraveller`); the brood rise off the bar's corners, clear of her; the river is found
     at an actor's height over the walkable ground; the end-of-pull line gives her health as the fight ended (it said 100%: she is made whole
     by the wipe); the run's summary lists each member's damage a second against its band.
   - **Fixed in the data file** (`unraveller.json`, its "note" says why): the lighthouse .18 to .10 of the top threat (the tanks kept losing
     her to the casters); Unravel 20 s to 10 s a stack (at 20 s the stacks outlast a swap at four: a test now runs two minutes of swapping and
     nobody is erased); brood 3 to 2 a wave at 12% of a camp elite's health; eaten only within 2.5 m, for 1% and 5% heavier blows.
   - **The meter** (Chris's: Archivist 93/s, Ranger 89, Mage 67, Rogue 66, Scoundrel 24, Warrior 24, the Paladin bot 23; the three healers
     exactly alike). The band at 20 is 88 a second for a pure damage-dealer (`SimCompanion.PureDps`), a hybrid 90% (79), a tank 3/4 (66).
     Why, and what changed: (1) the Archivist (a hybrid) went over its band on area blows: with her brood about her, the sims' area blow
     struck her and the brood together; a world boss is a single target now, and no area blow lands on her from her brood. (2) The Scoundrel
     and the Rogue were on the brood, chasing blinking Weave-Eaters across the bar: only Party 2's damage-dealers who fight from range take the
     brood now; the melee stay on her. (3) The Warrior and the bot tank were dead most of each pull (the swap, above). (4) Blackened steel is
     the books' (b1 ch.20:155) and hurts only those who strike her in reach: kept, but at 5% a stack (15% at three, not 30%), and it now
     blunts a sim's blade as it blunts yours (it did nothing to the sims before; the Lowtowns' own Weave-Eaters now blunt a sim's blade 10%
     as the book says). (5) The healers' damage is their own small blow (half the formula's, by level and practice); three healers at 20 hitting
     her at the same pace come out alike: not a shared source. The healers' 16 a second is about a fifth of the band, as the bands want.
   - **Her body** (Chris: "Both", then "maybe use this with the purple spike or some conglomeration. myassets free fantasy spider"):
     `UnravellerBody` builds a conglomeration on her: Gergely Zsolnay's Free Fantasy Spider (Asset Store, Kalamona; Asset Store EULA;
     `Resources/Props/fantasySpider`: the FBX with the package's own .meta and its legacy clips as the package split them, idle, walk,
     attack1/2, hit1/2, taunt, jump, death1/2; `spider_01_black.tga` capped at 1024 by `ThirdPartyImport`; the normal map; the Standard
     material; the demo scene, ground, the spinning script and the label holder left out), about 3.4 m to its back and its legs spread to
     about ten, its hide near black with a violet light in it; the Weave-Eaters' purple threads with their burning tips (the look Chris liked)
     as a crown three times their size on its back; and `VoidWrap` round all of it (CANON, b1 ch.20:119): a tower of jagged black shards to
     about seven metres, each shifting on its own noise, a shimmering violet aether rim, grey ash lifting where she stands and moves, and on
     her blink (ch.20:131) an afterimage of the shards where she was and a squash where she lands. The spider walks when she moves, strikes
     when she swings, jolts on a blink, and dies in its own death with the crown and the wrap gone white (salt's work, ch.20:205). She rises
     out of the river from seven metres down. `VoidWrap` goes on any body (Chris's downloaded model later). Cheap: 32 four-triangle shards on
     the sprite shader, one small particle system, the spider's one skinned mesh. Which way the spider's file faces is not yet seen:
     `UnravellerBody.FaceYaw`.
   - **To capture** (front, back and side at 1920x1080, by day and by night): her rising out of the river at the bar; standing (the spider's
     idle, the crown, the wrap's shifting edge and rim, the ash); walking; striking; a blink (the afterimage and the jolt); pinned (a harpoon in
     her, "Pinned" on the boss frame, her standing still); her death (the spider's death, the crown and the wrap white); and the boss frame and
     raid frames in a fight. Check the spider faces the way she moves (else set `FaceYaw`).
   - **Tests:** `DrainReachTests.Nothing_stands_over_the_bars_walk` failed on the bestiary branch's "Bank-snouts on the north bank" camp at
     (-25, 188), on the apron: the camp moved to (-24, 164), south of the apron, past where the river widens for the bar, on the bank band, dry and off Weir Lane (BestiaryMonsterTests' rules checked by hand; the bar untouched). New: `UnravellerRulesTests` (the phase
     holds; the void wrap's size and parts), `RaidJobsTests` (the swap goes round at 10 s; melee off the brood); the PlayMode phase-1 test
     checks her body (the spider in, 6-8 m, a clip playing).
   **Raid 2 (2026-10-19, run-captures\unraveller-raid-2; pull 1 a wipe at 7:11 with her at 19%, all three phases; pull 2 a wipe at 8:19 at
   32%) and what was changed (WRITTEN, NOT RUN again).** Chris, verbatim: "woah too much cant even make out the spider"; "she needs adds.
   smaller spiders with the original texture but purple"; "wipe but when down too fast when over 75% health"; "archivist now doing 80dps";
   "shaman healing much lower than others".
   - **The wrap** is now a thin edge, not a tower: 12 thin shards round her legs' and back's rim (none in the middle in front of her), none
     over 1.2 times her body (a test holds it under 1.5), the void translucent (45%) with the violet rim, shifting, each now and then gone for a
     moment. The ash at her feet and the purple crown on her back stay.
   - **Her adds** (play note 260): her brood are small spiders now (`BroodBody`): the same Free Fantasy Spider in its own painted hide
     (`spider_01.tga`, added) tinted purple, about 1.3 m long, scuttling on the walk clip, biting, dying; salt turns one white crystal and
     still, and one shattered dies white. The Weave-Eaters' rules still hold them. Phase 1 has no adds (as the doc); `broodEvery1` in the
     data file turns them on for step 6 if the fight wants them earlier (0 now, to keep the 1-in-2 target: pull 1 nearly won without).
   - **The first deaths** (read round the first death): pull 1 the Rogue at 2:29, pull 2 the Scoundrel at 1:58, the bot tank at 2:03 and
     the Rogue at 2:29, all just after phase 2 began, all melee, all with unmade ground under her. **The melee reach was measured from her
     centre** (2.4 m) and her unmade ground lies at her centre, 2.5 m round: a blade could not strike her without standing in it, so the
     melee stood in it or stepped out all fight (the Rogue at 18 a second, the Scoundrel 21: a fifth of their band). **Fixed:** her body has a
     reach (`EncounterEnemy.BodyRadius`, 2.5 m, the spider's body): a blade, a kick, a throw, your abilities and her own swing are measured
     from her edge; the melee now stand 2.5-4.9 m from her centre, outside the patch she lays.
   - **The Archivist** (a control and support class: BALANCE_REPORT.md, "control and support at most 75%"): a sim Archivist in the damage
     part was set at a hybrid's 90% of the band (79 at 20) and its unwrite ticks on while it moves (stepping out, throwing), so it led when the
     others lost time. Now `SimCompanion.ControlDpsShare` 78% (about 69 at 20): well under the damage-dealers, just over a tank. The player's
     Archivist kit is not changed.
   - **The Shaman's healing**: all three sim healers mend by the same rule; the likeliest cause is that three of them chose the same most-hurt
     member, and the Shaman's mend landed last on a full bar (the meter counts only what heals). Now a healer passes over one another
     healer's mend is already on its way to (unless they are under 35%). The run's band lines now show each healer's healing a second, to
     see it in the next run.
   - **Her damage numbers** stacked in a column over her: on a raid boss your own blows float as before, the rest of the raid's are summed and
     floated once each half second, smaller, to her side.
   - **To capture next** (front, back, side): her with the thin wrap (the spider reads first), the crown, the brood spiders scuttling out of the
     river, a salted one white and its shattering, the melee at her legs' edge outside the grey ground.
   **Raid 3 (2026-10-19, run-captures\unraveller-raid-3: pulls 1 and 2 kills in 6:42 and 6:36, the first death at 4:27 with her at 20%)
   and what was changed (WRITTEN, NOT RUN again).** Chris, verbatim: "seems too easy now. dont make the purple spikes permanently coming out
   of ground. randomly every 5-7secs". The melee and the healers now work (his meter: Ranger 86, Archivist 85, Scoundrel 61, Rogue 59; the
   healers even, 46-49).
   - **Harder, toward one kill in two with the bot playing well** (data file only): health 160,000 to 195,000 (about 8:15 a kill against the
     9:00 feast at raid 3's pace, less the Archivist's cut below); the brood back to 3 a wave; unmade ground 10% a second (8%); Swallowed 10 s
     (12); the Unravelling's pulse 2% and 0.12% more a pulse (1.5%, 0.1%). Step 6 reads the next run's kill rate before moving any more.
   - **The spikes erupt, not stand:** the void wrap's shards are under the ground until, at a random moment every 5-7 s, they burst up round
     her in new places, stand a moment and sink (about 1.25 s); a blink brings one where she lands, with the afterimage where she was; dead,
     they stand white. Purely a look (no hazard in it: her ground marks are her hazards). Between, the spider, its violet glow, the crown and
     the ash.
   - **The Archivist still at 85** (its aim 69 at 78%): its blows carry its buffs' edge (up to 15%: the raid's class buffs on Intellect),
     and its area blow found "packs" of her brood counting her as one of them (two brood beside her made three). She no longer counts toward
     a pack, and the control share is 72% (about 63 at 20, some 72 with its buffs): under every damage-dealer. SimDummyTests no longer asks
     the Archivist to out-hit the tank.
   **Raid 4 (2026-10-19, watched by Chris) and what was changed (WRITTEN, NOT RUN again).** Chris, verbatim: "something needs to be done
   about the swirling bowl spike above the mob..make it smaller and blink on and off randomly"; "ground damage should be increased"; "more
   brood per wave..need CC baby".
   - **The crown** (the Weave-Eater threads on her back) is its own size now (a third of the 3x), and blinks: shown 0.6-1.5 s, gone 2-5 s, a
     0.15 s fade each way, at random, apart from the ground spikes' eruptions; dead, it stands (crystal).
   - **Unmade ground:** 15% of health a second (10%), and 5% more for each second stood in it (`unmadeRamp`; capped at 60%): a mistake
     punished, not the raid. The sims look for marked ground four times a second and step out when it is shown, 3 s before it bites, so a sim
     is only in it when it walks through or misses on purpose.
   - **The brood and crowd control:** 5 a wave (3). The raid's control classes hold them (`RaidJobs.BroodHolder`): the Archivist first (its
     raid part, fitting its lower damage share), then the Rogue, the Scoundrel, the Mage, a Druid not healing; each holds the nearest free
     brood out of the fight for 18 s, then waits 15 s (`broodHoldSeconds`, `broodHoldEvery`), and marks it Moon (or Cross). A held brood is
     left alone; a marked one free again is fought and held again (the Moon/Cross "leave alone" no longer skips her brood once free). The rest
     are salted and shattered as before. With the raid's four control classes, four of five are held each wave; without them five go for the
     healers.
   - Toward 1 in 2: her health stays 195,000; the new ground and the bigger waves are the change this round. Step 6 reads raid 5's kill rate.
   **Raid 5 (2026-10-19, watched by Chris) and what was changed (WRITTEN, NOT RUN again).** Chris, verbatim: "once they are cc they need to
   be killed systematically or they will break and eventually kill the raid"; "sims are not attacking adds"; "off tank and dps kill adds
   while main tank stay on main spider"; "but there still needs to be cc while they take down the adds systematically".
   - **Why nobody killed them:** a held brood was marked Moon or Cross and "left alone", and only Party 2's ranged took brood; with all of a
     wave held there was nothing they might strike, the holds ran out, and the control classes held them again.
   - **The kill order** (`RaidJobs.KillOrder`, `EncounterSession.BroodKill`): the free one first (it is on the healers), then the held,
     Cross before Moon before the rest, the one whose hold runs out soonest first. The first wears the Skull; it is the only brood the raid
     strikes, held or not (a blow breaks its hold); the others stay held (no area blow or stray blow lands on a held brood). When it dies the
     Skull passes on. The boss frame has a line under it: "Kill: Skull, then Cross, Moon (3 held)".
   - **Who does what while a wave is up:** the main tank (the one she is on) stays on her alone; the off-tank takes the free one, taunting it
     off the healers; every damage-dealer kills in the kill order (salt then the shattering blow as the fast kill), then goes back to her;
     the control classes keep the waiting ones held (Archivist first, any that break held again; a hold is 18 s, the next 15 s on); the
     healers heal both tanks. A swap mid-wave: the tank who takes her stays on her, the one she leaves goes to the brood.
   - **Numbers:** brood 30% of a camp elite's health (about 1,050 at 22: a few seconds each with the raid's damage on it, so five held and
     killed in turn take most of a wave's 30 s, and a raid that lets the holds run out or kills out of order has free brood on its healers);
     her health 170,000 (195,000), for the damage the raid now spends on the brood.
   - **The run's log** gives each pull's brood killed, broken free of their holds, and eaten.
   - **The add phase's final shape** (Chris, verbatim: "if all is cc'd then the tank still needs to grab ONE and the dps assist him and not
     break other cc"): with every brood held, the off-tank grabs the Skull (its blow breaks that one hold, its taunt takes it) and the
     damage-dealers assist it, staying on her until the off-tank is on the Skull, never touching a held one; when it dies the off-tank grabs
     the next; no sim's area blow while a held brood is within its reach. The control classes re-hold any that break. A raid that drops its
     holds or kills out of order has free brood on its healers. (When you, the bot, are the off-tank, the damage-dealers do not wait: they
     open the Skull themselves.)
   **Raid 6 (2026-10-19) and what was changed (WRITTEN, NOT RUN again).** Chris, verbatim: "ok maybe 5 is too many. drop to 4" (the
   coordinator set four a wave, e11c8340); "are they buffed up? with raid food buffs? elixir? group buffs?"; "need to wait or build a raid
   buff food".
   - **The raid's buff-up before every pull** (`EncounterSession.RaidBuffUp`; the run's bot and every sim, as at a dungeon boss's stop):
     food, the elixir for one's part, a draught if low, and every class's standing buff laid on all ten; then a ready check, and the pull
     waits for it. The sims restock their packs with their coin first; the bot is given food, elixirs and draughts for every pull. The log
     has each pull's buff-up (who used what) and what each carries (class buffs, fed, elixir); the summary a line a pull.
   - **The raid's feast, the Drain Reach stewpot** (GAME-ONLY dish; the Rusty Spoon is CANON, b1 ch.1:105): `food.reach_stewpot`
     (Items/lowtowns.json), cooked at a fire from Cooking 100 (`recipe.reach_stewpot`: three hound flanks, two flour, salt, sootwort). Set
     down, a pot on a board stands where the cook stands for 60 s, and everyone of the raid who comes within 12 m eats once (ten at most):
     well fed for an hour, Spirit 10 and Stamina 14 (the best party feast's are 8 and 11; its health share at 20 about 8%, inside the
     buffs' 15%). A cook in the raid sets it down at the buff-up; with none carried, the sim with the most coin buys one of Mother Sall at the
     Rusty Spoon. A party's feast buff-up never takes it. Its icon is a recoloured stew's, to be painted.
   - **Her health 185,000** (170,000), for the buffs (up to 15% on a blow, some health).
   **Raid 6 read, raid 7's changes (2026-10-19; WRITTEN, NOT RUN again).** Chris, verbatim: "mare down to 3 spiders per add"; "i guess
   there needs to be a sign or warning where the spikes will pop before hand if they do damage" (then "Yes, with a warning (Recommended)");
   and the new target, **"i want it extremely hard but not impossible"** (2026-10-19): a disciplined raid (holds kept, adds killed in order,
   spikes and grey ground dodged, swaps and harpoons on time) wins about **1 in 4 (25-35%)**, killing her in the last minute before the 9:00
   feast; any sloppiness wipes; never 0% across ten seeds. This replaces the 1-in-2 of Chris's answer 2.
   - **Raid 6** (four pulls, all wipes at 4:10-4:30 with her at 66-70%; brood 30 a pull: killed 6-9, broke free of their holds 38-64,
     eaten 4-9). Why: the holds were broken by the raid's own blows: the Archivist's unwrite kept ticking on a brood after it was held, the
     Ranger's wolf kept biting a held brood, and a re-hold within 18 s lasted half as long and a third not at all (the diminishing returns);
     and the damage-dealers waited on her for the off-tank to grab a held Skull while the off-tank had gone back to her (she blinked onto a
     caster and it was the tank to take her back), so few were killed and the waves piled up (brood up 10-17).
   - **Fixed:** no unwrite on a held one; no pet bite on a held brood; her brood take a hold again and again at full length (no
     diminishing returns on their holds); the off-tank stays on the brood while a wave is up and a tank has her; the damage-dealers wait for
     the off-tank only while it is on its way to the Skull, else they open it themselves. Three a wave (Chris). At about 3,150 health a wave
     and five damage-dealers on one at a time, a disciplined raid clears a wave in about 15-20 s.
   - **The ground spikes are a hazard now:** every 5-7 s a violet crack (a ground mark, the sims step out of it as of any) shows at two
     spots, where one of the raid stands and at her edge, 1.5 s before the shards burst there: 27% of health and thrown up (stunned) 0.6 s.
     Her blink's landing gets the same warning. They never burst on a held brood and hurt only the raid (the brood's holds are the raid's to
     keep). Out of her fight the shards erupt as before, for the look.
   - **Toward one in four:** her health 190,000 (185,000) with three a wave, the buffs, the feast and the spikes in. The run's log now counts
     spike bursts and hits.
   **The tuning batch (2026-10-19; WRITTEN, NOT RUN again).** Chris, verbatim: "the spikes are launching in the air. not on the ground";
   "paladin always seems to be the first one to die every round. something wrong with his kit or armor?", "give the pally some hear. he's
   down again", "gear"; "one class. the shaman needs 1 in raid resurection" (play note 272).
   - **The Paladin tank** (the run's bot, the main tank, first to die in about half the pulls). Three causes, all fixed: (1) its gear: the run
     kitted it as a Paladin damage-dealer (some slots bare, as only the Warrior sim was counted a tank, and none of the tank's training):
     1,111 health to the Warrior sim's 1,303. Now a tank bot wears all seven plate pieces of its level with the sim tank's armour and health
     edges, and never less health than a Warrior sim tank of its level in those pieces; the run's log sets the two side by side. (2) The
     healers put the sim tanks first and you after anyone: a tank you are now healed as a tank. (3) The bot never pressed its defences: under
     60% it uses its Guard and its ward (Aegis) first. The Paladin's kit is not changed.
   - **The spikes on the ground:** each shard is seated on the floor at its spot (the first ground under it, else the zone's), its lowest
     point on the floor, and rises out of it and sinks back into it whole; they had sat at a height taken from her body. A test holds every
     shard's lowest point within 5 cm of the floor. The crown on her back is apart from them (unchanged).
   - **The Shaman's Barrow-wake** (GAME-ONLY, in the Shaman's own voice: the old dead send one back): once a fight (ready again 6 s after
     it), a 3 s call at 30 m that raises a fallen one of the raid at 40% health and a third of its pool. On the bar's 13th slot, from level 10
     (the highest a class's unlock may be), no talent; its icon the Barrow-call's for now. A Shaman sim uses it on the most needed of the
     fallen within reach: a tank, then a healer, then a damage-dealer, never one lying in marked ground. The run's log counts the raisings.
   **Before the next batch (2026-10-19; WRITTEN, NOT RUN).** Chris, verbatim: "all members should have lvl 20 gear", "or at least good gear",
   "that is the point of a raid"; "also should not stay on the ground permanently"; "mage doesnt look balanced now"; "cleric almost twice
   the healing as well"; "how can you tell which is good and which are bad. no way to know to get out or stand in it" (play note 275);
   "nothing should be permanent onscreen"; "all respawned while main spider at half health".
   - **The meter's "over the band" was its clock:** the meter counted fighting time only while you (the bot) were alive; the bot died early
     in most pulls and the sims fought on, so the tune-1 batch counted 687 s of 1,732. By the real time the damage-dealers were at about 74
     (Mage), 56 (Ranger), 51 (Archivist), 48 (Rogue, Scoundrel) a second against the band of 88 (with the time spent on adds, harpoons and
     ground), and the healers about 41 (Cleric), 27 (Shaman), 23 (Druid). The clock now runs while you lie dead. The Cleric's lead is real
     but smaller (it shares a party with the tank you play); the healers' spread rule stays.
   - **The great orange disc** was her unmade ground: seventeen patches for good, run together over the bar. Each patch now lasts 40 s after
     it bites (`unmadeLife`).
   - **One look for all ground** (play note 275): harmful marks red-violet, a jagged cracked edge, pulsing, filling as the warning runs out
     (her unmade ground keeps its grey body under the red-violet edge); helpful ground soft gold or green, a smooth edge, low and translucent,
     a slow shimmer, an ally's fainter and smaller (a Paladin's Consecrate and Colours, a Cleric's Sanctuary, a Salt-Mender's circle, a
     Shaman's rain, a Mage's burning field). A mark must name its kind and its lifetime to be made at all. The sims step out of harmful marks
     only. By your health a red line names the harmful ground you stand in ("Unmade ground: move!"), a green one the helpful ("In
     Consecrated ground").
   - **Nothing permanent:** every mark hides itself past its lifetime or 1.5 s after nothing shows it; dead brood are gone 8 s after they
     fall; her body 60 s after her death; the pot 60 s; everything of her fight is cleared when it ends (her death, a wipe, a reset, her
     sinking). The PlayMode fight test sweeps for leftovers after the feast.
   - **No getting up mid-fight:** in her fight nobody rises but by the Shaman's Barrow-wake; the whole raid down ends the pull; and her
     fight ending unkilled makes her whole (a pull had gone on from 50% with the raid up again).
   - **Raid gear:** the tuning runs dress every member (the bot and the nine) in a rare piece of level 20 in every slot (`SimGear.RaidGear`),
     logged per member; the bot tank has the sim tank's edges and health on top. In play, only level-20 sims with two or more bought
     upgrades (rare in most slots) answer the call to arms (`RaidCall.CallLevel`, `CallGear`).
   **Tune-2 read, and the changes for tune-3 (2026-10-19; WRITTEN, NOT RUN).** Tune-2's first four pulls (rare level-20 gear, buffed, the
   meter fixed): wipes at 7:56 (her at 69%), 6:21 (58%), 9:00 (46%, the feast); brood killed 18, 11, 29 of 39, 30, 45; the bot tank on par
   (1,306 health, armour 460, against the Warrior sim's 1,303 and 473). Chris, verbatim: "hybrids 75% of target"; "people still standing
   in red areas. they need to evacuate"; "some reds under her are staying a long time and pets just standing in it"; "need some type of
   mechanism to trap the brood..someone that can do an extra job" (play note 281).
   - **Hybrids 75%** (recorded in BALANCE_REPORT.md): `HybridDpsShare` .75; the players' hybrid damage paths down by the same step; a tank
     sim 72%, the Archivist (control) 70%. Hybrids today: Warrior, Paladin, Druid, Archivist, Salt-Mender, Cleric, Shaman.
   - **Out of the red:** a sim looks five times a second; when where it would strike from is in harmful ground (her edge lies in her unmade
     ground) it goes to a clear spot within reach first; a step-out looks up to 12 m further when patches have run together; pets step out
     as their keepers do; the bot strikes from a clear spot. The log counts each member's (and pet's) seconds in the red a pull.
   - **Her unmade ground:** 18 s a patch (40), no more than four at once (the oldest goes).
   - **The salt snare** (GAME-ONLY; CANON salt and bone, b1 ch.20:181-205; no machine: a ring of salt on bone stakes): the raid's Trapper
     (the Ranger by default; `/raid trapper` makes it you) lays one on the path of a free brood toward its quarry, one each 8 s, three out at
     most, a salt twist each; it lies 30 s or until a brood crosses it, which stands calcified 12 s (the next blow shatters it). You can lay
     them too: the Salt snare, sold by Tibb Marrow. The log counts snares laid and caught; it now tells holds broken by a blow from holds that
     ran their time out.
   - **The sims' uptime in her fight:** by the real clock the damage-dealers were at 48-74 a second against 88; their blows are x1.2 in her
     fight (`simRaidLift`) for the time the fight takes from them. Expected in tune-3 with the uptime fixes: Ranger, Mage, Rogue, Scoundrel
     about 80-95; the Archivist about 60-65; a Warrior or Paladin tank about 60; healers about 25-40 healing a second each, to be read (the
     Cleric's lead looked real in tune-1: the spread rule stands, the run's band lines now give each healer's healing).
   - **Her health 140,000** (190,000): tune-2's best pull reached the feast with her at 46%; with the snares, the uptime and the lift a
     disciplined raid should reach the last minute. Tune-3 reads it.
   - **Fixed:** the buff-up waits until the last pull's fight is over (tune-2's second pull buffed nobody: the brood still fought).
   **Tune-3 read (0 of 10), the changes for tune-4 (2026-10-19; WRITTEN, NOT RUN).** Two shapes of wipe: fast pulls (4:25-4:56, her at
   18-23%) died in phase 3 to the Unravelling's pulse; slow pulls (8:25-9:00, her 43-55%) drowned in brood (holds ran out 109-143 times a
   pull against 25-34 killed). The damage-dealers by the real clock: Mage 90, Ranger 67, Scoundrel 67, Rogue 62, Archivist 43 (aim 62),
   the Warrior tank 40 (aim 63), the bot Paladin 13 with 15 deaths; healing Cleric 45, Druid 33, Shaman 30.
   - **The pulse** flatter: 1.2% of health a pulse and 0.04% more each (it was 2% and 0.12%: about 2.5% a second by three minutes, more
     than three healers mend).
   - **The brood** were scaled to a group of ten (as any camp mob for a party): far more health than meant, so a wave outlived its holds.
     Her brood are set for the raid as they are now (about 1,050 at 22: a few seconds each under the raid's damage).
   - **The pets** stood in the red while their keeper lay dead (the Ranger's wolf 40-130 s a pull): a pet leaves harmful ground whatever its
     keeper's state.
   - **The healers** mended in a fixed order and the Cleric's came first to every most-hurt member (the meter counts only what heals): each
     healer's wait between mends now varies a little, so none is always first.
   - **The spikes** are warned 2 s (1.5).
   - **The hunt** looks only within 25 m of her (the PlayMode test's you, standing off on the apron, could be hunted and never ran).
   - **Her health 175,000** (140,000): the fast pulls had her at 18-23% by 4:50 and died only to the pulse.
   - **The log** now gives the last blow on each death ("last hit: The Unraveller 410", "Spikes 352", "Weave-Eater brood 96"): the bot
     Paladin's deaths are to be read there before anything of its is changed. The band lines say the shares in use (hybrid 75%, Archivist
     70%, tank 72%).
   - **Tests fixed:** "a broken file plays the defaults" compares with the defaults (not 160,000); the body's height test allows the
     spider's own 3.4 m (the crown blinks, the spikes erupt: often neither is up).
   **Tune-4 read (1 of 10: the kill at 7:12; near misses with her at 8%, 12%, 16%, 20%, 24%), the changes for tune-5 (2026-10-19;
   WRITTEN, NOT RUN).** The add phase works now (holds ran out 0-9 times a pull, most brood killed). The bot Paladin's deaths: "last hit:
   Unmade ground 457-627": it stopped at her edge in her grey ground, stepped out and came back to the same spot. Now, with marks about her,
   the bot stands on the nearest clear spot of her edge, and uses no ability while it is still in the red. Every pull buffs (pull 2 had
   not: the session was still in a fight with a straggler; the run now lets anything on the raid go and waits for the quiet). Her health
   165,000 (175,000), toward three kills in ten. The summary gives each member's damage (and healing) a living second beside a fighting
   second (wipes counted dead time against everyone).
   **Tune-5 notes (2026-10-19; WRITTEN, NOT RUN).** Chris, verbatim: "i think instead of the mobs floating but them on the ground!", "but
   make the main spider about 15% bigger", "still not moving out of red...it really looks pink so make it a deeper red".
   - **On the ground:** her spider and each brood spider are sized by their posed mesh and stood on the floor under them (the walkable
     ground: the bar's top, the apron), looked at twice a second as she is hauled, her rising and sinking on top of it; the crown sits on
     her back from the floor up. They had been sized and seated from the renderers' bounds before the first frame, and hung in the air. A
     PlayMode check: each spider's lowest point within 10 cm of the floor under it.
   - **15% bigger:** about 3.9 m to her back and her legs to about 11.5 m; her body's reach for the melee grown with her (2.9 m).
   - **Deep red:** harmful ground is a deep red now (#B0141E, its edge #6E0A10), not pink; the jagged edge kept.
   - **Out of the red:** her unmade ground is laid under one of the raid standing off from her (a ranged member or a healer), not where she
     floats, so the melee keep a clear edge round her; a sim leaving the red drops a mend that would not land within 0.3 s; a pet that finds
     no clear spot goes to its keeper when its keeper stands clear. (Tune-5's first pull: the Ranger's wolf 65 s in the red, the Ranger 23,
     the Warrior tank 19, the Rogue 11, most of it the warnings' seconds while walking out.)
   **The Leap (play note 285, 2026-10-19; WRITTEN, NOT RUN).** Chris, verbatim: "the spider needs an attack animation. maybe a jump mechanic
   to get away and release her brood"; "if she lands on you after jump its an insta death".
   - **Her strikes** play the spider's attack1/attack2 at once on each swing (stopping the walk or idle, a little slowed to be seen); the
     brood's bites the same. The feet's settling keeps the planted legs down.
   - **The Leap** (GAME-ONLY): at 75%, 50% and 25% of her health (not while she swallows, feeds or stands calcified: as soon as she is free)
     she tears free of the harpoons and leaps 12-18 m to another part of the bar or the apron (never the water): two red rings where she
     will land, 2 s before: the outer (5 m) and, solid, her body's own (about 2.9 m). Over 0.9 s her body goes up an arc of about 4 m (the
     jump clip). Anyone under her body as she lands dies outright (past the 60% cap; the Shaman's Barrow-wake can raise them); anyone else in
     the outer ring takes 35% and is thrown 4 m. A brood wave of three rises in a ring 7 m round where she lands. The raid moves: the main
     tank takes her up again, the harpoons pin her afresh, the Trapper, the control and the kill order meet the new wave. The sims step
     out of the rings as out of any red (the inner first: the deepest). Numbers in the data file (`leapAt1-3`, `leapWarn`, `leapSeconds`,
     `leapRadius`, `leapShare`, `leapKnock`, `leapRangeMin/Max`); the log counts leaps.
   - **Her health 160,000** (165,000) for the time the leaps cost.
   - **A 10-15 minute kill** (play note 286, 2026-10-19, WRITTEN, NOT RUN). Chris: "needs to be a 10-15min kill at least". GAME-ONLY
     numbers (her lore is unchanged). The feast (enrage) now comes at
     **16:00** (was 9:00). **Her health 340,000** (was 235,000): tune-6 dealt her about 500 a second; a longer fight with more brood waves
     and leaps puts about 430 a second on her, so some 13 minutes. The leaps stay at 75%, 50% and 25%; the brood waves keep coming
     as before. **The pulse grows 0.03% a pulse** (was 0.04%): 3% of health three minutes in, 6.6% at nine, so a good raid lives to
     its kill around minute 11-14 and a slow one dies to the pulse or the feast by 16. Target unchanged: sims alone kill her 25-35% of
     pulls. The run tool's pull limit is now 17 minutes, and `group_run.ps1 -Raid` waits 600 s + 420 s a pull (game at 3x) unless
     `-TimeoutSec` is given. Numbers in the data file (`health`, `enrageSeconds`, `pulseGrow`); tests pin them.
   - **Tune-8 and play note 290** (2026-10-19, WRITTEN, NOT RUN). All GAME-ONLY fight rules; the bone harpoons ("petrified leviathan bone")
     and their "trailing glowing ropes of amber light" are CANON (book1 ch.20).
     - **The Leap readable** (Chris: "leaps are too quick and not properly telegraphed"): the rings come down 3.8 s before she lands, the
       outer one filling, her body's own solid. She gathers 1.8 s first (stands, draws down low, rears back; "The Unraveller gathers to leap!"
       in raid warning, a screech line in chat), then 2 s in the air on an arc 6 m high, her shadow growing and darkening where she will
       land; dust thrown up and the view shaken a moment when she lands. Nothing is struck until she lands. The sims step out from the
       rings' first moment.
     - **The Leap far** (Chris: "also distance is too short", "still not jumping very far. need to jump out of melee and ranger range"):
       35-55 m, and at least 35 m both from where she took off and from the raid's middle (the longest reach of any attack in the game is
       30 m; 5 more). A bug had sent every leap to the bar's middle (the ground check never passed); fixed. The apron now runs on north along
       the bank to 56 m (z 177-233) so such a landing always exists on the bar or the apron, never the water; Tibb's barrel stays where it was.
     - **Time to move** (Chris: "yeh still not enough time to move after telegraphing"): every mark's warning is at least twice the time
       to run from its middle out of it (run speed 5.2 m/s); spike cracks 3 s, unmade ground 3 s, her hunt 4 s, the leap 3.8 s. A test holds it.
     - **The 40% mark** (Chris: "not sure what took everyone out around 40% mark"): unmade ground under the ranged and healers bit 15% at
       once and more each second; now 6%, then 6% more a second. Phase 3 comes in with "Phase 3: The Unravelling" in raid warning and 6 s
       of quiet: the pulse after it (low, as before), the hunt after 12 s, spikes and unmade ground not before 6-8 s.
     - **Pets** (Chris: "i think the pet pulled the mob or was still alive and restarted the raid"): a pet goes with its keeper when they
       fall (out of every foe's threat) and comes back when they are up; yours stands out of the fight while you are down. A wipe is the
       raid down; pets never count. Before the pull (and after a wipe's reset) no sim or pet strikes her until you or a tank pulls her.
     - **Seen harpoons and salt** (play note 290: "i dont see the salt or harpoons is that a visible item?"): a pale bone harpoon flies
       from the thrower's hand in an arc and sticks in her side, an amber rope from it back to the thrower while its pin lasts (one a pin;
       taut and brighter while she is hauled; fading as the pin ends). Salt: a white handful from the hand and a puff where it lands. Tibb's
       barrel and Ketta's rack stand out on the apron for every fight; a sim out of what it throws walks to them for three more. All
       cleared at the fight's end. (Not yet: an item shown in the hand before the throw.)
     - **The meter** (Chris: "healing seems lower..cant tell with dps"): totals shortened (155.4k), the columns wider, and the rate is a
       second while that member was up (dead time no longer drags the wiped down).
     - **The camera** (Chris: "camera gets inside the spider alot hard to see"): her body and her brood's stop the camera short; at her
       legs it looks down over her (pitch at least 28 degrees). The run's bot uses the same camera.
     - **Out of the water** (Chris: "when she is in the water hard to see the telegraphs. try to keep her out of water"): the harpoons
       now haul her toward the apron, away from the river (they hauled her toward its middle); her blink lands short of the water or not
       at all; pushed into the river any way, she is moved back onto the dry. Marks over water lie on its surface.
   - **Tune-9** (2026-10-19, WRITTEN, NOT RUN; GAME-ONLY).
     - **Waking by the raid** (Chris: "not sure where i respawned but not near the raid", "and stuck"): in a raid in the Lowtowns, while
       she is up and for 5 minutes after her fight, you (and the fallen who wake with you) wake on the apron across from the bar, not at the
       zone's camp far to the west. Nobody gets up while the raid fights her: Recover waits until a wipe or her death (getting up mid-fight
       had brought five fallen sims up with you, far off: the log's "4/9" to "9/9" at 7:30-8:00). The run's bot, stuck 10 s far from her,
       is put back beside the raid.
     - **Unmade ground still killing**: the bites of 300-480 were 5-7 s stood in it (6% then 6% more a second). The log now says the
       seconds ("Unmade ground (5 s in it) 392"), and a sim still in it a second after a bite goes straight out, whatever it was doing.
     - **The Feeding's growth**: one landed Feeding on a fully buffed raid ate 38 good effects (77% heavier blows and 11% of her health
       back at once). Now at most 8 count a Feeding (16% heavier, 2.4% back); `feedEatenMost` in the data file.
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
