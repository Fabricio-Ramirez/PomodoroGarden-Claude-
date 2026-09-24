# Pomodoro Garden 🌱

A pixel-art pomodoro timer for Windows. A plant grows in its pot while you focus and blooms when the round ends. Each bloom goes on your "today's garden" shelf.

## What's new in 2.0

- **Choose your plant**: daisy, rose, tulip, sunflower, little tree, cactus, or *surprise me* for a different one every round. Each comes in several colours (the tree grows apples, oranges, lemons or blossoms).
- **Dark mode**: near-black window and a dimmed view outside, so only your plant stays bright. It's on by default; switch it in **⚙ → LOOK**.
- **Spotify button**: the green **♫** in the title bar opens Spotify. If the Spotify app isn't installed, it opens the web player in your browser.

## Run it

Download `PomodoroGarden.exe` from the [latest release](https://github.com/Fabricio-Ramirez/PomodoroGarden-Claude-/releases/latest) and double-click it. You don't need to install anything; it runs on Windows 10 and 11 as they come.

- **START / PAUSE**: the big button, or the **Space** key
- **↻**: resets the current timer. **▶|** skips to the next one.
- **♫** (top right): opens Spotify
- **⚙** (top right): opens settings. The **TIMER** tab has focus, short break and long break minutes, how many rounds come before a long break, quick presets (25/5, 50/10, 90/20), sound and auto-start. The **LOOK** tab has the plant picker, dark mode, always on top, window size and a Spotify button. The **←/→** keys switch tabs.
- **Pin** (top right): keeps the window above other windows
- To move the window, drag it by any empty area. To change a number in settings, hold **−/+** or use the mouse wheel.

When a timer ends, the app plays a short chiptune, the window pops back up and the taskbar button flashes. The taskbar button also shows the timer's progress.

Settings and today's garden are saved in `PomodoroGarden.ini` next to the .exe. The app writes nothing else: no registry, no AppData. To reset everything, delete that file. Settings from version 1 carry over.

To have the music button open a specific playlist or album, copy its link in Spotify (*Share → Copy link*) and paste it after `spotify=` in `PomodoroGarden.ini`, for example `spotify=https://open.spotify.com/playlist/37i9dQZF1DX8Uebhn9wzrS`. Only Spotify links are accepted.

> If you share the .exe, Windows SmartScreen may warn on the first run because the file isn't code-signed: choose *More info → Run anyway*.

## Build

Run `build.cmd`. It uses the C# compiler that ships with Windows (.NET Framework 4.x), so there's nothing to install. The source is in `src/`. `tools/DevTool.cs` is a helper that isn't shipped: it renders preview images, generates `assets/app.ico`, and runs the tests (`devtool cycle`: timer rules, plants, Spotify links, the settings file, and clicking through the buttons).

GitHub Actions also builds the app on Windows for every push and runs the tests. You can download the `.exe` from the run's **Artifacts** section on the Actions tab. To publish a release, raise the version in `src/Program.cs` (`AssemblyVersion`) and update the "What's new" section above. When that reaches `main` or `main-embdfr` and the tests pass, the workflow creates the GitHub Release (tag `v` + version), with that section as the release notes and the `.exe` attached. Pushing a tag such as `v2.1.0` works too.
