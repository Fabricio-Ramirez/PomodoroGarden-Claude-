using System;

namespace PomodoroGarden
{
    // Everything the app draws. Each frame is redrawn from scratch and the clickable
    // areas (hot spots) are registered while drawing.
    sealed partial class App
    {
        const int SX = 8, SY = 32, SW = 144, SH = 88;   // the view through the window
        const int PotX = 67, PotY = 94, StemX = 79, SoilY = 95;

        int ModeFill { get { return Mode == Mode.Focus ? Pal.Red : Mode == Mode.ShortBreak ? Pal.Green : Pal.Blue; } }
        int ModeHover { get { return Mode == Mode.Focus ? Pal.RedHover : Mode == Mode.ShortBreak ? Pal.GreenHover : Pal.BlueHover; } }
        int ModeShine { get { return Mode == Mode.Focus ? Pal.Orange : Mode == Mode.ShortBreak ? Pal.Lime : Pal.Sky; } }
        int ModeDark { get { return Mode == Mode.Focus ? Pal.Plum : Mode == Mode.ShortBreak ? Pal.Teal : Pal.Navy; } }

        public void Render(Canvas c)
        {
            hots.Clear();
            c.NoClip();
            c.Clear(T.Bg);
            DrawTitleBar(c);
            if (InSettings) DrawSettings(c); else DrawMain(c);
            c.Outline(0, 0, W, H, T.Frame);
        }

        HotSpot AddHot(string id, int x, int y, int w, int h, Action click, string hint)
        {
            var hs = new HotSpot { Id = id, X = x, Y = y, W = w, H = h, Click = click, Hint = hint };
            hots.Add(hs);
            return hs;
        }

        // Chunky key-cap button. Returns 1 while held down so the caller can shift its label.
        int Button(Canvas c, string id, int x, int y, int w, int h, int fill, int hover, int shine, int dark,
                   Action click, string hint, bool repeat)
        {
            AddHot(id, x, y, w, h, click, hint).Repeat = repeat;
            bool hov = HoverId == id, down = hov && pressId == id;
            int off = down ? 1 : 0;
            c.Panel(x, y + 2, w, h - 2, dark, Pal.Ink);
            c.Panel(x, y + off, w, h - 2, hov ? hover : fill, Pal.Ink);
            c.HLine(x + 2, y + off + 1, w - 4, shine);
            return off;
        }

        int NeutralButton(Canvas c, string id, int x, int y, int w, int h, Action click, string hint, bool repeat)
        {
            return Button(c, id, x, y, w, h, T.Button, T.ButtonHover, T.ButtonShine, T.ButtonDark, click, hint, repeat);
        }

        void DrawBanner(Canvas c, string text, int fill, int shine, int dark)
        {
            int bw = PixelFont.Width(text) + 20, bx = (W - bw) / 2, by = 17;
            c.Panel(bx, by, bw, 12, fill, dark);
            c.HLine(bx + 2, by + 1, bw - 4, shine);
            c.Text(text, bx + 10, by + 3, dark);
            c.Text(text, bx + 10, by + 2, Pal.White);
        }

        void DrawHint(Canvas c, int y, string fallback)
        {
            if (Toast != null) { c.TextIn(Toast, 0, W, y, Pal.Spotify); return; }
            HotSpot h = Find(HoverId);
            bool hovered = h != null && h.Hint != null;
            c.TextIn(hovered ? h.Hint : fallback, 0, W, y, hovered ? T.Hint : T.Muted);
        }

        // ---- title bar ----

        void DrawTitleBar(Canvas c)
        {
            c.Rect(1, 1, W - 2, 13, T.Bar);
            c.Sprite(Art.TitleSprout, 3, 4, "LGQPp", new[] { Pal.Lime, Pal.Green, Pal.PotLight, Pal.Pot, Pal.PotDark });
            c.Text("POMODORO GARDEN", 12, 4, T.Text);
            TitleButton(c, "music", 104, Art.IconMusic, "OPEN SPOTIFY", OpenSpotify, false);
            TitleButton(c, "gear", 115, Art.IconGear, InSettings ? "BACK TO THE TIMER" : "SETTINGS", ToggleSettings, InSettings);
            TitleButton(c, "pin", 126, Art.IconPin, Cfg.OnTop ? "UNPIN WINDOW" : "KEEP WINDOW ON TOP", TogglePin, Cfg.OnTop);
            TitleButton(c, "min", 137, Art.IconMinimize, "MINIMIZE", host.Minimize, false);
            TitleButton(c, "close", 148, Art.IconClose, "CLOSE", host.Quit, false);
        }

