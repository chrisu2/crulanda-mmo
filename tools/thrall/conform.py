# Put the hand-built thrall mesh on the ORIGINAL Skeleton.fbx rig (its rest pose and its six clips: idle, move_slow, move_fast,
# attack_01, hit_01, death_01), each rigid part carried from its bone's rest frame in the new build to the same bone in the old.
# UVs point into a 4-colour palette (bone, socket, grave-cloth, glow) because ModelBeast skins every skeleton with Skeleton_palette.
import bpy, sys
from mathutils import Matrix
a = sys.argv[sys.argv.index("--") + 1:]; ORIG, NEW, OUT, PAL = a[0], a[1], a[2], a[3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=ORIG)
orig_rig = [o for o in bpy.data.objects if o.type == 'ARMATURE'][0]
for o in [o for o in bpy.data.objects if o.type == 'MESH']: bpy.data.objects.remove(o)
acts = list(bpy.data.actions)
for x in acts: x.name = x.name.split("|")[-1]
print("ACTIONS", [x.name for x in acts])
before = set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=NEW)
new_objs = [o for o in bpy.data.objects if o not in before]
new_rig = [o for o in new_objs if o.type == 'ARMATURE'][0]; body = [o for o in new_objs if o.type == 'MESH'][0]
for x in list(bpy.data.actions):
    if x not in acts: bpy.data.actions.remove(x)
me = body.data; W = body.matrix_world
M = {}
for b in new_rig.data.bones:
    ob = orig_rig.data.bones.get(b.name)
    if ob: M[b.name] = (orig_rig.matrix_world @ ob.matrix_local) @ (new_rig.matrix_world @ b.matrix_local).inverted()
names = {g.index: g.name for g in body.vertex_groups}
for v in me.vertices:
    g = max(v.groups, key=lambda x: x.weight) if v.groups else None
    m = M.get(names[g.group]) if g else None
    p = W @ v.co
    if m: p = m @ p
    v.co = p
body.matrix_world = Matrix()
# palette UVs
slot_u = {"Thrall_Bone": .125, "Thrall_Socket": .375, "Thrall_GraveCloth": .625, "Thrall_EyeGlow": .875}
if not me.uv_layers: me.uv_layers.new(name="UVMap")
uv = me.uv_layers.active.data
for p in me.polygons:
    mn = me.materials[p.material_index].name.split(".")[0] if me.materials else "Thrall_Bone"
    u = slot_u.get(mn, .125)
    for li in p.loop_indices: uv[li].uv = (u, .5)
bpy.data.objects.remove(new_rig)
body.parent = orig_rig
for md in list(body.modifiers): body.modifiers.remove(md)
md = body.modifiers.new("Armature", "ARMATURE"); md.object = orig_rig
body.name = "anc_crea_ossuary_knight__Body"
# the old file's material names, so the meta's remaps still match
for i, m in enumerate(me.materials):
    m.name = "AM_Matte" if i == 0 else m.name
print("TRIS", sum(len(p.vertices) - 2 for p in me.polygons), "H", round(body.dimensions.z, 3))
bpy.ops.export_scene.fbx(filepath=OUT, object_types={'ARMATURE', 'MESH'}, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
    axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, mesh_smooth_type='FACE', use_armature_deform_only=False)
img = bpy.data.images.new("pal", 64, 16)
cols = [(.86, .82, .70), (.05, .04, .04), (.36, .31, .24), (.55, .85, .75)]
px = []
for y in range(16):
    for x in range(64): px += [*cols[x // 16], 1]
img.pixels = px; img.filepath_raw = PAL; img.file_format = 'PNG'; img.save()
print("DONE")
