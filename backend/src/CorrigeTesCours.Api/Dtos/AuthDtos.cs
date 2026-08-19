using System.ComponentModel.DataAnnotations;
using CorrigeTesCours.Domain.Entities;

namespace CorrigeTesCours.Api.Dtos;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [Required, MinLength(2), MaxLength(50)] string Pseudo,
    [Required] NiveauScolaire Level);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

/// <summary>Le refresh token n'apparaît pas ici : il transite par un cookie HttpOnly.</summary>
public record AuthResponse(string AccessToken, int ExpiresInSeconds, UserResponse User);

public record UserResponse(Guid Id, string Email, string Pseudo, NiveauScolaire Level, DateTime CreatedAt)
{
    public static UserResponse From(User u) => new(u.Id, u.Email, u.Pseudo, u.Level, u.CreatedAt);
}
