using System.Windows.Documents;

namespace GitOut.Features.Text
{
    public interface IDecoratedMatch
    {
        Run Apply(string line);
    }
}
