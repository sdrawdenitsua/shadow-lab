"""
Shadow Lab — Blender Procedural Generator
==========================================
Run inside Blender 3.x / 4.x:
  Scripting tab → Open → shadow_lab_generator.py → Run Script

Generates the full Shadow Lab workshop:
  • Room shell (walls, floor, ceiling)
  • Support pillars
  • 2 Dornier HTV-style loom frames (detailed)
  • Main workbench + pegboard + tools
  • Mineral oil immersion PC tank
  • Overhead cable trays
  • Neon light strips (violet + amber)
  • Floor drain grates

Export: File → Export → FBX
  ✓ Selected Objects
  ✓ Apply Modifiers
  ✓ Forward: -Z Forward, Up: Y Up  (Unity's coordinate system)

All objects are named and organised in collections for easy Unity mapping.
"""

import bpy
import bmesh
import math
from mathutils import Vector

# ── Clear existing scene ─────────────────────────────────────────────
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete()
for col in list(bpy.data.collections):
    bpy.data.collections.remove(col)

# ── Dimensions ───────────────────────────────────────────────────────
ROOM_W  = 12.0
ROOM_D  = 18.0
ROOM_H  =  4.2
WALL_T  =  0.18

# ── Material palette ─────────────────────────────────────────────────
def make_mat(name, r, g, b, roughness=0.7, metallic=0.0, emission=None):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (r, g, b, 1)
    bsdf.inputs["Roughness"].default_value  = roughness
    bsdf.inputs["Metallic"].default_value   = metallic
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1)
        bsdf.inputs["Emission Strength"].default_value = 8.0
    return mat

MAT_CONCRETE     = make_mat("M_Concrete",   0.22, 0.20, 0.20, roughness=0.92)
MAT_METAL        = make_mat("M_Metal",      0.35, 0.35, 0.38, roughness=0.28, metallic=0.95)
MAT_GRATE        = make_mat("M_Grate",      0.18, 0.18, 0.20, roughness=0.55, metallic=0.80)
MAT_NEON_VIOLET  = make_mat("M_NeonViolet", 0.54, 0.00, 1.00, roughness=0.05, emission=(0.54, 0.00, 1.00))
MAT_NEON_AMBER   = make_mat("M_NeonAmber",  1.00, 0.60, 0.00, roughness=0.05, emission=(1.00, 0.60, 0.00))
MAT_GLASS        = make_mat("M_Glass",      0.80, 0.90, 0.88, roughness=0.02)
MAT_GLASS.node_tree.nodes["Principled BSDF"].inputs["Transmission Weight"].default_value = 0.92
MAT_OIL          = make_mat("M_Oil",        0.28, 0.22, 0.08, roughness=0.02)

# ── Helpers ──────────────────────────────────────────────────────────

def collection(name, parent=None):
    col = bpy.data.collections.new(name)
    (parent or bpy.context.scene.collection).children.link(col)
    return col

def box(name, loc, dims, mat, col):
    """Create a cube at loc with given dimensions and material."""
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = dims
    if mat:
        if obj.data.materials:
            obj.data.materials[0] = mat
        else:
            obj.data.materials.append(mat)
    # Move to collection
    for c in obj.users_collection:
        c.objects.unlink(obj)
    col.objects.link(obj)
    bpy.ops.object.transform_apply(scale=True)
    return obj

def cyl(name, loc, radius, depth, mat, col, segments=16):
    bpy.ops.mesh.primitive_cylinder_add(
        radius=radius, depth=depth, vertices=segments, location=loc)
    obj = bpy.context.active_object
    obj.name = name
    if mat:
        if obj.data.materials: obj.data.materials[0] = mat
        else: obj.data.materials.append(mat)
    for c in obj.users_collection: c.objects.unlink(obj)
    col.objects.link(obj)
    return obj

# ════════════════════════════════════════════════════════════════════
# ROOM
# ════════════════════════════════════════════════════════════════════

col_room = collection("Room")

