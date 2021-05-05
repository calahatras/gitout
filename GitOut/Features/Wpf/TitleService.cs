using System;

namespace GitOut.Features.Wpf
{
    public class TitleService : ITitleService
    {
        private string? title;
        public string? Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public event EventHandler<TitleChangedEventArgs>? TitleChanged;

        private void SetProperty(ref string? prop, string? value)
        {
            if (!ReferenceEquals(prop, value))
            {
                prop = value;
                TitleChanged?.Invoke(this, new TitleChangedEventArgs(value));
            }
        }
    }
}
