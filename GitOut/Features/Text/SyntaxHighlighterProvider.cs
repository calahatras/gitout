using System.IO;

namespace GitOut.Features.Text;

public static class SyntaxHighlighterProvider
{
    public static ISyntaxHighlighter GetHighlighter(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToUpperInvariant();
        return extension switch
        {
            ".CS" => new CSharpSyntaxHighlighter(),
            _ => new PlainTextSyntaxHighlighter(),
        };
    }
}
