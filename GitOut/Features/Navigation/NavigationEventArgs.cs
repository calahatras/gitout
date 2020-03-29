using System;
using System.Windows.Controls;

namespace GitOut.Features.Navigation
{
    public class NavigationEventArgs : EventArgs
    {
        public NavigationEventArgs(ContentControl control) => Control = control;

        public ContentControl Control { get; }
    }
}
