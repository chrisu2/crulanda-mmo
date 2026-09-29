# PROJECT MASTER BRIEF — Unity 6 Single-Player MMO / Simulated MMORPG

> **Purpose:** This file is the persistent source of truth for Claude while designing and implementing this game.
>
> **Engine:** Unity 6.x (C#)  
> **Primary language:** C#  
> **Development style:** Modular, data-driven, maintainable, source-controlled, testable, AI-friendly  
> **Core inspiration:** The *feeling* of classic MMORPGs, especially Erenshor-style simulated players and WoW Classic-style world/class progression — **without copying copyrighted names, maps, characters, quests, lore, art, dialogue, code, or assets**.

---

# 1. ROLE FOR CLAUDE

You are the lead gameplay programmer, systems architect, technical designer, and code reviewer for this project.

Your job is to help build a **fully playable single-player fantasy MMORPG simulation in Unity** that feels like playing on a living old-school MMO server.

You must:

1. Design systems before implementing them.
2. Keep architecture modular.
3. Prefer reusable Unity systems over one-off hacks.
4. Use C# for core systems and Prefab/Inspector exposure for content/design iteration.
5. Keep gameplay data in ScriptableObjects / data assets / CSV import whenever practical.
6. Avoid giant monolithic classes.
7. Produce compile-ready Unity C# whenever possible.
8. Clearly identify any Unity Editor setup steps that must be performed manually.
9. Never pretend a system is finished if only scaffolding exists.
10. Preserve existing working code unless there is a concrete reason to refactor it.
11. Before modifying multiple files, explain what will change and why.
12. After implementation, provide a verification checklist.
13. Track TODOs and known limitations.
14. Optimize for a solo/small-team developer.
15. Do not introduce online-service complexity until the offline game loop is proven.

When uncertain about an Unity API version or engine behavior, say so and verify before basing major architecture on an assumption.

---

# 2. HIGH-LEVEL VISION

Create a fantasy RPG that **feels like logging into a populated classic MMORPG server**, even though the game can be played entirely offline.

The world should contain:

- the human player
- towns
- wilderness
- dangerous zones
- dungeons
- rare enemies
- world bosses
- quests
- factions
- vendors
- trainers
- crafting
- gathering
- banks
- auction-like economy simulation
- guilds
- groups
- raids later in development
- dozens to eventually hundreds of simulated adventurers

These simulated adventurers should:

- exist persistently
- level independently
- acquire equipment
- travel through the world
- form groups
- enter dungeons
- die and recover
- buy and sell items
- pursue objectives
- develop relationships
- remember the player
- invite the player to groups
- ask to join the player's group
- join guilds
- develop reputations
- have class/role preferences
- have recognizable personalities

The player should feel:

> "This is a real old-school MMO server, except the other players are simulated and I can play at my own pace."

---

# 3. CORE DESIGN PILLARS

## Pillar 1 — A Living MMO World

The world should not revolve exclusively around the player.

SimPlayers should appear to be:

- questing
- grinding
- traveling
- crafting
- grouping
- selling
- dying
- upgrading equipment
- progressing

Some activity may be simulated statistically while actors are unloaded.

The world must feel alive without requiring every NPC or SimPlayer to be physically spawned at all times.

---

## Pillar 2 — Classic MMORPG Progression

Progress should feel meaningful and comparatively slow.

Avoid:

- showering the player with disposable loot
- constant gear replacement
- meaningless level scaling
- enemies automatically matching player level
- excessive handholding
- giant quest-marker chains that turn exploration into waypoint following

Prefer:

- recognizable gear upgrades
- dangerous enemies
- meaningful travel
- memorable zones
- named rare enemies
- rare drops
- dungeon loot
- class trainers
- spell/ability progression
- faction reputation
- professions
- secrets
- exploration rewards

---

## Pillar 3 — Strong Class Identity

Classes should play differently.

The initial target roster is:

1. Warrior
2. Paladin
3. Ranger
4. Rogue
5. Cleric
6. Druid
7. Shaman
8. Mage
9. Necromancer
10. Warlock
11. Bard
12. Monk

Not every class needs to ship in Prototype 1.

Prototype classes should be:

- Warrior
- Cleric
- Ranger
- Mage

These give us:

- Tank
- Healer
- Physical ranged DPS / pet potential
- Caster DPS

Each class must eventually have:

- unique resource mechanics where appropriate
- spell/ability progression
- class-specific utility
- gear preferences
- group roles
- solo strengths/weaknesses
- talent/specialization choices
- recognizable gameplay identity

Do not homogenize classes.

---

# 4. COMBAT PHILOSOPHY

Combat should mix:

- the readability and responsiveness of classic WoW-style tab-target combat
- the danger and tactical pacing of older MMORPGs
- modern responsiveness and controls

Target combat cadence:

- deliberate rather than twitch-heavy
- global cooldown based where appropriate
- auto attack for weapon classes
- cast times
- interrupts
- crowd control
- buffs
- debuffs
- damage-over-time
- healing-over-time
- threat/aggro
- taunting
- positioning
- fleeing
- pulling
- adds
- patrols

## Important

Do **not** turn combat into:

- pure action combat
- Souls-like dodge rolling
- giant AOE spam
- screen-filling particle chaos

Visual effects must remain readable.

---

# 5. TARGETING

Initial targeting system:

- Tab target
- Click target
- Assist target
- Target nearest hostile
- Target nearest friendly
- Target party member
- Target-of-target
- Focus target later

Each targetable actor should expose:

- name
- level
- health
- power/resource
- disposition
- faction
- elite/rare/boss classification
- target state
- threat state when appropriate

---

# 6. ENEMY DIFFICULTY

Enemy level should matter.

Use a color/con system inspired by classic MMORPG conventions, but with original terminology/UI.

Example relative difficulty categories:

- Trivial
- Easy
- Even
- Tough
- Dangerous
- Deadly

Avoid universal world scaling.

If the player walks into a high-level region early, the enemies should be able to destroy them.

That danger is intentional.

---

# 7. WORLD STRUCTURE

Start with **distinct zones**, not one gigantic seamless continent.

Advantages:

- easier development
- easier optimization
- easier navigation AI
- easier save/load
- easier spawn simulation
- easier content iteration
- easier world-state simulation

Example prototype world:

## Zone 1 — Greenhaven Valley

Level range: 1–8

Contains:

- starter town
- forest
- farm
- river
- bandit camp
- ruined watchtower
- cave mini-dungeon
- cemetery
- class trainers
- profession trainers
- bank
- inn
- vendors

## Zone 2 — Ashwood

Level range: 6–14

Contains:

- dense forest
- dangerous wildlife
- corruption storyline
- ruined settlement
- rare enemies
- dungeon entrance

## Zone 3 — Blackstone Depths

Level range: 10–16

First full group dungeon.

These are working names only.

---

# 8. PLAYER PROGRESSION

Prototype maximum level:

**Level 10**

Vertical slice target:

**Level 20**

Initial commercial target:

**Level 40–50**

Do not attempt level 60+ worth of content during early development.

Each level should feel meaningful.

Possible progression rewards:

- base stats
- new abilities
- improved ability ranks
- talent points
- profession access
- new gear
- new zones
- dungeon access
- faction unlocks

---

# 9. ATTRIBUTES

Initial primary attributes:

- Strength
- Agility
- Intellect
- Spirit
- Stamina

Derived attributes may include:

- Attack Power
- Spell Power
- Armor
- Critical Chance
- Accuracy
- Dodge
- Block
- Parry
- Haste
- Mana Regeneration
- Health Regeneration
- Resistance

Do not hardcode every formula directly into actors.

Combat calculations should live in dedicated gameplay systems.

---

# 10. ABILITY / EFFECT SYSTEM

Build a custom, modular, data-driven ability framework in C#.

It should support:

- active abilities
- passive abilities
- buffs
- debuffs
- damage
- healing
- attributes/stats
- cooldowns
- cast times
- resource costs
- status effects
- crowd control
- gameplay tags
- proc triggers
- targeting rules

Static definitions should be ScriptableObjects.

Runtime ability state should be plain C# or lightweight controller objects where possible.

Avoid hardcoding class logic into the player controller.

Suggested tag hierarchy:

```text
Ability.Attack
Ability.Spell
Ability.Heal
Ability.Utility
Ability.CrowdControl

State.Combat
State.Dead
State.Stunned
State.Silenced
State.Rooted
State.Casting

Damage.Physical
Damage.Fire
Damage.Frost
Damage.Nature
Damage.Shadow
Damage.Holy

Class.Warrior
Class.Cleric
Class.Ranger
Class.Mage
```

Tags can be implemented as stable string IDs, generated strongly typed IDs, enums for closed sets, or ScriptableObject tags. Prefer a solution that is safe, inspectable, and serializable.

# 11. THREAT / AGGRO SYSTEM

Threat must be a real mechanic.

Enemies should maintain threat tables.

Sources of threat:

- damage
- healing
- taunts
- buffs if appropriate
- proximity
- scripted encounter mechanics

Enemies should:

- switch targets based on threat
- respect taunts
- potentially flee
- call nearby allies
- leash appropriately

Threat must be inspectable during development.

Create debugging tools for threat tables.

---

# 12. GROUP SYSTEM

Standard group size:

**5 characters**

Possible composition:

- Tank
- Healer
- 3 Damage/Support

Groups may contain:

- player + SimPlayers
- all SimPlayers
- later: optional human multiplayer

Required group functionality:

- invite
- accept/decline
- leave
- kick
- leader
- role
- party frames
- loot rules
- group chat simulation
- assist
- shared kill credit
- quest kill credit rules

---

# 13. SIMULATED PLAYERS — MOST IMPORTANT FEATURE

This is the game's signature system.

Call them internally:

**SimAdventurers**

Do not make them dependent on an external LLM.

Core decision-making should run locally using:

- state machines
- utility AI
- behavior trees
- spatial queries where useful
- weighted personality parameters
- world-state simulation

External generative AI may someday enhance dialogue during development, but the shipped game must not require paid API calls.

---

# 14. SIMADVENTURER DATA MODEL

Each persistent SimAdventurer should contain data such as:

```text
UniqueID
Name
Race
Class
Level
Experience
CurrentZone
ApproximateLocation
HomeLocation
CurrentActivity
CurrentGoal
Guild
Group
Inventory
EquippedItems
Gold
Professions
KnownAbilities
QuestState
FactionReputations
PlayerRelationship
PersonalityProfile
PreferredActivities
PreferredRoles
Friends
Rivals
RecentEvents
DeathCount
DungeonExperience
BossKnowledge
OnlineStateSimulation
```

Do not serialize raw UnityEngine.Object pointers as persistent identity.

Use stable IDs.

---

# 15. SIMADVENTURER PERSONALITY

Personality dimensions may include:

- Friendly
- Rude
- Patient
- Impatient
- Skilled
- Reckless
- Cautious
- Greedy
- Generous
- Social
- Quiet
- Explorer
- Grinder
- Crafter
- Dungeon-focused
- Loot-driven
- Completionist
- Hardcore
- Casual

These characteristics influence behavior.

Examples:

A greedy character may:

- roll Need more aggressively
- chase rare spawns
- prioritize upgrades

A social character may:

- form groups frequently
- invite the player
- remain in groups longer

A cautious healer may:

- conserve mana
- warn against dangerous pulls
- leave reckless groups

---

# 16. SIMULATED ONLINE/OFFLINE STATE

The game should create the illusion of a server that continues to exist.

When a SimAdventurer is not physically loaded:

simulate progress statistically.

Examples:

- traveled from town to dungeon
- completed several encounters
- acquired XP
- found an item
- died
- returned to town
- sold loot
- crafted
- joined a group

Do NOT simulate every attack when offscreen.

Use coarse simulation.

When the SimAdventurer enters the loaded player's area:

materialize its current persistent state into an actor.

This architecture is critical.

---

# 17. SIMADVENTURER ACTIVITY STATES

Possible high-level activities:

```text
Idle
Socializing
Traveling
Questing
Grinding
Gathering
Crafting
Shopping
Banking
LookingForGroup
Grouping
DungeonRunning
RareHunting
Recovering
Dead
ReturningToCorpse
Training
Exploring
GuildActivity
```

Design activity selection using Utility AI.

---

# 18. SOCIAL MEMORY

SimAdventurers should remember meaningful interactions.

Examples:

- player revived them
- player abandoned group
- player gave them an item
- player won valuable loot
- player saved group from wipe
- player repeatedly caused wipes
- player grouped with them often
- player healed them
- player killed an allied faction

Relationships should gradually evolve.

Possible states:

- Stranger
- Familiar
- Friendly
- Friend
- Trusted
- Rival
- Disliked
- Hostile

Do not simulate unlimited conversation history.

Store compact event summaries / numerical relationship effects.

---

# 19. SIMULATED CHAT

Chat should help sell the illusion of an MMO.

Channels:

- Say
- Party
- Guild
- Zone
- Whisper
- System

Use authored / templated message pools based on:

- personality
- activity
- combat event
- loot event
- player relationship
- location

Examples of event categories:

- greeting
- LFG
- congratulations
- rare spotted
- wipe reaction
- low mana warning
- loot excitement
- player praise
- frustration
- goodbye

Do not use real-player toxicity as the primary joke.

The simulation should feel charming and believable.

---

# 20. QUEST DESIGN

Blend:

- straightforward classic questing
- environmental discovery
- faction-based quests
- class quests
- dungeon quests
- rare/hidden quests

Quest types may include:

- kill
- gather
- interact
- escort
- investigate
- deliver
- defend
- boss
- dungeon
- chain
- exploration

Avoid turning every quest into identical checklist busywork.

Quest text and lore should be original.

---

# 21. QUEST DISCOVERY

Difficulty options may determine how much guidance the player receives.

Possible modes:

### Guided
- map markers
- quest area hints
- clearer directions

### Classic
- limited map assistance
- quest text contains directions

### Explorer
- minimal markers
- relies heavily on dialogue and landmarks

---

# 22. LOOT PHILOSOPHY

Loot must feel exciting.

Rarity tiers:

- Common
- Uncommon
- Rare
- Epic
- Legendary

Names can later be changed.

Loot sources:

- ordinary mobs
- named mobs
- rare spawns
- quests
- dungeon bosses
- crafting
- faction vendors
- treasure chests
- world bosses

Rare items should remain useful long enough to matter.

Avoid replacing gear every fifteen minutes.

---

# 23. NAMED / RARE ENEMIES

The world should contain named enemies with:

- spawn regions
- respawn timers
- rare loot tables
- special abilities
- recognizable appearance
- lore

Some players may intentionally camp them.

SimAdventurers may also hunt them.

This should create MMO-like competition and surprise.

---

# 24. ITEM SYSTEM

Items should be data-driven.

Suggested item data:

```text
ItemID
DisplayName
Description
ItemType
Subtype
Quality
RequiredLevel
ItemLevel
BindRule
StackSize
VendorValue
Icon
Mesh
Stats
Effects
AllowedClasses
AllowedRaces
EquipSlot
Durability
LootTags
SetID
CraftingTags
```

Use Primary ScriptableObjects or another scalable Unity-friendly registry.

Avoid giant switch statements.

---

# 25. INVENTORY

Initial inventory features:

- character bags
- stackable items
- equipment
- bank
- shared account bank later
- drag/drop
- split stacks
- destroy
- sell
- buy
- equip
- compare

Equipment slots:

- Head
- Neck
- Shoulders
- Back
- Chest
- Wrists
- Hands
- Waist
- Legs
- Feet
- Ring 1
- Ring 2
- Trinket 1
- Trinket 2
- Main Hand
- Off Hand
- Ranged

---

# 26. LOOT RULES

Party loot prototype:

- Need
- Greed
- Pass

SimAdventurers should make reasonable decisions based on:

- whether item is usable
- whether it is an upgrade
- personality
- loot preference
- relationship

They should not cheat.

---

# 27. ECONOMY

Prototype economy includes:

- NPC vendors
- buying
- selling
- gold
- repair costs later
- profession materials

Later:

- simulated auction house
- SimAdventurer listings
- price ranges
- supply/demand abstraction
- rare item economy

Avoid trying to build a mathematically perfect economy during Prototype 1.

---

# 28. PROFESSIONS

Long-term profession examples:

Gathering:

- Mining
- Herbalism
- Skinning
- Fishing

Crafting:

- Blacksmithing
- Alchemy
- Leatherworking
- Tailoring
- Enchanting
- Engineering
- Cooking

Prototype:

- Mining
- Blacksmithing

---

# 29. FACTIONS

Factions should matter.

Possible reputation levels:

- Hated
- Hostile
- Unfriendly
- Neutral
- Friendly
- Honored
- Revered
- Exalted

Faction consequences can include:

- NPC hostility
- vendor prices
- quests
- trainers
- items
- safe areas
- alternate alliances

Attacking certain NPCs may damage reputation.

Actions should have consequences.

---

# 30. DEATH

Death should matter without becoming miserable.

Prototype approach:

- player becomes spirit/ghost or enters death state
- respawn at bind point or recover body depending on chosen design
- modest temporary penalty
- durability penalty later

Difficulty settings may modify punishment.

Avoid permanent character deletion as default gameplay.

---

# 31. DUNGEONS

Dungeons should feel like places, not linear amusement-park tunnels.

Include:

- multiple routes where appropriate
- patrols
- dangerous pulls
- named enemies
- optional bosses
- secrets
- rare drops
- dungeon quests
- faction connections

Prototype dungeon:

**Blackstone Depths**
- 30–45 minute initial target
- 3 bosses
- optional named rare
- several trash archetypes
- one simple mechanic per boss
- complete loot table

Working name only.

---

# 32. RAIDING

Do not implement raids during the first prototype.

Architecture should not make raids impossible later.

Long-term:

- 10-player raids initially
- player + SimAdventurers
- mechanics requiring roles
- progression loot
- lockout system if desirable

---

# 33. CLASS EXAMPLE — RANGER

Desired fantasy:

- ranged physical damage
- tracking
- pet companion
- traps
- wilderness utility

Potential mechanics:

- bow/crossbow
- ammunition only if it improves gameplay
- pet commands
- Hunter's Mark-like concept with an original name
- traps
- movement utility
- nature resistance/support

Do not copy Warcraft spell names.

Prototype abilities might be:

- Quick Shot
- Mark Quarry
- Piercing Arrow
- Snare Trap
- Call Companion
- Mend Companion
- Wild Sprint

Names are placeholders and may be changed.

---

# 34. PET SYSTEM

Build pets as a reusable framework.

Possible pet users:

- Ranger
- Warlock
- Necromancer
- Shaman

Pet states:

- Follow
- Stay
- Passive
- Defensive
- Aggressive
- Attack Target

Pet data:

- owner
- level
- stats
- abilities
- loyalty/relationship if used
- threat
- target
- equipment where relevant

Avoid class-specific duplicated pet code.

---

# 35. UI TARGET

UI should be practical and readable.

Style:

- fantasy
- clean
- old-school MMO influence
- modern usability
- scalable

Required prototype UI:

- player frame
- target frame
- party frames
- action bars
- cast bar
- buffs/debuffs
- chat
- minimap
- quest tracker
- inventory
- character panel
- loot window
- vendor
- group interface

Support ultrawide resolutions.

Do not hardcode UI placement for 16:9 only.

---

# 36. CAMERA / CONTROLS

Third-person camera.

Controls:

- WASD
- mouse look
- left/right mouse interaction
- zoom
- autorun
- tab target
- configurable hotkeys
- action bar 1–=
- modifier bars later

Camera should allow a useful MMO zoom distance without making the player character tiny.

---

# 37. SAVE SYSTEM

Persistent state must be robust from the beginning.

Do NOT serialize the entire world blindly.

Use versioned structured save data.

Separate:

- account/world save
- character save
- SimAdventurer persistent state
- quest state
- world-state changes
- faction state
- discovered locations

Every persistent entity requires a stable ID.

Save format must support version migration.

---

# 38. TECHNICAL ARCHITECTURE

Suggested Unity assemblies/components:

```text
Core
Characters
Combat
Abilities
AI
SimPlayers
Items
Inventory
Quests
Factions
Groups
World
Persistence
UI
Professions
Economy
Debug
```

Exact assembly/system boundaries may change after architecture review.

Prefer interfaces/components to inheritance chains that become impossible to maintain.

---

# 39. IMPORTANT UNREAL CLASSES

Claude should propose a clean architecture involving concepts such as:

```text
GameService / persistent singleton service
Scene/World service
Player service
MonoBehaviour component
ScriptableObject
ScriptableObject database / CSV-backed import
UAbilityDefinition
StatusEffectDefinition
StatBlock/StatBlock
GameplayTags
Behavior Trees / Utility AI
spatial queries
state machines / Utility AI where useful
DOTS/ECS only if justified
```

Do not use an Unity feature merely because it exists.

Explain why each system is appropriate.

---

# 40. PERFORMANCE REQUIREMENTS

Target:

- 60 FPS minimum on a reasonable mid-range PC
- scalable graphics
- hundreds of logically active SimAdventurers
- only a subset physically spawned

Core optimization principle:

**Simulation is not the same thing as representation.**

A SimAdventurer on the other side of the world should normally be data, not a ticking skeletal mesh actor.

Avoid unnecessary Update.

Use:

- events
- timers
- significance systems
- distance-based activation
- actor pooling only when proven useful
- async work where safe
- coarse offscreen simulation

Profile before optimizing heavily.

---

# 41. ART DIRECTION

Desired look:

- stylized fantasy
- readable silhouettes
- colorful but grounded
- timeless rather than photorealistic
- moderate poly/detail
- good performance

Avoid:

- hyper-real humans
- extreme visual clutter
- generic gray Unity Marketplace look
- excessive bloom
- excessive particles

Original visual identity is required.

Marketplace assets may be used during development if properly licensed, but the final world should not look like an asset flip.

---

# 42. AUDIO

Eventually include:

- zone ambience
- town ambience
- dungeon ambience
- combat sounds
- spell sounds
- UI feedback
- positional world sounds
- music themes

Do not block Prototype 1 on full audio production.

---

# 43. DIFFICULTY OPTIONS

The player should eventually be able to alter:

- enemy health
- enemy damage
- XP rate
- loot rate
- death penalty
- quest guidance

Presets:

- Relaxed
- Standard
- Veteran
- Hardcore

This is an offline game, so accessibility and customization do not threaten a shared economy.

---

# 44. THINGS WE WILL NOT BUILD YET

Do NOT prematurely implement:

- 100-player networking
- real-money store
- cash shop
- battle pass
- blockchain/NFT systems
- voice chat
- PvP battlegrounds
- enormous raids
- guild housing
- player housing
- seamless world technology unless justified
- procedural generation everywhere
- LLM-powered runtime NPC dialogue
- dozens of crafting professions
- hundreds of quests

Scope discipline is mandatory.

---

# 45. OPTIONAL MULTIPLAYER — FUTURE

The game should be **single-player first**.

However, avoid architectural decisions that make small co-op impossible.

Possible future target:

- host + 1–3 friends
- player plus SimAdventurers

Do not build full MMO backend infrastructure until there is a compelling reason.

---

# 46. DEVELOPMENT PHASES

## Phase 0 — Project Foundation

Goal:

A clean Unity project that compiles and runs.

Deliver:

- source control ready
- folder structure
- assembly structure
- coding standards
- gameplay tags
- base data architecture
- logging categories
- debug utilities
- base character classes
- test map

---

## Phase 1 — Combat Sandbox

Goal:

One player can fight enemies.

Deliver:

- movement
- camera
- targeting
- health/resource
- auto attack
- abilities
- damage
- healing
- death
- threat
- enemy AI
- loot drops
- XP
- leveling

Playable test:

> Player kills enemies, gains XP, levels, receives loot, equips an upgrade.

---

## Phase 2 — First Class Loop

Classes:

- Warrior
- Cleric
- Ranger
- Mage

Deliver:

- class data
- 5–8 abilities each by level 10
- resource systems
- equipment restrictions
- class trainers
- basic talent framework

---

## Phase 3 — Inventory / Items / Vendors

Deliver:

- inventory
- bags
- equipment
- item comparison
- loot window
- gold
- vendor buy/sell
- data-driven items

Target:

At least 50 test items.

---

## Phase 4 — Questing

Deliver:

- quest database
- accept
- abandon
- objectives
- completion
- rewards
- quest chains
- kill credit
- item collection
- NPC interaction

Target:

10–15 polished prototype quests.

---

## Phase 5 — SimAdventurer MVP

THIS IS THE CRITICAL MILESTONE.

Deliver:

- persistent SimAdventurer profile
- spawn/materialize
- travel
- combat
- basic class rotation
- grouping
- follow
- assist
- loot
- XP
- leveling
- equipment upgrades
- personality variables
- simple social memory
- simulated chat

Target:

20 SimAdventurers exist in the prototype world.

Only nearby SimAdventurers need full actors.

---

## Phase 6 — Group Gameplay

Deliver:

- group invites
- roles
- party UI
- group AI
- tank logic
- healer logic
- DPS logic
- threat cooperation
- loot rolls

Playable test:

> Player can invite four SimAdventurers and reliably clear outdoor group content.

---

## Phase 7 — First Dungeon

Create one complete dungeon.

Target:

- 30–45 minutes
- 3 bosses
- rare spawn
- quests
- loot table
- patrols
- wipes/recovery
- SimAdventurer navigation

This proves the game concept.

---

## Phase 8 — First True Vertical Slice

Deliver:

- 2 outdoor zones
- 1 town
- 1 dungeon
- 4 classes
- level 1–10
- 20–40 SimAdventurers
- 50–100 items
- professions MVP
- factions MVP
- polished UI
- save/load
- audio pass
- art pass

Only after this works should scope expand.

---

# 47. FIRST PLAYABLE MILESTONE

The FIRST PLAYABLE is intentionally tiny.

We need:

- one small map
- one player class: Warrior
- one enemy archetype
- one friendly SimAdventurer: Cleric
- targeting
- auto attack
- three Warrior abilities
- two Cleric abilities
- threat
- healing
- death
- XP
- one item drop
- inventory
- equip item
- save/load

Scenario:

1. Player enters test area.
2. Player invites simulated Cleric.
3. Player targets wolf.
4. Player attacks.
5. Wolf uses threat system.
6. Cleric follows player and heals intelligently.
7. Enemy dies.
8. Player gains XP.
9. Enemy drops item.
10. Player loots item.
11. Player equips item.
12. Game saves.
13. Game reloads correctly.

**Do not expand scope until this works reliably.**

---

# 48. DEFINITION OF DONE

A feature is not "done" merely because code exists.

For every feature:

1. It compiles.
2. It can be tested in-game.
3. Expected behavior is documented.
4. Edge cases are considered.
5. Save/load impact is considered.
6. Debugging information exists where useful.
7. No known blocking errors remain.
8. Manual Unity Editor steps are documented.
9. Files changed are listed.
10. Test procedure is provided.

---

# 48A. UNITY 6 PROJECT STANDARDS

Use Unity 6 LTS/current supported Unity 6 release unless the project is already pinned to a specific Unity 6 editor version.

## Rendering

Default recommendation:

- **URP** for the initial project
- stylized fantasy visuals
- strong performance
- scalable PC hardware targets

Do not choose HDRP unless the visual target truly requires it and the hardware/performance cost is justified.

## Input

Use the Unity Input System package.

Support:

- keyboard/mouse
- rebindable controls
- gamepad later
- MMO-style action bars
- modifier keys

## Navigation

Use Unity NavMesh for physically present characters.

Do not require loaded NavMesh agents for offscreen world simulation.

Offscreen SimAdventurer travel should use abstract zone/node travel data.

## UI

Use a pragmatic combination of:

- UI Toolkit where it improves maintainability
- uGUI where it is more mature/practical for runtime MMO HUD workflows

Do not force one UI technology if it makes implementation harder.

## Assets and loading

Consider Addressables for:

- large content sets
- zone assets
- creatures
- equipment visuals
- asynchronous loading

Do not add Addressables until the project needs them.

## Save data

Do not serialize scene GameObjects directly.

Use versioned DTOs / save records such as:

```text
WorldSaveData
CharacterSaveData
SimAdventurerSaveData
QuestSaveData
InventorySaveData
FactionSaveData
```

Use stable GUID/string IDs to reconnect saves to ScriptableObject definitions.

## Testing

Use Unity Test Framework for:

- EditMode tests for pure game logic
- PlayMode tests for scene/runtime behavior

Pure systems such as:

- threat math
- XP curves
- loot selection
- stat formulas
- relationship scoring
- utility-AI scoring

should be testable without loading a full scene.

## Scene strategy

Start with small scenes:

```text
Bootstrap
CombatSandbox
StarterTown
StarterWilderness
Dungeon_01
```

Do not build a giant open world first.

## Source control

Use Git.

Ignore generated folders such as:

```text
Library/
Temp/
Obj/
Logs/
Build/
Builds/
UserSettings/
```

Use Git LFS for large binary assets where appropriate.

## AI-friendly project rule

Prefer text-visible configuration and data wherever reasonable so Claude can inspect and modify the project without requiring constant manual Inspector work.

Use:

- ScriptableObjects
- JSON/CSV import pipelines
- prefabs
- clear serialized fields
- editor tools
- deterministic IDs

Avoid burying critical gameplay rules only in scene objects or hand-wired Inspector state.

---

# 49. CODING RULES FOR CLAUDE

## Never

- dump thousands of lines without structure
- silently rewrite unrelated systems
- use magic numbers everywhere
- create circular dependencies
- make every object globally accessible
- rely excessively on Update
- put all game logic in PlayerCharacter.cs
- build giant Prefab/Inspectors containing core business logic
- duplicate systems per class
- fabricate APIs
- claim code compiled when it was not compiled
- delete working functionality just to simplify implementation

## Prefer

- components
- subsystems
- interfaces
- gameplay tags (implemented as typed IDs/enums/ScriptableObject tags)
- data assets
- delegates/events
- test maps
- debug commands
- reusable systems
- configuration values
- explicit dependencies

---

# 50. FILE CHANGE FORMAT

Before a major implementation, respond with:

```markdown
## Implementation Plan

### Goal
...

### Files to create
- ...

### Files to modify
- ...

### Architecture
...

### Risks
...
```

After implementation:

```markdown
## Completed

### Files created
- ...

### Files modified
- ...

### Editor steps
1. ...

### Test procedure
1. ...

### Known limitations
- ...

### Recommended next step
...
```

---

# 51. PROJECT MEMORY FILES

Maintain these project documents:

```text
/Docs/PROJECT_MASTER.md
/Docs/ARCHITECTURE.md
/Docs/ROADMAP.md
/Docs/GAMEPLAY_TAGS.md
/Docs/DATA_SCHEMA.md
/Docs/SAVE_FORMAT.md
/Docs/SIMPLAYER_DESIGN.md
/Docs/KNOWN_ISSUES.md
/Docs/CHANGELOG.md
```

Update them when architecture changes.

Never rely exclusively on chat history for project decisions.

---

# 52. ARCHITECTURE DECISION RECORDS

For significant technical choices, create short ADR entries.

Example:

```markdown
## ADR-001 — Ability/Effect System

Status: Accepted

Decision:
Use Unity Ability/Effect System for combat abilities, attributes,
buffs, debuffs and cooldowns.

Reason:
...

Alternatives:
...

Consequences:
...
```

This prevents future sessions from undoing previous decisions arbitrarily.

---

# 53. VERSION CONTROL

Use Git.

Recommended:

- Git LFS for large binary assets
- frequent commits
- feature branches where helpful
- descriptive commit messages

Never make an enormous destructive refactor without first creating a clean checkpoint.

---

# 54. DEBUG TOOLS

Build development tools early.

Useful console/debug commands:

```text
Sim.Spawn
Sim.List
Sim.Teleport
Sim.SetLevel
Sim.SetActivity
Sim.DumpState

Combat.ShowThreat
Combat.GodMode
Combat.Damage
Combat.Heal

Player.SetLevel
Player.GiveItem
Player.GiveGold

Quest.Start
Quest.Complete
Quest.Reset
```

Exact names can change.

Debugging visibility is a priority.

---

# 55. DATA-DRIVEN CONTENT PIPELINE

The game eventually needs hundreds or thousands of:

- items
- NPCs
- enemies
- abilities
- quests
- loot entries

Therefore content creation must scale.

Claude should design import/export tooling where useful, potentially using:

- CSV
- JSON
- data assets / CSV import
- Primary ScriptableObjects
- Unity Editor tools
- Python editor scripting where appropriate

Do not hand-code every sword in C#.

---

# 56. ORIGINALITY / IP RULE

We are inspired by the **design principles** of classic MMORPGs.

Do not copy:

- Warcraft names
- Warcraft races
- Warcraft maps
- Warcraft lore
- Warcraft quests
- Warcraft dialogue
- Warcraft art
- Warcraft spell names
- Warcraft sound
- Erenshor names
- Erenshor maps
- Erenshor lore
- Erenshor characters
- Erenshor dialogue
- Erenshor assets

We may learn from broad mechanics such as:

- tab targeting
- class roles
- threat
- questing
- professions
- factions
- dungeon groups
- simulated players
- rare monsters
- persistent progression

The final game must have its own identity.

---

# 57. INITIAL GAME IDENTITY

Working title:

**PROJECT WAYFARER**

This is temporary.

Tone:

- heroic fantasy
- mysterious
- occasionally humorous
- adventurous
- dangerous
- grounded enough for ordinary towns and people to matter

The player begins as an unknown adventurer rather than "the chosen one."

Power is earned.

The world should feel older and larger than the player.

---

# 58. PLAYER EXPERIENCE TARGET

A great 90-minute play session might look like:

1. Log into town.
2. Notice simulated players talking/grouping.
3. Check inventory and bank.
4. Visit trainer.
5. Pick up two quests.
6. Travel into wilderness.
7. Encounter familiar SimAdventurer.
8. Form a group.
9. Kill mobs.
10. Discover a rare enemy.
11. Rare nearly wipes group.
12. Obtain meaningful item.
13. Hear about a dungeon group.
14. Join.
15. Complete part of dungeon.
16. Return to town.
17. Sell loot.
18. Train new ability.
19. Save/log out feeling stronger than before.

That is the heart of the game.

---

# 59. WHAT MAKES THIS GAME SPECIAL

The central innovation is not simply:

> "single-player World of Warcraft"

It is:

> **A persistent fantasy MMORPG simulation where the population itself is part of the game.**

The player should develop history with simulated adventurers.

Examples:

- "That healer saved me back at level 4."
- "That rogue always steals good rolls."
- "I haven't seen that Paladin in a few levels."
- "That Mage finally joined my guild."
- "These three characters have become my regular dungeon crew."

Those emergent relationships are a major retention mechanic.

---

# 60. CLAUDE'S FIRST TASK

Do **not** immediately generate the entire game.

Start by doing the following:

## Step 1 — Architecture Review

Review this document and propose the minimum Unity 6 architecture needed for:

- character base
- attributes
- combat
- abilities
- threat
- AI
- SimAdventurer persistent profile
- items
- inventory
- save system

Keep it appropriate for a solo developer.

## Step 2 — Challenge Bad Assumptions

Identify:

- unnecessary complexity
- systems that should be delayed
- technical risks
- Unity-specific concerns
- anything likely to cause major refactors

Do not agree with the document merely because it is provided.

## Step 3 — Establish Repository Structure

Produce proposed:

```text
/Assets
/Assets/Crulanda/Scripts
/Assets/Crulanda/Art
/Assets/Crulanda/Prefabs
/Assets/Crulanda/ScriptableObjects
/Assets/Crulanda/Scenes
/Assets/Crulanda/UI
/Assets/Crulanda/Audio
/Assets/Crulanda/Editor
/Packages
/ProjectSettings
/Docs
```

structure.

## Step 4 — Produce Milestone 1 Plan

Create a Unity 6 implementation plan for the **FIRST PLAYABLE MILESTONE** described above.

Break work into small, testable tasks.

Each task should ideally produce something we can compile or test.

## Step 5 — Begin Only the Foundation

Implement Phase 0 first.

Do not jump ahead to dozens of classes, zones, or quests.

---

# 61. RESPONSE TO USE WHEN STARTING A NEW CLAUDE SESSION

When this file is first loaded, begin with:

> Read `PROJECT_MASTER.md` completely before changing code.
>
> Treat it as the project's current source of truth.
>
> Inspect the existing Unity project and documentation before proposing changes.
>
> Do not assume systems are missing until you verify the repository.
>
> First report:
>
> 1. current project state,
> 2. architecture you observe,
> 3. conflicts with PROJECT_MASTER.md,
> 4. highest-risk technical issue,
> 5. smallest useful next milestone.
>
> Then propose the implementation plan.
>
> Do not perform a broad rewrite unless there is a demonstrated architectural problem.

---

# 62. NORTH STAR

At every major design decision, ask:

> **Does this make the player feel like they are adventuring in a living classic MMORPG world populated by believable simulated players?**

If not, reconsider whether we need it.


---

# 63. CRULANDA CANON OVERRIDE — PRIMARY WORLD SOURCE

**This section supersedes any generic fantasy assumptions elsewhere in this document.**

The game is to be based on the author's original fantasy setting and novels:

- **The Land of Crulanda — Book 1**
- **Land of Crulanda Book 2 / The Ashen Veil — Canon Master**
- supporting Crulanda character, lore, and canon documents

These books are the **primary canon source** for:

- playable races
- cultures
- nations
- factions
- geography
- settlements
- religions
- historical events
- magic systems
- technology
- creatures
- monsters
- class fantasies
- professions
- major NPCs
- visual motifs
- naming conventions
- political conflicts
- ruins
- dungeons
- artifacts
- quest themes

The game is no longer intended to use a generic fantasy setting.

The old working title **PROJECT WAYFARER** may remain as an internal development codename, but the game world is **Crulanda**.

If a generic placeholder in this document conflicts with established Crulanda canon, **Crulanda canon wins**.

---

# 64. CANON SOURCE RULES FOR CLAUDE

Before finalizing worldbuilding content, Claude must consult the available Crulanda canon documents.

Create and maintain:

```text
/Docs/CRULANDA_CANON.md
/Docs/CRULANDA_RACES.md
/Docs/CRULANDA_CLASSES.md
/Docs/CRULANDA_FACTIONS.md
/Docs/CRULANDA_MAGIC.md
/Docs/CRULANDA_LOCATIONS.md
/Docs/CRULANDA_CREATURES.md
/Docs/CRULANDA_TIMELINE.md
```

Each entry should distinguish:

```text
CANON
Directly established in the novels or author-approved reference material.

CANON-EXPANDED
A game-system interpretation that logically expands existing canon.

GAME-ONLY
New material created specifically for gameplay.

PROVISIONAL
An idea that still requires author approval.
```

Claude must **never silently convert an invented gameplay idea into book canon**.

When adapting the novels into game systems, preserve the spirit, terminology, geography, cultures, and established rules of Crulanda.

---

# 65. PLAYABLE RACES — TARGET: UP TO 8

The game should support **up to 8 playable races**.

The final eight must be derived from or carefully expanded from the peoples and cultures established in the Land of Crulanda canon.

## Already confirmed canon peoples/cultural identities include

### Driftkin

Established characteristics include:

- storm-linked culture
- ritual traditions
- bone weapon imagery
- Shatterlings connection
- stormglass
- Windcallers
- strong clan structure
- spiritual relationship with the storm
- distinctive rites and beliefs

Driftkin gameplay possibilities may include:

- storm affinity
- weather resistance
- movement/travel traits
- spear traditions
- ritual magic
- Windcaller class affinity

Do not reduce Driftkin to a generic "orc" or "tribal race."

Their Crulanda identity must remain distinct.

### Forgeborn

Established characteristics include:

- engineering
- forging
- mechanical construction
- explosives
- machinery
- technological ingenuity
- Forgespire heritage
- connection to ancient forge traditions and the Ebon Crucible

Forgeborn gameplay possibilities may include:

- crafting bonuses
- engineering
- deployable mechanisms
- mechanical companions
- explosives
- forge-related class affinities

Do not reduce Forgeborn to generic dwarves or gnomes.

Their technology/forge identity is part of Crulanda's distinctiveness.

## Remaining six playable races

Do **not** invent six generic races merely to reach eight.

Claude must first perform a canon extraction from Book 1, Book 2, and supporting lore documents and identify:

1. Which peoples are explicitly distinct races.
2. Which are cultures/nations rather than biological races.
3. Which can reasonably become playable.
4. Whether an established culture should contain multiple racial origins.
5. Which additional playable races require author-approved canon expansion.

Produce a proposed eight-race matrix before implementation:

```text
Race
Canon Source
Homeland
Culture
Visual Identity
Starting Region
Racial History
Faction Relationships
Magic Affinity
Technology Affinity
Playable Classes
Racial Traits
Game Expansion Required?
```

Racial traits should provide flavor and useful advantages without creating one mathematically mandatory race for a class.

---

# 66. CLASS SYSTEM — TARGET: UP TO 15 DISTINCT CLASSES

The game should eventually support **up to 15 genuinely distinct playable classes**.

Classes must be based on Crulanda's established characters, magic, martial traditions, religions, technology, cultures, and factions.

Do not simply recreate WoW's class list and rename it.

Classic MMORPG class design may inspire **mechanical clarity**, but class fantasy must belong to Crulanda.

## Canon-derived class/archetype seeds already established in Crulanda material

The novels and character documents already establish strong foundations for:

### Warrior

Canon examples include Veyra Crimsonlash, Zygnar Redclaw, Korath Ironvine, and other martial characters.

Potential roles:

- Tank
- melee DPS

Possible identity:

- weapon mastery
- stances
- battlefield control
- guard/intercept
- threat generation
- dual-wield or heavy-weapon paths

---

### Knight

Canon examples include Kaelith Dawnstrike and Talira Frostveil.

Potential roles:

- Tank
- support
- melee DPS

Knight must be mechanically different from Warrior.

Potential themes:

- discipline
- defensive techniques
- oath/order mechanics
- sword mastery
- protective abilities
- factional martial traditions

Kaelith's stormlight connection is special lore and should not automatically mean every Knight receives his exact powers.

---

### Ranger

Lorien Meadowlight provides a canon basis for a wilderness-oriented class.

Potential roles:

- ranged DPS
- utility
- support

Potential mechanics:

- tracking
- bows
- wilderness survival
- nature interaction
- scouting
- traps
- animal companion possibilities where canon supports them

Lorien's unique ancient nature and powers must not automatically be given wholesale to every ordinary Ranger.

---

### Seer

Sylvara Moonpetal provides a strong canon foundation.

Potential roles:

- support
- control
- magical DPS
- information/foresight utility

Possible mechanics:

- visions
- fate/tapestry motifs
- predictive defenses
- order-based magic
- probability or omen mechanics
- enemy intent revelation
- short-duration future sight

The class must translate foresight into fun combat mechanics rather than passive exposition.

---

### Tinker

Klyther Forgeheart provides a core archetype.

Potential roles:

- ranged/mechanical DPS
- utility
- support

Possible mechanics:

- deployables
- mechanical constructs
- drones
- traps
- temporary turrets
- repair
- battlefield gadgets

Tinker should feel unmistakably Crulandan and Forgeborn-influenced.

---

### Engineer

Tynara Embercoil provides a related but potentially separate engineering archetype.

Potential themes:

- explosives
- cannons
- demolition
- volatile devices
- area denial

Claude must evaluate whether **Tinker and Engineer** have enough mechanical separation to justify two classes.

If not, merge them into one class with distinct specializations rather than padding the roster.

---

### Healer

Mirene Thistledown provides a canon basis for medicine/healing.

Potential roles:

- primary healer
- support

Possible themes:

- herbal medicine
- restorative magic
- cleanses
- preventative healing
- alchemical support

The final class name should be rooted in Crulanda's culture rather than necessarily remaining the generic term "Healer."

---

### Mystic

Feyra Glintspire provides a canon basis through crystal magic.

Potential roles:

- magical DPS
- support
- control

Potential mechanics:

- crystal resonance
- barriers
- refraction
- magical amplification
- detection
- battlefield control

---

### Mage

Torvyn Embercloak and Valthira Emberstrike establish fire magic practitioners.

Potential roles:

- ranged magical DPS
- control

Crulanda's Mage must derive its schools and limitations from the actual Crulanda magic system rather than automatically inheriting generic fire/frost/arcane MMO conventions.

---

### Assassin / Shadow Operative

Feylith Bonecarver and Zythera Sandveil establish stealth, daggers, espionage, and assassination concepts.

Potential roles:

- melee DPS
- scouting
- control

Claude must determine whether Spy and Assassin belong as:

- one class with specializations,
- separate classes,
- or class + profession/social specialization.

Avoid unnecessary roster duplication.

---

### Windcaller

Kyrza Stormwoven provides a strong canon basis.

Potential roles:

- ranged magical DPS
- support
- control

Potential mechanics:

- storm energy
- wind
- lightning
- ritual magic
- weather interaction
- stormglass
- mobility

Windcaller should be deeply connected to Driftkin lore.

Other races accessing the class, if allowed, needs lore justification.

---

### Venom / Alchemical Specialist

Xyra Flintgaze establishes venom crafting and trade knowledge.

This may become:

- a full combat class,
- an Assassin specialization,
- an Alchemist profession,
- or a hybrid support archetype.

Do not force it into the 15-class roster if it works better as a profession.

---

# 67. FINAL 15-CLASS DESIGN RULE

Claude must not choose fifteen classes simply because fifteen slots exist.

The correct process is:

1. Extract all canon combat/magic/profession archetypes.
2. Group overlapping archetypes.
3. Identify missing gameplay roles.
4. Use **canon-consistent expansions** to fill useful gaps.
5. Produce a proposed roster of no more than 15.
6. Explain why each class deserves to be distinct.
7. Identify its source in Crulanda lore.
8. Get author approval before treating the roster as locked.

Use this matrix:

```text
Class
Canon Basis
Primary Role
Secondary Role
Armor
Weapons
Resource
Combat Range
Core Mechanic
Group Utility
Solo Strength
Solo Weakness
Race Restrictions
Three Possible Specializations
Canon vs Game Expansion
```

A good class must have a gameplay loop that could be recognized even with all spell names hidden.

---

# 68. ROLE COVERAGE FOR THE 15-CLASS SYSTEM

The complete roster should provide multiple ways to fill each group role.

Aim approximately for:

### Tanks
3–5 classes capable of tanking through class or specialization design.

### Healers
3–5 classes capable of meaningful healing.

### Melee DPS
5+ options.

### Ranged Physical DPS
2–4 options.

### Ranged Magical DPS
4+ options.

### Support / Utility
Several classes should provide meaningful non-DPS contribution.

Do not make every class capable of every role.

Class identity matters more than perfect symmetry.

---

# 69. SPECIALIZATIONS

Each class may eventually support **up to 3 specializations or disciplines**.

This gives the game breadth without requiring 30–40 separate classes.

Example design principle:

```text
Tinker
 ├─ Construct specialist
 ├─ Demolition specialist
 └─ Battlefield support specialist
```

Specializations should alter gameplay rather than merely provide percentage bonuses.

Do not design all 45 possible specializations during the first prototype.

---

# 70. RACE / CLASS MATRIX

Not every race must necessarily play every class.

Race/class restrictions are encouraged when they reinforce lore.

Examples:

- culturally unique magic may initially belong to its originating people
- Driftkin should have a special relationship with Windcalling
- Forgeborn should have a strong relationship with Tinker/engineering traditions

However, restrictions should be based on **Crulanda lore**, not copied from another MMO.

For every restriction, document:

```text
Restriction
Lore reason
Gameplay consequence
Whether unlocks/exceptions may exist
```

---

# 71. CRULANDA'S CORE WORLD IDENTITY

The game must preserve the setting elements that already distinguish Crulanda.

Confirmed major motifs include:

- **Order versus Chaos**
- the **Veil**
- Veil fragments
- stormlight
- ancient bindings/anchors
- Lumora
- crystalline architecture
- quartzine
- the High Concord
- Ashfall / Red Wastes
- Sandthrone power structures
- the Shatterlings
- Driftkin
- Forgeborn
- Forgespire
- the Ebon Crucible
- stormglass
- ancient technologies
- faction conflict
- moral ambiguity
- competing interpretations of history and power

These should influence gameplay rather than remain background lore.

Examples:

- crafting materials should come from Crulanda's world
- spells should reflect its magic
- dungeon history should connect to its past
- architecture should communicate faction/culture
- gear silhouettes should reveal origin
- professions should use setting-specific technology/resources
- racial starting experiences should explain the world through play

---

# 72. GAME TIMELINE RELATIVE TO THE NOVELS

Before writing the main game story, Claude must propose where the game occurs relative to the novels.

Options may include:

### Before Book 1
Advantages:
- player can experience the world before major events
- famous characters can appear without being displaced by the player
- known future events create historical depth

### During the novels
Advantages:
- recognizable events and characters
- player experiences consequences from another viewpoint

Risk:
- player can become irrelevant beside book protagonists
- canonical outcomes restrict agency

### After the novels
Advantages:
- maximum freedom
- consequences of the books shape the world
- new factions/classes/races can emerge naturally

### Parallel timeline / adjacent story
Player operates in regions and conflicts not directly followed by the novels.

Claude should recommend a position based on:

- gameplay freedom
- spoilers
- canon stability
- future books
- ability to expand the world

**Do not overwrite novel protagonists by making the player secretly responsible for all of their achievements.**

The player deserves their own important story.

---

# 73. BOOK CHARACTERS AS GAME NPCs

Major book characters can appear as:

- faction leaders
- trainers
- quest givers
- legendary figures
- allies
- antagonists
- dungeon/story NPCs
- historical figures depending on timeline

Their abilities may inspire classes.

However:

**A named character is not automatically a template for an ordinary member of that class.**

Examples:

- Sylvara's unique relationship with the Veil may exceed what a normal Seer can do.
- Lorien may possess abilities far beyond an ordinary Ranger.
- Kaelith's stormlight condition should not automatically become a standard Knight feature.
- Kyrza may represent an unusually powerful Windcaller.

Preserve legendary characters as legendary.

---

# 74. CRULANDA STARTING EXPERIENCE

Instead of a generic starter valley for all characters, the long-term design should consider racial/cultural starting regions.

Each should teach:

- the race's culture
- local conflict
- basic combat
- the race's relationship to the world's factions
- local creatures
- racial history
- class trainers
- Crulanda's wider crisis

For the first prototype, build **one** starting area only.

Do not attempt eight starting zones simultaneously.

---

# 75. CONTENT EXTRACTION TASK FOR CLAUDE

Before large-scale world implementation, Claude should perform a structured lore extraction from the Land of Crulanda books.

Create:

```text
CRULANDA_CANON_DATABASE
```

with records for:

```text
People/Race
Culture
Faction
Location
Character
Creature
Magic
Artifact
Material
Religion
Historical Event
Profession
Weapon Tradition
Organization
Political Relationship
Class Candidate
Quest Seed
Dungeon Seed
```

Every record should include:

```text
Name
Type
Book/Source
Canon Summary
Known Relationships
Gameplay Possibilities
Unresolved Questions
Canon Expansion Risk
```

The goal is to turn the novels into a usable game-design database without damaging continuity.

---

# 76. UPDATED NORTH STAR

The project's North Star is now:

> **Create a single-player simulated MMORPG set authentically in the Land of Crulanda, combining the feeling of a living Erenshor-style simulated server with the clarity, progression, class identity, exploration, grouping, loot excitement, and world attachment associated with classic MMORPGs — while remaining unmistakably Crulanda.**

When deciding between:

- a generic MMO convention, and
- a mechanic that expresses Crulanda,

prefer the Crulanda solution whenever it remains fun and understandable.

The ultimate test is:

> **Could someone see the world, classes, races, creatures, UI terminology, equipment, magic, and zones and know this is Crulanda rather than a renamed version of another MMORPG?**

The answer must eventually be **yes**.

