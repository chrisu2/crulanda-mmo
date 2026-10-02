"""body.py - the smooth figure (playtest note 12: "time to retire the block characters", "armor is way too blocky. needs to
feel flowing"; Chris chose to build them in code). The reference for the C# port: a skeleton, one skinned mesh lofted
along it, region colours, and poses, previewed with tools/wip/render/mesh_view.py.

Frame and contracts kept from ActorVisual (so worn gear still fits while it is refitted): body-local metres, origin 1 m
above the soles, +Z forward, +X the figure's right, Unity's left-handed axes. Shoulder pivots "Arm L/R" at (-/+.31, .53, 0)
with the hand point .62 below them; hip pivots "Leg L/R" at (-/+.12, -.08, 0); soles at y -1; head top about +.96.

    python body.py            preview sheets into this folder: rest, dressed, walking, posed
"""
import math, os, sys
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(os.path.dirname(HERE), 'render'))
import mesh_view as mv

# ---------------------------------------------------------------------------------------------------------- skeleton
# name: (parent, rest position in body space). Rotations are applied at each bone's own position.
BONES = {
    'Body':      (None,       (0, 0, 0)),
    'Spine':     ('Body',     (0, .08, 0)),
    'Chest':     ('Spine',    (0, .34, 0)),
    'Neck':      ('Chest',    (0, .62, -.01)),
    'Head':      ('Neck',     (0, .72, 0)),
    'Arm L':     ('Body',     (-.31, .53, 0)),
    'Forearm L': ('Arm L',    (-.315, .245, -.01)),
    'Hand L':    ('Forearm L', (-.315, -.04, .01)),
    'Arm R':     ('Body',     (.31, .53, 0)),
    'Forearm R': ('Arm R',    (.315, .245, -.01)),
    'Hand R':    ('Forearm R', (.315, -.04, .01)),
    'Leg L':     ('Body',     (-.12, -.08, 0)),
    'Shin L':    ('Leg L',    (-.12, -.51, .015)),
    'Foot L':    ('Shin L',   (-.12, -.92, -.005)),
    'Leg R':     ('Body',     (.12, -.08, 0)),
    'Shin R':    ('Leg R',    (.12, -.51, .015)),
    'Foot R':    ('Shin R',   (.12, -.92, -.005)),
}
NAMES = list(BONES)
IDX = {n: i for i, n in enumerate(NAMES)}

# Region colours for the bare figure (the gear covers these by region): skin, cloth (shirt and sleeves), legs (trousers),
# boots, belt, hair.
SKIN, CLOTH, LEGS, BOOTS, BELT, HAIR = (.76, .6, .48), (.16, .28, .52), (.32, .2, .12), (.18, .13, .1), (.22, .16, .11), (.1, .08, .07)


# ---------------------------------------------------------------------------------------------------------- lofting
def superellipse(n, a, b, p):
    """n points round a superellipse of half-sizes a (x) and b (z), exponent p (2 ellipse, larger boxier)."""
    t = np.linspace(0, 2 * math.pi, n, endpoint=False)
    c, s = np.cos(t), np.sin(t)
    x = a * np.sign(c) * np.abs(c) ** (2 / p); z = b * np.sign(s) * np.abs(s) ** (2 / p)
    return x, z


