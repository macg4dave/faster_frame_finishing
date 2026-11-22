"""Build the two 16:9 mod images from the square artwork in docs/images/Preview-original.png.

  About/thumb.png   - uploaded to the Steam Workshop by LaunchPad/the game (must be <= 1 MiB)
  About/Preview.png - shown in the in-game Workshop panel (a 16:9 box that stretches images)

The square art is centred at full height; the sides are a blurred, darkened copy of the same art.
Usage (needs Pillow): python -I tools/make_workshop_images.py
"""
import os

from PIL import Image, ImageEnhance, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "images", "Preview-original.png")
ABOUT = os.path.join(ROOT, "src", "FasterFrameFinishing", "About")


def wide(src, w, h):
    bg = src.resize((w, w), Image.LANCZOS).crop((0, (w - h) // 2, w, (w - h) // 2 + h))
    bg = ImageEnhance.Brightness(bg.filter(ImageFilter.GaussianBlur(w // 40))).enhance(0.45)
    shadow = Image.new("L", (w, h), 0)
    shadow.paste(Image.new("L", (h, h), 170), ((w - h) // 2, 0))
    shadow = shadow.filter(ImageFilter.GaussianBlur(w // 60))
    bg = Image.composite(Image.new("RGB", (w, h), (0, 0, 0)), bg, shadow)
    bg.paste(src.resize((h, h), Image.LANCZOS), ((w - h) // 2, 0))
    return bg


def main():
    src = Image.open(SRC).convert("RGB")
    for name, (w, h) in {"thumb.png": (1152, 648), "Preview.png": (1280, 720)}.items():
        path = os.path.join(ABOUT, name)
        wide(src, w, h).save(path, "PNG", optimize=True)
        print(f"{name}: {w}x{h}, {os.path.getsize(path)} bytes")
    assert os.path.getsize(os.path.join(ABOUT, "thumb.png")) <= 1024 * 1024, "thumb.png must be <= 1 MiB"


if __name__ == "__main__":
    main()
