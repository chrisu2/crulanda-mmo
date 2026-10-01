"""CLAUDE_HANDOFF: 2026-10-01 evening (replaces the block from 'PUBLISHED 2026-10-01 (two publishes)' down to the standing direction)."""
import io, shutil
p = 'D:/code/mmo/New Unity Project/Docs/CLAUDE_HANDOFF.md'
s = io.open(p, encoding='utf-8', newline='').read()
crlf = '\r\n' in s; s = s.replace('\r\n', '\n')
a = s.index('**PUBLISHED 2026-10-01 (two publishes)')
b = s.index('**The standing direction is in `GAME_BRIEF.md`')
BS = chr(92)
new = """**PUBLISHED 2026-10-01 (four publishes; the playable build is commit ecc233b):**
1. The Root-Mother's Deep, Crowsfoot's hidden mouth, every trade has a day (4c579e6).
2. The painted style pass parts 1-4: painted buildings, painted rock, sky and colour, chunky props (03fd06d).
3. Part 5: painted masonry on towers, walls, the gate, the keep, crypts, ruins (17168d8).
4. The out of work drink at the inn (VillageDrinkTests), and the visual review's first batch: Oakhaven's blue air, storms that
   darken, firelit night, leafy crowns, the meadow, gables, hedges and haystacks (ecc233b).
Tests on ecc233b: EditMode 186/186, PlayMode 83/83 (the full run was 82/83 on a flaw in the new drinker test itself, corrected
and rerun). Design: `WORLD_ZONES.md` "The painted style pass" and "Life, day and night"; the CHANGELOG.

**IN FLIGHT when this was written:** the visual review's SECOND batch (`tools/wip/painted/v2/w2`: ash, edge, ruins, peaks, caves)
is APPLIED in the working tree, its cave art built (`build_art.ps1`), with the full run and a five-zone tour running. If it is not
committed as published: `run_tests.ps1`, tour, LOOK (ashrim-01/03, the *-exit shots, the ruins, peaks-01/07, the eight cave
shots), fix, publish. The worklist is `tools/wip/painted/visual_review.md` (items 10 and 11 are still open: buildings on slopes;
the inn and the smithy as hero buildings).

**NEXT BIG JOB (Chris, 2026-10-01): PLAYER PROFESSIONS, GATHERING AND THE VILLAGE ECONOMY.** Everything he said and decided is
in `tools/wip/professions/OWNER_NOTES.md` (read it first): gather ore, lumber and herbs, sell or refine; two crafting professions
chosen from Blacksmith and Alchemist with Cooking and gathering free; the leatherworker makes profession bags by quest (bring
leathers) or purchase and spends his coin on bread and firewood for his family; every tradesperson has a purse; one named house
per household; a workshop per trade; an innkeeper; wild animals huntable and every beast skinnable for leather; farm animals and
cats never huntable. Design documents: `tools/wip/professions/DESIGN.md` (a three-way design panel's synthesis), `ADDENDUM.md`
and `BUILD_PLAN.md` (households, workshops, purses, bags; one integrated build order in small publishable steps; if those two are
missing, rerun the `professions-addendum` workflow). Hunting and skinning came after the addendum was commissioned: make sure the
build plan has them. The save format will change (format 8): BACK UP CHRIS'S SAVE FIRST (`hel/work/save-backups`).

**How today's work was done, worth repeating:** draft -> agents pre-check (port generated textures to Python and LOOK; recompute
geometry; a second agent tries to refute each finding) -> restage as anchor-based patch scripts verified by compiling a scratch
copy (`tools/wip/painted/v2/...`) -> apply one batch -> `build_art.ps1` if art changed -> full tests -> tour -> LOOK -> publish.
Before publishing gameplay, a read-only review workflow found a dozen real bugs the tests missed. Never edit Assets while a run
is in flight (the tour mirrors Assets again): stage in `tools/wip`. `run_focus.ps1 -Filter '<fixtures>'` runs a few fixtures fast.

**Chris asked whether to move to Unreal** (2026-10-01): answered no (the art, not the engine, is the limit; a port rewrites
everything; a Unity lighting-pipeline trial is the cheap experiment). He did not ask for the trial.

"""
s = s[:a] + new.replace('/', '/') + s[b:]
io.open(p, 'w', encoding='utf-8', newline='').write(s.replace('\n', '\r\n') if crlf else s)
shutil.copy(p, 'C:/Users/chris/Documents/Codex/2026-09-28/hel/outputs/Crulanda-Claude-Handoff.md')
base = 'C:/Users/chris/.claude/projects/'
m = base + 'D--code-mmo/memory/crulanda-where-we-left-off.md'
t = io.open(m, encoding='utf-8').read()
head, body = t.split('\n---\n', 1)
head = head.replace([l for l in head.splitlines() if l.startswith('description:')][0], 'description: "Crulanda Unity MMO resume point (2026-10-01 evening): four publishes today (jobs, the deep, painted pass, drinkers; build ecc233b); visual batch 2 applied and in test; NEXT BIG JOB: player professions, gathering, households, NPC purses (tools/wip/professions)"')
x = body.index('**State.**'); y = body.index('**Working method')
state = """**State.** Four publishes on 2026-10-01; the playable build is commit ecc233b: the Root-Mother's Deep and hidden Crowsfoot mouth; `VillageWork.cs` daily jobs and errands; the painted style pass (painted buildings, rock, masonry, sky/colour, chunky props); the out-of-work drinking at the inn; the visual review's first batch. Tests 186/186 EditMode, 83/83 PlayMode.

**Do next, in order** (Chris's rules: [[crulanda-autonomy]]; tell him at each publish):
1. Visual batch 2 (`tools/wip/painted/v2/w2`: Ash Rim ground, world edge, ruins, Peaks, caves) was APPLIED in the working tree with tests and a tour running: if not committed as published, run tests, tour, LOOK, publish. Worklist: `tools/wip/painted/visual_review.md` (items 10, 11 still open).
2. **Player professions, gathering and the village economy** (Chris's big request, 2026-10-01): read `tools/wip/professions/OWNER_NOTES.md` (his words and decisions), then `DESIGN.md`, `ADDENDUM.md`, `BUILD_PLAN.md`. Build it in the plan's small publishable steps; each step: implement, review workflow, tests, publish. Save format 8: back up Chris's save first.
3. Then the Verdant extras, weather polish, audio.

"""
body = body[:x] + state + body[y:]
io.open(m, 'w', encoding='utf-8').write(head + '\n---\n' + body)
shutil.copy(m, base + 'D--code/memory/crulanda-where-we-left-off.md')
line = "- [Crulanda: where we left off](crulanda-where-we-left-off.md) — 2026-10-01 evening: build ecc233b published (jobs, the deep, painted pass, drinkers); visual batch 2 in test; NEXT: player professions, gathering, households, NPC purses (tools/wip/professions)"
for d in ['D--code-mmo', 'D--code']:
    q = base + d + '/memory/MEMORY.md'; u = io.open(q, encoding='utf-8').read()
    old = [l for l in u.splitlines() if 'crulanda-where-we-left-off' in l][0]
    io.open(q, 'w', encoding='utf-8').write(u.replace(old, line))
print('handoff and memory ok')
