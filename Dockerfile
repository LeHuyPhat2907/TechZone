# =========================================================================
# Multi-stage Dockerfile cho ứng dụng TechZone E-Commerce (.NET 10)
# =========================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy toàn bộ file định nghĩa project (.csproj) để tận dụng cơ chế Docker layer caching
COPY ["src/TechZone.Domain/TechZone.Domain.csproj", "src/TechZone.Domain/"]
COPY ["src/TechZone.Application/TechZone.Application.csproj", "src/TechZone.Application/"]
COPY ["src/TechZone.Infrastructure/TechZone.Infrastructure.csproj", "src/TechZone.Infrastructure/"]
COPY ["src/TechZone.Web/TechZone.Web.csproj", "src/TechZone.Web/"]

# Khôi phục các gói NuGet dependencies
RUN dotnet restore "src/TechZone.Web/TechZone.Web.csproj"

# Copy toàn bộ mã nguồn
COPY . .

# Biên dịch và publish bản phát hành tối ưu
WORKDIR "/src/src/TechZone.Web"
RUN dotnet publish "TechZone.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image (Siêu nhẹ, bảo mật & hiệu năng cao)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Copy sản phẩm đã publish từ Stage 1
COPY --from=build /app/publish .

# Tạo thư mục uploads cho hình ảnh sản phẩm & avatar người dùng
RUN mkdir -p /app/uploads && chmod -R 777 /app/uploads

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 5000

ENTRYPOINT ["dotnet", "TechZone.Web.dll"]
