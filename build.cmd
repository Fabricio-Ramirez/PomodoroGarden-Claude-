@echo off
rem Builds release\PomodoroGarden.exe with the C# compiler that ships with Windows (.NET Framework 4.x).
rem Nothing needs to be installed.
setlocal
cd /d "%~dp0"

set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Could not find csc.exe from .NET Framework 4.
  exit /b 1
)

if not exist release mkdir release

"%CSC%" /nologo /target:winexe /optimize+ ^
  /out:release\PomodoroGarden.exe ^
  /win32icon:assets\app.ico ^
  /win32manifest:assets\app.manifest ^
  /resource:assets\app.ico,PomodoroGarden.app.ico ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll ^
  src\*.cs
if errorlevel 1 exit /b 1

echo Built release\PomodoroGarden.exe
