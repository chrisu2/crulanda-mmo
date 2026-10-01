"""CLAUDE_HANDOFF and memory: the three build tracks in flight (2026-10-01 evening)."""
import io, shutil
p = 'D:/code/mmo/New Unity Project/Docs/CLAUDE_HANDOFF.md'
s = io.open(p, encoding='utf-8', newline='').read()
crlf = '\r\n' in s; s = s.replace('\r\n', '\n')
a = s.index('**PUBLISHED 2026-10-01 (four publishes')
b = s.index('**The standing direction is in `GAME_BRIEF.md`')
new = """**PUBLISHED 2026-10-01 (five publishes; the playable build is commit 078d276):**
1. The Root-Mother's Deep, Crowsfoot's hidden mouth, every trade has a day (4c579e6).
2. The painted style pass parts 1-4 (03fd06d) and 3. part 5, painted masonry (17168d8).
4. The out of work drink at the inn; the visual review's first batch (ecc233b).
5. The visual review's second batch: Ash Rim, world edge, ruins, Peaks, caves (078d276).
Tests on 078d276: EditMode 186/186, PlayMode 83/83, 0 shader errors. Main has since gained an inn shot of the drinkers
(`oakhaven-99-inn-drinkers.png`) and the plan documents; nothing unpublished changes play.

**THREE BUILD TRACKS IN FLIGHT (each a git worktree in the scratchpad `wt` folder, on its own branch; `git worktree list`):**
- `trades/a1-buildings` (worktree `wt/a1`): professions BUILD_PLAN step 1, the village's new buildings and named houses.
- `trades/b2-format8` (worktree `wt/b2`): BUILD_PLAN step 2, save format 8, materials, tools, the Trades window.
- `loot/a1-weapons` (worktree `wt/la1`): loot DESIGN step A1, weapons and shields in hand, the wardrobe capture.
Each is built by a workflow (`build-step` / `trades-build-round`): implement -> three review lenses -> every finding refuted or
confirmed -> fix; the result names the branch head. THEN THE LEAD: `git merge --no-ff <branch>` into main one at a time (shared
files: `ZoneBuilder.cs`, `ZoneDefinition.cs`, `EncounterSession.cs`, `WorldLife.cs`, `items.json`, `oakhaven.json`), offline
compile, `build_art.ps1` if art changed, `run_tests.ps1`, tour (and `Crulanda.exe --crulanda-wardrobe-capture <dir>` for loot),
LOOK, fix, publish, backup, CHANGELOG, then start the next round from the plan's "Order at a glance" table.
If a session dies mid-round: the branches hold whatever was committed; check `git log main..<branch>` and the workflow journal
under `.claude/projects/D--code-mmo/<session>/subagents/workflows/`, and either rerun the step or finish it by hand.

**THE PLANS (read the owner notes first; they are his words and decisions):**
- Professions, gathering, households, purses, bags, hunting: `tools/wip/professions/OWNER_NOTES.md`, `DESIGN.md`, `ADDENDUM.md`,
  `BUILD_PLAN.md` (14 publishable steps on two tracks: A village 1, 3, 6, 7; B professions 2, 4, 5, 8-14).
- Loot and worn appearances: `tools/wip/loot/OWNER_NOTES.md`, `DESIGN.md` (steps A1-A3 run beside professions; L1-L6 land at
  professions step boundaries because they share the item code), `ITEMS_V1.md` (104 named items).
- Defaults told to Chris and being built unless he objects: the leatherworker is Maud (her family: Fen the skinner, Nettie);
  invented kin so every villager has a household, two new houses; a hide comes from searching the body (no skinning skill);
  only a Blacksmith smelts; low skill never blocks a node; empty gear slots show empty; cloaks as a tenth slot last; boss trophies
  become "one you do not own yet". To raise when L-steps start: a new character should not be left empty-handed (start with the
  training blade equipped).
- Chris's save is backed up before format 8: `hel/work/save-backups/20261001-1545-before-format8`.

**Visual worklist left** (`tools/wip/painted/visual_review.md`): item 10 (buildings on slopes) and item 11 (the inn and the smithy
as hero buildings), held until BUILD_PLAN step 1 has merged because it edits the same builders.

**How today's work was done, worth repeating:** design by a panel (readers map the code, three lenses design, one synthesis),
critique, then build in worktrees with review and refutation before merging; for visual work, agents render and LOOK at generated
textures before anything is built, patches are verified on scratch copies, and every batch is toured and looked at before it is
published. Never edit Assets while a run is in flight (the tour mirrors Assets again). `run_focus.ps1 -Filter '<fixtures>'` runs
a few fixtures fast; `build_art.ps1` builds generated art in the validation copy and brings it into the repo.

**Chris asked whether to move to Unreal** (2026-10-01): answered no (the art, not the engine, is the limit; a port rewrites
everything; a Unity lighting-pipeline trial is the cheap experiment). He did not ask for the trial.

"""
s = s[:a] + new + s[b:]
io.open(p, 'w', encoding='utf-8', newline='').write(s.replace('\n', '\r\n') if crlf else s)
shutil.copy(p, 'C:/Users/chris/Documents/Codex/2026-09-28/hel/outputs/Crulanda-Claude-Handoff.md')
base = 'C:/Users/chris/.claude/projects/'
m = base + 'D--code-mmo/memory/crulanda-where-we-left-off.md'
t = io.open(m, encoding='utf-8').read()
head, body = t.split('\n---\n', 1)
head = head.replace([l for l in head.splitlines() if l.startswith('description:')][0], 'description: "Crulanda Unity MMO resume point (2026-10-01 evening): five publishes today (build 078d276); three build tracks in flight in git worktrees (village buildings, save format 8 + Trades window, loot weapons in hand); plans in tools/wip/professions and tools/wip/loot"')
x = body.index('**State.**'); y = body.index('**Working method')
state = """**State.** Five publishes on 2026-10-01; the playable build is commit 078d276 (daily jobs and errands, the Root-Mother's Deep, the painted style pass with masonry and props, drinkers at the inn, both batches of the visual review). Tests 186/186 EditMode, 83/83 PlayMode.

**In flight** (git worktrees under the session scratchpad `wt` folder; see `git worktree list` and the handoff): `trades/a1-buildings` (professions BUILD_PLAN step 1), `trades/b2-format8` (step 2), `loot/a1-weapons` (loot DESIGN step A1). The lead merges each branch into main, compiles, tests, tours, LOOKS, publishes, then starts the next round from the plan's "Order at a glance".

**Do next, in order** (Chris's rules: [[crulanda-autonomy]]; tell him at each publish):
1. Finish the round in flight (merge, test, publish), then keep going down `tools/wip/professions/BUILD_PLAN.md` (14 steps, two tracks) and `tools/wip/loot/DESIGN.md` section 9 (A1-A3 beside professions; L1-L6 at professions step boundaries). Read each folder's `OWNER_NOTES.md` first: Chris's own words and decisions (gather/refine/craft, two crafts, leatherworker's bags by quest or purchase, NPC purses, one house per household, a workshop per trade, wild animals huntable but never farm animals or cats; gear that shows when worn, a loot database).
2. Visual worklist items 10 and 11 (`tools/wip/painted/visual_review.md`) after BUILD_PLAN step 1 merges.
3. Then the Verdant extras, weather polish, audio.

"""
body = body[:x] + state + body[y:]
io.open(m, 'w', encoding='utf-8').write(head + '\n---\n' + body)
shutil.copy(m, base + 'D--code/memory/crulanda-where-we-left-off.md')
line = "- [Crulanda: where we left off](crulanda-where-we-left-off.md) — 2026-10-01 evening: build 078d276 published; three build tracks in worktrees (village buildings, format 8 + Trades, loot weapons); plans in tools/wip/professions and tools/wip/loot"
for d in ['D--code-mmo', 'D--code']:
    q = base + d + '/memory/MEMORY.md'; u = io.open(q, encoding='utf-8').read()
    old = [l for l in u.splitlines() if 'crulanda-where-we-left-off' in l][0]
    io.open(q, 'w', encoding='utf-8').write(u.replace(old, line))
print('handoff and memory ok')
