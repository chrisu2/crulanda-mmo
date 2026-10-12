# Hand-built stylized skeleton thrall (Chris 1:29 "use my gpu to fix the skeleton"; fallback when Hunyuan3D won't load).
# Same 18-bone rig and object names as Resources/Creatures/Skeleton.fbx so the .meta (GUID, humanoid avatar) keeps working.
import bpy, bmesh, math, random, sys
from mathutils import Vector, Matrix
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else "/workspace/thrall/Skeleton.fbx"
RENDER = argv[1] if len(argv) > 1 else ""
random.seed(3)
bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene

def mat(name, rgb, rough=.7):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]; b.inputs[0].default_value = (*rgb, 1); b.inputs["Roughness"].default_value = rough
    m.diffuse_color = (*rgb, 1); return m
BONE = mat("Thrall_Bone", (0.86, 0.82, 0.70), .65)
DARK = mat("Thrall_Socket", (0.05, 0.04, 0.04), .9)
CLOTH = mat("Thrall_GraveCloth", (0.36, 0.31, 0.24), .95)
GLOW = mat("Thrall_EyeGlow", (0.55, 0.85, 0.75), .4)
MATS = [BONE, DARK, CLOTH, GLOW]

parts = []   # (object, bone name)
def add(obj, bone, m=BONE):
    obj.data.materials.clear(); obj.data.materials.append(m); parts.append((obj, bone)); return obj
def sphere(loc, scale, bone, m=BONE, seg=12, rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, location=loc); o = bpy.context.object; o.scale = scale; return add(o, bone, m)
def cube(loc, scale, bone, m=BONE, bevel=0.0, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=loc, rotation=rot); o = bpy.context.object; o.scale = scale
    if bevel: md = o.modifiers.new("b", "BEVEL"); md.width = bevel; md.segments = 2
    return add(o, bone, m)
def rod(a, b, r, bone, m=BONE, verts=8, knobs=True):
    a, b = Vector(a), Vector(b); d = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=d.length, location=(a + b) / 2)
    o = bpy.context.object; o.rotation_mode = 'QUATERNION'; o.rotation_quaternion = d.to_track_quat('Z', 'Y'); add(o, bone, m)
    if knobs:   # a bone's knobbly ends (condyles)
        side = d.orthogonal().normalized() * r * .55
        for p in (a + d * .04, b - d * .04):
            for s in (1, -1): sphere(p + side * s, (r * 1.15, r * 1.15, r * 1.15), bone, m, 8, 6)
    return o
def ring(z, rx, ry, minor, bone, front_gap=.035, back_gap=0.0, droop=0.0, m=BONE):
    bm = bmesh.new(); n = 28
    pts = []
    for i in range(n + 1):
        t = i / n * 2 * math.pi
        x, y = math.sin(t) * rx, -math.cos(t) * ry   # starts at the front (-Y)
        if abs(x) < front_gap and y < 0: continue
        pts.append(Vector((x, y, z - droop * (1 - abs(math.cos(t))) - (droop * .6 if y < 0 else 0))))
    me = bpy.data.meshes.new("rib"); o = bpy.data.objects.new("rib", me); sc.collection.objects.link(o)
    # a tube along the points (two open arcs, left and right)
    left = [p for p in pts if p.x >= 0]; right = [p for p in pts if p.x < 0]
    for arc in (sorted(left, key=lambda p: p.y), sorted(right, key=lambda p: p.y)):
        prev = None
        for i in range(len(arc) - 1):
            a, b = arc[i], arc[i + 1]; d = (b - a).normalized(); u = d.orthogonal().normalized(); v = d.cross(u)
            loop = [bm.verts.new(a + (u * math.cos(k / 6 * 2 * math.pi) + v * math.sin(k / 6 * 2 * math.pi)) * minor * (1, .55)[k % 2]) for k in range(6)]
            if prev:
                for k in range(6): bm.faces.new((prev[k], prev[(k + 1) % 6], loop[(k + 1) % 6], loop[k]))
            prev = loop
    bm.to_mesh(me); bm.free(); return add(o, bone)

H = 1.88
# ---- skull and jaw ----
sk = sphere((0, .01, 1.70), (.105, .125, .118), "head", seg=16, rings=12)
sphere((0, -.045, 1.635), (.082, .07, .05), "head", seg=14, rings=8)            # cheekbones / upper jaw
for s in (1, -1):
    sphere((s * .042, -.098, 1.695), (.032, .03, .03), "head", DARK, 10, 6)   # eye sockets
    sphere((s * .042, -.112, 1.695), (.009, .006, .009), "head", GLOW, 6, 4)  # a faint grave-light
cube((0, -.112, 1.648), (.011, .01, .017), "head", DARK)                 # nose hole
jaw = sphere((0, -.045, 1.578), (.062, .06, .026), "head", seg=14, rings=8)      # jaw (on the head bone: it hangs a little open)
for i in range(8):
    x = -.049 + i * .014
    cube((x, -.111, 1.607), (.0055, .004, .009), "head")                  # upper teeth
    cube((x, -.103, 1.592), (.0055, .004, .008), "head")                  # lower teeth
