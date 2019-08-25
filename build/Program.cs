using System;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using static Bullseye.Targets;
using static SimpleExec.Command;

namespace build
{
    class Program
    {
        private const bool RequireTests = true;

        private const string ArtifactsDir = "artifacts";
        private const string Build = "build";
        private const string Test = "test";
        private const string Pack = "pack";

        static void Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);

            CleanArtifacts();

            app.OnExecute(() =>
            {
                Target(Build, () => 
                {
                    var project = Directory.GetFiles("./src", "*.csproj", SearchOption.TopDirectoryOnly).First();

                    Run("dotnet", $"build {project} -c Release");
                });

                Target(Test, DependsOn(Build), () => 
                {
                    try
                    {
                        var tests = Directory.GetFiles("./test", "*.csproj", SearchOption.AllDirectories);

                        foreach (var test in tests)
                        {
                            Run("dotnet", $"test {test} -c Release");
                        }    
                    }
                    catch (System.IO.DirectoryNotFoundException ex)
                    {
                        if (RequireTests)
                        {
                            throw new Exception($"No tests found: {ex.Message}");
                        };
                    }
                });

                Target(Pack, DependsOn(Build), () => 
                {
                    var project = Directory.GetFiles("./src", "*.csproj", SearchOption.TopDirectoryOnly).First();

                    Run("dotnet", $"pack {project} -c Release -o ../{ArtifactsDir} --no-build");
                });

                Target("default", DependsOn(Test, Pack));
                RunTargetsAndExit(app.RemainingArguments);
            });

            app.Execute(args);
        }

        private static void CleanArtifacts()
        {
            Directory.CreateDirectory($"./{ArtifactsDir}");

            foreach (var file in Directory.GetFiles($"./{ArtifactsDir}"))
            {
                File.Delete(file);
            }
        }
    }
}
