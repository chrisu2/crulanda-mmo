"""Treasure chests (2026-10-05, Chris's animated chest): beside the elite camps out in the open, one or two a zone. Safe to rerun."""
import io, json, os
Z = r'D:\code\mmo\New Unity Project\Assets\Crulanda\EncounterContent\Zones'
PICK = {'oakhaven': ["Whitefoot's den"], 'khaven': ['The Grey Sexton', 'The Pale Reckoner'], 'peaks': ["Captain's eyrie", 'Old Scree-Tusk'],
        'ashrim': ['The Weave-Eater brood', 'The Ash-Deacon'], 'verdant': ['Greyheart', 'Old Ninebranch']}
for zone, names in PICK.items():
    p = os.path.join(Z, zone + '.json'); z = json.load(open(p, encoding='utf-8'))
    have = {q.get('name') for q in z['props']}
    for n in names:
        c = next(c for c in z['camps'] if c['name'] == n); d = c['radius'] + 2.5
        name = "Chest at " + n.replace('The ', 'the ', 1) if n.startswith('The ') else "Chest at " + n
        if name in have: continue
        prop = {"kind": "chest", "name": name, "at": {"x": round(c['center']['x'] + d * .7, 2), "y": round(c['center']['y'] + d * .7, 2)}, "rotation": 225,
                "interact": "Open the chest", "canonStatus": "GAME-ONLY"}
        board = next((i for i, q in enumerate(z['props']) if q.get('kind') == 'board'), len(z['props']))
        z['props'].insert(board, prop)
    io.open(p, 'w', encoding='utf-8', newline='\n').write(json.dumps(z, indent=2, ensure_ascii=False) + '\n')
print('ok')
