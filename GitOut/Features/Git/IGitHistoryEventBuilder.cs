namespace GitOut.Features.Git
{
    public interface IGitHistoryEventBuilder
    {
        IGitHistoryEventBuilder BuildBody(string body);
        IGitHistoryEventBuilder ParseHash(string hashes);
        IGitHistoryEventBuilder ParseDate(long unixTime);
        IGitHistoryEventBuilder ParseAuthorName(string authorName);
        IGitHistoryEventBuilder ParseAuthorEmail(string authorEmail);
        IGitHistoryEventBuilder ParseSubject(string subject);

        GitHistoryEvent Build();
    }
}
