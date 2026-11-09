namespace Product.Domain.Entities;

public class Stock
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime UpdatedAt { get; set; }
    public uint Version { get; set; }
    public Product? Product { get; set; }
}
