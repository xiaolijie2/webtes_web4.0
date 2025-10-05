Param(
    [switch]$Apply,
    [switch]$IncludeBuildArtifacts,
    [switch]$IncludeBackups
)

$ErrorActionPreference = "Stop"

# Resolve repo root (parent of the scripts directory)
$ScriptDir = $PSScriptRoot
$RepoRoot = Split-Path -Parent $ScriptDir

Write-Host "Repository root:" $RepoRoot

# Patterns for root-level test and temporary files
$rootFilePatterns = @(
    "test*.js",
    "test*.png",
    "test*.txt",
    "test*.json",
    "test_search_functionality.html",
    "test-salesperson-login.html",
    "build_output*.txt"
)

# Specific test/debug HTML files under wwwroot
$wwwrootTestPatterns = @(
    "auth-debug.html",
    "auth-diagnostic.html",
    "clear-auth-test.html",
    "direct-test.html",
    "profile-debug.html",
    "simulate-login.html",
    "test-account.html",
    "test-api.html",
    "test-navigation.html",
    "test-profile*.html",
    "test-yewuyuan1.html",
    "verify-fix.html"
)

$filesToDelete = @()

# Collect root-level files matching patterns
foreach ($pattern in $rootFilePatterns) {
    $matches = Get-ChildItem -Path $RepoRoot -File -Filter $pattern -ErrorAction SilentlyContinue
    if ($matches) { $filesToDelete += $matches }
}

# Collect wwwroot test HTML files
$wwwrootPath = Join-Path $RepoRoot "wwwroot"
if (Test-Path $wwwrootPath) {
    foreach ($pattern in $wwwrootTestPatterns) {
        $matches = Get-ChildItem -Path $wwwrootPath -File -Filter $pattern -ErrorAction SilentlyContinue
        if ($matches) { $filesToDelete += $matches }
    }
}

# Optional directories
$dirsToDelete = @()
if ($IncludeBackups) {
    $backupDir = Join-Path $RepoRoot "backup_html_files"
    if (Test-Path $backupDir) { $dirsToDelete += Get-Item $backupDir }
}

if ($IncludeBuildArtifacts) {
    $binDir = Join-Path $RepoRoot "bin"
    $objDir = Join-Path $RepoRoot "obj"
    if (Test-Path $binDir) { $dirsToDelete += Get-Item $binDir }
    if (Test-Path $objDir) { $dirsToDelete += Get-Item $objDir }
}

# De-duplicate
$filesToDelete = $filesToDelete | Select-Object -Unique
$dirsToDelete  = $dirsToDelete  | Select-Object -Unique

# Summary
Write-Host "Found" ($filesToDelete.Count) "files and" ($dirsToDelete.Count) "directories to delete." -ForegroundColor Yellow

if (-not $Apply) {
    Write-Host "DRY-RUN: Listing planned deletions (use -Apply to actually delete)" -ForegroundColor Cyan
    foreach ($f in $filesToDelete) { Write-Host "FILE  ->" $f.FullName }
    foreach ($d in $dirsToDelete)  { Write-Host "DIR   ->" $d.FullName }
    exit 0
}

Write-Host "Applying deletions..." -ForegroundColor Green

# Delete files
foreach ($f in $filesToDelete) {
    try {
        Remove-Item -LiteralPath $f.FullName -Force -ErrorAction Stop
        Write-Host "Deleted FILE:" $f.FullName
    } catch {
        Write-Warning ("Failed to delete FILE: {0} -> {1}" -f $f.FullName, $_.Exception.Message)
    }
}

# Delete directories (recursive)
foreach ($d in $dirsToDelete) {
    try {
        Remove-Item -LiteralPath $d.FullName -Recurse -Force -ErrorAction Stop
        Write-Host "Deleted DIR: " $d.FullName
    } catch {
        Write-Warning ("Failed to delete DIR: {0} -> {1}" -f $d.FullName, $_.Exception.Message)
    }
}

Write-Host "Cleanup completed." -ForegroundColor Green