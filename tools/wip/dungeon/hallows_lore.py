"""Hallow's Creek's chapter of the book of lore (EncounterContent/Lore/hallows.json) and its secrets' clues and hints
(Resources/Lore/hallows.json). Checks what LoreBookTests and DiscoveryLog ask: no landmark's name in a clue or hint, no secret's name in a line.

    python hallows_lore.py
"""
import io, json, os
HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.join(HERE, '..', '..', '..', 'New Unity Project', 'Assets', 'Crulanda')
E = []
def e(ch, subj, line, more=None, page=None, secret=None, canon='GAME-ONLY'):
    slug = ''.join(c if c.isalnum() else '-' for c in subj.lower()).strip('-')
    while '--' in slug: slug = slug.replace('--', '-')
    d = {'id': 'lore.hallows.%s.%s' % (ch, slug), 'chapter': ch, 'subject': subj, 'line': line}
    if more: d['more'] = more
    if page: d['page'] = page
    if secret: d['secret'] = secret
    d['canonStatus'] = canon; E.append(d)

e('places', 'The Ash Road', "The last road into the town the Wasting took first, its stone trees laying long shadows across it that you should not stand in.", canon='CANON (book1 ch.17: the hungry shadows); GAME-ONLY: the road as a way in')
e('places', 'The Fossilised Farm', "A farmhouse turned to stone with its farmer at the gate and its dog in the yard, the dog sitting up as if someone had just called its name.", canon='CANON (book1 ch.17)')
e('places', 'The Bridge of Shattered Glass', "The old bridge was blown apart and the blast stopped mid-air. You cross the chasm on its hanging slabs.", canon='CANON (book1 ch.17)')
e('places', 'The bridge-head gatehouse', "Where the Iron Pact holds the bridge's far end for the Bureau.", canon='GAME-ONLY (the Iron Pact is CANON, book1 ch.2)')
e('places', 'Main Street', "White fences gone to stone, a baker in his doorway with a tray, a ball hanging in the air over the cobbles. Everyone on it is watching you.", canon='CANON (book1 ch.18)')
e('places', 'The Square', "Where the town hall stood there is a bowl of glass, and a red door hanging over it with nothing to open into.", canon='CANON (book1 ch.19: the crater and the red door; the door is never reached)')
e('places', 'The Granary on the race', "The town's tithe barn, where the Bureau's pickers size what they cut into crates for the coast.",
  more="The Picker-Master's tally: forty of the paused cut, twelve stone of yield, and a line about the cellars he was told not to number.", page='doc.hallows.pickers_tally', canon='GAME-ONLY (the Bureau is CANON, book1 ch.3)')
e('places', 'The Old Archive', "A First Era library: columns twisted like pulled taffy, a crest of an open book with an eye, a maze of stone shelves. The Preservationists kept it once.",
  more="A page of the High Archivist's log, found loose in the stacks: the children came to the steps to hear the readings.", page='doc.hallows.archivist_page', canon='CANON (book1 ch.18)')
e('places', 'The Bell Tower', "The tower over the town, its great bell hung mid-swing and its painted dial a minute short of noon. Paint on stone: nothing in it turns.",
  more="Four names chalked low on the inside wall, the ringers' practice marks, the last stopping half way through the word.", secret='secret.hallows.ringers-names', canon='CANON-EXPANDED (book1 ch.21: the tower; the bell, the dial and the ringers GAME-ONLY)')
e('places', 'The Cut-Store', "The cellar where the Bureau's cutters throw what they have broken. What they throw there does not stay thrown.", canon='GAME-ONLY (the Hollow are CANON, book1 ch.4)')
e('creatures', 'Road-Captain Everard Hurst', "The Pact captain who keeps the Ash Road for the Bureau's carts and puts anyone in his way down in the ash.")
e('creatures', 'Sergeant Wymond Tredway', "He holds the gate at the bridge's far end. He says the span is his, and the span has not argued.")
e('creatures', 'Assessor Piers Upcott', "The Bureau man who walks the street with chalk, marking the paused for the cut. A cross on a door is a cross on a person.")
e('creatures', 'Picker-Master Fulke Yelland', "He sizes memory-quartz into crates and keeps a tally of everything but what walks after the cut.")
e('creatures', 'First-Cut', "The first of the paused the Bureau cut. Its shell broke. What was inside it did not go anywhere.", canon='GAME-ONLY (the Hollow are CANON, book1 ch.4: they pour from cellar doors)')
e('creatures', 'Registrar Dunstan Pardew', "He keeps the Archive's register for the Bureau now, and says hush to anyone who speaks above a whisper in it.")
e('creatures', 'Bureau Examiner Thurstan Vowell', "The Examiner learned the town's one trick: a bell rung at the frozen minute wakes the square, and the grey breathes a field wider.")
e('creatures', 'The Lamplighter', "A Hollow who walks the street at the hour, lighting lamps that give no light. Not every night.", canon='GAME-ONLY (the Hollow are CANON, book1 ch.4)')
e('creatures', 'The Old Ram', "Grey and light as paper, it stood in the east meadow when the meadow went pale. Sometimes it wakes.", canon="GAME-ONLY (Bet's letter is already in the game)")

