"""Round 29: the level cap 13 -> 15 and the zones' bands stretched (Chris, 2026-10-07: Oakhaven 1-5, Khaven 5-9, Peaks 9-12, Ash
Rim 12-15, Verdant Shore 13-15; "difficult and slow leveling, grind it out"; the Shore all elite, the Rim solo-friendly).

What it moves, and how:
- a zone's band (levelMin/levelMax) to the new one;
- its camps: the ordinary camps are ranked by their distance from the zone's middle and spread over the band, the nearest lowest,
  each a level or two wide; an elite stands a level over the ordinary camps near it; a `harder` camp (a cave's) runs past the
  band's top by its old margin, halved;
- the quests' `level` and `minLevel`, the bounties', the zone loot lists' levelMin/levelMax and the items' `level` by the old
  level -> new level map of their zone (the world's lists by the global map, old band -> new band, kept monotone);
- the code: LevelCap, the XP curve (XpToNext: 200+90(l-1) -> 400+170(l-1)), SimRoster.Homes, GearLooks.BandOf, the wardrobe's
  SetLevels; and the tests that name a 13.

    python rescale.py            report only
    python rescale.py --write    write it all
"""
import io, json, math, os, re, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
A = os.path.join(ROOT, 'New Unity Project', 'Assets', 'Crulanda')
C = os.path.join(A, 'EncounterContent')
WRITE = '--write' in sys.argv

OLD = {'zone.oakhaven': (1, 2), 'zone.khaven': (3, 5), 'zone.peaks': (6, 8), 'zone.ashrim': (9, 10), 'zone.verdant': (11, 13)}
NEW = {'zone.oakhaven': (1, 5), 'zone.khaven': (5, 9), 'zone.peaks': (9, 12), 'zone.ashrim': (12, 15), 'zone.verdant': (13, 15)}
FILE_ZONE = {'oakhaven': 'zone.oakhaven', 'khaven': 'zone.khaven', 'peaks': 'zone.peaks', 'ashrim': 'zone.ashrim', 'verdant': 'zone.verdant'}
CAP_OLD, CAP_NEW = 13, 15
report = []
def say(*a): report.append(' '.join(str(x) for x in a))

def zone_map(zone, old, harder=False):
    """Old level -> new, within a zone: linear over the band with its top level kept for the elites (ordinary content runs to
    nhi-1), past the band's ends by the margin halved (rounded up); a harder camp (a cave's) one over that, up to the cap."""
    lo, hi = OLD[zone]; nlo, nhi = NEW[zone]; top = max(nlo, nhi - 1)
    if old >= CAP_OLD: return CAP_NEW   # a thing of the old cap is a thing of the new one (the Shore's elites and their drops)
    if old < lo: v = max(1, nlo - math.ceil((lo - old) / 2))
    elif old > hi: v = top + math.ceil((old - hi) / 2)
    else:
        f = 0 if hi == lo else (old - lo) / (hi - lo); v = int(round(nlo + f * (top - nlo)))
    if harder: v += 1
    return min(CAP_NEW, v)
GLOBAL = {}
for z in OLD:
    for l in range(OLD[z][0], OLD[z][1] + 1): GLOBAL.setdefault(l, []).append(zone_map(z, l))
_prev = 0
for l in range(1, CAP_OLD + 1):
    v = max(GLOBAL.get(l, [_prev or 1])); v = max(v, _prev); GLOBAL[l] = v; _prev = v
def global_map(old): return CAP_NEW if old >= CAP_OLD else GLOBAL[max(1, min(CAP_OLD, old))]

def rj(p): return json.load(io.open(p, encoding='utf-8'))
def wj(p, d):
    if not WRITE: return
    io.open(p, 'w', encoding='utf-8', newline='\n').write(json.dumps(d, indent=2, ensure_ascii=False) + '\n')

