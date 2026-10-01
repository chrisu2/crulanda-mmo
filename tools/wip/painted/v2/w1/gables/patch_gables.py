#!/usr/bin/env python
"""Worklist item 6 (visual_review.md): the gable ends of every roof wear slate or thatch instead of wall.

  patch_gables.py "<root>"      (default: D:\\code\\mmo\\New Unity Project\\Assets\\Crulanda)

ZoneMeshes.GableRoof gets a 'verge' parameter (0: closed ends in the roof's own material, as before; > 0: open ends for a
gable wall that far in from each end) and ZoneMeshes.Gable is new (the wall under a gable roof's end, metre UVs running on
from the wall below). ZoneBuilder.Gables builds both gable ends of a walled building: the wall carried up in its own
material, eave cheeks, a tie beam with a king post and a collar, and barge boards down the verges. House (and so Mill), Inn,
Barn and Coop use it. The roofs with no wall under them keep closed ends; the well, the wayshrine, the crypt and the Keeper
porch get a thin facing over those ends (boards or stone). The fallen roof in RuinedHouse is left as it was.

Anchor based: every old block must occur exactly once, all checks run before any write, and a second run fails loudly
without writing (the old blocks are gone and the new ones are found).
"""
import os, sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'D:\code\mmo\New Unity Project\Assets\Crulanda'
MESHES = r'Scripts\World\ZoneMeshes.cs'
BUILDER = r'Scripts\World\ZoneBuilder.cs'
VERDANT = r'Scripts\World\ZoneBuilder.Verdant.cs'

EDITS = []   # (file, old, new)
def edit(f, old, new): EDITS.append((f, old, new))

