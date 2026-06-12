param(
    [string] $Version = "1.0.0.0",
    [string] $Publisher = "CN=18FB64AB-5A7B-47F7-AD1B-5E66071B7C0F",
    [string] $PackageName = "Dudiver.ImpreliaPrintAgent",
    [string] $DisplayName = "Imprelia Print Agent",
    [string] $PublisherDisplayName = "Dudiver",
    [switch] $CreateUploadZip
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sdkBin = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64"
$makeAppx = Join-Path $sdkBin "makeappx.exe"
$makePri = Join-Path $sdkBin "makepri.exe"

if (-not (Test-Path -LiteralPath $makeAppx)) {
    throw "makeappx.exe was not found. Install Windows 10/11 SDK."
}

if (-not (Test-Path -LiteralPath $makePri)) {
    throw "makepri.exe was not found. Install Windows 10/11 SDK."
}

$publishDir = Join-Path $root "artifacts\msix\publish"
$packageDir = Join-Path $root "artifacts\msix\package"
$outDir = Join-Path $root "artifacts\msix\out"
$msixPath = Join-Path $outDir "ImpreliaPrintAgent-$Version.msix"
$uploadPath = Join-Path $outDir "ImpreliaPrintAgent-$Version.msixupload"

Remove-Item -LiteralPath $publishDir, $packageDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDir, $packageDir, $outDir | Out-Null

Push-Location $root
try {
    dotnet restore ".\Imprelia.PrintAgent.csproj" -r win-x64
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }

    dotnet publish ".\Imprelia.PrintAgent.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

Copy-Item -Path (Join-Path $publishDir "*") -Destination $packageDir -Recurse -Force

$assetsDir = Join-Path $packageDir "Assets"
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null

Add-Type -AssemblyName System.Drawing

function New-PackageImage {
    param(
        [string] $Source,
        [string] $Destination,
        [int] $Width,
        [int] $Height
    )

    $src = [System.Drawing.Image]::FromFile($Source)
    try {
        $bitmap = New-Object System.Drawing.Bitmap $Width, $Height
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.Clear([System.Drawing.Color]::Transparent)
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

                $scale = [Math]::Min($Width / $src.Width, $Height / $src.Height)
                $drawWidth = [int]($src.Width * $scale)
                $drawHeight = [int]($src.Height * $scale)
                $x = [int](($Width - $drawWidth) / 2)
                $y = [int](($Height - $drawHeight) / 2)
                $graphics.DrawImage($src, $x, $y, $drawWidth, $drawHeight)
            }
            finally {
                $graphics.Dispose()
            }

            $bitmap.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $bitmap.Dispose()
        }
    }
    finally {
        $src.Dispose()
    }
}

$logoSource = Join-Path $root "images\Sin título-1@0,5x.png"
$iconSource = Join-Path $root "images\IconoTry.png"
if (-not (Test-Path -LiteralPath $logoSource)) { $logoSource = Join-Path $root "images\Logo-2.png" }
if (-not (Test-Path -LiteralPath $iconSource)) { $iconSource = Join-Path $root "images\ico.png" }

New-PackageImage -Source $iconSource -Destination (Join-Path $assetsDir "Square44x44Logo.png") -Width 44 -Height 44
New-PackageImage -Source $logoSource -Destination (Join-Path $assetsDir "Square150x150Logo.png") -Width 150 -Height 150
New-PackageImage -Source $logoSource -Destination (Join-Path $assetsDir "StoreLogo.png") -Width 50 -Height 50
New-PackageImage -Source $logoSource -Destination (Join-Path $assetsDir "Wide310x150Logo.png") -Width 310 -Height 150

$manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity
    Name="$PackageName"
    Publisher="$Publisher"
    Version="$Version"
    ProcessorArchitecture="x64" />

  <Properties>
    <DisplayName>$DisplayName</DisplayName>
    <PublisherDisplayName>$PublisherDisplayName</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.26100.0" />
  </Dependencies>

  <Resources>
    <Resource Language="en-us" />
    <Resource Language="es-es" />
  </Resources>

  <Applications>
    <Application
      Id="ImpreliaPrintAgent"
      Executable="ImpreliaPrintAgent.exe"
      EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="$DisplayName"
        Description="Local Windows print agent for web applications"
        BackgroundColor="transparent"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" />
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"@

Set-Content -LiteralPath (Join-Path $packageDir "AppxManifest.xml") -Value $manifest -Encoding UTF8

Push-Location $packageDir
try {
    & $makePri createconfig /cf priconfig.xml /dq en-US /o | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "makepri.exe createconfig failed with exit code $LASTEXITCODE."
    }

    & $makePri new /pr . /cf priconfig.xml /o | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "makepri.exe new failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

Remove-Item -LiteralPath $msixPath -Force -ErrorAction SilentlyContinue
& $makeAppx pack /d $packageDir /p $msixPath /o | Out-Host
if ($LASTEXITCODE -ne 0) {
    throw "makeappx.exe failed with exit code $LASTEXITCODE."
}

if ($CreateUploadZip) {
    Remove-Item -LiteralPath $uploadPath -Force -ErrorAction SilentlyContinue
    $tempZipPath = [System.IO.Path]::ChangeExtension($uploadPath, ".zip")
    Remove-Item -LiteralPath $tempZipPath -Force -ErrorAction SilentlyContinue
    Compress-Archive -Path $msixPath -DestinationPath $tempZipPath -Force
    Move-Item -LiteralPath $tempZipPath -Destination $uploadPath -Force
}

Write-Host ""
Write-Host "MSIX generated:" -ForegroundColor Green
Write-Host $msixPath

if ($CreateUploadZip) {
    Write-Host ""
    Write-Host "MSIX upload package generated:" -ForegroundColor Green
    Write-Host $uploadPath
}

Write-Host ""
Write-Host "Important: sign the MSIX before installation/submission if Partner Center requires your package to be signed." -ForegroundColor Yellow
