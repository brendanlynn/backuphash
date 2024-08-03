namespace BackupHash;
static partial class Commands {
    static Commands() {
        _Init_Root();

        _Init_Gen();
        Root.AddCommand(Gen);

        _Init_Backup();
        Root.AddCommand(Backup);

        _Init_View();
        Root.AddCommand(View);

        _Init_Restore();
        Root.AddCommand(Restore);
    }
}