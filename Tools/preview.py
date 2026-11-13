import os, sys, numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import matplotlib; matplotlib.use("Agg")
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
from make_models import build_ship, build_astro
COL = {"ship_hull":(.85,.88,.92),"ship_dark":(.12,.13,.17),"ship_accent":(.15,.3,.55),"ship_glass":(.3,.6,.8),
       "ship_glow":(.2,1,1),"ship_engine":(.25,.26,.3),"suit_white":(.92,.92,.9),"suit_dark":(.2,.2,.22),
       "suit_visor":(.9,.7,.2),"suit_orange":(1,.5,.1),"suit_blue":(.2,.4,.9),"suit_glow":(.2,1,.9),"suit_pack":(.75,.75,.78),"suit_boot":(.3,.3,.33)}
def render(meshes, ax, elev, azim, lim):
    L = np.array([.4,.8,.5]); L/=np.linalg.norm(L)
    polys=[];cols=[]
    for M, off in meshes:
        for mat, tri in M.triangles():
            t = tri + off; n = np.cross(t[1]-t[0], t[2]-t[0]); l=np.linalg.norm(n)
            if l<1e-12: continue
            n/=l; sh=.35+.65*max(0,np.dot(n,L))
            polys.append(t); cols.append(tuple(np.clip(np.array(COL.get(mat,(.5,.5,.5)))*sh,0,1)))
    pc = Poly3DCollection(polys, facecolors=cols, edgecolors='none'); ax.add_collection3d(pc)
    ax.set_xlim(-lim[0],lim[0]); ax.set_ylim(-lim[1],lim[1]); ax.set_zlim(-lim[2],lim[2]); ax.view_init(elev, azim); ax.set_axis_off()
    ax.set_box_aspect((lim[0],lim[1],lim[2]))
fig = plt.figure(figsize=(16,8), facecolor='#0a0f1c')
ship = build_ship()
# our axes: x right, y up, z forward  -> matplotlib (x, z, y)
def swap(M):
    from meshlib import Mesh
    N=Mesh()
    for mat,l in M.parts.items():
        for V,F,s in l: N.parts.setdefault(mat,[]).append((V[:,[0,2,1]],F,s))
    return N
S = swap(ship)
for i,(e,a) in enumerate(((22,-60),(90,-90),(0,-90),(0,0))):
    ax = fig.add_subplot(2,4,i+1, projection='3d', facecolor='#0a0f1c'); render([(S,0)],ax,e,a,(7.5,7.5,3.2))
# astronaut assembled
A = build_astro()
pl = {"astro_torso":(0,0.92,0),"astro_head":(0,1.54,0),"astro_pack":(0,0.92,0),"astro_arm":(0.34,1.48,0),"astro_leg":(0.14,0.92,0)}
meshes=[]
for k,off in pl.items():
    meshes.append((swap(A[k]), np.array([off[0],off[2],off[1]-0.95])))
    if k in("astro_arm","astro_leg"): meshes.append((swap(A[k]), np.array([-off[0],off[2],off[1]-0.95])))
for i,(e,a) in enumerate(((10,-60),(10,30),(0,-90),(0,0))):
    ax = fig.add_subplot(2,4,5+i, projection='3d', facecolor='#0a0f1c'); render(meshes,ax,e,a,(1,1,1))
plt.tight_layout(); plt.savefig('/home/claude/preview_models.png', dpi=70, facecolor=fig.get_facecolor())
# volume checks
from meshlib import signed_volume
for mat,l in ship.parts.items():
    print(mat, [round(signed_volume(V,F),2) for V,F,s in l][:4])
