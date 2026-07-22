using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Data;
using RetailInventory.Api.Dtos;
using RetailInventory.Api.Models;

namespace RetailInventory.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductListDto>>> GetProducts(
        [FromQuery] decimal? minimumPrice,
        [FromQuery] string? search)
    {
        var query = context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Tags)
            .AsQueryable();

        if (minimumPrice is not null)
        {
            query = query.Where(product => (double)product.Price >= (double)minimumPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(product => product.Name.Contains(search));
        }

        var products = await query
            .OrderByDescending(product => (double)product.Price)
            .Select(product => new ProductListDto(
                product.Id,
                product.Name,
                product.Price,
                product.StockQuantity,
                product.Category!.Name,
                product.RowVersion,
                product.Tags.Select(tag => tag.Name).OrderBy(name => name).ToList()))
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(int id)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.ProductDetail)
            .Include(item => item.Tags)
            .Where(item => item.Id == id)
            .Select(item => ToDetailDto(item))
            .FirstOrDefaultAsync();

        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("find/{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> FindProduct(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        await context.Entry(product).Reference(item => item.Category).LoadAsync();
        await context.Entry(product).Reference(item => item.ProductDetail).LoadAsync();
        await context.Entry(product).Collection(item => item.Tags).LoadAsync();

        return Ok(ToDetailDto(product));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDetailDto>> CreateProduct(CreateProductDto request)
    {
        if (!await context.Categories.AnyAsync(category => category.Id == request.CategoryId))
        {
            return BadRequest("Category does not exist.");
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CategoryId = request.CategoryId,
            ProductDetail = string.IsNullOrWhiteSpace(request.WarrantyInfo)
                ? null
                : new ProductDetail { WarrantyInfo = request.WarrantyInfo.Trim() }
        };

        await ApplyTags(product, request.TagIds);
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, await LoadDetail(product.Id));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> UpdateProduct(int id, UpdateProductDto request)
    {
        var product = await context.Products
            .Include(item => item.ProductDetail)
            .Include(item => item.Tags)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        if (!await context.Categories.AnyAsync(category => category.Id == request.CategoryId))
        {
            return BadRequest("Category does not exist.");
        }

        context.Entry(product).Property(item => item.RowVersion).OriginalValue = request.RowVersion;

        product.Name = request.Name.Trim();
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.CategoryId = request.CategoryId;
        product.RowVersion = Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(request.WarrantyInfo))
        {
            product.ProductDetail = null;
        }
        else if (product.ProductDetail is null)
        {
            product.ProductDetail = new ProductDetail { WarrantyInfo = request.WarrantyInfo.Trim() };
        }
        else
        {
            product.ProductDetail.WarrantyInfo = request.WarrantyInfo.Trim();
        }

        await ApplyTags(product, request.TagIds);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict detected. Reload the product and retry with the latest row version.");
        }

        return Ok(await LoadDetail(product.Id));
    }

    [HttpPatch("{id:int}/stock")]
    public async Task<ActionResult<ProductDetailDto>> AdjustStock(int id, StockAdjustmentDto request)
    {
        var product = await context.Products.FirstOrDefaultAsync(item => item.Id == id);
        if (product is null)
        {
            return NotFound();
        }

        context.Entry(product).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        product.StockQuantity += request.Quantity;
        product.RowVersion = Guid.NewGuid().ToString("N");

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Concurrency conflict detected. Stock was updated by another user.");
        }

        return Ok(await LoadDetail(product.Id));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        context.Products.Remove(product);
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("reports/expensive")]
    public async Task<ActionResult<IReadOnlyList<ReportProductDto>>> GetExpensiveProducts([FromQuery] decimal minimumPrice = 10000)
    {
        var products = new List<ReportProductDto>();

        await foreach (var product in CompiledInventoryQueries.ExpensiveProducts(context, minimumPrice))
        {
            products.Add(product);
        }

        return Ok(products);
    }

    [HttpPost("batch/restock")]
    public async Task<ActionResult<int>> RestockAll([FromQuery] int quantity = 10)
    {
        var updated = await context.Products.ExecuteUpdateAsync(setters =>
            setters.SetProperty(product => product.StockQuantity, product => product.StockQuantity + quantity)
                .SetProperty(product => product.RowVersion, _ => Guid.NewGuid().ToString("N")));

        return Ok(updated);
    }

    private async Task ApplyTags(Product product, IReadOnlyList<int>? tagIds)
    {
        product.Tags.Clear();

        if (tagIds is null || tagIds.Count == 0)
        {
            return;
        }

        var tags = await context.Tags
            .Where(tag => tagIds.Contains(tag.Id))
            .ToListAsync();

        product.Tags.AddRange(tags);
    }

    private async Task<ProductDetailDto?> LoadDetail(int id) =>
        await context.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.ProductDetail)
            .Include(product => product.Tags)
            .Where(product => product.Id == id)
            .Select(product => ToDetailDto(product))
            .FirstOrDefaultAsync();

    private static ProductDetailDto ToDetailDto(Product product) =>
        new(
            product.Id,
            product.Name,
            product.Price,
            product.StockQuantity,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.ProductDetail?.WarrantyInfo,
            product.RowVersion,
            product.Tags.Select(tag => tag.Name).OrderBy(name => name).ToList());
}
