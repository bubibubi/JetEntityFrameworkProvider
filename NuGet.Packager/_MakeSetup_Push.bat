@echo Press enter to upload nuget files
@pause
SET PARAMETERS= -Verbosity detail -Source https://www.nuget.org/api/v2/package/
FOR %%F IN (lib\*.nupkg) DO nuget push %%F %PARAMETERS% >>  lib\Makesetup.log