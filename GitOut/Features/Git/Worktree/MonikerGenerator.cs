using System;

namespace GitOut.Features.Git.Worktree;

public static class MonikerGenerator
{
    private static readonly string[] Names = new[]
    {
        "Andromeda",
        "Orion",
        "Pegasus",
        "Lyra",
        "Cygnus",
        "Cassiopeia",
        "Draco",
        "Aquila",
        "Centaurus",
        "Vela",
        "Carina",
        "Puppis",
        "Eridanus",
        "Phoenix",
        "Taurus",
        "Gemini",
        "Leo",
        "Virgo",
        "Scorpius",
        "Sagittarius"
    };

    public static string Generate()
    {
        var random = new Random();
#pragma warning disable CA1308 // Normalize strings to uppercase
        return Names[random.Next(Names.Length)].ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
    }
}
