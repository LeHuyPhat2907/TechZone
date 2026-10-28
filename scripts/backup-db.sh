#!/bin/bash
# =============================================================================
# Kịch bản Sao Lưu Định Kỳ Tự Động cho Cơ sở dữ liệu MongoDB
# Khuyến nghị: Thêm vào crontab (crontab -e):
# 0 2 * * * /path/to/TechZone/scripts/backup-db.sh >> /var/log/techzone_backup.log 2>&1
# =============================================================================

set -e

BACKUP_DIR="./backups/mongodb"
mkdir -p "$BACKUP_DIR"

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_NAME="TechZoneDb_daily_${TIMESTAMP}"

echo "[$(date)] Bắt đầu tiến trình sao lưu cơ sở dữ liệu MongoDB..."

# Dump database bên trong container
docker exec techzone_mongodb mongodump --db TechZoneDb --out /mongo_backup/${BACKUP_NAME}

# Nén thành file .tar.gz để tiết kiệm dung lượng đĩa
tar -czf "${BACKUP_DIR}/${BACKUP_NAME}.tar.gz" -C "${BACKUP_DIR}" "${BACKUP_NAME}"
rm -rf "${BACKUP_DIR}/${BACKUP_NAME}"

echo "[$(date)] Đã tạo file sao lưu: ${BACKUP_DIR}/${BACKUP_NAME}.tar.gz"

# Tự động dọn dẹp các bản backup cũ hơn 14 ngày
find "${BACKUP_DIR}" -name "TechZoneDb_*.tar.gz" -mtime +14 -exec rm {} \;
echo "[$(date)] Đã dọn dẹp các bản sao lưu cũ hơn 14 ngày thành công."
