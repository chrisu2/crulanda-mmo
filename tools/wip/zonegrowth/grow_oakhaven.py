"""Oakhaven grows from 380 to 560 m and Crowsfoot Hollow moves out to the north hills (playtest note 1, 2026-10-01).

A record of how the growth was made, and a way to make it again: it reads the zone file as it stood before (commit f4106d3,
through git) and writes the grown one over Assets/Crulanda/EncounterContent/Zones/oakhaven.json, without its nodes array.
Then run, in this order:

    python tools/wip/zonegrowth/grow_oakhaven.py
    python tools/wip/professions/place_nodes.py oakhaven --write     (checks the nodes by the placement rules and writes them)
    python tools/wip/zonegrowth/verify_oakhaven.py                   (the layout against a model of the ground)

Run again on an unchanged tree, the three leave the zone file byte for byte as it is committed. The other files the growth
touched were edited by hand: the arrivals in khaven.json and ashrim.json, and Quests/oakhaven.json (the deer-track point and
the words that say where things are).

The hollow's move is one rigid turn: everything that was laid out round the old mouth at (-16, 90) keeps its place in the
cave's own frame, turned a quarter (prop rotation 90: the passage runs east, the mouth faces west) and set down at (-120, 240).
A quarter turn exactly, so every camp's spread (an upright square) covers the same floor as it did.
"""
import json, math, os, subprocess, sys
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
ZONE = 'New Unity Project/Assets/Crulanda/EncounterContent/Zones/oakhaven.json'
BEFORE = 'f4106d3'   # the commit the growth started from
GIT = r'C:\Program Files\Git\cmd\git.exe' if os.path.exists(r'C:\Program Files\Git\cmd\git.exe') else 'git'
z = json.loads(subprocess.run([GIT, '-C', REPO, 'show', BEFORE + ':' + ZONE], capture_output=True, check=True).stdout.decode('utf-8-sig'))
z.pop('nodes')   # place_nodes.py writes them (its oakhaven candidates)

def P(x, y): return {'x': x, 'y': y}
def r1(v):
    v = round(v, 2)
    return int(v) if float(v).is_integer() else v
# ---- the hollow's move: its old frame (mouth prop at (-16, 90), facing south) turned a quarter (yaw 90: the cave runs east, the
# mouth faces west) and set down at (-120, 240).
OLD = (-16, 90); NEW = (-120, 240)
def T(p): return P(r1(NEW[0] + (p['y'] - OLD[1])), r1(NEW[1] - (p['x'] - OLD[0])))
def E(p, dx=90, dy=0): return P(r1(p['x'] + dx), r1(p['y'] + dy))   # out with the edge
def find(lst, name, key='name'):
    m = [e for e in lst if e.get(key) == name]
    assert len(m) == 1, (name, len(m)); return m[0]
def at_is(e, x, y, key='at'): assert (e[key]['x'], e[key]['y']) == (x, y), (e.get('name'), e[key])

z['size'] = 560
z['canonNote'] = z['canonNote'].replace(
    "Crowsfoot Hollow (the cave at the end of the North road, running deep under the northern hills)",
    "Crowsfoot Hollow (the cave past the end of the North road, in the west heel of Crowsfoot Ridge, running deep under the northern hills)").replace(
    "The outer farmland (2026-09-30 growth):",
    "The second growth (2026-10-01, 380 to 560 m, playtest note 1): the hollow and Crowsfoot Ridge moved out to the north hills, and Carder's field barn, Greyback Shaw, the Carter's Rest, Lark Hill, Sallow Bottom and the Bound wood are GAME-ONLY places. The outer farmland (2026-09-30 growth):")
assert 'second growth' in z['canonNote'] and 'west heel' in z['canonNote']

