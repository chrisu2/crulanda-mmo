# Pending patches (prepared 2026-09-29, not applied yet)

Diagnose-and-critique workflows prepared these edits for the next steps of the visual review
(`New Unity Project/Docs/VISUAL_REVIEW_2026-09-29.md`). A second agent checked each file. The `review.correctedEdits` list
replaces `diagnosis.edits` when the verdict is `fixable`.

| File | Step | Clusters |
|---|---|---|
| `step3-props-landmarks-creatures.json` | 3 | floating-props, creatures-npcs (the Pale's floating head, the Weave-Eater remodel to canon), landmarks-oak-khaven, landmarks-peaks-ash, capture-framing |
| `step4-hud.json` | 4 | overlaps (bubbles, labels, nameplates: line of sight, de-overlap, beast heights), readability (tracker backing, hint fade, chat panel, text outlines) |
| `step5-water-realism.json` | 5 | reflections (planar mirror camera `WaterReflection.cs`: `newFiles`/`correctedNewFiles` hold new files), surface-detail (ripples, sparkle) |

**How to apply one step:**

```powershell
.\apply_patches.ps1 -File .\step3-props-landmarks-creatures.json -Keys floating-props, creatures-npcs, landmarks-oak-khaven, landmarks-peaks-ash, capture-framing
```

- Every edit must match exactly once, or it is **skipped and reported**.
- These patches were written against older code. Step 2 and its fix round changed `ZoneBuilder.cs` afterwards, so expect some skips and merge those by hand.
  - Read the skipped edit's `why` field.
  - Watch for trailing comments that swallow a following statement (this caused a `seat` compile error once).
- New files (`newFiles`) are not created by the script; write them yourself.
- Then run `..\validation\run_tests.ps1` and `..\validation\build_and_tour.ps1 -Zones ...`, check `Shader error` in the build log, verify the screenshots, commit, publish and back up.
