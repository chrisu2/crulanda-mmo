"""Merges the balance harness's per-set CSVs (D:\\crulanda-work\\outputs\\balance\\balance-<set>.csv) into one table over every class:
per class and build, DPS single-target and against three, HPS in the heal test (seeds averaged), each against its role's median, and
the role caps (Docs/BALANCE_REPORT.md: healers at most 50% of the DPS-role median, control and support 75%, 3 points of slack).
Writes BALANCE_ALL.md beside the CSVs and prints it. Usage: python balance_merge.py [folder] [out.md]."""
import csv, glob, os, statistics, sys

folder = sys.argv[1] if len(sys.argv) > 1 else r'D:\crulanda-work\outputs\balance'
out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(folder, 'BALANCE_ALL.md')
CAPS = {'healer': .50, 'control': .75, 'support': .75}
SLACK = .03

rows = {}   # (class, build, role) -> {scenario: [dps...], 'hps': [hps...]}
tanks = {}  # (class, build) -> lists of the tank test's figures ("# tank" lines: taken, self-heal, net, threat a second; refills)
files = sorted((f for f in glob.glob(os.path.join(folder, 'balance-*.csv')) if not os.path.basename(f).startswith(('balance-summary', 'balance-runs', 'balance-all'))), key=os.path.getmtime)   # not the sheets this writes
for f in files:
    if '_TABLE' in f: continue
    with open(f, encoding='utf-8') as h:
        for r in csv.reader(h):
            if r and r[0].startswith('# tank '):
                import re
                m = re.match(r'# tank (\S+) (\S+) \d+: taken ([\d.]+) self-heal ([\d.]+) net (-?[\d.]+) threat ([\d.]+) refills (\d+)', ','.join(r))
                if m:
                    k = (m.group(1).replace('class.', ''), m.group(2)); d = tanks.setdefault(k, {'taken': [], 'self': [], 'net': [], 'threat': [], 'refills': []})
                    for name, g in (('taken', 3), ('self', 4), ('net', 5), ('threat', 6), ('refills', 7)): d[name].append(float(m.group(g)))
                continue
            if not r or r[0].startswith('#') or r[0] == 'class': continue
            cls, build, role, scen = r[0].replace('class.', ''), r[1], r[2], r[3]
            try: dps, hps = float(r[6]), float(r[7])
            except ValueError: continue
            k = (cls, build, role); rows.setdefault(k, {})
            rows[k].setdefault(scen, []).append(dps); rows[k].setdefault('hps:' + scen, []).append(hps)

def mean(v): return statistics.mean(v) if v else 0.0
table = []
for (cls, build, role), d in rows.items():
    single = mean(d.get('single', [])); aoe = mean(d.get('aoe3', []))
    hps = max(mean(d.get('hps:heal', [])), mean(d.get('hps:single', [])))
    table.append(dict(cls=cls, build=build, role=role, single=single, aoe=aoe, hps=hps))
