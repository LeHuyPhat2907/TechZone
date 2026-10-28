using Microsoft.AspNetCore.Mvc;

namespace TechZone.Web.Controllers;

public class StoreController : Controller
{
    [HttpGet]
    [Route("Store")]
    [Route("Store/Index")]
    [Route("StoreLocator")]
    [Route("he-thong-cua-hang")]
    public IActionResult Index(string? city = null, string? feature = null)
    {
        var allStores = GetTechZoneShowrooms();

        if (!string.IsNullOrWhiteSpace(city))
        {
            allStores = allStores.Where(s => s.City.Equals(city.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(feature))
        {
            if (feature == "rma")
            {
                allStores = allStores.Where(s => s.IsRmaServiceCenter).ToList();
            }
            else if (feature == "flagship")
            {
                allStores = allStores.Where(s => s.IsFlagship).ToList();
            }
        }

        ViewBag.SelectedCity = city ?? "all";
        ViewBag.SelectedFeature = feature ?? "all";
        return View(allStores);
    }

    private static List<StoreLocationDto> GetTechZoneShowrooms()
    {
        return new List<StoreLocationDto>
        {
            new()
            {
                Id = "store-hcm-01",
                Name = "TechZone Flagship Store Quận 1",
                Address = "123 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
                City = "TP. Hồ Chí Minh",
                Phone = "028 7300 8888",
                OpeningHours = "08:00 - 21:30 (Thứ 2 - Chủ Nhật, kể cả ngày Lễ)",
                IsFlagship = true,
                IsRmaServiceCenter = true,
                HasParking = true,
                GoogleMapsUrl = "https://maps.google.com/?q=123+Le+Loi+Ben+Nghe+District+1+Ho+Chi+Minh+City",
                EmbedMapQuery = "123 Lê Lợi, Bến Nghé, Quận 1, Hồ Chí Minh",
                Features = new List<string>
                {
                    "Trải nghiệm trực tiếp iPhone 16 Pro Max & Samsung S24 Ultra",
                    "Trung tâm Bảo hành Ủy quyền & Tiếp nhận RMA 1-đổi-1",
                    "Khu vực Thu Cũ Đổi Mới trợ giá ngay 3 triệu",
                    "Dán kính cường lực & Vệ sinh máy miễn phí trọn đời",
                    "Bãi đỗ ô tô và xe máy rộng rãi miễn phí"
                }
            },
            new()
            {
                Id = "store-hcm-02",
                Name = "TechZone Showroom & Service Hub Quận 10",
                Address = "382 Ba Tháng Hai, Phường 12, Quận 10, TP. Hồ Chí Minh",
                City = "TP. Hồ Chí Minh",
                Phone = "028 7300 8889",
                OpeningHours = "08:00 - 21:30",
                IsFlagship = false,
                IsRmaServiceCenter = true,
                HasParking = true,
                GoogleMapsUrl = "https://maps.google.com/?q=382+Ba+Thang+Hai+District+10+Ho+Chi+Minh+City",
                EmbedMapQuery = "382 Ba Tháng Hai, Phường 12, Quận 10, Hồ Chí Minh",
                Features = new List<string>
                {
                    "Khu trải nghiệm Laptop Gaming & AI Workstation",
                    "Phòng Kỹ thuật thẩm định & sửa chữa lấy liền 60 phút",
                    "Giao nhận hàng bưu tá hỏa tốc 2 giờ nội thành"
                }
            },
            new()
            {
                Id = "store-hn-01",
                Name = "TechZone Flagship Store Cầu Giấy (Hà Nội)",
                Address = "234 Cầu Giấy, Phường Quan Hoa, Quận Cầu Giấy, TP. Hà Nội",
                City = "Hà Nội",
                Phone = "024 7300 8888",
                OpeningHours = "08:00 - 21:30",
                IsFlagship = true,
                IsRmaServiceCenter = true,
                HasParking = true,
                GoogleMapsUrl = "https://maps.google.com/?q=234+Cau+Giay+Hanoi",
                EmbedMapQuery = "234 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội",
                Features = new List<string>
                {
                    "Showroom công nghệ diện tích 500m2 tiêu chuẩn Quốc tế",
                    "Trung tâm Bảo hành & Thay thế linh kiện chính hãng tại Hà Nội",
                    "Hỗ trợ trả góp 0% lãi suất xét duyệt CCCD trong 5 phút",
                    "Có chỗ đậu xe ô tô có bảo vệ trông giữ"
                }
            },
            new()
            {
                Id = "store-hn-02",
                Name = "TechZone Showroom Đống Đa",
                Address = "179 Thái Hà, Phường Láng Hạ, Quận Đống Đa, TP. Hà Nội",
                City = "Hà Nội",
                Phone = "024 7300 8889",
                OpeningHours = "08:00 - 21:30",
                IsFlagship = false,
                IsRmaServiceCenter = false,
                HasParking = false,
                GoogleMapsUrl = "https://maps.google.com/?q=179+Thai+Ha+Dong+Da+Hanoi",
                EmbedMapQuery = "179 Thái Hà, Đống Đa, Hà Nội",
                Features = new List<string>
                {
                    "Khu phụ kiện Apple Chính hãng & Cáp sạc GaN Anker/Ugreen",
                    "Điểm nhận hàng Click & Collect tiện lợi"
                }
            },
            new()
            {
                Id = "store-dn-01",
                Name = "TechZone Showroom & TTBH Đà Nẵng",
                Address = "45 Nguyễn Văn Linh, Phường Nam Dương, Quận Hải Châu, TP. Đà Nẵng",
                City = "Đà Nẵng",
                Phone = "0236 7300 8888",
                OpeningHours = "08:00 - 21:30",
                IsFlagship = true,
                IsRmaServiceCenter = true,
                HasParking = true,
                GoogleMapsUrl = "https://maps.google.com/?q=45+Nguyen+Van+Linh+Da+Nang",
                EmbedMapQuery = "45 Nguyễn Văn Linh, Hải Châu, Đà Nẵng",
                Features = new List<string>
                {
                    "Showroom trung tâm miền Trung lớn nhất của TechZone",
                    "Tiếp nhận bảo hành điện tử và đổi trả 1-1 chính hãng",
                    "Đầy đủ máy bay không người lái DJI và Audio cao cấp"
                }
            },
            new()
            {
                Id = "store-ct-01",
                Name = "TechZone Showroom Cần Thơ",
                Address = "88 Đại Lộ Hòa Bình, Phường Tân An, Quận Ninh Kiều, TP. Cần Thơ",
                City = "Cần Thơ",
                Phone = "0292 7300 8888",
                OpeningHours = "08:00 - 21:30",
                IsFlagship = false,
                IsRmaServiceCenter = true,
                HasParking = true,
                GoogleMapsUrl = "https://maps.google.com/?q=88+Hoa+Binh+Can+Tho",
                EmbedMapQuery = "88 Hòa Bình, Ninh Kiều, Cần Thơ",
                Features = new List<string>
                {
                    "Showroom hiện đại phục vụ khu vực Tây Nam Bộ",
                    "Tiếp nhận bảo hành điện tử chính hãng"
                }
            },
            new()
            {
                Id = "store-hp-01",
                Name = "TechZone Showroom Hải Phòng",
                Address = "156 Lạch Tray, Quận Ngô Quyền, TP. Hải Phòng",
                City = "Hải Phòng",
                Phone = "0225 7300 8888",
                OpeningHours = "08:00 - 21:30",
                IsFlagship = false,
                IsRmaServiceCenter = true,
                HasParking = true,
                GoogleMapsUrl = "https://maps.google.com/?q=156+Lach+Tray+Hai+Phong",
                EmbedMapQuery = "156 Lạch Tray, Ngô Quyền, Hải Phòng",
                Features = new List<string>
                {
                    "Khu trải nghiệm Gaming PC & Smartphone cao cấp",
                    "Dịch vụ kỹ thuật viên bảo hành tại chỗ"
                }
            }
        };
    }
}

public class StoreLocationDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string OpeningHours { get; set; } = string.Empty;
    public bool IsFlagship { get; set; }
    public bool IsRmaServiceCenter { get; set; }
    public bool HasParking { get; set; }
    public string GoogleMapsUrl { get; set; } = string.Empty;
    public string EmbedMapQuery { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
}
