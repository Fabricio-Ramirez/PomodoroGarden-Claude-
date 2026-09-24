using System;
using System.Runtime.InteropServices;

namespace PomodoroGarden
{
    static class Native
    {
        public const int WM_NCLBUTTONDOWN = 0xA1, HTCAPTION = 2;
        public const int SW_SHOWNOACTIVATE = 4, SW_RESTORE = 9;

        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool FlashWindowEx(ref FLASHWINFO info);

        [StructLayout(LayoutKind.Sequential)]
        struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        // Flash the taskbar button until the user looks at the window.
        public static void Flash(IntPtr hwnd)
        {
            var info = new FLASHWINFO();
            info.cbSize = (uint)Marshal.SizeOf(typeof(FLASHWINFO));
            info.hwnd = hwnd;
            info.dwFlags = 3 | 12; // FLASHW_ALL | FLASHW_TIMERNOFG
            FlashWindowEx(ref info);
        }
    }

    // Shows timer progress on the taskbar button: green while running, yellow when paused.
    sealed class Taskbar
    {
        [ComImport, Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface ITaskbarList3
        {
            void HrInit();
            void AddTab(IntPtr hwnd);
            void DeleteTab(IntPtr hwnd);
            void ActivateTab(IntPtr hwnd);
            void SetActiveAlt(IntPtr hwnd);
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);
            void SetProgressValue(IntPtr hwnd, ulong completed, ulong total);
            void SetProgressState(IntPtr hwnd, int flags);
        }

        [ComImport, Guid("56FDF344-FD6D-11d0-958A-006097C9A090"), ClassInterface(ClassInterfaceType.None)]
        class TaskbarList { }

        ITaskbarList3 list;
        bool failed;
        int lastState = -1, lastValue = -1, lastSent;

        public void Show(IntPtr hwnd, RunState state, double progress)
        {
            if (failed) return;
            try
            {
                if (list == null)
                {
                    list = (ITaskbarList3)new TaskbarList();
                    list.HrInit();
                }
                int s = state == RunState.Running ? 2 : state == RunState.Paused ? 8 : 0;
                int v = (int)(progress * 1000);
                bool stale = Environment.TickCount - lastSent > 3000; // re-send now and then (e.g. after Explorer restarts)
                if (s != lastState || stale)
                {
                    list.SetProgressState(hwnd, s);
                    lastState = s;
                    lastValue = -1;
                    lastSent = Environment.TickCount;
                }
                if (s != 0 && v != lastValue)
                {
                    list.SetProgressValue(hwnd, (ulong)v, 1000);
                    lastValue = v;
                }
            }
            catch { failed = true; }
        }
    }
}
