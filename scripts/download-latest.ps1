#!/usr/bin/env pwsh
# Download latest rgupdate release for Windows
# Usage: ./download-latest.ps1 [output-directory]

param(
    [string]$OutputDir = ".",
    [switch]$Help
)

if ($Help) {
    Write-Host @"
Download Latest rgupdate Release

Usage: 
    ./download-latest.ps1 [OutputDir]

Parameters:
    OutputDir    Directory to save the downloaded executable (default: current directory)
    -Help        Show this help message

Examples:
    ./download-latest.ps1                    # Download to current directory
    ./download-latest.ps1 C:\tools           # Download to C:\tools
    ./download-latest.ps1 -Help              # Show this help
"@
    exit 0
}

$repoUrl = "https://github.com/alex-yates-redgate/rgupdate"
$downloadUrl = "$repoUrl/releases/latest/download/rgupdate-windows.exe"
$outputPath = Join-Path $OutputDir "rgupdate.exe"

Write-Host "🔄 Downloading latest rgupdate release..." -ForegroundColor Cyan
Write-Host "   From: $downloadUrl" -ForegroundColor Gray
Write-Host "   To:   $outputPath" -ForegroundColor Gray

try {
    # Create output directory if it doesn't exist
    $dir = Split-Path $outputPath -Parent
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    # Download the file
    Invoke-WebRequest -Uri $downloadUrl -OutFile $outputPath -UseBasicParsing
    
    Write-Host "✅ Download completed successfully!" -ForegroundColor Green
    Write-Host "   Saved to: $outputPath" -ForegroundColor Gray
    
    # Show file info
    $fileInfo = Get-Item $outputPath
    Write-Host "   Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "🚀 You can now run rgupdate:" -ForegroundColor Yellow
    Write-Host "   $outputPath --help" -ForegroundColor White
    
} catch {
    Write-Error "❌ Failed to download rgupdate: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "💡 You can also download manually from:" -ForegroundColor Yellow
    Write-Host "   $repoUrl/releases/latest" -ForegroundColor White
    exit 1
}
