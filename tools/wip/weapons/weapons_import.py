"""Brings in the weapon and chest packs Chris added to his Asset Store account on 2026-10-05 ("ive added some new props,
weapons, animated chest to the assets ... use in the loot table. some of the glowing weapons should be epic or legendary only"),
each file with its package's own .meta so GUIDs are kept:

- Blink, "FREE - Low Poly Swords - RPG Weapons" (15 swords, 51 colourways) and "FREE - RPG Weapons" (axes, maces, spears,
  staff, wand, bow, shield; basic, medium and epic each);
- Lumo-Art 3D, "FREE Cartoon Weapon Pack - Mobile/VR" (swords, staves, shields, a dagger, an espadon);
- SICS Games, "Low Poly Weapons" (a dagger, bow, crossbow; potions and bombs kept for later);
- quiArt, "Animated PBR Chest Demo" (a wooden chest whose lid opens).

All under the standard Unity Asset Store EULA: used in this game, not to be redistributed as assets. The models, materials and
textures go to Assets/Crulanda/ThirdParty/<pack>/ (as the package lays them out); each prefab the game loads by name goes to
Resources/Weapons/ (Resources/Props/ for the chest). Demo scenes, ground materials and lighting data stay out. Run again to refresh.
"""
import io, os, tarfile
STORE = r'C:\Users\chris\AppData\Roaming\Unity\Asset Store-5.x'
ASSETS = r'D:\code\mmo\New Unity Project\Assets\Crulanda'
PACKS = [
    ('Blink/3D ModelsPropsWeapons/FREE - Low Poly Swords - RPG Weapons.unitypackage', 'Assets/Blink/Art/Weapons/LowPoly/FreeSwords/', 'BlinkSwords', 'Weapons'),
    ('Blink/3D ModelsPropsWeapons/FREE - RPG Weapons.unitypackage', 'Assets/Blink/Art/Weapons/LowPoly/FreeRPGWeapons/', 'BlinkRPG', 'Weapons'),
    ('Lumo-Art 3D/3D ModelsPropsWeapons/FREE Cartoon Weapon Pack - MobileVR.unitypackage', 'Assets/Cartoon_Weapon_Pack/', 'CartoonWeapons', 'Weapons'),
    ('SICS Games/3D ModelsPropsWeapons/Low Poly Weapons.unitypackage', 'Assets/Low-Poly Weapons/', 'SicsWeapons', 'Weapons'),
    ('quiArt/3D ModelsPropsInterior/Animated PBR Chest Demo.unitypackage', 'Assets/Animated PBR Chest Demo/', 'Chest', 'Props'),
    ('Sergi Nicols/3D ModelsPropsWeapons/Staff Of Pain.unitypackage', 'Assets/StaffOfPain/', 'StaffOfPain', 'Weapons'),   # Chris: the legendary staff
]
SKIP = ('.unity', 'readme', 'Documentation', '.lighting', '.exr', '.odt', 'LightingData', 'Ground', 'DemoScene', 'Demo_', 'Plane D', 'DemoPlane')
n = 0
for pkg, root, dest, kind in PACKS:
    t = tarfile.open(os.path.join(STORE, pkg), 'r:gz'); ent = {}
    for m in t.getmembers():
        g, _, part = m.name.partition('/')
        if part: ent.setdefault(g, {})[part] = m
    for g, parts in ent.items():
        if 'pathname' not in parts or 'asset' not in parts: continue
        path = t.extractfile(parts['pathname']).read().decode('utf-8').splitlines()[0]
        if not path.startswith(root) or any(s in path for s in SKIP): continue
        rel = path[len(root):]
        if path.endswith('.prefab'):
            if 'Camera' in path or '_Fx_' in path: continue
            # The two small packs name their prefabs plainly (Bow, Dagger, Sword): prefixed, so none overwrites another.
            pre = 'Cartoon_' if dest == 'CartoonWeapons' else 'Sics_' if dest == 'SicsWeapons' else ''
            out = os.path.join(ASSETS, 'Resources', kind, pre + os.path.basename(path).replace('Animated PBR Chest _Wood_Demo', 'Chest_Wood'))
        else: out = os.path.join(ASSETS, 'ThirdParty', dest, rel)
        os.makedirs(os.path.dirname(out), exist_ok=True)
        io.open(out, 'wb').write(t.extractfile(parts['asset']).read())
        if 'asset.meta' in parts: io.open(out + '.meta', 'wb').write(t.extractfile(parts['asset.meta']).read())
        n += 1
# ChillLands' Ashen Marches Free (itch.io; its own licence, see tools/wip/animals/creatures_import.py): its eight pieces of
# equipment (Chris, 2026-10-05: "more armor"), their shared materials and palette, and their prefabs (prefixed Ashen_).
import zipfile
z = zipfile.ZipFile(r'D:\code\mmo\tools\wip\animals\source\AshenMarches_Free100_Unity.zip')
t = tarfile.open(fileobj=io.BytesIO(z.read('AshenMarches_Free100.unitypackage')), mode='r:gz'); ent = {}
for m in t.getmembers():
    g, _, part = m.name.partition('/')
    if part: ent.setdefault(g, {})[part] = m
for g, parts in ent.items():
    if 'pathname' not in parts or 'asset' not in parts: continue
    path = t.extractfile(parts['pathname']).read().decode('utf-8').splitlines()[0]
    root = 'Assets/AshenMarches/'
    if not (('_equi_' in path and (path.startswith(root + 'Models/') or path.startswith(root + 'Prefabs/'))) or path.startswith(root + 'Materials/AM_') or path == root + 'Palette.png'): continue
    if path.endswith('.prefab'): out = os.path.join(ASSETS, 'Resources', 'Weapons', 'Ashen_' + os.path.basename(path))
    else: out = os.path.join(ASSETS, 'ThirdParty', 'AshenEquipment', path[len(root):])
    os.makedirs(os.path.dirname(out), exist_ok=True)
    io.open(out, 'wb').write(t.extractfile(parts['asset']).read())
    if 'asset.meta' in parts: io.open(out + '.meta', 'wb').write(t.extractfile(parts['asset.meta']).read())
    n += 1
io.open(os.path.join(ASSETS, 'ThirdParty', 'AshenEquipment', 'LICENSE.txt'), 'w', encoding='utf-8').write(
    'From "Ashen Marches - Free 100" by ChillLands (https://chilllands.itch.io/ashen-marches-free). Free for use in finished games;\n'
    'the source files are not to be redistributed on their own (see Resources/Creatures/LICENSE-AshenMarches.txt).\n')
for _, _, dest, _ in PACKS:
    d = os.path.join(ASSETS, 'ThirdParty', dest); os.makedirs(d, exist_ok=True)
    io.open(os.path.join(d, 'LICENSE.txt'), 'w', encoding='utf-8').write(
        'From the Unity Asset Store (see tools/wip/weapons/weapons_import.py), under the standard Asset Store EULA. Downloaded by\n'
        'Chris Underwood 2026-10-05 to his Asset Store account; used in this game, not to be redistributed as assets.\n')
print('copied', n)
