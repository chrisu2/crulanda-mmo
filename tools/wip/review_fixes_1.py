"""Fixes from the read-only review of 724a22c (2026-10-01): the ones that are small and certain."""
import io, json
A = r'D:\code\mmo\New Unity Project\Assets\Crulanda'
def patch(rel, reps):
    p = A + '\\' + rel
    s = io.open(p, encoding='utf-8', newline='').read()
    for a, b in reps:
        assert s.count(a) == 1, (rel, a[:80])
        s = s.replace(a, b)
    io.open(p, 'w', encoding='utf-8', newline='').write(s); print('patched', rel)

# 1. "Grey " made every Grey wolf an ash hound (and the Grey Sexton odd): the Verdant mobs are covered by the other words.
patch(r'Scripts\Encounter\EncounterSession.cs', [
    ('new[] { "Ash", "Withered", "Greyheart", "Hollow Root", "Grey ", "doe" }', 'new[] { "Ash", "Withered", "Greyheart", "Hollow Root", "doe" }'),
])
# 2. The hens' water never showed: the disc sat inside a solid pan. A shallow pan, the water lying in its top.
patch(r'Scripts\World\ZoneBuilder.cs', [
    ('Part(PrimitiveType.Cylinder, pan, new Vector3(0, .08f, 0), new Vector3(.8f, .08f, .8f), Tint(art.metal, new Color(.3f, .3f, .32f)));',
     'Part(PrimitiveType.Cylinder, pan, new Vector3(0, .05f, 0), new Vector3(.8f, .05f, .8f), Tint(art.metal, new Color(.3f, .3f, .32f)));'),
    # 4. The Root-Mother's roots "into the floor" went up to the mouth's ground level (the prop's y = 0), 12-16 m over the Heart.
    ('k % 3 == 0 ? 3 + D() * 3 : -from.y + .1f,', 'k % 3 == 0 ? 3 + D() * 3 : seat.y + .1f - from.y,'),
])
patch(r'Scripts\World\WorldClock.cs', [
    ('var p = water.localPosition; p.y = .05f + Level * .09f; water.localPosition = p;   // from the rim down into the pan',
     'var p = water.localPosition; p.y = .092f + Level * .02f; water.localPosition = p;   // lying in the pan\'s top (the pan is solid: 0 to .1), sinking as it dries'),
])
patch(r'Scripts\Encounter\WorldLife.cs', [
    # 7. The village's stock is the day's: it starts again before dawn, with the hens' count.
    ("""            nextLay = Time.time + 20;
            foreach (var coop in Zone.Coops)""",
     """            nextLay = Time.time + 20;
            if (WorldClock.Between(4, 5)) Stock.Clear();   // the stock is the day's deliveries: yesterday's are eaten, sold or burnt
            foreach (var coop in Zone.Coops)"""),
    # 9. The hen-wife spoke of eggs at the stall or the inn when none had got there (an errand is marked done when it starts).
    ('case "henwife": return v.Done("eggs to the stall") ?', 'case "henwife": return Count("stall.eggs") > 0 ?'),
    (': v.Done("eggs to the inn") ?', ': Count("inn.eggs") > 0 ?'),
    # 8. A delivery home went indoors in the same breath, wiping the line: say it at the door, then go in.
    ('if (e.to == "home" && home != null) { activity = "home"; until = Time.time + 20; Hide(); }',
     'if (e.to == "home" && home != null) { activity = "home"; goingIn = true; until = Time.time + 4; }'),
    ("""                    if (errand != null && leg == 0) { leg = 1; Go(dropAt); }   // picked up: carry it over
                    else ChooseNext();""",
     """                    if (errand != null && leg == 0) { leg = 1; Go(dropAt); }   // picked up: carry it over
                    else if (goingIn) { goingIn = false; until = Time.time + 20; Hide(); }   // said at the door; now indoors a while
                    else ChooseNext();"""),
    ('GameObject load; Load loadKind; int loadCount; Errand errand; int leg; Vector3 dropAt;', 'GameObject load; Load loadKind; int loadCount; Errand errand; int leg; Vector3 dropAt; bool goingIn;'),
    ('        void CancelErrand() { errand = null; DropLoad(); }', '        void CancelErrand() { errand = null; goingIn = false; DropLoad(); }'),
])
# 3. The finale's page was never awarded: rewards reads "documents" (a list), not "document".
p = A + r'\EncounterContent\Quests\verdant.json'
q = json.loads(io.open(p, encoding='utf-8').read())
for x in q['quests']:
    if x['id'] == 'main.verdant.5':
        r = x['rewards']; d = r.pop('document', None)
        if d: r['documents'] = [d]; print('rewards.documents =', r['documents'])
io.open(p, 'w', encoding='utf-8', newline='').write(json.dumps(q, indent=2, ensure_ascii=False) + '\n')
print('REVIEW FIXES OK')
