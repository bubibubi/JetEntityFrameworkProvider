@echo Press enter to upload nuget files
@pause
SET PARAMETERS= -Verbosity detail
FOR %%F IN (lib\*.nupkg) DO nuget push %%F %PARAMETERS% >>  lib\Makesetup.log