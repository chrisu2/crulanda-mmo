"""render_prims.py - a small software renderer for Unity-primitive models (numpy + PIL only).

    python render_prims.py scene.json out.png

Scene JSON:
  {"name": str,
   "parts": [part, ...],
   "ground": [r,g,b]   (optional, default grass green),
   "post": bool        (optional: a 1.8 m grey post beside the model, for scale)}
part:
  {"type": "sphere"|"cube"|"cylinder"|"capsule"|"pivot",
   "pos": [x,y,z], "euler": [x,y,z] (degrees), "scale": [x,y,z] (or one number), "color": [r,g,b] 0-1,
   "children": [parts in this part's local space]}
  {"type": "frustum", "a": [x,y,z], "b": [x,y,z], "r0": .., "r1": .., "color": [r,g,b]}
      a tapered limb between two points given in its parent's space.
  Extension: "quat": [x,y,z,w] may stand in for "euler" (for code that builds a Quaternion some other way).

Unity semantics: left-handed, Y up, +Z forward; euler applied Z, then X, then Y (R = Ry * Rx * Rz); a child's matrix is
parent * T * R * S with the parent's full matrix (non-uniform scale shears rotated children, as in Unity). Sphere
diameter 1, Cube side 1, Cylinder height 2 / diameter 1, Capsule height 2 / diameter 1.

The sheet: side, front, a three-quarter view from ~4 m at eye height 1.7 m, and the same three-quarter view from 25 m at
the size it has on a 1440x900 screen with a 60 degree vertical field of view (1:1) and that crop magnified 4x.
"""
import json
import math
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

EPS = 1e-5
INF = np.inf
GRASS = (0.36, 0.50, 0.24)
SKY_TOP = np.array([0.60, 0.76, 0.93])
SKY_LOW = np.array([0.90, 0.94, 0.96])


# ---------------------------------------------------------------- transforms
def euler_matrix(e):
    """Unity's Quaternion.Euler(x, y, z): Z first, then X, then Y (column vectors: Ry @ Rx @ Rz)."""
    x, y, z = [math.radians(v) for v in e]
    cx, sx, cy, sy, cz, sz = math.cos(x), math.sin(x), math.cos(y), math.sin(y), math.cos(z), math.sin(z)
    rx = np.array([[1, 0, 0], [0, cx, -sx], [0, sx, cx]])
    ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]])
    rz = np.array([[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]])
    return ry @ rx @ rz


def quat_matrix(q):
    x, y, z, w = q
    n = math.sqrt(x * x + y * y + z * z + w * w) or 1.0
    x, y, z, w = x / n, y / n, z / n, w / n
    return np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                     [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                     [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])


def trs(pos, rot3, scale):
    m = np.eye(4)
    m[:3, :3] = rot3 * np.asarray(scale, float)[None, :]
    m[:3, 3] = pos
    return m


def from_to_y(d):
    """A rotation taking +Y onto the unit vector d."""
    d = np.asarray(d, float)
    y = np.array([0.0, 1.0, 0.0])
    c = float(d @ y)
    if c > 1 - 1e-9:
        return np.eye(3)
    if c < -1 + 1e-9:
        return np.diag([1.0, -1.0, -1.0])
    v = np.cross(y, d)
    k = np.array([[0, -v[2], v[1]], [v[2], 0, -v[0]], [-v[1], v[0], 0]])
    return np.eye(3) + k + k @ k / (1 + c)


LOCAL_BOX = {"sphere": (0.5, 0.5, 0.5), "cube": (0.5, 0.5, 0.5), "cylinder": (0.5, 1.0, 0.5), "capsule": (0.5, 1.0, 0.5)}


def flatten(parts, parent=None, out=None):
    """Scene tree -> list of {kind, M, Minv, color, params, lo, hi (world AABB)}."""
    if out is None:
        out = []
    if parent is None:
        parent = np.eye(4)
    for p in parts or []:
        kind = p.get("type", "pivot")
        color = np.asarray(p.get("color", [0.6, 0.6, 0.6]), float)[:3]
        if kind == "frustum":
            a, b = np.asarray(p["a"], float), np.asarray(p["b"], float)
            length = float(np.linalg.norm(b - a))
            if length < 1e-6:
                continue
            m = parent @ trs(a, from_to_y((b - a) / length), (1, 1, 1))
            r0, r1 = float(p["r0"]), float(p["r1"])
            rm = max(r0, r1)
            add(out, "frustum", m, color, (length, r0, r1), (-rm, 0, -rm), (rm, length, rm))
            flatten(p.get("children"), m, out)
            continue
        s = p.get("scale", [1, 1, 1])
        if isinstance(s, (int, float)):
            s = [s, s, s]
        rot = quat_matrix(p["quat"]) if "quat" in p else euler_matrix(p.get("euler", [0, 0, 0]))
        m = parent @ trs(p.get("pos", [0, 0, 0]), rot, s)
        if kind in LOCAL_BOX:
            h = LOCAL_BOX[kind]
            add(out, kind, m, color, None, (-h[0], -h[1], -h[2]), h)
        elif kind != "pivot":
            raise ValueError("unknown part type: " + str(kind))
        flatten(p.get("children"), m, out)
    return out


