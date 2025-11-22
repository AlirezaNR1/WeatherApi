# WeatherApi

WeatherApi is a small ASP.NET Core **minimal API** that exposes a `/weather/{city}` endpoint.
It fetches live weather data from the **Visual Crossing** Weather API, applies **in-memory caching**,
and enforces **per-IP rate limiting** to avoid abuse.

The project is designed as a learning playground for:

- Minimal APIs in ASP.NET Core
- Dependency Injection (services, interfaces, HttpClient)
- Consuming 3rd-party APIs with `HttpClient`
- Configuration & secrets (`appsettings.json` + .NET user-secrets)
- In-memory caching with `IMemoryCache`
- Rate limiting with the built-in ASP.NET Core RateLimiter
- Error handling and mapping domain errors to proper HTTP status codes


## Tech Stack

- **Runtime:** .NET (minimal API template)
- **Framework:** ASP.NET Core Minimal API
- **Language:** C#
- **HTTP Client:** `HttpClient` via `AddHttpClient`
- **Config & Secrets:** `appsettings.json` + `.NET user-secrets`
- **Caching:** `IMemoryCache` (in-process)
- **Rate Limiting:** `AddRateLimiter` / `UseRateLimiter` (fixed-window, per-IP)
- **3rd-party API:** Visual Crossing Weather API

## High-Level Architecture

`HTTP request`  
→ Minimal API endpoint (`/weather/{city}`)  
→ `WeatherService` (business logic + caching + exceptions)  
→ `IWeatherProvider` (abstraction)  
→ `VisualCrossingWeatherProvider` (implementation using `HttpClient`)  
→ Visual Crossing API

### Main Components

- **WeatherResponse**  
  Public record returned to API clients:
  ```csharp
  public record WeatherResponse(string City, int TemperatureC, int TemperatureF, string Summary);
  ```
- **WeatherService**
  - Validates input.
  - Handles caching with IMemoryCache.
  - Calls IWeatherProvider when cache misses.
  - Throws WeatherNotFoundException if no data is available.

- **IWeatherProvider**
  Interface representing "something that can fetch weather for a city":
  ```csharp
  public interface IWeatherProvider
  {
    Task<WeatherResponse?> GetCurrentWeatherAsync(string city);
  }
  ```

---


## Getting Started

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/en-us/download) 
- A **Visual Crossing** Weather API key  
  (sign up at their website and get an API key)

### Clone & Restore

```bash
git clone https://github.com/AlirezaNR1/WeatherApi
cd WeatherApi
dotnet restore
```

### Configuration

  - Base configuration lives in appsettings.json:
  ```json
  "WeatherApi": {
  "BaseUrl": "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline",
  "ApiKey": ""
  }
  ```
  The API key is not checked into source control.
  Instead, it is provided via .NET user-secrets:
  ```bash
  cd WeatherApi

  # Initialize user-secrets for this project (if not done already)
  dotnet user-secrets init

  # Set the Visual Crossing API key
  dotnet user-secrets set "WeatherApi:ApiKey" "<YOUR_API_KEY_HERE>"
  ```

  You can verify the secret is set with:
  ```bash
  dotnet user-secrets list
  ```
---

## Running the API

From the project directory:

```bash
dotnet run
```

By default the API will listen on http://localhost:<port> (Kestrel).
The exact port may vary; check the console output.


---
## Caching

The API uses `IMemoryCache` in `WeatherService` to avoid hitting the external API on every request.

- **Cache key:** `weather:{city.ToLowerInvariant()}`
- **TTL (Time To Live):** 12 hours (`AbsoluteExpirationRelativeToNow`)
- **Behavior:**
  - First request for a city → **cache miss**, calls `IWeatherProvider`, stores result.
  - Subsequent requests within TTL → **cache hit**, returns cached `WeatherResponse`.
  - After TTL → entry expires and next request will hit the provider again.

Caching lives in the **service layer**, not in the provider.

## Rate Limiting

Rate limiting is applied to `/weather/{city}` using ASP.NET Core’s `RateLimiter` middleware.

- **Policy name:** `"weather"`
- **Type:** Fixed window
- **Partition key:** Client IP (`RemoteIpAddress`)
- **Limit:** 15 requests per minute per IP
- **Queue:** Disabled (`QueueLimit = 0`)

When the limit is exceeded, the API returns:

- **Status:** `429 Too Many Requests`
- **Body:**

```json
{
  "error": "TooManyRequests",
  "message": "Rate limit exceeded. Try again later."
}
```

## Error Handling

The /weather/{city} handler catches domain exceptions and converts them to HTTP status codes:
- 400 Bad Request
  - Thrown when the input city is invalid (e.g. null/whitespace).
  - Response body:
    ```json
    {
      "error": "InvalidRequest",
      "message": "City is required."
    }
    ```
- 404 Not Found
  - Thrown as WeatherNotFoundException when no weather data is available for the requested city.
  - Response body:
  ```json
  {
    "error": "NotFound",
    "message": "Weather data not found for city 'XYZ'."
  }
  ```
- 502 Bad Gateway
  - For unexpected errors / upstream failures from Visual Crossing.
  - Uses Results.Problem(...) with title "UpstreamWeatherError".

This separation makes it clear for clients whether the error is:
  - Their fault (bad request),
  - Data-related (city not found),
  - Or an upstream/external failure.

--- 
## Project Structure (logical)

- `Program.cs`
  - Minimal API setup
  - DI registration (`WeatherService`, `IWeatherProvider`, `HttpClient`, `IMemoryCache`, RateLimiter)
  - Endpoint definitions (`/weather/{city}`, `/weatherforecast`)
  - OpenAPI wiring for development

- `WeatherResponse.cs` (or record in Program.cs)
  - DTO returned to API clients

- `WeatherService.cs`
  - Business logic layer
  - Caching with `IMemoryCache`
  - Throws `WeatherNotFoundException` when no data

- `IWeatherProvider.cs`
  - Abstraction for weather providers

- `VisualCrossingWeatherProvider.cs`
  - `HttpClient` + `IConfiguration`
  - Calls Visual Crossing API
  - DTOs for external JSON (`VisualCrossingResponseDto`, etc.)
  - Maps external data → `WeatherResponse`

## Possible Future Improvements

- Add unit tests for `WeatherService` using a fake `IWeatherProvider`.
- Add unit/integration tests for `VisualCrossingWeatherProvider` using a mocked `HttpClient`.
- Add more endpoints (e.g. daily forecast, historical data) reusing the same service/provider pattern.

