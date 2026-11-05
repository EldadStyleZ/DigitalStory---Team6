"""
Draws the toy's soft glows into Assets/_Project/Art/Sprites, in the same soft, gradient look as the team's drawings.

- RangeGlow.png: how far the flute's music reaches. Clear in the middle, so the Piper and the frogs stay easy to see,
  growing to a soft bright edge at 92% of the picture's half-width, then fading out.
- RangeWave.png: a soft thin ring, the sound wave that rolls out from the Piper while a note is held.
- DiscoLight.png: a soft round light that sits under a frog during Disco.

They are white, so the game tints them with the flute's hole colours. Usage: python3 make_glows.py
"""
import math
import os
import struct
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "_Project", "Art", "Sprites"))

# Where the range glow's edge is drawn, as a share of the picture's half-width. MusicRing uses the same number.
EDGE = 0.92


def smoothstep(edge0, edge1, x):
    t = min(1.0, max(0.0, (x - edge0) / (edge1 - edge0)))
    return t * t * (3.0 - (2.0 * t))


def bump(x, centre, width):
    return math.exp(-((x - centre) / width) ** 2)


def range_glow(r):
    fill = 0.22 * smoothstep(0.25, EDGE, r) ** 2
    edge = 0.7 * bump(r, EDGE, 0.035)
    return (fill + edge) * (1.0 - smoothstep(EDGE + 0.02, 1.0, r))


def range_wave(r):
    return bump(r, EDGE, 0.03) * (1.0 - smoothstep(0.97, 1.0, r))


def disco_light(r):
    return bump(r, 0.0, 0.6) * (1.0 - smoothstep(0.8, 1.0, r))


def write_png(path, size, alpha_at):
    """Writes a white picture whose transparency follows alpha_at(distance from the middle, 0 to 1 at the edge)."""
    half = size / 2.0
    rows = bytearray()
    for y in range(size):
        rows.append(0)
        for x in range(size):
            r = math.hypot(x + 0.5 - half, y + 0.5 - half) / half
            alpha = max(0.0, min(1.0, alpha_at(r))) if r < 1.0 else 0.0
            rows.extend((255, 255, 255, round(alpha * 255)))

    def chunk(kind, data):
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

    header = struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(bytes(rows), 9)) + chunk(b"IEND", b"")
    with open(path, "wb") as file:
        file.write(png)
    print(f"wrote {os.path.relpath(path, os.path.join(HERE, '..', '..'))}")


if __name__ == "__main__":
    write_png(os.path.join(OUT, "RangeGlow.png"), 512, range_glow)
    write_png(os.path.join(OUT, "RangeWave.png"), 512, range_wave)
    write_png(os.path.join(OUT, "DiscoLight.png"), 128, disco_light)
