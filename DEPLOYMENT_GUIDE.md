# HƯỚNG DẪN TRIỂN KHAI TECHZONE LÊN PROXMOX QUA CLOUDFLARE TUNNEL
## (Kèm Quy Trình Cập Nhật 1-Lệnh & Tự Động Sao Lưu / Khôi Phục)

---

### I. KIẾN TRÚC HỆ THỐNG TRIỂN KHAI (ARCHITECTURE)

```
[Khách Hàng / Internet]
         │ (HTTPS / Tên miền riêng)
         ▼
[Cloudflare Edge Network] (Bảo vệ DDoS, Tự động cấp chứng chỉ SSL/TLS miễn phí)
         │ (Kênh bảo mật Cloudflare Tunnel mã hóa hai chiều)
         ▼
[Máy chủ Proxmox VE (Home / Lab Server)]
   └── [VM hoặc LXC Container: Ubuntu 24.04 / Debian 12]
         ├── cloudflared daemon (kết nối tunnel không cần mở port modem)
         └── Docker Compose Stack (Mạng nội bộ techzone_net):
               ├── techzone_web (.NET 10 MVC - Cổng 5000)
               │      └── Volume: web_uploads (Lưu ảnh sản phẩm/avatar)
               ├── techzone_mongodb (MongoDB 7.0 - Cổng 27017)
               │      └── Volume: mongo_data (Lưu trữ CSDL vĩnh viễn)
               └── techzone_redis (Redis 7.2 - Cổng 6379)
                      └── Volume: redis_data (Bộ nhớ đệm & Session)
```

> [!NOTE]
> **Ưu điểm vượt trội của Cloudflare Tunnel:**
> - **Không cần mở port Modem (No Port Forwarding)**: Hoạt động ngay cả khi mạng gia đình dùng CGNAT hoặc IP động.
> - **Bảo mật tuyệt đối**: Ẩn 100% địa chỉ IP thật của máy chủ Proxmox.
> - **Miễn phí hoàn toàn**: Đi kèm SSL HTTPS tự động gia hạn và bảo vệ chống tấn công DDoS.

---

### II. CHUẨN BỊ MÁY CHỦ TRÊN PROXMOX VE

#### 1. Tạo VM hoặc LXC Container
- **Hệ điều hành khuyến nghị**: `Ubuntu Server 24.04 LTS` hoặc `Debian 12`.
- **Cấu hình tối thiểu**:
  - **CPU**: 2 Cores.
  - **RAM**: 2 GB – 4 GB (Đủ cho .NET 10 + MongoDB + Redis).
  - **Ổ cứng**: 25 GB – 40 GB SSD.

#### 2. Cài đặt Docker & Docker Compose
Đăng nhập SSH vào máy chủ Proxmox VM/LXC và chạy lệnh:
```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y curl git docker.io docker-compose-plugin

# Cấp quyền docker cho user (không cần sudo)
sudo usermod -aG docker $USER
newgrp docker

# Kiểm tra docker hoạt động
docker --version
docker compose version
```

#### 3. Clone Repository TechZone về Máy Chủ
```bash
cd /opt
sudo git clone https://github.com/LeHuyPhat2907/TechZone.git
sudo chown -R $USER:$USER /opt/TechZone
cd /opt/TechZone

# Cấp quyền thực thi cho các script tự động
chmod +x scripts/*.sh
```

---

### III. CẤU HÌNH CLOUDFLARE TUNNEL (TRỎ TÊN MIỀN VỀ WEB)

