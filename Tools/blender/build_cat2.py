# Gotchi cat, second take: built only from the painted tuxedo-cat reference
# (references/creature-character/cat/reference-tuxedo-cat-fish-pair.png, the left cat; the fish is ignored).
#
#   /Applications/Blender.app/Contents/MacOS/Blender -b -P Tools/blender/build_cat2.py -- \
#       --preview /tmp/gotchi-cat2 --blend /tmp/gotchi-cat2/cat2.blend [--fbx Assets/Resources/Creatures/Cat3D/cat.fbx]
#
# --fbx rigs the cat (bone names Cat3DView expects), authors the game's animation clips, strips the Blender-only outline
# shells, collapses materials to plain palette names and exports for Unity (legacy clips, one per action).
#
# Also runnable inside a live Blender window (exec through the MCP add-on): it clears the scene and rebuilds.
# Look: flat painted colours (emission, no lights), one soft shadow tone, thick wine-coloured outline drawn as an
# inverted hull (Solidify modifier, outline material with backface culling). Blender Z up, the cat faces -Y.

import bpy, bmesh, math, sys, os
from mathutils import Vector, Matrix, Euler

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
def arg(name, default=None):
    return argv[argv.index(name) + 1] if name in argv else default
PREVIEW_DIR = arg("--preview")
BLEND_PATH = arg("--blend")
FBX_PATH = arg("--fbx")
ANIM_DIR = arg("--anim")        # render a 6-frame strip per animation clip into this folder (implies rigging)
ANIM_ONLY = arg("--anim-only")  # with --anim: comma-separated clip names, render strips for these only
HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else os.getcwd()
REF_PATH = arg("--ref", os.path.normpath(os.path.join(HERE, "..", "..", "references", "creature-character", "cat",
                                                       "reference-tuxedo-cat-fish-pair.png")))

# ------------------------------------------------------------------ palette (sampled from the painting, sRGB hex)
PALETTE = {
    "Fur":          ("#5e4142", "#4d3435"),   # base, soft shadow
    "White":        ("#fdf1df", "#e6dbd0"),
    "EarPink":      ("#fc85ad", "#f06f9c"),
    "EarPinkLight": ("#fd9dbb", "#fb8bb0"),
    "EarPinkDeep":  ("#f4679c", "#ec5a90"),
    "EarTip":       ("#8d6f73", "#8d6f73"),
    "Eye":          ("#fbc437", "#f2b52a"),
    "Pupil":        ("#4a1c25", "#4a1c25"),
    "Nose":         ("#47102a", "#47102a"),
    "Whisker":      ("#d9cbc0", "#d9cbc0"),
    "FurMark":      ("#563a3c", "#563a3c"),
    "Stripe":       ("#4a3133", "#4a3133"),
    "FaceWhite":    ("#fdf1df", "#e6dbd0"),   # export name for the white face mark (Unity draws it without an outline)
    "Tongue":       ("#ed7a8f", "#ed7a8f"),
    "Blush":        ("#f7a3b3", "#f7a3b3"),
    "Tear":         ("#7ac2f0", "#7ac2f0"),
    "HeartRed":     ("#ed546b", "#ed546b"),
    "Dirt":         ("#85664a", "#85664a"),
    "Beanie":       ("#f59eb3", "#f59eb3"),
    "Pom":          ("#fdf1df", "#e6dbd0"),
    "Scarf":        ("#8fd1c7", "#8fd1c7"),
    "Bow":          ("#e85461", "#e85461"),
    "Crown":        ("#f7c747", "#f7c747"),
    "Shadow":       ("#4d4050", "#4d4050"),
    "Pad":          ("#cfc6c0", "#c3b9b3"),
    "Ink":          ("#47102a", "#47102a"),
}
BG_HEX = "#eae5e2"
OUTLINE = 0.055          # internal boundaries (muzzle, bib); head radius is ~1
OUTLINE_BIG = 0.085      # silhouette shapes: head, ears, body, tail
OUTLINE_LIMB = 0.065     # legs, feet

def srgb_to_linear(hexstr):
    h = hexstr.lstrip("#"); out = []
    for i in (0, 2, 4):
        c = int(h[i:i + 2], 16) / 255.0
        out.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return tuple(out)

# ------------------------------------------------------------------ scene reset (works headless and in a live window)
def reset_scene():
    if bpy.app.background:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        return
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights,
                 bpy.data.images, bpy.data.worlds, bpy.data.armatures, bpy.data.actions):
        for d in list(coll):
            try: coll.remove(d)
            except Exception: pass

reset_scene()
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.eevee.taa_render_samples = 16
scene.render.film_transparent = False
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.resolution_x = scene.render.resolution_y = 900
scene.render.resolution_percentage = 100

world = bpy.data.worlds.new("World"); world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (*srgb_to_linear(BG_HEX), 1)
scene.world = world

