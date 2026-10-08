"""The Sealed Adit (Docs/DUNGEON_DESIGN.md): the dungeon zone's layout, made here and written to
New Unity Project/Assets/Crulanda/EncounterContent/Zones/adit.json (and the Peaks' way in, peaks.json).

The caves are cavern props with their own plans (rows x, z, half-width, height, floor drop below the mouth). A branch opens out of
another cave's chamber: its mouth is that cave's point `along` metres in, `aside` to the right, and it heads `turn` degrees off that
cave's heading there (the game reads `within` and sets the branch's mouth on the parent's floor). Every camp sits on a cave's floor at
(along, aside). The path (x, z and the distance along it) is traced exactly as Crulanda.World.Hollow traces it; the heights don't
come into it, so the camps are placed without the terrain. Run it again after changing a plan: the camps follow.

    python adit_layout.py            writes staged/adit.json and Docs/art/adit-plan.png (the game can't build the caves yet)
    python adit_layout.py --live     writes adit.json into the game's zones and the Peaks' way in
"""
import io, json, math, os, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
ZONES = os.path.join(ROOT, 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent', 'Zones')
DOCS = os.path.join(ROOT, 'New Unity Project', 'Docs')

# ---------------------------------------------------------------- the trace (Hollow's constructor, x and z only)
def catmull(p0, p1, p2, p3, t):
    t2, t3 = t * t, t * t * t
    return [.5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (3 * b - a - 3 * c + d) * t3) for a, b, c, d in zip(p0, p1, p2, p3)]

def trace(at, yaw, plan):
    """Rings: (x, z, half, height, drop, along); and each plan row's distance along."""
    n = len(plan); P = lambda i: plan[max(0, min(n - 1, i))][:4]; D = lambda i: plan[max(0, min(n - 1, i))][4]
    cy, sy = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    rings, rows, along, last = [], [], 0.0, None
    for seg in range(n - 1):
        p0, p1, p2, p3 = P(seg - 1), P(seg), P(seg + 1), P(seg + 2)
        steps = max(1, math.ceil(math.hypot(p2[0] - p1[0], p2[1] - p1[1]) / .7))
        k = 0
        while k < steps or (seg + 2 == n and k == steps):
            u = k / steps; q = catmull(p0, p1, p2, p3, u)
            x = at[0] + q[0] * cy + q[1] * sy; z = at[1] - q[0] * sy + q[1] * cy   # Quaternion.Euler(0, yaw, 0) * (x, 0, z)
            if last is not None: along += math.hypot(x - last[0], z - last[1])
            if k == 0: rows.append(along)
            rings.append((x, z, max(.2, q[2]), max(.3, q[3]), D(seg) + (D(seg + 1) - D(seg)) * u, along)); last = (x, z); k += 1
    rows.append(along)
    return rings, rows

def point(rings, along, aside=0.0):
    """Hollow.At: the floor point `along` in, `aside` to the right; and the heading there (unit x, z)."""
    i = 0
    while i + 1 < len(rings) and rings[i + 1][5] < along: i += 1
    j = min(i + 1, len(rings) - 1)
    f = 0 if j == i else max(0, min(1, (along - rings[i][5]) / max(1e-6, rings[j][5] - rings[i][5])))
    x = rings[i][0] + (rings[j][0] - rings[i][0]) * f; z = rings[i][1] + (rings[j][1] - rings[i][1]) * f
    fx, fz = rings[j][0] - rings[i][0], rings[j][1] - rings[i][1]
    if fx * fx + fz * fz < 1e-12 and i > 0: fx, fz = rings[i][0] - rings[i - 1][0], rings[i][1] - rings[i - 1][1]
    m = math.hypot(fx, fz) or 1; fx, fz = fx / m, fz / m
    return (x + fz * aside, z - fx * aside), (fx, fz)

def half_at(rings, along):
    i = 0
    while i + 1 < len(rings) and rings[i + 1][5] < along: i += 1
    return rings[i][2]

