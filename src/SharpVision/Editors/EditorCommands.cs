namespace SharpVision.Editors;

/// <summary>The editor commands, update and selection flags, dialog codes and search options.</summary>
/// Ported from include/tvision/editors.h (__EDIT_COMMAND_CODES), include/tvision/config.h (maxFindStrLen, maxReplaceStrLen).
public static class EditorCommands
{
    /// <summary>Update the cursor, scroll bars and indicator.</summary>
    public const byte ufUpdate = 0x01;
    /// <summary>Also redraw the current line.</summary>
    public const byte ufLine = 0x02;
    /// <summary>Also redraw the whole view.</summary>
    public const byte ufView = 0x04;

    /// <summary>Extend the selection.</summary>
    public const byte smExtend = 0x01;
    /// <summary>Select whole words.</summary>
    public const byte smDouble = 0x02;
    /// <summary>Select whole lines.</summary>
    public const byte smTriple = 0x04;

    /// <summary>The result of a failed scan.</summary>
    public const int sfSearchFailed = -0x01;

    /// <summary>Find text.</summary>
    public const ushort cmFind = 82;
    /// <summary>Find and replace text.</summary>
    public const ushort cmReplace = 83;
    /// <summary>Repeat the last find or replace.</summary>
    public const ushort cmSearchAgain = 84;

    /// <summary>Move one character left.</summary>
    public const ushort cmCharLeft = 500;
    /// <summary>Move one character right.</summary>
    public const ushort cmCharRight = 501;
    /// <summary>Move to the previous word.</summary>
    public const ushort cmWordLeft = 502;
    /// <summary>Move to the next word.</summary>
    public const ushort cmWordRight = 503;
    /// <summary>Move to the start of the line.</summary>
    public const ushort cmLineStart = 504;
    /// <summary>Move to the end of the line.</summary>
    public const ushort cmLineEnd = 505;
    /// <summary>Move one line up.</summary>
    public const ushort cmLineUp = 506;
    /// <summary>Move one line down.</summary>
    public const ushort cmLineDown = 507;
    /// <summary>Move one page up.</summary>
    public const ushort cmPageUp = 508;
    /// <summary>Move one page down.</summary>
    public const ushort cmPageDown = 509;
    /// <summary>Move to the start of the text.</summary>
    public const ushort cmTextStart = 510;
    /// <summary>Move to the end of the text.</summary>
    public const ushort cmTextEnd = 511;
    /// <summary>Insert a line break.</summary>
    public const ushort cmNewLine = 512;
    /// <summary>Delete the character before the cursor.</summary>
    public const ushort cmBackSpace = 513;
    /// <summary>Delete the character at the cursor.</summary>
    public const ushort cmDelChar = 514;
    /// <summary>Delete up to the next word.</summary>
    public const ushort cmDelWord = 515;
    /// <summary>Delete up to the start of the line.</summary>
    public const ushort cmDelStart = 516;
    /// <summary>Delete up to the end of the line.</summary>
    public const ushort cmDelEnd = 517;
    /// <summary>Delete the line.</summary>
    public const ushort cmDelLine = 518;
    /// <summary>Toggle insert and overwrite mode.</summary>
    public const ushort cmInsMode = 519;
    /// <summary>Start selecting with the cursor keys.</summary>
    public const ushort cmStartSelect = 520;
    /// <summary>Clear the selection.</summary>
    public const ushort cmHideSelect = 521;
    /// <summary>Toggle auto indent.</summary>
    public const ushort cmIndentMode = 522;
    /// <summary>Broadcast when the file name changes.</summary>
    public const ushort cmUpdateTitle = 523;
    /// <summary>Select the whole text.</summary>
    public const ushort cmSelectAll = 524;
    /// <summary>Delete back to the previous word.</summary>
    public const ushort cmDelWordLeft = 525;
    /// <summary>Toggle single-code-unit mode.</summary>
    public const ushort cmEncoding = 526;

    /// <summary>Dialog: out of memory.</summary>
    public const int edOutOfMemory = 0;
    /// <summary>Dialog: a file could not be read.</summary>
    public const int edReadError = 1;
    /// <summary>Dialog: a file could not be written.</summary>
    public const int edWriteError = 2;
    /// <summary>Dialog: a file could not be created.</summary>
    public const int edCreateError = 3;
    /// <summary>Dialog: save the modified file?</summary>
    public const int edSaveModify = 4;
    /// <summary>Dialog: save the untitled file?</summary>
    public const int edSaveUntitled = 5;
    /// <summary>Dialog: choose a name to save as.</summary>
    public const int edSaveAs = 6;
    /// <summary>Dialog: find.</summary>
    public const int edFind = 7;
    /// <summary>Dialog: the search failed.</summary>
    public const int edSearchFailed = 8;
    /// <summary>Dialog: replace.</summary>
    public const int edReplace = 9;
    /// <summary>Dialog: replace this occurrence?</summary>
    public const int edReplacePrompt = 10;

    /// <summary>Match case.</summary>
    public const ushort efCaseSensitive = 0x0001;
    /// <summary>Match whole words only.</summary>
    public const ushort efWholeWordsOnly = 0x0002;
    /// <summary>Ask before each replacement.</summary>
    public const ushort efPromptOnReplace = 0x0004;
    /// <summary>Replace every occurrence.</summary>
    public const ushort efReplaceAll = 0x0008;
    /// <summary>Replace rather than only find.</summary>
    public const ushort efDoReplace = 0x0010;
    /// <summary>Keep a backup when saving.</summary>
    public const ushort efBackupFiles = 0x0100;

    /// <summary>The horizontal scroll limit.</summary>
    public const int maxLineLength = 256;
    /// <summary>The length of the find text.</summary>
    public const int maxFindStrLen = 80;
    /// <summary>The length of the replacement text.</summary>
    public const int maxReplaceStrLen = 80;
}
