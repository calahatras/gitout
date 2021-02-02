using System.Windows;
using System.Windows.Input;

namespace GitOut.Features.Git.Details
{
    public partial class GitHistoryEventDetails
    {
        public static readonly DependencyProperty BranchTemplateProperty = DependencyProperty.Register(
            "BranchTemplate",
            typeof(DataTemplate),
            typeof(GitHistoryEventDetails)
        );

        public static readonly DependencyProperty GitHistoryEventProperty = DependencyProperty.Register(
            "GitHistoryEvent",
            typeof(GitHistoryEvent),
            typeof(GitHistoryEventDetails)
        );

        public static readonly DependencyProperty CopyHashCommandProperty = DependencyProperty.Register(
            "CopyHashCommand",
            typeof(ICommand),
            typeof(GitHistoryEventDetails)
        );

        public static readonly DependencyProperty GoToParentCommandProperty = DependencyProperty.Register(
            nameof(GoToParentCommand),
            typeof(ICommand),
            typeof(GitHistoryEventDetails)
        );

        public static readonly DependencyProperty GoToMergedParentCommandProperty = DependencyProperty.Register(
            nameof(GoToMergedParentCommand),
            typeof(ICommand),
            typeof(GitHistoryEventDetails)
        );

        public static readonly DependencyProperty CopyHashCommandParameterProperty = DependencyProperty.Register(
            "CopyHashCommandParameter",
            typeof(object),
            typeof(GitHistoryEventDetails)
        );

        public GitHistoryEventDetails()
        {
            InitializeComponent();
            BranchTemplate = (DataTemplate)Resources["DefaultBranchTemplate"];
        }

        public DataTemplate BranchTemplate
        {
            get => (DataTemplate)GetValue(BranchTemplateProperty);
            set => SetValue(BranchTemplateProperty, value);
        }

        public GitHistoryEvent GitHistoryEvent
        {
            get => (GitHistoryEvent)GetValue(GitHistoryEventProperty);
            set => SetValue(GitHistoryEventProperty, value);
        }

        public ICommand CopyHashCommand
        {
            get => (ICommand)GetValue(CopyHashCommandProperty);
            set => SetValue(CopyHashCommandProperty, value);
        }

        public ICommand GoToParentCommand
        {
            get { return (ICommand)GetValue(GoToParentCommandProperty); }
            set { SetValue(GoToParentCommandProperty, value); }
        }

        public ICommand GoToMergedParentCommand
        {
            get { return (ICommand)GetValue(GoToMergedParentCommandProperty); }
            set { SetValue(GoToMergedParentCommandProperty, value); }
        }

        public object CopyHashCommandParameter
        {
            get => GetValue(CopyHashCommandParameterProperty);
            set => SetValue(CopyHashCommandParameterProperty, value);
        }
    }
}
