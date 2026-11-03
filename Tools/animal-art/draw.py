"""
Draws the lab's nine animals seen from above, facing east, as SVG: a rest pose and four walk frames each.
Frames 1 and 3 have one diagonal pair of legs forward, frames 2 and 4 have the legs passing.
Each drawing goes in an HTML page of its own, which render.sh turns into a transparent PNG.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
CREAM = "#FFF4DC"
INK = "#22202C"
SWING = [1, 0, -1, 0]


# ---------- shapes ----------

def ellipse(cx, cy, rx, ry, turn=0):
    spin = f' transform="rotate({turn:.1f} {cx:.1f} {cy:.1f})"' if turn else ""
    return f'<ellipse cx="{cx:.1f}" cy="{cy:.1f}" rx="{rx:.1f}" ry="{ry:.1f}"{spin}'


def circle(cx, cy, r):
    return f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{r:.1f}"'


def path(d):
    return f'<path d="{d}"'


def poly(points):
    return path("M " + " L ".join(f"{x:.1f} {y:.1f}" for x, y in points) + " Z")


def spline(points, steps=14):
    pts = [points[0]] + list(points) + [points[-1]]
    out = []
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        for s in range(steps):
            t = s / steps
            out.append(tuple(
                0.5 * ((2 * p1[k]) + (-p0[k] + p2[k]) * t
                       + (2 * p0[k] - 5 * p1[k] + 4 * p2[k] - p3[k]) * t * t
                       + (-p0[k] + 3 * p1[k] - 3 * p2[k] + p3[k]) * t * t * t)
                for k in (0, 1)))
    out.append(points[-1])
    return out


def ribbon(points, start, end, bulge=0.0):
    """A tapered band along a curve. Bulge fattens its middle, for a bushy tail."""
    line = spline(points)
    left, right = [], []
    for i, (x, y) in enumerate(line):
        a = line[max(i - 1, 0)]
        b = line[min(i + 1, len(line) - 1)]
        dx, dy = b[0] - a[0], b[1] - a[1]
        length = math.hypot(dx, dy) or 1
        share = i / (len(line) - 1)
        half = (start + (end - start) * share + bulge * math.sin(math.pi * share)) / 2
        left.append((x - dy / length * half, y + dx / length * half))
        right.append((x + dy / length * half, y - dx / length * half))
    return poly(left + right[::-1])


def mirror_y(points):
    return [(x, 512 - y) for x, y in points]


def flip(shape):
    return f'<g transform="translate(0 512) scale(1 -1)">{shape}/></g>'


def part(shape, color):
    return (shape, color)


def closed(shape):
    return shape if shape.endswith("</g>") else shape + "/>"


def fill(shape, color, opacity=1.0):
    extra = f' opacity="{opacity}"' if opacity != 1 else ""
    s = closed(shape)
    for tag in ("<ellipse", "<circle", "<path"):
        s = s.replace(tag, f'{tag} fill="{color}"{extra}')
    return s


def stroke(d, color, width, opacity=1.0):
    extra = f' opacity="{opacity}"' if opacity != 1 else ""
    return f'<path d="{d}" fill="none" stroke="{color}" stroke-width="{width}" stroke-linecap="round" stroke-linejoin="round"{extra}/>'


def eye(x, y, r, iris=None):
    """An eye with a glint, so it still reads when small."""
    body = f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{r:.1f}" fill="{iris or INK}"/>'
    if iris:
        body += f'<circle cx="{x + r * 0.2:.1f}" cy="{y:.1f}" r="{r * 0.55:.1f}" fill="{INK}"/>'
    return body + f'<circle cx="{x + r * 0.35:.1f}" cy="{y - r * 0.35:.1f}" r="{r * 0.32:.1f}" fill="#FFFFFF"/>'


# The shading only depends on the outline, never on a direction, because the picture turns with the animal.
FILTERS = '''
<filter id="rim" x="-10%" y="-10%" width="120%" height="120%">
  <feGaussianBlur in="SourceAlpha" stdDeviation="13" result="blur"/>
  <feComposite in="SourceAlpha" in2="blur" operator="arithmetic" k2="1" k3="-1" result="edge"/>
  <feFlood flood-color="#000000" flood-opacity="0.42"/>
  <feComposite in2="edge" operator="in"/>
</filter>
<filter id="dome" x="-10%" y="-10%" width="120%" height="120%">
  <feGaussianBlur in="SourceAlpha" stdDeviation="30" result="blur"/>
  <feComponentTransfer in="blur" result="core"><feFuncA type="gamma" amplitude="1" exponent="4" offset="0"/></feComponentTransfer>
  <feComposite in="core" in2="SourceAlpha" operator="in" result="lit"/>
  <feFlood flood-color="#FFFFFF" flood-opacity="0.30"/>
  <feComposite in2="lit" operator="in"/>
</filter>'''


def build(parts, pattern="", face=""):
    """Parts make the outline; the pattern is shaded with them, and the face sits on top, crisp."""
    silhouette = "".join(closed(shape) for shape, _ in parts)
    colored = "".join(fill(shape, color) for shape, color in parts)
    return f'''<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 512 512" width="512" height="512">
<defs><g id="s">{silhouette}</g>{FILTERS}</defs>
<g transform="translate(256 256) scale(0.92) translate(-256 -256)">
<use href="#s" fill="{CREAM}" stroke="{CREAM}" stroke-width="24" stroke-linejoin="round"/>
<use href="#s" fill="{INK}" stroke="{INK}" stroke-width="12" stroke-linejoin="round"/>
{colored}
{pattern}
<use href="#s" filter="url(#rim)"/>
<use href="#s" filter="url(#dome)"/>
{face}
</g>
</svg>'''


# ---------- legs and tails ----------

def quad_legs(front_x, hind_x, front_out, hind_out, swing, stride, leg, paw, front_paw, hind_paw, width=(30, 22)):
    """
    Four legs seen from above. Each paw peeks out beside the body and the diagonal pairs swing together:
    front-left with hind-right, front-right with hind-left.
    """
    out = []
    for x, spread, sign, paw_size in ((front_x, front_out, 1, front_paw), (hind_x, hind_out, -1, hind_paw)):
        for side in (-1, 1):
            pair = 1 if (side == -1) == (sign == 1) else -1
            foot_x = x + pair * swing * stride
            hip = (x - pair * swing * stride * 0.3, 256 + side * spread * 0.35)
            foot = (foot_x, 256 + side * spread)
            mid = ((hip[0] + foot[0]) / 2, (hip[1] + foot[1]) / 2)
            out.append(part(ribbon([hip, mid, foot], *width), leg))
            out.append(part(ellipse(foot[0], foot[1], paw_size[0], paw_size[1]), paw))
    return out


def tail_points(root, length, curl, sway, droop=0.0):
    """A tail growing west from its root, swaying sideways more toward its tip."""
    x, y = root
    return [
        (x, y),
        (x - length * 0.35, y + sway * 0.25 + curl * 0.15),
        (x - length * 0.7, y + sway * 0.6 + curl * 0.5),
        (x - length, y + sway + curl + droop),
    ]


# ---------- the animals ----------

def mouse(frame):
    fur, pale, pink = "#D5C8B4", "#EFE6D6", "#F0A6A0"
    s = SWING[frame] if frame is not None else 0
    sway = [14, 0, -14, 0][frame] if frame is not None else 0
    feet = []
    for x, y, turn, pair in ((150, 160, -30, -1), (292, 172, 30, 1)):
        feet.append(part(ellipse(x + pair * s * 16, y, 26 if x < 200 else 22, 19 if x < 200 else 16, turn), pink))
        feet.append(part(ellipse(x - pair * s * 16, 512 - y, 26 if x < 200 else 22, 19 if x < 200 else 16, -turn), pink))
    parts = [part(ribbon([(120, 256), (70, 268 + sway * 0.3), (38, 240 + sway), (14, 190 + sway * 1.8)], 18, 7), pink)] + feet + [
        part(ellipse(205, 256, 112, 92), fur),
        part(circle(318, 166, 58), fur), part(flip(circle(318, 166, 58)), fur),
        part(path("M 262 256 C 262 188 345 172 392 208 C 428 232 470 246 476 256 C 470 266 428 280 392 304 C 345 340 262 324 262 256 Z"), fur),
    ]
    pattern = (
        fill(circle(318, 166, 36), pink) + fill(flip(circle(318, 166, 36)), pink)
        + fill(ellipse(190, 222, 72, 30, -6), pale, 0.75)
    )
    face = (
        fill(circle(474, 256, 15), "#D9636B")
        + eye(392, 222, 14) + eye(392, 290, 14)
        + stroke("M 430 232 L 462 196", INK, 4) + stroke("M 430 280 L 462 316", INK, 4)
    )
    return build(parts, pattern, face)


def rat(frame):
    fur, dark, pink = "#8E6A4B", "#6E4F36", "#E8A0A0"
    s = SWING[frame] if frame is not None else 0
    sway = [16, 0, -16, 0][frame] if frame is not None else 0
    feet = []
    for x, y, turn, rx, ry, pair in ((124, 158, -30, 28, 19, -1), (300, 170, 30, 24, 16, 1)):
        feet.append(part(ellipse(x + pair * s * 16, y, rx, ry, turn), pink))
        feet.append(part(ellipse(x - pair * s * 16, 512 - y, rx, ry, -turn), pink))
    parts = [part(ribbon([(84, 256), (44, 276 + sway * 0.3), (20, 322 + sway), (28, 386 + sway * 1.4)], 24, 8), pink)] + feet + [
        part(ellipse(208, 256, 132, 84), fur),
        part(circle(338, 182, 34), fur), part(flip(circle(338, 182, 34)), fur),
        part(path("M 270 256 C 270 190 350 182 400 214 C 440 236 488 248 496 256 C 488 264 440 276 400 298 C 350 330 270 322 270 256 Z"), fur),
    ]
    pattern = (
        fill(circle(338, 182, 19), pink) + fill(flip(circle(338, 182, 19)), pink)
        + fill(ellipse(205, 256, 100, 58), dark, 0.65)
    )
    face = (
        fill(circle(205, 256, 56), CREAM)
        + '<rect x="165" y="241" width="80" height="30" rx="7" fill="#29B765"/><rect x="190" y="216" width="30" height="80" rx="7" fill="#29B765"/>'
        + fill(circle(494, 256, 12), "#D9636B")
        + eye(404, 226, 12) + eye(404, 286, 12)
    )
    return build(parts, pattern, face)


def beetle(frame):
    shell, dark, leg = "#2FA866", "#1D7047", "#14432C"
    s = SWING[frame] if frame is not None else 0
    # Insects walk on tripods: front and back legs on one side move with the middle leg on the other.
    legs = [
        ([(310, 170), (350, 112), (400, 92)], 1),
        ([(240, 150), (240, 92), (200, 56)], -1),
        ([(160, 160), (120, 110), (70, 92)], 1),
    ]
    parts = []
    for points, pair in legs:
        for side in (1, -1):
            shift = pair * side * s * 20
            moved = [points[0], (points[1][0] + shift * 0.5, points[1][1]), (points[2][0] + shift, points[2][1])]
            if side == -1:
                moved = mirror_y(moved)
            parts.append(part(ribbon(moved, 20, 10), leg))
    parts += [
        part(ribbon([(420, 226), (462, 196), (500, 204)], 12, 6), leg),
        part(flip(ribbon([(420, 226), (462, 196), (500, 204)], 12, 6)), leg),
        part(ellipse(214, 256, 172, 122), shell),
        part(ellipse(346, 256, 58, 88), dark),
        part(ellipse(412, 256, 44, 56), leg),
    ]
    pattern = (
        stroke("M 48 256 L 296 256", INK, 9)
        + fill(ellipse(170, 196, 104, 28, 4), "#8CE8AE", 0.75)
        + fill(ellipse(170, 316, 90, 20, -4), "#8CE8AE", 0.35)
        + fill(ellipse(346, 226, 26, 12, 80), "#5FC98D", 0.8)
    )
    face = eye(432, 226, 12, "#FFE27A") + eye(432, 286, 12, "#FFE27A")
    return build(parts, pattern, face)


def cat(frame):
    fur, stripe, pale, pink = "#EC9638", "#B4601C", "#FFE0B2", "#F4A3A0"
    s = SWING[frame] if frame is not None else 0
    sway = [-30, 0, 30, 0][frame] if frame is not None else 0
    tail = tail_points((128, 256), 120, -40, sway)
    parts = [part(ribbon(tail, 30, 20), fur)]
    parts += quad_legs(330, 160, 74, 70, s, 30, fur, pale, (22, 17), (26, 20))
    parts += [
        part(ellipse(228, 256, 118, 66), fur),
        part(ellipse(312, 256, 46, 70), fur),
        part(poly([(352, 214), (362, 166), (402, 206)]), fur),
        part(poly(mirror_y([(352, 214), (362, 166), (402, 206)])), fur),
        part(ellipse(392, 256, 62, 60), fur),
        part(ellipse(446, 256, 26, 30), pale),
    ]
    bars = "".join(stroke(f"M {x} {256 - w} Q {x - 14} 256 {x} {256 + w}", stripe, 14) for x, w in ((150, 52), (196, 62), (242, 62), (288, 58)))
    pattern = (
        f'<clipPath id="back"><ellipse cx="228" cy="256" rx="118" ry="66"/><ellipse cx="312" cy="256" rx="46" ry="70"/></clipPath>'
        + f'<g clip-path="url(#back)">{bars}</g>'
        + stroke(f"M {tail[2][0]:.0f} {tail[2][1] - 14:.0f} L {tail[2][0]:.0f} {tail[2][1] + 14:.0f}", stripe, 12)
        + stroke(f"M {tail[1][0]:.0f} {tail[1][1] - 15:.0f} L {tail[1][0]:.0f} {tail[1][1] + 15:.0f}", stripe, 12)
        + fill(poly([(360, 206), (366, 178), (390, 204)]), pink) + fill(poly(mirror_y([(360, 206), (366, 178), (390, 204)])), pink)
        + stroke("M 372 228 L 352 238 M 372 284 L 352 274", stripe, 9)
    )
    face = (
        eye(416, 228, 16, "#A6E36B") + eye(416, 284, 16, "#A6E36B")
        + fill(poly([(462, 246), (474, 256), (462, 266)]), "#E0686E")
        + stroke("M 456 236 L 492 222 M 456 276 L 492 290", CREAM, 3, 0.8)
    )
    return build(parts, pattern, face)


def owl(frame):
    back, wing, light, disc, spot = "#9A6A3E", "#7A5130", "#C99A62", "#E8CFA0", "#F4E4C4"
    parts = []
    feathers = ""
    if frame is None:
        # Folded wings wrap its round body and cross over the tail, as an owl's do at rest.
        for side in (-1, 1):
            folded = [(330, 256 + side * 70), (290, 256 + side * 104), (200, 256 + side * 104), (130, 256 + side * 60), (96, 256 - side * 14), (140, 256 - side * 6), (230, 256 + side * 20), (320, 256 + side * 30)]
            parts.append(part(path("M " + " L ".join(f"{x:.1f} {y:.1f}" for x, y in spline(folded, 6)) + " Z"), wing))
            for share in (0.3, 0.55, 0.8):
                x = 300 - share * 170
                feathers += stroke(f"M {x + 24:.0f} {256 + side * (96 - share * 30):.0f} Q {x:.0f} {256 + side * (70 - share * 30):.0f} {x - 26:.0f} {256 + side * (34 - share * 20):.0f}", light, 6, 0.85)
    else:
        # In the air the wings beat: spread, half, lifted and narrow, half. The long feathers at each tip part like fingers.
        reach = [236, 172, 104, 172][frame]
        sweep = [0, 26, 60, 26][frame]
        for side in (-1, 1):
            tip = 256 + side * reach
            outline = [(330, 256 + side * 50), (316, 256 + side * reach * 0.5), (282 - sweep, tip)]
            for finger in range(4):
                x = 262 - sweep - finger * 22
                outline += [(x, tip - side * finger * 10), (x - 8, tip - side * (finger * 10 + 22))]
            outline += [(170 - sweep, 256 + side * reach * 0.62), (168, 256 + side * 50)]
            parts.append(part(poly(spline(outline, 3)), wing))
            for share in (0.4, 0.6, 0.8):
                feathers += stroke(f"M {300 - sweep * share:.0f} {256 + side * 56:.0f} L {236 - sweep * share:.0f} {256 + side * reach * share:.0f}", light, 6, 0.85)
    tail_width = 44 if frame is None else 64
    parts += [
        part(path(f"M 150 256 C 130 {256 - tail_width} 92 {256 - tail_width} 70 {256 - tail_width * 0.6} C 80 252 80 260 70 {256 + tail_width * 0.6} C 92 {256 + tail_width} 130 {256 + tail_width} 150 256 Z"), wing),
        part(ellipse(236, 256, 104, 92), back),
        part(poly([(380, 186), (392, 146), (414, 192)]), back),
        part(poly(mirror_y([(380, 186), (392, 146), (414, 192)])), back),
        part(circle(358, 256, 90), back),
    ]
    spots = "".join(fill(circle(x, y, 6), spot, 0.85) for x, y in [(206, 236), (236, 250), (206, 276), (266, 236), (266, 276), (236, 262), (298, 246), (298, 266)])
    pattern = (
        spots + feathers
        + stroke("M 86 246 L 132 252 M 86 266 L 132 260", light, 5, 0.7)
        + fill(path("M 356 256 C 356 188 402 168 438 188 C 466 210 466 302 438 324 C 402 344 356 324 356 256 Z"), disc)
        + stroke("M 300 212 Q 320 256 300 300", light, 6, 0.5)
    )
    face = (
        fill(circle(414, 220, 32), CREAM) + fill(circle(414, 292, 32), CREAM)
        + eye(420, 220, 23, "#FFB52E") + eye(420, 292, 23, "#FFB52E")
        + fill(poly([(446, 240), (476, 256), (446, 272)]), "#5A4630")
    )
    return build(parts, pattern, face)


def skunk(frame):
    black, white, pink = "#2E2B3A", "#F5F1E6", "#E48A95"
    s = SWING[frame] if frame is not None else 0
    sway = [-34, 0, 34, 0][frame] if frame is not None else 0
    # The plume is nearly as big as the body, which is what makes a skunk read as a skunk.
    plume = tail_points((168, 256), 140, 0, sway)
    parts = [part(ribbon(plume, 92, 76, bulge=118), black)]
    parts += quad_legs(320, 182, 70, 70, s, 22, black, black, (20, 15), (24, 18))
    parts += [
        part(ellipse(250, 256, 104, 70), black),
        part(circle(372, 210, 20), black), part(circle(372, 302, 20), black),
        part(ellipse(378, 256, 56, 50), black),
        part(path("M 400 222 C 436 226 470 246 486 256 C 470 266 436 286 400 290 Z"), black),
    ]
    pattern = (
        fill(ribbon([(476, 256), (430, 256), (392, 256)], 6, 20), white)
        + fill(ribbon([(392, 256), (340, 236), (270, 228), (190, 244)], 22, 18), white)
        + fill(ribbon(mirror_y([(392, 256), (340, 236), (270, 228), (190, 244)]), 22, 18), white)
        + fill(ribbon(plume, 20, 8, bulge=30), white, 0.95)
        + fill(ribbon(plume[1:], 10, 4, bulge=8), "#D9D3C4", 0.8)
        + fill(circle(372, 210, 10), "#5A5568") + fill(circle(372, 302, 10), "#5A5568")
    )
    face = (
        fill(circle(486, 256, 10), pink)
        + eye(424, 232, 11, "#7A5A3A") + eye(424, 280, 11, "#7A5A3A")
    )
    return build(parts, pattern, face)


def hornet(frame):
    yellow, black, wing = "#FFD83D", "#2A2733", "#CFEAFF"
    s = SWING[frame] if frame is not None else 0
    legs = [
        ([(300, 200), (312, 150), (352, 118)], 1),
        ([(268, 196), (252, 142), (214, 110)], -1),
        ([(330, 214), (372, 180), (420, 170)], 1),
    ]
    parts = []
    for points, pair in legs:
        for side in (1, -1):
            shift = pair * side * s * 10
            moved = [points[0], (points[1][0] + shift * 0.5, points[1][1]), (points[2][0] + shift, points[2][1])]
            if side == -1:
                moved = mirror_y(moved)
            parts.append(part(ribbon(moved, 14, 7), black))
    # Its wings beat as it moves: forward and long, back, lifted and short, back.
    big_turn, big_length = (22, 150) if frame is None else ([6, 24, 44, 24][frame], [150, 136, 104, 136][frame])
    small_turn, small_length = (8, 112) if frame is None else ([-4, 10, 26, 10][frame], [112, 100, 80, 100][frame])
    wing_big = ellipse(330 - big_length, 136 + (22 - big_turn) * 1.2, big_length, 46, big_turn)
    wing_small = ellipse(326 - small_length, 182, small_length, 30, small_turn)
    parts += [
        part(ribbon([(400, 228), (436, 192), (480, 186)], 12, 6), black),
        part(flip(ribbon([(400, 228), (436, 192), (480, 186)], 12, 6)), black),
        part(wing_big, wing), part(flip(wing_big), wing),
        part(wing_small, wing), part(flip(wing_small), wing),
        part(poly([(74, 234), (8, 256), (74, 278)]), black),
        part(ellipse(160, 256, 112, 68), yellow),
        part(ellipse(294, 256, 60, 62), "#C9861E"),
        part(ellipse(378, 256, 46, 58), yellow),
    ]
    bands = "".join(f'<rect x="{x}" y="170" width="{w}" height="172" fill="{black}"/>' for x, w in [(66, 26), (116, 30), (172, 30), (226, 24)])
    pattern = (
        f'<clipPath id="belly"><ellipse cx="160" cy="256" rx="112" ry="68"/></clipPath><g clip-path="url(#belly)">{bands}</g>'
        + fill(ellipse(150, 214, 76, 11), "#FFFFFF", 0.5)
        + fill(ellipse(294, 232, 36, 16), "#F0B64A", 0.8)
        + stroke(f"M {330 - big_length * 1.6:.0f} {150 + (22 - big_turn) * 2:.0f} L 300 200", "#9FC4E0", 4, 0.8)
        + stroke(f"M {330 - big_length * 1.6:.0f} {362 - (22 - big_turn) * 2:.0f} L 300 312", "#9FC4E0", 4, 0.8)
    )
    face = (
        fill(ellipse(392, 214, 26, 20, 20), black) + fill(flip(ellipse(392, 214, 26, 20, 20)), black)
        + fill(circle(400, 208, 6), "#FFFFFF") + fill(circle(400, 304, 6), "#FFFFFF")
    )
    return build(parts, pattern, face)


def bear(frame):
    fur, dark, hump, muzzle = "#7B4A27", "#55321A", "#96603A", "#D6B080"
    s = SWING[frame] if frame is not None else 0
    parts = [part(circle(58, 256, 24), fur)]
    parts += quad_legs(334, 140, 120, 118, s, 34, fur, dark, (40, 30), (52, 40))
    parts += [
        part(ellipse(214, 256, 152, 112), fur),
        part(ellipse(310, 256, 66, 104), fur),
        part(circle(380, 192, 26), fur), part(circle(380, 320, 26), fur),
        part(ellipse(404, 256, 70, 64), fur),
        part(ellipse(458, 256, 36, 32), muzzle),
    ]
    claws = ""
    # Claws sit at the front of each paw, wherever the paw has swung to.
    for x, spread, sign in ((334, 120, 1), (140, 118, -1)):
        for side in (-1, 1):
            pair = 1 if (side == -1) == (sign == 1) else -1
            foot_x = x + pair * s * 34
            foot_y = 256 + side * spread
            for offset in (-14, 0, 14):
                claws += stroke(f"M {foot_x + 30:.0f} {foot_y + offset:.0f} L {foot_x + 46:.0f} {foot_y + offset * 1.2:.0f}", CREAM, 6)
    tufts = "".join(stroke(f"M {x} {256 + side * w} Q {x - 16} {256 + side * (w + 10)} {x - 30} {256 + side * w}", dark, 7, 0.7)
                    for x, w in ((180, 60), (240, 70), (120, 40)) for side in (-1, 1))
    pattern = (
        fill(ellipse(300, 256, 54, 82), hump, 0.85)
        + tufts
        + fill(circle(380, 192, 13), dark) + fill(circle(380, 320, 13), dark)
        + claws
    )
    face = (
        fill(ellipse(484, 256, 12, 18), INK)
        + stroke("M 470 256 L 446 256", dark, 5)
        + eye(424, 222, 13) + eye(424, 290, 13)
    )
    return build(parts, pattern, face)


def wolf(frame):
    fur, saddle, pale = "#9EA9B6", "#5B6676", "#E4E9EF"
    s = SWING[frame] if frame is not None else 0
    sway = [-30, 0, 30, 0][frame] if frame is not None else 0
    brush = tail_points((132, 256), 116, 0, sway, droop=0)
    parts = [part(ribbon(brush, 40, 18, bulge=30), fur)]
    parts += quad_legs(320, 154, 70, 64, s, 34, fur, pale, (20, 16), (22, 16), width=(26, 18))
    parts += [
        part(ellipse(224, 256, 118, 58), fur),
        part(ellipse(304, 256, 52, 66), fur),
        part(poly([(368, 226), (346, 182), (396, 214)]), fur),
        part(poly(mirror_y([(368, 226), (346, 182), (396, 214)])), fur),
        part(ellipse(384, 256, 46, 44), fur),
        part(path("M 396 226 C 430 232 470 246 492 256 C 470 266 430 280 396 286 Z"), fur),
    ]
    pattern = (
        fill(ellipse(206, 256, 102, 34), saddle)
        + fill(ribbon([brush[1], brush[2], brush[3]], 18, 30), saddle)
        + fill(poly([(366, 220), (354, 196), (384, 214)]), saddle) + fill(poly(mirror_y([(366, 220), (354, 196), (384, 214)])), saddle)
        + fill(path("M 404 238 C 432 242 462 250 480 256 C 462 262 432 270 404 274 Z"), pale)
        + stroke("M 300 220 L 330 236 M 300 292 L 330 276", pale, 8, 0.8)
    )
    face = (
        fill(circle(490, 256, 11), INK)
        + eye(410, 232, 12, "#FFC23C") + eye(410, 280, 12, "#FFC23C")
    )
    return build(parts, pattern, face)


ANIMALS = {
    "Mouse": mouse, "Rat": rat, "Beetle": beetle, "Cat": cat, "Owl": owl,
    "Skunk": skunk, "Hornet": hornet, "Bear": bear, "Wolf": wolf,
}

FRAMES = [(None, "")] + [(frame, f"Walk{frame + 1}") for frame in range(4)]

if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "html")
    os.makedirs(out, exist_ok=True)
    for name, draw in ANIMALS.items():
        for frame, suffix in FRAMES:
            svg = draw(frame)
            with open(os.path.join(out, name + suffix + ".html"), "w") as page:
                page.write(f'<!doctype html><html><body style="margin:0;background:transparent;overflow:hidden">{svg}</body></html>')
    print("drew", len(ANIMALS) * len(FRAMES))
