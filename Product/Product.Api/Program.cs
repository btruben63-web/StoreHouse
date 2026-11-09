using Product.Api.Authentication;
using Product.Api.ErrorHandling;
using Product.Api.Extensions;
using Product.Api.Swagger;
using Product.Application;
using Product.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("StoreHouse")
    ?? throw new InvalidOperationException("Connection string 'StoreHouse' is not configured.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ResultExtensions.InvalidModelStateResponse);
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsExtensions.Customize);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerWithJwt();

var app = builder.Build();

await app.Services.MigrateProductDatabaseAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
