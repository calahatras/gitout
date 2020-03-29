using System.Windows.Input;

namespace GitOut.Features.Menu
{
    public class MenuItem
    {
        public bool IsDivider => Name == null;
        public bool IsHeader => Command == null;
        public bool IsItem => !IsHeader && !IsDivider;

        public string? Name { get; set; }
        public string? Icon { get; set; }

        public ICommand? Command { get; set; }
    }
}
