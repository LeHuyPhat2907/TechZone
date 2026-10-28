using MongoDB.Driver;
using TechZone.Domain.Entities;

namespace TechZone.Infrastructure.Data;

public interface IMongoDbContext
{
    IMongoClient Client { get; }
    IMongoDatabase Database { get; }
    IMongoCollection<T> GetCollection<T>(string? name = null);

    IMongoCollection<User> Users { get; }
    IMongoCollection<Category> Categories { get; }
    IMongoCollection<Brand> Brands { get; }
    IMongoCollection<Product> Products { get; }
    IMongoCollection<InventoryItem> InventoryItems { get; }
    IMongoCollection<InventoryTransaction> InventoryTransactions { get; }
    IMongoCollection<Supplier> Suppliers { get; }
    IMongoCollection<StockSlip> StockSlips { get; }
    IMongoCollection<Cart> Carts { get; }
    IMongoCollection<Order> Orders { get; }
    IMongoCollection<ShippingCarrierConfig> ShippingCarrierConfigs { get; }
    IMongoCollection<Payment> Payments { get; }
    IMongoCollection<Review> Reviews { get; }
    IMongoCollection<Coupon> Coupons { get; }
    IMongoCollection<CouponUsage> CouponUsages { get; }
    IMongoCollection<Banner> Banners { get; }
    IMongoCollection<Post> Posts { get; }
    IMongoCollection<PostCategory> PostCategories { get; }
    IMongoCollection<Warranty> Warranties { get; }
    IMongoCollection<WarrantyPolicy> WarrantyPolicies { get; }
    IMongoCollection<RmaRequest> RmaRequests { get; }
    IMongoCollection<Notification> Notifications { get; }
    IMongoCollection<SecurityAuditLog> SecurityAuditLogs { get; }
    IMongoCollection<Role> Roles { get; }
    IMongoCollection<SparePart> SpareParts { get; }
    IMongoCollection<SparePartRequest> SparePartRequests { get; }
    IMongoCollection<VendorRma> VendorRmas { get; }
    IMongoCollection<PurchaseOrder> PurchaseOrders { get; }
    IMongoCollection<StocktakeSession> StocktakeSessions { get; }
    IMongoCollection<StockTransfer> StockTransfers { get; }

    Task CreateIndexesAsync();
}
