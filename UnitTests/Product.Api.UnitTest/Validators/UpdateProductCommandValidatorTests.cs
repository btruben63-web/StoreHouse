using FluentValidation.TestHelper;
using Product.Application.Products.Commands.UpdateProduct;
using Product.Application.Products.DTOs;

namespace Product.Api.UnitTest.Validators;

public sealed class UpdateProductCommandValidatorTests
{
    private static readonly UpdateProductDTO ValidProduct = new("Martelo", null, 12.90m);

    private readonly UpdateProductCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_HasNoErrors() =>
        _validator.TestValidate(new UpdateProductCommand(100001, ValidProduct)).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData(99_999)]
    [InlineData(1_000_000)]
    public void IdOutsideRange_Fails(int id) =>
        _validator.TestValidate(new UpdateProductCommand(id, ValidProduct)).ShouldHaveValidationErrorFor(x => x.Id);

    [Theory]
    [InlineData(100_000)]
    [InlineData(999_999)]
    public void IdAtLimits_Passes(int id) =>
        _validator.TestValidate(new UpdateProductCommand(id, ValidProduct)).ShouldNotHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void EmptyName_Fails() =>
        _validator.TestValidate(new UpdateProductCommand(100001, ValidProduct with { Name = "" }))
            .ShouldHaveValidationErrorFor(x => x.Product.Name);

    [Fact]
    public void ZeroPrice_Fails() =>
        _validator.TestValidate(new UpdateProductCommand(100001, ValidProduct with { Price = 0m }))
            .ShouldHaveValidationErrorFor(x => x.Product.Price);

    [Fact]
    public void NullProduct_Fails() =>
        _validator.TestValidate(new UpdateProductCommand(100001, null!)).ShouldHaveValidationErrorFor(x => x.Product);
}
