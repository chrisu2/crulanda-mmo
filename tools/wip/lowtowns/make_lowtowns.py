"""
make_lowtowns.py: writes the Lowtowns and the Rust Market (zone.lowtowns, Docs/ZONE_LOWTOWNS.md, Phase 9.1, levels 15-20) and checks it.

Writes (byte for byte the same each run; JSON with two-space indents and CRLF, as the other content files):
  EncounterContent/Zones/lowtowns.json    the zone: walls, the river, the Gate Ward, the Rusty Spoon, the Tenement Rows (laid out by the
                                          infill below), the Rust Market, the Shard-Burners' Yard, the Docks, the Drain Courts, the
                                          Outfall, the Forge District's shut gate; people and life; camps 15-20; nodes; stations; secrets
  EncounterContent/Quests/lowtowns.json   the Lowtowners, the quest items, the Chronicle and lore pages, the breadcrumb, the main chain of
                                          five, eight side quests and the board's bounties
  EncounterContent/Lore/lowtowns.json     the zone's chapter of the book of lore
  EncounterContent/Items/lowtowns.json    salt and the bone harpoon (thrown; WeaveEaterRules) and who sells them
Run from anywhere: python tools/wip/lowtowns/make_lowtowns.py. It stops on the first problem its checks find (overlapping buildings, a
building in a road or the river, a clue naming a landmark, a person with no stories, a camp out of 15-20...).
Lore labels: every line says CANON (with book and chapter), CANON-EXPANDED, GAME-ONLY or PROVISIONAL. No machines (Chris, 2026-10-11).
"""
import json, math, os, random, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..', 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent'))
HALF = 240
def V(x, z): return {"x": round(x, 2), "y": round(z, 2)}
def problem(msg): sys.exit('PROBLEM: ' + msg)

# ======================================================================================================================================
# Geometry: roads, the river, footprints
# ======================================================================================================================================
def seg_dist(p, a, b):
    ax, az = a; bx, bz = b; px, pz = p; dx, dz = bx - ax, bz - az; L = dx * dx + dz * dz
    t = 0 if L == 0 else max(0, min(1, ((px - ax) * dx + (pz - az) * dz) / L))
    return math.hypot(px - ax - t * dx, pz - az - t * dz)
def path_dist(p, pts): return min(seg_dist(p, pts[i], pts[i + 1]) for i in range(len(pts) - 1))

