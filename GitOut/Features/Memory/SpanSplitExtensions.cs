using System;
using System.Collections.Generic;

namespace GitOut.Features.Memory
{
    public static class SpanSplitExtensions
    {
        public static Range[] Split(this ReadOnlySpan<char> span) => Split(span, ' ');

        public static Range[] Split(this ReadOnlySpan<char> span, char separator) => Split(span, separator, StringSplitOptions.None);

        public static Range[] Split(this ReadOnlySpan<char> span, char separator, StringSplitOptions options)
        {
            var ranges = new List<Range>();
            int previousIndex = 0;
            int endPos = span.Length;
            for (int currentIndex = 0; currentIndex < endPos; ++currentIndex)
            {
                if (span[currentIndex] == separator)
                {
                    if (currentIndex - previousIndex > 1 || (options & StringSplitOptions.RemoveEmptyEntries) != StringSplitOptions.RemoveEmptyEntries)
                    {
                        int endIndex = currentIndex;
                        if ((options & StringSplitOptions.TrimEntries) == StringSplitOptions.TrimEntries)
                        {
                            while (span[previousIndex] == ' ')
                            {
                                ++previousIndex;
                            }
                            while (span[endIndex - 1] == ' ')
                            {
                                --endIndex;
                            }
                        }
                        ranges.Add(new Range(previousIndex, endIndex));
                    }
                    previousIndex = currentIndex + 1;
                }
            }
            if (endPos - previousIndex > 1 || (options & StringSplitOptions.RemoveEmptyEntries) != StringSplitOptions.RemoveEmptyEntries)
            {
                int lastEndIndex = endPos;
                if ((options & StringSplitOptions.TrimEntries) == StringSplitOptions.TrimEntries)
                {
                    while (span[previousIndex] == ' ')
                    {
                        ++previousIndex;
                    }
                    while (span[lastEndIndex - 1] == ' ')
                    {
                        --lastEndIndex;
                    }
                }
                ranges.Add(new Range(previousIndex, lastEndIndex));
            }
            return ranges.ToArray();
        }
    }
}
