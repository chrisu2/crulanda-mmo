# The action bar: ten slots, chosen (design, not built)

## Chris's brief (2026-10-13 ~14:55, verbatim)

"we should limit to 10 bar slots and they have to choose what spells/abilities at max level they can earn alternate advancement point to
expand the slots"

## Today

Each class kit has a fixed bar of 11 to 13 abilities in a fixed order (`BarIds`), keys 1-0, -, =, [ (`EncounterInput.SlotKey`).

## Proposed shape (to confirm with Chris)

- **Ten slots,** keys 1-0. A spellbook (every ability the class has unlocked by level and talents) from which the player puts any ten on
  the bar; a new character gets a sensible ten for the build they lead with, and the bar changes only when they change it.
- **Alternate advancement (AA):** at the level cap, experience keeps coming and fills AA points instead of levels; one point buys one more
  bar slot (keys -, =, [ and then ]), up to four more.
- **Under the hood:** the kits keep their whole list of abilities (the spellbook); the bar maps ten (or more) slots onto it, so the kits,
  their tests and the balance harness work as they do. The bar's choice and the AA points are saved per character: a save-format change,
  so Chris's save is backed up first and old saves get the default ten.
- **The group heals** asked for at ~14:25 ("yes give a slower group heal") go in as spellbook abilities: the Paladin's Mending Oath
  (GAME-ONLY), the Salt-Mender's Seal the Wound (CANON-EXPANDED, Mother Vane's "I seal the Wound!"), the Cantor's Mending Verse
  (CANON-EXPANDED, the Song of Mending): slow casts that mend the whole party in reach, slotted by choice.