ROADS = [   # name, width, points (Gate Street first: the rare posting's courier walks the first road)
    ("Gate Street", 7, [(-200, 0), (-170, 2), (-140, 4), (-110, 2), (-75, -6), (-40, -14), (-14, -18), (14, -18), (40, -12), (75, -4), (110, 2), (150, 2), (185, 0), (203, 0)]),
    ("The Toll Road", 6, [(-246, 2), (-200, 0)]),
    ("Drain Lane", 5, [(-140, 4), (-138, 40), (-134, 80), (-128, 120), (-124, 150), (-138, 186)]),
    ("High Lane", 6, [(-110, 2), (-90, 30), (-60, 60), (-30, 88), (-12, 96), (22, 96), (45, 92), (70, 80), (92, 62)]),
    ("Wharf Walk", 5, [(-27, -16), (-25, -50), (-25, -90), (-21, -130), (-16, -170), (-14, -205)]),
    ("Rows Lane", 5, [(-75, -6), (-80, -50), (-86, -100), (-90, -150), (-94, -205)]),
    ("Cinder Row", 4, [(-176, -80), (-130, -82), (-86, -80), (-27, -84)]),
    ("Pell Row", 4, [(-176, -142), (-130, -144), (-90, -150), (-50, -158), (-18, -164)]),
    ("Market Lane", 6, [(75, -4), (80, 30), (84, 44), (82, 80), (86, 120), (98, 170)]),
    ("Teeth Lane", 4, [(150, 2), (162, 60), (172, 120), (180, 160)]),
    ("Skewer Lane", 5, [(24, 40), (60, 44), (86, 40), (130, 44), (160, 54)]),
    ("Burners' Row", 5, [(110, 2), (115, -50), (122, -100), (140, -140), (168, -168)]),
    ("Court Lane", 4, [(-128, 120), (-108, 150), (-104, 178)]),
    ("North Rows", 4, [(-60, 60), (-70, 110), (-80, 160), (-88, 205)]),
    ("Weir Lane", 4, [(-30, 88), (-36, 130), (-40, 180)]),
    ("Bridgefoot", 4, [(22, 96), (30, 140), (40, 190)]),
    # the back alleys (2026-10-16: the blocks between the lanes filled with a second row of terraces)
    ("Gutter Alley", 3.5, [(-176, -180), (-120, -176), (-96, -170)]),
    ("Lamp Alley", 3.5, [(-60, 60), (-34, 34), (-22, 22)]),
    ("Weir Back", 3.5, [(40, 140), (80, 132), (120, 140), (165, 150)]),
    ("Cooper's Row", 3.5, [(115, -50), (150, -48), (190, -50)]),
    ("Stair Alley", 3.5, [(-176, -52), (-140, -54), (-112, -50)]),
    ("Kettle Alley", 3.5, [(-60, -40), (-50, -60), (-40, -84)]),
]
RIVER = [(-12, 245), (-6, 180), (4, 120), (8, 60), (2, 0), (-4, -60), (2, -120), (10, -170), (6, -215), (0, -245)]
RIVER_W = 16
OUTFALL = [(203, -205), (160, -209), (120, -212), (70, -214), (17, -214)]   # it stops on the bank short of the river (a creek joined to a deeper one would be carved to the deeper's depth)
def river_x(z):
    for i in range(len(RIVER) - 1):
        (x0, z0), (x1, z1) = RIVER[i], RIVER[i + 1]
        if z1 <= z <= z0: return x0 + (x1 - x0) * (z0 - z) / (z0 - z1)
    return RIVER[-1][0]
def near_water(p, m): return path_dist(p, RIVER) < RIVER_W / 2 + m or path_dist(p, OUTFALL) < 3 + m

def obb(cx, cz, w, d, rot):
    r = math.radians(rot); c, s = math.cos(r), math.sin(r)
    ax = (c, -s); az = (s, c)     # local x and z in world (Unity's yaw: x' = x c + z s, z' = -x s + z c)
    pts = []
    for sx, sz in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
        pts.append((cx + ax[0] * sx * w / 2 + az[0] * sz * d / 2, cz + ax[1] * sx * w / 2 + az[1] * sz * d / 2))
    return pts
def overlap(A, B):
    for poly in (A, B):
        for i in range(4):
            x1, z1 = poly[i]; x2, z2 = poly[(i + 1) % 4]; nx, nz = -(z2 - z1), x2 - x1
            pa = [nx * x + nz * z for x, z in A]; pb = [nx * x + nz * z for x, z in B]
            if max(pa) < min(pb) or max(pb) < min(pa): return False
    return True
def inside(p, poly):
    for i in range(4):
        x1, z1 = poly[i]; x2, z2 = poly[(i + 1) % 4]
        if (x2 - x1) * (p[1] - z1) - (z2 - z1) * (p[0] - x1) < 0: return False
    return True
def facing(d):   # the yaw whose front (-z local) looks along the unit vector d
    return math.degrees(math.atan2(-d[0], -d[1])) % 360

FOOT = []      # (name, poly) of everything solid laid so far
def solid_clear(poly, gap=0.0, roads=True, water=True, who=''):
    big = poly if gap <= 0 else grow(poly, gap)
    for n, q in FOOT:
        if overlap(big, q): return 'overlaps ' + n
    pts = poly + [((poly[i][0] + poly[(i + 1) % 4][0]) / 2, (poly[i][1] + poly[(i + 1) % 4][1]) / 2) for i in range(4)]
    if roads:
        for name, w, rp in ROADS:
            for p in pts:
                if path_dist(p, rp) < w / 2 + .6: return 'stands in ' + name
    if water:
        for p in pts:
            if near_water(p, 1.5): return 'stands in the water'
    for p in pts:
        if abs(p[0]) > HALF - 4 or abs(p[1]) > HALF - 4: return 'off the edge'
    return None
