# Cleric and Shaman: design draft (note 178)

Chris, 19:18: "still need other healers to test. paladin, cleric (new class) , shaman (new class), salt mender".
This takes the class count from 8 to 10. Draft only; nothing is VERIFIED without Chris.

## Canon check (D:\code\crulanda, read-only, 2026-10-12)
- I searched the manuscripts, chapters and `world_bible.md` for "priest", "cleric", "shaman", "totem", "chaplain", "vicar", "temple" and "shrine".
- There are **no priests, clerics or shamans** in canon.
- The nearest canon is the **High Concord (formerly the High Council)** with its sunburst seal (world_bible, chapter 6), and a **library-temple** where the Preservationists and Archivists guard the history of the First Era (world_bible line 85).
- So both classes are **GAME-ONLY**. Their names and mechanics are provisional, in the same way as the Mage, Archivist and Salt-Mender.
- No machines, in lore or kit: both draw on faith, voice, weather, earth and the dead.

## Cleric (GAME-ONLY): a lay-sister or lay-brother of a hill shrine, outside the Concord
Resource is **Grace** (mana-like). Each build has a talent tree of 16+ ranks and the same 4-tier shape as the existing trees.

| build | role | loop |
|---|---|---|
| Lantern | healer (single) | Mend, Long Mend (cast), Vigil (HoT). Grace refunds on overheal-free heals. |
| Choir | healer (group) | Litany (heals around you), Hymn channel, Sanctuary circle. |
| Censure | dps | Rebuke bolt, Brand (DoT), Sunburst finisher on Branded foes. |
| Warden | support | Wards (barriers), Absolve (cleanse), Steadfast (damage reduction aura). |

## Shaman (GAME-ONLY): a weather-speaker of the high moors who calls on rain, stone and the old dead
Resource is **Breath** (mana-like). Stones stand in for totems: standing stones raised from the ground, not built.

| build | role | loop |
|---|---|---|
| Rainspeaker | healer | Rain Call (HoT field), Tide Mend (cast), Chain Mend (bounces 3). |
| Stormcaller | dps (ranged) | Squall bolt, Thunderhead (charge-up), Hailfall (AoE). |
| Stonebound | tank | Earthskin, Rooting Stamp (taunt and slow), Cairn (absorb). |
| Gravewind | control | Moor Wind (knockback), Mist (blind), Barrow-call (fear). |

## Work (35-min chunks, after the current one)
1. Content: class definitions, ability definitions and icons. Talent trees go in the calculator JSON shape.
2. Kits: `ClericKit.cs` and `ShamanKit.cs` on the ClassKit base. Then sim AI (SimRoster and SimCompanion casting), the class figure (`ActorVisual` outfit), and the balance harness classes list going from 8 to 10.
3. Tests: kit smoke, talents of 16+ ranks, the role caps (healers at 50% or less of the DPS median), and the party heal test.
4. Frames of both figures.

## Party heal test (queued; note 178 part 1)
- A 90 s party fight: Mira plus 3 sims taking spread and spike damage.
- Every healer build runs it: Paladin sanctuary, Salt-Mender mender, Archivist cantor, Druid rootmend, and later Cleric Lantern and Choir and Shaman Rainspeaker.
- Report HPS, overheal % and mana used and left.
