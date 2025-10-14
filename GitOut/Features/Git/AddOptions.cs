using System.Collections.Generic;

namespace GitOut.Features.Git
{
    public class AddOptions
    {
        private AddOptions(bool intentToAdd) => IntentToAdd = intentToAdd;

        public bool IntentToAdd { get; }

        public IEnumerable<string> BuildArguments()
        {
            if (IntentToAdd)
            {
                yield return "--intent-to-add";
            }
        }

        public static IAddOptionsBuilder Builder() => new AddOptionsBuilder();

        public static AddOptions None => new(false);

        private class AddOptionsBuilder : IAddOptionsBuilder
        {
            private bool intentToAdd;

            public AddOptions Build() => new(intentToAdd);

            public IAddOptionsBuilder WithIntent()
            {
                intentToAdd = true;
                return this;
            }
        }
    }
}