def grow(poly, g):
    cx = sum(p[0] for p in poly) / 4; cz = sum(p[1] for p in poly) / 4; out = []
    for x, z in poly:
        dx, dz = x - cx, z - cz; L = math.hypot(dx, dz) or 1; out.append((x + dx / L * g * 1.41, z + dz / L * g * 1.41))
    return out

PROPS = []
def prop(kind, name, x, z, rot=0, size=None, variant=0, solid=None, check=True, **kw):
    p = {"kind": kind}
    if name: p["name"] = name
    p["at"] = V(x, z)
    if rot: p["rotation"] = round(rot % 360, 1)
    if size: p["size"] = V(*size)
    if variant: p["variant"] = variant
    p.update(kw); PROPS.append(p)
    if solid:
        poly = obb(x, z, solid[0], solid[1], rot)
        if check:
            why = solid_clear(poly, who=name or kind)
            if why: problem('%s %s at (%s, %s) %s' % (kind, name, x, z, why))
        FOOT.append((name or kind, poly))
    return p

# Open ground kept clear of the infill (courts, yards, squares, camps, the quay's walk): centre and radius.
CLEAR = []
def keep(x, z, r): CLEAR.append((x, z, r))

# ======================================================================================================================================
# The places, laid by hand
# ======================================================================================================================================
# The walls: Argentis's outer wall on the west (its gate open: the Gate Ward), the Forge District's on the east (its gate shut).
prop("town_wall", "Argentis's outer wall", -200, 0, 90, (480, 10), 1)
prop("town_wall", "The Forge District's wall", 206, 0, 90, (480, 14), 1)   # open onto the Forge District (2026-10-19, tools/wip/forge/make_forge.py)
FOOT.append(("west wall", obb(-200, 0, 3.6, 480, 0))); FOOT.append(("forge wall", obb(206, 0, 3.6, 480, 0)))
for i, (x, z, h) in enumerate([(222, 150, 34), (228, 70, 40), (218, -20, 30), (230, -90, 38), (221, -160, 32), (226, 212, 28), (232, 20, 26)]):
    prop("chimney_stack", "Foundry stack", x, z, 0, (0, h), i % 3)

# ---- The Gate Ward (west) ----
prop("toll_house", "The toll-house", -188, 9.5, 0, (5, 4), solid=(5.6, 4.6), interact="Read the toll-book", item="item.warrant_page", once=True)
keep(-185, 0, 9)
prop("holding_house", "The Holding House", -172, 46, 0, (14, 10), solid=(14.6, 10.6))
prop("lane_wall", "Holding yard wall", -180.5, 34, 90, (12, 2.8), solid=(12, .6))
prop("lane_wall", "Holding yard wall", -163.5, 34, 90, (12, 2.8), solid=(12, .6))
keep(-172, 34, 8)
prop("cobbles", "Holding yard", -172, 34, 0, (16, 12))
for k, (x, z) in enumerate([(-170, -15), (-170, -37)]):
    prop("lane_wall", "Carters' yard wall", x, z, 0, (22, 2.6), 1 if k == 0 else 0, solid=(22, .6))
for x in (-181, -159):
    prop("lane_wall", "Carters' yard wall", x, -26, 90, (22, 2.6), solid=(22, .6), check=False)
keep(-170, -26, 10)
prop("cobbles", "Carters' yard", -170, -26, 0, (21, 21))
prop("cart", "Carter's cart", -175, -21, 30, solid=(2.6, 1.6)); prop("wagon", "Toll-road wagon", -163, -30, 80, solid=(3.4, 2))
prop("crates", None, -177, -32, 0, variant=2); prop("barrels", None, -164, -20, 0, variant=1); prop("hitch", None, -170, -34, 0)
prop("haystack", None, -176, -27)
prop("board", "Notice board", -178, -6, 180, interact="Read the notices", canonStatus="CANON-EXPANDED (the Council's men post the Seekers' wanted bills; the Lowtowners their bounties)")
prop("cobbles", "The Gate Ward square", -184, 2, 90, (18, 30))

