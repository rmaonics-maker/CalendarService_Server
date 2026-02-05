namespace Calendar.Api.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";

    
    public void Validate()
    {
        if (string.IsNullOrEmpty(Host))
            throw new ArgumentNullException(nameof(Host), "RabbitMQ Host is not configured!");
    }
}