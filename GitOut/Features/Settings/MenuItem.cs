using System.Windows.Input;

namespace GitOut.Features.Settings
{
    public class MenuItem
    {
        public bool IsDivider => Name == null;
        public bool IsHeader => Command == null;
        public bool IsItem => !IsHeader && !IsDivider;

        public string? Name { get; set; }
        public string? IconResourceKey { get; set; }

        public ICommand? Command { get; set; }
    }
}
