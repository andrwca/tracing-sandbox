using LoggingSandbox.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace LoggingSandbox.Listener
{
    internal class TestBackgroundService : BackgroundService
    {
        private readonly ILogger<TestBackgroundService> _logger;

        private static readonly ActivitySource ActivitySource = new(typeof(TestBackgroundService).FullName);

        private static readonly Meter Meter = new Meter("message_meter", "0.0.1");
        private static readonly Counter<int> MessagedReceivedCounter = Meter.CreateCounter<int>("messages_received");

        public TestBackgroundService(ILogger<TestBackgroundService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var client = new UdpClient(5000);

            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await client.ReceiveAsync();
                MessagedReceivedCounter.Add(1);

                var message = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(result.Buffer));
                var activityId = ActivityTraceId.CreateFromString(message.TraceId);
                ActivityContext parentContext = new ActivityContext(activityId, ActivitySpanId.CreateFromString(message.SpanId), ActivityTraceFlags.Recorded);

                using (var activity = ActivitySource.StartActivity("ProcessingMessage", ActivityKind.Internal, parentContext))
                {
                    activity?.SetTag("payload", message.Payload);

                    await UpdateMessageAsync();
                    await ForwardMessageAsync();

                    activity?.SetStatus(ActivityStatusCode.Ok);
                }

                _logger.LogInformation($"This is a test from the listener, activity ID {activityId}.");
            }
        }
        private async Task UpdateMessageAsync()
        {
            using var activity = ActivitySource.StartActivity("UpdatingMessage");

            //throw new Exception("Something nasty happened.");

            await Task.Delay(1000);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        private async Task ForwardMessageAsync()
        {
            using var activity = ActivitySource.StartActivity("ForwardingMessage");

            await Task.Delay(1000);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
    }
}