        void TitleButton(Canvas c, string id, int x, string[] icon, string hint, Action click, bool active)
        {
            const int Y = 2;
            AddHot(id, x, Y, 11, 11, click, hint);
            bool hov = HoverId == id, down = hov && pressId == id;
            int hoverFill = id == "close" ? Pal.Red : id == "music" ? Pal.Spotify : T.ButtonShine;
            if (hov) c.Panel(x, Y, 11, 11, hoverFill, hoverFill);
            else if (active) c.Panel(x, Y, 11, 11, T.Active, T.Active);
            int col = active && !hov ? Pal.Yellow : hov ? Pal.White : id == "music" ? Pal.Spotify : T.Hint;
            c.Mask(icon, x + 2, Y + 2 + (down ? 1 : 0), col);
        }

        // ---- main screen ----

        void DrawMain(Canvas c)
        {
            string name = Mode == Mode.Focus ? "FOCUS" : Mode == Mode.ShortBreak ? "SHORT BREAK" : "LONG BREAK";
            bool flash = Now - SwitchTime < 3 && (int)((Now - SwitchTime) * 4) % 2 == 0;
            DrawBanner(c, name, flash ? ModeHover : ModeFill, flash ? Pal.White : ModeShine, ModeDark);
            DrawScene(c);
            DrawTimer(c);
            DrawProgress(c);
            DrawRounds(c);
            DrawControls(c);
            DrawShelf(c);
            DrawHint(c, 194, StatusHint());
        }

        string StatusHint()
        {
            if (Mode == Mode.Focus)
            {
                if (State == RunState.Idle) return "START TO PLANT A SEED";
                if (State == RunState.Paused) return "PAUSED. PRESS RESUME";
                return "GROWING... STAY FOCUSED";
            }
            if (State == RunState.Paused) return "BREAK PAUSED";
            if (Mode == Mode.LongBreak)
                return State == RunState.Idle ? "GREAT WORK! LONG BREAK" : "REST YOUR EYES AND MIND";
            if (State == RunState.Idle) return BreakPlant >= 1 ? "IT BLOOMED! TAKE A BREAK" : "TAKE A SHORT BREAK";
            return "STRETCH AND DRINK WATER";
        }

        void DrawTimer(Canvas c)
        {
            string s = FormatTime(TimeLeft());
            int x = (W - PixelFont.Width(s) * 3) / 2, y = 126;
            bool dim = State == RunState.Paused && (int)(Now * 2) % 2 == 1;
            c.Text(s, x + 1, y + 1, ModeDark, 3);
            c.Text(s, x, y, dim ? T.Muted : T.Text, 3);
        }

        void DrawProgress(Canvas c)
        {
            const int X = 16, Y = 153, BW = 128;
            c.Panel(X, Y, BW, 6, T.Faint, T.Faint);
            int fill = (int)Math.Round((BW - 2) * Progress);
            if (fill <= 0) return;
            c.Rect(X + 1, Y + 1, fill, 4, ModeFill);
            c.HLine(X + 1, Y + 1, fill, ModeShine);
            c.HLine(X + 1, Y + 4, fill, ModeDark);
        }

        void DrawRounds(Canvas c)
        {
            int n = Cfg.Rounds, done = Math.Min(RoundsDone, n);
            int w = n * 7 + (n - 1) * 3, x = (W - w) / 2, y = 162;
            string hint = Mode == Mode.LongBreak ? "ALL ROUNDS DONE!" : done + " OF " + n + " ROUNDS DONE";
            AddHot("rounds", x - 2, y - 1, w + 4, 9, null, hint);
            for (int i = 0; i < n; i++)
            {
                int tx = x + i * 10;
                bool current = i == done && Mode == Mode.Focus && State != RunState.Idle;
                if (i < done) c.Sprite(Art.Tomato, tx, y, Art.TomatoKeys, Art.TomatoCols);
                else c.Mask(Art.Tomato, tx, y, current && (int)(Now * 2) % 2 == 0 ? Pal.Plum : T.Faint);
            }
        }

