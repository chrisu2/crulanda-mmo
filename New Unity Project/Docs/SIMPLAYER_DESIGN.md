# SimAdventurer design

## Implemented prototype
One healer (provisional name Mira) can be recruited, follow, heal with cast time/resource costs,
contribute damage, die and recover. Her stable identity, health, mana, recruited flag and a small
relationship score persist in EncounterProgress. Player and healer use the same ability timing runtime.
This is a companion proof of concept, not the Phase 5 population simulator.

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
