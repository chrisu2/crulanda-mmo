# The Bestiary monsters: what the Imp and the Puglin are in Crulanda

Chris, play note 233 (verbatim): "in downloads use this for some different monsters. Bestiary - Dungeon Monsters Kit[Standard]".

## Decided (2026-10-18)
Chris chose from the options below, in his words (question prompt, verbatim):
- **Imp:** "Ember-bound (Recommended)"
- **Puglin:** "Bank-snouts (Recommended)"

What was placed:
- **The Ember-bound, in the Sealed Adit (Ember Vent, level 11).** Two chained pairs on the Imp in its red skin:
  - "Chained in the garden": the Ash Garden, beside the gatehouse menders.
  - "Chained at the lake of fire": the Chapel of Embers, on the walk opposite the lake-tenders.
  - In a fight they come one at a time (solitary). Their move is the Chain-Lash, a 2 s root.
  - They take the place of the plan's stand-in "ember elementals", which were never in a camp, so no quest, loot or deed named them.
  - They come after every other camp but the rare (the Quiet Miner stays last).
  - They are written by `tools/wip/dungeon/adit_layout.py` and copied into `Zones/adit.json`.
  - Labels: GAME-ONLY creature; CANON cult and tethers (book1 ch.10, ch.12:19).
- **The Bank-snouts, in the Lowtowns (levels 15-19).** Four small packs on the Chemical River's banks, on the Puglin, with green, red and blue mixed in each pack:
  - across from the quays: 3 snouts, levels 15-16;
  - on the east mud: 4 snouts, levels 16-17;
  - between the bridges: 3 snouts, levels 17-18;
  - on the north bank: 3 snouts, levels 18-19.
  - In a fight they pack, bolt at a fifth of their health, and their move is the Tusk Butt (a small knockback).
  - They stand off the water, the walkways and the quays, before the Lantern-Eater.
  - Labels: GAME-ONLY creature; CANON-EXPANDED from the river's "and worse" (book1 ch.15:391).
- **Lore:** each has a line in its zone's chapter of the book of lore.
- **Loot:** they drop from the shared tables. No loot table of their own was needed.

**Before 2026-10-18 this was a PROPOSAL.** The options as they were put to Chris follow.

## What is in the game now
- **The kit:** Quaternius, Bestiary - Dungeon Monsters Kit (Standard, the free version). QAL licence: free to use in the game, no credit needed, the assets can't be resold.
- **The two monsters:** the free version has the Imp and the Puglin.
  - The Imp is wiry and horned, with glowing eyes, chains round its body, a spiked collar, ragged shorts and a spiked mace.
  - The Puglin is a small pot-bellied thing with a pig's snout, big ears and tusks, and it carries a stick.
- **Where the files are:** `Resources/Characters/Bestiary`. They import as Humanoid with their own avatars (`Editor/CharacterImport.cs`).
- **How they move:** they use the people's clips: idle, walk, run, three blows, two flinches and the death. The weapon is part of the model and stays in its hand.
- **Colourings (by variant):** three each, and the eyes glow.
  - Imp: red, green, blue-violet.
  - Puglin: green, red, blue.
- **Size:** the Imp is about 1.7 m and the Puglin about 1 m. An elite is about 15% bigger.
- **Camp data:** a camp can use them with `"look": "imp"` or `"look": "puglin"` (EncounterSession.LookFor).

## What the books give us (read first)
- **No imps or pig-monsters.** The books have none. "Demon" only appears as a figure of speech (b1 ch.7:89, ch.22:179). So both monsters are **GAME-ONLY**, hung on CANON things.
- **Chains and collars (CANON):**
  - The Indentured are "shackled together by thick, glowing chains" (b1 ch.12:19).
  - Their iron collars glow a sickly blue (b1 ch.12:25).
  - A collar can make a dead man stand "like a broken marionette" (b1 ch.12:37).
