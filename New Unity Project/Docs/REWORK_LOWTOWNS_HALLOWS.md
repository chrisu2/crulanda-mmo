# Rework: the Lowtowns and Hallow's Creek (a plan to choose from; PROVISIONAL)

Written 2026-10-18. Design only: nothing built, no game code changed, nothing run. Chris picks from section 3 ("do 1,2,4,7").

**Chris, 2026-10-18 ~01:40, verbatim:** "im not sure i like the lowlands or the harrow.seems boring just my opinion". Asked why, he
picked all four: "Looks flat and dull", "Nothing to do", "Fights feel samey", "Layout is a straight line".

**What is wrong now, in one line each (from the data):** the Lowtowns are dead flat (`hillHeight` 0; one 3 m dip at the burners'
cellar), 161 tenements in one soot-brown, the whole zone read west to east along Gate Street; 17 camps, most of them stand-and-hit
with one move each. Hallow's Creek puts the road, the bridge, Main Street, the Square and the tower on one north-south line (x = 0),
grey on grey, flat ground with its heights only inside halls; its ordinary mobs (Pact soldiers, cutters, clerks, Hollow) fight alike.

Shared rules kept: no machines of any kind (no engines, gears, cranes, lifts, wheels that drive anything); no WoW names, layouts or
assets; every zone's buildings its own; labels on every name. The "one owner" table (DUNGEON_DESIGN.md) is kept too: a red-brick sewer
wing and a works-floor riot are the Sludge-Tunnels'; Seekers are the Transit Hub's. So the Lowtowns' undercroft below is old stone, not
sewer brick, and its trouble is a tavern brawl and a warders' sweep, not a riot and not Seekers.

---

## 1. What it would feel like after the rework

**The Lowtowns.** A city you climb, not a street you walk. The slums step up in shelves from the black river to the foot of the
Crown's mountain, and over them runs a second town on the roofs: plank walks, rope bridges between the tenements, washing lines you
drop through to get down fast. Under the market lies the Drowned Market, the old stone vaults the river took. Each district has its
own colour (amber market lamps and dyed cloth, green lamp-lichen in the Drain Courts, cold blue shard-light at the Burners' Yard), and
the Stack, a leaning nine-storey rookery, is seen from everywhere. By day it is a grind town; at dusk the Rust Market comes alive, the
Spoon's dice pit can boil over into a brawl, and the Council's warders sweep a lane for sparked children while you get one out over
the roofs.

**Hallow's Creek.** A frozen town on a hill that you see from the first step: its bell tower over the chasm, the town hall's blast
hanging over the Square as a cracked dome of violet light that every shadow in the town points away from. You choose how to get in:
the high road over the hanging slabs (the Iron Pact) or the low road down the chasm wall and up through the cellars (the Hollow), and
which wing to take first. Frozen moments wait along the way; break one and the town moves for a minute: a stone cart rolls out of a
lane, a fall of slates becomes a ramp to the roofs, the creek pours on and drowns a cellar. Fights are timed against the town:
cutters racing to break a paused townsman, Hollow tearing out of doors, soldiers' echoes repeating their blows a beat late.

---

## 2. The details

Sizes: **S** a day or less, mostly data; **M** two or three days, some new code; **L** a week or so, new code and its tests.
"Reuses" names the code already in the game that carries most of it.

### 2A. The Lowtowns (zone.lowtowns, 15-20)

#### Looks flat and dull
1. **Three heights (M).** The ground in shelves: the river cut at -4 and the quays at -2; the Tenement Rows and Gate Ward at 0; the
   Rust Market and the Burners' Yard on a +5 shelf under the Forge District's wall; the **High Rows** a +10 to +12 ridge across the
   north between the Spoon and the Rag Bridge, under the mountain. The **Drain Courts** go the other way: sunk to -4 in a hollow, so
   you look down into them from the High Rows. Shelves joined by **step-streets** (stone stairs 4 m wide, a landing every 2 m of rise,
   lamps at each landing) and one long ramp for carts at Gate Street. GAME-ONLY. Reuses: `ZoneTerrace` and ramps (ZoneBuilder.Crown's
   terraces), the lane walls as shelf retaining walls.
2. **Colour by district (S).** The market: amber lamps, dyed awnings and cloth strung over the lanes in madder red, saffron and woad
   blue from the dyer's vat (Weaving's dyes made visible). The Drain Courts: the lamp-lichen glowing sick green on the wet brick. The
   Burners' Yard: the shards' pale blue light from the racks and windows (the shards are memory: they glow cold, GAME-ONLY). The Gate
   Ward: lime-washed walls, the only white in the Lowtowns. The Docks: tar-black with river lanterns on poles. At night the smog over the
   Forge District's wall glows orange from below (CANON: "a smear of orange fires and grey smog", b1 ch.1:191). Sun shafts where the smog
   thins (the vista secret already there). Reuses: `ZoneLighting`, `lane_lamp` (a colour per district), `dye_vat`, `washing_line`, ZonePost.