# Floor
box("Floor",   (0, 0, -WALL_T/2),      (ROOM_W, ROOM_D, WALL_T),   MAT_CONCRETE, col_room)
# Ceiling
box("Ceiling", (0, 0, ROOM_H+WALL_T/2),(ROOM_W, ROOM_D, WALL_T),   MAT_METAL,    col_room)
# Walls (Blender Z-up)
box("Wall_North",(0, ROOM_D/2+WALL_T/2, ROOM_H/2),(ROOM_W, WALL_T, ROOM_H), MAT_CONCRETE, col_room)
box("Wall_South",(0,-ROOM_D/2-WALL_T/2, ROOM_H/2),(ROOM_W, WALL_T, ROOM_H), MAT_CONCRETE, col_room)
box("Wall_East", (ROOM_W/2+WALL_T/2, 0, ROOM_H/2),(WALL_T, ROOM_D, ROOM_H), MAT_CONCRETE, col_room)
box("Wall_West", (-ROOM_W/2-WALL_T/2,0, ROOM_H/2),(WALL_T, ROOM_D, ROOM_H), MAT_CONCRETE, col_room)

# Grate panels along lower walls
for i in range(4):
    z_pos = -ROOM_D/2 + 1.0 + i * (ROOM_D / 4)
    box(f"GratePanel_W{i}", (-ROOM_W/2+0.06, z_pos, 0.45), (0.04, 2.4, 0.9), MAT_GRATE, col_room)
    box(f"GratePanel_E{i}", ( ROOM_W/2-0.06, z_pos, 0.45), (0.04, 2.4, 0.9), MAT_GRATE, col_room)

# Pillars
col_pillars = collection("Pillars")
for y in [-ROOM_D*0.3, 0, ROOM_D*0.3]:
    box(f"Pillar_W_{y:.0f}", (-ROOM_W/2+0.25, y, ROOM_H/2), (0.3, 0.3, ROOM_H), MAT_METAL, col_pillars)
    box(f"Pillar_E_{y:.0f}", ( ROOM_W/2-0.25, y, ROOM_H/2), (0.3, 0.3, ROOM_H), MAT_METAL, col_pillars)

# ════════════════════════════════════════════════════════════════════
# DORNIER HTV LOOM FRAMES (2x)
# ════════════════════════════════════════════════════════════════════

col_looms = collection("Looms")

LOOM_W = 2.8
LOOM_D = 1.4
LOOM_H = 2.1

for li, ly in enumerate([-1.9, 1.9]):

    lc = collection(f"Loom_{li:02d}", col_looms)
    ox, oy = 0, ly   # loom origin in XY (Blender coords, Z-up)

    def lb(name, loc, dims, mat=MAT_METAL):
        """Loom box — offsets from loom origin."""
        box(name, (ox+loc[0], oy+loc[1], loc[2]), dims, mat, lc)

    # Frame rails
    lb("LeftRail",  (-LOOM_W/2, 0, LOOM_H/2),  (0.08, LOOM_D, LOOM_H))
    lb("RightRail", ( LOOM_W/2, 0, LOOM_H/2),  (0.08, LOOM_D, LOOM_H))
    lb("TopBeam",   (0, 0, LOOM_H),             (LOOM_W, 0.10, 0.10))
    lb("BaseBeam",  (0, 0, 0.05),               (LOOM_W, LOOM_D, 0.10))

    # Reed slats
    for s in range(14):
        y_slat = 0.30 + s * (LOOM_H * 0.70 / 14)
        lb(f"Reed_{s:02d}", (0, -LOOM_D*0.30, y_slat), (LOOM_W*0.96, 0.012, 0.012))

    # Rapier guide rail
    lb("RapierRail", (0, -LOOM_D*0.45, LOOM_H*0.45), (LOOM_W, 0.025, 0.025))

    # Dobby head
    lb("DobbyHead",  (LOOM_W*0.45, 0, LOOM_H+0.25), (0.5, 0.45, 0.5))

    # Warp beam (back, cylinder)
    cyl(f"WarpBeam_{li}", (ox, oy+LOOM_D*0.48, 0.55),
        radius=0.11, depth=LOOM_W*0.88, mat=MAT_METAL, col=lc)

    # Cloth roll (front, cylinder)
    cyl(f"ClothRoll_{li}", (ox, oy-LOOM_D*0.46, 0.32),
        radius=0.09, depth=LOOM_W*0.85, mat=MAT_METAL, col=lc)

    # Tension beam
    lb("TensionBeam", (0, LOOM_D*0.45, 0.28),  (LOOM_W*0.90, 0.08, 0.08))

    # Control panel
    lb("ControlPanel", (LOOM_W*0.5+0.15, -LOOM_D*0.2, LOOM_H*0.6), (0.12, 0.4, 0.6))

    # Oil drip tray
    lb("OilTray",  (0, 0, 0.02), (LOOM_W+0.2, LOOM_D+0.1, 0.04), MAT_GRATE)

