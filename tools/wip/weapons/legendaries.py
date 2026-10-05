"""Five legendary weapons (2026-10-05, Chris: the glowing weapons "epic or legendary only"), one per zone's chief boss, each
a 0.5% lucky drop by day, 0.65% by night, all GAME-ONLY, on Blink's brightest blades and the RPG pack's epic staff. Stats on the generated curve at
legendary (power 2.5x the curve level; 7 stat points per k). Safe to rerun."""
import io, lootfile
A = r'D:\code\mmo\New Unity Project\Assets\Crulanda'
LEG = [
 ('oakhaven','drop.oak.caddock','Caddock, the Bandit King','loot.oak.kingsbane','Kingsbane, the Last Tithe',4,'model.weapon:Sword13_Orange/sandthrone',{'stamina':10,'strength':12},'The tithe it took was always more than the law allowed. It burns still.'),
 ('khaven','drop.kha.reckoner','The Pale Reckoner','loot.kha.the_reckoners_frost',"The Reckoner's Frost",6,'model.weapon:Sword15_Frost/pale',{'stamina':14,'strength':10,'agility':7},'Cold enough to stop a count in the middle. Whose, it does not say.'),
 ('peaks','drop.pea.captain','Sandthrone captain','loot.pea.the_toll_unpaid','The Toll Unpaid',7,'model.tall:StaffOfPain/tollroad',{'stamina':12,'intellect':16,'spirit':7},'Every coin the gate ever took is still owed to someone. The staff remembers who.'),
 ('ashrim','drop.ash.deacon','The Ash-Deacon','loot.ash.cinderheart','Cinderheart',9,'model.weapon:Sword15_Lava/cult',{'stamina':18,'strength':26},"The Deacon's last sermon, hammered into an edge. It has not stopped burning."),
 ('verdant','drop.ver.rootwarden','The Hollow Root-Warden','loot.ver.the_green_wrath','The Green Wrath',12,'model.weapon:Sword15_Earth/veridian',{'stamina':24,'strength':22,'agility':11},"The forest's patience is long. This is what is left when it runs out."),
]
base = io.open(A + r'\Tests\EditMode\LootIdBaseline.txt', encoding='utf-8').read(); add = []
for zone, drop, mob, iid, name, lvl, look, stats, desc in LEG:
    curve = lvl + 1; power = curve * 2.5; dmg = max(1, round(3 + power * 1.6)); value = max(1, round(curve * 6 * 1.4)); budget = round(7 * max(1, power / 4))
    assert sum(stats.values()) == budget, (iid, budget)
    p = A + r'\EncounterContent\Items\loot.%s.json' % zone; d = lootfile.load(p)
    if not any(i['id'] == iid for i in d['items']):
        it = {"id": iid, "name": name, "kind": "gear", "slot": "mainhand", "quality": 5, "level": lvl, "value": value, "weaponDamage": dmg}; it.update(stats)
        it.update({"description": desc, "canonStatus": "GAME-ONLY"})
        d['items'].append(it); d['gear'].append({"id": iid, "look": look, "source": "boss:" + mob})
        next(x for x in d['drops'] if x['id'] == drop)['groups'].append({"chance": 0.005, "lucky": True, "pick": [{"item": iid}]})
        lootfile.save(p, d)
    if iid not in base: add.append(iid)
if add: io.open(A + r'\Tests\EditMode\LootIdBaseline.txt', 'a', encoding='utf-8').write(('' if base.endswith('\n') else '\n') + '\n'.join(add) + '\n')
print('ok')
