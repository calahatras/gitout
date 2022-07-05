using System;

namespace GitOut.Features.Options
{
    public interface IOptionsWriter<T>
    {
        void Update(Action<T> modifier);
    }
}
