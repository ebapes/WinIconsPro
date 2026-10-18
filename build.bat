@echo off
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set WPF_LIB=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF

echo ==============================================
echo Building WinIcons Pro
echo ==============================================

if not exist "%CSC%" (
    echo Error: csc.exe not found at %CSC%
    pause
    exit /b 1
)

"%CSC%" /nologo /target:winexe /win32manifest:app.manifest /win32icon:assets\app_icon.ico /out:WinIconsPro.exe /lib:"%WPF_LIB%" /r:PresentationCore.dll /r:PresentationFramework.dll /r:WindowsBase.dll /r:System.Xaml.dll /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll src\Native\ShellApi.cs src\Core\IcoBuilder.cs src\Core\ImageProcessor.cs src\Core\DesktopScanner.cs src\Core\BackupManager.cs src\Core\IconPatcher.cs src\UI\MainWindow.cs src\Program.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ==============================================
    echo [SUCCESS] WinIconsPro.exe built successfully!
    echo ==============================================
) else (
    echo.
    echo [ERROR] Build failed! Check errors above.
)
