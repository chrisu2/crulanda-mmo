"""The Sealed Adit, reworked (Docs/ADIT_REWORK.md; Chris, 2026-10-14: "i think adit dungeon need to be more complex like wow's wailing
caves or the deadmine. adit is nothing like those"): the dungeon zone's layout, made here and written to
New Unity Project/Assets/Crulanda/EncounterContent/Zones/adit.json. Version 2: a web of wings round one great cave at three heights,
not one long queue.

The caves are cavern props with their own plans (rows x, z, half-width, height, floor drop below the mouth), here drawn in world
metres (x east, z north, floors in metres under the yard) and turned into each prop's frame. A cave opens out of another (`within`,
its mouth `rise` metres up the other's wall), and may end in another (`joins`: its last rows stay full width out in the other's air, a
level way back round or a lip over a hall). Decks (trestles, a rope bridge, catwalks, an island), one-way drops, pools and streams,
gates, named anchors, the wings the run is led through, the Wake and the secrets are all data the builder reads (ZoneBuilder.AditDecks,
ZoneBuilder.AditWater, ZoneBuilder.Adit). The path of every cave is traced exactly as Crulanda.World.Hollow traces it, and its floors and
open air are worked out as Hollow does (Open, FloorGivesWay), so camps, decks and secrets are placed on the floors they stand on.

    python adit_layout.py            checks the layout, writes staged/adit.json and the pictures (Docs/art/adit-plan.png, adit-levels.png)
    python adit_layout.py --live     also writes it into the game's zones (Zones/adit.json); the Peaks' way in is unchanged

Checks (it stops on a failure): every child's mouth on its parent's floor; every join's end in its target's air at the planned lip; no
cave's rock in another's way but where they meet; no floor steeper than 23 degrees (decks 20); every camp on its floor or its deck;
every deck end on a floor or another deck; everything inside the zone with its rock (800 m, +-395).

Labels: every place, person, camp and mechanic here is GAME-ONLY; CANON: the old workings under the Peaks and the deep line (book2
ch.19), the Sandthrone, the Cult of Ash, the goblins, the Hollow Men, the Wasting, the Silent Pilgrims. No World of Warcraft names,
layouts or assets: lessons only.
"""
import io, json, math, os, sys
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
ZONES = os.path.join(ROOT, 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent', 'Zones')
DOCS = os.path.join(ROOT, 'New Unity Project', 'Docs')
SIZE = 800
RESPAWN = 1200         # ADIT_REWORK Q1: Chris, 2026-10-15, "20 minute respawn"
RARE_RESPAWN = 1200
GREY_LINES = (45, 100, 160)   # (S7) the Grey Spreads' lines, metres along the Grey Breach

# ---------------------------------------------------------------- the trace (Hollow's constructor)
def catmull(p0, p1, p2, p3, t):
    t2, t3 = t * t, t * t * t
    return [.5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t2 + (3 * b - a - 3 * c + d) * t3) for a, b, c, d in zip(p0, p1, p2, p3)]

def trace(at, yaw, plan, mouth_y):
    """Rings (x, z, half, height, y, along), as Hollow lays them: rows smoothed (Catmull-Rom), the drop straight between rows."""
    n = len(plan); P = lambda i: plan[max(0, min(n - 1, i))][:4]; D = lambda i: plan[max(0, min(n - 1, i))][4]
    cy, sy = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    rings, along, last = [], 0.0, None
    for seg in range(n - 1):
        p0, p1, p2, p3 = P(seg - 1), P(seg), P(seg + 1), P(seg + 2)
        steps = max(1, math.ceil(math.hypot(p2[0] - p1[0], p2[1] - p1[1]) / .7))
        k = 0
        while k < steps or (seg + 2 == n and k == steps):
            u = k / steps; q = catmull(p0, p1, p2, p3, u)
            x = at[0] + q[0] * cy + q[1] * sy; z = at[1] - q[0] * sy + q[1] * cy
            if last is not None: along += math.hypot(x - last[0], z - last[1])
            rings.append((x, z, max(.2, q[2]), max(.3, q[3]), mouth_y + D(seg) + (D(seg + 1) - D(seg)) * u, along)); last = (x, z); k += 1
    return rings

class Cave:
    """A cave as Hollow makes it: its rings, the cave it opens out of and the one it ends in, and which rings have a floor of their own."""
    ALL = []
    def __init__(self, key, name, style, pts, within=None, rise=0.0, joins=None, canon=None):
        self.key, self.name, self.style, self.pts, self.within, self.rise, self.joins = key, name, style, pts, within, rise, joins
        self.canon = canon
        self.parent = self.join = None
    def build(self, caves):
        p0, p1 = self.pts[0], self.pts[1]
        self.at = (p0[0], p0[1]); dx, dz = p1[0] - p0[0], p1[1] - p0[1]
        self.yaw = math.degrees(math.atan2(dx, dz))
        if self.within:
            self.parent = caves[self.within]
            fy = self.parent.floor_smooth(*self.at)
            if fy is None: sys.exit('%s: its mouth %s is not on %s\'s floor' % (self.name, self.at, self.parent.name))
            self.mouth_y = fy + self.rise
        else: self.mouth_y = 0.0
        cy, sy = math.cos(math.radians(self.yaw)), math.sin(math.radians(self.yaw))
        def frame(x, z): ddx, ddz = x - self.at[0], z - self.at[1]; return (ddx * cy - ddz * sy, ddx * sy + ddz * cy)
        rows = []
        first = self.pts[0]
        rows.append((0.0, -1.0, first[2], first[3], 0.0))   # a metre behind the mouth, as every plan starts
        for i, (x, z, half, height, floor) in enumerate(self.pts):
            fx, fz = frame(x, z)
            drop = 0.0 if i == 0 else floor - self.mouth_y
            rows.append((round(fx, 3), round(fz, 3), half, height, round(drop, 3)))
        if not self.joins:   # the closing tip, four metres on
            lx, lz = rows[-1][0], rows[-1][1]; px, pz = rows[-2][0], rows[-2][1]; m = math.hypot(lx - px, lz - pz) or 1
            rows.append((round(lx + (lx - px) / m * 4, 3), round(lz + (lz - pz) / m * 4, 3), .3, .4, rows[-1][4]))
        self.plan = rows
        self.rings = trace(self.at, self.yaw, rows, self.mouth_y)
        r = np.array(self.rings); self.np = r
        self.length = self.rings[-1][5]
        self.box = (r[:, 0].min() - r[:, 2].max() - 6, r[:, 1].min() - r[:, 2].max() - 6, r[:, 0].max() + r[:, 2].max() + 6, r[:, 1].max() + r[:, 2].max() + 6)
        self.floored = None
    # ---- Hollow's questions, as it answers them
    def nearest(self, x, z):
        d = (self.np[:, 0] - x) ** 2 + (self.np[:, 1] - z) ** 2; i = int(np.argmin(d)); return i, math.sqrt(d[i])
    def in_box(self, x, z): b = self.box; return b[0] <= x <= b[2] and b[1] <= z <= b[3]
    def open(self, x, y, z, shrink=.86):
        if len(self.rings) < 2 or not self.in_box(x, z): return False
        i, _ = self.nearest(x, z); j = i + 1 if i + 1 < len(self.rings) else i - 1
        fx, fz = self.rings[j][0] - self.rings[i][0], self.rings[j][1] - self.rings[i][1]
        if j < i: fx, fz = -fx, -fz
        m = math.hypot(fx, fz)
        if m < 1e-8: return False
        fx, fz = fx / m, fz / m; dx, dz = x - self.rings[i][0], z - self.rings[i][1]
        if abs(dx * fx + dz * fz) > .75: return False
        u = abs(dx * fz - dz * fx) / self.rings[i][2]
        if u >= shrink: return False
        up = y - self.rings[i][4]
        if up < -.6: return False
        return up < self.rings[i][3] * max(0, 1 - u ** (8 / 3)) ** .425 * shrink
    def floor_at(self, x, z):
        if not self.in_box(x, z): return None
        i, d = self.nearest(x, z)
        if d > self.rings[i][2] or (self.floored is not None and not self.floored[i]): return None
        return self.rings[i][4]
    def floor_smooth(self, x, z):
        if not self.in_box(x, z): return None
        i, d = self.nearest(x, z)
        if d > self.rings[i][2] or (self.floored is not None and not self.floored[i]): return None
        n = len(self.rings); a = self.rings[i]
        if n < 2: return a[4]
        if i == 0: j = 1
        elif i == n - 1: j = i - 1
        else:
            b = self.rings[i + 1]; j = i + 1 if (x - a[0]) * (b[0] - a[0]) + (z - a[1]) * (b[1] - a[1]) >= 0 else i - 1
        b = self.rings[j]; ab2 = (b[0] - a[0]) ** 2 + (b[1] - a[1]) ** 2
        f = max(0, min(1, ((x - a[0]) * (b[0] - a[0]) + (z - a[1]) * (b[1] - a[1])) / max(1e-6, ab2)))
        return a[4] + (b[4] - a[4]) * f
    def at_(self, along, aside=0.0):
        """Hollow.At: the floor point along metres in, aside to the right."""
        R = self.rings; i = 0
        while i + 1 < len(R) and R[i + 1][5] < along: i += 1
        j = min(i + 1, len(R) - 1)
        f = 0 if j == i else max(0, min(1, (along - R[i][5]) / max(1e-6, R[j][5] - R[i][5])))
        x = R[i][0] + (R[j][0] - R[i][0]) * f; z = R[i][1] + (R[j][1] - R[i][1]) * f; y = R[i][4] + (R[j][4] - R[i][4]) * f
        fx, fz = R[j][0] - R[i][0], R[j][1] - R[i][1]
        if fx * fx + fz * fz < 1e-12 and i > 0: fx, fz = R[i][0] - R[i - 1][0], R[i][1] - R[i - 1][1]
        m = math.hypot(fx, fz) or 1; fx, fz = fx / m, fz / m
        return (x + fz * aside, y, z - fx * aside)
    def local(self, x, z):
        """Hollow.Local: a ground point as (along, aside)."""
        i, _ = self.nearest(x, z); j = i + 1 if i + 1 < len(self.rings) else i - 1
        fx, fz = self.rings[j][0] - self.rings[i][0], self.rings[j][1] - self.rings[i][1]
        if j < i: fx, fz = -fx, -fz
        m = math.hypot(fx, fz) or 1; fx, fz = fx / m, fz / m; dx, dz = x - self.rings[i][0], z - self.rings[i][1]
        return self.rings[i][5] + dx * fx + dz * fz, dx * fz - dz * fx
    def half_at(self, along):
        R = self.rings; i = 0
        while i + 1 < len(R) and R[i + 1][5] < along: i += 1
        return R[i][2]

def floor_gives_way(h, x, y, z):
    """Hollow.FloorGivesWay."""
    for o in Cave.ALL:
        if o is h or not o.open(x, y, z, .8): continue
        if o.parent is h or o.join is h:
            j, _ = o.nearest(x, z); r = o.rings[j]
            if h.open(r[0], r[4] + .3, r[1], .8): continue
        return True
    return False

def linked(a, b): return a.parent is b or b.parent is a or a.join is b or b.join is a

