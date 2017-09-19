@echo ========================================================= > Output\MakeSetup.log
call "C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Tools\VsDevCmd.bat"
msbuild ..\JetEntityFrameworkProvider.sln /p:Configuration=Release /p:Platform="Any CPU" >> lib\MakeSetup.log
