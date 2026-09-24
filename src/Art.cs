using System;

namespace PomodoroGarden
{
    sealed class Flower
    {
        public readonly int Main, Light, Dark, Line, Center, CenterDark;
        public readonly bool Blossom; // little tree: blossoms instead of fruit

        public Flower(int main, int light, int dark, int line, int center, int centerDark, bool blossom = false)
        {
            Main = main; Light = light; Dark = dark; Line = line; Center = center; CenterDark = centerDark; Blossom = blossom;
        }
    }

    // Pixel-art sprites, icons, the pot and the shared pieces of the plants.
    static class Art
    {
        public static readonly Flower[] Flowers =
        {
            new Flower(Pal.Rose, Pal.Coral, Pal.Red, Pal.Plum, Pal.Yellow, Pal.Orange),
            new Flower(Pal.Pink, Pal.PinkLight, Pal.PinkDark, Pal.Red, Pal.Yellow, Pal.Orange),
            new Flower(Pal.Yellow, Pal.Cream, Pal.Orange, Pal.PotDark, Pal.PotDark, Pal.Soil),
            new Flower(Pal.Violet, Pal.VioletLight, Pal.VioletDark, Pal.Plum, Pal.Yellow, Pal.Orange),
            new Flower(Pal.White, Pal.PureWhite, Pal.Silver, Pal.Slate, Pal.Yellow, Pal.Orange),
            new Flower(Pal.Orange, Pal.Yellow, Pal.Pot, Pal.PotDark, Pal.Plum, Pal.Ink),
        };

        // ---- icons (single colour masks) ----

        public static readonly string[] IconReset =
        {
            "..###.#",
            ".#...##",
            "#...###",
            "#......",
            "#.....#",
            ".#...#.",
            "..###..",
        };

        public static readonly string[] IconSkip =
        {
            "#....##",
            "##...##",
            "###..##",
            "####.##",
            "###..##",
            "##...##",
            "#....##",
        };

        public static readonly string[] IconGear =
        {
            "...#...",
            ".#####.",
            ".#...#.",
            "##...##",
            ".#...#.",
            ".#####.",
            "...#...",
        };

        public static readonly string[] IconPin =
        {
            ".#####.",
            "..###..",
            "..###..",
            ".#####.",
            "#######",
            "...#...",
            "...#...",
        };

        public static readonly string[] IconMinimize =
        {
            ".......",
            ".......",
            ".......",
            ".......",
            ".......",
            ".#####.",
            ".......",
        };

        public static readonly string[] IconClose =
        {
            ".......",
            ".#...#.",
            "..#.#..",
            "...#...",
            "..#.#..",
            ".#...#.",
            ".......",
        };

        public static readonly string[] IconMusic =
        {
            "..#####",
            "..#####",
            "..#...#",
            "..#...#",
            ".##..##",
            "###.###",
            ".#...#.",
        };

        public static readonly string[] IconMinus = { ".....", ".....", "#####", ".....", "....." };
        public static readonly string[] IconPlus = { "..#..", "..#..", "#####", "..#..", "..#.." };

        // ---- small sprites ----

        public static readonly string[] Tomato =
        {
            "...G...",
            ".RgGgR.",
            "RORRRRR",
            "RORRRRR",
            "RRRRRRD",
            ".RRRRD.",
            "..DDD..",
        };
        public static readonly string TomatoKeys = "RODGg";
        public static readonly int[] TomatoCols = { Pal.Red, Pal.Orange, Pal.Plum, Pal.Green, Pal.Teal };

        public static readonly string[] TitleSprout =
        {
            "LL...GG",
            ".LL.GG.",
            "...G...",
            "QPPPPPp",
            ".QPPPp.",
            ".QPPPp.",
            "..PPp..",
        };

        public static readonly string[] CloudBig =
        {
            "......WWW......",
            "...WWWWWWWW....",
            "..WWWWWWWWWWWW.",
            ".WWWWWWWWWWWWWW",
            "WWWWWWWWWWWWWWW",
            ".SSSSSSSSSSSSS.",
        };

