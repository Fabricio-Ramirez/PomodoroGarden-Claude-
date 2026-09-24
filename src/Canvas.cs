using System;

namespace PomodoroGarden
{
    // Low-resolution framebuffer. The whole UI is drawn here pixel by pixel and the
    // window scales it up with nearest-neighbour filtering, which keeps it crisp.
    sealed class Canvas
    {
        public readonly int W, H;
        public readonly int[] Px;
        int clipX0, clipY0, clipX1, clipY1; // clip rectangle, max is exclusive

        public Canvas(int w, int h)
        {
            W = w; H = h; Px = new int[w * h];
            NoClip();
        }

        public void Clip(int x, int y, int w, int h)
        {
            clipX0 = Math.Max(0, x); clipY0 = Math.Max(0, y);
            clipX1 = Math.Min(W, x + w); clipY1 = Math.Min(H, y + h);
        }

        public void NoClip() { clipX0 = 0; clipY0 = 0; clipX1 = W; clipY1 = H; }

        public void Clear(int c)
        {
            for (int i = 0; i < Px.Length; i++) Px[i] = c;
        }

        public void Set(int x, int y, int c)
        {
            if (x >= clipX0 && y >= clipY0 && x < clipX1 && y < clipY1) Px[y * W + x] = c;
        }

        public int Get(int x, int y)
        {
            return x >= 0 && y >= 0 && x < W && y < H ? Px[y * W + x] : 0;
        }

        public void Rect(int x, int y, int w, int h, int c)
        {
            int x0 = Math.Max(clipX0, x), y0 = Math.Max(clipY0, y);
            int x1 = Math.Min(clipX1, x + w), y1 = Math.Min(clipY1, y + h);
            for (int yy = y0; yy < y1; yy++)
            {
                int row = yy * W;
                for (int xx = x0; xx < x1; xx++) Px[row + xx] = c;
            }
        }

        public void HLine(int x, int y, int w, int c) { Rect(x, y, w, 1, c); }
        public void VLine(int x, int y, int h, int c) { Rect(x, y, 1, h, c); }

        public void Outline(int x, int y, int w, int h, int c)
        {
            HLine(x, y, w, c); HLine(x, y + h - 1, w, c);
            VLine(x, y, h, c); VLine(x + w - 1, y, h, c);
        }

        // Box with a 1px border and clipped corners: the basic pixel-art panel shape.
        public void Panel(int x, int y, int w, int h, int fill, int border)
        {
            Rect(x + 1, y + 1, w - 2, h - 2, fill);
            HLine(x + 1, y, w - 2, border); HLine(x + 1, y + h - 1, w - 2, border);
            VLine(x, y + 1, h - 2, border); VLine(x + w - 1, y + 1, h - 2, border);
        }

        public void Circle(double cx, double cy, double r, int c)
        {
            for (int y = (int)Math.Floor(cy - r); y <= (int)Math.Ceiling(cy + r); y++)
                for (int x = (int)Math.Floor(cx - r); x <= (int)Math.Ceiling(cx + r); x++)
                {
                    double dx = x + 0.5 - cx, dy = y + 0.5 - cy;
                    if (dx * dx + dy * dy <= r * r) Set(x, y, c);
                }
        }

        // Multi-colour sprite: each character is looked up in keys -> colors, '.' is transparent.
        public void Sprite(string[] rows, int x, int y, string keys, int[] colors, bool flipX = false, int scale = 1)
        {
            for (int r = 0; r < rows.Length; r++)
            {
                string row = rows[r];
                for (int i = 0; i < row.Length; i++)
                {
                    int k = keys.IndexOf(row[i]);
                    if (k < 0) continue;
                    int col = flipX ? row.Length - 1 - i : i;
                    Rect(x + col * scale, y + r * scale, scale, scale, colors[k]);
                }
            }
        }

        // Darkens an area towards a deep blue, keeping the pixel art readable (dark theme scenery).
        public void Dim(int x, int y, int w, int h)
        {
            int x0 = Math.Max(clipX0, x), y0 = Math.Max(clipY0, y);
            int x1 = Math.Min(clipX1, x + w), y1 = Math.Min(clipY1, y + h);
            for (int yy = y0; yy < y1; yy++)
                for (int xx = x0; xx < x1; xx++)
                {
                    int c = Px[yy * W + xx];
                    int r = (c >> 16) & 0xFF, g = (c >> 8) & 0xFF, b = c & 0xFF;
                    r = r * 45 / 100 + 6; g = g * 45 / 100 + 8; b = b * 52 / 100 + 22;
                    Px[yy * W + xx] = unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b;
                }
        }

        // Single-colour sprite: every character other than '.' is painted with c.
        public void Mask(string[] rows, int x, int y, int c)
        {
            for (int r = 0; r < rows.Length; r++)
                for (int i = 0; i < rows[r].Length; i++)
                    if (rows[r][i] != '.') Set(x + i, y + r, c);
        }

        public void Text(string s, int x, int y, int c, int scale = 1)
        {
            foreach (char ch in s)
            {
                string[] g = PixelFont.Glyph(ch);
                for (int r = 0; r < g.Length; r++)
                    for (int i = 0; i < g[r].Length; i++)
                        if (g[r][i] == '#') Rect(x + i * scale, y + r * scale, scale, scale, c);
                x += (g[0].Length + 1) * scale;
            }
        }

        // Text centred horizontally inside the span [x, x + w).
        public void TextIn(string s, int x, int w, int y, int c, int scale = 1)
        {
            int tw = PixelFont.Width(s) * scale;
            if (tw > w - 4 && !Overflows.Contains(s)) Overflows.Add(s);
            Text(s, x + (w - tw) / 2, y, c, scale);
        }

        // Texts that were too wide for their span; checked by the preview tool.
        public static readonly System.Collections.Generic.List<string> Overflows = new System.Collections.Generic.List<string>();
    }
}
