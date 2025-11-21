using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System;
public class WeatherService
{
    private readonly IWeatherProvider _weatherProvider;
    private readonly IMemoryCache _memoryCache;

    public WeatherService(IConfiguration configuration, IWeatherProvider weatherProvider, IMemoryCache memoryCache)
    {
        _weatherProvider = weatherProvider;
        _memoryCache = memoryCache;
    }

    public async Task<WeatherResponse> GetCurrentWeatherAsync(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("City is required.", nameof(city));
        }

        // Normalize city for cache key (case-insensitive)
        var normalizedCity = city.Trim().ToLowerInvariant();
        var cacheKey = $"weather:{normalizedCity}";

        // 1) Try cache first
        if (_memoryCache.TryGetValue(cacheKey, out WeatherResponse cached))
        { 
            Console.WriteLine($"[CACHE HIT] {cacheKey}");
            return cached;
        }
        Console.WriteLine($"[CACHE MISS] {cacheKey} – calling provider");

        //call provider
        var response = await _weatherProvider.GetCurrentWeatherAsync(city);

        if (response is null)
        {
            throw new WeatherNotFoundException(city);
        }

        // 3) Store in cache with TTL (e.g. 30 minutes or 12 hours)
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
        };

        _memoryCache.Set(cacheKey, response, cacheEntryOptions);

        return response;
    }


}

public class WeatherNotFoundException : Exception {
    public WeatherNotFoundException(string city)
        : base($"Weather data not found for city '{city}'.")
    { 
    }
}