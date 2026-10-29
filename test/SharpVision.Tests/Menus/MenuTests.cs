using SharpVision.Events;
using SharpVision.Menus;
using SharpVision.Objects;
using SharpVision.Tests.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests.Menus;

public sealed class MenuTests
{
    private static TSubMenu FileMenu() =>
        new TSubMenu("~F~ile", kbAltF) +
            new TMenuItem("~O~pen", cmOpen, kbF3, param: "F3") +
            TMenuItem.NewLine() +
            new TMenuItem("E~x~it", cmQuit, kbAltX, param: "Alt-X");

    [Fact]
    public void Should_ChainItemsIntoSubmenus()
    {
        var menu = FileMenu() + (new TSubMenu("~E~dit", kbAltE) + new TMenuItem("~U~ndo", cmUndo, kbNoKey));

        Assert.Equal("~F~ile", menu.Name);
        Assert.Equal("~O~pen", menu.SubMenu!.Items!.Name);
        Assert.Null(menu.SubMenu.Items.Next!.Name);
        Assert.Equal("E~x~it", menu.SubMenu.Items.Next.Next!.Name);
        Assert.Same(menu.SubMenu.Items, menu.SubMenu.Deflt);
        Assert.Equal("~E~dit", menu.Next!.Name);
        Assert.Equal("~U~ndo", menu.Next.SubMenu!.Items!.Name);
    }

    [Fact]
    public void Should_DrawEntries_When_MenuBarInserted()
    {
        var screen = new TestScreen(20, 1);

        screen.Insert(new TMenuBar(new TRect(0, 0, 20, 1), FileMenu() + new TSubMenu("~H~elp", kbAltH)));

        Assert.Equal("  File  Help        ", screen.Row(0));
    }

    [Fact]
    public void Should_TurnShortcutIntoCommand_When_MenuBarGetsHotKey()
    {
        var screen = new TestScreen(20, 1);
        screen.Insert(new TMenuBar(new TRect(0, 0, 20, 1), FileMenu()));
        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = kbF3 } };

        screen.HandleEvent(ref ev);

        Assert.Equal(evNothing, ev.What);
        var put = Assert.Single(screen.PutBack);
        Assert.Equal(evCommand, put.What);
        Assert.Equal(cmOpen, put.Message.Command);
    }

    [Fact]
    public void Should_SizeMenuBoxToItems()
    {
        var box = new TMenuBox(new TRect(0, 0, 80, 25), new TMenu(FileMenu().SubMenu!.Items!), null);

        // The widest item is "Exit" (4) + 6 + "Alt-X" (5) + 2 = 17; 3 items + 2 frame lines high.
        Assert.Equal(new TRect(0, 0, 17, 5), box.GetBounds());
    }

    [Fact]
    public void Should_TurnShortcutIntoCommand_When_StatusLineGetsKey()
    {
        var status = new TStatusLine(new TRect(0, 0, 20, 1),
            new TStatusDef(0, 0xFFFF) + new TStatusItem("~Alt-X~ Exit", kbAltX, cmQuit));
        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = kbAltX } };

        status.HandleEvent(ref ev);

        Assert.Equal(evCommand, ev.What);
        Assert.Equal(cmQuit, ev.Message.Command);
    }

    [Fact]
    public void Should_DrawItemsAndHint()
    {
        var screen = new TestScreen(24, 1);

        screen.Insert(new HintedStatusLine(new TRect(0, 0, 24, 1),
            new TStatusDef(0, 0xFFFF) + new TStatusItem("~Alt-X~ Exit", kbAltX, cmQuit) + new TStatusItem(null, kbF10, cmMenu)));

        Assert.Equal(" Alt-X Exit │ Ready     ", screen.Row(0));
    }

    private sealed class HintedStatusLine(TRect bounds, TStatusDef defs) : TStatusLine(bounds, defs)
    {
        public override string Hint(ushort helpCtx) => "Ready";
    }
}
