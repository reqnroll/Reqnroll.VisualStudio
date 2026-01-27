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

Write-Output "There is currently nothing to pre-build"
