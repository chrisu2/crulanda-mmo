# The first world boss: the Pale Auditor (a 10-player raid in the Lowtowns; design, PROVISIONAL)

Written 2026-10-19 from Chris's brief (below), the books (`D:\code\crulanda`) and the code as it stands on `feet-bow-adit-kit`. Nothing here is
built. Citations: `b1 ch.N:L` is `D:\code\crulanda\book1\chapter_N.md` at line L (b2, b3 the same for books 2 and 3); `WB:L` is
`D:\code\crulanda\world_bible.md`; `B1WB:L` is `book1\world_bible.md`. Labels: CANON (the books say it), CANON-EXPANDED (the game fills in
round a canon thing), GAME-ONLY, PROVISIONAL (to be settled later). No machines anywhere in it (CRULANDA_CANON; Chris 2026-10-11).

## Chris's brief (2026-10-19 ~11:00, verbatim; ROADMAP.md "The first world boss")
"i'd like to have one mini world boss in the highest zone. the mob is level 30 but needs 10 people to kill. has to be a raid. so id like to
balance it but definite on the hardest skill level to kill. drop components for legendary weapons for each class, but only one piece will
need other world bosses at end game to complete".

What this changes from the ROADMAP: world bosses were "a full group" at the cap of 30, and a raid was twenty players (the Spire). This one is a
**10-player raid** out in the open world **now**, in the highest zone (the Lowtowns, 15-20, cap 20), and its mob is level 30.

---

## Questions for Chris (recommended answer first; answer "all recommended" to take them all)

1. **The boss:** the Pale Auditor, an avatar of a Pale King come down into the Lowtowns (books 1 and 2)? *(Recommended.)* Or a giant Weave-Eater
   risen from the Chemical River; or an Iron Teeth war-boss.
2. **Where:** it lands in the Shard-Burners' Yard every time? *(Recommended; a second and third landing place later.)* Or it picks one of three
   places (the Yard, the Drain Courts, the Gate Ward's carters' yard).
3. **How often:** it lands every 3 hours of play, with a 5-minute warning, and stays 45 minutes? *(Recommended.)* Or every third game night; or at
   fixed evening hours on the real clock.
4. **Level 30 against 20:** it shows as level 30 (a skull); the game already counts any gap over 5 levels as 5 (it hits 60% harder, takes 30%
   less), and the hardness comes from the mechanics? *(Recommended.)* Or count the full 10 levels for raid bosses.
5. **How hard:** a sims-only raid in the best Lowtowns gear wins 1 time in 3; you playing well with them, about 1 in 2? *(Recommended.)* Or
   harder (1 in 5); or easier (2 in 3).
6. **The raid:** you and nine sims in two parties of five: 2 tanks, 3 healers, 5 damage? *(Recommended.)* Or 2 / 2 / 6.
7. **Loot for ten:** components are your own (every member rolls their own, nothing to win off the sims); the boss's gear goes by need and greed?
   *(Recommended.)* Or everything by need and greed.
8. **The legendary weapon:** forging the Auditor's three components makes the weapon now as an epic at level 20 (no special effect), and the
   last piece from the end-game world bosses wakes it to legendary? *(Recommended.)* Or nothing usable until all four pieces are in.
9. **The smith:** Tynara Embercoil, the Forgeborn who shapes metal (book 1), at a quiet forge under the Forge District's wall? *(Recommended.)*
   Or a smith of our own; or the goblin Weavers.
10. **The Shore's planned "Pale King's shadow":** keep it as a later world boss (a different Pale King)? *(Recommended.)* Or drop it now the
    Lowtowns has one.

---

## 1. The boss: the Pale Auditor

### 1.1 What the books give (read for this doc)
- **The Pale Kings** are the Void's things from "the Outside", come through the Fracture "drawn by the raw magical bleeding" (CANON, b1 ch.15:77).
  The world bible's rule: "Lean into the 'Pale Things' as cosmic entities/auditors, not just monsters" (CANON, WB:11).
- **An avatar of one** stands in Aethelgard's office: "impossibly tall. Draped in shadow. Wearing a featureless mask of pure gold that possessed
  absolutely no eyes" (CANON, b1 ch.12:375-377). "Its terrible voice wasn't an audible sound; it was an uncomfortable vibration deep in the teeth"
  (ch.12:381). It wants the world as "a perfect, pristine museum piece. We highly look forward to adding it to our vast collection" (ch.12:399).
- **What they are made of and how they land:** "geometric nightmares composed of jagged, shifting white crystal and incredibly hateful, localized
  gravity" (CANON, b2 ch.13:19); they come down out of the sky's "bruised-purple scar" (b2 ch.13:13); "Where it landed, the solid ... stone ...
  simply ceased to exist, replaced by a perfect, smoking crater of un-reality fifty feet across" (b2 ch.13:21); struck hard enough, "their
  crystalline structures" shatter "sending deadly shrapnel raining down" (b2 ch.13:25); a grown one throws "the direct, deliberate unmaking beam"
  (b2 ch.15:91).
- **Why the Lowtowns:** "we foolishly, loudly announced exactly that we were a massive, unguarded, incredibly rich feast" (b1 ch.14:63); the
  Spoke "will reach down into the Lowtowns—right here—and it will harvest our souls to appease the Pale Kings" (b1 ch.24:162). The Council's
  geodes are Lowtown souls, faces "frozen" in violet glass (b1 ch.3:133-137); the poor burn their dead for shards (ZONE_LOWTOWNS.md section 1,
  GAME-ONLY reading).
- **The warning:** the High City's brass horns, "a piercing, deep, two-tone wail" (CANON, b1 ch.3:243); the Seekers' "featureless masks of gold"
  (b1 ch.1:241) answer the Auditor's mask (the game's reading: the Council copied it; GAME-ONLY, never said aloud).

