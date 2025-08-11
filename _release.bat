@echo off
setlocal

rem === パス設定 ===
set SRC=%~dp0
set BLD=%SRC%bin\Release\net8.0-windows
set DST=%SRC%site.dodoneko.imeindicator.sdPlugin
set OUT=%SRC%dist

rem === ビルド ===
dotnet build -c Release "%SRC%ImeIndicator.csproj" || goto :eof

rem === パッケージ用フォルダ作成 ===
if not exist "%DST%" mkdir "%DST%"
if not exist "%DST%\net8.0-windows" mkdir "%DST%\net8.0-windows"
if not exist "%DST%\images" mkdir "%DST%\images"
if not exist "%OUT%" mkdir "%OUT%"

rem === 旧ファイル掃除（フォルダは消さない） ===
del /Q "%DST%\net8.0-windows\*" >nul 2>&1
del /Q "%DST%\images\*" >nul 2>&1
del /Q "%DST%\manifest.json" >nul 2>&1

rem === 配置 ===
xcopy "%BLD%\*" "%DST%\net8.0-windows\" /Y /I /Q /R /D >nul
xcopy "%SRC%images\*" "%DST%\images\" /Y /I /Q /R /D >nul
xcopy "%SRC%manifest.json" "%DST%\" /Y /Q >nul

rem === バリデート → パック ===
call :which streamdeck || (echo [ERROR] streamdeck CLI が見つかりません & goto :eof)

streamdeck validate "%DST%" || (echo [ERROR] validate でエラー & goto :eof)
streamdeck pack "%DST%" -o "%OUT%" --version 1.0.0 || (echo [ERROR] pack に失敗 & goto :eof)

for %%F in ("%OUT%\*.streamDeckPlugin") do set PKG=%%~fF
echo.
echo ✅ パッケージ作成: %PKG%
echo    ダブルクリックでインストールできます。
echo.

goto :eof

:which
where %1 >nul 2>&1
exit /b %ERRORLEVEL%
