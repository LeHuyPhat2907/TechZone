#!/bin/bash
# =============================================================================
# Kịch bản Khôi Phục Khẩn Cấp (Manual Rollback) về Phiên Bản Cũ
# Sử dụng trên máy chủ Proxmox khi muốn quay xe về bản ổn định trước đó
# =============================================================================

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${YELLOW}=====================================================${NC}"
echo -e "${YELLOW}       TIẾN HÀNH KHÔI PHỤC PHIÊN BẢN CŨ (ROLLBACK)   ${NC}"
echo -e "${YELLOW}=====================================================${NC}"

if ! docker images | grep -q "techzone_web.*previous_backup"; then
    echo -e "${RED}LỖI: Không tìm thấy image 'techzone_web:previous_backup'!${NC}"
    echo "Danh sách images hiện có:"
    docker images | grep techzone
    exit 1
fi

echo -e "${YELLOW}1. Khôi phục Docker image trước đó...${NC}"
docker tag techzone_web:previous_backup techzone_web:latest

echo -e "${YELLOW}2. Khởi động lại container web với phiên bản cũ...${NC}"
docker compose stop web
docker compose up -d --no-build web

echo -e "${YELLOW}3. Kiểm tra trạng thái container...${NC}"
sleep 5
docker ps | grep techzone_web

echo -e "\n${GREEN}✓ ĐÃ ROLLBACK THÀNH CÔNG VỀ PHIÊN BẢN ỔN ĐỊNH TRƯỚC ĐÓ!${NC}"
