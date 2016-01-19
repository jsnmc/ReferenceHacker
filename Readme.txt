
         <?xml version = "1.0" encoding="utf-8"?>
         <Project ToolsVersion = "3.5"xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
         <PropertyGroup>
            <OutputPath>$(SolutionDir)output</OutputPath>
            <WarningLevel>4</WarningLevel>
            <UseVSHostingProcess>false</UseVSHostingProcess>
            <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
         </PropertyGroup>
         </Project>
        <!-- u can import this Common.proj in each of your csprojs, for instance like so: -->
        <Import Project = "..\Common.proj" />