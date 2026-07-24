[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)][string]$Path,
    [Parameter(Mandatory = $true)][string]$Url,
    [Parameter(Mandatory = $true)][string]$Description,
    [Parameter(Mandatory = $false)][string]$Dependencies,
    [Parameter(Mandatory = $false)][string]$StoreDependencies,
    [Parameter(Mandatory = $true)][string]$Destination
)

$ErrorActionPreference = 'Stop'

if ($Description.Length -gt 256) {
    throw 'Description exceeds 256 characters'
}

$dir = Split-Path -LiteralPath $Path

if ($StoreDependencies) {
    $deps = $StoreDependencies.Split(';')
}
else {
    $deps = [string[]]@()
}

if ($Dependencies) {
    $deps += $Dependencies.Split(';') | Get-ItemPropertyValue -Name VersionInfo | ForEach-Object {
        $author = $_.LegalCopyright
        $name = $_.ProductName.Replace('.', '_')
        $version = $_.ProductVersion.Split('+')[0]
        $versionNumber = $version.Split('-')[0]
        if ($versionNumber -ne $version) {
            $name += '_BETA'
        }
        "$author-$name-$versionNumber"
    }
}

$vi = Get-ItemPropertyValue -LiteralPath $Path -Name VersionInfo
$name = $vi.ProductName.Replace('.', '_')
$version = $vi.ProductVersion.Split('+')[0]
$versionNumber = $version.Split('-')[0]

# if ($VersionNumber -ne $Version) {
#     $versionParts = $VersionNumber.Split('.')
#     $patch = $versionParts[2]
#     $beta = ($Version -split '-beta.0.')[1].PadLeft(3, '0')
#     $versionParts[2] = "$patch$beta"
#     $VersionNumber = $versionParts -join '.'
# }

$manifest = @{
    name           = $name
    version_number = $versionNumber
    version        = $version
    website_url    = $Url
    description    = $Description
    dependencies   = $deps
}

if ($versionNumber -ne $version) {
    $manifest.description = "(BETA) $($manifest.description)"
    $manifest.name += '_BETA'

    if ($manifest.description.Length -gt 256) {
        $manifest.description = "$($manifest.description.Substring(0, 253))..."
    }
}

$tmpDir = New-Item -Path "${Env:TEMP}\ServersideQoL\$(New-Guid)" -ItemType Directory
try {
    Get-ChildItem -LiteralPath $dir -File | Copy-Item -Destination $tmpDir
    $dir = $tmpDir.FullName

    $readme = Get-Content -LiteralPath "$PSScriptRoot\README.md" -Raw
    $readme = $readme.Replace('{PluginName}', $vi.ProductName)
    $readme = $readme.Replace('{Features}', (Get-Content -LiteralPath "$dir\FEATURES.md" -Raw))
    Remove-Item -LiteralPath "$dir\FEATURES.md" -Force
    Set-Content -LiteralPath "$dir\README.md" -Value $readme
    
    $patchers = Get-ChildItem -LiteralPath $dir -File -Filter '*.Patchers.dll'
    if ($patchers) {
        New-Item -Path "$dir\patchers" -ItemType Directory -ErrorAction SilentlyContinue
        $patchers  | ForEach-Object {
            $files = Get-ChildItem -LiteralPath $dir -Filter "$($_.BaseName).*" -File
            $files | ForEach-Object { $_ | Move-Item -Destination "$dir\patchers\$($_.Name)" -Force }
        }
        
        New-Item -Path "$dir\plugins" -ItemType Directory -ErrorAction SilentlyContinue
        Get-ChildItem -LiteralPath $dir -Filter '*.dll' -File | ForEach-Object {
            $files = Get-ChildItem -LiteralPath $dir -Filter "$($_.BaseName).*" -File
            $files | ForEach-Object { $_ | Move-Item -Destination "$dir\plugins\$($_.Name)" -Force }
        }
    }

    Set-Content -LiteralPath "$dir\manifest.json" -Value ($manifest | ConvertTo-Json)

    New-Item -Path (Split-Path $Destination) -ItemType Directory -ErrorAction SilentlyContinue
    Compress-Archive -Path "$dir\*" -DestinationPath $Destination -Force
}
finally {
    $tmpDir | Remove-Item -Recurse -Force
}