# Porting Status

Tracks which upstream (magiblot/tvision) types have been ported. Update this file in the same
change that ports a type. Status values: `todo`, `wip`, `done` (ported with tests), `skip`
(intentionally not ported, with a reason).

| Area | Upstream | C# type(s) | Status | Notes |
|---|---|---|---|---|
| Objects | `objects.h` | `TPoint`, `TRect` | done | |
| Objects | `objects.h` (`TCollection`, `TSortedCollection`) | `List<T>` / wrappers | todo | Port only when a consumer needs collection-specific semantics |
| Drawing | `colors.h`, `source/platform/colors.cpp` | `TColorRGB`, `TColorBIOS`, `TColorXTerm`, `TColorConversion`, `TColor`, `TColorAttr`, `TAttrPair`, `StyleFlags` | done | |
| Drawing | `scrncell.h` | `TScreenCharacter`, `TScreenCell` | done | Text is UTF-16, max 15 code units per cell |
| Drawing | `drawbuf.h`, `source/tvision/drivers.cpp` | `TDrawBuffer` | done | |
| Drawing | `ttext.h`, `source/platform/ttext.cpp`, `codepage.cpp` | `TText`, `TTextMetrics`, `CpTranslator` | done | UTF-16 instead of UTF-8; UTF-32 overloads not ported |
| Platform | `source/platform/platfchr.cpp` | `CharOps` (internal) | done | Managed `wcwidth` approximation; Windows and Linux console width variants todo |
| Platform | `system.h` | `TScreen` | wip | Only `ScreenWidth`/`ScreenHeight` so far |
| Drawing | `views.h` (`TPalette`) | `TPalette` | todo | |
| Events | `system.h`, `tkeys.h` | `TEvent`, `TKey`, `TEventQueue`, `TMouse` | todo | |
| Events | `views.h` (`TCommandSet`) | `TCommandSet` | todo | |
| Views | `views.h` | `TView` | todo | |
| Views | `views.h`, `tvwrite.cpp`, `tvexposd.cpp` | `TGroup` | todo | |
| Views | `views.h` | `TFrame`, `TWindow` | todo | |
| Views | `views.h` | `TScrollBar`, `TScroller`, `TListViewer` | todo | |
| Platform | `hardware.h`, `source/platform/*` | headless screen/input | todo | Needed for view tests |
| Platform | `ansiwrit.cpp`, `termio.cpp`, `far2l.cpp` | Unix ANSI/termios backend | todo | No curses (see AGENTS.md) |
| Platform | `source/platform/win32con.cpp` | Win32 console backend | todo | |
| App | `app.h` | `TProgram`, `TApplication`, `TDesktop`, `TBackground` | todo | |
| Menus | `menus.h` | `TMenuItem`, `TMenu`, `TMenuView`, `TMenuBar`, `TMenuBox`, `TMenuPopup` | todo | |
| Menus | `menus.h` | `TStatusLine`, `TStatusDef`, `TStatusItem` | todo | |
| Dialogs | `dialogs.h` | `TDialog`, `TButton`, `TStaticText`, `TLabel`, `TParamText` | todo | |
| Dialogs | `dialogs.h` | `TInputLine`, `THistory` | todo | |
| Dialogs | `dialogs.h` | `TCluster`, `TCheckBoxes`, `TRadioButtons`, `TMultiCheckBoxes` | todo | |
| Dialogs | `dialogs.h` | `TListBox`, `TSortedListBox` | todo | |
| Dialogs | `msgbox.h` | `MessageBox`, `InputBox` | todo | |
| Validators | `validate.h` | `TValidator`, `TPXPictureValidator`, `TFilterValidator`, `TRangeValidator`, `TLookupValidator`, `TStringLookupValidator` | todo | |
| StdDlg | `stddlg.h` | `TFileDialog`, `TChDirDialog`, `TDirListBox` | todo | |
| Editors | `editors.h` | `TEditor`, `TMemo`, `TFileEditor`, `TEditWindow`, `TIndicator` | todo | |
| Editors | `textview.h` | `TTextDevice`, `TTerminal` | todo | |
| Optional | `outline.h` | `TOutline` | todo | |
| Optional | `colorsel.h` | `TColorDialog` | todo | |
| Optional | `help.h`, `helpbase.h` | `THelpWindow`, `THelpFile` | todo | |
| Streams | `tobjstrm.h`, `resource.h` | `TStreamable`, `TResourceFile` | skip | Deferred until something needs it |
| Examples | `examples/hello` | `examples/Hello` | todo | |
| Examples | `examples/tvdemo`, `examples/tvedit` | | todo | |
