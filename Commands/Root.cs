using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using static System.Console;

namespace BackupHash;
static partial class Commands {
    private static void _Root() {
        WriteLine("Welcome to BackupHash v1.0.0!");
        WriteLine("by Brendan Lynn!");
    }
    public static RootCommand Root = new("A CLI for users to back up their files without redundancy.");
    private static void _Init_Root() {
        Root.Handler = CommandHandler.Create(_Root);
    }
}