# ---------------------------------------------------------------- the caves (frame: mouth at the origin, the passage running +z)
MAIN = 'The Sealed Adit'
CAVES = [
    # The Old Workings, the Singing Gallery, the shaft stair and the Rail Hall.
    dict(name=MAIN, at=(0, -100), yaw=0, plan=[
        (0, -1.2, 2.0, 3.0, 0), (0, 2, 2.1, 3.1, 0), (.5, 7, 2.2, 3.2, -.5),                                   # 0-2 the adit: timbered, square-ish
        (2, 12, 2.4, 3.2, -1.2), (1, 17, 2.4, 3.3, -2),                                                         # 3-4 the drift
        (-1, 21, 5.5, 4.6, -2.4), (-2, 26, 6.5, 5, -2.6), (-1, 31, 5.5, 4.6, -2.8),                             # 5-7 the cages
        (1.5, 35, 2.5, 3.3, -3.4), (4, 39, 2.4, 3.3, -4.6), (5, 44, 2.5, 3.4, -6),                              # 8-10 the sappers' drift
        (4, 49, 6, 5, -6.6), (2.5, 55, 7.5, 5.6, -7), (2, 61, 6.5, 5.4, -7.2),                                  # 11-13 the gang-boss's hall
        (3, 65, 2.6, 3.4, -8), (4.5, 69, 2.6, 3.4, -10.5), (5, 73, 2.7, 3.5, -13), (4.5, 77, 3.2, 3.8, -15.5),    # 14-17 the lower drift (down 2.5 a row: stairs)
        (4, 82, 8, 8, -18), (3, 88, 13, 11, -20.5), (2, 96, 15, 12.5, -21), (2, 104, 14, 12, -21), (2.5, 111, 10, 9.5, -21),   # 18-22 the Gallery, 21 m down: its roof well under the land (no knoll heaps over it)
        (3, 116, 3, 3.8, -21.2), (3.5, 120, 2.8, 3.6, -21.4),                                                   # 23-24 the cage gate
        (4, 125, 2.8, 3.8, -23.2), (5, 130, 2.8, 3.8, -25.4), (7, 135, 2.8, 3.8, -27.6), (10, 139, 2.9, 3.9, -29.8),
        (14, 142, 3, 4, -31.8), (18.5, 144.5, 3, 4, -33.8), (23, 147, 3, 4, -35.8), (27, 150, 3.2, 4.2, -37.6),
        (30, 154, 3.6, 4.6, -39.2), (32, 158, 4.4, 5, -40),                                                     # 25-34 the shaft stair
        (34, 163, 9, 9, -40.5), (36, 170, 15, 13, -40.5), (38, 180, 17, 14, -40.5), (39, 191, 17, 14, -40.5),
        (39, 202, 16, 13.5, -40.5), (38, 211, 12, 11, -40.5), (37, 216, 6, 7, -40.5), (36.5, 218, .3, .4, -40.5)]),   # 35-42 the Rail Hall
    # The three ways on from the Gallery, each lit its own colour.
    dict(name='The Geode Floor', within=MAIN, along_row=20, along_off=-4, aside=11.5, turn=90, plan=[
        (0, -1, 3, 4, 0), (0, 4, 3, 4, 0), (0, 8, 2.6, 3.4, 0), (1, 13, 2.6, 3.4, -.5), (2, 18, 2.6, 3.5, -1),
        (2, 23, 7, 6, -1.2), (1, 30, 9, 6.5, -1.4), (0, 37, 8, 6, -1.5),                                       # 5-7 the cutting floor
        (-1.5, 42, 2.7, 3.5, -2), (-3, 47, 2.7, 3.5, -2.6),
        (-3, 52, 8, 7, -3), (-2, 60, 10, 7.5, -3), (-1, 67, 8, 7, -3), (0, 71, .3, .4, -3)]),                  # 10-12 Nix's workshop
    dict(name='The Ember Vent', within=MAIN, along_row=20, along_off=0, aside=-12.5, turn=-90, plan=[
        (0, -1, 3, 4, 0), (0, 4, 3, 4, 0), (0, 8, 2.6, 3.6, 0), (-1, 13, 2.6, 3.6, -1), (-2, 18, 2.8, 3.8, -2.5), (-2, 23, 3, 4, -4),
        (-1, 28, 8, 8, -4.5), (0, 36, 14, 11, -5), (0, 46, 16, 12, -5), (0, 56, 15, 12, -5), (-1, 64, 10, 9, -5),   # 6-10 the lava hall
        (-2, 70, 6, 6, -5), (-2.5, 73, .3, .4, -5)]),
    dict(name='The Grey Breach', within=MAIN, along_row=21, along_off=2, aside=-10, turn=-45, plan=[
        (0, -1, 3, 4, 0), (0, 4, 3, 4, 0), (0, 8, 2.4, 3.2, 0), (1, 13, 2.4, 3.3, -.6), (0, 18, 2.5, 3.4, -1.2),
        (-1, 23, 6, 5, -1.6), (-1, 29, 7, 5.5, -1.8),                                                           # 5-6 the greying
        (0, 34, 3, 3.6, -2.4), (2, 39, 2.6, 3.4, -3), (2, 44, 2.6, 3.4, -3.4),
        (1, 49, 7, 6, -3.6), (0, 56, 9, 7, -3.6), (0, 62, 7, 6, -3.6), (0, 65, .3, .4, -3.6)]),                # 10-12 the Foreman's drift
]