# ---- exits, roads, creek, the Wasting: out with the edge
ex = z['exits']; at_is(ex[0], -181, 13.5); ex[0]['at'] = P(-271, 13.5); at_is(ex[1], -35, -183); ex[1]['at'] = P(-35, -273)
z['wasting']['x'] = 254
roads = z['roads']
def pts(*a): return [P(a[i], a[i + 1]) for i in range(0, len(a), 2)]
r = find(roads, 'North road'); assert len(r['points']) == 4
r['points'] += pts(-12, 96, -8, 120, -11, 146, -19, 168, -32, 184)
r = find(roads, 'West road'); assert r['points'][-1] == P(-188, 13.7)
r['points'] = r['points'][:-1] + pts(-188, 13.7, -205, 12, -222, 15.5, -240, 14.5, -257, 13, -271, 13.5, -278, 13.7)
r = find(roads, 'Road into the grey'); assert r['points'][-3:] == pts(125, 117, 145, 124, 164, 128)
r['points'] = r['points'][:-2] + pts(150, 121, 180, 119, 208, 122, 235, 124, 254, 128)
r = find(roads, 'South road'); assert r['points'][-2:] == pts(-35, -183, -36, -188)
r['points'] = r['points'][:-1] + pts(-39, -200, -34, -220, -31, -240, -34, -258, -35, -273, -36, -278)
r = find(roads, 'Mill road'); assert r['points'][-3:] == pts(120, -128, 140, -139, 160, -147)
r['points'] = r['points'][:-2] + pts(145, -134, 175, -137, 205, -135, 230, -139, 250, -147)
r = find(roads, 'Hollin lane'); r['points'] = pts(205, -135, 208, -110, 210, -88, 212, -70)
r = find(roads, 'Ridge path'); r['name'] = 'Overlook path'
r = find(roads, 'Crowsfoot track'); r['points'] = pts(-32, 184, -50, 188, -70, 187, -92, 191, -112, 199, -128, 211, -128, 227, -123.5, 232, -121.5, 240)
w = z['water'][0]; assert w['points'][0] == P(-190, -66) and w['points'][-1] == P(190, -19)
w['points'] = pts(-280, -72, -248, -66, -218, -61) + w['points'] + pts(220, -21, 250, -18, 280, -17)

# ---- Crowsfoot Hollow, whole: the cave, its scarps and boulders, its camps, its key and strongbox, the ridge over it
props = z['props']
c = find(props, 'Crowsfoot Hollow'); assert c['kind'] == 'cavern'; at_is(c, *OLD); c['at'] = P(*NEW); c['rotation'] = 90
for n in ('Crowsfoot boulder, east', 'Crowsfoot boulder, west', 'Crowsfoot boulder, track', 'Crowsfoot scarp, west', 'Crowsfoot scarp, east'):
    p = find(props, n); p['at'] = T(p['at']); p['rotation'] = (p.get('rotation', 0) + 90) % 360
find(props, 'Crowsfoot scarp, west')['name'] = 'Crowsfoot scarp, north'; find(props, 'Crowsfoot scarp, east')['name'] = 'Crowsfoot scarp, south'
find(props, 'Crowsfoot boulder, east')['name'] = 'Crowsfoot boulder, south'; find(props, 'Crowsfoot boulder, west')['name'] = 'Crowsfoot boulder, north'
for x, y in ((-42, 148), (-30, 161)):   # the two loose rocks that lay on the old ridge go with it
    m = [q for q in props if q['kind'] == 'rock' and (q['at']['x'], q['at']['y']) == (x, y)]; assert len(m) == 1; m[0]['at'] = T(m[0]['at'])
camps = z['camps']
HOLLOW_CAMPS = ['Hollow lookouts', "Deserters' camp", "King's guard", "Caddock's hall", 'Drop sentries', 'Store Caves', "The Quartermaster's desk", 'Deep Stair watch']
for n in HOLLOW_CAMPS: cp = find(camps, n); cp['center'] = T(cp['center'])
for sid in ('secret.oakhaven.strongbox-key', 'secret.oakhaven.quartermasters-strongbox'):
    s = find(z['secrets'], sid, 'id'); s['at'] = T(s['at']); s['rotation'] = (s.get('rotation', 0) + 90) % 360
shapes = z['shapes']
for n in ('Crowsfoot Hollow brow', 'Crowsfoot Ridge, over the Drop', 'Crowsfoot Ridge, the crown', 'Crowsfoot Ridge, east toe', 'Crowsfoot Hollow brow, west'):
    s = find(shapes, n); s['center'] = T(s['center'])
