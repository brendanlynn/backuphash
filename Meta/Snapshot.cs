namespace BackupHash;

class SnapshotMeta {
    public SnapshotFileMeta[] Files { get; set; }
    public DateTime Time { get; set; }
    public SnapshotMeta() {
        Files = default!;
        Time = default;
    }
    public SnapshotMeta(IEnumerable<SnapshotFileMeta> Files, DateTime Time) {
        this.Files = Files.ToArray();
        this.Time = Time;
    }
}

struct SnapshotFileMeta {
    public string Filepath { get; set; }
    public string Hash { get; set; }
    public SnapshotFileMeta() {
        Filepath = "";
        Hash = "";
    }
    public SnapshotFileMeta(string Filepath, string Hash) {
        this.Filepath = Filepath;
        this.Hash = Hash;
    }
}