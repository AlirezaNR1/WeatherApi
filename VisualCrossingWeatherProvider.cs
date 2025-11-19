using System.Threading.Tasks;

public class VisualCrossingWeatherProvider : IWeatherProvider
{
    public Task<WeatherResponse?> GetCurrentWeatherAsync(string city)
    {
        // For now this is just fake/hardcoded data.
        // Later we'll call the real Visual Crossing API here.

        var temperatureC = 25;
        var temperatureF = 77;
        var summary = "Sunny (from VisualCrossingWeatherProvider, hardcoded)";

        WeatherResponse response = new WeatherResponse(
            City: city,
            TemperatureC: temperatureC,
            TemperatureF: temperatureF,
            Summary: summary
        );

        // Wrap the result in a completed Task, same as before.
        return Task.FromResult<WeatherResponse?>(response);
    }
}
