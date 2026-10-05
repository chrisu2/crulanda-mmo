#!/usr/bin/env python
# tools/art/make_icons.py : paints every HUD icon for Crulanda (GAME-ONLY imagery) and writes them, with a .meta each, under
# Assets/Crulanda/Resources/Icons. Python with numpy and PIL only.
#   python tools/art/make_icons.py                 write the icons (existing .meta files are kept, so GUIDs never change)
#   python tools/art/make_icons.py --check         list ids without an icon (exit code 1 if any)
#   python tools/art/make_icons.py --sheets DIR    also write preview sheets (96 px and the HUD's true 38 px)
# Layout (Resources paths, dots in ids written as '-'): Icons/item/<item id>, Icons/quest/<quest item id>, Icons/ability/<ability id>,
# Icons/talent/<talent id>, Icons/trade/<profession id>, Icons/gear/<family>__<palette> (generated gear), Icons/kind/<kind>, Icons/slot/<slot>.

# ====================================================================================================
# icon_engine.py
# ====================================================================================================
# Crulanda icon painter: the engine (GAME-ONLY imagery; numpy and PIL only).
# Each icon is a bold single object on a soft dark vignette. Shapes are drawn in a 0..100 unit square (y down) at a 256 px
# master, as layered parts; each part is shaded by a hand-painted pass (light from the upper left, a darker rim, banded
# tones, a highlight), inked with a dark line, and the whole object gets a dark outline and a drop shadow.
import math, zlib
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageChops

S = 256
U = S / 100.0
OUT = 64                      # the size written for the game
INK = np.array([.07, .05, .045])

def col(c):
    if isinstance(c, str):
        h = c.lstrip('#'); return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255
    return np.array(c, np.float32)
def mix(a, b, t): return col(a) * (1 - t) + col(b) * t
def lite(c, t=.3): return mix(c, '#fff6e0', t)
def dark(c, t=.3): return mix(c, '#0c0a12', t)
def vivid(c, k=1.3, lift=0.0):
    """Push a muted palette colour toward something that reads on a dark tile."""
    c = col(c); g = c.mean(); c = g + (c - g) * k; return np.clip(c + lift, 0, 1)

MATS = {  # bevel radius (px at 256), light gain, specular, band mix, noise
    'matte': (9, 1.0, .0, .55, .09),
    'soft':  (15, 1.0, .0, .45, .10),
    'metal': (5, 1.25, .75, .6, .05),
    'gem':   (7, 1.2, .9, .5, .03),
    'wood':  (8, 1.0, .0, .55, .12),
    'flatish': (20, .6, .0, .3, .06),
}

