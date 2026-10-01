"""Fixes after the first full check of the merged trades steps 1-2 and loot A1 (2026-10-01): held gear at heroic scale, the
   wardrobe framed closer, house door points on the step, the Maud test's wait."""
import io
A = 'D:/code/mmo/New Unity Project/Assets/Crulanda/'
def patch(rel, reps):
    p = A + rel; s = io.open(p, encoding='utf-8', newline='').read()
    crlf = '\r\n' in s; s = s.replace('\r\n', '\n')
    for a, b in reps:
        assert s.count(a) == 1, (rel, a[:60], s.count(a))
    for a, b in reps: s = s.replace(a, b)
    io.open(p, 'w', encoding='utf-8', newline='').write(s.replace('\n', '\r\n') if crlf else s); print('patched', rel)

patch('Scripts/Encounter/ActorVisual.Gear.cs', [
 ('            t.SetParent(parent, false); t.localPosition = pos; t.localRotation = rot;\n            return t;',
  '            t.SetParent(parent, false); t.localPosition = pos; t.localRotation = rot;\n'
  '            // Heroic proportions, the classic-MMO way: what is held reads larger than life (in the first wardrobe line-up a life-size\n'
  '            // blade at the hip read as a twig). The grip stays in the fist; the piece grows out from it.\n'
  '            if (main) t.localScale = Vector3.one * HeldScale; else if (slot == EquipSlot.OffHand) t.localScale = Vector3.one * (hung ? 1.1f : ShieldScale);\n'
  '            return t;'),
 ('        static bool Tall(string family) { return family == "polearm" || family == "staff"; }',
  '        /// <summary>How much larger than life a held weapon and a shield are drawn.</summary>\n'
  '        public const float HeldScale = 1.35f, ShieldScale = 1.15f;\n'
  '        static bool Tall(string family) { return family == "polearm" || family == "staff"; }'),
])
patch('Scripts/Encounter/WardrobeCapture.cs', [
 ('        const int PerRow = 7; const float Spacing = 1.6f;', '        const int PerRow = 5; const float Spacing = 1.7f;   // five to a shot, framed close enough to judge a guard or a rim'),
])
patch('Scripts/World/ZoneBuilder.cs', [
 ('            if (!inn) { front = new ZoneDoor { name = t.name, openable = false, position = t.TransformPoint(new Vector3(0, 1, -d / 2 - .1f)) }; Doors.Add(front); }',
  '            // The door\'s point stands on the step in front of it: behind the slab, between door and wall, the navmesh can leave a\n'
  '            // pocket of its own that a villager going home cannot path to (the Crisp cottage, 2026-10-01).\n'
  '            if (!inn) { front = new ZoneDoor { name = t.name, openable = false, position = t.TransformPoint(new Vector3(0, 1, -d / 2 - .75f)) }; Doors.Add(front); }'),
])
patch('Tests/PlayMode/VillageWorkshopTests.cs', [
 ('                Time.timeScale = 3;\n                Vector3? at = null;',
  '                Time.timeScale = 4;   // she may start on the green, 40 m off; a game hour is 25 s at this speed\n                Vector3? at = null;'),
 ('return at.HasValue; }, 30);', 'return at.HasValue; }, 75);'),
])
print('FIXES OK')