class Part:
    """A lofted surface: vertices, triangles, per-vertex colour, and per-vertex bone weights (4 bones each)."""
    def __init__(self, name): self.name = name; self.V = []; self.F = []; self.C = []; self.W = []

    def ring_loft(self, rings, colour_of, weights_of, cap_start=True, cap_end=True):
        """rings: list of (n,3) arrays, all the same n, along the part. colour_of(i, j) and weights_of(i, j, v) per vertex.
        Where the colour changes from one ring to the next, the band is split at its middle into two coincident rings, one of
        each colour: a hard seam, as the C# figure's regions (submeshes) have."""
        n = len(rings[0])
        if len(rings) > 1:
            rr = []
            for i in range(len(rings)):
                if i and colour_of(i, 0) != colour_of(i - 1, 0):
                    mid = (np.asarray(rings[i - 1]) + np.asarray(rings[i])) / 2
                    rr += [(mid, i - 1), (mid, i)]
                rr.append((np.asarray(rings[i]), i))
            rings = [r for r, _ in rr]; src = [i for _, i in rr]
            co, wo = colour_of, weights_of
            colour_of = lambda i, j: co(src[i], j); weights_of = lambda i, j, v: wo(src[i], j, v)
        base = len(self.V)
        for i, ring in enumerate(rings):
            for j, v in enumerate(ring):
                self.V.append(v); self.C.append(colour_of(i, j)); self.W.append(weights_of(i, j, v))
        for i in range(len(rings) - 1):
            for j in range(n):
                a = base + i * n + j; b = base + i * n + (j + 1) % n; c = base + (i + 1) * n + j; d = base + (i + 1) * n + (j + 1) % n
                self.F += [(a, c, b), (b, c, d)]
        for end, use in ((0, cap_start), (len(rings) - 1, cap_end)):
            if not use: continue
            ring = np.asarray(rings[end]); centre = ring.mean(0); k = len(self.V)
            self.V.append(centre); self.C.append(colour_of(end, 0)); self.W.append(weights_of(end, 0, centre))
            for j in range(n):
                a = base + end * n + j; b = base + end * n + (j + 1) % n
                self.F.append((k, a, b) if end == 0 else (k, b, a))
        return self

    def finish(self):
        V = np.asarray(self.V, float); F = np.asarray(self.F, int)
        # wind outward: most faces' normals should point away from the part's axis through its centroid line
        a, b, c = V[F[:, 0]], V[F[:, 1]], V[F[:, 2]]
        fn = np.cross(c - a, b - a); mid = (a + b + c) / 3
        if np.sum(np.einsum('ij,ij->i', fn, mid - V.mean(0))) < 0: F = F[:, [0, 2, 1]]
        self.Vn, self.Fn = V, F
        return self


def weights(*pairs):
    """[(bone, w), ...] -> four (index, weight) pairs, normalised."""
    pairs = [(b, w) for b, w in pairs if w > 1e-4]
    tot = sum(w for _, w in pairs) or 1
    out = [(IDX[b], w / tot) for b, w in pairs][:4]
    while len(out) < 4: out.append((0, 0.0))
    return out


def blend(t, a, b, t0, t1):
    """Weights moving from bone a to bone b as t runs from t0 to t1 (smooth)."""
    u = min(1, max(0, (t - t0) / (t1 - t0))); u = u * u * (3 - 2 * u)
    return weights((a, 1 - u), (b, u))


# ---------------------------------------------------------------------------------------------------------- the body
def torso(n=28):
    # y, half-width, half-depth, centre z, squareness
    prof = [(-.14, .17, .112, -.004, 2.4), (-.1, .2, .128, -.005, 2.5), (-.02, .21, .134, 0, 2.6),
            (.03, .2, .13, .004, 2.5), (.035, .207, .136, .004, 2.5), (.095, .2, .132, .006, 2.5), (.1, .193, .126, .006, 2.5),
            (.16, .182, .122, .004, 2.4), (.25, .195, .134, .012, 2.5), (.34, .222, .152, .024, 2.6),
            (.43, .242, .158, .024, 2.7), (.5, .252, .146, .01, 2.9), (.553, .238, .124, -.006, 3.1), (.595, .172, .1, -.012, 2.6),
            (.625, .1, .078, -.012, 2.2), (.645, .066, .066, -.008, 2.0)]
    rings = []
    for y, a, b, zc, p in prof:
        x, z = superellipse(n, a, b, p)
        rings.append(np.stack([x, np.full(n, y), z + zc], 1))
    def col(i, j):
        y = prof[i][0]
        return BELT if .033 < y < .098 else (CLOTH if y >= .098 else LEGS)
    def wt(i, j, v):
        y = v[1]
        if y < .08: return blend(y, 'Body', 'Spine', -.05, .1)
        if y < .4: return blend(y, 'Spine', 'Chest', .15, .36)
        return blend(y, 'Chest', 'Neck', .6, .66)
    return Part('torso').ring_loft(rings, col, wt).finish()


