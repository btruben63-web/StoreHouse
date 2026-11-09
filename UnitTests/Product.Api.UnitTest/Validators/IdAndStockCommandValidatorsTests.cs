using FluentValidation.TestHelper;
using Product.Application.Products.Commands.AddStockToProduct;
using Product.Application.Products.Commands.DecreaseStockToProduct;
using Product.Application.Products.Commands.DeleteProduct;

namespace Product.Api.UnitTest.Validators;

public sealed class IdAndStockCommandValidatorsTests
{
    private readonly DeleteProductCommandValidator _deleteValidator = new();
    private readonly AddStockToProductCommandValidator _addValidator = new();
    private readonly DecreaseStockToProductCommandValidator _decreaseValidator = new();

    [Theory]
    [InlineData(5)]
    [InlineData(99_999)]
    [InlineData(1_000_000)]
    public void InvalidId_Fails(int id)
    {
        _deleteValidator.TestValidate(new DeleteProductCommand(id, "Admin")).ShouldHaveValidationErrorFor(x => x.Id);
        _addValidator.TestValidate(new AddStockToProductCommand(id, 1, "Manager")).ShouldHaveValidationErrorFor(x => x.Id);
        _decreaseValidator.TestValidate(new DecreaseStockToProductCommand(id, 1, "Manager")).ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void ValidDelete_HasNoErrors() =>
        _deleteValidator.TestValidate(new DeleteProductCommand(100001, "Admin")).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1_000_001)]
    public void InvalidQuantity_Fails(int quantity)
    {
        _addValidator.TestValidate(new AddStockToProductCommand(100001, quantity, "Manager"))
            .ShouldHaveValidationErrorFor(x => x.Quantity);
        _decreaseValidator.TestValidate(new DecreaseStockToProductCommand(100001, quantity, "Manager"))
            .ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1_000_000)]
    public void ValidQuantity_Passes(int quantity)
    {
        _addValidator.TestValidate(new AddStockToProductCommand(100001, quantity, "Manager"))
            .ShouldNotHaveAnyValidationErrors();
        _decreaseValidator.TestValidate(new DecreaseStockToProductCommand(100001, quantity, "Manager"))
            .ShouldNotHaveAnyValidationErrors();
    }
}