# ------------------------------------------------------------------ materials: flat emission + one soft shadow tone
MATS = {}
def mat(name, shade_amount=1.0):
    key = (name, shade_amount)
    if key in MATS: return MATS[key]
    base_hex, shade_hex = PALETTE[name]
    m = bpy.data.materials.new(name if shade_amount == 1.0 else f"{name}_{shade_amount:.2f}")
    m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    e_base = nt.nodes.new("ShaderNodeEmission"); e_base.inputs["Color"].default_value = (*srgb_to_linear(base_hex), 1)
    e_shade = nt.nodes.new("ShaderNodeEmission"); e_shade.inputs["Color"].default_value = (*srgb_to_linear(shade_hex), 1)
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    rng = nt.nodes.new("ShaderNodeMapRange")
    rng.inputs["From Min"].default_value = -0.85; rng.inputs["From Max"].default_value = -0.15
    rng.inputs["To Min"].default_value = shade_amount; rng.inputs["To Max"].default_value = 0.0
    mix = nt.nodes.new("ShaderNodeMixShader")
    nt.links.new(geo.outputs["Normal"], sep.inputs["Vector"])
    nt.links.new(sep.outputs["Z"], rng.inputs["Value"])
    nt.links.new(rng.outputs["Result"], mix.inputs["Fac"])
    nt.links.new(e_base.outputs["Emission"], mix.inputs[1])
    nt.links.new(e_shade.outputs["Emission"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    m.diffuse_color = (*srgb_to_linear(base_hex), 1)
    MATS[key] = m
    return m

def outline_mat():
    if "Outline" in bpy.data.materials: return bpy.data.materials["Outline"]
    m = bpy.data.materials.new("Outline"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    e = nt.nodes.new("ShaderNodeEmission"); e.inputs["Color"].default_value = (*srgb_to_linear(PALETTE["Ink"][0]), 1)
    nt.links.new(e.outputs["Emission"], out.inputs["Surface"])
    m.use_backface_culling = True
    m.diffuse_color = (*srgb_to_linear(PALETTE["Ink"][0]), 1)
    return m

# ------------------------------------------------------------------ mesh helpers (bmesh, no operators: context-safe)
OBJECTS = []
FUR_SHADE = 0.35   # the painting's fur is nearly flat; whites keep a soft grey shade
def new_object(name, bm, material, outline=OUTLINE, smooth=True, shade=1.0, even=True, extras=()):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = smooth
    o = bpy.data.objects.new(name, me)
    scene.collection.objects.link(o)
    if material: me.materials.append(mat(material, min(shade, FUR_SHADE) if material == "Fur" else shade))
    if outline:
        me.materials.append(outline_mat())
        sol = o.modifiers.new("Outline", 'SOLIDIFY')
        sol.thickness = outline; sol.offset = 1.0; sol.use_flip_normals = True; sol.use_rim = False
        sol.material_offset = 1; sol.use_even_offset = even; sol.use_quality_normals = True
    for e in extras:                       # painted regions: colour slot, each followed by an outline slot for the hull
        me.materials.append(mat(e, 0.0))
        if outline: me.materials.append(outline_mat())
    OBJECTS.append(o)
    return o

def basis(normal, up=Vector((0, 0, 1))):
    """Orthonormal frame with local Z = normal, local Y ≈ up. Returns a 3x3 Matrix with columns (X, Y, Z)."""
    n = normal.normalized()
    y = (up - up.dot(n) * n)
    if y.length < 1e-4: y = Vector((0, -1, 0)) - Vector((0, -1, 0)).dot(n) * n
    y.normalize()
    x = y.cross(n)
    return Matrix((x, y, n)).transposed()

def ellipsoid(name, material, center, radii, outline=OUTLINE, seg=64, rings=32, frame=None, shade=1.0, deform=None, even=True,
              paint=None, extras=()):
    """paint(unit) -> 0 for the base colour or i for extras[i-1]; decided per face on the undeformed unit sphere."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    units = [v.co.copy() for v in bm.verts]
    for v in bm.verts:
        p = Vector((v.co.x * radii[0], v.co.y * radii[1], v.co.z * radii[2]))
        if deform: p = deform(p, v.co)
        if frame: p = frame @ p
        v.co = p + Vector(center)
    o = new_object(name, bm, material, outline, shade=shade, even=even, extras=extras)
    if paint:
        for poly in o.data.polygons:
            u = Vector((0, 0, 0))
            for vi in poly.vertices: u += units[vi]
            i = paint(u / len(poly.vertices))
            if i: poly.material_index = 2 * i if outline else i
    return o

def tube(name, material, points, radii, outline=OUTLINE, res=24, shade=1.0):
    """Smooth bevelled bezier turned into a mesh; radii per control point."""
    curve = bpy.data.curves.new(name + "Curve", 'CURVE')
    curve.dimensions = '3D'; curve.bevel_depth = 1.0; curve.bevel_resolution = 10
    curve.resolution_u = res; curve.use_fill_caps = True
    sp = curve.splines.new('BEZIER'); sp.bezier_points.add(len(points) - 1)
    for i, p in enumerate(points):
        bp = sp.bezier_points[i]; bp.co = Vector(p)
        bp.handle_left_type = bp.handle_right_type = 'AUTO'; bp.radius = radii[i]
    tmp = bpy.data.objects.new(name + "Tmp", curve); scene.collection.objects.link(tmp)
    deps = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(tmp.evaluated_get(deps))
    bpy.data.objects.remove(tmp, do_unlink=True); bpy.data.curves.remove(curve)
    bm = bmesh.new(); bm.from_mesh(me); bpy.data.meshes.remove(me)
    return new_object(name, bm, material, outline, shade=shade)

def rod(name, material, start, end, radius, outline=0):
    bm = bmesh.new()
    d = Vector(end) - Vector(start); L = d.length
    bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=radius, radius2=radius * 0.6, depth=L)
    M = basis(d)
    for v in bm.verts:
        v.co = M @ Vector((v.co.x, v.co.y, v.co.z + L / 2)) + Vector(start)
    return new_object(name, bm, material, outline)

def rounded_triangle(base_w, height, tip_lean=0.06, n=16):
    """2D outline (u, v) of an ear: straight base, convex sides, softly rounded tip. Counter-clockwise."""
    bl, br, tip = Vector((-base_w / 2, 0)), Vector((base_w / 2, 0)), Vector((tip_lean, height))
    def quad(a, c, b, t): return (1 - t) ** 2 * a + 2 * (1 - t) * t * c + t ** 2 * b
    cl = Vector((-base_w / 2 - 0.02, height * 0.62)); cr = Vector((base_w / 2 + 0.04, height * 0.6))
    pts = [quad(br, cr, tip, i / n) for i in range(n)] + [quad(tip, cl, bl, i / n) for i in range(n)] + [bl.lerp(br, i / 4) for i in range(4)]
    for _ in range(2):  # Chaikin smoothing rounds the tip (and the hidden base corners)
        out = []
        for i in range(len(pts)):
            a, b = pts[i], pts[(i + 1) % len(pts)]
            out += [a * 0.75 + b * 0.25, a * 0.25 + b * 0.75]
        pts = out
    return pts

def slab(name, material, outline2d, thickness, origin, frame, outline=OUTLINE, subsurf=2, shade=1.0, u_sign=1.0):
    bm = bmesh.new()
    front = [bm.verts.new(frame @ Vector((p.x * u_sign, p.y, thickness / 2)) + origin) for p in outline2d]
    back = [bm.verts.new(frame @ Vector((p.x * u_sign, p.y, -thickness / 2)) + origin) for p in outline2d]
    bm.faces.new(front if u_sign > 0 else front[::-1])
    bm.faces.new(back[::-1] if u_sign > 0 else back)
    n = len(front)
    for i in range(n):
        quad = [front[i], back[i], back[(i + 1) % n], front[(i + 1) % n]]
        bm.faces.new(quad if u_sign > 0 else quad[::-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    o = new_object(name, bm, material, outline, shade=shade)
    if subsurf:
        ss = o.modifiers.new("Smooth", 'SUBSURF'); ss.levels = ss.render_levels = subsurf
        o.modifiers.move(len(o.modifiers) - 1, 0)   # subdivide before the outline hull
    return o

# ------------------------------------------------------------------ the cat: layout (units: head radius ≈ 1, ground z = 0)
HC = Vector((0, 0, 2.00)); HR = (1.24, 0.98, 0.90)          # head centre / radii (x across, y depth, z up): a soft ellipse
BC = Vector((0, 0, 0.78)); BR = (0.92, 0.76, 0.72)          # torso: short rounded bean under the head, standing on two feet
NECK = Vector((0, 0, 1.40))

def head_point(az_deg, el_deg):
    """Point on the head ellipsoid + its outward normal, from azimuth (0 = front, + toward +X) and elevation."""
    az, el = math.radians(az_deg), math.radians(el_deg)
    d = Vector((math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el), math.sin(el)))
    p = Vector((d.x * HR[0], d.y * HR[1], d.z * HR[2]))
    n = Vector((p.x / HR[0] ** 2, p.y / HR[1] ** 2, p.z / HR[2] ** 2)).normalized()
    return HC + p, n

def cheek_deform(p, unit):
    """Fuller lower half: cheeks swell a little below the eye line, the crown stays round."""
    t = max(0.0, -unit.z)                      # 0 at/above centre, 1 at the chin
    swell = 1.0 + 0.04 * math.sin(math.pi * min(t, 1.0))
    return Vector((p.x * swell, p.y * (1 + 0.03 * math.sin(math.pi * min(t, 1.0))), p.z))

U_PER_RAD, V_PER_RAD = 1.24, 0.92                  # surface arc per radian (head radii), for laying parts onto the head

def head_surface(az_deg, el_deg):
    """Point on the actual (cheek-swollen) head surface and its outward normal."""
    p, n = head_point(az_deg, el_deg)
    rel = p - HC
    unit = Vector((rel.x / HR[0], rel.y / HR[1], rel.z / HR[2]))
    return HC + cheek_deform(rel, unit), n

def head_cap(name, material, az0, el0, deform, roll_deg=0.0, base_h=0.0, seg=96, rings=48, shade=1.0, outline=0):
    """A deformed sphere laid onto the curved head: its local (x, y) become surface offsets from (az0, el0) and its z the
    height along the local normal (plus base_h), so rims follow the head instead of floating; the lower half is buried."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    c, sn = math.cos(math.radians(roll_deg)), math.sin(math.radians(roll_deg))
    for v in bm.verts:
        q = deform(None, v.co)
        x, y = q.x * c - q.y * sn, q.x * sn + q.y * c
        el = el0 + math.degrees(y / V_PER_RAD)
        az = az0 + math.degrees(x / (U_PER_RAD * math.cos(math.radians(el))))
        p, n = head_surface(az, el)
        v.co = p + n * (q.z + base_h)
    return new_object(name, bm, material, outline, shade=shade)

def smin(a, b, k):
    if k <= 0: return min(a, b)
    h = max(0.0, min(1.0, 0.5 + 0.5 * (b - a) / k))
    return b * (1 - h) + a * h - k * h * (1 - h)
def smax(a, b, k): return -smin(-a, -b, k)

def poly_sd(pts):
    """Signed distance to a closed 2D polygon (negative inside); works for any simple outline."""
    n = len(pts)
    def f(x, y):
        d2 = 1e9; inside = False
        for k in range(n):
            ax, ay = pts[k]; bx, by = pts[(k + 1) % n]
            ex, ey = bx - ax, by - ay; wx, wy = x - ax, y - ay
            t = max(0.0, min(1.0, (wx * ex + wy * ey) / (ex * ex + ey * ey)))
            qx, qy = wx - ex * t, wy - ey * t
            d2 = min(d2, qx * qx + qy * qy)
            if (ay > y) != (by > y) and x < ax + (y - ay) * ex / ey: inside = not inside
        return -math.sqrt(d2) if inside else math.sqrt(d2)
    return f

def surface_patch(name, material, frame, origin, deform, sd_fn, centre, lift=0.008, spokes=96, rings=8, shade=0.0, to_unit=None, reach=1.5):
    """A smooth-edged flat-colour patch lying on the front face of a deformed-sphere part: sd_fn(x, y) is a 2D signed
    distance (negative inside, star-shaped from centre) in the part's unit-disk coordinates, or in the part's own local
    2D coordinates when to_unit(px, py) -> (x, y) is given; vertices sit on the part's surface, lifted along its true normal."""
    def P(x, y):
        return deform(None, Vector((x, y, math.sqrt(max(0.0, 1 - x * x - y * y)))))
    def surf(px, py):
        x, y = to_unit(px, py) if to_unit else (px, py)
        h = 1e-3
        q = P(x, y)
        n = (P(x + h, y) - P(x - h, y)).cross(P(x, y + h) - P(x, y - h)).normalized()
        if n.z < 0: n = -n
        return frame @ (q + n * lift) + origin
    cx, cy = centre
    R = []
    for i in range(spokes):
        th = 2 * math.pi * i / spokes; dx, dy = math.cos(th), math.sin(th)
        lo, hi = 0.0, reach
        for _ in range(28):
            mid = (lo + hi) / 2
            if sd_fn(cx + dx * mid, cy + dy * mid) < 0: lo = mid
            else: hi = mid
        R.append((lo + hi) / 2)
    bm = bmesh.new()
    c = bm.verts.new(surf(cx, cy))
    rows = []
    for r in range(1, rings + 1):
        t = r / rings
        rows.append([bm.verts.new(surf(cx + math.cos(2 * math.pi * i / spokes) * R[i] * t,
                                       cy + math.sin(2 * math.pi * i / spokes) * R[i] * t)) for i in range(spokes)])
    for i in range(spokes):
        j = (i + 1) % spokes
        bm.faces.new((c, rows[0][i], rows[0][j]))
        for r in range(rings - 1):
            bm.faces.new((rows[r][i], rows[r + 1][i], rows[r + 1][j], rows[r][j]))
    return new_object(name, bm, material, 0, shade=shade)

head_pivot = bpy.data.objects.new("HeadPivot", None)
head_pivot.empty_display_type = 'SPHERE'; head_pivot.empty_display_size = 0.2
head_pivot.location = NECK
scene.collection.objects.link(head_pivot)
HEAD_PARTS = []

def head_part(o):
    HEAD_PARTS.append(o); return o

head = head_part(ellipsoid("Head", "Fur", HC, HR, deform=cheek_deform, outline=OUTLINE_BIG))

# ears: big rounded-triangle pillows (deformed spheres, so the outline hull stays smooth), splayed outward,
# pink inner face with a lighter core
def ear_shape(base_w, height, thick, taper=0.82, lean=0.06):
    def f(p, unit):
        vn = min(1.0, max(0.0, (unit.y + 1) / 2))           # 0 at the base, 1 at the tip (clamped: poles can be 1e-8 past)
        width = 1 - taper * vn ** 1.35
        return Vector((unit.x * base_w / 2 * width + lean * vn * base_w, vn * height,
                       unit.z * thick / 2 * (1 - 0.45 * vn)))
    return f

EAR_RIG = {}   # tag -> (origin, up axis), for the ear bones
def ear(side):  # side +1 = cat's left (+X), -1 = cat's right (-X)
    p, n = head_point(side * 40, 42)
    yaw_r, splay_r = math.radians(35), math.radians(27)
    front = Vector((side * math.sin(yaw_r), -math.cos(yaw_r), 0.0))                 # pink faces forward-outward, level
    across0 = Vector((0, 0, 1)).cross(front)                                          # horizontal base line (local +x ≈ world +X)
    up = Vector((0, 0, 1)) * math.cos(splay_r) + across0 * (side * math.sin(splay_r))   # ear axis, tip leaning outward
    across = across0 * math.cos(splay_r) - Vector((0, 0, 1)) * (side * math.sin(splay_r))
    frame = Matrix((across, up, front)).transposed()
    origin = p - up * 0.30
    tag = "L" if side > 0 else "R"
    EAR_RIG[tag] = (origin.copy(), up.copy())
    W, H, T, TAPER = 1.02, 1.06, 0.18, 0.90                                           # thin leaf, sharp tip
    LEAN = 0.06 * side                                                                # tip leans outward on both ears
    shape = ear_shape(W, H, T, taper=TAPER, lean=LEAN)
    head_part(ellipsoid(f"Ear{tag}", "Fur", origin, (1, 1, 1), frame=frame, deform=shape, even=False, outline=OUTLINE_BIG, seg=128, rings=64))
    # pink face = the ear's own outline inset by a thin band, with a wider fur band along the inner edge (tapering toward
    # the tip) and the grey-mauve cap at the tip; the deeper pink spot sits low in the pink. All in the ear's local
    # coordinates (x across, y = height from the base), so the edges run parallel to the ear's edges.
    outline = poly_sd([tuple(shape(None, Vector((math.cos(2 * math.pi * k / 180), math.sin(2 * math.pi * k / 180), 0.0))).xy) for k in range(180)])
    def to_unit(lx, ly):
        vn = min(0.999, max(0.001, ly / H)); width = 1 - TAPER * vn ** 1.35
        return (lx - LEAN * vn * W) / (W / 2 * width), 2 * vn - 1
    def pink_sd(lx, ly):
        vn = ly / H
        d = smax(outline(lx, ly) + 0.045, (-0.17 + 0.17 * vn) - lx * side, 0.05)  # inset 0.045; inner fur band tapering toward the tip
        return smax(d, ly - 0.90 * H, 0.04)                                       # stops under the tip cap
    def spot_sd(lx, ly):
        return (math.hypot((lx - (0.10 * side + LEAN * 0.40 * W)) / 0.17, (ly - 0.42) / 0.15) - 1.0) * 0.15
    def tip_sd(lx, ly):
        return smax(outline(lx, ly) + 0.025, 0.90 * H - ly, 0.03)
    head_part(surface_patch(f"EarInner{tag}", "EarPink", frame, origin, shape, pink_sd, (0.12 * side + LEAN * 0.45 * W, 0.45 * H), lift=0.008, to_unit=to_unit, reach=1.6))
    head_part(surface_patch(f"EarSpot{tag}", "EarPinkDeep", frame, origin, shape, spot_sd, (0.10 * side + LEAN * 0.40 * W, 0.42), lift=0.011, spokes=64, rings=4, to_unit=to_unit, reach=0.5))
    head_part(surface_patch(f"EarTip{tag}", "EarTip", frame, origin, shape, tip_sd, (LEAN * 0.95 * W, 0.95 * H), lift=0.008, spokes=48, rings=3, to_unit=to_unit, reach=0.4))
    # the painting shades the crown a little darker at each ear base
    q, m = head_point(side * 24, 56)
    head_part(ellipsoid(f"CrownMark{tag}", "FurMark", q - m * 0.008, (0.15, 0.09, 0.025), frame=basis(m), outline=0, shade=0.0))
ear(+1); ear(-1)

# eyes: big amber teardrops (round inner-lower side, pointed outer-upper corner) tilted outer-up, each on a thin
# wine rim plate, with narrow rounded-rectangle pupils. Stacked lenses just above the face, no Solidify.
def teardrop(w, h, d, side, tip_deg=140, L=1.35):
    """Oval pulled to a single corner: the boundary is the convex hull of the unit circle and a point at distance L
    in direction tip_deg (measured from the OUTER side toward up, so 140 deg = upper-inner)."""
    beta = math.acos(1.0 / L)
    def f(q, unit):
        x, y = unit.x * side, unit.y                                     # x: +1 = outer corner
        grow = 1.0
        if math.hypot(x, y) > 1e-6:
            delta = (math.atan2(y, x) - math.radians(tip_deg) + math.pi) % (2 * math.pi) - math.pi
            if abs(delta) <= beta:
                grow = 1.0 / math.cos(abs(delta) - beta)
        return Vector((unit.x * w / 2 * grow, unit.y * h / 2 * grow, unit.z * d / 2))
    return f

def lemon(length, ratio, d, e_pos=1.3, e_neg=0.95):
    """Pointed oval along local X: the +X end sharp (e > 1), the -X end as sharp as e_neg makes it (e < 1 rounds it)."""
    def f(q, unit):
        x, y = unit.x, unit.y
        e = e_pos if x > 0 else e_neg
        sq = max(0.0, 1 - x * x) ** (e - 0.5)
        return Vector((x * length / 2, y * ratio * sq * length / 2, unit.z * d / 2))
    return f

def rounded_rect(w, h, d, n=3.5):
    def f(q, unit):
        x, y = unit.x, unit.y
        r = math.hypot(x, y)
        rr = (abs(x / r) ** n + abs(y / r) ** n) ** (-1.0 / n) if r > 1e-6 else 1.0
        return Vector((x * rr * w / 2, y * rr * h / 2, unit.z * d / 2))
    return f

EYE_TILT = 5          # deg the HORIZONTAL almond tilts: inner (nose) corner up, outer (ear) corner down, as in the painting
PUPIL_TO_NOSE = 0.16  # vertical pupil sits toward the nose (measured from the user's Blender tweaks)
EYE_ROLL_IN = 4.8     # deg the eye's top rolls toward the nose (measured from the user's Blender tweaks)
PUPIL_POP = 0.012     # how far the pupil stands above the eye surface: "a very, very tiny bit"

def pupil_shell(pw, ph, eye_w, eye_h, eye_d, offx, pop=PUPIL_POP, back=0.06):
    """A pill that follows the curved eye lens: its face is the lens surface lifted by `pop` everywhere."""
    def f(q, unit):
        u = unit.x * pw / 2 + offx; v = unit.y * ph / 2
        r2 = (u / (eye_w / 2)) ** 2 + (v / (eye_h / 2)) ** 2
        lens = (eye_d / 2) * math.sqrt(max(0.0, 1 - r2))
        return Vector((u, v, lens + pop if unit.z >= 0 else lens - back))
    return f

def almond(w, h, d, side, L_inner=1.34, L_outer=1.05, inner_down_deg=18, outer_up_deg=4):
    """Round eye body pulled to two short corners: the boundary is the convex hull of the unit circle and two
    points, toward the nose (inner, a touch above level) and toward the ear (outer, a touch below level)."""
    tips = [(math.radians(180 + inner_down_deg), L_inner), (math.radians(outer_up_deg), L_outer)]   # inner corner aims down at the nose
    def f(q, unit):
        x, y = unit.x * side, unit.y                                     # x: +1 = outer (ear) side
        grow = 1.0
        if math.hypot(x, y) > 1e-6:
            phi = math.atan2(y, x)
            for tip, L in tips:
                beta = math.acos(1.0 / L)
                delta = (phi - tip + math.pi) % (2 * math.pi) - math.pi
                if abs(delta) <= beta:
                    grow = max(grow, 1.0 / math.cos(abs(delta) - beta))
        return Vector((unit.x * w / 2 * grow, unit.y * h / 2 * grow, unit.z * d / 2))
    return f

def eye(side):
    tag = "L" if side > 0 else "R"
    az0, el0 = side * 26.4, -0.75                                          # measured from the user's Blender tweaks
    W, H, RIM = 0.65, 0.61, 0.03
    D_EYE, D_RIM = 0.09, 0.07                                              # shallow domes: the eye bulges ~0.05, the rim ~0.03
    roll = EYE_ROLL_IN * side                                              # top rolled a little toward the nose
    head_part(head_cap(f"EyeRim{tag}", "Ink", az0, el0, almond(W + 2 * RIM, H + 2 * RIM, D_RIM, side), roll, base_h=-0.002, shade=0.0))
    head_part(head_cap(f"Eye{tag}", "Eye", az0, el0, almond(W, H, D_EYE, side), roll, base_h=0.004, shade=0.5))
    PW, PH = 0.14, 0.39
    head_part(head_cap(f"Pupil{tag}", "Pupil", az0, el0, pupil_shell(PW, PH, W, H, D_EYE, -side * PUPIL_TO_NOSE), roll, base_h=0.004, shade=0.0))
eye(+1); eye(-1)

# the white face mark: ONE piece, like the painting — a triangle (blaze) between the eyes flowing into two plump
# lobes, joined by smooth fillets so a single continuous outline runs around all of it. Built as a closed "cookie" on
# the head surface: a thin white slab whose outline is the smooth union of triangle + two ellipses, with the lobes as
# domes rising out of the slab and a shallow crease between them that carries the nose line.
def wedge(top_w, bot_w, height, thick, power=1.2):
    def f(q, unit):
        t = (1 - unit.y) / 2                                             # 0 at the top, 1 at the bottom
        w = top_w + (bot_w - top_w) * t ** power
        return Vector((unit.x * w / 2, unit.y * height / 2, unit.z * thick / 2))
    return f

FACE_EL0 = -15.4                                   # (u, v) surface coordinates are centred here: the nose
LOBE_C, LOBE_R = (0.12, -0.14), (0.19, 0.20)       # lobe centres (±u, v) and 2D semi-axes, in surface units
LOBE_DOME, BUMP_R = 0.14, (0.21, 0.22)             # dome height above the slab; bump radii (a cosine bump meets the slab tangentially)
TRI_TOP, TRI_BASE, TRI_HALF = 0.34, -0.08, 0.17    # blaze: apex v (visible tip lands at the eyes' upper third), base v, base half-width
TRI_ROUND = 0.03                                   # corner rounding, so the outline shell wraps the tip as a thin cap instead of a spike
SLAB_H = 0.006                                     # the white mark lies on the head (just clear of its mesh)
STROKE = 0.0                                       # painted outline band around the white mark (0 = none, as requested)
SEAM_W = (0.012, 0.007)                            # painted seam half-width under the nose / at the bottom

def uv_to_angles(u, v):
    el = FACE_EL0 + math.degrees(v / V_PER_RAD)
    return math.degrees(u / (U_PER_RAD * math.cos(math.radians(el)))), el

def face_point(u, v, h):
    az, el = uv_to_angles(u, v)
    p, n = head_surface(az, el)
    return p + n * h

def sd_ellipse(u, v, cu, cv, a, b):
    return (math.sqrt(((u - cu) / a) ** 2 + ((v - cv) / b) ** 2) - 1.0) * min(a, b)
def sd_polygon(u, v, pts):
    """Exact signed distance to a convex polygon (negative inside)."""
    n = len(pts); dx = 1e9; dy = 1e9
    e0 = (pts[1][0] - pts[0][0], pts[1][1] - pts[0][1]); e2 = (pts[0][0] - pts[-1][0], pts[0][1] - pts[-1][1])
    sgn = 1.0 if (e0[0] * e2[1] - e0[1] * e2[0]) > 0 else -1.0
    for k in range(n):
        (ax, ay), (bx, by) = pts[k], pts[(k + 1) % n]
        ex, ey = bx - ax, by - ay; wx, wy = u - ax, v - ay
        t = max(0.0, min(1.0, (wx * ex + wy * ey) / (ex * ex + ey * ey)))
        qx, qy = wx - ex * t, wy - ey * t
        dx = min(dx, qx * qx + qy * qy)
        dy = min(dy, sgn * (wx * ey - wy * ex))
    return -math.sqrt(dx) * (1.0 if dy > 0 else -1.0)

def sd_triangle(u, v):
    """The blaze: an isosceles triangle with corners rounded by TRI_ROUND (shrink the triangle, then grow the distance)."""
    r = TRI_ROUND
    L = math.hypot(TRI_HALF, TRI_TOP - TRI_BASE); nx, ny = (TRI_TOP - TRI_BASE) / L, TRI_HALF / L   # outward normal of the right side
    apex_v = TRI_TOP - r / ny
    base_v = TRI_BASE + r
    base_u = (ny * (TRI_TOP - base_v) - r) / nx
    return sd_polygon(u, v, [(0.0, apex_v), (-base_u, base_v), (base_u, base_v)]) - r

def face_sd(u, v):
    k_lobes = 0.10 + (0.02 - 0.10) * max(0.0, min(1.0, (v - LOBE_C[1] + 0.03) / 0.06))   # tight blend above the lobe centres, generous below
    lobes = smin(sd_ellipse(u, v, -LOBE_C[0], LOBE_C[1], *LOBE_R), sd_ellipse(u, v, LOBE_C[0], LOBE_C[1], *LOBE_R), k_lobes)
    return smin(sd_triangle(u, v), lobes, 0.06)                          # fillets where the triangle meets the lobes

def face_h(u, v):
    bumps = []
    for cu in (-LOBE_C[0], LOBE_C[0]):
        rho = math.sqrt(((u - cu) / BUMP_R[0]) ** 2 + ((v - LOBE_C[1]) / BUMP_R[1]) ** 2)
        bumps.append(LOBE_DOME * (0.5 + 0.5 * math.cos(math.pi * min(1.0, rho))))   # smooth dome, no crease at its foot
    return SLAB_H + smax(bumps[0], bumps[1], 0.05)                       # the two domes merge with a shallow crease between them

def build_face_patch(spokes=256, rings=28, ink_rings=None, back=0.12):
    if ink_rings is None: ink_rings = 3 if STROKE > 0 else 0
    """Closed patch on the head: white inside the outline (sd < 0), a painted ink band out to sd = STROKE, and a painted
    seam down the crease. No Solidify shell: the outline follows the head exactly from every angle."""
    R0, R1 = [], []
    for i in range(spokes):
        th = 2 * math.pi * i / spokes; cu, cv = math.cos(th), math.sin(th)
        def solve(level):
            lo, hi = 0.0, 0.8
            for _ in range(30):
                mid = (lo + hi) / 2
                if face_sd(cu * mid, cv * mid) < level: lo = mid
                else: hi = mid
            return (lo + hi) / 2
        R0.append(solve(0.0)); R1.append(solve(STROKE) if STROKE > 0 else R0[-1])
    def is_seam(u, v):
        if not (-0.29 < v < -0.045): return False
        t = (-0.045 - v) / (0.29 - 0.045)
        return abs(u) < SEAM_W[0] + (SEAM_W[1] - SEAM_W[0]) * t
    bm = bmesh.new()
    centre = bm.verts.new(face_point(0, 0, face_h(0, 0)))
    ringv, ringuv = [], []
    for r in range(1, rings + ink_rings + 1):
        row, rowuv = [], []
        for i in range(spokes):
            th = 2 * math.pi * i / spokes
            rad = R0[i] * r / rings if r <= rings else R0[i] + (R1[i] - R0[i]) * (r - rings) / ink_rings
            u, v = math.cos(th) * rad, math.sin(th) * rad
            row.append(bm.verts.new(face_point(u, v, face_h(u, v)))); rowuv.append((u, v))
        ringv.append(row); ringuv.append(rowuv)
    backv = [bm.verts.new(face_point(*ringuv[-1][i], -back)) for i in range(spokes)]
    backc = bm.verts.new(face_point(0, 0, -back))
    def paint(face, uvs, band):
        if band:
            face.material_index = 1
        else:
            cu = sum(q[0] for q in uvs) / len(uvs); cv = sum(q[1] for q in uvs) / len(uvs)
            if is_seam(cu, cv): face.material_index = 1
    for i in range(spokes):
        j = (i + 1) % spokes
        paint(bm.faces.new((centre, ringv[0][i], ringv[0][j])), [(0, 0), ringuv[0][i], ringuv[0][j]], False)
        for r in range(rings + ink_rings - 1):
            f = bm.faces.new((ringv[r][i], ringv[r + 1][i], ringv[r + 1][j], ringv[r][j]))
            paint(f, [ringuv[r][i], ringuv[r + 1][i], ringuv[r + 1][j], ringuv[r][j]], r + 1 >= rings)
        bm.faces.new((ringv[-1][i], backv[i], backv[j], ringv[-1][j])).material_index = 1
        bm.faces.new((backv[i], backc, backv[j])).material_index = 1
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm

head_part(new_object("FaceWhite", build_face_patch(), "White", outline=0, shade=0.7, extras=("Ink",)))

# nose: a thin dark wedge lying in the notch between the lobes (the seam below it is painted into the white mark)
p_n, n_n = head_surface(0, FACE_EL0)
head_part(ellipsoid("Nose", "Nose", p_n + n_n * (face_h(0, 0) + 0.022), (1, 1, 1), frame=basis(n_n), outline=0, shade=0.0,
                    deform=wedge(0.14, 0.03, 0.10, 0.036)))

# whiskers: three short thick brush strokes per cheek, nearly parallel, bowing slightly outward; no outline
for side, tag in ((+1, "L"), (-1, "R")):
    p, n = head_point(side * 43, -27)
    d = (Vector((side, 0, 0)) * math.cos(math.radians(30)) - Vector((0, 0, 1)) * math.sin(math.radians(30)) + 0.12 * n).normalized()
    for i, (dz, L) in enumerate(((0.12, 0.30), (0.0, 0.34), (-0.12, 0.34))):
        a = p - n * 0.02 + Vector((0, 0, dz)) - d * 0.05
        b = a + d * L
        m = (a + b) / 2 + n * 0.035
        head_part(tube(f"Whisker{tag}{i}", "Whisker", [a, m, b], [0.014, 0.026, 0.014], outline=0, res=12))


# ------------------------------------------------------------------ game features on the head (Unity toggles them by name)
BONE_OF = {}          # object name -> bone name (parts not listed: head parts -> Head, others by bone_for())
HIDDEN = []           # features hidden until Unity (or a preview shot) turns them on
def feature(o, bone="Head"):
    BONE_OF[o.name] = bone; return o
def hidden(o):
    HIDDEN.append(o); return o
for tag in ("L", "R"):
    for nm in ("Eye", "EyeRim", "Pupil"): BONE_OF[nm + tag] = "Eye" + tag
    for nm in ("Ear", "EarInner", "EarSpot", "EarTip"): BONE_OF[nm + tag] = "Ear" + tag

EYE_AZ, EYE_EL = 26.4, -0.75
def surf_pt(az0, el0, du, dv, h):
    """Point at surface offsets (du across, dv up) from (az0, el0), lifted h along the normal."""
    el = el0 + math.degrees(dv / V_PER_RAD)
    az = az0 + math.degrees(du / (U_PER_RAD * math.cos(math.radians(el))))
    p, n = head_surface(az, el)
    return p + n * h
def flat_ellipse(w, h, d):
    return lambda q, unit: Vector((unit.x * w / 2, unit.y * h / 2, unit.z * d / 2))

for side, tag in ((+1, "L"), (-1, "R")):
    az0 = side * EYE_AZ
    a = lambda du, dv: surf_pt(az0, EYE_EL, du, dv, 0.07)
    hidden(feature(head_part(tube(f"Happy{tag}", "Ink", [a(-0.26, -0.08), a(0.0, 0.11), a(0.26, -0.08)], [0.036, 0.036, 0.036], outline=0, res=12, shade=0.0)), "Eye" + tag))
    hidden(feature(head_part(tube(f"Shut{tag}", "Ink", [a(-0.25, 0.02), a(0.0, -0.05), a(0.25, 0.02)], [0.034, 0.034, 0.034], outline=0, res=12, shade=0.0)), "Eye" + tag))
    b = lambda du, dv: surf_pt(az0, EYE_EL, du, dv, 0.04)
    hidden(feature(head_part(tube(f"Brow{tag}", "Ink", [b(-0.20, 0.44), b(0.0, 0.49), b(0.20, 0.44)], [0.03, 0.034, 0.03], outline=0, res=12, shade=0.0)), "Brow" + tag))
    hidden(feature(head_part(head_cap(f"Blush{tag}", "Blush", side * 36, -18, flat_ellipse(0.32, 0.17, 0.02), base_h=0.008, shade=0.0))))
    hidden(feature(head_part(head_cap(f"Dirt{tag}", "Dirt", side * 34, -6, flat_ellipse(0.30, 0.20, 0.02), roll_deg=20 * side, base_h=0.008, shade=0.0))))
m = lambda du, dv, h=0.02: surf_pt(0, FACE_EL0, du, dv, h)
hidden(feature(head_part(tube("MouthFrown", "Ink", [m(-0.11, -0.40), m(0.0, -0.355), m(0.11, -0.40)], [0.016, 0.018, 0.016], outline=0, res=12, shade=0.0))))
hidden(feature(head_part(head_cap("MouthOpen", "Ink", 0, FACE_EL0 - 23.5, flat_ellipse(0.17, 0.12, 0.04), base_h=0.012, shade=0.0))))
hidden(feature(head_part(head_cap("MouthTongue", "Tongue", 0, FACE_EL0 - 25.5, flat_ellipse(0.10, 0.075, 0.03), base_h=0.03, shade=0.0))))
hidden(feature(head_part(head_cap("Tear", "Tear", EYE_AZ + 4, EYE_EL - 26, flat_ellipse(0.10, 0.17, 0.05), base_h=0.02, shade=0.0))))
hidden(feature(head_part(head_cap("Sweat", "Tear", EYE_AZ + 20, EYE_EL + 16, flat_ellipse(0.09, 0.15, 0.05), base_h=0.02, shade=0.0))))
hidden(feature(head_part(head_cap("Drool", "Tear", 7, FACE_EL0 - 27, flat_ellipse(0.07, 0.13, 0.04), base_h=0.03, shade=0.0))))
hc = HC + Vector((1.55, -0.35, 1.0))                # the heart floats beside the ear, not behind it
# One mesh. It used to be two lobe spheres plus a cone tip; Unity draws the outline per mesh, so each part's
# dark hull showed through its neighbours as a cut across the heart. A single pillow has one hull and no seams:
# the classic heart curve swept toward its centre, the depth following a flattened dome.
def heart_mesh(width=0.56, depth=0.19, spokes=72, rings=14, relax=7):
    bm = bmesh.new()
    def curve(t):
        x = 16 * math.sin(t) ** 3
        z = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        return x / 34.0 * width, (z + 2.5) / 34.0 * width       # centred, about `width` wide and as tall
    # The curve has a cusp at the cleft and at the tip, and its parameter crowds points there. Resample it evenly
    # by arc length, half a step off so no vertex lands on a cusp, then relax it: tip and cleft come out rounded
    # (a sharp cleft makes the outline hull fold over itself and leaves a dark V inside the heart).
    dense = [curve(i / 2400 * 2 * math.pi) for i in range(2400)]
    lengths = [0.0]
    for i in range(1, len(dense) + 1):
        a, b = dense[i - 1], dense[i % len(dense)]
        lengths.append(lengths[-1] + math.hypot(b[0] - a[0], b[1] - a[1]))
    outline, j = [], 0
    for i in range(spokes):
        target = (i + 0.5) / spokes * lengths[-1]
        while lengths[j + 1] < target: j += 1
        f = (target - lengths[j]) / max(1e-9, lengths[j + 1] - lengths[j])
        a, b = dense[j], dense[(j + 1) % len(dense)]
        outline.append((a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f))
    for _ in range(relax):
        outline = [(0.25 * outline[i - 1][0] + 0.5 * outline[i][0] + 0.25 * outline[(i + 1) % spokes][0],
                    0.25 * outline[i - 1][1] + 0.5 * outline[i][1] + 0.25 * outline[(i + 1) % spokes][1]) for i in range(spokes)]
    centre = (0.0, 0.035 * width)                               # every point of the curve is visible from here
    def ring(side, k):
        s_ = math.sin(k / rings * math.pi / 2)                   # 0 at the pole .. 1 on the rim; dense near the rim
        dome = math.sqrt(max(0.0, 1.0 - s_ ** 2.6))              # flatter than a sphere, so it reads as a pillow
        return [bm.verts.new(hc + Vector((centre[0] + (x - centre[0]) * s_, side * depth * 0.5 * dome, centre[1] + (z - centre[1]) * s_))) for x, z in outline]
    rim = ring(1, rings)
    for side in (-1, 1):
        pole = bm.verts.new(hc + Vector((centre[0], side * depth * 0.5, centre[1])))
        prev = None
        for k in range(1, rings + 1):
            cur = rim if k == rings else ring(side, k)
            for i in range(spokes):
                j = (i + 1) % spokes
                if prev is None: bm.faces.new((pole, cur[i], cur[j]) if side < 0 else (pole, cur[j], cur[i]))
                else: bm.faces.new((prev[i], cur[i], cur[j], prev[j]) if side < 0 else (prev[j], cur[j], cur[i], prev[i]))
            prev = cur
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return bm
hidden(feature(head_part(new_object("Heart", heart_mesh(), "HeartRed", OUTLINE, shade=0.0, even=False))))

# accessories (shown by Unity when equipped: SetAccessory("hat_beanie" | "scarf_star" | "bow_cherry" | "crown_tiny"))
def cap_ellipsoid(name, material, center, radii, zmin, seg=64, rings=32, outline=OUTLINE, shade=0.0):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < zmin], context='VERTS')
    for v in bm.verts: v.co = Vector((v.co.x * radii[0], v.co.y * radii[1], v.co.z * radii[2])) + Vector(center)
    return new_object(name, bm, material, outline, shade=shade)
def cone_obj(name, material, center, r1, r2, depth, outline=OUTLINE, shade=0.0, segments=24):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=segments, radius1=r1, radius2=r2, depth=depth)
    for v in bm.verts: v.co = v.co + Vector(center)
    return new_object(name, bm, material, outline, shade=shade)
