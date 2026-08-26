using Schedule2._0.Helpers;
using Xunit;

namespace Schedule2._0.Tests;

public class UpdateNoticePolicyTests
{
    [Fact]
    public void UpgradeFromVersion20_ShowsUpdateNotesOnce()
    {
        Assert.True(UpdateNoticePolicy.ShouldShow(wasVersion20User: true, version21UpdateNotesSeen: false));
        Assert.False(UpdateNoticePolicy.ShouldShow(wasVersion20User: true, version21UpdateNotesSeen: true));
    }

    [Fact]
    public void FreshInstall_DoesNotShowUpdateNotes()
    {
        Assert.False(UpdateNoticePolicy.ShouldShow(wasVersion20User: false, version21UpdateNotesSeen: false));
    }
}
