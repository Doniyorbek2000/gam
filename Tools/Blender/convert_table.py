"""Blender 4.2 headless: Sketchfab glTF pool table -> game-ready FBX + per-material URP maps.

Usage: blender -b --factory-startup -P convert_table.py -- <scene.gltf> <out_dir> <slug> <drop_regex> [max_tris]
Strips props by object-name regex and loose parts lying on the cloth (balls, rack, chalk) or floating (cues),
measures the cloth plane and cushion noses by ray casts, then fits the play area to 2.54 x 1.27 m
(cloth at z = 0, floor at z = -0.8). Writes <slug>.fbx, <mat>_BaseMap/_Normal/_Mask/_Emission.png, materials.json.
"""
import bpy, bmesh, sys, re, os, json
import numpy as np
from mathutils import Vector, Matrix

PLAY_L, PLAY_W, CLOTH_TO_FLOOR, TEX = 2.54, 1.27, 0.8, 2048
args = sys.argv[sys.argv.index("--") + 1:]
gltf, out, slug, drop = args[0], args[1], args[2], re.compile(args[3], re.I)
max_tris = int(args[4]) if len(args) > 4 else 40000
os.makedirs(out, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=gltf)
scene = bpy.context.scene
for ob in list(scene.objects):
    if ob.type != "MESH" or drop.search(ob.name):
        if ob.type == "MESH":
            print("DROP", ob.name)
        continue
meshes = [o for o in scene.objects if o.type == "MESH" and not drop.search(o.name)]
for o in [o for o in scene.objects if o.type == "MESH" and drop.search(o.name)]:
    bpy.data.objects.remove(o, do_unlink=True)

# Bake parent transforms, join to one object.
bpy.ops.object.select_all(action="DESELECT")
for o in meshes:
    o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
if len(meshes) > 1:
    bpy.ops.object.join()
table = bpy.context.view_layer.objects.active
for o in [o for o in scene.objects if o != table]:
    bpy.data.objects.remove(o, do_unlink=True)

def bounds(ob):
    vs = np.array([v.co[:] for v in ob.data.vertices])
    return vs.min(0), vs.max(0)

mn, mx = bounds(table)
centre = (mn + mx) / 2
long_is_x = (mx - mn)[0] >= (mx - mn)[1]
if not long_is_x:  # long axis -> X
    table.data.transform(Matrix.Rotation(np.pi / 2, 4, "Z"))
    table.data.update()
    mn, mx = bounds(table)
    centre = (mn + mx) / 2

deps = bpy.context.evaluated_depsgraph_get()

from mathutils.bvhtree import BVHTree
_bvh = [None]

def rebuild_bvh():
    bm2 = bmesh.new()
    bm2.from_mesh(table.data)
    _bvh[0] = BVHTree.FromBMesh(bm2)
    bm2.free()

def hit_z(x, y):
    loc, *_ = _bvh[0].ray_cast(Vector((x, y, mx[2] + 1)), Vector((0, 0, -1)))
    return loc.z if loc is not None else None

def cloth_and_extent():
    cx, cy = centre[0], centre[1]
    size = mx - mn
    # Most common hit height over the central area: balls and racks cover too little to win the vote.
    zs = [hit_z(cx + size[0] * 0.3 * (i / 7 - 1), cy + size[1] * 0.25 * (j / 3 - 1)) for i in range(15) for j in range(7)]
    zs = np.array([z for z in zs if z is not None])
    vals, counts = np.unique(np.round(zs / 0.005), return_counts=True)
    mode = vals[counts.argmax()] * 0.005
    cloth = float(np.median(zs[np.abs(zs - mode) < 0.005]))
    def march(x0, y0, ux, uy, reach):
        # Cushion nose = first point where the surface stays raised for 4 cm (thin leftovers are skipped).
        step, need, run = reach / 2000, 0.04 * size[0] / 2.8, 0
        for i in range(1, 2000):
            z = hit_z(x0 + ux * i * step, y0 + uy * i * step)
            run = run + 1 if z is not None and z > cloth + 0.012 * (size[0] / 2.8) else 0
            if run * step >= need:
                return (i - run + 1) * step
        return None
    hl = [march(cx, cy, s, 0, size[0] / 2) for s in (1, -1)]
    q = size[0] / 4  # long rails have side pockets at the middle: measure the width at a quarter
    hw = [march(cx + dx, cy, 0, s, size[1] / 2) for dx in (q, -q) for s in (1, -1)]
    return cloth, hl, hw

