# Loot v1: the named item list (2026-10-01)

Companion to `DESIGN.md` in this folder. 104 new named items, plus looks for the 12 named items already in the game and the 21 pieces the Blacksmith will craft. Everything here is **GAME-ONLY** (invented for the game; places, factions and creatures named are the game's existing ones). No item is in the game yet: this is the data to type into `loot.<zone>.json`.

## How to read the tables

- **Id**: the full id is `loot.<zone>.<id>`, for example `loot.oak.whitefoot_mantle`. Ids are permanent once shipped (saves store ids only).
- **Req**: the level needed to wear it (`ItemDef.level`). The item's curve level is Req + 1, the same rule generated gear uses (a level-5 mob drops gear that needs level 4).
- **Armour / damage** and **Value** were computed with the game's own formulas (`ItemDatabase.Generate`, float maths and rounding reproduced in a script), at the curve level and quality. A named item never has more armour or damage than a generated item of the same level and quality.
- **Stats**: Sta, Str, Agi, Int, Spi. The total is the budget from `DESIGN.md` section 3.3: uncommon 4k, rare 5k, boss signature rare 6k, epic 6k (k = power / 4). Every row was checked against its budget by the script; the two luck charms deliberately spend fewer points.
- **Effect or set**: only the six effect kinds the engine can carry out (`DESIGN.md` section 3.6).
- **Source**: "signature" = the boss's own list (one piece you do not own per kill; always at the two dungeon bosses and Hesk, 50% at outdoor elites). "Zone table 3%" = one roll per kill, then one pick from that mob's 2-3 items. Quest ids and vendor names were checked against the quest and zone files.
- **Look**: `family:variant/palette`, `+glow` forces an emissive accent. Families and palettes are in `DESIGN.md` section 2.
- Intellect and Spirit do nothing in the game today, so only six items carry them (staves, a censer, a lantern, a mask and a cord).

## Counts

| Zone | Items | Uncommon | Rare | Epic |
|---|---|---|---|---|
| Oakhaven (incl. Crowsfoot Hollow) | 21 | 12 | 8 | 1 |
| Khaven | 17 | 10 | 6 | 1 |
| The Shattered Peaks | 17 | 8 | 8 | 1 |
| The Ashland Rim | 19 | 10 | 8 | 1 |
| The Verdant Shore (incl. the Root-Mother's Deep) | 24 | 9 | 13 | 2 |
| World drops | 6 | 0 | 5 | 1 |
| **Total** | **104** | **49** | **48** | **7** |

- Every zone has named gear in all nine slots.
- All 12 elites have a signature list; 6 have an epic.
- By source: 24 boss signature pieces, 5 boss rare-table pieces, 7 epics (6 boss, 1 world), 43 from ordinary mobs, 5 world rares, 15 quest rewards, 5 vendor pieces.

## Oakhaven (ids `loot.oak.*`)

| Id | Name | Slot | Req | Quality | Armour / damage | Stats | Effect or set | Source | Look | Value | Flavour |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `whitefoot_mantle` | Whitefoot's Winter Mantle | shoulders | 2 | Rare | 7 armour | +4 Sta, +4 Agi | - | Old Whitefoot: signature | `shoulder.mantle:fur/pilgrim` | 12 | The one white paw is still on it. |
| `whitefoot_fang` | The Old Dog's Tooth | neck | 2 | Rare | - | +3 Sta, +3 Str, +2 Agi | - | Old Whitefoot: signature | `neck.cord:fang/oakhaven` | 12 | Longer than a thumb, and yellow as old butter. |
| `den_mothers_wraps` | Den-Mother's Paw Wraps | hands | 2 | Uncommon | 6 armour | +2 Sta, +2 Agi | - | Old Whitefoot: rare table 15% | `hands.wraps:fur/oakhaven` | 9 | Wound from the bedding of the den. |
| `hollin_pitchfork` | Hollin's Bent Pitchfork | mainhand | 1 | Uncommon | 7 damage | +2 Sta, +2 Str | - | Wolves, Oakhaven: zone table 3% | `polearm:fork/oakhaven` | 8 | Bent on a wolf, by the look of it. |
| `mastwood_tuskguard` | Mastwood Tusk-Guard | offhand | 1 | Uncommon | 7 armour | +3 Sta, +1 Str | - | Boars, Oakhaven: zone table 3% | `shield.buckler:tusk/oakhaven` | 6 | Two tusks and a barrel lid. It works. |
| `brookside_waders` | Brookside Waders | feet | 1 | Uncommon | 4 armour | +2 Sta, +2 Agi | - | Boars, Oakhaven: zone table 3% | `feet.boots:waders/oakhaven` | 6 | Greased against the brook. |
| `greycoat_tabard` | Collector's Turned Coat | chest | 1 | Uncommon | 6 armour | +3 Sta, +1 Str | - | Quest main.oakhaven.3 (The Ruts Go West) | `chest.coat:grey/concord` | 6 | Grey outside. Someone has stitched oak leaves into the lining. |
| `brannocs_good_iron` | Brannoc's Good Iron | mainhand | 1 | Uncommon | 7 damage | +1 Sta, +3 Str | - | Quest npc.brannoc.iron (Good Iron) | `sword.short:plain/oakhaven` | 8 | He made it for you, so he sharpened it twice. |
| `saltmenders_cord` | Salt-Mender's Knotted Cord | neck | 1 | Uncommon | - | +2 Sta, +2 Spi | - | Quest faction.saltmenders.1 (Salt in the Tithe) | `neck.cord:knot/pilgrim` | 6 | Nine knots, one for each thing mended. |
| `ivels_leaf_cap` | Ivel's Leaf-Stitched Cap | head | 1 | Uncommon | 4 armour | +2 Sta, +2 Agi | - | Quest faction.preservationists.1 (Roots Before Ruin) | `head.cap:leaf/veridian` | 6 | Roots before ruin, sewn inside the brim. |
| `market_day_breeches` | Market-Day Breeches | legs | 1 | Uncommon | 6 armour | +2 Sta, +2 Agi | - | Vendor: Ama Rusk | `legs.breeches:plain/oakhaven` | 6 | Kept for best, and for the road. |
| `deserters_falchion` | Company-Issue Falchion | mainhand | 3 | Uncommon | 12 damage | +2 Sta, +3 Str | - | Deserters, Crowsfoot Hollow: zone table 3% | `sword.falchion:clipped/sandthrone` | 17 | The company mark has been filed off. |
| `sandthrone_halfhelm` | Sand-Scoured Half-Helm | head | 3 | Uncommon | 8 armour | +3 Sta, +2 Str | - | Deserters, Crowsfoot Hollow: zone table 3% | `head.kettle:half/sandthrone` | 12 | Polished by a desert that is a long way from here. |
| `lookouts_breeches` | Lookout's Patched Breeches | legs | 3 | Uncommon | 12 armour | +2 Sta, +3 Agi | - | Deserters, Crowsfoot Hollow: zone table 3% | `legs.breeches:patched/sandthrone` | 12 | Worn thin at the knees from watching the road. |
| `hesks_counting_gloves` | Hesk's Counting Gloves | hands | 3 | Rare | 10 armour | +4 Sta, +3 Str, +3 Agi | - | Quartermaster Hesk: signature | `hands.gloves:fingerless/sandthrone` | 16 | Fingertips cut away for the coins. |
| `strongbox_lid` | The Quartermaster's Strongbox Lid | offhand | 3 | Rare | 18 armour | +6 Sta, +4 Str | - | Quartermaster Hesk: signature | `shield.round:lid/sandthrone` | 16 | Iron-bound, and nothing under it any more. |
| `due_cleaver` | Caddock's Notched Cleaver | mainhand | 4 | Rare | 17 damage | +5 Sta, +8 Str | Set: The Deserter King's Due | Caddock, the Bandit King: signature | `cleaver:notched/sandthrone` | 28 | A notch for every man who called him captain. |
| `due_coat` | The King's Stolen Coat | chest | 4 | Rare | 19 armour | +8 Sta, +5 Str | Set: The Deserter King's Due | Caddock, the Bandit King: signature | `chest.coat:skirted/sandthrone` | 20 | Cut for a bigger man with a better name. |
| `due_boots` | Boots of the Deep Stair | feet | 4 | Rare | 12 armour | +5 Sta, +6 Agi | Set: The Deserter King's Due | Deserters level 4-5 (King's guard, Store Caves, Deep Stair): 4% | `feet.boots:plain/sandthrone` | 20 | Quiet on stone. They had to be. |
| `broken_oath_sabre` | Broken-Oath Sabre | mainhand | 4 | Epic | 20 damage | +7 Sta, +9 Str | Each kill restores 12 health | Caddock, the Bandit King: epic 10%, certain by the 10th dry kill | `sword.sabre:broken/sandthrone+glow` | 35 | Snapped across a knee and forged whole again by worse hands. |
| `harrow_luck_knot` | Harrow Wood Luck-Knot | neck | 2 | Rare | - | +2 Sta, +2 Agi | +10% luck on lucky drops | Any Oakhaven camp mob: 0.4% | `neck.cord:knot/oakhaven` | 12 | Tied by a child, lost by a poacher, found by you. |

## Khaven (ids `loot.kha.*`)

| Id | Name | Slot | Req | Quality | Armour / damage | Stats | Effect or set | Source | Look | Value | Flavour |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `outrider_bearded_axe` | Outrider's Bearded Axe | mainhand | 4 | Uncommon | 14 damage | +3 Sta, +4 Str | - | Outriders, Khaven: zone table 3% | `axe.bearded:plain/sandthrone` | 21 | Hooks a shield as well as it splits one. |
| `dune_scarf` | Dune-Red Head Scarf | head | 4 | Uncommon | 9 armour | +3 Sta, +4 Agi | - | Outriders, Khaven: zone table 3% | `head.wrap:scarf/sandthrone` | 15 | Still smells of a hotter country. |
| `picket_shield` | Picket's Hide Shield | offhand | 4 | Uncommon | 18 armour | +5 Sta, +2 Str | - | Outriders, Khaven: zone table 3% | `shield.round:hide/sandthrone` | 15 | Stretched hide, drummed tight. |
| `gloom_creek_waders` | Gloom Creek Waders | feet | 4 | Uncommon | 9 armour | +4 Sta, +3 Agi | - | Hollow Men, Khaven: zone table 3% | `feet.boots:waders/khaven` | 15 | The creek gave them back. It does not give much back. |
| `petrified_knuckle` | Petrified Knuckle | mainhand | 4 | Uncommon | 14 damage | +5 Sta, +2 Str | - | Hollow Men, Khaven: zone table 3% | `mace.flanged:fist/khaven` | 21 | It was a hand once. |
| `carrion_hide_leggings` | Carrion-Hide Leggings | legs | 3 | Uncommon | 12 armour | +3 Sta, +2 Agi | - | Boars, Khaven: zone table 3% | `legs.leggings:hide/khaven` | 12 | Cured twice, and it needed it. |
| `hush_pelt` | Hush-Wolf Pelt | shoulders | 4 | Uncommon | 9 armour | +3 Sta, +4 Agi | - | Wolves, Khaven: zone table 3% | `shoulder.mantle:fur/khaven` | 15 | The wolves of the Hush make no sound. Neither does this. |
| `sextons_spade` | The Sexton's Spade | mainhand | 4 | Rare | 17 damage | +7 Sta, +6 Str | - | The Grey Sexton: signature | `polearm:spade/khaven` | 28 | He dug every grave in Khaven, and then his own. |
| `shawl_of_the_uncounted` | Shawl of the Uncounted | shoulders | 4 | Rare | 12 armour | +8 Sta, +5 Str | - | The Grey Sexton: signature | `shoulder.mantle:shawl/khaven` | 20 | One thread for each name he never wrote down. |
| `mourners_iron_band` | Mourner's Iron Band | neck | 4 | Rare | - | +6 Sta, +5 Str | - | The Grey Sexton: rare table 15% | `neck.torc:band/khaven` | 20 | Worn until the grief is done, so it is never taken off. |
| `pane_of_the_final_sum` | Pane of the Final Sum | offhand | 6 | Rare | 31 armour | +9 Sta, +9 Str | - | The Pale Reckoner: signature | `shield.heater:glass/pale+glow` | 28 | Cold glass. Your reflection is a little behind you. |
| `cold_count_gauntlets` | Gauntlets of the Cold Count | hands | 6 | Rare | 17 armour | +9 Sta, +9 Str | - | The Pale Reckoner: signature | `hands.gauntlets:glass/pale` | 28 | The fingers keep count on their own. |
| `the_unpaid_debt` | The Unpaid Debt | neck | 6 | Epic | - | +10 Sta, +8 Str, +4 Agi | +5% maximum health | The Pale Reckoner: epic 4%, certain by the 25th dry kill | `neck.pendant:glass/pale+glow` | 35 | It was owed to the Reckoner. Now it is owed to you. |
| `wood_edge_jerkin` | Wenna's Wood-Edge Jerkin | chest | 3 | Uncommon | 12 armour | +3 Sta, +2 Agi | - | Quest npc.wenna.wolves (Grey at the Wood's Edge) | `chest.jerkin:leather/khaven` | 12 | Her brother's. He does not go to the wood any more. |
| `hold_breakers_maul` | Hold-Breaker's Maul | mainhand | 3 | Uncommon | 12 damage | +2 Sta, +3 Str | - | Quest main.khaven.1 (Break the Sandthrone Hold) | `hammer.war:maul/khaven` | 17 | For doors, mostly. |
| `morrows_road_greaves` | Morrow's Road Greaves | legs | 4 | Rare | 19 armour | +6 Sta, +5 Str | - | Quest main.khaven.3 (The Toll Road North) | `legs.greaves:plate/khaven` | 20 | Ansel walked north in these once. He came back. |
| `tallow_proofed_capelet` | Cato's Tallow-Proofed Capelet | shoulders | 4 | Uncommon | 9 armour | +4 Sta, +3 Agi | - | Vendor: Cato Brisk | `shoulder.mantle:cloth/khaven` | 15 | Sheds rain, and smells of candles. |