# ---------------------------------------------------------------- the caves (world x, z; half-width; height; floor under the yard)
# (Docs/ADIT_REWORK.md section 3; every place GAME-ONLY.)
OLD = 'CANON-EXPANDED: the old workings under the Shattered Peaks (book2 ch.19); GAME-ONLY: this place and what stands in it'
CAVES = [
    # W The Sealed Adit: the cut, the Cage Bowl, the Sappers' Stair, Lusk's Tally Hall, the Dark Drift and the Overlook, a lip 14 m up
    # the Singing Gallery's south-west wall (the reveal).
    Cave('W', 'The Sealed Adit', 'workings', [
        (0, -370, 3.0, 4.2, 0), (0, -363, 3.15, 4.3, 0), (1, -350, 3.3, 4.5, -1), (0, -337, 3.6, 4.6, -2),
        (-1, -327, 12, 9, -3), (-3, -313, 20, 12, -6), (-4, -298, 20, 12, -11), (-3, -284, 19, 12, -11.5), (-1, -270, 14, 10, -8.5), (0, -261, 5, 6, -9),
        (1, -250, 3.4, 4.6, -12), (2, -240, 6, 5, -15), (2, -230, 3.4, 4.6, -18), (1, -220, 6, 5, -21), (0, -210, 3.4, 4.6, -24), (0, -203, 4.5, 5.2, -26),
        (0, -195, 11, 8, -26), (0, -184, 14, 9, -26), (0, -171, 14, 9, -26), (0, -162, 10, 8, -26),
        (0, -155, 3.4, 4.6, -26.2), (0, -146, 3.2, 4.4, -27), (-1, -135, 3.2, 4.4, -28.5), (-4, -115, 3.2, 4.4, -31), (-6, -90, 3.2, 4.4, -35),
        (-6, -65, 3.2, 4.4, -39), (-3, -44, 3.4, 4.6, -42),
        (6, -28, 4.5, 6, -43.5), (16, -18, 5.5, 7, -44), (27, -14, 6, 8, -44), (36, -14, 6, 8, -44)], joins='G', canon=OLD),
    # FW The Flooded Working (optional): off the bowl's north-west, round by the bailing chamber, into the tally hall's west wall.
    Cave('FW', 'The Flooded Working', 'flooded', [
        (-15, -276, 3.4, 4.6, None), (-24, -276, 3.4, 4.6, -10), (-38, -272, 3.4, 4.6, -12), (-51, -263, 3.4, 4.6, -14),
        (-58, -250, 8, 6.5, -15.5), (-60, -238, 10, 7, -16), (-59, -226, 9, 7, -16), (-54, -215, 3.4, 4.6, -17.5),
        (-46, -205, 3.4, 4.6, -20), (-36, -196, 3.4, 4.6, -23), (-25, -189, 3.6, 4.8, -25.5), (-15, -183, 3.8, 5, -26), (-7, -180, 3.8, 5, -26)],
        within='W', joins='W', canon=OLD),
    # P The Pen (optional): off the tally hall's east wall, a stockaded pen.
    Cave('P', 'The Pen', 'pen', [
        (10, -178, 2.8, 4, None), (18, -178, 2.8, 4, -26), (32, -180, 2.8, 4, -26), (42, -181, 7, 6, -26), (52, -181, 10, 7, -26), (63, -180, 9, 7, -26), (70, -180, 5, 5, -26)],
        within='W', canon=OLD),
    # G The Singing Gallery, the hub: the sump drift off the dark drift (barred from the hub's side), then one great natural cave, 80 m
    # across and 30 high, its floor 58 m under the yard; the north neck on to the Deep Way.
    Cave('G', 'The Singing Gallery', 'gallery', [
        (0.5, -135, 3.2, 4.4, None), (8, -135, 3.2, 4.4, -29), (22, -134, 3.4, 4.5, -31.5), (38, -130, 3.4, 4.5, -35), (52, -120, 3.4, 4.5, -39),
        (60, -105, 3.4, 4.5, -43.5), (62, -88, 3.6, 4.7, -48.5), (62, -74, 4.5, 6, -53), (62, -64, 8, 9, -57),
        (62, -55, 20, 16, -58), (62, -42, 33, 24, -58), (62, -24, 38, 28, -58), (62, 4, 40, 30, -58), (62, 34, 40, 30, -58), (62, 58, 38, 28, -58),
        (62, 76, 30, 24, -58), (62, 88, 18, 16, -58), (62, 97, 8, 9, -58), (62, 104, 6, 8, -58), (62, 110, 5, 7, -58)],
        within='W', canon='CANON-EXPANDED: a great cave that sings under the Peaks, where the Silent Pilgrims listened (world_bible); GAME-ONLY: its name, its trestle, bridge, tower, pools and island'),
    # F1 The Cutting Drift: a Company door 12 m up the Gallery's east wall at the rope bridge's end; the cutting floor; the haul drift up
    # into the Geode Foundry's far end, a lip 12 m over its floor (enter high).
    Cave('F1', 'The Cutting Drift', 'geode', [
        (90, -3.5, 3, 4, None), (96, -3.5, 3, 4, -46), (106, -5, 3, 4.2, -46), (116, -12, 3.2, 4.4, -46), (124, -22, 3.4, 4.6, -46),
        (130, -34, 11, 7, -45.5), (140, -44, 15, 9, -45), (152, -52, 15, 9, -45), (162, -56, 10, 8, -45),
        (172, -56, 3.5, 4.5, -44.5), (186, -50, 3.5, 4.5, -42.5), (197, -38, 3.5, 4.5, -40), (203, -22, 3.6, 4.6, -38.5), (205, -9, 4.2, 5.2, -38), (206, 4, 4.5, 5.5, -38)],
        within='G', rise=12, joins='F2', canon=OLD),
    # F2 The Geode Foundry: its mouth the chute lip 6 m up the Gallery's east wall (the way home is the drop into the chute pool); the
    # chute and its door; the built hall, 46 m across and 24 high; the Rock-Eater's berth.
    Cave('F2', 'The Geode Foundry', 'foundry', [
        (92, 13, 3.5, 4.5, None), (98, 13, 3.5, 4.5, -52), (110, 13, 3.8, 4.8, -51.5), (125, 13, 4, 5, -51), (136, 13, 4.5, 5.5, -50.5),
        (143, 13, 9, 9, -50), (150, 13, 21, 20, -50), (160, 13, 23, 24, -50), (190, 13, 23, 24, -50), (212, 13, 23, 24, -50), (222, 13, 15, 14, -50),
        (230, 13, 11, 10, -50), (248, 13, 12, 10, -50), (258, 13, 10, 9, -50)],
        within='G', rise=6, canon=OLD),
    # V1 The Ash Garden: the ash steps down from the Gallery's east wall to the gatehouse, and the garden of the order's dead.
    Cave('V1', 'The Ash Garden', 'ashgarden', [
        (90, 62, 3.5, 4.5, None), (97, 62, 3.5, 4.5, -58), (104, 62, 3.5, 4.5, -58.5), (114, 62, 3.5, 4.5, -61), (124, 62, 3.5, 4.5, -64), (130, 62, 4.5, 5.5, -66),
        (138, 62, 12, 10, -66), (148, 62, 17, 12, -66), (166, 62, 17, 12, -66), (178, 62, 15, 11, -66), (186, 62, 9, 8, -66)],
        within='G', canon='GAME-ONLY chapter-house; CANON cult (the Cult of Ash)'),
    # VS The Scriptorium (one side of the fork): north out of the garden into the library, on to the chapel.
    Cave('VS', 'The Scriptorium', 'scriptorium', [
        (170, 75, 3.5, 4.5, None), (170, 82, 3.5, 4.5, -66), (171, 92, 3.8, 4.8, -66.5), (172, 100, 9, 8, -67), (172, 108, 12, 10, -67.5),
        (172, 126, 12, 10, -68.5), (172, 134, 8, 8, -69), (172, 141, 4.5, 5.5, -69.5), (172, 150, 4, 5, -70)],
        within='V1', canon='GAME-ONLY chapter-house; CANON cult (the Cult of Ash)'),
    # VSx The Censored Room: behind the bookcase in the Scriptorium's west wall.
    Cave('VSx', 'The Censored Room', 'censored', [
        (163, 118, 2.6, 3.6, None), (158, 118, 2.6, 3.6, -68.2), (152, 118, 4, 4, -68.2), (146, 118, 3.4, 3.8, -68.2)],
        within='VS', canon='GAME-ONLY'),
    # VM The Muster Yard (the other side of the fork): east out of the garden's end, north up the yard, west into the chapel's north door.
    Cave('VM', 'The Muster Yard', 'musteryard', [
        (180, 62, 3.5, 4.5, None), (188, 62, 3.5, 4.5, -66), (200, 63, 3.5, 4.5, -66.3), (210, 70, 3.6, 4.6, -66.6), (215, 82, 8, 7, -67),
        (216, 92, 13, 10, -67.3), (217, 105, 13, 10, -67.6), (217, 118, 13, 10, -68), (216, 130, 9, 8, -68.3), (215, 140, 4, 5, -68.6),
        (214, 152, 3.5, 4.5, -69), (210, 164, 3.5, 4.5, -69.4), (198, 175, 3.5, 4.5, -69.7), (182, 176, 3.5, 4.5, -70), (168, 169, 3.8, 4.8, -70), (160, 163, 4, 5, -70)],
        within='V1', joins='VC', canon='GAME-ONLY chapter-house; CANON cult (the Cult of Ash)'),
    # VMk The Kennel: a bay off the yard's east wall, where the Vent-Hound is kept.
    Cave('VMk', 'The Kennel', 'kennel', [
        (227, 112, 3.6, 4.5, None), (233, 112, 3.6, 4.5, -68), (240, 112, 6, 5.5, -68), (244, 112, 4, 4.5, -68)],
        within='VM', canon='GAME-ONLY'),
    # VC The Chapel of Embers and the cloister: in from the Scriptorium's end (the east door; the Muster Yard's comes in at the north-east),
    # the nave round the lake of fire, the altar at the west apse, the cloister door behind it, the cloister stair home onto the
    # Gallery's north-east floor (the loop).
    Cave('VC', 'The Chapel of Embers', 'chapel', [
        (172, 147.5, 4, 5, None), (166, 149, 6, 7, -70), (160, 151, 13, 14, -70), (152, 152, 17, 18, -70), (130, 152, 17, 20, -70), (108, 152, 17, 18, -70),
        (100, 152, 10, 10, -70), (94, 151, 4.2, 5.4, -70), (91, 146, 3.6, 4.8, -69.5), (90, 132, 3.6, 4.8, -68), (89, 116, 3.6, 4.8, -66),
        (86, 102, 3.6, 4.8, -63.5), (81, 92, 3.8, 5, -59.5), (75, 86, 4, 5, -57.3), (70, 83, 4, 5, -56.5)],   # the stair's foot meets the Gallery's floor (-56.5): 1.5 m below it, the way home went 321 m round (the step walk, 2026-10-15 01:20)
        within='VS', joins='G', canon='GAME-ONLY chapter-house; CANON cult (the Cult of Ash)'),
    # R The Sunless River: west out of the Gallery's floor, the wet way, the Lurker's Pool, the falls stair up beside the water.
    Cave('R', 'The Sunless River', 'river', [
        (33, 48, 4, 5.5, None), (26, 48, 4.5, 6, -58), (18, 47.5, 5, 6, -59), (8, 47, 5, 6, -60), (0, 46, 5, 6, -60.5), (-10, 45, 5, 6, -61),
        (-22, 44, 7, 5.5, -61.5), (-40, 42, 7, 5, -62), (-60, 41, 7, 5, -62), (-78, 40, 7, 5, -61.5), (-90, 39, 7, 5.5, -61),
        (-100, 38, 14, 10, -61), (-114, 37, 21, 14, -61), (-130, 40, 22, 14, -61), (-144, 46, 16, 12, -61), (-152, 56, 6, 7, -60.5),
        (-157, 70, 4, 6, -56), (-161, 86, 4, 6, -50), (-163, 102, 4, 6, -44), (-163, 114, 9, 8, -40.5), (-163, 124, 10, 8, -40)],
        within='G', canon='GAME-ONLY (stagnant water in the old workings is CANON, book2 ch.19)'),
    # R2 The Dry Ledge: the long dry way round the wet way, through the pillared hall, back into the river.
    Cave('R2', 'The Dry Ledge', 'river', [
        (-10, 41, 3.5, 4.5, None), (-11, 34, 3.5, 4.5, -61), (-16, 22, 3.8, 4.8, -60), (-26, 12, 8, 8, -59), (-40, 6, 16, 10, -58.5),
        (-58, 5, 16, 10, -58.5), (-72, 10, 10, 8, -59.5), (-82, 20, 4, 5, -61), (-88, 30, 4, 5, -61), (-90, 37, 4, 5, -61)],
        within='R', joins='R', canon='GAME-ONLY'),
    # B The Grey Breach: from the falls head east, the Hollow drift, the Unmade Gallery, the Foreman's drowned drift, the sluice-boards and
    # the falls lip 12 m up the Gallery's north-west wall (the way home is the falls drop into the plunge pool).
    Cave('B', 'The Grey Breach', 'breach', [
        (-160, 119, 4, 5, None), (-154, 119, 4, 5, -40), (-140, 119, 5, 5.5, -40.5), (-122, 118, 7, 6, -41), (-104, 120, 7, 6, -41),
        (-90, 122, 12, 8, -41.5), (-74, 123, 12, 8, -41.5), (-62, 122, 6, 6, -42), (-48, 120, 13, 12, -42.5), (-30, 118, 14, 12, -42.5),
        (-16, 114, 8, 8, -43), (-2, 106, 14, 10, -44), (12, 98, 15, 10, -44), (22, 90, 8, 8, -45), (30, 84, 5, 6, -46), (36, 80, 5, 6, -46), (42, 78, 5, 6, -46)],
        within='R', joins='G', canon='CANON-EXPANDED: the Wasting unmaking the Peaks\' stone (world_bible, book2 ch.18); GAME-ONLY: the breach'),
    # Q The Quiet Miner's Drift (optional): an old timbered dead end off the Hollow drift.
    Cave('Q', "The Quiet Miner's Drift", 'quiet', [
        (-82, 131, 3, 4, None), (-82, 138, 3, 4, -41.5), (-83, 150, 3, 4, -42), (-84, 162, 6, 5, -42), (-84, 172, 9, 6, -42), (-84, 180, 6, 5, -42)],
        within='B', canon=OLD),
    # D The Deep Way: the singing door at the Gallery's north neck, the shaft stair and its landings (the powder bay), the platform gate,
    # and the Rail Hall running west (the second reveal).
    Cave('D', 'The Deep Way', 'deep', [
        (62, 104, 6, 8, None), (62, 110, 6, 8, -58), (62, 116, 5, 6.5, -58), (62, 124, 4.5, 6, -59.5), (62, 140, 4.5, 6, -64), (63, 154, 7, 7, -68),
        (64, 168, 4.5, 6, -72), (64, 186, 4.5, 6, -77), (63, 200, 7, 7, -81), (61, 214, 4.5, 6, -85), (57, 230, 4.5, 6, -90), (53, 242, 8, 7, -93),
        (50, 252, 4, 5.5, -96), (47, 260, 3.6, 5, -98.5), (42, 268, 10, 10, -100), (32, 280, 30, 22, -100), (15, 292, 40, 27, -100),
        (-20, 300, 40, 28, -100), (-55, 302, 38, 28, -100), (-80, 302, 28, 22, -100), (-96, 302, 14, 14, -100)],
        within='G', canon='CANON-EXPANDED: the mag-rail line south under the range (book2 ch.19); GAME-ONLY: the Rail Hall and all in it'),
    # KP The Pitch Pit (optional, hidden): a crawl out of the Rail Hall's north-west corner to the pitch-boiler's pit.
    Cave('KP', 'The Pitch Pit', 'pitch', [
        (-62, 330, 2.5, 3.5, None), (-64, 336, 2.5, 3.5, -100), (-70, 346, 2.5, 3.5, -102), (-80, 354, 2.5, 3.5, -104.5), (-92, 360, 8, 6, -106),
        (-104, 362, 10, 7, -106), (-114, 360, 8, 6, -106)],
        within='D', canon='GAME-ONLY'),
    # (S6, the Deadmines end-piece; Chris 2026-10-15, Q3 "Build the Cove") DL The Dead Line: the old line runs on west out of the Rail
    # Hall's far end, a rail tunnel falling gently, with two sidings where the rearguard and the spare wagons stand.
    Cave('DL', 'The Dead Line', 'deadline', [
        # (R129: out through the Rail Hall's south-west side wall where the line runs, not its far end: a branch leaving through its
        # parent's closing end met the parent's end shell, and the whole line was cut off 5 m in. The Pitch Pit leaves by a side wall too.)
        (-72, 282, 4.5, 5.5, None), (-80, 274, 4.5, 5.5, -100), (-90, 267, 4.5, 5.5, -100.5), (-104, 263, 4.5, 5.5, -101.5), (-120, 262, 9, 7, -103),
        (-134, 262, 9, 7, -104), (-148, 263, 4.5, 5.5, -105), (-170, 266, 4.5, 5.5, -107), (-192, 270, 4.5, 5.5, -108.5), (-210, 273, 9, 7, -110),
        (-222, 274, 9, 7, -111), (-234, 275, 4.5, 5.5, -112.5), (-246, 275, 4.5, 5.5, -113.5), (-258, 275, 4.5, 5.5, -114), (-270, 275, 4.5, 5.5, -114)],
        within='D', canon='CANON-EXPANDED: the mag-rail line south under the range (book2 ch.19); GAME-ONLY: this stretch, its sidings and all in it'),
    # BC The Black Cove: off the Dead Line's end, a beach of black scree down into a great natural cave of still black water, 88 m across
    # and 36 high; the quay along its west wall, the scaffold, the moored ore barge and its stern castle; the drain at its south end,
    # where the water goes out under the range. Swim water (the flood), 2.6 m deep over a floor 121 m under the yard.
    Cave('BC', 'The Black Cove', 'cove', [
        # (R129: its mouth in the Dead Line's south wall, the line running on past it to the buffers, not out of the line's closing end.)
        (-247, 273.5, 4.5, 5.5, None), (-256, 262, 5, 6, -114), (-268, 250, 9, 9, -114.3), (-282, 238, 22, 20, -115), (-294, 224, 36, 28, -117),
        (-302, 208, 42, 34, -119.5), (-304, 190, 44, 36, -121), (-304, 165, 44, 36, -121), (-304, 140, 42, 34, -121), (-302, 120, 30, 26, -121),
        (-299, 106, 14, 14, -121), (-298, 98, 6, 8, -121)],
        within='DL', canon='GAME-ONLY (the Black Cove, its quay, the barge and all in it; the Sandthrone are CANON)'),
]
CAVE = {c.key: c for c in CAVES}
# (S6) Danner's finale in the Black Cove (his camp and his guard on the stern castle; no rising in the Weaver's escort; the carriage rides
# once the lock is matched or he is dead: EncounterSession.Haul.cs reads it from where his camp stands). False keeps him in the Rail Hall
# as before S6 (Docs/ADIT_REWORK.md section 11, S6: until the Cove's own group run passes; then revert the two quest texts of section 20).
COVE_FINALE = True

