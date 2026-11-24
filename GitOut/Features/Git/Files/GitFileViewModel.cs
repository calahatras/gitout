using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using GitOut.Features.Git.Diff;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Files;

public class GitFileViewModel : IGitFileEntryViewModel, INotifyPropertyChanged
{
    private static readonly TimeSpan DefaultLoadTimeout = TimeSpan.FromSeconds(30);

    private readonly IGitRepository repository;
    private readonly GitFileId sourceId;
    private readonly GitFileId? destinationId;
    private readonly TimeSpan loadTimeout;
    private DiffOptions options;
    private CancellationTokenSource? loadingCancellationTokenSource;

    private GitFileViewModel(
        IGitRepository repository,
        RelativeDirectoryPath path,
        FileName fileName,
        string displayName,
        GitFileId sourceId,
        GitFileId? destinationId = null,
        GitDiffType diffType = GitDiffType.None,
        DiffOptions? options = null,
        TimeSpan? loadTimeout = null
    )
    {
        this.repository = repository;
        this.options = options ?? DiffOptions.Builder().Build();
        this.loadTimeout = loadTimeout ?? DefaultLoadTimeout;
        Path = path;
        FileName = fileName;
        DisplayName = displayName;
        this.sourceId = sourceId;
        this.destinationId = destinationId;
        Status = diffType;
        IconResourceKey = diffType switch
        {
            GitDiffType.None => "File",
            GitDiffType.Create => "FilePlus",
            GitDiffType.Delete => "FileRemove",
            GitDiffType.InPlaceEdit => "FileEdit",
            GitDiffType.RenameEdit => "FileMove",
            GitDiffType.CopyEdit => "FileReplace",
            _ => "FileHidden",
        };
    }

    public RelativeDirectoryPath Path { get; }
    public string RootPath => repository.WorkingDirectory.ToString();
    public string RelativePath => FullPath[(RootPath.Length + 1)..];
    public string RelativeDirectory =>
        System.IO.Path.Combine(
            RootPath,
            Path.ToString().Replace("\\", "/", StringComparison.InvariantCulture)
        );
    public string FullPath =>
        System
            .IO.Path.Combine(
                RootPath,
                Path.ToString(),
                System.IO.Path.GetFileName(FileName.ToString())
            )
            .Replace('\\', '/');
    public FileName FileName { get; }
    public GitDiffType Status { get; }
    public string DisplayName { get; }
    public string IconResourceKey { get; }

    // note: this viewmodel is used in tree view and as such requires IsExpanded property
    public bool IsExpanded { get; set; }

    public bool IsLoading { get; private set; }

    public DiffContext? DiffResult
    {
        get
        {
            if (field is null && !IsLoading)
            {
                _ = RefreshDiffAsync();
            }
            return field;
        }
        private set;
    }

    public void UpdateOptions(DiffOptions newOptions)
    {
        CancelLoad();
        options = newOptions;
        DiffResult = null;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiffResult)));
    }

    /// <summary>
    /// Cancels an in-progress diff or blob load, if any.
    /// </summary>
    public void CancelLoad() => loadingCancellationTokenSource?.Cancel();

    public event PropertyChangedEventHandler? PropertyChanged;

    public static GitFileViewModel Snapshot(
        IGitRepository repository,
        GitFileEntry file,
        RelativeDirectoryPath relativePath,
        DiffOptions? options = null,
        TimeSpan? loadTimeout = null
    ) =>
        file.Type != GitFileType.Blob
            ? throw new ArgumentException($"Invalid file type for blob {file.Type}", nameof(file))
            : new GitFileViewModel(
                repository,
                relativePath,
                file.FileName,
                file.FileName.ToString(),
                file.Id,
                null,
                GitDiffType.None,
                options,
                loadTimeout
            );

    public static GitFileViewModel Difference(
        IGitRepository repository,
        GitDiffFileEntry file,
        RelativeDirectoryPath relativePath,
        DiffOptions? options = null,
        TimeSpan? loadTimeout = null
    ) =>
        file.FileType != GitFileType.Blob
            ? throw new ArgumentException($"Invalid file type for blob {file.Type}", nameof(file))
            : new GitFileViewModel(
                repository,
                relativePath,
                file.Source.FileName,
                file.Source.FileName.ToString(),
                file.Source.Id,
                file.Destination.Id,
                file.Type,
                options,
                loadTimeout
            );

    public static GitFileViewModel RelativeDifference(
        IGitRepository repository,
        GitDiffFileEntry file,
        DiffOptions? options = null,
        TimeSpan? loadTimeout = null
    ) =>
        file.FileType != GitFileType.Blob
            ? throw new ArgumentException($"Invalid file type for blob {file.Type}", nameof(file))
            : new GitFileViewModel(
                repository,
                file.Source.Directory,
                file.Source.FileName,
                System
                    .IO.Path.Combine(
                        file.Source.Directory.ToString(),
                        file.Source.FileName.ToString()
                    )
                    .Replace('\\', '/'),
                file.Source.Id,
                file.Destination.Id,
                file.Type,
                options,
                loadTimeout
            );

    private async Task RefreshDiffAsync()
    {
        loadingCancellationTokenSource?.Cancel();
        var cts = new CancellationTokenSource(loadTimeout);
        loadingCancellationTokenSource = cts;
        IsLoading = true;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
        try
        {
            DiffResult = destinationId is null
                ? await DiffContext.SnapshotFileAsync(
                    repository,
                    Path,
                    FileName,
                    sourceId,
                    cts.Token
                )
                : await DiffContext.DiffFileAsync(
                    repository,
                    Path,
                    FileName,
                    sourceId,
                    destinationId,
                    options,
                    cts.Token
                );
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiffResult)));
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Loading timed out or was cancelled
        }
        finally
        {
            if (ReferenceEquals(loadingCancellationTokenSource, cts))
            {
                loadingCancellationTokenSource = null;
                IsLoading = false;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
            }
            cts.Dispose();
        }
    }
}
