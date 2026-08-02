@echo off
rem ---------------------------------------------------------------------------
rem LaserGRBL machine wide installer payload.
rem Run by the IExpress self extracting package from a temporary folder.
rem Version is replaced by build-installer.ps1
rem ---------------------------------------------------------------------------
setlocal EnableExtensions

set "APPNAME=LaserGRBL"
set "APPVERSION=__VERSION__"
set "PUBLISHER=HermesSilva (LaserGRBL fork)"
set "UNKEY=HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\LaserGRBL"

rem use the native Program Files even when running from a 32 bit host process
if defined ProgramW6432 (set "PFROOT=%ProgramW6432%") else (set "PFROOT=%ProgramFiles%")
set "INSTALLDIR=%PFROOT%\%APPNAME%"

rem ---------------------------------------------------------------------------
rem standard materials belong to the user profile, so copy them before elevating
rem (after elevation %APPDATA% would point to the administrator profile)
rem ---------------------------------------------------------------------------
if not "%~1"=="/elevated" (
    if exist "%~dp0StandardMaterials.psh" (
        if not exist "%APPDATA%\%APPNAME%" mkdir "%APPDATA%\%APPNAME%" >nul 2>&1
        copy /y "%~dp0StandardMaterials.psh" "%APPDATA%\%APPNAME%\" >nul 2>&1
    )
)

rem ---------------------------------------------------------------------------
rem elevate: installing for all users needs administrative rights
rem ---------------------------------------------------------------------------
net session >nul 2>&1
if errorlevel 1 (
    echo Requesting administrative rights...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -ArgumentList '/elevated' -Verb RunAs -Wait"
    if errorlevel 1 (
        echo.
        echo Installation cancelled: administrative rights are required.
        echo.
        pause
    )
    endlocal
    exit /b
)

echo Installing %APPNAME% %APPVERSION% to "%INSTALLDIR%"
echo.

rem ---------------------------------------------------------------------------
rem close a running instance, otherwise the executable cannot be replaced
rem ---------------------------------------------------------------------------
tasklist /fi "imagename eq LaserGRBL.exe" | find /i "LaserGRBL.exe" >nul
if not errorlevel 1 (
    echo Closing the running instance...
    taskkill /im LaserGRBL.exe >nul 2>&1
    timeout /t 3 /nobreak >nul
    taskkill /f /im LaserGRBL.exe >nul 2>&1
)

rem ---------------------------------------------------------------------------
rem files
rem ---------------------------------------------------------------------------
if not exist "%INSTALLDIR%" mkdir "%INSTALLDIR%"
if errorlevel 1 goto :failed

for %%F in (LaserGRBL.exe LaserGRBL.exe.config StandardButtons.zbn StandardMaterials.psh lasergrblfile.ico zippedbutton.ico uninstall.cmd) do (
    if exist "%~dp0%%F" (
        copy /y "%~dp0%%F" "%INSTALLDIR%\" >nul
        if errorlevel 1 goto :failed
    )
)

rem ---------------------------------------------------------------------------
rem shortcuts, for all users
rem ---------------------------------------------------------------------------
set "STARTMENU=%ProgramData%\Microsoft\Windows\Start Menu\Programs"
call :shortcut "%STARTMENU%\%APPNAME%.lnk" ""
call :shortcut "%STARTMENU%\%APPNAME% (disable opengl).lnk" "nogl"
call :shortcut "%STARTMENU%\%APPNAME% (soft opengl).lnk" "swgl"
call :shortcut "%PUBLIC%\Desktop\%APPNAME%.lnk" ""

rem ---------------------------------------------------------------------------
rem file associations
rem ---------------------------------------------------------------------------
call :associate ".nc"  "LaserGRBL gcode file"    "GCode file for laser engraving"        "lasergrblfile.ico"
call :associate ".zbn" "LaserGRBL zipped button" "This file contains LaserGRBL buttons"  "zippedbutton.ico"
call :associate ".lps" "LaserGRBL Project file"  "Project file for laser engraving"      "lasergrblfile.ico"

rem ---------------------------------------------------------------------------
rem add remove programs entry
rem ---------------------------------------------------------------------------
reg add "%UNKEY%" /v DisplayName /d "%APPNAME%" /f >nul
reg add "%UNKEY%" /v DisplayVersion /d "%APPVERSION%" /f >nul
reg add "%UNKEY%" /v Publisher /d "%PUBLISHER%" /f >nul
reg add "%UNKEY%" /v DisplayIcon /d "%INSTALLDIR%\LaserGRBL.exe" /f >nul
reg add "%UNKEY%" /v InstallLocation /d "%INSTALLDIR%" /f >nul
reg add "%UNKEY%" /v UninstallString /d "\"%INSTALLDIR%\uninstall.cmd\"" /f >nul
reg add "%UNKEY%" /v NoModify /t REG_DWORD /d 1 /f >nul
reg add "%UNKEY%" /v NoRepair /t REG_DWORD /d 1 /f >nul

rem let explorer notice the new associations
powershell -NoProfile -Command "Add-Type -Namespace W -Name S -MemberDefinition '[DllImport(\"shell32.dll\")] public static extern void SHChangeNotify(int e, uint f, IntPtr a, IntPtr b);'; [W.S]::SHChangeNotify(0x8000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)" >nul 2>&1

echo.
echo %APPNAME% %APPVERSION% installed successfully.
echo Location: %INSTALLDIR%
echo.
choice /c YN /n /m "Start LaserGRBL now? [Y/N] "
if not errorlevel 2 start "" "%INSTALLDIR%\LaserGRBL.exe"

endlocal
exit /b 0

rem ---------------------------------------------------------------------------
:shortcut
rem %~1 = link path, %~2 = command line arguments
powershell -NoProfile -ExecutionPolicy Bypass -Command "$s=(New-Object -ComObject WScript.Shell).CreateShortcut('%~1'); $s.TargetPath='%INSTALLDIR%\LaserGRBL.exe'; $s.Arguments='%~2'; $s.WorkingDirectory='%INSTALLDIR%'; $s.IconLocation='%INSTALLDIR%\LaserGRBL.exe,0'; $s.Save()" >nul 2>&1
exit /b

rem ---------------------------------------------------------------------------
:associate
rem %~1 = extension, %~2 = prog id, %~3 = description, %~4 = icon file
reg add "HKCR\%~1" /ve /d "%~2" /f >nul
reg add "HKCR\%~2" /ve /d "%~3" /f >nul
reg add "HKCR\%~2\Shell\Open\Command" /ve /d "\"%INSTALLDIR%\LaserGRBL.exe\" \"%%1\"" /f >nul
reg add "HKCR\%~2\DefaultIcon" /ve /d "%INSTALLDIR%\%~4,0" /f >nul
exit /b

rem ---------------------------------------------------------------------------
:failed
echo.
echo Installation failed while copying files to "%INSTALLDIR%".
echo.
pause
endlocal
exit /b 1
