"""The Sealed Adit as a map Chris can check places against (2026-10-15: "can you produce a complete map of the dungeon? so i can verify
locations"). Reads EncounterContent/Zones/adit.json and draws every cave from its plan rows the way Hollow.cs builds them (Catmull-Rom
centreline every ~0.7 m, the half-width, the floor's drop), with every camp, boss, gate, secret, landmark, deck, pool and drop on top.
Writes one self-contained HTML page: pan (drag), zoom (wheel or the buttons), layers, a search box, and a hover card with the world
x / z the game uses (/loc in game prints yours) and the floor's depth below the Adit's mouth.

Usage: python adit_map.py [out.html]   (default D:/crulanda-work/outputs/adit-map.html)
"""
import html, json, math, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ZONE = os.path.join(HERE, '..', '..', '..', 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent', 'Zones', 'adit.json')
OUT = sys.argv[1] if len(sys.argv) > 1 else r'D:\crulanda-work\outputs\adit-map.html'

d = json.load(open(ZONE, encoding='utf-8'))


def catmull(p0, p1, p2, p3, u):
    u2, u3 = u * u, u * u * u
    return [0.5 * ((2 * b) + (-a + c) * u + (2 * a - 5 * b + 4 * c - e) * u2 + (-a + 3 * b - 3 * c + e) * u3)
            for a, b, c, e in zip(p0, p1, p2, p3)]


class Cave:
    def __init__(self, prop, mouth_y):
        self.name, self.prop = prop['name'], prop
        flat = prop['plan']; rows = [flat[i:i + 5] for i in range(0, len(flat), 5)]
        yaw = math.radians(prop.get('rotation', 0) or 0); cy, sy = math.cos(yaw), math.sin(yaw)
        ax, az = prop['at']['x'], prop['at']['y']
        self.c, self.half, self.along, self.y = [], [], [], []
        along, last = 0.0, None

        def P(i):
            r = rows[max(0, min(i, len(rows) - 1))]; return r[:4]

        def D(i):
            r = rows[max(0, min(i, len(rows) - 1))]; return r[4] if len(r) > 4 else 0

        for seg in range(len(rows) - 1):
            p0, p1, p2, p3 = P(seg - 1), P(seg), P(seg + 1), P(seg + 2)
            steps = max(1, math.ceil(math.hypot(p2[0] - p1[0], p2[1] - p1[1]) / .7))
            k = 0
            while k < steps or (seg + 2 == len(rows) and k == steps):
                u = k / steps; q = catmull(p0, p1, p2, p3, u)
                # Unity's Quaternion.Euler(0, yaw, 0) * (x, 0, z)
                wx, wz = q[0] * cy + q[1] * sy, -q[0] * sy + q[1] * cy
                x, z = ax + wx, az + wz
                if last is not None: along += math.hypot(x - last[0], z - last[1])
                self.c.append((x, z)); self.half.append(max(.2, q[2])); self.along.append(along)
                self.y.append(mouth_y + D(seg) + (D(seg + 1) - D(seg)) * u)
                last = (x, z); k += 1

    def at(self, along, aside=0.0):
        i = 0
        while i + 1 < len(self.along) and self.along[i + 1] < along: i += 1
        j = min(i + 1, len(self.c) - 1)
        f = 0 if j == i or self.along[j] == self.along[i] else max(0, min(1, (along - self.along[i]) / (self.along[j] - self.along[i])))
        x = self.c[i][0] + (self.c[j][0] - self.c[i][0]) * f; z = self.c[i][1] + (self.c[j][1] - self.c[i][1]) * f
        y = self.y[i] + (self.y[j] - self.y[i]) * f
        fx, fz = self.c[j][0] - self.c[i][0], self.c[j][1] - self.c[i][1]
        if fx * fx + fz * fz < 1e-9 and i > 0: fx, fz = self.c[i][0] - self.c[i - 1][0], self.c[i][1] - self.c[i - 1][1]
        n = math.hypot(fx, fz) or 1; fx, fz = fx / n, fz / n
        return x + fz * aside, z - fx * aside, y

    def nearest_y(self, x, z):
        best, by = 1e18, 0
        for (cx, cz), y in zip(self.c, self.y):
            dd = (cx - x) ** 2 + (cz - z) ** 2
            if dd < best: best, by = dd, y
        return by

    def outline(self):
        left, right = [], []
        for i, (x, z) in enumerate(self.c):
            j = min(i + 1, len(self.c) - 1); k = max(i - 1, 0)
            fx, fz = self.c[j][0] - self.c[k][0], self.c[j][1] - self.c[k][1]; n = math.hypot(fx, fz) or 1
            nx, nz = fz / n, -fx / n; h = self.half[i]
            left.append((x - nx * h, z - nz * h)); right.append((x + nx * h, z + nz * h))
        return left + right[::-1]


# Caves, parents first (a cave "within" another opens off its floor).
props = [p for p in d['props'] if p.get('kind') == 'cavern']
caves = {}
pending = list(props)
while pending:
    moved = False
    for p in list(pending):
        parent = p.get('within')
        if parent and parent not in caves and any(q['name'] == parent for q in props): continue
        if parent and parent in caves: mouth = caves[parent].nearest_y(p['at']['x'], p['at']['y']) + (p.get('rise', 0) or 0)
        else: mouth = 0.0
        caves[p['name']] = Cave(p, mouth); pending.remove(p); moved = True
    if not moved:
        for p in pending: caves[p['name']] = Cave(p, 0.0)
        break


def place(o):
    """World x, z and floor y of anything placed by 'at', 'center', or a cave with along/aside."""
    if o.get('cave') in caves and ('along' in o or 'from' in o):
        a = o.get('along', o.get('from', 0)) or 0
        return caves[o['cave']].at(a, o.get('aside', 0) or 0)
    p = o.get('center') or o.get('at')
    if p is None: return None
    x, z = p['x'], p['y']
    y = None
    for c in caves.values():
        for (cx, cz), h, cyy in zip(c.c[::4], c.half[::4], c.y[::4]):
            if (cx - x) ** 2 + (cz - z) ** 2 <= (h + 1) ** 2: y = cyy; break
        if y is not None: break
    return x, z, (y if y is not None else 0.0)


wing_of = {}
for w in d.get('wings', []): pass
items = []   # (layer, kind, name, x, z, y, extra dict)
for cm in d['camps']:
    pl = place(cm)
    if not pl: continue
    boss = any(k in (cm.get('mob', '') + cm.get('name', '')) for k in ('(elite)',)) or cm.get('elite')
    layer = 'boss' if boss else ('patrol' if cm.get('route') else ('released' if cm.get('after') else 'camp'))
    extra = {'mob': cm.get('mob'), 'count': cm.get('count'), 'levels': f"{cm.get('levelMin')}-{cm.get('levelMax')}",
             'respawn': cm.get('respawn'), 'cave': cm.get('cave'), 'label': cm.get('canonStatus', '')}
    if cm.get('after'): extra['released by'] = cm['after']
    if cm.get('optional'): extra['optional'] = 'yes'
    items.append((layer, 'camp', cm['name'], *pl, extra))
    if cm.get('route'):
        items.append(('patrol', 'route', cm['name'], *pl, {'route': [(r['x'], r['y']) for r in cm['route']]}))
for g in d.get('gates', []):
    pl = place(g)
    if pl: items.append(('gate', 'gate', g.get('title', 'gate'), *pl, {'kind': g.get('kind'), 'cave': g.get('cave'), 'prompt': g.get('prompt')}))
for s in d.get('secrets', []):
    pl = place(s)
    if pl: items.append(('secret', 'secret', s.get('name', s.get('id')), *pl, {'kind': s.get('kind'), 'prompt': s.get('prompt'), 'id': s.get('id')}))
for l in d.get('landmarks', []):
    pl = place(l)
    if pl: items.append(('landmark', 'landmark', l['name'], *pl, {'label': l.get('canonStatus', '')}))
for a in d.get('anchors', []):
    pl = place(a)
    if pl: items.append(('anchor', 'anchor', a['name'], *pl, {'cave': a.get('cave')}))
for e in d.get('exits', []):
    items.append(('landmark', 'exit', e.get('name', 'exit'), e['at']['x'], e['at']['y'], 0.0, {'to': e.get('to')}))

decks = []
for dk in d.get('decks', []):
    c = caves.get(dk.get('cave'))
    if not c: continue
    x0, z0, y0 = c.at(dk['from'], dk.get('aside', 0) or 0); x1, z1, y1 = c.at(dk['to'], dk.get('aside2', dk.get('aside', 0)) or 0)
    decks.append((dk['name'], x0, z0, x1, z1, dk.get('width', 2), y0 + (dk.get('rise', 0) or 0), y1 + (dk.get('rise2', dk.get('rise', 0)) or 0)))
waters = []
for w in d.get('caveWater', []):
    c = caves.get(w.get('cave'))
    if not c: continue
    if w.get('radius') and w.get('from') == w.get('to'):
        x, z, y = c.at(w['from'], w.get('aside', 0) or 0); waters.append(('pool', w['name'], [(x, z)], w['radius'], w.get('depth')))
    else:
        pts = []; a, b = sorted((w['from'], w['to']))
        while a <= b: pts.append(c.at(a, w.get('aside', 0) or 0)[:2]); a += 2
        pts.append(c.at(b, w.get('aside', 0) or 0)[:2])
        waters.append(('stretch', w['name'], pts, w.get('half', w.get('radius', 3)) or 3, w.get('depth')))
drops = []
for dr in d.get('drops', []):
    a, b = caves.get(dr['fromCave']), caves.get(dr['toCave'])
    if a and b: drops.append((dr['name'], *a.at(dr['from'], dr.get('fromAside', 0))[:2], *b.at(dr['to'], dr.get('toAside', 0))[:2]))
crumbs = []
for cr in d.get('crumbs', []):
    c = caves.get(cr['cave'])
    if c:
        pts = []; a = cr['from']
        while a <= cr['to']: pts.append(c.at(a, cr.get('aside', 0))[:2]); a += 3
        crumbs.append(pts)

# Bounds
xs = [p[0] for c in caves.values() for p in c.outline()] + [i[3] for i in items]
zs = [p[1] for c in caves.values() for p in c.outline()] + [i[4] for i in items]
minx, maxx, minz, maxz = min(xs) - 15, max(xs) + 15, min(zs) - 15, max(zs) + 15
ymin = min(min(c.y) for c in caves.values()); ymax = max(max(c.y) for c in caves.values())


def depth_colour(y):
    t = 0 if ymax == ymin else (ymax - y) / (ymax - ymin)
    # pale sand at the mouth to deep blue-violet at the bottom
    a, b = (214, 196, 150), (52, 62, 120)
    return '#%02x%02x%02x' % tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def sx(x): return round(x - minx, 2)
def sz(z): return round(maxz - z, 2)


svg = []
svg.append(f'<g id="grid">')
step = 25
gx = math.floor(minx / step) * step
while gx <= maxx:
    svg.append(f'<line x1="{sx(gx)}" y1="0" x2="{sx(gx)}" y2="{maxz - minz:.1f}" class="g{" major" if gx % 100 == 0 else ""}"/>')
    if gx % 100 == 0: svg.append(f'<text x="{sx(gx) + 1}" y="4" class="gl">x {gx}</text>')
    gx += step
gz = math.floor(minz / step) * step
while gz <= maxz:
    svg.append(f'<line x1="0" y1="{sz(gz)}" x2="{maxx - minx:.1f}" y2="{sz(gz)}" class="g{" major" if gz % 100 == 0 else ""}"/>')
    if gz % 100 == 0: svg.append(f'<text x="1" y="{sz(gz) - 1}" class="gl">z {gz}</text>')
    gz += step
svg.append('</g><g id="caves">')
for c in caves.values():
    # each cave drawn as short slabs coloured by depth, so a descending passage shades as it goes down
    ol = c.outline(); n = len(c.c)
    left, right = ol[:n], ol[n:][::-1]
    for i in range(0, n - 1, 3):
        j = min(i + 3, n - 1)
        pts = [left[i], left[j], right[j], right[i]]
        svg.append(f'<polygon points="{" ".join(f"{sx(x)},{sz(z)}" for x, z in pts)}" fill="{depth_colour(c.y[i])}" stroke="{depth_colour(c.y[i])}" class="cave" '
                   f'data-n="{html.escape(c.name)}" data-k="cave" data-x="{c.c[i][0]:.1f}" data-z="{c.c[i][1]:.1f}" data-y="{c.y[i]:.1f}" data-a="{c.along[i]:.0f}"/>')
    svg.append(f'<polyline points="{" ".join(f"{sx(x)},{sz(z)}" for x, z in ol + [ol[0]])}" class="rim"/>')
svg.append('</g><g id="L-water">')
for kind, name, pts, r, depth in waters:
    if kind == 'pool':
        x, z = pts[0]
        svg.append(f'<circle cx="{sx(x)}" cy="{sz(z)}" r="{r}" class="water" data-n="{html.escape(name)}" data-k="water" data-x="{x:.1f}" data-z="{z:.1f}" data-extra="{html.escape(json.dumps({"depth m": depth}))}"/>')
    else:
        svg.append(f'<polyline points="{" ".join(f"{sx(x)},{sz(z)}" for x, z in pts)}" class="waterline" stroke-width="{2 * r}" data-n="{html.escape(name)}" data-k="water" data-x="{pts[0][0]:.1f}" data-z="{pts[0][1]:.1f}" data-extra="{html.escape(json.dumps({"depth m": depth}))}"/>')
svg.append('</g><g id="L-deck">')
for name, x0, z0, x1, z1, w, y0, y1 in decks:
    svg.append(f'<line x1="{sx(x0)}" y1="{sz(z0)}" x2="{sx(x1)}" y2="{sz(z1)}" stroke-width="{w}" class="deck" data-n="{html.escape(name)}" data-k="deck" '
               f'data-x="{(x0 + x1) / 2:.1f}" data-z="{(z0 + z1) / 2:.1f}" data-y="{(y0 + y1) / 2:.1f}" data-extra="{html.escape(json.dumps({"from": f"({x0:.0f}, {z0:.0f}) floor {y0:.1f}", "to": f"({x1:.0f}, {z1:.0f}) floor {y1:.1f}"}))}"/>')
svg.append('</g><g id="L-drop">')
for name, x0, z0, x1, z1 in drops:
    svg.append(f'<line x1="{sx(x0)}" y1="{sz(z0)}" x2="{sx(x1)}" y2="{sz(z1)}" class="drop" marker-end="url(#arr)" data-n="{html.escape(name)}" data-k="drop (one way)" data-x="{x0:.1f}" data-z="{z0:.1f}"/>')
svg.append('</g><g id="L-crumb">')
for pts in crumbs:
    svg.append(f'<polyline points="{" ".join(f"{sx(x)},{sz(z)}" for x, z in pts)}" class="crumb"/>')
svg.append('</g>')
order = ['patrol', 'camp', 'released', 'anchor', 'gate', 'secret', 'landmark', 'boss']
for layer in order:
    svg.append(f'<g id="L-{layer}">')
    for (ly, kind, name, x, z, y, extra) in items:
        if ly != layer: continue
        if kind == 'route':
            pts = [(x, z)] + extra['route']
            svg.append(f'<polyline points="{" ".join(f"{sx(a)},{sz(b)}" for a, b in pts)}" class="route"/>'); continue
        ex = html.escape(json.dumps({k: v for k, v in extra.items() if v not in (None, '')}))
        attrs = f'data-n="{html.escape(name)}" data-k="{kind if layer != "boss" else "boss"}" data-x="{x:.1f}" data-z="{z:.1f}" data-y="{y:.1f}" data-extra="{ex}"'
        X, Z = sx(x), sz(z)
        if layer == 'boss':
            svg.append(f'<g class="pin" {attrs}><path d="M{X},{Z - 3.4} L{X + 3},{Z} L{X},{Z + 3.4} L{X - 3},{Z} Z" class="boss"/></g>')
        elif layer == 'gate':
            svg.append(f'<g class="pin" {attrs}><rect x="{X - 2}" y="{Z - 2}" width="4" height="4" class="gate"/></g>')
        elif layer == 'secret':
            svg.append(f'<g class="pin" {attrs}><path d="M{X},{Z - 2.6} L{X + .8},{Z - .8} L{X + 2.6},{Z} L{X + .8},{Z + .8} L{X},{Z + 2.6} L{X - .8},{Z + .8} L{X - 2.6},{Z} L{X - .8},{Z - .8} Z" class="secret"/></g>')
        elif layer == 'landmark':
            svg.append(f'<g class="pin" {attrs}><circle cx="{X}" cy="{Z}" r="1.2" class="lm"/></g>')
        elif layer == 'anchor':
            svg.append(f'<g class="pin" {attrs}><circle cx="{X}" cy="{Z}" r=".8" class="anchor"/></g>')
        else:
            svg.append(f'<g class="pin" {attrs}><circle cx="{X}" cy="{Z}" r="1.7" class="{layer}"/></g>')
    svg.append('</g>')
# labels: caves at their middles, bosses beside their pins
svg.append('<g id="L-labels">')
for c in caves.values():
    m = len(c.c) // 2
    svg.append(f'<text x="{sx(c.c[m][0])}" y="{sz(c.c[m][1])}" class="cavelab">{html.escape(c.name)}</text>')
for (ly, kind, name, x, z, y, extra) in items:
    if ly == 'boss': svg.append(f'<text x="{sx(x) + 4}" y="{sz(z) + 1.2}" class="bosslab">{html.escape(name)}</text>')
svg.append('</g>')

# ---------- the run, step by step, and the links checked (Chris, 2026-10-15: "i also need a step by step progression to make sure
# everything links together") ----------
QDIR = os.path.join(os.path.dirname(ZONE), '..', 'Quests', 'adit.json')
SETS = os.path.join(os.path.dirname(ZONE), '..', 'Items', 'loot.sets.json')
quests = json.load(open(QDIR, encoding='utf-8')).get('quests', []) if os.path.exists(QDIR) else []
CODE_EVENTS = {'powder', 'greyline1', 'greyline2', 'greyline3', 'haul1', 'haul2'}   # raised by the code (EncounterSession.Adit/GreySpread/Haul)
wings = d.get('wings', []); wing_ids = {w['id'] for w in wings}; groups = {}
for w in wings:
    if w.get('group'): groups.setdefault(w['group'], []).append(w['id'])
camps = d['camps']; names = {}
for c in camps:
    for k in ('name', 'mob'):
        if c.get(k): names.setdefault(c[k], c)
issues, oks = [], []


def ok(cond, good, bad):
    (oks if cond else issues).append(good if cond else bad)


def parents(w):
    out = []
    for a in (w.get('after') or '').split(','):
        a = a.strip()
        if a: out += groups.get(a, [a])
    return out


for w in wings:
    for a in parents(w): ok(a in wing_ids, f"wing '{w['name']}' follows '{a}'", f"wing '{w['name']}' follows '{a}', which is no wing or group")
wing_order, placed = [], set()
while len(wing_order) < len(wings):
    nxt = [w for w in wings if w['id'] not in placed and all(a in placed or a not in wing_ids for a in parents(w))]
    if not nxt:
        issues.append('the wings loop: ' + ', '.join(w['id'] for w in wings if w['id'] not in placed)); wing_order += [w for w in wings if w['id'] not in placed]; break
    for w in nxt: wing_order.append(w); placed.add(w['id'])
ancestors = {}


def anc(wid, seen=None):
    if wid in ancestors: return ancestors[wid]
    seen = seen or set(); out = set()
    for a in parents(next((w for w in wings if w['id'] == wid), {})):
        if a in seen: continue
        out.add(a); out |= anc(a, seen | {a})
    ancestors[wid] = out; return out


for c in camps:
    ok(c.get('wing') in wing_ids, '', f"camp '{c['name']}' is in wing '{c.get('wing')}', which does not exist")
gate_by = {g['title']: g for g in d.get('gates', [])}
for g in d.get('gates', []):
    ok(g.get('cave') in caves, f"gate '{g['title']}' stands in {g.get('cave')}", f"gate '{g['title']}' names cave '{g.get('cave')}', not built")
    if g.get('opensOn'):
        boss = names.get(g['opensOn'])
        ok(boss is not None, f"gate '{g['title']}' opens when {g['opensOn']} dies", f"gate '{g['title']}' opens on '{g['opensOn']}', who is in no camp")
        if boss:
            behind = [c for c in camps if c.get('gate') == g['title']]
            bad = [c['name'] for c in behind if c.get('wing') != boss.get('wing') and boss.get('wing') not in anc(c.get('wing'))]
            ok(not bad, f"everything behind '{g['title']}' comes after {g['opensOn']}'s wing",
               f"behind '{g['title']}' but not after {g['opensOn']}'s wing ({boss.get('wing')}): " + ', '.join(bad[:6]))
    if g.get('lord'): ok(g['lord'] in names, f"gate '{g['title']}': its lord {g['lord']} stands", f"gate '{g['title']}' names lord '{g['lord']}', who is in no camp")
for c in camps:
    if c.get('gate') and c['gate'] != 'none': ok(c['gate'] in gate_by, '', f"camp '{c['name']}' is behind gate '{c['gate']}', which does not exist")
keyed = [c for c in camps if c.get('key')]
for g in [g for g in d.get('gates', []) if g.get('kind') == 'liftgate']:
    behind = {c.get('wing') for c in camps if c.get('gate') == g['title']}
    for k in keyed:
        late = [w for w in behind if k.get('wing') not in anc(w)]
        ok(not late, f"'{g['title']}' needs {k.get('keyName')} from {k.get('mob')} ({k.get('wing')}), fought before it",
           f"'{g['title']}' needs {k.get('keyName')} from {k.get('mob')}, but wing(s) {', '.join(late)} behind it do not come after {k.get('wing')}")
wake_ids = set()
for e in d.get('events', []):
    for i, wv in enumerate(e.get('waves', [])):
        wake_ids.add(f"{e['id']}{i + 1}")
        ok(wv.get('camp') in names, f"event {e['id']} wave {i + 1}: {wv.get('camp')}", f"event {e['id']} wave {i + 1} names '{wv.get('camp')}', no such camp")
for c in camps:
    a = c.get('after')
    if not a: continue
    kind, _, who = a.partition(':')
    if kind in ('kill', 'loose'): ok(who in names, f"'{c['name']}' comes on {kind} of {who}", f"'{c['name']}' waits on {kind} of '{who}', who is in no camp")
    elif kind == 'event': ok(who in wake_ids or who in CODE_EVENTS, f"'{c['name']}' comes on event {who}", f"'{c['name']}' waits on event '{who}', which nothing raises")
    else: issues.append(f"'{c['name']}' has an unknown release '{a}'")
qids = {q['id'] for q in quests}
for q in quests:
    for r in (q.get('requires') or []):
        ok(r in qids or not r.startswith('side.adit'), f"quest '{q.get('title')}' follows {r}", f"quest '{q.get('title')}' needs '{r}', which is not an Adit quest")
if os.path.exists(SETS):
    sd = json.load(open(SETS, encoding='utf-8'))
    for dr in sd.get('drops', []):
        m = dr.get('mob')
        if m: ok(m in names, f"class-set drop from {m}", f"a class-set drop names '{m}', who is in no Adit camp")
for w in wings:
    if w.get('home'): ok(any(a['name'] == w['home'] for a in d.get('anchors', [])), '', f"wing '{w['name']}' comes home to '{w['home']}', no such anchor")
for dr in d.get('drops', []): ok(dr['fromCave'] in caves and dr['toCave'] in caves, '', f"drop '{dr['name']}' joins caves that are not built")
oks = [o for o in oks if o]


def pin_attr(c):
    pl = place(c)
    return f' data-go="{pl[0]:.1f},{pl[1]:.1f}"' if pl else ''


P = ['<div class="pc">']
P.append(f'<div class="chk {"bad" if issues else "good"}"><b>{"Links: " + str(len(issues)) + " problem(s)" if issues else "Every link checks out"}</b> '
         f'({len(oks)} links checked: wings, gates, keys, waves, releases, quests, set drops)')
if issues: P.append('<ul>' + ''.join(f'<li>{html.escape(i)}</li>' for i in issues) + '</ul>')
P.append('</div><p class="dim">Click a line to find it on the map.</p><ol class="steps">')
gates_shown = set()
for w in wing_order:
    tag = ' <span class="opt">optional</span>' if w.get('optional') else ''
    after = ', '.join(next((x['name'] for x in wings if x['id'] == a), a) for a in parents(w))
    P.append(f'<li class="wing"><div class="wh">{html.escape(w["name"])}{tag}</div>')
    if after: P.append(f'<div class="dim">after {html.escape(after)}</div>')
    if w.get('exit'): P.append(f'<div class="dim">out by {"a one-way drop" if w["exit"] == "drop" else "walking back"}' + (f' to {html.escape(w["home"])}' if w.get('home') else '') + '</div>')
    ws = [c for c in camps if c.get('wing') == w['id']]
    steps = sorted([c for c in ws if not c.get('after')], key=lambda c: (c.get('step') is None, c.get('step') or 0, bool(c.get('route'))))
    P.append('<ul>')
    for c in steps:
        g = c.get('gate')
        if g and g != 'none' and g not in gates_shown and g in gate_by:
            gates_shown.add(g); gd = gate_by[g]
            how = f"opens when {gd['opensOn']} dies" if gd.get('opensOn') else (gd.get('prompt') or 'opens of itself')
            if gd.get('kind') == 'liftgate': how = 'set the rail sigils: ' + ', '.join(f"{k.get('keyName')} ({k.get('mob')})" for k in keyed)
            P.append(f'<li class="gate"{pin_attr(gd)}>Gate: {html.escape(g)}: {html.escape(how)}</li>')
        kind = 'boss' if c.get('elite') else ('patrol' if c.get('route') else 'camp')
        bits = [f"lv {c.get('levelMin')}-{c.get('levelMax')}", f"{c.get('count')}x {c.get('mob')}"]
        if c.get('step') is not None: bits.insert(0, f"step {c['step']}")
        if c.get('optional'): bits.append('optional')
        if c.get('key'): bits.append('drops ' + str(c.get('keyName')))
        P.append(f'<li class="{kind}"{pin_attr(c)}><b>{html.escape(c["name"])}</b> <span class="dim">{html.escape(", ".join(str(b) for b in bits))}</span></li>')
        for r in camps:
            a = r.get('after') or ''; k0, _, who = a.partition(':')
            if k0 in ('kill', 'loose') and who in (c.get('mob'), c.get('name')):
                when = 'when it dies' if k0 == 'kill' else 'during the fight'
                P.append(f'<li class="released"{pin_attr(r)}>&rarr; {html.escape(r["name"])} <span class="dim">comes {when}: {r.get("count")}x {html.escape(str(r.get("mob")))}</span></li>')
    for e in d.get('events', []):
        if e.get('wing') == w['id']:
            P.append(f'<li class="gate">Event: {html.escape(e["id"])} ({e.get("kind")}) at {html.escape(str(e.get("anchor")))}: '
                     + ', '.join(f"{wv['at']} s {html.escape(wv['camp'])}" for wv in e.get('waves', [])) + '</li>')
    for r in ws:
        a = r.get('after') or ''
        if a.startswith('event:') and a[6:] in CODE_EVENTS:
            P.append(f'<li class="released"{pin_attr(r)}>&rarr; {html.escape(r["name"])} <span class="dim">comes on {html.escape(a[6:])}: {r.get("count")}x {html.escape(str(r.get("mob")))}</span></li>')
    wcaves = {c.get('cave') for c in ws}
    for x in d.get('secrets', []):
        if x.get('cave') in wcaves: P.append(f'<li class="secret"{pin_attr(x)}>Secret: {html.escape(x.get("name", ""))} <span class="dim">{html.escape(x.get("prompt", ""))}</span></li>')
    P.append('</ul></li>')
P.append('</ol><h3>Quests</h3><ol class="quests">')
for q in quests:
    req = ', '.join(next((x.get('title') for x in quests if x['id'] == r), r) for r in (q.get('requires') or []))
    P.append(f'<li><b>{html.escape(q.get("title", q["id"]))}</b> <span class="dim">lv {q.get("level")}, from {html.escape(str(q.get("giver")))}'
             + (f'; after {html.escape(req)}' if req else '') + f'</span><div class="dim">{html.escape(q.get("summary", ""))}</div></li>')
P.append('</ol></div>')
prog_html = ''.join(P)
print('links:', len(oks), 'ok,', len(issues), 'problems')
for i in issues: print('  PROBLEM', i)

W, H = maxx - minx, maxz - minz
counts = {k: sum(1 for i in items if i[0] == k and i[1] != 'route') for k in order}
mobs = sum((cm.get('count') or 0) for cm in d['camps'])
page = f'''<!doctype html><html><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Sealed Adit map</title><style>
:root{{--bg:#16151a;--panel:#211f27;--ink:#ece6da;--dim:#a39c8f;--line:#3a3742}}
*{{box-sizing:border-box}} body{{margin:0;background:var(--bg);color:var(--ink);font:13px/1.4 system-ui,Segoe UI,sans-serif;overflow:hidden}}
#bar{{position:fixed;left:0;top:0;right:0;display:flex;gap:10px;align-items:center;flex-wrap:wrap;padding:8px 12px;background:var(--panel);border-bottom:1px solid var(--line);z-index:3}}
#bar h1{{font-size:15px;margin:0 8px 0 0;font-weight:600}} #bar label{{color:var(--dim);white-space:nowrap;cursor:pointer}} #bar input[type=search]{{background:#121116;color:var(--ink);border:1px solid var(--line);border-radius:6px;padding:4px 8px;width:200px}}
button{{background:#2d2a35;color:var(--ink);border:1px solid var(--line);border-radius:6px;padding:3px 9px;cursor:pointer}}
#map{{position:fixed;inset:0;top:0}} svg{{width:100%;height:100%;display:block;cursor:grab}} svg.drag{{cursor:grabbing}}
.g{{stroke:#24222b;stroke-width:.25}} .g.major{{stroke:#34313d;stroke-width:.4}} .gl{{fill:#5c5866;font-size:3px}}
.cave{{stroke-width:.35}} .rim{{fill:none;stroke:#00000088;stroke-width:.35}}
.water{{fill:#2a5d9ccc;stroke:#7fb3ff;stroke-width:.3}} .waterline{{fill:none;stroke:#2a5d9caa;stroke-linecap:round}}
.deck{{stroke:#f2e6c9;stroke-linecap:round;opacity:.85}} .drop{{stroke:#ff9d4d;stroke-width:.5;stroke-dasharray:1.5 1}}
.crumb{{fill:none;stroke:#7dffb0;stroke-width:.4;stroke-dasharray:.6 1.2;opacity:.8}} .route{{fill:none;stroke:#ff6b6b;stroke-width:.4;stroke-dasharray:2 1;opacity:.8}}
.camp{{fill:#e5484d;stroke:#2a0a0a;stroke-width:.3}} .patrol{{fill:#ff8f8f;stroke:#2a0a0a;stroke-width:.3}} .released{{fill:#b07cff;stroke:#1d0a2a;stroke-width:.3}}
.boss{{fill:#ffd23f;stroke:#3a2a00;stroke-width:.4}} .gate{{fill:#ff5a1f;stroke:#2a0a00;stroke-width:.3}} .secret{{fill:#58e6ff;stroke:#00303a;stroke-width:.3}}
.lm{{fill:#fff;stroke:#000;stroke-width:.3}} .anchor{{fill:#9a96a3}}
.cavelab{{fill:#fff;font-size:4px;font-weight:600;paint-order:stroke;stroke:#000;stroke-width:.9px;text-anchor:middle;pointer-events:none}}
.bosslab{{fill:#ffe48a;font-size:2.6px;paint-order:stroke;stroke:#000;stroke-width:.7px;pointer-events:none}}
.pin{{cursor:pointer}} .pin:hover *,.hit *{{stroke:#fff;stroke-width:.8}}
#card{{position:fixed;z-index:4;max-width:340px;background:#0e0d12ee;border:1px solid var(--line);border-radius:8px;padding:8px 10px;pointer-events:none;display:none}}
#card b{{font-size:14px}} #card .k{{color:var(--dim);text-transform:uppercase;font-size:11px;letter-spacing:.04em}} #card table{{border-collapse:collapse;margin-top:4px}} #card td{{padding:1px 8px 1px 0;vertical-align:top}} #card td:first-child{{color:var(--dim)}}
#legend{{position:fixed;right:12px;bottom:12px;z-index:3;background:var(--panel);border:1px solid var(--line);border-radius:8px;padding:8px 10px;font-size:12px}}
#legend div{{display:flex;align-items:center;gap:6px}} .sw{{width:12px;height:12px;border-radius:50%;display:inline-block}}
#depth{{height:8px;width:140px;border-radius:4px;background:linear-gradient(90deg,{depth_colour(ymax)},{depth_colour(ymin)})}}
#cursor{{position:fixed;left:12px;bottom:12px;z-index:3;background:var(--panel);border:1px solid var(--line);border-radius:8px;padding:6px 10px;color:var(--dim);font-variant-numeric:tabular-nums}}
#prog{{position:fixed;left:0;top:44px;bottom:0;width:430px;max-width:92vw;overflow:auto;z-index:3;background:var(--panel);border-right:1px solid var(--line);padding:10px 12px;display:none}}
#prog.on{{display:block}} #prog .wh{{font-weight:600;font-size:14px;margin-top:6px}} #prog ul{{list-style:none;padding-left:8px;margin:4px 0 8px}} #prog li{{margin:2px 0}} #prog [data-go]{{cursor:pointer;border-radius:4px}} #prog [data-go]:hover{{background:#2d2a35}}
.dim{{color:var(--dim)}} #prog .opt{{font-size:11px;color:#c9a3ff;border:1px solid #6a4f8f;border-radius:4px;padding:0 4px}} #prog li.boss b{{color:#ffd23f}} #prog li.gate{{color:#ff9a6b}} #prog li.secret{{color:#58e6ff}} #prog li.released{{color:#c9a3ff}} #prog li.patrol b{{color:#ff8f8f}}
#prog .chk{{padding:6px 8px;border-radius:6px;margin-bottom:8px}} #prog .chk.good{{background:#173a26}} #prog .chk.bad{{background:#4a1d1d}} #prog ol.steps{{padding-left:20px}} #prog h3{{margin:12px 0 4px}}
@media (max-width:700px){{#legend{{display:none}} #bar input[type=search]{{width:130px}}}}
</style></head><body>
<div id="bar"><h1>The Sealed Adit</h1>
<input type="search" id="q" placeholder="Find a camp, boss, place...">
<label><input type="checkbox" data-l="boss" checked> Bosses ({counts['boss']})</label>
<label><input type="checkbox" data-l="camp" checked> Camps ({counts['camp']})</label>
<label><input type="checkbox" data-l="patrol" checked> Patrols ({counts['patrol']})</label>
<label><input type="checkbox" data-l="released" checked> Released later ({counts['released']})</label>
<label><input type="checkbox" data-l="gate" checked> Gates ({counts['gate']})</label>
<label><input type="checkbox" data-l="secret" checked> Secrets ({counts['secret']})</label>
<label><input type="checkbox" data-l="landmark" checked> Landmarks</label>
<label><input type="checkbox" data-l="deck" checked> Decks</label>
<label><input type="checkbox" data-l="water" checked> Water</label>
<label><input type="checkbox" data-l="anchor"> Anchors</label>
<label><input type="checkbox" data-l="labels" checked> Names</label>
<button id="runb">Run order</button><button id="zin">+</button><button id="zout">&minus;</button><button id="fit">Fit</button></div>
<div id="map"><svg id="svg" viewBox="0 0 {W:.1f} {H:.1f}" xmlns="http://www.w3.org/2000/svg">
<defs><marker id="arr" viewBox="0 0 10 10" refX="8" refY="5" markerWidth="4" markerHeight="4" orient="auto"><path d="M0,0 L10,5 L0,10 z" fill="#ff9d4d"/></marker></defs>
{"".join(svg)}</svg></div>
<div id="prog">{prog_html}</div><div id="card"></div>
<div id="cursor">x –, z –</div>
<div id="legend"><div><span class="sw" style="background:#ffd23f;border-radius:2px;transform:rotate(45deg)"></span>Boss or elite</div>
<div><span class="sw" style="background:#e5484d"></span>Camp</div><div><span class="sw" style="background:#ff8f8f"></span>Patrol (dashed: its road)</div>
<div><span class="sw" style="background:#b07cff"></span>Released later (waves, relief)</div><div><span class="sw" style="background:#ff5a1f;border-radius:2px"></span>Gate, door, bar</div>
<div><span class="sw" style="background:#58e6ff"></span>Secret</div><div><span class="sw" style="background:#f2e6c9;border-radius:2px"></span>Deck, catwalk, stair</div>
<div><span class="sw" style="background:#2a5d9c"></span>Water</div><div><span class="sw" style="background:#7dffb0"></span>Lichen trail</div>
<div style="margin-top:4px">Floor depth: mouth <span id="depth"></span> {ymin:.0f} m</div>
<div style="color:var(--dim);margin-top:4px">{len(caves)} caves, {len(d['camps'])} camps, {mobs} mobs. North is up.</div></div>
<script>
const svg=document.getElementById('svg'),card=document.getElementById('card'),cur=document.getElementById('cursor');
const W={W:.1f},H={H:.1f},MINX={minx:.2f},MAXZ={maxz:.2f};
let vb={{x:0,y:0,w:W,h:H}};
function set(){{svg.setAttribute('viewBox',vb.x+' '+vb.y+' '+vb.w+' '+vb.h);const px=vb.w/svg.getBoundingClientRect().width;document.querySelectorAll('.cavelab').forEach(t=>{{t.style.fontSize=(13*px)+'px';t.style.strokeWidth=(3*px)+'px'}});document.querySelectorAll('.bosslab').forEach(t=>{{t.style.fontSize=(11*px)+'px';t.style.strokeWidth=(2.5*px)+'px'}});document.querySelectorAll('.gl').forEach(t=>t.style.fontSize=(10*px)+'px')}}
function fit(){{const r=svg.getBoundingClientRect(),a=r.width/r.height;if(W/H>a){{vb.w=W;vb.h=W/a;vb.x=0;vb.y=(H-vb.h)/2}}else{{vb.h=H;vb.w=H*a;vb.y=0;vb.x=(W-vb.w)/2}}set()}}
function pt(e){{const r=svg.getBoundingClientRect();return{{x:vb.x+(e.clientX-r.left)/r.width*vb.w,y:vb.y+(e.clientY-r.top)/r.height*vb.h}}}}
function zoom(k,c){{c=c||{{x:vb.x+vb.w/2,y:vb.y+vb.h/2}};vb.x=c.x-(c.x-vb.x)*k;vb.y=c.y-(c.y-vb.y)*k;vb.w*=k;vb.h*=k;set()}}
svg.addEventListener('wheel',e=>{{e.preventDefault();zoom(e.deltaY>0?1.2:1/1.2,pt(e))}},{{passive:false}});
let drag=null;svg.addEventListener('pointerdown',e=>{{drag={{p:pt(e)}};svg.classList.add('drag');svg.setPointerCapture(e.pointerId)}});
svg.addEventListener('pointerup',e=>{{drag=null;svg.classList.remove('drag')}});
svg.addEventListener('pointermove',e=>{{const p=pt(e);cur.textContent='x '+(p.x+MINX).toFixed(1)+', z '+(MAXZ-p.y).toFixed(1);if(drag){{vb.x-=p.x-drag.p.x;vb.y-=p.y-drag.p.y;set()}}}});
document.getElementById('zin').onclick=()=>zoom(1/1.4);document.getElementById('zout').onclick=()=>zoom(1.4);document.getElementById('fit').onclick=fit;
document.querySelectorAll('#bar input[type=checkbox]').forEach(cb=>cb.onchange=()=>{{const g=document.getElementById('L-'+cb.dataset.l);if(g)g.style.display=cb.checked?'':'none'}});
document.getElementById('L-anchor').style.display='none';
function show(el,e){{const ex=el.dataset.extra?JSON.parse(el.dataset.extra):{{}};let rows='<tr><td>x, z</td><td>'+el.dataset.x+', '+el.dataset.z+'</td></tr>';
if(el.dataset.y)rows+='<tr><td>floor</td><td>'+el.dataset.y+' m</td></tr>';if(el.dataset.a)rows+='<tr><td>along</td><td>'+el.dataset.a+' m into the cave</td></tr>';
for(const k in ex)rows+='<tr><td>'+k+'</td><td>'+String(ex[k]).replace(/</g,'&lt;')+'</td></tr>';
card.innerHTML='<div class="k">'+el.dataset.k+'</div><b>'+el.dataset.n.replace(/</g,'&lt;')+'</b><table>'+rows+'</table>';card.style.display='block';
card.style.left=Math.min(e.clientX+14,innerWidth-350)+'px';card.style.top=Math.min(e.clientY+14,innerHeight-card.offsetHeight-10)+'px'}}
svg.addEventListener('mousemove',e=>{{const el=e.target.closest('[data-n]');if(el&&!drag)show(el,e);else card.style.display='none'}});
svg.addEventListener('mouseleave',()=>card.style.display='none');
const all=[...document.querySelectorAll('.pin')];
document.getElementById('q').oninput=e=>{{const q=e.target.value.trim().toLowerCase();all.forEach(p=>p.classList.remove('hit'));if(q.length<2)return;
const m=all.filter(p=>p.dataset.n.toLowerCase().includes(q));m.forEach(p=>p.classList.add('hit'));
if(m.length){{const b=m[0].getBBox();vb.w=Math.max(60,vb.w>200?120:vb.w);vb.h=vb.w*svg.getBoundingClientRect().height/svg.getBoundingClientRect().width;vb.x=b.x-vb.w/2;vb.y=b.y-vb.h/2;set()}}}};
document.getElementById('runb').onclick=()=>document.getElementById('prog').classList.toggle('on');
document.querySelectorAll('#prog [data-go]').forEach(li=>li.onclick=()=>{{const [x,z]=li.dataset.go.split(',').map(Number);const r=svg.getBoundingClientRect();vb.w=90;vb.h=90*r.height/r.width;vb.x=(x-MINX)-vb.w/2;vb.y=(MAXZ-z)-vb.h/2;set();all.forEach(p=>p.classList.toggle('hit',p.dataset.x==x.toFixed(1)&&p.dataset.z==z.toFixed(1)))}});
addEventListener('resize',fit);fit();
</script></body></html>'''
os.makedirs(os.path.dirname(OUT), exist_ok=True)
open(OUT, 'w', encoding='utf-8').write(page)
print(OUT, f'{len(caves)} caves, {len(items)} items, {len(decks)} decks, {len(waters)} waters; floor {ymin:.1f}..{ymax:.1f}; '
      f'x {minx:.0f}..{maxx:.0f}, z {minz:.0f}..{maxz:.0f}; ' + ', '.join(f'{k} {v}' for k, v in counts.items()))
