#!/usr/bin/env python3
"""Makes the Sunny Meadow level: the tile set, the map, the binding, the area and the info file.

Run it from any folder: python3 generate.py. It writes the files into SONICORCA/LEVELS/MEADOW next to this script.
The level uses the item block of the lua-item-block mod, so install both mods.

The art is plain shapes that this script draws. A tile is 64 x 64 pixels, and one pixel of the original game is four
units.
"""
import math
import os
import struct
import uuid
import zlib

T = 64
ROWS = 24
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, 'SONICORCA', 'LEVELS', 'MEADOW')

# The height of the grass on the ground, in pixels
GRASS = 18


# ---------------------------------------------------------------------------------------------------------------------
# PNG

def write_png(path, width, height, pixel):
    rows = []
    for y in range(height):
        row = bytearray(b'\x00')
        for x in range(width):
            row.extend(pixel(x, y))
        rows.append(bytes(row))

    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)

    with open(path, 'wb') as f:
        f.write(b'\x89PNG\r\n\x1a\n')
        f.write(chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0)))
        f.write(chunk(b'IDAT', zlib.compress(b''.join(rows), 9)))
        f.write(chunk(b'IEND', b''))


def noise(x, y, seed=0):
    """A fixed pseudo-random value from 0 to 1 for a pixel."""
    n = (x * 374761393 + y * 668265263 + seed * 2147483647) & 0xffffffff
    n = ((n ^ (n >> 13)) * 1274126177) & 0xffffffff
    return (n & 0xffff) / 65535.0


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


# ---------------------------------------------------------------------------------------------------------------------
# Terrain

DIRT = (196, 140, 82)
DIRT_DARK = (160, 108, 60)
DIRT_LIGHT = (220, 170, 110)
GRASS_TOP = (120, 210, 70)
GRASS_MID = (70, 170, 50)
GRASS_EDGE = (40, 110, 35)