def build_caves():
    Cave.ALL[:] = []
    for c in CAVES:
        c.build(CAVE); Cave.ALL.append(c)
        if c.pts[0][4] is None: pass
    for c in CAVES:
        if c.joins:
            c.join = CAVE[c.joins]; e = c.rings[-1]
            ty = c.join.floor_smooth(e[0], e[1])
            if ty is None: sys.exit('%s joins %s, but its end %s is not over %s\'s floor' % (c.name, c.join.name, (round(e[0], 1), round(e[1], 1)), c.join.name))
            c.join_rise = e[4] - ty
    for c in CAVES:
        c.floored = [not floor_gives_way(c, r[0], r[4] + .3, r[1]) for r in c.rings]

# ---------------------------------------------------------------- where things are: helpers
def A(key, x, z):
    """(along, aside) of a world point in a cave."""
    a, s = CAVE[key].local(x, z); return round(a, 2), round(s, 2)
def along(key, x, z): return A(key, x, z)[0]

# ---------------------------------------------------------------- decks (ADIT_REWORK sections 3; timber, stone, rope and rubble, no machines)
DECKS = []
def deck(name, key, start, end=None, aside=0.0, aside2=None, rise=0.0, rise2=None, width=3.0, style='timber', rails=None, radius=0.0, solid=False, wall=0, canon='GAME-ONLY', free=()):
    """start/end: world (x, z) on the cave's line, or a number (an along). free: the ends ('start', 'end') that stop in the open on purpose
    (a barge's bow and her stern castle's back, railed): check 6 lets them be."""
    def al(p): return p if isinstance(p, (int, float)) else along(key, *p)
    f = al(start); t = f if end is None else al(end)
    d = dict(name=name, cave=CAVE[key].name, style=style, wall=wall, **{'from': round(f, 2)}, to=round(t, 2), aside=aside, aside2=aside if aside2 is None else aside2,
             rise=rise, rise2=rise if rise2 is None else rise2, width=width, canonStatus=canon)
    if rails: d['rails'] = rails
    if radius: d['radius'] = radius
    if solid: d['solid'] = True
    d['_key'] = key; d['_free'] = tuple(free)
    DECKS.append(d); return d

def make_decks():
    DECKS[:] = []
    W = 'W'
    # The Cage Bowl's overseers' walk: level at 3 m under the yard down the west side, stepping up from the south rim, then a stair down
    # to the north rim (its top over the falling floor: 0 to 8 m). Railed.
    top = -3.0
    def rise_at(key, x, z, y): return round(y - CAVE[key].at_(along(key, x, z))[1], 2)
    # The walk's north end comes down to 5 m over the falling floor (level, it ran 8.4 m up at aside 12.5, where the bowl's wall
    # (Half 20, Height 12, bumps to 19 %) leans in to about 8 m: its planks were in the shell from z -298 on; AditNavDiagnostics 19:24).
    deck("The overseers' south steps", W, (-1, -327), (-2.5, -318), aside=-8, aside2=-11.5, rise=rise_at(W, -1, -327, top), rise2=rise_at(W, -2.5, -318, top), width=3)
    deck("The overseers' walk", W, (-2.5, -318), (-3, -287), aside=-11.5, rise=rise_at(W, -2.5, -318, top), rise2=5.0, width=3)
    deck("The overseers' stair", W, (-3, -287), (-1, -268), aside=-11.5, aside2=-9, rise=5.0, rise2=rise_at(W, -1, -268, -8.6), width=3)
    # Lusk's tally stage on its spoil heap, a ramp up at either end (the hill-with-a-ramp lesson: you fight up to him).
    deck('The tally ramp (south)', W, (0, -193), (0, -183), aside=0, rise=0, rise2=3.5, width=6, style='scree')
    deck("Lusk's tally stage", W, (0, -183), (0, -172), aside=0, rise=3.5, width=12, style='stone', solid=True)
    deck('The tally ramp (north)', W, (0, -172), (0, -162), aside=0, rise=3.5, rise2=0, width=6, style='scree')
    # The overlook: a timber balcony out over the Gallery from the drift's end, where the trestle starts.
    deck('The overlook balcony', W, along(W, 26, -16.75), along(W, 34.8, -16.75), aside=2.5, rise=0, width=5.5)   # the lip's south half, over the trestle head and not the ramp (its planks took the ramp's headroom: AditNavDiagnostics 2026-10-14)
    # The Miners' Trestle down the Gallery's west wall (+14, +12 at the landing where the bridge starts, then down to the floor).
    G = 'G'; tr = -28
    deck("The Miners' Trestle (head)", G, (62, -19), (62, -14), aside=tr, rise=14, width=4)
    deck("The Miners' Trestle (ramp)", G, (62, -14), (62, -6), aside=tr, rise=14, rise2=12, width=4)
    deck('The trestle landing', G, (62, -6), (62, -1), aside=tr, rise=12, width=4)
    deck("The Miners' Trestle (down)", G, (62, -1), (62, 40), aside=tr, aside2=-27, rise=12, rise2=0, width=3.5)
    # The rope bridge at +12 from the landing to the Company door 12 m up the east wall, the tower at its middle on posts from the floor.
    deck('The rope bridge', G, (62, -3.5), aside=-29.5, aside2=33.5, rise=12, width=2.6, style='rope', rails='both')
    deck('The bridge tower', G, (62, -3.5), aside=0, rise=12, radius=2.2)
    # The island in the central pool (the Singing Stone and the Wake), two rubble causeways out to it.
    deck('The island', G, (62, 34), aside=-4, rise=1.6, radius=5, style='stone', solid=True)
    deck('The west causeway', G, (62, 34), aside=-19, aside2=-8.6, rise=0, rise2=1.6, width=3, style='scree')
    deck('The east causeway', G, (62, 34), aside=11, aside2=.6, rise=0, rise2=1.6, width=3, style='scree')
    # The Geode Foundry: the haul drift's landing out over the hall (+12), the catwalk along the south wall, the cross-walk, the north
    # ramp down to the walkway at +6, and the far ramp to the floor (enter high, work down round the edge).
    F1 = CAVE['F1']; F2 = 'F2'
    deck('The haul-drift landing', 'F1', F1.length - 15, F1.length - 7, aside=0, rise=0, width=9)   # the builder lays an apron from the lip to the catwalk (ZoneBuilder.AditDecks.JoinAprons)
    deck('The catwalk', F2, (208, 13), (160, 13), aside=16.5, rise=12, width=3)
    deck('The cross-walk', F2, (160, 13), aside=17.5, aside2=-17.5, rise=12, width=3)
    deck('The north ramp', F2, (160, 13), (178, 13), aside=-16.5, rise=12, rise2=6, width=3)
    deck('The walkway', F2, (178, 13), (204, 13), aside=-16.5, rise=6, width=3)
    deck('The far ramp', F2, (204, 13), (222, 13), aside=-16.5, aside2=-10, rise=6, rise2=0, width=3)
    if 'BC' in CAVE: cove_decks()

# (S6) The Black Cove's works: heights over its black water (the flood's surface, WATER_Y), asides to the right going south (west).
WATER_Y = -121 + 2.6      # the cove's floor and the flood's depth (make_works)
QUAY_Y, TIER1_Y, TIER2_Y = WATER_Y + .8, WATER_Y + .8 + 2.2, WATER_Y + .8 + 3.4
BARGE_Y, CASTLE_Y = WATER_Y + 4.2, WATER_Y + 7.5
QUAY_A, SCAFF_A, BARGE_A, STAIR_A = 26.5, 20.5, 12.5, 9.0   # (R129: two metres further in; the quay's outer edge at 28.5 met the beach's wall)
def cz(z):
    """The Black Cove's along where its line crosses z (it runs south down x about -304)."""
    return along('BC', -304, z)
def rise_to(key, at, top):
    """The rise over a cave's floor (its middle, at that along) that puts a deck's end at the height top."""
    return round(top - CAVE[key].at_(at)[1], 2)

def cove_decks():
    """The quay along the west wall from the beach; the scaffold beside the barge (tier 1 at quay +2.2, a ramp at its north end; tier 2 at
    +3.4, its ramp at the south end: the ramps alternate ends); the gangway from tier 2 onto the barge's deck; the barge (45 m by 10, deck
    at water +4.2: a solid plank-and-iron hull from the cove's floor) and her stern castle (+7.5) with a stair up its east side; the scree
    landing stage at the quay's south end, where a swimmer climbs out. Timber, rope and rubble, hand-worked: no machine (0.05)."""
    B = 'BC'; shore = 74.0   # where the beach's floor comes up to the quay's height
    q0, q1 = shore, cz(146)
    deck('The quay', B, q0, q1, aside=QUAY_A, rise=rise_to(B, q0, QUAY_Y), rise2=rise_to(B, q1, QUAY_Y), width=8, rails='none')   # (R129: its rail stood across the scaffold ramp's foot)
    r0, r1 = cz(208), cz(200)
    deck('The scaffold ramp (north)', B, r0, r1, aside=QUAY_A - 4, aside2=SCAFF_A, rise=rise_to(B, r0, QUAY_Y), rise2=rise_to(B, r1, TIER1_Y), width=3)
    t0, t1 = r1, cz(176)
    deck('The scaffold, tier 1', B, t0, t1, aside=SCAFF_A, rise=rise_to(B, t0, TIER1_Y), rise2=rise_to(B, t1, TIER1_Y), width=3)
    u0, u1 = t1, cz(170)
    deck('The scaffold ramp (south)', B, u0, u1, aside=SCAFF_A, rise=rise_to(B, u0, TIER1_Y), rise2=rise_to(B, u1, TIER2_Y), width=3)
    v0, v1 = u1, cz(160)
    deck('The scaffold, tier 2', B, v0, v1, aside=SCAFF_A, rise=rise_to(B, v0, TIER2_Y), rise2=rise_to(B, v1, TIER2_Y), width=3, free=('end',))
    g = cz(166)
    deck('The gangway', B, g, aside=SCAFF_A, aside2=BARGE_A, rise=rise_to(B, g, TIER2_Y), width=2.6, rails='none')   # (R129: onto the deck's middle; no rails to wall it off from the deck)
    b0, b1 = cz(200), cz(165)
    deck('The ore barge', B, b0, b1, aside=BARGE_A, rise=rise_to(B, b0, BARGE_Y), rise2=rise_to(B, b1, BARGE_Y), width=10, solid=True, free=('start', 'end'), rails='none')   # (R129: her rails walled the gangway off; a gunwale for the eye: ZoneBuilder.BlackCove)
    s0, s1 = cz(177), cz(165)
    deck("The stern castle's stair", B, s0, s1, aside=STAIR_A, rise=rise_to(B, s0, BARGE_Y), rise2=rise_to(B, s1, CASTLE_Y), width=3)
    c0, c1 = cz(165), cz(155)
    deck('The stern castle', B, c0, c1, aside=BARGE_A, rise=rise_to(B, c0, CASTLE_Y), rise2=rise_to(B, c1, CASTLE_Y), width=10, solid=True, free=('start', 'end'), rails='none')   # (its stair comes up its east side: check 6 holds the stair's end to it)
    l0, l1 = q1, cz(136)
    deck('The landing stage', B, l0, l1, aside=QUAY_A, rise=rise_to(B, l0, QUAY_Y), rise2=0, width=5, style='scree')

