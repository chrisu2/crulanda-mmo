"""Proposes (and with --apply writes) the per-build scales in EncounterSession.RoleScale.cs and the area builds' splash shares in
EncounterSession.Spread.cs, from the balance harness's CSVs on the fixed dummies (2026-10-13).

Targets for the level cap of 15 (settled by Chris 2026-10-13 02:07: "good til lvl 15 and will change as we progress to 30"):
- DPS 62-72 (Chris: "62-72 is good range"). By specialization (Chris: AoE "based on specializations"; the split PROPOSED): single-target
  damage builds 66-72 (moved to 69), area builds 62-67 (moved to 64); an area build's splash share tuned so it does 1.7-2.3 times against
  three what it does against one (moved to 2.0). Pet and spirit builds are measured with their pet (Chris: "pet+class ... 62-72 combined").
- CC, Support (and utility) 45-52, to 48.5; Tank 42-55, to 48.5; Healer at most 35 DPS, to 32; Healer HPS on the wounded dummy 40-50, to 45.
Builds in range are left alone. A build with a companion in BuildPetDamageScale gets the same factor on its pet.
Usage: python balance_retune.py [csv folder] [--apply] [--skip=cleric,shaman]."""
import csv, glob, os, re, statistics, sys

args = [a for a in sys.argv[1:] if not a.startswith('--')]; apply = '--apply' in sys.argv
skip = [a.split('=', 1)[1].split(',') for a in sys.argv[1:] if a.startswith('--skip=')]; skip = skip[0] if skip else []
folder = args[0] if args else r'D:\crulanda-work\outputs\balance'
ENC = os.path.join(r'D:\code\mmo', 'New Unity Project', 'Assets', 'Crulanda', 'Scripts', 'Encounter')
src, spread_src = os.path.join(ENC, 'EncounterSession.RoleScale.cs'), os.path.join(ENC, 'EncounterSession.Spread.cs')
ROLE = {'pet': 'dps', 'utility': 'support', 'control': 'cc'}
DPS_T = {'dps': (62, 72, 67), 'cc': (45, 52, 48.5), 'support': (45, 52, 48.5), 'tank': (42, 55, 48.5), 'healer': (0, 35, 32)}
ST_T, AOE_T, AOE_RATIO = (66, 72, 69), (62, 67, 64), (1.7, 2.3, 2.0)
AOE = {'class.warrior/dps', 'class.paladin/judicator', 'class.ranger/skirmisher', 'class.mage/combustion', 'class.shaman/stormcaller'}
HPS_T = (40, 50, 45)

single, three, heal, role = {}, {}, {}, {}
for f in glob.glob(os.path.join(folder, 'balance-*.csv')):
    if '_TABLE' in f: continue
    for r in csv.reader(open(f, encoding='utf-8')):
        if not r or r[0].startswith('#') or r[0] == 'class' or len(r) < 8: continue
        try: d, h = float(r[6]), float(r[7])
        except ValueError: continue
        k = r[0] + '/' + r[1]; role[k] = ROLE.get(r[2], r[2])
        if r[3] == 'single': single.setdefault(k, []).append(d)
        if r[3] == 'aoe3': three.setdefault(k, []).append(d)
        if r[3] == 'heal': heal.setdefault(k, []).append(h)

text = open(src, encoding='utf-8').read()
def table(name):
    m = re.search(name + r' = new Dictionary<string, float>\s*\{(.*?)\};', text, re.S)
    return {k: float(v) for k, v in re.findall(r'\{\s*"([^"]+)",\s*([0-9.]+)f\s*\}', m.group(1))}
dmg, pet, hs = table('BuildDamageScale'), table('BuildPetDamageScale'), table('BuildHealScale')
def skipped(k): return k.split('/')[0].replace('class.', '') in skip

changes = []   # (table, key, old, new)
for k in sorted(single):
    if skipped(k): continue
    v = statistics.mean(single[k]); r = role[k]; lo, hi, mid = DPS_T.get(r, (0, 1e9, 0)); old = dmg.get(k, 1.0)
    if r == 'dps': lo, hi, mid = AOE_T if k in AOE else ST_T
    line = '%-34s %-8s DPS %6.1f  scale %.2f' % (k, r + ('/area' if k in AOE else ''), v, old)
    if v > 0 and not (lo <= v <= hi):
        new = round(old * mid / v, 2); changes.append(('BuildDamageScale', k, old, new)); line += '  -> %.2f (to %.1f)' % (new, mid)
    if r == 'healer' and k in heal:
        h = statistics.mean(heal[k]); oh = hs.get(k, 1.0); line += ' | HPS %.1f heal scale %.2f' % (h, oh)
        if h > 0 and not (HPS_T[0] <= h <= HPS_T[1]):
            nh = round(oh * HPS_T[2] / h, 2); changes.append(('BuildHealScale', k, oh, nh)); line += ' -> %.2f (to %.0f)' % (nh, HPS_T[2])
    print(line)

# The area builds' splash: share x (target - 1) / (measured - 1); against three the ratio is about 1 + 2 x share for a fixed rotation.
spread_text = open(spread_src, encoding='utf-8').read(); spread_changes = []
for k in sorted(AOE):
    if skipped(k) or k not in single or k not in three: continue
    one, tri = statistics.mean(single[k]), statistics.mean(three[k]); ratio = tri / one if one > 0 else 0
    m = re.search(r'\{ "' + re.escape(k) + r'", new Spread\("[^"]+", ([0-9.]+)f', spread_text)
    if not m: continue
    share = float(m.group(1)); line = '%-34s against three x%.2f  share %.2f' % (k, ratio, share)
    if ratio > 1.01 and not (AOE_RATIO[0] <= ratio <= AOE_RATIO[1]):
        new = round(max(.1, min(1.0, share * (AOE_RATIO[2] - 1) / (ratio - 1))), 2); spread_changes.append((k, share, new)); line += '  -> %.2f' % new
    print(line)
print('%d scale changes, %d splash changes' % (len(changes), len(spread_changes)))

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
if apply and spread_changes:
    for k, old, new in spread_changes:
        spread_text = re.sub(r'(\{ "' + re.escape(k) + r'", new Spread\("[^"]+", )[0-9.]+f', lambda m: m.group(1) + fmt(new), spread_text)
    open(spread_src, 'w', encoding='utf-8', newline='').write(spread_text)
    print('written', spread_src)