def _smooth(pts, closed=True, n=10):
    p = np.array(pts, np.float32); k = len(p); out = []
    rng = range(k) if closed else range(k - 1)
    for i in rng:
        if closed: p0, p1, p2, p3 = p[(i - 1) % k], p[i], p[(i + 1) % k], p[(i + 2) % k]
        else: p0, p1, p2, p3 = p[max(i - 1, 0)], p[i], p[i + 1], p[min(i + 2, k - 1)]
        for j in range(n):
            t = j / n; t2 = t * t; t3 = t2 * t
            out.append(.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    if not closed: out.append(p[-1])
    return [tuple(x) for x in out]

class Icon:
    def __init__(self, name, bg='#3a3f47'):
        self.name = name
        self.rng = np.random.RandomState(zlib.crc32(name.encode()) & 0x7fffffff)
        self.obj = np.zeros((S, S, 4), np.float32)       # premultiplied
        self.halo = np.zeros((S, S, 3), np.float32)      # additive light behind and over the object
        self.over = np.zeros((S, S, 3), np.float32)      # additive light over the object (sparks)
        self.bg = col(bg)
        self.M = [np.eye(3, dtype=np.float32)]
        n = self.rng.rand(9, 9).astype(np.float32)
        self.noise = np.asarray(Image.fromarray((n * 255).astype(np.uint8)).resize((S, S), Image.BICUBIC), np.float32) / 255 - .5
        n2 = self.rng.rand(40, 40).astype(np.float32)
        self.grain = np.asarray(Image.fromarray((n2 * 255).astype(np.uint8)).resize((S, S), Image.BICUBIC), np.float32) / 255 - .5

    # ---- transform ----------------------------------------------------------------------
    def push(self, rot=0, sc=1, dx=0, dy=0, cx=50, cy=50, flip=False, sx=None, sy=None):
        a = math.radians(rot); c, s = math.cos(a), math.sin(a)
        sx = sc if sx is None else sx; sy = sc if sy is None else sy
        if flip: sx = -sx
        T1 = np.array([[1, 0, -cx], [0, 1, -cy], [0, 0, 1]], np.float32)
        R = np.array([[c * sx, -s * sy, 0], [s * sx, c * sy, 0], [0, 0, 1]], np.float32)
        T2 = np.array([[1, 0, cx + dx], [0, 1, cy + dy], [0, 0, 1]], np.float32)
        self.M.append(self.M[-1] @ T2 @ R @ T1); return self
    def pop(self): self.M.pop(); return self
    def _tf(self, pts):
        p = np.array(pts, np.float32).reshape(-1, 2); m = self.M[-1]
        q = p @ m[:2, :2].T + m[:2, 2]
        return [(float(x) * U, float(y) * U) for x, y in q]
    def _sc(self): m = self.M[-1]; return math.sqrt(abs(m[0, 0] * m[1, 1] - m[0, 1] * m[1, 0]))

    # ---- masks ---------------------------------------------------------------------------
    def new(self): return Image.new('L', (S, S), 0)
    def poly(self, pts, smooth=False, n=10):
        if smooth: pts = _smooth(pts, True, n)
        m = self.new(); ImageDraw.Draw(m).polygon(self._tf(pts), fill=255); return m
    def ell(self, cx, cy, rx, ry=None, rot=0, a0=0, a1=360):
        ry = rx if ry is None else ry; r = math.radians(rot); pts = []
        steps = 56
        for i in range(steps + 1):
            t = math.radians(a0 + (a1 - a0) * i / steps); x, y = rx * math.cos(t), ry * math.sin(t)
            pts.append((cx + x * math.cos(r) - y * math.sin(r), cy + x * math.sin(r) + y * math.cos(r)))
        if a1 - a0 < 360: pts.append((cx, cy))
        return self.poly(pts)
    def rect(self, x0, y0, x1, y1, r=0):
        if r <= 0: return self.poly([(x0, y0), (x1, y0), (x1, y1), (x0, y1)])
        pts = []
        for cx, cy, a in ((x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180)):
            for i in range(7):
                t = math.radians(a + 90 * i / 6); pts.append((cx + r * math.cos(t), cy + r * math.sin(t)))
        return self.poly(pts)
    def line(self, pts, w, smooth=False, closed=False):
        if smooth: pts = _smooth(pts, closed, 10)
        elif closed: pts = list(pts) + [pts[0]]
        q = self._tf(pts); ww = max(1, w * U * self._sc()); m = self.new(); d = ImageDraw.Draw(m)
        d.line(q, fill=255, width=int(round(ww)), joint='curve'); r = ww / 2
        for x, y in q: d.ellipse((x - r, y - r, x + r, y + r), fill=255)
        return m
    def taper(self, pts, w0, w1, smooth=True, wmid=None):
        """A stroke whose width runs from w0 to w1 (through wmid at the middle if given): fangs, horns, leaves, tails."""
        if smooth: pts = _smooth(pts, False, 10)
        p = np.array(pts, np.float32); n = len(p); L, R = [], []
        for i in range(n):
            a = p[max(i - 1, 0)]; b = p[min(i + 1, n - 1)]; d = b - a; l = math.hypot(*d) or 1; nx, ny = -d[1] / l, d[0] / l
            t = i / (n - 1)
            if wmid is None: w = w0 + (w1 - w0) * t
            else: w = (w0 + (wmid - w0) * t * 2) if t < .5 else (wmid + (w1 - wmid) * (t - .5) * 2)
            L.append((p[i][0] + nx * w / 2, p[i][1] + ny * w / 2)); R.append((p[i][0] - nx * w / 2, p[i][1] - ny * w / 2))
        m = self.poly(L + R[::-1])
        if w0 > 1: m = ImageChops.lighter(m, self.ell(p[0][0], p[0][1], w0 / 2))
        if w1 > 1: m = ImageChops.lighter(m, self.ell(p[-1][0], p[-1][1], w1 / 2))
        return m
    def blob(self, cx, cy, rx, ry=None, n=9, jit=.18, rot=0, smooth=False):
        ry = rx if ry is None else ry; pts = []
        for i in range(n):
            a = math.radians(rot) + 2 * math.pi * i / n; k = 1 + (self.rng.rand() - .5) * 2 * jit
            pts.append((cx + rx * k * math.cos(a), cy + ry * k * math.sin(a)))
        return self.poly(pts, smooth=smooth)
    def star(self, cx, cy, r0, r1, n=5, rot=-90):
        pts = []
        for i in range(n * 2):
            a = math.radians(rot + 180 * i / n); r = r0 if i % 2 == 0 else r1
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
        return self.poly(pts)
    @staticmethod
    def U_(*ms):
        out = ms[0]
        for m in ms[1:]: out = ImageChops.lighter(out, m)
        return out
    @staticmethod
    def sub(a, b): return ImageChops.subtract(a, b)
    @staticmethod
    def AND(a, b): return ImageChops.darker(a, b)
    @staticmethod
    def grow(m, px):
        if px > 0: return m.filter(ImageFilter.MaxFilter(int(px) * 2 + 1))
        if px < 0: return m.filter(ImageFilter.MinFilter(int(-px) * 2 + 1))
        return m

    # ---- paint ---------------------------------------------------------------------------
    def _over(self, rgb, a):
        a = a[..., None]
        self.obj[..., :3] = rgb * a + self.obj[..., :3] * (1 - a)
        self.obj[..., 3:] = a + self.obj[..., 3:] * (1 - a)
    def part(self, mask, color, mat='matte', ink=True, alpha=1.0, clip=None, grain=0.0, light=1.0):
        """One shaded piece of the object. grain: streaks along the y axis of the noise (wood, fur)."""
        if clip is not None: mask = ImageChops.multiply(mask, clip)
        base = col(color); bev, k, spec, band, nz = MATS[mat]
        m = np.asarray(mask.filter(ImageFilter.GaussianBlur(.8)), np.float32) / 255
        if ink:
            im = np.asarray(self.grow(mask, 2).filter(ImageFilter.GaussianBlur(.8)), np.float32) / 255
            self._over(dark(base, .82)[None, None, :] * np.ones((S, S, 1), np.float32), im * .92 * alpha)
        h = np.asarray(mask.filter(ImageFilter.GaussianBlur(bev)), np.float32) / 255
        gy, gx = np.gradient(h)
        lit = (gx * .62 + gy * .78) * bev * k * 1.25 * light
        yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
        lit += (1 - (xx + yy) / S) * .2
        lit -= np.clip((1 - h) * 2, 0, 1) * .10
        lit += self.noise * nz * 2 + self.grain * grain
        t = np.clip(.5 + lit, 0, 1)
        t = t * (1 - band) + (np.round(t * 4) / 4) * band
        sh = base * np.array([.36, .33, .46], np.float32)            # cool, deep shadow
        hi = np.clip(base * 1.18 + np.array([.26, .23, .15], np.float32), 0, 1)   # warm light
        t3 = t[..., None]
        rgb = np.where(t3 < .5, sh + (base - sh) * (t3 * 2), base + (hi - base) * ((t3 - .5) * 2))
        if spec > 0:
            sp = np.clip((t - .8) / .2, 0, 1) ** 2 * spec
            rgb = rgb + (1 - rgb) * sp[..., None]
        self._over(rgb.astype(np.float32), m * alpha)
        return mask
    def flat(self, mask, color, alpha=1.0, clip=None, blur=0):
        if clip is not None: mask = ImageChops.multiply(mask, clip)
        m = np.asarray(mask.filter(ImageFilter.GaussianBlur(max(.8, blur))), np.float32) / 255
        self._over(col(color)[None, None, :] * np.ones((S, S, 1), np.float32), m * alpha)
        return mask
    def back(self, mask, color, power=.6, blur=3):
        """Light painted on the tile behind the object (rays, streaks, rings): no ink, no outline."""
        m = np.asarray(mask.filter(ImageFilter.GaussianBlur(blur)), np.float32) / 255
        self.halo += col(color)[None, None, :] * m[..., None] * power
        return mask
    def glow(self, mask, color, core=None, halo=10, power=1.0, clip=None):
        """An emissive piece: a bright core with a soft halo that spills over the object and the tile."""
        if clip is not None: mask = ImageChops.multiply(mask, clip)
        c = col(color); core = lite(c, .6) if core is None else core
        m = np.asarray(mask.filter(ImageFilter.GaussianBlur(1)), np.float32) / 255
        inner = np.asarray(self.grow(mask, -3).filter(ImageFilter.GaussianBlur(4)), np.float32) / 255 * .85
        rgb = c[None, None, :] * (1 - inner[..., None]) + col(core)[None, None, :] * inner[..., None]
        self._over(rgb, m)
        hm = np.asarray(mask.filter(ImageFilter.GaussianBlur(halo)), np.float32) / 255
        self.halo += c[None, None, :] * hm[..., None] * power
        return mask
    def spark(self, x, y, r, color='#ffffff', power=1.0):
        """A small four-point glint (drawn over everything)."""
        m = self.U_(self.poly([(x - r, y), (x, y - r * .22), (x + r, y), (x, y + r * .22)]), self.poly([(x, y - r), (x + r * .22, y), (x, y + r), (x - r * .22, y)]))
        a = np.asarray(m.filter(ImageFilter.GaussianBlur(1.2)), np.float32) / 255
        b = np.asarray(m.filter(ImageFilter.GaussianBlur(6)), np.float32) / 255
        self.over += (col('#ffffff')[None, None, :] * a[..., None] + col(color)[None, None, :] * b[..., None] * .9) * power
    def shine(self, mask, alpha=.5, clip=None): return self.flat(mask, '#fffbe8', alpha, clip, blur=1.5)

    # ---- finish --------------------------------------------------------------------------
    def render(self, size=OUT, tile=True):
        yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
        a = self.obj[..., 3]
        amask = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
        outline = np.asarray(self.grow(amask, 6).filter(ImageFilter.GaussianBlur(1.2)), np.float32) / 255
        shadow = np.asarray(ImageChops.offset(self.grow(amask, 4), 7, 9).filter(ImageFilter.GaussianBlur(7)), np.float32) / 255
        back = np.asarray(amask.filter(ImageFilter.GaussianBlur(26)), np.float32) / 255
        if tile:
            d = np.hypot((xx - S * .42) / S, (yy - S * .40) / S)
            v = np.clip(1.08 - d * 1.45, .12, 1)
            bg = self.bg[None, None, :] * v[..., None]
            bg = bg * (1 + self.noise[..., None] * .25)
            bg = bg + lite(self.bg, .35)[None, None, :] * (back[..., None] * .42)          # a soft light behind the object
            e = np.minimum(np.minimum(xx, yy), np.minimum(S - 1 - xx, S - 1 - yy))          # darker toward the tile's edge
            bg = bg * np.clip(.45 + e / 22, 0, 1)[..., None]
            bg = bg * (1 - shadow[..., None] * .55)
            bg = bg + self.halo * .9
            rgb = bg * (1 - outline[..., None]) + INK[None, None, :] * outline[..., None]
            rgb = self.obj[..., :3] + rgb * (1 - a[..., None])
            rgb = rgb + self.halo * a[..., None] * .35 + self.over
            out = np.dstack([np.clip(rgb, 0, 1), np.ones((S, S), np.float32)])
        else:
            oa = np.maximum(a, outline)
            rgb = self.obj[..., :3] + INK[None, None, :] * (oa - a)[..., None]
            rgb = np.where(oa[..., None] > 0, rgb / np.maximum(oa[..., None], 1e-4), 0) + self.over
            out = np.dstack([np.clip(rgb, 0, 1), oa])
        im = Image.fromarray((out * 255 + .5).astype(np.uint8), 'RGBA')
        if size != S:
            im = im.resize((size, size), Image.LANCZOS)
            im = im.filter(ImageFilter.UnsharpMask(radius=1.0, percent=45, threshold=2))
        return im

# ====================================================================================================
# lib1.py
# ====================================================================================================
# Drawers, batch 1: materials, food, potions, tools, bags, teeth.

BG = dict(ore='#3d4756', wood='#4b3b2a', herb='#2c4433', hide='#4a3a30', meat='#4f3030', food='#55402a', potion='#35304d',
          tool='#3c4048', bag='#43382c', junk='#3b3b3e', quest='#57492a', gear='#363b45', neck='#33303f')

def ore(c, rock, vein, hot=None):
    body = c.poly([(14, 62), (22, 38), (40, 22), (64, 20), (84, 38), (88, 62), (72, 82), (36, 84)])
    c.part(body, rock, 'matte')
    c.part(c.poly([(22, 38), (40, 22), (64, 20), (58, 44), (34, 52)]), lite(rock, .18), 'matte', clip=body)
    c.part(c.poly([(58, 44), (64, 20), (84, 38), (88, 62), (66, 62)]), dark(rock, .22), 'matte', clip=body)
    c.part(c.poly([(34, 52), (58, 44), (66, 62), (72, 82), (36, 84), (28, 66)]), rock, 'matte', clip=body)
    for x, y, r in ((40, 36, 9), (67, 47, 8), (47, 66, 10), (26, 58, 6), (72, 70, 5)):
        nug = c.blob(x, y, r, r * .8, n=6, jit=.3, rot=c.rng.rand() * 60)
        if hot: c.glow(nug, hot, core=lite(hot, .6), halo=7, power=.7, clip=body)
        else:
            c.part(nug, vein, 'metal', clip=body)
            c.shine(c.ell(x - r * .25, y - r * .3, r * .28), .7, clip=body)

def bar(c, metal, mark=True):
    top = [(22, 40), (66, 26), (86, 38), (42, 54)]; front = [(42, 54), (86, 38), (90, 58), (40, 78)]; end = [(22, 40), (42, 54), (40, 78), (14, 58)]
    c.part(c.poly(front), dark(metal, .28), 'metal', light=.5)
    c.part(c.poly(end), dark(metal, .05), 'metal', light=.5)
    c.part(c.poly(top), lite(metal, .22), 'metal', light=.6)
    c.shine(c.line([(26, 40.5), (66, 28)], 1.6), .75)
    if mark: c.flat(c.line([(50, 38), (60, 35), (64, 40), (54, 43), (50, 38)], 1.3), dark(metal, .55), .8)

def log(c, bark, wood, ring=None, glowc=None, char=False):
    c.push(rot=-28)
    body = c.rect(10, 33, 82, 67, 5)
    c.part(body, bark, 'wood', grain=.25)
    for y in (40, 47, 55, 61):
        c.flat(c.line([(12 + c.rng.rand() * 8, y), (40, y + c.rng.rand() * 2 - 1), (74, y)], 1.4), dark(bark, .55), .6, clip=body)
    c.part(c.taper([(38, 36), (42, 26), (50, 20)], 9, 6, smooth=False), bark, 'wood')            # a branch stub
    c.part(c.ell(50, 20, 3.6, 2.6, rot=-30), wood, 'matte')
    end = c.ell(82, 50, 9, 17)
    c.part(end, wood, 'matte', light=.5)
    for k in (.72, .45, .2):
        c.flat(c.sub(c.ell(82, 50, 9 * k, 17 * k), c.ell(82, 50, 9 * k - 1.3, 17 * k - 1.3)), ring or dark(wood, .4), .75)
    if glowc: c.glow(c.ell(82, 50, 3, 6), glowc, halo=9, power=.8)
    if char:
        for x in (22, 40, 60): c.glow(c.line([(x, 44), (x + 5, 50), (x + 2, 57)], 1.4), '#ff6a1f', halo=4, power=.5, clip=body)
    c.pop()

def herb_yarrow(c, flower='#f4f1e4', leaf='#5f9a48'):
    for x0, x1, y1 in ((48, 24, 34), (50, 50, 22), (52, 76, 34)):
        c.part(c.taper([(50, 94), (x0, 62), (x1, y1 + 6)], 4.5, 2.5), dark(leaf, .15), 'matte')
    for sx in (-1, 1):
        for y in (60, 70, 80):
            c.part(c.taper([(50, y + 8), (50 + sx * 12, y), (50 + sx * 24, y - 2)], 5, 0.5), leaf, 'matte')
    for cx, cy in ((24, 32), (50, 20), (76, 32)):
        c.part(c.ell(cx, cy + 3, 15, 8), dark(flower, .35), 'matte')
        for dx, dy in ((-9, 2), (-3, -3), (4, -3), (10, 2), (-5, 4), (3, 4), (0, 0)):
            c.part(c.ell(cx + dx, cy + dy, 4.6), flower, 'soft', ink=False)
            c.flat(c.ell(cx + dx, cy + dy, 1.2), '#e6c24a')

def herb_cap(c, cap='#2b2630', gill='#6a5a78', stem='#d9d2c0', glowc=None, spots=None):
    c.part(c.taper([(52, 92), (48, 70), (50, 46)], 15, 10), stem, 'soft')
    c.part(c.ell(50, 90, 16, 5), '#4a6a3a', 'matte')
    under = c.ell(50, 46, 34, 9)
    c.part(under, gill, 'matte')
    for i in range(-5, 6): c.flat(c.line([(50 + i * 1.5, 44), (50 + i * 6.2, 52)], 1.1), dark(gill, .6), .8, clip=under)
    top = c.AND(c.ell(50, 46, 36, 34), c.rect(0, 0, 100, 46))
    if glowc: c.glow(top, glowc, core=lite(glowc, .5), halo=14, power=.6)
    c.part(top, cap, 'soft')
    c.shine(c.ell(36, 26, 9, 4.5, rot=-25), .45)
    if spots:
        for x, y, r in ((58, 24, 4), (44, 36, 3), (68, 36, 3.2), (30, 38, 2.4)): c.part(c.ell(x, y, r, r * .8), spots, 'soft', ink=False)

def pelt(c, fur, belly=None, spots=None, small=False, bristle=False, stone=False):
    belly = belly if belly is not None else lite(fur, .3)
    pts = [(50, 8), (60, 14), (66, 24), (86, 22), (92, 30), (74, 40), (72, 60), (90, 74), (84, 84), (66, 76), (56, 82), (52, 96), (48, 96), (44, 82),
           (34, 76), (16, 84), (10, 74), (28, 60), (26, 40), (8, 30), (14, 22), (34, 24), (40, 14)]
    if small: c.push(sc=.82)
    body = c.poly(pts, smooth=True, n=6)
    c.part(body, dark(fur, .18), 'soft', grain=.5 if not stone else .15)
    c.part(c.grow(body, -9), fur, 'soft', ink=False, grain=.5 if not stone else .15)
    c.part(c.poly([(50, 16), (60, 30), (62, 56), (56, 74), (50, 80), (44, 74), (38, 56), (40, 30)], smooth=True), belly, 'soft', ink=False, clip=body, alpha=.85)
    c.flat(c.taper([(50, 12), (50, 50), (50, 90)], 3, 2, wmid=7), dark(fur, .5), .75, clip=body)
    if spots:
        for x, y in ((36, 40), (64, 42), (38, 62), (63, 64), (32, 52), (68, 54), (44, 30), (57, 30)): c.flat(c.ell(x, y, 3, 2.4), spots, .95, clip=body)
    if bristle:
        for y in range(22, 84, 9): c.flat(c.line([(47, y), (50, y - 6), (53, y)], 1.6), dark(fur, .7), .9, clip=body)
    if small: c.pop()

def meat_ham(c, flesh='#c4483f', fat='#f0d9b8', bone='#efe6d2', lean=False):
    c.push(rot=-35)
    c.part(c.rect(62, 44, 92, 56, 5), bone, 'soft')
    c.part(c.U_(c.ell(91, 45, 6.5), c.ell(91, 55, 6.5)), bone, 'soft')
    m = c.poly([(12, 50), (18, 28), (40, 20), (62, 30), (72, 50), (62, 70), (40, 80), (18, 72)], smooth=True)
    c.part(m, fat, 'soft')
    c.part(c.poly([(18, 50), (23, 33), (41, 27), (58, 35), (65, 50), (58, 65), (41, 73), (23, 67)], smooth=True), flesh, 'soft', clip=m)
    if not lean:
        for pts in ([(26, 44), (38, 40), (50, 44)], [(30, 58), (44, 56), (56, 60)]): c.flat(c.line(pts, 1.8, smooth=True), lite(fat, .1), .8, clip=m)
    c.shine(c.ell(32, 36, 9, 3.5, rot=-20), .4)
    c.pop()

def bowl_stew(c, stew='#9a5a24', bowl='#7a4e2c', bits=('#6e3a1e', '#e08a2c', '#7fae48')):
    for x, k in ((38, 1), (52, -1), (64, 1)):
        c.flat(c.line([(x, 34), (x + 4 * k, 26), (x - 3 * k, 18), (x + 3 * k, 9)], 3.2, smooth=True), '#f6efe2', .55, blur=2)
    body = c.AND(c.ell(50, 54, 40, 36), c.rect(0, 54, 100, 100))
    c.part(body, bowl, 'wood')
    c.part(c.rect(38, 86, 62, 92, 2), dark(bowl, .25), 'wood')
    c.part(c.ell(50, 54, 41, 13), lite(bowl, .2), 'wood')
    surf = c.ell(50, 54, 36, 9.5)
    c.part(surf, stew, 'soft', light=.4)
    for i, (x, y, r) in enumerate(((34, 53, 6), (50, 57, 5), (63, 52, 6.5), (45, 50, 4), (72, 56, 3.5), (25, 56, 3.5))):
        c.part(c.blob(x, y, r, r * .7, n=6, jit=.2), bits[i % 3], 'soft', clip=surf)
    c.shine(c.ell(30, 74, 4, 9, rot=25), .25)

def loaf(c, crust='#b8742e', slash='#f0d7a0', dust=True, hot=None):
    c.push(rot=-22)
    body = c.poly([(8, 52), (16, 34), (38, 26), (64, 26), (86, 36), (92, 54), (84, 70), (60, 78), (36, 78), (16, 70)], smooth=True)
    c.part(body, crust, 'soft')
    c.part(c.AND(body, c.rect(0, 62, 100, 100)), dark(crust, .3), 'soft', ink=False, alpha=.6)
    for x in (30, 48, 66):
        s = c.taper([(x - 5, 34), (x + 1, 46), (x + 5, 60)], 1, 1, wmid=7)
        if hot: c.glow(s, hot, halo=5, power=.6, clip=body)
        else: c.part(s, slash, 'soft', clip=body)
    if dust:
        for i in range(14): c.flat(c.ell(18 + c.rng.rand() * 64, 32 + c.rng.rand() * 20, 1.1), '#fff3da', .7, clip=body)
    c.pop()

def flask(c, liquid, shape='round', glass='#bfe0e6', cork='#9a6a3a', level=.62, glowc=None, label=None):
    if shape == 'round': body = c.U_(c.ell(50, 62, 29, 28), c.rect(41, 22, 59, 44)); top = 22
    elif shape == 'tall': body = c.U_(c.rect(30, 40, 70, 92, 9), c.poly([(30, 48), (42, 28), (58, 28), (70, 48)]), c.rect(42, 16, 58, 32)); top = 16
    elif shape == 'tri': body = c.U_(c.poly([(50, 20), (86, 84), (80, 92), (20, 92), (14, 84)], smooth=False), c.rect(42, 14, 58, 40)); top = 14
    else: body = c.U_(c.rect(34, 30, 66, 92, 8), c.rect(41, 12, 59, 34)); top = 12        # vial
    c.part(body, dark(glass, .45), 'gem', alpha=.9)
    inner = c.grow(body, -5)
    ly = top + (94 - top) * (1 - level)
    liq = c.AND(inner, c.rect(0, ly, 100, 100))
    if liquid is not None:
        if glowc: c.glow(liq, glowc, core=lite(liquid, .3), halo=12, power=.5)
        c.part(liq, liquid, 'gem', ink=False)
        c.flat(c.AND(inner, c.rect(0, ly, 100, ly + 3)), lite(liquid, .45), .9)
    c.part(c.rect(39, top - 3, 61, top + 4, 2), lite(glass, .1), 'gem')
    c.part(c.rect(42, top - 12, 58, top - 2, 2), cork, 'matte')
    c.shine(c.taper([(34, 50), (30, 62), (33, 76)], 2, 2, wmid=6), .6, clip=inner)
    c.shine(c.ell(64, 82, 3, 2), .35, clip=inner)
    if label: c.part(c.rect(40, 62, 62, 78, 1), label, 'matte', clip=inner)

def tool_pick(c, iron='#8d96a3', wood='#8a5a30'):
    c.push(rot=40)
    c.part(c.rect(45.5, 20, 54.5, 104, 3), wood, 'wood', grain=.3)
    c.part(c.taper([(8, 40), (28, 22), (50, 16), (72, 22), (92, 40)], 1.5, 1.5, wmid=15), iron, 'metal')
    c.part(c.rect(43, 11, 57, 25, 2), dark(iron, .2), 'metal')
    c.pop()

def tool_hatchet(c, iron='#9aa2ad', wood='#8a5a30', wrap='#5a3f28'):
    c.push(rot=38)
    c.part(c.rect(46, 10, 55, 102, 3), wood, 'wood', grain=.3)
    c.part(c.rect(45, 76, 56, 96, 2), wrap, 'matte')
    head = c.poly([(58, 12), (58, 36), (44, 34), (26, 48), (12, 34), (12, 12), (26, 2), (44, 14)], smooth=False)
    c.part(head, iron, 'metal')
    c.part(c.poly([(26, 2), (12, 12), (12, 34), (26, 48), (20, 34), (20, 14)]), lite(iron, .45), 'metal', ink=False, clip=head)
    c.pop()

def pouch(c, leather, tie='#d9c9a0', emblem=None, peek=None):
    if peek: peek(c)
    body = c.poly([(50, 30), (66, 34), (84, 56), (86, 78), (72, 92), (50, 95), (28, 92), (14, 78), (16, 56), (34, 34)], smooth=True)
    c.part(body, leather, 'soft')
    c.part(c.poly([(30, 30), (38, 14), (50, 20), (62, 12), (72, 30), (60, 36), (40, 36)], smooth=True), lite(leather, .1), 'soft')
    for x in (38, 50, 62): c.flat(c.line([(x, 22), (x + (x - 50) * .25, 34)], 1.3), dark(leather, .6), .7)
    c.part(c.line([(30, 35), (50, 39), (70, 35)], 4.5, smooth=True), tie, 'matte')
    c.part(c.taper([(66, 36), (76, 46), (74, 58)], 3.5, 2), tie, 'matte')
    c.part(c.ell(74, 60, 3.4), tie, 'matte')
    c.shine(c.ell(32, 60, 5, 11, rot=18), .22)
    if emblem: emblem(c)

def fang(c, bone='#efe6cf', root='#8a3b34', curve=1.0):
    c.part(c.taper([(66, 12), (64, 46), (34 + (1 - curve) * 10, 94)], 36, 0.5, wmid=27), bone, 'soft')
    c.part(c.blob(66, 15, 21, 11, n=8, jit=.15, smooth=True), root, 'soft')
    c.shine(c.taper([(56, 32), (53, 50), (42, 72)], 3, 0.5), .5)

def tusk(c, bone='#e9dcc0', root='#6b4a34'):
    c.part(c.taper([(18, 74), (48, 88), (78, 68), (84, 12)], 30, 0.5, wmid=24), bone, 'soft')
    c.part(c.blob(19, 74, 10, 17, n=8, jit=.12, smooth=True), root, 'soft')
    for t in ((40, 80, 44, 90), (56, 74, 62, 84), (68, 60, 78, 64)): c.flat(c.line([(t[0], t[1]), (t[2], t[3])], 1.2), dark(bone, .45), .6)
    c.shine(c.taper([(40, 76), (62, 68), (74, 44)], 2.5, .5), .45)

# ====================================================================================================
# lib2.py
# ====================================================================================================
# Drawers, batch 2: gear by family and variant, in a palette's colours (Resources/Gear/looks.json).

def _lift(c, lo=.30):
    c = col(c); l = float(c[0] * .3 + c[1] * .59 + c[2] * .11)
    return np.clip(c + max(0, lo - l), 0, 1)
def mkpal(p, tint=None):
    """A looks.json palette made readable on a dark tile: more saturated, lifted off black."""
    g = lambda k, s=1.3, lo=.3: _lift(vivid(p[k], s), lo)
    return dict(cloth=g('cloth', 1.4, .42), cloth2=g('cloth2', 1.4, .42), leather=g('leather', 1.3, .40), metal=_lift(vivid(tint or p['metal'], 1.25), .46),
                trim=g('trim', 1.3, .5), wood=g('wood', 1.3, .42), glow=col(p['glow']), bone=col('#e2d8bc'), tinted=tint is not None)

# ---- weapons: drawn point-up along x = 50, then turned 45 degrees so the point is at the upper right ----
def _grip(c, P, y0=66, y1=90, w=9, pommel=True, wrap=True):
    c.part(c.rect(50 - w / 2, y0, 50 + w / 2, y1, 2), P['leather'], 'matte')
    if wrap:
        for y in range(int(y0) + 5, int(y1) - 1, 6): c.flat(c.line([(50 - w / 2, y), (50 + w / 2, y + 2.5)], 1.2), dark(P['leather'], .6), .8)
    if pommel: c.part(c.ell(50, y1 + 4, 6.5), P['trim'], 'metal')
def _guard(c, P, y=64, w=40, kind='bar'):
    if kind == 'disc': c.part(c.ell(50, y, 14, 5.5), P['trim'], 'metal')
    elif kind == 'curve': c.part(c.taper([(50 - w / 2, y - 7), (50, y + 2), (50 + w / 2, y - 7)], 5, 5, wmid=7.5), P['trim'], 'metal')
    else:
        c.part(c.rect(50 - w / 2, y - 3.5, 50 + w / 2, y + 3.5, 2.5), P['trim'], 'metal')
def _blade(c, pts, metal, edge=True, fuller=True, smooth=False, mat='metal', y0=8, y1=60):
    b = c.poly(pts, smooth=smooth)
    c.part(b, metal, mat)
    if edge: c.part(c.AND(b, c.rect(0, -20, 49.5, 120)), lite(metal, .38), mat, ink=False, light=.5)
    if fuller: c.flat(c.line([(50.5, y0 + 10), (50.5, y1)], 1.5), dark(metal, .55), .7, clip=b)
    return b
def _veins(c, b, color, pts=((50, 60), (48, 44), (53, 30), (49, 14))):
    c.glow(c.line(pts, 2.2, smooth=True), color, halo=6, power=.7, clip=b)

def g_sword(c, P, fam, v, glow):
    c.push(rot=45, sc=1.2)
    m = P['metal']
    if fam == 'sword.short':
        b = _blade(c, [(41, 64), (41, 26), (50, 10), (59, 26), (59, 64)], m, y0=18)
        if v == 'notched': c.part(c.poly([(40, 40), (45, 43), (40, 47)]), dark(m, .75), 'matte', ink=False); c.part(c.poly([(60, 30), (55, 33), (60, 36)]), dark(m, .75), 'matte', ink=False)
        _guard(c, P, 66, 34); _grip(c, P, 69, 90)
    elif fam == 'sword.arming':
        if v == 'curved': b = _blade(c, [(44, 64), (43, 30), (48, 4), (58, 22), (57, 64)], m, y0=12)
        else: b = _blade(c, [(43.5, 64), (43.5, 14), (50, -2), (56.5, 14), (56.5, 64)], m, y0=4)
        _guard(c, P, 66, 42, 'disc' if v == 'disc' else 'bar'); _grip(c, P, 69, 90)
    elif fam == 'sword.falchion':
        w = 4 if v == 'heavy' else 0
        b = _blade(c, [(44, 64), (43, 30), (40 - w, 12), (48, -2), (62 + w, 14), (58, 64)], m, fuller=False)
        c.flat(c.line([(54, 14), (54, 60)], 1.5), dark(m, .5), .6, clip=b)
        _guard(c, P, 66, 36, 'curve'); _grip(c, P, 69, 90)
    elif fam == 'sword.sabre':
        if v == 'broken':
            b = c.poly([(44, 64), (44, 34), (49, 38), (52, 30), (56, 36), (58, 30), (57, 64)])
            c.part(b, m, 'metal'); c.part(c.AND(b, c.rect(0, 0, 49.5, 100)), lite(m, .38), 'metal', ink=False)
            c.glow(c.line([(44, 34), (49, 38), (52, 30), (56, 36), (58, 30)], 2), P['glow'], halo=9, power=.9)
            c.glow(c.poly([(50, 22), (54, 10), (58, 4), (55, 16)]), P['glow'], halo=8, power=.6)
        else:
            b = c.taper([(50, 64), (51, 40), (56, 18), (66, -2)], 13, 1)
            c.part(b, m, 'metal'); c.shine(c.taper([(47, 60), (48, 40), (53, 20), (62, 4)], 2.5, .5), .55, clip=b)
        c.part(c.line([(36, 64), (33, 78), (44, 91)], 3.4, smooth=True), P['trim'], 'metal')
        _guard(c, P, 66, 30); _grip(c, P, 69, 90)
    elif fam == 'sword.leaf':
        mm = '#c9924a' if (v == 'bronze' and not P['tinted']) else m
        b = _blade(c, [(50, -2), (61, 22), (57, 44), (56, 64), (44, 64), (43, 44), (39, 22)], mm, smooth=True)
        if v == 'root': c.part(c.line([(44, 70), (56, 60), (44, 52), (55, 44)], 3, smooth=True), P['wood'], 'wood')
        _guard(c, P, 66, 26); _grip(c, P, 69, 90)
    else:   # sword.great
        mm = P['wood'] if v in ('wood', 'living') else m
        b = _blade(c, [(41, 56), (41, 10), (50, -8), (59, 10), (59, 56)], mm, mat='wood' if v != 'steel' else 'metal', y0=0, y1=52)
        if v == 'wood' or glow: _veins(c, b, P['glow'], ((50, 54), (47, 40), (53, 26), (49, 6)))
        if v == 'living':
            _veins(c, b, '#7fe07a', ((50, 54), (47, 40), (53, 26), (49, 6)))
            c.part(c.taper([(58, 34), (68, 28), (72, 20)], 6, .5), '#6dbb4e', 'matte')
        _guard(c, P, 58, 50); _grip(c, P, 61, 94, 8)
    c.pop()

def g_knife(c, P, v, glow):
    c.push(rot=45, sc=1.25)
    m = P['metal']
    if v == 'needle': b = _blade(c, [(46, 62), (47, 20), (50, 0), (53, 20), (54, 62)], m)
    elif v == 'glass':
        b = c.poly([(42, 62), (40, 34), (47, 8), (52, 2), (60, 30), (58, 62)])
        c.glow(b, P['glow'], core=lite(P['glow'], .7), halo=12, power=.5)
        c.part(b, mix(P['glow'], '#e8f6ff', .45), 'gem', alpha=.92)
        c.part(c.poly([(42, 62), (40, 34), (47, 8), (50, 30), (49, 62)]), '#ffffff', 'gem', ink=False, alpha=.45)
    elif v == 'sickle':
        b = c.taper([(50, 62), (44, 40), (52, 16), (74, 10), (86, 24)], 12, 1)
        c.part(b, m, 'metal'); c.shine(c.taper([(47, 44), (54, 20), (74, 14)], 2, .5), .6, clip=b)
    else:   # hooked
        b = c.poly([(43, 62), (42, 30), (46, 10), (58, 2), (66, 12), (58, 16), (60, 30), (58, 62)], smooth=False)
        c.part(b, m, 'metal'); c.part(c.AND(b, c.rect(0, 0, 49.5, 100)), lite(m, .38), 'metal', ink=False)
    if glow and v != 'glass': c.glow(c.line([(50, 56), (50, 24)], 1.8), P['glow'], halo=7, power=.7, clip=b)
    _guard(c, P, 64, 26); _grip(c, P, 67, 92, 10)
    c.pop()

def _haft(c, P, y0=8, y1=104, w=8.5, wrap=True):
    c.part(c.rect(50 - w / 2, y0, 50 + w / 2, y1, 3), P['wood'], 'wood', grain=.3)
    if wrap: c.part(c.rect(50 - w / 2 - 1, y1 - 26, 50 + w / 2 + 1, y1 - 8, 2), P['leather'], 'matte')
def _axehead(c, pts, m, edge):
    h = c.poly(pts); c.part(h, m, 'metal'); c.part(c.poly(edge), lite(m, .45), 'metal', ink=False, clip=h); return h

def g_axe(c, P, fam, v, glow):
    c.push(rot=40, sc=1.15, dx=3, dy=-3); m = P['metal']; _haft(c, P)
    if fam == 'axe.hand':
        if v == 'wedge': _axehead(c, [(57, 12), (57, 36), (44, 36), (18, 44), (16, 6), (44, 14)], m, [(18, 44), (16, 6), (24, 10), (25, 40)])
        else: _axehead(c, [(58, 12), (58, 36), (44, 34), (26, 48), (12, 34), (12, 12), (26, 2), (44, 14)], m, [(26, 2), (12, 12), (12, 34), (26, 48), (20, 34), (20, 14)])
    elif fam == 'axe.bearded':
        h = _axehead(c, [(57, 10), (57, 30), (44, 30), (34, 62), (14, 54), (10, 14), (28, 4), (44, 12)], m, [(34, 62), (14, 54), (10, 14), (28, 4), (20, 18), (21, 50)])
        if v == 'hooked': c.part(c.poly([(57, 12), (74, 8), (70, 20), (57, 26)]), m, 'metal')
    else:   # crescent
        h = _axehead(c, [(57, 10), (57, 38), (44, 36), (32, 58), (8, 26), (30, -6), (44, 14)], m, [(32, 58), (8, 26), (30, -6), (20, 26)])
        if v == 'spiked': c.part(c.poly([(56, 16), (80, 24), (56, 32)]), m, 'metal'); c.part(c.poly([(45, 10), (50, -8), (55, 10)]), m, 'metal')
    c.pop()

def g_cleaver(c, P, v, glow):
    c.push(rot=40, sc=1.2); m = P['metal']
    b = c.poly([(40, 4), (70, 2), (74, 54), (58, 60), (40, 58)])
    if v == 'notched': b = c.sub(b, c.U_(c.poly([(76, 20), (66, 24), (76, 29)]), c.poly([(76, 38), (68, 41), (76, 45)])))
    c.part(b, m, 'metal'); c.part(c.AND(b, c.rect(62, 0, 100, 70)), lite(m, .42), 'metal', ink=False)
    c.part(c.ell(48, 13, 3.4), dark(m, .8), 'matte', ink=False)
    _grip(c, P, 58, 92, 11, pommel=False); c.pop()

def g_club(c, P, v, glow):
    if v == 'tankard':
        gold = '#e0aa3a'
        c.part(c.sub(c.ell(76, 54, 16, 20), c.ell(76, 54, 8.5, 12)), gold, 'metal')
        body = c.poly([(22, 24), (68, 24), (72, 90), (18, 90)])
        c.part(body, gold, 'metal')
        for y in (36, 78): c.part(c.rect(18, y - 3.5, 72, y + 3.5), lite(gold, .25), 'metal', clip=body)
        c.part(c.ell(45, 57, 10, 12), dark(gold, .25), 'metal', clip=body); c.flat(c.star(45, 57, 7, 3, 5), lite(gold, .5))
        c.part(c.U_(c.ell(30, 20, 11, 8), c.ell(46, 16, 13, 9), c.ell(62, 21, 10, 7)), '#f6efe0', 'soft')
        return
    c.push(rot=40, sc=1.15); w = P['wood']
    body = c.taper([(50, 102), (50, 60), (50, 8)], 9, 25, smooth=False)
    c.part(body, w, 'wood', grain=.35)
    c.part(c.rect(44, 80, 56, 98, 2), P['leather'], 'matte')
    if v == 'studded':
        for x, y in ((44, 14), (57, 20), (46, 30), (57, 38), (50, 22), (50, 44)): c.part(c.ell(x, y, 3.6), P['metal'], 'metal')
    elif v == 'bound':
        for y in (14, 30, 46): c.part(c.rect(34, y - 3.5, 66, y + 3.5), P['metal'], 'metal', clip=c.grow(body, 2))
    elif v == 'tusk':
        for sx in (-1, 1): c.part(c.taper([(50 + sx * 8, 34), (50 + sx * 22, 26), (50 + sx * 26, 4)], 10, .5), P['bone'], 'soft')
        c.part(c.rect(37, 28, 63, 38, 2), P['leather'], 'matte')
    c.pop()

def g_mace(c, P, fam, v, glow):
    c.push(rot=40, sc=1.15, dx=2, dy=-2); m = P['metal']
    if fam == 'mace.flanged':
        _haft(c, P, 20)
        if v == 'fist':
            st = '#9a968c'
            c.part(c.blob(50, 20, 21, 19, n=9, jit=.1, smooth=True), st, 'matte')
            for x in (38, 46, 54, 62): c.part(c.ell(x, 8, 5, 6), lite(st, .12), 'matte')
            c.part(c.ell(33, 24, 6, 8, rot=-20), lite(st, .1), 'matte')
        else:
            for sx in (-1, 1): c.part(c.poly([(50, 4), (50 + sx * 22, 12), (50 + sx * 24, 26), (50, 40)]), m, 'metal')
            c.part(c.poly([(42, 2), (58, 2), (60, 40), (40, 40)]), lite(m, .2), 'metal')
            c.part(c.poly([(46, 2), (50, -8), (54, 2)]), m, 'metal'); c.part(c.rect(42, 40, 58, 46, 1), P['trim'], 'metal')
    elif fam == 'hammer.war':
        _haft(c, P, 12)
        if v == 'maul':
            c.part(c.rect(22, 4, 78, 38, 4), m, 'metal'); c.part(c.rect(22, 4, 32, 38, 3), lite(m, .3), 'metal'); c.part(c.rect(68, 4, 78, 38, 3), dark(m, .2), 'metal')
            c.part(c.rect(44, 0, 56, 42, 1), P['trim'], 'metal')
        else:
            c.part(c.poly([(56, 10), (86, 20), (56, 30)]), m, 'metal')
            c.part(c.rect(18, 6, 48, 34, 3), m, 'metal'); c.part(c.rect(18, 6, 26, 34, 2), lite(m, .3), 'metal')
            c.part(c.rect(43, 4, 57, 36, 1), P['trim'], 'metal')
    else:   # mace.root
        w = P['wood']
        c.part(c.taper([(50, 104), (52, 70), (50, 36)], 8, 11), w, 'wood', grain=.3)
        if v == 'antler':
            for pts, w0 in (([(50, 40), (40, 22), (26, 6)], 9), ([(50, 40), (60, 20), (70, 0)], 9), ([(42, 26), (30, 28), (20, 22)], 6), ([(60, 22), (72, 22), (82, 12)], 6)):
                c.part(c.taper(pts, w0, 2), '#8a5f44', 'soft')
            c.part(c.rect(43, 38, 57, 48, 2), P['leather'], 'matte')
        else:
            c.part(c.blob(50, 22, 21, 20, n=10, jit=.16, smooth=True), w, 'wood', grain=.3)
            for x, y, r in ((42, 14, 6), (58, 26, 7), (52, 8, 4)): c.part(c.ell(x, y, r), lite(w, .12), 'wood', ink=False)
            if v == 'briar':
                for a in range(0, 360, 45): c.part(c.taper([(50 + 18 * math.cos(math.radians(a)), 22 + 17 * math.sin(math.radians(a))), (50 + 31 * math.cos(math.radians(a + 10)), 22 + 30 * math.sin(math.radians(a + 10)))], 6, .5, smooth=False), '#3f5a2a', 'matte')
                c.glow(c.ell(50, 22, 6.5), '#ff4a3a', halo=8, power=.8)
            elif glow: c.glow(c.ell(50, 22, 6), P['glow'], halo=8, power=.8)
    c.pop()

def g_polearm(c, P, v, glow):
    c.push(rot=42, sc=1.12, dx=2, dy=-2); m = P['metal']
    c.part(c.rect(46.5, 20, 53.5, 112, 2), P['wood'], 'wood', grain=.3)
    if v == 'harpoon':
        b = c.poly([(50, -12), (58, 8), (55, 8), (64, 24), (55, 22), (55, 34), (45, 34), (45, 22), (36, 24), (45, 8), (42, 8)])
        c.part(b, P['bone'], 'soft'); c.part(c.line([(53, 36), (62, 50), (56, 66), (64, 84)], 2.6, smooth=True), '#d9c9a0', 'matte'); c.part(c.rect(44, 32, 56, 40, 1), P['leather'], 'matte')
    elif v == 'fork':
        for x in (34, 50, 66): c.part(c.taper([(50, 34), (x, 26), (x + (4 if x == 66 else 0), -8)], 5, 1.5), m, 'metal')
        c.part(c.rect(44, 30, 56, 38, 1), m, 'metal')
    elif v == 'billhook':
        b = c.poly([(44, 36), (44, 6), (48, -8), (60, -10), (70, 0), (60, 2), (58, 12), (66, 16), (58, 22), (57, 36)])
        c.part(b, m, 'metal'); c.part(c.AND(b, c.rect(0, -20, 49, 60)), lite(m, .4), 'metal', ink=False)
    elif v == 'spade':
        b = c.poly([(32, 30), (34, 4), (50, -10), (66, 4), (68, 30)], smooth=False)
        c.part(b, m, 'metal'); c.part(c.AND(b, c.rect(0, -20, 44, 60)), lite(m, .35), 'metal', ink=False); c.part(c.rect(44, 28, 56, 38, 1), dark(m, .2), 'metal')
    else:
        b = _blade(c, [(50, -12), (60, 10), (55, 30), (45, 30), (40, 10)], m, y0=-4, y1=26); c.part(c.rect(44, 28, 56, 36, 1), P['trim'], 'metal')
    c.pop()

def g_staff(c, P, v, glow):
    c.push(rot=40, sc=1.12); w = P['wood']
    c.part(c.taper([(50, 110), (51, 60), (50, 18)], 7, 8), w, 'wood', grain=.3)
    c.part(c.rect(45, 62, 55, 76, 2), P['leather'], 'matte')
    if v == 'crook':
        c.part(c.taper([(50, 22), (50, 6), (62, -6), (76, 2), (76, 16), (66, 20)], 8, 5), w, 'wood', grain=.3)
    elif v == 'forked':
        for sx in (-1, 1): c.part(c.taper([(50, 26), (50 + sx * 14, 12), (50 + sx * 13, -8)], 7, 3), w, 'wood')
        c.glow(c.ell(50, 4, 7.5), P['glow'], halo=11, power=1.0)
    elif v == 'skull':
        b = P['bone']
        c.part(c.U_(c.ell(50, 8, 14, 13), c.rect(42, 14, 58, 26, 2)), b, 'soft')
        for sx in (-1, 1): c.part(c.ell(50 + sx * 6, 10, 3.8, 4.4), '#1a1216', 'matte', ink=False)
        c.flat(c.poly([(50, 15), (47.5, 20), (52.5, 20)]), '#1a1216')
        if glow:
            for sx in (-1, 1): c.glow(c.ell(50 + sx * 6, 10, 1.8), P['glow'], halo=5, power=.8)
    else:
        c.part(c.ell(50, 12, 12, 13), w, 'wood'); c.part(c.rect(43, 22, 57, 28, 1), P['trim'], 'metal')
        if glow: c.glow(c.ell(50, 12, 5), P['glow'], halo=9, power=.9)
    c.pop()

# ---- shields and things hung from the off hand ----
def _rivets(c, pts, m, r=2.4):
    for x, y in pts: c.part(c.ell(x, y, r), lite(m, .3), 'metal')

def g_shield(c, P, fam, v, glow):
    m = P['metal']; f1 = P['cloth2']; f2 = P['cloth']
    if fam == 'shield.buckler':
        if v == 'tusk':
            for sx in (-1, 1): c.part(c.taper([(50 + sx * 26, 70), (50 + sx * 44, 46), (50 + sx * 36, 10)], 13, .5), P['bone'], 'soft')
        face = P['wood'] if v != 'chitin' else '#3d6a4a'
        d = c.ell(50, 52, 34); c.part(d, m if v != 'chitin' else dark(face, .3), 'metal'); c.part(c.ell(50, 52, 28), face, 'wood' if v != 'chitin' else 'gem', grain=.2)
        if v == 'chitin':
            for y in (38, 52, 66): c.flat(c.line([(24, y), (50, y + 6), (76, y)], 1.6, smooth=True), dark(face, .6), .8, clip=d)
        c.part(c.ell(50, 52, 12), m, 'metal'); c.shine(c.ell(46, 47, 4, 3), .6)
        if v != 'chitin': _rivets(c, [(50 + 31 * math.cos(a), 52 + 31 * math.sin(a)) for a in [i * math.pi / 4 for i in range(8)]], m, 2)
    elif fam == 'shield.round':
        if v == 'lid':
            body = c.poly([(12, 30), (88, 24), (90, 78), (14, 84)])
            c.part(body, P['wood'], 'wood', grain=.3)
            for x in (30, 70): c.part(c.poly([(x - 4, 24), (x + 4, 24), (x + 5, 86), (x - 3, 86)]), m, 'metal', clip=body)
            c.part(c.sub(body, c.grow(body, -9)), m, 'metal')
            c.part(c.rect(43, 44, 57, 66, 2), P['trim'], 'metal'); c.flat(c.U_(c.ell(50, 52, 2.6), c.rect(49, 52, 51, 60)), '#141014')
            _rivets(c, [(17, 34), (84, 29), (86, 74), (19, 79)], m)
            return
        d = c.ell(50, 52, 40)
        if v == 'hide':
            c.part(d, P['wood'], 'wood'); hide = c.blob(50, 52, 34, n=12, jit=.07, smooth=True); c.part(hide, P['leather'], 'soft', grain=.3)
            for a in range(0, 360, 30): c.flat(c.line([(50 + 30 * math.cos(math.radians(a)), 52 + 30 * math.sin(math.radians(a))), (50 + 38 * math.cos(math.radians(a)), 52 + 38 * math.sin(math.radians(a)))], 1.6), '#e2d8bc', .9)
            c.part(c.ell(50, 52, 9), m, 'metal')
        else:
            c.part(d, m, 'metal'); face = c.ell(50, 52, 34); c.part(face, P['wood'], 'wood', grain=.3)
            if v == 'cask':
                for x in (30, 42, 58, 70): c.flat(c.line([(x, 14), (x, 90)], 1.5), dark(P['wood'], .6), .8, clip=face)
                c.part(c.sub(c.ell(50, 52, 24), c.ell(50, 52, 19)), m, 'metal'); c.part(c.ell(50, 52, 6), dark(P['wood'], .5), 'wood')
            else:
                for x in (36, 50, 64): c.flat(c.line([(x, 14), (x, 90)], 1.5), dark(P['wood'], .6), .8, clip=face)
                c.part(c.AND(face, c.rect(0, 46, 100, 58)), f1, 'matte'); c.part(c.ell(50, 52, 11), m, 'metal'); c.shine(c.ell(46, 48, 4, 3), .6)
    elif fam == 'shield.heater':
        pts = [(16, 12), (50, 16), (84, 12), (86, 46), (74, 74), (50, 96), (26, 74), (14, 46)]
        d = c.poly(pts, smooth=True, n=6)
        if v == 'glass':
            c.glow(d, P['glow'], core=lite(P['glow'], .5), halo=13, power=.45)
            c.part(d, mix(P['glow'], '#dff2ff', .35), 'gem', alpha=.95)
            for p in ([(50, 16), (40, 50), (50, 96)], [(16, 12), (40, 50)], [(84, 12), (62, 44), (50, 96)], [(40, 50), (62, 44)]): c.flat(c.line(p, 1.6), '#ffffff', .65, clip=d)
            c.part(c.sub(d, c.grow(d, -6)), m, 'metal')
        else:
            c.part(d, m, 'metal'); inner = c.grow(d, -8); c.part(inner, f1, 'matte')
            if v == 'striped':
                for x in (28, 50, 72): c.part(c.rect(x - 6, 0, x + 6, 100), f2, 'matte', ink=False, clip=inner)
            else:
                c.part(c.poly([(10, 34), (50, 58), (90, 34), (90, 48), (50, 72), (10, 48)]), P['trim'], 'matte', clip=inner)
            if glow: c.glow(c.ell(50, 40, 6), P['glow'], halo=9, power=.8)
    elif fam == 'shield.kite':
        pts = [(22, 12), (50, 6), (78, 12), (82, 40), (68, 74), (50, 98), (32, 74), (18, 40)]
        d = c.poly(pts, smooth=True, n=6)
        if v == 'slab':
            st = '#8f8a84'
            d = c.poly([(24, 10), (52, 4), (80, 14), (84, 44), (70, 76), (48, 98), (30, 72), (16, 42)])
            c.part(d, st, 'matte'); c.part(c.poly([(24, 10), (52, 4), (46, 40), (16, 42)]), lite(st, .16), 'matte', clip=d); c.part(c.poly([(46, 40), (84, 44), (70, 76), (48, 98)]), dark(st, .2), 'matte', clip=d)
            for p in ([(52, 4), (46, 40), (60, 60), (48, 98)], [(46, 40), (22, 52)], [(60, 60), (80, 50)]): c.glow(c.line(p, 2.2), P['glow'], halo=6, power=.7, clip=d)
        else:
            c.part(d, m, 'metal'); inner = c.grow(d, -8); c.part(inner, f1, 'matte')
            c.part(c.rect(45, 0, 55, 100), P['trim'], 'matte', clip=inner); c.part(c.rect(0, 30, 100, 40), P['trim'], 'matte', clip=inner)
            c.part(c.ell(50, 35, 8), m, 'metal')
            if glow: c.glow(c.ell(50, 35, 4), P['glow'], halo=9, power=.8)
    else:   # shield.leaf
        face = '#c9924a' if (v == 'bronze' and not P['tinted']) else (P['wood'] if v == 'bark' else m)
        d = c.poly([(50, 2), (76, 26), (82, 54), (68, 80), (50, 98), (32, 80), (18, 54), (24, 26)], smooth=True, n=6)
        c.part(d, face, 'wood' if v == 'bark' else 'metal', grain=.4 if v == 'bark' else 0)
        c.part(c.taper([(50, 6), (50, 50), (50, 94)], 2, 2, wmid=8), lite(face, .25) if v != 'bark' else dark(face, .3), 'metal' if v != 'bark' else 'wood', clip=d)
        for y in (30, 46, 62):
            for sx in (-1, 1): c.flat(c.line([(50, y + 8), (50 + sx * 24, y - 6)], 1.5), dark(face, .55), .7, clip=d)
        if v == 'bark':
            c.part(c.taper([(60, 22), (74, 12), (86, 14)], 9, .5), '#6dbb4e', 'matte')
            if glow: c.glow(c.ell(50, 50, 5), P['glow'], halo=9, power=.7)

def g_hung(c, P, v, glow):
    m = P['metal']
    if v == 'scale':
        br = P['trim']
        c.part(c.rect(47.5, 14, 52.5, 86, 1), br, 'metal'); c.part(c.poly([(34, 92), (66, 92), (58, 82), (42, 82)]), br, 'metal')
        c.part(c.line([(12, 30), (50, 22), (88, 30)], 4), br, 'metal'); c.part(c.ell(50, 14, 5), br, 'metal')
        for x in (14, 86):
            for dx in (-10, 10): c.flat(c.line([(x, 30), (x + dx, 58)], 1.2), dark(br, .3), .95)
            c.part(c.AND(c.ell(x, 58, 13, 10), c.rect(0, 58, 100, 100)), br, 'metal')
        return
    if v == 'censer':
        for sx in (-1, 0, 1): c.flat(c.line([(50, 6), (50 + sx * 16, 46)], 1.6), lite(m, .2), .95)
        c.part(c.sub(c.ell(50, 8, 6), c.ell(50, 8, 3)), m, 'metal')
        body = c.ell(50, 64, 25, 22); c.part(body, P['trim'], 'metal')
        c.part(c.rect(24, 58, 76, 66), dark(P['trim'], .25), 'metal', clip=body)
        for x in (34, 44, 56, 66): c.glow(c.ell(x, 52, 2.6, 3.4), P['glow'], halo=6, power=.8)
        c.part(c.poly([(40, 86), (60, 86), (64, 94), (36, 94)]), P['trim'], 'metal')
        for x, k in ((44, 1), (58, -1)): c.flat(c.line([(x, 40), (x + 5 * k, 32), (x - 3 * k, 24)], 3, smooth=True), lite(P['glow'], .5), .45, blur=2)
        return
    g = '#7fe07a' if v == 'moss' else P['glow']
    c.part(c.sub(c.ell(50, 14, 11, 10), c.ell(50, 14, 6.5, 6)), m, 'metal')
    c.part(c.poly([(34, 30), (50, 20), (66, 30)]), m, 'metal')
    glass = c.rect(30, 32, 70, 80, 3)
    if v == 'shuttered':
        c.part(glass, dark(m, .15), 'metal')
        for x in (42, 50, 58): c.glow(c.rect(x - 1.6, 38, x + 1.6, 74, 1), g, halo=7, power=.8)
        c.part(c.rect(28, 52, 72, 58), m, 'metal')
    else:
        c.glow(glass, mix(g, '#b06a20', .35) if v != 'moss' else g, core=g, halo=16, power=.6)
        if v == 'moss':
            for x, y, r in ((42, 66, 8), (56, 68, 9), (50, 56, 7)): c.part(c.blob(x, y, r, n=8, jit=.25, smooth=True), '#4f9a4a', 'soft', ink=False, alpha=.85)
        else: c.flat(c.taper([(50, 74), (45, 60), (50, 42)], 11, .5), '#fffbe0', .95)
        for x in (30, 50, 70): c.part(c.rect(x - 2, 30, x + 2, 82), m, 'metal')
    c.part(c.rect(26, 28, 74, 35, 2), m, 'metal'); c.part(c.rect(26, 78, 74, 88, 2), m, 'metal')

# ---- head ----
def g_head(c, P, fam, v, glow):
    m = P['metal']; L = P['leather']; C = P['cloth']
    if fam == 'head.cap':
        if v == 'flaps':
            for sx in (-1, 1): c.part(c.poly([(50 + sx * 30, 50), (50 + sx * 38, 84), (50 + sx * 20, 90), (50 + sx * 16, 52)], smooth=True), L, 'soft')
        dome = c.AND(c.ell(50, 60, 37, 44), c.rect(0, 0, 100, 62)); c.part(dome, L, 'soft')
        for x in (36, 50, 64): c.flat(c.line([(50, 17), (x, 60)], 1.4), dark(L, .55), .7, clip=dome)
        c.part(c.rect(11, 56, 89, 68, 5), P['trim'] if v != 'leaf' else C, 'matte'); c.part(c.ell(50, 16, 4.5), P['trim'], 'matte')
        if v == 'leaf':
            for x, r in ((30, -30), (50, 0), (70, 30)): c.part(c.taper([(x, 60), (x + r * .12, 46), (x + r * .25, 32)], 2, 1, wmid=13), '#6dbb4e', 'matte')
    elif fam == 'head.hood':
        col_ = C if v == 'cloth' else mix(C, '#3a4a3a', .45)
        out = c.poly([(50, 4), (76, 16), (88, 46), (86, 80), (70, 96), (30, 96), (14, 80), (12, 46), (24, 16)], smooth=True)
        c.part(out, col_, 'soft' if v == 'cloth' else 'gem')
        face = c.poly([(50, 30), (66, 40), (70, 62), (60, 84), (40, 84), (30, 62), (34, 40)], smooth=True)
        c.part(face, '#15121a', 'matte'); c.part(c.sub(c.grow(face, 4), face), lite(col_, .22), 'soft', ink=False)
        c.part(c.poly([(40, 86), (50, 92), (60, 86), (56, 96), (44, 96)]), P['trim'], 'matte')
    elif fam == 'head.coif':
        out = c.poly([(50, 6), (74, 16), (82, 44), (80, 70), (94, 94), (6, 94), (20, 70), (18, 44), (26, 16)], smooth=True)
        c.part(out, m, 'metal', light=.6)
        for y in range(10, 96, 6):
            for x in range(8 + (y // 6 % 2) * 3, 96, 6): c.flat(c.ell(x, y, 1.5), dark(m, .6), .55, clip=out)
        face = c.poly([(50, 32), (64, 40), (66, 60), (58, 76), (42, 76), (34, 60), (36, 40)], smooth=True); c.part(face, '#15121a', 'matte')
    elif fam == 'head.wrap':
        out = c.ell(50, 44, 36, 32); c.part(out, C, 'soft')
        for pts in ([(16, 40), (40, 26), (84, 34)], [(15, 52), (46, 40), (86, 50)], [(20, 64), (50, 54), (82, 64)]): c.flat(c.line(pts, 1.8, smooth=True), dark(C, .5), .75, clip=out)
        c.part(c.taper([(72, 62), (82, 78), (76, 96)], 15, 7), P['cloth2'], 'soft'); c.part(c.ell(40, 36, 5.5), P['trim'], 'metal')
    elif fam == 'head.kettle':
        if v == 'half':
            dome = c.AND(c.ell(50, 60, 36, 46), c.rect(0, 0, 100, 62)); c.part(dome, m, 'metal')
            c.part(c.rect(12, 56, 88, 67, 3), dark(m, .15), 'metal'); c.part(c.poly([(45, 60), (55, 60), (54, 92), (46, 92)]), m, 'metal')
            c.part(c.rect(47, 12, 53, 60), lite(m, .2), 'metal', clip=dome)
        else:
            c.part(c.ell(50, 68, 46, 14), dark(m, .12), 'metal')
            dome = c.AND(c.ell(50, 64, 30, 50), c.rect(0, 0, 100, 66)); c.part(dome, m, 'metal')
            c.part(c.ell(50, 66, 30, 7), dark(m, .3), 'metal', ink=False, alpha=.6); c.part(c.rect(47, 14, 53, 62), lite(m, .2), 'metal', clip=dome)
            _rivets(c, [(24, 62), (37, 65), (50, 66), (63, 65), (76, 62)], m, 2)
        c.shine(c.ell(36, 32, 5, 10, rot=25), .5)
    elif fam == 'head.barbute':
        out = c.poly([(50, 4), (76, 14), (86, 42), (84, 80), (74, 96), (26, 96), (16, 80), (14, 42), (24, 14)], smooth=True)
        c.part(out, m, 'metal')
        c.part(c.U_(c.rect(26, 42, 74, 54, 3), c.rect(44, 50, 56, 98)), '#121016', 'matte')
        c.part(c.rect(47.5, 6, 52.5, 42), lite(m, .22), 'metal', clip=out); c.shine(c.ell(32, 28, 5, 11, rot=25), .5)
        if v == 'rimed':
            for x, y, r in ((22, 84, 6), (34, 92, 5), (70, 90, 6), (80, 78, 5), (20, 44, 4), (58, 8, 5)): c.part(c.blob(x, y, r, n=7, jit=.3), '#f2f4f6', 'matte')
    elif fam == 'head.mask':
        b = P['bone']
        out = c.poly([(50, 6), (76, 14), (84, 40), (72, 74), (50, 96), (28, 74), (16, 40), (24, 14)], smooth=True); c.part(out, b, 'soft')
        for sx in (-1, 1):
            c.part(c.ell(50 + sx * 16, 40, 9, 7, rot=sx * 15), '#141016', 'matte', ink=False)
            if v == 'tear': c.part(c.taper([(50 + sx * 16, 48), (50 + sx * 17, 62), (50 + sx * 15, 76)], 4.5, 1), '#3a2a44', 'matte', ink=False)
        c.flat(c.poly([(50, 50), (45, 62), (55, 62)]), '#141016'); c.flat(c.line([(50, 8), (52, 22), (48, 30)], 1.3), dark(b, .5), .7)
    elif fam == 'head.circlet':
        if v == 'briar':
            g = '#5d7a3a'
            c.part(c.sub(c.ell(50, 54, 40, 22), c.ell(50, 50, 31, 14)), g, 'wood')
            for a in range(0, 360, 30):
                x, y = 50 + 37 * math.cos(math.radians(a)), 54 + 20 * math.sin(math.radians(a))
                c.part(c.taper([(x, y), (x + 7 * math.cos(math.radians(a + 20)), y - 8)], 4.5, .5, smooth=False), dark(g, .2), 'matte')
            c.part(c.ell(50, 72, 5), '#d0404a', 'gem')
        else:
            c.part(c.sub(c.ell(50, 54, 40, 22), c.ell(50, 49, 33, 14)), P['trim'], 'metal'); c.part(c.poly([(50, 56), (58, 68), (50, 82), (42, 68)]), P['glow'], 'gem'); c.shine(c.ell(48, 66, 2, 3), .7)
    else:   # head.crown
        if v == 'antler':
            for sx in (-1, 1):
                c.part(c.taper([(50 + sx * 18, 62), (50 + sx * 30, 36), (50 + sx * 26, 4)], 8, 1.5), '#a8845e', 'soft')
                c.part(c.taper([(50 + sx * 27, 44), (50 + sx * 42, 36), (50 + sx * 44, 18)], 6, 1), '#a8845e', 'soft')
                c.part(c.taper([(50 + sx * 28, 26), (50 + sx * 16, 18), (50 + sx * 12, 6)], 5, 1), '#a8845e', 'soft')
            c.part(c.rect(20, 60, 80, 78, 4), P['wood'], 'wood'); c.part(c.ell(50, 69, 5), '#6dbb4e', 'gem')
        else:
            col_ = '#a9adb2' if v == 'tin' else P['wood']
            band = c.poly([(14, 86), (10, 28), (28, 52), (38, 18), (50, 48), (62, 18), (72, 52), (90, 28), (86, 86)])
            c.part(band, col_, 'metal' if v == 'tin' else 'wood', grain=0 if v == 'tin' else .4)
            c.part(c.rect(12, 72, 88, 88, 2), dark(col_, .18), 'metal' if v == 'tin' else 'wood')
            if v == 'tin':
                _rivets(c, [(26, 80), (50, 80), (74, 80)], col_, 3); c.flat(c.line([(20, 60), (30, 66)], 1.4), dark(col_, .6), .8)
            else:
                for x in (30, 50, 70): c.glow(c.ell(x, 80, 3.2), P['glow'], halo=7, power=.8)
                for p in ([(20, 70), (34, 56), (44, 70)], [(56, 70), (66, 54), (80, 70)]): c.flat(c.line(p, 1.6, smooth=True), dark(col_, .5), .7, clip=band)

# ---- shoulders ----
def g_shoulder(c, P, fam, v, glow):
    m = P['metal']
    if fam == 'shoulder.mantle':
        base = {'cloth': P['cloth'], 'fur': P['leather'], 'hide': P['leather'], 'frayed': P['cloth'], 'shawl': P['cloth']}[v]
        out = [(50, 20), (72, 16), (92, 40), (94, 74), (72, 84), (50, 92), (28, 84), (6, 74), (8, 40), (28, 16)]
        if v == 'shawl': out = [(50, 18), (74, 16), (94, 44), (80, 66), (50, 98), (20, 66), (6, 44), (26, 16)]
        body = c.poly(out, smooth=True); c.part(body, base, 'soft', grain=.35 if v in ('fur', 'hide') else 0)
        if v == 'frayed':
            for x in range(14, 90, 10): c.part(c.taper([(x, 78), (x + 2, 88), (x - 1, 97)], 4, 1), dark(base, .1), 'soft')
        if v == 'shawl':
            for x in range(26, 78, 7): c.flat(c.line([(x, 70 + abs(50 - x) * -.55 + 22), (x, 78 + abs(50 - x) * -.55 + 22)], 1.6), P['trim'], .9)
            c.flat(c.line([(12, 44), (50, 76), (88, 44)], 2.4, smooth=True), P['cloth2'], .9, clip=body)
        if v in ('cloth', 'frayed'):
            for p in ([(34, 36), (22, 56), (20, 78)], [(50, 40), (50, 62), (50, 88)], [(66, 36), (78, 56), (80, 78)]): c.flat(c.line(p, 2, smooth=True), dark(base, .5), .6, clip=body)
            if v == 'cloth': c.part(c.sub(body, c.poly([(0, 0), (100, 0), (100, 70), (72, 78), (50, 84), (28, 78), (0, 70)])), P['trim'], 'matte', ink=False, alpha=.9)
        neck = c.ell(50, 26, 17, 11); c.part(neck, '#17131a', 'matte')
        if v in ('fur', 'hide'):
            ruff = c.sub(c.blob(50, 28, 30, 20, n=14, jit=.14, smooth=True), neck)
            c.part(ruff, lite(base, .35) if v == 'fur' else dark(base, .15), 'soft', grain=.6)
            if v == 'hide':
                for x, y in ((24, 60), (40, 70), (62, 70), (78, 58), (50, 80)): c.flat(c.ell(x, y, 3.2, 2.6), '#f0e6cf', .9, clip=body)
        else:
            c.part(c.sub(c.grow(neck, 5), neck), P['trim'] if v != 'frayed' else dark(base, .2), 'matte')
            c.part(c.ell(50, 40, 4.5), P['trim'], 'metal')
    elif fam == 'shoulder.pauldron':
        col_ = P['bone'] if v == 'bone' else m; mat = 'soft' if v == 'bone' else 'metal'
        for i, (y, w) in enumerate(((78, 30), (66, 36))): c.part(c.AND(c.ell(52, y - 10, w, 20), c.rect(0, y - 8, 100, 100)), dark(col_, .12 + .08 * (1 - i)), mat)
        dome = c.AND(c.ell(52, 56, 42, 44), c.rect(0, 0, 100, 60)); c.part(dome, col_, mat)
        c.part(c.rect(8, 54, 96, 62, 3), P['trim'] if v != 'bone' else dark(col_, .25), mat)
        if v == 'bone':
            for x, r in ((30, -20), (52, 0), (74, 20)): c.part(c.taper([(x, 30), (x + r * .2, 16), (x + r * .4, 2)], 9, .5), col_, 'soft')
        else: _rivets(c, [(22, 58), (42, 58), (62, 58), (82, 58)], m); c.shine(c.ell(34, 30, 9, 5, rot=-30), .55)
    else:   # spaulder
        col_ = {'lames': m, 'glass': mix(P['glow'], '#e6f4ff', .4), 'bark': P['wood']}[v]; mat = {'lames': 'metal', 'glass': 'gem', 'bark': 'wood'}[v]
        if v == 'glass': c.glow(c.ell(50, 52, 34, 28), P['glow'], halo=14, power=.4)
        for i, y in enumerate((74, 58, 42)):
            w = 30 + i * 6
            c.part(c.AND(c.ell(50, y - 6, w, 26), c.rect(0, y - 14, 100, 100)), dark(col_, .22 - .1 * i) if v != 'glass' else lite(col_, .1 * i), mat, grain=.4 if v == 'bark' else 0)
        top = c.AND(c.ell(50, 42, 42, 34), c.rect(0, 0, 100, 40)); c.part(top, col_, mat, grain=.4 if v == 'bark' else 0)
        if v == 'lames': _rivets(c, [(18, 44), (82, 44), (24, 62), (76, 62)], m)
        if v == 'bark': c.part(c.taper([(60, 14), (74, 4), (88, 6)], 8, .5), '#6dbb4e', 'matte')
        c.shine(c.ell(34, 22, 9, 4, rot=-28), .5)

# ---- chest ----
def _torso(c, sleeves=1, long=False):
    by = 98 if long else 92
    pts = [(38, 10), (50, 16), (62, 10), (78, 16)]
    if sleeves == 2: pts += [(96, 40), (90, 70), (76, 66), (74, 44)]         # long, wide sleeves
    elif sleeves == 1: pts += [(94, 36), (82, 46), (74, 36)]
    else: pts += [(74, 34)]
    pts += [(72, 60), (76 if long else 72, by), (24 if long else 28, by), (28, 60)]
    if sleeves == 2: pts += [(26, 44), (24, 66), (10, 70), (4, 40)]
    elif sleeves == 1: pts += [(26, 36), (18, 46), (6, 36)]
    else: pts += [(26, 34)]
    pts += [(22, 16)]
    return c.poly(pts)

def g_chest(c, P, fam, v, glow):
    m = P['metal']; C = P['cloth']; L = P['leather']; T = P['trim']
    if fam == 'chest.tunic':
        b = _torso(c, 1); c.part(b, C, 'soft')
        c.part(c.poly([(38, 10), (50, 16), (62, 10), (58, 22), (50, 34), (42, 22)]), dark(C, .5), 'matte', ink=False, clip=b)
        c.part(c.rect(24, 62, 76, 70), L, 'matte', clip=b); c.part(c.rect(45, 60, 55, 72, 1), T, 'metal')
        c.part(c.rect(24, 86, 76, 94), P['cloth2'], 'matte', clip=b)
    elif fam == 'chest.jerkin':
        b = _torso(c, 0); base = m if v == 'scale' else L
        c.part(b, base, 'metal' if v == 'scale' else 'soft', light=.6 if v == 'scale' else 1)
        if v == 'scale':
            for y in range(24, 94, 8):
                for x in range(24 + (y // 8 % 2) * 4, 80, 8): c.part(c.AND(c.ell(x, y, 4.6, 5.4), c.rect(0, y, 100, 100)), lite(m, .12), 'metal', clip=b)
            c.part(c.poly([(38, 10), (50, 16), (62, 10), (58, 20), (50, 28), (42, 20)]), L, 'matte', clip=b)
        else:
            c.part(c.poly([(38, 10), (50, 16), (62, 10), (56, 28), (50, 60), (44, 28)]), dark(L, .55), 'matte', ink=False, clip=b)
            for y in (32, 40, 48, 56): c.flat(c.line([(45, y), (55, y + 3)], 1.5), '#e6dcc0', .9); c.flat(c.line([(55, y), (45, y + 3)], 1.5), '#e6dcc0', .9)
            if v == 'hide':
                for p in ((24, 16, 38, 12), (62, 12, 76, 16)): c.part(c.blob((p[0] + p[2]) / 2, 16, 10, 6, n=9, jit=.25, smooth=True), lite(L, .35), 'soft', grain=.6)
        c.part(c.rect(24, 66, 76, 74), dark(L, .25) if v != 'scale' else L, 'matte', clip=b); c.part(c.rect(45, 64, 55, 76, 1), T, 'metal')
    elif fam == 'chest.hauberk':
        b = _torso(c, 1); c.part(b, m, 'metal', light=.55)
        for y in range(14, 96, 5):
            for x in range(6 + (y // 5 % 2) * 2, 96, 5): c.flat(c.ell(x, y, 1.5), dark(m, .62), .6, clip=b)
        c.part(c.poly([(38, 10), (50, 16), (62, 10), (60, 20), (50, 26), (40, 20)]), L, 'matte', clip=b)
        c.part(c.rect(24, 62, 76, 70), L, 'matte', clip=b); c.part(c.rect(45, 60, 55, 72, 1), T, 'metal')
        for x in range(28, 74, 8): c.part(c.poly([(x, 92), (x + 4, 99), (x + 8, 92)]), m, 'metal')
    elif fam == 'chest.coat':
        b = _torso(c, 2, long=True); c.part(b, C, 'soft')
        for sx in (-1, 1): c.part(c.poly([(50, 16), (50 + sx * 12, 10), (50 + sx * 16, 34), (50, 50)]), P['cloth2'], 'matte', clip=b)
        c.flat(c.line([(50, 50), (50, 98)], 1.8), dark(C, .6), .85)
        for y in (58, 70, 82): c.part(c.ell(54, y, 2.6), T, 'metal')
        for sx in (-1, 1): c.part(c.poly([(50 + sx * 30, 60), (50 + sx * 46, 62), (50 + sx * 45, 71), (50 + sx * 29, 68)]), T, 'matte', clip=b)
        if v == 'skirted': c.part(c.rect(20, 62, 80, 69), L, 'matte', clip=b); c.part(c.poly([(50, 70), (38, 100), (62, 100)]), dark(C, .35), 'matte', ink=False, clip=b)
    elif fam == 'chest.cuirass':
        base = P['wood'] if v == 'bark' else m; mat = 'wood' if v == 'bark' else 'metal'
        b = c.poly([(34, 10), (50, 20), (66, 10), (84, 20), (78, 40), (76, 66), (70, 90), (30, 90), (24, 66), (22, 40), (16, 20)], smooth=False)
        c.part(b, base, mat, grain=.45 if v == 'bark' else 0)
        for sx in (-1, 1): c.part(c.ell(50 + sx * 14, 40, 15, 14), lite(base, .1), mat, clip=b, grain=.3 if v == 'bark' else 0)
        c.part(c.poly([(48, 22), (52, 22), (53, 88), (47, 88)]), lite(base, .3), mat, ink=False, clip=b)
        c.part(c.rect(22, 70, 78, 78), dark(base, .2), mat, clip=b)
        if v == 'bark':
            c.part(c.taper([(66, 18), (80, 8), (92, 12)], 9, .5), '#6dbb4e', 'matte')
            for p in ([(30, 30), (36, 50), (30, 64)], [(70, 30), (64, 52), (70, 66)]): c.flat(c.line(p, 1.6, smooth=True), dark(base, .6), .8, clip=b)
        else: c.part(c.poly([(34, 10), (50, 20), (66, 10), (62, 4), (38, 4)]), T, 'metal'); c.shine(c.ell(34, 34, 5, 9, rot=20), .5)
    else:   # robe
        col_ = C
        b = _torso(c, 2, long=True); c.part(b, col_, 'soft')
        c.part(c.poly([(38, 10), (50, 16), (62, 10), (57, 30), (50, 98), (43, 30)]), P['cloth2'] if v != 'shroud' else lite(col_, .25), 'matte', clip=b)
        hood = c.poly([(30, 14), (50, 2), (70, 14), (62, 26), (50, 20), (38, 26)], smooth=True); c.part(hood, dark(col_, .15), 'soft')
        if v == 'vestment':
            c.glow(c.U_(c.rect(48.5, 40, 51.5, 70), c.rect(43, 48, 57, 51)), P['glow'], halo=6, power=.7)
        elif v == 'cassock':
            for y in range(36, 96, 9): c.part(c.ell(50, y, 2.2), T, 'metal')
            c.part(c.rect(22, 60, 78, 66), '#1a1418', 'matte', clip=b)
        else:
            for x in (16, 30, 44, 58, 72): c.part(c.taper([(x, 90), (x + 3, 96), (x + 1, 104)], 6, 1), col_, 'soft')

# ---- hands ----
def g_hands(c, P, fam, v, glow):
    m = P['metal']; L = P['leather']
    c.push(rot=-10)
    def hand(colr, mat, tips=None, mitt=False):
        palm = c.rect(28, 40, 70, 72, 6)
        if mitt: fingers = c.rect(28, 8, 70, 50, 16)
        else: fingers = c.U_(*[c.line([(x, 46), (x, top)], 9.6) for x, top in ((34, 20), (44.5, 11), (55, 13), (65, 22))])
        thumb = c.line([(70, 60), (82, 50), (86, 36)], 10.5)
        c.part(c.U_(palm, fingers, thumb), colr, mat)
        if not mitt:
            for x in (39.2, 49.7, 60): c.flat(c.line([(x, 22), (x, 46)], 1.4), dark(colr, .65), .85)
        if tips:
            for x, top in ((34, 20), (44.5, 11), (55, 13), (65, 22)): c.part(c.line([(x, top + 6), (x, top)], 9.6), tips, 'soft')
            c.part(c.line([(84.5, 42), (86, 36)], 10.5), tips, 'soft')
    if fam == 'hands.gloves':
        c.part(c.poly([(24, 66), (74, 66), (80, 96), (18, 96)]), dark(L, .12), 'matte')
        hand(L, 'soft', tips='#e3b48e' if v == 'fingerless' else None, mitt=(v == 'mitts'))
        c.part(c.rect(22, 66, 76, 74, 2), P['trim'], 'matte')
        if v == 'mitts': c.part(c.rect(18, 84, 80, 97, 4), '#efe6d2', 'soft', grain=.5)
    elif fam == 'hands.wraps':
        col_ = P['cloth']
        c.part(c.poly([(26, 66), (72, 66), (74, 96), (24, 96)]), col_, 'soft')
        hand(col_, 'gem' if v == 'silk' else 'soft', tips='#e3b48e')
        for y in (34, 44, 54, 64, 76, 88): c.flat(c.line([(26, y + 4), (72, y - 4)], 1.8), dark(col_, .55), .8)
        if v == 'fur': c.part(c.blob(49, 90, 30, 9, n=14, jit=.2, smooth=True), lite(P['leather'], .4), 'soft', grain=.6)
    else:
        col_ = {'plate': m, 'bone': P['bone'], 'glass': mix(P['glow'], '#e6f4ff', .4), 'root': P['wood']}[v]; mat = {'plate': 'metal', 'bone': 'soft', 'glass': 'gem', 'root': 'wood'}[v]
        if v == 'glass': c.glow(c.ell(50, 50, 30, 36), P['glow'], halo=14, power=.4)
        hand(col_, mat)
        for y in (40, 50): c.flat(c.line([(29, y), (69, y)], 1.5), dark(col_, .6), .8)
        c.part(c.poly([(24, 62), (74, 62), (86, 98), (12, 98)]), col_, mat, grain=.4 if v == 'root' else 0)
        c.part(c.rect(22, 60, 76, 68, 2), P['trim'] if v == 'plate' else dark(col_, .25), mat)
        if v == 'plate': _rivets(c, [(30, 82), (50, 86), (70, 82)], m)
        if v == 'root':
            for p in ([(20, 92), (40, 76), (60, 84), (78, 70)],): c.part(c.line(p, 3, smooth=True), dark(col_, .25), 'wood')
            c.part(c.taper([(70, 66), (84, 62), (92, 68)], 8, .5), '#6dbb4e', 'matte')
    c.pop()

# ---- legs ----
def g_legs(c, P, fam, v, glow):
    m = P['metal']; C = P['cloth']; L = P['leather']
    if fam == 'legs.kilt':
        b = P['bone']
        c.part(c.poly([(22, 20), (78, 20), (90, 86), (10, 86)]), L, 'soft')
        for i, x in enumerate((16, 31, 46, 61, 76)): c.part(c.poly([(x + 4, 26), (x + 16, 26), (x + 18 + (i - 2) * 2, 90), (x + 10 + (i - 2) * 2, 98), (x + 2 + (i - 2) * 2, 90)]), b, 'soft')
        c.part(c.rect(20, 12, 80, 26, 3), dark(L, .2), 'matte'); c.part(c.rect(44, 10, 56, 28, 2), P['trim'], 'metal')
        return
    long_ = fam != 'legs.breeches'
    by = 96 if long_ else 80
    legs = c.poly([(22, 10), (78, 10), (82, 40), (78 if long_ else 80, by), (56, by), (50, 46), (44, by), (22 if long_ else 20, by), (18, 40)])
    if fam == 'legs.greaves':
        c.part(legs, dark(C, .25), 'soft')
        for sx in (-1, 1):
            x = 50 + sx * 17
            c.part(c.poly([(x - 11, 50), (x + 11, 50), (x + 9, 96), (x - 9, 96)]), m, 'metal'); c.part(c.ell(x, 50, 11.5, 9.5), lite(m, .15), 'metal')
            c.part(c.rect(x - 1.5, 58, x + 1.5, 96), lite(m, .3), 'metal', ink=False)
            if v == 'full': c.part(c.poly([(x - 13, 20), (x + 13, 20), (x + 11, 44), (x - 11, 44)]), m, 'metal')
        c.part(c.rect(20, 8, 80, 18, 2), L, 'matte'); c.part(c.rect(45, 6, 55, 20, 1), P['trim'], 'metal')
    else:
        base = L if (fam == 'legs.leggings' and v == 'hide') else C
        c.part(legs, base, 'soft', grain=.4 if base is L else 0)
        c.flat(c.line([(50, 14), (50, 46)], 1.6), dark(base, .6), .8)
        c.part(c.rect(20, 8, 80, 18, 2), L if base is not L else dark(L, .25), 'matte'); c.part(c.rect(45, 6, 55, 20, 1), P['trim'], 'metal')
        if fam == 'legs.breeches':
            for sx in (-1, 1): c.part(c.rect(50 + sx * 17 - 14, 74, 50 + sx * 17 + 14, 82, 3), P['cloth2'], 'matte')
            if v == 'patched':
                c.part(c.rect(58, 40, 72, 54, 1), P['cloth2'], 'matte'); c.part(c.rect(26, 54, 37, 65, 1), lite(C, .25), 'matte')
        else:
            for sx in (-1, 1):
                x = 50 + sx * 17
                if v == 'garter':
                    for y in (56, 70): c.flat(c.line([(x - 12, y - 4), (x + 12, y + 4)], 2.2), P['trim'], .95); c.flat(c.line([(x + 12, y - 4), (x - 12, y + 4)], 2.2), P['trim'], .95)
                else:
                    for y in (40, 58, 76): c.flat(c.line([(x - 2, y), (x + 2, y + 8)], 1.8), '#eadfc4', .9)

# ---- feet (one boot, side on, toe to the right) ----
def g_feet(c, P, fam, v, glow):
    m = P['metal']; L = P['leather']
    if fam == 'feet.sabatons':
        b = c.poly([(26, 10), (54, 10), (56, 52), (74, 62), (96, 78), (94, 88), (22, 88), (24, 52)])
        c.part(b, m, 'metal'); c.part(c.rect(18, 86, 96, 93, 2), dark(m, .4), 'metal')
        for x0, x1 in ((58, 54), (68, 66), (78, 78)): c.flat(c.line([(x0, 56 + (x0 - 58) * .5), (x1, 88)], 1.6), dark(m, .6), .85, clip=b)
        for y in (26, 40): c.flat(c.line([(25, y), (55, y)], 1.6), dark(m, .6), .85)
        c.part(c.ell(40, 52, 8, 7), lite(m, .15), 'metal'); c.part(c.rect(22, 6, 58, 14, 2), P['trim'], 'metal'); c.shine(c.ell(32, 30, 3, 9), .5)
        return
    top = {'feet.shoes': 44, 'feet.boots': 12}[fam]
    if v == 'waders': top = 2
    if fam == 'feet.shoes': c.push(sc=1.3, cx=55, cy=92)
    b = c.poly([(26, top), (54, top), (56, 54), (74, 60), (90, 72), (92, 86), (22, 86), (24, 54)], smooth=False)
    b = c.U_(b, c.ell(80, 78, 13, 10))
    c.part(b, L if v != 'waders' else mix(L, '#3a4a3a', .3), 'soft' if v != 'waders' else 'gem')
    c.part(c.U_(c.rect(20, 84, 94, 92, 2), c.rect(20, 84, 38, 96, 1)), dark(L, .55), 'matte')
    if fam == 'feet.shoes':
        c.part(c.rect(23, top - 3, 57, top + 6, 3), P['cloth'], 'matte')
        for x in (58, 66): c.flat(c.line([(x - 4, 58 + (x - 58) * .4), (x + 4, 52 + (x - 58) * .4)], 1.6), '#e6dcc0', .9)
        c.pop()
    else:
        c.part(c.rect(22, top - 2, 58, top + 10, 3), P['trim'] if v != 'waders' else dark(L, .1), 'matte')
        c.flat(c.line([(30, 56), (52, 56)], 1.5), dark(L, .6), .7)
        if v == 'waders': c.part(c.rect(24, 30, 56, 36, 1), P['trim'], 'matte'); c.shine(c.ell(33, 60, 3, 16), .3)
        if v == 'hobnail':
            for x in range(26, 92, 9): c.part(c.ell(x, 93, 2.6), lite(m, .2), 'metal')
            c.part(c.rect(24, 28, 56, 33, 1), m, 'metal')
        if v == 'root':
            for p in ([(22, 70), (40, 60), (56, 68), (80, 62)], [(24, 34), (40, 28), (58, 36)]): c.part(c.line(p, 3.2, smooth=True), P['wood'], 'wood')
            c.part(c.taper([(52, 20), (66, 12), (78, 16)], 8, .5), '#6dbb4e', 'matte')

# ---- neck ----
def _cordloop(c, colr, w=3, y=44, metal=False):
    c.part(c.sub(c.ell(50, 32, 31, 29), c.ell(50, 32, 31 - w, 29 - w)), colr, 'metal' if metal else 'matte')

def g_neck(c, P, fam, v, glow):
    m = P['metal']; T = P['trim']; G = P['glow']
    if fam == 'neck.torc':
        col_ = {'metal': T, 'band': m, 'wood': P['wood'], 'charm': T}[v]; mat = 'wood' if v == 'wood' else 'metal'
        ring = c.sub(c.ell(50, 48, 38, 36), c.ell(50, 48, 27, 25))
        if v != 'band': ring = c.sub(ring, c.poly([(38, 60), (62, 60), (70, 100), (30, 100)]))
        c.part(ring, col_, mat, grain=.4 if v == 'wood' else 0)
        if v != 'band':
            for sx in (-1, 1): c.part(c.ell(50 + sx * 17, 80, 7.5), lite(col_, .15) if v != 'wood' else T, 'metal')
        if v == 'wood':
            for a in range(200, 350, 25): c.flat(c.line([(50 + 28 * math.cos(math.radians(a)), 48 + 26 * math.sin(math.radians(a))), (50 + 37 * math.cos(math.radians(a + 8)), 48 + 35 * math.sin(math.radians(a + 8)))], 1.6), dark(col_, .6), .8)
        if v == 'charm':
            c.flat(c.line([(50, 14), (50, 40)], 1.6), '#d9c9a0', .95)
            c.part(c.taper([(50, 38), (48, 56), (52, 72)], 9, 13), '#c9b08a', 'soft', grain=.6); c.part(c.rect(44, 36, 56, 44, 2), T, 'metal')
        if v == 'band': c.flat(c.line([(30, 30), (36, 24)], 1.5), dark(col_, .6), .8)
        c.shine(c.ell(28, 28, 3, 8, rot=40), .5)
        return
    cord = '#b9955e' if fam == 'neck.cord' else lite(m, .1)
    _cordloop(c, cord, 3.4 if fam == 'neck.cord' else 2.8, metal=fam != 'neck.cord')
    if not (fam == 'neck.cord' and v == 'beads'): c.push(sc=1.32, cx=50, cy=100)
    if fam == 'neck.cord':
        if v == 'beads':
            for a in range(20, 161, 20):
                x, y = 50 + 29.5 * math.cos(math.radians(a)), 32 + 27.5 * math.sin(math.radians(a))
                c.part(c.ell(x, y, 5.2 if a != 80 and a != 100 else 6.4), [P['bone'], '#b94a3a', '#4a6a8a'][a // 20 % 3], 'gem')
        elif v == 'knot':
            for p in ([(40, 60), (60, 74), (50, 86), (40, 74), (60, 60)],): c.part(c.line(p, 5.5, smooth=True), '#e2d2a8', 'matte')
            for sx in (-1, 1): c.part(c.taper([(50, 84), (50 + sx * 7, 92), (50 + sx * 9, 99)], 4, 2), '#e2d2a8', 'matte')
        elif v == 'tine':
            c.part(c.taper([(50, 58), (54, 76), (46, 98)], 12, 1), '#a8845e', 'soft'); c.part(c.taper([(52, 70), (64, 74), (70, 86)], 7, 1), '#a8845e', 'soft'); c.part(c.rect(43, 56, 57, 64, 2), T, 'metal')
        else:
            big = v == 'tooth'
            c.part(c.taper([(50, 58), (51, 76), (44 if big else 47, 99)], 20 if big else 13, .5), P['bone'], 'soft')
            c.part(c.rect(41 if big else 44, 54, 59 if big else 56, 63, 2), T if big else '#8a5a3a', 'metal' if big else 'matte')
            if not big:
                for sx in (-1, 1): c.part(c.ell(50 + sx * 15, 58, 4), '#8a5a3a', 'gem')
    else:   # pendant
        if v in ('vial', 'jar'):
            body = c.rect(39, 58, 61, 96, 6) if v == 'vial' else c.U_(c.rect(34, 66, 66, 96, 8), c.rect(41, 58, 59, 70, 2))
            c.glow(c.grow(body, -3), G, core=lite(G, .6), halo=11, power=.7); c.part(c.sub(body, c.grow(body, -3)), '#cfe6ee', 'gem')
            c.part(c.rect(42, 52, 58, 60, 2), '#9a6a3a' if v == 'vial' else T, 'matte'); c.shine(c.line([(43, 68), (43, 86)], 2), .6)
        elif v == 'seal':
            c.part(c.blob(50, 76, 20, 19, n=12, jit=.08, smooth=True), '#b23a30', 'gem'); c.part(c.ell(50, 76, 12), '#c8483c', 'gem', ink=False)
            c.flat(c.star(50, 76, 8, 3.5, 6), dark('#b23a30', .5), .9)
        elif v == 'signet':
            c.part(c.sub(c.ell(50, 80, 16, 15), c.ell(50, 80, 9.5, 8.5)), T, 'metal'); c.part(c.rect(39, 58, 61, 72, 3), T, 'metal'); c.part(c.rect(43, 61, 57, 69, 1), '#7a2a2a', 'gem')
        elif v == 'glass':
            c.glow(c.ell(50, 76, 13), G, core=lite(G, .7), halo=11, power=.6); c.part(c.sub(c.ell(50, 76, 19), c.ell(50, 76, 13)), T, 'metal'); c.shine(c.ell(45, 71, 4, 2.5, rot=-30), .8)
        elif v == 'ember':
            c.part(c.ell(50, 78, 18, 17), dark(m, .2), 'metal'); c.glow(c.poly([(50, 62), (62, 78), (50, 94), (38, 78)]), G, core='#fff0c0', halo=14, power=1.0)
        elif v == 'leaf':
            lf = c.taper([(50, 56), (52, 76), (48, 99)], 2, 1, wmid=25); c.glow(lf, G, halo=12, power=.35)
            c.part(lf, '#58c04a', 'gem'); c.flat(c.line([(50, 60), (51, 78), (48.5, 95)], 1.4), dark('#58c04a', .5), .8)
        else:
            gem = c.poly([(50, 56), (63, 78), (50, 98), (37, 78)], smooth=True)
            if glow: c.glow(gem, G, halo=11, power=.5)
            c.part(gem, mix(G, P['cloth2'], .5) if not glow else G, 'gem'); c.shine(c.ell(46, 72, 3, 5, rot=20), .7); c.part(c.rect(46, 52, 54, 60, 2), T, 'metal')
    if len(c.M) > 1: c.pop()

def draw_gear(c, P, family, variant, glow=False):
    f = family
    if f.startswith('sword.'): g_sword(c, P, f, variant, glow)
    elif f == 'knife': g_knife(c, P, variant, glow)
    elif f.startswith('axe.'): g_axe(c, P, f, variant, glow)
    elif f == 'cleaver': g_cleaver(c, P, variant, glow)
    elif f == 'club': g_club(c, P, variant, glow)
    elif f in ('mace.flanged', 'hammer.war', 'mace.root'): g_mace(c, P, f, variant, glow)
    elif f == 'polearm': g_polearm(c, P, variant, glow)
    elif f == 'staff': g_staff(c, P, variant, glow)
    elif f.startswith('shield.'): g_shield(c, P, f, variant, glow)
    elif f == 'offhand.hung': g_hung(c, P, variant, glow)
    elif f.startswith('head.'): g_head(c, P, f, variant, glow)
    elif f.startswith('shoulder.'): g_shoulder(c, P, f, variant, glow)
    elif f.startswith('chest.'): g_chest(c, P, f, variant, glow)
    elif f.startswith('hands.'): g_hands(c, P, f, variant, glow)
    elif f.startswith('legs.'): g_legs(c, P, f, variant, glow)
    elif f.startswith('feet.'): g_feet(c, P, f, variant, glow)
    elif f.startswith('neck.'): g_neck(c, P, f, variant, glow)
    else: raise KeyError(f)
    if glow and f.split('.')[0] in ('sword', 'axe', 'cleaver', 'club', 'polearm', 'head', 'shoulder', 'chest', 'hands', 'legs', 'feet'):
        c.spark(74, 24, 7, P['glow'], .8)

FAMILIES = {
    'sword.short': ('plain', 'notched'), 'sword.arming': ('straight', 'curved', 'disc'), 'sword.falchion': ('clipped', 'heavy'), 'sword.sabre': ('officer', 'broken'),
    'sword.leaf': ('bronze', 'root'), 'sword.great': ('steel', 'wood', 'living'), 'knife': ('hooked', 'needle', 'glass', 'sickle'), 'axe.hand': ('wedge', 'hatchet'),
    'axe.bearded': ('plain', 'hooked'), 'axe.crescent': ('plain', 'spiked'), 'cleaver': ('slab', 'notched'), 'club': ('plain', 'studded', 'bound', 'tusk', 'tankard'),
    'mace.flanged': ('six', 'fist'), 'hammer.war': ('pick', 'maul'), 'mace.root': ('burl', 'antler', 'briar'), 'polearm': ('spear', 'harpoon', 'fork', 'billhook', 'spade'),
    'staff': ('knob', 'crook', 'forked', 'skull'), 'shield.buckler': ('plain', 'tusk', 'chitin'), 'shield.round': ('boards', 'hide', 'cask', 'lid'),
    'shield.heater': ('plain', 'striped', 'glass'), 'shield.kite': ('plain', 'slab'), 'shield.leaf': ('bronze', 'bark'), 'offhand.hung': ('lantern', 'shuttered', 'moss', 'censer', 'scale'),
    'head.cap': ('plain', 'flaps', 'leaf'), 'head.hood': ('cloth', 'oilskin'), 'head.coif': ('mail',), 'head.wrap': ('scarf',), 'head.kettle': ('plain', 'half'),
    'head.barbute': ('plain', 'rimed'), 'head.mask': ('bone', 'tear'), 'head.circlet': ('band', 'briar'), 'head.crown': ('tin', 'root', 'antler'),
    'shoulder.mantle': ('cloth', 'fur', 'hide', 'frayed', 'shawl'), 'shoulder.pauldron': ('dome', 'bone'), 'shoulder.spaulder': ('lames', 'glass', 'bark'),
    'chest.tunic': ('plain',), 'chest.jerkin': ('leather', 'hide', 'scale'), 'chest.hauberk': ('mail',), 'chest.coat': ('grey', 'skirted'), 'chest.cuirass': ('plate', 'bark'),
    'chest.robe': ('vestment', 'cassock', 'shroud'), 'hands.gloves': ('plain', 'fingerless', 'mitts'), 'hands.wraps': ('cloth', 'fur', 'silk'),
    'hands.gauntlets': ('plate', 'bone', 'glass', 'root'), 'legs.breeches': ('plain', 'patched'), 'legs.leggings': ('garter', 'hide'), 'legs.greaves': ('plate', 'full'),
    'legs.kilt': ('bone',), 'feet.shoes': ('plain',), 'feet.boots': ('plain', 'waders', 'hobnail', 'root'), 'feet.sabatons': ('plate',),
    'neck.pendant': ('drop', 'glass', 'seal', 'signet', 'ember', 'leaf', 'vial', 'jar'), 'neck.cord': ('beads', 'knot', 'fang', 'tooth', 'tine'), 'neck.torc': ('metal', 'band', 'wood', 'charm'),
}

# ====================================================================================================
# lib3.py
# ====================================================================================================
# Drawers, batch 3: abilities (the action bar) and talent emblems.

STEEL = dict(cloth='#3c5f96', cloth2='#3c5f96', leather='#6a4a30', metal='#b9c2cc', trim='#e0b040', wood='#8a5a30', glow='#ffd98a')
def _P(): return mkpal(STEEL)

def rays(c, cx, cy, r0, r1, n, color, power=.5, w=7, rot=0):
    for i in range(n):
        a = math.radians(rot + 360 * i / n)
        c.back(c.taper([(cx + r0 * math.cos(a), cy + r0 * math.sin(a)), (cx + r1 * math.cos(a), cy + r1 * math.sin(a))], w, .5, smooth=False), color, power, 2)
def rings(c, cx, cy, radii, color, power=.5, a0=0, a1=360, w=3):
    for r in radii:
        m = c.sub(c.ell(cx, cy, r, r, a0=a0, a1=a1), c.ell(cx, cy, r - w, r - w)); c.back(m, color, power, 2)
def slash(c, pts, w, color, core='#fff6d8', power=.8):
    c.glow(c.taper(pts, .5, .5, wmid=w), color, core=core, halo=8, power=power)
def leafshape(c, x0, y0, x1, y1, w, color, mat='matte', vein=True, **k):
    mx, my = (x0 + x1) / 2, (y0 + y1) / 2; nx, ny = -(y1 - y0), (x1 - x0); l = math.hypot(nx, ny) or 1
    m = c.taper([(x0, y0), (mx + nx / l * w * .12, my + ny / l * w * .12), (x1, y1)], 1.5, .5, wmid=w)
    c.part(m, color, mat, **k)
    if vein: c.flat(c.line([(x0, y0), (mx + nx / l * w * .12, my + ny / l * w * .12), (x1, y1)], 1.2, smooth=True), dark(color, .5), .7, clip=m)
    return m
def heart(c, cx, cy, s):
    return c.poly([(cx, cy + s * .95), (cx - s * .95, cy - s * .05), (cx - s * .9, cy - s * .65), (cx - s * .45, cy - s * .9), (cx, cy - s * .45),
                   (cx + s * .45, cy - s * .9), (cx + s * .9, cy - s * .65), (cx + s * .95, cy - s * .05)], smooth=True, n=6)
def pawprint(c, cx, cy, s, color, mat='soft', claws=None, clawlen=14):
    c.part(c.poly([(cx, cy - s * .2), (cx + s * .62, cy + s * .2), (cx + s * .5, cy + s * .85), (cx, cy + s * .7), (cx - s * .5, cy + s * .85), (cx - s * .62, cy + s * .2)], smooth=True), color, mat)
    for dx, dy, r in ((-.78, -.38, .26), (-.3, -.82, .29), (.3, -.82, .29), (.78, -.38, .26)):
        x, y = cx + dx * s, cy + dy * s
        if claws: c.part(c.taper([(x, y - r * s * .4), (x + dx * 3, y - r * s - clawlen)], r * s * 1.1, .5, smooth=False), claws, 'soft')
        c.part(c.ell(x, y, r * s, r * s * 1.15, rot=dx * 25), color, mat)
def thornvine(c, pts, w, color='#4f8a3a', thorn='#2f5a2a', every=3, tl=7):
    sm = _smooth(pts, False, 6) if True else pts
    for i in range(1, len(sm) - 1, every):
        a = np.array(sm[i - 1]); b = np.array(sm[i + 1]); d = b - a; l = float(np.hypot(*d)) or 1; n = np.array([-d[1], d[0]]) / l * (1 if (i // every) % 2 else -1)
        p = np.array(sm[i]); c.part(c.poly([tuple(p - d / l * w * .6), tuple(p + n * tl), tuple(p + d / l * w * .6)]), thorn, 'matte')
    c.part(c.line(sm, w), color, 'matte')

# ---- warrior ----
def a_strike(c):
    c.back(c.taper([(8, 66), (26, 28), (60, 8), (92, 10)], 1, 1, wmid=26), '#ff7a2a', .55, 5)
    g_sword(c, _P(), 'sword.arming', 'straight', False)
    slash(c, [(8, 50), (26, 22), (54, 8), (84, 8)], 7, '#ffb040')
def a_challenge(c):
    rings(c, 74, 30, (24, 34, 44), '#ff7a4a', .8, a0=-150, a1=-30, w=4.5)
    horn = c.taper([(8, 74), (34, 88), (64, 74), (74, 34)], 5, 34)
    c.part(horn, '#e9dcc0', 'soft')
    for t, w in ((.3, 12), (.62, 19)):
        pass
    c.part(c.taper([(28, 86), (34, 88), (40, 87)], 16, 16), '#e0b040', 'metal', clip=c.grow(horn, 2))
    c.part(c.taper([(60, 78), (65, 72), (68, 66)], 30, 30), '#e0b040', 'metal', clip=c.grow(horn, 2))
    c.part(c.ell(74, 32, 19, 9, rot=-8), '#e0b040', 'metal'); c.part(c.ell(74, 32, 14, 5.5, rot=-8), '#2a1612', 'matte', ink=False)
    c.part(c.ell(8, 74, 5), '#e0b040', 'metal')
def a_guard(c):
    d = c.poly([(16, 12), (50, 16), (84, 12), (86, 46), (74, 74), (50, 96), (26, 74), (14, 46)], smooth=True, n=6)
    c.back(c.grow(d, 5), '#6fb6ff', .7, 9)
    c.part(d, '#c3ccd6', 'metal'); inner = c.grow(d, -9); c.part(inner, '#3466b0', 'matte')
    c.part(c.poly([(10, 30), (50, 54), (90, 30), (90, 44), (50, 68), (10, 44)]), '#e0b040', 'metal', clip=inner)
    c.part(c.ell(50, 36, 7), '#e0b040', 'metal'); c.shine(c.ell(32, 30, 4, 10, rot=20), .45, clip=inner)
def a_intercept(c):
    for y, x0 in ((30, 4), (50, 0), (70, 6), (86, 16)): c.back(c.taper([(x0, y), (60, y - 6)], 1, 9, smooth=False), '#7fe8ff', .6, 3)
    c.push(rot=-14, sc=.98, dx=8)
    g_feet(c, _P(), 'feet.sabatons', 'plate', False)
    c.pop()
    for y in (36, 54, 70): c.glow(c.taper([(2, y + 6), (22, y)], .5, 4, smooth=False), '#9ff0ff', halo=5, power=.5)
def a_breach(c):
    rays(c, 30, 72, 8, 46, 12, '#ffb030', .6, 9, rot=10)
    c.push(rot=-128, sc=1.1, dx=4, dy=-4)
    c.part(c.rect(45.5, 20, 54.5, 110, 3), '#8a5a30', 'wood', grain=.3)
    c.part(c.rect(24, 2, 76, 36, 4), '#b9c2cc', 'metal'); c.part(c.rect(24, 2, 34, 36, 3), dark('#b9c2cc', .2), 'metal'); c.part(c.rect(44, -2, 56, 40, 1), '#e0b040', 'metal')
    c.pop()
    c.glow(c.star(22, 80, 17, 6, 7, rot=-80), '#ffc040', core='#fffbe0', halo=9, power=.8)
    for x, y, r in ((8, 56, 3.5), (54, 92, 3), (12, 92, 2.6)): c.part(c.blob(x, y, r, n=5, jit=.3), '#d8d0c0', 'matte')
def a_muster(c):
    rays(c, 52, 40, 20, 70, 14, '#ffe08a', .35, 9)
    c.part(c.rect(22, 4, 28, 98, 2), '#8a5a30', 'wood'); c.part(c.ell(25, 6, 5.5), '#e0b040', 'metal')
    flag = c.poly([(28, 12), (56, 16), (88, 12), (84, 36), (88, 62), (72, 54), (56, 66), (28, 60)], smooth=False)
    c.part(flag, '#e2a52a', 'soft')
    c.part(c.poly([(28, 12), (88, 12), (86, 22), (28, 22)]), '#b8402a', 'matte', clip=flag)
    c.part(c.star(55, 40, 12, 5, 5), '#b8402a', 'matte', clip=flag)
    for x in (44, 68): c.flat(c.line([(x, 14), (x - 2, 36), (x, 60)], 1.6, smooth=True), dark('#e2a52a', .45), .5, clip=flag)

# ---- the companion ----
def a_weave(c):
    rays(c, 50, 50, 10, 60, 10, '#ffe9a0', .3, 8)
    for p in ([(20, 50), (36, 26), (50, 50), (64, 74), (80, 50), (64, 26), (50, 50), (36, 74), (20, 50)],):
        c.glow(c.line(p, 6, smooth=True, closed=False), '#ffcf5a', core='#fff7d8', halo=9, power=.7)
    c.spark(50, 50, 13, '#ffe9a0', 1.0)
def a_lightbolt(c):
    c.back(c.taper([(4, 96), (60, 40)], 2, 22, smooth=False), '#ffe9a0', .5, 5)
    c.glow(c.taper([(10, 90), (64, 36)], 2, 14, smooth=False), '#ffd060', core='#ffffff', halo=10, power=.8)
    c.glow(c.star(68, 32, 24, 8, 4, rot=-45), '#ffe9a0', core='#ffffff', halo=10, power=.9)

# ---- druid ----
BARK = '#8a5f38'; FUR = '#5c6e48'; LEAF = '#62b84a'; VIOLET = '#a070f0'
def a_barkhide(c):
    for sx in (-1, 1): c.part(c.ell(50 + sx * 27, 24, 12, 12), BARK, 'wood'); c.part(c.ell(50 + sx * 27, 25, 6, 6), dark(BARK, .5), 'matte', ink=False)
    head = c.ell(50, 54, 36, 34); c.part(head, BARK, 'wood', grain=.5)
    for p in ([(36, 24), (40, 36), (36, 46)], [(50, 20), (52, 32)], [(64, 24), (60, 36), (64, 44)], [(20, 56), (26, 66)], [(80, 56), (74, 66)]): c.flat(c.line(p, 1.8, smooth=len(p) > 2), dark(BARK, .65), .8, clip=head)
    c.part(c.ell(50, 68, 17, 14), lite(BARK, .28), 'soft'); c.part(c.poly([(42, 60), (58, 60), (50, 70)], smooth=True), '#1c1410', 'matte', ink=False)
    c.flat(c.line([(50, 69), (50, 76)], 1.5), '#1c1410', .9); c.flat(c.line([(42, 78), (50, 76), (58, 78)], 1.5, smooth=True), '#1c1410', .9)
    for sx in (-1, 1): c.glow(c.ell(50 + sx * 15, 46, 4.2, 3.6), '#9be05a', halo=5, power=.8)
def a_thornclaw(c):
    pawprint(c, 50, 62, 30, '#3d4a32', claws='#9be05a', clawlen=17)
    for dx, dy in ((-.78, -.38), (-.3, -.82), (.3, -.82), (.78, -.38)): pass
    c.back(c.ell(50, 50, 40), '#d0403a', .25, 12)
def a_rootmend(c):
    c.back(c.ell(50, 40, 26), '#9be05a', .5, 12)
    for p, w in (([(50, 60), (40, 76), (24, 92)], 5), ([(50, 60), (52, 80), (48, 98)], 5), ([(50, 60), (62, 76), (80, 90)], 5), ([(42, 72), (30, 74), (16, 72)], 3.4), ([(60, 72), (72, 72), (88, 74)], 3.4)):
        c.part(c.taper(p, w, 1), '#a07448', 'wood')
    c.part(c.line([(50, 62), (50, 40)], 4.5), '#4f9a3a', 'matte')
    leafshape(c, 50, 42, 22, 14, 22, LEAF, 'gem'); leafshape(c, 50, 42, 80, 16, 22, LEAF, 'gem')
    c.part(c.ell(50, 62, 10, 8), '#8a5f38', 'wood'); c.spark(50, 30, 8, '#d8ffb0', .9)
def a_thornsong(c):
    c.back(c.ell(50, 50, 34), VIOLET, .45, 14)
    pts = [(50 + (6 + t * 4.4) * math.cos(t * .9), 50 + (6 + t * 4.4) * math.sin(t * .9)) for t in range(0, 9)]
    thornvine(c, pts, 6, '#58a040', '#2f6a2a', every=4, tl=9)
    c.glow(c.ell(50, 50, 6), VIOLET, halo=9, power=.9)
def a_bough_strike(c):
    c.back(c.taper([(10, 70), (30, 30), (64, 10), (94, 14)], 1, 1, wmid=24), '#c8e070', .45, 5)
    c.push(rot=42, sc=1.15)
    c.part(c.taper([(50, 104), (50, 60), (50, 6)], 9, 26, smooth=False), BARK, 'wood', grain=.45)
    c.part(c.taper([(60, 40), (72, 30), (78, 16)], 7, 4), BARK, 'wood'); leafshape(c, 77, 18, 92, 0, 14, LEAF, 'gem')
    for y in (20, 34, 50): c.flat(c.line([(42, y), (58, y + 5)], 1.8), dark(BARK, .6), .8)
    c.pop()
    slash(c, [(10, 60), (30, 30), (58, 14), (88, 12)], 7, '#c8e070')
def a_roar(c):
    rings(c, 50, 54, (30, 40, 49), '#ffb060', .5, a0=-150, a1=-30, w=4)
    mouth = c.poly([(14, 40), (50, 24), (86, 40), (80, 74), (50, 92), (20, 74)], smooth=True)
    c.part(mouth, BARK, 'wood', grain=.4)
    inner = c.poly([(24, 44), (50, 34), (76, 44), (72, 70), (50, 82), (28, 70)], smooth=True); c.part(inner, '#7a1c1c', 'matte')
    c.part(c.ell(50, 74, 14, 8), '#d05a5a', 'soft', clip=inner)
    for x, d in ((32, 1), (68, 1)): c.part(c.taper([(x, 40), (x + (50 - x) * .1, 60)], 10, .5, smooth=False), '#f4ecd8', 'soft')
    for x in (44, 56): c.part(c.taper([(x, 36), (x, 48)], 7, .5, smooth=False), '#f4ecd8', 'soft')
    for x in (36, 64): c.part(c.taper([(x, 78), (x + (50 - x) * .1, 62)], 8, .5, smooth=False), '#f4ecd8', 'soft')
def a_barkmend(c):
    b = c.poly([(22, 14), (60, 8), (84, 26), (80, 78), (56, 94), (22, 84), (14, 50)]); c.part(b, BARK, 'wood', grain=.5)
    for p in ([(30, 16), (34, 40), (26, 80)], [(70, 16), (72, 50), (64, 90)]): c.flat(c.line(p, 2, smooth=True), dark(BARK, .65), .8, clip=b)
    plus = c.U_(c.rect(42, 26, 58, 76, 3), c.rect(25, 43, 75, 59, 3)); c.glow(plus, '#7fe060', core='#eaffd0', halo=10, power=.8)
def a_brace(c):
    c.back(c.ell(50, 50, 46), '#ffd070', .35, 8)
    d = c.blob(50, 50, 42, n=14, jit=.05, smooth=True); c.part(d, BARK, 'wood', grain=.4)
    c.part(c.ell(50, 50, 34), '#d9b47a', 'wood')
    for r in (27, 19): c.flat(c.sub(c.ell(50, 50, r), c.ell(50, 50, r - 1.6)), dark('#d9b47a', .45), .8)
    c.glow(heart(c, 50, 50, 13), '#ff7a4a', core='#ffe0b0', halo=9, power=.8)
def a_rake(c):
    for dx in (-22, 0, 22): slash(c, [(64 + dx, 6), (50 + dx, 46), (26 + dx, 94)], 11, '#ff4a2a', core='#ffd8b0')
def a_lunge(c):
    for y in (26, 46, 66): c.back(c.taper([(0, y + 30), (50, y - 4)], 1, 10, smooth=False), '#ff9a5a', .5, 3)
    c.push(rot=38, sc=.95, dx=10, dy=-8); pawprint(c, 50, 58, 28, '#4a5a3a', claws='#e8f0d0', clawlen=15); c.pop()
def a_tear(c):
    c.back(c.ell(50, 50, 30, 18), '#ff3a2a', .6, 10)
    for x, w in ((26, 15), (50, 12), (74, 15)): c.part(c.taper([(x, 4), (x, 20), (x + (50 - x) * .15, 46)], w + 3, .5), '#f4ecd8', 'soft')
    for x, w in ((38, 13), (62, 13)): c.part(c.taper([(x, 98), (x, 80), (x + (50 - x) * .1, 56)], w + 2, .5), '#f4ecd8', 'soft')
    c.part(c.rect(8, -4, 92, 10, 4), '#8a2a2a', 'soft'); c.part(c.rect(8, 90, 92, 104, 4), '#8a2a2a', 'soft')
    c.glow(c.taper([(14, 52), (50, 49), (86, 52)], .5, .5, wmid=5), '#ff4a2a', halo=6, power=.6)
def a_flurry(c):
    for p in ([(80, 8), (50, 40), (14, 60)], [(90, 30), (58, 58), (22, 84)], [(60, 4), (36, 30), (8, 38)], [(20, 10), (48, 44), (70, 90)], [(44, 6), (66, 44), (90, 78)], [(8, 26), (30, 56), (44, 94)]):
        slash(c, p, 6.5, '#ff6a2a', core='#ffe8c0', power=.55)
def a_seedling(c):
    c.back(c.ell(50, 36, 26), '#b8ff80', .5, 12)
    c.part(c.ell(50, 86, 34, 11), '#6a4a2c', 'matte'); c.part(c.line([(50, 84), (49, 60), (50, 44)], 5, smooth=True), '#4f9a3a', 'matte')
    leafshape(c, 50, 46, 20, 24, 24, LEAF, 'gem'); leafshape(c, 50, 46, 80, 20, 26, LEAF, 'gem')
def _flower(c, cx, cy, r, petal, heartc='#ffd040', n=5, w=None, rot=-90):
    for i in range(n):
        a = math.radians(rot + 360 * i / n)
        c.part(c.ell(cx + r * .62 * math.cos(a), cy + r * .62 * math.sin(a), r * .55, w or r * .36, rot=math.degrees(a)), petal, 'soft')
    c.part(c.ell(cx, cy, r * .3), heartc, 'gem')
def a_quickbloom(c):
    c.back(c.ell(50, 44, 34), '#ffb0e0', .4, 12)
    c.part(c.line([(50, 98), (52, 76), (50, 56)], 5, smooth=True), '#4f9a3a', 'matte'); leafshape(c, 51, 82, 76, 70, 13, LEAF)
    _flower(c, 50, 42, 32, '#f6c0e0')
def a_ward(c):
    d = c.poly([(50, 2), (76, 26), (82, 54), (68, 80), (50, 98), (32, 80), (18, 54), (24, 26)], smooth=True, n=6)
    c.back(c.grow(d, 6), '#8fff8a', .6, 9); c.part(d, '#4fae4a', 'gem')
    c.part(c.taper([(50, 6), (50, 50), (50, 94)], 2, 2, wmid=7), lite('#4fae4a', .35), 'gem', clip=d)
    for y in (30, 46, 62):
        for sx in (-1, 1): c.flat(c.line([(50, y + 8), (50 + sx * 24, y - 6)], 1.6), dark('#4fae4a', .5), .7, clip=d)
    c.shine(c.ell(34, 34, 4, 11, rot=25), .5, clip=d)
def a_burst_bloom(c):
    rays(c, 50, 50, 16, 66, 12, '#ffd070', .5, 10)
    for i in range(8):
        a = math.radians(i * 45 + 22); c.part(c.ell(50 + 34 * math.cos(a), 50 + 34 * math.sin(a), 12, 6.5, rot=math.degrees(a)), '#f08ac0', 'soft')
    c.glow(c.ell(50, 50, 15), '#ffd040', core='#fffbe0', halo=12, power=.9)
def a_seedshot(c):
    for y, x0 in ((60, 2), (76, 10), (90, 26)): c.back(c.taper([(x0, y + 8), (50, y - 22)], 1, 8, smooth=False), '#b090ff', .55, 3)
    c.push(rot=-38, sc=1.2, dx=6, dy=-6); seed = c.taper([(20, 50), (52, 50), (88, 50)], 4, .5, wmid=36); c.part(seed, '#a8763c', 'wood')
    c.flat(c.line([(24, 50), (84, 50)], 1.6), dark('#a8763c', .6), .8); c.shine(c.ell(48, 42, 12, 3.5), .45, clip=seed); c.pop()
    leafshape(c, 28, 66, 10, 60, 9, LEAF)
def a_thornbolt(c):
    c.back(c.taper([(0, 100), (60, 40)], 2, 26, smooth=False), VIOLET, .55, 6)
    c.push(rot=45, sc=1.2)
    b = c.poly([(50, -6), (62, 40), (56, 46), (58, 74), (50, 66), (42, 74), (44, 46), (38, 40)])
    c.part(b, '#5fb04a', 'gem'); c.part(c.AND(b, c.rect(0, -20, 49.5, 100)), lite('#5fb04a', .4), 'gem', ink=False)
    c.pop(); c.spark(80, 20, 9, VIOLET, .9)
def a_briar_snare(c):
    pts = [(50 + 30 * math.cos(math.radians(a)), 50 + 28 * math.sin(math.radians(a))) for a in range(-100, 261, 30)]
    thornvine(c, pts, 7, '#58a040', '#2f6a2a', every=3, tl=10)
    thornvine(c, [(22, 50), (8, 70), (4, 94)], 6, '#58a040', '#2f6a2a', every=4, tl=8)
    c.back(c.ell(50, 50, 16), VIOLET, .5, 8)
def a_bramblestorm(c):
    c.back(c.ell(50, 50, 40), VIOLET, .4, 10)
    for k in range(3):
        pts = [(50 + (8 + t * 5.5) * math.cos(t * .75 + k * 2.09), 50 + (8 + t * 5.5) * math.sin(t * .75 + k * 2.09)) for t in range(0, 7)]
        thornvine(c, pts, 5, '#58a040', '#2f6a2a', every=4, tl=8)
    c.glow(c.ell(50, 50, 7), VIOLET, core='#f0e0ff', halo=10, power=1.0)
def drop(c, cx, cy, s): return c.poly([(cx, cy - s * 1.25), (cx + s * .72, cy + s * .1), (cx + s * .5, cy + s * .8), (cx, cy + s), (cx - s * .5, cy + s * .8), (cx - s * .72, cy + s * .1)], smooth=True, n=6)
def a_swiftroot(c):
    c.part(c.taper([(8, 92), (30, 80), (40, 60), (30, 44), (44, 34)], 10, 3), '#a07448', 'wood', grain=.3)
    c.part(c.taper([(30, 80), (50, 88), (70, 84)], 6, 1.5), '#a07448', 'wood')
    d = drop(c, 62, 46, 26); c.glow(d, '#4ab8ff', core='#d8f4ff', halo=10, power=.4); c.part(d, '#4aa8f0', 'gem'); c.shine(c.ell(54, 44, 4, 9, rot=20), .7)
def a_stillroot(c):
    st = c.blob(50, 50, 22, 20, n=8, jit=.12); c.part(st, '#8f8a84', 'matte')
    for pts in ([(10, 96), (16, 60), (34, 40), (50, 36)], [(90, 96), (86, 60), (68, 42), (52, 44)], [(50, 100), (46, 80), (52, 62)]):
        c.part(c.taper(pts, 12, 3), '#a07448', 'wood', grain=.3)
    c.back(c.ell(50, 50, 30), VIOLET, .3, 10)

ABILITIES = {
    'ability.strike': ('#6a2a20', a_strike), 'ability.challenge': ('#6a2420', a_challenge), 'ability.guard': ('#24406a', a_guard),
    'ability.intercept': ('#1f4a54', a_intercept), 'ability.breaching_blow': ('#6a3c14', a_breach), 'ability.muster': ('#4f5a1e', a_muster),
    'ability.restorative_weave': ('#3a4a2a', a_weave), 'ability.light_bolt': ('#4a4420', a_lightbolt),
    'druid.barkhide': ('#4a3620', a_barkhide), 'druid.thornclaw': ('#5a2226', a_thornclaw), 'druid.rootmend': ('#214a2c', a_rootmend), 'druid.thornsong': ('#38285a', a_thornsong),
    'druid.bough_strike': ('#4a4420', a_bough_strike), 'druid.bellowing_roar': ('#5a3018', a_roar), 'druid.barkmend': ('#2c4424', a_barkmend), 'druid.heartwood_brace': ('#54401c', a_brace),
    'druid.rake': ('#4a1c1c', a_rake), 'druid.lunge': ('#5a2c1c', a_lunge), 'druid.tear': ('#3a1418', a_tear), 'druid.rending_flurry': ('#4a2416', a_flurry),
    'druid.seedling': ('#214a2c', a_seedling), 'druid.quickbloom': ('#2c4a3a', a_quickbloom), 'druid.verdant_ward': ('#1c4434', a_ward), 'druid.burst_bloom': ('#4a2a44', a_burst_bloom),
    'druid.seedshot': ('#38285a', a_seedshot), 'druid.thornbolt': ('#30245a', a_thornbolt), 'druid.briar_snare': ('#2c2a4a', a_briar_snare), 'druid.bramblestorm': ('#3a2460', a_bramblestorm),
    'druid.swiftroot': ('#1c3c54', a_swiftroot), 'druid.stillroot': ('#3a3048', a_stillroot),
}

# ---- talents: an emblem per icon word, in the branch's colours ----
def m_shield(c, A, B):
    d = c.poly([(18, 14), (50, 18), (82, 14), (84, 46), (72, 72), (50, 92), (28, 72), (16, 46)], smooth=True, n=6)
    c.part(d, lite(B, .2), 'metal'); c.part(c.grow(d, -8), A, 'matte'); c.part(c.rect(46, 20, 54, 90), lite(B, .2), 'metal', clip=c.grow(d, -8))
def m_leaf(c, A, B): c.part(c.line([(30, 86), (40, 70)], 4), B, 'matte'); leafshape(c, 36, 76, 84, 12, 40, A, 'gem')
def m_heart(c, A, B): h = heart(c, 50, 52, 34); c.part(h, A, 'gem'); c.shine(c.ell(34, 38, 6, 9, rot=30), .5, clip=h)
def m_chain(c, A, B):
    for i, (x, y) in enumerate(((28, 28), (50, 50), (72, 72))):
        c.part(c.sub(c.ell(x, y, 19, 12, rot=45 if i % 2 == 0 else -45), c.ell(x, y, 11, 5, rot=45 if i % 2 == 0 else -45)), A if i != 1 else lite(B, .2), 'metal')
def m_sword(c, A, B):
    c.push(rot=45, sc=1.15); P = dict(_P(), metal=col(lite(A, .35)), trim=col(B)); b = _blade(c, [(43.5, 64), (43.5, 14), (50, -2), (56.5, 14), (56.5, 64)], P['metal'], y0=4)
    c.part(c.rect(30, 62.5, 70, 69.5, 2.5), B, 'metal'); c.part(c.rect(45.5, 69, 54.5, 90, 2), '#6a4a30', 'matte'); c.part(c.ell(50, 94, 6.5), B, 'metal'); c.pop()
def m_target(c, A, B):
    for r, k in ((38, A), (26, '#efe6d2'), (14, A)): c.part(c.ell(50, 50, r), k, 'matte')
    c.part(c.ell(50, 50, 5), B, 'gem')
def m_clock(c, A, B):
    c.part(c.ell(50, 50, 38), B, 'metal'); c.part(c.ell(50, 50, 30), '#efe6d2', 'matte')
    c.part(c.line([(50, 50), (50, 28)], 4.5), A, 'matte'); c.part(c.line([(50, 50), (66, 58)], 4.5), A, 'matte'); c.part(c.ell(50, 50, 4), dark(A, .3), 'matte')
    for a in range(0, 360, 90): c.flat(c.ell(50 + 25 * math.cos(math.radians(a)), 50 + 25 * math.sin(math.radians(a)), 2.2), dark(B, .5))
def m_hand(c, A, B):
    palm = c.rect(28, 42, 70, 78, 8); fingers = c.U_(*[c.line([(x, 48), (x, top)], 9.6) for x, top in ((34, 22), (44.5, 13), (55, 15), (65, 24))]); thumb = c.line([(70, 64), (82, 54), (86, 40)], 10.5)
    c.part(c.U_(palm, fingers, thumb), A, 'soft'); c.part(c.rect(26, 76, 72, 94, 3), B, 'metal')
    for x in (39.2, 49.7, 60): c.flat(c.line([(x, 24), (x, 48)], 1.4), dark(A, .6), .85)
def m_star(c, A, B): s = c.star(50, 52, 42, 18, 5); c.glow(s, A, core=lite(A, .7), halo=10, power=.5); c.part(s, A, 'gem'); c.shine(c.ell(42, 40, 4, 8, rot=30), .5, clip=s)
def m_dagger(c, A, B):
    c.push(rot=-135, sc=1.2); b = _blade(c, [(43, 60), (44, 24), (50, 4), (56, 24), (57, 60)], lite(A, .35), y0=10)
    c.part(c.rect(34, 59, 66, 66, 2.5), B, 'metal'); c.part(c.rect(45.5, 66, 54.5, 86, 2), '#6a4a30', 'matte'); c.part(c.ell(50, 89, 6), B, 'metal'); c.pop()
def m_paw(c, A, B): pawprint(c, 50, 60, 31, A, claws=B, clawlen=10)
def m_wind(c, A, B):
    for y, x1, w in ((30, 78, 9), (52, 90, 11), (74, 70, 9)):
        c.part(c.taper([(8, y + 4), (x1 - 20, y), (x1, y - 4), (x1 + 2, y - 16), (x1 - 10, y - 16)], 2, 4, wmid=w), A if y != 52 else lite(A, .3), 'soft')
def m_eye(c, A, B):
    e = c.poly([(6, 50), (28, 28), (50, 22), (72, 28), (94, 50), (72, 72), (50, 78), (28, 72)], smooth=True); c.part(e, '#efe6d2', 'soft')
    c.part(c.ell(50, 50, 19), A, 'gem', clip=e); c.part(c.ell(50, 50, 8.5), '#141018', 'matte', ink=False); c.shine(c.ell(44, 43, 4, 3), .85)
def m_spiral(c, A, B):
    pts = [(50 + (4 + t * 3.3) * math.cos(t * .62), 50 + (4 + t * 3.3) * math.sin(t * .62)) for t in range(0, 12)]
    c.part(c.taper(pts, 5, 12), A, 'gem')
def m_drop(c, A, B): d = drop(c, 50, 54, 36); c.part(d, A, 'gem'); c.shine(c.ell(38, 54, 5, 12, rot=20), .6, clip=d)
def m_plus(c, A, B): p = c.U_(c.rect(38, 12, 62, 88, 4), c.rect(12, 38, 88, 62, 4)); c.glow(p, A, halo=10, power=.4); c.part(p, A, 'gem')
def m_moon(c, A, B): m = c.sub(c.ell(50, 50, 38), c.ell(66, 40, 32)); c.glow(m, A, halo=10, power=.4); c.part(m, lite(A, .3), 'gem')
def m_flask(c, A, B):
    body = c.U_(c.ell(50, 62, 29, 28), c.rect(41, 22, 59, 44)); c.part(body, dark('#bfe0e6', .45), 'gem'); inner = c.grow(body, -5)
    c.part(c.AND(inner, c.rect(0, 52, 100, 100)), A, 'gem', ink=False); c.part(c.rect(39, 18, 61, 26, 2), B, 'metal'); c.part(c.rect(42, 8, 58, 20, 2), '#9a6a3a', 'matte'); c.shine(c.taper([(34, 50), (30, 62), (33, 76)], 2, 2, wmid=6), .6, clip=inner)
def m_bolt(c, A, B):
    b = c.poly([(60, 2), (22, 54), (44, 54), (34, 98), (80, 40), (56, 40), (72, 2)]); c.glow(b, A, core=lite(A, .75), halo=10, power=.6); c.part(b, lite(A, .25), 'gem')
def m_crystal(c, A, B):
    for pts, k in (([(30, 90), (20, 50), (34, 26), (46, 52), (44, 90)], .0), ([(44, 92), (40, 30), (54, 4), (68, 32), (64, 92)], .2), ([(62, 90), (64, 56), (76, 40), (86, 60), (80, 90)], -.1)):
        m = c.poly(pts); c.part(m, lite(A, .2 + k), 'gem'); c.part(c.AND(m, c.rect(0, 0, pts[2][0], 100)), '#ffffff', 'gem', ink=False, alpha=.3)
def m_note(c, A, B):
    c.part(c.rect(56, 12, 63, 72, 2), A, 'gem'); c.part(c.poly([(60, 10), (88, 24), (84, 44), (62, 30)], smooth=True), A, 'gem'); c.part(c.ell(44, 74, 19, 14, rot=-20), A, 'gem'); c.shine(c.ell(38, 68, 6, 3, rot=-20), .5)
def m_banner(c, A, B):
    c.part(c.rect(22, 6, 28, 98, 2), '#8a5a30', 'wood'); c.part(c.ell(25, 7, 5.5), B, 'metal')
    f = c.poly([(28, 12), (56, 16), (88, 12), (84, 36), (88, 62), (72, 54), (56, 66), (28, 60)]); c.part(f, A, 'soft'); c.part(c.star(55, 38, 12, 5, 5), B, 'metal', clip=f)
def m_gear(c, A, B):
    g = c.U_(c.ell(50, 50, 30), *[c.rect(44, 8, 56, 92, 2).rotate(a, resample=Image.BICUBIC) for a in (0, 45, 90, 135)])
    c.part(c.sub(g, c.ell(50, 50, 12)), lite(A, .2), 'metal')
def m_skull(c, A, B):
    b = '#e9dfc8'; c.part(c.U_(c.ell(50, 42, 34, 32), c.rect(32, 60, 68, 88, 5)), b, 'soft')
    for sx in (-1, 1): c.part(c.ell(50 + sx * 14, 46, 9, 10), '#1a1216', 'matte', ink=False); c.glow(c.ell(50 + sx * 14, 47, 3), A, halo=5, power=.8)
    c.flat(c.poly([(50, 56), (45, 66), (55, 66)]), '#1a1216')
    for x in (41, 50, 59): c.flat(c.line([(x, 76), (x, 88)], 1.6), '#1a1216', .85)
def m_flame(c, A, B):
    f = c.poly([(50, 4), (66, 30), (62, 44), (78, 36), (82, 66), (66, 92), (36, 94), (18, 70), (24, 44), (36, 54), (36, 28)], smooth=True)
    c.glow(f, A, core=lite(A, .3), halo=12, power=.5); c.part(f, A, 'gem')
    c.part(c.poly([(50, 44), (62, 66), (56, 88), (42, 88), (36, 70)], smooth=True), '#ffe070', 'gem', ink=False)
MOTIFS = dict(shield=m_shield, leaf=m_leaf, heart=m_heart, chain=m_chain, sword=m_sword, target=m_target, clock=m_clock, hand=m_hand, star=m_star, dagger=m_dagger,
              paw=m_paw, wind=m_wind, eye=m_eye, spiral=m_spiral, drop=m_drop, plus=m_plus, moon=m_moon, flask=m_flask, bolt=m_bolt, crystal=m_crystal, note=m_note,
              banner=m_banner, gear=m_gear, skull=m_skull, flame=m_flame)
# branch: tile, then the emblem colours it cycles through when a branch repeats an icon word
BRANCH = {
    'tank': ('#24364e', ['#4a80d0', '#8fb8e8', '#6a70d8', '#3fa0b0', '#a8b4c4']), 'dps': ('#4e2420', ['#e0503a', '#f09040', '#c83a6a', '#e8c040', '#d07050']),
    'support': ('#4a4018', ['#e8b830', '#f0e080', '#e08a30', '#b8d050', '#f0c890']), 'barkhide': ('#44321e', ['#c08a4a', '#e0b070', '#a0a050', '#d07040', '#b89a7a']),
    'thornclaw': ('#4a2024', ['#e0504a', '#9be05a', '#f08a50', '#d04a80', '#c8d070']), 'rootmend': ('#1e4428', ['#62c84a', '#b0e86a', '#40c0a0', '#e8e070', '#8ad0f0']),
    'thornsong': ('#33265a', ['#a070f0', '#70c0f0', '#e070d0', '#9be05a', '#f0d070']),
}
KIND_TRIM = dict(passive='#c9ccd2', modifier='#e0b040', active='#fff0b0', signature='#fff0b0', capstone='#ffd040')
def draw_talent(c, icon, branch, kind, k):
    """k: how many earlier talents in the branch used the same icon word (0 = first)."""
    tile, cols = BRANCH.get(branch, ('#3a3a44', ['#c0c0c8', '#e0b040', '#80a0d0', '#d08080', '#a0d0a0']))
    A = cols[k % len(cols)]; B = KIND_TRIM.get(kind, '#c9ccd2')
    if kind in ('active', 'signature'): rays(c, 50, 50, 14, 72, 12 if kind == 'active' else 16, lite(A, .4), .38 if kind == 'active' else .5, 9)
    elif kind == 'capstone': rays(c, 50, 50, 14, 72, 16, '#ffd860', .55, 10); rings(c, 50, 50, (47,), '#ffd860', .5, w=3)
    elif kind == 'modifier': rings(c, 50, 50, (46,), lite(A, .3), .45, w=3)
    tilt = (0, -14, 12, 0, -8)[k % 5]; sc = (1, .92, .92, .8, .9)[k % 5]
    c.push(rot=tilt, sc=sc)
    MOTIFS.get(icon, m_star)(c, A, B)
    c.pop()
    if k % 5 == 1: c.spark(80, 20, 10, lite(A, .5), .9)
    elif k % 5 == 2:
        for x in (20, 50, 80): c.part(c.ell(x, 92, 4.5), B, 'metal')
    elif k % 5 == 3: c.part(c.sub(c.ell(50, 50, 47), c.ell(50, 50, 42)), B, 'metal')
    elif k % 5 == 4: c.spark(20, 22, 8, lite(A, .5), .8); c.spark(82, 78, 8, lite(A, .5), .8)

# ====================================================================================================
# lib4.py
# ====================================================================================================
# Drawers, batch 4: every hand-made item that is not gear (materials, food, potions, tools, bags, junk) and the quest items.

GREEN = '#5f9a48'
# ---- herbs ----
def herb_tarnwort(c):
    c.part(c.ell(50, 90, 30, 6), '#3a6a8a', 'gem')
    for x1, y1, w in ((18, 30, 24), (50, 8, 28), (82, 30, 24)):
        c.part(c.line([(50, 90), ((50 + x1) / 2, 70)], 3.5), '#2f7a6a', 'matte'); leafshape(c, (50 + x1) / 2, 72, x1, y1, w, '#3fa890', 'gem')
    c.part(c.ell(50, 62, 6), '#bfe8ff', 'gem')
def herb_thistle(c):
    c.part(c.line([(50, 98), (48, 70), (50, 50)], 5, smooth=True), '#6a8a4a', 'matte')
    for sx in (-1, 1): c.part(c.poly([(50, 84), (50 + sx * 10, 74), (50 + sx * 8, 66), (50 + sx * 22, 62), (50 + sx * 14, 56), (50 + sx * 26, 46), (50 + sx * 8, 54), (50, 62)]), '#7a9a5a', 'matte')
    for a in range(-150, -29, 15):
        r = math.radians(a); c.glow(c.taper([(50 + 8 * math.cos(r), 36 + 8 * math.sin(r)), (50 + 34 * math.cos(r), 34 + 34 * math.sin(r))], 5, .5, smooth=False), '#ff5a2a', core='#ffc080', halo=5, power=.35)
    c.part(c.blob(50, 44, 15, 13, n=10, jit=.1, smooth=True), '#55703a', 'matte')
    for x, y in ((44, 40), (54, 44), (48, 50), (58, 38)): c.flat(c.poly([(x - 3, y), (x, y - 4), (x + 3, y)]), dark('#55703a', .55), .9)
def herb_fern(c):
    spine = [(30, 96), (40, 66), (58, 38), (70, 22), (66, 12), (58, 14), (60, 22)]
    for i, t in enumerate((.12, .24, .36, .48, .6)):
        x = 30 + (70 - 30) * t * 1.3; y = 96 - 80 * t * 1.25; L = 26 - i * 4
        leafshape(c, x, y, x - L, y - L * .45, 9 - i, '#58b06a', 'matte', vein=False); leafshape(c, x, y, x + L * .8, y + L * .25, 9 - i, '#58b06a', 'matte', vein=False)
    c.part(c.taper(spine, 5, 3), '#3f8a50', 'matte')
    for x, y in ((24, 58), (74, 52), (46, 30)): d = drop(c, x, y, 5); c.part(d, '#a8e0ff', 'gem'); c.shine(c.ell(x - 1.5, y, 1.2, 2.4), .8)
def herb_bell(c, bell, glowc=None, leaf=GREEN, n=3):
    c.part(c.taper([(40, 98), (36, 60), (50, 22), (70, 14)], 5, 2.5), dark(leaf, .1), 'matte')
    leafshape(c, 38, 84, 12, 60, 14, leaf)
    for x, y, s in ((72, 36, 17), (48, 50, 14), (28, 34, 11))[:n]:
        c.flat(c.line([(x, y - s), (x - 4, y - s - 8)], 1.6), dark(leaf, .3), .95)
        b = c.poly([(x - s * .45, y - s), (x + s * .45, y - s), (x + s * .8, y + s * .3), (x + s, y + s), (x + s * .4, y + s * .75), (x, y + s), (x - s * .4, y + s * .75), (x - s, y + s), (x - s * .8, y + s * .3)], smooth=True, n=5)
        if glowc: c.glow(b, glowc, halo=9, power=.45)
        c.part(b, bell, 'gem'); c.part(c.ell(x, y + s * .9, 2.2), '#ffe070', 'gem')
def herb_lamp(c):
    c.part(c.taper([(30, 98), (30, 50), (50, 14), (74, 20)], 5, 2.5), '#6a7a3a', 'matte'); leafshape(c, 30, 80, 8, 62, 13, GREEN)
    for x, y, s in ((72, 50, 19), (46, 46, 13)):
        c.flat(c.line([(x, y - s), (x + (2 if x > 60 else 4), y - s - 12)], 1.6), '#6a7a3a', .95)
        b = c.poly([(x, y - s), (x + s * .8, y - s * .2), (x + s * .7, y + s * .6), (x, y + s * 1.1), (x - s * .7, y + s * .6), (x - s * .8, y - s * .2)], smooth=True)
        c.glow(b, '#ff9a30', core='#ffe9a0', halo=11, power=.6)
        for dx in (-.4, 0, .4): c.flat(c.line([(x + dx * s * .3, y - s * .8), (x + dx * s * 1.4, y + s * .2), (x + dx * s * .4, y + s)], 1.3, smooth=True), '#c05a18', .7, clip=b)
def herb_lastlight(c):
    c.part(c.taper([(50, 98), (46, 70), (50, 46)], 5, 3), '#6a8a4a', 'matte'); leafshape(c, 48, 80, 22, 66, 12, GREEN); leafshape(c, 49, 74, 76, 58, 12, GREEN)
    s = c.star(50, 34, 30, 11, 6); c.glow(s, '#ffc840', core='#fff8d0', halo=13, power=.7); c.part(c.ell(50, 34, 6), '#e08a20', 'gem'); c.spark(68, 16, 8, '#ffe9a0', .8)
def herb_moss(c):
    c.part(c.blob(50, 74, 34, 14, n=9, jit=.12), '#7a7a80', 'matte')
    for x, y, r in ((34, 60, 15), (62, 58, 17), (48, 46, 15), (72, 70, 10), (26, 72, 9)):
        m = c.blob(x, y, r, r * .85, n=10, jit=.2, smooth=True); c.glow(m, '#5fe07a', core='#d0ffc0', halo=10, power=.35); c.part(m, '#4fae5a', 'soft', alpha=.75)
    c.spark(52, 40, 9, '#c8ffd0', .8)

# ---- stuff from the smith, the mill and the still ----
def charcoal(c):
    for x, y, rx, ry, r in ((36, 62, 24, 17, 20), (66, 64, 22, 16, -15), (52, 40, 24, 16, 5)):
        b = c.blob(x, y, rx, ry, n=7, jit=.14, rot=r); c.part(b, '#3a3840', 'matte')
        c.part(c.blob(x - 4, y - 5, rx * .5, ry * .4, n=5, jit=.2), '#5a5862', 'matte', ink=False, clip=b)
        for k in (-8, 8): c.flat(c.line([(x + k, y - ry * .5), (x + k + 4, y + ry * .5)], 1.3), '#17161a', .8, clip=b)
    for x, y in ((44, 54), (64, 52), (30, 66)): c.glow(c.ell(x, y, 2.2), '#ff6a1f', halo=5, power=.6)
def sack(c, cloth='#c9b48a', top='#f4f0e6', mark=None, tie='#8a5a3a', knots=0):
    body = c.poly([(50, 30), (72, 34), (86, 58), (84, 84), (66, 94), (34, 94), (16, 84), (14, 58), (28, 34)], smooth=True)
    c.part(c.blob(50, 26, 22, 12, n=10, jit=.12, smooth=True), top, 'soft')
    c.part(body, cloth, 'soft'); c.part(c.line([(26, 38), (50, 43), (74, 38)], 4.5, smooth=True), tie, 'matte')
    for i in range(knots): c.part(c.ell(38 + i * 12, 42, 4.2), lite(tie, .3), 'matte')
    if mark == 'wheat':
        c.flat(c.line([(50, 84), (50, 54)], 1.8), '#8a6a3a', .95)
        for y in (58, 66, 74):
            for sx in (-1, 1): c.part(c.ell(50 + sx * 6, y, 5.5, 2.8, rot=sx * -35), '#a8843e', 'matte', ink=False)
    elif mark: mark(c)
def salt(c, colr='#eef2f6', crust=False):
    if crust:
        b = c.poly([(12, 64), (24, 40), (52, 32), (80, 40), (90, 62), (76, 80), (30, 82)]); c.part(b, '#c8ccd0', 'matte')
        for p in ([(24, 40), (40, 58), (30, 82)], [(52, 32), (56, 56), (76, 80)], [(40, 58), (56, 56), (90, 62)]): c.flat(c.line(p, 1.6), dark('#c8ccd0', .5), .8, clip=b)
        c.spark(44, 46, 9, '#ffffff', .8); return
    c.part(c.AND(c.ell(50, 66, 38, 26), c.rect(0, 66, 100, 100)), '#8a6a4a', 'wood'); c.part(c.ell(50, 66, 39, 10), lite('#8a6a4a', .2), 'wood')
    pile = c.poly([(18, 66), (34, 44), (50, 30), (66, 44), (82, 66)], smooth=True); c.part(pile, colr, 'soft')
    for x, y in ((40, 52), (56, 46), (62, 58), (34, 62), (50, 60)): c.part(c.poly([(x - 4, y), (x, y - 4), (x + 4, y), (x, y + 4)]), '#ffffff', 'gem', ink=False)
    c.spark(54, 36, 8, '#ffffff', .7)

# ---- meat and food ----
def meat_slab(c, flesh, fat='#f0d9b8', ribs=True, crust=None):
    c.push(rot=-18)
    m = c.poly([(10, 44), (30, 28), (70, 26), (90, 42), (88, 66), (66, 78), (28, 76), (12, 62)], smooth=True); c.part(m, crust or fat, 'soft')
    c.part(c.grow(m, -6 if crust else -5), flesh, 'soft', clip=m)
    if ribs:
        for x in (30, 44, 58, 72): c.part(c.line([(x, 36), (x + 3, 68)], 4.5), '#efe6d2', 'soft', clip=m)
    if crust:
        for i in range(12): c.flat(c.ell(18 + c.rng.rand() * 64, 32 + c.rng.rand() * 40, 1.5), '#ffffff', .85, clip=m)
    c.pop()
def meat_chop(c, flesh, fat='#eadfb8', glaze=None, herb=False):
    m = c.poly([(22, 40), (44, 18), (72, 22), (86, 46), (74, 74), (44, 80), (24, 64)], smooth=True); c.part(m, fat, 'soft')
    c.part(c.grow(m, -6), flesh, 'soft', clip=m)
    c.part(c.taper([(30, 62), (16, 80), (8, 92)], 10, 8), '#efe6d2', 'soft'); c.part(c.ell(8, 92, 7), '#efe6d2', 'soft')
    c.part(c.ell(56, 48, 8, 6), fat, 'soft', ink=False)
    if glaze: c.shine(c.ell(52, 34, 16, 5, rot=-10), .4); c.flat(c.line([(36, 50), (56, 62), (72, 52)], 3, smooth=True), glaze, .7, clip=m)
    if herb: leafshape(c, 60, 66, 88, 84, 12, '#58b06a')
def meat_steak(c, flesh, fat='#f0d9b8'):
    m = c.blob(50, 52, 38, 30, n=10, jit=.1, smooth=True); c.part(m, fat, 'soft'); c.part(c.grow(m, -6), flesh, 'soft', clip=m)
    c.part(c.sub(c.ell(40, 50, 9, 7), c.ell(40, 50, 4.5, 3)), '#efe6d2', 'soft')
    for p in ([(52, 38), (64, 44), (74, 40)], [(50, 66), (62, 62), (72, 68)]): c.flat(c.line(p, 2, smooth=True), fat, .8, clip=m)
def eggs(c, basket=False):
    if basket:
        c.part(c.sub(c.ell(50, 44, 34, 36), c.ell(50, 44, 28, 30)), '#a8763c', 'wood', clip=c.rect(0, 0, 100, 50))
    for x, y, k, r in ((34, 56, '#f2e6d2', -15), (62, 54, '#d9b48a', 12), (48, 44, '#e6d0b0', 0)):
        e = c.ell(x, y, 15, 19, rot=r); c.part(e, k, 'soft'); c.shine(c.ell(x - 5, y - 8, 3, 5, rot=20), .5)
        if k == '#d9b48a':
            for i in range(6): c.flat(c.ell(x - 8 + c.rng.rand() * 16, y - 10 + c.rng.rand() * 20, 1.2), '#8a5a3a', .7, clip=e)
    if basket:
        b = c.poly([(10, 58), (90, 58), (80, 94), (20, 94)]); c.part(b, '#a8763c', 'wood')
        for y in (66, 76, 86): c.flat(c.line([(12, y), (88, y)], 1.5), dark('#a8763c', .55), .8, clip=b)
        for x in range(22, 86, 12): c.flat(c.line([(x, 58), (x + 2, 94)], 1.5), dark('#a8763c', .55), .6, clip=b)
        c.part(c.rect(8, 54, 92, 62, 3), lite('#a8763c', .15), 'wood')
def cheese(c):
    c.part(c.poly([(10, 58), (62, 30), (92, 46), (92, 72), (40, 90), (10, 80)]), '#e0a830', 'soft')
    c.part(c.poly([(10, 58), (62, 30), (92, 46), (40, 68)]), '#f4d060', 'soft')
    for x, y, r in ((30, 76, 4.5), (60, 74, 5.5), (78, 62, 3.5), (48, 52, 4), (66, 44, 3)): c.part(c.ell(x, y, r, r * .8), '#b8801c', 'matte', ink=False)
    c.part(c.poly([(10, 58), (10, 80), (14, 82), (14, 59)]), '#c05a2a', 'matte', ink=False)
def jerky(c):
    for x, r, k in ((30, -12, '#6e2e22'), (50, 4, '#8a3a28'), (70, 16, '#5e281e')):
        c.push(rot=r); s = c.poly([(x - 9, 10), (x + 7, 12), (x + 10, 40), (x + 6, 66), (x + 9, 92), (x - 7, 90), (x - 10, 62), (x - 6, 36)], smooth=True); c.part(s, k, 'soft', grain=.5)
        for y in (30, 52, 74): c.flat(c.line([(x - 7, y), (x + 7, y + 4)], 1.4), dark(k, .55), .7, clip=s)
        c.pop()
def flatbread(c, colr='#d9a860', marks=True, currants=False, small=False):
    r = 32 if small else 40
    c.part(c.ell(50, 60, r, r * .62), dark(colr, .3), 'soft'); top = c.ell(50, 52, r, r * .62); c.part(top, colr, 'soft')
    if marks:
        for x in (-22, -8, 8, 22): c.flat(c.line([(50 + x - 6, 34), (50 + x + 6, 72)], 2.6), '#7a4a20', .75, clip=top)
    if currants:
        for x, y in ((36, 46), (54, 42), (64, 56), (44, 60), (30, 56), (56, 66)): c.part(c.ell(x, y, 3), '#4a1c2a', 'gem', ink=False)
        for i in range(10): c.flat(c.ell(26 + c.rng.rand() * 48, 40 + c.rng.rand() * 24, 1), '#fff6e0', .8, clip=top)
def skewer(c):
    c.push(rot=-38)
    c.part(c.rect(2, 48, 104, 52, 1), '#c9a060', 'wood'); c.part(c.poly([(100, 46), (112, 50), (100, 54)]), '#c9a060', 'wood')
    for x, k in ((22, '#8a3e24'), (42, '#a04a28'), (62, '#7a3420'), (80, '#96442a')):
        b = c.blob(x, 50, 10, 13, n=8, jit=.12, smooth=True); c.part(b, k, 'soft'); c.flat(c.line([(x - 6, 44), (x + 6, 46)], 2), '#3a1c14', .6, clip=b); c.shine(c.ell(x - 3, 42, 3, 2), .4)
    c.pop()
def pasty(c):
    b = c.AND(c.ell(50, 66, 42, 46), c.rect(0, 0, 100, 68)); c.part(b, '#d9a04a', 'soft')
    for a in range(190, 351, 16):
        r = math.radians(a); c.part(c.ell(50 + 40 * math.cos(r), 66 + 44 * math.sin(r), 5.5), '#e8bc6a', 'soft')
    for x in (38, 50, 62): c.flat(c.line([(x, 44), (x + 3, 54)], 2), '#7a4a20', .8)
    c.part(c.rect(8, 64, 92, 72, 3), '#b87a30', 'soft')
def roast(c, crust='#7a3a22', flesh='#d07a5a'):
    c.push(rot=-14)
    b = c.rect(26, 30, 90, 74, 18); c.part(b, crust, 'soft')
    for x in (44, 58, 72): c.flat(c.line([(x, 30), (x, 74)], 2.2), '#e6dcc0', .8, clip=b)
    for x in (4, 14): s = c.ell(x + 12, 56, 9, 20); c.part(s, crust, 'soft'); c.part(c.ell(x + 12, 56, 6, 16), flesh, 'soft', ink=False)
    c.pop()
def pie(c, crust='#d9a04a', fill='#7a2e2a'):
    c.part(c.poly([(8, 56), (92, 56), (82, 88), (18, 88)]), '#9aa2ad', 'metal')
    top = c.ell(50, 54, 44, 20); c.part(top, crust, 'soft')
    for x in range(14, 90, 12): c.part(c.ell(x, 54 + 17 * (1 - ((x - 50) / 44) ** 2) ** .5 if abs(x - 50) < 44 else 54, 5, 4), lite(crust, .2), 'soft', ink=False)
    cut = c.poly([(50, 54), (96, 40), (96, 72)]); c.part(c.AND(top, cut), fill, 'soft')
    for sx in (-1, 0): c.flat(c.line([(28 + sx * -18, 46), (36 + sx * -18, 60)], 2.2), '#7a4a20', .8, clip=top)
    for x, k in ((36, 1), (52, -1)): c.flat(c.line([(x, 30), (x + 4 * k, 22), (x - 3 * k, 12)], 3, smooth=True), '#f6efe2', .5, blur=2)
def sapcake(c):
    c.part(c.poly([(12, 60), (60, 36), (90, 52), (90, 74), (40, 92), (12, 82)]), '#b8701c', 'soft'); c.part(c.poly([(12, 60), (60, 36), (90, 52), (40, 70)]), '#f0b030', 'gem')
    c.flat(c.line([(12, 71), (40, 81), (90, 63)], 2.2), '#f6e6c0', .8)
    for x, y in ((36, 56), (58, 50), (70, 56)): c.part(c.ell(x, y, 3, 2), '#7a3a10', 'matte', ink=False)
    c.shine(c.ell(50, 46, 12, 3, rot=-22), .5)

# ---- bags ----
def bag_wallet(c):
    b = c.rect(10, 28, 90, 82, 8); c.part(b, '#5f7a4a', 'soft')
    c.part(c.poly([(10, 28), (90, 28), (90, 50), (50, 62), (10, 50)]), '#7a9a5a', 'soft', clip=b)
    for x in range(18, 86, 9): c.flat(c.line([(x, 76), (x + 4, 76)], 1.5), '#e6dcc0', .9)
    c.part(c.ell(50, 58, 5), '#d9c9a0', 'metal'); leafshape(c, 30, 46, 52, 30, 12, '#bfe89a', 'gem'); c.part(c.line([(2, 55), (98, 55)], 3), '#8a5a3a', 'matte')
def bag_sling(c):
    c.push(rot=-20)
    for y, k in ((38, '#7a5230'), (62, '#6a4628')):
        c.part(c.rect(8, y - 13, 86, y + 13, 5), k, 'wood', grain=.3); e = c.ell(86, y, 6, 13); c.part(e, '#e2c48a', 'matte'); c.flat(c.sub(c.ell(86, y, 3.5, 8), c.ell(86, y, 2.4, 6.6)), '#a8843e', .8)
    for x in (28, 62): c.part(c.rect(x - 6, 20, x + 6, 80, 2), '#a8703a', 'matte'); c.part(c.ell(x, 50, 3), '#d9c9a0', 'metal')
    c.pop(); c.part(c.line([(24, 30), (40, 6), (66, 4), (76, 22)], 4.5, smooth=True), '#a8703a', 'matte')
def bag_scrip(c):
    c.part(c.line([(18, 40), (30, 8), (70, 8), (82, 40)], 4.5, smooth=True), '#8a5a3a', 'matte')
    b = c.rect(10, 36, 90, 94, 10); c.part(b, '#b8894e', 'soft')
    c.part(c.poly([(10, 36), (90, 36), (90, 58), (50, 74), (10, 58)], smooth=False), '#c99a5e', 'soft', clip=b)
    c.part(c.rect(44, 62, 56, 80, 2), '#8a5a3a', 'matte'); c.part(c.ell(50, 74, 3), '#e0b040', 'metal')
    c.part(c.ell(28, 50, 9, 6, rot=-20), '#e0a860', 'soft'); c.flat(c.line([(24, 49), (28, 52)], 1.2), '#7a4a20', .8); c.flat(c.line([(28, 47), (32, 50)], 1.2), '#7a4a20', .8)
def bag_poke(c):
    def peek(c):
        for x, y, k in ((40, 22, '#e8873a'), (58, 20, '#8d96a3'), (50, 14, '#6b5a50')): c.part(c.blob(x, y, 10, 9, n=6, jit=.2), k, 'metal' if k != '#6b5a50' else 'matte')
    def emblem(c):
        c.push(sc=.34, rot=0, dx=0, dy=16); tool_pick(c, '#d9c9a0', '#d9c9a0'); c.pop()
    pouch(c, '#7a5a3c', emblem=emblem, peek=peek)

# ---- junk ----
def coin(c, metal, mark='crown', worn=False):
    c.part(c.ell(52, 56, 36, 34), dark(metal, .35), 'metal'); d = c.ell(50, 50, 36, 34); c.part(d, metal, 'metal'); c.part(c.sub(c.ell(50, 50, 30, 28), c.ell(50, 50, 27, 25)), dark(metal, .35), 'metal', ink=False)
    if mark == 'crown': c.part(c.poly([(34, 60), (32, 40), (42, 50), (50, 36), (58, 50), (68, 40), (66, 60)]), dark(metal, .4), 'metal', ink=False)
    else: c.part(c.U_(c.rect(47, 34, 53, 66), c.rect(36, 42, 64, 48), c.rect(40, 60, 60, 65)), dark(metal, .45), 'metal', ink=False)
    if worn: c.flat(c.line([(26, 34), (40, 28)], 1.6), dark(metal, .6), .8); c.flat(c.line([(60, 70), (72, 62)], 1.6), dark(metal, .6), .8)
def rag(c, colr='#b0563a', stripe='#d9b070'):
    b = c.poly([(14, 22), (40, 14), (62, 26), (88, 18), (84, 52), (90, 82), (72, 76), (60, 90), (46, 78), (30, 88), (16, 74), (20, 48)], smooth=False); c.part(b, colr, 'soft')
    for y in (36, 60): c.flat(c.line([(12, y), (50, y + 6), (92, y)], 5, smooth=True), stripe, .85, clip=b)
    for x in (24, 38, 54, 68, 80): c.flat(c.line([(x, 78), (x + 2, 96)], 1.5), colr, .95)
def shard(c, colr, mat='matte', glowc=None, spark=False, thin=False):
    pts = [(54, 4), (72, 40), (64, 92), (40, 96), (26, 50)] if not thin else [(56, 2), (66, 44), (52, 98), (40, 50)]
    b = c.poly(pts)
    if glowc: c.glow(b, glowc, halo=12, power=.45)
    c.part(b, colr, mat); c.part(c.poly([pts[0], pts[-1], pts[-2], (50, 60)]), lite(colr, .3), mat, ink=False, clip=b)
    c.flat(c.line([pts[0], (50, 60), pts[2]], 1.4), dark(colr, .5), .6, clip=b)
    if spark: c.spark(64, 26, 10, glowc or '#ffffff', .9)
def maskpiece(c):
    b = c.poly([(22, 14), (60, 8), (82, 26), (70, 52), (78, 70), (52, 90), (34, 66), (40, 50), (20, 40)]); c.part(b, '#e2d8bc', 'soft')
    c.part(c.ell(52, 34, 11, 8, rot=15), '#141016', 'matte', ink=False); c.flat(c.line([(60, 8), (56, 22)], 1.4), '#8a7a5a', .8); c.flat(c.line([(40, 50), (52, 58), (56, 72)], 1.4), '#8a7a5a', .8)
def filament(c):
    c.glow(c.line([(20, 84), (34, 50), (56, 60), (64, 30), (44, 18), (40, 36), (70, 46), (84, 16)], 3.2, smooth=True), '#cfe8ff', core='#ffffff', halo=8, power=.6)
def badge(c):
    d = c.poly([(22, 14), (50, 20), (78, 14), (80, 48), (66, 76), (50, 90), (34, 76), (20, 48)], smooth=True, n=6); c.part(d, '#9a8a5a', 'metal')
    c.part(c.grow(d, -8), '#6a6a70', 'metal')
    for y in (36, 46, 56, 66): c.flat(c.line([(30, y + 6), (70, y - 6)], 1.8), '#2a2a30', .75, clip=d)
def bell(c):
    c.part(c.rect(45, 4, 55, 30, 3), '#8a5a30', 'wood')
    b = c.poly([(38, 28), (62, 28), (70, 60), (84, 80), (16, 80), (30, 60)], smooth=False); c.part(b, '#c9a040', 'metal'); c.part(c.rect(14, 76, 86, 86, 3), '#b08a30', 'metal')
    c.part(c.ell(50, 90, 7), '#8a6a20', 'metal'); c.flat(c.line([(58, 30), (54, 46), (62, 58), (56, 80)], 2, smooth=False), '#1a1410', .9); c.shine(c.ell(36, 52, 3, 12, rot=12), .5)
def tine(c, colr='#8a5f44', moss=False):
    c.part(c.taper([(30, 96), (40, 60), (58, 30), (64, 4)], 15, 2), colr, 'soft', grain=.4)
    c.part(c.taper([(44, 54), (64, 52), (84, 36)], 9, 1.5), colr, 'soft'); c.part(c.taper([(36, 74), (20, 60), (12, 40)], 8, 1.5), colr, 'soft')
    if moss:
        for x, y, r in ((44, 56, 8), (57, 32, 6), (34, 74, 7)): c.part(c.blob(x, y, r, n=8, jit=.25, smooth=True), '#5fae4a', 'soft', grain=.5)
def chitin(c):
    b = c.poly([(20, 30), (50, 12), (80, 30), (86, 62), (50, 90), (14, 62)], smooth=True); c.part(b, '#2f5a48', 'gem')
    for y in (36, 54, 70): c.flat(c.line([(14, y), (50, y + 10), (86, y)], 2, smooth=True), '#13241e', .85, clip=b)
    c.shine(c.ell(36, 30, 10, 4, rot=-25), .5, clip=b)
def gland(c):
    b = c.poly([(50, 14), (74, 34), (80, 64), (60, 88), (36, 86), (22, 62), (30, 32)], smooth=True); c.glow(b, '#b8ff8a', halo=10, power=.3); c.part(b, '#a8d87a', 'gem')
    for p in ([(40, 30), (36, 54), (46, 78)], [(62, 36), (68, 58), (58, 80)]): c.flat(c.line(p, 1.5, smooth=True), '#5a8a3a', .7, clip=b)
    c.part(c.taper([(50, 16), (46, 8), (52, 2)], 6, 3), '#7aa85a', 'soft'); c.shine(c.ell(40, 40, 4, 9, rot=20), .6)
def thorns(c): thornvine(c, [(12, 86), (34, 62), (52, 52), (66, 30), (88, 14)], 8, '#6a5a3a', '#3f5a2a', every=4, tl=13)
def knot(c):
    b = c.blob(50, 52, 34, 32, n=11, jit=.14, smooth=True); c.part(b, '#7a5a3a', 'wood', grain=.5)
    for p in ([(22, 40), (50, 26), (78, 42)], [(20, 62), (50, 78), (80, 60)]): c.flat(c.line(p, 2, smooth=True), '#3a2a1a', .8, clip=b)
    c.glow(c.ell(50, 52, 9, 8), '#ff4a3a', core='#ffc0a0', halo=10, power=.8)
def bark(c):
    b = c.poly([(20, 12), (62, 8), (84, 30), (78, 88), (38, 94), (16, 60)]); c.part(b, '#8a8a8c', 'wood', grain=.5)
    for x in (34, 50, 66): c.flat(c.line([(x, 14), (x - 4, 50), (x + 2, 90)], 2.2, smooth=True), '#3a3a3e', .8, clip=b)
    c.part(c.ell(54, 46, 5, 8), '#4a4a50', 'matte', ink=False)
def trinket(c):
    c.part(c.sub(c.ell(50, 22, 13, 12), c.ell(50, 22, 7, 6.5)), '#9aa2ad', 'metal')
    b = c.poly([(50, 30), (72, 52), (62, 86), (38, 86), (28, 52)]); c.glow(b, '#9fe8ff', halo=11, power=.35); c.part(b, '#a8d8f0', 'gem')
    c.flat(c.line([(50, 30), (50, 86)], 1.4), '#ffffff', .6); c.flat(c.line([(28, 52), (50, 60), (72, 52)], 1.4), '#ffffff', .6)

# ---- quest things ----
def crystal(c, colr='#f2f4f8', glowc='#cfe8ff', n=3):
    sets = (([(28, 92), (18, 54), (32, 30), (44, 54), (44, 92)], 0), ([(42, 94), (38, 30), (54, 4), (68, 32), (66, 94)], .15), ([(62, 92), (64, 58), (76, 42), (88, 62), (82, 92)], -.1))[:n] if n > 1 else (([(34, 94), (30, 34), (52, 6), (72, 36), (66, 94)], .1),)
    for pts, k in sets:
        m = c.poly(pts); c.glow(m, glowc, halo=10, power=.3); c.part(m, mix(colr, glowc, .25 - k), 'gem'); c.part(c.AND(m, c.rect(0, 0, pts[2][0], 100)), '#ffffff', 'gem', ink=False, alpha=.35)
    c.spark(60, 22, 10, glowc, .9)
def cask(c, mark=True):
    b = c.poly([(26, 14), (74, 14), (84, 50), (74, 88), (26, 88), (16, 50)], smooth=True, n=5); c.part(b, '#a8763c', 'wood', grain=.3)
    for x in (36, 50, 64): c.flat(c.line([(x, 14), (x + (x - 50) * .25, 50), (x, 88)], 1.5, smooth=True), '#4a3018', .8, clip=b)
    for y in (26, 74): c.part(c.rect(14, y - 4, 86, y + 4), '#8d96a3', 'metal', clip=c.grow(b, 2))
    if mark: c.part(c.taper([(38, 62), (62, 38)], 3, .5, smooth=False), '#efe6d2', 'matte'); c.part(c.poly([(62, 38), (52, 40), (60, 48)]), '#efe6d2', 'matte')
def bone(c, colr='#b8b4ac'):
    c.push(rot=-40); c.part(c.rect(22, 43, 78, 57, 5), colr, 'soft')
    for x in (20, 80):
        for y in (42, 58): c.part(c.ell(x, y, 9), colr, 'soft')
    c.flat(c.line([(36, 46), (50, 52), (62, 47)], 1.5), dark(colr, .5), .7); c.pop()
def shackle(c):
    m = '#7a7f88'
    c.part(c.sub(c.ell(40, 56, 28, 28), c.ell(40, 56, 17, 17)), m, 'metal'); c.part(c.rect(34, 78, 46, 90, 2), dark(m, .2), 'metal')
    for i, (x, y) in enumerate(((66, 34), (78, 22), (90, 10))): c.part(c.sub(c.ell(x, y, 10, 7, rot=-45 if i % 2 == 0 else 45), c.ell(x, y, 5, 2.5, rot=-45 if i % 2 == 0 else 45)), m, 'metal')
    c.flat(c.U_(c.ell(40, 84, 2), c.rect(39, 84, 41, 88)), '#141014'); c.shine(c.ell(24, 42, 3, 8, rot=40), .5)
def tallow(c):
    c.part(c.AND(c.ell(50, 60, 36, 34), c.rect(0, 50, 100, 100)), '#8a6a4a', 'wood'); c.part(c.ell(50, 50, 37, 11), '#a8845e', 'wood'); c.part(c.ell(50, 49, 31, 8), '#f0e2b0', 'soft')
    c.part(c.line([(50, 48), (50, 34)], 2.5), '#3a2a1a', 'matte'); f = drop(c, 50, 24, 9); c.glow(f, '#ffb040', core='#fff6c0', halo=9, power=.7)
def goods(c):
    c.part(c.rect(14, 46, 86, 90, 6), '#6a7a9a', 'soft')
    for y in (58, 70, 80): c.flat(c.line([(14, y), (86, y)], 1.5), '#3a4a6a', .7)
    for x in (30, 44): c.part(c.rect(x - 5, 14, x + 5, 50, 2), '#f0e6c0', 'soft'); c.flat(c.line([(x, 14), (x, 8)], 1.5), '#2a2018', .95)
    c.part(c.ell(68, 40, 15, 6), '#b9c2cc', 'metal'); c.part(c.ell(68, 36, 9, 3.5), '#8d96a3', 'metal', ink=False)
    c.part(c.line([(8, 66), (92, 66)], 3.5), '#c9a060', 'matte'); c.part(c.line([(50, 46), (50, 90)], 3.5), '#c9a060', 'matte')
def book(c, cover='#2a2a34', band='#c9a040', seal=None, wrap=None, thick=True):
    c.push(rot=-10)
    c.part(c.poly([(24, 14), (80, 14), (84, 20), (84, 92), (28, 92), (24, 86)]), '#e6dcc0', 'matte')
    b = c.rect(18, 10, 78, 88, 3); c.part(b, cover, 'soft'); c.part(c.rect(18, 10, 28, 88, 2), dark(cover, .3) if cover != '#2a2a34' else '#4a4a58', 'soft')
    for y in (10, 78): c.part(c.poly([(68, y), (78, y), (78, y + 10)] if y == 10 else [(78, y), (78, y + 10), (68, y + 10)]), band, 'metal')
    if seal: c.part(c.ell(52, 48, 11), seal, 'gem'); c.flat(c.star(52, 48, 6.5, 3, 6), dark(seal, .5), .9)
    else: c.part(c.rect(38, 28, 68, 36, 1), band, 'metal'); c.part(c.rect(38, 44, 68, 48, 1), band, 'metal', ink=False)
    if wrap:
        c.part(c.rect(14, 44, 82, 54, 1), wrap, 'matte'); c.part(c.rect(44, 6, 54, 92, 1), wrap, 'matte')
    c.pop()
def twist(c):
    b = c.poly([(50, 96), (26, 40), (50, 30), (74, 40)]); c.part(b, '#e6dcc0', 'matte'); c.flat(c.line([(50, 96), (50, 32)], 1.5), '#a89a7a', .7)
    c.part(c.poly([(30, 40), (22, 14), (40, 24), (50, 8), (60, 24), (78, 14), (70, 40), (50, 32)]), '#f2ead4', 'matte'); c.part(c.ell(50, 36, 9, 3.5), '#b8b4ac', 'soft', ink=False)
def ironbars(c):
    for dx, dy in ((0, 16), (-6, -4), (8, -22)):
        c.push(dx=dx, dy=dy, sc=.72); bar(c, '#7a8088', mark=False); c.pop()
def letter(c, seal='#b23a30', broken=True, pouch_=False):
    if pouch_: c.part(c.rect(12, 36, 88, 92, 8), '#8a6a3a', 'soft')
    c.push(rot=-8)
    b = c.rect(16, 20, 84, 72, 2); c.part(b, '#efe6cc', 'matte'); c.flat(c.line([(16, 20), (50, 50), (84, 20)], 1.6), '#a89a7a', .8, clip=b)
    for y in (58, 64): c.flat(c.line([(26, y), (74, y)], 1.3), '#a89a7a', .6)
    s = c.blob(50, 50, 11, 10, n=10, jit=.1, smooth=True); c.part(s, seal, 'gem')
    if broken: c.flat(c.line([(44, 40), (50, 50), (48, 60)], 2.2), '#efe6cc', .95)
    c.pop()
    if pouch_: c.part(c.rect(12, 62, 88, 92, 8), '#a07a44', 'soft'); c.part(c.ell(50, 66, 4), '#d9c9a0', 'metal')
def cordknots(c):
    c.part(c.line([(16, 14), (40, 30), (30, 52), (60, 56), (54, 78), (84, 90)], 6, smooth=True), '#f0ece0', 'matte')
    for x, y in ((38, 30), (44, 55), (60, 78)): c.part(c.ell(x, y, 8), '#ffffff', 'soft')
def skein(c):
    c.part(c.taper([(14, 90), (50, 52), (88, 10)], 7, 4, smooth=False), '#7a5a3a', 'wood')
    b = c.ell(50, 52, 24, 30, rot=40); c.glow(b, '#e8f4ff', halo=9, power=.25); c.part(b, '#e6eef4', 'gem')
    for k in (-12, -4, 4, 12): c.flat(c.line([(30 + k, 62 + k), (62 + k, 34 + k)], 1.4), '#9ab0c0', .7, clip=b)
def husk(c):
    b = c.poly([(40, 6), (60, 10), (70, 50), (62, 94), (42, 96), (30, 52)]); c.glow(b, '#a64dff', halo=14, power=.5); c.part(b, '#2a2434', 'gem')
    for p in ([(40, 6), (50, 50), (42, 96)], [(60, 10), (50, 50), (62, 94)], [(30, 52), (50, 50), (70, 50)]): c.glow(c.line(p, 1.4), '#a64dff', halo=4, power=.5, clip=b)

RED = '#c4483f'
ITEMS = {
    # ores, bars, logs
    'mat.copper_ore': ('ore', ore, ('#6b5a50', '#e8873a')), 'mat.bogiron_ore': ('ore', ore, ('#4a3a34', '#a8502e')), 'mat.adit_ore': ('ore', ore, ('#5a6470', '#c8d4e0')),
    'mat.cinder_ore': ('ore', ore, ('#2e2a2c', '#ff6a1f'), dict(hot='#ff6a1f')), 'mat.veridian_ore': ('ore', ore, ('#4a5a44', '#b8d850')),
    'mat.geode_shard': ('ore', shard, ('#a8b4c4',), dict(mat='gem', glowc='#cfe0ff', spark=True)),
    'mat.copper_bar': ('ore', bar, ('#c97a43',)), 'mat.bogiron_bar': ('ore', bar, ('#6a5a54',)), 'mat.ridgesteel_bar': ('ore', bar, ('#b4bcc8',)),
    'mat.ashsteel_bar': ('ore', bar, ('#4a464c',)), 'mat.veridian_bar': ('ore', bar, ('#8fa458',)),
    'mat.oak_log': ('wood', log, ('#7a5230', '#e2c48a')), 'mat.blackpine_log': ('wood', log, ('#3a3230', '#e8c860')), 'mat.stonepine_log': ('wood', log, ('#8a8a86', '#d8c8a8')),
    'mat.snag_wood': ('wood', log, ('#4a4444', '#6a6260'), dict(char=True)), 'mat.ghostoak_log': ('wood', log, ('#c8d0d4', '#f0f6fa'), dict(glowc='#9fe8ff')),
    # herbs
    'mat.yarrow': ('herb', herb_yarrow), 'mat.mourners_cap': ('herb', herb_cap), 'mat.tarnwort': ('herb', herb_tarnwort), 'mat.cinder_thistle': ('herb', herb_thistle), 'mat.dewfern': ('herb', herb_fern),
    'herb.moonbell': ('herb', herb_bell, ('#a8c8ff',), dict(glowc='#a8c8ff')), 'herb.widows_lamp': ('herb', herb_lamp), 'herb.frostbell': ('herb', herb_bell, ('#f0fbff',), dict(glowc='#9fe8ff', leaf='#8ab8b0', n=2)),
    'herb.last_light': ('herb', herb_lastlight), 'herb.lantern_moss': ('herb', herb_moss),
    'mat.charcoal': ('ore', charcoal), 'mat.flour': ('food', sack, (), dict(mark='wheat')), 'mat.salt': ('food', salt), 'mat.vial': ('potion', flask, (None,), dict(shape='vial')),
    # hides
    'junk.wolf_pelt': ('hide', pelt, ('#8d9096',)), 'junk.ash_hide': ('hide', pelt, ('#4a4648',), dict(belly='#6a6462')), 'junk.moss_hide': ('hide', pelt, ('#6a7a4a',), dict(belly='#8aa05a')),
    'junk.dappled_hide': ('hide', pelt, ('#b8854e',), dict(spots='#f6ecd6')), 'hide.coney': ('hide', pelt, ('#c9a878',), dict(small=True)), 'hide.hill_deer': ('hide', pelt, ('#a8703a',), dict(belly='#e6d0a8')),
    'hide.boar': ('hide', pelt, ('#5a4030',), dict(bristle=True)),
    # meat
    'junk.boar_meat': ('meat', meat_ham), 'mat.wolf_haunch': ('meat', meat_ham, ('#8a2a2e', '#d8c0a8'), dict(lean=True)), 'mat.hound_flank': ('meat', meat_slab, ('#8a5a5a',)),
    'mat.mossback_chop': ('meat', meat_chop, ('#b84a48', '#c8d8a0')), 'mat.venison': ('meat', meat_steak, ('#a02a36',)),
    # food
    'food.brown_loaf': ('food', loaf), 'food.fresh_eggs': ('food', eggs), 'food.harrow_cheese': ('food', cheese), 'food.mountain_jerky': ('food', jerky), 'food.griddle_bread': ('food', flatbread),
    'food.boar_stew': ('food', bowl_stew), 'food.hearth_cake': ('food', flatbread, ('#c98a40',), dict(marks=False, currants=True, small=True)), 'food.wolf_skewer': ('food', skewer), 'food.harrow_pasty': ('food', pasty),
    'food.smoked_loin': ('food', roast), 'food.salt_flank': ('food', meat_slab, ('#a85a40',), dict(ribs=False, crust='#e6e2d8')), 'food.cinder_loaf': ('food', loaf, ('#3a302c',), dict(dust=False, hot='#ff7a2a')),
    'food.mossback_chop': ('food', meat_chop, ('#9a5a30', '#d8b070'), dict(glaze='#8ad060', herb=True)), 'food.venison_pie': ('food', pie), 'food.sap_cake': ('food', sapcake),
    # potions
    'potion.minor': ('potion', flask, ('#d8332e',), dict(shape='vial', level=.5)), 'potion.healing': ('potion', flask, ('#e02a2a',)), 'potion.salt': ('potion', flask, ('#e8eef2',), dict(shape='tall', label='#c9b48a')),
    'potion.tarn': ('potion', flask, ('#3a8ae0',), dict(shape='tri')), 'potion.dewfern': ('potion', flask, ('#4ac060',), dict(shape='round', cork='#5f9a48')),
    # tools and bags
    'tool.pick': ('tool', tool_pick), 'tool.hatchet': ('tool', tool_hatchet),
    'bag.simples_wallet': ('bag', bag_wallet), 'bag.log_sling': ('bag', bag_sling), 'bag.larder_scrip': ('bag', bag_scrip), 'bag.ore_poke': ('bag', bag_poke),
    # junk
    'junk.wolf_fang': ('junk', fang), 'junk.boar_tusk': ('junk', tusk), 'junk.tithe_token': ('junk', coin, ('#8d96a3', 'scale')), 'junk.toll_coin': ('junk', coin, ('#c9963a', 'crown'), dict(worn=True)),
    'junk.desert_wrap': ('junk', rag), 'junk.petrified_shard': ('junk', shard, ('#8a8680',)), 'junk.bone_mask': ('junk', maskpiece), 'junk.static_glass': ('junk', shard, ('#b8a8f0', 'gem'), dict(glowc='#a080ff', spark=True)),
    'junk.pale_filament': ('junk', filament), 'junk.company_badge': ('junk', badge), 'junk.sexton_bell': ('junk', bell), 'junk.mossback_tusk': ('junk', tusk, ('#9ab86a', '#5a6a3a')),
    'junk.velvet_tine': ('junk', tine), 'junk.spider_chitin': ('junk', chitin), 'junk.venom_gland': ('junk', gland), 'junk.briar_thorns': ('junk', thorns), 'junk.briar_heart': ('junk', knot),
    'junk.grey_bark': ('junk', bark), 'junk.cold_trinket': ('junk', trinket), 'junk.cold_sliver': ('junk', shard, ('#d8f0ff', 'gem'), dict(glowc='#9fe8ff', thin=True)),
    # the bears and the drowned dead (2026-10-04)
    'hide.bear': ('hide', pelt, ('#6a4a30',), dict(belly='#8a6a4a')), 'junk.bear_claw': ('junk', fang, (), dict(bone='#4a3e34', root='#7a5a44', curve=1.5)),
    'mat.bear_haunch': ('meat', meat_ham, ('#7a2a26', '#e0c8a8')), 'junk.creek_bone': ('junk', bone, ('#9a8a66',)),
    'junk.grave_goods': ('junk', coin, ('#8a8a80', 'crown'), dict(worn=True)),
}
QUEST = {
    'item.brood_heart': (crystal,), 'item.calcified_shard': (crystal, (), dict(n=1)), 'item.salt_cask': (cask,), 'item.petrified_bone': (bone,), 'item.first_sea_salt': (sack, ('#e6e2d8', '#ffffff'), dict(knots=3)),
    'item.small_shackle': (shackle,), 'item.mourners_cap': (herb_cap,), 'item.boar_tallow': (tallow,), 'item.khaven_goods': (goods,), 'item.bureau_ledger': (book, ('#2a2a34', '#c9a040'), dict(seal='#8d96a3')),
    'item.egg_basket': (eggs, (), dict(basket=True)), 'item.flour_sample': (twist,), 'item.clean_grain': (sack, ('#b89a5e', '#e0b850'), dict(mark='wheat')), 'item.yarrow': (herb_yarrow,),
    'item.concord_iron': (ironbars,), 'item.toll_ledger': (book, ('#7a5a34', '#b5893c')), 'item.escort_orders': (letter,), 'item.rockhide': (pelt, ('#8a8a88',), dict(stone=True, belly='#a8a8a4')),
    'item.mender_letters': (letter, ('#e8eef2',), dict(broken=False, pouch_=True)), 'item.pilgrim_journal': (book, ('#1e1c20', '#6a6a6a'), dict(wrap='#8a7a5a')), 'item.salt_cord': (cordknots,),
    'item.glade_light': (herb_cap, ('#3fae6a', '#bfffd0', '#d9e8d0'), dict(glowc='#5fe08a', spots='#d8ffe0')), 'item.moss_antler': (tine, ('#b89a7a',), dict(moss=True)), 'item.web_silk': (skein,), 'item.charnel_silk': (skein,),
    'item.veridian_sap': (flask, ('#2fd070',), dict(shape='round', glowc='#40e080', cork='#8a5a3a')), 'item.cold_glass': (shard, ('#d8f0ff', 'gem'), dict(glowc='#9fe8ff', thin=True, spark=True)),
    'item.void_husk': (husk,), 'item.shore_salt': (salt, (), dict(crust=True)),
}
# what a kind looks like when an item has no picture of its own (new content): the resolver's second step
KINDS = {
    'ore': ('ore', ore, ('#5a5a5e', '#c8c8d0')), 'timber': ('wood', log, ('#7a5a3a', '#e2c48a')), 'herb': ('herb', lambda c: leafshape(c, 30, 86, 78, 14, 38, GREEN, 'gem')), 'larder': ('food', sack),
    'hide': ('hide', pelt, ('#a8804e',)), 'food': ('food', loaf, ('#c98a40',), dict(dust=False)), 'potion': ('potion', flask, ('#b040d0',)), 'material': ('junk', sack, ('#a89a7a', '#a89a7a')),
    'tool': ('tool', tool_hatchet, ('#b0a890', '#6a5a40')), 'bag': ('bag', pouch, ('#8a6a44',)), 'junk': ('junk', rag, ('#7a7a7a', '#5a5a5a')), 'quest': ('quest', letter, ('#e0b040',), dict(broken=False)),
}

# ====================================================================================================
# sheet.py
# ====================================================================================================
# Sample sheet: each icon at 96 px and at the HUD's true 38 px (64 px texture scaled bilinear, as IMGUI does) on the slot
# colour with its quality border.
from PIL import Image, ImageDraw, ImageFont

QUAL = [(158, 158, 158), (255, 255, 255), (31, 255, 0), (0, 112, 222), (163, 54, 237)]
SLOT = (23, 26, 28); PANEL = (18, 15, 13); BAR = (41, 51, 61); GOLDB = (158, 133, 84)

def font(sz):
    for f in ('segoeui.ttf', 'arial.ttf'):
        try: return ImageFont.truetype(f, sz)
        except Exception: pass
    return ImageFont.load_default()

def paste_slot(sheet, im64, x, y, px, q=1, ability=False, count=None):
    d = ImageDraw.Draw(sheet)
    border = GOLDB if ability else tuple(int(v * .9) for v in QUAL[q])
    d.rectangle((x - 2, y - 2, x + px + 1, y + px + 1), fill=border)
    d.rectangle((x, y, x + px - 1, y + px - 1), fill=BAR if ability else SLOT)
    im = im64 if im64.size[0] == px else im64.resize((px, px), Image.BILINEAR)
    sheet.paste(im, (x, y), im)
    if count:
        f = font(12 if px > 40 else 11); w = d.textlength(str(count), font=f)
        d.text((x + px - w - 2, y + px - 15), str(count), font=f, fill=(0, 0, 0)); d.text((x + px - w - 3, y + px - 16), str(count), font=f, fill=(255, 255, 255))

def make_sheet(entries, path, cols=7):
    """entries: (label, Icon, quality, isAbility, count)"""
    cw, ch = 168, 138; rows = (len(entries) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * cw + 16, rows * ch + 16), PANEL); d = ImageDraw.Draw(sheet); f = font(12)
    for i, (label, ic, q, ab, count) in enumerate(entries):
        x = 12 + (i % cols) * cw; y = 12 + (i // cols) * ch
        big = ic.render(96); small = ic.render(OUT)
        paste_slot(sheet, big, x, y, 96, q, ab)
        paste_slot(sheet, small, x + 108, y, 38, q, ab, count)
        paste_slot(sheet, small, x + 104, y + 48, 50 if ab else 52, q, ab, count)
        d.text((x, y + 102), label[:26], font=f, fill=(215, 205, 185))
    sheet.save(path)
    return sheet

# ====================================================================================================
# main_part.py
# ====================================================================================================
# ---- the build: read the game's content, paint an icon for every id, write PNG + .meta, report what is missing ----
import os, sys, json, glob, argparse, hashlib

META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""
FOLDER_META = "fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"

def guid_for(rel):
    """A stable guid per icon path, so regenerating never changes a .meta (an existing .meta is kept as it is)."""
    return hashlib.md5(('crulanda-icon:' + rel).encode()).hexdigest()
def safe(id_): return id_.replace('.', '-')        # Resources paths: no dots inside a name
SLOT_FALLBACK_BG = '#2a2c30'

def parse_look(s):
    glow = s.endswith('+glow'); s = s[:-5] if glow else s
    left, pal = s.split('/'); fam, _, var = left.partition(':')
    return fam, var or FAMILIES[fam][0], pal, glow

def load_content(assets):
    C = {}
    items = []; gearlooks = {}
    for f in sorted(glob.glob(os.path.join(assets, 'EncounterContent', 'Items', '*.json'))):
        j = json.load(open(f, encoding='utf-8'))
        items += j.get('items') or []
        for g in j.get('gear') or []:
            if g.get('look'): gearlooks[g['id']] = g['look']
    looks = json.load(open(os.path.join(assets, 'Resources', 'Gear', 'looks.json'), encoding='utf-8'))
    for l in looks.get('looks') or []: gearlooks[l['item']] = l['look']
    quest = []
    for f in sorted(glob.glob(os.path.join(assets, 'EncounterContent', 'Quests', '*.json'))):
        quest += json.load(open(f, encoding='utf-8')).get('items') or []
    prof = json.load(open(os.path.join(assets, 'EncounterContent', 'Professions', 'professions.json'), encoding='utf-8'))
    talents = []
    for f in sorted(glob.glob(os.path.join(assets, 'EncounterContent', 'Talents', '*.json'))):
        for b in json.load(open(f, encoding='utf-8'))['branches']:
            seen = {}
            for n in b['nodes']:
                k = seen.get(n.get('icon'), 0); seen[n.get('icon')] = k + 1
                talents.append(dict(id=n['id'], name=n['name'], icon=n.get('icon'), kind=n.get('kind'), branch=b['id'], k=k))
    C.update(items=items, gearlooks=gearlooks, looks=looks, quest=quest, prof=prof, talents=talents)
    return C

def gear_icon(name, looks, look, item_id=''):
    fam, var, pal, glow = look
    pals = {p['id']: p for p in looks['palettes']}
    tint = next((t['metal'] for t in looks.get('tints') or [] if item_id.startswith(t['prefix'])), None)
    P = mkpal(pals.get(pal) or pals['oakhaven'], tint)
    c = Icon(name, mix('#343a44', P['cloth'], .22) * .9)
    draw_gear(c, P, fam, var, glow)
    return c
def plain_icon(name, spec, bg=None):
    bgk, fn = spec[0], spec[1]; a = spec[2] if len(spec) > 2 else (); k = spec[3] if len(spec) > 3 else {}
    c = Icon(name, bg or BG[bgk]); fn(c, *a, **k); return c

PROF_ICONS = {
    'mining': ('tool', tool_pick), 'woodcutting': ('tool', tool_hatchet), 'herbalism': ('herb', herb_yarrow), 'cooking': ('food', bowl_stew),
    'blacksmithing': ('ore', bar, ('#b4bcc8',)), 'alchemy': ('potion', flask, ('#4ac060',)),
}
SLOT_SILHOUETTE = {'head': ('head.kettle', 'plain'), 'neck': ('neck.pendant', 'drop'), 'shoulders': ('shoulder.pauldron', 'dome'), 'chest': ('chest.tunic', 'plain'), 'hands': ('hands.gloves', 'plain'),
                   'legs': ('legs.breeches', 'plain'), 'feet': ('feet.boots', 'plain'), 'mainhand': ('sword.arming', 'straight'), 'offhand': ('shield.heater', 'plain')}
GREY = dict(id='grey', cloth='#6a6a6e', cloth2='#5a5a5e', leather='#5e5a56', metal='#7a7a7e', trim='#8a8a8e', wood='#625e5a', glow='#b0b0b0')

def build_all(C):
    """Every icon as (resource path under Icons/, Icon, label, quality, isAbility). Returns the list and the ids left without one."""
    out = []; missing = []; looks = C['looks']
    fallbacks = {f['slot']: f['family'] for f in looks.get('fallbacks') or []}
    for it in C['items']:
        id_ = it['id']; kind = it.get('kind', 'gear'); q = it.get('quality', 1)
        if kind == 'gear':
            lk = C['gearlooks'].get(id_)
            look = parse_look(lk) if lk else (fallbacks.get(it.get('slot'), 'sword.arming'), FAMILIES[fallbacks.get(it.get('slot'), 'sword.arming')][0], 'oakhaven', False)
            out.append(('item/' + safe(id_), gear_icon(id_, looks, look, id_), it['name'], q, False))
        elif id_ in ITEMS: out.append(('item/' + safe(id_), plain_icon(id_, ITEMS[id_]), it['name'], q, False))
        else: missing.append('item ' + id_)
    for r in C['prof'].get('recipes') or []:
        if not any(i['id'] == r['output'] for i in C['items']): missing.append('recipe output ' + r['output'])
    for qi in C['quest']:
        if qi['id'] in QUEST: out.append(('quest/' + safe(qi['id']), plain_icon('q' + qi['id'], ('quest',) + QUEST[qi['id']]), qi['name'], 1, False))
        else: missing.append('quest item ' + qi['id'])
    for aid, (bg, fn) in ABILITIES.items():
        c = Icon(aid, bg); fn(c); out.append(('ability/' + safe(aid), c, aid, 1, True))
    for t in C['talents']:
        if t['icon'] not in MOTIFS: missing.append('talent ' + t['id'] + ' (icon word ' + str(t['icon']) + ')')
        c = Icon(t['id'], BRANCH.get(t['branch'], ('#3a3a44',))[0]); draw_talent(c, t['icon'], t['branch'], t['kind'], t['k'])
        out.append(('talent/' + safe(t['id']), c, t['name'], 1, True))
    for p in C['prof'].get('professions') or []:
        if p['id'] in PROF_ICONS: out.append(('trade/' + safe(p['id']), plain_icon('trade' + p['id'], PROF_ICONS[p['id']]), p['name'], 1, True))
        else: missing.append('trade ' + p['id'])
    # generated gear: the family its piece word can give (first variant) in each palette a material word can give
    fams = sorted({f for w in looks.get('words') or [] for f in w['families']})
    pals = sorted({m['palette'] for m in looks.get('materials') or []})
    for fam in fams:
        for pal in pals:
            out.append(('gear/' + safe(fam) + '__' + pal, gear_icon(fam + pal, looks, (fam, FAMILIES[fam][0], pal, False)), fam + '/' + pal, 1, False))
    # by kind (new content without a picture yet) and by slot (the Armoury's unknown pieces: grey)
    for k, spec in KINDS.items(): out.append(('kind/' + k, plain_icon('kind' + k, spec), 'kind:' + k, 1, False))
    for slot, (fam, var) in SLOT_SILHOUETTE.items():
        c = Icon('slot' + slot, SLOT_FALLBACK_BG); draw_gear(c, mkpal(GREY), fam, var, False); out.append(('slot/' + slot, c, 'slot:' + slot, 0, False))
    return out, missing

def write_icons(icons, out_dir, size, meta=True):
    n = 0
    def ensure_meta(path, rel, folder=False):
        if meta and not os.path.exists(path + '.meta'):
            open(path + '.meta', 'w', encoding='utf-8', newline='\n').write((FOLDER_META if folder else META).format(guid=guid_for(rel)))
    os.makedirs(out_dir, exist_ok=True)
    for rel, ic, _, _, _ in icons:
        path = os.path.join(out_dir, rel.replace('/', os.sep) + '.png'); d = os.path.dirname(path)
        if not os.path.isdir(d): os.makedirs(d)
        ensure_meta(d, 'dir:' + os.path.dirname(rel), folder=True)
        ic.render(size).save(path, optimize=True); ensure_meta(path, rel); n += 1
    return n

SAMPLE = ['ability/ability-strike', 'ability/ability-challenge', 'ability/ability-guard', 'ability/ability-intercept', 'ability/ability-breaching_blow', 'ability/ability-muster',
          'ability/druid-rake', 'ability/druid-seedling', 'item/mat-copper_ore', 'item/mat-cinder_ore', 'item/mat-copper_bar', 'item/mat-ridgesteel_bar', 'item/mat-oak_log', 'item/mat-yarrow',
          'item/mat-mourners_cap', 'item/mat-dewfern', 'item/junk-wolf_pelt', 'item/junk-boar_meat', 'item/food-boar_stew', 'item/food-brown_loaf', 'item/potion-minor', 'item/tool-pick',
          'item/tool-hatchet', 'item/bag-ore_poke', 'item/junk-wolf_fang', 'item/item-training_blade', 'item/loot-oak-broken_oath_sabre', 'item/craft-bogiron_hatchet', 'item/craft-veridian_shield',
          'item/craft-bogiron_helm', 'item/craft-bogiron_hauberk', 'item/loot-oak-due_boots', 'item/loot-oak-whitefoot_fang', 'item/loot-kha-mourners_iron_band', 'quest/item-bureau_ledger',
          'quest/item-brood_heart', 'talent/tk-timed-guard', 'talent/rm-slow-sap', 'trade/mining', 'gear/sword-short__oakhaven', 'gear/sword-short__veridian', 'slot/head']

def main():
    here = os.path.dirname(os.path.abspath(__file__))
    ap = argparse.ArgumentParser(description='Paints the Crulanda HUD icons (items, quest items, abilities, talents, trades).')
    ap.add_argument('--assets', default=os.path.normpath(os.path.join(here, '..', '..', 'New Unity Project', 'Assets', 'Crulanda')))
    ap.add_argument('--out', default=None, help='default: <assets>/Resources/Icons')
    ap.add_argument('--size', type=int, default=OUT)
    ap.add_argument('--sheets', default=None, help='a folder for preview sheets (sample.png and one sheet per group)')
    ap.add_argument('--check', action='store_true', help='only list ids without an icon; write nothing')
    ap.add_argument('--no-meta', action='store_true')
    a = ap.parse_args()
    C = load_content(a.assets)
    icons, missing = build_all(C)
    groups = {}
    for rel, *_ in icons: groups[rel.split('/')[0]] = groups.get(rel.split('/')[0], 0) + 1
    print('icons:', len(icons), groups)
    print('missing:', len(missing)); [print('  ' + m) for m in missing]
    if not a.check:
        out = a.out or os.path.join(a.assets, 'Resources', 'Icons')
        print('wrote', write_icons(icons, out, a.size, not a.no_meta), 'PNG files of', a.size, 'px under', out)
    if a.sheets:
        os.makedirs(a.sheets, exist_ok=True); by = {rel: (lab, ic, q, ab, None) for rel, ic, lab, q, ab in icons}
        counts = {'item/mat-copper_ore': 20, 'item/mat-oak_log': 9, 'item/mat-yarrow': 17, 'item/junk-wolf_pelt': 4, 'item/junk-boar_meat': 4, 'item/food-brown_loaf': 5, 'item/potion-minor': 3, 'item/junk-wolf_fang': 6, 'item/mat-copper_bar': 3}
        make_sheet([(by[r][0], by[r][1], by[r][2], by[r][3], counts.get(r)) for r in SAMPLE if r in by], os.path.join(a.sheets, 'sample.png'), cols=7)
        for g in groups:
            make_sheet([by[r] for r in by if r.startswith(g + '/')], os.path.join(a.sheets, 'all_' + g + '.png'), cols=12)
        hud_mock(by, os.path.join(a.sheets, 'hud_mock.png'))
    return 1 if missing else 0

def hud_mock(by, path):
    """The action bar (50 px) and a bags window's rows (52 px and 38 px pouches) as the HUD lays them out."""
    W, H = 760, 330; im = Image.new('RGB', (W, H), (18, 15, 13)); d = ImageDraw.Draw(im); f = font(12)
    def row(ids, x, y, px, gap, ab=False, counts=()):
        for i, r in enumerate(ids):
            if r in by: paste_slot(im, by[r][1].render(OUT), x + i * (px + gap), y, px, by[r][2], ab, counts[i] if i < len(counts) else None)
            else: paste_slot(im, Image.new('RGBA', (OUT, OUT), (0, 0, 0, 0)), x + i * (px + gap), y, px, 1, ab)
    d.text((14, 8), 'action bar, 50 px', font=f, fill=(215, 205, 185))
    row(['ability/ability-strike', 'ability/ability-challenge', 'ability/ability-guard', 'ability/ability-intercept', 'ability/ability-breaching_blow', 'ability/ability-muster'], 16, 28, 50, 6, True)
    row(['ability/druid-barkhide', 'ability/druid-thornclaw', 'ability/druid-rootmend', 'ability/druid-thornsong', 'ability/druid-bough_strike', 'ability/druid-bellowing_roar'], 380, 28, 50, 6, True)
    d.text((14, 92), 'bags, 52 px', font=f, fill=(215, 205, 185))
    row(['item/item-training_blade', 'item/junk-wolf_pelt', 'item/tool-pick', 'item/craft-copper_buckler', 'item/loot-oak-poachers_hood' if 'item/loot-oak-poachers_hood' in by else 'item/item-poachers_hood', 'item/junk-wolf_fang',
         'item/potion-minor', 'item/food-brown_loaf', 'item/craft-bogiron_hauberk', 'item/loot-oak-due_boots', 'item/item-echo_jar', 'item/junk-toll_coin'], 16, 112, 52, 6, False, (None, 4, None, None, None, 6, 3, 5))
    d.text((14, 178), 'trade bags, 38 px', font=f, fill=(215, 205, 185))
    row(['item/mat-yarrow', 'item/mat-mourners_cap', 'item/mat-tarnwort', 'item/mat-cinder_thistle', 'item/mat-dewfern', 'item/mat-vial'], 16, 198, 38, 4, False, (17, 3, 2, 5, 8, 4))
    row(['item/mat-copper_ore', 'item/mat-bogiron_ore', 'item/mat-adit_ore', 'item/mat-cinder_ore', 'item/mat-veridian_ore', 'item/mat-copper_bar', 'item/mat-ridgesteel_bar', 'item/mat-charcoal'], 280, 198, 38, 4, False, (20, 13, 4, 2, 6, 3, 2, 9))
    row(['item/mat-oak_log', 'item/mat-blackpine_log', 'item/mat-stonepine_log', 'item/mat-snag_wood', 'item/mat-ghostoak_log'], 16, 250, 38, 4, False, (9, 4, 2, 3, 1))
    row(['item/junk-boar_meat', 'item/mat-wolf_haunch', 'item/mat-venison', 'item/food-boar_stew', 'item/food-fresh_eggs', 'item/food-harrow_cheese', 'item/mat-flour', 'item/mat-salt'], 280, 250, 38, 4, False, (4, 2, 3, 2, 6, 1, 5, 3))
    im.save(path)

if __name__ == '__main__':
    sys.exit(main())
