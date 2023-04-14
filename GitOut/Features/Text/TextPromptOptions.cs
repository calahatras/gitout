using System;

namespace GitOut.Features.Text;

public record TextPromptOptions(
    string? StartValue,
    string Prompt,
    Func<string, bool>? Validator,
    Func<string, object>? ResultConverter
);