## The Shattered Peaks (ids `loot.pea.*`)

| Id | Name | Slot | Req | Quality | Armour / damage | Stats | Effect or set | Source | Look | Value | Flavour |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `tollkeepers_billhook` | Toll-Keeper's Billhook | mainhand | 6 | Uncommon | 18 damage | +4 Sta, +5 Str | - | Toll-guards: zone table 3% | `polearm:billhook/sandthrone` | 29 | Lowered across the road ten thousand times. |
| `toll_gate_kettle` | Toll-Gate Kettle Helm | head | 6 | Uncommon | 13 armour | +6 Sta, +3 Str | - | Toll-guards: zone table 3% | `head.kettle:plain/tollroad` | 21 | The brim keeps the sleet out of your eyes. |
| `signal_tower_mitts` | Signal-Tower Mitts | hands | 6 | Uncommon | 13 armour | +4 Sta, +5 Agi | - | Sandthrone pickets, Peaks: zone table 3% | `hands.gloves:mitts/tollroad` | 21 | Singed from the beacon. |
| `mule_track_hobnails` | Mule-Track Hobnails | feet | 6 | Uncommon | 13 armour | +5 Sta, +4 Agi | - | Wolves, Peaks: zone table 3% | `feet.boots:hobnail/tollroad` | 21 | They grip where a mule would think twice. |
| `shieling_crook` | Shieling Crook | mainhand | 6 | Uncommon | 18 damage | +5 Sta, +2 Int, +2 Spi | - | Wolves, Peaks: zone table 3% | `staff:crook/pilgrim` | 29 | The flock is gone. The crook stayed. |
| `rockhide_jerkin` | Rockhide Jerkin | chest | 7 | Uncommon | 24 armour | +7 Sta, +4 Str | - | Boars, Peaks: zone table 3% | `chest.jerkin:hide/tollroad` | 24 | Tanning it broke two knives. |
| `watchers_cold_lens` | Watcher's Cold Lens | neck | 7 | Rare | - | +7 Sta, +4 Str, +6 Agi | - | Pale watchers, the High Ledge: 2% | `neck.pendant:glass/pale+glow` | 32 | Look through it and the ledge looks back. |
| `cold_glass_spaulders` | Spaulders of Cold Glass | shoulders | 7 | Rare | 19 armour | +9 Sta, +8 Str | - | Pale watchers, the High Ledge: 2% | `shoulder.spaulder:glass/pale` | 32 | They never warm, whatever the weather. |
| `the_captains_toll` | The Captain's Toll | mainhand | 7 | Rare | 25 damage | +8 Sta, +12 Str | - | Sandthrone captain: signature | `sword.sabre:officer/sandthrone` | 45 | What the road pays when it cannot pay in coin. |
| `eyrie_cuirass` | Eyrie Cuirass | chest | 7 | Rare | 30 armour | +12 Sta, +8 Str | - | Sandthrone captain: signature | `chest.cuirass:plate/sandthrone` | 32 | Dented on the left, where the wind throws stones. |
| `signet_of_the_toll_road` | Signet of the Toll Road | neck | 7 | Rare | - | +8 Sta, +9 Str | +15% coins from bodies | Sandthrone captain: rare table 15% | `neck.pendant:signet/sandthrone` | 32 | Press it in wax and people hand over their purses. |
| `scree_tusk` | Scree-Tusk's Broken Tusk | mainhand | 7 | Rare | 25 damage | +12 Sta, +8 Str | - | Old Scree-Tusk: signature | `club:tusk/tollroad` | 45 | It broke on the mountain. The mountain lost a piece too. |
| `scree_hide_legguards` | Scree-Hide Legguards | legs | 7 | Rare | 30 armour | +11 Sta, +9 Agi | - | Old Scree-Tusk: signature | `legs.leggings:hide/tollroad` | 32 | Grey as the slope he slept on. |
| `rockfall` | Rockfall | offhand | 7 | Epic | 44 armour | +15 Sta, +10 Str | +10% armour | Old Scree-Tusk: epic 4%, certain by the 25th dry kill | `shield.kite:slab/tollroad+glow` | 40 | A slab of slate with a strap. It has already fallen on someone. |
| `seekers_sealed_pendant` | Seeker's Sealed Pendant | neck | 6 | Uncommon | - | +5 Sta, +4 Str | - | Quest main.peaks.2 (Under a Seeker's Seal) | `neck.pendant:seal/pilgrim` | 21 | The wax is unbroken. Leave it that way. |
| `salt_rimed_helm` | Tarsk's Salt-Rimed Helm | head | 7 | Rare | 19 armour | +10 Sta, +7 Str | - | Quest main.peaks.3 (Salt of the First Sea) | `head.barbute:rimed/tollroad` | 32 | White at the edges, like a tide-line. |
| `quells_climbing_gloves` | Yara's Climbing Gloves | hands | 7 | Uncommon | 15 armour | +5 Sta, +6 Agi | - | Vendor: Yara Quell | `hands.gloves:plain/tollroad` | 24 | Palms of goat-hide. She does not sell the bad pairs. |

