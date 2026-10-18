# Rework: the Lowtowns and Hallow's Creek (a plan to choose from, checked against the books; PROVISIONAL)

Written 2026-10-18; redone the same day against the novels. Design only: nothing built, no game code changed, nothing run. Chris
picks from section 4 ("do 1,2,4,7") and answers section 3.

**Chris, 2026-10-18 ~01:40, verbatim:** "im not sure i like the lowlands or the harrow.seems boring just my opinion". Asked why, he
picked all four: "Looks flat and dull", "Nothing to do", "Fights feel samey", "Layout is a straight line".

**Chris, 2026-10-18, on the first draft of this plan, verbatim:** "please consult the lore canon and redo / edit these choices based
on my descriptions in the novels and we will go from there. i like some of the ideas but want to make sure its lore accurate".

**What is wrong now, in one line each (from the data):** the Lowtowns are dead flat (`hillHeight` 0; one 3 m dip at the burners'
cellar), 161 tenements in one soot-brown, the whole zone read west to east along Gate Street; 17 camps, most of them stand-and-hit
with one move each. Hallow's Creek puts the road, the bridge, Main Street, the Square and the tower on one north-south line (x = 0),
grey on grey, flat ground with its heights only inside halls; its ordinary mobs (Pact soldiers, cutters, clerks, Hollow) fight alike.
**And against the books:** the Lowtowns miss most of what chapter 1 and chapter 3 show (the roofs, the Drop, the river's poison, the
neon moss); Hallow's Creek is grey where the books make it white and perfect, and its first draft here put it on a hill (the books
say a valley) and hung a violet dome over the crater (the books have a glass bowl, a mirror floor and a red door).

