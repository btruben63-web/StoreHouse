namespace App.IdentityServer.DTOs;

public sealed record TokenResponseDTO(string AccessToken, string TokenType, int ExpiresIn);