## The Ashland Rim (ids `loot.ash.*`)

| Id | Name | Slot | Req | Quality | Armour / damage | Stats | Effect or set | Source | Look | Value | Flavour |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `tear_marked_mask` | Tear-Marked Bone Mask | head | 9 | Uncommon | 19 armour | +7 Sta, +4 Str, +3 Spi | - | Cultists: zone table 3% | `head.mask:tear/cult` | 30 | One painted tear. They all weep the same. |
| `skull_knob_staff` | Skull-Knob Staff | mainhand | 9 | Uncommon | 25 damage | +5 Sta, +6 Int, +3 Spi | - | Cultists: zone table 3% | `staff:skull/cult` | 42 | It is not a large skull. Do not ask. |
| `cinder_sewn_vestment` | Cinder-Sewn Vestment | chest | 9 | Uncommon | 30 armour | +8 Sta, +6 Str | - | Cultists: zone table 3% | `chest.robe:vestment/cult` | 30 | The hem is always warm. |
| `ash_hound_wraps` | Ash-Hound Wraps | hands | 8 | Uncommon | 17 armour | +5 Sta, +7 Agi | - | Ash hounds: zone table 3% | `hands.wraps:fur/ashwalker` | 27 | Grey fur that leaves grey on everything. |
| `harpoon_line_boots` | Harpoon-Line Boots | feet | 8 | Uncommon | 17 armour | +7 Sta, +5 Agi | - | Ash hounds: zone table 3% | `feet.boots:plain/ashwalker` | 27 | Soled with old line, for decks that are long gone. |
| `static_glass_shiv` | Static-Glass Shiv | mainhand | 9 | Uncommon | 25 damage | +4 Sta, +10 Agi | - | Weave-Eaters: zone table 3% | `knife:glass/pale+glow` | 42 | It hums against your teeth. |
| `unwoven_cord` | Cord of Unwoven Thread | neck | 9 | Uncommon | - | +6 Sta, +4 Str, +4 Agi | - | Weave-Eaters: zone table 3% | `neck.cord:beads/ashwalker` | 30 | Thread the eaters pulled out of something. |
| `cinderfold_greaves` | Cinderfold Greaves | legs | 9 | Uncommon | 30 armour | +9 Sta, +5 Str | - | Hollow Men, Cinderfold: 4% | `legs.greaves:plate/ashwalker` | 30 | Somebody marched a long way in these before they stopped. |
| `brood_glass_ward` | Brood-Glass Ward | offhand | 9 | Rare | 44 armour | +16 Sta, +10 Str | - | Brood Weave-Eater: signature | `shield.heater:glass/ashwalker+glow` | 40 | Grown, not made. |
| `threadcutter` | Threadcutter | mainhand | 9 | Rare | 30 damage | +10 Sta, +8 Str, +8 Agi | - | Brood Weave-Eater: signature | `knife:sickle/ashwalker+glow` | 56 | It parts cloth, rope and anything else that holds together. |
| `weave_eaten_mantle` | Weave-Eaten Mantle | shoulders | 9 | Rare | 24 armour | +11 Sta, +10 Agi | - | Brood Weave-Eater: rare table 15% | `shoulder.mantle:frayed/ashwalker` | 40 | More hole than mantle, and stronger for it. |
| `the_deacons_censer` | The Deacon's Censer | offhand | 9 | Rare | 44 armour | +10 Sta, +10 Int, +6 Spi | - | The Ash-Deacon: signature | `offhand.hung:censer/cult+glow` | 40 | It burns something that was not wood. |
| `cassock_of_the_last_sermon` | Cassock of the Last Sermon | chest | 9 | Rare | 37 armour | +15 Sta, +11 Str | - | The Ash-Deacon: signature | `chest.robe:cassock/cult` | 40 | He promised them an ending. He was the only one who got it. |
| `deacons_ash_treads` | Ash-Pit Treads | feet | 9 | Rare | 24 armour | +11 Sta, +10 Agi | - | The Ash-Deacon: rare table 15% | `feet.boots:plain/cult` | 40 | They leave no prints in ash. |
| `ember_that_remembers` | The Ember That Remembers | neck | 9 | Epic | - | +14 Sta, +12 Str, +6 Agi | +10 health per rest tick out of combat | The Ash-Deacon: epic 4%, certain by the 25th dry kill | `neck.pendant:ember/ashwalker+glow` | 50 | It was a hearth once, in a house, in a town. |
| `line_hauler_gauntlets` | Grohl's Line-Hauler Gauntlets | hands | 8 | Uncommon | 17 armour | +6 Sta, +6 Str | - | Quest npc.grohl.hounds (Hounds on the Harpoon-Line) | `hands.gauntlets:bone/ashwalker` | 27 | For hauling in something that does not want to come. |
| `oskas_bone_kilt` | Oska's Bone-Plate Kilt | legs | 8 | Rare | 34 armour | +10 Sta, +9 Str | - | Quest npc.oska.bone (Leviathan Bone) | `legs.kilt:bone/ashwalker` | 36 | Every plate carved from the same rib. |
| `old_salt_road_pauldrons` | Pauldrons of the Old Salt Road | shoulders | 9 | Rare | 24 armour | +12 Sta, +9 Str | - | Quest main.ashrim.5 (The Old Salt Road) | `shoulder.pauldron:bone/ashwalker` | 40 | The Ash-Walkers give these to people who are leaving. |
| `cask_head_shield` | Sefa's Cask-Head Shield | offhand | 9 | Uncommon | 35 armour | +9 Sta, +5 Str | - | Vendor: Sefa Brine | `shield.round:cask/ashwalker` | 30 | The end of a salt cask, still stamped. |

