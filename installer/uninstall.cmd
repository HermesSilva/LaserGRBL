@echo off
rem ---------------------------------------------------------------------------
rem LaserGRBL uninstaller. Installed next to the application and registered
rem as UninstallString in Add/Remove Programs.
rem ---------------------------------------------------------------------------
setlocal EnableExtensions

set "APPNAME=LaserGRBL"
set "UNKEY=HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\LaserGRBL"
set "INSTALLDIR=%~dp0"
if "%INSTALLDIR:~-1%"=="\" set "INSTALLDIR=%INSTALLDIR:~0,-1%"

rem ---------------------------------------------------------------------------
rem elevate if needed
rem ---------------------------------------------------------------------------
net session >nul 2>&1
if errorlevel 1 (
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -ArgumentList '%INSTALLDIR%' -Verb RunAs -Wait"
    endlocal
    exit /b
)

rem when relaunched from the temp copy, the install folder comes as argument
if not "%~1"=="" set "INSTALLDIR=%~1"

rem ---------------------------------------------------------------------------
rem run from temp, so the install folder can be deleted while this script runs
rem ---------------------------------------------------------------------------
if /i not "%~dp0"=="%TEMP%\" (
    copy /y "%~f0" "%TEMP%\lasergrbl-uninstall.cmd" >nul
    "%TEMP%\lasergrbl-uninstall.cmd" "%INSTALLDIR%"
    endlocal
    exit /b
)

echo Uninstalling %APPNAME% from "%INSTALLDIR%"
echo.

choice /c YN /n /m "Remove LaserGRBL from this computer? [Y/N] "
if errorlevel 2 (
    endlocal
    exit /b
)

tasklist /fi "imagename eq LaserGRBL.exe" | find /i "LaserGRBL.exe" >nul
if not errorlevel 1 (
    taskkill /im LaserGRBL.exe >nul 2>&1
    timeout /t 3 /nobreak >nul
    taskkill /f /im LaserGRBL.exe >nul 2>&1
)

rem shortcuts
set "STARTMENU=%ProgramData%\Microsoft\Windows\Start Menu\Programs"
del /f /q "%STARTMENU%\%APPNAME%.lnk" >nul 2>&1
del /f /q "%STARTMENU%\%APPNAME% (disable opengl).lnk" >nul 2>&1
del /f /q "%STARTMENU%\%APPNAME% (soft opengl).lnk" >nul 2>&1
del /f /q "%PUBLIC%\Desktop\%APPNAME%.lnk" >nul 2>&1

rem file associations
reg delete "HKCR\.nc" /f >nul 2>&1
reg delete "HKCR\LaserGRBL gcode file" /f >nul 2>&1
reg delete "HKCR\.zbn" /f >nul 2>&1
reg delete "HKCR\LaserGRBL zipped button" /f >nul 2>&1
reg delete "HKCR\.lps" /f >nul 2>&1
reg delete "HKCR\LaserGRBL Project file" /f >nul 2>&1

rem add remove programs entry
reg delete "%UNKEY%" /f >nul 2>&1

rem program files
if exist "%INSTALLDIR%\LaserGRBL.exe" rd /s /q "%INSTALLDIR%" >nul 2>&1

echo.
if exist "%INSTALLDIR%\LaserGRBL.exe" (
    echo Some files could not be removed from "%INSTALLDIR%".
) else (
    echo %APPNAME% has been removed.
    echo User settings in "%APPDATA%\%APPNAME%" were kept.
)
echo.
pause

endlocal
exit /b 0
