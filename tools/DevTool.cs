using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

// Build helper, not shipped: renders preview PNGs of the UI and generates the app icon.
//   devtool preview <dir>
//   devtool icon <file.ico>
namespace PomodoroGarden
{
    sealed class NullHost : IHost
    {
        public int MaxScale { get { return 4; } }
        public void ApplyScale(int scale) { }
        public void ApplyTopMost(bool on) { }
        public void Minimize() { }
        public void Quit() { }
        public void Alert() { }
    }

    static class DevTool
    {
        static int Main(string[] args)
        {
            if (args.Length == 2 && args[0] == "preview") { Previews(args[1]); return 0; }
            if (args.Length == 2 && args[0] == "icon") { MakeIcon(args[1]); return 0; }
            if (args.Length == 1 && args[0] == "cycle") return CycleTest();
            Console.WriteLine("usage: devtool preview <dir> | devtool icon <file.ico>");
            return 1;
        }

        // ---- rules: focus -> short -> focus -> long -> focus ----

        static int CycleTest()
        {
            var cfg = new Settings { Rounds = 2, Sound = false };
            var app = new App(cfg, new NullHost());
            int failures = 0;
            Action<string, bool> check = (what, ok) => { Console.WriteLine((ok ? "ok    " : "FAIL  ") + what); if (!ok) failures++; };
            Action finish = () => { app.DebugState(app.Mode, RunState.Running, 1.0); app.Update(1); };

            check("starts idle in focus", app.Mode == Mode.Focus && app.State == RunState.Idle && app.TimeLeft() == 25 * 60);
            finish();
            check("focus done -> short break, 1 round, 1 plant", app.Mode == Mode.ShortBreak && app.RoundsDone == 1 && cfg.Garden.Count == 1 && app.BreakPlant == 1);
            check("break waits for start", app.State == RunState.Idle && app.TimeLeft() == 5 * 60);
            finish();
            check("break done -> focus with a new seed", app.Mode == Mode.Focus && app.PlantGrowth == 0);
            finish();
            check("2nd focus done -> long break", app.Mode == Mode.LongBreak && app.RoundsDone == 2 && cfg.Garden.Count == 2);
            finish();
            check("long break done -> focus, rounds reset", app.Mode == Mode.Focus && app.RoundsDone == 0);

            cfg.AutoStart = true;
            finish();
            check("auto-start runs the break by itself", app.Mode == Mode.ShortBreak && app.State == RunState.Running);

            cfg.AutoStart = false;
            app.DebugState(Mode.Focus, RunState.Idle, 0);
            cfg.Focus = 50;
            app.Key(System.Windows.Forms.Keys.Space);
            check("space starts the timer", app.State == RunState.Running);
            app.Key(System.Windows.Forms.Keys.Space);
            check("space pauses the timer", app.State == RunState.Paused);
            check("time format", App.FormatTime(25 * 60) == "25:00" && App.FormatTime(59.2) == "01:00" && App.FormatTime(0) == "00:00" && App.FormatTime(120 * 60) == "120:00");

            Console.WriteLine(failures == 0 ? "cycle test: all passed" : "cycle test: " + failures + " failed");
            return failures == 0 ? 0 : 1;
        }

        // ---- previews ----

