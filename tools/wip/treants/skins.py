"""The Keepers' bark by kind (Chris, 2026-10-04: "color them differently based on their type"), made from the Treant Pack's own
albedos (CC0) into Resources/CreatureSkins/Treant: <Kind>_Grey (the withered Keepers of the Greying: the pale bark drained of
colour, cool), <Kind>_Rot (the deep's withered, under the Temple: the dark bark half-drained, blackened, a violet cast) and
<Kind>_Glow (an emission mask: the moss, where the living Keepers' sap-light and the void's violet show). Run again to refresh."""
from PIL import Image, ImageChops, ImageFilter
import os
D = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Resources\CreatureSkins\Treant'
def lerp(a, b, t): return a + (b - a) * t
for kind in ('Treant1', 'Treant2'):
    light = Image.open(os.path.join(D, kind + '_Light.png')).convert('RGB')
    dark = Image.open(os.path.join(D, kind + '_Dark.png')).convert('RGB')
    def drain(im, keep, mul):
        px = im.load(); out = Image.new('RGB', im.size); po = out.load(); w, h = im.size
        for y in range(h):
            for x in range(w):
                r, g, b = px[x, y]; l = .3 * r + .59 * g + .11 * b
                po[x, y] = tuple(max(0, min(255, int(lerp(l, c, keep) * m))) for c, m in zip((r, g, b), mul))
        return out
    drain(light, .12, (.93, .96, 1.04)).save(os.path.join(D, kind + '_Grey.png'))
    drain(dark, .35, (.8, .7, .9)).save(os.path.join(D, kind + '_Rot.png'))
    # The moss: where green stands over red and blue (the same places in both albedos), softened.
    px = dark.load(); w, h = dark.size; mask = Image.new('L', dark.size); pm = mask.load()
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]; pm[x, y] = max(0, min(255, int((g - max(r, b)) * 6)))
    mask.filter(ImageFilter.GaussianBlur(2)).convert('RGB').save(os.path.join(D, kind + '_Glow.png'))
    print(kind, 'ok')
