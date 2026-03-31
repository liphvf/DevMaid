@echo off

set EXCLUDES=IDE0052 IDE0060 IDE1006 CA1000 CA1002 CA1031 CA1305 CA1307 CA1308 CA1310 CA1502 CA1506 CA1508 CA1819 CA1849 CA1859 CA1866 CA1869 CA1873 CA2000 CA2100 CA2201 CA2227 CA2254 CS0103 MSTEST0058

for /r %%f in (*.csproj) do (
    echo ==========================
    echo Formatando %%f...
    dotnet format "%%f" --severity info --exclude-diagnostics %EXCLUDES% 2>nul
    echo.
)

echo Finalizado!
pause