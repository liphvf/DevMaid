@echo off

for /r %%f in (*.csproj) do (
    echo ==========================
    echo Formatando %%f...
    dotnet format "%%f" --severity info
    echo.
)

echo Finalizado!
pause