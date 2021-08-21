using System.Windows.Documents;

namespace GitOut.Features.Text
{
    public interface ILineDecorator
    {
        void Decorate(TextElement paragraph, int lineNumber);
    }
}
