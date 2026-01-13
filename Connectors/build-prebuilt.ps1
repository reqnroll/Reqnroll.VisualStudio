param (
	[string]$configuration = "Release",
	[string]$additionalArgs = '-'
)

$outputFolder = "$PSScriptRoot\prebuilt"
$buildArgs = @("-c", $configuration)
if ($additionalArgs -ne '-') {
	$buildArgs += $additionalArgs.Split(' ')
}


Write-Output "ARGS: $buildArgs"

Remove-Item $outputFolder -Recurse -Force -ErrorAction SilentlyContinue

mkdir $outputFolder

# build Reqnroll generic .NET 10.0
pushd
cd Reqnroll.VisualStudio.ReqnrollConnector.Generic

dotnet publish -f net10.0 -p CustomConnectorFrameworks="net10.0" $buildArgs

Copy-Item bin\$configuration\net10.0\publish\ $outputFolder\Reqnroll-Generic-net10.0\ -Recurse

popd
