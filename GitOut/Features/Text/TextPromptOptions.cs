using System;

namespace GitOut.Features.Text;

public sealed record TextPromptOptions(
    string? StartValue,
    string Prompt,
    Func<string, bool>? Validator,
    Func<string, object>? ResultConverter
);
