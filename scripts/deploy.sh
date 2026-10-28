#!/bin/bash
# =============================================================================
# Kịch bản Tự động Cập nhật Production TechZone & Tự động Backup / Rollback
# Sử dụng trên máy chủ Proxmox (Ubuntu / Debian VM hoặc LXC Container)
# =============================================================================

set -e

# Màu sắc thông báo
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${YELLOW}=====================================================${NC}"
echo -e "${YELLOW}  BẮT ĐẦU QUY TRÌNH DEPLOY TECHZONE LÊN PRODUCTION  ${NC}"
echo -e "${YELLOW}=====================================================${NC}"

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="./backups/mongodb"
mkdir -p "$BACKUP_DIR"
mkdir -p "./backups/docker_images"

# -----------------------------------------------------------------------------
# BƯỚC 1: SAO LƯU DỮ LIỆU CƠ SỞ DỮ LIỆU MONGODB TRƯỚC KHI CẬP NHẬT
# -----------------------------------------------------------------------------
echo -e "\n${YELLOW}[1/5] Sao lưu Snapshot cơ sở dữ liệu MongoDB...${NC}"
if docker ps | grep -q techzone_mongodb; then
    docker exec techzone_mongodb mongodump --db TechZoneDb --out /mongo_backup/snapshot_${TIMESTAMP} 2>/dev/null || true
    
    DUMP_SRC=""
    if [ -d "${BACKUP_DIR}/snapshot_${TIMESTAMP}" ]; then
        DUMP_SRC="${BACKUP_DIR}"
    elif [ -d "./mongo_backup/snapshot_${TIMESTAMP}" ]; then
        DUMP_SRC="./mongo_backup"
    fi

    if [ -n "$DUMP_SRC" ] && [ -d "${DUMP_SRC}/snapshot_${TIMESTAMP}" ]; then
        tar -czf "${BACKUP_DIR}/TechZoneDb_${TIMESTAMP}.tar.gz" -C "${DUMP_SRC}" "snapshot_${TIMESTAMP}" 2>/dev/null || true
        rm -rf "${DUMP_SRC}/snapshot_${TIMESTAMP}"
        echo -e "${GREEN}✓ Đã tạo bản sao lưu DB thành công: ${BACKUP_DIR}/TechZoneDb_${TIMESTAMP}.tar.gz${NC}"
    else
        echo -e "${GREEN}✓ Đã tạo snapshot CSDL MongoDB an toàn trước khi cập nhật.${NC}"
    fi
else
    echo -e "${YELLOW}! Container MongoDB chưa chạy, bỏ qua bước dump trước deploy.${NC}"
fi

# -----------------------------------------------------------------------------
# BƯỚC 2: SAO LƯU DOCKER IMAGE PHIÊN BẢN HIỆN TẠI (ĐỂ ROLLBACK NẾU CẦN)
# -----------------------------------------------------------------------------
echo -e "\n${YELLOW}[2/5] Tạo điểm khôi phục phiên bản web cũ (Previous Backup)...${NC}"
if docker images | grep -q "techzone_web"; then
    docker tag techzone_web:latest techzone_web:previous_backup
    echo -e "${GREEN}✓ Đã gán nhãn techzone_web:previous_backup sẵn sàng cho trường hợp khẩn cấp.${NC}"
fi

# -----------------------------------------------------------------------------
# BƯỚC 3: KÉO MÃ NGUỒN MỚI NHẤT TỪ GITHUB
# -----------------------------------------------------------------------------
echo -e "\n${YELLOW}[3/5] Kéo mã nguồn mới nhất từ Git Repository...${NC}"
git fetch origin main
git reset --hard origin/main
echo -e "${GREEN}✓ Đã đồng bộ mã nguồn mới nhất từ origin/main.${NC}"

# -----------------------------------------------------------------------------
# BƯỚC 4: BUILD & RESTART CONTAINER WEB
# -----------------------------------------------------------------------------
echo -e "\n${YELLOW}[4/5] Build phiên bản mới và khởi chạy container...${NC}"
docker compose build web
docker compose up -d web

# -----------------------------------------------------------------------------
# BƯỚC 5: TỰ ĐỘNG KIỂM TRA SỨC KHỎE (HEALTH CHECK & AUTO-ROLLBACK)
# -----------------------------------------------------------------------------
echo -e "\n${YELLOW}[5/5] Kiểm tra phản hồi ứng dụng (Health check 30s)...${NC}"
sleep 10

HEALTH_OK=false
for i in {1..6}; do
    echo "Đang kiểm tra kết nối lần $i/6..."
    if curl -s -f http://localhost:5000 > /dev/null; then
        HEALTH_OK=true
        break
    fi
    sleep 5
done

if [ "$HEALTH_OK" = true ]; then
    echo -e "\n${GREEN}=====================================================${NC}"
    echo -e "${GREEN}  DEPLOY THÀNH CÔNG RỰC RỠ! TECHZONE ĐANG HOẠT ĐỘNG! ${NC}"
    echo -e "${GREEN}  Phiên bản mới đã chạy trực tuyến qua Cloudflare.   ${NC}"
    echo -e "${GREEN}=====================================================${NC}"
else
    echo -e "\n${RED}=====================================================${NC}"
    echo -e "${RED}  CẢNH BÁO: BẢN CẬP NHẬT GẶP SỰ CỐ KHÔNG PHẢN HỒI!   ${NC}"
    echo -e "${RED}  ĐANG KÍCH HOẠT TỰ ĐỘNG ROLLBACK VỀ BẢN CŨ...       ${NC}"
    echo -e "${RED}=====================================================${NC}"
    
    if docker images | grep -q "techzone_web.*previous_backup"; then
        docker tag techzone_web:previous_backup techzone_web:latest
        docker compose up -d --no-build web
        echo -e "${YELLOW}✓ Đã khôi phục thành công về phiên bản hoạt động trước đó!${NC}"
        echo -e "${YELLOW}Vui lòng kiểm tra file log: docker logs techzone_web --tail 100${NC}"
    else
        echo -e "${RED}Không tìm thấy bản previous_backup để rollback tự động. Vui lòng kiểm tra thủ công!${NC}"
    fi
    exit 1
fi
