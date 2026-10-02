"""Today's models (2026-10-01, commit d6bfec7) transcribed from CritterBody.Make and ZoneBuilder.Windfall into scene
JSONs under baseline/, and rendered. Also a self-test of the euler order and the parent-scale shear."""
import json
import math
import os
import random
import sys

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import render_prims as rp

OUT = os.path.join(HERE, "baseline")
os.makedirs(OUT, exist_ok=True)


def part(kind, pos, scale, color, euler=None, children=None, quat=None):
    p = {"type": kind, "pos": [round(float(v), 5) for v in pos],
         "scale": [round(float(v), 5) for v in (scale if not isinstance(scale, (int, float)) else [scale] * 3)],
         "color": [round(float(v), 4) for v in color]}
    if euler is not None:
        p["euler"] = [float(v) for v in euler]
    if quat is not None:
        p["quat"] = [round(float(v), 6) for v in quat]
    if children:
        p["children"] = children
    return p


def leg(hip, thick, color, extra=None):
    """CritterBody.Leg: a hip pivot in body space; a slim cylinder from it down to the ground."""
    kids = []
    if thick > 0:
        kids.append(part("cylinder", (0, -hip[1] / 2, 0), (thick, hip[1] / 2, thick), color))
    return {"type": "pivot", "pos": list(hip), "children": kids + (extra or [])}


def lerp(a, b, t):
    return [a[i] + (b[i] - a[i]) * t for i in range(3)]


# ---------------------------------------------------------------- critters (CritterBody.Make)
def sheep(r=0.5):
    wool = lerp((.9, .88, .82), (.8, .77, .7), r)
    dark = (.12, .11, .1)
    parts = [part("sphere", (0, .62, 0), (.75, .6, 1), wool),
             part("sphere", (0, .7, .55), (.25, .28, .32), dark)]          # Head (no children)
    for sx in (-1, 1):
        for sz in (-1, 1):
            parts.append(leg((sx * .2, .47, sz * .28), .08, dark))
    return {"name": "sheep (today: CritterBody.Make case \"sheep\", r=%.2f)" % r, "post": True, "parts": parts}


def cat(r=0.5, white_socks=False):
    coat = (.15, .14, .13) if r < .33 else (.75, .45, .2) if r < .66 else (.55, .55, .55)
    paw = (.9, .88, .82) if white_socks else lerp(coat, (1, 1, 1), .12)
    ears = [part("cube", (s * .28, .45, 0), (.2, .3, .1), coat, euler=(0, 0, s * 15)) for s in (-1, 1)]   # children of Head
    parts = [part("capsule", (0, .26, 0), (.18, .22, .18), coat, euler=(90, 0, 0)),
             part("sphere", (0, .38, .22), .15, coat, children=ears),                                          # Head
             part("cylinder", (0, .38, -.28), (.04, .2, .04), coat, euler=(-50, 0, 0))]                       # tail
    for sx in (-1, 1):
        for sz in (-1, 1):
            parts.append(leg((sx * .055, .22, sz * .14), .05, coat,
                             [part("sphere", (0, -.202, .015), (.065, .036, .085), paw)]))
    return {"name": "cat (today: CritterBody.Make default, ginger, r=%.2f)" % r, "post": False, "parts": parts}


def deer(r=0.25):
    hide = (.5, .34, .2)
    legc = [c * .8 for c in hide]
    antlers = []
    if r < .5:                                                                                               # children of the scaled Head
        antlers = [part("cylinder", (s * .5, 1.4, -.3), (.12, .9, .12), (.7, .62, .5), euler=(-10, 0, s * 25)) for s in (-1, 1)]
    parts = [part("capsule", (0, 1, 0), (.45, .6, .45), hide, euler=(90, 0, 0))]
    for sx in (-1, 1):
        for sz in (-1, 1):
            parts.append(leg((sx * .15, .92, sz * .42), .08, legc))
    parts += [part("capsule", (0, 1.35, .55), (.18, .32, .18), hide, euler=(35, 0, 0)),                      # neck
              part("sphere", (0, 1.62, .78), (.2, .2, .32), hide, children=antlers),                         # Head
              part("sphere", (0, 1.05, -.62), .14, (.9, .88, .82))]                                          # scut
    return {"name": "deer (today: CritterBody.Make case \"deer\", antlered, r=%.2f)" % r, "post": True, "parts": parts}


# ---------------------------------------------------------------- windfall (ZoneBuilder.Windfall, flat ground)
def m2q(m):
    """3x3 rotation matrix -> quaternion [x, y, z, w]."""
    t = m[0, 0] + m[1, 1] + m[2, 2]
    if t > 0:
        s = math.sqrt(t + 1) * 2
        return [(m[2, 1] - m[1, 2]) / s, (m[0, 2] - m[2, 0]) / s, (m[1, 0] - m[0, 1]) / s, s / 4]
    i = int(np.argmax([m[0, 0], m[1, 1], m[2, 2]]))
    j, k = (i + 1) % 3, (i + 2) % 3
    s = math.sqrt(1 + m[i, i] - m[j, j] - m[k, k]) * 2
    q = [0, 0, 0, 0]
    q[i] = s / 4
    q[j] = (m[j, i] + m[i, j]) / s
    q[k] = (m[k, i] + m[i, k]) / s
    q[3] = (m[k, j] - m[j, k]) / s
    return q


