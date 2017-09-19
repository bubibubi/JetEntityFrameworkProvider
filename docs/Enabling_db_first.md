# DB First

You need to start from the source code to use DB First.

## Walkthrough

1. Start Visual Studio as Administrator  
2. Update install.cmd (REGROOT variable) according to your Visual Studio version  
3. Compile the project
4. Register the ADO.Net provider in machine.config (see below)

Now you can use DB First in new projects. In new projects don't forget to include Entity Framework and the JetEntityFrameworkProvider from NuGet.   
You need to redo this procedure if I update the JetEntityFrameworkProvider on NuGet because the versions (GAC registered and NuGet) must be the same.  


## An explanation of what happening during compile

DB First require a full provider setup (ADO.Net provider setup).
ADO.Net provider setup for MS Access is included into Entity Framework Provider (you don't need an ADO.Net provider for MS Access if you use only MS Access with ADO.Net; in this case you can use OLEDB ADO.Net provider directly).  
  
**Setup EF for Visual Studio**   
1. If you are using VS 2012 or 2013 install "Entity Framework 6 Tools for Visual Studio 2012 & 2013" from here  
[https://www.microsoft.com/en-us/download/details.aspx?id=40762](https://www.microsoft.com/en-us/download/details.aspx?id=40762)  
This is required to enable EF6 in Visual Studio. It is required also if you need to enable DB First for SQL Server or other providers.
  
**Compile JetEntityFrameworkProvider**    
(the following steps are not required because the are already included in JetDdexProvider build)

1. Sign the JetEntityFrameworkProvider  
I usually enable it from visual studio. You can also find a test key inside the project.  
  
2. Register the ADO.Net provider for MS Access (JetEntityFrameworkProvider.dll) in the GAC.


**Register the ADO.Net provider for MS Access in the machine.config.**  
You need to edit the right machine.config. The right machine.config is the machine.config used during Db First process by Visual Studio to retrieve informations. In my case is the 32 bits v4.0 so is here  
C:\Windows\Microsoft.NET\Framework\v4.0.30319\Config  
  
in configuration\system.data\DbProviderFactories  
add this line (please check version first)  
  
```xml
<add name="JetEntityFrameworkProvider" invariant="JetEntityFrameworkProvider" description="JetEntityFrameworkProvider" type="JetEntityFrameworkProvider.JetProviderFactory, JetEntityFrameworkProvider, Version=6.0.0.0, Culture=neutral, PublicKeyToken=756cf6beb8fe7b41" />
```

**Register DDEX provider (the Visual Studio provider).**   

1. Update install.cmd (REGROOT variable) according to your Visual Studio version  

2. Run ```install.cmd``` (as Administrator) inside JetDdexProvider directory (this step is not required because is already included in JetDdexProvider build)  
  


