#!/usr/bin/env pwsh
<#
.SYNOPSIS
    DStream SDK Version Management Script
    
.DESCRIPTION
    Manages version bumping and release preparation for the DStream .NET SDK packages.
    
.PARAMETER Type
    The type of version bump: major, minor, patch, or custom
    
.PARAMETER CustomVersion
    Custom version string when Type is 'custom'
    
.PARAMETER PreRelease
    Pre-release identifier (e.g., 'beta', 'alpha', 'rc')
    
.PARAMETER DryRun
    Show what would happen without making changes
    
.EXAMPLE
    ./scripts/version-bump.ps1 -Type patch
    # Bumps patch version (0.1.0 -> 0.1.1)
    
.EXAMPLE
    ./scripts/version-bump.ps1 -Type minor -PreRelease beta
    # Bumps minor version with beta pre-release (0.1.0 -> 0.2.0-beta.1)
    
.EXAMPLE
    ./scripts/version-bump.ps1 -Type custom -CustomVersion "1.0.0"
    # Sets custom version to 1.0.0
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("major", "minor", "patch", "custom")]
    [string]$Type,
    
    [string]$CustomVersion = "",
    
    [string]$PreRelease = "",
    
    [switch]$DryRun = $false
)

# Set script location
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$VersionFile = Join-Path $RepoRoot "VERSION.txt"

Write-Host "🎯 DStream SDK Version Management" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Read current version
if (Test-Path $VersionFile) {
    $currentVersion = Get-Content $VersionFile -Raw | ForEach-Object { $_.Trim() }
    Write-Host "📋 Current version: $currentVersion" -ForegroundColor Yellow
} else {
    Write-Host "❌ VERSION.txt not found!" -ForegroundColor Red
    exit 1
}

# Parse current version
if ($currentVersion -match '^(\d+)\.(\d+)\.(\d+)(?:-(.+))?$') {
    $major = [int]$matches[1]
    $minor = [int]$matches[2] 
    $patch = [int]$matches[3]
    $currentPreRelease = $matches[4]
} else {
    Write-Host "❌ Invalid version format in VERSION.txt: $currentVersion" -ForegroundColor Red
    exit 1
}

# Calculate new version
switch ($Type) {
    "major" { 
        $major += 1
        $minor = 0
        $patch = 0
    }
    "minor" { 
        $minor += 1 
        $patch = 0
    }
    "patch" { 
        $patch += 1 
    }
    "custom" {
        if ([string]::IsNullOrEmpty($CustomVersion)) {
            Write-Host "❌ CustomVersion parameter required when Type is 'custom'" -ForegroundColor Red
            exit 1
        }
        if ($CustomVersion -match '^(\d+)\.(\d+)\.(\d+)$') {
            $major = [int]$matches[1]
            $minor = [int]$matches[2]
            $patch = [int]$matches[3]
        } else {
            Write-Host "❌ Invalid custom version format: $CustomVersion" -ForegroundColor Red
            exit 1
        }
    }
}

# Build new version string
$newVersion = "$major.$minor.$patch"
if (![string]::IsNullOrEmpty($PreRelease)) {
    # Handle pre-release versioning
    if ($currentVersion -match "$newVersion-$PreRelease\.(\d+)") {
        $preReleaseNumber = [int]$matches[1] + 1
    } else {
        $preReleaseNumber = 1
    }
    $newVersion = "$newVersion-$PreRelease.$preReleaseNumber"
}

Write-Host "🚀 New version: $newVersion" -ForegroundColor Green

if ($DryRun) {
    Write-Host "🔍 Dry run mode - no changes will be made" -ForegroundColor Magenta
    Write-Host "Would update:"
    Write-Host "  - VERSION.txt: $currentVersion → $newVersion"
    Write-Host "  - Git tag: v$newVersion"
    exit 0
}

# Confirm with user
$confirmation = Read-Host "Do you want to proceed? (y/N)"
if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
    Write-Host "❌ Cancelled by user" -ForegroundColor Red
    exit 0
}

# Update VERSION.txt
Write-Host "📝 Updating VERSION.txt..." -ForegroundColor Blue
$newVersion | Out-File -FilePath $VersionFile -Encoding UTF8 -NoNewline

# Verify git is available and repository is clean
try {
    $gitStatus = git status --porcelain 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Git not available or not in a git repository" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Error checking git status" -ForegroundColor Red
    exit 1
}

# Commit changes
Write-Host "📦 Committing version bump..." -ForegroundColor Blue
git add VERSION.txt
git commit -m "🏷️ Bump version to $newVersion

- Updated VERSION.txt: $currentVersion → $newVersion
- Ready for release tagging and NuGet publishing"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to commit changes" -ForegroundColor Red
    exit 1
}

# Create git tag
Write-Host "🏷️ Creating git tag: v$newVersion..." -ForegroundColor Blue
git tag -a "v$newVersion" -m "Release v$newVersion

DStream .NET SDK Release $newVersion

This release will automatically publish:
- Katasec.DStream.Abstractions v$newVersion
- Katasec.DStream.SDK.Core v$newVersion

To trigger the release:
git push origin v$newVersion"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to create git tag" -ForegroundColor Red
    exit 1
}

Write-Host "" 
Write-Host "✅ Version bump completed successfully!" -ForegroundColor Green
Write-Host "" 
Write-Host "📋 Summary:" -ForegroundColor Cyan
Write-Host "  • Version: $currentVersion → $newVersion" -ForegroundColor White
Write-Host "  • Commit: Created with version bump" -ForegroundColor White
Write-Host "  • Tag: v$newVersion created" -ForegroundColor White
Write-Host ""
Write-Host "🚀 Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Review the changes: git log --oneline -2" -ForegroundColor White
Write-Host "  2. Push to trigger release: git push origin main && git push origin v$newVersion" -ForegroundColor White
Write-Host "  3. Monitor GitHub Actions for NuGet publishing" -ForegroundColor White
Write-Host ""