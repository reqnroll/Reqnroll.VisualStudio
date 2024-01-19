
function BuildExternalPackage ([string]$packageFolder)
{
    pushd .
	Write-Host $packageFolder
    cd $packageFolder

    dotnet build --configuration Release
 
    Get-ChildItem -Path .\bin\Release\*.nupkg | Move-Item -Force -Destination ..\

    popd
}

Get-ChildItem -Directory -Exclude "PackagesForTests" | ForEach-Object { BuildExternalPackage $_ }

