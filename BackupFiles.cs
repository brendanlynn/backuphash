using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BackupHash;
static class BackupFiles {
    private static char _ByteToChar(int Data) {
        if (Data < 10)
            return (char)(Data + 0x30);
        else
            return (char)(Data + 0x41);
    }
    private static string _BytesToString(byte[] Bytes) {
        StringBuilder sb = new(Bytes.Length << 1) {
            Length = Bytes.Length << 1
        };
        for (int i = 0; i < Bytes.Length; i++) {
            int i2 = i << 1;
            sb[i2] = _ByteToChar(Bytes[i] & 0x0F);
            sb[i2 | 1] = _ByteToChar((Bytes[i] & 0xF0) >> 4);
        }
        return sb.ToString();
    }
    public static string GetFileHash(string Path, HashAlgorithm HashAlgorithm) {
        byte[] bytes;
        using (FileStream fs = new(Path, FileMode.Open))
            bytes = HashAlgorithm.ComputeHash(fs);
        return _BytesToString(bytes);
    }
    public static (string Hash, bool IsNew) BackupFileNoUpdate(string Path, string BackupDir, HashAlgorithm HashAlgorithm, string Timestamp) {
        string hash = GetFileHash(Path, HashAlgorithm);
        FileInfo file = new(Path);
        string filePath = System.IO.Path.Combine(BackupDir, hash);
        bool isNew = !File.Exists(filePath);
        if (isNew) _ = file.CopyTo(filePath, false);
        return (hash, isNew);
    }
    public static (int Added, int Preexisting) TakeSnapshot(List<string> Files, string BackupDir, DateTime Time, string Timestamp) {
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
            (string hash, bool isNew) = BackupFileNoUpdate(file, BackupDir, hashAlg, Timestamp);
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