def zone_file(name):
    p = os.path.join(C, 'Zones', name + '.json'); z = rj(p); zid = z['id']; nlo, nhi = NEW[zid]
    say('==', name, z['levelMin'], '-', z['levelMax'], '->', nlo, '-', nhi)
    z['levelMin'], z['levelMax'] = nlo, nhi
    camps = [c for c in z.get('camps', []) if c]
    centre = (z['spawns']['player']['x'], z['spawns']['player']['y']) if 'spawns' in z else (0, 0)
    def dist(c): return math.hypot(c['center']['x'] - centre[0], c['center']['y'] - centre[1])
    ordinary = [c for c in camps if not c.get('elite') and not c.get('harder')]
    ordinary.sort(key=dist)
    span = nhi - nlo   # the ordinary camps fill nlo .. nhi-1 (the top level is the elites' and the harder camps')
    top = max(nlo, nhi - 1)
    for i, c in enumerate(ordinary):
        f = i / max(1, len(ordinary) - 1); lvl = int(round(nlo + f * (top - nlo)))
        wide = c['levelMax'] > c['levelMin']
        old = (c['levelMin'], c['levelMax']); c['levelMin'] = lvl; c['levelMax'] = min(top, lvl + (1 if wide else 0))
        say('  ', c['name'], old, '->', (c['levelMin'], c['levelMax']), '%.0f m' % dist(c))
    for c in camps:
        if c.get('harder'):
            old = (c['levelMin'], c['levelMax']); c['levelMin'] = zone_map(zid, old[0], True); c['levelMax'] = zone_map(zid, old[1], True)
            say('  harder', c['name'], old, '->', (c['levelMin'], c['levelMax']))
        elif c.get('elite'):
            near = [o for o in ordinary if math.hypot(o['center']['x'] - c['center']['x'], o['center']['y'] - c['center']['y']) < 60]
            base = max([o['levelMax'] for o in near], default=top)
            old = (c['levelMin'], c['levelMax']); c['levelMin'] = c['levelMax'] = min(nhi, base + 1)
            say('  elite', c['name'], old, '->', (c['levelMin'], c['levelMax']))
    if zid == 'zone.verdant':
        z['groupZone'] = True   # every mob elite-strength (Chris: "all epic mobs, group required"); EncounterSession.SpawnCamps reads it
        say('   group zone')
    wj(p, z)
    return zid

def walk_levels(obj, fn, path=''):
    """Every `level`, `minLevel`, `levelMin` and `levelMax` under obj, through fn(old) -> new."""
    if isinstance(obj, dict):
        for k, v in list(obj.items()):
            if k in ('level', 'minLevel', 'levelMin', 'levelMax') and isinstance(v, int):
                nv = fn(v)
                if nv != v: obj[k] = nv; say('   ', path + '.' + k, v, '->', nv)
            else: walk_levels(v, fn, path + '.' + str(obj.get('id', k)) if k in ('id',) or isinstance(v, (dict, list)) else path)
    elif isinstance(obj, list):
        for i, v in enumerate(obj): walk_levels(v, fn, path)

def quests():
    for name, zid in FILE_ZONE.items():
        p = os.path.join(C, 'Quests', name + '.json'); d = rj(p); say('== quests', name); walk_levels(d, lambda o, z=zid: zone_map(z, o)); wj(p, d)
    p = os.path.join(C, 'Quests', 'bounties.json'); d = rj(p); say('== bounties')
    def bounty(o): return global_map(o)
    if isinstance(d, dict) and 'bounties' in d:
        for b in d['bounties']:
            z = b.get('zone'); walk_levels(b, (lambda o, z=z: zone_map(z, o)) if z in NEW else bounty, b.get('id', '?'))
    else: walk_levels(d, bounty)
    wj(p, d)

def items():
    for name, zid in FILE_ZONE.items():
        p = os.path.join(C, 'Items', 'loot.' + name + '.json'); d = rj(p); say('== loot', name); walk_levels(d, lambda o, z=zid: zone_map(z, o)); wj(p, d)
    p = os.path.join(C, 'Items', 'loot.world.json'); d = rj(p); say('== loot world'); walk_levels(d, global_map)
    for g in d.get('gear', []):   # the world lists' sources name their level band: "world:1-5"
        src = g.get('source', '')
        if src.startswith('world:'):
            a, b = src[6:].split('-'); g['source'] = 'world:%d-%d' % (global_map(int(a)), global_map(int(b))); say('   ', g['id'], src, '->', g['source'])
    wj(p, d)
    p = os.path.join(C, 'Items', 'items.json'); d = rj(p); say('== items')
    for it in d.get('items', d if isinstance(d, list) else []):
        if 'level' not in it: continue
        zid = None
        for name, z in FILE_ZONE.items():
            if name in it['id'] or ('zone.' + name) in it.get('canonStatus', ''): zid = z   # by id or an explicit zone, never by a word of the description
        old = it['level']; it['level'] = zone_map(zid, old) if zid else global_map(old)
        if it['level'] != old: say('   ', it['id'], old, '->', it['level'], zid or 'global')
    wj(p, d)