# ---- neck and spine ----
for i in range(4): rod((0, .02, 1.45 + i * .025), (0, .02, 1.468 + i * .025), .022, "neck", knobs=False)
for i in range(9):
    z = 1.0 + i * .048; b = "spine" if z < 1.22 else "chest"
    rod((0, .06, z), (0, .06, z + .032), .028, b, knobs=False); cube((0, .095, z + .016), (.012, .025, .01), b)   # vertebra and its spine
# ---- ribcage ----
for i, z in enumerate([1.39, 1.35, 1.31, 1.27, 1.23, 1.19]):
    w = .13 + .025 * math.sin(min(i, 4) / 4 * math.pi * .8)
    ring(z, w, .1 + i * .004, .011, "chest", front_gap=.03, droop=.035)
cube((0, -.105, 1.30), (.022, .01, .085), "chest", bevel=.008)           # sternum
for s in (1, -1): rod((s * .02, -.08, 1.415), (s * .17, .0, 1.425), .013, "chest", knobs=False)   # collarbones
for s in (1, -1): sphere((s * .17, .02, 1.41), (.05, .055, .035), "chest")                        # shoulder blades' heads
# ---- pelvis ----
ring(.96, .12, .08, .02, "pelvis", front_gap=.0)
for s in (1, -1): sphere((s * .095, .01, 1.0), (.065, .03, .06), "pelvis")   # hip wings
sphere((0, .06, .96), (.04, .03, .05), "pelvis")                              # sacrum
# ---- arms (A-pose, 40 degrees down) ----
def arm(s):
    sh = Vector((s * .2, .01, 1.40)); down = Vector((s * math.sin(math.radians(50)), 0, -math.cos(math.radians(50))))
    el = sh + down * .29; wr = el + down * .26
    rod(sh, el, .02, "upper_arm." + ("L" if s > 0 else "R")); rod(el, wr, .016, "forearm." + ("L" if s > 0 else "R"))
    rod(el + Vector((0, .02, 0)), wr + Vector((0, .02, 0)), .011, "forearm." + ("L" if s > 0 else "R"), knobs=False)   # the second forearm bone
    hb = "hand." + ("L" if s > 0 else "R"); palm = wr + down * .045
    cube(palm, (.035, .015, .03), hb, bevel=.006, rot=(0, -s * math.radians(40), 0))
    for f in range(4):
        base = palm + down * .03 + Vector((0, -.022 + f * .015, 0)); rod(base, base + down * .065 + Vector((0, 0, -.01)), .0065, hb, verts=6, knobs=False)
    rod(palm + Vector((0, -.03, 0)), palm + Vector((0, -.055, -.03)), .007, hb, verts=6, knobs=False)   # thumb
    return sh, el, wr, wr + down * .14
ARM = {s: arm(s) for s in (1, -1)}
# ---- legs ----
def leg(s):
    hip = Vector((s * .09, 0, .93)); kn = Vector((s * .1, -.01, .5)); an = Vector((s * .1, .01, .085))
    side = "L" if s > 0 else "R"
    rod(hip, kn, .026, "thigh." + side); rod(kn, an, .02, "shin." + side)
    rod(kn + Vector((s * .025, .01, -.02)), an + Vector((s * .02, .01, .02)), .011, "shin." + side, knobs=False)   # fibula
    sphere(kn + Vector((0, -.03, 0)), (.03, .02, .03), "shin." + side)                                            # kneecap
    cube(an + Vector((0, -.07, -.05)), (.04, .1, .02), "foot." + side, bevel=.012)                               # foot
    sphere(an + Vector((0, .01, -.04)), (.035, .045, .035), "foot." + side)                                       # heel
    return hip, kn, an, an + Vector((0, -.17, -.06))
LEG = {s: leg(s) for s in (1, -1)}
# ---- grave-cloth: a tattered wrap round the hips and a strip over one shoulder ----
bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=.155, depth=.26, location=(0, 0, .86), end_fill_type='NOTHING')
wrap = bpy.context.object; wrap.scale = (1, .78, 1)
bm = bmesh.new(); bm.from_mesh(wrap.data)
bmesh.ops.subdivide_edges(bm, edges=[e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > .01], cuts=3)
for v in bm.verts:
    if v.co.z < -.12: v.co.z -= random.uniform(0, .09); v.co.x *= 1.12; v.co.y *= 1.12   # ragged hem, flaring
    v.co += Vector((random.uniform(-.006, .006), random.uniform(-.006, .006), 0))
bm.to_mesh(wrap.data); bm.free(); md = wrap.modifiers.new("s", "SOLIDIFY"); md.thickness = .008
add(wrap, "pelvis", CLOTH)
# shoulder strip (right shoulder to left hip, over the ribs)
bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, 0)); st = bpy.context.object
bm = bmesh.new(); bm.from_mesh(st.data); bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=6); bm.to_mesh(st.data); bm.free()
for v in st.data.vertices:
    u, w = v.co.x + .5, v.co.y   # u along the strip, w across
    p0, p1 = Vector((-.17, -.06, 1.43)), Vector((.13, -.13, .98))
    c = p0.lerp(p1, u); c.y -= math.sin(u * math.pi) * .05
    v.co = c + Vector((.05, .04, .05)).normalized() * w * .09 + Vector((0, random.uniform(-.004, .004), 0))
