using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("Pomodoro Garden")]
[assembly: AssemblyDescription("A pixel-art pomodoro timer that grows a plant while you focus.")]
[assembly: AssemblyProduct("Pomodoro Garden")]
[assembly: AssemblyCopyright("2026")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace PomodoroGarden
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool first;
            using (var mutex = new Mutex(true, "PomodoroGarden.SingleInstance", out first))
            {
                if (!first)
                {
                    BringOtherInstanceForward();
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Chiptune.Init();
                Application.Run(new MainForm());
            }
        }

        // Only one garden at a time: a second launch just shows the running window.
        static void BringOtherInstanceForward()
        {
            Process me = Process.GetCurrentProcess();
            foreach (Process p in Process.GetProcessesByName(me.ProcessName))
            {
                if (p.Id == me.Id || p.MainWindowHandle == IntPtr.Zero) continue;
                Native.ShowWindow(p.MainWindowHandle, Native.SW_RESTORE);
                Native.SetForegroundWindow(p.MainWindowHandle);
                return;
            }
        }
    }
}
