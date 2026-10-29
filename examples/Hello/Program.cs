// Ported from hello.cpp.

using SharpVision.App;
using SharpVision.Events;
using SharpVision.Menus;
using SharpVision.Objects;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

var helloWorld = new HelloApp();
helloWorld.Run();
TObject.Destroy(helloWorld);

internal sealed class HelloApp : TApplication
{
    private const ushort GreetThemCmd = 100;

    public HelloApp()
        : base(InitStatusLine, InitMenuBar, InitDeskTop)
    {
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evCommand && ev.Message.Command == GreetThemCmd)
        {
            GreetingBox();
            ClearEvent(ref ev);
        }
    }

    private static void GreetingBox()
    {
        // TBD: show the "Hello, World!" dialog once TDialog, TButton and TStaticText are ported.
    }

    public static new TMenuBar? InitMenuBar(TRect r)
    {
        r.B.Y = r.A.Y + 1;
        return new TMenuBar(r,
            new TSubMenu("~H~ello", kbAltH) +
                new TMenuItem("~G~reeting...", GreetThemCmd, kbAltG) +
                TMenuItem.NewLine() +
                // UPSTREAM: hello.cpp passes cmQuit as the item's key (i.e. Ctrl+A).
                new TMenuItem("E~x~it", cmQuit, cmQuit, hcNoContext, "Alt-X"));
    }

    public static new TStatusLine? InitStatusLine(TRect r)
    {
        r.A.Y = r.B.Y - 1;
        return new TStatusLine(r,
            new TStatusDef(0, 0xFFFF) +
                new TStatusItem("~Alt-X~ Exit", kbAltX, cmQuit) +
                new TStatusItem(null, kbF10, cmMenu));
    }
}
