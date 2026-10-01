"""The painted style pass, part 3 (sky, light and ground colour per zone): patch. Usage: python patch_p3.py "<root>"
   (<root> is the Crulanda assets folder; default D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda). Independent of parts 1 and 2:
   it can be applied before or after them (no anchor here is a line they touch).

   Zone JSON (EncounterContent\\Zones, only the named "lighting" fields; everything else is byte-identical):
     oakhaven  a blue sky (skyTint, skyExposure, skyHaze are new), cool fog against a warmer sun, blue skylight, fog end 210 -> 240
     peaks     a warm-white sun against blue skylight, bluer and farther fog, a deeper sky (the three sky fields are new)
     khaven    the dusk kept; a stronger sun over bluer, darker ambient (four fields)
     ashrim    the grey kept; a stronger sun over darker ambient, the fog a little farther (six fields)
   ZonePost.cs     the mountain, ash, gloom and default (meadow) grades.
   ZoneBuilder.cs  a meadow-only ground palette, greener alpine turf, and a runtime grass tint for meadow and mountain.
   Verdant is the reference and is not touched. Nothing here draws from the zone's random stream.

   Everything is checked before anything is written: every code anchor must occur exactly once, every lighting field must
   hold its expected old value, and every JSON must round-trip byte for byte. So a second run, or a run on a tree that has
   moved on, fails loudly and changes nothing."""
import io, json, os, sys
from collections import OrderedDict

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'

ABSENT = object()   # the field must not be there yet (it is added at the end of the lighting block)

# zone -> ordered (field, old, new)
LIGHTING = OrderedDict([
('oakhaven', [
    ('sunIntensity', 1.05, 1.15),
    ('sunColor', '#FFD7A6', '#FFE1B4'),
    ('ambientSky', '#8C96A3', '#8AA3C4'),
    ('ambientEquator', '#7A7465', '#86886E'),
    ('ambientGround', '#3A352C', '#3E4030'),
    ('fogColor', '#A39F91', '#AFC0C6'),
    ('fogEnd', 210, 240),
    ('skyTint', ABSENT, '#788FB8'),
    ('skyExposure', ABSENT, 1.2),
    ('skyHaze', ABSENT, 1.0),
]),
('peaks', [
    ('sunColor', '#EEF2FF', '#FFF3DC'),
    ('ambientSky', '#8C9CB1', '#7FA0CC'),
    ('ambientEquator', '#747B84', '#74808F'),
    ('ambientGround', '#34373C', '#363A40'),
    ('fogColor', '#A7B4C3', '#A6BEDA'),
    ('fogStart', 60, 80),
    ('fogEnd', 240, 300),
    ('skyTint', ABSENT, '#6B87B8'),
    ('skyExposure', ABSENT, 1.15),
    ('skyHaze', ABSENT, 0.9),
]),
('khaven', [
    ('sunIntensity', 1.05, 1.15),
    ('ambientSky', '#6C6588', '#62608E'),
    ('ambientEquator', '#68545E', '#5C4E60'),
    ('ambientGround', '#2C2428', '#282228'),
]),
('ashrim', [
    ('sunIntensity', 0.9, 1.0),
    ('ambientSky', '#7C7B82', '#72717C'),
    ('ambientEquator', '#69676C', '#5E5C64'),
    ('ambientGround', '#2D2B2D', '#262428'),
    ('fogStart', 16, 22),
    ('fogEnd', 90, 105),
]),
])

# The lighting fields a zone may have (ZoneLighting in ZoneDefinition.cs); a field outside this set would be ignored by JsonUtility.
FIELDS = {'sunPitch', 'sunYaw', 'sunIntensity', 'sunColor', 'ambientSky', 'ambientEquator', 'ambientGround', 'fogColor', 'fogStart', 'fogEnd', 'sunHigh', 'skyExposure', 'skyHaze', 'skyTint'}

