=== This is an old document. Actually DB First does not work ===

DB First require a full provider setup (ADO.Net provider setup).
ADO.Net provider setup for MS Access is included into Entity Framework Provider (you don't need an ADO.Net provider for MS Access if you use only MS Access with ADO.Net; in this case you can use OLEDB ADO.Net provider directly).  
  
Setup  
1. If you are using VS 2012 or 2013 install "Entity Framework 6 Tools for Visual Studio 2012 & 2013" from here  
[https://www.microsoft.com/en-us/download/details.aspx?id=40762](https://www.microsoft.com/en-us/download/details.aspx?id=40762)  
  
2. Sign the JetEntityFrameworkProvider  
I usually enable it from visual studio. You can also find a test key inside the project.  
  
3. Register the ADO.Net provider for MS Access (JetEntityFrameworkProvider.dll) in the GAC. I usually do this inside visual studio (post build event) but you need to run Visual Studio as administrator to do this.  
My Visual Studio events are:  
Pre-build:  
"%ProgramFiles%\Microsoft SDKs\Windows\v8.0A\Bin\NETFX 4.0 Tools\gacutil.exe" /u "$(TargetName)"  
  
Post-build:  
"%ProgramFiles%\Microsoft SDKs\Windows\v8.0A\Bin\NETFX 4.0 Tools\gacutil.exe" /if "$(TargetPath)"  
  
4. Register the ADO.Net provider for MS Access in the machine.config.  
You need to edit the right machine.config. The right machine.config is the machine.config used during Db First process by Visual Studio to retrieve informations. In my case is the 32 bits v4.0 so is here  
C:\Windows\Microsoft.NET\Framework\v4.0.30319\Config  
  
in configuration\system.data\DbProviderFactories  
add this line  
  
```
     <add name="JetEntityFrameworkProvider" invariant="JetEntityFrameworkProvider" description="JetEntityFrameworkProvider" type="JetEntityFrameworkProvider.JetProviderFactory, JetEntityFrameworkProvider, Version=1.0.0.0, Culture=neutral, PublicKeyToken=756cf6beb8fe7b41" />
```

5. Register DDEX provider (the Visual Studio provider).  
To do it run install.cmd (as Administrator) inside JetDdexProvider directory  


