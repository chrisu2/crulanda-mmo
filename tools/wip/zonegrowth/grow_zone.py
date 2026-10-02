"""Grows a zone by about 20% and moves the camps that stand too near its homes (Chris, 2026-10-02: "yes. grow about 20%",
answering whether the other four zones should grow like Oakhaven; playtest note 1's rule: nothing hostile within about
120 m of a home). One zone a run:

    python tools/wip/zonegrowth/grow_zone.py khaven            (reports the plan: what moves where, and every check)
    python tools/wip/zonegrowth/grow_zone.py khaven --write    (writes the zone file and the arrivals in its neighbours)

What it does, in the zone's own frame (the village stays exactly where it is):
- The edge goes out by (new - old) / 2 on every side. What stands at the edge goes out with it: the exits (and the arrival
  points the neighbouring zones give for them), the last points of the roads that run to an exit, the creek's ends (new
  points at the new edge; its swing is counted from its old first point, so its line through the zone is the one it had).
- Each camp nearer than 125 m to a home moves, whole: the camp and everything that belongs to its place (its landmark and
  that landmark's view, the props, clearings, groves, lakes, hills, tall grass, secrets, gathering nodes and critter groups
  within its reach), by one translation, to the nearest spot (preferring its own bearing from the village) where it is
  125 m or more from every home, inside the new edge (and the Wasting's curtain), off the water and the roads, and clear of
  every other place.
Nothing else moves. Homes are props of kind house, inn, mill, treehouse, shelter and keep, and any household's house, less
the enemy's own buildings named per zone.
"""
import json, math, os, re, sys
HERE = os.path.dirname(os.path.abspath(__file__))
ZD = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent', 'Zones'))
ZONES = ['oakhaven', 'khaven', 'peaks', 'ashrim', 'verdant']
RULE = 125.0       # a moved camp's centre stands this far from every home (the rule is about 120)
# What belongs to a camp's place and moves with it; the land and the roads' furniture stay (cliffs, walls, bridges, the big
# trees, signposts and lamps, herb patches, gathering nodes and stations, critter groups).
MOVES_WITH = {'grave', 'crypt', 'ruin', 'ruined_house', 'crates', 'barrels', 'cart', 'brazier', 'rock', 'fence', 'dead_oak', 'idol',
              'rib', 'spine', 'brood', 'monolith', 'mushrooms', 'perch', 'wallow', 'fallen_giant', 'shrine', 'lamp'}
# Per zone: the new size, the homes that are not homes (the enemy's own buildings), and how far a camp's place reaches.
CONF = {
    'khaven':  {'size': 410, 'not_homes': [], 'reach': 22},
    'peaks':   {'size': 430, 'not_homes': ['Toll-house', "Captain's eyrie"], 'reach': 22,
                # The Sandthrone's own toll-gate on the road and their captain's keep stay where they are: their guards stand at
                # their gate and their captain at his keep (ZoneGrowthTests.NearHomes names them).
                'stay': ['Toll-gate guards', "Captain's eyrie"]},
    'ashrim':  {'size': 430, 'not_homes': ["Hunters' hide"], 'reach': 22},   # nobody lives at the hunters' hide: all four Ash-Walkers are at the enclave
    'verdant': {'size': 430, 'not_homes': [], 'reach': 22,
                # The Briar Way guards the way to the Root-Mother's Deep and stays at its door (as Crowsfoot's deserters do).
                'stay': ['The Briar Way'],
                # Spots chosen by eye (the search found only far corners for these): the stags' meadow north of Rootfast, Old
                # Ninebranch west on his knoll, the Fallen Ghost-Oak and its spiders north-west, past the temple.
                'targets': {'Antler Meadow stags': (-40, 190), 'Old Ninebranch': (-155, 100), 'Fallen Ghost-Oak spiders': (-165, 150)}},
}


def load(zn): return json.load(open(os.path.join(ZD, zn + '.json'), encoding='utf-8-sig'))
def P(x, y): return {'x': round(x, 2) if not float(round(x, 2)).is_integer() else int(round(x, 2)), 'y': round(y, 2) if not float(round(y, 2)).is_integer() else int(round(y, 2))}
def xy(p): return (p['x'], p['y'])
def dist(a, b): return math.hypot(a[0] - b[0], a[1] - b[1])
def seg_dist(p, a, b):
    ax, ay = a; bx, by = b; px, py = p; dx, dy = bx - ax, by - ay; L = dx * dx + dy * dy
    t = 0 if L == 0 else max(0, min(1, ((px - ax) * dx + (py - ay) * dy) / L))
    return math.hypot(px - ax - t * dx, py - ay - t * dy)
def line_dist(p, pts): return min(seg_dist(p, pts[i], pts[i + 1]) for i in range(len(pts) - 1)) if len(pts) > 1 else dist(p, pts[0])