# Two CSVs to sort in a spreadsheet (Chris, 2026-10-14: "give me a balance sheet for all that is sortable. csv?", "well it is not
# sortable in a spreadsheet", "dps is mixed with single and aoe3. i need these seperate"): balance-summary.csv, one row a build with
# single-target DPS, DPS against three and HPS in their own columns and every number plain (the target as its two ends, blanks for
# none); balance-runs.csv, one row a build and seed with the same columns from that seed's runs.
def _csvs():
    import csv
    name = {'dps': 'DPS', 'tank': 'Tank', 'support': 'Support', 'healer': 'Healer', 'control': 'CC', 'pet': 'DPS (pet)', 'utility': 'Support (utility)'}
    target = {'dps': (62, 72), 'pet': (62, 72), 'control': (45, 52), 'support': (45, 52), 'utility': (45, 52), 'healer': (0, 35), 'tank': (42, 55)}
    out_rows = [['class', 'build', 'role', 'dps_single', 'dps_vs_3', 'vs3_over_single', 'hps', 'target_min', 'target_max', 'in_range']]
    for t in sorted(table, key=lambda t: (t['cls'], t['role'], t['build'])):
        lo, hi = target.get(t['role'], (0, 0))
        out_rows.append([t['cls'], t['build'], name.get(t['role'], t['role']), round(t['single'], 1), round(t['aoe'], 1), round(t['aoe'] / t['single'], 2) if t['single'] > 0 else '',
                         round(t['hps'], 1) if t['hps'] > 0 else '', lo, hi, 1 if (lo <= t['single'] <= hi or (lo == 0 and t['single'] <= hi)) else 0])
    with open(os.path.join(folder, 'balance-summary.csv'), 'w', encoding='utf-8', newline='') as h: csv.writer(h).writerows(out_rows)
    runs = {}
    for f in files:
        if '_TABLE' in f: continue
        with open(f, encoding='utf-8') as h:
            for r in csv.reader(h):
                if not r or r[0].startswith('#') or r[0] == 'class' or len(r) < 8: continue
                try: dps, hps = float(r[6]), float(r[7])
                except ValueError: continue
                k = (r[0].replace('class.', ''), r[1], name.get(r[2], r[2]), r[4]); d = runs.setdefault(k, {})
                d['dps_' + r[3]] = dps; d['hps_' + r[3]] = hps
    run_rows = [['class', 'build', 'role', 'seed', 'dps_single', 'dps_vs_3', 'hps_heal', 'hps_single']]
    for k in sorted(runs):
        d = runs[k]; run_rows.append(list(k) + [d.get('dps_single', ''), d.get('dps_aoe3', ''), d.get('hps_heal', '') or '', d.get('hps_single', '') or ''])
    with open(os.path.join(folder, 'balance-runs.csv'), 'w', encoding='utf-8', newline='') as h: csv.writer(h).writerows(run_rows)
    # And a workbook of both, numbers as numbers with filters (Chris: "balance runs still not sortable"); skipped without openpyxl.
    try:
        from openpyxl import Workbook
        from openpyxl.styles import Font
        wb = Workbook(); ws = wb.active
        for k, (title, rows_) in enumerate((('Summary', out_rows), ('Runs', run_rows))):
            if k: ws = wb.create_sheet()
            ws.title = title
            for r in rows_: ws.append([None if c == '' else c for c in r])
            for c in ws[1]: c.font = Font(bold=True)
            ws.freeze_panes = 'A2'; ws.auto_filter.ref = ws.dimensions
            for col in ws.columns: ws.column_dimensions[col[0].column_letter].width = max(10, min(28, max(len(str(c.value or '')) for c in col) + 2))
        wb.save(os.path.join(folder, 'balance.xlsx'))
    except ImportError: pass
_csvs()
dps_median = statistics.median([t['single'] for t in table if t['role'] == 'dps']) if any(t['role'] == 'dps' for t in table) else 0
medians = {}
for role in set(t['role'] for t in table):
    medians[role] = statistics.median([t['single'] for t in table if t['role'] == role])
heal_median = statistics.median([t['hps'] for t in table if t['role'] == 'healer' and t['hps'] > 0]) if any(t['role'] == 'healer' and t['hps'] > 0 for t in table) else 0
# The role each build plays, as Chris names them (2026-10-13: "designate it as tank/dps/support/healer/CC"), and its target on the fixed
# dummies, for the level cap of 15. DPS 62-72 is Chris's (2026-10-13 00:37); the others, from the role caps on a 67 DPS middle, he
# settled at 02:07: "the #s you proposed are good til lvl 15 and will change as we progress to 30".
NAME = {'dps': 'DPS', 'tank': 'Tank', 'support': 'Support', 'healer': 'Healer', 'control': 'CC', 'pet': 'DPS (pet)', 'utility': 'Support (utility)'}
TARGET = {'dps': (62, 72), 'pet': (62, 72), 'control': (45, 52), 'support': (45, 52), 'utility': (45, 52), 'healer': (0, 35), 'tank': (42, 55)}
# Area specialists (EncounterSession.BuildSpread; Chris 2026-10-13: AoE "based on specializations", not the same for every class):
# against three they should do at least 1.7 times what they do against one; every other damage build at most 1.25 times (PROPOSED).
AOE = {'warrior/dps', 'paladin/judicator', 'ranger/skirmisher', 'mage/combustion', 'shaman/stormcaller'}
AOE_MIN, ST_MAX = 1.7, 1.25
HPS_T = (40, 50)   # healer HPS on the wounded dummy (Chris, 02:07, for level 15)
def status(t):
    lo, hi = TARGET.get(t['role'], (0, 1e9)); v = t['single']
    out = 'ok' if lo <= v <= hi else ('**HIGH**' if v > hi else '**LOW**')
    if t['role'] in ('dps', 'pet') and t['single'] > 0:
        ratio = t['aoe'] / t['single']; aoe = (t['cls'] + '/' + t['build']) in AOE
        if aoe and ratio < AOE_MIN: out += ' **AoE LOW** (x%.2f)' % ratio
        # Chris, 2026-10-13: "they are limited on single targets if they are an aoe build": an area build (a splash build, or one whose
        # own spells do over x1.25 against three) is held to 62-67 against one.
        if (aoe or ratio > ST_MAX) and t['single'] > 67.5: out += ' **area build over 67 single**'
    if t['role'] == 'healer':
        if t['hps'] <= 0: out += ' **NO HEALING**'
        elif not (HPS_T[0] <= t['hps'] <= HPS_T[1]): out += ' heal **%s**' % ('HIGH' if t['hps'] > HPS_T[1] else 'LOW')
    return out
