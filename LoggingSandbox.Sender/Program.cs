using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
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
                        tracing

                            // Use Jaeger
                            .AddOtlpExporter(o =>
                            {
                                o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                                o.Endpoint = new Uri("http://jaeger:4317");
                                o.ExportProcessorType = OpenTelemetry.ExportProcessorType.Simple;
                            })

                            // Or Zipkin
                            .AddZipkinExporter(o => o.Endpoint = new Uri("http://zipkin:9411/api/v2/spans"))
                            
                            .AddSource("*")
                            .SetErrorStatusOnException(true);
                    });

                _ = services.AddHostedService<TestBackgroundService>();

            });

            await builder.RunConsoleAsync();
        }
    }
}