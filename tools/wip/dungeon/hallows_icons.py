"""hallows_icons.py: paints Hallow's Creek's named gear icons (GAME-ONLY imagery) with tools/art/make_icons.py's drawers, as
tools/wip/lowtowns/make_lowtown_icons.py does for the Lowtowns: every piece in loot.hallows.json (the elites' signature pieces and the
secrets' rings) drawn from its look, written with its .meta (a GUID from its path, as make_icons does; an existing .meta is kept) under
Resources/Icons/item. An icon already there is left alone unless --all is given (so the first pass's twenty-three keep their files).

    python tools/wip/dungeon/hallows_icons.py           the pieces with no icon yet (the secrets' three rings)
    python tools/wip/dungeon/hallows_icons.py --all     every piece again
"""
import json, os, sys
HERE = os.path.dirname(os.path.abspath(__file__)); REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
sys.path.insert(0, os.path.join(REPO, 'tools', 'art'))
import make_icons as M
ASSETS = os.path.join(REPO, 'New Unity Project', 'Assets', 'Crulanda')
OUT = os.path.join(ASSETS, 'Resources', 'Icons')
LOOKS = json.load(open(os.path.join(ASSETS, 'Resources', 'Gear', 'looks.json'), encoding='utf-8'))
LOOT = json.load(open(os.path.join(ASSETS, 'EncounterContent', 'Items', 'loot.hallows.json'), encoding='utf-8-sig'))
every = '--all' in sys.argv
icons = []
for g in LOOT['gear']:
    rel = 'item/' + M.safe(g['id'])
    if not every and os.path.exists(os.path.join(OUT, rel.replace('/', os.sep) + '.png')): continue
    icons.append((rel, M.gear_icon(g['id'], LOOKS, M.parse_look(g['look']), g['id']), g['id'], 3, False))
print('wrote', M.write_icons(icons, OUT, M.OUT), 'icons under', OUT, ':', ', '.join(i[2] for i in icons))