hidden(feature(head_part(cap_ellipsoid("Acc_hat_beanie", "Beanie", HC + Vector((0, 0, 0.10)), (HR[0] * 1.06, HR[1] * 1.08, HR[2] * 1.10), 0.32))))
hidden(feature(head_part(ellipsoid("Acc_hat_beanie_pom", "Pom", HC + Vector((0, 0, HR[2] * 1.10 + 0.22)), (0.22, 0.22, 0.22), shade=0.0))))
bcen, bnor = head_surface(52, 26); BF = basis(bnor)   # bow sits on the side of the head, clear of the ear
for dx, nm in ((-0.16, "l"), (0.16, "r")):
    hidden(feature(head_part(ellipsoid(f"Acc_bow_cherry_{nm}", "Bow", bcen + BF @ Vector((dx, 0.02, 0.06)), (0.21, 0.145, 0.10), frame=BF, shade=0.0))))
hidden(feature(head_part(ellipsoid("Acc_bow_cherry_knot", "Bow", bcen + bnor * 0.10, (0.09, 0.09, 0.09), shade=0.0))))
_top = HC + Vector((0, 0.05, HR[2] + 0.06))
hidden(feature(head_part(cone_obj("Acc_crown_tiny", "Crown", _top + Vector((0, 0, 0.11)), 0.36, 0.36, 0.22))))
for i in range(5):
    _a = math.radians(90 + i * 72)
    hidden(feature(head_part(cone_obj(f"Acc_crown_tiny_p{i}", "Crown", _top + Vector((math.cos(_a) * 0.30, math.sin(_a) * 0.30, 0.33)), 0.09, 0.004, 0.22, segments=12))))

