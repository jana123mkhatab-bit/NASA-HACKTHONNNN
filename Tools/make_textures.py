"""Upscales the 2K source textures to 4K and adds fine detail so close-ups are not mushy.
Real fix = drop NASA 8K/16K maps over these files (same names)."""
import sys, os, numpy as np
from PIL import Image, ImageFilter
from scipy.ndimage import zoom, gaussian_filter
src, dst = sys.argv[1], sys.argv[2]
rng = np.random.default_rng(7)
def fbm(h, w, octs=6, base=8):
    out = np.zeros((h, w), np.float32); amp = 1.0; tot = 0
    for o in range(octs):
        n = base * 2**o
        g = rng.standard_normal((max(2, n//2), n)).astype(np.float32)
        g = zoom(g, (h/g.shape[0], w/g.shape[1]), order=3, mode='wrap')[:h, :w]
        out += g*amp; tot += amp; amp *= 0.55
    return out/tot
def up(name, detail, mask_fn=None, size=(4096, 2048), sharpen=1.0, quality=90):
    im = Image.open(os.path.join(src, name+'.jpg')).convert('RGB').resize(size, Image.LANCZOS)
    im = im.filter(ImageFilter.UnsharpMask(radius=2.0, percent=int(120*sharpen), threshold=2))
    a = np.asarray(im).astype(np.float32)/255.0
    if detail > 0:
        n = fbm(size[1], size[0], 7, 16)
        n = (n - n.mean())/ (n.std()+1e-6)
        m = 1.0 if mask_fn is None else mask_fn(a)
        a = a*(1 + (detail*n*m)[..., None])
    Image.fromarray((np.clip(a, 0, 1)*255).astype(np.uint8)).save(os.path.join(dst, name+'.jpg'), quality=quality, subsampling=0)
    print('wrote', name)
def land(a):  # not ocean, not ice -> add terrain detail on land
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    return np.clip((r + g - 1.6*b)*3 + 0.15, 0, 1)*0.9
os.makedirs(dst, exist_ok=True)
up('earth_daymap', 0.16, land)
up('earth_nightmap', 0.0, sharpen=0.6)
up('earth_clouds', 0.0, sharpen=0.8)
up('moon', 0.22)
up('sun', 0.10, quality=88)
up('stars_milkyway', 0.0, sharpen=0.5, quality=85)
