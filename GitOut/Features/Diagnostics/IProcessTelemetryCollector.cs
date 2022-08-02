using System;
using System.Collections.Generic;

namespace GitOut.Features.Diagnostics
{
    public interface IProcessTelemetryCollector
    {
        IReadOnlyCollection<ProcessEventArgs> Events { get; }
        IObservable<ProcessEventArgs> EventsStream { get; }

        void Report(ProcessEventArgs args);
    }
}