for o in HEAD_PARTS:
    o.parent = head_pivot
    o.matrix_parent_inverse = Matrix.Translation(NECK).inverted()   # matrix_world is not evaluated yet

# torso, bib, feet, tail (standing chibi, like the painting)
body = ellipsoid("Body", "Fur", BC, BR, outline=OUTLINE_BIG)
bib = ellipsoid("ChestBib", "White", BC + Vector((0, -0.20, -0.04)), (0.72, 0.68, 0.66), shade=0.9)   # belly white up to the armpits
# back stripes: three soft darker bands across the upper back, drooping toward the sides (painted on the torso surface)
body_shape = lambda q, unit: Vector((unit.x * BR[0], unit.y * BR[2], unit.z * BR[1]))
back_frame = basis(Vector((0, 1, 0)))
for k, y0 in enumerate((0.34, 0.10, -0.14)):
    def stripe_sd(x, y, y0=y0):
        return smax(abs(y - (y0 - 0.35 * x * x)) - 0.045, abs(x) - 0.55, 0.06)
    surface_patch(f"BackStripe{k}", "Stripe", back_frame, BC, body_shape, stripe_sd, (0.0, y0), lift=0.006, spokes=96, rings=4, reach=0.8)
for side, tag in ((+1, "L"), (-1, "R")):
    fc = Vector((side * 0.40, -0.28, 0.27))
    ellipsoid(f"Foot{tag}", "White", fc, (0.27, 0.27, 0.27), shade=0.8, outline=OUTLINE_LIMB)
    pd = Vector((0, -0.60, -0.80)).normalized()                                # grey soles turned to the viewer
    ellipsoid(f"Pad{tag}", "Pad", fc + pd * 0.255, (0.16, 0.14, 0.04), frame=basis(pd), outline=0, shade=0.3)
TAIL_PTS = [(0.55, 0.40, 0.40), (1.05, 0.46, 0.36), (1.28, 0.26, 0.66), (1.08, 0.08, 0.92)]
tube("Tail", "Fur", TAIL_PTS, [0.16, 0.16, 0.14, 0.10], outline=OUTLINE_BIG)

def capsule(name, material, start, end, r, outline=OUTLINE, shade=1.0):
    """A straight cylinder with hemispherical ends: the sphere halves pushed apart along local Z."""
    start, end = Vector(start), Vector(end)
    L = (end - start).length
    def f(q, unit):
        return Vector((unit.x * r, unit.y * r, unit.z * r + (L / 2 if unit.z >= 0 else -L / 2)))
    return ellipsoid(name, material, (start + end) / 2, (1, 1, 1), frame=basis(end - start), deform=f, outline=outline, shade=shade)

# arms: short straight dark capsules rooted in the upper torso, only the paw tip dipped in white.
# Two sets (hanging / one arm raised for the reference-pose shot), toggled per shot.
ARM_SETS = {"neutral": [], "hug": []}
def arm(setname, side, top, bottom):
    tag = "L" if side > 0 else "R"
    r = 0.20
    leg = capsule(f"Arm{tag}_{setname}", "Fur", top, bottom, r, outline=OUTLINE_LIMB)
    axis = (Vector(top) - Vector(bottom)).normalized()
    tip = capsule(f"PawTip{tag}_{setname}", "White", Vector(bottom) + axis * 0.05, bottom, r + 0.015, shade=0.8, outline=OUTLINE_LIMB)
    ARM_SETS[setname] += [leg, tip]
arm("neutral", -1, (-0.84, -0.22, 1.24), (-0.92, -0.32, 0.50))
arm("neutral", +1, (0.84, -0.22, 1.24), (0.92, -0.32, 0.50))
arm("hug", -1, (-0.80, -0.25, 1.24), (-0.50, -0.95, 1.62))
arm("hug", +1, (0.84, -0.22, 1.24), (0.92, -0.32, 0.50))

def use_arm_set(name):
    for k, objs in ARM_SETS.items():
        for o in objs:
            o.hide_render = o.hide_viewport = (k != name)