# ---------------------------------------------------------------- the camps: (cave, row, along offset, aside, entry)
S = 'GAME-ONLY band; CANON company (Sandthrone)'
def mob(name, mob_, look, count, lo, hi, tag=None, **kw):
    d = dict(name=name, mob=mob_, tag=tag or look, look=look, canonStatus=kw.pop('canon', S), radius=kw.pop('radius', 2.5), count=count, levelMin=lo, levelMax=hi, respawn=kw.pop('respawn', 900))
    d.update(kw); return d
CAMPS = [
    (MAIN, 1, 4, 0, mob('Adit pickets', 'Sandthrone picket', 'deserter', 2, 10, 10, tag='picket', radius=1.6)),
    (MAIN, 4, 0, 0, mob("Diggers' drift", 'Sandthrone digger', 'deserter', 3, 10, 10, tag='digger', radius=1.8)),
    (MAIN, 6, 0, -2, mob('The cages', 'Sandthrone overseer', 'deserter', 3, 10, 11, tag='overseer')),
    (MAIN, 9, 0, 0, mob("Sappers' drift", 'Sandthrone sapper', 'deserter', 2, 11, 11, tag='sapper', radius=1.6)),
    (MAIN, 12, -3, 3, mob("Gang-boss's guards", 'Sandthrone overseer', 'deserter', 2, 11, 11, tag='overseer', radius=1.5)),
    (MAIN, 12, 2, 0, mob('Gang-Boss Haddo Lusk', 'Gang-Boss Haddo Lusk', 'deserter', 1, 11, 11, tag='gangboss', canon='GAME-ONLY', radius=1, elite=True, guards="Gang-boss's guards")),
    (MAIN, 15, 0, 0, mob('Lower drift', 'Sandthrone digger', 'deserter', 2, 11, 11, tag='digger', radius=1.6)),
    (MAIN, 19, 0, -6, mob('Gallery carriers', 'Sandthrone geode-carrier', 'outrider', 3, 11, 11, tag='carrier', radius=3)),
    (MAIN, 21, 0, 6, mob('Gallery watch', 'Sandthrone watchman', 'outrider', 2, 11, 11, tag='watchman', radius=2)),
    (MAIN, 26, 0, 0, mob('Stair watch', 'Sandthrone watchman', 'outrider', 2, 11, 12, tag='watchman', radius=1.5)),
    (MAIN, 36, 0, -5, mob('Platform guards', 'Sandthrone platform guard', 'outrider', 3, 12, 12, tag='platformguard', radius=2.5)),
    (MAIN, 37, 0, 8, mob('Dockers', 'Sandthrone docker', 'deserter', 3, 12, 12, tag='docker', radius=2.5)),
    (MAIN, 38, -3, -3, mob("Quartermaster's guards", 'Sandthrone platform guard', 'outrider', 2, 12, 12, tag='platformguard', radius=1.5)),
    (MAIN, 38, 2, 0, mob('Quartermaster Brannigan Sorrel', 'Quartermaster Brannigan Sorrel', 'outrider', 1, 12, 12, tag='railquartermaster', canon='GAME-ONLY', radius=1, elite=True, guards="Quartermaster's guards")),
    (MAIN, 39, 2, 9, mob('Carriage gunners', 'Sandthrone carriage gunner', 'outrider', 2, 12, 12, tag='gunner', radius=2)),
    (MAIN, 40, -2, -5, mob("Rail-Captain's guard", 'Sandthrone platform guard', 'outrider', 2, 12, 12, tag='platformguard', radius=1.5)),
    (MAIN, 40, 2, 0, mob('Rail-Captain Orsk Danner', 'Rail-Captain Orsk Danner', 'outrider', 1, 12, 12, tag='railcaptain', canon='GAME-ONLY', radius=1, elite=True, harder=True, guards="Rail-Captain's guard")),

    ('The Geode Floor', 3, 0, 0, mob("Cutters' drift", 'Sandthrone geode-cutter', 'outrider', 2, 11, 11, tag='cutter', radius=1.6)),
    ('The Geode Floor', 5, 2, -3, mob('The cutting floor', 'Sandthrone geode-cutter', 'outrider', 3, 11, 11, tag='cutter', radius=2.5)),
    ('The Geode Floor', 7, -1, 4, mob('Sorting troughs', 'Sandthrone sorter', 'deserter', 3, 11, 11, tag='sorter', radius=2.5)),
    ('The Geode Floor', 10, -1, -3, mob('Workshop guards', 'Sandthrone overseer', 'deserter', 2, 11, 11, tag='overseer', radius=1.6)),
    ('The Geode Floor', 11, 3, 0, mob('Nix and the Rock-Eater', 'Nix, the turncoat', 'outrider', 1, 11, 11, tag='nix', canon='GAME-ONLY (the goblins, the Weavers of the Warrens, are CANON)', radius=1, elite=True, guards='Workshop guards')),

    ('The Ember Vent', 3, 0, 0, mob('Vent mouth', 'Ash initiate', 'cultist', 2, 11, 11, tag='ashinitiate', canon='GAME-ONLY cell; CANON cult (the Cult of Ash)', radius=1.6)),
    ('The Ember Vent', 7, 0, -9, mob('Ash initiates', 'Ash initiate', 'cultist', 3, 11, 11, tag='ashinitiate', canon='GAME-ONLY cell; CANON cult (the Cult of Ash)', radius=2.2)),
    ('The Ember Vent', 8, 0, 11, mob('Lava ledge', 'Ash mender', 'cultist', 3, 11, 11, tag='ashmender', canon='GAME-ONLY cell; CANON cult (the Cult of Ash)', radius=2.2)),
    ('The Ember Vent', 8, 0, -12, mob('The Vent-Hound', 'The Vent-Hound', 'wolf', 1, 11, 11, tag='venthound', canon='GAME-ONLY', radius=1.5, elite=True, guards='none', respawn=1200, social='solitary')),
    ('The Ember Vent', 9, 0, -9, mob('Ember circle', 'Ash initiate', 'cultist', 3, 11, 11, tag='ashinitiate', canon='GAME-ONLY cell; CANON cult (the Cult of Ash)', radius=2.2)),
    ('The Ember Vent', 10, 1, 0, mob('Cinder-Warden Ysolt', 'Cinder-Warden Ysolt', 'cultist', 1, 11, 11, tag='ysolt', canon='GAME-ONLY (the Cult of Ash is CANON)', radius=1, elite=True, guards='none')),

    ('The Grey Breach', 3, 0, 0, mob('Greyed crawlers', 'Greyed crawler', 'spider', 3, 11, 11, tag='crawler', canon='GAME-ONLY (the Wasting is CANON)', radius=1.8)),
    ('The Grey Breach', 6, 0, 0, mob('The Hollow drift', 'Hollow Man', 'hollow', 3, 11, 11, tag='hollow', canon='CANON creature (Hollow Men) and CANON tunnels under the Peaks (book2 ch.19); GAME-ONLY camp', radius=2.5)),
    ('The Grey Breach', 9, 0, 0, mob('Grey drift', 'Greyed bear', 'bear', 2, 11, 11, tag='greybear', canon='GAME-ONLY (the Wasting is CANON)', radius=1.8)),
    ('The Grey Breach', 11, 3, 0, mob('The Foreman Who Forgot', 'The Foreman Who Forgot', 'hollow', 1, 11, 11, tag='foreman', canon='GAME-ONLY (the unmade are CANON)', radius=1, elite=True, guards='none')),
]

