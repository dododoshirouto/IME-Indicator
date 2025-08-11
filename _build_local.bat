@echo off
setlocal

rem === パス設定 ===
set SRC=%~dp0
set BLD=%SRC%bin\Release\net8.0-windows
set DST=%APPDATA%\Elgato\StreamDeck\Plugins\site.dodoneko.imeindicator.sdPlugin

rem === ビルド ===
dotnet build -c Release "%SRC%ImeIndicator.csproj" || goto :eof

rem === フォルダ作成 ===
if not exist "%DST%" mkdir "%DST%"
if not exist "%DST%\net8.0-windows" mkdir "%DST%\net8.0-windows"
if not exist "%DST%\images" mkdir "%DST%\images"

rem === Stream Deckを止めてロック解除（失敗しても続行） ===
taskkill /IM StreamDeck.exe /F >nul 2>&1

rem === 旧ファイル掃除（フォルダは消さない） ===
del /Q "%DST%\net8.0-windows\*" >nul 2>&1
del /Q "%DST%\images\*" >nul 2>&1
del /Q "%DST%\manifest.json" >nul 2>&1

rem === 配置（/Y:上書き /I:宛先はディレクトリ扱い /Q:静かに /R:読み取り専用上書き /D:新しいものだけ） ===
xcopy "%BLD%\*" "%DST%\net8.0-windows\" /Y /I /Q /R /D >nul
xcopy "%SRC%images\*" "%DST%\images\" /Y /I /Q /R /D >nul
xcopy "%SRC%manifest.json" "%DST%\" /Y /Q >nul

rem === Stream Deck起動（インストール場所違うなら手で起動でもOK） ===
if exist "%ProgramFiles%\Elgato\StreamDeck\StreamDeck.exe" (
  start "" "%ProgramFiles%\Elgato\StreamDeck\StreamDeck.exe"
)

echo Done.

:eof
endlocal
exit /b 0