# ---------------------------------------------------------------- ZoneMeshes.GableRoof: the verge parameter
edit(MESHES,
'''        /// <summary>Gable roof: ridge runs along X; width (X) x depth (Z) footprint, ridge at +height. Pivot at eave centre.</summary>
        public static Mesh GableRoof(float width, float depth, float height, float thickness = .25f)
        {
''',
'''        /// <summary>
        /// Gable roof: ridge runs along X; width (X) x depth (Z) footprint, ridge at +height. Pivot at eave centre. With
        /// <paramref name="verge"/> 0 the two ends are closed with triangles in the roof's own material: a roof with no wall
        /// under it (a well, a porch hood, a fallen roof). On a walled building <paramref name="verge"/> is how far the roof
        /// reaches past the gable wall at each end: the ends are left open for that wall (see <see cref="Gable"/>), the slopes
        /// get undersides (seen under the verge) and the flat soffit stops at the wall.
        /// </summary>
        public static Mesh GableRoof(float width, float depth, float height, float thickness = .25f, float verge = 0)
        {
''')
edit(MESHES,
'''            Vector3 d = Vector3.down * thickness;
            Quad(v, t, aR + d, bR + d, bL + d, aL + d); // underside
            Tri(v, t, aL, bL, rL); Tri(v, t, bR, aR, rR); // gable ends (closed triangles)
            Quad(v, t, aL + d, aL, aR, aR + d); Quad(v, t, bR + d, bR, bL, bL + d); // eave edges
            Quad(v, t, aR + d, aR, bR, bR + d); Quad(v, t, bL + d, bL, aL, aL + d); // slab ends under the gables (left open, the eave slab read as a loose board)
            var m = Build("Gable roof", v, t);
''',
'''            Vector3 d = Vector3.down * thickness, e = Vector3.right * verge; bool open = verge > 0;
            if (open) { Quad(v, t, aR, rR, rL, aL); Quad(v, t, bL, rL, rR, bR); } // the slopes' undersides (vertices 8 to 15)
            Quad(v, t, aR - e + d, bR - e + d, bL + e + d, aL + e + d); // underside (between the gable walls on an open roof)
            if (!open) { Tri(v, t, aL, bL, rL); Tri(v, t, bR, aR, rR); } // gable ends (closed triangles)
            Quad(v, t, aL + d, aL, aR, aR + d); Quad(v, t, bR + d, bR, bL, bL + d); // eave edges
            if (!open) { Quad(v, t, aR + d, aR, bR, bR + d); Quad(v, t, bL + d, bL, aL, aL + d); } // slab ends under the gables (left open, the eave slab read as a loose board)
            var m = Build("Gable roof", v, t);
''')
edit(MESHES,
'''            // The rest (underside, gable ends, edges): flat, across and up.
            float slope = Mathf.Sqrt(hd * hd + height * height);
''',
'''            // An open roof's slope undersides (the next eight) are laid the same. The rest (underside, gable ends, edges): flat,
            // across and up.
            float slope = Mathf.Sqrt(hd * hd + height * height); int slopes = open ? 16 : 8;
''')
edit(MESHES,
'''                uv.Add(i < 8 ? new Vector2(p.x / 2.5f, p.y / height * slope / 2.5f) : new Vector2((p.x + p.z) / 2.5f, p.y / 2.5f));
            }
            m.SetUVs(0, uv); return m;
        }
''',
'''                uv.Add(i < slopes ? new Vector2(p.x / 2.5f, p.y / height * slope / 2.5f) : new Vector2((p.x + p.z) / 2.5f, p.y / 2.5f));
            }
            m.SetUVs(0, uv); return m;
        }
        /// <summary>
        /// The wall under the end of a gable roof: <paramref name="width"/> wide (X) and <paramref name="thickness"/> thick (Z),
        /// standing on the wall top at y = 0 and following the roof's slope up to the ridge at <paramref name="height"/>. With
        /// <paramref name="eave"/> 0 it is a triangle. Otherwise the roof reaches that far past it at each eave, so its two ends
        /// stand upright from the wall top to the slope. Its two faces only (+z and -z): the roof covers the rest. UVs in metres
        /// (<paramref name="tile"/> metres a tile), laid as <see cref="Box"/> lays its z faces, with v counted up from
        /// <paramref name="foot"/>, the height of the wall top in its building, so the texture runs on from the wall below.
        /// </summary>
        public static Mesh Gable(float width, float height, float thickness, float tile = 2, float eave = 0, float foot = 0)
        {
            var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            float hw = width / 2, hz = thickness / 2, sh = eave > 0 ? height * eave / (hw + eave) : 0;
            var ring = sh > 0 ? new[] { new Vector2(hw, 0), new Vector2(hw, sh), new Vector2(0, height), new Vector2(-hw, sh), new Vector2(-hw, 0) }
                : new[] { new Vector2(hw, 0), new Vector2(0, height), new Vector2(-hw, 0) };
            foreach (int s in new[] { 1, -1 })   // the +z face, then the -z face mirrored, so both are wound clockwise seen from outside
            {
                int b = v.Count;
                foreach (var p in ring) { v.Add(new Vector3(s * p.x, p.y, s * hz)); uv.Add(new Vector2(-p.x, p.y + foot) / tile); }
                for (int k = 1; k + 1 < ring.Length; k++) t.AddRange(new[] { b, b + k, b + k + 1 });
            }
            var m = Build("Gable", v, t); m.SetUVs(0, uv); return m;
        }
''')

