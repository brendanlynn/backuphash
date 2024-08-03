using System.Text.RegularExpressions;

namespace BackupHash;
static partial class Shared {
    public const string ConfigFilename = "backuphashignore";
    public const string DefaultConfig =
        "# Generated default ignore file for the .NET command line tool backuphash.\n" +
        "# Format is the same as .gitignore for Git.\n";

    public readonly static string WorkingDirectory = Environment.CurrentDirectory;

    public static string MakePathFull(string? Path) {
        if (Path is null)
            return WorkingDirectory;
        Path = System.IO.Path.Combine(WorkingDirectory, Path);
        while (true) {
            string newPath = PathBackRegex.Replace(Path, "");
            if (Path == newPath)
                break;
            Path = newPath;
        }
        return Path;
    }

    public static string ToTimestamp(DateTime Time)
        => Time.ToString("yyyyMMddHHmmssffff");
    public static DateTime FromTimestamp(string Timestamp)
        => DateTime.ParseExact(Timestamp, "yyyyMMddHHmmssffff", System.Globalization.CultureInfo.InvariantCulture);

    public static readonly Regex SnapshotMetaFileRegex = _SnapshotMetaFileRegexC();
    [GeneratedRegex(@"^.*[\/\\]snapshot_[0-9]{18}\.json$")]
    private static partial Regex _SnapshotMetaFileRegexC();

    public const string SnapshotSearchString = "snapshot_??????????????????.json";
    
    public static string GetSnapshotFilepathFromTimestamp(string BackupDir, long Timestamp)
        => Path.Combine(BackupDir, $"snapshot_{Timestamp:000000000000000000}.json");

    public static string GetTimestampFromFilepath(string Filepath)
        => Filepath.Substring(Filepath.Length - 23, 18);

    public static readonly Regex PathBackRegex = _PathBackRegexC();
    [GeneratedRegex(@"[\/\\][^\/\\]*[\/\\]\.\.")]
    private static partial Regex _PathBackRegexC();

    public static readonly Regex ParentDirRegex = _ParentDirRegexC();
    [GeneratedRegex(@"^.*[\/\\](?=[^\/\\]+[\/\\]?$)")]
    private static partial Regex _ParentDirRegexC();

    public static string GetParentDir(string Path)
        => ParentDirRegex.Match(Path).Value;
}