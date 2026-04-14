from __future__ import annotations

import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


SIZE = 420
COUNT = 320
ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "wwwroot" / "images" / "generated-covers"

PALETTES = [
    ("#101522", "#243b55", "#f6d365", "#ee964b", "#f4f1de"),
    ("#0f172a", "#334155", "#93c5fd", "#fca5a5", "#f8fafc"),
    ("#1b1032", "#5b3f8c", "#ffcad4", "#84dcc6", "#f7f7ff"),
    ("#122620", "#3a6351", "#f2c14e", "#e26d5c", "#f6f7eb"),
    ("#170f1e", "#522b5b", "#ffb997", "#f67e7d", "#fff1e6"),
    ("#111827", "#1f6f8b", "#99f6e4", "#fdbb74", "#f9fafb"),
    ("#20120f", "#6b4423", "#ffd166", "#ef476f", "#f8f9fa"),
    ("#0d1b2a", "#415a77", "#e0e1dd", "#f4a261", "#fefae0"),
    ("#0b132b", "#1c2541", "#5bc0be", "#f4d35e", "#f0f3bd"),
    ("#1a1423", "#372549", "#f7b267", "#f25f5c", "#f7f7ff"),
]


def hex_to_rgb(value: str) -> tuple[int, int, int]:
    value = value.lstrip("#")
    return tuple(int(value[index:index + 2], 16) for index in (0, 2, 4))


def lerp(a: int, b: int, t: float) -> int:
    return round(a + ((b - a) * t))


def blend(c1: tuple[int, int, int], c2: tuple[int, int, int], t: float) -> tuple[int, int, int]:
    return tuple(lerp(a, b, t) for a, b in zip(c1, c2))


def vertical_gradient(top: tuple[int, int, int], bottom: tuple[int, int, int]) -> Image.Image:
    image = Image.new("RGB", (SIZE, SIZE))
    pixels = image.load()
    for y in range(SIZE):
        t = y / (SIZE - 1)
        row = blend(top, bottom, t)
        for x in range(SIZE):
            pixels[x, y] = row
    return image


def radial_glow(base: Image.Image, center: tuple[int, int], radius: int, color: tuple[int, int, int], alpha: int) -> None:
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    x, y = center
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=(*color, alpha))
    layer = layer.filter(ImageFilter.GaussianBlur(radius / 2))
    base.alpha_composite(layer)


def add_noise(base: Image.Image, seed: int, opacity: int = 24) -> None:
    noise = Image.effect_noise((SIZE, SIZE), 14 + (seed % 8)).convert("L")
    noise = noise.point(lambda p: min(255, max(0, p + 112)))
    alpha = noise.point(lambda p: int((p / 255) * opacity))
    layer = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 0))
    layer.putalpha(alpha)
    base.alpha_composite(layer)


def random_polygon(rng: random.Random, cx: int, cy: int, rx: int, ry: int, points: int) -> list[tuple[int, int]]:
    result = []
    for i in range(points):
        angle = (math.pi * 2 * i / points) + rng.uniform(-0.34, 0.34)
        px = int(cx + math.cos(angle) * rx * rng.uniform(0.72, 1.12))
        py = int(cy + math.sin(angle) * ry * rng.uniform(0.72, 1.12))
        result.append((px, py))
    return result


def draw_frame(base: Image.Image, accent: tuple[int, int, int]) -> None:
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    draw.rounded_rectangle((10, 10, SIZE - 10, SIZE - 10), radius=28, outline=(*accent, 76), width=2)
    draw.rounded_rectangle((22, 22, SIZE - 22, SIZE - 22), radius=22, outline=(*accent, 34), width=1)
    base.alpha_composite(layer)


def draw_stars(base: Image.Image, rng: random.Random, color: tuple[int, int, int], limit_y: int = 210) -> None:
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    points = []
    for _ in range(rng.randint(10, 26)):
        x = rng.randint(18, SIZE - 18)
        y = rng.randint(18, limit_y)
        r = rng.uniform(1.0, 2.8)
        draw.ellipse((x - r, y - r, x + r, y + r), fill=(*color, rng.randint(136, 224)))
        points.append((x, y))
    if rng.random() < 0.45:
        for idx in range(1, len(points), 2):
            draw.line((points[idx - 1], points[idx]), fill=(*color, 42), width=1)
    base.alpha_composite(layer)


