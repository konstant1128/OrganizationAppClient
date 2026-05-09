namespace OrganizationAppClient.Models;

public abstract class ClientException : Exception
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string ServiceName { get; set; } = "OrganizationApi";
    
    protected ClientException(string message) : base(message) { }
    protected ClientException(string message, Exception inner) : base(message, inner) { }
}

public class NetworkException : ClientException
{
    public NetworkException(string message, Exception inner) : base(message, inner) { }
}

public class TimeoutException : ClientException
{
    public TimeSpan TimeoutDuration { get; }
    public TimeoutException(TimeSpan timeout, string serviceName = "OrganizationApi") 
        : base($"Таймаут при обращении к {serviceName} ({timeout.TotalSeconds} сек)")
    {
        TimeoutDuration = timeout;
        ServiceName = serviceName;
    }
}

public class ServiceUnavailableException : ClientException
{
    public int StatusCode { get; }
    public ServiceUnavailableException(int statusCode, string serviceName = "OrganizationApi")
        : base($"Сервис {serviceName} недоступен (код {statusCode})")
    {
        StatusCode = statusCode;
        ServiceName = serviceName;
    }
}

public class ValidationException : ClientException
{
    public Dictionary<string, string[]> Errors { get; }
    public ValidationException(Dictionary<string, string[]> errors) 
        : base("Ошибка валидации данных")
    {
        Errors = errors;
    }
}