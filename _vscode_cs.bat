@echo off
setlocal

set "filename=%~nx0"

echo %filename% | findstr /i "js" >nul
if %errorlevel%==0 (
    code ./ --profile js-plane
    goto :eof
)

echo %filename% | findstr /i "python" >nul
if %errorlevel%==0 (
    code ./ --profile python
    goto :eof
)

echo %filename% | findstr /i "arduino" >nul
if %errorlevel%==0 (
    code ./ --profile Arduino
    goto :eof
)

echo %filename% | findstr /i "unity" >nul
if %errorlevel%==0 (
    code ./ --profile Unity
    goto :eof
)

echo %filename% | findstr /i "cs" >nul
if %errorlevel%==0 (
    code ./ --profile CSharp
    goto :eof
)

code ./

endlocal