# Remove loose parts: props on the cloth (balls, rack, chalk) and parts floating above the rails (cues).
def cloth_height():
    size = mx - mn
    zs = [hit_z(centre[0] + size[0] * 0.3 * (i / 7 - 1), centre[1] + size[1] * 0.25 * (j / 3 - 1)) for i in range(15) for j in range(7)]
    zs = np.array([z for z in zs if z is not None])
    vals, counts = np.unique(np.round(zs / 0.005), return_counts=True)
    mode = vals[counts.argmax()] * 0.005
    return float(np.median(zs[np.abs(zs - mode) < 0.005]))

rebuild_bvh()
cloth = cloth_height()
print("PRE cloth", cloth)
bm = bmesh.new()
bm.from_mesh(table.data)
bm.verts.ensure_lookup_table()
seen, islands = set(), []
for v in bm.verts:
    if v.index in seen:
        continue
    stack, isl = [v], []
    seen.add(v.index)
    while stack:
        a = stack.pop()
        isl.append(a)
        for e in a.link_edges:
            b = e.other_vert(a)
            if b.index not in seen:
                seen.add(b.index)
                stack.append(b)
    islands.append(isl)
size0 = mx - mn
inner_x, inner_y = size0[0] * 0.4, size0[1] * 0.4
rail_top = cloth + 0.06 * ((mx - mn)[0] / 2.8)
kill = []
for isl in islands:
    co = np.array([v.co[:] for v in isl])
    lo, hi = co.min(0), co.max(0)
    c = (lo + hi) / 2
    small = max(hi[0] - lo[0], hi[1] - lo[1]) < 0.5 * size0[0] / 2.8  # racks are ~0.4 m; cushions and rails are longer
    on_cloth = small and abs(c[0] - centre[0]) < inner_x and abs(c[1] - centre[1]) < inner_y and lo[2] >= cloth - 0.02 and hi[2] < cloth + 0.25
    floating = lo[2] > rail_top + 0.05
    if on_cloth or floating:
        kill.extend(isl)
print("LOOSE parts", len(islands), "removed verts", len(kill))
bmesh.ops.delete(bm, geom=list(set(kill)), context="VERTS")
bm.to_mesh(table.data)
bm.free()
table.data.update()

mn, mx = bounds(table)
centre = (mn + mx) / 2
rebuild_bvh()
cloth, hl, hw = cloth_and_extent()
print("MEASURE cloth", cloth, "hl", hl, "hw", hw)
play_l, play_w = sum(hl), 2 * max(hw)
floor = mn[2]
sx, sy, sz = PLAY_L / play_l, PLAY_W / play_w, CLOTH_TO_FLOOR / (cloth - floor)
off_x = centre[0] + (hl[0] - hl[1]) / 2
off_y = centre[1] + (max(hw[0], hw[2]) - max(hw[1], hw[3])) / 2
print(f"FIT play {play_l:.3f} x {play_w:.3f}, cloth {cloth - floor:.3f} above floor -> scale {sx:.3f} {sy:.3f} {sz:.3f}")
table.data.transform(Matrix.Diagonal((sx, sy, sz, 1)) @ Matrix.Translation((-off_x, -off_y, -cloth)))
table.data.update()