        public static readonly string[] CloudSmall =
        {
            "...WWW....",
            ".WWWWWWWW.",
            "WWWWWWWWWW",
            ".SSSSSSSS.",
        };

        public static readonly string[] ButterflyOpen =
        {
            "WW.WW",
            "WSBSW",
            ".WBW.",
            "..B..",
        };

        public static readonly string[] ButterflyShut =
        {
            ".....",
            ".WBW.",
            ".WBW.",
            "..B..",
        };

        public static readonly string[] SparkBig =
        {
            "..Y..",
            "..W..",
            "YWWWY",
            "..W..",
            "..Y..",
        };

        public static readonly string[] SparkSmall = { ".Y.", "YWY", ".Y." };

        // ---- plant parts (the plants themselves are in Plants.cs) ----

        public static readonly string[] BudSmall = { ".LM.", ".MD.", "gMDG", ".gG." };
        public static readonly string[] BudBig = { "..LM..", ".LMMD.", ".LMMD.", "gLMDDG", ".gMDG.", "..gG.." };

        // Curved leaf growing from (ax, ay) up and away from the stem.
        // up is the angle above horizontal: 0.55 is about 30 degrees.
        public static void DrawLeaf(Canvas c, double ax, double ay, double len, bool right, double up = 0.55)
        {
            double Up = up;
            double side = right ? 1 : -1;
            double dx = Math.Cos(Up) * side, dy = -Math.Sin(Up);
            double nx = Math.Sin(Up) * side, ny = Math.Cos(Up); // perpendicular, pointing down
            double width = Math.Max(0.9, len * 0.3);
            for (int y = (int)Math.Floor(ay - len - 2); y <= (int)Math.Ceiling(ay + 3); y++)
                for (int x = (int)Math.Floor(ax - len - 2); x <= (int)Math.Ceiling(ax + len + 2); x++)
                {
                    double px = x + 0.5 - ax, py = y + 0.5 - ay;
                    double u = px * dx + py * dy, v = px * nx + py * ny;
                    if (u < 0 || u > len) continue;
                    v -= 0.2 * u * u / len; // the tip droops a little
                    double hw = width * Math.Pow(Math.Sin(Math.PI * (u + 0.7) / (len + 0.7)), 0.75);
                    if (Math.Abs(v) > hw + 0.2) continue;
                    int col = v < -0.25 ? Pal.Lime : Pal.Green;
                    if (v > 0.3 && v > hw - 0.9) col = Pal.Teal;
                    c.Set(x, y, col);
                }
        }

        static readonly string[] Seed = { ".CW.", "CWWl", "WWlD" };

        public static void DrawSeed(Canvas c, int sx, int soilY)
        {
            c.Sprite(Seed, sx - 1, soilY - 2, "CWlD", new[] { Pal.Cream, Pal.WoodLight, Pal.Wood, Pal.WoodDark });
        }

