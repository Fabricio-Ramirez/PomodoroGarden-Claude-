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
        public bool SpotifyInstalled { get { return Installed; } }
        public bool Installed = true, OpenWorks = true;
        public readonly System.Collections.Generic.List<string> Opened = new System.Collections.Generic.List<string>();
        public bool Open(string target) { Opened.Add(target); return OpenWorks; }
    }

    static class DevTool
    {
        static int Main(string[] args)
        {
            // Never read or overwrite a real PomodoroGarden.ini.
            Settings.PathOverride = Path.Combine(Path.GetTempPath(), "PomodoroGarden-devtool-" + Guid.NewGuid().ToString("N") + ".ini");
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

            PlantTests(check);
            SpotifyTests(check);
            SettingsFileTests(check);
            ClickTests(check);
            SoundTests(check);
            WeatherTests(check);

            Console.WriteLine(failures == 0 ? "cycle test: all passed" : "cycle test: " + failures + " failed");
            return failures == 0 ? 0 : 1;
        }

        // ---- version 2: plants, Spotify, settings file, clicking through the UI ----

        static void PlantTests(Action<string, bool> check)
        {
            check("old garden entries are daisies", Plants.SpeciesOf(3) == Plants.Daisy && Plants.ColourOf(3) == Art.Flowers[3]);
            check("plant code round trip", Plants.SpeciesOf(Plants.Code(Plants.Tree, 2)) == Plants.Tree);
            check("unknown codes fall back safely", Plants.SpeciesOf(999) == Plants.Daisy && Plants.ColourOf(-5) != null);
            check("plant names parse", Plants.ParseChoice("rose") == Plants.Rose && Plants.ParseChoice("nonsense") == Plants.Surprise
                && Plants.KeyOf(Plants.Surprise) == "surprise" && Plants.ParseChoice(Plants.KeyOf(Plants.Cactus)) == Plants.Cactus);

            var cfg = new Settings { Rounds = 2, Sound = false, Plant = Plants.Rose };
            var app = new App(cfg, new NullHost());
            check("chosen plant is planted", Plants.SpeciesOf(app.PlantCode) == Plants.Rose);
            Action finish = () => { app.DebugState(app.Mode, RunState.Running, 1.0); app.Update(1); };
            finish();
            check("grown rose goes on the shelf", cfg.Garden.Count == 1 && Plants.SpeciesOf(cfg.Garden[0]) == Plants.Rose);
            finish();
            check("next round is a rose again", Plants.SpeciesOf(app.PlantCode) == Plants.Rose);

            cfg.Plant = Plants.Surprise;
            bool changes = true;
            for (int i = 0; i < 20; i++)
            {
                int before = Plants.SpeciesOf(app.PlantCode);
                finish(); finish();
                if (Plants.SpeciesOf(app.PlantCode) == before) changes = false;
            }
            check("surprise me changes the plant every round", changes);

            // Every plant, colour and growth stage stays inside the view, and draws something.
            bool inside = true, visible = true;
            var c = new Canvas(App.W, App.H);
            for (int sp = 0; sp < Plants.Count; sp++)
                for (int col = 0; col < Plants.ColoursOf(sp); col++)
                    for (int step = 0; step <= 40; step++)
                    {
                        double p = step / 40.0;
                        c.Clear(0);
                        int hx, hy;
                        Plants.Draw(c, 79, 95, p, Plants.Code(sp, col), 0.37 * step, true, out hx, out hy);
                        int count = 0;
                        for (int y = 0; y < c.H; y++)
                            for (int x = 0; x < c.W; x++)
                                if (c.Px[y * c.W + x] != 0)
                                {
                                    count++;
                                    if (x < 8 || x >= 152 || y < 32 || y >= 120) inside = false;
                                }
                        if (count == 0) visible = false;
                        if (!inside) { Console.WriteLine("  out of view: " + Plants.Names[sp] + " at " + p); break; }
                    }
            check("all plants fit the window at every stage", inside);
            check("all plants draw at every stage", visible);
        }

        static void SpotifyTests(Action<string, bool> check)
        {
            check("spotify: no link opens the app", Spotify.Target("", true) == "spotify:");
            check("spotify: no app opens the web player", Spotify.Target("", false) == Spotify.Web);
            check("spotify: web playlist link opens in the app",
                Spotify.Target("https://open.spotify.com/playlist/37i9dQZF1DX8Uebhn9wzrS?si=abc123", true) == "spotify:playlist:37i9dQZF1DX8Uebhn9wzrS");
            check("spotify: localised link", Spotify.Target("https://open.spotify.com/intl-es/album/4aawyAB9vmqN3uQ7FjRGTy", true) == "spotify:album:4aawyAB9vmqN3uQ7FjRGTy");
            check("spotify: app link without the app goes to the web",
                Spotify.Target("spotify:playlist:37i9dQZF1DX8Uebhn9wzrS", false) == "https://open.spotify.com/playlist/37i9dQZF1DX8Uebhn9wzrS");
            check("spotify: other programs are refused",
                Spotify.Target("C:\\Windows\\System32\\calc.exe", true) == "spotify:"
                && Spotify.Target("file:///C:/evil.exe", false) == Spotify.Web
                && Spotify.Target("https://evil.example/playlist/abc", true) == "spotify:"
                && Spotify.Target("spotify:playlist:abc&calc", true) == "spotify:"
                && Spotify.Target("https://open.spotify.com.evil.example/playlist/abc", false) == Spotify.Web);
        }

        static void SettingsFileTests(Action<string, bool> check)
        {
            var cfg = new Settings { Dark = false, Plant = Plants.Sunflower, Spotify = "https://open.spotify.com/playlist/abc?si=x=y" };
            cfg.Garden.AddRange(new[] { 3, Plants.Code(Plants.Tree, 3) });
            cfg.Save();
            Settings back = Settings.Load();
            check("settings file keeps the new options", !back.Dark && back.Plant == Plants.Sunflower && back.Spotify == cfg.Spotify
                && back.Garden.Count == 2 && back.Garden[1] == Plants.Code(Plants.Tree, 3));
            File.WriteAllLines(Settings.FilePath, new[] { "focus=30", "garden=0,1,5" }); // a version 1 file
            Settings old = Settings.Load();
            check("version 1 settings load with dark mode and surprise plants", old.Focus == 30 && old.Dark && old.Plant == Plants.Surprise && old.Garden.Count == 3);
            File.Delete(Settings.FilePath);
        }

        // Presses a button the way the mouse does, at the centre of its hot spot.
        static void Click(App app, Canvas c, string id)
        {
            app.Render(c);
            var hotsField = typeof(App).GetField("hots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (HotSpot h in (System.Collections.Generic.List<HotSpot>)hotsField.GetValue(app))
                if (h.Id == id)
                {
                    int x = h.X + h.W / 2, y = h.Y + h.H / 2;
                    app.MouseDown(x, y);
                    app.MouseUp(x, y);
                    app.Render(c);
                    return;
                }
            throw new Exception("no button " + id);
        }

        static void ClickTests(Action<string, bool> check)
        {
            var host = new NullHost();
            var cfg = new Settings { Sound = false };
            var app = new App(cfg, host);
            var c = new Canvas(App.W, App.H);

            Click(app, c, "music");
            check("music button opens the Spotify app", host.Opened.Count == 1 && host.Opened[0] == "spotify:" && app.Toast == "OPENING SPOTIFY...");
            host.Installed = false; host.Opened.Clear();
            Click(app, c, "music");
            check("without the app it opens the web player", host.Opened.Count == 1 && host.Opened[0] == Spotify.Web && app.Toast == "OPENING SPOTIFY IN BROWSER");
            host.Installed = true; host.OpenWorks = false; host.Opened.Clear();
            Click(app, c, "music");
            check("if nothing opens it says so", host.Opened.Count == 2 && app.Toast == "COULD NOT OPEN SPOTIFY");
            host.OpenWorks = true; host.Opened.Clear();
            cfg.Spotify = "https://open.spotify.com/playlist/37i9dQZF1DX8Uebhn9wzrS";
            Click(app, c, "music");
            check("music button opens the saved playlist", host.Opened.Count == 1 && host.Opened[0] == "spotify:playlist:37i9dQZF1DX8Uebhn9wzrS");

            Click(app, c, "gear");
            check("gear opens settings on the timer tab", app.InSettings && app.SettingsTab == 0);
            Click(app, c, "tab-look");
            check("look tab opens", app.SettingsTab == 1);
            Click(app, c, "plant5");
            check("tree tile picks the little tree", cfg.Plant == Plants.Tree && Plants.SpeciesOf(app.PlantCode) == Plants.Tree);
            Click(app, c, "plant0");
            check("? tile picks surprise me", cfg.Plant == Plants.Surprise);
            bool dark = cfg.Dark;
            Click(app, c, "dark");
            check("dark mode button switches theme", cfg.Dark == !dark && app.T == (cfg.Dark ? Theme.Dark : Theme.Classic));
            Click(app, c, "dark-row");
            check("clicking the dark mode label switches it back", cfg.Dark == dark);
            host.Opened.Clear();
            Click(app, c, "spotify");
            check("settings Spotify button opens it too", host.Opened.Count == 1);
            Click(app, c, "tab-timer");
            check("timer tab opens", app.SettingsTab == 0);
            Click(app, c, "done");
            check("done closes settings", !app.InSettings);
        }

        // ---- version 2.1: sounds and weather ----

        static void SoundTests(Action<string, bool> check)
        {
            Chiptune.Init();
            byte[] wav = Chiptune.BloomWav;
            bool header = wav != null && wav.Length > 44 && System.Text.Encoding.ASCII.GetString(wav, 0, 4) == "RIFF"
                && BitConverter.ToInt32(wav, 4) == wav.Length - 8 && System.Text.Encoding.ASCII.GetString(wav, 8, 4) == "WAVE";
            check("sounds are valid WAV data", header);
            int loud = 0;
            for (int i = 44; i + 1 < wav.Length; i += 2) loud = Math.Max(loud, Math.Abs((int)BitConverter.ToInt16(wav, i)));
            check("sounds are audible and don't clip", loud > 8000 && loud < 32000);

            var cfg = new Settings { Rounds = 2, Sound = true };
            var app = new App(cfg, new NullHost());
            Action finish = () => { app.DebugState(app.Mode, RunState.Running, 1.0); app.Update(1); };
            Chiptune.Log.Clear();
            app.Key(System.Windows.Forms.Keys.Space);
            check("pressing start plays the start sound", Chiptune.Log.Count == 1 && Chiptune.Log[0] == "pop");
            Chiptune.Log.Clear();
            finish();
            check("end of focus plays the bloom jingle", Chiptune.Log.Count == 1 && Chiptune.Log[0] == "bloom");
            Console.WriteLine("  Windows PlaySound accepted the jingle: " + (Chiptune.LastPlayWorked ? "yes" : "no (no audio device or not Windows)"));
            Chiptune.Log.Clear();
            finish();
            check("end of a break plays the wake-up jingle", Chiptune.Log.Count == 1 && Chiptune.Log[0] == "wake");
            Chiptune.Log.Clear();
            finish(); finish();
            check("end of the long break plays too", Chiptune.Log.Count == 2 && Chiptune.Log[1] == "wake");
            Chiptune.Log.Clear();
            var c = new Canvas(App.W, App.H);
            Click(app, c, "skip");
            check("skip gives a click sound", Chiptune.Log.Count == 1 && Chiptune.Log[0] == "pop");
            cfg.AutoStart = true;
            Chiptune.Log.Clear();
            app.DebugState(Mode.Focus, RunState.Running, 1.0); app.Update(1);
            check("with auto-start the jingle still plays", Chiptune.Log.Count == 1 && Chiptune.Log[0] == "bloom");
            cfg.Sound = false;
            Chiptune.Log.Clear();
            finish(); app.Key(System.Windows.Forms.Keys.Space); Click(app, c, "skip");
            check("sound off means silence", Chiptune.Log.Count == 0);
        }

        static void WeatherTests(Action<string, bool> check)
        {
            var seen = new int[Weather.Count];
            var start = new DateTime(2026, 1, 1);
            for (int d = 0; d < 365; d++) seen[Weather.ForDay(start.AddDays(d))]++;
            bool all = true;
            foreach (int n in seen) if (n < 10) all = false;
            Console.WriteLine("  weather over a year: " + string.Join(", ", Array.ConvertAll(new[] { 0, 1, 2, 3, 4, 5 }, i => Weather.Names[i] + " " + seen[i])));
            check("every kind of weather happens during a year", all);
            check("weather stays the same all day", Weather.ForDay(new DateTime(2026, 9, 24)) == Weather.ForDay(new DateTime(2026, 9, 24, 23, 0, 0).Date));
            check("weather names parse", Weather.ParseChoice("snow") == Weather.Snow && Weather.ParseChoice("x") == Weather.Daily
                && Weather.ParseChoice(Weather.KeyOf(Weather.Fog)) == Weather.Fog && Weather.KeyOf(Weather.Daily) == "daily");

            var cfg = new Settings { Weather = Weather.Storm };
            cfg.Save();
            check("weather choice is saved", Settings.Load().Weather == Weather.Storm);
            File.Delete(Settings.FilePath);
            File.WriteAllLines(Settings.FilePath, new[] { "focus=25" });
            check("older settings get daily weather", Settings.Load().Weather == Weather.Daily);
            File.Delete(Settings.FilePath);

            var app = new App(new Settings { Sound = false }, new NullHost());
            var c = new Canvas(App.W, App.H);
            Click(app, c, "gear"); Click(app, c, "tab-look");
            Click(app, c, "weather+");
            check("weather + picks clear", app.Cfg.Weather == Weather.Clear && app.TodaysWeather == Weather.Clear);
            Click(app, c, "weather+"); Click(app, c, "weather+"); Click(app, c, "weather+");
            check("weather + steps through the kinds", app.Cfg.Weather == Weather.Snow);
            Click(app, c, "weather-");
            check("weather - steps back", app.Cfg.Weather == Weather.Rain);
            for (int i = 0; i < 4; i++) Click(app, c, "weather+");
            check("weather wraps round to daily", app.Cfg.Weather == Weather.Daily);

            // Every weather in every scene and both themes draws without touching the sill or pot.
            bool potClean = true;
            var clean = new Canvas(App.W, App.H);
            app.InSettings = false;
            foreach (Mode m in new[] { Mode.Focus, Mode.ShortBreak, Mode.LongBreak })
                for (int w = -1; w < Weather.Count; w++)
                    for (int t = 0; t < 40; t++)
                    {
                        app.Cfg.Weather = w < 0 ? Weather.Clear : w;
                        app.DebugState(m, RunState.Idle, 0);
                        app.Now = t * 0.173;
                        app.Render(w < 0 ? clean : c);
                        if (w < 0) continue;
                        for (int y = 112; y < 120; y++) // the sill and pot rows
                            for (int x = 8; x < 152; x++)
                                if (c.Get(x, y) != clean.Get(x, y)) potClean = false;
                    }
            check("weather stays outside the window", potClean);
        }

        // ---- previews ----

        static void Previews(string dir)
        {
            Directory.CreateDirectory(dir);
            var cfg = new Settings { Scale = 3, Dark = false, Plant = Plants.Daisy, Weather = Weather.Clear };
            cfg.Day = DateTime.Now.ToString("yyyy-MM-dd");
            var app = new App(cfg, new NullHost());
            var c = new Canvas(App.W, App.H);

            app.PlantCode = 0;
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
            app.RoundsDone = 4; app.PlantCode = 3;
            for (int i = 0; i < 12; i++) cfg.Garden.Add(i);
            Shot(app, c, dir, "06_long_break", Mode.LongBreak, RunState.Idle, 0, 7.7);
            app.RoundsDone = 0;
            app.InSettings = true;
            app.HoverId = "focus+";
            Shot(app, c, dir, "07_settings", Mode.Focus, RunState.Idle, 0, 1);
            app.HoverId = "plant3";
            app.SettingsTab = 1;
            Shot(app, c, dir, "08_settings_look", Mode.Focus, RunState.Idle, 0, 1);
            app.InSettings = false;
            app.SettingsTab = 0;
            app.HoverId = null;

            // Dark mode, one shot per plant.
            cfg.Dark = true;
            cfg.Garden.Clear();
            for (int sp = 0; sp < Plants.Count; sp++) cfg.Garden.Add(Plants.Code(sp, 0));
            app.RoundsDone = 1;
            Mode[] modes = { Mode.Focus, Mode.Focus, Mode.ShortBreak, Mode.Focus, Mode.LongBreak, Mode.ShortBreak };
            for (int sp = 0; sp < Plants.Count; sp++)
            {
                app.PlantCode = Plants.Code(sp, sp % Plants.ColoursOf(sp));
                app.BreakPlant = 1;
                Shot(app, c, dir, "2" + sp + "_dark_" + Plants.Keys[sp], modes[sp], modes[sp] == Mode.Focus ? RunState.Running : RunState.Idle,
                     modes[sp] == Mode.Focus ? (sp == 0 ? 1.0 : 0.6 + sp * 0.07) : 0, 2.3);
            }
            // Every weather, in a mix of scenes, both themes.
            Mode[] wModes = { Mode.Focus, Mode.Focus, Mode.ShortBreak, Mode.Focus, Mode.LongBreak, Mode.Focus };
            for (int w = 0; w < Weather.Count; w++)
                foreach (bool dark in new[] { false, true })
                {
                    cfg.Weather = w; cfg.Dark = dark;
                    app.PlantCode = Plants.Code(w % Plants.Count, 0);
                    Shot(app, c, dir, "4" + w + (dark ? "d" : "c") + "_weather_" + Weather.Keys[w], wModes[w],
                         wModes[w] == Mode.Focus ? RunState.Running : RunState.Idle, 0.8, w == Weather.Storm ? 13.05 : 3.3);
                }
            cfg.Weather = Weather.Clear; cfg.Dark = true;
            app.InSettings = true; app.SettingsTab = 1; app.HoverId = "music";
            Shot(app, c, dir, "30_dark_settings_look", Mode.Focus, RunState.Idle, 0, 1);
            app.SettingsTab = 0; app.HoverId = null;
            Shot(app, c, dir, "31_dark_settings_timer", Mode.Focus, RunState.Idle, 0, 1);
            app.InSettings = false;
            cfg.Dark = false;

            // Hover every button on every screen and report any text that doesn't fit.
            var hotsField = typeof(App).GetField("hots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (Mode m in new[] { Mode.Focus, Mode.ShortBreak, Mode.LongBreak })
                foreach (RunState s in new[] { RunState.Idle, RunState.Running, RunState.Paused })
                    foreach (int screen in new[] { -1, 0, 1 })
                    {
                        app.InSettings = screen >= 0;
                        app.SettingsTab = Math.Max(0, screen);
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

            // Growth strips: each plant at several stages, one pot per column, one row per plant.
            double[] stages = { 0, 0.02, 0.06, 0.12, 0.25, 0.4, 0.55, 0.7, 0.84, 0.88, 0.93, 0.97, 1.0 };
            var strip = new Canvas(stages.Length * 50, Plants.Count * 90);
            strip.Clear(Pal.Sky);
            for (int sp = 0; sp < Plants.Count; sp++)
                for (int i = 0; i < stages.Length; i++)
                {
                    int hx, hy, y = sp * 90 + 70;
                    Art.DrawPot(strip, i * 50 + 12, y);
                    Plants.Draw(strip, i * 50 + 24, y + 1, stages[i], Plants.Code(sp, 0), 0, false, out hx, out hy);
                }
            Save(strip, Path.Combine(dir, "10_growth.png"), 2);

            // Every plant in every colour, fully grown, plus the shelf versions.
            int most = 0;
            for (int sp = 0; sp < Plants.Count; sp++) most = Math.Max(most, Plants.ColoursOf(sp));
            var blooms = new Canvas(most * 50, Plants.Count * 90);
            blooms.Clear(Pal.Sky);
            for (int sp = 0; sp < Plants.Count; sp++)
                for (int col = 0; col < Plants.ColoursOf(sp); col++)
                {
                    int hx, hy, y = sp * 90 + 70;
                    Art.DrawPot(blooms, col * 50 + 12, y);
                    Plants.Draw(blooms, col * 50 + 24, y + 1, 1, Plants.Code(sp, col), 0, false, out hx, out hy);
                    Plants.DrawMini(blooms, col * 50 + 2, y + 5, Plants.Code(sp, col));
                }
            Save(blooms, Path.Combine(dir, "11_blooms.png"), 3);
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