def draw_mountain_layers(base: Image.Image, rng: random.Random, palette: tuple[tuple[int, int, int], ...]) -> int:
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    horizon = rng.randint(152, 214)
    for band in range(rng.randint(3, 5)):
        y = horizon + (band * rng.randint(24, 38))
        points = [(0, SIZE)]
        step = rng.randint(46, 86)
        x = 0
        while x <= SIZE + step:
            points.append((x, y - rng.randint(20, 70)))
            x += step
        points.extend([(SIZE, SIZE), (0, SIZE)])
        color = palette[(band + 1) % len(palette)]
        draw.polygon(points, fill=(*color, 106 + (band * 20)))
    base.alpha_composite(layer)
    return horizon


def draw_cityline(base: Image.Image, rng: random.Random, palette: tuple[tuple[int, int, int], ...], ground: int) -> None:
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    x = 0
    while x < SIZE:
        width = rng.randint(18, 46)
        height = rng.randint(72, 184)
        y = ground - height
        color = blend(palette[0], palette[1], rng.uniform(0.18, 0.78))
        draw.rectangle((x, y, x + width, ground), fill=(*color, 202))
        for wy in range(y + 12, ground - 8, 14):
            for wx in range(x + 5, x + width - 5, 9):
                if rng.random() < 0.42:
                    draw.rectangle((wx, wy, wx + 3, wy + 5), fill=(*palette[4], 144))
        x += width + rng.randint(4, 10)
    base.alpha_composite(layer)