**Labels on every pick:** CANON (the books say it; citation given), CANON-EXPANDED (the books give the seed; the game grows it; the
seed is cited), GAME-ONLY (the game's own), CONTRADICTS (the first draft had it; the books say otherwise; changed or dropped).

**Citations:** `b1 ch.N:L` is `D:\code\crulanda\book1\chapter_N.md` at line L (the chapter files Chris's final manuscript was built
from). `WB:L` is `crulanda\world_bible.md`, `B1WB:L` is `crulanda\book1\world_bible.md`, `lore #N` is story N of
`crulanda\lore_stories.md`, `HE doc N` is `crulanda\historical_evidence.md`, `draft ch.N:L` is the early short draft in
`crulanda\chapter_N.md` (weaker than the book; used only as a hint), `map` is `crulanda\maps\argentis_city_map.png`.

Shared rules kept: no machines of any kind (no engines, gears, cranes, lifts, pipes that hiss, clockwork, wheels that drive
anything); no WoW names, layouts or assets; every zone's buildings its own; the game is set shortly before Book 1, and Book 1's people
stay out (Gorm excepted, by Chris's yes); one owner for each faction and set piece (DUNGEON_DESIGN.md: the Sludge-Tunnels own the
red-brick sewer wing, a works-floor riot and the Iron Teeth as a dungeon foe; the Transit Hub owns Seekers, Inquisitors, Redaction and
the Half-Light; the Deep Veins own the Warrens' way down, the Blind Crow and the Bread Drop; the Copper-Tithe are never enemies).
Where the books put one of those in these two zones, section 3 asks Chris instead of deciding.

---

## Chris's decisions (2026-10-18 ~02:00, verbatim)

"keep all. conflicts: take 2 out. 8. fifty years . stone girl is talla. snivel is fine. i never said not use the name characters. it would
be nice to see some of them come alive" ... "like the leader in crowns".
- Picks: **all of them**, both zones (Lowtowns 1-12, Hallow's Creek 1-12).
- Conflict 2 (the Copper-Tithe): **out** of the game.
- Conflict 8: Hallow's Creek fell **fifty years** ago; the stone girl by the willow **is Talla**, Valen's sister (named, CANON per Chris).
- Conflict 9: **Snivels Gear-Grinder** behind the Spoon's bar, as the books write him.
- The books' named characters may come alive in the game (GAME_BRIEF.md standing rules).
- Conflicts 1, 3, 4, 5, 6, 7: not answered separately: the recommended answers stand (1 warders, a Seeker glimpsed far off; 3 the Iron Teeth
  kept in the Lowtowns' turn of the turf; 4 the drowned vaults as the Warrens' upper edge; 5 Dead Man's Drop as a view and a fall onto the
  nets; 6 blue-chalk cache marks; 7 Gorm's line and the dug-out hollow by the willow).

**Built (branch rw-hallows, 2026-10-18; WRITTEN, compiles, NOT RUN):** Hallow's Creek picks 1 (white and perfect), 2 (the silence: the
bubble of still air, no music out of doors, the whisper; no footstep audio exists in the game), 3 (the Square as chapter 21 tells it) and
12 (the people: Mr Gable and his pie, Martha's voice, Mrs Miller, the children, Talla). What was built and what is still to see:
DUNGEON2_DESIGN.md, "The town as the books write it".

**Built (branch w2-ashbridge, 2026-10-18; WRITTEN, compiles, NOT RUN):** Hallow's Creek picks 7 (the Ash Road's dangers: the time-squall
with the overhang as shelter, stone leaves that cut, green fissures, shadows toward the sun) and 10 (the bridge's hanging and turning
slabs, the fifth slab that dips under a foot and tips to 45 degrees once the party is past). What was built and what is still to see:
DUNGEON2_DESIGN.md, "The Ash Road and the bridge as chapter 17 tells them".
**Built (branch w2-hollow, 2026-10-18; WRITTEN, compiles, NOT RUN):** Hallow's Creek pick 6, the Hollow as chapter 4 shows them: out of
cellar trapdoors behind you (ch.4:161), the slow silent shift (ch.4:109), the choking dust (ch.4:177), halves that crawl on (ch.4:181) and
the anchor-well in the Cellars under the well-house (ch.4:191-245). What was built, the numbers and what to run: DUNGEON2_DESIGN.md, "The
Hollow as chapter 4 shows them".

## 1. What the books say (check at a glance)

### The Lowtowns (Argentis's lower city)
1. **Where:** "the sprawling, shadowed districts at the base of the mountain" under the Crown, "filled with industry and poverty"
   (WB:97). From the air: "a sprawling, chaotic sea of slate rooftops", "vibrant, unnatural neon moss", smoke that "stained the sky
   brown" (b1 ch.7:17).
2. **It is vertical, not flat:** the Rust Market sits against "the crumbling stone walls of the Forge District" (b1 ch.3:7); a
   "twisting causeway that led ... down into the deep, dark slums" (ch.3:257); **Dead Man's Drop**, where "the street simply vanished"
   into the Under-City, a rotting fungal net "three stories down" (ch.1:313, 347); the Half-Light's scaffold slum under the High City,
   reached by chain-ladders (ch.1:165-167); below, the Warrens and "drowned lower levels" round an underground river (ch.6:41, 185).
   The early draft: "Forty levels beneath the gilded balconies" (draft ch.3:10).
3. **The Rust Market:** it "did not sleep" (ch.3:5); "roasting skewered-rat", "cheap aether-spiced wine" (ch.3:7); smog "like a
   suffocating blanket of dirty wool" trapping heat and noise in "twisting, narrow alleyways" (ch.3:9); stew pots, carts, canvas
   awnings (ch.1:311; ch.6:11); the Blind Seer's Antiquities, the Copper-Tithe's front (ch.3:13); the Iron Teeth's warehouse with its
   teeth "in faded yellow ochre" on the slate roof, "neutral ground" (ch.7:21).
4. **The Rusty Spoon:** "watered-down ale and high-stakes games" (ch.1:9); it smells "like wet dog and stale beer" (ch.1:7); the game
   is Triad, "Three cards. High card takes it" (ch.1:45); a drunk "butchering The Ballad of the One-Legged Goat" (ch.1:59); Snivels
   Gear-Grinder the goblin bartender (ch.1:105); the brawl "engulfed the tavern instantly" (ch.1:107); out "through the shattered front
   window" into alley mud (ch.1:121); Gorm: "Ale tastes like rust" (ch.1:133).
5. **Streets and roofs:** laundry lines "strung between two tenement blocks" (ch.1:225; ch.7:35); dead-end alleys "hemmed in by high
   brick walls" (ch.1:237); "a rusted rain-spout" up "over the rooftop edge" (ch.1:297); a fire escape (ch.1:261); steep slate roofs,
   brick chimneys, iron gutters, "a precarious water tower" (ch.7:35-41); "freezing puddles of questionable origin" (ch.1:143); the twin
   moons, "The Eye and The Tear" (ch.1:135).
6. **Air, colour, sound:** "a dirty yellow haze" that "coated the tongue in grease and copper" (ch.1:145); from above "a smear of
   orange fires and grey smog" (ch.1:191); "hot, smelting copper and ... long-dead river fish" (ch.9:5); "sickly, yellow light of the
   street lamps" (ch.3:45); "bioluminescent mold" glowing green in the under-ways (ch.6:129; ch.9:55); the High City's brass horns, a
   "two-tone wail" that means the Inquisitors are coming (ch.3:243); the sky hums, "that low thrummmm in your teeth" (lore #3).
7. **The river:** the Chemical River "bisected Argentis like an infected artery" (ch.16:5), "a thick, ugly ribbon of bubbling black
   sludge" (ch.15:335), toxic; "Sludge-eels. Mutated crocodiles." (ch.15:391); its runoff "interferes with magical tracking" (ch.16:75);
   "The river died first. Then the fish." (ch.16:89); "honest labor at the docks" (ch.3:17).
8. **Who is there:** the Copper-Tithe (knees broken over pennies, fruit carts burned, children pointed out to the Inquisitors;
   families smear "silver dust over their windows", ch.3:15-17, 103); the Iron Teeth, "black-market aether" (B1WB:26); Seekers taking a
   sparked girl and Jonas from alleys (ch.1:239-303; ch.2:273-303); "former Lowtown militia" (ch.20:127); an urchin network whose blue
   chalk on a lintel means safe (ch.9:7); the Warrens' silent hand cant (ch.8:41); an orphanage in the outer Warrens (ch.3:105).
9. **Weave-Eaters:** Lowtown mothers' tale of spirits "born from the friction of the High City's industry" who "unravel their dreams"
   (B1WB:121); darkness pooled round a sparked child's ankles, "viscous", like "black smoke" (ch.1:249); in the Wasting they move by
   "a sudden, jerking displacement of space" (ch.20:131), are drawn to a mage's aura "as a lighthouse" (ch.20:121), eat a ward "as fast
   as I cast it" (ch.20:143); harpoons stop them "displacing" (ch.20:191); salt calcifies them (ch.20:205).
10. **Shards and the Lowtowners' rules:** shards are "spent magical batteries used by the poor" (WB:144); the Council's geodes are
   souls, violet, with faces "frozen in ... screaming terror" (ch.3:133-137); Rule One, "If you see a Seeker, exist somewhere else";
   Rule Two, "Never interfere" (ch.1:257-259).

### Hallow's Creek (CANON: the town exists in the books)
1. **What it was:** "The first major settlement confirmed to be consumed by the Wasting" (WB:120); ground zero, "fifty years ago"
   (b1 ch.18:107; ch.19:33); "a ... small, ... idyllic, ... valley town" (ch.19:31); farmland known for "blue wheat" (ch.21:324). The
   early draft adds "sweet-water springs" and grain trade (draft ch.2:18, 68).
2. **The Ash Road:** a stone forest, leaves "sharp enough to cut skin" (ch.17:23); white cobbles "bleached" (ch.17:19); shadows that are
   "an absence of time" (ch.17:11); sound "fell to the ground and died" (ch.17:9); a time-squall of "glowing silver sparks" that ages
   whatever it touches, sheltered from under a rock overhang (ch.17:79-121); fissures "emitting a pale green light" (ch.17:185).
3. **The Fossilized Farm:** a stone farmhouse, a stone dog "Frozen in mid-bark" whose eyes move (ch.17:49, 59); "If you break the shell,
   you kill the soul" (ch.17:67).
4. **The chasm and bridge:** "over a full mile wide", "purple fog" (ch.17:189); the bridge's blast "frozen ... in mid-air", slabs
   hanging; "Some stones were tilted sideways. Some were spinning slowly." (ch.17:193-201); one tips "forty-five degrees" (ch.17:243).
5. **The town:** "Picket fences. White houses. A town square." (ch.17:269); fences "white, without a speck of peeling paint"; roses
   turned to "flakes of red quartz" (ch.21:11); "pristine gravel"; "a bubble of static reality" holds the fog back; "no wind", "no
   birds", a grey sky (ch.21:19); "The static field repels Chaos" (ch.21:54).
6. **The paused:** "completely, terrifyingly grey", eyes "blue. Human. Wet." that follow you (ch.21:25, 34-36); a whisper, "Help us",
   that tastes of copper (ch.21:38-40); a baker with loaves in a doorway, a woman at a laundry line "as rigid as steel wire", two
   children and a ball "suspended in the air" (ch.21:23); dogs "suspended in mid-air" (ch.21:122).
7. **The Glitch and the loop:** the bakery's door-bell, and for a second colour, sun and laughing children (ch.21:82-88); Mr Gable,
   "Fresh pies! Two coppers!", his wife Martha, Mrs Miller at her laundry (ch.21:VI, 370-386); "Take it. Break the loop." (ch.21:442);
   a clock tower stopped at 11:59 (ch.21:58); "The timeline ... has begun to loop" (ch.21:351).
8. **The Square:** the Town Hall "a perfect hemisphere of missing reality", "smooth, glassy obsidian" (ch.21:126); a fifty-foot drop
   (ch.21:134) to a mirror floor whose reflection "wasn't moving" (ch.21:224); a red door on "floating marble" with a knocker face,
   "lips sewn shut" (ch.21:130, 230); debris orbiting: chairs, desks, a stone horse, a vault's corner, a bench, a lamppost (ch.21:136,
   188); the crater wind smells "of ozone and old paper" (ch.21:222).
9. **The day it broke (ch.19):** a weeping willow and "the ancient town well" by the square (ch.19:41); equations "chalked onto the town
   square paving stones" (ch.19:49); the chapel's "stained glass windows" shattered (ch.19:53); a sister petrified over her little
   brother, who was dug out by Gorm ten years before the book (ch.19:39, 53); the void "bloomed directly from the center" (ch.13:21).
10. **Close by in the books:** the Old Archive library (columns "like pulled taffy", an open-book-and-eye crest, ch.18:13-17); a
   Lowtowner's letter: the well water "tasting like ash", shadows "stretching the wrong way at noon", the road shut as a "quarantine",
   "a perfect square of nothing" from the ridge (lore #5). The **Hollow Men** are Oakhaven's (ch.4): they pour "out of ... every dark
   cellar door" (ch.4:161), shift close without a step (ch.4:109), puff choking dust when hit (ch.4:177), and their halves "drag
   themselves" on (ch.4:181); a purple-pulsing well anchors them (ch.4:197); the Bureau's black-iron wagon of "child-sized iron
   manacles" (ch.4:283-293).

---

## 2. What it would feel like after the rework

**The Lowtowns.** The city of chapter 1 and chapter 3, walked downhill. The Rust Market stands high against the Forge District's
crumbling wall, its lanes so narrow the smog sits in them like wet wool; from it the streets fall in shelves and twisting causeways to
the black, poisonous river and the slums along it. Over the streets is a second town on the slate: rain-spouts and fire escapes up,
chimneys, iron gutters and water towers, laundry strung between tenements to drop through. Neon moss glows on the roofs and green
mould in the drains; at night the orange fires come up through yellow smog and the twin moons hang over the iron skyline. Somewhere in
the market a street ends at Dead Man's Drop. By dusk the market wakes, the Spoon's Triad table can boil over into the brawl from page
one, and when the High City's horns give their two-tone wail, the hired warders come for a sparked child and you take her over the roofs.

**Hallow's Creek.** A town too perfect to be real. After the grey of the Ash Road and the hanging bridge, the valley opens below you:
white houses, white picket fences, gravel raked clean, roses of red quartz in the boxes, and every person in it grey stone with wet,
living eyes. No wind, no birds, no echo; your boots on the gravel are the only sound until the whispers start. Main Street runs down to
the Square, where the town hall is a bowl of black glass with a mirror floor that does not show you moving, a red door hangs on its
slab, and chairs, desks and a stone horse drift round it. You choose how to come in (over the slabs past the Iron Pact, or down the
chasm wall and up through the cellars past the Hollow), and the town fights back with what the books gave it: silver rain that ages,
shadows that take time, Hollow that shift close and crawl on in halves, and Glitches where for one minute the sun comes out, the baker
sells his pie, and the town moves.

---

## 3. Conflicts for Chris to decide (one line each; the recommended answer first)
1. **Seekers in the Lowtowns.** The books' child-snatchers in the alleys are Seekers (b1 ch.1:239-303; ch.2:273-303); the Hub owns
   Seekers, so the game has hired warders (your yes, 2026-10-15). Keep warders, with a Seeker only glimpsed on a far roof when the horns
   sound (never fought)? Or warders only?
2. **The Copper-Tithe.** The books' Lowtowns gang (the Blind Seer's back room, kneecaps, burned carts, children pointed out; ch.3:13-17)
   is never an enemy in the game. Show them as informers in the Sweep (pointing, never fought; recommended), or leave them out?
3. **The Iron Teeth** belong to the Sludge-Tunnels as a dungeon foe; the books make their market warehouse "neutral ground" (ch.7:21).
   Keep the Teeth in the Lowtowns as now (a yard by the Outfall; their corners in gang turf, pick 12), or take them out of the turf?
4. **The drowned levels.** The books' "drowned lower levels" and black-water canal are the Warrens' (ch.6:185-187), a future zone with
   the Deep Veins. Build the Drowned Market under the Lowtowns as the Warrens' upper edge (rubble-stone vaults, no red brick;
   recommended), or keep the drowned levels for the Warrens zone?
5. **Dead Man's Drop** is in the Rust Market's lanes in the book (ch.1:313); the Deep Veins have their own sinkhole (the Bread Drop,
   GAME-ONLY). Build the Drop in the Lowtowns as a lip, a view and a fall onto fungal nets (recommended), or leave it out?
6. **Blue chalk and the cant.** The urchins' blue chalk is shown at the Blind Crow (the Deep Veins' way down). Share the custom (chalk
   marks on Lowtowns lintels lead to caches; recommended), keeping the Blind Crow itself for the Warrens?
7. **Gorm and Hallow's Creek.** The books say Gorm dug Valen out of Hallow's Creek's ash ten years before Book 1 (ch.19:39). Give Gorm a
   line about it and put a dug-out hollow by the stone willow (Valen never named; recommended), or keep the two zones apart?
8. **Two contradictions in the books.** (a) Hallow's Creek fell "fifty years ago" (ch.18:107; ch.19:33) but the Council speaks of it as
   lost "within a week" (ch.2:125): the game keeps fifty years. (b) Valen's "only sister" Talla died of the Grey Rot in the Forge
   District (ch.17:300-374), but in ch.19:53 an older sister is petrified shielding him: show a stone girl bent over a hollow by the
   willow, unnamed (recommended), or not?
9. **Snivels Gear-Grinder**, the Spoon's goblin bartender in the book (ch.1:105), who later knows "the forgotten eastern paths"
   (ch.4:13). Add him behind the Spoon's bar beside Mother Sall (a Book 1 person, so an exception like Gorm), or keep him out
   (recommended, the standing rule)?

---

## 4. Pick lists (recommended order; answer "Lowtowns 1,2,4; Hallow's 1,2,3")

**The Lowtowns**
1. **Built downhill** (M): the market high under the Forge wall, lanes falling in shelves and causeways to the river. CANON-EXPANDED (ch.3:7, 257; WB:97)
2. **Colour from the books** (S): yellow smog, orange fires, neon moss, green drain-glow, violet shard-light, the twin moons. CANON (ch.1:135, 145, 191; ch.7:17; ch.6:129)
3. **The roofs** (L): up by rain-spouts and fire escapes, over slate and gutters, down through the laundry; rope bridges added. CANON-EXPANDED (ch.1:225, 261, 297; ch.7:35-41)
4. **Weave-Eaters as the books show them** (M): they blink, go for casters, eat wards; harpoons stop the blink, salt calcifies. CANON (ch.20:121-205) - BUILT 2026-10-18 (rw-lowlife; ZONE_LOWTOWNS.md, "The rework, picks 4 and 5"), not yet run in Unity
5. **The Spoon as in chapter 1** (M): Triad, the Ballad of the One-Legged Goat, the brawl out through the front window. CANON (ch.1:7-133) - BUILT 2026-10-18 with Snivels at the bar (conflict 9), not yet run in Unity
6. **The Chemical River** (S): black iridescent sludge that poisons, sludge-eels, a mutated crocodile rare, wading hides you. CANON (ch.15:335, 391; ch.16:5, 75)
7. **New tricks for the other camps** (M): warders' shield-line, slate-throwers, hounds that circle, barge fights, the Grey-touched's flash. CANON-EXPANDED / GAME-ONLY
8. **The Night Market** (M): the market that never sleeps, the Merchant of Nothing's jars, the candlemaker's light-butterflies, a chalk ring. CANON (ch.3:5; lore #6, #9) + GAME-ONLY ring - BUILT 2026-10-18 (w2-lowlife; ZONE_LOWTOWNS.md, "Dead Man's Drop and the Night Market"), not yet run in Unity
9. **The Sweep** (L): the horns wail, warders sweep a lane, you take a sparked child over the roofs to the soup-hall (needs 3). CANON-EXPANDED (ch.1:239-303; ch.3:243; conflict 1)
10. **Dead Man's Drop** (M): a street that ends in the abyss; a view down, and a fall onto the fungal nets with a ladder out. CANON (ch.1:313, 347; conflict 5) - BUILT 2026-10-18 (w2-lowlife; same section), not yet run in Unity
11. **The smugglers' drowned vaults** (L): under the market, a black canal, a causeway, green glow, a fence's boat, a loop under the river. CANON-EXPANDED (ch.6:185-193; conflict 4)
12. **Landmarks, turf and crossings** (M): the Stack, the Rag Bridge of houses, gang corners by the day, the punt ferry, the weir. GAME-ONLY (turf seeded by ch.7:21; conflict 3)

**Hallow's Creek**
1. **White and perfect, not grey** (S): white houses and fences, clean gravel, red-quartz roses; only the people are grey. CANON (ch.17:269; ch.21:11-25) - fixes a CONTRADICTION
2. **The silence** (S): sound falls flat, no wind, no birds, only boots on gravel; near the paused, the whisper "Help us". CANON (ch.17:9; ch.21:19, 38)
3. **The Square as chapter 21 tells it** (S-M): the glass bowl, the mirror floor that does not move, the red door, the drifting debris, the chalked array. CANON (ch.21:126-230; ch.19:49) - replaces the violet dome (CONTRADICTS)
4. **A valley town seen from the lip** (M): over the bridge the valley opens below; Main Street winds down to the Square, the tower stands up out of it. CANON-EXPANDED (ch.19:31; ch.17:267) - replaces "on a hill" (CONTRADICTS)
5. **Two roads in** (M): the high road over the slabs (the Pact) or the low road down the chasm wall and through the cellars (the Hollow). GAME-ONLY
6. **Hollow as Oakhaven showed them** (M): out of cellar doors behind you, shifting close, blinding dust, halves that crawl on, a well that feeds them. CANON creature (ch.4:109-197), CANON-EXPANDED place
7. **The Ash Road's dangers** (M): a time-squall to shelter from, stone leaves that cut, green fissures, shadows that point the wrong way. CANON (ch.17:11-23, 79-121, 185; lore #5)
8. **New tricks for the Bureau and the Pact** (M): cutters against the clock, Time-Lost echoes, roof crossbows, the shield-wall, "Struck from the record". CANON-EXPANDED / GAME-ONLY
9. **Glitches that change the route** (M-L): a minute of sun and colour that rolls a cart, lands a slate ramp, raises the creek, drops a key. CANON-EXPANDED (ch.21:82-112, 351)
10. **The bridge as the book left it** (M): slabs that tilt and slowly spin, one tipping to 45 degrees as you cross. CANON-EXPANDED (ch.17:193-201, 243) - replaces the rewinding span
11. **Choose the wing first** (S): the Granary, the Cellars and the Archive open together; each done takes a wave off the tower. GAME-ONLY
12. **The people of chapters 19 and 21** (S): Gable's pie and Martha, Mrs Miller, the children's ball, the chapel's hanging glass, the stone sister. CANON (ch.21:VI; ch.19:53; conflicts 7, 8)

---

## 5. The details

Sizes: **S** a day or less, mostly data; **M** two or three days, some new code; **L** a week or so, new code and its tests.
"Reuses" names the code already in the game that carries most of it.

### 5A. The Lowtowns (zone.lowtowns, 15-20)

#### Looks flat and dull
1. **Built downhill (M). CANON-EXPANDED.** The books: the market against the Forge District's wall (ch.3:7), a causeway "down into
   the deep, dark slums" (ch.3:257), the whole at the mountain's base (WB:97); the city map shows the Lowtowns spread below the walled
   High City (map). The game: the **Rust Market** on a +6 shelf under the Forge wall (east); the **Burners' Yard** beside it at +4; the
   **High Rows** a +10 ridge across the north, under the mountain (GAME-ONLY name); the **Tenement Rows** and **Gate Ward** at 0; the
   **Drain Courts** sunk to -4 in a hollow (seen from the High Rows); the **quays** at -2 and the river cut at -4. Shelves joined by
   **step-streets** (stone stairs 4 m wide, a landing every 2 m of rise) and **causeways** (narrow ramps that twist between walls,
   canon's word), one long cart ramp at Gate Street. Edit from the first draft: the market is the top of the town, not a shelf in the
   middle. Reuses: `ZoneTerrace` and ramps, the lane walls as retaining walls.
2. **Colour from the books (S). CANON** unless marked.
   - Smog by day a **dirty yellow-brown** (ch.1:145; ch.7:17), not grey; at night **orange fires** under grey smog (ch.1:191).
   - **Neon moss** on the slate roofs, a sick bright green that glows at night (ch.7:17); **green glowing mould** on the drain arches
     and in the Drain Courts (ch.6:129; ch.9:55). Replaces the first draft's GAME-ONLY "lamp-lichen" colour (the herb keeps its name).
   - Street lamps **sickly yellow** (ch.3:45); the market's **canvas awnings** (ch.6:11).
   - The Burners' Yard: shards glow **violet**, faint faces in them when you look close (CANON-EXPANDED: the geodes, ch.3:133). The
     first draft's "pale blue" is changed (the books' shard-light is violet; the Tuning-Shard's pulsing blue is Nila's and the Hub's).
   - The **Iron Teeth's teeth in faded yellow ochre** on their warehouse's slate roof, seen from the High Rows (ch.7:21).
   - **The twin moons, the Eye and the Tear**, low and pale over the skyline at night (ch.1:135). Needs a second moon in the sky (S).
   - GAME-ONLY, kept small: dyed cloth on a few market lines (the dyer's vat); the Gate Ward's lime-wash.
   - Reuses: `ZoneLighting`, `lane_lamp` (yellow), ZonePost, the sky's moon.
3. **Landmarks seen from afar (M). GAME-ONLY.** **The Stack**, a leaning nine-storey rookery at the High Rows' east end, every window
   lit at night; **the Rag Bridge as a bridge of houses** over the river. The books give no such buildings, but nothing against them.
   The Crown stays on the skyline (built). No tower: Hallow's Creek has the tower.
4. **The river as the books have it (S). CANON.** Black and iridescent, "like a thick, ugly ribbon of bubbling black sludge"
   (ch.15:335), thick "as curdled molasses" (ch.15:391); the banks downriver "skeletons of houses, crushed bridges" (ch.16:255). Barges
   moored three deep (GAME-ONLY). The first draft's mist and white-water weir stay GAME-ONLY. Reuses: `waterTint`, `waterReflect`, `barge`.

#### Nothing to do
1. **The roofs (L). CANON-EXPANDED.** Built from the books' own roofs: steep slate with brick chimneys and iron gutters, water towers
   on stilts (ch.7:35-41), laundry strung between blocks (ch.1:225). **Up** by rusted rain-spouts (the sparked girl's way, ch.1:297)
   and iron fire escapes (ch.1:261); **along** the ridges on plank walks and across lanes on rope bridges (GAME-ONLY); **down** by
   one-way drops through the laundry into a lane (Valen's fall, ch.1:225), onto the market's awnings, into the deepest Drain Court.
   Roof-dwellers' shanty (GAME-ONLY), a vendor and a quest; two lore pages up there only. Blue chalk on a few lintels marks a safe door
   or a cache (CANON custom, ch.9:7; conflict 6). Not the Half-Light: no scaffold climb up under the High City (the Hub's). Reuses:
   open-ground `ZoneDeck`, `ZoneDrop`. New: a rope-bridge deck style (S).
2. **The smugglers' drowned vaults (L). CANON-EXPANDED; conflict 4.** The books: "drowned lower levels" where smugglers and fences cling
   to an underground river, a causeway along "a massive, black-water canal", faint green light on "high, arched ceilings", a boat and a
   sea-shanty drifting up the water (ch.6:185-193). The game: under the market, rubble-stone vaults (never red sewer brick: the
   Sludge-Tunnels'), knee- to chest-deep black water, a fence's moored boat (a vendor who buys), a Weave-Eater nest; three ways in (the
   Spoon's cellar flap, a water-stair from the quays, a crawl from the Drain Courts), so it is a loop under the river. Renamed from "the
   Drowned Market" (the vaults as an old market were GAME-ONLY; kept as a name only if Chris likes it). Rare: **the Old Croc**, a
   mutated river crocodile (CANON creature, ch.15:391; replaces the GAME-ONLY Pale Pike). Reuses: cavern style "cellar",
   `ZoneCaveWater`, hidden doors.
3. **The Night Market (M). CANON market, CANON lore vendors.** The market "did not sleep" (ch.3:5): at dusk the skewered-rat and
   aether-spiced wine stalls light (ch.3:7); **the Merchant of Nothing** sells jars of "premium nothingness" (lore #9: a curio, a
   Discovery); **the candlemaker** whose sparks turn to "little blue butterflies made of light" (lore #6: a lamp-hazard he cannot
   control, harmless, pretty); the Lantern-Eater's lane (built). A chalk fighting ring in the hearth yard, three rounds of hold waves
   for coin and a title, stays GAME-ONLY. Reuses: `WorldClock.IsNight`, `ZoneEvent` hold waves, vendors.
4. **The Spoon as in chapter 1 (M). CANON.** A Triad table (three cards, high card takes it, ch.1:45) beside the game's dice pit, where
   the cheat's "Grand Emperor's Flush" is a joke card you can win (ch.1:87); the corner drunk singing "The Ballad of the One-Legged
   Goat" until the table goes quiet (ch.1:59); **the brawl**: a loser flips the table, "decades of simmering Lowtown resentments"
   boil over (ch.1:107), fists and bottles only, drinkers knocked down not killed; it ends when Gorm throws the worst of them through
   the front window into the alley mud (CANON-EXPANDED from ch.1:103-121; in the book Gorm starts it). New: a "knocked down, not dead"
   rule for brawlers (M). Gorm's own side quest: a fishing pole "that isn't fashioned from scrap wire" for the pier he dreams of
   (CANON-EXPANDED, ch.1:153-159).
5. **The Sweep (L). CANON-EXPANDED; conflict 1, 2.** The books: children "snatched from the dirty alleys and the gutters" (ch.13:143);
   the snatchers are Seekers with a paralysing wand and a sack (ch.2:291-303); the High City's brass horns give a "two-tone wail" when
   the Inquisitors come (ch.3:243); families smear silver dust on their windows (ch.3:103). The game, tied to Gytha's Girl: at dusk the
   horns sound, the Council's hired warders (Chris's yes) sweep a lane; a Copper-Tithe informer points at doors (shown, never fought);
   a sparked girl in a dead-end alley with black smoke pooled at her ankles (ch.1:249, the Weave-Eaters drawn to her); you take her up
   the rain-spout, along the roofs, down to Brother Aldo's soup-hall, warders on the stairs behind you in waves, a lantern-man calling
   more unless you drop him first. Rule One and Rule Two are read on a wall at the start (ch.1:257-259). Reuses: the escort,
   `ZoneWave.unless`, the roofs.
6. **Gang turf that shifts (M). GAME-ONLY; conflict 3.** The books keep a "delicate balance of smuggler cartels and heavily armed
   thieves" round the Teeth's warehouse (ch.7:21). The game: dock-rats and the Iron Teeth's runners hold three corners (the Sett
   Bridge's foot, Teeth Lane, the weir), a gang a corner a day; break a corner's camp to turn it. The dock-rats' corner gives the punt
   ferry and a fence; the Teeth's a back gate into their yard. The warehouse itself stays neutral, as in the book. New: camps chosen by
   the day (M).
7. **Dead Man's Drop (M). CANON; conflict 5.** A lane off the market ends at a lip "where the street simply vanished" (ch.1:313): a
   railed-less edge with loose stones, a view down between two towers into the dark, wind up it. Step off and you land in "a thick,
   rotting fungal-net suspended three stories down" (ch.1:347), with a ladder of iron rungs back to a lane (no way further down: the
   Under-City below is not this zone's). A secret: a card stuck in the net (an Ace of Wyrms, the book's last image, ch.1:353). Reuses:
   `ZoneDrop`, a deck for the net.

#### Fights feel samey
1. **Weave-Eaters as the books show them (M). CANON.** They **blink** 6-8 m, "a sudden, jerking displacement of space" (ch.20:131),
   through lane walls; they go for **whoever casts most** ("as a lighthouse", ch.20:121); a ward or shield on a friend is **eaten**
   "as fast as I cast it" (ch.20:143): an absorb on the struck player drains fast. A bone harpoon **pins** and stops the blink (ch.20:191);
   salt calcifies (ch.20:205); steel through them turns black and brittle (built, ch.20). Reuses: `WeaveEaterRules`. New: the blink
   (a short warp with a shimmer) and the ward-drain.
2. **Slate-throwers on the roofs (S). GAME-ONLY.** Dock-rat and Teeth lookouts on the roofs throw slates into the lanes (ranged, a
   short daze); in a lane you take fire from above until you go up. Reuses: camps with `rise` on decks, `MobCast` bolt.
3. **Hounds that circle (S). GAME-ONLY** (the books have a stray dog and "a massive, aggressive stray mastiff", ch.3:61; ch.7:73).
   Gutter-hounds out of the back yards as an ambush, circle to the back of the group, bolt at a quarter health, come back with the
   pack. Reuses: `ambush`, `flee`, `social: "pack"`.
4. **Warders' shield-line (S). CANON-EXPANDED** (the Council's men, Chris's yes; the books' Seekers are the Hub's). In the narrow lanes
   the warders lock shields across the lane and walk it forward, a net-thrower behind, a lantern-man who runs for more. Reuses:
   EliteMove cc, call, `flee`.
5. **Barge fights in a poisoned river (S). CANON river, GAME-ONLY fight.** The dock-rats fight on their moored barges; the poler's Pole
   Sweep knocks you into the river (built), and the river is **poison** (a tick while you swim, ch.15:391) but **hides you** (a mob
   that loses sight of you in the water drops you, ch.16:75). Reuses: barges, knock, a water hazard.
6. **The Grey-touched's flash (S). CANON-EXPANDED** (the geodes' frozen faces, ch.3:133). A Grey-touched dies in a patch of violet light
   on the floor that dazes whoever stands in it 8 s (someone else's memory). The **Mother of Drains** at half pulls the one she holds
   down her drain into the drowned vaults (needs 5A "Nothing to do" 2). Reuses: `ZoneHazard`, phases.

#### Layout is a straight line
1. **Three layers, many crossings (comes with the above).** The streets, the roofs over them, the drowned vaults under: every district
   has a way up and a way down, so the zone is a web, not Gate Street.
2. **The river crossed five ways (M). GAME-ONLY.** The Sett Bridge and the Rag Bridge (built), a punt ferry poled by a dock-rat (when
   his gang holds the corner), the weir's stepping-stones, the vaults under it. Reuses: `ZoneRide`, decks.
3. **Hidden ways (S). CANON beats.** The Spoon's cellar flap to the vaults (GAME-ONLY); the Blind Seer's back alley, "an unofficial
   dumping ground" of rot and wash-water (ch.3:59), runs through to Skewer Lane (its back door never opened: the Copper-Tithe's); a
   loose board on the roofs drops into the deepest Drain Court behind the Mother of Drains; the chalk marks lead to two caches (ch.9:7).
4. **The market as the hub (S).** Four ways out (down the causeways to the Rows, the stair to the vaults, the Rag Bridge, Teeth Lane);
   the main chain's steps 2 and 3 (Gytha's Girl, What the Drains Keep) open together, either first. Reuses: quests' prerequisites.

### 5B. Hallow's Creek (zone.hallows, dungeon, 15-20)

#### Looks flat and dull
1. **White and perfect, not grey (S). CANON; the current build CONTRADICTS it.** "Picket fences. White houses." (ch.17:269); fences
   "without a speck of peeling paint", roses of "red quartz" in full flower boxes (ch.21:11); "pristine gravel" (ch.21:19). The houses,
   fences, doors and window boxes are clean white and painted colour, the gravel raked, the quartz roses glinting; **only the people,
   the dog and the beasts are grey stone**. The horror is how perfect it is ("It's a painting", ch.21:13). Grey belongs to the Ash Road
   and the stone forest outside the bubble. Reuses: props' materials, `ZonePost`.
2. **The bubble and the sky (S). CANON.** "A bubble of static reality" holds the fog back: the fog is thinner inside (ch.21:19), so
   walk out of the town's edge and the grey closes in. Inside, a grey sky; outside, the Wasting's purple bruise and "perpetual,
   unbroken twilight" (ch.17:79-81). Reuses: fog by area, ZoneLighting.
3. **A valley town seen from the lip (M). CANON-EXPANDED; the first draft's "town on a hill" CONTRADICTS.** The books: "a ... small,
   ... idyllic, ... valley town" (ch.19:31) that appears "through the clearing fog" past the chasm (ch.17:267). The game: the bridge's
   far end and the gatehouse stand on the valley's lip at +10; Main Street winds down in three steps to the Square on the valley floor
   at 0, by the creek, the willow and the well; the bell tower's belfry at +28 stands up out of the bowl so it is on the skyline from
   the milestone; the Archive's steps climb the far side. The road bends round the Farm and Main Street doglegs at the bakery (nothing
   on one axis). Reuses: `ZoneTerrace` and ramps, halls on terrain (N2, built).
4. **The Square as chapter 21 tells it (S-M). CANON; the first draft's violet dome CONTRADICTS.** The town hall is gone: "a perfect
   hemisphere of missing reality", smooth glassy obsidian (ch.21:126), fifty feet down (ch.21:134) to a **mirror floor** whose
   reflection does not move when you do (ch.21:224); the **red door** "Bright, wet, impossible red" on a floating marble slab (ch.21:130),
   its knocker a face with lips sewn shut (ch.21:230), never reached (decision 7); round it drift chairs, desks, bricks, a stone horse,
   a vault's corner with its iron bars, a bench, a lamppost (ch.21:136, 188); the wind up out of it smells "of ozone and old paper"
   (ch.21:222). On the rim's paving, the chalked equations and the central glyph (ch.19:49). Reuses: the crater prop, `hangball`
   generalised, a reflection that is a still render.
5. **Colour and stopped motion (S). CANON** unless marked. Red-quartz roses (ch.21:11), blue wheat in the Farm's field (ch.21:324),
   the red door, pale green light out of the road's fissures (ch.17:185), the purple fog in the chasm (ch.17:189). Stopped: the ball over
   the children (ch.21:23), dogs in mid-air (ch.21:122), Mrs Miller's laundry "as rigid as steel wire" (ch.21:23), stone smoke over the
   pie (ch.21:68), the chapel's stained glass hanging in the air as it burst (CANON-EXPANDED, ch.19:53), the creek's splash at the ford
   as glass (GAME-ONLY). Dropped from the first draft: **birds** hanging over Main Street (CONTRADICTS: "There were no birds", ch.21:19)
   and **washing blown sideways** (CONTRADICTS: "no wind"); rain stopped mid-fall stays GAME-ONLY (no canon against it). The books'
   bicycle frozen mid-spin (ch.21:11) becomes a handcart with one wheel off the ground (no machines); the mailbox (ch.21:16) is left out.
   Reuses: `hangball`, particles at zero speed.
6. **The silence (S). CANON.** Sound "fell to the ground and died" (ch.17:9); "no wind", "no birds", "Just the crunch of their boots"
   (ch.21:19). A heavy low-pass on everything, no reverb, the party's voices flat; the ambient bed is nothing but footsteps on gravel.
   Near a paused townsperson, a whisper, "Help us" (ch.21:38), and a copper taste (a screen tint). Reuses: the audio mixer.

#### Nothing to do
1. **Glitches that change the route (M-L). CANON-EXPANDED.** The books: the bakery's bell rings and "the grey washed away", sun,
   laughing children, the baker selling his pie (ch.21:82-94); "You could have been trapped in there with him!" (ch.21:112); "The
   timeline ... has begun to loop" (ch.21:351). The game: four Glitch spots besides the bakery, each a short wake (40-60 s, colour and
   sun back for its minute, waves as now) that leaves the town changed for the visit:
   - the **stone handcart** in Cooper's Lane rolls on and clears the lane: a short way from the Reed house to the Granary's footbridge;
   - the **slater's fall** on the laundry's roof finishes: the slates land as a ramp to the east row's roofs and the Archive's gallery;
   - the **ford's splash** runs on: the creek rises a metre and floods the Cellars' long vault but drains the dark cellar's crawl;
   - the **bell-ringer's boy** at the tower door finishes his run and drops a key: the tower's side door, a second way up.
   Anyone still standing in a Glitch's ring when it snaps is paused (built: the books' "trapped in there"). The four moments are
   GAME-ONLY. New: a gate or deck an event opens (S), water raised by an event (M).
2. **Choose the wing first (S). GAME-ONLY.** From the Square the Granary, the Cellars and the Archive open together, signposted; the
   Granary down takes the Pact wave off the belfry stair, the Cellars down the Hollow wave; the Archive is needed (the Warden).
3. **The people of chapters 19 and 21 (S). CANON.** **Gable's pie**: in the bakery's Glitch, take the pie he holds out ("Take it. Break
   the loop.", ch.21:442) and the loop ends for him: his shell cracks and he is still at last (the books' wish; "break the shell, kill
   the soul", ch.17:67, made mercy). **Martha** calls from the back (ch.21:386). **Mrs Miller** at her rigid laundry (ch.21:376). The
   two children and their ball. **The stone girl** by the willow, bent over a hollow in the ash where someone was dug out (ch.19:53, 39;
   conflicts 7, 8). Three half-cut townsfolk laid to rest with the Preservationists' rite (GAME-ONLY rite; Warden Ivel's line).
   Reuses: quests, the Glitch, Discoveries.
4. **The Fossilised Farm awake (S). CANON farm, GAME-ONLY wake.** The farm's own small Glitch: the stone dog (ch.17:49) barks once and
   its eyes follow you (ch.17:59), the Old Ram wakes and the farm's carters with it. Blue wheat in its field. Reuses: rares, hold.
5. **The time-squall (M). CANON.** On the Ash Road the sky darkens to purple and silver sparks fall (ch.17:79-97): a 30 s event, warned;
   under the rock overhang (built, the books' shelter, ch.17:93) you are safe; caught in the open, each spark ages you (a stacking
   weakness that lasts the run until a spirit stone), and a stone sapling by the road grows, dies and falls in seconds (ch.17:121).
   The first draft kept squalls as dressing only (decision 9); this makes them a thing to do. New: a weather event with shelter areas (M).

#### Fights feel samey
1. **Hollow as Oakhaven showed them (M). CANON creature (b1 ch.4), CANON-EXPANDED place.** The books put the Hollow Men in Oakhaven and
   keep Chaos out of the static field (ch.21:54); the game's Hollow are the cutters' broken townsfolk (decided 2026-10-15). As the
   books show them: they **pour out of cellar doors and alleys** behind the party (ch.4:161); they **shift close** without a step, "a
   violent, localized reality shift" (ch.4:109); each hit **puffs choking grey dust** that stings the eyes (ch.4:177: a short miss chance
   on whoever hit them); cut one at a quarter health and **its halves crawl on** (ch.4:181: two weak crawlers); their stutter, "Help...
   hel... lp" (ch.4:107). The **well-house's well** pulses purple while the Cellars' Hollow keep coming, as Oakhaven's did (ch.4:197);
   seal it (a channelled interact, held against a wave) to stop them. Reuses: ambush, `MobCast`, the split as a death-spawn. New: the
   dust, the shift, the halves.
2. **Cutters against the clock (M). CANON-EXPANDED** ("If you break the shell, you kill the soul", ch.17:67; the geodes as souls,
   ch.3:133). At each of the three cutting spots the cutters are at work when you come: not stopped in 15 s, they break their
   townsman, and he rises as a Hollow mid-fight. New: a timer that releases a camp unless a camp is dead.
3. **Time-Lost echoes (M). CANON-EXPANDED** (the books' name for the paused, "a Time-Lost Echo", ch.17:55; the hungry shadows, ch.17:11).
   Pact soldiers fought inside the shadows leave an echo: a grey after-image that repeats each heavy blow at the same spot 2 s later
   (telegraphed). Step out of where you were. Reuses: EliteMove's ground telegraphs.
4. **Shadows that point the wrong way (S). CANON** ("the shadows were stretching the wrong way at noon", lore #5; "they're hungry",
   ch.17:11). The hazard strips (built) lie toward the light, not away from it, so they read as wrong at a glance, and at the Waking
   Hour they swing round. Reuses: the strips.
5. **Crossbows on the roofs, a shield-wall in the street (S). GAME-ONLY.** Pact crossbowmen on the roofs and the gatehouse wall-walk
   shoot down; a shield-wall walks Main Street at you between the fences. Reuses: camps with `rise`, root, Tredway's shield-wall.
6. **Clerks that undo (S). GAME-ONLY.** The Bureau clerks' "Struck from the record": every 20 s one clerk wipes the last 5 s of damage
   off a friend unless interrupted. The Bureau's own (its black sunburst is CANON, ch.3:145).

#### Layout is a straight line
1. **Two roads into town (M). GAME-ONLY.** The high road (the slabs, the gatehouse, down Main Street: the Iron Pact) or the low road
   (down the chasm wall, then the Cellars under the whole street to the well-house: the Hollow). One is enough to reach the Square; the
   other is optional for a full run. Reuses: `ZoneWing.group` ("of which one is enough", built for the Adit).
2. **The descent (L). GAME-ONLY, in a CANON chasm.** Off the second slab a stair of small slabs spirals down the chasm wall past the
   Fallen Mason to a ledge at -30 over the purple fog (the books' chasm is "seemingly entirely bottomless", ch.17:189: no floor is ever
   shown); a frozen fall of the creek, a cave mouth, the low road up into the Cellars. A fall puts you on the last slab (built).
3. **The bridge as the book left it (M). CANON-EXPANDED; replaces the first draft's rewinding span.** "Some stones were tilted
   sideways. Some were spinning slowly." (ch.17:201); one tips "forty-five degrees" under the runners (ch.17:243). Two side slabs turn
   slowly (step on as they come round, a fight on them is a race); one slab on the high road tips to 45 degrees for 10 s every minute
   (slide off its low edge and you land on the lower orbit). Kaelen's stasis is never used. Lighter than the rewinding span (decks that
   move, but never vanish). New: moving decks with the navmesh (M-L, still the riskiest item on this list).
4. **The Archive to the tower by a flying bridge (M). GAME-ONLY.** A stone flying bridge from the Archive's gallery at +9 to the tower's
   second landing: in at the bottom or across at the top.
5. **Shortcuts that open behind you (S).** The sally-port (built), the well-house trapdoor (built), the handcart's lane and the slater's
   ramp (the Glitches above), the wagon home (built). Each area leaves a quicker way back to the last spirit stone.

### 5C. What is honestly new code
- The Lowtowns: rope-bridge deck style (S); a second moon (S); camps chosen by the day (M); a knocked-down-not-dead rule for brawlers
  (M); the Weave-Eater blink and ward-drain (M); a swim that poisons and drops pursuit (S).
- Hallow's Creek: a gate or deck opened by an event, water raised by an event (S-M); the cutters' timer (M); the Hollow's dust, shift
  and halves (M); the echoes (M); "Struck from the record" (S); the time-squall with shelter (M); the low-pass silence (S); moving decks
  with the navmesh (M-L).
- The sims: each new way (the roofs, the low road, the wings in any order) needs the leader's plan and the navmesh to know it; the
  Glitches must never leave a required way shut; the sims shelter from the squall. Every piece gets its own targeted tests and front,
  back and side captures.

### 5D. Kept out on purpose (and why)
- Steam pipes in the market (ch.1:311), the gondola (ch.1:187), Horgus's boiler barge (ch.16), the clock's works (ch.21:58), the
  bicycle (ch.21:11): machines. The mailbox: an anachronism.
- The Half-Light and its chain-ladders (the Hub's); the Blind Crow and the Rust & Ruin with its Raid Bell (the Warrens' and Book 1's);
  the Blind Seer's back room (the Copper-Tithe's); Seekers and Inquisitors fought (the Hub's); a street riot (the Sludge-Tunnels own a
  riot; the books' Shatter-Riots and bread riots stay talk, ch.1:155, lore #2).
- Book 1's people: Valen, Rill, Silas, Veyra, Kaelen, Vala in person, Krunk (superseded 2026-10-18: Chris, "i never said not use the name characters"; Snivels is in, conflict 9; Gorm by his earlier yes).
- Hallow's Creek's end (ch.23: the statues screaming, the town unwritten): after the game's time.
- "The Cabbage Rogue" custom (lore #10): after Book 1.
