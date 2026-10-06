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

## Phase 5.2b (2026-10-06): inviting sims
Click a sim (its figure, or a party member) to focus it: its frame shows name, class, level and folk with Invite (or Leave
party). EncounterSession.Invite accepts unless the party is full (MaxPartySims 3), you are fighting, the level gap is over 5
("too far apart") or the sim is busy (friendly under .15). On joining, its figure is replaced by a party actor (SimCompanion,
its sim id as its EntityId) with health 150/115 + 22 a level (melee/ranged): it follows a pace behind in its own place, fights
your target when a fight is on (or whatever is on the party): Warriors and Paladins in melee every 2 s (a Warrior's blows add
threat), Rangers arrows, Mages fire and Druids thorns from 20 m every 2.4 s (Bolt.cs), and Druids and Paladins heal the most hurt
under 60% every 6 s. It falls when beaten and gets up at 40% once the fight is over; Recover heals the party. Party rows under
Mira's frame with a Leave button; plates say "· party". The party is not saved: on load the sims are back in the world.

## Phase 5.2 round 2 (2026-10-06): gear, who, group scaling
SimGear.For(sim, items): generated ids (ItemDatabase.GearId) at the sim's level, each slot's seed (gearSeed + slot*101 + k*7)
tried until the piece name is the class's weight (heavy: Coif/Torc/Spaulders/Hauberk/Gauntlets/Greaves/Sabatons; leather:
Cap/Cord/Mantle/Jerkin/Gloves/Breeches/Boots; cloth: Hood/Pendant/Mantle/Tunic/Wraps/Leggings/Shoes). Weapons for Warrior and
Paladin only. Not stored: derived on sight. The who list (EncounterHud.Who, O) lists the online. EncounterEnemy.GroupScale: each
party sim adds clamp(simLevel/mobLevel, .25, 1.25) shares; health x(1+.6 shares), damage x(1+.15 shares).

## Phase 5.3a (2026-10-06): their day
SimFigure (Scripts/Encounter/SimFigure.cs) carries an Actor (sim id as EntityId) and Combatant. Choose scores: hunt .35+.6 bold
(a camp mob of level -2..+1 within 160 m, health over 60%), gather .3+.5(1-bold) (a ready node within 140 m), inn 1.2 when under
60% health else .12+.2 chatty, loiter .25+.45 chatty, each plus up to .3 chance. Hunt: 2-4 mobs; melee 2.4 m every 2 s, ranged 18 m
every 2.4 s, damage as a party sim; breaks off under 30% (non-healers); healers heal every 8 s under 50%. Fallen: up after 12 s at
35%, then the inn. Gather: 1-3 nodes, 4-6 s kneeling each (EncounterSession.SimGathered rests the node). Inn: hidden 60-120 s,
healing. EncounterEnemy.TappedBy (first hitter) decides credit; FightingParty separates your fights from theirs.

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
