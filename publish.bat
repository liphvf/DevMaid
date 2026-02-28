@echo off
setlocal

set PROJECT=DevMaid.csproj
set RUNTIME=win-x64
set CONFIG=Release
set OUTPUT=.\publish

echo Publishing DevMaid (AOT - %RUNTIME%)...
echo.

dotnet publish %PROJECT% ^
    -c %CONFIG% ^
    -r %RUNTIME% ^
    -o %OUTPUT% ^
    /p:PublishAot=true

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Publish failed.
    exit /b %ERRORLEVEL%
)

echo.
echo [OK] Published to %OUTPUT%
endlocal