        void DrawControls(Canvas c)
        {
            const int Y = 172;
            int o = NeutralButton(c, "reset", 26, Y, 18, 18, Reset, "RESET THIS TIMER", false);
            c.Mask(Art.IconReset, 31, Y + 5 + o, T.Text);

            string label = State == RunState.Running ? "PAUSE" : State == RunState.Paused ? "RESUME" : "START";
            string hint = State == RunState.Running ? "PAUSE  (SPACE)" : "START  (SPACE)";
            o = Button(c, "start", 50, Y, 60, 18, ModeFill, ModeHover, ModeShine, ModeDark, StartPause, hint, false);
            c.TextIn(label, 50, 60, Y + 6 + o, ModeDark);
            c.TextIn(label, 50, 60, Y + 5 + o, Pal.White);

            o = NeutralButton(c, "skip", 116, Y, 18, 18, Skip, Mode == Mode.Focus ? "SKIP TO BREAK" : "SKIP TO FOCUS", false);
            c.Mask(Art.IconSkip, 121, Y + 5 + o, T.Text);
        }

        void DrawShelf(Canvas c)
        {
            c.HLine(6, 217, W - 12, Pal.WoodLight);
            c.Rect(6, 218, W - 12, 2, Pal.Wood);
            c.HLine(6, 220, W - 12, Pal.WoodDark);

            int n = Cfg.Garden.Count;
            AddHot("garden", 6, 204, W - 12, 17, null,
                n == 0 ? "FINISH A ROUND TO GROW ONE" : n + (n == 1 ? " PLANT" : " PLANTS") + " GROWN TODAY");
            if (n == 0)
            {
                c.TextIn("TODAY'S GARDEN", 0, W, 208, T.Faint);
                return;
            }
            int show = n <= 14 ? n : 13;
            for (int i = 0; i < show; i++)
                Plants.DrawMini(c, 9 + i * 10, 206, Cfg.Garden[n - show + i]);
            if (n > 14) c.Text("+" + (n - 13), 9 + 13 * 10, 209, Pal.Yellow);
        }

        // ---- the view through the window ----

        void DrawScene(Canvas c)
        {
            c.Rect(SX - 3, SY - 3, SW + 6, SH + 6, Pal.WoodDark);
            c.Rect(SX - 2, SY - 2, SW + 4, SH + 4, Pal.Wood);
            c.HLine(SX - 2, SY - 2, SW + 4, Pal.WoodLight);
            c.VLine(SX - 2, SY - 2, SH + 4, Pal.WoodLight);
            c.Rect(SX - 1, SY - 1, SW + 2, SH + 2, Pal.WoodDark);

            AddHot("view", SX, SY, SW, SH - 8, null, Weather.Hint(TodaysWeather));
            c.Clip(SX, SY, SW, SH);
            if (Mode == Mode.Focus) DrawDay(c);
            else if (Mode == Mode.ShortBreak) DrawSunset(c);
            else DrawNight(c);
            DrawWeather(c);
            if (T.DimScene) c.Dim(SX, SY, SW, SH);

            int sill = SY + SH - 8;
            c.HLine(SX, sill, SW, Pal.WoodLight);
            c.Rect(SX, sill + 1, SW, 5, Pal.Wood);
            c.Rect(SX, sill + 6, SW, 2, Pal.WoodDark);
            c.HLine(PotX + 3, sill, 20, Pal.WoodDark);

            Art.DrawPot(c, PotX, PotY);
            double p = PlantGrowth;
            int headX, headY;
            Plants.Draw(c, StemX, SoilY, p, PlantCode, Now, State == RunState.Running && Mode == Mode.Focus, out headX, out headY);
            if (p >= 1) DrawSparkles(c, headX, headY);
            if (Mode == Mode.ShortBreak && p >= 1) DrawButterfly(c);
            if (Mode == Mode.LongBreak) DrawFireflies(c);
            c.NoClip();
        }

