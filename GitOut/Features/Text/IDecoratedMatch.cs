using System.Collections.Generic;
using System.Windows.Documents;

namespace GitOut.Features.Text;

public interface IDecoratedMatch
{
    IEnumerable<Run> Apply(string line);
}
