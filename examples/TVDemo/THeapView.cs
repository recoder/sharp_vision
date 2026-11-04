using System;
using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Views;

/// <summary>Shows the memory in use, in the bottom-right corner.</summary>
/// Ported from examples/tvdemo/gadgets.h, gadgets.cpp (class THeapView).
internal sealed class THeapView : TView
{
    private uint _oldMem;
    private uint _newMem;
    private string _heapStr = "";

    public THeapView(TRect r) : base(r)
    {
        _oldMem = 0;
        _newMem = HeapSize();
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var c = GetColor(2);

        buf.MoveChar(0, ' ', c, Size.X);
        buf.MoveStr(0, _heapStr, c);
        WriteLine(0, 0, Size.X, 1, buf);
    }

    public void Update()
    {
        if ((_newMem = HeapSize()) != _oldMem)
        {
            _oldMem = _newMem;
            DrawView();
        }
    }

    // Upstream shows the bytes in use by malloc (glibc) or the private bytes (Windows);
    // the managed heap is the counterpart here.
    public uint HeapSize()
    {
        var total = (uint)GC.GetTotalMemory(false);
        _heapStr = $"{total,12}";
        return total;
    }
}
