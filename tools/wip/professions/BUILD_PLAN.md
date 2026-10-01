# Trades of Crulanda: the build plan (DESIGN.md + ADDENDUM.md)

Status: a plan only. Nothing here was built, run or tested. Section references are to `DESIGN.md` (D) and `ADDENDUM.md` (A) in this folder. All paths are under `D:\code\mmo\New Unity Project\Assets\Crulanda\` unless they start with `Docs\`.

**When the owner first sees what he asked for**
- Maud at her own leather shop, named houses, the new buildings: **step 1**.
- Mining a seam and selling the ore: **step 4**.
- A trade bag from the leatherworker, by quest or bought outright: **step 5**.
- Nettie carrying the loaf home and Fen the firewood, because you paid: **step 7**.
- Hunting deer and rabbits for hides: **step 11**.

**Two tracks.** Track A (the village) is steps 1, 3, 6, 7. Track B (professions) is steps 2, 4, 5, 8-14. One engineer per track can work at the same time; the "Parallel" line of each step says with what. Shared files to merge by hand, each a few lines: `ZoneBuilder.cs`, `ZoneDefinition.cs`, `EncounterSession.cs`, `WorldLife.cs` (`StockLine` is B's, everything else A's), `items.json`, and `oakhaven.json` (one engineer at a time).

**Every step, before and after**
- Before step 1: `git status` is clean (the painted-cave pass that last touched `ZoneBuilder.cs` and `ZoneDefinition.cs` is committed).
- Before step 2's first run: copy the owner's save folder to `...\hel\work\save-backups\<date>-pre-format-8`.
- Every new test sets `SaveDirectoryOverride`; every village test calls `VillageEconomy.ResetAll()` in teardown from step 7 on.
- After each step: sync to the validation copy, full EditMode and PlayMode run there, a playable build, update `Docs\CHANGELOG.md` and `Docs\CLAUDE_HANDOFF.md`, commit, `tools\Backup.ps1`.
- Always green: `VillageDayTests`, `VillageErrandTests`, `VillageDrinkTests`, `VillageWorkTests`, `OakhavenQuestTests`, `ZoneContentTests`, `SecretPlacementTests`, `EncounterLoopTests`, `ItemTests`, `SaveMigratorTests`.

---

## Step 1. The buildings (track A)
- **Goal:** every new building stands in Oakhaven, houses have names, and Maud works at her own shop.
- **Files:** `Scripts/World/ZoneBuilder.cs` (four `case` lines, the `legacy == 0` list, `Footprint`, `Openness`, the `Inn()` bar spot, rooms door and the two skipped back windows, `Smoke()` returns its particle system, the `DefinitionFilter` test hook); new `Scripts/World/ZoneBuilder.Workshops.cs`; `Scripts/World/ZoneDoor.cs` (`kind`, `smoke`); `Scripts/Encounter/WorldLife.cs` (`FindPlaces` keys, `placeName`, `Needs`, `PoseFor`); `Scripts/Encounter/VillageWork.cs` (the leatherworker's and herbalist's shifts only); `Tests/EditMode/VillageWorkTests.cs` (six place names added).
- **Data:** `Zones/oakhaven.json`: names on props 4-13; appended props Carder farmhouse, Crisp cottage, leather shop, drying hut, kitchen, game rack; three landmarks. `Zones/khaven.json`: house names.
- **Tests:** PlayMode `VillageStreamTests.NewVillageProps_LeaveTreesSceneryAndCreekUnmoved`; `VillageWorkshopTests.Every_workshop_stand_is_reachable`, `.The_leatherworker_keeps_shop_apart_from_the_tannery_yard`; EditMode `WorkshopDataTests.Kitchen_abuts_the_inn`, `.New_props_keep_clear_of_groves_and_the_creek`.
- **Acceptance:** no tree, rock or creek point moves; no grass inside the new floors; the kitchen covers no window; Maud stands at her counter 9-12 and 14-18.
- **The owner sees:** two new houses, the leather shop by the South road with Maud in it, the drying hut, the kitchen behind the Cask, the game rack at the lodge, "Knock · Tanner house" on doors.
- **Depends on:** nothing. **Parallel:** with step 2.

## Step 2. Save format 8, materials, tools, the Trades window (track B)
- **Goal:** the save and item groundwork for the whole feature, in one migration.
- **Files:** new `Scripts/Encounter/Professions.cs` (data classes, `ProfessionDatabase`, `ProfessionLog` skills and tools); `Items.cs` (`trade`, `teaches`, `pouch`); `EncounterProgress.cs` (`professions`, `pouches`); `EncounterSave.cs` (`FormatVersion = 8`, `AddProfessionsMigration` inserting both lists, guards); `EncounterContent.cs` and `Encounter.asset` (`professionFiles`); `EncounterSession.cs` (`LoadProfessions`, bind, tools and materials in `EquipFromBag`, key K); `EncounterInput.cs`; new `EncounterHud.Professions.cs` (skill column); `EncounterHud.cs`, `EncounterHud.Items.cs` (glyphs, tooltips).
- **Data:** `Items/items.json`: the 15 raw materials, charcoal, flour, salt, vials, two tools, vendor additions (D 7); new `Professions/professions.json` (professions and node types). `Docs\SAVE_FORMAT.md`, `Docs\DATA_SCHEMA.md`.
- **Tests:** EditMode `ProfessionDataTests.ProfessionsFile_Parses`; `ProfessionLogTests.Tool_TeachesSkill_AndIsConsumed`, `.SecondTool_IsKept`; `SaveMigratorTests.V7Payload_MigratesTo8_AddsProfessionsAndPouches_KeepsEveryOtherCharacter`, `.V8_RoundTrips`, `.SkillOutOfRange_RefusesSave_FileUnchanged`, `.Too_many_pouches_refuses_the_save_file_unchanged`; `ItemTests.SellJunk_KeepsMaterials`.
- **Acceptance:** the owner's real save loads as format 8 with empty `professions` and `pouches` and nothing else changed; a pick bought from Ama Rusk hangs at the belt; K opens the Trades window.
- **The owner sees:** picks, hatchets, flour, salt and vials at vendors; the Trades window with his skills.
- **Depends on:** nothing. **Parallel:** with step 1.

## Step 3. Households and homes (track A)
- **Goal:** everybody sleeps behind their own named door; knocking knows who lives there.
- **Files:** `Scripts/World/ZoneDefinition.cs` (`households`, `workshops`, `ZoneHousehold`, `ZoneMember`, `ZoneWorkshop`, `ZoneResident.works`); `ZoneBuilder.cs` (`BuildHomeDoors` for barn homes); `WorldLife.cs` (`Households`, `Init`, the derived fallback, `KnockLine`, `AtHome`, the hunter's bed hour, empty `Paid`); `VillageWork.cs` (the hunter's shifts); `EncounterSession.cs` (the knock).
- **Data:** `life.households` in `oakhaven.json` (15 households; the Golden Cask's comes with Hob in step 6) and `khaven.json` (5). `Docs\WORLD_ZONES.md`.
- **Tests:** EditMode `HouseholdDataTests.Oakhaven_every_villager_is_in_exactly_one_household`, `.Every_household_house_names_a_prop_and_no_house_is_shared`, `.Khaven_households_cover_its_six_villagers`, `.Zones_without_households_still_parse`. PlayMode `VillageHomeTests.Every_villager_sleeps_behind_their_own_named_door`, `.The_Tanners_share_one_house`, `.Farmers_live_with_the_hen_wife_at_their_farm`, `.The_hunter_is_hidden_by_2330_starting_from_the_inn`, `.Home_doors_are_on_the_navmesh`, `.Knocking_names_the_household`.
- **Acceptance:** at 23:30 every Oakhaven villager is behind their own door; Wil Carder and Hettie Brook walk to the new farmhouse, Aldo Crisp to the cottage, Garet Moss to the lodge.
- **The owner sees:** families going home together at dusk; knock answers such as "Maud's at the shop by the South road."
- **Depends on:** step 1. **Parallel:** with step 4.

## Step 4. Gathering in Oakhaven (track B)
- **Goal:** mine, chop and pick in Oakhaven, and sell what you gather to a village that notices.
- **Files:** `ZoneDefinition.cs` (`ZoneNode`, `ZoneProp.node`, `ZoneInteractable.node`); new `ZoneBuilder.Nodes.cs` (`BuildNodes`, `OreSeam`, `Windfall`, herb variants); `ZoneBuilder.cs` (the call after `BuildSecrets`); `Professions.cs` (gather logic); `EncounterSession.cs` (`TryGather`, `GatherNow`, work bar, respawn table, `SellBag` delivery); `EncounterHud.Professions.cs` (node guide); `WorldLife.cs` (`StockLine` cases).
- **Data:** `oakhaven.json` `nodes` (10 ore, 8 timber, 2 new herbs) and `node` on the 8 yarrow props.
- **Tests:** PlayMode `NodeStreamTests.TreesAndScenery_Unmoved` (Oakhaven), `NodePlacementTests.EveryNodeAndStation_IsReachable` (Oakhaven), `GatherTests.CopperSeam_WithPick_FillsBag_HidesAndReturns`, `.Respawn_SurvivesZoneReload`, `.Yarrow_GivesBagHerbAlways_AndQuestItemWhenWanted`, `.Moving_CancelsWork`, `VillageSupplyTests.SellingOre_DeliversForgeOre_AndVellRemarks`. EditMode `ProfessionLogTests` gather and skill-band tests (D 12); `ProfessionDataTests.EveryNodeAndRecipe_UsesKnownItems`.
- **Acceptance:** mine a Crowsfoot seam, see "Mining 2.", sell the ore, hear Vell remark on it; the yarrow quest still works.
- **The owner sees:** ore seams, windfall timber and herbs to work; skills rising; the smith talking about the ore.
- **Depends on:** step 2; step 1's `oakhaven.json` and `DefinitionFilter` merged first. **Parallel:** with step 3.

## Step 5. The leatherworker's bags (track B)
- **Goal:** wear a trade bag, got from Maud by quest or bought outright.
- **Files:** `Items.cs` (`holds`, `slots`, `Inventory` pouch rules); `Quests.cs` (`bring`, `rewards.bagItems`, `unlessWorn`); `EncounterSession.cs` (`Wear`, vendor list, `CompleteQuest`, the `Paid` call, the hide line); `EncounterHud.Items.cs` (bag rows, tooltips); `EncounterHud.Professions.cs` (the bag line).
- **Data:** `items.json`: four bags (12, 16, 20, 24 gold), the leatherworker vendor, the four existing hides become materials. `Quests/oakhaven.json`: `npc.leatherworker.wallet` (3 wolf pelts). `Docs\QUEST_DESIGN.md`.
- **Tests:** EditMode `PouchTests.Wearing_a_trade_bag_adds_its_slots_and_uses_up_the_item`, `.A_second_bag_of_a_kind_is_refused_and_kept`, `.Materials_fill_their_trade_bag_before_the_ordinary_slots`, `.Nothing_else_enters_a_trade_bag`, `.Count_Remove_and_Room_see_trade_bag_slots`, `.FreeSlots_counts_ordinary_slots_only`; `ItemTests.Hides_are_materials_and_SellJunk_keeps_them`; `QuestLogTests.Bring_hands_over_bag_items_on_talk`, `.Bring_without_enough_changes_nothing`, `.A_bag_quest_is_not_offered_once_the_bag_is_worn_or_carried`, `.TurnIn_with_the_bag_already_worn_pays_gold`, `.TurnIn_with_full_bags_is_refused_and_nothing_is_lost`; `SaveMigratorTests.Pouches_round_trip_with_their_slots`, `.An_unknown_pouch_is_kept_and_ignored`. PlayMode `TradeBagTests.The_leatherworker_sells_the_four_bags_and_a_worn_one_leaves_her_list`, `.Knocking_at_night_opens_her_wares` (once step 3 is in), `TradeBagUiTests.Capture`.
- **Acceptance:** bring Maud three wolf pelts, wear the wallet, pick yarrow and see it land in the wallet's row; buy the ore-poke for 24 gold; "Sell junk" keeps pelts; a sword is refused by a poke slot; save, quit, reload and the rows are there.
- **The owner sees:** bags hanging in Maud's shop that he can buy or earn, and new labelled rows in his bags.
- **Depends on:** steps 2 and 4; step 3 for the knock. **Parallel:** with step 6.

## Step 6. Workshop days and the innkeeper (track A)
- **Goal:** each trade works its own workshop, and the inn has Hob Linden and a working kitchen.
- **Files:** `WorldLife.cs` (`PlaceFor`, `Resolve`, `WorkedPlace` by owner, innkeeper title, hours and lines); `VillageWork.cs` (`Errand.door`, `Errand.toRole`, the innkeeper's day, "tanned hides from the yard", "leather to the stall" from the shop, "herbs to dry" to the hut); `Tests/EditMode/VillageWorkTests.cs` (innkeeper in `Trades`).
- **Data:** `oakhaven.json`: `life.workshops`, Hob Linden as the third resident, his household. `items.json`: the innkeeper vendor.
- **Tests:** EditMode `VillageWorkTests.The_innkeeper_has_a_day_from_first_light_to_the_last_table`; `WorkshopDataTests.Every_workshop_owner_is_a_villager`. PlayMode `VillageWorkshopTests.Each_trade_stands_at_its_own_workshop`, `.Eggs_loaves_and_hares_are_handed_over_at_the_kitchen`, `.The_innkeeper_is_abed_by_2330`.
- **Acceptance:** Ama, Tamsin and Hedda each keep their own stall; the hen-wives hand eggs over at the kitchen and Hob answers; Hob is at the bar by evening and abed by 23:30; `VillageErrandTests` still sees `inn.eggs`.
- **The owner sees:** an innkeeper behind the bar who sells bread and cheese, deliveries going round the back, Lisbet drying herbs at her hut.
- **Depends on:** step 3. **Parallel:** with step 5.

## Step 7. Purses and spending (track A)
- **Goal:** what the player pays the leatherworker, her family spends where you can see it; short of coin they go without and say so.
- **Files:** new `Scripts/Encounter/VillageEconomy.cs`; `VillageWork.cs` (`Errand.need`, `Shopping`); `WorldLife.cs` (economy, new-day check, `Paid`, claimed shopping errands, settle at pick-up, release on cancel, `PurseLine`, `ApplyHearth`); `EncounterSession.cs` (`Paid` from `Buy` and bag-quest turn-in).
- **Data:** `oakhaven.json`: `stipend: 3` on Tanner; `stipend: 2, needs: ["bread"]` on Jory.
- **Tests:** EditMode `VillageEconomyTests.With_no_player_only_the_Tanners_go_without`, `.The_Tanners_have_bread_daily_and_firewood_every_third_day`, `.One_worn_bag_keeps_the_Tanners_warm_every_day`, `.Needs_are_bought_in_order_and_stop_at_the_first_unaffordable`, `.Stipend_then_cap_keeps_every_purse_at_or_under_forty`, `.A_purchase_reaches_the_sellers_household`, `.A_household_never_buys_its_own_trade`, `.A_cancelled_errand_releases_its_claim`. PlayMode `VillagePurseTests.Buying_from_the_leatherworker_at_1500_sends_her_family_for_bread_and_firewood`, `.Short_of_coin_they_go_without_and_say_so`, `.A_house_without_firewood_goes_cold_and_the_rest_keep_smoking`, `.Drinkers_drink_with_every_purse_at_zero`.
- **Acceptance:** with no bag worn the Tanner chimney is cold at dusk two days in three and every other chimney smokes; buy a bag between 14:00 and 18:00 and within a game hour Nettie carries a loaf and Fen a bundle of firewood home; after 18:00 Maud says "That's tomorrow's fire."; with one bag worn they are warm every day, also after a restart.
- **The owner sees:** his gold turning into a loaf and firewood carried to the Tanner house, and a lit chimney.
- **Depends on:** steps 3, 5 and 6. **Parallel:** with step 8 or 9.

## Step 8. Nodes in the other four zones (track B)
- **Goal:** gathering tiers 2 to 5.
- **Files:** none new (data and the placement script `tools/wip/professions/place_nodes.py`).
- **Data:** `nodes` and herb `node` fields in `khaven.json`, `peaks.json`, `ashrim.json`, `verdant.json` (D 2.4). `Docs\WORLD_ZONES.md`.
- **Tests:** `ProfessionDataTests.EveryZone_HasTenOreEightTimberTenHerbNodes`; `NodeStreamTests` and `NodePlacementTests` for all five zones.
- **Acceptance:** a level-13 character can mine in Verdant at once ("hard going"); no tree or secret moves in any zone.
- **The owner sees:** seams, windfalls and herbs in every zone.
- **Depends on:** step 4. **Parallel:** with steps 7 and 9 (data only; a third hand can take it).

## Step 9. Stations and charcoal (track B)
- **Goal:** the recipe engine, the stations, and the first thing to make.
- **Files:** `Professions.cs` (`Craft`, `CanMake`, `DifficultyOf`); `ZoneDefinition.cs` (`ZoneStation`); `ZoneBuilder.Nodes.cs` (`BuildStations`, station props); `ZoneBuilder.cs` (`Workplace()` maps forge, oven, `dryhut` to bench, `kitchen` to fire; `Inn()` hearth); `EncounterSession.cs` (`StationNear`, `Make`, the station prompt); `EncounterHud.Professions.cs` (recipe pane, Make, Make all).
- **Data:** five charcoal recipes; `stations` in Khaven, Peaks, Ashrim and Verdant (D 6.1; Oakhaven needs none).
- **Tests:** `ProfessionLogTests.Craft_*`, `.CanMake_CountsAcrossSplitStacks`; `ProfessionDataTests.Recipes_NeverBeatTheirInputs`, `.Recipes_FromVendorGoods_NeverProfit`, `.EveryRecipeStation_ExistsSomewhere`; PlayMode `CraftStationTests.Charcoal_AtVellsSmithy_Works_TenMetresAway_Refused`, `.GoldenCaskHearth_CountsAsFire`, `.CaskKitchen_CountsAsFire`.
- **Acceptance:** chop a windfall and burn charcoal at Vell's forge at night with nobody there.
- **The owner sees:** "Work at the forge", a recipe list, charcoal in his bags.
- **Depends on:** steps 4 and 1. **Parallel:** with step 7.

## Step 10. Cooking (track B)
- **Goal:** Cooking for everyone at any fire.
- **Data:** ten recipes and foods (D 5.4); meat drops in the wolf, hound, mossboar and stag tables; boar meat becomes a material.
- **Files:** none beyond data, `ItemTests`.
- **Tests:** `ItemTests.BoarMeat_IsMaterial`; the economy tests of step 9 over the new recipes.
- **Acceptance:** cook boar stew at the Cask's kitchen range and eat it; "Sell junk" keeps the meat.
- **The owner sees:** cooking at the inn's kitchen and hearth; better food than the vendor's.
- **Depends on:** step 9. **Parallel:** with step 11 if a second engineer is free (both touch `items.json`).

## Step 11. Hunting for hides (track B)
- **Goal:** deer and rabbits are huntable, every beast gives its hide, and the last three bag quests open.
- **Files:** `EncounterEnemy.cs` (`Game`); `WorldLife.cs` (`CritterBody`, groups handed to the session); `EncounterSession.cs` (`Game` list, `SpawnGame`, targeting and corpse search over both lists, no XP or coin, "Skin the body").
- **Data:** `items.json`: `hide.coney`, `hide.hill_deer`, `hide.boar`, loot tables `rabbit` and `deer`, the boar entry, the skinner vendor. `Quests/oakhaven.json`: `npc.leatherworker.sling`, `.scrip`, `.poke`.
- **Tests:** PlayMode `HuntTests.A_deer_can_be_targeted_killed_and_skinned`, `.Game_bolts_and_sneaking_gets_closer`, `.Game_returns_after_its_respawn`, `.Chickens_sheep_and_cats_cannot_be_targeted`, `.Game_never_counts_as_an_enemy_for_the_village`; EditMode `ItemTests.Every_beast_table_has_a_hide`; `QuestDataTests.Bring_and_bag_rewards_name_real_items`.
- **Acceptance:** stalk and kill a deer by the Harrow wood, skin it, bring Maud three hides for the log-sling; hens, sheep and cats cannot be selected; villagers do not flee from a deer; "zone clear" is unaffected.
- **The owner sees:** animals he can hunt, hides in his bags, bags earned from them.
- **Depends on:** step 5. **Parallel:** with step 10 (see there). First task: read the targeting code (A G.2).

## Step 12. Blacksmithing and the two-craft rule (track B)
- **Goal:** the first craft, with take up, forget and the two-slot limit.
- **Files:** `Professions.cs` (`Learn`, `Forget`, slots); `EncounterSession.cs` (`LearnCraft`, `ForgetCraft`, `TradeNpcNear`, the 1 s speed-up); `EncounterHud.Professions.cs` (take up, forget, confirm).
- **Data:** five smelts and 21 pieces (D 5.2).
- **Tests:** `ProfessionLogTests.ThirdCraft_IsRefused`, `.Forget_FreesSlot_AndDropsSkill`, `.Cooking_CannotBeForgotten`, `.Craft_BelowSkill_IsRefused`; `ProfessionDataTests.CraftedGear_SitsOnTheGeneratedCurve`; `CraftStationTests.TakeUpBlacksmithing_AtTheForge_AtNight_Works`.
- **Acceptance:** take up Blacksmithing at the smithy, smelt copper, make and equip the Copper-shod cudgel; Forget frees the slot.
- **The owner sees:** his character as a blacksmith making gear he can wear.
- **Depends on:** step 9 (step 8 for tiers 2-5). **Parallel:** no (track B).

## Step 13. Alchemy (track B)
- **Goal:** the second craft, at the herbalist's drying hut.
- **Data:** five potions (D 5.3); `potion.tarn`.
- **Files:** data only, plus trainer lines.
- **Tests:** `CraftStationTests.DryingHut_IsTheBench`; the economy tests over the new recipes.
- **Acceptance:** take up Alchemy at Lisbet's drying hut and make a minor potion; with both crafts taken a third is refused.
- **The owner sees:** potions he makes himself at Lisbet's hut.
- **Depends on:** steps 12 and 1. **Parallel:** no.

## Step 14. Depth and tuning (track B)
- **Goal:** timed buffs, the smith's daily piece, and one tuning pass from play.
- **Files:** `Items.cs` (`buff` fields); `EncounterSession.cs` (buff timers, `UseItem`, the smith's piece in `OpenVendor`).
- **Data:** five elixirs, "Well fed" on seven foods, tuned prices and stipends.
- **Tests:** `BuffTests.An_elixir_adds_its_stat_for_ten_minutes`, `.One_elixir_and_one_meal_at_a_time`; `VillageSupplyTests.Six_ore_puts_the_smiths_piece_on_sale`.
- **Acceptance:** a Yarrow tonic shows +2 Stamina for 10 minutes; six ore sold to Vell puts his uncommon piece on sale that day.
- **The owner sees:** elixirs, well-fed meals, a smith who makes something from his ore.
- **Depends on:** steps 10, 12, 13. **Parallel:** no.

---

## Order at a glance

| Round | Track A (village) | Track B (professions) |
|---|---|---|
| 1 | 1 Buildings | 2 Format 8, items, tools |
| 2 | 3 Households | 4 Gathering in Oakhaven |
| 3 | 6 Workshop days, innkeeper | 5 Bags |
| 4 | 7 Purses | 8 Nodes in four zones, then 9 Stations and charcoal |
| 5 | free to help: 11 Hunting | 10 Cooking |
| 6 | | 12 Blacksmithing, 13 Alchemy, 14 Depth |

## Open questions (defaults are what gets built if unanswered)

1. The leatherworker is already Maud Tanner, a woman, in the published build. **Default: keep Maud; it is "her family" (husband Fen the skinner, daughter Nettie).**
2. Invented kin so every villager has a household (Tobin and Edda Pell married, Pim is Osk Farrow's son, two hen-wives are farm aunts, Jory lives alone). **Default: accept; two new houses are built and those families share.**
3. Skinning. **Default: a hide comes from searching the body, no knife and no skill.** A Skinning skill would add about one step after step 11.

## Risks to watch

- Three new props sit near a placement limit (leather shop and Crisp cottage to the creek, game rack to the Mastwood). `VillageStreamTests` in step 1 is the gate.
- Step 11 was designed from two files; it starts with a read of the targeting code.
- The loot and gear design in `tools/wip/loot` shares `Items.cs`, `items.json`, the save and the item HUD; land it between steps, not beside steps 2, 5, 10 or 11.
- Purse feel and hunting pace are unplayed; step 14 holds the tuning pass.
