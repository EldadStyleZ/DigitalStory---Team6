"""
Turns the team's drawings into the toy's sprites in Assets/_Project/Art/Sprites.

The drawings are 9922 by 9922 pixels with most of each one empty, so this trims each to its drawing, turns the
frog and the Piper to face right (the way the game turns its pictures), and shrinks everything to a sensible size.
It also measures where each hole of the flute is, from how halil1 to halil6 differ from halil, and prints the
positions for ToyHud.uss. The shapes the team chose as obstacles become Shape1.png and so on. Last, it traces the
edge of the background's field and prints the points, as shares of the picture, for the Ground's Edge Collider 2D.

Usage: python3 prepare_art.py "<folder with the drawings>"
It needs macOS, because it uses sips to crop, flip and resize.
"""
import math
import os
import struct
import subprocess
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "Sprites"))
PREVIEW = 1000
PADDING = 0.01

# The shapes the team picked to stand on the field as obstacles.
OBSTACLE_SHAPES = [1, 2, 4, 5, 6]

# Output name, source names (one crop shared by all), longest side in pixels, mirror to face right.
SPRITES = [
    ("Piper", ["halilan"], 512, True),
    ("Frog", ["frogg"], 256, True),
    ("Flute", ["halil", "halil1", "halil2", "halil3", "halil4", "halil5", "halil6"], 1024, False),
] + [(f"Shape{n}", [f"shape_{n}"], 512, False) for n in OBSTACLE_SHAPES]

BACKGROUND = ("Background", "background", 2048)


def sips(*args):
    subprocess.run(["sips", *args], check=True, capture_output=True)


def size_of(path):
    out = subprocess.run(["sips", "-g", "pixelWidth", "-g", "pixelHeight", path], check=True, capture_output=True, text=True).stdout
    values = [int(line.split(":")[1]) for line in out.splitlines() if "pixel" in line]
    return values[0], values[1]


def read_bmp(path):
    """Reads the 32-bit BMP that sips writes into rows of (red, green, blue, alpha) tuples."""
    data = open(path, "rb").read()
    offset = struct.unpack("<I", data[10:14])[0]
    width, height = struct.unpack("<ii", data[18:26])
    masks = struct.unpack("<IIII", data[54:70])
    shifts = [(mask & -mask).bit_length() - 1 for mask in masks]
    rows = []
    for y in range(abs(height)):
        start = offset + (y * width * 4)
        row = []
        for x in range(width):
            value = struct.unpack("<I", data[start + (x * 4):start + (x * 4) + 4])[0]
            row.append(tuple((value & mask) >> shift for mask, shift in zip(masks, shifts)))
        rows.append(row)
    # A positive height means the rows are stored from the bottom up.
    return rows if height < 0 else rows[::-1]


def preview(source, work):
    path = os.path.join(work, os.path.basename(source)[:-4] + ".bmp")
    sips("-Z", str(PREVIEW), "-s", "format", "bmp", source, "--out", path)
    return read_bmp(path)


def drawn_box(rows):
    """The smallest box around every pixel that is not see-through, as left, top, right, bottom."""
    xs, ys = [], []
    for y, row in enumerate(rows):
        for x, pixel in enumerate(row):
            if pixel[3] > 8:
                xs.append(x)
                ys.append(y)
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def union(boxes):
    return min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes)


def hole_centers(folder, work, box):
    """Where each pressed hole is drawn, as shares of the trimmed flute's width and height."""
    plain = preview(os.path.join(folder, "halil.png"), work)
    left, top, right, bottom = box
    centers = []
    for key in range(1, 7):
        pressed = preview(os.path.join(folder, f"halil{key}.png"), work)
        xs, ys = [], []
        for y in range(top, bottom):
            for x in range(left, right):
                if sum(abs(a - b) for a, b in zip(plain[y][x], pressed[y][x])) > 60:
                    xs.append(x)
                    ys.append(y)
        cx, cy = sum(xs) / len(xs), sum(ys) / len(ys)
        centers.append(((cx - left) / (right - left), (cy - top) / (bottom - top)))
    return centers


