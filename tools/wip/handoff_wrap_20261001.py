"""CLAUDE_HANDOFF and memory at the wrap-up of 2026-10-01 night."""
import io, shutil
p = 'D:/code/mmo/New Unity Project/Docs/CLAUDE_HANDOFF.md'
s = io.open(p, encoding='utf-8', newline='').read()
crlf = '\r\n' in s; s = s.replace('\r\n', '\n')
a = s.index('**STOPPED 2026-10-01 evening')
b = s.index('**THE PLANS')
new = """**WRAPPED UP 2026-10-01 night at Chris's request. Main is AHEAD of the published build and NOT YET PUBLISHED.**
Main (commit after 48df0e1) has three merged, reviewed pieces on top of the published 078d276:
- **Trades step 2:** save format 8 (professions, pouches; migration from 7 with guards), 19 materials, pick and hatchet, vendor
  stock, `professions.json`, `Professions.cs`, the Trades window (K).
- **Trades step 1:** the new buildings (Carder farmhouse, Crisp cottage, Maud's leather shop, Lisbet's drying hut, the inn's
  kitchen lean-to, the game rack at Moss's lodge), named houses, Maud working at her shop. Temporary `VillageLife.UnsettledHouses`
  keeps the old home deal until step 3.
- **Loot step A1:** weapons and shields shown in hand (`GearLooks`, `Resources/Gear/looks.json`, `GearMeshes`, `GearMats`,
  `ActorVisual.Gear*.cs`, `GearBinder`, `WardrobeCapture`); empty slots show empty, so a new Warrior starts unarmed until the
  Trailblade.
Each was built in a worktree, reviewed by three lenses with every finding refuted or confirmed, and fixed (21 confirmed defects
fixed in all) before merging. **Tested so far: EditMode 221/221** (the run was stopped there). **Still to do before publishing:**
1. `run_tests.ps1` (PlayMode not yet run on this main), then `build_and_tour.ps1 -Zones zone.oakhaven,zone.khaven,zone.peaks,
   zone.ashrim,zone.verdant`, then `tools\\validation\\capture_extra.ps1` (the HUD captures incl. `17-trades.png` and the wardrobe
   line-up in `ui-captures\\wardrobe`: 01-weapon-rack-a..g, 02-shield-wall-a..c, 03-quality-ladder-a..c).
2. LOOK: the new buildings (landmark and trade shots, `oakhaven-97-trade-leatherworker/-herbalist`), the Trades window, every
   weapon and shield in the wardrobe shots, the player now unarmed by default.
3. Fix what shows, publish, backup, CHANGELOG entry (none written yet for these three).
Chris's save is backed up before format 8 (`hel/work/save-backups/20261001-1545-before-format8`); his save migrates to 8 the first
time the published build saves.

**NEXT ROUND (worktrees made, nothing built yet; branches at main):** `trades/a3-households` (BUILD_PLAN step 3),
`trades/b4-gathering` (step 4), `loot/a2-armour` (loot A2), `visual/w3-slopes-hero` (visual worklist items 10 and 11). Worktrees
in the session scratchpad `wt\\a3|b4|la2|v11` with `compile-<key>.py` beside them (`wt\\mk_compile.py <keys>` makes more). The
workflow that builds a round is saved at `.claude/projects/D--code-mmo/<session>/workflows/scripts/build-step-resume-*.js`
(modes: implement, finish, review); its last args (the briefs for these four) are in that run's journal. Do the publish of main
first, then start this round from the new main (recreate the branches if main moved).

"""
s = s[:a] + new + s[b:]
old_head = s[s.index('**THREE BUILD TRACKS IN FLIGHT'):s.index('**THE PLANS')] if '**THREE BUILD TRACKS IN FLIGHT' in s else ''
if old_head: s = s.replace(old_head, '')
io.open(p, 'w', encoding='utf-8', newline='').write(s.replace('\n', '\r\n') if crlf else s)
shutil.copy(p, 'C:/Users/chris/Documents/Codex/2026-09-28/hel/outputs/Crulanda-Claude-Handoff.md')
base = 'C:/Users/chris/.claude/projects/'
m = base + 'D--code-mmo/memory/crulanda-where-we-left-off.md'
t = io.open(m, encoding='utf-8').read()
head, body = t.split('\n---\n', 1)
head = head.replace([l for l in head.splitlines() if l.startswith('description:')][0], 'description: "Crulanda Unity MMO resume point (2026-10-01 night wrap-up): published build 078d276; main has trades steps 1-2 + loot A1 merged and reviewed but NOT published (EditMode 221/221, PlayMode not run); next round worktrees ready (households, gathering, armour, slopes/hero buildings)"')
x = body.index('**Stopped mid-round'); y = body.index('**Do next, in order**')
body = body[:x] + """**Wrapped up 2026-10-01 night.** Main is ahead of the published build: trades step 2 (save format 8, materials, tools, Trades window), trades step 1 (new buildings, named houses), loot A1 (weapons and shields in hand) are merged after review; EditMode 221/221, PlayMode and tours not yet run, NOT published. First thing next session: run tests, tour, `tools/validation/capture_extra.ps1` (HUD + wardrobe shots), LOOK, publish. Then the next round: worktrees `trades/a3-households`, `trades/b4-gathering`, `loot/a2-armour`, `visual/w3-slopes-hero` (nothing built yet). Details in the handoff's WRAPPED UP paragraph.

""" + body[y:]
io.open(m, 'w', encoding='utf-8').write(head + '\n---\n' + body)
shutil.copy(m, base + 'D--code/memory/crulanda-where-we-left-off.md')
line = "- [Crulanda: where we left off](crulanda-where-we-left-off.md) — 2026-10-01 night: published 078d276; main has trades 1-2 + loot A1 merged, unpublished (PlayMode not run); next: test, look, publish, then households/gathering/armour round"
for d in ['D--code-mmo', 'D--code']:
    q = base + d + '/memory/MEMORY.md'; u = io.open(q, encoding='utf-8').read()
    old = [l for l in u.splitlines() if 'crulanda-where-we-left-off' in l][0]
    io.open(q, 'w', encoding='utf-8').write(u.replace(old, line))
print('handoff and memory ok')
