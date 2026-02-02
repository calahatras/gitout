namespace GitOut.Features.Git;

public interface IAddOptionsBuilder
{
    AddOptions Build();
    IAddOptionsBuilder WithIntent();
}
