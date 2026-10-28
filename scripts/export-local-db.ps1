# =============================================================================
# Kịch bản Xuất Dữ Liệu MongoDB từ Máy Local để Đưa lên Máy Chủ Proxmox
# =============================================================================

param(
    [string]$DbName = "TechZoneDb",
    [string]$OutputDir = "$PSScriptRoot\..\backups\initial_seed"
)

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  XUẤT DỮ LIỆU MONGODB TỪ LOCAL ĐỂ DI CHUYỂN SERVER  " -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# 1. Tìm công cụ mongodump trong tools local hoặc PATH
$toolPath = "$PSScriptRoot\tools\mongodump.exe"
if (-not (Test-Path $toolPath)) {
    $toolPath = (Get-Command mongodump -ErrorAction SilentlyContinue)?.Source
}

if (-not $toolPath -or -not (Test-Path $toolPath)) {
    Write-Host "Đang tự động tải công cụ MongoDB Tools độc lập..." -ForegroundColor Yellow
    $toolsDir = "$PSScriptRoot\tools"
    if (-not (Test-Path $toolsDir)) { New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null }
    $zipPath = "$toolsDir\tools.zip"
    curl.exe -L "https://fastdl.mongodb.org/tools/db/mongodb-database-tools-windows-x86_64-100.10.0.zip" -o $zipPath
    Expand-Archive -Path $zipPath -DestinationPath "$toolsDir\extracted" -Force
    $innerBin = Get-ChildItem -Path "$toolsDir\extracted" -Recurse -Filter "mongodump.exe" | Select-Object -First 1
    if ($innerBin) {
        Copy-Item -Path "$($innerBin.Directory.FullName)\*" -Destination "$toolsDir" -Force
    }
    Remove-Item -Recurse -Force "$toolsDir\extracted", $zipPath -ErrorAction SilentlyContinue
    $toolPath = "$toolsDir\mongodump.exe"
}

Write-Host "Đang xuất toàn bộ dữ liệu CSDL '$DbName'..." -ForegroundColor Yellow
& $toolPath --db $DbName --out $OutputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Xuất dữ liệu thành công vào: $OutputDir\$DbName" -ForegroundColor Green
    
    # Nén thành file tar.gz sẵn sàng gửi lên Proxmox
    $tarPath = "$PSScriptRoot\..\backups\TechZoneDb.tar.gz"
    tar.exe -czvf $tarPath -C $OutputDir $DbName
    Write-Host "✓ Đã đóng gói thành file: $tarPath (Sẵn sàng copy lên Proxmox)" -ForegroundColor Green
    
    Write-Host "`nCÁCH CHUYỂN LÊN PROXMOX:" -ForegroundColor Cyan
    Write-Host "scp $tarPath root@<PROXMOX_IP>:/opt/TechZone/backups/mongodb/" -ForegroundColor White
} else {
    Write-Host "[LỖI]: Không thể dump cơ sở dữ liệu. Vui lòng kiểm tra MongoDB local có đang chạy không." -ForegroundColor Red
}
