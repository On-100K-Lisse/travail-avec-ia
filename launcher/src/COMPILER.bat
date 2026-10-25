@echo off
rem Recompile DLSS5-Launcher.exe avec le compilateur C# inclus dans Windows (.NET Framework 4.x)
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
"%CSC%" /nologo /target:winexe /optimize+ /win32icon:icon.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /out:..\DLSS5-Launcher.exe Core.cs Install.cs Cleaner.cs Ui.cs
if errorlevel 1 (echo. & echo ECHEC de la compilation. & pause & exit /b 1)
echo DLSS5-Launcher.exe recompile.
pause
