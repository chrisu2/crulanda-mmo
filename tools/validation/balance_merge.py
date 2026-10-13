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
files = sorted(glob.glob(os.path.join(folder, 'balance-*.csv')), key=os.path.getmtime)
for f in files:
    if '_TABLE' in f: continue
    with open(f, encoding='utf-8') as h:
        for r in csv.reader(h):
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
dps_median = statistics.median([t['single'] for t in table if t['role'] == 'dps']) if any(t['role'] == 'dps' for t in table) else 0
medians = {}
for role in set(t['role'] for t in table):
    medians[role] = statistics.median([t['single'] for t in table if t['role'] == role])
lines = ['# Balance, all classes (' + ', '.join(sorted(set(t["cls"] for t in table))) + ')', '',
         'Single-target DPS, DPS against three, HPS in the heal test; seeds averaged. DPS-role median: %.1f.' % dps_median,
         'Caps on single-target DPS as a share of the DPS median: healer 50%, control and support 75% (3 points of slack).', '',
         '| class | build | role | DPS single | DPS vs 3 | HPS | vs role median | vs DPS median | flag |', '|---|---|---|---|---|---|---|---|---|']
violations = []
for t in sorted(table, key=lambda t: (t['role'], t['cls'], t['build'])):
    med = medians.get(t['role'], 0); vs_role = (t['single'] / med - 1) if med else 0
    share = t['single'] / dps_median if dps_median else 0
    flag = ''
    if t['role'] in CAPS and share > CAPS[t['role']] + SLACK: flag = '**OVER CAP** (%.0f%% of DPS median)' % (share * 100); violations.append((t, share))
    elif abs(vs_role) > .15: flag = '**OUTLIER**'
    if t['role'] == 'healer' and t['hps'] <= 0: flag = (flag + ' ' if flag else '') + '**NO HEALING**'
    lines.append('| %s | %s | %s | %.1f | %.1f | %.1f | %+.0f%% | %.0f%% | %s |' % (t['cls'], t['build'], t['role'], t['single'], t['aoe'], t['hps'], vs_role * 100, share * 100, flag))
lines += ['', 'Role medians (single DPS): ' + ', '.join('%s %.1f' % (r, m) for r, m in sorted(medians.items())),
          'Cap violations: ' + (', '.join('%s/%s %.0f%%' % (t['cls'], t['build'], s * 100) for t, s in violations) if violations else 'none'),
          'Files: ' + ', '.join(os.path.basename(f) for f in files if '_TABLE' not in f)]
open(out, 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
print('\n'.join(lines))