# ------------------------------------------------------------------ game features on the body, ground shadow, scarf
_n3 = Vector((-0.35, -0.80, 0.30)).normalized()
hidden(feature(ellipsoid("Dirt3", "Dirt", BC + Vector((_n3.x * BR[0], _n3.y * BR[1], _n3.z * BR[2])), (0.17, 0.12, 0.02), frame=basis(_n3), outline=0, shade=0.0), "Body"))
def torus(name, material, center, R, r, scale=(1, 1, 1), seg=48, rings=16, outline=OUTLINE, shade=0.0):
    bm = bmesh.new(); grid = []
    for i in range(seg):
        a = 2 * math.pi * i / seg; row = []
        for j in range(rings):
            b = 2 * math.pi * j / rings
            x = (R + r * math.cos(b)) * math.cos(a); y = (R + r * math.cos(b)) * math.sin(a); z = r * math.sin(b)
            row.append(bm.verts.new(Vector((x * scale[0], y * scale[1], z * scale[2])) + Vector(center)))
        grid.append(row)
    for i in range(seg):
        for j in range(rings):
            bm.faces.new((grid[i][j], grid[(i + 1) % seg][j], grid[(i + 1) % seg][(j + 1) % rings], grid[i][(j + 1) % rings]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return new_object(name, bm, material, outline, shade=shade)
hidden(feature(torus("Acc_scarf_star", "Scarf", (0, 0.02, 1.32), 0.86, 0.17, (1.0, 0.9, 0.62)), "Body"))
hidden(feature(ellipsoid("Acc_scarf_star_tail", "Scarf", (0.46, -0.72, 1.0), (0.14, 0.10, 0.24), shade=0.0), "Body"))
# ground shadow: a flat disc tinted by mood in Unity (its shader fades by UV distance from the centre)
_bm = bmesh.new(); bmesh.ops.create_circle(_bm, cap_ends=True, segments=48, radius=1.15)
_uv = _bm.loops.layers.uv.new("UVMap")
for v in _bm.verts: v.co = Vector((v.co.x + 0.05, v.co.y * 0.6 + 0.05, 0.005))
for f in _bm.faces:
    for l in f.loops: l[_uv].uv = ((l.vert.co.x - 0.05) / 2.3 + 0.5, (l.vert.co.y - 0.05) / (2.3 * 0.6) + 0.5)
shadow = new_object("Shadow", _bm, "Shadow", 0, smooth=False, shade=0.0); BONE_OF["Shadow"] = None
shadow.hide_render = True   # the Eevee previews use the flat background; Unity draws the shadow itself

def bone_for(o):
    n = o.name
    if n in BONE_OF: return BONE_OF[n]
    if o in HEAD_PARTS: return "Head"
    if n.startswith("Arm") or n.startswith("Paw"): return "Arm" + ("L" if ("L_" in n or n.endswith("L")) else "R")
    if n.startswith("Foot") or n.startswith("Pad"): return "Leg" + n[-1]
    if n == "Tail": return "Tail"
    return "Body"

# ------------------------------------------------------------------ cameras, previews, comparison sheet
cam_data = bpy.data.cameras.new("Cam"); cam_data.lens_unit = 'FOV'; cam_data.angle = math.radians(27)
cam = bpy.data.objects.new("Cam", cam_data); scene.collection.objects.link(cam); scene.camera = cam
def aim(pos, target):
    cam.location = Vector(pos)
    cam.rotation_euler = (Vector(target) - Vector(pos)).to_track_quat('-Z', 'Y').to_euler()

def pose(pitch=0, yaw=0, arms="neutral", roll=0):
    head_pivot.rotation_euler = Euler((math.radians(pitch), math.radians(roll), math.radians(yaw)), 'XYZ')
    use_arm_set(arms)

def render(name):
    scene.render.filepath = os.path.join(PREVIEW_DIR, name + ".png")
    bpy.ops.render.render(write_still=True)
    return scene.render.filepath

def compare_sheet(render_path, out_path):
    """Render on the left, the reference cat (cropped from the painting) on the right, same height."""
    import numpy as np
    def load(path):
        im = bpy.data.images.load(path); w, h = im.size
        a = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]
        bpy.data.images.remove(im); return a
    ren = load(render_path)
    ref = load(REF_PATH)[268:548, 118:338]            # left cat + a slice of the fish, rows from the top
    H = ren.shape[0]
    scale = H / ref.shape[0]
    idx_y = np.minimum((np.arange(H) / scale).astype(int), ref.shape[0] - 1)
    idx_x = np.minimum((np.arange(int(ref.shape[1] * scale)) / scale).astype(int), ref.shape[1] - 1)
    ref_s = ref[idx_y][:, idx_x]
    sheet = np.concatenate([ren, ref_s], axis=1)
    sheet[:, :, 3] = 1.0
    out = bpy.data.images.new("compare", sheet.shape[1], sheet.shape[0], alpha=True)
    out.pixels = sheet[::-1].ravel().tolist()
    out.filepath_raw = out_path; out.file_format = 'PNG'; out.save()
    bpy.data.images.remove(out)

def show(names=()):
    for o in HIDDEN: o.hide_render = o.name not in names

if PREVIEW_DIR:
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    show()
    pose(); aim((0, -11.0, 2.3), (0, 0, 1.78)); render("front")
    pose(); aim((7.0, -8.6, 2.6), (0, 0, 1.78)); render("three-quarter")
    pose(); aim((11.0, 0, 2.3), (0, 0, 1.78)); render("side")
    pose(); aim((6.5, 9.0, 2.6), (0, 0, 1.78)); render("back")
    pose(); aim((0, -11.0, 2.3), (0, 0, 1.78))
    def face_shot(name, names, hide_eyes=False):
        show(names)
        for tag in ("L", "R"):
            for nm in ("Eye", "EyeRim", "Pupil"): bpy.data.objects[nm + tag].hide_render = hide_eyes
        render(name)
        for tag in ("L", "R"):
            for nm in ("Eye", "EyeRim", "Pupil"): bpy.data.objects[nm + tag].hide_render = False
    face_shot("happy-face", ("HappyL", "HappyR", "BlushL", "BlushR"), hide_eyes=True)
    face_shot("sad-face", ("MouthFrown", "BrowL", "BrowR", "Tear"))
    face_shot("surprised", ("MouthOpen", "MouthTongue", "Sweat"))
    face_shot("sleep-face", ("ShutL", "ShutR"), hide_eyes=True)
    face_shot("love", ("BlushL", "BlushR", "Heart"))              # eyes stay open: the arcs are only for petting
    face_shot("dirty", ("DirtL", "DirtR", "Dirt3", "Drool"))
    face_shot("beanie", ("Acc_hat_beanie", "Acc_hat_beanie_pom"))
    face_shot("scarf", ("Acc_scarf_star", "Acc_scarf_star_tail"))
    face_shot("bow", ("Acc_bow_cherry_l", "Acc_bow_cherry_r", "Acc_bow_cherry_knot"))
    face_shot("crown", tuple(o.name for o in HIDDEN if o.name.startswith("Acc_crown")))
    show()
    pose(pitch=-22, yaw=-18, roll=25, arms="hug"); aim((-3.6, -10.4, 2.3), (0.05, 0, 1.85))
    shot = render("reference-pose")
    compare_sheet(shot, os.path.join(PREVIEW_DIR, "compare.png"))
    pose(); aim((7.0, -8.6, 2.6), (0, 0, 1.78))

pose()
for o in ARM_SETS["hug"]: o.hide_viewport = True
for o in HIDDEN: o.hide_viewport = True; o.hide_render = True

if BLEND_PATH:
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type != 'VIEW_3D': continue
            sp = area.spaces.active
            sp.shading.type = 'MATERIAL'
            sp.region_3d.view_perspective = 'CAMERA'
    os.makedirs(os.path.dirname(os.path.abspath(BLEND_PATH)), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(BLEND_PATH))
    print("[build_cat2] saved", BLEND_PATH)


# ------------------------------------------------------------------ game export: rig, clips, plain materials, FBX
if FBX_PATH or ANIM_DIR:
    # pose-only arm set goes; the neutral arms get their game names
    for o in ARM_SETS["hug"]:
        OBJECTS.remove(o)
        if o in HIDDEN: HIDDEN.remove(o)
        bpy.data.objects.remove(o, do_unlink=True)
    for o in ARM_SETS["neutral"]:
        o.name = o.name.replace("_neutral", "").replace("PawTip", "Paw"); o.data.name = o.name
    # armature
    arm_data = bpy.data.armatures.new("CatRig")
    rig = bpy.data.objects.new("Armature", arm_data); scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    def bone(name, head, tail, parent=None):
        b = arm_data.edit_bones.new(name)
        b.head = Vector(head); b.tail = Vector(tail); b.roll = 0.0
        if parent: b.parent = arm_data.edit_bones[parent]
        return b
    bone("Root", (0, 0, 0), (0, 0, 0.35))
    bone("Body", (0, 0, 0.0), (0, 0, 1.20), "Root")      # head on the ground: squash/stretch scales toward the floor, never through it
    bone("Head", (0, 0, 1.40), (0, 0, 2.90), "Body")
    for side, tag in ((+1, "L"), (-1, "R")):
        eo, eu = EAR_RIG[tag]; bone("Ear" + tag, eo, eo + eu * 0.9, "Head")
        ec, en = head_surface(side * EYE_AZ, EYE_EL); bone("Eye" + tag, ec, ec + Vector((0, 0, 0.25)), "Head")
        bc_, bn_ = head_surface(side * EYE_AZ, EYE_EL + 26); bone("Brow" + tag, bc_ + bn_ * 0.03, bc_ + bn_ * 0.03 + Vector((0, 0, 0.15)), "Head")
        bone("Arm" + tag, (side * 0.84, -0.22, 1.24), (side * 0.84, -0.22, 1.49), "Body")
        bone("Leg" + tag, (side * 0.40, -0.28, 0.45), (side * 0.40, -0.28, 0.70), "Body")
    TP = [Vector(q) for q in TAIL_PTS]
    bone("Tail1", TP[0], (TP[0] + TP[1]) / 2, "Body"); bone("Tail2", (TP[0] + TP[1]) / 2, (TP[1] + TP[2]) / 2, "Tail1"); bone("Tail3", (TP[1] + TP[2]) / 2, TP[3], "Tail2")
    bpy.ops.object.mode_set(mode='OBJECT')

    # skinning: rigid, one group per part; the tail blends across its three bones
    def tail_param(co):
        best = (1e9, 0.0); acc = 0.0; total = sum((TP[i + 1] - TP[i]).length for i in range(3))
        for i in range(3):
            a, b = TP[i], TP[i + 1]; ab = b - a; L = ab.length
            t = max(0.0, min(1.0, (co - a).dot(ab) / (L * L)))
            d = (a + ab * t - co).length
            if d < best[0]: best = (d, (acc + t * L) / total)
            acc += L
        return best[1]
    def tail_weights(co):
        t = tail_param(co); centres = (0.15, 0.5, 0.85)
        w = [max(0.0, 1.0 - abs(t - c) / 0.45) for c in centres]; sm = sum(w) or 1.0
        return [x / sm for x in w]
    for o in OBJECTS:
        o.parent = rig; o.matrix_parent_inverse = Matrix.Identity(4)
        b = bone_for(o)
        if b is None: continue
        mod = o.modifiers.new("Armature", 'ARMATURE'); mod.object = rig
        if b == "Tail":
            groups = [o.vertex_groups.new(name="Tail%d" % i) for i in (1, 2, 3)]
            for v in o.data.vertices:
                for g, w in zip(groups, tail_weights(v.co)):
                    if w > 0.001: g.add([v.index], w, 'REPLACE')
        else:
            o.vertex_groups.new(name=b).add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')
    bpy.data.objects.remove(head_pivot, do_unlink=True)

    # animation: the game's loops and one-shots (bone deltas in world axes, converted into each bone's rest frame)
    BONES = ["Root", "Body", "Head", "EarL", "EarR", "EyeL", "EyeR", "BrowL", "BrowR", "ArmL", "ArmR", "LegL", "LegR", "Tail1", "Tail2", "Tail3"]
    rig.animation_data_create()
    for pb in rig.pose.bones: pb.rotation_mode = 'XYZ'
    REST = {pb.name: pb.bone.matrix_local.to_3x3() for pb in rig.pose.bones}
    def key(frame, name, loc=None, rot=None, scale=None):
        pb = rig.pose.bones[name]; R = REST[name]; Ri = R.inverted()
        if loc is not None:
            pb.location = Ri @ Vector(loc); pb.keyframe_insert("location", frame=frame)
        if rot is not None:
            W = Euler([math.radians(a) for a in rot], 'XYZ').to_matrix()
            pb.rotation_euler = (Ri @ W @ R).to_euler('XYZ'); pb.keyframe_insert("rotation_euler", frame=frame)
        if scale is not None:
            sx, sy, sz = scale; loc_scale = []
            for i in range(3):
                axis = R.col[i]; a = max(range(3), key=lambda k: abs(axis[k])); loc_scale.append((sx, sy, sz)[a])
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
            slot = act.slots.new(id_type='OBJECT', name="Armature"); rig.animation_data.action_slot = slot
        for pb in rig.pose.bones:
            pb.location = (0, 0, 0); pb.rotation_euler = (0, 0, 0); pb.scale = (1, 1, 1)
        rest_all(0)
        for f, b, *rest in keys:
            key(f, b, rest[0] if len(rest) > 0 else None, rest[1] if len(rest) > 1 else None, rest[2] if len(rest) > 2 else None)
        rest_all(length)
        if loop:
            for f, b, *rest in keys:
                if f == 0: key(length, b, *(rest + [None] * (3 - len(rest))))
        act.use_frame_range = True; act.frame_start = 0; act.frame_end = length
        for fc in action_fcurves(act):
            for kp in fc.keyframe_points:
                kp.interpolation = 'BEZIER'; kp.easing = 'AUTO'
                if loop: kp.handle_left_type = kp.handle_right_type = 'AUTO_CLAMPED'
        act.use_fake_user = True
        return act
    def rotk(f, b, x=0, y=0, z=0): return (f, b, None, (x, y, z))
    def lock(f, b, x=0, y=0, z=0): return (f, b, (x, y, z))
    def sck(f, b, x=1, y=1, z=1): return (f, b, None, None, (x, y, z))

    # ---------------------------------------------------------------- animation, authored for THIS chibi body
    # The previous set was ported from the taller model and read as tiny wobbles here. Principles:
    #   * the Body bone's squash/stretch is the main tool — a chibi reads shape change, not limb detail, and
    #     Blender's bones inherit scale, so squashing Body squashes the whole cat (the classic cartoon look);
    #   * the head is ~45% of the silhouette: it leads every action and lags coming out (anticipate, overshoot);
    #   * ears and tail are the big thin shapes — they get 20-50 deg, not 5;
    #   * arms and legs are stubs: they ride the body and only swing wide when the pose must read;
    #   * one-shots are additive in Unity, so nothing is keyed at frame 0 and every clip returns to rest;
    #   * loops key frame 0 (the helper copies those keys onto the last frame, so they close seamlessly).
    # Axes for key(): X = pitch (+ = nose/ear down), Y = roll, Z = yaw (+ turns the face toward +X = the cat's left).
    # L parts sit at +X on this rig. A world-Y roll moves an UP-pointing part (ear) and a DOWN-hanging part (arm) in
    # opposite directions: EarL splays outward with a POSITIVE Y, ArmL swings outward with a NEGATIVE Y (mirror for R).

    # ---- loops
    clip("Idle", 120, [
        sck(0, "Body", 1, 1, 1), sck(40, "Body", 0.97, 0.97, 1.05), sck(80, "Body", 1.045, 1.045, 0.955),
        lock(40, "Root", 0, 0, 0.05), lock(80, "Root", 0, 0, -0.02),
        rotk(0, "Head", 0, 3, 0), rotk(30, "Head", -3, 5, -5), rotk(60, "Head", 2, 0, 0), rotk(90, "Head", 0, -5, 6),
        lock(40, "Head", 0, 0, 0.05), lock(80, "Head", 0, 0, -0.05),
        rotk(0, "Tail1", 0, 0, -16), rotk(40, "Tail1", 0, 0, 18), rotk(80, "Tail1", 0, 0, -8),
        rotk(0, "Tail2", 0, 0, -20), rotk(46, "Tail2", 0, 0, 24), rotk(86, "Tail2", 0, 0, -10),
        rotk(0, "Tail3", 0, 0, -24), rotk(52, "Tail3", 0, 0, 28), rotk(92, "Tail3", 0, 0, -12),
        rotk(30, "EarL"), rotk(34, "EarL", -6, 26, 0), rotk(40, "EarL", 0, -4, 0), rotk(45, "EarL"),
        rotk(88, "EarR"), rotk(92, "EarR", -6, -26, 0), rotk(98, "EarR", 0, 4, 0), rotk(103, "EarR"),
    ], loop=True)

    # Happy is the everyday loop of a well-kept pet, so it has to stay pleasant for minutes: a planted bounce
    # (Body squash/stretch about the ground pivot — the feet never leave the floor), head sway, a wide tail wag
    # and ear flicks. The airborne jumps live in the Hop and Celebrate one-shots, which answer an event.
    # (The first version jumped 0.46 twice per loop; with Love as the usual mood the cat never stopped hopping.)
    clip("Happy", 64, [
        sck(0, "Body", 1.05, 1.05, 0.94), sck(16, "Body", 0.965, 0.965, 1.06), sck(32, "Body", 1.05, 1.05, 0.94), sck(48, "Body", 0.965, 0.965, 1.06),
        rotk(0, "Head", 4, -7, 0), rotk(16, "Head", -6, 0, 3), rotk(32, "Head", 4, 7, 0), rotk(48, "Head", -6, 0, -3),
        lock(0, "Head", 0, 0, -0.02), lock(16, "Head", 0, 0, 0.04), lock(32, "Head", 0, 0, -0.02), lock(48, "Head", 0, 0, 0.04),
        rotk(0, "EarL", 6, 0, 0), rotk(16, "EarL", -12, 8, 0), rotk(32, "EarL", 6, 0, 0), rotk(48, "EarL", -12, 8, 0),
        rotk(0, "EarR", 6, 0, 0), rotk(16, "EarR", -12, -8, 0), rotk(32, "EarR", 6, 0, 0), rotk(48, "EarR", -12, -8, 0),
        rotk(0, "Tail1", 0, 0, -32), rotk(16, "Tail1", 0, 0, 32), rotk(32, "Tail1", 0, 0, -32), rotk(48, "Tail1", 0, 0, 32),
        rotk(0, "Tail2", 0, 0, -30), rotk(19, "Tail2", 0, 0, 30), rotk(35, "Tail2", 0, 0, -30), rotk(51, "Tail2", 0, 0, 30),
        rotk(0, "Tail3", 0, 0, -28), rotk(22, "Tail3", 0, 0, 28), rotk(38, "Tail3", 0, 0, -28), rotk(54, "Tail3", 0, 0, 28),
        rotk(16, "ArmL", 0, -20, 0), rotk(32, "ArmL"), rotk(48, "ArmL", 0, -20, 0),
        rotk(16, "ArmR", 0, 20, 0), rotk(32, "ArmR"), rotk(48, "ArmR", 0, 20, 0),
    ], loop=True)

    clip("Sad", 150, [
        rotk(0, "Head", 13, 3, 0), rotk(75, "Head", 15, 2, 0),
        lock(0, "Head", 0, 0, -0.05),
        rotk(0, "EarL", 16, 30, 0), rotk(75, "EarL", 18, 28, 0),
        rotk(0, "EarR", 16, -30, 0), rotk(75, "EarR", 18, -28, 0),
        sck(0, "Body", 1.05, 1.05, 0.92), sck(75, "Body", 1.02, 1.02, 0.95),
        lock(0, "Root", 0, 0, -0.02),
        rotk(0, "Tail1", 0, 12, -24), rotk(75, "Tail1", 0, 12, -20),
        rotk(0, "Tail2", 0, 10, -32), rotk(75, "Tail2", 0, 10, -28),
        rotk(0, "Tail3", 0, 8, -40), rotk(75, "Tail3", 0, 8, -36),
        rotk(0, "ArmL", 0, -12, 0), rotk(0, "ArmR", 0, 12, 0),
    ], loop=True)

    clip("Sleep", 180, [
        rotk(0, "Head", 15, 9, 0), rotk(90, "Head", 17, 8, 0),
        lock(0, "Head", 0, 0, -0.08),
        rotk(0, "EarL", 14, 28, 0), rotk(0, "EarR", 14, -28, 0),
        sck(0, "Body", 1.07, 1.07, 0.91), sck(90, "Body", 0.98, 0.98, 1.06),
        lock(0, "Root", 0, 0, 0.0), lock(90, "Root", 0, 0, -0.02),
        rotk(0, "Tail1", 0, 10, 30), rotk(90, "Tail1", 0, 10, 34),
        rotk(0, "Tail2", 0, 8, 40), rotk(90, "Tail2", 0, 8, 44),
        rotk(0, "Tail3", 0, 6, 48), rotk(90, "Tail3", 0, 6, 52),
        rotk(0, "ArmL", 0, -10, 0), rotk(0, "ArmR", 0, 10, 0),
    ], loop=True)

    clip("Alert", 72, [
        rotk(0, "EarL", -32, 4, 0), rotk(0, "EarR", -32, -4, 0),
        rotk(0, "Head", -13, 0, 0), rotk(18, "Head", -11, 0, -18), rotk(36, "Head", -13, 0, 0), rotk(54, "Head", -11, 0, 18),
        sck(0, "Body", 0.95, 0.95, 1.11),
        lock(0, "Root", 0, 0, 0.07),
        rotk(0, "Tail1", 0, -34, 0), rotk(20, "Tail1", 0, -32, 14), rotk(48, "Tail1", 0, -32, -14),
        rotk(0, "Tail2", 0, -24, 0), rotk(24, "Tail2", 0, -22, 18), rotk(52, "Tail2", 0, -22, -18),
        rotk(0, "Tail3", 0, -14, 0), rotk(28, "Tail3", 0, -12, 22), rotk(56, "Tail3", 0, -12, -22),
    ], loop=True)

    clip("Walk", 24, [
        lock(0, "Root", 0.05, 0, 0.06), lock(6, "Root", 0, 0, 0.13), lock(12, "Root", -0.05, 0, 0.06), lock(18, "Root", 0, 0, 0.13),
        rotk(0, "Body", 0, 11, 5), rotk(12, "Body", 0, -11, -5),
        rotk(0, "LegL", 34, 0, 0), rotk(12, "LegL", -34, 0, 0),
        rotk(0, "LegR", -34, 0, 0), rotk(12, "LegR", 34, 0, 0),
        rotk(0, "ArmL", -28, -6, 0), rotk(12, "ArmL", 28, -6, 0),
        rotk(0, "ArmR", 28, 6, 0), rotk(12, "ArmR", -28, 6, 0),
        rotk(0, "Head", -4, -7, 0), rotk(6, "Head", 3, 0, 0), rotk(12, "Head", -4, 7, 0), rotk(18, "Head", 3, 0, 0),
        rotk(0, "EarL", 10, 0, 0), rotk(6, "EarL", -8, 0, 0), rotk(12, "EarL", 10, 0, 0), rotk(18, "EarL", -8, 0, 0),
        rotk(0, "EarR", 10, 0, 0), rotk(6, "EarR", -8, 0, 0), rotk(12, "EarR", 10, 0, 0), rotk(18, "EarR", -8, 0, 0),
        rotk(0, "Tail1", 0, -10, 26), rotk(12, "Tail1", 0, -10, -26),
        rotk(3, "Tail2", 0, -8, 30), rotk(15, "Tail2", 0, -8, -30),
        rotk(6, "Tail3", 0, -6, 32), rotk(18, "Tail3", 0, -6, -32),
        sck(0, "Body", 1.02, 1.02, 0.98), sck(6, "Body", 0.99, 0.99, 1.03), sck(12, "Body", 1.02, 1.02, 0.98), sck(18, "Body", 0.99, 0.99, 1.03),
    ], loop=True)

    clip("Fainted", 60, [
        rotk(0, "Root", 0, 88, 0), lock(0, "Root", -1.6, 0, 1.32),
        sck(0, "Body", 1.04, 1.04, 0.95), sck(30, "Body", 0.99, 0.99, 1.04),
        rotk(0, "Head", 12, 0, 0), rotk(30, "Head", 14, 0, 3),
        rotk(0, "ArmL", 0, 72, 0), rotk(0, "ArmR", 0, 72, 0),
        rotk(0, "LegL", -48, 0, 0), rotk(0, "LegR", -48, 0, 0),
        rotk(0, "EarL", 8, -38, 0), rotk(0, "EarR", 8, -38, 0),
        rotk(0, "Tail1", 0, 0, 30), rotk(30, "Tail1", 0, 0, 24),
        rotk(0, "Tail2", 0, 0, 34), rotk(30, "Tail2", 0, 0, 28),
    ], loop=True)

    # ---- one-shots (additive: never keyed at frame 0, always back to rest)
    clip("Hop", 24, [
        lock(2, "Root", 0, 0, -0.04), lock(9, "Root", 0, 0, 0.92), lock(15, "Root", 0, 0, 0.0), lock(19, "Root", 0, 0, 0.06),
        sck(2, "Body", 1.18, 1.18, 0.80), sck(9, "Body", 0.86, 0.86, 1.22), sck(15, "Body", 1.15, 1.15, 0.83), sck(19, "Body", 0.98, 0.98, 1.04),
        rotk(2, "Head", 12, 0, 0), rotk(9, "Head", -18, 0, 0), rotk(15, "Head", 12, 0, 0), rotk(19, "Head", -5, 0, 0),
        rotk(2, "EarL", 18, 8, 0), rotk(9, "EarL", -30, 14, 0), rotk(15, "EarL", 20, 6, 0),
        rotk(2, "EarR", 18, -8, 0), rotk(9, "EarR", -30, -14, 0), rotk(15, "EarR", 20, -6, 0),
        rotk(9, "ArmL", 0, -46, 0), rotk(9, "ArmR", 0, 46, 0),
        rotk(6, "Tail1", 0, -24, 0), rotk(13, "Tail1", 0, 18, 0),
        rotk(8, "Tail2", 0, -28, 0), rotk(15, "Tail2", 0, 20, 0),
    ])

    clip("Wiggle", 28, [
        rotk(4, "Body", 0, 6, 22), rotk(10, "Body", 0, -6, -22), rotk(16, "Body", 0, 4, 16), rotk(21, "Body", 0, -3, -10), rotk(25, "Body", 0, 0, 5),
        rotk(5, "Head", 0, -10, -16), rotk(11, "Head", 0, 10, 16), rotk(17, "Head", 0, -7, -11), rotk(22, "Head", 0, 5, 7),
        rotk(5, "EarL", 0, 30, 0), rotk(11, "EarL", 0, -14, 0), rotk(17, "EarL", 0, 18, 0),
        rotk(5, "EarR", 0, -14, 0), rotk(11, "EarR", 0, 30, 0), rotk(17, "EarR", 0, -18, 0),
        rotk(6, "Tail1", 0, 0, -34), rotk(13, "Tail1", 0, 0, 34), rotk(20, "Tail1", 0, 0, -22),
        rotk(8, "Tail2", 0, 0, -38), rotk(15, "Tail2", 0, 0, 38), rotk(22, "Tail2", 0, 0, -24),
        sck(8, "Body", 1.05, 1.05, 0.96), sck(18, "Body", 1.05, 1.05, 0.96),
    ])

    clip("Pat", 22, [
        sck(4, "Head", 1.18, 1.18, 0.76), sck(11, "Head", 0.95, 0.95, 1.10), sck(17, "Head", 1.03, 1.03, 0.97),
        lock(4, "Head", 0, 0, -0.18), lock(11, "Head", 0, 0, 0.05),
        sck(4, "Body", 1.07, 1.07, 0.93), sck(11, "Body", 0.98, 0.98, 1.04),
        rotk(4, "EarL", 16, 34, 0), rotk(11, "EarL", -16, -8, 0), rotk(17, "EarL", 4, 6, 0),
        rotk(4, "EarR", 16, -34, 0), rotk(11, "EarR", -16, 8, 0), rotk(17, "EarR", 4, -6, 0),
        rotk(4, "Head", 8, 0, 0), rotk(11, "Head", -6, 0, 0),
        rotk(6, "Tail1", 0, 0, 24), rotk(14, "Tail1", 0, 0, -18),
    ])

    clip("WaveL", 38, [
        rotk(5, "ArmL", 0, -68, 0), rotk(11, "ArmL", 0, -112, 0), rotk(17, "ArmL", 0, -82, 0), rotk(23, "ArmL", 0, -112, 0), rotk(29, "ArmL", 0, -84, 0), rotk(34, "ArmL", 0, -36, 0),
        rotk(8, "Body", 0, 7, 0), rotk(26, "Body", 0, 7, 0), lock(8, "Root", 0, 0, 0.05), lock(26, "Root", 0, 0, 0.05),
        rotk(8, "Head", -8, -12, 6), rotk(20, "Head", -8, -14, 6), rotk(30, "Head", -5, -8, 4),
        rotk(8, "EarL", -16, 10, 0), rotk(26, "EarL", -16, 10, 0),
        rotk(8, "EarR", -12, -10, 0), rotk(26, "EarR", -12, -10, 0),
        rotk(10, "Tail1", 0, 0, 26), rotk(20, "Tail1", 0, 0, -22), rotk(30, "Tail1", 0, 0, 16),
        sck(11, "Body", 0.98, 0.98, 1.04), sck(29, "Body", 0.98, 0.98, 1.04),
    ])

    clip("WaveR", 38, [
        rotk(5, "ArmR", 0, 68, 0), rotk(11, "ArmR", 0, 112, 0), rotk(17, "ArmR", 0, 82, 0), rotk(23, "ArmR", 0, 112, 0), rotk(29, "ArmR", 0, 84, 0), rotk(34, "ArmR", 0, 36, 0),
        rotk(8, "Body", 0, -7, 0), rotk(26, "Body", 0, -7, 0), lock(8, "Root", 0, 0, 0.05), lock(26, "Root", 0, 0, 0.05),
        rotk(8, "Head", -8, 12, -6), rotk(20, "Head", -8, 14, -6), rotk(30, "Head", -5, 8, -4),
        rotk(8, "EarR", -16, -10, 0), rotk(26, "EarR", -16, -10, 0),
        rotk(8, "EarL", -12, 10, 0), rotk(26, "EarL", -12, 10, 0),
        rotk(10, "Tail1", 0, 0, -26), rotk(20, "Tail1", 0, 0, 22), rotk(30, "Tail1", 0, 0, -16),
        sck(11, "Body", 0.98, 0.98, 1.04), sck(29, "Body", 0.98, 0.98, 1.04),
    ])

    clip("TailFlick", 20, [
        rotk(3, "Tail1", 0, -12, 44), rotk(9, "Tail1", 0, 8, -36), rotk(15, "Tail1", 0, -4, 16),
        rotk(5, "Tail2", 0, -14, 50), rotk(11, "Tail2", 0, 10, -42), rotk(17, "Tail2", 0, -4, 18),
        rotk(7, "Tail3", 0, -16, 54), rotk(13, "Tail3", 0, 12, -46), rotk(18, "Tail3", 0, -4, 20),
        rotk(5, "Body", 0, 0, -6), rotk(12, "Body", 0, 0, 5),
        rotk(4, "EarR", -10, -12, 0), rotk(11, "EarR", 0, 6, 0),
    ])

    clip("Attack", 22, [
        lock(3, "Root", 0, 0.30, 0.04), lock(9, "Root", 0, -0.72, 0.16), lock(14, "Root", 0, -0.40, 0.0), lock(19, "Root", 0, -0.08, 0),
        rotk(3, "Body", -11, 0, 0), rotk(9, "Body", 14, 0, 0), rotk(14, "Body", 7, 0, 0), rotk(19, "Body", -3, 0, 0),
        rotk(3, "Head", -12, 0, 0), rotk(9, "Head", 9, 0, 0), rotk(15, "Head", 3, 0, 0),
        rotk(3, "EarL", 20, 14, 0), rotk(9, "EarL", -34, 6, 0), rotk(3, "EarR", 20, -14, 0), rotk(9, "EarR", -34, -6, 0),
        rotk(3, "ArmL", 34, 0, 0), rotk(9, "ArmL", -82, -12, 0), rotk(15, "ArmL", -30, -6, 0),
        rotk(3, "ArmR", 34, 0, 0), rotk(9, "ArmR", -82, 12, 0), rotk(15, "ArmR", -30, 6, 0),
        sck(3, "Body", 1.10, 1.10, 0.90), sck(9, "Body", 0.90, 0.90, 1.14), sck(15, "Body", 1.04, 1.04, 0.97),
        rotk(6, "Tail1", 0, -30, 0), rotk(13, "Tail1", 0, 22, 0),
    ])

    clip("Hurt", 24, [
        lock(3, "Root", 0, 0.52, 0.12), lock(8, "Root", 0.12, 0.30, 0.03), lock(13, "Root", -0.10, 0.14, 0.02), lock(18, "Root", 0.04, 0.04, 0),
        rotk(3, "Body", -12, 6, 0), rotk(8, "Body", 6, -5, 0), rotk(13, "Body", -4, 4, 0),
        rotk(3, "Head", -24, 0, 0), rotk(8, "Head", 14, 0, 0), rotk(13, "Head", -8, 0, 0), rotk(18, "Head", 4, 0, 0),
        rotk(3, "EarL", 32, 30, 0), rotk(10, "EarL", 22, 20, 0), rotk(16, "EarL", 10, 8, 0),
        rotk(3, "EarR", 32, -30, 0), rotk(10, "EarR", 22, -20, 0), rotk(16, "EarR", 10, -8, 0),
        sck(3, "Body", 1.14, 1.14, 0.86), sck(10, "Body", 0.96, 0.96, 1.06), sck(16, "Body", 1.03, 1.03, 0.98),
        rotk(4, "ArmL", 0, -40, 0), rotk(12, "ArmL", 0, -18, 0), rotk(4, "ArmR", 0, 40, 0), rotk(12, "ArmR", 0, 18, 0),
        rotk(5, "Tail1", 0, 12, -20), rotk(13, "Tail1", 0, 8, 14),
    ])

    clip("Eat", 44, [
        rotk(5, "Head", 30, 0, 0), rotk(9, "Head", 13, 0, 4), rotk(14, "Head", 30, 0, 0), rotk(18, "Head", 13, 0, -4),
        rotk(23, "Head", 30, 0, 0), rotk(27, "Head", 13, 0, 3), rotk(32, "Head", 28, 0, 0), rotk(38, "Head", 8, 0, 0),
        lock(5, "Head", 0, -0.12, -0.10), lock(14, "Head", 0, -0.12, -0.10), lock(23, "Head", 0, -0.12, -0.10), lock(32, "Head", 0, -0.10, -0.08),
        sck(5, "Body", 1.06, 1.06, 0.93), sck(14, "Body", 1.06, 1.06, 0.93), sck(23, "Body", 1.06, 1.06, 0.93), sck(32, "Body", 1.04, 1.04, 0.95),
        lock(5, "Root", 0, 0, -0.03), lock(23, "Root", 0, 0, -0.03),
        rotk(5, "EarL", 16, 14, 0), rotk(14, "EarL", 12, 10, 0), rotk(23, "EarL", 16, 14, 0),
        rotk(5, "EarR", 16, -14, 0), rotk(14, "EarR", 12, -10, 0), rotk(23, "EarR", 16, -14, 0),
        rotk(6, "Tail1", 0, 0, 20), rotk(16, "Tail1", 0, 0, -20), rotk(26, "Tail1", 0, 0, 20), rotk(36, "Tail1", 0, 0, -14),
        rotk(9, "Tail2", 0, 0, 24), rotk(19, "Tail2", 0, 0, -24), rotk(29, "Tail2", 0, 0, 22),
    ])

    clip("Celebrate", 48, [
        lock(4, "Root", 0, 0, -0.04), lock(11, "Root", 0, 0, 0.80), lock(18, "Root", 0, 0, 0.02), lock(24, "Root", 0, 0, -0.03),
        lock(31, "Root", 0, 0, 0.80), lock(38, "Root", 0, 0, 0.02), lock(44, "Root", 0, 0, 0.05),
        sck(4, "Body", 1.16, 1.16, 0.82), sck(11, "Body", 0.86, 0.86, 1.22), sck(18, "Body", 1.12, 1.12, 0.87),
        sck(24, "Body", 1.16, 1.16, 0.82), sck(31, "Body", 0.86, 0.86, 1.22), sck(38, "Body", 1.10, 1.10, 0.90), sck(44, "Body", 0.99, 0.99, 1.02),
        rotk(7, "ArmL", 0, -128, 0), rotk(20, "ArmL", 0, -112, 0), rotk(31, "ArmL", 0, -128, 0), rotk(42, "ArmL", 0, -44, 0),
        rotk(7, "ArmR", 0, 128, 0), rotk(20, "ArmR", 0, 112, 0), rotk(31, "ArmR", 0, 128, 0), rotk(42, "ArmR", 0, 44, 0),
        rotk(4, "Head", 10, 0, 0), rotk(11, "Head", -20, 0, 0), rotk(18, "Head", 8, 0, 0), rotk(31, "Head", -20, 0, 0), rotk(40, "Head", 4, 0, 0),
        rotk(7, "EarL", -28, 12, 0), rotk(20, "EarL", 12, 4, 0), rotk(31, "EarL", -28, 12, 0),
        rotk(7, "EarR", -28, -12, 0), rotk(20, "EarR", 12, -4, 0), rotk(31, "EarR", -28, -12, 0),
        rotk(6, "Tail1", 0, -20, 40), rotk(16, "Tail1", 0, -16, -40), rotk(26, "Tail1", 0, -20, 40), rotk(36, "Tail1", 0, -16, -34),
        rotk(9, "Tail2", 0, -16, 44), rotk(19, "Tail2", 0, -12, -44), rotk(29, "Tail2", 0, -16, 42),
    ])

    clip("Dance", 60, [
        rotk(7, "Body", 0, 12, -8), rotk(22, "Body", 0, -12, 8), rotk(37, "Body", 0, 12, -8), rotk(52, "Body", 0, -12, 8),
        lock(7, "Root", 0.24, 0, 0.08), lock(15, "Root", 0, 0, 0.14), lock(22, "Root", -0.24, 0, 0.08), lock(30, "Root", 0, 0, 0.14),
        lock(37, "Root", 0.24, 0, 0.08), lock(45, "Root", 0, 0, 0.14), lock(52, "Root", -0.24, 0, 0.08),
        rotk(7, "ArmL", 0, -104, 0), rotk(22, "ArmL", 0, -26, 0), rotk(37, "ArmL", 0, -104, 0), rotk(52, "ArmL", 0, -26, 0),
        rotk(7, "ArmR", 0, 26, 0), rotk(22, "ArmR", 0, 104, 0), rotk(37, "ArmR", 0, 26, 0), rotk(52, "ArmR", 0, 104, 0),
        rotk(7, "Head", -8, -12, 10), rotk(22, "Head", -8, 12, -10), rotk(37, "Head", -8, -12, 10), rotk(52, "Head", -8, 12, -10),
        rotk(7, "EarL", -10, 24, 0), rotk(22, "EarL", -10, -6, 0), rotk(37, "EarL", -10, 24, 0), rotk(52, "EarL", -10, -6, 0),
        rotk(7, "EarR", -10, 6, 0), rotk(22, "EarR", -10, -24, 0), rotk(37, "EarR", -10, 6, 0), rotk(52, "EarR", -10, -24, 0),
        sck(15, "Body", 0.96, 0.96, 1.07), sck(30, "Body", 0.96, 0.96, 1.07), sck(45, "Body", 0.96, 0.96, 1.07),
        rotk(7, "Tail1", 0, -14, 32), rotk(22, "Tail1", 0, -14, -32), rotk(37, "Tail1", 0, -14, 32), rotk(52, "Tail1", 0, -14, -32),
        rotk(11, "Tail2", 0, -10, 36), rotk(26, "Tail2", 0, -10, -36), rotk(41, "Tail2", 0, -10, 36),
    ])

    clip("Nod", 26, [
        rotk(4, "Head", 28, 0, 0), rotk(9, "Head", 0, 0, 0), rotk(14, "Head", 25, 0, 0), rotk(19, "Head", 3, 0, 0), rotk(23, "Head", 9, 0, 0),
        lock(4, "Head", 0, 0, -0.07), lock(14, "Head", 0, 0, -0.06),
        sck(5, "Body", 1.05, 1.05, 0.95), sck(15, "Body", 1.04, 1.04, 0.96),
        lock(5, "Root", 0, 0, -0.02), lock(15, "Root", 0, 0, -0.02),
        rotk(4, "EarL", 16, 10, 0), rotk(14, "EarL", 14, 8, 0), rotk(4, "EarR", 16, -10, 0), rotk(14, "EarR", 14, -8, 0),
        rotk(8, "Tail1", 0, 0, 16), rotk(18, "Tail1", 0, 0, -12),
    ])

    clip("Shiver", 30, [
        *[rotk(f, "Body", 0, 8 if (f // 2) % 2 == 0 else -8, 0) for f in range(2, 29, 2)],
        *[rotk(f, "Head", 0, -6 if (f // 2) % 2 == 0 else 6, 0) for f in range(2, 29, 2)],
        *[lock(f, "Root", 0.05 if (f // 2) % 2 == 0 else -0.05, 0, 0) for f in range(2, 29, 2)],
        *[rotk(f, "EarL", 22, 30 if (f // 3) % 2 == 0 else 14, 0) for f in range(3, 28, 3)],
        *[rotk(f, "EarR", 22, -30 if (f // 3) % 2 == 0 else -14, 0) for f in range(3, 28, 3)],
        sck(4, "Body", 1.06, 1.06, 0.94), sck(12, "Body", 1.05, 1.05, 0.95), sck(20, "Body", 1.06, 1.06, 0.94),
        rotk(6, "Tail1", 0, 10, -18), rotk(16, "Tail1", 0, 10, 16), rotk(24, "Tail1", 0, 8, -12),
    ])

    clip("Stretch", 54, [
        sck(6, "Body", 1.14, 1.14, 0.86), sck(17, "Body", 0.86, 0.86, 1.26), sck(32, "Body", 0.88, 0.88, 1.24),
        sck(42, "Body", 1.08, 1.08, 0.92), sck(49, "Body", 0.99, 0.99, 1.02),
        lock(6, "Root", 0, 0, -0.04), lock(17, "Root", 0, 0, 0.14), lock(32, "Root", 0, 0, 0.12), lock(44, "Root", 0, 0, -0.01),
        rotk(6, "Head", 14, 0, 0), rotk(17, "Head", -24, 0, 0), rotk(32, "Head", -22, 0, 0), rotk(44, "Head", 8, 0, 0),
        rotk(6, "ArmL", 26, 0, 0), rotk(17, "ArmL", 0, -148, 0), rotk(32, "ArmL", 0, -146, 0), rotk(44, "ArmL", 0, -34, 0),
        rotk(6, "ArmR", 26, 0, 0), rotk(17, "ArmR", 0, 148, 0), rotk(32, "ArmR", 0, 146, 0), rotk(44, "ArmR", 0, 34, 0),
        rotk(6, "EarL", 18, 12, 0), rotk(17, "EarL", -30, 8, 0), rotk(32, "EarL", -28, 6, 0),
        rotk(6, "EarR", 18, -12, 0), rotk(17, "EarR", -30, -8, 0), rotk(32, "EarR", -28, -6, 0),
        rotk(17, "Tail1", 0, -40, 0), rotk(32, "Tail1", 0, -38, 8), rotk(44, "Tail1", 0, -12, 0),
        rotk(19, "Tail2", 0, -32, 0), rotk(34, "Tail2", 0, -30, 10),
    ])

    clip("Yawn", 40, [
        rotk(6, "Head", -10, 0, 0), rotk(15, "Head", -28, 0, 0), rotk(25, "Head", -26, 0, 0), rotk(33, "Head", 12, 0, 0), rotk(38, "Head", -3, 0, 0),
        sck(6, "Body", 1.04, 1.04, 0.97), sck(15, "Body", 0.92, 0.92, 1.14), sck(25, "Body", 0.93, 0.93, 1.12), sck(34, "Body", 1.08, 1.08, 0.92),
        lock(15, "Root", 0, 0, 0.08), lock(25, "Root", 0, 0, 0.06), lock(34, "Root", 0, 0, -0.02),
        rotk(15, "EarL", -22, 16, 0), rotk(25, "EarL", -20, 14, 0), rotk(34, "EarL", 12, 4, 0),
        rotk(15, "EarR", -22, -16, 0), rotk(25, "EarR", -20, -14, 0), rotk(34, "EarR", 12, -4, 0),
        rotk(10, "Tail1", 0, -24, 0), rotk(26, "Tail1", 0, -22, 12), rotk(36, "Tail1", 0, -8, 0),
    ])

    clip("Shake", 24, [
        rotk(3, "Head", 0, 0, -30), rotk(7, "Head", 0, 0, 28), rotk(11, "Head", 0, 0, -24), rotk(15, "Head", 0, 0, 20), rotk(19, "Head", 0, 0, -11), rotk(22, "Head", 0, 0, 5),
        rotk(3, "EarL", 0, 40, 0), rotk(7, "EarL", 0, -16, 0), rotk(11, "EarL", 0, 32, 0), rotk(15, "EarL", 0, -12, 0), rotk(19, "EarL", 0, 16, 0),
        rotk(3, "EarR", 0, -16, 0), rotk(7, "EarR", 0, 40, 0), rotk(11, "EarR", 0, -12, 0), rotk(15, "EarR", 0, 32, 0), rotk(19, "EarR", 0, -16, 0),
        rotk(4, "Body", 0, 0, 9), rotk(9, "Body", 0, 0, -9), rotk(14, "Body", 0, 0, 7), rotk(19, "Body", 0, 0, -4),
        sck(6, "Body", 1.05, 1.05, 0.96), sck(16, "Body", 1.04, 1.04, 0.97),
        rotk(6, "Tail1", 0, 0, 26), rotk(14, "Tail1", 0, 0, -22),
    ])

    clip("EarTwitch", 16, [
        rotk(3, "EarR", -14, -36, 0), rotk(7, "EarR", 4, 14, 0), rotk(11, "EarR", -6, -22, 0), rotk(14, "EarR", 0, -4, 0),
        rotk(4, "Head", 0, 0, -7), rotk(11, "Head", 0, 0, 3),
        rotk(5, "EarL", -6, 8, 0), rotk(12, "EarL", 0, -4, 0),
    ])

    clip("LookAround", 64, [
        rotk(8, "Head", -6, 0, 36), rotk(20, "Head", -6, 0, 34), rotk(32, "Head", -4, 0, 0), rotk(42, "Head", -6, 0, -36), rotk(54, "Head", -6, 0, -34), rotk(62, "Head", 0, 0, -6),
        rotk(6, "EarL", -22, 16, 0), rotk(20, "EarL", -18, 10, 0), rotk(42, "EarL", -14, 6, 0),
        rotk(6, "EarR", -14, -6, 0), rotk(20, "EarR", -12, -4, 0), rotk(42, "EarR", -22, -16, 0), rotk(54, "EarR", -18, -10, 0),
        rotk(14, "Body", 0, 0, 9), rotk(32, "Body", 0, 0, 0), rotk(48, "Body", 0, 0, -9),
        rotk(12, "Tail1", 0, -16, 22), rotk(32, "Tail1", 0, -12, 0), rotk(50, "Tail1", 0, -16, -22),
    ])

    clip("Sniff", 32, [
        rotk(4, "Head", 28, 0, 0), rotk(8, "Head", 21, 0, 5), rotk(12, "Head", 30, 0, 0), rotk(16, "Head", 21, 0, -5), rotk(20, "Head", 30, 0, 0), rotk(25, "Head", 18, 0, 0), rotk(29, "Head", 6, 0, 0),
        lock(4, "Head", 0, -0.16, -0.12), lock(12, "Head", 0, -0.18, -0.12), lock(20, "Head", 0, -0.16, -0.12), lock(27, "Head", 0, -0.06, -0.04),
        rotk(4, "EarL", -24, 10, 0), rotk(16, "EarL", -20, 8, 0), rotk(26, "EarL", -10, 4, 0),
        rotk(4, "EarR", -24, -10, 0), rotk(16, "EarR", -20, -8, 0), rotk(26, "EarR", -10, -4, 0),
        sck(6, "Body", 1.05, 1.05, 0.95), sck(14, "Body", 1.05, 1.05, 0.95), sck(22, "Body", 1.04, 1.04, 0.96),
        lock(6, "Root", 0, 0, -0.02), lock(20, "Root", 0, 0, -0.02),
        rotk(8, "Tail1", 0, 0, 18), rotk(20, "Tail1", 0, 0, -16),
    ])

    clip("Groom", 56, [
        rotk(8, "Head", 16, -6, -24), rotk(24, "Head", 18, -6, -26), rotk(40, "Head", 16, -6, -24), rotk(50, "Head", 5, -2, -8),
        rotk(10, "ArmR", -108, -16, 0), rotk(17, "ArmR", -122, -22, 0), rotk(24, "ArmR", -108, -16, 0), rotk(31, "ArmR", -122, -22, 0),
        rotk(38, "ArmR", -108, -16, 0), rotk(44, "ArmR", -118, -20, 0), rotk(51, "ArmR", -40, -6, 0),
        rotk(8, "Body", 4, -6, -5), rotk(40, "Body", 4, -6, -5), lock(8, "Root", 0, 0, 0.05), lock(40, "Root", 0, 0, 0.05),
        rotk(8, "EarR", 8, -26, 0), rotk(24, "EarR", 10, -28, 0), rotk(40, "EarR", 8, -26, 0),
        rotk(8, "EarL", 6, 14, 0), rotk(40, "EarL", 6, 14, 0),
        sck(12, "Body", 1.04, 1.04, 0.96), sck(30, "Body", 1.04, 1.04, 0.96),
        rotk(12, "Tail1", 0, -8, 22), rotk(30, "Tail1", 0, -8, -18), rotk(46, "Tail1", 0, -6, 14),
    ])

    rig.animation_data.action = bpy.data.actions["Idle"]
    scene.frame_set(0)

    if ANIM_DIR:
        # one strip per clip: 6 evenly spaced frames, three-quarter view, features hidden
        import numpy as np
        os.makedirs(ANIM_DIR, exist_ok=True)
        show()
        for o in HIDDEN: o.hide_render = True
        shadow.hide_render = True
        scene.render.resolution_x = scene.render.resolution_y = 360
        aim((7.0, -8.6, 2.6), (0, 0, 1.78))
        for act in sorted(bpy.data.actions, key=lambda a: a.name):
            if ANIM_ONLY and act.name not in ANIM_ONLY.split(","): continue
            rig.animation_data.action = act
            frames = []
            for i in range(6):
                f = int(round(act.frame_end * i / 5.0))
                scene.frame_set(f)
                scene.render.filepath = os.path.join(ANIM_DIR, "_frame.png")
                bpy.ops.render.render(write_still=True)
                im = bpy.data.images.load(scene.render.filepath); w, h = im.size
                a = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]; bpy.data.images.remove(im)
                frames.append(a)
            strip = np.concatenate(frames, axis=1)
            out = bpy.data.images.new("strip", strip.shape[1], strip.shape[0], alpha=True)
            out.pixels = strip[::-1].ravel().tolist()
            out.filepath_raw = os.path.join(ANIM_DIR, "anim-%s.png" % act.name); out.file_format = 'PNG'; out.save()
            bpy.data.images.remove(out)
        os.remove(os.path.join(ANIM_DIR, "_frame.png"))
        rig.animation_data.action = bpy.data.actions["Idle"]; scene.frame_set(0)
        print("[build_cat2] animation strips:", len(bpy.data.actions), "->", ANIM_DIR)

if FBX_PATH:
    # Blender-only outline shells go (Unity draws its own), then one plain material per palette key
    for o in OBJECTS:
        o.hide_viewport = False; o.hide_render = False
        for mod in list(o.modifiers):
            if mod.type == 'SOLIDIFY': o.modifiers.remove(mod)          # Unity's toon shader draws its own outline hull

    # materials: one plain material per palette key (Unity picks colours and outline rules by that name)
    for mtl in list(bpy.data.materials):
        if mtl.name in PALETTE: mtl.name = mtl.name + "_preview"
    export_mats = {}
    def export_mat(key):
        if key not in export_mats:
            pm = bpy.data.materials.new(key); pm.use_nodes = False
            pm.diffuse_color = (*srgb_to_linear(PALETTE[key][0]), 1.0); pm.roughness = 0.9
            export_mats[key] = pm
        return export_mats[key]
    SLOT_KEY_OVERRIDE = {("FaceWhite", 0): "FaceWhite"}
    for o in OBJECTS:
        me = o.data
        old = [mt.name if mt else "" for mt in me.materials]
        keys = []
        for i, nm in enumerate(old):
            base = nm.replace("_preview", "").split("_")[0]
            if base == "Outline": base = "Ink"
            keys.append(SLOT_KEY_OVERRIDE.get((o.name, i), base if base in PALETTE else "Fur"))
        used = sorted({p.material_index for p in me.polygons}) or [0]
        new_keys = []; remap = {}
        for idx in used:
            k = keys[idx] if idx < len(keys) else "Fur"
            if k not in new_keys: new_keys.append(k)
            remap[idx] = new_keys.index(k)
        idx = [p.material_index for p in me.polygons]           # materials.clear() resets these, so snapshot first
        me.materials.clear()
        for k in new_keys: me.materials.append(export_mat(k))
        for p, i in zip(me.polygons, idx): p.material_index = remap.get(i, 0)

    os.makedirs(os.path.dirname(os.path.abspath(FBX_PATH)), exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    for o in OBJECTS: o.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=os.path.abspath(FBX_PATH), use_selection=True, object_types={'ARMATURE', 'MESH'},
        apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
        bake_space_transform=False, use_mesh_modifiers=True, mesh_smooth_type='OFF', use_custom_props=False,
        add_leaf_bones=False, primary_bone_axis='Y', secondary_bone_axis='X', armature_nodetype='NULL',
        bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
        path_mode='STRIP', embed_textures=False)
    print("[build_cat2] exported", FBX_PATH, "objects:", len(OBJECTS), "actions:", len(bpy.data.actions))

print("[build_cat2] objects:", len(OBJECTS))
