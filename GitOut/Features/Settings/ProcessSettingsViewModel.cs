using System.Collections.Generic;
using GitOut.Features.Diagnostics;

namespace GitOut.Features.Settings
{
    public class ProcessSettingsViewModel
    {
        public ProcessSettingsViewModel(
            IProcessTelemetryCollector telemetry
        ) => Reports = telemetry.Events;

        public IEnumerable<ProcessEventArgs> Reports { get; }
    }
}