def code():
    def sub(rel, pairs):
        p = os.path.join(A, rel); s = io.open(p, encoding='utf-8', newline='').read()
        for a, b in pairs:
            if b in s: continue
            assert s.count(a) == 1, (rel, s.count(a), a[:60]); s = s.replace(a, b); say('   ', rel, '|', a[:50].strip())
        if WRITE: io.open(p, 'w', encoding='utf-8', newline='').write(s)
    say('== code')
    sub('Scripts/Encounter/EncounterProgress.cs', [
        ('public const int LevelCap = 13;', 'public const int LevelCap = 15;   // 15 now, 30 in the end (Chris, 2026-10-07; ROADMAP: Phase 9)'),
        ('public static int XpToNext(int level) { return 200 + 90 * (Math.Max(1, level) - 1); }',
         'public static int XpToNext(int level) { return 400 + 170 * (Math.Max(1, level) - 1); }   // round 29: "difficult and slow leveling, grind it out" (was 200 + 90 a level)'),
    ])
    sub('Scripts/Encounter/SimAdventurers.cs', [
        ('("zone.oakhaven", "Oakhaven folk", 1, 5), ("zone.khaven", "Khaven folk", 4, 8), ("zone.peaks", "Peaks folk", 7, 10), ("zone.ashrim", "Rim folk", 9, 12), ("zone.verdant", "Shore folk", 11, 13) };',
         '("zone.oakhaven", "Oakhaven folk", 1, 5), ("zone.khaven", "Khaven folk", 5, 9), ("zone.peaks", "Peaks folk", 9, 12), ("zone.ashrim", "Rim folk", 12, 15), ("zone.verdant", "Shore folk", 13, 15) };   // round 29 bands'),
    ])
    sub('Scripts/Encounter/GearLooks.cs', [
        ('public static int BandOf(int level) { return level <= 2 ? 0 : level <= 5 ? 1 : level <= 8 ? 2 : level <= 10 ? 3 : 4; }',
         'public static int BandOf(int level) { return level <= 5 ? 0 : level <= 9 ? 1 : level <= 12 ? 2 : level <= 14 ? 3 : 4; }   // round 29 bands (the Rim and the Shore share the cap: 15 is the Shore\'s)'),
    ])
    sub('Scripts/Encounter/WardrobeCapture.cs', [
        ('static readonly int[] SetLevels = { 2, 5, 8, 10, 13 };', 'static readonly int[] SetLevels = { 3, 7, 10, 13, 15 };'),
        ('full generated kits at levels 2, 5, 8, 10 and 13', 'full generated kits at levels 3, 7, 10, 13 and 15'),
        ('a rare level-13 kit standing', 'a rare level-15 kit standing'),
    ])
    sub('Tests/EditMode/GearVisualTests.cs', [('foreach (var (q, level) in new[] { (1, 1), (3, 9), (4, 13) })', 'foreach (var (q, level) in new[] { (1, 1), (3, 10), (4, 15) })')])
    sub('Tests/EditMode/LootRollTests.cs', [('Rates(Mob("verdant", "Withered Keeper", false, 13), n / 4)', 'Rates(Mob("verdant", "Withered Keeper", false, 15), n / 4)')])

if __name__ == '__main__':
    say('global map:', ' '.join('%d->%d' % (l, GLOBAL[l]) for l in range(1, CAP_OLD + 1)))
    for name in FILE_ZONE: zone_file(name)
    quests(); items(); code()
    # The breadcrumb trail (Chris, 2026-10-07): each zone's last main quest, turned in in the next zone, at the top of its band.
    say('== breadcrumbs (level, minLevel -> next zone)')
    for name, qid in (('oakhaven', 'main.oakhaven.3'), ('khaven', 'main.khaven.3'), ('peaks', 'main.peaks.3'), ('ashrim', 'main.ashrim.5')):
        d = rj(os.path.join(C, 'Quests', name + '.json')); q = next(x for x in d['quests'] if x['id'] == qid); zid = FILE_ZONE[name]
        say('   ', qid, zone_map(zid, q.get('level', 1)), zone_map(zid, q.get('minLevel', q.get('level', 1))), '->', q.get('turnIn'), '(band top', NEW[zid][1] - 1, ')')
    print('\n'.join(report)); print('WRITTEN' if WRITE else 'report only')
