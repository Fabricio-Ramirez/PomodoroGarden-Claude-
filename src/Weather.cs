using System;
using System.Globalization;

namespace PomodoroGarden
{
    // The weather outside the window. Each day gets its own, picked from the date, so it
    // stays the same all day and changes tomorrow. Settings can also fix one kind.
    static class Weather
    {
        public const int Clear = 0, Cloudy = 1, Rain = 2, Snow = 3, Storm = 4, Fog = 5;
        public const int Count = 6;
        public const int Daily = -1; // the settings choice "a different weather every day"

        public static readonly string[] Names = { "CLEAR", "CLOUDY", "RAIN", "SNOW", "STORM", "FOG" };
        public static readonly string[] Keys = { "clear", "cloudy", "rain", "snow", "storm", "fog" };

        // Out of 100 days: 34 clear, 20 cloudy, 16 rain, 12 snow, 9 storm, 9 fog.
        static readonly int[] chance = { 34, 20, 16, 12, 9, 9 };

        public static int ForDay(DateTime day)
        {
            uint seed = (uint)(day.Year * 10000 + day.Month * 100 + day.Day);
            int roll = (int)(Art.Hash(seed * 2654435761u) % 100);
            for (int i = 0; i < Count; i++)
            {
                if (roll < chance[i]) return i;
                roll -= chance[i];
            }
            return Clear;
        }

        public static string NameOf(int choice) { return choice == Daily ? "DAILY" : Names[choice]; }

        public static int ParseChoice(string key)
        {
            for (int i = 0; i < Count; i++)
                if (Keys[i] == key) return i;
            return Daily;
        }

        public static string KeyOf(int choice) { return choice >= 0 && choice < Count ? Keys[choice] : "daily"; }

        public static string Hint(int w)
        {
            switch (w)
            {
                case Cloudy: return "CLOUDY TODAY";
                case Rain: return "RAINY TODAY. COSY STUDY TIME";
                case Snow: return "SNOWING TODAY";
                case Storm: return "STORMY TODAY. STAY INSIDE";
                case Fog: return "FOGGY TODAY";
                default: return "CLEAR SKIES TODAY";
            }
        }
    }

    // Drawing the weather over the view: tint, clouds, snow on the hills, fog, rain,
    // snowflakes and lightning. Everything stays outside, behind the window sill and pot.
    sealed partial class App
    {
        public int TodaysWeather
        {
            get
            {
                int w = Cfg.Weather;
                return w >= 0 && w < Weather.Count ? w : Weather.ForDay(DateTime.Now.Date);
            }
        }

        void DrawWeather(Canvas c)
        {
            int w = TodaysWeather;
            if (w == Weather.Clear) return;

            bool night = Mode == Mode.LongBreak;
            int grey = w == Weather.Storm ? 55 : w == Weather.Rain ? 45 : w == Weather.Snow ? 50 : w == Weather.Fog ? 30 : 35;
            c.Overcast(SX, SY, SW, SH, grey, w == Weather.Snow ? 0xE4EAF4 : 0x5A6478);

            int light = night ? Pal.Charcoal : w == Weather.Snow ? Pal.PureWhite : w == Weather.Cloudy ? Pal.White : Pal.Silver;
            int shade = night ? Pal.Shadow : w == Weather.Snow ? Pal.Silver : w == Weather.Cloudy ? Pal.Silver : Pal.Slate;
            if (w == Weather.Storm) { light = night ? Pal.Shadow : Pal.Slate; shade = night ? Pal.Ink : Pal.Charcoal; }
            if (w != Weather.Fog) DrawCloudBank(c, w == Weather.Cloudy || w == Weather.Snow ? 7 : 11, light, shade);

            switch (w)
            {
                case Weather.Rain: DrawRain(c, 55, 95, night); break;
                case Weather.Storm: DrawLightning(c); DrawRain(c, 80, 130, night); break;
                case Weather.Snow: DrawSnowCaps(c, night); DrawSnowflakes(c); break;
                case Weather.Fog: DrawFog(c, night); break;
            }
        }

        // A band of overlapping clouds drifting along the top of the sky.
        void DrawCloudBank(Canvas c, int clouds, int light, int shade)
        {
            for (int i = 0; i < clouds; i++)
            {
                uint h = Art.Hash((uint)i * 2246822519u + 3);
                int y = SY - 2 + (int)(h % 14);
                double speed = 0.8 + (h >> 16) % 10 / 10.0;
                Cloud(c, i % 3 == 2 ? Art.CloudSmall : Art.CloudBig, (h >> 4) % 200, y, speed, light, shade);
            }
        }

