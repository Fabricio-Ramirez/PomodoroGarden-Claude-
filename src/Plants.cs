using System;

namespace PomodoroGarden
{
    // The plants you can grow. A grown plant is stored as a code: species * 16 + colour,
    // so the garden of version 1 (colours 0-5, all daisies) still reads the same.
    static class Plants
    {
        public const int Daisy = 0, Rose = 1, Tulip = 2, Sunflower = 3, Tree = 4, Cactus = 5;
        public const int Count = 6;
        public const int Surprise = -1; // the settings choice "a different plant every round"

        public static readonly string[] Names = { "DAISY", "ROSE", "TULIP", "SUNFLOWER", "LITTLE TREE", "CACTUS" };
        public static readonly string[] Keys = { "daisy", "rose", "tulip", "sunflower", "tree", "cactus" };

        static readonly Flower[][] colours =
        {
            Art.Flowers,
            new[]
            {
                new Flower(Pal.Rose, Pal.Coral, Pal.Red, Pal.Plum, Pal.Red, Pal.Plum),
                new Flower(Pal.Pink, Pal.PinkLight, Pal.PinkDark, Pal.Red, Pal.PinkDark, Pal.Red),
                new Flower(Pal.Yellow, Pal.Cream, Pal.Orange, Pal.PotDark, Pal.Orange, Pal.PotDark),
                new Flower(Pal.White, Pal.PureWhite, Pal.Silver, Pal.Slate, Pal.Silver, Pal.Slate),
            },
            new[]
            {
                new Flower(Pal.Rose, Pal.Coral, Pal.Red, Pal.Plum, Pal.Yellow, Pal.Orange),
                new Flower(Pal.Pink, Pal.PinkLight, Pal.PinkDark, Pal.Red, Pal.Yellow, Pal.Orange),
                new Flower(Pal.Yellow, Pal.Cream, Pal.Orange, Pal.PotDark, Pal.Yellow, Pal.Orange),
                new Flower(Pal.Violet, Pal.VioletLight, Pal.VioletDark, Pal.Plum, Pal.Yellow, Pal.Orange),
                new Flower(Pal.Orange, Pal.Yellow, Pal.Pot, Pal.PotDark, Pal.Yellow, Pal.Orange),
            },
            new[]
            {
                new Flower(Pal.Yellow, Pal.Cream, Pal.Orange, Pal.PotDark, Pal.SoilLight, Pal.Soil),
                new Flower(Pal.Orange, Pal.Yellow, Pal.Pot, Pal.PotDark, Pal.SoilLight, Pal.Soil),
            },
            new[]
            {
                new Flower(Pal.Rose, Pal.Coral, Pal.Red, Pal.Plum, Pal.Rose, Pal.Red),                 // apples
                new Flower(Pal.Orange, Pal.Yellow, Pal.Pot, Pal.PotDark, Pal.Orange, Pal.Pot),         // oranges
                new Flower(Pal.Yellow, Pal.Cream, Pal.Orange, Pal.PotDark, Pal.Yellow, Pal.Orange),    // lemons
                new Flower(Pal.Pink, Pal.PureWhite, Pal.PinkDark, Pal.Red, Pal.Yellow, Pal.Orange, true), // blossoms
            },
            new[]
            {
                new Flower(Pal.Pink, Pal.PinkLight, Pal.PinkDark, Pal.Red, Pal.Yellow, Pal.Orange),
                new Flower(Pal.Yellow, Pal.Cream, Pal.Orange, Pal.PotDark, Pal.Orange, Pal.Pot),
                new Flower(Pal.Rose, Pal.Coral, Pal.Red, Pal.Plum, Pal.Yellow, Pal.Orange),
            },
        };

        public static int Code(int species, int colour) { return species * 16 + colour; }

        public static int SpeciesOf(int code)
        {
            int s = code / 16;
            return s >= 0 && s < Count ? s : Daisy;
        }

        public static int ColoursOf(int species) { return colours[species].Length; }