def build():
    traced = {}
    for c in CAVES:
        if 'within' in c:
            prings, prows = traced[c['within']]
            along = prows[c['along_row']] + c['along_off']
            (x, z), (fx, fz) = point(prings, along, c['aside'])
            heading = math.degrees(math.atan2(fx, fz)) + c['turn']
            c['at'] = (round(x, 2), round(z, 2)); c['yaw'] = round(heading, 2)
            i = 0
            while i + 1 < len(prings) and prings[i + 1][5] < along: i += 1
            base = prings[i][4]   # the branch's floor starts on the parent's (drops here are below the main mouth)
        else: base = 0
        rings, rows = trace(c['at'], c['yaw'], c['plan'])
        traced[c['name']] = ([r[:4] + (r[4] + base,) + r[5:] for r in rings], rows)
    camps = []
    for cave, row, off, aside, d in CAMPS:
        rings, rows = traced[cave]; along = rows[row] + off
        (x, z), _ = point(rings, along, aside)
        assert abs(aside) + d['radius'] < half_at(rings, along) + .01, (d['name'], aside, half_at(rings, along))
        e = dict(d); e['center'] = {'x': round(x, 2), 'y': round(z, 2)}; e['cave'] = cave; e['along'] = round(along, 2); e['aside'] = aside
        camps.append(e)
    # No two caves may lie over each other (a floor under a floor): only the branches' mouths, inside the Gallery, may.
    names = [c['name'] for c in CAVES]
    for a in range(len(names)):
        for b in range(a + 1, len(names)):
            ra, rb = traced[names[a]][0], traced[names[b]][0]
            for p in ra[::3]:
                for q in rb[::3]:
                    d = math.hypot(p[0] - q[0], p[1] - q[1])
                    if d < p[2] + q[2] + 1 and abs(p[4] - q[4]) > 1.5:
                        sys.exit('caves overlap: %s at %.1f and %s at %.1f' % (names[a], p[5], names[b], q[5]))
    return traced, camps

