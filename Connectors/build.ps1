param (
	[string]$configuration = "Debug"
)

$outputFolder = "$PSScriptRoot\bin\$configuration"

Remove-Item $outputFolder -Recurse -Force -ErrorAction SilentlyContinue

mkdir $outputFolder 

# build V1 any cpu

cd SpecFlow.VisualStudio.SpecFlowConnector.V1

dotnet publish -c $configuration

mkdir $outputFolder\V1\
Copy-Item bin\$configuration\net48\publish\* $outputFolder\V1\ -Exclude @('TechTalk.*','System.*', 'Gherkin.*','*.exe.config')

# build V1 x86

Remove-Item bin\$configuration\net48\win-x86\publish -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish -r win-x86 -c $configuration /p:PlatformTarget=x86

Rename-Item bin\$configuration\net48\win-x86\publish\specflow-vs.exe specflow-vs-x86.exe -Force
Rename-Item bin\$configuration\net48\win-x86\publish\specflow-vs.pdb specflow-vs-x86.pdb -Force

Copy-Item bin\$configuration\net48\win-x86\publish\specflow-vs-x86.* $outputFolder\V1\

cd ..

# build V2 any cpu

cd SpecFlow.VisualStudio.SpecFlowConnector.V2

dotnet publish -f net6.0 -c $configuration

Copy-Item bin\$configuration\net6.0\publish\ $outputFolder\V2-net6.0\ -Recurse

cd ..

# build V3 any cpu

cd SpecFlow.VisualStudio.SpecFlowConnector.V3

dotnet publish -f net6.0 -c $configuration

Copy-Item bin\$configuration\net6.0\publish\ $outputFolder\V3-net6.0\ -Recurse

dotnet publish -f net7.0 -c $configuration

Copy-Item bin\$configuration\net7.0\publish\ $outputFolder\V3-net7.0\ -Recurse

dotnet publish -f net8.0 -c $configuration

Copy-Item bin\$configuration\net8.0\publish\ $outputFolder\V3-net8.0\ -Recurse

cd ..

# build generic any cpu
pushd
cd SpecFlow.VisualStudio.SpecFlowConnector.Generic

dotnet publish -f net6.0 -c $configuration

Copy-Item bin\$configuration\net6.0\publish\ $outputFolder\Generic-net6.0\ -Recurse

dotnet publish -f net7.0 -c $configuration

Copy-Item bin\$configuration\net7.0\publish\ $outputFolder\Generic-net7.0\ -Recurse

dotnet publish -f net8.0 -c $configuration

Copy-Item bin\$configuration\net8.0\publish\ $outputFolder\Generic-net8.0\ -Recurse

popd