tris = sum(len(p.vertices) - 2 for p in table.data.polygons)
if tris > max_tris:
    mod = table.modifiers.new("dec", "DECIMATE")
    mod.ratio = max_tris / tris * 0.98
    bpy.context.view_layer.objects.active = table
    bpy.ops.object.modifier_apply(modifier="dec")
    tris = sum(len(p.vertices) - 2 for p in table.data.polygons)
mn, mx = bounds(table)
print("TRIS", tris, "SIZE", [round(float(v), 3) for v in mx - mn])
table.name = slug
table.data.name = slug

# Materials: export URP-ready maps per material.
def img_of(node_input):
    for link in node_input.links:
        n = link.from_node
        while n.type not in ("TEX_IMAGE",) and n.inputs:
            ls = [l for i in n.inputs for l in i.links]
            if not ls:
                return None
            n = ls[0].from_node
        if n.type == "TEX_IMAGE":
            return n.image
    return None

def pixels(img):
    img.scale(min(img.size[0], TEX), min(img.size[1], TEX))
    a = np.array(img.pixels[:], dtype=np.float32).reshape(img.size[1], img.size[0], 4)
    return a

def save(arr, path, colorspace):
    h, w = arr.shape[:2]
    im = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    im.colorspace_settings.name = colorspace
    im.pixels.foreach_set(arr.astype(np.float32).ravel())
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)

meta = []
for slot in table.material_slots:
    mat = slot.material
    name = re.sub(r"[^A-Za-z0-9_]", "_", mat.name if mat.name != "None" else slug + "_mat")
    mat.name = name
    bsdf = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    entry = {"alpha": "OPAQUE", "doubleSided": not mat.use_backface_culling,
             "color": list(bsdf.inputs["Base Color"].default_value), "metallic": bsdf.inputs["Metallic"].default_value,
             "roughness": bsdf.inputs["Roughness"].default_value}
    base = img_of(bsdf.inputs["Base Color"])
    if base is not None:
        px = pixels(base)
        if (px[..., 3] < 0.5).mean() > 0.001:  # Blender 4.2 marks every glTF material HASHED; trust the pixels
            entry["alpha"] = "MASK" if (px[..., 3] < 0.5).mean() < 0.2 else "BLEND"
        save(px, f"{out}/{name}_BaseMap.png", "sRGB")
        entry["base"] = f"{name}_BaseMap.png"
    nrm_node = bsdf.inputs["Normal"]
    nimg = img_of(nrm_node)
    if nimg is not None:
        save(pixels(nimg), f"{out}/{name}_Normal.png", "Non-Color")
        entry["normal"] = f"{name}_Normal.png"
    mr = img_of(bsdf.inputs["Roughness"]) or img_of(bsdf.inputs["Metallic"])
    if mr is not None:
        p = pixels(mr)  # glTF: G = roughness, B = metallic
        mask = np.zeros_like(p)
        mask[..., 0] = p[..., 2]
        mask[..., 1] = 1.0
        mask[..., 3] = 1.0 - p[..., 1]
        save(mask, f"{out}/{name}_Mask.png", "Non-Color")
        entry["mask"] = f"{name}_Mask.png"
    em = img_of(bsdf.inputs["Emission Color"])
    if em is not None:
        save(pixels(em), f"{out}/{name}_Emission.png", "sRGB")
        entry["emission"] = f"{name}_Emission.png"
    entry["name"] = name
    meta.append(entry)
    print("MAT", name, entry)
json.dump({"materials": meta, "tris": tris}, open(f"{out}/materials.json", "w"), indent=2)

bpy.ops.object.select_all(action="DESELECT")
table.select_set(True)
bpy.ops.export_scene.fbx(filepath=f"{out}/{slug}.fbx", use_selection=True, object_types={"MESH"},
                         axis_forward="-Z", axis_up="Y", apply_scale_options="FBX_SCALE_UNITS",
                         mesh_smooth_type="FACE", use_tspace=True, path_mode="STRIP", embed_textures=False)
print("EXPORTED", f"{out}/{slug}.fbx")
