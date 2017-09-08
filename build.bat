@echo Off
set config=%1
if "%config%" == "" (
   set config=Release
)

set version=
if not "%PackageVersion%" == "" (
   set version=%GitVersion.NuGetVersionV2%
)

REM Package restore
call %NuGet% restore CircuitBreaker.Tests\packages.config -OutputDirectory %cd%\packages -NonInteractive

REM Build
call "%msbuild%" IDL.Net.CircuitBreaker.sln /p:Configuration="%config%" /m /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false
if not "%errorlevel%"=="0" goto failure

REM Unit tests
"%GallioEcho%" CircuitBreaker.Tests\bin\%config%\CircuitBreaker.Tests.dll
if not "%errorlevel%"=="0" goto failure

REM Package
mkdir Build
call %nuget% pack "CircuitBreaker\CircuitBreaker.nuspec" -Version %version% -symbols -o Build -p Configuration=%config%
if not "%errorlevel%"=="0" goto failure

:success
exit 0

:failure
exit -1

:success
exit 0

:failure
exit -1
