"""Hallow's Creek, the second dungeon (Docs/DUNGEON2_DESIGN.md; Chris, 2026-10-16: "after this work on next dungeon and raise top level to
20. make sure there is a zone for 15-20 and a dungeon for the same range", answered "Lowtowns + Hallow's Creek (Recommended)"): the zone's
layout, made here and written to New Unity Project/Assets/Crulanda/EncounterContent/Zones/hallows.json.

A frozen town walked at street level (a built place, not a cave web: the Adit's lesson), on open ground: the Ash Road, the broken bridge's
slabs over the chasm, Main Street and the Square round the crater, the Granary, the Old Archive and the Bell Tower; the Cellars and the
overhang are the only caves (cavern props, traced as Crulanda.World.Hollow traces them: adit_layout.Cave). Everything on open ground is in
world metres (x east, z north) with heights over the yard (the land at the zone's start): decks (slabs, lofts, landings, ramps), gates,
anchors, one drop, the chasm (a pit) and the hungry shadows (hazard strips) are the data ZoneBuilder.HallowsGround reads (N1-N4); the town's
buildings and the paused townsfolk are prop kinds ZoneBuilder.Hallows builds.

    python hallows_layout.py           checks the layout and writes Zones/hallows.json

Checks (it stops on a failure): every ramp no steeper than 30 degrees; every camp and anchor on the land, a deck or a cave floor; levels
15-20; the wings, gates, events and camps resolve.

Labels: DUNGEON2_DESIGN.md section 7. CANON (book1 ch.15-21, the world bible): Hallow's Creek, the paused town, the bridge's hanging slabs,
the crater and the red door, the Old Archive, the Hollow, the Bureau, the Iron Pact, the Preservationists. GAME-ONLY: every person, camp,
item, the Bureau's quartz-cutting, the wings, the Waking Hour as a worked thing. No machines (DUNGEON_DESIGN 0.05): the clock is a painted
dial, nothing turns. No World of Warcraft names or layouts: lessons only.
"""
import io, json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import adit_layout as AL   # the cave trace (Cave, as Hollow makes it)

