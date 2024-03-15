using Microsoft.Extensions.Logging;

namespace LoggingSandbox.Sender
{
    public partial class Logger
    {
        [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Executing method `{MethodName}`")]
        public static partial void LogExecutingMethod(ILogger logger, string methodName);
    }
}