def homes_of(z, conf):
    named = {h.get('house') for h in z.get('life', {}).get('households', [])}
    return [(p.get('name') or p['kind'], xy(p['at'])) for p in z['props']
            if (p['kind'] in ('house', 'inn', 'mill', 'treehouse', 'shelter', 'keep') or (p.get('name') and p.get('name') in named))
            and (p.get('name') not in conf['not_homes'])]


def entities(z):
    """Every movable thing with a position: (collection, index, point, label). Roads, water, exits and spawns are handled apart."""
    out = []
    for coll, key in (('props', 'at'), ('landmarks', 'at'), ('secrets', 'at'), ('clearings', 'center'), ('groves', 'center'),
                      ('lakes', 'center'), ('shapes', 'center'), ('tallGrass', 'center'), ('nodes', 'at'), ('stations', 'at')):
        for i, e in enumerate(z.get(coll, [])):
            if isinstance(e, dict) and key in e: out.append((coll, i, xy(e[key]), e.get('name') or e.get('id') or e.get('kind') or e.get('node') or coll))
    for i, c in enumerate(z.get('life', {}).get('critters', [])):
        out.append(('critters', i, xy(c['center']), c.get('kind', 'critters')))
    return out


def plan(zn):
    conf = CONF[zn]; z = load(zn)
    H, H2 = z['size'] / 2, conf['size'] / 2; grow = H2 - H
    homes = homes_of(z, conf)
    near = lambda p: min((dist(p, h[1]) for h in homes), default=999)
    flagged = [i for i, c in enumerate(z['camps']) if near(xy(c['center'])) < RULE - 5 and not c.get('under') and c['name'] not in conf.get('stay', [])]
    ents = entities(z)
    # A camp's place: the things within its reach, each given to the nearest flagged camp (never a home, the core, an exit).
    homeset = {h[1] for h in homes}
    owner = {}
    for coll, i, p, lab in ents:
        if p in homeset or coll in ('nodes', 'stations', 'critters'): continue
        if coll == 'props' and z['props'][i]['kind'] not in MOVES_WITH: continue
        if coll == 'shapes' and min(dist(p, xy(z['camps'][ci]['center'])) for ci in flagged) > 12: continue
        best = None
        for ci in flagged:
            d = dist(p, xy(z['camps'][ci]['center']))
            if d <= conf['reach'] and (best is None or d < best[0]): best = (d, ci)
        if best: owner[(coll, i)] = best[1]
    stays = [xy(p['at']) for p in z['props'] if 'at' in p and p['kind'] not in MOVES_WITH]
    for (coll, i), ci in list(owner.items()):
        if coll != 'landmarks': continue
        nm = z['landmarks'][i]['name']
        if any(p.get('name', '').startswith(nm) for p in z['props'] if p['kind'] not in MOVES_WITH) or any(dist(xy(z['landmarks'][i]['at']), q) < 8 for q in stays): del owner[(coll, i)]
    # A landmark taken by a camp brings whatever lies within 12 m of it too (a camp beside its place's landmark).
    for (coll, i), ci in list(owner.items()):
        if coll != 'landmarks': continue
        lp = xy(z['landmarks'][i]['at'])
        for c2, i2, p2, _ in ents:
            if (c2, i2) in owner or dist(p2, lp) > 12 or p2 in homeset or c2 in ('nodes', 'stations', 'critters', 'shapes'): continue
            if c2 == 'props' and z['props'][i2]['kind'] not in MOVES_WITH: continue
            owner[(c2, i2)] = ci
    roads = [[xy(p) for p in r['points']] for r in z.get('roads', [])]
    water = [[xy(p) for p in w['points']] for w in z.get('water', [])]
    lakes = [(xy(l['center']), l.get('radius', 5)) for l in z.get('lakes', [])]
    exits = [xy(e['at']) for e in z['exits']]
    # The Wasting's curtain is the zone's east edge where it has one (the Ash Rim's stays where it stands: its places line it):
    # past it is the unmade, where nothing walks. A moved place keeps 15 m inside it (a camp's reach, and its landmark's).
    east = z['wasting']['x'] - 15 if 'wasting' in z else H2 - 10
    moves = {}
    others = [(p, lab) for coll, i, p, lab in ents if (coll, i) not in owner and coll not in ('groves', 'critters')]   # a camp may stand in a wood
    groups = []
    for ci in sorted(flagged, key=lambda k: -near(xy(z['camps'][k]['center']))):
        g = next((g for g in groups if any(dist(xy(z['camps'][ci]['center']), xy(z['camps'][k]['center'])) < 35 for k in g)), None)
        if g: g.append(ci)
        else: groups.append([ci])
    for g in groups:
        for k, c2 in enumerate(z['camps']):
            if k not in g and k not in flagged and not c2.get('under') and c2['name'] not in conf.get('stay', []) and any(dist(xy(c2['center']), xy(z['camps'][j]['center'])) < 35 for j in g): g.append(k)
    for k in [k for g in groups for k in g]:
        if k not in flagged: flagged.append(k)
    for g in groups:
        ci = g[0]; c = z['camps'][ci]
        C = (sum(z['camps'][k]['center']['x'] for k in g) / len(g), sum(z['camps'][k]['center']['y'] for k in g) / len(g))
        members = [(coll, i, p, lab) for coll, i, p, lab in ents if owner.get((coll, i)) in g]
        centres = [xy(z['camps'][k]['center']) for k in g]
        pts = centres + [p for _, _, p, _ in members]
        hub = min(homes, key=lambda h: dist(C, h[1]))[1]
        bearing = math.atan2(C[1] - hub[1], C[0] - hub[0])
        best = None
        def ok(d):
            moved = [(p[0] + d[0], p[1] + d[1]) for p in pts]
            return (all(near(moved[k]) >= RULE for k in range(len(g))) and not any(max(abs(q[0]), abs(q[1])) > H2 - 10 or q[0] > east for q in moved)
                and not any(line_dist(q, w) < 9 for q in moved for w in water) and not any(line_dist(q, r) < 7 for q in moved for r in roads)
                and not any(dist(q, lc) < lr + 6 for q in moved for lc, lr in lakes) and not any(dist(q, e) < 30 for q in moved for e in exits)
                and not any(dist(q, o) < 9 for q in moved for o, _ in others)
                and not any(dist(moved[0], xy(z['camps'][k]['center'])) < 40 for k in range(len(z['camps'])) if k not in flagged)
                and not any(dist(moved[0], m['to']) < 45 for m in moves.values()))
        want = conf.get('targets', {}).get(c['name'])
        if want:
            d = (want[0] - centres[0][0], want[1] - centres[0][1])
            if ok(d): best = (0, d, (C[0] + d[0], C[1] + d[1]), round(dist(C, (C[0] + d[0], C[1] + d[1]))), 0)
            else: print('  (the chosen spot for %s fails a check: searching)' % c['name'])
        for step in (range(0, 140, 2) if not best else ()):   # how far out along a bearing
            for turn in range(0, 91, 5):               # how far the bearing swings from its own
                for sgn in ((1,) if turn == 0 else (1, -1)):
                    a = bearing + sgn * math.radians(turn)
                    target = (hub[0] + math.cos(a) * (dist(C, hub) + step), hub[1] + math.sin(a) * (dist(C, hub) + step))
                    d = (target[0] - C[0], target[1] - C[1])
                    moved = [(p[0] + d[0], p[1] + d[1]) for p in pts]
                    if any(near(moved[k]) < RULE for k in range(len(g))): continue
                    if any(max(abs(q[0]), abs(q[1])) > H2 - 10 or q[0] > east for q in moved): continue
                    if any(line_dist(q, w) < 9 for q in moved for w in water): continue
                    if any(line_dist(q, r) < 7 for q in moved for r in roads): continue
                    if any(dist(q, lc) < lr + 6 for q in moved for lc, lr in lakes): continue
                    if any(dist(q, e) < 30 for q in moved for e in exits): continue
                    if any(dist(q, o) < 9 for q in moved for o, _ in others): continue
                    if any(dist(moved[0], xy(z['camps'][k]['center'])) < 40 for k in range(len(z['camps'])) if k not in flagged): continue
                    if any(dist(moved[0], m['to']) < 45 for m in moves.values()): continue
                    score = step + turn * 1.2
                    if best is None or score < best[0]: best = (score, d, target, step, turn * sgn)
            if best and best[3] <= step - 20: break
        moves[ci] = {'camps': g, 'name': ' + '.join(z['camps'][k]['name'] for k in g), 'from': C, 'to': best[2] if best else None, 'd': best[1] if best else None, 'members': members,
                     'near_before': near(C), 'near_after': near(best[2]) if best else None, 'step': best[3] if best else None, 'turn': best[4] if best else None}
        if best:   # later camps keep clear of this one's new place
            others = [(o, lab) for o, lab in others] + [((p[0] + best[1][0], p[1] + best[1][1]), lab) for _, _, p, lab in members]
    return z, conf, H, H2, grow, homes, flagged, moves


