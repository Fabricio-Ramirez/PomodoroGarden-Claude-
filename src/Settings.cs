using System;
using System.Collections.Generic;
using System.IO;

namespace PomodoroGarden
{
    // User preferences, stored in a small .ini file next to the .exe (portable: no registry, no AppData).
    sealed class Settings
    {
        public int Focus = 25, Short = 5, Long = 15, Rounds = 4, Scale = 0;
        public bool Sound = true, AutoStart = false, OnTop = false;
        public int X = int.MinValue, Y = int.MinValue;
        public string Day = "";
        public readonly List<int> Garden = new List<int>(); // flower colours grown today

        public static string FilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PomodoroGarden.ini"); }
        }

        public static Settings Load()
        {
            var s = new Settings();
            try
            {
                if (File.Exists(FilePath))
                {
                    foreach (string line in File.ReadAllLines(FilePath))
                    {
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        string key = line.Substring(0, eq).Trim().ToLowerInvariant();
                        string val = line.Substring(eq + 1).Trim();
                        int n;
                        bool isNum = int.TryParse(val, out n);
                        switch (key)
                        {
                            case "focus": if (isNum) s.Focus = n; break;
                            case "short": if (isNum) s.Short = n; break;
                            case "long": if (isNum) s.Long = n; break;
                            case "rounds": if (isNum) s.Rounds = n; break;
                            case "scale": if (isNum) s.Scale = n; break;
                            case "sound": s.Sound = val == "1"; break;
                            case "autostart": s.AutoStart = val == "1"; break;
                            case "ontop": s.OnTop = val == "1"; break;
                            case "x": if (isNum) s.X = n; break;
                            case "y": if (isNum) s.Y = n; break;
                            case "day": s.Day = val; break;
                            case "garden":
                                foreach (string part in val.Split(','))
                                {
                                    int g;
                                    if (int.TryParse(part, out g)) s.Garden.Add(g);
                                }
                                break;
                        }
                    }
                }
            }
            catch { } // unreadable file: just use the defaults

            s.Focus = Clamp(s.Focus, 1, 180);
            s.Short = Clamp(s.Short, 1, 60);
            s.Long = Clamp(s.Long, 1, 90);
            s.Rounds = Clamp(s.Rounds, 1, 10);
            s.Scale = Clamp(s.Scale, 0, 12);
            return s;
        }

        public void Save()
        {
            try
            {
                File.WriteAllLines(FilePath, new[]
                {
                    "; Pomodoro Garden settings",
                    "focus=" + Focus,
                    "short=" + Short,
                    "long=" + Long,
                    "rounds=" + Rounds,
                    "sound=" + (Sound ? 1 : 0),
                    "autostart=" + (AutoStart ? 1 : 0),
                    "ontop=" + (OnTop ? 1 : 0),
                    "scale=" + Scale,
                    "x=" + X,
                    "y=" + Y,
                    "day=" + Day,
                    "garden=" + string.Join(",", Garden),
                });
            }
            catch { } // read-only folder: settings just won't persist
        }

        public static int Clamp(int v, int min, int max) { return v < min ? min : v > max ? max : v; }
    }
}