3. **Landmarks seen from afar (M).** **The Stack** (GAME-ONLY): a nine-storey rookery at the High Rows' east end, leaning on its
   neighbour, every window lit at night, a lean-to shanty on its roof: the roof route's summit and a lookout. **The Rag Bridge as a bridge
   of houses** (GAME-ONLY): the north bridge carries two rows of narrow timber shops and homes, their backs hanging over the black water,
   lamps reflected under it. The **Crown** stays on the skyline (built). No tower: Hallow's Creek has the bell tower. Reuses: the
   `tenement` builder (a tall, leaning variant), `brick_bridge`, `CrownSkyline`.
4. **The river with a face (S).** An oily sheen on the black water that catches the lamps in colour, mist off it at night, a weir at the
   south end that white-waters, barges moored three deep. Reuses: `waterTint`, `waterReflect`, the zone water path, `barge`.

#### Nothing to do
1. **The Leads: the roof route (L).** A second way through the zone at +12 to +16 on the tenement roofs: plank walks along the ridges,
   **rope bridges** over the lanes (plank floors, rope hand-lines), a shanty camp of roof-dwellers (friendly; a vendor and a quest),
   pigeon lofts, chimney pots. Up by an outside timber stair at the soup-hall, a stair inside the Stack, and the **rain-spout climb**
   by the Rusty Spoon (CANON beat: the sparked girl "scrambled up a rusted rain-spout ... over the rooftop edge", b1 ch.1:297). Down by
   one-way drops: through the washing lines into Pell Row (CANON beat: Valen through a laundry line, b1 ch.1:225), onto the market's
   awnings, into the deepest Drain Court. Secrets and two lore pages live up there only. Reuses: open-ground `ZoneDeck` (HallowsGround),
   `ZoneDrop` (one-way), the navmesh reach to the highest deck. New: a rope-bridge deck style (S).
2. **The Drowned Market (L).** Below the Rust Market: the old market vaults, built before the river was walled, now half drowned
   (GAME-ONLY). Rubble-stone vaults and old shop fronts (not red sewer brick: that is the Sludge-Tunnels'), knee- to chest-deep black
   water, a stair up into the market's hearth yard, a water-stair up to the Docks and a crawl to the Drain Courts: three ways in, so it
   is a loop under the river, not a dead end. The dock-rats' stash, a Weave-Eater nest, a rare (the **Pale Pike**, a blind eel as long
   as a barge). Reuses: cavern props of style "cellar" (Hallow's Creek's Cellars), `ZoneCaveWater` flood, hidden doors.
