namespace Calendar_Service.Contracts
{
    public sealed record CreateUserRequest(string Name, string Email, decimal Balance);
}
