using System.Threading.Tasks;

public interface IWeatherProvider
{
    Task<WeatherResponse?> GetCurrentWeatherAsync(string city);
}