1. Truy cập vào **[Cloudflare Zero Trust Dashboard](https://one.dash.cloudflare.com/)**.
2. Chọn menu **Networks** $\rightarrow$ **Tunnels** $\rightarrow$ bấm **Create a tunnel**.
3. Chọn loại kết nối: **Cloudflared** $\rightarrow$ Đặt tên Tunnel: `techzone-production` $\rightarrow$ bấm **Save tunnel**.
4. Cloudflare sẽ cấp 1 đoạn lệnh cài đặt tự động cho hệ điều hành Linux (dạng `curl -L ... | sudo bash -s --token <YOUR_TUNNEL_TOKEN>`). Hãy copy lệnh đó và chạy trên máy chủ Proxmox.
5. Quay lại Cloudflare Dashboard, chuyển sang tab **Public Hostname**:
   - **Subdomain**: Nhập tên mong muốn (VD: `shop` hoặc để trống nếu dùng domain gốc `@`).
   - **Domain**: Chọn tên miền đã có của bạn (VD: `yourdomain.com`).
   - **Type**: `HTTP`.
   - **URL**: `localhost:5000` (hoặc IP nội bộ của container TechZone).
   - Bấm **Save Hostname**.
6. **Xong!** Ngay lập tức trang web sẽ có thể truy cập toàn cầu qua `https://yourdomain.com` với SSL xanh lá.

---

### IV. QUẢN LÝ DỮ LIỆU & DI CHUYỂN CSDL (DATABASE MIGRATION)

#### 1. Nguyên tắc An toàn Dữ liệu (Persistence)
- File [`docker-compose.yml`](file:///D:/PhatProject/TechZone/docker-compose.yml) đã được cấu hình các Volume gắn kết:
  - `mongo_data:/data/db`: Toàn bộ sản phẩm, đơn hàng, người dùng, IMEI bảo hành lưu trữ vĩnh viễn trên ổ cứng máy chủ. Việc build lại code hay cập nhật web **tuyệt đối không làm mất dữ liệu CSDL**.
  - `web_uploads:/app/uploads`: Hình ảnh sản phẩm do bạn tải lên sẽ không bị ghi đè hay mất mát.

#### 2. Di chuyển dữ liệu từ Local lên Production (Lần đầu tiên)
- **Trên máy tính Local (Windows):**
  Mở PowerShell tại thư mục dự án và chạy:
  ```powershell
  .\scripts\export-local-db.ps1
  ```
  *(Dữ liệu CSDL `TechZoneDb` sẽ được xuất ra thư mục `./backups/initial_seed/TechZoneDb`)*

- **Copy lên máy chủ Proxmox:**
  ```powershell
  scp -r ./backups/initial_seed/TechZoneDb root@<PROXMOX_IP>:/opt/TechZone/backups/mongodb/
  ```

- **Trên máy chủ Proxmox (Khôi phục vào MongoDB Container):**
  ```bash
  cd /opt/TechZone
  docker compose up -d mongodb
  docker exec -i techzone_mongodb mongorestore --db TechZoneDb /mongo_backup/TechZoneDb
  ```

#### 3. Cài Đặt Tự Động Sao Lưu Hàng Ngày (Auto Daily Backup)
Thiết lập cron job để hệ thống tự động backup mỗi đêm lúc 02:00 sáng:
```bash
crontab -e
```
Thêm dòng sau vào cuối file:
```cron
0 2 * * * /opt/TechZone/scripts/backup-db.sh >> /var/log/techzone_db_backup.log 2>&1
```
*(Script sẽ tự động nén CSDL dạng `.tar.gz`, đánh dấu ngày giờ và tự xóa các bản sao lưu cũ hơn 14 ngày để không đầy dung lượng).*

---

### V. QUY TRÌNH CẬP NHẬT 1-LỆNH & TỰ ĐỘNG SAO LƯU / ROLLBACK

#### Bước 1: Khi bạn chỉnh sửa / sửa lỗi ở máy Local
Chỉ cần chạy 1 lệnh duy nhất trong PowerShell:
```powershell
.\scripts\push-update.ps1 -Message "Nâng cấp tính năng X và fix bug Y"
```
> **Script này tự động:**
> 1. Chạy `dotnet build` để kiểm tra có lỗi cú pháp không (nếu lỗi sẽ dừng ngay, không đẩy code hỏng lên).
> 2. Đóng gói Git commit kèm lời nhắn.
> 3. Tự động `git push origin main` lên GitHub repository.

#### Bước 2: Đẩy cập nhật lên Internet (Trên máy chủ Proxmox)
Mở terminal máy chủ Proxmox và gõ 1 lệnh duy nhất:
```bash
cd /opt/TechZone && ./scripts/deploy.sh
```

> [!IMPORTANT]
> **Quy trình 5 bước an toàn của `deploy.sh`:**
> 1. **Tự động sao lưu CSDL**: Dump CSDL MongoDB tại đúng thời điểm trước khi nâng cấp phòng ngừa sự cố.
> 2. **Tạo điểm khôi phục (Backup Image)**: Đổi tên Docker Image đang chạy thành `techzone_web:previous_backup`.
> 3. **Kéo mã nguồn mới**: `git pull origin main`.
> 4. **Biên dịch & Khởi chạy**: Build lại container phiên bản mới (`docker compose up -d --build web`).
> 5. **Tự động kiểm tra sức khỏe (Health-Check & Auto-Rollback)**:
>    - Thử gửi request đến `http://localhost:5000`.
>    - Nếu web hoạt động bình thường $\rightarrow$ Báo thành công.
>    - Nếu web bị lỗi (crash, loop, config sai) $\rightarrow$ **Tự động kích hoạt Rollback về bản cũ ngay lập tức**, đảm bảo website không bao giờ bị gián đoạn (Zero-Downtime).

#### Bước 3: Khôi phục thủ công khẩn cấp (Nếu muốn quay về bản cũ bất cứ lúc nào)
Nếu sau vài ngày chạy bạn muốn quay xe về phiên bản trước đó:
```bash
cd /opt/TechZone && ./scripts/rollback.sh
```
Hệ thống sẽ kích hoạt lại bản backup cũ trong 3 giây.