S = [
 ('secret.hallows.satchel', "Off the east side of the road in, under a lip of rock the ash cannot reach, a strap has been cut and something left behind.", "A Pact soldier's mother in Oakhaven never had his last letter. Her boy wrote that he kept his things dry under a rock by the road."),
 ('secret.hallows.bets-cart', "A cart packed for the road west, turned to stone in a side yard with a chair lashed on top. What was under the seat was not stone.", "Ada Hollin says her sister Bet had the cart packed the night before. She was waiting for the morning."),
 ('secret.hallows.masons-cache', "Below the slabs you cross, on the lowest of them, a mason left his tub of mortar and something wrapped in his apron.", "They say the man who built the bridge would not leave it. If he is anywhere, he is under it."),
 ('secret.hallows.two-coppers', "Two coins left on a shop's sill for a loaf, fifty years ago. When the shop bell rings, they are warm.", "Mr Gable sold the best bread in the valley, the old folk say, and nobody ever left without paying. Nobody."),
 ('secret.hallows.reed-key', "A careful woman hangs her spare key just inside the door of a little roofed house on the crater's rim, where the stair goes down.", "Tamsin Reed says her grandmother kept a spare key to everything, and kept it where she drew her water."),
 ('secret.hallows.reed-chest', "By the white fence of a house with a blue door gone grey, a blanket chest set out for a cart that never came. Its lock never turned to stone.", "Tamsin Reed says her grandmother packed the blanket chest to follow them west, and never let anyone see the bottom of it."),
 ('secret.hallows.archivists-page', "High on the gallery of the stone library, behind a row of stone books, one page is paper still.", "The Preservationists say the old Archivists hid their own words among the books, where only a reader would look."),
 ('secret.hallows.ringers-names', "Low on the inside wall of the tower where the bell hangs, four names in chalk, the way ringers mark a practice.", "Bell-ringers chalk their names on the wall at practice. Four rang in that tower the day it stopped, the old folk say."),
]

def main():
    d = {'zone': 'zone.hallows', 'canonStatus': "GAME-ONLY: Hallow's Creek's chapter of the book of lore, its lines the game's. What they tell of is CANON or CANON-EXPANDED as hallows.json's canonNote says: the paused town, the bridge's slabs, the crater and the red door, the Old Archive and the hungry shadows (book1 ch.15-21), the Hollow (book1 ch.4), the Bureau (book1 ch.3). Written 2026-10-16.", 'entries': E}
    with io.open(os.path.join(ASSETS, 'EncounterContent', 'Lore', 'hallows.json'), 'w', encoding='utf-8', newline='\n') as f: f.write(json.dumps(d, indent=2, ensure_ascii=False) + '\n')
    r = {'zone': 'zone.hallows', 'canonStatus': "GAME-ONLY: every clue and hint here is the game's. What they tell of is CANON or CANON-EXPANDED as hallows.json's canonNote says (book1 ch.15-21). Tamsin Reed, Ada Hollin and Bet are Oakhaven's people (GAME-ONLY); Mr Gable is CANON (book1 ch.21). Written 2026-10-16.",
         'secrets': [{'id': i, 'clue': c, 'hint': h} for i, c, h in S], 'stories': []}
    with io.open(os.path.join(ASSETS, 'Resources', 'Lore', 'hallows.json'), 'w', encoding='utf-8', newline='\n') as f: f.write(json.dumps(r, indent=2, ensure_ascii=False) + '\n')
    z = json.load(open(os.path.join(ASSETS, 'EncounterContent', 'Zones', 'hallows.json'), encoding='utf-8'))
    L = [l['name'].lower() for l in z['landmarks']]; SN = [s['name'].lower() for s in z['secrets']]
    bad = 0
    for i, c, h in S:
        for l in L:
            if l in c.lower() or l in h.lower(): print('a landmark in a clue or hint:', i, l); bad += 1
    for x in E:
        for n in SN:
            if n in (x['line'] + ' ' + x.get('more', '')).lower(): print('a secret named in a line:', x['id'], n); bad += 1
    if set(i for i, _, _ in S) != {s['id'] for s in z['secrets']}: print('the clues do not match the secrets'); bad += 1
    print('entries', len(E), 'clues', len(S), 'problems', bad)

if __name__ == '__main__':
    main()