# ---- The Rusty Spoon (CANON, book1 ch.1:9) ----
# Inn variant 4: the Spoon's own building (ZoneBuilder.Spoon.cs; Chris 2026-10-16: "each zone has to be unique not reused assets").
prop("inn", "The Rusty Spoon", -125, 18, 0, (14, 10), variant=4, solid=(15.6, 11.6))
prop("kitchen", "The Spoon's kitchen", -121.4, 24.65, 180, solid=(5.6, 3.6), check=False)
keep(-125, 9, 6)
prop("lane_lamp", None, -133, 11.5, 0, variant=1); prop("lane_lamp", None, -117, 11.5, 0, variant=1)
prop("barrels", None, -134, 20, 90, variant=2)

# ---- The bridges over the Chemical River (CANON, book1 ch.15-16) ----
prop("brick_bridge", "The Sett Bridge", river_x(-18), -18, 90, (26, 7.5))
prop("brick_bridge", "The Rag Bridge", river_x(96), 96, 90, (26, 6.5))
prop("brick_bridge", "The Outfall footbridge", 120, -212, 0, (12, 3.2))

# ---- The Docks (CANON as a place of honest labour, book1 ch.3:17): three quays on the west bank, barges poled, no cranes ----
for k, z in enumerate((-55, -105, -158)):
    bank = river_x(z) - RIVER_W / 2 - 1.2
    prop("dock", "Quay %d" % (k + 1), bank, z, 270, (30, 5.5), (2, 1, 3)[k])
    FOOT.append(("quay %d" % (k + 1), obb(bank + 2.5, z, 6, 30, 0)))
    keep(bank - 5, z, 8)
for x, z, r in [(river_x(-200) - 1, -200, 15), (river_x(20) + 2, 20, 200), (river_x(150), 150, 160)]:
    prop("barge", "A barge on the river", x, z, r, (9, 0), 1)
prop("crates", None, -28, -62, 0, variant=2); prop("barrels", None, -28, -112, 90, variant=2); prop("crates", None, -27, -150, 0, variant=1)

# ---- The Tenement Rows: the named households' terraces and the soup-hall, the oven, the dyer's vats, the well ----
HOMES = {}   # house name -> terrace
def home(name, x, z, rot, w, variant=0, style=None):
    kw = {"style": style} if style else {}
    HOMES[name] = prop("tenement", name, x, z, rot, (w, 8), variant, solid=(w, 9.6), **kw)
home("Pell's tenement", -108, -134, 0, 18, 1)      # Gytha Pell's, on Pell Row's north side (front south, onto the row)
home("Coke's tenement", -150, -70, 180, 16, 3)
home("Sump's tenement", -110, -92, 0, 16, 2)
home("Barrow's tenement", -60, -70, 180, 18, 5)
home("Tolley's tenement", -150, -132, 0, 16, 4)
home("Rook's tenement", -130, -36, 0, 18, 1)
home("Fenn's tenement", -46, -96, 0, 16, 6)
home("Mallow's tenement", -60, 40, 45, 16, 7)
prop("soup_hall", "The soup-hall", -55, -122, 0, (10, 16), solid=(10.8, 16.8))
keep(-55, -136, 7)
prop("cobbles", "Soup-hall steps", -55, -134, 0, (12, 8))
prop("oven", "Sump's bakehouse", -98, -60, 270, solid=(3.6, 3.6))
prop("leathershop", "Mallow's leather shop", -42, 20, 200, solid=(7.6, 6.6))
prop("tannery", "The tanning yard", -38, -136, 90, solid=(4.6, 4.4))
prop("dye_vat", "The dyer's vats", -120, -112, 0, solid=(4.8, 2))
keep(-120, -116, 5)
prop("well", "Rows well", -78, -96, 0, solid=(2.4, 2.4))
keep(-78, -96, 5)
prop("puddle", None, -82, -102, 0, (1.6, 0)); prop("puddle", None, -112, -146, 0, (1.2, 0))

