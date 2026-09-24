using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PomodoroGarden
{
    // Borderless window that hosts the App: forwards input, and scales the
    // low-res canvas up to the screen with crisp nearest-neighbour pixels.
    sealed class MainForm : Form, IHost
    {
        readonly App app;
        readonly Canvas canvas = new Canvas(App.W, App.H);
        readonly Bitmap frame = new Bitmap(App.W, App.H, PixelFormat.Format32bppRgb);
        readonly Timer ticker = new Timer();
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly Taskbar taskbar = new Taskbar();
        int scale;
        Point normalLocation;

        public MainForm()
        {
            Settings cfg = Settings.Load();
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            KeyPreview = true;
            Text = "Pomodoro Garden";
            BackColor = Color.FromArgb(cfg.Dark ? Theme.Dark.Bg : Theme.Classic.Bg);
            Icon icon = LoadIcon();
            if (icon != null) Icon = icon;

            app = new App(cfg, this);

            Rectangle wa = Screen.FromPoint(cfg.X == int.MinValue ? Point.Empty : new Point(cfg.X, cfg.Y)).WorkingArea;
            int max = MaxScaleFor(wa);
            scale = cfg.Scale > 0 ? Math.Min(cfg.Scale, max) : Math.Max(1, Math.Min(max, (int)Math.Round(wa.Height * 0.55 / App.H)));
            cfg.Scale = scale;
            ClientSize = new Size(App.W * scale, App.H * scale);

            var saved = new Rectangle(cfg.X, cfg.Y, Width, Height);
            bool visible = false;
            if (cfg.X != int.MinValue)
                foreach (Screen s in Screen.AllScreens)
                    if (s.WorkingArea.Contains(new Point(saved.X + 20, saved.Y + 10))) visible = true;
            Location = visible ? saved.Location
                : new Point(wa.Left + (wa.Width - Width) / 2, wa.Top + (wa.Height - Height) / 2);
            normalLocation = Location;
            TopMost = cfg.OnTop;

            ticker.Interval = 40;
            ticker.Tick += delegate { Tick(); };
            ticker.Start();
        }

        static Icon LoadIcon()
        {
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PomodoroGarden.app.ico");
                return stream == null ? null : new Icon(stream);
            }
            catch { return null; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x00020000;      // WS_MINIMIZEBOX: taskbar click can minimise/restore a borderless window
                cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
                return cp;
            }
        }

        void Tick()
        {
            app.Update(clock.Elapsed.TotalSeconds);
            string title = app.WindowTitle;
            if (Text != title) Text = title;
            taskbar.Show(Handle, app.State, app.Progress);
            if (WindowState != FormWindowState.Minimized) Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            app.Render(canvas);
            BitmapData data = frame.LockBits(new Rectangle(0, 0, App.W, App.H), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            for (int y = 0; y < App.H; y++)
                Marshal.Copy(canvas.Px, y * App.W, data.Scan0 + y * data.Stride, App.W);
            frame.UnlockBits(data);

            Graphics g = e.Graphics;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.CompositingMode = CompositingMode.SourceCopy;
            g.DrawImage(frame, new Rectangle(0, 0, App.W * scale, App.H * scale));
        }

        // ---- input ----

        protected override void OnMouseMove(MouseEventArgs e)
        {
            app.MouseMove(e.X / scale, e.Y / scale);
            Cursor = app.OverClickable ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            app.MouseLeave();
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (app.MouseDown(e.X / scale, e.Y / scale))
            {
                Native.ReleaseCapture();
                Native.SendMessage(Handle, Native.WM_NCLBUTTONDOWN, (IntPtr)Native.HTCAPTION, IntPtr.Zero);
            }
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            app.MouseUp(e.X / scale, e.Y / scale);
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            app.Wheel(e.X / scale, e.Y / scale, Math.Sign(e.Delta));
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (app.Key(e.KeyCode))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                Invalidate();
            }
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (WindowState == FormWindowState.Normal) normalLocation = Location;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ticker.Stop();
            app.Cfg.X = normalLocation.X;
            app.Cfg.Y = normalLocation.Y;
            app.Cfg.Save();
            base.OnFormClosing(e);
        }

        // ---- IHost ----

        static int MaxScaleFor(Rectangle wa)
        {
            return Math.Max(1, Math.Min((wa.Height - 16) / App.H, (wa.Width - 16) / App.W));
        }

        public int MaxScale
        {
            get { return MaxScaleFor((IsHandleCreated ? Screen.FromHandle(Handle) : Screen.PrimaryScreen).WorkingArea); }
        }

        public void ApplyScale(int s)
        {
            s = Math.Max(1, Math.Min(s, MaxScale));
            app.Cfg.Scale = s;
            if (s == scale) return;
            scale = s;
            ClientSize = new Size(App.W * s, App.H * s);
            Rectangle wa = Screen.FromHandle(Handle).WorkingArea;
            Left = Math.Max(wa.Left, Math.Min(Left, wa.Right - Width));
            Top = Math.Max(wa.Top, Math.Min(Top, wa.Bottom - Height));
            Invalidate();
        }

        public void ApplyTopMost(bool on) { TopMost = on; }

        public void Minimize() { WindowState = FormWindowState.Minimized; }

        public void Quit() { Close(); }

        // The desktop app registers the spotify: link type; the Microsoft Store app adds a
        // "Spotify.exe" alias in WindowsApps. Only reads: the app never writes to the registry.
        public bool SpotifyInstalled
        {
            get
            {
                try
                {
                    using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"spotify\shell\open\command"))
                        if (key != null) return true;
                }
                catch { }
                try
                {
                    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    return System.IO.File.Exists(System.IO.Path.Combine(local, @"Microsoft\WindowsApps\Spotify.exe"))
                        || System.IO.File.Exists(System.IO.Path.Combine(roaming, @"Spotify\Spotify.exe"));
                }
                catch { return false; }
            }
        }

        public bool Open(string target)
        {
            try
            {
                using (Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })) { }
                return true;
            }
            catch { return false; }
        }

        // A timer finished: pop the window back up (without stealing focus) and flash the taskbar.
        public void Alert()
        {
            if (WindowState == FormWindowState.Minimized) Native.ShowWindow(Handle, Native.SW_SHOWNOACTIVATE);
            if (Form.ActiveForm != this) Native.Flash(Handle);
        }
    }
}
