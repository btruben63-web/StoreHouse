using App.IdentityServer.Configuration;
using App.IdentityServer.Controllers;
using App.IdentityServer.Data;
using App.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("StoreHouseIdentity");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'StoreHouseIdentity' is not configured.");
}

var identityServerSection = builder.Configuration.GetSection(IdentityServerSettings.SectionName);
var identityServerSettings = identityServerSection.Get<IdentityServerSettings>()
    ?? throw new InvalidOperationException($"Configuration section '{IdentityServerSettings.SectionName}' is missing.");
identityServerSettings.Validate();
builder.Services.Configure<IdentityServerSettings>(identityServerSection);

builder.Services.AddDbContext<AppIdentityDbContext>(options => options.UseNpgsql(connectionString));
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services
    .AddIdentityServer(options =>
    {
        options.IssuerUri = identityServerSettings.IssuerUri;
        options.EmitStaticAudienceClaim = false;
        if (!string.IsNullOrWhiteSpace(identityServerSettings.KeyPath))
        {
            options.KeyManagement.KeyPath = identityServerSettings.KeyPath;
        }
    })
    .AddInMemoryApiScopes(IdentityServerConfig.GetApiScopes(identityServerSettings))
    .AddInMemoryApiResources(IdentityServerConfig.GetApiResources(identityServerSettings))
    .AddInMemoryClients(IdentityServerConfig.GetClients(identityServerSettings))
    .AddAspNetIdentity<IdentityUser>()
    .AddProfileService<DistinctClaimsProfileService>();

builder.Services.AddHttpClient(AuthController.TokenClientName);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "StoreHouse Identity", Version = "v1" }));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseIdentityServer();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
