"""Morro Motion's "Idle MoCap" (Unity Asset Store, standard EULA; Chris added it 2026-10-05: "use the idle mocap in the asset for
npcs standing around or when your character just stands"): the ten motion-captured idles (stances, looking around, a cough,
four cold idles, a greeting stance), each with its package .meta, into Resources/Characters/Animations/Morro. The demo man,
scene and materials stay out. Run again to refresh."""
import io, os, tarfile
PKG = r'C:\Users\chris\AppData\Roaming\Unity\Asset Store-5.x\Morro Motion\AnimationBipedal\Idle MoCap.unitypackage'
DEST = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Resources\Characters\Animations\Morro'
t = tarfile.open(PKG, 'r:gz'); ent = {}
for m in t.getmembers():
    g, _, part = m.name.partition('/')
    if part: ent.setdefault(g, {})[part] = m
n = 0
for g, parts in ent.items():
    if 'pathname' not in parts or 'asset' not in parts: continue
    path = t.extractfile(parts['pathname']).read().decode('utf-8').splitlines()[0]
    if not path.startswith('Assets/Idle MoCap/Animations/') or not path.endswith('.fbx'): continue
    os.makedirs(DEST, exist_ok=True); out = os.path.join(DEST, os.path.basename(path))
    io.open(out, 'wb').write(t.extractfile(parts['asset']).read())
    if 'asset.meta' in parts: io.open(out + '.meta', 'wb').write(t.extractfile(parts['asset.meta']).read())
    n += 1
io.open(os.path.join(DEST, 'LICENSE.txt'), 'w', encoding='utf-8').write(
    'Idle MoCap by Morro Motion (Unity Asset Store), under the standard Unity Asset Store EULA. Downloaded by Chris Underwood\n'
    '2026-10-05 to his Asset Store account; used in this game, not to be redistributed as assets.\n')
print('copied', n)