def add(out, kind, m, color, params, lo, hi):
    corners = np.array([[x, y, z, 1.0] for x in (lo[0], hi[0]) for y in (lo[1], hi[1]) for z in (lo[2], hi[2])])
    w = (corners @ m.T)[:, :3]
    out.append({"kind": kind, "M": m, "Minv": np.linalg.inv(m), "color": color, "params": params,
                "lo": w.min(0), "hi": w.max(0)})


# ---------------------------------------------------------------- ray / primitive tests (local space)
def quad_roots(a, b, c):
    """Roots of a t^2 + 2 b t + c = 0 (nan where none)."""
    with np.errstate(divide="ignore", invalid="ignore"):
        disc = b * b - a * c
        sq = np.sqrt(np.where(disc >= 0, disc, np.nan))
        a = np.where(np.abs(a) < 1e-12, np.nan, a)
        return (-b - sq) / a, (-b + sq) / a


def pick(cands, n):
    """Nearest valid candidate of [(t, valid, normal)]."""
    t = np.full(n, INF)
    nrm = np.zeros((n, 3))
    for tc, ok, nc in cands:
        ok = ok & np.isfinite(tc) & (tc > EPS) & (tc < t)
        t = np.where(ok, tc, t)
        nrm[ok] = nc[ok]
    return t, nrm


def hit_sphere(o, d, centre=(0, 0, 0), r=0.5):
    oc = o - np.asarray(centre, float)
    t0, _ = quad_roots((d * d).sum(1), (oc * d).sum(1), (oc * oc).sum(1) - r * r)
    return t0, np.isfinite(t0), oc + np.nan_to_num(t0)[:, None] * d


def hit_cube(o, d):
    with np.errstate(divide="ignore", invalid="ignore"):
        inv = 1.0 / np.where(np.abs(d) < 1e-12, 1e-12, d)
        t1, t2 = (-0.5 - o) * inv, (0.5 - o) * inv
    near, far = np.minimum(t1, t2), np.maximum(t1, t2)
    tn, tf = near.max(1), far.min(1)
    ax = near.argmax(1)
    nrm = np.zeros_like(o)
    idx = np.arange(len(o))
    nrm[idx, ax] = -np.sign(d[idx, ax])
    return [(tn, tf >= tn, nrm)]


def side_y(o, d, half, r=0.5):
    """An open cylinder of radius r about Y, |y| <= half."""
    a = d[:, 0] ** 2 + d[:, 2] ** 2
    b = o[:, 0] * d[:, 0] + o[:, 2] * d[:, 2]
    c = o[:, 0] ** 2 + o[:, 2] ** 2 - r * r
    out = []
    for t in quad_roots(a, b, c):
        tt = np.nan_to_num(t)
        p = o + tt[:, None] * d
        nrm = p.copy()
        nrm[:, 1] = 0
        out.append((t, np.isfinite(t) & (np.abs(p[:, 1]) <= half), nrm))
    return out


def cap_y(o, d, y, r, up):
    with np.errstate(divide="ignore", invalid="ignore"):
        t = (y - o[:, 1]) / np.where(np.abs(d[:, 1]) < 1e-12, np.nan, d[:, 1])
    p = o + np.nan_to_num(t)[:, None] * d
    nrm = np.zeros_like(o)
    nrm[:, 1] = up
    return t, np.isfinite(t) & (p[:, 0] ** 2 + p[:, 2] ** 2 <= r * r), nrm


