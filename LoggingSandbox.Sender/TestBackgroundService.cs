﻿using LoggingSandbox.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace LoggingSandbox.Sender
{
    internal class TestBackgroundService : BackgroundService
    {
        private readonly ILogger<TestBackgroundService> _logger;

        private static readonly ActivitySource ActivitySource = new(typeof(TestBackgroundService).FullName);


        private static readonly Meter Meter = new Meter("message_meter", "0.0.1");
        private static readonly Counter<int> MessagesSentCounter = Meter.CreateCounter<int>("messages_sent");

        public TestBackgroundService(ILogger<TestBackgroundService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(5000);

            using var udpClient = new UdpClient();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("This is a test.");

                using (var activity = ActivitySource.StartActivity("SendingMessage"))
                {
                    var message = new Message()
                    {
                        Payload = "Hello",
                        TraceId = activity.TraceId.ToString(),
                        SpanId = activity.SpanId.ToString()
                    };

                    udpClient.Send(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)), "loggingsandboxlistener", 5000);

                    MessagesSentCounter.Add(1);

                    activity?.SetStatus(ActivityStatusCode.Ok, "Successfully sent message");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }
    }
}