# ---------------------------------------------------------------- water, drops, gates, anchors
WATER, DROPS, GATES, ANCHORS = [], [], [], []
def make_works():
    WATER[:] = []; DROPS[:] = []; GATES[:] = []; ANCHORS[:] = []
    def pool(name, key, x, z, radius, depth):
        a, s = A(key, x, z); WATER.append(dict(name=name, cave=CAVE[key].name, **{'from': a}, to=a, aside=s, radius=radius, depth=depth))
    def stream(name, key, p0, p1, aside, half, depth):
        WATER.append(dict(name=name, cave=CAVE[key].name, **{'from': along(key, *p0)}, to=along(key, *p1), aside=aside, half=half, depth=depth))
    pool('The bailing pool', 'FW', -60, -238, 6.5, 1.1)
    pool('The central pool', 'G', 58, 34, 13, 1.2)
    pool('The chute pool', 'G', 86, 12, 6.5, 1.3)
    pool('The plunge pool', 'G', 44, 76, 8, 1.4)
    stream("The Gallery's river", 'G', (62, 70), (62, 50), -27, 2.5, .6)
    stream('The river drift', 'R', (16, 47), (-6, 45.5), 0, 2.5, .6)
    stream('The wet way', 'R', (-16, 44.5), (-86, 39.5), 0, 5.8, 1.1)
    pool("The Lurker's Pool", 'R', -122, 39, 13, 1.3)
    pool('The drowned drift', 'B', 5, 102, 8, .5)
    if 'BC' in CAVE:   # (S6) the Black Cove's black water, wall to wall over its floor, 2.6 m deep (swim): out from the beach to the drain
        WATER.append(dict(name='The black water', cave=CAVE['BC'].name, **{'from': round(cz(196), 2)}, to=round(cz(108), 2), depth=2.6, flood=True))
    def drop(name, fk, fp, tk, tp, width=2.5, falls=False):
        fa, fs = A(fk, *fp); ta, ts = A(tk, *tp)
        d = dict(name=name, fromCave=CAVE[fk].name, toCave=CAVE[tk].name, **{'from': fa}, fromAside=fs, fromRise=0, to=ta, toAside=ts, toRise=0, width=width)
        if falls: d['falls'] = True
        DROPS.append(d)
    drop('The chute drop', 'F2', (95.5, 13), 'G', (84, 13), 2.5)
    drop('The falls', 'B', (37, 79.5), 'G', (44, 76), 3, falls=True)
    def gate(title, key, at, kind, prompt=None, lord=None, opensOn=None, look=None, side=None):
        a = at if isinstance(at, (int, float)) else along(key, *at)
        g = dict(title=title, cave=CAVE[key].name, along=round(a, 2), kind=kind)
        for k, v in (('prompt', prompt), ('lord', lord), ('opensOn', opensOn), ('look', look), ('side', side)):
            if v: g[k] = v
        GATES.append(g)
    gate('The sump bar', 'G', 7, 'bar', prompt='Lift the bar', look='bar', side='far')
    gate("Lusk's door", 'W', (0, -152), 'bossdoor', opensOn='Gang-Boss Haddo Lusk', look='door')
    gate('The pen gate', 'P', (36, -180), 'pen', prompt='Lift the pen bar', look='stockade')
    gate('The chute door', 'F2', (137, 13), 'bossdoor', opensOn='Nix, the turncoat', look='door')
    gate('The bookcase', 'VSx', 3, 'hiddendoor', prompt='Search the shelves', look='bookcase')
    gate('The cloister door', 'VC', (92, 148.5), 'bossdoor', opensOn='Cinder-Warden Ysolt', look='door')
    gate('The sluice-boards', 'B', (31, 83.5), 'bossdoor', opensOn='The Foreman Who Forgot', look='boards')
    gate('The singing door', 'D', 16, 'liftgate', prompt='Set the rail sigils', look='song')
    gate('The platform gate', 'D', (47, 260), 'platformgate', prompt='Open the gate', lord='Quartermaster Brannigan Sorrel')
    def anchor(name, key, at, aside=None, rise=0):
        if isinstance(at, (int, float)): a, s = at, (aside or 0)
        else:
            a, s = A(key, *at)
            if aside is not None: s = aside
        d = dict(name=name, cave=CAVE[key].name, along=round(a, 2), aside=round(s, 2))
        if rise: d['rise'] = rise
        ANCHORS.append(d)
    anchor('cages', 'W', (-3.5, -291))
    anchor('boss', 'W', (0, -177))
    anchor('overlook', 'W', (24, -15.5))
    anchor('gallery', 'G', (62, 18))
    anchor('spirit stone', 'G', (42, 24))
    anchor('island', 'G', (58, 34), rise=1.6)
    anchor('trestle landing', 'G', (34, -3.5), rise=12)
    anchor('chute pool', 'G', (75, 5))
    anchor('plunge pool', 'G', (48, 64))
    anchor('cloister foot', 'G', (76, 78))
    anchor('lamp ring amber', 'G', (88, -3.5))
    anchor('lamp ring red', 'G', (85, 62))   # (clear of the ash steps' own air, where nothing of the Gallery's stands)
    anchor('lamp ring grey', 'G', (40, 46))
    anchor('the neck', 'G', (62, 95))
    anchor('furnace', 'F2', (186, 13))
    anchor('slag pit', 'F2', (172, 20))
    anchor('berth', 'F2', (244, 13))
    anchor('far ramp foot', 'F2', (224, 3))
    anchor('urns north', 'V1', (152, 51))
    anchor('urns south', 'V1', (166, 73))
    anchor('lake', 'VC', (138, 152))
    anchor('altar', 'VC', (110, 152))
    anchor("lurker's pool", 'R', (-122, 39))
    anchor('foreman', 'B', (10, 100))
    # (S7) The Grey Spreads (ADIT_REWORK E6): three lines across the Breach, in the quiet between its fights (the mouth crawlers at 13 m,
    # the Hollow drift at 79, the Grey den at 122, the Whisperer at 141, the Foreman at 178). Crossing one greys the lamps behind it,
    # shows the static there and lets a seep of crawlers out 25 m behind (EncounterSession.GreySpread.cs). GAME-ONLY.
    for n, a in enumerate(GREY_LINES): anchor('grey line %d' % (n + 1), 'B', a, aside=0)
    # (S7) The swing-loads (Chris, 2026-10-15, Q4 "Yes, add them"): ore buckets on ropes across the Foundry's high walks, swung by hand by
    # rope teams on the hall floor (EncounterSession's AditSwingLoad). Where each bucket's arc bottoms out on its walk: two on the catwalk
    # (clear of the sorters at 95 m) and one on the cross-walk. GAME-ONLY; no wheel, winch or crane: a rope from a block lashed under a tie-beam.
    anchor('swing load 1', 'F2', 108, aside=16.5, rise=12)
    anchor('swing load 2', 'F2', 82, aside=16.5, rise=12)
    anchor('swing load 3', 'F2', 69, aside=5, rise=12)   # (its team, 9 m on into the hall, clear of the slag pit at (81, -7))
    anchor('powder bay', 'D', (53, 242))
    anchor('hall', 'D', (-2, 296))
    anchor('rail spirit stone', 'D', (40, 271), aside=7)   # just inside the platform gate, to the right of the way
    # (S6) Where the carriage stands in the Rail Hall: 14 m short of Danner's old camp, as ZoneBuilder.Adit's RailHall put it while he
    # stood there (so the Weaver's lock, her waves and the mules stand where they did now that he keeps the barge).
    anchor('carriage', 'D', round(A('D', -66, 302)[0] - 14, 2), aside=0)
    if 'BC' in CAVE:
        B = 'BC'
        anchor('cove', B, cz(190), aside=0)
        anchor('haul', B, cz(208), aside=QUAY_A, rise=rise_to(B, cz(208), QUAY_Y))                  # the quay's north end: the haul starts as you come onto it
        anchor('bollards north', B, cz(204), aside=QUAY_A - 3, rise=rise_to(B, cz(204), QUAY_Y))    # the bow's warps
        anchor('bollards south', B, cz(156), aside=QUAY_A - 3, rise=rise_to(B, cz(156), QUAY_Y))    # the stern's warps
        anchor('barge bow', B, cz(198), aside=BARGE_A, rise=rise_to(B, cz(198), BARGE_Y))
        anchor('stern castle', B, cz(160), aside=BARGE_A, rise=rise_to(B, cz(160), CASTLE_Y))
        anchor('drain', B, cz(112), aside=0)

# ---------------------------------------------------------------- camps (ADIT_REWORK section 4; the array's head is pinned: no save moves)
S = 'GAME-ONLY band; CANON company (Sandthrone)'
CA = 'GAME-ONLY cell; CANON cult (the Cult of Ash)'
GB = 'GAME-ONLY (the Wasting is CANON)'
HM = 'CANON creature (Hollow Men) and CANON tunnels under the Peaks (book2 ch.19); GAME-ONLY camp'
def mob(name, mob_, look, count, lo, hi, tag, radius, canon=S, **kw):
    d = dict(name=name, mob=mob_, tag=tag, look=look, canonStatus=canon, radius=radius, count=count, levelMin=lo, levelMax=hi, respawn=kw.pop('respawn', RESPAWN))
    d.update(kw); return d

def camp(key, at, d, wing, step=0, rise=0.0, gate=None, after=None, optional=False, aside=None):
    """A camp in a cave at a world point (x, z) or (along, aside); on a deck when rise > 0."""
    c = CAVE[key]
    if isinstance(at[0], str): a, s = float(at[1]), float(at[2])   # ('al', along, aside)
    else: a, s = c.local(*at)
    if aside is not None: s = aside
    a, s = round(a, 2), round(s, 2); x, _, z = c.at_(a, s)   # its centre from its along and aside as written (Hollow.At)
    e = dict(d); e['center'] = {'x': round(x, 2), 'y': round(z, 2)}; e['cave'] = c.name; e['along'] = round(a, 2); e['aside'] = round(s, 2)
    e['wing'] = wing
    if step: e['step'] = step
    if rise: e['rise'] = rise
    if gate: e['gate'] = gate
    if after: e['after'] = after
    if optional: e['optional'] = True
    e['_key'] = key
    return e

def patrol(key, p0, p1, d, wing, gate=None, aside=0.0, loop=None):
    """A patrol: its road down the middle of its cave from p0 to p1 (world points), a point every 12 m or so; or a loop of points."""
    c = CAVE[key]
    if loop:
        road = [{'x': round(x, 2), 'y': round(z, 2)} for x, z in loop]
        a, s = c.local(*loop[0])
    else:
        a0, a1 = along(key, *p0), along(key, *p1); n = max(2, int(abs(a1 - a0) / 12) + 1); road = []
        for k in range(n):
            x, _, z = c.at_(a0 + (a1 - a0) * k / (n - 1), aside); road.append({'x': round(x, 2), 'y': round(z, 2)})
        a, s = a0, aside
    e = dict(d); e['center'] = road[0]; e['cave'] = c.name; e['along'] = round(a, 2); e['aside'] = round(s, 2); e['route'] = road; e['wing'] = wing
    if gate: e['gate'] = gate
    e['_key'] = key
    return e

def ring_route(cx, cz, r, n=10):
    return [(cx + r * math.cos(2 * math.pi * k / n), cz + r * math.sin(2 * math.pi * k / n)) for k in range(n)] + [(cx + r, cz)]

LUSK, NIX, YSOLT, FOREMAN = 'Gang-Boss Haddo Lusk', 'Nix, the turncoat', 'Cinder-Warden Ysolt', 'The Foreman Who Forgot'
SORREL, DANNER, SABEL, WHISPERER, HOLLOWSONG = 'Quartermaster Brannigan Sorrel', 'Rail-Captain Orsk Danner', 'Urn-Keeper Sabel', 'The Grey Whisperer', 'Brother Hollowsong'
LDOOR, SDOOR, PGATE, PEN = "Lusk's door", 'The singing door', 'The platform gate', 'The pen gate'

def deck_camp(key, at, top, d, wing, step=0, aside=0.0, **kw):
    """A camp standing on a deck whose top is at height top (its rise over the cave's floor there written from it)."""
    a = at if isinstance(at, (int, float)) else along(key, *at)
    return camp(key, ('al', a, aside), d, wing, step, rise=rise_to(key, a, top), **kw)

def danner_camps():
    """(S6) Rail-Captain Danner and his guard on the stern castle of the ore barge in the Black Cove (pinned 15 and 16)."""
    B = 'BC'
    return [deck_camp(B, cz(158), CASTLE_Y, mob("Rail-Captain's guard", 'Sandthrone platform guard', 'outrider', 2, 12, 12, 'platformguard', 1.2), 'cove', 5, aside=BARGE_A - 2.5, gate=PGATE),   # 15
            deck_camp(B, cz(160.5), CASTLE_Y, mob(DANNER, DANNER, 'outrider', 1, 12, 12, 'railcaptain', 1, canon='GAME-ONLY', elite=True, harder=True, guards="Rail-Captain's guard"), 'cove', 5, aside=BARGE_A + 1.5, gate=PGATE)]   # 16

BG = "GAME-ONLY band (the barge's crew); CANON company (Sandthrone)"
def cove_camps():
    """(S6) The Dead Line's rearguard; the Black Cove's two rope teams on the quay (the Haul: hand over hand on the warps round the bollards),
    the harbour netters on the scaffold (Weighted Net), and the bargemen who come up out of the hold to Danner's calls once the haul has her
    off the bar (released by EncounterSession.Haul: event:haul1, event:haul2). No machine: ropes, timber bollards, hands."""
    B = 'BC'
    return [
        camp('DL', (-127, 262), mob('Siding rearguard', 'Sandthrone docker', 'deserter', 3, 12, 12, 'docker', 2.5), 'cove', 1, aside=2, gate=PGATE),
        deck_camp(B, cz(214), QUAY_Y, mob('Rope team (north)', 'Sandthrone bargeman', 'deserter', 3, 12, 12, 'bargeman', 1.6, canon=BG), 'cove', 2, aside=QUAY_A - 1, gate=PGATE),
        deck_camp(B, cz(188), TIER1_Y, mob('Harbour netters', 'Sandthrone harbour netter', 'deserter', 3, 12, 12, 'netter', .6, canon=BG), 'cove', 3, aside=SCAFF_A, gate=PGATE),
        deck_camp(B, cz(152), QUAY_Y, mob('Rope team (south)', 'Sandthrone bargeman', 'deserter', 3, 12, 12, 'bargeman', 1.6, canon=BG), 'cove', 4, aside=QUAY_A - 1, gate=PGATE),
        deck_camp(B, cz(186), BARGE_Y, mob('Up from the hold (first)', 'Sandthrone bargeman', 'deserter', 2, 12, 12, 'bargeman', 1.5, canon=BG), 'cove', after='event:haul1', aside=BARGE_A, gate='none'),
        deck_camp(B, cz(180), BARGE_Y, mob('Up from the hold (second)', 'Sandthrone bargeman', 'deserter', 2, 12, 12, 'bargeman', 1.5, canon=BG), 'cove', after='event:haul2', aside=BARGE_A, gate='none'),
    ]

