"""Gathering nodes for Khaven, the Shattered Peaks, the Ashland Rim and the Verdant Shore (BUILD_PLAN step 8, DESIGN 2.4).

Checks every candidate node against the zone's own data before it is written, by the rules ZoneBuilder.NodeClear and the
PlayMode NodePlacementTests apply, with a margin, plus a model of the ground (ZoneBuilder.HeightAt ported: rolling hills,
cliff shelves, the mountains' crags held off the kept ways, landmark pads; creeks and lakes are not carved) and a walk over it
from the player's start, so a node that would stand on a crag's face or behind one is caught here.

    python place_nodes.py                 check the four zones' candidates; exit 1 on any problem
    python place_nodes.py peaks           check one zone
    python place_nodes.py --probe peaks X Y [R]   list clear, walkable spots for an ore seam near (X, Y)
    python place_nodes.py --write         check, then write the nodes arrays and the herb props' node fields

The model is not the engine: grove trees, the forest edge and the mountains' strewn boulders come from the zone's random
stream and are not known here, and the ground is an approximation (Unity's Mathf.PerlinNoise as ported below). In the game
NodeClear moves a node up to 3 m off anything it finds there, so candidates keep 3 m inside every rule that moving could
break (the wood's edge for a windfall, the open for a herb). The PlayMode tests are the judge.
"""
import json, math, os, sys
from collections import deque
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ZONES = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent', 'Zones'))
PROF = os.path.normpath(os.path.join(ZONES, '..', 'Professions', 'professions.json'))

# ---------------------------------------------------------------- Unity's Mathf.PerlinNoise (improved noise, z = 0, normalised)
_p = [151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,190,6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,74,165,71,134,139,48,27,166,77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,102,143,54,65,25,63,161,1,216,80,73,209,76,132,187,208,89,18,169,200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,64,52,217,226,250,124,123,5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,223,183,170,213,119,248,152,2,44,154,163,70,221,153,101,155,167,43,172,9,129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,218,246,97,228,251,34,242,193,238,210,144,12,191,179,162,241,81,51,145,235,249,14,239,107,49,192,214,31,181,199,106,157,184,84,204,176,115,121,50,45,127,4,150,254,138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180]
PERM = np.array(_p + _p, dtype=np.int64)
def _fade(t): return t * t * t * (t * (t * 6 - 15) + 10)
def _grad(h, x, y):
    h = h & 15
    u = np.where(h < 8, x, y)
    v = np.where(h < 4, y, np.where((h == 12) | (h == 14), x, 0.0))
    return np.where((h & 1) == 0, u, -u) + np.where((h & 2) == 0, v, -v)
def perlin(x, y):
    x = np.asarray(x, dtype=np.float64); y = np.asarray(y, dtype=np.float64)
    fx = np.floor(x); fy = np.floor(y); X = fx.astype(np.int64) & 255; Y = fy.astype(np.int64) & 255
    x = x - fx; y = y - fy; u = _fade(x); v = _fade(y)
    A = PERM[X] + Y; AA = PERM[A]; AB = PERM[A + 1]; B = PERM[X + 1] + Y; BA = PERM[B]; BB = PERM[B + 1]
    n = (1 - v) * ((1 - u) * _grad(PERM[AA], x, y) + u * _grad(PERM[BA], x - 1, y)) + v * ((1 - u) * _grad(PERM[AB], x, y - 1) + u * _grad(PERM[BB], x - 1, y - 1))
    return (n + .69) / (.793 + .69)
def smooth(a, b, t):
    t = np.clip(t, 0, 1); t = -2 * t * t * t + 3 * t * t; return b * t + a * (1 - t)
def ilerp(a, b, v): return np.clip((np.asarray(v, dtype=np.float64) - a) / (b - a), 0, 1)

# ---------------------------------------------------------------- geometry
def V(a): return (float(a.get('x', 0)), float(a.get('y', 0))) if a else (0.0, 0.0)
def dist(a, b): return math.hypot(a[0] - b[0], a[1] - b[1])
def seg(p, a, b):
    ab = (b[0] - a[0], b[1] - a[1]); L = ab[0] ** 2 + ab[1] ** 2
    t = max(0.0, min(1.0, ((p[0] - a[0]) * ab[0] + (p[1] - a[1]) * ab[1]) / max(L, 1e-4)))
    return dist(p, (a[0] + ab[0] * t, a[1] + ab[1] * t))
def path(p, pts): return min(seg(p, pts[i], pts[i + 1]) for i in range(len(pts) - 1)) if len(pts) > 1 else 1e9
def seg_np(px, py, a, b):
    abx, aby = b[0] - a[0], b[1] - a[1]; L = max(abx * abx + aby * aby, 1e-4)
    t = np.clip(((px - a[0]) * abx + (py - a[1]) * aby) / L, 0, 1)
    return np.hypot(px - (a[0] + abx * t), py - (a[1] + aby * t))
def local(p, at, rot):
    """A world point in a prop's frame (Unity yaw: local x is (cos r, -sin r), local z is (sin r, cos r))."""
    r = math.radians(rot); dx, dz = p[0] - at[0], p[1] - at[1]
    return (dx * math.cos(r) - dz * math.sin(r), dx * math.sin(r) + dz * math.cos(r))
def world(at, rot, lx, lz):
    r = math.radians(rot); return (at[0] + lx * math.cos(r) + lz * math.sin(r), at[1] - lx * math.sin(r) + lz * math.cos(r))
def outside_rect(p, c, s, rot=0):
    """How far a point lies outside a rectangle (negative inside), as NodePlacementTests.OutsideRect."""
    r = -math.radians(rot); dx, dz = p[0] - c[0], p[1] - c[1]
    dx, dz = dx * math.cos(r) - dz * math.sin(r), dx * math.sin(r) + dz * math.cos(r)
    ox, oz = abs(dx) - s[0] / 2, abs(dz) - s[1] / 2
    return math.hypot(max(0, ox), max(0, oz)) if ox > 0 or oz > 0 else max(ox, oz)
def densify(pts, step):
    out = []
    for i in range(len(pts) - 1):
        n = max(1, math.ceil(dist(pts[i], pts[i + 1]) / step))
        for k in range(n): f = k / n; out.append((pts[i][0] + (pts[i + 1][0] - pts[i][0]) * f, pts[i][1] + (pts[i + 1][1] - pts[i][1]) * f))
    if pts: out.append(pts[-1])
    return out

