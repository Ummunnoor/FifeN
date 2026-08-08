using Application.DTOs;
using Application.Modules.Catalog.DTOs;
using Application.Modules.Discovery.DTOs;
using Application.Modules.Discovery.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Public browse, search, fresh-supply feed, categories, and shop pages.</summary>
    [ApiController]
    [Route("api/v1")]
    [AllowAnonymous]
    public class DiscoveryController(IDiscoveryService discovery) : ControllerBase
    {
        /// <summary>Searches and filters live listings.</summary>
        [HttpGet("products")]
        public async Task<ActionResult<PagedResponse<ProductSummary>>> Search(
            [FromQuery] ProductQuery query, CancellationToken ct) =>
            Ok(await discovery.SearchAsync(query, ct));

        /// <summary>"New this week" fresh-supply feed, optionally filtered by category.</summary>
        [HttpGet("products/new")]
        public async Task<ActionResult<PagedResponse<ProductSummary>>> NewThisWeek(
            [FromQuery] Guid? categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
            Ok(await discovery.NewThisWeekAsync(categoryId, page, pageSize, ct));

        /// <summary>Active categories.</summary>
        [HttpGet("categories")]
        public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> Categories(CancellationToken ct) =>
            Ok(await discovery.GetCategoriesAsync(ct));

        /// <summary>A vendor's public shop page (their live listings).</summary>
        [HttpGet("vendors/{id:guid}/products")]
        public async Task<ActionResult<PagedResponse<ProductSummary>>> VendorListings(
            Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
            Ok(await discovery.GetVendorListingsAsync(id, page, pageSize, ct));
    }
}