md = st.modifiers.new("s", "SOLIDIFY"); md.thickness = .006
add(st, "chest", CLOTH)

# ---- armature: the same 18 bones as before ----
arm_d = bpy.data.armatures.new("anc_crea_ossuary_knight__Rig"); rig = bpy.data.objects.new("anc_crea_ossuary_knight__Rig", arm_d)
sc.collection.objects.link(rig); bpy.context.view_layer.objects.active = rig; bpy.ops.object.mode_set(mode='EDIT')
E = arm_d.edit_bones
def B(name, h, t, parent=None):
    b = E.new(name); b.head = h; b.tail = t
    if parent: b.parent = E[parent]; b.use_connect = False
    return b
B("root", (0, 0, 0), (0, 0, .1)); B("pelvis", (0, 0, .95), (0, 0, 1.05), "root"); B("spine", (0, .02, 1.05), (0, .02, 1.22), "pelvis")
B("chest", (0, .02, 1.22), (0, .02, 1.42), "spine"); B("neck", (0, .02, 1.42), (0, .02, 1.53), "chest"); B("head", (0, .01, 1.53), (0, .01, 1.82), "neck")
for s, side in ((1, "L"), (-1, "R")):
    sh, el, wr, tip = ARM[s]; B("upper_arm." + side, sh, el, "chest"); B("forearm." + side, el, wr, "upper_arm." + side); B("hand." + side, wr, tip, "forearm." + side)
    hip, kn, an, toe = LEG[s]; B("thigh." + side, hip, kn, "pelvis"); B("shin." + side, kn, an, "thigh." + side); B("foot." + side, an, toe, "shin." + side)
bpy.ops.object.mode_set(mode='OBJECT')

# ---- one mesh, every part rigidly on its bone (a skeleton's bones do not bend) ----
for o, bone in parts:
    bpy.context.view_layer.objects.active = o
    for md in list(o.modifiers): bpy.ops.object.modifier_apply(modifier=md.name)
    o.data.transform(o.matrix_world); o.matrix_world = Matrix()
    vg = o.vertex_groups.new(name=bone); vg.add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')
    for m in MATS:
        if m.name not in [x.name for x in o.data.materials]: o.data.materials.append(m)
bpy.ops.object.select_all(action='DESELECT')
for o, _ in parts: o.select_set(True)
bpy.context.view_layer.objects.active = parts[0][0]; bpy.ops.object.join()
body = bpy.context.object; body.name = "anc_crea_ossuary_knight__Body"; body.data.name = "anc_crea_ossuary_knight__Body"
bpy.ops.object.shade_auto_smooth(angle=math.radians(40)) if hasattr(bpy.ops.object, "shade_auto_smooth") else None
# remove unused material slots
bpy.ops.object.material_slot_remove_unused()
body.parent = rig; md = body.modifiers.new("Armature", "ARMATURE"); md.object = rig
tris = sum(len(p.vertices) - 2 for p in body.data.polygons)
print("TRIS", tris, "VERTS", len(body.data.vertices), "DIMS", [round(x, 3) for x in body.dimensions])
if tris > 8000:
    dm = body.modifiers.new("dec", "DECIMATE"); dm.ratio = 7800 / tris; bpy.context.view_layer.objects.active = body
    bpy.ops.object.modifier_move_to_index(modifier="dec", index=0); bpy.ops.object.modifier_apply(modifier="dec")
    print("DECIMATED", sum(len(p.vertices) - 2 for p in body.data.polygons))
bpy.ops.export_scene.fbx(filepath=OUT, use_selection=False, object_types={'ARMATURE', 'MESH'}, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                         axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=False, mesh_smooth_type='FACE', use_armature_deform_only=True)
print("EXPORTED", OUT)
if RENDER:
    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (.25, .27, .3, 1)
    sc.render.engine = 'BLENDER_EEVEE_NEXT'; sc.render.resolution_x = 900; sc.render.resolution_y = 1100
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", 'SUN')); sun.data.energy = 3.5; sun.rotation_euler = (math.radians(50), 0, math.radians(-30)); sc.collection.objects.link(sun)
    fill = bpy.data.objects.new("fill", bpy.data.lights.new("fill", 'SUN')); fill.data.energy = 1.2; fill.rotation_euler = (math.radians(70), 0, math.radians(150)); sc.collection.objects.link(fill)
    for name, loc, look, lens in [("full_front", (0, -4.2, 1.0), (0, 0, .95), 50), ("full_3q", (2.6, -3.2, 1.2), (0, 0, .95), 50), ("head_close", (.35, -1.1, 1.68), (0, 0, 1.66), 60), ("torso_close", (.5, -1.4, 1.3), (0, 0, 1.2), 50)]:
        cam.location = loc; cam.data.lens = lens; d = Vector(look) - Vector(loc); cam.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
        sc.render.filepath = RENDER + "_" + name + ".png"; bpy.ops.render.render(write_still=True)
    print("RENDERED")
