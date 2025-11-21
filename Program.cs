using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        var response = context.HttpContext.Response;
        response.ContentType = "application/json";
        var json = "{\"error\":\"TooManyRequests\",\"message\":\"Rate limit exceeded. Try again later.\"}";
        await response.WriteAsync(json, token);
    };

    options.AddPolicy("weather", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

builder.Services.AddMemoryCache();  

builder.Services.AddScoped<WeatherService>();
builder.Services.AddHttpClient<IWeatherProvider, VisualCrossingWeatherProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRateLimiter();

app.MapGet("/weather/{city}", async (string city, WeatherService weatherService) =>
{
    try
    {
        var response = await weatherService.GetCurrentWeatherAsync(city);
        return Results.Ok(response);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new
        {
            error = "Invalid Input",
            message = ex.Message
        });
    }
    catch (WeatherNotFoundException ex)
    {
        return Results.NotFound(new
        {
            error = "NotFound",
            message = ex.Message
        });
    }
    catch (Exception ex)
    {
        // Everything else: log and return 502/500 style error
        Console.WriteLine($"[ERROR] Unexpected error in /weather endpoint: {ex}");

        return Results.Problem(
            title: "UpstreamWeatherError",
            detail: "Failed to retrieve weather from external provider.",
            statusCode: 502
        );
    }

}).WithName("GetWeatherForCity").RequireRateLimiting("weather");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record WeatherResponse(string City, int TemperatureC, int TemperatureF, string Summary);
