# AGENTS.md

This file provides guidance to Claude Code (claude.ai/code) and other coding agents when
working with code in this repository. `CLAUDE.md` is a symlink to this file, so both names
resolve to the same guidance.

SharpVision is a C# port of **Borland Turbo Vision** (the 1990s text-mode UI framework:
TView/TGroup/TProgram, menus, dialogs, editors). The reference implementation is
**magiblot/tvision** (<https://github.com/magiblot/tvision>), a modern, portable C++ version of
Turbo Vision 2.0. When behavior is in question, the upstream C++ code is the spec.

## Build and Test Commands

### Building the Solution
```bash
# Standard build
dotnet build
dotnet build -c Release

# Build with warnings as errors (CI validation; Directory.Build.props already sets this)
dotnet build -warnaserror
```

### Running Tests
```bash
# Run all tests
dotnet test -c Release

# Run a specific test by name
dotnet test --filter "FullyQualifiedName~TRectTests"

# Run tests in a specific project
dotnet test test/SharpVision.Tests/SharpVision.Tests.csproj -c Release
```

### Code Quality
```bash
# Format check (CI runs this)
dotnet format --verify-no-changes

# Apply formatting
dotnet format
```

### Upstream Reference Source
```bash
# Clone magiblot/tvision at the pinned commit into .upstream/tvision (git-ignored)
./scripts/fetch-upstream.sh
```
The pinned commit is `UpstreamTVisionCommit` in `Directory.Build.props`. Keep it in sync with
`scripts/fetch-upstream.sh`. Bump it in a dedicated change and note which ported files need
another look.

## High-Level Architecture

### Project Structure
- **`/src/SharpVision/`**: the library. Namespaces follow the upstream headers (see the
  table below)
- **`/test/SharpVision.Tests/`**: xUnit tests. Folder layout mirrors `src/SharpVision/`
- **`/examples/`**: sample apps ported from upstream `examples/` (`hello`, `tvdemo`,
  `tvedit`, `tvdir`, `mmenu`, `palette`, `tvforms`, ...)
- **`/contrib/`**: optional add-ons that are not part of classic Turbo Vision
- **`/docs/`**: documentation. [`docs/porting-status.md`](docs/porting-status.md) tracks what
  has been ported
- **`/scripts/`**: helper scripts

### Upstream → SharpVision Map
| Upstream header(s) (`include/tvision/`) | Namespace | Contents |
|---|---|---|
| `objects.h`, `ttypes.h`, `util.h` | `SharpVision.Objects` | `TPoint`, `TRect`, helpers. Collections become `List<T>` |
| `scrncell.h`, `colors.h`, `drawbuf.h`, `ttext.h`, `tspan.h` | `SharpVision.Drawing` | `TScreenCell`, `TColorAttr`, `TDrawBuffer`, `TText` (cell width), palettes |
| `system.h`, `tkeys.h` | `SharpVision.Events` | `TEvent`, `TEventQueue`, `TMouse`, `TKey`, `kbXxx`/`evXxx` |
| `hardware.h`, `source/platform/*` | `SharpVision.Platform` | Screen/input backends (ANSI+termios on Unix, Win32 console on Windows; no curses, see below) |
| `views.h` | `SharpVision.Views` | `TView`, `TGroup`, `TFrame`, `TScrollBar`, `TScroller`, `TListViewer`, `TWindow` |
| `app.h` | `SharpVision.App` | `TProgram`, `TApplication`, `TDesktop`, `TBackground` |
| `menus.h` | `SharpVision.Menus` | `TMenuBar`, `TMenuBox`, `TMenuPopup`, `TStatusLine` |
| `dialogs.h`, `msgbox.h` | `SharpVision.Dialogs` | `TDialog`, `TButton`, `TInputLine`, `TCluster`, `TListBox`, `MessageBox` |
| `validate.h` | `SharpVision.Validators` | `TValidator` and subclasses |
| `stddlg.h` | `SharpVision.StdDlg` | `TFileDialog`, `TChDirDialog` |
| `editors.h`, `textview.h` | `SharpVision.Editors` | `TEditor`, `TMemo`, `TFileEditor`, `TTerminal` |
| `outline.h`, `colorsel.h`, `help.h`, `helpbase.h` | `SharpVision.Outline`, `.ColorSel`, `.Help` | Optional modules |
| `tobjstrm.h`, `resource.h`, `tvobjs.h` | `SharpVision.Streams` | Deferred. Do not port `TStreamable` until something needs it |

Do not create a `SharpVision.System` namespace, because it shadows `System`.

### Key Architectural Concepts
- **View hierarchy**: every visible element is a `TView`. `TGroup` owns children in a
  **circular, z-ordered linked list** (`last`, `next`). Keep this structure. `forEach`,
  `firstThat`, focus order and redraw clipping all depend on it.
- **Event loop**: `TProgram.Run` → `GetEvent` → `HandleEvent` dispatch. Events are routed in
  three phases (preprocess, focused, postprocess). Commands (`cmXxx`) are 16-bit numbers that
  can be enabled or disabled through `TCommandSet`.
- **Drawing**: views write `TDrawBuffer` rows through `WriteLine`/`WriteBuf`. The group's
  buffer and exposure logic (`tvwrite.cpp`, `tvexposd.cpp`) clip them to visible regions.
- **Palettes**: colors are indexes mapped through each owner's palette, ending at the
  application palette (`GetColor`/`MapColor`).
- **Platform layer**: everything above `SharpVision.Platform` is platform-independent. Tests
  must be able to swap in a headless in-memory screen and a scripted event source.

### Platform Layer (Terminal I/O)
Decision: **no curses dependency.** Port upstream's curses-free path to managed code instead.
- **Unix (Linux, macOS)**: output goes through an ANSI/VT writer ported from
  `source/platform/ansiwrit.cpp`. Input uses raw mode set with `tcgetattr`/`tcsetattr`
  (a handful of `[LibraryImport("libc")]` calls) plus an escape-sequence parser for keys,
  mouse (SGR 1006) and the far2l/kitty extensions, ported from `termio.cpp` and `far2l.cpp`.
  Terminal size comes from `ioctl(TIOCGWINSZ)`, with `SIGWINCH` handled through
  `PosixSignalRegistration`.
- **Windows**: call the Win32 console API directly via `[LibraryImport("kernel32")]`,
  porting `source/platform/win32con.cpp` (`ReadConsoleInputW`, `WriteConsoleOutputW`, or VT
  output where it is supported).
- **Rejected: `dotnet-curses` (MV10/dotnet-curses) and other ncurses wrappers.** They bind
  only the narrow-character API (no `add_wch`/`get_wch`), so they can't render Unicode cell
  widths correctly. On Windows they need ncurses DLLs installed separately, and they add
  third-party runtime dependencies. Do not reintroduce them.
- If terminfo or ncurses features are ever needed (e.g. key definitions for unusual
  terminals), write a small set of targeted `LibraryImport` bindings against `libncursesw`,
  loaded optionally through `NativeLibrary.TryLoad`. Do not take on a wrapper package.
- Use `LibraryImport` (source-generated) rather than `DllImport`. Keep every P/Invoke
  declaration in `SharpVision.Platform.Interop` and do not call it from outside
  `SharpVision.Platform`.

## Porting Rules

### Fidelity
1. **Port behavior, not syntax.** Read the upstream `.cpp` and `.h` before you write a member.
   Keep the same algorithm, the same edge cases and the same order of operations.
2. Port the portable C++ path only. Ignore `.asm` files, `__BORLANDC__`/DOS/far-pointer
   branches and the `nmXxx.cpp`/`sXxx.cpp` stream-registration stubs.
3. The public API is the **classic Turbo Vision 2.0 surface**. magiblot's internals (Unicode
   text, UTF-8, `TColorAttr`, platform backends) are the reference *implementation*. Add any
   magiblot-only public extension, such as true color or clipboard, as a separate addition and
   mark it in its XML docs.
