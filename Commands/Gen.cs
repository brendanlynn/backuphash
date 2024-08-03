using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace BackupHash;
static partial class Commands {
    private static void _Gen(bool ow) {
        string filePath = Path.Combine(Shared.WorkingDirectory, Shared.ConfigFilename);
        bool exists = File.Exists(filePath);
        if (!ow) {
            if (exists)
            {
                if (!UserInteraction.Warning_PromptContinue($"Config file '{Shared.ConfigFilename}' already exists."))
                {
                    UserInteraction.Fatal("Operation cancelled by user.");
                    return;
                }
            }
        }
        try {
            File.WriteAllText(filePath, Shared.DefaultConfig);
        }
        catch (Exception e) {
            UserInteraction.Fatal(e.Message);
            return;
        }
        if (exists)
            UserInteraction.Info($"Successfully replaced config file '{filePath}' with default.");
        else
            UserInteraction.Info($"Successfully added config file '{filePath}'.");
    }
    public static Command Gen = new("gen", $"Generates a default '{Shared.ConfigFilename}' file in the working directory.") {
        new Option<bool>(["--overwrite", "-ow"], "Overwrites the file if it already exists, without asking the user.")
    };
    private static void _Init_Gen() {
        Gen.Handler = CommandHandler.Create<bool>(_Gen);
    }
}