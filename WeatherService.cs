using System.Threading.Tasks;
public class WeatherService
{
    private readonly IWeatherProvider _weatherProvider;

    public WeatherService(IConfiguration configuration, IWeatherProvider weatherProvider)
    {
        _weatherProvider = weatherProvider;
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