def angle_axis(deg, axis):
    a = math.radians(deg)
    x, y, z = np.asarray(axis, float) / np.linalg.norm(axis)
    k = np.array([[0, -z, y], [z, 0, -x], [-y, x, 0]])
    return np.eye(3) + math.sin(a) * k + (1 - math.cos(a)) * (k @ k)


def look_rotation(fwd, up=(0, 1, 0)):
    z = np.asarray(fwd, float)
    z = z / np.linalg.norm(z)
    x = np.cross(up, z)
    x = x / np.linalg.norm(x)
    y = np.cross(z, x)
    return np.stack([x, y, z], axis=1)


def norm(v):
    v = np.asarray(v, float)
    return v / np.linalg.norm(v)


def windfall(variant=0, seed=7):
    looks = [((.34, .29, .24), (.62, .5, .34)), ((.2, .18, .17), (.6, .5, .38)), ((.42, .38, .33), (.7, .6, .45)),
             ((.15, .13, .12), (.3, .25, .2)), ((.72, .72, .68), (.8, .78, .7))]
    bark, heart = looks[variant]
    rnd = random.Random(seed)
    R = rnd.random
    G = lambda x, z: 0.0                                               # flat ground
    E = rp.euler_matrix
    up, fwd = np.array([0.0, 1, 0]), np.array([0.0, 0, 1])
    parts = []
    r0 = .27 + R() * .05
    length = 4.4 + R() * .6
    sx = -2.7
    gs = 0.0
    sh = .45 + R() * .2
    parts.append(part("cylinder", (sx, gs + sh / 2 - .1, 0), (r0 * 2.3, sh / 2 + .1, r0 * 2.3), bark))
    parts.append(part("cylinder", (sx, gs + sh - .08, 0), (r0 * 2.1, .09, r0 * 2.1), heart))
    for k in range(6):                                                  # splinters round the stump's rim
        q = E((0, k * 60 + R() * 25, 0))
        p = np.array([sx, 0, 0]) + q @ np.array([0, 0, r0 * .8])
        y = gs + sh + .02 + R() * .08
        sc = (.05 + R() * .04, .14 + R() * .22, .04)
        ex = -8 - R() * 16
        ez = (R() - .5) * 24
        rot = q @ E((ex, 0, ez))
        parts.append(part("cube", (p[0], y, p[2]), sc, heart if k % 2 == 0 else bark, quat=m2q(rot)))
    for k in range(4):                                                  # the root flare
        q = E((0, k * 90 + 45 + R() * 20, 0))
        p = np.array([sx, 0, 0]) + q @ np.array([0, 0, r0 * 1.15])
        sc = np.array([.2, .15, .46]) * (r0 / .27)
        parts.append(part("sphere", (p[0], G(p[0], p[2]) + .03, p[2]), sc, bark, quat=m2q(q @ E((14, 0, 0)))))
    full = []
    ax = sx + .4
    bx = ax + length
    r1 = r0 * .58
    a = np.array([ax, G(ax, 0) + r0 * .7, 0])
    b = np.array([bx, G(bx, 0) + r1 * .55, 0])
    lift = 0.0
    for k in range(1, 8):
        f = k / 8
        c = a + (b - a) * f
        lift = max(lift, c[1] - (r0 + (r1 - r0) * f) * .75 - G(c[0], 0))
    if lift > 0:
        a[1] -= lift
        b[1] -= lift
    d = norm(b - a)
    full.append({"type": "frustum", "a": a.round(5).tolist(), "b": b.round(5).tolist(), "r0": round(r0, 5), "r1": round(r1, 5), "color": list(bark)})
    full.append(part("cylinder", a - d * .01, (r0 * 2.6, .02, r0 * 2.6), heart, quat=m2q(rp.from_to_y(d))))
    for k in range(5):                                                  # splinters at the trunk's break
        rnd_dir = angle_axis(k * 72 + R() * 30, d) @ norm(np.cross(d, fwd))
        p = a + rnd_dir * r0 * (.3 + R() * .7) - d * .1
        sc = (.04 + R() * .03, .04, .18 + R() * .2)
        rot = look_rotation(-d + rnd_dir * (R() - .5) * .3)
        full.append(part("cube", p, sc, heart if k % 2 == 0 else bark, quat=m2q(rot)))
    for k in range(4):                                                  # stub limbs: two up, two out to the sides
        f = .3 + k * .17 + R() * .08
        frm = a + (b - a) * f
        side = np.array([0, 0, 1.0 if k % 2 == 0 else -1.0])
        u = .9 + R() * .5 if k < 2 else .2 + R() * .2
        to = frm + norm(d * (.3 + R() * .3) + side * (.35 if k < 2 else 1) + up * u) * (.55 + R() * .55)
        to[1] = max(to[1], G(to[0], to[2]) + .08)
        full.append({"type": "frustum", "a": frm.round(5).tolist(), "b": to.round(5).tolist(),
                     "r0": round(r0 * (.36 - f * .12), 5), "r1": .025, "color": list(bark)})
    parts.append({"type": "pivot", "pos": [0, 0, 0], "children": full})
    return {"name": "windfall, oak (today: ZoneBuilder.Windfall variant 0, flat ground, python seed %d)" % seed, "post": True, "parts": parts}


