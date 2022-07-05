?using System;
using GitOut.Features.Storage;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Options
{
    public class OptionsWriter<T> : IOptionsWriter<T> where T : class, new()
    {
        private readonly IOptions<T> options;
        private readonly IWritableStorage storage;
        private readonly string section;

        public OptionsWriter(
            IOptions<T> options,
            IWritableStorage storage,
            string section
        )
        {
            this.options = options;
            this.storage = storage;
            this.section = section;
        }

        public void Update(Action<T> modifier)
        {
            T snapshot = options.Value;
            modifier(snapshot);
            storage.Write(section, snapshot);
        }
    }
}