# ---------------------------------------------------------------- ZoneBuilder.Gables, after Eaves
edit(BUILDER,
'''            Part(PrimitiveType.Cube, t, new Vector3(0, top + roofH + .05f - (sag + .07f) / 2, 0), new Vector3(w + 1.3f, sag + .07f, .34f), roofMat == art.slate ? dark : Tint(art.thatch, new Color(.6f, .48f, .26f)));   // ridge cap: its bottom edges sink 2 cm into the slopes
        }
''',
'''            Part(PrimitiveType.Cube, t, new Vector3(0, top + roofH + .05f - (sag + .07f) / 2, 0), new Vector3(w + 1.3f, sag + .07f, .34f), roofMat == art.slate ? dark : Tint(art.thatch, new Color(.6f, .48f, .26f)));   // ridge cap: its bottom edges sink 2 cm into the slopes
        }
        /// <summary>
        /// The two gable ends of a walled building under an open gable roof (ZoneMeshes.GableRoof with a verge): the wall carried
        /// up to the roof in <paramref name="wall"/>, its texture running on from the wall below (<paramref name="tile"/> metres a
        /// tile); a dark cheek that closes the corner under each eave, beside the wall; a barge board down each verge, so the
        /// roof's edge reads thick; and, with <paramref name="beam"/> above 0 (the timbers' depth), a tie beam on the wall top
        /// with a king post and a collar over it. <paramref name="face"/> is the gable walls' outer face (x) and
        /// <paramref name="d"/> their width, <paramref name="top"/> the wall top, <paramref name="run"/> half the roof's depth,
        /// <paramref name="end"/> half its length and <paramref name="slab"/> its thickness. For the eye only: no colliders.
        /// </summary>
        void Gables(Transform t, float face, float d, float top, float roofH, float run, float end, Material wall, float tile, float beam, float slab = .25f)
        {
            var dark = Tint(art.timber, new Color(.26f, .17f, .11f));
            var infill = ZoneMeshes.Gable(d, roofH, .3f, tile, run - d / 2, top);
            var cheeks = ZoneMeshes.Gable(run * 2, roofH + slab, .06f, 1, run * slab / roofH); cheeks.name = "Eave cheeks";   // from the soffit up: the same slope, upright for the slab's depth at each eave
            float slope = Mathf.Sqrt(run * run + roofH * roofH), pitch = Mathf.Atan2(roofH, run) * Mathf.Rad2Deg, board = Mathf.Min(.3f, roofH * .2f);
            foreach (int sx in new[] { -1, 1 })
            {
                var turn = Quaternion.Euler(0, sx * 90, 0);   // the meshes' +z face looks out
                MeshPart(infill, t, new Vector3(sx * (face - .15f), top, 0), wall, turn);       // face-.3 .. face
                MeshPart(cheeks, t, new Vector3(sx * (face - .05f), top - slab, 0), dark, turn);   // face-.08 .. face-.02: inside the wall, seen only past its corners
                if (beam > 0)
                {
                    float post = roofH * (1 - beam * .45f / run) - beam / 2, half = run * (.5f - beam * .4f / roofH);   // the post's head and the collar's ends stop on the slope
                    Part(PrimitiveType.Cube, t, new Vector3(sx * (face + .02f), top, 0), new Vector3(.12f, beam, d), art.timber);                                   // tie beam: face-.04 .. face+.08
                    Part(PrimitiveType.Cube, t, new Vector3(sx * (face + .02f), top + beam / 2 + post / 2, 0), new Vector3(.12f, post, beam * .9f), art.timber);   // king post, standing on it
                    Part(PrimitiveType.Cube, t, new Vector3(sx * (face + .02f), top + roofH * .5f, 0), new Vector3(.1f, beam * .8f, half * 2), art.timber);       // collar: 1 cm behind the post's face
                }
                // Barge boards: 3 cm above the slope and hanging down from it, from 3 cm past the ridge (the two cross there) to 12 cm past the eave; end-.02 .. end+.06, 1 cm proud of the fascia's end.
                foreach (int sz in new[] { -1, 1 })
                {
                    var down = new Vector3(0, -roofH, sz * run) / slope; var up = new Vector3(0, run, sz * roofH) / slope;
                    Part(PrimitiveType.Cube, t, new Vector3(sx * (end + .02f), top + roofH / 2, sz * run / 2) + down * .045f - up * (board / 2 - .03f), new Vector3(.08f, board, slope + .15f), dark, Quaternion.Euler(sz * pitch, 0, 0));
                }
            }
        }
''')

