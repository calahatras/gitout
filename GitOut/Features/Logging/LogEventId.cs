using Microsoft.Extensions.Logging;

namespace GitOut.Features.Logging
{
    public static class LogEventId
    {
        public static readonly EventId Application = new(1, "Application");
        public static readonly EventId Navigation = new(2, "Navigation");

        public static readonly EventId Unhandled = new(500, "Unhandled_exception");
    }
}
