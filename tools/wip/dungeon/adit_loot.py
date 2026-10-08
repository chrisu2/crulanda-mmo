"""The Sealed Adit's named loot (DUNGEON_DESIGN.md section 7, dungeon step D6, first pass): a signature list for each of the seven
elites (two or three boss pieces, rare, on the generated curve at the piece's level, a signature piece's stat budget), the beast
tables the Vent-Hound and the greyed bears skin into, and the ids in LootIdBaseline.txt. Writes loot.adit.json and edits
items.json's loot tables and the baseline. GAME-ONLY names.

    python adit_loot.py
"""
import io, json, os, re, sys
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'levels'))
from regen_named import curve, budget, STATS

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
A = os.path.join(ROOT, 'New Unity Project', 'Assets', 'Crulanda')
ITEMS = os.path.join(A, 'EncounterContent', 'Items')

# (slug, name, slot, level, look, stats {stat: share}, description)
PIECES = {
    'Gang-Boss Haddo Lusk': [
        ('lusks_tally_stick', "Lusk's Tally-Stick", 'mainhand', 11, 'mace.flanged:six/tollroad', {'strength': 3, 'stamina': 2}, "An iron-shod stick notched for every cart that went down and every man who did not come up."),
        ('overseers_long_coat', "Overseer's Long Coat", 'chest', 11, 'chest.coat:skirted/sandthrone', {'stamina': 3, 'strength': 2}, "Sandthrone grey, the skirts stiff with rock dust. A tally book's shape is worn into the breast."),
    ],
    'Nix, the turncoat': [
        ('the_borer_bit', 'The Borer Bit', 'mainhand', 11, 'hammer.war:pick/khaven', {'strength': 3, 'agility': 2}, "The Rock-Eater's drill head, cut down to a pick. It still hums if you hold it to the stone."),
        ('turncoats_greasy_gloves', "Turncoat's Greasy Gloves", 'hands', 11, 'hands.gloves:fingerless/sandthrone', {'agility': 3, 'stamina': 2}, "Nix's own. Small, quick, and black to the wrist with engine oil."),
    ],
    'Cinder-Warden Ysolt': [
        ('ember_wardens_brand', "Ember-Warden's Brand", 'offhand', 11, 'offhand.hung:censer/cult+glow', {'intellect': 3, 'spirit': 2, 'stamina': 1}, "A censer on a chain that never quite goes out. The Cult of Ash carries fire the way others carry a lamp."),
        ('cinder_wardens_sabatons', "Cinder-Warden's Sabatons", 'feet', 11, 'feet.sabatons:plate/ashwalker', {'stamina': 3, 'strength': 2}, "Plate boots scorched to blue. She walked the lake's rim in them."),
        ('ash_red_pendant', 'Ash-Red Pendant', 'neck', 11, 'neck.pendant:ember/ashwalker', {'intellect': 2, 'spirit': 2, 'stamina': 1}, "A drop of red glass on a cord, warm to the touch, as if it remembered the vent."),
    ],
    'The Foreman Who Forgot': [
        ('the_grey_lantern', 'The Grey Lantern', 'offhand', 11, 'offhand.hung:shuttered/sandthrone+glow', {'spirit': 3, 'stamina': 2}, "A miner's lantern that gives a light with no colour in it. Things look washed out by it, and some look less there."),
        ('foremans_unmade_breeches', "Foreman's Unmade Breeches", 'legs', 11, 'legs.breeches:patched/sandthrone', {'stamina': 3, 'agility': 2}, "One leg is cloth. The other is cloth most of the time."),
    ],
    'The Vent-Hound': [
        ('vent_hounds_ember_pelt', "Vent-Hound's Ember Pelt", 'shoulders', 11, 'shoulder.mantle:fur/pilgrim', {'stamina': 3, 'strength': 2}, "A mantle of hound hide with a line of embers still along the spine. It smells of the vent."),
        ('vent_hound_tooth', 'Vent-Hound Tooth', 'neck', 11, 'neck.cord:tooth/ashwalker', {'agility': 3, 'strength': 2}, "A tooth the length of a finger, black as a coal."),
    ],
    'Quartermaster Brannigan Sorrel': [
        ('sorrels_heavier_hammer', "Sorrel's Heavier Hammer", 'mainhand', 12, 'hammer.war:maul/khaven', {'strength': 3, 'stamina': 2}, "The last thing he took off the rack. He never got to the one after it."),
        ('quartermasters_gauntlets', "Quartermaster's Gauntlets", 'hands', 12, 'hands.gauntlets:plate/tollroad', {'strength': 3, 'stamina': 2}, "Plate gauntlets stamped with the Sandthrone mark, the knuckles bright from the rack's iron."),
        ('platform_masters_greaves', "Platform-Master's Greaves", 'legs', 12, 'legs.greaves:plate/tollroad', {'stamina': 3, 'strength': 2}, "Greaves with a boot-heel's dent in the left shin. He stamped a great deal."),
    ],
    'Rail-Captain Orsk Danner': [
        ('rail_captains_coat', "Rail-Captain's Coat", 'chest', 12, 'chest.cuirass:plate/tollroad', {'stamina': 3, 'strength': 2, 'spirit': 1}, "An armoured coat with the carriage's rivets down its front. The letter's pocket is empty now."),
        ('danners_kettle_helm', "Danner's Kettle Helm", 'head', 12, 'head.kettle:plain/tollroad', {'stamina': 3, 'intellect': 2}, "A rail-captain's kettle hat, the brim lamp-blacked from a hundred nights on the dead line."),
        ('the_dead_line_shield', 'The Dead Line Shield', 'offhand', 12, 'shield.heater:plain/tollroad', {'stamina': 3, 'strength': 2}, "A heater shield cut from a carriage plate. The rail's mark runs across it like a scar."),
    ],
}
BEASTS = [('venthound', 'hound', 10, 12), ('greybear', 'bear', 10, 12)]   # tag, the table to copy, levels

