"""Writes a .meta with a fresh guid beside each of Hallow's Creek's new files that has none yet (an existing .meta is never touched: GUIDs
are kept), and registers the new content files where the editor's Crulanda > World > Build Oakhaven would (Encounter.asset's quest, item
and lore files, the Oakhaven scene's zones), so a build without that menu run still has them. Prints what it did.

    python hallows_metas.py
"""
import os, re, uuid
HERE = os.path.dirname(os.path.abspath(__file__))
A = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'New Unity Project', 'Assets', 'Crulanda'))
TEXT = """fileFormatVersion: 2
guid: {g}
TextScriptImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""
MONO = """fileFormatVersion: 2
guid: {g}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""
FILES = ['EncounterContent/Zones/hallows.json', 'EncounterContent/Items/loot.hallows.json', 'EncounterContent/Quests/hallows.json', 'EncounterContent/Lore/hallows.json',
         'Resources/Lore/hallows.json', 'Scripts/World/ZoneBuilder.Hallows.cs', 'Scripts/World/ZoneBuilder.HallowsGround.cs', 'Scripts/Encounter/EncounterSession.Hallows.cs',
         'Tests/EditMode/HallowsDataTests.cs', 'Tests/PlayMode/HallowsNavTests.cs']

def guid_of(rel):
    m = re.search(r'guid: ([0-9a-f]{32})', open(os.path.join(A, rel + '.meta'), encoding='utf-8').read()); return m.group(1)

def metas():
    for rel in FILES:
        p = os.path.join(A, rel)
        if not os.path.exists(p): print('missing', rel); continue
        if os.path.exists(p + '.meta'): print('kept', rel + '.meta', guid_of(rel)); continue
        with open(p + '.meta', 'w', encoding='utf-8', newline='\n') as f: f.write((MONO if rel.endswith('.cs') else TEXT).format(g=uuid.uuid4().hex))
        print('wrote', rel + '.meta', guid_of(rel))

def register(path, key, new_rel, before_name):
    """Adds the file's reference to the list under key: in the asset (after the existing entries, or in name order before before_name)."""
    s = open(path, encoding='utf-8').read(); g = guid_of(new_rel)
    if g in s: print('already listed in', os.path.basename(path), key); return
    line = '  - {fileID: 4900000, guid: %s, type: 3}\n' % g
    i = s.index('  ' + key + ':\n') + len('  ' + key + ':\n')
    j = i
    while s.startswith('  - ', j):
        if before_name:
            m = re.search(r'guid: ([0-9a-f]{32})', s[j:s.index('\n', j)])
            name = names.get(m.group(1)) if m else None
            if name is not None and name > before_name: break
        j = s.index('\n', j) + 1
    s = s[:j] + line + s[j:]
    with open(path, 'w', encoding='utf-8', newline='\n') as f: f.write(s)
    print('listed', new_rel, 'in', os.path.basename(path), key)

names = {}
def index_names():
    for root, _, files in os.walk(os.path.join(A, 'EncounterContent')):
        for fn in files:
            if fn.endswith('.json.meta'):
                m = re.search(r'guid: ([0-9a-f]{32})', open(os.path.join(root, fn), encoding='utf-8').read())
                if m: names[m.group(1)] = fn[:-len('.json.meta')]

if __name__ == '__main__':
    metas(); index_names()
    asset = os.path.join(A, 'EncounterContent', 'Encounter.asset')
    register(asset, 'questFiles', 'EncounterContent/Quests/hallows.json', None)
    register(asset, 'itemFiles', 'EncounterContent/Items/loot.hallows.json', 'loot.hallows')
    register(asset, 'loreFiles', 'EncounterContent/Lore/hallows.json', None)
    register(os.path.join(A, 'Scenes', 'Oakhaven.unity'), 'zones', 'EncounterContent/Zones/hallows.json', None)
