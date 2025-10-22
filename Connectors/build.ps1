param (
	[string]$configuration = "Debug",
	[string]$additionalArgs = '-'
)

$outputFolder = "$PSScriptRoot\bin\$configuration"
$buildArgs = @("-c", $configuration)
if ($additionalArgs -ne '-') {
	$buildArgs += $additionalArgs.Split(' ')
}


Write-Output "ARGS: $buildArgs"

Remove-Item $outputFolder -Recurse -Force -ErrorAction SilentlyContinue

mkdir $outputFolder 

# build Reqnroll V1 any cpu

cd Reqnroll.VisualStudio.ReqnrollConnector.V1

dotnet publish $buildArgs

mkdir $outputFolder\Reqnroll-V1\
Copy-Item bin\$configuration\net48\publish\* $outputFolder\Reqnroll-V1\ -Exclude @('System.*', 'Gherkin.*','*.exe.config')

# build Reqnroll V1 x86

Remove-Item bin\$configuration\net48\win-x86\publish -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish -r win-x86 $buildArgs /p:PlatformTarget=x86

Rename-Item bin\$configuration\net48\win-x86\publish\reqnroll-vs.exe reqnroll-vs-x86.exe -Force
Rename-Item bin\$configuration\net48\win-x86\publish\reqnroll-vs.pdb reqnroll-vs-x86.pdb -Force

Copy-Item bin\$configuration\net48\win-x86\publish\reqnroll-vs-x86.* $outputFolder\Reqnroll-V1\

cd ..

# build Reqnroll generic any cpu
pushd
cd Reqnroll.VisualStudio.ReqnrollConnector.Generic

dotnet publish -f net6.0 $buildArgs

Copy-Item bin\$configuration\net6.0\publish\ $outputFolder\Reqnroll-Generic-net6.0\ -Recurse

dotnet publish -f net7.0 $buildArgs

Copy-Item bin\$configuration\net7.0\publish\ $outputFolder\Reqnroll-Generic-net7.0\ -Recurse

dotnet publish -f net8.0 $buildArgs

Copy-Item bin\$configuration\net8.0\publish\ $outputFolder\Reqnroll-Generic-net8.0\ -Recurse

dotnet publish -f net9.0 $buildArgs

Copy-Item bin\$configuration\net9.0\publish\ $outputFolder\Reqnroll-Generic-net9.0\ -Recurse

dotnet publish -f net10.0 $buildArgs

Copy-Item bin\$configuration\net10.0\publish\ $outputFolder\Reqnroll-Generic-net10.0\ -Recurse

popd

# build SpecFlow V1 any cpu

cd SpecFlow.VisualStudio.SpecFlowConnector.V1

dotnet publish $buildArgs

mkdir $outputFolder\SpecFlow-V1\
Copy-Item bin\$configuration\net48\publish\* $outputFolder\SpecFlow-V1\ -Exclude @('TechTalk.*','System.*', 'Gherkin.*','*.exe.config')

# build SpecFlow V1 x86

Remove-Item bin\$configuration\net48\win-x86\publish -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish -r win-x86 $buildArgs /p:PlatformTarget=x86

Rename-Item bin\$configuration\net48\win-x86\publish\specflow-vs.exe specflow-vs-x86.exe -Force
Rename-Item bin\$configuration\net48\win-x86\publish\specflow-vs.pdb specflow-vs-x86.pdb -Force

Copy-Item bin\$configuration\net48\win-x86\publish\specflow-vs-x86.* $outputFolder\SpecFlow-V1\

cd ..

# build SpecFlow V2 any cpu

cd SpecFlow.VisualStudio.SpecFlowConnector.V2

dotnet publish -f net6.0 $buildArgs

Copy-Item bin\$configuration\net6.0\publish\ $outputFolder\SpecFlow-V2-net6.0\ -Recurse

cd ..

# build SpecFlow V3 any cpu

cd SpecFlow.VisualStudio.SpecFlowConnector.V3

dotnet publish -f net6.0 $buildArgs

Copy-Item bin\$configuration\net6.0\publish\ $outputFolder\SpecFlow-V3-net6.0\ -Recurse

dotnet publish -f net7.0 $buildArgs

Copy-Item bin\$configuration\net7.0\publish\ $outputFolder\SpecFlow-V3-net7.0\ -Recurse

dotnet publish -f net8.0 $buildArgs

Copy-Item bin\$configuration\net8.0\publish\ $outputFolder\SpecFlow-V3-net8.0\ -Recurse

dotnet publish -f net9.0 $buildArgs

Copy-Item bin\$configuration\net9.0\publish\ $outputFolder\SpecFlow-V3-net9.0\ -Recurse

cd ..

# build SpecFlow generic any cpu
pushd
cd SpecFlow.VisualStudio.SpecFlowConnector.Generic

dotnet publish -f net6.0 $buildArgs

Copy-Item bin\$configuration\net6.0\publish\ $outputFolder\SpecFlow-Generic-net6.0\ -Recurse

dotnet publish -f net7.0 $buildArgs

Copy-Item bin\$configuration\net7.0\publish\ $outputFolder\SpecFlow-Generic-net7.0\ -Recurse

dotnet publish -f net8.0 $buildArgs

Copy-Item bin\$configuration\net8.0\publish\ $outputFolder\SpecFlow-Generic-net8.0\ -Recurse

dotnet publish -f net9.0 $buildArgs

Copy-Item bin\$configuration\net9.0\publish\ $outputFolder\SpecFlow-Generic-net9.0\ -Recurse

popd
