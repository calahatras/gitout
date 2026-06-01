using System;
using System.Windows.Controls;
using GitOut.Features.Input;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.RepositoryList;

public partial class RepositoryListPage : UserControl
{
    public RepositoryListPage(
        RepositoryListViewModel dataContext,
        IOptionsMonitor<KeyboardShortcutsOptions> shortcutsOptions
    )
    {
        InitializeComponent();
        DataContext = dataContext;
        new KeyboardShortcutsAdornerController(
            this,
            shortcutsOptions,
            Array.Empty<KeyboardShortcutEntry>()
        );
    }
}
