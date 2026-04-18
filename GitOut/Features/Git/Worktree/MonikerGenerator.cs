using System;

namespace GitOut.Features.Git.Worktree;

public static class MonikerGenerator
{
    private static readonly string[] Names = new[]
    {
        "andromeda",
        "orion",
        "pegasus",
        "lyra",
        "cygnus",
        "cassiopeia",
        "draco",
        "aquila",
        "centaurus",
        "vela",
        "carina",
        "puppis",
        "eridanus",
        "phoenix",
        "taurus",
        "gemini",
        "leo",
        "virgo",
        "scorpius",
        "sagittarius",
    };

    public static string Generate()
    {
        var random = new Random();
        return Names[random.Next(Names.Length)];
    }
}
