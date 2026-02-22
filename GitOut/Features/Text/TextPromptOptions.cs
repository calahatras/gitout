using System;

namespace GitOut.Features.Text;

public sealed record TextPromptOptions(
    string? StartValue,
    string Prompt,
    Func<string, bool>? Validator,
    Func<string, object>? ResultConverter,
    string? Title = null,
    string? ConfirmButtonText = "OK"
);
