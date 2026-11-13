import os, sys, math, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from meshlib import *

OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Resources", "Models", "Custom")
os.makedirs(OUT, exist_ok=True)

def mirrored(M, mat, V, F, smooth=True):
    M.add(mat, V, F, smooth); V2, F2 = mirror_x(V, F); M.add(mat, V2, F2, smooth)

# ---------------------------------------------------------------- SHIP  (nose = +Z, up = +Y)
def build_ship():
    M = Mesh()
    st = [  # z, cx, cy, w, h, n
        (7.4, 0, -0.05, 0.04, 0.04, 2.2),
        (6.4, 0, -0.04, 0.32, 0.20, 2.4),
        (5.0, 0, -0.05, 0.72, 0.36, 2.6),
        (3.0, 0, 0.00, 1.20, 0.58, 2.8),
        (0.5, 0, 0.05, 1.55, 0.76, 3.0),
        (-2.0, 0, 0.02, 1.55, 0.72, 3.2),
        (-4.2, 0, -0.04, 1.30, 0.58, 3.4),
        (-5.0, 0, -0.04, 1.15, 0.50, 3.4)]
    M.add("ship_hull", *loft(st, 44))
    # dark belly plate (slightly smaller, lower loft)
    belly = [(6.0, 0, -0.18, 0.28, 0.06, 2.4), (3.0, 0, -0.45, 1.0, 0.18, 2.8), (0.0, 0, -0.6, 1.3, 0.2, 3.0),
             (-3.0, 0, -0.55, 1.15, 0.18, 3.0), (-4.9, 0, -0.5, 0.95, 0.14, 3.0)]
    M.add("ship_dark", *loft(belly, 32))
    # canopy
    V, F = superellipsoid((0, 0.55, 2.4), (0.78, 0.5, 1.9), 1.0, 1.0, 14, 32, -5, 90)
    M.add("ship_glass", *orient_outward(V, F, (0, 0.4, 2.4)), closed=False)
    # canopy frame ring
        # wings (swept delta with slight dihedral) - one side then mirrored
    poly = [(1.2, 3.2), (7.0, -2.0), (7.6, -3.9), (6.6, -4.6), (1.2, -4.7)]
    y0 = [-0.12 + 0.06*x for x, z in poly]; y1 = [v + 0.15 for v in y0]
    mirrored(M, "ship_hull", *prism(poly, y0, y1), smooth=False)
    # wing under-plate in dark
    poly2 = [(1.3, 2.6), (6.3, -1.6), (6.8, -3.4), (6.0, -4.0), (1.3, -4.1)]
    y0b = [-0.145 + 0.06*x for x, z in poly2]; y1b = [v + 0.03 for v in y0b]
    mirrored(M, "ship_dark", *prism(poly2, y0b, y1b), smooth=False)
    # winglets (canted outward)
    wl = [(-1.8, 0.0), (-4.4, 0.0), (-4.5, 1.9), (-3.5, 2.2)]  # (z, y) polygon
    P = [(z, y) for z, y in wl]
    # build as prism in (z,y) -> rotate: use prism on (x,z)=(z,y) then permute axes
    Vp, Fp = prism(P, -0.05, 0.05)       # coordinates: [x=z, y=thick, z=y]
    Vw = np.stack([Vp[:, 1], Vp[:, 2], Vp[:, 0]], axis=1)   # x=thick, y=height, z=zpos
    Vw = rot(Vw, 2, -18)                                     # cant outward
    Vw += np.array([7.3, 0.28, 0.0])
    mirrored(M, "ship_accent", Vw, Fp, smooth=False)
    # V-tail fins
    tl = [(-2.6, 0.0), (-5.0, 0.0), (-5.1, 1.6), (-4.2, 1.7)]
    Vp, Fp = prism(tl, -0.04, 0.04)
    Vt = np.stack([Vp[:, 1], Vp[:, 2], Vp[:, 0]], axis=1)
    Vt = rot(Vt, 2, -24) + np.array([0.95, 0.62, 0.0])
    mirrored(M, "ship_hull", Vt, Fp, smooth=False)
    # engine nacelles (3): dark casing, bright inner glow disc, ring
    for ex, er, ez in ((2.45, 0.55, -3.2), (-2.45, 0.55, -3.2), (0.0, 0.7, -4.6)):
        M.add("ship_engine", *cylinder((ex, 0.02, ez), (ex, 0.02, ez - 2.6 if ex else ez - 1.9), er*0.9, er, 28))
        zend = ez - 2.6 if ex else ez - 1.9
        M.add("ship_accent", *torus((ex, 0.02, zend), er*1.02, 0.07, 2, 36, 10))
        M.add("ship_glow", *cylinder((ex, 0.02, zend + 0.02), (ex, 0.02, zend - 0.06), er*0.82, er*0.82, 28))
        if ex:   # intake scoop at front of nacelle
            V, F = superellipsoid((ex, 0.02, ez), (er*0.95, er*0.95, 0.9), 1, 1, 10, 24, -90, 90)
            # only the forward hemisphere (z > ez) is wanted as a scoop
            M.add("ship_hull", *orient_outward(V, F, (ex, 0.02, ez)), closed=False)
    # glow rails: wing leading edges, fuselage flanks, canopy base, tips
    def rail(p0, p1, r=0.05, mat="ship_glow"):
        M.add(mat, *tube(p0, p1, r, 6))
    for s in (1, -1):
        rail((s*1.3, -0.02 + 0.06*1.3 + 0.2, 3.0), (s*6.9, -0.12 + 0.06*7.0 + 0.2, -1.95))   # LE rail
        rail((s*6.9, 0.50, -1.95), (s*7.45, 0.52, -3.8), 0.04)
        rail((s*1.45, 0.42, 1.5), (s*1.5, 0.42, -3.6), 0.04)                                   # flank
        rail((s*0.95, 0.45, 4.6), (s*1.2, 0.45, 2.6), 0.035)
        # wingtip beacon
        V, F = superellipsoid((s*7.55, 0.7, -3.4), (0.12, 0.12, 0.12), 1, 1, 6, 10)
        M.add("ship_glow", V, F)
    # nose sensor sphere + dorsal antenna
    V, F = superellipsoid((0, 0.05, 7.2), (0.12, 0.12, 0.18), 1, 1, 8, 12); M.add("ship_accent", V, F)
    M.add("ship_dark", *tube((0, 0.72, -3.0), (0, 1.5, -3.6), 0.03, 6))
    return M