def tube(name, pts, radii, n, colour, bone_of, cap=(True, True), squash=1.0):
    """A tube through pts (body space) with radii per point; bone_of(k, t) gives weights at point k (t 0..1 along)."""
    pts = np.asarray(pts, float); rings = []
    L = np.r_[0, np.cumsum(np.linalg.norm(np.diff(pts, axis=0), axis=1))]; L /= L[-1]
    for k, (c, r) in enumerate(zip(pts, radii)):
        d = pts[min(k + 1, len(pts) - 1)] - pts[max(k - 1, 0)]; d /= np.linalg.norm(d)
        ref = np.array([0, 0, 1.0]) if abs(d[2]) < .9 else np.array([1.0, 0, 0])
        u = np.cross(d, ref); u /= np.linalg.norm(u); w = np.cross(d, u)
        t = np.linspace(0, 2 * math.pi, n, endpoint=False)
        ring = c + np.outer(np.cos(t), u) * r + np.outer(np.sin(t), w) * r * squash
        rings.append(ring)
    return Part(name).ring_loft(rings, lambda i, j: colour(L[i]), lambda i, j, v: bone_of(i, L[i]), cap[0], cap[1]).finish()


def arm(side):
    s = -1 if side == 'L' else 1; A, F, H = 'Arm ' + side, 'Forearm ' + side, 'Hand ' + side
    x = .31 * s
    pts = [(x - .012 * s, .61, -.006), (x - .01 * s, .595, -.005), (x - .006 * s, .565, -.004), (x + .002 * s, .5, 0), (x + .005 * s, .42, 0),
           (x + .006 * s, .33, -.005), (x + .005 * s, .245, -.01), (x + .004 * s, .17, -.005), (x + .004 * s, .09, .002), (x + .003 * s, .02, .008),
           (x + .003 * s, -.035, .01)]
    radii = [.03, .062, .082, .08, .066, .06, .055, .062, .058, .048, .04]
    def bone(k, t):
        y = pts[k][1]
        if y > .29: return weights((A, 1))
        if y > .2: return blend(-y, A, F, -.29, -.2)
        if y > -.0: return weights((F, 1))
        return blend(-y, F, H, .0, .04)
    return tube('arm ' + side, pts, radii, 18, lambda t: CLOTH if t < .88 else SKIN, bone, cap=(False, True), squash=.9)


def hand(side):
    s = -1 if side == 'L' else 1; x = .313 * s; H = 'Hand ' + side
    # a mitten hung from the wrist: palm toward the thigh (inward), fingers together and a little curled forward
    pts = [(x, -.03, .01), (x, -.065, .013), (x, -.105, .017), (x, -.145, .024), (x, -.175, .034), (x, -.192, .042)]
    radii = [.04, .05, .054, .05, .038, .018]
    p = tube('hand ' + side, pts, radii, 14, lambda t: SKIN, lambda k, t: weights((H, 1)), squash=.55)
    # turn the flat of the hand to face the thigh: the tube's squash axis is its w axis; rotate rings about the hand axis
    return p


def thumb(side):
    s = -1 if side == 'L' else 1; x = .313 * s; H = 'Hand ' + side
    pts = [(x - .01 * s, -.055, .032), (x - .012 * s, -.09, .056), (x - .012 * s, -.118, .066)]
    return tube('thumb ' + side, pts, [.019, .018, .013], 10, lambda t: SKIN, lambda k, t: weights((H, 1)))


def leg(side):
    s = -1 if side == 'L' else 1; Lg, Sh, Ft = 'Leg ' + side, 'Shin ' + side, 'Foot ' + side
    x = .12 * s
    pts = [(x - .01 * s, .03, -.004), (x - .006 * s, -.04, 0), (x, -.12, .004), (x + .002 * s, -.25, .012), (x + .002 * s, -.38, .016), (x, -.51, .016),
           (x, -.6, .004), (x, -.7, -.006), (x, -.8, -.004), (x, -.9, -.004)]
    radii = [.098, .118, .114, .1, .086, .068, .074, .07, .056, .05]
    def bone(k, t):
        y = pts[k][1]
        if y > -.08: return weights(('Body', .55 if y > 0 else .4), (Lg, .45 if y > 0 else .6))
        if y > -.44: return weights((Lg, 1))
        if y > -.56: return blend(-y, Lg, Sh, .44, .56)
        if y > -.86: return weights((Sh, 1))
        return blend(-y, Sh, Ft, .86, .92)
    return tube('leg ' + side, pts, radii, 18, lambda t: LEGS if t < .64 else BOOTS, bone, cap=(False, True))