        // Five round petals around a centre, lit from the top-left, with a darker outline.
        public static void DrawBloom(Canvas c, double cx, double cy, double r, Flower f)
        {
            const int Petals = 5;
            double pr = r * 0.44, pd = r - pr, core = r * 0.3;
            int x0 = (int)Math.Floor(cx - r) - 1, y0 = (int)Math.Floor(cy - r) - 1;
            int n = (int)Math.Ceiling(r * 2) + 4;
            var cols = new int[n, n];
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    double dx = x0 + i + 0.5 - cx, dy = y0 + j + 0.5 - cy;
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    if (d <= core)
                    {
                        cols[i, j] = dx + dy > 0.6 ? f.CenterDark : f.Center;
                        continue;
                    }
                    int hits = 0;
                    double best = 1e9, lx = 0, ly = 0;
                    for (int k = 0; k < Petals; k++)
                    {
                        double a = -Math.PI / 2 + k * 2 * Math.PI / Petals;
                        double px = dx - Math.Cos(a) * pd, py = dy - Math.Sin(a) * pd;
                        double pdist = Math.Sqrt(px * px + py * py);
                        if (pdist > pr) continue;
                        hits++;
                        if (pdist < best) { best = pdist; lx = px; ly = py; }
                    }
                    if (hits == 0)
                    {
                        if (d < r * 0.5) cols[i, j] = f.Main;
                        continue;
                    }
                    double s = (lx + ly) / pr;
                    if (hits >= 2 && d > core + 1) cols[i, j] = f.Dark;             // gap between petals
                    else if (s < -0.45) cols[i, j] = f.Light;                       // lit side
                    else if (best > pr - 1.0 && s > 0.55) cols[i, j] = f.Dark;      // shaded rim
                    else cols[i, j] = f.Main;
                }

            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    if (cols[i, j] != 0) { c.Set(x0 + i, y0 + j, cols[i, j]); continue; }
                    bool edge = (i > 0 && cols[i - 1, j] != 0) || (i < n - 1 && cols[i + 1, j] != 0)
                             || (j > 0 && cols[i, j - 1] != 0) || (j < n - 1 && cols[i, j + 1] != 0);
                    if (edge) c.Set(x0 + i, y0 + j, f.Line);
                }
        }

        // Terracotta pot, 26x18, (x, y) = top-left. The soil row is y + 1.
        public static void DrawPot(Canvas c, int x, int y)
        {
            c.HLine(x + 1, y, 24, Pal.Ink);
            c.Set(x, y + 1, Pal.Ink); c.Set(x + 25, y + 1, Pal.Ink);
            c.Set(x + 1, y + 1, Pal.PotLight); c.Set(x + 24, y + 1, Pal.PotDark);
            c.HLine(x + 2, y + 1, 22, Pal.Soil);
            for (int i = 1; i < 22; i += 6) c.Set(x + 2 + i, y + 1, Pal.SoilLight);

            for (int r = 2; r <= 5; r++)
            {
                c.Set(x, y + r, Pal.Ink); c.Set(x + 25, y + r, Pal.Ink);
                c.HLine(x + 1, y + r, 24, r == 2 ? Pal.PotLight : Pal.Pot);
                if (r > 2)
                {
                    c.HLine(x + 1, y + r, 2, Pal.PotLight);
                    c.HLine(x + 22, y + r, 3, Pal.PotDark);
                }
            }
            c.HLine(x, y + 6, 26, Pal.Ink);

            for (int r = 7; r <= 16; r++)
            {
                int inset = 2 + (r - 7) / 4;
                int bx = x + inset, bw = 26 - inset * 2;
                c.Set(bx, y + r, Pal.Ink); c.Set(bx + bw - 1, y + r, Pal.Ink);
                if (r == 7) { c.HLine(bx + 1, y + r, bw - 2, Pal.PotDark); continue; }
                c.HLine(bx + 1, y + r, bw - 2, Pal.Pot);
                c.HLine(bx + 1, y + r, 2, Pal.PotLight);
                c.HLine(bx + bw - 4, y + r, 3, Pal.PotDark);
            }
            c.HLine(x + 5, y + 17, 16, Pal.Ink);
        }

        // Vertical gradient with 2x2 ordered dithering between the colour bands.
        public static void Gradient(Canvas c, int x, int y, int w, int h, int[] cols)
        {
            int bands = cols.Length - 1;
            for (int r = 0; r < h; r++)
            {
                double t = r * bands / (double)Math.Max(1, h - 1);
                int i = Math.Min(bands - 1, (int)t);
                double f = (t - i - 0.5) * 3.5 + 0.5; // squeeze the dither into the middle of each band
                for (int xx = 0; xx < w; xx++)
                {
                    double threshold = Bayer[((y + r) & 1) * 2 + ((x + xx) & 1)];
                    c.Set(x + xx, y + r, f > threshold ? cols[i + 1] : cols[i]);
                }
            }
        }

        static readonly double[] Bayer = { 0.125, 0.625, 0.875, 0.375 };

        public static uint Hash(uint x)
        {
            x ^= x >> 16; x *= 0x7feb352d;
            x ^= x >> 15; x *= 0x846ca68b;
            x ^= x >> 16;
            return x;
        }
    }
}
