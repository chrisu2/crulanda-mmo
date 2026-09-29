# Gameplay Tags

Source of truth: `Assets/Crulanda/Scripts/Core/Tags/GameTags.cs`. Keep this list in sync.

Format: dotted PascalCase path. Segments start uppercase, then letters/digits. Matching is hierarchical:
a set holding `Damage.Fire` answers true to `Has(Damage)` and `Has(Damage.Fire)`.

| Group | Tags |
|---|---|
| Ability | Attack, Spell, Heal, Utility, CrowdControl |
| State | Combat, Dead, Stunned, Silenced, Rooted, Casting |
| Damage | Physical, Fire, Frost, Nature, Shadow, Holy |
| Class (PROVISIONAL) | Warrior, Cleric, Ranger, Mage |
| Role | Tank, Healer, Damage, Support |

Rules: never rename a released tag; add via `GameTags`; `DefinitionBase.OnValidate` and Crulanda > Validate Content
warn on unknown tags. Class tags are provisional until the Crulanda class roster is approved (brief sections 66-67).
Runtime state tags with stacking (e.g. two stuns) need a counted container; that arrives with status effects (Phase 1).
