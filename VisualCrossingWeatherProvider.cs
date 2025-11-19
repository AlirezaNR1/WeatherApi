using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Collections.Generic;

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
            // Config not set → fallback
            return new WeatherResponse(
                City: city,
                TemperatureC: 25,
                TemperatureF: 77,
                Summary: "Sunny (config missing, fallback hardcoded)"
            );
        }

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
            return null;
        }

        if (!httpResponse.IsSuccessStatusCode)
        {
            Console.WriteLine($"Visual Crossing returned status {(int)httpResponse.StatusCode} for city '{city}'.");
            return null;
        }

        var rawJson = await httpResponse.Content.ReadAsStringAsync();

        //saving the json file
        using var jsonDoc = JsonDocument.Parse(rawJson);
        var saveOptions = new JsonSerializerOptions { WriteIndented = true };
        string prettyJson = JsonSerializer.Serialize(jsonDoc.RootElement, saveOptions);

        // Save to file
        string filePath = "visualcrossing_response.json";
        await File.WriteAllTextAsync(filePath, prettyJson);

        Console.WriteLine($"✅ JSON saved to {filePath}");

        // --- NEW: parse the JSON into our DTO ---

        VisualCrossingResponseDto? data;
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            data = JsonSerializer.Deserialize<VisualCrossingResponseDto>(rawJson, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deserialize Visual Crossing JSON: {ex.Message}");
            return null;
        }

        if (data is null)
        {
            Console.WriteLine("Deserialized Visual Crossing response is null.");
            return null;
        }

        // Pick temperature (Celsius) and conditions:
        // 1) Prefer currentConditions.temp, 2) fall back to first day.temp
        double? tempCSource = data.CurrentConditions?.Temp
                              ?? data.Days?.FirstOrDefault()?.Temp;

        if (tempCSource is null)
        {
            Console.WriteLine("Could not find temp in Visual Crossing response.");
            return null;
        }

        int tempC = (int)Math.Round(tempCSource.Value);

        // Compute Fahrenheit from Celsius (like in your WeatherForecast)
        int tempF = 32 + (int)(tempC / 0.5556);

        string summary = data.CurrentConditions?.Conditions
                          ?? data.Days?.FirstOrDefault()?.Conditions
                          ?? "No description";

        string resolvedCity = data.ResolvedAddress ?? city;

        return new WeatherResponse(
            City: resolvedCity,
            TemperatureC: tempC,
            TemperatureF: tempF,
            Summary: summary
        );
    }
}

// Internal DTOs for deserializing Visual Crossing response
file class VisualCrossingResponseDto
{
    public string? ResolvedAddress { get; set; }
    public CurrentConditionsDto? CurrentConditions { get; set; }
    public DayDto[]? Days { get; set; }
}

file class CurrentConditionsDto
{
    public double? Temp { get; set; }
    public string? Conditions { get; set; }
}

file class DayDto
{
    public string? Datetime { get; set; }
    public double? Temp { get; set; }
    public string? Conditions { get; set; }
}