using System.Collections.Generic;

namespace GitOut.Features.Diagnostics
{
    public class ProcessTelemetryCollector : IProcessTelemetryCollector
    {
        private readonly List<ProcessEventArgs> events = new List<ProcessEventArgs>();

        public IReadOnlyCollection<ProcessEventArgs> Events => events.AsReadOnly();

        public void Report(ProcessEventArgs args) => events.Add(args);
    }
}
