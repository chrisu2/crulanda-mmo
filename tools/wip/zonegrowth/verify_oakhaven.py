"""Oakhaven's layout checked against a model of the ground (playtest note 1: the zone grown to 560 m, Crowsfoot Hollow moved).

    python tools/wip/zonegrowth/verify_oakhaven.py

It uses tools/wip/professions/place_nodes.py's model (ZoneBuilder.HeightAt ported, the solid props, a walk over a 1 m grid from
the player's start) and prints, for the zone file as it stands:
- the cave: its rings, the rock over its roof past the Drop (CaveTests wants more than 3 m), how the land over it compares with
  the land it lay under before the move (commit f4106d3, read through git), and how far it stays inside the edge;
- every camp: its nearest house (125 m and more is the rule's margin over 120), whether its spread is walkable, whether a cave
  camp's spread stays within the passage;
- every road's steepest stretch, the exits and arrivals, every secret, landmark, prop, field and grove, the highest ground on
  the boundary line (the boundary's wall is 11 m), the game animals' circles.
The model is not the engine: it knows no grove tree, no forest edge and no carved creek, and it takes a perch's whole ring of
boulders as solid, so a den or lookout on a perch (Whitefoot's den, the Overlook, the Long View) always prints UNREACHABLE,
as do roads that start among the village's houses and lamps (BLOCKED). Those lines are the same before and after the growth.
The PlayMode tests are the judge.
"""
import sys, os, json, math, subprocess, tempfile
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
sys.path.insert(0, os.path.join(HERE, '..', 'professions'))
import place_nodes as pn
V = pn.V; dist = pn.dist
BEFORE = 'f4106d3'
GIT = r'C:\Program Files\Git\cmd\git.exe' if os.path.exists(r'C:\Program Files\Git\cmd\git.exe') else 'git'
def load(orig=False, zid='oakhaven'):
    allz = pn.load_all()
    if not orig: return pn.Zone(zid, allz)
    raw = subprocess.run([GIT, '-C', REPO, 'show', BEFORE + ':New Unity Project/Assets/Crulanda/EncounterContent/Zones/' + zid + '.json'], capture_output=True, check=True).stdout
    with tempfile.TemporaryDirectory() as d:
        open(os.path.join(d, zid + '.json'), 'wb').write(raw)
        keep = pn.ZONES; pn.ZONES = d
        try: return pn.Zone(zid, allz)
        finally: pn.ZONES = keep
def roof(r): return r[4] + r[3] * 1.19 + .15
def cave_report(z, label):
    rings = z.caves[0]; along = 0; worst = 1e9; ws = None; near = []
    for i, r in enumerate(rings):
        if i > 0: along += math.hypot(r[0] - rings[i - 1][0], r[1] - rings[i - 1][1])
        cover = r[5] - roof(r)
        if along > 45 and cover < worst: worst = cover; ws = (round(r[0], 1), round(r[1], 1), round(along, 1))
        if cover < 3: near.append(round(along, 1))
    xs = [r[0] for r in rings]; zs = [r[1] for r in rings]
    print(label, 'rings', len(rings), 'length %.1f' % along, 'mouth ground %.2f' % rings[0][4], 'floor end %.2f' % rings[-1][4])
    print('   least rock over the roof past 45 m in: %.2f m at %s (must be > 3)' % (worst, ws))
    if near: print('   near-surface stretch (cover<3): %s .. %s m in' % (near[0], near[-1]))
    print('   with walls: x %.1f..%.1f z %.1f..%.1f' % (min(r[0] - r[2] * 1.2 for r in rings), max(r[0] + r[2] * 1.2 for r in rings), min(r[1] - r[2] * 1.2 for r in rings), max(r[1] + r[2] * 1.2 for r in rings)))
    return rings
bad = 0
def problem(*a):
    global bad
    bad += 1; print('  !!', *a)
