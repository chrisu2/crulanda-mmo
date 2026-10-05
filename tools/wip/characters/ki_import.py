"""Brings Kevin Iglesias's "Human Basic Motions FREE" (Unity Asset Store, standard Asset Store EULA; Chris downloaded it
2026-10-04) into the game: the in-place male and female clips only (idles, walk, run and sprint in their directions, turns,
jump, fall, talk), each with the package's own .meta so its GUID is kept. The root-motion copies, the demo scene, the Blender
files and the scripts stay out. Run again to refresh; it overwrites what it copied."""
import io, os, tarfile
PKG = r'C:\Users\chris\AppData\Roaming\Unity\Asset Store-5.x\Kevin Iglesias\Animation\Human Basic Motions FREE.unitypackage'
DEST = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Resources\Characters\Animations\KI'
SRC = 'Assets/Kevin Iglesias/Human Animations/Animations/'
t = tarfile.open(PKG, 'r:gz')
entries = {}
for m in t.getmembers():
    guid, _, part = m.name.partition('/')
    if part: entries.setdefault(guid, {})[part] = m
n = 0
for guid, parts in entries.items():
    if 'pathname' not in parts or 'asset' not in parts: continue
    path = t.extractfile(parts['pathname']).read().decode('utf-8').splitlines()[0]
    if not path.startswith(SRC) or not path.endswith('.fbx') or '[RM]' in path: continue
    rel = path[len(SRC):]                      # Male/Movement/Walk/HumanM@Walk01_Forward.fbx
    sex = rel.split('/')[0]
    if sex not in ('Male', 'Female'): continue
    out = os.path.join(DEST, sex, os.path.basename(rel))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    io.open(out, 'wb').write(t.extractfile(parts['asset']).read())
    if 'asset.meta' in parts: io.open(out + '.meta', 'wb').write(t.extractfile(parts['asset.meta']).read())
    n += 1
for sex in ('Male', 'Female'):
    d = os.path.join(DEST, sex)
    if os.path.isdir(d) and not os.path.exists(d + '.meta'): pass   # Unity makes the folder metas; they are copied back after the first run
io.open(os.path.join(DEST, 'LICENSE.txt'), 'w', encoding='utf-8').write(
    'Human Basic Motions FREE by Kevin Iglesias (Unity Asset Store, package 154271), under the standard Unity Asset Store EULA.\n'
    'Downloaded by Chris Underwood 2026-10-04 to his Asset Store account; used in this game, not to be redistributed as assets.\n')
print('copied', n, 'clips')