        void Sky(Canvas c, int[] cols)
        {
            c.Rect(SX, SY, SW, SH, cols[cols.Length - 1]);
            Art.Gradient(c, SX, SY, SW, 66, cols);
        }

        void Hills(Canvas c, int back, int backTop, int front, int frontTop)
        {
            for (int x = SX; x < SX + SW; x++)
            {
                int hb = HillBack(x);
                c.VLine(x, hb, SY + SH - hb, back);
                c.Set(x, hb, backTop);
                int hf = HillFront(x);
                c.VLine(x, hf, SY + SH - hf, front);
                c.Set(x, hf, frontTop);
            }
        }

        static int HillBack(int x) { return SY + 60 + (int)Math.Round(4 * Math.Sin(x * 0.06 + 0.8) + 2 * Math.Sin(x * 0.17)); }
        static int HillFront(int x) { return SY + 70 + (int)Math.Round(3 * Math.Sin(x * 0.09 + 2.1) + 1.5 * Math.Sin(x * 0.23 + 1)); }

        void Cloud(Canvas c, string[] sprite, double offset, int y, double speed, int light, int shade)
        {
            int span = SW + 40;
            int x = SX - 20 + (int)(((offset + Now * speed) % span + span) % span);
            c.Sprite(sprite, x, y, "WS", new[] { light, shade });
        }

        void DrawDay(Canvas c)
        {
            Sky(c, new[] { Pal.Blue, Pal.Sky, Pal.Cyan });
            c.Circle(127, 50, 7.5, Pal.Yellow);
            c.Circle(125.5, 48.5, 3.5, Pal.Cream);
            Cloud(c, Art.CloudBig, 10, 42, 1.3, Pal.White, Pal.Silver);
            Cloud(c, Art.CloudSmall, 90, 58, 0.8, Pal.White, Pal.Silver);
            Cloud(c, Art.CloudSmall, 150, 36, 1.8, Pal.White, Pal.Silver);
            Hills(c, Pal.Teal, Pal.Green, Pal.Green, Pal.Lime);
        }

        void DrawSunset(Canvas c)
        {
            Sky(c, new[] { Pal.Plum, Pal.Red, Pal.Orange });
            const double CX = 44, CY = 92, R = 13;
            for (int y = (int)(CY - R); y <= CY + R; y++)
            {
                if (y > CY - 9 && (y - (int)CY) % 3 == 0) continue; // retro stripes
                for (int x = (int)(CX - R); x <= CX + R; x++)
                {
                    double dx = x + 0.5 - CX, dy = y + 0.5 - CY;
                    if (dx * dx + dy * dy <= R * R) c.Set(x, y, y < CY - 7 ? Pal.Cream : Pal.Yellow);
                }
            }
            Cloud(c, Art.CloudBig, 60, 44, 1.0, Pal.PinkLight, Pal.Pink);
            Cloud(c, Art.CloudSmall, 0, 60, 0.6, Pal.PinkLight, Pal.Pink);
            Hills(c, Pal.Plum, Pal.Red, Pal.Charcoal, Pal.Plum);
        }

        void DrawNight(Canvas c)
        {
            Sky(c, new[] { Pal.Ink, Pal.Navy, Pal.Navy });
            uint seed = 12345;
            for (int i = 0; i < 26; i++)
            {
                seed = Art.Hash(seed + (uint)i);
                int x = SX + 2 + (int)(seed % (SW - 4));
                int y = SY + 2 + (int)((seed >> 12) % 52);
                double tw = Math.Sin(Now * (1.2 + (seed >> 24) % 5 * 0.3) + i);
                if (tw < -0.7) continue;
                c.Set(x, y, tw > 0.3 ? Pal.PureWhite : Pal.Silver);
                if (i % 6 == 0 && tw > 0.6)
                {
                    c.Set(x - 1, y, Pal.Slate); c.Set(x + 1, y, Pal.Slate);
                    c.Set(x, y - 1, Pal.Slate); c.Set(x, y + 1, Pal.Slate);
                }
            }
            for (int y = 40; y <= 57; y++)
                for (int x = 118; x <= 135; x++)
                {
                    double ax = x + 0.5 - 126, ay = y + 0.5 - 48, bx = x + 0.5 - 129.5, by = y + 0.5 - 45;
                    if (ax * ax + ay * ay <= 49 && bx * bx + by * by > 36) c.Set(x, y, Pal.Cream);
                }
            Hills(c, Pal.Shadow, Pal.Charcoal, Pal.Ink, Pal.Charcoal);
        }