## The Verdant Shore (ids `loot.ver.*`)

| Id | Name | Slot | Req | Quality | Armour / damage | Stats | Effect or set | Source | Look | Value | Flavour |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `reed_wader_boots` | Reed-Wader Boots | feet | 10 | Uncommon | 21 armour | +8 Sta, +7 Agi | - | Moss-backed boars: zone table 3% | `feet.boots:waders/veridian` | 33 | Woven so the mere lets go of them. |
| `velvet_tine_club` | Velvet-Tine Club | mainhand | 11 | Uncommon | 29 damage | +7 Sta, +9 Str | - | Stags: zone table 3% | `mace.root:antler/veridian` | 50 | Still in velvet. It will not be for long. |
| `dappled_hide_mantle` | Dappled Hide Mantle | shoulders | 11 | Uncommon | 23 armour | +8 Sta, +8 Agi | - | Stags: zone table 3% | `shoulder.mantle:hide/veridian` | 36 | Stand still in it and the meadow forgets you. |
| `canopy_silk_wraps` | Canopy-Silk Wraps | hands | 11 | Uncommon | 23 armour | +6 Sta, +10 Agi | - | Canopy spiders: zone table 3% | `hands.wraps:silk/veridian` | 36 | Stronger than rope and lighter than breath. |
| `chitin_buckler` | Chitin Buckler | offhand | 11 | Uncommon | 42 armour | +11 Sta, +5 Agi | - | Canopy spiders: zone table 3% | `shield.buckler:chitin/veridian` | 36 | One plate off something with too many legs. |
| `briar_wound_circlet` | Briar-Wound Circlet | head | 12 | Uncommon | 25 armour | +9 Sta, +9 Str | - | Creeping briars: zone table 3% | `head.circlet:briar/veridian` | 39 | The thorns point outward. Mostly. |
| `heart_of_briar_maul` | Heart-of-Briar Maul | mainhand | 12 | Rare | 38 damage | +10 Sta, +10 Str, +8 Agi | - | Creeping briars: 1.5% | `mace.root:briar/veridian` | 73 | It still tries to root, if you set it down. |
| `grey_bark_breastplate` | Grey-Bark Breastplate | chest | 12 | Uncommon | 39 armour | +11 Sta, +7 Str | - | Withered Keepers, the Greying: zone table 3% | `chest.cuirass:bark/pale` | 39 | Bark with the green gone out of it. |
| `shroud_of_a_forgotten_name` | Shroud of a Forgotten Name | chest | 12 | Rare | 49 armour | +16 Sta, +12 Agi | - | Mist-walkers, Palemist: 2% | `chest.robe:shroud/pale` | 52 | There was a name sewn in the collar. It has been picked out. |
| `sliver_of_the_pale` | Sliver of the Pale | mainhand | 12 | Rare | 38 damage | +8 Sta, +20 Agi | - | Pale shadows: 4% | `knife:glass/pale+glow` | 73 | A splinter of a shape that should not have had edges. |
| `greyhearts_withered_boughs` | Greyheart's Withered Boughs | shoulders | 12 | Rare | 31 armour | +18 Sta, +15 Str | - | Greyheart: signature | `shoulder.spaulder:bark/pale` | 52 | They held up a sky of leaves once. |
| `ward_of_living_heartwood` | Ward of Living Heartwood | offhand | 12 | Rare | 57 armour | +20 Sta, +13 Str | - | Greyheart: signature | `shield.leaf:bark/veridian` | 52 | The one part of him the cold did not reach. |
| `the_last_green_leaf` | The Last Green Leaf | neck | 12 | Epic | - | +18 Sta, +12 Str, +11 Agi | +20 maximum resource | Greyheart: epic 4%, certain by the 25th dry kill | `neck.pendant:leaf/veridian+glow` | 65 | Greyheart kept one. He could not remember why. |
| `crown_of_nine_branches` | Crown of Nine Branches | head | 12 | Rare | 31 armour | +17 Sta, +16 Agi | - | Old Ninebranch: signature | `head.crown:antler/veridian` | 52 | Nine points, and moss on every one. |
| `the_old_kings_hide` | The Old King's Hide | legs | 12 | Rare | 49 armour | +18 Sta, +15 Agi | - | Old Ninebranch: signature | `legs.leggings:hide/veridian` | 52 | Scarred by every hunter who was not good enough. |
| `vigil_bark_plate` | Vigil Bark-Plate | chest | 12 | Rare | 49 armour | +19 Sta, +14 Str | Set: Vigil of the Root-Mother | The Hollow Root-Warden: signature | `chest.cuirass:bark/veridian` | 52 | Grown around its wearer over a very long watch. |
| `vigil_root_grips` | Vigil Root-Grips | hands | 12 | Rare | 31 armour | +15 Sta, +13 Str | Set: Vigil of the Root-Mother | Withered Keepers in the Deep (Gallery, the Heart): 5% | `hands.gauntlets:root/veridian` | 52 | They close when you close your hand, and a moment after. |
| `vigil_sap_treads` | Vigil Sap-Treads | feet | 12 | Rare | 31 armour | +15 Sta, +13 Agi | Set: Vigil of the Root-Mother | Mist-walkers in the Deep (Sap Well): 5% | `feet.boots:root/veridian` | 52 | Amber to the ankle. |
| `the_wardens_root_maul` | The Warden's Root-Maul | mainhand | 12 | Rare | 38 damage | +16 Sta, +17 Str | - | The Hollow Root-Warden: signature | `mace.root:burl/veridian` | 73 | A root-ball the size of a child, on a haft of ghost-oak. |
| `the_root_mothers_patience` | The Root-Mother's Patience | mainhand | 12 | Epic | 47 damage | +20 Sta, +21 Str | Each kill restores 30 health | The Hollow Root-Warden: epic 10%, certain by the 10th dry kill | `sword.great:living/veridian+glow` | 91 | She has waited longer than the Concord has had a name. |
| `leggings_the_wood_learned` | Leggings the Wood Learned | legs | 10 | Uncommon | 33 armour | +9 Sta, +6 Agi | - | Quest main.verdant.1 (Let the Wood Learn You) | `legs.leggings:garter/veridian` | 33 | The brambles part for them now. |
| `oak_banes_carved_torc` | Oak-Bane's Carved Torc | neck | 12 | Rare | - | +12 Sta, +9 Str, +7 Agi | - | Quest npc.oakbane.ninebranch (The Old King of the Deep Wood) | `neck.torc:wood/veridian` | 52 | Carved the night the Old King fell. |
| `lantern_of_the_deeps_quiet` | Lantern of the Deep's Quiet | offhand | 12 | Rare | 57 armour | +14 Sta, +8 Int, +6 Spi | - | Quest main.verdant.5 (The Root-Mother's Deep) | `offhand.hung:moss/veridian+glow` | 52 | Lantern-moss under glass. It dims when something is listening. |
| `sap_sealed_boots` | Ondine's Sap-Sealed Boots | feet | 11 | Uncommon | 23 armour | +8 Sta, +8 Agi | - | Vendor: Ondine Varro | `feet.boots:plain/veridian` | 36 | Green gold, she calls it, and charges accordingly. |

