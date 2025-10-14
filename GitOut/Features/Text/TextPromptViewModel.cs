using System;
using System.Windows.Input;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

namespace GitOut.Features.Text;

public sealed class TextPromptViewModel
{
    private readonly TextPromptOptions options;

    public TextPromptViewModel(INavigationService navigation)
    {
        options =
            navigation.GetOptions<TextPromptOptions>(typeof(TextPromptPage).FullName!)
            ?? throw new ArgumentNullException(nameof(options), "Options may not be null");
        CancelCommand = new CallbackCommand(navigation.Close);
        SetResultCommand = new NotNullCallbackCommand<string>(
            input => navigation.Close(options.ResultConverter?.Invoke(input) ?? input),
            input => options.Validator?.Invoke(input) ?? true
        );
    }

    public string? Prompt => options.Prompt;
    public string? StartValue => options.StartValue;

    public ICommand CancelCommand { get; }
    public ICommand SetResultCommand { get; }
}
