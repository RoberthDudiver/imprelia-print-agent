param(
    [string] $Version = "1.0.0",
    [switch] $SkipInstaller
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    dotnet clean .\Imprelia.PrintAgent.sln -c Release
    dotnet build .\Imprelia.PrintAgent.sln -c Release

    if (-not $SkipInstaller) {
        dotnet build .\installer\Imprelia.PrintAgent.Installer.wixproj -c Release -p:ProductVersion=$Version
    }
}
finally {
    Pop-Location
}
