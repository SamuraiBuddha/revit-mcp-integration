@echo off
echo Building RevitMcpServer...
echo.

REM Build the project
cd RevitMcpServer
dotnet build --configuration Release

if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)

echo.
echo Build successful!
echo.

REM Set the Revit addins path
set REVIT_ADDINS=%APPDATA%\Autodesk\Revit\Addins\2024

echo Creating Revit addins directory if it doesn't exist...
if not exist "%REVIT_ADDINS%" mkdir "%REVIT_ADDINS%"

echo.
echo Copying files to Revit addins folder...
echo Destination: %REVIT_ADDINS%

REM Copy the DLL and dependencies (updated path - no net48 subdirectory)
xcopy /Y "bin\Release\*.dll" "%REVIT_ADDINS%\"
xcopy /Y "bin\Release\*.pdb" "%REVIT_ADDINS%\"

REM Copy the .addin manifest file
copy /Y "RevitMcpServer.addin" "%REVIT_ADDINS%\"

echo.
echo Deployment complete!
echo.
echo Next steps:
echo 1. Start Revit 2024
echo 2. Check logs at: %LOCALAPPDATA%\RevitMcpServer\logs
echo 3. Test the endpoints:
echo    - curl http://localhost:7891/api/health
echo    - curl http://localhost:7891/api/revit/version
echo.
pause