# ---------------------------------------------------------------- caves (Hollow): rings from a cavern prop's plan
CROWSFOOT = [[0,-1.2,1.9,2.7,0],[0,1.5,2,2.9,0],[.3,5,2.4,3.3,0],[.6,8.5,4.4,4.4,0],[1,12,6.4,5.6,0],[.5,15.5,6.1,5.4,0],[-1.5,18.5,4,4.3,0],
 [-4.5,20.6,2.6,3.4,0],[-8,21.6,2.5,3.3,-.2],[-11.2,23.8,2.8,3.5,-.8],[-12.8,27.4,2.6,3.5,-2.2],[-12.6,31.2,2.5,3.6,-4.4],
 [-11.6,35,2.6,3.7,-7],[-10.4,38.8,2.7,3.8,-9.6],[-9.2,42.4,3,4,-11.4],[-6.8,46.2,4.8,4.8,-12.2],[-4.4,50,6,5.4,-12.5],[-1.6,53.8,5.6,5.2,-12.6],[.8,57,3.6,4.2,-12.8],
 [3.6,59.6,2.5,3.3,-13.1],[7.6,61.2,2.5,3.3,-13.6],[11.8,62.6,2.6,3.4,-14.3],[15.4,64.8,2.8,3.6,-15.1],[17.6,68.6,3.8,4.4,-15.7],
 [18.8,73.4,7.8,7.6,-16],[18.6,78.6,9.2,8.8,-16],[16.8,83.6,8.4,8.2,-16],[14,87.4,4.6,5,-16],[12,89.6,1.8,2.2,-16],[11.2,90.4,.3,.4,-16]]
ROOTDEEP = [[0,-1.2,2.2,3.2,0],[0,2,2.3,3.2,-.6],[.4,6,2.5,3.4,-2.6],[.8,10,2.7,3.6,-4.6],[1.2,14,3.2,4,-6.6],[1,18,5,5,-8],[.5,23,6.5,6,-8.6],[0,29,6.2,5.8,-8.9],
 [1,34,4,4.4,-9.2],[4,38,2.6,3.2,-9.8],[8,41.5,2.4,3,-10.6],[12,45,2.6,3.2,-11.4],[15,50,5,4.8,-12],[16,56,6,5.2,-12.2],[14.5,62,4.5,4.4,-12.4],
 [11,66,2.6,3.2,-13.2],[6.5,69.5,2.5,3.1,-14.6],[2,73,2.8,3.4,-16],[0,78,7,7,-16.5],[-1,84,9,8.5,-16.8],[-1.5,90,8.5,8,-16.8],[-2,95,7,8.5,-16.8],
 [-2,98,5,8,-16.8],[-2,100.5,.3,.4,-16.8]]
def cave_rings(at, yaw, plan, ground):
    """Hollow's rings: (x, z, half-width, height, floor y, land y), Catmull-Rom on x, z, half, height; the drop straight between rows."""
    def P(i): r = plan[max(0, min(len(plan) - 1, i))]; return r
    rings = []; mouth = ground(at[0], at[1])
    for s in range(len(plan) - 1):
        p0, p1, p2, p3 = P(s - 1), P(s), P(s + 1), P(s + 2)
        steps = max(1, math.ceil(math.hypot(p2[0] - p1[0], p2[1] - p1[1]) / .7))
        for k in range(steps + (1 if s + 2 == len(plan) else 0)):
            u = k / steps; u2 = u * u; u3 = u2 * u
            q = [.5 * (2 * p1[i] + (p2[i] - p0[i]) * u + (2 * p0[i] - 5 * p1[i] + 4 * p2[i] - p3[i]) * u2 + (3 * p1[i] - p0[i] - 3 * p2[i] + p3[i]) * u3) for i in range(4)]
            w = world(at, yaw, q[0], q[1])
            rings.append((w[0], w[1], max(.2, q[2]), max(.3, q[3]), mouth + p1[4] + (p2[4] - p1[4]) * u, ground(w[0], w[1])))
    return rings

