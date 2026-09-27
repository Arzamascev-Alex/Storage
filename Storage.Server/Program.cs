using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Storage.Core;

namespace Storage.Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                    .AddService(ServerTelemetry.Name))
                .AddSource(ServerTelemetry.Name)
                .AddConsoleExporter()
                .Build();

            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                    .AddService(ServerTelemetry.Name))
                .AddMeter(ServerTelemetry.Name)
                .AddConsoleExporter()
                .Build();

            using var store = new SimpleStore();

            TcpServer server = new(store);

            await server.StartAsync();
        }
    }
}
