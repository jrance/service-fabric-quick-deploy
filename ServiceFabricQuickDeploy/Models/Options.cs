using CommandLine;

namespace ServiceFabricQuickDeploy.Models
{
    public class Options
    {
        [Option('a', "attach", DefaultValue = true,
             HelpText = "Determines whether the Visual Studio debugger should be automatically attached after deploy")]
        public bool AttachDebugger { get; set; }
    }
}