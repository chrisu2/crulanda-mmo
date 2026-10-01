# Owner's request for loot (2026-10-01)

Verbatim: "time to improve armor and weapons and each has a different visual appearance when worn. so we need to start building
a database of loot and start catering to the people who love loot"

Read back to the owner: every piece of gear shows on the character (weapons, shields, helms, shoulders, chest, gloves, legs,
boots, cloaks, each with its own built shape and colours); a real loot database (named items by zone and boss, rarity tiers,
sets, rare world drops, uniques with their own look, on top of the generated gear; the blacksmith's crafted gear draws on the same
looks); loot that feels like loot (rarity colours and glow on drops, better-or-worse comparison, things worth hunting for).

Sequencing: designed in parallel with the professions build (`tools/wip/professions`); the two share the item code (Items.cs,
items.json, the save, the HUD's item panels), so they land one after the other, and the loot design prefers new files.
Documents: `DESIGN.md` and `ITEMS_V1.md` (from the `loot-design` workflow).