# ---------------------------------------------------------------- House (and so Mill)
edit(BUILDER,
'''            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH), t, new Vector3(0, top, 0), roofMat);
            Eaves(t, w, d, top, roofH, roofMat);
''',
'''            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH, .25f, .6f), t, new Vector3(0, top, 0), roofMat);
            Eaves(t, w, d, top, roofH, roofMat);
            Gables(t, w / 2, d, top, roofH, d / 2 + .7f, w / 2 + .6f, plaster, 2, .22f);   // plaster to the ridge, framed like the walls
''')
# ---------------------------------------------------------------- Inn
edit(BUILDER,
'''            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH), t, new Vector3(0, H, 0), variant % 2 == 0 ? art.thatch : art.slate);
            Eaves(t, w, d, H, roofH, variant % 2 == 0 ? art.thatch : art.slate);
''',
'''            MeshPart(ZoneMeshes.GableRoof(w + 1.2f, d + 1.4f, roofH, .25f, .6f - wall / 2), t, new Vector3(0, H, 0), variant % 2 == 0 ? art.thatch : art.slate);
            Eaves(t, w, d, H, roofH, variant % 2 == 0 ? art.thatch : art.slate);
            Gables(t, w / 2 + wall / 2, d + wall, H, roofH, d / 2 + .7f, w / 2 + .6f, plaster, 2, .22f);   // the end walls' outer faces, out to the front and back walls' faces
''')
# ---------------------------------------------------------------- Barn
edit(BUILDER,
'''            MeshPart(ZoneMeshes.GableRoof(w + 1, d + 1.4f, roofH), t, new Vector3(0, h, 0), art.thatch);
            Eaves(t, w - .2f, d, h, roofH, art.thatch);   // the barn's roof is 1 m wider than its walls, not 1.2
''',
'''            MeshPart(ZoneMeshes.GableRoof(w + 1, d + 1.4f, roofH, .25f, .5f), t, new Vector3(0, h, 0), art.thatch);
            Eaves(t, w - .2f, d, h, roofH, art.thatch);   // the barn's roof is 1 m wider than its walls, not 1.2
            Gables(t, w / 2, d, h, roofH, d / 2 + .7f, w / 2 + .5f, boards, d, .2f);   // boards to the ridge: the wall below is a cube, one tile across its width
''')
# ---------------------------------------------------------------- Well: closed roof, boarded ends
edit(BUILDER,
'''            MeshPart(ZoneMeshes.GableRoof(2.8f, 2.2f, 1f, .1f), t, new Vector3(0, 2.75f, 0), art.slate, Quaternion.Euler(0, 90, 0));
''',
'''            MeshPart(ZoneMeshes.GableRoof(2.8f, 2.2f, 1f, .1f), t, new Vector3(0, 2.75f, 0), art.slate, Quaternion.Euler(0, 90, 0));   // on posts, no wall under it: closed ends
            foreach (int s in new[] { -1, 1 }) MeshPart(PropMesh("Well gable", () => ZoneMeshes.Gable(2.2f, 1, .04f, 1)), t, new Vector3(0, 2.75f, s * 1.41f), dark);   // boarded over: 3 cm proud of each end
''')
# ---------------------------------------------------------------- Coop
edit(BUILDER,
'''            MeshPart(ZoneMeshes.GableRoof(w + .5f, d + .6f, .9f), t, new Vector3(0, floor + h, 0), art.thatch);
''',
'''            MeshPart(ZoneMeshes.GableRoof(w + .5f, d + .6f, .9f, .25f, .25f), t, new Vector3(0, floor + h, 0), art.thatch);
            Gables(t, w / 2, d, floor + h, .9f, d / 2 + .3f, w / 2 + .25f, boards, d, 0);   // boards to the ridge and barge boards; too small for a truss
''')
# ---------------------------------------------------------------- Wayshrine: closed roof, stone ends
edit(BUILDER,
'''            MeshPart(ZoneMeshes.GableRoof(1.25f, 1.15f, .45f, .12f), t, new Vector3(0, 2.8f, .25f), art.slate, Quaternion.Euler(0, 90, 0));
''',
'''            MeshPart(ZoneMeshes.GableRoof(1.25f, 1.15f, .45f, .12f), t, new Vector3(0, 2.8f, .25f), art.slate, Quaternion.Euler(0, 90, 0));   // a cap on the niche: closed ends
            foreach (int s in new[] { -1, 1 }) MeshPart(PropMesh("Wayshrine gable", () => ZoneMeshes.Gable(1.15f, .45f, .04f, 1)), t, new Vector3(0, 2.8f, .25f + s * .635f), Dressed(new Color(.6f, .57f, .5f)));   // faced in the shrine's stone, 3 cm proud
''')
# ---------------------------------------------------------------- Crypt: closed roof, stone pediments
edit(BUILDER,
'''                MeshPart(ZoneMeshes.GableRoof(6.6f, 6.6f, 1.6f), t, new Vector3(0, 2.4f, 1.5f), Tint(art.slate, new Color(.27f, .28f, .3f)));
''',
'''                MeshPart(ZoneMeshes.GableRoof(6.6f, 6.6f, 1.6f), t, new Vector3(0, 2.4f, 1.5f), Tint(art.slate, new Color(.27f, .28f, .3f)));   // the vault fills it: closed ends
                foreach (int s in new[] { -1, 1 }) MeshPart(PropMesh("Crypt pediment", () => ZoneMeshes.Gable(6.6f, 1.6f, .06f, 1.75f, 0, 2.4f)), t, new Vector3(s * 3.31f, 2.4f, 1.5f), stone, Quaternion.Euler(0, s * 90, 0));   // a pediment of the vault's stone, 4 cm proud of each end
''')
# ---------------------------------------------------------------- Keeper porch hood (Verdant): closed roof, boarded front
edit(VERDANT,
'''            MeshPart(ZoneMeshes.GableRoof(2.6f, 1.6f, .6f, .1f), t, house + new Vector3(0, floor + 2.8f, -hr - .7f), tile, Quaternion.Euler(0, 90, 0));
''',
'''            MeshPart(ZoneMeshes.GableRoof(2.6f, 1.6f, .6f, .1f), t, house + new Vector3(0, floor + 2.8f, -hr - .7f), tile, Quaternion.Euler(0, 90, 0));   // a hood on two posts: closed ends
            MeshPart(PropMesh("Porch gable", () => ZoneMeshes.Gable(1.6f, .6f, .04f, 1)), t, house + new Vector3(0, floor + 2.8f, -hr - 2.01f), wood);   // its front boarded, 3 cm proud (the back is in the house)
''')

