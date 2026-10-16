"""Hallow's Creek's loot (Docs/DUNGEON2_DESIGN.md section 2's named pieces; levels 15-20): every elite's signature list, its pieces named gear
on the generated curve (LootDataTests: armour, damage and value as generated gear's at level + 1, the stat budget of a signature piece),
written to New Unity Project/Assets/Crulanda/EncounterContent/Items/loot.hallows.json. All GAME-ONLY. A rare (the Old Ram, the Grey Carter,
the Lamplighter) has one piece. The class sets stay the Adit's tier (the design names no more). Named gear stops at level 19 (the curve's
level + 1 is clamped at the cap, 20), so the last bosses' pieces are level 19.

    python hallows_loot.py       writes the file and prints the new ids (for Tests/EditMode/LootIdBaseline.txt)
"""
import io, json, os
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, '..', '..', '..', 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent', 'Items', 'loot.hallows.json')
F = np.float32
def rnd(x): return int(round(float(x)))     # Mathf.RoundToInt: to even on a half, of the float
def curve(slot, level, q=3):
    c = level + 1; power = F(c) * F({2: 1.35, 3: 1.7, 4: 2.1, 5: 2.5}[q])
    d = {}
    if slot == 'mainhand': d['weaponDamage'] = max(1, rnd(F(3) + power * F(1.6)))
    elif slot != 'neck': d['armor'] = max(1, rnd(power * F(2.2 if slot in ('chest', 'legs') else 2.6 if slot == 'offhand' else 1.4)))
    d['value'] = max(1, rnd(F(c * (1 + q)) * F(1.4 if slot == 'mainhand' else 1)))
    k = max(F(1), power / F(4)); budget = rnd(F(6) * k)
    return d, budget

