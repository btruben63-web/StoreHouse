using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Product.Api.Authorization;
using Product.Api.Extensions;
using Product.Application.Common.DTOs;
using Product.Application.Products.Commands.AddStockToProduct;
using Product.Application.Products.Commands.CreateProduct;
using Product.Application.Products.Commands.DecreaseStockToProduct;
using Product.Application.Products.Commands.DeleteProduct;
using Product.Application.Products.Commands.UpdateProduct;
using Product.Application.Products.DTOs;
using Product.Application.Products.Queries.GetProductById;
using Product.Application.Products.Queries.GetProducts;
using Product.Application.Products.Queries.SearchProductsByName;
using Product.Application.Products.Queries.SearchProductsByStockRange;
using Product.Domain.Constants;

namespace Product.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResultDTO<ProductDTO>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> GetProducts(
        int? lastItemId,
        int pageSize = ProductConstraints.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductsQuery(lastItemId, pageSize), cancellationToken);
        return result.ToActionResult(this, page => Ok(page));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDTO>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetProductById(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return result.ToActionResult(this, product => Ok(product));
    }

    [HttpPost]
    [Authorize(Roles = AuthorizationRoles.Admin)]
    [ProducesResponseType<ProductDTO>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDTO product, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateProductCommand(product, User.Identity?.Name), cancellationToken);
        return result.ToActionResult(this, created => CreatedAtAction(nameof(GetProductById), new { id = created.Id }, created));
    }

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType<ProductDTO>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDTO product, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateProductCommand(id, product), cancellationToken);
        return result.ToActionResult(this, updated => Ok(updated));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AuthorizationRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteProductCommand(id, User.Identity?.Name), cancellationToken);
        return result.ToActionResult(this, NoContent);
    }

    [HttpPost("{id:int}/decrement-stock/{quantity:int}")]
    [Authorize]
    [ProducesResponseType<StockDTO>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> DecreaseStockToProduct(int id, int quantity, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DecreaseStockToProductCommand(id, quantity, User.Identity?.Name), cancellationToken);
        return result.ToActionResult(this, stock => Ok(stock));
    }

    [HttpPost("{id:int}/add-to-stock/{quantity:int}")]
    [Authorize]
    [ProducesResponseType<StockDTO>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> AddStockToProduct(int id, int quantity, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddStockToProductCommand(id, quantity, User.Identity?.Name), cancellationToken);
        return result.ToActionResult(this, stock => Ok(stock));
    }

    [HttpGet("search")]
    [ProducesResponseType<PagedResultDTO<ProductDTO>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> SearchProductsByName(
        string name,
        int? lastItemId,
        int pageSize = ProductConstraints.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SearchProductsByNameQuery(name, lastItemId, pageSize), cancellationToken);
        return result.ToActionResult(this, page => Ok(page));
    }

    [HttpGet("stock-level")]
    [ProducesResponseType<PagedResultDTO<ProductDTO>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> SearchProductsByStockRange(
        [BindRequired] int min,
        [BindRequired] int max,
        int? lastItemId,
        int pageSize = ProductConstraints.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new SearchProductsByStockRangeQuery(min, max, lastItemId, pageSize), cancellationToken);
        return result.ToActionResult(this, page => Ok(page));
    }
}
