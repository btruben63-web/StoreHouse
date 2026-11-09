using FluentValidation.TestHelper;
using Product.Application.Products.Commands.CreateProduct;
using Product.Application.Products.DTOs;

namespace Product.Api.UnitTest.Validators;

public sealed class CreateProductCommandValidatorTests
{
    private static readonly CreateProductDTO ValidProduct =
        new("Martelo de Carpinteiro", "Cabo de madeira.", 12.90m, 10);

    private readonly CreateProductCommandValidator _validator = new();

    public static TheoryData<decimal> InvalidPrices => new() { 0m, -1m, 0.001m, 100_000_000m };

    public static TheoryData<decimal> ValidPrices => new() { 0.01m, 99_999_999.99m };

    private static CreateProductCommand Command(CreateProductDTO product) => new(product, "Admin");

    [Fact]
    public void ValidCommand_HasNoErrors() =>
        _validator.TestValidate(Command(ValidProduct)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NullProduct_Fails() =>
        _validator.TestValidate(new CreateProductCommand(null!, "Admin")).ShouldHaveValidationErrorFor(x => x.Product);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_Fails(string name) =>
        _validator.TestValidate(Command(ValidProduct with { Name = name })).ShouldHaveValidationErrorFor(x => x.Product.Name);

    [Fact]
    public void NameAboveMaxLength_Fails() =>
        _validator.TestValidate(Command(ValidProduct with { Name = new string('a', 101) }))
            .ShouldHaveValidationErrorFor(x => x.Product.Name);

    [Fact]
    public void NameAtMaxLength_Passes() =>
        _validator.TestValidate(Command(ValidProduct with { Name = new string('a', 100) }))
            .ShouldNotHaveValidationErrorFor(x => x.Product.Name);

    [Fact]
    public void DescriptionAboveMaxLength_Fails() =>
        _validator.TestValidate(Command(ValidProduct with { Description = new string('a', 501) }))
            .ShouldHaveValidationErrorFor(x => x.Product.Description);

    [Fact]
    public void NullDescription_Passes() =>
        _validator.TestValidate(Command(ValidProduct with { Description = null }))
            .ShouldNotHaveValidationErrorFor(x => x.Product.Description);

    [Theory]
    [MemberData(nameof(InvalidPrices))]
    public void InvalidPrice_Fails(decimal price) =>
        _validator.TestValidate(Command(ValidProduct with { Price = price }))
            .ShouldHaveValidationErrorFor(x => x.Product.Price);

    [Theory]
    [MemberData(nameof(ValidPrices))]
    public void ValidPrice_Passes(decimal price) =>
        _validator.TestValidate(Command(ValidProduct with { Price = price }))
            .ShouldNotHaveValidationErrorFor(x => x.Product.Price);

    [Theory]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public void InvalidInitialStock_Fails(int initialStock) =>
        _validator.TestValidate(Command(ValidProduct with { InitialStock = initialStock }))
            .ShouldHaveValidationErrorFor(x => x.Product.InitialStock);

    [Theory]
    [InlineData(0)]
    [InlineData(1_000_000)]
    public void ValidInitialStock_Passes(int initialStock) =>
        _validator.TestValidate(Command(ValidProduct with { InitialStock = initialStock }))
            .ShouldNotHaveValidationErrorFor(x => x.Product.InitialStock);
}
