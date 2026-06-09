using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Abstractions;
using Application.Modules.Catalog.DTOs;
using Application.Modules.Catalog.Services.Interfaces;
using Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    /// <summary>Vendor listing management and public listing detail.</summary>
    [ApiController]
    [Route("api/v1/products")]
    public class ProductController(IProductService products, ICurrentUserService currentUser) : ControllerBase
    {
        /// <summary>Creates a listing (approved vendors only).</summary>
        [HttpPost]
        [Authorize(Policy = "RequireVendor")]
        public async Task<ActionResult<ProductDetail>> Create([FromBody] CreateProductRequest request, CancellationToken ct)
        {
            var detail = await products.CreateAsync(currentUser.RequireUserId(), request, ct);
            return CreatedAtAction(nameof(GetById), new { id = detail.Id }, detail);
        }

        /// <summary>Updates a listing (owner only).</summary>
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "RequireVendor")]
        public async Task<ActionResult<ProductDetail>> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct) =>
            Ok(await products.UpdateAsync(currentUser.RequireUserId(), id, request, ct));

        /// <summary>Changes a listing's status (owner only).</summary>
        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = "RequireVendor")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request, CancellationToken ct)
        {
            await products.ChangeStatusAsync(currentUser.RequireUserId(), id, request, ct);
            return Ok();
        }

        /// <summary>Soft-deletes (archives) a listing. Owners or admins.</summary>
        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            await products.DeleteAsync(currentUser.RequireUserId(), currentUser.IsAdmin, id, ct);
            return NoContent();
        }

        /// <summary>Uploads 1–5 images (JPEG/PNG/WebP, ≤5MB each) to a listing (owner only).</summary>
        [HttpPost("{id:guid}/images")]
        [Authorize(Policy = "RequireVendor")]
        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<ActionResult<IReadOnlyList<string>>> UploadImages(
            Guid id, [FromForm] List<IFormFile> files, CancellationToken ct)
        {
            var uploads = files.Select(f =>
                new ImageUpload(f.OpenReadStream(), f.FileName, f.ContentType, f.Length)).ToList();

            try
            {
                var urls = await products.AddImagesAsync(currentUser.RequireUserId(), id, uploads, ct);
                return Created($"/api/v1/products/{id}", urls);
            }
            finally
            {
                // Dispose the request streams even when validation/upload throws.
                foreach (var upload in uploads)
                    upload.Content.Dispose();
            }
        }

        /// <summary>Public listing detail. Increments the view count; hidden listings return 404.</summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<ProductDetail>> GetById(Guid id, CancellationToken ct) =>
            Ok(await products.GetPublicDetailAsync(id, ct));
    }
}