        public static Flower ColourOf(int code)
        {
            Flower[] set = colours[SpeciesOf(code)];
            int n = set.Length, i = code % 16;
            return set[((i % n) + n) % n];
        }

        public static string NameOf(int choice) { return choice == Surprise ? "SURPRISE ME" : Names[choice]; }

        public static int ParseChoice(string key)
        {
            for (int i = 0; i < Count; i++)
                if (Keys[i] == key) return i;
            return Surprise;
        }

        public static string KeyOf(int choice) { return choice >= 0 && choice < Count ? Keys[choice] : "surprise"; }

        // Draws the plant at growth p (0 = seed, 1 = fully grown). sx is the left column of the
        // 2px stem, soilY the soil row it grows from. (headX, headY) is where the bloom ends up,
        // for the sparkles.
        public static void Draw(Canvas c, int sx, int soilY, double p, int code, double time, bool animate,
                                out int headX, out int headY)
        {
            Flower f = ColourOf(code);
            headX = sx + 1;
            headY = soilY - 51;
            if (p <= 0) { Art.DrawSeed(c, sx, soilY); return; }

            double sway = animate ? 1.6 * Math.Sin(time * 1.8) : 0;
            switch (SpeciesOf(code))
            {
                case Tree: DrawTree(c, sx, soilY, p, f, sway * 0.5, out headX, out headY); return;
                case Cactus: DrawCactus(c, sx, soilY, p, f, out headX, out headY); return;
            }

            int species = SpeciesOf(code);
            int topX, topY;
            if (species == Tulip)
            {
                Stem(c, sx, soilY, p, 36, 0, sway * 0.6, TulipLeaves, false, out topX, out topY);
                headX = topX + 1; headY = topY - 5;
                if (p < 0.85) return;
                double q = (p - 0.85) / 0.15;
                if (q < 0.35) Bud(c, Art.BudSmall, topX - 1, topY - 3, f);
                else DrawTulip(c, topX + 1, topY + 1, p >= 1 ? 1 : 0.6 + (q - 0.35) / 0.65 * 0.4, f);
                return;
            }

            int maxStem = species == Sunflower ? 44 : 46;
            Stem(c, sx, soilY, p, maxStem, 1.4, sway, species == Sunflower ? BigLeaves : NormalLeaves,
                 species == Rose, out topX, out topY);
            if (p < 0.85) return;

            double k = (p - 0.85) / 0.15;
            if (k < 0.35) { Bud(c, Art.BudSmall, topX - 1, topY - 3, f); return; }
            if (k < 0.7 && species != Rose) { Bud(c, Art.BudBig, topX - 2, topY - 5, f); return; }
            double grow = p >= 1 ? 1 : (k - 0.35) / 0.65;
            switch (species)
            {
                case Rose:
                    double rr = 3.5 + grow * 3.5;
                    DrawRose(c, topX + 1, topY - rr * 0.7, rr, f);
                    headX = topX + 1; headY = (int)(topY - rr);
                    break;
                case Sunflower:
                    double sr = p >= 1 ? 9 : 5.5 + (k - 0.7) / 0.3 * 3.5;
                    DrawSunflower(c, topX + 1, topY - sr + 2, sr, f);
                    headX = topX + 1; headY = (int)(topY - sr);
                    break;
                default:
                    double r = p >= 1 ? 8 : 4.5 + (k - 0.7) / 0.3 * 3;
                    Art.DrawBloom(c, topX + 1, topY - r + 1.5, r, f);
                    headX = topX + 1; headY = (int)(topY - r);
                    break;
            }
        }

        static void Bud(Canvas c, string[] sprite, int x, int y, Flower f)
        {
            c.Sprite(sprite, x, y, "LMDgG", new[] { f.Light, f.Main, f.Dark, Pal.Teal, Pal.Green });
        }

        // ---- stems and leaves ----

        sealed class Leaves
        {
            public int[] At; public bool[] Right; public double[] Len; public double Up;
        }