find(shapes, 'Crowsfoot Ridge, east toe')['name'] = 'Crowsfoot Ridge, south toe'
find(shapes, 'Crowsfoot Hollow brow, west')['name'] = 'Crowsfoot Hollow brow, north'
s = find(shapes, 'Crowsfoot Ridge, west toe'); s['name'] = 'Overlook hill'   # the knob the Overlook stands on stays where it is,
s['radius'] = 4; s['blend'] = 22                                              # a hill of its own now the ridge has gone from behind it: a broader foot
g = find(z['groves'], 'Crowsfoot thicket'); g['center'] = T(g['center']); g['size'] = P(g['size']['y'], g['size']['x'])
lm = z['landmarks']
l = find(lm, 'Crowsfoot Hollow'); l['at'] = T(l['at']); l['view'] = T(l['view'])
l['text'] = "A cave in the north hills, in the west heel of the ridge where nobody looks. Lately, smoke by night."
l = find(lm, 'Crowsfoot Ridge'); l['at'] = P(-45, 224); l['view'] = P(-34, 172); l['viewZoom'] = 18
find(z['groves'], 'Ridge pines')['name'] = 'Upper pines'

# ---- the grey's edge: what stood by the Wasting stays by it
l = find(lm, 'The Wasting'); l['at'] = E(l['at']); l['view'] = E(l['view'])
s = find(z['secrets'], 'secret.oakhaven.roads-end', 'id'); s['at'] = E(s['at'])
for n in ("Hallow's Creek milestone", 'Grey husk', 'Hollin farmhouse', 'Hollin barn', 'Hollin cart'):
    p = find(props, n); p['at'] = E(p['at'])
def prop_at(kind, x, y):
    m = [p for p in props if p['kind'] == kind and (p['at']['x'], p['at']['y']) == (x, y)]; assert len(m) == 1, (kind, x, y, len(m)); return m[0]
for kind, x, y in (('signpost', 146, -136.5), ('rock', 156.5, -136), ('barrels', 113.5, -66), ('haystack', 139, -44), ('fence', 108, -66), ('fence', 134.9, -80.2)):
    p = prop_at(kind, x, y); p['at'] = E(p['at'])
for n in ("Hallow's Creek milestone", 'Hollin farm'): l = find(lm, n); l['at'] = E(l['at'])
for n in ('Hollin orchard', 'Greying oaks', 'Grey wood'): g = find(z['groves'], n); g['center'] = E(g['center'])
for n in ('Hollin home field', 'Hollin east field'): f = find(z['fields'], n); f['center'] = E(f['center'])
cl = find(z['clearings'], 'Hollin yard'); cl['center'] = E(cl['center'])
s = find(shapes, 'Hollin farmyard'); s['center'] = E(s['center'])
s = find(z['secrets'], 'secret.oakhaven.hallows-creek-letter', 'id'); s['at'] = E(s['at'])
t = find(z['tallGrass'], 'Hollin fallow'); t['center'] = E(t['center'])
crit = z['life']['critters']
assert crit[22]['kind'] == 'crow' and crit[22]['center'] == P(128, -62); crit[22]['center'] = P(218, -62)

# ---- the west bound: the Bound Stone stands where Oakhaven ends
for kind, x, y in (('grave', -161, 19.5), ('rock', -163.5, 21.5), ('signpost', -153, 19)):
    p = prop_at(kind, x, y); p['at'] = E(p['at'], -90)
l = find(lm, 'The Bound Stone'); l['at'] = E(l['at'], -90)
find(z['groves'], 'Bound copse')['name'] = 'Downs copse'

# ---- wolf and boar country: every camp 125 m and more from every house (the farms and fields lie inside that)
def camp(name, x, y, newname=None):
    cp = find(camps, name); cp['center'] = P(x, y)
    if newname: cp['name'] = newname
camp('Harrow wood wolves', 44, 168, 'Upper pines wolves')
camp('South copse boars', -100, -186, 'Southwood boars')
camp('North pines wolves', 92, 194, 'Greyback Shaw wolves')
camp('Tall-grass stalkers', -6, -214)
camp('Brookside boars', 166, -2)
camp('Mastwood boars', -132, 168)
camp("Whitefoot's den", -222, 120); camp("Whitefoot's pack", -210, 104)
camp('Withy pool boars', -174, -128)
camp('Hollin farm wolves', 205, -40)
def wild(name, mob, tag, x, y, radius, count, lo, hi): return {'name': name, 'mob': mob, 'tag': tag, 'look': tag, 'canonStatus': 'GAME-ONLY', 'center': P(x, y), 'radius': radius, 'count': count, 'levelMin': lo, 'levelMax': hi, 'respawn': 75}
camps += [wild('Hazel bank wolves', 'Grey wolf', 'wolf', -72, 172, 7, 3, 2, 2),
          wild('Sallow Bottom boars', 'Wild boar', 'boar', 160, -214, 8, 4, 2, 2),
          wild('Bound wood wolves', 'Grey wolf', 'wolf', -234, -36, 8, 4, 1, 2)]
