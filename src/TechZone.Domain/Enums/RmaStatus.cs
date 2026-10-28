namespace TechZone.Domain.Enums;

public enum RmaStatus
{
    Submitted = 0,        // Khách mới tạo yêu cầu đổi trả / Tiếp nhận
    UnderReview = 1,      // CSKH / Kỹ thuật đang thẩm định video, hình ảnh
    DeviceReceived = 2,   // Đã nhận máy vật lý tại trung tâm bảo hành
    AssignedToTech = 7,   // Đã điều phối kỹ thuật viên
    Inspecting = 3,       // Đang kiểm tra ngoại quan, quỳ tím, chẩn đoán lỗi
    QuotationPending = 8, // Đã báo giá linh kiện & Chờ khách duyệt
    WaitingForParts = 11, // Chờ xuất linh kiện thay thế từ kho
    Approved = 4,         // Chấp thuận đổi 1-1 / phương án sửa chữa
    Repairing = 9,        // Đang tiến hành sửa chữa / thay thế linh kiện
    TestedQC = 12,        // Đã test chức năng đạt chuẩn QC
    ReadyForPickup = 10,  // Đã sửa xong & Chờ trả máy cho khách
    SentToVendor = 13,    // Đã chuyển về TTBH ủy quyền của Hãng (Vendor RMA)
    Completed = 6,        // Hoàn tất (Đã bàn giao máy cho khách)
    Rejected = 5          // Từ chối bảo hành (do lỗi người dùng / không duyệt)
}
