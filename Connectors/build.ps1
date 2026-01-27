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
