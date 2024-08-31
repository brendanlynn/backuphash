using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BackupHash;
static class BackupFiles {
    private static char _ByteToChar(int Data) {
        if (Data < 10)
            return (char)(Data + 0x30);
        else
            return (char)(Data + 0x41 - 10);
    }
    private static string _BytesToString(byte[] Bytes) {
        StringBuilder sb = new(Bytes.Length << 1) {
            Length = Bytes.Length << 1
        };
        for (int i = 0; i < Bytes.Length; i++) {
            int i2 = i << 1;
            sb[i2] = _ByteToChar((Bytes[i] & 0xF0) >> 4);
            sb[i2 | 1] = _ByteToChar(Bytes[i] & 0x0F);
        }
        return sb.ToString();
    }
    public static string GetFileHash(string Path, HashAlgorithm HashAlgorithm) {
        byte[] bytes;
        using (FileStream fs = new(Path, FileMode.Open, FileAccess.Read, FileShare.Read))
            bytes = HashAlgorithm.ComputeHash(fs);
        return _BytesToString(bytes);
    }
    public static string GetStringHash(string String, HashAlgorithm HashAlgorithm) {
        Encoding enc = Encoding.UTF8;
        byte[] bytes = HashAlgorithm.ComputeHash(enc.GetBytes(String));
        return _BytesToString(bytes);
    }
    public static (string Hash, bool IsNew) BackupFileNoUpdate(string Path, string BackupDir, HashAlgorithm HashAlgorithm, string Timestamp, bool RegardMeta) {
        string pathHash = GetStringHash(Path, HashAlgorithm);
        string metaPath = System.IO.Path.Combine(BackupDir, "lo_" + pathHash);
        FileInfo fi = new(Path);
        DateTime cCreation = fi.CreationTimeUtc;
        DateTime cModification = fi.LastWriteTimeUtc;
        DateTime lastModified = cCreation > cModification ? cCreation : cModification;
        long lastModifiedTicks = lastModified.Ticks;
        if (RegardMeta) {
            if (File.Exists(metaPath)) {
                try {
                    string metaData = File.ReadAllText(metaPath);
                    string[] metas = metaData.Split(';');
                    if (metas.Length == 2) {
                        long lastObtained = long.Parse(metas[0]);
                        string lastHash = metas[1];
                        if (lastModifiedTicks <= lastObtained && File.Exists(System.IO.Path.Combine(BackupDir, lastHash)))
                            return (lastHash, false);
                    }
                }
                catch { }
            }
        }
        string hash = GetFileHash(Path, HashAlgorithm);
        string filePath = System.IO.Path.Combine(BackupDir, hash);
        bool isNew = !File.Exists(filePath);
        if (isNew) _ = fi.CopyTo(filePath, false);
        string metaText = lastModifiedTicks.ToString() + ";" + hash;
        try {
            File.WriteAllText(metaPath, metaText);
        }
        catch { }
        return (hash, isNew);
    }
    public static (int Added, int Preexisting) TakeSnapshot(List<string> Files, string BackupDir, DateTime Time, string Timestamp, bool RegardMeta) {
        if (!Directory.Exists(BackupDir))
            _ = Directory.CreateDirectory(BackupDir);
        
        string snapshotPath = Path.Combine(BackupDir, $"snapshot_{Timestamp}.json");
        if (File.Exists(snapshotPath))
            throw new Exception($"Snapshot '{Timestamp}' already exists.");

        int added = 0;
        int preexisting = 0;
        List<SnapshotFileMeta> backedHashes = [];
        using HashAlgorithm hashAlg = SHA1.Create();
        foreach (string file in Files) {
            (string hash, bool isNew) = BackupFileNoUpdate(file, BackupDir, hashAlg, Timestamp, RegardMeta);
            if (isNew)
                ++added;
            else
                ++preexisting;
            backedHashes.Add(new SnapshotFileMeta(file, hash));
            UserInteraction.Info($"Backed up file '{file}' with hash '{hash}'.");
        }

        SnapshotMeta snapshotMeta = new(backedHashes, Time);
        string snapshotJson = JsonSerializer.Serialize(snapshotMeta);
        File.WriteAllText(snapshotPath, snapshotJson);

        return (added, preexisting);
    }
}