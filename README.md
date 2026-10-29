# SharpVision

A C# port of **Turbo Vision**, the text-mode UI framework Borland shipped in the early 1990s
with Turbo Pascal and Borland C++. Turbo Vision gives you overlapping windows, pull-down menus,
dialogs, a status line, mouse support and a text editor, all in a terminal.

The port is based on [magiblot/tvision](https://github.com/magiblot/tvision), a modern,
cross-platform C++ version of Turbo Vision 2.0.

> **Status:** early work. See [docs/porting-status.md](docs/porting-status.md).

## Building

Requires the .NET 8 SDK.

```bash
dotnet build
dotnet test
```

## Running the example

The `Hello` example runs in a terminal on Linux, macOS or Windows:

```bash
dotnet run --project examples/Hello
```

Press F10 or click the menu bar to open the menu, and Alt-X to quit.

## Contributing

Read [AGENTS.md](AGENTS.md) for the porting rules, code conventions and workflow. It applies
to humans and coding agents.

## License and attribution

SharpVision is a derived work of Borland's Turbo Vision and magiblot/tvision. Their notices
are reproduced in [THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt).
