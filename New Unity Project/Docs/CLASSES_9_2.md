# Phase 9.2: new classes to level 20 (design, 2026-10-19)

## Chris's answers (2026-10-19, play note 298, verbatim)
- First class: **"Pact-Blade"** (not the recommended Seer). Its build spec is section 4; built 2026-10-19 (CLASS_STATUS.md).
- Seer paths: **"Maker/Keeper/Breaker (Recommended)"**.
- Names: **"Seer + Sight (Recommended)"**.
- Races: **"Seer human, Ash-Walker orc (Recommended)"**.

Chris, 2026-10-19 ~20:08 (verbatim): "just want everything up to level 20 for now with classes" (PLAYTEST_NOTES 297; ROADMAP "Next, after
the Unraveller's tuning"). Phase 9.2 (ROADMAP): new class kits with talents, AI rotations, looks and sim support, modelled on the books'
people. The Tinker stays set aside (machinery, 2026-10-11). **No machines** over all of it.

Read from `D:\code\crulanda` on 2026-10-19, books first (`Book1_The_First_Spoke_Final.md` is cited as B1F:line, with its chapter), then
`world_bible.md`, then the character sheets in `chars/` (lore notes, weaker than the books). Labels as CRULANDA_CANON: CANON, CANON-EXPANDED,
GAME-ONLY, PROVISIONAL. Every class name and ability name below is PROVISIONAL until Chris says yes.

## Recommended

1. **The Seer first.** The books' own word for one of their heroes (Sylvara, "the Seer", B1 ch.19-25), and the book gives the class its
   three paths: the Trinity, "The Maker builds the house. The Keeper tends the hearth. The Breaker tears it down" (Korath, B1F:5370). Maker =
   support (hard light laid *before* the blow), Keeper = healer (threads that share a hard blow across the party), Breaker = damage (pull a
   foe's threads loose, then snap them). Nothing in the game plays like it: every ward and heal today answers a blow after it lands; the Seer
   sees it coming. It also answers the raid's open notes (294: the leap marks one member; 287/289: telegraphs too short): Foretell wards
   whoever a marked foe is about to hit.
2. **The Ash-Walker second.** Grohl's orc harpooners (B1 ch.20 and ch.25): bone harpoons on amber ropes, "Pull!", a mace of volcanic
   glass that breaks what is held. A heavy-armour tank / damage / control class, the first one led by the orcs (Races.cs has orcs; only
   the Shaman needs them today), and the "someone that can do an extra job" of note 281 (harpoon and haul the brood).

The full build spec for the Seer is section 3. The other four candidates are sketched in section 1 so Chris can pick another.

## The gaps today (11 classes: SimAdventurers.ClassIds)

Warrior, Paladin, Druid, Ranger, Mage, Rogue, Archivist, Salt-Mender, Cleric, Shaman, Scoundrel.
- **Tanks** (5): Warrior, Paladin Oathguard, Druid Barkhide, Salt-Mender Mason, Shaman Stonebound. **Healers** (7 paths): Paladin, Druid,
  Archivist Cantor, Salt-Mender Mender, Cleric Lantern and Choir, Shaman Rainspeaker. **Pure damage classes** (4): Ranger, Mage, Rogue,
  Scoundrel, all in leather or cloth.
- **Missing:** a healer or support that acts *before* the blow (every ward is laid on the most hurt, after); a healer that spreads one big
  blow across the party; an orc-led class besides the Shaman; a pure damage class in heavy armour; a caster whose magic is wild (the
  world bible's pillar 8: "Magic must be strangely unpredictable"); any goblin class.

## Chris's bands (what every new build is held to; BALANCE_REPORT.md, PLAYTEST_NOTES 279)

Single-target damage on the harness's fixed dummies. The level-15 band is the measured one; at another level it is scaled by
MobHealth(L)/MobHealth(15) (SimCompanion.BandScale: 690/540 = x1.28 at 20).

| Role | At 15 | At 20 | Rule |
|---|---|---|---|
| Pure damage | 62-72 (middle 69) | 79-92 (middle 88) | the pure classes |
| Hybrid damage path | about 52 | about 66 | 75% of the pure band (note 279, "hybrids 75% of target"; **the code still has `HybridDpsShare = .90`**: the Seer and Ash-Walker are built to 75%) |
| Tank | about 50 | about 63 | ~72%, never over the damage-dealers (sims: `TankDpsShare` .75, test 0.70-0.80) |
| Control / support | 45-52 | 58-66 | ~70% (cap 75%) |
| Healer | at most 35 damage; 40-50 HPS on the wounded dummy | at most 45; 51-64 HPS | plus a slower group heal (Chris, 2026-10-13: "yes give a slower group heal") |

Talents at 20: 21 points (level + 1). Tier t opens at 5 x t points in that branch, so tier 4 opens at 20 points (level 19). Every branch
below has 22-24 ranks, so a level-20 character cannot take all of one branch: one choice is always left.

---

## 1. The candidates

### 1a. The Seer (RECOMMENDED FIRST): support / healer / damage

| | |
|---|---|
| **From the books** | Sylvara "the Seer" (B1F:5250), "Sylvara of the Silver Dawn" (B1F:5526). Her Seer's sight "sought the threads of the Weave, the intricate tapestry of magic that underpinned all reality" (B1F:5316). She weaves "a fragile net of detection-Wards using her own aura" over the camp (B1F:5358), "a delicate, golden web stretching out over the sleeping camp" (B1F:5390); "If I drop the Wards, the cold will kill the children before morning" (B1F:5386). Against the Weave-Eaters she raises "a barricade of hard light" that "didn't kill them. It merely paused them" and they eat it as fast as she casts (B1F:5432-5436). Korath: "You are carrying three hundred people on your back, Seer" (B1F:5366) and "Your Spark is burning too bright... You are a beacon in the dark" (B1F:5384). The Trinity, Maker / Keeper / Breaker (B1F:5370-5378; world bible pillar 12). She reads the memory of magic in a map (B1F:7043). "A blessing of the Silver Dawn" (B1F:7199). Book 2: "the Seer", vanished (book2/character_status.md:15); "THE SOUL-THREAD HAS SLIPPED THE LOOM" (book2/chapter_3.md:39). Book 3: her voice "a thin, gold thread in the wind" (book3/chapter_5.md:78). World bible: "Sylvara Moonpetal (Seer): Can see visions via tapestry weaving." Character sheet: the Loom Needles, an Aether-Glass Orb, Portent, Vision-Exhaustion. |
| **Label** | The people (Seers, the Silver Dawn), the Trinity, hard light and detection-wards: CANON. The class's kit (threads as a resource, the Knot, sharing a blow): CANON-EXPANDED. Numbers: GAME-ONLY. A player Seer is an ordinary Seer of the Silver Dawn, not Sylvara (PROJECT_MASTER: "Preserve legendary characters as legendary"): no centuries-long loom, no Void-Knot visions. |
| **Paths (the Trinity)** | **Maker** support (hard light laid ahead of the blow, a wall of light, a raid-wide "measured ahead"); **Keeper** healer (threads tied to three allies that share a hard blow, a long mend, the hearth-light group heal); **Breaker** damage (Unpick lays frays, Unravel pulls them over time, Snap the Weft spends them). |
| **Gap filled** | Prevention before the blow (nothing does it); a healer that spreads a spike (nothing does it); a support path for raids built on the telegraphs the raid already shows. |
| **Resource and feel** | **Sight** (a pool of 100; mana-like). Under 20 Sight you are worn by the visions: casts take 20% longer (the sheet's Vision-Exhaustion). The **Trinity Knot**: cast a Maker, a Keeper and a Breaker working within 10 s and your next working is free and 30% stronger ("Even the Weave requires three points to remain stable", B1F:5366). Every path wants a little of the other two. Your wards make threat as heals do: the beacon in the dark. |
| **Signature abilities (to 20)** | Unpick, Keeper's Thread, Hard Light, Foretell, Stay the Thread, Cut the Thread; by path: Wall of Light, Measured Ahead, The Thread Held (Maker); Tether, Long Thread, Hearth-Light, Three Hundred on My Back (Keeper); Unravel, Snap the Weft, The Breaker Walks, The Old House Falls (Breaker). Full numbers in section 3. |
| **How sims play it** | Ranged (20 m). A Keeper sim heals when no other healer is along (the Salt-Mender's rule), tying threads on the tank and the two most hurt. A Maker sim wards whoever a foe's cast or ground warning is aimed at, and holds the moon (Stay the Thread). A Breaker sim deals the hybrid band. All interrupt (Cut the Thread). |
| **Race / trained** | Human (the Silver Dawn are human in the books). Trainer: the Crown's library (CAPITAL_HUB.md lists class trainers in the castle): a Seer of the Silver Dawn among the books, GAME-ONLY placement. |

### 1b. The Ash-Walker (RECOMMENDED SECOND): tank / damage / control

| | |
|---|---|
| **From the books** | B1 ch.20: bone harpoons out of the dark, "thick, brutal" (B1F:5470-5474); five warriors "easily eight feet tall... heavy, overlapping plates of dark, chitinous armor that looked like it had been grown rather than forged... helms made of bleached skulls" (B1F:5480); "Each held the end of an amber rope... and hauled" (B1F:5482); "The harpoons... acted as anchors, preventing them from displacing" (B1F:5484); the leader's "Pull!", "like an avalanche given voice" (B1F:5476); "a heavy mace, covered in spikes of volcanic glass" that shatters the salted, crystal Weave-Eaters (B1F:5500); "We learned to eat the ash. We learned to hunt the things that hunt the dark" (B1F:5520). B1 ch.25: "chitin-armored Orcs and wildlings" in "tight, disciplined defensive circles" (B1F:6979-6985); Chieftain Grohl and his spiked mace (B1F:6987). The orcs: Gorm, "the Orc brute" (B1F:1714; world bible). |
| **Label** | The people and their harpoons, ropes, chitin and maces: CANON. The class: CANON-EXPANDED. The world bible also calls the Cult of Ash's people "Ashwalkers" (Vorn; book2/chapter_13.md:55): the game already split the allied Ash-Walkers from the cult (DUNGEON_DESIGN.md "The Cult That Wears Our Name"). |
| **Paths** | **Rope-Holder** tank (hold the circle, haul foes off the soft ones, chitin that grows over hits); **Glass-Mace** damage (two-handed mace; breaks held foes hardest); **Harpooner** control (pins a foe in place, hauls packs together). |
| **Gap filled** | An orc-led class; a heavy-armour hybrid built around one new thing, the rope: pin, haul, break. It pairs with every hold in the game (the Salt-Mender's Calcify, Stay the Thread, the Jailer's Shackle): Glass-Break hits a held foe twice as hard. |
| **Resource and feel** | **Ash** (0-100): built by blows given and taken, and by standing on blighted ground ("we learned to eat the ash"); spent on hauls and breaks. Slow and heavy: a harpoon out, a haul, then the mace. |
| **Signature abilities (to 20)** | Bone Harpoon (20 m; the foe pinned, cannot move or blink, 6 s), Pull! (haul a harpooned foe to you), Glass-Spike Swing, Glass-Break (x2 on a held or crystal foe), Skull-Helm Roar (taunt), Defensive Circle (the party within 6 m takes 8% less while you stand), Chitin Grows (an absorb that thickens as you are hit), Eat the Ash (spend Ash to mend), Hunt the Dark (harder on the Wasting's things), Haul Together (the capstone: every harpooned foe within 20 m dragged to you). |
| **How sims play it** | As the Warrior tank sim, plus a harpoon pull every 15 s on a foe hitting a soft party member; the damage variant breaks whatever is held. |
| **Race / trained** | Orc first (and human wildlings, as B1F:6979 has them). Trainer: the Ash-Walker enclave on the Ashland Rim (already in the game, WORLD_ZONES.md). Needs the orc figure (RACES.md: not built yet). |

### 1c. The Pact-Blade: damage / damage / control (pure)

| | |
|---|---|
| **From the books** | Veyra: "the incredibly hot spark of Vital-Aether that every Iron-Born fiercely carried in their blood" (B1F:1310); "an absolute, terrifying whirlwind of... blinding white light", the glass-steel sword leaving "burning afterimages of bright gold", "minimum wasted movement... maximum lethality" (B1F:2765); Truth-Taker humming in the dark (B1F:4471). World bible: Veyra "Dual-sword master"; Talira Frostveil "(Knight): Sword master"; the Iron Citadel, "the primary military training ground". Kaelith Dawnstrike's grace (book2/chapter_29.md:21). Sheets: Veyra's Iron Vow (no fear), the Blood-Vow; Zygnar's Redclaw Codex. |
| **Label** | CANON roots; the class CANON-EXPANDED. It folds in ROADMAP 9.2's **Knight** (Talira, Kaelith), which was held over its overlap with the Paladin and Warrior: this one has no healing, no tanking, no oaths. "Stormlight" stays unused. |
| **Paths** | **Twin-Blade** (single target: two swords, measured cuts), **White Whirl** (area: the whirl of light), **Sentinel** (control: cuts that stop a foe moving, an answering cut on a missed blow). |
| **Gap filled** | The only pure damage class in heavy armour; the Iron Pact's own fighters. |
| **Resource and feel** | **Vital-Aether** (0-100): built by cuts, spent on aether-cuts; above 80 it burns your own health a little each second. Fast, precise, pushing the burn. |
| **Abilities** | Measured Cut, Second Blade, Afterimage (the gold lingers: a delayed second hit), White Whirl, Minimum Movement (a short dash), Sentinel's Stop (a fleeing foe rooted), Answering Cut, Iron Vow (no fear or charm 8 s), Blood-Vow (capstone: spend health for 10 s of harder cuts). |
| **Sims / race / trained** | Melee damage sim (the pure band). Human, the North's Iron-Born. The Iron Citadel when it exists; until then the Crown's barracks. |

### 1d. The Wildspark: damage / control / support (pure)

| | |
|---|---|
| **From the books** | Valen's street magic: "A concussive wave of violet kinetic force" (B1F:412), his gravity shard (B1F:490, 3889); world bible: Shards, "spent magical batteries used by the poor for minor tricks (like Valen's push spell)". Kai's wild Spark: "a faint, erratic hum" (B1F:5308), "his small hands glowing with the erratic, uncontrolled magic" (B1F:5438), "A wild Spark. The Concord would harvest him" (B1F:5528); "the raw chaotic Spark that defies the Concord's order... a necessary counter-weight" (B1F:7197). Pillar 8: magic "strangely unpredictable". |
| **Label** | CANON roots; the class CANON-EXPANDED. |
| **Paths** | **Surge** (ranged damage: Spark climbs with each cast; past a line a cast may surge bigger or backlash), **Push** (control: shoves, pulls, a gravity well; bosses only slowed), **Counter-weight** (support: Spark lent to an ally's next blows). |
| **Gap filled** | The Lowtowns' own magic, in the zone the cap of 20 is built round (Lowtowns 15-20); a caster whose feel is risk, not the Mage's schooled Heat. |
| **Abilities** | Spark-Bolt, Violet Shove, Gravity Shard, Erratic Hum (a damage-over-time that jumps), Surge, Counter-weight, Spent Shard (a cheap burst from a used shard), Harvest-Proof (a shield against drains), Wild Bloom (capstone). |
| **Sims / race / trained** | Ranged damage sim. Human Lowtowners and orcs. The Rust Market. Risk: surges must use the session's seeded random so tests hold. Name kept off "Shard" (the Rogue's Shardwork). |

### 1e. The Charter (goblin): control / support / damage

| | |
|---|---|
| **From the books** | World bible: the Weavers of the Warrens, "record-keepers and light-catalogers of the High City", "Vibration-Locking and Static-Charting... manipulate the world's physics through resonance rather than raw magic". Razzle, "a former clerk for the Weaver's Guild, a man whose clever fingers had once cataloged the city's light" (B1F:18); the silent hand-cant of the Warrens (B1F:2360); Vibration-Locking, metal treated "exactly like complex music" (B1F:914). Snivels's sheet: the Chart-Maker, the Golden Compass, Cutting Words. |
| **Label** | CANON people; Static-Charting is named but never shown in the books, so the kit is CANON-EXPANDED and thin. |
| **Paths** | **Static-Charter** (control: lines charted on the ground that slow and hold), **Lamp-Clerk** (support: foes catalogued, the party's blows find their weak point), **Resonant** (damage: a foe's note found and struck). |
| **Gap filled** | The first goblin class. |
| **Why last** | Needs a playable goblin race (none yet); overlaps the Rogue's Locksmith (vibration-locking) and the Archivist (resonance). |

---

## 2. Questions for Chris (recommended answers first)

1. **Which class first?** (Recommended) the Seer, then the Ash-Walker. / The Ash-Walker first. / The Pact-Blade. / The Wildspark. / The Charter.
2. **The Seer's three paths?** (Recommended) the Trinity: Maker support, Keeper healer, Breaker damage. / Swap the healer for control.
3. **Names?** (Recommended) "Seer", with "Sight" for its pool. / "Loom-Seer". / "Seer of the Silver Dawn".
   For the second: (Recommended) "Ash-Walker" (the allied people's own name; the cult is kept apart) / "Ridge-Hunter".
4. **Races?** (Recommended) the Seer human only; the Ash-Walker orc first, human wildlings later. / Both open to humans and orcs.

---

## 3. The Seer: build spec (ready to build when Chris picks it)

### Shape
- **id** `class.seer`; resource `ResourceKind.Mana`, named **Sight**, a pool of 100, regained as the Cleric's Grace is.
- **Hybrid** (it has a healer's path): goes in `EncounterSession.HybridClasses`; its Breaker targets 75% of the pure band.
- **Base stats** (`EncounterSession.Innate`): (15 + L, 8 + L/2, 12 + L/2, 21 + 2L, 21 + 2L); health as the Cleric (160 + 19 a level).
- **Gear**: cloth (`SimGear` "cloth"); a two-handed staff (starter `item.seers_staff`, a plain pale staff); main stats Intellect and Spirit;
  draught at buff-ups: the mana draught. Profession leanings: tailoring (as the Mage).
- **Standing party buff** (BossBuffs): **Blessing of the Silver Dawn**, +3% Intellect for half an hour ("a blessing of the Silver Dawn", B1F:7199).
- **Passive (every Seer)**: detection-wards: stealthed and hidden foes within 20 m show to the party (B1F:5358).
- **Every class has one hold and one interrupt** (CC_DESIGN.md): Stay the Thread and Cut the Thread.

### Rules shared by all three paths
- **Aspects.** Maker: Hard Light, Foretell, Stay the Thread, Wall of Light, Measured Ahead. Keeper: Keeper's Thread, Tether, Long Thread,
  Hearth-Light. Breaker: Unpick, Cut the Thread, Unravel, Snap the Weft.
- **The Trinity Knot.** A working of each aspect cast within 10 s (any order) ties the Knot: the next working costs nothing and is 30%
  stronger. At most once each 10 s. A small three-pointed knot over the bar shows it (the rune Sylvara draws, B1F:5382).
- **Frays.** Unpick lays 1 Fray on the target (at most 5, each lasting 12 s). Unravel and Snap the Weft read them.
- **Vision-Exhaustion.** Under 20 Sight every cast takes 20% longer.
- **The beacon.** Hard Light, Wall of Light and Hearth-Light make threat as healing does (`HealThreat`).

### Abilities by level (numbers at level L; at 15 and 20 in brackets)
The kits accept base unlocks at levels 1-10 only (`u.level > 10` throws in every kit); the path abilities unlock by talent (as the
Scoundrel's: `unlock.talentId`), and a talent tier opens at 5 points a tier, so they arrive at 1, 4, 9, 14 and 19.

| Slot | Ability | Aspect | Unlock | Cast / cost / cooldown / reach | Does |
|---|---|---|---|---|---|
| 1 | **Unpick** | Breaker | level 1 | 1.5 s / 8 / none / 30 m | 9 + 2L damage [39, 49]; lays a Fray |
| 2 | **Keeper's Thread** | Keeper | level 1 | 1.5 s / 12 / none / 30 m | mends 14 + 2L [44, 54] on the target, or the most hurt |
| 3 | **Hard Light** | Maker | level 2 | instant / 14 / 8 s / 30 m | absorbs 16 + 3L [61, 76] for 10 s; one on an ally at a time |
| 4 | **Foretell** | Maker | level 4 | instant / 10 / 15 s / 30 m | marks a foe 15 s: when it begins a cast or a ground warning, whoever it aims at (up to 5 in a ring) gets a free Hard Light at half strength [30, 38]; once a mark |
| 5 | **Stay the Thread** | Maker | level 6 | 1.5 s / 20 / 20 s / 30 m | holds a foe in hard light: 8 s (an elite 4 s); a boss only slowed 30% for 4 s; damage breaks it after the first 2 s ("It merely paused them", B1F:5434) |
| 6 | **Cut the Thread** | Breaker | level 8 | instant / 8 / 12 s / 30 m | interrupts a cast; that school locked 4 s |
| 7 | **Wall of Light** | Maker | talent, tier 0 | instant / 30 / 30 s / 30 m | a ring of hard light 6 m across for 8 s: the party inside takes 10% less from foes outside it; a non-boss foe crossing in is held 2 s (once each); each foe that touches it eats 1 s of it (B1F:5436) |
| 8 | **Measured Ahead** | Maker | talent, tier 2 (level 9) | instant / 25 / 120 s / party | for 10 s, the next blow each party member takes is 30% lighter |
| 9 | **Tether** | Keeper | talent, tier 0 | instant / 12 / none / 30 m | ties a thread to an ally for 12 s (three at once): mends 5 + L every 2 s [20, 25]; a blow over 10% of a tied ally's health is split evenly over every tied ally and you |
| 10 | **Long Thread** | Keeper | talent, tier 1 (level 4) | 2.5 s / 20 / none / 30 m | mends 26 + 4L [86, 106] on the most hurt |
| 11 | **Hearth-Light** | Keeper | talent, tier 2 (level 9) | 2 s / 24 / 12 s / 15 m | the golden web: every party member within 15 m mends 9 + 2L [39, 49] at once and 2% of their health a second for 6 s (the slower group heal) |
| 12 | **Unravel** | Breaker | talent, tier 0 | instant / 10 / none / 30 m | 4 + L every 2 s for 12 s [19, 24 a tick], 10% more a tick for each Fray on the foe |
| 13 | **Snap the Weft** | Breaker | talent, tier 1 (level 4) | 1 s / 14 / 6 s / 30 m | spends every Fray: (14 + 3L) x (1 + 0.4 a Fray) [177, 222 at five]; needs one Fray |
| 14 | **The Breaker Walks** | Breaker | talent, tier 2 (level 9) | instant / 0 / 90 s / self | 10 s: Unpick is instant and lays 2 Frays |
| 15 | (path capstones at tier 4, level 19: The Thread Held, Three Hundred on My Back, The Old House Falls: below) | | | | |

Three Hundred on My Back is an active (slot 15, Keeper only); the other two capstones are passives. A level-20 Seer of one path uses the
six base workings and its own path's three or four: nine or ten on the bar.

### What a one-path Seer has, level by level
| Level | Points | Maker | Keeper | Breaker |
|---|---|---|---|---|
| 1 | 2 | Unpick, Keeper's Thread, **Wall of Light** | Unpick, Keeper's Thread, **Tether** | Unpick, Keeper's Thread, **Unravel** |
| 2 | 3 | + Hard Light (the Knot is open to every path) | + Hard Light | + Hard Light |
| 4 | 5 | + Foretell; tier 1: Lattice | + Foretell; **Long Thread** | + Foretell; **Snap the Weft** |
| 6 | 7 | + Stay the Thread | + Stay the Thread | + Stay the Thread |
| 8 | 9 | + Cut the Thread | + Cut the Thread | + Cut the Thread |
| 9 | 10 | **Measured Ahead** | **Hearth-Light** | **The Breaker Walks** |
| 14 | 15 | signature: Three Points Stable | signature: Keeper of the Hearth | signature: Tear It Down |
| 19 | 20 | capstone: The Thread Held | capstone: Three Hundred on My Back | capstone: The Old House Falls |
| 20 | 21 | one rank to place (the branch has 22-24) | | |

### Talents (`Talents/seer.json`; prefixes mk- kp- br-; three branches, tiers 0-4)

**Maker (support)**: "Read the blow, lay the light before it lands." Tradeoff: little damage; a ward that is never struck is Sight wasted. 24 ranks.
| Tier | Talent | Ranks | Kind | Does |
|---|---|---|---|---|
| 0 | Wall of Light | 1 | active | learn Wall of Light |
| 0 | Dense Light | 5 | passive | Hard Light absorbs 5% more a rank |
| 0 | Quick Light | 3 | passive | Hard Light's cooldown 1 s shorter a rank |
| 1 | Lattice | 3 | modifier | Hard Light also lays 10% a rank of its strength on the two allies nearest its target |
| 1 | Long Sight | 3 | passive | Foretell lasts 5 s longer and its ward is 15% stronger a rank |
| 2 | Measured Ahead | 1 | active | learn Measured Ahead |
| 2 | Light Held | 2 | modifier | a Hard Light that runs out unbroken gives back 10 Sight a rank |
| 3 | Three Points Stable | 1 | signature | with the Knot tied, your next Hard Light lands on the whole party at 40% |
| 3 | Steady Beacon | 3 | passive | your workings make 10% less threat a rank |
| 3 | Warm Light | 1 | modifier | a Hard Light broken by a blow mends 15% of its strength |
| 4 | The Thread Held | 1 | capstone | once a fight, a party member who would die is held in hard light at 1 health, unharmed, for 4 s |

**Keeper (healer)**: "Tie the threads, share the weight, keep the hearth lit." Tradeoff: a thread shares the hurt with you; a split party breaks the web. 22 ranks.
| Tier | Talent | Ranks | Kind | Does |
|---|---|---|---|---|
| 0 | Tether | 1 | active | learn Tether |
| 0 | Gentle Hands | 5 | passive | Keeper's Thread and Tether mend 4% more a rank |
| 0 | Steady Hands | 3 | passive | Keeper's Thread casts 0.1 s faster a rank |
| 1 | Long Thread | 1 | active | learn Long Thread |
| 1 | Strong Weft | 3 | passive | Tethers last 2 s longer a rank |
| 2 | Hearth-Light | 1 | active | learn Hearth-Light |
| 2 | Shared Weight | 2 | modifier | a blow split over the threads is 10% lighter a rank |
| 3 | Keeper of the Hearth | 1 | signature | with the Knot tied, your next Hearth-Light is free and ties a Tether to each it mends (up to five) |
| 3 | Warmth Kept | 3 | passive | Tethers mend 5% more a rank on allies under half health |
| 3 | Quiet Mind | 1 | modifier | Vision-Exhaustion never slows Keeper's Thread |
| 4 | Three Hundred on My Back | 1 | capstone (active) | 8 s: you take a fifth of every blow on the party in their place and every Tether mends double; 180 s cooldown (B1F:5366) |

**Breaker (damage)**: "Unpick, unravel, snap." Tradeoff: one foe at a time; Frays fall away if you switch. 22 ranks.
| Tier | Talent | Ranks | Kind | Does |
|---|---|---|---|---|
| 0 | Unravel | 1 | active | learn Unravel |
| 0 | Keen Sight | 5 | passive | Unpick and Snap the Weft deal 4% more a rank |
| 0 | Loose Ends | 3 | passive | Frays last 3 s longer a rank |
| 1 | Snap the Weft | 1 | active | learn Snap the Weft |
| 1 | Frayed Edges | 3 | passive | a foe with three or more Frays takes 3% more from you a rank |
| 2 | The Breaker Walks | 1 | active | learn The Breaker Walks |
| 2 | Hungry Threads | 2 | modifier | Snap the Weft at five Frays gives back 10 Sight a rank |
| 3 | Tear It Down | 1 | signature | with the Knot tied, your next Snap the Weft leaves its Frays in place |
| 3 | Ripped Weft | 3 | passive | Unravel ticks 4% harder a rank |
| 3 | Spreading Fray | 1 | modifier | Snap at five Frays lays two Frays on one other foe within 8 m (keeps it a single-target build: under x1.25 against three) |
| 4 | The Old House Falls | 1 | capstone | Snap the Weft on a foe under 30% hits 50% harder ("The Breaker tears it down so a new house can be built", B1F:5370) |

### The numbers against the bands (first estimate; the harness sets the scale)
- **Breaker** at 15 before its scale: Unravel kept up (about 14 a second with Frays), Snap at five Frays every 7.5 s (about 24), Unpick
  between (about 21), talents about +20%: some 70. Target 52 (hybrid 75%): `BuildDamageScale["class.seer/breaker"]` about .75. At three
  foes, Spreading Fray adds little: under x1.25.
- **Maker** at 15: Unpick only for damage (about 26 a second raw): target 45-52, scale about 1.8 (the Cleric's Warden sits at 2.32).
  Its worth is the absorbs: Hard Light about 7.6 a second on one ally, more with Lattice, Foretell and the Wall; the harness reports
  absorbed as healing in its own column.
- **Keeper** at 15: Keeper's Thread about 29 HPS, Long Thread 34, Tether 10 each: 40-50 HPS on the wounded dummy with Sight lasting the
  180 s (the OOM column). Damage under the healer cap (35). In the party test, Hearth-Light is the group heal.
- At 20 the same shares (x1.28): Breaker about 66, Maker 58-66, Keeper 51-64 HPS.

### Look
White robes (B1F:7187) with a silver thread worked through them; a pale wooden staff carved with old runes that bursts into white light
when she casts (B1F:5332, 5400; the book's word for the wood is another author's, so it is not used); loom needles at the belt and a
small glass orb (the character sheet). The three-pointed rune in silver on the hood. Hard light is white-gold and sharp-edged; the Keeper's
threads are thin gold lines between the tied allies; Frays are grey threads hanging off the foe. HUD colour a silver-lilac
(.78, .80, .95). First build borrows `ActorLook.Mage` (as the Cleric did); its own `ActorLook.Seer` in the figure round, with a wardrobe
capture (front, back, side) for Chris.

### Sim AI (SimCompanion; variant by the sim's seed, one of each path)
- **Every Seer sim**: ranged (Reach 20), an Interrupter (Cut the Thread every 12 s). Chat: "I see it coming. Behind the light." / "threads
  on you, stay close" / "Knot's tied".
- **Maker**: when a foe in the fight begins a cast or a ground warning, ward whoever it aims at (15% of their health, once each 6 s); else
  Hard Light on the tank every 8 s (12% of its health). Wall of Light when three or more foes are on the party (every 30 s). A Controller:
  holds the moon with Stay the Thread (CC_DESIGN section 3). Damage: the support share of the band.
- **Keeper**: heals when no other healer is along (else plays Maker): Tether on the tank and the two most hurt every 12 s, Keeper's
  Thread on the most hurt under 70% every 2 s, Hearth-Light when two or more are under 75% (every 12 s). Damage: the healer's half blow.
- **Breaker**: the hybrid share of the band (75% of the middle at its level); "Snapped" over the foe every 8 s.
- **Roster**: six Seer sims (two a path) in the later mix (`SimAdventurers`), homes in the 1-20 bands.

### Hooks to add (the Cleric's and Scoundrel's footprint)
`ClassKit.Create` (SeerKit.cs, on ClassKit); Encounter.asset (class.seer, its 15 abilities, unlocks with talentIds); Talents/seer.json;
`EncounterSession.Innate`, the look and colour switches (EncounterSession.cs ~124/139), `EncounterHud.ClassColour`;
`EncounterSession.RoleScale` (BuildDamageScale / BuildHealScale rows, HybridClasses); `SimAdventurers.ClassIds` + ClassName + the roster
mix; SimCompanion (Interrupter, Controller, a SeerRotation beside SaltRotation/PathRotation), SimFigure, SimGear (cloth, staff),
SimEconomy; BossBuffs (ClassIds, Standing, Pack, main stat); `Races.HumanClasses`; the starter staff (EncounterSession.Loot); icons in
make_icons.py; the balance harness's class list (11 to 12). The enemy-cast and ground-warning hooks Foretell needs: the cast start already
exists for interrupts; the ground warnings are the raid's telegraph rings (note 275).

### Tests to write
- **EditMode `SeerRulesTests`**: the tree parses at 15 and 20 with `SeerKit.ImplementedIds`; each branch 22+ ranks and tier 4 reachable at
  20 points; path abilities locked without their talent ("Talent"), base ones by level; the Knot ties only with all three aspects inside
  10 s and is spent once; Snap's number at 0-5 Frays and the cap of 5; a Tether split (300 on one of three tied plus you: 75 each; a blow
  under 10% not split); Vision-Exhaustion (x1.2 under 20 Sight); the Wall eaten 1 s a foe and each foe held once; `class.seer` selectable,
  human only; in HybridClasses (TalentDataTests).
- **PlayMode `SeerLoopTests`**: every bar action casts at 20 with its talent; Hard Light takes a blow; Foretell wards the target when the
  marked foe begins a cast; Stay the Thread holds a mob, breaks on damage after 2 s, only slows a boss; Tether shares a big blow; Hearth-
  Light mends the party within 15 m; The Thread Held saves one death a fight.
- **Balance**: harness rows for maker (support), keeper (healer, plus the party heal test), breaker (hybrid damage) at 15, then 18 and 20;
  `SimDummyTests` (a Breaker sim within 3% of its aim; a Keeper sim heals in the party test); `BossBuffTests` (the Silver Dawn's buff);
  `IconCoverageTests`.
- Targeted runs only (test budget), headless; no save-format change (a new class id), but Chris's save backed up before the first build.

---

## 4. The Pact-Blade: build spec (Chris's pick, 2026-10-19; built the same day, see CLASS_STATUS.md)

**Label:** CANON roots (Veyra's Iron Pact sword-work and Vital-Aether, B1F:1310, 2765, 4471; the world bible's "Dual-sword master",
Talira Frostveil "Sword master", the Iron Citadel "the primary military training ground"; Kaelith's grace, book2/chapter_29.md:21), the
class CANON-EXPANDED, every number GAME-ONLY, names PROVISIONAL. It takes in ROADMAP 9.2's Knight. No healing, no tanking, no oaths
(those are the Paladin's and the Warrior's); "Stormlight" is not used; Veyra's sword Truth-Taker is hers, not a class ability.

### Shape
- **id** `class.pactblade`, "Pact-Blade". A **pure** damage class (not in `HybridClasses`): its damage paths aim at the pure band
  (62-72 at 15, 79-92 at 20); its control path at the control band (45-52 at 15).
- **Resource: Vital-Aether** (`ResourceKind.Focus`, named "Vital-Aether", 100). "The incredibly hot spark of Vital-Aether that every
  Iron-Born fiercely carried in their blood" (B1F:1310). Built only by its cuts (no Aether from a plain swing, as Chris ruled for the
  Scoundrel's Nerve), spent on the aether-cuts; it starts a fight at nothing and drains away six seconds after it.
  **Burning Blood**: above 80 Aether your cuts land 10% harder and you lose 0.5% of your health a second (never below a tenth).
- **Two swords**: the Pact Sword in the right hand, the Second Sword in the left (a new held family, `sword.off`, drawn as the arming
  sword and mounted as the Rogue's second knife is). Heavy armour (`SimGear` "heavy"), Strength its stat, blacksmithing its trade.
- **Base stats** (Innate): (18 + 3L/2, 19 + 2L, 15 + L, 8 + L/2, 10 + L/2); health 175 + 22 a level.
- **Party buff**: **Iron-Born Blood**, +3% Strength for half an hour.
- **Race**: human (the North's Iron-Born). **Trainer**: Brannoc Vell, Pact-Blade Trainer, at the end of the Hall of Arms' row in the
  Crown (x 74, its banner crossed swords on steel-grey; GAME-ONLY). The Iron Citadel takes the trainer when it is built.
- Look: the Warrior's heavy figure for now (`ActorLook.Warrior`), a steel-grey surcoat over it; its own figure in the figure round.

### Abilities (WD is the weapon's hit: attack power)
Base unlocks at 1-10; the path abilities by talent (as the Scoundrel's), from 1, 4 (tier 1) and 9 (tier 2).
| Slot | Ability | Unlock | Cost / cooldown / reach | Does |
|---|---|---|---|---|
| 1 | **Measured Cut** | 1 | 0 / none / 3.2 m | 5 + 0.35 WD; +12 Aether; starts the swings ("minimum wasted movement", B1F:2765) |
| 2 | **Second Blade** | 2 | 0 / 6 s / 3.2 m | the left sword follows: 4 + 0.55 WD; +10 Aether |
| 3 | **Minimum Movement** | 4 | 0 / 15 s / 4-15 m | a dash to the target; +10 Aether |
| 4 | **Hilt Blow** | 6 | 10 / 12 s / 3.2 m | 3 + 0.2 WD; stops its cast, silenced 3 s (the interrupt) |
| 5 | **Iron Vow** | 8 | 0 / 60 s / self | 6 s: you take 20% less (Veyra's sheet: the Iron Vow) |
| 6 | **Sentinel's Stop** | 10 | 20 / 20 s / 3.2 m | 4 + 0.3 WD; rooted 4 s (an elite once a fight, half), slowed 40% (the hold) |
| 7 | Aether Edge | Twin-Blade tier 0 | 30+ / none / 3.2 m | spends all Aether: (8 + 0.5 WD) x spent / 30 |
| 8 | Afterimage | Twin-Blade tier 1 | 25 / 10 s / 3.2 m | 0.9 WD now; its gold afterimage strikes again 2 s later for 60% ("burning afterimages of bright gold", B1F:2765) |
| 9 | Blood-Vow | Twin-Blade tier 2 | 0 / 90 s / self | 12 s: your cuts 20% harder; you lose 1% of your health a second (Veyra's Blood-Vow, her sheet) |
| 10 | White Whirl | White Whirl tier 0 | 30 / none / 5 m | every foe within 5 m: 4 + 0.55 WD ("a whirlwind of... blinding white light", B1F:2765) |
| 11 | Afterglow | White Whirl tier 1 | 20 / 12 s / 4 m | gold light left round you 6 s: foes in it take 3 + 0.12 WD a second |
| 12 | Turning Steel | White Whirl tier 2 | 15 / 45 s / self | 8 s: Measured Cut and Second Blade also cut up to 2 more foes within 4 m of the target, at 60% |
| 13 | Answering Cut | Sentinel tier 0 | 10 / 6 s / 3.2 m | only within 4 s of a blow taken: 0.8 WD, the foe slowed 30% 4 s |
| 14 | Disarming Cut | Sentinel tier 1 | 20 / 15 s / 3.2 m | 0.5 WD; its blows 25% lighter for 8 s (a boss's 10%) |
| 15 | Iron Ring | Sentinel tier 2 | 30 / 30 s / 6 m | every foe within 6 m rooted 3 s and slowed 30% (the Pact's defensive circle) |

### Talents (`Talents/pactblade.json`; tiers 0-4; 22 ranks a branch, 21 points at 20)
**Twin-Blade** (damage, single target): tb-aether-edge (active), tb-keen-pair 5 (Measured Cut, Second Blade and Aether Edge 4% a rank),
tb-quick-blood 3 (Measured Cut +2 Aether a rank); tb-afterimage (active), tb-bright-wake 3 (the afterimage +10% a rank); tb-blood-vow
(active), tb-hot-blood 2 (Burning Blood +5% a rank); tb-measured-end (signature: Aether Edge spent at 80+ gives back 20), tb-redclaw 3
(+3% a rank on foes under 35%: Zygnar's Redclaw Codex), tb-no-wasted (Second Blade 2 s sooner); tb-twin-vow (capstone: Second Blade
leaves an afterimage too, at 40%).
**White Whirl** (damage, area: at least x1.7 against three): ww-whirl (active), ww-wide-turn 5 (the Whirl 4% a rank), ww-reach 3 (+0.5 m
a rank); ww-afterglow (active), ww-lingering 3 (+1 s a rank); ww-turning-steel (active), ww-blinding 2 (the Whirl slows 10% a rank, 3 s);
ww-white-light (signature: each foe the Whirl strikes gives 3 Aether), ww-long-turn 3 (Turning Steel +1 s a rank), ww-steady-feet (10% less
taken while Turning Steel); ww-ring-of-gold (capstone: the Whirl leaves a free Afterglow, once each 12 s).
**Sentinel** (control): sn-answering-cut (active), sn-watchful 5 (Answering Cut 4% a rank), sn-long-stop 3 (Sentinel's Stop +1 s a rank);
sn-disarming-cut (active), sn-heavy-hand 3 (Disarming Cut 3% lighter again a rank); sn-iron-ring (active), sn-held-ground 2 (Iron Ring
+1 s a rank); sn-sentinel (signature: a rooted or slowed foe takes 15% more from your cuts; Veyra's Sentinel, her sheet), sn-patient 3
(Hilt Blow 1 s sooner a rank), sn-goad (Answering Cut turns the foe on you 3 s: the Goading of her sheet); sn-iron-vow-kept (capstone:
Iron Vow roots every foe within 5 m for 3 s).

### Sims (dungeon and raid)
- A melee damage-dealer (`BossBuffs.NominalRole`: Damage; `EncounterSession.Queue.DamageClasses`, so the group finder and the raid fill
  count it in the damage part); an Interrupter (Hilt Blow); it carries both swords; heavy armour. Its blow is the pure band's middle at its
  level (SimCompanion.Damage), so a Pact-Blade sim matches a Rogue's. Area blow: "White Whirl". Eight sims, 89-96, over the bands.
- In a raid it stands at the boss's side with the melee, moves out of the red as every sim does, and kicks casts.

### Tests
EditMode `PactBladeRulesTests` (the tree parses at 15 and 20 with the kit's ids; each branch 22 ranks, tier 4 reachable at 20 points; the
class selectable, human only; the path abilities gated by talent; the Burning Blood and Aether Edge numbers); PlayMode `PactBladeLoopTests`
(every bar ability casts at 20; Aether built and spent; the Whirl hits three; Sentinel's Stop roots); `SimDummyTests` (a Pact-Blade sim in
the pure band at 11, 15, 17 and 20); TalentDataTests, RaceTests, SimRosterTests, CrownZoneTests, IconCoverageTests take it in.
The player's band scale (`BuildDamageScale`) for its three builds is 1 until the balance harness measures them.
