using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using GitOut.Features.Navigation;
using GitOut.Features.Settings;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Ignore;

public class GitIgnoreViewModel : INotifyPropertyChanged
{
    private readonly IGitIgnoreService ignoreService;
    private readonly IGitRepository repository;
    private string checkPathText = string.Empty;
    private string? checkResult;
    private GitIgnoreRule? highlightedRule;

    public GitIgnoreViewModel(
        INavigationService navigation,
        IGitIgnoreService ignoreService,
        IOptionsMonitor<GitGeneralOptions> options
    )
    {
        this.ignoreService = ignoreService;

        GitIgnorePageOptions value =
            navigation.GetOptions<GitIgnorePageOptions>(typeof(GitIgnorePage).FullName!)
            ?? throw new ArgumentNullException(nameof(options), "Options may not be null");

        this.repository = value.Repository;

        Rules = new ObservableCollection<GitIgnoreRule>();
        CheckPathCommand = new AsyncCallbackCommand(CheckPathAsync);
        CopyPatternCommand = new CallbackCommand<GitIgnoreRule>(rule =>
        {
            if (rule is not null)
            {
                Wpf.Commands.Application.Copy?.Execute(rule.Pattern);
            }
        });
        OpenIgnoreFileCommand = new CallbackCommand(() =>
        {
            string path = Path.Combine(repository.WorkingDirectory.ToString(), ".gitignore");
            if (!File.Exists(path))
            {
                return;
            }

            string? editor = options.CurrentValue.DefaultEditorPath;
            if (!string.IsNullOrEmpty(editor))
            {
                Process.Start(editor, path);
            }
            else
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
        });

        _ = LoadRulesAsync();
    }

    public ObservableCollection<GitIgnoreRule> Rules { get; }

    public string CheckPathText
    {
        get => checkPathText;
        set
        {
            if (SetProperty(ref checkPathText, value))
            {
                CheckResult = null;
                HighlightedRule = null;
            }
        }
    }

    public string? CheckResult
    {
        get => checkResult;
        private set => SetProperty(ref checkResult, value);
    }

    public GitIgnoreRule? HighlightedRule
    {
        get => highlightedRule;
        set => SetProperty(ref highlightedRule, value);
    }

    public ICommand CheckPathCommand { get; }
    public ICommand CopyPatternCommand { get; }
    public ICommand OpenIgnoreFileCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async Task LoadRulesAsync()
    {
        Rules.Clear();
        System.Collections.Generic.IReadOnlyList<GitIgnoreRule> rules =
            await ignoreService.GetRulesAsync(repository);
        foreach (GitIgnoreRule rule in rules)
        {
            Rules.Add(rule);
        }
    }

    private async Task CheckPathAsync()
    {
        if (string.IsNullOrWhiteSpace(CheckPathText))
        {
            CheckResult = "Please enter a path";
            return;
        }

        GitCheckIgnoreResult? result = await repository.CheckIgnoreAsync(CheckPathText);
        if (result is null)
        {
            CheckResult = "Tracked (Not Ignored)";
            HighlightedRule = null;
        }
        else
        {
            CheckResult = $"Ignored by: {result.Pattern} (Line {result.LineNumber})";

            GitIgnoreRule? rule = Rules.FirstOrDefault(r =>
                r.LineNumber == result.LineNumber && r.Pattern.Trim() == result.Pattern.Trim()
            );
            rule ??= Rules.FirstOrDefault(r => r.LineNumber == result.LineNumber);

            HighlightedRule = rule;
        }
    }

    private bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null
    )
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