        // Slanted streaks, looping from top to bottom.
        void DrawRain(Canvas c, int drops, double speed, bool night)
        {
            int col = night ? Pal.Slate : Pal.Silver, tip = night ? Pal.Silver : Pal.PureWhite;
            int span = SH + 12;
            for (int i = 0; i < drops; i++)
            {
                uint h = Art.Hash((uint)i * 7919u + 17);
                double sp = speed * (0.85 + (h >> 20) % 30 / 100.0);
                int y = SY - 6 + (int)((((h >> 8) % (uint)span) + Now * sp) % span);
                int x = SX - 10 + (int)(h % (uint)(SW + 20)) + (int)((y - SY) * 0.25);
                c.Set(x - 1, y - 3, col);
                c.Set(x - 1, y - 2, col);
                c.Set(x, y - 1, col);
                c.Set(x, y, tip);
            }
        }

        // Flakes drift down slowly and sway a little.
        void DrawSnowflakes(Canvas c)
        {
            int span = SH + 6;
            for (int i = 0; i < 42; i++)
            {
                uint h = Art.Hash((uint)i * 104729u + 5);
                double sp = 9 + (h >> 24) % 9;
                double fy = (((h >> 8) % (uint)span) + Now * sp) % span;
                int y = SY - 3 + (int)fy;
                int x = SX + (int)(h % (uint)SW) + (int)Math.Round(2.5 * Math.Sin(Now * 0.9 + i * 1.7 + fy * 0.05));
                if (i % 5 == 0)
                {
                    c.Set(x, y, Pal.PureWhite);
                    c.Set(x - 1, y, Pal.White); c.Set(x + 1, y, Pal.White);
                    c.Set(x, y - 1, Pal.White); c.Set(x, y + 1, Pal.White);
                }
                else c.Set(x, y, i % 3 == 0 ? Pal.White : Pal.PureWhite);
            }
        }

        // A white rim along the top of both hill lines.
        void DrawSnowCaps(Canvas c, bool night)
        {
            int snow = night ? Pal.Silver : Pal.PureWhite, snowShade = night ? Pal.Slate : Pal.White;
            for (int x = SX; x < SX + SW; x++)
            {
                int hb = HillBack(x), hf = HillFront(x);
                if (hb < hf)
                {
                    c.Set(x, hb, snow);
                    c.Set(x, hb + 1, snowShade);
                    if (((x * 7) % 5) == 0) c.Set(x, hb + 2, snowShade);
                }
                c.Set(x, hf, snow);
                c.Set(x, hf + 1, snow);
                c.Set(x, hf + 2, snowShade);
            }
        }

        // Drifting bands of mist, thicker near the ground, in a few flat steps like pixel art.
        void DrawFog(Canvas c, bool night)
        {
            int mist = night ? 0x566C86 : 0xF4F4F4;
            for (int y = SY + 20; y < SY + SH; y++)
                for (int x = SX; x < SX + SW; x += 2)
                {
                    double ground = (y - SY - 20) / 68.0;
                    double band = Math.Sin(y * 0.22 + Math.Sin((x + Now * 4) * 0.05) * 1.5 + Now * 0.3) * 0.5 + 0.5;
                    int step = (int)((ground * 0.55 + band * 0.3) * 4);
                    if (step > 0) c.Blend(x, y, 2, 1, Math.Min(80, step * 18), mist);
                }
        }

        // Every few seconds: a flash and a jagged bolt.
        void DrawLightning(Canvas c)
        {
            const double Every = 6.5;
            int strike = (int)Math.Floor(Now / Every);
            double t = Now - strike * Every;
            if (t > 0.35 || (t > 0.1 && t < 0.18)) return; // two quick flickers
            uint h = Art.Hash((uint)strike * 31u + 7);
            c.Blend(SX, SY, SW, SH, 35, 0xFFFFFF);
            int x = SX + 20 + (int)(h % (uint)(SW - 40)), y = SY;
            int ground = SY + 58;
            while (y < ground)
            {
                c.Set(x, y, Pal.PureWhite);
                c.Set(x + 1, y, Pal.Yellow);
                y++;
                h = Art.Hash(h + (uint)y);
                if (h % 3 == 0) x += (h & 8) != 0 ? 1 : -1;
            }
        }
    }
}
