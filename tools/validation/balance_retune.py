"""Proposes (and with --apply writes) the per-build scales in EncounterSession.RoleScale.cs from the balance harness's CSVs on the fixed
dummies (2026-10-13). Targets, single-target DPS: DPS (and pet builds) 62-72 (Chris: "62-72 is good range"), to 67; CC, Support (and
utility) 45-52, to 48.5; Tank 42-55, to 48.5; Healer at most 35, to 32. Healer HPS on the wounded dummy 40-50, to 45 (BuildHealScale).
All but Chris's DPS range are PROPOSED. Builds in range are left alone. A build with a companion in BuildPetDamageScale gets the same
factor on its pet (the harness measures both together). Usage: python balance_retune.py [csv folder] [--apply] [--skip=cleric,shaman]."""
import csv, glob, os, re, statistics, sys

args = [a for a in sys.argv[1:] if not a.startswith('--')]; apply = '--apply' in sys.argv
skip = [a.split('=', 1)[1].split(',') for a in sys.argv[1:] if a.startswith('--skip=')]; skip = skip[0] if skip else []   # --skip=cleric,shaman: classes left as they are
folder = args[0] if args else r'D:\crulanda-work\outputs\balance'
src = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Scripts\Encounter\EncounterSession.RoleScale.cs'
ROLE = {'pet': 'dps', 'utility': 'support', 'control': 'cc'}
DPS_T = {'dps': (62, 72, 67), 'cc': (45, 52, 48.5), 'support': (45, 52, 48.5), 'tank': (42, 55, 48.5), 'healer': (0, 35, 32)}
HPS_T = (40, 50, 45)

single, heal, role = {}, {}, {}
for f in glob.glob(os.path.join(folder, 'balance-*.csv')):
    if '_TABLE' in f: continue
    for r in csv.reader(open(f, encoding='utf-8')):
        if not r or r[0].startswith('#') or r[0] == 'class' or len(r) < 8: continue
        try: d, h = float(r[6]), float(r[7])
        except ValueError: continue
        k = r[0] + '/' + r[1]; role[k] = ROLE.get(r[2], r[2])
        if r[3] == 'single': single.setdefault(k, []).append(d)
        if r[3] == 'heal': heal.setdefault(k, []).append(h)

text = open(src, encoding='utf-8').read()
def table(name):
    m = re.search(name + r' = new Dictionary<string, float>\s*\{(.*?)\};', text, re.S)
    return {k: float(v) for k, v in re.findall(r'\{\s*"([^"]+)",\s*([0-9.]+)f\s*\}', m.group(1))}
dmg, pet, hs = table('BuildDamageScale'), table('BuildPetDamageScale'), table('BuildHealScale')

changes = []   # (table, key, old, new)
for k in sorted(single):
    if k.split('/')[0].replace('class.', '') in skip: continue
    v = statistics.mean(single[k]); r = role[k]; lo, hi, mid = DPS_T.get(r, (0, 1e9, 0)); old = dmg.get(k, 1.0)
    line = '%-34s %-8s DPS %6.1f  scale %.2f' % (k, r, v, old)
    if v > 0 and not (lo <= v <= hi):
        new = round(old * mid / v, 2); changes.append(('BuildDamageScale', k, old, new)); line += '  -> %.2f (to %.1f)' % (new, mid)
    if r == 'healer' and k in heal:
        h = statistics.mean(heal[k]); oh = hs.get(k, 1.0); line += ' | HPS %.1f heal scale %.2f' % (h, oh)
        if h > 0 and not (HPS_T[0] <= h <= HPS_T[1]):
            nh = round(oh * HPS_T[2] / h, 2); changes.append(('BuildHealScale', k, oh, nh)); line += ' -> %.2f (to %.0f)' % (nh, HPS_T[2])
    print(line)
print('%d changes' % len(changes))

def fmt(x): return ('%.2f' % x).rstrip('0').rstrip('.') + 'f'
if apply and changes:
    for tab, k, old, new in changes:
        m = re.search('(' + tab + r' = new Dictionary<string, float>\s*\{)(.*?)(\n\s*\};)', text, re.S)
        body = m.group(2); pat = r'\{\s*"' + re.escape(k) + r'",\s*[0-9.]+f\s*\}'
        if re.search(pat, body): body = re.sub(pat, '{ "%s", %s }' % (k, fmt(new)), body)
        else: body = body.rstrip() + '\n            { "%s", %s },   // 2026-10-13 fixed-dummy retune' % (k, fmt(new))
        text = text[:m.start(2)] + body + text[m.end(2):]
        if tab == 'BuildDamageScale' and k in pet:
            m = re.search(r'(BuildPetDamageScale = new Dictionary<string, float> \{)(.*?)(\};)', text, re.S)
            body = re.sub(r'\{\s*"' + re.escape(k) + r'",\s*[0-9.]+f\s*\}', '{ "%s", %s }' % (k, fmt(round(pet[k] * new / old, 2))), m.group(2))
            text = text[:m.start(2)] + body + text[m.end(2):]
    open(src, 'w', encoding='utf-8', newline='').write(text)
    print('written', src)
