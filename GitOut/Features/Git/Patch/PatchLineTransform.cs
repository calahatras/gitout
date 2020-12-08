using GitOut.Features.Text;

namespace GitOut.Features.Git.Patch
{
    public struct PatchLineTransform : ITextTransform
    {
        public static readonly PatchLineTransform None = new PatchLineTransform();

        private readonly bool trimLineEndings;
        private readonly bool convertToSpaces;
        private readonly string? tabReplacement;

        private PatchLineTransform(
            bool trimLineEndings,
            bool convertToSpaces,
            string? tabReplacement
        )
        {
            this.trimLineEndings = trimLineEndings;
            this.convertToSpaces = convertToSpaces;
            this.tabReplacement = tabReplacement;
        }

        public override bool Equals(object? obj) =>
            obj is PatchLineTransform opts
            && trimLineEndings == opts.trimLineEndings
            && convertToSpaces == opts.convertToSpaces;

        public override int GetHashCode() =>
            (trimLineEndings ? 1 : 0) +
            (convertToSpaces ? 2 : 0);

        public string Transform(string input)
        {
            if (trimLineEndings)
            {
                input = input.TrimEnd();
            }
            if (convertToSpaces)
            {
                input = input.Replace("\t", tabReplacement);
            }
            return input;
        }

        public static bool operator ==(PatchLineTransform left, PatchLineTransform right) => left.Equals(right);

        public static bool operator !=(PatchLineTransform left, PatchLineTransform right) => !(left == right);

        public static IPatchLineTransformBuilder Builder() => new PatchLineTransformBuilder();

        private class PatchLineTransformBuilder : IPatchLineTransformBuilder
        {
            private bool trimEndings;
            private bool convertToSpaces;
            private string? tabReplacement;

            public ITextTransform Build() => new PatchLineTransform(trimEndings, convertToSpaces, tabReplacement);

            public IPatchLineTransformBuilder ConvertTabsToSpaces(string replacement)
            {
                convertToSpaces = true;
                tabReplacement = replacement;
                return this;
            }

            public IPatchLineTransformBuilder TrimLines()
            {
                trimEndings = true;
                return this;
            }
        }
    }
}
