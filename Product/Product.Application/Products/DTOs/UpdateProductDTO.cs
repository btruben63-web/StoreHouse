using System.ComponentModel.DataAnnotations;
using Product.Domain.Constants;

namespace Product.Application.Products.DTOs;

public sealed record UpdateProductDTO(
    [Required, StringLength(ProductConstraints.NameMaxLength)] string Name,
    [StringLength(ProductConstraints.DescriptionMaxLength)] string? Description,
    [Range(0.01, 99_999_999.99)] decimal Price);