# ---------------------------------------------------------------- a zone: its data, its ground, what stands in it
BUILDING = {'house': None, 'inn': (12, 8), 'barn': (12, 8), 'mill': (7, 6), 'keep': (7, 6)}
class Zone:
    def __init__(self, zid, all_zones):
        self.id = zid
        self.z = json.load(open(os.path.join(ZONES, zid + '.json'), encoding='utf-8-sig'))
        z = self.z; self.half = z['size'] / 2; self.seed = z.get('seed', 1); self.flat = z.get('flatRadius', 50); self.hill = z.get('hillHeight', 3)
        self.biome = z.get('biome', 'meadow'); self.wasting = z.get('wasting')
        self.props = [p for p in z.get('props', []) if p]
        self.roads = [([V(q) for q in r['points']], r.get('width', 4), r.get('name', '')) for r in z.get('roads', []) if len(r.get('points', [])) > 1]
        self.creeks = [([V(q) for q in w['points']], max(1, w.get('width', 4)), w.get('name', '')) for w in z.get('water', []) if len(w.get('points', [])) > 1]
        self.lakes = [(V(l['center']), l.get('radius', 10), l.get('name', '')) for l in (z.get('lakes') or [])]
        self.camps = [(V(c['center']), c.get('radius', 10), c.get('name', '')) for c in (z.get('camps') or [])]
        self.secrets = [(V(s['at']), s.get('id', '')) for s in (z.get('secrets') or [])]
        self.groves = [(V(g['center']), V(g['size']), g.get('kind', ''), g.get('name', '')) for g in (z.get('groves') or [])]
        self.fields = [(V(f['center']), V(f['size']), f.get('rotation', 0), f.get('name', '')) for f in (z.get('fields') or [])]
        self.tall = [(V(t['center']), t.get('radius', 6), t.get('name', '')) for t in (z.get('tallGrass') or [])]
        self.shapes = [dict(c=V(s['center']), r=s.get('radius', 8), h=s.get('height', 0), b=max(.5, s.get('blend', 6)), name=s.get('name', '')) for s in (z.get('shapes') or [])]
        self.exits = [(V(e['at']), e.get('radius', 5)) for e in z.get('exits', [])]
        self.arrivals = [V(e['arrive']) for oz in all_zones for e in oz.get('exits', []) if e.get('to') == z['id']]
        self.spawn = V(z['spawns']['player'])
        self.lifts = [(V(p['at']), math.cos(math.radians(p.get('rotation', 0))), math.sin(math.radians(p.get('rotation', 0))), ((p.get('size') or {}).get('x', 0) or 20) / 2, p['lift'])
                      for p in self.props if p['kind'] == 'cliff' and p.get('lift', 0)]
        self.keep = None
        if self.biome == 'mountain': self._keep_grid()
        for s in self.shapes: s['level'] = None
        for s in self.shapes:   # PrepareShapes: before any shape applies
            perch = s['h'] > 0; r = s['r'] + (max(.5, s['b']) if perch else 0)
            a = np.arange(32) * math.pi / 16; ys = self.raw(s['c'][0] + np.cos(a) * r, s['c'][1] + np.sin(a) * r)
            s['level'] = (ys.min() if perch else ys.mean()) + s['h']
        self.caves = [cave_rings(V(p['at']), p.get('rotation', 0), ROOTDEEP if p.get('variant', 0) == 1 else CROWSFOOT, lambda x, y: float(self.height(x, y)))
                      for p in self.props if p['kind'] == 'cavern']
        self.trunks = [V(p['at']) for p in self.props if p['kind'] in ('tree', 'pine', 'dead_oak', 'great_oak', 'giant_tree', 'treehouse')]
        self.blockers = self._blockers()

    # ---- ground
    def _keep_grid(self):
        z = self.z; circles = []; lines = []
        for pts, w, _ in self.roads:
            for i in range(len(pts) - 1): lines.append((pts[i], pts[i + 1], w / 2 + 3))
        for c in z.get('clearings', []): circles.append((V(c['center']), c.get('radius', 8) + 3))
        for c, r, _ in self.camps: circles.append((c, r + 4))
        for e, r in self.exits: circles.append((e, r + 4))
        for l in z.get('landmarks', []): circles.append((V(l['at']), min(l.get('radius', 10) * .5, 6)))
        for a in self.arrivals: circles.append((a, 5))
        sp = z['spawns']; circles += [(V(sp['player']), 5), (V(sp.get('companion')), 4), (V(sp.get('recovery')), 4)]
        for at, cos, sin, half, lift in self.lifts:
            along = (cos * half, -sin * half); f = -math.copysign(1, lift) * 7; front = (sin * f, cos * f)
            lines.append(((at[0] + along[0] + front[0], at[1] + along[1] + front[1]), (at[0] - along[0] + front[0], at[1] - along[1] + front[1]), 4))
        for p in self.props:
            if p['kind'] == 'cliff' or (p['kind'] == 'rock' and not p.get('interact')): continue
            if p['kind'] == 'wall':
                pts = [V(q) for q in p.get('points', [])]
                for i in range(len(pts) - 1): lines.append((pts[i], pts[i + 1], 3))
                continue
            s = p.get('size') or {}; circles.append((V(p['at']), max(s.get('x', 0), s.get('y', 0)) * .75 + 3))
        life = z.get('life') or {}
        for r in life.get('residents', []): circles.append((V(r['at']), 4))
        for c in life.get('critters', []): circles.append((V(c['center']), min(c.get('radius', 6) * .7, 8)))
        road = []
        for pts, _, _ in self.roads: road += densify(pts, 2)
        if road:
            R = np.array(road)
            for c, _ in list(circles):
                k = int(np.argmin((R[:, 0] - c[0]) ** 2 + (R[:, 1] - c[1]) ** 2)); lines.append((c, tuple(R[k]), 3))
        n = math.ceil(z['size'] / 2) + 1; self.keepN = n
        gx, gy = np.meshgrid(-self.half + np.arange(n) * 2.0, -self.half + np.arange(n) * 2.0)
        d = np.full(gx.shape, 1e9)
        for c, r in circles: d = np.minimum(d, np.hypot(gx - c[0], gy - c[1]) - r)
        for a, b, w in lines: d = np.minimum(d, seg_np(gx, gy, a, b) - w)
        self.keep = 1 - smooth(0, 1, d / 10)
    def keep_at(self, x, y):
        if self.keep is None: return np.ones_like(np.asarray(x, dtype=float))
        s, h, n = self.z['size'], self.half, self.keepN
        x = np.asarray(x, dtype=float).copy(); y = np.asarray(y, dtype=float).copy()
        x = np.where(x > h, s - x, np.where(x < -h, -s - x, x)); y = np.where(y > h, s - y, np.where(y < -h, -s - y, y))
        fx = np.clip((x + h) / 2, 0, n - 1.001); fy = np.clip((y + h) / 2, 0, n - 1.001); ix = fx.astype(int); iy = fy.astype(int); fx -= ix; fy -= iy
        k = self.keep
        return (k[iy, ix] * (1 - fx) + k[iy, ix + 1] * fx) * (1 - fy) + (k[iy + 1, ix] * (1 - fx) + k[iy + 1, ix + 1] * fx) * fy
    def crag(self, x, y):
        free = 1 - self.keep_at(x, y)
        cheb = np.maximum(np.abs(x), np.abs(y))
        rib = 1 - np.abs(perlin(x * .019 + 170, y * .019 + 60) * 2 - 1); peak = 1 - np.abs(perlin(x * .031 + 12, y * .031 + 140) * 2 - 1)
        knoll = np.clip((perlin(x * .043 + 33, y * .043 + 81) - .52) * 4.5, 0, 1); edge = smooth(0, 1, ilerp(self.half - 40, self.half - 6, cheb))
        rib = rib ** 4
        h = rib * 5 + knoll * knoll * 8 + edge * (6 + peak * peak * 14)
        return np.where(free > 0, h * free * (1 - .6 * smooth(0, 1, (cheb - self.half) / 60)), 0)
    def cliff_lift(self, x, y):
        best = np.zeros_like(np.asarray(x, dtype=float))
        for at, cos, sin, half, lift in self.lifts:
            wx = x - at[0]; wz = y - at[1]; lx = wx * cos - wz * sin; lz = (wx * sin + wz * cos) * math.copysign(1, lift)
            along = 1 - smooth(0, 1, (np.abs(lx) - half) / 14); behind = smooth(0, 1, (lz + 1) / 3) * (1 - smooth(0, 1, (lz - 16) / 12))
            v = np.where((lz < -1) | (lz > 28) | (np.abs(lx) > half + 14), 0, abs(lift) * along * behind)
            best = np.maximum(best, v)
        return best
    def raw(self, x, y):
        x = np.asarray(x, dtype=float); y = np.asarray(y, dtype=float)
        r = np.hypot(x, y); hills = smooth(0, 1, ilerp(self.flat, self.flat + 22, r))
        n = perlin(x * .035 + self.seed, y * .035 + self.seed * 2) * .75 + perlin(x * .11, y * .11) * .25
        h = hills * self.hill * n + (self.crag(x, y) * hills if self.keep is not None else 0) + self.cliff_lift(x, y)
        if self.wasting: h = np.where(x > self.wasting['x'] - 4, h * np.clip((self.wasting['x'] + 2 - x) / 6, 0, 1), h)
        return h
    def height(self, x, y):
        h = self.raw(x, y)
        for s in self.shapes:
            if s['level'] is None: continue
            d = np.hypot(np.asarray(x) - s['c'][0], np.asarray(y) - s['c'][1]) - s['r']
            h = np.where(d < s['b'], h + (s['level'] - h) * (1 - smooth(0, 1, d / s['b'])), h)
        return h

    # ---- what stands in it
    def footprint(self, p):
        k = p['kind']; s = p.get('size') or {}; sx, sy = s.get('x', 0), s.get('y', 0)
        if k == 'house': return ((sx or 7) / 2 + .8, (sy or 5) / 2 + .8) if sx else (4.3, 3.3)
        if k in ('inn', 'barn'): return (sx / 2 + .8, sy / 2 + .8) if sx else (6.8, 4.8)
        if k in ('mill', 'keep'): return (sx / 2 + .8, sy / 2 + .8) if sx else (4.3, 3.8)
        if k == 'tower': return ((sx or 4.4) / 2 + .3,) * 2
        if k == 'crypt': return (3.6, 4.6)
        if k == 'forge': return (2.9, 2.4)
        if k in ('tannery', 'shelter'): return (2.2, 2.1)
        return None
    def in_building(self, q, margin=0):
        for p in self.props:
            h = self.footprint(p)
            if not h: continue
            lx, lz = local(q, V(p['at']), p.get('rotation', 0))
            if abs(lx) <= h[0] + margin and abs(lz) <= h[1] + margin: return p.get('name') or p['kind']
        return None
    def _blockers(self):
        """Solid things (colliders, or their spread), as circles (at, r) and boxes (at, rot, hx, hz). A perch is its whole ring of
        boulders (out to its radius plus 4.8 m); a giant tree its root flare (4.5 m at scale 1)."""
        circ, box = [], []
        for p in self.props:
            k = p['kind']; at = V(p.get('at')); rot = p.get('rotation', 0); s = p.get('size') or {}; sx, sy = s.get('x', 0), s.get('y', 0); sc = p.get('scale', 1) or 1
            fp = self.footprint(p)
            if fp: box.append((at, rot, fp[0], fp[1], p.get('name') or k)); continue
            if k == 'cliff':
                L = sx or 20; lift = p.get('lift', 0)
                if lift: side = math.copysign(1, lift); box.append((world(at, rot, 0, side * 1.6), rot, L / 2 + 1.5, 3.6, p.get('name') or k)); box.append((world(at, rot, 0, side * 4.2), rot, L / 2 + 1, 1.8, (p.get('name') or k) + ' lip'))
                else: box.append((world(at, rot, 0, .8), rot, L / 2 + 2.4, 3.2, p.get('name') or k))
                continue
            if k == 'wall':
                pts = [V(q) for q in p.get('points', [])]
                for i in range(len(pts) - 1):
                    a, b = pts[i], pts[i + 1]; c = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2); L = dist(a, b)
                    box.append((c, -math.degrees(math.atan2(b[1] - a[1], b[0] - a[0])), L / 2 + .5, .9, p.get('name') or k))
                continue
            if k in ('fence', 'hedge', 'ruin', 'spine', 'fallen_giant'):
                L = sx or {'fence': 8, 'hedge': 6, 'ruin': 6, 'spine': 24, 'fallen_giant': 36}[k]
                box.append((at, rot, L / 2 + .4, {'fence': .4, 'hedge': .8, 'ruin': .8, 'spine': 1.6, 'fallen_giant': 3}[k], p.get('name') or k)); continue
            if k == 'ruined_house': box.append((at, rot, (sx or 8) / 2 + .5, (sy or 6) / 2 + .5, p.get('name') or k)); continue
            r = {'rock': 1.15 * (1 + p.get('variant', 0) * .6), 'crates': 1.3, 'barrels': 1.3, 'cart': 2.2, 'wagon': 3, 'stall': 2.6, 'well': 1.6, 'woodpile': 1.6, 'gallows': 1.6,
                 'brazier': .7, 'idol': .9, 'shrine': 2.4, 'wayshrine': 1.3, 'monolith': 2.6, 'rib': (sx or 6) / 2 + .5, 'brood': (sx or 5) / 2 + .5, 'perch': (sx or 8) + 4.8,
                 'cave': (sx or 20) / 2 + 2, 'giant_tree': 4.5 * sc, 'treehouse': 4.5 * sc, 'waterfall': (sx or 6) / 2 + 2, 'mushrooms': (sx or 2) * .5 * sc + .3,
                 'dead_oak': 1 * sc, 'tree': .6, 'pine': .6, 'great_oak': 3, 'signpost': .4, 'lamp': .4, 'grave': .5, 'gate': 4, 'coop': 2, 'haystack': 1.6, 'cavern': 0,
                 'bridge': 0, 'herb': 0, 'wallow': 0}.get(k, 1.5)
            if r > 0: circ.append((at, r, p.get('name') or k))
        return circ, box
    def blocked(self, q, pad):
        """The solid thing within pad of q, if any."""
        circ, box = self.blockers
        for at, r, name in circ:
            if dist(q, at) < r + pad: return name
        for at, rot, hx, hz, name in box:
            lx, lz = local(q, at, rot)
            if abs(lx) < hx + pad and abs(lz) < hz + pad: return name
        return None
    def near_water(self, q, margin):
        for c, r, name in self.lakes:
            if dist(q, c) < r * 1.2 + margin: return name or 'a lake'
        for pts, w, name in self.creeks:
            if path(q, pts) < w * 1.3 + 2 + margin: return name or 'a creek'
        return None
    def road_near(self, q, margin):
        for pts, w, name in self.roads:
            d = path(q, pts)
            if d < w / 2 + margin: return '%s %.1f' % (name, d)
        return None
    def cave_at(self, q):
        """The ring a point stands over and how far in from its wall: (ring, metres inside), or None."""
        best = None
        for rings in self.caves:
            for i, r in enumerate(rings):
                d = dist(q, (r[0], r[1]))
                if d <= r[2] and (best is None or d < dist(q, (best[0][0], best[0][1]))): best = (r, r[2] - d, i, rings)
        return best
    def cave_cover(self, q, margin):
        for rings in self.caves:
            for r in rings:
                if r[5] - (r[4] + r[3] * 1.19 + .15) < 3 and dist(q, (r[0], r[1])) < r[2] + margin + .1: return True
        return False

