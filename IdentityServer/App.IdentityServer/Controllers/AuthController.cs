using App.IdentityServer.Configuration;
using App.IdentityServer.DTOs;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace App.IdentityServer.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(
    IHttpClientFactory httpClientFactory,
    IOptions<IdentityServerSettings> options,
    ILogger<AuthController> logger) : ControllerBase
{
    public const string TokenClientName = "IdentityServerTokenClient";

    [HttpPost("login")]
    [ProducesResponseType<TokenResponseDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var httpClient = httpClientFactory.CreateClient(TokenClientName);

        using var tokenRequest = new PasswordTokenRequest
        {
            Address = settings.TokenEndpoint,
            ClientId = settings.ClientId,
            ClientSecret = settings.ClientSecret,
            Scope = settings.ApiScope,
            UserName = request.UserName,
            Password = request.Password
        };

        using var tokenResponse = await httpClient.RequestPasswordTokenAsync(tokenRequest, cancellationToken);

        if (!tokenResponse.IsError)
        {
            return Ok(new TokenResponseDTO(tokenResponse.AccessToken!, tokenResponse.TokenType!, tokenResponse.ExpiresIn));
        }

        if (tokenResponse.Error == OidcConstants.TokenErrors.InvalidGrant)
        {
            logger.LogWarning("Failed login attempt for user {UserName}", request.UserName);
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                detail: "The user name or password is incorrect.");
        }

        throw new InvalidOperationException(
            $"Token endpoint returned '{tokenResponse.Error}': {tokenResponse.ErrorDescription}");
    }
}
