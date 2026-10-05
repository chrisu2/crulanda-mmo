"""Brings in the creatures Chris picked on 2026-10-04 (after the treants), each with its package's own .meta so its GUID is kept:

- ChillLands' "Ashen Marches - Free 100" (itch.io; free for use in games, no redistribution of the source files, see its
  LICENSE.txt; Chris downloaded it 2026-10-04; the zip is kept in tools/wip/animals/source, not committed): the crypt spider (our spiders), the carrion
  raven (the village crows) and the ossuary knight (Khaven's risen dead), as Resources/Creatures/Spider.fbx, Raven.fbx and
  Skeleton.fbx, each with its palette texture beside it (<Kind>_palette.png).
- Blink's "FREE Stylized Bear - RPG Forest Animal" (Unity Asset Store, standard EULA; in Chris's Asset Store cache): the mesh as
  Resources/Creatures/Bear.fbx, its clips (a file each) in Resources/Creatures/BearClips, and its albedo and normal map in
  Resources/CreatureSkins/Bear.

Run again to refresh; it overwrites what it copied."""
import io, os, tarfile, zipfile
GAME = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Resources'
CREAT = os.path.join(GAME, 'Creatures')

def entries(t):
    out = {}
    for m in t.getmembers():
        guid, _, part = m.name.partition('/')
        if part: out.setdefault(guid, {})[part] = m
    for guid, parts in out.items():
        if 'pathname' in parts and 'asset' in parts:
            yield t.extractfile(parts['pathname']).read().decode('utf-8').splitlines()[0], parts

def put(t, parts, dest):
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    io.open(dest, 'wb').write(t.extractfile(parts['asset']).read())
    if 'asset.meta' in parts: io.open(dest + '.meta', 'wb').write(t.extractfile(parts['asset.meta']).read())
    print('  ', os.path.relpath(dest, GAME))

# Ashen Marches
z = zipfile.ZipFile(r'D:\code\mmo\tools\wip\animals\source\AshenMarches_Free100_Unity.zip')   # Chris's download, kept here (not committed)
t = tarfile.open(fileobj=io.BytesIO(z.read('AshenMarches_Free100.unitypackage')), mode='r:gz')
ASHEN = {'wld_crea_crypt_spider': 'Spider', 'wld_anim_carrion_raven': 'Raven', 'anc_crea_ossuary_knight': 'Skeleton'}
for path, parts in entries(t):
    for src, kind in ASHEN.items():
        if path == 'Assets/AshenMarches/Models/%s/%s.fbx' % (src, src): put(t, parts, os.path.join(CREAT, kind + '.fbx'))
        if path == 'Assets/AshenMarches/Models/%s/palette.png' % src: put(t, parts, os.path.join(CREAT, kind + '_palette.png'))
io.open(os.path.join(CREAT, 'LICENSE-AshenMarches.txt'), 'w', encoding='utf-8').write(
    'Spider.fbx, Raven.fbx, Skeleton.fbx and their palettes: from "Ashen Marches - Free 100" by ChillLands\n'
    '(https://chilllands.itch.io/ashen-marches-free), downloaded by Chris Underwood 2026-10-04. Free for use in finished games;\n'
    'the source files are not to be redistributed on their own. Its license:\n\n' + z.read('LICENSE.txt').decode('utf-8', 'replace'))

# Blink's bear
PKG = r'C:\Users\chris\AppData\Roaming\Unity\Asset Store-5.x\Blink\3D ModelsCharactersAnimals\FREE Stylized Bear - RPG Forest Animal.unitypackage'
t = tarfile.open(PKG, 'r:gz')
B = 'Assets/Blink/Art/Animals/Stylized/Bear/'
for path, parts in entries(t):
    if path == B + 'Bear_Meshes/Bear.fbx': put(t, parts, os.path.join(CREAT, 'Bear.fbx'))
    elif path.startswith(B + 'Bear_Animations/') and path.endswith('.fbx'): put(t, parts, os.path.join(CREAT, 'BearClips', os.path.basename(path)))
    elif path == B + 'Bear_Textures/Stylized_Bear_Albedo4.png': put(t, parts, os.path.join(GAME, 'CreatureSkins', 'Bear', 'Bear_Albedo.png'))
    elif path == B + 'Bear_Textures/Stylized_Bear_Normal.png': put(t, parts, os.path.join(GAME, 'CreatureSkins', 'Bear', 'Bear_Normal.png'))
io.open(os.path.join(GAME, 'CreatureSkins', 'Bear', 'LICENSE.txt'), 'w', encoding='utf-8').write(
    'Bear.fbx, BearClips and these textures: "FREE Stylized Bear - RPG Forest Animal" by Blink (Unity Asset Store, package 228910),\n'
    'under the standard Unity Asset Store EULA. Downloaded by Chris Underwood 2026-10-04 to his Asset Store account; used in this\n'
    'game, not to be redistributed as assets.\n')
print('ok')
