using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;
using GitOut.Features.Diagnostics;
using GitOut.Features.Material.Snackbar;

namespace GitOut.Features.Settings;

public sealed class ProcessSettingsViewModel : IDisposable, INotifyPropertyChanged
{
    private readonly IDisposable streamSubscription;
    private readonly ObservableCollection<ProcessEventArgsViewModel> processEvents;
    private readonly object processEventsLock = new();

    public ProcessSettingsViewModel(IProcessTelemetryCollector telemetry, ISnackbarService snacks)
    {
        processEvents = new ObservableCollection<ProcessEventArgsViewModel>(
            telemetry.Events.Select(CreateViewModel)
        );
        streamSubscription = telemetry
            .EventsStream.Select(CreateViewModel)
            .Subscribe(item =>
            {
                lock (processEventsLock)
                {
                    processEvents.Add(item);
                }
            });

        BindingOperations.EnableCollectionSynchronization(processEvents, processEventsLock);
        Reports = CollectionViewSource.GetDefaultView(processEvents);
        Reports.Filter = FilterReport;

        ProcessEventArgsViewModel CreateViewModel(ProcessEventArgs model) => new(model, snacks);
    }

    public ICollectionView Reports { get; }

    public string? SearchText
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchText)));
                Reports.Refresh();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() => streamSubscription.Dispose();

    private bool FilterReport(object item) =>
        string.IsNullOrWhiteSpace(SearchText)
        || (item is ProcessEventArgsViewModel report
            && (
                report.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || report.Arguments.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            ));
}
