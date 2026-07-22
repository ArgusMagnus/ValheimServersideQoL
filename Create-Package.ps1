[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)][string]$Path,
    [Parameter(Mandatory = $true)][string]$Url,
    [Parameter(Mandatory = $true)][string]$Description,
    [Parameter(Mandatory = $false)][string]$Dependencies,
    [Parameter(Mandatory = $false)][string]$StoreDependencies,
    [Parameter(Mandatory = $true)][string]$Destination,
    [Parameter(Mandatory = $false)][string]$HasPatchers
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
        $version = $_.ProductVersion.Substring(0, $_.ProductVersion.IndexOfAny(@('-', '+')))
        "$author-$name-$version"
    }
}

$vi = Get-ItemPropertyValue -LiteralPath $Path -Name VersionInfo
$name = $vi.ProductName.Replace('.', '_')
$version = $vi.ProductVersion.Substring(0, $vi.ProductVersion.IndexOf('+'))
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

# if ($VersionNumber -ne $Version) {
#     $manifest.description = "This is the public test channel of $($manifest.name)"
#     $manifest.name += '_BETA'
# }

$tmpDir = New-Item -Path "${Env:TEMP}\ServersideQoL\$(New-Guid)" -ItemType Directory
try {
    Get-ChildItem -LiteralPath $dir -File | Copy-Item -Destination $tmpDir
    $dir = $tmpDir.FullName
    
    if ('true' -eq $HasPatchers) {
        New-Item -Path "$dir\patchers" -ItemType Directory -ErrorAction SilentlyContinue
        New-Item -Path "$dir\plugins" -ItemType Directory -ErrorAction SilentlyContinue
        Get-ChildItem -LiteralPath $dir -Filter '*.dll' -File | ForEach-Object {
            $files = Get-ChildItem -LiteralPath $dir -Filter "$($_.BaseName).*" -File
            $files | ForEach-Object { $_ | Copy-Item -Destination "$dir\patchers\$($_.Name)" -Force }
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