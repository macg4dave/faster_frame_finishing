"""Draw original artwork for Faster Frame Finishing (no game assets).

Usage: python -I tools/make_art.py <out_dir>   (needs Pillow; uses the Windows Bahnschrift font)
Copy Preview.png to src/FasterFrameFinishing/About/ and banner.png to docs/images/.
Writes Preview.png (512x512, Steam Workshop preview) and banner.png (1280x640, GitHub).
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

FONT = r"C:\Windows\Fonts\bahnschrift.ttf"
SS = 4  # supersampling factor

NAVY_TOP = (14, 22, 38)
NAVY_BOT = (28, 44, 70)
STEEL = (120, 132, 148)
STEEL_DARK = (62, 70, 84)
STEEL_HI = (190, 200, 212)
PLASTIC_TOP = (236, 242, 248)
PLASTIC_LEFT = (205, 214, 226)
PLASTIC_RIGHT = (170, 182, 198)
ORANGE = (255, 140, 26)
ORANGE_DARK = (204, 92, 0)
CYAN = (90, 200, 255)


def font(size, weight="Bold"):
    f = ImageFont.truetype(FONT, size)
    try:
        f.set_variation_by_name(weight)
    except Exception:  # noqa: BLE001
        pass
    return f


def background(w, h):
    img = Image.new("RGB", (w, h))
    px = ImageDraw.Draw(img)
    for y in range(h):
        t = y / max(1, h - 1)
        c = tuple(int(NAVY_TOP[i] + (NAVY_BOT[i] - NAVY_TOP[i]) * t) for i in range(3))
        px.line([(0, y), (w, y)], fill=c)
    # faint blueprint grid
    grid = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    g = ImageDraw.Draw(grid)
    step = 32 * SS
    for x in range(0, w, step):
        g.line([(x, 0), (x, h)], fill=(120, 170, 230, 18), width=SS)
    for y in range(0, h, step):
        g.line([(0, y), (w, y)], fill=(120, 170, 230, 18), width=SS)
    # soft glow behind the cube
    glow = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    r = int(min(w, h) * 0.42)
    cx, cy = int(w * 0.5), int(h * 0.46)
    gd.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(70, 150, 255, 70))
    glow = glow.filter(ImageFilter.GaussianBlur(r // 2))
    img = Image.alpha_composite(img.convert("RGBA"), grid)
    return Image.alpha_composite(img, glow)


def lerp(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)


def inset(poly, f):
    cx = sum(p[0] for p in poly) / len(poly)
    cy = sum(p[1] for p in poly) / len(poly)
    return [(cx + (x - cx) * f, cy + (y - cy) * f) for x, y in poly]


def beam(d, a, b, width):
    d.line([a, b], fill=STEEL_DARK, width=int(width * 1.35))
    d.line([a, b], fill=STEEL, width=width)
    d.line([a, b], fill=STEEL_HI, width=max(1, width // 4))


def screw(d, p, r):
    d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=(120, 130, 145), outline=(70, 78, 92), width=max(1, r // 3))
    d.line([(p[0] - r * 0.6, p[1] - r * 0.6), (p[0] + r * 0.6, p[1] + r * 0.6)], fill=(60, 66, 78), width=max(1, r // 3))


def cube(layer, cx, cy, s):
    d = ImageDraw.Draw(layer)
    k = math.cos(math.radians(30))
    top = (cx, cy - s)
    ur = (cx + s * k, cy - s / 2)
    lr = (cx + s * k, cy + s / 2)
    bot = (cx, cy + s)
    ll = (cx - s * k, cy + s / 2)
    ul = (cx - s * k, cy - s / 2)
    ctr = (cx, cy)

    top_face = [top, ur, ctr, ul]
    left_face = [ul, ctr, bot, ll]
    right_face = [ctr, ur, lr, bot]

    # dark interior visible through the unfinished right face
    d.polygon(right_face, fill=(22, 30, 44))
    # inner bracing on the open face (diagonal struts)
    w_in = int(s * 0.045)
    beam(d, lerp(ctr, ur, 0.0), lerp(bot, lr, 1.0), w_in)

    # finished plastic panels (top + left)
    d.polygon(inset(top_face, 0.86), fill=PLASTIC_TOP)
    d.polygon(inset(left_face, 0.86), fill=PLASTIC_LEFT)

    # right panel sliding into place (partially in, offset up-right)
    off = (s * 0.16, -s * 0.09)
    sliding = [(x + off[0], y + off[1]) for x, y in inset(right_face, 0.86)]
    # motion streaks
    for i, t in enumerate((0.25, 0.5, 0.75)):
        a = lerp(sliding[1], sliding[2], t)
        streak_len = s * (0.55 - 0.1 * i)
        d.line([(a[0] + s * 0.06, a[1] - s * 0.03), (a[0] + streak_len, a[1] - streak_len * 0.56)],
               fill=(150, 220, 255, 190), width=int(s * 0.03))
    d.polygon(sliding, fill=PLASTIC_RIGHT)
    d.line(sliding + [sliding[0]], fill=(235, 245, 255), width=int(s * 0.012))

    # steel frame edges
    w = int(s * 0.075)
    for a, b in [(top, ur), (ur, lr), (lr, bot), (bot, ll), (ll, ul), (ul, top), (ctr, ur), (ctr, ul), (ctr, bot)]:
        beam(d, a, b, w)
    for p in (top, ur, lr, bot, ll, ul, ctr):
        r = w * 0.75
        d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=STEEL)

    # screws on finished panels
    sr = int(s * 0.03)
    for face in (top_face, left_face):
        for p in inset(face, 0.72):
            screw(d, p, sr)
    return {"right_face": right_face, "sliding": sliding}


def screwdriver(layer, tip, angle_deg, length):
    d = ImageDraw.Draw(layer)
    a = math.radians(angle_deg)
    ux, uy = math.cos(a), math.sin(a)
    nx, ny = -uy, ux
    shaft_end = (tip[0] + ux * length * 0.45, tip[1] + uy * length * 0.45)
    handle_end = (tip[0] + ux * length, tip[1] + uy * length)
    sw = length * 0.045
    d.line([tip, shaft_end], fill=(205, 212, 222), width=int(sw))
    d.line([tip, shaft_end], fill=(250, 252, 255), width=max(1, int(sw * 0.3)))
    hw = length * 0.075

    def quad(p0, p1, half):
        return [(p0[0] + nx * half, p0[1] + ny * half), (p1[0] + nx * half, p1[1] + ny * half),
                (p1[0] - nx * half, p1[1] - ny * half), (p0[0] - nx * half, p0[1] - ny * half)]

    collar = (tip[0] + ux * length * 0.5, tip[1] + uy * length * 0.5)
    d.polygon(quad(shaft_end, collar, hw * 0.8), fill=(40, 40, 46))
    d.polygon(quad(collar, handle_end, hw), fill=ORANGE)
    d.line([(collar[0] + nx * hw * 0.45, collar[1] + ny * hw * 0.45),
            (handle_end[0] + nx * hw * 0.45, handle_end[1] + ny * hw * 0.45)], fill=(255, 196, 120), width=int(hw * 0.3))
    for t in (0.62, 0.74, 0.86):
        p = (tip[0] + ux * length * t, tip[1] + uy * length * t)
        d.line([(p[0] + nx * hw, p[1] + ny * hw), (p[0] - nx * hw, p[1] - ny * hw)], fill=ORANGE_DARK, width=int(hw * 0.18))
    r = hw
    d.ellipse([handle_end[0] - r, handle_end[1] - r, handle_end[0] + r, handle_end[1] + r], fill=ORANGE)


def sparkle(d, c, r, color=(255, 255, 255)):
    d.polygon([(c[0], c[1] - r), (c[0] + r * 0.22, c[1] - r * 0.22), (c[0] + r, c[1]), (c[0] + r * 0.22, c[1] + r * 0.22),
               (c[0], c[1] + r), (c[0] - r * 0.22, c[1] + r * 0.22), (c[0] - r, c[1]), (c[0] - r * 0.22, c[1] - r * 0.22)],
              fill=color)


def badge(layer, c, r, text_big, text_small):
    shadow = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow)
    sd.ellipse([c[0] - r, c[1] - r + r * 0.08, c[0] + r, c[1] + r + r * 0.08], fill=(0, 0, 0, 120))
    layer.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(r * 0.08)))
    d = ImageDraw.Draw(layer)
    d.ellipse([c[0] - r, c[1] - r, c[0] + r, c[1] + r], fill=ORANGE, outline=(255, 220, 170), width=int(r * 0.06))
    d.ellipse([c[0] - r * 0.84, c[1] - r * 0.84, c[0] + r * 0.84, c[1] + r * 0.84], outline=(255, 190, 110), width=int(r * 0.03))
    fb = font(int(r * 0.95))
    fs = font(int(r * 0.26))
    d.text((c[0], c[1] - r * 0.08), text_big, font=fb, fill=(255, 255, 255), anchor="mm")
    d.text((c[0], c[1] + r * 0.52), text_small, font=fs, fill=(255, 245, 230), anchor="mm")


def title(d, xy, lines, sizes, colors, anchor="ms"):
    x, y = xy
    for line, size, color in zip(lines, sizes, colors):
        f = font(size)
        d.text((x + size * 0.04, y + size * 0.05), line, font=f, fill=(0, 0, 0, 150), anchor=anchor)
        d.text((x, y), line, font=f, fill=color, anchor=anchor)
        y += int(size * 1.02)


def preview(out):
    W = H = 512 * SS
    img = background(W, H)
    art = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    s = W * 0.25
    cx, cy = W * 0.46, H * 0.40
    geo = cube(art, cx, cy, s)
    # screwdriver pointing at the sliding panel
    tip = lerp(geo["sliding"][1], geo["sliding"][3], 0.45)
    screwdriver(art, tip, 28, W * 0.40)
    d = ImageDraw.Draw(art)
    sparkle(d, (tip[0] - W * 0.015, tip[1] - W * 0.02), W * 0.035)
    sparkle(d, (W * 0.22, H * 0.17), W * 0.018, (190, 230, 255))
    sparkle(d, (W * 0.75, H * 0.62), W * 0.014, (190, 230, 255))
    badge(art, (W * 0.80, H * 0.18), W * 0.135, "5×", "FASTER")
    img = Image.alpha_composite(img, art)

    # title plate
    plate = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    pd = ImageDraw.Draw(plate)
    pd.rectangle([0, H * 0.74, W, H], fill=(8, 12, 22, 200))
    pd.rectangle([0, H * 0.74, W, H * 0.74 + SS * 3], fill=ORANGE + (255,))
    img = Image.alpha_composite(img, plate)
    d = ImageDraw.Draw(img)
    title(d, (W / 2, H * 0.855), ["FASTER FRAME"], [int(W * 0.105)], [(255, 255, 255)], anchor="mm")
    title(d, (W / 2, H * 0.945), ["FINISHING"], [int(W * 0.085)], [ORANGE], anchor="mm")

    img = img.resize((512, 512), Image.LANCZOS).convert("RGB")
    img.save(os.path.join(out, "Preview.png"), optimize=True)


def banner(out):
    W, H = 1280 * SS, 640 * SS
    img = background(W, H)
    art = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    s = H * 0.30
    cx, cy = W * 0.26, H * 0.50
    geo = cube(art, cx, cy, s)
    tip = lerp(geo["sliding"][1], geo["sliding"][3], 0.45)
    screwdriver(art, tip, 32, H * 0.33)
    d = ImageDraw.Draw(art)
    sparkle(d, (tip[0] - H * 0.02, tip[1] - H * 0.03), H * 0.045)
    sparkle(d, (W * 0.10, H * 0.18), H * 0.022, (190, 230, 255))
    badge(art, (W * 0.905, H * 0.19), H * 0.125, "5×", "FASTER")
    img = Image.alpha_composite(img, art)
    d = ImageDraw.Draw(img)
    x = W * 0.535
    title(d, (x, H * 0.47), ["FASTER FRAME", "FINISHING"], [int(H * 0.12), int(H * 0.12)],
          [(255, 255, 255), ORANGE], anchor="ls")
    f = font(int(H * 0.047), "SemiLight Condensed")
    d.text((x, H * 0.74), "Plastic-sheet finishing on steel frames, 5× faster.", font=f, fill=(200, 215, 235), anchor="ls")
    d.text((x, H * 0.81), "Everything else stays vanilla.", font=f, fill=(200, 215, 235), anchor="ls")
    img = img.resize((1280, 640), Image.LANCZOS).convert("RGB")
    img.save(os.path.join(out, "banner.png"), optimize=True)


if __name__ == "__main__":
    out_dir = sys.argv[1]
    os.makedirs(out_dir, exist_ok=True)
    preview(out_dir)
    banner(out_dir)
    print("ok")
