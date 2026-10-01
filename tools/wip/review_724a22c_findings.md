# Read-only review of 724a22c (2026-10-01), raw findings and verdicts

## review:session

- [medium] LookVariant's new "Grey " keyword turns every "Grey wolf" into an ash hound (New Unity Project/Assets/Crulanda/Scripts/Encounter/EncounterSession.cs:710)
  Scenario: SpawnActor now calls LookVariant(label), which returns 1 for any label containing "Grey " (case-insensitive). No mob in the zone data is a "Grey Keeper" or "Grey briar"; the only labels that match are "Grey wolf" (8 camps in Zones/oakhaven.json and Zones/khaven.json, look "wolf") and "The Grey Sexton". ActorVisual.BuildBeast uses `ash = variant % 2 == 1`, so every ordinary Grey wolf in Oakhaven and Khaven is now built with the charcoal coat and ember eyes of an Ash hound instead of the grey-brown wolf. Before this commit only labels containing "Ash" got variant 1. The Grey Sexton (look "hollow") also gets variant 1 where it had 0.
  Suggested fix: Drop the "Grey " entry (Withered, Greyheart and Hollow Root already cover the Verdant mobs), or key the variant on look as well as name, e.g. apply "Grey " only when the look is keeper or briar.

### verdict verify:session:LookVariant's new "Grey " keyword turns: isReal=True
Confirmed against current code and data. EncounterSession.SpawnActor (EncounterSession.cs:736) passes LookVariant(label) to ActorVisual.Attach; camp spawns call SpawnActor(camp.mob ...) at line 861. LookVariant (line 710) returns 1 for any label containing "Grey " case-insensitively. Camps with "mob": "Grey wolf", "look": "wolf" exist in Zones/oakhaven.json (lines 3450, 3482, 3498, 3703, 3735) and Zones/khaven.json (3013, 3095). ActorVisual.BuildBeast (ActorVisual.cs:108-114) uses ash = variant % 2 == 1, giving the charcoal coat (.16,.15,.15) and emissive ember eyes, so every ordinary Grey wolf now renders as an Ash hound; before the commit only labels containing "Ash" got variant 1. No camp mob in any zone file is a "Grey " Keeper or briar: the only mob labels matching "Grey " are "Grey wolf" and "The Grey Sexton" (khaven.json:3078, whose variant also flips from 0 to 1). So the entry serves no intended mob and only causes the regression. (The reviewer said 8 camps; I count 7, which does not change the outcome.)
Fix: In New Unity Project/Assets/Crulanda/Scripts/Encounter/EncounterSession.cs, LookVariant (line 710): remove the "Grey " entry from the keyword array, leaving { "Ash", "Withered", "Greyheart", "Hollow Root", "doe" }. No mob in the zone data depends on "Grey " other than the two it wrongly affects. Optionally update the summary comment on line 707 to drop "or grey".

## review:errands

- [medium] Every errand that ends at "home" has its hand-over line wiped in the same call (Hide() nulls Bubble) (New Unity Project/Assets/Crulanda/Scripts/Encounter/WorldLife.cs:558)
  Scenario: ErrandArrive leg 1 calls Say(e.line) at line 554, then at line 558, for e.to == "home" with a home, calls Hide(), which sets Bubble = null (line 617). Resolve("home") only succeeds when home != null, so this happens on every home errand. The authored lines never show: farmer "Seed-corn. It sleeps under my bed...", gossip "Water's bitter again...", child "Chores.", merchant "Shutters up...", hen-wife "{n} for the pot." The villager vanishes at the door silently, with no drop pose either.
  Suggested fix: For home errands, say the line and hold e.work seconds at the door before hiding (e.g. set activity = "home" and let the Activity timeout call Hide), or have Hide() keep a bubble set in the same frame.

- [medium] VillageLife.Stock is never reset, so "today's" deliveries carry over and pile up across days (New Unity Project/Assets/Crulanda/Scripts/Encounter/WorldLife.cs:235)
  Scenario: Stock is documented as "what has been delivered where today", but nothing clears it (no Stock.Clear anywhere; only Villager.done resets per day). Stay in Oakhaven past midnight (a day is 40 real minutes). At 07:00 on day 2, before any delivery: the blacksmith says "The woodcutter brought oak this morning", the merchant says "Eggs in from the hen-wife... Fresh today" and still sells yesterday's unsold food.fresh_eggs (EncounterSession line 258), the drinkers say "Hare in the Cask's pot tonight", and the baker's "No flour from the mill yet today" branch can never fire again. stall.eggs grows without bound day after day.
  Suggested fix: Clear Stock on the day rollover in VillageLife.Update, e.g. alongside the existing WorldClock.Between(4, 5) reset of coop.LaidToday, or track the last hour and clear when it wraps.

- [low] A homeless villager who cowers on the way to a pickup skips the pickup and "delivers" nothing (New Unity Project/Assets/Crulanda/Scripts/Encounter/WorldLife.cs:746)
  Scenario: A villager with home == null (a zone with no house doors) is walking to an errand's pickup (errand set, leg 0, state Travel) when a fight starts nearby. Flee() takes the no-home branch (line 697): state = Activity, until = now + 8, and the errand is not cancelled. When the danger passes, line 746 sees errand != null && leg == 0 and treats it as "picked up": leg = 1; Go(dropAt). They walk empty-handed to the drop, say the delivery line and call Deliver(Max(1, loadCount)) with a stale loadCount, so stock appears from nothing. For a hen-wife the eggs are never taken from the nest. I did not check which zones currently have villagers without homes.
  Suggested fix: Call CancelErrand() in Flee(), or track pickup explicitly (a pickedUp flag set in ErrandArrive leg 0) instead of inferring it from state == Activity && leg == 0.