## World drops (ids `loot.world.*`)

| Id | Name | Slot | Req | Quality | Armour / damage | Stats | Effect or set | Source | Look | Value | Flavour |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `golden_cask_tankard` | The Golden Cask's Lost Tankard | mainhand | 2 | Rare | 11 damage | +4 Sta, +2 Str | Each kill restores 5 resource | World drop, camp mobs level 1-5 | `club:tankard/oakhaven` | 17 | Pewter, dented, and owed back at the bar. |
| `assessors_honest_scale` | Assessor's Honest Scale | offhand | 4 | Rare | 22 armour | +6 Sta, +5 Str | +20% coins from bodies | World drop, camp mobs level 3-7 | `offhand.hung:scale/concord` | 20 | The only honest thing a grey-coat ever carried. |
| `pilgrims_last_mile` | Pilgrim's Last Mile | feet | 7 | Rare | 19 armour | +9 Sta, +8 Agi | +6 health per rest tick out of combat | World drop, camp mobs level 6-10 | `feet.boots:plain/pilgrim` | 32 | Whoever wore them got where they were going. |
| `hares_foot_torc` | Hare's-Foot Torc | neck | 9 | Rare | - | +8 Sta, +8 Agi | +15% luck on lucky drops | World drop, camp mobs level 8-13 | `neck.torc:charm/oakhaven` | 40 | Lucky for you. Less so for the hare. |
| `helm_with_no_makers_mark` | Helm With No Maker's Mark | head | 11 | Rare | 29 armour | +14 Sta, +12 Str | - | World drop, camp mobs level 11-13 | `head.barbute:plain/tollroad` | 48 | Every smith who sees it says it is not theirs. |
| `vial_of_the_first_sea` | Vial of the First Sea | neck | 12 | Epic | - | +16 Sta, +13 Str, +12 Agi | +8% maximum health | World drop (epic), camp mobs level 11-13 | `neck.pendant:vial/pale+glow` | 65 | Water from before there was a shore to name it. |

