using System.ComponentModel;
using System.Windows.Input;

namespace GitOut.Features.Input
{
    [TypeConverter(typeof(MouseWheelGestureTypeConverter))]
    public class MouseWheelGesture : MouseGesture
    {
        public MouseWheelGesture(MouseWheelAction mouseAction, ModifierKeys modifiers)
            : base(MouseAction.None, modifiers) => Action = mouseAction;

        public MouseWheelGesture(MouseAction mouseAction, ModifierKeys modifiers)
            : base(mouseAction, modifiers) { }

        public bool Matches(MouseWheelEventArgs e) =>
            e.Delta > 0 && Action == MouseWheelAction.MouseWheelUp
            || e.Delta < 0 && Action == MouseWheelAction.MouseWheelDown;

        public MouseWheelAction Action { get; } = MouseWheelAction.None;
    }
}
