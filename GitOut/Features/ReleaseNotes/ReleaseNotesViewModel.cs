using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GitOut.Features.Git;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

namespace GitOut.Features.ReleaseNotes
{
    public class ReleaseNotesViewModel : INotifyPropertyChanged
    {
        private readonly IGitRepository repository;

        private string markdownText = string.Empty;
        private string commit1 = "head~6";
        private string commit2 = "head";

        public ReleaseNotesViewModel(
            INavigationService navigation,
            ITitleService title
        )
        {
            GitRepositoryOptions options = navigation.GetOptions<GitRepositoryOptions>(typeof(ReleaseNotesPage).FullName!)
                ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
            repository = options.Repository;
            title.Title = $"{repository.Name} (Release Notes)";
            _ = UpdateReleaseNotesAsync();
        }

        public string MarkdownText
        {
            get => markdownText;
            set => SetProperty(ref markdownText, value);
        }

        public string Commit1
        {
            get => commit1;
            set
            {
                if (SetProperty(ref commit1, value))
                {
                    _ = UpdateReleaseNotesAsync();
                }
            }
        }

        public string Commit2
        {
            get => commit2;
            set
            {
                if (SetProperty(ref commit2, value))
                {
                    _ = UpdateReleaseNotesAsync();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async Task UpdateReleaseNotesAsync()
        {
            IEnumerable<GitHistoryEvent>? entries = (await repository
                .LogAsync(new LogOptions
                {
                    IncludeStashes = false,
                    RevisionRange = new[] { Commit1, Commit2 }
                })
            )
                .ToList();
            var markdownBuilder = new StringBuilder();
            foreach (GitHistoryEvent item in entries.Where(s => !string.IsNullOrEmpty(s.Body)))
            {
                markdownBuilder.AppendLine(item.Body.ToString());
            }

            IEnumerable<IGrouping<string, GitHistoryEvent>> scopeGroups = entries
                .GroupBy(item =>
                {
                    int scopeStartIndex = item.Subject.IndexOf('(', StringComparison.InvariantCulture);
                    if (scopeStartIndex == -1)
                    {
                        return string.Empty;
                    }
                    int scopeEndIndex = item.Subject.IndexOf(')', StringComparison.InvariantCulture);
                    if (scopeEndIndex == -1)
                    {
                        return string.Empty;
                    }
                    string scope = item.Subject.Substring(scopeStartIndex + 1, scopeEndIndex - scopeStartIndex - 1);
                    return scope;
                })
                .OrderByDescending(g => g.Key);
            const int typeStartIndex = 0;
            foreach (IGrouping<string, GitHistoryEvent> item in scopeGroups)
            {
                markdownBuilder.AppendLine();
                markdownBuilder.AppendLine($"**{item.Key}**");
                markdownBuilder.AppendLine();
                markdownBuilder.AppendLine("| Commit | Type | Description |");
                markdownBuilder.AppendLine("|--|--|--|");
                foreach (GitHistoryEvent entry in item)
                {
                    int scopeStartIndex = entry.Subject.IndexOf('(', StringComparison.InvariantCulture);
                    int scopeEndIndex = entry.Subject.IndexOf(')', StringComparison.InvariantCulture);
                    string? type = entry.Subject.Substring(typeStartIndex, scopeStartIndex - typeStartIndex);
                    string? description = entry.Subject.Substring(scopeEndIndex + 2).Trim();
                    markdownBuilder.AppendLine($"| {entry.Id} | {type} | {description} |");
                }
            }
            MarkdownText = markdownBuilder.ToString();
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
}