### 1.2 The boss (recommended)
**The Pale Auditor** (CANON-EXPANDED: an avatar of a Pale King, b1 ch.12:375-403; its name from WB:11's "auditors"; the fight GAME-ONLY).
Level 30, a raid boss (a new rank above elite: a skull on its plate and "Raid (10)" under its name). Four times a man's height: a tall shape of
shadow in a cloak that falls like smoke, the gold mask with no eyes, and under the cloak a frame of jagged white crystal that shifts as it moves.
It floats a hand's breadth off the ground; where it stands the setts go grey. It does not speak aloud: its lines come as a hum (the chat shows
them in its own pale colour, the screen shakes a little).

Why it is level 30 in a zone of 20: it is not of this world. The zone's mobs stay 15-20; nothing else changes.

It has come to count the Lowtowns: what is worth adding to the collection. Its lines (GAME-ONLY, in the books' voice):
- On landing: "The ledger is open. Let us see what this place is worth."
- Phase 2: "Hold still. The collection keeps what is still."
- Phase 3: "Silence is the only perfect state."
- On a wipe: "Noted. Of no value." It rises back into the tear.
- On death: the mask falls and rings on the stones; the crystal shatters (b2 ch.13:25); "...the audit... is not... closed."

**Options (Chris's call, question 1):**
- **A. The Pale Auditor** (above). *Recommended:* the books' own cosmic enemy, it explains level 30 in a zone of 20, and the Lowtowns is
  exactly what the books say the Pale Kings hunger for. A Pale King is never killed for good in the books, so this is an avatar: it can come back.
- **B. The Drowned Hunger:** a Weave-Eater grown vast in the Chemical River (CANON-EXPANDED: Weave-Eaters are "manifestations of the Wasting
  itself", b1 ch.20:115; the Lowtown mothers' tale, B1WB:121). Fits the zone, but the Lowtowns already has the Mother of Drains (elite 20) and
  the Rim was promised "the Weave-Eater brood's mother" (ROADMAP), so it would be the third big Weave-Eater.
- **C. An Iron Teeth war-boss** (CANON faction, b1 ch.7:21): a man; level 30 and ten players would read wrong for one smuggler.

### 1.3 Where it lands
- **The Shard-Burners' Yard** (south-east; the Lowtowns' dead-house, ZONE_LOWTOWNS.md section 2) *(recommended)*: it comes for the burning of
  souls. The yard must hold ten players, the shards and the craters with room to move: about 40 m across of open, level ground. If the yard is
  smaller (to measure in the build's first step), the landing clears its sheds (they come back when it leaves).
- **Later (question 2):** two more landing places the boss picks between, so it is not always the same walk: the Drain Courts' biggest court
  (north-west) and the Gate Ward's carters' yard (west).
- The villagers clear out when the horns sound (Rule One, "If you see a Seeker, exist somewhere else", b1 ch.1:257, carried over); the camps
  within 40 m of the landing are emptied while it is down (the fight must be the Auditor's, not a pull of hounds).
- When it lands it leaves **a crater of un-reality** 15 m across at the yard's middle (b2 ch.13:21; "fifty feet"): grey static ground, not
  standable (it hurts; section 3). It stays a game day after the Auditor goes, then the setts come back.

### 1.4 Spawn timer and the announcements (question 3)
- **Recommended: every 3 hours of play** (counted in any zone, saved), landing at a random point in the next 30 minutes after the 3 hours are
  up. It stays **45 minutes** or until it is killed or wipes the raid twice. A kill or a departure starts the next 3 hours.
- **The lockout is the game's own (already built):** an open-world boss is looted once a week by each character (`Lockouts.WorldBoss`, seven
  days to the 6 am reset; Chris, 2026-10-15: "open world boss is once per week"). While you are locked out it still comes: you can fight it for
  the coin, standing and the Chronicle's line, and you roll no components (the raid window says so before the pull).
- **Zone chat (the warning, 5 minutes before):** a System line in the Lowtowns, "The sky over the Lowtowns is tearing. The horns of the High
  City are sounding.", the horns' two-tone wail (b1 ch.3:243; sound when sound exists), and the sky's bruised-purple scar opening over the
  south-east (b2 ch.13:13). Sims in the zone talk about it in their own voices ("Auditor's coming, who's in?", "LF raid, need heals"), as
  `SimChatter` does today.
- **On landing:** System, to every zone (the General line): "The Pale Auditor has come down in the Shard-Burners' Yard of the Lowtowns."
  The map shows a skull at the yard.
- **On a kill:** System, every zone: "<leader> and <guild or 'their raid'> have driven the Pale Auditor back into the tear." The Chronicle
  marks it for every member (ROADMAP "Endgame at the cap": the Chronicle marking each kill). On a departure: "The Pale Auditor rises back into
  the tear. The Lowtowns have been counted."
- **When you are not there (PROVISIONAL, question for later):** the sims' guilds try it on their own while you are away (the offscreen world,
  `SimPopulation.LiveAway`): one try in three wins; the line comes over the General channel. It changes nothing of yours, only the world's feel.

---

## 2. How a 10-player raid forms

### 2.1 What exists (code, 2026-10-19)
- A party is five: you and up to four sims (`EncounterSession.MaxPartySims = 4`, `InviteLevelGap = 5`). Roles are Tank, Healer, Damage
  (`SimCompanion.Role`); the dungeon queue ("Gather a Party", `EncounterSession.Queue.cs`) gathers sims of your level by role and summons them.
- Guilds exist (`EncounterSession.Guild.cs`, chat, tags, joining); Zone chat and System lines exist (`ChatChannel`, `SimChatter`).
- Group scaling (`EncounterEnemy.GroupScale`): each party sim adds a share of a mob's health and blows. Lockouts exist, including the world boss's.
- No raid: no group of more than five, no raid frames, no rule for ten-way loot.

### 2.2 The raid group (recommended)
- **A raid is two parties of five** (Party 1 and Party 2), ten in all: you and nine sims. "Form raid" turns your party into Party 1; new members
  fill Party 2. Each party keeps the five-player rules it has today (party-wide heals and buffs reach your own party; raid-wide ones reach all
  ten, tagged so in the ability data). You lead the raid (or a sim leads, as in a sim-led run, when you join someone else's).
- **Roles (question 6): 2 tanks, 3 healers, 5 damage** *(recommended)*: one tank and one or two healers in each party. With 2 healers the
  numbers have no room for a single mistake, which is too hard even for "the hardest"; with 3 the healers carry the fight's spikes and the
  damage check stays tight. You fill any role; the call fills the rest round you.
- **Gear and level gate:** sims of 18-20 only (the call asks for 18+; `InviteLevelGap` is 5 today, so it is narrowed for the raid); the raid
  window shows each member's gear level (the mean of their pieces) so you can see a raid that cannot win.

### 2.3 Calling the sims
- **The call to arms** (a button in the raid window, or `/raid call`): you post in Zone chat, "LFM Pale Auditor, need 1 tank 2 heals 4 dps"
  (built from what the raid lacks). Answers come over 20-60 s, by role:
  1. **Your guild first:** guild members of 18+ who are online come wherever they are (they travel; a minute or two), as friends do today.
  2. **The Lowtowns next:** sims online in the zone answer by their boldness (`bold`), level and role; the timid stay away from a level-30
     thing (a bold-under-.3 sim declines with a line).
  3. **Friends anywhere:** a friend of 18+ always answers if online (`SimMemory` Friend).
  The queue's summoning is **not** used for the world boss: it should feel like the zone rallying, and an empty zone at 3 am is part of it
  (the window says "Not enough of the Lowtowns' adventurers are about. Try your guild, or wait for the evening.").
- **Sims' own raids:** a sim guild leader can call one too, and you can answer it (you join their raid; their leader runs the fight's calls).

### 2.4 What the raid UI needs
- **Raid frames:** two columns of five (one per party), compact: name in class colour, a health bar, a role mark (shield, cross, blade),
  a mana sliver for healers, and one small pip for each of the fight's marks on that member (Weight stacks, "Struck from the Record",
  "Collected"); click to target, as the party frames do. The party frames hide while a raid is formed.
- **A boss frame** with the Auditor's health, its phase name, its cast bar (to interrupt), and the fight's clock against the enrage.
- **Raid warnings:** a large centred line for the moments that kill ("The Auditor looks at YOU", "Break the crystal!"), the same style as
  the existing toasts, held 2 s.
- **A ready check** (everyone answers in 3 s; sims answer at once unless they are drinking) and **the marks** the game has (the held and
  marked targets the sims respect) for the shards in phase 2.
- **The raid window** (from the queue's window): the roster by party, roles, gear levels, your lockout ("You will roll for components" or
  "Locked until <day>: no components"), the call button, and leave.

### 2.5 Loot for ten (question 7)
- **Components: your own roll** *(recommended)*: every member who is not locked out rolls their own component drop off the body (section 4);
  nothing you can win or lose to a sim. Sims get theirs too (they carry them in their gear story; the who list can show a sim's legendary
  later). This keeps the bad-luck protection honest: it is yours.
- **The boss's gear** (a short list of epics for 18-20, its signature pieces, section 4.5): two pieces a kill by **need and greed** among the
  ten (`PartyLoot`, as dungeons do), with the sims' choices by their class and what they wear.
- **Coin and standing** to everyone (Lowtowners' standing: it drove the thing off their dead).
- *Option:* everything (components too) by need and greed among the ten. Rejected as the default: losing a once-a-week component to a sim is
  a bad feeling with no upside in a single-player game.

---

## 3. The fight

### 3.1 Level 30 against a raid capped at 20 (question 4)
The game's level math as built (`EncounterEnemy.cs`, `EncounterEnemy.Elite.cs`): there are **no hit, miss, crit or resist rolls** by level;
a level gap is two multipliers, how much harder the mob hits and how much less it takes. An elite with a move hits 12% harder and takes 6% less a
level over its foe (a plain mob 25% and 10%), and **both stop at 5 levels** already: a +10 elite hits x1.6 and takes x0.7, the same as +5. No
single blow can take more than 60% of a member's health (`MaxBlowShare`). Its con colour is Deadly (over +5, `LevelCon`).
**Recommended:** keep it that way. The Auditor **shows** level 30 (a skull) and the existing clamp treats it as +5 (x1.6 hits, x0.7 taken: the
health in 3.2 is the health it has after that). The hardness comes from the mechanics and the numbers below, not from a level table. Its
blows that must kill (a crater opening on you, the beam, the enrage) are marked to pass the 60% cap (a flag on the move), every other blow keeps
it. When the cap rises to 30 the real gap shrinks and it becomes an older, easier fight, as old world bosses do (or it is raised then).
*Options:* lift the clamp for raid bosses so +10 counts in full (x2.2 hits, x0.4 taken: every number then tuned round that, with no gain in
feel); or a hidden "??" level.

### 3.2 The numbers to balance (first targets; set by the harness, section 3.6)
Measured shares, not hand-tuned guesses. From BALANCE_REPORT.md: at level 19-20 a pure damage-dealer does about 60-70 a second on a boss, a
hybrid 90% of that, a tank 75% of the pure band (`TankDpsShare`), a healer sim about 20%.
- **The raid's damage:** 5 damage (about 330 a second) + 2 tanks (about 100) + 3 healers (about 40) = about 470 a second with everyone up.
  The mechanics take about a quarter of the time away (moving, breaking crystal, killing shards), so **about 350 a second** lands on the boss.
- **Health: 160,000** (no group scaling: the boss opts out of `GroupScale`, its health is fixed for ten). At 350 a second that is 7 minutes 40 s.
- **Enrage: 9 minutes** (the hard limit, below). A clean raid has about 80 s to spare; one damage-dealer dead from the 4-minute mark costs about
  60 s; two dead early is a loss. That is "the hardest skill level": no slack for standing in things.
- **The tank's blow:** every 2.2 s, about 22% of a level-20 tank's health after armour (about 280-320 on today's tank of about 1,300-1,400 with
  gear). With Weight (below) it climbs to about 40% at 5 stacks, so tanks must swap.
- **The raid's damage taken:** about 85% of the three healers' measured sustained healing over the fight, with spikes (the Hum, the beam) that
  need two healers at once. Healers' mana must just last 9 minutes with one drink-free fight (no mana in hand at the end of a clean kill).
- All these numbers are GAME-ONLY and sit in one data file (`EncounterContent/Raids/pale_auditor.json`) so the harness can tune them.

### 3.3 Phases and mechanics (built from what the game has)
The game's boss pieces today (DUNGEON_DESIGN.md): holds (a root or stun within reach on a timer), heavy blows that knock back, ground marks,
rallies and adds, the Count-Master's stone shell ("Set in Stone": a member held in a shell the party must break), blinks, interrupts, enrage
at a health mark, the Weave-Eaters' rules in the Lowtowns (the lighthouse, wards eaten, harpoons stop the blink, salt calcifies). The
Auditor uses those, scaled to ten, with three new pieces (Weight, the crater ground, the beam), each small.

**Phase 1, "The Count" (100% to 70%)**
- **Weight** (new; "incredibly hateful, localized gravity", b2 ch.13:19): each of its blows on its target adds a stack, 20 s: 6% more damage
  taken and 5% slower a stack. At 4-5 stacks the other tank must taunt (`EncounterThreat.Taunt`) and the first lets the stacks fall off.
  No swap: by 7 stacks the tank dies. *Tests the tanks.*
- **Struck from the Record** (the crater ground; b2 ch.13:21): every 18 s it marks two members who are not tanks (a pip on the frames, a raid
  warning to the marked); 5 s later a crater of un-reality 6 m across opens where each stands: 70% of health on whoever is in it when it
  opens, and the grey stays for the rest of the fight (8% of health a second inside it). The marked must walk to the yard's edge before it
  opens. Badly placed craters eat the floor; this is the fight's slow squeeze. *Tests where everyone stands.*
- **The Hum** (an interrupt; "an uncomfortable vibration deep in the teeth", b1 ch.12:381): a 2.5 s cast every 14 s. Interrupted, nothing.
  Not interrupted: 30% of health to all ten and a 3 s silence. Each interrupt makes it immune to the next for 10 s, so one kicker cannot keep
  up (the game's interrupts are on 10-15 s cooldowns): **the raid needs two or three interrupters taking turns.** *Tests the rotation.*

**Phase 2, "The Collection" (70% to 40%)** (the Hum carries on; Struck from the Record every 24 s)
- **Pale Shards** (adds; "deadly shrapnel raining down", b2 ch.13:25): every 30 s three shards of white crystal (level 25, about a fifth of a
  camp elite's health each) break off and drift toward **the healers** (the Weave-Eaters' lighthouse rule: whoever works magic draws them,
  `WeaveEaterRules`, ch.20:121). A shard that reaches the Auditor is taken back in: it heals 3% and its blows grow 10% (stacking, for the
  fight). The off-tank picks them up; the damage-dealers swap to them (marked targets); crowd control holds them; **the Lowtowns' salt calcifies
  them for 4 s** and **a bone harpoon stops their drift for 12 s** (the zone's consumables from Tibb Marrow and Ketta Rune: CANON, b1
  ch.20:191, 205). A raid that brings salt and harpoons has it easier: the zone's lesson pays off at its top.
- **Museum Piece** (the shell, as Set in Stone; "a perfect, pristine museum piece ... our vast collection", b1 ch.12:399): every 35 s one member
  who is not its target is closed in white crystal. They cannot act and take no harm. The shell has about a camp elite's health; break it within
  12 s and they step out. Not broken: **they are collected** (dead, no corpse to raise until the fight ends). *Tests the target swap.*
- The tank swap on Weight carries on.

**Phase 3, "The Silence" (40% to 0%)**
- **The Silence** (the soft enrage): a pulse to all ten every 3 s, 1.5% of health at first and 0.1% more each pulse (at 3 minutes in the phase,
  about 7.5% a pulse). The healers' mana runs out on a slow kill.
- **The Unmaking Beam** (new; "the direct, deliberate unmaking beam of a full-grown Pale King", b2 ch.15:91): every 20 s the mask turns to a
  member (the raid warning "The Auditor looks at YOU"); 2 s later a line 4 m wide and 40 m long from the boss through where they stood: 80% of
  health to everyone in it. The raid stands in **two spread groups** (one per party, at marked points either side of the boss, as the tanks
  hold it in the middle): the looked-at member steps clear of their group, and nobody else stands in the line.
- Struck from the Record every 30 s (the floor is already small).
- **Hard enrage, 9:00 from the pull: "The audit closes."** The crater swallows the yard: everyone left is unmade (killed). It rises back into
  the tear. The raid can try again while it stays (45 minutes), from the yard's edge (the run back is short: the graveyard is the Lowtowns'
  own; a wipe twice sends it away).

**What makes it the hardest skill level (and fair):** every mechanic is shown before it kills (a mark, a cast bar, a warning, a pip), every one
has a counter the game already teaches (move, interrupt, taunt, swap targets, salt, harpoons), and the numbers leave no room for a missed one in
phase 3. Nothing is random-unfair: the marked are told, the beam is aimed at someone who is told.

### 3.4 How the sims handle it (what exists and what is new)
- **Exists** (`SimCompanion`, `SimCompanion.Fight.cs`): healers and archers step out of a warned hold ring (`EliteMove.ccWarn`); sims who can
  interrupt stop a cast in reach (one every 12 s each); sims strike the Skull mark first, then your target, and leave crowd-controlled mobs
  alone; Rogue and Archivist sims hold the Moon mark; tanks taunt and peel (`EncounterThreat.Taunt`); healers heal the most hurt; the
  Count-Master's shell is broken by marking it.
- **Not there yet:** no sim steps out of a ground mark (`BlowMark`) or a trick's area. That is the biggest new piece for the sims, and every
  ground mechanic below needs it.
- **New, each small:**
  - **Out of the ground marks:** any sim inside a warned ground area walks to the nearest safe spot (outside every mark and every crater)
    before it lands, and comes back after. Shared with the dungeons (their heavy blows' marks gain it too).
  - **The tank swap:** the off-tank taunts at 4 Weight stacks on the main tank; the main tank stops taunting until its stacks fall.
  - **The interrupt rotation:** the raid's interrupters take turns in a fixed order (the leader sets it: the two or three with the shortest
    interrupt), skipping one whose interrupt is down. You can be in it (your turn shows on the boss frame).
  - **The marked walk out:** a marked sim walks to the nearest free spot of the yard's edge (away from craters already there) and comes back
    after.
  - **Shards:** the off-tank picks up shards; damage-dealers in Party 2 switch to them; a sim with salt or harpoons in its bags (they buy them
    from Tibb and Ketta when a raid is called) throws them at the shard nearest the boss.
  - **The beam:** the sims stand at their party's point; the looked-at sim steps 6 m sideways; the others stay out of the line.
  - **Mistakes on purpose:** each sim has a small chance to miss a mechanic by its personality and level (a careless 18 more than a careful
    20), so a sims-only raid can fail as a real one does; the player's own play is what tips it. Set from the harness's win rates.

### 3.5 What else the fight touches
- **Group scaling off** for this boss (fixed numbers for ten); the shards scale by the raid's size like any add.
- **The lockout:** the kill locks the character for components for the week (`Lockouts.WorldBoss`, already built).
- **Deaths:** a fallen member can be raised in the fight by the classes that can; otherwise they run back from the Lowtowns' graveyard
  (a short run on purpose; nobody joins mid-pull: the yard is sealed by the crater's ring while the fight is on).
- **Feel:** the sky's scar over the yard, the grey crater, the shards' white glitter, the mask's gold, the hum on the screen. The camera shake
  stays small (Chris plays long sessions).

### 3.6 How we test it
1. **EditMode rules tests** (no scene): each mechanic's numbers and timers (Weight stacks, crater damage, the Hum's immunity window, shards
   healing the boss, the shell's timer, the beam's line, the Silence's growth, the 9-minute enrage), the level cap on the tables, loot and
   bad-luck protection, the save round-trip.
2. **PlayMode fight tests:** a raid of ten at 20 in the Lowtowns' gear on a fixed seed: the swap happens, the marked walk out, the rotation keeps
   the Hum down, the shells are broken, the beam hits only the looked-at.
3. **The group run tool at 10** (`group_run.ps1`, with a raid option): you (the bot) and nine sims at 20 in the best Lowtowns gear (tier 6
   crafted and the zones' rares), the HUD, meter and scorecard on, ten pulls on ten seeds, on a quiet machine. **Targets (question 5):**
   sims-only raid wins 30-40%; with the bot playing perfectly about 50%; a raid in plain uncommon gear under 10%; a raid of 2 healers
   under 15%. Each loss read from the log around the first death (memory: read the wipe first) before any number is changed.
4. **Chris's run:** a preview build with its own save folder (`--crulanda-save-dir`), a level-20 character in good gear, the call to arms and a
   kill. Nothing is done until he has played it.

---

## 4. The legendary weapons

### 4.1 What the books give
- **Glass-steel**, "a lost art of the First Era" (CANON, b1 ch.7:149): Veyra's sword *Truth-Taker* hums "a vibration Valen could feel in his
  teeth" (b1 ch.17:25), glows white, and leaves "searing, burning afterimages of gold" (b1 ch.10:67). The game's legendaries are glass-steel,
  remade: that is why they need a Pale King's crystal (the same hum; GAME-ONLY reading).
- **Named weapons and their kind in the books:** Baston Iron-Hand's war hammer "so incredibly dense it possessed its own minor gravitational
  pull" (b2 ch.13:23), Baston as the Iron Anchor (WB:19); the Ash-Walkers' bone harpoons on amber ropes that "actively resisted the Unmaking"
  (b1 ch.20:181); the Glass-Archers' "salt-tipped shafts" and their "splash of emerald light that neutralized the violet void" (b3 ch.8:36);
  the Praetorian's "double-bladed Sun-staff" that "hummed with concentrated solar magic" (b1 ch.26:69); the Root-Mother "encased in
  amber-wood" (WB:70); the Silent Pilgrims' "Original Song" and Sister Lyra's echo-jars (WB:74-78); Valen's two Rules (b1 ch.1:257-259) and his
  "Song of Sifting" (b3 ch.4:46); Klyther and Tynara's "tools they had forged from salvaged scrap and sea-salt" (b3 ch.1:70).
- **The smith:** Tynara Embercoil, Forgeborn, "can telekinetically manipulate metal" (CANON, B1WB:31; WB:36). In the game's time she serves the
  Spoke's works (b1 ch.2:7); in book 3 she says "I'm tired of iron anyway" (b3 ch.1:74).

### 4.2 The weapons (one per class; each four pieces)
Every weapon is the same recipe: **three pieces from the Auditor + one piece from the end-game world bosses.**
1. **Shards of the Gold Mask** x3 (shared by every class; a stack): the mask's gold, which holds the hum.
2. **The class's glass-steel blank** x1 (the blade, head, limbs or staff for that class; named per weapon below).
3. **Pale Crystal Heart** x1 (shared; the Auditor's core: "the crystal it is made of, still humming").
4. **The last piece: A Note of the Original Song** (PROVISIONAL; WB:74-75: "only the 'Original Song' can close the Fracture"): one per
   weapon, made from three **Verses**, one from each later world boss (section 4.4). It wakes the weapon.

| Class | Weapon | Kind | Its blank (dropped by the Auditor) | Book root | Label |
|---|---|---|---|---|---|
| Warrior | **Second Truth** | glass-steel longsword | Glass-Steel Sword Blank | Veyra's *Truth-Taker*, the glass-steel that hums and leaves gold afterimages (b1 ch.7:149, ch.10:67, ch.17:25); never *Truth-Taker* itself, which is Veyra's | CANON-EXPANDED |
| Paladin | **The Iron Anchor** | war hammer | Glass-Steel Hammer Head | Baston's hammer with its own pull (b2 ch.13:23); Baston as the Iron Anchor (WB:19) | CANON-EXPANDED |
| Rogue | **The Two Rules** ("Exist Elsewhere" and "Never Interfere") | a pair of glass-steel dirks | Glass-Steel Dirk Blanks | the Lowtowners' Rule One and Rule Two (b1 ch.1:257-259); Valen's dirks (CLASS_STATUS: a pair of Lowtown dirks) | CANON-EXPANDED |
| Scoundrel | **The Song of Sifting** | a singing stiletto | Glass-Steel Stiletto Blank | Valen's "Song of Sifting" sung instead of a bow (b3 ch.4:46) | CANON-EXPANDED |
| Ranger | **The Salt-Wall Bow** | glass longbow | Glass-Steel Bow Limbs | the Glass-Archers on the Salt-Wall and their salt-tipped shafts (b3 ch.4:46, ch.8:36) | CANON-EXPANDED |
| Mage | **The Sun-Staff** | double-ended staff | Void-Iron Staff Haft | the Praetorian's Sun-staff and its void-iron haft (b1 ch.26:69, 137) | CANON-EXPANDED |
| Druid | **Amber-Wood Crook** | staff | Amber-Wood Stave | the Root-Mother encased in amber-wood (WB:70) | CANON-EXPANDED |
| Archivist | **The First Verse** | staff with an echo-jar | Bone Tuning Staff | the Original Song and echo-jars (WB:74-78); the pilgrim's bone staff (b1 ch.13:73) | CANON-EXPANDED |
| Salt-Mender | **Mender's First Tool** | salt-stone maul | Salt-Stone Maul Head | Klyther and Tynara's tools of scrap and sea-salt (b3 ch.1:70-74) | CANON-EXPANDED |
| Shaman | **Grohl's Harpoon** | bone harpoon on amber rope (a spear) | Bone Harpoon Head | the Ash-Walkers' harpoons that resist the Unmaking (b1 ch.20:181, 191); Grohl, their chieftain (b1 ch.25:25) | CANON-EXPANDED |
| Cleric | **The Hill-Shrine Lantern** | mace with a hung lantern | Glass-Steel Mace Head | no clerics in the books (CLASS_CLERIC_SHAMAN.md); the Song of Mending (WB:20) | GAME-ONLY |

No machines in any of them: no workings, gears, springs or engines; glass-steel, gold, crystal, bone, amber-wood, salt-stone.

### 4.3 What the Auditor drops, and the bad-luck protection
Per kill, for each member who is not locked out (personal, section 2.5):
- **1 Shard of the Gold Mask, always.** (Three kills give the three.)
- **Your class's blank: 25%**, plus 15% for every kill without it (25, 40, 55, 70, 85, 100: never more than six kills).
- **Pale Crystal Heart: 20%**, plus 15% a kill without it (at most six kills).
- Smart loot: the blank is always your class's (the character's class), never another's.
- What a kill gives is shown in the loot window with the counters ("Heart: 35% next time").
- With the weekly lockout that is **3 to 6 weeks** for the Auditor's half, about 4 on average: a legendary chase, not a month of nothing,
  because every kill gives a Shard and the epic stage (4.5) comes the day the three pieces are in.
- Bad-luck counters are per character and saved (section 5). Sims roll the same way (their own counters, kept in the world slot).

### 4.4 The one piece that needs the end-game world bosses (PROVISIONAL)
**A Note of the Original Song** (one per weapon): Elder Orym, the blind High Archivist "who hears the world's song" (WB:77), or Brother Aris
(WB:76) tells you a weapon of the old glass-steel only wakes with a note of the Original Song, and that its verses were scattered when the Weave
broke. Three **Verses** make one Note; each is dropped by a later world boss (one guaranteed Verse a kill, personal):
- **Verse of the Forge** from the **Forge District's world boss** (19-22 zone; PROVISIONAL: something born of the foundries' slag-memory,
  never a machine).
- **Verse of the Deep** from the **Warrens' world boss** (22-25 zone; PROVISIONAL: from the Deep Veins' geode seams, b1 ch.9).
- **Verse of the Crown** from the **Spire's or the Crown's world boss** (25-30; PROVISIONAL: a Pale King's greater avatar, the Auditor's
  master).
None of these is designed; this doc only names the slot each fills. The Verses wait in the component bag until all three are in.

### 4.5 Where the combining happens, and what the weapon does
**The forge (question 9; recommended): Tynara Embercoil at a quiet forge under the Forge District's wall**, at the Rust Market's east edge
(CANON-EXPANDED: the Forgeborn who shapes metal, B1WB:31; the Rust Market against the Forge District's walls, b1 ch.3:7). In the game's time
she works for the Council by day (b1 ch.2:7); at night she remakes the old glass-steel for anyone who brings her the Pale crystal, because she
has seen what the Spoke eats (the turn she makes by book 3, b3 ch.1:74). She is only at her forge at night (21:00 to 06:00) and says little.
When the Forge District opens, her forge moves inside its gate.
*Options:* a GAME-ONLY glass-steel smith in the same place; the goblin Weavers of the Warrens, who "manipulate the world's physics through
resonance" (WB:87), at the drowned vaults' fence.

**Stage 1, the epic (question 8; recommended):** three Shards, the class's blank and the Heart, plus coin (a week's worth for a level-20
player) and a crafted part from the Lowtowns (the tier-6 slag-iron bar, Blacksmithing), give **the weapon, unawakened**: an **epic** named
weapon of **level 20** (quality 4: about 70 weapon damage on today's curve, `ItemDatabase` power level x 2.1), with its look but **dim** (no
glow) and its effect line greyed: "Sleeps. A note of the Original Song would wake it."
**Stage 2, the legendary:** the Note added at the same forge wakes it to **legendary, level 30** (quality 5, power level x 2.5: about 123
weapon damage at 30 on today's curve, like the Cinderheart's 55 at 12) with its **one unique effect** and the bright glow of a legendary
(Chris, 2026-10-05: the brightest glowing weapons epic or legendary only). It keeps its id; the save just marks it awake.
*Option:* nothing usable until all four pieces are in (a longer wait, a bigger moment).

**The effects** (one each; kept inside the balance bands: each worth about 3-5% of the build's output, measured by the harness at the
cap; GAME-ONLY numbers; they work for every path of the class):

| Weapon | Effect |
|---|---|
| Second Truth (Warrior) | **Afterimage:** every 5th blow leaves a gold afterimage that strikes again for 40% (with the tank's threat when tanking). (b1 ch.10:67) |
| The Iron Anchor (Paladin) | **The Anchor:** dropping under 30% health roots you in place and takes 25% off every blow for 6 s; once a minute. (WB:19) |
| The Two Rules (Rogue) | **Exist Elsewhere:** a blow of over 20% of your health moves you 5 m behind your foe (a blink, as the Weave-Eaters move, b1 ch.20:131); once every 30 s. |
| The Song of Sifting (Scoundrel) | **Sifting:** your holds last 20% longer, and the first blow on a held foe as it breaks does 30% more. |
| The Salt-Wall Bow (Ranger) | **Salt-Tipped:** every 6th shot is salt-tipped: 50% more damage and it strips one good effect from the target (double on things of the Void and the Wasting). (b3 ch.8:36) |
| The Sun-Staff (Mage) | **Sunfire:** a spell cast at full Heat leaves a 3 m patch of sunfire for 4 s (burns foes, never friends). (b1 ch.26:69) |
| Amber-Wood Crook (Druid) | **Amber:** every 20 s your next heal also wraps the target in amber (a small ward), or in a damage form your next blow roots for 1.5 s. (WB:70) |
| The First Verse (Archivist) | **Echo-Jar:** every 8th spell is kept in the jar, and your next spell is echoed at 50%. (WB:78) |
| Mender's First Tool (Salt-Mender) | **Salt Line:** a salt working also pours a short salt line that calcifies the first foe across it for 2 s; once every 20 s. (b1 ch.20:205) |
| Grohl's Harpoon (Shaman) | **Grounding:** every 20 s your next spell pins its target: no blink, no move for 3 s, and things of the Void are silenced too. (b1 ch.20:181, 191) |
| The Hill-Shrine Lantern (Cleric) | **Kept Light:** healing past full becomes a ward of up to 10% of the target's health (gone after 8 s). |

Their looks (with Phase 8.5's figures): glass-steel is a pale, almost white blade with a faint gold seam (the mask's gold), the hum shown as a
slow shimmer; awake, a gold glow on the seam. Each zone's art rule holds (every one its own design, no reused models).

### 4.6 The boss's other loot (signature list)
Two epics a kill by need and greed (2.5), level 20, drawn from a list of eight (GAME-ONLY names, to be written with the item data): a gold-masked
helm for each armour weight ("Auditor's Mask" in cloth, leather, mail: a faceless gold face-plate, never the Seekers' full mask), a crystal
mantle, a grey-crater cloak (if the back slot exists by then), a ring of the shattered crystal, a ledger-tome off-hand, and the **Pale
Tally**, a trinket-like neck piece. Plus a 1% legendary on the Auditor's own table for the day it exists (none now).

---

## 5. Save and data, and the build plan

### 5.1 What has to be saved
Back up Chris's save before the format change (CLAUDE.md rule). Today's save is per character (`EncounterSave`, format 11, one slot per
class, `EncounterProgress` with the lockouts; migrations through `SaveMigrator`) plus the world slot (`WorldSave`, format 2, payload
`CrulandaWorld`: the sims, their party and guilds, the exchange). No world timer is saved today. Item kinds are strings ("gear", "bag",
"junk", "material", "recipe"); bags are 24-slot `Inventory` lists of `ItemStack { item, count }`. Raid bosses: `ZoneDefinition.raid` and the
raid marks (`RaidMark` Skull, Moon, Cross) exist; `ActorClassification.Boss` exists in the enums but the encounter code uses `Elite` bools, so
the raid-boss rank is a new flag on `EncounterEnemy` (with `EliteMove.phases` for the three phases).
- **Per character (`EncounterProgress`, format 12):**
  - **The component bag:** a list of `{ id, count }` (Shards of the Gold Mask, the class blank, the Heart, Verses, Notes), outside the bags
    (no slot taken), shown in a "Legendary" tab of the bags or the Armoury. Items of a new kind, `"component"`, stackable, bound to the
    character, never sold or traded.
  - **Bad-luck counters:** `{ component id, kills without it }`.
  - **The weapon's stage:** the forged weapon is an ordinary item with its own id; awake or not is a flag on the item's saved state (or a
    progress key, `legend.awake:<item id>`, if that keeps the format change smaller).
  - **The lockout:** already saved (`EncounterProgress.lockouts`, `Lockouts.WorldBoss`).
- **World slot (`WorldSave`, format 3):** the boss's timer (play-seconds until the next landing, or the landing's state: warned, down,
  left), the yard's crater (until when), and the sims' component counters and forged weapons (when sims carry legendaries).
- **Data files (no save):** `EncounterContent/Raids/pale_auditor.json` (the boss's numbers, phases, lines), the components and the eleven
  weapons in an item file (`loot.legendary.json`) with looks, and the recipes for the forge.

### 5.2 The build plan (each step its own branch, tests, a build; Chris looks before the next where marked)
1. **The raid group** (M): two parties of five, `MaxPartySims` for a raid, raid-wide and party-wide effects, raid frames, the ready check,
   the raid window. Tests: EditMode raid rules (join, leave, roles, party of each member, raid-wide reach); PlayMode ten in the Lowtowns.
   *Chris looks: the frames.*
2. **The call to arms** (S): Zone chat call, guild first, the Lowtowns, friends; boldness and level gates; sims' own raids. Tests: EditMode on
   a fixed roster (who answers, by role; the 3 am empty zone).
3. **The boss and its timer** (M): the raid-boss rank (skull, "Raid (10)"), the level-25 tables, no group scaling, the landing in the yard
   (the yard measured first; sheds cleared if needed), the crater, the camps emptied, the play-time timer in the world slot, the warnings and
   the System lines, the departure. Tests: EditMode timer and save round-trip; PlayMode the landing. *Chris looks: the Auditor and the yard.*
4. **Phase 1** (M): Weight, Struck from the Record, the Hum with its immunity, and the sims' swap, walk-out (sims leaving ground marks,
   new and shared with the dungeons) and rotation. Tests: EditMode each
   mechanic; PlayMode a phase-1 fight of ten.
5. **Phases 2 and 3** (M): shards (salt and harpoons work on them), Museum Piece, the Silence, the beam, the hard enrage, the sims' parts.
   Tests as step 4.
6. **The balance** (M): the group run tool at 10 (a raid option), ten seeds, the targets of 3.6; read each wipe; tune the data file only.
   *Chris watches a run (group runs he watches: HUD, meter, scorecard on).*
7. **The loot** (M, the save change): the component kind, the component bag and its tab, the drops with bad-luck counters, the signature epics,
   format 12 with a migration test, Chris's save backed up first. Tests: EditMode drops over 10,000 kills (the counters cap at six), save
   round-trip, migration from 11.
8. **The forge and the weapons** (L): Tynara's night forge, the eleven weapons' data, looks and effects, the epic stage, the awake flag (no
   Notes yet: the Verses' bosses do not exist; a test fixture gives one). Tests: EditMode each effect's numbers within its band; the harness
   measures the effects at 20 (asleep, so none) and with a test Note at 30 later.
9. **Chris plays it** (a preview build with its own save folder and a .bat): call a raid, kill it, forge the epic. Nothing is called done
   until he has.

---

## Lore audit (every name used)
| Name | Label | Source |
|---|---|---|
| The Pale Kings; an avatar of one with a gold mask; auditors; crystal, gravity, craters; the unmaking beam | CANON | b1 ch.12:375-403; WB:11; b2 ch.13:13-25; b2 ch.15:91 |
| The Pale Auditor (the name and this avatar's fight) | CANON-EXPANDED / GAME-ONLY fight | from WB:11 and b1 ch.12 |
| The Lowtowns as a feast; the Spoke's reach into them | CANON | b1 ch.14:63; ch.24:162 |
| The Shard-Burners' Yard | GAME-ONLY | ZONE_LOWTOWNS.md |
| The horns' two-tone wail; Rule One and Rule Two | CANON | b1 ch.3:243; ch.1:257-259 |
| Salt and bone harpoons against the Unmaking | CANON | b1 ch.20:181-205 |
| Glass-steel, a lost art; Truth-Taker's hum and afterimages | CANON | b1 ch.7:149; ch.10:67; ch.17:25 |
| Tynara Embercoil, Forgeborn metal-shaper, as the night smith | CANON-EXPANDED | B1WB:31; b1 ch.2:7; b3 ch.1:70-74 |
| The Original Song; Elder Orym; Brother Aris; echo-jars | CANON | WB:74-78 |
| A Note of the Original Song and its three Verses | PROVISIONAL | this doc |
| The eleven weapons | CANON-EXPANDED (the Cleric's GAME-ONLY) | section 4.2 |
| The later world bosses (Forge District, Warrens, Spire/Crown) | PROVISIONAL | ROADMAP "The 16-30 road" |

Kept out on purpose: Truth-Taker itself (Veyra's own), Kael's "kinetic-axe" and the Wardens' "arc-throwers" (b2 ch.9:77, ch.13:9: machines),
the Spoke's works and levers (b1 ch.2:7: machinery), any other game's boss or weapon names.