# ---------------------------------------------------------------- self-test
def selftest():
    E = rp.euler_matrix
    near = lambda v, w: np.allclose(v, w, atol=1e-9)
    assert near(E((0, 90, 0)) @ [0, 0, 1], [1, 0, 0])          # yaw: forward -> right
    assert near(E((90, 0, 0)) @ [0, 0, 1], [0, -1, 0])         # pitch: forward -> down
    assert near(E((0, 0, 90)) @ [1, 0, 0], [0, 1, 0])          # roll: right -> up
    assert near(E((90, 90, 0)) @ [0, 1, 0], [1, 0, 0])         # Z, X, then Y: up -> forward -> right
    assert near(E((0, 90, 90)) @ [1, 0, 0], [0, 1, 0])         # Z first: right -> up, which yaw leaves alone
    assert near(E((90, 0, 90)) @ [1, 0, 0], [0, 0, 1])         # Z then X: right -> up -> forward
    # A child cube turned 45 degrees under a parent squashed in Y: its world matrix is sheared (axes no longer square).
    scene = [{"type": "sphere", "pos": [0, 1, 0], "scale": [1, .3, 1], "color": [.8, .8, .8],
              "children": [{"type": "cube", "pos": [0, 0, 0], "euler": [0, 0, 45], "scale": [1.4, .5, .3], "color": [.8, .2, .2]}]}]
    prims = rp.flatten(scene)
    m = prims[1]["M"][:3, :3]
    x, y = m[:, 0], m[:, 1]
    assert abs(x @ y) > .05 and near(x, [1.4 * math.cos(math.pi / 4), 1.4 * .3 * math.sin(math.pi / 4), 0])
    # Ray tests: straight down onto a cylinder of scale.y = .5 at the origin hits its cap at y = .5 (half height = scale.y).
    cyl = rp.flatten([{"type": "cylinder", "pos": [0, 0, 0], "scale": [1, .5, 1], "color": [1, 1, 1]},
                      {"type": "capsule", "pos": [3, 0, 0], "scale": [1, 1, 1], "color": [1, 1, 1]},
                      {"type": "frustum", "a": [6, 0, 0], "b": [6, 2, 0], "r0": .5, "r1": .1, "color": [1, 1, 1]}])
    o = np.array([[0, 5, 0.0], [3, 5, 0], [6, 5, 0], [6.3, 1, -5], [6.31, 1, -5]])
    d = np.array([[0, -1, 0.0], [0, -1, 0], [0, -1, 0], [0, 0, 1], [0, 0, 1]])
    t, n, _, hit = rp.trace(cyl, o, d)
    assert near(t[:3], [4.5, 4.0, 3.0]) and hit[3] and not hit[4], (t, hit)   # frustum radius at half height is .3
    print("self-test passed")
    test = {"name": "self-test: sticks by euler (0,0,90) red, (90,0,0) green, (90,90,0) blue; a squashed parent shears its turned child",
            "post": True, "parts": [
                {"type": "cylinder", "pos": [-1.2, .6, 0], "euler": [0, 0, 90], "scale": [.1, .5, .1], "color": [.8, .2, .2]},
                {"type": "cylinder", "pos": [-1.2, 1.0, 0], "euler": [90, 0, 0], "scale": [.1, .5, .1], "color": [.2, .7, .2]},
                {"type": "cylinder", "pos": [-1.2, 1.4, 0], "euler": [90, 90, 0], "scale": [.1, .5, .1], "color": [.2, .3, .9]},
                {"type": "sphere", "pos": [.8, .8, 0], "scale": [1.2, .4, 1.2], "color": [.85, .85, .8],
                 "children": [{"type": "cube", "pos": [0, 0, -.6], "euler": [0, 0, 45], "scale": [.6, .6, .1], "color": [.9, .5, .1]}]},
                {"type": "cube", "pos": [.8, 1.6, -.6], "euler": [0, 0, 45], "scale": [.6, .6, .1], "color": [.9, .5, .1]}]}
    save("selftest", test, os.path.join(HERE, "selftest"))


def save(name, scene, folder=OUT):
    os.makedirs(folder, exist_ok=True)
    jp = os.path.join(folder, name + ".json")
    with open(jp, "w", encoding="utf-8") as fh:
        json.dump(scene, fh, indent=1)
    rp.sheet(scene).save(os.path.join(folder, name + ".png"))
    print("wrote", jp)


if __name__ == "__main__":
    selftest()
    save("sheep", sheep())
    save("cat", cat())
    save("deer", deer())
    save("windfall", windfall())
