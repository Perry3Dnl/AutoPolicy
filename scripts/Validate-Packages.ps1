param(
    [string]$ArtifactsPath = (Join-Path $PSScriptRoot "../artifacts")
)

$ErrorActionPreference = "Stop"

function Assert-True {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw "Package validation failed: $Message"
    }
}

function Get-ZipEntryText {
    param(
        [System.IO.Compression.ZipArchive]$Archive,
        [System.IO.Compression.ZipArchiveEntry]$Entry
    )

    $stream = $Entry.Open()
    try {
        $reader = [System.IO.StreamReader]::new($stream)
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$propsPath = Join-Path $repoRoot "Directory.Build.props"
Assert-True (Test-Path $propsPath) "Directory.Build.props was not found."

[xml]$props = Get-Content $propsPath -Raw
$versionNode = $props.SelectSingleNode("/Project/PropertyGroup/AutoPolicyVersion")
Assert-True ($null -ne $versionNode) "AutoPolicyVersion is missing from Directory.Build.props."
$version = $versionNode.InnerText.Trim()
Assert-True (-not [string]::IsNullOrWhiteSpace($version)) "AutoPolicyVersion is empty."

$ArtifactsPath = [System.IO.Path]::GetFullPath($ArtifactsPath)
Assert-True (Test-Path $ArtifactsPath) "Artifacts directory '$ArtifactsPath' does not exist."

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$id = "AutoPolicy"
$framework = "net10.0"
$assembly = "AutoPolicy"
$nupkg = Join-Path $ArtifactsPath "$id.$version.nupkg"
$snupkg = Join-Path $ArtifactsPath "$id.$version.snupkg"

Assert-True (Test-Path $nupkg) "Missing $id.$version.nupkg."
Assert-True (Test-Path $snupkg) "Missing $id.$version.snupkg."

$archive = [System.IO.Compression.ZipFile]::OpenRead($nupkg)
try {
    $entryNames = @($archive.Entries | ForEach-Object { $_.FullName })
    $expectedEntries = @(
        "README.NUGET.md",
        "autopolicy-icon.png",
        "lib/$framework/$assembly.dll",
        "lib/$framework/$assembly.xml"
    )

    foreach ($expectedEntry in $expectedEntries) {
        Assert-True ($entryNames -contains $expectedEntry) "$id package is missing '$expectedEntry'."
    }

    $nuspecEntry = $archive.Entries | Where-Object { $_.FullName -like "*.nuspec" } | Select-Object -First 1
    Assert-True ($null -ne $nuspecEntry) "$id package does not contain a .nuspec file."

    [xml]$nuspec = Get-ZipEntryText -Archive $archive -Entry $nuspecEntry
    $metadata = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']")
    Assert-True ($null -ne $metadata) "$id package metadata is missing."

    $idNode = $metadata.SelectSingleNode("*[local-name()='id']")
    $versionMetadataNode = $metadata.SelectSingleNode("*[local-name()='version']")
    $licenseNode = $metadata.SelectSingleNode("*[local-name()='license']")
    $copyrightNode = $metadata.SelectSingleNode("*[local-name()='copyright']")
    $readmeNode = $metadata.SelectSingleNode("*[local-name()='readme']")
    $iconNode = $metadata.SelectSingleNode("*[local-name()='icon']")
    $repositoryNode = $metadata.SelectSingleNode("*[local-name()='repository']")

    Assert-True ($idNode.InnerText -eq $id) "$id package metadata has the wrong package ID."
    Assert-True ($versionMetadataNode.InnerText -eq $version) "$id package metadata has version '$($versionMetadataNode.InnerText)' instead of '$version'."
    Assert-True ($licenseNode.InnerText -eq "MPL-2.0") "$id package license expression is not MPL-2.0."
    Assert-True ($licenseNode.GetAttribute("type") -eq "expression") "$id package license metadata is not an SPDX expression."
    Assert-True ($copyrightNode.InnerText -eq "Copyright (c) 2026 Perry3D.nl") "$id package copyright metadata is incorrect."
    Assert-True ($readmeNode.InnerText -eq "README.NUGET.md") "$id package README metadata is incorrect."
    Assert-True ($null -ne $iconNode) "$id package icon metadata is missing."
    Assert-True ($iconNode.InnerText -eq "autopolicy-icon.png") "$id package icon metadata is incorrect."
    Assert-True ($null -ne $repositoryNode) "$id package repository metadata is missing."
    Assert-True ($repositoryNode.GetAttribute("type") -eq "git") "$id package repository type is not git."
    Assert-True ($repositoryNode.GetAttribute("url") -eq "https://github.com/Perry3Dnl/AutoPolicy") "$id package repository URL is incorrect."
    Assert-True (-not [string]::IsNullOrWhiteSpace($repositoryNode.GetAttribute("commit"))) "$id package repository commit metadata is missing."

    $sourceLinkDependency = $nuspec.SelectSingleNode("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='dependencies']//*[local-name()='dependency'][@id='Microsoft.SourceLink.GitHub']")
    Assert-True ($null -eq $sourceLinkDependency) "$id leaked Microsoft.SourceLink.GitHub as a consumer dependency."
}
finally {
    $archive.Dispose()
}

$symbolArchive = [System.IO.Compression.ZipFile]::OpenRead($snupkg)
try {
    $symbolEntries = @($symbolArchive.Entries | ForEach-Object { $_.FullName })
    Assert-True ($symbolEntries -contains "lib/$framework/$assembly.pdb") "$id symbol package is missing its portable PDB."
}
finally {
    $symbolArchive.Dispose()
}

Write-Host "Validated $id $version"
Write-Host "All AutoPolicy NuGet packages passed validation."
