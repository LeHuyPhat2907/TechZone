using TechZone.Domain.Enums;

namespace TechZone.Domain.Common;

public class PermissionItem
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class PermissionCategoryGroup
{
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-shield";
    public List<PermissionItem> Permissions { get; set; } = new();
}

public static class AppPermissions
{
    // 1.1 Catalog & Products
    public const string CategoryView = "category.view";
    public const string CategoryCreate = "category.create";
    public const string CategoryUpdate = "category.update";
    public const string CategoryDelete = "category.delete";
    public const string BrandManage = "brand.manage";
    public const string AttributeManage = "attribute.manage";
    public const string ProductView = "product.view";
    public const string ProductCreate = "product.create";
    public const string ProductUpdate = "product.update";
    public const string ProductDelete = "product.delete";
    public const string ProductPriceOverride = "product.price_override";
    public const string ReviewManage = "review.manage";

    // 1.2 Inventory & Hardware
    public const string InventoryView = "inventory.view";
    public const string InventoryAdjust = "inventory.adjust";
    public const string StockImport = "stock.import";
    public const string StockExport = "stock.export";
    public const string ImeiSerialTrack = "imei_serial.track";
    public const string SupplierManage = "supplier.manage";

    // 1.3 Orders & Fulfillment
    public const string OrderViewAll = "order.view_all";
    public const string OrderCreateManual = "order.create_manual";
    public const string OrderUpdateStatus = "order.update_status";
    public const string OrderCancel = "order.cancel";
    public const string OrderForceDelete = "order.force_delete";
    public const string RefundApprove = "refund.approve";
    public const string ShippingManage = "shipping.manage";

    // 1.4 Warranty & Technical Services
    public const string WarrantyPolicyManage = "warranty.policy_manage";
    public const string WarrantyLookup = "warranty.lookup";
    public const string TicketManage = "ticket.manage";

    // 2. Technician / Support (Nhân viên Kỹ thuật & Bảo hành)
    public const string ImeiSerialLookup = "imei_serial.lookup";
    public const string WarrantyPolicyView = "warranty_policy.view";
    public const string OrderLookupHistory = "order.lookup_history";
    public const string TicketCreate = "ticket.create";
    public const string TicketViewAssigned = "ticket.view_assigned";
    public const string TicketUpdateDiagnosis = "ticket.update_diagnosis";
    public const string TicketUpdateStatus = "ticket.update_status";
    public const string TicketUploadEvidence = "ticket.upload_evidence";
    public const string TicketPrintReceipt = "ticket.print_receipt";
    public const string SparePartsViewStock = "spare_parts.view_stock";
    public const string SparePartsRequest = "spare_parts.request";
    public const string SparePartsReturnDefective = "spare_parts.return_defective";
    public const string ServiceCostEstimate = "service_cost.estimate";
    public const string ServiceCostSubmitForApproval = "service_cost.submit_for_approval";
    public const string CustomerNoteAdd = "customer_note.add";
    public const string VendorRmaCreate = "vendor_rma.create";
    public const string VendorRmaTrack = "vendor_rma.track";

    // 3. Warehouse / Inventory Manager (Thủ kho / Quản lý kho)
    // 3.1 Inbound Logistics
    public const string InboundViewPo = "inbound.view_po";
    public const string InboundCreateReceipt = "inbound.create_receipt";
    public const string InboundInspect = "inbound.inspect";
    public const string InboundScanImeiBatch = "inbound.scan_imei_batch";
    public const string InboundConfirm = "inbound.confirm";

    // 3.2 IMEI / Serial Number Tracking
    public const string SerialImeiView = "serial_imei.view";
    public const string SerialImeiBindSku = "serial_imei.bind_sku";
    public const string SerialImeiUpdateStatus = "serial_imei.update_status";
    public const string SerialImeiPrintBarcode = "serial_imei.print_barcode";

    // 3.3 Outbound & Fulfillment
    public const string OutboundViewPending = "outbound.view_pending";
    public const string OutboundCreatePickList = "outbound.create_pick_list";
    public const string OutboundScanDispatch = "outbound.scan_dispatch";
    public const string OutboundPrintShippingLabel = "outbound.print_shipping_label";
    public const string OutboundHandoverCarrier = "outbound.handover_carrier";

    // 3.4 Stocktake, Transfer & Reverse Logistics
    public const string StocktakePerform = "stocktake.perform";
    public const string StocktakeLogDiscrepancy = "stocktake.log_discrepancy";
    public const string InventoryAdjustDamaged = "inventory.adjust_damaged";
    public const string InventoryThresholdAlert = "inventory.threshold_alert";
    public const string TransferCreate = "transfer.create";
    public const string TransferReceive = "transfer.receive";
    public const string ReturnsReceive = "returns.receive";
    public const string ReturnsInspect = "returns.inspect";
    public const string ReturnsRestock = "returns.restock";

    // 4. Sales / Order Processor (Nhân viên Bán hàng & Xử lý đơn)
    // 4.1 Order Processing
    public const string OrderViewList = "order.view_list";
    public const string OrderViewDetail = "order.view_detail";
    public const string OrderAssignStaff = "order.assign_staff";
    public const string OrderPrintInvoice = "order.print_invoice";

    // 4.2 Customer Support & Editing
    public const string CustomerViewBasic = "customer.view_basic";
    public const string CustomerContactInfo = "customer.contact_info";
    public const string OrderUpdateShippingInfo = "order.update_shipping_info";
    public const string OrderItemModify = "order.item_modify";
    public const string OrderInternalNote = "order.internal_note";

