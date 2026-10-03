"""animals_import.py - brings Quaternius's animated animals (CC0) into the project (2026-10-03: real animal models, after the
people of playtest note 12). From the Ultimate Animated Animal Pack (July 2021; quaternius.com, mirrored on poly.pizza as the
"Animated Animal Pack" bundle, whose FBX download is a zip of one folder per animal) it takes the animals the game uses:

    Wolf    wolves, and ash hounds (recoloured charcoal, eyes like embers)
    Stag    the forest stag, and an antlered hill deer
    Deer    the doe, and the hill deer without antlers

into New Unity Project/Assets/Crulanda/Resources/Creatures/, with a licence note. More animals: name them after the zip.

    python animals_import.py "<Animated Animal Pack-zip.zip>" [Fox Horse ...]

The zip is kept at tools/wip/animals/source/AnimatedAnimalPack.zip (git-ignored; 18 MB). It also holds Alpaca, Bull, Cow, Donkey,
Fox, Horse, Horse_White, Husky and ShibaInu. To fetch it again: poly.pizza/bundle/Animated-Animal-Pack-ILAPXeUYiS, "Download FBX"
(a direct zip; the single-model downloads sit behind a bot check, and Quaternius's own Google Drive runs out of quota).
"""
import os, sys, zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, '..', '..', '..', 'New Unity Project', 'Assets', 'Crulanda', 'Resources', 'Creatures'))
WANT = ['Wolf', 'Stag', 'Deer']

LICENCE = """Ultimate Animated Animal Pack by Quaternius (quaternius.com, July 2021), CC0 1.0 Universal (public domain):
https://creativecommons.org/publicdomain/zero/1.0/  No attribution required; credited in the game's credits all the same.
Taken from the poly.pizza bundle "Animated Animal Pack" (FBX) on 2026-10-03.
"""


def main():
    src = sys.argv[1]
    want = WANT + sys.argv[2:]
    z = zipfile.ZipFile(src)
    os.makedirs(OUT, exist_ok=True)
    got = []
    for name in z.namelist():
        base = os.path.basename(name)
        stem = os.path.splitext(base)[0]
        if base.endswith('.fbx') and stem in want:
            open(os.path.join(OUT, base), 'wb').write(z.read(name)); got.append(base)
    open(os.path.join(OUT, 'License.txt'), 'w', encoding='utf-8').write(LICENCE)
    missing = [w for w in want if w + '.fbx' not in got]
    print('copied', ', '.join(sorted(got)), '->', OUT)
    if missing: print('MISSING', missing); sys.exit(1)


if __name__ == '__main__':
    main()
