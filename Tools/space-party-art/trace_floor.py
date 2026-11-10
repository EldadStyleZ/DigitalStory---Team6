"""
Traces the edge of the crater floor in the Space Party Game's Background.png, for the Map's EdgeCollider2D.

The floor is the light, orange and beige area; the rim round it is painted dark red-brown. A pixel counts as floor
when its red plus green is above FLOOR_LIGHTNESS. The tool keeps the one light area that holds the middle of the
picture and fills any holes in it, because darker patches inside the floor are walkable. It pulls the edge in by INSET
pixels so bodies stop just short of the dark paint, follows that edge all the way round, thins out straight runs and
prints the points in world units for a sprite at 100 pixels per unit centred on (0, 0).

Usage: python trace_floor.py [--overlay <out.png>]
--overlay writes a copy of the background with the traced edge drawn on it, to check by eye.
"""
import math
import os
import sys

from prepare_art import OUT, read_png, write_png

FLOOR_LIGHTNESS = 250
STEP = 4
SMOOTHING = 2
INSET = 12
TOLERANCE = 1.5
PIXELS_PER_UNIT = 100


def lightness_grid(width, height, rows):
    """A coarse grid of red plus green, averaged over each STEP by STEP block."""
    columns, lines = width // STEP, height // STEP
    mask = []
    for gy in range(lines):
        line = []
        for gx in range(columns):
            red = green = 0
            for y in range(gy * STEP, (gy + 1) * STEP):
                row = rows[y]
                for x in range(gx * STEP, (gx + 1) * STEP):
                    red += row[x * 4]
                    green += row[(x * 4) + 1]
            line.append((red + green) / (STEP * STEP))
        mask.append(line)
    return mask


def grow(mask, cells, value):
    """Grows the cells equal to value outward by the given number of cells, square by square."""
    lines, columns = len(mask), len(mask[0])
    result = [row[:] for row in mask]
    for _ in range(cells):
        current = [row[:] for row in result]
        for y in range(lines):
            for x in range(columns):
                if current[y][x] == value:
                    continue
                for ny in range(max(0, y - 1), min(lines, y + 2)):
                    if any(current[ny][nx] == value for nx in range(max(0, x - 1), min(columns, x + 2))):
                        result[y][x] = value
                        break
    return result


def flood(mask, start, value):
    """Every cell reachable from start through cells equal to value."""
    lines, columns = len(mask), len(mask[0])
    seen = [[False] * columns for _ in range(lines)]
    stack = [start]
    seen[start[1]][start[0]] = True
    while stack:
        x, y = stack.pop()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < columns and 0 <= ny < lines and not seen[ny][nx] and mask[ny][nx] == value:
                seen[ny][nx] = True
                stack.append((nx, ny))
    return seen


def floor_area(lightness):
    mask = [[value > FLOOR_LIGHTNESS for value in row] for row in lightness]
    lines, columns = len(mask), len(mask[0])
    # Opening: shrink then grow, so thin light streaks into the rim are cut off.
    opened = grow(grow(mask, SMOOTHING, False), SMOOTHING, True)
    centre = (columns // 2, lines // 2)
    if not opened[centre[1]][centre[0]]:
        raise ValueError("the middle of the picture is not floor; check FLOOR_LIGHTNESS")
    floor = flood(opened, centre, True)
    # Fill holes: anything the outside cannot reach is floor.
    padded = [[False] * (columns + 2)] + [[False] + row + [False] for row in floor] + [[False] * (columns + 2)]
    outside = flood(padded, (0, 0), False)
    filled = [[not outside[y + 1][x + 1] for x in range(columns)] for y in range(lines)]
    return grow(filled, max(1, round(INSET / STEP)), False)


def trace(area):
    """Follows the edge of the area once round, starting from its leftmost cell on the middle line."""
    lines, columns = len(area), len(area[0])

    def inside(x, y):
        return 0 <= x < columns and 0 <= y < lines and area[y][x]

    row = lines // 2
    column = next(x for x in range(columns) if inside(x, row))
    around = [(-1, 0), (-1, -1), (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1)]
    edge, current, came_from = [], (column, row), 0
    while True:
        edge.append(current)
        for turn in range(8):
            direction = (came_from + turn) % 8
            nx, ny = current[0] + around[direction][0], current[1] + around[direction][1]
            if inside(nx, ny):
                came_from = (direction + 5) % 8
                current = (nx, ny)
                break
        if current == edge[0] and len(edge) > 2:
            return edge


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


def outline(edge):
    half = len(edge) // 2
    return simplify(edge[:half + 1], TOLERANCE)[:-1] + simplify(edge[half:] + [edge[0]], TOLERANCE)[:-1]


def to_pixels(cell):
    return (cell[0] * STEP) + (STEP / 2), (cell[1] * STEP) + (STEP / 2)


def draw(rows, width, height, points):
    """Draws the closed outline in bright cyan, three pixels thick."""
    pixels = [to_pixels(p) for p in points]
    for (x1, y1), (x2, y2) in zip(pixels, pixels[1:] + pixels[:1]):
        steps = int(max(abs(x2 - x1), abs(y2 - y1))) + 1
        for i in range(steps + 1):
            x = round(x1 + ((x2 - x1) * i / steps))
            y = round(y1 + ((y2 - y1) * i / steps))
            for dy in range(-1, 2):
                for dx in range(-1, 2):
                    if 0 <= x + dx < width and 0 <= y + dy < height:
                        rows[y + dy][(x + dx) * 4:((x + dx) * 4) + 4] = bytes((0, 255, 255, 255))


def main():
    with open(os.path.join(OUT, "Background.png"), "rb") as file:
        width, height, rows = read_png(file.read())
    points = outline(trace(floor_area(lightness_grid(width, height, rows))))
    units = []
    for cell in points:
        x, y = to_pixels(cell)
        units.append((round((x - (width / 2)) / PIXELS_PER_UNIT, 3), round(((height / 2) - y) / PIXELS_PER_UNIT, 3)))
    xs, ys = [p[0] for p in units], [p[1] for p in units]
    print(f"{len(units)} points, x {min(xs)} to {max(xs)}, y {min(ys)} to {max(ys)}")
    print(" ".join(f"{x},{y}" for x, y in units))

    if "--overlay" in sys.argv:
        draw(rows, width, height, points)
        write_png(sys.argv[sys.argv.index("--overlay") + 1], width, height, rows)


if __name__ == "__main__":
    main()