- [low] Hen-wife says eggs are at the stall or inn when none were delivered (Done() is set before the errand succeeds) (New Unity Project/Assets/Crulanda/Scripts/Encounter/WorldLife.cs:344)
  Scenario: StartErrand adds e.id to done before resolving places (line 506), and ErrandArrive ends the errand at the nest when Coop.Eggs == 0 (line 546). So if the nest is empty at 15.4, the stall is blocked by a collector within 7 m, or dusk/flight interrupts the walk, Done("eggs to the stall") is still true. StockLine then has her say "Eggs are at the produce stall if you're wanting any" while stall.eggs is 0 and the merchant sells none. The same applies to "Took the Cask its eggs this morning".
  Suggested fix: Base the hen-wife's line on Count("stall.eggs") > 0 and Count("inn.eggs") > 0 (as the other trades do), or record a delivered set only in ErrandArrive leg 1.

### verdict verify:errands:Hen-wife says eggs are at the stall or i: isReal=True
Confirmed by reading D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\Encounter\WorldLife.cs and VillageWork.cs. StartErrand (WorldLife.cs:505-509) does done.Add(e.id) before anything else, and nothing ever removes an id except the daily done.Clear(). Three concrete paths leave Done("eggs to the stall") true with nothing delivered: (1) ErrandArrive leg 0 (line 541-546) sets n = Coop.Eggs and, when n == 0, clears the errand at the nest, so life.Deliver("stall.eggs") at line 552 is never reached; (2) Resolve(e.to) returns null and the Blocked/continue branch at lines 507-508 runs after the id is already in done; (3) any interruption on the walk (flee, dusk herding via KeeperNext) goes through ChooseNext -> CancelErrand (line 448/513), which drops the load but leaves the id in done, so the errand is never retried that day. In all three, StockLine line 344 makes the hen-wife say "Eggs are at the produce stall if you're wanting any" while Count("stall.eggs") is 0; the merchant's own line (339) and the vendor stock (EncounterSession.cs:258) both key off Count("stall.eggs") > 0, so the merchant offers no eggs. Same for "Took the Cask its eggs this morning" vs inn.eggs. It also stays wrong after the player buys the stall out (Take reduces the count to 0, Done stays true). Severity is low (a one-in-three ambient chat line contradicting the stall), but the path is real and deterministic once the errand fails.
Fix: In WorldLife.cs, VillageLife.StockLine(Villager v), change the "henwife" case (line 344) to key off deliveries like the other trades: case "henwife": return Count("stall.eggs") > 0 ? "Eggs are at the produce stall if you're wanting any. I don't sell from the yard." : Count("inn.eggs") > 0 ? "Took the Cask its eggs this morning. The rest go to the stall after dinner." : null; No change to StartErrand/ErrandArrive is needed; Villager.Done can stay for other callers.

### verdict verify:errands:VillageLife.Stock is never reset, so "to: isReal=True
Confirmed by reading the current code. VillageLife.Stock (D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\Encounter\WorldLife.cs line 235) is written only by Deliver (called from Villager.ErrandArrive, line 552) and decremented only by Take, whose single caller is the fresh-egg purchase in EncounterSession.cs line 284. A repo-wide grep finds no Stock.Clear, no reassignment (the field is readonly) and no other Take. The clock wraps in place (WorldClock.cs line 59, Mathf.Repeat(...,24)) while the same VillageLife instance lives for as long as the player stays in the zone (created once per zone in EncounterSession.cs line 696), and the code itself expects day rollovers within one instance: Villager.StartErrand clears its per-day 'done' set on hour wrap (line 500) and Update resets coop.LaidToday between 04:00 and 05:00 (line 114). So after the first day every key that was ever delivered (forge.wood, oven.flour, mill.grain, tannery.hides, inn.meat, inn.bread, inn.eggs, inn.wood, stall.flour, stall.goods) stays > 0 forever, and on day 2 before any delivery StockLine (lines 335-348) returns the 'today' lines: blacksmith 'The woodcutter brought oak this morning', drinkers 'Hare in the Cask's pot tonight', merchant 'Fresh today', and the baker's 'No flour from the mill yet today' branch is unreachable because oven.flour never returns to 0. stall.eggs only falls when the player buys, so unsold eggs carry over and accumulate, and OpenVendor (EncounterSession.cs line 258) keeps listing yesterday's eggs. Reachable by simply staying in Oakhaven past one day (RealMinutesPerDay real minutes) or via WorldClock.Advance. Impact is wrong ambient dialogue and stale/accumulating egg stock, not a crash or soft-lock, but it certainly shows.
Fix: In WorldLife.cs, VillageLife.Update(): clear Stock once per day at the same pre-dawn point as the LaidToday reset. Do it before the per-coop loop so it also works in villages with no coops, and after the existing 20 s throttle:

    nextLay = Time.time + 20;
    if (WorldClock.Between(4, 5)) Stock.Clear();   // yesterday's deliveries are gone before the day's errands start
    foreach (var coop in Zone.Coops) { ... }

This is safe to run repeatedly during the 04:00-05:00 window provided no errand in VillageWork delivers in that hour (the hen-wife/trade errands start from the morning); if any errand does deliver between 04:00 and 05:00, use a wrap check instead: keep a float lastHour field on VillageLife and do `if (WorldClock.Hour < lastHour - 6) Stock.Clear(); lastHour = WorldClock.Hour;` at the top of Update (before the throttle), mirroring Villager.StartErrand line 500. Note the 20 s throttle samples the 04:00-05:00 window several times at normal clock speed (1 game hour = RealMinutesPerDay*60/24 s, 100 s at a 40-minute day), but a WorldClock.Advance that jumps over 04:00-05:00 would skip the Between-based reset, so the wrap check is the more robust of the two.

### verdict verify:errands:Every errand that ends at "home" has its: isReal=True
Confirmed in the current code of D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\Encounter\WorldLife.cs. In ErrandArrive() leg 1, line 554 calls Say(e.line...) (sets Bubble/BubbleUntil), and line 558, for e.to == "home" && home != null, calls Hide() in the same call. Hide() (lines 613-618) sets state = State.Hidden and Bubble = null. Resolve("home") returns null when home == null, so StartErrand never starts a home errand without a home; the Hide branch is therefore taken on every home errand. The HUD cannot show it by any other route: EncounterHud.GatherPlates (line 513) skips villagers with !v.Visible (state == Hidden), and the bubble draw at line 574 returns when Bubble == null. VillageWork.cs has home errands with authored hand-over lines that can therefore never be seen: farmer "seed-corn home" (line 51), merchant "shutters up" (77), gossip "the morning's water" (79), child "chores" (83), henwife "eggs for the pot" "{n} for the pot." (95). The dropPose and the e.work hold set at line 551 are also never seen, since renderers are disabled in the same frame. (The child's "a loaf for Mum" line is a pickupLine said at leg 0, so that one is unaffected.) Purely cosmetic (no soft-lock: the Hidden branch in Update emerges after 20 s), but it certainly happens every time.
Fix: WorldLife.cs, two small edits.

1) ErrandArrive(), leg-1 branch, replace line 558
   if (e.to == "home" && home != null) { activity = "home"; until = Time.time + 20; Hide(); }