# ---------------------------------------------------------------- walking: a 1 m grid from the player's start
class Walk:
    def __init__(self, zone, step=1.0):
        self.z = zone; self.step = step; h = zone.half - 2
        n = int(2 * h / step) + 1; self.n = n; self.o = -h
        xs = self.o + np.arange(n) * step; gx, gy = np.meshgrid(xs, xs)
        H = zone.height(gx, gy)
        # The slope between neighbours (the ground mesh is 1.25 m; agents climb 45 degrees).
        sx = np.abs(np.diff(H, axis=1)) / step; sy = np.abs(np.diff(H, axis=0)) / step
        self.H = H; self.sx = sx; self.sy = sy
        ok = np.ones((n, n), dtype=bool)
        circ, box = zone.blockers
        for at, r, _ in circ: ok &= np.hypot(gx - at[0], gy - at[1]) >= r + .5
        for at, rot, hx, hz, _ in box:
            rr = math.radians(rot); dx, dz = gx - at[0], gy - at[1]
            lx = dx * math.cos(rr) - dz * math.sin(rr); lz = dx * math.sin(rr) + dz * math.cos(rr)
            ok &= ~((np.abs(lx) < hx + .5) & (np.abs(lz) < hz + .5))
        for c, r, _ in zone.lakes: ok &= np.hypot(gx - c[0], gy - c[1]) >= r * .85   # swim-deep water
        self.ok = ok; self.reach = self._flood(zone.spawn)
    def cell(self, q): return int(round((q[1] - self.o) / self.step)), int(round((q[0] - self.o) / self.step))
    def _flood(self, start):
        n = self.n; seen = np.zeros((n, n), dtype=bool); j, i = self.cell(start)
        best = None
        for dj in range(-3, 4):
            for di in range(-3, 4):
                if 0 <= j + dj < n and 0 <= i + di < n and self.ok[j + dj, i + di]: best = (j + dj, i + di); break
            if best: break
        if not best: return seen
        dq = deque([best]); seen[best] = True; lim = 1.0   # tan 45
        while dq:
            j, i = dq.popleft()
            for dj, di in ((0, 1), (0, -1), (1, 0), (-1, 0)):
                a, b = j + dj, i + di
                if not (0 <= a < n and 0 <= b < n) or seen[a, b] or not self.ok[a, b]: continue
                s = self.sx[j, min(i, b)] if dj == 0 else self.sy[min(j, a), i]
                if s > lim * .9: continue   # a margin under 45 degrees
                seen[a, b] = True; dq.append((a, b))
        return seen
    def reachable(self, q, within=2.4):
        j0, i0 = self.cell(q); k = int(math.ceil(within / self.step))
        for dj in range(-k, k + 1):
            for di in range(-k, k + 1):
                j, i = j0 + dj, i0 + di
                if 0 <= j < self.n and 0 <= i < self.n and self.reach[j, i] and math.hypot(dj, di) * self.step <= within: return True
        return False
    def steep(self, q, r):
        """The steepest rise per metre within r of q (1 is 45 degrees)."""
        j0, i0 = self.cell(q); k = int(math.ceil(r / self.step)); worst = 0
        for dj in range(-k, k + 1):
            for di in range(-k, k):
                j, i = j0 + dj, i0 + di
                if 0 <= j < self.n and 0 <= i < self.n - 1: worst = max(worst, self.sx[j, i])
                if 0 <= j < self.n - 1 and 0 <= i + 1 < self.n: worst = max(worst, self.sy[j, i + 1])
        return worst