## Sets

| Set | Where | Pieces | Bonuses |
|---|---|---|---|
| `set.crowsfoot` The Deserter King's Due | Crowsfoot Hollow (Oakhaven) | Caddock's Notched Cleaver, The King's Stolen Coat, Boots of the Deep Stair, Caddock's Tin Crown (existing `item.tin_crown`) | 2 worn: +30 health. 3 worn: +6 attack power. |
| `set.vigil` Vigil of the Root-Mother | The Root-Mother's Deep (Verdant) | Vigil Bark-Plate, Vigil Root-Grips, Vigil Sap-Treads, The Root-Warden's Crown (existing `item.wardens_crown`) | 2 worn: +60 health. 3 worn: +12 attack power. 4 worn: each kill restores 25 health. |

## Boss drop lists at a glance

Respawn times are from the zone files. "Existing" marks an item already in the game whose 100% drop becomes part of the signature list.

| Boss (level, respawn) | Signature list | Rare table (15%) | Epic |
|---|---|---|---|
| Old Whitefoot (3, 4 min) | Whitefoot's Winter Mantle, The Old Dog's Tooth | Den-Mother's Paw Wraps | - |
| Quartermaster Hesk (4, 15 min) | Hesk's Counting Gloves, The Quartermaster's Strongbox Lid | - | - |
| Caddock, the Bandit King (5, 15 min) | Caddock's Tin Crown (existing), Caddock's Notched Cleaver, The King's Stolen Coat | - | Broken-Oath Sabre (10%) |
| The Grey Sexton (5, 4 min) | The Sexton's Spade, Shawl of the Uncounted | Mourner's Iron Band | - |
| The Pale Reckoner (7, 5 min) | Pane of the Final Sum, Gauntlets of the Cold Count | - | The Unpaid Debt (4%) |
| Sandthrone captain (8, 3 min) | The Captain's Toll, Eyrie Cuirass | Signet of the Toll Road | - |
| Old Scree-Tusk (8, 3 min) | Scree-Tusk's Broken Tusk, Scree-Hide Legguards | - | Rockfall (4%) |
| Brood Weave-Eater (10, 3 min) | Brood-Glass Ward, Threadcutter | Weave-Eaten Mantle | - |
| The Ash-Deacon (10, 4 min) | The Deacon's Censer, Cassock of the Last Sermon | Ash-Pit Treads | The Ember That Remembers (4%) |
| Greyheart (13, 4 min) | Greyheart's Salt-Wood Stave (existing), Greyheart's Withered Boughs, Ward of Living Heartwood | - | The Last Green Leaf (4%) |
| Old Ninebranch (13, 4 min) | Tine of the Old King (existing), Crown of Nine Branches, The Old King's Hide | - | - |
| The Hollow Root-Warden (13, 15 min) | The Root-Warden's Crown (existing), Vigil Bark-Plate, The Warden's Root-Maul | - | The Root-Mother's Patience (10%) |