items, gear, drops, ids = [], [], [], []
for mob, pieces in PIECES.items():
    picks = []
    for slug, name, slot, level, look, shares, desc in pieces:
        iid = 'loot.adit.' + slug; ids.append(iid)
        dmg, arm, value, power = curve(slot, level, 3); b = budget(3, power, True)
        total = sum(shares.values()); raw = {s: shares[s] * b / total for s in shares}; stats = {s: int(v) for s, v in raw.items()}
        for s in sorted(raw, key=lambda s: raw[s] - stats[s], reverse=True)[:b - sum(stats.values())]: stats[s] += 1
        it = {'id': iid, 'name': name, 'kind': 'gear', 'slot': slot, 'quality': 3, 'level': level, 'value': value}
        if dmg: it['weaponDamage'] = dmg
        if arm: it['armor'] = arm
        for s in STATS:
            if stats.get(s): it[s] = stats[s]
        it['description'] = desc; it['canonStatus'] = 'GAME-ONLY'
        items.append(it); gear.append({'id': iid, 'look': look, 'source': 'boss:' + mob, 'boss': True, 'unique': True}); picks.append({'item': iid})
    drops.append({'id': 'drop.adit.' + re.sub(r'[^a-z]+', '_', mob.lower().split(',')[0]).strip('_'), 'zone': 'adit', 'mob': mob, 'rank': 'elite', 'groups': [{'chance': .5, 'signature': True, 'pick': picks}]})
out = {'items': items, 'vendors': [], 'gear': gear, 'drops': drops, 'sets': []}
io.open(os.path.join(ITEMS, 'loot.adit.json'), 'w', encoding='utf-8', newline='\n').write(json.dumps(out, indent=2, ensure_ascii=False) + '\n')

# The beast tables, copied from the Rim's hounds and the hill bears at the Adit's levels.
p = os.path.join(ITEMS, 'items.json'); core = json.load(io.open(p, encoding='utf-8'))
tables = core['loot']
for tag, like, lo, hi in BEASTS:
    if any(t['tag'] == tag for t in tables): continue
    src = next(t for t in tables if t['tag'] == like); t = json.loads(json.dumps(src)); t['tag'] = tag; t['levelMin'] = lo; t['levelMax'] = hi; tables.append(t)
io.open(p, 'w', encoding='utf-8', newline='\n').write(json.dumps(core, indent=2, ensure_ascii=False) + '\n')

# The baseline: every loot id, for good.
b = os.path.join(A, 'Tests', 'EditMode', 'LootIdBaseline.txt'); s = io.open(b, encoding='utf-8', newline='').read(); nl = '\r\n' if '\r\n' in s else '\n'
new = [i for i in ids if i not in s]
if new: s = s.rstrip(nl) + nl + '# The Sealed Adit (dungeon D6 first pass, 2026-10-08)' + nl + nl.join(new) + nl; io.open(b, 'w', encoding='utf-8', newline='').write(s)
print('adit loot:', len(items), 'items,', len(drops), 'lists,', len(new), 'new baseline ids')
