using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BackupHash;
static partial class Commands {
    private static void _View(long? ts) {
        if (ts.HasValue) {
            string snapshotFilename = Shared.GetSnapshotFilepathFromTimestamp(Shared.WorkingDirectory, ts.Value);
            string snapshotJson;
            try {
                snapshotJson = File.ReadAllText(snapshotFilename);
            }
            catch (Exception e) {
                UserInteraction.Fatal(e.Message);
                return;
            }
            SnapshotMeta? snapshot = JsonSerializer.Deserialize<SnapshotMeta>(snapshotJson);
            if (snapshot is null) {
                UserInteraction.Fatal($"Invalid JSON file '{snapshotFilename}'.");
                return;
            }
            UserInteraction.Info($"Backup directory structure at time '{snapshot.Time}' is:");
            IOrderedEnumerable<SnapshotFileMeta> orderedFiles = snapshot.Files.OrderBy(F => F.Filepath);
            foreach (SnapshotFileMeta file in orderedFiles) {
                string filePath = Path.Combine(Shared.WorkingDirectory, file.Hash);
                FileInfo fileInfo = new(filePath);
                Console.WriteLine($"    {file.Filepath}");
            }
        }
        else {
            string[] files = Directory.GetFiles(Shared.WorkingDirectory, Shared.SnapshotSearchString, SearchOption.TopDirectoryOnly);
            UserInteraction.Info("The following snapshots are available:");
            foreach (string file in files) {
                if (!Shared.SnapshotMetaFileRegex.IsMatch(file))
                    continue;
                string timestamp = Shared.GetTimestampFromFilepath(file);
                DateTime time = Shared.FromTimestamp(timestamp);
                Console.WriteLine($"    {timestamp} ({time})");
            }
        }
    }
    public static Command View = new("view", "Views the backups present in the backup directory. If a timestamp is provided, the directory structure of the backup at that time will be revealed. If a timestamp is not provided, the timestamps of the various snapshots will be listed.") {
        new Option<long>(["--timestamp", "-ts"], "The timestamp of the specific backup to view the directory structure of, if applicable.")
    };
    private static void _Init_View() {
        View.Handler = CommandHandler.Create<long?>(_View);
    }
}