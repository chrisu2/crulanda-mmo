# Loot: worn looks, a loot database and loot that feels like loot (design, 2026-10-01)

Owner's request: "time to improve armor and weapons and each has a different visual appearance when worn. so we need to start building a database of loot and start catering to the people who love loot"

This merges three panel designs (appearance, database, thrill) into one plan. The item list is in `ITEMS_V1.md` beside this file. Nothing was run (no Unity, builds or tests) and nothing in the repo was changed; the claims the plan rests on were checked by reading the code at 078d276 (section 11 lists what was checked and what was not).

## Decisions needed from Chris (defaults in bold; details in section 10)

1. **Empty slots show empty** (no sword until one is equipped, no shoulder pads). Yes?
2. **Cloaks: add a tenth "back" slot, as the last step.** Yes?
3. **Boss trophies stop dropping at 100% every kill**; each kill gives a piece from the boss's list you do not own yet. Yes?

---

## 1. What the player gets

- **Everything you wear shows on your character.** Weapons, shields and lanterns in the hands; helms, hoods and crowns; shoulders, chest, gloves, legs, boots and neck pieces. 54 shape families with 146 variants, coloured by where the item comes from (Oakhaven homespun, Concord grey, Sandthrone sand and red, Khaven gloom, toll-road steel, pilgrim cream, Ash-Walker bone and soot, Cult black, Veridian green and bronze, Pale cold glass). Better quality shows: trim on uncommon, a glowing accent on rare, two pulsing ones on epic.
- **A loot database.** 104 new named items across the five zones on top of the 12 that exist, each with its own look, a source and a line of flavour. Every one of the 12 elites has a signature list. Six bosses and the world table carry the game's first purple items. Both dungeons have a four-piece set. Fifteen quests give gear and five merchants sell one named piece each.
- **Loot that feels like loot.** A coloured beam on a body that holds something good, a loot window instead of a chat line (nothing is lost to full bags any more), green and red stat differences against what you wear, an upgrade arrow, "RARE" and "EPIC" call-outs, and an Armoury tab in the quest book that lists every named item by zone and source, with silhouettes for the ones you have not found.
- **A character sheet that shows you.** The flat rectangles become a turnable 3D figure in your gear, items get rendered icons instead of two-letter glyphs, and Ctrl+click tries a piece on.
- **The Blacksmith fits in.** The 21 pieces the professions work will craft already have looks (one metal tint per tier), and they keep their stated place: above vendor commons, level with an uncommon drop, under the rares.

---

## 2. The appearance system

### 2.1 How an item gets its look

A look is `family : variant / palette`, plus quality trim. It is computed from the item id and `ItemDef` and never saved.

| Item | How the look is found |
|---|---|
| Named (`loot.*`, `item.*`) | Explicit: the `look` string in the item's `gear` entry (section 3.2), registered with `GearLooks.Register(id, look)` when the loot database loads. |
| Crafted (`craft.*`) | Explicit: the `looks` table in `looks.json`, with a metal tint per tier. |
| Generated (`gen.<slot>.<level>.<quality>.<seed>`) | Deterministic from the id and name. Slot, level, quality and seed come from the id. The name gives two words: strip a leading "Worn " and everything from " of " on; the last word is the piece (Blade, Hauberk), the rest is the material (Tanned, Ridge-forged). `words` maps slot + piece + level band to a family; `materials` maps the material to a palette, shade and detail. `variant = (seed / 7) % family.gen`, where `gen` is how many of the family's variants generated gear may use. |
| Anything else | The slot's fallback family in the `oakhaven` palette. Nothing renders as missing. |

- Matching name words means no random numbers are replayed, so the mapping survives any change to the draw order in `ItemDatabase.Generate`. If the Materials or Pieces words in `Items.cs` change, `looks.json` changes in the same commit; a test fails otherwise.
- The same id always gives the same look. Two "Tanned Blades" with different seeds may differ in one detail (guard or pommel).
- Look string grammar: `<family>[:<variant>]/<palette>[+glow]`, for example `shield.round:hide/sandthrone` or `knife:glass/pale+glow`. `+glow` forces an emissive accent whatever the quality.

### 2.2 Data: `Assets/Crulanda/Resources/Gear/looks.json` (new)

Loaded with `Resources.Load<TextAsset>("Gear/looks")`. There is no `Resources` folder in the project today, so this adds one. It is deliberate: it needs no edit to `EncounterContent.cs` or `Encounter.asset` (both edited by the professions work), and the file must not sit in `EncounterContent/Items/`, because `ZoneSceneBuilder` registers every JSON there as an item file. It can move to an explicit `lookFiles` list later.

```json
{
  "palettes": [
    { "id": "oakhaven",   "cloth": "#8C6B47", "cloth2": "#5F7350", "leather": "#5A3F28", "metal": "#7A7770", "trim": "#9A8A68", "wood": "#6B4A2B", "glow": "#FFD98A" },
    { "id": "concord",    "cloth": "#6E737F", "cloth2": "#B8B8BC", "leather": "#34363F", "metal": "#9EA3AD", "trim": "#BF9E40", "wood": "#4D3823", "glow": "#DCE6FF" },
    { "id": "sandthrone", "cloth": "#A88552", "cloth2": "#8A3B2A", "leather": "#5C4630", "metal": "#70706F", "trim": "#B5893C", "wood": "#5A4127", "glow": "#FFB347" },
    { "id": "khaven",     "cloth": "#4C4A58", "cloth2": "#6B6470", "leather": "#3A3038", "metal": "#5B5F66", "trim": "#8F8F95", "wood": "#3F3530", "glow": "#A64DFF" },
    { "id": "tollroad",   "cloth": "#5B6B80", "cloth2": "#8E2F2A", "leather": "#4A3B2E", "metal": "#A9B0BA", "trim": "#C9CCD2", "wood": "#54402C", "glow": "#9FD0FF" },
    { "id": "pilgrim",    "cloth": "#C9C0A8", "cloth2": "#8D8672", "leather": "#6A5A44", "metal": "#8C8A84", "trim": "#D8D2BC", "wood": "#7A6244", "glow": "#FFF2C0" },
    { "id": "ashwalker",  "cloth": "#3A3634", "cloth2": "#D8CDB4", "leather": "#2A2523", "metal": "#4E4A48", "trim": "#D9CFBA", "wood": "#2E2622", "glow": "#FF6A1F" },
    { "id": "cult",       "cloth": "#231F24", "cloth2": "#DBD1B8", "leather": "#1C191B", "metal": "#3C3840", "trim": "#DBD1B8", "wood": "#2E2620", "glow": "#8C33CC" },
    { "id": "veridian",   "cloth": "#2F5A3C", "cloth2": "#7A5A2E", "leather": "#4B3A22", "metal": "#8A7A45", "trim": "#C9A548", "wood": "#5E3E22", "glow": "#E8B84A" },
    { "id": "pale",       "cloth": "#C8D4DC", "cloth2": "#8FA3B3", "leather": "#6E7C88", "metal": "#B9C6D0", "trim": "#E6EEF4", "wood": "#7C8790", "glow": "#9FE8FF" }
  ],
  "materials": [
    { "word": "Homespun",     "palette": "oakhaven",   "shade": 0, "detail": "none" },
    { "word": "Frayed",       "palette": "oakhaven",   "shade": 1, "detail": "fray" },
    { "word": "Patched",      "palette": "oakhaven",   "shade": 2, "detail": "patch" },
    { "word": "Tanned",       "palette": "khaven",     "shade": 0, "detail": "none" },
    { "word": "Riveted",      "palette": "sandthrone", "shade": 0, "detail": "rivets" },
    { "word": "Stitched",     "palette": "khaven",     "shade": 2, "detail": "stitch" },
    { "word": "Toll-road",    "palette": "tollroad",   "shade": 0, "detail": "band" },
    { "word": "Ridge-forged", "palette": "tollroad",   "shade": 1, "detail": "plate" },
    { "word": "Pilgrim's",    "palette": "pilgrim",    "shade": 0, "detail": "cord" },
    { "word": "Ash-hardened", "palette": "ashwalker",  "shade": 0, "detail": "none" },
    { "word": "Salt-cured",   "palette": "ashwalker",  "shade": 2, "detail": "bone" },
    { "word": "Emberbound",   "palette": "ashwalker",  "shade": 1, "detail": "ember" },
    { "word": "Veridian",     "palette": "veridian",   "shade": 0, "detail": "leaf" },
    { "word": "Root-bound",   "palette": "veridian",   "shade": 2, "detail": "root" },
    { "word": "Sap-steeped",  "palette": "veridian",   "shade": 1, "detail": "sap" }
  ],
  "words": [
    { "slot": "mainhand",  "word": "Blade",     "families": ["sword.short", "sword.arming", "sword.arming", "sword.falchion", "sword.leaf"] },
    { "slot": "mainhand",  "word": "Hatchet",   "families": ["axe.hand", "axe.hand", "axe.bearded", "axe.bearded", "axe.crescent"] },
    { "slot": "mainhand",  "word": "Cudgel",    "families": ["club", "club", "mace.flanged", "hammer.war", "mace.root"] },
    { "slot": "offhand",   "word": "Buckler",   "families": ["shield.buckler"] },
    { "slot": "offhand",   "word": "Shield",    "families": ["shield.round", "shield.round", "shield.heater", "shield.kite", "shield.leaf"] },
    { "slot": "offhand",   "word": "Lantern",   "families": ["offhand.hung"] },
    { "slot": "head",      "word": "Cap",       "families": ["head.cap", "head.cap", "head.kettle", "head.kettle", "head.barbute"] },
    { "slot": "head",      "word": "Hood",      "families": ["head.hood"] },
    { "slot": "head",      "word": "Coif",      "families": ["head.coif"] },
    { "slot": "shoulders", "word": "Mantle",    "families": ["shoulder.mantle"] },
    { "slot": "shoulders", "word": "Pauldrons", "families": ["shoulder.pauldron"] },
    { "slot": "shoulders", "word": "Spaulders", "families": ["shoulder.spaulder"] },
    { "slot": "chest",     "word": "Jerkin",    "families": ["chest.jerkin"] },
    { "slot": "chest",     "word": "Tunic",     "families": ["chest.tunic"] },
    { "slot": "chest",     "word": "Hauberk",   "families": ["chest.hauberk"] },
    { "slot": "hands",     "word": "Gloves",    "families": ["hands.gloves"] },
    { "slot": "hands",     "word": "Wraps",     "families": ["hands.wraps"] },
    { "slot": "hands",     "word": "Gauntlets", "families": ["hands.gauntlets"] },
    { "slot": "legs",      "word": "Breeches",  "families": ["legs.breeches"] },
    { "slot": "legs",      "word": "Leggings",  "families": ["legs.leggings"] },
    { "slot": "legs",      "word": "Greaves",   "families": ["legs.greaves"] },
    { "slot": "feet",      "word": "Boots",     "families": ["feet.boots"] },
    { "slot": "feet",      "word": "Shoes",     "families": ["feet.shoes"] },
    { "slot": "feet",      "word": "Sabatons",  "families": ["feet.sabatons"] },
    { "slot": "neck",      "word": "Pendant",   "families": ["neck.pendant"] },
    { "slot": "neck",      "word": "Cord",      "families": ["neck.cord"] },
    { "slot": "neck",      "word": "Torc",      "families": ["neck.torc"] }
  ],
  "fallbacks": [
    { "slot": "mainhand", "family": "sword.arming" }, { "slot": "offhand", "family": "shield.round" }, { "slot": "head", "family": "head.cap" },
    { "slot": "shoulders", "family": "shoulder.mantle" }, { "slot": "chest", "family": "chest.tunic" }, { "slot": "hands", "family": "hands.gloves" },
    { "slot": "legs", "family": "legs.breeches" }, { "slot": "feet", "family": "feet.shoes" }, { "slot": "neck", "family": "neck.cord" }
  ],
  "tints": [
    { "prefix": "craft.copper_",     "metal": "#B87345" }, { "prefix": "craft.bogiron_",  "metal": "#4F4A46" },
    { "prefix": "craft.ridgesteel_", "metal": "#A9B0BA" }, { "prefix": "craft.ashsteel_", "metal": "#3E3B3D" },
    { "prefix": "craft.veridian_",   "metal": "#7F8F52" }, { "prefix": "craft.heartwood_", "metal": "#6B3A22" }
  ],
  "looks": [
    { "item": "craft.copper_cudgel", "look": "club:studded/oakhaven" },
    { "item": "craft.heartwood_greatblade", "look": "sword.great:wood/veridian+glow" }
  ]
}
```

