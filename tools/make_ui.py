"""
Generates ClownID's UI assets (standard library only):

  resources/ClownID.ico           plug-in icon
  dist/icon.png                   package icon
  resources/ClownID.rui           toolbar with ClownID and ClownIDCapture buttons

Run from the repository root:  python3 tools/make_ui.py
GUIDs are fixed so Rhino keeps the toolbar's position and visibility across updates.
"""
import base64
import os
import struct
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


# --- tiny PNG / ICO writer --------------------------------------------------

def png(w, h, rgba):
    raw = b''.join(b'\x00' + rgba[y * w * 4:(y + 1) * w * 4] for y in range(h))

    def chunk(t, d):
        return struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)

    return (b'\x89PNG\r\n\x1a\n'
            + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, 6, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(raw, 9))
            + chunk(b'IEND', b''))


def ico(images):
    """images: list of (size, png bytes)."""
    header = struct.pack('<HHH', 0, 1, len(images))
    offset = 6 + 16 * len(images)
    entries = b''
    for size, data in images:
        entries += struct.pack('<BBBBHHII', size % 256, size % 256, 0, 0, 1, 32, len(data), offset)
        offset += len(data)
    return header + entries + b''.join(d for _, d in images)


# --- icon drawing (unit square, y down) -------------------------------------

def hexc(s):
    return tuple(int(s[i:i + 2], 16) for i in (1, 3, 5))


BLACK, WHITE = hexc('#000000'), hexc('#FFFFFF')
GREEN, PINK, BLUE = hexc('#26D609'), hexc('#FF4FA3'), hexc('#33A1FF')


def in_rounded_square(x, y, r=0.2):
    cx = min(max(x, r), 1 - r)
    cy = min(max(y, r), 1 - r)
    return (x - cx) ** 2 + (y - cy) ** 2 <= r * r


def shapes(x, y, box=(0.0, 0.0, 1.0, 1.0)):
    """The clown-pass motif, mapped into box; returns a colour or None."""
    x0, y0, x1, y1 = box
    u, v = (x - x0) / (x1 - x0), (y - y0) / (y1 - y0)
    col = None
    if 0.13 <= u <= 0.60 and 0.44 <= v <= 0.87:
        col = GREEN
    if 0.29 <= u <= 0.51 and 0.13 <= v <= 0.62:
        col = PINK
    if (u - 0.69) ** 2 + (v - 0.60) ** 2 <= 0.19 ** 2:
        col = BLUE
    return col


def mode_icon(x, y):
    if not in_rounded_square(x, y):
        return None
    return shapes(x, y) or BLACK


def capture_icon(x, y):
    if not in_rounded_square(x, y):
        return None
    # viewfinder corners
    a, b, t = 0.11, 0.37, 0.10
    for cx, cy in ((a, a), (1 - a, a), (a, 1 - a), (1 - a, 1 - a)):
        dx, dy = abs(x - cx), abs(y - cy)
        sx = 1 if cx < 0.5 else -1
        sy = 1 if cy < 0.5 else -1
        along_x = 0 <= (x - cx) * sx <= b - a and 0 <= (y - cy) * sy <= t
        along_y = 0 <= (y - cy) * sy <= b - a and 0 <= (x - cx) * sx <= t
        if along_x or along_y:
            return WHITE
    return shapes(x, y, (0.2, 0.2, 0.8, 0.8)) or BLACK