        static readonly Leaves NormalLeaves = new Leaves
        {
            At = new[] { 3, 3, 10, 16, 22, 28, 34, 39 },
            Right = new[] { false, true, true, false, true, false, true, false },
            Len = new double[] { 4, 4, 10, 11, 11, 10, 8, 6 },
            Up = 0.55,
        };

        static readonly Leaves BigLeaves = new Leaves
        {
            At = new[] { 3, 3, 9, 15, 21, 27, 33 },
            Right = new[] { false, true, true, false, true, false, true },
            Len = new double[] { 5, 5, 13, 14, 13, 12, 9 },
            Up = 0.5,
        };

        static readonly Leaves TulipLeaves = new Leaves
        {
            At = new[] { 2, 5 },
            Right = new[] { false, true },
            Len = new double[] { 15, 13 },
            Up = 1.05,
        };

        static int StemOffset(int k, int maxStem, double curve, double sway)
        {
            double u = k / (double)maxStem;
            return (int)Math.Round(curve * Math.Sin(u * Math.PI * 1.3) + sway * Math.Pow(u, 1.5));
        }

        // A stem growing to maxStem pixels by p = 0.85, with leaves unfolding along it.
        static void Stem(Canvas c, int sx, int soilY, double p, int maxStem, double curve, double sway, Leaves leaves,
                         bool thorns, out int topX, out int topY)
        {
            double g = Math.Min(1, p / 0.85);
            int h = Math.Max(2, (int)Math.Round(maxStem * (1 - Math.Pow(1 - g, 1.4))));

            for (int k = 0; k < h; k++)
            {
                int x = sx + StemOffset(k, maxStem, curve, sway);
                c.Set(x, soilY - k, Pal.Teal);
                c.Set(x + 1, soilY - k, Pal.Green);
                if (thorns && k > 4 && k < h - 3 && k % 7 == 0) c.Set(k % 14 == 0 ? x - 1 : x + 2, soilY - k, Pal.Plum);
            }

            for (int i = 0; i < leaves.At.Length; i++)
            {
                int grow = h - leaves.At[i];
                if (grow < 1) continue;
                double len = Math.Min(leaves.Len[i], 1.5 + grow * 0.55);
                int x = sx + StemOffset(leaves.At[i], maxStem, curve, sway);
                double y = soilY - leaves.At[i] + 0.5;
                if (leaves.Right[i]) Art.DrawLeaf(c, x + 2, y, len, true, leaves.Up);
                else Art.DrawLeaf(c, x, y, len, false, leaves.Up);
            }

            topX = sx + StemOffset(h - 1, maxStem, curve, sway);
            topY = soilY - h + 1;
            if (p < 0.85) c.Set(topX + 1, topY, Pal.Lime); // fresh growing tip
        }

        // ---- flower heads ----