def report(zn):
    z, conf, H, H2, grow, homes, flagged, moves = plan(zn)
    print('== %s: %d -> %d m (edge out %g m); homes %s' % (zn, z['size'], conf['size'], grow, [h[0] for h in homes]))
    for ci, m in moves.items():
        if not m['to']: print('  !! camp %d %s: NO SPOT FOUND' % (ci, m['name'])); continue
        print('  camp %d %-26s %s -> (%.1f, %.1f)  nearest home %.0f -> %.0f m  (out %d m, turned %d deg)' % (ci, m['name'], m['from'], m['to'][0], m['to'][1], m['near_before'], m['near_after'], m['step'], m['turn']))
        print('      brings: ' + ', '.join('%s %s' % (coll, lab) for coll, i, p, lab in m['members']))
    return z, conf, H, H2, grow, homes, flagged, moves


def out_with_edge(p, H, grow, near=25):
    """A point by the edge goes out with it (on each axis it stands near)."""
    x, y = p
    if abs(x) > H - near: x += grow if x > 0 else -grow
    if abs(y) > H - near: y += grow if y > 0 else -grow
    return (x, y)


def write(zn):
    z, conf, H, H2, grow, homes, flagged, moves = report(zn)
    assert all(m['to'] for m in moves.values()), 'a camp has no spot'
    for ci, m in moves.items():
        dx, dy = m['d']
        for k in m['camps']: c = z['camps'][k]; c['center'] = P(c['center']['x'] + dx, c['center']['y'] + dy)
        for coll, i, p, lab in m['members']:
            e = z['life']['critters'][i] if coll == 'critters' else z[coll][i]
            key = 'center' if 'center' in e else 'at'
            e[key] = P(e[key]['x'] + dx, e[key]['y'] + dy)
            if coll == 'landmarks' and 'view' in e: e['view'] = P(e['view']['x'] + dx, e['view']['y'] + dy)
    z['size'] = conf['size']
    for e in z['exits']: e['at'] = P(*out_with_edge(xy(e['at']), H, grow))
    for r in z.get('roads', []):   # a road's end at the edge (one running to an exit) runs on to the new edge
        for end in (0, -1):
            q = xy(r['points'][end])
            if max(abs(q[0]), abs(q[1])) > H - 12: r['points'][end] = P(*out_with_edge(q, H, grow, 12))
    for w in z.get('water', []):   # the creek's ends go on to the new edge; its swing is counted from its old first point
        pts = [xy(q) for q in w['points']]; half_old, half_new = H - 1, H2 - 1
        clamp = lambda q, h: (max(-h, min(h, q[0])), max(-h, min(h, q[1])))
        if max(abs(pts[0][0]), abs(pts[0][1])) > H - 12:
            old_leg = dist(clamp(pts[0], half_old), clamp(pts[1], half_old)); new_leg = dist(clamp(pts[0], half_new), clamp(pts[1], half_new))
            w['points'].insert(0, P(*out_with_edge(pts[0], H, grow, 12)))
            w['swingFrom'] = w.get('swingFrom', 0) + 1; w['swingAlong'] = round(w.get('swingAlong', 0) - (new_leg - old_leg), 2)
        if max(abs(pts[-1][0]), abs(pts[-1][1])) > H - 12: w['points'].append(P(*out_with_edge(pts[-1], H, grow, 12)))
    def dump(name, zz):
        open(os.path.join(ZD, name + '.json'), 'w', encoding='utf-8', newline=chr(10)).write(json.dumps(zz, indent=2, ensure_ascii=False) + chr(10))
    dump(zn, z)
    for other in ZONES:   # where the neighbours put you when you come in: out with the edge too (their text edited in place,
        if other == zn: continue   # so their own layout and node rows stay as they are)
        path = os.path.join(ZD, other + '.json'); raw = open(path, 'rb').read().decode('utf-8-sig'); crlf = chr(13) + chr(10) in raw
        s = raw.replace(chr(13) + chr(10), chr(10)); o = json.loads(s); hit = False
        for e in o['exits']:
            if e.get('to') != z['id']: continue
            new = P(*out_with_edge(xy(e['arrive']), H, grow))
            i = s.index('"to": "%s"' % z['id']); j = s.index('"arrive"', i); k = s.index('}', j)   # the arrival's own text only
            seg = re.sub(r'("x":\s*)-?[\d.]+', lambda m: m.group(1) + json.dumps(new['x']), s[j:k + 1], count=1)
            seg = re.sub(r'("y":\s*)-?[\d.]+', lambda m: m.group(1) + json.dumps(new['y']), seg, count=1)
            s = s[:j] + seg + s[k + 1:]; hit = True
        if hit:
            assert [xy(e['arrive']) for e in json.loads(s)['exits'] if e.get('to') == z['id']] == [xy(P(*out_with_edge(xy(e['arrive']), H, grow))) for e in o['exits'] if e.get('to') == z['id']]
            open(path, 'wb').write((s.replace(chr(10), chr(13) + chr(10)) if crlf else s).encode('utf-8')); print('  arrivals moved in', other)
    print('written', zn, '(run place_nodes.py --write next: it checks the nodes against the moved camps and writes their rows)')


if __name__ == '__main__':
    zn = sys.argv[1]
    write(zn) if '--write' in sys.argv else report(zn)
