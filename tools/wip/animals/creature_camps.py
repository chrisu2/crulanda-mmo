"""Camps and quests for the new creatures (Chris, 2026-10-04: bears in Oakhaven's woods and on the Peaks; the skeleton as
Khaven's graveyard dead). All GAME-ONLY. Safe to rerun."""
import io, json, os
A = r'D:\code\mmo\New Unity Project\Assets\Crulanda\EncounterContent'
def load(p): return json.load(open(p, encoding='utf-8'))
def save(p, d): io.open(p, 'w', encoding='utf-8', newline='\n').write(json.dumps(d, indent=2, ensure_ascii=False) + '\n')

def camps(zone, new, note_key, note):
    zp = os.path.join(A, 'Zones', zone + '.json'); z = load(zp)
    have = {c['name'] for c in z['camps']}
    for c in new:
        if c['name'] not in have: z['camps'].append(c)
    if note_key not in z['canonNote']: z['canonNote'] += note
    save(zp, z)
    return {c['name']: i for i, c in enumerate(z['camps'])}

def quests(zone, new):
    qp = os.path.join(A, 'Quests', zone + '.json'); q = load(qp)
    have = {d['id'] for d in q['quests']}
    for d in new:
        if d['id'] not in have: q['quests'].append(d)
    save(qp, q)

# Oakhaven: brown bears in the hill hazels, and an old one.
at = camps('oakhaven', [
    {"name": "Hill hazel bears", "mob": "Brown bear", "tag": "bear", "look": "bear", "canonStatus": "GAME-ONLY",
     "center": {"x": -70, "y": 178}, "radius": 8, "count": 2, "levelMin": 2, "levelMax": 2, "respawn": 90},
    {"name": "Old Hazelmaw", "mob": "Old Hazelmaw", "tag": "bear", "look": "bear", "canonStatus": "GAME-ONLY (an old bear of the hill hazels)",
     "center": {"x": -58, "y": 194}, "radius": 3, "count": 1, "levelMin": 3, "levelMax": 3, "respawn": 180},
], 'Bears in the hill hazels', " Bears in the hill hazels under Crowsfoot Ridge, and Old Hazelmaw (2026-10-04): GAME-ONLY.")
Z = 'zone.oakhaven'
quests('oakhaven', [
 {"id": "npc.garet.bears", "title": "Bears in the Hazels", "kind": "npc", "giver": "Garet Moss", "turnIn": "Garet Moss", "zone": Z,
  "level": 2, "requires": ["npc.garet.tracks"], "canonStatus": "GAME-ONLY",
  "summary": "Bears have come down off Crowsfoot Ridge into the hill hazels, an old one with them. Garet wants them gone before they find the Harrows' pigs.",
  "offer": "Garet Moss is scraping a hide on the rail and doesn't look up. \"Bears. Down off the ridge into the hill hazels, east of the Mastwood. They never come this low.\" He sets the scraper down. \"Something up there has them moving. Two young ones, and an old boar the size of a cart. Hazelmaw, my father called him, and my father's been dead twenty years.\"\n\n\"Before they find the Harrows' pigs. Mind the old one: he doesn't bluff.\"",
  "complete": "Garet looks at the claw you bring him a long while. \"Hazelmaw. I'll be.\" He hangs it on the rail with the hides. \"Twenty years he kept to the ridge. Whatever moved him, I'd rather it hadn't.\"",
  "steps": [{"text": "Drive the bears out of the hill hazels.", "objectives": [
    {"type": "kill", "zone": Z, "target": "mob.bear.oakhaven.%d.*" % at["Hill hazel bears"], "count": 2, "text": "Brown bears"},
    {"type": "kill", "zone": Z, "target": "mob.bear.oakhaven.%d.*" % at["Old Hazelmaw"], "text": "Old Hazelmaw",
     "say": "The old bear goes down like a felled tree, and the hazels go quiet around it."}]}],
  "rewards": {"xp": 90, "gold": 14, "reputation": [{"faction": "oakhaven", "amount": 150}]}},
])

