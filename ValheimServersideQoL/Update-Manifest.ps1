[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$VersionNumber,
    [Parameter(Mandatory=$true)][string]$Version
)

Set-Content -LiteralPath $Path -Value ((Get-Content -LiteralPath $Path -Raw).Replace('{VersionNumber}',$VersionNumber).Replace('{Version}',$Version))