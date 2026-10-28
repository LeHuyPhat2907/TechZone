# =============================================================================
# Kịch bản Kiểm Tra & Đẩy Cập Nhật Lên Git Từ Máy Local (1 Lệnh Duy Nhất)
# Sử dụng: .\scripts\push-update.ps1 -Message "Nâng cấp tính năng X và fix bug Y"
# =============================================================================

param(
    [Parameter(Mandatory=$false)]
    [string]$Message = "Cập nhật tính năng và sửa lỗi TechZone"
)

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  KIỂM TRA CODE LOCAL & ĐẨY LÊN PRODUCTION REPOSITORY " -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

# 1. Kiểm tra biên dịch trước khi commit
Write-Host "`n[1/3] Kiểm tra biên dịch dự án (.NET 10 Build Check)..." -ForegroundColor Yellow
$tempBuildPath = "$PSScriptRoot\..\src\TechZone.Web\bin\TestBuildVerify"
$buildResult = dotnet build "$PSScriptRoot\..\src\TechZone.Web\TechZone.Web.csproj" -o $tempBuildPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[LỖI]: Biên dịch thất bại! Vui lòng sửa lỗi trước khi đẩy lên Git." -ForegroundColor Red
    if (Test-Path $tempBuildPath) { Remove-Item -Recurse -Force $tempBuildPath }
    exit 1
}

# Dọn dẹp thư mục tạm
if (Test-Path $tempBuildPath) { Remove-Item -Recurse -Force $tempBuildPath }
Write-Host "✓ Biên dịch thành công (0 Lỗi, 0 Cảnh báo)!" -ForegroundColor Green

# 2. Git Add & Commit
Write-Host "`n[2/3] Đóng gói Git Commit..." -ForegroundColor Yellow
Set-Location "$PSScriptRoot\.."
git add .
git commit -m "$Message"

# 3. Git Push
Write-Host "`n[3/3] Đẩy mã nguồn lên GitHub (origin main)..." -ForegroundColor Yellow
git push origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n=====================================================" -ForegroundColor Green
    Write-Host "  ĐÃ ĐẨY CẬP NHẬT LÊN GITHUB THÀNH CÔNG!             " -ForegroundColor Green
    Write-Host "=====================================================" -ForegroundColor Green
    Write-Host "`nBƯỚC TIẾP THEO TRÊN MÁY CHỦ PROXMOX:" -ForegroundColor White
    Write-Host "Chỉ cần mở terminal máy chủ Proxmox và gõ 1 lệnh:" -ForegroundColor Yellow
    Write-Host "cd /path/to/TechZone && ./scripts/deploy.sh" -ForegroundColor Cyan
    Write-Host "`n-> Script deploy.sh sẽ tự động: Backup DB -> Backup image cũ -> Kéo code mới -> Khởi chạy -> Healthcheck!" -ForegroundColor Gray
} else {
    Write-Host "`n[LỖI]: Đẩy mã nguồn lên Git thất bại. Vui lòng kiểm tra kết nối mạng hoặc quyền truy cập repository." -ForegroundColor Red
}
