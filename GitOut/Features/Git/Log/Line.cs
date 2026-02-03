namespace GitOut.Features.Git.Log;

public readonly struct Line
{
    public Line(int up, int down) => (Up, Down) = (up, down);

    public int Up { get; }
    public int Down { get; }
}
