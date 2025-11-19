using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
public class WeatherService
{
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly IWeatherProvider _weatherProvider;

    public WeatherService(IConfiguration configuration, IWeatherProvider weatherProvider)
    {
        _weatherProvider = weatherProvider;

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

    public async Task<WeatherResponse> GetCurrentWeatherAsync(string city)
    {
        var response = await _weatherProvider.GetCurrentWeatherAsync(city);

        if (response is null)
        {
            throw new Exception($"Weather data not found for city '{city}'.");
        }
        return response;
    }
}