# ════════════════════════════════════════════════════════════════════
# WORKBENCH
# ════════════════════════════════════════════════════════════════════

col_bench = collection("Workbench")
BX, BY = -ROOM_W/2 + 1.2, ROOM_D*0.30
BW, BD, BH = 2.4, 0.7, 0.9

def bb(name, loc, dims, mat=MAT_METAL):
    box(name, (BX+loc[0], BY+loc[1], loc[2]), dims, mat, col_bench)

bb("Top",       (0, 0, BH),          (BW, BD, 0.06))
for i, (bx, by) in enumerate([(-BW*.46,-BD*.46),( BW*.46,-BD*.46),(-BW*.46, BD*.46),( BW*.46, BD*.46)]):
    bb(f"Leg_{i}", (bx, by, BH/2),   (0.06, 0.06, BH))
bb("Shelf",     (0, 0, BH*0.45),     (BW, BD*0.9, 0.03))
bb("Pegboard",  (0, -BD/2+0.02, BH+0.60), (BW, 0.04, 1.1),  MAT_GRATE)
for i in range(5):
    tx = -BW*0.4 + i*(BW*0.2)
    bb(f"Tool_{i}", (tx, -BD/2+0.01, BH+0.55), (0.04, 0.02, 0.32))

# Vise
bb("Vise_Body",  (-BW*.44, BD*.3,  BH+0.07), (0.18, 0.22, 0.14))
bb("Vise_Jaw",   (-BW*.44, BD*.1,  BH+0.12), (0.18, 0.06, 0.08))
cyl("Vise_Screw", (BX-BW*.44, BY+BD*.45, BH+0.07),
    radius=0.02, depth=0.28, mat=MAT_METAL, col=col_bench)

# ════════════════════════════════════════════════════════════════════
# MINERAL OIL TANK
# ════════════════════════════════════════════════════════════════════

col_tank = collection("OilTank")
TX, TY = ROOM_W/2 - 1.4, -ROOM_D*0.30
TW, TD, TH = 0.70, 0.50, 0.55

def tb(name, loc, dims, mat=MAT_METAL):
    box(name, (TX+loc[0], TY+loc[1], loc[2]), dims, mat, col_tank)

tb("Wall_Front", (0, -TD/2, TH/2),  (TW, 0.012, TH),  MAT_GLASS)
tb("Wall_Back",  (0,  TD/2, TH/2),  (TW, 0.012, TH),  MAT_GLASS)
tb("Wall_Left",  (-TW/2, 0, TH/2),  (0.012, TD, TH),  MAT_GLASS)
tb("Wall_Right", ( TW/2, 0, TH/2),  (0.012, TD, TH),  MAT_GLASS)
tb("Base",       (0, 0, 0.02),      (TW+0.04, TD+0.04, 0.04))
tb("OilSurface", (0, 0, TH*0.82),   (TW-0.015, TD-0.015, 0.008), MAT_OIL)
tb("PCBoard",    (0, 0, TH*0.40),   (TW*0.85, TD*0.85, 0.01))
tb("Pump",       (TW/2+0.10, 0, 0.12), (0.16, 0.16, 0.18))
tb("Tube_H",     (TW*0.3, 0, TH*0.9),  (TW*0.4, 0.025, 0.025))
tb("Tube_V",     (TW/2+0.02, 0, TH*0.55), (0.025, 0.025, TH*0.7))
for i, (lx,ly) in enumerate([(-TW*.45,-TD*.45),(TW*.45,-TD*.45),(-TW*.45,TD*.45),(TW*.45,TD*.45)]):
    tb(f"Leg_{i}", (lx, ly, 0.12), (0.04, 0.04, 0.24))

