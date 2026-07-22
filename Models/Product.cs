using System.ComponentModel.DataAnnotations;

namespace RetailInventory.Api.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public ProductDetail? ProductDetail { get; set; }
    public List<Tag> Tags { get; set; } = [];

    [ConcurrencyCheck]
    public string RowVersion { get; set; } = Guid.NewGuid().ToString("N");
}
