"""Adds the cap-19 talent nodes to the worktree's talent files, keeping each file's own layout and CRLF endings."""
import json, os, re, sys
T = r'D:\code\mmo-lowtowns\New Unity Project\Assets\Crulanda\EncounterContent\Talents'

def N(i, name, tier, col, mx, kind, icon, desc, emph):
    return {"id": i, "name": name, "tier": tier, "col": col, "max": mx, "req": None, "kind": kind, "icon": icon, "desc": desc, "emphasis": emph, "impl": True}

ADD = {
 'archivist': {
  'the-still-vow': [N('vw-long-stillness', 'Long Stillness', 3, 0, 3, 'modifier', 'clock', "Vow of Stillness's ward, and Hushed Pain with it, lasts 1s longer per rank.", {"mitigation": 1})],
  'the-lull': [N('lu-long-echo', 'Long Echo', 3, 0, 3, 'passive', 'spiral', 'Echo Bind holds 3 seconds longer per rank.', {"control": 1}),
               N('lu-half-waking', 'Half-Waking', 3, 2, 2, 'passive', 'moon', 'A mob woken early from your Lull or Echo Bind strikes 5% softer per rank for 6 seconds.', {"control": 1})]},
 'cleric': {
  'lantern': [N('ln-trimmed-wick', 'Trimmed Wick', 3, 0, 3, 'passive', 'drop', 'Mend and Long Mend cost 2 less Grace per rank.', {"healer": 1}),
              N('ln-lamp-held-low', 'Lamp Held Low', 3, 1, 1, 'signature', 'flame', 'Mend and Long Mend heal 10% more on someone under 35% health.', {"healer": 2})],
  'choir': [N('ch-even-breath', 'Even Breath', 3, 0, 3, 'passive', 'clock', 'Litany costs 2 less Grace per rank.', {"healer": 1}),
            N('ch-answering-hills', 'Answering Hills', 3, 1, 1, 'signature', 'note', 'Each tick of the Hymn mends the most hurt within its reach for 10% more.', {"healer": 2})],
  'censure': [N('cn-burning-word', 'Burning Word', 3, 0, 3, 'passive', 'fire', 'Brand burns 3% harder per rank.', {"dps": 1}),
              N('cn-ashes-returned', 'Ashes Returned', 3, 1, 1, 'signature', 'drop', 'Sunburst gives back 4 Grace for each Brand it spends.', {"dps": 2})],
  'warden': [N('wd-patient-ward', 'Patient Ward', 3, 0, 3, 'passive', 'clock', 'Ward lasts 2s longer per rank.', {"support": 1}),
             N('wd-shrine-holds', 'The Shrine Holds', 3, 1, 1, 'signature', 'shield', 'While Steadfast stands, Ward absorbs 15% more.', {"support": 2})]},
 'mage': {
  'combustion': [N('cb-even-flame', 'Even Flame', 3, 0, 3, 'modifier', 'flame', 'Ember Bolt builds 1 less Heat per rank.', {"tempo": 1}),
                 N('cb-kept-coals', 'Kept Coals', 3, 1, 1, 'signature', 'fire', 'A Flare of 70 Heat or more leaves 20 Heat behind.', {"tempo": 1.5})],
  'heatweaver': [N('hw-thick-smoke', 'Thick Smoke', 3, 1, 3, 'modifier', 'cloud', 'Enemies in your Cinder Field are slowed 5% more per rank.', {"control": 1})],
  'spellbinder': [N('sb-open-hands', 'Open Hands', 3, 1, 3, 'passive', 'hand', 'Unbind gives you and Mira back 4 Mana per rank.', {"support": 1})]},
 'paladin': {
  'oathguard': [N('og-iron-oath', 'Iron Oath', 3, 1, 3, 'passive', 'shield', 'Under the Ward you take 2% less per rank.', {"mitigation": 1})],
  'judicator': [N('ju-sentence-passed', 'Sentence Passed', 3, 1, 3, 'passive', 'target', 'Judgement hits 3% harder per rank.', {"damage": 1})],
  'sanctuary': [N('sa-shared-mercy', 'Shared Mercy', 3, 1, 3, 'passive', 'heart', 'Lay On also mends the rest of your party within 10m for 3% of their maximum health per rank.', {"healing": 1})]},
 'ranger': {
  'marksman': [N('mk-practised-draw', 'Practised Draw', 3, 1, 3, 'modifier', 'clock', 'Aimed Shot is drawn 0.1s quicker per rank.', {"tempo": 1})],
  'beastbond': [N('bb-scarred-hide', 'Scarred Hide', 3, 0, 3, 'passive', 'shield', 'Your wolf takes 3% less per rank.', {"mitigation": 1}),
                N('bb-shared-hunt', 'Shared Hunt', 3, 1, 1, 'signature', 'paw', "Each of your wolf's bites gives you back 2 Focus.", {"tempo": 1})],
  'pathfinder': [N('pf-lasting-mark', 'Lasting Mark', 3, 1, 3, 'modifier', 'eye', "Hunter's Mark lasts 3s longer per rank.", {"control": 1})]},
 'rogue': {
  'thiefs-grace': [N('tg-gone-clean', 'Gone Clean', 3, 0, 3, 'passive', 'moon', 'Vanish gives back 10 Focus per rank.', {"control": 1}),
                   N('tg-unremarked', 'Unremarked', 3, 2, 2, 'passive', 'eye', 'Mobs notice you 1 metre closer per rank.', {"control": 1})],
  'shardwork': [N('sw-spare-motion', 'Spare Motion', 3, 0, 3, 'passive', 'clock', 'Sinister Strike costs 3 less Focus per rank.', {"control": 1}),
                N('sw-close-work', 'Close Work', 3, 3, 1, 'signature', 'dagger', 'Sinister Strike on a foe under 30% health builds two combo points.', {"control": 1})],
  'locksmith': [N('lk-practised-hands', 'Practised Hands', 3, 0, 3, 'passive', 'hand', 'Kick costs 4 less Focus per rank.', {"control": 1}),
                N('lk-pocket-dust', 'Pocket of Dust', 3, 2, 2, 'passive', 'eye', 'Blind comes back 10 seconds sooner per rank.', {"control": 1})]},
 'saltmender': {
  'salt-speaker': [N('ss-ready-salt', 'Ready Salt', 3, 1, 3, 'modifier', 'crystal', 'Calcify comes back 2s sooner per rank.', {"control": 1})]},
 'scoundrel': {
  'cutthroat': [N('ct-old-grudge', 'Old Grudge', 3, 0, 3, 'passive', 'dagger', 'Settle the Score cuts 3% harder per rank.', {"dps": 1}),
                N('ct-collected', 'Collected', 3, 2, 1, 'modifier', 'coin', 'A debt that comes due gives you back 10 Nerve.', {"dps": 1})],
  'silver-tongue': [N('st-honeyed-words', 'Honeyed Words', 3, 0, 3, 'passive', 'note', 'Stay costs 4 less Nerve per rank.', {"control": 1}),
                    N('st-rude-awakening', 'Rude Awakening', 3, 2, 1, 'modifier', 'wind', 'A foe woken from your Glimmer by a blow is slowed by 30% for 4 seconds.', {"control": 1})],
  'fixer': [N('fx-second-chorus', 'Second Chorus', 3, 0, 3, 'passive', 'note', 'The Rallying Verse lasts 2 seconds longer per rank.', {"support": 1}),
            N('fx-greased-palms', 'Greased Palms', 3, 2, 1, 'modifier', 'coin', 'A Bribe costs half the crowns and holds 3 seconds longer.', {"support": 1})]},
 'shaman': {
  'rainspeaker': [N('rs-soft-rain', 'Soft Rain', 3, 0, 3, 'passive', 'cloud', 'Rain Call costs 3 less Breath per rank.', {"healer": 1}),
                  N('rs-spring-tide', 'Spring Tide', 3, 2, 1, 'passive', 'drop', 'Tide Mend heals 10% more on someone under 35% health.', {"healer": 1})],
  'stormcaller': [N('sc-low-clouds', 'Low Clouds', 3, 0, 3, 'passive', 'cloud', 'Thunderhead costs 3 less Breath per rank.', {"dps": 1}),
                  N('sc-heavy-sky', 'Heavy Sky', 3, 2, 1, 'passive', 'bolt', "Squall's charge builds to 6, not 5.", {"dps": 1})],
  'stonebound': [N('sb-bedrock', 'Bedrock', 3, 0, 3, 'passive', 'stone', 'Earthskin lasts 2s longer per rank.', {"tank": 1}),
                 N('sb-stone-call', 'Stone-Call', 3, 2, 1, 'passive', 'fist', 'Rooting Stamp turns every foe it strikes on you for 3 seconds.', {"tank": 1})],
  'gravewind': [N('gw-far-wind', 'Far Wind', 3, 0, 3, 'passive', 'wind', 'Moor Wind reaches 1 m further per rank.', {"control": 1}),
                N('gw-clinging-mist', 'Clinging Mist', 3, 2, 1, 'passive', 'cloud', 'Mist also slows the foes it holds by 30% for 6 seconds.', {"control": 1})]},
}
# Written nodes built now: (file, id) -> changed fields. The 3-rank ones move from tier 4 to tier 3's free column 0: tier 4 opens at 20
# points spent in the branch, so at the cap of 19 (20 points) a tier-4 node can never be bought.
CHANGE = {
 ('cleric', 'wd-renew'): {"impl": True},
 ('mage', 'bc-ossuary-coat'): {"tier": 3, "col": 0, "impl": True},
 ('ranger', 'sk-second-wind'): {"tier": 3, "col": 0, "name": "Catch Your Breath", "desc": "A kill in melee (within 5m) gives back 5 Focus per rank.", "impl": True},
 ('rogue', 'bk-crowd-pleaser'): {"tier": 3, "col": 0, "impl": True},
}
ORDER = ['id', 'name', 'tier', 'col', 'max', 'req', 'kind', 'icon', 'desc', 'emphasis', 'impl']

