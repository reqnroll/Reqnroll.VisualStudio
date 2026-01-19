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

cd $PSScriptRoot\Reqnroll.VisualStudio.ReqnrollConnector.V1

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

# Copy prebuilt connectors	

Copy-Item "$PSScriptRoot\prebuilt\*" $outputFolder -Recurse
