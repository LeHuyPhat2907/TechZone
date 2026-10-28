# =============================================================================
# Kịch bản Xuất Dữ Liệu MongoDB từ Máy Local để Đưa lên Máy Chủ Proxmox
# =============================================================================

param(
    [string]$DbName = "TechZoneDb",
    [string]$OutputDir = "./backups/initial_seed"
)

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  XUẤT DỮ LIỆU MONGODB TỪ LOCAL ĐỂ DI CHUYỂN SERVER  " -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$dumpTool = "mongodump"
$dumpFound = Get-Command $dumpTool -ErrorAction SilentlyContinue

if ($dumpFound) {
    Write-Host "Đang xuất cơ sở dữ liệu '$DbName' bằng mongodump..." -ForegroundColor Yellow
    & mongodump --db $DbName --out $OutputDir
    Write-Host "✓ Đã xuất dữ liệu thành công vào thư mục: $OutputDir" -ForegroundColor Green
    Write-Host ""
    Write-Host "BƯỚC TIẾP THEO:" -ForegroundColor Yellow
    Write-Host "1. Nén thư mục '$OutputDir/$DbName' thành file zip/tar."
    Write-Host "2. Copy lên máy chủ Proxmox (thư mục ./backups/mongodb của dự án)."
    Write-Host "3. Trên Proxmox, chạy lệnh restore:"
    Write-Host "   docker exec -i techzone_mongodb mongorestore --db TechZoneDb /mongo_backup/$DbName" -ForegroundColor Cyan
} else {
    Write-Host "Không tìm thấy công cụ mongodump trong PATH hệ thống." -ForegroundColor Yellow
    Write-Host "Nếu MongoDB chạy bằng Docker trên máy local, bạn có thể chạy lệnh sau trong PowerShell:" -ForegroundColor White
    Write-Host "docker exec <local_mongo_container> mongodump --db $DbName --out /mongo_backup" -ForegroundColor Cyan
}