# ---- The Drain Courts (north-west): closed courts round drain-mouths of red clay brick (CANON, book1 ch.9:47) ----
COURTS = [("The first court", -150, 130, 15), ("The tanners' court", -106, 160, 15), ("The deepest court", -146, 198, 16)]
for name, x, z, r in COURTS:
    keep(x, z, r + 2)
    prop("cobbles", name, x, z, 0, (r * 2, r * 2))
drains = [(-150, 117, 0), (-163, 134, 90), (-106, 174, 180), (-93, 160, 270), (-146, 213, 180), (-161, 202, 90), (-131, 188, 270)]
for i, (x, z, rot) in enumerate(drains):
    prop("drain_mouth", "Drain mouth", x, z, rot, (3 if i % 3 else 3.6, 0), solid=(7, 2.4), check=False)
prop("lane_lamp", None, -140, 124, 0); prop("lane_lamp", None, -116, 154, 0); prop("lane_lamp", None, -136, 206, 0)

# ---- The Rust Market (Sector 4, east, under the Forge District's wall; CANON, book1 ch.3:3-9) ----
prop("market_hall", "The long stalls", 104, 22, 180, (27, 6), 0, solid=(27.4, 6.4))
prop("market_hall", "The rag stalls", 143, 70, 270, (24, 6), 2, solid=(24.4, 6.4))
prop("market_hall", "The skewer stalls", 58, 54, 0, (21, 6), 1, solid=(21.4, 6.4))
prop("forge", "The Rust Market hearth", 72, 24, 90, solid=(5.8, 4.8))
keep(78, 24, 6)
prop("cobbles", "The Rust Market floor", 114, 70, 0, (52, 40))
keep(114, 70, 20)
for i, (x, z, rot) in enumerate([(102, 60, 90), (126, 60, 270), (112, 84, 0), (100, 76, 90)]):
    prop("stall", ["Penny's stall", "Salt and sundries", "Skewers", "Aether wine"][i], x, z, rot, variant=i % 3, solid=(3, 2.4))
prop("well", "Market well", 116, 70, 0, 0, solid=(2.4, 2.4))
prop("tenement", "The Blind Seer's Antiquities", 118, 101, 0, (12, 8), 2, solid=(12, 9.6), style="shop")
prop("warehouse", "The Iron Teeth's warehouse", 182, 96, 270, (26, 14), 1, solid=(26.4, 14.4))
keep(170, 80, 8)
for x, z, v in [(106, 66, 2), (122, 78, 20), (64, 34, 11), (132, 62, 21), (96, 86, 27)]:
    prop("market", None, x, z, (x * 7) % 360, variant=v)
prop("barrels", None, 136, 52, 0, variant=2); prop("crates", None, 72, 36, 30, variant=1)

# ---- The Shard-Burners' Yard (south-east; the dead-house, GAME-ONLY reading) and its cellar ----
prop("dead_house", "The dead-house", 151, -112, 270, (16, 11), solid=(16.6, 11.6))
keep(138, -112, 8)
prop("cobbles", "The burners' yard", 138, -112, 90, (22, 12))
for x, z in [(139, -122), (139, -102)]:
    prop("shard_rack", "The burners' racks", x, z, 90, (3.2, 0), solid=(3.4, .8), interact="Buy back a shard", item="item.bought_shard")
prop("brazier", None, 136, -112, 0)
CELLAR = (154, -150)
keep(CELLAR[0], CELLAR[1], 11)
for x, z, rot, L in [(154, -161, 0, 16), (165, -150, 90, 18)]:
    prop("lane_wall", "Cellar wall", x, z, rot, (L, 2.4), solid=(L, .6), check=False)

# ---- The Outfall's mouth (south-east corner): the Sludge-Tunnels' door, not built: a grating and a sign ----
prop("drain_mouth", "The Outfall", 200.6, -205, 90, (3.6, 0), 1, solid=(4, 13), check=False)
prop("signpost", "The Outfall (dangerous, 18-20)", 189, -196, 250)
keep(186, -205, 7)
# The Iron Teeth's smuggling yard by the Outfall (CANON faction, book1 ch.7:21; the camp GAME-ONLY)
for x, z, rot, L in [(165, -164, 0, 16), (157, -172, 90, 16)]:
    prop("lane_wall", "Smugglers' yard wall", x, z, rot, (L, 2.6), 1 if rot == 0 else 0, solid=(L, .6), check=False)
