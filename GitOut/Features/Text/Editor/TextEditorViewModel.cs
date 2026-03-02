using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GitOut.Features.Git.Stage;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

namespace GitOut.Features.Text.Editor;

public class TextEditorViewModel : INotifyPropertyChanged
{
    private readonly ISnackbarService snack;

    private string textContent = string.Empty;
    private bool isWorking;
    private bool hasUnsavedChanges;

    public TextEditorViewModel(
        INavigationService navigation,
        ITitleService title,
        ISnackbarService snack
    )
    {
        this.snack = snack;
        TextEditorOptions options =
            navigation.GetOptions<TextEditorOptions>(typeof(TextEditorPage).FullName!)
            ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
        FilePath = options.FilePath.Replace('/', Path.DirectorySeparatorChar);

        title.Title = options.Title ?? Path.GetFileName(FilePath);

        SaveCommand = new AsyncCallbackCommand(
            SaveFileAsync,
            () => HasUnsavedChanges && !IsWorking
        );

        _ = LoadFileAsync();
    }

    public string FilePath { get; }

    public string TextContent
    {
        get => textContent;
        set
        {
            if (SetProperty(ref textContent, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => hasUnsavedChanges;
        private set => SetProperty(ref hasUnsavedChanges, value);
    }

    public bool IsWorking
    {
        get => isWorking;
        private set => SetProperty(ref isWorking, value);
    }

    public ICommand SaveCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task LoadFileAsync()
    {
        IsWorking = true;
        try
        {
            if (File.Exists(FilePath))
            {
                string text = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    textContent = text;
                    PropertyChanged?.Invoke(
                        this,
                        new PropertyChangedEventArgs(nameof(TextContent))
                    );
                    HasUnsavedChanges = false;
                });
            }
        }
        catch (Exception ex)
        {
            snack.ShowError($"Failed to load file: {ex.Message}", ex, TimeSpan.FromSeconds(5));
        }
        finally
        {
            IsWorking = false;
        }
    }

    private async Task SaveFileAsync()
    {
        IsWorking = true;
        try
        {
            await File.WriteAllTextAsync(FilePath, TextContent).ConfigureAwait(false);
            HasUnsavedChanges = false;
            snack.ShowSuccess("File saved successfully");
        }
        catch (Exception ex)
        {
            snack.ShowError($"Failed to save file: {ex.Message}", ex, TimeSpan.FromSeconds(5));
        }
        finally
        {
            IsWorking = false;
        }
    }

    private bool SetProperty<T>(ref T prop, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!ReferenceEquals(prop, value))
        {
            prop = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
        return false;
    }
}