def zone(traced, camps):
    main_at = CAVES[0]['at']
    props = []
    for c in CAVES:
        p = {'kind': 'cavern', 'name': c['name'], 'at': {'x': c['at'][0], 'y': c['at'][1]}, 'rotation': c['yaw'], 'variant': 2,
             'plan': [round(v, 3) for row in c['plan'] for v in row]}
        if 'within' in c: p['within'] = c['within']
        props.append(p)
    yard = (main_at[0], main_at[1] - 14)
    return {
        'id': 'zone.adit', 'displayName': 'The Sealed Adit', 'subtitle': 'Old workings under the Shattered Peaks',
        'canonStatus': 'CANON-EXPANDED', 'dungeon': True,
        'canonNote': 'Old mining tunnels under the Shattered Peaks with a mag-rail line deep down (CANON, book2 ch.19); aether-geodes cut '
                     'from deep seams (CANON, book1 ch.3); the Wasting eating the Peaks\' stone (CANON, world_bible); the Sandthrone, the Cult '
                     'of Ash, the goblins and the Hollow Men (CANON). GAME-ONLY: the Sandthrone cutting geodes here and running them south on '
                     'the old line, the Ash cell in the vent, the breach, every person, camp and place name. Docs/DUNGEON_DESIGN.md.',
        'size': 260, 'levelMin': 10, 'levelMax': 12, 'roadsNote': 'the cart track from the cut to the mouth', 'biome': 'mountain',
        'weather': [{'kind': 'overcast', 'weight': 3}, {'kind': 'fair', 'weight': 1}, {'kind': 'flurries', 'weight': 1}],
        'worldMapPosition': {'x': 0.5, 'y': 0.56}, 'worldMapNote': 'Under the Shattered Peaks, through the Sealed Adit.',
        'flatRadius': 14, 'hillHeight': 3, 'seed': 7717,
        # The yard levelled at the mouth, and the hill's shoulder heaped up behind it, so the adit runs into a hillside.
        'shapes': [{'name': 'The Adit cut', 'center': {'x': yard[0], 'y': yard[1] + 2}, 'radius': 12, 'height': 0, 'blend': 8},
                   {'name': 'The shoulder over the adit', 'center': {'x': main_at[0], 'y': main_at[1] + 22}, 'radius': 8, 'height': 9, 'blend': 14}],
        'lighting': {'sunPitch': 38, 'sunYaw': -20, 'sunIntensity': .95, 'sunColor': '#E9E4DA', 'ambientSky': '#6E7E99', 'ambientEquator': '#5E6670',
                     'ambientGround': '#2C2F33', 'fogColor': '#8C98A8', 'fogStart': 40, 'fogEnd': 220, 'skyTint': '#5E7096', 'skyExposure': 1, 'skyHaze': 1},
        'spawns': {'player': {'x': yard[0], 'y': yard[1]}, 'playerFacing': 0, 'companion': {'x': yard[0] + 2, 'y': yard[1] - 2},
                   'recovery': {'x': yard[0] - 2, 'y': yard[1] - 2}, 'leash': 24, 'enemies': []},
        'objectives': ['Find out what the Sandthrone are cutting under the pass', 'Stop the carriage on the dead line'],
        'exits': [{'to': 'zone.peaks', 'name': 'The cut back up to the Peaks', 'at': {'x': yard[0], 'y': yard[1] - 9}, 'arrive': {'x': -140.5, 'y': -42}, 'radius': 3.5}],
        'roads': [{'name': 'Adit track', 'width': 3, 'points': [{'x': yard[0], 'y': yard[1] - 11}, {'x': yard[0], 'y': yard[1]}, {'x': main_at[0], 'y': main_at[1] + 1}]}],
        'clearings': [{'name': 'The Adit cut', 'center': {'x': yard[0], 'y': yard[1] + 2}, 'radius': 9}],
        'landmarks': [{'name': 'The Sealed Adit', 'at': {'x': main_at[0], 'y': main_at[1] + 2}, 'radius': 6}],
        'props': props,
        'camps': camps,
    }