    // 4.3 Catalog, Availability, Cancel & Returns
    public const string ProductViewCatalog = "product.view_catalog";
    public const string InventoryCheckAvailable = "inventory.check_available";
    public const string PromotionApplyCode = "promotion.apply_code";
    public const string RefundRequestCreate = "refund.request_create";
    public const string ReturnInitiate = "return.initiate";

    // 5. Customer / Member (Khách hàng thành viên)
    // 5.1 Profile & Security
    public const string ProfileViewOwn = "profile.view_own";
    public const string ProfileUpdateOwn = "profile.update_own";
    public const string AddressManageOwn = "address.manage_own";
    public const string SecurityChangePassword = "security.change_password";
    public const string AccountDeleteRequest = "account.delete_request";

    // 5.2 Shopping & Checkout
    public const string CartManageOwn = "cart.manage_own";
    public const string WishlistManageOwn = "wishlist.manage_own";
    public const string CouponSaveOwn = "coupon.save_own";
    public const string OrderCreateCheckout = "order.create_checkout";
    public const string OrderApplyPoints = "order.apply_points";
    public const string PaymentInitiate = "payment.initiate";

    // 5.3 Order Tracking & Warranty
    public const string OrderViewOwnList = "order.view_own_list";
    public const string OrderViewOwnDetail = "order.view_own_detail";
    public const string OrderTrackShipping = "order.track_shipping";
    public const string OrderCancelOwn = "order.cancel_own";
    public const string OrderConfirmReceived = "order.confirm_received";
    public const string WarrantyViewOwnDevices = "warranty.view_own_devices";
    public const string WarrantyCheckStatus = "warranty.check_status";
    public const string ReturnRequestCreate = "return.request_create";
    public const string ReviewCreateVerified = "review.create_verified";

    // 6. Guest (Khách vãng lai & Công cộng)
    // 6.1 Discovery & Lookup
    public const string CatalogViewPublic = "catalog.view_public";
    public const string ProductViewDetail = "product.view_detail";
    public const string SearchQuery = "search.query";
    public const string FilterApply = "filter.apply";
    public const string ProductCompareSession = "product.compare_session";
    public const string WarrantyPublicLookup = "warranty.public_lookup";
    public const string StoreLocatorView = "store_locator.view";
    public const string ReviewReadPublic = "review.read_public";

    // 6.2 Session Cart & Guest Checkout
    public const string CartCreateSession = "cart.create_session";
    public const string CartAddItem = "cart.add_item";
    public const string CartUpdateQuantity = "cart.update_quantity";
    public const string CouponSimulate = "coupon.simulate";
    public const string CheckoutGuestMode = "checkout.guest_mode";
    public const string OrderTrackByToken = "order.track_by_token";
    public const string AuthRegisterLogin = "auth.register_login";

    // 1.5 Customer & Marketing
    public const string CustomerView = "customer.view";
    public const string CustomerUpdate = "customer.update";
    public const string CustomerBan = "customer.ban";
    public const string CustomerAddressManage = "customer.address_manage";
    public const string CouponManage = "coupon.manage";
    public const string BannerSeoManage = "banner_seo.manage";

    // 1.6 IAM & Staff Administration
    public const string StaffCreate = "staff.create";
    public const string StaffUpdate = "staff.update";
    public const string StaffDelete = "staff.delete";
    public const string RoleManage = "role.manage";
    public const string PermissionAssign = "permission.assign";
    public const string SessionRevoke = "session.revoke";

    // 1.7 Analytics & Finance
    public const string ReportRevenue = "report.revenue";
    public const string ReportProfitMargin = "report.profit_margin";
    public const string ReportInventoryTurnover = "report.inventory_turnover";
    public const string ExportFinancialData = "export.financial_data";

    // 1.8 System & DevOps Config
    public const string PaymentGatewayConfig = "payment_gateway.config";
    public const string ThirdPartyApiManage = "third_party_api.manage";
    public const string AuditLogView = "audit_log.view";
    public const string SystemMaintenance = "system.maintenance";