## Looks for the 12 named items already in the game

Stats and sources are unchanged. These rows go in `loot.world.json` as `gear` entries with `"legacy": true` (look and source only; the curve and budget tests skip them).

| Item id | Name | Look |
|---|---|---|
| `item.training_blade` | Tempered Trailblade | `sword.arming:straight/oakhaven` |
| `item.poachers_hood` | Poacher's Oilskin Hood | `head.hood:oilskin/oakhaven` |
| `item.hesks_lantern` | Hesk's Shuttered Lantern | `offhand.hung:shuttered/sandthrone+glow` |
| `item.tin_crown` | Caddock's Tin Crown | `head.crown:tin/sandthrone` |
| `item.outriders_knife` | Outrider's Hooked Knife | `knife:hooked/sandthrone` |
| `item.echo_jar` | Silent Pilgrim's Echo-Jar | `neck.pendant:jar/pilgrim+glow` |
| `item.bone_harpoon` | Leviathan-Bone Harpoon | `polearm:harpoon/ashwalker` |
| `item.leviathan_charm` | Leviathan-Tooth Charm | `neck.cord:tooth/ashwalker` |
| `item.tappers_gloves` | Sap-Tapper's Gloves | `hands.gloves:plain/veridian` |
| `item.ninebranch_tine` | Tine of the Old King | `neck.cord:tine/veridian` |
| `item.greyheart_stave` | Greyheart's Salt-Wood Stave | `staff:forked/pale+glow` |
| `item.wardens_crown` | The Root-Warden's Crown | `head.crown:root/veridian+glow` |

