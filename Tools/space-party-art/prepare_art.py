"""
Turns the team's inGameSprites.zip into the Space Party Game's sprites.

The zip holds seven 1920 by 1080 PNGs with Hebrew names, each mostly empty. This tool:
  1. Copies each original, byte for byte, into Assets/_Project/SpacePartyGame/Sprites/Source/ under its Hebrew name.
  2. Writes an English-named copy of each into Assets/_Project/SpacePartyGame/Sprites/, trimmed to its drawing with
     a small margin, the way Tools/toy-art/prepare_art.py trims the toy's art. Nothing is resized, so every sprite
     keeps the scale it was drawn at on the shared 1920 by 1080 canvas, and one Pixels Per Unit fits them all.
  3. Cuts Cages.png into its six cages, named by colour: CageOrange, CagePink, CageGreen, CageBlue, CageTeal and
     CageViolet.

It prints each output's size, and where it sat on the canvas, so the scene can place things as they were drawn.
It uses only the Python standard library, so it runs on Windows and macOS alike.

Usage: python prepare_art.py "<path to inGameSprites.zip>"
"""
import colorsys
import os
import struct
import sys
import zipfile
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "SpacePartyGame", "Sprites"))
SOURCE = os.path.join(OUT, "Source")

# Hebrew name in the zip, English name for the copy.
NAMES = {
    "אסטרונאוט.png": "Astronaut",
    "כלובים.png": "Cages",
    "יצור 1.png": "Vermin",
    "כמה יצורים.png": "VerminGroup",
    "רקע לבד.png": "Background",
    "רקע עם כלובים.png": "BackgroundWithCages",
    "עיגול.png": "CallRing",
}

# A pixel counts as drawn above this alpha, as in the toy's tool.
SEEN_ALPHA = 8

# The margin left round each drawing, in pixels, so soft edges are not cut off.
MARGIN = 12

# Each cage's name and the hue of its fill, in degrees, measured from Cages.png.
CAGE_HUES = {
    "CageOrange": 29,
    "CagePink": 333,
    "CageGreen": 98,
    "CageBlue": 214,
    "CageTeal": 146,
    "CageViolet": 236,
}


def read_png(data):
    """Decodes an 8-bit RGBA, non-interlaced PNG into its width, height and rows of bytes."""
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG")
    index, chunks = 8, []
    width = height = None
    while index < len(data):
        length, kind = struct.unpack(">I4s", data[index:index + 8])
        body = data[index + 8:index + 8 + length]
        if kind == b"IHDR":
            width, height, depth, colour, _, _, interlace = struct.unpack(">IIBBBBB", body)
            if (depth, colour, interlace) != (8, 6, 0):
                raise ValueError(f"only 8-bit RGBA without interlacing is supported, not {depth}/{colour}/{interlace}")
        elif kind == b"IDAT":
            chunks.append(body)
        index += 12 + length

    raw = zlib.decompress(b"".join(chunks))
    stride = width * 4
    rows, previous = [], bytearray(stride)
    for y in range(height):
        start = y * (stride + 1)
        kind = raw[start]
        row = bytearray(raw[start + 1:start + 1 + stride])
        unfilter(kind, row, previous)
        rows.append(row)
        previous = row
    return width, height, rows


def unfilter(kind, row, previous):
    if kind == 0:
        return
    if kind == 1:
        for i in range(4, len(row)):
            row[i] = (row[i] + row[i - 4]) & 0xFF
    elif kind == 2:
        for i in range(len(row)):
            row[i] = (row[i] + previous[i]) & 0xFF
    elif kind == 3:
        for i in range(len(row)):
            left = row[i - 4] if i >= 4 else 0
            row[i] = (row[i] + ((left + previous[i]) >> 1)) & 0xFF
    elif kind == 4:
        for i in range(len(row)):
            a = row[i - 4] if i >= 4 else 0
            b = previous[i]
            c = previous[i - 4] if i >= 4 else 0
            p = a + b - c
            pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
            nearest = a if pa <= pb and pa <= pc else (b if pb <= pc else c)
            row[i] = (row[i] + nearest) & 0xFF
    else:
        raise ValueError(f"unknown PNG filter {kind}")


