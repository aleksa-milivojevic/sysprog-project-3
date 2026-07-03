public sealed class WeatherDataNotFound
{
    public static WeatherDataNotFound Instance { get; } = new();

    public WeatherDataNotFound() { }
}

public sealed class RequestOutOfBounds
{
    public static RequestOutOfBounds Instance { get; } = new();

    public RequestOutOfBounds() { }
}