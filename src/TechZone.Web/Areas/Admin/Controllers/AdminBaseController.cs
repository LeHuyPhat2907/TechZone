using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TechZone.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "SuperAdmin,SalesStaff,WarehouseStaff,Technician,Accountant")]
public abstract class AdminBaseController : Controller
{
}