def hit_frustum(o, d, length, r0, r1):
    k = (r1 - r0) / length
    rr = r0 + k * o[:, 1]
    a = d[:, 0] ** 2 + d[:, 2] ** 2 - (k * d[:, 1]) ** 2
    b = o[:, 0] * d[:, 0] + o[:, 2] * d[:, 2] - k * rr * d[:, 1]
    c = o[:, 0] ** 2 + o[:, 2] ** 2 - rr * rr
    out = []
    for t in quad_roots(a, b, c):
        p = o + np.nan_to_num(t)[:, None] * d
        rad = r0 + k * p[:, 1]
        nrm = p.copy()
        nrm[:, 1] = -k * rad
        out.append((t, np.isfinite(t) & (p[:, 1] >= 0) & (p[:, 1] <= length) & (rad >= 0), nrm))
    out.append(cap_y(o, d, 0.0, r0, -1.0))
    out.append(cap_y(o, d, length, r1, 1.0))
    return out


def hit_local(kind, params, o, d):
    n = len(o)
    if kind == "sphere":
        return pick([hit_sphere(o, d)], n)
    if kind == "cube":
        return pick(hit_cube(o, d), n)
    if kind == "cylinder":
        return pick(side_y(o, d, 1.0) + [cap_y(o, d, 1.0, 0.5, 1.0), cap_y(o, d, -1.0, 0.5, -1.0)], n)
    if kind == "capsule":
        return pick(side_y(o, d, 0.5) + [hit_sphere(o, d, (0, 0.5, 0)), hit_sphere(o, d, (0, -0.5, 0))], n)
    if kind == "frustum":
        return pick(hit_frustum(o, d, *params), n)
    raise ValueError(kind)


# ---------------------------------------------------------------- tracing and shading
def trace(prims, o, d):
    """Nearest hit of rays (o, d) (N x 3 each, d unit): t, world normal, colour, hit mask."""
    n = len(o)
    t = np.full(n, INF)
    nrm = np.zeros((n, 3))
    col = np.zeros((n, 3))
    for p in prims:
        centre = (p["lo"] + p["hi"]) / 2
        rad = np.linalg.norm(p["hi"] - p["lo"]) / 2 + 1e-4
        oc = centre - o
        along = (oc * d).sum(1)
        near = ((oc * oc).sum(1) - along ** 2) <= rad * rad
        if not near.any():
            continue
        idx = np.nonzero(near)[0]
        mi = p["Minv"]
        lo = o[idx] @ mi[:3, :3].T + mi[:3, 3]
        ld = d[idx] @ mi[:3, :3].T
        tl, nl = hit_local(p["kind"], p["params"], lo, ld)
        better = tl < t[idx]
        if not better.any():
            continue
        sel = idx[better]
        t[sel] = tl[better]
        wn = nl[better] @ mi[:3, :3]            # normals go by the inverse transpose
        wn /= np.maximum(np.linalg.norm(wn, axis=1, keepdims=True), 1e-12)
        nrm[sel] = wn
        col[sel] = p["color"]
    return t, nrm, col, np.isfinite(t)


def shade(prims, o, d, sky_v, light, ground, centre, ortho_fwd=None):
    """Colours for rays; sky_v is each ray's 0 (top of picture) to 1 (bottom) for the sky gradient."""
    t, nrm, col, hit = trace(prims, o, d)
    out = SKY_TOP[None, :] + (SKY_LOW - SKY_TOP)[None, :] * np.clip(sky_v * 1.6, 0, 1)[:, None]
    # the ground plane y = 0
    with np.errstate(divide="ignore", invalid="ignore"):
        tg = np.where(d[:, 1] < -1e-9, -o[:, 1] / d[:, 1], INF)
    g = (tg > 0) & (tg < t)
    if g.any():
        p = o[g] + tg[g][:, None] * d[g]
        dark = np.ones(len(p))
        for q in prims:
            c = (q["lo"] + q["hi"]) / 2
            hx = (q["hi"][0] - q["lo"][0]) / 2 * 1.15 + 0.03
            hz = (q["hi"][2] - q["lo"][2]) / 2 * 1.15 + 0.03
            s = 0.5 / (1 + 1.6 * max(0.0, q["lo"][1]))
            dark *= 1 - s * np.exp(-(((p[:, 0] - c[0]) / hx) ** 2 + ((p[:, 2] - c[2]) / hz) ** 2))
        dark = np.maximum(dark, 0.42)
        if ortho_fwd is None:                                         # perspective: haze by distance from the eye
            haze = 1 - np.exp(-tg[g] / 110.0)
        else:                                                         # orthographic: only the ground behind the model fades
            haze = 1 - np.exp(-np.maximum((p - centre) @ ortho_fwd, 0) / 30.0)
        gc = np.asarray(ground, float)[None, :] * (0.92 * dark)[:, None]
        out[g] = gc * (1 - haze)[:, None] + SKY_LOW[None, :] * haze[:, None]
    if hit.any():
        n = nrm[hit]
        v = -d[hit]
        facing = np.clip((n * v).sum(1), 0, 1)
        lam = np.clip(n @ light, 0, 1)
        amb = 0.36 + 0.12 * (n[:, 1] * 0.5 + 0.5)
        lit = (amb + 0.66 * lam) * (1 - 0.3 * (1 - facing) ** 3)     # lambert + ambient, a slightly darker rim
        out[hit] = np.clip(col[hit] * lit[:, None], 0, 1)
    return out


