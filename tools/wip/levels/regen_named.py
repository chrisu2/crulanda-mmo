"""Round 29, after rescale.py: every named item's weapon damage, armour and value are the generated gear's at its new level and
quality (Items.cs Generate: the tests hold named gear to that curve), and its stat points are scaled to the budget at that level
(LootDataTests.Named_gear_stat_budgets_hold: per * k, k = power / 4; per 4 for uncommon, 5 rare, 6 epic or a signature piece, 7
legendary). A luck charm keeps under its budget. Run after the levels move.

    python regen_named.py            report only
    python regen_named.py --write    write
"""
import io, json, os, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
ITEMS = os.path.join(ROOT, 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent', 'Items')
WRITE = '--write' in sys.argv
FILES = ['oakhaven', 'khaven', 'peaks', 'ashrim', 'verdant', 'world', 'adit']
STATS = ['stamina', 'strength', 'agility', 'intellect', 'spirit']

def mult(q): return .5 if q <= 0 else 1 if q == 1 else 1.35 if q == 2 else 1.7 if q == 3 else 2.1 if q == 4 else 2.5
def curve(slot, level, q):
    """weaponDamage, armor, value for named gear of this level and quality (the generated piece at level + 1)."""
    L = level + 1; power = L * mult(q); weapon = slot == 'mainhand'; jewel = slot == 'neck'
    dmg = max(1, round(3 + power * 1.6)) if weapon else 0
    arm = 0 if weapon or jewel else max(1, round(power * (2.2 if slot in ('chest', 'legs') else 2.6 if slot == 'offhand' else 1.4)))
    value = max(1, round(L * (1 + q) * (1.4 if weapon else 1)))
    return dmg, arm, value, power
def budget(q, power, signature): per = 7 if q == 5 else 4 if q == 2 else 6 if (q == 4 or signature) else 5; return round(per * max(1, power / 4))
def scale(item, target):
    cur = [item.get(s, 0) for s in STATS]; total = sum(cur)
    if total == 0 or target <= 0: return
    raw = [c * target / total for c in cur]; new = [int(x) for x in raw]; short = target - sum(new)
    for i in sorted(range(5), key=lambda i: raw[i] - new[i], reverse=True)[:short]: new[i] += 1
    for s, v in zip(STATS, new):
        if v > 0: item[s] = v
        elif s in item: del item[s]

report = []
for name in FILES:
    p = os.path.join(ITEMS, 'loot.' + name + '.json')
    if not os.path.exists(p): continue
    d = json.load(io.open(p, encoding='utf-8'))
    signature = set(k['item'] for dr in d.get('drops', []) for g in dr.get('groups', []) if g.get('signature') for k in g.get('pick', []))
    charms = set(g['id'] for g in d.get('gear', []) if any(e.get('kind') == 'luck' for e in (g.get('effects') or [])))
    for it in d.get('items', []):
        if it.get('kind') != 'gear' or not it['id'].startswith('loot.'): continue
        q = it.get('quality', 1); dmg, arm, value, power = curve(it['slot'], it['level'], q)
        before = (it.get('weaponDamage', 0), it.get('armor', 0), it.get('value'), sum(it.get(s, 0) for s in STATS))
        if dmg: it['weaponDamage'] = dmg
        elif 'weaponDamage' in it: del it['weaponDamage']
        if arm: it['armor'] = arm
        elif 'armor' in it: del it['armor']
        it['value'] = value
        b = budget(q, power, it['id'] in signature); scale(it, b if it['id'] not in charms else min(b, before[3]) if before[3] else b)
        after = (it.get('weaponDamage', 0), it.get('armor', 0), it['value'], sum(it.get(s, 0) for s in STATS))
        if before != after: report.append('%s L%d q%d: dmg/arm/value/stats %s -> %s (budget %d)' % (it['id'], it['level'], q, before, after, b))
    if WRITE: io.open(p, 'w', encoding='utf-8', newline='\n').write(json.dumps(d, indent=2, ensure_ascii=False) + '\n')
print('\n'.join(report)); print(len(report), 'items', 'WRITTEN' if WRITE else 'report only')