def ground_pixel(px, py, surface, edge_left=False, edge_right=False):
    """The color of a pixel of a ground tile. surface(px) is the y of the ground at px, local to the tile."""
    s = surface(px)
    depth = py - s
    if depth < 0:
        return (0, 0, 0, 0)
    if depth < GRASS:
        # Blades: the grass goes a little deeper at some columns
        if depth < 3:
            return GRASS_TOP + (255,)
        return (GRASS_EDGE if depth >= GRASS - 3 else GRASS_MID) + (255,)
    color = DIRT
    # Stones in the dirt, on a pattern that repeats each 64 pixels
    sx, sy = px % 32, (py + (px // 32) * 13) % 32
    if (sx - 16) ** 2 + (sy - 16) ** 2 < 20:
        color = DIRT_LIGHT if noise(px // 32, (py + (px // 32) * 13) // 32) > 0.5 else DIRT_DARK
    if (edge_left and px < 6) or (edge_right and px >= T - 6):
        color = DIRT_DARK
    return color + (255,)


# A tile of ground is the key (left, right, edge_left, edge_right): the y of the surface at the left and right of the
# tile, local to the tile. A surface below the tile gives no tile, a surface far above gives the dirt tile.
def ground_key(y_left, y_right, edge_left, edge_right):
    if y_left >= T and y_right >= T:
        return None
    if y_left <= -GRASS and y_right <= -GRASS:
        return ('dirt', edge_left, edge_right)
    return (y_left, y_right, edge_left, edge_right)


def ground_tile_pixel(key):
    if key[0] == 'dirt':
        _, el, er = key
        return lambda px, py: ground_pixel(px, py, lambda x: -1000, el, er)
    yl, yr, el, er = key
    return lambda px, py: ground_pixel(px, py, lambda x: yl + (yr - yl) * (x + 0.5) / T, el, er)


# ---------------------------------------------------------------------------------------------------------------------
# Other tiles

def platform_pixel(part):
    """A floating log platform: part is 'l', 'm' or 'r'."""
    def pixel(px, py):
        if py >= 40:
            return (0, 0, 0, 0)
        x = px if part != 'l' else px - 8
        if part == 'l' and px < 8 or part == 'r' and px >= T - 8:
            return (0, 0, 0, 0)
        ring = ((py - 20) ** 2) / 400.0
        wood = mix((150, 95, 45), (110, 65, 30), min(1.0, ring * 2))
        if py < 3 or py >= 37:
            wood = (80, 45, 20)
        if (x + py // 2) % 16 == 0:
            wood = mix(wood, (90, 55, 25), 0.6)
        return wood + (255,)
    return pixel


def flower_pixel(px, py):
    """A small flower on the grass."""
    cx, cy = 32, 40
    if abs(px - cx) <= 1 and cy <= py < 64:
        return (40, 120, 40, 255)
    d = math.hypot(px - cx, py - cy)
    if d < 4:
        return (255, 230, 80, 255)
    for i in range(5):
        a = i * 2 * math.pi / 5
        if math.hypot(px - (cx + 8 * math.cos(a)), py - (cy + 8 * math.sin(a))) < 5:
            return (255, 255, 255, 255)
    return (0, 0, 0, 0)


def sky_pixel(row):
    top, bottom = (70, 140, 230), (190, 225, 255)

    def pixel(px, py):
        t = (row * T + py) / (18 * T)
        return mix(top, bottom, t) + (255,)
    return pixel


def far_pixel(x, y):
    """The far backdrop, 32 x 17 tiles: clouds, pale mountains and soft hills. It repeats in x."""
    w = 32 * T
    # Clouds
    for cx, cy, r in ((200, 140, 60), (260, 120, 70), (330, 145, 55), (1100, 220, 50), (1160, 200, 65), (1225, 225, 50),
                      (1700, 120, 45), (1750, 105, 55)):
        for dx in (-w, 0, w):
            if math.hypot(x - cx - dx, (y - cy) * 1.6) < r:
                shade = 255 if y < cy else 235
                return (shade, shade, 255, 255)
    # The sum of whole waves on the width of the backdrop, so that the backdrop repeats without a seam
    def wave(*parts):
        return sum(a * math.sin(2 * math.pi * k * x / w + p) for a, k, p in parts)
    hills = 500 + wave((40, 3, 0.4), (22, 7, 1.9), (10, 13, 0.2))
    if y >= hills:
        return mix((120, 190, 110), (80, 150, 95), min(1.0, (y - hills) / 220)) + (255,)
    mountains = 330 + wave((90, 2, 0.0), (45, 5, 2.1), (20, 11, 4.0))
    if y >= mountains:
        snow = y < mountains + 18 and mountains < 290
        return ((235, 240, 250) if snow else mix((150, 170, 205), (125, 150, 190), min(1.0, (y - mountains) / 200))) + (255,)
    return (0, 0, 0, 0)


def near_pixel(x, y):
    """The near backdrop, 32 x 17 tiles: a band of bushes at the bottom. It repeats in x."""
    base = 600 + 50 * math.sin(2 * math.pi * 9 * x / (32 * T)) + 30 * math.sin(2 * math.pi * 23 * x / (32 * T) + 1.0)
    if y < base:
        return (0, 0, 0, 0)
    color = mix((70, 160, 70), (30, 100, 45), min(1.0, (y - base) / 160))
    if noise(x // 6, y // 6, 3) > 0.92:
        color = mix(color, (140, 220, 110), 0.5)
    return color + (255,)


# ---------------------------------------------------------------------------------------------------------------------
# Layout. Each part of the ground is a run of columns. The row is the row of the surface at the start.

class Ground:
    def __init__(self, row):
        self.row = row
        self.columns = []      # For each column: (y_left, y_right) in level units, or None for a pit

    def flat(self, n):
        y = self.row * T
        self.columns += [(y, y)] * n

    def up(self, rows):
        """A 45 degree slope up: one column for each row."""
        for _ in range(rows):
            self.columns.append((self.row * T, (self.row - 1) * T))
            self.row -= 1

    def down(self, rows):
        for _ in range(rows):
            self.columns.append((self.row * T, (self.row + 1) * T))
            self.row += 1

    def up_gentle(self, rows):
        """A gentle slope up: two columns for each row."""
        for _ in range(rows):
            y = self.row * T
            self.columns += [(y, y - T // 2), (y - T // 2, y - T)]
            self.row -= 1

    def down_gentle(self, rows):
        for _ in range(rows):
            y = self.row * T
            self.columns += [(y, y + T // 2), (y + T // 2, y + T)]
            self.row += 1

    def pit(self, n):
        self.columns += [None] * n


def build_layout():
    g = Ground(18)
    g.flat(24)                  # 0: the start, with the first blocks
    g.up_gentle(2)              # 24
    g.flat(10)                  # 28: a hill top with blocks over a dip
    g.down_gentle(2)            # 38
    g.flat(8)                   # 42
    g.pit(5)                    # 50: the first gap, with a log over it
    g.flat(12)                  # 55
    g.up(3)                     # 67: steps up to a plateau
    g.flat(16)                  # 70: the plateau, with a row of blocks
    g.down(3)                   # 86
    g.flat(6)                   # 89
    g.pit(8)                    # 95: a wide gap with logs
    g.flat(10)                  # 103: a spring to a high ledge
    g.up_gentle(3)              # 113
    g.flat(6)                   # 119
    g.down_gentle(3)            # 125
    g.flat(14)                  # 131: blocks in a staircase
    g.pit(4)                    # 145
    g.flat(8)                   # 149
    g.up(2)                     # 157
    g.flat(8)                   # 159
    g.down(2)                   # 167
    g.flat(40)                  # 169: the last run to the signpost
    return g.columns


# Floating logs: (first column, number of columns, row of the top)
LOGS = [(51, 3, 15), (96, 3, 16), (100, 2, 14), (108, 6, 9)]

# Objects: (key, column, y in level units, behaviour)
BLOCK = 'SONICORCA/OBJECTS/LUAITEMBLOCK'
RING = 'SONICORCA/OBJECTS/RING'


def build_objects(columns):
    objects = []

    def ground_y(column):
        # Over a pit, the ground before the pit
        while columns[column] is None:
            column -= 1
        return min(columns[column])

    def block(column, rows_above, contents=None, rings=None):
        # The block is 128 units: its centre is at the left edge of the column after this column
        y = ground_y(column) - rows_above * T
        behaviour = {}
        if contents:
            behaviour['contents'] = contents
        if rings is not None:
            behaviour['rings'] = rings
        objects.append((BLOCK, (column + 1) * T, y, behaviour))

    def rings(first, count, rows_above, arc=0):
        for i in range(count):
            column = first + i
            lift = int(arc * math.sin(math.pi * i / max(1, count - 1)))
            objects.append((RING, column * T + T // 2, ground_y(column) - rows_above * T - lift, {}))

    rings(6, 4, 1)
    block(12, 4)
    block(16, 4, contents=RING, rings=5)
    rings(29, 6, 1)
    block(31, 5)
    block(35, 5)
    rings(48, 9, 2, arc=180)
    block(58, 4)
    for c in (73, 76, 79, 82):
        block(c, 4)
    rings(74, 8, 1)
    objects.append(('SONICORCA/OBJECTS/SPRING', 106 * T, ground_y(106) - 32, {'Direction': 'Up', 'Strength': 56}))
    rings(109, 4, 0, arc=0)
    block(134, 3)
    block(137, 5)
    block(140, 7)
    rings(150, 6, 1, arc=120)
    rings(176, 10, 1)
    block(180, 4, contents=RING, rings=10)
    objects.append(('SONICORCA/OBJECTS/SIGNPOST', 196 * T, ground_y(196) - 92, {}))
    return objects


# ---------------------------------------------------------------------------------------------------------------------
# Output

def main():
    columns = build_layout()
    cols = len(columns)
    os.makedirs(os.path.join(OUT, 'TILESET'), exist_ok=True)

    # The tiles: id -> pixel function, and the map grids
    tiles = {}
    tile_ids = {}

    def tile_id(key, make):
        if key not in tile_ids:
            tile_ids[key] = len(tile_ids) + 1
            tiles[tile_ids[key]] = make()
        return tile_ids[key]

    sky = [[0] * 1 for _ in range(18)]
    for r in range(18):
        sky[r][0] = tile_id(('sky', r), lambda r=r: sky_pixel(r))

    def backdrop(name, pixel):
        grid = [[0] * 32 for _ in range(17)]
        for r in range(17):
            for c in range(32):
                key = (name, c, r)
                if any(pixel(c * T + x, r * T + y)[3] for x in range(0, T, 8) for y in range(0, T, 8)):
                    grid[r][c] = tile_id(key, lambda c=c, r=r: (lambda x, y: pixel(c * T + x, r * T + y)))
        return grid

    far = backdrop('far', far_pixel)
    near = backdrop('near', near_pixel)

    front = [[0] * cols for _ in range(ROWS)]
    vectors = []
    for c, col in enumerate(columns):
        if col is None:
            continue
        y_left, y_right = col
        edge_left = c > 0 and columns[c - 1] is None
        edge_right = c + 1 < cols and columns[c + 1] is None
        top_row = min(y_left, y_right) // T
        for r in range(max(0, top_row - 1), ROWS):
            key = ground_key(y_left - r * T, y_right - r * T, edge_left, edge_right)
            if key is not None:
                front[r][c] = tile_id(key, lambda key=key: ground_tile_pixel(key))
        # The top of the ground, from left to right, so that the solid side is below
        flags = 0 if y_left == y_right else 8
        vectors.append(((c * T, y_left), ((c + 1) * T, y_right), flags))
        bottom = ROWS * T + 256
        if edge_left:
            vectors.append(((c * T, bottom), (c * T, y_left), 0))
        if edge_right:
            vectors.append((((c + 1) * T, y_right), ((c + 1) * T, bottom), 0))
    # The walls at the ends of the level
    vectors.append(((0, 0), (0, columns[0][0]), 0))
    vectors.append(((cols * T, columns[-1][1]), (cols * T, 0), 0))

    # Flowers on some flat columns
    for c, col in enumerate(columns):
        if col and col[0] == col[1] and noise(c, 7) > 0.8 and 0 < c < cols - 1:
            front[col[0] // T - 1][c] = tile_id('flower', lambda: flower_pixel)

    # Logs: tiles and a line on top that a character can jump through from below
    for first, count, row in LOGS:
        for i in range(count):
            part = 'l' if i == 0 else 'r' if i == count - 1 else 'm'
            front[row][first + i] = tile_id(('log', part), lambda part=part: platform_pixel(part))
        vectors.append(((first * T + 8, row * T), ((first + count) * T - 8, row * T), 0))

    # The texture: 16 tiles in each row
    ids = sorted(tiles)
    per_row = 16
    tex_w = per_row * T
    tex_h = ((len(ids) + per_row - 1) // per_row) * T

    def texture_pixel(x, y):
        index = (y // T) * per_row + x // T
        if index >= len(ids):
            return (0, 0, 0, 0)
        return tiles[ids[index]](x % T, y % T)

    write_png(os.path.join(OUT, 'TILESET', 'TEXTURE.png'), tex_w, tex_h, texture_pixel)
    with open(os.path.join(OUT, 'TILESET', '_.tileset.xml'), 'w') as f:
        f.write('<?xml version="1.0" encoding="utf-8"?>\n<tileset>\n  <textures>\n    <texture>/TEXTURE</texture>\n  </textures>\n  <tiles>\n')
        for i, tid in enumerate(ids):
            f.write(f'    <tile id="{tid}" texture="0" x="{(i % per_row) * T}" y="{(i // per_row) * T}" />\n')
        f.write('  </tiles>\n</tileset>\n')

    def layer(name, grid, extra=''):
        rows = '\n'.join(f'        <row>{",".join(str(v) for v in row)}</row>' for row in grid)
        return f'    <layer name="{name}">\n{extra}      <tiles{name_attributes[name]}>\n{rows}\n      </tiles>\n    </layer>\n'

    # The backdrops do not move in y, and move slower than the level in x
    name_attributes = {
        'sky': ' parallax_y="0" wrap_x="true"',
        'far': ' parallax_y="0" wrap_x="true"',
        'near': ' parallax_y="0" wrap_x="true"',
        'front': '',
    }
    rowdef = lambda parallax, rows: f'      <rowdefinitions>\n        <rowdefinition height="{rows * T}" parallax_x="{parallax}" />\n      </rowdefinitions>\n'
    with open(os.path.join(OUT, 'MAP.map.xml'), 'w') as f:
        f.write('<?xml version="1.0" encoding="utf-8"?>\n<map>\n  <tiles>\n')
        f.write(layer('sky', sky, rowdef(0, 18)))
        f.write(layer('far', far, rowdef(0.15, 17)))
        f.write(layer('near', near, rowdef(0.4, 17)))
        f.write(layer('front', front))
        f.write('  </tiles>\n  <collision>\n    <paths>\n      <path layer="3" />\n      <path layer="3" />\n    </paths>\n    <vectors>\n')
        for (ax, ay), (bx, by), flags in vectors:
            f.write(f'      <vector a="{ax},{ay}" b="{bx},{by}" paths="3" flags="{flags}" />\n')
        f.write('    </vectors>\n  </collision>\n  <markers>\n')
        f.write(f'    <marker name="startpos" x="{4 * T}" y="{columns[4][0] - 80}" layer="3" />\n')
        f.write(f'    <marker name="bounds_lt" x="0" y="0" layer="3" />\n')
        f.write(f'    <marker name="bounds_rb" x="{cols * T}" y="{ROWS * T}" layer="3" />\n')
        f.write('  </markers>\n</map>\n')

    with open(os.path.join(OUT, 'BINDING.binding.xml'), 'w') as f:
        f.write('<?xml version="1.0" encoding="utf-8"?>\n<Binding>\n  <Definitions DefaultLayer="3">\n')
        counts = {}
        for key, x, y, behaviour in build_objects(columns):
            short = key.split('/')[-1].title()
            counts[short] = counts.get(short, -1) + 1
            uid = uuid.uuid5(uuid.NAMESPACE_URL, f'meadow/{key}/{x}/{y}')
            f.write(f'    <Definition>\n      <Common>\n        <Key>{key}</Key>\n        <Uid>{uid}</Uid>\n')
            f.write(f'        <Name>{short} {counts[short]}</Name>\n        <Position X="{x}" Y="{y}" />\n      </Common>\n')
            if behaviour:
                f.write('      <Behaviour>\n')
                for k, v in behaviour.items():
                    f.write(f'        <{k}>{v}</{k}>\n')
                f.write('      </Behaviour>\n')
            f.write('    </Definition>\n')
        f.write('  </Definitions>\n</Binding>\n')

    with open(os.path.join(OUT, 'AREA.area.cs'), 'w') as f:
        f.write('#use SONICORCA.MODAREA\n')
    with open(os.path.join(OUT, 'INFO.xml'), 'w') as f:
        f.write('<?xml version="1.0" encoding="utf-8"?>\n'
                '<level name="Sunny Meadow" music="SONICORCA/MUSIC/LEVELS/EHZ/COOPERATIVE/ACT1" start="startpos" />\n')
    print(f'{cols} columns, {len(ids)} tiles, {len(vectors)} lines')


if __name__ == '__main__':
    main()
