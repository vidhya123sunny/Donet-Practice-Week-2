using Microsoft.EntityFrameworkCore;
using RetailInventory.Api.Dtos;

namespace RetailInventory.Api.Data;

public static class CompiledInventoryQueries
{
    public static readonly Func<AppDbContext, decimal, IAsyncEnumerable<ReportProductDto>> ExpensiveProducts =
        EF.CompileAsyncQuery((AppDbContext context, decimal minimumPrice) =>
            context.Products
                .AsNoTracking()
                .Where(product => (double)product.Price >= (double)minimumPrice)
                .OrderByDescending(product => (double)product.Price)
                .Select(product => new ReportProductDto(
                    product.Name,
                    product.Price,
                    product.StockQuantity,
                    product.Category!.Name)));
}
