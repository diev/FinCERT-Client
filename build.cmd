@echo off
for %%i in (.) do (
 set repo=%%~nxi
)

call :build FeedsAPI FinCERT-Client
exit /b 0

:build
if exist bin rd /s /q bin
if /%1/==// goto :eof
if not exist %1 goto next
set src=%1

echo build %1

for %%i in (%src%\*.csproj) do (
 set prj=%%~dpnxi
 set app=%%~ni
)

rem Build an app with many dlls (default)
rem dotnet publish %prj% -o bin

rem Build a single-file app when NET Desktop runtime required 
dotnet publish %prj% -o bin -r win-x64 -p:PublishSingleFile=true --no-self-contained

rem Build a single-file app when no runtime required
rem dotnet publish %prj% -o bin -r win-x64 -p:PublishSingleFile=true

for /f "tokens=3 delims=<>" %%v in ('findstr "<Version>" %prj%') do set version=%%v
for /f "tokens=3 delims=<>" %%v in ('findstr "<Description>" %prj%') do set description=%%v

for /f "tokens=3,3" %%a in ('reg query "hkcu\control panel\international" /v sshortdate') do set sfmt=%%a
for /f "tokens=3,3" %%a in ('reg query "hkcu\control panel\international" /v slongdate') do set lfmt=%%a

reg add "hkcu\control panel\international" /v sshortdate /t reg_sz /d yyyy-MM-dd /f >nul
reg add "hkcu\control panel\international" /v slongdate /t reg_sz /d yyyy-MM-dd /f >nul

set ymd=%date%

reg add "hkcu\control panel\international" /v sshortdate /t reg_sz /d %sfmt% /f >nul
reg add "hkcu\control panel\international" /v slongdate /t reg_sz /d %lfmt% /f >nul

if /%AppZip%/==// set pack=%app%-v%version%.zip
if not /%AppZip%/==// set pack=%AppZip%
if exist %pack% del %pack%

call :version_txt > bin\version.txt

"C:\Program Files\7-Zip\7z.exe" a %pack% LICENSE *.md *.sln *.cmd bin\
"C:\Program Files\7-Zip\7z.exe" a %pack% -r -x!.* -x!bin -x!obj -x!PublishProfiles -x!*.user %src%\

set store=G:\BankApps\AppStore
if exist %store% copy /y %pack% %store%

:next
shift
goto build

:lower
echo>%Temp%\%2
for /f %%f in ('dir /b/l %Temp%\%2') do set %1=%%f
del %Temp%\%2
goto :eof

:version_txt
call :lower repol %repo%
echo %app%
echo %description%
echo.
echo Version: v%version%
echo Date:    %ymd%
echo.
echo Requires SDK .NET 8.0 to build
echo Requires .NET Desktop Runtime 8.0 to run
echo Download from https://dotnet.microsoft.com/download
echo.
echo Run once to create %app%.config.json
echo and correct it
echo.
echo https://github.com/diev/%repo%
echo https://gitverse.ru/diev/%repo%
echo https://gitflic.ru/project/diev/%repol%
goto :eof
