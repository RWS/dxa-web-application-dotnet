@ECHO OFF
:Init
SET EnableNuGetPackageRestore=true
SET location=%~dp0
SET name=%~n0
IF %1.==. GOTO Usage
:Command
"%location%nuget.exe" restore %1
GOTO End
:Usage
ECHO usage: %name% ^<solution file^>
:End