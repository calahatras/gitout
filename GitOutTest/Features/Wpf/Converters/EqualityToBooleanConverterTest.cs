using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NUnit.Framework;

namespace GitOut.Features.Wpf.Converters;

public class EqualityToBooleanConverterTest
{
#pragma warning disable CS8618
    private EqualityToBooleanConverter converter;
#pragma warning restore CS8618

    [SetUp]
    [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(converter))]
    public void Setup() => converter = new EqualityToBooleanConverter();

    [Test]
    public void ConvertShouldReturnTrueWhenStringsAreEqual()
    {
        object result = converter.Convert(
            ["C:\\repos", "C:\\repos"],
            typeof(bool),
            null!,
            CultureInfo.InvariantCulture
        );

        Assert.That(result, Is.True);
    }

    [Test]
    public void ConvertShouldReturnFalseWhenStringsAreDifferent()
    {
        object result = converter.Convert(
            ["C:\\repos\\a", "C:\\repos\\b"],
            typeof(bool),
            null!,
            CultureInfo.InvariantCulture
        );

        Assert.That(result, Is.False);
    }

    [Test]
    public void ConvertShouldReturnFalseWhenValueIsNull()
    {
        object result = converter.Convert(
            ["C:\\repos", null!],
            typeof(bool),
            null!,
            CultureInfo.InvariantCulture
        );

        Assert.That(result, Is.False);
    }

    [Test]
    public void ConvertShouldReturnFalseWhenValueIsUnset()
    {
        object result = converter.Convert(
            ["C:\\repos", DependencyProperty.UnsetValue],
            typeof(bool),
            null!,
            CultureInfo.InvariantCulture
        );

        Assert.That(result, Is.False);
    }

    [Test]
    public void ConvertBackShouldReturnNullForSecondBindingWhenFalse()
    {
        object[] result = converter.ConvertBack(
            false,
            [typeof(string), typeof(string)],
            null!,
            CultureInfo.InvariantCulture
        );

        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(Binding.DoNothing));
        Assert.That(result[1], Is.Null);
    }

    [Test]
    public void ConvertBackShouldReturnDoNothingWhenTrue()
    {
        object[] result = converter.ConvertBack(
            true,
            [typeof(string), typeof(string)],
            null!,
            CultureInfo.InvariantCulture
        );

        Assert.That(result, Has.Length.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(Binding.DoNothing));
        Assert.That(result[1], Is.EqualTo(Binding.DoNothing));
    }
}
