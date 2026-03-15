namespace Calendar.Contracts
{
    public sealed record CreateUserRequest(string Name, string Email, decimal Balance);
}
