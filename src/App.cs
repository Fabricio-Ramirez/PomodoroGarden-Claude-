using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace PomodoroGarden
{
    enum Mode { Focus, ShortBreak, LongBreak }
    enum RunState { Idle, Running, Paused }

    // What the app needs from the window hosting it.
    interface IHost
    {
        int MaxScale { get; }
        void ApplyScale(int scale);
        void ApplyTopMost(bool on);
        void Minimize();
        void Quit();
        void Alert();
        bool SpotifyInstalled { get; }
        bool Open(string target); // opens a link with its app; false if that failed
    }

    // A clickable / hoverable rectangle, registered while a frame is drawn.
    sealed class HotSpot
    {
        public string Id, Hint;
        public int X, Y, W, H;
        public Action Click;
        public Action<int> Wheel;
        public bool Repeat;

        public bool Contains(int x, int y) { return x >= X && y >= Y && x < X + W && y < Y + H; }
    }

    // Timer rules and input handling. Drawing is in AppDraw.cs, the window in MainForm.cs.
    sealed partial class App
    {
        public const int W = 160, H = 224;

        public readonly Settings Cfg;
        readonly IHost host;
        readonly Random rng = new Random();

        public Mode Mode = Mode.Focus;
        public RunState State = RunState.Idle;
        public double Total;          // length of the current timer, seconds
        double remaining;             // seconds left while idle or paused
        DateTime endUtc;              // when the running timer reaches zero
        public int RoundsDone;        // focus rounds finished in the current cycle
        public int PlantCode = -1;    // the plant being grown (see Plants.Code), -1 before the first one
        public double BreakPlant;     // growth shown in the pot during a break (1 = in bloom)
        public bool InSettings;
        public int SettingsTab;       // 0 = timer, 1 = look
        public double Now;            // animation clock, seconds
        public double SwitchTime = -100;
        string toast;                 // short message shown in the hint line, e.g. after opening Spotify
        double toastUntil;

        readonly List<HotSpot> hots = new List<HotSpot>();
        public string HoverId;
        string pressId;
        int mouseX = -1, mouseY = -1;
        int repeatCount;
        double nextRepeat;
        DateTime nextDayCheck;

        public App(Settings cfg, IHost host)
        {
            Cfg = cfg;
            this.host = host;
            RollDay();
            SetMode(Mode.Focus);
        }

        // ---- timer ----

        public double TimeLeft()
        {
            if (State != RunState.Running) return remaining;
            return Math.Max(0, (endUtc - DateTime.UtcNow).TotalSeconds);
        }

        public double Progress
        {
            get { return Total > 0 ? Math.Max(0, Math.Min(1, 1 - TimeLeft() / Total)) : 0; }
        }

        // Growth of the potted plant: live while focusing, frozen during a break.
        public double PlantGrowth
        {
            get { return Mode == Mode.Focus ? Progress : BreakPlant; }
        }

        public string WindowTitle
        {
            get
            {
                if (State == RunState.Idle) return "Pomodoro Garden";
                string mode = Mode == Mode.Focus ? "Focus" : Mode == Mode.ShortBreak ? "Short break" : "Long break";
                return (State == RunState.Paused ? "Paused " : "") + FormatTime(TimeLeft()) + " - " + mode;
            }
        }

        public static string FormatTime(double seconds)
        {
            int s = (int)Math.Ceiling(seconds - 1e-6);
            return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }

        int Minutes(Mode m)
        {
            return m == Mode.Focus ? Cfg.Focus : m == Mode.ShortBreak ? Cfg.Short : Cfg.Long;
        }

        void SetMode(Mode m)
        {
            Mode = m;
            Total = remaining = Minutes(m) * 60.0;
            State = RunState.Idle;
            if (m == Mode.Focus) PickPlant();
        }

        public Theme T { get { return Cfg.Dark ? Theme.Dark : Theme.Classic; } }

        // The chosen plant in a random colour, or with "surprise me" a different plant each round.
        void PickPlant()
        {
            int species = Cfg.Plant;
            if (species < 0 || species >= Plants.Count)
            {
                int prev = PlantCode >= 0 ? Plants.SpeciesOf(PlantCode) : -1;
                do species = rng.Next(Plants.Count); while (species == prev);
            }
            int n = Plants.ColoursOf(species), next, tries = 0;
            do next = Plants.Code(species, rng.Next(n)); while (next == PlantCode && n > 1 && tries++ < 20);
            PlantCode = next;
        }

        // Picking a plant in settings swaps the one in the pot, unless it's already grown (break time).
        void ChoosePlant(int choice)
        {
            Cfg.Plant = choice;
            if (Mode == Mode.Focus) PickPlant();
        }

        void OpenSpotify()
        {
            string target = Spotify.Target(Cfg.Spotify, host.SpotifyInstalled);
            bool ok = host.Open(target);
            bool inApp = target.StartsWith("spotify:");
            if (!ok && inApp)
            {
                inApp = false;
                ok = host.Open(Spotify.Target(Cfg.Spotify, false));
            }
            ShowToast(!ok ? "COULD NOT OPEN SPOTIFY" : inApp ? "OPENING SPOTIFY..." : "OPENING SPOTIFY IN BROWSER");
        }

        void ShowToast(string text)
        {
            toast = text;
            toastUntil = Now + 3;
        }

        public string Toast { get { return toast != null && Now < toastUntil ? toast : null; } }

        void Start()
        {
            if (remaining <= 0) remaining = Total;
            endUtc = DateTime.UtcNow.AddSeconds(remaining);
            State = RunState.Running;
        }

        void StartPause()
        {
            if (State == RunState.Running)
            {
                remaining = TimeLeft();
                State = RunState.Paused;
                return;
            }
            if (State == RunState.Idle && Cfg.Sound) Chiptune.Pop();
            Start();
        }

        void Reset()
        {
            Total = remaining = Minutes(Mode) * 60.0;
            State = RunState.Idle;
        }

        // Skipping never counts as a finished round.
        void Skip()
        {
            if (Mode == Mode.Focus)
            {
                BreakPlant = Progress;
                SetMode(Mode.ShortBreak);
            }
            else
            {
                if (Mode == Mode.LongBreak) RoundsDone = 0;
                SetMode(Mode.Focus);
            }
            if (Cfg.AutoStart) Start();
        }

        void Complete()
        {
            RollDay();
            SwitchTime = Now;
            if (Mode == Mode.Focus)
            {
                Cfg.Garden.Add(PlantCode);
                RoundsDone++;
                BreakPlant = 1;
                if (Cfg.Sound) Chiptune.Bloom();
                SetMode(RoundsDone >= Cfg.Rounds ? Mode.LongBreak : Mode.ShortBreak);
            }
            else
            {
                if (Mode == Mode.LongBreak) RoundsDone = 0;
                if (Cfg.Sound) Chiptune.Wake();
                SetMode(Mode.Focus);
            }
            if (Cfg.AutoStart) Start();
            Cfg.Save();
            host.Alert();
        }

        // New lengths apply right away to a timer that hasn't started, otherwise from the next one.
        void DurationsChanged()
        {
            if (State == RunState.Idle) Reset();
        }

        // The garden shelf shows today's plants only.
        void RollDay()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (Cfg.Day != today)
            {
                Cfg.Day = today;
                Cfg.Garden.Clear();
            }
            nextDayCheck = DateTime.Now.AddSeconds(30);
        }

        public void Update(double now)
        {
            Now = now;
            if (State == RunState.Running && TimeLeft() <= 0) Complete();
            if (DateTime.Now >= nextDayCheck) RollDay();

            // Holding a +/- button keeps changing the value.
            if (pressId != null && pressId == HoverId && now >= nextRepeat)
            {
                HotSpot h = Find(pressId);
                if (h != null && h.Repeat && h.Click != null)
                {
                    repeatCount++;
                    h.Click();
                    nextRepeat = now + (repeatCount > 6 ? 0.05 : 0.1);
                }
            }
        }

        int Step(bool minutes)
        {
            return minutes && pressId != null && repeatCount > 20 ? 5 : 1;
        }

        void ToggleSettings()
        {
            InSettings = !InSettings;
            if (!InSettings) Cfg.Save();
            else SettingsTab = 0;
        }

        void TogglePin()
        {
            Cfg.OnTop = !Cfg.OnTop;
            host.ApplyTopMost(Cfg.OnTop);
        }

        // Used by the preview tool to put the app into a given state.
        public void DebugState(Mode m, RunState s, double progress)
        {
            Mode = m;
            Total = Minutes(m) * 60.0;
            remaining = Total * (1 - progress);
            State = s;
            if (s == RunState.Running) endUtc = DateTime.UtcNow.AddSeconds(remaining);
        }

        // ---- input ----

        HotSpot HotAt(int x, int y)
        {
            for (int i = hots.Count - 1; i >= 0; i--)
                if (hots[i].Contains(x, y)) return hots[i];
            return null;
        }

        HotSpot Find(string id)
        {
            if (id == null) return null;
            foreach (HotSpot h in hots)
                if (h.Id == id) return h;
            return null;
        }

        public bool OverClickable
        {
            get
            {
                HotSpot h = HotAt(mouseX, mouseY);
                return h != null && h.Click != null;
            }
        }

        public void MouseMove(int x, int y)
        {
            mouseX = x; mouseY = y;
            HotSpot h = HotAt(x, y);
            HoverId = h == null ? null : h.Id;
        }

        public void MouseLeave()
        {
            mouseX = mouseY = -1;
            HoverId = null;
        }

        // Returns true when the press lands on empty space, so the window should be dragged.
        public bool MouseDown(int x, int y)
        {
            MouseMove(x, y);
            HotSpot h = HotAt(x, y);
            if (h == null || h.Click == null) return true;
            pressId = h.Id;
            if (h.Repeat)
            {
                repeatCount = 0;
                h.Click();
                nextRepeat = Now + 0.4;
            }
            return false;
        }

        public void MouseUp(int x, int y)
        {
            MouseMove(x, y);
            HotSpot h = HotAt(x, y);
            if (h != null && h.Id == pressId && !h.Repeat && h.Click != null) h.Click();
            pressId = null;
        }

        public void Wheel(int x, int y, int dir)
        {
            for (int i = hots.Count - 1; i >= 0; i--)
                if (hots[i].Wheel != null && hots[i].Contains(x, y))
                {
                    hots[i].Wheel(dir);
                    return;
                }
        }

        public bool Key(Keys k)
        {
            if (k == Keys.Space && !InSettings) { StartPause(); return true; }
            if ((k == Keys.Escape || k == Keys.Enter) && InSettings) { ToggleSettings(); return true; }
            if ((k == Keys.Left || k == Keys.Right) && InSettings) { SettingsTab = k == Keys.Left ? 0 : 1; return true; }
            return false;
        }
    }
}