keep(172, -182, 10)
prop("crates", None, 178, -174, 20, variant=2); prop("barrels", None, 168, -190, 0, variant=2); prop("brazier", None, 174, -184, 0)

# ---- The Forge District's gate (east wall; shut until the Forge District opens, 19-22) ----
prop("signpost", "The Forge District (19-22)", 192, 8, 270)
prop("lane_lamp", None, 196, -6, 0, variant=1)

# ---- Washing lines and lamps on the main ways, puddles here and there ----
def along(pts, step, start=0):
    out = []; acc = -start
    for i in range(len(pts) - 1):
        (x0, z0), (x1, z1) = pts[i], pts[i + 1]; L = math.hypot(x1 - x0, z1 - z0); t = 0
        while acc + (L - t) >= step:
            t += step - acc; acc = 0
            if t > L: break
            out.append((x0 + (x1 - x0) * t / L, z0 + (z1 - z0) * t / L, (x1 - x0) / L, (z1 - z0) / L))
        acc += L - t if t <= L else 0
    return out
rng = random.Random(1719)
road = {n: (w, p) for n, w, p in ROADS}
for name in ("Rows Lane", "Cinder Row", "Pell Row", "North Rows", "Court Lane"):
    w, pts = road[name]
    for k, (x, z, dx, dz) in enumerate(along(pts, 30, 14)):
        if near_water((x, z), 4): continue
        prop("washing_line", None, x, z, math.degrees(math.atan2(-dx, -dz)) % 360, (w + 6, 6.4 + (k % 3) * .6))   # its line across the lane
for name in ("Gate Street", "Rows Lane", "Cinder Row", "Market Lane", "Drain Lane"):
    w, pts = road[name]
    for (x, z, dx, dz) in along(pts, 37, 5):
        if not near_water((x, z), 2): prop("puddle", None, x + dz * w * .2, z - dx * w * .2, 0, (1 + rng.random(), 0))

exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'part_people.py'), encoding='utf-8').read())   # people, camps and hidden finds (and the ground they keep clear)

# The ruined terraces (slag-iron and cinder-beam are got there) and every node's spot are kept clear of the infill.
RUINS = [(-170, -190, 0), (-60, -200, 20), (60, -150, 0), (90, -190, 0), (-170, 90, 90), (40, 160, 0), (150, 190, 0)]
SLAG = [(-166, -186), (-55, -196), (64, -146), (86, -186), (-166, 94), (44, 164), (146, 192), (-30, -226), (186, -150), (60, -40)]
BEAM = [(-176, -194), (-64, -205), (56, -155), (94, -194), (-174, 86), (36, 156), (155, 187), (-150, -225)]
LICH = [(-156, 140), (-114, 168), (-140, 208), (-98, 152), (-158, 122)]
SOOT = [(-188, -60), (-120, -200), (30, -100), (160, -60), (-30, 200)]
for x, z, r in RUINS: keep(x, z, 9)
for x, z in SLAG + BEAM + LICH + SOOT: keep(x, z, 3)

# ======================================================================================================================================
# The infill: terraces along the lanes, both sides, wherever they fit (the Tenement Rows, the Courts, the market's back streets)
# ======================================================================================================================================
TOWN = [n for n, _, _ in ROADS if n not in ("The Toll Road",)]
infill = random.Random(4242); count = 0
def clear_of_keeps(poly, x, z, w):
    for cx, cz, r in CLEAR:
        if math.hypot(cx - x, cz - z) > r + w: continue
        if inside((cx, cz), poly) or min(seg_dist((cx, cz), poly[i], poly[(i + 1) % 4]) for i in range(4)) < r: return False
    return True
