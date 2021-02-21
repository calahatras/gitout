using System.Windows.Documents;

namespace GitOut.Features.Text
{
    public interface ILineDecorator
    {
        void Decorate(Paragraph paragraph, int lineNumber);
    }
}