- A one-element `families` array applies to all level bands; a five-element array is indexed by band (levels 1-2, 3-5, 6-8, 9-10, 11-13). The resolver has its own copy of that function, because `ItemDatabase.BandOf` is private.
- `shade` 0, 1, 2 = the palette as is, 12% darker and cooler, 12% lighter and warmer. `detail` adds one or two small parts (rivet dots, a patch, a bone toggle, an ember seam).
- `looks` holds the 21 crafted pieces (full list in `ITEMS_V1.md`). Hex colours are first guesses, to be tuned from the wardrobe shots.

### 2.3 Families and build recipes

All gear parts hang under per-slot roots named `Gear <SlotName>`. Coordinates are in the body's local frame (feet at y = -1, +Z forward); the rig is `ActorVisual`'s: `body`, `legL`, `legR`, `armL`, `armR` pivots, torso cube, head sphere. Variants change one feature. Variants in *italics* are for named items only; generated gear uses the others. Budget: at most 12 parts per slot, 70 gear parts on a fully dressed player (37 parts today).

**Roots**
- Main hand: on `armR` at (0, -.62, 0), euler (110, 0, 0), weapon along +Y (pointing forward and 20 degrees down, like today's sword). Staves and polearms: euler (8, 0, 0), shaft through the hand.
- Off hand: on `armL` at (-.1, -.38, .05), euler (0, 0, 90), face normal +Y. Hung items (lantern, censer) hang from the hand at arm-local (0, -.70, .04).
- Stowed copies for swimming: main hand on `body` at (.26, .66, -.18), euler (0, 0, 145); off hand on `body` at (0, .3, -.24), euler (-90, 0, 0).

**Main hand (17 families, 47 variants)**

| Family | Variants | Recipe (weapon-local, +Y from the grip) |
|---|---|---|
| `sword.short` | plain, notched | Blade mesh .50 x .06; straight guard .16; round pommel |
| `sword.arming` | straight, curved, disc | Blade .78 x .055, diamond section; guard .22 (straight, down-curved or disc); wheel pommel |
| `sword.falchion` | clipped, heavy | Blade .70 widening to .10, clipped tip, single edge; S-guard (the deserter's falchion) |
| `sword.sabre` | *officer, broken* | Curved single-edge blade .80; knuckle bow (Tube). *broken*: a reforged seam across the blade in the glow colour |
| `sword.leaf` | bronze, root | Leaf blade .74, widest .09 at 60%; guard grown as two root curls (Tube) |
| `sword.great` | *steel, wood, living* | Blade 1.05 x .075; guard .34; grip .26. Held one-handed (there is no two-hand rule). *living*: sprouting twigs with emissive buds |
| `knife` | hooked, needle, *glass, sickle* | Blade .30; hooked tip or needle. *glass*: translucent emissive shard. *sickle*: Arc blade |
| `axe.hand` | wedge, hatchet | Haft cylinder .50; wedge head plate .16 x .14 |
| `axe.bearded` | plain, hooked | Haft .62; head with dropped beard .26 x .22 (the Outrider's axe) |
| `axe.crescent` | plain, spiked | Haft .62; crescent head (Arc mesh), optional back spike |
| `cleaver` | *slab, notched* | Haft .30; slab blade .40 x .16, hole near the tip (Caddock's recipe) |
| `club` | plain, studded, bound, *tusk, tankard* | Tapered lathe .55; studs or cord. *tusk*: a curved tusk Tube. *tankard*: lathe mug with handle |
| `mace.flanged` | six, *fist* | Haft .50; lathe core with 6 flange plates. *fist*: a stone hand Blob |
| `hammer.war` | pick, maul | Haft .60; box head .18 x .10 x .10 with back pick. maul: head .24 x .14 x .14 |
| `mace.root` | burl, *antler, briar* | Twisted Tube haft; Blob burl head with 3 bronze thorns. *antler*: forked tines. *briar*: thorned ball |
| `polearm` | *spear, harpoon, fork, billhook, spade* | Shaft 1.5 through the hand; head by variant (leaf, barbed bone, two tines, hooked bill, flat spade) |
| `staff` | knob, *crook, forked, skull* | Shaft 1.45; head by variant (the Cultist's skull knob is reused) |

**Off hand (6 families, 19 variants)**

| Family | Variants | Recipe |
|---|---|---|
| `shield.buckler` | plain, *tusk, chitin* | Lathe dome r .17 with boss; steel rim |
| `shield.round` | boards, hide, *cask, lid* | Disc r .26, 3 board seams, boss, rim band (today's Warrior shield, improved) |
| `shield.heater` | plain, striped, *glass* | Plate outline .42 x .50, slight bulge; chief stripe in `cloth2` |
| `shield.kite` | plain, *slab* | Plate .40 x .66 teardrop; central rib. *slab*: rough slate Blob with a strap |
| `shield.leaf` | bronze, *bark* | Leaf outline .40 x .60; bronze midrib and veins |
| `offhand.hung` | lantern, *shuttered, moss, censer, scale* | Cage cylinder .10 x .14, cap cone, ring, emissive core sphere .06 in the palette glow (always lit, dimmer at poor quality). *censer*: on a short chain. *scale*: a balance beam with two pans |

**Head (9 families, 18 variants; parented to `body`, over the hair)**

| Family | Variants | Recipe |
|---|---|---|
| `head.cap` | plain, flaps, *leaf* | Lathe skull-cap r .17 at y .88 |
| `head.hood` | cloth, *oilskin* | Sphere (.36, .38, .38) at (0, .84, -.03) with a neck drape cone; oilskin has a peaked brim |
| `head.coif` | mail | Mail sphere (.35, .36, .36) with a face opening, collar ring |
| `head.wrap` | *scarf* | The Outrider's head scarf with a hanging tail |
| `head.kettle` | plain, *half* | Lathe dome with wide brim r .24 at y .92. *half*: no brim, nasal bar |
| `head.barbute` | plain, *rimed* | Lathe dome to cheek level, T-slot as a dark inset strip; hides the hair |
| `head.mask` | *bone, tear* | The Cultist's bone mask, with hood; *tear*: the emissive tear |
| `head.circlet` | *band, briar* | Thin Tube ring at y .9, front gem or thorns |
| `head.crown` | *tin, root, antler* | Band with 5-8 points. *tin*: dented, one bent point (the Bandit King's recipe). *root*: living roots with emissive buds. *antler*: nine tines |

**Shoulders, chest, hands, legs, feet, neck (22 families, 62 variants)**

| Family | Variants | Recipe |
|---|---|---|
| `shoulder.mantle` | cloth, fur, *hide, frayed, shawl* | Flattened cone over both shoulders, short back drape to y .2 at z -.17 |
| `shoulder.pauldron` | dome, *bone* | Two lathe half-domes (.26, .18, .26) at (+-.31, .58, 0) with a rim band |
| `shoulder.spaulder` | lames, *glass, bark* | Two domes plus 2 lames each, stepping down the upper arm |
| `chest.tunic` | plain | Recolour of torso, chest and sleeves; hem strip at y -.05; collar |
| `chest.jerkin` | leather, hide, *scale* | Leather shell box (.50, .52, .31) at y .32, front lacing; no sleeves (the class tunic shows) |
| `chest.hauberk` | mail | Mail shell, mail skirt cylinder to y -.25, short mail sleeves, belt |
| `chest.coat` | *grey, skirted* | Shell plus split skirt panels to the knee and a collar (the Concord tabard and Caddock's coat) |
| `chest.cuirass` | *plate, bark* | Breastplate lathe half-dome, backplate, 2 fauld lames |
| `chest.robe` | *vestment, cassock, shroud* | Tunic plus skirt cylinder (0, -.5, 0) (.52, .45, .44) (the Healer's recipe) |
| `hands.gloves` | plain, *fingerless, mitts* | Sphere .135 over each hand plus cuff cylinder at arm (0, -.5, 0) |
| `hands.wraps` | cloth, *fur, silk* | 3 thin bands on the forearm, fingers bare |
| `hands.gauntlets` | plate, *bone, glass, root* | Glove plus flared cuff cone plus knuckle plate |
| `legs.breeches` | plain, patched | Recolour of the leg material; knee patch |
| `legs.leggings` | garter, *hide* | Recolour plus cross-garter bands |
| `legs.greaves` | plate, *full* | Shin plate half-cylinder at leg (0, -.62, .03) plus knee cop; *full* adds a thigh plate |
| `legs.kilt` | *bone* | Ring of 8 hanging plates from the belt to the knee |
| `feet.shoes` | plain | Recolour of the boot cube |
| `feet.boots` | plain, *waders, hobnail, root* | Shaft cylinder at leg (0, -.72, 0) plus folded top; waders reach the knee |
| `feet.sabatons` | plate | Plated toe box, 2 lames, shaft |
| `neck.pendant` | drop, *glass, seal, signet, ember, leaf, vial, jar* | Thin cord ring at y .62 plus a drop on the chest at (0, .5, .165) |
| `neck.cord` | beads, *knot, fang, tooth, tine* | Cord plus 3 beads or one hung piece |
| `neck.torc` | metal, *band, wood, charm* | Open ring at the collar |

**Back (step L6, 1 family):** `back.cloak` short, long, tattered, furred: cube (.52, .95, .05) at (0, .2, -.17), euler (-6, 0, 0), plus clasp; furred adds a collar.

### 2.4 How quality shows

| Quality | Treatment |
|---|---|
| Poor | Colours desaturated 35% and darkened 15%; no trim; one "wear" detail (a notch, a patch) |
| Common | Palette as is; trim in the palette's leather or plain metal |
| Uncommon | Trim in the palette `trim` colour (smooth .5, metal .6); one extra detail part (rim band, edge strip, studs) |
| Rare | Polished trim (smooth .7, metal .8); one emissive accent (rune strip, brow gem, boss inlay) in the palette glow at emission x1.5, which the existing bloom picks up |
| Epic | Gold-toned trim; two emissive accents at x2.5 with a slow pulse (`GearGlow`, 0.6 Hz) |

No lights are attached to gear. Cut from the panel design: particle motes on epic weapons.

### 2.5 The base body, all classes, rebuild on equip

- **Looks are per item, not per class.** Any class can wear anything today. The class sets only the base tunic colour.
- **Head:** hair shows when empty; barbutes hide it.
- **Shoulders:** nothing when empty. The Warrior's two fixed pads become class kit and are removed when gear drives the figure.
- **Chest:** the class tunic when empty. A chest piece switches the torso, chest and shoulder renderers (and the sleeves, by family) to the gear material and back on unequip. Druid form tint still recolours the class cloth, so it shows on whatever stays uncovered.
- **Legs and feet:** base breeches and shoes when empty; gear recolours them and adds plates.
- **Hands:** skin when empty.
- **Main hand and off hand:** empty means empty (owner decision 1). The Druid's staff yields to a main-hand item and the hood to a head piece; the Druid's cloak stays until the back slot exists.
- **Rebuild:** `ActorVisual.ApplyGear` keeps a signature per slot (item id) and rebuilds only the slots that changed: destroy the slot root's children, build the new parts, replace the `held` and `stowed` arrays together and set each new part's active state from `gearStowed`, so gear equipped while swimming shows on the back.
- **Legacy path** (`session.Items == null`): the class kit stays exactly as today.
- **Not gear-driven:** Mira, villagers and enemies. Named drops reuse the enemies' recipes (falchion, cleaver, bearded axe, bone mask, tin crown), so what a boss carries is what you loot.
- **The hook** is `GearBinder`, a new component that watches `session.Progress.equipment` and the `session.Player` instance and calls `ApplyGear` when either changes. This needs no edit to `EncounterSession.cs`. `Player`, `Progress` and `Items` are public there (checked). It can be replaced by one line at the end of `ApplyEquipment()` once the professions work is done.

### 2.6 The paper doll, icons and try-on (step L5)

- `GearDoll`: a mannequin actor built with `ActorVisual.Attach` far below the world, on its own layer, rendered by a disabled camera into a 512 x 640 RenderTexture with `Camera.Render()`. Ambient and fog are saved, overridden and restored around the render; a key light lives on the doll's layer.
- The character sheet draws that texture in place of the five rectangles; drag turns the figure.
- Item squares draw a 96 px rendered icon (cached per look key) behind the glyph for gear.
- Ctrl+click on a bag, vendor or loot-window square previews the piece on the doll and opens the sheet.

---

## 3. The loot database

### 3.1 Files and id scheme

- New data files, one per zone plus one for the world: `loot.oakhaven.json`, `loot.khaven.json`, `loot.peaks.json`, `loot.ashrim.json`, `loot.verdant.json`, `loot.world.json`.
- Each is an ordinary item file (`items`, `vendors`) with extra top-level sections (`gear`, `drops`, `sets`). `ItemDatabase.Parse` reads the first two and ignores the rest (JsonUtility drops unknown fields); the new `LootDatabase.Parse` reads the rest from the same texts.
- They are written in `EncounterContent/Loot/` (not scanned by anything) while professions is in flight, and moved with their `.meta` files into `EncounterContent/Items/` at step L2. `ZoneSceneBuilder.RegisterQuests` already adds every JSON in that folder to `Encounter.asset.itemFiles` (checked), so no loader code is needed.
- **Ids:** `loot.<oak|kha|pea|ash|ver|world>.<snake_name>`. Permanent once shipped, because saves store ids only; an item is retired by removing its sources, never by renaming or deleting. A checked-in baseline list is tested. Existing `item.*` ids and the professions' `craft.*` ids are untouched.
- A camp mob's zone, camp and name are recovered from its id (`mob.<tag>.<zoneShort>.<campIndex>.<n>`) and `Zone.Zone.camps[campIndex].mob`. That is how Old Whitefoot, Old Scree-Tusk and the Pale Reckoner (tags `wolf`, `boar`, `pale`) get their own lists with **no zone JSON edit**.

### 3.2 Schema (exact JSON)

```json
{
  "items": [
    { "id": "loot.oak.whitefoot_mantle", "name": "Whitefoot's Winter Mantle", "kind": "gear", "slot": "shoulders",
      "quality": 3, "level": 2, "value": 12, "armor": 7, "stamina": 4, "agility": 4,
      "description": "The one white paw is still on it.", "canonStatus": "GAME-ONLY" },
    { "id": "loot.oak.broken_oath_sabre", "name": "Broken-Oath Sabre", "kind": "gear", "slot": "mainhand",
      "quality": 4, "level": 4, "value": 35, "weaponDamage": 20, "stamina": 7, "strength": 9,
      "description": "Snapped across a knee and forged whole again by worse hands.", "canonStatus": "GAME-ONLY" }
  ],
  "vendors": [
    { "npc": "Ama Rusk", "items": [ "loot.oak.market_day_breeches" ] }
  ],
  "gear": [
    { "id": "loot.oak.whitefoot_mantle", "look": "shoulder.mantle:fur/pilgrim", "source": "boss:Old Whitefoot", "boss": true, "unique": true },
    { "id": "loot.oak.due_cleaver", "look": "cleaver:notched/sandthrone", "source": "boss:Caddock, the Bandit King", "boss": true, "unique": true, "set": "set.crowsfoot" },
    { "id": "loot.oak.broken_oath_sabre", "look": "sword.sabre:broken/sandthrone+glow", "source": "boss:Caddock, the Bandit King", "boss": true, "unique": true,
      "effects": [ { "kind": "onKillHeal", "amount": 12, "text": "Each kill restores 12 health." } ] },
    { "id": "item.tin_crown", "look": "head.crown:tin/sandthrone", "source": "boss:Caddock, the Bandit King", "legacy": true, "unique": true, "set": "set.crowsfoot" }
  ],
  "drops": [
    { "id": "drop.oak.caddock", "zone": "oakhaven", "mob": "Caddock, the Bandit King", "rank": "elite",
      "groups": [
        { "chance": 1.0, "signature": true, "pick": [ { "item": "item.tin_crown" }, { "item": "loot.oak.due_cleaver" }, { "item": "loot.oak.due_coat" } ] },
        { "chance": 0.10, "lucky": true, "pity": 10, "pick": [ { "item": "loot.oak.broken_oath_sabre" } ] } ] },
    { "id": "drop.oak.deserters", "zone": "oakhaven", "tag": "deserter", "rank": "normal",
      "groups": [ { "chance": 0.03, "lucky": true, "pick": [
        { "item": "loot.oak.deserters_falchion" }, { "item": "loot.oak.sandthrone_halfhelm" }, { "item": "loot.oak.lookouts_breeches" } ] } ] },
    { "id": "drop.oak.deepstair", "zone": "oakhaven", "tag": "deserter", "rank": "normal", "levelMin": 4,
      "groups": [ { "chance": 0.04, "lucky": true, "pick": [ { "item": "loot.oak.due_boots" } ] } ] },
    { "id": "drop.world.low", "rank": "normal", "levelMin": 1, "levelMax": 5,
      "groups": [ { "chance": 0.0025, "lucky": true, "pick": [ { "item": "loot.world.golden_cask_tankard" } ] } ] }
  ],
  "sets": [
    { "id": "set.crowsfoot", "name": "The Deserter King's Due",
      "pieces": [ "loot.oak.due_cleaver", "loot.oak.due_coat", "loot.oak.due_boots", "item.tin_crown" ],
      "bonuses": [
        { "count": 2, "effects": [ { "kind": "stat", "stat": "MaxHealth", "amount": 30, "text": "+30 health" } ] },
        { "count": 3, "effects": [ { "kind": "stat", "stat": "AttackPower", "amount": 6, "text": "+6 attack power" } ] } ] }
  ]
}
```

Field rules:
- `items`: plain `ItemDef`. `level` is the required level; the item's **curve level is `level + 1`**, the same rule generated gear follows. Armour, damage and value are the generated formula at the curve level and quality.
- `vendors`: plain `VendorDef` by NPC name. `StockFor` already adds a named entry on top of the role's stock (checked), so vendor shelves need no code. The five names (Ama Rusk, Cato Brisk, Yara Quell, Sefa Brine, Ondine Varro) are merchants in the zone files or tests.
- `gear[]`: one entry per named item. `source` is one of `boss:<mob>`, `mob:<tag>@<zone>`, `quest:<quest id>`, `vendor:<npc>`, `world:<min>-<max>`, `secret:<id>`. `legacy: true` marks the 12 existing items (look, source and set only; exempt from the curve tests).
- `drops[]`: match on any of `zone` (the id without `zone.`), `tag`, `mob` (the camp's `mob` string), `rank` (`elite`, `normal`, or absent for both), `levelMin` / `levelMax` (defaults 1 and 13).
- `groups[]`: `chance`; `signature` (unowned-first, see 3.4); `lucky` (scaled by luck); `pity` (certain on that many dry kills, from step L3); `pick` with optional `weight` (default 1).
- No `ItemDef` field is added and `Items.cs` is not edited.

### 3.3 Power curve (from `ItemDatabase.Generate`, float maths reproduced in a script)

`power = level x {common 1, uncommon 1.35, rare 1.7, epic 2.1}`; weapon = round(3 + 1.6 x power); armour = power x 1.4 (head, shoulders, hands, feet), 2.2 (chest, legs), 2.6 (off hand); k = max(1, power / 4). Each cell is common / uncommon / rare / epic.

| Curve level | Weapon damage | Armour x1.4 | Armour x2.2 | Armour x2.6 | Stat points: uncommon 4k / rare 5k / boss rare 6k / epic 6k | Value (armour): uncommon / rare / epic |
|---|---|---|---|---|---|---|
| 1 | 5 / 5 / 6 / 6 | 1 / 2 / 2 / 3 | 2 / 3 / 4 / 5 | 3 / 4 / 4 / 5 | 4 / 5 / 6 / 6 | 3 / 4 / 5 |
| 2 | 6 / 7 / 8 / 10 | 3 / 4 / 5 / 6 | 4 / 6 / 7 / 9 | 5 / 7 / 9 / 11 | 4 / 5 / 6 / 6 | 6 / 8 / 10 |
| 3 | 8 / 9 / 11 / 13 | 4 / 6 / 7 / 9 | 7 / 9 / 11 / 14 | 8 / 11 / 13 / 16 | 4 / 6 / 8 / 9 | 9 / 12 / 15 |
| 4 | 9 / 12 / 14 / 16 | 6 / 8 / 10 / 12 | 9 / 12 / 15 / 18 | 10 / 14 / 18 / 22 | 5 / 8 / 10 / 13 | 12 / 16 / 20 |
| 5 | 11 / 14 / 17 / 20 | 7 / 9 / 12 / 15 | 11 / 15 / 19 / 23 | 13 / 18 / 22 / 27 | 7 / 11 / 13 / 16 | 15 / 20 / 25 |
| 6 | 13 / 16 / 19 / 23 | 8 / 11 / 14 / 18 | 13 / 18 / 22 / 28 | 16 / 21 / 27 / 33 | 8 / 13 / 15 / 19 | 18 / 24 / 30 |
| 7 | 14 / 18 / 22 / 27 | 10 / 13 / 17 / 21 | 15 / 21 / 26 / 32 | 18 / 25 / 31 / 38 | 9 / 15 / 18 / 22 | 21 / 28 / 35 |
| 8 | 16 / 20 / 25 / 30 | 11 / 15 / 19 / 24 | 18 / 24 / 30 / 37 | 21 / 28 / 35 / 44 | 11 / 17 / 20 / 25 | 24 / 32 / 40 |
| 9 | 17 / 22 / 27 / 33 | 13 / 17 / 21 / 26 | 20 / 27 / 34 / 42 | 23 / 32 / 40 / 49 | 12 / 19 / 23 / 28 | 27 / 36 / 45 |
| 10 | 19 / 25 / 30 / 37 | 14 / 19 / 24 / 29 | 22 / 30 / 37 / 46 | 26 / 35 / 44 / 55 | 14 / 21 / 26 / 32 | 30 / 40 / 50 |
| 11 | 21 / 27 / 33 / 40 | 15 / 21 / 26 / 32 | 24 / 33 / 41 / 51 | 29 / 39 / 49 / 60 | 15 / 23 / 28 / 35 | 33 / 44 / 55 |
| 12 | 22 / 29 / 36 / 43 | 17 / 23 / 29 / 35 | 26 / 36 / 45 / 55 | 31 / 42 / 53 / 66 | 16 / 26 / 31 / 38 | 36 / 48 / 60 |
| 13 | 24 / 31 / 38 / 47 | 18 / 25 / 31 / 38 | 29 / 39 / 49 / 60 | 34 / 46 / 57 / 71 | 18 / 28 / 33 / 41 | 39 / 52 / 65 |

Weapon value is the armour value x 1.4.

Rules for named items:
- **Armour and damage are exactly the generated number** at the curve level and quality. Named items win on stat points and looks, never on raw armour or damage.
- **Stat budget** (sum of the five stats): uncommon 4k, rare 5k, boss signature rare 6k, epic 6k at epic power. Generated suffixes give 4 to 5k, so a boss piece carries about 20% more than the best random rare, and the points go on stats that work (Stamina, Strength, Agility). Luck charms spend fewer points.
- **Generation is not changed.** No table, multiplier or draw in `Generate` moves, so every generated item in Chris's save keeps its name and stats.
- **The 12 existing named items keep their stats.** They gain looks, set membership and sources only.
- **Crafted gear's place holds:** vendor common < crafted (on-curve uncommon, one stat of about k) < generated or named uncommon < rare < boss rare < epic. The capstone `craft.heartwood_greatblade` (36) stays under every level-13 rare weapon (38).

### 3.4 Rarity and drop rules

`ItemDatabase.RollLoot` is unchanged (junk entries plus at most one generated piece: 10-25% on normal mobs, 70% on elites). `LootDatabase.Roll` adds to it:

| Group | Source | Chance | Rule |
|---|---|---|---|
| Signature | Hesk, Caddock, the Root-Warden (15 min respawn) | 1.0 | One piece from the list you do not own. When you own them all: any one of them at 35%. |
| Signature | The other nine elites (3-5 min respawn) | 0.5 | Same. |
| Boss rare table | Five elites | 0.15 | Lucky. |
| Epic | Six bosses | 0.10 dungeon end bosses, 0.04 outdoor | Lucky. From step L3: certain on the 10th (dungeon) or 25th (outdoor) dry kill; a quarter as often once owned. |
| Zone table | Normal mobs by tag and zone | 0.03 (0.04-0.05 on small or slow camps) | Lucky. One pick among 2-3 items. |
| Zone rare | Named in the item list | 0.015-0.04 | Lucky. |
| World drop | Any camp mob in a level band | 0.0025 normal, 0.02 elite (epic 0.001 / 0.01) | Lucky. |
| Generated epic | Elites | 6% of generated rares become epic (about 1.5% per elite kill) | Same slot, level and seed, quality 4. The first use of the epic formula. |

- **Owned** means in the bags or worn (`Inventory.Has`) in step L2, and "ever found" (the Armoury list) from step L3.
- **Caps:** at most 1 named item per normal kill and 2 per elite kill; an epic does not count against the cap.
- **A boss never drops only junk:** if an elite's roll holds no gear, a generated piece at its level is added (rare 35%, else uncommon).
- **The four existing 100% boss entries** in `items.json` (tin crown, stave, tine, crown) are not edited while the professions engineer owns that file. `Roll` removes from the base roll any item that is in the same mob's signature list, so they stop double-dropping. They can be deleted from `items.json` afterwards. "The Tin Crown" quest counts kills, not the item (checked), and `ItemTests` only rolls wolf and boar tables (checked).
- **Luck** from worn effects multiplies lucky chances by (1 + luck), capped at +30%.
- **Farm maths:** a 3% table of 3 items is about 100 kills per chosen piece (about 30 minutes on a 4-5 mob camp). An outdoor elite's two signatures take about 4 kills. An outdoor epic takes about 16 kills on average and never more than 25; a dungeon epic about 7 runs and never more than 10.
- **Full bags:** drops stay on the body until taken (the loot window, step L1). Today anything that does not fit is destroyed (`LootCamp`, checked).

### 3.5 Sources

| Source | How | Count in v1 |
|---|---|---|
| Camp mobs | `drops` by zone and tag | 43 items |
| Elites and bosses | `drops` by mob name: signature list, rare table, epic | 24 + 5 + 6 |
| World drops | `drops` by level band | 5 rares + 1 epic |
| Quests | `rewards.bagItems` on 15 quests (the field the professions addendum D.4 adds to `Quests.cs`) | 15 |
| Vendors | `vendors` entries by NPC name in the loot files | 5 |
| Secrets and chests | Unchanged: 7 of the 12 existing named items; their looks and sources are listed in `loot.world.json` | 0 new |
| Crafted | The professions' 21 `craft.*` pieces; this design gives them looks only | 0 new |

Cut from v1: random-roll treasure chests, rare spawns, salvage (section 4).

### 3.6 Sets and unique effects

Two sets, both four pieces including the existing boss crown (`ITEMS_V1.md`): The Deserter King's Due (Crowsfoot Hollow) and Vigil of the Root-Mother (the Root-Mother's Deep). Bonuses switch on by pieces worn and are recomputed whenever equipment changes; nothing is saved.

Only these effect kinds are allowed, each checked against the code:

| Kind | What it does | Carried by |
|---|---|---|
| `stat` | Flat or percent modifier on MaxHealth, MaxPower, AttackPower, Armor or a primary stat | `Player.Stats.AddModifiers` with `ModifierOp.Flat` or `PercentAdd`, from a separate source object so it clears like the equipment modifiers |
| `onKillHeal` | Heal N on a kill | `Player.Health.ApplyHealing` in `EnemyDied` |
| `onKillPower` | Restore N resource on a kill | `Player.Resource.Pool` in `EnemyDied` |
| `restRegen` | +N on the out-of-combat health tick (7 today) | the regen line in `Update` |
| `coins` | +N% coins from bodies | the corpse roll |
| `luck` | +N% on lucky drop groups | `LootDatabase.Roll` |

Caps: coins and luck +30% in total, restRegen +20, onKillHeal 60. Crit, dodge, block, haste, on-hit procs and move speed are not offered: no gameplay code reads them. Thirteen items carry an effect in v1: the 7 epics and 6 rares (two luck charms, two coin pieces, one rest piece and one resource-on-kill piece).

### 3.7 The item list

`ITEMS_V1.md`: 104 named items (49 uncommon, 48 rare, 7 epic) with slot, required level, quality, stats, effect, source, look and flavour, plus the two sets, a drop list per boss, and looks for the 12 existing named items and the 21 crafted pieces. Every zone has named gear in all nine slots. Every stat line was checked against its budget by script.

---

## 4. Thrill features

| # | Feature | What the player sees | Cost | Step |
|---|---|---|---|---|
| 1 | Roll at death | Loot is decided when the mob dies, so the body can show it | 0.25 d | L1 |
| 2 | Corpse beacon | White twinkle (junk and common), green glint and 1.2 m column (uncommon), blue 4 m beam (rare), purple 7 m beam with a ground ring (epic). Gone when the body is emptied or respawns | 0.5 d | L1 |
| 3 | Loot window | E opens a framed list: quality border, coloured name, upgrade arrow, coins line, "Take all [E]". What does not fit stays on the body. Junk-only bodies still loot with one key press | 0.75 d | L1 |
| 4 | Better or worse | Tooltip shows differences against the worn piece ("+3 weapon damage" green, "-2 Stamina" red); a green arrow on squares in bags, at vendors and in the loot window | 0.5 d | L1 |
| 5 | Rarity call-outs | A coloured chat line per item; a "RARE" or "EPIC" toast over the item name | 0.25 d | L1 |
| 6 | Signature lists | Every elite has its own named pieces, unowned first | in L2 | L2 |
| 7 | Sets and unique effects | Tooltip lines: "Unique", the effect, the set with pieces worn and bonuses | 0.75 d | L2 |
| 8 | The Armoury | Fifth quest-book tab: zones on the left with found / total, sources on the right. Unknown = silhouette with slot and source kind; known (you have killed the source) = grey name; found = full colour with tooltip | 1 d | L3 |
| 9 | Epic pity | The purple is certain by the 10th or 25th dry kill | 0.25 d | L3 |
| 10 | "New look" | First time an appearance enters your bags: a "NEW LOOK" toast and a count on the Armoury tab | 0.25 d | L3 |
| 11 | 3D paper doll, icons, try-on | Section 2.6 | 2 d | L5 |

Cut or deferred, with the reason:
- **Rare spawns** (a named mob that sometimes replaces a camp mob): 1 d plus `EncounterEnemy` respawn changes. Good, but v1 already has 12 elites with lists. Later.
- **Treasure chests that roll gear for your weakest slot:** needs five new chest secrets in the zone JSONs, which the professions work is editing. Later.
- **Salvage at the forge:** depends on bars and the Trades window. After professions step P3.
- **Epic particle motes, a fallback loader that retries without the loot files:** not worth their cost; the data tests guard the files.
- **Giving Intellect and Spirit an effect, weapon types and speeds, two-handers, armour classes, sockets, rotating vendor stock:** separate designs.

---

## 5. Code changes by file and class

All under `Assets/Crulanda/`. "New" files do not collide with the professions work.

### 5.1 New files

| File | Contents |
|---|---|
| `Scripts/Encounter/GearLooks.cs` | `[Serializable] GearLookFile, GearPalette, GearMaterialWord, GearPieceWord, GearFallback, GearTint, GearLookDef`; `struct GearLook { family, variant, quality; Color cloth, cloth2, leather, metal, trim, wood, glow; bool forceGlow; string detail }`; `static GearLooks Load()` (Resources, cached); `static GearLooks Parse(string json)` (collects errors, throws one `ArgumentException`); `GearLook Resolve(ItemDef d)`; `void Register(string itemId, string look)`; `static bool TryParseLook(string s, out string family, out string variant, out string palette, out bool glow)`; `static bool TrySplitGenerated(ItemDef d, out string material, out string piece, out int level, out int seed)`; `static string LookKey(ItemDef d)`; `static readonly GearFamily[] Families` (name, slot, variants, `gen`) |
| `Scripts/Encounter/GearMeshes.cs` | Cached generators: `Lathe(Vector2[] profile, int sides, float arc = 360)`, `Blade(length, width, thick, tip, belly, curve)`, `Plate(Vector2[] outline, float thick, float bulge)`, `Prim(PrimitiveType)` (the built-in meshes fetched once, so gear parts are made without colliders and work in EditMode tests). Reuses the public statics `ZoneMeshes.Cone`, `Arc`, `Tube`, `Blob`, `Box` |
| `Scripts/Encounter/GearMats.cs` | `static Material Get(Color c, float smooth, float metal, Color emission)` from a dictionary keyed on quantised values (today's `ActorVisual.Mat` makes a new material per call); `Instance(...)` for pulsing epics, destroyed with the part |
| `Scripts/Encounter/ActorVisual.Gear.cs` | `partial class ActorVisual`: `public void ApplyGear(IList<ItemStack> equipment, ItemDatabase items, GearLooks looks)`; `public void ApplyGearIds(string[] itemIds, ItemDatabase items, GearLooks looks)` (mannequins, doll); `void GearInit()`; `void ClearSlot(int slot)`; `Transform GPart(...)`; per-slot signature; `public int GearPartCount` |
| `Scripts/Encounter/ActorVisual.GearWeapons.cs`, `ActorVisual.GearArmor.cs` | One `Build<Family>(GearLook l, Transform root)` per family (section 2.3) |
| `Scripts/Encounter/GearGlow.cs` | MonoBehaviour pulsing one material's emission |
| `Scripts/Encounter/GearBinder.cs` | `[RuntimeInitializeOnLoadMethod]` creates it; `LateUpdate` finds the `EncounterSession`, hashes `Progress.equipment` ids plus the `Player` instance and calls `ApplyGear` on change. Does nothing when `session.Items == null` |
| `Scripts/Encounter/WardrobeCapture.cs` | Section 8 |
| `Scripts/Encounter/Loot.cs` | Data: `LootFile { GearMeta[] gear; DropDef[] drops; SetDef[] sets }`, `GearMeta { id, look, source, set; bool unique, boss, legacy; GearEffect[] effects }`, `GearEffect { kind, stat, text; float amount; bool percent }`, `DropDef { id, zone, tag, mob, rank; int levelMin = 1, levelMax = 13; DropGroup[] groups }`, `DropGroup { float chance; bool signature, lucky; int pity; DropPick[] pick }`, `DropPick { string item; int weight = 1 }`, `SetDef { id, name; string[] pieces; SetBonus[] bonuses }`, `SetBonus { int count; GearEffect[] effects }`, `[Serializable] LootLuck { string source; int kills, dry }`, `struct LootDrop { string item; int count }`, `struct LootContext { zone, tag, mob; int level; bool elite; static LootContext From(string persistentId, ZoneCamp[] camps, int level, bool elite) }`. `sealed class LootDatabase`: `static Parse(IEnumerable<string> jsonFiles, ItemDatabase items)` (collects errors, throws one `ArgumentException`; registers looks with `GearLooks`); `List<LootDrop> Roll(LootContext c, ItemDatabase items, Func<string,bool> owned, float luck, List<LootLuck> pity, System.Random rng)`; `GearMeta Meta(string id)`; `SetDef SetOf(string id)`; `string TooltipLines(string id, EncounterProgress p)`; `static List<string> Validate(LootDatabase, IEnumerable<ZoneDefinition>, QuestDatabase)`. `static class GearEffects`: `GearEffectTotals Compute(EncounterProgress p, ItemDatabase items, LootDatabase loot, object source)` (pure; applies the caps). `static class LootJudge`: `Score(ItemDef)` = weaponDamage + Strength + 0.5 x Stamina + 0.25 x armor + 0.25 x Agility (Intellect and Spirit score 0 because nothing reads them); `Compare(candidate, worn)`; `IsUpgrade(candidate, worn, playerLevel)`; `DeltaLines(...)`. `sealed class ArmouryLog` (step L3; the `DiscoveryLog` pattern): `Bind(progress)`, `Sweep()` (scans bags and equipment; raises `NewItem` and `NewLook`), `StateOf(itemId)`, `TallyOf(zoneId)` |
| `Scripts/Encounter/LootBeacon.cs` | `static void Show(EncounterEnemy, int bestQuality)`, `static void Clear(EncounterEnemy)`. A stretched cylinder and two crossed quads on the enemy root (not `Body`, which tips over), five shared emissive materials, no lights |
| `Scripts/Encounter/EncounterSession.Loot.cs` | `partial EncounterSession`: `LootDatabase Loot`; `LoadLoot()`; `RollCorpse(EncounterEnemy)`; `OpenLoot(EncounterEnemy)`; `LootOpen`, `LootItems`, `TakeLoot(int)`, `TakeAllLoot()`, `CloseLoot()`; `TickLoot()` (closes beyond 5 m; from L3 runs `Armoury.Sweep()` twice a second, so buying, crafting and quest rewards are noticed with no hook in their code); `ApplyGearEffects()`; `OnKillEffects()` |
| `Scripts/Encounter/EncounterHud.Loot.cs` | `partial EncounterHud`: `LootRect`, `DrawLoot()`, `LootUiBlocks(Vector2)`, `SquareMarks(Rect, ItemDef)` (upgrade arrow), `CompareLines(ItemDef d, ItemDef worn)`, `LootLines(ItemDef d)` |
| `Scripts/Encounter/EncounterHud.Armoury.cs` | `BookArmoury(Rect list, Rect page)`, modelled on `BookDiscoveries` (L3) |
| `Scripts/Encounter/GearDoll.cs`, `EncounterHud.Doll.cs` | Section 2.6 (L5) |
| `Resources/Gear/looks.json` (+ folder and `.meta` files) | Section 2.2 |
| `EncounterContent/Loot/loot.*.json` (6 files + `.meta`) | Sections 3.2 and 3.7; moved into `Items/` at L2 |
| `Tests/EditMode/GearLookTests.cs`, `GearVisualTests.cs`, `LootDataTests.cs`, `LootRollTests.cs`, `LootJudgeTests.cs`, `ArmouryTests.cs`; `Tests/PlayMode/LootWindowTests.cs` | Section 7 |

### 5.2 Edits to files the professions work does not list

- **`ActorVisual.cs`** (small, needed because today's build keeps no references):
  - `Build`: keep the torso, chest, shoulder, sleeve, leg, hip and boot renderers in new fields (`baseChest`, `baseSleeves`, `baseLegs`, `baseBoots`).
  - `Features`: keep the hair parts in `Transform[] hairParts`.
  - Warrior case: keep the two pads in `Transform[] classKit`; Druid case: the hood.
  - No signature changes. The enemy child names pinned by `HollowQuestTests` are untouched.
- **`EncounterCapture.cs`:** dispatch lines for `--crulanda-wardrobe-capture <dir>` and `--crulanda-loot-capture <dir>`.
- **`EncounterEnemy.cs`:** `public List<LootDrop> Drops;` cleared in `Respawn()` beside `Looted = false`, with `LootBeacon.Clear(this)` (L1).

The owner's hunting request (wild animals huntable and skinnable) may add creature builders to `ActorVisual.cs` and touch `EncounterEnemy.cs`; these edits are a few lines each and merge by hand.

### 5.3 Touch points in shared files (land after professions step P1, one engineer at a time)

| File | Edit | Step |
|---|---|---|
| `EncounterSession.cs` | `sealed class` becomes `sealed partial class` (it is not partial today) | L1 |
| | `EnemyDied`: `if (enemy.Camp) RollCorpse(enemy);` | L1 |
| | `Interact`: `if (LootOpen) { TakeAllLoot(); return; }` at the top; `LootCamp(corpse)` becomes `OpenLoot(corpse)`; `LootCamp` moves into the partial | L1 |
| | `InteractPrompt`: `if (LootOpen) return "Take all";`; `Update`: `TickLoot();` | L1 |
| | `LoadLoot();` after `LoadItems()` | L2 |
| | `ApplyEquipment` (end): `ApplyGearEffects();`; `EnemyDied`: `OnKillEffects();`; regen line: `ApplyHealing(7 + gearFx.restRegen)` for the player | L2 |
| | `StartArmoury();` beside `StartDiscoveries()`; `Armoury.Bind(Progress)` in `Load()` | L3 |
| `EncounterHud.cs` | `DrawLoot();` before `DrawDiscoveryToast()`; `LootUiBlocks` in `BlocksPointer` | L1 |
| `EncounterHud.Items.cs` | `ItemSquare`: `SquareMarks(r, d);`. `ItemTooltip`: the "Currently worn" line becomes `CompareLines(d, w)` | L1 |
| | `ItemTooltip`: append `LootLines(d)` (unique, effect, set, source) | L2 |
| | `DrawCharacter`: the five figure lines become `DrawDoll(mid);`. `ItemSquare`: icon behind the glyph; Ctrl+click preview | L5 |
| | `RightSlots { 4, 5, 6, 9 }`, glyph "Bk" | L6 |
| `EncounterHud.Quests.cs` | "armoury" / "Armoury" added to the two tab arrays (four tabs at 130 px today; a fifth needs narrower tabs or a wider strip); `else if (BookTab == "armoury") BookArmoury(list, page);` | L3 |
| `EncounterProgress.cs`, `EncounterSave.cs` | Section 6 | L3 |
| `Encounter.asset` | Six `itemFiles` entries, written by the scene builder | L2 |
| `items.json` | Optional tidy-up: delete the four 100% boss entries | after professions |
| Quest JSONs (`oakhaven`, `khaven`, `peaks`, `ashrim`, `verdant`) | `rewards.bagItems` on 15 quests | L4 |
| `Items.cs` | Append `Back` to `EquipSlot`, `"back"` to `SlotIds`, `"Back"` to `SlotNames`, and a `back` row to `Pieces` (Cloak, Cape, Shawl). The `Pieces` row is required: `Generate` and `RollLoot` index it for every slot | L6 |

Toasts use the professions' planned `ShowToast(kicker, name)`; if L1 lands first, L1 adds that method and the professions work drops its copy.

---

## 6. Save, migration and sequencing with professions

### 6.1 Save

| Step | Save change |
|---|---|
| A1-A3, L1, L2, L4, L5 | **None.** Looks, uniqueness, set bonuses and effects are all derived from the item ids already in `progress.bag` and `progress.equipment`. New ids resolve through `ItemDatabase`. |
| L3 Armoury | **Format 9.** `EncounterProgress`: `List<string> armoury`, `List<string> looks`, `List<LootLuck> lootLuck`. `AddArmouryMigration : ISaveMigration` (8 to 9), a copy of `AddDiscoveriesMigration` inserting `"armoury":[],"looks":[],"lootLuck":[]`, registered after `AddProfessionsMigration`. `Read` guards: null lists become empty; blank ids and later duplicates dropped; negative counts refuse the save ("Invalid loot data.", file untouched); unknown ids are kept and ignored. On first bind, named gear in the bags or worn, and found secrets that carry named gear, count as found. |
| L6 Back slot | No format step: `Inventory.Ensure` pads the equipment list to ten. An older build would reject the newer save (it requires exactly `SlotIds.Length` entries), so note it in `SAVE_FORMAT.md`. |

- If format 8 has not been committed when L3 is ready, the three lists ride in `AddProfessionsMigration` and there is no format 9.
- Back up Chris's save to `...\hel\work\save-backups\<date>-pre-loot` before L2 (the first time `loot.*` ids can enter it) and again before L3 and L6.
- An older build that loads a save holding `loot.*` ids shows them as "?" and keeps them (existing behaviour).
- Not saved: drops on bodies, the open loot window, set and effect state.
- Every new test sets `SaveDirectoryOverride`; none may touch the real save.

### 6.2 Sequencing

The professions work (engineer B) owns `Items.cs`, `items.json`, `Quests.cs`, `EncounterProgress.cs`, `EncounterSave.cs`, `EncounterHud*.cs`, the quest JSONs and most of `EncounterSession.cs`, and moves the save to format 8 in its step P1.

| Loot step | Shares with professions | Must land first |
|---|---|---|
| A1, A2, A3 | Nothing. New files, `ActorVisual.cs`, `EncounterCapture.cs` | Nothing. Runs in parallel now. |
| L1 Loot feel | `EncounterSession.cs`, `EncounterHud.cs`, `EncounterHud.Items.cs` (about 10 lines) | P1 published (format 8, the reworked item panels). Land at one of B's step boundaries. |
| L2 Named loot live | `EncounterSession.cs` (5 lines), `EncounterHud.Items.cs` (1 line), `Encounter.asset` | L1. B's current step committed, so the scene builder's `Encounter.asset` write does not cross B's `professionFiles` edit. |
| L3 Armoury | `EncounterProgress.cs`, `EncounterSave.cs`, `EncounterHud.Quests.cs`, `EncounterSession.cs` | Format 8 committed (or ride inside it). |
| L4 Quest rewards | The five quest JSONs | B's `rewards.bagItems` (addendum step V4). |
| L5 Doll and icons | `EncounterHud.Items.cs` | B's trade-bag rows in the bags window (V4), so the layout is settled. |
| L6 Back slot | `Items.cs`, `EncounterHud.Items.cs` | All professions steps that edit `Items.cs` (through P4), and owner decision 2. |

What professions needs from loot: nothing. When B adds the `craft.*` items their looks already exist, and a test names any id that was renamed. B's new item kinds (`material`, `tool`, `bag`) are ignored by the look resolver and by `LootJudge`.

One balance note for B: the crafted-gear rule "stays under the rare quest and elite pieces (38)" still holds, but epics (up to 47 damage) now exist above it.

---

## 7. Tests

EditMode `GearLookTests` (A1):
- `Looks_file_parses_and_every_family_and_palette_named_exists`
- `Every_generated_name_resolves_to_a_look` (9 slots x levels 1-13 x qualities 0-4 x 40 seeds; none hits the fallback)
- `Every_crafted_piece_has_a_look` (the 21 ids, even before they exist in `items.json`)
- `Same_id_always_gives_the_same_look`
- `Variant_comes_from_the_seed_and_generated_gear_never_gets_a_named_variant`
- `Quality_raises_trim_and_only_rare_and_epic_glow`
- `Unknown_item_falls_back_to_the_slot_default`
- `Look_strings_parse_and_reject_unknown_families`

EditMode `GearVisualTests` (A1 weapons, A2 the rest):
- `Bare_warrior_holds_nothing_and_has_no_pads`
- `Equipping_each_slot_adds_parts_under_its_gear_root`
- `Unequipping_restores_the_base_body_materials`
- `Swapping_gear_destroys_the_old_parts_and_reuses_materials` (cache count stable over 50 swaps)
- `Gear_built_while_swimming_is_stowed_not_held`
- `Barbutes_hide_the_hair_and_open_helms_do_not`
- `Full_kit_stays_under_the_part_budget` (70)
- `Every_family_and_variant_builds_at_every_quality`
- `Legacy_session_without_items_keeps_the_class_kit`
- `Enemy_weapon_and_crown_parts_are_unchanged`
- `Binder_redresses_the_player_after_equip_unequip_and_load`

EditMode `LootDataTests` (A3, reading the JSON from disk in either folder):
- `Every_item_file_parses_together`
- `Each_loot_file_parses_alone_beside_the_core_file` (names the bad file)
- `Loot_ids_follow_the_scheme_and_match_their_file`
- `Loot_id_baseline_still_resolves` (`Tests/EditMode/LootIdBaseline.txt`)
- `Named_gear_sits_on_the_generated_curve` (armour, damage and value equal `db.Get(GearId(slot, level + 1, quality, 0))`; legacy items exempt)
- `Named_gear_stat_budgets_hold`
- `Every_named_item_has_meta_look_text_and_canon_label`
- `Every_look_string_names_a_real_family_variant_and_palette`
- `Every_named_item_has_a_source_and_every_source_exists` (mob names and tags against the zone camps, quest ids against the quest files, vendor names against `StockFor`)
- `Every_elite_camp_has_a_signature_list`
- `Every_zone_has_named_gear_for_every_slot`
- `Drop_groups_are_sane` (chance in (0, 1], weights above 0, items resolve)
- `Sets_have_real_pieces_in_distinct_slots_and_rising_bonuses`
- `Effects_use_only_kinds_the_game_carries_out_and_stay_inside_their_caps`
- `Crafted_gear_stays_under_named_rares_of_its_band` (skips while no `craft.*` item exists)

EditMode `LootRollTests` (A3 logic, L2 live):
- `Roll_is_deterministic_for_a_seed_and_respects_the_caps`
- `A_signature_kill_gives_an_unowned_piece_until_the_list_is_owned`
- `Old_guaranteed_entries_do_not_double_drop`
- `A_boss_never_drops_only_junk`
- `Drop_rates_match_the_table` (200,000 seeded rolls per source)
- `Luck_raises_only_lucky_groups_and_is_capped`
- `Elites_sometimes_drop_a_generated_epic`
- `Set_bonuses_switch_on_and_off_with_pieces_worn`
- `Gear_effects_total_and_clear`
- `The_epic_is_certain_by_its_pity_count` and `An_owned_epic_drops_a_quarter_as_often` (L3)

EditMode `LootJudgeTests` (L1): `An_empty_slot_makes_any_wearable_piece_an_upgrade`, `Deltas_list_gains_and_losses_by_stat`, `A_piece_above_your_level_is_not_marked_as_an_upgrade`, `Intellect_and_Spirit_do_not_count`.

EditMode `ArmouryTests` (L3): `Sweep_records_named_gear_once`, `Generated_gear_adds_a_look_not_an_entry`, `Unknown_entries_give_slot_and_source_kind_only`, `A_killed_boss_reveals_the_names_on_its_list`, `Hidden_find_items_never_name_their_place`, `Found_secrets_and_worn_gear_count_on_first_bind`.

EditMode `SaveMigratorTests` (L3): `V8Payload_MigratesTo9_AddsArmouryLooksAndLuck_KeepsEveryOtherCharacter`, `Negative_loot_luck_refuses_the_save_file_unchanged`. (L6): `Old_saves_gain_an_empty_back_slot`.

PlayMode `LootWindowTests` (L1; each sets `SaveDirectoryOverride`): `A_dead_camp_mob_carries_its_drops_and_a_beacon`, `Beacon_colour_is_the_best_quality_on_the_body`, `Junk_only_bodies_loot_with_one_press`, `Gear_opens_the_window_and_E_takes_all`, `Full_bags_leave_the_item_on_the_body`, `The_window_closes_when_you_walk_away`, `Respawn_clears_drops_and_beacon`.

PlayMode (L5): `Doll_matches_the_player_gear_and_restores_scene_lighting`.

Must stay green: `ItemTests` (all six; `RollLoot` and `Generate` are untouched), `HollowQuestTests`, `DiscoveryTests`, `EncounterLoopTests`, `DruidLoopTests`, `WaterTests`.

---

## 8. Capture plan: the wardrobe line-up

`--crulanda-wardrobe-capture <dir>` (`WardrobeCapture.cs`) writes to `...\hel\work\ui-captures\wardrobe\`. It runs in the validation copy with `SaveDirectoryOverride`, time fixed at noon in clear weather, on the Oakhaven green. Mannequins are bare actors built with `ActorVisual.Attach` plus `ApplyGearIds`. Every family and variant appears in at least one shot, with its name as a label.

| Shot | Contents | From step |
|---|---|---|
| `01-weapon-rack-a`, `01-weapon-rack-b` | Every main-hand family x variant, held, in rows | A1 |
| `02-shield-wall` | Every off-hand family x variant | A1 |
| `03-quality-ladder` | One sword, shield and helm from poor to epic, at dusk so the glow reads | A1 |
| `04-sets-front`, `05-sets-back` | 5 columns (levels 2, 5, 8, 10, 13) x 3 rows (common, uncommon, rare): full generated kits from fixed seeds | A2 |
| `06-helms`, `07-shoulders-chests`, `08-hands-legs-feet-neck` | Close rows per slot family and variant | A2 |
| `09-palettes` | The same kit in all 10 palettes | A2 |
| `10-crafted` | The 21 blacksmith pieces by metal tier | A2 |
| `11-in-motion` | The player in rare level-13 kit: walking, sneaking, sitting, swimming (stow check), front and back | A2 |
| `12-druid-forms` | The Druid in the same kit in each form | A2 |
| `13-named-oakhaven` ... `17-named-verdant`, `18-named-world` | Every named item of the zone on a mannequin, boss lists and sets grouped | A3 |

`--crulanda-loot-capture <dir>` (from L1): `01-beams` (white, green, blue and purple bodies side by side, day and night), `02-loot-window`, `03-compare-tooltip`, `04-upgrade-arrows`, `05-set-tooltip` (L2), `06-armoury` (L3), `07-sheet-doll`, `08-icons` (L5), `09-cloaks` (L6).

---

## 9. Build order

After each step: full EditMode and PlayMode run in the validation copy, playable build, commit, `tools\Backup.ps1`. Steps A1-A3 run now, beside the professions build; L steps follow section 6.2.

| # | Step | Contents | Acceptance check | What Chris sees |
|---|---|---|---|---|
| A1 | **Weapons and shields in hand** (about 3 d) | `GearLooks`, `looks.json`, `GearMeshes`, `GearMats`, `ActorVisual.Gear.cs`, `ActorVisual.GearWeapons.cs`, `GearGlow`, `GearBinder`, the `classKit` edit, wardrobe shots 01-03 | `GearLookTests` and the weapon half of `GearVisualTests` green; equip, unequip, F9 load and swim all show the right thing; Chris's save loads unchanged (format 7 or 8) | Equip a hatchet and hold an axe; a lantern hangs lit from the off hand; a rare blade has a glowing rune; shots of every weapon and shield |
| A2 | **Armour on the body** (about 3 d) | `ActorVisual.GearArmor.cs`, base-body replacement, Druid handling, wardrobe shots 04-12 | All of `GearVisualTests` green; full kit under 70 parts; no change to enemies or villagers | Helm, shoulders, chest, gloves, legs, boots and neck piece all show; a level-13 rare kit looks nothing like a level-2 common one |
| A3 | **The loot ledger, drafted** (about 2 d) | `Loot.cs` (parse, roll, effects, judge), six data files in `EncounterContent/Loot/`, `LootDataTests`, the roll tests, wardrobe shots 13-18 | Data and roll tests green against the unregistered files; `ItemTests` untouched and green; nothing in the running game changes | The line-up of all 104 named items on mannequins, to approve or change before any of it drops |
| L1 | **Loot feel** (about 2.25 d) | Roll at death, beacons, loot window, differences and upgrade arrows, call-outs; loot capture 01-04 | `LootWindowTests` and `LootJudgeTests` green; with full bags the item is still on the body after closing the window | A blue beam on a body; a loot window; "+3 weapon damage" in green on a tooltip |
| L2 | **Named loot live** (about 2 d) | Move the six files into `Items/`, run the scene builder, `LoadLoot`, drops, uniques by inventory, generated epics, effects and sets, tooltip lines, vendor pieces; back up the save first | Kill Old Whitefoot twice and hold both his pieces; Caddock gives crown, cleaver or coat, never the same one twice until all three are owned; two set pieces show "+30 health" on the sheet; Ama Rusk sells the Market-Day Breeches; drop-rate test green | Every elite drops its own named gear; the first purple item; a set bonus |
| L3 | **The Armoury** (about 1.5 d) | Save format 9, `ArmouryLog`, the tab, pity, "new look" toasts; back up the save first | Chris's save migrates to 9 with what he holds marked found and nothing else changed; the 10th dry Caddock kill gives the sabre in a test | A fifth tab in the quest book: "Oakhaven 4 / 25", silhouettes to hunt |
| L4 | **Quest rewards** (about 0.5 d) | `rewards.bagItems` on 15 quests | Turning in "Good Iron" gives Brannoc's Good Iron; with full bags the turn-in is refused and nothing is lost | Quests give gear |
| L5 | **Paper doll, icons, try-on** (about 2 d) | `GearDoll`, `EncounterHud.Doll.cs`, the `DrawCharacter` and `ItemSquare` edits; loot capture 07-08 | Doll test green (scene lighting restored); icons cached, no frame-time spike on opening the bags | A turnable 3D figure on the character sheet; real icons in the bags; Ctrl+click tries a piece on |
| L6 | **Back slot and cloaks** (about 1.5 d; owner decision 2) | `Items.cs` slot and `Pieces` row, sheet layout, `back.cloak`, 5-10 named cloaks added to the ledger; back up the save first | Old saves gain an empty tenth slot; `ItemTests` loot expectations updated for ten slots | Cloaks |

---

## 10. Open questions for the owner

1. **Empty slots show empty?** Default: **yes.** A character with no main-hand item holds nothing and has no shoulder pads; a new Warrior is unarmed until the Trailblade. The alternative keeps the class's default sword and shield in empty hands, which hides the point of the feature.
2. **Cloaks as a tenth "back" slot?** Default: **yes, as the last step (L6).** It edits `Items.cs`, which the professions engineer owns until then, and it changes the random gear roll from nine slots to ten. Until then the Mantle shoulder family is the nearest thing.
3. **Boss trophies become "one you do not own yet" instead of 100% every kill?** Default: **yes.** Caddock's crown, Greyheart's stave, Ninebranch's tine and the Root-Warden's crown join each boss's signature list; the first kills still pay out, and they can no longer be farmed in stacks. The alternative keeps them at 100% beside the new pieces.

---

## 11. What was checked, what was corrected, what was not verified

Checked in the code and data:
- `ZoneSceneBuilder.RegisterQuests` adds every `.json` under `EncounterContent/Items` to `itemFiles`; `ItemDatabase.Parse` merges several files.
- `EncounterSession.Player`, `Progress` and `Items` are public; the class is `sealed`, not `partial`.
- `LootCamp` adds drops straight to the bags and destroys what does not fit; camp ids are `mob.<tag>.<zoneShort>.<campIndex>.<n>`; `EncounterEnemy.Respawn` clears `Looted`.
- All 12 elite mob names, their levels and respawn times, and every camp tag and level used in the item list, against the five zone files. Hesk respawns in 15 minutes, like Caddock and the Root-Warden.
- The 15 quest ids exist. The five vendor NPCs are merchants, and a `VendorDef` by NPC name stacks on the role's stock.
- `ModifierOp` has `Flat` and `PercentAdd`; `StatType` has `MaxHealth`, `MaxPower`, `AttackPower`, `Armor`.
- `ZoneMeshes.Cone`, `Arc`, `Tube`, `Blob` and `Box` are public statics. There is no `Resources` folder yet.
- `ActorVisual.Build` and `Features` keep no references to hair, pads or base body parts, so `ActorVisual.cs` needs the small edit in 5.2.
- The quest book has four tabs at 130 px. The professions addendum gives `EncounterHud*.cs` (all partials) to engineer B.
- "The Tin Crown" quest counts kills, not the crown; `ItemTests` rolls only wolf and boar tables.

Corrected from the panel designs:
- The item list has 104 items and 7 epics (not 103 and 8), and 15 quest rewards (not 17).
- "Full bags refuse the search" is not current behaviour; the loot window is what fixes lost drops.
- The curve level is derived from the required level (`level + 1`) instead of a second `dropLevel` field.
- Vendor pieces use ordinary `vendors` entries; the proposed `shelves` section and its code are not needed.
- One look grammar and one family list are shared by the look file and the loot files.

Not verified:
- Nothing was run. The power numbers were computed outside Unity with the same float formulas; the curve test compares against `Generate` in the editor.
- That JsonUtility keeps field-initialiser defaults (`levelMin = 1`, `weight = 1`) for absent fields in nested array elements. The existing `LootTableDef` relies on the same behaviour.
- `EncounterCapture`'s dispatch beyond the three existing flags, and whether EditMode tests can build a full session for the binder test (it may need to be PlayMode).
- Mob tuning against a player in full boss rares and set bonuses.
- Whether the hunting and skinning work will edit `ActorVisual.cs` or `EncounterEnemy.cs`; there is no build plan for it in `tools/wip/professions` yet.
- Palette colours, recipe sizes and day estimates are first guesses, to be tuned from the wardrobe shots.
