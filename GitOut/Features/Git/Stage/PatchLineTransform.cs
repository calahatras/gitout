namespace GitOut.Features.Git.Stage
{
    public struct PatchLineTransform
    {
        public static readonly PatchLineTransform None = new PatchLineTransform();

        private readonly bool trimLineEndings;
        private readonly bool convertToSpaces;

        private PatchLineTransform(
            bool trimLineEndings,
            bool convertToSpaces
        )
        {
            this.trimLineEndings = trimLineEndings;
            this.convertToSpaces = convertToSpaces;
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
                input = input.Replace('\t', ' ');
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

            public PatchLineTransform Build() => new PatchLineTransform(trimEndings, convertToSpaces);

            public IPatchLineTransformBuilder ConvertTabsToSpaces()
            {
                convertToSpaces = true;
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
