using System;
using NUnit.Framework;

namespace GitOut.Features.Memory;

public class SpanSplitExtensionsTest
{
    [Test]
    public void SplitShouldSplitSimple()
    {
        ReadOnlySpan<char> span = "a b".AsSpan();
        Range[] result = span.Split();
        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(0..1));
        Assert.That(result[1], Is.EqualTo(2..3));
        Assert.That(span[result[0]].ToString(), Is.EqualTo("a"));
        Assert.That(span[result[1]].ToString(), Is.EqualTo("b"));
    }

    [Test]
    public void SplitShouldSplitMultiple()
    {
        ReadOnlySpan<char> span = "a b c d e".AsSpan();
        Range[] result = span.Split();
        Assert.That(result.Length, Is.EqualTo(5));
        Assert.That(result[0], Is.EqualTo(0..1));
        Assert.That(result[1], Is.EqualTo(2..3));
        Assert.That(result[2], Is.EqualTo(4..5));
        Assert.That(result[3], Is.EqualTo(6..7));
        Assert.That(result[4], Is.EqualTo(8..9));
        Assert.That(span[result[0]].ToString(), Is.EqualTo("a"));
        Assert.That(span[result[1]].ToString(), Is.EqualTo("b"));
        Assert.That(span[result[2]].ToString(), Is.EqualTo("c"));
        Assert.That(span[result[3]].ToString(), Is.EqualTo("d"));
        Assert.That(span[result[4]].ToString(), Is.EqualTo("e"));
    }

    [Test]
    public void SplitShouldTrimValues()
    {
        ReadOnlySpan<char> span = " a , b ".AsSpan();
        Range[] result = span.Split(',', StringSplitOptions.TrimEntries);
        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(1..2));
        Assert.That(result[1], Is.EqualTo(5..6));
        Assert.That(span[result[0]].ToString(), Is.EqualTo("a"));
        Assert.That(span[result[1]].ToString(), Is.EqualTo("b"));
    }

    [Test]
    public void SplitShouldRemoveEmpty()
    {
        ReadOnlySpan<char> span = ", a ,, b ,".AsSpan();
        Range[] result = span.Split(',', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(result.Length, Is.EqualTo(2));
        Assert.That(result[0], Is.EqualTo(1..4));
        Assert.That(result[1], Is.EqualTo(6..9));
        Assert.That(span[result[0]].ToString(), Is.EqualTo(" a "));
        Assert.That(span[result[1]].ToString(), Is.EqualTo(" b "));
    }
}