        void DrawSparkles(Canvas c, int cx, int cy)
        {
            int[] cols = { Pal.PureWhite, Pal.Yellow };
            for (int i = 0; i < 3; i++)
            {
                double ph = Now * 1.4 + i * 0.33;
                int cycle = (int)Math.Floor(ph);
                double f = ph - cycle;
                uint hsh = Art.Hash((uint)(cycle * 3 + i));
                double angle = (hsh % 360) * Math.PI / 180, dist = 12 + (hsh >> 9) % 6; // a ring around the flower
                int x = cx + (int)Math.Round(Math.Cos(angle) * dist), y = cy + (int)Math.Round(Math.Sin(angle) * dist * 0.8);
                string[] spr = f < 0.3 || f > 0.7 ? Art.SparkSmall : Art.SparkBig;
                c.Sprite(spr, x - spr[0].Length / 2, y - spr.Length / 2, "WY", cols);
            }
        }

        void DrawButterfly(Canvas c)
        {
            int x = 78 + (int)Math.Round(26 * Math.Sin(Now * 0.55));
            int y = 58 + (int)Math.Round(12 * Math.Sin(Now * 1.1 + 1));
            string[] spr = (int)(Now * 7) % 2 == 0 ? Art.ButterflyOpen : Art.ButterflyShut;
            c.Sprite(spr, x, y, "WSB", new[] { Pal.PureWhite, Pal.Yellow, Pal.Ink });
        }

        void DrawFireflies(Canvas c)
        {
            for (int i = 0; i < 6; i++)
            {
                double t = Now + i * 7.3;
                int x = SX + 12 + i * 23 + (int)Math.Round(7 * Math.Sin(t * 0.5 + i));
                int y = SY + 56 + (int)Math.Round(9 * Math.Sin(t * 0.37 + i * 2.1));
                double glow = Math.Sin(t * 1.9 + i * 1.7);
                if (glow < -0.3) continue;
                if (glow > 0.4)
                {
                    c.Set(x - 1, y, Pal.Teal); c.Set(x + 1, y, Pal.Teal);
                    c.Set(x, y - 1, Pal.Teal); c.Set(x, y + 1, Pal.Teal);
                }
                c.Set(x, y, glow > 0.4 ? Pal.Yellow : Pal.Lime);
            }
        }

        // ---- settings screen ----

        void DrawSettings(Canvas c)
        {
            SettingsTabButton(c, "tab-timer", 0, 28, "TIMER", "TIMER LENGTHS AND SOUND");
            SettingsTabButton(c, "tab-look", 1, 82, "LOOK", "PLANT, THEME, WEATHER, MUSIC");
            if (SettingsTab == 0) DrawTimerSettings(c); else DrawLookSettings(c);

            int o = Button(c, "done", 50, 188, 60, 16, Pal.Green, Pal.GreenHover, Pal.Lime, Pal.Teal, ToggleSettings, "BACK TO THE TIMER  (ESC)", false);
            c.TextIn("DONE", 50, 60, 193 + o, Pal.Teal);
            c.TextIn("DONE", 50, 60, 192 + o, Pal.White);

            DrawHint(c, 210, "SAVED AUTOMATICALLY");
        }

        void SettingsTabButton(Canvas c, string id, int tab, int x, string label, string hint)
        {
            bool on = SettingsTab == tab;
            int o = Button(c, id, x, 16, 50, 13,
                on ? Pal.Slate : T.Button, on ? Pal.Silver : T.ButtonHover,
                on ? Pal.Silver : T.ButtonShine, on ? Pal.Charcoal : T.ButtonDark,
                () => SettingsTab = tab, hint, false);
            if (on) c.TextIn(label, x, 50, 20 + o, Pal.Charcoal);
            c.TextIn(label, x, 50, 19 + o, on ? Pal.White : T.Hint);
        }