l = find(lm, 'The tall grass'); l['at'] = P(-6, -214)
# Old Whitefoot's rocks and thicket go with him; the Withy wallow goes with its boars.
p = find(props, "Whitefoot's rocks"); at_is(p, -164, 108); p['at'] = P(-222, 120)
s = find(shapes, "Whitefoot's rocks"); s['center'] = P(-222, 120)
g = find(z['groves'], "Whitefoot's thicket"); g['center'] = P(-226, 126)
p = find(props, 'Withy wallow'); at_is(p, -142, -118); p['at'] = P(-174, -128)
s = find(shapes, 'Withy wallow'); s['center'] = P(-174, -128)
g = find(z['groves'], 'Southwood'); g['center'] = P(-92, -170); g['size'] = P(48, 44); g['count'] = 44

# ---- the new ground: fields nearest, then woods and rough grass, then the hills
z['fields'] += [
    # (All past the village's reach, WorldLife.VillageReach: no villager's day changes for them.)
    {'name': 'Top field', 'center': P(-36, 164), 'size': P(20, 14), 'rotation': 4, 'crop': 'stubble'},
    {'name': 'Carder north acre', 'center': P(8, 146), 'size': P(24, 16), 'rotation': 5, 'crop': 'soil'},
    {'name': 'West strips', 'center': P(-192, 36), 'size': P(22, 14), 'rotation': -4, 'crop': 'stubble'},
]
z['clearings'] += [{'name': 'Field barn yard', 'center': P(0, 128), 'radius': 5}, {'name': "Carter's rest", 'center': P(-216, 20), 'radius': 5}]
z['groves'] += [
    {'name': 'Greyback Shaw', 'kind': 'broadleaf', 'center': P(92, 196), 'size': P(56, 36), 'count': 40},
    {'name': 'Ridge pines', 'kind': 'pine', 'center': P(-60, 201), 'size': P(60, 22), 'count': 30},
    {'name': 'Hill pines', 'kind': 'pine', 'center': P(20, 240), 'size': P(52, 30), 'count': 30},
    {'name': 'Pale wood', 'kind': 'dead', 'center': P(232, 208), 'size': P(22, 40), 'count': 16},
    {'name': 'Alder holt', 'kind': 'broadleaf', 'center': P(172, 8), 'size': P(34, 26), 'count': 18},
    {'name': 'Sallows', 'kind': 'broadleaf', 'center': P(160, -216), 'size': P(44, 30), 'count': 24},
    {'name': 'Ashward pines', 'kind': 'pine', 'center': P(66, -240), 'size': P(52, 28), 'count': 26},
    {'name': 'Bound wood', 'kind': 'broadleaf', 'center': P(-236, -32), 'size': P(40, 34), 'count': 30},
    {'name': 'Lark hill thorns', 'kind': 'broadleaf', 'center': P(-206, -222), 'size': P(20, 18), 'count': 8},
    {'name': 'Hanger', 'kind': 'broadleaf', 'center': P(-222, 196), 'size': P(44, 36), 'count': 28},
]
z['tallGrass'] += [{'name': 'Wold grass', 'center': P(-238, 66), 'radius': 9}, {'name': 'Shaw grass', 'center': P(56, 226), 'radius': 8}, {'name': 'Sour meadow', 'center': P(112, -196), 'radius': 8}]
shapes += [
    {'name': 'Lark hill', 'center': P(-232, -206), 'radius': 4, 'height': 7, 'blend': 24},
    {'name': 'Lark hill crown', 'center': P(-232, -206), 'radius': 3, 'height': 7.8, 'blend': 7},
    {'name': 'Sallow bottom', 'center': P(160, -214), 'radius': 12, 'height': -0.7, 'blend': 10},
    {'name': 'Sallow wallow', 'paint': 'mud', 'center': P(150, -208), 'radius': 4.5, 'height': -0.5, 'blend': 5},
    {'name': 'Crowsfoot Ridge, east shoulder', 'center': P(-8, 236), 'radius': 6, 'height': 8.5, 'blend': 26},
    # Foothills either end of the ridge, so the north reads as hill country and the mouth lies in a fold between two hills.
    {'name': 'Crowsfoot Ridge, west knee', 'center': P(-162, 258), 'radius': 8, 'height': 7, 'blend': 28},
    {'name': 'North downs', 'center': P(62, 262), 'radius': 8, 'height': 6, 'blend': 30},
]
new_props = [
    # Carder's field barn on the North road, the last roof before the wild: the deserters had it three nights running.
    {'kind': 'barn', 'name': "Carder's field barn", 'at': P(8, 128), 'rotation': 90, 'size': P(9, 6)},
    {'kind': 'cart', 'name': 'Tipped cart', 'at': P(-1, 133.5), 'rotation': 290},
    {'kind': 'crates', 'at': P(2.5, 123), 'rotation': 25, 'variant': 0},
    {'kind': 'haystack', 'at': P(14, 124.5)},
    {'kind': 'fence', 'at': P(8, 136.6), 'rotation': 5, 'size': P(22, 0)},
    {'kind': 'fence', 'at': P(-36, 156), 'rotation': 4, 'size': P(18, 0)},
    {'kind': 'signpost', 'at': P(-28.5, 187.5), 'rotation': 300},
    # The Carter's Rest on the West road: where the Khaven carters water their horses.
    {'kind': 'tree', 'name': 'Halfway oak', 'at': P(-218, 28), 'variant': 1, 'scale': 1.5},
    {'kind': 'cart', 'name': "Carter's wain", 'at': P(-213, 22), 'rotation': 80},
    {'kind': 'barrels', 'at': P(-218.5, 19.5), 'variant': 1},
    {'kind': 'wayshrine', 'name': "Carter's shrine", 'at': P(-224, 22), 'rotation': 0},
    {'kind': 'rock', 'at': P(-227.5, 25), 'variant': 1},
    # Lark Hill in the south-west: thorn and stone on a bare down.
    {'kind': 'perch', 'name': 'Lark hill stones', 'at': P(-232, -206), 'rotation': 0, 'size': P(3, 0)},
    {'kind': 'tree', 'name': 'Lark hill thorn', 'at': P(-225, -212), 'variant': 3, 'scale': 1.05},
    # Sallow Bottom in the south-east, where the ground never dries.
    {'kind': 'wallow', 'name': 'Sallow wallow', 'at': P(150, -208), 'size': P(4.5, 0)},
    {'kind': 'dead_oak', 'name': 'Sallow snag', 'at': P(171, -206), 'scale': 0.55},
    {'kind': 'rock', 'at': P(146, -221), 'variant': 1},
    # Stones along the hills and on the way up to them.
    {'kind': 'rock', 'at': P(-58, 180), 'variant': 2},
    {'kind': 'rock', 'at': P(-101, 189), 'variant': 1},
    {'kind': 'rock', 'at': P(-144, 226), 'variant': 2},
    {'kind': 'rock', 'at': P(8, 214), 'variant': 2},
    {'kind': 'rock', 'at': P(44, 256), 'variant': 1},
    {'kind': 'rock', 'at': P(-178, 236), 'variant': 2},
    {'kind': 'dead_oak', 'name': 'Crow oak', 'at': P(-152, 200), 'scale': 0.8},
    {'kind': 'tree', 'at': P(-4, 106), 'variant': 2},
    {'kind': 'tree', 'at': P(26, 126), 'variant': 0},
    {'kind': 'tree', 'at': P(-206, 47), 'variant': 1},
    {'kind': 'rock', 'at': P(196, 88), 'variant': 1},
    {'kind': 'rock', 'at': P(-250, -120), 'variant': 2},
    {'kind': 'rock', 'at': P(20, -250), 'variant': 1},
]
# The trades' six buildings stay the last six of the list (WorkshopDataTests, VillageStreamTests: the zone is built with and
# without them and nothing else may move), so the new ground's props go in before them.
SIX = ['Carder farmhouse', 'Crisp cottage', "Tanner's leather shop", "Lisbet's drying hut", "The Cask's kitchen", "Moss's game rack"]
assert [q.get('name') for q in props[-6:]] == SIX
props[-6:-6] = new_props
assert [q.get('name') for q in props[-6:]] == SIX and len(props) == 188 + len(new_props)
lm += [
    {'name': "Carder's field barn", 'text': "Wil Carder's barn on the north fields, the last roof on the North road. The door hangs off one hinge and the seed-corn is gone.", 'at': P(8, 128), 'radius': 9, 'canonStatus': 'GAME-ONLY'},
    {'name': 'Greyback Shaw', 'text': "Oak and thorn too thick to plough. The grey-backed wolves lie up in it by day, and the north farms count their lambs twice.", 'at': P(92, 196), 'radius': 16, 'canonStatus': 'GAME-ONLY'},
    {'name': "The Carter's Rest", 'text': "Half way to the bound: an oak, a shrine and room to turn a wain. Khaven's carters water their horses here and do not stay the night.", 'at': P(-218, 21), 'radius': 8, 'canonStatus': 'GAME-ONLY'},
    {'name': 'Lark Hill', 'text': "A bare down with a ring of stones on its crown. Larks go up from it all summer, and from the top you can see the road to both ends.", 'at': P(-232, -206), 'radius': 10, 'canonStatus': 'GAME-ONLY'},
    {'name': 'Sallow Bottom', 'text': "The ground never dries here. The Brook farm's pigs went wild in the sallows two winters back, and nobody has gone to fetch them.", 'at': P(160, -214), 'radius': 12, 'canonStatus': 'GAME-ONLY'},
    {'name': 'The Bound wood', 'text': "The last wood before the bound. Its wolves cross the West road at dusk, and the carters whip up when they pass it.", 'at': P(-236, -32), 'radius': 14, 'canonStatus': 'GAME-ONLY'},
]
z['secrets'] += [
    {'id': 'secret.oakhaven.long-view', 'name': 'The Long View', 'kind': 'vista', 'at': P(-232, -206), 'radius': 5, 'rotation': 40,
     'text': "From the stones on the crown the whole south-west lies open: the West road a pale thread to the bound, Withy pool like a dropped coin, the Great Oak a green smudge over the roofs. Westward the sky is the colour of a bruise, and stays that colour.",
     'xp': 35, 'canonStatus': 'GAME-ONLY place. Khaven lying west of Oakhaven under a dusk that does not lift is CANON-EXPANDED (the Khaven village map; the permanent dusk is the game\'s reading).'},
    {'id': 'secret.oakhaven.carters-stash', 'name': "The Carter's Stash", 'kind': 'cache', 'prompt': 'Reach in behind the shrine', 'at': P(-224, 23.3), 'rotation': 180,
     'text': "A tin pushed in behind the wayside shrine, where a carter could reach it from the box without getting down: a few coins against a broken axle, a heel of cheese gone hard, and a lucky acorn on a thong.",
     'item': 'food.harrow_cheese', 'xp': 25, 'gold': 6, 'canonStatus': 'GAME-ONLY'},
    {'id': 'secret.oakhaven.carder-sack', 'name': 'A Sack with the Carder Mark', 'kind': 'cache', 'prompt': 'Search the sack under the stone', 'at': P(-46, 181), 'rotation': 20,
     'text': "Dropped where the track starts to climb and kicked under a stone to be fetched later: a seed-corn sack with the Carder mark burnt into it, a purse somebody skimmed from the king's share, and a stoppered draught. Whoever hid it has not been back.",
     'item': 'potion.minor', 'xp': 30, 'gold': 7, 'canonStatus': 'GAME-ONLY'},
]
crit += [
    {'kind': 'rabbit', 'center': P(-2, 112), 'radius': 16, 'count': 5},
    {'kind': 'deer', 'center': P(4, 206), 'radius': 14, 'count': 3},
    {'kind': 'rabbit', 'center': P(-222, 44), 'radius': 16, 'count': 4},
    {'kind': 'deer', 'center': P(-206, 176), 'radius': 12, 'count': 3},
    {'kind': 'crow', 'center': P(6, 130), 'radius': 10, 'count': 4},
    {'kind': 'rabbit', 'center': P(196, 62), 'radius': 16, 'count': 4},
    {'kind': 'deer', 'center': P(96, -232), 'radius': 12, 'count': 2},
]
# Grass and plants thin out past the farms (ZoneDefinition.wildFrom): full to 160 m from the middle, 45% at the edge.
z['wildFrom'] = 160; z['wildDensity'] = 0.45

# ---- write (two-space JSON as the file always was; place_nodes.py appends the nodes array in its one-line rows)
wild = (z.pop('wildFrom'), z.pop('wildDensity'))
out = {}
for k in list(z.keys()):
    out[k] = z[k]
    if k == 'seed': out['wildFrom'], out['wildDensity'] = wild
t = json.dumps(out, indent=2, ensure_ascii=False)
path = os.path.join(REPO, *ZONE.split('/'))
open(path, 'wb').write(t.replace('\n', '\r\n').encode('utf-8'))
print('written', path, '(without nodes: run place_nodes.py oakhaven --write next)')
