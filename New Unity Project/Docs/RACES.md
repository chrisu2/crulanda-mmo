# Races (draft, 2026-10-12, note: Shaman is orc-only)

Status: research only. No race code exists yet.

## Canon check (D:\code\crulanda)
| People | Canon status | Source |
|---|---|---|
| Humans (Lowtown, High City, Concord) | CANON | every book; world_bible.md |
| Orcs: green, tusked, ~7 ft; Gorm ("Orc Brute", "Orc-blood"), Krunk; "my people" use deep-cavern root-sap remedies | CANON (as a people, with little culture written down) | world_bible.md l.34; book1/world_bible.md l.24; Book1 ch.1-6 |
| Goblins, the "Weavers of the Warrens" (Vibration-Locking, Static-Charting, the Resonance Purge); Snivels, Razzle, Rill | CANON | world_bible.md l.37, l.86-87 |
| Dwarves (Captain Horgus) | CANON (only seen once) | Book1 l.1904 |
| Ancient non-human wood-and-moss beings of the Western Shore | CANON (not playable) | world_bible.md l.69 |
| An orc homeland, orc religion, or orc shamans | NOT IN CANON: orc shamanism would be **CANON-EXPANDED**, linked to Gorm's "ancient references" and the root-sap remedies, and needs Chris's sign-off | � |

The canon has no machines, so an orc Shaman should be spirit/root/earth themed, matching the Weave and Root-Mother.

## In the game today
- **There is no race.** The only thing stored is `EncounterProgress.classId` (default `class.warrior`), plus a sim's `classId`/`variant`. `ActorArchetypeDefinition` says "class/race data layers on later".
- Looks come from `ActorLook` (ActorVisual.cs) and the class colour. The player and the sims all use the same human ModelFigure rig.
- Goblins show up only as NPCs/enemies (the Adit cages, Nix, the Weaver), not as a playable race.

## What adding an orc race needs
1. **Data:** a `raceId` on `EncounterProgress` and on the sim record. Old saves default to `race.human`. Add a race definition (id, name, canonStatus, allowed classes, look). The Shaman's allowed list is orc only.
2. **Character creation:** pick the race, then filter the classes by it. `StartClassOverride` needs a race counterpart for tests.
3. **Figure:** an orc body on the same humanoid rig: bigger scale (~1.15), a wider frame, tusks/jaw, green skin. Gear fitting (GearArmor), FootTests/Grounding and the hoe need re-checking at the new scale.
4. **Sims:** a race mix in SimPopulation; orc sims are the only ones who roll Shaman.
5. **UI:** race on the character sheet and in the party frames.
6. **Tests:** saves default to human; a non-orc can't pick Shaman; the orc figure passes Foot/Grounding.
7. **Lore:** a short orc page (CANON-EXPANDED) for Chris to approve before any content is written.

## Status 2026-10-12 23:25 (code only, not compiled yet)
- Races.cs: Human and Orc. Orc classes (Chris, 23:31): Warrior, Ranger, Rogue, Mage and Shaman. No Druid. Shaman is orc-only; Cleric is human-only for now.
- EncounterProgress.raceId defaults to human, and a loaded save is normalised to its class (Races.For). FreshProgress takes a race. SimAdventurer.raceId plus a Race property.
- EditMode RaceTests (4 tests).
- Not done yet: the orc figure, the race pick at character creation, the sim race mix, and UI.