# ════════════════════════════════════════════════════════════════════
# CABLE TRAYS
# ════════════════════════════════════════════════════════════════════

col_cables = collection("CableTrays")
CY = ROOM_H - 0.30

box("CableTray_Main", (0, 0, CY), (0.30, ROOM_D*0.9, 0.08), MAT_GRATE, col_cables)
for i, ly in enumerate([-1.9, 1.9]):
    box(f"CableTray_Cross_{i}", (0, ly, CY-0.05), (ROOM_W*0.7, 0.22, 0.06), MAT_GRATE, col_cables)
for i in range(6):
    cy_z = -ROOM_D*0.4 + i*(ROOM_D*0.16)
    box(f"Conduit_{i}", (0.08, cy_z, ROOM_H*0.6), (0.04, 0.04, ROOM_H*0.8), MAT_METAL, col_cables)

# ════════════════════════════════════════════════════════════════════
# NEON LIGHTS
# ════════════════════════════════════════════════════════════════════

col_neon = collection("NeonLights")
NY = ROOM_H - 0.15

box("Neon_Violet_W", (-ROOM_W*0.25, 0, NY), (0.04, ROOM_D*0.85, 0.04), MAT_NEON_VIOLET, col_neon)
box("Neon_Violet_E", ( ROOM_W*0.25, 0, NY), (0.04, ROOM_D*0.85, 0.04), MAT_NEON_VIOLET, col_neon)
box("Neon_Amber_Bench", (BX, BY, 0.82), (2.4, 0.02, 0.02), MAT_NEON_AMBER, col_neon)

for y in [-ROOM_D*0.35, 0, ROOM_D*0.35]:
    box(f"Neon_Wall_W_{y:.0f}", (-ROOM_W/2+0.06, y, ROOM_H*0.7), (0.025, 1.8, 0.025), MAT_NEON_VIOLET, col_neon)

# ════════════════════════════════════════════════════════════════════
# FLOOR DRAINS
# ════════════════════════════════════════════════════════════════════

col_drains = collection("FloorDrains")
for i, (dx, dy) in enumerate([(0,0),(-ROOM_W*.3,-ROOM_D*.3),(ROOM_W*.3,ROOM_D*.3)]):
    box(f"Drain_{i}", (dx, dy, 0.001), (0.5, 0.5, 0.025), MAT_GRATE, col_drains)

# ════════════════════════════════════════════════════════════════════
# WORLD SETTINGS
# ════════════════════════════════════════════════════════════════════

# Dark world background
bpy.context.scene.world.node_tree.nodes["Background"].inputs[0].default_value = (0.01, 0.01, 0.02, 1)
bpy.context.scene.world.node_tree.nodes["Background"].inputs[1].default_value = 0.05

# Set render engine to Cycles for accurate emission preview
bpy.context.scene.render.engine = 'CYCLES'
bpy.context.scene.cycles.samples = 64

print("=" * 60)
print("[ShadowLab] Blender scene generated successfully.")
print(f"  Room: {ROOM_W}m x {ROOM_D}m x {ROOM_H}m")
print(f"  Looms: 2x Dornier HTV-style")
print(f"  Collections: Room, Pillars, Looms, Workbench, OilTank,")
print(f"               CableTrays, NeonLights, FloorDrains")
print("")
print("  EXPORT: File → Export → FBX")
print("    ✓ Apply Modifiers")
print("    ✓ Forward: -Z Forward")
print("    ✓ Up: Y Up")
print("=" * 60)