def foot(side):
    s = -1 if side == 'L' else 1; x = .12 * s; Ft = 'Foot ' + side
    n = 16; rings = []
    # along +z from heel to toe; a section in the x-y plane, flat at the sole (y -1)
    prof = [(-.08, .05, .054, -.952), (-.05, .057, .07, -.942), (0, .06, .076, -.938), (.06, .061, .058, -.954), (.12, .059, .045, -.966),
            (.17, .053, .036, -.972), (.205, .038, .028, -.974)]
    for z, a, h, yc in prof:
        t = np.linspace(0, 2 * math.pi, n, endpoint=False)
        xx = a * np.sign(np.cos(t)) * np.abs(np.cos(t)) ** (2 / 2.6); yy = h * np.sign(np.sin(t)) * np.abs(np.sin(t)) ** (2 / 2.6)
        yy = np.maximum(yy + yc, -1.0) - 0 * yc
        rings.append(np.stack([x + xx, yy, np.full(n, z)], 1))
    return Part('foot ' + side).ring_loft(rings, lambda i, j: BOOTS, lambda i, j, v: weights((Ft, 1))).finish()


def neck():
    return tube('neck', [(0, .6, -.012), (0, .66, -.008), (0, .72, 0)], [.06, .056, .056], 16, lambda t: SKIN,
                lambda k, t: blend(t, 'Neck', 'Head', .5, 1.0))


def head(nu=28, nv=20):
    # a deformed sphere: a little narrower than deep, a jaw that narrows to the chin, the back of the skull full
    cy, cz, r = .812, .012, .156
    rings = []
    for i in range(1, nv):
        v = math.pi * i / nv; y = math.cos(v); ring = []
        for j in range(nu):
            u = 2 * math.pi * j / nu; x = math.sin(v) * math.cos(u); z = math.sin(v) * math.sin(u)
            sx, sz = .9, 1.0
            if y < 0:                       # jaw and chin: narrower below, the chin forward
                sx *= 1 - .22 * (-y) ** 1.5
                if z > 0: z = z * (1 + .08 * (-y))
            if z < 0: sz *= 1.06            # back of the skull
            ring.append((x * r * sx, cy + y * r * 1.08, cz + z * r * sz))
        rings.append(np.asarray(ring))
    return Part('head').ring_loft(rings, lambda i, j: SKIN, lambda i, j, v: weights(('Head', 1))).finish()


def hair(nu=30, nv=22):
    """A cap over the skull: the head's shape a little larger, kept where hair grows (not the face, not the neck)."""
    cy, cz, r = .812, .012, .17
    V, F, idx = [], [], {}
    def keep(y, z): return (y > .18) or (z < -.1 and y > -.45) or (y > -.15 and z < .25 and abs(z) < 1)
    grid = []
    for i in range(0, nv):
        v = math.pi * i / nv * .78; row = []
        for j in range(nu):
            u = 2 * math.pi * j / nu; x = math.sin(v) * math.cos(u); y = math.cos(v); z = math.sin(v) * math.sin(u)
            row.append((x * r * .93, cy + .012 + y * r * 1.08, cz - .006 + z * r * 1.05, y, z))
        grid.append(row)
    p = Part('hair')
    for i in range(nv - 1):
        for j in range(nu):
            q = [grid[i][j], grid[i][(j + 1) % nu], grid[i + 1][j], grid[i + 1][(j + 1) % nu]]
            if not all(keep(t[3], t[4]) for t in q): continue
            k = len(p.V)
            for t in q: p.V.append(t[:3]); p.C.append(HAIR); p.W.append(weights(('Head', 1)))
            p.F += [(k, k + 2, k + 1), (k + 1, k + 2, k + 3)]
    return p.finish()


def features():
    """Eyes, brows, nose and ears as small ellipsoids on the head (kept from today's face, smoothed)."""
    parts = []
    def ell(name, c, r, col, nu=10, nv=8):
        p = Part(name); rings = []
        for i in range(1, nv):
            v = math.pi * i / nv
            rings.append(np.asarray([(c[0] + r[0] * math.sin(v) * math.cos(2 * math.pi * j / nu), c[1] + r[1] * math.cos(v), c[2] + r[2] * math.sin(v) * math.sin(2 * math.pi * j / nu)) for j in range(nu)]))
        return p.ring_loft(rings, lambda i, j: col, lambda i, j, v: weights(('Head', 1))).finish()
    for s in (-1, 1):
        parts.append(ell('eye', (s * .055, .838, .146), (.021, .017, .012), (.12, .1, .1)))
        parts.append(ell('brow', (s * .058, .868, .148), (.03, .009, .013), HAIR))
        parts.append(ell('ear', (s * .144, .818, .0), (.019, .04, .027), SKIN))
    parts.append(ell('nose', (0, .8, .168), (.021, .032, .024), (.72, .55, .43)))
    return parts


