using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;

namespace GitOut.Features.Input
{
    public class MouseWheelGestureTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
            sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string str)
            {
                string[] parts = str.Split('+', StringSplitOptions.RemoveEmptyEntries);
                ModifierKeys modifier = ModifierKeys.None;
                foreach (string part in parts[..^1])
                {
                    modifier |= part.ToUpper() switch
                    {
                        "CTRL" => ModifierKeys.Control,
                        "ALT" => ModifierKeys.Alt,
                        "SHIFT" => ModifierKeys.Shift,
                        "WIN" => ModifierKeys.Windows,
                        _ => ModifierKeys.None
                    };
                }

                if (Enum.IsDefined(typeof(MouseWheelAction), parts[^1]))
                {
                    return new MouseWheelGesture(Enum.Parse<MouseWheelAction>(parts[^1]), modifier);
                }
                if (Enum.IsDefined(typeof(MouseAction), parts[^1]))
                {
                    return new MouseWheelGesture(Enum.Parse<MouseAction>(parts[^1]), modifier);
                }
                throw new NotSupportedException($"Unsupported MouseWheelAction {parts[..^1]}");
            }
            throw new NotSupportedException("value is not a string");
        }
    }
}