# ---------------------------------------------------------------- the node kinds (professions.json) and their footprints
KINDS = {n['id']: n for n in json.load(open(PROF, encoding='utf-8-sig'))['nodes']}
def foot(look): return 2.1 if look == 'ore' else 2.1 * 1.35 if look == 'ore_rich' else 3 if look == 'windfall' else .6
TIER = {'khaven': 20, 'peaks': 40, 'ashrim': 60, 'verdant': 80}

def windfall_ok(zone, at, yaw):
    """WindfallYaw: the first of twelve turns that lies the whole windfall clear; None when none does (it lies at the best then)."""
    for k in range(12):
        y = yaw + (1 if k % 2 == 0 else -1) * ((k + 1) // 2) * 30; r = math.radians(y); d = (math.cos(r), -math.sin(r))
        a = (at[0] - d[0] * 2.9, at[1] - d[1] * 2.9); b = (at[0] + d[0] * 2.5, at[1] + d[1] * 2.5); m = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
        if any(zone.road_near(e, .5) or zone.near_water(e, .5) or zone.in_building(e) or zone.cave_cover(e, .5) or zone.blocked(e, .4) for e in (a, m, b)): continue
        if all(seg(t, a, b) >= 1.3 for t in zone.trunks): return y
    return None

def check(zone, walk, n, others):
    """Every rule the node would break, as words (empty: it is fine)."""
    probs = []; at = n['at']; kind = KINDS.get(n['node']); under = n.get('under', False)
    if not kind: return ['unknown node ' + n['node']]
    look = kind['look']; f = foot(look)
    if kind['skill'] != TIER[zone.id]: probs.append('tier %d is not the zone\'s %d' % (kind['skill'], TIER[zone.id]))
    if not under and max(abs(at[0]), abs(at[1])) > zone.half - 14: probs.append('in the forest edge')
    if zone.wasting and at[0] > zone.wasting['x'] - 8: probs.append('in the Wasting')
    cave = zone.cave_at(at)
    if under:
        if not cave: probs.append('not over a cave floor')
        elif cave[1] < 1.2: probs.append('%.1f m from the cave wall' % cave[1])
        if look != 'ore_rich': probs.append('under, but no rich seam')
    elif n.get('prop'):
        # A herb prop worked as a node stands where the zone always had it (moving it would move the grove trees round it):
        # only the water itself and the other nodes are its business here.
        for pts, w, name in zone.creeks:
            if path(at, pts) < w * .8: probs.append('in ' + name)
        return probs
    else:
        r = zone.road_near(at, 1.5)
        if r: probs.append('road ' + r)
        w = zone.near_water(at, 1.5)
        if w: probs.append('water: ' + w)
        b = zone.in_building(at, 1)
        if b: probs.append('building ' + b)
        if zone.cave_cover(at, 3): probs.append('over a cave near the surface')
        if look == 'ore_rich': probs.append('a rich seam out of the cave')
    for c, r, name in zone.camps:
        if dist(at, c) < r * 1.42 + f + .8: probs.append('camp %s (%.1f < %.1f)' % (name, dist(at, c), r * 1.42 + f + .8))
    for s, sid in zone.secrets:
        if dist(at, s) < 6.5: probs.append('secret %s %.1f' % (sid, dist(at, s)))
    for name, q in others:
        if (q[0], q[1]) != (at[0], at[1]) and dist(at, q) < 6.2: probs.append('node %s %.1f' % (name, dist(at, q)))
    for e, r in zone.exits:
        if dist(at, e) < r + 6: probs.append('at an exit')
    for p in zone.props:   # E takes the nearest usable thing: a node never sits on a quest prop
        if p.get('interact') and p['kind'] != 'herb' and dist(at, V(p['at'])) < 6: probs.append('by the usable %s %.1f' % (p.get('name') or p['kind'], dist(at, V(p['at']))))
    for a in zone.arrivals + [zone.spawn]:
        if dist(at, a) < 8: probs.append('where you arrive')
    if not under:
        for t in zone.trunks:
            if dist(at, t) < 2.8: probs.append('trunk %.1f' % dist(at, t))
        blk = zone.blocked(at, .9 if look == 'windfall' else .7)
        if blk: probs.append('in ' + blk)
    # Where it belongs (NodePlacementTests), 3 m inside the rule (NodeClear may move it that far).
    groves = [(outside_rect(at, c, s), k, name) for c, s, k, name in zone.groves]
    if look == 'windfall':
        if not any(-1 <= o <= 3 for o, k, name in groves): probs.append('no wood\'s edge (%s)' % ', '.join('%s %+.1f' % (name, o) for o, k, name in sorted(groves)[:2]))
        if not under and windfall_ok(zone, at, n.get('rotation', 0)) is None: probs.append('no way to lie clear')
    if look == 'herb':
        inside = [name for o, k, name in groves if o < 3]
        if inside: probs.append('herb by or in a wood: ' + ', '.join(inside))
        for c, s, rot, name in zone.fields:
            if outside_rect(at, c, s, rot) < 3: probs.append('herb on the field ' + name)
        for c, r, name in zone.tall:
            if dist(at, c) < r + 1 and not (n['node'] == 'node.dewfern' and 'fern' in name.lower()): probs.append('herb in tall grass')   # dewfern grows with the ferns
    if look in ('ore', 'ore_rich') and not under:
        for o, k, name in groves:
            if o < 1.5: probs.append('seam in the wood ' + name)
    if look == 'ore':
        crag = False
        for p in zone.props:
            k = p['kind']; s = (p.get('size') or {}).get('x', 0)
            if k == 'cliff' and dist(V(p['at']), at) < (s or 20) / 2 + 4 - 3: crag = True
            if k in ('rock', 'perch') and dist(V(p['at']), at) < 6 - 2: crag = True
        if any(s['h'] > 8 and dist(s['c'], at) < s['r'] - 3 for s in zone.shapes): crag = True
        if not crag: probs.append('no crag or rock by it')
    # Walkable to, and not on a face.
    if not under:
        if not walk.reachable(at): probs.append('no walk to it from the start')
        st = walk.steep(at, .7)
        if st > .8: probs.append('steep ground (%.2f)' % st)
    return probs

# ---------------------------------------------------------------- the candidates, zone by zone (see the notes on each)
def ore_at_cliff(zone, name, along, out, node, label=''):
    """A seam out from a cliff's foot, turned so its rock backs onto the face and its ore looks out: out > 0 on a free crag's
    front (-z) or a scarp's low side, out < 0 behind a free crag. along: metres along the cliff from its centre."""
    p = next(p for p in zone.props if p['kind'] == 'cliff' and p.get('name') == name)
    side = math.copysign(1, p['lift']) if p.get('lift') else 1
    at = world(V(p['at']), p.get('rotation', 0), along, -side * out)
    turn = (0 if side > 0 else 180) + (180 if out < 0 else 0)
    return dict(node=node, at=(round(at[0], 1), round(at[1], 1)), rotation=round((p.get('rotation', 0) + turn) % 360), label=label)
def N(node, x, y, label='', rotation=0, **kw): d = dict(node=node, at=(x, y), rotation=rotation, label=label); d.update(kw); return d

CANDIDATES = {}   # filled in below, zone by zone

def khaven(z):
    """Khaven, tier 2. Bog-iron seams at the feet of the Carrion Cliffs, the Carrion Heights, the Grey scarp and the North
    ridge; black-pine windfalls at the edges of the pine woods; the seven Mourner's cap props on the creek banks and three more
    round the Drowned graveyard (kept out of the hollows' camp)."""
    O = 'node.bogiron'; W = 'node.blackpine'; H = 'node.mourners_cap'
    return [
        ore_at_cliff(z, 'Carrion Cliffs', -8, 4.5, O, 'cliffs W'), ore_at_cliff(z, 'Carrion Cliffs', 8, 4.5, O, 'cliffs E'),
        ore_at_cliff(z, 'Carrion Cliffs (east)', 0, 4.5, O, 'cliffs east'), ore_at_cliff(z, 'Carrion Cliffs (low)', 0, 4.5, O, 'cliffs low'),
        ore_at_cliff(z, 'Carrion Cliffs (far)', -8, -5.5, O, 'cliffs far'),
        ore_at_cliff(z, 'The Carrion Heights', -12, 4, O, 'heights N'), ore_at_cliff(z, 'The Carrion Heights', 8, 4, O, 'heights S'),
        ore_at_cliff(z, 'Grey scarp', 0, 4.5, O, 'grey scarp'), ore_at_cliff(z, 'Grey scarp', 9, 4.5, O, 'grey scarp E'),
        ore_at_cliff(z, 'North ridge', 0, -5.5, O, 'north ridge'),
        N(W, -14, 42, 'N pines S1'), N(W, 24, 42, 'N pines S2'), N(W, 58, 64, 'ridge W', 90), N(W, 80, 48, 'ridge S'),
        N(W, 130, 46, 'ridge-east E', 90), N(W, 112, 104, 'ridge-north S'), N(W, -66, 138, 'bound-wall S'), N(W, -86, 150, 'bound-wall W', 90),
        N(H, -14, -104, 'graves W', item='item.mourners_cap'), N(H, 10, -114, 'graves S', item='item.mourners_cap'), N(H, 26, -112, 'graves E', item='item.mourners_cap'),
    ]
CANDIDATES['khaven'] = khaven

def peaks(z):
    """The Shattered Peaks, tier 3. Adit-iron seams by the Sealed Adit's face and the boulder by its yard, at the feet of the
    Umbra, Goat-path, East and South scarps and the Switchback crag (none on the spoil heap: its ring of boulders fills it);
    stone-pine windfalls at the pine woods' edges and one in the Avalanche deadfall; tarnwort round the Cold Tarn and on the
    Shieling's hay meadow (off the mown field itself)."""
    O = 'node.adit'; W = 'node.stonepine'; H = 'node.tarnwort'
    return [
        ore_at_cliff(z, 'Adit face', -10, 4, O, 'adit N'), ore_at_cliff(z, 'Adit face', 8, 4, O, 'adit S'),
        N(O, -141, -33, 'adit rock', 270), ore_at_cliff(z, 'Goat-path scarp', 16, 4, O, 'goat-path S'),
        ore_at_cliff(z, 'Umbra scarp', -8, 4, O, 'umbra N'), ore_at_cliff(z, 'Umbra scarp', 8, 4, O, 'umbra S'),
        ore_at_cliff(z, 'East wall', -6, 4, O, 'east wall'), ore_at_cliff(z, 'South scarp', 0, 4, O, 'south scarp'),
        ore_at_cliff(z, 'Goat-path scarp', 0, 4, O, 'goat-path'), ore_at_cliff(z, 'Switchback crag', 0, 4.5, O, 'switchback'),
        N(W, -59, 32, 'wolf pines S'), N(W, -44, 84, 'high pines S'), N(W, -3, 38, 'gate pines E', 90), N(W, -133, -8, 'ore-road pines N'),
        N(W, 70, -62, 'east pines W', 90), N(W, -12, -59, 'south pines N'), N(W, 137, 40.5, 'deadfall N'), N(W, -112, 82, 'umbra pines E', 90),
        N(H, -36.5, 136, 'tarn W'), N(H, -12, 131, 'tarn E'), N(H, -30.3, 125.2, 'tarn S1'), N(H, -24, 123.5, 'tarn S2'), N(H, -17.8, 125.2, 'tarn SE'),
        N(H, -24, 148.5, 'tarn N'), N(H, -34.8, 142.2, 'tarn NW'),
        N(H, -10.5, -126.8, 'meadow W'), N(H, 6, -136, 'meadow S'), N(H, -12.5, -114.6, 'meadow N'),
    ]
CANDIDATES['peaks'] = peaks

def ashrim(z):
    """The Ashland Rim, tier 4: thin pickings, spread out and one at a time. Cinder seams at the feet of the Eastern Ridge, the
    South and North rims, the West scarp and the Walled Mouth scarp (none on the Ash Pit's rim: the cultists' camp fills it);
    fallen ash-snags at the dead woods' edges, one wood each (none in the Last Orchard, where the hounds lie); single
    cinder-thistles in the open ash between them."""
    O = 'node.cinder'; W = 'node.snag'; H = 'node.cinder_thistle'
    return [
        ore_at_cliff(z, 'Eastern Ridge (north)', -12, 4.5, O, 'ridge N'), ore_at_cliff(z, 'Eastern Ridge (south)', 10, 4.5, O, 'ridge S'),
        ore_at_cliff(z, 'Eastern Ridge (far north)', 0, 4.5, O, 'ridge far N'), ore_at_cliff(z, 'Eastern Ridge (far south)', 0, 4.5, O, 'ridge far S'),
        ore_at_cliff(z, 'South rim', -8, 4.5, O, 'south rim W'), ore_at_cliff(z, 'South rim', 8, 4.5, O, 'south rim E'),
        ore_at_cliff(z, 'West scarp', -8, 4.5, O, 'west scarp N'), ore_at_cliff(z, 'West scarp', 10, 4.5, O, 'west scarp S'),
        ore_at_cliff(z, 'North rim', -6, 4.5, O, 'north rim'), ore_at_cliff(z, 'Walled Mouth scarp', -12, 4, O, 'walled mouth W'),
        N(W, -80, -6, 'ashen snags S'), N(W, -67, 99, 'cinderfold S'), N(W, 25, 62, 'rim snags W', 90), N(W, 27, -100, 'south snags E', 90),
        N(W, -81, -96, 'grey thicket E', 90), N(W, -133, -24, 'west snags E', 90), N(W, 106, 74, 'ridge-back W', 90), N(W, -62, 138, 'north snags S'),
        N(H, -120, -60, 'ash W'), N(H, -30, -60, 'ash S'), N(H, 10, -20, 'ash middle'), N(H, -10, 50, 'ash N'), N(H, -120, 110, 'ash NW'),
        N(H, 110, -90, 'ash E'), N(H, 40, -120, 'ash SE'), N(H, -60, 80, 'cinderfold W'), N(H, 120, 10, 'ridge-back'), N(H, -128, 44, 'wayside'),
    ]
CANDIDATES['ashrim'] = ashrim

def verdant(z):
    """The Verdant Shore, tier 5: rich pickings. Veridian seams at the feet of the basalt crags and on the Ridge of Long Shadows'
    shoulders, and four rich ones on the Root-Mother's Deep's floor (the Gallery's far end, the Sap Well's, both sides of the
    Heart: each clear of the deep's withered, walkers and briars); ghost-oak windfalls at the edges of the canopy woods and the
    Tappers' wood (none by the Fallen Ghost-Oak, where the spiders nest); dewfern round the Mistmere's shore and the Fern Hollow."""
    O = 'node.veridian'; R = 'node.veridian_rich'; W = 'node.ghostoak'; H = 'node.dewfern'
    def ring(cx, cy, r, a, label): return N(H, round(cx + math.cos(math.radians(a)) * r, 1), round(cy + math.sin(math.radians(a)) * r, 1), label)
    return [
        ore_at_cliff(z, 'Basalt crag', 0, 4.5, O, 'crag'), ore_at_cliff(z, 'Basalt crag (south)', 0, 4.5, O, 'crag S'),
        ore_at_cliff(z, 'Basalt crag (north)', 0, 4.5, O, 'crag N'), ore_at_cliff(z, 'Basalt crag (far south)', 0, 4.5, O, 'crag far S'),
        N(O, 157, -46, 'south shoulder', 90), N(O, 156, 88, 'north shoulder', 90),
        N(R, -109, 164.5, 'deep gallery', under=True), N(R, -97.5, 192.5, 'deep sap well', under=True),
        N(R, -106, 218, 'deep heart E', under=True), N(R, -120, 218, 'deep heart W', under=True),
        N(W, 122, 26, 'ridge-foot E', 90), N(W, -119, -46.5, 'tappers N'), N(W, -133, -70, 'tappers W', 90), N(W, -79, -88, 'mere E', 90),
        N(W, -48, 62.5, 'westbank N'), N(W, 48, 67, 'riverbank N'), N(W, 113, 108, 'ridge-foot N W', 90), N(W, -4, 105, 'rootfast N'),
        ring(-56, -92, 20, 20, 'mere NE'), ring(-56, -92, 20, 70, 'mere N'), ring(-56, -92, 20, 140, 'mere NW'), ring(-56, -92, 20, 330, 'mere SE'),
        ring(34, -74, 11, 340, 'fern E'), ring(34, -74, 11, 15, 'fern NE'), ring(34, -74, 11, 50, 'fern N'),
        ring(34, -74, 11, 140, 'fern NW'), ring(34, -74, 11, 185, 'fern W'), ring(34, -74, 11, 230, 'fern SW'),
    ]
CANDIDATES['verdant'] = verdant

def load_all():
    allz = [json.load(open(os.path.join(ZONES, f), encoding='utf-8-sig')) for f in os.listdir(ZONES) if f.endswith('.json')]
    return allz

def run(ids, write=False):
    allz = load_all(); bad = 0; todo = []
    for zid in ids:
        zone = Zone(zid, allz); walk = Walk(zone)
        cands = CANDIDATES[zid](zone)
        herbs = [dict(node=HERB_PROPS[zid][1], at=V(h['at']), name='prop %s' % h.get('name'), prop=True) for h in zone.props if zid in HERB_PROPS and h['kind'] == 'herb' and h.get('name') == HERB_PROPS[zid][0]]
        others = [(c.get('label', c['node']), c['at']) for c in cands] + [(h['name'], h['at']) for h in herbs]
        print('== %s (%d nodes + %d herb props)' % (zid, len(cands), len(herbs)))
        counts = {}
        for c in cands + herbs:
            look = KINDS[c['node']]['look']; counts[look] = counts.get(look, 0) + 1
            pr = check(zone, walk, c, others)
            bad += bool(pr)
            print('  %-16s %-20s (%7.1f,%7.1f) steep %.2f  %s' % (c.get('label', c.get('name', ''))[:16], c['node'], c['at'][0], c['at'][1], walk.steep(c['at'], .7), '; '.join(pr) if pr else 'ok'))
        print('  counts', counts)
        if counts.get('ore', 0) + counts.get('ore_rich', 0) != 10 or counts.get('windfall', 0) != 8 or counts.get('herb', 0) != 10: print('  COUNTS WRONG'); bad += 1
        written = zone.z.get('nodes')
        if written is not None:
            # Already written: the file must hold exactly these candidates (and the herb props their node).
            want = [(c['node'], round(c['at'][0], 1), round(c['at'][1], 1), round(c.get('rotation', 0) or 0, 1), bool(c.get('under')), c.get('item')) for c in cands]
            have = [(n['node'], round(n['at']['x'], 1), round(n['at']['y'], 1), round(n.get('rotation', 0), 1), bool(n.get('under')), n.get('item')) for n in written]
            if want != have: print('  the file differs from the candidates: --write writes them'); todo.append((zid, cands))
            if zid in HERB_PROPS and any(h.get('node') != HERB_PROPS[zid][1] for h in zone.props if h['kind'] == 'herb' and h.get('name') == HERB_PROPS[zid][0]): print('  A HERB PROP HAS NO NODE'); bad += 1
        else: todo.append((zid, cands))
    print('problems:', bad, '' if not todo else '(to write: %s)' % ', '.join(z for z, _ in todo))
    if write:
        if bad: print('nothing written: fix the problems first')
        else:
            for zid, cands in todo: write_zone(zid, cands, HERB_PROPS.get(zid)); print('written:', zid)
    return bad

def probe(zid, x, y, r=12, look='ore'):
    allz = load_all(); zone = Zone(zid, allz); walk = Walk(zone)
    node = {'ore': 'node.' + {'khaven': 'bogiron', 'peaks': 'adit', 'ashrim': 'cinder', 'verdant': 'veridian'}[zid],
            'windfall': 'node.' + {'khaven': 'blackpine', 'peaks': 'stonepine', 'ashrim': 'snag', 'verdant': 'ghostoak'}[zid],
            'herb': 'node.' + {'khaven': 'mourners_cap', 'peaks': 'tarnwort', 'ashrim': 'cinder_thistle', 'verdant': 'dewfern'}[zid]}[look]
    cands = CANDIDATES[zid](zone)
    others = [(c.get('label', c['node']), c['at']) for c in cands if dist(c['at'], (x, y)) > .5]
    if zid in HERB_PROPS: others += [('prop', V(h['at'])) for h in zone.props if h['kind'] == 'herb' and h.get('name') == HERB_PROPS[zid][0]]
    for d in np.arange(0, r + .1, 1.0):
        for k in range(max(1, int(d * 2))):
            a = k * 2 * math.pi / max(1, int(d * 2)); q = (round(x + math.cos(a) * d, 1), round(y + math.sin(a) * d, 1))
            pr = check(zone, walk, dict(node=node, at=q), others)
            print('ok' if not pr else '  ', q, '; '.join(pr))

# ---------------------------------------------------------------- writing
HERB_PROPS = {'khaven': ("Mourner's cap", 'node.mourners_cap')}   # existing herb props worked as nodes (the quest's still given)
def num(v): v = round(float(v), 1); return str(int(v)) if v.is_integer() else str(v)
def write_zone(zid, cands, herbprop):
    path_ = os.path.join(ZONES, zid + '.json'); raw = open(path_, 'rb').read(); crlf = b'\r\n' in raw
    t = raw.decode('utf-8-sig').replace('\r\n', '\n'); nl = '\r\n' if crlf else '\n'
    if herbprop:
        name, node = herbprop
        lines = t.split('\n'); out = []; inside = False; done = 0
        for i, line in enumerate(lines):
            out.append(line)
            if line.strip() == '"name": "%s",' % name.replace("'", "'"): inside = True
            if inside and line.strip().startswith('"item":') and not line.rstrip().endswith(','):
                out[-1] = line.rstrip() + ','; out.append(line[:len(line) - len(line.lstrip())] + '"node": "%s"' % node); inside = False; done += 1
        t = '\n'.join(out); print('  %d %s props worked as nodes' % (done, name))
    if '"nodes"' in t:   # written before: the array (always last) is written again
        k = t.index(',\n  "nodes": ['); t = t[:k] + '\n}' + ('\n' if t.endswith('\n') else '')
    rows = []
    for c in cands:
        parts = ['"node": "%s"' % c['node']]
        if c.get('item'): parts.append('"item": "%s"' % c['item'])
        parts.append('"at": { "x": %s, "y": %s }' % (num(c['at'][0]), num(c['at'][1])))
        if c.get('rotation'): parts.append('"rotation": %s' % num(c['rotation']))
        if c.get('under'): parts.append('"under": true')
        rows.append('    { ' + ', '.join(parts) + ' }')
    end = '\n' if t.endswith('\n') else ''; body = t.rstrip(); assert body.endswith('}'); body = body[:-1].rstrip(); assert body.endswith(']')
    t = body + ',\n  "nodes": [\n' + ',\n'.join(rows) + '\n  ]\n}' + end
    json.loads(t)
    open(path_, 'wb').write(t.replace('\n', nl).encode('utf-8'))

if __name__ == '__main__':
    a = sys.argv[1:]
    if a and a[0] == '--probe': probe(a[1], float(a[2]), float(a[3]), float(a[4]) if len(a) > 4 else 12, a[5] if len(a) > 5 else 'ore'); sys.exit(0)
    write = '--write' in a; ids = [x for x in a if not x.startswith('--')] or ['khaven', 'peaks', 'ashrim', 'verdant']
    sys.exit(1 if run(ids, write) else 0)
