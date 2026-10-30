// Ported from include/tvision/msgbox.h, source/tvision/tvtext2.cpp (class MsgBoxText).

namespace SharpVision.Dialogs;

/// <summary>The button labels and titles used by <see cref="MsgBox"/>. Assign them to localize.</summary>
public static class MsgBoxText
{
    /// <summary>The Yes button.</summary>
    public static string YesText = "~Y~es";

    /// <summary>The No button.</summary>
    public static string NoText = "~N~o";

    /// <summary>The OK button.</summary>
    public static string OkText = "O~K~";

    /// <summary>The Cancel button.</summary>
    public static string CancelText = "~C~ancel";

    /// <summary>The title of <c>mfWarning</c> boxes.</summary>
    public static string WarningText = "Warning";

    /// <summary>The title of <c>mfError</c> boxes.</summary>
    public static string ErrorText = "Error";

    /// <summary>The title of <c>mfInformation</c> boxes.</summary>
    public static string InformationText = "Information";

    /// <summary>The title of <c>mfConfirmation</c> boxes.</summary>
    public static string ConfirmText = "Confirm";
}
