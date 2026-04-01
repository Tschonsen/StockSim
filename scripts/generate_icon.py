"""Generate a pixel-art style StockSim app icon (.ico + .png)"""
from PIL import Image, ImageDraw

def create_icon():
    # Work at 64x64 pixel-art scale, then export multi-size .ico
    size = 64
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    px = 2  # pixel block size (64/2 = 32x32 pixel grid)

    def block(x, y, color):
        """Draw a pixel block at grid position (x, y)"""
        draw.rectangle([x * px, y * px, (x + 1) * px - 1, (y + 1) * px - 1], fill=color)

    # Colors — dark terminal aesthetic
    bg = (10, 14, 23)         # --bg-primary
    green = (34, 197, 94)     # bullish green
    red = (239, 68, 68)       # bearish red
    accent = (96, 165, 250)   # --text-accent blue
    dark_accent = (55, 100, 180)
    grid_line = (30, 40, 60)  # subtle grid

    # Background: rounded-ish dark square
    for y in range(32):
        for x in range(32):
            # Round corners (skip 2px corners)
            if (x < 2 and y < 2) or (x > 29 and y < 2) or (x < 2 and y > 29) or (x > 29 and y > 29):
                if (x < 1 and y < 1) or (x > 30 and y < 1) or (x < 1 and y > 30) or (x > 30 and y > 30):
                    continue
            block(x, y, bg)

    # Subtle grid lines (every 4 pixels)
    for y in range(4, 28, 4):
        for x in range(3, 29):
            block(x, y, grid_line)
    for x in range(7, 28, 5):
        for y in range(4, 28):
            block(x, y, grid_line)

    # Stock chart line — green uptrend with a dip
    # Chart goes from left to right, bottom to top = bullish
    chart_points = [
        (4, 24), (5, 23), (6, 22), (7, 21), (8, 22), (9, 23),  # dip
        (10, 22), (11, 20), (12, 19), (13, 18), (14, 17),       # recovery
        (15, 18), (16, 16), (17, 15), (18, 13), (19, 12),       # rally
        (20, 13), (21, 11), (22, 10), (23, 9), (24, 8),         # continuation
        (25, 7), (26, 6), (27, 5),                                # peak
    ]

    # Green area fill under the chart line (subtle)
    fill_green = (34, 197, 94, 40)
    img_alpha = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw_alpha = ImageDraw.Draw(img_alpha)
    for cx, cy in chart_points:
        for fy in range(cy + 1, 27):
            draw_alpha.rectangle([cx * px, fy * px, (cx + 1) * px - 1, (fy + 1) * px - 1], fill=fill_green)
    img = Image.alpha_composite(img, img_alpha)
    draw = ImageDraw.Draw(img)

    # Chart line itself (bright green)
    for cx, cy in chart_points:
        block(cx, cy, green)

    # Red candlestick bars (a few for atmosphere)
    candles_red = [(6, 20, 23), (15, 16, 19)]  # (x, top, bottom)
    for cx, top, bot in candles_red:
        for cy in range(top, bot + 1):
            block(cx, cy, red)

    # Green candlestick bars
    candles_green = [(19, 10, 13), (24, 6, 9)]
    for cx, top, bot in candles_green:
        for cy in range(top, bot + 1):
            block(cx, cy, green)

    # Dollar sign "$" in accent blue (top-left area, 5x7 pixel font)
    dollar = [
        "  #  ",
        " ####",
        "# #  ",
        " ### ",
        "  # #",
        "#### ",
        "  #  ",
    ]
    dx, dy = 3, 4
    for row_i, row in enumerate(dollar):
        for col_i, ch in enumerate(row):
            if ch == '#':
                block(dx + col_i, dy + row_i, accent)

    # Border highlight (top + left edge, subtle)
    for x in range(2, 30):
        block(x, 1, dark_accent)
    for y in range(2, 30):
        block(1, y, (20, 30, 50))

    # Export multiple sizes for .ico
    sizes = [16, 24, 32, 48, 64, 128, 256]
    icons = []
    for s in sizes:
        resized = img.resize((s, s), Image.NEAREST)  # NEAREST = pixel-art sharp
        icons.append(resized)

    # Save .ico (multi-size) — 256x256 image must be first/largest for electron-builder
    ico_path = 'frontend/build/icon.ico'
    import os
    os.makedirs('frontend/build', exist_ok=True)
    # Pillow .ico: save the largest, append smaller sizes
    icons[-1].save(ico_path, format='ICO', sizes=[(s, s) for s in sizes], append_images=icons[:-1])
    print(f"  -> {ico_path}")

    # Also save 256px PNG for electron-builder
    png_path = 'frontend/build/icon.png'
    icons[-1].save(png_path)
    print(f"  -> {png_path}")

    # Save 512px for Steam/store
    img_512 = img.resize((512, 512), Image.NEAREST)
    store_path = 'frontend/build/icon-512.png'
    img_512.save(store_path)
    print(f"  -> {store_path}")

if __name__ == '__main__':
    print("Generating StockSim pixel-art icon...")
    create_icon()
    print("Done!")