- **The Cult of Ash and the Void (CANON):** bone-masked grey things in the cult's halls (b1 ch.10:45-55). The Void "hungers for the light" (b1 ch.10:55). The Void-Stalker is one of the "children of the Fracture" (b2 ch.2:31).
- **The Chemical River (CANON):** "Sludge-eels. Mutated crocodiles. And worse." (b1 ch.15:391). The eels and the croc are in the Lowtowns already, and "and worse" is still open.
- **Goblins are a people (CANON):** the goblins are the Weavers of the Warrens (world_bible.md:86-87). **The Puglin must never be called or played as a goblin.** It is a beast.
- **Names to avoid:** these are taken already: gutter-hound, dock-rat, sludge-hog, sewer swine, slag-fed swine, Deep-Rat, the Breaker, wired thrall, the Spent and Half-Bound.

## The Imp: options (recommended first)
1. **Ember-bound** (GAME-ONLY, on CANON roots: the Cult of Ash, b1 ch.10, and the glowing tethers and collars, b1 ch.12:19-25).
   - **What it is:** a fire-born thing the Cult of Ash keeps chained at its vent, its collar holding it to the cult's will.
   - **Where:** the Sealed Adit's Ember Vent (dungeon, 10-12), around Ysolt. It replaces the placeholder "ember elementals" (DUNGEON_DESIGN.md:375). A few could also go in the Ash Rim's Ash Pit (13-14).
   - **In a fight:** a melee bruiser with the mace. It hits hard and slowly, and lashes its chain to root you for a moment. It uses the red colouring and ember eyes.
2. **Pit-brute** (GAME-ONLY, on the CANON collars and the "seven years in the deep dark", b1 ch.12:133).
   - **What it is:** a chained brute the gaolers of the Deep Veins keep in the Hole and set on whoever comes down. The Breaker (a man, unchanged) holds their chains.
   - **Where:** Deep Veins, the Hole (dungeon, 23-25), as packs of two around the Breaker.
   - **In a fight:** it charges at the healer when its chain is slipped, and it fights harder while the Breaker stands. It uses the green or red colouring.
3. **Fracture-spawn** (PROVISIONAL, on the CANON "children of the Fracture", b2 ch.2:31).
   - **What it is:** a violet thing come up through the Grey Breach.
   - **Where:** the Sealed Adit's Grey Breach (dungeon, 12).
   - **In a fight:** fast, with weak blows. Its strikes drain mana. It uses the blue-violet colouring.

## The Puglin: options (recommended first)
1. **Bank-snouts: the "and worse" of the Chemical River** (GAME-ONLY, on CANON b1 ch.15:391).
   - **What it is:** small, mean scavengers that root in the river's black mud and steal from the docks.
   - **Where:** the Lowtowns (zone, 15-20), on the river banks and the Outfall, as packs of 3-5. The night camps are a little stronger.
   - **In a fight:** weak alone and dangerous in a pack. When one squeals the others come running (a call for help), and they flee at low health. The green and blue colourings fit the river mud.
2. **Tunnel-snouts** (GAME-ONLY).
   - **What it is:** a real body for the Sludge-Tunnels' "sewer swine", which are a tinted boar standing in for now (DUNGEON_DESIGN.md:1669). Old Tosh stays as the rare. The slag-fed ones with violet eyes take the blue colouring.
   - **Where:** the Sludge-Tunnels (dungeon, 18-20). Chris has not decided on that dungeon yet.
   - **In a fight:** packs that swarm and bite. The slag-fed ones spit for poison damage.
3. **Ash-snouts** (GAME-ONLY, a nod to the boar-skull helms of the Ash-Walkers, b1 ch.20:187).
   - **What it is:** a scavenger pack picking over the burnt farms.
   - **Where:** the Ash Rim (zone, 12-13), for example around the Last Orchard or Cinderfold. This fills the gap for a small pest pack at the low levels.
   - **In a fight:** easy packs for the levelling road. They use the red colouring.

**Not used for either monster:** the Deep-Rats stay rats (CANON, b1 ch.9:85), and the Pale Things are never killable (DUNGEON_DESIGN.md:3319).

## To check in the game before any camp uses them
- **Not seen in the game yet.** This is built, not verified: Unity has not been run on them.
- **Captures:** take front, back and side captures of both monsters walking, swinging and dying. Check that they face forward, that their feet are on the ground and that the mace and stick stay in the hand.
- **Fingers:** the people's clips open and close the fingers, so check that the grip on the mace and stick reads right.