def write_png(path, width, height, rows):
    def chunk(kind, body):
        return struct.pack(">I", len(body)) + kind + body + struct.pack(">I", zlib.crc32(kind + body) & 0xFFFFFFFF)

    raw = b"".join(b"\x00" + bytes(row) for row in rows)
    header = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    with open(path, "wb") as file:
        file.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def drawn_box(rows, box=None):
    """The smallest box round every drawn pixel inside box, as left, top, right and bottom."""
    left, top, right, bottom = box or (0, 0, len(rows[0]) // 4, len(rows))
    xs, ys = [], []
    for y in range(top, bottom):
        row = rows[y]
        for x in range(left, right):
            if row[(x * 4) + 3] > SEEN_ALPHA:
                xs.append(x)
                ys.append(y)
    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def padded(box, width, height):
    left, top, right, bottom = box
    return max(0, left - MARGIN), max(0, top - MARGIN), min(width, right + MARGIN), min(height, bottom + MARGIN)


def crop(rows, box):
    left, top, right, bottom = box
    return [row[left * 4:right * 4] for row in rows[top:bottom]]


def export(name, rows, box, width, height):
    left, top, right, bottom = padded(box, width, height)
    write_png(os.path.join(OUT, name + ".png"), right - left, bottom - top, crop(rows, (left, top, right, bottom)))
    centre_x = (left + right) / 2
    centre_y = (top + bottom) / 2
    print(f"{name}: {right - left}x{bottom - top}, centre at ({centre_x:.0f}, {centre_y:.0f}) on the canvas")


def separate_drawings(rows, width, height, step=4):
    """Boxes round each separate drawing, found on a coarse grid of the alpha so it stays quick."""
    columns, lines = width // step, height // step
    drawn = [[rows[y * step][(x * step * 4) + 3] > SEEN_ALPHA for x in range(columns)] for y in range(lines)]
    seen = [[False] * columns for _ in range(lines)]
    boxes = []
    for y in range(lines):
        for x in range(columns):
            if not drawn[y][x] or seen[y][x]:
                continue
            stack, xs, ys = [(x, y)], [], []
            seen[y][x] = True
            while stack:
                cx, cy = stack.pop()
                xs.append(cx)
                ys.append(cy)
                for nx in range(cx - 2, cx + 3):
                    for ny in range(cy - 2, cy + 3):
                        if 0 <= nx < columns and 0 <= ny < lines and drawn[ny][nx] and not seen[ny][nx]:
                            seen[ny][nx] = True
                            stack.append((nx, ny))
            if len(xs) > 50:
                boxes.append((min(xs) * step, min(ys) * step, min(width, (max(xs) + 1) * step), min(height, (max(ys) + 1) * step)))
    return boxes


def hue_of(rows, box):
    """The average hue, in degrees, of the strongly drawn pixels in a box."""
    red = green = blue = count = 0
    left, top, right, bottom = box
    for y in range(top, bottom, 2):
        row = rows[y]
        for x in range(left, right, 2):
            if row[(x * 4) + 3] > 200:
                red += row[x * 4]
                green += row[(x * 4) + 1]
                blue += row[(x * 4) + 2]
                count += 1
    hue, _, _ = colorsys.rgb_to_hsv(red / count / 255, green / count / 255, blue / count / 255)
    return hue * 360


def hue_gap(a, b):
    gap = abs(a - b) % 360
    return min(gap, 360 - gap)


def name_cages(rows, boxes):
    """Gives each cage the colour name whose hue is nearest, and stops if two cages want the same name."""
    names = {}
    for box in boxes:
        hue = hue_of(rows, box)
        name = min(CAGE_HUES, key=lambda candidate: hue_gap(CAGE_HUES[candidate], hue))
        print(f"cage at {box}: hue {hue:.0f}, named {name}")
        if name in names:
            raise ValueError(f"two cages are both nearest to {name}; check CAGE_HUES")
        names[name] = box
    return names


def hebrew_name(info):
    """Zip tools that do not flag UTF-8 names store them as CP437, so the Hebrew is read back from that."""
    if info.flag_bits & 0x800:
        return info.filename
    return info.filename.encode("cp437").decode("utf-8")


def main():
    archive = zipfile.ZipFile(sys.argv[1])
    os.makedirs(SOURCE, exist_ok=True)
    found = set()
    for info in archive.infolist():
        name = hebrew_name(info)
        if name not in NAMES:
            raise ValueError(f"unexpected file in the zip: {name}")
        found.add(name)
        data = archive.read(info)
        with open(os.path.join(SOURCE, name), "wb") as file:
            file.write(data)

        english = NAMES[name]
        width, height, rows = read_png(data)
        export(english, rows, drawn_box(rows), width, height)

        if english == "Cages":
            boxes = separate_drawings(rows, width, height)
            if len(boxes) != len(CAGE_HUES):
                raise ValueError(f"expected {len(CAGE_HUES)} cages, found {len(boxes)}")
            for cage, box in name_cages(rows, boxes).items():
                export(cage, rows, drawn_box(rows, box), width, height)

    missing = set(NAMES) - found
    if missing:
        raise ValueError(f"missing from the zip: {', '.join(sorted(missing))}")


if __name__ == "__main__":
    main()
