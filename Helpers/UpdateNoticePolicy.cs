namespace Schedule2._0.Helpers;

public static class UpdateNoticePolicy
{
    public static bool ShouldShow(bool wasVersion20User, bool version21UpdateNotesSeen) =>
        wasVersion20User && !version21UpdateNotesSeen;
}
