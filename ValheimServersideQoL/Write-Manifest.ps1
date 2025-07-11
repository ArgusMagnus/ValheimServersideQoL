[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$VersionNumber,
    [Parameter(Mandatory=$true)][string]$Version
)

if ($VersionNumber -ne $Version) {
    $versionParts = $VersionNumber.Split('.')
    $patch = $versionParts[2]
    $beta = ($Version -split '-beta.0.')[1].PadLeft(3, '0')
    $versionParts[2] = "$patch$beta"
    $VersionNumber = $versionParts -join '.'
}

$manifest = @{
    name = 'ServersideQoL'
    version_number = $VersionNumber
    version = $Version
    website_url = 'https://github.com/ArgusMagnus/ValheimServersideQoL'
    description = 'Serverside-only QoL mod, compatible with vanilla (e.g. XBox) clients. Auto-sort chests, track portals/ships on map tables, auto-close doors, refuel smelters from containers, disable rain damage, infinite building/farming stamina and more.'
    dependencies = @('denikson-BepInExPack_Valheim-5.4.2202')
}

if ($VersionNumber -ne $Version) {
    $manifest.description = "This is the public test channel of $($manifest.name)"
    $manifest.name += '_BETA'
}

Set-Content -LiteralPath $Path -Value ($manifest | ConvertTo-Json)