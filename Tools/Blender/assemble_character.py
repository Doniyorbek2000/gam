"""Blender 4.2 headless: Quaternius UBC head + Modular Outfit + hair -> one Humanoid-ready FBX on the outfit skeleton.

Usage: blender -b --factory-startup -P assemble_character.py -- <spec.json> <out_dir>
spec: {slug, outfit, base, hair: [fbx...], drop: regex, max_part_tris, max_tris, render: png}
The base body keeps only the vertices weighted to the head and neck (the outfit carries hands and the rest),
every mesh is re-bound to the outfit armature (same 65-bone skeleton), parts over max_part_tris are decimated.
"""
import bpy, sys, re, os, json, math
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
spec = json.load(open(args[0], encoding="utf-8"))
out = args[1]
os.makedirs(out, exist_ok=True)
KEEP_BONES = {"Head", "neck_01", "neck_02"}

bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    return next(o for o in new if o.type == "ARMATURE"), [o for o in new if o.type == "MESH"]


def tris(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)


def rebind(meshes, armature, old_armature):
    for m in meshes:
        world = m.matrix_world.copy()
        m.parent = armature
        m.matrix_world = world
        for mod in m.modifiers:
            if mod.type == "ARMATURE":
                mod.object = armature
    bpy.data.objects.remove(old_armature, do_unlink=True)


def keep_head(body):
    groups = {g.index: g.name for g in body.vertex_groups}
    drop = []
    for v in body.data.vertices:
        head = sum(g.weight for g in v.groups if groups.get(g.group) in KEEP_BONES)
        total = sum(g.weight for g in v.groups) or 1.0
        if head / total < 0.5:
            drop.append(v.index)
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = body
    body.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="DESELECT")
    bpy.ops.object.mode_set(mode="OBJECT")
    for i in drop:
        body.data.vertices[i].select = True
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.delete(type="VERT")
    bpy.ops.object.mode_set(mode="OBJECT")


armature, meshes = import_fbx(spec["outfit"])
drop = re.compile(spec.get("drop") or "$^", re.I)
for m in [m for m in meshes if drop.search(m.name)]:
    print("DROP", m.name)
    meshes.remove(m)
    bpy.data.objects.remove(m, do_unlink=True)

base_arm, base_meshes = import_fbx(spec["base"])
for m in base_meshes:
    if m.name.lower().startswith("superhero"):
        keep_head(m)
        m.name = "Face"
rebind(base_meshes, armature, base_arm)
meshes += base_meshes

for hair in spec.get("hair", []):
    hair_arm, hair_meshes = import_fbx(hair)
    rebind(hair_meshes, armature, hair_arm)
    meshes += hair_meshes

# One material per garment part (Top/Bottom/Shoes...) so Unity can tint each; Blender's ".001" duplicates merge back.
outfit_mat = re.compile(spec.get("outfit_material", "^MI_(Peasant|Ranger)"))
for m in meshes:
    part = next((p for key, p in spec.get("parts", {}).items() if key in m.name), "Acc")
    for slot in m.material_slots:
        mat = slot.material
        if mat is None:
            continue
        base_name = re.sub(r"\.\d{3}$", "", mat.name)
        if outfit_mat.search(base_name):
            name = f"Outfit_{part}"
            target = bpy.data.materials.get(name) or mat.copy()
            target.name = name
            slot.material = target
        elif base_name != mat.name:
            slot.material = bpy.data.materials.get(base_name) or mat

limit = spec.get("max_part_tris", 4000)
for m in meshes:
    t = tris(m)
    if t > limit:
        mod = m.modifiers.new("Decimate", "DECIMATE")
        mod.ratio = limit / t
        bpy.context.view_layer.objects.active = m
        bpy.ops.object.modifier_move_to_index(modifier="Decimate", index=0)
        bpy.ops.object.modifier_apply(modifier="Decimate")
        print("DECIMATE", m.name, t, "->", tris(m))

total = sum(tris(m) for m in meshes)
mats = sorted({s.material.name for m in meshes for s in m.material_slots if s.material})
print("TRIS", total, "MATERIALS", mats)
if total > spec.get("max_tris", 20000):
    raise SystemExit(f"{spec['slug']}: {total} tris over budget")

bpy.ops.object.select_all(action="DESELECT")
armature.select_set(True)
for m in meshes:
    m.select_set(True)
bpy.context.view_layer.objects.active = armature
bpy.ops.export_scene.fbx(filepath=f"{out}/{spec['slug']}.fbx", use_selection=True, object_types={"ARMATURE", "MESH"},
                         axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS", add_leaf_bones=False,
                         mesh_smooth_type="FACE", use_tspace=True, path_mode="STRIP", embed_textures=False, bake_anim=False)
json.dump({"tris": total, "materials": mats}, open(f"{out}/assembly.json", "w"), indent=1)
print("EXPORTED", f"{out}/{spec['slug']}.fbx")

# Workbench preview (front, 3/4) so the result can be checked without Unity.
if spec.get("render"):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.color_type = "TEXTURE" if spec.get("textured") else "MATERIAL"
    scene.render.resolution_x, scene.render.resolution_y = 512, 768
    lo = Vector((min((m.matrix_world @ Vector(c)).x for m in meshes for c in m.bound_box), 0, 0))
    height = max((m.matrix_world @ Vector(c)).z for m in meshes for c in m.bound_box)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = height * 1.1
    cam.location = (math.sin(math.radians(30)) * 5, 5 * math.cos(math.radians(30)), height * 0.5)
    cam.rotation_euler = (math.radians(90), 0, math.radians(150))
    scene.render.filepath = spec["render"]
    bpy.ops.render.render(write_still=True)