def main():
    global bad
    zo = load(True); zn = load(False)
    print('size', zo.z['size'], '->', zn.z['size'])
    ro = cave_report(zo, 'OLD'); rn = cave_report(zn, 'NEW')
    d = [rn[i][5] - rn[i][4] - (ro[i][5] - ro[i][4]) for i in range(len(rn))]
    print('   land height over the floor, new minus old: min %.2f max %.2f' % (min(d), max(d)))
    z = zn; walk = pn.Walk(z)
    green = (-8, -8); mouth = V(next(p for p in z.props if p['kind'] == 'cavern')['at'])
    print('mouth', mouth, 'from the green %.1f m, from the Great Oak %.1f m' % (dist(mouth, green), dist(mouth, (0, 0))))
    houses = [(p.get('name'), V(p['at'])) for p in z.props if p['kind'] in ('house', 'inn', 'mill')]
    print('== camps (nearest house)')
    for c in z.z['camps']:
        at = V(c['center']); n = min(houses, key=lambda h: dist(h[1], at)); d0 = dist(n[1], at)
        cave = z.cave_at(at)
        r = c['radius']; pts = [(at[0] + dx * r, at[1] + dy * r) for dx in (-1, 0, 1) for dy in (-1, 0, 1)]
        notes = []
        if d0 < 125: notes.append('TOO NEAR A HOUSE')
        if cave:
            for q in pts:
                if not z.cave_at(q): notes.append('spread leaves the passage at %s' % (q,)); break
        else:
            if not walk.reachable(at, 2.5): notes.append('UNREACHABLE')
            nreach = sum(1 for q in pts if walk.reachable(q, 3))
            if nreach < 7: notes.append('only %d/9 of its spread reachable' % nreach)
            if z.near_water(at, 0): notes.append('in water')
            if z.road_near(at, r * .5): notes.append('on a road: %s' % z.road_near(at, r * .5))
            if z.cave_cover(at, 12 + r): notes.append('within 12 m of the passage near the surface')
            st = walk.steep(at, r)
            if st > .6: notes.append('steep %.2f' % st)
            if max(abs(at[0]), abs(at[1])) > z.half - 20: notes.append('in the forest edge')
            if z.wasting and at[0] + r > z.wasting['x'] - z.wasting.get('fade', 20): notes.append('in the grey')
        if notes: bad += 1
        print('  %-26s (%7.1f,%7.1f) L%d-%d %5.1f m from %-18s %s%s' % (c['name'], at[0], at[1], c['levelMin'], c['levelMax'], d0, n[0], 'in the cave ' if cave else '', '; '.join(notes)))
    cs = [(c['name'], V(c['center']), c['radius']) for c in z.z['camps'] if not z.cave_at(V(c['center']))]
    for i in range(len(cs)):
        for j in range(i + 1, len(cs)):
            if dist(cs[i][1], cs[j][1]) < cs[i][2] + cs[j][2] + 14 and not ('Whitefoot' in cs[i][0] and 'Whitefoot' in cs[j][0]): problem('camps close:', cs[i][0], cs[j][0], round(dist(cs[i][1], cs[j][1]), 1))
    print('== roads')
    for pts, w, name in z.roads:
        dd = pn.densify(pts, 1.0); hs = [float(z.height(p[0], p[1])) for p in dd]; sl = max(abs(hs[i + 1] - hs[i]) / max(.2, dist(dd[i], dd[i + 1])) for i in range(len(dd) - 1))
        unre = [p for p in dd if not walk.reachable(p, 2.5) and max(abs(p[0]), abs(p[1])) < z.half - 3 and not (z.wasting and p[0] > z.wasting['x'] - 2)]
        blocked = [(p, z.blocked(p, w / 2)) for p in dd if z.blocked(p, w / 2)]
        note = ''
        if sl > .45: note += ' STEEP'
        if unre: note += ' UNREACHABLE at %s' % (unre[0],)
        if blocked: note += ' BLOCKED %s' % (blocked[0],)
        if note: bad += 1
        print('  %-20s %3d m  max slope %.2f  height %.1f..%.1f%s' % (name, len(dd), sl, min(hs), max(hs), note))
    print('== exits, arrivals, spawn')
    for e, r in z.exits:
        if not walk.reachable(e, 3): problem('exit unreachable', e)
        if not z.road_near(e, 1): problem('exit off its road', e)
    for a in z.arrivals:
        print('  arrival', a, 'reachable', walk.reachable(a, 2.5), 'road', z.road_near(a, 1))
        if not walk.reachable(a, 2.5): problem('arrival unreachable', a)
        if max(abs(a[0]), abs(a[1])) > z.half - 4: problem('arrival outside', a)
    print('== secrets')
    for s in z.z['secrets']:
        at = V(s['at']); cave = z.cave_at(at); notes = []
        if not cave:
            if not walk.reachable(at, 2.4 if s['kind'] != 'vista' else s.get('radius', 5)): notes.append('UNREACHABLE')
            r = z.road_near(at, 4 if s['kind'] != 'vista' else .5)
            if r: notes.append('by road ' + r)
            if z.in_building(at): notes.append('in a building')
            if z.near_water(at, 0): notes.append('in water')
            if z.cave_cover(at, 12): notes.append('within 12 m of the passage')
            for c, r_, n in z.camps:
                if dist(at, c) < r_: notes.append('in camp ' + n)
            for p in z.props:
                if p.get('interact') and dist(V(p['at']), at) < 5: notes.append('on quest prop')
            if max(abs(at[0]), abs(at[1])) > z.half - 10: notes.append('at the edge')
        elif cave[1] < .3: notes.append('at the cave wall %.2f' % cave[1])
        if notes: bad += 1
        print('  %-44s (%7.1f,%7.1f) %s %s' % (s['id'], at[0], at[1], 'cave' if cave else '', '; '.join(notes)))
    print('== landmarks')
    for l in z.z['landmarks']:
        at = V(l['at']); notes = []
        if max(abs(at[0]), abs(at[1])) > z.half - 8: notes.append('OUTSIDE')
        if not walk.reachable(at, max(3, l.get('radius', 10))): notes.append('unreachable within its radius')
        if 'view' in l:
            v = V(l['view'])
            if max(abs(v[0]), abs(v[1])) > z.half - 12: notes.append('view outside')
            if not walk.reachable(v, 3): notes.append('view point not walkable')
        if notes: bad += 1; print('  !!', l['name'], at, notes)
    print('== props')
    for i, p in enumerate(z.props):
        at = V(p['at']); k = p['kind']
        if k in ('bridge', 'cavern'): continue
        if max(abs(at[0]), abs(at[1])) > z.half - 6: problem('prop at the edge', k, p.get('name'), at)
        if z.wasting and at[0] > z.wasting['x'] - 2: problem('prop in the grey', k, p.get('name'), at)
        if k not in ('fence', 'hedge', 'signpost', 'lamp', 'cliff', 'herb', 'stall') and z.road_near(at, .3): problem('prop on a road', k, p.get('name'), at, z.road_near(at, .3))
        if k not in ('mill',) and z.near_water(at, -2): problem('prop in water', k, p.get('name'), at)
        if k in ('fence', 'hedge'):
            L = (p.get('size') or {}).get('x', 8) or 8; a = pn.world(at, p.get('rotation', 0), -L / 2, 0); b = pn.world(at, p.get('rotation', 0), L / 2, 0)
            for q in pn.densify([a, b], 1):
                if z.road_near(q, 0): problem('fence across a road', at, z.road_near(q, 0)); break
        if 182 <= i < len(z.props) - 6:   # the new ones (they sit before the trades' six): clear of everything solid already there
            for j, o in enumerate(z.props):
                if j == i or o['kind'] in ('cavern', 'bridge'): continue
                if dist(V(o['at']), at) < 2.2 and k not in ('perch',): problem('new prop', k, p.get('name'), at, 'on', o['kind'], o.get('name'))
            if z.cave_cover(at, 3): problem('new prop over the passage', k, at)
    print('== fields')
    for c, s, rot, name in z.fields:
        for pts, w, rn_ in z.roads:
            hit = [q for q in pn.densify(pts, 1) if pn.outside_rect(q, c, s, rot) < w / 2 - .5]
            if hit: problem('field', name, 'on road', rn_, hit[0])
        for p in z.props:
            if p['kind'] in ('tree', 'pine', 'rock', 'house', 'barn', 'dead_oak', 'herb') and pn.outside_rect(V(p['at']), c, s, rot) < 0: problem('field', name, 'has', p['kind'], p.get('name'), V(p['at']))
        for c2, s2, k2, n2 in z.groves:
            if abs(c[0] - c2[0]) < (s[0] + s2[0]) / 2 - 1 and abs(c[1] - c2[1]) < (s[1] + s2[1]) / 2 - 1: problem('field', name, 'in grove', n2)
        for c2, r2, n2 in z.tall:
            if pn.outside_rect(c2, c, s, rot) < r2 - 1: problem('field', name, 'in tall grass', n2)
    print('== groves')
    gs = z.groves
    for i, (c, s, k, n) in enumerate(gs):
        if max(abs(c[0]) + s[0] / 2, abs(c[1]) + s[1] / 2) > z.half - 4: problem('grove past the edge', n)
        if z.wasting and k != 'dead' and c[0] + s[0] / 2 > z.wasting['x'] - z.wasting.get('fade', 20): problem('live grove in the grey', n)
        for j in range(i + 1, len(gs)):
            c2, s2, k2, n2 = gs[j]
            if abs(c[0] - c2[0]) < (s[0] + s2[0]) / 2 and abs(c[1] - c2[1]) < (s[1] + s2[1]) / 2: print('   groves overlap:', n, n2)
    print('== the edge: highest ground on the boundary line')
    t = np.linspace(-z.half + 1, z.half - 1, 560)
    for nm, x, y in (('north', t, np.full_like(t, z.half - 1)), ('south', t, np.full_like(t, -z.half + 1)), ('west', np.full_like(t, -z.half + 1), t), ('east', np.full_like(t, z.half - 1), t)):
        h = z.height(x, y); k = int(np.argmax(h)); print('  %-5s max %.1f m at (%.0f,%.0f)' % (nm, h[k], x[k], y[k]))
        if h[k] > 9.5: problem('ground over the boundary wall on the', nm)
    print('== reachable share of the walk grid: %.1f%%' % (100 * walk.reach.sum() / walk.ok.sum()))
    print('== critters')
    for c in z.z['life']['critters']:
        at = V(c['center'])
        if max(abs(at[0]), abs(at[1])) + c['radius'] > z.half - 6: problem('critters at the edge', c)
        if c['kind'] in ('deer', 'rabbit') and not walk.reachable(at, c['radius']): problem('game unreachable', c)
        if z.wasting and at[0] + c['radius'] > z.wasting['x']: problem('critters in the grey', c)
    print('== heights of note')
    for name, q in (('mouth', mouth), ('track end', (-121.5, 240)), ('ridge crown', (-45, 224)), ('over the Drop', (-72, 248)), ('brow', (-98, 243)), ('east shoulder', (-8, 236)), ('Overlook', (-50, 136.5)), ('Lark hill', (-232, -206)), ('road end', (-32, 184))):
        print('  %-14s %s  %.1f m' % (name, q, float(z.height(q[0], q[1]))))
    print('PROBLEMS', bad)
    return z, walk
if __name__ == '__main__': main()
