"""CLAUDE_HANDOFF: the stop of 2026-10-01 ~02:05 (replaces the earlier 'STOPPED MID-STEP' paragraph)."""
import io, sys
p = r'D:\code\mmo\New Unity Project\Docs\CLAUDE_HANDOFF.md'
s = io.open(p, encoding='utf-8', newline='').read()
a = s.index('**STOPPED MID-STEP (Chris rebooting')
b = s.index('**The standing direction is in `GAME_BRIEF.md`')
focus = sys.argv[1] if len(sys.argv) > 1 else '(focused run result not recorded)'
new = """**STOPPED AT A CLEAN POINT (2026-10-01 ~02:05, Chris's call). NOT PUBLISHED: the playable build is still the Verdant Shore one.**
On `main`, unpublished: the Root-Mother's Deep, the hidden Crowsfoot mouth, the trades' daily schedules, and a first round of fixes
from a read-only review of that work.
- **Last full run (before the fixes below):** EditMode 186/186, PlayMode 80/81, 0 shader errors. The one failure was real and is
  fixed: nobody could draw water because a Concord collector stands 7 m from the well and villagers kept 12 m from enemies
  (`VillageLife.KeepClear` is 7 now; a blocked errand says so).
- **Focused run after the fixes** (`tools\\validation\\run_focus.ps1`, the village, deep and Oakhaven quest fixtures): FOCUS_RESULT
- **The two "new failures" of the earlier stop were false:** `run_focus.ps1` mirrored Assets without registering the content, so
  the session had no item database. It registers now, as `run_tests.ps1` does.
- **Review fixes applied** (`tools\\wip\\review_fixes_1.py`, `review_fixes_2_tests.py`; the raw findings and verdicts are in
  `tools\\wip\\review_724a22c_findings.md`): "Grey " no longer turns every Grey wolf into an ash hound; the hens' water shows (the
  disc sat inside a solid pan); `main.verdant.5` awards its page (`documents`, a list); the Root-Mother's roots go down into the
  floor, not 14 m up; the village's stock starts again before dawn; a delivery home says its line at the door before going in;
  the hen-wife only speaks of eggs that got there; three flaky spots in `VillageErrandTests`.
- **Review findings still OPEN, do these before publishing:**
  1. The Root-Mother's upper knot, face and eyes sit above the passage roof at the Heart's tapered end (`RootDeepInterior`,
     `sEnd = h.Length - 3.5f`). Move the seat about 7 m into the hall (roof 8 m there) and move the camps "The Root-Warden"
     (plan (-1.5, 90)) and "The Heart's withered" (plan (-1, 83)) clear of the knot's `Block`; rerun `RootDeepTests`, look at
     `verdant-86-hollow-hall.png`.
  2. "The cold in the root" is `kind = "crates"` without `once`: salted, it vanishes and comes back after 90 s. Make it stay
     salted (see `EncounterSession.UseInteractable`, `ZoneInteractable.once`/`Vanishes`).
  3. `main.verdant.5`'s kills are credited by the surface camps that share the tags (`withered`, `mistwalker`): give the deep's
     camps their own tags (and loot tables), or accept it. Its first step (visit, radius 8) completes on the barrow above the
     Gallery: needs a depth check or a smaller radius.
- **Then:** the full run (`run_tests.ps1`), the tour (`build_and_tour.ps1 -Zones zone.oakhaven,zone.verdant`), view
  `oakhaven-99-errands-lineup-*.png`, `oakhaven-90-coop-morning.png` (the pan) and the deep's `verdant-85/86/88/89`, publish,
  `tools\\Backup.ps1`, tell Chris.
- **Backup:** after the reboot J: came up as a different, nearly empty drive; Chris was moving the backup files back onto it.
  BACKUP_RESULT
- **Next job after the publish: the painted style pass**, prepared in `tools\\wip\\painted\\`:
  - P1 buildings: `ZoneSceneBuilder.Painted.cs` goes in `Assets/Crulanda/Editor/` (make the class `public static partial class
    ZoneSceneBuilder`, call `PaintedTextures(art)` at the end of `EnsureArt`); `ZoneArt` gains `public Material masonry, rock;`;
    `patch_buildings.py` adds `ZoneMeshes.Box` (metre UVs), `ZoneBuilder.BoxPart`, eaves, framed windows, plank doors.
  - P2 rock: `PaintedRock.shader` goes in `Assets/Crulanda/World/Shaders/` (with a new .meta), `patch_rock.py` switches natural
    rock to `art.rock`. Then sky and ground colour per zone (each keeps its mood), tiled masonry on towers and the keep.
  - P3 chunky props. View captures after each part; none of this has been built or seen yet.

"""
s = s[:a] + new + s[b:]
s = s.replace('FOCUS_RESULT', focus)
io.open(p, 'w', encoding='utf-8', newline='').write(s); print('handoff ok')
