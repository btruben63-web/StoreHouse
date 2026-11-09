using System.ComponentModel.DataAnnotations;

namespace App.IdentityServer.DTOs;

public sealed record LoginRequestDTO(
    [Required, StringLength(256)] string UserName,
    [Required, StringLength(128)] string Password);
