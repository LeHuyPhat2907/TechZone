using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using TechZone.Application.Common.Interfaces;
using TechZone.Domain.Common;
using TechZone.Domain.Entities;
using TechZone.Domain.Enums;

namespace TechZone.Infrastructure.Data;

public class DbSeeder
{
    private readonly IMongoDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(IMongoDbContext context, IPasswordHasher passwordHasher, ILogger<DbSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting TechZone database seeding...");

            await SeedRolesAsync();
            await SeedUsersAsync();
            var categories = await SeedCategoriesAsync();
            var brands = await SeedBrandsAsync();
            await SeedProductsAndInventoryAsync(categories, brands);
            await SeedCouponsAsync();
            await SeedBannersAsync();
            await SeedSuppliersAndStockSlipsAsync();
            await SeedShippingCarriersAsync();
            await SeedWarrantyPoliciesAsync();
            await SeedSparePartsAsync();
            await SeedWarehouseDataAsync();

            _logger.LogInformation("TechZone database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during database seeding.");
        }
    }

    private async Task SeedUsersAsync()
    {
        var existingCount = await _context.Users.CountDocumentsAsync(u => !u.IsDeleted);
        if (existingCount > 0) return;

        var users = new List<User>
        {
            new()
            {
                FullName = "Nguyễn Văn Phát (Super Admin)",
                Email = "admin@techzone.vn",
                PasswordHash = _passwordHasher.HashPassword("Admin@123"),
                PhoneNumber = "0901234567",
                Role = UserRole.SuperAdmin,
                IsActive = true,
                IsEmailVerified = true,
                RewardPoints = 50000,
                Addresses = new List<UserAddress>
                {
                    new()
                    {
                        ReceiverName = "Nguyễn Văn Phát",
                        PhoneNumber = "0901234567",
                        Province = "Thành phố Hồ Chí Minh",
                        District = "Quận 1",
                        Ward = "Phường Bến Nghé",
                        DetailAddress = "Số 123 Đường Lê Lợi",
                        IsDefault = true
                    }
                }
            },
            new()
            {
                FullName = "Trần Thị Thu Thảo (Nhân viên Bán hàng)",
                Email = "sales@techzone.vn",
                PasswordHash = _passwordHasher.HashPassword("Staff@123"),
                PhoneNumber = "0912345678",
                Role = UserRole.SalesStaff,
                IsActive = true,
                IsEmailVerified = true
            },
            new()
            {
                FullName = "Lê Hoàng Kho (Thủ kho)",
                Email = "kho@techzone.vn",
                PasswordHash = _passwordHasher.HashPassword("Staff@123"),
                PhoneNumber = "0923456789",
                Role = UserRole.WarehouseStaff,
                IsActive = true,
                IsEmailVerified = true
            },
            new()
            {
                FullName = "Vũ Kỹ Thuật (Kỹ thuật viên Bảo hành)",
                Email = "baohanh@techzone.vn",
                PasswordHash = _passwordHasher.HashPassword("Staff@123"),
                PhoneNumber = "0934567890",
                Role = UserRole.Technician,
                Department = "Kỹ thuật",
                IsActive = true,
                IsEmailVerified = true
            },
            new()
            {
                FullName = "Đặng Thu Hương (Kế toán trưởng)",
                Email = "ketoan@techzone.vn",
                PasswordHash = _passwordHasher.HashPassword("Staff@123"),
                PhoneNumber = "0945678901",
                Role = UserRole.Accountant,
                Department = "Kế toán",
                IsActive = true,
                IsEmailVerified = true
            },
            new()
            {
                FullName = "Đặng Minh Tâm (Khách hàng VIP)",
                Email = "khachhang@techzone.vn",
                PasswordHash = _passwordHasher.HashPassword("Customer@123"),
                PhoneNumber = "0987654321",
                Role = UserRole.Customer,
                IsActive = true,
                IsEmailVerified = true,
                RewardPoints = 1500,
                Addresses = new List<UserAddress>
                {
                    new()
                    {
                        ReceiverName = "Đặng Minh Tâm",
                        PhoneNumber = "0987654321",
                        Province = "Thành phố Hồ Chí Minh",
                        District = "Quận 3",
                        Ward = "Phường Võ Thị Sáu",
                        DetailAddress = "Số 456 Đường Nguyễn Thị Minh Khai",
                        IsDefault = true
                    }
                }
            }
        };

        await _context.Users.InsertManyAsync(users);
        _logger.LogInformation("Seeded {Count} users.", users.Count);
    }

    private async Task<Dictionary<string, Category>> SeedCategoriesAsync()
    {
        var existingCategories = await _context.Categories.Find(c => !c.IsDeleted).ToListAsync();
        if (existingCategories.Count > 0)
        {
            return existingCategories.ToDictionary(c => c.Slug, c => c);
        }

        var categories = new List<Category>
        {
            new()
            {
                Name = "Điện thoại",
                Slug = "dien-thoai",
                Description = "Điện thoại thông minh chính hãng Apple, Samsung, Xiaomi...",
                IconUrl = "bi-phone",
                DisplayOrder = 1,
                IsActive = true,
                Level = 1
            },
            new()
            {
                Name = "Laptop",
                Slug = "laptop",
                Description = "Laptop gaming, đồ họa, văn phòng cao cấp từ Asus, Dell, Apple...",
                IconUrl = "bi-laptop",
                DisplayOrder = 2,
                IsActive = true,
                Level = 1
            },
            new()
            {
                Name = "Máy tính bảng",
                Slug = "may-tinh-bang",
                Description = "iPad, Samsung Galaxy Tab phục vụ học tập và sáng tạo nghệ thuật...",
                IconUrl = "bi-tablet",
                DisplayOrder = 3,
                IsActive = true,
                Level = 1
            },
            new()
            {
                Name = "Tai nghe & Âm thanh",
                Slug = "tai-nghe-am-thanh",
                Description = "Tai nghe chống ồn, loa Bluetooth đỉnh cao Sony, Apple...",
                IconUrl = "bi-headphones",
                DisplayOrder = 4,
                IsActive = true,
                Level = 1
            },
            new()
            {
                Name = "Đồng hồ thông minh",
                Slug = "dong-ho-thong-minh",
                Description = "Apple Watch, Galaxy Watch đo nhịp tim, oxy máu, thể thao chuyên nghiệp...",
                IconUrl = "bi-smartwatch",
                DisplayOrder = 5,
                IsActive = true,
                Level = 1
            },
            new()
            {
                Name = "Phụ kiện",
                Slug = "phu-kien",
                Description = "Củ sạc GaN, cáp sạc nhanh, chuột công thái học chính hãng...",
                IconUrl = "bi-plug",
                DisplayOrder = 6,
                IsActive = true,
                Level = 1
            }
        };

        await _context.Categories.InsertManyAsync(categories);
        _logger.LogInformation("Seeded {Count} categories.", categories.Count);
        return categories.ToDictionary(c => c.Slug, c => c);
    }

    private async Task<Dictionary<string, Brand>> SeedBrandsAsync()
    {
        var existingBrands = await _context.Brands.Find(b => !b.IsDeleted).ToListAsync();
        if (existingBrands.Count > 0)
        {
            return existingBrands.ToDictionary(b => b.Slug, b => b);
        }

        var brands = new List<Brand>
        {
            new() { Name = "Apple", Slug = "apple", CountryOfOrigin = "Hoa Kỳ", IsFeatured = true, DisplayOrder = 1, Description = "Thương hiệu công nghệ hàng đầu thế giới với iPhone, iPad, Mac" },
            new() { Name = "Samsung", Slug = "samsung", CountryOfOrigin = "Hàn Quốc", IsFeatured = true, DisplayOrder = 2, Description = "Tập đoàn công nghệ tiên phong về màn hình gập và Galaxy AI" },
            new() { Name = "ASUS", Slug = "asus", CountryOfOrigin = "Đài Loan", IsFeatured = true, DisplayOrder = 3, Description = "Nhà sản xuất bo mạch chủ và laptop gaming ROG số 1" },
            new() { Name = "Dell", Slug = "dell", CountryOfOrigin = "Hoa Kỳ", IsFeatured = true, DisplayOrder = 4, Description = "Thương hiệu laptop doanh nhân XPS và đồ họa hàng đầu" },
            new() { Name = "Sony", Slug = "sony", CountryOfOrigin = "Nhật Bản", IsFeatured = true, DisplayOrder = 5, Description = "Đỉnh cao âm thanh chống ồn và giải trí đỉnh cao" },
            new() { Name = "Xiaomi", Slug = "xiaomi", CountryOfOrigin = "Trung Quốc", IsFeatured = true, DisplayOrder = 6, Description = "Hệ sinh thái thông minh với cấu hình mạnh mẽ, giá hợp lý" },
            new() { Name = "Logitech", Slug = "logitech", CountryOfOrigin = "Thụy Sĩ", IsFeatured = true, DisplayOrder = 7, Description = "Thương hiệu phụ kiện chuột, bàn phím số 1 thế giới" },
            new() { Name = "Anker", Slug = "anker", CountryOfOrigin = "Hoa Kỳ", IsFeatured = true, DisplayOrder = 8, Description = "Tiên phong công nghệ củ sạc GaN và pin dự phòng" }
        };

        await _context.Brands.InsertManyAsync(brands);
        _logger.LogInformation("Seeded {Count} brands.", brands.Count);
        return brands.ToDictionary(b => b.Slug, b => b);
    }

    private async Task SeedProductsAndInventoryAsync(Dictionary<string, Category> catMap, Dictionary<string, Brand> brandMap)
    {
        var existingCount = await _context.Products.CountDocumentsAsync(p => !p.IsDeleted);
        if (existingCount > 0) return;

        var products = new List<Product>();
        var inventoryItems = new List<InventoryItem>();

        // 1. iPhone 15 Pro Max
        var p1 = new Product
        {
            Name = "iPhone 15 Pro Max",
            Slug = "iphone-15-pro-max",
            Sku = "IPHONE15PM",
            CategoryId = catMap["dien-thoai"].Id,
            CategoryName = "Điện thoại",
            BrandId = brandMap["apple"].Id,
            BrandName = "Apple",
            ThumbnailUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=600&auto=format&fit=crop&q=80",
            OriginalPrice = 34990000m,
            Price = 29990000m,
            WarrantyMonths = 12,
            HasVariants = true,
            StockQuantity = 25,
            AvailableQuantity = 25,
            RatingAverage = 4.9,
            RatingCount = 128,
            IsFeatured = true,
            IsBestSeller = true,
            ShortDescription = "Khung viền Titan chuẩn hàng không vũ trụ, chip A17 Pro cực mạnh, camera tiềm vọng zoom quang học 5x.",
            Description = "iPhone 15 Pro Max là chiếc điện thoại đỉnh cao nhất của Apple. Với thiết kế Titan nguyên khối siêu nhẹ và bền bỉ, màn hình Super Retina XDR 6.7 inch với ProMotion 120Hz và Dynamic Island thông minh. Trải nghiệm đồ họa đỉnh cao với chip A17 Pro 3nm đầu tiên trên thế giới.",
            HighlightSpecs = new List<string> { "Màn hình 6.7\" OLED 120Hz", "Chip Apple A17 Pro (3nm)", "Camera 48MP Zoom 5x", "Pin 4.422 mAh Sạc nhanh" },
            Variants = new List<ProductVariant>
            {
                new() { Sku = "IP15PM-256-NAT", VariantName = "Titan Tự Nhiên - 256GB", Color = "Titan Tự Nhiên", ColorHex = "#9E9A93", Storage = "256GB", Ram = "8GB", OriginalPrice = 34990000m, Price = 29990000m, StockQuantity = 10, AvailableQuantity = 10, ImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=600&auto=format&fit=crop&q=80" },
                new() { Sku = "IP15PM-256-BLU", VariantName = "Titan Xanh - 256GB", Color = "Titan Xanh", ColorHex = "#2E3B4E", Storage = "256GB", Ram = "8GB", OriginalPrice = 34990000m, Price = 29990000m, StockQuantity = 8, AvailableQuantity = 8, ImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=600&auto=format&fit=crop&q=80" },
                new() { Sku = "IP15PM-512-NAT", VariantName = "Titan Tự Nhiên - 512GB", Color = "Titan Tự Nhiên", ColorHex = "#9E9A93", Storage = "512GB", Ram = "8GB", OriginalPrice = 40990000m, Price = 35990000m, StockQuantity = 7, AvailableQuantity = 7, ImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=600&auto=format&fit=crop&q=80" }
            },
            Specifications = new List<SpecificationGroup>
            {
                new() { GroupName = "Màn hình", Items = new() { new() { Name = "Công nghệ màn hình", Value = "Super Retina XDR OLED, ProMotion 120Hz" }, new() { Name = "Kích thước", Value = "6.7 inch" }, new() { Name = "Độ phân giải", Value = "2796 x 1290 Pixels" } } },
                new() { GroupName = "Hệ thống Camera", Items = new() { new() { Name = "Camera sau", Value = "Chính 48 MP & Phụ 12 MP, 12 MP (Tele 5x)" }, new() { Name = "Quay phim", Value = "4K 60fps, ProRes, Action Mode" } } },
                new() { GroupName = "Vi xử lý & Bộ nhớ", Items = new() { new() { Name = "Chip xử lý (CPU)", Value = "Apple A17 Pro 6 nhân (3nm)" }, new() { Name = "Chip đồ họa (GPU)", Value = "Apple GPU 6 nhân" } } }
            }
        };
        products.Add(p1);

        // 2. Samsung Galaxy S24 Ultra
        var p2 = new Product
        {
            Name = "Samsung Galaxy S24 Ultra 5G",
            Slug = "samsung-galaxy-s24-ultra",
            Sku = "SAMS24ULTRA",
            CategoryId = catMap["dien-thoai"].Id,
            CategoryName = "Điện thoại",
            BrandId = brandMap["samsung"].Id,
            BrandName = "Samsung",
            ThumbnailUrl = "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?w=600&auto=format&fit=crop&q=80",
            OriginalPrice = 33990000m,
            Price = 27490000m,
            WarrantyMonths = 12,
            HasVariants = true,
            StockQuantity = 20,
            AvailableQuantity = 20,
            RatingAverage = 4.8,
            RatingCount = 95,
            IsFeatured = true,
            ShortDescription = "Khung viền Titan phẳng, tích hợp quyền năng Galaxy AI phiên dịch trực tiếp và Bút S-Pen tiện ích.",
            Description = "Galaxy S24 Ultra mở ra kỷ nguyên mới của trí tuệ nhân tạo di động Galaxy AI: Khoanh tròn để tìm kiếm trên Google, trợ lý phiên dịch cuộc gọi hai chiều theo thời gian thực và trợ lý ghi chú thông minh. Màn hình phẳng Dynamic AMOLED 2X 2600 nits chống chói Corning Gorilla Armor.",
            HighlightSpecs = new List<string> { "Màn hình 6.8\" Dynamic AMOLED 2X", "Snapdragon 8 Gen 3 for Galaxy", "Camera 200MP Zoom 100x", "Bút S-Pen tích hợp" },
            Variants = new List<ProductVariant>
            {
                new() { Sku = "S24U-256-GRY", VariantName = "Xám Titan - 256GB", Color = "Xám Titan", ColorHex = "#5E5F61", Storage = "256GB", Ram = "12GB", OriginalPrice = 33990000m, Price = 27490000m, StockQuantity = 12, AvailableQuantity = 12, ImageUrl = "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?w=600&auto=format&fit=crop&q=80" },
                new() { Sku = "S24U-256-BLK", VariantName = "Đen Titan - 256GB", Color = "Đen Titan", ColorHex = "#212121", Storage = "256GB", Ram = "12GB", OriginalPrice = 33990000m, Price = 27490000m, StockQuantity = 8, AvailableQuantity = 8, ImageUrl = "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?w=600&auto=format&fit=crop&q=80" }
            },
            Specifications = new List<SpecificationGroup>
            {
                new() { GroupName = "Màn hình", Items = new() { new() { Name = "Công nghệ", Value = "Dynamic AMOLED 2X, 120Hz, 2600 nits" }, new() { Name = "Kích thước", Value = "6.8 inch Quad HD+" } } },
                new() { GroupName = "Cấu hình", Items = new() { new() { Name = "CPU", Value = "Snapdragon 8 Gen 3 for Galaxy (4nm)" }, new() { Name = "RAM", Value = "12 GB" } } }
            }
        };
        products.Add(p2);

        // 3. MacBook Pro 14 M3 Pro
        var p3 = new Product
        {
            Name = "MacBook Pro 14 inch M3 Pro (18GB/512GB)",
            Slug = "macbook-pro-14-m3-pro",
            Sku = "MBP14M3PRO",
            CategoryId = catMap["laptop"].Id,
            CategoryName = "Laptop",
            BrandId = brandMap["apple"].Id,
            BrandName = "Apple",
            ThumbnailUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=600&auto=format&fit=crop&q=80",
            OriginalPrice = 49990000m,
            Price = 46490000m,
            WarrantyMonths = 12,
            HasVariants = true,
            StockQuantity = 15,
            AvailableQuantity = 15,
            RatingAverage = 5.0,
            RatingCount = 42,
            IsFeatured = true,
            ShortDescription = "Sức mạnh chuyên nghiệp vượt bậc với chip M3 Pro, màu Space Black chống bám vân tay sang trọng.",
            Description = "MacBook Pro 14 inch M3 Pro mang lại hiệu năng phi thường cho các tác vụ lập trình, dựng video 8K, đồ họa 3D. Màn hình Liquid Retina XDR với độ sáng lên đến 1600 nits, thời lượng pin kinh ngạc lên đến 18 tiếng.",
            HighlightSpecs = new List<string> { "Apple M3 Pro 11-core CPU", "GPU 14-core Ray Tracing", "RAM 18GB Unified", "Liquid Retina XDR 120Hz" },
            Variants = new List<ProductVariant>
            {
                new() { Sku = "MBP14-M3P-BLK", VariantName = "Space Black - 18GB/512GB", Color = "Space Black", ColorHex = "#1D1E20", Storage = "512GB SSD", Ram = "18GB", OriginalPrice = 49990000m, Price = 46490000m, StockQuantity = 8, AvailableQuantity = 8, ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=600&auto=format&fit=crop&q=80" },
                new() { Sku = "MBP14-M3P-SLV", VariantName = "Silver - 18GB/512GB", Color = "Silver", ColorHex = "#E3E4E5", Storage = "512GB SSD", Ram = "18GB", OriginalPrice = 49990000m, Price = 46490000m, StockQuantity = 7, AvailableQuantity = 7, ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=600&auto=format&fit=crop&q=80" }
            },
            Specifications = new List<SpecificationGroup>
            {
                new() { GroupName = "Hiệu năng", Items = new() { new() { Name = "Vi xử lý", Value = "Apple M3 Pro (11-core CPU, 14-core GPU)" }, new() { Name = "RAM", Value = "18GB Unified Memory" }, new() { Name = "Ổ cứng", Value = "512GB PCIe SSD tốc độ 6000MB/s" } } },
                new() { GroupName = "Màn hình", Items = new() { new() { Name = "Kích thước & Công nghệ", Value = "14.2 inch Liquid Retina XDR, Mini-LED 120Hz" } } }
            }
        };
        products.Add(p3);

        // 4. ASUS ROG Zephyrus G16 (2024)
        var p4 = new Product
        {
            Name = "Laptop Gaming ASUS ROG Zephyrus G16 (2024)",
            Slug = "asus-rog-zephyrus-g16-2024",
            Sku = "ROG-G16-2024",
            CategoryId = catMap["laptop"].Id,
            CategoryName = "Laptop",
            BrandId = brandMap["asus"].Id,
            BrandName = "ASUS",
            ThumbnailUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=600&auto=format&fit=crop&q=80",
            OriginalPrice = 62990000m,
            Price = 57990000m,
            WarrantyMonths = 24,
            HasVariants = false,
            StockQuantity = 10,
            AvailableQuantity = 10,
            RatingAverage = 4.9,
            RatingCount = 31,
            IsFeatured = true,
            ShortDescription = "Laptop AI Gaming mỏng nhẹ chỉ 1.85kg, vi xử lý Intel Core Ultra 9 185H và đồ họa RTX 4070 8GB.",
            Description = "ROG Zephyrus G16 2024 định nghĩa lại laptop gaming cao cấp. Vỏ nhôm CNC nguyên khối với dải đèn Slash Lighting độc đáo. Màn hình ROG Nebula OLED 2.5K 240Hz 0.2ms chuẩn điện ảnh DCI-P3 100%. Tản nhiệt kim loại lỏng ROG Intelligent Cooling.",
            HighlightSpecs = new List<string> { "Intel Core Ultra 9 185H", "NVIDIA RTX 4070 8GB", "32GB RAM LPDDR5X", "Màn 2.5K OLED 240Hz" },
            Specifications = new List<SpecificationGroup>
            {
                new() { GroupName = "Cấu hình", Items = new() { new() { Name = "CPU", Value = "Intel Core Ultra 9 185H (16 nhân, 22 luồng, NPU Intel AI Boost)" }, new() { Name = "Card đồ họa", Value = "NVIDIA GeForce RTX 4070 8GB GDDR6 (105W TGP)" }, new() { Name = "RAM", Value = "32GB LPDDR5X 7467MHz" }, new() { Name = "Ổ cứng", Value = "1TB M.2 NVMe PCIe 4.0 SSD" } } },
                new() { GroupName = "Màn hình", Items = new() { new() { Name = "Kích thước & Tần số quét", Value = "16.0 inch 2.5K (2560 x 1600) OLED, 240Hz, 0.2ms, G-Sync" } } }
            }
        };
        products.Add(p4);

        // 5. iPad Pro M4 11 inch
        var p5 = new Product
        {
            Name = "iPad Pro M4 11 inch Wi-Fi 256GB",
            Slug = "ipad-pro-m4-11-inch",
            Sku = "IPAD-M4-11",
            CategoryId = catMap["may-tinh-bang"].Id,
            CategoryName = "Máy tính bảng",
            BrandId = brandMap["apple"].Id,
            BrandName = "Apple",
            ThumbnailUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=600&auto=format&fit=crop&q=80",
            OriginalPrice = 28990000m,
            Price = 26490000m,
            WarrantyMonths = 12,
            HasVariants = true,
            StockQuantity = 18,
            AvailableQuantity = 18,
            RatingAverage = 4.9,
            RatingCount = 55,
            IsFeatured = true,
            ShortDescription = "Thiết kế siêu mỏng nhẹ chỉ 5.3mm, chip Apple M4 đột phá, màn hình Tandem OLED Ultra Retina XDR rực rỡ.",
            Description = "iPad Pro M4 là bước nhảy vọt của Apple. Trang bị chip M4 thế hệ kế tiếp với hiệu năng CPU nhanh hơn 1.5 lần và khả năng xử lý AI lên đến 38 nghìn tỷ phép tính mỗi giây (TOPS). Màn hình Ultra Retina XDR sử dụng công nghệ OLED hai lớp Tandem.",
            HighlightSpecs = new List<string> { "Apple M4 9-core CPU", "Màn hình Tandem OLED", "Độ mỏng chỉ 5.3mm", "Tương thích Apple Pencil Pro" },
            Variants = new List<ProductVariant>
            {
                new() { Sku = "IPADM4-11-BLK", VariantName = "Space Black - 256GB", Color = "Space Black", ColorHex = "#1D1E20", Storage = "256GB", Ram = "8GB", OriginalPrice = 28990000m, Price = 26490000m, StockQuantity = 10, AvailableQuantity = 10, ImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=600&auto=format&fit=crop&q=80" },
                new() { Sku = "IPADM4-11-SLV", VariantName = "Silver - 256GB", Color = "Silver", ColorHex = "#E3E4E5", Storage = "256GB", Ram = "8GB", OriginalPrice = 28990000m, Price = 26490000m, StockQuantity = 8, AvailableQuantity = 8, ImageUrl = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=600&auto=format&fit=crop&q=80" }
            }
        };
        products.Add(p5);

        // 6. Sony WH-1000XM5
        var p6 = new Product
        {
            Name = "Tai nghe Chống ồn Sony WH-1000XM5",
            Slug = "sony-wh-1000xm5",
            Sku = "SONY-XM5",
            CategoryId = catMap["tai-nghe-am-thanh"].Id,
            CategoryName = "Tai nghe & Âm thanh",
            BrandId = brandMap["sony"].Id,
            BrandName = "Sony",
            ThumbnailUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=600&auto=format&fit=crop&q=80",
            OriginalPrice = 8990000m,
            Price = 6990000m,
            WarrantyMonths = 12,
            HasVariants = true,
            StockQuantity = 22,
            AvailableQuantity = 22,
            RatingAverage = 4.8,
            RatingCount = 89,
            IsFeatured = true,
            ShortDescription = "Công nghệ chống ồn số 1 thị trường với 8 micro và 2 bộ xử lý chuyên biệt, pin 30 giờ nghe nhạc.",
            Description = "Sony WH-1000XM5 viết lại các tiêu chuẩn về nghe nhạc không tiếng ồn. Hai bộ xử lý điều khiển 8 micro tối ưu hóa chống ồn tự động theo vị trí và áp suất không khí. Trình điều khiển 30mm thiết kế đặc biệt cho âm thanh độ phân giải cao Hi-Res Audio LDAC.",
            HighlightSpecs = new List<string> { "Chống ồn kép Auto NC", "Thời lượng pin 30 giờ", "Sạc nhanh 3 phút được 3 giờ", "Âm thanh Hi-Res LDAC" },
            Variants = new List<ProductVariant>
            {
                new() { Sku = "SONY-XM5-BLK", VariantName = "Màu Đen", Color = "Đen", ColorHex = "#1A1A1A", OriginalPrice = 8990000m, Price = 6990000m, StockQuantity = 12, AvailableQuantity = 12, ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=600&auto=format&fit=crop&q=80" },
                new() { Sku = "SONY-XM5-SLV", VariantName = "Màu Bạc (Silver)", Color = "Bạc", ColorHex = "#C0C0C0", OriginalPrice = 8990000m, Price = 6990000m, StockQuantity = 10, AvailableQuantity = 10, ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=600&auto=format&fit=crop&q=80" }
            }
        };
        products.Add(p6);

        // 7. Apple AirPods Pro 2 USB-C
        var p7 = new Product
        {
            Name = "Tai nghe Apple AirPods Pro 2 (USB-C)",
            Slug = "apple-airpods-pro-2-usb-c",
            Sku = "AIRPODSPRO2",
            CategoryId = catMap["tai-nghe-am-thanh"].Id,
            CategoryName = "Tai nghe & Âm thanh",
            BrandId = brandMap["apple"].Id,
            BrandName = "Apple",
            ThumbnailUrl = "https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?w=600&auto=format&fit=crop&q=80",
            OriginalPrice = 6190000m,
            Price = 5390000m,
            WarrantyMonths = 12,
            HasVariants = false,
            StockQuantity = 30,
            AvailableQuantity = 30,
            RatingAverage = 4.9,
            RatingCount = 145,
            IsFeatured = true,
            ShortDescription = "Cổng sạc USB-C, chip H2 chống ồn gấp đôi, âm thanh thích ứng và kháng bụi nước chuẩn IP54.",
            Description = "AirPods Pro 2 mang đến trải nghiệm âm thanh sống động chưa từng có với Chip H2 của Apple. Chế độ Âm thanh thích ứng kết hợp Chống ồn chủ động và Xuyên âm theo ngữ cảnh môi trường xung quanh. Hộp sạc MagSafe tích hợp loa cảnh báo và chip U1 tìm kiếm chính xác.",
            HighlightSpecs = new List<string> { "Chip Apple H2", "Chống ồn chủ động 2X", "Cổng sạc USB-C", "Chuẩn chống nước IP54" }
        };
        products.Add(p7);

        // 8. Apple Watch Ultra 2
        var p8 = new Product
        {
            Name = "Đồng hồ Apple Watch Ultra 2 GPS + Cellular 49mm",
            Slug = "apple-watch-ultra-2",
            Sku = "AW-ULTRA-2",
            CategoryId = catMap["dong-ho-thong-minh"].Id,
            CategoryName = "Đồng hồ thông minh",
            BrandId = brandMap["apple"].Id,
            BrandName = "Apple",
            ThumbnailUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&auto=format&fit=crop&q=80",
            OriginalPrice = 21990000m,
            Price = 19790000m,
            WarrantyMonths = 12,
            HasVariants = false,
            StockQuantity = 14,
            AvailableQuantity = 14,
            RatingAverage = 4.9,
            RatingCount = 38,
            IsFeatured = true,
            ShortDescription = "Vỏ titan 49mm bền bỉ, màn hình 3000 nits siêu sáng, GPS tần số kép độ chính xác cực cao.",
            Description = "Chiếc đồng hồ thể thao và dã ngoại chuyên nghiệp nhất của Apple. Khung vỏ Titan tái chế chống ăn mòn và kính sapphire phẳng bảo vệ tối đa. Nút Hành động màu cam tùy biến chức năng, pin dùng đến 72 giờ ở chế độ tiết kiệm năng lượng.",
            HighlightSpecs = new List<string> { "Khung vỏ Titan 49mm", "Màn hình 3000 nits", "Kháng nước 100m chuẩn lặn", "GPS tần số kép L1 & L5" }
        };
        products.Add(p8);

        // 9. Củ sạc Anker Prime 67W GaN
        var p9 = new Product
        {
            Name = "Củ sạc nhanh Anker Prime 67W 3 Cổng GaN (2C1A)",
            Slug = "cu-sac-anker-prime-67w-gan",
            Sku = "ANKER-67W-GAN",
            CategoryId = catMap["phu-kien"].Id,
            CategoryName = "Phụ kiện",
            BrandId = brandMap["anker"].Id,
            BrandName = "Anker",
            ThumbnailUrl = "https://images.unsplash.com/photo-1583863788434-e58a36330cf0?w=600&auto=format&fit=crop&q=80",
            OriginalPrice = 1290000m,
            Price = 990000m,
            WarrantyMonths = 18,
            HasVariants = false,
            StockQuantity = 50,
            AvailableQuantity = 50,
            RatingAverage = 4.9,
            RatingCount = 112,
            IsFeatured = false,
            ShortDescription = "Công nghệ GaNPrime độc quyền, 2 cổng Type-C và 1 cổng USB-A sạc đồng thời cho MacBook và iPhone.",
            Description = "Củ sạc Anker Prime 67W nhỏ hơn 51% so với củ sạc 67W truyền thống. Phân bổ công suất thông minh PowerIQ 4.0 và công nghệ kiểm soát nhiệt độ ActiveShield 2.0 đo nhiệt độ 3 triệu lần mỗi ngày.",
            HighlightSpecs = new List<string> { "Công suất tối đa 67W", "3 cổng sạc: 2 Type-C, 1 Type-A", "Công nghệ GaNPrime siêu mát", "Bảo hành đổi mới 18 tháng" }
        };
        products.Add(p9);

        // 10. Chuột Logitech MX Master 3S
        var p10 = new Product
        {
            Name = "Chuột không dây Logitech MX Master 3S",
            Slug = "chuot-logitech-mx-master-3s",
            Sku = "LOGI-MX3S",
            CategoryId = catMap["phu-kien"].Id,
            CategoryName = "Phụ kiện",
            BrandId = brandMap["logitech"].Id,
            BrandName = "Logitech",
            ThumbnailUrl = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?w=600&auto=format&fit=crop&q=80",
            OriginalPrice = 2490000m,
            Price = 1990000m,
            WarrantyMonths = 12,
            HasVariants = false,
            StockQuantity = 35,
            AvailableQuantity = 35,
            RatingAverage = 4.9,
            RatingCount = 76,
            IsFeatured = false,
            ShortDescription = "Cảm biến 8000 DPI di chuyển trên mọi bề mặt kể cả mặt kính, cuộn vô cực MagSpeed và cú nhấp êm ái Quiet Click.",
            Description = "Logitech MX Master 3S là chiếc chuột công thái học cao cấp tiêu chuẩn vàng cho lập trình viên và nhà sáng tạo nội dung. Cuộn từ tính MagSpeed đạt tốc độ 1.000 dòng/giây. Hỗ trợ kết nối cùng lúc 3 thiết bị và di chuyển file qua lại với Logitech Flow.",
            HighlightSpecs = new List<string> { "Cảm biến 8000 DPI trên mặt kính", "Cuộn MagSpeed 1000 dòng/giây", "Click êm ái giảm 90% tiếng ồn", "Pin sạc dùng đến 70 ngày" }
        };
        products.Add(p10);

        await _context.Products.InsertManyAsync(products);
        _logger.LogInformation("Seeded {Count} products.", products.Count);

        // Seed Physical Inventory Items with Serial / IMEI for each product
        int serialCounter = 1000;
        foreach (var prod in products)
        {
            if (prod.HasVariants)
            {
                foreach (var variant in prod.Variants)
                {
                    for (int i = 1; i <= Math.Min(3, variant.StockQuantity); i++)
                    {
                        serialCounter++;
                        inventoryItems.Add(new InventoryItem
                        {
                            SerialNumber = $"TZ{serialCounter:D6}",
                            Imei2 = prod.CategoryId == catMap["dien-thoai"].Id ? $"86{serialCounter:D13}" : null,
                            ProductId = prod.Id,
                            ProductName = prod.Name,
                            VariantSku = variant.Sku,
                            VariantName = variant.VariantName,
                            Status = InventoryStatus.InStock,
                            WarehouseLocation = "Kệ A1-Kho Tổng",
                            CostPrice = variant.Price * 0.82m,
                            ImportBatchCode = "BATCH-2026-Q3",
                            ReceivedDate = DateTime.UtcNow.AddDays(-10)
                        });
                    }
                }
            }
            else
            {
                for (int i = 1; i <= Math.Min(3, prod.StockQuantity); i++)
                {
                    serialCounter++;
                    inventoryItems.Add(new InventoryItem
                    {
                        SerialNumber = $"TZ{serialCounter:D6}",
                        ProductId = prod.Id,
                        ProductName = prod.Name,
                        VariantSku = prod.Sku,
                        VariantName = prod.Name,
                        Status = InventoryStatus.InStock,
                        WarehouseLocation = "Kệ B2-Kho Phụ Kiện",
                        CostPrice = prod.Price * 0.78m,
                        ImportBatchCode = "BATCH-2026-Q3",
                        ReceivedDate = DateTime.UtcNow.AddDays(-10)
                    });
                }
            }
        }

        if (inventoryItems.Count > 0)
        {
            await _context.InventoryItems.InsertManyAsync(inventoryItems);
            _logger.LogInformation("Seeded {Count} physical inventory items with Serial/IMEI.", inventoryItems.Count);
        }
    }

    private async Task SeedCouponsAsync()
    {
        var existingCount = await _context.Coupons.CountDocumentsAsync(c => !c.IsDeleted);
        if (existingCount > 0) return;

        var coupons = new List<Coupon>
        {
            new()
            {
                Code = "TZWELCOME",
                Title = "Ưu đãi chào mừng thành viên mới TechZone",
                Description = "Giảm trực tiếp 100.000đ cho đơn hàng đầu tiên từ 1.000.000đ",
                Scope = CouponScope.Order,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 100000m,
                MinOrderValue = 1000000m,
                UsageLimit = 500,
                UsedCount = 12,
                UsageLimitPerUser = 1,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddMonths(3),
                IsActive = true
            },
            new()
            {
                Code = "TZTECH500",
                Title = "Voucher công nghệ cao cấp",
                Description = "Giảm ngay 500.000đ cho đơn hàng thiết bị từ 10.000.000đ",
                Scope = CouponScope.Order,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 500000m,
                MinOrderValue = 10000000m,
                UsageLimit = 200,
                UsedCount = 45,
                UsageLimitPerUser = 1,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddMonths(2),
                IsActive = true
            },
            new()
            {
                Code = "FREESHIP",
                Title = "Miễn phí giao hàng hỏa tốc",
                Description = "Miễn phí vận chuyển toàn quốc lên đến 50.000đ cho đơn hàng từ 500.000đ",
                Scope = CouponScope.Shipping,
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 50000m,
                MinOrderValue = 500000m,
                UsageLimit = 1000,
                UsedCount = 88,
                UsageLimitPerUser = 2,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow.AddMonths(6),
                IsActive = true
            },
            new()
            {
                Code = "VIPTECH10",
                Title = "Đặc quyền thành viên VIP TechZone",
                Description = "Giảm 10% tối đa 1.000.000đ cho đơn hàng từ 5.000.000đ",
                Scope = CouponScope.Order,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10m,
                MaxDiscountAmount = 1000000m,
                MinOrderValue = 5000000m,
                UsageLimit = 100,
                UsedCount = 15,
                UsageLimitPerUser = 1,
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsActive = true
            }
        };

        await _context.Coupons.InsertManyAsync(coupons);
        _logger.LogInformation("Seeded {Count} promotional vouchers.", coupons.Count);
    }

    private async Task SeedBannersAsync()
    {
        var existingCount = await _context.Banners.CountDocumentsAsync(b => !b.IsDeleted);
        if (existingCount > 0) return;

        var banners = new List<Banner>
        {
            new()
            {
                Title = "Siêu Lễ Hội Công Nghệ TechZone - Giảm Đến 30%",
                ImageUrl = "https://images.unsplash.com/photo-1550009158-9ebf69173e03?w=1200&auto=format&fit=crop&q=80",
                MobileImageUrl = "https://images.unsplash.com/photo-1550009158-9ebf69173e03?w=600&auto=format&fit=crop&q=80",
                LinkUrl = "/catalog",
                Position = BannerPosition.HomeMainSlider,
                DisplayOrder = 1,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddMonths(1)
            },
            new()
            {
                Title = "iPhone 15 & 16 Series Chính Hãng VN/A",
                ImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=1200&auto=format&fit=crop&q=80",
                MobileImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?w=600&auto=format&fit=crop&q=80",
                LinkUrl = "/products/iphone-15-pro-max",
                Position = BannerPosition.HomeMainSlider,
                DisplayOrder = 2,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddMonths(1)
            },
            new()
            {
                Title = "Laptop AI & Gaming Thế Hệ Mới - Trả Góp 0%",
                ImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=1200&auto=format&fit=crop&q=80",
                MobileImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=600&auto=format&fit=crop&q=80",
                LinkUrl = "/products/asus-rog-zephyrus-g16-2024",
                Position = BannerPosition.HomeMainSlider,
                DisplayOrder = 3,
                IsActive = true,
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddMonths(1)
            }
        };

        await _context.Banners.InsertManyAsync(banners);
        _logger.LogInformation("Seeded {Count} homepage banners.", banners.Count);
    }

    private async Task SeedSuppliersAndStockSlipsAsync()
    {
        var existingSuppliers = await _context.Suppliers.CountDocumentsAsync(s => !s.IsDeleted);
        if (existingSuppliers > 0) return;

        var suppliers = new List<Supplier>
        {
            new()
            {
                Name = "Công ty TNHH Apple Việt Nam",
                Code = "NCC-APPLE",
                ContactPerson = "Trần Minh Quân",
                Phone = "02838271000",
                Email = "b2b-vn@apple.com",
                Address = "Tầng 33, Tòa nhà Deutsches Haus, 33 Lê Duẩn, P. Bến Nghé, Quận 1, TP.HCM",
                TaxCode = "0313504825",
                PaymentTerms = "Net 30 ngày (Bảo lãnh ngân hàng)",
                TotalDebt = 450000000,
                TotalImportAmount = 2500000000,
                IsActive = true,
                Note = "Đối tác phân phối trực tiếp thiết bị Apple chính hãng VN/A"
            },
            new()
            {
                Name = "Công ty CP Dịch vụ Phân phối FPT Synnex",
                Code = "NCC-SYNNEX",
                ContactPerson = "Lê Văn Hùng",
                Phone = "02873006666",
                Email = "distribution@synnexfpt.com.vn",
                Address = "Tòa nhà FPT Tân Thuận, Lô L.29B-31B-33B, KCX Tân Thuận, Quận 7, TP.HCM",
                TaxCode = "0102665089",
                PaymentTerms = "Net 45 ngày",
                TotalDebt = 320000000,
                TotalImportAmount = 1800000000,
                IsActive = true,
                Note = "Nhà phân phối ủy quyền thiết bị Asus, Dell, Xiaomi, linh phụ kiện"
            },
            new()
            {
                Name = "Công ty CP Thế Giới Số (Digiworld - DGW)",
                Code = "NCC-DGW",
                ContactPerson = "Nguyễn Thị Mai",
                Phone = "02839290059",
                Email = "support@digiworld.com.vn",
                Address = "Tòa nhà Nam Á, 201-203 Cách Mạng Tháng 8, Phường 4, Quận 3, TP.HCM",
                TaxCode = "0302861742",
                PaymentTerms = "Net 30 ngày (Chiết khấu thanh toán 1.5%)",
                TotalDebt = 180000000,
                TotalImportAmount = 1200000000,
                IsActive = true,
                Note = "Nhà phân phối chính thức Xiaomi, Apple, HP, Acer tại Việt Nam"
            },
            new()
            {
                Name = "Công ty TNHH Điện Tử Samsung Vina",
                Code = "NCC-SAMS",
                ContactPerson = "Park Min Ho",
                Phone = "02838219000",
                Email = "b2b.vn@samsung.com",
                Address = "Số 2 Hải Triều, P. Bến Nghé, Quận 1, TP.HCM",
                TaxCode = "2300346857",
                PaymentTerms = "Net 30 ngày",
                TotalDebt = 290000000,
                TotalImportAmount = 1600000000,
                IsActive = true,
                Note = "Đối tác cung cấp Galaxy Series, Màn hình & Phụ kiện Samsung"
            }
        };

        await _context.Suppliers.InsertManyAsync(suppliers);
        _logger.LogInformation("Seeded {Count} verified electronic hardware suppliers.", suppliers.Count);

        // Seed demo stock slips (1 GRN, 1 GDN)
        var sampleProducts = await _context.Products.Find(p => p.IsActive).Limit(2).ToListAsync();
        if (sampleProducts.Count > 0)
        {
            var p1 = sampleProducts[0];
            var stockSlips = new List<StockSlip>
            {
                new()
                {
                    SlipCode = "GRN-261023-01",
                    Type = StockSlipType.Import,
                    SupplierId = suppliers[0].Id,
                    SupplierName = suppliers[0].Name,
                    WarehouseLocation = "Kệ A1-Kho Tổng",
                    CreatedByName = "Thủ kho Nguyễn Văn Phát",
                    ApprovedByName = "Quản trị viên SuperAdmin",
                    ApprovedAt = DateTime.UtcNow.AddDays(-2),
                    Status = StockSlipStatus.Approved,
                    TotalQuantity = 10,
                    TotalAmount = p1.Price * 0.8m * 10,
                    Note = "Phiếu nhập kho lô hàng chính hãng mở bán tuần lễ công nghệ",
                    Items = new List<StockSlipItem>
                    {
                        new()
                        {
                            ProductId = p1.Id,
                            ProductName = p1.Name,
                            Sku = p1.Sku,
                            Quantity = 10,
                            UnitPrice = p1.Price * 0.8m,
                            SubTotal = p1.Price * 0.8m * 10,
                            SerialNumbers = Enumerable.Range(1, 10).Select(i => $"IMEI2610-{i:0000}").ToList()
                        }
                    }
                },
                new()
                {
                    SlipCode = "GDN-261023-01",
                    Type = StockSlipType.Export,
                    ExportReason = StockExportReason.ReturnToSupplier,
                    SupplierId = suppliers[1].Id,
                    SupplierName = suppliers[1].Name,
                    WarehouseLocation = "Kho Lỗi Cách Ly",
                    CreatedByName = "Kỹ thuật viên TechZone",
                    ApprovedByName = "Quản trị viên SuperAdmin",
                    ApprovedAt = DateTime.UtcNow.AddDays(-1),
                    Status = StockSlipStatus.Approved,
                    TotalQuantity = 1,
                    TotalAmount = p1.Price * 0.8m,
                    Note = "Xuất trả 1 máy cấn móp góc lưng do vận chuyển từ nhà máy",
                    Items = new List<StockSlipItem>
                    {
                        new()
                        {
                            ProductId = p1.Id,
                            ProductName = p1.Name,
                            Sku = p1.Sku,
                            Quantity = 1,
                            UnitPrice = p1.Price * 0.8m,
                            SubTotal = p1.Price * 0.8m,
                            SerialNumbers = new List<string> { "DEFECTIVE-DOA-01" }
                        }
                    }
                }
            };

            await _context.StockSlips.InsertManyAsync(stockSlips);
            _logger.LogInformation("Seeded demo stock slips (GRN/GDN).");
        }
    }

    private async Task SeedShippingCarriersAsync()
    {
        if (await _context.ShippingCarrierConfigs.CountDocumentsAsync(_ => true) > 0)
        {
            return;
        }

        var carriers = new List<ShippingCarrierConfig>
        {
            new()
            {
                CarrierCode = "GHN",
                CarrierName = "Giao Hàng Nhanh (GHN)",
                LogoUrl = "/images/carriers/ghn.png",
                IsActive = true,
                IsSandbox = true,
                ApiEndpoint = "https://dev-online-gateway.ghn.vn/shiip/public-api/v2/shipping-order/create",
                ApiToken = "ghn_sandbox_token_techzone_demo_2026",
                ShopIdOrPartnerCode = "192847",
                BaseShippingFee = 30000m,
                FreeShippingThreshold = 500000m,
                EstimatedDeliveryTime = "1 - 2 ngày làm việc",
                SupportSameDayDelivery = false,
                TrackingUrlTemplate = "https://donhang.ghn.vn/?order_code={tracking_number}",
                Notes = "Đối tác logistics chiến lược - Tích hợp tạo đơn tự động & Webhook đối soát COD"
            },
            new()
            {
                CarrierCode = "GHTK",
                CarrierName = "Giao Hàng Tiết Kiệm (GHTK)",
                LogoUrl = "/images/carriers/ghtk.png",
                IsActive = true,
                IsSandbox = true,
                ApiEndpoint = "https://services.giaohangtietkiem.vn/services/shipment/order",
                ApiToken = "ghtk_sandbox_token_techzone_demo_2026",
                ShopIdOrPartnerCode = "S22.TZ.HCM",
                BaseShippingFee = 25000m,
                FreeShippingThreshold = 500000m,
                EstimatedDeliveryTime = "2 - 3 ngày làm việc",
                SupportSameDayDelivery = false,
                TrackingUrlTemplate = "https://giaohangtietkiem.vn",
                Notes = "Tối ưu chi phí giao nội thành & tỉnh lân cận, hỗ trợ hẹn giờ giao máy"
            },
            new()
            {
                CarrierCode = "VIETTELPOST",
                CarrierName = "Viettel Post",
                LogoUrl = "/images/carriers/viettelpost.png",
                IsActive = true,
                IsSandbox = true,
                ApiEndpoint = "https://partner.viettelpost.vn/v2/order/createOrder",
                ApiToken = "vtp_partner_token_techzone_demo_2026",
                ShopIdOrPartnerCode = "VTP_TECHZONE_STORE",
                BaseShippingFee = 32000m,
                FreeShippingThreshold = 700000m,
                EstimatedDeliveryTime = "2 - 4 ngày làm việc",
                SupportSameDayDelivery = false,
                TrackingUrlTemplate = "https://tracking.viettelpost.com.vn/?order_number={tracking_number}",
                Notes = "Mạng lưới bưu cục rộng khắp 63 tỉnh thành, độ tin cậy cao cho đơn vị vùng sâu vùng xa"
            },
            new()
            {
                CarrierCode = "GRABEXPRESS",
                CarrierName = "GrabExpress (Hỏa Tốc 2H)",
                LogoUrl = "/images/carriers/grabexpress.png",
                IsActive = true,
                IsSandbox = true,
                ApiEndpoint = "https://partner-api.grab.com/grabexpress/v1/deliveries",
                ApiToken = "grab_express_token_techzone_demo_2026",
                ShopIdOrPartnerCode = "GRAB_TECHZONE_HCM",
                BaseShippingFee = 45000m,
                FreeShippingThreshold = 1000000m,
                EstimatedDeliveryTime = "1 - 2 giờ (Nội thành HCM/HN)",
                SupportSameDayDelivery = true,
                TrackingUrlTemplate = "https://grab.com/express/track/{tracking_number}",
                Notes = "Dịch vụ giao ngay trong 2 giờ cho các dòng Flagship iPhone, Laptop cao cấp"
            }
        };

        await _context.ShippingCarrierConfigs.InsertManyAsync(carriers);
        _logger.LogInformation("Seeded {Count} shipping carrier integrations.", carriers.Count);
    }

    private async Task SeedWarrantyPoliciesAsync()
    {
        if (await _context.WarrantyPolicies.CountDocumentsAsync(_ => true) > 0)
        {
            return;
        }

        var policies = new List<WarrantyPolicy>
        {
            new()
            {
                PolicyCode = "POL-PHONE",
                PolicyName = "Chính sách Bảo hành Điện thoại Flagship & iPhone Chính Hãng",
                CategoryGroup = "Điện thoại",
                StandardWarrantyMonths = 12,
                DoaPeriodDays = 30,
                ScreenDeadPixelThreshold = 3,
                BatteryHealthThreshold = 80,
                IsReturnExchangeSupported = true,
                ExtendedWarrantySupported = true,
                TermsAndConditions = "Bảo hành thân máy, camera, màn hình, FaceID/vân tay chính hãng. Thẩm định qua cổng kỹ thuật Apple GSX & Samsung Smart Tool. 1 đổi 1 trong 30 ngày nếu phát hiện lỗi phần cứng nhà sản xuất.",
                ExclusionClauses = "Từ chối bảo hành đối với các trường hợp: Máy có vết cấn móp biến dạng khung sườn, rơi vỡ mặt kính, quỳ tím chuyển màu đỏ/hồng (dính nước), can thiệp mạch điện tử hoặc bị khóa iCloud/Knox.",
                IsActive = true,
                DisplayOrder = 1,
                Notes = "Tiêu chuẩn ủy quyền AASP (Apple Authorised Service Provider) & TTBH Samsung Premium"
            },
            new()
            {
                PolicyCode = "POL-LAPTOP",
                PolicyName = "Chính sách Bảo hành Laptop Doanh Nhân & Gaming Cao Cấp",
                CategoryGroup = "Laptop",
                StandardWarrantyMonths = 24,
                DoaPeriodDays = 30,
                ScreenDeadPixelThreshold = 3,
                BatteryHealthThreshold = 75,
                IsReturnExchangeSupported = true,
                ExtendedWarrantySupported = true,
                TermsAndConditions = "Bảo hành 24 tháng chính hãng cho toàn bộ CPU, GPU, Bo mạch chủ, Bàn phím và Màn hình. Miễn phí vệ sinh, thay keo tản nhiệt gốm cao cấp định kỳ trọn đời máy tại trung tâm TechZone.",
                ExclusionClauses = "Không bảo hành bàn phím bị đổ chất lỏng (nước ngọt, cà phê), bản lề cong gãy do mở quá góc thiết kế, màn hình nứt chảy mực, rách tem niêm phong ốc vít bo mạch.",
                IsActive = true,
                DisplayOrder = 2,
                Notes = "Áp dụng cho Asus ROG, Dell XPS, Lenovo Legion, HP Omen và MacBook Pro/Air"
            },
            new()
            {
                PolicyCode = "POL-PC-PARTS",
                PolicyName = "Chính sách Bảo hành Linh kiện PC & Bo Mạch Chủ (Mainboard/VGA)",
                CategoryGroup = "Linh kiện PC",
                StandardWarrantyMonths = 36,
                DoaPeriodDays = 30,
                ScreenDeadPixelThreshold = 0,
                BatteryHealthThreshold = 0,
                IsReturnExchangeSupported = true,
                ExtendedWarrantySupported = false,
                TermsAndConditions = "Bảo hành 36 tháng theo serial number đối soát trực tiếp với nhà phân phối chính thức (Synnex FPT, Viễn Sơn, Digiworld). 1 đổi 1 ngay lập tức trong 30 ngày đầu tiên nếu phát sinh lỗi.",
                ExclusionClauses = "Cong vênh hoặc gãy chân socket CPU, cháy nổ nứt IC nguồn do sét đánh/chập điện gia dụng, rỉ sét linh kiện do môi trường ẩm mốc hoặc sử dụng cho mục đích đào coin (crypto mining).",
                IsActive = true,
                DisplayOrder = 3,
                Notes = "Bảo hành chuẩn nhà phân phối Synnex FPT & Viễn Sơn ủy quyền"
            },
            new()
            {
                PolicyCode = "POL-MONITOR",
                PolicyName = "Chính sách Bảo hành Màn hình Đồ Họa & Gaming Siêu Tần Số",
                CategoryGroup = "Màn hình",
                StandardWarrantyMonths = 36,
                DoaPeriodDays = 30,
                ScreenDeadPixelThreshold = 3,
                BatteryHealthThreshold = 0,
                IsReturnExchangeSupported = true,
                ExtendedWarrantySupported = true,
                TermsAndConditions = "Bảo hành 36 tháng chính hãng. Áp dụng chính sách Zero Bright Dot (0 điểm sáng đổi mới ngay lập tức trong 1 năm đầu). Miễn phí dịch vụ cân chỉnh màu sắc chuyên nghiệp bằng Spyder X Elite.",
                ExclusionClauses = "Nứt vỡ panel màn hình do tác động cơ học ngoại lực, sọc màn hình do cấn vật cứng khi gập hoặc lau chùi bằng dung dịch tẩy rửa ăn mòn.",
                IsActive = true,
                DisplayOrder = 4,
                Notes = "Hỗ trợ Dell UltraSharp, Asus ProArt, LG UltraGear, Samsung Odyssey"
            },
            new()
            {
                PolicyCode = "POL-ACCESSORY",
                PolicyName = "Chính sách Bảo hành Phụ kiện Âm thanh & Củ Cáp Sạc Nhanh",
                CategoryGroup = "Phụ kiện",
                StandardWarrantyMonths = 12,
                DoaPeriodDays = 30,
                ScreenDeadPixelThreshold = 0,
                BatteryHealthThreshold = 0,
                IsReturnExchangeSupported = true,
                ExtendedWarrantySupported = false,
                TermsAndConditions = "Chính sách 1 đổi 1 trực tiếp ngay tại quầy thu ngân trong suốt 12 tháng nếu củ sạc, cáp sạc, sạc dự phòng không vào điện hoặc tai nghe lỗi kết nối Bluetooth.",
                ExclusionClauses = "Đứt gãy đầu cáp sạc do lực kéo giật mạnh, biến dạng chân cắm kim loại, tai nghe ngấm nước mồ hôi hoặc mất hộp sạc dock charging.",
                IsActive = true,
                DisplayOrder = 5,
                Notes = "Áp dụng Anker, Belkin, Sony, Apple, Marshall, Ugreen"
            }
        };

        await _context.WarrantyPolicies.InsertManyAsync(policies);
        _logger.LogInformation("Seeded {Count} official warranty policies.", policies.Count);
    }

    private async Task SeedRolesAsync()
    {
        var existingCount = await _context.Roles.CountDocumentsAsync(r => !r.IsDeleted);
        if (existingCount > 0) return;

        var roles = new List<Role>
        {
            new()
            {
                Name = "Quản Trị Viên Tối Cao (SuperAdmin)",
                Code = "SUPER_ADMIN",
                Description = "Nắm toàn bộ quyền hạn cao nhất trong hệ thống, cấu hình tham số bảo mật, hạ tầng thanh toán và kiểm toán.",
                BaseRole = UserRole.SuperAdmin,
                IsSystem = true,
                Department = "Ban Quản Trị",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.SuperAdmin)
            },
            new()
            {
                Name = "Nhân Viên Bán Hàng & CSKH (Sales Staff)",
                Code = "SALES_STAFF",
                Description = "Quản lý đơn hàng, tạo đơn thủ công hotline/fanpage, hỗ trợ địa chỉ giao hàng và mã ưu đãi khuyến mãi.",
                BaseRole = UserRole.SalesStaff,
                IsSystem = true,
                Department = "Kinh Doanh",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.SalesStaff)
            },
            new()
            {
                Name = "Thủ Kho & Quản Lý Thiết Bị (Warehouse Staff)",
                Code = "WAREHOUSE_STAFF",
                Description = "Kiểm soát số lượng tồn kho vật lý, tạo phiếu nhập/xuất kho (GRN/GDN), quản lý mã IMEI/Serial từng máy.",
                BaseRole = UserRole.WarehouseStaff,
                IsSystem = true,
                Department = "Kho Vận",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.WarehouseStaff)
            },
            new()
            {
                Name = "Kỹ Thuật Viên Bảo Hành & Sửa Chữa (Technician)",
                Code = "TECHNICIAN",
                Description = "Tiếp nhận phiếu RMA sửa chữa, thẩm định tình trạng máy theo IMEI, thay thế linh kiện và cấu hình chính sách bảo hành.",
                BaseRole = UserRole.Technician,
                IsSystem = true,
                Department = "Kỹ Thuật",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.Technician)
            },
            new()
            {
                Name = "Kế Toán & Quản Trị Tài Chính (Accountant)",
                Code = "ACCOUNTANT",
                Description = "Kiểm soát công nợ nhà cung cấp, phê duyệt lệnh hoàn tiền đơn đổi trả, xem báo cáo doanh thu & lãi gộp.",
                BaseRole = UserRole.Accountant,
                IsSystem = true,
                Department = "Kế Toán",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.Accountant)
            },
            new()
            {
                Name = "Chuyên Viên Tiếp Thị & SEO (Marketing Staff)",
                Code = "MARKETING_STAFF",
                Description = "Thiết lập mã giảm giá voucher, chiến dịch Flash Sale giờ vàng, banner trang chủ và thẻ meta SEO landing page.",
                BaseRole = UserRole.MarketingStaff,
                IsSystem = true,
                Department = "Marketing",
                Permissions = AppPermissions.GetDefaultPermissionsForRole(UserRole.MarketingStaff)
            },
            new()
            {
                Name = "Trưởng Nhóm Telesales & CSKH Cao Cấp",
                Code = "CS_LEAD",
                Description = "Role tùy biến mở rộng: Giám sát toàn bộ đơn hàng, phê duyệt chiết khấu đặc biệt cho khách hàng VIP.",
                BaseRole = UserRole.SalesStaff,
                IsSystem = false,
                Department = "Kinh Doanh",
                Permissions = new List<string>
                {
                    AppPermissions.CategoryView, AppPermissions.ProductView, AppPermissions.ReviewManage,
                    AppPermissions.OrderViewAll, AppPermissions.OrderCreateManual, AppPermissions.OrderUpdateStatus, AppPermissions.OrderCancel,
                    AppPermissions.CustomerView, AppPermissions.CustomerUpdate, AppPermissions.CustomerAddressManage,
                    AppPermissions.CouponManage, AppPermissions.BannerSeoManage, AppPermissions.WarrantyLookup
                }
            }
        };

        await _context.Roles.InsertManyAsync(roles);
        _logger.LogInformation("Seeded {Count} system and company roles.", roles.Count);
    }

    private async Task SeedSparePartsAsync()
    {
        var existingCount = await _context.SpareParts.CountDocumentsAsync(p => !p.IsDeleted);
        if (existingCount > 0) return;

        var parts = new List<SparePart>
        {
            new()
            {
                PartCode = "SP-SCR-15PM-01",
                PartName = "Màn hình Super Retina XDR OLED 120Hz (iPhone 15 Pro Max)",
                Category = "Màn hình",
                CompatibleModels = new List<string> { "iPhone 15 Pro Max", "A3106" },
                WarehouseLocation = "Tủ Kỹ Thuật T1-Kệ B",
                StockQuantity = 8,
                DefectiveQuantity = 2,
                MinStockAlert = 3,
                CostPrice = 3200000m,
                ServicePrice = 4800000m,
                WarrantyMonths = 6,
                SupplierName = "Apple Authorized Service Provider (Thakral One)"
            },
            new()
            {
                PartCode = "SP-BAT-15P-01",
                PartName = "Pin Zin Li-Ion Apple Care+ 4422mAh (iPhone 15 Pro Max / 15 Plus)",
                Category = "Pin",
                CompatibleModels = new List<string> { "iPhone 15 Pro Max", "iPhone 15 Plus" },
                WarehouseLocation = "Tủ Kỹ Thuật T1-Kệ C",
                StockQuantity = 15,
                DefectiveQuantity = 4,
                MinStockAlert = 5,
                CostPrice = 650000m,
                ServicePrice = 1250000m,
                WarrantyMonths = 12,
                SupplierName = "Apple AASP"
            },
            new()
            {
                PartCode = "SP-MB-M3AIR-01",
                PartName = "Bo Mạch Chủ Logic Board Apple M3 16GB / 512GB (MacBook Air 13 M3)",
                Category = "Bo mạch chủ",
                CompatibleModels = new List<string> { "MacBook Air 13 M3 (2024)", "A3113" },
                WarehouseLocation = "Tủ Kỹ Thuật T2-Kệ A",
                StockQuantity = 3,
                DefectiveQuantity = 1,
                MinStockAlert = 2,
                CostPrice = 7800000m,
                ServicePrice = 11500000m,
                WarrantyMonths = 12,
                SupplierName = "Apple AASP"
            },
            new()
            {
                PartCode = "SP-SCR-S24U-01",
                PartName = "Màn hình Dynamic AMOLED 2X 120Hz QHD+ (Samsung Galaxy S24 Ultra)",
                Category = "Màn hình",
                CompatibleModels = new List<string> { "Galaxy S24 Ultra", "SM-S928B" },
                WarehouseLocation = "Tủ Kỹ Thuật T1-Kệ B",
                StockQuantity = 6,
                DefectiveQuantity = 1,
                MinStockAlert = 2,
                CostPrice = 3500000m,
                ServicePrice = 5200000m,
                WarrantyMonths = 6,
                SupplierName = "Samsung Electronics Vietnam"
            },
            new()
            {
                PartCode = "SP-FAN-ROG-01",
                PartName = "Cụm Quạt Tản Nhiệt Kép Arc Flow ROG Strix (Asus ROG Strix G16 G614)",
                Category = "Quạt & Tản nhiệt",
                CompatibleModels = new List<string> { "ROG Strix G16", "ROG Strix SCAR 16" },
                WarehouseLocation = "Tủ Kỹ Thuật T3-Kệ D",
                StockQuantity = 12,
                DefectiveQuantity = 0,
                MinStockAlert = 3,
                CostPrice = 450000m,
                ServicePrice = 850000m,
                WarrantyMonths = 6,
                SupplierName = "ASUS Service Center"
            },
            new()
            {
                PartCode = "SP-KB-THINKPAD-01",
                PartName = "Bàn phím Chiclet chống tràn có TrackPoint LED (ThinkPad X1 Carbon Gen 11)",
                Category = "Bàn phím",
                CompatibleModels = new List<string> { "ThinkPad X1 Carbon Gen 11", "Gen 12" },
                WarehouseLocation = "Tủ Kỹ Thuật T2-Kệ C",
                StockQuantity = 5,
                DefectiveQuantity = 1,
                MinStockAlert = 2,
                CostPrice = 600000m,
                ServicePrice = 1100000m,
                WarrantyMonths = 6,
                SupplierName = "Lenovo Vietnam"
            },
            new()
            {
                PartCode = "SP-PORT-TYPEC-01",
                PartName = "Cụm Cáp Sạc Nhanh Đa Năng USB-C Power Delivery 100W",
                Category = "Cụm sạc",
                CompatibleModels = new List<string> { "MacBook Pro", "Dell XPS", "ThinkPad" },
                WarehouseLocation = "Tủ Kỹ Thuật T1-Kệ D",
                StockQuantity = 20,
                DefectiveQuantity = 3,
                MinStockAlert = 5,
                CostPrice = 180000m,
                ServicePrice = 450000m,
                WarrantyMonths = 6,
                SupplierName = "Synnex FPT"
            },
            new()
            {
                PartCode = "SP-CAM-15PM-01",
                PartName = "Cụm Camera Tiềm Vọng Telephoto 5X 48MP OIS (iPhone 15 Pro Max)",
                Category = "Camera",
                CompatibleModels = new List<string> { "iPhone 15 Pro Max" },
                WarehouseLocation = "Tủ Kỹ Thuật T1-Kệ A",
                StockQuantity = 4,
                DefectiveQuantity = 0,
                MinStockAlert = 2,
                CostPrice = 1800000m,
                ServicePrice = 2900000m,
                WarrantyMonths = 6,
                SupplierName = "Apple AASP"
            }
        };

        await _context.SpareParts.InsertManyAsync(parts);
        _logger.LogInformation("Seeded {Count} technician spare parts.", parts.Count);
    }

    private async Task SeedWarehouseDataAsync()
    {
        // 1. Seed Purchase Orders
        var existingPo = await _context.PurchaseOrders.CountDocumentsAsync(p => !p.IsDeleted);
        if (existingPo == 0)
        {
            var pos = new List<PurchaseOrder>
            {
                new()
                {
                    PoCode = "PO-202610-001",
                    SupplierName = "Synnex FPT Distribution",
                    TargetWarehouse = "Tổng Kho Tân Bình - TP.HCM",
                    OrderDate = DateTime.UtcNow.AddDays(-3),
                    ExpectedDeliveryDate = DateTime.UtcNow.AddDays(2),
                    ApprovedBy = "Ban Giám Đốc (Nguyễn Văn Phát)",
                    Status = "Approved",
                    TotalQuantity = 15,
                    TotalAmount = 375000000m,
                    Notes = "Lô hàng điện thoại & laptop nhập khẩu chính hãng đợt 1 tháng 10",
                    Items = new List<PurchaseOrderItem>
                    {
                        new()
                        {
                            ProductName = "iPhone 15 Pro Max 256GB Titan Tự Nhiên",
                            Sku = "IP15PM-256-NAT",
                            OrderedQuantity = 10,
                            ReceivedQuantity = 0,
                            UnitCost = 25000000m
                        },
                        new()
                        {
                            ProductName = "MacBook Air 13 M3 8GB/256GB Silver",
                            Sku = "MBA-M3-8-256-SLV",
                            OrderedQuantity = 5,
                            ReceivedQuantity = 0,
                            UnitCost = 25000000m
                        }
                    }
                },
                new()
                {
                    PoCode = "PO-202610-002",
                    SupplierName = "Digiworld (DGW) Corporation",
                    TargetWarehouse = "Tổng Kho Tân Bình - TP.HCM",
                    OrderDate = DateTime.UtcNow.AddDays(-5),
                    ExpectedDeliveryDate = DateTime.UtcNow.AddDays(1),
                    ApprovedBy = "Ban Giám Đốc (Nguyễn Văn Phát)",
                    Status = "Approved",
                    TotalQuantity = 12,
                    TotalAmount = 360000000m,
                    Notes = "Lô laptop gaming ROG và màn hình OLED phục vụ mùa tựu trường",
                    Items = new List<PurchaseOrderItem>
                    {
                        new()
                        {
                            ProductName = "Laptop Gaming Asus ROG Strix G16 G614JVR",
                            Sku = "ROG-G16-14900HX",
                            OrderedQuantity = 12,
                            ReceivedQuantity = 0,
                            UnitCost = 30000000m
                        }
                    }
                }
            };
            await _context.PurchaseOrders.InsertManyAsync(pos);
            _logger.LogInformation("Seeded {Count} purchase orders.", pos.Count);
        }

        // 2. Seed Stock Transfers
        var existingTransfer = await _context.StockTransfers.CountDocumentsAsync(t => !t.IsDeleted);
        if (existingTransfer == 0)
        {
            var transfers = new List<StockTransfer>
            {
                new()
                {
                    TransferCode = "TRF-202610-001",
                    FromWarehouse = "Tổng Kho Tân Bình - TP.HCM",
                    ToWarehouse = "Chi Nhánh Showroom Quận 1",
                    CreatedByStaff = "Thủ Kho Tân Bình",
                    CarrierName = "Đội Giao Hàng Nội Bộ TechZone",
                    TrackingNumber = "NB-HCM-8899",
                    TotalItems = 5,
                    Status = "InTransit",
                    SentDate = DateTime.UtcNow.AddHours(-4),
                    Notes = "Điều chuyển 5 máy phục vụ khách trải nghiệm và giao hỏa tốc tại Showroom",
                    Items = new List<StockTransferItem>
                    {
                        new()
                        {
                            ProductName = "iPhone 15 Pro Max 256GB Titan Tự Nhiên",
                            Sku = "IP15PM-256-NAT",
                            Quantity = 5,
                            SerialNumbers = new List<string> { "F17N12345678", "F17N12345679", "F17N12345680", "F17N12345681", "F17N12345682" }
                        }
                    }
                }
            };
            await _context.StockTransfers.InsertManyAsync(transfers);
            _logger.LogInformation("Seeded {Count} stock transfers.", transfers.Count);
        }

        // 3. Seed Stocktake Session
        var existingStocktake = await _context.StocktakeSessions.CountDocumentsAsync(s => !s.IsDeleted);
        if (existingStocktake == 0)
        {
            var stocktakes = new List<StocktakeSession>
            {
                new()
                {
                    SessionCode = "ST-202610-001",
                    Title = "Kiểm Kê Định Kỳ Toàn Bộ Kho Tổng Tháng 10/2026",
                    WarehouseLocation = "Tổng Kho Tân Bình - TP.HCM",
                    ConductedByStaff = "Thủ Kho (Trần Văn Kho)",
                    ConductedDate = DateTime.UtcNow.AddDays(-2),
                    TotalSystemItems = 120,
                    TotalCountedItems = 120,
                    DiscrepancyCount = 0,
                    Status = "Completed",
                    AuditNotes = "Toàn bộ số lượng đếm thực tế khớp 100% với dữ liệu phần mềm. Không phát sinh chênh lệch.",
                    Items = new List<StocktakeDiscrepancyItem>()
                }
            };
            await _context.StocktakeSessions.InsertManyAsync(stocktakes);
            _logger.LogInformation("Seeded {Count} stocktake sessions.", stocktakes.Count);
        }
    }
}