        static void Previews(string dir)
        {
            Directory.CreateDirectory(dir);
            var cfg = new Settings { Scale = 3 };
            cfg.Day = DateTime.Now.ToString("yyyy-MM-dd");
            var app = new App(cfg, new NullHost());
            var c = new Canvas(App.W, App.H);

            app.FlowerIndex = 0;
            Shot(app, c, dir, "01_focus_idle", Mode.Focus, RunState.Idle, 0, 1.2);
            cfg.Garden.AddRange(new[] { 0, 1, 2 });
            app.RoundsDone = 1;
            Shot(app, c, dir, "02_focus_35", Mode.Focus, RunState.Running, 0.35, 1.2);
            app.HoverId = "start";
            Shot(app, c, dir, "03_focus_70_hover", Mode.Focus, RunState.Running, 0.7, 1.3);
            app.HoverId = null;
            Shot(app, c, dir, "04_focus_paused_93", Mode.Focus, RunState.Paused, 0.93, 1.2);
            app.BreakPlant = 1; app.RoundsDone = 2; cfg.Garden.Add(0);
            Shot(app, c, dir, "05_short_break", Mode.ShortBreak, RunState.Running, 0.4, 5.4);
            app.RoundsDone = 4; app.FlowerIndex = 3;
            for (int i = 0; i < 12; i++) cfg.Garden.Add(i);
            Shot(app, c, dir, "06_long_break", Mode.LongBreak, RunState.Idle, 0, 7.7);
            app.RoundsDone = 0;
            app.InSettings = true;
            app.HoverId = "focus+";
            Shot(app, c, dir, "07_settings", Mode.Focus, RunState.Idle, 0, 1);
            app.InSettings = false;
            app.HoverId = null;

            // Hover every button on every screen and report any text that doesn't fit.
            var hotsField = typeof(App).GetField("hots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (Mode m in new[] { Mode.Focus, Mode.ShortBreak, Mode.LongBreak })
                foreach (RunState s in new[] { RunState.Idle, RunState.Running, RunState.Paused })
                    foreach (bool settings in new[] { false, true })
                    {
                        app.InSettings = settings;
                        app.DebugState(m, s, 0.5);
                        app.HoverId = null;
                        app.Render(c);
                        var ids = new System.Collections.Generic.List<string>();
                        foreach (HotSpot h in (System.Collections.Generic.List<HotSpot>)hotsField.GetValue(app)) ids.Add(h.Id);
                        foreach (string id in ids) { app.HoverId = id; app.Render(c); app.Render(c); }
                    }
            app.InSettings = false;
            app.HoverId = null;
            Console.WriteLine(Canvas.Overflows.Count == 0 ? "text check: all texts fit" : "text too wide: " + string.Join(" | ", Canvas.Overflows));

            // Growth strip: the plant at several stages, one pot per column.
            double[] stages = { 0, 0.02, 0.06, 0.12, 0.25, 0.4, 0.55, 0.7, 0.84, 0.88, 0.93, 0.97, 1.0 };
            var strip = new Canvas(stages.Length * 30, 80);
            strip.Clear(Pal.Sky);
            for (int i = 0; i < stages.Length; i++)
            {
                Art.DrawPot(strip, i * 30 + 2, 60);
                Art.DrawPlant(strip, i * 30 + 14, 61, stages[i], Art.Flowers[i % Art.Flowers.Length], 0, false);
            }
            Save(strip, Path.Combine(dir, "10_growth.png"), 4);

            // All flower colours in bloom.
            var blooms = new Canvas(Art.Flowers.Length * 24, 24);
            blooms.Clear(Pal.Sky);
            for (int i = 0; i < Art.Flowers.Length; i++) Art.DrawBloom(blooms, i * 24 + 12, 12, 8, Art.Flowers[i]);
            Save(blooms, Path.Combine(dir, "11_blooms.png"), 8);
        }

        static void Shot(App app, Canvas c, string dir, string name, Mode m, RunState s, double progress, double now)
        {
            app.DebugState(m, s, progress);
            app.Now = now;
            app.Render(c);
            app.Render(c); // second pass so hover hints resolve against this frame's hot spots
            Save(c, Path.Combine(dir, name + ".png"), 3);
        }

        static void Save(Canvas c, string path, int scale)
        {
            using (var bmp = Scaled(c, scale)) bmp.Save(path, ImageFormat.Png);
        }

        static Bitmap Scaled(Canvas c, int scale)
        {
            int w = c.W * scale, h = c.H * scale;
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var row = new int[w];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++) row[x] = c.Px[(y / scale) * c.W + x / scale];
                Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, w);
            }
            bmp.UnlockBits(data);
            return bmp;
        }

        // ---- icon ----

        static void MakeIcon(string path)
        {
            Canvas small = Icon16(), big = Icon32();
            var images = new[]
            {
                Scale(small, 1), Scale(big, 1), Scale(small, 3), Scale(big, 2), Scale(big, 4), Scale(big, 8),
            };
            using (var fs = File.Create(path))
            {
                var w = new BinaryWriter(fs);
                w.Write((short)0); w.Write((short)1); w.Write((short)images.Length);
                int offset = 6 + 16 * images.Length;
                var blobs = new byte[images.Length][];
                for (int i = 0; i < images.Length; i++)
                {
                    int size = images[i].W;
                    blobs[i] = Dib(images[i]);
                    w.Write((byte)(size >= 256 ? 0 : size)); w.Write((byte)(size >= 256 ? 0 : size));
                    w.Write((byte)0); w.Write((byte)0);
                    w.Write((short)1); w.Write((short)32);
                    w.Write(blobs[i].Length); w.Write(offset);
                    offset += blobs[i].Length;
                }
                foreach (byte[] b in blobs) w.Write(b);
            }
            string preview = Path.ChangeExtension(path, ".preview.png");
            var sheet = new Canvas(16 + 32 + 48 + 64 + 128 + 20, 128);
            int x = 0;
            foreach (Canvas img in images)
            {
                if (img.W > 128) continue;
                for (int yy = 0; yy < img.H; yy++)
                    for (int xx = 0; xx < img.W; xx++) sheet.Set(x + xx, yy, img.Px[yy * img.W + xx]);
                x += img.W + 4;
            }
            Save(sheet, preview, 2);
        }

        static Canvas Icon16()
        {
            var c = new Canvas(16, 16);
            c.HLine(3, 10, 10, Pal.PotLight);
            c.HLine(3, 11, 10, Pal.Pot); c.Set(3, 11, Pal.PotLight); c.Set(12, 11, Pal.PotDark);
            for (int y = 12; y <= 14; y++)
            {
                int x0 = y == 14 ? 5 : 4, x1 = y == 14 ? 10 : 11;
                c.HLine(x0, y, x1 - x0 + 1, Pal.Pot);
                c.Set(x0, y, Pal.PotLight); c.Set(x1, y, Pal.PotDark);
            }
            for (int y = 6; y <= 9; y++) { c.Set(7, y, Pal.Teal); c.Set(8, y, Pal.Green); }
            c.Set(4, 7, Pal.Lime); c.Set(5, 7, Pal.Lime); c.Set(5, 8, Pal.Green); c.Set(6, 8, Pal.Green);
            c.Set(10, 7, Pal.Lime); c.Set(11, 7, Pal.Lime); c.Set(9, 8, Pal.Green); c.Set(10, 8, Pal.Green);
            Art.DrawBloom(c, 8.0, 4.4, 3.7, Art.Flowers[0]);
            AddOutline(c);
            return c;
        }

        static Canvas Icon32()
        {
            var c = new Canvas(32, 32);
            // pot
            for (int y = 20; y <= 22; y++) c.HLine(6, y, 20, y == 20 ? Pal.PotLight : Pal.Pot);
            c.VLine(6, 21, 2, Pal.PotLight); c.VLine(7, 21, 2, Pal.PotLight); c.VLine(24, 21, 2, Pal.PotDark); c.VLine(25, 21, 2, Pal.PotDark);
            c.HLine(8, 23, 16, Pal.PotDark);
            for (int y = 24; y <= 29; y++)
            {
                int inset = (y - 24) / 3;
                int x0 = 8 + inset, x1 = 23 - inset;
                c.HLine(x0, y, x1 - x0 + 1, Pal.Pot);
                c.HLine(x0, y, 2, Pal.PotLight);
                c.HLine(x1 - 2, y, 3, Pal.PotDark);
            }
            // stem and leaves
            for (int y = 12; y <= 19; y++) { c.Set(15, y, Pal.Teal); c.Set(16, y, Pal.Green); }
            int[] leafCols = { Pal.Lime, Pal.Green, Pal.Teal };
            string[] leaf = { "....LLL", "..LLLLG", ".LLGGG.", "gGG...." };
            c.Sprite(leaf, 17, 14, "LGg", leafCols);
            c.Sprite(leaf, 8, 16, "LGg", leafCols, true);
            Art.DrawBloom(c, 16.0, 8.6, 7.4, Art.Flowers[0]);
            AddOutline(c);
            return c;
        }

        // Dark 1px outline around the opaque shape, so the icon reads on any background.
        static void AddOutline(Canvas c)
        {
            var copy = (int[])c.Px.Clone();
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                {
                    if (copy[y * c.W + x] != 0) continue;
                    bool edge = false;
                    if (x > 0 && copy[y * c.W + x - 1] != 0) edge = true;
                    if (x < c.W - 1 && copy[y * c.W + x + 1] != 0) edge = true;
                    if (y > 0 && copy[(y - 1) * c.W + x] != 0) edge = true;
                    if (y < c.H - 1 && copy[(y + 1) * c.W + x] != 0) edge = true;
                    if (edge) c.Px[y * c.W + x] = Pal.Ink;
                }
        }

        static Canvas Scale(Canvas src, int s)
        {
            var c = new Canvas(src.W * s, src.H * s);
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++) c.Px[y * c.W + x] = src.Px[(y / s) * src.W + x / s];
            return c;
        }

        // 32-bit BGRA DIB icon image (bottom-up rows + AND mask).
        static byte[] Dib(Canvas c)
        {
            int size = c.W;
            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            w.Write(40); w.Write(size); w.Write(size * 2); w.Write((short)1); w.Write((short)32);
            w.Write(0); w.Write(size * size * 4); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
            for (int y = size - 1; y >= 0; y--)
                for (int x = 0; x < size; x++) w.Write(c.Px[y * size + x]);
            int maskStride = (size + 31) / 32 * 4;
            for (int y = size - 1; y >= 0; y--)
            {
                var row = new byte[maskStride];
                for (int x = 0; x < size; x++)
                    if (((c.Px[y * size + x] >> 24) & 0xFF) == 0) row[x >> 3] |= (byte)(0x80 >> (x & 7));
                w.Write(row);
            }
            return ms.ToArray();
        }
    }
}
