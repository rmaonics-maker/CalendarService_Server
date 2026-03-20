namespace Calendar.Booking.API.Options;

public class BookingRabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";
    public bool SslEnabled {get; set;}
    public int Port {get; set;}
    
    public void Validate()
    {
        if (string.IsNullOrEmpty(Host))
            throw new ArgumentNullException(nameof(Host), "RabbitMQ Host is not configured!");
    }
}