3. **The Night Market and the Spoon's brawl (M).** At dusk the Rust Market changes (CANON: the market "did not sleep", b1 ch.3): night
   stalls, a moth-lamp seller with goods sold only after dark, a fighting ring of chalk in the hearth yard (three rounds of hold waves for
   coin and a title), and the Lantern-Eater's lane. At the Rusty Spoon the dice pit can boil over (CANON: "the brawl engulfed the tavern
   instantly", b1 ch.1:107): a bar-room brawl event, fists only, the drinkers knocked down rather than killed; Gorm Rock-Wall ends it.
   Reuses: `WorldClock.IsNight` and night camps, `ZoneEvent` hold waves, vendors, the Spoon's dice pit; `flee` for the downed. New: a
   "knocked down, not dead" rule for brawlers (M).
4. **The Sweep (L).** The zone's heart, tied to Gytha's Girl: at dusk the Council's hired warders sweep a lane for sparked children
   (CANON beat: children snatched "from the dirty alleys and the gutters", b1 ch.13:143; Rule One). You get a child out: up the
   rain-spout, along the Leads, across a rope bridge, down to Brother Aldo's soup-hall, warders coming up the stairs behind you in waves,
   a warder's lantern-man calling more unless you drop him first. Reuses: the escort (the Warden's walk), `ZoneWave.unless`, the Leads.
5. **Gang turf that shifts (M).** The dock-rats and the Iron Teeth's runners fight over three corners (the Sett Bridge's foot, Teeth
   Lane, the weir): each game day one gang holds each, its chalk marks on the walls and its camp there. Break a corner's camp and its
   boss to turn it for the day: the dock-rats' corner gives the punt ferry (below) and a fence who buys; the Teeth's corner a back gate
   into the warehouse yard. Reuses: camps, `rare` (rolled by day instead of by visit), Lowtowners standing. New: camps chosen by the day (M).

#### Fights feel samey
1. **Weave-Eaters that drift through walls (M).** They move as in canon, "a sudden, jerking displacement of space" (b1 ch.20:131): a
   blink of 6-8 m every few seconds, through lane walls, so they come at the healer. Salt fixes them in place (built), a harpoon pins.
   Reuses: `WeaveEaterRules`, salt and harpoons. New: the blink (a short warp with a shimmer).
2. **Slate-throwers on the roofs (S).** Dock-rat and Teeth lookouts on the Leads throw slates down into the lanes (ranged, a short
   daze): in a lane you take fire from above until you go up. Reuses: camps with `rise` on decks, `MobCast` bolt.
3. **Hounds that circle (S).** Gutter-hounds come out of the back yards as an ambush (the tall-grass ambush, from yard gates), circle to
   the back of the group, bolt at a quarter health and come back with the pack. Reuses: `ambush`, `flee`, `social: "pack"`, Hamstring.
4. **Warders' shield-line (S).** In the narrow lanes the warders lock shields across the lane and walk it forward (root on whoever stands
   at the line, as Kersey's "Form the line"), a net-thrower behind, a lantern-man who runs for more. Reuses: EliteMove cc, call, `flee`.