classes = sorted(set(t['cls'] for t in table))
lines = ['# Balance, every class and build (' + ', '.join(classes) + ')', '',
         'Fixed dummies (level 15, no armour, no blows), 180 s fights, three seeds averaged. DPS = single target; vs 3 = three dummies;',
         'HPS = healing a second on the wounded dummy. Targets for level 15 (single-target DPS), settled by Chris: DPS 62-72; CC and Support 45-52, Healer at most 35,',
         "Tank 42-55. Healer HPS 40-50 (the healers' median is %.1f; the wounded dummy loses 120 a second, the most any healer can show)." % heal_median, '',
         '| class | build | role | DPS | DPS vs 3 | vs 3 / 1 | HPS | DPS target | status |', '|---|---|---|---|---|---|---|---|---|']
order = {'tank': 0, 'dps': 1, 'pet': 1, 'control': 2, 'support': 3, 'utility': 3, 'healer': 4}
counts = {}
for t in sorted(table, key=lambda t: (t['cls'], order.get(t['role'], 9), t['build'])):
    lo, hi = TARGET.get(t['role'], (0, 0)); st = status(t); counts[st.startswith('ok')] = counts.get(st.startswith('ok'), 0) + 1
    tgt = ('at most %d' % hi) if lo == 0 else '%d-%d' % (lo, hi)
    kind = (' (AoE)' if (t['cls'] + '/' + t['build']) in AOE else ' (area spells)' if t['single'] > 0 and t['aoe'] / t['single'] > ST_MAX else ' (single)') if t['role'] in ('dps', 'pet') else ''
    lines.append('| %s | %s | %s%s | %.1f | %.1f | %s | %s | %s | %s |' % (t['cls'], t['build'], NAME.get(t['role'], t['role']), kind, t['single'], t['aoe'],
                 ('x%.2f' % (t['aoe'] / t['single'])) if t['single'] > 0 else '-',
                 ('%.1f' % t['hps']) if t['hps'] > 0 else '-', tgt, st))
if tanks:
    lines += ['', "## Tanks (three dummies each striking a tough level-15 mob's blow every 2 s; lower net intake is sturdier)", '',
              '| rank | class | build | taken /s | self-heal /s | net /s | threat /s | near-deaths in 180 s |', '|---|---|---|---|---|---|---|---|']
    for i, ((c, b), d) in enumerate(sorted(tanks.items(), key=lambda kv: mean(kv[1]['net']))):
        lines.append('| %d | %s | %s | %.1f | %.1f | %.1f | %.1f | %.1f |' % (i + 1, c, b, mean(d['taken']), mean(d['self']), mean(d['net']), mean(d['threat']), mean(d['refills'])))
lines += ['', 'In range: %d of %d builds.' % (counts.get(True, 0), len(table)),
          'Role medians (single DPS): ' + ', '.join('%s %.1f' % (NAME.get(r, r), m) for r, m in sorted(medians.items())) + '; DPS median %.1f.' % dps_median,
          'Files: ' + ', '.join(os.path.basename(f) for f in files if '_TABLE' not in f)]
open(out, 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
