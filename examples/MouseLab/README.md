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
- **Change dir** (`Lab > Change dir...`):
  - double-click a directory in the tree: the tree re-roots there and `Directory name` follows;
  - with the tree focused, `Chdir` is the default button; `Revert` goes back to the current directory;
  - click `OK`, then reopen `Open file...`: it lists the new directory.
- **Editor** (`Lab > Editor`):
  - click to place the cursor; drag to select (hold above or below the text, or past the sides, to keep scrolling);
  - double-click selects a word, a run of blanks or a run of punctuation; triple-click selects a line;
  - Shift+click extends the selection; middle-drag scrolls the text;
  - the wheel scrolls; drag the scroll bars; the indicator at the bottom left shows `line:column`
    and `☼` once the text is modified;
  - right-click: a menu with `Cut`, `Copy`, `Paste` and `Undo` opens at the pointer;
  - select text, then cut or copy (right-click menu, `Clipboard` menu, Shift-Del or Ctrl-Ins); click
    elsewhere and paste (Shift-Ins): the text appears at the cursor. Paste also works into a second
    editor window. With no selection, `Cut` and `Copy` are greyed out in the menus;
  - `Clipboard > Show clipboard` shows the clipboard window, titled `Clipboard`, holding the last
    cut or copied text selected; closing it only hides it;
  - the window is titled `Untitled`; drag its bottom-right corner: it stops at 24x6 and the editor
    follows; closing it discards the text.
