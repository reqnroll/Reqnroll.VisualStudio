param(
  [string] $version
)

function Set-Versions($content, $versionToSet) {


  $newContent = [regex]::replace($content,'<PackageReference Include="(?<packageName>Reqnroll[^"]*)" Version="(?<version>[^"]*)" />', { 
    param($m)
    $m.Value.Replace($m.Groups["version"].Value, $versionToSet)
  })
  return $newContent
}

if (-not $version) {
  Write-Error "Error. The version was not specified."
  Write-Error "Usage:"
  Write-Error "  update-versions.ps1 [version]"
  Exit
}

$projectFiles = Get-ChildItem -Path $PSScriptRoot -File -Recurse -Filter '*.*proj'
$changedCount = 0
foreach ($path in $projectFiles){
  $fileContent = Get-Content -LiteralPath $path.FullName -Raw
  $newFileContent = Set-Versions $fileContent $version
  if ($newFileContent -ne $fileContent){
    Write-Host "Updating $($path.Name)"
    Set-Content -Path $path.FullName -Value $newFileContent -NoNewline -Encoding utf8
    $changedCount++
  }
}

Write-Host "Updated $changedCount files."