def make_camps():
    pick = lambda **kw: kw
    # --- the pinned head, 0-31 (their places in the Adit as it was: elitesSlain "mob.<tag>.adit.<i>" keeps)
    head = [
        camp('W', (0, -355), mob('Adit pickets', 'Sandthrone picket', 'deserter', 2, 10, 10, 'picket', 1.6), 'workings', 1, gate='none'),                       # 0
        camp('W', (-3, -302), mob("The bowl's rim", 'Sandthrone overseer', 'deserter', 3, 10, 10, 'overseer', 1.2), 'workings', 2, aside=-11.5, gate='none'),     # 1 (rise below; on the overseers' walk, its aside)
        camp('W', (0, -290), mob("The cages' guard", 'Sandthrone digger', 'deserter', 3, 10, 10, 'digger', 3), 'workings', 3, aside=3, gate='none'),              # 2
        camp('W', (2, -240), mob("Sappers' drift", 'Sandthrone sapper', 'deserter', 3, 11, 11, 'sapper', 2), 'workings', 5, gate='none'),                          # 3
        camp('W', (0, -175), mob("Gang-boss's guards", 'Sandthrone overseer', 'deserter', 2, 11, 11, 'overseer', 1.2), 'workings', 7, rise=3.5, aside=-3, gate='none'),   # 4
        camp('W', (0, -177), mob(LUSK, LUSK, 'deserter', 1, 11, 11, 'gangboss', 1, canon='GAME-ONLY', elite=True, guards="Gang-boss's guards"), 'workings', 7, rise=3.5, aside=1.5, gate='none'),   # 5
        camp('W', (-4, -112), mob('Timber crew', 'Sandthrone digger', 'deserter', 3, 11, 11, 'digger', 1.6), 'workings', 8, gate=LDOOR),                          # 6
        camp('G', (80, 2), mob('Gallery carriers', 'Sandthrone geode-carrier', 'outrider', 3, 11, 11, 'carrier', 3.5), 'floor', 1, gate=LDOOR),                  # 7
        camp('G', (34, -3.5), mob('Landing guard', 'Sandthrone watchman', 'outrider', 3, 11, 11, 'watchman', 1.2), 'ledge', 1, rise=12, gate=LDOOR),               # 8
        camp('G', (62, -3.5), mob('Bridge-tower crossbows', 'Sandthrone picket', 'deserter', 2, 11, 11, 'picket', .9), 'ledge', 2, rise=12, gate=LDOOR),         # 9
        camp('D', (14, 288), mob('Platform guards', 'Sandthrone platform guard', 'outrider', 3, 12, 12, 'platformguard', 3), 'deep', 6, aside=14, gate=PGATE),  # 10
        camp('D', (36, 278), mob('Siding dockers', 'Sandthrone docker', 'deserter', 4, 12, 12, 'docker', 3), 'deep', 4, aside=10, gate=PGATE, optional=COVE_FINALE),   # 11 (S6: off the quick run, for the Cove's time: Q2)
        camp('D', (-25, 300), mob("Quartermaster's guards", 'Sandthrone platform guard', 'outrider', 2, 12, 12, 'platformguard', 1.5), 'deep', 7, aside=12, gate=PGATE),   # 12
        camp('D', (-28, 300), mob(SORREL, SORREL, 'outrider', 1, 12, 12, 'railquartermaster', 1, canon='GAME-ONLY', elite=True, guards="Quartermaster's guards"), 'deep', 7, aside=14, gate=PGATE),   # 13
        camp('D', (-45, 300), mob('Carriage gunners', 'Sandthrone carriage gunner', 'outrider', 3, 12, 12, 'gunner', 2), 'deep', 5, aside=-12, gate=PGATE),     # 14
        *(danner_camps() if COVE_FINALE else [
        camp('D', (-62, 302), mob("Rail-Captain's guard", 'Sandthrone platform guard', 'outrider', 2, 12, 12, 'platformguard', 1.5), 'deep', 8, aside=-5, gate=PGATE),   # 15
        camp('D', (-66, 302), mob(DANNER, DANNER, 'outrider', 1, 12, 12, 'railcaptain', 1, canon='GAME-ONLY', elite=True, harder=True, guards="Rail-Captain's guard"), 'deep', 8, aside=0, gate=PGATE)]),   # 16
        camp('F1', (140, -44), mob('The cutting floor', 'Sandthrone geode-cutter', 'outrider', 3, 11, 11, 'cutter', 4), 'foundry', 1, gate=LDOOR),               # 17
        camp('F1', (199, -34), mob('Haul-drift porters', 'Sandthrone geode-carrier', 'outrider', 3, 11, 11, 'carrier', 1.6), 'foundry', 2, gate=LDOOR),         # 18
        camp('F2', (186, 13), mob('Catwalk sorters', 'Sandthrone sorter', 'deserter', 3, 11, 11, 'sorter', 1.2), 'foundry', 3, rise=12, aside=16.5, gate=LDOOR), # 19
        camp('F2', (238, 13), mob('Workshop guards', 'Sandthrone overseer', 'deserter', 2, 11, 11, 'overseer', 1.5), 'foundry', 5, aside=-5, gate=LDOOR),        # 20
        camp('F2', (244, 13), mob('Nix and the Rock-Eater', NIX, 'outrider', 1, 11, 11, 'nix', 1, canon='GAME-ONLY (the goblins, the Weavers of the Warrens, are CANON)', elite=True, guards='Workshop guards', key='sigil.amber', keyName='the Amber Sigil'), 'foundry', 5, aside=0, gate=LDOOR),   # 21
        camp('V1', (133, 62), mob('Gatehouse wardens', 'Ash zealot', 'cultist', 3, 11, 11, 'zealot', 2, canon=CA), 'vent', 1, gate=LDOOR),                       # 22
        camp('V1', (150, 62), mob('Garden mourners', 'Ash initiate', 'cultist', 3, 11, 11, 'ashinitiate', 3, canon=CA), 'vent', 3, aside=5, gate=LDOOR),          # 23
        camp('VM', (216, 96), mob('Zealots in ranks', 'Ash zealot', 'cultist', 4, 11, 11, 'zealot', 4, canon=CA), 'musteryard', 1, gate=LDOOR),                   # 24
        camp('VMk', (240, 112), mob('The Vent-Hound', 'The Vent-Hound', 'wolf', 1, 11, 11, 'venthound', 1.5, canon='GAME-ONLY', elite=True, guards='none', social='solitary', respawn=RARE_RESPAWN), 'musteryard', 0, gate=LDOOR, optional=True),   # 25
        camp('VC', (124, 152), mob('Chapel faithful', 'Ash zealot', 'cultist', 3, 11, 11, 'zealot', 3, canon=CA), 'chapel', 1, gate=LDOOR),                      # 26
        camp('VC', (114, 152), mob(YSOLT, YSOLT, 'cultist', 1, 11, 11, 'ysolt', 1, canon='GAME-ONLY (the Cult of Ash is CANON)', elite=True, guards='Sister Ashe Varrow', key='sigil.ember', keyName='the Ember Sigil'), 'chapel', 3, aside=-2, gate=LDOOR),   # 27
        camp('B', (-148, 119), mob('Breach mouth crawlers', 'Greyed crawler', 'spider', 3, 11, 11, 'crawler', 2, canon=GB, flee=.25), 'breach', 1, gate=LDOOR),    # 28
        camp('B', (-82, 122), mob('The Hollow drift', 'Hollow Man', 'hollow', 3, 11, 11, 'hollow', 3, canon=HM), 'breach', 2, aside=3, gate=LDOOR),                # 29
        camp('B', (8, 101), mob("The Foreman's shift", 'Hollow Man', 'hollow', 3, 11, 11, 'hollow', 2, canon=HM), 'breach', 5, aside=7, gate=LDOOR),              # 30
        camp('B', (12, 98), mob(FOREMAN, FOREMAN, 'hollow', 1, 11, 11, 'foreman', 1, canon='GAME-ONLY (the unmade are CANON)', elite=True, guards="The Foreman's shift", key='sigil.grey', keyName='the Grey Sigil'), 'breach', 5, aside=-4, gate=LDOOR),   # 31
    ]
    # The bowl's rim stands on the overseers' walk: its rise over the floor there.
    w = CAVE['W']; rim = head[1]; walk = next(d for d in DECKS if d['name'] == "The overseers' walk")
    f = (rim['along'] - walk['from']) / (walk['to'] - walk['from'])
    y0 = w.at_(walk['from'])[1] + walk['rise']; y1 = w.at_(walk['to'])[1] + walk['rise2']   # the walk's planks, straight between its ends
    rim['rise'] = round(y0 + (y1 - y0) * f - w.at_(rim['along'])[1], 2)
    rest = [
        # The Old Workings
        camp('W', (-1, -276), mob('Ore-cart loaders', 'Sandthrone digger', 'deserter', 3, 10, 10, 'digger', 3), 'workings', 4, aside=8, gate='none'),
        camp('W', (0, -190), mob('Spoil heaps', 'Sandthrone digger', 'deserter', 3, 11, 11, 'digger', 2.5), 'workings', 6, aside=-9, gate='none'),   # the west side: on the east its dressing blocks walled the strip beside the stage that the Pen hangs off (the R91 picture)
        camp('W', (12, -20), mob('Overlook watch', 'Sandthrone watchman', 'outrider', 3, 11, 11, 'watchman', 2), 'workings', 9, gate=LDOOR),
        camp('W', (-1, -282), mob("Lusk's relief shift", 'Sandthrone overseer', 'deserter', 3, 11, 11, 'overseer', 2), 'workings', after='kill:' + LUSK, aside=-3, gate='none'),
        # The Flooded Working (optional)
        camp('FW', (-60, -238), mob("Bailers' chain", 'Sandthrone digger', 'deserter', 3, 11, 11, 'digger', 3), 'flooded', gate='none', optional=True),
        camp('FW', (-59, -229), mob('Brack the Bailer', 'Brack the Bailer', 'deserter', 1, 11, 11, 'bailer', 1, canon='GAME-ONLY', elite=True, guards="Bailers' chain"), 'flooded', gate='none', optional=True),
        # The Pen (optional)
        camp('P', (52, -181), mob('Pit-hounds', 'Company pit-hound', 'wolf', 2, 11, 11, 'pithound', 2.5, canon='GAME-ONLY (the Sandthrone are CANON)'), 'pen', gate=PEN, optional=True),
        camp('P', (62, -180), mob('Old Bristle', 'Old Bristle', 'boar', 1, 12, 12, 'bristle', 1.5, canon='GAME-ONLY', elite=True, guards='none', social='solitary'), 'pen', gate=PEN, optional=True),
        # The Gallery's floor
        camp('G', (41, 34), mob('Pool watch', 'Sandthrone watchman', 'outrider', 3, 11, 11, 'watchman', 2.5), 'floor', 2, gate=LDOOR),
        camp('G', (50, 62), mob('Gallery lamp-men', 'Sandthrone watchman', 'outrider', 2, 11, 11, 'watchman', 2), 'floor', 3, gate=LDOOR),
        # The Geode Foundry
        camp('F2', (186, 13), mob('Whip-hands', 'Sandthrone overseer', 'deserter', 2, 11, 11, 'overseer', 1.2), 'foundry', 4, rise=6, aside=-16.5, gate=LDOOR),
        camp('F2', (192, 13), mob('Overseer Grael Vosk', 'Overseer Grael Vosk', 'deserter', 1, 11, 11, 'vosk', 1, canon='GAME-ONLY', elite=True, guards='Whip-hands'), 'foundry', 4, rise=6, aside=-16.5, gate=LDOOR),
        camp('F2', (216, 13), mob('Berth night crew', 'Sandthrone geode-cutter', 'outrider', 3, 11, 11, 'cutter', 2), 'foundry', after='kill:' + NIX, aside=-8, gate='none'),
        camp('F2', (180, 13), mob('Smelters at the furnace', 'Sandthrone smelter', 'deserter', 4, 11, 11, 'smelter', 3), 'foundry', 6, aside=-4, gate=LDOOR),
        camp('F2', (146, 13), mob('Chute-door guards', 'Sandthrone overseer', 'deserter', 3, 11, 11, 'overseer', 2), 'foundry', 7, gate=LDOOR),
        # The Ash Garden
        camp('V1', (140, 62), mob('Gatehouse menders', 'Ash mender', 'cultist', 2, 11, 11, 'ashmender', 2, canon=CA), 'vent', 2, aside=-5, gate=LDOOR),
        camp('V1', (176, 62), mob('Urn-tenders', 'Ash initiate', 'cultist', 2, 11, 11, 'ashinitiate', 1.5, canon=CA), 'vent', 4, aside=-5, gate=LDOOR),
        camp('V1', (179, 62), mob(SABEL, SABEL, 'cultist', 1, 11, 11, 'urnkeeper', 1, canon='GAME-ONLY (the Cult of Ash is CANON)', elite=True, guards='Urn-tenders'), 'vent', 4, aside=1, gate=LDOOR),
        camp('V1', (152, 51), mob('Urn-risen (first)', 'Ash-bound dead', 'skeleton', 2, 10, 10, 'ashbound', 1.5, canon=CA), 'vent', after='loose:' + SABEL, gate='none'),
        camp('V1', (166, 73), mob('Urn-risen (second)', 'Ash-bound dead', 'skeleton', 2, 10, 10, 'ashbound', 1.5, canon=CA), 'vent', after='loose:' + SABEL, gate='none'),
        # The Scriptorium
        camp('VS', (172, 108), mob('Copyists', 'Ash scribe', 'cultist', 3, 11, 11, 'ashscribe', 3, canon=CA), 'scriptorium', 1, gate=LDOOR),
        camp('VS', (172, 124), mob('Lectors', 'Ash scribe', 'cultist', 2, 11, 11, 'ashscribe', 1.5, canon=CA), 'scriptorium', 2, aside=-4, gate=LDOOR),
        camp('VS', (172, 127), mob('Loremaster Ivo Senn', 'Loremaster Ivo Senn', 'cultist', 1, 11, 11, 'loremaster', 1, canon='GAME-ONLY (the Cult of Ash is CANON)', elite=True, guards='Lectors'), 'scriptorium', 2, aside=1, gate=LDOOR),
        # The Muster Yard
        camp('VM', (217, 120), mob('Armourers', 'Ash zealot', 'cultist', 2, 11, 11, 'zealot', 1.5, canon=CA), 'musteryard', 2, aside=-5, gate=LDOOR),
        camp('VM', (217, 123), mob('Master-at-Arms Corvane', 'Master-at-Arms Corvane', 'cultist', 1, 11, 11, 'armsmaster', 1, canon='GAME-ONLY (the Cult of Ash is CANON)', elite=True, guards='Armourers'), 'musteryard', 2, aside=1, gate=LDOOR),
        # The Chapel of Embers
        camp('VC', (138, 152), mob('Lake-tenders', 'Ash mender', 'cultist', 3, 11, 11, 'ashmender', 2, canon=CA), 'chapel', 2, aside=12, gate=LDOOR),
        camp('VC', (114, 152), mob('Sister Ashe Varrow', 'Sister Ashe Varrow', 'cultist', 1, 11, 11, 'varrow', 1, canon='GAME-ONLY (the Cult of Ash is CANON)', elite=True, guards='none'), 'chapel', 3, aside=3.5, gate=LDOOR),
        camp('VC', (158, 152), mob('The cloister bell', 'Ash zealot', 'cultist', 3, 11, 11, 'zealot', 2, canon=CA), 'chapel', after='kill:' + YSOLT, aside=8, gate='none'),
        # The Sunless River and the Dry Ledge
        camp('R', (14, 47), mob('River-mouth crawlers', 'Greyed crawler', 'spider', 3, 11, 11, 'crawler', 2.5, canon=GB, flee=.25), 'river', 1, gate=LDOOR),
        camp('R', (-50, 41.5), mob('Under the water', 'Greyed crawler', 'spider', 3, 11, 11, 'crawler', 3, canon=GB, flee=.25), 'river', gate=LDOOR, optional=True),
        camp('R2', (-24, 13), mob('The Hollow sump', 'Hollow Man', 'hollow', 3, 11, 11, 'hollow', 2.5, canon=HM), 'river', 2, gate=LDOOR),
        camp('R2', (-50, 5), mob('Pillar whisperers', 'Grey whisperer', 'pale', 2, 11, 11, 'whisperer', 3, canon=GB, flee=.2), 'river', 3, gate=LDOOR),
        camp('R', (-128, 56), mob('Pool crawlers', 'Greyed crawler', 'spider', 3, 11, 11, 'crawler', 2.5, canon=GB, flee=.25), 'river', 4, gate=LDOOR),
        # The Grey Breach
        camp('B', (-40, 119), mob('Grey den', 'Greyed bear', 'bear', 2, 11, 11, 'greybear', 3, canon=GB), 'breach', 3, gate=LDOOR),
        camp('B', (-24, 116), mob("Whisperer's hollow", 'Grey whisperer', 'pale', 2, 11, 11, 'whisperer', 2, canon=GB, flee=.2), 'breach', 4, aside=-4, gate=LDOOR),
        camp('B', (-21, 115), mob(WHISPERER, WHISPERER, 'pale', 1, 11, 11, 'greywhisperer', 1, canon='GAME-ONLY (the Wasting is CANON)', elite=True, guards="Whisperer's hollow"), 'breach', 4, aside=1, gate=LDOOR),
        camp('B', (-12, 112), mob("The Whisperer's flock", 'Greyed crawler', 'spider', 4, 11, 11, 'crawler', 2.5, canon=GB, flee=.25), 'breach', after='loose:' + WHISPERER, gate='none'),
        camp('B', (-36, 118), mob('What the grey kept', 'Hollow Man', 'hollow', 2, 11, 11, 'hollow', 2, canon=HM), 'breach', after='kill:' + FOREMAN, gate='none'),
        # The Wake on the island (released by its waves)
        camp('G', (36, 38), mob('Wake porters', 'Sandthrone geode-carrier', 'outrider', 4, 11, 11, 'carrier', 2.5), 'wake', after='event:wake1', gate='none'),
        camp('G', (88, 62), mob('Wake zealots', 'Ash zealot', 'cultist', 4, 11, 11, 'zealot', 2.5, canon=CA), 'wake', after='event:wake2', gate='none'),
        camp('G', (34, 50), mob('Wake crawlers', 'Greyed crawler', 'spider', 5, 11, 11, 'crawler', 2.5, canon=GB, flee=.25), 'wake', after='event:wake3', gate='none'),
        camp('G', (62, 93), mob(HOLLOWSONG, HOLLOWSONG, 'pale', 1, 12, 12, 'hollowsong', 1, canon='GAME-ONLY (the Silent Pilgrims and the unmade are CANON)', elite=True, guards='none'), 'wake', after='event:wake4', gate='none'),
        camp('G', (62, 86), mob("Hollowsong's choir", 'Hollow Man', 'hollow', 3, 11, 11, 'hollow', 2.5, canon=HM), 'wake', after='loose:' + HOLLOWSONG, gate='none'),
        # The Deep Way
        camp('D', (62, 132), mob('Stair guard', 'Sandthrone platform guard', 'outrider', 3, 12, 12, 'platformguard', 2), 'deep', 1, gate=SDOOR),
        camp('D', (63, 200), mob('Middle landing', 'Sandthrone platform guard', 'outrider', 3, 12, 12, 'platformguard', 2.5), 'deep', 2, gate=SDOOR),
        camp('D', (53, 242), mob('Powder store', 'Sandthrone sapper', 'deserter', 2, 12, 12, 'sapper', 1.5), 'deep', gate=SDOOR, optional=True, aside=-3),
        camp('D', (59, 222), mob('Powder-Sergeant Hobb', 'Powder-Sergeant Hobb', 'deserter', 1, 12, 12, 'hobb', 1, canon='GAME-ONLY', elite=True, guards="Hobb's sappers"), 'deep', after='event:powder', gate='none'),
        camp('D', (60.5, 216), mob("Hobb's sappers", 'Sandthrone sapper', 'deserter', 2, 12, 12, 'sapper', 1.5), 'deep', after='event:powder', gate='none'),
        camp('D', (50, 252), mob('Lower stair guard', 'Sandthrone platform guard', 'outrider', 3, 12, 12, 'platformguard', 1.6), 'deep', 3, gate=SDOOR),
        camp('D', (-85, 302), mob("Sorrel's dockers", 'Sandthrone docker', 'deserter', 3, 12, 12, 'docker', 2), 'deep', after='kill:' + SORREL, aside=-12, gate='none'),
        # The Pitch Pit (optional, hidden)
        camp('KP', (-98, 361), mob('Pitch-gangers', 'Sandthrone docker', 'deserter', 2, 12, 12, 'docker', 2), 'pitch', gate=PGATE, optional=True),
        camp('KP', (-106, 362), mob('Old Kettle, the pitch-boiler', 'Old Kettle, the pitch-boiler', 'deserter', 1, 13, 13, 'kettle', 1, canon='GAME-ONLY', elite=True, guards='Pitch-gangers'), 'pitch', gate=PGATE, optional=True),
        # The Old Burrower (optional, passive): it wanders a loop round the Lurker's Pool.
        patrol('R', None, None, mob('The Old Burrower', 'The Old Burrower', 'bear', 1, 13, 13, 'burrower', 1.5, canon='GAME-ONLY (the Rock-Eater\'s wild kin)', elite=True, guards='none', social='solitary', passive=True, optional=True), 'river', gate=LDOOR, loop=ring_route(-122, 39, 17.5, 12)),
        # (S6) The Dead Line and the Black Cove: after every camp of the rework, so none before them moves (only the patrols and the rare
        # Quiet Miner, at the end, move on six places).
        *(cove_camps() if COVE_FINALE else []),
    ]
    patrols = [
        patrol('W', (0.5, -348), (-1, -322), mob('Drift patrol', 'Sandthrone picket', 'deserter', 2, 10, 10, 'picket', 1.2), 'workings', gate='none'),
        patrol('W', (-2, -322), (-1, -270), mob('Bowl patrol', 'Sandthrone overseer', 'deserter', 2, 10, 11, 'overseer', 1.2), 'workings', gate='none', aside=6),
        patrol('W', (-6, -95), (-3, -46), mob("Sergeants' round", 'Sandthrone overseer', 'deserter', 2, 11, 11, 'overseer', 1.2), 'workings', gate=LDOOR),
        patrol('G', None, None, mob('Gallery porters', 'Sandthrone geode-carrier', 'outrider', 3, 11, 11, 'carrier', 1.2), 'floor', gate=LDOOR, loop=ring_route(58, 34, 20, 12)),
        patrol('F1', (126, -28), (195, -40), mob('Floor runners', 'Sandthrone geode-cutter', 'outrider', 2, 11, 11, 'cutter', 1.2), 'foundry', gate=LDOOR),
        patrol('V1', (140, 62), (182, 62), mob('Ash procession', 'Ash initiate', 'cultist', 3, 11, 11, 'ashinitiate', 1.2, canon=CA), 'vent', gate=LDOOR, aside=9),
        patrol('VC', (91, 140), (78, 89), mob('Cloister walkers', 'Ash zealot', 'cultist', 2, 11, 11, 'zealot', 1.2, canon=CA), 'chapel', gate='none'),
        patrol('R', (-155, 62), (-163, 112), mob('Wandering Hollow', 'Hollow Man', 'hollow', 2, 11, 11, 'hollow', 1.2, canon=HM), 'river', gate=LDOOR),
        patrol('B', (-100, 120), (-50, 120), mob('Wandering grey', 'Greyed crawler', 'spider', 2, 11, 11, 'crawler', 1.2, canon=GB, flee=.25), 'breach', gate=LDOOR),
        patrol('D', (62, 140), (64, 186), mob('Stair patrol', 'Sandthrone watchman', 'outrider', 2, 11, 12, 'watchman', 1.2), 'deep', gate=SDOOR),
        patrol('D', (30, 282), (-40, 300), mob('Platform round', 'Sandthrone platform guard', 'outrider', 2, 12, 12, 'platformguard', 1.2), 'deep', gate=PGATE, aside=18),
    ]
    rare = [camp('Q', (-84, 176), mob('The Quiet Miner', 'The Quiet Miner', 'pale', 1, 12, 12, 'quietminer', 1.2, canon='GAME-ONLY (the unmade are CANON)', elite=True, guards='none', respawn=RARE_RESPAWN, rare=.2, social='solitary'), 'quiet', gate=LDOOR, optional=True)]
    # (S7) The Grey Spreads' seeps: two crawlers each (the plan's three cut to two to hold the run near 75 minutes), 25 m behind their line,
    # released when it is crossed ("event:greylineN", raised by the code), sent at whoever crossed it. After the patrols, before the rare
    # (released camps draw nothing from the camps' stream, so no other camp's draws move).
    seeps = [camp('B', ('al', a - 25, 0), mob('The grey seep (%s)' % nth, 'Greyed crawler', 'spider', 2, 11, 11, 'crawler', 2.5, canon=GB, flee=.25), 'breach',
                  after='event:greyline%d' % (n + 1), gate='none') for n, (a, nth) in enumerate(zip(GREY_LINES, ('first', 'second', 'third')))]
    # The Ember-bound (Chris, 2026-10-18, picking from Docs/BESTIARY_MONSTERS.md: "Ember-bound (Recommended)"; GAME-ONLY creature on the CANON
    # cult and its glowing tethers, book1 ch.10 and ch.12:19-25): the Ember Vent's chained things in place of the plan's stand-in "ember
    # elementals" (DUNGEON_DESIGN.md section 5), pulled one or two at a time; each pair chained at its keepers' station, the Quaternius Imp
    # in its red skin. After the seeps and before the rare, so only the rare moves on (no other camp's draws move).
    EB = 'GAME-ONLY creature (the Ember-bound); CANON cult and tethers (the Cult of Ash, book1 ch.10; glowing chains, book1 ch.12:19)'
    embers = [
        camp('V1', (146, 62), mob('Chained in the garden', 'Ember-bound', 'imp', 2, 11, 11, 'emberbound', 1.5, canon=EB, social='solitary'), 'vent', 2, aside=-12, gate=LDOOR),
        camp('VC', (146, 152), mob('Chained at the lake of fire', 'Ember-bound', 'imp', 2, 11, 11, 'emberbound', 1.5, canon=EB, social='solitary'), 'chapel', 2, aside=-12, gate=LDOOR),
    ]
    camps = head + rest + patrols + seeps + embers + rare
    pinned = {5: LUSK, 13: SORREL, 16: DANNER, 21: NIX, 25: 'The Vent-Hound', 27: YSOLT, 31: FOREMAN}
    for i, m in pinned.items(): assert camps[i]['mob'] == m, (i, camps[i]['mob'])
    assert camps[-1]['mob'] == 'The Quiet Miner'
    names = [c['name'] for c in camps]; assert len(names) == len(set(names)), 'camp names repeat'
    return camps