        void DrawTimerSettings(Canvas c)
        {
            c.Text("TIMER (MINUTES)", 8, 34, T.Muted);
            Stepper(c, "focus", 43, "FOCUS", () => Cfg.Focus, v => { Cfg.Focus = v; DurationsChanged(); }, 1, 180, "", true, "MINUTES OF FOCUS PER ROUND");
            Stepper(c, "short", 56, "SHORT BREAK", () => Cfg.Short, v => { Cfg.Short = v; DurationsChanged(); }, 1, 60, "", true, "MINUTES FOR A SHORT BREAK");
            Stepper(c, "long", 69, "LONG BREAK", () => Cfg.Long, v => { Cfg.Long = v; DurationsChanged(); }, 1, 90, "", true, "MINUTES FOR A LONG BREAK");
            Stepper(c, "rounds", 82, "LONG BREAK EVERY", () => Cfg.Rounds, v => Cfg.Rounds = v, 1, 10, "", false, "FOCUS ROUNDS PER LONG BREAK");

            c.Text("PRESETS", 8, 99, T.Muted);
            Preset(c, "p1", 8, 108, 25, 5, 15, "CLASSIC POMODORO");
            Preset(c, "p2", 58, 108, 50, 10, 20, "LONGER STUDY BLOCKS");
            Preset(c, "p3", 108, 108, 90, 20, 30, "DEEP WORK SESSIONS");

            c.Text("OPTIONS", 8, 127, T.Muted);
            Toggle(c, "sound", 136, "SOUND", Cfg.Sound, () => { Cfg.Sound = !Cfg.Sound; if (Cfg.Sound) Chiptune.Bloom(); }, "CHIME WHEN A TIMER ENDS");
            Toggle(c, "auto", 149, "AUTO-START NEXT", Cfg.AutoStart, () => Cfg.AutoStart = !Cfg.AutoStart, "NO NEED TO PRESS START");
        }

        // Plant picker: a tile per plant, then appearance and music.
        void DrawLookSettings(Canvas c)
        {
            c.Text("PLANT", 8, 34, T.Muted);
            c.Text(Plants.NameOf(Cfg.Plant), 152 - PixelFont.Width(Plants.NameOf(Cfg.Plant)), 34, Pal.Yellow);
            for (int i = 0; i <= Plants.Count; i++)
            {
                int choice = i - 1, x = 8 + i * 21;
                PlantTile(c, "plant" + i, x, 43, choice);
            }

            c.Text("APPEARANCE", 8, 78, T.Muted);
            Toggle(c, "dark", 87, "DARK MODE", Cfg.Dark, () => Cfg.Dark = !Cfg.Dark, "EASY ON THE EYES AT NIGHT");
            Toggle(c, "top", 100, "ALWAYS ON TOP", Cfg.OnTop, TogglePin, "KEEP ABOVE OTHER WINDOWS");
            Stepper(c, "size", 113, "WINDOW SIZE", () => Cfg.Scale, v => { Cfg.Scale = v; host.ApplyScale(v); }, 1, Math.Max(1, host.MaxScale), "X", false, "ZOOM OF THIS WINDOW");
            WeatherPicker(c, 126);

            c.Text("MUSIC", 8, 143, T.Muted);
            c.Text("SPOTIFY", 8, 154, T.Text);
            int o = Button(c, "spotify", 111, 152, 41, 12, Pal.Spotify, unchecked((int)0xFF3BD16F), Pal.Lime, Pal.Teal,
                           OpenSpotify, string.IsNullOrEmpty(Cfg.Spotify) ? "OPEN THE SPOTIFY APP" : "OPEN YOUR SAVED PLAYLIST", false);
            c.TextIn("OPEN", 111, 41, 155 + o, Pal.Teal);
            c.TextIn("OPEN", 111, 41, 154 + o, Pal.White);
        }

