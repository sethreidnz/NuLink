using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;

namespace NuLink.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("NuLink tool");
            var rootCommand = BuildCommandLine();
            return rootCommand.InvokeAsync(args).Result;
        }
        
        private static RootCommand BuildCommandLine()
        {
            var consumerOption = new Option(new[] { "--consumer", "-c" }, HelpText.ProjectOption, new Argument<string>() {
                Name = "file-path",
                Arity = ArgumentArity.ZeroOrOne
            });
            var packageOption = new Option(new[] { "--package", "-p" }, HelpText.PackageOption, new Argument<string>() {
                Name = "package-id",
                Arity = ArgumentArity.ZeroOrOne
            });
            var dryRunOption = new Option(new[] { "--dry-run", "-d" }, HelpText.DryRunOption, new Argument<bool>() {
                Name = "on/off",
                Arity = ArgumentArity.ZeroOrOne
            });

            return new RootCommand() {
                new Command("status", HelpText.StatusCommand, handler: HandleStatus()) {
                    consumerOption,
                    packageOption
                },
                new Command("link", HelpText.LinkCommand, handler: HandleLink()) {
                    consumerOption,
                    packageOption,
                    dryRunOption
                },
                new Command("unlink", HelpText.UnlinkCommand, handler: HandleUnlink()) {
                    consumerOption,
                    packageOption,
                    dryRunOption
                }
            };
        }

        private static ICommandHandler HandleStatus() => 
            CommandHandler.Create<string, string>((consumer, package) => {
                return ExecuteCommand(consumer, package, dryRun: false, commandName: "status");
            });

        private static ICommandHandler HandleLink() => 
            CommandHandler.Create<string, string, bool>((consumer, package, dryRun) => {
                return ExecuteCommand(consumer, package, dryRun: false, commandName: "link");
            });

        private static ICommandHandler HandleUnlink() => 
            CommandHandler.Create<string, string, bool>((consumer, package, dryRun) => {
                return ExecuteCommand(consumer, package, dryRun: false, commandName: "unlink");
            });
        
        private static int ExecuteCommand(
            string projectPath, 
            string packageId,
            bool dryRun,
            string commandName)
        {
            projectPath = ValidateProjectFile(projectPath);
            if (projectPath == null)
            {
                return 1;
            }
            
            var options = new NuLinkCommandOptions(projectPath, packageId, dryRun);
            
            try
            {
                var command = CreateCommand(commandName, options);
                command.Execute(options);
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Fatal error: {e.Message}\nException: {e}");
                return 100;
            }
        }

        private static string ValidateProjectFile(string filePath)
        {
            if (filePath != null)
            {
                if (File.Exists(filePath))
                {
                    return filePath;
                }
                
                Console.Error.WriteLine($"Error: File does not exist: {filePath}");
                return null;
            }
            
            filePath = Directory
                .GetFiles(Directory.GetCurrentDirectory(), "*.*")
                .FirstOrDefault(path => 
                    path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || 
                    path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase));

            if (filePath == null)
            {
                Console.Error.WriteLine("Error: No .sln/.csproj file found in current directory, and --consumer was not specified");
                return null;
            }

            return filePath;
        }

        private static INuLinkCommand CreateCommand(string name, NuLinkCommandOptions options)
        {
            switch (name)
            {
                case "status":
                    return new StatusCommand();
                default:
                    throw new Exception($"Command not supported: {name}.");
            }
        }

        private static class HelpText
        {
            public const string StatusCommand = 
                "Check status of packages in consumer project/solution";
            public const string LinkCommand = 
                "Link package to a directory in local file system, machine-wide";
            public const string UnlinkCommand = 
                "Unlink package from local file system, machine-wide";
            public const string ProjectOption = 
                "Path to consumer .csproj or .sln (default: first .csproj or .sln in current directory)";
            public const string PackageOption = 
                "Package id (default: all packages in consumer project/solution)";
            public const string DryRunOption = 
                "If specified, list intended actions without executing them";
        }
    }
}