def draw_road(base: Image.Image, rng: random.Random, accent: tuple[int, int, int]) -> None:
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    top_y = rng.randint(148, 232)
    left = rng.randint(122, 162)
    right = rng.randint(258, 298)
    draw.polygon([(0, SIZE), (SIZE, SIZE), (right, top_y), (left, top_y)], fill=(20, 18, 24, 212))
    for offset in range(7):
        y = SIZE - (offset * 34)
        width = 12 + (offset * 2)
        draw.line([(SIZE // 2, y), (SIZE // 2, max(top_y, y - 18))], fill=(*accent, 138), width=width)
    base.alpha_composite(layer)


def draw_arch_window(base: Image.Image, rng: random.Random, accent: tuple[int, int, int]) -> None:
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    x = rng.randint(44, 96)
    y = rng.randint(40, 96)
    w = rng.randint(230, 310)
    h = rng.randint(176, 250)
    draw.rounded_rectangle((x, y, min(SIZE - 24, x + w), min(SIZE - 40, y + h)), radius=20, outline=(*accent, 118), width=3)
    for offset in range(4):
        inset = 16 + (offset * 18)
        draw.rounded_rectangle((x + inset, y + inset, min(SIZE - 24, x + w - inset), min(SIZE - 40, y + h - inset)), radius=14, outline=(*accent, 44), width=2)
    base.alpha_composite(layer)


def draw_leaf_cluster(base: Image.Image, rng: random.Random, fill: tuple[int, int, int]) -> None:
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    for _ in range(rng.randint(4, 8)):
        stem_x = rng.randint(28, SIZE - 28)
        stem_y = rng.randint(236, 360)
        draw.line((stem_x, SIZE, stem_x, stem_y), fill=(*fill, 152), width=rng.randint(4, 7))
        for side in (-1, 1):
            for leaf in range(rng.randint(2, 4)):
                ly = stem_y + (leaf * 20)
                leaf_w = rng.randint(24, 52)
                leaf_h = rng.randint(12, 24)
                bbox = (stem_x - leaf_w, ly - leaf_h, stem_x + leaf_w, ly + leaf_h)
                start, end = (200, 340) if side < 0 else (20, 160)
                draw.pieslice(bbox, start=start, end=end, fill=(*fill, 118))
    base.alpha_composite(layer)


def draw_person(base: Image.Image, rng: random.Random) -> None:
    if rng.random() >= 0.26:
        return
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    x = rng.randint(116, 304)
    ground = rng.randint(268, 338)
    color = (18, 16, 20, 220)
    draw.ellipse((x - 12, ground - 96, x + 12, ground - 72), fill=color)
    draw.line((x, ground - 70, x, ground - 12), fill=color, width=12)
    draw.line((x, ground - 52, x - 24, ground - 18), fill=color, width=8)
    draw.line((x, ground - 52, x + 24, ground - 20), fill=color, width=8)
    draw.line((x, ground - 12, x - 18, ground + 28), fill=color, width=10)
    draw.line((x, ground - 12, x + 18, ground + 28), fill=color, width=10)
    base.alpha_composite(layer)


def family_landscape(rng: random.Random, palette: tuple[tuple[int, int, int], ...]) -> Image.Image:
    base = vertical_gradient(palette[0], palette[1]).convert("RGBA")
    radial_glow(base, (rng.randint(90, 320), rng.randint(54, 148)), rng.randint(92, 150), palette[2], 96)
    horizon = draw_mountain_layers(base, rng, palette)
    if rng.random() < 0.62:
        water = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
        draw = ImageDraw.Draw(water)
        top = horizon + rng.randint(24, 54)
        draw.rectangle((0, top, SIZE, SIZE), fill=(*palette[1], 88))
        for offset in range(8):
            y = top + (offset * rng.randint(12, 18))
            points = [(x, y + rng.randint(-4, 5)) for x in range(0, SIZE + 30, 34)]
            draw.line(points, fill=(*palette[4], 34), width=2)
        base.alpha_composite(water)
    sun = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(sun)
    x = rng.randint(72, 338)
    y = rng.randint(56, 130)
    r = rng.randint(28, 58)
    draw.ellipse((x - r, y - r, x + r, y + r), fill=(*palette[3], 214))
    base.alpha_composite(sun.filter(ImageFilter.GaussianBlur(0.6)))
    if rng.random() < 0.66:
        draw_leaf_cluster(base, rng, palette[3])
    draw_person(base, rng)
    draw_frame(base, palette[4])
    return base


def family_city(rng: random.Random, palette: tuple[tuple[int, int, int], ...]) -> Image.Image:
    base = vertical_gradient(palette[0], blend(palette[1], palette[2], 0.3)).convert("RGBA")
    radial_glow(base, (rng.randint(120, 300), rng.randint(48, 120)), rng.randint(80, 132), palette[3], 82)
    ground = rng.randint(258, 314)
    if rng.random() < 0.54:
        draw_road(base, rng, palette[4])
    draw_cityline(base, rng, palette, ground)
    draw_stars(base, rng, palette[4], limit_y=180)
    if rng.random() < 0.58:
        window = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
        draw = ImageDraw.Draw(window)
        for _ in range(rng.randint(2, 4)):
            x = rng.randint(40, 260)
            y = rng.randint(28, 190)
            w = rng.randint(68, 142)
            h = rng.randint(82, 156)
            draw.rounded_rectangle((x, y, x + w, y + h), radius=12, fill=(*palette[2], 54), outline=(*palette[4], 42), width=2)
        base.alpha_composite(window.filter(ImageFilter.GaussianBlur(1.1)))
    draw_frame(base, palette[4])
    return base


def family_neon(rng: random.Random, palette: tuple[tuple[int, int, int], ...]) -> Image.Image:
    base = vertical_gradient(blend(palette[0], (0, 0, 0), 0.2), palette[1]).convert("RGBA")
    radial_glow(base, (rng.randint(96, 324), rng.randint(64, 150)), rng.randint(88, 140), palette[2], 86)
    radial_glow(base, (rng.randint(80, 330), rng.randint(120, 250)), rng.randint(90, 154), palette[3], 72)
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    center = (rng.randint(118, 302), rng.randint(106, 238))
    for i in range(rng.randint(3, 5)):
        radius = 40 + (i * rng.randint(22, 36))
        bbox = (center[0] - radius, center[1] - radius, center[0] + radius, center[1] + radius)
        start = rng.randint(0, 160)
        end = start + rng.randint(120, 320)
        draw.arc(bbox, start=start, end=end, fill=(*(palette[2] if i % 2 == 0 else palette[3]), 208), width=max(8, 22 - (i * 3)))
    for _ in range(rng.randint(3, 6)):
        pts = [(rng.randint(10, 120), rng.randint(60, 360))]
        for _ in range(rng.randint(4, 6)):
            pts.append((pts[-1][0] + rng.randint(36, 72), max(24, min(SIZE - 24, pts[-1][1] + rng.randint(-62, 62)))))
        draw.line(pts, fill=(*palette[4], 62), width=rng.randint(2, 4))
    base.alpha_composite(layer.filter(ImageFilter.GaussianBlur(0.8)))
    draw_frame(base, palette[4])
    return base


def family_botanical(rng: random.Random, palette: tuple[tuple[int, int, int], ...]) -> Image.Image:
    base = vertical_gradient(blend(palette[0], palette[1], 0.4), palette[1]).convert("RGBA")
    radial_glow(base, (rng.randint(108, 300), rng.randint(68, 156)), rng.randint(96, 146), palette[4], 70)
    draw_arch_window(base, rng, palette[4])
    draw_leaf_cluster(base, rng, palette[3])
    draw_leaf_cluster(base, rng, palette[2])
    if rng.random() < 0.4:
        draw_person(base, rng)
    draw_frame(base, palette[4])
    return base


def family_minimal(rng: random.Random, palette: tuple[tuple[int, int, int], ...]) -> Image.Image:
    base = vertical_gradient(blend(palette[0], palette[1], 0.5), blend(palette[1], palette[4], 0.08)).convert("RGBA")
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    for _ in range(rng.randint(4, 8)):
        if rng.random() < 0.5:
            x = rng.randint(30, 300)
            y = rng.randint(30, 300)
            w = rng.randint(40, 140)
            h = rng.randint(40, 140)
            draw.rounded_rectangle((x, y, x + w, y + h), radius=rng.randint(10, 24), fill=(*(palette[2] if rng.random() < 0.5 else palette[3]), 118))
        else:
            poly = random_polygon(rng, rng.randint(90, 330), rng.randint(90, 250), rng.randint(32, 96), rng.randint(28, 86), rng.randint(5, 8))
            draw.polygon(poly, fill=(*(palette[2] if rng.random() < 0.5 else palette[4]), 102))
    if rng.random() < 0.7:
        center = (rng.randint(120, 300), rng.randint(100, 220))
        for i in range(rng.randint(2, 4)):
            radius = 36 + (i * rng.randint(16, 32))
            draw.arc((center[0] - radius, center[1] - radius, center[0] + radius, center[1] + radius), start=rng.randint(0, 120), end=rng.randint(180, 340), fill=(*palette[3], 200), width=max(8, 20 - (i * 3)))
    base.alpha_composite(layer.filter(ImageFilter.GaussianBlur(1.1)))
    draw_frame(base, palette[4])
    return base


def family_vinyl(rng: random.Random, palette: tuple[tuple[int, int, int], ...]) -> Image.Image:
    base = vertical_gradient(palette[0], blend(palette[1], palette[2], 0.24)).convert("RGBA")
    radial_glow(base, (rng.randint(90, 330), rng.randint(54, 132)), rng.randint(84, 136), palette[3], 88)
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    cx = rng.randint(122, 296)
    cy = rng.randint(118, 238)
    outer = rng.randint(96, 144)
    draw.ellipse((cx - outer, cy - outer, cx + outer, cy + outer), fill=(20, 18, 24, 228))
    for ring in range(5):
        r = outer - (ring * rng.randint(10, 18))
        draw.ellipse((cx - r, cy - r, cx + r, cy + r), outline=(*palette[4], 36), width=1)
    label = rng.randint(28, 44)
    draw.ellipse((cx - label, cy - label, cx + label, cy + label), fill=(*palette[2], 210))
    hole = rng.randint(6, 10)
    draw.ellipse((cx - hole, cy - hole, cx + hole, cy + hole), fill=(*palette[4], 220))
    if rng.random() < 0.6:
        draw.rectangle((0, cy + rng.randint(20, 60), SIZE, SIZE), fill=(*palette[1], 78))
    base.alpha_composite(layer)
    draw_frame(base, palette[4])
    return base


def family_seaside(rng: random.Random, palette: tuple[tuple[int, int, int], ...]) -> Image.Image:
    base = vertical_gradient(blend(palette[0], palette[1], 0.4), blend(palette[1], palette[4], 0.2)).convert("RGBA")
    sun = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(sun)
    x = rng.randint(82, 336)
    y = rng.randint(62, 128)
    r = rng.randint(32, 60)
    draw.ellipse((x - r, y - r, x + r, y + r), fill=(*palette[2], 220))
    base.alpha_composite(sun.filter(ImageFilter.GaussianBlur(0.4)))
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    horizon = rng.randint(164, 210)
    draw.rectangle((0, horizon, SIZE, SIZE), fill=(*palette[1], 110))
    for band in range(6):
        y = horizon + (band * 20)
        pts = [(px, y + rng.randint(-6, 7)) for px in range(0, SIZE + 28, 28)]
        draw.line(pts, fill=(*palette[4], 44), width=2)
    if rng.random() < 0.68:
        for _ in range(rng.randint(2, 5)):
            sail_x = rng.randint(70, 350)
            sail_y = horizon + rng.randint(20, 70)
            draw.line((sail_x, sail_y - 28, sail_x, sail_y + 20), fill=(*palette[4], 160), width=3)
            draw.polygon([(sail_x, sail_y - 28), (sail_x, sail_y + 4), (sail_x + rng.randint(18, 34), sail_y - 6)], fill=(*palette[3], 160))
    base.alpha_composite(layer)
    draw_frame(base, palette[4])
    return base


def family_poster(rng: random.Random, palette: tuple[tuple[int, int, int], ...]) -> Image.Image:
    base = vertical_gradient(blend(palette[0], palette[1], 0.3), palette[1]).convert("RGBA")
    radial_glow(base, (rng.randint(102, 312), rng.randint(62, 142)), rng.randint(82, 136), palette[2], 84)
    layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    for _ in range(rng.randint(3, 6)):
        x = rng.randint(24, 290)
        y = rng.randint(24, 220)
        w = rng.randint(64, 134)
        h = rng.randint(86, 164)
        draw.rounded_rectangle((x, y, x + w, y + h), radius=rng.randint(10, 18), fill=(*(palette[2] if rng.random() < 0.5 else palette[3]), 84), outline=(*palette[4], 46), width=2)
        poly = random_polygon(rng, x + (w // 2), y + (h // 2), w // 3, h // 3, rng.randint(5, 8))
        draw.polygon(poly, fill=(*palette[4], 48))
    if rng.random() < 0.56:
        draw_arch_window(base, rng, palette[4])
    base.alpha_composite(layer.filter(ImageFilter.GaussianBlur(1.2)))
    draw_frame(base, palette[4])
    return base


FAMILIES = (
    family_landscape,
    family_city,
    family_neon,
    family_botanical,
    family_minimal,
    family_vinyl,
    family_seaside,
    family_poster,
)


def create_cover(index: int) -> Image.Image:
    rng = random.Random(9157 + (index * 17))
    palette = tuple(hex_to_rgb(value) for value in rng.choice(PALETTES))
    family = FAMILIES[index % len(FAMILIES)] if rng.random() < 0.5 else rng.choice(FAMILIES)
    image = family(rng, palette)
    if rng.random() < 0.52:
        draw_stars(image, rng, palette[4], limit_y=180)
    if rng.random() < 0.34:
        draw_person(image, rng)
    add_noise(image, index)
    return image.convert("RGB")


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for path in OUT_DIR.glob("cover-*.png"):
        path.unlink()

    for index in range(COUNT):
        image = create_cover(index)
        image.save(OUT_DIR / f"cover-{index:03}.png", format="PNG", optimize=True)

    print(f"Generated {COUNT} covers in {OUT_DIR}")


if __name__ == "__main__":
    main()
