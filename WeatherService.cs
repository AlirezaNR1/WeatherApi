public class WeatherService
{
    public WeatherResponse GetCurrentWeather(string city)
    {
        var temperatureC = 15;
        var temperatureF = 30;
        var summery = "cold AF";

        return new WeatherResponse(
            City: city,
            TemperatureC: temperatureC,
            TemperatureF: temperatureF,
            Summery: summery
        );
    }
}