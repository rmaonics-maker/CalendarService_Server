namespace Calendar.Contracts.Types;
/// <summary>
/// Обёртка для email (Value Object)
/// </summary>
public sealed record Email(string Value)
{
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email format", nameof(email));

        return new Email(email);
    }
};