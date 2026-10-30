# MouseLab

Not an upstream example: a manual checklist for mouse handling. Run it with
`dotnet run --project examples/MouseLab` and check each item with the mouse only.

- **Menu bar**: click `Lab` and `Windows` to open them; click an item to run it; click outside to close.
- **Status line**: click `Alt-D Dialog` and `Alt-W Window`; press and drag across items (the highlight follows).
- **Windows** (`Lab > Window`, open several):
  - drag the title bar to move; drag the bottom-right corner to resize;
  - click `[■]` to close; click `[↑]` or double-click the title bar to zoom and unzoom;
  - click a window behind another to bring it to the front;
  - scroll bars: click the arrows (hold to repeat), click the page area (the thumb jumps
    there), drag the thumb, and use the wheel over each bar (the horizontal one needs a tilt wheel or sideways touchpad scroll;
    Shift+wheel works only if the terminal reports it as horizontal). The values line updates.
- **Dialog** (`Lab > Dialog`):
  - drag the title bar to move it;
  - click a label (`Name`, `Long text`) to focus its input line;
  - in an input line, click to place the cursor, drag to select, double-click to select all;
  - in `Long text`, hold the button on the `◄`/`►` arrows (or drag past the edges) to scroll;
  - click a check box to toggle it; click a radio button to choose it; press on one item and
    release on another: nothing changes; click `Options` / `Mode` to focus the group;
  - in `Fruits`, click an item to focus it, drag up/down (hold below or above the list to keep
    scrolling), use the scroll bar (or the wheel over the bar); double-click an item (or press Space):
    the status line shows `Picked: ...`;
  - click `Alpha` / `Beta`: the `Last click` line updates; press and release outside a button: nothing happens;
  - type something, click `OK`, reopen the dialog: the text and choices are kept. `Cancel` discards changes.
- **Message box** (`Lab > Message box...`): click `Yes`, `No` or `Cancel`; the next box reports
  the command (12 = Yes, 13 = No, 11 = Cancel). Clicking `[■]` also cancels.