# Left alone on purpose: RuinedHouse's fallen roof (GableRoof(rw, rd, rh)): a roof section lying in the shell, no wall under it.
UNTOUCHED = [(BUILDER, 'MeshPart(ZoneMeshes.GableRoof(rw, rd, rh), t, roofAt, Tint(art.thatch, new Color(.24f, .2f, .15f)), fallen);')]
# Every GableRoof call, each handled above: eight in ZoneBuilder.cs (House, Inn, Barn, Well, Coop, Wayshrine, RuinedHouse,
# Crypt) and one in ZoneBuilder.Verdant.cs (the Keeper porch hood). The definition is in ZoneMeshes.cs.
CALLS = {BUILDER: 8, VERDANT: 1}

def main():
    texts, problems = {}, []
    for f in sorted({e[0] for e in EDITS}):
        path = os.path.join(ROOT, f)
        if not os.path.isfile(path): problems.append('missing file: ' + path); continue
        with open(path, 'rb') as h: raw = h.read()
        if b'\r' in raw: problems.append(f + ': has CR line ends (expected LF)')
        texts[f] = raw.decode('utf-8')
    if problems: fail(problems)
    for f, n in CALLS.items():
        got = texts[f].count('ZoneMeshes.GableRoof(')
        if got != n: problems.append('%s: %d GableRoof calls, expected %d (a new call needs a deliberate choice of ends)' % (f, got, n))
    for f, s in UNTOUCHED:
        if texts[f].count(s) != 1: problems.append('%s: the call left alone is not there exactly once: %s' % (f, s[:70]))
    for marker, f in (('public static Mesh Gable(', MESHES), ('void Gables(Transform t', BUILDER), ('float verge = 0', MESHES)):
        if marker in texts[f]: problems.append('%s: already patched (found "%s")' % (f, marker))
    out = dict(texts)
    for i, (f, old, new) in enumerate(EDITS):
        n = texts[f].count(old)
        if n != 1: problems.append('%s: edit %d: old block found %d times, expected 1: %s' % (f, i + 1, n, old.strip().splitlines()[0][:90])); continue
        if out[f].count(old) != 1: problems.append('%s: edit %d: old block no longer unique after earlier edits' % (f, i + 1)); continue
        out[f] = out[f].replace(old, new)
    if problems: fail(problems)
    for f in out:
        with open(os.path.join(ROOT, f), 'wb') as h: h.write(out[f].encode('utf-8'))
        print('patched', f)
    print('gables: %d edits in %d files' % (len(EDITS), len(out)))

def fail(problems):
    print('patch_gables: NOTHING WRITTEN')
    for p in problems: print('  ' + p)
    sys.exit(1)

if __name__ == '__main__': main()
