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
  (GAME-ONLY), the Salt-Mender's Mending Cry (CANON-EXPANDED, the Mender-Craft cried aloud), the Cantor's Mending Verse
  (CANON-EXPANDED, the Song of Mending): slow casts that mend the whole party in reach, slotted by choice.

## Chris's answers (2026-10-13 ~15:00)

- Alternate advancement: **up to four more slots, 14 in all** (keys -, =, [ and ] after 1-0).
- Each extra slot costs **1, 2, 3, then 4** AA points (one point fills like a level's worth of experience past the cap).
- The Druid's four forms sit on **a small bar of their own** and do not use up the ten.

## Fly-out slots (Chris, 2026-10-15 01:25: "keep this in mind when designing hotbar and ui.")
From his picture (a slot with a small arrow at its edge; hovering or holding it opens a row of three related abilities beside the bar,
each with its charge count; the one last used sits on the slot's face): a hotbar slot may hold a *family* of actions and open into them.
Families this game has: the Scoundrel's verses, the Ranger's traps, the potions and foods, the Mage's wards, mounts or waystones later.
The idea only, not the art: no borrowed icons or frames (GAME-ONLY UI). Rules for the pass that builds it: the face shows the last-used
member and its cooldown; the row opens on hover after a quarter second or on a held press, closes when the pointer leaves; keys 1-0 still
fire the face; the row's members can be dragged onto their own slots as today. Record where the family comes from in the data (an
ability's `family` field) rather than hand lists.
