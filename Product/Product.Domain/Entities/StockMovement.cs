using Product.Domain.Enums;

namespace Product.Domain.Entities;

public class StockMovement
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; }
    public int QuantityAfter { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public Product? Product { get; set; }
}
