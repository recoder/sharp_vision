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
| Platform | `system.h`, `tscreen.cpp` | `TScreen`, `TDisplay` | done | Static classes; `Initialize`/`Done` replace constructor/destructor |
| Platform | `hardware.h`, `hardware.cpp` | `THardwareInfo`, `IDisplayOutput` (internal) | done | Clipboard functions not ported |
| Views | `views.h`, `palette.cpp` | `TPalette` | done | Built from bytes or `TColorAttr`s |
| Views | `views.h` | `Commands`, `ViewFlags` | done | |
| Objects | `tvobjs.h`, `tobject.cpp` | `TObject` | done | `Destroy` only calls `ShutDown` |
| Events | `system.h`, `tkeys.h`, `tkey.cpp` | `TEvent`, `MouseEventType`, `KeyDownEvent`, `CharScanType`, `MessageEvent`, `TKey`, `EventCodes`, `KeyCodes` | done | `TEvent` parts don't overlap; `MessageEvent.InfoPtr` is separate from the integers |
| Events | `system.h`, `tevent.cpp`, `tmouse.cpp` | `TEventQueue`, `TMouse`, `TEvent.GetMouseEvent`/`GetKeyEvent` | done | Static classes; `Initialize`/`Done` replace constructor/destructor |
| Events | `system.h`, `ttimerqu.cpp` | `TTimerQueue` | done | |
| Events | `views.h`, `tcmdset.cpp` | `TCommandSet` | done | |
| Views | `views.h`, `tview.cpp`, `tvwrite.cpp`, `tvexposd.cpp`, `tvcursor.cpp`, `mapcolor.cpp`, `misc.cpp` | `TView` | done | DOS `ushort` cell overloads of `WriteBuf`/`WriteLine` not ported |
| Views | `views.h`, `tgroup.cpp`, `grp.cpp`, `tgrmv.cpp` | `TGroup` | done | |
| Views | `views.h`, `tframe.cpp`, `framelin.cpp`, `twindow.cpp` | `TFrame`, `TWindow` | done | `TWindowInit` becomes a constructor delegate. `StandardScrollBar` waits for `TScrollBar` |
| Views | `views.h` | `TScrollBar`, `TScroller`, `TListViewer` | todo | |
| Platform | `platform.h`, `platform.cpp`, `events.cpp`, `dispbuff.cpp`, `sigwinch.cpp` | `Platform`, `EventWaiter`, `WakeUpEventSource`, `SigwinchSource`, `DisplayBuffer`, `DisplayAdapter` (internal) | done | Suspend signals (SIGTSTP/SIGCONT) not handled: raw mode disables them |
| Platform | `ansiwrit.cpp`, `termio.cpp`, `ncursinp.cpp`, `ncurdisp.cpp`, `unixcon.cpp`, `conctl.cpp` | `AnsiDisplay`, `TermIO`, `TerminalInput`, `UnixConsole`, `ConsoleCtl` (internal) | done | No curses: colors guessed from `TERM`/`COLORTERM`; far2l, win32-input-mode, OSC 52 clipboard and the Linux console/GPM backends not ported |
| Platform | `source/platform/win32con.cpp` | Win32 console backend | todo | |
| App | `app.h`, `tprogram.cpp`, `tapplica.cpp`, `tdesktop.cpp`, `tbkgrnd.cpp` | `TProgram`, `TApplication`, `TDeskTop`, `TBackground`, `AppPalettes` | wip | `TProgInit`/`TDeskInit` become constructor delegates. `ExecuteDialog` data is an `object?[]` of slots. Not yet: history (`initHistory`) |
| Menus | `menus.h`, `menu.cpp`, `tmnuview.cpp`, `tmenubar.cpp`, `tmenubox.cpp` | `TMenuItem`, `TSubMenu`, `TMenu`, `TMenuView`, `TMenuBar`, `TMenuBox` | done | |
| Menus | `menus.h`, `tmenupop.cpp` | `TMenuPopup` | todo | |
| Menus | `menus.h`, `tstatusl.cpp` | `TStatusLine`, `TStatusDef`, `TStatusItem` | done | |
| Objects | `util.h`, `drivers2.cpp`, `tvtext2.cpp` | `Util` (`Cstrlen`, `HotKeyStr`, `GetAltChar`, `GetAltCode`, ...) | done | |
| Dialogs | `dialogs.h`, `tdialog.cpp`, `tbutton.cpp`, `tstatict.cpp` | `TDialog`, `TButton`, `TStaticText`, `DialogFlags` | done | `TStaticText.GetText` returns a string |
| Dialogs | `dialogs.h`, `tlabel.cpp`, `tparamte.cpp` | `TLabel`, `TParamText` | done | `TParamText.SetText` takes a formatted string instead of `printf` arguments |
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
| Examples | `hello.cpp` | `examples/Hello` | done | |
| Examples | `examples/tvdemo`, `examples/tvedit` | | todo | |
