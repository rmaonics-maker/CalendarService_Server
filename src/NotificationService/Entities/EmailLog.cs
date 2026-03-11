namespace NotificationService.Entities;

public sealed class EmailLog
{
    public Guid Id {get;init;} = Guid.NewGuid();
    public Guid UserId {get;init;}
    public string EmailAddress {get;init;} = string.Empty;
    public string EmailType {get;init;} = string.Empty;
    public DateTimeOffset SentAt {get;init;}
}