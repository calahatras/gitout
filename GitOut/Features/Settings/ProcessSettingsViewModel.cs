using System.Collections.Generic;
using System.Linq;
using GitOut.Features.Diagnostics;
using GitOut.Features.Material.Snackbar;

namespace GitOut.Features.Settings
{
    public class ProcessSettingsViewModel
    {
        public ProcessSettingsViewModel(
            IProcessTelemetryCollector telemetry,
            ISnackbarService snacks
        ) => Reports = telemetry.Events.Select(model => new ProcessEventArgsViewModel(model, snacks)).ToList();

        public IEnumerable<ProcessEventArgsViewModel> Reports { get; }
    }
}