## Looks for the Blacksmith's 21 pieces (professions `DESIGN.md` section 5.2)

The ids belong to the professions work; only the looks are defined here, in `Resources/Gear/looks.json`, so the pieces show correctly the day they exist. Each metal tier has a tint that replaces the palette's metal colour: copper `#B87345`, bog-iron `#4F4A46`, ridge-steel `#A9B0BA`, ash-steel `#3E3B3D` (with an ember seam), Veridian bronze-green `#7F8F52`, heartwood `#6B3A22`.

| Item id | Look |
|---|---|
| `craft.copper_cudgel` | `club:studded/oakhaven` |
| `craft.copper_buckler` | `shield.buckler:plain/oakhaven` |
| `craft.copper_gauntlets` | `hands.gauntlets:plate/oakhaven` |
| `craft.copper_jerkin` | `chest.jerkin:scale/oakhaven` |
| `craft.bogiron_hatchet` | `axe.hand:hatchet/khaven` |
| `craft.bogiron_helm` | `head.kettle:plain/khaven` |
| `craft.bogiron_greaves` | `legs.greaves:plate/khaven` |
| `craft.bogiron_hauberk` | `chest.hauberk:mail/khaven` |
| `craft.ridgesteel_blade` | `sword.arming:straight/tollroad` |
| `craft.ridgesteel_shield` | `shield.heater:plain/tollroad` |
| `craft.ridgesteel_pauldrons` | `shoulder.pauldron:dome/tollroad` |
| `craft.ridgesteel_cuirass` | `chest.cuirass:plate/tollroad` |
| `craft.ashsteel_cleaver` | `cleaver:slab/ashwalker` |
| `craft.ashsteel_helm` | `head.barbute:plain/ashwalker` |
| `craft.ashsteel_sabatons` | `feet.sabatons:plate/ashwalker` |
| `craft.ashsteel_hauberk` | `chest.hauberk:mail/ashwalker` |
| `craft.veridian_warblade` | `sword.great:steel/veridian` |
| `craft.veridian_shield` | `shield.kite:plain/veridian` |
| `craft.veridian_legplates` | `legs.greaves:full/veridian` |
| `craft.veridian_breastplate` | `chest.cuirass:plate/veridian` |
| `craft.heartwood_greatblade` | `sword.great:wood/veridian+glow` |

## Where crafted gear sits against this list

| Level band | Vendor common weapon | Crafted weapon | Named uncommon | Named rare | Epic |
|---|---|---|---|---|---|
| 1-2 | 6 | 7 | 7 | - | - |
| 3-5 | 11 | 14 | 12-14 | 14-17 | 20 |
| 6-8 | 16 | 20 | 18-20 | 22-25 | no epic weapon (Rockfall: 44 armour) |
| 9-10 | 19 | 25 | 22-25 | 27-30 | - |
| 11-13 | 24 | 31, capstone 36 | 27-31 | 33-38 | 47 |

The professions rule holds: crafted pieces beat the vendor's commons, match a named or lucky uncommon in armour and damage (with fewer stat points: one stat of about k against 4k), and stay under the rares. The capstone greatblade (36) stays under every level-13 rare weapon (38). Epics sit above everything, which is new and intended.

## Not verified

- The numbers were computed outside Unity with the same float formulas and round-half-even; the curve test compares each item with `ItemDatabase.Get(GearId(slot, curveLevel, quality, 0))` in the editor, so any one-point difference shows up there.
- Mob tuning against a character in a full set of boss rares (about 20% more stat points than the best random rares) was not re-checked.