# ---------------------------------------------------------------- the wings, the Wake, the lichen, the secrets, the places
WINGS = [
    dict(id='workings', name='The Old Workings', exit='walk', home='overlook'),
    dict(id='flooded', name='The Flooded Working', after='workings', optional=True),
    dict(id='pen', name='The Pen', after='workings', optional=True),
    dict(id='ledge', name="The Miners' Trestle", after='workings', exit='walk', home='trestle landing'),
    dict(id='floor', name="The Gallery's floor", after='ledge', exit='walk', home='island'),
    dict(id='foundry', name='The Geode Foundry', after='ledge', exit='drop', home='chute pool'),
    dict(id='vent', name='The Ash Garden', after='ledge', exit='walk'),
    dict(id='scriptorium', name='The Scriptorium', after='vent', group='ventside'),
    dict(id='musteryard', name='The Muster Yard', after='vent', group='ventside'),
    dict(id='chapel', name='The Chapel of Embers', after='ventside', exit='walk', home='cloister foot'),
    dict(id='river', name='The Sunless River', after='ledge'),
    dict(id='breach', name='The Grey Breach', after='river', exit='drop', home='plunge pool'),
    dict(id='quiet', name="The Quiet Miner's Drift", after='breach', optional=True),
    dict(id='wake', name='The Wake', after='foundry,chapel,breach,floor', home='island'),
    dict(id='deep', name='The Deep Way', after='wake'),
    dict(id='pitch', name='The Pitch Pit', after='deep', optional=True),
] + ([dict(id='cove', name='The Black Cove', after='deep', exit='walk')] if COVE_FINALE else [])
EVENTS = [dict(id='wake', kind='hold', wing='wake', anchor='island', step=1,
               waves=[dict(at=15, camp='Wake porters'), dict(at=60, camp='Wake zealots'), dict(at=105, camp='Wake crawlers'), dict(at=150, camp=HOLLOWSONG)])]

def make_crumbs():
    W = CAVE['W']; R = CAVE['R']; B = CAVE['B']; D = CAVE['D']; VC = CAVE['VC']
    return [
        dict(cave=W.name, **{'from': round(along('W', 0, -150), 1)}, to=round(along('W', 20, -16), 1), aside=-1.2),   # the dark drift: no lamps, the living lichen
        dict(cave=R.name, **{'from': round(along('R', -150, 52), 1)}, to=round(R.length - 6, 1), aside=-1.5),          # the falls stair
        dict(cave=B.name, **{'from': 4}, to=round(B.length - 12, 1), aside=-1.5),
        dict(cave=VC.name, **{'from': round(along('VC', 91, 146), 1)}, to=round(VC.length - 6, 1), aside=-1),          # the cloister stair home
        dict(cave=D.name, **{'from': 16}, to=round(along('D', 47, 260) - 4, 1), aside=-1.5),                            # the shaft stair
    ]