class Camera:
    def __init__(self, pos, fwd):
        self.pos = np.asarray(pos, float)
        f = np.asarray(fwd, float)
        self.fwd = f / np.linalg.norm(f)
        r = np.cross([0.0, 1.0, 0.0], self.fwd)                     # left-handed: right = up x forward
        self.right = r / np.linalg.norm(r)
        self.up = np.cross(self.fwd, self.right)
        h = np.array([self.fwd[0], 0.0, self.fwd[2]])
        h /= np.linalg.norm(h)
        light = -0.55 * self.right + 0.95 * np.array([0.0, 1.0, 0.0]) - 0.5 * h   # the sun: above, left, in front
        self.light = light / np.linalg.norm(light)


def render(prims, cam, w, h, ground, centre, ortho=None, persp=None, ss=2):
    """ortho = (cu, cv, half_height) in the camera's right/up plane through cam.pos;
    persp = (screen_w, screen_h, vfov_deg, x0, y0): the w x h window at (x0, y0) of that screen."""
    W, H = w * ss, h * ss
    px, py = np.meshgrid((np.arange(W) + 0.5) / ss, (np.arange(H) + 0.5) / ss)
    px, py = px.ravel(), py.ravel()
    if ortho is not None:
        cu, cv, hh = ortho
        u = cu + (px - w / 2) / (h / 2) * hh
        v = cv - (py - h / 2) / (h / 2) * hh
        o = cam.pos[None, :] + u[:, None] * cam.right[None, :] + v[:, None] * cam.up[None, :] - 60.0 * cam.fwd[None, :]
        d = np.repeat(cam.fwd[None, :], len(px), 0)
        sky_v = py / h
    else:
        sw, sh, fov, x0, y0 = persp
        f = (sh / 2) / math.tan(math.radians(fov) / 2)
        sx, sy = px + x0 - sw / 2, py + y0 - sh / 2
        d = cam.fwd[None, :] + (sx / f)[:, None] * cam.right[None, :] - (sy / f)[:, None] * cam.up[None, :]
        d /= np.linalg.norm(d, axis=1, keepdims=True)
        o = np.repeat(cam.pos[None, :], len(px), 0)
        sky_v = 1 - np.clip(d[:, 1] * 2.2 + 0.25, 0, 1)
    img = np.zeros((W * H, 3))
    step = 400000
    for i in range(0, W * H, step):
        s = slice(i, i + step)
        img[s] = shade(prims, o[s], d[s], sky_v[s], cam.light, ground, centre, cam.fwd if ortho is not None else None)
    img = img.reshape(H, W, 3).reshape(h, ss, w, ss, 3).mean((1, 3))
    return Image.fromarray((np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8))


