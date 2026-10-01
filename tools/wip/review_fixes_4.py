"""Review item 3: the finale's kills are the deep's own (their own tags and loot), and a place down a cave is only visited
   from down in the cave, not from the hill over it."""
import io, json, copy
A = 'D:/code/mmo/New Unity Project/Assets/Crulanda/'
def load(rel): return json.loads(io.open(A + rel, encoding='utf-8').read())
def save(rel, d): io.open(A + rel, 'w', encoding='utf-8', newline='').write(json.dumps(d, indent=2, ensure_ascii=False) + '\n')

z = load('EncounterContent/Zones/verdant.json')
retag = {'Gallery withered': 'deepwithered', "The Heart's withered": 'deepwithered', 'Sap Well walkers': 'deepwalker'}
for c in z['camps']:
    if c['name'] in retag: c['tag'] = retag[c['name']]; print('camp', c['name'], '->', c['tag'])
save('EncounterContent/Zones/verdant.json', z)

it = load('EncounterContent/Items/items.json')
tags = {t['tag']: t for t in it['loot']}
for new, old in (('deepwithered', 'withered'), ('deepwalker', 'mistwalker')):
    if new not in tags:
        t = copy.deepcopy(tags[old]); t['tag'] = new; it['loot'].append(t); print('loot', new)
save('EncounterContent/Items/items.json', it)

q = load('EncounterContent/Quests/verdant.json')
for x in q['quests']:
    if x['id'] != 'main.verdant.5': continue
    for st in x['steps']:
        for o in st['objectives']:
            if o.get('target') == 'mob.withered.verdant*': o['target'] = 'mob.deepwithered.verdant*'; print('objective', o['target'])
            if o.get('target') == 'mob.mistwalker.verdant*': o['target'] = 'mob.deepwalker.verdant*'; print('objective', o['target'])
save('EncounterContent/Quests/verdant.json', q)

p = A + 'Scripts/Encounter/EncounterSession.cs'
s = io.open(p, encoding='utf-8', newline='').read()
a = "&& Vector2.Distance(new Vector2(p.x, p.z), o.at) <= o.radius);"
b = "&& Vector2.Distance(new Vector2(p.x, p.z), o.at) <= o.radius && OnItsLevel(o.at, p));"
assert s.count(a) == 1; s = s.replace(a, b)
a = "        bool emptiedHidden;\n"
b = ("        bool emptiedHidden;\n"
     "        /// <summary>A place down a cave is visited from down in the cave, not from the hill over it: where a passage floor lies under the\n"
     "        /// place, the visitor must be standing within four metres of that floor's height. (At a cave's mouth the two are the same.)</summary>\n"
     "        static bool OnItsLevel(Vector2 place, Vector3 visitor)\n"
     "        {\n"
     "            float floor = 0; return !Crulanda.World.Hollow.FloorUnder(place, ref floor) || Mathf.Abs(visitor.y - floor) < 4;\n"
     "        }\n")
assert s.count(a) == 1; s = s.replace(a, b)
io.open(p, 'w', encoding='utf-8', newline='').write(s); print('FIXES 4 OK')
