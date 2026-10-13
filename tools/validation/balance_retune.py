"""Proposes (and with --apply writes) the per-build damage scales in EncounterSession.RoleScale.cs from the balance harness's CSVs on the
fixed dummies. Chris, 2026-10-13: "62-72 is good range" for single-target DPS. A DPS build outside 62-72 is moved to 67; a healer over
50% of the DPS median is moved to 45%, a control or support build over 75% to 70% (the role caps, Docs/BALANCE_REPORT.md). Builds in range
are left alone. A build with a companion in BuildPetDamageScale gets the same factor on its pet (the harness measures both together).
Usage: python balance_retune.py [csv folder] [--apply]."""
import csv, glob, os, re, statistics, sys

args = [a for a in sys.argv[1:] if not a.startswith('--')]; apply = '--apply' in sys.argv
folder = args[0] if args else r'D:\crulanda-work\outputs\balance'
src = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\Encounter\EncounterSession.RoleScale.cs'
LO, HI, MID = 62.0, 72.0, 67.0
CAPS = {'healer': (.50, .45), 'control': (.75, .70), 'support': (.75, .70)}
SLACK = .03

single = {}; role = {}
for f in glob.glob(os.path.join(folder, 'balance-*.csv')):
    if '_TABLE' in f: continue
    for r in csv.reader(open(f, encoding='utf-8')):
        if not r or r[0].startswith('#') or r[0] == 'class' or len(r) < 7 or r[3] != 'single': continue
        try: v = float(r[6])
        except ValueError: continue
        k = r[0] + '/' + r[1]; single.setdefault(k, []).append(v); role[k] = r[2]
dps = {k: statistics.mean(v) for k, v in single.items()}
median = statistics.median([v for k, v in dps.items() if role[k] == 'dps'])

text = open(src, encoding='utf-8').read()
def table(name):
    m = re.search(name + r' = new Dictionary<string, float>\s*\{(.*?)\};', text, re.S)
    return {k: float(v) for k, v in re.findall(r'\{\s*"([^"]+)",\s*([0-9.]+)f\s*\}', m.group(1))}
dmg, pet = table('BuildDamageScale'), table('BuildPetDamageScale')

changes = []
for k in sorted(dps):
    v, r = dps[k], role[k]; old = dmg.get(k, 1.0); want = None
    if r == 'dps' and not (LO <= v <= HI): want = MID
    elif r in CAPS and v > (CAPS[r][0] + SLACK) * median: want = CAPS[r][1] * median
    line = '%-34s %-8s %6.1f  scale %.2f' % (k, r, v, old)
    if want is None: print(line + '  ok'); continue
    new = round(old * want / v, 2); changes.append((k, old, new))
    print(line + '  -> %.2f (to %.1f)' % (new, want))
print('DPS-role median %.1f; %d builds to change' % (median, len(changes)))

if apply and changes:
    for k, old, new in changes:
        pat = r'\{\s*"' + re.escape(k) + r'",\s*[0-9.]+f\s*\}'
        def fmt(x): return ('%.2f' % x).rstrip('0').rstrip('.') + 'f'
        if re.search(pat, text):
            text = re.sub(pat, '{ "%s", %s }' % (k, fmt(new)), text)   # every table the build is in (its own blows and its pet's)
            if k in pet: text = re.sub(r'(BuildPetDamageScale = new Dictionary<string, float> \{.*?)\{\s*"' + re.escape(k) + r'",\s*[0-9.]+f\s*\}',
                                       lambda m: m.group(1) + '{ "%s", %s }' % (k, fmt(round(pet[k] * new / old, 2))), text, flags=re.S)
        else:
            text = re.sub(r'(BuildDamageScale = new Dictionary<string, float>\s*\{.*?)(\n\s*\};)',
                          lambda m: m.group(1) + '\n            { "%s", %s },   // 2026-10-13 fixed-dummy retune (62-72)' % (k, fmt(new)) + m.group(2), text, count=1, flags=re.S)
    open(src, 'w', encoding='utf-8', newline='').write(text)
    print('written', src)
