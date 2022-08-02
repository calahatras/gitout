using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace GitOut.Features.Diagnostics
{
    public class ProcessTelemetryCollector : IProcessTelemetryCollector
    {
        private readonly List<ProcessEventArgs> events = new();
        private readonly Subject<ProcessEventArgs> eventsStream = new();

        public IReadOnlyCollection<ProcessEventArgs> Events => events.AsReadOnly();

        public IObservable<ProcessEventArgs> EventsStream => eventsStream;

        public void Report(ProcessEventArgs args)
        {
            events.Add(args);
            eventsStream.OnNext(args);
        }
    }
}
