REM other targets are:
REM 'build'
REM 'test'
REM 'test-integration'

@ECHO OFF
cls
@ECHO building...

tools\nant\bin\nant.exe test -f:Spring.Config.build > buildlog.txt

@ECHO displaying log file...
start "ignored but required placeholder window title argument" buildlog.txt