def make_secrets():
    def inc(slug, name, kind, key, at, prompt=None, text=None, rise=0, aside=None, **kw):
        c = CAVE[key]; a, s = c.local(*at)
        if aside is not None: s = aside
        d = dict(id='secret.adit.' + slug, name=name, kind=kind)
        if prompt: d['prompt'] = prompt
        if text: d['text'] = text
        d.update(kw); d['cave'] = c.name; d['along'] = round(a, 2); d['aside'] = round(s, 2)
        if rise: d['rise'] = rise
        x, _, z = c.at_(a, s); d['at'] = {'x': round(x, 2), 'y': round(z, 2)}
        if 'canonStatus' not in d: d['canonStatus'] = 'GAME-ONLY'
        return d
    return [
        dict(id='secret.adit.shoulder', name="A Pilgrim's Bundle", kind='cache', prompt='Unwrap the bundle in the rocks', at={'x': 7.5, 'y': -352},
             text='Up on the shoulder over the cut, wedged between two rocks out of the wind: a grey wool bundle of the kind the Silent Pilgrims carry, '
                  'and a slate tucked in its knot.', document='doc.adit.pilgrim_note', xp=120, gold=12,
             canonStatus='CANON-EXPANDED: the Silent Pilgrims are CANON (world_bible); this bundle and its slate are GAME-ONLY'),
        inc('nail_key', 'A Rusted Key on a Nail', 'key', 'W', (-5, -95), prompt='Take the key off its nail', aside=-2.4,
            text='Behind a fallen prop in the dark drift, where the lichen gives out for a stride or two, a key hangs on a nail driven into the rock. '
                 'The Company never came this far with a lamp.', xp=60, height=1.4),
        inc('behind_falls', 'Behind the Falls', 'chest', 'G', (35, 77), prompt='Open the chest behind the falls', needs='secret.adit.nail_key',
            text='Behind the sheet of falling water, in the dry under the lip, an old miners\' chest with a rusted lock. The key from the dark drift turns in it.',
            xp=140, gold=40, item='loot.adit.runners_hobnail_boots'),   # (S7) a rare piece: the Adit-Runner's boots
        inc('lamp_niche', "The Lamp-Man's Niche", 'cache', 'G', (33.5, -3.5), prompt='Search the niche by the landing', rise=12,
            text='A niche cut in the rock at the trestle landing\'s back, where the lamp-men kept their oil and their pay.', xp=90, gold=45,
            document='doc.adit.lamp_oil'),   # (S7) their mixing card, a page like a recipe, and more of their pay
        inc('censored_room', 'The Struck-Out Pages', 'note', 'VSx', (149, 118), prompt='Read the struck-out pages',
            text='On a reading desk in the room behind the shelves, a history of the order with whole pages inked over, and one the censor missed.',
            document='doc.adit.censored_history', xp=120),
        inc('censored_box', 'The Censor\'s Box', 'cache', 'VSx', (147, 119.5), prompt='Search the censor\'s box',
            text='Under the desk, a small iron-bound box: the fines the censor took from copyists who wrote what they should not.', xp=60, gold=60),   # (S7) worth the search
        inc('grey_key', 'The Grey Key', 'key', 'Q', (-84, 179), prompt='Take the grey key',
            text='At the drift\'s dead end, on a nail over a miner\'s cot gone grey as ash: a key with no colour at all.', xp=80, height=1.3),
        inc('foremans_box', "The Foreman's Strongbox", 'chest', 'B', (14, 108), prompt="Open the Foreman's strongbox", needs='secret.adit.grey_key',
            text='The Foreman\'s strongbox, its gilt gone dull and grey. The grey key opens it without a sound.', xp=140, gold=55, item='loot.adit.runners_scale_jerkin',   # (S7) a rare piece: the Adit-Runner's jerkin
            canonStatus='GAME-ONLY (the unmade are CANON)'),
        inc('drowned_purse', 'A Drowned Purse', 'cache', 'R', (-117, 36), prompt='Feel in the pool for the purse',
            text='In the Lurker\'s Pool, under a hand of black water, a leather purse snagged on a rock. Somebody\'s month of pay.', xp=80, gold=60),   # (S7) a month's pay
        inc('ember_cap', 'Ember-Cap', 'herb', 'VC', (89, 120), prompt='Pick the ember-cap', aside=-2.2,
            text='Low on the cloister stair\'s wall, where the chapel\'s heat seeps through the rock: a cluster of small red caps that glow like coals.', xp=90,
            item='herb.last_light'),   # (S7) a rare herb (Last-light) to carry out
        *([inc('scaffold_a', "A Rigger's Tin", 'cache', 'BC', (-304, 181), prompt='Search under the scaffold planks', aside=SCAFF_A, rise=rise_to('BC', cz(181), TIER1_Y),
               text="Wedged under a loose plank of the scaffold's first tier, a rigger's tin: twine, a marlinspike and a twist of coins against a wet week.", xp=60, gold=20),
           inc('scaffold_b', 'The Gangway Purse', 'cache', 'BC', (-304, 162), prompt='Feel along the beam under the top tier', aside=SCAFF_A, rise=rise_to('BC', cz(162), TIER2_Y),
               text='Hung on a nail under the top tier, where the bargemen go aboard, a purse nobody has come back for.', xp=60, gold=30)] if 'BC' in CAVE else []),
        inc('kettle_tally', "A Pitch-Boiler's Tally", 'note', 'D', (38, 274), prompt='Read the tally chalked on the board', aside=-8,
            text='A board leaned against the platform\'s end, chalked with barrels of pitch and the name of whoever boils it: "Old Kettle, north-west, past the sleepers."',
            document='doc.adit.kettle_tally', xp=70),
    ]

PLACES = [   # landmarks (named like their caves, the maps show each one's levels): discovered within 18 m of the point on the ground plane
    ('The Sealed Adit', (0, -368), 6), ('The Cage Bowl', (-2, -292), 14), ("Lusk's Tally Hall", (0, -177), 12), ('The Flooded Working', (-60, -238), 10),
    ('The Pen', (56, -181), 8), ('The Singing Gallery', (62, 18), 18), ('The Geode Foundry', (186, 13), 18), ('The Ash Garden', (158, 62), 15),
    ('The Scriptorium', (172, 116), 12), ('The Muster Yard', (217, 106), 12), ('The Chapel of Embers', (130, 152), 16), ('The Sunless River', (-122, 39), 16),
    ('The Grey Breach', (5, 102), 14), ('The Deep Way', (63, 154), 8), ('The Rail Hall', (-20, 300), 18), ('The Dead Line', (-170, 297), 8),
    ('The Black Cove', (-304, 185), 18)]

# ---------------------------------------------------------------- checks
def fail(msg): sys.exit('LAYOUT CHECK FAILED: ' + msg)

def check(camps, decks):
    bad = []
    # 1. Every child's mouth on its parent's floor (FloorSmooth), its first floored ring past the parent's open air.
    for c in CAVES:
        if c.parent:
            if c.parent.floor_smooth(*c.at) is None: bad.append('%s: its mouth is not on %s\'s floor' % (c.name, c.parent.name))
    # 2. Every join's end in its target's open air, at the lip planned.
    for c in CAVES:
        if c.join:
            e = c.rings[-1]
            if not c.join.open(e[0], e[4] + .3, e[1], .86): bad.append('%s: its end is not in %s\'s air' % (c.name, c.join.name))
            want = c.pts[-1][4] - (c.join.floor_smooth(e[0], e[1]) or 0)
            if abs(c.join_rise - want) > .3: bad.append('%s: lip %.2f, planned %.2f' % (c.name, c.join_rise, want))
    # 3. No cave's rock in another's way (the knoll stands 2-5 m round the walls and up to about a third of the height over the roof),
    #    but where a branch opens out of its parent or a cave ends in the one it joins.
    for ai, a in enumerate(CAVES):
        for b in CAVES[ai + 1:]:
            ra, rb = a.np, b.np
            dx = ra[:, None, 0] - rb[None, :, 0]; dz = ra[:, None, 1] - rb[None, :, 1]
            gap = np.sqrt(dx * dx + dz * dz) - ra[:, None, 2] - rb[None, :, 2]
            near = np.argwhere(gap < 6.5)
            if len(near) == 0: continue
            for i, j in near[::7]:
                p, q = a.rings[i], b.rings[j]
                if linked(a, b): continue
                lo_a, hi_a = p[4] - 1.5, p[4] + p[3] * 1.19 + 5.5 + .35 * p[3]
                lo_b, hi_b = q[4] - 1.5, q[4] + q[3] * 1.19 + 5.5 + .35 * q[3]
                if lo_a < hi_b and lo_b < hi_a:
                    bad.append('%s at %.0f m and %s at %.0f m: rock in each other\'s way (%.1f m apart, floors %.1f and %.1f)' % (a.name, p[5], b.name, q[5], gap[i, j], p[4], q[4])); break
    # 3b. A cave's floor gives way only at its own mouth (in its parent's air) and at its end in the cave it joins.
    for c in CAVES:
        for i, r in enumerate(c.rings):
            if c.floored[i]: continue
            if c.parent and r[5] < 40: continue
            if c.join and r[5] > c.length - 45: continue
            # (S6) a cave's closing end in the air of a branch opening out of it there (the Dead Line's end in the Black Cove's mouth): the
            # branch's own floor carries on under it.
            if any(o.parent is c and o.floor_at(r[0], r[1]) is not None and abs(o.floor_at(r[0], r[1]) - r[4]) < 1 for o in CAVES): continue
            bad.append('%s: no floor of its own at %.0f m (%.1f, %.1f)' % (c.name, r[5], r[0], r[1])); break
    # 4. Slopes: no floor steeper than 23 degrees between rings a metre and more apart (agents climb 45; feet a little less).
    for c in CAVES:
        R = c.rings; i = 0
        while i < len(R) - 1:
            j = i + 1
            while j < len(R) - 1 and R[j][5] - R[i][5] < 1.5: j += 1
            run = R[j][5] - R[i][5]
            if run > .5 and abs(R[j][4] - R[i][4]) / run > math.tan(math.radians(23.5)) and c.floored[i] and c.floored[j]:
                bad.append('%s: %.0f degrees at %.0f m' % (c.name, math.degrees(math.atan(abs(R[j][4] - R[i][4]) / run)), R[i][5]))
            i = j
    for d in decks:
        if d.get('radius'): continue
        c = CAVE[d['_key']]; y0 = c.at_(d['from'])[1] + d['rise']; y1 = c.at_(d['to'])[1] + d['rise2']
        x0, _, z0 = c.at_(d['from'], d['aside']); x1, _, z1 = c.at_(d['to'], d['aside2'])
        run = math.hypot(x1 - x0, z1 - z0)
        if run > .1 and math.degrees(math.atan(abs(y1 - y0) / run)) > 20.5: bad.append('deck %s: %.1f degrees' % (d['name'], math.degrees(math.atan(abs(y1 - y0) / run))))
    # 5. Every camp on its floor (its centre and its spread), or on its deck.
    def deck_top(x, z):
        tops = []
        for d in decks:
            c = CAVE[d['_key']]
            if d.get('radius'):
                cx, cy, cz = c.at_(d['from'], d['aside'])
                if math.hypot(x - cx, z - cz) <= d['radius']: tops.append(cy + d['rise'])
                continue
            n = 40
            for k in range(n + 1):
                u = k / n; s = d['from'] + (d['to'] - d['from']) * u; a = d['aside'] + (d['aside2'] - d['aside']) * u
                px, py, pz = c.at_(s, a)
                if math.hypot(x - px, z - pz) <= d['width'] / 2 + .05:
                    tops.append(deck_y(d, u)); break
        return tops
    for cp in camps:
        c = CAVE[cp['_key']]; x, z = cp['center']['x'], cp['center']['y']
        if cp.get('route'):
            for p in cp['route']:
                if not any(o.floor_at(p['x'], p['y']) is not None for o in CAVES): bad.append('%s: a point of its road %s is on no floor' % (cp['name'], (p['x'], p['y'])))
            continue
        if cp.get('rise'):
            want = c.at_(cp['along'])[1] + cp['rise']
            tops = deck_top(x, z)
            if not any(abs(t - want) < .6 for t in tops): bad.append('%s: no deck at +%.1f under it (decks %s)' % (cp['name'], cp['rise'], [round(t - c.at_(cp['along'])[1], 1) for t in tops]))
            continue
        if c.floor_at(x, z) is None: bad.append('%s: its centre is on no floor of %s' % (cp['name'], c.name))
        half = c.half_at(cp['along'])
        if abs(cp['aside']) + cp['radius'] > half - .3: bad.append('%s: its ground (aside %.1f, radius %.1f) runs into the wall (half %.1f)' % (cp['name'], cp['aside'], cp['radius'], half))
        for k in range(8):
            ang = k * math.pi / 4; px, pz = x + math.cos(ang) * cp['radius'], z + math.sin(ang) * cp['radius']
            if c.floor_at(px, pz) is None and not any(o.floor_at(px, pz) is not None and abs(o.floor_at(px, pz) - c.floor_at(x, z)) < 1 for o in CAVES):
                bad.append('%s: part of its ground is on no floor' % cp['name']); break
    # 6. Deck ends meet a floor or another deck at their height.
    for d in decks:
        if d.get('radius'): continue
        c = CAVE[d['_key']]
        for which, s, a, r in (('start', d['from'], d['aside'], d['rise']), ('end', d['to'], d['aside2'], d['rise2'])):
            if which in d.get('_free', ()): continue
            x, y, z = c.at_(s, a); y = deck_y(d, 0 if which == 'start' else 1); ok = False
            for o in CAVES:
                f = o.floor_at(x, z)
                if f is not None and abs(f - y) < .35: ok = True
            for e in decks:
                if e is d: continue
                # Deck to deck within 1.6 m: this model's Geode Foundry floor lies 1.5 m under Unity's (Hollow), so the landing's end is
                # set to Unity's catwalk height (2026-10-14; AditNavDiagnostics is the judge of the join).
                if any(abs(t - y) < 1.6 for t in deck_top_one(e, x, z)): ok = True
            if not ok: bad.append('deck %s: its %s (%.1f, %.1f, %.1f) meets no floor and no deck' % (d['name'], which, x, y, z))
    # 8. Everything inside the zone with its rock.
    for c in CAVES:
        r = c.np; ext = max(np.abs(r[:, 0]).max() + 0, np.abs(r[:, 1]).max())
        m = np.max(np.maximum(np.abs(r[:, 0]), np.abs(r[:, 1])) + r[:, 2] + 6)
        if m > SIZE / 2 - 5: bad.append('%s: reaches %.0f m out, past the zone\'s edge' % (c.name, m))
    if bad:
        for b in bad[:60]: print('  ' + b)
        fail('%d problems' % len(bad))