        // Weather: DAILY (random each day) or one fixed kind. Click or scroll to change.
        void WeatherPicker(Canvas c, int y)
        {
            Action<int> change = d =>
            {
                int n = Weather.Count + 1; // DAILY plus every kind
                int i = (Cfg.Weather + 1 + d + n) % n;
                Cfg.Weather = i - 1;
            };
            string hint = Cfg.Weather == Weather.Daily ? "NEW WEATHER EVERY DAY" : "ALWAYS " + Weather.Names[Cfg.Weather];
            AddHot("weather-row", 4, y, 84, 12, () => change(1), hint).Wheel = change;
            c.Text("WEATHER", 8, y + 2, T.Text);
            int o = NeutralButton(c, "weather-", 89, y, 11, 12, () => change(-1), hint, false);
            c.Mask(Art.IconMinus, 92, y + 3 + o, T.Text);
            c.TextIn(Weather.NameOf(Cfg.Weather), 100, 41, y + 2, Pal.Yellow);
            o = NeutralButton(c, "weather+", 141, y, 11, 12, () => change(1), hint, false);
            c.Mask(Art.IconPlus, 144, y + 3 + o, T.Text);
        }

        void PlantTile(Canvas c, string id, int x, int y, int choice)
        {
            bool on = Cfg.Plant == choice;
            string hint = choice == Plants.Surprise ? "NEW PLANT EVERY ROUND" : "GROW A " + Plants.Names[choice];
            int o = Button(c, id, x, y, 18, 28,
                on ? Pal.Green : T.Button, on ? Pal.GreenHover : T.ButtonHover,
                on ? Pal.Lime : T.ButtonShine, on ? Pal.Teal : T.ButtonDark,
                () => ChoosePlant(choice), hint, false);
            if (choice == Plants.Surprise)
            {
                c.Text("?", x + 4, y + 8 + o, Pal.Teal, 2);
                c.Text("?", x + 4, y + 7 + o, Pal.Yellow, 2);
                return;
            }
            // Show each plant in its first colour, or in the colour being grown right now.
            int code = PlantCode >= 0 && Plants.SpeciesOf(PlantCode) == choice ? PlantCode : Plants.Code(choice, 0);
            Plants.DrawMini(c, x + 2, y + 3 + o, code, 2);
        }

        void Stepper(Canvas c, string id, int y, string label, Func<int> get, Action<int> set,
                     int min, int max, string suffix, bool minutes, string hint)
        {
            Action<int> change = d => set(Math.Max(min, Math.Min(max, get() + d)));
            AddHot(id, 4, y, W - 8, 12, null, hint).Wheel = change;
            c.Text(label, 8, y + 2, T.Text);

            int v = get();
            int o = NeutralButton(c, id + "-", 109, y, 11, 12, () => change(-Step(minutes)), hint, true);
            c.Mask(Art.IconMinus, 112, y + 3 + o, v > min ? T.Text : T.Muted);
            c.TextIn(v + suffix, 120, 21, y + 2, Pal.Yellow);
            o = NeutralButton(c, id + "+", 141, y, 11, 12, () => change(Step(minutes)), hint, true);
            c.Mask(Art.IconPlus, 144, y + 3 + o, v < max ? T.Text : T.Muted);
        }

        void Toggle(Canvas c, string id, int y, string label, bool on, Action flip, string hint)
        {
            AddHot(id + "-row", 4, y, 112, 12, flip, hint);
            c.Text(label, 8, y + 2, T.Text);
            int o = Button(c, id, 121, y, 31, 12,
                on ? Pal.Green : T.Button, on ? Pal.GreenHover : T.ButtonHover,
                on ? Pal.Lime : T.ButtonShine, on ? Pal.Teal : T.ButtonDark, flip, hint, false);
            c.TextIn(on ? "ON" : "OFF", 121, 31, y + 2 + o, on ? Pal.White : T.Hint);
        }

        void Preset(Canvas c, string id, int x, int y, int focus, int brk, int lng, string hint)
        {
            bool active = Cfg.Focus == focus && Cfg.Short == brk && Cfg.Long == lng;
            int o = Button(c, id, x, y, 44, 12,
                active ? Pal.Green : T.Button, active ? Pal.GreenHover : T.ButtonHover,
                active ? Pal.Lime : T.ButtonShine, active ? Pal.Teal : T.ButtonDark,
                () => { Cfg.Focus = focus; Cfg.Short = brk; Cfg.Long = lng; DurationsChanged(); },
                hint, false);
            c.TextIn(focus + "/" + brk, x, 44, y + 2 + o, active ? Pal.White : T.Hint);
        }
    }
}
