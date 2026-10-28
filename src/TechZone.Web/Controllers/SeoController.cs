using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using TechZone.Domain.Entities;
using TechZone.Domain.Interfaces;

namespace TechZone.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class SeoController : Controller
{
    private readonly IRepository<Product> _productRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly ILogger<SeoController> _logger;

    public SeoController(
        IRepository<Product> productRepo,
        IRepository<Category> categoryRepo,
        ILogger<SeoController> logger)
    {
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
        _logger = logger;
    }

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            
            var root = new XElement(ns + "urlset");

            // 1. Static Core Pages
            var staticPages = new (string Path, string ChangeFreq, string Priority)[]
            {
                ("/", "daily", "1.0"),
                ("/Catalog", "daily", "0.9"),
                ("/Warranty", "monthly", "0.5"),
                ("/Order/Track", "monthly", "0.5")
            };

            var nowFormatted = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz");

            foreach (var page in staticPages)
            {
                root.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}{page.Path}"),
                    new XElement(ns + "lastmod", nowFormatted),
                    new XElement(ns + "changefreq", page.ChangeFreq),
                    new XElement(ns + "priority", page.Priority)
                ));
            }

            // 2. Dynamic Categories
            var categories = await _categoryRepo.FindAsync(c => c.IsActive, cancellationToken);
            foreach (var cat in categories)
            {
                var lastMod = (cat.UpdatedAt ?? cat.CreatedAt).ToString("yyyy-MM-ddTHH:mm:sszzz");
                root.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/Catalog?category={cat.Slug}"),
                    new XElement(ns + "lastmod", lastMod),
                    new XElement(ns + "changefreq", "weekly"),
                    new XElement(ns + "priority", "0.8")
                ));
            }

            // 3. Dynamic Products
            var products = await _productRepo.FindAsync(p => p.IsActive, cancellationToken);
            foreach (var product in products)
            {
                var lastMod = (product.UpdatedAt ?? product.CreatedAt).ToString("yyyy-MM-ddTHH:mm:sszzz");
                root.Add(new XElement(ns + "url",
                    new XElement(ns + "loc", $"{baseUrl}/product/{product.Slug}"),
                    new XElement(ns + "lastmod", lastMod),
                    new XElement(ns + "changefreq", "daily"),
                    new XElement(ns + "priority", "0.9")
                ));
            }

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
            return Content(doc.ToString(), "application/xml", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while dynamically generating sitemap.xml");
            return StatusCode(500, "Error generating sitemap");
        }
    }

    [HttpGet("robots.txt")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Disallow: /Admin/");
        sb.AppendLine("Disallow: /Cart/");
        sb.AppendLine("Disallow: /Checkout/");
        sb.AppendLine("Disallow: /Auth/");
        sb.AppendLine("Disallow: /api/");
        sb.AppendLine("Disallow: /hub/");
        sb.AppendLine("Disallow: /Notification/");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }
}