def build_gear():
    M = Mesh()
    M.add("ship_dark", *cylinder((0, 0, 0), (0, -1.15, 0), 0.09, 0.07, 10))
    M.add("ship_accent", *cylinder((0, -1.15, 0), (0, -1.25, 0), 0.5, 0.46, 20))
    M.add("ship_glow", *torus((0, -1.22, 0), 0.47, 0.025, 1, 24, 6))
    return M

# ---------------------------------------------------------------- ASTRONAUT parts (metres)
def build_astro():
    parts = {}
    # torso: pivot at hip centre (y=0), spans 0..0.62
    M = Mesh()
    V, F = superellipsoid((0, 0.32, 0), (0.27, 0.32, 0.16), 0.55, 0.6, 14, 24); M.add("suit_white", V, F)
    V, F = superellipsoid((0, 0.05, 0), (0.25, 0.1, 0.15), 0.6, 0.7, 8, 20); M.add("suit_dark", V, F)
    V, F = box((0, 0.42, 0.155), (0.26, 0.2, 0.05)); M.add("suit_dark", V, F)             # chest control box
    for i, c in enumerate(("suit_glow", "suit_orange", "suit_blue")):
        V, F = superellipsoid((-0.07 + 0.07*i, 0.45, 0.185), (0.022, 0.022, 0.012), 1, 1, 6, 10); M.add(c, V, F)
    V, F = superellipsoid((0, 0.62, 0), (0.17, 0.06, 0.13), 0.6, 0.8, 8, 16); M.add("suit_dark", V, F)  # neck ring
    parts["astro_torso"] = M
    # head/helmet: pivot at neck base (0,0,0)
    M = Mesh()
    V, F = superellipsoid((0, 0.2, 0), (0.22, 0.23, 0.23), 0.9, 0.9, 16, 32); M.add("suit_white", V, F)
    V, F = superellipsoid((0, 0.21, 0.07), (0.17, 0.15, 0.17), 0.85, 0.8, 12, 28, -60, 75); M.add("suit_visor", V, F)
    V, F = superellipsoid((0.0, 0.43, 0.0), (0.06, 0.03, 0.06), 1, 1, 6, 12); M.add("suit_orange", V, F)
    parts["astro_head"] = M
    # backpack: pivot at attach point on back
    M = Mesh()
    V, F = superellipsoid((0, 0.28, -0.14), (0.26, 0.32, 0.13), 0.45, 0.5, 12, 24); M.add("suit_pack", V, F)
    V, F = box((0, 0.1, -0.27), (0.34, 0.1, 0.05)); M.add("suit_dark", V, F)
    for sx in (-0.12, 0.12):
        M.add("suit_orange", *cylinder((sx, 0.0, -0.22), (sx, -0.16, -0.22), 0.06, 0.07, 14))
        M.add("suit_glow", *cylinder((sx, -0.16, -0.22), (sx, -0.17, -0.22), 0.05, 0.05, 14))
    V, F = box((0, 0.5, -0.2), (0.22, 0.03, 0.03)); M.add("suit_glow", V, F)
    parts["astro_pack"] = M
    # arm: pivot at shoulder, hangs along -Y (one arm; builder places two)
    M = Mesh()
    V, F = superellipsoid((0, 0, 0), (0.1, 0.1, 0.1), 1, 1, 10, 18); M.add("suit_white", V, F)                 # shoulder joint
    M.add("suit_white", *cylinder((0, -0.02, 0), (0, -0.3, 0), 0.075, 0.07, 18))
    V, F = superellipsoid((0, -0.32, 0), (0.075, 0.075, 0.075), 1, 1, 8, 16); M.add("suit_dark", V, F)         # elbow
    M.add("suit_white", *cylinder((0, -0.33, 0), (0, -0.56, 0), 0.07, 0.06, 18))
    V, F = superellipsoid((0, -0.64, 0), (0.07, 0.085, 0.07), 0.9, 0.9, 10, 18); M.add("suit_dark", V, F)      # glove
    M.add("suit_orange", *torus((0, -0.57, 0), 0.062, 0.015, 1, 20, 6))
    parts["astro_arm"] = M
    # leg: pivot at hip joint, hangs along -Y
    M = Mesh()
    V, F = superellipsoid((0, 0, 0), (0.11, 0.1, 0.11), 1, 1, 10, 18); M.add("suit_dark", V, F)
    M.add("suit_white", *cylinder((0, -0.02, 0), (0, -0.44, 0), 0.105, 0.085, 20))
    V, F = superellipsoid((0, -0.46, 0.01), (0.09, 0.09, 0.09), 1, 1, 8, 16); M.add("suit_dark", V, F)         # knee
    M.add("suit_white", *cylinder((0, -0.47, 0), (0, -0.82, 0), 0.085, 0.075, 20))
    V, F = superellipsoid((0, -0.88, 0.07), (0.095, 0.075, 0.18), 0.7, 0.8, 10, 18); M.add("suit_boot", V, F)  # boot
    M.add("suit_orange", *torus((0, -0.78, 0), 0.08, 0.015, 1, 20, 6))
    parts["astro_leg"] = M
    return parts

if __name__ == "__main__":
    ship = build_ship(); ship.write(os.path.join(OUT, "ship_aurora.obj"), "ShipAurora")
    build_gear().write(os.path.join(OUT, "ship_gear.obj"), "ShipGear")
    for k, m in build_astro().items(): m.write(os.path.join(OUT, k + ".obj"), k)
    print("written", os.listdir(OUT))
