# SimAdventurer design

## Implemented prototype
One healer (provisional name Mira) can be recruited, follow, heal with cast time/resource costs,
contribute damage, die and recover. Her stable identity, health, mana, recruited flag and a small
relationship score persist in EncounterProgress. Player and healer use the same ability timing runtime.
This is a companion proof of concept, not the Phase 5 population simulator.

## Phase 5.2 round 1 (2026-10-06): the roster
SimAdventurer (Scripts/Encounter/SimAdventurers.cs): id (sim.01-sim.20), name (GAME-ONLY, never from the books), folk (an
origin label, PROVISIONAL until the canon's races are checked), classId (one of the five kits), level with a home zone to match
(1-5 Oakhaven, 4-8 Khaven, 7-10 Peaks, 9-12 Rim, 11-13 Shore), variant and gear seed, personality (bold, friendly, chatty: 0-1),
online hours by the world clock (some all hours, some mornings, most evenings), and where they were last seen (zone, x, z).
SimRoster.Generate(seed) makes the twenty, the same for a seed. Saved in the world slot (SaveFileStore slot "world", payload
CrulandaWorld, format 1), shared by every character, created with the first character and written with each autosave.
SimPopulation (in the session) keeps a SimFigure for each sim in the player's zone who is online: the class's look (no gear
yet), a NavMesh agent, standing about the zone's named places and wandering between them; the friendly turn to face you when
you stand close. Their nameplates show the name in the class's colour with the class and level beneath. Round 2 brings gear
on them and a who list; 5.3 their activities; 5.4 the unloaded zones; 5.5 chat and memory.

## Next extraction
Introduce a plain persistent SimAdventurer profile keyed by EntityId, distinct from its Actor.
Move companion state out of the scenario DTO behind a versioned save migration. Preserve existing IDs.
Then add activity/personality data, bounded social-event memory and utility-based goal selection.
Loaded actors use NavMesh; unloaded profiles use coarse zone/node simulation without scene objects.
Start with a few distinct profiles before increasing toward the 20-Sim MVP.

## Not implemented
Independent leveling/equipment, simulated schedules, offscreen travel/progression, guilds,
friend/rival networks, group leadership, varied personalities, or a simulated chat system.
No external language-model service is required for shipped behavior.
