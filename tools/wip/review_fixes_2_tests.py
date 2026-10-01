"""Test hardening from the review of 724a22c: the three flake/hang risks in VillageErrandTests."""
import io
p = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Tests\PlayMode\VillageErrandTests.cs'
s = io.open(p, encoding='utf-8', newline='').read()
reps = [
 # Wait on the carrier herself, not on whichever hen-wife delivers first.
 ('yield return WaitUntil(() => life.Count("inn.eggs") > 0, 120);',
  'yield return WaitUntil(() => life.Count("inn.eggs") > 0 && !carrier.Carrying, 120);   // she herself has handed over (three hen-wives are at it)'),
 # A bounded buy loop with gold to spare (it never yields: short gold would hang the run).
 ('int had = life.Count("stall.eggs"); session.Progress.gold += 50;', 'int had = life.Count("stall.eggs"); session.Progress.gold += 1000;'),
 ('while (life.Count("stall.eggs") > 0) session.Buy(EncounterSession.FreshEggs);',
  'for (int i = 0; i < 40 && life.Count("stall.eggs") > 0; i++) session.Buy(EncounterSession.FreshEggs);\n            Assert.AreEqual(0, life.Count("stall.eggs"), "The stall can be bought out.");'),
 # The farmers may already have delivered while the test watched the hen-wife: either sight counts.
 ('yield return WaitUntil(() => (farmer = life.Villagers.FirstOrDefault(v => v.Role == "farmer" && v.Carried == Load.Grain)) != null, 90);\n            Assert.NotNull(farmer, ',
  'yield return WaitUntil(() => life.Count("mill.grain") > 0 || (farmer = life.Villagers.FirstOrDefault(v => v.Role == "farmer" && v.Carried == Load.Grain)) != null, 90);\n            Assert.IsTrue(farmer != null || life.Count("mill.grain") > 0, '),
 ('Assert.Greater(life.Count("mill.grain"), 0, "and it reaches the mill (" + farmer.Activity + ").");',
  'Assert.Greater(life.Count("mill.grain"), 0, "and it reaches the mill (" + (farmer != null ? farmer.Activity : "already there") + ").");'),
]
for a, b in reps:
    assert s.count(a) == 1, a[:70]
    s = s.replace(a, b)
io.open(p, 'w', encoding='utf-8', newline='').write(s); print('TEST FIXES OK')
