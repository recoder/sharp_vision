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
    Shift+wheel works only if the terminal reports it as horizontal). The text and the
    `(column,row)` grid scroll with the bars; resizing the window changes the page size;
  - right-click inside a window: a context menu opens below the pointer (above it near the
    bottom of the screen); click `Zoom`, `Next window` or `Close`, or click outside to dismiss it.
- **Dialog** (`Lab > Dialog`):
  - drag the title bar to move it;
  - click a label (`Name`, `Long text`) to focus its input line;
  - in an input line, click to place the cursor, drag to select, double-click to select all;
  - type a name, tab away, then click the `↓` arrow next to `Name`: a list of earlier names drops
    down; double-click one to put it in the line, or click outside the list to close it;
  - in `Long text`, hold the button on the `◄`/`►` arrows (or drag past the edges) to scroll;
  - click a check box to toggle it; click a radio button to choose it; press on one item and
    release on another: nothing changes; click `Options` / `Mode` to focus the group;
  - in `Checks` (multi-state boxes), each click cycles an item through ` `, `+` and `!`;
  - in `Fruits`, click an item to focus it, drag up/down (hold below or above the list to keep
    scrolling), use the scroll bar (or the wheel over the bar); double-click an item (or press Space):
    the status line shows `Picked: ...`;
  - with `Fruits` focused, type capital letters then lowercase (`B`, `l`): the focus jumps to the first
    match and the cursor follows the typed text; Backspace steps back (keyboard, but handy to check here);
  - click `Alpha` / `Beta`: the `Last click` line updates; press and release outside a button: nothing happens;
  - type something, click `OK`, reopen the dialog: the text and choices are kept. `Cancel` discards changes.
- **Message box** (`Lab > Message box...`): click `Yes`, `No` or `Cancel`; the next box reports
  the command (12 = Yes, 13 = No, 11 = Cancel). Clicking `[■]` also cancels.
- **Open file** (`Lab > Open file...`):
  - click entries in `Files`: the `Name` line and the info pane at the bottom follow the focus;
  - double-click a directory (`name\`) or `..\`: the list changes directory; double-click a file:
    the dialog closes and a box shows the full path;
  - drag the list's scroll bar; click the `↓` next to `Name` for the history of earlier picks;
  - drag the bottom-right corner: the dialog resizes (at least 49x19) and the list grows with it.
