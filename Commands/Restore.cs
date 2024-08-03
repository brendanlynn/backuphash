using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Text.Json;

namespace BackupHash;
static partial class Commands {
    private static void _Restore(string? o, string? b, string? i, long? ts)
    {
        o = Shared.MakePathFull(o);
        b = Shared.MakePathFull(b);
        if (i is null)
            i = o;
        else
            i = Shared.MakePathFull(i);
        if (o == b || i == b) {
            UserInteraction.Fatal("Neither argument --output/-o nor --input/-i should equal --backup-dir/-b. This is not the case.");
            return;
        }
        string snapshotFilepath;
        if (ts.HasValue)
            snapshotFilepath = Shared.GetSnapshotFilepathFromTimestamp(b, ts.Value);
        else
            snapshotFilepath = Directory.GetFiles(b, Shared.SnapshotSearchString)
                                   .Where(F => Shared.SnapshotMetaFileRegex.IsMatch(F))
                                   .Order()
                                   .Last();

        string snapshotJson;
        try {
            snapshotJson = File.ReadAllText(snapshotFilepath);
        }
        catch (Exception e) {
            UserInteraction.Fatal(e.Message);
            return;
        }

        SnapshotMeta? snapshotMeta;
        try {
            snapshotMeta = JsonSerializer.Deserialize<SnapshotMeta>(snapshotJson);
        }
        catch (Exception e) {
            UserInteraction.Fatal(e.Message);
            return;
        }
        if (snapshotMeta is null) {
            UserInteraction.Fatal($"JSON file '{snapshotFilepath}' is invalid.");
            return;
        }

        List<string> deletingFiles;
        if (Directory.Exists(o)) {
            try {
                deletingFiles = FindAllFiles.Find(o);
            }
            catch (Exception e) {
                UserInteraction.Fatal(e.Message);
                return;
            }
        }
        else
            deletingFiles = [];

        Dictionary<string, ConfigInfo?> configs = [];
        ConfigInfo? GetConfigForDir(string Dir)
        {
            if (configs.TryGetValue(Dir, out ConfigInfo? config))
                return config;
            _ = ConfigInfo.ConditionalFromDirectory(Dir, out config);
            configs.Add(Dir, config);
            return config;
        }
        bool DestinationIncluded(string Filepath) {
            string? directory = Path.GetDirectoryName(Filepath);
            if (directory is null)
                return false;
            List<string> upperDirectories = [directory];
            string? currentDirectory = directory;
            while (true) {
                currentDirectory = Shared.GetParentDir(currentDirectory);
                if (string.IsNullOrEmpty(currentDirectory))
                    break;
                upperDirectories.Add(currentDirectory);
                if (Path.GetRelativePath(b, currentDirectory) == currentDirectory)
                    break;
            }
            string workingDirectory = b;
            ConfigInfo? currentConfig = GetConfigForDir(workingDirectory);
            for (int idx = upperDirectories.Count - 1; idx > 0; --idx) {
                string thisDir = upperDirectories[idx];
                ConfigInfo? thisConfig = GetConfigForDir(thisDir);
                if (thisConfig is not null) {
                    currentConfig = thisConfig;
                    workingDirectory = thisDir;
                }
                if (currentConfig is null)
                    continue;
                string relativeNext = Path.GetRelativePath(workingDirectory, upperDirectories[idx - 1]);
                if (!currentConfig.IncludedDirectory(relativeNext))
                    return false;
            }
            return currentConfig?.IncludedFile(Filepath) ?? true;
        }

        List<(string Hash, string Destination)> pairs = [];
        foreach (SnapshotFileMeta fileMeta in snapshotMeta.Files) {
            string relative = Path.GetRelativePath(i, fileMeta.Filepath);
            if (relative == fileMeta.Filepath)
                continue;
            string newPath = Path.Combine(o, relative);
            if (!DestinationIncluded(newPath))
                continue;
            pairs.Add((fileMeta.Hash, newPath));
        }

        HashSet<string> set_deleted = [..deletingFiles];
        HashSet<string> set_added = [.. pairs.Select(P => P.Destination)];
        HashSet<string> set_modified = [..deletingFiles];
        set_modified.IntersectWith(set_added);
        set_deleted.ExceptWith(set_modified);
        set_added.ExceptWith(set_modified);

        if (set_added.Count > 0) {
            UserInteraction.Info("The following file(s) will be added:");
            foreach (string file in set_added)
                Console.WriteLine("    " + file);
        }
        else
            UserInteraction.Info("No files will be added.");
        if (set_modified.Count > 0) {
            UserInteraction.Warning("The following file(s) will be overwritten:");
            foreach (string file in set_modified)
                Console.WriteLine("    " + file);
        }
        else
            UserInteraction.Info("No files will be overwritten.");
        if (set_deleted.Count > 0) {
            UserInteraction.Warning("The following file(s) will be deleted:");
            foreach (string file in set_deleted)
                Console.WriteLine("    " + file);
        }
        else
            UserInteraction.Info("No files will be deleted.");

        if (!UserInteraction.Warning_PromptContinue("Please reread all input. There is no undo button.")) {
            UserInteraction.Fatal("Operation cancelled by user.");
            return;
        }

        if (Directory.Exists(o))
            foreach (string file in set_deleted)
                try {
                    File.Delete(file);
                }
                catch {
                    UserInteraction.Warning($"Could not delete existing file '{file}'.");
                }

        foreach ((string hash, string destination) in pairs) {
            UserInteraction.Info($"Copying to file {destination}");
            string? directory = Path.GetDirectoryName(destination);
            if (directory is null) {
                UserInteraction.Fatal(null);
                return;
            }
            if (!Directory.Exists(directory))
                _ = Directory.CreateDirectory(directory);

            FileInfo fileInfo = new(Path.Combine(b, hash));
            _ = fileInfo.CopyTo(destination, true);
        }
        UserInteraction.Info($"Operation succeeded with {pairs.Count} file(s) restored: {set_added.Count} added; {set_modified.Count} modified; and {set_deleted.Count} deleted.");
    }
    public static Command Restore = new("restore", "Restores a backup in the backup directory to the output directory. Use --input to restore files to a different location than where they were when they were first backed up.") {
        new Option<string>(["--output", "-o"], "The output file/directory. Defaults to the working directory."),
        new Option<string>(["--backup-dir", "-b"], "The backup directory. Defaults to the working directory."),
        new Option<string>(["--input", "-i"], "The location of the files when they were first backed up; the directory in the backups. Defaults to the value of --output/-o."),
        new Option<long>(["--timestamp", "-ts"], "The timestamp of the backup used. Defaults to the most recent backup."),
    };
    private static void _Init_Restore() {
        Restore.Handler = CommandHandler.Create<string?, string?, string?, long?>(_Restore);
    }
}