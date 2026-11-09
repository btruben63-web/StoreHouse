namespace Product.Application.Products.DTOs;

public sealed record ProductDTO(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity);