def load(f):
    raw = open(os.path.join(T, f + '.json'), 'rb').read().decode('utf-8')
    return raw, json.loads(raw)

def every_id():
    ids = set()
    for f in os.listdir(T):
        if f.endswith('.json'):
            for b in json.loads(open(os.path.join(T, f), encoding='utf-8').read())['branches']:
                for n in b['nodes']: ids.add(n['id'])
    return ids

def compact(n): return json.dumps({k: n[k] for k in ORDER}, ensure_ascii=False)

def ps_block(n, template):
    """A node in the PowerShell ConvertTo-Json layout of shaman.json, cloned from a template node's text."""
    out = []
    for line in template.split('\n'):
        m = re.match(r'^(\s*)"(\w+)":\s\s(.*?)(,?)$', line)
        if m and m.group(2) in n and m.group(2) != 'emphasis':
            out.append(m.group(1) + '"' + m.group(2) + '":  ' + json.dumps(n[m.group(2)], ensure_ascii=False) + m.group(4)); continue
        if m and m.group(2) not in n:   # the emphasis key inside
            (k, v), = n['emphasis'].items()
            out.append(m.group(1) + '"' + k + '":  ' + json.dumps(v) + m.group(4)); continue
        out.append(line)
    return '\n'.join(out)

def main():
    have = every_id()
    for f, branches in ADD.items():
        for b, nodes in branches.items():
            for n in nodes: assert n['id'] not in have, n['id']
    for f in sorted(set(list(ADD) + [k[0] for k in CHANGE])):
        raw, d = load(f)
        crlf = '\r\n' in raw; text = raw.replace('\r\n', '\n'); end_nl = text.endswith('\n')
        if f in ('archivist', 'cleric', 'mage', 'scoundrel'):   # json.dumps(indent=1) layout: rewrite whole
            assert json.dumps(d, indent=1) == text.rstrip('\n'), f
            for b in d['branches']:
                for n in b['nodes']:
                    for k, v in CHANGE.get((f, n['id']), {}).items(): n[k] = v
                b['nodes'].extend(ADD.get(f, {}).get(b['id'], []))
            text = json.dumps(d, indent=1) + ('\n' if end_nl else '')
        elif f == 'shaman':
            for b in d['branches']:
                adds = ADD[f].get(b['id'], [])
                if not adds: continue
                last = b['nodes'][-1]['id']
                i = text.index('"id":  "' + last + '"'); start = text.rindex('\n', 0, i); start = text.rindex('\n', 0, start) + 1
                close = re.compile(r'\n(\s*)\}\n(\s*)\]').search(text, i); stop = close.start() + 1 + len(close.group(1)) + 1
                template = text[start:stop]
                blocks = [ps_block(n, template) for n in adds]
                text = text[:stop] + ''.join(',\n' + bl for bl in blocks) + text[stop:]
        else:   # one compact line a node
            lines = text.split('\n')
            for idx, line in enumerate(lines):
                m = re.match(r'^(\s*)(\{"id": "([^"]+)".*\})(,?)$', line)
                if m and (f, m.group(3)) in CHANGE:
                    n = json.loads(m.group(2)); assert compact(n) == m.group(2), m.group(3)
                    n.update(CHANGE[(f, m.group(3))]); lines[idx] = m.group(1) + compact(n) + m.group(4)
            for b in d['branches']:
                adds = ADD.get(f, {}).get(b['id'], [])
                if not adds: continue
                last = b['nodes'][-1]['id']
                idx = next(k for k, l in enumerate(lines) if l.lstrip().startswith('{"id": "' + last + '"'))
                indent = re.match(r'^(\s*)', lines[idx]).group(1)
                assert not lines[idx].endswith(','), last
                lines[idx] += ','
                new = [indent + compact(n) + (',' if k < len(adds) - 1 else '') for k, n in enumerate(adds)]
                lines[idx + 1:idx + 1] = new
            text = '\n'.join(lines)
        json.loads(text)   # still JSON
        if crlf: text = text.replace('\n', '\r\n')
        open(os.path.join(T, f + '.json'), 'wb').write(text.encode('utf-8'))
        print('wrote', f)

if __name__ == '__main__': main()