with a hold at the door instead of an immediate hide:
   if (e.to == "home" && home != null) { activity = "homecoming"; until = Time.time + Mathf.Max(e.work, 4); }
(errand = null on the next line stays. Optionally move DropLoad() at line 557 so home errands keep the load in hand during the hold; Hide() already calls DropLoad().)

2) Update(), in the State.Activity timeout block (line 743-748), go indoors when the hold ends:
   else if (Time.time > until)
   {
       partner = null;
       if (errand != null && leg == 0) { leg = 1; Go(dropAt); }
       else if (activity == "homecoming" && home != null) { activity = "home"; until = Time.time + 20; Hide(); }
       else ChooseNext();
   }

This keeps the villager visible with the drop pose and the bubble for the hold, then hides them for the same 20 s as before (the Hidden branch's `until - calmSince` still works because Hide() sets calmSince to now). Flee/Interrupted paths are unaffected: both run before this block and call ChooseNext/Flee, which overwrite activity.

### verdict verify:errands:A homeless villager who cowers on the wa: isReal=False
The code path exists but is unreachable with the current code and data, so the failure cannot happen in the game as committed.

The mechanism itself is as described. In `D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\Encounter\WorldLife.cs`, the no-home branch of `Flee()` (line 697) sets `state = Activity` and `until = now + 8` without cancelling the errand, and `Update()` line 746 would then promote `leg` 0 to 1 without a pickup.

It needs an errand-running villager with `home == null`, and none can exist:

- **Ordinary villagers** (`Init`, line 72) get `Homes[i % Homes.Count]`, which is null only when the zone has no non-openable door. Only `khaven.json` (6 villagers, 5 `house` props) and `oakhaven.json` (20 villagers, houses present) have `life.villagers > 0`. Every `house` adds a non-openable door (`ZoneBuilder.House`, line 1058), so every villager there has a home.
- **Hen-wives** (line 94) take the nearest home. Coops exist only in `oakhaven.json`, which has houses.
- **Residents** (line 85) are the only villagers spawned with a null home, in any zone. They always have a `fixedPlace`, and `ChooseNext()` returns at line 458 before `StartErrand()` at line 459, so they never have an errand. They are not hen-wives either, so the `KeeperNext` path (line 643) does not apply.
- **Zones with no villagers** (`ashrim.json`, `peaks.json`, `verdant.json`) have `villagers: 0` and no coops, so only residents live there.
- **A homed villager off the NavMesh** cannot reach the branch either: `Update()` returns at line 704 when `!agent.isOnNavMesh`, before `Flee()` is called.

This is a latent hazard only: it would become real if a future zone had villagers or a coop but no house.
Fix: No fix is required for this commit. If hardening is wanted, in `Villager.Flee()` in `WorldLife.cs`, call `CancelErrand()` in the else (no-home) branch at line 697, so a cowering villager drops the errand and `ChooseNext()` runs once `until` passes.

## review:world

- [medium] Coop water disc is always buried inside the solid pan cylinder, so the water never shows (New Unity Project/Assets/Crulanda/Scripts/World/WorldClock.cs:179)
  Scenario: ZoneBuilder.Coop() builds the pan as a solid Unity cylinder at local y .08 with scale (.8, .08, .8): it occupies y 0 to .16, radius .4. The water is a cylinder of scale (.68, .012, .68) (radius .34, half-height .012) parented to the same pan. ZoneCoop.Show() sets its y to .05 + Level * .09, so at Level 1 the disc is centred at .14 and its top is at .152, still under the pan's top face at .16, and it only sinks further as it dries. When the hen-wife fills the pan (Coop.Water()), the disc is activated but is wholly enclosed in the opaque pan at every Level, so the player never sees water. VillageErrandTests only asserts water.gameObject.activeSelf, so it passes anyway.
  Suggested fix: Make the full-level water top clear the pan top (.16), e.g. p.y = .15f + Level * .02f, or build the pan as a low base plus a rim (base top at about .05) so the .05-.14 travel is visible.

- [medium] The Root-Mother's upper knot, face hollow and sap-light eyes are placed above the passage roof at the tapered end of the Heart (New Unity Project/Assets/Crulanda/Scripts/World/ZoneBuilder.cs:3292)
  Scenario: RootDeepInterior puts seat at On(h.Length - 3.5, 0) and knot 1.5 m further back, i.e. plan z about 96 and 97.5. RootDeepPlan tapers there: height 5.5 at z 95, 2.5 at z 98, .4 at z 99.5 (Catmull-Rom gives roughly 5.1 at z 95.5, 4.7 at z 96, 3.1 at z 97.5), half-width about 3.9 falling to 2.4. But the figure is sized for the 8 m high hall: the upper sphere is centred 5.4 m up (spans 3.7-7.1 m) over a roof of about 3.1 m; the face hollow is centred at 5.3 m at z about 96.1 (roof about 4.6 m); the two sap eyes are at 5.6 m at z about 95.55 (roof about 5.1 m, +/- 15 percent noise). The shell is opaque and drawn both sides, so the upper knot, the face hollow and its eyes are in the rock above the roof; from inside the player sees only the lower 7 m wide sphere plugging the end of the tunnel (and it is wider than the 5-8 m passage there). The dungeon's centrepiece, 'a hollow where a face would be', is not visible.
  Suggested fix: Seat the knot where the Heart is still tall (e.g. sEnd = h.Length - 9 or so, height about 7-8) or scale the knot/face/eye heights by h.Height[RingAt(sEnd)] so the face sits under the roof.

- [medium] Root-Mother's 'roots into the floor' use -from.y + .1 in the prop's frame, so they shoot 12-16 m up to the mouth's ground level instead of down (New Unity Project/Assets/Crulanda/Scripts/World/ZoneBuilder.cs:3302)
  Scenario: In the 14-limb loop, non-'up' limbs (9 of 14, k % 3 != 0) end at to = from + face * (.., -from.y + .1f, ..), which makes to.y = .1 in the cavern root's local frame. That frame's origin is the mouth's ground; the Heart floor is at local y about -16.8 (plan drop), and from.y is about -15.8 to -11.8. So these limbs do not run down into the Heart floor: each is a .3-.55 m thick tube climbing 12-16 m through the roof and the earth to the mouth's surface level above the Heart (their 5 cm tips end at mouth-ground height + .1, which is at or above the land wherever the land over the Heart is not higher than the Temple hollow). Result: every one of the 14 roots goes up, none into the floor as designed, and long hidden meshes run through the rock toward the surface. It would only be right for a cavern with zero floor drop.
  Suggested fix: Target the Heart floor: use seat.y (or FloorY at the end point) instead of 0, e.g. y component = seat.y + .1f - from.y.

### verdict verify:world:Coop water disc is always buried inside: isReal=True
Confirmed by reading the current code. ZoneBuilder.Part() (ZoneBuilder.cs:842) sets localPosition and localScale verbatim on a Unity primitive, with no snapping. In ZoneBuilder.Coop() (line 1789) the pan is a solid Unity cylinder (unit height 2, radius .5) at y .08 with scale (.8, .08, .8), so it fills y 0 to .16 at radius .4. The water (line 1790) is a cylinder of scale (.68, .012, .68): radius .34, half-height .012, under the same "Water pan" parent. ZoneCoop.Show() (WorldClock.cs:179) sets y = .05 + Level * .09, so the highest the disc ever gets is centre .14, top .152, which is below the pan top at .16 and inside its radius. The pan is opaque (tinted art.metal), so the water is fully enclosed at every Level and is never visible after Coop.Water(). Nothing else moves or rescales the disc (ZoneCoop.water is only touched in Show()). I did not run the game, per the read-only brief; this is from geometry alone.
Fix: In ZoneCoop.Show() (New Unity Project/Assets/Crulanda/Scripts/World/WorldClock.cs line 179), change the height so the disc's top face stays above the pan top (.16) across the whole visible range: `p.y = .15f + Level * .02f;`. At Level 1 the top is at .182; at the .02 cut-off it is at about .162, so it still sinks as it dries and never z-fights the pan top. Alternative in ZoneBuilder.Coop() (line 1789): make the pan a low base, e.g. Part(Cylinder, pan, (0, .02, 0), (.8, .02, .8)), so the existing .05-.14 travel sits above it; that loses the rim unless one is added.

## review:data

- [medium] main.verdant.5 never awards its page: rewards uses "document" (singular), the reward class only reads "documents" (New Unity Project/Assets/Crulanda/EncounterContent/Quests/verdant.json:791)
  Scenario: QuestRewardDef (Quests.cs:63-68) has only `string[] documents`; the singular `document` field exists on objectives, not rewards. JsonUtility silently drops the unknown key, so rewards.documents is empty: QuestDatabase.Check raises nothing and the turn-in loop `foreach (var d in r.documents) Reveal(d)` (Quests.cs:265) does nothing. Turning in 'The Root-Mother's Deep' to Willow-Whisper gives xp, gold and reputation but 'What the Root-Mother Dreams' (doc.verdant.root_mother) never enters the Chronicle, and nothing else references that document. Every other reward page in this file uses the array form (lines 145, 364, 540).
  Suggested fix: Change to "documents": ["doc.verdant.root_mother"].

- [low] The dungeon kill objectives are credited by the surface camps that share the tags (New Unity Project/Assets/Crulanda/EncounterContent/Quests/verdant.json:735)
  Scenario: Camp mob ids are mob.<tag>.verdant.<campIndex>.<n> (EncounterSession.cs:860) and QuestLog.Matches is a prefix match, so 'mob.withered.verdant*' also matches The Greying (camp 4, 4 mobs at 84,142) and 'mob.mistwalker.verdant*' matches Palemist mist-walkers (camp 6, 5 mobs at 116,-110). Once step 1 is done, killing 3 mist-walkers at Palemist Hollow completes 'Mist-walkers broken in the Sap Well', and Greying kills count toward 'Withered Keepers laid to rest' in the deep, without touching the dungeon's camps. (In-dungeon counts are otherwise sufficient: withered 3+2 = 5 needed 5, mist-walkers 3 needed 3, Root-Warden 1.)
  Suggested fix: Give the dungeon camps their own tags (e.g. deepwithered, deepwalker) with matching loot tables, and target those in the quest; or reword the objective text so it does not claim the Sap Well.

- [low] Step 1 'Go down into the Root Gallery' completes from the surface on top of the Temple's barrow (New Unity Project/Assets/Crulanda/EncounterContent/Quests/verdant.json:718)
  Scenario: Visit objectives are tested in 2D only (EncounterSession.cs:382: Vector2.Distance of player x,z to o.at <= radius, no height or Hollow check). The objective is at (-111.5,153) radius 8, and this commit adds the walkable shape 'The Temple's barrow' centred (-112,152), radius 12, height 3, blend 10, directly above it (the Gallery floor is about 8.6 m below the mouth). A player with the quest who walks up the mound behind the Temple gets the step completed and the narration 'The stair ends in a hall of roots...' while standing on the grass above.
  Suggested fix: Move the visit point further down the plan to where the land above is beyond reach, shrink the radius, or make the visit check require Hollow.InsideAny at the player's position when the objective point lies in a hollow.

- [low] 'The cold in the root' vanishes when salted, then reappears intact 90 s later (New Unity Project/Assets/Crulanda/Scripts/World/ZoneBuilder.cs:3316)
  Scenario: The interactable is registered with kind = "crates" and once left false. ZoneInteractable.Vanishes is true for crates, so UseInteractable (EncounterSession.cs:364-365) takes the non-once branch: hiddenUntil = now + 90 and HideFor(root, 90). On salting, the whole husk (black rod, violet light, hoarfrost and the pale root it is lodged in) disappears, and 90 seconds later comes back unchanged, hoarfrost and rod included, with the 'Salt the cold root' prompt live again (it then answers 'You look it over...'). After a reload it is always back. This contradicts the objective's own text (the frost turns to water on living bark) and the quest's outcome.
  Suggested fix: Set once = true so it stays used across reloads, and use a kind that does not hide the whole root (or hide only the rod and frost children) so the pale root stays.

- [low] The Pilgrim's pack is described and prompted as 'by the sap-pool' but sits in the entry passage about 10 m from the pool (New Unity Project/Assets/Crulanda/EncounterContent/Zones/verdant.json:3030)
  Scenario: secret.verdant.pilgrims-pack is at world (-99,178), i.e. plan (13,48): between plan rows (12,45) and (15,50), in the mouth of the root-choked passage before the Sap Well, close to the passage centre line. RootDeepInterior puts the pool at On(Along[well], Half - pr - 0.7), about plan (19.1,56), world (-92.9,186), against the east wall. That is roughly 10 m from the pack, with the Sap Well walkers camp (plan 13.5,55) between them. The prompt 'Search the pack by the sap-pool' and the text 'left by the sap-pool' therefore describe a spot the pack is not at, and it lies in the middle of the walkway rather than tucked beside anything. (It is inside the passage and off the pool, so it is reachable.)
  Suggested fix: Move it to about world (-95.5,183) (plan 16.5,53), which is beside the pool rim but clear of the pool's blocker, or reword the prompt and text to 'at the mouth of the Sap Well'.

### verdict verify:world:Root-Mother's 'roots into the floor' use: isReal=True
Confirmed by reading the current code. In Cavern(), c[i] = t.InverseTransformPoint(h.Centre[i]) and Hollow sets Centre.y = mouth ground + plan drop, so the cavern prop's local y=0 is the mouth's ground and On() returns l.y = c[RingAt(s)].y. RootDeepPlan's last rows have drop -16.8, so in RootDeepInterior seat.y (and knot.y) is about -16.8. from = knot + up*(1 + D()*4) gives from.y of about -15.8 to -11.8. For k % 3 != 0 (9 of 14 limbs: k = 1,2,4,5,7,8,10,11,13) the y offset is -from.y + .1f, so to.y = +0.1 in the local frame, i.e. the mouth's ground level, 12-16 m ABOVE the start rather than on the Heart floor (local y about -16.8). face is a pure yaw rotation so the y component passes through unchanged. Heart roof is only about 8-8.5 m high (top at about local -8.5), so these .3-.55 m thick limbs go up through the roof and the earth; none of the 14 roots goes into the floor (the other 5 go up 3-6 m by design). The formula is only right for a cavern with zero floor drop (it was evidently written as if floor y were 0). Visible inside the Heart as 9 thick roots all rising steeply into the ceiling instead of spreading into the floor, with long hidden tubes through the rock up to about mouth-ground level.
Fix: D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZoneBuilder.cs, RootDeepInterior(), the 14-limb loop (line 3303): make the downward limbs end on the Heart floor instead of local y = 0. Replace `-from.y + .1f` with `seat.y + .1f - from.y`:

var to = from + face * new Vector3(Mathf.Sin(a) * r, k % 3 == 0 ? 3 + D() * 3 : seat.y + .1f - from.y, Mathf.Cos(a) * r * .8f + 2);

(The Heart floor is level at drop -16.8 over the last rows, so seat.y is adequate; optionally set to.y = FloorY(to.x, to.z) + .1f after computing to for the non-up limbs. Do not add extra D() calls, to keep the random stream unchanged.)

### verdict verify:data:main.verdant.5 never awards its page: re: isReal=True
Could not refute; the path is concrete. verdant.json line 791 (quest main.verdant.5) has "document": "doc.verdant.root_mother" inside "rewards". QuestRewardDef (Scripts/Encounter/Quests.cs:63-68) declares only xp, gold, items, documents (string[]) and reputation; the singular `document` field exists only on the objective class (Quests.cs:59). Files are loaded with JsonUtility.FromJson<QuestFile> (Quests.cs:96), which silently ignores unknown keys, so rewards.documents stays the empty default. The validator at Quests.cs:141 iterates an empty array and raises nothing, and the turn-in loop at Quests.cs:265 (`foreach (var d in r.documents) Reveal(d)`) reveals nothing. A grep of Assets/Crulanda shows doc.verdant.root_mother is referenced only at its definition (verdant.json:79) and at line 791; none of the quest's objectives (lines 715-781) carries a `document`. So turning in 'The Root-Mother's Deep' gives xp, gold and Keepers reputation but the page is never added to Progress.documents and is unobtainable. The other three reward pages in this file (lines 145, 364, 540) use the array form.
Fix: In D:\code\mmo\New Unity Project\Assets\Crulanda\EncounterContent\Quests\verdant.json, quest main.verdant.5 rewards (line 791): replace `"document": "doc.verdant.root_mother"` with `"documents": ["doc.verdant.root_mother"]`. No code change needed.

### verdict verify:data:'The cold in the root' vanishes when sal: isReal=True
Confirmed by reading the current code; I could not refute it.

- ZoneBuilder.cs:3316 registers the prop as `kind = "crates"` with `once` left at its default (false). The rod, violet sphere, nine hoarfrost patches and the pale root (line 3314) are all children of `husk`, which is the interactable's `root`.
- ZoneDefinition.cs:196: `Vanishes` is true for "crates".
- EncounterSession.UseInteractable (lines 364-365): with `once == false` it takes the `else if (i.Vanishes)` branch, sets `hiddenUntil = Time.time + 90` and starts `HideFor(root, 90)`.
- HideFor (lines 370-374) disables every Renderer under the husk, then re-enables them all after 90 s.

Resulting behaviour when the player salts the root during the verdant finale step (verdant.json:759-767):
1. The rod, frost and the pale root itself all disappear at once. The objective text says the frost becomes water on living bark, so the root should remain.
2. After 90 s the rod and hoarfrost come back unchanged, and `NearbyInteractable` offers "Salt the cold root" again. Using it gives "You look it over, but find nothing you need right now."
3. Nothing is written to `usedInteractables`, and the reload pass at line 385 only covers `once` props, so after any reload or zone re-entry the cold is back in full even with the quest finished.

One addition to the original finding: HideFor and HideProp only toggle Renderers, so the violet `Glow` at line 3315 is probably never hidden at all. That assumes `Glow` creates a Light; I did not open it.

RootDeepTests.cs:103-104 only asserts the prop exists and sits in the Heart, so the test run will not catch this.
Fix: File: New Unity Project/Assets/Crulanda/Scripts/World/ZoneBuilder.cs, in the Root-Mother's Deep builder at lines 3314-3316.

1. Line 3316: add `once = true` so the use is saved in `usedInteractables`, the prompt never returns, and the reload pass at EncounterSession.cs:385 keeps it hidden:
`Interactables.Add(new ZoneInteractable { name = husk.name, prompt = "Salt the cold root", kind = "crates", once = true, position = husk.position, root = husk });`

2. Line 3314: parent the pale root to `t` instead of `husk` so it stays when the husk is hidden:
`Part(PrimitiveType.Sphere, t, cold + new Vector3(0, .25f, 0), new Vector3(1.6f, .5f, 1.6f), pale, face);`
The prop name, position and save key are unchanged, so RootDeepTests is unaffected.

3. Optional, same cause: if `Glow` at line 3315 creates a Light, the violet light stays on after salting because HideProp only disables Renderers. Either disable Lights in EncounterSession.HideProp as well (`foreach (var l in t.GetComponentsInChildren<Light>()) l.enabled = false;`) or accept it.

### verdict verify:world:The Root-Mother's upper knot, face hollo: isReal=True
Could not refute it: the geometry puts the upper knot, most of the face hollow and almost certainly the eyes above the passage roof. Derived from the code only; nothing was run.

**Where the figure sits.** In `RootDeepInterior` (ZoneBuilder.cs:3292-3298), `seat = On(h.Length - 3.5, 0)` and `knot` is 1.5 m further from the door. `Hollow.Length` is the horizontal path length and the last plan rows run straight in z (x = -2), so the seat is at plan z of about 96 and the knot at about 97.5. The floor is level there (-16.8), and `On()` puts y on the floor.

**Roof height there.** The `Hollow` constructor smooths height with Catmull-Rom over rows 8 (z 90), 5.5 (z 95), 2.5 (z 98), .4 (z 99.5). I recomputed it:

| Plan z | Roof crown height |
|---|---|
| 95.6 | about 5.1 m |
| 96.2 | about 4.5 m |
| 97.4 | about 3.2 m |

The shell ring in `Cavern` uses `up = Height * sin^.85 * (1 + bump * .8)` with bump within about ±0.19, so the roof varies by at most ±15%.

**Each part against that roof.**
- **Upper knot:** centre 5.4 m, y-scale 3.4, so it spans 3.7-7.1 m at z 97.5, where the roof is about 3.2 m. It is entirely above the roof.
- **Face hollow:** centre 5.3 m at z of about 96.1, spanning 4.2-6.4 m under a roof of about 4.6 m. At most a 0.4 m sliver of its underside pokes through.
- **Sap eyes:** 5.6 m at z of about 95.55, roof about 5.15 m. They are hidden unless the noise there is near its maximum.

**Why that hides them.** The shell mesh is opaque with inward-facing triangles and a MeshCollider, so anything above it cannot be seen from inside. The player sees only the lower 7 x 5.2 m sphere plugging the tunnel end. The figure is sized for the 8-8.5 m hall, which is what `heart = widest(...)` picks, but it is seated in the taper.

The face light at 5.2 m, z of about 94.9, is still just under the roof (about 5.5 m), so the light works but the face it should light is in the rock.
Fix: File `New Unity Project/Assets/Crulanda/Scripts/World/ZoneBuilder.cs`, function `RootDeepInterior`, the `if (heart >= 0)` block (line 3292 onward).

1. Seat her where the Heart is still tall: change `float sEnd = h.Length - 3.5f;` to `float sEnd = h.Length - 6.5f;`. That puts the seat at z of about 93 and the knot at about 94.5, where the roof is about 5.8 m.

2. Fit the figure under the roof at the knot. After `knot` is computed, add:
   `float ky = Mathf.Min(1f, h.Height[RingAt(sEnd + 1.5f)] * .95f / 7.1f);`

3. Multiply every vertical offset and y-scale of the figure by `ky`:
   - lower sphere: `Vector3.up * 2.6f * ky`, scale `(7, 5.2f * ky, 4)`
   - upper sphere: `Vector3.up * 5.4f * ky`, scale `(4.2f, 3.4f * ky, 3)`
   - face hollow: `face * new Vector3(0, 5.3f * ky, 1.4f)`, scale `(1.6f, 2.2f * ky, 1)`
   - eyes: `face * new Vector3(side * .55f, 5.6f * ky, 1.95f)`
   - face Glow: `face * new Vector3(0, 5.2f * ky, 2.6f)`
   - Limb start heights: `Vector3.up * (1 + D() * 4) * ky`
   - `Block(knot, new Vector3(6, 7 * ky, 3.5f), face)`

The cold rod and its interactable are positioned from `seat`, so they follow. `RootDeepTests` only asserts `Depth > .9` for the rod, which still holds.

Not checked: whether the Hollow Root-Warden's spawn point is clear of the knot's new Block, now 3 m nearer the door.

### verdict verify:data:The dungeon kill objectives are credited: isReal=True
Confirmed by reading the current code and data; I could not refute it.

- Camp mob ids are built as "mob." + tag + "." + zoneShort + "." + campIndex + "." + n (EncounterSession.cs:860).
- QuestLog.Matches (Quests.cs:430) is a plain StartsWith on the pattern minus "*", and QuestLog.Notify (Quests.cs:283-301) applies no camp or position filter to kill objectives.
- Zones/verdant.json has surface camps "The Greying" (tag withered, 4 mobs at 84,142) and "Palemist mist-walkers" (tag mistwalker, 5 mobs at 116,-110). The dungeon camps "Gallery withered", "The Heart's withered" and "Sap Well walkers" use the same two tags.
- main.verdant.5 step 2 (Quests/verdant.json:735-748) targets "mob.withered.verdant*" x5 and "mob.mistwalker.verdant*" x3, so surface kills are credited.

Concrete path: finish step 1 (visit the Root Gallery), walk out, kill 3 mist-walkers at Palemist Hollow. "Mist-walkers broken in the Sap Well" then reads 3/3 without the player entering the Sap Well. The 4 Greying keepers (90 s respawn) likewise fill 4 of the 5 withered kills, and a respawn supplies the fifth.

It is worse on load: ReconcileQuests (EncounterSession.cs:89-91) counts every dead entry in Progress.enemies that matches the prefix, so surface mobs still dead in the save are credited the moment step 2 becomes current, via Math.Max in Quests.cs:422.

Only the Root-Warden objective is unique to the dungeon. The quest still cannot be finished without going down, but two of its three dungeon objectives can be completed on the surface, contradicting their own text.
Fix: Data-only fix, no code change.

1. In New Unity Project/Assets/Crulanda/EncounterContent/Zones/verdant.json, give the three dungeon camps their own tags:
   - "Gallery withered" and "The Heart's withered": "tag": "deepwithered"
   - "Sap Well walkers": "tag": "deepwalker"
   Leave "look" as is (keeper / hollow), so visuals are unchanged.

2. In New Unity Project/Assets/Crulanda/EncounterContent/Quests/verdant.json, quest main.verdant.5, step 2, change the targets:
   - "mob.withered.verdant*" to "mob.deepwithered.verdant*"
   - "mob.mistwalker.verdant*" to "mob.deepwalker.verdant*"
   Neither new prefix is matched by the earlier quests' "mob.withered.verdant*" / "mob.mistwalker.verdant*" patterns (lines 248, 326, 340), and those patterns do not match the new ids.

3. In New Unity Project/Assets/Crulanda/EncounterContent/Items/items.json, the loot entries at about lines 972 ("tag": "withered") and 1006 ("tag": "mistwalker") are keyed by tag. Duplicate them for "deepwithered" and "deepwalker" so the dungeon mobs keep their drops. I did not read the loot lookup code, so check whether it is an exact tag match before relying on this.

Side effect to accept or handle: the earlier quests (lines 248, 326, 340) will no longer be credited by dungeon kills. That is correct for their text (The Greying / Palemist).

### verdict verify:data:The Pilgrim's pack is described and prom: isReal=False
The geometry in the finding is accurate, but it does not amount to a defect under the stated bar: it is a wording-precision nit.

Checked against current code and data:
- Cavern prop "The Root-Mother's Deep" is at (-112,130), rotation 0, variant 1 (verdant.json ~line 2128), so the pack at world (-99,178) is plan (13,48). The spline between rows (12,45,half 2.6) and (15,50,half 5) puts the centre near x=13.8 with a half-width of about 4 there, so the pack is inside the walls, about 0.8 m off the centre line, where the passage is already flaring into the Sap Well chamber.
- RootDeepInterior (ZoneBuilder.cs line 3257/3276): well = widest ring in 45-70% of length (~51-79 m of ~113 m), which is row (16,56,half 6) at about 63 m in. pr = min(2.4, 6*0.36) = 2.16; pool = On(sw, 6 - 2.16 - 0.7 = 3.14), i.e. plan (19.1,56), world (-92.9,186). Hollow.At's right vector is +x for a north-running passage, so that is the east wall as the comment says.
- Pack to pool centre is about 10 m, about 7.5 m from the pool's rim.

Why it is not a real defect: the pack is reachable (inside the walls, not under the pool's Block, radius 2.5 m interaction), it is in the opening of the same chamber as the pool, with an unobstructed line of sight to it (the line from plan (13,48) to (19.1,56) stays inside the walls at every row), and it is inside the pool's Glow range (11 m, line 3279), so it is literally lit by the sap-pool when the player reads the prompt. Nothing breaks, nothing is unreachable, nothing renders wrong; "by the sap-pool" is loose for something 7.5 m from the rim but not false enough to be a certain visible glitch. The complaint that it sits "in the middle of the walkway" is also harmless: the passage is about 8 m wide there and a cache does not block the way.

Optional polish only (not required): in verdant.json secret.verdant.pilgrims-pack, either move "at" to about (-95.5,183) or reword prompt/text to "at the mouth of the Sap Well". If moved, note the pool Block is a 4.9 m square centred at world (-92.9,186), and the Sap Well walkers camp centre is (-98.5,185).

File: D:\code\mmo\New Unity Project\Assets\Crulanda\EncounterContent\Zones\verdant.json (line 3030); D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\ZoneBuilder.cs (RootDeepPlan line 2829, RootDeepInterior line 3276); D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\World\Hollow.cs (At, line 152).

## review:tests

- [medium] Farmer half of the water/barley test races the water half: the grain errand can be finished before the test starts looking for it (New Unity Project/Assets/Crulanda/Tests/PlayMode/VillageErrandTests.cs:96)
  Scenario: The test opens at 9.65 and runs at timeScale 4. The clock passes 10.0 (the start of the farmer's "grain to the mill" window) 35 game seconds in, so Oakhaven's three farmers start the errand on their next ChooseNext while the test is still waiting for the hen-wife. Her pan is filled roughly 125-150 game seconds in (nest, then about 85 m to the well at 1.6 m/s, 5 s work, and back). A farmer's whole errand typically lands between about 80 and 210 game seconds in. Only after the pan fills does the test set Hour = 10.05, which assumes the errand has not begun; the jump back is under 6 h, so Villager.done is not cleared. If all three farmers have already delivered, nobody carries Load.Grain again until "seed-corn home" at 16.5, and the 90 s wait only reaches about 13.65, so Assert.NotNull(farmer) fails although the feature worked. In the other ordering, where one farmer has delivered and another is still carrying, the final mill.grain > 0 check is already true and proves nothing about the farmer the test found.
  Suggested fix: Give the farmer leg its own test opened at about 9.95, or record a farmer carrying grain and the mill.grain count with one predicate from the start of the test instead of sampling after the water wait. Assert on a mill.grain increase from a value captured when the chosen farmer picked up.

- [medium] "Handed over" asserts on the first hen-wife seen carrying, but the wait ends when any of the three hen-wives delivers (New Unity Project/Assets/Crulanda/Tests/PlayMode/VillageErrandTests.cs:57)
  Scenario: Oakhaven has three coops (Harrow at -90,61; West field at -61,28; Brook at 68,-93), so three hen-wives. The test sets Eggs = 4 on every coop, and all three run the same sequence from the same coop-local geometry: morning feed (2.6 m walk, 16 s), then the nest. `carrier` is whichever is seen carrying first, decided by frame-level differences; if they pick up on the same frame, FirstOrDefault returns Goody Marl of the Harrow coop, about 104 m from the inn. The wait at line 57 ends on life.Count("inn.eggs") > 0, which the West-field wife (about 60 m from the inn) satisfies about 27 game seconds before the other two can arrive. Unless `carrier` happens to be the West-field wife, lines 59-60 (IsFalse(carrier.Carrying), carrier.Done("eggs to the inn")) fail while she is still walking. The test has passed so far only because of which wife reaches her nest a frame earlier.
  Suggested fix: Wait on the carrier herself: WaitUntil(() => carrier.Done("eggs to the inn") && !carrier.Carrying, ...). Capture inn.eggs when she picks up and assert that it grew. Alternatively choose the carrier by coop rather than by first sighting.

- [low] Unbounded synchronous buy loop hangs the test run if the stall holds more eggs than the player can afford (New Unity Project/Assets/Crulanda/Tests/PlayMode/VillageErrandTests.cs:76)
  Scenario: `while (life.Count("stall.eggs") > 0) session.Buy(FreshEggs);` never yields. Eggs cost 8 gold (value 2 x 4) and the test grants 50 on a fresh character, so 6 purchases succeed and gold is left at 2. The stall count is whatever the delivering coop held: 3 set by the test plus whatever the hens laid before pickup (a 35% roll every 20 game seconds until 16.5; the Harrow coop has 6 hens). If the delivery is 7 or more, the seventh Buy returns "You need 8 gold" without taking stock, and the loop spins forever. That freezes the editor and the whole run rather than failing one test. It needs at least four lays between 15.45 and pickup, so it is improbable.
  Suggested fix: Bound the loop (for i < 20, then assert the count is 0), or grant enough gold (for example += 500), or set Stock["stall.eggs"] to a known small number before buying.

### verdict verify:data:Step 1 'Go down into the Root Gallery' c: isReal=True
Confirmed by reading the current code and data; nothing blocks the path.

1. **The check is 2D only.** `EncounterSession.TickQuests` (`EncounterSession.cs:382`) passes only when `Vector2.Distance(new Vector2(p.x, p.z), o.at) <= o.radius`, plus zone and night conditions. There is no height or Hollow test.

2. **The objective sits under walkable land.** The visit point is (-111.5, 153) radius 8 in `Quests/verdant.json`. The cavern prop "The Root-Mother's Deep" is at (-112, 130), rotation 0, variant 1. `RootDeepPlan` row `{.5, 23, 6.5, 6, -8.6}` puts the Gallery at world (-111.5, 153), floor 8.6 m below the mouth's ground.

3. **The mouth is itself sunk.** It lies inside the shape "The Veridian Temple's root-stair" (centre (-112, 124), radius 8, height -2.5), so the Gallery floor is about 11 m below the surrounding land.

4. **The barrow adds 3 m on top.** "The Temple's barrow" (centre (-112, 152), radius 12, height 3, blend 10) raises the land directly above. The Gallery roof (`Hollow.Roof` = floor + 6*1.19 + .15) ends up about 6-7 m under the surface.

5. **The ground above is intact.** These rings are not `NearSurface`, and `Hollow.Inside` is false at land height, so the ground mesh is not cut there (`ZoneBuilder.cs:411`).

6. **The mound is reachable.** Verdant is not a mountain biome (`hillHeight` 5, no crags). The boundary colliders are at Half-1 = 179 m, and the barrow's slope is 3 m over a 10 m blend. A player can walk round the mouth and up the mound.

Scenario: a player on step 1 stands on the grass at, say, (-112, 150). The 2D distance to the objective is about 3 m, so the step completes and the "The stair ends in a hall of roots..." narration plays above ground.

The suggested data-only fixes (move the point or shrink the radius) do not work: every point of the passage from the Gallery inward has walkable land above it. The fix has to be in code.
Fix: File: `New Unity Project/Assets/Crulanda/Scripts/Encounter/EncounterSession.cs`, function `TickQuests()` (line 382).

Make the visit predicate height-aware when the objective point lies in a cave passage well under the land. For example:

```csharp
var p = Player.transform.position;
bool Below(Vector2 at) { float f = 0; return Crulanda.World.Hollow.FloorUnder(at, ref f) && Zone.HeightAt(at.x, at.y) - f > 4; }   // the point is down in a passage, not on the land over it
bool down = Crulanda.World.Hollow.InsideAny(p + Vector3.up * .5f, .5f);
Quests.CheckVisits(o => (string.IsNullOrEmpty(o.zone) || o.zone == Zone.Zone.id)
    && (!o.night || Crulanda.World.WorldClock.Darkness > .5f)
    && Vector2.Distance(new Vector2(p.x, p.z), o.at) <= o.radius
    && (!Below(o.at) || down));
```

Surface objectives are unaffected, because `Below` is false wherever no passage floor lies more than 4 m under the land at the objective point. The existing `InHollow` helper in the same file can supply `down`.

No data change is needed. Optionally shrink the objective's radius in `Quests/verdant.json` from 8 to about 5 so it fires in the Gallery itself and not on the last flight of the stair.
