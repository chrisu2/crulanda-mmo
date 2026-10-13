# The Scoundrel: design draft (2026-10-13, not built)

## Chris's brief (verbatim)

"check the canon lore for a bard type class"
"ok next class can be a scoundrel (assassin type rogue with charisma for songs/merchants/wheeling and dealing/ he should have a high
single target dps spec/ a cc spec for cc group of mobs/ and a support spec"

## Canon roots (checked 2026-10-13 in D:\code\crulanda, read-only)

| Root | Where | Label |
|---|---|---|
| "Street-level scoundrel": the canon's own word for Valen (world_bible.md, the Rogue) | world_bible.md | CANON |
| Valen's "Song of Sifting": with no hands left, he sings a jagged verse from the Salt-Wall that sets the glass-archers' salt arrows on the Void-Touchers | book 3 ch. 4 | CANON |
| The Glass Coast's "Glimmer": the sound of trade, laughter and scoundrel's wit | book 3 ch. 11 | CANON |
| Matriarch Vala's Command Voice: one word, "Stay.", freezes a foe | book 1 ch. 18 | CANON (Vala's, borrowed) |
| No bard, minstrel or skald order exists in the canon | | the class's order is CANON-EXPANDED |

**The class: CANON-EXPANDED.** A blade and a voice: the scoundrel's second life once the hands are not enough, as Valen's was.
Chris, 02:36, on Valen as the root: "yes good example".

**Kept apart from:** the Rogue (Valen's Thief's Grace, the dirks; Shardwork, Thief's Grace, Swashbuckler, Locksmith), the Archivist (the
Silent Pilgrims, the Original Song, the Song of Mending), the Salt-Mender (Mother Vane's chant, the Mender-Craft). No machines.

## Shape (PROPOSED; names PROVISIONAL)

A dirk in one hand and a voice. **Nerve** (a pool of 100) is built by blade strikes and spent on verses and finishers.
**Charm, outside a fight (every path):** buys 10% cheaper and sells 10% dearer at merchants, haggles once a day per merchant, and hears an
innkeeper's rumour (a hint toward a quest or a rare mob).

| Path | Role | Target at level 15 | What it does |
|---|---|---|---|
| **Cutthroat** | DPS, single target | 62-72, at the top of it; x1.25 or less against three | The assassin: a quick cut as filler, a debt marked on one foe and collected as one hard blow ("the debt comes due"), poisoned wine (damage over time), a finisher that spends Nerve |
| **Silver Tongue** | CC of groups | 45-52 | The Song of Sifting and the Glimmer: a verse that dazes up to four foes near the target (damage breaks it); "Stay.", one word that holds one foe (Vala's Command Voice); a dissonant note that interrupts a cast and slows those near |
| **Fixer** | Support | 45-52 | Wheeling and dealing in a fight: a war-verse (the party does more), a rallying verse (the party's pools refill faster), a bribe (a non-elite humanlike foe stops fighting for a while), calling in a favour (an ally's longest cooldown comes back sooner) |
| (a fourth?) | | | Every class has four builds; the brief names three. Chris to say: a fourth, or three |

## Build plan (when Chris says go)

Same steps as the Cleric and Shaman, with what Oct 13 taught:
- Content: class, 12 abilities, unlocks **gated by path talents** (the Cleric's and Shaman's bars locked nothing, so every build cast
  everything); talents JSON, 4 paths x 17 ranks; ScoundrelKit.cs; icons in make_icons.py; sim roster; save slot.
- Charm: a merchant price hook and a rumour line at innkeepers.
- Balance harness: the toggled verses on the harness's once-a-fight list (and left alone when already up); the group daze kept off the
  dummies; Cutthroat not on the area list.
- Tests: kit smoke, talents, the role targets, the group daze (holds four, damage breaks it), the merchant prices.

## Open questions for Chris

1. The name: "Scoundrel" (the canon's own word) or another?
2. A fourth path, or three?
3. Which races may be one?
4. Voice only, or an instrument too? (The canon has no instruments in anyone's hands; Valen sang.)
