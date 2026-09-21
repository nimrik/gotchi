# Builds the Gotchi tuxedo cat: a chibi 3D model in the flat-colour, dark-outline style of the reference art,
# rigged with a simple bone set and authored with every LoopClip / OneShot the game uses, then exported as FBX
# for Unity (legacy Animation clips, one per Blender action).
#
#   /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/build_cat.py -- \
#       --fbx Assets/Resources/Creatures/Cat3D/cat.fbx --preview /tmp/cat-preview [--blend /tmp/cat-preview/cat.blend]
#
# --blend saves the finished scene as a .blend so it can be opened in the Blender GUI (or by an MCP session) for inspection.
#
# Conventions: Blender Z up, the cat faces -Y. Every part is its own skinned object (rigid weights to one
# bone), except the tail which blends across three bones. Objects that Unity toggles (mouths, brows, tears,
# accessories, dirt…) are exported visible and hidden by name at runtime (see Cat3DView).

import bpy, bmesh, math, sys, os
from mathutils import Vector, Matrix, Euler

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
def arg(name, default=None):
    return argv[argv.index(name) + 1] if name in argv else default
FBX_PATH = arg("--fbx")
PREVIEW_DIR = arg("--preview")
BLEND_PATH = arg("--blend")

# ------------------------------------------------------------------ palette (reference: left cat)
PALETTE = {
    "Fur":      (0.235, 0.165, 0.150),
    "White":    (1.000, 0.965, 0.915),
    "EarPink":  (0.960, 0.600, 0.690),
    "Eye":      (0.965, 0.700, 0.190),
    "Pupil":    (0.110, 0.085, 0.085),
    "Glint":    (1.000, 1.000, 1.000),
    "Nose":     (0.985, 0.900, 0.880),
    "Whisker":  (0.930, 0.880, 0.800),
    "Ink":      (0.150, 0.105, 0.105),
    "Tongue":   (0.930, 0.480, 0.560),
    "Blush":    (0.970, 0.640, 0.700),
    "Tear":     (0.480, 0.760, 0.940),
    "HeartRed": (0.930, 0.330, 0.420),
    "Dirt":     (0.520, 0.400, 0.290),
    "Shadow":   (0.300, 0.250, 0.300),
    "Beanie":   (0.960, 0.620, 0.700),
    "Pom":      (1.000, 0.965, 0.915),
    "Scarf":    (0.560, 0.820, 0.780),
    "Bow":      (0.910, 0.330, 0.380),
    "Crown":    (0.970, 0.780, 0.280),
}
MATS = {}
def mat(name):
    if name in MATS: return MATS[name]
    m = bpy.data.materials.new(name)
    r, g, b = PALETTE[name]
    m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (r, g, b, 1)
    m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.9
    m.diffuse_color = (r, g, b, 1)
    MATS[name] = m
    return m

# ------------------------------------------------------------------ scene reset
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0

OBJECTS = []   # (object, bone-or-None)

def finish(obj, name, material, bone, smooth=True):
    obj.name = name
    obj.data.name = name
    obj.data.materials.clear()
    if material is not None: obj.data.materials.append(mat(material))
    for p in obj.data.polygons: p.use_smooth = smooth
    OBJECTS.append((obj, bone))
    return obj

def sphere(name, material, bone, center, radius, scale=(1, 1, 1), rot=(0, 0, 0), seg=48, rings=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=radius, location=center)
    o = bpy.context.active_object
    o.rotation_euler = Euler([math.radians(a) for a in rot], 'XYZ')
    o.scale = scale
    return finish(o, name, material, bone)

def cone(name, material, bone, center, r1, depth, rot=(0, 0, 0), r2=0.0, verts=32):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth, location=center)
    o = bpy.context.active_object
    o.rotation_euler = Euler([math.radians(a) for a in rot], 'XYZ')
    return finish(o, name, material, bone)

def cylinder(name, material, bone, center, radius, depth, rot=(0, 0, 0), verts=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=center)
    o = bpy.context.active_object
    o.rotation_euler = Euler([math.radians(a) for a in rot], 'XYZ')
    return finish(o, name, material, bone)

