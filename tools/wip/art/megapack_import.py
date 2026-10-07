"""Brings in the Stylized Megapack 2in1's medieval half and the HQ Rock Pack (art round 3, 2026-10-07).

- "Stylized Megapack 2in1" (Downloads, Asset Store EULA): only its "Stylized Medieval Kingdom URP" half (the Asia half is off
  the world's look). Models, materials and textures go to Assets/Crulanda/Resources/Props/Megapack/ with their own .meta files
  (GUIDs kept, so each FBX's externalObjects still finds its material). Scenes, the sky, the ground and grass textures, the
  shader graphs and the prefabs stay out. The materials are URP Lit; the project is the built-in pipeline, so each .mat is
  rewritten to the Standard shader here, keeping its _MainTex and _BumpMap, its _BaseColor as _Color and its _Smoothness as
  _Glossiness; the few shader-graph materials (plants, wheat, pines) become Standard cutout; water is left out.
- "HQ Rock Pack Free" (DNKDEV, Asset Store cache): its three rocks, materials and textures to Resources/Props/HQRocks/.
  LEFT OUT on 2026-10-07: photoreal scans clash with the painted world (PropCapture row); the import below is kept, its
  output deleted. Re-run and delete nothing to bring it back.

Run again to refresh. Textures are capped by ThirdPartyImport (Resources/Props/...).
"""
import io, os, re, tarfile
ASSETS = r'D:\code\mmo\New Unity Project\Assets\Crulanda\Resources\Props'
STANDARD = '{fileID: 46, guid: 0000000000000000f000000000000000, type: 0}'
URP_LIT = '933532a4fcc9baf4fa0491de14d08ed7'

def entries(pkg):
    t = tarfile.open(pkg, 'r:gz'); ent = {}
    for m in t.getmembers():
        g, _, part = m.name.partition('/')
        if part: ent.setdefault(g, {})[part] = m
    for g, parts in ent.items():
        if 'pathname' not in parts: continue
        yield t, t.extractfile(parts['pathname']).read().decode('utf-8', 'ignore').splitlines()[0], parts

def standardise(text):
    """A URP (or shader-graph) material as a Standard one, its maps and colour kept."""
    lit = URP_LIT in text
    text = re.sub(r'm_Shader: \{[^}]*\}', 'm_Shader: ' + STANDARD, text, count=1)
    base = re.search(r'- _BaseColor: \{([^}]*)\}', text)
    if base and re.search(r'- _Color: \{[^}]*\}', text): text = re.sub(r'- _Color: \{[^}]*\}', '- _Color: {' + base.group(1) + '}', text, count=1)
    sm = re.search(r'- _Smoothness: ([0-9.eE-]+)', text)
    if sm and re.search(r'- _Glossiness: [0-9.eE-]+', text): text = re.sub(r'- _Glossiness: [0-9.eE-]+', '- _Glossiness: ' + str(min(float(sm.group(1)), .35)), text, count=1)
    if not lit:   # a shader graph (grass, plants, wheat, pine needles): Standard cutout
        text = re.sub(r'm_ValidKeywords: \[\]', 'm_ValidKeywords:\n  - _ALPHATEST_ON', text, count=1)
        text = re.sub(r'- _Mode: [0-9.]+', '- _Mode: 1', text, count=1)
        text = re.sub(r'm_CustomRenderQueue: -?\d+', 'm_CustomRenderQueue: 2450', text, count=1)
    return text

n = {'megapack': 0, 'rocks': 0}
root = 'Assets/Stylized Megapack 2in1/Stylized Medieval Kingdom URP/'
SKIP = ('Scenes', 'Sky', 'Shader Graphs', 'Prefabs', 'Scripts', 'Settings', 'Layers', 'Terrain', 'Ground ', 'Grass ', 'Water', 'Lighting', '.unity', '.asset', '.lighting', '.terrainlayer', '.cs')
for t, path, parts in entries(r'C:\Users\chris\Downloads\Stylized+Megapack+2in1_2022.3.6f1.unitypackage'):
    if not path.startswith(root): continue
    rel = path[len(root):]
    if any(s in rel for s in SKIP): continue
    dst = os.path.join(ASSETS, 'Megapack', rel.replace('/', os.sep))
    if 'asset' not in parts: os.makedirs(dst, exist_ok=True)
    else:
        os.makedirs(os.path.dirname(dst), exist_ok=True); data = t.extractfile(parts['asset']).read()
        if rel.endswith('.mat'): data = standardise(data.decode('utf-8')).encode('utf-8')
        open(dst, 'wb').write(data); n['megapack'] += 1
    if 'asset.meta' in parts: open(dst + '.meta', 'wb').write(t.extractfile(parts['asset.meta']).read())

rroot = 'Assets/Dnk_Dev/RockPack/'
for t, path, parts in entries(r'C:\Users\chris\AppData\Roaming\Unity\Asset Store-5.x\DNKDEV\3D ModelsPropsExterior\HQ Rock Pack Free.unitypackage'):
    if not path.startswith(rroot): continue
    rel = path[len(rroot):]
    if any(s in rel for s in ('.unity', '.lighting', '.exr', 'Lighting', 'Scene', 'Demo', 'M_mapDemo')): continue
    dst = os.path.join(ASSETS, 'HQRocks', rel.replace('/', os.sep))
    if 'asset' not in parts: os.makedirs(dst, exist_ok=True)
    else:
        os.makedirs(os.path.dirname(dst), exist_ok=True); data = t.extractfile(parts['asset']).read()
        if rel.endswith('.mat'): data = standardise(data.decode('utf-8')).encode('utf-8')   # URP Lit too
        open(dst, 'wb').write(data); n['rocks'] += 1
    if 'asset.meta' in parts: open(dst + '.meta', 'wb').write(t.extractfile(parts['asset.meta']).read())
print(n)
