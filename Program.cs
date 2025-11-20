var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

app.MapGet("/weather/{city}", async (string city, WeatherService weatherService) =>
{
    var response = await weatherService.GetCurrentWeatherAsync(city);
    return Results.Ok(response);
}).WithName("GetWeatherForCity");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record WeatherResponse(string City, int TemperatureC, int TemperatureF, string Summary);
