namespace RetailInventory.Api.Dtos;

public record CategoryDto(int Id, string Name, int ProductCount);

public record ProductListDto(
    int Id,
    string Name,
    decimal Price,
    int StockQuantity,
    string CategoryName,
    string RowVersion,
    IReadOnlyList<string> Tags);

public record ProductDetailDto(
    int Id,
    string Name,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    string CategoryName,
    string? WarrantyInfo,
    string RowVersion,
    IReadOnlyList<string> Tags);

public record CreateProductDto(
    string Name,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    string? WarrantyInfo,
    IReadOnlyList<int>? TagIds);

public record UpdateProductDto(
    string Name,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    string? WarrantyInfo,
    IReadOnlyList<int>? TagIds,
    string RowVersion);

public record StockAdjustmentDto(int Quantity, string RowVersion);

public record ReportProductDto(string Name, decimal Price, int StockQuantity, string CategoryName);
