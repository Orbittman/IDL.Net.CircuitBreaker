@echo Off
set config=%1
if "%config%" == "" (
   set config=Release
)

set version=
if not "%PackageVersion%" == "" (
   set version=-Version %GitVersion.ClassicVersionWithTag%
)
REM Package restore
call %NuGet% restore IDL.MapsApi.Net.Tests\packages.config -OutputDirectory %cd%\packages -NonInteractive

REM Build
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild IDL.MapsApi.Net.sln /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false
if not "%errorlevel%"=="0" goto failure

REM Unit tests
"%GallioEcho%" IDL.MapsApi.Net.Tests\bin\%config%\IDL.MapsApi.Net.Tests.dll
if not "%errorlevel%"=="0" goto failure

REM Package
mkdir Build
call %nuget% pack "IDL.MapsApi.Net\IDL.MapsApi.Net.nuspec" -symbols -o Build -p Configuration=%config% %version%
if not "%errorlevel%"=="0" goto failure

:success
exit 0

:failure
exit -1
