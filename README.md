# Pomodoro Garden 🌱

A pixel-art pomodoro timer for Windows. A plant grows in its pot while you focus and blooms when the round ends. Each bloom goes on your "today's garden" shelf.

## Run it

Double-click `release\PomodoroGarden.exe`. You don't need to install anything; it runs on Windows 10 and 11 as they come.

- **START / PAUSE**: the big button, or the **Space** key
- **↻**: resets the current timer. **▶|** skips to the next one.
- **⚙** (top right): opens settings, where you can set focus, short break and long break minutes, how many rounds come before a long break, sound, auto-start, always on top, window size, and quick presets (25/5, 50/10, 90/20)
- **Pin** (top right): keeps the window above other windows
- To move the window, drag it by any empty area. To change a number in settings, hold **−/+** or use the mouse wheel.

When a timer ends, the app plays a short chiptune, the window pops back up and the taskbar button flashes. The taskbar button also shows the timer's progress.

Settings and today's garden are saved in `PomodoroGarden.ini` next to the .exe. The app writes nothing else: no registry, no AppData. To reset everything, delete that file.

> If you share the .exe, Windows SmartScreen may warn on the first run because the file isn't code-signed: choose *More info → Run anyway*.

## Build

Run `build.cmd`. It uses the C# compiler that ships with Windows (.NET Framework 4.x), so there's nothing to install. The source is in `src/`. `tools/DevTool.cs` is a helper that isn't shipped: it renders preview images, generates `assets/app.ico`, and runs a timer-rules test.
