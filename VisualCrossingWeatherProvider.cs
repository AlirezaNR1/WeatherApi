using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

public class VisualCrossingWeatherProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public VisualCrossingWeatherProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        var section = configuration.GetSection("WeatherApi");

        _baseUrl = section["BaseUrl"] ?? "";
        _apiKey = section["ApiKey"] ?? "";

        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            Console.WriteLine("Warning: WeatherApi:BaseUrl is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            Console.WriteLine("Warning: WeatherApi:ApiKey is not configured. External API calls will fail until you set it.");
        }
    }

    public async Task<WeatherResponse?> GetCurrentWeatherAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl) || string.IsNullOrWhiteSpace(_apiKey))
        {
            // Config not set, we can't call the real API.
            // For now just return a fake value.
            return new WeatherResponse(
                City: city,
                TemperatureC: 25,
                TemperatureF: 77,
                Summary: "Sunny (config missing, fallback hardcoded)"
            );
        }

        // Build Visual Crossing URL:
        // Example timeline endpoint:
        // {baseUrl}/{city}?unitGroup=metric&key={apiKey}&contentType=json

        var url = $"{_baseUrl}/{Uri.EscapeDataString(city)}?unitGroup=metric&key={_apiKey}&contentType=json";

        Console.WriteLine($"Requesting weather from: {url}");

        HttpResponseMessage httpResponse;

        try
        {
            httpResponse = await _httpClient.GetAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling Visual Crossing API: {ex.Message}");
            // On network error, we return null to let the service handle it.
            return null;
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Visual Crossing returned status {(int)httpResponse.StatusCode} for city '{city}'.");
            return null;
        }

        // For NOW: just read the raw JSON and log it.
        var rawJson = await httpResponse.Content.ReadAsStringAsync();
        Console.WriteLine("Raw response from Visual Crossing:");
        Console.WriteLine(rawJson);

        // TODO: parse JSON properly and map to WeatherResponse.
        // For now, return a dummy response so the API works.
        return new WeatherResponse(
            City: city,
            TemperatureC: 20,
            TemperatureF: 68,
            Summary: "Dummy data from VisualCrossing (JSON not parsed yet)"
        );
    }
}
