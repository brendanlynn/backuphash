using static System.Console;

namespace BackupHash;

static class UserInteraction {
    public static void Info(string? Message) {
        Message ??= "Unknown info.";
        ConsoleColor formerBackgroundColor = BackgroundColor;
        ConsoleColor formerForegroundColor = ForegroundColor;
        BackgroundColor = ConsoleColor.Blue;
        ForegroundColor = ConsoleColor.White;
        Write("INFO:");
        BackgroundColor = formerBackgroundColor;
        ForegroundColor = formerForegroundColor;
        WriteLine(" " + Message);
    }
    public static void Warning(string? Message) {
        Message ??= "Unknown warning.";
        ConsoleColor formerBackgroundColor = BackgroundColor;
        ConsoleColor formerForegroundColor = ForegroundColor;
        BackgroundColor = ConsoleColor.Yellow;
        ForegroundColor = ConsoleColor.Black;
        Write("WARNING:");
        BackgroundColor = formerBackgroundColor;
        ForegroundColor = formerForegroundColor;
        WriteLine(" " + Message);
    }
    public static bool Warning_PromptContinue(string? Message) {
        Warning(Message);
        Write("    Proceed? (y/n): ");
        string? c = ReadLine();
        return c is "y";
    }
    public static void Fatal(string? Message) {
        Message ??= "Unknown fatal error.";
        ConsoleColor formerBackgroundColor = BackgroundColor;
        ConsoleColor formerForegroundColor = ForegroundColor;
        BackgroundColor = ConsoleColor.Red;
        ForegroundColor = ConsoleColor.White;
        Write("FATAL:");
        BackgroundColor = formerBackgroundColor;
        ForegroundColor = formerForegroundColor;
        WriteLine(" " + Message);
    }
}