4. Every ported file starts with a provenance comment naming its sources:
   ```csharp
   // Ported from source/tvision/tview.cpp, include/tvision/views.h (class TView).
   ```
5. Do not "improve" behavior while porting. If upstream has a bug or quirk, port it as is
   and leave a `// UPSTREAM:` comment. Propose a fix in a separate change.
6. Update [`docs/porting-status.md`](docs/porting-status.md) in the same change that ports
   or finishes a type.

### C++ → C# Mapping
| C++ | C# |
|---|---|
| `class TFoo` | `public class TFoo`. Keep the `T` prefix and the original type names |
| `camelCase` methods (`handleEvent`, `getPalette`) | `PascalCase` (`HandleEvent`, `GetPalette`) |
| public data members (`origin`, `size`, `state`) | public members in `PascalCase`. Use **fields** when they are mutable structs mutated in place (`Origin.X += 1`), properties otherwise |
| small value classes (`TPoint`, `TRect`, `TEvent`) | mutable `struct` with public fields, matching C++ copy semantics |
| `getX()` / `setX()` accessor pairs | property `X` (e.g. `TColorAttr.Foreground`) |
| `virtual` / overrides | `public virtual` / `override`. Do not seal types that upstream subclasses |
| `cmXxx`, `evXxx`, `kbXxx`, `sfXxx`, `ofXxx`, `gfXxx`, `hcXxx` | `const` with the **original names**, grouped in one static class per upstream header section: `Commands` and `ViewFlags` (views.h), `EventCodes` and `KeyCodes` (system.h, tkeys.h), `StyleFlags` (colors.h). Use them via `using static` |
| palette strings (`cpFrame "\x01\x01\x02..."`) | byte collection expressions (`[0x01, 0x01, 0x02, ...]`) passed to `TPalette`. C#'s `\x` escape takes up to 4 hex digits, so never copy palette strings |
| `Boolean`, `uchar`, `ushort`, `short` | `bool`, `byte`, `ushort`, `short`. Keep the width where it matters (bit masks, key codes) |
| `char*`, `TStringView` | `string` / `ReadOnlySpan<char>`. Indexes and lengths are UTF-16 code units, and a character is a `Rune`. Measure display width in **columns** with `TText.Width`, never with `.Length` |
| CP437 byte literals (`'\xB3'`, `"\xC4\xC4"`, `'\x10'`) | the Unicode glyph (`'│'`, `"──"`, `'►'`). `TText.FromCodePage` gives the mapping. Never use `'\xNN'` for N ≥ 0x80: in C# that is Latin-1, not CP437 |
| `TSpan<T>` | `Span<T>` / `ReadOnlySpan<T>` |
| `ushort` position/count parameters | `int` parameters narrowed with `(ushort)` on entry, so out-of-range values behave as in C++ (see `TDrawBuffer`) |
| `TNSCollection`, `TCollection`, `TSortedCollection` | `List<T>` or a thin wrapper when sorted, duplicate or search semantics matter |
| `new` / `delete` / `destroy()` / `shutDown()` | GC. Keep `ShutDown()` as a virtual teardown hook and port `TObject.Destroy(obj)` as a static helper that calls it |
| `messageBox(...)`, `message(...)` free functions | static methods on a static class (`MsgBox.MessageBox`, `TView.Message`) |
| out-parameters by reference (`TRect&`) | `ref`/`out` parameters, or return the value when upstream only fills it |
| `#define Uses_TFoo` | Not needed. Use ordinary `using` directives |