# ---------------------------------------------------------------- the sheet
def font(size):
    for name in ("arial.ttf", "segoeui.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def corners_of(lo, hi):
    return np.array([[x, y, z] for x in (lo[0], hi[0]) for y in (lo[1], hi[1]) for z in (lo[2], hi[2])])


def ortho_view(prims, lo, hi, fwd, w, h, ground, centre):
    cam = Camera(centre, fwd)
    c = corners_of(lo, hi) - cam.pos
    u, v = c @ cam.right, c @ cam.up
    cu, cv = (u.min() + u.max()) / 2, (v.min() + v.max()) / 2
    hh = max((v.max() - v.min()) / 2, (u.max() - u.min()) / 2 * h / w) * 1.22 + 0.03
    return render(prims, cam, w, h, ground, centre, ortho=(cu, cv - hh * 0.06, hh))


def sheet(scene):
    prims = flatten(scene.get("parts", []))
    if not prims:
        raise ValueError("the scene has no primitives")
    lo = np.min([p["lo"] for p in prims], 0)
    hi = np.max([p["hi"] for p in prims], 0)
    size, tall = hi - lo, hi[1]
    if scene.get("post"):
        post = [{"type": "cylinder", "pos": [float(lo[0]) - 0.3, 0.9, float(lo[2]) - 0.2], "scale": [0.08, 0.9, 0.08],
                 "color": [0.62, 0.62, 0.64]}]
        prims += flatten(post)
        lo = np.min([p["lo"] for p in prims], 0)
        hi = np.max([p["hi"] for p in prims], 0)
    lo = np.minimum(lo, [np.inf, 0, np.inf])
    centre = (lo + hi) / 2
    ground = scene.get("ground", GRASS)
    tilt = math.tan(math.radians(6))

    PW, PH, LAB = 800, 500, 30
    side = ortho_view(prims, lo, hi, (-1, -tilt, 0), PW, PH, ground, centre)          # from +X: +Z (forward) to the right
    front = ortho_view(prims, lo, hi, (0, -tilt, -1), PW, PH, ground, centre)         # from +Z: nose to camera

    # Three-quarter: from the front right at eye height 1.7 m, about 4 m off (further only if it will not fit in 60 degrees).
    az = math.radians(35)
    horiz = np.array([math.sin(az), 0.0, math.cos(az)])
    W3 = 560
    dist = 4.0
    while True:
        dh = math.sqrt(max(dist * dist - (1.7 - centre[1]) ** 2, 0.25))
        pos = np.array([centre[0], 0, centre[2]]) + horiz * dh + [0, 1.7, 0]
        cam = Camera(pos, centre - pos)
        c = corners_of(lo, hi) - cam.pos
        z = np.maximum(c @ cam.fwd, 0.05)
        tv, tu = np.abs(c @ cam.up / z).max(), np.abs(c @ cam.right / z).max()
        need = 2 * math.degrees(math.atan(max(tv, tu * PH / W3) * 1.15))
        if need <= 60 or dist > 40:
            break
        dist *= 1.15
    fov = max(need, 6.0)
    three = render(prims, cam, W3, PH, ground, centre, persp=(W3, PH, fov, 0, 0))

    # The same view from 25 m on a 1440 x 900 screen, 60 degrees: a 1:1 strip of the screen's middle, and its heart x4.
    SW, SH, FOV, W1, CW, CH = 1440, 900, 60.0, 240, 200, 125
    pos = np.array([centre[0], 0, centre[2]]) + horiz * 25.0 + [0, 1.7, 0]
    camf = Camera(pos, centre - pos)
    far = render(prims, camf, W1, PH, ground, centre, persp=(SW, SH, FOV, (SW - W1) // 2, (SH - PH) // 2))
    bx, by = (W1 - CW) // 2, (PH - CH) // 2
    big = far.crop((bx, by, bx + CW, by + CH)).resize((CW * 4, CH * 4), Image.NEAREST)
    ImageDraw.Draw(far).rectangle((bx - 1, by - 1, bx + CW, by + CH), outline=(255, 255, 255))

    HEAD = 40
    out = Image.new("RGB", (1600, HEAD + 2 * (PH + LAB)), (28, 30, 34))
    dr = ImageDraw.Draw(out)
    dr.text((12, 8), "%s    (%.2f wide x %.2f tall x %.2f long, metres%s)" % (
        scene.get("name", "scene"), size[0], tall, size[2], "; the grey post is 1.8 m" if scene.get("post") else ""),
        fill=(240, 240, 240), font=font(20))
    f = font(15)

    def put(img, x, y, label):
        dr.text((x + 8, y + 6), label, fill=(210, 214, 220), font=f)
        out.paste(img, (x, y + LAB))

    put(side, 0, HEAD, "SIDE (orthographic, from its right; forward is to the right)")
    put(front, PW, HEAD, "FRONT (orthographic, nose to camera)")
    y2 = HEAD + PH + LAB
    put(three, 0, y2, "THREE-QUARTER  %.1f m, eye 1.7 m (zoomed: fov %.0f deg)" % (dist, fov))
    put(far, W3, y2, "25 m, 1:1 on 1440x900")
    put(big, W3 + W1, y2, "25 m, the boxed 200x125 px magnified 4x (nearest)")
    for x in (PW,):
        dr.line((x, HEAD + LAB, x, HEAD + LAB + PH), fill=(28, 30, 34))
    dr.line((W3, y2 + LAB, W3, y2 + LAB + PH), fill=(28, 30, 34), width=2)
    dr.line((W3 + W1, y2 + LAB, W3 + W1, y2 + LAB + PH), fill=(28, 30, 34), width=2)
    return out


def main(argv):
    if len(argv) != 3:
        print("usage: render_prims.py scene.json out.png")
        return 2
    with open(argv[1], "r", encoding="utf-8") as fh:
        scene = json.load(fh)
    sheet(scene).save(argv[2])
    print("wrote", argv[2])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
