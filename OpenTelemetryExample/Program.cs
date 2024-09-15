using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetryExample;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddOpenTelemetry(builder);

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        MapEndpoints(app);

        app.Run();
    }

    private static IServiceCollection AddOpenTelemetry(this IServiceCollection services, WebApplicationBuilder builder)
    {
        var otel = services.AddOpenTelemetry();
        otel.ConfigureResource(resource =>
        {
            resource.AddService(builder.Environment.ApplicationName,
                serviceNamespace: "OpenTelemetryExample");
        });

        otel.WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                .AddOtlpExporter()
                .AddConsoleExporter();
        });

        otel.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter()
                .AddConsoleExporter();
        });

        otel.WithLogging(logging =>
        {
        });

        return services;
    }

    private static void MapEndpoints(WebApplication app)
    {
        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };
        app.MapGet("/weatherforecast", () =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                        new WeatherForecast
                        (
                            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            Random.Shared.Next(-20, 55),
                            summaries[Random.Shared.Next(summaries.Length)]
                        ))
                    .ToArray();
                return forecast;
            })
            .WithName("GetWeatherForecast")
            .WithOpenApi();
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int) (TemperatureC / 0.5556);
}