    public static readonly List<PermissionCategoryGroup> AllGroups = new()
    {
        new PermissionCategoryGroup
        {
            Category = "1.1 Quản lý Sản phẩm & Danh mục (Catalog)",
            Icon = "bi-diagram-3-fill",
            Permissions = new List<PermissionItem>
            {
                new() { Code = CategoryView, Name = "Xem danh mục", Description = "Xem cây danh mục thiết bị và linh kiện", Category = "1.1" },
                new() { Code = CategoryCreate, Name = "Tạo danh mục", Description = "Tạo danh mục sản phẩm mới", Category = "1.1" },
                new() { Code = CategoryUpdate, Name = "Sửa danh mục", Description = "Chỉnh sửa thông tin danh mục", Category = "1.1" },
                new() { Code = CategoryDelete, Name = "Xóa danh mục", Description = "Xóa danh mục khỏi hệ thống", Category = "1.1" },
                new() { Code = BrandManage, Name = "Quản lý thương hiệu", Description = "Quản lý đối tác hãng Apple, Asus, Samsung...", Category = "1.1" },
                new() { Code = AttributeManage, Name = "Thông số kỹ thuật", Description = "Quản lý bộ thuộc tính đặc thù CPU, RAM, Pin...", Category = "1.1" },
                new() { Code = ProductView, Name = "Xem sản phẩm", Description = "Tra cứu danh sách và chi tiết thiết bị", Category = "1.1" },
                new() { Code = ProductCreate, Name = "Thêm sản phẩm", Description = "Tạo thiết bị mới, cấu hình thư viện ảnh", Category = "1.1" },
                new() { Code = ProductUpdate, Name = "Sửa sản phẩm", Description = "Chỉnh sửa thông số, bài viết mô tả", Category = "1.1" },
                new() { Code = ProductDelete, Name = "Xóa sản phẩm", Description = "Xóa sản phẩm ngừng kinh doanh", Category = "1.1" },
                new() { Code = ProductPriceOverride, Name = "Sửa giá & Bán sỉ", Description = "Quyền sửa giá bán lẻ, giá niêm yết và giá sỉ", Category = "1.1" },
                new() { Code = ReviewManage, Name = "Kiểm duyệt đánh giá", Description = "Duyệt, phản hồi hoặc xóa bình luận", Category = "1.1" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "1.2 Quản lý Kho & IMEI/Serial (Inventory)",
            Icon = "bi-upc-scan",
            Permissions = new List<PermissionItem>
            {
                new() { Code = InventoryView, Name = "Tra cứu tồn kho", Description = "Xem số lượng tồn kho vật lý và khả dụng", Category = "1.2" },
                new() { Code = InventoryAdjust, Name = "Điều chỉnh tồn kho", Description = "Cân bằng kiểm kê, điều chỉnh lệch kho", Category = "1.2" },
                new() { Code = StockImport, Name = "Phiếu nhập kho (GRN)", Description = "Tạo và duyệt phiếu nhập hàng từ nhà phân phối", Category = "1.2" },
                new() { Code = StockExport, Name = "Phiếu xuất kho (GDN)", Description = "Tạo và duyệt phiếu xuất kho điều chuyển/hủy", Category = "1.2" },
                new() { Code = ImeiSerialTrack, Name = "Vòng đời IMEI/Serial", Description = "Quản lý danh sách IMEI từng đơn vị máy", Category = "1.2" },
                new() { Code = SupplierManage, Name = "Nhà cung cấp & Công nợ", Description = "Quản lý danh bạ NCC và điều khoản công nợ", Category = "1.2" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "1.3 Quản lý Đơn hàng & Giao vận (Orders)",
            Icon = "bi-box-seam-fill",
            Permissions = new List<PermissionItem>
            {
                new() { Code = OrderViewAll, Name = "Xem toàn bộ đơn hàng", Description = "Xem đơn hàng không giới hạn phân vùng", Category = "1.3" },
                new() { Code = OrderCreateManual, Name = "Tạo đơn thủ công", Description = "Tạo đơn đặt qua điện thoại / Telesales / Fanpage", Category = "1.3" },
                new() { Code = OrderUpdateStatus, Name = "Cập nhật trạng thái đơn", Description = "Chuyển đơn từ Tiếp nhận -> Giao hàng -> Hoàn tất", Category = "1.3" },
                new() { Code = OrderCancel, Name = "Hủy đơn hàng", Description = "Hủy đơn và hoàn tồn kho khả dụng", Category = "1.3" },
                new() { Code = OrderForceDelete, Name = "Xóa đơn rác / Đơn ảo", Description = "Xóa vết đơn rác (đặc quyền SuperAdmin)", Category = "1.3" },
                new() { Code = RefundApprove, Name = "Duyệt đổi trả & Hoàn tiền", Description = "Phê duyệt bồi hoàn tiền hoặc đổi mới", Category = "1.3" },
                new() { Code = ShippingManage, Name = "Cấu hình đơn vị vận chuyển", Description = "Kết nối API GHN, GHTK, ViettelPost, GrabExpress", Category = "1.3" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "1.4 Bảo hành & Kỹ thuật (Warranty & RMA)",
            Icon = "bi-shield-check",
            Permissions = new List<PermissionItem>
            {
                new() { Code = WarrantyPolicyManage, Name = "Chính sách bảo hành", Description = "Cấu hình chính sách AASP, Zero Dot, 1 đổi 1", Category = "1.4" },
                new() { Code = WarrantyLookup, Name = "Tra cứu hạn bảo hành", Description = "Tra cứu vòng đời máy và số ngày bảo hành", Category = "1.4" },
                new() { Code = TicketManage, Name = "Phiếu RMA & Điều phối KTV", Description = "Tiếp nhận máy sửa chữa, điều phối KTV", Category = "1.4" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "1.5 Khách hàng & Marketing (CRM & Promotions)",
            Icon = "bi-megaphone-fill",
            Permissions = new List<PermissionItem>
            {
                new() { Code = CustomerView, Name = "Xem danh sách khách hàng", Description = "Xem hồ sơ CRM, hạng VIP và lịch sử mua", Category = "1.5" },
                new() { Code = CustomerUpdate, Name = "Cập nhật điểm & Hạng VIP", Description = "Điều chỉnh điểm thưởng CRM và hồ sơ khách", Category = "1.5" },
                new() { Code = CustomerBan, Name = "Khóa tài khoản vi phạm", Description = "Khóa tài khoản gian lận kèm lý do biên bản", Category = "1.5" },
                new() { Code = CustomerAddressManage, Name = "Can thiệp sổ địa chỉ", Description = "Chỉnh sửa địa chỉ nhận hàng khi có sự cố", Category = "1.5" },
                new() { Code = CouponManage, Name = "Quản lý mã giảm giá", Description = "Tạo voucher, Flash Sale giờ vàng, combo", Category = "1.5" },
                new() { Code = BannerSeoManage, Name = "Banner & Meta SEO", Description = "Tùy biến banner đa vị trí và landing page SEO", Category = "1.5" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "1.6 Quản trị Tài khoản & Phân quyền nội bộ (IAM)",
            Icon = "bi-shield-lock-fill",
            Permissions = new List<PermissionItem>
            {
                new() { Code = StaffCreate, Name = "Tạo tài khoản nhân viên", Description = "Tạo tài khoản Sales, Kho, Kỹ thuật, Kế toán", Category = "1.6" },
                new() { Code = StaffUpdate, Name = "Sửa tài khoản nhân viên", Description = "Cập nhật thông tin, đổi phòng ban, reset mật khẩu", Category = "1.6" },
                new() { Code = StaffDelete, Name = "Xóa tài khoản nhân viên", Description = "Xóa hoặc vô hiệu hóa tài khoản nhân sự", Category = "1.6" },
                new() { Code = RoleManage, Name = "Quản lý Role công ty", Description = "Tạo và định nghĩa các Role chức danh mới", Category = "1.6" },
                new() { Code = PermissionAssign, Name = "Gán quyền hạn cụ thể", Description = "Gán/thu hồi quyền cho từng Role hoặc cá nhân", Category = "1.6" },
                new() { Code = SessionRevoke, Name = "Buộc đăng xuất (Force Logout)", Description = "Thu hồi phiên đăng nhập khi phát hiện rò rỉ", Category = "1.6" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "1.7 Báo cáo & Tài chính (Analytics & Finance)",
            Icon = "bi-graph-up-arrow",
            Permissions = new List<PermissionItem>
            {
                new() { Code = ReportRevenue, Name = "Báo cáo doanh thu", Description = "Xem biểu đồ doanh số theo ngày/tháng/quý", Category = "1.7" },
                new() { Code = ReportProfitMargin, Name = "Lợi nhuận & Biên lãi gộp", Description = "Phân tích lãi gộp sau khi trừ giá vốn COGS", Category = "1.7" },
                new() { Code = ReportInventoryTurnover, Name = "Vòng quay tồn kho", Description = "Phân tích tỉ lệ quay vòng, cảnh báo hàng om", Category = "1.7" },
                new() { Code = ExportFinancialData, Name = "Xuất dữ liệu tài chính", Description = "Xuất hóa đơn, báo cáo tài chính ra Excel/CSV", Category = "1.7" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "2. Technician & Support (Kỹ thuật & Bảo hành)",
            Icon = "bi-tools",
            Permissions = new List<PermissionItem>
            {
                new() { Code = ImeiSerialLookup, Name = "Tra cứu lịch sử IMEI/Serial", Description = "Tra cứu kích hoạt, thời hạn và vòng đời máy", Category = "2.1" },
                new() { Code = WarrantyPolicyView, Name = "Xem chính sách bảo hành", Description = "Xem chi tiết chính sách áp dụng theo dòng máy/linh kiện", Category = "2.1" },
                new() { Code = OrderLookupHistory, Name = "Tra cứu đơn hàng gốc (Đã ẩn TT thanh toán)", Description = "Tra cứu đơn gốc xác minh mua hàng, ẩn thông tin nhạy cảm", Category = "2.1" },
                new() { Code = TicketCreate, Name = "Lập phiếu tiếp nhận RMA", Description = "Ghi nhận thông tin khách, ngoại quan và lỗi mô tả", Category = "2.2" },
                new() { Code = TicketViewAssigned, Name = "Xem ca sửa chữa phân công", Description = "Xem danh sách ca sửa chữa cá nhân/chi nhánh", Category = "2.2" },
                new() { Code = TicketUpdateDiagnosis, Name = "Cập nhật chẩn đoán kỹ thuật", Description = "Ghi nhận lỗi NSX, rơi vỡ, quỳ tím, ẩm nước", Category = "2.2" },
                new() { Code = TicketUpdateStatus, Name = "Cập nhật tiến độ 6 bước", Description = "Tiếp nhận -> Kiểm tra -> Chờ linh kiện -> Đang sửa -> Đã test -> Chờ trả", Category = "2.2" },
                new() { Code = TicketUploadEvidence, Name = "Tải ảnh/video bằng chứng", Description = "Upload ảnh tem vỡ, quỳ tím, cấn móp", Category = "2.2" },
                new() { Code = TicketPrintReceipt, Name = "In phiếu biên nhận & Bàn giao", Description = "In biên nhận nhận máy, phiếu giao nhận, biên bản bàn giao", Category = "2.2" },
                new() { Code = SparePartsViewStock, Name = "Tra cứu tồn kho linh kiện", Description = "Tra cứu số lượng linh kiện màn hình, pin, mainboard", Category = "2.3" },
                new() { Code = SparePartsRequest, Name = "Yêu cầu xuất linh kiện ca sửa", Description = "Tạo phiếu xuất linh kiện phục vụ ca bảo hành", Category = "2.3" },
                new() { Code = SparePartsReturnDefective, Name = "Trả linh kiện hỏng về kho", Description = "Phiếu trả linh kiện hỏng cũ để đổi trả hãng / tiêu hủy", Category = "2.3" },
                new() { Code = ServiceCostEstimate, Name = "Bảng dự toán chi phí sửa chữa", Description = "Dự toán chi phí máy hết hạn / lỗi người dùng", Category = "2.4" },
                new() { Code = ServiceCostSubmitForApproval, Name = "Gửi báo giá duyệt can thiệp", Description = "Gửi báo giá cho khách hàng/quản lý phê duyệt", Category = "2.4" },
                new() { Code = CustomerNoteAdd, Name = "Ghi chú trao đổi & Hẹn trả máy", Description = "Lịch sử trao đổi, hẹn ngày trả máy với khách", Category = "2.4" },
                new() { Code = VendorRmaCreate, Name = "Tạo phiếu gửi TTBH của Hãng", Description = "Điều phối gửi thiết bị về TTBH ủy quyền hãng (AASP, ROG...)", Category = "2.4" },
                new() { Code = VendorRmaTrack, Name = "Theo dõi tiến độ từ Hãng", Description = "Theo dõi lộ trình và kết quả xử lý từ TTBH Hãng", Category = "2.4" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "3. Warehouse & Logistics (Thủ kho & Quản lý kho)",
            Icon = "bi-boxes",
            Permissions = new List<PermissionItem>
            {
                new() { Code = InboundViewPo, Name = "Xem đơn đặt hàng nhập (PO)", Description = "Xem danh sách PO từ nhà phân phối đã duyệt", Category = "3.1" },
                new() { Code = InboundCreateReceipt, Name = "Lập phiếu nhập kho (GRN)", Description = "Lập phiếu tiếp nhận thực tế từ kiện hàng nhà cung cấp", Category = "3.1" },
                new() { Code = InboundInspect, Name = "Kiểm định số lượng & Ngoại quan", Description = "Ghi nhận kết quả kiểm định kiện hàng, seal máy", Category = "3.1" },
                new() { Code = InboundScanImeiBatch, Name = "Quét & Import IMEI hàng loạt", Description = "Quét mã vạch nhập hàng loạt IMEI/Serial khui kiện", Category = "3.1" },
                new() { Code = InboundConfirm, Name = "Xác nhận hoàn tất nhập kho", Description = "Cộng dồn số lượng tồn khả dụng tự động", Category = "3.1" },
                new() { Code = SerialImeiView, Name = "Tra cứu định danh IMEI/Serial", Description = "Tra cứu trạng thái máy: trong kho, đã xuất, lỗi, đổi trả", Category = "3.2" },
                new() { Code = SerialImeiBindSku, Name = "Gán Serial với SKU sản phẩm", Description = "Ràng buộc IMEI với từng biến thể phần cứng", Category = "3.2" },
                new() { Code = SerialImeiUpdateStatus, Name = "Chuyển trạng thái IMEI", Description = "Chuyển từ In Stock sang Damaged, Reserved, InTransit", Category = "3.2" },
                new() { Code = SerialImeiPrintBarcode, Name = "Tạo & In tem Barcode/QR phụ", Description = "Tạo và in tem dán lên vỏ hộp máy", Category = "3.2" },
                new() { Code = OutboundViewPending, Name = "Xem đơn cần đóng gói xuất kho", Description = "Danh sách đơn hàng đã xác nhận chờ soạn hàng", Category = "3.3" },
                new() { Code = OutboundCreatePickList, Name = "Tạo phiếu nhặt hàng (Pick List)", Description = "Tối ưu hóa đường đi theo vị trí kệ hàng", Category = "3.3" },
                new() { Code = OutboundScanDispatch, Name = "Quét IMEI xuất đơn hàng", Description = "Ràng buộc Serial cụ thể vào đơn để kích hoạt BH", Category = "3.3" },
                new() { Code = OutboundPrintShippingLabel, Name = "In phiếu giao nhận & Vận đơn", Description = "In biên bản bàn giao cho bưu tá GHTK, GHN, Viettel", Category = "3.3" },
                new() { Code = OutboundHandoverCarrier, Name = "Bàn giao đối tác vận chuyển", Description = "Xác nhận bàn giao bưu tá và chuyển trạng thái đơn", Category = "3.3" },
                new() { Code = StocktakePerform, Name = "Thực hiện phiên kiểm kê kho", Description = "Nhập số liệu đếm thực tế, quét đối soát IMEI kệ", Category = "3.4" },
                new() { Code = StocktakeLogDiscrepancy, Name = "Lập biên bản chênh lệch kho", Description = "Ghi nhận thừa/thiếu và lý do chênh lệch kho", Category = "3.4" },
                new() { Code = InventoryAdjustDamaged, Name = "Giảm tồn hàng hư hại vật lý", Description = "Cách ly và giảm tồn máy rơi vỡ/lỗi trong quá trình lưu", Category = "3.4" },
                new() { Code = InventoryThresholdAlert, Name = "Cấu hình mức tồn an toàn", Description = "Cài đặt ngưỡng Safety Stock để nhận cảnh báo sắp hết", Category = "3.4" },
                new() { Code = TransferCreate, Name = "Lập phiếu điều chuyển chi nhánh", Description = "Tạo phiếu xuất điều chuyển giữa các kho", Category = "3.4" },
                new() { Code = TransferReceive, Name = "Tiếp nhận hàng điều chuyển đến", Description = "Xác nhận nhận máy từ kho khác gửi đến", Category = "3.4" },
                new() { Code = ReturnsReceive, Name = "Tiếp nhận hàng hoàn / Hàng đổi", Description = "Nhận lại kiện hàng bom hoặc đổi trả bảo hành", Category = "3.4" },
                new() { Code = ReturnsInspect, Name = "Kiểm tra đối chiếu IMEI hàng hoàn", Description = "Kiểm định ngoại quan và đối chiếu IMEI đơn gốc", Category = "3.4" },
                new() { Code = ReturnsRestock, Name = "Nhập lại kho / Cách ly hàng lỗi", Description = "Nhập lại tồn bán tiếp hoặc chuyển kho hàng lỗi", Category = "3.4" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "4. Sales & Order Processor (Bán hàng & Xử lý đơn)",
            Icon = "bi-headset",
            Permissions = new List<PermissionItem>
            {
                new() { Code = OrderViewList, Name = "Xem danh sách đơn", Description = "Tra cứu và lọc đơn hàng toàn hệ thống", Category = "4.1" },
                new() { Code = OrderViewDetail, Name = "Xem chi tiết đơn", Description = "Xem chi tiết sản phẩm, thanh toán và ghi chú", Category = "4.1" },
                new() { Code = OrderCreateManual, Name = "Tạo đơn thủ công", Description = "Tạo đơn khách chốt qua Hotline, Zalo OA, Fanpage, Quầy", Category = "4.1" },
                new() { Code = OrderUpdateStatus, Name = "Cập nhật trạng thái", Description = "Chờ xác nhận -> Đã gọi xác nhận -> Chờ đóng gói", Category = "4.1" },
                new() { Code = OrderAssignStaff, Name = "Nhận & Điều phối đơn", Description = "Tự nhận đơn hoặc chuyển giao đơn cho Sales khác", Category = "4.1" },
                new() { Code = OrderPrintInvoice, Name = "In hóa đơn & Phiếu tạm tính", Description = "In phiếu tạm tính, hóa đơn bán lẻ kẹp kiện hàng", Category = "4.1" },
                new() { Code = CustomerViewBasic, Name = "Tra cứu hồ sơ khách CRM", Description = "Xem lịch sử mua, công nợ và thứ hạng thành viên", Category = "4.2" },
                new() { Code = CustomerContactInfo, Name = "Xem & Xác thực liên hệ", Description = "Kiểm tra SĐT và email người nhận hàng", Category = "4.2" },
                new() { Code = OrderUpdateShippingInfo, Name = "Chỉnh sửa địa chỉ & Khung giờ", Description = "Đổi địa chỉ, SĐT, hẹn giờ giao trước khi chuyển bưu tá", Category = "4.2" },
                new() { Code = OrderItemModify, Name = "Thay đổi biến thể/thêm linh kiện", Description = "Đổi màu, nâng dung lượng hoặc thêm phụ kiện trước chốt", Category = "4.2" },
                new() { Code = OrderInternalNote, Name = "Ghi chú nội bộ", Description = "Ghi chú ca giao, lưu ý đóng gói gửi Thủ kho", Category = "4.2" },
                new() { Code = ProductViewCatalog, Name = "Tra cứu Catalog thông số", Description = "Tra cứu thông số kỹ thuật để tư vấn bán hàng", Category = "4.3" },
                new() { Code = InventoryCheckAvailable, Name = "Kiểm tra tồn khả dụng chi nhánh", Description = "Xem tồn kho khả dụng để báo khách còn/hết hàng", Category = "4.3" },
                new() { Code = PromotionApplyCode, Name = "Áp dụng mã khuyến mãi", Description = "Nhập mã voucher, áp dụng Flash Sale vào đơn", Category = "4.3" },
                new() { Code = OrderCancel, Name = "Hủy đơn hàng", Description = "Hủy đơn khi khách đổi ý, spam hoặc không liên lạc được", Category = "4.3" },
                new() { Code = RefundRequestCreate, Name = "Tạo yêu cầu hoàn tiền", Description = "Lập phiếu yêu cầu hoàn tiền gửi Kế toán duyệt", Category = "4.3" },
                new() { Code = ReturnInitiate, Name = "Khởi tạo yêu cầu đổi/trả", Description = "Tiếp nhận đổi/trả ban đầu chuyển Kỹ thuật thẩm định", Category = "4.3" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "5. Customer & Member (Khách hàng thành viên)",
            Icon = "bi-person-badge",
            Permissions = new List<PermissionItem>
            {
                new() { Code = ProfileViewOwn, Name = "Xem hồ sơ cá nhân", Description = "Xem thông tin cá nhân, hạng thành viên VIP, điểm tích lũy", Category = "5.1" },
                new() { Code = ProfileUpdateOwn, Name = "Cập nhật hồ sơ cá nhân", Description = "Cập nhật họ tên, ảnh đại diện, ngày sinh", Category = "5.1" },
                new() { Code = AddressManageOwn, Name = "Sổ địa chỉ cá nhân", Description = "Thêm, sửa, xóa và chọn địa chỉ nhận hàng mặc định", Category = "5.1" },
                new() { Code = SecurityChangePassword, Name = "Đổi mật khẩu & 2FA", Description = "Đổi mật khẩu bảo vệ tài khoản, bật xác thực 2 bước", Category = "5.1" },
                new() { Code = AccountDeleteRequest, Name = "Yêu cầu xóa tài khoản", Description = "Gửi yêu cầu hủy hoặc xóa dữ liệu tài khoản cá nhân", Category = "5.1" },
                new() { Code = CartManageOwn, Name = "Quản lý giỏ hàng", Description = "Thêm/bớt sản phẩm, đồng bộ giỏ hàng đa thiết bị", Category = "5.2" },
                new() { Code = WishlistManageOwn, Name = "Danh sách yêu thích", Description = "Lưu sản phẩm quan tâm, nhận thông báo giảm giá", Category = "5.2" },
                new() { Code = CouponSaveOwn, Name = "Ví voucher cá nhân", Description = "Lưu mã giảm giá và coupon khuyến mãi vào tài khoản", Category = "5.2" },
                new() { Code = OrderCreateCheckout, Name = "Đặt hàng trực tuyến", Description = "Tiến hành checkout, chọn địa chỉ và hình thức nhận", Category = "5.2" },
                new() { Code = OrderApplyPoints, Name = "Dùng điểm tích lũy", Description = "Đổi điểm thưởng cấn trừ trực tiếp vào giá trị đơn", Category = "5.2" },
                new() { Code = PaymentInitiate, Name = "Thanh toán trực tuyến", Description = "Khởi tạo thanh toán VNPay, MoMo, VietQR", Category = "5.2" },
                new() { Code = OrderViewOwnList, Name = "Lịch sử đơn mua", Description = "Xem danh sách các đơn hàng đã đặt của bản thân", Category = "5.3" },
                new() { Code = OrderViewOwnDetail, Name = "Chi tiết đơn hàng", Description = "Xem chi tiết mặt hàng, linh kiện, Serial/IMEI và hóa đơn VAT", Category = "5.3" },
                new() { Code = OrderTrackShipping, Name = "Theo dõi vận đơn bưu tá", Description = "Tra cứu lộ trình di chuyển kiện hàng theo thời gian thực", Category = "5.3" },
                new() { Code = OrderCancelOwn, Name = "Tự hủy đơn chờ xử lý", Description = "Hủy đơn hàng của mình khi đơn còn ở trạng thái Chờ xác nhận", Category = "5.3" },
                new() { Code = OrderConfirmReceived, Name = "Xác nhận đã nhận máy", Description = "Xác nhận nhận hàng để kích hoạt tích điểm và bảo hành", Category = "5.3" },
                new() { Code = WarrantyViewOwnDevices, Name = "Thiết bị bảo hành của tôi", Description = "Xem danh sách máy đã mua và hạn bảo hành theo IMEI", Category = "5.3" },
                new() { Code = WarrantyCheckStatus, Name = "Tiến độ sửa chữa RMA", Description = "Tra cứu tiến độ xử lý máy bảo hành qua mã phiếu tiếp nhận", Category = "5.3" },
                new() { Code = ReturnRequestCreate, Name = "Yêu cầu đổi/trả & hoàn tiền", Description = "Gửi yêu cầu đổi trả kèm hình ảnh/video bằng chứng", Category = "5.3" },
                new() { Code = ReviewCreateVerified, Name = "Đánh giá đơn mua thành công", Description = "Viết đánh giá, chấm sao sản phẩm đã mua thành công", Category = "5.3" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "6. Khách vãng lai & Công cộng (Guest & Public)",
            Icon = "bi-globe",
            Permissions = new List<PermissionItem>
            {
                new() { Code = CatalogViewPublic, Name = "Khám phá danh mục & chính sách", Description = "Xem trang chủ, bài viết công nghệ và các trang chính sách", Category = "6.1" },
                new() { Code = ProductViewDetail, Name = "Chi tiết thiết bị & giá bán", Description = "Xem thông số kỹ thuật, video unbox, ảnh thực tế, tình trạng kho", Category = "6.1" },
                new() { Code = SearchQuery, Name = "Tìm kiếm sản phẩm", Description = "Tìm kiếm theo tên thiết bị, model, từ khóa linh kiện hoặc mã SKU", Category = "6.1" },
                new() { Code = FilterApply, Name = "Bộ lọc sản phẩm đa tiêu chí", Description = "Lọc theo CPU, RAM, GPU, kích thước màn hình, tầm giá, thương hiệu", Category = "6.1" },
                new() { Code = ProductCompareSession, Name = "So sánh thiết bị song song", Description = "So sánh thông số 2 hoặc nhiều máy song song (LocalStorage)", Category = "6.1" },
                new() { Code = WarrantyPublicLookup, Name = "Tra cứu bảo hành công cộng", Description = "Tra cứu thời hạn bảo hành điện tử bằng số IMEI/Serial", Category = "6.1" },
                new() { Code = StoreLocatorView, Name = "Hệ thống showroom & chi nhánh", Description = "Xem danh sách showroom, trung tâm bảo hành và bản đồ chỉ đường", Category = "6.1" },
                new() { Code = ReviewReadPublic, Name = "Đọc đánh giá & nhận xét", Description = "Đọc các bình luận, đánh giá sao từ khách hàng mua trước", Category = "6.1" },
                new() { Code = CartCreateSession, Name = "Giỏ hàng phiên tạm thời", Description = "Khởi tạo giỏ hàng dựa trên session_id không cần tài khoản", Category = "6.2" },
                new() { Code = CartAddItem, Name = "Thêm thiết bị vào giỏ tạm", Description = "Thêm biến thể máy vào giỏ hàng vãng lai", Category = "6.2" },
                new() { Code = CartUpdateQuantity, Name = "Cập nhật số lượng giỏ tạm", Description = "Tăng giảm số lượng thiết bị trong giỏ hàng tạm", Category = "6.2" },
                new() { Code = CouponSimulate, Name = "Thử mã giảm giá (Simulate)", Description = "Nhập thử mã voucher công khai để kiểm tra điều kiện giảm giá", Category = "6.2" },
                new() { Code = CheckoutGuestMode, Name = "Đặt hàng nhanh vãng lai", Description = "Đặt hàng không cần tài khoản (nhập tên, SĐT, địa chỉ nhận)", Category = "6.2" },
                new() { Code = OrderTrackByToken, Name = "Tra cứu đơn qua Mã + SĐT", Description = "Tra cứu lộ trình đơn hàng bằng cặp mã đơn và số điện thoại", Category = "6.2" },
                new() { Code = AuthRegisterLogin, Name = "Đăng ký & Đăng nhập", Description = "Gửi yêu cầu đăng ký tài khoản hoặc đăng nhập hệ thống", Category = "6.2" }
            }
        },
        new PermissionCategoryGroup
        {
            Category = "1.8 Cấu hình Hệ thống & DevOps",
            Icon = "bi-gear-wide-connected",
            Permissions = new List<PermissionItem>
            {
                new() { Code = PaymentGatewayConfig, Name = "Cổng thanh toán API", Description = "Cài đặt VNPay, MoMo, ZaloPay, VietQR", Category = "1.8" },
                new() { Code = ThirdPartyApiManage, Name = "API SMS, Email, Firebase", Description = "Cấu hình SMS OTP Brandname, SMTP, Push Notif", Category = "1.8" },
                new() { Code = AuditLogView, Name = "Nhật ký hệ thống (Audit Log)", Description = "Xem ai vừa sửa giá, xóa đơn, cấp quyền", Category = "1.8" },
                new() { Code = SystemMaintenance, Name = "Bảo trì & Cache Redis", Description = "Bật chế độ bảo trì, flush cache, backup CSDL", Category = "1.8" }
            }
        }
    };

    public static List<string> GetAllPermissionCodes()
    {
        return AllGroups.SelectMany(g => g.Permissions).Select(p => p.Code).ToList();
    }

    public static List<string> GetPublicGuestPermissions()
    {
        return new List<string>
        {
            CatalogViewPublic, ProductViewDetail, SearchQuery, FilterApply,
            ProductCompareSession, WarrantyPublicLookup, StoreLocatorView, ReviewReadPublic,
            CartCreateSession, CartAddItem, CartUpdateQuantity, CouponSimulate,
            CheckoutGuestMode, OrderTrackByToken, AuthRegisterLogin
        };
    }

    public static List<string> GetDefaultPermissionsForRole(UserRole role)
    {
        switch (role)
        {
            case UserRole.SuperAdmin:
                return GetAllPermissionCodes();

            case UserRole.SalesStaff:
                return new List<string>
                {
                    CategoryView, ProductView, ReviewManage,
                    InventoryView, ImeiSerialTrack,
                    OrderViewAll, OrderCreateManual, OrderUpdateStatus, OrderCancel,
                    WarrantyLookup,
                    CustomerView, CustomerUpdate, CustomerAddressManage, CouponManage, BannerSeoManage,
                    // Section 4 Permissions
                    OrderViewList, OrderViewDetail, OrderAssignStaff, OrderPrintInvoice,
                    CustomerViewBasic, CustomerContactInfo, OrderUpdateShippingInfo, OrderItemModify, OrderInternalNote,
                    ProductViewCatalog, InventoryCheckAvailable, PromotionApplyCode, RefundRequestCreate, ReturnInitiate
                };

            case UserRole.WarehouseStaff:
                return new List<string>
                {
                    CategoryView, ProductView,
                    InventoryView, InventoryAdjust, StockImport, StockExport, ImeiSerialTrack, SupplierManage,
                    OrderViewAll, OrderUpdateStatus,
                    WarrantyLookup,
                    InboundViewPo, InboundCreateReceipt, InboundInspect, InboundScanImeiBatch, InboundConfirm,
                    SerialImeiView, SerialImeiBindSku, SerialImeiUpdateStatus, SerialImeiPrintBarcode,
                    OutboundViewPending, OutboundCreatePickList, OutboundScanDispatch, OutboundPrintShippingLabel, OutboundHandoverCarrier,
                    StocktakePerform, StocktakeLogDiscrepancy, InventoryAdjustDamaged, InventoryThresholdAlert,
                    TransferCreate, TransferReceive, ReturnsReceive, ReturnsInspect, ReturnsRestock
                };

            case UserRole.Technician:
                return new List<string>
                {
                    CategoryView, ProductView,
                    WarrantyPolicyManage, WarrantyLookup, TicketManage, ImeiSerialTrack,
                    ImeiSerialLookup, WarrantyPolicyView, OrderLookupHistory,
                    TicketCreate, TicketViewAssigned, TicketUpdateDiagnosis, TicketUpdateStatus, TicketUploadEvidence, TicketPrintReceipt,
                    SparePartsViewStock, SparePartsRequest, SparePartsReturnDefective,
                    ServiceCostEstimate, ServiceCostSubmitForApproval, CustomerNoteAdd, VendorRmaCreate, VendorRmaTrack
                };

            case UserRole.Accountant:
                return new List<string>
                {
                    OrderViewAll, RefundApprove, SupplierManage,
                    ReportRevenue, ReportProfitMargin, ReportInventoryTurnover, ExportFinancialData
                };

            case UserRole.MarketingStaff:
                return new List<string>
                {
                    CategoryView, ProductView, ReviewManage,
                    CustomerView, CouponManage, BannerSeoManage
                };

            case UserRole.Customer:
                return new List<string>
                {
                    ProductView,
                    ProfileViewOwn, ProfileUpdateOwn, AddressManageOwn, SecurityChangePassword, AccountDeleteRequest,
                    CartManageOwn, WishlistManageOwn, CouponSaveOwn, OrderCreateCheckout, OrderApplyPoints, PaymentInitiate,
                    OrderViewOwnList, OrderViewOwnDetail, OrderTrackShipping, OrderCancelOwn, OrderConfirmReceived,
                    WarrantyViewOwnDevices, WarrantyCheckStatus, ReturnRequestCreate, ReviewCreateVerified
                };

            default:
                return GetPublicGuestPermissions();
        }
    }
}