5. **Barge fights and the Grey-touched's flash (S).** The dock-rats fight on their moored barges: the poler's Pole Sweep knocks you
   into the river (built: knock 2.5) and you swim back. A Grey-touched dies in a **memory flash**, a patch of cold light on the floor
   that dazes whoever stands in it for 8 s (someone else's memory). And the **Mother of Drains** at half pulls the one she holds down
   her drain: the fight finishes in the Drowned Market (needs the Drowned Market; M). Reuses: barges, knock, `ZoneHazard`, phases.

#### Layout is a straight line
1. **Three layers, many crossings (comes with the above).** Streets, the Leads over them, the Drowned Market under: every district
   has a way up and a way down, so the zone is a web, not Gate Street.
2. **The river crossed five ways (M).** The Sett Bridge and the Rag Bridge of houses (built), a **punt ferry** poled by a dock-rat (once
   his gang holds the corner; GAME-ONLY), the **weir's stepping-stones** at the south end, and the Drowned Market under it. Reuses:
   `ZoneRide` (the wagon's ride, as "Pole across"), decks.
3. **Hidden ways (S).** The Spoon's cellar flap opens on a crawl to the Drowned Market; the Blind Seer's back alley (CANON: "the narrow
   alleyway running behind The Blind Seer", b1 ch.3:59) runs through to Skewer Lane; a loose board on the Leads drops into the deepest
   Drain Court behind the Mother of Drains. Reuses: `hiddendoor` gates, drops, secrets.
4. **The market as the hub (S).** The Rust Market gets four ways out (the step-street up to the High Rows, the stair down to the Drowned
   Market, the Rag Bridge, Teeth Lane), and the main chain's steps 2 and 3 (Gytha's Girl, What the Drains Keep) open together, either
   first. Reuses: quests' prerequisites.

### 2B. Hallow's Creek (zone.hallows, dungeon, 15-20)

#### Looks flat and dull
1. **The town on a hill (M).** The land rises from the chasm's far lip: the gatehouse at 0, Main Street climbing in three steps to
   +8, the Square on top at +10, the Archive's steps to +14, the tower's belfry at +28 over the yard. From the yard at the milestone the
   tower and the hanging blast are on the skyline across the chasm: you see the end at the start. Reuses: `ZoneTerrace` and ramps, the
   halls on terrain (N2 built), decks keep their heights.
2. **The frozen blast (S-M).** Over the crater, the town hall's blast hangs frozen as the bridge's did (CANON-EXPANDED: the bridge's
   blast froze, b1 ch.17): a cracked dome of violet light with the debris orbiting in it (CANON: the debris round the red door). It lights
   the town from the middle; every shadow in the town points away from it, so the hungry shadows read as one thing. Reuses: the crater
   prop, a light and a glass shell, the hazard strips laid along the radial lines.
3. **Colour in the grey (S).** What canon gives: roses turned to red quartz flakes in the window boxes (CANON, b1 ch.21:11), blue
   wheat in the farm's field (CANON), the red door, pale green light out of the road's fissures (CANON, b1 ch.17:185), the purple fog in
   the chasm (built). The quartz glints in the dusk; the Waking Hour's colour swing (built) then has more to swing to. Reuses: props'
   variants, `ZonePost.Swing`.
4. **Motion stopped everywhere (S).** A flock of birds hanging over Main Street, the creek's splash at the ford standing as glass,
   rain stopped in mid-fall over the Square, washing on the laundry's lines blown out sideways and stone. Reuses: the `hangball` prop
   generalised, particles with zero speed.

#### Nothing to do
1. **Frozen moments that change the route (M-L).** Four Glitch spots besides the bakery, each a short wake (40-60 s, waves as now)
   that leaves the town changed for the visit:
   - the **stone cart** in Cooper's Lane rolls on and clears the lane: a short way from the Reed house to the Granary's footbridge;
   - the **slater's fall** on the laundry's roof finishes: the slates land as a ramp up to the roofs of the east row (a roof walk to
     the Archive's gallery window);
   - the **ford's splash** runs on: the creek rises a metre and floods the Cellars' long vault (swim it, or wait for the snap) but drains
     the dark cellar's crawl;
   - the **bell-ringer's boy** at the tower door finishes his run and drops a key: the tower's side door, a second way up.
   CANON: "The timeline in the immediate vicinity has begun to loop" (b1 ch.21:351). Reuses: `ZoneEvent` hold, `ZonePost.Swing`, gates
   with `opensOn` an event, the paused townsfolk. New: a gate or deck that an event opens (S), water raised by an event (M).
2. **Choose the wing first (S).** From the Square three wings open at once (the Granary, the Cellars, the Archive), signposted, and
   each one done changes the tower: the Granary down takes the Pact wave off the belfry stair; the Cellars down takes the Hollow wave;
   the Archive is needed (the Warden). Already half there: every wing is `after: street`, and `ZoneWave.unless` does the rest.
3. **Mercy for the paused (S).** Gable's pie (CANON interlude: "Take it. Break the loop", b1 ch.21:442): a side quest in the bakery's
   Glitch to give the baker his release; three half-cut townsfolk whose eyes follow you, laid to rest with the Preservationists'
   rite (Warden Ivel's line, decision 13). Reuses: quests, the Glitch, Discoveries.
4. **The Fossilised Farm awake (S).** The farm's own small Glitch: the stone dog barks once (CANON: "frozen in mid-bark", b1 ch.17:49),
   the Old Ram wakes and the farm's carters with it. Reuses: rares, hold.

#### Fights feel samey
1. **Cutters at the paused, against the clock (M).** At each of the three cutting spots the cutters are at work when you come: if they
   are not stopped in 15 s they break their townsman, and he rises as a Hollow mid-fight (CANON-EXPANDED: break the shell, kill the
   soul). Reuses: `after` released camps, a timed wave. New: a timer that releases a camp unless a camp is dead.
2. **Hollow out of the doors (S-M).** The Hollow "pour from cellar doors" (CANON, b1 ch.4) as you pass, two or three a door, behind
   the party, and puff dust when struck (a short blind: a miss chance on whoever hit them). Reuses: ambush, `MobCast`. New: the dust.
3. **Echoes (M).** Pact soldiers in the time-field leave an echo: a grey after-image that repeats each heavy blow at the same spot
   2 s later (telegraphed on the ground). Step out of where you were. GAME-ONLY. Reuses: the ground telegraphs of EliteMove.
4. **Crossbows on the roofs, a shield-wall in the street (S).** Pact crossbowmen on the roofs and the gatehouse wall-walk shoot down;
   a shield-wall walks Main Street at you between the fences. Reuses: camps with `rise`, root, the shield-wall at half (Tredway's).
5. **Clerks that undo (S).** The Bureau clerks' ward becomes "Struck from the record": every 20 s one clerk wipes the last 5 s of
   damage off a friend unless interrupted. Distinct, readable, the Bureau's own. Reuses: EliteMove mend, interrupts.

#### Layout is a straight line
1. **Two roads into town (M).** The high road (the slabs, the gatehouse, Main Street: the Iron Pact) or the low road (the descent below,
   then the Cellars under the whole street, from the gatehouse's undercroft to the well-house: the Hollow). One is enough to reach the
   Square; the other is optional for a full run. Reuses: `ZoneWing.group` ("of which one is enough", built for the Adit), the Cellars.
2. **The descent (L).** Off the second slab a stair of small slabs spirals down the chasm wall past the Fallen Mason to a ledge at -30
   over the purple fog: a frozen waterfall of the creek, a cave mouth, and the low road's way up into the Cellars. The fog rule stays
   (a fall puts you on the last slab). Reuses: decks, drops, the pit and its fog, cavern props.
3. **Bend the line (M).** Nothing on one axis: the Ash Road bends west round the farm, Main Street doglegs at the bakery, the Square is
   walked round the crater's rim (the tower reached from the far side), the Archive joined to the tower by a **stone flying bridge** from
   its gallery at +9 to the tower's second landing: in at the bottom or across at the top. Reuses: roads, decks, gates.
4. **Shortcuts that open behind you (S).** The sally-port (built), the well-house trapdoor (built), the stone cart's lane and the
   slater's ramp (the frozen moments above), the wagon home (built). The run leaves each area with a quicker way back to the last still lamp.
5. **The rewinding span (L, a risk).** On the high road one span of the bridge rewinds with the loop: every two minutes its blown
   pieces fly back together for 40 s, then burst apart again; cross in time or wait, and a fight on it is a race. New code: decks that
   come and go, the navmesh with them, sims that wait for it. Bold, and the hardest thing on either list.

### 2C. What is honestly new code
- Rope-bridge deck style (S); camps chosen by the day (M); a knocked-down-not-dead rule for brawlers (M); the Weave-Eater blink (M).
- A gate or deck opened by an event, water raised by an event (S-M); the cutters' timer (M); the Hollow's dust and the echoes (M);
  "Struck from the record" (S); decks that come and go with the navmesh (L).
- The sims: each new way (the Leads, the low road, the wings in any order) needs the leader's plan and the navmesh to know it; the
  frozen moments must never leave a required way shut. Every piece gets its own targeted tests and front, back and side captures.

### 2D. Lore labels
| Thing | Label | Source |
|---|---|---|
| Rooftops reached by a rain-spout; a laundry line between tenements; the Spoon's brawl; the market that does not sleep; sparked children taken from the alleys; the Blind Seer's back alley; Weave-Eaters' jerking movement; the orange fires under the smog | CANON (beats) | b1 ch.1:107, 191, 225, 297; ch.3:7, 59; ch.13:143; ch.20:131 |
| The quartz roses, blue wheat, green fissure light, the stone dog mid-bark, the loop, Gable's pie | CANON | b1 ch.17:49, 185; ch.21:11, 351, 442 |
| The town hall's blast frozen like the bridge's; the cutters breaking a paused into a Hollow | CANON-EXPANDED | b1 ch.17; ch.17:II |
| The Leads, the Stack, the Rag Bridge of houses, the Drowned Market, the Pale Pike, the Night Market's ring and moth-lamp seller, the Sweep, gang turf, the punt ferry; the town on a hill, the frozen moments, the descent, the echoes, the flying bridge, the rewinding span, "Struck from the record" | GAME-ONLY | this doc |
| The whole rework | PROVISIONAL | until Chris picks |

---

## 3. Pick list (recommended order; answer "do 1,2,4,7")

**The Lowtowns**
1. **Three heights** (M): shelves and step-streets, sunk Drain Courts, the High Rows ridge. Fixes "flat" at the root.
2. **Colour by district** (S): amber market, green courts, blue Burners' Yard, white Gate Ward. Cheapest big change to "dull".
3. **The Leads** (L): roofs, rope bridges, drops through washing lines. The zone's signature; a second way through.
4. **New tricks for every camp** (M): blinking Weave-Eaters, slate-throwers, circling hounds, the warders' shield-line, barge fights.
5. **The Night Market and the Spoon's brawl** (M): something to do after dark, from canon's own scenes.
6. **The Stack and the Rag Bridge of houses** (M): landmarks you steer by.
7. **The Drowned Market** (L): the old vaults under the market, three ways in, a loop under the river.
8. **The Sweep** (L): get a sparked child out over the roofs; the zone's heart (needs 3).
9. **Gang turf by the day and the punt ferry** (M): the zone changes from day to day.
10. **Hidden ways and the market as hub** (S): the Spoon's cellar crawl, the Seer's alley, two main quests in either order.

**Hallow's Creek**
1. **Two roads into town** (M): the high road (Pact) or the low road (Hollow, through the Cellars). Breaks the line most cheaply.
2. **The town on a hill and the frozen blast** (M): the tower and the violet dome seen from the first step.
3. **New tricks for the fights** (M): cutters against the clock, Hollow out of doors, echoes, crossbows on the roofs, clerks that undo.
4. **Frozen moments that change the route** (M-L): four Glitches that open lanes, ramps and doors, or flood a cellar.
5. **Choose the wing first** (S): three wings open from the Square; each done takes a wave off the tower.
6. **Colour and stopped motion** (S): quartz roses, blue wheat, birds and rain hanging in the air.
7. **Bend the line and the flying bridge** (M): no more one axis; into the tower from the Archive's gallery.
8. **The descent** (L): down the chasm wall past the Fallen Mason to the low road (goes with 1).
9. **Mercy for the paused** (S): Gable's pie and the half-cut townsfolk laid to rest.
10. **The rewinding span** (L, risky): a bridge that comes and goes with the loop. Last: the hardest to make reliable.