def render(fn, n, ss=8):
    out = bytearray()
    for py in range(n):
        for px in range(n):
            acc, hits = [0, 0, 0], 0
            for sy in range(ss):
                for sx in range(ss):
                    c = fn((px + (sx + .5) / ss) / n, (py + (sy + .5) / ss) / n)
                    if c:
                        acc = [acc[i] + c[i] for i in range(3)]
                        hits += 1
            out += bytes([acc[i] // hits if hits else 0 for i in range(3)] + [round(255 * hits / ss / ss)])
    return bytes(out)


def strip(icons, n):
    """Horizontal strip of icons, n×n each, as one PNG."""
    tiles = [render(fn, n) for fn in icons]
    rows = b''.join(b''.join(t[y * n * 4:(y + 1) * n * 4] for t in tiles) for y in range(n))
    return png(n * len(icons), n, rows)


# --- toolbar file ------------------------------------------------------------

FILE_ID = 'f432d18e-991a-4279-bec2-9527afbfc950'
MENU_ID = '3a8d4273-e846-4dda-9734-bafa902cd778'
GROUP_ID = '5a2ef227-ac3a-4d0d-82b9-ee8e659b4d2f'
DOCK_ID = 'f47e3ffc-75ec-49ac-9979-65cf4892f8f6'
GROUP_ITEM_ID = '43eebabd-27cb-4159-a2b3-3dd1503ea3bc'
TOOLBAR_ID = '7bbdee36-478b-48b3-9b72-f7d27f672b5b'

BUTTONS = [
    # (item id, macro id, bitmap id, icon, script, name, tooltip)
    ('7b4c28f3-5ef8-4097-b8dd-8e4c60dc7d20', 'c630949d-947d-4262-9b06-2b4f4d1bd7f3',
     'a430581f-5f34-4b50-a219-c9fce38923f3', mode_icon, '! _ClownID', 'Clown ID',
     'Switch the active viewport to Clown ID and back'),
    ('ac494afc-3985-4107-b634-7416c8b22eda', 'b8f3e1d2-6a4c-4f7e-9d1b-2c5e8a7f3b61',
     'e2d7c9a4-1b3f-4e8d-a6c5-9f0b2d4e7a18', capture_icon, '! _ClownIDCapture', 'Clown ID Capture',
     'Save the active view as a clown-pass PNG with a JSON colour key'),
]


def text(tag, value, indent):
    pad = ' ' * indent
    return f'{pad}<{tag}>\n{pad}  <locale_1033>{value}</locale_1033>\n{pad}</{tag}>\n'


def rui():
    x = '<?xml version="1.0" encoding="utf-8"?>\n'
    x += (f'<RhinoUI major_ver="5" minor_ver="0" guid="{FILE_ID}" localize="False" '
          f'default_language_id="1033" dpi_scale="100">\n')
    x += '  <extend_rhino_menus>\n'
    x += f'    <menu guid="{MENU_ID}">\n' + text('text', 'Extend Rhino Menus', 6) + '    </menu>\n'
    x += '  </extend_rhino_menus>\n'
    x += '  <menus />\n'
    x += '  <tool_bar_groups>\n'
    x += (f'    <tool_bar_group guid="{GROUP_ID}" dock_bar_guid64="{DOCK_ID}" '
          f'active_tool_bar_group="{TOOLBAR_ID}" single_file="True" '
          f'hide_single_tab="True" point_floating="400,200">\n')
    x += text('text', 'ClownID', 6)
    x += f'      <tool_bar_group_item guid="{GROUP_ITEM_ID}" major_version="1" minor_version="1">\n'
    x += text('text', 'ClownID', 8)
    x += f'        <tool_bar_id>{TOOLBAR_ID}</tool_bar_id>\n'
    x += '      </tool_bar_group_item>\n'
    x += ('      <dock_bar_info dpi_scale="100" dock_bar="False" docking="True" horz="True" visible="True" '
          'floating="True" mru_float_style="8192" bar_id="59650" mru_width="100" point_pos="-2,-2" '
          'float_point="400,200" dock_location_u="59419" dock_location="top" float_size="90,46" />\n')
    x += '    </tool_bar_group>\n'
    x += '  </tool_bar_groups>\n'
    x += '  <tool_bars>\n'
    x += f'    <tool_bar guid="{TOOLBAR_ID}" bitmap_id="{BUTTONS[0][2]}">\n'
    x += text('text', 'ClownID', 6)
    for item_id, macro_id, *_ in BUTTONS:
        x += f'      <tool_bar_item guid="{item_id}">\n'
        x += f'        <left_macro_id>{macro_id}</left_macro_id>\n'
        x += '      </tool_bar_item>\n'
    x += '    </tool_bar>\n'
    x += '  </tool_bars>\n'
    x += '  <macros>\n'
    for _, macro_id, bitmap_id, _, script, name, tip in BUTTONS:
        x += f'    <macro_item guid="{macro_id}" bitmap_id="{bitmap_id}">\n'
        x += text('text', name, 6) + text('tooltip', tip, 6) + text('help_text', tip, 6)
        x += text('button_text', name, 6) + text('menu_text', name, 6)
        x += f'      <script>{script}</script>\n'
        x += '    </macro_item>\n'
    x += '  </macros>\n'
    x += '  <bitmaps>\n'
    for tag, n in (('small_bitmap', 16), ('normal_bitmap', 24), ('large_bitmap', 32)):
        x += f'    <{tag} item_width="{n}" item_height="{n}">\n'
        for i, b in enumerate(BUTTONS):
            x += f'      <bitmap_item guid="{b[2]}" index="{i}" />\n'
        data = base64.b64encode(strip([b[3] for b in BUTTONS], n)).decode()
        x += f'      <bitmap>{data}</bitmap>\n'
        x += f'    </{tag}>\n'
    x += '  </bitmaps>\n'
    x += '  <scripts />\n'
    x += '</RhinoUI>\n'
    return x


def main():
    big = render(mode_icon, 256, 4)
    with open(os.path.join(ROOT, 'dist', 'icon.png'), 'wb') as f:
        f.write(png(256, 256, big))
    sizes = [16, 24, 32, 48, 256]
    images = [(s, png(s, s, big if s == 256 else render(mode_icon, s))) for s in sizes]
    with open(os.path.join(ROOT, 'resources', 'ClownID.ico'), 'wb') as f:
        f.write(ico(images))
    with open(os.path.join(ROOT, 'resources', 'ClownID.rui'), 'w', encoding='utf-8') as f:
        f.write(rui())
    print('wrote dist/icon.png, resources/ClownID.ico, resources/ClownID.rui')


if __name__ == '__main__':
    main()