CODE = [
(r'Scripts\World\ZonePost.cs', [
("""                case "mountain": return new Grade { saturation = 1.08f, contrast = 1.2f, exposure = .95f, vignette = .55f, bloom = .42f, threshold = 1.1f, tint = new Color(.97f, 1, 1.04f) };""",
 """                case "mountain": return new Grade { saturation = 1.2f, contrast = 1.2f, exposure = .95f, vignette = .5f, bloom = .42f, threshold = 1.1f, tint = new Color(.99f, 1, 1.02f) };   // the cool cast is in the skylight (peaks.json), so sunlit faces stay warm"""),
("""                case "ash": return new Grade { saturation = .7f, contrast = 1.16f, exposure = .95f, vignette = .8f, bloom = .6f, threshold = .95f, tint = new Color(.99f, .985f, 1.02f) };""",
 """                case "ash": return new Grade { saturation = .7f, contrast = 1.24f, exposure = .97f, vignette = .85f, bloom = .45f, threshold = 1.05f, tint = new Color(.99f, .985f, 1.02f) };   // less glow and more contrast, so the pale fogged ground is not lifted to a milky mid-grey"""),
("""                case "gloom": return new Grade { saturation = .82f, contrast = 1.12f, exposure = 1.04f, vignette = .85f, bloom = .6f, threshold = .95f, tint = new Color(1.05f, .95f, 1.03f) };   // Khaven: drained, rose-violet dusk""",
 """                case "gloom": return new Grade { saturation = .86f, contrast = 1.2f, exposure = 1.04f, vignette = .85f, bloom = .65f, threshold = .9f, tint = new Color(1.04f, .96f, 1.02f) };   // Khaven: drained, rose-violet dusk; deeper darks so the lit windows and the low sun carry it"""),
("""                default: return new Grade { saturation = 1.18f, contrast = 1.14f, exposure = 1f, vignette = .6f, bloom = .55f, threshold = 1f, tint = new Color(1.03f, 1, .95f) };""",
 """                default: return new Grade { saturation = 1.28f, contrast = 1.12f, exposure = 1.03f, vignette = .55f, bloom = .62f, threshold = .95f, tint = new Color(1.02f, 1.01f, .98f) };   // Oakhaven: a clear pastoral day, warm light and cool distance"""),
]),
(r'Scripts\World\ZoneBuilder.cs', [
# Grass tufts: the baked tints are shared by every zone, so meadow and mountain are tinted here, as Verdant is.
("""                var lush = Zone.biome == "verdant" ? art.grass.Select(m => new Material(m) { name = m.name + " (lush)", color = Color.Lerp(m.color, new Color(.3f, .62f, .22f), .55f), enableInstancing = true }).ToArray() : grass;""",
 """                // Meadow and mountain: the shared tufts are yellow-olive, so they are pulled toward each zone's turf green.
                var lush = Zone.biome == "verdant" ? art.grass.Select(m => new Material(m) { name = m.name + " (lush)", color = Color.Lerp(m.color, new Color(.3f, .62f, .22f), .55f), enableInstancing = true }).ToArray()
                    : Zone.biome == "meadow" ? art.grass.Select(m => new Material(m) { name = m.name + " (meadow)", color = Color.Lerp(m.color, new Color(.34f, .6f, .2f), .4f), enableInstancing = true }).ToArray()
                    : Zone.biome == "mountain" ? art.grass.Select(m => new Material(m) { name = m.name + " (alpine)", color = Color.Lerp(m.color, new Color(.3f, .5f, .25f), .35f), enableInstancing = true }).ToArray()
                    : grass;"""),
# The meadow's own ground palette; the defaults above stay the base for the other biomes.
("""            if (Zone.biome == "mountain") { dirt = new Color(.4f, .35f, .28f); rut = new Color(.31f, .27f, .21f); }   // yards and roads a shade darker in the hard light""",
 """            if (Zone.biome == "mountain") { dirt = new Color(.4f, .35f, .28f); rut = new Color(.31f, .27f, .21f); }   // yards and roads a shade darker in the hard light
            if (Zone.biome == "meadow") { grassA = new Color(.22f, .40f, .16f); grassB = new Color(.35f, .47f, .19f); grassC = new Color(.50f, .44f, .20f); dirt = new Color(.49f, .39f, .25f); rut = new Color(.36f, .28f, .19f); }   // Oakhaven: fresh pasture green with late-summer gold patches; warm trodden earth"""),
# Alpine turf: greener at about the same value, so the balance against rock and scree holds.
("""                        Color alp = Color.Lerp(new Color(.27f, .31f, .18f), new Color(.38f, .36f, .22f), n1);""",
 """                        Color alp = Color.Lerp(new Color(.24f, .36f, .17f), new Color(.38f, .41f, .2f), n1);"""),
]),
]

def dump(d): return json.dumps(d, indent=2, ensure_ascii=False) + '\n'

out = []   # (path, text)
for zone, fields in LIGHTING.items():
    path = os.path.join(ROOT, 'EncounterContent', 'Zones', zone + '.json')
    assert os.path.isfile(path), ('missing file', path)
    s = io.open(path, encoding='utf-8', newline='').read()
    d = json.loads(s, object_pairs_hook=OrderedDict)
    assert dump(d) == s, ('the file does not round-trip byte for byte; edit it by hand', path)
    assert d.get('id') == 'zone.' + zone,('not this zone', path, d.get('id'))
    light = d['lighting']
    for name, old, new in fields:
        assert name in FIELDS, ('not a ZoneLighting field', name)
        if old is ABSENT: assert name not in light, ('already there (already patched?)', path, name, light.get(name))
        else: assert name in light and light[name] == old and type(light[name]) == type(old), ('unexpected old value (already patched?)', path, name, light.get(name), old)
        light[name] = new
    assert set(light) <= FIELDS, ('unknown lighting field', path, sorted(set(light) - FIELDS))
    out.append((path, dump(d)))

for rel, edits in CODE:
    path = os.path.join(ROOT, rel)
    assert os.path.isfile(path), ('missing file', path)
    s = io.open(path, encoding='utf-8', newline='').read()
    eol = '\r\n' if '\r\n' in s else '\n'
    for a, b in edits:
        a = a.replace('\n', eol); b = b.replace('\n', eol)
        assert s.count(a) == 1, ('anchor must occur exactly once', path, s.count(a), a.strip()[:90])
        s = s.replace(a, b)
    out.append((path, s))

for path, s in out:
    io.open(path, 'w', encoding='utf-8', newline='').write(s); print('patched', os.path.basename(path))
print('P3 OK: %d files' % len(out))
