namespace RetailInventory.Api.Models;

public class ProductDetail
{
    public int ProductDetailId { get; set; }
    public string WarrantyInfo { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product? Product { get; set; }
}
