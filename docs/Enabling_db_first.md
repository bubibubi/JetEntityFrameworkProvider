# DB First

You need to start from the source code to use DB First.

DB First require a full provider setup (ADO.Net provider setup).
ADO.Net provider setup for MS Access is included into Entity Framework Provider (you don't need an ADO.Net provider for MS Access if you use only MS Access with ADO.Net; in this case you can use OLEDB ADO.Net provider directly).  
  
**Setup EF for Visual Studio**
1. If you are using VS 2012 or 2013 install "Entity Framework 6 Tools for Visual Studio 2012 & 2013" from here  
[https://www.microsoft.com/en-us/download/details.aspx?id=40762](https://www.microsoft.com/en-us/download/details.aspx?id=40762)  
  
**Compile JetEntityFrameworkProvider**
(these steps are already included in JetDdexProvider build)

1. Sign the JetEntityFrameworkProvider  
I usually enable it from visual studio. You can also find a test key inside the project.  
  
2. Register the ADO.Net provider for MS Access (JetEntityFrameworkProvider.dll) in the GAC.


**Register the ADO.Net provider for MS Access in the machine.config.**
You need to edit the right machine.config. The right machine.config is the machine.config used during Db First process by Visual Studio to retrieve informations. In my case is the 32 bits v4.0 so is here  
C:\Windows\Microsoft.NET\Framework\v4.0.30319\Config  
  
in configuration\system.data\DbProviderFactories  
add this line (please check version first)  
  
```xml
<add name="JetEntityFrameworkProvider" invariant="JetEntityFrameworkProvider" description="JetEntityFrameworkProvider" type="JetEntityFrameworkProvider.JetProviderFactory, JetEntityFrameworkProvider, Version=1.0.0.0, Culture=neutral, PublicKeyToken=756cf6beb8fe7b41" />
```

5. Register DDEX provider (the Visual Studio provider).  
(this step is already included in JetDdexProvider build)
To do it run ```install.cmd``` (as Administrator) inside JetDdexProvider directory  