# (boss, its level, [(slug, name, slot, look, stat shares, description)])
B = [
 ('Road-Captain Everard Hurst', 16, [
   ('road_captains_grey_cloak', "Road-Captain's Grey Cloak", 'shoulders', 'shoulder.mantle:shawl/pale', {'stamina': 3, 'strength': 2}, "A Pact cloak gone the colour of the road it was worn on, ash in every fold that will not shake out."),
   ('carters_ash_boots', "Carter's Ash-Boots", 'feet', 'feet.boots:hobnail/tollroad', {'stamina': 2, 'agility': 2}, "Hobnailed boots that have walked the Ash Road more times than their carter could count, and never once off it.")]),
 ('The Old Ram', 16, [
   ('old_rams_horn_torc', "Old Ram's Horn Torc", 'neck', 'neck.cord:fang/oakhaven', {'stamina': 3, 'strength': 2}, "A curl of grey horn on a cord, light as paper. It was the old ram's, and the old ram was Bet's.")]),
 ('The Grey Carter', 16, [
   ('grey_carters_reins', "Grey Carter's Reins", 'hands', 'hands.wraps:silk/pale', {'agility': 3, 'stamina': 2}, "Leather reins wound round the hands for gloves. Whatever they once drove is fifty years down the road.")]),
 ('Sergeant Wymond Tredway', 17, [
   ('the_span_holder', 'The Span-Holder', 'offhand', 'shield.heater:plain/tollroad', {'stamina': 3, 'strength': 2}, "A Pact sergeant's shield, the bridge's arms painted over it badly. It held the span; the span did not hold."),
   ('pact_sergeants_greaves', "Pact Sergeant's Greaves", 'legs', 'legs.greaves:plate/tollroad', {'strength': 3, 'stamina': 2}, "Plate greaves with a grey bloom on them that no oil lifts.")]),
 ('The Fallen Mason', 17, [
   ('the_masons_trowel', "The Mason's Trowel", 'mainhand', 'cleaver:notched/sandthrone', {'strength': 3, 'stamina': 2}, "The trowel that laid the bridge's courses, its edge ground to a blade by fifty years of nothing to lay.")]),
 ('Assessor Piers Upcott', 18, [
   ('the_assessors_chalk', "The Assessor's Chalk", 'offhand', 'offhand.hung:scale/concord', {'intellect': 3, 'spirit': 2}, "A Bureau tally-board and its chalk on a chain. The last mark on it is a cross."),
   ('assessors_grey_coat', "Assessor's Grey Coat", 'chest', 'chest.coat:grey/concord', {'intellect': 3, 'stamina': 2}, "A Bureau coat with the black sunburst on the cuff, chalk dust in the seams.")]),
 ('The Lamplighter', 18, [
   ('lamplighters_hood', "Lamplighter's Hood", 'head', 'head.wrap:scarf/pale', {'agility': 3, 'stamina': 2}, "A grey hood smelling of lamp-oil that has not burned in fifty years.")]),
 ('Picker-Master Fulke Yelland', 18, [
   ('pickers_gloves', "Picker's Gloves", 'hands', 'hands.gloves:plain/concord', {'agility': 3, 'stamina': 2}, "Heavy gloves for handling memory-quartz. The palms are worn through where the stone was warm."),
   ('picker_masters_hook', "Picker-Master's Hook", 'mainhand', 'knife:hooked/sandthrone', {'agility': 3, 'stamina': 2}, "A sorting hook for sacks and crates, with a point the Picker-Master kept sharper than sorting needs.")]),
 ('Gil Coldwell, the Loft Watch', 18, [
   ('loft_watch_jerkin', "Loft Watch's Jerkin", 'chest', 'chest.jerkin:leather/sandthrone', {'agility': 3, 'stamina': 2}, "A Pact archer's jerkin, padded at the shoulder where the net-bow kicks.")]),
 ('First-Cut', 19, [
   ('the_cutters_chisel', "The Cutter's Chisel", 'mainhand', 'knife:hooked/tollroad', {'agility': 3, 'stamina': 2}, "The chisel the Bureau's first cutter used on the first of the paused. It has never come clean."),
   ('grey_quartz_pendant', 'Grey Quartz Pendant', 'neck', 'neck.pendant:glass/pale+glow', {'intellect': 3, 'spirit': 2}, "A chip of memory-quartz on a cord. Hold it and you remember a kitchen you have never stood in.")]),
 ('The Long Shadow', 19, [
   ('a_minute_in_amber', 'A Minute in Amber', 'offhand', 'offhand.hung:shuttered/sandthrone+glow', {'intellect': 3, 'spirit': 2}, "A drop of amber with nothing in it but a minute. It is always the same minute.")]),
 ('Registrar Dunstan Pardew', 19, [
   ('registrars_robe', "Registrar's Robe", 'chest', 'chest.robe:vestment/concord', {'intellect': 3, 'spirit': 2}, "A Bureau registrar's robe, black, its cuffs stiff with ink."),
   ('ink_stained_gloves', 'Ink-Stained Gloves', 'hands', 'hands.gloves:plain/tollroad', {'intellect': 3, 'stamina': 2}, "Thin gloves for copying, the fingers black to the second knuckle.")]),
 ('The Reader at the Desk', 19, [
   ('the_readers_lens', "The Reader's Lens", 'neck', 'neck.pendant:seal/pilgrim', {'intellect': 3, 'spirit': 2}, "A reading lens on a chain, ground in the First Era. The page under it is always the one you need.")]),
 ('Pact-Captain Rowena Kersey', 20, [
   ('pact_captains_breastplate', "Pact-Captain's Breastplate", 'chest', 'chest.cuirass:plate/concord', {'strength': 3, 'stamina': 2}, "An Iron Pact captain's breastplate, the company's mark struck through on the chest by her own hand."),
   ('pact_captains_sabatons', "Pact-Captain's Sabatons", 'feet', 'feet.sabatons:plate/concord', {'stamina': 3, 'strength': 2}, "Plate sabatons that climbed the tower stair a thousand times on the Examiner's errands.")]),
 ('Bureau Examiner Thurstan Vowell', 20, [
   ('the_examiners_sunburst', "The Examiner's Sunburst", 'neck', 'neck.pendant:signet/sandthrone', {'intellect': 3, 'spirit': 2}, "The Bureau's black sunburst in jet, on the chain an Examiner wears. It is heavier than it looks."),
   ('the_examiners_seal', "The Examiner's Seal", 'offhand', 'offhand.hung:scale/concord', {'intellect': 3, 'stamina': 2}, "The Examiner's seal on a fob: the High Council's word, pressed into wax a thousand times, into stone once.")]),
]
STATS = ['stamina', 'strength', 'agility', 'intellect', 'spirit']

def main():
    items, gear, drops = [], [], []
    for boss, lvl, pieces in B:
        level = min(19, lvl); pick = []
        for slug, name, slot, look, w, desc in pieces:
            d, budget = curve(slot, level)
            tot = sum(w.values()); stats = {s: int(budget * w[s] // tot) for s in w}
            stats[max(w, key=w.get)] += budget - sum(stats.values())
            iid = 'loot.hal.' + slug
            it = {'id': iid, 'name': name, 'kind': 'gear', 'slot': slot, 'quality': 3, 'level': level, 'value': d['value']}
            if 'weaponDamage' in d: it['weaponDamage'] = d['weaponDamage']
            if 'armor' in d: it['armor'] = d['armor']
            for s in STATS:
                if stats.get(s): it[s] = stats[s]
            it['description'] = desc; it['canonStatus'] = 'GAME-ONLY'
            items.append(it); gear.append({'id': iid, 'look': look, 'source': 'boss:' + boss, 'boss': True, 'unique': True}); pick.append({'item': iid})
        slugb = ''.join(ch if ch.isalnum() else '_' for ch in boss.lower()).strip('_')
        while '__' in slugb: slugb = slugb.replace('__', '_')
        drops.append({'id': 'drop.hallows.' + slugb, 'zone': 'hallows', 'mob': boss, 'rank': 'elite', 'groups': [{'chance': 1, 'signature': True, 'pick': pick}]})
    with io.open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write(json.dumps({'items': items, 'vendors': [], 'gear': gear, 'drops': drops, 'sets': []}, indent=2, ensure_ascii=False) + '\n')
    print(len(items), 'items')
    for it in items: print(it['id'])

if __name__ == '__main__':
    main()