        // Fills a shape pixel by pixel (colour 0 = empty) and outlines it with line.
        static void Shape(Canvas c, int x0, int y0, int w, int h, Func<double, double, int> colourAt, int line)
        {
            var cols = new int[w, h];
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++) cols[i, j] = colourAt(x0 + i + 0.5, y0 + j + 0.5);
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    if (cols[i, j] != 0) { c.Set(x0 + i, y0 + j, cols[i, j]); continue; }
                    bool edge = (i > 0 && cols[i - 1, j] != 0) || (i < w - 1 && cols[i + 1, j] != 0)
                             || (j > 0 && cols[i, j - 1] != 0) || (j < h - 1 && cols[i, j + 1] != 0);
                    if (edge) c.Set(x0 + i, y0 + j, line);
                }
        }

        // A rose seen from the side-top: a cup of petals with a spiral in the middle.
        static void DrawRose(Canvas c, double cx, double cy, double r, Flower f)
        {
            c.Sprite(new[] { "gG.Gg", ".gGg." }, (int)Math.Round(cx) - 3, (int)Math.Round(cy + r * 0.75), "gG", new[] { Pal.Teal, Pal.Green });
            int x0 = (int)Math.Floor(cx - r) - 1, y0 = (int)Math.Floor(cy - r) - 1, n = (int)Math.Ceiling(r * 2) + 3;
            Shape(c, x0, y0, n, n, (x, y) =>
            {
                double dx = x - cx, dy = (y - cy) / 0.85;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (d > r) return 0;
                if (d < r * 0.22) return f.Dark;
                double a = Math.Atan2(dy, dx) / (2 * Math.PI);
                double s = a + d / (r * 0.45);
                double frac = s - Math.Floor(s);
                if (frac < 0.22) return f.Dark;
                if (dx + dy < -r * 0.5) return f.Light;
                return f.Main;
            }, f.Line);
        }

        // Tulip cup: three pointed petal tips over a round bottom. size 1 = full bloom.
        static void DrawTulip(Canvas c, double cx, double bottom, double size, Flower f)
        {
            double hw = 4.6 * size, ht = 10 * size, top = bottom - ht;
            int x0 = (int)Math.Floor(cx - hw) - 1, y0 = (int)Math.Floor(top) - 1;
            Shape(c, x0, y0, (int)Math.Ceiling(hw * 2) + 3, (int)Math.Ceiling(ht) + 3, (x, y) =>
            {
                double u = x - cx, v = y - top;
                if (v < 0 || v > ht || Math.Abs(u) > hw) return 0;
                double split = ht * 0.32;
                if (v < split)
                {
                    double t = v / split, reach = hw * 0.36 * t + 0.35;
                    bool tip = Math.Abs(u) <= reach || Math.Abs(u + hw * 0.62) <= reach || Math.Abs(u - hw * 0.62) <= reach;
                    if (!tip) return 0;
                }
                else
                {
                    double ev = (v - split) / (ht - split);
                    if ((u / hw) * (u / hw) + ev * ev > 1.02) return 0;
                }
                if (v > split * 0.6 && Math.Abs(Math.Abs(u) - hw * 0.34) < 0.5) return f.Dark; // petal edges
                if (u < -hw * 0.4) return f.Light;
                if (u > hw * 0.45) return f.Dark;
                return f.Main;
            }, f.Line);
        }

        // Sunflower: a ring of pointed petals around a big seedy centre.
        static void DrawSunflower(Canvas c, double cx, double cy, double r, Flower f)
        {
            const int Petals = 12;
            double core = r * 0.48;
            int x0 = (int)Math.Floor(cx - r) - 1, y0 = (int)Math.Floor(cy - r) - 1, n = (int)Math.Ceiling(r * 2) + 3;
            Shape(c, x0, y0, n, n, (x, y) =>
            {
                double dx = x - cx, dy = y - cy, d = Math.Sqrt(dx * dx + dy * dy);
                if (d <= core)
                {
                    bool seed = (((int)Math.Floor(x) + (int)Math.Floor(y)) & 1) == 0;
                    if (d > core - 1 || dx + dy > core * 0.6) return f.CenterDark;
                    return seed ? f.Center : f.CenterDark;
                }
                double a = Math.Atan2(dy, dx);
                double petal = Math.Abs(Math.Cos(a * Petals / 2));
                if (d > core + (r - core) * (0.35 + 0.65 * petal)) return 0;
                if (petal < 0.3) return f.Dark;
                return dx + dy < 0 ? f.Light : f.Main;
            }, f.Line);
        }

        // ---- little tree ----

        static readonly double[,] Blobs =
        {
            { 0, 0, 0.8 }, { -0.62, 0.18, 0.62 }, { 0.62, 0.18, 0.62 },
            { -0.32, -0.42, 0.6 }, { 0.34, -0.4, 0.6 }, { 0, -0.62, 0.5 },
        };

        static readonly double[,] FruitAt =
        {
            { -0.55, 0.15 }, { 0.5, 0.25 }, { -0.1, -0.4 }, { 0.2, 0.05 }, { -0.35, 0.5 }, { 0.45, -0.3 }, { -0.5, -0.25 },
        };

        static void DrawTree(Canvas c, int sx, int soilY, double p, Flower f, double sway, out int headX, out int headY)
        {
            double t = Math.Min(1, p / 0.7);
            int th = 2 + (int)Math.Round(24 * (1 - Math.Pow(1 - t, 1.5)));
            for (int k = 0; k < th; k++)
            {
                c.Set(sx, soilY - k, Pal.Wood);
                c.Set(sx + 1, soilY - k, Pal.WoodDark);
            }
            c.Set(sx - 1, soilY, Pal.WoodLight); c.Set(sx + 2, soilY, Pal.WoodDark);
            if (th > 14)
            {
                for (int i = 1; i <= 3; i++) c.Set(sx - i, soilY - (int)(th * 0.55) - i, Pal.Wood);
                for (int i = 1; i <= 3; i++) c.Set(sx + 1 + i, soilY - (int)(th * 0.72) - i, Pal.WoodDark);
            }

            double cg = Math.Max(0, Math.Min(1, (p - 0.06) / 0.79));
            double R = 2.5 + 13.5 * Math.Pow(cg, 0.8);
            double cx = sx + 1 + sway, cy = soilY - th - R * 0.3;
            headX = (int)Math.Round(cx); headY = (int)Math.Round(cy - R * 0.4);
            if (cg <= 0) return;

            int x0 = (int)Math.Floor(cx - R * 1.4) - 1, y0 = (int)Math.Floor(cy - R * 1.2) - 1;
            int w = (int)Math.Ceiling(R * 2.8) + 3, h = (int)Math.Ceiling(R * 2.2) + 3;
            Shape(c, x0, y0, w, h, (x, y) =>
            {
                double dx = x - cx, dy = y - cy;
                bool inside = false;
                for (int b = 0; b < Blobs.GetLength(0) && !inside; b++)
                {
                    double bx = dx - Blobs[b, 0] * R, by = dy - Blobs[b, 1] * R, br = Blobs[b, 2] * R;
                    inside = bx * bx + by * by <= br * br;
                }
                if (!inside) return 0;
                double shade = (dx * 0.7 + dy) / R;
                uint hsh = Art.Hash((uint)((int)Math.Floor(x) * 73856093 ^ (int)Math.Floor(y) * 19349663));
                if (shade < -0.7) return Pal.Lime;
                if (shade < -0.45) return hsh % 3 == 0 ? Pal.Green : Pal.Lime;
                if (shade > 0.5) return Pal.Teal;
                if (shade > 0.3) return hsh % 3 == 0 ? Pal.Green : Pal.Teal;
                return hsh % 11 == 0 ? Pal.Lime : Pal.Green;
            }, Pal.LeafDark);

            if (p < 0.85) return;
            int fruits = p >= 1 ? FruitAt.GetLength(0) : (int)((p - 0.85) / 0.15 * FruitAt.GetLength(0));
            for (int i = 0; i < fruits; i++)
            {
                int fx = (int)Math.Round(cx + FruitAt[i, 0] * R), fy = (int)Math.Round(cy + FruitAt[i, 1] * R);
                if (f.Blossom)
                {
                    c.Set(fx, fy - 1, f.Main); c.Set(fx - 1, fy, f.Main); c.Set(fx + 1, fy, f.Dark); c.Set(fx, fy + 1, f.Dark);
                    c.Set(fx, fy, f.Center);
                }
                else
                {
                    c.Set(fx, fy, f.Light); c.Set(fx + 1, fy, f.Main);
                    c.Set(fx, fy + 1, f.Main); c.Set(fx + 1, fy + 1, f.Dark);
                }
            }
        }

        // ---- cactus ----

        static bool InRoundRect(double x, double y, int rx, int ry, int rw, int rh)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            if (xi < rx || yi < ry || xi >= rx + rw || yi >= ry + rh) return false;
            bool cornerX = xi == rx || xi == rx + rw - 1, cornerY = yi == ry;
            return !(cornerX && cornerY); // only the top corners are rounded
        }

        static void DrawCactus(Canvas c, int sx, int soilY, double p, Flower f, out int headX, out int headY)
        {
            double g = Math.Min(1, p / 0.85);
            int bh = 3 + (int)Math.Round(25 * (1 - Math.Pow(1 - g, 1.4)));
            int bx = sx - 2, top = soilY - bh + 1;
            double la = Math.Max(0, Math.Min(1, (p - 0.35) / 0.3)), ra = Math.Max(0, Math.Min(1, (p - 0.55) / 0.3));
            int lAttach = soilY - 10, rAttach = soilY - 15;
            bool left = la > 0 && bh > 13, right = ra > 0 && bh > 18;
            int lLen = 2 + (int)Math.Round(7 * la), rLen = 2 + (int)Math.Round(6 * ra);

            Func<double, double, bool> inside = (x, y) =>
                InRoundRect(x, y, bx, top, 7, bh)
                || (left && (InRoundRect(x, y, bx - 3, lAttach - 2, 4, 3) || InRoundRect(x, y, bx - 5, lAttach - lLen, 4, lLen + 1)))
                || (right && (InRoundRect(x, y, bx + 6, rAttach - 2, 4, 3) || InRoundRect(x, y, bx + 8, rAttach - rLen, 4, rLen + 1)));

            Shape(c, bx - 7, top - 1, 21, bh + 2, (x, y) =>
            {
                if (!inside(x, y)) return 0;
                if (!inside(x - 1, y)) return Pal.Lime;
                if (!inside(x + 1, y)) return Pal.Teal;
                int xi = (int)Math.Floor(x);
                if (xi == bx + 3 && y > top + 1) return Pal.Teal; // rib down the middle
                return Pal.Green;
            }, Pal.LeafDark);

            for (int y = top + 3; y < soilY - 1; y += 4)
            {
                c.Set(bx - 1, y, Pal.Cream);
                c.Set(bx + 7, y + 2, Pal.Cream);
            }

            headX = bx + 3; headY = top - 4;
            if (p < 0.85) return;
            double k = (p - 0.85) / 0.15;
            if (k < 0.5) Bud(c, Art.BudSmall, bx + 2, top - 3, f);
            else Art.DrawBloom(c, bx + 3.5, top - 2.5, p >= 1 ? 4.5 : 3.5, f);
        }

        // ---- tiny potted plants for the "today's garden" shelf, 7x11 ----

        static readonly string[] PotRows = { "QPPPPPp", ".QPPPp.", ".QPPPp.", "..PPp.." };

        static readonly string[][] Minis =
        {
            new[] { "..fFF..", ".fFyFd.", "..Fdd..", "...G...", ".LLG...", "...GLL.", "...G..." },
            new[] { "..fFd..", ".fdFFd.", ".FFddd.", "..gGg..", ".LLG...", "...G...", "...GLL." },
            new[] { ".f.F.d.", ".fFFdd.", ".fFFdd.", "..FFd..", "...G...", "L..G..L", ".LLGLL." },
            new[] { "..fFf..", ".fyYyd.", ".FYYYd.", "..dFd..", ".LLG...", "...GLL.", "...G..." },
            new[] { "..LLL..", ".LGGGg.", "LGyGGGg", ".GGGyg.", "..gTg..", "...T...", "...T..." },
            new[] { "...F...", "..LGg..", "L.LGg..", "L.LGg.g", "LLLGg.g", "..LGggg", "..LGg.." },
        };

        public static void DrawMini(Canvas c, int x, int y, int code, int scale = 1)
        {
            int species = SpeciesOf(code);
            Flower f = ColourOf(code);
            string keys = "fFdyYGLgTQPp";
            int dot = species == Tree ? f.Main : f.Center; // fruit on the tree, the flower's centre otherwise
            var cols = new[] { f.Light, f.Main, f.Dark, dot, f.CenterDark, Pal.Green, Pal.Lime, Pal.Teal, Pal.Wood,
                               Pal.PotLight, Pal.Pot, Pal.PotDark };
            c.Sprite(Minis[species], x, y, keys, cols, false, scale);
            c.Sprite(PotRows, x, y + 7 * scale, keys, cols, false, scale);
        }
    }
}
