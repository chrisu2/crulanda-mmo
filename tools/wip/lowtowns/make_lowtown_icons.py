"""make_lowtown_icons.py: paints the Lowtowns' icons (Phase 9.1; GAME-ONLY imagery) with tools/art/make_icons.py's drawers and writes them,
each with its .meta (a GUID from its path, as make_icons does; an existing .meta is kept), under Resources/Icons: the two thrown items
(salt and the bone harpoon) the zone's eight quest items and its elites' five signature pieces. Run: python tools/wip/lowtowns/make_lowtown_icons.py
(Kept apart from make_icons.py so the two do not collide on a merge; its tables can take these entries later.)"""
import json, os, sys
HERE = os.path.dirname(os.path.abspath(__file__)); REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
sys.path.insert(0, os.path.join(REPO, 'tools', 'art'))
import make_icons as M
ITEMS = {
    'throw.salt_twist': ('food', M.salt, ('#dcdcd4',), dict(crust=True)),
    'throw.bone_harpoon': ('tool', M.fang, (), dict(bone='#d8d0bc', root='#c8862e', curve=.4)),
}
QUEST = {
    'item.warrant_page': (M.letter, ('#8a8a90',), dict(broken=True)),
    'item.bought_shard': (M.shard, ('#d8e4f0', 'gem'), dict(glowc='#bcd8ff', thin=True, spark=True)),
    'item.brannoc_ledger': (M.book, ('#3a2a20', '#7a7a80'), dict(wrap='#2a1e16')),
    'item.lamp_glass': (M.shard, ('#26242c', 'gem'), dict(glowc='#ff9a40')),
    'item.carter_bale': (M.sack, ('#8a7a5a', '#c9b48a'), dict(knots=3)),
    'item.seer_parcel': (M.goods,),
    'item.hound_bone': (M.bone, ('#c8c0b0',)),
    'item.eater_thread': (M.skein,),
}
icons = [('item/' + M.safe(i), M.plain_icon(i, spec), i, 1, False) for i, spec in ITEMS.items()]
icons += [('quest/' + M.safe(i), M.plain_icon('q' + i, ('quest',) + spec), i, 1, False) for i, spec in QUEST.items()]
# the elites' signature pieces (loot.lowtowns.json): drawn from their looks, as make_icons.py draws named gear
LOOKS = json.load(open(os.path.join(REPO, 'New Unity Project', 'Assets', 'Crulanda', 'Resources', 'Gear', 'looks.json'), encoding='utf-8'))
LOOT = json.load(open(os.path.join(REPO, 'New Unity Project', 'Assets', 'Crulanda', 'EncounterContent', 'Items', 'loot.lowtowns.json'), encoding='utf-8-sig'))
icons += [('item/' + M.safe(g['id']), M.gear_icon(g['id'], LOOKS, M.parse_look(g['look']), g['id']), g['id'], 3, False) for g in LOOT['gear']]
out = os.path.join(REPO, 'New Unity Project', 'Assets', 'Crulanda', 'Resources', 'Icons')
print('wrote', M.write_icons(icons, out, M.OUT), 'icons under', out)
