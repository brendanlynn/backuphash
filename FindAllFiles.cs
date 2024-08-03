namespace BackupHash;
static class FindAllFiles {
    public static List<string> Find(string BaseDirectory) {
        return Find(new DirectoryInfo(BaseDirectory));
    }
    public static List<string> Find(DirectoryInfo BaseDirectory) {
        ConfigInfo ci = ConfigInfo.FromDirectory(BaseDirectory);

        List<string> result = [];

        _GetSubs(BaseDirectory, ci, BaseDirectory, result);

        return result;
    }
    public static List<string> Find(DirectoryInfo WorkingDirectory, ConfigInfo UpperInfo, DirectoryInfo Directory) {
        List<string> result = [];
        Find(WorkingDirectory, UpperInfo, Directory, result);
        return result;
    }
    public static void Find(DirectoryInfo WorkingDirectory, ConfigInfo UpperInfo, DirectoryInfo Directory, List<string> FileList) {
        ConfigInfo ci = ConfigInfo.UpdateFromDirectory(UpperInfo, Directory);

        if (ci != UpperInfo)
            WorkingDirectory = Directory;

        _GetSubs(WorkingDirectory, ci, Directory, FileList);
    }

    private static void _GetSubs(DirectoryInfo WorkingDirectory, ConfigInfo CurrentInfo, DirectoryInfo Directory, List<string> FileList) {
        FileInfo[] files = Directory.GetFiles();
        DirectoryInfo[] directories = Directory.GetDirectories();

        foreach (FileInfo file in files) {
            string relativePath = Path.GetRelativePath(WorkingDirectory.FullName, file.FullName);
            if (file.Name != Shared.ConfigFilename && CurrentInfo.IncludedFile(relativePath))
                FileList.Add(file.FullName);
        }

        foreach (DirectoryInfo dir in directories) {
            string relativePath = Path.GetRelativePath(WorkingDirectory.FullName, dir.FullName);
            if (CurrentInfo.IncludedDirectory(relativePath))
                Find(WorkingDirectory, CurrentInfo, dir, FileList);
        }
    }
}