## Code Style and Conventions

### C# Style
- Allman style braces (opening brace on new line)
- 4 spaces indentation, no tabs
- File-scoped namespaces, one top-level type per file, file named after the type
- Private fields prefixed with underscore `_fieldName`; PascalCase for public/protected members
- Use `var` when the type is apparent
- No `this.` qualifier unless necessary
- Sort `using` statements with `System.*` first. Implicit usings are **off**
- XML doc comments on public APIs. `GenerateDocumentationFile` is on, so a missing doc is a
  build error. A one-line summary of the upstream intent is enough
- Nullable reference types are enabled everywhere. Upstream `nullptr` checks map to `T?`
- Do not use `unsafe` code without a measured reason
- Treat warnings as errors. Fix warnings instead of suppressing them. Any suppression in
  `.editorconfig` needs a comment that explains it (see CA1051)

### Test Naming
- Test classes are `public sealed class TFooTests` in the mirrored namespace
- Follow pattern: `Should_ExpectedBehavior_When_Condition`
- When upstream has a test (`test/tvision/*`), port it first

### General Conventions
- Port one cohesive unit per change (one type, or a type and its tight helpers). Keep pull
  requests small and focused (< 300 lines when possible, excluding mechanical constant tables)
- Include unit tests with all changes
- Treat `TBD` comments as action items to be resolved
- Do not add runtime dependencies to `SharpVision` without asking first. The core library
  should depend only on the BCL

## Testing Guidelines
- Pure logic (geometry, collections, key mapping, text width, validators, editor buffer
  operations) gets direct unit tests.
- Views are tested **headlessly**: render into an in-memory screen buffer and assert on cell
  characters and attributes, and drive input from a scripted event queue. Never touch the real
  console in tests.
- When porting an algorithm with tricky edge cases (exposure/clipping, `TEditor` gap buffer,
  menu hotkeys), write tests from the upstream behavior first. Compare against the C++ build
  when in doubt.
- Tests must be deterministic: no timers, sleeps or real terminal size.

## Development Workflow

### Git Branches
- **`main`**: main development branch
- Feature branches: `feature/description` (e.g. `feature/port-tgroup`)
- Bugfix branches: `fix/description`

### Making Changes
1. Read the upstream source for the type (`./scripts/fetch-upstream.sh`, then
   `.upstream/tvision/`)
2. Read the existing ported code it depends on and follow its patterns
3. Port the code, with the provenance comment, and add tests
4. Run `dotnet build -warnaserror`, `dotnet test` and `dotnet format --verify-no-changes`
5. Update `docs/porting-status.md`

### Suggested Porting Order
Objects → Drawing (`TScreenCell`, `TColorAttr`, `TDrawBuffer`, `TText`) → Events and keys →
`TView` → `TGroup` (with exposure and write logic) → headless platform → `TProgram`,
`TApplication`, `TDesktop` → `TFrame`, `TWindow` → Menus and status line → real terminal
backends → Dialogs and controls → Validators → StdDlg → Editors → Help, Outline, ColorSel →
examples (`hello` first, then `tvdemo` and `tvedit`).

### Target Frameworks
- **.NET 8.0**: the only target for now (`Directory.Build.props`)

## Important Files
- `Directory.Build.props`: shared MSBuild properties, test package versions, pinned upstream commit
- `global.json`: .NET SDK version (8.0.413, `latestFeature` roll-forward)
- `.editorconfig`: formatting, naming and analyzer severities
- `docs/porting-status.md`: per-type porting tracker
- `scripts/fetch-upstream.sh`: fetches the upstream reference source
- `THIRD_PARTY_NOTICES.txt`: Borland and magiblot/tvision notices. Keep it, because this code
  is a derived work
