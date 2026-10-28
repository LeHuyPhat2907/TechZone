using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using TechZone.Domain.Entities;
using TechZone.Infrastructure.Configuration;

namespace TechZone.Infrastructure.Data;

public class MongoDbContext : IMongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly IMongoClient _client;
    private readonly ILogger<MongoDbContext> _logger;

    static MongoDbContext()
    {
        try
        {
            var conventionPack = new ConventionPack
            {
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("TechZoneConventions", conventionPack, _ => true);
        }
        catch (ArgumentException)
        {
            // Convention already registered
        }
    }

    public MongoDbContext(IOptions<MongoDbSettings> settings, ILogger<MongoDbContext> logger)
    {
        _logger = logger;
        var mongoSettings = MongoClientSettings.FromConnectionString(settings.Value.ConnectionString);
        _client = new MongoClient(mongoSettings);
        _database = _client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoClient Client => _client;
    public IMongoDatabase Database => _database;

    public IMongoCollection<T> GetCollection<T>(string? name = null)
    {
        var collectionName = name ?? GetDefaultCollectionName(typeof(T));
        return _database.GetCollection<T>(collectionName);
    }

    public IMongoCollection<User> Users => GetCollection<User>("users");
    public IMongoCollection<Category> Categories => GetCollection<Category>("categories");
    public IMongoCollection<Brand> Brands => GetCollection<Brand>("brands");
    public IMongoCollection<Product> Products => GetCollection<Product>("products");
    public IMongoCollection<InventoryItem> InventoryItems => GetCollection<InventoryItem>("inventory_items");
    public IMongoCollection<InventoryTransaction> InventoryTransactions => GetCollection<InventoryTransaction>("inventory_transactions");
    public IMongoCollection<Supplier> Suppliers => GetCollection<Supplier>("suppliers");
    public IMongoCollection<StockSlip> StockSlips => GetCollection<StockSlip>("stock_slips");
    public IMongoCollection<Cart> Carts => GetCollection<Cart>("carts");
    public IMongoCollection<Order> Orders => GetCollection<Order>("orders");
    public IMongoCollection<ShippingCarrierConfig> ShippingCarrierConfigs => GetCollection<ShippingCarrierConfig>("shipping_carrier_configs");
    public IMongoCollection<Payment> Payments => GetCollection<Payment>("payments");
    public IMongoCollection<Review> Reviews => GetCollection<Review>("reviews");
    public IMongoCollection<Coupon> Coupons => GetCollection<Coupon>("coupons");
    public IMongoCollection<CouponUsage> CouponUsages => GetCollection<CouponUsage>("coupon_usages");
    public IMongoCollection<Banner> Banners => GetCollection<Banner>("banners");
    public IMongoCollection<Post> Posts => GetCollection<Post>("posts");
    public IMongoCollection<PostCategory> PostCategories => GetCollection<PostCategory>("post_categories");
    public IMongoCollection<Warranty> Warranties => GetCollection<Warranty>("warranties");
    public IMongoCollection<WarrantyPolicy> WarrantyPolicies => GetCollection<WarrantyPolicy>("warranty_policies");
    public IMongoCollection<RmaRequest> RmaRequests => GetCollection<RmaRequest>("rma_requests");
    public IMongoCollection<Notification> Notifications => GetCollection<Notification>("notifications");
    public IMongoCollection<SecurityAuditLog> SecurityAuditLogs => GetCollection<SecurityAuditLog>("security_audit_logs");
    public IMongoCollection<Role> Roles => GetCollection<Role>("roles");
    public IMongoCollection<SparePart> SpareParts => GetCollection<SparePart>("spare_parts");
    public IMongoCollection<SparePartRequest> SparePartRequests => GetCollection<SparePartRequest>("spare_part_requests");
    public IMongoCollection<VendorRma> VendorRmas => GetCollection<VendorRma>("vendor_rmas");
    public IMongoCollection<PurchaseOrder> PurchaseOrders => GetCollection<PurchaseOrder>("purchase_orders");
    public IMongoCollection<StocktakeSession> StocktakeSessions => GetCollection<StocktakeSession>("stocktake_sessions");
    public IMongoCollection<StockTransfer> StockTransfers => GetCollection<StockTransfer>("stock_transfers");

    public async Task CreateIndexesAsync()
    {
        try
        {
            _logger.LogInformation("Ensuring MongoDB indexes are created...");

            // 1. Users Indexes
            var userEmailIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_users_email" });
            var userRoleIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Role),
                new CreateIndexOptions { Name = "ix_users_role" });
            await Users.Indexes.CreateManyAsync(new[] { userEmailIndex, userRoleIndex });

            // 2. Categories Indexes
            var categorySlugIndex = new CreateIndexModel<Category>(
                Builders<Category>.IndexKeys.Ascending(c => c.Slug),
                new CreateIndexOptions { Unique = true, Name = "ux_categories_slug" });
            var categoryParentIndex = new CreateIndexModel<Category>(
                Builders<Category>.IndexKeys.Ascending(c => c.ParentId),
                new CreateIndexOptions { Name = "ix_categories_parentId" });
            await Categories.Indexes.CreateManyAsync(new[] { categorySlugIndex, categoryParentIndex });

            // 3. Brands Indexes
            var brandSlugIndex = new CreateIndexModel<Brand>(
                Builders<Brand>.IndexKeys.Ascending(b => b.Slug),
                new CreateIndexOptions { Unique = true, Name = "ux_brands_slug" });
            await Brands.Indexes.CreateOneAsync(brandSlugIndex);

            // 4. Products Indexes
            var productSlugIndex = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.Slug),
                new CreateIndexOptions { Unique = true, Name = "ux_products_slug" });
            var productSkuIndex = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.Sku),
                new CreateIndexOptions { Unique = true, Name = "ux_products_sku" });
            var productCategoryIndex = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.CategoryId).Ascending(p => p.IsActive),
                new CreateIndexOptions { Name = "ix_products_category_active" });
            var productBrandIndex = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Ascending(p => p.BrandId).Ascending(p => p.IsActive),
                new CreateIndexOptions { Name = "ix_products_brand_active" });
            var productSearchIndex = new CreateIndexModel<Product>(
                Builders<Product>.IndexKeys.Text(p => p.Name).Text(p => p.BrandName).Text(p => p.CategoryName),
                new CreateIndexOptions { Name = "tx_products_search" });
            await Products.Indexes.CreateManyAsync(new[] { productSlugIndex, productSkuIndex, productCategoryIndex, productBrandIndex, productSearchIndex });

            // 5. InventoryItems Indexes
            var invSerialIndex = new CreateIndexModel<InventoryItem>(
                Builders<InventoryItem>.IndexKeys.Ascending(i => i.SerialNumber),
                new CreateIndexOptions { Unique = true, Name = "ux_inventory_serial" });
            var invProductSkuIndex = new CreateIndexModel<InventoryItem>(
                Builders<InventoryItem>.IndexKeys.Ascending(i => i.ProductId).Ascending(i => i.VariantSku).Ascending(i => i.Status),
                new CreateIndexOptions { Name = "ix_inventory_product_variant_status" });
            await InventoryItems.Indexes.CreateManyAsync(new[] { invSerialIndex, invProductSkuIndex });

            // 6. Orders Indexes
            var orderCodeIndex = new CreateIndexModel<Order>(
                Builders<Order>.IndexKeys.Ascending(o => o.OrderCode),
                new CreateIndexOptions { Unique = true, Name = "ux_orders_orderCode" });
            var orderUserIndex = new CreateIndexModel<Order>(
                Builders<Order>.IndexKeys.Ascending(o => o.UserId).Descending(o => o.CreatedAt),
                new CreateIndexOptions { Name = "ix_orders_userId_createdAt" });
            var orderStatusIndex = new CreateIndexModel<Order>(
                Builders<Order>.IndexKeys.Ascending(o => o.OrderStatus),
                new CreateIndexOptions { Name = "ix_orders_status" });
            await Orders.Indexes.CreateManyAsync(new[] { orderCodeIndex, orderUserIndex, orderStatusIndex });

            // 7. Coupons Indexes
            var couponCodeIndex = new CreateIndexModel<Coupon>(
                Builders<Coupon>.IndexKeys.Ascending(c => c.Code),
                new CreateIndexOptions { Unique = true, Name = "ux_coupons_code" });
            await Coupons.Indexes.CreateOneAsync(couponCodeIndex);

            // 8. Warranties Indexes
            var warrantySerialIndex = new CreateIndexModel<Warranty>(
                Builders<Warranty>.IndexKeys.Ascending(w => w.SerialNumber),
                new CreateIndexOptions { Unique = true, Name = "ux_warranties_serial" });
            var warrantyPhoneIndex = new CreateIndexModel<Warranty>(
                Builders<Warranty>.IndexKeys.Ascending(w => w.CustomerPhone),
                new CreateIndexOptions { Name = "ix_warranties_phone" });
            await Warranties.Indexes.CreateManyAsync(new[] { warrantySerialIndex, warrantyPhoneIndex });

            _logger.LogInformation("All MongoDB indexes ensured successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create some MongoDB indexes. The database may already have indexes or might be a standalone instance.");
        }
    }

    private static string GetDefaultCollectionName(Type type)
    {
        return type.Name switch
        {
            nameof(User) => "users",
            nameof(Category) => "categories",
            nameof(Brand) => "brands",
            nameof(Product) => "products",
            nameof(InventoryItem) => "inventory_items",
            nameof(InventoryTransaction) => "inventory_transactions",
            nameof(Supplier) => "suppliers",
            nameof(StockSlip) => "stock_slips",
            nameof(Cart) => "carts",
            nameof(Order) => "orders",
            nameof(ShippingCarrierConfig) => "shipping_carrier_configs",
            nameof(Payment) => "payments",
            nameof(Review) => "reviews",
            nameof(Coupon) => "coupons",
            nameof(CouponUsage) => "coupon_usages",
            nameof(Banner) => "banners",
            nameof(Post) => "posts",
            nameof(PostCategory) => "post_categories",
            nameof(Warranty) => "warranties",
            nameof(WarrantyPolicy) => "warranty_policies",
            nameof(RmaRequest) => "rma_requests",
            nameof(Notification) => "notifications",
            nameof(SecurityAuditLog) => "security_audit_logs",
            nameof(Role) => "roles",
            nameof(SparePart) => "spare_parts",
            nameof(SparePartRequest) => "spare_part_requests",
            nameof(VendorRma) => "vendor_rmas",
            nameof(PurchaseOrder) => "purchase_orders",
            nameof(StocktakeSession) => "stocktake_sessions",
            nameof(StockTransfer) => "stock_transfers",
            _ => type.Name.ToLowerInvariant() + "s"
        };
    }
}
