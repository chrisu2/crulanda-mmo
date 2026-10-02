"""mesh_view.py - a small z-buffer rasterizer for previewing generated meshes (numpy + PIL only), the triangle-mesh
companion of render_prims.py (which ray-traces Unity primitives). Used to design the smooth characters (playtest note 12)
before any C#.

    import mesh_view as mv
    sheet = mv.sheet([mv.Mesh(V, F, colors)], title='...')     # side, front, three-quarter and a 25 m view
    sheet.save('out.png')

A Mesh is vertices V (n,3) in Unity's frame (left-handed, Y up, +Z forward), triangles F (m,3) wound clockwise seen from
outside (Unity's front faces), and a colour per vertex (n,3) or one colour (3,). Normals are computed per vertex from the
faces (smooth shading) unless given. Shading is a soft painted look: wrapped (half) Lambert from a warm key light, a cool
fill from the other side, ambient sky and ground terms, and a thin rim. No shadows; a grey 1.8 m post stands for scale.
"""
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont

SKY_TOP = np.array([.60, .76, .93]); SKY_LOW = np.array([.90, .94, .96]); GRASS = np.array([.36, .50, .24])


class Mesh:
    def __init__(self, V, F, color, N=None):
        self.V = np.asarray(V, float); self.F = np.asarray(F, int)
        c = np.asarray(color, float)
        self.C = np.tile(c, (len(self.V), 1)) if c.ndim == 1 else c
        self.N = normals(self.V, self.F) if N is None else np.asarray(N, float)


def normals(V, F):
    n = np.zeros_like(V)
    a, b, c = V[F[:, 0]], V[F[:, 1]], V[F[:, 2]]
    fn = np.cross(c - a, b - a)          # clockwise front faces in a left-handed frame: outward
    for k in range(3): np.add.at(n, F[:, k], fn)
    l = np.linalg.norm(n, axis=1, keepdims=True); l[l == 0] = 1
    return n / l


def merge(meshes):
    V, F, C, N, off = [], [], [], [], 0
    for m in meshes:
        V.append(m.V); F.append(m.F + off); C.append(m.C); N.append(m.N); off += len(m.V)
    return Mesh(np.vstack(V), np.vstack(F), np.vstack(C), np.vstack(N))


def camera(eye, target, up=(0, 1, 0)):
    eye = np.asarray(eye, float); f = np.asarray(target, float) - eye; f /= np.linalg.norm(f)
    r = np.cross(np.asarray(up, float), f); r /= np.linalg.norm(r); u = np.cross(f, r)
    return eye, np.stack([r, u, f])     # rows: right, up, forward (left-handed: right = up x forward)


def shade(n, col, view_f):
    key = np.array([-.45, .75, -.5]); key /= np.linalg.norm(key)          # from front-left above (toward the light)
    fill = np.array([.6, .2, .35]); fill /= np.linalg.norm(fill)
    d1 = np.clip(n @ key * .5 + .5, 0, 1) ** 1.4
    d2 = np.clip(n @ fill, 0, 1)
    sky = np.clip(n[:, 1] * .5 + .5, 0, 1)
    rim = np.clip(1 - np.abs(n @ view_f), 0, 1) ** 3
    light = .30 + .85 * d1[:, None] * np.array([1, .96, .88]) + .18 * d2[:, None] * np.array([.75, .85, 1]) + .12 * sky[:, None] + .10 * rim[:, None]
    if light.ndim == 1: light = light[:, None]
    return np.clip(col * light, 0, 1)