def export(folder, work, name, sources, longest, mirror):
    canvas, _ = size_of(os.path.join(folder, sources[0] + ".png"))
    scale = canvas / PREVIEW
    box = union([drawn_box(preview(os.path.join(folder, s + ".png"), work)) for s in sources])
    pad = int(canvas * PADDING)
    left = max(0, int(box[0] * scale) - pad)
    top = max(0, int(box[1] * scale) - pad)
    right = min(canvas, int(box[2] * scale) + pad)
    bottom = min(canvas, int(box[3] * scale) + pad)

    outputs = [name] if len(sources) == 1 else [name] + [f"{name}Hole{n}" for n in range(1, len(sources))]
    for source, output in zip(sources, outputs):
        path = os.path.join(OUT, output + ".png")
        sips("-c", str(bottom - top), str(right - left), "--cropOffset", str(top), str(left), os.path.join(folder, source + ".png"), "--out", path)
        if mirror:
            sips("-f", "horizontal", path)
        sips("-Z", str(longest), path)
        print(f"{output}: {size_of(path)[0]}x{size_of(path)[1]}")
    return box


def field_outline(folder, work):
    """
    Points on the edge of the field. Every pixel that differs from the border's colour is field; the edge is then
    followed pixel by pixel all the way round, so it goes into the notch, and straight runs are thinned out.
    """
    pixels = preview(os.path.join(folder, BACKGROUND[1] + ".png"), work)
    step = 4
    size = len(pixels) // step
    border = pixels[2][2]

    def is_field(x, y):
        if not (0 <= x < size and 0 <= y < size):
            return False
        pixel = pixels[y * step][x * step]
        return sum(abs(a - b) for a, b in zip(pixel[:3], border[:3])) > 24

    # Start on the field's leftmost pixel in the middle row, with the border to its left.
    row = size // 2
    column = next(x for x in range(size) if is_field(x, row))
    around = [(-1, 0), (-1, -1), (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1)]
    edge, current, came_from = [], (column, row), 0
    while True:
        edge.append(current)
        for turn in range(8):
            direction = (came_from + turn) % 8
            nx, ny = current[0] + around[direction][0], current[1] + around[direction][1]
            if is_field(nx, ny):
                came_from = (direction + 5) % 8
                current = (nx, ny)
                break
        if current == edge[0] and len(edge) > 2:
            break

    def simplify(points, tolerance):
        if len(points) < 3:
            return points
        (x1, y1), (x2, y2) = points[0], points[-1]
        length = math.hypot(x2 - x1, y2 - y1) or 1.0
        farthest, index = 0.0, 0
        for i in range(1, len(points) - 1):
            x, y = points[i]
            distance = abs(((y2 - y1) * x) - ((x2 - x1) * y) + (x2 * y1) - (y2 * x1)) / length
            if distance > farthest:
                farthest, index = distance, i
        if farthest <= tolerance:
            return [points[0], points[-1]]
        return simplify(points[:index + 1], tolerance)[:-1] + simplify(points[index:], tolerance)

    half = len(edge) // 2
    outline = simplify(edge[:half + 1], 0.8)[:-1] + simplify(edge[half:] + [edge[0]], 0.8)[:-1]
    shares = [(round((x + 0.5) / size, 4), round(1 - ((y + 0.5) / size), 4)) for x, y in outline]
    print(f"outline, {len(shares)} points (share across, share up from the bottom):", " ".join(f"{x},{y}" for x, y in shares))


def main():
    folder = sys.argv[1]
    os.makedirs(OUT, exist_ok=True)
    with tempfile.TemporaryDirectory() as work:
        boxes = {name: export(folder, work, name, sources, longest, mirror) for name, sources, longest, mirror in SPRITES}
        name, source, longest = BACKGROUND
        path = os.path.join(OUT, name + ".png")
        sips("-Z", str(longest), os.path.join(folder, source + ".png"), "--out", path)
        print(f"{name}: {size_of(path)[0]}x{size_of(path)[1]}")
        for key, (x, y) in enumerate(hole_centers(folder, work, boxes["Flute"]), start=1):
            print(f"hole {key}: left {x * 100:.1f}% top {y * 100:.1f}%")
        field_outline(folder, work)


if __name__ == "__main__":
    main()
