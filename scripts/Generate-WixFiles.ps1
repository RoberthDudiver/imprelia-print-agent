param(
    [Parameter(Mandatory = $true)]
    [string] $SourceDir,

    [Parameter(Mandatory = $true)]
    [string] $OutputPath
)

$ErrorActionPreference = "Stop"

$source = (Resolve-Path -LiteralPath $SourceDir).Path
$files = Get-ChildItem -LiteralPath $source -File -Recurse |
    Where-Object { $_.Extension -notin @(".pdb", ".xml") } |
    Sort-Object FullName

function Convert-ToWixId {
    param([string] $Value)
    $id = $Value -replace '[^A-Za-z0-9_\.]', '_'
    if ($id -match '^[0-9]') { $id = "_$id" }
    if ($id.Length -gt 70) { $id = $id.Substring(0, 70) }
    return $id
}

function Get-StableGuid {
    param([string] $Value)
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value.ToLowerInvariant())
    $md5 = [System.Security.Cryptography.MD5]::Create()
    try {
        $hash = $md5.ComputeHash($bytes)
        $hash[6] = ($hash[6] -band 0x0f) -bor 0x30
        $hash[8] = ($hash[8] -band 0x3f) -bor 0x80
        return [Guid]::new($hash).ToString().ToUpperInvariant()
    }
    finally {
        $md5.Dispose()
    }
}

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
[void]$sb.AppendLine('<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">')
[void]$sb.AppendLine('  <Fragment>')
[void]$sb.AppendLine('    <ComponentGroup Id="PublishedFiles">')

$directoryIds = [ordered]@{ "" = "INSTALLFOLDER" }

foreach ($file in $files) {
    $basePath = $source.TrimEnd('\') + '\'
    $relativePath = $file.FullName.Substring($basePath.Length).Replace('\', '/')
    $relativeDir = [System.IO.Path]::GetDirectoryName($relativePath)
    if ($relativeDir -eq $null) { $relativeDir = "" }
    $relativeDir = $relativeDir.Replace('\', '/')

    if (-not $directoryIds.Contains($relativeDir)) {
        $directoryIds[$relativeDir] = "Dir_" + (Convert-ToWixId $relativeDir)
    }

    $componentId = "Cmp_" + (Convert-ToWixId $relativePath)
    $fileId = "File_" + (Convert-ToWixId $relativePath)
    $guid = Get-StableGuid $relativePath
    $sourcePath = '$(var.PublishDir)\' + $relativePath.Replace('/', '\')
    $directoryId = $directoryIds[$relativeDir]

    [void]$sb.AppendLine("      <Component Id=""$componentId"" Directory=""$directoryId"" Guid=""{$guid}"">")
    [void]$sb.AppendLine("        <File Id=""$fileId"" Source=""$sourcePath"" KeyPath=""yes"" />")
    [void]$sb.AppendLine("      </Component>")
}

[void]$sb.AppendLine('    </ComponentGroup>')
[void]$sb.AppendLine('  </Fragment>')

$dirs = $directoryIds.GetEnumerator() | Where-Object { $_.Key -ne "" } | Sort-Object { $_.Key.Length }
if ($dirs.Count -gt 0) {
    [void]$sb.AppendLine('  <Fragment>')
    [void]$sb.AppendLine('    <DirectoryRef Id="INSTALLFOLDER">')

    foreach ($dir in $dirs) {
        $name = Split-Path -Path $dir.Key -Leaf
        [void]$sb.AppendLine("      <Directory Id=""$($dir.Value)"" Name=""$name"" />")
    }

    [void]$sb.AppendLine('    </DirectoryRef>')
    [void]$sb.AppendLine('  </Fragment>')
}

[void]$sb.AppendLine('</Wix>')

$outputDir = Split-Path -Parent $OutputPath
if ($outputDir) {
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
}

Set-Content -LiteralPath $OutputPath -Value $sb.ToString() -Encoding UTF8