def render(mesh, eye, target, w, h, fov=None, ortho=None, ss=2, post=None, ground_y=-1.0):
    W, H = w * ss, h * ss
    eye, R = camera(eye, target)
    P = (mesh.V - eye) @ R.T                     # camera space: x right, y up, z forward
    if ortho:
        sx = P[:, 0] / ortho * (H / 2) + W / 2; sy = -P[:, 1] / ortho * (H / 2) + H / 2
    else:
        fpx = (H / 2) / math.tan(math.radians(fov) / 2)
        z = np.maximum(P[:, 2], 1e-3); sx = P[:, 0] / z * fpx + W / 2; sy = -P[:, 1] / z * fpx + H / 2
    # background: sky and the ground plane
    img = np.zeros((H, W, 3)); zb = np.full((H, W), np.inf)
    ys, xs = np.mgrid[0:H, 0:W]
    if ortho:
        world_y = -(ys - H / 2) / (H / 2) * ortho * R[1, 1] + eye[1]
        is_ground = world_y < ground_y
    else:
        dirs = np.stack([(xs - W / 2) / fpx, -(ys - H / 2) / fpx, np.ones_like(xs, float)], -1) @ R
        t = (ground_y - eye[1]) / np.where(dirs[..., 1] < -1e-6, dirs[..., 1], -1e-6)
        is_ground = (dirs[..., 1] < -1e-6) & (t > 0) & (t < 400)
    sky_t = np.clip(ys / H, 0, 1)[..., None]
    img[:] = SKY_TOP * (1 - sky_t) + SKY_LOW * sky_t
    img[is_ground] = GRASS * .95
    cols = shade(mesh.N, mesh.C, R[2])
    F = mesh.F; X = np.stack([sx, sy], 1)
    depth = P[:, 2]
    for f in F:
        a, b, c = X[f]; za, zb_, zc = depth[f]
        if min(za, zb_, zc) <= 0.01: continue
        x0, x1 = int(max(0, math.floor(min(a[0], b[0], c[0])))), int(min(W - 1, math.ceil(max(a[0], b[0], c[0]))))
        y0, y1 = int(max(0, math.floor(min(a[1], b[1], c[1])))), int(min(H - 1, math.ceil(max(a[1], b[1], c[1]))))
        if x1 < x0 or y1 < y0: continue
        area = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])
        if abs(area) < 1e-9: continue
        gy, gx = np.mgrid[y0:y1 + 1, x0:x1 + 1]; px, py = gx + .5, gy + .5
        w0 = ((b[0] - px) * (c[1] - py) - (b[1] - py) * (c[0] - px)) / area
        w1 = ((c[0] - px) * (a[1] - py) - (c[1] - py) * (a[0] - px)) / area
        w2 = 1 - w0 - w1
        inside = (w0 >= -1e-4) & (w1 >= -1e-4) & (w2 >= -1e-4)
        if not inside.any(): continue
        z = w0 * za + w1 * zb_ + w2 * zc
        cur = zb[y0:y1 + 1, x0:x1 + 1]
        hit = inside & (z < cur)
        if not hit.any(): continue
        cur[hit] = z[hit]
        col = w0[..., None] * cols[f[0]] + w1[..., None] * cols[f[1]] + w2[..., None] * cols[f[2]]
        img[y0:y1 + 1, x0:x1 + 1][hit] = col[hit]
    im = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8))
    return im.resize((w, h), Image.LANCZOS)


def font(size):
    for f in ('arial.ttf', 'DejaVuSans.ttf'):
        try: return ImageFont.truetype(f, size)
        except OSError: pass
    return ImageFont.load_default()


def sheet(meshes, title='', centre=(0, 0, 0), height=2.0, extra=()):
    """Side, front, back and three-quarter views of the figure(s), each 400 x 500, and any extra (label, mesh) views."""
    m = merge(meshes); cx, cy, cz = centre
    views = [('FRONT', (cx, cy, cz + 6), 'o'), ('SIDE (its right)', (cx + 6, cy, cz), 'o'), ('BACK', (cx, cy, cz - 6), 'o'),
             ('THREE-QUARTER 3.2 m, eye 1.6', (cx + 2.0, -1 + 1.6, cz + 2.5), 'p')]
    tiles = []
    for label, eye, kind in views:
        if kind == 'o': im = render(m, eye, (cx, cy, cz), 400, 500, ortho=height * .58)
        else: im = render(m, eye, (cx, cy - .1, cz), 400, 500, fov=38)
        tiles.append((label, im))
    for label, mm, eye, tgt, fov in extra:
        tiles.append((label, render(merge(mm), eye, tgt, 400, 500, fov=fov)))
    out = Image.new('RGB', (400 * len(tiles), 530), (32, 32, 36)); d = ImageDraw.Draw(out)
    for i, (label, im) in enumerate(tiles):
        out.paste(im, (i * 400, 30)); d.text((i * 400 + 8, 8), label, fill=(230, 230, 230), font=font(14))
    if title: d.text((8 + 400 * len(tiles) - 8 - 9 * len(title), 8), '', fill=(255, 255, 0))
    return out, title
