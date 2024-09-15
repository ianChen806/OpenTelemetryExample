using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
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

        var meter = new Meter("My.Otel.Example", "1.0.0");
        builder.Services.AddOpenTelemetry(builder, meter);

        var app = builder.Build();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        MapEndpoints(app);
        MapOtelEndpoints(app, meter);

        app.Run();
    }

    private static void MapOtelEndpoints(WebApplication app, Meter meter)
    {
        var counter = meter.CreateCounter<int>("call.count", description: "call count");
        app.MapGet("/metrics", () =>
        {
            counter.Add(1);
            return "Hello World!";
        });
    }

    private static IServiceCollection AddOpenTelemetry(
        this IServiceCollection services,
        WebApplicationBuilder builder,
        Meter meter)
    {
        var otel = services.AddOpenTelemetry();
        otel.ConfigureResource(resource =>
        {
            resource.AddService(builder.Environment.ApplicationName,
                serviceNamespace: "OpenTelemetryExample");
        });

        otel.WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(meter.Name)
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                .AddOtlpExporter()
                .AddConsoleExporter();
        });

        otel.WithTracing(tracing =>
        {
            tracing.AddXRayTraceId()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddAWSInstrumentation()
                .AddOtlpExporter()
                .AddConsoleExporter();
        });

        otel.WithLogging(logging =>
        {
        });

        Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());

        return services;
    }

    private static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/weatherforecast", () =>
            {
                var summaries = new[]
                {
                    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
                };
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

        app.MapPost("/exception", () =>
        {
            throw new Exception(Guid.NewGuid().ToString());
        });
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int) (TemperatureC / 0.5556);
}
