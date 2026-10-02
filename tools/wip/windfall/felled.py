"""Design 3: the felled tree (playtest note 6, Chris: "need to look chopped down"). An axe-cut stump with a pale face, the
hinge's few torn fibres and chips round it; the trunk limbed (flush stubs with pale ends, no sticks) and bucked in two, each
cut end pale; the lopped crown as a low brush heap beside it. Stays: stump, chips, brush. Full (cut away): the two logs.
Extent along X: the stump's back at -2.9 to the top log's end at about +2.4 (WindfallYaw's -2.9..+2.5).
The C# (ZoneBuilder.Nodes.cs Windfall) is the one that counts; this drew its previews (python felled.py: PNG sheets here).
The frustums here are straight: in the game the branches bow a little (Limb) and the twigs start on the bowed line (LimbAt)."""
import json, math, os, random, sys
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(os.path.dirname(HERE), 'render'))   # tools/wip/render
import render_prims as rp
from gen_baselines import part, m2q, look_rotation, norm

UP = np.array([0.0, 1, 0])
LOOKS = [((.34, .29, .24), (.62, .5, .34)), ((.2, .18, .17), (.6, .5, .38)), ((.42, .38, .33), (.7, .6, .45)),
         ((.15, .13, .12), (.3, .25, .2)), ((.72, .72, .68), (.8, .78, .7))]
LEAF = [(.46, .3, .13), (.17, .24, .13), (.24, .3, .16), None, (.6, .64, .55)]
NAMES = ["oak", "black pine", "stone-pine", "ash-snag", "ghost-oak"]


def v3(x, y, z): return np.array([x, y, z], float)
def fr(a, b, r0, r1, col):
    return {"type": "frustum", "a": [round(float(v), 4) for v in a], "b": [round(float(v), 4) for v in b], "r0": round(float(r0), 4), "r1": round(float(r1), 4), "color": list(col)}
def disc(dst, centre, axis, r, thick, col):
    """A flat round face (Unity cylinder) square to axis."""
    dst.append(part("cylinder", centre, (r * 2, thick / 2, r * 2), col, quat=m2q(look_rotation(perp(axis), axis))))
def perp(a):
    a = norm(a); p = np.cross(a, UP)
    return norm(p) if np.linalg.norm(p) > 1e-3 else v3(1, 0, 0)
def box(dst, centre, size, col, fwd, up=UP):
    dst.append(part("cube", centre, size, col, quat=m2q(look_rotation(norm(fwd), up))))


