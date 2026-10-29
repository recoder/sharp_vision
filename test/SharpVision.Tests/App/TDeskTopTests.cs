using SharpVision.App;
using SharpVision.Objects;
using SharpVision.Views;
using Xunit;

using static SharpVision.Views.ViewFlags;

namespace SharpVision.Tests.App;

public sealed class TDeskTopTests
{
    private static TView Window(TDeskTop desk)
    {
        var v = new TView(new TRect(0, 0, 5, 5)) { Options = ofTileable | ofSelectable };
        desk.Insert(v);
        return v;
    }

    [Fact]
    public void Should_FillWithBackground()
    {
        var desk = new TDeskTop(new TRect(0, 0, 10, 5));

        Assert.NotNull(desk.Background);
        Assert.Equal(TDeskTop.DefaultBkgrnd, desk.Background!.Pattern);
        Assert.Equal(desk.GetExtent(), desk.Background.GetBounds());
    }

    [Fact]
    public void Should_TileWindowsInRowsFirst()
    {
        var desk = new TDeskTop(new TRect(0, 0, 40, 10), null);
        var a = Window(desk);
        var b = Window(desk);

        desk.Tile(desk.GetExtent());

        // Without TileColumnsFirst, two windows are stacked in one column.
        Assert.Equal(new TRect(0, 0, 40, 5), a.GetBounds());
        Assert.Equal(new TRect(0, 5, 40, 10), b.GetBounds());
    }

    [Fact]
    public void Should_CascadeWindows()
    {
        var desk = new TDeskTop(new TRect(0, 0, 40, 10), null);
        var a = Window(desk);
        var b = Window(desk);

        desk.Cascade(desk.GetExtent());

        Assert.Equal(new TRect(0, 0, 40, 10), a.GetBounds());
        Assert.Equal(new TRect(1, 1, 40, 10), b.GetBounds());
    }
}