def build():
    parts = [torso(), neck(), head(), hair()] + features()
    for s in ('L', 'R'): parts += [arm(s), hand(s), thumb(s), leg(s), foot(s)]
    return parts


# ---------------------------------------------------------------------------------------------------------- posing
def euler(e):
    """Unity's Euler (degrees): Z, then X, then Y."""
    x, y, z = (math.radians(a) for a in e)
    Rx = np.array([[1, 0, 0], [0, math.cos(x), -math.sin(x)], [0, math.sin(x), math.cos(x)]])
    Ry = np.array([[math.cos(y), 0, math.sin(y)], [0, 1, 0], [-math.sin(y), 0, math.cos(y)]])
    Rz = np.array([[math.cos(z), -math.sin(z), 0], [math.sin(z), math.cos(z), 0], [0, 0, 1]])
    return Ry @ Rx @ Rz


def world_matrices(rot):
    """rot: bone -> Euler degrees (local). Returns bone -> 4x4 world matrix (rest translation at each bone's position)."""
    M = {}
    for n in NAMES:
        parent, pos = BONES[n]; pos = np.asarray(pos, float)
        local = np.eye(4); local[:3, :3] = euler(rot.get(n, (0, 0, 0)))
        ppos = np.asarray(BONES[parent][1], float) if parent else np.zeros(3)
        local[:3, 3] = pos - ppos
        M[n] = (M[parent] @ local) if parent else local
    return M


def pose(parts, rot):
    rest = world_matrices({}); cur = world_matrices(rot)
    skin = {n: cur[n] @ np.linalg.inv(rest[n]) for n in NAMES}
    out = []
    for p in parts:
        V = np.c_[p.Vn, np.ones(len(p.Vn))]; P = np.zeros_like(p.Vn)
        for i, ws in enumerate(p.W):
            acc = np.zeros(4)
            for b, w in ws:
                if w: acc += w * (skin[NAMES[b]] @ V[i])
            P[i] = acc[:3]
        out.append(mv.Mesh(P, p.Fn, np.asarray(p.C, float)))
    return out


def walk(phase, stride=1.0):
    """The walk with knees and elbows: legs swing from the hip, the knee bends on the swing-through and flexes a little on
    the contact, the foot keeps level; arms swing opposite with the forearm trailing; the chest counter-turns a little."""
    s = math.sin(phase); c = math.cos(phase); r = {}
    for side, k in (('L', 1), ('R', -1)):
        sw = k * s * 30 * stride                         # hip pitch (+X turns +Z down: a leg forward is negative X)
        r['Leg ' + side] = (-sw, 0, 0)
        bend = max(0, k * c) * 52 * stride + 6 * stride   # knee: most as the leg swings through
        r['Shin ' + side] = (bend, 0, 0)
        r['Foot ' + side] = (-(-sw + bend) * .55, 0, 0)  # keep the sole roughly level
        a = -k * s * 24 * stride                          # arms opposite
        r['Arm ' + side] = (-a, 0, k * 4)
        r['Forearm ' + side] = (-(12 + max(0, -k * s) * 18) * stride, 0, 0)
    r['Chest'] = (0, s * 5 * stride, 0); r['Spine'] = (3 * stride, -s * 2 * stride, 0)
    return r


if __name__ == '__main__':
    parts = build()
    tris = sum(len(p.Fn) for p in parts); verts = sum(len(p.Vn) for p in parts)
    print('parts', len(parts), 'vertices', verts, 'triangles', tris)
    rest = pose(parts, {})
    tag = sys.argv[1] if len(sys.argv) > 1 else 'v2'
    img, _ = mv.sheet(rest); img.save(os.path.join(HERE, tag + '_rest.png'))
    w = pose(parts, walk(math.pi / 2)); img, _ = mv.sheet(w); img.save(os.path.join(HERE, tag + '_walk.png'))
    print('written')
