"""The crypt spider's palette (ChillLands, Ashen Marches) recoloured for our spiders (2026-10-04), beside it in Resources/Creatures:
Spider_palette_charnel (Khaven's charnel spiders: bone and ash), _basalt (the Ridge of Long Shadows: black stone, a little blue)
and _canopy (the Verdant canopy: moss green). Run again to refresh."""
from PIL import Image
import os
D = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Resources\Creatures'
src = Image.open(os.path.join(D, 'Spider_palette.png')).convert('RGB')
def make(name, f):
    out = Image.new('RGB', src.size); po = out.load(); ps = src.load()
    for y in range(src.size[1]):
        for x in range(src.size[0]):
            r, g, b = ps[x, y]; l = .3 * r + .59 * g + .11 * b
            po[x, y] = tuple(max(0, min(255, int(c))) for c in f(r, g, b, l))
    out.save(os.path.join(D, 'Spider_palette_' + name + '.png'))
make('charnel', lambda r, g, b, l: (l * 1.12 + 18, l * 1.06 + 14, l * .9 + 6))
make('basalt', lambda r, g, b, l: (l * .34 + r * .06, l * .35, l * .4 + b * .06))
make('canopy', lambda r, g, b, l: (l * .55 + r * .1, l * .92 + g * .1, l * .42))
print('ok')
