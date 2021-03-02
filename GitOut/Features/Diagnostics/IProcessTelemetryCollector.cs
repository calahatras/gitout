using System.Collections.Generic;

namespace GitOut.Features.Diagnostics
{
    public interface IProcessTelemetryCollector
    {
        IReadOnlyCollection<ProcessEventArgs> Events { get; }

        void Report(ProcessEventArgs args);
    }
}