def deck_top_one(d, x, z):
    c = CAVE[d['_key']]
    if d.get('radius'):
        cx, cy, cz = c.at_(d['from'], d['aside'])
        return [cy + d['rise']] if math.hypot(x - cx, z - cz) <= d['radius'] + .3 else []
    n = 60; out = []
    for k in range(n + 1):
        u = k / n; s = d['from'] + (d['to'] - d['from']) * u; a = d['aside'] + (d['aside2'] - d['aside']) * u
        px, py, pz = c.at_(s, a)
        if math.hypot(x - px, z - pz) <= d['width'] / 2 + .3: out.append(deck_y(d, u))
    return out

def deck_y(d, u):
    """A deck's top u of the way along it: straight between its ends' tops (ZoneBuilder.PrepareDeck)."""
    c = CAVE[d['_key']]; y0 = c.at_(d['from'])[1] + d['rise']; y1 = c.at_(d['to'])[1] + d['rise2']
    return y0 + (y1 - y0) * u

# ---------------------------------------------------------------- the zone
def zone(camps):
    props = []
    for c in CAVES:
        p = {'kind': 'cavern', 'name': c.name, 'at': {'x': round(c.at[0], 3), 'y': round(c.at[1], 3)}, 'rotation': round(c.yaw, 3), 'variant': 2,
             'plan': [round(v, 3) for row in c.plan for v in row], 'style': c.style}
        if c.within: p['within'] = CAVE[c.within].name
        if c.rise: p['rise'] = c.rise
        if c.joins: p['joins'] = CAVE[c.joins].name
        props.append(p)
    main_at = CAVE['W'].at; yard = (main_at[0], main_at[1] - 14)
    clean = lambda lst: [{k: v for k, v in d.items() if not k.startswith('_')} for d in lst]
    return {
        'id': 'zone.adit', 'displayName': 'The Sealed Adit', 'subtitle': 'Old workings under the Shattered Peaks',
        'canonStatus': 'CANON-EXPANDED', 'dungeon': True,
        'canonNote': 'Old mining tunnels under the Shattered Peaks with a mag-rail line deep down (CANON, book2 ch.19); stagnant water, rusted '
                     'iron ladders and lum-tubes in the old workings (CANON, book2 ch.19); aether-geodes cut from deep seams (CANON, book1 ch.3); the '
                     'Wasting eating the Peaks\' stone (CANON, world_bible); the Sandthrone, the Cult of Ash, the goblins and their vibration-locks, '
                     'the Hollow Men and the Silent Pilgrims (CANON). GAME-ONLY: the Sandthrone cutting geodes here and running them south on the old '
                     'line, the Cult\'s chapter-house, the breach, the Singing Gallery\'s trestle, bridge and island, the singing door, every person, '
                     'camp, place and event. Docs/DUNGEON_DESIGN.md and Docs/ADIT_REWORK.md.',
        'size': SIZE, 'levelMin': 10, 'levelMax': 12, 'roadsNote': 'the cart track from the cut to the mouth', 'biome': 'mountain',
        'weather': [{'kind': 'overcast', 'weight': 3}, {'kind': 'fair', 'weight': 1}, {'kind': 'flurries', 'weight': 1}],
        'worldMapPosition': {'x': 0.5, 'y': 0.56}, 'worldMapNote': 'Under the Shattered Peaks, through the Sealed Adit.',
        'flatRadius': 14, 'hillHeight': 3, 'seed': 7717,
        'shapes': [{'name': 'The Adit cut', 'center': {'x': yard[0], 'y': yard[1] + 2}, 'radius': 12, 'height': 0, 'blend': 8},
                   {'name': 'The shoulder over the adit', 'center': {'x': main_at[0], 'y': main_at[1] + 22}, 'radius': 8, 'height': 9, 'blend': 14}],
        'lighting': {'sunPitch': 38, 'sunYaw': -20, 'sunIntensity': .95, 'sunColor': '#E9E4DA', 'ambientSky': '#6E7E99', 'ambientEquator': '#5E6670',
                     'ambientGround': '#2C2F33', 'fogColor': '#8C98A8', 'fogStart': 40, 'fogEnd': 220, 'skyTint': '#5E7096', 'skyExposure': 1, 'skyHaze': 1},
        'spawns': {'player': {'x': yard[0], 'y': yard[1]}, 'playerFacing': 0, 'companion': {'x': yard[0] + 2, 'y': yard[1] - 2},
                   'recovery': {'x': yard[0] - 2, 'y': yard[1] - 2}, 'leash': 24, 'enemies': []},
        'objectives': ['Find out what the Sandthrone are cutting under the pass', 'Win the three rail sigils off the Gallery\'s wings', 'Stop the carriage on the dead line'] + (['Bring down Danner on his barge in the Black Cove'] if COVE_FINALE else []),
        'exits': [{'to': 'zone.peaks', 'name': 'The cut back up to the Peaks', 'at': {'x': yard[0], 'y': yard[1] - 9}, 'arrive': {'x': -140.5, 'y': -42}, 'radius': 3.5}],
        'roads': [{'name': 'Adit track', 'width': 3, 'points': [{'x': yard[0], 'y': yard[1] - 11}, {'x': yard[0], 'y': yard[1]}, {'x': main_at[0], 'y': main_at[1] + 1}]}],
        'clearings': [{'name': 'The Adit cut', 'center': {'x': yard[0], 'y': yard[1] + 2}, 'radius': 9}],
        'landmarks': [{'name': n, 'at': {'x': x, 'y': z}, 'radius': r, 'canonStatus': 'CANON-EXPANDED' if n == 'The Sealed Adit' else 'GAME-ONLY'} for n, (x, z), r in PLACES],
        'secrets': SECRETS,
        'props': props,
        'decks': clean(DECKS), 'drops': DROPS, 'caveWater': WATER, 'gates': GATES, 'anchors': ANCHORS,
        'wings': WINGS, 'events': EVENTS, 'crumbs': CRUMBS,
        'camps': clean(camps),
    }

# ---------------------------------------------------------------- pictures (the plan by floor height; the levels side on)
def pictures(camps):
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        return
    xs = [r[0] for c in CAVES for r in c.rings]; zs = [r[1] for c in CAVES for r in c.rings]
    x0, x1, z0, z1 = min(xs) - 30, max(xs) + 30, min(zs) - 30, max(zs) + 30; s = 2.0
    W_, H_ = int((x1 - x0) * s), int((z1 - z0) * s)
    img = Image.new('RGB', (W_, H_), (22, 21, 20)); g = ImageDraw.Draw(img)
    P = lambda x, z: ((x - x0) * s, H_ - (z - z0) * s)
    def colour(y):
        t = max(0, min(1, -y / 106)); stops = [(0, (215, 205, 150)), (.4, (120, 175, 120)), (.6, (90, 140, 200)), (.8, (150, 100, 190)), (1, (200, 80, 90))]
        for (a, ca), (b, cb) in zip(stops, stops[1:]):
            if a <= t <= b: f = (t - a) / (b - a); return tuple(int(ca[k] + (cb[k] - ca[k]) * f) for k in range(3))
        return stops[-1][1]
    for c in CAVES:
        for i, r in enumerate(c.rings[::2]):
            cx, cz = P(r[0], r[1]); rr = r[2] * s; col = colour(r[4]) if c.floored[i * 2] else (60, 60, 60)
            g.ellipse((cx - rr, cz - rr, cx + rr, cz + rr), fill=col)
    for w in WATER:
        c = next(o for o in CAVES if o.name == w['cave'])
        if w.get('radius'):
            cx, _, cz = c.at_(w['from'], w['aside']); a = P(cx, cz); rr = w['radius'] * s
            g.ellipse((a[0] - rr, a[1] - rr, a[0] + rr, a[1] + rr), fill=(40, 90, 160))
        elif w.get('flood'):   # (S6) wall to wall: the cave's own width
            for k in range(30):
                sa = w['from'] + (w['to'] - w['from']) * k / 29; x, _, z = c.at_(sa); a = P(x, z); rr = c.half_at(sa) * .8 * s
                g.ellipse((a[0] - rr, a[1] - rr, a[0] + rr, a[1] + rr), fill=(30, 60, 120))
        else:
            for k in range(30):
                sa = w['from'] + (w['to'] - w['from']) * k / 29; x, _, z = c.at_(sa, w['aside']); a = P(x, z); rr = w['half'] * s
                g.ellipse((a[0] - rr, a[1] - rr, a[0] + rr, a[1] + rr), fill=(40, 90, 160))
    for d in DECKS:
        c = CAVE[d['_key']]
        if d.get('radius'):
            cx, _, cz = c.at_(d['from'], d['aside']); a = P(cx, cz); rr = d['radius'] * s
            g.ellipse((a[0] - rr, a[1] - rr, a[0] + rr, a[1] + rr), outline=(250, 240, 220), width=2); continue
        a = P(*[c.at_(d['from'], d['aside'])[k] for k in (0, 2)]); b = P(*[c.at_(d['to'], d['aside2'])[k] for k in (0, 2)])
        g.line((a, b), fill=(250, 240, 220), width=max(2, int(d['width'] * s * .6)))
    for d in DROPS:
        fc = next(o for o in CAVES if o.name == d['fromCave']); tc = next(o for o in CAVES if o.name == d['toCave'])
        a = P(*[fc.at_(d['from'], d['fromAside'])[k] for k in (0, 2)]); b = P(*[tc.at_(d['to'], d['toAside'])[k] for k in (0, 2)])
        g.line((a, b), fill=(90, 220, 255), width=4); g.ellipse((b[0] - 6, b[1] - 6, b[0] + 6, b[1] + 6), outline=(90, 220, 255), width=3)
    for gt in GATES:
        c = next(o for o in CAVES if o.name == gt['cave']); x, _, z = c.at_(gt['along']); a = P(x, z)
        g.rectangle((a[0] - 5, a[1] - 5, a[0] + 5, a[1] + 5), fill=(240, 60, 40)); g.text((a[0] + 7, a[1] - 6), gt['title'], fill=(255, 150, 120))
    for cp in camps:
        x, z = cp['center']['x'], cp['center']['y']; a = P(x, z); r = 6 if cp.get('elite') else 4
        col = (250, 210, 80) if cp.get('elite') else (230, 60, 50)
        if cp.get('after'): col = (200, 120, 255)
        if cp.get('route'):
            pts = [P(p['x'], p['y']) for p in cp['route']]; g.line(pts, fill=(255, 140, 60), width=1)
        g.ellipse((a[0] - r, a[1] - r, a[0] + r, a[1] + r), fill=col)
        if cp.get('elite'): g.text((a[0] + 8, a[1] - 6), cp['name'], fill=(255, 235, 170))
    for c in CAVES:
        r = c.rings[len(c.rings) // 2]; a = P(r[0], r[1]); g.text((a[0] - 30, a[1] - 22), c.name, fill=(255, 255, 255))
    img.save(os.path.join(DOCS, 'art', 'adit-plan.png'))
    # Side on: each cave's floor along x (west to east), at its height.
    H2 = 700; img = Image.new('RGB', (W_, H2), (22, 21, 20)); g = ImageDraw.Draw(img)
    Q = lambda x, y: ((x - x0) * s, 40 + (-y) * 6)
    for c in CAVES:
        pts = [Q(r[0], r[4]) for r in c.rings]; g.line(pts, fill=colour(c.rings[len(c.rings) // 2][4]), width=3)
        r = c.rings[len(c.rings) // 2]; a = Q(r[0], r[4]); g.text((a[0], a[1] - 14), c.name, fill=(255, 255, 255))
    for d in DECKS:
        c = CAVE[d['_key']]
        a = Q(c.at_(d['from'], d['aside'])[0], c.at_(d['from'])[1] + d['rise']); b = Q(c.at_(d['to'], d['aside2'])[0], c.at_(d['to'])[1] + d['rise2'])
        g.line((a, b), fill=(250, 240, 220), width=2)
    img.save(os.path.join(DOCS, 'art', 'adit-levels.png'))

def summary(camps):
    for c in CAVES:
        nf = sum(1 for f in c.floored if not f)
        print('%-26s %-11s len %6.1f  rings %4d  unfloored %3d  floors %.1f..%.1f%s' % (c.name, c.style, c.length, len(c.rings), nf, min(r[4] for r in c.rings), max(r[4] for r in c.rings),
              ('  joins %s +%.1f' % (c.join.name, c.join_rise)) if c.join else ''))
    mobs = sum(c['count'] for c in camps); start = sum(c['count'] for c in camps if not c.get('after') and not c.get('rare'))
    print('camps %d (patrols %d, released later %d, optional %d, elites %d); mobs %d (%d from the start)' % (len(camps), sum(1 for c in camps if c.get('route')), sum(1 for c in camps if c.get('after')),
          sum(1 for c in camps if c.get('optional')), sum(1 for c in camps if c.get('elite')), mobs, start))
    print('cave length %.0f m' % sum(c.length for c in CAVES))

if __name__ == '__main__':
    build_caves()
    make_decks(); make_works()
    camps = make_camps()
    CRUMBS = make_crumbs(); SECRETS = make_secrets()
    summary(camps)
    check(camps, DECKS)
    z = zone(camps); live = '--live' in sys.argv
    out = ZONES if live else os.path.join(HERE, 'staged')
    # Windows line ends, as the game's other content files have them in the working tree (git keeps LF).
    with io.open(os.path.join(out, 'adit.json'), 'w', encoding='utf-8', newline='\r\n') as f: f.write(json.dumps(z, indent=2, ensure_ascii=False) + '\n')
    pictures(camps)
    print('written to', out)
