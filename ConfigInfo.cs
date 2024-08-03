using System.Text;
using System.Text.RegularExpressions;

namespace BackupHash;

class ConfigInfo {
    private readonly List<Line> _Lines;

    public ConfigInfo() {
        _Lines = [];
    }
    public ConfigInfo(string ConfigText)
        : this(ConfigText.Replace("\r", "").Split('\n')) { }
    public ConfigInfo(string[] ConfigLines) 
        : this() {
        for (int i = 0; i < ConfigLines.Length; i++) {
            string line = ConfigLines[i];

            string startLine = line;

            if (line.StartsWith('#'))
                continue;
            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (line.StartsWith("\\#"))
                line = line[1..];

            if (line.Contains('"'))
                throw new Exception("Character '\"' is not a valid path character.");
            else if (line.Contains('\\'))
                throw new Exception("Paths must be specified with a directory seperator of '/'.");

            line = line.TrimEnd();
            if (line.EndsWith('\\'))
                line = line[..^1];

            bool include = line.StartsWith('!');
            if (include || line.StartsWith("\\!"))
                line = line[1..];

            bool rooted = line[..^1].Contains('/');
            if (rooted) {
                if (line.StartsWith('/'))
                    line = line[1..];
            }
            bool directoryOnly = line.EndsWith('/');
            if (directoryOnly)
                line = line[..^1];

            line = Regex.Escape(line);

            line = line.Replace("\\*\\*/", "(.*|)");
            if (line.EndsWith("\\*\\*"))
                line = line[..^2];
            line = line
                .Replace("\\*", "[^/]*")
                .Replace("\\?", "[^/]");
            line = (rooted ? "^" : "") + line + "$";

            _Lines.Add(
                new Line(
                    include,
                    directoryOnly,
                    new Regex(
                        line,
                        RegexOptions.IgnoreCase | RegexOptions.Compiled
                    )
                )
            );
        }
    }

    public static ConfigInfo FromText(string Text)
        => new(Text);
    public static ConfigInfo FromFile(FileInfo File) {
        return FromText(File.FullName);
    }
    public static ConfigInfo FromFile(string File) {
        string text = System.IO.File.ReadAllText(File);
        return new(text);
    }
    public static ConfigInfo FromDirectory(DirectoryInfo Directory)
        => FromDirectory(Directory.FullName);
    public static ConfigInfo FromDirectory(string Directory) {
        string filePath = Path.Combine(Directory, Shared.ConfigFilename);
        string text;
        try {
            text = File.ReadAllText(filePath);
        }
        catch {
            return new();
        }
        return new(text);
    }
    public static bool ConditionalFromDirectory(DirectoryInfo Directory, out ConfigInfo? Config)
        => ConditionalFromDirectory(Directory.FullName, out Config);
    public static bool ConditionalFromDirectory(string Directory, out ConfigInfo? Config) {
        string filePath = Path.Combine(Directory, Shared.ConfigFilename);
        string text;
        try {
            text = File.ReadAllText(filePath);
        }
        catch {
            Config = null;
            return false;
        }
        Config = new(text);
        return true;
    }
    public static ConfigInfo UpdateFromDirectory(ConfigInfo CurrentInfo, DirectoryInfo Directory) {
        return UpdateFromDirectory(CurrentInfo, Directory.FullName);
    }
    public static ConfigInfo UpdateFromDirectory(ConfigInfo CurrentInfo, string Directory) {
        string filePath = Path.Combine(Directory, Shared.ConfigFilename);
        string text;
        try {
            text = File.ReadAllText(filePath);
        }
        catch {
            return CurrentInfo;
        }
        return new(text);
    }

    public bool IncludedFile(string RelativePath) {
        bool included = true;
        foreach (Line line in _Lines) {
            if (line.Include == included || line.DirectoryOnly)
                continue;
            if (line.Pattern.IsMatch(RelativePath.Replace(Path.DirectorySeparatorChar, '/')))
                included = line.Include;
        }
        return included;
    }
    public bool IncludedDirectory(string RelativePath)
    {
        bool included = true;
        foreach (Line line in _Lines)
        {
            if (line.Include == included)
                continue;
            if (line.Pattern.IsMatch(RelativePath.Replace(Path.DirectorySeparatorChar, '/')))
                included = line.Include;
        }
        return included;
    }

    private struct Line {
        public bool Include;
        public bool DirectoryOnly;
        public Regex Pattern;

        public Line(bool Include, bool DirectoryOnly, Regex Pattern) {
            this.Include = Include;
            this.DirectoryOnly = DirectoryOnly;
            this.Pattern = Pattern;
        }
    }
}