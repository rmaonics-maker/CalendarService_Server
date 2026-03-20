namespace Calendar.Contracts.Dtos;
/// <summary>
/// DTO для credentials пользователя
/// Используется только внутри Identity API (auth)
/// Не передаётся между сервисами!
/// </summary>
public sealed record UserCredentialDto(
    Guid UserId,
    string Email,
    string PasswordHash,
    DateTime CreatedAt
);