@ECHO OFF

REM The following directory is for .NET 4.0
set DOTNETFX2=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319
set PATH=%PATH%;%DOTNETFX2%

echo Installing IEPPAMS Win Service...
echo ---------------------------------------------------
net stop "POSBridgePOS"
C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil /u "%~dp0POSBridgePOS.exe"

echo ---------------------------------------------------
pause
echo Done.