namespace LoggingSandbox.Models
{
    public class Message
    {
        public string TraceId { get; set; }
        public string SpanId { get; set; }
        public string Payload { get; set; }
    }
}