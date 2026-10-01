"""CLAUDE_HANDOFF: the publish of 2026-10-01 afternoon (replaces the 'STOPPED AT A CLEAN POINT' block)."""
import io
p = 'D:/code/mmo/New Unity Project/Docs/CLAUDE_HANDOFF.md'
s = io.open(p, encoding='utf-8', newline='').read()
crlf = '\r\n' in s; s = s.replace('\r\n', '\n')
a = s.index('**STOPPED AT A CLEAN POINT')
b = s.index('**The standing direction is in `GAME_BRIEF.md`')
new = """**PUBLISHED 2026-10-01 afternoon: THE ROOT-MOTHER'S DEEP, CROWSFOOT'S HIDDEN MOUTH, and EVERY TRADE HAS A DAY.**
Tests on the published code: EditMode 186/186, PlayMode 81/81, 0 shader errors; the tour of Oakhaven and the Verdant Shore
viewed (the errands line-up, the coop's water pan, the deep's stair, Sap Well and Heart). A read-only review (five reviewers,
each finding refuted or confirmed by a second) found a dozen real defects before publishing; all are fixed (CHANGELOG, "A review
of the deep and the trades' days"; the raw findings are in `tools\\wip\\review_724a22c_findings.md`).
- Small thing seen on the tour and left for the next publish: one of the Root-Mother's root limbs hangs straight down in front
  of her face (`RootDeepInterior`, the 14 limbs: skip the ones that start front-centre; the cavern has its own random stream).
- **NOW: THE PAINTED STYLE PASS** (Chris: "keep going with the painted style pass"). The first draft in `tools\\wip\\painted\\` was
  pre-checked by four agents (`precheck_results.md`: the draft textures did not tile and did not read as painted; trim floated;
  the rock helper's name collided) and restaged as patch scripts verified on a scratch copy, in `tools\\wip\\painted\\v2\\`:
  `p1` buildings (painted plaster, thatch, slate, masonry and timber on metre-UV walls; eaves, framed windows, plank doors),
  `p2` natural rock (`Crulanda/PaintedRock`, `art.rock`), `p3` sky, light and ground colour per zone. Apply in that order, then
  tests, a tour of all five zones, LOOK at the captures (nothing of this has been seen in the game yet), iterate, publish.
  Approved texture previews: the scratchpad's `painted-review\\textures\\v2_*.png` (regenerate with `v2.py`).
- `tools\\validation\\run_focus.ps1 -Filter '<fixtures>'` runs a few fixtures in three minutes; the full run is `run_tests.ps1`.

"""
s = s[:a] + new + s[b:]
s = s.replace('## RESUME HERE (updated 2026-10-01, morning)', '## RESUME HERE (updated 2026-10-01, afternoon)')
io.open(p, 'w', encoding='utf-8', newline='').write(s.replace('\n', '\r\n') if crlf else s); print('handoff ok')