for name in TOWN:
    w, pts = road[name]
    for side in (-1, 1):
        for (x, z, dx, dz) in along(pts, 4, 2):
            tw = 12 + infill.random() * 8; d = 8 + (infill.random() < .3) * 2
            nx, nz = -dz * side, dx * side
            off = w / 2 + 1.9 + d / 2; cx, cz = x + nx * off, z + nz * off   # the stoops' front 1.1 m clear of the road's edge
            if cx < -194 or cx > 200: continue
            rot = facing((-nx, -nz)); poly = obb(cx, cz, tw, d + 2.2, rot)
            if solid_clear(poly, gap=1.8) or not clear_of_keeps(poly, cx, cz, tw): continue   # at least 1.8 m between terraces: a way through to the yards behind (the navmesh needs 1.2)
            v = infill.randrange(12)
            p = prop("tenement", None, cx, cz, rot, (round(tw, 1), d), v, solid=(tw, d + 2.2), check=False)
            count += 1
TENEMENTS = count

# Lamps on the main ways, on the verge clear of the stoops (after the infill, so none stands in a doorway).
for name in ("Gate Street", "High Lane", "Market Lane", "Burners' Row", "Rows Lane", "Wharf Walk", "Drain Lane"):
    w, pts = road[name]
    for k, (x, z, dx, dz) in enumerate(along(pts, 24, 10)):
        s = 1 if k % 2 else -1; lx, lz = x - dz * s * (w / 2 + .35), z + dx * s * (w / 2 + .35)
        if near_water((lx, lz), 2) or any(inside((lx, lz), grow(q, .5)) for _, q in FOOT) or abs(lx + 200) < 4 or abs(lx - 206) < 4: continue
        prop("lane_lamp", None, lx, lz, 0)
        FOOT.append(("lamp", obb(lx, lz, .5, .5, 0)))


# ======================================================================================================================================
# Ground: the burners' cellar sunk three metres; ruined terraces where slag-iron and cinder-beam are got
# ======================================================================================================================================
SHAPES = [{"name": "The burners' cellar", "center": V(*CELLAR), "radius": 8, "height": -3, "blend": 7}]
for i, (x, z, r) in enumerate(RUINS):
    if any(inside((x, z), q) for _, q in FOOT): problem('ruin %d at (%s, %s) is in a building' % (i, x, z))
    prop("ruin", "A fallen tenement", x, z, r, (9, 0))

# ======================================================================================================================================
# Nodes: the level-20 helper's tier 6 (all skill 90): ten slag-iron heaps, eight fallen cinder-beams, ten herbs (soot-wort, lamp-lichen
# by the drains); the rare grey-bell is a herb secret in the burners' yard. No rich seams.
# ======================================================================================================================================
NODES = []
BAD = []
def spot_ok(what, x, z, roads=True):
    for n, q in FOOT:
        if inside((x, z), grow(q, 1)): BAD.append('%s at (%s, %s) is in %s' % (what, x, z, n)); return False
    if roads:
        for n, w, pts in ROADS:
            if path_dist((x, z), pts) < w / 2 + .5: BAD.append('%s at (%s, %s) is in %s' % (what, x, z, n)); return False
    if near_water((x, z), 1): BAD.append('%s at (%s, %s) is in the water' % (what, x, z)); return False
    return True
def node(kind, x, z, rot=0):
    spot_ok(kind, x, z)
    NODES.append({"node": kind, "at": V(x, z), "rotation": rot})
for i, (x, z) in enumerate(SLAG):
    node("node.slagiron", x, z, (i * 47) % 360)
for i, (x, z) in enumerate(BEAM):
    node("node.cinderbeam", x, z, (i * 61) % 360)
for i, (x, z) in enumerate(LICH):
    node("node.lamp_lichen", x, z, 0)
for i, (x, z) in enumerate(SOOT):
    node("node.sootwort", x, z, 0)

