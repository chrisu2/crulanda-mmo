"""CLAUDE_HANDOFF: the painted pass publish of 2026-10-01 (replaces the block from the jobs publish down to the standing direction)."""
import io
p = 'D:/code/mmo/New Unity Project/Docs/CLAUDE_HANDOFF.md'
s = io.open(p, encoding='utf-8', newline='').read()
crlf = '\r\n' in s; s = s.replace('\r\n', '\n')
a = s.index("**PUBLISHED 2026-10-01 afternoon: THE ROOT-MOTHER'S DEEP")
b = s.index('**The standing direction is in `GAME_BRIEF.md`')
new = """**PUBLISHED 2026-10-01 (two publishes): (1) THE ROOT-MOTHER'S DEEP, CROWSFOOT'S HIDDEN MOUTH and EVERY TRADE HAS A DAY;
(2) THE PAINTED STYLE PASS, parts 1-4** (commit 03fd06d): painted buildings and trim, painted natural rock, sky and colour per
zone, chunky props. Tests on the published code: EditMode 186/186, PlayMode 81/81, 0 shader errors; all five zones toured and
viewed. Design: `WORLD_ZONES.md` "The painted style pass", the CHANGELOG.
- **In flight when this was written:** part 5 (`tools\\wip\\painted\\v2\\p5\\patch_p5.py`, painted masonry with metre UVs on towers,
  curtain walls, the gate, the keep, crypts, ruins, wayshrines; headstones and waystones as painted rock) is APPLIED in the working
  tree; the full run and a five-zone tour were running. If it is not committed as published: run `run_tests.ps1`, tour, look at
  `peaks-03-the-toll-gate.png`, `khaven-04`, `ashrim-01`, the crypts and ruins, then publish.
- **A visual review of the published captures** (four reviewers, one ranked worklist) was also running; its worklist is the next
  job's input. If it is not in `tools\\wip\\painted\\visual_review.md`, rerun the workflow or review the captures in
  `world-captures\\archive-20261001-painted-published-p4`.
- **How the painted pass was done, worth repeating:** draft -> agents pre-check (port generated textures to Python and LOOK at
  them; recompute geometry; refute each finding) -> restage as anchor-based patch scripts verified by compiling a scratch copy
  (`tools\\wip\\painted\\v2\\pN`) -> apply one part -> `build_art.ps1` if art changed -> tests -> tour -> LOOK -> fix -> publish.
  Never edit Assets while a run is in flight (the tour mirrors Assets again); stage in `tools\\wip`.
- `tools\\validation\\run_focus.ps1 -Filter '<fixtures>'` runs a few fixtures in three minutes; `build_art.ps1` builds generated
  art in the validation copy and brings it into the repo.
- Then: the Verdant extras (mist banks, glass-frogs, a Temple root-stair prop, the sea backdrop), a `hollow` mist-walker
  variant, weather polish, audio. Steam-blurb decisions are still Chris's (store title; whether The First Spoke is out).

"""
s = s[:a] + new + s[b:]
io.open(p, 'w', encoding='utf-8', newline='').write(s.replace('\n', '\r\n') if crlf else s); print('handoff ok')