def build(variant, seed, G=lambda x, z: 0.0, worked=False):
    bark, wood = LOOKS[variant]; leaf = LEAF[variant]
    heart = tuple(w * .82 for w in wood); pale = tuple(w + (1 - w) * .18 for w in wood)
    rnd = random.Random(seed); R = rnd.random
    stay, full = [], []
    r0 = .29 + R() * .05; sx = -2.6; rs = r0 * 1.06
    # ---- the stump: a short bole on a root flare, cut low and nearly level
    gs = min(G(sx, 0), G(sx - rs, 0), G(sx + rs, 0), G(sx, -rs), G(sx, rs)); hs = .38 + R() * .16
    top = v3(sx, gs + hs, 0)
    stay.append(fr((sx, gs - .3, 0), top, rs * 1.32, rs, bark))
    for k in range(5):
        an = math.radians(k * 72 + R() * 30); o = v3(math.cos(an), 0, math.sin(an)); p = v3(sx, 0, 0) + o * rs * 1.1
        stay.append(part("sphere", (p[0], G(p[0], p[2]) + .04, p[2]), (rs * .55, rs * .45, rs * 1.3), bark, quat=m2q(look_rotation(o) @ rp.euler_matrix((14, 0, 0)))))
    # the cut: a pale face tipped a little toward the fall (+X), the heartwood darker, the pith
    tilt = math.radians(4 + R() * 4); n = norm(v3(math.sin(tilt), math.cos(tilt), 0))
    disc(stay, top - UP * .02, n, rs * .97, .05, wood)
    disc(stay, top + n * .006 - UP * .02, n, rs * .55, .05, heart)
    stay.append(part("sphere", top + n * .012 - UP * .005, (.05, .02, .05), tuple(c * .6 for c in heart)))
    # the hinge: a short ridge of torn fibres across the face, two thirds of the way to the fall side
    for k in range(5):
        zz = (k - 2) / 2.2 * rs * .75 + (R() - .5) * .05; xx = sx + rs * .28 + (R() - .5) * .04
        h = .05 + R() * .07
        box(stay, (xx, top[1] + h / 2 - .01, zz), (.03 + R() * .02, h, .02 + R() * .015), pale if k % 2 else wood, v3(1, 0, 0), norm(v3(.35 + R() * .3, 1, (R() - .5) * .3)))
    # chips: the axe's, thrown mostly toward the fall side, flat on the ground
    for k in range(13):
        an = (R() - .5) * math.pi * 1.5 + (0 if k % 3 else math.pi); d = rs + .2 + R() * 1.1
        x, z = max(sx - .25, sx + math.cos(an) * d), math.sin(an) * d
        box(stay, (x, G(x, z) + .008, z), (.06 + R() * .07, .014, .03 + R() * .04), bark if k % 4 == 0 else wood if k % 2 else pale, v3(math.cos(R() * 6.3), (R() - .5) * .25, math.sin(R() * 6.3)))
    # ---- the logs: the trunk limbed and bucked in two, lying where it fell, each cut end pale
    ax = sx + rs + .25; len_ = 4.2 + R() * .3; cut = .58 + R() * .08; gap = .14 + R() * .08
    r1 = r0 * .62; rc = r0 + (r1 - r0) * cut
    a = v3(ax, G(ax, 0) + r0 * .86, 0); bx = ax + len_ * cut; b = v3(bx, G(bx, 0) + rc * .86, 0)
    roll = (R() - .5) * .5; yaw = math.radians((R() - .5) * 14)
    c = v3(bx + gap, G(bx + gap, roll) + rc * .86, roll); dl = len_ * (1 - cut) - gap
    d = c + v3(math.cos(yaw), 0, math.sin(yaw)) * dl; d[1] = G(d[0], d[2]) + r1 * .86
    full.append(fr(a, b, r0, rc, bark)); full.append(fr(c, d, rc * .97, r1, bark))
    for p, q, r in ((a, b, r0), (b, a, rc), (c, d, rc * .97), (d, c, r1)):
        ax_ = norm(p - q); disc(full, p + ax_ * .01, ax_, r * .97, .03, wood); disc(full, p + ax_ * .02, ax_, r * .55, .03, heart)
    # the hinge's fibres on the butt, pointing back at the stump
    for k in range(3):
        o = norm(v3(0, R() - .3, R() - .5)); box(full, a + o * r0 * (.3 + R() * .4) - v3(.04, 0, 0), (.025, .025, .1 + R() * .08), pale, v3(-1, (R() - .5) * .3, (R() - .5) * .3))
    # lopped limbs: short stubs flush with the bark, a pale cut on each (never a stick)
    for log, (p, q, rp_, rq) in enumerate(((a, b, r0, rc), (c, d, rc, r1))):
        for k in range(3 if log == 0 else 2):
            f = .25 + k * .25 + R() * .12; ctr = p + (q - p) * f; rr = rp_ + (rq - rp_) * f
            an = math.radians(-60 + R() * 240); o = norm(v3(0, math.sin(an), math.cos(an)) + norm(q - p) * .35)
            base = ctr + o * rr * .7; tip = ctr + o * (rr + .06 + R() * .07); st = .04 + R() * .03
            full.append(fr(base, tip, st * 1.25, st, bark)); disc(full, tip, o, st * .95, .02, pale)
    # ---- the lopped crown, heaped low beside the top log and past the cut
    side = 1 if R() < .5 else -1
    for k in range(7):
        x0 = .2 + R() * 1.6; z0 = side * (rc + .25 + R() * .55) + roll
        an = math.radians((R() - .5) * 70 + (180 if k % 3 == 0 else 0)); L = .7 + R() * .8
        p = v3(x0, G(x0, z0) + .05, z0); q = p + v3(math.cos(an), 0, math.sin(an)) * L; q[1] = G(q[0], q[2]) + .03 + R() * .1
        q[0] = min(q[0], 2.45); q[1] += R() * .12 if k % 2 else 0
        br = .035 + R() * .03
        stay.append(fr(p, q, br, .012, bark))
        for j in range(3):
            m = p + (q - p) * (.3 + j * .22); t2 = m + norm(q - p + v3(0, .1 + R() * .25, (R() - .5) * 1.8)) * (.25 + R() * .35)
            t2[0] = min(t2[0], 2.45); t2[1] = max(t2[1], G(t2[0], t2[2]) + .02)
            stay.append(fr(m, t2, br * .6, .008, bark))
    parts = stay + ([] if worked else full)
    xs = [p["pos"][0] for p in parts if "pos" in p] + [v for p in parts if "a" in p for v in (p["a"][0], p["b"][0])]
    return {"name": "%s felled%s (seed %d)" % (NAMES[variant], ", WORKED" if worked else "", seed), "post": True, "parts": parts}, (min(xs), max(xs))


if __name__ == "__main__":
    out = []
    for variant, seed, worked in ((0, 3, False), (0, 3, True), (1, 11, False), (4, 8, False)):
        sc, ext = build(variant, seed, worked=worked); tag = "v2_%s%s" % (NAMES[variant].replace(" ", "").replace("-", ""), "_worked" if worked else "")
        json.dump(sc, open(os.path.join(HERE, tag + ".json"), "w")); im = rp.sheet(sc); im.save(os.path.join(HERE, tag + ".png")); out.append(tag)
        print(tag, "extent x %.2f .. %.2f" % ext)