# The Peaks: mountain bears in the high pines.
at = camps('peaks', [
    {"name": "High pines bears", "mob": "Mountain bear", "tag": "bear", "look": "bear", "canonStatus": "GAME-ONLY",
     "center": {"x": -44, "y": 99}, "radius": 7, "count": 3, "levelMin": 7, "levelMax": 8, "respawn": 90},
], 'Mountain bears in the high pines', " Mountain bears in the high pines (2026-10-04): GAME-ONLY.")
Z = 'zone.peaks'
quests('peaks', [
 {"id": "npc.yara.bears", "title": "Bears in the High Pines", "kind": "npc", "giver": "Yara Quell", "turnIn": "Yara Quell", "zone": Z,
  "level": 7, "canonStatus": "GAME-ONLY",
  "summary": "Mountain bears have come down into the high pines below the Cold Tarn, and they have taken two of the pilgrims' mules. Yara wants three of them dead.",
  "offer": "Yara Quell is mending a mule's harness that something has bitten clean through. \"Bears. Grey ones, mountain bears, down in the high pines under the tarn.\" She holds up the harness. \"Two mules this week. They don't come below the snow in a good year, and this isn't one.\"\n\n\"Three of them. Keep your back to a tree, and don't run: they're quicker than you downhill.\"",
  "complete": "\"Three.\" Yara turns a grey pelt over in her hands, thick as a blanket. \"That's a winter's warmth for somebody up here.\" She folds it. \"The mules will thank you. In their way.\"",
  "steps": [{"text": "Kill the mountain bears in the high pines.", "objectives": [
    {"type": "kill", "zone": Z, "target": "mob.bear.peaks.%d.*" % at["High pines bears"], "count": 3, "text": "Mountain bears"}]}],
  "rewards": {"xp": 250, "gold": 52}},
])

# Khaven: the old bones the creek gave back, at the drowned graveyard.
at = camps('khaven', [
    {"name": "The drowned dead", "mob": "Drowned dead", "tag": "drowned", "look": "skeleton",
     "canonStatus": "GAME-ONLY (the old dead of the drowned graveyard's low graves)",
     "center": {"x": 38, "y": -176}, "radius": 5, "count": 4, "levelMin": 5, "levelMax": 5, "respawn": 90},
], 'The drowned dead (2026', " The drowned dead (2026-10-04): the old bones the creek gave back, risen at the drowned graveyard; GAME-ONLY.")
Z = 'zone.khaven'
quests('khaven', [
 {"id": "npc.ansel.drowned", "title": "The Old Bones", "kind": "npc", "giver": "Ansel Morrow", "turnIn": "Ansel Morrow", "zone": Z,
  "level": 5, "requires": ["npc.ansel.hollows"], "canonStatus": "GAME-ONLY",
  "summary": "The flood gave back the oldest graves too: bare bones, walking by the drowned chapel wall. Ansel wants them laid down again.",
  "offer": "Ansel turns the pages of his book back, and back, to names in a hand that isn't his. \"The grey ones were the new dead. These are older. The low graves the creek took before my grandfather's time.\" He closes the book. \"Bones, walking. Nothing on them but mud and what they were buried in. By the drowned chapel wall.\"\n\n\"Four of them. I don't know their names. Somebody did, once.\"",
  "complete": "Ansel listens, then writes four lines in the book that say only: \"one of the old ones, laid down again.\" \"It's the best I can do for them,\" he says. \"It's more than the creek did.\"",
  "steps": [{"text": "Lay the drowned dead to rest by the drowned chapel wall.", "objectives": [
    {"type": "kill", "zone": Z, "target": "mob.drowned.khaven.%d.*" % at["The drowned dead"], "count": 4, "text": "Drowned dead laid to rest"}]}],
  "rewards": {"xp": 165, "gold": 32}},
])
print('ok')