def tube(name, material, bone, points, bevel, radii=None, res=16):
    """A smooth bevelled bezier curve turned into a mesh (used for the tail, mouths and closed-eye arcs)."""
    curve = bpy.data.curves.new(name + "Curve", 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth = bevel
    curve.bevel_resolution = 6
    curve.resolution_u = res
    curve.use_fill_caps = True
    sp = curve.splines.new('BEZIER')
    sp.bezier_points.add(len(points) - 1)
    for i, p in enumerate(points):
        bp = sp.bezier_points[i]
        bp.co = Vector(p)
        bp.handle_left_type = bp.handle_right_type = 'AUTO'
        bp.radius = radii[i] if radii else 1.0
    tmp = bpy.data.objects.new(name + "Tmp", curve)
    scene.collection.objects.link(tmp)
    deps = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(tmp.evaluated_get(deps))
    bpy.data.objects.remove(tmp, do_unlink=True)
    o = bpy.data.objects.new(name, me)
    scene.collection.objects.link(o)
    return finish(o, name, material, bone)

def apply_transform(o):
    """Bake rotation/scale into the mesh so region tests and export are in plain world coordinates."""
    o.data.transform(o.matrix_world)
    o.matrix_world = Matrix.Identity(4)

def paint_region(o, second_material, predicate):
    """Assign `second_material` to every face whose centre (in the primitive's own unit coordinates) passes."""
    o.data.materials.append(mat(second_material))
    inv = o.matrix_world.inverted()
    for poly in o.data.polygons:
        c = inv @ (o.matrix_world @ poly.center)  # already local
        c = poly.center
        if predicate(c): poly.material_index = 1

def bake_mask_texture(o, path, sdf, inside, outside, size=(1024, 512)):
    """Rasterises `sdf` (negative = inside, evaluated on the primitive's unit coordinates) into the object's
    UV space so patch borders are smooth in Unity instead of following the sphere's face grid."""
    import numpy as np
    W, H = size
    img = np.empty((H, W, 4), dtype=np.float32)
    img[..., 0:3] = outside; img[..., 3] = 1.0
    me = o.data; uv = me.uv_layers.active.data
    for poly in me.polygons:
        loops = list(poly.loop_indices)
        uvs = [np.array(uv[l].uv) for l in loops]
        cos = [np.array(me.vertices[me.loops[l].vertex_index].co) for l in loops]
        for i in range(1, len(loops) - 1):
            (u0, u1, u2), (c0, c1, c2) = (uvs[0], uvs[i], uvs[i + 1]), (cos[0], cos[i], cos[i + 1])
            px = np.array([u0[0], u1[0], u2[0]]) * W; py = (1.0 - np.array([u0[1], u1[1], u2[1]])) * H
            x0, x1 = int(max(0, np.floor(px.min()) - 1)), int(min(W - 1, np.ceil(px.max()) + 1))
            y0, y1 = int(max(0, np.floor(py.min()) - 1)), int(min(H - 1, np.ceil(py.max()) + 1))
            if x1 < x0 or y1 < y0: continue
            gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
            det = (px[1] - px[0]) * (py[2] - py[0]) - (px[2] - px[0]) * (py[1] - py[0])
            if abs(det) < 1e-9: continue
            l1 = ((gx - px[0]) * (py[2] - py[0]) - (px[2] - px[0]) * (gy - py[0])) / det
            l2 = ((px[1] - px[0]) * (gy - py[0]) - (gx - px[0]) * (py[1] - py[0])) / det
            l0 = 1.0 - l1 - l2
            eps = -0.08
            m = (l0 >= eps) & (l1 >= eps) & (l2 >= eps)
            if not m.any(): continue
            pos = l0[..., None] * c0 + l1[..., None] * c1 + l2[..., None] * c2
            f = sdf(pos[..., 0], pos[..., 1], pos[..., 2])
            t = np.clip((0.012 - f) / 0.024, 0.0, 1.0)  # ~1-2 texel soft edge
            col = np.array(outside)[None, None, :] * (1 - t[..., None]) + np.array(inside)[None, None, :] * t[..., None]
            sub = img[y0:y1 + 1, x0:x1 + 1, 0:3]
            sub[m] = col[m]
    image = bpy.data.images.new(os.path.basename(path), W, H, alpha=True)
    image.pixels = np.flipud(img).ravel().tolist()
    image.filepath_raw = os.path.abspath(path); image.file_format = 'PNG'; image.save()
    return image

def textured_material(name, image):
    m = bpy.data.materials.new(name)
    tex = m.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = image
    m.node_tree.links.new(tex.outputs["Color"], m.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    m.diffuse_color = (0.5, 0.5, 0.5, 1)
    return m

def ellipse(x, z, a, b, cz=0.0):
    import numpy as np
    return np.sqrt((x / a) ** 2 + ((z - cz) / b) ** 2) - 1.0

def keep_faces(o, predicate):
    """Delete faces whose local centre fails the predicate (used to cut the beanie cap out of a sphere)."""
    bm = bmesh.new(); bm.from_mesh(o.data)
    doomed = [f for f in bm.faces if not predicate(f.calc_center_median())]
    bmesh.ops.delete(bm, geom=doomed, context='FACES')
    bm.to_mesh(o.data); bm.free()

# ------------------------------------------------------------------ geometry
HEAD_C = Vector((0.0, 0.0, 1.84)); HEAD_S = Vector((1.08, 0.98, 1.0))
BODY_C = Vector((0.0, 0.03, 0.72)); BODY_S = Vector((1.0, 0.85, 0.95))

def head_deform(u):
    """Unit-sphere → the reference head: wider toward the top, flattened crown, slightly flat chin."""
    x, y, z = u[0], u[1], u[2]
    x = x * (1.0 + 0.05 * z)
    z = z * (1.0 - 0.15 * z) if z > 0 else z * (1.0 - 0.05 * (-z))
    return (x, y, z)

def on_head(u):
    """World point for unit-sphere coordinates on the head."""
    u = head_deform(u)
    return HEAD_C + Vector((u[0] * HEAD_S.x, u[1] * HEAD_S.y, u[2] * HEAD_S.z))
def head_normal(u):
    n = Vector((u[0] / HEAD_S.x, u[1] / HEAD_S.y, u[2] / HEAD_S.z)); n.normalize(); return n

# Head with the white muzzle/forehead patch.
TEX_DIR = os.path.dirname(os.path.abspath(FBX_PATH)) if FBX_PATH else (PREVIEW_DIR or ".")
head = sphere("Head", None, "Head", HEAD_C, 1.0, HEAD_S, seg=96, rings=48)
for v in head.data.vertices: v.co = Vector(head_deform(v.co))
def muzzle_sdf(x, y, z):
    import numpy as np
    # rounded chin oval + a V wedge whose point sits between the eyes (top at z = 0)
    zt = 0.02
    wedge = np.maximum(np.abs(x) - (0.07 + 0.62 * (zt - z)), z - zt)
    wedge = np.maximum(wedge, -(z + 0.55))
    chin = ellipse(x, z, 0.40, 0.40, -0.55)
    front = (y + 0.30) * 2.0
    return np.maximum(np.minimum(wedge, chin), front)
head_img = bake_mask_texture(head, os.path.join(TEX_DIR, "cat_head.png"), muzzle_sdf, PALETTE["White"], PALETTE["Fur"])
head.data.materials.append(textured_material("HeadFur", head_img))

# Body with the white chest/belly.
body = sphere("Body", None, "Body", BODY_C, 0.58, BODY_S, seg=64, rings=32)
def chest_sdf(x, y, z):
    import numpy as np
    return np.maximum(ellipse(x, z, 0.64, 0.92, 0.02), (y + 0.22) * 2.0)
body_img = bake_mask_texture(body, os.path.join(TEX_DIR, "cat_body.png"), chest_sdf, PALETTE["White"], PALETTE["Fur"])
body.data.materials.append(textured_material("BodyFur", body_img))

# Ears: outer cone + pink inner cone poking through the front.
for side, bone in ((-1, "EarL"), (1, "EarR")):
    base = on_head((0.68 * side, 0.10, 0.70))
    tilt = (-8.0, 36.0 * side, 0.0)
    axis = Euler([math.radians(a) for a in tilt], 'XYZ').to_matrix() @ Vector((0, 0, 1))
    cone("EarOuter" + bone[-1], "Fur", bone, base + axis * 0.32, 0.46, 0.82, tilt)
    cone("EarInner" + bone[-1], "EarPink", bone, base + axis * 0.32 + Vector((0, -0.16, -0.03)), 0.34, 0.70, tilt)

# Arms: the -X arm is the white "glove", the +X arm dark with a white paw.
for side, bone, material in ((-1, "ArmL", "White"), (1, "ArmR", "Fur")):
    rot = (-30.0, 14.0 * side * -1, 0.0)
    arm = sphere("Arm" + bone[-1], material, bone, (0.46 * side, -0.32, 0.74), 0.26, (1, 1, 1.2), rot, seg=32, rings=20)
    if material == "Fur": paint_region(arm, "White", lambda c: c.z < -0.30)

# Feet: white socks.
for side, bone in ((-1, "LegL"), (1, "LegR")):
    sphere("Foot" + bone[-1], "White", bone, (0.28 * side, -0.16, 0.19), 0.25, (1.0, 1.15, 0.62), seg=32, rings=16)

# Tail: curls out to the right and up.
TAIL_PTS = [(0.25, 0.40, 0.42), (0.85, 0.50, 0.32), (1.10, 0.22, 0.82)]
tail = tube("Tail", "Fur", "Tail", TAIL_PTS, 0.17, radii=[1.0, 0.92, 0.72])

# Eyes: big yellow discs set into the face; pupil and glint in front.
EYE_U = {"L": (-0.54, -0.82, 0.0), "R": (0.54, -0.82, 0.0)}
EYE_C, EYE_N = {}, {}
for s, u in EYE_U.items():
    bone = "Eye" + s
    c = on_head(u); n = head_normal(u); EYE_C[s] = c; EYE_N[s] = n
    side = -1 if s == "L" else 1
    q = n.to_track_quat('-Y', 'Z')
    tilt = Matrix.Rotation(math.radians(-30.0 * side), 3, n)     # outer corners up (lemon-shaped)
    rot = [math.degrees(a) for a in (tilt @ q.to_matrix()).to_euler('XYZ')]
    sphere("Eye" + s, "Eye", bone, c, 0.36, (1.0, 0.36, 0.60), rot, seg=48, rings=24)
    sphere("Pupil" + s, "Pupil", bone, c + n * 0.13 + Vector((-0.04 * side, 0, 0.0)), 0.14, (0.45, 0.30, 1.05), rot, seg=32, rings=16)
    sphere("Glint" + s, "Glint", bone, c + n * 0.19 + Vector((-0.04, 0, 0.06)), 0.03, (1, 0.6, 1), rot, seg=16, rings=8)
    # closed-eye variants (Unity toggles): happy arch and sleepy line
    a = c + n * 0.17
    right = Vector((1, 0, 0)); up = Vector((0, 0, 1))
    tube("Happy" + s, "Ink", bone, [a - right * 0.25 - up * 0.08, a + up * 0.10, a + right * 0.25 - up * 0.08], 0.036)
    tube("Shut" + s, "Ink", bone, [a - right * 0.24 + up * 0.02, a - up * 0.06, a + right * 0.24 + up * 0.02], 0.034)

# Nose, mouths, whiskers, brows, blush, tears, sweat, heart, drool, dirt.
nose_c = on_head((0, -0.975, -0.33))
sphere("Nose", "White", "Head", nose_c, 0.075, (1.25, 0.65, 0.85), seg=24, rings=12)
m = on_head((0, -0.975, -0.47)) + Vector((0, -0.02, 0))
tube("MouthSmile", "Ink", "Head", [m + Vector((-0.09, 0, 0.03)), m + Vector((-0.045, 0, -0.025)), m + Vector((0, 0, 0.015)), m + Vector((0.045, 0, -0.025)), m + Vector((0.09, 0, 0.03))], 0.015)
tube("MouthFrown", "Ink", "Head", [m + Vector((-0.09, 0, -0.03)), m + Vector((0, 0, 0.025)), m + Vector((0.09, 0, -0.03))], 0.017)
sphere("MouthOpen", "Ink", "Head", m + Vector((0, 0, -0.01)), 0.07, (1.15, 0.5, 1.25), seg=24, rings=12)
sphere("MouthTongue", "Tongue", "Head", m + Vector((0, -0.02, -0.05)), 0.042, (1.1, 0.5, 0.9), seg=16, rings=8)
for side, s in ((-1, "L"), (1, "R")):
    base = on_head((0.62 * side, -0.70, -0.36))
    for wi, (dz, dy) in enumerate(((0.14, -0.30), (-0.02, -0.30), (-0.18, -0.28))):
        d = Vector((side * 1.0, dy, dz)); d.normalize()
        q = d.to_track_quat('Z', 'Y'); rot = [math.degrees(a) for a in q.to_euler('XYZ')]
        cylinder("Whisker%s%d" % (s, wi + 1), "Whisker", "Head", base + d * 0.26, 0.011, 0.52, rot, verts=8)
    bu = (0.46 * side, -0.74, 0.36)
    sphere("Brow" + s, "Ink", "Brow" + s, on_head(bu) + head_normal(bu) * 0.03, 0.09, (1.7, 0.4, 0.45), seg=24, rings=12)
    bl = (0.64 * side, -0.70, -0.24)
    sphere("Blush" + s, "Blush", "Head", on_head(bl) + head_normal(bl) * 0.02, 0.12, (1.35, 0.3, 0.8), [math.degrees(a) for a in head_normal(bl).to_track_quat('-Y', 'Z').to_euler('XYZ')], seg=24, rings=12)
    dp = (0.55 * side, -0.62, 0.20)
    sphere("Dirt" + s, "Dirt", "Head", on_head(dp) + head_normal(dp) * 0.02, 0.15, (1.3, 0.3, 0.9), [math.degrees(a) for a in head_normal(dp).to_track_quat('-Y', 'Z').to_euler('XYZ')], seg=24, rings=12)
sphere("Dirt3", "Dirt", "Body", BODY_C + Vector((-0.30, -0.62, 0.30)), 0.16, (1.2, 0.3, 0.8), (-40, 0, 0), seg=24, rings=12)
t = EYE_C["L"] + EYE_N["L"] * 0.18 + Vector((0.09, 0, -0.30))
sphere("Tear", "Tear", "Head", t, 0.07, (1.0, 0.55, 1.45), seg=24, rings=12)
sw = on_head((0.64, -0.60, 0.44)); sphere("Sweat", "Tear", "Head", sw + head_normal((0.64, -0.60, 0.44)) * 0.06, 0.07, (1.0, 0.6, 1.45), seg=24, rings=12)
dr = on_head((0.11, -0.97, -0.52)); sphere("Drool", "Tear", "Head", dr + Vector((0, -0.04, -0.06)), 0.045, (1.0, 0.6, 1.6), seg=16, rings=8)
hc = HEAD_C + Vector((1.05, -0.25, 1.05))
sphere("HeartL", "HeartRed", "Head", hc + Vector((-0.11, 0, 0.08)), 0.14, (1, 0.6, 1), seg=24, rings=12)
sphere("HeartR", "HeartRed", "Head", hc + Vector((0.11, 0, 0.08)), 0.14, (1, 0.6, 1), seg=24, rings=12)
cone("HeartTip", "HeartRed", "Head", hc + Vector((0, 0, -0.10)), 0.235, 0.30, (180, 0, 0), verts=32)
apply_transform(bpy.data.objects["HeartTip"])
bpy.data.objects["HeartTip"].scale = (1, 0.6, 1)

# Ground shadow (not weighted to any bone, stays on the floor; tinted by mood in Unity).
bpy.ops.mesh.primitive_circle_add(vertices=48, radius=1.15, fill_type='NGON', location=(0.05, 0.05, 0.005))
sh = bpy.context.active_object; sh.scale = (1.0, 0.6, 1.0)
finish(sh, "Shadow", "Shadow", None, smooth=False)

# Accessories (hidden by Unity unless equipped).
beanie = sphere("Acc_hat_beanie", "Beanie", "Head", HEAD_C + Vector((0, 0, 0.14)), 1.0, (1.15, 1.05, 1.02), seg=64, rings=32)
keep_faces(beanie, lambda c: c.z > 0.40)
sphere("Acc_hat_beanie_pom", "Pom", "Head", HEAD_C + Vector((0, 0, 1.20)), 0.22, seg=24, rings=12)
bpy.ops.mesh.primitive_torus_add(major_radius=0.66, minor_radius=0.17, major_segments=48, minor_segments=16, location=(0, 0.02, 1.36))
sc = bpy.context.active_object; sc.scale = (1.0, 0.92, 0.65); finish(sc, "Acc_scarf_star", "Scarf", "Body")
sphere("Acc_scarf_star_tail", "Scarf", "Body", (0.42, -0.60, 1.02), 0.14, (1, 0.7, 1.7), (-10, 0, 0), seg=24, rings=12)
bc = on_head((0.62, -0.30, 0.60))
sphere("Acc_bow_cherry_l", "Bow", "Head", bc + Vector((-0.16, 0, 0.02)), 0.17, (1.25, 0.6, 0.85), seg=24, rings=12)
sphere("Acc_bow_cherry_r", "Bow", "Head", bc + Vector((0.16, 0, 0.02)), 0.17, (1.25, 0.6, 0.85), seg=24, rings=12)
sphere("Acc_bow_cherry_knot", "Bow", "Head", bc + Vector((0, -0.05, 0)), 0.09, seg=16, rings=8)
cylinder("Acc_crown_tiny", "Crown", "Head", HEAD_C + Vector((0, 0.05, 1.04)), 0.36, 0.22, verts=24)
for i in range(5):
    a = math.radians(90 + i * 72)
    cone("Acc_crown_tiny_p%d" % i, "Crown", "Head", HEAD_C + Vector((math.cos(a) * 0.30, 0.05 + math.sin(a) * 0.30, 1.24)), 0.09, 0.22, verts=12)

for o, _ in OBJECTS:
    if o.name != "Shadow": apply_transform(o)

# ------------------------------------------------------------------ armature
arm_data = bpy.data.armatures.new("CatRig")
rig = bpy.data.objects.new("Armature", arm_data)
scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='EDIT')

def bone(name, head, tail, parent=None):
    b = arm_data.edit_bones.new(name)
    b.head = Vector(head); b.tail = Vector(tail); b.roll = 0.0
    if parent: b.parent = arm_data.edit_bones[parent]
    return b

bone("Root", (0, 0, 0), (0, 0, 0.35))
bone("Body", (0, 0.03, 0.38), (0, 0.03, 1.10), "Root")
bone("Head", (0, 0, 1.20), (0, 0, 2.50), "Body")
for side, n in ((-1, "L"), (1, "R")):
    base = on_head((0.68 * side, 0.10, 0.70))
    bone("Ear" + n, base, base + Vector((0.34 * side, -0.05, 0.62)), "Head")
    bone("Eye" + n, EYE_C[n], EYE_C[n] + Vector((0, 0, 0.25)), "Head")
    bu = (0.46 * side, -0.74, 0.36)
    bone("Brow" + n, on_head(bu) + head_normal(bu) * 0.03, on_head(bu) + head_normal(bu) * 0.03 + Vector((0, 0, 0.15)), "Head")
    bone("Arm" + n, (0.46 * side, -0.32, 1.02), (0.46 * side, -0.32, 1.27), "Body")
    bone("Leg" + n, (0.28 * side, -0.16, 0.40), (0.28 * side, -0.16, 0.65), "Body")
p0, p1, p2 = [Vector(p) for p in TAIL_PTS]
ta = p0; tb = (p0 + p1) / 2; tc = (p1 + p2) / 2; td = p2 + (p2 - p1).normalized() * 0.12
bone("Tail1", ta, tb, "Body"); bone("Tail2", tb, tc, "Tail1"); bone("Tail3", tc, td, "Tail2")
bpy.ops.object.mode_set(mode='OBJECT')

# ------------------------------------------------------------------ skinning (rigid, one group per part)
def tail_weights(v):
    t = max(0.0, min(1.0, (v.x - p0.x) / (p2.x - p0.x)))
    centres = (0.12, 0.50, 0.88)
    w = [max(0.0, 1.0 - abs(t - c) / 0.42) for c in centres]
    s = sum(w) or 1.0
    return [x / s for x in w]

for o, b in OBJECTS:
    o.parent = rig
    if b is None: continue                       # the shadow stays a plain static child of the rig
    mod = o.modifiers.new("Armature", 'ARMATURE'); mod.object = rig
    idx = list(range(len(o.data.vertices)))
    if b == "Tail":
        groups = [o.vertex_groups.new(name="Tail%d" % i) for i in (1, 2, 3)]
        for v in o.data.vertices:
            for g, w in zip(groups, tail_weights(v.co)):
                if w > 0.001: g.add([v.index], w, 'REPLACE')
    else:
        o.vertex_groups.new(name=b).add(idx, 1.0, 'REPLACE')

# ------------------------------------------------------------------ animation
BONES = ["Root", "Body", "Head", "EarL", "EarR", "EyeL", "EyeR", "BrowL", "BrowR", "ArmL", "ArmR", "LegL", "LegR", "Tail1", "Tail2", "Tail3"]
rig.animation_data_create()
for pb in rig.pose.bones: pb.rotation_mode = 'XYZ'
REST = {pb.name: pb.bone.matrix_local.to_3x3() for pb in rig.pose.bones}

def key(frame, name, loc=None, rot=None, scale=None):
    """Keys a bone with deltas expressed in WORLD axes (converted into the bone's rest frame)."""
    pb = rig.pose.bones[name]; R = REST[name]; Ri = R.inverted()
    if loc is not None:
        pb.location = Ri @ Vector(loc); pb.keyframe_insert("location", frame=frame)
    if rot is not None:
        W = Euler([math.radians(a) for a in rot], 'XYZ').to_matrix()
        pb.rotation_euler = (Ri @ W @ R).to_euler('XYZ'); pb.keyframe_insert("rotation_euler", frame=frame)
    if scale is not None:
        sx, sy, sz = scale
        # bone axes: which world axis does each local axis lie along?
        loc_scale = []
        for i in range(3):
            axis = R.col[i]
            a = max(range(3), key=lambda k: abs(axis[k]))
            loc_scale.append((sx, sy, sz)[a])
        pb.scale = loc_scale; pb.keyframe_insert("scale", frame=frame)

def action_fcurves(act):
    if hasattr(act, "fcurves"): return list(act.fcurves)
    out = []
    for layer in act.layers:
        for strip in layer.strips:
            for cb in strip.channelbags: out.extend(cb.fcurves)
    return out

def rest_all(frame):
    for b in BONES: key(frame, b, (0, 0, 0), (0, 0, 0), (1, 1, 1))

def clip(name, length, keys, loop=False):
    act = bpy.data.actions.new(name)
    rig.animation_data.action = act
    if hasattr(act, "slots") and rig.animation_data.action_slot is None:
        slot = act.slots.new(id_type='OBJECT', name="Armature")
        rig.animation_data.action_slot = slot
    for pb in rig.pose.bones:
        pb.location = (0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
    rest_all(0)
    for f, b, *rest in keys:
        loc = rest[0] if len(rest) > 0 else None
        rot = rest[1] if len(rest) > 1 else None
        sc = rest[2] if len(rest) > 2 else None
        key(f, b, loc, rot, sc)
    rest_all(length) if not loop else rest_all(length)
    # loops: the last frame must equal frame 0 — re-key what was touched at frame 0 onto the last frame
    if loop:
        for f, b, *rest in keys:
            if f == 0:
                key(length, b, *(rest + [None] * (3 - len(rest))))
    act.use_frame_range = True
    act.frame_start = 0; act.frame_end = length
    for fc in action_fcurves(act):
        for kp in fc.keyframe_points:
            kp.interpolation = 'BEZIER'; kp.easing = 'AUTO'
            if loop: kp.handle_left_type = kp.handle_right_type = 'AUTO_CLAMPED'
    act.use_fake_user = True
    return act

Z = (0, 0, 0); ONE = (1, 1, 1)
def rotk(f, b, x=0, y=0, z=0): return (f, b, None, (x, y, z))
def lock(f, b, x=0, y=0, z=0): return (f, b, (x, y, z))
def sck(f, b, x=1, y=1, z=1): return (f, b, None, None, (x, y, z))

# ---- loops
clip("Idle", 96, [
    sck(0, "Body", 1, 1, 1), sck(48, "Body", 1.015, 1.015, 1.03),
    lock(0, "Head", 0, 0, 0), lock(48, "Head", 0, 0, 0.035),
    rotk(0, "Head", 0, 0, 0), rotk(48, "Head", 0, 2.5, 0),
    rotk(0, "Tail1", 0, 0, -9), rotk(48, "Tail1", 0, 0, 9),
    rotk(0, "Tail2", 0, 0, -6), rotk(48, "Tail2", 0, 0, 8),
    rotk(0, "Tail3", 0, 0, -4), rotk(48, "Tail3", 0, 0, 10),
    rotk(0, "EarL"), rotk(28, "EarL"), rotk(31, "EarL", 0, -14, 0), rotk(35, "EarL"),
    rotk(0, "EarR"), rotk(70, "EarR"), rotk(73, "EarR", 0, 14, 0), rotk(77, "EarR"),
], loop=True)

clip("Happy", 48, [
    lock(0, "Root", 0, 0, 0), lock(12, "Root", 0, 0, 0.22), lock(24, "Root", 0, 0, 0), lock(36, "Root", 0, 0, 0.22),
    sck(0, "Body", 1.06, 1.06, 0.92), sck(12, "Body", 0.97, 0.97, 1.06), sck(24, "Body", 1.06, 1.06, 0.92), sck(36, "Body", 0.97, 0.97, 1.06),
    rotk(0, "Head", 0, -6, 0), rotk(24, "Head", 0, 6, 0),
    rotk(0, "EarL", -10, 0, 0), rotk(0, "EarR", -10, 0, 0),
    rotk(0, "Tail1", 0, 0, -22), rotk(12, "Tail1", 0, 0, 22), rotk(24, "Tail1", 0, 0, -22), rotk(36, "Tail1", 0, 0, 22),
    rotk(0, "Tail2", 0, 0, -18), rotk(12, "Tail2", 0, 0, 18), rotk(24, "Tail2", 0, 0, -18), rotk(36, "Tail2", 0, 0, 18),
    rotk(0, "ArmL", 0, 10, 0), rotk(12, "ArmL", 0, 22, 0), rotk(0, "ArmR", 0, -10, 0), rotk(12, "ArmR", 0, -22, 0),
], loop=True)

clip("Sad", 120, [
    rotk(0, "Head", 12, 0, 0), rotk(60, "Head", 13, 3, 0),
    rotk(0, "EarL", 10, -36, 0), rotk(0, "EarR", 10, 36, 0),
    rotk(0, "Tail1", 0, 38, 0), rotk(0, "Tail2", 0, 22, 0), rotk(60, "Tail2", 0, 26, 0),
    sck(0, "Body", 1, 1, 0.99), sck(60, "Body", 1.01, 1.01, 1.01),
    lock(0, "Root", 0, 0, -0.03),
], loop=True)

clip("Sleep", 150, [
    rotk(0, "Head", 18, 9, 0), rotk(75, "Head", 19, 9, 0),
    rotk(0, "EarL", 8, -26, 0), rotk(0, "EarR", 8, 26, 0),
    rotk(0, "Tail1", 0, 32, 0), rotk(0, "Tail2", 0, 24, 0),
    sck(0, "Body", 1.02, 1.02, 0.96), sck(75, "Body", 1.0, 1.0, 1.02),
    lock(0, "Root", 0, 0, -0.08),
    rotk(0, "ArmL", 0, 8, 0), rotk(0, "ArmR", 0, -8, 0),
], loop=True)

clip("Alert", 60, [
    rotk(0, "Head", -6, 0, 0), rotk(20, "Head", -6, 0, -9), rotk(40, "Head", -6, 0, 9),
    rotk(0, "EarL", -16, 0, 0), rotk(0, "EarR", -16, 0, 0),
    rotk(0, "Tail1", 0, -24, 0), rotk(0, "Tail2", 0, -12, 0),
    sck(0, "Body", 1, 1, 1.02),
], loop=True)

clip("Walk", 24, [
    rotk(0, "LegL", 26, 0, 0), rotk(12, "LegL", -26, 0, 0),
    rotk(0, "LegR", -26, 0, 0), rotk(12, "LegR", 26, 0, 0),
    rotk(0, "ArmL", -22, 0, 0), rotk(12, "ArmL", 22, 0, 0),
    rotk(0, "ArmR", 22, 0, 0), rotk(12, "ArmR", -22, 0, 0),
    lock(0, "Root", 0, 0, 0), lock(6, "Root", 0, 0, 0.07), lock(12, "Root", 0, 0, 0), lock(18, "Root", 0, 0, 0.07),
    rotk(0, "Body", 0, 0, 4), rotk(12, "Body", 0, 0, -4),
    rotk(0, "Head", 0, 3, 0), rotk(12, "Head", 0, -3, 0),
    rotk(0, "Tail1", 0, 0, 16), rotk(12, "Tail1", 0, 0, -16),
], loop=True)

clip("Fainted", 60, [
    rotk(0, "Root", 0, 88, 0), lock(0, "Root", -1.6, 0, 0.9),
    sck(0, "Body", 1, 1, 1), sck(30, "Body", 1.01, 1.01, 1.02),
    rotk(0, "ArmL", 0, 60, 0), rotk(0, "ArmR", 0, -60, 0),
    rotk(0, "LegL", -40, 0, 0), rotk(0, "LegR", -40, 0, 0),
    rotk(0, "EarL", 0, -30, 0), rotk(0, "EarR", 0, 30, 0),
], loop=True)

# ---- one-shots (additive in Unity: start AND end at rest)
clip("Hop", 16, [
    lock(0, "Root"), lock(7, "Root", 0, 0, 0.7), lock(14, "Root"),
    sck(2, "Body", 1.06, 1.06, 0.9), sck(7, "Body", 0.95, 0.95, 1.1), sck(14, "Body", 1.06, 1.06, 0.92),
    rotk(7, "EarL", -12, 0, 0), rotk(7, "EarR", -12, 0, 0),
])
clip("Wiggle", 20, [
    rotk(4, "Body", 0, 11, 0), rotk(10, "Body", 0, -11, 0), rotk(14, "Body", 0, 8, 0), rotk(17, "Body", 0, -4, 0),
    rotk(6, "Head", 0, -8, 0), rotk(12, "Head", 0, 8, 0), rotk(16, "Head", 0, -5, 0),
])
clip("Pat", 14, [
    sck(4, "Head", 1.08, 1.08, 0.85), sck(9, "Head", 0.97, 0.97, 1.05),
    lock(4, "Head", 0, 0, -0.09),
    rotk(4, "EarL", 0, -18, 0), rotk(4, "EarR", 0, 18, 0),
])
clip("WaveL", 30, [
    rotk(8, "ArmL", 0, 150, 0), rotk(12, "ArmL", 0, 166, 0), rotk(16, "ArmL", 0, 134, 0), rotk(20, "ArmL", 0, 166, 0), rotk(24, "ArmL", 0, 134, 0),
    rotk(8, "Head", 0, -6, 0), rotk(24, "Head", 0, -6, 0),
])
clip("WaveR", 30, [
    rotk(8, "ArmR", 0, -150, 0), rotk(12, "ArmR", 0, -166, 0), rotk(16, "ArmR", 0, -134, 0), rotk(20, "ArmR", 0, -166, 0), rotk(24, "ArmR", 0, -134, 0),
    rotk(8, "Head", 0, 6, 0), rotk(24, "Head", 0, 6, 0),
])
clip("TailFlick", 14, [
    rotk(4, "Tail1", 0, 0, 36), rotk(9, "Tail1", 0, 0, -30),
    rotk(5, "Tail2", 0, 0, 40), rotk(10, "Tail2", 0, 0, -34),
    rotk(6, "Tail3", 0, 0, 40), rotk(11, "Tail3", 0, 0, -30),
])
clip("Attack", 14, [
    lock(5, "Root", 0, -0.6, 0.05), lock(12, "Root"),
    rotk(5, "Body", 14, 0, 0), rotk(5, "Head", -4, 0, 0),
    rotk(5, "ArmL", -70, 0, 0), rotk(5, "ArmR", -70, 0, 0),
])
clip("Hurt", 14, [
    lock(3, "Root", 0, 0.35, 0), lock(6, "Root", 0.08, 0.2, 0), lock(9, "Root", -0.08, 0.1, 0),
    rotk(3, "Head", 0, 12, 0), rotk(8, "Head", 0, -8, 0),
    rotk(3, "EarL", 0, -28, 0), rotk(3, "EarR", 0, 28, 0), rotk(10, "EarL", 0, -20, 0), rotk(10, "EarR", 0, 20, 0),
])
clip("Eat", 36, [
    rotk(6, "Head", 18, 0, 0), rotk(12, "Head", 5, 0, 0), rotk(18, "Head", 18, 0, 0), rotk(24, "Head", 5, 0, 0), rotk(30, "Head", 18, 0, 0),
    sck(6, "Body", 1, 1, 0.98), sck(18, "Body", 1, 1, 0.98), sck(30, "Body", 1, 1, 0.98),
])
clip("Celebrate", 36, [
    lock(6, "Root", 0, 0, 0.5), lock(12, "Root"), lock(22, "Root", 0, 0, 0.5), lock(28, "Root"),
    rotk(6, "ArmL", 0, 150, 0), rotk(28, "ArmL", 0, 150, 0), rotk(6, "ArmR", 0, -150, 0), rotk(28, "ArmR", 0, -150, 0),
    rotk(6, "Head", -10, 0, 0), rotk(28, "Head", -10, 0, 0),
    rotk(6, "Tail1", 0, 0, 26), rotk(17, "Tail1", 0, 0, -26), rotk(28, "Tail1", 0, 0, 26),
])
clip("Dance", 48, [
    rotk(6, "Body", 0, 12, 0), rotk(18, "Body", 0, -12, 0), rotk(30, "Body", 0, 12, 0), rotk(42, "Body", 0, -12, 0),
    lock(6, "Root", 0, 0, 0.15), lock(12, "Root"), lock(18, "Root", 0, 0, 0.15), lock(24, "Root"), lock(30, "Root", 0, 0, 0.15), lock(36, "Root"), lock(42, "Root", 0, 0, 0.15),
    rotk(6, "ArmL", 0, 95, 0), rotk(18, "ArmL", 0, 20, 0), rotk(30, "ArmL", 0, 95, 0), rotk(42, "ArmL", 0, 20, 0),
    rotk(6, "ArmR", 0, -20, 0), rotk(18, "ArmR", 0, -95, 0), rotk(30, "ArmR", 0, -20, 0), rotk(42, "ArmR", 0, -95, 0),
    rotk(6, "Head", 0, -8, 0), rotk(18, "Head", 0, 8, 0), rotk(30, "Head", 0, -8, 0), rotk(42, "Head", 0, 8, 0),
])
clip("Nod", 18, [rotk(4, "Head", 15, 0, 0), rotk(9, "Head", 0, 0, 0), rotk(13, "Head", 12, 0, 0)])
clip("Shiver", 24, [
    *[rotk(f, "Body", 0, 4 if (f // 2) % 2 == 0 else -4, 0) for f in range(2, 23, 2)],
    *[rotk(f, "Head", 0, -3 if (f // 2) % 2 == 0 else 3, 0) for f in range(2, 23, 2)],
    rotk(3, "EarL", 0, -22, 0), rotk(20, "EarL", 0, -22, 0), rotk(3, "EarR", 0, 22, 0), rotk(20, "EarR", 0, 22, 0),
])
clip("Stretch", 40, [
    sck(12, "Body", 0.96, 0.96, 1.13), sck(24, "Body", 0.96, 0.96, 1.13),
    rotk(12, "ArmL", 0, 160, 0), rotk(24, "ArmL", 0, 160, 0), rotk(12, "ArmR", 0, -160, 0), rotk(24, "ArmR", 0, -160, 0),
    rotk(12, "Head", -12, 0, 0), rotk(24, "Head", -12, 0, 0),
    lock(12, "Root", 0, 0, 0.05), lock(24, "Root", 0, 0, 0.05),
])
clip("Yawn", 30, [rotk(8, "Head", -18, 0, 0), rotk(20, "Head", -18, 0, 0), sck(8, "Body", 1, 1, 1.05), sck(20, "Body", 1, 1, 1.05)])
clip("Shake", 18, [rotk(3, "Head", 0, 0, -20), rotk(8, "Head", 0, 0, 20), rotk(12, "Head", 0, 0, -15), rotk(15, "Head", 0, 0, 10)])
clip("EarTwitch", 10, [rotk(3, "EarR", 0, 20, 0), rotk(7, "EarR", 0, -8, 0)])
clip("LookAround", 44, [rotk(8, "Head", 0, 0, -25), rotk(16, "Head", 0, 0, -25), rotk(26, "Head", 0, 0, 25), rotk(34, "Head", 0, 0, 25)])
clip("Sniff", 24, [
    rotk(4, "Head", 10, 0, 0), rotk(8, "Head", 6, 0, 0), rotk(12, "Head", 12, 0, 0), rotk(16, "Head", 6, 0, 0), rotk(20, "Head", 12, 0, 0),
    lock(4, "Head", 0, -0.1, 0), lock(20, "Head", 0, -0.1, 0),
])
clip("Groom", 40, [
    rotk(8, "Head", 6, 22, 0), rotk(30, "Head", 6, 22, 0),
    rotk(8, "ArmR", -30, -120, 0), rotk(14, "ArmR", -30, -105, 0), rotk(20, "ArmR", -30, -120, 0), rotk(26, "ArmR", -30, -105, 0), rotk(30, "ArmR", -30, -120, 0),
])
rig.animation_data.action = bpy.data.actions["Idle"]

# ------------------------------------------------------------------ preview renders (Workbench: flat colours + outline)
HIDDEN_BY_DEFAULT = ["MouthFrown", "MouthOpen", "MouthTongue", "BrowL", "BrowR", "BlushL", "BlushR", "HappyL", "HappyR", "ShutL", "ShutR",
                     "Tear", "Sweat", "Drool", "HeartL", "HeartR", "HeartTip", "DirtL", "DirtR", "Dirt3"] + [o.name for o, _ in OBJECTS if o.name.startswith("Acc_")]

if PREVIEW_DIR:
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    scene.render.engine = 'BLENDER_WORKBENCH'
    sh = scene.display.shading
    sh.light = 'FLAT'; sh.color_type = 'TEXTURE'; sh.show_object_outline = True; sh.object_outline_color = (0.10, 0.07, 0.07)
    sh.show_shadows = False; sh.show_cavity = False
    scene.render.film_transparent = True
    scene.render.resolution_x = scene.render.resolution_y = 700
    scene.render.image_settings.file_format = 'PNG'; scene.render.image_settings.color_mode = 'RGBA'
    cam_data = bpy.data.cameras.new("Cam"); cam_data.lens_unit = 'FOV'; cam_data.angle = math.radians(26)
    cam = bpy.data.objects.new("Cam", cam_data); scene.collection.objects.link(cam); scene.camera = cam
    def aim(pos, target):
        cam.location = Vector(pos)
        cam.rotation_euler = (Vector(target) - Vector(pos)).to_track_quat('-Z', 'Y').to_euler()
    def show(names):
        for o, _ in OBJECTS: o.hide_render = o.name in HIDDEN_BY_DEFAULT and o.name not in names
    def shot(name, frame=0, action="Idle", visible=(), pos=(0, -10.8, 2.6), target=(0, 0, 1.75)):
        rig.animation_data.action = bpy.data.actions[action]
        scene.frame_set(frame); show(visible); aim(pos, target)
        scene.render.filepath = os.path.join(PREVIEW_DIR, name + ".png")
        bpy.ops.render.render(write_still=True)
    shot("front")
    shot("three-quarter", pos=(7.5, -7.8, 2.8))
    shot("side", pos=(10.8, 0, 2.2))
    shot("happy-face", visible=("HappyL", "HappyR", "BlushL", "BlushR"))
    shot("sad-face", frame=30, action="Sad", visible=("MouthFrown", "BrowL", "BrowR", "Tear"))
    shot("surprised", visible=("MouthOpen", "MouthTongue", "Sweat"))
    shot("love", visible=("HappyL", "HappyR", "BlushL", "BlushR", "HeartL", "HeartR", "HeartTip"))
    shot("sleep", frame=40, action="Sleep", visible=("ShutL", "ShutR"))
    shot("happy-hop", frame=12, action="Happy")
    shot("walk", frame=6, action="Walk")
    shot("wave", frame=12, action="WaveL")
    shot("stretch", frame=18, action="Stretch")
    shot("fainted", frame=10, action="Fainted", visible=("ShutL", "ShutR", "MouthOpen"))
    shot("beanie", visible=("Acc_hat_beanie", "Acc_hat_beanie_pom"))
    shot("scarf", visible=("Acc_scarf_star", "Acc_scarf_star_tail"))
    shot("bow", visible=("Acc_bow_cherry_l", "Acc_bow_cherry_r", "Acc_bow_cherry_knot"))
    shot("crown", visible=tuple(o.name for o, _ in OBJECTS if o.name.startswith("Acc_crown")))
    shot("dirty", visible=("DirtL", "DirtR", "Dirt3", "Drool"))
    for o, _ in OBJECTS: o.hide_render = False
    rig.animation_data.action = bpy.data.actions["Idle"]
    scene.frame_set(0)

# ------------------------------------------------------------------ export
if FBX_PATH:
    os.makedirs(os.path.dirname(os.path.abspath(FBX_PATH)), exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    for o, _ in OBJECTS: o.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=os.path.abspath(FBX_PATH), use_selection=True, object_types={'ARMATURE', 'MESH'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
        bake_space_transform=False, use_mesh_modifiers=True, mesh_smooth_type='OFF', use_custom_props=False,
        add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL',
        bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
        path_mode='STRIP', embed_textures=False)
    print("[build_cat] exported", FBX_PATH)

# ------------------------------------------------------------------ save .blend (optional, for opening in the GUI)
if BLEND_PATH:
    os.makedirs(os.path.dirname(os.path.abspath(BLEND_PATH)), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(BLEND_PATH))
    print("[build_cat] saved", BLEND_PATH)

print("[build_cat] objects:", len(OBJECTS), "actions:", [a.name for a in bpy.data.actions])
