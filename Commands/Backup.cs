using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using static System.Console;

namespace BackupHash;
static partial class Commands {
    private static void _Backup(string? i, string? b, bool nc) {
        i = Shared.MakePathFull(i);
        b = Shared.MakePathFull(b);

        if (Path.TrimEndingDirectorySeparator(i) == Path.TrimEndingDirectorySeparator(b)) {
            UserInteraction.Fatal("Backup directory cannot be the same as source file/directory.");
            return;
        }

        if (!Directory.Exists(b)) {
            try {
                _ = Directory.CreateDirectory(b);
            }
            catch {
                UserInteraction.Fatal($"Directory '{b}' does not exist, and could not be created.");
                return;
            }
        }

        List<string> files;
        if (File.Exists(i)) {
            files = [i];
        }
        else if (Directory.Exists(i)) {
            try {
                files = FindAllFiles.Find(i);
            }
            catch (Exception e) {
                UserInteraction.Fatal(e.Message);
                return;
            }
        }
        else {
            UserInteraction.Fatal($"Input path '{i}' does not exist.");
            return;
        }

        if (files.Count == 0) {
            UserInteraction.Info("The program found no files applicable to backup.");
            return;
        }

        UserInteraction.Info("Backing up all of the following:");
        foreach (string file in files) {
            WriteLine("    " + file);
        }

        DateTime time = DateTime.UtcNow;
        string timestamp = Shared.ToTimestamp(time);
        UserInteraction.Info($"Using timestamp {timestamp} ({time}).");

        if (!nc && !UserInteraction.Warning_PromptContinue("This operation may take time.")) {
            UserInteraction.Fatal("Operation cancelled by user.");
            return;
        }

        int added;
        int preexisting;
        try {
            (added, preexisting) = BackupFiles.TakeSnapshot(files, b, time, timestamp);
        }
        catch (Exception e) {
            UserInteraction.Fatal(e.Message);
            return;
        }
        UserInteraction.Info($"Operation succeeded with {added + preexisting} files backed up: {added} not seen before; and {preexisting} duplicate(s).");
    }
    public static Command Backup = new("backup", "Backs up the provided input file/directory (working directory if unspecified) to the provided output directory (working directory if unspecified).") {
        new Option<string>(["--input", "-i"], "The input file/directory. Defaults to the working directory."),
        new Option<string>(["--backup-dir", "-b"], "The backup directory. Defaults to the working directory."),
        new Option<bool>(["--no-confirm", "-nc"], "Whether or not the program confirms the backup before executing it.")
    };
    private static void _Init_Backup() {
        Backup.Handler = CommandHandler.Create<string?, string?, bool>(_Backup);
    }
}