# ======================================================================================================================================
# Stations of their own (the forge is the market hearth's, the fire the Spoon's hearth and kitchen's): the dyer's vats (a bench, for
# the Weavers) and Brother Aldo's soup kettle (a cookfire).
# ======================================================================================================================================
STATIONS = [
    {"kind": "bench", "name": "Bench: the dyer's vats", "at": V(-120, -116.5), "rotation": 0, "canonStatus": "GAME-ONLY"},
    {"kind": "fire", "name": "Brother Aldo's soup kettle", "at": V(-49, -136), "rotation": 200, "canonStatus": "GAME-ONLY"},
]
for r in RESIDENTS:
    if "at" in r: spot_ok("resident " + r["name"], r["at"]["x"], r["at"]["y"], roads=False)
for c in CAMPS: spot_ok("camp " + c["name"], c["center"]["x"], c["center"]["y"], roads=False)
for s in SECRETS: spot_ok("secret " + s["id"], s["at"]["x"], s["at"]["y"], roads=False)
if BAD: problem('\n  '.join(BAD))
print('laid out:', len(PROPS), 'props,', TENEMENTS, 'terraces of infill,', len(NODES), 'nodes')

exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'part_zone.py'), encoding='utf-8').read())
exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'part_quests.py'), encoding='utf-8').read())
exec(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'part_lore.py'), encoding='utf-8').read())

# ---- cross-checks: every objective's interactable, document and item is real ----
docs = {d["id"] for d in DOCUMENTS}; qitems = {i["id"] for i in ITEMS}
for q in QUESTS + BOUNTIES:
    for st in q["steps"]:
        for o in st["objectives"]:
            if o.get("document") and o["document"] not in docs: problem(q["id"] + ": unknown page " + o["document"])
            if o["type"] in ("collect", "deliver") and o.get("item") not in qitems: problem(q["id"] + ": unknown quest item " + str(o.get("item")))
            if o["type"] == "collect" and not o["target"].startswith("kill:") and o["target"] not in ZONE_NAMES: problem(q["id"] + ": nothing named " + o["target"])
            if o["type"] in ("talk", "deliver") and q["id"] != "main.lowtowns.0" and o["target"] not in names: problem(q["id"] + ": nobody named " + o["target"])
            if o["type"] == "kill" and not any(o["target"].split(".")[1] == c["tag"] for c in CAMPS) and ".courier." not in o["target"]: problem(q["id"] + ": no camp tagged " + o["target"])
    for who in (q["giver"], q["turnIn"]):
        if who not in names and who not in ("board", "Corwin Ashby"): problem(q["id"] + ": nobody named " + who)
lore_docs = {d["id"] for d in DOCUMENTS if d.get("kind") == "lore"}
held = {s.get("document") for s in SECRETS}
if lore_docs - held: problem("lore pages at no hidden find: " + str(lore_docs - held))
for e in LORE_ENTRIES:
    if e.get("page") and e["page"] not in docs: problem(e["id"] + ": unknown page")
    if e.get("secret") and e["secret"] not in {s["id"] for s in SECRETS}: problem(e["id"] + ": unknown secret")
    if e["canonStatus"].startswith("CANON") and not e["canonStatus"].startswith("CANON-EXPANDED"):
        import re
        if not re.search(r"book\s*\d+\s*ch\.?\s*\d+", e["canonStatus"], re.I): problem(e["id"] + ": CANON without book and chapter")
for s in SECRETS:
    for e in LORE_ENTRIES:
        for t in (e.get("line"), e.get("more")):
            if t and s["name"].lower() in t.lower() and not (e.get("secret") == s["id"] or (e.get("page") and e.get("page") == s.get("document"))): problem(e["id"] + " names the find " + s["name"])

# ---- write ----
def dump(obj, rel):
    path = os.path.join(ROOT, *rel.split('/'))
    text = json.dumps(obj, indent=2, ensure_ascii=False).replace('\n', '\r\n') + '\r\n'
    open(path, 'wb').write(text.encode('utf-8')); print('wrote', rel, len(text), 'bytes')
dump(ZONE, 'Zones/lowtowns.json'); dump(QUEST_FILE, 'Quests/lowtowns.json'); dump(LORE_FILE, 'Lore/lowtowns.json'); dump(ITEM_FILE, 'Items/lowtowns.json')
