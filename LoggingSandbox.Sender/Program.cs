using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace LoggingSandbox.Sender
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureServices((hostContext, services) =>
            {
                _ = services.AddLogging(b =>
                {
                    _ = b.ClearProviders();
                    _ = b.AddOpenTelemetry(o =>
                    {
                        o.IncludeScopes = true;
                        o.IncludeFormattedMessage = true;
                        o.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri("http://otel_collector:4317");
                        });
                        var resourceBuilder = ResourceBuilder
                            .CreateDefault()
                            .AddService(Assembly.GetExecutingAssembly().GetName().Name);

                        o.SetResourceBuilder(resourceBuilder);
                        o.AddConsoleExporter();
                    });
                });

                services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource
                    .AddService(serviceName: Assembly.GetExecutingAssembly().GetName().Name))
                    .WithTracing(tracing =>
                    {
                        tracing.AddOtlpExporter(o =>
                            {
                                o.Endpoint = new Uri("http://otel_collector:4317");
                                o.ExportProcessorType = ExportProcessorType.Simple;
                            })

                            .AddSource("*")
                            .SetErrorStatusOnException(true);
                    })
                    .WithMetrics(metrics =>
                    {
                        metrics.AddOtlpExporter(o =>
                        {
                            o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                            o.Endpoint = new Uri("http://otel_collector:4317");
                            o.ExportProcessorType = ExportProcessorType.Simple;
                        })
                        .AddMeter("*");
                    });

                _ = services.AddHostedService<TestBackgroundService>();

            });

            await builder.RunConsoleAsync();
        }
    }
}