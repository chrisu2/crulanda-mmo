"""quaternius_import.py - brings the Quaternius character kits (CC0) into the project (playtest note 12: Chris chose real
models over the code-built figure; 2026-10-02). From the three free [Standard] zips it takes what the game uses:

    Universal Base Characters   Superhero_Male/Female_FullBody.fbx (Unity), their skin, eye and hair textures, and the
                                hairstyles and eyebrows rigged to the head bone (Unity FBX)
    Modular Character Outfits   the Peasant and Ranger outfits, male and female (Unity FBX), and their textures
    Universal Animation Library UAL1_Standard.fbx (Unity): the humanoid clips

into New Unity Project/Assets/Crulanda/Resources/Characters/{Bodies,Hair,Outfits,Animations,Textures}, with the licences.
Textures for the Standard shader: base colours as they are; normal maps (the packs' OpenGL-style ones, as Unity wants);
the packed ORM (occlusion, roughness, metallic) and the bodies' roughness maps become a MetallicSmoothness map (R metallic,
A smoothness = 1 - roughness) and an Occlusion map (G).

    python quaternius_import.py <folder with the three zips>
"""
import io, os, sys, zipfile
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'New Unity Project', 'Assets', 'Crulanda', 'Resources', 'Characters'))
SRC = sys.argv[1] if len(sys.argv) > 1 else '.'

ZIPS = {'base': 'Universal Base Characters[Standard].zip', 'outfits': 'Modular Character Outfits - Fantasy[Standard].zip',
        'anims': 'Universal Animation Library[Standard].zip'}


def put(rel, data):
    p = os.path.join(OUT, rel); os.makedirs(os.path.dirname(p), exist_ok=True)
    open(p, 'wb').write(data); return p


def metal_smooth(img_bytes, packed):
    """packed ORM -> (MetallicSmoothness RGBA, Occlusion); a plain roughness map -> (MetallicSmoothness, None)."""
    im = Image.open(io.BytesIO(img_bytes))
    if packed:
        im = im.convert('RGB'); o, r, m = im.split()
        a = r.point(lambda v: 255 - v)
        ms = Image.merge('RGBA', (m, m, m, a)); occ = Image.merge('RGB', (o, o, o))
        return ms, occ
    r = im.convert('L'); a = r.point(lambda v: 255 - v); z = Image.new('L', r.size, 0)
    return Image.merge('RGBA', (z, z, z, a)), None


def png(im):
    b = io.BytesIO(); im.save(b, 'PNG', optimize=True); return b.getvalue()


def main():
    z = {k: zipfile.ZipFile(os.path.join(SRC, v)) for k, v in ZIPS.items()}
    n = 0
    # Bodies, and their textures (the Unity normals).
    for name in z['base'].namelist():
        if name.endswith('/'): continue
        tail = name.split('[Standard]/', 1)[1]
        if tail.startswith('Base Characters/Unity/') and tail.endswith('.fbx'): put('Bodies/' + os.path.basename(tail), z['base'].read(name)); n += 1
        elif tail.startswith('Hairstyles/Rigged to Head Bone/FBX (Unity)/') and tail.endswith('.fbx'): put('Hair/' + os.path.basename(tail), z['base'].read(name)); n += 1
        elif tail.startswith('Base Characters/Textures/Normals Unity - Godot/') or tail.startswith('Hairstyles/Textures/Normals Unity - Godot/'):
            put('Textures/' + os.path.basename(tail), z['base'].read(name)); n += 1
        elif tail.startswith('Base Characters/Textures/') and tail.count('/') == 2:
            b = os.path.basename(tail)
            if 'Normal' in b: continue   # the Unity normals above
            if 'Roughness' in b:
                ms, _ = metal_smooth(z['base'].read(name), False); put('Textures/' + b.replace('Roughness', 'MetalSmooth'), png(ms)); n += 1
            else:
                b = b.replace('T_Superhero_Male_Ligh.png', 'T_Superhero_Male_Light_BaseColor.png').replace('T_Superhero_Male_Dark.png', 'T_Superhero_Male_Dark_BaseColor.png')
                put('Textures/' + b, z['base'].read(name)); n += 1
        elif tail.startswith('Hairstyles/Textures/') and tail.count('/') == 2 and 'Normal' not in tail:
            put('Textures/' + os.path.basename(tail), z['base'].read(name)); n += 1
        elif tail == 'License_Standard.txt': put('Bodies/License.txt', z['base'].read(name)); n += 1
    # Outfits (whole outfits: one FBX each) and their textures (from the glTF folder: the same images).
    for name in z['outfits'].namelist():
        if name.endswith('/'): continue
        tail = name.split('[Standard]/', 1)[1]
        if tail.startswith('Exports/FBX (Unity)/Outfits/') and tail.endswith('.fbx'): put('Outfits/' + os.path.basename(tail), z['outfits'].read(name)); n += 1
        elif tail.startswith('Exports/glTF (Godot-Unreal)/Outfits/') and tail.endswith('.png'):
            b = os.path.basename(tail)
            if b.endswith('_ORM.png'):
                ms, occ = metal_smooth(z['outfits'].read(name), True)
                put('Textures/' + b.replace('_ORM', '_MetalSmooth'), png(ms)); put('Textures/' + b.replace('_ORM', '_Occlusion'), png(occ)); n += 2
            elif b.endswith('_Roughness.png'):
                ms, _ = metal_smooth(z['outfits'].read(name), False); put('Textures/' + b.replace('_Roughness', '_MetalSmooth'), png(ms)); n += 1
            else: put('Textures/' + b, z['outfits'].read(name)); n += 1
        elif tail == 'License_Standard.txt': put('Outfits/License.txt', z['outfits'].read(name)); n += 1
    # The animation library (the in-place clips; root motion is the game's own).
    for name in z['anims'].namelist():
        tail = name.split('[Standard]/', 1)[1] if '[Standard]/' in name else name
        if tail == 'Unity/UAL1_Standard.fbx': put('Animations/UAL1_Standard.fbx', z['anims'].read(name)); n += 1
        elif tail == 'License.txt': put('Animations/License.txt', z['anims'].read(name)); n += 1
    open(os.path.join(OUT, 'README.txt'), 'w', encoding='utf-8').write(
        'Quaternius character kits (CC0, quaternius.com): Universal Base Characters, Modular Character Outfits - Fantasy\n'
        '(Peasant and Ranger, the free Standard tier) and the Universal Animation Library (Standard). Brought in by\n'
        'tools/wip/characters/quaternius_import.py on 2026-10-02 (playtest note 12). GAME-ONLY art; CC0: no attribution\n'
        'required, credited in the game\'s credits all the same.\n')
    print(n, 'files into', OUT)


if __name__ == '__main__':
    main()
