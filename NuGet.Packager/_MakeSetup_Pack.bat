@echo Deleting all package files >> Output\Makesetup.log
del /q lib\*.nupkg >> lib\Makesetup.log
nuget pack JetEntityFrameworkProvider.nuspec -version %VERSION% -OutputDirectory lib >> lib\Makesetup.log