def peaks_exit(z):
    exits = [e for e in z['exits'] if e.get('to') != 'zone.adit']
    road = next(r for r in z['roads'] if r.get('name') == 'Ore road')
    road['points'] = [q for q in road['points'] if q['x'] >= -140.6] + [{'x': -145, 'y': -41.4}]   # on toward the adit's face, short of the seal's secret   # on to the adit's face
    exits.append({'to': 'zone.adit', 'name': 'Into the Sealed Adit', 'at': {'x': -143.3, 'y': -41.4}, 'arrive': {'x': CAVES[0]['at'][0], 'y': CAVES[0]['at'][1] - 12}, 'radius': 1.6})
    z['exits'] = exits

def picture(traced, camps, path):
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        return
    xs = [r[0] for rs, _ in traced.values() for r in rs]; zs = [r[1] for rs, _ in traced.values() for r in rs]
    x0, x1, z0, z1 = min(xs) - 20, max(xs) + 20, min(zs) - 25, max(zs) + 20; s = 4
    W, H = int((x1 - x0) * s), int((z1 - z0) * s)
    img = Image.new('RGB', (W, H), (24, 22, 20)); g = ImageDraw.Draw(img)
    colours = {MAIN: (150, 170, 120), 'The Geode Floor': (170, 120, 210), 'The Ember Vent': (230, 110, 60), 'The Grey Breach': (150, 150, 150)}
    P = lambda x, z: ((x - x0) * s, H - (z - z0) * s)
    for name, (rings, _) in traced.items():
        for r in rings:
            cx, cz = P(r[0], r[1]); rr = r[2] * s; shade = max(0, min(1, 1 + r[4] / 45))
            col = tuple(int(c * (.45 + .55 * shade)) for c in colours[name])
            g.ellipse((cx - rr, cz - rr, cx + rr, cz + rr), fill=col)
    for c in camps:
        cx, cz = P(c['center']['x'], c['center']['y']); r = 6 if c.get('elite') else 4
        g.ellipse((cx - r, cz - r, cx + r, cz + r), fill=(250, 210, 80) if c.get('elite') else (230, 60, 50))
        g.text((cx + 8, cz - 6), c['name'] + ' (%d-%d)' % (c['levelMin'], c['levelMax']), fill=(240, 235, 225))
    for name, (rings, _) in traced.items():
        mx, mz = P(rings[len(rings) // 2][0], rings[len(rings) // 2][1]); g.text((mx - 30, mz - 30), name, fill=(255, 255, 255))
    mx, mz = P(*CAVES[0]['at']); g.text((mx - 20, mz + 10), 'mouth (the Adit cut)', fill=(255, 255, 255))
    img.save(path)

if __name__ == '__main__':
    traced, camps = build()
    for c in CAVES:
        rows = traced[c['name']][1]; print(c['name'], 'length %.1f' % rows[-1], 'rows', ' '.join('%d:%.0f' % (i, a) for i, a in enumerate(rows)))
    z = zone(traced, camps); live = '--live' in sys.argv
    out = ZONES if live else os.path.join(os.path.dirname(os.path.abspath(__file__)), 'staged')
    with io.open(os.path.join(out, 'adit.json'), 'w', encoding='utf-8', newline='\n') as f: f.write(json.dumps(z, indent=2, ensure_ascii=False) + '\n')
    if live:
        pz = os.path.join(ZONES, 'peaks.json'); s = io.open(pz, encoding='utf-8', newline='').read(); peaks = json.loads(s)
        peaks_exit(peaks)
        with io.open(pz, 'w', encoding='utf-8', newline='\n') as f: f.write(json.dumps(peaks, indent=2, ensure_ascii=False) + '\n')
    picture(traced, camps, os.path.join(DOCS, 'art', 'adit-plan.png'))
    print('camps', len(camps), 'written')
