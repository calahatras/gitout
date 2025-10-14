namespace GitOut.Features.Git
{
    public interface IGitHistoryEventBuilder<T>
        where T : GitHistoryEvent
    {
        IGitHistoryEventBuilder<T> BuildBody(string body);
        IGitHistoryEventBuilder<T> ParseHash(string hashes);
        IGitHistoryEventBuilder<T> ParseDate(long unixTime);
        IGitHistoryEventBuilder<T> ParseAuthorName(string authorName);
        IGitHistoryEventBuilder<T> ParseAuthorEmail(string authorEmail);
        IGitHistoryEventBuilder<T> ParseSubject(string subject);

        T Build();
    }
}