ROOT = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
ZONES = os.path.join(ROOT, 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent', 'Zones')
SIZE = 800
RESPAWN = 7200            # no respawns while inside (decision 11): a visit is shorter than two hours
MAX_RAMP = 30.0           # degrees
G = 'GAME-ONLY'

# ---------------------------------------------------------------- caves (the Cellars, the well-house stair, the dark cellar, the overhang)
CAVES = [
    # The well-house stair: from the well-house on the Square's west rim down to the cellars (where the Cellars come out; one way in the run).
    AL.Cave('WS', "The well-house stair", 'cellar', [
        (-32, 47, 2.2, 3.2, None), (-33, 42, 2.4, 3.3, -2), (-35, 37, 3, 3.4, -4.5), (-37, 32, 3.5, 3.5, -6.5), (-38, 27, 3.5, 3.5, -6.8)],
        canon='GAME-ONLY (the cellars of the paused town)'),
    # The Cellars: from the stair in the bakery's back yard, north under the bank behind the west houses to the well-house stair.
    AL.Cave('CE', "The Cellars", 'cellar', [
        (-30, -77, 2.2, 3.2, None), (-30, -71, 2.4, 3.3, -1.5), (-33, -63, 2.6, 3.4, -3.5), (-37, -54, 3, 3.5, -5.2), (-40, -42, 3.5, 3.5, -6),
        (-41, -28, 6, 4.2, -6.4), (-41, -18, 8, 4.5, -6.5), (-41, -8, 5, 4, -6.5), (-41, 2, 3.5, 3.5, -6.7), (-40, 12, 3.5, 3.5, -6.8),
        (-39, 21, 3.5, 3.5, -6.8), (-38.4, 29, 3.5, 3.5, -6.8)],
        joins='WS', canon='GAME-ONLY (the cellars of the paused town, the Cut-Store)'),
    # The dark cellar: one cellar with no lamp, off the Cellars' west side (behind a door you find by feel).
    AL.Cave('DK', "The dark cellar", 'darkcellar', [
        (-43.4, 6, 1.6, 2.6, None), (-47, 6.5, 1.8, 2.8, -6.7), (-51, 7, 4.5, 3.4, -6.7), (-57, 7.5, 5, 3.5, -6.7), (-61, 8, 4, 3.3, -6.7)],
        within='CE', canon='GAME-ONLY'),
    # The overhang off the Ash Road's east side (a secret).
    AL.Cave('OV', "The overhang", 'overhang', [
        (9.5, -245, 2.6, 3, None), (15, -245, 3, 3.2, -1), (21, -246, 4.2, 3.6, -2.4), (27, -246.5, 4.5, 3.6, -3)],
        canon='GAME-ONLY'),
]
CAVE = {c.key: c for c in CAVES}

def build_caves():
    AL.Cave.ALL[:] = []
    for c in CAVES:
        c.build(CAVE); AL.Cave.ALL.append(c)
    for c in CAVES:
        if c.joins:
            c.join = CAVE[c.joins]; e = c.rings[-1]
            ty = c.join.floor_smooth(e[0], e[1])
            if ty is None: sys.exit('%s joins %s, but its end %s is not over its floor' % (c.name, c.join.name, (round(e[0], 1), round(e[1], 1))))
            c.join_rise = e[4] - ty
    for c in CAVES: c.floored = [not AL.floor_gives_way(c, r[0], r[4] + .3, r[1]) for r in c.rings]

def CA(key, x, z):
    a, s = CAVE[key].local(x, z); return round(a, 2), round(s, 2)

# ---------------------------------------------------------------- the open ground: decks (N1, N3)
DECKS = []
def deck(name, at, at2=None, rise=0.0, rise2=None, width=3.0, style='stone', radius=0.0, solid=False, rails='none', canon=G):
    d = {'name': name, 'style': style, 'rails': rails, 'at': {'x': round(at[0], 2), 'y': round(at[1], 2)},
         'at2': {'x': round((at2 or at)[0], 2), 'y': round((at2 or at)[1], 2)}, 'rise': round(rise, 2), 'rise2': round(rise if rise2 is None else rise2, 2),
         'width': width, 'canonStatus': canon}
    if radius: d['radius'] = radius
    if solid: d['solid'] = True
    DECKS.append(d); return d

def slope(d):
    a, b = d['at'], d['at2']; run = math.hypot(b['x'] - a['x'], b['y'] - a['y'])
    return 0 if run < .01 else math.degrees(math.atan2(abs(d['rise2'] - d['rise']), run))

def ramp(name, a, ha, b, hb, width=3.0, style='stone'):
    """A ramp from a (height ha) to b (height hb), each end run 0.4 m into what it joins."""
    dx, dz = b[0] - a[0], b[1] - a[1]; m = math.hypot(dx, dz) or 1; ux, uz = dx / m, dz / m
    return deck(name, (a[0] - ux * .4, a[1] - uz * .4), (b[0] + ux * .4, b[1] + uz * .4), ha, hb, width, style)

# The Bridge of Shattered Glass (B): six slabs stepping down and up again across the chasm, two of them the Tables (12 x 9).
LIP_S = -211.0            # the Ash Road's lip, where the chasm opens
SLABS = [  # name, x, depth along z, width across, height
    ('The first slab', 0, 6, 8, -1.5), ('The near Table', 6, 9, 12, -4), ('The third slab', -4, 5, 6, -6),
    ('The far Table', 2, 9, 12, -7), ('The fifth slab', 8, 6, 6, -4.5), ('The last slab', 2, 6, 8, -2.5)]
SLAB = {}
def make_bridge():
    z = LIP_S; x0, h0 = 0.0, 0.0; prev = None
    for i, (name, x, depth, width, h) in enumerate(SLABS):
        run = max(3.0, abs(h - h0) / math.tan(math.radians(23)))
        z1 = z + run; z2 = z1 + depth
        ramp('Ramp to ' + name.lower(), (x0, z), h0, (x, z1), h, 3)
        deck(name, (x, z1), (x, z2), h, h, width, 'stone')
        SLAB[name] = (x, (z1 + z2) / 2, h, width, depth)
        z, x0, h0 = z2, x, h
    run = max(3.0, abs(h0) / math.tan(math.radians(23)))
    ramp('Ramp to the far lip', (x0, z), h0, (0, z + run), 0, 3)
    return z + run      # the far lip

# The lower orbit (B2): a drop off the third slab's west edge, two slabs below, a stair of ramps and two small slabs back up to the fifth.
def make_orbit():
    x3, z3, h3, w3, d3 = SLAB['The third slab']
    l1 = (x3 - 12, z3 + 1); deck('The low slab', (l1[0], l1[1] - 3), (l1[0], l1[1] + 3), -12, -12, 6)
    l2 = (x3 - 18, z3 + 10); deck("The Mason's slab", (l2[0], l2[1] - 5), (l2[0], l2[1] + 5), -13, -13, 10)
    ramp('The low ramp', (l1[0] + 1, l1[1] + 3), -12, (l2[0] + 2, l2[1] - 5), -13, 3)
    x5, z5, h5, w5, d5 = SLAB['The fifth slab']
    s1 = (l2[0] + 6, l2[1] + 12); deck('The first step-stone', (s1[0], s1[1] - 1.5), (s1[0], s1[1] + 1.5), -10.2, -10.2, 4)
    ramp('The first stair', (l2[0] + 3, l2[1] + 5), -13, (s1[0], s1[1] - 1.5), -10.2, 2.6)
    s2 = (s1[0] + 9, s1[1] + 2); deck('The second step-stone', (s2[0] - 1.5, s2[1]), (s2[0] + 1.5, s2[1]), -7.4, -7.4, 4)
    ramp('The second stair', (s1[0] + 2, s1[1]), -10.2, (s2[0] - 1.5, s2[1]), -7.4, 2.6)
    ramp('The third stair', (s2[0] + 1.5, s2[1]), -7.4, (x5 - w5 / 2, z5), h5, 2.6)
    DROPS.append({'name': 'Off the third slab', 'fromAt': {'x': round(x3 - w3 / 2 + .8, 2), 'y': round(z3, 2)}, 'fromRise': h3,
                  'toAt': {'x': round(l1[0] + 1, 2), 'y': round(l1[1], 2)}, 'toRise': -12, 'width': 2.5})
    return l1, l2

DROPS = []

# ---------------------------------------------------------------- the built places (their walls are props; their floors are decks)
GH = {}   # the gatehouse
def make_gatehouse(far_lip):
    cz = far_lip + 7 + 1   # its south wall a metre past the lip
    GH.update(at=(0, cz), w=16, d=14)
    s = cz - 7   # south wall line
    # The wall-walk at +5 along the south wall (over the bridge), and its stair up the east wall inside.
    deck('The wall-walk', (-6.5, s + 1.4), (6.5, s + 1.4), 5, 5, 2.4, 'stone')
    ramp('The wall-walk stair', (6.3, s + 12.6), 0, (6.3, s + 2.6), 5, 2.2)
    return cz

TOWER = (0, 118)
def make_tower():
    cx, cz = TOWER; o = 3.9; corners = [(o, -o), (o, o), (-o, o), (-o, -o)]   # SE, NE, NW, SW (x, z offsets)
    h = 0.0; names = {2: 'The first landing', 4: 'The second landing', 6: 'The third landing'}
    for k in range(8):
        a = corners[k % 4]; b = corners[(k + 1) % 4]; h2 = h + 2.25
        # each flight from a corner to the next, its ends in the corner squares
        ax, az = cx + a[0], cz + a[1]; bx, bz = cx + b[0], cz + b[1]
        dx, dz = (bx - ax), (bz - az); m = math.hypot(dx, dz); ux, uz = dx / m, dz / m
        deck('Tower flight %d' % (k + 1), (ax + ux * 1.1, az + uz * 1.1), (bx - ux * 1.1, bz - uz * 1.1), h, h2, 2.2, 'stone')
        if k < 7: deck(names.get(k + 1, 'Tower corner %d' % (k + 1)), (bx - ux * 1.1, bz - uz * 1.1), (bx + ux * 1.1, bz + uz * 1.1), h2, h2, 2.2, 'stone')
        h = h2
    # The belfry floor at +18: the tower less the west strip (over the seventh flight) and the south strip (the eighth comes up there).
    deck('The belfry floor', (cx + 1.1, cz - 2.8), (cx + 1.1, cz + 5), 18, 18, 7.8, 'timber')
    deck('The belfry stair-head', (cx + o, cz - o - 1.1), (cx + o, cz - o + 1.1), 18, 18, 2.2, 'timber')

ARCHIVE = (72, 70)
def make_archive():
    x, z = ARCHIVE
    deck('The Archive floor', (x - 24, z), (x + 24, z), 2, 2, 23, 'stone', solid=True)
    deck('The copying cells', (x - 13, z + 14.5), (x + 13, z + 14.5), 2, 2, 5, 'stone', solid=True)
    ramp('The Archive steps', (x - 33, z), 0, (x - 24, z), 2, 7)
    deck('The north gallery', (x - 22, z + 10.2), (x + 22.5, z + 10.2), 5, 5, 2.8, 'timber')
    deck('The south gallery', (x - 22, z - 10.2), (x + 22.5, z - 10.2), 5, 5, 2.8, 'timber')
    deck('The apse gallery', (x + 22.4, z - 10.2), (x + 22.4, z + 10.2), 5, 5, 2.8, 'timber')
    ramp('The apse stair (south)', (x + 18, z - 3.5), 2, (x + 18, z - 9.2), 5, 2.2, 'timber')
    ramp('The apse stair (north)', (x + 18, z + 3.5), 2, (x + 18, z + 9.2), 5, 2.2, 'timber')

GRANARY = (95, -70)
def make_granary():
    x, z = GRANARY
    deck('The north loft', (x - 18, z + 6.6), (x + 18.5, z + 6.6), 4, 4, 3.2, 'timber')
    deck('The south loft', (x - 18, z - 6.6), (x + 18.5, z - 6.6), 8, 8, 3.2, 'timber')
    ramp('The loft stair', (x - 17, z - 4.5), 0, (x - 17, z + 5), 4, 2.4, 'timber')
    ramp('The high stair', (x + 17.6, z + 5), 4, (x + 17.6, z - 5), 8, 2.4, 'timber')

# ---------------------------------------------------------------- camps
CAMPS = []
def camp(name, mob, tag, look, count, lo, hi, at, wing, step=0, radius=3.0, rise=0.0, elite=False, gate=None, after=None, optional=False,
         guards=None, social=None, rare=0.0, passive=False, route=None, canon=None, cave=None, flee=0.0):
    c = {'name': name, 'mob': mob, 'tag': tag, 'look': look, 'canonStatus': canon or (G + ' (the Bureau and the Iron Pact are CANON, book1 ch.2-3)'),
         'radius': radius, 'count': count, 'levelMin': lo, 'levelMax': hi, 'respawn': RESPAWN, 'center': {'x': round(at[0], 2), 'y': round(at[1], 2)},
         'wing': wing}
    if step: c['step'] = step
    if elite: c['elite'] = True
    if guards: c['guards'] = guards
    if social: c['social'] = social
    if rare: c['rare'] = rare
    if passive: c['passive'] = True
    if optional: c['optional'] = True
    if after: c['after'] = after
    if gate: c['gate'] = gate
    if rise: c['rise'] = round(rise, 2)
    if flee: c['flee'] = flee
    if route: c['route'] = [{'x': round(p[0], 2), 'y': round(p[1], 2)} for p in route]
    if cave:
        a, s = CA(cave, *at); c['cave'] = CAVE[cave].name; c['along'] = a; c['aside'] = s
    if wing == 'granary': c['optional'] = True   # (the standard run takes its elites, the full run all of it, the quick run none)
    if wing == 'cellars': c.pop('step', None)       # (an optional wing's camps are the full run's side fights, fitted in where the street passes the bakery)
    CAMPS.append(c); return c

PICKER, PACT, BOW, CUTTER, CLERK, TEAM, HOLLOW, RINGER = ('Bureau grey-picker', 'greypicker', 'collector'), ('Iron Pact soldier', 'pactsoldier', 'outrider'), \
    ('Iron Pact crossbowman', 'pactbow', 'outrider'), ('Bureau cutter', 'bureaucutter', 'collector'), ('Bureau clerk', 'bureauclerk', 'collector'), \
    ('Bureau teamster', 'teamster', 'collector'), ('Hallow\'s Creek Hollow', 'creekhollow', 'hollow'), ('Paused ringer', 'ringer', 'pale')
HC = 'CANON creature (the Hollow, book1 ch.4); GAME-ONLY camp and its origin (the cut paused)'
PC = 'GAME-ONLY camp; CANON townsfolk paused (book1 ch.17-21)'

def make_camps(gate_z, l2):
    x2, z2, h2, *_ = SLAB['The near Table']; x4, z4, h4, *_ = SLAB['The far Table']; x1, z1, h1, *_ = SLAB['The first slab']
    gx, gz = GH['at']
    # ---- the Ash Road (15-16)
    camp('Pickers at the milestone', *PICKER, 3, 15, 15, (2, -346), 'road', 1)
    camp('The road picket', *PACT, 3, 15, 16, (-2, -318), 'road', 2)
    camp("Hurst's carters", *TEAM, 3, 16, 16, (-36, -284), 'road', 3, radius=3)
    camp('Road-Captain Everard Hurst', 'Road-Captain Everard Hurst', 'roadcaptain', 'outrider', 1, 16, 16, (-46, -289), 'road', 3, radius=1.5, elite=True, guards="Hurst's carters")
    camp('Pickers by the overhang', *PICKER, 3, 16, 16, (-3, -252), 'road', 4)
    camp('The picket at the lip', *PACT, 3, 16, 16, (1, -222), 'road', 5)
    camp('The road patrol', *PACT, 2, 16, 16, (0, -340), 'road', 0, radius=2, route=[(0, -340), (0, -300), (0, -262), (0, -232)])
    # ---- the Bridge of Shattered Glass (16-17)
    camp('The span watch', *BOW, 2, 16, 16, (x1, z1), 'bridge', 1, radius=1.5, rise=h1)
    camp('The near Table', *PACT, 4, 16, 17, (x2, z2), 'bridge', 2, radius=3, rise=h2)
    camp('The far Table', *PICKER, 3, 17, 17, (x4, z4), 'bridge', 3, radius=3, rise=h4)
    camp("Tredway's shield-line", *PACT, 3, 17, 17, (gx + 1, gz - 2), 'bridge', 4, radius=2.5)
    camp('Sergeant Wymond Tredway', 'Sergeant Wymond Tredway', 'spansergeant', 'outrider', 1, 17, 17, (gx - 2, gz - 1), 'bridge', 4, radius=1.2, elite=True, guards="Tredway's shield-line")
    camp('The wall-walk bows', *BOW, 2, 17, 17, (0, gz - 7 + 1.4), 'bridge', 4, radius=2, rise=5)
    camp('The Fallen Mason', 'The Fallen Mason', 'fallenmason', 'hollow', 1, 17, 17, l2, 'bridge', 0, radius=1.5, rise=-13, elite=True, guards='none',
         social='solitary', optional=True, canon='GAME-ONLY (the Hollow are CANON, book1 ch.4)')
    # ---- Main Street and the Square (the hub; 17-18)
    camp('Cutters at the first doorstep', *CUTTER, 3, 17, 17, (-6, -113), 'street', 1, gate="Tredway's gate")
    camp('Cutters at the bakery step', *CUTTER, 3, 17, 17, (-6, -79), 'street', 2, gate="Tredway's gate")
    camp("Upcott's chalk-men", *CLERK, 2, 17, 18, (12, -40), 'street', 3, radius=2.5, gate="Tredway's gate")
    camp('Assessor Piers Upcott', 'Assessor Piers Upcott', 'assessor', 'collector', 1, 18, 18, (19, -45), 'street', 3, radius=1.2, elite=True, guards="Upcott's chalk-men", gate="Tredway's gate")
    camp('Cutters at the last doorstep', *CUTTER, 3, 18, 18, (6, -8), 'street', 4, gate="Tredway's gate")
    camp('The ford watch', *PACT, 3, 18, 18, (-3, 14), 'street', 5, gate="Tredway's gate")
    camp('The rim guard', *PACT, 3, 18, 18, (-20, 42), 'street', 6, gate="Tredway's gate")
    camp('The street patrol', *PACT, 2, 17, 17, (0, -118), 'street', 0, radius=2, route=[(0, -118), (0, -80), (0, -40), (0, 0)], gate="Tredway's gate")
    # ---- the Granary on the race (optional; 17-18)
    x, z = GRANARY
    camp('The footbridge watch', *PACT, 2, 17, 17, (60, -62), 'granary', 1, radius=2)
    camp('The sorting floor', *PICKER, 4, 17, 17, (x - 2, z), 'granary', 2, radius=3.5)
    camp("The Picker-Master's sorters", *PICKER, 2, 18, 18, (x - 4, z + 6.6), 'granary', 3, radius=2, rise=4)
    camp('Picker-Master Fulke Yelland', 'Picker-Master Fulke Yelland', 'pickermaster', 'collector', 1, 18, 18, (x + 3, z + 6.6), 'granary', 3, radius=1.2, rise=4,
         elite=True, guards="The Picker-Master's sorters")
    camp("Coldwell's netters", *BOW, 2, 18, 18, (x - 2, z - 6.6), 'granary', 4, radius=2, rise=8)
    camp('Gil Coldwell, the Loft Watch', 'Gil Coldwell, the Loft Watch', 'loftwatch', 'outrider', 1, 18, 18, (x + 6, z - 6.6), 'granary', 4, radius=1.2, rise=8,
         elite=True, guards="Coldwell's netters")
    camp('The yard teamsters', *TEAM, 3, 18, 18, (x, z - 30), 'granary', 5, radius=3)
    # ---- the Cellars (optional, full runs; 18-19)
    camp('Hollow in the cellar stair', *HOLLOW, 3, 18, 18, (-36, -56), 'cellars', 1, radius=2, cave='CE', canon=HC)
    camp('Hollow in the long cellar', *HOLLOW, 3, 18, 18, (-41, -36), 'cellars', 2, radius=2, cave='CE', canon=HC)
    camp("First-Cut's kin", *HOLLOW, 3, 19, 19, (-38, -16), 'cellars', 3, radius=2.5, cave='CE', canon=HC)
    camp('First-Cut', 'First-Cut', 'firstcut', 'hollow', 1, 19, 19, (-43, -20), 'cellars', 3, radius=1.2, elite=True, guards="First-Cut's kin", cave='CE', canon=HC)
    camp('Hollow at the far stair', *HOLLOW, 3, 19, 19, (-40, 15), 'cellars', 4, radius=2, cave='CE', canon=HC)
    camp('The Long Shadow', 'The Long Shadow', 'longshadow', 'pale', 1, 19, 19, (-55, 7.5), 'cellars', 0, radius=1.2, elite=True, guards='none', social='solitary',
         optional=True, gate='The dark cellar door', cave='DK', canon='GAME-ONLY (the hungry shadows are CANON, book1 ch.17)')
    # ---- the Old Archive (18-19)
    ax, az = ARCHIVE
    camp('Clerks in the reading hall', *CLERK, 3, 18, 18, (ax - 18, az), 'archive', 1, radius=2.5, rise=2)
    camp('Clerks in the stacks', *CLERK, 3, 18, 19, (ax - 6, az - 6), 'archive', 2, radius=2.5, rise=2)
    camp('Clerks at the lecterns', *CLERK, 3, 19, 19, (ax + 5, az + 5), 'archive', 3, radius=2.5, rise=2)
    camp("Pardew's copyists", *CLERK, 2, 19, 19, (ax + 12, az), 'archive', 4, radius=2, rise=2)
    camp('Registrar Dunstan Pardew', 'Registrar Dunstan Pardew', 'registrar', 'collector', 1, 19, 19, (ax + 16, az + 1), 'archive', 4, radius=1.2, rise=2,
         elite=True, guards="Pardew's copyists")
    camp('Clerks from the cells', *CLERK, 2, 19, 19, (ax + 8, az + 14.5), 'archive', 0, radius=1.5, rise=2, after='loose:Registrar Dunstan Pardew')
    camp('The Reader at the Desk', 'The Reader at the Desk', 'reader', 'pale', 1, 19, 19, (ax - 6, az + 7), 'archive', 0, radius=1.2, rise=2, elite=True, guards='none',
         social='solitary', optional=True, passive=True, canon='GAME-ONLY (the Archivists are CANON, book1 ch.18)')
    # ---- the Warden's walk (E2: waves while she walks, each unless its boss is down)
    camp('Pact relief on the walk', *PACT, 3, 19, 19, (ax - 40, az + 22), 'walk', 0, radius=2.5, after='event:walk1')
    camp('Hollow up out of the well', *HOLLOW, 3, 19, 19, (-24, 57), 'walk', 0, radius=2.5, after='event:walk2', canon=HC)
    # ---- the Bell Tower (19-20)
    tx, tz = TOWER
    camp('The tower door watch', *PACT, 3, 19, 19, (tx + 2, tz - 12), 'tower', 1, radius=2.5, gate='The ward-lock')
    camp("Kersey's line", *PACT, 2, 19, 19, (tx - 1.5, tz + 1.5), 'tower', 2, radius=1.5, gate='The ward-lock')
    camp('Pact-Captain Rowena Kersey', 'Pact-Captain Rowena Kersey', 'pactcaptain', 'outrider', 1, 20, 20, (tx + 1, tz - 1), 'tower', 2, radius=1, elite=True,
         guards="Kersey's line", gate='The ward-lock')
    camp("The Examiner's clerks", *CLERK, 2, 19, 19, (tx + 2.5, tz + 3), 'tower', 3, radius=1.2, rise=18, gate='The ward-lock')
    camp('Bureau Examiner Thurstan Vowell', 'Bureau Examiner Thurstan Vowell', 'examiner', 'collector', 1, 20, 20, (tx + 1, tz + 1), 'tower', 3, radius=1, rise=18,
         elite=True, guards="The Examiner's clerks", gate='The ward-lock')
    for n in range(1, 3):
        camp('Ringers on the stair (%s)' % ('first' if n == 1 else 'second'), *RINGER, 2, 19, 19, (tx, tz), 'tower', 0, radius=1.5, after='event:waking%d' % n, canon=PC)
    # ---- the bakery's Glitch (E1, optional: two waves of Hollow up the trapdoor)
    camp('Hollow up the bakery trapdoor', *HOLLOW, 2, 17, 17, (-22, -72), 'street', 0, radius=2, after='event:glitch1', canon=HC)
    camp('More Hollow up the trapdoor', *HOLLOW, 3, 17, 17, (-22, -72), 'street', 0, radius=2, after='event:glitch2', canon=HC)
    # ---- the rares (last, so a visit without them moves nobody else)
    camp('The Old Ram', 'The Old Ram', 'oldram', 'boar', 1, 16, 16, (-62, -300), 'road', 0, radius=1.5, elite=True, guards='none', social='solitary',
         rare=.25, optional=True, canon="GAME-ONLY (Bet's letter's ram, already in the game; the paused beasts are CANON-EXPANDED)")
    camp('The Grey Carter', 'The Grey Carter', 'greycarter', 'hollow', 1, 16, 16, (24, -300), 'road', 0, radius=1.5, elite=True, guards='none', social='solitary',
         rare=.2, optional=True, canon=HC)
    camp('The Lamplighter', 'The Lamplighter', 'lamplighter', 'hollow', 1, 18, 18, (-4, -100), 'street', 0, radius=1.2, elite=True, guards='none', social='solitary',
         rare=.2, optional=True, gate="Tredway's gate", route=[(-4, -100), (-4, -60), (-4, -20), (-4, -60)], canon=HC)

# ---------------------------------------------------------------- gates, anchors, events, wings
GATES, ANCHORS = [], []
def gate(title, at, kind, rotation=0.0, width=6.0, prompt=None, opensOn=None, look=None, side=None, rise=0.0, cave=None, along=None):
    g = {'title': title, 'kind': kind}
    if cave: g['cave'] = CAVE[cave].name; g['along'] = along
    else: g['at'] = {'x': round(at[0], 2), 'y': round(at[1], 2)}; g['rotation'] = rotation; g['width'] = width
    if rise: g['rise'] = rise
    if prompt: g['prompt'] = prompt
    if opensOn: g['opensOn'] = opensOn
    if look: g['look'] = look
    if side: g['side'] = side
    GATES.append(g)
def anchor(name, at, rise=0.0, cave=None):
    a = {'name': name}
    if cave: a['cave'] = CAVE[cave].name; a['along'], a['aside'] = CA(cave, *at)
    else: a['at'] = {'x': round(at[0], 2), 'y': round(at[1], 2)}
    if rise: a['rise'] = rise
    ANCHORS.append(a)

def make_works():
    gx, gz = GH['at']
    gate("Tredway's gate", (gx, gz + 7), 'bossdoor', 0, 6, opensOn='Sergeant Wymond Tredway', look='door')
    ax, az = ARCHIVE
    gate("The Warden's cell", (ax, az + 12), 'pen', 0, 2.4, prompt='Lift the bar', opensOn='Registrar Dunstan Pardew', look='stockade', rise=2)
    tx, tz = TOWER
    gate('The ward-lock', (tx, tz - 6), 'ward', 0, 3, look='door')
    x, z = GRANARY
    gate('The yard bar', (x, z - 43), 'bar', 0, 5, prompt='Lift the yard bar', look='bar', side='far')
    gate('The dark cellar door', None, 'hiddendoor', prompt='Feel along the wall', look='door', cave='DK', along=3.5)
    anchor('the milestone', (0, -366)); anchor('the gatehouse', (gx, gz)); anchor('the bakery counter', (-14, -70))
    anchor('the well-house', (-31, 51)); anchor("the Archive's steps", (ax - 30, az)); anchor("the Warden's cell", (ax, az + 14.5), 2)
    anchor('the tower door', (tx, tz - 9)); anchor('the belfry stair-head', (tx + 3.9, tz - 3.9), 18); anchor('the wagon', (x - 6, z - 30))
    anchor('the Cut-Store', (-41, -18), cave='CE')

WINGS = [
    {'id': 'road', 'name': 'The Ash Road'},
    {'id': 'bridge', 'name': 'The Bridge of Shattered Glass', 'after': 'road', 'exit': 'walk', 'home': 'the gatehouse'},
    {'id': 'street', 'name': 'Main Street', 'after': 'bridge'},
    # The Granary: a wing of the standard run (its two elites, DUNGEON2_DESIGN section 3) but not the quick one: every camp in it optional.
    {'id': 'granary', 'name': 'The Granary on the race', 'after': 'street', 'exit': 'walk', 'home': 'the wagon'},
    {'id': 'cellars', 'name': 'The Cellars', 'after': 'street', 'exit': 'walk', 'home': 'the well-house', 'optional': True},
    {'id': 'archive', 'name': 'The Old Archive', 'after': 'street', 'exit': 'walk', 'home': "the Archive's steps"},
    {'id': 'walk', 'name': "The Warden's walk", 'after': 'archive'},
    {'id': 'tower', 'name': 'The Bell Tower', 'after': 'walk'},
]
EVENTS = [
    {'id': 'glitch', 'kind': 'hold', 'wing': 'street', 'anchor': 'the bakery counter', 'step': 0, 'waves': [
        {'at': 4, 'camp': 'Hollow up the bakery trapdoor', 'text': 'The bakery\'s bell gives one note. For a breath the room is warm and smells of bread; then the trapdoor lifts and the Hollow come up out of the cellar.'},
        {'at': 30, 'camp': 'More Hollow up the trapdoor', 'text': 'The bell rings again, a minute that will not end. More of them come up the cellar stair.'}]},
    {'id': 'walk', 'kind': 'escort', 'wing': 'walk', 'anchor': "the Warden's cell", 'step': 0, 'waves': [
        {'at': 20, 'camp': 'Pact relief on the walk', 'unless': 'Picker-Master Fulke Yelland', 'text': 'Boots on the Granary road: the Pact\'s relief comes up from the yard to take the Warden back.'},
        {'at': 45, 'camp': 'Hollow up out of the well', 'unless': 'First-Cut', 'text': 'Something climbs out of the well-house: the Hollow, come up from the Cellars after the Warden.'}]},
    {'id': 'waking', 'kind': 'phase', 'wing': 'tower', 'anchor': 'the belfry stair-head', 'step': 0, 'waves': [
        {'at': 3, 'camp': 'Ringers on the stair (first)', 'text': 'Colour runs back into the town like water into a print. Below, the square moves; on the stair, two of the paused ringers wake and climb.'},
        {'at': 30, 'camp': 'Ringers on the stair (second)', 'text': 'Two more of the ringers come up the stair, their faces still stone, their hands already on the rope that is not there.'}]},
]

# ---------------------------------------------------------------- props: the town (ZoneBuilder.Hallows), the caves
PROPS = []
def prop(kind, at, rotation=0.0, name=None, size=None, variant=0, style=None, interact=None, scale=None, **kw):
    p = {'kind': kind, 'at': {'x': round(at[0], 2), 'y': round(at[1], 2)}}
    if name: p['name'] = name
    if rotation: p['rotation'] = rotation
    if size: p['size'] = {'x': size[0], 'y': size[1]}
    if variant: p['variant'] = variant
    if style: p['style'] = style
    if interact: p['interact'] = interact
    if scale: p['scale'] = scale
    p.update(kw); PROPS.append(p); return p

def make_props():
    for c in CAVES:
        p = {'kind': 'cavern', 'name': c.name, 'at': {'x': round(c.at[0], 3), 'y': round(c.at[1], 3)}, 'rotation': round(c.yaw, 3), 'variant': 2,
             'plan': [round(v, 3) for row in c.plan for v in row], 'style': c.style}
        if c.within: p['within'] = CAVE[c.within].name
        if c.joins: p['joins'] = CAVE[c.joins].name
        PROPS.append(p)
    prop('grave', (4, -368), 0, 'The far milestone', scale=1.35)
    # The Fossilised Farm (A2): the stone house, the byre, the stone dog that is never fought.
    prop('house', (-56, -280), 90, 'The fossilised farmhouse', (10, 12), 2, 'stone')
    prop('barn', (-40, -270), 0, 'The stone byre', (10, 6), style='stone')
    prop('fence', (-30, -296), 0, None, (24, 0), style='stone'); prop('fence', (-30, -274), 0, None, (10, 0), style='stone')
    prop('paused', (-44, -280), 200, 'The stone dog', variant=4)
    prop('paused', (-52, -286), 130, 'A farmer at his gate', variant=0)
    # Main Street (M): two rows of white houses, every door and window stone; picket fences; the paused on their doorsteps.
    zs = [-118, -104, -90, -76, -62, -48, -34, -20, -6, 8]
    names_w = {-118: None, -104: None, -76: "Gable's bakery", -62: None}
    k = 0
    for z in zs:
        for side in (-1, 1):
            k += 1
            if side == -1 and z == -104: continue          # Bet's yard
            if side == 1 and z == -48: continue           # the laundry yard
            name = "Gable's bakery" if (side == -1 and z == -76) else ('The Reed house' if (side == 1 and z == -90) else None)
            prop('house', (side * 14, z), 270 if side < 0 else 90, name, (9, 8), k % 4, 'stone')
            prop('fence', (side * 8.6, z - 3.2), 90, None, (3, 0), style='stone'); prop('fence', (side * 8.6, z + 3.2), 90, None, (3, 0), style='stone')
            if k % 3 != 0 or name: prop('paused', (side * 8.9, z + .4), 90 if side < 0 else 270, None, variant=(k % 3))
    prop('signpost', (-8.4, -71.5), 90, 'Gable, Baker')
    prop('paused', (-9.2, -76.5), 90, 'The baker in his doorway', variant=1)
    prop('paused', (9.2, -89.6), 270, 'Old Mother Reed', variant=2)
    prop('paused', (2.5, -60), 30, 'A man with his hat in the wind', variant=0); prop('paused', (-3, -30), 200, 'A woman calling down the street', variant=0)
    prop('hangball', (3.5, -26), 0, 'A ball in the air')
    prop('cart', (-21, -104), 30, "Bet's cart", style='stone')
    prop('fence', (-14, -110), 0, None, (10, 0), style='stone')
    prop('laundry', (17, -48), 90, 'The laundry yard')
    # The cutters at work on three of the paused (decision 5): their sacks and chisels by the figures.
    for (x, z, r) in [(-9, -113, 90), (-9, -80, 90), (9, -6, 270)]:
        prop('paused', (x, z), r, 'Marked for the cut', variant=3)
        prop('crates', (x + (1.5 if x < 0 else -1.5), z + 2), r, None, variant=1)
    # The Square (Q): the crater's rim, the red door, the well-house, the willow, the tower and the Archive's front.
    prop('crater', (0, 70), 0, 'The crater', (24, 25))
    prop('wellhouse', (-33, 49), 30, 'The well-house')
    prop('dead_oak', (28, 52), 0, 'The stone willow', scale=1.3)
    prop('belfry', TOWER, 0, 'The Bell Tower', (12, 12))
    prop('archive', ARCHIVE, 0, 'The Old Archive', (50, 24))
    prop('granary', GRANARY, 0, 'The Granary', (40, 18))
    prop('gatehouse', GH['at'], 0, 'The bridge-head gatehouse', (GH['w'], GH['d']))
    prop('bridge', (46, -60), 90, 'The Granary footbridge', (8, 0))
    prop('ride', (GRANARY[0] - 2, GRANARY[1] - 27), 90, 'The black-iron wagon', interact='Climb onto the wagon')
    # The spirit stones (two, 20 m from any camp): under the gatehouse's wall-walk, by the willow.
    prop('stone', (GH['at'][0] - 5.5, GH['at'][1] - 3), 0, "The gatehouse's spirit stone", interact='Touch the spirit stone')
    prop('stone', (34, 48), 0, "The willow's spirit stone", interact='Touch the spirit stone')
    # The Bureau's copying desks (the Preservationists' second quest: three to spoil).
    ax, az = ARCHIVE
    for i, dx in enumerate((-8, 8)):
        prop('copydesk', (ax + dx, az + 15), 0, 'A Bureau copying desk', interact='Spoil the copies', once=True)
    prop('copydesk', (ax - 16, az - 8), 90, 'A Bureau copying desk', interact='Spoil the copies', once=True)
    prop('satchel', (ax + 1.5, az + 15.5), 0, "Warden Elvet's satchel", interact="Take the Warden's papers")
    # The Warden's papers' tally and the pickers' crates (dressing).
    for i in range(5): prop('crates', (GRANARY[0] - 10 + i * 4, GRANARY[1] - 3), 0, None, variant=i % 3)
    for (x, z) in [(-14, -328), (14, -300), (-15, -262), (16, -232)]: prop('rock', (x, z), 0, None, variant=1)

# ---------------------------------------------------------------- secrets (7; DUNGEON2_DESIGN section 5)
SECRETS = []
def secret(slug, name, kind, at, prompt, text, xp, canon, **kw):
    s = {'id': 'secret.hallows.' + slug, 'name': name, 'kind': kind, 'at': {'x': round(at[0], 2), 'y': round(at[1], 2)}, 'prompt': prompt, 'text': text, 'xp': xp, 'canonStatus': canon}
    s.update(kw); SECRETS.append(s); return s

def make_secrets(l2):
    a, s = CA('OV', 25, -246.3)
    secret('satchel', "A Soldier's Satchel", 'cache', (25, -246.3), 'Pull the satchel out from under the rock',
           "Wedged under the overhang's lip where the ash cannot reach it: a Pact-issue satchel, the strap cut, and a letter folded small inside.", 220,
           'GAME-ONLY (the Iron Pact is CANON, book1 ch.2)', document='doc.hallows.soldiers_letter', gold=28, cave="The overhang", along=a, aside=s)
    secret('bets-cart', "Bet's Packed Cart", 'cache', (-23.5, -101.5), 'Look under the stone tarpaulin',
           "The cart is packed for the road west and turned to stone with it: a chair lashed on top, a quilt, a tin box under the seat that the stone did not take.", 260,
           "GAME-ONLY (Bet's letter is already in the game)", gold=40)
    secret('masons-cache', "The Mason's Cache", 'cache', (l2[0] - 3, l2[1] + 3), 'Open the mortar-tub',
           "A mason's tub on the lowest slab, the mortar in it grey and set, and under it, wrapped in a leather apron, the man's own ring.", 280,
           'GAME-ONLY', rise=-13, gold=45)
    secret('two-coppers', 'Two Coppers on the Sill', 'cache', (-9.4, -73.8), 'Pick up the two coppers',
           "Two copper pennies on the bakery's window sill, left for a loaf fifty years ago and never taken in. They are warm.", 160, 'CANON-EXPANDED (the baker and his shop, book1 ch.21; the coppers GAME-ONLY)', gold=12)
    secret('reed-key', "Old Mother Reed's Key", 'key', (-34.5, 51.5), 'Take the key from the well-house hook',
           "On a hook inside the well-house door, where a careful woman would hang a spare: a small brass key with REED scratched on its bow.", 120, 'GAME-ONLY')
    secret('reed-chest', 'The Reed Chest', 'chest', (7.4, -93.2), 'Unlock the chest by the fence',
           "A blanket chest set out by the Reed house's fence for a cart that never came, the lid stone but the lock still brass. Inside: linen, a kettle-holder worked with a kingfisher, and a purse.", 300,
           'GAME-ONLY (Tamsin Reed and her mother are already in the game)', needs='secret.hallows.reed-key', gold=55)
    secret('archivists-page', 'A Page in the Stacks', 'read', (ARCHIVE[0] - 20, ARCHIVE[1] + 10.2), 'Take the page from the gallery shelf',
           "Pushed behind a row of stone books on the north gallery, where no Bureau clerk has thought to look: one loose page in a careful First Era hand.", 320,
           'CANON-EXPANDED (High Archivist Malakor and the Archivists are CANON, book1 ch.18; this page paraphrased, GAME-ONLY)', rise=5, document='doc.hallows.archivist_page')
    secret('ringers-names', "The Ringers' Chalked Names", 'carving', (TOWER[0] - 4.6, TOWER[1] - 2), 'Read the names chalked on the tower wall',
           "Chalked low on the tower's inside wall in four hands: the ringers' names and a day of the week, the way bell-ringers mark a practice. The last one stops half way through the word.", 200,
           'GAME-ONLY')

def landmarks():
    L = [("The Ash Road", (0, -300), 16, G), ('The Fossilised Farm', (-48, -282), 14, 'CANON (book1 ch.17)'), ('The Bridge of Shattered Glass', (0, -180), 20, 'CANON (book1 ch.17)'),
         ('The bridge-head gatehouse', GH['at'], 8, G), ('Main Street', (0, -60), 14, 'CANON (book1 ch.18)'), ('The Square', (0, 70), 26, 'CANON (book1 ch.19)'),
         ('The Granary on the race', GRANARY, 16, G), ('The Old Archive', ARCHIVE, 18, 'CANON (book1 ch.18)'), ('The Bell Tower', TOWER, 8, 'CANON-EXPANDED (book1 ch.21)'),
         ('The Cut-Store', (-41, -20), 8, G)]
    return [{'name': n, 'at': {'x': p[0], 'y': p[1]}, 'radius': r, 'canonStatus': c} for n, p, r, c in L]

HAZARDS = [  # the hungry shadows (N4): strips across part of a way, never the whole of it
    ('A long shadow on the road', (-7, -332), (2, -327), 3), ('A shadow across the farm track', (-14, -290), (-4, -286), 3),
    ('A shadow under the stone trees', (2, -268), (7, -276), 3), ('A shadow by the doorsteps', (-7, -96), (-1, -92), 3),
    ('A shadow at the ford', (2, 22), (7, 18), 3), ('A shadow in the long cellar', (-42.5, -33), (-39.5, -30), 2.5)]

def zone_json(far_lip):
    gx, gz = GH['at']
    pit_s, pit_n = LIP_S + .6, far_lip - .6
    return {
        'id': 'zone.hallows', 'displayName': "Hallow's Creek", 'subtitle': 'The town the Wasting paused', 'canonStatus': 'CANON-EXPANDED', 'dungeon': True,
        'canonNote': "Hallow's Creek, the first settlement the Wasting took, paused mid-second fifty years before the books: the white fences, the "
                     "baker in his doorway, the ball in the air, the townsfolk stone to the skin with living eyes, the broken bridge's slabs hanging over the "
                     "chasm, the crater with the red door at its heart, the Old Archive and the hungry shadows (CANON, book1 ch.15-21; world_bible). "
                     "CANON-EXPANDED: the Bureau holding the town and cutting memory-quartz from the paused (book1 ch.3: geodes are fossilised memory), "
                     "the Hollow as what the cut leaves, the bell tower with a painted dial a minute short of noon (no works). GAME-ONLY: every person, camp, "
                     "the wings, the Waking Hour as a worked thing, the wagon. Kept out: the Archive's vault, the red door's riddle, the Prism, Hollow children. "
                     "Docs/DUNGEON2_DESIGN.md.",
        'size': SIZE, 'levelMin': 15, 'levelMax': 20, 'roadsNote': 'the Ash Road, Main Street and the wagon track', 'biome': 'ash',
        'weather': [{'kind': 'overcast', 'weight': 3}, {'kind': 'ashsquall', 'weight': 2}, {'kind': 'mist', 'weight': 1}],
        'worldMapPosition': {'x': 0.66, 'y': 0.47}, 'worldMapNote': "East of Oakhaven, past the Road's End, in the grey.",
        'flatRadius': 400, 'hillHeight': 0, 'seed': 9161,
        'shapes': [{'name': 'The crater', 'center': {'x': 0, 'y': 70}, 'radius': 20, 'height': -16, 'blend': 4, 'paint': 'unmade'}],
        'pits': [{'name': 'The chasm', 'center': {'x': 0, 'y': round((pit_s + pit_n) / 2, 2)}, 'size': {'x': 380, 'y': round(pit_n - pit_s, 2)}, 'depth': 40, 'wall': 2.5, 'fog': -26}],
        'hazards': [{'name': n, 'kind': 'shadow', 'from': {'x': a[0], 'y': a[1]}, 'to': {'x': b[0], 'y': b[1]}, 'width': w, 'slow': .6, 'tick': .01,
                     'canonStatus': 'CANON (the hungry shadows, book1 ch.17); GAME-ONLY strip'} for n, a, b, w in HAZARDS],
        'ride': {'name': 'The black-iron wagon', 'opensOn': 'Picker-Master Fulke Yelland', 'seconds': 6, 'arrive': {'x': 2, 'y': -372},
                 'shut': "The wagon is the Picker-Master's: his teamsters will not let a stranger up while he stands.",
                 'text': "The mules lean into the traces. The wagon creaks out of the yard, round the chasm's east head on the Bureau's track and down to the milestone, where the grey thins. Nobody looks back at the town; it does not look back either."},
        'lighting': {'sunPitch': 9, 'sunYaw': -60, 'sunIntensity': .55, 'sunColor': '#C8C4CE', 'ambientSky': '#77788A', 'ambientEquator': '#66667A',
                     'ambientGround': '#3A3A44', 'fogColor': '#8E8C9A', 'fogStart': 35, 'fogEnd': 190, 'sunHigh': 12, 'skyTint': '#7A788C', 'skyExposure': .8, 'skyHaze': 1.2},
        'spawns': {'player': {'x': 0, 'y': -376}, 'playerFacing': 0, 'companion': {'x': 2, 'y': -378}, 'recovery': {'x': -2, 'y': -378}, 'leash': 24, 'enemies': []},
        'objectives': ["Cross the broken bridge into Hallow's Creek", "Free the Preservationists' Warden from the Old Archive", "Silence the bell in the Bell Tower"],
        'exits': [{'to': 'zone.oakhaven', 'name': "The Ash Road back to Oakhaven", 'at': {'x': 0, 'y': -389}, 'arrive': {'x': 236, 'y': 124.2}, 'radius': 3.5}],
        'roads': [{'name': 'The Ash Road', 'width': 12, 'points': [{'x': 0, 'y': -396}, {'x': 0, 'y': -366}, {'x': 0, 'y': LIP_S - .5}]},
                  {'name': 'Main Street', 'width': 12, 'points': [{'x': 0, 'y': round(gz + 7, 2)}, {'x': 0, 'y': 28}]},
                  {'name': 'The Granary road', 'width': 4, 'points': [{'x': 50, 'y': -60}, {'x': 75, 'y': -70}]},
                  {'name': 'The wagon track', 'width': 4, 'points': [{'x': GRANARY[0], 'y': GRANARY[1] - 44}, {'x': 200, 'y': -130}, {'x': 200, 'y': -250},
                                                                   {'x': 60, 'y': -340}, {'x': 8, 'y': -366}]}],
        'water': [{'name': "Hallow's Creek", 'width': 5, 'depth': 1.1, 'points': [{'x': -22, 'y': 25}, {'x': 0, 'y': 25}, {'x': 46, 'y': 24}, {'x': 46, 'y': -40},
                                                                                {'x': 46, 'y': -100}, {'x': 46, 'y': round(pit_n + 6, 2)}]},
                  {'name': 'The race', 'width': 2.5, 'depth': 1.1, 'points': [{'x': 46, 'y': -56}, {'x': 76, 'y': -59}, {'x': 116, 'y': -59}]}],
        'clearings': [{'name': 'The yard', 'center': {'x': 0, 'y': -370}, 'radius': 18}],
        'groves': [{'name': 'Stone trees, west of the road', 'kind': 'dead', 'center': {'x': -26, 'y': -335}, 'size': {'x': 22, 'y': 60}, 'count': 30},
                   {'name': 'Stone trees, east of the road', 'kind': 'dead', 'center': {'x': 26, 'y': -320}, 'size': {'x': 22, 'y': 80}, 'count': 34},
                   {'name': 'Stone trees by the lip', 'kind': 'dead', 'center': {'x': -30, 'y': -232}, 'size': {'x': 30, 'y': 24}, 'count': 16}],
        'landmarks': landmarks(),
        'secrets': SECRETS, 'props': PROPS, 'decks': DECKS, 'drops': DROPS, 'gates': GATES, 'anchors': ANCHORS, 'wings': WINGS, 'events': EVENTS,
        'camps': CAMPS,
    }

def fail(msg): sys.exit('LAYOUT CHECK FAILED: ' + msg)

def check():
    for d in DECKS:
        if slope(d) > MAX_RAMP: fail('%s is %.1f degrees' % (d['name'], slope(d)))
    names = {c['name'] for c in CAMPS}; mobs = {c['mob'] for c in CAMPS}; elites = {c['mob'] for c in CAMPS if c.get('elite')}
    if len(names) != len(CAMPS): fail('camp names twice')
    for c in CAMPS:
        if not (15 <= c['levelMin'] <= c['levelMax'] <= 20): fail(c['name'] + ' levels')
        if c.get('gate') and c['gate'] != 'none' and c['gate'] not in {g['title'] for g in GATES}: fail(c['name'] + ' gate')
        if c.get('guards') and c['guards'] != 'none':
            for g in c['guards'].split(','):
                if g.strip() not in names: fail(c['name'] + ' guard ' + g)
    for g in GATES:
        if g.get('opensOn') and g['opensOn'] not in elites: fail(g['title'] + ' opensOn')
    for e in EVENTS:
        for i, w in enumerate(e['waves']):
            c = next((c for c in CAMPS if c['name'] == w['camp']), None)
            if not c or c.get('after') != 'event:%s%d' % (e['id'], i + 1): fail('event %s wave %d' % (e['id'], i + 1))
            if w.get('unless') and w['unless'] not in elites: fail('unless ' + w['unless'])
    anames = {a['name'] for a in ANCHORS}
    for w in WINGS:
        if w.get('home') and w['home'] not in anames: fail('home ' + w['home'])
    for e in EVENTS:
        if e['anchor'] not in anames: fail('anchor ' + e['anchor'])
    if len(SECRETS) > 9: fail('too many secrets for DiscoveryLogTests (85-100 in all)')

CALL = {'Road-Captain Everard Hurst': 20, 'Sergeant Wymond Tredway': 20, 'Assessor Piers Upcott': 20, 'Picker-Master Fulke Yelland': 20, 'Gil Coldwell, the Loft Watch': 18,
        'First-Cut': 18, 'Registrar Dunstan Pardew': 20, 'Pact-Captain Rowena Kersey': 16, 'Bureau Examiner Thurstan Vowell': 12}
def kin(look): return {'deserter': 'deserters', 'banditking': 'deserters', 'outrider': 'sandthrone', 'collector': 'concord', 'warden': 'concord', '': 'concord', 'cultist': 'cult', 'keeper': 'keepers'}.get(look, look)
def balance():
    guarded = answered = 0
    for c in CAMPS:
        if not c.get('elite'): continue
        guards = [g.strip() for g in (c.get('guards') or '').split(',') if g.strip() and c.get('guards') != 'none']
        if guards or c['count'] > 1: guarded += 1
        reach = CALL.get(c['mob'])
        if reach is None: continue
        near = []
        for o in CAMPS:
            if o is c or o.get('elite') or o['name'] in guards or kin(o['look']) != kin(c['look']): continue
            d = math.hypot(o['center']['x'] - c['center']['x'], o['center']['y'] - c['center']['y'])
            if d - o['radius'] <= reach: near.append((o['name'], round(d, 1)))
        for o in CAMPS:
            if o is c or not o.get('elite') or kin(o['look']) != kin(c['look']): continue
            d = math.hypot(o['center']['x'] - c['center']['x'], o['center']['y'] - c['center']['y'])
            if d - o['radius'] <= reach: print('   !! %s calls within reach of the elite %s' % (c['mob'], o['mob']))
        if near: answered += 1; print('   %s is answered by %s' % (c['mob'], near))
    print('elites with guards %d, answered by kin %d' % (guarded, answered))
    print('tags', sorted({c['tag'] for c in CAMPS}))

def summary():
    elites = [c for c in CAMPS if c.get('elite')]
    print('camps %d (elites %d, released later %d, optional %d, patrols %d); mobs %d' % (len(CAMPS), len(elites), sum(1 for c in CAMPS if c.get('after')),
          sum(1 for c in CAMPS if c.get('optional')), sum(1 for c in CAMPS if c.get('route')), sum(c['count'] for c in CAMPS)))
    for d in DECKS: print('  deck %-28s %5.1f deg  %s -> %s  %.1f..%.1f' % (d['name'], slope(d), (d['at']['x'], d['at']['y']), (d['at2']['x'], d['at2']['y']), d['rise'], d['rise2']))
    for c in CAVES: print('  cave %-22s len %6.1f  floors %.1f..%.1f%s' % (c.name, c.length, min(r[4] for r in c.rings), max(r[4] for r in c.rings), ('  joins %s +%.1f' % (c.join.name, c.join_rise)) if c.join else ''))

if __name__ == '__main__':
    build_caves()
    far = make_bridge(); l1, l2 = make_orbit(); make_gatehouse(far); make_tower(); make_archive(); make_granary()
    make_camps(GH['at'][1], l2); make_works(); make_props(); make_secrets(l2)
    check(); summary(); balance()
    z = zone_json(far)
    with io.open(os.path.join(ZONES, 'hallows.json'), 'w', encoding='utf-8', newline='\n') as f: f.write(json.dumps(z, indent=2, ensure_ascii=False) + '\n')
    print('written', os.path.join(ZONES, 'hallows.json'))
