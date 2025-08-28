@echo off
REM Initialize PerSpec Framework as a separate git repository
REM This script sets up the package folder as an independent git repo
REM that can be used as a git submodule or installed via Unity Package Manager
REM
REM Usage: init-package-repo.bat [remote-url]
REM Example: init-package-repo.bat https://github.com/yourusername/perspec-framework.git

setlocal enabledelayedexpansion

echo === PerSpec Framework Package Repository Setup ===
echo.

REM Change to script directory
cd /d "%~dp0"

REM Check if already a git repository
if exist ".git" (
    echo Warning: This directory is already a git repository
    set /p "REINIT=Do you want to reinitialize? This will lose git history! (y/N): "
    if /i not "!REINIT!"=="y" (
        echo Aborted
        exit /b 1
    )
    rmdir /s /q .git
)

REM Initialize git repository
echo Initializing git repository...
git init
if errorlevel 1 (
    echo Error: Failed to initialize git repository
    echo Make sure git is installed and in your PATH
    exit /b 1
)

REM Configure git for this repository
git config core.autocrlf input
git config core.safecrlf warn

REM Check for required files
if not exist ".gitattributes" (
    echo Warning: .gitattributes not found, please create it
)

if not exist ".gitignore" (
    echo Warning: .gitignore not found, please create it
)

REM Stage all files
echo Adding package files...
git add .

REM Create initial commit
echo Creating initial commit...
git commit -m "Initial commit: PerSpec Framework v1.0.0

- Unity TDD framework with UniTask async patterns
- DOTS/ECS testing support  
- SQLite-based test coordination
- Intelligent console log capture
- Background test polling
- Comprehensive test execution management"

REM Set up main branch
git branch -M main

REM Add remote if provided
if not "%~1"=="" (
    echo Adding remote origin: %1
    git remote add origin %1
    
    echo Repository initialized successfully!
    echo.
    echo To push to remote repository, run:
    echo   git push -u origin main
) else (
    echo Repository initialized locally!
    echo.
    echo To add a remote repository later, run:
    echo   git remote add origin https://github.com/yourusername/perspec-framework.git
    echo   git push -u origin main
)

echo.
echo === Setup Complete ===
echo.
echo Next steps:
echo 1. Create a repository on GitHub/GitLab/Bitbucket
echo 2. Push this package to the remote repository
echo 3. Update the parent project's .gitmodules with the correct URL
echo 4. In the parent project, run:
echo    git submodule add ^<repository-url^> Packages/com.perspec.framework
echo.
echo Users can then install your package via Unity Package Manager:
echo    Window ^> Package Manager ^> + ^> Add package from git URL...
echo    Enter: ^<repository-url^>#main

endlocal